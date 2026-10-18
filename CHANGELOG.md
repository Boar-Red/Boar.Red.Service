# 更新日志 / Changelog — Boar.Red.Service

## 10.0.0 — 首个开源版本 / First Open-Source Release

Boar.Red 微服务框架核心库以 MIT 协议开源，目标框架 .NET 10。

The Boar.Red microservice framework core library is open-sourced under the MIT license, targeting .NET 10.

核心能力 / Highlights：

- 服务注册与发现（`ServiceDiscoverer`）/ service registration & discovery
- 基于 WebSocket 的 `DataPacket` 二进制通信协议，支持压缩与 AES 加密 / binary `DataPacket` protocol over WebSocket with compression and AES encryption
- 非侵入式接入：现有 ASP.NET Core 应用在启动阶段即可成为微服务节点 / non-intrusive integration: any ASP.NET Core app becomes a node at startup
- 统一请求管道：跨域控制、请求体限制、Token 校验、管理员会话 / unified request pipe: CORS, body size limit, token checker, administrator session
