### 注意

- `vue-pure-admin` 从 `3.3.0` 版本之后（不包括 `3.3.0` 版本），大部分工具和 `hooks` 都集成到了 [@pureadmin/utils](https://pure-admin-utils.netlify.app/)
- [npm](https://www.npmjs.com/package/@pureadmin/utils)
- [文档代码地址](https://github.com/pure-admin/pure-admin-utils-docs)

# 通用地图工具库 (mapUtils.ts)

这个工具库提供了在应用中渲染和处理Leaflet地图的通用功能。它被设计用于在不同页面中共享地图相关的代码，如 `home/index.vue` 和 `information/buildingMap` 模块。

## 主要功能

### 状态和样式相关

- `getStatusClass(status: number)`: 根据设备状态码返回对应的CSS类名
- `getColorByStatus(status: number)`: 根据设备状态码返回对应的颜色代码
- `getSvgIcon(deviceTypeCode: string)`: 根据设备类型代码返回对应的SVG图标
- `addPopupStyles()`: 添加弹出窗口的通用样式到页面

### 设备类型和参数

- `getDeviceTypeCode(deviceType: number | string)`: 将设备类型ID映射到设备类型代码
- `renderParamHtml(device: any, showParams: boolean)`: 渲染设备参数的HTML

### 地图创建和配置

- `createMapCRS(mapExtent, mapMaxZoom, mapMaxResolution, tileExtent)`: 创建自定义地图坐标系统
- `createMapLayer(filePath, mapMinZoom, mapMaxZoom, opacity)`: 创建地图图层
- `getDefaultMapConfig()`: 获取默认地图配置

### 标记点操作

- `centerMapOnMarkers(map, markerList, maxZoom)`: 居中显示地图上的所有标记点
- `clearMarkers(markerGroup)`: 清除地图上的所有标记点

## 使用示例

```typescript
// 导入需要的函数
import {
  getStatusClass,
  getSvgIcon,
  renderParamHtml,
  createMapCRS,
  centerMapOnMarkers,
  getDefaultMapConfig
} from "@/utils/mapUtils";

// 初始化地图
async function initMap() {
  // 获取默认地图配置
  const mapConfig = getDefaultMapConfig();
  const { mapExtent, mapMinZoom, mapMaxZoom, mapMaxResolution, tileExtent } = mapConfig;

  // 创建自定义坐标系统
  const crs = createMapCRS(mapExtent, mapMaxZoom, mapMaxResolution, tileExtent);
  
  // ... 其他地图初始化代码
  
  // 居中显示标记点
  centerMapOnMarkers(map, markerList.value);
}

// 添加标记点
function addMarker(item, size) {
  // 获取设备状态和图标
  const status = getStatusClass(item.Status);
  const icon = getSvgIcon(item.DeviceTypeCode || getDeviceTypeCode(item.DeviceType));
  
  // 渲染参数HTML
  const paramHtml = renderParamHtml(item, showParams.value);
  
  // ... 其他标记点创建代码
}
```

## 在不同页面中使用

这个工具库被设计为可以在两个主要页面中使用：

1. **首页地图背景** (`src/views/home/utils/mapHook.ts`)
2. **建筑地图** (`src/views/information/buildingMap/utils/mapHook.ts`)

在这两个页面中，我们使用了共同的地图渲染和标记点管理功能，但可以根据具体需求进行自定义扩展。

## 扩展

如果需要添加新的设备类型或图标，只需在 `getSvgIcon` 和 `getDeviceTypeCode` 函数中添加相应的映射即可。
