# Assets/Script 详细结构文档

本文档用于帮助你快速重新熟悉 `Assets/Script` 的代码组织、模块职责、脚本协作关系与运行流程。

## 1) 目录分层

`Assets/Script` 按职责分为三层：

- `Core`：AR 跟踪、标定、可视化、姿态/准星等核心交互逻辑
- `Services`：数据状态管理、WebSocket 通信、输入数据容器
- `UI`：按钮/Toggle/Slider 等界面交互入口

---

## 2) 当前目录树（脚本文件）

```text
Assets/Script
├── Core
│   ├── ARImageTrackingDebugger.cs
│   ├── ARMarkerTracking.cs
│   ├── ARRaycast.cs
│   ├── CameraLocation.cs
│   ├── EnhancedMarkerVisualizer.cs
│   ├── GyroReticleController.cs
│   ├── MarkerVisualizer.cs
│   └── TrackingModeSelector.cs
├── Services
│   ├── DataManager.cs
│   ├── InputTextManager.cs
│   └── WebSocketClient.cs
└── UI
    ├── ButtonHandler.cs
    ├── MarkerTrackingController.cs
    ├── SliderHandle.cs
    ├── TemplateHander2.cs
    ├── TemplateHandle.cs
    └── ToggleManager.cs
```

---

## 3) 模块与依赖关系（高层）

核心数据流大致是：

1. `Core` 从 AR/传感器获取位姿与标定结果  
2. 写入 `Services/DataManager`（作为共享状态中心）  
3. `Services/WebSocketClient` 从 `DataManager` 取值并发送到服务端  
4. `UI` 脚本通过按钮/开关/滑条改变状态并触发 Core/Service 行为  

典型耦合关系：

- `ARMarkerTracking` -> `DataManager`
- `ARRaycast` -> `DataManager`
- `ToggleManager` -> `DataManager`
- `SliderHandle` -> `WebSocketClient` + `DataManager`
- `ButtonHandler` -> `ARMarkerTracking` + `ARTrackedImageManager` + `WebSocketClient` + `DataManager`
- `WebSocketClient` -> `DataManager`

---

## 4) Core 详细说明

### `ARMarkerTracking.cs`

**职责**
- 使用 `ARTrackedImageManager` 识别目标 Marker（默认名 `DesktopMarker`）
- 建立基准（位置/旋转/距离）并做延迟自动校准
- 持续更新与 Marker 的相对交互数据并写入 `DataManager`

**关键流程**
- `OnEnable/OnDisable`：订阅/取消订阅 `trackedImagesChanged`
- `HandleTrackedImage`：首次识别目标 Marker，记录状态并可实例化可视化对象
- `PerformCalibration`：计算原点位姿并写入 `DataManager`
- `UpdateInteractionData`：每帧更新距离等交互数据（并可输出调试日志）

**对外状态**
- `IsTracking`、`IsCalibrated`、`CurrentMarker`、`CurrentDistance`
- `RecalibrateManually()` 支持手动重标定

---

### `ARImageTrackingDebugger.cs`

**职责**
- 图像追踪调试面板：显示 AR session、reference library、当前追踪图像数量与状态
- 日志打印图像新增/更新/移除事件，辅助排查“识别不到图像”

**适用场景**
- 排查 Reference Library 未配置
- 排查图像识别质量（光线、距离、图像清晰度）

---

### `ARRaycast.cs`（继承 `GyroReticleController`）

**职责**
- 旧版“4 点标定”路径：用屏幕准星进行 AR Raycast，采集点位并计算 2D 映射
- 将原点位置、姿态、距离、lookPoint 写入 `DataManager`

**关键点**
- `initSet()`：执行一次采点，累计到 4 次后计算 `lookPoint2D`
- `CalculateLookPoint2D()`：将 3D 位置投影到屏幕坐标系比例坐标

---

### `GyroReticleController.cs`

**职责**
- 统一处理“基于设备加速度移动 UI 准星”
- 为派生类（如 `ARRaycast`）提供 `UpdateReticle()` 与 `ScreenCenter`

---

### `TrackingModeSelector.cs`

**职责**
- 在 `ArUco（Marker）` 与 `Raycast4Point（四点标定）` 两套模式间切换
- 同步启停相关脚本组件和 GameObject

**特点**
- `Start()` 自动应用配置模式
- `SetMode()` 支持运行时切换
- `OnValidate()` 在编辑态保持 Inspector 状态一致

---

### `MarkerVisualizer.cs`

**职责**
- 按 `ARMarkerTracking` 状态显示基础调试文本（检测中/已校准/距离）

**备注**
- 文件中 `Update` 方法附近有异常字符（疑似编码或误编辑痕迹），建议后续单独清理，以免潜在编译风险。

---

### `EnhancedMarkerVisualizer.cs`

**职责**
- 在检测到 Marker 后动态创建更显眼的 3D 可视化（中心立方体 + 轨道立方体）
- 实时更新脉动、旋转与轨道动画

---

### `CameraLocation.cs`

**职责**
- 与 `GyroReticleController` 类似的准星移动逻辑（直接版本）
- 根据 `Input.acceleration` 更新 `RectTransform` 位置并输出日志

**备注**
- 功能与 `GyroReticleController` 有重叠，可能属于早期/实验脚本。

---

## 5) Services 详细说明

### `DataManager.cs`

**职责**
- 作为全局交互状态中心，保存用户操作状态和 AR 相机相关数据
- 为 UI、Core、WebSocket 提供统一读写 API

**核心内容**
- `UserState`：`Origin / Zoom / Rotate / LookPoint / Partial / texture1 / texture2`
- 持有 `XROrigin`，每帧更新真实相机位置/旋转和前进位移
- 保存基准位姿、校准状态、注释、lookPoint、缩放、相对旋转等

**典型被调用方**
- 被 `ARMarkerTracking` / `ARRaycast` 写入
- 被 `WebSocketClient` / `UI` 读取与更新

---

### `WebSocketClient.cs`

**职责**
- 建立并维护 WebSocket 连接（`NativeWebSocket`）
- 按 `DataManager.UserState` 周期性打包 JSON 并发送交互数据

**关键流程**
- `Start()`：创建连接、注册事件、启动发送循环、执行 `Connect()`
- `Update()`：分发消息队列；侦测用户状态变化并重启发送定时器
- `SendingMessage()`：根据状态构造消息并发送
- `createJson(...)`：统一消息格式组装

**当前默认地址**
- `_defaultUrl = ws://172.16.172.184:8888`

---

### `InputTextManager.cs`

**职责**
- 目前仅持有 `InputField annotation` 引用，逻辑为空

**备注**
- 注释文本实际主要在 `ButtonHandler` 中写入 `DataManager`。

---

## 6) UI 详细说明

### `ButtonHandler.cs`

**职责**
- 主入口按钮点击后的流程协调
- 根据 `useMarkerTracking` 选择 Marker 追踪路径（默认）或旧四点标定路径（备用注释代码）

**Marker 路径下行为**
- 启用 `ARTrackedImageManager` 与 `ARMarkerTracking`
- 打开 Toggle 交互
- 读取输入框注释并写入 `DataManager`
- `WaitForMarkerCalibration()` 等待校准成功后：
  - 初始化播放组件（`SrsPlayer.Init()`）
  - 发送 origin 初始化消息

---

### `MarkerTrackingController.cs`

**职责**
- 提供独立的“开始/停止追踪”按钮控制器
- 管理 `ARTrackedImageManager` 与 `ARMarkerTracking` 启停
- 同步更新按钮可用状态与状态文本显示

---

### `ToggleManager.cs`

**职责**
- 将 Toggle 名称映射为 `DataManager.UserState`
- 处理 Toggle 切换后的状态更新与 Slider 参数恢复

**关键点**
- `Toggle_Origin / Toggle_Zoom / Toggle_Rotate / Toggle_LookPoint / Toggle_Partial`
- 启动时默认将所有 Toggle 设为不可交互，等待主流程解锁

---

### `SliderHandle.cs`

**职责**
- 监听 Slider 值变化并发送 JSON
- 按当前 `UserState` 在 `PlayerPrefs` 里保存/加载对应参数（Zoom 与 LookPoint 分离）

---

### `TemplateHandle.cs`

**职责**
- 模板按钮点击后发送模板态初始化消息（`getTemplate()` / `getTemplateState()`）
- 会启用 Toggle 交互，并通过 WebSocket 发出模板数据

---

### `TemplateHander2.cs`

**职责**
- 空脚本占位（目前无实际逻辑）

---

## 7) 典型运行流程（默认 Marker 模式）

1. 场景启动后，`ButtonHandler` 初始关闭 Marker 检测组件  
2. 用户点击主按钮 -> 启用 `ARTrackedImageManager + ARMarkerTracking`  
3. `ARMarkerTracking` 识别目标 Marker，等待短延迟后自动校准  
4. 校准结果写入 `DataManager`（origin、quaternion、distance、correctState）  
5. `ButtonHandler` 协程检测到 `IsCalibrated` 后发送 origin 初始消息  
6. `WebSocketClient` 按当前 `UserState` 持续发送交互消息  
7. 用户通过 `ToggleManager` / `SliderHandle` 切换状态并调参，消息持续同步

---

## 8) 场景挂载与检查清单（建议）

最小可运行链路建议确保以下引用已绑定：

- `DataManager`
  - `xrOrigin` 已绑定
- `WebSocketClient`
  - `_dataManager` 已正确赋值（当前脚本依赖该引用）
- `ButtonHandler`
  - `dataManagerInstance` / `toggleManagerInstance` / `webSocketClientInstance`
  - `trackedImageManager` / `markerTrackingInstance`
  - `srsPlayerInstance`（若流程依赖视频初始化）
- `ARMarkerTracking`
  - `_trackedImageManager` / `_arCamera` / `_dataManager`
- `ToggleManager`
  - `DataManagerInstance` / `SliderHandleInstance` / `ButtonHandlerInstance`

---

## 9) 现状与后续可优化点

- `MarkerVisualizer.cs` 有可疑字符，建议尽快清理一次编码/文本污染。
- `CameraLocation` 与 `GyroReticleController` 功能重叠，可考虑保留一套。
- `InputTextManager`、`TemplateHander2` 目前为空壳，后续可补齐或移除。
- `WebSocketClient` 中 `_sendIntervals` 已定义但发送间隔逻辑主要由 `interval(...)` 决定，可统一策略。
- 推荐补一份“场景对象 -> 脚本 -> Inspector 引用”清单（可按具体场景文件生成）。

