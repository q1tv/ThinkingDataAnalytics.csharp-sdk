**v1.5.0** (2022/10/28)

- 支持日志开关
- 支持多项目appId

**v1.4.0** (2022/05/06)

- 新增支持批量异步上报事件
- 新增支持动态公共属性
- 新增首次事件接口
- 新增 UserUniqAppend 接口
- 新增支持复杂属性

**v1.3.1** (2021/04/22)

- 修复 LoggerConsumer 写文件时文件锁创建失败的问题

**v1.3.0** (2020/12/29)

- 新增 track_update 接口，支持可更新事件
- 新增 track_overwrite 接口，支持可重写事件
- 新增 #first_check_id 属性，支持首次发生事件
- 优化LoggerConsumer，增加指定生成文件前缀
- 优化LoggerConsumer，增加自动创建未存在目录

**v1.2.1** (2020/03/16)

- 修复数据未及时flush问题

**v1.2.0** (2020/02/13)

- 数据类型支持list类型
- 新增 user_append 接口，支持用户的数组类型的属性追加
- 新增 user_unset 接口，支持删除用户属性
- BatchConsumer 性能优化：支持配置压缩模式；移除 Base64 编码
- DebugConsumer 优化: 在服务端对数据进行更完备准确地校验

**v1.1.0** (2019/09/24)

- 新增 DebugConsumer, 便于调试接口 
- 优化 LoggerConsumer, 支持按小时切分日志文件
- 去掉 LoggerConsumer单个文件默认大小为1G的切分
- 优化代码，修复属性为null时报错的bug，提升稳定性

**v1.0.0** (2019/07/09)

- 上线LoggerConsumer，BatchConsumer的基本功能
