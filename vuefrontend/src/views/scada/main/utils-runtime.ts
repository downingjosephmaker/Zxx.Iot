/**
 * 运行态渲染器
 *
 * 背景：运行态原本自带一套 v-html 字符串渲染(只认 text/image/video)，与编辑器的命令式 DOM
 * 渲染是两套实现——结果是发布后 SVG 图元/图表/表格全部渲染成空白方块。此处不再另写渲染器，
 * 而是直接复用编辑器的组件创建链路 `utils1.createComponentElement`，只把编辑交互
 * (选中/拖拽/缩放手柄/配置弹窗)替换为空实现。组态运行态与报表运行态共用本模块。
 *
 * 约束：容器 DOM 内必须存在 `.canvas-content` 子元素——createComponentElement 以此为挂载点。
 */
import { nextTick } from "vue";
import * as echarts from "echarts";
import { componentManager } from "../core/ComponentManager";
import { svgManager, createSvgComponent } from "../core/SvgManager";
import { ledDisplayManager } from "../core/LedDisplayComponent";
import { textCardManager } from "../core/TextCardComponent";
import * as utils1 from "./utils1";
import * as utils2 from "./utils2";
import * as utils3 from "./utils3";
import * as utils4 from "./utils4";
import * as utilsButton from "./utils-button";

/** 运行态无编辑交互：选中、拖拽、缩放手柄、属性弹窗一律不绑 */
const noop = () => {
  /* 运行态不绑编辑交互 */
};

/**
 * 运行态开关外观：只反映状态，不写编辑态元数据(updated 时间戳等)。
 * 直接走 SvgManager 的统一方法，与编辑器共用同一份底层实现。
 */
export const updateSwitchAppearance = (component: any, element: HTMLElement) => {
  if (!element) return;
  const state =
    component.style?.switchState || component.switchState || "off";
  const svg = element.querySelector("svg");
  if (svg && svgManager.updateSwitchState) {
    svgManager.updateSwitchState(
      svg as SVGSVGElement,
      state as "on" | "off",
      component.style?.switchOnColor || "#67c23a",
      component.style?.switchOffColor || "#909399"
    );
  }
};

export interface RuntimeRenderer {
  /** 渲染单个组件到画布，返回其 DOM 元素 */
  render: (component: any) => HTMLElement | undefined;
  /** 图表实例初始化(数据刷新时复用) */
  initEChart: (container: HTMLElement, component: any) => void;
}

/**
 * 建运行态渲染器。
 * @param container 画布容器 ref（其内部需含 `.canvas-content`）
 */
export function createRuntimeRenderer(container: {
  value: HTMLElement | null;
}): RuntimeRenderer {
  const initEChart = (el: HTMLElement, component: any) =>
    utils3.initEChart(el, component, echarts);

  const createPathElement = (component: any, canvasContent: Element) =>
    utils3.createPathElement(component, canvasContent, noop);

  const createLineElement = (component: any, canvasContent: Element) =>
    utils3.createLineElement(component, canvasContent, noop);

  const createImageElement = (component: any, canvasContent: Element) =>
    utils3.createImageElement(component, canvasContent, noop);

  const createIframeElement = (component: any, canvasContent: Element) =>
    utils3.createIframeElement(component, canvasContent, noop);

  const createVideoElement = (component: any, canvasContent: Element) =>
    utils3.createVideoElement(component, canvasContent, noop);

  const createWebcamElement = (component: any, canvasContent: Element) =>
    utils3.createWebcamElement(component, canvasContent, noop);

  const createButtonElement = (component: any, canvasContent: Element) =>
    utilsButton.createButtonElement(component, canvasContent, noop);

  const createTableElement = (component: any, canvasContent: Element) =>
    utils3.createTableElement(component, canvasContent, noop);

  // 运行态画板只读：不给工具栏
  const createPaintBoardElement = (component: any, canvasContent: Element) =>
    utils3.createPaintBoardElement(component, canvasContent, noop, noop, false);

  // 运行态点图表不弹配置框
  const createChartElement = (component: any, canvasContent: Element) =>
    utils3.createChartElement(component, canvasContent, noop, noop, initEChart);

  const render = (component: any) =>
    utils1.createComponentElement(
      component,
      container,
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
      noop, // setupComponentInteractions
      utils1.extractComponentNameFromPath,
      utils1.applySvgStyles,
      utils1.applyStyleToElement,
      utils2.applyTransformToElement,
      updateSwitchAppearance,
      utilsButton.updateButtonAppearance,
      componentManager,
      createSvgComponent,
      nextTick
    );

  return { render, initEChart };
}

/** 判断组件是否配了液体动画(储罐/温度计等按点位值涨落) */
const isLiquidComponent = (component: any) =>
  component.style?.svgAnimation === "liquidFill" ||
  component.style?.svgAnimation === "liquidDrain";

/** 判断组件是否为开关形态 */
const isSwitchComponent = (component: any) =>
  component.type === "switch" ||
  component.style?.switchState !== undefined ||
  component.switchState !== undefined;

/**
 * 把一个点位值应用到组件上。
 *
 * 按组件形态分派：液位/开关/LED/文本卡走各自的**增量更新**（重建 DOM 会重置动画与图表实例）；
 * 其余组件回落为"改属性后重建元素"——这也是编辑器里表格刷新采用的做法。
 *
 * @returns 是否已处理（false 表示调用方需自行兜底）
 */
export function applyPointValue(
  component: any,
  value: number | string,
  unit: string,
  renderer: RuntimeRenderer
): boolean {
  const element = document.getElementById(component.id);
  if (!element) return false;

  const num = typeof value === "number" ? value : Number(value);
  const hasNum = !Number.isNaN(num);
  const text = `${value !== "" && value !== null ? value : "-"}${unit || ""}`;

  // 液位：按数值涨落，走 SvgManager 的液体动画
  if (isLiquidComponent(component) && hasNum) {
    const svg = element.querySelector("svg");
    if (svg) {
      svgManager.updateLiquidLevel(svg as SVGSVGElement, num, 800);
      return true;
    }
  }

  // 开关：非零即开
  if (isSwitchComponent(component) && hasNum) {
    component.style = component.style || {};
    component.style.switchState = num ? "on" : "off";
    updateSwitchAppearance(component, element);
    return true;
  }

  // LED 显示屏
  if (component.type === "led-display") {
    component.properties = { ...component.properties, text, value: num };
    ledDisplayManager.updateLedDisplayComponent(component.id, component.properties);
    return true;
  }

  // 文本卡片
  if (component.type === "text-card") {
    component.properties = { ...component.properties, content: text };
    textCardManager.updateTextCardComponent(component.id, component.properties);
    return true;
  }

  // 其余（文本/数值/按钮标签等）：改属性后重建元素
  component.properties = { ...component.properties, text, value: num };
  element.remove();
  renderer.render(component);
  return true;
}

/**
 * 运行态图表接线：复用编辑器的取数与刷新逻辑(fetchChartData/updateChartData/setupChartDataRefresh)。
 * 实时模式由数据集定时刷新驱动；历史模式一次性取曲线。
 * @param datasetList 项目 JSON 里的 datasets（需为 { value: [...] } 形状，fetchChartData 按 ref 取用）
 */
export async function startChartComponents(
  components: any[],
  datasetList: { value: any[] },
  renderer: RuntimeRenderer
) {
  const charts = components.filter(c => c.chartConfig?.datasetId);
  for (const component of charts) {
    try {
      const data = await utils4.fetchChartData(component, datasetList);
      if (data) utils4.updateChartData(component, data, renderer.initEChart);
      if (component.chartConfig?.refreshInterval > 0) {
        utils4.setupChartDataRefresh(component, datasetList, renderer.initEChart);
      }
    } catch {
      /* 单个图表取数失败不阻断整页渲染 */
    }
  }
}

/** 停止所有图表的定时刷新(运行态卸载时调用,防定时器泄漏) */
export function stopChartComponents(components: any[]) {
  components.forEach(c => {
    if (c._chartRefreshTimer) {
      clearInterval(c._chartRefreshTimer);
      c._chartRefreshTimer = null;
    }
  });
}
