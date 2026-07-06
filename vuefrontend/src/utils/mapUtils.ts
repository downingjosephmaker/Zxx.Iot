import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { ref, Ref } from "vue";
import { getWapianUrl } from "@/config";

/**
 * 通用地图工具库 - 从 home/utils/mapHook.ts 和 buildingMap/utils/hook.tsx 提取的共同功能
 */

// 地图状态类名映射
export function getStatusClass(status: number): string {
  switch (status) {
    case 0:
      return "normal";
    case 1:
      return "offline";
    case 2:
      return "alarm";
    default:
      return "normal";
  }
}

// 根据状态获取颜色
export function getColorByStatus(status: number): string {
  switch (status) {
    case 0:
      return "#42a5f5"; // 正常
    case 1:
      return "#9e9e9e"; // 离线
    case 2:
      return "#ef5350"; // 告警
    default:
      return "#42a5f5";
  }
}

// 根据设备类型ID映射到设备类型代码
export function getDeviceTypeCode(deviceType: number | string): string {
  // 设备类型ID到设备类型代码的映射
  const typeCodeMap = {
    11: "wsd", // 温湿度
    12: "sf6", // SF6
    13: "zhkt", // 智慧空调
    14: "sj", // 设备类型14
    15: "sb", // 水泵
    16: "mj", // 门禁
    17: "shoubao", // 手报
    18: "hj", // 环境
    19: "zhrg", // 智慧人感
    20: "yg", // 烟感
    27: "dxkzq", // 智能电气控制器
    28: "gffd", // 光伏发电
    30007: "gffd", // 光伏发电
    30008: "zhxny" // 智慧新能源
  };
  return typeCodeMap[deviceType] || "zhwg"; // 如果找不到映射，默认返回智能网关
}

// 根据设备类型代码获取对应的SVG图标
export function getSvgIcon(deviceTypeCode: string): string {
  // 从文件导入SVG内容
  const svgMap = {
    // 智能网关 - 更现代化的网络连接图标
    zhwg: `<svg width="32" height="32" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z"/></svg>`,
    // 智慧空调 - 更精致的空调图标
    zhkt: `<svg width="32" height="32" viewBox="0 0 24 24"><path fill="currentColor" d="M21 11H3V5c0-1.1.9-2 2-2h14c1.1 0 2 .9 2 2v6zm0 2v6c0 1.1-.9 2-2 2H5c-1.1 0-2-.9-2-2v-6h18zm-9 1.5c-1.93 0-3.5 1.57-3.5 3.5s1.57 3.5 3.5 3.5 3.5-1.57 3.5-3.5-1.57-3.5-3.5-3.5zm0 5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zM7 9h2V7H7v2zm4 0h2V7h-2v2zm4 0h2V7h-2v2z"/></svg>`,
    // 智能电表 - 更现代的电表图标
    zndb: `<svg width="32" height="32" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm-1-13h2v6h-2zm0 8h2v2h-2zm5-4.5c0 2.76-1.12 5.26-2.93 7.07l-1.42-1.42c1.45-1.45 2.35-3.45 2.35-5.65 0-1.13-.24-2.2-.66-3.18l1.5-.86C15.61 9.09 16 10.5 16 12zm-8.07 7.07c-1.81-1.81-2.93-4.31-2.93-7.07 0-1.5.39-2.91 1.07-4.14l1.5.86c-.42.98-.66 2.05-.66 3.18 0 2.2.9 4.2 2.35 5.65l-1.33 1.52z"/></svg>`,
    // 智慧中控 - 更精致的控制面板图标
    zhzk: `<svg width="32" height="32" viewBox="0 0 24 24"><path fill="currentColor" d="M4 5h16v2H4zm0 6h16v2H4zm0 6h16v2H4zm19-3l-1.41-1.41L18 17.17V7.83l3.59 3.59L23 10z"/><path fill="currentColor" d="M13.5 17a1.5 1.5 0 100-3 1.5 1.5 0 000 3zm-7-6a1.5 1.5 0 100-3 1.5 1.5 0 000 3zm3.5 6a1.5 1.5 0 100-3 1.5 1.5 0 000 3z" opacity=".6"/></svg>`,
    // 智慧灯开 - 更精美的灯泡图标
    zhdk: `<svg width="32" height="32" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2C8.13 2 5 5.13 5 9c0 2.38 1.19 4.47 3 5.74V17c0 .55.45 1 1 1h6c.55 0 1-.45 1-1v-2.26c1.81-1.27 3-3.36 3-5.74 0-3.87-3.13-7-7-7zm-1 14v-1h2v1h-2zm5.29-5.29l-1.79 1.79V14h-4.42v-1.5l-1.79-1.79C7.54 10.05 7 9.11 7 8c0-2.76 2.24-5 5-5s5 2.24 5 5c0 1.11-.54 2.05-1.29 2.71zM9 21c0 .55.45 1 1 1h4c.55 0 1-.45 1-1v-1H9v1z"/><path fill="currentColor" d="M12 6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z" opacity=".7"/></svg>`,
    // 智慧人感 - 更现代的人体感应图标
    zhrg: `<svg width="32" height="32" viewBox="0 0 24 24"><path fill="currentColor" d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/><path fill="currentColor" d="M19 15.41V10c0-2.66-1.62-4.94-3.92-5.93C15.64 3.4 16 2.73 16 2h-2c0 1.1-.9 2-2 2s-2-.9-2-2H8c0 .73.36 1.4.92 2.07C6.62 5.06 5 7.34 5 10v5.41l-1.3 1.3c-.39.39-.39 1.02 0 1.41.39.4 1.01.4 1.4.01L7 16.25V21c0 .55.45 1 1 1h8c.55 0 1-.45 1-1v-4.75l1.9 1.88c.39.39 1.01.39 1.4 0 .39-.39.39-1.02 0-1.41L19 15.41z" opacity=".3"/></svg>`,
    // 智能电气控制器 - 更精致的控制器图标
    dxkzq: `<svg width="32" height="32" viewBox="0 0 24 24"><path fill="currentColor" d="M7 5h10v2h-1v10c0 1.1-.9 2-2 2H7c-1.1 0-2-.9-2-2V7H4V5h3zm1 2v10h8V7H8zm2 2h4v2h-4V9zm0 3h4v2h-4v-2zm0 3h4v2h-4v-2z"/><path fill="currentColor" d="M20 9v6c0 1.1-.9 2-2 2h-2v-2h2V9h-2V7h2c1.1 0 2 .9 2 2z"/><path fill="currentColor" d="M3 17h2v-2H3v-2h2V9H3V7h2c1.1 0 2 .9 2 2v6c0 1.1-.9 2-2 2z"/></svg>`,
    // 光伏发电 - 更精美的太阳能电池板图标
    gffd: `<svg width="32" height="32" viewBox="0 0 24 24"><path fill="currentColor" d="M3 3h18v7H3V3zm2 2v3h14V5H5zm-2 9h18v7H3v-7zm2 2v3h14v-3H5zm7-14h2v3h-2V2zm0 16h2v3h-2v-3zM5 12h3v2H5v-2zm11 0h3v2h-3v-2z"/><path fill="currentColor" d="M6 6h4v1H6V6zm8 0h4v1h-4V6zm-8 10h4v1H6v-1zm8 0h4v1h-4v-1z" opacity=".7"/></svg>`,
    // 智慧新能源 - 更现代的能源管理图标
    zhxny: `<svg width="32" height="32" viewBox="0 0 24 24"><path fill="currentColor" d="M12 3L2 12h3v8h14v-8h3L12 3zm0 13h-2v-2h2v2zm0-4h-2V8h2v4z"/><path fill="currentColor" d="M12 7.5c-.83 0-1.5.67-1.5 1.5s.67 1.5 1.5 1.5 1.5-.67 1.5-1.5-.67-1.5-1.5-1.5zm5 8.5h-2v-2h2v2zm-6 0H9v-2h2v2zm-4 0H5v-2h2v2z" opacity=".6"/><path fill="currentColor" d="M7 14c-.55 0-1 .45-1 1s.45 1 1 1 1-.45 1-1-.45-1-1-1zm10 0c-.55 0-1 .45-1 1s.45 1 1 1 1-.45 1-1-.45-1-1-1z" opacity=".4"/></svg>`,
    // 温湿度传感器
    wsd: `<svg width="32" height="32" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2c-5.33 4.55-8 8.48-8 11.8 0 4.98 3.8 8.2 8 8.2s8-3.22 8-8.2c0-3.32-2.67-7.25-8-11.8zM7.83 14c.37 0 .67.26.74.62.41 2.22 2.28 2.98 3.64 2.87.43-.02.79.32.79.75 0 .4-.32.73-.72.75-2.13.13-4.62-1.09-5.19-4.12-.08-.45.28-.87.74-.87z"/></svg>`
    // 其他设备类型可以继续添加...
  };
  // 如果找到对应的SVG，则返回它，否则返回默认图标
  return svgMap[deviceTypeCode] || svgMap["zhwg"];
}

// 渲染参数HTML (通用)
export function renderParamHtml(
  device: any,
  showParams: boolean = true
): string {
  if (
    !device ||
    !device.DisplayParams ||
    device.DisplayParams.length === 0 ||
    !showParams
  ) {
    return "";
  }

  // 使用generateSelectedParamListHtml函数来确保样式一致
  // 这里我们不使用缩放，确保文本大小固定
  try {
    // 检查是否已导入generateSelectedParamListHtml函数
    if (
      typeof window !== "undefined" &&
      window["generateSelectedParamListHtml"]
    ) {
      return window["generateSelectedParamListHtml"](
        null,
        device.DisplayParams,
        false,
        1
      );
    } else {
      // 如果函数不可用，则使用简单的HTML生成
      let html = "";
      device.DisplayParams.forEach(param => {
        const paramName = param.ParamName || param.paramName || "未知参数";
        const paramValue = param.ParamValue || param.paramValue || "--";
        const paramUnit = param.ParamUnit || param.paramUnit || "";

        html += `<div class="selected-param" style="height: 24px; display: flex; justify-content: space-between; margin: 4px 0;">
                   <span class="param-name" style="color: #333; font-size: 11px;">${paramName}：</span>
                   <span class="param-value" style="font-weight: bold; color: #1296db; background-color: #ffffff; padding: 2px 6px; border-radius: 3px; border: 1px solid rgba(18, 150, 219, 0.3); font-size: 11px; min-width: 60px; text-align: center;">${paramValue}${paramUnit}</span>
                 </div>`;
      });

      return `<div class="params-grid" style="max-height: 120px; overflow-y: auto; padding: 6px; width: 200px;">${html}</div>`;
    }
  } catch (error) {
    console.error("Error rendering parameter HTML:", error);
    return "";
  }
}

// 添加弹出窗口的通用样式
export function addPopupStyles(): void {
  // 检查是否已存在样式
  if (document.getElementById("marker-popup-styles")) return;

  // 创建样式标签
  const style = document.createElement("style");
  style.id = "marker-popup-styles";
  style.textContent = `
    .marker-custom-popup .leaflet-popup-content-wrapper {
      background: rgba(10, 20, 40, 0.9);
      color: #fff;
      border-radius: 10px;
      box-shadow: 0 3px 14px rgba(0, 162, 255, 0.4);
      border: 1px solid rgba(0, 162, 255, 0.5);
      backdrop-filter: blur(10px);
    }
    .marker-custom-popup .leaflet-popup-tip {
      background: rgba(0, 162, 255, 0.8);
    }
    .marker-popup {
      padding: 5px;
    }
    .marker-popup-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      border-bottom: 1px solid rgba(0, 162, 255, 0.3);
      padding-bottom: 8px;
      margin-bottom: 8px;
    }
    .marker-popup-header h3 {
      margin: 0;
      font-size: 16px;
      color: #fff;
    }
    .marker-popup-content {
      margin-bottom: 10px;
    }
    .params-table {
      width: 100%;
      border-collapse: collapse;
    }
    .params-table tr:nth-child(odd) {
      background: rgba(255, 255, 255, 0.05);
    }
    .params-table td {
      padding: 4px 6px;
      font-size: 12px;
    }
    .params-table td:first-child {
      color: rgba(255, 255, 255, 0.7);
    }
    .params-table td:last-child {
      text-align: right;
      font-weight: bold;
    }
    .marker-popup-footer {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      border-top: 1px solid rgba(0, 162, 255, 0.3);
      padding-top: 8px;
    }
    .popup-btn {
      background: linear-gradient(135deg, #00a2ff, #0078d4);
      border: none;
      color: white;
      padding: 4px 10px;
      border-radius: 4px;
      cursor: pointer;
      font-size: 12px;
      transition: all 0.3s;
    }
    .popup-btn:hover {
      transform: translateY(-2px);
      box-shadow: 0 2px 8px rgba(0, 162, 255, 0.5);
    }
    .status-badge {
      padding: 2px 8px;
      border-radius: 10px;
      font-size: 11px;
      font-weight: bold;
    }
    .status-badge.normal {
      background: #00ff87;
      color: #003d20;
    }
    .status-badge.offline {
      background: #9e9e9e;
      color: #ffffff;
    }
    .status-badge.alarm {
      background: #ff5252;
      color: #ffffff;
      animation: blink 1s infinite alternate;
    }
  `;

  // 添加到文档头部
  document.head.appendChild(style);
}

// 创建和配置地图的基本坐标系统
export function createMapCRS(
  mapExtent: number[],
  mapMaxZoom: number,
  mapMaxResolution: number,
  tileExtent: number[]
): L.CRS {
  const mapMinResolution = Math.pow(2, mapMaxZoom) * mapMaxResolution;

  // 创建自定义坐标系统
  const crs = L.CRS.Simple;
  crs.transformation = new L.Transformation(
    1,
    -tileExtent[0],
    -1,
    tileExtent[3]
  );
  crs.scale = function (zoom) {
    return Math.pow(2, zoom) / mapMinResolution;
  };
  crs.zoom = function (scale) {
    return Math.log(scale * mapMinResolution) / Math.LN2;
  };

  return crs;
}

// 使地图居中显示所有标记点的通用方法
export function centerMapOnMarkers(
  map: L.Map,
  markerList: any[],
  maxZoom: number = 3
): void {
  if (!map || !markerList || markerList.length === 0) {
    console.log("没有标记点或地图未初始化，无法调整地图视图");
    return;
  }

  console.log(`尝试居中显示 ${markerList.length} 个标记点`);

  // 创建一个边界对象
  const bounds = L.latLngBounds();

  // 将所有标记点添加到边界中
  let validMarkers = 0;
  markerList.forEach(item => {
    if (
      item.position &&
      Array.isArray(item.position) &&
      item.position.length === 2
    ) {
      bounds.extend(L.latLng(item.position));
      validMarkers++;
    }
  });

  console.log(`找到 ${validMarkers} 个有效标记点位置`);

  // 如果边界有效，则使地图适应这些边界
  if (bounds.isValid()) {
    map.fitBounds(bounds, {
      padding: [50, 50], // 添加一些内边距
      maxZoom: maxZoom, // 限制最大缩放级别
      animate: true // 使用动画效果
    });
    console.log("地图已调整到合适的视图");
  } else {
    console.warn("无法调整地图视图，没有有效的边界");
  }
}

// 地图初始化辅助函数
export interface MapOptions {
  containerId: string;
  buildId: string | number;
  filePath?: string;
  mapExtent?: number[];
  centerOffset?: number[];
  minZoom?: number;
  maxZoom?: number;
}

// 创建地图图层
export function createMapLayer(
  filePath: string,
  mapMinZoom: number,
  mapMaxZoom: number,
  opacity: number = 1
): L.TileLayer {
  if (filePath) {
    return L.tileLayer(
      getWapianUrl() + filePath + "/{z}/{x}/{y}.png",
      {
        minZoom: mapMinZoom,
        maxZoom: mapMaxZoom,
        attribution: "",
        noWrap: true,
        tms: false,
        opacity: opacity
      }
    );
  } else {
    // 如果没有文件路径，使用空白图层
    return L.tileLayer("", {
      minZoom: mapMinZoom,
      maxZoom: mapMaxZoom
    });
  }
}

// 定义地图参数接口
export interface MapConfig {
  mapExtent: number[];
  mapMinZoom: number;
  mapMaxZoom: number;
  mapMaxResolution: number;
  tileExtent: number[];
}

// 获取默认地图配置
export function getDefaultMapConfig(): MapConfig {
  return {
    mapExtent: [0.0, -5032.0, 10000.0, 0.0],
    mapMinZoom: 0,
    mapMaxZoom: 6,
    mapMaxResolution: 0.25,
    tileExtent: [0.0, -5032.0, 10000.0, 0.0]
  };
}

// 移除地图上的所有标记
export function clearMarkers(markerGroup: any[]): void {
  if (markerGroup && markerGroup.length > 0) {
    markerGroup.forEach(marker => {
      if (marker && marker.remove) {
        marker.remove();
      }
    });
  }
}
