# C# SDK使用指南

本指南将会为您介绍如何使用C# SDK接入您的项目。如果您想获得API接口的详细说明，可以查看[C# SDK API文档](/technical_document/API_document/csharp_api_document.md)。



**最新版本为：** 1.5.0

**更新时间为：** 2022-10-28

**[C# SDK下载地址](http://download.thinkingdata.cn/server/release/ta_csharp_sdk.zip)**


## 1. 集成并初始化SDK

### 1.1 集成SDK

a.C# SDK 运用于服务端 `.NET Framework` 的应用，下载[SDK文件](http://download.thinkingdata.cn/server/release/ta_csharp_sdk.zip)，解压后将dll文件引用即可。

b.SDK 源代码编译后从 Release 目录下取得 dll 文件集成

c.SDK 项目其作为模块添加进需要集成的项目中使用

在头部加入以下代码引入SDK：

```csharp
using ThinkingData.Analytics
```

### 1.2 初始化SDK

您可以通过两种方法获得SDK实例：

**(1)LoggerConsumer：** 批量实时写本地文件，并以天为分隔，需要与LogBus搭配使用进行数据上传

```csharp
//创建按天切分的 LogConsumer, 不设置单个日志上限
ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new LoggerConsumer(logDirectory));
//创建按小时切分的 LogConsumer, 不设置单个日志上限
//ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new LoggerConsumer(logDirectory,LoggerConsumer.RotateMode.HOURLY));
//创建按天切分的 LogConsumer，并设置单个日志文件大小
//ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new LoggerConsumer(logDirectory,LoggerConsumer.ROTATE_DAILY, 10 * 1024));
// 调用SDK...

// 上报数据
ta.Track(accountId, distinctId, "Payment", properties); 

// 调用SDK...
  
// 在关闭服务器前需要调用关闭接口
ta.Close();

```

传入的参数为写入本地的文件夹地址，您只需将LogBus的监听文件夹地址设置为此处的地址，即可使用LogBus进行数据的监听上传。


**(2)BatchConsumer：** 批量实时向服务器传输数据，不需要搭配传输工具，**<font color="red">不建议在生产环境中使用</font>**

```csharp
ThinkingdataAnalytics ta = new ThinkingdataAnalytics(new BatchConsumer(serverURL, appid));
```

`serverURL`为传输数据的URL，`appid`为您的项目的APP ID

如果您使用的是云服务，请输入以下URL:

http://receiver.ta.thinkingdata.cn/logagent

如果您使用的是私有化部署的版本，请输入以下URL:

http://<font color="red">数据采集地址</font>/logagent



## 2. 上报数据

在SDK初始化完成之后，您就可以调用`track`来上传事件，一般情况下，您可能需要上传十几到上百个不同的事件，如果您是第一次使用TA后台，我们推荐您先上传几个关键事件。

如果您对需要发送什么样的事件有疑惑，可以查看[快速使用指南](/getting_started/getting_started_menu.md)了解更多信息。


### 2.1 发送事件

您可以调用`track`来上传事件，建议您根据先前梳理的文档来设置事件的属性以及发送信息的条件，此处以用户付费作为范例：

```csharp
//设置未登录状态下的访客ID
string distinctId= "distinctId";

//设置登录后的账号ID
string accountId = "accountId";

Dictionary<string, object> properties= new Dictionary<string, object>();

// 设置用户的ip地址，TA系统会根据IP地址解析用户的地理位置信息，如果不设置的话，默认不上报
properties.Add("#ip", "123.123.123.123");

// 设置事件发生的时间，如果不设置的话，则默认使用为当前时间
properties.Add("#time", DateTime.Now); 

//设置事件属性
properties.Add("Product_Name", "商品A");
properties.Add("Price", 30);
properties.Add("OrderId", "订单号abc_123");
       
// 上传事件，包含账号ID与访客ID  
ta.Track(accountId, distinctId, "Payment", properties);             

// 您也可以只上传访客ID
// ta.Track(null, distinctId, "Payment", properties); 
// 或者只上传账号ID
// ta.Track(accountId, null, "Payment", properties); 
```
**注：** 为了保证访客ID与账号ID能够顺利进行绑定，如果您的游戏中会用到访客ID与账号ID，我们极力建议您同时上传这两个ID，<font color="red">否则将会出现账号无法匹配的情况，导致用户重复计算</font>，具体的ID绑定规则可参考[用户识别规则](/user_guide/user_identify.md)一章。


* 事件的名称只能以字母开头，可包含数字，字母和下划线“\_”，长度最大为50个字符，对字母大小写不敏感。
* 事件属性是`Dictionary<string, object>` 类型，其中每个元素代表一个属性；
* 事件属性`Key`为属性名称，为`string`类型，规定只能以字母开头，包含数字，字母和下划线“_”，长度最大为50个字符，对字母大小写不敏感；
* 属性值支持四种类型：字符串、数值类、`bool`、`DateTime`。


### 2.2 设置公共事件属性

对于一些需要出现在所有事件中的属性属性，您可以调用`SetPublicProperties`来设置公共事件属性，我们推荐您在发送事件前，先设置公共事件属性。

```csharp
// 设置公共事件属性
Dictionary<string, object> superProperties= new Dictionary<string, object>();

superProperties.Add("server_version", "1.2.3A");
superProperties.Add("server_name", "A1001");

ta.SetPublicProperties(superProperties);

// 设置事件属性
Dictionary<string, object> properties= new Dictionary<string, object>();
properties.Add("Product_Name", "商品A");
properties.Add("Price", 30);
properties.Add("OrderId", "订单号abc_123");

// 上传事件，事件中将会带有公共事件属性以及该事件本身的属性
ta.Track(accountId, distinctId, "Payment", properties); 

/*
相当于进行下列操作

Dictionary<string, object> properties= new Dictionary<string, object>();

properties.Add("server_version", "1.2.3A");
properties.Add("server_name", "A1001");
properties.Add("Product_Name", "商品A");
properties.Add("Price", 30);
properties.Add("OrderId", "订单号abc_123");

ta.track(distinct_id,account_id,"Payment",properties)
*/

```  


* 公共事件属性是`Dictionary<string, object>` 类型，其中每个元素代表一个属性；
* 公共事件属性`Key`为属性名称，为`string`类型，规定只能以字母开头，包含数字，字母和下划线“_”，长度最大为50个字符，对字母大小写不敏感；
* 公共事件属性值支持四种类型：字符串、数值类、`bool`、`DateTime`。



如果调用`SetPublicPropperties`设置先前已设置过的公共事件属性，则会覆盖之前的属性值。如果公共事件属性和`track`上传事件中的某个属性的Key重复，则该事件的属性会覆盖公共事件属性：


```csharp
// 设置公共事件属性
Dictionary<string, object> superProperties= new Dictionary<string, object>();
superProperties.Add("server_version", "1.2.3A");
superProperties.Add("server_name", "A1001");
ta.SetPublicProperties(superProperties);

// 再次设置公共事件属性，此时"server_name"的值变为"B9999"
superProperties.clear();
superProperties.Add("server_name","B9999");
ta.SetPublicProperties(superProperties);


// 设置事件属性
Dictionary<string, Object> properties= new Dictionary<string, object>();
properties.Add("server_version", "1.3.4");
properties.Add("Product_Name", "商品A");
properties.Add("Price", 30);
properties.Add("OrderId", "订单号abc_123");

// 上传事件，此时"server_version"的值为"1.3.4","server_name"的值为"B9999"
ta.Track(accountId, distinctId, "Payment", properties); 

```

如果您想要清空所有公共事件属性，可以调用`ClearPublicProperties`。

## 3. 用户属性

TA平台目前支持的用户属性设置接口为`UserSet`、`UserSetOnce`、`UserAdd`、`UserDelete`。

### 3.1 UserSet

对于一般的用户属性，您可以调用`UserSet`来进行设置，使用该接口上传的属性将会覆盖原有的属性值，如果之前不存在该用户属性，则会新建该用户属性，类型与传入属性的类型一致：

```csharp
// 上传用户属性，"user_name"的值为"ABC"

Dictionary<string, object> properties= new Dictionary<string, object>();
properties.Add("user_name","ABC");
ta.UserSet(accountId,distinctId, properties);
properties.clear();

//再次上传用户属性，该用户的"user_name"被覆盖为"XYZ"

properties.Add("user_name","XYZ");
ta.UserSet(accountId,distinctId, properties);
```

* `UserSet`设置的用户属性是一个`Dictionary<string, object>` 类型，其中每个元素代表一个属性；
* 用户属性`Key`为属性名称，为`string`类型，规定只能以字母开头，包含数字，字母和下划线“_”，长度最大为50个字符，对字母大小写不敏感；
* 属性值支持四种类型：字符串、数值类、`bool`、`DateTime`。


### 3.2 UserSetOnce

如果您要上传的用户属性只要设置一次，则可以调用`UserSetOnce`来进行设置，当该属性之前已经有值的时候，将会忽略这条信息：

```csharp
// 上传用户属性，"user_name"的值为"ABC"

Dictionary<string, object> properties= new Dictionary<string, object>();
properties.Add("user_name","ABC");
ta.UserSetOnce(accountId,distinctId, properties);
properties.clear();

//上传用户属性，该用户的"user_name"已设置因此忽略该属性设置
//该用户的"user_age"没有被设置，因此设置值为18

properties.Add("user_name","XYZ");
properties.Add("user_age",18);
ta.UserSetOnce(accountId,distinctId, properties);

```

`UserSetOnce`设置的用户属性类型及限制条件与`UserSet`一致。


### 3.3 UserAdd

当您要上传数值型的属性时，您可以调用`UserAdd`来对该属性进行累加操作，如果该属性还未被设置，则会赋值0后再进行计算，可传入负值，等同于相减操作。

```csharp

//上传用户属性，给该用户的"TotalRevenue"属性与"VipLevel"属性分别加上30和1
Dictionary<string, object> properties= new Dictionary<string, object>();

properties.Add("TotalRevenue",30);
properties.Add("VipLevel",1);
ta.UserAdd(accountId,distinctId, properties);

```

`UserAdd`设置的用户属性类型及限制条件与`UserSet`一致，但只对数值型的用户属性有效。


### 3.4 UserDelete

如果您要删除某个用户，可以调用`UserDelete`将这名用户删除，您将无法再查询该名用户的用户属性，但该用户产生的事件仍然可以被查询到

```csharp
ta.UserDelete(accountId, distinctId);
```

## 4. 其他操作

### 4.1 立即提交数据

```csharp
ta.Flush();
```

立即提交数据到相应的接收器

### 4.2 关闭sdk

```csharp
ta.Close();
```

关闭并退出sdk，**请在关闭服务器前调用本接口，以避免缓存内的数据丢失**


