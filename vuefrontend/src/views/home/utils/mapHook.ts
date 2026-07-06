import L from "leaflet";
import { onMounted, onUnmounted, ref, nextTick } from "vue";
import "leaflet/dist/leaflet.css";
import { GetInfoByPk } from "@/api/information/buildingMap";
import { getWapianUrl } from "@/config";
// 导入通用地图工具
import {
  getStatusClass,
  getDeviceTypeCode,
  getSvgIcon,
  renderParamHtml,
  addPopupStyles,
  createMapCRS,
  createMapLayer,
  getDefaultMapConfig
} from "@/utils/mapUtils";

// 确保颜色与buildingMap完全一致：
// 正常状态: #42a5f5
// 离线状态: #9e9e9e
// 告警状态: #ef5350

const fixedMapCenter = ref(null);

export function useMapBackground() {
  let map = null;
  const mapInitialized = ref(false);
  const mapContainerId = ref("homeMapBackground");
  const buildId = ref("2"); // 固定建筑ID为2，使用字符串类型
  const markerList = ref([]);
  const markerGroup = ref([]);
  const featureGroup = L.featureGroup();
  let filePath = "";
  // 添加弹出窗口引用
  const activeMarkerId = ref(null);
  // 添加参数显示控制
  const showParams = ref(true);

  // 初始化地图
  async function initMap() {
    console.log("开始初始化地图...");

    // 清理旧地图
    if (map) {
      console.log("清理旧地图实例");
      if (markerGroup.value.length > 0) {
        markerGroup.value.forEach(item => {
          if (item && item.remove) {
            item.remove();
          }
        });
        markerGroup.value = [];
      }
      map.remove();
      map = null;
    }

    // 获取地图数据
    await getMapData();
    console.log(`获取到 ${markerList.value.length} 个标记点数据`);

    // 获取默认地图配置
    const mapConfig = getDefaultMapConfig();
    const { mapExtent, mapMinZoom, mapMaxZoom, mapMaxResolution, tileExtent } =
      mapConfig;

    // 创建自定义坐标系统
    const crs = createMapCRS(
      mapExtent,
      mapMaxZoom,
      mapMaxResolution,
      tileExtent
    );
    try {
      console.log("开始创建地图实例");
      console.log(
        "地图文件路径:",
        filePath ? getWapianUrl() + filePath : "未设置"
      );

      // 使用从API获取的地图图层
      const layer = createMapLayer(filePath, mapMinZoom, mapMaxZoom);

      // 计算地图中心点
      const centerX = (mapExtent[0] + mapExtent[2]) / 2;
      const centerY = (mapExtent[1] + mapExtent[3]) / 2;
      const centerPoint = [centerY, centerX]; // 移除偏移，使地图居中
      console.log("地图中心点:", centerPoint);

      // 创建地图实例
      const mapContainerElement = document.getElementById(mapContainerId.value);
      if (!mapContainerElement) {
        console.error(`找不到地图容器元素: ${mapContainerId.value}`);
        return;
      }

      // 确保地图容器可见且样式正确
      mapContainerElement.style.height = "100%";
      mapContainerElement.style.width = "100%";
      mapContainerElement.style.position = "fixed";
      mapContainerElement.style.top = "0";
      mapContainerElement.style.left = "0";
      mapContainerElement.style.zIndex = "0";
      mapContainerElement.style.pointerEvents = "auto";
      mapContainerElement.style.touchAction = "none";

      map = L.map(mapContainerId.value, {
        maxZoom: mapMaxZoom,
        minZoom: mapMinZoom,
        center: centerPoint,
        crs: crs,
        layers: [layer],
        attributionControl: false,
        zoomControl: false,
        scrollWheelZoom: false,
        doubleClickZoom: false,
        touchZoom: false,
        boxZoom: false,
        keyboard: false
      });

      // 地图点击事件已禁用，保留日志记录功能
      map.on("click", function (e) {
        console.log("地图点击位置:", e.latlng);
        console.log("像素坐标:", e.containerPoint);
        console.log("地图坐标:", e.layerPoint);
        // 点击不再触发地图移动或缩放
      });

      map.fitBounds([
        crs.unproject(L.point(mapExtent[2], mapExtent[3])),
        crs.unproject(L.point(mapExtent[0], mapExtent[1]))
      ]);

      // 设置默认视图为地图中心，缩放级别为适中的3级
      // map.setView([2701, 520], 4, { animate: true });
      // 添加延迟以确保所有标记点都已加载，然后居中显示
      // setTimeout(() => {
      //   map.setView([2701, 520], 4, { animate: true });
      // }, 800);
      console.log("地图实例创建成功");
      // 缩放功能已禁用，但设置初始标记点大小
      const initialZoom = map.getZoom();
      updateMarkerSize(initialZoom);

      // 添加标记点
      console.log("开始添加标记点");
      updateMarker();
      featureGroup.addTo(map);

      mapInitialized.value = true;
      // 添加自定义样式
      addCustomStyles();
      // 直接设置地图初始视图，无需动画
      map.setView(fixedMapCenter.value, 4);
    } catch (error) {
      console.error("地图初始化失败:", error);
      // 如果加载失败，使用备用方案
      createBackupMap(mapExtent, crs);
    }
  }

  // 更新标记点大小
  function updateMarkerSize(zoom) {
    // 地图的缩放层级范围是0-6
    const _maxZoom = 6;
    const _minZoom = 0;
    // 计算缩放比例：反转逻辑，地图缩放级别为0时图标最小，为6时图标最大
    // 设置缩放范围：最小0.6，最大1.5（整体缩小）
    // 直接使用线性映射：zoom=0对应scale=0.6，zoom=6对应scale=1.5
    const scale = 0.6 + (zoom / 6) * 0.9;
    // 确保比例在合理范围内
    const finalScale = Math.max(0.6, Math.min(1.5, scale));
    markerGroup.value.forEach(marker => {
      if (marker && marker.setIcon) {
        const item = markerList.value.find(i => i.DeviceId === marker.DeviceId);
        if (item) {
          const status = getStatusClass(item.Status);
          const blinkClass = item.Status === 2 ? "blink" : "";
          // 设备名称使用固定大小
          const deviceNameFontSize = 10;
          const deviceNameWidth = Math.max(90, item.DeviceName.length * 10);
          // 渲染参数HTML
          const paramHtml = renderParamHtml(item, true);
          marker.setIcon(
            L.divIcon({
              className: `my-custom-pin marker-with-shadow marker-${item.DeviceId}`,
              html: `<div style="text-align: center; width: 100%;">
                    <div class="svg-icon-container ${status} ${blinkClass}" style="width: ${30 * finalScale}px; height: ${30 * finalScale}px; margin: 0 auto;">
                      <div class="svg-icon" style="width: ${30 * finalScale}px; height: ${30 * finalScale}px;">
                        ${getSvgIcon(item.DeviceTypeCode || getDeviceTypeCode(item.DeviceType))}
                      </div>
                    </div>
                    <div class="marker-bottom-text" style="margin-top: 4px; width: ${deviceNameWidth}px; font-size: ${deviceNameFontSize}px;">${item.DeviceName || "未命名设备"}</div>
                    <div id="device_${item.DeviceId}" class="paramList ${status}" style="margin: 0 auto; font-size: 11px; width: 200px;">
                      ${paramHtml}
                    </div>
                  </div>`,
              iconSize: [220, 120],
              iconAnchor: [110, 60],
              popupAnchor: [0, -40]
            })
          );
        }
      }
    });
  }
  // 创建备用地图（在主地图加载失败时使用）
  function createBackupMap(mapExtent: number[], crs: L.CRS) {
    console.log("创建备用地图");
    try {
      // 创建一个简单的备用地图
      if (map) {
        map.remove();
      }
      map = L.map(mapContainerId.value, {
        crs: crs,
        minZoom: 1,
        maxZoom: 6,
        zoomControl: false,
        attributionControl: false,
        scrollWheelZoom: false,
        doubleClickZoom: false,
        touchZoom: false,
        boxZoom: false,
        keyboard: false,
        dragging: false
      });

      // 使用默认的背景颜色
      const container = document.getElementById(mapContainerId.value);
      if (container) {
        container.style.backgroundColor = "#143e6c";
      }

      // 定义地图的有效边界范围
      map.fitBounds([
        crs.unproject(L.point(mapExtent[2], mapExtent[3])),
        crs.unproject(L.point(mapExtent[0], mapExtent[1]))
      ]);

      // 显示加载失败的提示
      const message = L.DomUtil.create("div", "map-error-message");
      message.innerHTML = "地图加载失败，使用备用视图";
      message.style.cssText = `
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        background-color: rgba(0, 0, 0, 0.7);
        color: white;
        padding: 10px 20px;
        border-radius: 5px;
        z-index: 1000;
      `;
      map.getContainer().appendChild(message);

      console.log("备用地图创建成功");
      mapInitialized.value = true;

      // 尝试加载标记点
      updateMarker();
    } catch (error) {
      console.error("备用地图创建失败:", error);
    }
  }

  // 从API获取地图数据
  async function getMapData() {
    try {
      filePath = "";
      markerList.value = [];
      markerGroup.value = [];

      const data = await GetInfoByPk(buildId.value);
      console.log("API返回原始数据:", data);

      if (data && data.Status) {
        const result = JSON.parse(data.Result);
        console.log("API返回解析后数据:", result);

        // 设置地图文件路径
        if (result.FilePath) {
          filePath = result.FilePath;
        }
        let configData = JSON.parse(result.MapConfig);
        const centerPointConfig = configData.find(item => item.DeviceId == -1);
        fixedMapCenter.value = centerPointConfig.position;
        // 处理 ExpandObjects 数据（实时设备数据）
        if (result.ExpandObjects && Array.isArray(result.ExpandObjects)) {
          console.log(
            "获取到 ExpandObjects 数据:",
            result.ExpandObjects.length,
            "个设备"
          );
          markerList.value = result.ExpandObjects;
        }
        // 如果没有 ExpandObjects，尝试解析 MapConfig
        else if (result.MapConfig) {
          try {
            if (typeof result.MapConfig === "string") {
              configData = JSON.parse(result.MapConfig);
            } else {
              configData = result.MapConfig;
            }

            if (Array.isArray(configData)) {
              console.log(
                "从 MapConfig 获取到数据:",
                configData.length,
                "个设备"
              );
              markerList.value = configData;
            }
          } catch (e) {
            console.error("解析 MapConfig 失败:", e);
          }
        }

        console.log("最终标记点数据:", markerList.value);
      } else {
        console.warn("API 请求失败或返回数据格式不正确");
      }
    } catch (error) {
      console.error("获取地图数据失败:", error);
    }
  }

  // 更新标记点
  function updateMarker() {
    if (featureGroup) {
      featureGroup.clearLayers();
    }

    if (markerGroup.value.length > 0) {
      markerGroup.value.forEach(marker => {
        if (marker && marker.remove) {
          marker.remove();
        }
      });
      markerGroup.value = [];
    }

    console.log(`正在更新 ${markerList.value.length} 个标记点`);

    // 如果没有标记点，直接返回
    if (!markerList.value || markerList.value.length === 0) {
      console.warn("没有标记点数据可显示");
      return;
    }

    // 遍历标记点数据
    markerList.value.forEach(item => {
      // 检查 position 属性
      if (
        !item.position ||
        !Array.isArray(item.position) ||
        item.position.length !== 2
      ) {
        console.warn(
          `设备 ${item.DeviceName || item.DeviceId} 缺少有效的 position 属性，跳过`
        );
        return;
      }
      // 添加标记点
      if (item.DeviceId !== -1) {
        addMarker(item, map ? map.getZoom() : 3);
      }
    });
  }

  // 添加标记点
  function addMarker(item, size) {
    if (item.DeviceId == -1) {
      return false;
    }
    // 地图的缩放层级范围是0-6
    const _maxZoom = 6;
    const _minZoom = 0;

    // 计算缩放比例：反转逻辑，地图缩放级别为0时图标最小，为6时图标最大
    // 设置缩放范围：最小0.6，最大1.5（整体缩小）

    // 直接使用线性映射：zoom=0对应scale=0.6，zoom=6对应scale=1.5
    const scale = 0.6 + (size / 6) * 0.9;

    // 确保比例在合理范围内
    const finalScale = Math.max(0.6, Math.min(1.5, scale));

    // 确保 Status 属性存在
    if (item.Status === undefined || item.Status === null) {
      item.Status = 0; // 默认为正常状态
    }

    const status = getStatusClass(item.Status);
    // 使用renderParamHtml函数渲染参数HTML
    const paramHtml = renderParamHtml(item, showParams.value);
    const blinkClass = item.Status === 2 ? "blink" : "";

    // 设备名称使用固定大小
    const deviceNameFontSize = 11;
    const deviceNameWidth = Math.max(90, item.DeviceName.length * 10);

    console.log(
      `添加标记点: ${item.DeviceName || "未命名设备"}, 状态: ${status}, 位置: [${item.position[0]}, ${item.position[1]}]`
    );

    // 设置不同状态的样式
    let statusColor, statusBorder, statusGlow;

    switch (item.Status) {
      case 1: // 离线
        statusColor = "#9e9e9e";
        statusBorder = "1px solid #9e9e9e";
        statusGlow = "0 0 6px rgba(158, 158, 158, 0.3)";
        break;
      case 2: // 告警
        statusColor = "#ef5350";
        statusBorder = "1px solid #ef5350";
        statusGlow = "0 0 8px rgba(239, 83, 80, 0.5)";
        break;
      default: // 正常
        statusColor = "#42a5f5";
        statusBorder = "1px solid #42a5f5";
        statusGlow = "0 0 6px rgba(66, 165, 245, 0.3)";
    }

    // 使用SVG图标而不是PNG图像
    const marker = L.marker(item.position, {
      icon: L.divIcon({
        className: `my-custom-pin marker-with-shadow marker-${item.DeviceId}`,
        html: `<div style="text-align: center; width: 100%;">
                <div class="svg-icon-container ${status} ${blinkClass}" style="width: ${32 * finalScale}px; height: ${32 * finalScale}px; margin: 0 auto; border: ${statusBorder}; box-shadow: ${statusGlow}; transition: all 0.3s ease;">
                  <div class="svg-icon" style="width: ${30 * finalScale}px; height: ${30 * finalScale}px; color: ${statusColor};">
                    ${getSvgIcon(item.DeviceTypeCode || getDeviceTypeCode(item.DeviceType))}
                  </div>
                </div>
                <div class="marker-bottom-text" style="margin-top: 4px; width: ${deviceNameWidth}px; font-size: ${deviceNameFontSize}px; background-color: rgba(${item.Status === 2 ? "239, 83, 80" : item.Status === 1 ? "158, 158, 158" : "66, 165, 245"}, 0.85); box-shadow: 0 2px 4px rgba(0, 0, 0, 0.15); border-radius: 4px; padding: 2px 6px; backdrop-filter: blur(3px);">${item.DeviceName || "未命名设备"}</div>
                <div id="device_${item.DeviceId}" class="paramList ${status}" style="margin: 0 auto; font-size: 11px; width: auto; max-width: 240px;">
                  ${showParams.value ? paramHtml : ""}
                </div>
              </div>`,
        iconSize: [240, 140],
        iconAnchor: [120, 70],
        popupAnchor: [0, -40]
      }),
      title: item.DeviceName || "未命名设备",
      alt: item.DeviceName || "未命名设备",
      interactive: true // 确保标记点可交互
    }).addTo(map);

    marker.DeviceId = item.DeviceId;
    markerGroup.value.push(marker);

    // 添加点击事件 - 仅触发activeMarkerId变化，不显示弹框
    marker.on("click", () => {
      console.log(`点击了设备: ${item.DeviceName} (ID: ${item.DeviceId})`);
      activeMarkerId.value = item.DeviceId;

      // 添加点击效果
      const markerElement = document.querySelector(
        `.marker-${item.DeviceId} .svg-icon-container`
      );
      if (markerElement) {
        markerElement.classList.add("marker-clicked");
        setTimeout(() => {
          markerElement.classList.remove("marker-clicked");
        }, 500);
      }
    });

    return marker;
  }

  // 切换参数显示状态
  function toggleParamsDisplay() {
    showParams.value = !showParams.value;
    // 清除现有标记
    if (markerGroup.value.length > 0) {
      markerGroup.value.forEach(marker => {
        if (marker && marker.remove) {
          marker.remove();
        }
      });
      markerGroup.value = [];
    }
    // 重新添加标记
    if (map) {
      markerList.value.forEach(item => {
        if (
          item.position &&
          Array.isArray(item.position) &&
          item.position.length === 2 &&
          item.DeviceId !== -1
        ) {
          addMarker(item, map.getZoom());
        }
      });
    }
  }

  // 添加自定义样式
  function addCustomStyles() {
    // 检查是否已存在样式
    if (document.getElementById("home-map-styles")) return;
    const style = document.createElement("style");
    style.id = "home-map-styles";
    style.textContent = `
      /* SVG图标容器样式 */
      .svg-icon-container {
        display: flex;
        justify-content: center;
        align-items: center;
        border-radius: 50%;
        background: #ffffff;
        box-shadow: 0 2px 6px rgba(0, 0, 0, 0.12);
        transition: all 0.3s ease;
        padding: 0px;
        transform: scale(0.9); /* 整体缩小到原来的90% */
      }

      /* 正常状态 - 使用浅蓝色调 */
      .svg-icon-container.normal {
        color: #42a5f5;
        border: 1px solid #42a5f5;
        background: rgba(255, 255, 255, 0.95);
      }

      /* 离线状态 - 使用淡灰色调 */
      .svg-icon-container.offline {
        color: #9e9e9e;
        border: 1px solid #9e9e9e;
        background: rgba(255, 255, 255, 0.95);
      }

      /* 告警状态 - 使用较柔和的红色调 */
      .svg-icon-container.alarm {
        color: #ef5350;
        border: 1px solid #ef5350;
        background: rgba(255, 255, 255, 0.95);
        animation: alarmPulse 1.5s infinite;
      }

      /* 告警动画 */
      @keyframes alarmPulse {
        0% {
          box-shadow: 0 0 6px rgba(239, 83, 80, 0.5);
        }
        50% {
          box-shadow: 0 0 12px rgba(239, 83, 80, 0.8);
        }
        100% {
          box-shadow: 0 0 6px rgba(239, 83, 80, 0.5);
        }
      }

      /* 点击效果 */
      .marker-clicked {
        transform: scale(1.1) !important;
        box-shadow: 0 0 16px rgba(66, 165, 245, 0.8) !important;
      }

      /* SVG图标本身的样式 */
      .svg-icon {
        display: flex;
        justify-content: center;
        align-items: center;
        filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.1));
      }

      .svg-icon svg {
        width: 100%;
        height: 100%;
        display: block;
      }

      /* 标记底部文本样式 */
      .marker-bottom-text {
        color: white;
        text-align: center;
        padding: 2px 4px;
        border-radius: 4px;
        font-size: 11px;
        font-weight: 500;
        z-index: 1000;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        text-shadow: 0 1px 2px rgba(0, 0, 0, 0.2);
        backdrop-filter: blur(3px);
        transition: all 0.3s ease;
      }
      
      /* 悬停效果 */
      .my-custom-pin:hover .marker-bottom-text {
        transform: translateY(-2px);
        box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
      }
      
      .my-custom-pin:hover .svg-icon-container {
        transform: scale(1.05);
        box-shadow: 0 4px 10px rgba(0, 0, 0, 0.15);
      }

      /* 闪烁效果 */
      .blink {
        animation: blinkAnimation 1s infinite alternate;
      }

      @keyframes blinkAnimation {
        from {
          opacity: 1;
        }
        to {
          opacity: 0.6;
        }
      }

      /* 参数容器样式 */
      .paramList {
        transition: all 0.3s ease;
        opacity: 0.95;
        transform: translateY(0);
      }

      .paramList:hover {
        opacity: 1;
        transform: translateY(-2px);
      }
      
      /* 改进滚动条样式 */
      .params-grid::-webkit-scrollbar {
        width: 4px;
      }
      
      .params-grid::-webkit-scrollbar-track {
        background: transparent;
      }
      
      .params-grid::-webkit-scrollbar-thumb {
        background: rgba(66, 165, 245, 0.5);
        border-radius: 4px;
      }
      
      .params-grid::-webkit-scrollbar-thumb:hover {
        background: rgba(66, 165, 245, 0.8);
      }
    `;
    document.head.appendChild(style);

    // 确保地图容器可以接收鼠标事件
    const mapContainer = document.getElementById(mapContainerId.value);
    if (mapContainer) {
      // 设置地图容器样式
      mapContainer.style.position = "fixed";
      mapContainer.style.top = "0";
      mapContainer.style.left = "0";
      mapContainer.style.width = "100%";
      mapContainer.style.height = "100%";
      mapContainer.style.zIndex = "0";
      mapContainer.style.pointerEvents = "auto";
      mapContainer.style.touchAction = "none";
      mapContainer.style.userSelect = "none";
      mapContainer.style.background = "transparent";
      mapContainer.style.overflow = "visible"; // 确保内容不被裁剪

      // 确保地图元素在DOM中的顺序是正确的（放在最底层）
      const parent = mapContainer.parentElement;
      if (parent) {
        parent.insertBefore(mapContainer, parent.firstChild);
      }

      // 添加鼠标事件监听器，确保拖动功能正常
      mapContainer.addEventListener("mousedown", e => {
        console.log("地图容器鼠标按下事件");
        // 防止事件冒泡，确保地图可以正常拖动
        e.stopPropagation();
      });

      // 处理滚轮事件，确保滚轮缩放功能正常
      mapContainer.addEventListener(
        "wheel",
        e => {
          // 阻止默认滚动行为
          e.preventDefault();
          // 确保事件不会冒泡到父元素
          e.stopPropagation();
          console.log("地图容器滚轮事件");
        },
        { passive: false }
      );

      // 防止页面滚动干扰地图
      document.addEventListener(
        "wheel",
        e => {
          const target = e.target as HTMLElement;
          if (
            target &&
            mapContainer &&
            (target === mapContainer || mapContainer.contains(target))
          ) {
            e.preventDefault();
          }
        },
        { passive: false }
      );
    }
  }

  // 组件挂载时初始化地图
  onMounted(() => {
    // 将generateSelectedParamListHtml函数注册到全局窗口对象
    if (typeof window !== "undefined") {
      window["generateSelectedParamListHtml"] = generateSelectedParamListHtml;
    }

    // 确保DOM已经渲染
    setTimeout(() => {
      // 检查地图容器是否存在
      const mapElement = document.getElementById(mapContainerId.value);
      if (!mapElement) {
        console.error(`找不到地图容器: ${mapContainerId.value}`);
        return;
      }

      // 设置容器样式确保可见
      mapElement.style.height = "100%";
      mapElement.style.minHeight = "100vh";
      mapElement.style.width = "100%";
      mapElement.style.position = "absolute";
      mapElement.style.top = "0";
      mapElement.style.left = "0";
      mapElement.style.zIndex = "0";
      mapElement.style.pointerEvents = "auto"; // 确保可以接收鼠标事件

      // 初始化地图
      nextTick(() => {
        initMap();
      });
      // 添加全局样式
      addPopupStyles();
      addCustomStyles();
    }, 800); // 延长延迟时间，确保DOM已经完全渲染
  });

  // 组件卸载时清理地图
  onUnmounted(() => {
    if (map) {
      map.remove();
      map = null;
    }

    // 移除样式
    const style = document.getElementById("marker-popup-styles");
    if (style) {
      style.remove();
    }

    const homeStyle = document.getElementById("home-map-styles");
    if (homeStyle) {
      homeStyle.remove();
    }

    // 清理全局窗口对象中的函数
    if (
      typeof window !== "undefined" &&
      window["generateSelectedParamListHtml"]
    ) {
      delete window["generateSelectedParamListHtml"];
    }
  });

  return {
    mapContainer: mapContainerId,
    mapInitialized,
    activeMarkerId,
    showParams,
    toggleParamsDisplay
  };
}

// 添加一个新函数，用于生成选定参数的HTML，确保与buildingMap模块样式一致
export function generateSelectedParamListHtml(
  paramList,
  selectedParams,
  _useScale = true,
  _scale = 1
) {
  const fontSize = 11; // 固定字体大小为11px

  if (!selectedParams || selectedParams.length === 0) return "";

  // 创建一个Map，用于快速查找选定的参数
  const selectedParamMap = new Map();
  selectedParams.forEach(param => {
    selectedParamMap.set(param.ParamCode, param);
  });

  // 如果有参数列表，则尝试从中获取最新的参数值
  if (paramList && paramList.length > 0) {
    // 构建一个参数代码到参数值的映射
    const paramValueMap = new Map();
    paramList.forEach(param => {
      if (param.ParamCode) {
        paramValueMap.set(param.ParamCode, {
          value: param.ParamValue || "--",
          unit: param.ParamUnit || ""
        });
      }
    });

    // 生成HTML，使用最新的参数值
    const paramsHtml = selectedParams
      .map(param => {
        const currentValue = paramValueMap.get(param.ParamCode);
        const displayValue = currentValue
          ? currentValue.value
          : param.ParamValue || "--";
        const displayUnit = currentValue
          ? currentValue.unit
          : param.ParamUnit || "";

        // 根据参数类型设置不同的颜色和显示样式
        let valueColor = "#1296db"; // 默认蓝色
        let bgColor = "rgba(18, 150, 219, 0.05)";
        let borderColor = "rgba(18, 150, 219, 0.3)";

        // 根据参数名或类型设置不同的颜色（可根据实际需求调整）
        if (param.ParamName.includes("温度")) {
          valueColor = "#ff6b6b"; // 温度用红色
          bgColor = "rgba(255, 107, 107, 0.05)";
          borderColor = "rgba(255, 107, 107, 0.3)";
        } else if (param.ParamName.includes("湿度")) {
          valueColor = "#00c9a7"; // 湿度用青绿色
          bgColor = "rgba(0, 201, 167, 0.05)";
          borderColor = "rgba(0, 201, 167, 0.3)";
        } else if (
          param.ParamName.includes("电压") ||
          param.ParamName.includes("功率")
        ) {
          valueColor = "#ffa726"; // 电力参数用橙色
          bgColor = "rgba(255, 167, 38, 0.05)";
          borderColor = "rgba(255, 167, 38, 0.3)";
        } else if (
          param.ParamName.includes("状态") ||
          param.ParamName.includes("开关")
        ) {
          valueColor = "#42a5f5"; // 状态参数用蓝色
          bgColor = "rgba(66, 165, 245, 0.05)";
          borderColor = "rgba(66, 165, 245, 0.3)";
        }

        // 改进的参数显示样式
        return `<div class="selected-param" style="height: 24px; display: flex; justify-content: space-between; align-items: center; margin: 4px 0; backdrop-filter: blur(5px);">
                  <span class="param-name" style="color: #333; font-size: ${fontSize}px; white-space: nowrap; margin-right: 6px;">${param.ParamName}:</span>
                  <span class="param-value" style="font-weight: 500; color: ${valueColor}; background-color: ${bgColor}; padding: 2px 8px; border-radius: 4px; border: 1px solid ${borderColor}; font-size: ${fontSize}px; min-width: 28px; text-align: center; box-shadow: 0 1px 2px rgba(0,0,0,0.05);">${displayValue}${displayUnit}</span>
                </div>`;
      })
      .join("");

    // 改进的参数容器
    return `<div class="params-grid" style="max-height: 120px; overflow-y: auto; padding: 4px; background: rgba(255, 255, 255, 0.7); border-radius: 6px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08); backdrop-filter: blur(8px); scrollbar-width: thin; scrollbar-color: rgba(25, 100, 250, 0.3) transparent; margin-top: 5px;">
      <style>
        .params-grid::-webkit-scrollbar {
          width: 4px;
          height: 4px;
        }
        .params-grid::-webkit-scrollbar-track {
          box-shadow: 0px 1px 3px transparent inset;
          border-radius: 10px;
          background-color: transparent;
        }
        .params-grid::-webkit-scrollbar-thumb {
          box-shadow: 0px 1px 3px rgba(25, 100, 250, 0.3) inset;
          border-radius: 10px;
          background-color: rgba(92, 92, 92, 0.5);
        }
        .params-grid .selected-param:hover {
          transform: translateY(-1px);
          transition: all 0.2s ease;
        }
        .params-grid .selected-param .param-value {
          transition: all 0.3s ease;
        }
        .params-grid .selected-param:hover .param-value {
          box-shadow: 0 2px 5px rgba(0,0,0,0.1);
        }
      </style>
      ${paramsHtml}
    </div>`;
  } else {
    // 如果没有参数列表，则直接使用选定参数中的值
    const paramsHtml = selectedParams
      .map(param => {
        // 根据参数类型设置不同的颜色和显示样式
        let valueColor = "#1296db"; // 默认蓝色
        let bgColor = "rgba(18, 150, 219, 0.05)";
        let borderColor = "rgba(18, 150, 219, 0.3)";

        // 根据参数名或类型设置不同的颜色
        if (param.ParamName.includes("温度")) {
          valueColor = "#ff6b6b"; // 温度用红色
          bgColor = "rgba(255, 107, 107, 0.05)";
          borderColor = "rgba(255, 107, 107, 0.3)";
        } else if (param.ParamName.includes("湿度")) {
          valueColor = "#00c9a7"; // 湿度用青绿色
          bgColor = "rgba(0, 201, 167, 0.05)";
          borderColor = "rgba(0, 201, 167, 0.3)";
        } else if (
          param.ParamName.includes("电压") ||
          param.ParamName.includes("功率")
        ) {
          valueColor = "#ffa726"; // 电力参数用橙色
          bgColor = "rgba(255, 167, 38, 0.05)";
          borderColor = "rgba(255, 167, 38, 0.3)";
        } else if (
          param.ParamName.includes("状态") ||
          param.ParamName.includes("开关")
        ) {
          valueColor = "#42a5f5"; // 状态参数用蓝色
          bgColor = "rgba(66, 165, 245, 0.05)";
          borderColor = "rgba(66, 165, 245, 0.3)";
        }

        return `<div class="selected-param" style="height: 24px; display: flex; justify-content: space-between; align-items: center; margin: 4px 0; backdrop-filter: blur(5px);">
                  <span class="param-name" style="color: #333; font-size: ${fontSize}px; white-space: nowrap; margin-right: 6px;">${param.ParamName}:</span>
                  <span class="param-value" style="font-weight: 500; color: ${valueColor}; background-color: ${bgColor}; padding: 2px 8px; border-radius: 4px; border: 1px solid ${borderColor}; font-size: ${fontSize}px; min-width: 28px; text-align: center; box-shadow: 0 1px 2px rgba(0,0,0,0.05);">${param.ParamValue || "--"}${param.ParamUnit || ""}</span>
                </div>`;
      })
      .join("");

    // 改进的参数容器
    return `<div class="params-grid" style="max-height: 120px; overflow-y: auto; padding: 4px; background: rgba(255, 255, 255, 0.7); border-radius: 6px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08); backdrop-filter: blur(8px); scrollbar-width: thin; scrollbar-color: rgba(25, 100, 250, 0.3) transparent; margin-top: 5px;">
      <style>
        .params-grid::-webkit-scrollbar {
          width: 4px;
          height: 4px;
        }
        .params-grid::-webkit-scrollbar-track {
          box-shadow: 0px 1px 3px transparent inset;
          border-radius: 10px;
          background-color: transparent;
        }
        .params-grid::-webkit-scrollbar-thumb {
          box-shadow: 0px 1px 3px rgba(25, 100, 250, 0.3) inset;
          border-radius: 10px;
          background-color: rgba(92, 92, 92, 0.5);
        }
        .params-grid .selected-param:hover {
          transform: translateY(-1px);
          transition: all 0.2s ease;
        }
        .params-grid .selected-param .param-value {
          transition: all 0.3s ease;
        }
        .params-grid .selected-param:hover .param-value {
          box-shadow: 0 2px 5px rgba(0,0,0,0.1);
        }
      </style>
      ${paramsHtml}
    </div>`;
  }
}
