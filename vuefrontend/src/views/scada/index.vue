<script setup lang="ts">
import {
  ref,
  onMounted,
  onUnmounted,
  computed,
  reactive,
  watch,
  nextTick
} from "vue";
import { useRoute, useRouter } from "vue-router";
import { ElMessage, ElMessageBox } from "element-plus";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { fuxaMqttService } from "./core/fuxaMqttService";
import FuxaComponentPanel from "./components/FuxaComponentPanel.vue";
import DatasetPanel from "./components/DatasetPanel.vue";
import PropertyPanel from "./components/PropertyPanel.vue";
import PropertyEditDialog from "./components/PropertyEditDialog.vue";
import ChartPropertyDialog from "./components/ChartPropertyDialog.vue";
import IframePropertyDialog from "./components/IframePropertyDialog.vue";
import VideoPropertyDialog from "./components/VideoPropertyDialog.vue";
import WebcamPropertyDialog from "./components/WebcamPropertyDialog.vue";
import TablePropertyDialog from "./components/TablePropertyDialog.vue";
import ThermometerPropertyDialog from "./components/ThermometerPropertyDialog.vue";
import TextCardPropertyDialog from "./components/TextCardPropertyDialog.vue";
import {
  addResizeHandles,
  removeResizeHandles,
  fuxaResizeHandles
} from "./core/FuxaResizeHandles";
import { pathTool } from "./core/PathTool";
import * as echarts from "echarts";
import { componentManager } from "./core/ComponentManager";
import { ledDisplayManager } from "./core/LedDisplayComponent";
import { textCardManager } from "./core/TextCardComponent";
import {
  svgManager,
  createSvgComponent,
  cleanupAbnormalSvgElements
} from "./core/SvgManager";

// 导入分离的工具方法
import * as utils1 from "./main/utils1";
import * as utils2 from "./main/utils2";
import * as utils3 from "./main/utils3";
import * as utils4 from "./main/utils4";
import * as utilsButton from "./main/utils-button";
import * as utilsProject from "./main/utils-project";
import {
  DEFAULT_PROJECT_DATA,
  getProjectId,
  getProjectKind,
  setProjectKind
} from "./main/utils-project";
import {
  rehydrateDatasetList,
  previewIotDataset
} from "./main/utils-dataset";
import scadaApi, { type ScadaProjectInfo } from "@/api/scada/project";

import {
  Close,
  Delete,
  Operation,
  Lightning,
  Plus,
  VideoPlay,
  Back,
  Right,
  Top,
  Bottom,
  Connection,
  Check,
  ArrowLeft,
  Grid,
  Setting
} from "@element-plus/icons-vue";

defineOptions({
  name: "ScadaEditor"
});

const route = useRoute();
const router = useRouter();

const projectId = getProjectId(route);
/** 项目类型：scada=监控组态(全部图元) / dash=自定义报表(仅报表组件) */
const projectKind = getProjectKind(route);

/**
 * 编辑器→编辑器直跳（换项目或换 kind，路由同名仅 params/query 变）时，Vue 会复用组件实例、
 * 不重跑 setup —— 结果是画布残留上一个项目的组件、组件面板仍按旧 kind 过滤、读写走错接口。
 * 编辑器状态极重（画布/组件/数据集/MQTT/自动保存），局部重载难以覆盖干净，整页重载最稳。
 * 编辑器自身从不改路由，不会触发重载循环。
 */
watch(
  () => route.fullPath,
  (next, prev) => {
    if (prev && next !== prev) window.location.reload();
  }
);

const projectData = ref({ ...DEFAULT_PROJECT_DATA });
const loading = ref(false);
const editorContainer = ref<HTMLDivElement>();

// 编辑器工具栏状态
const isSimulating = ref(false);
const isSaved = ref(true);

// 直线绘制状态
const lineDrawingState = reactive({
  isDrawing: false,
  startPoint: null,
  tempLineElement: null,
  currentPoints: []
});

const mqttStatus = computed(() => fuxaMqttService.status.value);
const mqttDeviceCount = computed(() => fuxaMqttService.devices.size);
const mqttMessageCount = computed(() => fuxaMqttService.messageCount.value);

// 项目基本信息
const projectInfo = ref<ScadaProjectInfo>({
  SnowId: "",
  ProjectName: "未命名项目",
  ProjectDesc: "",
  ProjectStatus: 0,
  TenantId: 0
});

const showComponentPanel = ref(true);
const showPropertyPanel = ref(true);
const canvasZoom = ref(100);
const selectedCanvasComponent = ref(null);

// 画布网格和吸附功能
const showGrid = ref(true); // 显示网格
const enableSnap = ref(true); // 启用吸附
const gridSize = ref(20); // 网格大小（像素）

// 画布尺寸和样式
const canvasWidth = ref(1920);
const canvasHeight = ref(1080);
const canvasBackgroundColor = ref("#f5f5f5");
const canvasBackgroundImage = ref("");

// 右键菜单状态
const contextMenuVisible = ref(false);
const contextMenuPosition = reactive({ x: 0, y: 0 });
const clipboardData = ref(null);

// 属性编辑弹框状态
const propertyDialogVisible = ref(false);

// 图表配置弹框状态
const chartConfigVisible = ref(false);
const currentChartComponent = ref(null);

// 图表属性弹框状态
const chartPropertyDialogVisible = ref(false);

// iframe配置弹框状态
const iframeConfigDialogVisible = ref(false);
const currentIframeComponent = ref(null);

// 视频配置弹框状态
const videoConfigDialogVisible = ref(false);
const currentVideoComponent = ref(null);

// 摄像头配置弹框状态
const webcamConfigDialogVisible = ref(false);
const currentWebcamComponent = ref(null);

// 表格配置弹框状态
const tableConfigDialogVisible = ref(false);
const currentTableComponent = ref(null);

// 温度计配置弹框状态
const thermometerConfigVisible = ref(false);

// 文本卡片配置弹框状态
const textCardConfigDialogVisible = ref(false);
const currentTextCardComponent = ref(null);

// 数据集配置状态
const datasetDialogVisible = ref(false);
const datasetList = ref([
  { id: "ds1", name: "传感器数据", type: "api" },
  { id: "ds2", name: "MQTT数据", type: "mqtt" }
]);

// 设备列表
// 设备列表 - 从数据集配置获取(优先API数据,其次静态数据)
const deviceList = computed(() => {
  // 1. 尝试从 API 数据获取
  if (apiDeviceData.value && apiDeviceData.value.length > 0) {
    return apiDeviceData.value;
  }

  // 2. 从静态数据获取
  if (staticDeviceData.value && staticDeviceData.value.length > 0) {
    return staticDeviceData.value;
  }

  // 3. 返回空数组
  return [];
});

// API 设备数据
const apiDeviceData = ref([]);

// 静态设备数据
const staticDeviceData = ref([]);
const currentDataset = ref(null);
const chartConfigData = ref({
  type: "pie", // pie, line, bar
  title: "图表标题",
  dataSource: "static", // static, api, mqtt
  staticData: [
    { name: "数据1", value: 30 },
    { name: "数据2", value: 50 },
    { name: "数据3", value: 20 }
  ],
  apiConfig: {
    url: "",
    method: "GET",
    headers: {},
    params: {},
    dataPath: "data"
  },
  mqttConfig: {
    topic: "",
    dataPath: "value"
  },
  refreshInterval: 5000,
  theme: "default"
});

// 属性面板状态
const activeCollapse = ref("basic");
const componentFormData = ref({
  id: "",
  name: "",
  type: "",
  position: { x: 0, y: 0 },
  size: { width: 0, height: 0 },
  style: {
    backgroundColor: "transparent",
    borderColor: "#d9d9d9",
    color: "#303133"
  },
  // 新增变换属性
  rotation: 0,
  scale: 1,
  flipHorizontal: false,
  flipVertical: false,
  lockAspectRatio: false,
  // 交互属性
  clickable: false,
  hoverable: false,
  longPress: false,
  doubleClick: false,
  bindVariable: "",
  updateRate: "normal",
  dataFormat: "",
  visibilityCondition: "",
  enableCondition: "",
  requiredPermission: "none",
  userGroups: "",
  // 形状属性
  strokeStyle: "solid",
  dashArray: 5,
  lineCap: "round",
  fillType: "solid",
  gradientStart: "#409eff",
  gradientEnd: "#67c23a",
  gradientAngle: 0,
  enableShadow: false,
  shadowColor: "#00000040",
  shadowOffsetX: 2,
  shadowOffsetY: 2,
  shadowBlur: 4,
  blur: 0,
  brightness: 1,
  contrast: 1,
  properties: {},
  events: [],
  componentBinding: null, // 完整的ComponentBinding对象
  deviceId: "",
  paramcode: "",
  targetProperty: "text",
  bindingMode: "direct", // direct, conditional, hybrid
  valueTransform: "",
  conditions: [],
  actions: [],
  previewData: null
});

// 右键菜单样式
const contextMenuStyle = computed(() => ({
  position: "fixed",
  left: `${contextMenuPosition.x}px`,
  top: `${contextMenuPosition.y}px`,
  zIndex: 9999,
  display: contextMenuVisible.value ? "block" : "none"
}));

// 右键菜单项配置
const contextMenuItems = computed(() => {
  const hasSelection = !!selectedCanvasComponent.value;
  const hasClipboard = !!clipboardData.value;
  const isChartComponent =
    hasSelection && selectedCanvasComponent.value?.chartConfig;
  const isIframeComponent =
    hasSelection && selectedCanvasComponent.value?.type === "iframe";
  const isVideoComponent =
    hasSelection && selectedCanvasComponent.value?.type === "video";
  const isWebcamComponent =
    hasSelection && selectedCanvasComponent.value?.type === "webcam";
  const isTableComponent =
    hasSelection &&
    (selectedCanvasComponent.value?.type === "table" ||
      selectedCanvasComponent.value?.tableConfig);
  const isThermometerComponent =
    hasSelection && selectedCanvasComponent.value?.type === "thermometer";
  const isTextCardComponent =
    hasSelection && selectedCanvasComponent.value?.type === "text-card";

  return [
    {
      id: "property-edit",
      label: "属性编辑",
      icon: "ep:setting",
      disabled: !hasSelection,
      action: "propertyEdit"
    },
    {
      id: "chart-config",
      label: "图表属性",
      icon: "ep:data-line",
      disabled: !isChartComponent,
      action: "chartConfig"
    },
    {
      id: "iframe-config",
      label: "配置内嵌网页",
      icon: "ep:link",
      disabled: !isIframeComponent,
      action: "iframeConfig"
    },
    {
      id: "video-config",
      label: "视频配置",
      icon: "ep:video-play",
      disabled: !isVideoComponent,
      action: "videoConfig"
    },
    {
      id: "webcam-config",
      label: "摄像头配置",
      icon: "ep:video-camera",
      disabled: !isWebcamComponent,
      action: "webcamConfig"
    },
    {
      id: "table-config",
      label: "表格配置",
      icon: "ep:grid",
      disabled: !isTableComponent,
      action: "tableConfig"
    },
    {
      id: "thermometer-config",
      label: "温度计配置",
      icon: "ep:tools",
      disabled: !isThermometerComponent,
      action: "thermometerConfig"
    },
    {
      id: "text-card-config",
      label: "文本卡片配置",
      icon: "ep:document",
      disabled: !isTextCardComponent,
      action: "textCardConfig"
    },
    { separator: true },
    {
      id: "cut",
      label: "剪切",
      icon: "ep:scissors",
      shortcut: "Ctrl+X",
      disabled: !hasSelection,
      action: "cut"
    },
    {
      id: "copy",
      label: "复制",
      icon: "ep:copy-document",
      shortcut: "Ctrl+C",
      disabled: !hasSelection,
      action: "copy"
    },
    {
      id: "paste",
      label: "粘贴",
      icon: "ep:document-copy",
      shortcut: "Ctrl+V",
      disabled: !hasClipboard,
      action: "paste"
    },
    { separator: true },
    {
      id: "delete",
      label: "删除",
      icon: "ep:delete",
      shortcut: "Delete",
      disabled: !hasSelection,
      action: "delete"
    },
    { separator: true },
    {
      id: "bring-front",
      label: "置于顶层",
      icon: "ep:top",
      disabled: !hasSelection,
      action: "bringFront"
    },
    {
      id: "send-back",
      label: "置于底层",
      icon: "ep:bottom",
      disabled: !hasSelection,
      action: "sendBack"
    },
    { separator: true },
    {
      id: "auto-fit",
      label: "自适应边框",
      icon: "ep:operation",
      disabled: !hasSelection,
      action: "autoFit"
    }
  ];
});

// 开始仿真
const startSimulation = () => utils1.startSimulation(isSimulating, ElMessage);

// 停止仿真
const stopSimulation = () => utils1.stopSimulation(isSimulating, ElMessage);

// 从SVG路径中提取组件名称的辅助函数
const extractComponentNameFromPath = (svgPath: string): string =>
  utils1.extractComponentNameFromPath(svgPath);

// 返回项目列表
const goBack = () => utils1.goBack(isSaved, ElMessageBox, saveProject, router);

// ========== 项目保存/加载功能 ==========

/**
 * 保存项目
 */
const saveProject = () =>
  utilsProject.saveProject(loading, projectInfo, projectData, isSaved);

/**
 * 发布/取消发布项目
 */
const handlePublishProject = async () => {
  if (!projectInfo.value.SnowId) {
    ElMessage.warning("请先保存项目");
    return;
  }

  const newStatus = projectInfo.value.ProjectStatus === 1 ? 0 : 1;
  const success = await utilsProject.publishProject(
    Number(projectInfo.value.SnowId),
    newStatus
  );

  if (success) {
    projectInfo.value.ProjectStatus = newStatus;
  }
};

/**
 * 从数据库加载项目
 */
const loadProject = async (projectId: string) => {
  await utilsProject.loadProject(
    projectId,
    loading,
    projectInfo,
    projectData,
    isSaved,
    redrawCanvas,
    nextTick
  );
  // 持久化的数据集回灌编辑器列表(否则刷新后停留在mock数据,保存即丢)
  rehydrateDatasetList(projectData, datasetList);

  // 重建已保存组件的 DOM。redrawCanvas 只给"当前选中的组件"刷样式、不创建任何元素，
  // 组件 DOM 原本只在拖拽落画布那一刻创建过——因此打开已有项目会是一张白纸，存过的组态再也编辑不了。
  await nextTick();
  const canvasContent = editorContainer.value?.querySelector(".canvas-content");
  if (canvasContent) {
    canvasContent.innerHTML = "";
    (projectData.value.components || []).forEach((component: any) =>
      createComponentElement(component)
    );
  }
};

/**
 * 初始化新项目（已弃用 - 应从项目列表创建）
 */
const initializeNewProject = () => {
  utilsProject.initializeNewProject(projectInfo, projectData, isSaved);
};

// ========== 资源文件上传管理 ==========

/**
 * 处理图片上传并创建组件
 */
const handleImageUpload = (event: Event, position?: { x: number; y: number }) =>
  utilsProject.handleImageUpload(
    event,
    projectInfo,
    loading,
    addComponentToCanvas,
    position
  );

/**
 * 处理视频上传并创建组件
 */
const handleVideoUpload = (event: Event, position?: { x: number; y: number }) =>
  utilsProject.handleVideoUpload(
    event,
    projectInfo,
    loading,
    addComponentToCanvas,
    position
  );

/**
 * 处理SVG上传
 */
const handleSvgUpload = (event: Event) =>
  utilsProject.handleSvgUpload(event, projectInfo, loading);

// ========== 项目导入/导出功能 ==========

/**
 * 导出项目为.fuxa文件
 */
const handleExportProject = () => utilsProject.handleExportProject(projectInfo);

/**
 * 导入.fuxa项目文件
 */
const handleImportProject = () =>
  utilsProject.handleImportProject(loading, router);

// ========== 自动保存功能 ==========

// 自动保存配置
const autoSaveConfig = utilsProject.createAutoSaveConfig();

/**
 * 启动自动保存
 */
const startAutoSave = () =>
  utilsProject.startAutoSave(autoSaveConfig, isSaved, projectInfo, saveProject);

/**
 * 停止自动保存
 */
const stopAutoSave = () => utilsProject.stopAutoSave(autoSaveConfig);

/**
 * 监听离开页面前提示
 */
const handleBeforeUnload = utilsProject.handleBeforeUnload(isSaved);

// 编辑器模式状态
const currentEditorMode = ref("select");
const activeComponent = ref(null);

// 处理组件添加
const handleAddComponent = (
  component: any,
  position?: { x: number; y: number }
) =>
  utils1.handleAddComponent(
    component,
    position,
    createComponentInstance,
    projectData,
    isSaved,
    ElMessage
  );

// 处理组件激活模式
const handleActivateComponent = (component: any) => {
  return utils1.handleActivateComponent(
    component,
    currentEditorMode,
    activeComponent,
    setCanvasMode
  );
};

// 创建组件实例
const createComponentInstance = (
  component: any,
  position = { x: 100, y: 100 }
) => utils1.createComponentInstance(component, position, calculateSmartSize);

// 智能尺寸计算函数
const calculateSmartSize = (component: any) =>
  utils1.calculateSmartSize(component);

// 设置画布模式
const setCanvasMode = (mode: string) =>
  utils1.setCanvasMode(mode, editorContainer);

// 处理画布拖放
const handleCanvasDrop = (event: DragEvent) =>
  utils1.handleCanvasDrop(
    event,
    editorContainer,
    canvasZoom,
    ElMessage,
    createTextComponent,
    createComponentInstance,
    addComponentToCanvas,
    currentEditorMode,
    activeComponent,
    setCanvasMode,
    snapToGrid
  );

// 添加组件到画布
const addComponentToCanvas = (componentInstance: any) =>
  utils1.addComponentToCanvas(
    componentInstance,
    projectData,
    createComponentElement,
    editorContainer,
    isSaved,
    ElMessage,
    cleanupAbnormalSvgElements
  );

// 创建组件DOM元素
const createComponentElement = (component: any) =>
  utils1.createComponentElement(
    component,
    editorContainer,
    createPathElement,
    createLineElement,
    createImageElement,
    createIframeElement,
    createVideoElement,
    createWebcamElement,
    createButtonElement,
    createTableElement,
    createPaintBoardElement,
    createChartElement,
    setupComponentInteractions,
    extractComponentNameFromPath,
    applySvgStyles,
    applyStyleToElement,
    applyTransformToElement,
    updateSwitchAppearance,
    updateButtonAppearance,
    componentManager,
    createSvgComponent,
    nextTick
  );

// 应用SVG图标样式到组件元素
const applySvgStyles = (element: HTMLElement, component: any) =>
  utils1.applySvgStyles(element, component);

// 使组件可拖拽移动
const makeComponentDraggable = (element: HTMLElement, component: any) =>
  utils2.makeComponentDraggable(
    element,
    component,
    canvasZoom,
    projectData,
    isSaved,
    updatePathSVGDuringDrag,
    createUpdatedPathSVG,
    snapToGrid
  );

// 拖拽过程中更新路径SVG
const updatePathSVGDuringDrag = (
  element: HTMLElement,
  deltaX: number,
  deltaY: number
) => utils2.updatePathSVGDuringDrag(element, deltaX, deltaY);

// 创建更新后的路径SVG
const createUpdatedPathSVG = (pathComponent: any) =>
  utils2.createUpdatedPathSVG(pathComponent);

// 画布拖拽悬停
const handleCanvasDragOver = (event: DragEvent) =>
  utils3.handleCanvasDragOver(event);

// 画布拖拽离开
const handleCanvasDragLeave = (event: DragEvent) =>
  utils3.handleCanvasDragLeave(event);

// 处理拖拽结束
const handleCanvasDragEnd = (event: DragEvent) =>
  utils3.handleCanvasDragEnd(event);

// 处理直线工具的点击绘制
const handleLineToolClick = (
  event: MouseEvent,
  scaledX: number,
  scaledY: number
) =>
  utils3.handleLineToolClick(
    event,
    scaledX,
    scaledY,
    lineDrawingState,
    canvasZoom,
    createTempLine,
    handleLineDraw,
    removeTempLine,
    createLineComponent,
    addComponentToCanvas,
    resetLineDrawingState,
    currentEditorMode,
    activeComponent,
    setCanvasMode,
    ElMessage
  );

// 创建临时预览线
const createTempLine = (canvas: HTMLElement, startX: number, startY: number) =>
  utils3.createTempLine(canvas, startX, startY, lineDrawingState);

// 移除临时预览线
const removeTempLine = () => utils3.removeTempLine(lineDrawingState);

// 处理直线绘制时的鼠标移动
const handleLineDraw = (event: MouseEvent) =>
  utils3.handleLineDraw(event, lineDrawingState, canvasZoom);

// 创建直线组件
const createLineComponent = (
  startPoint: { x: number; y: number },
  endPoint: { x: number; y: number }
) => utils3.createLineComponent(startPoint, endPoint);

// 重置直线绘制状态
const resetLineDrawingState = () =>
  utils3.resetLineDrawingState(lineDrawingState);

// 处理图像工具的点击上传
const handleImageToolClick = (
  event: MouseEvent,
  scaledX: number,
  scaledY: number
) =>
  utils3.handleImageToolClick(
    event,
    scaledX,
    scaledY,
    createImageComponent,
    addComponentToCanvas,
    currentEditorMode,
    activeComponent,
    setCanvasMode,
    ElMessage
  );

// 处理文本工具的点击创建
const handleTextToolClick = (
  event: MouseEvent,
  scaledX: number,
  scaledY: number
) =>
  utils3.handleTextToolClick(
    event,
    scaledX,
    scaledY,
    createTextComponent,
    addComponentToCanvas,
    currentEditorMode,
    activeComponent,
    setCanvasMode,
    ElMessage
  );

// 创建文本组件
const createTextComponent = (position: { x: number; y: number }) =>
  utils3.createTextComponent(position);

// 处理iframe工具的点击创建
const handleIframeToolClick = (
  event: MouseEvent,
  scaledX: number,
  scaledY: number
) =>
  utils3.handleIframeToolClick(
    event,
    scaledX,
    scaledY,
    createIframeComponent,
    addComponentToCanvas,
    currentEditorMode,
    activeComponent,
    setCanvasMode,
    ElMessage
  );

// 创建iframe组件
const createIframeComponent = (position: { x: number; y: number }) =>
  utils3.createIframeComponent(position);

// 处理视频工具的点击创建
const handleVideoToolClick = (
  event: MouseEvent,
  scaledX: number,
  scaledY: number
) =>
  utils3.handleVideoToolClick(
    event,
    scaledX,
    scaledY,
    createVideoComponent,
    addComponentToCanvas,
    currentEditorMode,
    activeComponent,
    setCanvasMode,
    ElMessage
  );

// 创建视频组件
const createVideoComponent = (position: { x: number; y: number }) =>
  utils3.createVideoComponent(position);

// 处理表格工具的点击创建
const handleTableToolClick = (
  event: MouseEvent,
  scaledX: number,
  scaledY: number
) =>
  utils3.handleTableToolClick(
    event,
    scaledX,
    scaledY,
    createTableComponent,
    addComponentToCanvas,
    currentEditorMode,
    activeComponent,
    setCanvasMode,
    ElMessage
  );

// 创建表格组件
const createTableComponent = (position: { x: number; y: number }) =>
  utils3.createTableComponent(position);

// 处理画板工具的点击创建
const handlePaintBoardToolClick = (
  event: MouseEvent,
  scaledX: number,
  scaledY: number
) =>
  utils3.handlePaintBoardToolClick(
    event,
    scaledX,
    scaledY,
    createPaintBoardComponent,
    addComponentToCanvas,
    currentEditorMode,
    activeComponent,
    setCanvasMode,
    ElMessage
  );

// 创建画板组件
const createPaintBoardComponent = (position: { x: number; y: number }) =>
  utils3.createPaintBoardComponent(position);

// 创建图像组件
const createImageComponent = (
  position: { x: number; y: number },
  fileName: string,
  imageDataUrl: string,
  displayWidth: number,
  displayHeight: number,
  originalWidth: number,
  originalHeight: number
) =>
  utils3.createImageComponent(
    position,
    fileName,
    imageDataUrl,
    displayWidth,
    displayHeight,
    originalWidth,
    originalHeight
  );

// 处理图表工具的点击创建
const handleChartToolClick = (
  event: MouseEvent,
  scaledX: number,
  scaledY: number,
  componentType: any
) =>
  utils3.handleChartToolClick(
    event,
    scaledX,
    scaledY,
    componentType,
    createChartComponent,
    addComponentToCanvas,
    currentEditorMode,
    activeComponent,
    setCanvasMode,
    ElMessage
  );

// 创建图表组件
const createChartComponent = (
  position: { x: number; y: number },
  componentType: any
) => utils3.createChartComponent(position, componentType);

// 创建直线DOM元素
const createLineElement = (component: any, canvasContent: Element) =>
  utils3.createLineElement(
    component,
    canvasContent,
    setupComponentInteractions
  );

// 创建路径DOM元素
const createPathElement = (component: any, canvasContent: Element) =>
  utils3.createPathElement(
    component,
    canvasContent,
    pathTool,
    setupPathComponentInteractions
  );

// 创建图像DOM元素
const createImageElement = (component: any, canvasContent: Element) =>
  utils3.createImageElement(
    component,
    canvasContent,
    setupComponentInteractions
  );

// 创建iframe DOM元素
const createIframeElement = (component: any, canvasContent: Element) =>
  utils3.createIframeElement(
    component,
    canvasContent,
    setupComponentInteractions
  );

// 创建视频 DOM元素
const createVideoElement = (component: any, canvasContent: Element) =>
  utils3.createVideoElement(
    component,
    canvasContent,
    setupComponentInteractions
  );

// 创建摄像头 DOM元素
const createWebcamElement = (component: any, canvasContent: Element) =>
  utils3.createWebcamElement(
    component,
    canvasContent,
    setupComponentInteractions
  );

// 创建画板 DOM元素
const createPaintBoardElement = (component: any, canvasContent: Element) =>
  utils3.createPaintBoardElement(
    component,
    canvasContent,
    setupComponentInteractions,
    selectComponent
  );

// 创建按钮 DOM元素
const createButtonElement = (component: any, canvasContent: Element) =>
  utilsButton.createButtonElement(
    component,
    canvasContent,
    setupComponentInteractions
  );

// 创建表格 DOM元素
const createTableElement = (component: any, canvasContent: Element) =>
  utils3.createTableElement(
    component,
    canvasContent,
    setupComponentInteractions
  );

// 创建图表DOM元素
const createChartElement = (component: any, canvasContent: Element) =>
  utils3.createChartElement(
    component,
    canvasContent,
    setupComponentInteractions,
    showChartConfigDialog,
    initEChart
  );

// 初始化ECharts图表
const initEChart = (container: HTMLElement, component: any) =>
  utils3.initEChart(container, component, echarts);

// 生成图表配置选项
const generateChartOptions = (chartConfig: any) =>
  utils3.generateChartOptions(chartConfig);

// 显示图表配置对话框
const showChartConfigDialog = (component: any) => {
  currentChartComponent.value = component;

  // 复制当前图表配置到表单数据
  chartConfigData.value = {
    type: component.chartConfig.type,
    title: component.chartConfig.title,
    dataSource: component.chartConfig.dataSource,
    staticData: [...component.chartConfig.staticData],
    apiConfig: { ...component.chartConfig.apiConfig },
    mqttConfig: { ...component.chartConfig.mqttConfig },
    refreshInterval: component.chartConfig.refreshInterval,
    theme: component.chartConfig.theme
  };

  chartConfigVisible.value = true;
  ElMessage.info("图表数据源配置");
};

// 添加静态数据项
const addStaticDataItem = () => {
  chartConfigData.value.staticData.push({
    name: `数据项${chartConfigData.value.staticData.length + 1}`,
    value: 0
  });
};

// 移除静态数据项
const removeStaticDataItem = (index: number) => {
  if (chartConfigData.value.staticData.length > 1) {
    chartConfigData.value.staticData.splice(index, 1);
  } else {
    ElMessage.warning("至少需要保留一个数据项");
  }
};

// 保存图表配置
const saveChartConfig = () =>
  utils2.saveChartConfig(
    currentChartComponent,
    chartConfigData,
    chartConfigVisible,
    isSaved,
    initEChart,
    ElMessage
  );

const onDeviceChange = () => {
  if (!selectedCanvasComponent.value) return;

  // 当设备改变时，重置相关的绑定配置
  componentFormData.value.paramcode = "";
  componentFormData.value.targetProperty = "text";
  componentFormData.value.bindingMode = "direct";
  componentFormData.value.valueTransform = "";
  componentFormData.value.conditions = [];
  componentFormData.value.actions = [];

  updateComponentBinding();
  ElMessage.info("设备已更换，请重新选择设备属性");
};

const onParamcodeChange = () => {
  updateComponentBinding();
};

const getCurrentDeviceAttributes = () => {
  const device = deviceList.value.find(
    d => d.id === componentFormData.value.deviceId
  );
  return device ? device.attributes : [];
};

const onBindingModeChange = () => {
  // 根据绑定模式初始化相应的配置
  if (componentFormData.value.bindingMode === "conditional") {
    // 条件模式下，如果没有条件则添加一个默认条件
    if (
      !componentFormData.value.conditions ||
      componentFormData.value.conditions.length === 0
    ) {
      addCondition();
    }
    if (
      !componentFormData.value.actions ||
      componentFormData.value.actions.length === 0
    ) {
      addBindingAction();
    }
  }
  updateComponentBinding();
};

const updateComponentBinding = () =>
  utils2.updateComponentBinding(
    selectedCanvasComponent,
    componentFormData,
    isSaved
  );

// 条件管理方法
const addCondition = () =>
  utils2.addCondition(componentFormData, updateComponentBinding);
const removeCondition = (index: number) =>
  utils2.removeCondition(index, componentFormData, updateComponentBinding);

const addBindingAction = () =>
  utils2.addBindingAction(componentFormData, updateComponentBinding);
const removeBindingAction = (index: number) =>
  utils2.removeBindingAction(index, componentFormData, updateComponentBinding);

// 绑定预览方法
const previewBinding = () =>
  utils2.previewBinding(componentFormData, deviceList, ElMessage);
const getBindingPreview = () => utils2.getBindingPreview(componentFormData);
const getBindingModeLabel = () => utils2.getBindingModeLabel(componentFormData);

// 获取当前绑定的设备名称
const getCurrentDeviceName = () => {
  const device = deviceList.value.find(
    d => d.id === componentFormData.value.deviceId
  );
  return device ? device.name : "未知设备";
};

// 获取当前绑定的参数名称
const getCurrentParamcodeName = () => {
  const device = deviceList.value.find(
    d => d.id === componentFormData.value.deviceId
  );
  if (!device) return "未知属性";

  const attribute = device.attributes.find(
    attr => attr.paramcode === componentFormData.value.paramcode
  );
  return attribute
    ? `${attribute.paramname} (${attribute.paramcode})`
    : "未知属性";
};

const previewDataBinding = async () => {
  if (!componentFormData.value.datasetId) {
    ElMessage.warning("请先选择数据集");
    return;
  }

  try {
    // 查找对应的数据集配置
    const dataset = datasetList.value.find(
      ds => ds.id === componentFormData.value.datasetId
    );
    if (!dataset) {
      ElMessage.error("找不到对应的数据集");
      return;
    }

    // 模拟获取数据（实际项目中这里应该调用真实的数据获取逻辑）
    let mockData = null;

    if (dataset.type === "api") {
      // 模拟API数据
      mockData = {
        sensors: [
          { id: 1, name: "温度传感器", value: 25.6, unit: "°C" },
          { id: 2, name: "湿度传感器", value: 68.3, unit: "%" }
        ],
        timestamp: new Date().toISOString()
      };
    } else if (dataset.type === "mqtt") {
      // 模拟MQTT数据
      mockData = {
        value: 42.5,
        status: "online",
        timestamp: new Date().toISOString()
      };
    } else if (dataset.type === "iot") {
      // IoT点位数据集走平台真实最新值
      mockData = await previewIotDataset(dataset);
    } else if (dataset.type === "static") {
      // 使用静态数据
      mockData = dataset.data;
    }

    // 存储预览数据
    componentFormData.value.previewData = mockData;

    // 如果是样式绑定，应用样式映射预览
    if (componentFormData.value.bindingType === "style") {
      applyStyleMappingPreview(mockData);
    }

    ElMessage.success("数据预览已刷新");
  } catch (error) {
    ElMessage.error("数据预览失败: " + (error as Error).message);
  }
};

const getPreviewValue = () => {
  if (
    !componentFormData.value.previewData ||
    !componentFormData.value.dataPath
  ) {
    return "暂无数据";
  }

  try {
    // 简单的JSON路径解析
    const pathSegments = componentFormData.value.dataPath.split(".");
    let value = componentFormData.value.previewData;

    for (const segment of pathSegments) {
      // 处理数组索引，如 sensors[0]
      const arrayMatch = segment.match(/(.+)\[(\d+)\]/);
      if (arrayMatch) {
        const [, arrayName, index] = arrayMatch;
        value = value[arrayName]?.[parseInt(index)];
      } else {
        value = value[segment];
      }

      if (value === undefined) {
        return "路径不存在";
      }
    }

    // 格式化显示
    if (componentFormData.value.dataFormat && typeof value !== "object") {
      return componentFormData.value.dataFormat.replace(
        "{value}",
        value.toString()
      );
    }

    return typeof value === "object"
      ? JSON.stringify(value, null, 2)
      : String(value);
  } catch (error) {
    return "数据解析错误";
  }
};

const addStyleMapping = () => {
  if (!componentFormData.value.styleMappings) {
    componentFormData.value.styleMappings = [];
  }

  componentFormData.value.styleMappings.push({
    condition: "数据值",
    operator: "==",
    value: "",
    targetValue: "#67c23a"
  });

  updateDataBinding();
};

const removeStyleMapping = (index: number) => {
  if (componentFormData.value.styleMappings) {
    componentFormData.value.styleMappings.splice(index, 1);
    updateDataBinding();
  }
};

// 快速绑定模板定义
const bindingTemplates = ref([
  {
    name: "温度显示",
    config: {
      bindingType: "value",
      targetProperty: "text",
      dataPath: "temperature",
      dataFormat: "{value}°C"
    }
  },
  {
    name: "状态指示灯",
    config: {
      bindingType: "style",
      styleProperty: "backgroundColor",
      dataPath: "status"
    }
  },
  {
    name: "报警可见性",
    config: { bindingType: "visibility", visibilityCondition: "alarm > 0" }
  }
]);

const applyBindingTemplate = (template: any) => {
  if (!selectedCanvasComponent.value) return;

  // 应用模板配置
  Object.assign(componentFormData.value, template.config);

  // 更新绑定
  updateDataBinding();

  ElMessage.success(`已应用绑定模板: ${template.name}`);
};

// 应用样式映射预览
const applyStyleMappingPreview = (data: any) => {
  if (!selectedCanvasComponent.value || !componentFormData.value.dataPath)
    return;

  try {
    // 解析数据值
    const pathSegments = componentFormData.value.dataPath.split(".");
    let value = data;

    for (const segment of pathSegments) {
      const arrayMatch = segment.match(/(.+)\[(\d+)\]/);
      if (arrayMatch) {
        const [, arrayName, index] = arrayMatch;
        value = value[arrayName]?.[parseInt(index)];
      } else {
        value = value[segment];
      }

      if (value === undefined) return;
    }

    // 查找匹配的样式映射
    const styleMappings = componentFormData.value.styleMappings || [];
    for (const mapping of styleMappings) {
      if (evaluateCondition(value, mapping.operator, mapping.value)) {
        // 应用样式到组件预览
        const element = document.getElementById(
          selectedCanvasComponent.value.id
        );
        if (element && componentFormData.value.styleProperty) {
          element.style[componentFormData.value.styleProperty] =
            mapping.targetValue;
        }
        break;
      }
    }
  } catch (error) {}
};

// 评估条件
const evaluateCondition = (
  dataValue: any,
  operator: string,
  compareValue: any
): boolean => {
  const numericDataValue = parseFloat(dataValue);
  const numericCompareValue = parseFloat(compareValue);

  if (!isNaN(numericDataValue) && !isNaN(numericCompareValue)) {
    switch (operator) {
      case ">":
        return numericDataValue > numericCompareValue;
      case "<":
        return numericDataValue < numericCompareValue;
      case ">=":
        return numericDataValue >= numericCompareValue;
      case "<=":
        return numericDataValue <= numericCompareValue;
      case "==":
        return numericDataValue === numericCompareValue;
      default:
        return false;
    }
  } else {
    // 字符串比较
    switch (operator) {
      case "==":
        return String(dataValue) === String(compareValue);
      default:
        return false;
    }
  }
};

// 数据集配置管理方法
const handleSaveDatasetConfig = async (config: any) => {
  // 生成数据集ID
  if (!config.id) {
    config.id = `dataset_${Date.now()}`;
  }

  // 如果是MQTT类型,连接并订阅主题
  if (config.type === 'mqtt') {
    try {
      // 确保MQTT服务已连接
      if (fuxaMqttService.status.value !== 'connected') {
        // 使用完整的 MqttConfig 对象连接
        await fuxaMqttService.connect({
          host: config.host,
          clientId: config.clientId || `dataset_mqtt_${Date.now()}`,
          username: config.username,
          password: config.password,
          dataTopic: config.dataTopic,
          alarmTopic: config.alarmTopic,
          qos: config.qos
        });
        
        ElMessage.success('MQTT连接并订阅成功');
      } else {
        // 已连接,只订阅新主题
        const qos = parseInt(config.qos) as 0 | 1 | 2;
        const topics = [];

        if (config.dataTopic) {
          await fuxaMqttService.subscribeCustomTopic(config.dataTopic, qos);
          topics.push(config.dataTopic);
        }

        if (config.alarmTopic) {
          await fuxaMqttService.subscribeCustomTopic(config.alarmTopic, qos);
          topics.push(config.alarmTopic);
        }

        if (topics.length > 0) {
          ElMessage.success(`已订阅MQTT主题: ${topics.join(', ')}`);
        }
      }
    } catch (error) {
      ElMessage.error(`MQTT连接失败: ${error instanceof Error ? error.message : '未知错误'}`);
      console.error('MQTT连接失败:', error);
    }
  }

  // 添加到数据集列表
  const existingIndex = datasetList.value.findIndex(ds => ds.id === config.id);
  if (existingIndex > -1) {
    datasetList.value[existingIndex] = config;
  } else {
    datasetList.value.push(config);
  }

  // 保存到项目数据
  if (projectData.value) {
    if (!projectData.value.datasets) {
      projectData.value.datasets = [];
    }
    projectData.value.datasets = [...datasetList.value];
  }

  isSaved.value = false;
  ElMessage.success("数据集配置已保存");
};

const handleTestDataset = async (config: any) => {
  ElMessage.info("正在测试数据集连接...");

  try {
    if (config.type === 'api') {
      // 测试API连接
      // TODO: 实际调用API接口测试
      ElMessage.success("API连接测试成功");
    } else if (config.type === 'mqtt') {
      // 测试MQTT连接(使用完整的 MqttConfig 对象)
      await fuxaMqttService.connect({
        host: config.host,
        clientId: `test_mqtt_${Date.now()}`,
        username: config.username,
        password: config.password,
        dataTopic: config.dataTopic,
        alarmTopic: config.alarmTopic,
        qos: config.qos
      });
      ElMessage.success("MQTT连接测试成功");
    } else if (config.type === 'static') {
      // 静态数据无需测试
      ElMessage.success("静态数据配置有效");
    }
  } catch (error) {
    ElMessage.error(`连接测试失败: ${error instanceof Error ? error.message : '未知错误'}`);
    console.error('数据集测试失败:', error);
  }
};

// 处理画布点击
const handleCanvasClick = (event: MouseEvent) =>
  utils3.handleCanvasClick(
    event,
    currentEditorMode,
    activeComponent,
    hideContextMenu,
    canvasZoom,
    handleLineToolClick,
    handleImageToolClick,
    handleTextToolClick,
    handleIframeToolClick,
    handleVideoToolClick,
    handleTableToolClick,
    handleChartToolClick,
    createComponentInstance,
    addComponentToCanvas,
    setCanvasMode,
    removeResizeHandles,
    selectedCanvasComponent,
    snapToGrid
  );

// 选择组件
const selectComponent = (component: any) =>
  utils3.selectCanvasComponent(
    component,
    selectedCanvasComponent,
    removeResizeHandles,
    addResizeHandles,
    ElMessage
  );

// 切换面板显示
const togglePanel = (panel: "component" | "property") => {
  if (panel === "component") {
    showComponentPanel.value = !showComponentPanel.value;
  } else {
    showPropertyPanel.value = !showPropertyPanel.value;
  }
};

// 获取模式显示名称
const getModeDisplayName = (mode: string) => {
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
    thermometer: "温度计",
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
const getCurrentViewComponentCount = () => {
  return projectData.value?.components?.length || 0;
};

// 键盘快捷键处理
const handleKeydown = (event: KeyboardEvent) =>
  utils3.handleKeydown(
    event,
    currentEditorMode,
    pathTool,
    lineDrawingState,
    removeTempLine,
    resetLineDrawingState,
    activeComponent,
    setCanvasMode,
    selectedCanvasComponent,
    deleteSelectedComponent,
    handleCopyComponent,
    handleCutComponent,
    handlePasteComponent,
    clipboardData
  );

// 删除选择的组件
const deleteSelectedComponent = () => {
  if (!selectedCanvasComponent.value) return;

  const componentId = selectedCanvasComponent.value.id;

  // 从项目数据中移除
  if (projectData.value?.components) {
    const index = projectData.value.components.findIndex(
      comp => comp.id === componentId
    );
    if (index > -1) {
      projectData.value.components.splice(index, 1);
    }
  }

  // 从DOM中移除
  const element = document.getElementById(componentId);
  if (element) {
    element.remove();
  }

  selectedCanvasComponent.value = null;
  isSaved.value = false;
  ElMessage.success("已删除组件");
};

// 右键菜单处理函数（画布空白区域）
const handleContextMenu = (event: MouseEvent) => {
  // 只有点击到画布本身才显示菜单（不是组件）
  if (
    event.target === event.currentTarget ||
    (event.target as HTMLElement).classList.contains("canvas-content") ||
    (event.target as HTMLElement).classList.contains("canvas-background") ||
    (event.target as HTMLElement).classList.contains("fuxa-canvas")
  ) {
    event.preventDefault();
    event.stopPropagation();

    // 设置菜单位置
    contextMenuPosition.x = event.clientX;
    contextMenuPosition.y = event.clientY;

    // 先显示菜单
    contextMenuVisible.value = true;

    // 然后在下一帧调整菜单位置(确保菜单已经渲染)
    nextTick(() => {
      adjustMenuPosition();
    });
  }
};

// 调整菜单位置
const adjustMenuPosition = (componentRect?: DOMRect) =>
  utils3.adjustMenuPosition(contextMenuPosition, componentRect);

// 隐藏右键菜单
const hideContextMenu = () => {
  contextMenuVisible.value = false;
};

// 菜单项点击处理
const handleMenuClick = (item: any) => {
  if (item.disabled) return;

  switch (item.action) {
    case "propertyEdit":
      handlePropertyEdit();
      break;
    case "chartConfig":
      showChartPropertyDialog();
      break;
    case "iframeConfig":
      showIframeConfigDialog(selectedCanvasComponent.value);
      break;
    case "videoConfig":
      showVideoConfigDialog(selectedCanvasComponent.value);
      break;
    case "webcamConfig":
      showWebcamConfigDialog(selectedCanvasComponent.value);
      break;
    case "tableConfig":
      showTableConfigDialog(selectedCanvasComponent.value);
      break;
    case "thermometerConfig":
      thermometerConfigVisible.value = true;
      break;
    case "textCardConfig":
      showTextCardConfigDialog(selectedCanvasComponent.value);
      break;
    case "binding":
      handleDataBinding();
      break;
    case "cut":
      handleCutComponent();
      break;
    case "copy":
      handleCopyComponent();
      break;
    case "paste":
      handlePasteComponent();
      break;
    case "delete":
      deleteSelectedComponent();
      break;
    case "bringFront":
      handleBringToFront();
      break;
    case "sendBack":
      handleSendToBack();
      break;
    case "autoFit":
      autoFitToIcon();
      break;
  }

  hideContextMenu();
};

// 属性编辑弹框
const handlePropertyEdit = () => {
  if (!selectedCanvasComponent.value) return;

  propertyDialogVisible.value = true;
  ElMessage.success("属性编辑弹框已打开");
};

// 数据绑定
const handleDataBinding = () => {
  if (!selectedCanvasComponent.value) return;

  // 这里可以打开数据绑定对话框
  ElMessage.info("数据绑定功能待实现");
};

// 剪切组件
const handleCutComponent = () => {
  if (!selectedCanvasComponent.value) return;

  // 复制到剪贴板
  clipboardData.value = {
    ...selectedCanvasComponent.value,
    id: `${selectedCanvasComponent.value.type}_${Date.now()}` // 生成新ID
  };

  // 删除原组件
  deleteSelectedComponent();
  ElMessage.success("组件已剪切");
};

// 复制组件
const handleCopyComponent = () => {
  if (!selectedCanvasComponent.value) return;

  clipboardData.value = {
    ...selectedCanvasComponent.value,
    id: `${selectedCanvasComponent.value.type}_${Date.now()}` // 生成新ID
  };

  ElMessage.success("组件已复制");
};

// 粘贴组件
const handlePasteComponent = () => {
  if (!clipboardData.value) return;

  // 创建新组件实例，位置稍微偏移
  const newComponent = {
    ...clipboardData.value,
    id: `${clipboardData.value.type}_${Date.now()}`,
    position: {
      x: clipboardData.value.position.x + 20,
      y: clipboardData.value.position.y + 20
    }
  };

  // 添加到画布
  addComponentToCanvas(newComponent);

  // 选中新组件
  selectComponent(newComponent);

  ElMessage.success("组件已粘贴");
};

// 置于顶层
const handleBringToFront = () => {
  if (!selectedCanvasComponent.value) return;

  const element = document.getElementById(selectedCanvasComponent.value.id);
  if (element) {
    element.style.zIndex = "1000";
  }

  ElMessage.success("组件已置于顶层");
};

// 置于底层
const handleSendToBack = () => {
  if (!selectedCanvasComponent.value) return;

  const element = document.getElementById(selectedCanvasComponent.value.id);
  if (element) {
    element.style.zIndex = "1";
  }

  ElMessage.success("组件已置于底层");
};

// 显示图表属性对话框
const showChartPropertyDialog = () => {
  if (
    !selectedCanvasComponent.value ||
    !selectedCanvasComponent.value.chartConfig
  ) {
    ElMessage.warning("请选择一个图表组件");
    return;
  }

  currentChartComponent.value = selectedCanvasComponent.value;
  chartPropertyDialogVisible.value = true;
  ElMessage.info("图表属性配置");
};

// 保存图表属性配置
const handleSaveChartProperty = (config: any) =>
  utils4.handleSaveChartProperty(
    config,
    currentChartComponent.value,
    initEChart,
    setupChartDataRefresh,
    isSaved,
    ElMessage
  );

// 显示iframe配置对话框
const showIframeConfigDialog = (component: any) => {
  currentIframeComponent.value = component;
  iframeConfigDialogVisible.value = true;
  ElMessage.info("内嵌网页配置");
};

// 保存iframe配置
const handleSaveIframeConfig = (config: any) =>
  utils4.handleSaveIframeConfig(
    config,
    currentIframeComponent.value,
    isSaved,
    ElMessage
  );

// 显示视频配置对话框
const showVideoConfigDialog = (component: any) => {
  currentVideoComponent.value = component;
  videoConfigDialogVisible.value = true;
  ElMessage.info("视频配置");
};

// 显示摄像头配置对话框
const showWebcamConfigDialog = (component: any) => {
  currentWebcamComponent.value = component;
  webcamConfigDialogVisible.value = true;
  ElMessage.info("摄像头配置");
};

// 显示表格配置对话框
const showTableConfigDialog = (component: any) => {
  currentTableComponent.value = component;
  tableConfigDialogVisible.value = true;
  ElMessage.info("表格配置");
};

// 显示文本卡片配置对话框
const showTextCardConfigDialog = (component: any) => {
  currentTextCardComponent.value = component;
  textCardConfigDialogVisible.value = true;
  ElMessage.info("文本卡片配置");
};

// 保存文本卡片配置
const handleSaveTextCardConfig = (config: any) => {
  if (!currentTextCardComponent.value) return;

  // 更新组件的 properties
  currentTextCardComponent.value.properties = {
    ...currentTextCardComponent.value.properties,
    ...config
  };

  // 使用 ComponentManager 更新组件
  componentManager.updateComponent(currentTextCardComponent.value.id, config);

  // 标记未保存
  isSaved.value = false;

  ElMessage.success("文本卡片配置已更新");
};

// 保存视频配置
const handleSaveVideoConfig = (config: any) =>
  utils4.handleSaveVideoConfig(
    config,
    currentVideoComponent.value,
    isSaved,
    ElMessage
  );

// 保存摄像头配置
const handleSaveWebcamConfig = (config: any) =>
  utils4.handleSaveWebcamConfig(
    config,
    currentWebcamComponent.value,
    isSaved,
    ElMessage
  );

// 保存表格配置
const handleSaveTableConfig = (config: any) =>
  utils4.handleSaveTableConfig(
    config,
    currentTableComponent.value,
    createTableElement,
    setupTableDataRefresh,
    editorContainer,
    isSaved,
    ElMessage
  );

// 保存温度计配置
const handleSaveThermometerConfig = (config: any) => {
  if (
    !selectedCanvasComponent.value ||
    selectedCanvasComponent.value.type !== "thermometer"
  )
    return;

  try {
    // 更新组件 properties
    if (!selectedCanvasComponent.value.properties) {
      selectedCanvasComponent.value.properties = {};
    }

    // 分别更新 value 和 options
    if (config.value !== undefined) {
      selectedCanvasComponent.value.properties.value = config.value;
    }

    // 检测方向是否改变
    const oldOrientation =
      selectedCanvasComponent.value.properties.options?.orientation ||
      "vertical";
    const newOrientation = config.options?.orientation || oldOrientation;

    if (config.options) {
      selectedCanvasComponent.value.properties.options = {
        ...selectedCanvasComponent.value.properties.options,
        ...config.options
      };

      // 画板配置保存处理
    }

    // 如果方向改变，自动交换宽高
    if (oldOrientation !== newOrientation) {
      const oldWidth = selectedCanvasComponent.value.size.width;
      const oldHeight = selectedCanvasComponent.value.size.height;

      selectedCanvasComponent.value.size.width = oldHeight;
      selectedCanvasComponent.value.size.height = oldWidth;
      selectedCanvasComponent.value.width = oldHeight;
      selectedCanvasComponent.value.height = oldWidth;

      console.log("🔄 方向改变，交换宽高:", {
        from: `${oldWidth}x${oldHeight}`,
        to: `${oldHeight}x${oldWidth}`
      });
    }

    console.log("🌡️ 更新温度计配置:", {
      value: selectedCanvasComponent.value.properties.value,
      options: selectedCanvasComponent.value.properties.options,
      size: selectedCanvasComponent.value.size
    });

    // 移除旧元素
    const element = document.getElementById(selectedCanvasComponent.value.id);
    if (element) {
      element.remove();
    }

    // 重新创建温度计元素
    const canvasContent =
      editorContainer.value?.querySelector(".canvas-content");
    if (canvasContent) {
      const newElement = utils1.createFuxaSliderElement(
        selectedCanvasComponent.value,
        canvasContent,
        setupComponentInteractions
      );

      if (newElement) {
        // 更新外观
        setTimeout(() => {
          utils1.updateFuxaSliderAppearance(
            selectedCanvasComponent.value,
            newElement
          );
        }, 0);
      }
    }

    isSaved.value = false;
    ElMessage.success("温度计配置已更新");
  } catch (error) {
    console.error("保存温度计配置失败:", error);
    ElMessage.error("温度计配置保存失败");
  }
};

// 设置表格数据刷新
const setupTableDataRefresh = (component: any) =>
  utils4.setupTableDataRefresh(
    component,
    datasetList,
    createTableElement,
    editorContainer
  );

// 设置图表数据刷新
const setupChartDataRefresh = (component: any) =>
  utils4.setupChartDataRefresh(component, datasetList, initEChart);

// 自适应边框到图标大小
const autoFitToIcon = () =>
  utils1.autoFitToIcon(
    selectedCanvasComponent,
    componentFormData,
    isSaved,
    removeResizeHandles,
    addResizeHandles,
    ElMessage
  );

// 属性面板处理函数
const clearSelectedComponent = () =>
  utils2.clearSelectedComponent(selectedCanvasComponent, removeResizeHandles);

// 监听选中组件变化，更新表单数据
const updateFormData = () =>
  utils1.updateFormData(selectedCanvasComponent, componentFormData);

// 格式化属性标签
const formatPropertyLabel = (key: string) => {
  const labelMap: Record<string, string> = {
    defaultValue: "默认值",
    placeholder: "占位符",
    text: "文本内容",
    value: "数值",
    unit: "单位",
    decimals: "小数位数",
    variableId: "变量ID"
  };
  return labelMap[key] || key;
};

// 更新组件基本属性
const updateComponentProperty = () => {
  if (!selectedCanvasComponent.value) return;

  selectedCanvasComponent.value.name = componentFormData.value.name;

  // 更新项目数据
  isSaved.value = false;
  ElMessage.success("组件属性已更新");
};

// 更新组件位置和尺寸
const updateComponentTransform = () =>
  utils1.updateComponentTransform(
    selectedCanvasComponent,
    componentFormData,
    isSaved,
    removeResizeHandles,
    addResizeHandles,
    nextTick,
    ElMessage
  );

// 对齐组件
const alignComponent = (alignment: string) =>
  utils2.alignComponent(
    alignment,
    selectedCanvasComponent,
    editorContainer,
    componentFormData,
    isSaved,
    ElMessage
  );

// 获取对齐名称
const getAlignmentName = (alignment: string) =>
  utils2.getAlignmentName(alignment);

// 更新组件交互属性
const updateComponentInteractivity = () =>
  utils2.updateComponentInteractivity(
    selectedCanvasComponent,
    componentFormData,
    isSaved,
    ElMessage
  );

// 更新组件形状属性
const updateComponentShape = () =>
  utils2.updateComponentShape(
    selectedCanvasComponent,
    componentFormData,
    isSaved,
    ElMessage
  );

// 更新组件样式
const updateComponentStyle = () =>
  utils2.updateComponentStyle(
    selectedCanvasComponent,
    componentFormData,
    isSaved,
    applySvgStyles,
    ElMessage
  );

// 更新组件属性
const updateComponentProperties = () => {
  if (!selectedCanvasComponent.value) return;

  selectedCanvasComponent.value.properties = {
    ...componentFormData.value.properties
  };

  isSaved.value = false;
  ElMessage.success("组件属性已更新");
};

// 事件配置相关函数
const getEventTypeName = (type: string) => {
  const typeNames: Record<string, string> = {
    click: "点击事件",
    dblclick: "双击事件",
    hover: "鼠标悬停",
    leave: "鼠标离开",
    valuechange: "数值变化",
    timer: "定时器",
    custom: "自定义"
  };
  return typeNames[type] || type;
};

const getActionTypeName = (type: string) => {
  const typeNames: Record<string, string> = {
    visibility: "显示/隐藏",
    color: "颜色变化",
    move: "位置移动",
    dialog: "弹出对话框",
    command: "发送命令",
    setValue: "设置数值",
    sound: "播放声音",
    script: "执行脚本"
  };
  return typeNames[type] || type;
};

// 添加事件
const addEvent = () => {
  if (!componentFormData.value.events) {
    componentFormData.value.events = [];
  }

  componentFormData.value.events.push({
    id: `event_${Date.now()}`,
    type: "click",
    enabled: true,
    actions: []
  });

  updateComponentEvents();
  ElMessage.success("已添加新事件");
};

// 删除事件
const removeEvent = (eventIndex: number) => {
  componentFormData.value.events.splice(eventIndex, 1);
  updateComponentEvents();
  ElMessage.success("已删除事件");
};

// 切换事件启用状态
const toggleEventEnabled = (eventIndex: number, enabled: boolean) => {
  componentFormData.value.events[eventIndex].enabled = enabled;
  updateComponentEvents();
};

// 更新事件类型
const updateEventType = (eventIndex: number, type: string) => {
  componentFormData.value.events[eventIndex].type = type;
  updateComponentEvents();
};

// 更新事件条件
const updateEventCondition = (eventIndex: number, condition: string) => {
  componentFormData.value.events[eventIndex].condition = condition;
  updateComponentEvents();
};

// 更新定时器间隔
const updateEventInterval = (eventIndex: number, interval: number) => {
  componentFormData.value.events[eventIndex].interval = interval;
  updateComponentEvents();
};

// 更新重复次数
const updateEventRepeatCount = (eventIndex: number, repeatCount: number) => {
  componentFormData.value.events[eventIndex].repeatCount = repeatCount;
  updateComponentEvents();
};

// 添加动作
const addAction = (eventIndex: number) => {
  if (!componentFormData.value.events[eventIndex].actions) {
    componentFormData.value.events[eventIndex].actions = [];
  }

  componentFormData.value.events[eventIndex].actions.push({
    id: `action_${Date.now()}`,
    type: "dialog",
    delay: 0,
    params: {}
  });

  updateComponentEvents();
  ElMessage.success("已添加新动作");
};

// 删除动作
const removeAction = (eventIndex: number, actionIndex: number) => {
  componentFormData.value.events[eventIndex].actions.splice(actionIndex, 1);
  updateComponentEvents();
  ElMessage.success("已删除动作");
};

// 更新动作类型
const updateActionType = (
  eventIndex: number,
  actionIndex: number,
  type: string
) => {
  const action =
    componentFormData.value.events[eventIndex].actions[actionIndex];
  action.type = type;
  action.params = {}; // 重置参数
  updateComponentEvents();
};

// 更新动作延迟
const updateActionDelay = (
  eventIndex: number,
  actionIndex: number,
  delay: number
) => {
  componentFormData.value.events[eventIndex].actions[actionIndex].delay = delay;
  updateComponentEvents();
};

// 更新动作参数
const updateActionParams = (
  eventIndex: number,
  actionIndex: number,
  params: any
) => {
  const currentParams =
    componentFormData.value.events[eventIndex].actions[actionIndex].params ||
    {};
  componentFormData.value.events[eventIndex].actions[actionIndex].params = {
    ...currentParams,
    ...params
  };
  updateComponentEvents();
};

// 获取动作配置组件
const getActionConfigComponent = (actionType: string) => {
  return "ActionConfigForm";
};

// 更新组件文字
const updateComponentText = () =>
  utils2.updateComponentText(
    selectedCanvasComponent,
    componentFormData,
    isSaved,
    ElMessage
  );

// 更新SVG图标样式
const updateSvgIconStyle = () =>
  utils2.updateSvgIconStyle(
    selectedCanvasComponent,
    componentFormData,
    svgManager,
    isSaved,
    ElMessage
  );

// 颜色转换工具函数
const hexToRgb = (hex: string) => utils2.hexToRgb(hex);
const rgbToHsl = (r: number, g: number, b: number) => utils2.rgbToHsl(r, g, b);

// 批量应用样式更新
const applyBulkStyleUpdates = (updates: Record<string, any>) => {
  if (!selectedCanvasComponent.value) return;

  const component = selectedCanvasComponent.value;
  const element = document.getElementById(component.id);

  if (element) {
    Object.entries(updates).forEach(([property, value]) => {
      if (property.startsWith("data-")) {
        element.setAttribute(property, value);
      } else {
        element.style[property as any] = value;
      }
    });
  }

  // 更新组件数据
  Object.assign(component, updates);
  isSaved.value = false;
};

const handlePropertyFormDataUpdate = (newFormData: any) => {
  if (!selectedCanvasComponent.value) return;

  // 更新表单数据 - 修复: 应该赋值给 componentFormData.value 而不是 componentFormData 本身
  Object.assign(componentFormData.value, newFormData);

  // 同步更新组件数据
  Object.assign(selectedCanvasComponent.value, newFormData);

  // 调用相应的更新函数
  updateComponentInteractivity();
  updateComponentShape();
  updateSvgIconStyle();

  isSaved.value = false;
  redrawCanvas();
};

const handleUpdateComponentProperty = (property: string, value: any) =>
  utils1.handleUpdateComponentProperty(
    property,
    value,
    selectedCanvasComponent,
    applyStyleToElement,
    updateSvgIconStyle,
    refreshComponentEvents,
    updateComponentInteractivity,
    updateComponentShape,
    redrawCanvas,
    isSaved,
    updateButtonAppearance,
    createComponentElement
  );

const handleUpdateProperty = (property: string, value: any) => {
  if (!selectedCanvasComponent.value) return;
  selectedCanvasComponent.value[property] = value;

  // 🔲 如果是按钮组件且修改了 properties 属性，立即更新按钮外观
  if (
    selectedCanvasComponent.value.type === "button" &&
    property === "properties"
  ) {
    const element = document.getElementById(selectedCanvasComponent.value.id);
    if (element) {
      updateButtonAppearance(selectedCanvasComponent.value, element);
    }
  }

  isSaved.value = false;
  redrawCanvas();
};

const handleUpdatePosition = (axis: string, value: number) => {
  if (!selectedCanvasComponent.value) return;
  if (!selectedCanvasComponent.value.position) {
    selectedCanvasComponent.value.position = { x: 0, y: 0 };
  }

  // 更新组件位置数据
  selectedCanvasComponent.value.position[axis] = value;

  // 同时更新兼容性属性
  if (axis === "x") {
    selectedCanvasComponent.value.x = value;
  } else if (axis === "y") {
    selectedCanvasComponent.value.y = value;
  }

  // 更新表单数据以保持同步
  if (componentFormData.value.position) {
    componentFormData.value.position[axis] = value;
  }

  // 立即更新DOM元素
  const element = document.getElementById(selectedCanvasComponent.value.id);
  if (element) {
    if (axis === "x") {
      element.style.left = value + "px";
    } else if (axis === "y") {
      element.style.top = value + "px";
    }
  }

  isSaved.value = false;
  redrawCanvas();
};

const handleUpdateSize = (dimension: string, value: number) => {
  if (!selectedCanvasComponent.value) return;
  if (!selectedCanvasComponent.value.size) {
    selectedCanvasComponent.value.size = { width: 100, height: 50 };
  }
  // 更新组件尺寸数据
  selectedCanvasComponent.value.size[dimension] = value;
  // 同时更新兼容性属性
  if (dimension === "width") {
    selectedCanvasComponent.value.width = value;
  } else if (dimension === "height") {
    selectedCanvasComponent.value.height = value;
  }

  // 更新表单数据以保持同步
  if (componentFormData.value.size) {
    componentFormData.value.size[dimension] = value;
  }

  // 立即更新DOM元素
  const element = document.getElementById(selectedCanvasComponent.value.id);
  if (element) {
    if (dimension === "width") {
      element.style.width = value + "px";
    } else if (dimension === "height") {
      element.style.height = value + "px";
    }
  }

  // ✨ LED 组件特殊处理: 属性面板修改尺寸后需要更新 SVG
  if (selectedCanvasComponent.value.type === 'led-display') {
    console.log('💡 LED 组件尺寸通过属性面板修改，触发 SVG 更新:', {
      componentId: selectedCanvasComponent.value.id,
      dimension,
      value,
      newSize: {
        width: selectedCanvasComponent.value.width,
        height: selectedCanvasComponent.value.height
      }
    })
    // 调用 LED 组件管理器更新实例
    ledDisplayManager.updateLedDisplayComponent(selectedCanvasComponent.value.id, {
      // 传递当前完整配置，触发 updateDisplay
      ...selectedCanvasComponent.value.properties
    })
  }

  isSaved.value = false;
  redrawCanvas();
};

const handleUpdateStyle = (property: string, value: any) =>
  utils2.handleUpdateStyle(
    property,
    value,
    selectedCanvasComponent,
    applySvgStyles,
    applyStyleToElement,
    redrawCanvas,
    isSaved
  );

const handleUpdateComponentTransform = () => {
  updateComponentTransform();
};

const handleAlignComponent = (alignment: string) => {
  alignComponent(alignment);
};

const handleUpdateText = (property: string, value: any) =>
  utils2.handleUpdateText(
    property,
    value,
    selectedCanvasComponent,
    componentFormData,
    updateComponentText,
    isSaved,
    updateButtonAppearance
  );

// 处理文本卡片配置更新
const handleUpdateTextCardConfig = (property: string, value: any) => {
  if (!selectedCanvasComponent.value) return;

  // 初始化 textCardConfig 对象（如果不存在）
  if (!selectedCanvasComponent.value.properties) {
    selectedCanvasComponent.value.properties = {};
  }
  if (!selectedCanvasComponent.value.properties.textCardConfig) {
    selectedCanvasComponent.value.properties.textCardConfig = {
      content: '这是一段文本内容\n可以支持多行显示',
      multiLine: true,
      maxLines: 10,
      lineHeight: 1.6,
      backgroundColor: '#ffffff',
      borderColor: '#d1d5db',
      borderWidth: 1,
      borderRadius: 4,
      shadow: {
        enabled: true,
        color: 'rgba(0,0,0,0.1)',
        offsetX: 2,
        offsetY: 2,
        blur: 4
      },
      textStyle: {
        fontFamily: "'Microsoft YaHei', sans-serif",
        fontSize: 14,
        fontWeight: 'normal',
        color: '#333333',
        textAlign: 'left'
      },
      padding: 16
    };
  }

  // 更新配置
  selectedCanvasComponent.value.properties.textCardConfig[property] = value;

  // 调用 TextCardComponentManager 更新组件
  textCardManager.updateTextCardComponent(
    selectedCanvasComponent.value.id,
    selectedCanvasComponent.value.properties
  );

  isSaved.value = false;
};

// 处理文本卡片阴影配置更新
const handleUpdateTextCardShadow = (property: string, value: any) => {
  if (!selectedCanvasComponent.value) return;

  // 初始化配置（如果不存在）
  if (!selectedCanvasComponent.value.properties) {
    selectedCanvasComponent.value.properties = {};
  }
  if (!selectedCanvasComponent.value.properties.textCardConfig) {
    selectedCanvasComponent.value.properties.textCardConfig = {
      content: '这是一段文本内容\n可以支持多行显示',
      multiLine: true,
      maxLines: 10,
      lineHeight: 1.6,
      backgroundColor: '#ffffff',
      borderColor: '#d1d5db',
      borderWidth: 1,
      borderRadius: 4,
      shadow: {
        enabled: true,
        color: 'rgba(0,0,0,0.1)',
        offsetX: 2,
        offsetY: 2,
        blur: 4
      },
      textStyle: {
        fontFamily: "'Microsoft YaHei', sans-serif",
        fontSize: 14,
        fontWeight: 'normal',
        color: '#333333',
        textAlign: 'left'
      },
      padding: 16
    };
  }
  if (!selectedCanvasComponent.value.properties.textCardConfig.shadow) {
    selectedCanvasComponent.value.properties.textCardConfig.shadow = {
      enabled: true,
      color: 'rgba(0,0,0,0.1)',
      offsetX: 2,
      offsetY: 2,
      blur: 4
    };
  }

  // 更新阴影配置
  selectedCanvasComponent.value.properties.textCardConfig.shadow[property] = value;

  // 调用 TextCardComponentManager 更新组件
  textCardManager.updateTextCardComponent(
    selectedCanvasComponent.value.id,
    selectedCanvasComponent.value.properties
  );

  isSaved.value = false;
};

// 处理图表配置更新
const handleUpdateChartConfig = (property: string, value: any) =>
  utils2.handleUpdateChartConfig(
    property,
    value,
    selectedCanvasComponent,
    componentFormData,
    isSaved,
    ElMessage
  );

// 统一处理SVG容器背景的方法
const applySvgContainerBackground = (
  element: HTMLElement,
  backgroundType: string,
  style: any
) => utils2.applySvgContainerBackground(element, backgroundType, style);

// 应用样式到DOM元素的方法
const applyStyleToElement = (component: any) =>
  utils1.applyStyleToElement(component);

// 画布重绘方法
const redrawCanvas = () =>
  utils2.redrawCanvas(
    selectedCanvasComponent,
    applyStyleToElement,
    applyTransformToElement,
    nextTick
  );

// 应用变换到DOM元素的方法
const applyTransformToElement = (component: any) =>
  utils2.applyTransformToElement(component);

// 应用条件样式
const applyConditionalStyles = (component: any) =>
  utils2.applyConditionalStyles(component);

// 更新组件事件
const updateComponentEvents = () =>
  utils2.updateComponentEvents(
    selectedCanvasComponent,
    componentFormData,
    isSaved
  );

// 测试事件
const testEvents = () =>
  utils2.testEvents(
    componentFormData,
    getEventTypeName,
    getActionTypeName,
    ElMessage
  );

// 初始化FUXA编辑器
const initFuxaEditor = () => {};

// 处理拖拽位置更新事件
const handlePositionUpdate = (event: CustomEvent) => {
  const { componentId, newPosition, isDragging } = event.detail;

  if (
    selectedCanvasComponent.value &&
    selectedCanvasComponent.value.id === componentId
  ) {
    // 直接更新组件的位置数据
    if (selectedCanvasComponent.value.position) {
      selectedCanvasComponent.value.position.x = newPosition.x;
      selectedCanvasComponent.value.position.y = newPosition.y;
    } else {
      selectedCanvasComponent.value.position = {
        x: newPosition.x,
        y: newPosition.y
      };
    }

    // 兼容性支持
    selectedCanvasComponent.value.x = newPosition.x;
    selectedCanvasComponent.value.y = newPosition.y;

    // 更新时间戳以触发响应式更新
    selectedCanvasComponent.value.updated = new Date().toISOString();

    // 强制刷新表单数据以确保UI更新
    nextTick(() => {
      updateFormData();
    });
  }
};

// 处理SVG样式更新事件
const handleSvgStyleUpdate = (event: CustomEvent) => {
  const { componentId, property, value } = event.detail;

  if (
    selectedCanvasComponent.value &&
    selectedCanvasComponent.value.id === componentId
  ) {
    // 更新组件的样式数据
    if (!selectedCanvasComponent.value.style) {
      selectedCanvasComponent.value.style = {};
    }
    selectedCanvasComponent.value.style[property] = value;

    // 立即应用SVG样式
    const element = document.getElementById(componentId);
    if (element && selectedCanvasComponent.value.svgPath) {
      applySvgStyles(element, selectedCanvasComponent.value);
    }

    isSaved.value = false;
  }
};

// 切换网格显示
const toggleGrid = () => {
  showGrid.value = !showGrid.value;
  ElMessage.success(showGrid.value ? "网格已开启" : "网格已关闭");
};

// 切换吸附功能
const toggleSnap = () => {
  enableSnap.value = !enableSnap.value;
  ElMessage.success(enableSnap.value ? "吸附已启用" : "吸附已禁用");
};

// 吸附到网格
const snapToGrid = (value: number) => {
  if (!enableSnap.value) return value;
  return Math.round(value / gridSize.value) * gridSize.value;
};

// 根据ID查找组件
const findComponentById = (componentId: string) => {
  if (!projectData.value?.components) return null;
  return projectData.value.components.find(comp => comp.id === componentId);
};

const parseRuntimeNumber = (value: any): number => {
  if (typeof value === "number") {
    return Number.isFinite(value) ? value : 0;
  }
  if (typeof value === "string") {
    const numeric = parseFloat(value.replace(/[^0-9+\-.,]/g, ""));
    return Number.isNaN(numeric) ? 0 : numeric;
  }
  return 0;
};

const applyRuntimeDataUpdate = (
  componentId: string,
  propertyName: string,
  rawValue: any
) => {
  const component = findComponentById(componentId);
  if (!component) return;

  if (!component.properties) {
    component.properties = {};
  }

  const element = document.getElementById(componentId) as HTMLElement | null;

  if (component.type === "progress-v" && propertyName === "value") {
    const numericValue = parseRuntimeNumber(rawValue);
    component.properties.value = numericValue;

    if (
      selectedCanvasComponent.value?.id === componentId &&
      selectedCanvasComponent.value.properties
    ) {
      selectedCanvasComponent.value.properties.value = numericValue;
    }

    // 🎯 使用 SvgManager 更新进度条
    if (element) {
      const svgContainer = element.querySelector(
        ".svg-container"
      ) as HTMLElement;
      if (svgContainer) {
        const svgOptions: any = {
          animation: component.style?.svgAnimation || "none",
          animationSpeed: component.style?.animationSpeed || "normal",
          animationDuration: component.style?.animationDuration,
          animationIterationCount:
            component.style?.animationIterationCount || "infinite",
          animationStaticValue: numericValue, // 使用运行时数据的值作为目标值
          strokeColor: component.style?.borderColor,
          strokeWidth: component.style?.borderWidth,
          opacity: component.style?.opacity
        };
        svgManager.updateComponentStyle(svgContainer, svgOptions, "progress-v");
        console.log("📊 运行时数据更新进度条值:", numericValue);
      }
    }
    return;
  }

  component.properties[propertyName] = rawValue;

  if (selectedCanvasComponent.value?.id === componentId) {
    if (!selectedCanvasComponent.value.properties) {
      selectedCanvasComponent.value.properties = {};
    }
    selectedCanvasComponent.value.properties[propertyName] = rawValue;
  }
};

const handleRuntimeDataUpdate = (event: Event) => {
  const detail = (
    event as CustomEvent<{
      componentId: string;
      propertyName: string;
      value: any;
    }>
  ).detail;
  if (!detail) return;
  applyRuntimeDataUpdate(detail.componentId, detail.propertyName, detail.value);
};

const handleRuntimeBatchUpdate = (event: Event) => {
  const detail = (
    event as CustomEvent<{
      updates: Array<{ componentId: string; propertyName: string; value: any }>;
    }>
  ).detail;
  if (!detail?.updates || !Array.isArray(detail.updates)) return;
  detail.updates.forEach(update => {
    applyRuntimeDataUpdate(
      update.componentId,
      update.propertyName,
      update.value
    );
  });
};

// 从项目中移除组件
const removeComponentFromProject = (componentId: string) => {
  // 找到要删除的组件并清理定时器
  const component = findComponentById(componentId);
  if (component) {
    clearComponentTimers(component);
  }

  // 从项目数据中移除
  if (projectData.value?.components) {
    const index = projectData.value.components.findIndex(
      comp => comp.id === componentId
    );
    if (index > -1) {
      projectData.value.components.splice(index, 1);
    }
  }

  // 从DOM中移除
  const element = document.getElementById(componentId);
  if (element) {
    element.remove();
  }

  // 清除选择状态
  if (selectedCanvasComponent.value?.id === componentId) {
    selectedCanvasComponent.value = null;
  }
};

// 创建绘图形状
// 创建绘图形状
const createDrawingShape = (element: HTMLElement, component: any) =>
  utils3.createDrawingShape(element, component);

// 切换开关状态
// 设置开关状态（供事件系统调用）
const setSwitchState = (
  component: any,
  element: HTMLElement,
  state: boolean
) => {
  // 🔘 同时设置两个位置以保持兼容性和一致性
  const switchState = state ? "on" : "off";

  // 旧位置（兼容性）
  component.switchState = state;

  // 新位置（主要存储位置）
  if (!component.style) component.style = {};
  component.style.switchState = switchState;

  // 更新视觉样式
  updateSwitchAppearance(component, element);

  // 标记项目未保存
  isSaved.value = false;
};

// 切换开关状态（供事件系统调用）
const toggleSwitchState = (component: any, element: HTMLElement) => {
  // 🔘 从 style.switchState 读取当前状态（优先），如果没有则从旧位置读取
  const currentState =
    component.style?.switchState ||
    (component.switchState ? "on" : "off") ||
    "off";
  const isCurrentlyOn = currentState === "on";

  // 切换到相反状态
  const newState = !isCurrentlyOn;
  const newSwitchState = newState ? "on" : "off";

  // 🔘 同时更新两个位置
  component.switchState = newState;

  if (!component.style) component.style = {};
  component.style.switchState = newSwitchState;

  // 更新视觉样式
  updateSwitchAppearance(component, element);

  // 标记项目未保存
  isSaved.value = false;
};

// 更新开关外观
const updateSwitchAppearance = (component: any, element: HTMLElement) => {
  if (!element) return;

  // 🔘 从 style.switchState 读取开关状态（优先），如果没有则使用 component.switchState
  const switchState =
    component.style?.switchState || component.switchState || "off";
  const isOn = switchState === "on";

  // 🔘 根据动画配置获取过渡时长和缓动函数
  let duration = "0.3s";
  let timingFunction = "cubic-bezier(0.4, 0, 0.2, 1)";

  if (component.style?.svgAnimation === "switchToggle") {
    // 如果启用了开关切换动画，使用配置的时长
    duration = component.style?.animationDuration || "0.3s";
    timingFunction =
      component.style?.animationTimingFunction ||
      "cubic-bezier(0.4, 0, 0.2, 1)";
  }

  // 🔘 不再设置外层容器的背景色，保持透明，让用户通过样式面板自定义
  // 开关的视觉效果完全由SVG内部元素（圆球和轨道）控制

  // 🔘 查找SVG元素并使用 SvgManager 更新开关状态
  const svgElement = element.querySelector("svg");
  if (svgElement) {
    // 🔘 获取用户配置的颜色（如果有）
    const onColor = component.style?.switchOnColor || "#67c23a";
    const offColor = component.style?.switchOffColor || "#909399";

    // 🔘 检查是否有 _switchConfig（表示已初始化开关切换动画）
    const hasSwitchConfig = (svgElement as any)._switchConfig;

    if (hasSwitchConfig && svgManager.updateSwitchState) {
      // 如果已初始化开关动画，使用 SvgManager 的统一方法
      svgManager.updateSwitchState(
        svgElement as SVGSVGElement,
        switchState as "on" | "off",
        onColor,
        offColor
      );
    } else {
      // 直接更新开关状态（不需要先初始化动画）
      const ellipse = svgElement.querySelector("ellipse, circle") as SVGElement;
      if (ellipse) {
        const color = isOn ? onColor : offColor;
        const translateX = isOn ? "28.6%" : "0";

        ellipse.style.transition = `transform ${duration} ${timingFunction}, fill ${duration} ease`;
        ellipse.style.fill = color;
        ellipse.style.transform = `translateX(${translateX})`;
        ellipse.style.transformOrigin = "center";
      }

      const path = svgElement.querySelector("path") as SVGElement;
      if (path) {
        const trackStroke = isOn ? onColor : "#dcdfe6";
        path.style.transition = `stroke ${duration} ease, fill ${duration} ease`;
        path.style.stroke = trackStroke;
      }
    }
  }

  // 🔘 不再处理旧版 img 标签和外层容器样式
  // 所有开关视觉效果都由 SVG 内部元素控制

  // 更新组件属性
  if (!component.properties) {
    component.properties = {};
  }
  component.properties.switchState = isOn;
  component.properties.value = isOn ? 1 : 0; // 数值表示

  // 更新修改时间
  component.updated = new Date().toISOString();
};

// 更新按钮外观
const updateButtonAppearance = (component: any, element: HTMLElement) =>
  utilsButton.updateButtonAppearance(component, element);

// 设置组件交互
const setupComponentInteractions = (element: HTMLElement, component: any) =>
  utils3.setupComponentInteractions(
    element,
    component,
    toggleSwitchState,
    executeComponentEvents,
    selectComponent,
    makeComponentDraggable,
    setupComponentTimers,
    setupValueChangeEvents,
    contextMenuPosition,
    adjustMenuPosition,
    contextMenuVisible,
    isSimulating
  );

// 执行组件事件
const executeComponentEvents = (
  component: any,
  eventType: string,
  event?: Event
) => utils3.executeComponentEvents(component, eventType, event, executeEvent);

// 执行单个事件
const executeEvent = (component: any, eventConfig: any, triggerEvent?: Event) =>
  utils3.executeEvent(
    component,
    eventConfig,
    triggerEvent,
    executeComponentAction
  );

// 执行组件动作
const executeComponentAction = (component: any, action: any) =>
  utils3.executeComponentAction(
    component,
    action,
    executeBackgroundColorChange,
    executeSvgColorChange,
    executeMoveAction,
    executeSetValueAction,
    executeDialogAction,
    executeCommandAction
  );

// 执行背景颜色变化动作
const executeBackgroundColorChange = (
  component: any,
  element: HTMLElement,
  action: any
) => {
  // 同步更新组件数据
  if (!component.style) component.style = {};
  component.style.backgroundType = action.colorType || "solid";
  component.style.backgroundColor = action.backgroundColor;
  component.style.gradientStart = action.gradientStart;
  component.style.gradientEnd = action.gradientEnd;
  component.style.gradientAngle = action.gradientAngle;
  component.style.gradientShape = action.gradientShape;

  // 立即应用样式到DOM元素 - 这会触发完整的样式应用逻辑
  applyStyleToElement(component);

  // 标记项目为未保存状态
  isSaved.value = false;

  // 触发画布重绘
  redrawCanvas();

  // 如果当前组件被选中，同步更新属性面板数据
  if (selectedCanvasComponent.value?.id === component.id) {
    updateFormData();
  }
};

// 执行SVG颜色变化动作
const executeSvgColorChange = (
  component: any,
  element: HTMLElement,
  action: any
) => {
  const svgProperty = action.svgProperty || "fill";
  const svgColor = action.svgColor;
  const opacity = action.opacity || 1;

  // 同步更新组件数据
  if (!component.style) component.style = {};
  component.style[svgProperty] = svgColor;
  component.style[`${svgProperty}Opacity`] = opacity;

  // 使用SVG管理器更新SVG样式 - 这会正确处理SVG元素
  const svgContainer = element.querySelector(".svg-container");
  if (svgContainer) {
    const svgOptions = {
      [svgProperty === "fill" ? "fillColor" : "strokeColor"]: svgColor,
      [`${svgProperty}Opacity`]: opacity,
      // 保留其他现有选项
      animation:
        component.properties?.animation ||
        component.style?.svgAnimation ||
        "none",
      animationSpeed: component.properties?.animationSpeed || "normal"
    };
    // 直接操作SVG元素
    const svgElement = svgContainer.querySelector("svg");
    if (svgElement) {
      const shapes = svgElement.querySelectorAll(
        "path, rect, circle, ellipse, polygon, polyline"
      );
      shapes.forEach(shape => {
        shape.setAttribute(svgProperty, svgColor);
        if (svgProperty === "fill") {
          shape.setAttribute("fill-opacity", opacity.toString());
        } else if (svgProperty === "stroke") {
          shape.setAttribute("stroke-opacity", opacity.toString());
        }
      });
    }
  }

  // 标记项目为未保存状态
  isSaved.value = false;

  // 触发画布重绘
  redrawCanvas();

  // 如果当前组件被选中，同步更新属性面板数据
  if (selectedCanvasComponent.value?.id === component.id) {
    updateFormData();
  }
};

// 执行移动动作
const executeMoveAction = (
  component: any,
  element: HTMLElement,
  action: any
) => {
  const deltaX = action.deltaX || 0;
  const deltaY = action.deltaY || 0;

  const currentLeft =
    parseInt(element.style.left) || component.position?.x || 0;
  const currentTop = parseInt(element.style.top) || component.position?.y || 0;

  const newLeft = currentLeft + deltaX;
  const newTop = currentTop + deltaY;

  // 同步更新组件数据
  if (!component.position) component.position = { x: 0, y: 0 };
  component.position.x = newLeft;
  component.position.y = newTop;

  // 立即应用位置变化
  element.style.left = `${newLeft}px`;
  element.style.top = `${newTop}px`;
  // 标记项目为未保存状态
  isSaved.value = false;

  // 触发画布重绘
  redrawCanvas();

  // 如果当前组件被选中，同步更新属性面板数据
  if (selectedCanvasComponent.value?.id === component.id) {
    updateFormData();
  }
};

// 执行设置数值动作
const executeSetValueAction = (
  component: any,
  element: HTMLElement,
  action: any
) => {
  if (component.type === "text" || component.type === "text-box") {
    // 同步更新组件数据
    component.text = action.value;
    if (component.properties) {
      component.properties.text = action.value;
    }

    // 更新文本内容显示
    const textElement =
      element.querySelector("[contenteditable], input, textarea") || element;
    if (textElement.tagName === "INPUT" || textElement.tagName === "TEXTAREA") {
      (textElement as HTMLInputElement).value = action.value;
    } else {
      textElement.textContent = action.value;
    }
  } else {
    // 其他类型组件的数值更新逻辑
    component.value = action.value;
    if (component.properties) {
      component.properties.value = action.value;
    }
  }

  // 标记项目为未保存状态
  isSaved.value = false;

  // 触发画布重绘
  redrawCanvas();

  // 如果当前组件被选中，同步更新属性面板数据
  if (selectedCanvasComponent.value?.id === component.id) {
    updateFormData();
  }
};

// 执行对话框动作
const executeDialogAction = (action: any) => {
  const message = action.message || "提示信息";
  ElMessage.info(message);
};

// 执行命令动作
const executeCommandAction = (action: any) => {
  const command = action.command || "";

  // 这里可以添加具体的命令执行逻辑
  // 比如发送MQTT消息、调用API等

  ElMessage.success(`命令已执行: ${command}`);
};

// 设置组件定时器
const setupComponentTimers = (component: any, element: HTMLElement) => {
  if (!component.events) return;

  component.events
    .filter(evt => evt.type === "timer" && evt.enabled !== false)
    .forEach(timerEvent => {
      const interval = timerEvent.interval || 1000;
      const timerId = setInterval(() => {
        executeEvent(component, timerEvent);
      }, interval);

      // 存储定时器ID以便后续清理
      if (!component._timers) component._timers = [];
      component._timers.push(timerId);
    });
};

// 设置数值变化事件
const setupValueChangeEvents = (component: any, element: HTMLElement) => {
  if (!component.events) return;

  const valueChangeEvents = component.events.filter(
    evt => evt.type === "valuechange" && evt.enabled !== false
  );

  if (valueChangeEvents.length === 0) return;
  // 这里可以添加数值变化监听逻辑
  // 比如监听数据绑定的变化、MQTT消息等

  // 为每个数值变化事件设置监听器
  valueChangeEvents.forEach(valueEvent => {
    // 可以根据实际需求实现数值变化检测逻辑
    // 例如监听组件的value属性变化
    if (component.dataBinding) {
      // 如果有数据绑定，可以监听数据变化
    }
  });
};

// 清理组件定时器
const clearComponentTimers = (component: any) => {
  if (component._timers && Array.isArray(component._timers)) {
    component._timers.forEach(timerId => {
      clearInterval(timerId);
    });
    component._timers = [];
  }
};

// 重新设置组件事件（用于更新事件配置后）
const refreshComponentEvents = (component: any) => {
  const element = document.getElementById(component.id);
  if (!element) {
    return;
  }

  // 清理现有定时器
  clearComponentTimers(component);

  // 重新设置定时器事件
  if (component.events) {
    setupComponentTimers(component, element);
    setupValueChangeEvents(component, element);
  }
};

// 监听选中组件变化
watch(
  () => selectedCanvasComponent.value,
  () => {
    updateFormData();
  },
  { immediate: true }
);

/**
 * 根据MQTT数据更新组件
 * @param topic MQTT主题
 * @param devices 设备数据数组
 */
const updateComponentsWithMqttData = (topic: string, devices: any[]) => {
  if (!projectData.value?.components) return;

  // 创建设备数据映射: DeviceId -> Device
  const deviceMap = new Map();
  devices.forEach(device => {
    deviceMap.set(device.DeviceId, device);
  });

  // 创建参数值映射: DeviceId_ParamCode -> ParamValue
  const paramValueMap = new Map();
  devices.forEach(device => {
    if (device.DeviceParams && Array.isArray(device.DeviceParams)) {
      device.DeviceParams.forEach(param => {
        const key = `${device.DeviceId}_${param.ParamCode}`;
        paramValueMap.set(key, param.ParamValue);
      });
    }
  });

  console.log('参数值映射:', paramValueMap.size, '个参数');

  // 遍历所有组件,更新绑定了设备参数的组件
  projectData.value.components.forEach(component => {
    // 检查组件是否绑定了设备和参数
    if (component.deviceId && component.paramcode) {
      const key = `${component.deviceId}_${component.paramcode}`;
      const newValue = paramValueMap.get(key);
      
      if (newValue !== undefined) {
        console.log(`更新组件 ${component.id}: ${component.deviceId}/${component.paramcode} = ${newValue}`);
        updateComponentWithValue(component, newValue);
      }
    }

    // 更新图表组件(如果绑定了数据集)
    if (component.chartConfig?.dataSource === 'dataset' || 
        component.chartConfig?.dataSource === 'mqtt') {
      updateChartComponentData(component, devices, deviceMap);
    }
  });
};

/**
 * 更新组件值
 * @param component 组件对象
 * @param value 新值
 */
const updateComponentWithValue = (component: any, value: any) => {
  try {
    // 根据组件类型更新不同的属性
    const updates: any = { lastUpdate: Date.now() };

    // 根据 targetProperty 确定更新哪个属性
    if (component.targetProperty) {
      updates[component.targetProperty] = value;
    } else {
      // 默认更新 value 属性
      updates.value = value;
    }

    // 使用 ComponentManager 更新组件
    componentManager.updateComponent(component.id, updates);

    // 同时更新 projectData 中的组件数据
    Object.assign(component, updates);

    // 触发组件的 valuechange 事件
    const element = document.getElementById(component.id);
    if (element && component.events) {
      const valueChangeEvents = component.events.filter(
        evt => evt.type === 'valuechange' && evt.enabled !== false
      );
      valueChangeEvents.forEach(event => {
        executeEvent(component, event);
      });
    }
  } catch (error) {
    console.error('更新组件失败:', component.id, error);
  }
};

/**
 * 更新图表组件数据
 * @param component 图表组件
 * @param devices 设备数据
 * @param deviceMap 设备映射
 */
const updateChartComponentData = (component: any, devices: any[], deviceMap: Map<string, any>) => {
  if (!component.chartConfig) return;
  
  try {
    // 如果图表绑定了特定设备
    if (component.deviceId) {
      const device = deviceMap.get(component.deviceId);
      if (device && device.DeviceParams) {
        const chartData = device.DeviceParams.map(param => ({
          name: param.ParamName,
          value: parseFloat(param.ParamValue) || 0
        }));
        
        componentManager.updateComponent(component.id, { 
          chartData,
          lastUpdate: Date.now() 
        });
      }
    } else {
      // 图表显示所有设备的某个参数
      const paramCode = component.paramcode || component.chartConfig.paramCode;
      if (paramCode) {
        const chartData = devices.map(device => {
          const param = device.DeviceParams?.find(p => p.ParamCode === paramCode);
          return {
            name: device.DeviceName,
            value: parseFloat(param?.ParamValue || 0)
          };
        });
        
        componentManager.updateComponent(component.id, { 
          chartData,
          lastUpdate: Date.now() 
        });
      }
    }
  } catch (error) {
    console.error('更新图表组件失败:', component.id, error);
  }
};

/**
 * 处理MQTT告警消息
 * @param topic MQTT主题
 * @param alarms 告警数据
 */
const handleMqttAlarms = (topic: string, alarms: any[]) => {
  // 触发告警通知
  alarms.forEach(alarm => {
    ElMessage.warning({
      message: `设备告警: ${alarm.DeviceName || '未知设备'}`,
      duration: 5000
    });
  });
  
  // 可以在这里更新告警组件、记录告警历史等
  console.log('收到告警:', alarms);
};

onMounted(async () => {
  // 组态项目/报表项目共用本编辑器，先按路由 kind 切到对应的一套后端接口
  setProjectKind(projectKind);

  document.addEventListener(
    "fuxa:data:update",
    handleRuntimeDataUpdate as EventListener
  );
  document.addEventListener(
    "fuxa:data:batch-update",
    handleRuntimeBatchUpdate as EventListener
  );

  // 同步画布缩放值到调整手柄管理器
  fuxaResizeHandles.canvasZoom = canvasZoom.value;
  watch(canvasZoom, newZoom => {
    fuxaResizeHandles.canvasZoom = newZoom;
  });

  // 设置调整大小过程中的实时数据同步回调
  fuxaResizeHandles.onResize((component, dimensions) => {
    // 在调整大小过程中实时更新选中组件的表单数据
    if (
      selectedCanvasComponent.value &&
      selectedCanvasComponent.value.id === component.id
    ) {
      // 直接使用传递的尺寸数据，这是最准确的实时数据
      const componentX = dimensions.x;
      const componentY = dimensions.y;
      const componentWidth = dimensions.width;
      const componentHeight = dimensions.height;

      // 更新位置数据
      if (selectedCanvasComponent.value.position) {
        selectedCanvasComponent.value.position.x = componentX;
        selectedCanvasComponent.value.position.y = componentY;
      } else {
        selectedCanvasComponent.value.position = {
          x: componentX,
          y: componentY
        };
      }

      // 更新尺寸数据
      if (selectedCanvasComponent.value.size) {
        selectedCanvasComponent.value.size.width = componentWidth;
        selectedCanvasComponent.value.size.height = componentHeight;
      } else {
        selectedCanvasComponent.value.size = {
          width: componentWidth,
          height: componentHeight
        };
      }

      // 兼容性支持
      selectedCanvasComponent.value.x = componentX;
      selectedCanvasComponent.value.y = componentY;
      selectedCanvasComponent.value.width = componentWidth;
      selectedCanvasComponent.value.height = componentHeight;

      // 更新时间戳以触发响应式更新
      selectedCanvasComponent.value.updated = new Date().toISOString();

      // 实时更新表单数据
      nextTick(() => {
        updateFormData();
      });
    }
  });

  // 设置调整大小完成后的数据同步回调
  fuxaResizeHandles.onResizeEnd((component, dimensions) => {
    // 同步更新项目数据中的组件尺寸和位置
    if (projectData.value?.components) {
      const projectComponent = projectData.value.components.find(
        comp => comp.id === component.id
      );
      if (projectComponent) {
        // 直接使用传递的最终尺寸数据
        const componentX = dimensions.x;
        const componentY = dimensions.y;
        const componentWidth = dimensions.width;
        const componentHeight = dimensions.height;

        if (projectComponent.position) {
          projectComponent.position.x = componentX;
          projectComponent.position.y = componentY;
        } else {
          projectComponent.position = { x: componentX, y: componentY };
        }

        if (projectComponent.size) {
          projectComponent.size.width = componentWidth;
          projectComponent.size.height = componentHeight;
        } else {
          projectComponent.size = {
            width: componentWidth,
            height: componentHeight
          };
        }

        // 兼容性支持
        projectComponent.x = componentX;
        projectComponent.y = componentY;
        projectComponent.width = componentWidth;
        projectComponent.height = componentHeight;

        // ✨ LED 组件特殊处理: 调整大小后需要更新 SVG 尺寸
        if (projectComponent.type === 'led-display') {
          console.log('💡 LED 组件尺寸调整完成，触发 SVG 更新:', {
            componentId: projectComponent.id,
            newSize: { width: componentWidth, height: componentHeight }
          })
          // 调用 LED 组件管理器更新实例
          ledDisplayManager.updateLedDisplayComponent(projectComponent.id, {
            // 传递当前完整配置，触发 updateDisplay
            ...projectComponent.properties
          })
        }

        // 如果调整大小的组件是当前选中的组件，同步更新表单数据
        if (
          selectedCanvasComponent.value &&
          selectedCanvasComponent.value.id === component.id
        ) {
          nextTick(() => {
            updateFormData();
          });
        }
      }
    }
    isSaved.value = false;
  });

  // 监听拖拽位置更新事件
  document.addEventListener("componentPositionUpdated", handlePositionUpdate);

  // 监听SVG样式更新事件
  document.addEventListener("svgStyleUpdate", handleSvgStyleUpdate);

  // 初始化MQTT服务(使用默认配置)
  try {
    await fuxaMqttService.connect({
      host: 'ws://localhost:9001/mqtt',
      clientId: `scada_editor_${Date.now()}`
    });
    console.log("MQTT服务已初始化");

    // 设置MQTT数据更新回调
    fuxaMqttService.onDataUpdate((topic, devices) => {
      console.log(`MQTT数据更新 [${topic}]:`, devices.length, '个设备');
      // 查找绑定了该数据集的组件并更新
      updateComponentsWithMqttData(topic, devices);
    });

    fuxaMqttService.onAlarmUpdate((topic, alarms) => {
      console.log(`MQTT告警更新 [${topic}]:`, alarms.length, '条告警');
      // 处理告警消息
      handleMqttAlarms(topic, alarms);
    });
  } catch (error) {
    ElMessage.warning("MQTT服务连接失败，将以离线模式运行");
  }

  // 初始化组件管理器
  try {
    componentManager.initialize(editorContainer.value!);

    // 设置组件管理器事件回调
    componentManager.onComponentCreated = component => {
      // 注意：不要重复调用addComponentToCanvas，因为组件已经被管理器创建了
      // 只需要添加到项目数据中
      if (projectData.value) {
        if (!projectData.value.components) {
          projectData.value.components = [];
        }
        projectData.value.components.push(component);
      }
      isSaved.value = false;
    };

    componentManager.onComponentUpdated = (componentId, newProperties) => {
      const component = findComponentById(componentId);
      if (component) {
        Object.assign(component.properties, newProperties);
        isSaved.value = false;
      }
    };

    componentManager.onComponentDeleted = componentId => {
      removeComponentFromProject(componentId);
      isSaved.value = false;
    };

    ElMessage.success("组件管理器初始化成功");
  } catch (error) {
    ElMessage.warning("组件管理器初始化失败，部分功能可能不可用");
  }

  // 初始化PathTool
  try {
    pathTool.initialize(editorContainer.value!);

    // 设置路径完成回调
    pathTool.onPathComplete = pathComponent => {
      // 创建路径组件并添加到画布
      const canvasContent =
        editorContainer.value?.querySelector(".canvas-content");
      if (canvasContent) {
        const pathElement = pathTool.createPathComponent(
          pathComponent,
          canvasContent
        );
      }

      // 添加到项目数据
      if (projectData.value) {
        if (!projectData.value.components) {
          projectData.value.components = [];
        }

        // 转换为项目数据格式
        const projectComponent = {
          id: pathComponent.id,
          type: pathComponent.type,
          position: { x: pathComponent.x, y: pathComponent.y },
          size: { width: pathComponent.width, height: pathComponent.height },
          properties: {
            ...pathComponent.properties,
            points: pathComponent.points
          },
          // 兼容性属性
          x: pathComponent.x,
          y: pathComponent.y,
          width: pathComponent.width,
          height: pathComponent.height
        };

        projectData.value.components.push(projectComponent);
      }

      isSaved.value = false;
      ElMessage.success(`路径绘制完成，共${pathComponent.points.length}个节点`);

      // 切换到选择模式并选中新创建的路径
      currentEditorMode.value = "select";
      activeComponent.value = null;
      setCanvasMode("select");

      // 延迟选中路径组件，确保DOM已更新
      nextTick(() => {
        const createdPathElement = document.getElementById(pathComponent.id);
        if (createdPathElement) {
          selectComponent(pathComponent);
        } else {
        }
      });
    };
  } catch (error) {
    ElMessage.warning("路径工具初始化失败，路径绘制功能可能不可用");
  }

  // 添加键盘事件监听
  document.addEventListener("keydown", handleKeydown);
  // 添加全局点击事件监听（隐藏右键菜单）
  document.addEventListener("click", hideContextMenu);

  initFuxaEditor();

  // 完全禁用SVG清理功能，防止影响Vue模板渲染
  // 注意：SVG清理功能可能会意外清除Vue模板元素，暂时禁用
  // setTimeout(() => {
  //  try {
  //   svgManager.cleanupHiddenSvgElements();
  //  } catch (error) {
  //   console.warn('SVG清理过程中出现警告:', error);
  //  }
  // }, 3000);

  
// 从数据集加载设备数据
const loadDeviceDataFromDataset = () => {
  const datasets = datasetConfigRef.value?.getDatasets() || [];

  // 1. 查找并加载 API 数据集
  const apiDataset = datasets.find(ds => ds.type === 'api' && ds.config?.deviceUrl);
  if (apiDataset) {
    loadApiDeviceData(apiDataset);
  }

  // 2. 查找并加载静态数据集
  const staticDataset = datasets.find(ds => ds.type === 'static');
  if (staticDataset) {
    loadStaticDeviceData(staticDataset);
  }
};


// 监听数据集对话框关闭,重新加载设备数据
watch(datasetDialogVisible, (newVal, oldVal) => {
  if (oldVal === true && newVal === false) {
    // 对话框关闭时重新加载设备数据
    loadDeviceDataFromDataset();
  }
});

// 页面加载时初始化设备数据
onMounted(() => {
  loadDeviceDataFromDataset();
});
// 加载 API 设备数据
const loadApiDeviceData = async (dataset: any) => {
  try {
    const response = await fetch(dataset.config.deviceUrl, {
      method: 'GET',
      headers: dataset.config.headers || {}
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const data = await response.json();

    if (data.Result && typeof data.Result === 'string') {
      apiDeviceData.value = JSON.parse(data.Result);
      console.log('✅ API 设备数据加载成功:', apiDeviceData.value.length, '个设备');
    }
  } catch (error) {
    console.error('❌ API 设备数据加载失败:', error);
    apiDeviceData.value = [];
  }
};

// 加载静态设备数据
const loadStaticDeviceData = (dataset: any) => {
  try {
    if (dataset.data && typeof dataset.data === 'string') {
      const data = JSON.parse(dataset.data);
      if (data.Result && typeof data.Result === 'string') {
        staticDeviceData.value = JSON.parse(data.Result);
        console.log('✅ 静态设备数据加载成功:', staticDeviceData.value.length, '个设备');
      }
    }
  } catch (error) {
    console.error('❌ 静态设备数据加载失败:', error);
    staticDeviceData.value = [];
  }
};

// 加载项目数据
  if (projectId.value && projectId.value !== "new") {
    await loadProject(projectId.value);
  } else {
    // 新建项目，初始化空数据
    initializeNewProject();
  }

  // 启动自动保存
  startAutoSave();

  // 添加页面离开前提示
  window.addEventListener("beforeunload", handleBeforeUnload);
});

onUnmounted(() => {
  document.removeEventListener(
    "fuxa:data:update",
    handleRuntimeDataUpdate as EventListener
  );
  document.removeEventListener(
    "fuxa:data:batch-update",
    handleRuntimeBatchUpdate as EventListener
  );
  // 清理MQTT服务连接
  fuxaMqttService.disconnect();

  // 清理组件管理器
  componentManager.destroy();

  // 清理PathTool
  pathTool.destroy();

  // 清理自定义事件监听器
  document.removeEventListener(
    "componentPositionUpdated",
    handlePositionUpdate
  );

  // 暂时禁用SVG清理功能
  // svgManager.cleanupHiddenSvgElements();

  // 移除键盘事件监听
  document.removeEventListener("keydown", handleKeydown);

  // 移除全局点击事件监听
  document.removeEventListener("click", hideContextMenu);

  // 停止自动保存
  stopAutoSave();

  // 移除页面离开前提示
  window.removeEventListener("beforeunload", handleBeforeUnload);
});
</script>

<template>
  <div v-loading="loading" class="scada-editor">
    <!-- 编辑器顶部工具栏 -->
    <div class="editor-toolbar">
      <div class="toolbar-left">
        <el-button @click="goBack">
          <el-icon><ArrowLeft /></el-icon>
          返回
        </el-button>
        <el-divider direction="vertical" />
        <span class="project-name">{{ projectInfo.ProjectName }}</span>
      </div>

      <div class="toolbar-center">
        <el-button-group>
          <el-button
            :type="isSimulating ? 'danger' : 'success'"
            @click="isSimulating ? stopSimulation() : startSimulation()"
          >
            {{ isSimulating ? "停止仿真" : "开始仿真" }}
          </el-button>
          <el-button type="primary" @click="saveProject"> 保存项目 </el-button>
          <el-button
            :type="projectInfo.ProjectStatus === 1 ? 'warning' : 'success'"
            @click="handlePublishProject"
          >
            {{ projectInfo.ProjectStatus === 1 ? "取消发布" : "发布项目" }}
          </el-button>
        </el-button-group>

        <el-divider direction="vertical" />

        <el-button-group>
          <el-button
            :type="showComponentPanel ? 'primary' : 'default'"
            @click="togglePanel('component')"
          >
            <el-icon><Grid /></el-icon>
            组件库
          </el-button>
          <el-button
            :type="showPropertyPanel ? 'primary' : 'default'"
            @click="togglePanel('property')"
          >
            <el-icon><Setting /></el-icon>
            属性
          </el-button>
          <el-button type="info" @click="datasetDialogVisible = true">
            <el-icon><Connection /></el-icon>
            数据集
          </el-button>
        </el-button-group>
      </div>

      <div class="toolbar-right">
        <el-tag v-if="!isSaved" type="warning">未保存</el-tag>
        <el-tag v-if="isSimulating" type="success">仿真中</el-tag>
        <span class="version">v1.0.0</span>
      </div>
    </div>

    <!-- FUXA编辑器主体 -->
    <div class="editor-main">
      <!-- 左侧组件库面板 -->
      <div v-show="showComponentPanel" class="left-panel">
        <FuxaComponentPanel
          :project-kind="projectKind"
          @add-component="handleAddComponent"
          @activate-component="handleActivateComponent"
        />
      </div>

      <!-- 中间编辑区域 -->
      <div class="editor-center">
        <!-- 画布工具栏 -->
        <div class="canvas-toolbar">
          <div class="canvas-tools">
            <el-button-group>
              <el-button
                :icon="useRenderIcon('ep:zoom-out')"
                @click="canvasZoom = Math.max(25, canvasZoom - 25)"
                >缩小</el-button
              >
              <el-button>{{ canvasZoom }}%</el-button>
              <el-button
                :icon="useRenderIcon('ep:zoom-in')"
                @click="canvasZoom = Math.min(200, canvasZoom + 25)"
                >放大</el-button
              >
            </el-button-group>

            <el-divider direction="vertical" />

            <el-button-group>
              <el-button
                :icon="useRenderIcon('ep:grid')"
                :type="showGrid ? 'primary' : ''"
                @click="toggleGrid"
              >
                网格
              </el-button>
              <el-button
                :icon="useRenderIcon('ep:magnet')"
                :type="enableSnap ? 'primary' : ''"
                @click="toggleSnap"
              >
                吸附
              </el-button>
            </el-button-group>
          </div>

          <div class="canvas-info">
            <span>画布: 1200 × 800</span>
            <span v-if="currentEditorMode !== 'select'" class="mode-indicator">
              模式: {{ getModeDisplayName(currentEditorMode) }}
            </span>
            <span>组件: {{ getCurrentViewComponentCount() }}</span>
          </div>
        </div>

        <!-- 画布容器 -->
        <div class="canvas-container">
          <div
            ref="editorContainer"
            class="fuxa-canvas"
            :style="{ transform: `scale(${canvasZoom / 100})` }"
            @drop="handleCanvasDrop"
            @dragover="handleCanvasDragOver"
            @dragleave="handleCanvasDragLeave"
            @dragend="handleCanvasDragEnd"
            @click="handleCanvasClick"
            @contextmenu="handleContextMenu"
          >
            <!-- FUXA画布内容 -->
            <div class="canvas-background">
              <div v-show="showGrid" class="canvas-grid" />
              <div class="canvas-content" />
            </div>
          </div>
        </div>
      </div>

      <!-- 右侧属性面板 -->
      <div v-show="showPropertyPanel" class="right-panel">
        <PropertyPanel
          :selected-component="selectedCanvasComponent"
          :device-list="deviceList"
          @update-property="handleUpdateProperty"
          @update-position="handleUpdatePosition"
          @update-size="handleUpdateSize"
          @update-style="handleUpdateStyle"
          @update-text="handleUpdateText"
          @update-text-card-config="handleUpdateTextCardConfig"
          @update-text-card-shadow="handleUpdateTextCardShadow"
          @update-component-property="handleUpdateComponentProperty"
          @update-component-transform="handleUpdateComponentTransform"
          @align-component="handleAlignComponent"
          @bring-to-front="handleBringToFront"
          @send-to-back="handleSendToBack"
          @clear-selected-component="clearSelectedComponent"
          @update-chart-config="handleUpdateChartConfig"
          @open-dataset-dialog="datasetDialogVisible = true"
        />
      </div>
    </div>

    <!-- 底部状态栏 -->
    <div class="editor-statusbar">
      <div class="status-left">
        <span>项目ID: {{ projectId }}</span>
        <el-divider direction="vertical" />
        <span
          >状态: {{ projectInfo.ProjectStatus === 0 ? "草稿" : "发布" }}</span
        >
      </div>
      <div class="status-right">
        <span :class="`mqtt-status mqtt-${mqttStatus}`">
          MQTT:
          {{
            mqttStatus === "connected"
              ? "已连接"
              : mqttStatus === "connecting"
                ? "连接中"
                : mqttStatus === "error"
                  ? "错误"
                  : "已断开"
          }}
        </span>
        <el-divider direction="vertical" />
        <span>设备: {{ mqttDeviceCount }}个</span>
        <el-divider direction="vertical" />
        <span>消息: {{ mqttMessageCount }}条</span>
      </div>
    </div>

    <!-- 右键菜单 -->
    <ul
      v-if="contextMenuVisible"
      :style="contextMenuStyle"
      class="context-menu"
      @click.stop
    >
      <li
        v-for="(item, index) in contextMenuItems"
        :key="item.id || `separator-${index}`"
      >
        <a
          v-if="!item.separator"
          :class="{ disabled: item.disabled }"
          @click="handleMenuClick(item)"
        >
          <el-icon v-if="item.icon" class="menu-icon">
            <component :is="useRenderIcon(item.icon)" />
          </el-icon>
          <span class="menu-label">{{ item.label }}</span>
          <span v-if="item.shortcut" class="shortcut">{{ item.shortcut }}</span>
        </a>
        <hr v-else class="separator" />
      </li>
    </ul>

    <!-- 属性编辑弹框 -->
    <PropertyEditDialog
      :visible="propertyDialogVisible"
      :selected-component="selectedCanvasComponent"
      :device-list="deviceList"
      :form-data="componentFormData"
      @update:visible="propertyDialogVisible = $event"
      @update:form-data="handlePropertyFormDataUpdate"
      @update-component-property="handleUpdateComponentProperty"
      @open-dataset-dialog="datasetDialogVisible = true"
    />

    <!-- 数据集配置组件 -->
    <DatasetPanel
      v-model:visible="datasetDialogVisible"
      :dataset="currentDataset"
      @save-config="handleSaveDatasetConfig"
      @test-dataset="handleTestDataset"
    />

    <!-- 图表属性配置对话框 -->
    <ChartPropertyDialog
      :visible="chartPropertyDialogVisible"
      :chart-component="currentChartComponent"
      :dataset-list="datasetList"
      @update:visible="chartPropertyDialogVisible = $event"
      @save-config="handleSaveChartProperty"
      @open-dataset-panel="datasetDialogVisible = true"
    />

    <!-- iframe属性配置对话框 -->
    <IframePropertyDialog
      :visible="iframeConfigDialogVisible"
      :iframe-component="currentIframeComponent"
      @update:visible="iframeConfigDialogVisible = $event"
      @save-config="handleSaveIframeConfig"
    />

    <!-- 视频属性配置对话框 -->
    <VideoPropertyDialog
      :visible="videoConfigDialogVisible"
      :video-component="currentVideoComponent"
      @update:visible="videoConfigDialogVisible = $event"
      @save-config="handleSaveVideoConfig"
    />

    <!-- 摄像头属性配置对话框 -->
    <WebcamPropertyDialog
      :visible="webcamConfigDialogVisible"
      :webcam-component="currentWebcamComponent"
      @update:visible="webcamConfigDialogVisible = $event"
      @save-config="handleSaveWebcamConfig"
    />

    <!-- 表格属性配置对话框 -->
    <TablePropertyDialog
      :visible="tableConfigDialogVisible"
      :table-component="currentTableComponent"
      :dataset-list="datasetList"
      @update:visible="tableConfigDialogVisible = $event"
      @save-config="handleSaveTableConfig"
      @open-dataset-panel="datasetDialogVisible = true"
    />

    <!-- 温度计配置对话框 -->
    <ThermometerPropertyDialog
      :visible="thermometerConfigVisible"
      :thermometer-component="selectedCanvasComponent"
      @update:visible="thermometerConfigVisible = $event"
      @save-config="handleSaveThermometerConfig"
    />

    <!-- 文本卡片配置对话框 -->
    <TextCardPropertyDialog
      :visible="textCardConfigDialogVisible"
      :text-card-component="currentTextCardComponent"
      @update:visible="textCardConfigDialogVisible = $event"
      @save-config="handleSaveTextCardConfig"
    />
  </div>
</template>

<style scoped lang="scss">
@import "./main/index.scss";
</style>
