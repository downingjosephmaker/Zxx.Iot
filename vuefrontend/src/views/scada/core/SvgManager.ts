/**
 * SVG管理器 - 统一管理所有组件的内联SVG渲染
 * 支持动态样式、动画效果、状态变化
 */

import {
  type ComponentIconMapping,
  findComponentByName
} from "./fuxa-icon-mapping";
import { drawingTools, type DrawingTool } from "./DrawingTools";
import { SvgTypeDetector, type SvgTypeInfo } from "./SvgTypeDetector";

export interface SvgRenderOptions {
  width?: number;
  height?: number;
  fillColor?: string;
  strokeColor?: string;
  strokeWidth?: number;
  className?: string;
  animation?: "rotate" | "pulse" | "blink" | "bounce" | "shake" | "scale" | "moveX" | "moveY" | "fade" | "pipeFlow" | "none";
  animationSpeed?: "slow" | "normal" | "fast";
  animationDuration?: string;
  animationIterationCount?: string;
  animationTimingFunction?: string;
  animationDelay?: string;
  animationPlayStateOnHover?: boolean;
  // 管道流动方向
  pipeFlowDirection?: "forward" | "backward" | "bidirectional";
  // 动画静态值参数A - 用于液体动画和进度条动画的目标值 (0-100)
  animationStaticValue?: number;
  opacity?: number;
  hoverEffect?: boolean;
  // SVG特有属性
  strokeDasharray?: string;
  strokeLinecap?: string;
  strokeLinejoin?: string;
  fillOpacity?: number;
  strokeOpacity?: number;
  // SVG滤镜
  enableDropShadow?: boolean;
  dropShadowColor?: string;
  dropShadowOffsetX?: number;
  dropShadowOffsetY?: number;
  dropShadowBlur?: number;
  svgBlur?: number;
  // 渐变
  gradientType?: string;
  gradientStart?: string;
  gradientEnd?: string;
  gradientDirection?: number;
  // SVG初始化回调 - 仅在animationStaticValue为undefined时调用，用于设置默认值
  onSvgInitialized?: (defaultValue: number) => void;
}

export interface ComponentSvgData {
  name: string;
  title: string;
  svgContent: string;
  category: string;
  defaultOptions?: SvgRenderOptions;
}

/**
 * SVG内容缓存 - 避免重复读取文件
 */
const svgCache = new Map<string, string>();

/**
 * SVG管理器类
 */
export class SvgManager {
  private static instance: SvgManager;

  /**
   * 获取单例实例
   */
  static getInstance(): SvgManager {
    if (!SvgManager.instance) {
      SvgManager.instance = new SvgManager();
    }
    return SvgManager.instance;
  }

  /**
   * 异步加载SVG文件内容
   */
  async loadSvgContent(iconPath: string): Promise<string> {
    if (svgCache.has(iconPath)) {
      return svgCache.get(iconPath)!;
    }

    try {
      // 处理路径格式
      let fullPath = iconPath;
      if (iconPath.startsWith("@/assets/svg/")) {
        fullPath = iconPath.replace("@/assets/svg/", "/src/assets/svg/");
      }

      const response = await fetch(fullPath);
      if (!response.ok) {
        throw new Error(`Failed to load SVG: ${response.status}`);
      }

      const svgContent = await response.text();
      svgCache.set(iconPath, svgContent);
      return svgContent;
    } catch (error) {
      console.warn(`无法加载SVG文件: ${iconPath}`, error);
      return this.getDefaultSvg();
    }
  }

  /**
   * 同步创建内联SVG元素
   */
  createInlineSvg(
    componentName: string,
    options: SvgRenderOptions = {}
  ): HTMLElement {
    // 特殊处理text组件 - 优先处理，不需要SVG文件
    if (componentName === "text" || componentName === "text-box") {
      const textContainer = document.createElement("div");
      textContainer.className = `svg-container text-preview ${options.className || ""}`;
      textContainer.style.cssText = `
        width: 100%;
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        position: relative;
        border: 1px dashed #409eff;
        background: #f8f9fa;
        color: #409eff;
        font-size: 12px;
        font-weight: 500;
      `;
      // textContainer.textContent = 'Aa'
      return textContainer;
    }

    // 首先在常规组件中查找
    let component = findComponentByName(componentName);
    let isDrawingTool = false;

    // 如果常规组件中没找到，在绘图工具中查找
    if (!component) {
      const drawingTool = drawingTools.find(
        tool => tool.name === componentName
      );
      if (drawingTool) {
        // 转换绘图工具为组件格式
        component = {
          name: drawingTool.name,
          title: drawingTool.title,
          iconPath: drawingTool.icon,
          category: drawingTool.category
        };
        isDrawingTool = true;
      }
    }

    if (!component) {
      console.warn("SvgManager 组件未找到:", componentName);
      return this.createErrorSvg(componentName, options);
    }

    // 创建容器
    const container = document.createElement("div");
    container.className = `svg-container ${options.className || ""}`;
    container.style.cssText = `
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      position: relative;
    `;

    // 异步加载并渲染SVG
    this.loadSvgContent(component.iconPath)
      .then(svgContent => {
        this.renderSvgContent(container, svgContent, options, componentName);
      })
      .catch(() => {
        this.renderSvgContent(container, this.getDefaultSvg(), options, componentName);
      });

    return container;
  }

  /**
   * 渲染SVG内容到容器
   */
  private renderSvgContent(
    container: HTMLElement,
    svgContent: string,
    options: SvgRenderOptions,
    componentName?: string
  ): void {
    // 清空容器
    container.innerHTML = "";

    // 使用更安全的SVG解析方法，避免DOMParser创建临时DOM元素
    let svgElement: SVGSVGElement;

    try {
      // 创建临时容器在内存中解析SVG，避免影响主文档
      const tempDiv = document.createElement('div');
      tempDiv.innerHTML = svgContent;
      const tempSvg = tempDiv.querySelector('svg');

      if (!tempSvg) {
        container.innerHTML = this.getDefaultSvg();
        return;
      }

      // 克隆SVG元素并立即清理临时容器
      svgElement = tempSvg.cloneNode(true) as SVGSVGElement;
      tempDiv.innerHTML = ''; // 立即清理临时容器

    } catch (error) {
      console.warn('SVG解析失败，使用默认图标:', error);
      container.innerHTML = this.getDefaultSvg();
      return;
    }

    // 检测SVG类型并决定是否清理尺寸属性
    const svgTypeInfo = SvgTypeDetector.detectSvgType(
      svgContent,
      componentName
    );
    
    // 根据类型决定是否移除固定尺寸
    if (svgTypeInfo.recommendations.shouldCleanAttributes) {
      svgElement.removeAttribute('width');
      svgElement.removeAttribute('height');
    }


    // 应用样式选项
    this.applySvgOptions(svgElement, options, componentName);

    // 添加到容器
    container.appendChild(svgElement);

    // 添加动画效果（包括"none"的情况，因为"none"时需要设置静态值）
    if (options.animation) {
      this.addAnimation(svgElement, options.animation, options.animationSpeed, options);
    }

    // 添加悬停效果
    if (options.hoverEffect) {
      this.addHoverEffect(container);
    }
  }

  /**
   * 应用SVG选项
   */
  private applySvgOptions(
    svgElement: SVGSVGElement,
    options: SvgRenderOptions,
    componentName?: string
  ): void {
    // 设置基础尺寸样式
    svgElement.style.width = "100%";
    svgElement.style.height = "100%";
    svgElement.style.display = "block";

    // 设置透明度
    if (options.opacity !== undefined) {
      svgElement.style.opacity = options.opacity.toString();
    }

    // 使用新的类型检测器分析SVG
    const svgTypeInfo = SvgTypeDetector.detectSvgType(
      svgElement.outerHTML,
      componentName
    );

    console.log(`SVG类型检测 [${componentName}]:`, svgTypeInfo);

    // 根据检测结果处理SVG
    this.processSvgByType(svgElement, svgTypeInfo, options, componentName);

    // 应用SVG滤镜效果（传递componentName以便特殊处理）
    this.applySvgFilters(svgElement, options, componentName);
  }

  /**
   * 根据SVG类型进行不同的处理
   */
  private processSvgByType(
    svgElement: SVGSVGElement,
    typeInfo: SvgTypeInfo,
    options: SvgRenderOptions,
    componentName?: string
  ): void {
    // 根据类型选择处理策略
    switch (typeInfo.type) {
      case 'interactive':
        this.processInteractiveSvg(svgElement, options);
        break;
        
      case 'complex':
        this.processComplexSvg(svgElement, typeInfo, options);
        break;
        
      case 'transformed':
        this.processTransformedSvg(svgElement, typeInfo, options);
        break;
        
      case 'inkscape':
        this.processInkscapeSvg(svgElement, typeInfo, options);
        break;
        
      case 'simple':
      default:
        this.processSimpleSvg(svgElement, typeInfo, options, componentName);
        break;
    }

    // 确保所有SVG都有正确的缩放支持
    this.ensureScalingSupport(svgElement, componentName);
  }

  /**
   * 处理交互式SVG（包含JavaScript）
   */
  private processInteractiveSvg(
    svgElement: SVGSVGElement,
    options: SvgRenderOptions
  ): void {
    console.log('处理交互式SVG - 保持原有样式和脚本');

    // 交互式SVG完全保持原样，不做任何样式修改
    // 只确保基本的显示属性
    svgElement.style.opacity = options.opacity?.toString() || '1';

    // 移除可能干扰的强制样式
    const existingStyles = svgElement.querySelectorAll('style');
    existingStyles.forEach(style => {
      if (style.textContent?.includes('!important') &&
          style.textContent?.includes('fill:')) {
        // 只移除强制颜色样式，保留其他样式
        const content = style.textContent.replace(/fill:\s*[^;]*\s*!important[^;]*/g, '');
        if (content.trim()) {
          style.textContent = content;
        } else {
          style.remove();
        }
      }
    });

    // 执行SVG内嵌的JavaScript脚本
    // 注意：通过innerHTML插入的<script>标签不会自动执行，需要手动执行
    this.executeInlineSvgScripts(svgElement);
  }

  /**
   * 执行SVG内嵌的JavaScript脚本
   * 注意：避免重复执行导致变量重复声明错误
   */
  private executeInlineSvgScripts(svgElement: SVGSVGElement): void {
    const scripts = svgElement.querySelectorAll('script');

    scripts.forEach((oldScript, index) => {
      if (oldScript.type === 'text/javascript' || !oldScript.type) {
        try {
          // 检查脚本是否已经执行过（通过标记属性）
          if (oldScript.hasAttribute('data-executed')) {
            console.log('SVG脚本已执行过，跳过重复执行');
            return;
          }

          const scriptContent = oldScript.textContent || '';

          // 检查脚本内容是否包含可能导致重复声明的关键字
          const hasBlockScopedDeclarations = /\b(let|const)\s+\w+/.test(scriptContent);

          // 创建新的script元素来执行代码
          const newScript = document.createElement('script');
          newScript.type = 'text/javascript';

          if (scriptContent) {
            if (hasBlockScopedDeclarations) {
              // 如果包含let/const声明，将其替换为var（允许重复声明）
              const modifiedContent = scriptContent
                .replace(/\blet\s+/g, 'var ')
                .replace(/\bconst\s+/g, 'var ');

              newScript.textContent = modifiedContent;
              console.log('SVG脚本包含let/const声明，已转换为var');
            } else {
              // 否则直接使用原始内容
              newScript.textContent = scriptContent;
            }
          }

          // 标记脚本已执行
          newScript.setAttribute('data-executed', 'true');

          // 替换旧脚本（这会触发执行）
          oldScript.parentNode?.replaceChild(newScript, oldScript);

          console.log(`已执行SVG内嵌脚本 #${index + 1}`);
        } catch (error) {
          console.warn('执行SVG脚本时出错:', error);
        }
      }
    });
  }

  /**
   * 处理复杂SVG（多色彩、渐变等） - 完全保持原有样式
   */
  private processComplexSvg(
    svgElement: SVGSVGElement,
    typeInfo: SvgTypeInfo,
    options: SvgRenderOptions
  ): void {
    console.log('处理复杂SVG - 完全保持原有颜色和样式');
    
    // 复杂SVG完全保持原有样式，不做任何颜色修改
    // 这确保了工业组件的原始设计意图得到保持
    return;
  }

  /**
   * 处理包含变换的SVG
   */
  private processTransformedSvg(
    svgElement: SVGSVGElement,
    typeInfo: SvgTypeInfo,
    options: SvgRenderOptions
  ): void {
    console.log('处理变换SVG - 调整viewBox和保持变换');
    
    // 对于变换SVG，主要关注viewBox的正确性
    if (typeInfo.recommendations.shouldForceViewBox && 
        !typeInfo.viewBoxInfo.hasViewBox) {
      // 尝试计算合适的viewBox
      this.calculateAndSetViewBox(svgElement);
    }

    // 保持原有的变换样式
    console.log('保持变换SVG的原有样式');
  }

  /**
   * 处理Inkscape生成的SVG - 只清理元数据，保持颜色
   */
  private processInkscapeSvg(
    svgElement: SVGSVGElement,
    typeInfo: SvgTypeInfo,
    options: SvgRenderOptions
  ): void {
    console.log('处理Inkscape SVG - 清理元数据但保持颜色');
    
    // 清理Inkscape特定的属性
    if (typeInfo.recommendations.shouldCleanAttributes) {
      this.cleanInkscapeAttributes(svgElement);
    }

    // Inkscape SVG也保持原有颜色，不应用主题色
    console.log('保持Inkscape SVG的原有颜色设计');
    return;
  }

  /**
   * 处理简单图标SVG - 新版本：默认保持原有颜色
   */
  private processSimpleSvg(
    svgElement: SVGSVGElement,
    typeInfo: SvgTypeInfo,
    options: SvgRenderOptions,
    componentName?: string
  ): void {
    console.log('处理简单SVG - 保持原有颜色');

    // 新策略：对于简单SVG也保持原有颜色，不强制应用主题色
    // 只在明确没有颜色定义的情况下才应用主题色
    if (options.fillColor || options.strokeColor || options.strokeWidth) {
      this.applyThemeStyles(svgElement, options, false, componentName); // 不强制覆盖，传递componentName
    }
  }

  /**
   * 应用主题样式 - 新版本：通过元素属性而非CCSS覆盖
   * 🌊 支持管道流动动画的智能颜色处理
   * 🎯 支持进度条的描边样式处理
   */
  private applyThemeStyles(
    svgElement: SVGSVGElement,
    options: SvgRenderOptions,
    forceOverride: boolean = false,
    componentName?: string
  ): void {
    // 如果没有颜色选项，直接返回
    if (!options.fillColor && !options.strokeColor && !options.strokeWidth) {
      return;
    }

    // 🌊 检测是否启用管道流动动画
    const isPipeFlowAnimation = options.animation === 'pipeFlow';

    // 🎯 检测是否是液位/进度条动画（liquidFill 和 liquidDrain）
    const isLevelAnimation = options.animation === 'liquidFill' || options.animation === 'liquidDrain';

    console.log('应用主题样式（元素属性方式）:', {
      fillColor: options.fillColor,
      strokeColor: options.strokeColor,
      strokeWidth: options.strokeWidth,
      forceOverride,
      isPipeFlowAnimation,  // 🌊
      isLevelAnimation      // 🎯
    });

    // 获取所有可着色的SVG元素
    const colorableElements = svgElement.querySelectorAll(
      'path, rect, circle, ellipse, polygon, polyline, line, text, tspan, use'
    );

    colorableElements.forEach((element: Element) => {
      // 🎯 液位/进度条特殊处理 - 仅修改边框，不修改内部填充
      if (isLevelAnimation) {
        const elementId = element.id;

        // 只对轨道元素应用描边样式（边框）
        if (elementId === 'A-GXP_TRACK') {
          console.log('🎯 检测到轨道元素，应用描边样式');

          if (options.strokeColor) {
            element.setAttribute('stroke', options.strokeColor);
          }

          if (options.strokeWidth !== undefined) {
            element.setAttribute('stroke-width', options.strokeWidth.toString());
          }

          // 保持原有的fill（不修改背景色）
          return;
        }

        // 对于填充元素(A-GXP_FILL)和其他装饰元素，保持原有颜色
        // 不做任何修改
        return;
      }

      // 只对需要主题化的元素应用颜色
      if (this.shouldApplyThemeToElement(element, forceOverride)) {

        // 🌊 管道流动动画特殊处理
        if (isPipeFlowAnimation) {
          // 对于管道流动，只应用描边，不应用填充
          console.log('🌊 检测到管道流动动画，仅应用描边样式');

          // 确保fill为none，保持管道空心
          element.setAttribute('fill', 'none');

          // 应用描边颜色 - 优先使用strokeColor，其次使用fillColor
          if (options.strokeColor || options.fillColor) {
            const pipeColor = options.strokeColor || options.fillColor;
            element.setAttribute('stroke', pipeColor!);
          }

          // 应用描边宽度
          if (options.strokeWidth !== undefined) {
            element.setAttribute('stroke-width', options.strokeWidth.toString());
          } else {
            // 默认描边宽度，确保管道可见
            const currentStrokeWidth = element.getAttribute('stroke-width');
            if (!currentStrokeWidth || parseFloat(currentStrokeWidth) === 0) {
              element.setAttribute('stroke-width', '2');
            }
          }
        } else {
          // 普通模式：正常应用填充和描边
          if (options.fillColor) {
            element.setAttribute('fill', options.fillColor);
          }

          if (options.strokeColor) {
            element.setAttribute('stroke', options.strokeColor);
          }

          if (options.strokeWidth !== undefined) {
            element.setAttribute('stroke-width', options.strokeWidth.toString());
          }
        }
      }
    });
  }

  /**
   * 判断元素是否应该应用主题色
   */
  private shouldApplyThemeToElement(element: Element, forceOverride: boolean): boolean {
    // 如果强制覆盖，对所有元素应用
    if (forceOverride) {
      return true;
    }

    // 检查元素当前的fill值
    const currentFill = element.getAttribute('fill');
    const currentStroke = element.getAttribute('stroke');

    // 检查style属性中的颜色（字符串形式）
    const styleAttr = element.getAttribute('style');
    const styleHasFill = styleAttr && styleAttr.includes('fill:');
    const styleHasStroke = styleAttr && styleAttr.includes('stroke:');

    // 🎨 检查style对象中的颜色（JavaScript形式）- 修复用户手动设置颜色被覆盖的问题
    const svgElement = element as SVGElement;
    const styleFill = svgElement.style.fill;
    const styleStroke = svgElement.style.stroke;

    // 🎨 如果用户已经通过style对象设置了颜色（非通用颜色），则不应用主题色
    if (styleFill && !this.isGenericColor(styleFill)) {
      console.log('🎨 检测到用户手动设置的填充颜色，跳过主题色应用:', styleFill);
      return false;
    }

    if (styleStroke && !this.isGenericColor(styleStroke)) {
      console.log('🎨 检测到用户手动设置的描边颜色，跳过主题色应用:', styleStroke);
      return false;
    }

    // 只对以下情况应用主题色：
    // 1. 没有颜色定义
    // 2. 使用了currentColor
    // 3. 使用了黑色或白色等通用颜色
    const hasNoColor = !currentFill && !currentStroke && !styleHasFill && !styleHasStroke && !styleFill && !styleStroke;
    const usesCurrentColor = currentFill === 'currentColor' || currentStroke === 'currentColor';
    const usesGenericColor = this.isGenericColor(currentFill) || this.isGenericColor(currentStroke);

    return hasNoColor || usesCurrentColor || usesGenericColor;
  }

  /**
   * 判断是否为通用颜色（黑、白、灰色）
   */
  private isGenericColor(color: string | null): boolean {
    if (!color) return false;
    
    const normalizedColor = color.toLowerCase().trim();
    const genericColors = [
      '#000000', '#000', 'black',
      '#ffffff', '#fff', 'white', 
      '#808080', '#888', '#999', '#aaa', '#bbb', '#ccc', '#ddd', '#eee',
      'rgb(0,0,0)', 'rgb(255,255,255)',
      'rgb(0, 0, 0)', 'rgb(255, 255, 255)',
      'none', 'transparent'
    ];
    
    return genericColors.includes(normalizedColor);
  }

  /**
   * 确保SVG的缩放支持
   */
  private ensureScalingSupport(
    svgElement: SVGSVGElement,
    componentName?: string
  ): void {
    // 检查是否已有缩放样式 - 修复：检查所有style标签的内容
    const existingStyles = svgElement.querySelectorAll('style');
    let hasScalingStyle = false;

    existingStyles.forEach(style => {
      if (style.textContent && style.textContent.includes('确保SVG能够正确缩放')) {
        hasScalingStyle = true;
      }
    });

    if (!hasScalingStyle) {
      const scaleStyle = document.createElement("style");
      scaleStyle.textContent = `
        /* 确保SVG能够正确缩放 */
        svg {
          width: 100% !important;
          height: 100% !important;
        }
      `;
      svgElement.insertBefore(scaleStyle, svgElement.firstChild);
    }
  }


  /**
   * 计算并设置适当的viewBox
   */
  private calculateAndSetViewBox(svgElement: SVGSVGElement): void {
    try {
      // 获取SVG的边界框
      const bbox = svgElement.getBBox();
      if (bbox.width > 0 && bbox.height > 0) {
        const padding = Math.max(bbox.width, bbox.height) * 0.1; // 10%的边距
        const x = bbox.x - padding;
        const y = bbox.y - padding;
        const width = bbox.width + padding * 2;
        const height = bbox.height + padding * 2;
        
        svgElement.setAttribute('viewBox', `${x} ${y} ${width} ${height}`);
        console.log(`自动设置viewBox: ${x} ${y} ${width} ${height}`);
      }
    } catch (error) {
      console.warn('无法计算viewBox，使用默认值:', error);
      svgElement.setAttribute('viewBox', '0 0 24 24');
    }
  }

  /**
   * 清理Inkscape特定属性
   */
  private cleanInkscapeAttributes(svgElement: SVGSVGElement): void {
    // 移除Inkscape命名空间声明
    svgElement.removeAttribute('xmlns:inkscape');
    svgElement.removeAttribute('xmlns:sodipodi');
    
    // 移除Inkscape特定属性
    const inkscapeAttrs = ['inkscape:version', 'sodipodi:docname', 'inkscape:current-layer'];
    inkscapeAttrs.forEach(attr => {
      svgElement.removeAttribute(attr);
    });
    
    // 移除sodipodi:namedview元素
    const namedviews = svgElement.querySelectorAll('sodipodi\\:namedview');
    namedviews.forEach(nv => nv.remove());
    
    console.log('已清理Inkscape元数据');
  }

  /**
   * 应用SVG滤镜效果 - 修复版本：确保滤镜正确应用到SVG元素
   * 🎨 智能识别SVG类型，避免破坏交互式和复杂SVG的样式
   */
  private applySvgFilters(svgElement: SVGSVGElement, options: SvgRenderOptions, componentName?: string): void {
    // 🎨 检测SVG类型 - 对于交互式SVG（液位罐等），完全跳过滤镜和描边处理
    const svgTypeInfo = SvgTypeDetector.detectSvgType(svgElement.outerHTML, componentName);
    const isInteractive = svgTypeInfo.type === 'interactive';
    const isComplex = svgTypeInfo.type === 'complex';

    // 🎨 特殊处理：液位罐和复杂进度条组件不应用描边
    const isSpecialProgressComponent = componentName && (
      componentName.includes('Tank') ||      // circularTankLevel, squareTankLevel
      componentName.includes('progress')     // progress-v 等进度条
    );

    if (isInteractive || isSpecialProgressComponent) {
      console.log(`🎨 检测到特殊SVG [${componentName}]，跳过滤镜和描边处理，保持原有样式`);
      return; // 完全跳过处理
    }

    // 首先清理旧的滤镜定义，避免重复
    const existingDefs = svgElement.querySelector('defs');
    if (existingDefs) {
      // 移除旧的drop-shadow和blur滤镜
      const oldFilters = existingDefs.querySelectorAll('filter[id^="drop-shadow-"], filter[id^="blur-"]');
      oldFilters.forEach(filter => filter.remove());
    }

    const defs = svgElement.querySelector('defs') || document.createElementNS('http://www.w3.org/2000/svg', 'defs');
    if (!svgElement.querySelector('defs')) {
      svgElement.insertBefore(defs, svgElement.firstChild);
    }

    const filters: string[] = [];
    // 使用时间戳确保滤镜ID唯一性，避免多个组件间的ID冲突
    const timestamp = Date.now();
    let filterId = 0;

    // 应用投影滤镜
    if (options.enableDropShadow) {
      const dropShadowId = `drop-shadow-${timestamp}-${filterId++}`;
      const dropShadowFilter = document.createElementNS('http://www.w3.org/2000/svg', 'filter');
      dropShadowFilter.id = dropShadowId;

      const feDropShadow = document.createElementNS('http://www.w3.org/2000/svg', 'feDropShadow');
      feDropShadow.setAttribute('dx', (options.dropShadowOffsetX || 2).toString());
      feDropShadow.setAttribute('dy', (options.dropShadowOffsetY || 2).toString());
      feDropShadow.setAttribute('stdDeviation', (options.dropShadowBlur || 4).toString());
      feDropShadow.setAttribute('flood-color', options.dropShadowColor || 'rgba(0,0,0,0.3)');

      dropShadowFilter.appendChild(feDropShadow);
      defs.appendChild(dropShadowFilter);
      filters.push(`url(#${dropShadowId})`);
    }

    // 应用模糊滤镜
    if (options.svgBlur && options.svgBlur > 0) {
      const blurId = `blur-${timestamp}-${filterId++}`;
      const blurFilter = document.createElementNS('http://www.w3.org/2000/svg', 'filter');
      blurFilter.id = blurId;

      const feGaussianBlur = document.createElementNS('http://www.w3.org/2000/svg', 'feGaussianBlur');
      feGaussianBlur.setAttribute('stdDeviation', options.svgBlur.toString());

      blurFilter.appendChild(feGaussianBlur);
      defs.appendChild(blurFilter);
      filters.push(`url(#${blurId})`);
    }

    // 应用渐变填充 - 保持原有逻辑
    if (options.gradientType && options.gradientType !== 'none') {
      const gradientId = `gradient-${filterId++}`;
      let gradient: SVGElement;

      if (options.gradientType === 'linear') {
        gradient = document.createElementNS('http://www.w3.org/2000/svg', 'linearGradient');
        gradient.id = gradientId;

        const direction = options.gradientDirection || 0;
        const radians = (direction * Math.PI) / 180;
        const x1 = Math.round(50 - 50 * Math.cos(radians));
        const y1 = Math.round(50 - 50 * Math.sin(radians));
        const x2 = Math.round(50 + 50 * Math.cos(radians));
        const y2 = Math.round(50 + 50 * Math.sin(radians));

        gradient.setAttribute('x1', `${x1}%`);
        gradient.setAttribute('y1', `${y1}%`);
        gradient.setAttribute('x2', `${x2}%`);
        gradient.setAttribute('y2', `${y2}%`);
      } else {
        gradient = document.createElementNS('http://www.w3.org/2000/svg', 'radialGradient');
        gradient.id = gradientId;
      }

      const stop1 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
      stop1.setAttribute('offset', '0%');
      stop1.setAttribute('stop-color', options.gradientStart || '#409eff');

      const stop2 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
      stop2.setAttribute('offset', '100%');
      stop2.setAttribute('stop-color', options.gradientEnd || '#67c23a');

      gradient.appendChild(stop1);
      gradient.appendChild(stop2);
      defs.appendChild(gradient);

      // 应用渐变到适合的元素（只对没有特定颜色的元素）
      const shapes = svgElement.querySelectorAll('path, rect, circle, ellipse, polygon, polyline');
      shapes.forEach(shape => {
        if (this.shouldApplyThemeToElement(shape, false)) {
          shape.setAttribute('fill', `url(#${gradientId})`);
        }
      });
    }

    // 🎯 检测是否是液位/进度条动画
    const isLevelAnimation = options.animation === 'liquidFill' || options.animation === 'liquidDrain';

    // 应用其他描边属性（不包括颜色）和滤镜效果
    const shapes = svgElement.querySelectorAll('path, rect, circle, ellipse, polygon, polyline');

    // 如果找到了shape元素，应用到每个shape
    if (shapes.length > 0) {
      shapes.forEach(shape => {
        // 🎯 液位/进度条特殊处理 - 只对轨道应用描边属性
        if (isLevelAnimation) {
          const elementId = (shape as Element).id;

          // 只对轨道元素(A-GXP_TRACK)应用描边属性
          if (elementId === 'A-GXP_TRACK') {
            if (options.strokeDasharray && options.strokeDasharray !== 'none') {
              shape.setAttribute('stroke-dasharray', options.strokeDasharray);
            } else if (options.strokeDasharray === 'none') {
              shape.removeAttribute('stroke-dasharray');
            }
            if (options.strokeLinecap) {
              shape.setAttribute('stroke-linecap', options.strokeLinecap);
            }
            if (options.strokeLinejoin) {
              shape.setAttribute('stroke-linejoin', options.strokeLinejoin);
            }
          }

          // 对填充元素和其他元素不应用任何描边属性
          // 保持原有样式
          return;
        }

        // 普通模式：应用非颜色属性
        if (options.strokeDasharray && options.strokeDasharray !== 'none') {
          shape.setAttribute('stroke-dasharray', options.strokeDasharray);
        } else if (options.strokeDasharray === 'none') {
          shape.removeAttribute('stroke-dasharray');
        }
        if (options.strokeLinecap) {
          shape.setAttribute('stroke-linecap', options.strokeLinecap);
        }
        if (options.strokeLinejoin) {
          shape.setAttribute('stroke-linejoin', options.strokeLinejoin);
        }
        if (options.fillOpacity !== undefined) {
          shape.setAttribute('fill-opacity', options.fillOpacity.toString());
        }
        if (options.strokeOpacity !== undefined) {
          shape.setAttribute('stroke-opacity', options.strokeOpacity.toString());
        }

        // 应用滤镜
        if (filters.length > 0) {
          shape.setAttribute('filter', filters.join(' '));
        }
      });
    } else {
      // 如果没有找到直接的shape元素（可能SVG使用了<g>分组或其他复杂结构）
      // 将滤镜应用到所有可能包含图形的元素，包括<g>标签
      const allGraphicElements = svgElement.querySelectorAll('g, path, rect, circle, ellipse, polygon, polyline, line, text, image, use');

      if (allGraphicElements.length > 0) {
        // 应用滤镜到找到的元素
        allGraphicElements.forEach(element => {
          if (filters.length > 0) {
            element.setAttribute('filter', filters.join(' '));
          }

          // 也应用其他样式属性
          if (options.strokeDasharray && options.strokeDasharray !== 'none') {
            element.setAttribute('stroke-dasharray', options.strokeDasharray);
          } else if (options.strokeDasharray === 'none') {
            element.removeAttribute('stroke-dasharray');
          }
          if (options.strokeLinecap) {
            element.setAttribute('stroke-linecap', options.strokeLinecap);
          }
          if (options.strokeLinejoin) {
            element.setAttribute('stroke-linejoin', options.strokeLinejoin);
          }
          if (options.fillOpacity !== undefined) {
            element.setAttribute('fill-opacity', options.fillOpacity.toString());
          }
          if (options.strokeOpacity !== undefined) {
            element.setAttribute('stroke-opacity', options.strokeOpacity.toString());
          }
        });
      } else {
        // 作为最后的fallback，直接应用到SVG根元素
        if (filters.length > 0) {
          svgElement.setAttribute('filter', filters.join(' '));
          console.log('滤镜直接应用到SVG根元素:', filters.join(' '));
        }
      }
    }
  }

  /**
   * 清除SVG元素上的所有动画
   * @param svgElement SVG元素
   */
  private clearAllAnimations(svgElement: SVGSVGElement): void {
    let hasAnimation = false;

    // 🛑 清除进度条动画的 requestAnimationFrame 循环
    const progressAnimationId = (svgElement as any)._progressAnimationLoopId;
    if (progressAnimationId) {
      console.log('🛑 取消进度条动画循环:', progressAnimationId);
      cancelAnimationFrame(progressAnimationId);
      delete (svgElement as any)._progressAnimationLoopId;
      hasAnimation = true;
    }

    // 🛑 清除进度条动画的 setTimeout 定时器
    const progressTimeoutId = (svgElement as any)._progressAnimationTimeoutId;
    if (progressTimeoutId) {
      console.log('🛑 清除进度条动画的setTimeout:', progressTimeoutId);
      clearTimeout(progressTimeoutId);
      delete (svgElement as any)._progressAnimationTimeoutId;
      hasAnimation = true;
    }

    // 🛑 清除液体动画的 requestAnimationFrame 循环
    const liquidAnimationId = (svgElement as any)._liquidAnimationLoopId;
    if (liquidAnimationId) {
      console.log('🛑 取消液体动画循环:', liquidAnimationId);
      cancelAnimationFrame(liquidAnimationId);
      delete (svgElement as any)._liquidAnimationLoopId;
      hasAnimation = true;
    }

    // 🛑 清除液体动画的 setTimeout 定时器
    const liquidTimeoutId = (svgElement as any)._liquidAnimationTimeoutId;
    if (liquidTimeoutId) {
      console.log('🛑 清除液体动画的setTimeout:', liquidTimeoutId);
      clearTimeout(liquidTimeoutId);
      delete (svgElement as any)._liquidAnimationTimeoutId;
      hasAnimation = true;
    }

    // 🛑 清除CSS动画
    if (svgElement.style.animation) {
      svgElement.style.animation = '';
      hasAnimation = true;
    }

    if (hasAnimation) {
      console.log('✅ 已清除SVG元素上的所有动画');
    }
  }

  /**
   * 添加动画效果
   */
  private addAnimation(
    svgElement: SVGSVGElement,
    animation: string,
    speed: string = "normal",
    options?: SvgRenderOptions
  ): void {
    // 🛑 在添加新动画前，先清除所有旧动画
    this.clearAllAnimations(svgElement);

    // 🔍 检查是否为液体/进度相关的动画或设置
    const isLiquidRelatedAnimation =
      animation === 'none' ||
      animation === 'liquidFill' ||
      animation === 'liquidDrain';

    // 🔍 如果是液体相关的动画，但组件不支持，则跳过
    if (isLiquidRelatedAnimation && !this.supportsLiquidAnimation(svgElement)) {
      console.warn(`⚠️ 当前组件不支持液体/进度动画，跳过 ${animation} 设置`);
      return;
    }

    // 🎯 获取目标值
    const targetValue = options?.animationStaticValue !== undefined
      ? options.animationStaticValue
      : this.getDefaultValueFromSvg(svgElement);

    // 🎯 当动画为none时，直接设置目标值
    if (animation === "none") {
      this.setStaticLevelValue(svgElement, targetValue);
      return;
    }

    // 🎯 特殊处理液体动画 - 动画播放完后停留在目标值位置
    if (animation === 'liquidFill' || animation === 'liquidDrain') {
      this.addLiquidAnimation(svgElement, animation, speed, options);
      return;
    }

    // 特殊处理管道流动动画 - 需要特殊的stroke-dasharray动画
    if (animation === 'pipeFlow') {
      this.addPipeFlowAnimation(svgElement, speed, options);
      return;
    }

    // 特殊处理开关切换动画 - 添加平滑过渡效果
    if (animation === 'switchToggle') {
      this.addSwitchToggleAnimation(svgElement, speed, options);
      return;
    }

    const speedMap = {
      slow: "3s",
      normal: "2s",
      fast: "1s"
    };

    const duration = options?.animationDuration || speedMap[speed as keyof typeof speedMap] || "2s";
    const iterationCount = options?.animationIterationCount || "infinite";
    const timingFunction = options?.animationTimingFunction || "ease";
    const delay = options?.animationDelay || "0s";

    const animationName = `svg-animation-${animation}`;
    svgElement.style.animation = `${animationName} ${duration} ${timingFunction} ${delay} ${iterationCount}`;

    // 悬停暂停效果
    if (options?.animationPlayStateOnHover) {
      svgElement.addEventListener('mouseenter', () => {
        svgElement.style.animationPlayState = 'paused';
      });
      svgElement.addEventListener('mouseleave', () => {
        svgElement.style.animationPlayState = 'running';
      });
    }

    // 添加动画样式到文档头部（如果还没有的话）
    this.ensureAnimationStyles();
  }

  /**
   * 添加液体动画效果 - 液位从0%上涨到100%或从100%下降到0%
   * 🎯 语义包装：调用统一的底层实现
   */
  private addLiquidAnimation(
    svgElement: SVGSVGElement,
    animation: string,
    speed: string = "normal",
    options?: SvgRenderOptions
  ): void {
    console.log('🌊 液体动画入口:', { animation, speed, options });

    // 确定动画方向
    const direction = animation === 'liquidFill' ? 'up' : 'down';

    // 调用统一的底层实现
    this.addLevelSlideAnimation(svgElement, direction, speed, options);
  }

  // 🗑️ 原addLiquidAnimation的170+行实现代码已移除
  // 功能已整合到统一的addLevelSlideAnimation函数中

  /**
   * 液体动画降级方案 - 直接操作SVG元素
   */
  private addLiquidAnimationFallback(
    svgElement: SVGSVGElement,
    animation: string,
    speed: string = "normal",
    options?: SvgRenderOptions
  ): void {
    console.log('使用降级方案 - addLiquidAnimationFallback');

    // 查找液体相关的元素
    const waterShape = svgElement.querySelector('#waterShape, [id*="water"], [id*="liquid"]');

    console.log('查找液体元素:', {
      found: !!waterShape,
      selector: '#waterShape, [id*="water"], [id*="liquid"]',
      allElements: Array.from(svgElement.querySelectorAll('*')).map(el => ({
        tagName: el.tagName,
        id: el.id,
        classes: el.className
      })).slice(0, 10) // 只显示前10个元素
    });

    if (!waterShape) {
      console.warn('未找到液体元素，无法应用液体动画');
      console.log('SVG结构:', svgElement.outerHTML.substring(0, 500)); // 输出前500个字符
      return;
    }

    const speedMap = {
      slow: "4s",
      normal: "2s",
      fast: "1s"
    };

    const duration = options?.animationDuration || speedMap[speed as keyof typeof speedMap] || "2s";
    const iterationCount = options?.animationIterationCount || "infinite";
    const timingFunction = options?.animationTimingFunction || "ease-in-out";
    const delay = options?.animationDelay || "0s";

    // 创建液体动画样式 - 使用scaleY和translateY模拟液位变化
    const animationName = animation === 'liquidFill' ? 'liquid-fill-up' : 'liquid-drain-down';

    // 应用动画到液体元素
    const animationValue = `${animationName} ${duration} ${timingFunction} ${delay} ${iterationCount}`;
    (waterShape as HTMLElement).style.animation = animationValue;
    (waterShape as HTMLElement).style.transformOrigin = 'bottom'; // 从底部开始缩放

    console.log('降级方案动画已应用:', {
      element: waterShape.tagName,
      id: (waterShape as Element).id,
      animationValue,
      transformOrigin: 'bottom'
    });

    // 确保液体动画样式已添加
    this.ensureLiquidAnimationStyles();
  }

  /**
   * 确保液体动画样式存在
   */
  private ensureLiquidAnimationStyles(): void {
    const existingStyle = document.getElementById("liquid-animations");
    if (existingStyle) return;

    const style = document.createElement("style");
    style.id = "liquid-animations";
    style.textContent = `
      /* 液体上涨动画 - 从0%到100% */
      @keyframes liquid-fill-up {
        0% {
          transform: translateY(100%) scaleY(0);
          opacity: 0.3;
        }
        100% {
          transform: translateY(0%) scaleY(1);
          opacity: 1;
        }
      }

      /* 液体下降动画 - 从100%到0% */
      @keyframes liquid-drain-down {
        0% {
          transform: translateY(0%) scaleY(1);
          opacity: 1;
        }
        100% {
          transform: translateY(100%) scaleY(0);
          opacity: 0.3;
        }
      }
    `;
    document.head.appendChild(style);
  }

  /**
   * 🎯 统一的液位/进度滑动动画底层实现
   * @param svgElement SVG元素
   * @param direction 动画方向：'up'(从0到目标值) 或 'down'(从目标值到0)
   * @param speed 动画速度
   * @param options 渲染选项
   */
  private addLevelSlideAnimation(
    svgElement: SVGSVGElement,
    direction: 'up' | 'down',
    speed: string = "normal",
    options?: SvgRenderOptions
  ): void {
    console.log('🎯 统一液位滑动动画:', { direction, speed, options });

    const svgInstance = (svgElement as any).__svgInstance;

    if (!svgInstance || !svgInstance.putValue) {
      console.warn('⚠️ SVG实例未初始化或缺少putValue函数，使用降级方案');
      this.addLiquidAnimationFallback(svgElement, direction === 'up' ? 'liquidFill' : 'liquidDrain', speed, options);
      return;
    }

    const putValueFunc = svgInstance.putValue;

    // 解析动画时长
    const speedMap = {
      slow: 4000,    // 4秒
      normal: 2000,  // 2秒
      fast: 1000     // 1秒
    };

    let duration = speedMap[speed as keyof typeof speedMap] || 2000;

    // 如果提供了animationDuration，优先使用它
    if (options?.animationDuration) {
      const durationStr = options.animationDuration;
      const match = durationStr.match(/^([\d.]+)(s|ms)?$/);
      if (match) {
        const value = parseFloat(match[1]);
        const unit = match[2] || 's';
        duration = unit === 'ms' ? value : value * 1000;
      }
    }

    const iterationCount = options?.animationIterationCount || "infinite";
    const isInfinite = iterationCount === "infinite";

    // 🎯 获取目标值
    const targetValue = options?.animationStaticValue !== undefined
      ? Math.max(0, Math.min(100, options.animationStaticValue))
      : this.getDefaultValueFromSvg(svgElement);

    // 根据方向确定起点和终点
    const startValue = direction === 'up' ? 0 : targetValue;
    const endValue = direction === 'up' ? targetValue : 0;

    let currentIteration = 0;
    let targetIterations = isInfinite ? Infinity : parseInt(iterationCount as string);

    console.log('🎯 统一动画参数:', {
      direction: direction === 'up' ? '上升↑' : '下降↓',
      startValue: `${startValue}%`,
      endValue: `${endValue}%`,
      targetValue: `${targetValue}%`,
      duration: `${duration}ms`,
      iterationCount,
      isInfinite,
      targetIterations
    });

    // 取消之前的动画（如果存在）
    const animationId = (svgElement as any)._liquidAnimationLoopId;
    if (animationId) {
      cancelAnimationFrame(animationId);
      console.log('🛑 取消之前的液位动画');
    }
    const timeoutId = (svgElement as any)._liquidAnimationTimeoutId;
    if (timeoutId) {
      clearTimeout(timeoutId);
      console.log('🛑 取消之前的液位动画延迟');
    }

    const animate = () => {
      if (currentIteration >= targetIterations) {
        delete (svgElement as any)._liquidAnimationLoopId;
        console.log(`✅ 液位动画完成，共循环${currentIteration}次`);
        return;
      }

      // 每次循环开始前，重置液位到起点
      const svgInstance = (svgElement as any).__svgInstance;
      if (svgInstance && typeof svgInstance.updateWaterLevel === 'function') {
        svgInstance._pn_value = startValue;
        svgInstance.updateWaterLevel(startValue);
      }

      const startTime = Date.now();

      const frame = () => {
        // 检查动画是否已被取消
        if (!(svgElement as any)._liquidAnimationLoopId) {
          console.log('⚠️ 液位动画已被取消，停止frame回调');
          return;
        }

        const elapsed = Date.now() - startTime;
        const progress = Math.min(elapsed / duration, 1);

        // 使用缓动函数
        const easedProgress = this.easeInOutCubic(progress);
        const currentValue = startValue + (endValue - startValue) * easedProgress;

        // 更新液位
        try {
          const svgInstance = (svgElement as any).__svgInstance;

          // 优先使用updateWaterLevel或updateProgressLevel
          if (svgInstance && typeof svgInstance.updateWaterLevel === 'function') {
            svgInstance._pn_value = currentValue;
            svgInstance.updateWaterLevel(currentValue);
          } else if (svgInstance && typeof svgInstance.updateProgressLevel === 'function') {
            svgInstance._pn_value = currentValue;
            svgInstance.updateProgressLevel(currentValue);
          } else {
            // 降级：使用putValue
            putValueFunc('_pn_value', currentValue);
          }
        } catch (error) {
          console.warn('更新液位失败:', error);
        }

        if (progress < 1) {
          const frameId = requestAnimationFrame(frame);
          (svgElement as any)._liquidAnimationLoopId = frameId;
        } else {
          // 一次循环完成
          currentIteration++;

          if (currentIteration < targetIterations) {
            // 继续下一次循环，延迟300ms
            const timeoutId = setTimeout(() => {
              if (!(svgElement as any)._liquidAnimationLoopId && !(svgElement as any)._liquidAnimationTimeoutId) {
                console.log('⚠️ 液位动画已被取消，停止setTimeout回调');
                return;
              }
              delete (svgElement as any)._liquidAnimationTimeoutId;
              animate();
            }, 300);
            (svgElement as any)._liquidAnimationTimeoutId = timeoutId;
          } else {
            // 所有循环完成
            delete (svgElement as any)._liquidAnimationLoopId;
            delete (svgElement as any)._liquidAnimationTimeoutId;
            console.log(`✅ 液位动画完成，共循环${currentIteration}次`);
          }
        }
      };

      const frameId = requestAnimationFrame(frame);
      (svgElement as any)._liquidAnimationLoopId = frameId;
    };

    // 开始动画
    console.log(`🚀 开始液位动画: ${direction === 'up' ? '上升' : '下降'}, 时长: ${duration}ms, 循环: ${iterationCount}`);
    animate();
  }

  /**
   * 缓动函数 - 三次方缓入缓出
   */
  private easeInOutCubic(t: number): number {
    return t < 0.5
      ? 4 * t * t * t
      : 1 - Math.pow(-2 * t + 2, 3) / 2;
  }


  /**
   * 添加管道流动动画效果
   * @param svgElement SVG元素
   * @param speed 动画速度
   * @param options 渲染选项
   */
  private addPipeFlowAnimation(
    svgElement: SVGSVGElement,
    speed: string = "normal",
    options?: SvgRenderOptions
  ): void {
    const speedMap = {
      slow: 4000,    // 4秒
      normal: 2000,  // 2秒
      fast: 1000     // 1秒
    };

    let duration = speedMap[speed as keyof typeof speedMap] || 2000;

    // 优先使用自定义时长
    if (options?.animationDuration) {
      const durationStr = options.animationDuration;
      const match = durationStr.match(/^([\d.]+)(s|ms)?$/);
      if (match) {
        const value = parseFloat(match[1]);
        const unit = match[2] || 's';
        duration = unit === 'ms' ? value : value * 1000;
      }
    }

    const direction = options?.pipeFlowDirection || 'forward';
    const iterationCount = options?.animationIterationCount || 'infinite';
    const durationStr = options?.animationDuration || `${duration / 1000}s`;

    // 打印启动信息
    console.log('🌊 添加管道流动动画:', {
      speed,
      direction,
      duration: durationStr
    });

    // 查找所有管道路径元素（path, line, polyline等）
    const allPipeElements = svgElement.querySelectorAll('path, line, polyline');

    if (allPipeElements.length === 0) {
      console.warn('⚠️ 未找到管道元素（path/line/polyline），无法应用流动动画');
      return;
    }

    // 🌊 只对原本就有stroke-dasharray的元素应用流动动画
    // 这样可以避免边框等实线元素也流动
    const pipeElements = Array.from(allPipeElements).filter(element => {
      const hasDashArray = element.hasAttribute('stroke-dasharray') ||
        element.getAttribute('style')?.includes('stroke-dasharray');
      return hasDashArray;
    });

    if (pipeElements.length === 0) {
      console.warn('⚠️ 未找到带虚线的管道元素，无法应用流动动画');
      console.log('💡 提示：只有原本就有stroke-dasharray的元素才会应用流动动画');
      return;
    }

    console.log(`找到 ${pipeElements.length} 个虚线管道元素（共${allPipeElements.length}个path）`);

    pipeElements.forEach((element, index) => {
      const pathElement = element as SVGGeometryElement;

      // 🌊 检查并显示隐藏的flowline组
      // 遍历父级元素，将visibility="hidden"改为visible
      let parent = pathElement.parentElement;
      while (parent && parent !== svgElement) {
        if (parent.getAttribute('visibility') === 'hidden') {
          parent.setAttribute('visibility', 'visible');
          console.log(`🌊 显示隐藏的父级组: id="${parent.id || '(无ID)'}", visibility="${parent.getAttribute('visibility')}"`);
        }
        parent = parent.parentElement;
      }

      // 🌊 强制设置管道样式 - 确保管道可见且保持空心
      // 1. 设置fill="none"，保持管道空心（不被填充）
      pathElement.setAttribute('fill', 'none');

      // 2. 设置stroke颜色 - 优先使用strokeColor，其次fillColor，最后保留原色
      let pipeColor = pathElement.getAttribute('stroke') || '#000000'; // 默认保留原色或黑色
      if (options?.strokeColor || options?.fillColor) {
        pipeColor = options?.strokeColor || options?.fillColor || pipeColor;
        pathElement.setAttribute('stroke', pipeColor);
      }

      // 3. 设置stroke-width - 保留原值或使用用户设置
      let strokeWidth = pathElement.getAttribute('stroke-width') || '2';
      if (options?.strokeWidth !== undefined) {
        strokeWidth = options.strokeWidth.toString();
        pathElement.setAttribute('stroke-width', strokeWidth);
      }

      // 4. 设置虚线样式（模拟流体颗粒）
      // dasharray: "实线长度 间隙长度" - 创建流动的虚线效果
      pathElement.setAttribute('stroke-dasharray', '10 5');

      // 根据方向设置不同的动画
      let animationName = '';
      switch (direction) {
        case 'forward':
          animationName = 'pipe-flow-forward';
          break;
        case 'backward':
          animationName = 'pipe-flow-backward';
          break;
        case 'bidirectional':
          animationName = 'pipe-flow-bidirectional';
          break;
        default:
          animationName = 'pipe-flow-forward';
      }

      // 应用CSS动画
      const animationDuration = `${duration}ms`;
      const timingFunction = options?.animationTimingFunction || 'linear';
      const delay = options?.animationDelay || '0s';

      pathElement.style.animation = `${animationName} ${animationDuration} ${timingFunction} ${delay} ${iterationCount}`;

      console.log(`🌊 管道元素 #${index + 1} 应用动画: ${animationName}, stroke: ${pipeColor}, width: ${strokeWidth}`);
    });

    // 确保动画样式已添加到文档
    this.ensurePipeFlowAnimationStyles();

    console.log('✅ 管道流动动画样式已添加');
  }

  /**
   * 确保管道流动动画样式存在
   */
  private ensurePipeFlowAnimationStyles(): void {
    const existingStyle = document.getElementById("pipe-flow-animations");
    if (existingStyle) return;

    const style = document.createElement("style");
    style.id = "pipe-flow-animations";
    style.textContent = `
      /* 管道流动动画 - 正向流动 */
      @keyframes pipe-flow-forward {
        0% {
          stroke-dashoffset: 0;
        }
        100% {
          stroke-dashoffset: -15;  /* 负值表示向前移动 */
        }
      }

      /* 管道流动动画 - 反向流动 */
      @keyframes pipe-flow-backward {
        0% {
          stroke-dashoffset: 0;
        }
        100% {
          stroke-dashoffset: 15;  /* 正值表示向后移动 */
        }
      }

      /* 管道流动动画 - 双向流动（往返） */
      @keyframes pipe-flow-bidirectional {
        0% {
          stroke-dashoffset: 0;
        }
        50% {
          stroke-dashoffset: -15;
        }
        100% {
          stroke-dashoffset: 0;
        }
      }
    `;
    document.head.appendChild(style);
    console.log('✅ 管道流动动画样式已添加');
  }

  /**
   * 添加开关切换动画效果 - 为开关组件添加平滑过渡
   * 支持独立的开/关状态管理
   */
  private addSwitchToggleAnimation(
    svgElement: SVGSVGElement,
    speed: string = "normal",
    options?: SvgRenderOptions
  ): void {
    console.log('🔘 添加开关切换动画:', { speed, options });

    // 速度映射
    const speedMap = {
      slow: "0.5s",
      normal: "0.3s",
      fast: "0.15s"
    };

    const duration = options?.animationDuration || speedMap[speed as keyof typeof speedMap] || "0.3s";
    const timingFunction = options?.animationTimingFunction || "cubic-bezier(0.4, 0, 0.2, 1)";

    // 为容器元素添加过渡效果
    const container = svgElement.parentElement;
    if (container) {
      container.style.transition = `background-color ${duration} ${timingFunction}, border-color ${duration} ${timingFunction}`;
      console.log('容器过渡效果已添加');
    }

    // 为SVG内部元素添加过渡效果
    const ellipse = svgElement.querySelector('ellipse, circle') as SVGElement;
    if (ellipse) {
      ellipse.style.transition = `transform ${duration} ${timingFunction}, fill ${duration} ease`;

      // 🔘 应用初始状态颜色（如果已定义）
      const switchState = (options as any)?.switchState || 'off';
      const isOn = switchState === 'on';
      const color = isOn
        ? ((options as any)?.switchOnColor || '#67c23a')
        : ((options as any)?.switchOffColor || '#909399');

      ellipse.style.fill = color;

      // 🔘 应用初始位置（开关位置）
      // 根据SVG的viewBox="0 0 21 21"计算：
      // 椭圆中心cx=7.4817，半径rx=4.7386
      // 开启状态应移动到右侧：约6个SVG单位 = 28.6%的宽度
      const translateX = isOn ? '28.6%' : '0';
      ellipse.style.transform = `translateX(${translateX})`;
      ellipse.style.transformOrigin = 'center';

      console.log(`🔘 椭圆（圆球）过渡效果已添加，初始状态: ${isOn ? '开启' : '关闭'}，颜色: ${color}, 位置: ${translateX}`);
    }

    const path = svgElement.querySelector('path');
    if (path) {
      (path as SVGElement).style.transition = `stroke ${duration} ease, fill ${duration} ease`;

      // 🔘 设置初始轨道边框颜色（不修改fill背景色，保持SVG原有透明背景）
      const switchState = (options as any)?.switchState || 'off';
      const isOn = switchState === 'on';

      const trackStroke = isOn
        ? ((options as any)?.switchOnColor || '#67c23a')
        : '#dcdfe6';
      (path as SVGElement).style.stroke = trackStroke;

      console.log(`路径（轨道）过渡效果已添加，边框: ${trackStroke}`);
    }

    // 🔘 保存过渡配置到元素，供后续状态切换使用
    (svgElement as any)._switchConfig = {
      duration,
      timingFunction,
      currentState: (options as any)?.switchState || 'off'
    };

    console.log('✅ 开关切换动画已配置');
  }

  /**
   * 更新开关状态 - 平滑切换开关的开/关状态
   * @param svgElement SVG元素
   * @param newState 新状态 ('on' 或 'off')
   * @param onColor 开启状态颜色（可选）
   * @param offColor 关闭状态颜色（可选）
   */
  updateSwitchState(
    svgElement: SVGSVGElement,
    newState: 'on' | 'off',
    onColor?: string,
    offColor?: string
  ): void {
    const config = (svgElement as any)._switchConfig;
    if (!config) {
      console.warn('🔘 开关配置未找到，请先启用开关切换动画');
      return;
    }

    const isOn = newState === 'on';
    const color = isOn
      ? (onColor || '#67c23a')
      : (offColor || '#909399');

    console.log(`🔘 更新开关状态: ${config.currentState} → ${newState}, 颜色: ${color}`);

    // 查找开关圆球元素
    const ellipse = svgElement.querySelector('ellipse, circle') as SVGElement;
    if (ellipse) {
      // 应用颜色
      ellipse.style.fill = color;

      // 应用位置（开关位置）
      // 使用百分比以适应SVG缩放
      const translateX = isOn ? '28.6%' : '0';
      ellipse.style.transform = `translateX(${translateX})`;
      ellipse.style.transformOrigin = 'center';

      console.log(`🔘 开关状态已更新: ${newState}, 位置: ${translateX}`);
    }

    // 更新轨道边框颜色（不修改fill背景色，保持透明）
    const path = svgElement.querySelector('path') as SVGElement;
    if (path) {
      // 边框颜色跟随状态变化
      const trackStroke = isOn
        ? (onColor || '#67c23a')
        : '#dcdfe6';
      path.style.stroke = trackStroke;
    }

    // 更新配置中的当前状态
    config.currentState = newState;
  }

  /**
   * 数据绑定驱动的液位更新 - 从当前值平滑过渡到目标值
   * @param svgElement SVG元素
   * @param targetValue 目标液位值（0-100）
   * @param duration 动画时长（毫秒），默认1000ms
   */
  updateLiquidLevel(svgElement: SVGSVGElement, targetValue: number, duration: number = 1000): void {
    // 限制目标值范围
    targetValue = Math.max(0, Math.min(100, targetValue));

    // 尝试调用SVG内部的putValue函数
    let putValueFunc = (svgElement as any).putValue;

    if (!putValueFunc) {
      console.warn('未找到putValue函数，尝试直接操作液体元素');
      this.updateLiquidLevelFallback(svgElement, targetValue, duration);
      return;
    }

    // 获取当前液位值（尝试从SVG元素的data属性读取）
    let currentValue = parseFloat(svgElement.getAttribute('data-current-level') || '0');

    // 如果当前值与目标值相同，直接返回
    if (Math.abs(currentValue - targetValue) < 0.1) {
      return;
    }

    console.log(`液位更新: ${currentValue.toFixed(1)}% → ${targetValue.toFixed(1)}%`);

    const startTime = Date.now();
    const startValue = currentValue;
    const deltaValue = targetValue - startValue;

    // 取消之前的动画（如果存在）
    const animationId = (svgElement as any)._liquidAnimationId;
    if (animationId) {
      cancelAnimationFrame(animationId);
    }

    const animate = () => {
      const elapsed = Date.now() - startTime;
      const progress = Math.min(elapsed / duration, 1);

      // 使用缓动函数
      const easedProgress = this.easeInOutCubic(progress);
      const currentValue = startValue + deltaValue * easedProgress;

      // 调用putValue更新液位
      // 注意：SVG的putValue函数需要两个参数 (id, value)
      // id为 '_pn_value' 表示液位值
      try {
        putValueFunc('_pn_value', currentValue);

        // 保存当前液位值
        svgElement.setAttribute('data-current-level', currentValue.toString());
      } catch (error) {
        console.warn('调用putValue失败:', error);
      }

      if (progress < 1) {
        const newAnimationId = requestAnimationFrame(animate);
        (svgElement as any)._liquidAnimationId = newAnimationId;
      } else {
        // 动画完成，清除动画ID
        delete (svgElement as any)._liquidAnimationId;
        console.log(`液位更新完成: ${targetValue.toFixed(1)}%`);
      }
    };

    // 开始动画
    const newAnimationId = requestAnimationFrame(animate);
    (svgElement as any)._liquidAnimationId = newAnimationId;
  }

  /**
   * 液位更新降级方案 - 直接操作液体元素
   */
  private updateLiquidLevelFallback(svgElement: SVGSVGElement, targetValue: number, duration: number = 1000): void {
    const waterShape = svgElement.querySelector('#waterShape, [id*="water"], [id*="liquid"]');

    if (!waterShape) {
      console.warn('未找到液体元素，无法更新液位');
      return;
    }

    // 使用CSS transition实现平滑过渡
    const waterElement = waterShape as HTMLElement;
    waterElement.style.transition = `transform ${duration}ms cubic-bezier(0.4, 0, 0.2, 1)`;

    // 计算液位对应的transform值
    // 假设0%液位对应translateY(100%)，100%液位对应translateY(0%)
    const translateY = 100 - targetValue;
    const scaleY = targetValue / 100;

    waterElement.style.transform = `translateY(${translateY}%) scaleY(${scaleY})`;
    waterElement.style.transformOrigin = 'bottom';

    // 保存当前液位值
    svgElement.setAttribute('data-current-level', targetValue.toString());

    console.log(`液位更新完成（降级方案）: ${targetValue.toFixed(1)}%`);
  }

  /**
   * 🔍 检查SVG组件是否支持液体/进度条动画
   * 只有progress-v、circularTankLevel-v2、squareTankLevel-v2支持
   * @param svgElement SVG元素
   * @returns 是否支持液体/进度条动画
   */
  private supportsLiquidAnimation(svgElement: SVGSVGElement): boolean {
    // 🔑 优先检查关键元素（即使SVG脚本还未执行）
    const hasProgressElements = !!svgElement.querySelector('#A-GXP_FILL'); // 进度条
    const hasWaterElements = !!svgElement.querySelector('#waterShape'); // 液位罐

    // 如果有关键元素，说明支持液体动画
    if (hasProgressElements || hasWaterElements) {
      console.log('✅ 通过元素检测确认支持液体动画:', { hasProgressElements, hasWaterElements });
      return true;
    }

    // 其次检查SVG实例数据（如果脚本已执行）
    const svgInstance = (svgElement as any).__svgInstance;
    if (svgInstance) {
      const hasUpdateFunction =
        typeof svgInstance.updateWaterLevel === 'function' ||
        typeof svgInstance.updateProgressLevel === 'function';

      if (hasUpdateFunction) {
        console.log('✅ 通过函数检测确认支持液体动画');
        return true;
      }
    }

    console.log('⚠️ 组件不支持液体动画');
    return false;
  }

  /**
   * 🎯 从SVG内部读取默认值
   * @param svgElement SVG元素
   * @returns 默认值（0-100）
   */
  private getDefaultValueFromSvg(svgElement: SVGSVGElement): number {
    // 🔑 从SVG元素本身读取实例数据，而不是全局window
    const svgInstance = (svgElement as any).__svgInstance;

    if (svgInstance) {
      // 优先读取当前值
      if (typeof svgInstance._pn_value !== 'undefined') {
        console.log(`📖 从SVG实例读取到默认值 _pn_value: ${svgInstance._pn_value}%`);
        return svgInstance._pn_value;
      }

      // 其次读取初始值
      if (typeof svgInstance.initValue !== 'undefined') {
        console.log(`📖 从SVG实例读取到默认值 initValue: ${svgInstance.initValue}%`);
        return svgInstance.initValue;
      }
    }

    // 如果都读取不到，使用默认值30
    console.log(`⚠️ 未找到SVG实例数据，使用fallback值: 30%`);
    return 30;
  }

  /**
   * 🎯 设置液体或进度条的静态值（无动画）
   * 用于当动画设置为"none"时，直接设置最终的液体高度或进度条位置
   * @param svgElement SVG元素
   * @param targetValue 目标值（0-100）
   */
  private setStaticLevelValue(svgElement: SVGSVGElement, targetValue: number, retryCount: number = 0): void {
    // 限制目标值范围
    targetValue = Math.max(0, Math.min(100, targetValue));

    console.log(`🎯 设置静态液位/进度值: ${targetValue}%, 重试次数: ${retryCount}`);

    const svgInstance = (svgElement as any).__svgInstance;

    if (!svgInstance) {
      // 如果SVG实例还未初始化，等待一下再重试（最多重试3次）
      if (retryCount < 3) {
        console.warn(`⚠️ SVG实例数据未初始化，${50}ms后重试 (${retryCount + 1}/3)`);
        setTimeout(() => {
          this.setStaticLevelValue(svgElement, targetValue, retryCount + 1);
        }, 50);
      } else {
        console.error('❌ SVG实例数据未初始化，已达最大重试次数，无法设置静态值');
      }
      return;
    }

    // 🔑 统一使用putValue函数，这是修改SVG内部变量的唯一正确方式
    if (typeof svgInstance.putValue === 'function') {
      console.log('✅ 使用putValue函数设置静态值:', targetValue);
      svgInstance.putValue('_pn_value', targetValue);
    } else {
      console.warn('⚠️ 未找到putValue函数');
    }
  }

  /**
   * 添加悬停效果
   */
  private addHoverEffect(container: HTMLElement): void {
    container.style.cursor = "pointer";
    container.style.transition = "all 0.2s ease";

    container.addEventListener("mouseenter", () => {
      container.style.transform = "scale(1.05)";
      container.style.filter = "brightness(1.1) contrast(1.1)";
    });

    container.addEventListener("mouseleave", () => {
      container.style.transform = "scale(1)";
      container.style.filter = "none";
    });
  }

  /**
   * 确保动画样式存在
   */
  private ensureAnimationStyles(): void {
    // 检查是否已经存在动画样式
    const existingStyle = document.getElementById("svg-animations");
    if (existingStyle) return;

    // 检查是否有重复的样式标签
    const existingAnimationStyles = document.querySelectorAll('style[id*="svg"]');
    if (existingAnimationStyles.length > 0) {
      // 如果有其他SVG相关样式，先清理
      existingAnimationStyles.forEach(style => {
        if (style.id !== "svg-animations" && style.textContent?.includes('@keyframes svg')) {
          style.remove();
        }
      });
    }

    const style = document.createElement("style");
    style.id = "svg-animations";
    style.textContent = `
      @keyframes svg-animation-rotate {
        from { transform: rotate(0deg); }
        to { transform: rotate(360deg); }
      }

      @keyframes svg-animation-pulse {
        0%, 100% { transform: scale(1); opacity: 1; }
        50% { transform: scale(1.1); opacity: 0.8; }
      }

      @keyframes svg-animation-blink {
        0%, 50%, 100% { opacity: 1; }
        25%, 75% { opacity: 0; }
      }

      @keyframes svg-animation-bounce {
        0%, 20%, 53%, 80%, 100% { transform: translateY(0); }
        40%, 43% { transform: translateY(-15px); }
        70% { transform: translateY(-7px); }
        90% { transform: translateY(-3px); }
      }

      @keyframes svg-animation-shake {
        0%, 100% { transform: translateX(0); }
        10%, 30%, 50%, 70%, 90% { transform: translateX(-3px); }
        20%, 40%, 60%, 80% { transform: translateX(3px); }
      }

      @keyframes svg-animation-scale {
        0%, 100% { transform: scale(1); }
        50% { transform: scale(1.2); }
      }

      @keyframes svg-animation-moveX {
        0%, 100% { transform: translateX(0); }
        50% { transform: translateX(10px); }
      }

      @keyframes svg-animation-moveY {
        0%, 100% { transform: translateY(0); }
        50% { transform: translateY(-10px); }
      }

      @keyframes svg-animation-fade {
        0%, 100% { opacity: 1; }
        50% { opacity: 0.3; }
      }
    `;
    document.head.appendChild(style);
  }

  /**
   * 创建错误SVG
   */
  private createErrorSvg(
    componentName: string,
    options: SvgRenderOptions
  ): HTMLElement {
    const container = document.createElement("div");
    container.className = `svg-container error ${options.className || ""}`;
    container.style.cssText = `
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #f5f5f5;
      border: 1px dashed #d9d9d9;
      border-radius: 4px;
      color: #999;
      font-size: 12px;
    `;
    container.textContent = `组件未找到: ${componentName}`;
    return container;
  }

  /**
   * 获取默认SVG
   */
  private getDefaultSvg(): string {
    return `
      <svg width="100" height="100" viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
        <rect x="10" y="10" width="80" height="80" fill="none" stroke="#d9d9d9" stroke-width="2" stroke-dasharray="5,5"/>
        <text x="50" y="55" text-anchor="middle" font-size="12" fill="#999">SVG</text>
      </svg>
    `;
  }

  /**
   * 更新组件样式
   */
  updateComponentStyle(
    container: HTMLElement,
    options: Partial<SvgRenderOptions>,
    componentName?: string
  ): void {
    console.log('🔄 updateComponentStyle调用:', {
      componentName,
      animation: options.animation,
      animationDuration: options.animationDuration,
      animationIterationCount: options.animationIterationCount,
      allOptions: options
    });

    const svgElement = container.querySelector("svg") as SVGSVGElement;
    if (svgElement) {
      // 🛑 先清除所有旧动画（使用统一的清除方法）
      this.clearAllAnimations(svgElement);

      // 清除旧的悬停样式
      container.style.cursor = "";
      container.style.transition = "";
      container.style.transform = "";
      container.style.filter = "";

      // 🎨 保存用户手动设置的渐变和样式（克隆前）
      const userGradients: { id: string; element: Element }[] = [];
      const userStyles = new Map<string, { fill: string; fillOpacity: string; stroke: string; strokeOpacity: string }>();

      const defs = svgElement.querySelector('defs');
      if (defs) {
        // 保存所有用户创建的渐变（ID以gradient_开头的）
        const gradients = defs.querySelectorAll('[id^="gradient_"]');
        gradients.forEach(grad => {
          userGradients.push({
            id: grad.id,
            element: grad.cloneNode(true) as Element
          });
        });
      }

      // 保存所有元素的手动样式(包括没有style属性但有内联样式的元素)
      const allElements = svgElement.querySelectorAll('*');
      allElements.forEach((el, index) => {
        const svgEl = el as SVGElement;
        const fill = svgEl.style.fill;
        const fillOpacity = svgEl.style.fillOpacity;
        const stroke = svgEl.style.stroke;
        const strokeOpacity = svgEl.style.strokeOpacity;

        // 只保存非空的样式
        if (fill || fillOpacity || stroke || strokeOpacity) {
          // 优先使用ID,如果没有ID则使用标签名+索引
          const elementId = el.id || `${el.tagName.toLowerCase()}_${index}`;
          userStyles.set(elementId, { fill, fillOpacity, stroke, strokeOpacity });
          console.log(`🎨 保存元素样式: ${elementId}`, { fill, fillOpacity, stroke, strokeOpacity });
        }
      });

      console.log('🎨 克隆前保存用户样式:', {
        gradients: userGradients.length,
        styledElements: userStyles.size
      });

      // 移除旧的事件监听器（通过克隆节点的方式）
      const newContainer = container.cloneNode(true) as HTMLElement;
      container.parentNode?.replaceChild(newContainer, container);

      // 重新获取SVG元素引用
      const newSvgElement = newContainer.querySelector("svg") as SVGSVGElement;
      if (newSvgElement) {
        // 🎨 恢复用户手动设置的渐变（克隆后）
        if (userGradients.length > 0) {
          let newDefs = newSvgElement.querySelector('defs');
          if (!newDefs) {
            newDefs = document.createElementNS('http://www.w3.org/2000/svg', 'defs');
            newSvgElement.insertBefore(newDefs, newSvgElement.firstChild);
          }

          // 清除旧的用户渐变（避免重复）
          const oldUserGradients = newDefs.querySelectorAll('[id^="gradient_"]');
          oldUserGradients.forEach(grad => grad.remove());

          // 添加保存的渐变
          userGradients.forEach(({ element }) => {
            newDefs!.appendChild(element.cloneNode(true));
          });

          console.log('🎨 克隆后恢复渐变:', userGradients.length);
        }

        // 🎨 恢复元素的手动样式
        if (userStyles.size > 0) {
          userStyles.forEach((styles, elementId) => {
            let targetElement: SVGElement | null = null;

            // 优先通过ID查找元素
            if (!elementId.includes('_')) {
              // 如果elementId不包含下划线,说明是真实的ID
              targetElement = newSvgElement.querySelector(`#${elementId}`) as SVGElement;
            } else {
              // 如果包含下划线,说明是 tagName_index 格式
              const [tagName, indexStr] = elementId.split('_');
              const index = parseInt(indexStr);
              const allElements = newSvgElement.querySelectorAll(tagName);
              if (index < allElements.length) {
                targetElement = allElements[index] as SVGElement;
              }
            }

            if (targetElement) {
              if (styles.fill) {
                targetElement.style.fill = styles.fill;
                console.log(`🎨 恢复元素 ${elementId} 的 fill:`, styles.fill);
              }
              if (styles.fillOpacity) {
                targetElement.style.fillOpacity = styles.fillOpacity;
              }
              if (styles.stroke) {
                targetElement.style.stroke = styles.stroke;
              }
              if (styles.strokeOpacity) {
                targetElement.style.strokeOpacity = styles.strokeOpacity;
              }
            } else {
              console.warn(`⚠️ 无法找到元素 ${elementId} 来恢复样式`);
            }
          });

          console.log('🎨 克隆后恢复样式:', userStyles.size);
        }
        // 🛑 克隆后，确保清除克隆节点上可能残留的动画标记
        // 因为克隆会复制对象的属性，但这些动画ID已经在旧元素上被取消了
        delete (newSvgElement as any)._progressAnimationLoopId;
        delete (newSvgElement as any)._progressAnimationTimeoutId;
        delete (newSvgElement as any)._liquidAnimationLoopId;
        delete (newSvgElement as any)._liquidAnimationTimeoutId;

        // 🔑 克隆后，清除script标签的data-executed属性，以便重新执行
        // 这很重要，因为克隆会复制该属性，导致executeInlineSvgScripts跳过执行
        const clonedScripts = newSvgElement.querySelectorAll('script[data-executed]');
        clonedScripts.forEach(script => {
          script.removeAttribute('data-executed');
          console.log('🔄 清除克隆script的data-executed属性，准备重新执行');
        });

        // 应用新的SVG选项
        this.applySvgOptions(newSvgElement, options as SvgRenderOptions, componentName);

        // 🔑 重要：先执行SVG内嵌脚本（如果有）
        // 必须在addAnimation之前执行，因为液位/进度组件需要__svgInstance才能工作
        // 克隆的script标签不会自动执行，需要手动触发
        this.executeInlineSvgScripts(newSvgElement);

        console.log('🔍 执行脚本后检查__svgInstance:', {
          hasSvgInstance: !!(newSvgElement as any).__svgInstance,
          hasPutValue: !!(newSvgElement as any).__svgInstance?.putValue
        });

        // 重新应用动画效果
        if (options.animation) {
          console.log('✅ 启动新动画:', options.animation, '目标值:', options.animationStaticValue);
          this.addAnimation(newSvgElement, options.animation, options.animationSpeed, options as SvgRenderOptions);

          // 🔘 特别处理：如果是开关切换动画，立即应用当前状态
          if (options.animation === 'switchToggle' && (options as any).switchState) {
            console.log('🔘 updateComponentStyle: 应用开关状态', (options as any).switchState);
            const onColor = (options as any).switchOnColor || '#67c23a';
            const offColor = (options as any).switchOffColor || '#909399';
            this.updateSwitchState(newSvgElement, (options as any).switchState as 'on' | 'off', onColor, offColor);
          }
        } else {
          console.log('⛔ 未设置动画类型');
        }

        // 重新应用悬停效果
        if (options.hoverEffect) {
          this.addHoverEffect(newContainer);
        }
      }
    }
  }

  /**
   * 通过名称查找组件
   */
  findComponentByName(name: string): ComponentIconMapping | undefined {
    return findComponentByName(name);
  }

  /**
   * 清理缓存
   */
  clearCache(): void {
    svgCache.clear();
  }

  /**
   * 专门清理具有特定ID模式的异常SVG元素（如___mt__edit__icons__dom__）
   */
  cleanupAbnormalSvgElements(): void {
    console.log('开始清理异常SVG元素...');

    let removedCount = 0;

    // 查找具有异常ID模式的SVG元素
    const abnormalSvgs = document.querySelectorAll('svg[id*="___mt__"], svg[id*="__edit__"], svg[id*="__dom__"], svg[id*="__icons__"]');

    abnormalSvgs.forEach(svg => {
      // 检查是否是异常的SVG元素（通常在body底部）
      const isAbnormal = (
        svg.id.includes('___mt__') ||
        svg.id.includes('__edit__') ||
        svg.id.includes('__dom__') ||
        svg.id.includes('__icons__')
      );

      if (isAbnormal) {
        console.log('移除异常SVG元素:', svg.id, svg);
        svg.remove();
        removedCount++;
      }
    });

    // 同时清理任何位置为absolute且尺寸为0的无效SVG
    const zeroSizeSvgs = document.querySelectorAll('svg');
    zeroSizeSvgs.forEach(svg => {
      const style = window.getComputedStyle(svg);
      const isZeroSizeSvg = (
        style.position === 'absolute' &&
        (style.width === '0px' || style.width === '0') &&
        (style.height === '0px' || style.height === '0') &&
        svg.parentElement === document.body && // 只清理直接添加到body的
        !svg.closest('.canvas-content') && // 不在画布内的
        !svg.closest('.svg-container') // 不在组件容器内的
      );

      if (isZeroSizeSvg) {
        console.log('移除零尺寸异常SVG元素:', svg.id || '(无ID)', svg);
        svg.remove();
        removedCount++;
      }
    });

    console.log(`异常SVG清理完成，共移除 ${removedCount} 个异常元素`);
  }

  /**
   * 安全清理页面中的隐藏SVG元素 - 仅清理明确的临时元素
   */
  cleanupHiddenSvgElements(): void {
    console.log('开始安全清理隐藏的SVG元素...');

    // 重要：只在canvas-content容器内进行清理，避免影响Vue组件
    const canvasContent = document.querySelector('.canvas-content');
    if (!canvasContent) {
      console.log('未找到画布内容区域，跳过清理');
      return;
    }

    let removedCount = 0;

    // 只清理canvas-content内部的隐藏SVG元素
    const hiddenSvgs = canvasContent.querySelectorAll('svg');
    hiddenSvgs.forEach(svg => {
      const style = window.getComputedStyle(svg);

      // 只清理明确标识为临时的SVG元素
      const isTempSvg = (
        style.position === 'absolute' &&
        style.width === '0px' &&
        style.height === '0px' &&
        style.overflow === 'hidden' &&
        !svg.id &&
        !svg.className &&
        !svg.hasAttribute('data-important') &&
        !svg.querySelector('defs') &&
        svg.parentElement &&
        !svg.parentElement.className
      );

      if (isTempSvg) {
        console.log('移除临时SVG元素:', svg);
        svg.remove();
        removedCount++;
      }
    });

    // 只清理明确的重复动画样式，且必须在document head中
    const headStyles = document.head.querySelectorAll('style');
    headStyles.forEach(style => {
      const isRedundantSvgStyle = (
        style.textContent?.includes('@keyframes svg') &&
        style.id !== 'svg-animations' &&
        !style.id &&
        style.textContent?.includes('transform: rotate') &&
        document.getElementById('svg-animations') // 确保主样式存在
      );

      if (isRedundantSvgStyle) {
        console.log('移除重复的SVG动画样式:', style);
        style.remove();
        removedCount++;
      }
    });

    console.log(`安全SVG清理完成，共移除 ${removedCount} 个临时元素`);
  }
}

/**
 * 导出单例实例
 */
export const svgManager = SvgManager.getInstance();

/**
 * 便捷函数：创建内联SVG组件
 */
export function createSvgComponent(
  componentName: string,
  options: SvgRenderOptions = {}
): HTMLElement {
  return svgManager.createInlineSvg(componentName, options);
}

/**
 * 便捷函数：异步加载SVG内容
 */
export async function loadSvgContent(iconPath: string): Promise<string> {
  return svgManager.loadSvgContent(iconPath);
}

/**
 * 便捷函数：清理异常的SVG元素
 */
export function cleanupAbnormalSvgElements(): void {
  return svgManager.cleanupAbnormalSvgElements();
}

/**
 * 便捷函数：清理隐藏的SVG元素
 */
export function cleanupHiddenSvgElements(): void {
  return svgManager.cleanupHiddenSvgElements();
}
