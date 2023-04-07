using ThinkingData.Analytics;

namespace ThinkingdataAnalyticsTest
{
    class DynamicPublicProperties : IDynamicPublicProperties
    {
        public Dictionary<string, object> GetDynamicPublicProperties()
        {
            return new Dictionary<string, object>()
            {
                {"DynamicPublicProperty", DateTime.Now},
                {"name", "dynamic"},
            };
        }
    }

    class Program
    {
        static void Main()
        {
            //TALogger.Enable = true;

            ThinkingdataAnalytics ta = new(new LoggerConsumer("H:/log", LoggerConsumer.RotateMode.HOURLY));

            //ThinkingdataAnalytics ta = new(new BatchConsumer("serverUrl", "appId", true));

            //ThinkingdataAnalytics ta = new(new AsyncBatchConsumer("serverUrl", "appId", true));

            //ThinkingdataAnalytics ta = new(new DebugConsumer("serverUrl", "appId", true, deviceId: "123456789"));

            ta.SetDynamicPublicProperties(new DynamicPublicProperties());

            ta.SetPublicProperties(new Dictionary<string, object>
            {
                {"PublicProperty", DateTime.Now},
                {"name", "PublicProperty"},
            });

            String distinctId = "1";
            String accountId = "csharp";

            Dictionary<string, Object> dic1 = new()
            {
                { "#ip", "123.123.123.123" },
                { "name", "property" },

                // if not add, SDK would use current time.
                { "#time", DateTime.Now },
                { "id", 44 }
            };

            Dictionary<string, Object> subDic1 = new()
            {
                { "subKey", "subValue" }
            };
            dic1.Add("subDic", subDic1);

            // object group
            List<Object> subList1 = new()
            {
                subDic1
            };
            dic1.Add("subList", subList1);

            ta.Track(accountId, distinctId, "testEventName", dic1);


            Dictionary<string, Object> dic2 = new Dictionary<string, object>();
            dic2.Add("id", 618834);
            dic2.Add("create_date", Convert.ToDateTime("2019-7-8 20:23:22"));
            dic2.Add("group_no", "T22514");
            dic2.Add("group_title", "title");
            dic2.Add("group_purchase_id", 438);
            dic2.Add("group_order_is_vip", 3);
            dic2.Add("service_id", 0);
            ta.Track(accountId, distinctId, "testEventName2", dic2);

            ta.TrackFirst(accountId, distinctId, "firstEventName", "firstEventId", dic2);
            ta.TrackUpdate(accountId, distinctId, "updateEventName", "updateEventId", dic2);
            ta.TrackOverwrite(accountId, distinctId, "overwriteEventName", "overwriteEventId", dic2);


            Dictionary<string, Object> dic3 = new Dictionary<string, object>();
            dic3.Add("login_name", "1");
            dic3.Add("login_time", DateTime.Now);
            dic3.Add("age", 12);
            dic3.Add("nickname", "xiao");
            List<string> list1 = new List<string>();
            list1.Add("str1");
            list1.Add("str2");
            list1.Add("str3");
            dic3.Add("arrkey4", list1);
            ta.UserSet(accountId, distinctId, dic3);


            Dictionary<string, Object> dic4 = new Dictionary<string, object>();
            dic4.Add("TotalRevenue", 648);
            ta.UserAdd(accountId, distinctId, dic4);


            Dictionary<string, Object> dic5 = new Dictionary<string, object>();
            dic5.Add("login_name", "2");
            dic5.Add("#time", new DateTime(2019, 12, 10, 15, 12, 11, 444));
            dic5.Add("#ip", "192.168.1.1");
            ta.UserSetOnce(accountId, distinctId, dic5);


            List<string> list2 = new List<string>();
            list2.Add("nickname");
            list2.Add("age");
            ta.UserUnSet(accountId, distinctId, list2);


            Dictionary<string, Object> dic7 = new Dictionary<string, object>();
            dic7.Add("double1", (double)1);
            dic7.Add("string1", "string");
            dic7.Add("boolean1", true);
            dic7.Add("DateTime4", DateTime.Now);
            List<string> list5 = new List<string>();
            list5.Add("6.66");
            list5.Add("test");
            dic7.Add("arrkey4", list5);
            ta.Track(accountId, distinctId, "test", dic7);

            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            List<string> list6 = new List<string>();
            list6.Add("true");
            list6.Add("test");
            dictionary.Add("arrkey4", list6);
            ta.UserAppend(accountId, distinctId, dictionary);

            ta.UserUniqAppend(accountId, distinctId, dictionary);

            ta.UserSet(accountId, distinctId, dic7);

            ta.UserDelete(accountId, distinctId);


            ta.Flush();
            ta.Close();
        }
    }
}