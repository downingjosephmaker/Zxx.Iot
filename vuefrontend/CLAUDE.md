<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

<!-- OPENSPEC:END -->

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个基于 Vue 3 + TypeScript 的工业级 SCADA (Supervisory Control and Data Acquisition) 组态编辑器系统,提供强大的可视化工业组态界面设计能力,支持实时数据绑定、MQTT通信、智能SVG处理、路径绘制、高级动画等功能。

### 核心技术栈
- Vue 3.5+ (Composition API) + TypeScript 5.8+
- Element Plus 2.9+ UI 框架
- Vite 6 构建工具
- Pinia 3 状态管理
- MQTT 5.13+ 实时数据通信
- ECharts 5.x 图表库

## 常用开发命令

```bash
# 安装依赖(必须使用 pnpm)
pnpm install

# 启动开发服务器
pnpm dev

# 构建生产版本
pnpm build

# TypeScript 类型检查
pnpm typecheck

# 代码格式化和检查
pnpm lint
```

## SCADA 核心架构

### ⚠️ 重要文件大小约束

**强制要求**: scada 文件夹内所有文件大小不能超过 100KB

**拆分策略** (参考 index.vue 的处理方法):
- 当单个文件接近或超过 100KB 时,必须拆分成多个模块
- index.vue (92KB) 已拆分为:
  - `main/utils1.ts` (92KB) - 仿真、组件初始化
  - `main/utils2.ts` (60KB) - 样式、背景处理
  - `main/utils3.ts` (68KB) - 工具函数集
  - `main/utils4.ts` (48KB) - 扩展工具函数
  - `main/utils-button.ts` (16KB) - 按钮组件
  - `main/utils-fuxa-controls.ts` (44KB) - FUXA 控件
  - `main/index.scss` (29KB) - 样式文件

**当前最大文件** (都在限制内):
- SvgManager.ts: 92KB ✅
- index.vue: 92KB ✅
- utils1.ts: 92KB ✅
- PropertyPanel.vue: 88KB ✅

### 主控制器 (src/views/scada/index.vue)
- **代码行数**: 2867 行 (92KB)
- **功能**: 系统核心控制器,负责编辑器主界面布局、画布交互、组件管理、MQTT连接、项目保存加载
- **拆分策略**: 功能模块已拆分到 `main/` 目录下的 7 个工具文件

### 组件目录 (src/views/scada/components/)

#### 核心面板组件

1. **FuxaComponentPanel.vue** (718 行) - 工业组件库面板
   - 7大类组件: 常规、控制、图形、工业、动画、小部件、资源
   - 200+ 工业组件图标
   - 拖拽交互系统

2. **PropertyPanel.vue** (2985 行) - 属性编辑面板
   - 位置、尺寸、旋转、缩放控制
   - 文字、颜色、边框、透明度设置
   - 图表配置模块

3. **DataBindingPanel.vue** (1153 行) - 数据绑定面板
   - 支持5种数据源: 静态数据、REST API、MQTT、WebSocket、数据库
   - 数据集管理和转换脚本
   - 组件绑定配置

4. **DatasetPanel.vue** (1860 行) - 数据集配置面板
   - API接口配置
   - MQTT订阅模块
   - WebSocket连接
   - 数据库查询

5. **PropertyEditDialog.vue** (2099 行) - 高级属性编辑
   - 事件系统: 点击、双击、悬停、数值变化、定时器
   - 动作系统: 显示/隐藏、颜色变化、位置移动等
   - SVG图标样式配置

#### 图表和样式配置组件

6. **ChartPropertyDialog.vue** (2254 行) - 图表配置
   - 支持折线图、柱状图、饼图、雷达图、仪表盘
   - 坐标轴、图例、标题配置
   - 数据系列配置

7. **AppearanceStyleConfig.vue** (1308 行) - 外观样式配置
   - 颜色系统: 纯色、渐变、图案
   - 描边配置: 样式、虚线、线帽
   - 滤镜效果: 投影、模糊、亮度

8. **SvgIconStyleConfig.vue** (1425 行) - SVG样式配置
   - viewBox配置
   - 路径编辑
   - 变换控制

#### 多媒体和控制组件配置

9. **IframePropertyDialog.vue** (519 行) - 网页嵌入配置
   - 支持嵌入外部网页、内部页面
   - 沙箱安全配置 (allow-scripts, allow-same-origin)
   - 自动刷新间隔设置
   - URL参数配置

10. **VideoPropertyDialog.vue** (526 行) - 视频播放配置
    - 支持 MP4、WebM、Ogg 视频格式
    - 播放控制: 自动播放、循环、静音
    - 视频源 URL 配置
    - 海报图设置

11. **WebcamPropertyDialog.vue** (749 行) - 摄像头实时流配置
    - 支持5种流媒体协议: HLS、RTSP、RTMP、WebRTC、HTTP/HTTPS
    - PTZ云台控制配置
    - 自动重连机制设置
    - 快照功能配置
    - 视频质量和码率设置

12. **TablePropertyDialog.vue** (805 行) - 数据表格配置
    - 表格列配置 (标题、字段、宽度、对齐)
    - 分页设置 (每页行数、总数)
    - 数据绑定配置
    - 样式设置 (边框、斑马纹)

13. **SliderPropertyDialog.vue** (709 行) - 滑块控制配置
    - 数值范围设置 (min, max, step)
    - 标记点配置
    - 显示输入框选项
    - 颜色预设方案
    - 垂直/水平方向切换

#### 工具组件

14. **SvgIcon.vue** (122 行) - SVG图标包装器组件
    - SvgManager 的高级封装
    - 支持动态尺寸、颜色配置
    - 5种动画效果: rotate、pulse、blink、bounce、shake
    - 悬停效果(亮度、对比度、缩放)
    - 错误处理和占位符显示
    - 响应式属性监听(name、color、size)

### 核心服务模块 (src/views/scada/core/)

#### 1. SvgManager.ts ⭐ (2522 行) - SVG渲染引擎核心
这是整个系统最复杂和最核心的模块。

**智能SVG处理系统**:
- **simple**: 简单图标(单色、无动画) - 可应用主题色
- **complex**: 复杂SVG(多色、渐变) - 保持原有样式
- **interactive**: 交互式SVG(包含JavaScript) - 完全保持原样
- **transformed**: 变换SVG - 调整viewBox
- **inkscape**: Inkscape生成 - 清理元数据

**14种专业动画系统**:
- 基础动画: rotate、pulse、blink、bounce、shake、scale、moveX、moveY、fade
- 工业动画:
  - **liquidFill/liquidDrain**: 液体上涨/下降(0%→100%)
  - **pipeFlow**: 管道流动效果
  - **switchToggle**: 开关切换动画

**关键功能**:
```typescript
// 创建内联SVG元素
createInlineSvg(componentName: string, options: SvgRenderOptions): HTMLElement

// 更新液位(数据绑定驱动)
updateLiquidLevel(svgElement: SVGSVGElement, targetValue: number, duration: number): void

// 更新开关状态
updateSwitchState(svgElement: SVGSVGElement, isOn: boolean): void
```

#### 2. fuxaMqttService.ts (600+ 行) - MQTT通信服务
```typescript
// MQTT连接管理
connect(config: MqttConfig): Promise<void>

// 主题订阅
subscribe(topic: string, qos: 0 | 1 | 2): void

// 消息发布
publish(topic: string, message: string | Buffer, options): void

// 连接状态
status: Ref<'connected' | 'disconnected' | 'connecting'>
devices: Map<string, MqttDeviceData>
```

#### 3. FuxaDataBinding.ts (791 行) - 数据绑定引擎

这是一个全面的实时数据绑定系统,负责管理组件与数据源的绑定关系。

**支持的数据源类型**:
- **MQTT**: 实时设备数据订阅
- **API**: REST API 定时轮询
- **Static**: 静态数据值
- **Expression**: JavaScript 表达式计算

**核心功能**:
- **绑定管理**: 创建、更新、删除、启用/禁用数据绑定
- **数据转换**: 线性映射、自定义函数转换
- **数据验证**: 必填、范围、正则表达式验证
- **批量更新**: 60fps 批量更新机制,性能优化
- **缓存系统**: 数据缓存和状态管理
- **表达式引擎**: 支持安全的 JavaScript 表达式求值

**关键方法**:
```typescript
// MQTT 数据绑定
bindMqttData(componentId: string, propertyName: string, deviceId: string, tagId: string): string

// API 数据绑定
bindApiData(componentId: string, propertyName: string, apiEndpoint: string): string

// 静态数据绑定
bindStaticData(componentId: string, propertyName: string, staticValue: any): string

// 表达式数据绑定
bindExpressionData(componentId: string, propertyName: string, expression: string): string
```

**数据转换示例**:
```typescript
// 线性映射: 传感器值 0-100 映射到角度 0-360
transform: {
  type: 'linear',
  min: 0,
  max: 100,
  outputMin: 0,
  outputMax: 360
}

// 自定义函数
transform: {
  type: 'custom',
  customFunction: 'value * 1.8 + 32'  // 摄氏度转华氏度
}
```

#### 4. SvgTypeDetector.ts (408 行) - SVG类型智能检测
```typescript
// 检测SVG类型并生成建议
detectSvgType(svgContent: string, componentName?: string): SvgTypeInfo

// 返回信息包括:
// - type: 'simple' | 'complex' | 'interactive' | 'transformed' | 'inkscape'
// - hasScript, hasAnimation, hasGradients, hasTransforms
// - recommendations: 应用主题色、保持原样、清理属性等建议
```

#### 5. WebcamComponent.ts (578 行) - 摄像头流媒体管理系统

这是一个全面的实时视频流管理模块,支持多种工业监控场景。

**支持的流媒体协议**:
```typescript
export type StreamProtocol = 'rtsp' | 'rtmp' | 'hls' | 'webrtc' | 'http' | 'https'
```

**核心功能**:
- **多协议支持**:
  - HLS (HTTP Live Streaming) - 使用 hls.js
  - RTSP/RTMP - 通过转码服务
  - WebRTC - 低延迟实时流
  - HTTP/HTTPS - 标准视频流

- **自动重连机制**: 网络断开时自动尝试重新连接
- **快照功能**: 捕获当前帧并保存为图片
- **PTZ云台控制**: 支持摄像头的平移、倾斜、缩放控制
- **全屏支持**: 视频全屏显示
- **性能优化**:
  - 可配置缓冲区大小
  - 码率自适应
  - 低延迟模式

**关键方法**:
```typescript
// 连接视频流
connectStream(url: string, protocol: StreamProtocol, videoElement: HTMLVideoElement): Promise<void>

// 断开连接
disconnect(): void

// 捕获快照
captureSnapshot(): Promise<Blob>

// PTZ控制
ptzControl(command: 'up' | 'down' | 'left' | 'right' | 'zoomIn' | 'zoomOut'): void
```

**HLS 流连接示例**:
```typescript
const connectHLS = async (url: string, video: HTMLVideoElement): Promise<void> => {
  if ((window as any).Hls && Hls.isSupported()) {
    const hls = new Hls({
      maxBufferLength: config.bufferSize || 1,
      maxMaxBufferLength: 3,
      liveSyncDuration: 1,  // 低延迟配置
    })
    hls.loadSource(url)
    hls.attachMedia(video)
  }
}
```

#### 6. TextCardComponent.ts (474 行) - 文本卡片组件管理系统

这是一个专门用于展示多行文本内容的卡片组件。

**支持的功能**:
- **多行文本**: 标题 + 正文 (支持 `\n` 换行)
- **卡片样式**: 背景色、边框、圆角、阴影
- **文本样式**: 字体、颜色、大小、对齐、行高
- **布局控制**: 内边距、行间距、最大行数

**核心功能**:
- **智能换行**: 自动将文本按换行符分割为多行
- **样式独立**: 标题和正文可以独立配置字体样式
- **SVG 渲染**: 使用 `<text>` + `<tspan>` 实现多行显示
- **阴影效果**: 使用 SVG 滤镜实现卡片阴影

**关键方法**:
```typescript
// 创建文本卡片组件
createTextCardComponent(component: ScadaComponent, container: HTMLElement): HTMLElement

// 更新组件配置
updateTextCardComponent(componentId: string, properties: any): void

// 销毁组件
destroyTextCardComponent(componentId: string): void
```

**配置示例**:
```typescript
const textCardConfig: TextCardConfig = {
  title: '系统状态',
  content: '当前运行正常\n温度: 25°C\n湿度: 60%',
  maxLines: 10,
  backgroundColor: '#ffffff',
  borderColor: '#d1d5db',
  borderRadius: 4,
  shadow: {
    enabled: true,
    color: 'rgba(0,0,0,0.1)',
    offsetX: 2,
    offsetY: 2,
    blur: 4
  }
}
```

#### 7. 其他核心模块
- **DataBindingManager.ts** (730 行): 数据绑定管理器
- **PropertyEditorManager.ts** (872 行): 属性编辑器管理
- **ComponentManager.ts** (419 行): 组件管理系统
- **DrawingTools.ts** (1285 行): 绘图工具
- **PathTool.ts** (1467 行): 路径绘制工具
- **FuxaResizeHandles.ts** (788 行): 组件缩放手柄
- **ChartComponents.ts** (155 行): 图表组件定义
- **ImageComponent.ts** (625 行): 图片组件管理
- **TimePickerComponent.ts** (661 行): 时间选择组件
- **fuxa-icon-mapping.ts** (1853 行): 200+工业图标映射表
- **AnimationTypes.ts** (86 行): 动画类型定义

## SCADA 数据流向

### 组件创建流程
```
用户拖拽组件 (FuxaComponentPanel)
  ↓
index.vue 接收 drop 事件
  ↓
ComponentManager 创建组件实例
  ↓
SvgManager.createInlineSvg()
  ↓
SvgTypeDetector.detectSvgType() (智能检测)
  ↓
SvgManager.renderSvgContent() (渲染)
  ↓
画布显示组件
```

### 数据绑定流程
```
DataBindingPanel 配置绑定
  ↓
DataBindingManager 注册绑定
  ↓
fuxaMqttService 订阅主题
  ↓
MQTT 消息到达
  ↓
FuxaDataBinding 数据转换
  ↓
组件更新 (液位、开关状态等)
```

### MQTT实时通信流程
```
fuxaMqttService.connect() → MQTT Broker连接
  ↓
订阅主题 (sensor/+/data)
  ↓
消息到达 (topic: "sensor/temp/data", payload: {value: 25.5})
  ↓
DataBindingManager 查找绑定组件
  ↓
组件实时更新 (温度显示、仪表盘、图表)
```

## 代理配置

开发环境 API 代理 (vite.config.ts):
```typescript
proxy: {
  "/Api": {
    target: "http://192.168.0.76:13696/Api",
    changeOrigin: true,
    rewrite: path => path.replace(/^\/Api/, "/Api")
  }
}
```

## 测试流程

### 自动化测试步骤

使用 MCP chrome-devtools 工具进行自动化测试:

1. **启动开发服务器**
   ```bash
   pnpm dev
   ```

   **注意**:
   - 默认端口配置在 `.env.development` 中为 `VITE_PORT = 8848`
   - 如果端口被占用,Vite 会自动递增端口号 (8849, 8850, ...)
   - 启动后终端会显示实际使用的端口,例如: `Local: http://localhost:8849/`
   - 请根据终端输出的实际端口号进行访问

2. **自动登录测试**
   - 访问登录页面: `http://localhost:{实际端口}/#/login`
   - 测试账号:
     - 用户名: `superadmin`
     - 密码: `Admin@666`

3. **访问 SCADA 页面**
   - 登录成功后访问: `http://localhost:{实际端口}/#/scada`
   - 验证 SCADA 组态编辑器加载
   - 检查 MQTT 连接状态
   - 测试组件拖拽和属性编辑

### 使用 chrome-devtools MCP 工具

在使用 chrome-devtools MCP 工具之前,需要:
1. 先运行 `pnpm dev` 并确认实际运行端口
2. 根据终端输出的端口号构建正确的 URL

可以调用 chrome-devtools MCP 工具执行:
- 自动打开浏览器并导航到登录页 (使用实际端口)
- 自动填充用户名 `superadmin` 和密码 `Admin@666`
- 点击登录按钮
- 导航到 SCADA 页面
- 验证核心功能

## SCADA 代码示例

### 示例1: 创建带液体动画的储罐组件

```typescript
import { svgManager } from './core/SvgManager'

// 创建储罐组件
const tankElement = svgManager.createInlineSvg('tank', {
  width: 150,
  height: 200,
  fillColor: '#67c23a',
  animation: 'liquidFill',
  animationSpeed: 'normal',
  animationIterationCount: 'infinite'
})

// 更新液位(数据绑定)
const updateTankLevel = (level: number) => {
  const svgElement = tankContainer.value?.querySelector('svg')
  if (svgElement) {
    svgManager.updateLiquidLevel(svgElement, level, 1000)
  }
}
```

### 示例2: MQTT数据绑定

```typescript
import { fuxaMqttService } from './core/fuxaMqttService'
import { DataBindingManager } from './core/DataBindingManager'

// 连接MQTT
await fuxaMqttService.connect({
  broker: 'mqtt://localhost:1883',
  clientId: 'scada-editor-' + Date.now(),
  username: 'admin',
  password: 'password'
})

// 订阅主题
fuxaMqttService.subscribe('sensor/temperature', 0)

// 绑定组件
bindingManager.bindComponent('temp-gauge', {
  id: 'binding-1',
  sourceType: 'mqtt',
  source: 'sensor/temperature',
  target: { componentId: 'temp-gauge', property: 'value' },
  transform: (data) => parseFloat(data.value),
  updateMode: 'onChange'
})
```

### 示例3: 配置实时摄像头监控

```typescript
import { WebcamComponent } from './core/WebcamComponent'

// 创建摄像头组件实例
const webcamComponent = new WebcamComponent({
  url: 'rtsp://192.168.1.100:554/stream',
  protocol: 'rtsp',
  autoReconnect: true,
  reconnectInterval: 5000,
  bufferSize: 1,
  quality: 'high'
})

// 连接视频流
const videoElement = document.querySelector('video')
await webcamComponent.connectStream(
  'http://192.168.1.100:8080/stream.m3u8',
  'hls',
  videoElement
)

// 捕获快照
const snapshot = await webcamComponent.captureSnapshot()
const link = document.createElement('a')
link.href = URL.createObjectURL(snapshot)
link.download = `snapshot-${Date.now()}.jpg`
link.click()

// PTZ云台控制
webcamComponent.ptzControl('up')
webcamComponent.ptzControl('zoomIn')
```

### 示例4: 注册自定义组件

```typescript
// 在 fuxa-icon-mapping.ts 中注册
export const componentIconMapping: ComponentIconMapping[] = [
  {
    name: 'custom-valve',
    title: '自定义阀门',
    category: 'industrial',
    iconPath: '@/assets/svg/industrial/valve.svg',
    defaultProps: {
      width: 100,
      height: 100,
      fillColor: '#409eff'
    }
  }
]

// 注册属性编辑器
propertyEditorManager.registerEditor({
  type: 'custom-valve',
  title: '阀门属性',
  category: 'industrial',
  fields: [
    {
      name: 'status',
      label: '阀门状态',
      type: 'select',
      defaultValue: 'closed',
      options: [
        { label: '开启', value: 'open' },
        { label: '关闭', value: 'closed' }
      ]
    }
  ]
})
```

## 常见问题

### Q: 如何处理超过 100KB 的文件?
- **UI组件总数**: 15个主要组件 (核心面板5个 + 图表样式3个 + 多媒体控制5个 + 工具组件2个)
- **核心服务模块**: 17个核心服务模块 (SvgManager、fuxaMqttService、FuxaDataBinding、SvgTypeDetector、WebcamComponent、TextCardComponent + 其他11个)
2. **拆分策略**:
   - 将相关功能提取到独立的工具文件中
- **常规分类组件**: 26个 (新增文本卡片组件)
   - 按功能领域分类 (如 utils-button.ts、utils-fuxa-controls.ts)
   - 在主文件中导入并使用这些工具函数
3. **命名规范**:
   - 工具文件放在 `main/` 子目录下
   - 使用 `utils-<功能名>.ts` 命名格式
   - 导出的函数使用清晰的命名
4. **示例**: index.vue (2867行/92KB) 拆分为 7 个工具文件

### Q: SVG组件颜色无法修改
**解决**: SVG可能被检测为complex类型,使用 `forceOverride: true` 强制覆盖

### Q: 液体动画不生效
**解决**: 检查SVG文件是否包含 `putValue` 函数,或使用降级方案

### Q: MQTT连接失败
**解决**: 检查 Broker 地址、认证信息,监听 `fuxaMqttService.status.value` 状态

### Q: HLS视频流无法播放
**解决**:
- 确认视频流 URL 可访问 (使用 VLC 等工具测试)
- 检查 hls.js 库是否正确加载
- 浏览器是否支持 HLS (Safari 原生支持,其他需 hls.js)
- 查看控制台是否有 CORS 跨域错误

### Q: RTSP摄像头连接失败
**解决**:
- RTSP 不能直接在浏览器中播放,需要转码服务
- 使用 FFmpeg 或其他转码服务将 RTSP 转为 HLS/WebRTC
- 或使用 WebRTC 协议实现低延迟播放

### Q: 摄像头快照功能不工作
**解决**:
- 确认视频流已成功加载 (video.readyState >= 2)
- 检查 canvas API 是否可用
- 对于跨域视频,需要设置 CORS 头 `crossOrigin="anonymous"`

## 系统规模

- **主控制器**: index.vue (2867 行)
- **UI组件总数**: 14个主要组件 (核心面板5个 + 图表样式3个 + 多媒体控制5个 + 工具组件1个)
- **核心服务模块**: 16个核心服务模块 (SvgManager、fuxaMqttService、FuxaDataBinding、SvgTypeDetector、WebcamComponent + 其他11个)
- **工业组件库**: 200+ 组件, 7大分类
- **动画类型**: 14种专业动画效果
- **多媒体支持**: 视频播放、摄像头监控、网页嵌入
- **流媒体协议**: 支持 HLS、RTSP、RTMP、WebRTC、HTTP/HTTPS
- **代码总量**: 20000+ 行 TypeScript/Vue 代码

## 适用场景

- 工业监控系统 (SCADA)
- 智能制造 MES 系统
- 能源管理系统
- 楼宇自控系统
- 智慧城市监控平台
- IoT 可视化平台
