using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ThinkingData.Analytics
{
    /* 
     * 动态公共属性
     */
    public interface IDynamicPublicProperties
    {
        Dictionary<string, object> GetDynamicPublicProperties();
    }

    public class ThinkingdataAnalytics
    {
        private const string LibVersion = "1.5.1";
        private const string LibName = "tga_csharp_sdk";

        private static readonly Regex KeyPattern =
            new Regex("^(#[a-z][a-z0-9_]{0,49})|([a-z][a-z0-9_]{0,50})$", RegexOptions.IgnoreCase);

        private readonly IConsumer _consumer;

        private readonly bool _enableUuid;

        private readonly Dictionary<string, object> _pubicProperties;

        private IDynamicPublicProperties _dynamicPublicProperties;


        /*
         * 实例化tga类，接收一个Consumer的一个实例化对象
         * @param consumer	BatchConsumer,LoggerConsumer实例
        */
        public ThinkingdataAnalytics(IConsumer consumer) : this(consumer, false)
        {
        }

        public ThinkingdataAnalytics(IConsumer consumer, bool enableUuid)
        {
            _consumer = consumer;
            _enableUuid = enableUuid;
            _pubicProperties = new Dictionary<string, object>();
            ClearPublicProperties();
        }

        /*
        * 公共属性只用于track接口，其他接口无效，且每次都会自动向track事件中添加公共属性
        * @param properties	公共属性
        */
        public void SetPublicProperties(Dictionary<string, object> properties)
        {
            lock (_pubicProperties)
            {
                foreach (var kvp in properties)
                {
                    _pubicProperties[kvp.Key] = kvp.Value;
                }
            }
        }

        /*
	     * 清理掉公共属性，之后track属性将不会再加入公共属性
	     */
        public void ClearPublicProperties()
        {
            lock (_pubicProperties)
            {
                _pubicProperties.Clear();
                _pubicProperties.Add("#lib", LibName);
                _pubicProperties.Add("#lib_version", LibVersion);
            }
        }

        public void SetDynamicPublicProperties(IDynamicPublicProperties dynamicPublicProperties)
        {
            _dynamicPublicProperties = dynamicPublicProperties;
        }

            // 记录一个没有任何属性的事件
            public void Track(string account_id, string distinct_id, string event_name)
        {
            _Add(account_id, distinct_id, "track", event_name, null, null);
        }

        /*
	    * 用户事件属性(注册)
	    * @param	account_id	账号ID
	    * @param	distinct_id	匿名ID
	    * @param	event_name	事件名称
	    * @param	properties	事件属性
	    */
        public void Track(string account_id, string distinct_id, string event_name,
            Dictionary<string, object> properties)
        {
            if (string.IsNullOrEmpty(event_name))
            {
                throw new SystemException("The event name must be provided.");
            }

            _Add(account_id, distinct_id, "track", event_name, null, properties);
        }

        /*
        * 首次事件属性
        * @param	account_id	    账号ID
        * @param	distinct_id	    匿名ID
        * @param	event_name	    事件名称
        * @param    first_check_id  事件ID
        * @param	properties	    事件属性
        */
        public void TrackFirst(string account_id, string distinct_id, string event_name, string first_check_id,
            Dictionary<string, object> properties)
        {
            if (string.IsNullOrEmpty(event_name))
            {
                throw new SystemException("The event name must be provided.");
            }

            if (string.IsNullOrEmpty(first_check_id))
            {
                throw new SystemException("The first check id must be provided.");
            }

            _Add(account_id, distinct_id, "track_first", event_name, first_check_id, properties);
        }
        /*
	    * 可更新事件属性
	    * @param	account_id	账号ID
	    * @param	distinct_id	匿名ID
	    * @param	event_name	事件名称
        * @param    event_id    事件ID
	    * @param	properties	事件属性
	    */
        public void TrackUpdate(string account_id, string distinct_id, string event_name, string event_id,
            Dictionary<string, object> properties)
        {
            if (string.IsNullOrEmpty(event_name))
            {
                throw new SystemException("The event name must be provided.");
            }

            if (string.IsNullOrEmpty(event_id))
            {
                throw new SystemException("The event id must be provided.");
            }

            _Add(account_id, distinct_id, "track_update", event_name, event_id, properties);
        }

        /*
	    * 可重写事件属性
	    * @param	account_id	账号ID
	    * @param	distinct_id	匿名ID
	    * @param	event_name	事件名称
        * @param    event_id    事件ID
	    * @param	properties	事件属性
	    */
        public void TrackOverwrite(string account_id, string distinct_id, string event_name, string event_id,
            Dictionary<string, object> properties)
        {
            if (string.IsNullOrEmpty(event_name))
            {
                throw new SystemException("The event name must be provided.");
            }

            if (string.IsNullOrEmpty(event_id))
            {
                throw new SystemException("The event id must be provided.");
            }

            _Add(account_id, distinct_id, "track_overwrite", event_name, event_id, properties);
        }

        /*
         * 设置用户属性，如果已经存在，则覆盖，否则，新创建
         * @param	account_id	账号ID
         * @param	distinct_id	匿名ID
         * @param	properties	增加的用户属性
	     */
        public void UserSet(string account_id, string distinct_id, Dictionary<string, object> properties)
        {
            _Add(account_id, distinct_id, "user_set", properties);
        }

        /*
        * 删除用户属性
        * @param account_id 账号 ID
        * @param distinct_id 访客 ID
        * @param properties 用户属性
        */
        public void UserUnSet(string account_id, string distinct_id, List<string> properties)
        {
            var props = properties.ToDictionary<string, string, object>(property => property, property => 0);
            _Add(account_id, distinct_id, "user_unset", props);
        }

        /**
          * 设置用户属性，首次设置用户的属性,如果该属性已经存在,该操作为无效.
          * @param	account_id	账号ID
          * @param	distinct_id	匿名ID
          * @param	properties	增加的用户属性
         */
        public void UserSetOnce(string account_id, string distinct_id, Dictionary<string, object> properties)
        {
            _Add(account_id, distinct_id, "user_setOnce", properties);
        }

        /**
          * 首次设置用户的属性。这个接口只能设置单个key对应的内容。
          * @param	account_id	账号ID
          * @param	distinct_id	匿名ID
          * @param	properties	增加的用户属性
         */
        public void UserSetOnce(string account_id, string distinct_id, string property, object value)
        {
            var properties = new Dictionary<string, object> {{property, value}};
            _Add(account_id, distinct_id, "user_setOnce", properties);
        }

        /*
          * 用户属性修改，只支持数字属性增加的接口
          * @param	account_id	账号ID
          * @param	distinct_id	匿名ID
          * @param	properties	增加的用户属性
         */
        public void UserAdd(string account_id, string distinct_id, Dictionary<string, object> properties)
        {
            _Add(account_id, distinct_id, "user_add", properties);
        }

        public void UserAdd(string account_id, string distinct_id, string property, long value)
        {
            var properties = new Dictionary<string, object> {{property, value}};
            _Add(account_id, distinct_id, "user_add", properties);
        }

        /*
          * 追加用户的集合类型的一个或多个属性
          * @param	account_id	账号ID
          * @param	distinct_id	匿名ID
          * @param	properties	增加的用户属性
         */
        public void UserAppend(string account_id, string distinct_id, Dictionary<string, object> properties)
        {
            _Add(account_id, distinct_id, "user_append", properties);
        }

        /*
          * 追加用户的集合类型的一个或多个属性(对于重复元素进行去重处理)
          * @param	account_id	账号ID
          * @param	distinct_id	匿名ID
          * @param	properties	增加的用户属性
         */
        public void UserUniqAppend(string account_id, string distinct_id, Dictionary<string, object> properties)
        {
            _Add(account_id, distinct_id, "user_uniq_append", properties);
        }

        /**
         * 用户删除,此操作不可逆
         * @param 	account_id	账号ID
         * @param	distinct_id	匿名ID
        */
        public void UserDelete(string account_id, string distinct_id)
        {
            _Add(account_id, distinct_id, "user_del", new Dictionary<string, object>());
        }

        /// 立即发送缓存中的所有日志
        public void Flush()
        {
            _consumer.Flush();
        }

        //关闭并退出 sdk 所有线程，停止前会清空所有本地数据
        public void Close()
        {
            _consumer.Close();
        }

        private static bool IsNumber(object value)
        {
            return (value is sbyte) || (value is short) || (value is int) || (value is long) || (value is byte)
                   || (value is ushort) || (value is uint) || (value is ulong) || (value is decimal) ||
                   (value is float) || (value is double);
        }

        private static bool IsDictionary(object obj) 
        {
            if (obj == null)
                return false;
            return (obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>));
        }

        private static void AssertProperties(string type, IDictionary<string, object> properties)
        {
            if (null == properties)
            {
                return;
            }

            foreach (var kvp in properties)
            {
                var key = kvp.Key;
                var value = kvp.Value;
                if (null == value)
                {
                    continue;
                }

                if (KeyPattern.IsMatch(key))
                {
                    if (!IsNumber(value) && !(value is string) && !(value is DateTime) && !(value is bool) &&
                        !(value is IList) && !IsDictionary(kvp.Value))
                    {
                        throw new ArgumentException(
                            "The supported data type including: Number, String, Date, Boolean,List. Invalid property: {key}");
                    }

                    if (type == "user_add" && !IsNumber(value))
                    {
                        throw new ArgumentException("Only Number is allowed for user_add. Invalid property:" + key);
                    }
                }
                else
                {
                    throw new ArgumentException("The " + type + "'" + key + "' is invalid.");
                }
            }
        }

        private void _Add(string account_id, string distinct_id, string type, IDictionary<string, object> properties)
        {
            _Add(account_id, distinct_id, type, null, null, properties);
        }

        private void _Add(string account_id, string distinct_id, string type, string event_name, string event_id,
            IDictionary<string, object> properties)
        {
            if (_consumer.IsStrict() && string.IsNullOrEmpty(account_id) && string.IsNullOrEmpty(distinct_id))
            {
                throw new SystemException("account_id or distinct_id must be provided. ");
            }

            var eventProperties = new Dictionary<string, object>(properties);
            if (type == "track" || type == "track_update" || type == "track_overwrite"  || type == "track_first")
            {
                if (_dynamicPublicProperties != null)
                {
                    foreach (var kvp in _dynamicPublicProperties.GetDynamicPublicProperties())
                    {
                        if (!eventProperties.ContainsKey(kvp.Key))
                        {
                            eventProperties.Add(kvp.Key, kvp.Value);
                        }
                    }
                }

                if (_pubicProperties != null)
                    lock (_pubicProperties)
                    {
                        foreach (var kvp in _pubicProperties)
                        {
                            if (!eventProperties.ContainsKey(kvp.Key))
                            {
                                eventProperties.Add(kvp.Key, kvp.Value);
                            }
                        }
                    }

                if (type == "track_first")
                {
                    type = "track";
                    if (event_id != null)
                    {
                        eventProperties.Add("#first_check_id", event_id);
                        event_id = null;
                    }
                }
            }

            var evt = new Dictionary<string, object>();
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

            if (event_id != null)
            {
                evt.Add("#event_id", event_id);
            }

            //#uuid 只支持UUID标准格式xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
            if (eventProperties.ContainsKey("#uuid"))
            {
                evt.Add("#uuid", eventProperties["#uuid"]);
                eventProperties.Remove("#uuid");
            }
            else if (_enableUuid)
            {
                evt.Add("#uuid", Guid.NewGuid().ToString("D"));
            }

            if (_consumer.IsStrict())
            {
                AssertProperties(type, eventProperties);
            }

            if (eventProperties.ContainsKey("#ip"))
            {
                evt.Add("#ip", eventProperties["#ip"]);
                eventProperties.Remove("#ip");
            }

            if (eventProperties.ContainsKey("#app_id"))
            {
                evt.Add("#app_id", eventProperties["#app_id"]);
                eventProperties.Remove("#app_id");
            }

            //#first_check_id
            if (eventProperties.ContainsKey("#first_check_id"))
            {
                evt.Add("#first_check_id", eventProperties["#first_check_id"]);
                eventProperties.Remove("#first_check_id");
            }

            if (eventProperties.ContainsKey("#time"))
            {
                evt.Add("#time", eventProperties["#time"]);
                eventProperties.Remove("#time");
            }
            else
            {
                evt.Add("#time", DateTime.Now);
            }

            evt.Add("#type", type);
            evt.Add("properties", eventProperties);
            _consumer.Send(evt);
        }
    }
    public class TALogger
    {
        public static bool Enable = false;

        public static void Log(string format, params object[] args)
        {
            if (Enable)
            {
                string prefix = string.Format("[TA][{0}]: ", (DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss"));
                if (args != null)
                {
                    Console.WriteLine(prefix + format, args);
                }
                else
                {
                    Console.WriteLine(prefix + format, null, null);
                }
            }
        }
    }
}