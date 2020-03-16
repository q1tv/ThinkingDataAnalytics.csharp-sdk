using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.IO.Compression;

namespace ThinkingData.Analytics
{
    public interface IConsumer
    {
        void Send(Dictionary<string, Object> message);

        void Flush();

        void Close();
    }

    public class LoggerConsumer : IConsumer
    {
        private readonly JavaScriptSerializer js = new JavaScriptSerializer();
        private readonly String logDirectory;
        private readonly StringBuilder messageBuffer;
        private readonly int bufferSize = 8192;
        private readonly int fileSize;
        private RotateMode rotateHourly;
        private InnerLoggerFileWriter fileWriter;

        public enum RotateMode
        {
            /** 按日切分 */
            DAILY,

            /** 按小时切分 */
            HOURLY
        }

        //默认按天切分文件
        public LoggerConsumer(String logDirectory) : this(logDirectory, RotateMode.DAILY)
        {
        }

        public LoggerConsumer(String logDirectory, int fileSize) : this(logDirectory, RotateMode.DAILY, fileSize)
        {
        }
        
        // 默认情况下不限制文件大小 fileSize = 0
        public LoggerConsumer(String logDirectory, RotateMode rotateHourly) : this(logDirectory, rotateHourly, 0)
        {
        }


        public LoggerConsumer(String logDirectory, RotateMode rotateHourly, int fileSize)
        {
            this.logDirectory = logDirectory+"/";
            this.rotateHourly = rotateHourly;
            this.fileSize = fileSize;
            this.messageBuffer = new StringBuilder(bufferSize);
        }


        public virtual void Send(Dictionary<string, Object> message)
        {
            lock (this)
            {
                try
                {
                    messageBuffer.Append(js.Serialize(message));
                    messageBuffer.Append("\r\n");
                }
                catch (Exception e)
                {
                    throw new SystemException("Failed to add data", e);
                }

                if (messageBuffer.Length >= bufferSize)
                {
                    this.Flush();
                }
            }
        }

        public void Flush()
        {
            lock (this)
            {
                if (messageBuffer.Length == 0)
                {
                    return;
                }

                String fileName = this.getFileName();

                if (fileWriter != null && !fileWriter.IsValid(fileName))
                {
                    InnerLoggerFileWriter.RemoveInstance(fileWriter);
                    fileWriter = null;
                }

                if (fileWriter == null)
                {
                    fileWriter = InnerLoggerFileWriter.GetInstance(fileName);
                }

                if (fileWriter.Write(messageBuffer))
                {
                    messageBuffer.Length = 0;
                }
            }
        }

        public String getFileName()
        {
            var sdf = this.rotateHourly == RotateMode.HOURLY
                ? DateTime.Now.ToString("yyyy-MM-dd-HH")
                : DateTime.Now.ToString("yyyy-MM-dd");
            String file_base = logDirectory + "log." + sdf + "_";
            int count = 0;
            String file_complete = file_base + count;
            FileInfo target = new FileInfo(file_complete);
            if (this.fileSize > 0)
            {
                while (File.Exists(file_complete) && fileSizeOut(target))
                {
                    count += 1;
                    file_complete = file_base + count;
                    target = new FileInfo(file_complete);
                }
            }

            return file_complete;
        }

        public Boolean fileSizeOut(FileInfo target)
        {
            long fsize = target.Length;
            fsize = fsize / (1024 * 1024);
            if (fsize >= fileSize)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Close()
        {
            this.Flush();
            if (fileWriter != null)
            {
                InnerLoggerFileWriter.RemoveInstance(fileWriter);
                fileWriter = null;
            }
        }

        private class InnerLoggerFileWriter
        {
            private static Dictionary<String, InnerLoggerFileWriter> instances =
                new Dictionary<string, InnerLoggerFileWriter>();

            private readonly String fileName;
            private readonly Mutex mutex;
            private readonly FileStream outputStream;
            private int refCount;

            private InnerLoggerFileWriter(String fileName)
            {
                this.outputStream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                this.fileName = fileName;
                this.refCount = 0;
                String mutexName = "Global\\ThinkingdataAnalytics " + Path.GetFullPath(fileName).Replace('\\', '_');
                this.mutex = new Mutex(false, mutexName);
            }

            public static InnerLoggerFileWriter GetInstance(String fileName)
            {
                lock (instances)
                {
                    if (!instances.ContainsKey(fileName))
                    {
                        instances.Add(fileName, new InnerLoggerFileWriter(fileName));
                    }

                    InnerLoggerFileWriter writer = instances[fileName];
                    writer.refCount = writer.refCount + 1;
                    return writer;
                }
            }

            public static void RemoveInstance(InnerLoggerFileWriter writer)
            {
                lock (instances)
                {
                    writer.refCount = writer.refCount - 1;
                    if (writer.refCount == 0)
                    {
                        writer.Close();
                        instances.Remove(writer.fileName);
                    }
                }
            }

            public void Close()
            {
                outputStream.Close();
                mutex.Close();
            }

            public bool IsValid(String fileName)
            {
                return this.fileName.Equals(fileName);
            }

            public bool Write(StringBuilder data)
            {
                lock (outputStream)
                {
                    mutex.WaitOne();
                    outputStream.Seek(0, SeekOrigin.End);
                    byte[] bytes = Encoding.UTF8.GetBytes(data.ToString());
                    outputStream.Write(bytes, 0, bytes.Length);
                    outputStream.Flush();
                    mutex.ReleaseMutex();
                }

                return true;
            }
        }
    }

    public class BatchConsumer : IConsumer
    {
        private readonly static int MAX_FLUSH_BATCH_SIZE = 20;
        private readonly static int DEFAULT_TIME_OUT_SECOND = 30;

        private readonly List<Dictionary<string, Object>> messageList;
        private readonly JavaScriptSerializer js;


        private readonly String url;
        private readonly String appId;
        private readonly int batchSize;
        private readonly int requestTimeoutMillisecond;
        private readonly bool throwException;
        private readonly bool compress;

        public BatchConsumer(string serverUrl, string appId) : this(serverUrl, appId,MAX_FLUSH_BATCH_SIZE, DEFAULT_TIME_OUT_SECOND,false,true)
        {
        }
        
        /**
         * 数据是否需要压缩，compress 内网可设置 false
         */
        public BatchConsumer(string serverUrl, string appId,bool compress) : this(serverUrl, appId,MAX_FLUSH_BATCH_SIZE, DEFAULT_TIME_OUT_SECOND,false,compress)
        {
        }
        /**
         * batchSize 每次flush到TA的数据条数，默认20条
         */

        public BatchConsumer(string serverUrl, string appId, int batchSize) : this(serverUrl, appId, batchSize,DEFAULT_TIME_OUT_SECOND)
        {
        }

        /**
         * batchSize 每次flush到TA的数据条数，默认20条
         * requestTimeoutSecond 发送服务器请求时间设置，默认30s
         */
        public BatchConsumer(string serverUrl, string appId, int batchSize, int requestTimeoutSecond) : this(serverUrl,
            appId, batchSize, requestTimeoutSecond, false,true)
        {
        }
        
        public BatchConsumer(string serverUrl, string appId, int batchSize, int requestTimeoutSecond, bool throwException,bool compress = true)
        {
            messageList = new List<Dictionary<string, object>>();
            js = new JavaScriptSerializer();
            Uri relativeUri = new Uri("/sync_server", UriKind.Relative);
            url = new Uri(new Uri(serverUrl), relativeUri).AbsoluteUri;
            this.appId = appId;
            this.batchSize = Math.Min(MAX_FLUSH_BATCH_SIZE, batchSize);
            this.throwException = throwException;
            this.compress = compress;
            this.requestTimeoutMillisecond = requestTimeoutSecond * 1000;
        }

        public void Send(Dictionary<string, Object> message)
        {
            lock (messageList)
            {
                messageList.Add(message);
                if (messageList.Count >= batchSize)
                {
                    this.Flush();
                }
            }
        }

        public void Flush()
        {
            lock (messageList)
            {
                while (messageList.Count != 0)
                {
                    int batchRecordCount = Math.Min(batchSize, messageList.Count);
                    List<Dictionary<string, Object>> batchList = messageList.GetRange(0, batchRecordCount);
                    string sendingData;
                    try
                    {
                        sendingData = js.Serialize(batchList);
                    }
                    catch (Exception exception)
                    {
                        messageList.RemoveRange(0, batchRecordCount);
                        if (throwException)
                        {
                            throw new SystemException("Failed to serialize data.", exception);
                        }

                        continue;
                    }

                    try
                    {
                        this.SendToServer(sendingData);
                        messageList.RemoveRange(0, batchRecordCount);
                    }
                    catch (Exception exception)
                    {
                        if (throwException)
                        {
                            throw new SystemException("Failed to send message with BatchConsumer.", exception);
                        }

                        return;
                    }
                }
            }
        }

        private void SendToServer(string dataStr)
        {
            try
            {
                byte[] dataBytes =Encoding.UTF8.GetBytes(dataStr);
                byte[] dataCompressed = null;
                if (this.compress)
                {
                    dataCompressed = Gzip(dataStr);
                }
                else
                {
                    dataCompressed = dataBytes;
                }
                
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(this.url);
                request.Method = "POST";
                request.ReadWriteTimeout = requestTimeoutMillisecond;
                request.Timeout = requestTimeoutMillisecond;
                request.UserAgent = "C# SDK";
                request.Headers.Set("appid", this.appId);
                request.Headers.Set("compress",this.compress ? "gzip" : "none");
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(dataCompressed, 0, dataCompressed.Length);
                    stream.Flush();
                }

                HttpWebResponse response = (HttpWebResponse) request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();


                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new SystemException("C# SDK send response is not 200, content: " + responseString);
                }
                response.Close();
                Dictionary<string,object> resultJson = (Dictionary<string, object>)js.DeserializeObject(responseString);

                int code = (int) resultJson["code"];

                if (code != 0)
                {
                    if (code == -1)
                    {
                        throw new SystemException("error msg:" +
                                                  (resultJson.ContainsKey("msg")
                                                      ? resultJson["msg"]
                                                      : "invalid data format"));
                    }
                    else if (code == -2)
                    {
                        throw new SystemException("error msg:" +
                                                  (resultJson.ContainsKey("msg")
                                                      ? resultJson["msg"]
                                                      : "APP ID doesn't exist"));
                    }
                    else if (code == -3)
                    {
                        throw new SystemException("error msg:" +
                                                  (resultJson.ContainsKey("msg")
                                                      ? resultJson["msg"]
                                                      : "invalid ip transmission"));
                    }
                    else
                    {
                        throw new SystemException("Unexpected response return code: " + code);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e + "\n  Cannot post message to " + this.url);
                throw;
            }
        }

        private byte[] Gzip(string inputStr)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputStr);
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
                    gzipStream.Write(inputBytes, 0, inputBytes.Length);
                return outputStream.ToArray();
            }
        }

        public void Close()
        {
            Flush();
        }
    }

    //逐条传输数据，如果发送失败则抛出异常
    public class DebugConsumer : IConsumer
    {
        private readonly string url;
        private readonly string appId;
        private readonly int requestTimeout = 30000;
        private readonly JavaScriptSerializer js;
        private readonly bool writeData;

        public DebugConsumer(string serverUrl, string appId) : this(serverUrl, appId, 30000)
        {
        }

        public DebugConsumer(string serverUrl, string appId, bool writeData):this(serverUrl,appId,30000,writeData)
        {   
        }

        public DebugConsumer(string serverUrl, String appId, int requestTimeout,bool writeData = true)
        {
            Uri relativeUri = new Uri("/data_debug", UriKind.Relative);
            url = new Uri(new Uri(serverUrl), relativeUri).AbsoluteUri;
            js = new JavaScriptSerializer();
            this.appId = appId;
            this.requestTimeout = requestTimeout;
            this.writeData = writeData;
        }

        public void Send(Dictionary<string, Object> message)
        {
            string sendingData;
            try
            {
                sendingData = js.Serialize(message);
                this.SendToServer(sendingData);
            }
            catch (Exception exception)
            {
                throw new SystemException("Failed to send message with DebugConsumer.", exception);
            }
        }


        private void SendToServer(string dataStr)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest) WebRequest.Create(url);

                req.Method = "POST";

                req.ContentType = "application/x-www-form-urlencoded";

                int dryRun = 1;
                if (this.writeData)
                {
                    dryRun = 0;
                }

                var postData = "appid=" + this.appId + "&source=server&dryRun="+dryRun+"&data=" + dataStr;


                byte[] data = Encoding.UTF8.GetBytes(postData);

                req.ContentLength = data.Length;

                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);

                    reqStream.Close();
                }

                HttpWebResponse response = (HttpWebResponse) req.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new SystemException("C# SDK send response is not 200, content: " + responseString);
                }
                response.Close();
                Dictionary<string,object> resultJson = (Dictionary<string, object>)js.DeserializeObject(responseString);

                 int errorLevel = (int) resultJson["errorLevel"];

                if (errorLevel != 0)
                {
                   throw new Exception("\n Can't send because :\n"+responseString);
                }
            }
            catch (Exception e)
            {
                throw new SystemException(e + "\n Cannot post message to " + this.url);
            }
        }
        
        public void Flush()
        {
        }

        public void Close()
        {
        }
    }
}