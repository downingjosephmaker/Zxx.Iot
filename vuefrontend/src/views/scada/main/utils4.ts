/**
 * utils4.ts - 编辑器工具函数集合4
 *
 * 本文件包含大型DOM创建和图表相关功能
 * 从 utils3.ts 中提取以减小文件大小
 */

import { ElMessage } from "element-plus";
import { nextTick } from "vue";
import * as echarts from "echarts";
import { getDeviceLatest, getDeviceHistory } from "@/api/iot/monitor";

/** 本地时间格式化(yyyy-MM-dd HH:mm:ss)，GetDeviceHistory入参用 */
const formatLocalTime = (d: Date): string => {
  const pad = (n: number) => (n < 10 ? `0${n}` : String(n));
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
};

/** IoT数据集：拉设备最新值并按勾选点位过滤 */
const fetchIotLatestPoints = async (dataset: any) => {
  const res = await getDeviceLatest(dataset.deviceId);
  if (!res.Status) return null;
  const points = JSON.parse(res.Result) as any[];
  const codes = new Set((dataset.points || []).map((p: any) => p.ParamCode));
  return codes.size ? points.filter(p => codes.has(p.ParamCode)) : points;
};

// ========================================
// 图表主题配置
// ========================================

// 主题配置
const chartThemes: any = {
  'default': {
    color: ['#5470c6', '#91cc75', '#fac858', '#ee6666', '#73c0de', '#3ba272', '#fc8452', '#9a60b4', '#ea7ccc'],
    backgroundColor: 'transparent',
    textStyle: { color: '#666666' }
  },
  'dark': {
    color: ['#c23531', '#2f4554', '#61a0a8', '#d48265', '#91c7ae', '#749f83'],
    backgroundColor: '#100C2A',
    textStyle: { color: '#ffffff' }
  },
  'light': {
    color: ['#5470c6', '#91cc75', '#fac858', '#ee6666', '#73c0de', '#3ba272'],
    backgroundColor: '#ffffff',
    textStyle: { color: '#333333' }
  },
  'tech-blue': {
    color: ['#4facfe', '#00f2fe', '#43e97b', '#38f9d7', '#fa709a', '#fee140'],
    backgroundColor: 'linear-gradient(135deg, rgba(79, 172, 254, 0.1) 0%, rgba(0, 242, 254, 0.1) 100%)',
    textStyle: { color: '#2c3e50', fontWeight: '500' }
  },
  'pink-dream': {
    color: ['#f093fb', '#f5576c', '#fa709a', '#fee140', '#30cfd0', '#a8edea'],
    backgroundColor: 'linear-gradient(135deg, rgba(240, 147, 251, 0.1) 0%, rgba(245, 87, 108, 0.1) 100%)',
    textStyle: { color: '#c2185b', fontWeight: '500' }
  },
  'nature-green': {
    color: ['#11998e', '#38ef7d', '#43e97b', '#38f9d7', '#17ead9', '#6078ea'],
    backgroundColor: 'linear-gradient(135deg, rgba(17, 153, 142, 0.1) 0%, rgba(56, 239, 125, 0.1) 100%)',
    textStyle: { color: '#2e7d32', fontWeight: '500' }
  },
  'sunset-orange': {
    color: ['#ff6b6b', '#ff8e53', '#ffd93d', '#6bcf7f', '#4d96ff', '#a78bfa'],
    backgroundColor: 'linear-gradient(135deg, rgba(255, 107, 107, 0.1) 0%, rgba(255, 142, 83, 0.1) 100%)',
    textStyle: { color: '#d84315', fontWeight: '500' }
  },
  'ocean-blue': {
    color: ['#0093E9', '#80D0C7', '#13547a', '#009ffd', '#2a2a72', '#00c9ff'],
    backgroundColor: 'linear-gradient(135deg, rgba(0, 147, 233, 0.1) 0%, rgba(128, 208, 199, 0.1) 100%)',
    textStyle: { color: '#01579b', fontWeight: '500' }
  },
  'business': {
    color: ['#2c3e50', '#34495e', '#7f8c8d', '#95a5a6', '#bdc3c7', '#ecf0f1'],
    backgroundColor: '#f8f9fa',
    textStyle: { color: '#2c3e50', fontFamily: 'Arial, sans-serif', fontWeight: '600' }
  },
  'minimal-bw': {
    color: ['#000000', '#333333', '#666666', '#999999', '#cccccc', '#eeeeee'],
    backgroundColor: '#ffffff',
    textStyle: { color: '#000000', fontFamily: 'Helvetica, Arial, sans-serif', fontWeight: '500' }
  },
  'heatmap': {
    color: ['#313695', '#4575b4', '#74add1', '#abd9e9', '#fee090', '#fdae61', '#f46d43', '#d73027', '#a50026'],
    backgroundColor: 'linear-gradient(135deg, rgba(49, 54, 149, 0.05) 0%, rgba(165, 0, 38, 0.05) 100%)',
    textStyle: { color: '#311b92', fontWeight: '500' }
  },
  'rainbow': {
    color: ['#FF0080', '#FF8C00', '#FFD700', '#00FF00', '#00CED1', '#1E90FF', '#9370DB', '#FF1493'],
    backgroundColor: 'linear-gradient(135deg, rgba(255, 0, 128, 0.05) 0%, rgba(147, 112, 219, 0.05) 100%)',
    textStyle: { color: '#4a148c', fontWeight: '500' }
  }
};

// 颜色方案
const colorSchemes: any = {
  default: ['#5470c6', '#91cc75', '#fac858', '#ee6666', '#73c0de', '#3ba272', '#fc8452', '#9a60b4', '#ea7ccc'],
  warm: ['#ff6b6b', '#f06595', '#cc5de8', '#845ef7', '#5c7cfa', '#339af0', '#22b8cf', '#20c997', '#51cf66'],
  cool: ['#1890ff', '#13c2c2', '#52c41a', '#faad14', '#f5222d', '#722ed1', '#eb2f96', '#fa8c16', '#a0d911'],
  vibrant: ['#ff0080', '#7928ca', '#ff0080', '#ff4d4f', '#faad14', '#52c41a', '#1890ff', '#722ed1', '#eb2f96']
};

// ========================================
// 图表相关函数
// ========================================

/**
 * 初始化ECharts图表
 * @param container - 图表容器元素
 * @param component - 组件配置
 * @param echarts - ECharts实例
 */
export const initEChart = (container: HTMLElement, component: any, echartsInstance: any) => {
  try {
    // 先清理旧的ECharts实例和观察器
    const oldInstance = (container as any).__echarts__;
    if (oldInstance) {
      console.log("销毁旧的ECharts实例");
      oldInstance.dispose();
      (container as any).__echarts__ = null;
    }

    const oldObserver = (container as any).__resizeObserver__;
    if (oldObserver) {
      oldObserver.disconnect();
      (container as any).__resizeObserver__ = null;
    }

    // 创建新的ECharts实例
    const chartInstance = echartsInstance.init(container);
    const options = generateChartOptions(component.chartConfig);
    chartInstance.setOption(options);

    // 保存图表实例引用
    (container as any).__echarts__ = chartInstance;

    // 响应式处理
    const resizeObserver = new ResizeObserver(() => {
      chartInstance.resize();
    });
    resizeObserver.observe(container);

    // 保存观察器引用以便清理
    (container as any).__resizeObserver__ = resizeObserver;

    console.log("ECharts图表初始化成功:", component.name, "类型:", component.chartConfig?.type);
  } catch (error) {
    console.error("ECharts图表初始化失败:", error);
    container.innerHTML = `
      <div style="
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 100%;
        color: #999;
        font-size: 14px;
      ">
        <div style="font-size: 32px; margin-bottom: 10px;">📊</div>
        <div>图表初始化失败</div>
        <div style="font-size: 12px; margin-top: 5px;">右键配置数据源</div>
      </div>
    `;
  }
};

/**
 * 生成图表配置选项
 * @param chartConfig - 图表配置对象
 * @returns ECharts配置对象
 */
export const generateChartOptions = (chartConfig: any) => {
  const type = chartConfig.type || 'line';
  const theme = chartConfig.theme || 'default';
  const colorScheme = chartConfig.colorScheme || 'default';

  // 获取主题配置
  const themeConfig = chartThemes[theme] || chartThemes.default;

  // 优先使用颜色方案，如果没有则使用主题颜色
  const colors = colorSchemes[colorScheme] || themeConfig.color;
  const textColor = themeConfig.textStyle?.color || '#666666';
  const backgroundColor = themeConfig.backgroundColor || 'transparent';

  const options: any = {
    color: colors,
    backgroundColor: backgroundColor,
    textStyle: themeConfig.textStyle || { color: textColor },
    title: {
      text: chartConfig.title,
      show: true,
      left: "center",
      textStyle: {
        fontSize: 16,
        fontWeight: "bold",
        color: textColor
      }
    },
    tooltip: {
      show: true,
      trigger: ['pie', 'gauge', 'funnel'].includes(type) ? "item" : "axis",
      backgroundColor: 'rgba(50, 50, 50, 0.9)',
      borderColor: '#333',
      borderWidth: 0,
      textStyle: {
        color: '#fff'
      }
    }
  };

  // 根据图表类型生成配置
  switch (type) {
    case 'line':
      options.legend = { show: true, bottom: 10, textStyle: { color: textColor } };
      options.xAxis = {
        type: "category",
        data: chartConfig.staticData?.map(item => item.name) || [],
        axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
        axisLabel: { color: textColor }
      };
      options.yAxis = {
        type: "value",
        axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
        axisLabel: { color: textColor },
        splitLine: { lineStyle: { color: textColor, opacity: 0.1 } }
      };
      options.series = [{
        type: "line",
        data: chartConfig.staticData?.map(item => item.value) || [],
        smooth: chartConfig.smoothLine !== false,
        lineStyle: { width: 2 }
      }];
      break;

    case 'bar':
      options.legend = { show: true, bottom: 10, textStyle: { color: textColor } };
      options.xAxis = {
        type: "category",
        data: chartConfig.staticData?.map(item => item.name) || [],
        axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
        axisLabel: { color: textColor }
      };
      options.yAxis = {
        type: "value",
        axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
        axisLabel: { color: textColor },
        splitLine: { lineStyle: { color: textColor, opacity: 0.1 } }
      };
      options.series = [{
        type: "bar",
        data: chartConfig.staticData?.map(item => item.value) || []
      }];
      break;

    case 'pie':
      options.legend = { show: true, bottom: 10, textStyle: { color: textColor } };
      options.series = [{
        type: "pie",
        radius: ["40%", "70%"],
        center: ["50%", "60%"],
        data: chartConfig.staticData || [],
        emphasis: {
          itemStyle: {
            shadowBlur: 10,
            shadowOffsetX: 0,
            shadowColor: "rgba(0, 0, 0, 0.5)"
          }
        },
        label: {
          show: true,
          formatter: "{b}: {d}%",
          color: textColor
        }
      }];
      break;

    case 'area':
      options.legend = { show: true, bottom: 10, textStyle: { color: textColor } };
      options.xAxis = {
        type: "category",
        boundaryGap: false,
        data: chartConfig.staticData?.map(item => item.name) || [],
        axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
        axisLabel: { color: textColor }
      };
      options.yAxis = {
        type: "value",
        axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
        axisLabel: { color: textColor },
        splitLine: { lineStyle: { color: textColor, opacity: 0.1 } }
      };
      options.series = [{
        type: "line",
        data: chartConfig.staticData?.map(item => item.value) || [],
        smooth: chartConfig.smoothLine !== false,
        areaStyle: { opacity: 0.6 },
        lineStyle: { width: 2 }
      }];
      break;

    case 'gauge':
      // 仪表盘不需要legend
      delete options.legend;
      const gaugeValue = chartConfig.staticData?.[0]?.value || 0;
      options.series = [{
        type: 'gauge',
        radius: '80%',
        startAngle: 200,
        endAngle: -20,
        min: 0,
        max: 100,
        splitNumber: 10,
        progress: {
          show: true,
          width: 18
        },
        axisLine: {
          lineStyle: {
            width: 18
          }
        },
        axisTick: {
          show: true,
          splitNumber: 5,
          lineStyle: {
            width: 2,
            color: textColor
          }
        },
        splitLine: {
          length: 12,
          lineStyle: {
            width: 3,
            color: textColor
          }
        },
        axisLabel: {
          distance: 25,
          color: textColor,
          fontSize: 12
        },
        detail: {
          valueAnimation: true,
          fontSize: 36,
          fontWeight: 'bold',
          offsetCenter: [0, '50%'],
          formatter: '{value}%',
          color: colors[0] || textColor
        },
        data: [{
          value: gaugeValue,
          name: chartConfig.staticData?.[0]?.name || '指标'
        }]
      }];
      break;

    case 'radar':
      options.legend = { show: true, bottom: 10, textStyle: { color: textColor } };
      // 从staticData生成雷达图指标
      const indicators = chartConfig.staticData?.map(item => ({
        name: item.name,
        max: 100
      })) || [];
      const radarValues = chartConfig.staticData?.map(item => item.value) || [];

      // 添加整体布局控制
      options.grid = {
        left: '15%',
        right: '15%',
        top: '10%',
        bottom: '15%',
        containLabel: true
      };

      options.radar = {
        center: ['50%', '45%'], // 雷达图中心位置，略微下移为标题留空间
        radius: '45%', // 减小半径到45%，为指标标签留出足够空间
        indicator: indicators,
        shape: 'circle',
        splitNumber: 4,
        name: {
          textStyle: {
            color: textColor,
            fontSize: 12
          }
        },
        axisLine: {
          lineStyle: {
            color: textColor,
            opacity: 0.3
          }
        },
        splitLine: {
          lineStyle: {
            color: textColor,
            opacity: 0.2
          }
        },
        splitArea: {
          areaStyle: {
            color: 'transparent'
          }
        }
      };
      options.series = [{
        type: 'radar',
        data: [{
          value: radarValues,
          name: chartConfig.title || '数据'
        }]
      }];
      break;

    case 'funnel':
      options.legend = { show: true, bottom: 10, textStyle: { color: textColor } };
      options.series = [{
        type: 'funnel',
        left: '10%',
        top: 60,
        bottom: 60,
        width: '80%',
        min: 0,
        max: 100,
        minSize: '0%',
        maxSize: '100%',
        sort: 'descending',
        gap: 2,
        label: {
          show: true,
          position: 'inside',
          formatter: '{b}: {c}',
          fontSize: 14
        },
        data: chartConfig.staticData || []
      }];
      break;

    case 'scatter':
      options.legend = { show: true, bottom: 10, textStyle: { color: textColor } };
      options.xAxis = {
        type: "value",
        axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
        axisLabel: { color: textColor },
        splitLine: { lineStyle: { color: textColor, opacity: 0.1 } }
      };
      options.yAxis = {
        type: "value",
        axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
        axisLabel: { color: textColor },
        splitLine: { lineStyle: { color: textColor, opacity: 0.1 } }
      };
      // 将{name, value}格式转换为scatter需要的[x, y]格式
      const scatterData = chartConfig.staticData?.map((item, index) =>
        [index, item.value]
      ) || [];
      options.series = [{
        type: 'scatter',
        symbolSize: 12,
        data: scatterData,
        itemStyle: {
          shadowBlur: 10,
          shadowOffsetY: 3,
          opacity: 0.8
        }
      }];
      break;

    case 'candlestick':
      options.legend = { show: true, bottom: 10, textStyle: { color: textColor } };
      options.xAxis = {
        type: "category",
        data: chartConfig.staticData?.map(item => item.name) || [],
        axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
        axisLabel: { color: textColor }
      };
      options.yAxis = {
        type: "value",
        scale: true,
        axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
        axisLabel: { color: textColor },
        splitLine: { lineStyle: { color: textColor, opacity: 0.1 } }
      };
      // K线图需要[开盘, 收盘, 最低, 最高]格式
      // 这里简化处理，使用value生成模拟数据
      const klineData = chartConfig.staticData?.map(item => {
        const value = item.value || 100;
        const open = value * (0.95 + Math.random() * 0.05);
        const close = value * (0.95 + Math.random() * 0.1);
        const low = Math.min(open, close) * (0.97 + Math.random() * 0.03);
        const high = Math.max(open, close) * (1 + Math.random() * 0.05);
        return [open, close, low, high];
      }) || [];

      options.series = [{
        type: 'candlestick',
        data: klineData,
        itemStyle: {
          color: '#ec0000',
          color0: '#00da3c',
          borderColor: '#ec0000',
          borderColor0: '#00da3c'
        }
      }];
      break;

    default:
      // 默认使用折线图
      options.legend = { show: true, bottom: 10, textStyle: { color: textColor } };
      options.xAxis = {
        type: "category",
        data: chartConfig.staticData?.map(item => item.name) || [],
        axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
        axisLabel: { color: textColor }
      };
      options.yAxis = {
        type: "value",
        axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
        axisLabel: { color: textColor },
        splitLine: { lineStyle: { color: textColor, opacity: 0.1 } }
      };
      options.series = [{
        type: "line",
        data: chartConfig.staticData?.map(item => item.value) || [],
        smooth: true,
        lineStyle: { width: 2 }
      }];
  }

  return options;
};

/**
 * 创建图表DOM元素
 * @param component - 组件配置
 * @param canvasContent - 画布内容元素
 * @param setupComponentInteractions - 组件交互设置函数
 * @param showChartConfigDialog - 显示图表配置对话框函数
 * @param initEChart - 初始化ECharts函数
 */
export const createChartElement = (
  component: any,
  canvasContent: Element,
  setupComponentInteractions: (element: HTMLElement, component: any) => void,
  showChartConfigDialog: (component: any) => void,
  initEChartFn: (container: HTMLElement, component: any) => void
) => {
  const element = document.createElement("div");
  element.id = component.id;
  element.className = "fuxa-component chart-component";

  // 设置容器样式
  element.style.cssText = `
    position: absolute;
    left: ${component.position.x}px;
    top: ${component.position.y}px;
    width: ${component.size.width}px;
    height: ${component.size.height}px;
    border: ${component.style?.borderWidth || 1}px solid ${component.style?.borderColor || "#e4e7ed"};
    border-radius: ${component.style?.borderRadius || 4}px;
    background: ${component.style?.backgroundColor || "#ffffff"};
    cursor: pointer;
    user-select: none;
    z-index: 10;
    overflow: hidden;
  `;

  // 创建图表容器
  const chartContainer = document.createElement("div");
  chartContainer.className = "chart-container";
  chartContainer.style.cssText = `
    width: 100%;
    height: 100%;
    padding: 0;
    margin: 0;
  `;

  element.appendChild(chartContainer);

  // 存储原始边框样式
  element.setAttribute(
    "data-original-border-color",
    component.style?.borderColor || "#e4e7ed"
  );
  element.setAttribute(
    "data-original-border-width",
    (component.style?.borderWidth || 1) + "px"
  );

  // 设置组件交互
  setupComponentInteractions(element, component);

  // 添加右键菜单事件（图表专用）
  element.addEventListener("contextmenu", e => {
    e.preventDefault();
    e.stopPropagation();
    showChartConfigDialog(component);
  });

  canvasContent.appendChild(element);

  // 初始化ECharts图表
  setTimeout(() => {
    initEChartFn(chartContainer as HTMLElement, component);
  }, 100);

  return element;
};

// ========================================
// 大型DOM创建函数
// ========================================

/**
 * 创建摄像头DOM元素（实时视频流）
 * @param component - 组件配置
 * @param canvasContent - 画布内容元素
 * @param setupComponentInteractions - 组件交互设置函数
 */
export const createWebcamElement = (component: any, canvasContent: Element, setupComponentInteractions: (element: HTMLElement, component: any) => void) => {
  const element = document.createElement("div");
  element.id = component.id;
  element.className = "fuxa-component webcam-component";

  // 设置容器样式
  element.style.cssText = `
    position: absolute;
    left: ${component.position.x}px;
    top: ${component.position.y}px;
    width: ${component.size.width}px;
    height: ${component.size.height}px;
    border: ${component.style?.borderWidth || 1}px solid ${component.style?.borderColor || "#00b4db"};
    border-radius: ${component.style?.borderRadius || 4}px;
    background: ${component.style?.backgroundColor || "#000000"};
    overflow: hidden;
    cursor: pointer;
    user-select: none;
    z-index: 10;
  `;

  // 如果没有配置流地址，显示占位符
  if (!component.properties?.streamUrl) {
    element.innerHTML = `
      <div style="
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        color: #00b4db;
        font-size: 12px;
        text-align: center;
        padding: 20px;
        width: 100%;
        height: 100%;
        background: #000;
      ">
        <div style="font-size: 48px; margin-bottom: 10px;">📹</div>
        <div>实时摄像头</div>
        <div style="font-size: 10px; margin-top: 4px; color: #666;">右键选择"摄像头配置"设置视频流</div>
      </div>
    `;

    setupComponentInteractions(element, component);
    canvasContent.appendChild(element);
    return element;
  }

  // 创建video元素
  const video = document.createElement("video");
  const streamUrl = component.properties.streamUrl;
  const protocol = component.properties.protocol || "hls";

  // 设置video基本属性
  video.controls = component.properties.enableControls !== false;
  video.autoplay = component.properties.autoPlay !== false;
  video.muted = true; // 默认静音以支持自动播放
  video.playsInline = true; // iOS支持

  video.style.cssText = `
    width: 100%;
    height: 100%;
    object-fit: ${component.properties.objectFit || "contain"};
    display: block;
  `;

  // 创建状态指示器
  const statusIndicator = document.createElement("div");
  statusIndicator.style.cssText = `
    position: absolute;
    top: 8px;
    right: 8px;
    padding: 4px 8px;
    background: rgba(0, 0, 0, 0.7);
    color: #fff;
    font-size: 10px;
    border-radius: 4px;
    z-index: 20;
    display: none;
  `;
  statusIndicator.textContent = "连接中...";

  // 创建错误提示
  const errorDiv = document.createElement("div");
  errorDiv.style.cssText = `
    display: none;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    color: #ff6b6b;
    font-size: 12px;
    text-align: center;
    padding: 20px;
    width: 100%;
    height: 100%;
    background: #000;
    position: absolute;
    top: 0;
    left: 0;
  `;

  // 连接视频流
  const connectStream = async () => {
    try {
      statusIndicator.style.display = "block";
      statusIndicator.textContent = "连接中...";

      if (protocol === "hls" && streamUrl.includes(".m3u8")) {
        // HLS流处理
        if ((window as any).Hls) {
          const Hls = (window as any).Hls;

          if (Hls.isSupported()) {
            const hls = new Hls({
              maxBufferLength: component.properties.bufferSize || 1,
              maxMaxBufferLength: 3,
              liveSyncDuration: 1,
            });

            hls.loadSource(streamUrl);
            hls.attachMedia(video);

            hls.on(Hls.Events.MANIFEST_PARSED, () => {
              console.log("HLS流连接成功:", streamUrl);
              statusIndicator.textContent = "● 在线";
              statusIndicator.style.color = "#4ade80";

              if (component.properties.autoPlay !== false) {
                video.play().catch(err => {
                  console.warn("自动播放失败:", err);
                });
              }

              // 3秒后隐藏状态指示器
              setTimeout(() => {
                statusIndicator.style.display = "none";
              }, 3000);
            });

            hls.on(Hls.Events.ERROR, (_: any, data: any) => {
              if (data.fatal) {
                console.error("HLS错误:", data);
                showError(`HLS错误: ${data.type} - ${data.details}`);
              }
            });

            // 保存hls实例到元素上，方便后续清理
            (element as any).__hlsInstance = hls;
          } else if (video.canPlayType("application/vnd.apple.mpegurl")) {
            // Safari原生支持HLS
            video.src = streamUrl;
            video.onloadedmetadata = () => {
              console.log("HLS流连接成功(原生):", streamUrl);
              statusIndicator.textContent = "● 在线";
              statusIndicator.style.color = "#4ade80";
              setTimeout(() => statusIndicator.style.display = "none", 3000);
            };
          } else {
            showError("浏览器不支持HLS播放，请使用Chrome/Safari");
          }
        } else {
          // 尝试原生播放
          if (video.canPlayType("application/vnd.apple.mpegurl")) {
            video.src = streamUrl;
            video.onloadedmetadata = () => {
              console.log("HLS流连接成功(原生):", streamUrl);
              statusIndicator.textContent = "● 在线";
              statusIndicator.style.color = "#4ade80";
              setTimeout(() => statusIndicator.style.display = "none", 3000);
            };
          } else {
            showError("未加载hls.js库，且浏览器不支持原生HLS");
          }
        }
      } else if (protocol === "http" || protocol === "https") {
        // HTTP/HTTPS直接流
        video.src = streamUrl;
        video.onloadedmetadata = () => {
          console.log("HTTP流连接成功:", streamUrl);
          statusIndicator.textContent = "● 在线";
          statusIndicator.style.color = "#4ade80";
          setTimeout(() => statusIndicator.style.display = "none", 3000);
        };
      } else if (protocol === "rtsp" || protocol === "rtmp") {
        showError(`${protocol.toUpperCase()}需要服务器端转码支持`);
      } else if (protocol === "webrtc") {
        showError("WebRTC需要配置信令服务器");
      } else {
        showError("不支持的协议类型");
      }

      // 视频加载错误处理
      video.onerror = (e) => {
        console.error("视频加载失败:", e);
        showError(`连接失败: ${streamUrl}`);
      };

    } catch (error) {
      console.error("连接视频流失败:", error);
      showError(`连接失败: ${(error as Error).message}`);
    }
  };

  // 显示错误
  const showError = (message: string) => {
    statusIndicator.style.display = "none";
    errorDiv.style.display = "flex";
    errorDiv.innerHTML = `
      <div style="font-size: 48px; margin-bottom: 10px;">⚠️</div>
      <div>${message}</div>
      <div style="font-size: 10px; margin-top: 4px; word-break: break-all; color: #666;">${streamUrl}</div>
      <button style="
        margin-top: 12px;
        padding: 6px 16px;
        background: #00b4db;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 12px;
      " onclick="this.closest('.webcam-component').dispatchEvent(new CustomEvent('reconnect'))">
        重新连接
      </button>
    `;
  };

  // 组装元素
  element.appendChild(video);
  element.appendChild(statusIndicator);
  element.appendChild(errorDiv);

  // 监听重连事件
  element.addEventListener("reconnect", () => {
    errorDiv.style.display = "none";
    video.style.display = "block";
    connectStream();
  });

  // 存储原始边框样式
  element.setAttribute("data-original-border-color", component.style?.borderColor || "#00b4db");
  element.setAttribute("data-original-border-width", (component.style?.borderWidth || 1) + "px");

  // 添加摄像头数据属性
  element.setAttribute("data-webcam-url", streamUrl);
  element.setAttribute("data-webcam-protocol", protocol);
  element.setAttribute("data-webcam-name", component.properties.name || "摄像头");

  // 设置组件交互
  setupComponentInteractions(element, component);

  canvasContent.appendChild(element);

  // 启动连接
  connectStream();

  return element;
};

/**
 * 创建iframe DOM元素
 * @param component - 组件配置
 * @param canvasContent - 画布内容元素
 * @param setupComponentInteractions - 组件交互设置函数
 */
export const createIframeElement = (component: any, canvasContent: Element, setupComponentInteractions: (element: HTMLElement, component: any) => void) => {
  const element = document.createElement("div");
  element.id = component.id;
  element.className = "fuxa-component iframe-component";

  // 设置容器样式（position: relative 用于遮罩层的绝对定位）
  element.style.cssText = `
    position: absolute;
    left: ${component.position.x}px;
    top: ${component.position.y}px;
    width: ${component.size.width}px;
    height: ${component.size.height}px;
    border: ${component.style?.borderWidth || 1}px solid ${component.style?.borderColor || "#e4e7ed"};
    border-radius: ${component.style?.borderRadius || 4}px;
    background: ${component.style?.backgroundColor || "#ffffff"};
    overflow: hidden;
    cursor: pointer;
    user-select: none;
    z-index: 10;
  `;

  // 确保容器内部使用相对定位，使遮罩层正确覆盖
  element.style.position = "absolute";

  // 创建iframe元素 - 只设置基本属性
  const iframe = document.createElement("iframe");
  iframe.src = component.properties.url || "https://www.example.com";
  iframe.style.cssText = `
    width: 100%;
    height: 100%;
    border: none;
    display: block;
  `;

  // iframe加载错误处理
  iframe.onerror = () => {
    console.warn("iframe加载失败:", component.properties.url);
    element.innerHTML = `
      <div style="
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        color: #999;
        font-size: 12px;
        text-align: center;
        padding: 20px;
        width: 100%;
        height: 100%;
      ">
        <div style="font-size: 32px; margin-bottom: 10px;">🌐</div>
        <div>网页加载失败</div>
        <div style="font-size: 10px; margin-top: 4px; word-break: break-all;">${component.properties.url || "未设置URL"}</div>
      </div>
    `;
  };

  // iframe加载成功处理
  iframe.onload = () => {
    console.log("iframe加载成功:", component.properties.url);
  };

  element.appendChild(iframe);

  // 创建透明遮罩层，用于捕获编辑模式下的交互事件
  // 这个遮罩层防止iframe捕获鼠标事件，使得拖拽、缩放、右键菜单等功能正常工作
  const overlay = document.createElement("div");
  overlay.className = "iframe-overlay";
  overlay.style.cssText = `
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: transparent;
    cursor: pointer;
    z-index: 1;
    pointer-events: auto;
  `;

  // 添加一个标识，表明这是编辑模式遮罩
  overlay.setAttribute("data-edit-overlay", "true");

  // 提示信息（可选，鼠标悬停时显示）
  overlay.title = "编辑模式下无法与网页交互，请启动仿真模式";

  element.appendChild(overlay);

  // 存储原始边框样式（用于选中效果）
  element.setAttribute(
    "data-original-border-color",
    component.style?.borderColor || "#e4e7ed"
  );
  element.setAttribute(
    "data-original-border-width",
    (component.style?.borderWidth || 1) + "px"
  );

  // 添加iframe数据属性
  element.setAttribute("data-iframe-url", component.properties.url || "");

  // 设置组件交互
  setupComponentInteractions(element, component);

  canvasContent.appendChild(element);

  return element;
};

/**
 * 创建表格DOM元素
 * @param component - 组件配置
 * @param canvasContent - 画布内容元素
 * @param setupComponentInteractions - 组件交互设置函数
 */
export const createTableElement = (
  component: any,
  canvasContent: Element,
  setupComponentInteractions: (element: HTMLElement, component: any) => void
) => {
  const element = document.createElement("div");
  element.id = component.id;
  element.className = "fuxa-component table-component";

  // 设置容器样式
  element.style.cssText = `
    position: absolute;
    left: ${component.position.x}px;
    top: ${component.position.y}px;
    width: ${component.size.width}px;
    height: ${component.size.height}px;
    border: ${component.style?.borderWidth || 1}px solid ${component.style?.borderColor || "#e4e7ed"};
    border-radius: ${component.style?.borderRadius || 4}px;
    background: ${component.style?.backgroundColor || "#ffffff"};
    overflow: hidden;
    cursor: pointer;
    user-select: none;
    z-index: 10;
  `;

  // 创建表格容器
  const tableContainer = document.createElement("div");
  tableContainer.className = "table-container";
  tableContainer.style.cssText = `
    width: 100%;
    height: 100%;
    padding: 16px;
    overflow: auto;
    box-sizing: border-box;
  `;

  // 获取表格配置
  const tableConfig = component.tableConfig || {};
  const columns = tableConfig.columns || [];

  // 创建表格标题
  if (tableConfig.title && tableConfig.showHeader !== false) {
    const titleElement = document.createElement("div");
    titleElement.className = "table-title";
    titleElement.textContent = tableConfig.title;
    titleElement.style.cssText = `
      font-size: 16px;
      font-weight: 600;
      color: ${tableConfig.headerTextColor || "#606266"};
      margin-bottom: 12px;
      padding: 0 4px;
    `;
    tableContainer.appendChild(titleElement);
  }

  // 创建表格元素
  const table = document.createElement("table");
  table.className = "data-table";
  table.style.cssText = `
    width: 100%;
    border-collapse: collapse;
    font-size: 14px;
    background-color: ${tableConfig.rowBgColor || "#ffffff"};
    ${tableConfig.border ? `border: 1px solid ${tableConfig.borderColor || "#ebeef5"};` : ""}
  `;

  // 创建表头
  if (tableConfig.showHeader !== false) {
    const thead = document.createElement("thead");
    const headerRow = document.createElement("tr");
    headerRow.style.cssText = `
      background-color: ${tableConfig.headerBgColor || "#f5f7fa"};
      color: ${tableConfig.headerTextColor || "#606266"};
    `;

    columns.forEach((column: any) => {
      const th = document.createElement("th");
      th.textContent = column.label || "";
      th.style.cssText = `
        padding: 12px 8px;
        text-align: ${column.align || "left"};
        font-weight: 600;
        ${tableConfig.border ? `border: 1px solid ${tableConfig.borderColor || "#ebeef5"};` : ""}
        ${column.width ? `width: ${column.width}px;` : ""}
      `;
      headerRow.appendChild(th);
    });

    thead.appendChild(headerRow);
    table.appendChild(thead);
  }

  // 创建表体（示例数据）
  const tbody = document.createElement("tbody");
  const sampleData = [
    { id: 1, name: "示例数据 1", status: "正常" },
    { id: 2, name: "示例数据 2", status: "异常" },
    { id: 3, name: "示例数据 3", status: "正常" }
  ];

  sampleData.forEach((row, rowIndex) => {
    const tr = document.createElement("tr");

    // 斑马纹效果
    if (tableConfig.stripe && rowIndex % 2 === 1) {
      tr.style.backgroundColor = tableConfig.stripeBgColor || "#fafafa";
    }

    // 悬停效果
    tr.addEventListener("mouseenter", () => {
      tr.style.backgroundColor = tableConfig.hoverBgColor || "#f5f7fa";
    });

    tr.addEventListener("mouseleave", () => {
      if (tableConfig.stripe && rowIndex % 2 === 1) {
        tr.style.backgroundColor = tableConfig.stripeBgColor || "#fafafa";
      } else {
        tr.style.backgroundColor = tableConfig.rowBgColor || "#ffffff";
      }
    });

    // 高亮当前行
    if (tableConfig.highlightCurrentRow) {
      tr.addEventListener("click", (e) => {
        e.stopPropagation();
        // 移除其他行的高亮
        tbody.querySelectorAll("tr").forEach(r => {
          r.style.backgroundColor = "";
        });
        tr.style.backgroundColor = "#ecf5ff";
      });
    }

    columns.forEach((column: any) => {
      const td = document.createElement("td");
      td.textContent = row[column.prop] || "";
      td.style.cssText = `
        padding: 12px 8px;
        text-align: ${column.align || "left"};
        ${tableConfig.border ? `border: 1px solid ${tableConfig.borderColor || "#ebeef5"};` : ""}
      `;
      tr.appendChild(td);
    });

    tbody.appendChild(tr);
  });

  table.appendChild(tbody);
  tableContainer.appendChild(table);

  // 创建分页（如果启用）
  if (tableConfig.pagination?.enabled) {
    const pagination = document.createElement("div");
    pagination.className = "table-pagination";
    pagination.style.cssText = `
      margin-top: 16px;
      display: flex;
      justify-content: flex-end;
      align-items: center;
      font-size: 12px;
      color: #606266;
    `;
    pagination.textContent = `共 3 条，每页 ${tableConfig.pagination.pageSize || 10} 条`;
    tableContainer.appendChild(pagination);
  }

  element.appendChild(tableContainer);

  // 存储原始边框样式
  element.setAttribute(
    "data-original-border-color",
    component.style?.borderColor || "#e4e7ed"
  );
  element.setAttribute(
    "data-original-border-width",
    (component.style?.borderWidth || 1) + "px"
  );

  // 添加表格数据属性
  element.setAttribute("data-table-config", JSON.stringify(tableConfig));

  // 设置组件交互
  setupComponentInteractions(element, component);

  canvasContent.appendChild(element);

  return element;
};

// ========================================
// 配置保存和数据管理函数
// ========================================

/**
 * 保存图表配置
 */
export const handleSaveChartProperty = (
  config: any,
  currentChartComponent: any,
  initEChartFn: any,
  setupChartDataRefreshFn: any,
  isSaved: any,
  ElMessage: any
) => {
  if (!currentChartComponent) return;

  try {
    currentChartComponent.chartConfig = {
      ...currentChartComponent.chartConfig,
      ...config
    };

    if (config.width) {
      currentChartComponent.size = currentChartComponent.size || {};
      currentChartComponent.size.width = config.width;
      currentChartComponent.width = config.width;
    }
    if (config.height) {
      currentChartComponent.size = currentChartComponent.size || {};
      currentChartComponent.size.height = config.height;
      currentChartComponent.height = config.height;
    }

    const element = document.getElementById(currentChartComponent.id);
    if (element) {
      if (config.width) element.style.width = `${config.width}px`;
      if (config.height) element.style.height = `${config.height}px`;

      const chartContainer = element.querySelector('.chart-container') as HTMLElement;
      if (chartContainer) {
        initEChartFn(chartContainer, currentChartComponent);
      }
    }

    isSaved.value = false;
    ElMessage.success("图表配置已更新");

    if (config.datasetId && config.refreshInterval > 0) {
      setupChartDataRefreshFn(currentChartComponent);
    }
  } catch (error) {
    ElMessage.error("保存图表配置失败: " + (error as Error).message);
  }
};

/**
 * 保存iframe配置
 */
export const handleSaveIframeConfig = (
  config: any,
  currentIframeComponent: any,
  isSaved: any,
  ElMessage: any
) => {
  if (!currentIframeComponent) return;

  try {
    currentIframeComponent.properties = {
      ...currentIframeComponent.properties,
      url: config.url,
      allowFullscreen: config.allowFullscreen,
      sandbox: config.sandbox
    };

    const element = document.getElementById(currentIframeComponent.id);
    if (element) {
      const iframe = element.querySelector('iframe');
      if (iframe) {
        iframe.src = config.url;
        if (config.allowFullscreen) {
          iframe.setAttribute('allowfullscreen', 'true');
        } else {
          iframe.removeAttribute('allowfullscreen');
        }
        if (config.sandbox) {
          iframe.setAttribute('sandbox', config.sandbox);
        } else {
          iframe.removeAttribute('sandbox');
        }
      }
    }

    isSaved.value = false;
    ElMessage.success("iframe配置已更新");
  } catch (error) {
    ElMessage.error("保存iframe配置失败: " + (error as Error).message);
  }
};

/**
 * 保存视频配置
 */
export const handleSaveVideoConfig = (
  config: any,
  currentVideoComponent: any,
  isSaved: any,
  ElMessage: any
) => {
  if (!currentVideoComponent) return;

  try {
    currentVideoComponent.properties = {
      ...currentVideoComponent.properties,
      url: config.url,
      poster: config.poster,
      controls: config.controls,
      autoplay: config.autoplay,
      loop: config.loop,
      muted: config.muted,
      preload: config.preload
    };

    const element = document.getElementById(currentVideoComponent.id);
    if (element) {
      const video = element.querySelector('video');
      if (video) {
        video.src = config.url;
        if (config.poster) {
          video.poster = config.poster;
        } else {
          video.removeAttribute('poster');
        }
        video.controls = config.controls;
        video.autoplay = config.autoplay;
        video.loop = config.loop;
        video.muted = config.muted;
        video.preload = config.preload;
        video.load();
      }
    }

    isSaved.value = false;
    ElMessage.success("视频配置已更新");
  } catch (error) {
    ElMessage.error("保存视频配置失败: " + (error as Error).message);
  }
};

/**
 * 保存摄像头配置
 */
export const handleSaveWebcamConfig = (
  config: any,
  currentWebcamComponent: any,
  isSaved: any,
  ElMessage: any
) => {
  if (!currentWebcamComponent) return;

  try {
    currentWebcamComponent.properties = {
      ...currentWebcamComponent.properties,
      ...config
    };

    isSaved.value = false;
    ElMessage.success("摄像头配置已保存");
  } catch (error) {
    ElMessage.error("保存摄像头配置失败: " + (error as Error).message);
  }
};

/**
 * 保存表格配置
 */
export const handleSaveTableConfig = (
  config: any,
  currentTableComponent: any,
  createTableElementFn: any,
  setupTableDataRefreshFn: any,
  editorContainer: any,
  isSaved: any,
  ElMessage: any
) => {
  if (!currentTableComponent) return;

  try {
    currentTableComponent.tableConfig = {
      ...currentTableComponent.tableConfig,
      ...config
    };

    const element = document.getElementById(currentTableComponent.id);
    if (element) {
      const canvasContent = editorContainer.value?.querySelector(".canvas-content");
      if (canvasContent) {
        element.remove();
        createTableElementFn(currentTableComponent, canvasContent);
      }
    }

    isSaved.value = false;
    ElMessage.success("表格配置已更新");

    if (config.datasetId && config.autoRefresh && config.refreshInterval > 0) {
      setupTableDataRefreshFn(currentTableComponent);
    }
  } catch (error) {
    ElMessage.error("保存表格配置失败: " + (error as Error).message);
  }
};

// ========================================
// 数据刷新和管理函数
// ========================================

/**
 * 设置表格数据刷新
 */
export const setupTableDataRefresh = (
  component: any,
  datasetList: any,
  createTableElementFn: any,
  editorContainer: any
) => {
  if (component._tableRefreshTimer) {
    clearInterval(component._tableRefreshTimer);
  }

  component._tableRefreshTimer = setInterval(() => {
    fetchTableData(component, datasetList).then(data => {
      updateTableData(component, data, createTableElementFn, editorContainer);
    }).catch(error => {});
  }, component.tableConfig.refreshInterval);
};

/**
 * 获取表格数据
 */
export const fetchTableData = async (component: any, datasetList: any) => {
  const config = component.tableConfig;

  if (!config.datasetId) {
    return null;
  }

  const dataset = datasetList.value.find((ds: any) => ds.id === config.datasetId);
  if (!dataset) {
    return null;
  }

  if (dataset.type === 'iot') {
    // IoT点位数据集：一行一个点位的最新值
    const points = await fetchIotLatestPoints(dataset);
    if (!points) return null;
    const metaMap = new Map<string, any>(
      (dataset.points || []).map((p: any) => [p.ParamCode, p])
    );
    return points.map((p: any) => ({
      ParamCode: p.ParamCode,
      ParamName: p.ParamName || metaMap.get(p.ParamCode)?.ParamName || p.ParamCode,
      ParamValue: p.ValueStr ?? p.Value ?? "-",
      ValueUnit: metaMap.get(p.ParamCode)?.ValueUnit || "",
      CollectTime: p.Ts || ""
    }));
  } else if (dataset.type === 'api') {
    return null;
  }

  return null;
};

/**
 * 更新表格数据
 */
export const updateTableData = (
  component: any,
  data: any,
  createTableElementFn: any,
  editorContainer: any
) => {
  if (!data) return;

  component.tableConfig.data = data;

  const element = document.getElementById(component.id);
  if (element) {
    const canvasContent = editorContainer.value?.querySelector(".canvas-content");
    if (canvasContent) {
      element.remove();
      createTableElementFn(component, canvasContent);
    }
  }
};

/**
 * 设置图表数据刷新
 */
export const setupChartDataRefresh = (
  component: any,
  datasetList: any,
  initEChartFn: any
) => {
  if (component._chartRefreshTimer) {
    clearInterval(component._chartRefreshTimer);
  }

  component._chartRefreshTimer = setInterval(() => {
    fetchChartData(component, datasetList).then(data => {
      updateChartData(component, data, initEChartFn);
    }).catch(error => {});
  }, component.chartConfig.refreshInterval);
};

/**
 * 获取图表数据
 */
export const fetchChartData = async (component: any, datasetList: any) => {
  const config = component.chartConfig;

  if (!config.datasetId) {
    return null;
  }

  const dataset = datasetList.value.find((ds: any) => ds.id === config.datasetId);
  if (!dataset) {
    return null;
  }

  if (dataset.type === 'iot') {
    // 历史模式：取第一个勾选点位的曲线({name:时间,value:数值}[])
    if (dataset.mode === 'history' && dataset.points?.length) {
      // 报表运行态的查询条件栏会写入显式区间(queryStart/queryEnd)覆盖"最近N小时"；
      // 未指定时仍按 historyHours 回溯，编辑器与组态运行态行为不变。
      const end = dataset.queryEnd ? new Date(dataset.queryEnd) : new Date();
      const start = dataset.queryStart
        ? new Date(dataset.queryStart)
        : new Date(end.getTime() - (dataset.historyHours || 24) * 3600 * 1000);
      const res = await getDeviceHistory(
        dataset.deviceId,
        dataset.points[0].ParamCode,
        formatLocalTime(start),
        formatLocalTime(end),
        dataset.historyMode || 'auto'
      );
      if (!res.Status) return null;
      const history = JSON.parse(res.Result);
      return (history.Points || [])
        .filter((p: any) => p.Value !== null && p.Value !== undefined)
        .map((p: any) => ({ name: p.Ts, value: p.Value }));
    }
    // 实时模式：每个点位最新值作为一项
    const points = await fetchIotLatestPoints(dataset);
    if (!points) return null;
    return points
      .filter((p: any) => p.Value !== null && p.Value !== undefined)
      .map((p: any) => ({
        name: p.ParamName || p.ParamCode,
        value: p.Value
      }));
  } else if (dataset.type === 'api') {
    return null;
  }

  return null;
};

/**
 * 更新图表数据
 */
export const updateChartData = (
  component: any,
  data: any,
  initEChartFn: any
) => {
  if (!data) return;

  component.chartConfig.staticData = data;

  const element = document.getElementById(component.id);
  if (element) {
    initEChartFn(element, component);
  }
};
