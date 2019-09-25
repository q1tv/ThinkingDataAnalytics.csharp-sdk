using System;
using System.Collections.Generic;
using ThinkingData.Analytics;

namespace ThinkingdataAnalyticsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //LoggerConsumer,结合logbus使用，推荐这个
            ThinkingdataAnalytics
                ta = new ThinkingdataAnalytics(new LoggerConsumer("I:/log/logdata/")); //默认按照天切分，无大小切分，适用于大多的情景
            // ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new LoggerConsumer("I:/log/logdata/",LoggerConsumer.RotateMode.HOURLY));//按小时切分，无大小切分
            //ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new LoggerConsumer("I:/log/logdata/", LoggerConsumer.RotateMode.DAILY,5)); //按小时切分,按大小切分，大小单位为MB

            //BatchConsumer，适用于小数量的历史数据使用
            //ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new BatchConsumer(你的接受端url,APPID));

            //DebugConsumer，一条一条发送，适合测试数据是否错误
            //ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new DebugConsumer(你的接受端url,APPID));
            String distinctId = "AE75F92A-FB22-4150-86E7-385266F4E9";

            Dictionary<string, Object> dic1 = new Dictionary<string, object>();
            dic1.Add("#ip", "123.123.123.123"); //可自动解析出城市省份
            dic1.Add("#time", DateTime.Now); //用户事件发生时刻,不用上传，默认是当前时间
            dic1.Add("id", 44);

            ta.Track(null, distinctId, "testEventName", dic1); //未登录状态下传入distinctId


            //刷新数据，立即上报
            ta.Flush(); //如果不调用此函数，LoggerConsumer,默认8k刷新一次;BatchConsumer默认50条发送一次，BatchConsumer可设置批次上报条数

            String accountId = "1111"; //登录时传入accountId
            Dictionary<string, Object> dic2 = new Dictionary<string, object>();
            dic2.Add("customer_id", 618834); //传入事件属性，可根据业务需要
            dic2.Add("create_date", Convert.ToDateTime("2019-7-8 20:23:22")); //传入datetime类型的
            dic2.Add("group_order_no", "T22472514");
            dic2.Add("group_order_title", "【爆款拼装来袭】");
            dic2.Add("group_purchase_id", 438);
            dic2.Add("group_order_is_vip", 3);
            dic2.Add("service_id", 0);
            ta.Track(accountId, distinctId, "testEventName2", dic2); //用户登录后与之前未登录的distinctid,进行绑定，详情可见官网
            //刷新数据，立即上报
            ta.Flush();
            //传入用户属性
            Dictionary<string, Object> dic3 = new Dictionary<string, object>();
            dic3.Add("login_name", "皮1");
            dic3.Add("login_time", DateTime.Now);
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
            dic5.Add("#time", new DateTime(2009, 12, 10, 15, 12, 11, 444));
            dic5.Add("#ip", "192.168.1.1");
            ta.UserSetOnce(accountId, distinctId, dic5);
            //刷新数据，立即上报
            ta.Flush();


            Dictionary<string, Object> dic6 = new Dictionary<string, object>();
            dic6.Add("double1", (double) 1);
            dic6.Add("string1", "string");
            dic6.Add("boolean1", true);
            dic6.Add("DateTime4", DateTime.Now);
            ta.Track(accountId, distinctId, "test", dic6);
            //刷新数据，立即上报
            ta.Flush();
            ta.UserSet(accountId, distinctId, dic6);
            //刷新数据，立即上报
            ta.Flush();

            //删除用户
            ta.UserDelete(accountId, distinctId);
            //刷新数据，立即上报
            ta.Flush();
            //停服时关闭
            ta.Close();
        }
    }
}