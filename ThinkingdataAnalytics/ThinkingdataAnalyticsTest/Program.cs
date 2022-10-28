using System;
using System.Collections.Generic;
using ThinkingData.Analytics;

namespace ThinkingdataAnalyticsTest
{
    class DynamicPublicProperties : IDynamicPublicProperties
    {
        // 动态公共属性接口
        public Dictionary<string, object> GetDynamicPublicProperties()
        {
            return new Dictionary<string, object>()
            {
                {"DynamicPublicProperty", DateTime.Now},
            };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //LoggerConsumer,结合logbus使用，推荐这个
            //默认按照天切分，无大小切分，适用于大多的情景
            //ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new LoggerConsumer("./test/log/"));

            //按小时切分，无大小切分
            //ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new LoggerConsumer("I:/log/logdata/", LoggerConsumer.RotateMode.HOURLY));

            //按小时切分,按大小切分，大小单位为MB,等同于new LoggerConsumer("I:/log/logdata/",5)
            //ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new LoggerConsumer("I:/log/logdata/", LoggerConsumer.RotateMode.DAILY,5));

            //BatchConsumer
            //适用于小数量的历史数据使用
            //ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new BatchConsumer("https://receiver-ta-demo.thinkingdata.cn", "cf8bb16389af47bd9752b503142a7de9"));

            //如果是内网传输，可以按以下方式初始化,默认是true
            //ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new BatchConsumer(你的接受端url,APPID,false));

            //AsyncBatchConsumer
            //批量异步上报数据
            ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new AsyncBatchConsumer("https://receiver-ta-demo.thinkingdata.cn", "cf8bb16389af47bd9752b503142a7de9"));
            TALogger.Enable = true;

            //DebugConsumer，一条一条发送，适合测试数据是否错误
            //ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new DebugConsumer(你的接受端url,APPID));
            //是否上传到TA库中
            //ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new DebugConsumer(你的接受端url,APPID,true));

            ta.SetDynamicPublicProperties(new DynamicPublicProperties());
            ta.SetPublicProperties(new Dictionary<string, object>
            {
                {"PublicProperty", DateTime.Now},
            });

            String distinctId = "AE75F92A-FB22-4150-86E7-385266F4E9";
            String accountId = "csharp"; //登录时传入accountId

            Dictionary<string, Object> dic1 = new Dictionary<string, object>();
            dic1.Add("#ip", "123.123.123.123"); //可自动解析出城市省份
            dic1.Add("#time", DateTime.Now); //用户事件发生时刻,不用上传，默认是当前时间
            dic1.Add("id", 44);
            //对象
            Dictionary<string, Object> subDic1 = new Dictionary<string, object>();
            subDic1.Add("subKey", "subValue");
            dic1.Add("subDic", subDic1);
            //对象组
            List<Object> subList1 = new List<Object>();
            subList1.Add(subDic1);
            dic1.Add("subList", subList1);

            ta.Track(accountId, distinctId, "testEventName", dic1); //未登录状态下传入distinctId


            //刷新数据，立即上报
            ta.Flush(); //如果不调用此函数，LoggerConsumer,默认8k刷新一次;BatchConsumer默认50条发送一次，BatchConsumer可设置批次上报条数


            Dictionary<string, Object> dic2 = new Dictionary<string, object>();
            dic2.Add("id", 618834);
            dic2.Add("create_date", Convert.ToDateTime("2019-7-8 20:23:22")); //传入datetime类型的
            dic2.Add("group_no", "T22514");
            dic2.Add("group_title", "【爆款拼装来袭】");
            dic2.Add("group_purchase_id", 438);
            dic2.Add("group_order_is_vip", 3);
            dic2.Add("service_id", 0);
            ta.Track(accountId, distinctId, "testEventName2", dic2); //用户登录后与之前未登录的distinctid,进行绑定，详情可见官网

            ta.TrackFirst(accountId, distinctId, "firstEventName", "firstEventId", dic2);
            ta.TrackUpdate(accountId, distinctId, "updateEventName", "updateEventId", dic2);
            ta.TrackOverwrite(accountId, distinctId, "overwriteEventName", "overwriteEventId", dic2);
            //刷新数据，立即上报
            ta.Flush();

            //传入用户属性
            Dictionary<string, Object> dic3 = new Dictionary<string, object>();
            dic3.Add("login_name", "皮1");
            dic3.Add("login_time", DateTime.Now);
            dic3.Add("age", 12);
            dic3.Add("nickname", "xiao");
            List<string> list1 = new List<string>();
            list1.Add("str1");
            list1.Add("str2");
            list1.Add("str3");
            dic3.Add("arrkey4", list1);
            ta.UserSet(accountId, distinctId, dic3);
            //刷新数据，立即上报
            ta.Flush();

            //传入用户属性，只能是数值型的，如果传用户属性，请用UserSet
            Dictionary<string, Object> dic4 = new Dictionary<string, object>();
            dic4.Add("TotalRevenue", 648);
            ta.UserAdd(accountId, distinctId, dic4);
            //刷新数据，立即上报
            ta.Flush();

            Dictionary<string, Object> dic5 = new Dictionary<string, object>();
            dic5.Add("login_name", "皮2");
            dic5.Add("#time", new DateTime(2019, 12, 10, 15, 12, 11, 444));
            dic5.Add("#ip", "192.168.1.1");
            //dic5.Add("#uuid",Guid.NewGuid().ToString("D")); 上传#uuid为标准格式(8-4-4-4-12)的string,服务端比较稳定，可不上传
            ta.UserSetOnce(accountId, distinctId, dic5);
            //刷新数据，立即上报
            ta.Flush();

            //删除这个用户的某个属性 必须是string类型的集合，例如：
            List<string> list2 = new List<string>();
            list2.Add("nickname");
            list2.Add("age");
            ta.UserUnSet(accountId, distinctId, list2);
            //刷新数据，立即上报
            ta.Flush();

            Dictionary<string, Object> dic7 = new Dictionary<string, object>();
            dic7.Add("double1", (double) 1);
            dic7.Add("string1", "string");
            dic7.Add("boolean1", true);
            dic7.Add("DateTime4", DateTime.Now);
            List<string> list5 = new List<string>();
            list5.Add("6.66");
            list5.Add("test");
            dic7.Add("arrkey4", list5);
            ta.Track(accountId, distinctId, "test", dic7);

            //user_append,追加集合属性
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            List<string> list6 = new List<string>();
            list6.Add("true");
            list6.Add("test");
            dictionary.Add("arrkey4", list6);
            ta.UserAppend(accountId, distinctId, dictionary);

            //user_uniq_append,追加集合属性(对于重复元素进行去重处理)
            ta.UserUniqAppend(accountId, distinctId, dictionary);
            //刷新数据，立即上报
            ta.Flush();

            //ta.UserSet(accountId, distinctId, dic7);
            //刷新数据，立即上报
            //ta.Flush();

            //删除用户
            //ta.UserDelete(accountId, distinctId);
            //刷新数据，立即上报
            //ta.Flush();

            ta.Close();
        }
    }
}