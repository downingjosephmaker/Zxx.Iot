import { ElMessage, ElMessageBox } from "element-plus";
import { nextTick } from "vue";
import { fuxaMqttService } from "../core/fuxaMqttService";
import {
  addResizeHandles,
  removeResizeHandles,
  fuxaResizeHandles
} from "../core/FuxaResizeHandles";
import { pathTool } from '../core/PathTool';
import { initializeButtonComponent } from './utils-button';
import {
  initializeFuxaSliderComponent, createFuxaSliderElement, updateFuxaSliderAppearance,
  initializeGaugeProgressComponent, // 保留初始化函数
  initializeGaugeSemaphoreComponent, createGaugeSemaphoreElement, updateGaugeSemaphoreAppearance,
  initializeHtmlBagComponent, createHtmlBagElement, updateHtmlBagAppearance
} from './utils-fuxa-controls';
import * as echarts from "echarts";
import { componentManager } from "../core/ComponentManager";
import { svgManager, createSvgComponent, cleanupAbnormalSvgElements } from "../core/SvgManager";
import { ledDisplayManager } from "../core/LedDisplayComponent";
import { applySvgContainerBackground } from "./utils2";

// 开始仿真
export const startSimulation = (isSimulating: any) => {
  isSimulating.value = true;

  // 隐藏所有iframe的编辑遮罩层，允许与iframe内容交互
  const iframeOverlays = document.querySelectorAll('.iframe-overlay[data-edit-overlay="true"]');
  iframeOverlays.forEach((overlay: any) => {
    overlay.style.display = 'none';
  });

  ElMessage.info("仿真模式已启动");
};

// 停止仿真
export const stopSimulation = (isSimulating: any) => {
  isSimulating.value = false;

  // 显示所有iframe的编辑遮罩层，恢复编辑功能
  const iframeOverlays = document.querySelectorAll('.iframe-overlay[data-edit-overlay="true"]');
  iframeOverlays.forEach((overlay: any) => {
    overlay.style.display = 'block';
  });

  ElMessage.info("仿真模式已停止");
};

// 从SVG路径中提取组件名称的辅助函数
export const extractComponentNameFromPath = (svgPath: string): string => {
  if (!svgPath) return '';

  // 处理 @/assets/svg/xxx.svg 格式
  if (svgPath.startsWith('@/assets/svg/')) {
    return svgPath.replace('@/assets/svg/', '').replace('.svg', '');
  }

  // 处理其他路径格式
  const fileName = svgPath.split('/').pop() || '';
  return fileName.replace('.svg', '');
};

// 返回项目列表
export const goBack = async (
  isSaved: any,
  saveProject: () => Promise<void>,
  router: any
) => {
  if (!isSaved.value) {
    const result = await ElMessageBox.confirm(
      "当前项目有未保存的修改，是否要保存？",
      "提示",
      {
        confirmButtonText: "保存",
        cancelButtonText: "不保存",
        distinguishCancelAndClose: true,
        type: "warning"
      }
    ).catch(() => "cancel");

    if (result === "confirm") {
      await saveProject();
    }
  }

  router.push({ name: "ScadaProject" });
};

// 处理组件添加
export const handleAddComponent = (
  component: any,
  projectData: any,
  isSaved: any,
  position?: { x: number; y: number }
) => {
  console.log("添加组件到画布:", component, position);

  // 如果没有指定位置，使用随机位置避免重叠
  const finalPosition = position || {
    x: 50 + Math.random() * 200, // 随机位置避免重叠
    y: 50 + Math.random() * 200
  };

  // 创建组件实例
  const componentInstance = createComponentInstance(component, finalPosition);

  // 添加到当前视图
  if (projectData.value) {
    if (!projectData.value.components) {
      projectData.value.components = [];
    }
    projectData.value.components.push(componentInstance);
  }

  isSaved.value = false;
  ElMessage.success(`已添加组件: ${component.title}`);
};

// 处理组件激活模式
export const handleActivateComponent = (
  component: any,
  currentEditorMode: any,
  activeComponent: any,
  setCanvasMode: (mode: string) => void
) => {
  console.log("激活组件模式:", component);

  // 如果当前正在绘制路径，先停止路径绘制
  if (pathTool.isActive()) {
    pathTool.stopDrawing();
    console.log("停止当前路径绘制");
  }

  currentEditorMode.value = component.name;
  activeComponent.value = component;

  // 特殊处理选择工具
  if (component.name === 'select') {
    console.log("激活选择工具");
    setCanvasMode('default');
    return;
  }

  // 特殊处理路径工具
  if (component.name === 'path') {
    console.log("激活路径工具");
    pathTool.startDrawing();
    setCanvasMode('crosshair');
    return;
  }

  // 设置画布为组件创建模式
  setCanvasMode(component.name);
};

// 创建组件实例
export const createComponentInstance = (
  component: any,
  position = { x: 100, y: 100 }
) => {
  // 智能尺寸计算
  const smartSize = calculateSmartSize(component);

  console.log('========== createComponentInstance 调用 ==========');
  console.log('组件名称:', component.name);
  console.log('组件标题:', component.title);
  console.log('传入的位置参数:', position);
  console.log('位置 x:', position.x);
  console.log('位置 y:', position.y);
  console.log('智能尺寸:', smartSize);
  console.log('=======================================');

  // 🔘 为开关组件初始化默认状态
  const componentInstance: any = {
    id: `${component.name}_${Date.now()}`,
    type: component.name,
    name: component.title,
    svgPath: component.svgPath, // 保存SVG路径
    position: position,
    // 为兼容性添加直接坐标属性
    x: position.x,
    y: position.y,
    size: smartSize,
    // 为 DrawingComponent 接口添加直接的 width 和 height 属性
    width: smartSize.width,
    height: smartSize.height,
    properties:
      component.properties?.reduce((props, prop) => {
        props[prop.name] = prop.defaultValue || "";
        return props;
      }, {}) || {},
    style: {
      backgroundType: "solid",  // 默认使用纯色背景
      backgroundColor: "#ffffff",  // 默认白色背景
      borderColor: "#d9d9d9",
      color: "#303133",
      // SVG样式默认配置 - 不设置默认fill颜色，保持原始SVG颜色
      fillType: "solid",
      svgOpacity: 1,
      svgAnimation: "none",
      animationSpeed: "normal",
      svgHoverEffect: false,
      // 🎯 所有组件初始化时默认设置 animationStaticValue = 30
      animationStaticValue: 30
    },
    created: new Date().toISOString()
  };

  // 🔘 如果是开关组件，初始化开关状态（双重存储以确保兼容性）
  if (component.name === 'switch') {
    componentInstance.switchState = false; // 旧位置：默认关闭
    componentInstance.style.switchState = 'off'; // 新位置：默认关闭
    componentInstance.style.switchOnColor = '#67c23a'; // 默认开启颜色
    componentInstance.style.switchOffColor = '#909399'; // 默认关闭颜色
    console.log('🔘 创建开关组件，初始化默认状态为关闭');
  }

  // 🔲 如果是按钮组件，初始化按钮特定属性
  if (component.name === 'button') {
    initializeButtonComponent(componentInstance);
  }

  // 🎚️ 如果是滑块组件，初始化滑块特定属性 (使用 FUXA 版本)
  if (component.name === 'slider') {
    initializeFuxaSliderComponent(componentInstance);

    // 根据方向调整默认尺寸
    const orientation = componentInstance.properties?.options?.orientation || 'vertical';
    if (orientation === 'horizontal') {
      // 水平滑块：宽而矮
      componentInstance.size = { width: 200, height: 80 };
      componentInstance.width = 200;
      componentInstance.height = 80;
    } else {
      // 垂直滑块：窄而高
      componentInstance.size = { width: 80, height: 200 };
      componentInstance.width = 80;
      componentInstance.height = 200;
    }
  }

  // 📊 FUXA 控制组件初始化
  if (component.name === 'progress-v') {
    initializeGaugeProgressComponent(componentInstance);
  }

  if (component.name === 'semaphore') {
    initializeGaugeSemaphoreComponent(componentInstance);
  }

  if (component.name === 'bag') {
    initializeHtmlBagComponent(componentInstance);
  }

  // 🎯 液位罐组件初始化 - circularTankLevel-v2, squareTankLevel-v2
  if (component.name === 'circularTankLevel-v2' || component.name === 'squareTankLevel-v2') {
    componentInstance.style.animationStaticValue = componentInstance.style.animationStaticValue ?? 30; // SVG 默认值
    componentInstance.style.svgAnimation = componentInstance.style.svgAnimation || 'none';
    console.log(`🎯 初始化液位罐组件 [${component.name}]，animationStaticValue:`, componentInstance.style.animationStaticValue);
  }

  // 💡 LED 显示组件初始化
  if (component.name === 'led-display') {
    componentInstance.properties = {
      text: '88:88:88',
      color: '#ff3333',
      backgroundColor: '#0a0a0a',
      fontSize: 42,
      fontFamily: "'Courier New', monospace",
      fontWeight: 'bold',
      glowEffect: true,
      glowIntensity: 'strong',
      multiLine: false,
      lineHeight: 1.2,
      maxLines: 3,
      alignment: 'left',
      padding: 10,
      animation: 'none',
      animationSpeed: 1000,
      sevenSegmentMode: false,
      format: 'none'
    };
    console.log('💡 初始化 LED 显示组件，默认属性:', componentInstance.properties);
  }

  return componentInstance;
};

// 智能尺寸计算函数
export const calculateSmartSize = (component: any) => {
  console.log('calculateSmartSize 调用，组件:', component);
  console.log('CRITICAL: calculateSmartSize被调用 - 这可能是20x20问题的根源!', {
    componentId: component.id,
    name: component.name,
    type: component.type,
    currentWidth: component.width,
    currentHeight: component.height,
    currentSizeWidth: component.size?.width,
    currentSizeHeight: component.size?.height,
    callStack: new Error().stack
  });
  // 预设的组件类型尺寸映射
  const componentSizeMap = {
    // 基础组件
    text: { width: 120, height: 30 },
    label: { width: 100, height: 25 },
    button: { width: 80, height: 32 },
    input: { width: 150, height: 32 },
    switch: { width: 50, height: 24 },
    slider: { width: 80, height: 200 },  // 垂直滑块：留出右侧空间放刻度标签

    // 仪表和图表
    "gauge-circular": { width: 120, height: 120 },
    "gauge-linear": { width: 200, height: 50 },
    "chart-line": { width: 300, height: 200 },
    "chart-bar": { width: 300, height: 200 },
    "chart-pie": { width: 200, height: 200 },
    "trend-chart": { width: 400, height: 150 },

    // 图形组件
    rectangle: { width: 100, height: 60 },
    circle: { width: 80, height: 80 },
    ellipse: { width: 120, height: 80 },
    triangle: { width: 80, height: 80 },
    line: { width: 100, height: 2 },
    polyline: { width: 150, height: 100 },
    polygon: { width: 100, height: 100 },
    path: { width: 120, height: 100 },

    // 工业组件
    valve: { width: 40, height: 40 },
    pump: { width: 50, height: 50 },
    motor: { width: 45, height: 45 },
    tank: { width: 80, height: 100 },
    "pipe-straight": { width: 100, height: 10 },
    "pipe-elbow": { width: 30, height: 30 },
    "pipe-tee": { width: 30, height: 30 },
    sensor: { width: 25, height: 25 },
    alarm: { width: 30, height: 30 },
    indicator: { width: 20, height: 20 },

    // 容器组件
    panel: { width: 200, height: 150 },
    frame: { width: 180, height: 120 },
    group: { width: 160, height: 100 },

    // 图片组件
    "image-upload": { width: 150, height: 150 },
    "image-url": { width: 150, height: 150 },
    "image-symbol": { width: 60, height: 60 },
    "image-background": { width: 200, height: 150 },

    // 时间组件
    "datetime-picker": { width: 200, height: 40 },
    "date-picker": { width: 180, height: 40 },
    "time-picker": { width: 150, height: 40 },
    "datetime-range-picker": { width: 350, height: 40 },
    "digital-clock": { width: 200, height: 60 },

    // 多媒体组件
    video: { width: 400, height: 300 },
    webcam: { width: 400, height: 300 },
    iframe: { width: 400, height: 300 },
    table: { width: 500, height: 300 },
    drawingboard: { width: 800, height: 600 },
    "paint-board": { width: 500, height: 500 },  // 画板组件

    // LED 显示屏组件
    "led-display": { width: 280, height: 80 },

    // 默认尺寸
    default: { width: 60, height: 60 }
  };

  // 根据组件类型确定基础尺寸
  const componentType = component.name || component.type;
  console.log('🔍 calculateSmartSize - componentType:', componentType, 'component:', component);
  let baseSize = componentSizeMap[componentType] || componentSizeMap["default"];
  console.log('🔍 calculateSmartSize - baseSize from map:', baseSize, 'for type:', componentType);

  // 如果组件配置中指定了尺寸，优先使用
  // 但是排除 60x60 这个 FuxaComponentPanel 的占位默认值
  const hasCustomSize = component.width && component.height &&
                        !(component.width === 60 && component.height === 60);

  if (hasCustomSize) {
    console.log('🔍 calculateSmartSize - 检测到组件有自定义尺寸，将覆盖map配置:', {
      'component.width': component.width,
      'component.height': component.height,
      '原baseSize': baseSize,
      '将使用': { width: component.width, height: component.height }
    });
    baseSize = { width: component.width, height: component.height };
  } else {
    if (component.width === 60 && component.height === 60) {
      console.log('🔍 calculateSmartSize - 检测到60x60占位默认值，忽略并使用map配置:', baseSize);
    } else {
      console.log('🔍 calculateSmartSize - 组件没有预设尺寸，使用map配置:', baseSize);
    }
  }

  // 根据组件分类进行额外调整
  const category = component.category || "";
  if (category === "drawing") {
    // 绘图工具通常需要更大的默认尺寸
    baseSize.width = Math.max(baseSize.width, 100);
    baseSize.height = Math.max(baseSize.height, 50);
  } else if (category === "charts") {
    // 图表组件需要更大的尺寸
    baseSize.width = Math.max(baseSize.width, 300);
    baseSize.height = Math.max(baseSize.height, 200);
  } else if (category === "industrial") {
    // 工业组件通常较小但需要保持比例
    const maxDimension = Math.max(baseSize.width, baseSize.height);
    if (maxDimension < 30) {
      const scale = 30 / maxDimension;
      baseSize.width = Math.round(baseSize.width * scale);
      baseSize.height = Math.round(baseSize.height * scale);
    }
  }

  // 根据SVG图标尺寸进行微调
  if (component.svgPath) {
    // 对于有SVG图标的组件，确保最小尺寸以显示图标
    baseSize.width = Math.max(baseSize.width, 40);
    baseSize.height = Math.max(baseSize.height, 40);
  }

  // 根据组件名称长度调整文本类组件宽度
  if (["text", "label", "button"].includes(componentType)) {
    const titleLength = (component.title || component.name || "").length;
    if (titleLength > 0) {
      // 每个字符大约占用8像素，加上内边距
      const estimatedWidth = Math.max(titleLength * 8 + 20, baseSize.width);
      baseSize.width = Math.min(estimatedWidth, 300); // 限制最大宽度
    }
  }

  const finalSize = {
    width: Math.round(baseSize.width),
    height: Math.round(baseSize.height)
  };

  console.log('calculateSmartSize 结果:', finalSize, '组件类型:', componentType);
  return finalSize;
};

// 设置画布模式
export const setCanvasMode = (mode: string, editorContainer: any) => {
  const canvas = editorContainer.value;
  if (canvas) {
    canvas.style.cursor = mode === "select" ? "default" : "crosshair";
  }
};

// 获取模式显示名称
export const getModeDisplayName = (mode: string) => {
  const modeNames = {
    select: "选择",
    label: "文本标签",
    button: "按钮",
    input: "输入框",
    "gauge-circular": "圆形仪表",
    "gauge-linear": "线性仪表",
    "chart-line": "折线图",
    "chart-bar": "柱状图",
    switch: "开关",
    slider: "滑块",
    alarm: "报警灯",
    rectangle: "矩形",
    circle: "圆形",
    line: "直线",
    "pipe-straight": "直管道",
    "pipe-elbow": "弯管道"
  };
  return modeNames[mode] || mode;
};

// 获取当前视图组件数量
export const getCurrentViewComponentCount = (projectData: any) => {
  return projectData.value?.components?.length || 0;
};

// 处理画布拖放
export const handleCanvasDrop = (
  event: DragEvent,
  editorContainer: any,
  canvasZoom: any,
  ElMessage: any,
  createTextComponent: any,
  createComponentInstance: any,
  addComponentToCanvas: any,
  currentEditorMode: any,
  activeComponent: any,
  setCanvasMode: any,
  snapToGrid?: (value: number) => number // 可选的吸附函数
) => {
  event.preventDefault();

  if (!event.dataTransfer) return;

  try {
    const dragData = JSON.parse(event.dataTransfer.getData("application/json"));

    if (dragData.type === "fuxa-component") {
      const rect = (event.target as HTMLElement).getBoundingClientRect();
      const canvasRect = editorContainer.value?.getBoundingClientRect();

      console.log('========== handleCanvasDrop 位置计算 ==========');
      console.log('event.clientX:', event.clientX);
      console.log('event.clientY:', event.clientY);
      console.log('canvasRect:', canvasRect);
      console.log('canvasZoom.value:', canvasZoom.value);

      if (canvasRect) {
        // 计算相对于画布的坐标
        const x = event.clientX - canvasRect.left;
        const y = event.clientY - canvasRect.top;

        console.log('相对位置 x:', x);
        console.log('相对位置 y:', y);

        // 调整缩放比例（不进行 Math.round，让 snapToGrid 来处理精确对齐）
        let scaledX = x / (canvasZoom.value / 100);
        let scaledY = y / (canvasZoom.value / 100);

        console.log('缩放后 scaledX:', scaledX);
        console.log('缩放后 scaledY:', scaledY);

        // 应用吸附功能（如果启用），否则进行四舍五入
        if (snapToGrid) {
          scaledX = snapToGrid(scaledX);
          scaledY = snapToGrid(scaledY);
          console.log('吸附后 scaledX:', scaledX);
          console.log('吸附后 scaledY:', scaledY);
        } else {
          scaledX = Math.round(scaledX);
          scaledY = Math.round(scaledY);
          console.log('四舍五入后 scaledX:', scaledX);
          console.log('四舍五入后 scaledY:', scaledY);
        }

        console.log("✅ 最终计算的画布位置:", {
          x: scaledX,
          y: scaledY,
          component: dragData.component
        });
        console.log('========================================');

        // 特殊处理路径组件 - 不支持拖拽创建，只能通过绘制工具创建
        if (dragData.component.name === 'path') {
          ElMessage.warning('路径组件不支持拖拽创建，请点击工具栏中的路径工具后在画布上绘制');
          return;
        }

        // 特殊处理文本工具 - 使用专门的创建函数
        let componentInstance;
        if (dragData.component.name === 'text') {
          componentInstance = createTextComponent({ x: scaledX, y: scaledY });
        } else {
          // 创建带位置信息的组件实例
          componentInstance = createComponentInstance(dragData.component, {
            x: scaledX,
            y: scaledY
          });
        }

        // 添加到画布
        addComponentToCanvas(componentInstance);

        // 切换回选择模式
        currentEditorMode.value = "select";
        activeComponent.value = null;
        setCanvasMode("select");
      }
    }
  } catch (error) {
    console.error("处理拖放失败:", error);
    ElMessage.error("组件添加失败: " + (error as Error).message);
  }
};

// 添加组件到画布
export const addComponentToCanvas = (
  componentInstance: any,
  projectData: any,
  createComponentElement: any,
  editorContainer: any,
  isSaved: any,
  ElMessage: any,
  cleanupAbnormalSvgElements: any
) => {
  console.log('========== addComponentToCanvas 调用 ==========');
  console.log('接收到的组件实例:', componentInstance);
  console.log('组件类型:', componentInstance.type);
  console.log('组件名称:', componentInstance.name);
  console.log('当前组件总数:', projectData.value?.components?.length || 0);
  console.log('=======================================');

  // 添加到项目数据
  if (projectData.value) {
    if (!projectData.value.components) {
      projectData.value.components = [];
    }
    projectData.value.components.push(componentInstance);
    console.log('✅ 组件已添加到 projectData，新的组件总数:', projectData.value.components.length);
  } else {
    console.error('❌ projectData.value 不存在！');
  }

  // 创建DOM元素
  createComponentElement(componentInstance);

  // 移除拖拽样式
  const canvas = editorContainer.value;
  if (canvas) {
    canvas.classList.remove("drag-over");
  }

  isSaved.value = false;
  ElMessage.success(`已添加组件: ${componentInstance.name}`);

  // 延迟清理异常的SVG元素，防止DOMParser创建的临时元素影响页面
  setTimeout(() => {
    try {
      cleanupAbnormalSvgElements();
    } catch (error) {
      console.warn('清理异常SVG元素时出现警告:', error);
    }
  }, 100);
};

// 创建组件DOM元素
export const createComponentElement = (
  component: any,
  editorContainer: any,
  createPathElement: any,
  createLineElement: any,
  createImageElement: any,
  createIframeElement: any,
  createVideoElement: any,
  createWebcamElement: any,
  createButtonElement: any,
  createTableElement: any,
  createPaintBoardElement: any,
  createChartElement: any,
  setupComponentInteractions: any,
  extractComponentNameFromPath: any,
  applySvgStyles: any,
  applyStyleToElement: any,
  applyTransformToElement: any,
  updateSwitchAppearance: any,
  updateButtonAppearance: any,
  componentManager: any,
  createSvgComponent: any,
  nextTick: any
) => {
  console.log('========== createComponentElement 调用 ==========');
  console.log('接收到的组件:', component);
  console.log('组件类型:', component.type);
  console.log('组件名称:', component.name);
  console.log('============================================');

  const canvasContent = editorContainer.value?.querySelector(".canvas-content");
  if (!canvasContent) {
    console.error('createComponentElement: 无法找到 .canvas-content 元素');
    return;
  }

  // 特殊处理路径组件
  if (component.type === "path") {
    return createPathElement(component, canvasContent);
  }

  // 特殊处理直线组件
  if (component.type === "line") {
    return createLineElement(component, canvasContent);
  }

  // 特殊处理图像组件
  if (component.type === "image" && component.imageData) {
    return createImageElement(component, canvasContent);
  }

  // 特殊处理iframe组件
  if (component.type === "iframe") {
    return createIframeElement(component, canvasContent);
  }

  // 特殊处理视频组件
  if (component.type === "video") {
    return createVideoElement(component, canvasContent);
  }

  // 特殊处理摄像头组件
  if (component.type === "webcam") {
    return createWebcamElement(component, canvasContent);
  }

  // 特殊处理画板组件
  if (component.type === "paint-board" || component.type === "drawing-board") {
    return createPaintBoardElement(component, canvasContent);
  }

  // 特殊处理按钮组件
  if (component.type === "button") {
    console.log('检测到按钮组件:', component.type, component.name);
    // createButtonElement 通过参数传入
    const buttonElement = createButtonElement(component, canvasContent, setupComponentInteractions);

    // 设置按钮的初始外观
    if (updateButtonAppearance) {
      setTimeout(() => {
        updateButtonAppearance(component, buttonElement);
      }, 0);
    }

    return buttonElement;
  }

  // 温度计组件使用通用的SVG加载流程，不需要特殊处理
  // SvgManager 会自动检测 #waterShape 元素并应用液体动画

  // 特殊处理 Value 组件 - 文本值显示
  if (component.type === "value") {
    console.log('📊 检测到 Value 组件:', component.type, component.name);
    const valueElement = createValueElement(component, canvasContent, setupComponentInteractions);

    setTimeout(() => {
      updateValueAppearance(component, valueElement);
    }, 0);

    return valueElement;
  }

  // 特殊处理 HtmlInput 组件 - 可编辑输入框 (editvalue)
  if (component.type === "editvalue") {
    console.log('📝 检测到 HtmlInput 组件:', component.type, component.name);
    const inputElement = createHtmlInputElement(component, canvasContent, setupComponentInteractions);

    setTimeout(() => {
      updateHtmlInputAppearance(component, inputElement);
    }, 0);

    return inputElement;
  }

  // 特殊处理 HtmlSelect 组件 - 下拉选择框 (selectvalue)
  if (component.type === "selectvalue") {
    console.log('📋 检测到 HtmlSelect 组件:', component.type, component.name);
    const selectElement = createHtmlSelectElement(component, canvasContent, setupComponentInteractions);

    setTimeout(() => {
      updateHtmlSelectAppearance(component, selectElement);
    }, 0);

    return selectElement;
  }

  // 特殊处理 GaugeProgress 组件 - 垂直进度条 (progress-v)
  // 🎯 现在使用 SvgManager 来渲染进度条SVG和处理动画
  if (component.type === "progress-v") {
    console.log('📊 检测到 GaugeProgress 组件 (使用SvgManager):', component.type, component.name);

    // 创建主容器
    const container = document.createElement('div');
    container.id = component.id;
    container.className = 'fuxa-component gaugeprogress-component';
    container.style.cssText = `
      position: absolute;
      left: ${component.position.x}px;
      top: ${component.position.y}px;
      width: ${component.size.width}px;
      height: ${component.size.height}px;
    `;
    (container as any).__componentRef = component;

    // 使用 SvgManager 创建 SVG 元素
    // 构建 SvgRenderOptions
    const svgOptions: any = {
      animation: component.style?.svgAnimation || 'none',
      animationSpeed: component.style?.animationSpeed || 'normal',
      animationDuration: component.style?.animationDuration,
      animationIterationCount: component.style?.animationIterationCount || 'infinite',
      animationStaticValue: component.style?.animationStaticValue || 100,
      strokeColor: component.style?.borderColor,
      strokeWidth: component.style?.borderWidth,
      opacity: component.style?.opacity
    };

    console.log('🎯 SvgManager 渲染选项:', svgOptions);

    // 使用 SvgManager 创建内联 SVG
    const svgContainer = svgManager.createInlineSvg('progress-v', svgOptions);
    container.appendChild(svgContainer);

    setupComponentInteractions(container, component);
    canvasContent.appendChild(container);

    return container;
  }

  // 特殊处理 GaugeSemaphore 组件 - 信号灯 (semaphore)
  if (component.type === "semaphore") {
    console.log('🚦 检测到 GaugeSemaphore 组件:', component.type, component.name);
    const semaphoreElement = createGaugeSemaphoreElement(component, canvasContent, setupComponentInteractions);

    setTimeout(() => {
      updateGaugeSemaphoreAppearance(component, semaphoreElement);
    }, 0);

    return semaphoreElement;
  }

  // 特殊处理 HtmlBag 组件 - 仪表盘 (bag)
  if (component.type === "bag") {
    console.log('🎯 检测到 HtmlBag 组件:', component.type, component.name);
    const bagElement = createHtmlBagElement(component, canvasContent, setupComponentInteractions);

    setTimeout(() => {
      updateHtmlBagAppearance(component, bagElement);
    }, 0);

    return bagElement;
  }

  // 特殊处理表格组件 - 检查tableConfig是否存在来判断是否为表格组件
  if (component.tableConfig || component.type === "table") {
    console.log('检测到表格组件 (通过tableConfig或type):', component.type, component.name);
    return createTableElement(component, canvasContent);
  }

  // 特殊处理图表组件 - 检查chartConfig是否存在来判断是否为图表组件
  if (component.chartConfig) {
    console.log('检测到图表组件 (通过chartConfig):', component.type, component.name);
    return createChartElement(component, canvasContent);
  }

  // 特殊处理 LED 显示屏组件
  if (component.type === "led-display") {
    console.log('💡 检测到 LED 显示屏组件:', component.type, component.name);

    // 创建容器元素
    const container = document.createElement('div');
    container.id = component.id;
    container.className = 'fuxa-component led-display-component';
    container.style.cssText = `
      position: absolute;
      left: ${component.position.x}px;
      top: ${component.position.y}px;
      width: ${component.size.width}px;
      height: ${component.size.height}px;
    `;

    // 加载 SVG
    const svgContainer = createSvgComponent('led-display', {
      width: component.size.width,
      height: component.size.height
    });
    container.appendChild(svgContainer);

    // 先添加到画布,确保 SVG 在 DOM 中
    setupComponentInteractions(container, component);
    canvasContent.appendChild(container);

    // 等待 SVG 渲染完成后再初始化 - 使用重试机制
    console.log('💡 等待 SVG 渲染...');

    const initializeLedDisplay = (retryCount = 0) => {
      const svgElement = container.querySelector('svg');
      console.log(`💡 查找 SVG 元素 (尝试 ${retryCount + 1}/5):`, svgElement);

      if (svgElement) {
        console.log('💡 SVG 元素已渲染,初始化 LED 管理器');
        try {
          ledDisplayManager.createLedDisplayComponent(component, container);
          console.log('💡 LED 管理器初始化完成');
        } catch (error) {
          console.error('❌ LED 管理器初始化失败:', error);
        }
      } else if (retryCount < 4) {
        // 最多重试 5 次，每次延迟递增
        console.log(`⏳ SVG 未找到，${50 * (retryCount + 2)}ms 后重试...`);
        setTimeout(() => initializeLedDisplay(retryCount + 1), 50 * (retryCount + 2));
      } else {
        console.error('❌ 重试 5 次后仍找不到 SVG 元素!');
      }
    };

    // 首次尝试使用较短延迟
    setTimeout(() => initializeLedDisplay(0), 100);

    console.log('✅ LED 显示屏组件创建完成');
    return container;
  }

  // 特殊处理文本卡片组件
  if (component.type === "text-card") {
    console.log('📝 检测到文本卡片组件:', component.type, component.name);

    // 直接使用 ComponentManager 创建文本卡片
    const textCardElement = componentManager.createComponent(component, canvasContent);

    console.log('📝 文本卡片组件创建完成:', textCardElement);
    setupComponentInteractions(textCardElement, component);
    return textCardElement;
  }

  console.log('createComponentElement 检查绘图工具组件...');
  console.log('componentManager 对象:', componentManager);
  console.log('isDrawingToolComponent 方法存在:', !!componentManager.isDrawingToolComponent);

  // 检查是否为绘图工具组件
  if (
    componentManager.isDrawingToolComponent &&
    componentManager.isDrawingToolComponent(component.type)
  ) {
    console.log('createComponentElement 检测到绘图工具组件:', component.type);
    console.log('createComponentElement 传递给管理器的组件数据:', component);
    console.log('createComponentElement 画布容器信息:', {
      width: canvasContent.offsetWidth,
      height: canvasContent.offsetHeight,
      scrollWidth: canvasContent.scrollWidth,
      scrollHeight: canvasContent.scrollHeight
    });

    // 使用绘图管理器创建绘图组件
    const drawingElement = componentManager.createComponent(
      component,
      canvasContent
    );

    console.log('createComponentElement 绘图元素创建完成:', {
      elementId: drawingElement?.id,
      width: drawingElement?.offsetWidth,
      height: drawingElement?.offsetHeight,
      parentWidth: drawingElement?.parentElement?.offsetWidth,
      parentHeight: drawingElement?.parentElement?.offsetHeight
    });

    setupComponentInteractions(drawingElement, component);
    return drawingElement;
  } else {
    console.log('createComponentElement 不是绘图工具组件，使用默认处理');
    if (componentManager.isDrawingToolComponent) {
      console.log('isDrawingToolComponent 返回:', componentManager.isDrawingToolComponent(component.type));
    } else {
      console.log('isDrawingToolComponent 方法不存在');
    }
  }

  // 原有的FUXA组件创建逻辑
  const element = document.createElement("div");
  element.id = component.id;
  element.className = "fuxa-component";

  // 设置基础样式
  element.style.cssText = `
    position: absolute;
    left: ${component.position.x}px;
    top: ${component.position.y}px;
    width: ${component.size.width}px;
    height: ${component.size.height}px;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    user-select: none;
    z-index: 10;
    padding: 0;
    box-sizing: border-box;
  `;

  // 存储原始边框样式
  element.setAttribute(
    "data-original-border-color",
    component.style?.borderColor || "#e4e7ed"
  );
  element.setAttribute(
    "data-original-border-width",
    (component.style?.borderWidth || 1) + "px"
  );

  // 应用阴影效果
  if (component.style?.boxShadow) {
    element.style.boxShadow = component.style.boxShadow;
  }

  // SVG图标处理 - 使用内联SVG渲染
  if (component.svgPath || component.name) {
    try {
      // 获取组件名称用于SVG管理器 - 确保使用英文名而不是中文标题
      const componentName = component.type || component.name || extractComponentNameFromPath(component.svgPath);

      // 使用SVG管理器创建内联SVG
      const svgContainer = createSvgComponent(componentName, {
        width: component.size.width,
        height: component.size.height,
        fillColor: component.properties?.color || component.properties?.fillColor,
        strokeColor: component.properties?.strokeColor || component.properties?.borderColor,
        strokeWidth: component.properties?.strokeWidth || component.properties?.borderWidth,
        opacity: component.properties?.opacity,
        animation: component.properties?.animation || 'none',
        animationSpeed: component.properties?.animationSpeed || 'normal',
        hoverEffect: component.properties?.hoverEffect || false
      });

      // 设置容器样式
      svgContainer.style.cssText = `
        width: 100%;
        height: 100%;
        pointer-events: none;
        display: flex;
        align-items: center;
        justify-content: center;
      `;

      element.appendChild(svgContainer);
    } catch (error) {
      console.warn("组件SVG创建失败:", component.svgPath || component.name, error);
      // 显示默认图标
      element.innerHTML = `<div style="font-size: ${Math.min(component.size.width, component.size.height) * 0.6}px; color: #999; display: flex; align-items: center; justify-content: center; width: 100%; height: 100%;">⛭</div>`;
    }
  } else {
    // 如果没有SVG路径，显示默认图标
    element.innerHTML = `<div style="font-size: ${Math.min(component.size.width, component.size.height) * 0.6}px; color: #999; display: flex; align-items: center; justify-content: center; width: 100%; height: 100%;">⛭</div>`;
  }

  // 设置组件交互
  setupComponentInteractions(element, component);

  canvasContent.appendChild(element);

  // 立即应用完整的样式
  nextTick(() => {
    applyStyleToElement(component);
    applyTransformToElement(component);
  });

  // 如果有SVG图标，应用默认样式
  if (component.svgPath) {
    setTimeout(() => {
      applySvgStyles(element, component);

      // 开关组件的初始状态设置
      if (component.type === "switch") {
        updateSwitchAppearance(component, element);
      }
    }, 100); // 延迟应用，确保img元素已加载
  }
};

// 应用SVG图标样式到组件元素
export const applySvgStyles = (element: HTMLElement, component: any) => {
  console.log('applySvgStyles调用:', {
    componentId: component.id,
    hasSvgPath: !!component.svgPath,
    svgPath: component.svgPath,
    elementId: element.id,
    componentType: component.type,
    style: component.style
  });

  if (!component.svgPath) {
    console.log('没有svgPath，跳过SVG样式应用');
    return;
  }

  // 🔘 如果是开关组件，跳过 SVG 样式应用
  // 开关组件的样式完全由 switchState 控制，不应该被通用的 SVG 样式覆盖
  if (component.type === 'switch') {
    console.log('🔘 开关组件，跳过通用SVG样式应用（样式由switchState控制）');
    return;
  }

  // 查找SVG容器和实际的SVG元素
  const svgContainer = element.querySelector(".svg-container");
  const svgElement = element.querySelector("svg");

  console.log('查找SVG元素:', {
    svgContainerFound: !!svgContainer,
    svgElementFound: !!svgElement,
    svgContainerChildren: svgContainer?.children.length || 0,
    svgElementTagName: svgElement?.tagName
  });

  if (!svgElement) {
    console.log('未找到SVG元素，跳过样式应用');
    return;
  }

  // 获取组件的SVG样式配置 - 优先保持原始SVG颜色
  const fillType = component.style?.fillType || "solid";
  // 只有当用户明确设置了fill颜色时才使用，否则保持原始SVG颜色
  const svgColor = component.style?.fill || component.style?.svgColor;
  const fillOpacity = component.style?.fillOpacity || 1;
  const svgOpacity = component.style?.svgOpacity || 1;

  // 如果用户没有设置任何fill颜色，保持原始SVG的颜色，不做任何修改
  const shouldPreserveOriginalColors = !svgColor && fillType === "solid";

  console.log('SVG样式配置:', {
    fillType,
    svgColor,
    fillOpacity,
    svgOpacity,
    fill: component.style?.fill,
    svgColorLegacy: component.style?.svgColor
  });

  // 设置整个SVG的透明度
  svgElement.style.opacity = svgOpacity.toString();

  // 查找SVG内部的所有可填充元素
  const fillableElements = svgElement.querySelectorAll('path, circle, rect, ellipse, polygon, polyline');
  console.log('找到的可填充SVG元素数量:', fillableElements.length);

  // 创建渐变定义函数
  const createGradientDefs = (svgEl: SVGSVGElement, gradientId: string, type: 'linear' | 'radial', startColor: string, endColor: string, angle?: number) => {
    let defs = svgEl.querySelector('defs');
    if (!defs) {
      defs = document.createElementNS('http://www.w3.org/2000/svg', 'defs');
      svgEl.insertBefore(defs, svgEl.firstChild);
    }

    // 移除现有的同名渐变
    const existingGradient = defs.querySelector(`#${gradientId}`);
    if (existingGradient) {
      existingGradient.remove();
    }

    let gradient;
    if (type === 'linear') {
      gradient = document.createElementNS('http://www.w3.org/2000/svg', 'linearGradient');
      gradient.setAttribute('id', gradientId);

      // 设置渐变方向（基于角度）
      const radians = ((angle || 0) * Math.PI) / 180;
      const x2 = Math.cos(radians);
      const y2 = Math.sin(radians);

      gradient.setAttribute('x1', '0%');
      gradient.setAttribute('y1', '0%');
      gradient.setAttribute('x2', `${(x2 + 1) * 50}%`);
      gradient.setAttribute('y2', `${(y2 + 1) * 50}%`);
    } else {
      gradient = document.createElementNS('http://www.w3.org/2000/svg', 'radialGradient');
      gradient.setAttribute('id', gradientId);
      gradient.setAttribute('cx', '50%');
      gradient.setAttribute('cy', '50%');
      gradient.setAttribute('r', '50%');
    }

    // 创建渐变停止点
    const stop1 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
    stop1.setAttribute('offset', '0%');
    stop1.setAttribute('stop-color', startColor);
    stop1.setAttribute('stop-opacity', fillOpacity.toString());

    const stop2 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
    stop2.setAttribute('offset', '100%');
    stop2.setAttribute('stop-color', endColor);
    stop2.setAttribute('stop-opacity', fillOpacity.toString());

    gradient.appendChild(stop1);
    gradient.appendChild(stop2);
    defs.appendChild(gradient);

    return `url(#${gradientId})`;
  };

  // 根据fillType设置SVG样式 - 使用style属性确保优先级
  switch (fillType) {
    case "transparent":
      console.log('应用透明填充样式');
      fillableElements.forEach((el) => {
        const element = el as SVGElement;
        element.style.fill = 'none';
        element.style.fillOpacity = '0';
      });
      break;

    case "solid":
      if (shouldPreserveOriginalColors) {
        console.log('保持原始SVG颜色，不做任何修改');
        // 不修改任何颜色，保持原始SVG文件的颜色设计
        break;
      }

      console.log('应用用户设置的纯色填充样式:', svgColor);
      if (svgColor && svgColor !== 'transparent') {
        fillableElements.forEach((el) => {
          const element = el as SVGElement;
          element.style.fill = svgColor;
          element.style.fillOpacity = fillOpacity.toString();
        });
      } else {
        fillableElements.forEach((el) => {
          const element = el as SVGElement;
          element.style.fill = 'currentColor';
          element.style.fillOpacity = fillOpacity.toString();
        });
      }
      break;

    case "linear-gradient":
      console.log('应用线性渐变填充样式');
      const gradientStart = component.style?.fillGradientStart || "#409eff";
      const gradientEnd = component.style?.fillGradientEnd || "#67c23a";
      const gradientAngle = component.style?.fillGradientAngle || 0;

      console.log('线性渐变参数:', { gradientStart, gradientEnd, gradientAngle });

      const linearGradientUrl = createGradientDefs(
        svgElement as SVGSVGElement,
        'linearGradient_' + component.id,
        'linear',
        gradientStart,
        gradientEnd,
        gradientAngle
      );

      fillableElements.forEach((el) => {
        const element = el as SVGElement;
        element.style.fill = linearGradientUrl;
        element.style.fillOpacity = '1'; // 渐变通过stop-opacity控制透明度
      });
      break;

    case "radial-gradient":
      console.log('应用径向渐变填充样式');
      const radialGradientStart = component.style?.fillGradientStart || "#409eff";
      const radialGradientEnd = component.style?.fillGradientEnd || "#67c23a";

      console.log('径向渐变参数:', { radialGradientStart, radialGradientEnd });

      const radialGradientUrl = createGradientDefs(
        svgElement as SVGSVGElement,
        'radialGradient_' + component.id,
        'radial',
        radialGradientStart,
        radialGradientEnd
      );

      fillableElements.forEach((el) => {
        const element = el as SVGElement;
        element.style.fill = radialGradientUrl;
        element.style.fillOpacity = '1'; // 渐变通过stop-opacity控制透明度
      });
      break;

    default:
      console.log('默认样式 - 应用当前颜色');
      fillableElements.forEach((el) => {
        const element = el as SVGElement;
        element.style.fill = 'currentColor';
        element.style.fillOpacity = fillOpacity.toString();
      });
  }

  // 处理SVG动画效果
  const svgAnimation = component.style?.svgAnimation || "none";
  const animationSpeed = component.style?.animationSpeed || "normal";

  // 清除之前的动画类
  svgElement.classList.remove(
    "svg-animate-rotate",
    "svg-animate-rotate-slow",
    "svg-animate-rotate-fast",
    "svg-animate-pulse",
    "svg-animate-pulse-slow",
    "svg-animate-pulse-fast",
    "svg-animate-blink",
    "svg-animate-blink-slow",
    "svg-animate-blink-fast",
    "svg-animate-bounce",
    "svg-animate-bounce-slow",
    "svg-animate-bounce-fast",
    "svg-animate-shake",
    "svg-animate-shake-slow",
    "svg-animate-shake-fast",
    "svg-hover-effect"
  );

  // 应用动画效果
  if (svgAnimation !== "none") {
    const speedClass =
      animationSpeed === "slow"
        ? "-slow"
        : animationSpeed === "fast"
          ? "-fast"
          : "";
    svgElement.classList.add(`svg-animate-${svgAnimation}${speedClass}`);
  }

  // 应用悬停效果
  const svgHoverEffect = component.style?.svgHoverEffect || false;
  if (svgHoverEffect) {
    svgElement.classList.add("svg-hover-effect");
  }

  console.log('SVG样式应用完成:', {
    fillType,
    fillableElementsCount: fillableElements.length,
    hasAnimation: svgAnimation !== "none",
    hasHoverEffect: svgHoverEffect,
    appliedColor: svgColor,
    elementId: element.id,
    componentType: component.type
  });
};

// 监听选中组件变化，更新表单数据
export const updateFormData = (selectedCanvasComponent: any, componentFormData: any) => {
  if (selectedCanvasComponent.value) {
    const component = selectedCanvasComponent.value;
    console.log("更新表单数据:", component);

    // 调试：检查尺寸数据来源
    console.log("尺寸数据检查:", {
      componentId: component.id,
      originalSizeWidth: component.size?.width,
      originalWidth: component.width,
      originalSizeHeight: component.size?.height,
      originalHeight: component.height,
      willUseWidth: component.size?.width || 60,
      willUseHeight: component.size?.height || 60
    });

    componentFormData.value = {
      id: component.id || "",
      name: component.name || component.title || "",
      type: component.type || "",
      position: {
        x: component.position?.x || 0,
        y: component.position?.y || 0
      },
      size: {
        width: component.size?.width || 60,
        height: component.size?.height || 60
      },
      style: {
        // 背景样式 - 完整保留所有背景相关属性
        backgroundType: component.style?.backgroundType || "solid",
        backgroundColor: component.style?.backgroundColor || "transparent",
        gradientStart: component.style?.gradientStart,
        gradientEnd: component.style?.gradientEnd,
        gradientAngle: component.style?.gradientAngle,
        gradientShape: component.style?.gradientShape,
        backgroundImage: component.style?.backgroundImage,
        backgroundRepeat: component.style?.backgroundRepeat,
        backgroundSize: component.style?.backgroundSize,
        backgroundPosition: component.style?.backgroundPosition,

        // 边框样式 - 完整保留所有边框相关属性
        borderStyle: component.style?.borderStyle || "none",
        borderColor: component.style?.borderColor || "#d9d9d9",
        borderWidth: component.style?.borderWidth || 1,
        borderRadius: component.style?.borderRadius || 0,

        // 文本颜色和透明度
        color: component.style?.color || "#303133",
        opacity: component.style?.opacity || 1,

        // 阴影效果 - 完整保留所有阴影相关属性
        enableShadow: component.style?.enableShadow || false,
        shadowType: component.style?.shadowType || "box",
        shadowColor: component.style?.shadowColor,
        shadowOffsetX: component.style?.shadowOffsetX,
        shadowOffsetY: component.style?.shadowOffsetY,
        shadowBlur: component.style?.shadowBlur,
        shadowSpread: component.style?.shadowSpread,
        shadowInset: component.style?.shadowInset,
        boxShadow: component.style?.boxShadow || "",

        // 视觉效果滤镜
        blur: component.style?.blur,
        brightness: component.style?.brightness,
        contrast: component.style?.contrast,
        saturate: component.style?.saturate,
        hueRotate: component.style?.hueRotate,
        invert: component.style?.invert,
        sepia: component.style?.sepia,
        grayscale: component.style?.grayscale,

        // SVG样式属性 - 不设置默认fill颜色，保持原始SVG颜色
        fillType: component.style?.fillType || "solid",
        fill: component.style?.fill, // 不设置默认值，保持用户设置或undefined
        svgColor: component.style?.svgColor, // 不设置默认值，保持用户设置或undefined
        fillOpacity: component.style?.fillOpacity,
        svgOpacity: component.style?.svgOpacity || 1,
        stroke: component.style?.stroke,
        strokeWidth: component.style?.strokeWidth,
        strokeOpacity: component.style?.strokeOpacity,
        strokeDasharray: component.style?.strokeDasharray,
        strokeLinecap: component.style?.strokeLinecap,
        strokeLinejoin: component.style?.strokeLinejoin,
        fillGradientStart: component.style?.fillGradientStart,
        fillGradientEnd: component.style?.fillGradientEnd,
        fillGradientAngle: component.style?.fillGradientAngle,
        fillGradientShape: component.style?.fillGradientShape,
        svgStyleEnabled: component.style?.svgStyleEnabled,
        enableDropShadow: component.style?.enableDropShadow,
        dropShadowColor: component.style?.dropShadowColor,
        dropShadowOffsetX: component.style?.dropShadowOffsetX,
        dropShadowOffsetY: component.style?.dropShadowOffsetY,
        dropShadowBlur: component.style?.dropShadowBlur,
        svgBlur: component.style?.svgBlur,
        svgAnimation: component.style?.svgAnimation || "none",
        animationSpeed: component.style?.animationSpeed || "normal",
        svgHoverEffect: component.style?.svgHoverEffect || false,
        // 🎯 动画相关属性 - 完整保留
        animationDuration: component.style?.animationDuration,
        animationIterationCount: component.style?.animationIterationCount,
        animationStaticValue: component.style?.animationStaticValue,
        animationTimingFunction: component.style?.animationTimingFunction,
        animationDelay: component.style?.animationDelay,
        animationPlayStateOnHover: component.style?.animationPlayStateOnHover,
        // 🌊 管道流动方向
        pipeFlowDirection: component.style?.pipeFlowDirection,
        // 🔘 开关组件特有属性
        switchState: component.style?.switchState,
        switchOnColor: component.style?.switchOnColor,
        switchOffColor: component.style?.switchOffColor
      },
      // 变换属性
      rotation: component.rotation || 0,
      scale: component.scale || 1,
      flipHorizontal: component.flipHorizontal || false,
      flipVertical: component.flipVertical || false,
      lockAspectRatio: component.lockAspectRatio || false,
      // 交互属性
      clickable: component.clickable || false,
      hoverable: component.hoverable || false,
      longPress: component.longPress || false,
      doubleClick: component.doubleClick || false,
      bindVariable: component.bindVariable || "",
      updateRate: component.updateRate || "normal",
      dataFormat: component.dataFormat || "",
      visibilityCondition: component.visibilityCondition || "",
      enableCondition: component.enableCondition || "",
      requiredPermission: component.requiredPermission || "none",
      userGroups: component.userGroups || "",
      // 形状属性
      strokeStyle: component.strokeStyle || "solid",
      dashArray: component.dashArray || 5,
      lineCap: component.lineCap || "round",
      fillType: component.fillType || "solid",
      gradientStart: component.gradientStart || "#409eff",
      gradientEnd: component.gradientEnd || "#67c23a",
      gradientAngle: component.gradientAngle || 0,
      enableShadow: component.enableShadow || false,
      shadowColor: component.shadowColor || "#00000040",
      shadowOffsetX: component.shadowOffsetX || 2,
      shadowOffsetY: component.shadowOffsetY || 2,
      shadowBlur: component.shadowBlur || 4,
      blur: component.blur || 0,
      brightness: component.brightness || 1,
      contrast: component.contrast || 1,
      text: component.text || component.properties?.text || "",
      fontSize: component.fontSize || component.properties?.fontSize || 14,
      fontWeight: component.fontWeight || component.properties?.fontWeight || "normal",
      color: component.color || component.properties?.color || "#303133",
      textAlign: component.textAlign || component.properties?.textAlign || "center",
      verticalAlign: component.verticalAlign || component.properties?.verticalAlign || "middle",
      textDecoration: component.textDecoration || component.properties?.textDecoration || "none",
      properties: { ...component.properties } || {},
      events: [...(component.events || [])],
      // ComponentBinding配置
      componentBinding: component.componentBinding || null,
      deviceId: component.componentBinding?.deviceId || "",
      paramcode: component.componentBinding?.paramcode || "",
      targetProperty: component.componentBinding?.targetProperty || "text",
      bindingMode: component.componentBinding
        ? component.componentBinding.directMapping
          ? component.componentBinding.conditions
            ? "hybrid"
            : "direct"
          : "conditional"
        : "direct",
      valueTransform: component.componentBinding?.valueTransform || "",
      conditions: component.componentBinding?.conditions || [],
      actions: component.componentBinding?.actions || [],
      previewData: null
    };

    console.log("表单数据已更新:", componentFormData.value);
  } else {
    console.log("没有选中组件，清空表单数据");
    componentFormData.value = {
      id: "",
      name: "",
      type: "",
      position: { x: 0, y: 0 },
      size: { width: 0, height: 0 },
      style: {
        backgroundColor: "transparent",
        borderColor: "#d9d9d9",
        borderWidth: 1,
        borderRadius: 0,
        color: "#303133",
        opacity: 1,
        boxShadow: ""
      },
      text: "",
      fontSize: 14,
      fontWeight: "normal",
      textAlign: "center",
      verticalAlign: "middle",
      textDecoration: "none",
      properties: {},
      events: [],
      // ComponentBinding数据结构
      componentBinding: null,
      deviceId: "",
      paramcode: "",
      targetProperty: "text",
      bindingMode: "direct",
      valueTransform: "",
      conditions: [],
      actions: [],
      previewData: null
    };
  }
};

// 应用样式到DOM元素的方法
export const applyStyleToElement = (component: any) => {
  const element = document.getElementById(component.id);

  console.log('applyStyleToElement 调用:', {
    componentId: component.id,
    componentType: component.type,
    elementExists: !!element,
    elementTagName: element?.tagName,
    elementClasses: element?.className,
    hasStyle: !!component.style,
    style: component.style
  });

  if (!element) {
    console.error('DOM元素未找到:', component.id);
    return;
  }

  if (!component.style) {
    console.warn('组件样式为空:', component.id);
    return;
  }

  // text-card 组件特殊处理 - 使用 TextCardComponent 管理器
  if (component.type === 'text-card') {
    console.log('📝 text-card 组件样式更新,调用 TextCardComponent');

    // 从 style 中提取外观样式配置
    const style = component.style;
    const textCardConfig = {
      content: component.text || '',
      multiLine: component.properties?.textCardConfig?.multiLine !== false,
      maxLines: component.properties?.textCardConfig?.maxLines || 10,
      lineHeight: component.properties?.textCardConfig?.lineHeight || 1.6,

      // 背景样式 (支持纯色和渐变)
      backgroundType: style.backgroundType || 'solid',
      backgroundColor: style.backgroundColor || '#ffffff',
      gradientStartColor: style.gradientStartColor || '#409eff',
      gradientEndColor: style.gradientEndColor || '#67c23a',
      gradientAngle: style.gradientAngle !== undefined ? style.gradientAngle : 0,

      // 边框样式 (支持实线、虚线、点线等)
      borderStyle: style.borderStyle || 'solid',
      borderColor: style.borderColor || '#d1d5db',
      borderWidth: style.borderWidth !== undefined ? style.borderWidth : 1,
      borderRadius: style.borderRadius !== undefined ? style.borderRadius : 4,

      // 阴影样式
      shadow: {
        enabled: style.enableShadow || false,
        color: style.shadowColor || 'rgba(0,0,0,0.1)',
        offsetX: style.shadowOffsetX !== undefined ? style.shadowOffsetX : 2,
        offsetY: style.shadowOffsetY !== undefined ? style.shadowOffsetY : 2,
        blur: style.shadowBlur !== undefined ? style.shadowBlur : 4
      },

      // 文本样式
      textStyle: {
        fontFamily: component.fontFamily || "'Microsoft YaHei', sans-serif",
        fontSize: component.fontSize || 14,
        fontWeight: component.fontWeight || 'normal',
        color: component.color || '#333333',
        textAlign: component.textAlign || 'left',
        verticalAlign: component.verticalAlign || 'top',
        textDecoration: component.textDecoration || 'none'
      },

      // 布局
      padding: style.padding !== undefined ? style.padding : 16
    };

    // 调用 TextCardComponent 管理器更新
    import('../core/TextCardComponent').then(({ textCardManager }) => {
      textCardManager.updateTextCardComponent(component.id, textCardConfig);
    });

    return; // 跳过通用的样式应用
  }

  const style = component.style;

  console.log('应用样式到组件:', {
    componentId: component.id,
    backgroundType: style.backgroundType,
    backgroundColor: style.backgroundColor,
    style: style
  });

  // 应用背景样式 - 根据backgroundType决定处理方式
  const backgroundType = style.backgroundType || 'solid';

  console.log('背景类型处理开始:', {
    backgroundType: backgroundType,
    originalBackgroundType: style.backgroundType,
    isUndefined: style.backgroundType === undefined,
    styleKeys: Object.keys(style)
  });

  // 找到 svg-container 元素
  const svgContainer = element.querySelector('.svg-container');

  console.log('背景样式目标元素:', {
    mainElement: element.tagName + '#' + element.id,
    svgContainer: svgContainer ? (svgContainer.tagName + '.' + svgContainer.className) : 'null',
    mainElementStyle: element.style.cssText,
    svgContainerStyle: svgContainer ? svgContainer.style.cssText : 'null'
  });

  // 清除主容器的所有背景相关样式，避免冲突
  element.style.removeProperty('background');
  element.style.removeProperty('background-color');
  element.style.removeProperty('background-image');
  element.style.removeProperty('background-repeat');
  element.style.removeProperty('background-size');
  element.style.removeProperty('background-position');

  // 如果存在svg-container，也清除它的背景样式
  if (svgContainer) {
    svgContainer.style.removeProperty('background');
    svgContainer.style.removeProperty('background-color');
    svgContainer.style.removeProperty('background-image');
    svgContainer.style.removeProperty('background-repeat');
    svgContainer.style.removeProperty('background-size');
    svgContainer.style.removeProperty('background-position');
  }

  console.log('清除背景样式后:', {
    mainElement: element.style.cssText,
    svgContainer: svgContainer ? svgContainer.style.cssText : 'null'
  });

  if (backgroundType === 'transparent') {
    // 透明背景 - 完全清除背景，包括渐变残留
    console.log('应用透明背景 - 清除所有背景样式');

    // 同时对主容器和svg-container应用透明背景，确保完全清除
    const applyTransparentStyle = (targetElement: any) => {
      targetElement.style.setProperty('background', 'none', 'important');
      targetElement.style.setProperty('background-color', 'transparent', 'important');
      targetElement.style.setProperty('background-image', 'none', 'important');
      targetElement.style.setProperty('background-size', 'auto', 'important');
      targetElement.style.setProperty('background-position', 'initial', 'important');
      targetElement.style.setProperty('background-repeat', 'initial', 'important');
    };

    // 应用到主容器
    applyTransparentStyle(element);

    // 应用到svg-container（如果存在）
    if (svgContainer) {
      applyTransparentStyle(svgContainer);
    }

    console.log('透明背景应用后的样式:', {
      mainElement: element.style.cssText,
      svgContainer: svgContainer ? svgContainer.style.cssText : 'null',
      mainElementComputed: {
        background: window.getComputedStyle(element).background,
        backgroundColor: window.getComputedStyle(element).backgroundColor
      },
      svgContainerComputed: svgContainer ? {
        background: window.getComputedStyle(svgContainer).background,
        backgroundColor: window.getComputedStyle(svgContainer).backgroundColor
      } : 'null'
    });

    return; // 直接返回，避免执行后续的背景处理逻辑
  } else if (backgroundType === 'linear-gradient') {
    // 线性渐变
    const angle = style.gradientAngle || 0;
    const start = style.gradientStart || '#409eff';
    const end = style.gradientEnd || '#67c23a';
    const gradientValue = `linear-gradient(${angle}deg, ${start}, ${end})`;
    console.log('应用线性渐变:', gradientValue);

    // 同时应用到主容器和svg-container
    element.style.setProperty('background', gradientValue, 'important');
    if (svgContainer) {
      svgContainer.style.setProperty('background', gradientValue, 'important');
    }
  } else if (backgroundType === 'radial-gradient') {
    // 径向渐变
    const start = style.gradientStart || '#409eff';
    const end = style.gradientEnd || '#67c23a';
    const shape = style.gradientShape || 'circle';
    const gradientValue = `radial-gradient(${shape}, ${start}, ${end})`;
    console.log('应用径向渐变:', gradientValue);

    // 同时应用到主容器和svg-container
    element.style.setProperty('background', gradientValue, 'important');
    if (svgContainer) {
      svgContainer.style.setProperty('background', gradientValue, 'important');
    }
  } else if (backgroundType === 'image' && style.backgroundImage) {
    // 背景图片
    const applyImageStyle = (targetElement: any) => {
      targetElement.style.setProperty('background-image', `url(${style.backgroundImage})`, 'important');
      targetElement.style.setProperty('background-repeat', style.backgroundRepeat || 'no-repeat', 'important');
      targetElement.style.setProperty('background-size', style.backgroundSize || 'cover', 'important');
      targetElement.style.setProperty('background-position', style.backgroundPosition || 'center', 'important');
    };

    // 同时应用到主容器和svg-container
    applyImageStyle(element);
    if (svgContainer) {
      applyImageStyle(svgContainer);
    }
  } else {
    // 纯色背景（solid或其他情况）
    let bgColor = style.backgroundColor;

    console.log('处理纯色背景:', {
      backgroundType: backgroundType,
      originalBgColor: bgColor,
      isTransparent: bgColor === 'transparent',
      isEmpty: !bgColor
    });

    // 检查和修正rgba透明度问题
    if (bgColor && typeof bgColor === 'string' && bgColor.includes('rgba')) {
      const rgbaMatch = bgColor.match(/rgba\((\d+),\s*(\d+),\s*(\d+),\s*([\d.]+)\)/);
      if (rgbaMatch) {
        const [, r, g, b, a] = rgbaMatch;
        const alpha = parseFloat(a);
        if (alpha === 0) {
          bgColor = `rgba(${r}, ${g}, ${b}, 1)`;
          console.log('修正rgba透明度从0到1:', bgColor);
        }
      }
    }

    // 如果是solid类型但没有设置backgroundColor，给一个默认的白色
    // 但如果用户明确设置为transparent，则保持透明
    if (backgroundType === 'solid' && !bgColor) {
      bgColor = 'rgba(255, 255, 255, 1)';
      console.log('设置默认白色背景:', bgColor);
    } else if (!bgColor) {
      bgColor = 'transparent';
      console.log('设置透明背景:', bgColor);
    }

    console.log('最终应用的背景色:', bgColor);

    // 同时应用到主容器和svg-container，使用!important确保样式优先级
    element.style.setProperty('background-color', bgColor, 'important');
    if (svgContainer) {
      svgContainer.style.setProperty('background-color', bgColor, 'important');
    }

    // 验证是否成功应用
    const mainAppliedColor = element.style.backgroundColor;
    const mainComputedStyle = window.getComputedStyle(element);
    const mainComputedBgColor = mainComputedStyle.backgroundColor;

    const svgAppliedColor = svgContainer ? svgContainer.style.backgroundColor : 'null';
    const svgComputedStyle = svgContainer ? window.getComputedStyle(svgContainer) : null;
    const svgComputedBgColor = svgComputedStyle ? svgComputedStyle.backgroundColor : 'null';

    console.log('样式应用结果:', {
      设置的颜色: bgColor,
      主容器: {
        style属性: mainAppliedColor,
        计算后样式: mainComputedBgColor,
        元素类名: element.className,
        内联样式: element.style.cssText
      },
      svgContainer: {
        style属性: svgAppliedColor,
        计算后样式: svgComputedBgColor,
        内联样式: svgContainer ? svgContainer.style.cssText : 'null'
      }
    });

  }

  // 统一处理所有背景类型的SVG容器样式
  applySvgContainerBackground(element, backgroundType, style);

  // 应用边框样式
  const borderStyle = style.borderStyle || 'solid';  // 默认实线,而不是none
  const borderWidth = style.borderWidth !== undefined ? style.borderWidth : 0;
  const borderColor = style.borderColor || '#d9d9d9';
  const borderRadius = style.borderRadius || 0;

  console.log('应用边框样式:', {
    borderStyle: borderStyle,
    borderWidth: borderWidth,
    borderColor: borderColor,
    borderRadius: borderRadius,
    组件ID: component.id,
    是否选中: element.classList.contains('selected')
  });

  // 如果组件当前被选中，先移除选中状态的边框，应用样式边框，然后重新添加选中效果
  const isCurrentlySelected = element.classList.contains('selected');

  if (borderStyle !== 'none' && borderWidth > 0) {
    // 应用边框样式
    element.style.setProperty('border-width', `${borderWidth}px`, 'important');
    element.style.setProperty('border-style', borderStyle, 'important');
    element.style.setProperty('border-color', borderColor, 'important');

    // 更新原始边框样式数据，用于选中状态恢复
    element.setAttribute('data-original-border-color', borderColor);
    element.setAttribute('data-original-border-width', `${borderWidth}px`);
    element.setAttribute('data-original-border-style', borderStyle);

    console.log('边框样式已应用:', `${borderWidth}px ${borderStyle} ${borderColor}`);
  } else {
    // 无边框
    element.style.removeProperty('border');
    element.style.setProperty('border', 'none', 'important');

    // 清除原始边框样式数据
    element.setAttribute('data-original-border-color', 'transparent');
    element.setAttribute('data-original-border-width', '0px');
    element.setAttribute('data-original-border-style', 'none');

    console.log('边框样式已移除');
  }

  // 应用边框圆角
  if (borderRadius > 0) {
    element.style.setProperty('border-radius', `${borderRadius}px`, 'important');
    console.log('圆角已应用:', `${borderRadius}px`);
  } else {
    element.style.removeProperty('border-radius');
  }

  // 如果组件之前是选中状态，重新应用选中效果
  if (isCurrentlySelected) {
    console.log('重新应用选中状态的边框和阴影效果');
    // 稍后重新设置选中效果，确保用户能看到选中状态
    setTimeout(() => {
      if (element.classList.contains('selected')) {
        // 🔲 按钮组件特殊处理：只应用阴影，不修改边框
        if (element.classList.contains('button-component')) {
          console.log('🔲 按钮组件选中，组合用户阴影和选中指示器阴影');

          // 获取用户设置的阴影(从 data 属性中读取)
          const originalShadow = element.getAttribute('data-original-shadow');
          let finalShadow = '0 0 0 2px rgba(64, 158, 255, 0.4)'; // 选中指示器阴影

          if (originalShadow && originalShadow !== 'none') {
            // 如果有用户设置的阴影,组合显示: 用户阴影 + 选中指示器阴影
            finalShadow = `${originalShadow}, ${finalShadow}`;
            console.log('🔲 组合阴影:', finalShadow);
          }

          element.style.setProperty('box-shadow', finalShadow, 'important');
        } else {
          // 其他组件：应用选中边框(保持用户设置的边框样式)
          const originalBorderWidth = element.getAttribute('data-original-border-width') || '2px';
          const originalBorderStyle = element.getAttribute('data-original-border-style') || 'solid';
          const originalBorderColor = element.getAttribute('data-original-border-color') || '#409eff';

          // 使用用户设置的边框样式,而不是强制 solid
          element.style.setProperty('border', `${originalBorderWidth} ${originalBorderStyle} #409eff`, 'important');
          console.log('选中边框已应用(保持用户样式):', `${originalBorderWidth} ${originalBorderStyle} #409eff`);

          // 组合选中阴影和原始阴影
          const originalShadow = element.getAttribute('data-original-shadow');
          let finalShadow = '0 0 0 2px rgba(64, 158, 255, 0.2)';

          if (originalShadow && style.enableShadow) {
            // 如果有原始阴影，则组合显示
            finalShadow = `${originalShadow}, ${finalShadow}`;
            console.log('组合阴影显示:', finalShadow);
          }

          element.style.setProperty('box-shadow', finalShadow, 'important');
        }
      }
    }, 50);
  }

  // 应用阴影效果
  if (style.enableShadow) {
    const shadowType = style.shadowType || 'box';
    const shadowColor = style.shadowColor || 'rgba(0,0,0,0.3)'; // 增加默认透明度
    const offsetX = style.shadowOffsetX || 4; // 增加默认偏移
    const offsetY = style.shadowOffsetY || 4;
    const blur = style.shadowBlur || 8; // 增加默认模糊

    console.log('应用阴影效果:', {
      shadowType: shadowType,
      shadowColor: shadowColor,
      offsetX: offsetX,
      offsetY: offsetY,
      blur: blur,
      组件ID: component.id,
      是否选中: isCurrentlySelected
    });

    if (shadowType === 'box') {
      const spread = style.shadowSpread || 0;
      const inset = style.shadowInset ? 'inset' : '';
      const shadowValue = `${inset} ${offsetX}px ${offsetY}px ${blur}px ${spread}px ${shadowColor}`;

      console.log('盒阴影值:', shadowValue);

      // 存储阴影值到元素属性，用于选中状态恢复
      element.setAttribute('data-original-shadow', shadowValue);

      if (!isCurrentlySelected) {
        element.style.setProperty('box-shadow', shadowValue, 'important');
      }
    } else {
      const shadowValue = `${offsetX}px ${offsetY}px ${blur}px ${shadowColor}`;
      console.log('文字阴影值:', shadowValue);

      // 应用文字阴影到主元素
      element.style.setProperty('text-shadow', shadowValue, 'important');

      // 查找并应用到所有文本子元素
      const textElements = element.querySelectorAll('span, p, div, text, tspan, h1, h2, h3, h4, h5, h6, label');
      textElements.forEach(textEl => {
        // 只对包含文本内容的元素应用
        if (textEl.textContent && textEl.textContent.trim()) {
          (textEl as HTMLElement).style.setProperty('text-shadow', shadowValue, 'important');
          console.log('为文本元素应用阴影:', textEl.tagName, textEl.textContent.substring(0, 20));
        }
      });

      // 特别处理SVG文本元素
      const svgTextElements = element.querySelectorAll('text, tspan');
      svgTextElements.forEach(svgTextEl => {
        // SVG文本元素使用filter效果模拟阴影
        const filterId = `text-shadow-${Math.random().toString(36).substr(2, 9)}`;
        let svg = (svgTextEl as Element).closest('svg');
        if (!svg) {
          svg = element.querySelector('svg') || element;
        }

        // 创建SVG滤镜
        let defs = svg.querySelector('defs');
        if (!defs) {
          defs = document.createElementNS('http://www.w3.org/2000/svg', 'defs');
          svg.insertBefore(defs, svg.firstChild);
        }

        const filter = document.createElementNS('http://www.w3.org/2000/svg', 'filter');
        filter.id = filterId;
        filter.setAttribute('x', '-50%');
        filter.setAttribute('y', '-50%');
        filter.setAttribute('width', '200%');
        filter.setAttribute('height', '200%');

        const dropShadow = document.createElementNS('http://www.w3.org/2000/svg', 'feDropShadow');
        dropShadow.setAttribute('dx', offsetX.toString());
        dropShadow.setAttribute('dy', offsetY.toString());
        dropShadow.setAttribute('stdDeviation', (blur / 2).toString());
        dropShadow.setAttribute('flood-color', shadowColor);
        dropShadow.setAttribute('flood-opacity', '1');

        filter.appendChild(dropShadow);
        defs.appendChild(filter);

        (svgTextEl as HTMLElement).style.filter = `url(#${filterId})`;
        console.log('为SVG文本应用阴影滤镜:', svgTextEl.textContent);
      });
    }
  } else {
    console.log('清除阴影效果');
    element.removeAttribute('data-original-shadow');

    if (!isCurrentlySelected) {
      element.style.removeProperty('box-shadow');
    }

    // 清除主元素的文字阴影
    element.style.removeProperty('text-shadow');

    // 清除所有文本子元素的阴影
    const textElements = element.querySelectorAll('span, p, div, text, tspan, h1, h2, h3, h4, h5, h6, label');
    textElements.forEach(textEl => {
      (textEl as HTMLElement).style.removeProperty('text-shadow');
    });

    // 清除SVG文本元素的滤镜阴影
    const svgTextElements = element.querySelectorAll('text, tspan');
    svgTextElements.forEach(svgTextEl => {
      (svgTextEl as HTMLElement).style.removeProperty('filter');
    });

    // 清理SVG中的阴影滤镜定义
    const svg = element.querySelector('svg');
    if (svg) {
      const defs = svg.querySelector('defs');
      if (defs) {
        const shadowFilters = defs.querySelectorAll('filter[id^="text-shadow-"]');
        shadowFilters.forEach(filter => {
          defs.removeChild(filter);
        });
        // 如果defs为空，则移除它
        if (!defs.hasChildNodes()) {
          svg.removeChild(defs);
        }
      }
    }

    console.log('已清除所有文字阴影效果');
  }

  // 应用视觉效果滤镜
  const filters = [];
  if (style.opacity !== undefined && style.opacity !== 1) {
    element.style.opacity = style.opacity.toString();
  }
  if (style.blur) filters.push(`blur(${style.blur}px)`);
  if (style.brightness !== undefined && style.brightness !== 1) filters.push(`brightness(${style.brightness})`);
  if (style.contrast !== undefined && style.contrast !== 1) filters.push(`contrast(${style.contrast})`);
  if (style.saturate !== undefined && style.saturate !== 1) filters.push(`saturate(${style.saturate})`);
  if (style.hueRotate) filters.push(`hue-rotate(${style.hueRotate}deg)`);
  if (style.invert) filters.push(`invert(${style.invert})`);
  if (style.sepia) filters.push(`sepia(${style.sepia})`);
  if (style.grayscale) filters.push(`grayscale(${style.grayscale})`);

  element.style.filter = filters.length > 0 ? filters.join(' ') : 'none';

  // 应用SVG样式（如果是SVG元素） - 使用SVG属性而不是CSS样式
  if (element.tagName === 'svg' || element.querySelector('svg')) {
    const svgElement = element.tagName === 'svg' ? element : element.querySelector('svg');
    if (svgElement) {
      // 🎨 特殊处理：progress-v 组件的SVG根元素不应用描边样式
      // Tank 组件（circularTankLevel, squareTankLevel）可以正常使用描边
      const componentType = component.type || component.name;
      const isProgressComponent = componentType && componentType.includes('progress');

      if (isProgressComponent) {
        console.log(`🎨 检测到进度条组件 [${componentType}]，跳过SVG根元素的描边样式应用，避免破坏渐变和滤镜效果`);
        // 填充相关属性由applySvgStyles函数统一处理，避免冲突
        return; // 跳过所有描边样式应用
      }

      // 注意：fill和fillOpacity应该通过applySvgStyles函数统一处理，这里只处理描边相关属性
      if (style.stroke) (svgElement as SVGElement).setAttribute('stroke', style.stroke);
      if (style.strokeWidth) (svgElement as SVGElement).setAttribute('stroke-width', style.strokeWidth.toString());
      if (style.strokeDasharray) (svgElement as SVGElement).setAttribute('stroke-dasharray', style.strokeDasharray);
      if (style.strokeLinecap) (svgElement as SVGElement).setAttribute('stroke-linecap', style.strokeLinecap);
      if (style.strokeLinejoin) (svgElement as SVGElement).setAttribute('stroke-linejoin', style.strokeLinejoin);
      if (style.strokeOpacity !== undefined) (svgElement as SVGElement).setAttribute('stroke-opacity', style.strokeOpacity.toString());

      // 填充相关属性由applySvgStyles函数统一处理，避免冲突
    }
  }
};

// 自适应边框到图标大小
export const autoFitToIcon = (selectedCanvasComponent: any, componentFormData: any, isSaved: any, removeResizeHandles: any, addResizeHandles: any, ElMessage: any) => {
  if (!selectedCanvasComponent.value) {
    return;
  }

  const component = selectedCanvasComponent.value;
  const element = document.getElementById(component.id);

  if (!element) {
    return;
  }

  // 检查是否为文本组件
  const isTextComponent = element.classList.contains('text-component');

  if (isTextComponent) {
    ElMessage.warning('文本组件无需自适应边框');
    return;
  }

  // 查找SVG元素 - 不论是绘图组件还是其他组件，只要有SVG图标就能自适应
  const svgElement = element.querySelector('svg');

  if (svgElement) {
      try {
        console.log('🎯 开始自适应边框分析:', component.id);

        // 第1步：获取SVG内部实际绘制内容的边界（用户坐标系）
        // 优先查找 id="shape" 元素（排除不可见的 frame 框架）
        let targetElement = svgElement.querySelector('#shape') || svgElement;
        const isUsingShapeElement = targetElement !== svgElement;

        const bbox = targetElement.getBBox();
        console.log('📐 SVG内容边界框 (getBBox):', {
          使用元素: isUsingShapeElement ? '#shape' : 'svg根元素',
          x: bbox.x,
          y: bbox.y,
          width: bbox.width,
          height: bbox.height
        });

        // 第2步：获取SVG的viewBox和当前渲染尺寸
        const viewBox = svgElement.viewBox.baseVal;
        const svgRect = svgElement.getBoundingClientRect();

        console.log('📏 SVG viewBox和渲染尺寸:', {
          viewBoxWidth: viewBox.width,
          viewBoxHeight: viewBox.height,
          renderedWidth: svgRect.width,
          renderedHeight: svgRect.height
        });

        // 第3步：计算从viewBox坐标到屏幕像素的缩放比例
        // 使用统一的缩放比例（取较小值）以保持宽高比，避免变形
        let scaleX = 1;
        let scaleY = 1;
        let uniformScale = 1;

        if (viewBox.width > 0 && viewBox.height > 0) {
          scaleX = svgRect.width / viewBox.width;
          scaleY = svgRect.height / viewBox.height;
          // 取较小的缩放比例，确保内容不会被裁切且不变形
          uniformScale = Math.min(scaleX, scaleY);
        } else {
          uniformScale = 1;
        }

        console.log('🔍 缩放比例:', {
          原scaleX: scaleX,
          原scaleY: scaleY,
          统一缩放: uniformScale
        });

        // 第4步：计算内容在屏幕上的实际尺寸（像素）- 使用统一缩放
        const contentWidthPx = bbox.width * uniformScale;
        const contentHeightPx = bbox.height * uniformScale;

        console.log('📦 内容实际屏幕尺寸（保持宽高比）:', {
          宽度: contentWidthPx,
          高度: contentHeightPx
        });

        // 第5步：添加最小边距（4px）防止边框裁切
        const minPadding = 4;
        let newWidth = Math.round(contentWidthPx + minPadding * 2);
        let newHeight = Math.round(contentHeightPx + minPadding * 2);

        // 确保最小尺寸
        const minSize = 10;
        newWidth = Math.max(minSize, newWidth);
        newHeight = Math.max(minSize, newHeight);

        console.log('✅ 最终容器尺寸（紧贴内容）:', {
          新宽度: newWidth,
          新高度: newHeight
        });

        // 更新组件尺寸
        component.size.width = newWidth;
        component.size.height = newHeight;

        // 更新兼容性属性
        component.width = newWidth;
        component.height = newHeight;

        // 更新表单数据
        componentFormData.value.size.width = newWidth;
        componentFormData.value.size.height = newHeight;

        // 更新DOM元素尺寸 - 完全移除padding和margin
        element.style.width = `${newWidth}px`;
        element.style.height = `${newHeight}px`;
        element.style.padding = '0';
        element.style.margin = '0';
        element.style.boxSizing = 'border-box';

        // 清除svg-container的padding（如果存在）
        const svgContainer = element.querySelector('.svg-container');
        if (svgContainer) {
          (svgContainer as HTMLElement).style.padding = '0';
          (svgContainer as HTMLElement).style.margin = '0';
          (svgContainer as HTMLElement).style.width = '100%';
          (svgContainer as HTMLElement).style.height = '100%';
          (svgContainer as HTMLElement).style.boxSizing = 'border-box';
        }

        // 设置SVG填充容器，并调整viewBox使内容居中
        svgElement.style.width = '100%';
        svgElement.style.height = '100%';
        svgElement.style.display = 'block';
        svgElement.style.margin = '0';
        svgElement.style.padding = '0';

        // 更新viewBox使内容紧贴边界（使用统一缩放计算边距）
        const viewBoxPadding = minPadding / uniformScale;
        const newViewBox = `${bbox.x - viewBoxPadding} ${bbox.y - viewBoxPadding} ${bbox.width + viewBoxPadding * 2} ${bbox.height + viewBoxPadding * 2}`;
        svgElement.setAttribute('viewBox', newViewBox);

        console.log('🎨 更新viewBox:', newViewBox);

        // 重新添加调整手柄
        removeResizeHandles(element);
        addResizeHandles(element, component);

        isSaved.value = false;
        ElMessage.success(`✅ 组件已自适应，尺寸: ${newWidth} × ${newHeight}px`);
        console.log('🎉 自适应边框完成');
        return;

      } catch (error) {
        console.error('获取SVG尺寸时出错:', error);
      }

      // 如果所有方法都失败，提示用户
      ElMessage.warning('无法分析图标边界，建议手动调整尺寸');
      return;
  } else {
    // 如果没有SVG元素，检查是否有img元素（其他类型的图标组件）
    const imgElement = element.querySelector("img");

    if (imgElement && component.svgPath) {
      // 如果图片已经加载，分析SVG内容尺寸
      if (imgElement.naturalWidth > 0 && imgElement.naturalHeight > 0) {
        const currentImgRect = imgElement.getBoundingClientRect();

        // 检查是否为data URI格式的SVG
        if (imgElement.src && imgElement.src.startsWith('data:image/svg+xml')) {
          try {
            // 解码SVG内容
            const svgContent = decodeURIComponent(imgElement.src.replace('data:image/svg+xml,', ''));

            // 解析SVG的viewBox和原始尺寸
            const viewBoxMatch = svgContent.match(/viewBox=['"]([^'"]+)['"]/);
            const widthMatch = svgContent.match(/width=['"](\d+)['"]/);
            const heightMatch = svgContent.match(/height=['"](\d+)['"]/);

            if (viewBoxMatch) {
              const viewBoxValues = viewBoxMatch[1].split(/\s+/).map(v => parseFloat(v));
              if (viewBoxValues.length >= 4) {
                const [vbX, vbY, vbWidth, vbHeight] = viewBoxValues;

                // SVG原始宽高比
                const svgAspectRatio = vbWidth / vbHeight;

                // 当前容器的宽高
                const containerWidth = element.offsetWidth;
                const containerHeight = element.offsetHeight;
                const containerAspectRatio = containerWidth / containerHeight;

                let optimalWidth, optimalHeight;

                // 根据宽高比计算最适合的尺寸
                if (Math.abs(svgAspectRatio - 1) < 0.1) {
                  // 如果是正方形图标，使用最小边作为基准
                  const minDimension = Math.min(containerWidth, containerHeight);
                  optimalWidth = optimalHeight = minDimension;
                } else if (svgAspectRatio > containerAspectRatio) {
                  // SVG更宽，以宽度为基准
                  optimalWidth = containerWidth;
                  optimalHeight = containerWidth / svgAspectRatio;
                } else {
                  // SVG更高，以高度为基准
                  optimalHeight = containerHeight;
                  optimalWidth = containerHeight * svgAspectRatio;
                }

                // 添加适当的边距
                const padding = 16;
                let newWidth = Math.round(optimalWidth + padding);
                let newHeight = Math.round(optimalHeight + padding);

                // 确保最小尺寸
                const minSize = 32;
                newWidth = Math.max(minSize, newWidth);
                newHeight = Math.max(minSize, newHeight);

                // 更新组件尺寸
                component.size.width = newWidth;
                component.size.height = newHeight;
                componentFormData.value.size.width = newWidth;
                componentFormData.value.size.height = newHeight;
                element.style.width = `${newWidth}px`;
                element.style.height = `${newHeight}px`;

                removeResizeHandles(element);
                addResizeHandles(element, component);

                isSaved.value = false;
                ElMessage.success(`图标已自适应到最佳尺寸: ${newWidth} × ${newHeight} (宽高比 ${svgAspectRatio.toFixed(2)}:1)`);
                return;
              }
            }
          } catch (error) {
            console.warn('解析SVG data URI失败:', error);
          }
        }

        // fallback: 如果无法解析SVG内容，使用渲染尺寸
        const padding = 8;
        let newWidth = Math.round(currentImgRect.width + padding);
        let newHeight = Math.round(currentImgRect.height + padding);

        // 确保最小尺寸
        const minSize = 32;
        newWidth = Math.max(minSize, newWidth);
        newHeight = Math.max(minSize, newHeight);

        // 更新组件尺寸
        component.size.width = newWidth;
        component.size.height = newHeight;
        componentFormData.value.size.width = newWidth;
        componentFormData.value.size.height = newHeight;
        element.style.width = `${newWidth}px`;
        element.style.height = `${newHeight}px`;

        removeResizeHandles(element);
        addResizeHandles(element, component);

        isSaved.value = false;
        ElMessage.success(`组件已自适应到图标尺寸: ${newWidth} × ${newHeight}`);
        return;
      }

      // 创建临时图片获取原始尺寸
      const tempImg = new Image();
      tempImg.onload = () => {
        const iconWidth = tempImg.naturalWidth || 60;
        const iconHeight = tempImg.naturalHeight || 60;

        const padding = 8;
        let newWidth = Math.round(iconWidth + padding);
        let newHeight = Math.round(iconHeight + padding);

        const minSize = 32;
        newWidth = Math.max(minSize, newWidth);
        newHeight = Math.max(minSize, newHeight);

        // 更新组件尺寸
        component.size.width = newWidth;
        component.size.height = newHeight;
        componentFormData.value.size.width = newWidth;
        componentFormData.value.size.height = newHeight;
        element.style.width = `${newWidth}px`;
        element.style.height = `${newHeight}px`;

        removeResizeHandles(element);
        addResizeHandles(element, component);

        isSaved.value = false;
        ElMessage.success(`组件已自适应到图标尺寸: ${newWidth} × ${newHeight}`);
      };

      tempImg.onerror = () => {
        ElMessage.warning('无法加载图标文件');
      };

      // 加载图标文件
      if (component.svgPath.startsWith("@/assets/svg/")) {
        const fileName = component.svgPath.replace("@/assets/svg/", "");
        try {
          tempImg.src = new URL(`../../../assets/svg/${fileName}`, import.meta.url).href;
        } catch (error) {
          tempImg.src = `/src/assets/svg/${fileName}`;
        }
      } else {
        tempImg.src = component.svgPath;
      }
    } else {
      ElMessage.warning('该组件没有可识别的图标，无法进行自适应边框调整');
    }
  }
};

// 更新组件变换属性
export const updateComponentTransform = (selectedCanvasComponent: any, componentFormData: any, isSaved: any, removeResizeHandles: any, addResizeHandles: any, nextTick: any, ElMessage: any) => {
  if (!selectedCanvasComponent.value) return;

  const component = selectedCanvasComponent.value;

  // 确保基础属性存在
  if (!component.position) component.position = { x: component.x || 0, y: component.y || 0 };
  if (!component.size) component.size = { width: component.width || 100, height: component.height || 100 };

  // 变换属性直接从组件读取，不依赖formData
  const rotation = component.rotation || 0;
  const scale = component.scale || 1;
  const flipHorizontal = component.flipHorizontal || false;
  const flipVertical = component.flipVertical || false;
  const skewX = component.skewX || 0;
  const skewY = component.skewY || 0;

  // 更新DOM元素样式
  const element = document.getElementById(component.id);
  if (element) {
    console.log('更新组件变换:', {
      id: component.id,
      position: component.position,
      size: component.size,
      rotation,
      scale,
      flipHorizontal,
      flipVertical,
      skewX,
      skewY
    });

    // 更新位置和尺寸
    element.style.left = `${component.position.x}px`;
    element.style.top = `${component.position.y}px`;
    element.style.width = `${component.size.width}px`;
    element.style.height = `${component.size.height}px`;

    // 构建变换字符串
    let transform = "";

    // 添加旋转
    if (rotation !== 0) {
      transform += `rotate(${rotation}deg) `;
    }

    // 添加缩放
    if (scale !== 1) {
      transform += `scale(${scale}) `;
    }

    // 添加翻转
    if (flipHorizontal && flipVertical) {
      transform += `scale(-1, -1) `;
    } else if (flipHorizontal) {
      transform += `scaleX(-1) `;
    } else if (flipVertical) {
      transform += `scaleY(-1) `;
    }

    // 添加倾斜
    if (skewX !== 0) {
      transform += `skewX(${skewX}deg) `;
    }
    if (skewY !== 0) {
      transform += `skewY(${skewY}deg) `;
    }

    // 应用变换
    if (transform) {
      element.style.transform = transform.trim();
      console.log('应用变换:', transform.trim());
    } else {
      element.style.transform = "";
    }

    // 重新添加调整手柄
    removeResizeHandles(element);
    addResizeHandles(element, component);
  }

  isSaved.value = false;
  ElMessage.success("组件变换属性已更新");
};

// 处理组件属性更新
export const handleUpdateComponentProperty = (property: string, value: any, selectedCanvasComponent: any, applyStyleToElement: any, updateSvgIconStyle: any, refreshComponentEvents: any, updateComponentInteractivity: any, updateComponentShape: any, redrawCanvas: any, isSaved: any, updateButtonAppearance?: any, createComponentElement?: any) => {
  if (!selectedCanvasComponent.value) return;

  if (property === "binding") {
    // 处理数据绑定更新
    selectedCanvasComponent.value.componentBinding = value;
  } else if (property === "style") {
    // 处理样式更新
    if (!selectedCanvasComponent.value.style) {
      selectedCanvasComponent.value.style = {};
    }

    // 过滤掉undefined和null的属性，避免覆盖现有有效值
    const cleanedValue = {};
    for (const key in value) {
      if (value[key] !== undefined && value[key] !== null) {
        cleanedValue[key] = value[key];
      }
    }

    console.log('handleUpdateComponentProperty更新组件样式:', {
      原始value: value,
      清理后的value: cleanedValue,
      当前style: selectedCanvasComponent.value.style,
      合并后的style: { ...selectedCanvasComponent.value.style, ...cleanedValue }
    });

    Object.assign(selectedCanvasComponent.value.style, cleanedValue);

    // 🔘 如果更新了 switchState，同步到旧位置以保持兼容性
    if (cleanedValue['switchState'] !== undefined) {
      const switchState = cleanedValue['switchState'];
      selectedCanvasComponent.value.switchState = (switchState === 'on');
      console.log('🔘 同步 switchState:', {
        'style.switchState': switchState,
        'component.switchState': selectedCanvasComponent.value.switchState
      });
    }

    // 应用样式到组件元素 - 统一由此处理，避免重复调用
    applyStyleToElement(selectedCanvasComponent.value);

    // 对于SVG组件，也需要调用SVG特定的样式更新
    updateSvgIconStyle();

    // 标记项目已修改
    // 注意: 不调用redrawCanvas()，因为applyStyleToElement已经直接更新了DOM
    isSaved.value = false;

    console.log('组件样式更新完成:', {
      componentId: selectedCanvasComponent.value.id,
      newStyle: selectedCanvasComponent.value.style
    });
  } else if (property === "events") {
    // 处理事件配置更新
    selectedCanvasComponent.value.events = value;
    // 刷新组件事件设置（重新绑定定时器等）
    refreshComponentEvents(selectedCanvasComponent.value);
    console.log(`组件 ${selectedCanvasComponent.value.id} 的事件配置已更新`);
  } else if (property === "paintBoardConfig") {
    // 🎨 处理画板配置更新 (主要是背景颜色)
    selectedCanvasComponent.value.paintBoardConfig = value;
    console.log('🎨 画板配置更新:', value);

    // 更新 wrapper 背景颜色
    const element = document.getElementById(selectedCanvasComponent.value.id);
    if (element && value.backgroundColor) {
      element.style.backgroundColor = value.backgroundColor;
    }

    isSaved.value = false;
    return;  // 提前返回,避免后续重复的 redrawCanvas
  } else {
    // 处理其他属性更新
    selectedCanvasComponent.value[property] = value;

    // 🔲 如果是按钮组件且更新了 properties，立即更新按钮外观
    if (selectedCanvasComponent.value.type === 'button' && property === 'properties' && updateButtonAppearance) {
      const element = document.getElementById(selectedCanvasComponent.value.id);
      if (element) {
        updateButtonAppearance(selectedCanvasComponent.value, element);
        console.log('🔲 按钮组件 properties 更新，已刷新外观');
      }
    }

    // 🎚️ FUXA 控制组件 properties 更新处理
    if (property === 'properties') {
      const element = document.getElementById(selectedCanvasComponent.value.id);
      if (element) {
        const componentType = selectedCanvasComponent.value.type;

        if (componentType === 'slider') {
          updateFuxaSliderAppearance(selectedCanvasComponent.value, element);
          console.log('🎚️ Slider 组件 properties 更新，已刷新外观');
        } else if (componentType === 'value') {
          updateValueAppearance(selectedCanvasComponent.value, element);
          console.log('📊 Value 组件 properties 更新，已刷新外观');
        } else if (componentType === 'editvalue') {
          updateHtmlInputAppearance(selectedCanvasComponent.value, element);
          console.log('📝 HtmlInput 组件 properties 更新，已刷新外观');
        } else if (componentType === 'selectvalue') {
          updateHtmlSelectAppearance(selectedCanvasComponent.value, element);
          console.log('📋 HtmlSelect 组件 properties 更新，已刷新外观');
        } else if (componentType === 'progress-v') {
          // 🎯 进度条现在使用 SvgManager，需要使用 updateComponentStyle 方法
          const svgContainer = element.querySelector('.svg-container') as HTMLElement;
          if (svgContainer) {
            const component = selectedCanvasComponent.value;
            const svgOptions: any = {
              animation: component.style?.svgAnimation || 'none',
              animationSpeed: component.style?.animationSpeed || 'normal',
              animationDuration: component.style?.animationDuration,
              animationIterationCount: component.style?.animationIterationCount || 'infinite',
              animationStaticValue: component.style?.animationStaticValue || 100,
              strokeColor: component.style?.borderColor,
              strokeWidth: component.style?.borderWidth,
              opacity: component.style?.opacity
            };
            svgManager.updateComponentStyle(svgContainer, svgOptions, 'progress-v');
            console.log('📊 GaugeProgress 组件 properties 更新，已通过SvgManager刷新外观');
          }
        } else if (componentType === 'semaphore') {
          updateGaugeSemaphoreAppearance(selectedCanvasComponent.value, element);
          console.log('🚦 GaugeSemaphore 组件 properties 更新，已刷新外观');
        } else if (componentType === 'bag') {
          updateHtmlBagAppearance(selectedCanvasComponent.value, element);
          console.log('🎯 HtmlBag 组件 properties 更新，已刷新外观');
        } else if (componentType === 'led-display') {
          // 💡 LED 显示屏组件更新
          ledDisplayManager.updateLedDisplayComponent(
            selectedCanvasComponent.value.id,
            value
          );
          console.log('💡 LED 显示屏组件 properties 更新，已刷新外观');
        }
      }
    }

    // 根据属性类型调用对应的更新函数
    if (
      [
        "clickable",
        "hoverable",
        "longPress",
        "doubleClick",
        "bindVariable",
        "updateRate",
        "dataFormat",
        "visibilityCondition",
        "enableCondition",
        "requiredPermission",
        "userGroups"
      ].includes(property)
    ) {
      updateComponentInteractivity();
    } else if (
      [
        "strokeStyle",
        "dashArray",
        "lineCap",
        "fillType",
        "gradientStart",
        "gradientEnd",
        "gradientAngle",
        "enableShadow",
        "shadowColor",
        "shadowOffsetX",
        "shadowOffsetY",
        "shadowBlur",
        "blur",
        "brightness",
        "contrast"
      ].includes(property)
    ) {
      updateComponentShape();
    }
  }

  isSaved.value = false;
  redrawCanvas();
};

// 重新导出 FUXA 控制组件的创建和更新函数，以便在 index.vue 中使用
export {
  createFuxaSliderElement,
  updateFuxaSliderAppearance,
  // createGaugeProgressElement 和 updateGaugeProgressAppearance 已移除，现在使用 SvgManager
  createGaugeSemaphoreElement,
  updateGaugeSemaphoreAppearance,
  createHtmlBagElement,
  updateHtmlBagAppearance
};