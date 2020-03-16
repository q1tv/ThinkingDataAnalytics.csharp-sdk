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
