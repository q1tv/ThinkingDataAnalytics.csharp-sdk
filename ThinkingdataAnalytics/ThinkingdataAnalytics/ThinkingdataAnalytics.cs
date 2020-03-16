using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ThinkingData.Analytics
{
    public class ThinkingdataAnalytics
    {
        private static readonly String LIB_VERSION = "1.2.1";
        private static readonly String LIB_NAME = "tga_csharp_sdk";

        private static readonly Regex KEY_PATTERN =
            new Regex("^(#[a-z][a-z0-9_]{0,49})|([a-z][a-z0-9_]{0,50})$", RegexOptions.IgnoreCase);

        private IConsumer consumer;

        private Dictionary<String, Object> pubicProperties;

        /*
         * 实例化tga类，接收一个Consumer的一个实例化对象
         * @param consumer	BatchConsumer,LoggerConsumer实例
        */
        public ThinkingdataAnalytics(IConsumer consumer)
        {
            this.consumer = consumer;
            this.pubicProperties = new Dictionary<String, Object>();
            this.ClearPublicProperties();
        }

        /*
        * 公共属性只用于track接口，其他接口无效，且每次都会自动向track事件中添加公共属性
        * @param properties	公共属性
        */
        public void SetPublicProperties(Dictionary<String, Object> properties)
        {
            lock (this.pubicProperties)
            {
                foreach (KeyValuePair<String, Object> kvp in properties)
                {
                    this.pubicProperties[kvp.Key] = kvp.Value;
                }
            }
        }

        /*
	     * 清理掉公共属性，之后track属性将不会再加入公共属性
	     */
        public void ClearPublicProperties()
        {
            lock (this.pubicProperties)
            {
                this.pubicProperties.Clear();
                this.pubicProperties.Add("#lib", ThinkingdataAnalytics.LIB_NAME);
                this.pubicProperties.Add("#lib_version", ThinkingdataAnalytics.LIB_VERSION);
            }
        }

        // 记录一个没有任何属性的事件
        public void Track(String account_id, String distinct_id, String event_name)
        {
            _Add(account_id, distinct_id, "track", event_name, null);
        }

        /*
	    * 用户事件属性(注册)
	    * @param	account_id	账号ID
	    * @param	distinct_id	匿名ID
	    * @param	event_name	事件名称
	    * @param	properties	事件属性
	    */
        public void Track(String account_id, String distinct_id, String event_name,
            Dictionary<String, Object> properties)
        {
            _Add(account_id, distinct_id, "track", event_name, properties);
        }

        /*
         * 设置用户属性，如果已经存在，则覆盖，否则，新创建
         * @param	account_id	账号ID
         * @param	distinct_id	匿名ID
         * @param	properties	增加的用户属性
	     */
        public void UserSet(String account_id, String distinct_id, Dictionary<String, Object> properties)
        {
            _Add(account_id, distinct_id, "user_set", properties);
        }
        
        /*
        * 删除用户属性
        * @param account_id 账号 ID
        * @param distinct_id 访客 ID
        * @param properties 用户属性
        */
        public void UserUnSet(String account_id, String distinct_id, List<string> properties)
        {
            Dictionary<String, Object> props = new Dictionary<String, Object>();
            foreach (String property in properties)
            {
                props.Add(property, 0);
            }
           
            _Add(account_id, distinct_id, "user_unset", props);
        }

        /**
          * 设置用户属性，首次设置用户的属性,如果该属性已经存在,该操作为无效.
          * @param	account_id	账号ID
          * @param	distinct_id	匿名ID
          * @param	properties	增加的用户属性
         */
        public void UserSetOnce(String account_id, String distinct_id, Dictionary<String, Object> properties)
        {
            _Add(account_id, distinct_id, "user_setOnce", properties);
        }

        /**
          * 首次设置用户的属性。这个接口只能设置单个key对应的内容。
          * @param	account_id	账号ID
          * @param	distinct_id	匿名ID
          * @param	properties	增加的用户属性
         */
        public void UserSetOnce(String account_id, String distinct_id, String property, Object value)
        {
            Dictionary<String, Object> properties = new Dictionary<String, Object>();
            properties.Add(property, value);
            _Add(account_id, distinct_id, "user_setOnce", properties);
        }

        /*
          * 用户属性修改，只支持数字属性增加的接口
          * @param	account_id	账号ID
          * @param	distinct_id	匿名ID
          * @param	properties	增加的用户属性
         */
        public void UserAdd(String account_id, String distinct_id, Dictionary<String, Object> properties)
        {
            _Add(account_id, distinct_id, "user_add", properties);
        }

        public void UserAdd(String account_id, String distinct_id, String property, long value)
        {
            Dictionary<String, Object> properties = new Dictionary<String, Object>();
            properties.Add(property, value);
            _Add(account_id, distinct_id, "user_add", properties);
        }
        
        /*
          * 追加用户的集合类型的一个或多个属性
          * @param	account_id	账号ID
          * @param	distinct_id	匿名ID
          * @param	properties	增加的用户属性
         */
        public void UserAppend(String account_id, String distinct_id, Dictionary<String, Object> properties)
        {
            _Add(account_id, distinct_id, "user_append", properties);
        }

        /**
         * 用户删除,此操作不可逆
         * @param 	account_id	账号ID
         * @param	distinct_id	匿名ID
        */
        public void UserDelete(String account_id, String distinct_id)
        {
            _Add(account_id, distinct_id, "user_del", new Dictionary<String, Object>());
        }

        /// 立即发送缓存中的所有日志
        public void Flush()
        {
            this.consumer.Flush();
        }

        //关闭并退出 sdk 所有线程，停止前会清空所有本地数据
        public void Close()
        {
            this.consumer.Close();
        }

        private void AssertKey(String type, String key)
        {
            if (key == null || key.Length < 1)
            {
                throw new ArgumentNullException("The " + type + " is empty.");
            }

            if (key.Length > 50)
            {
                throw new ArgumentOutOfRangeException("The " + type + " is too long, max length is 50.");
            }
        }

        private void AssertKeyWithRegex(String type, String key)
        {
            AssertKey(type, key);
            if (!KEY_PATTERN.IsMatch(key))
            {
                throw new ArgumentException("The " + type + "'" + key + "' is invalid.");
            }
        }

        private bool IsNumber(Object value)
        {
            return (value is sbyte) || (value is short) || (value is int) || (value is long) || (value is byte)
                   || (value is ushort) || (value is uint) || (value is ulong) || (value is decimal) ||
                   (value is Single)
                   || (value is float) || (value is double);
        }

        private void AssertProperties(String type, Dictionary<String, Object> properties)
        {
            if (null == properties)
            {
                return;
            }

            foreach (KeyValuePair<String, Object> kvp in properties)
            {
                string key = kvp.Key;
                Object value = kvp.Value;
                if (null == value)
                {
                    continue;
                }

                AssertKeyWithRegex("property", kvp.Key);

                if (!this.IsNumber(value) && !(value is string) && !(value is DateTime) && !(value is bool) && !(value is IList))
                {
                    throw new ArgumentException(
                        "The supported data type including: Number, String, Date, Boolean,List. Invalid property: {key}");
                }

                if (value is List<object>)
                {
                    
                    List<object> list = value as List<object>;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] is DateTime)
                        {
                            list[i] = (DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss.fff");
                        }
                        
                    }
                }

                if (type == "user_add" && !this.IsNumber(value))
                {
                    throw new ArgumentException("Only Number is allowed for user_add. Invalid property:" + key);
                }

                if (key == "#time" && !(value is DateTime))
                {
                    throw new ArgumentException("The property value of key '#time' should be a DateTime type.");
                }
            }
        }

        private void _Add(String account_id, String distinct_id, String type, Dictionary<String, Object> properties)
        {
            _Add(account_id, distinct_id, type, null, properties);
        }

        private void _Add(String account_id, String distinct_id, String type, String event_name,
            Dictionary<String, Object> properties)
        {
            if (account_id == null && distinct_id == null)
            {
                throw new SystemException("account_id or distinct_id must be provided. ");
            }

            if (type.Equals("track"))
            {
                AssertKey("eventName", event_name);
                AssertKeyWithRegex("eventName", event_name);
            }

            Dictionary<String, Object> eventProperties = new Dictionary<String, Object>();
            Dictionary<String, Object> evt = new Dictionary<String, Object>();
            if (account_id != null)
            {
                evt.Add("#account_id", account_id);
            }

            if (distinct_id != null)
            {
                evt.Add("#distinct_id", distinct_id);
            }

            if (event_name != null)
            {
                evt.Add("#event_name", event_name);
            }
            
            //#uuid 只支持UUID标准格式xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
            if (properties != null && properties.ContainsKey("#uuid"))
            {
                evt.Add("#uuid", properties["#uuid"]);
                properties.Remove("#uuid");
            }
            
            AssertProperties(type, properties);
            if (properties != null && properties.ContainsKey("#ip"))
            {
                evt.Add("#ip", properties["#ip"]);
                properties.Remove("#ip");
            }           

            if (type.Equals("track"))
            {
                foreach (KeyValuePair<String, Object> kvp in pubicProperties)
                {
                    eventProperties.Add(kvp.Key, kvp.Value);
                }
            }

            if (properties != null)
            {
                foreach (KeyValuePair<String, Object> kvp in properties)
                {
                    if (kvp.Value is DateTime)
                    {
                        eventProperties[kvp.Key] = ((DateTime) kvp.Value).ToString("yyyy-MM-dd HH:mm:ss.fff");
                    }
                    else
                    {
                        eventProperties[kvp.Key] = kvp.Value;
                    }
                }
            }

            if (eventProperties != null && eventProperties.ContainsKey("#time"))
            {
                evt.Add("#time", eventProperties["#time"]);
                eventProperties.Remove("#time");
            }
            else
            {
                evt.Add("#time", (DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }

            evt.Add("#type", type);
            evt.Add("properties", eventProperties);
            this.consumer.Send(evt);
        }
    }
}