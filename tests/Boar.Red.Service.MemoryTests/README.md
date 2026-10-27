# 内存问题回归检查

运行：

```powershell
dotnet run --project tests/Boar.Red.Service.MemoryTests/Boar.Red.Service.MemoryTests.csproj -c Release -p:GeneratePackageOnBuild=false
```

检查控制器及其同步、异步依赖的调用作用域、异常和重定向清理、统计过期与关闭统计后的请求入队，以及 WebSocket 发送和关闭超时。

真实连接检查仅启动随机本机端口：持续接收控制帧的空闲客户端应保持连接，不处理 Ping 的客户端应被保活超时中断。检查不读取业务配置、不访问数据库、不启动服务发现，也不需要生产服务。
