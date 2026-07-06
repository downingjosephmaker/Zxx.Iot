/**
 * SVG类型检测器 - 用于区分不同类型的SVG组件
 * 帮助渲染系统选择合适的处理策略
 */

export interface SvgTypeInfo {
  type: 'simple' | 'complex' | 'interactive' | 'transformed' | 'inkscape';
  hasScript: boolean;
  hasAnimation: boolean;
  hasGradients: boolean;
  hasTransforms: boolean;
  hasInkscapeMetadata: boolean;
  hasCustomStyles: boolean;
  viewBoxInfo: {
    hasViewBox: boolean;
    viewBoxValue?: string;
    isStandardSize: boolean; // 是否是标准的24x24或类似尺寸
  };
  colorInfo: {
    hasMultipleColors: boolean;
    hasFixedColors: boolean;
    shouldPreserveColors: boolean;
  };
  recommendations: {
    shouldApplyThemeColor: boolean;
    shouldPreserveOriginalStyles: boolean;
    shouldForceViewBox: boolean;
    shouldCleanAttributes: boolean;
  };
}

export class SvgTypeDetector {
  /**
   * 检测SVG类型和特征
   */
  static detectSvgType(svgContent: string, componentName?: string): SvgTypeInfo {
    // 创建临时容器解析SVG
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = svgContent;
    const svgElement = tempDiv.querySelector('svg');

    if (!svgElement) {
      return this.createDefaultTypeInfo();
    }

    // 基础特征检测
    const hasScript = this.hasScript(svgContent);
    const hasAnimation = this.hasAnimation(svgElement);
    const hasGradients = this.hasGradients(svgElement);
    const hasTransforms = this.hasTransforms(svgElement);
    const hasInkscapeMetadata = this.hasInkscapeMetadata(svgElement);
    const hasCustomStyles = this.hasCustomStyles(svgElement);

    // ViewBox分析
    const viewBoxInfo = this.analyzeViewBox(svgElement);
    
    // 颜色分析
    const colorInfo = this.analyzeColors(svgElement, svgContent);

    // 确定SVG类型
    const type = this.determineSvgType({
      hasScript,
      hasAnimation,
      hasGradients,
      hasTransforms,
      hasInkscapeMetadata,
      hasCustomStyles,
      componentName
    });

    // 生成建议
    const recommendations = this.generateRecommendations({
      type,
      hasScript,
      hasCustomStyles,
      colorInfo,
      viewBoxInfo,
      componentName
    });

    // 清理临时容器
    tempDiv.innerHTML = '';

    return {
      type,
      hasScript,
      hasAnimation,
      hasGradients,
      hasTransforms,
      hasInkscapeMetadata,
      hasCustomStyles,
      viewBoxInfo,
      colorInfo,
      recommendations
    };
  }

  /**
   * 检测是否包含JavaScript脚本
   */
  private static hasScript(svgContent: string): boolean {
    return svgContent.includes('<script') || 
           svgContent.includes('javascript:') ||
           svgContent.includes('function ') ||
           svgContent.includes('//!export-');
  }

  /**
   * 检测是否包含动画元素
   */
  private static hasAnimation(svgElement: SVGSVGElement): boolean {
    // 检查动画元素
    const hasAnimateElements = !!svgElement.querySelector('animate, animateMotion, animateTransform, animateColor');
    const hasAttributeName = !!svgElement.querySelector('[attributeName]');
    const hasHrefLinks = !!svgElement.querySelector('[href^="#"]');
    
    // 通过字符串检查xlink:href（避免CSS选择器问题）
    const hasXlinkHref = svgElement.outerHTML.includes('xlink:href="#');
    
    return hasAnimateElements || hasAttributeName || hasHrefLinks || hasXlinkHref;
  }

  /**
   * 检测是否包含渐变
   */
  private static hasGradients(svgElement: SVGSVGElement): boolean {
    return !!(
      svgElement.querySelector('linearGradient, radialGradient') ||
      svgElement.querySelector('[fill^="url(#"]') ||
      svgElement.querySelector('[stroke^="url(#"]')
    );
  }

  /**
   * 检测是否包含变换
   */
  private static hasTransforms(svgElement: SVGSVGElement): boolean {
    return !!(
      svgElement.querySelector('[transform]') ||
      svgElement.querySelector('[transform*="matrix"]') ||
      svgElement.querySelector('[transform*="translate"]') ||
      svgElement.querySelector('[transform*="rotate"]') ||
      svgElement.querySelector('[transform*="scale"]')
    );
  }

  /**
   * 检测是否包含Inkscape元数据
   */
  private static hasInkscapeMetadata(svgElement: SVGSVGElement): boolean {
    // 检查命名空间属性
    const hasInkscapeNamespace = !!svgElement.getAttribute('xmlns:inkscape');
    const hasSodipodiNamespace = !!svgElement.getAttribute('xmlns:sodipodi');
    
    // 检查是否包含Inkscape相关元素（通过innerHTML检查，避免CSS选择器问题）
    const svgContent = svgElement.outerHTML;
    const hasInkscapeElements = svgContent.includes('inkscape:') || 
                               svgContent.includes('sodipodi:') ||
                               svgContent.includes('sodipodi\\:namedview') ||
                               svgContent.includes('inkscape\\:');
    
    return hasInkscapeNamespace || hasSodipodiNamespace || hasInkscapeElements;
  }

  /**
   * 检测是否包含自定义样式
   */
  private static hasCustomStyles(svgElement: SVGSVGElement): boolean {
    // 检查内联样式
    const hasInlineStyles = !!(
      svgElement.querySelector('[style*="fill:#"]') ||
      svgElement.querySelector('[style*="stroke:#"]') ||
      svgElement.querySelector('[style*="fill:rgb"]') ||
      svgElement.querySelector('[style*="fill:hsl"]')
    );

    // 检查CSS样式表
    const hasStyleSheet = !!svgElement.querySelector('style');

    // 检查具体的颜色属性
    const hasColorAttrs = !!(
      svgElement.querySelector('[fill]:not([fill="none"]):not([fill="currentColor"])') ||
      svgElement.querySelector('[stroke]:not([stroke="none"]):not([stroke="currentColor"])')
    );

    return hasInlineStyles || hasStyleSheet || hasColorAttrs;
  }

  /**
   * 分析ViewBox信息
   */
  private static analyzeViewBox(svgElement: SVGSVGElement): SvgTypeInfo['viewBoxInfo'] {
    const viewBox = svgElement.getAttribute('viewBox');
    const hasViewBox = !!viewBox;
    
    let isStandardSize = false;
    if (viewBox) {
      // 检查是否是标准尺寸（24x24, 100x100, 1024x1024等）
      const standardSizes = ['0 0 24 24', '0 0 100 100', '0 0 1024 1024', '0 0 48 48', '0 0 32 32', '0 0 16 16'];
      isStandardSize = standardSizes.includes(viewBox);
    }

    return {
      hasViewBox,
      viewBoxValue: viewBox || undefined,
      isStandardSize
    };
  }

  /**
   * 分析颜色信息
   */
  private static analyzeColors(svgElement: SVGSVGElement, svgContent: string): SvgTypeInfo['colorInfo'] {
    // 检测是否包含多种颜色
    const colorRegex = /#[0-9a-fA-F]{3,6}|rgb\([^)]+\)|hsl\([^)]+\)|rgba\([^)]+\)|hsla\([^)]+\)/g;
    const colors = svgContent.match(colorRegex) || [];
    const uniqueColors = [...new Set(colors)];
    const hasMultipleColors = uniqueColors.length > 1;

    // 检测是否有固定颜色（非通用颜色）
    const hasFixedColors = !!(
      svgElement.querySelector('[fill^="#"]') ||
      svgElement.querySelector('[stroke^="#"]') ||
      svgElement.querySelector('[style*="fill:#"]') ||
      svgElement.querySelector('[style*="stroke:#"]')
    );

    // 判断是否应该保持原有颜色
    const shouldPreserveColors = hasMultipleColors || hasFixedColors;

    return {
      hasMultipleColors,
      hasFixedColors,
      shouldPreserveColors
    };
  }

  /**
   * 确定SVG类型
   */
  private static determineSvgType(features: {
    hasScript: boolean;
    hasAnimation: boolean;
    hasGradients: boolean;
    hasTransforms: boolean;
    hasInkscapeMetadata: boolean;
    hasCustomStyles: boolean;
    componentName?: string;
  }): SvgTypeInfo['type'] {
    // 交互式SVG（包含脚本）
    if (features.hasScript) {
      return 'interactive';
    }

    // Inkscape生成的复杂SVG
    if (features.hasInkscapeMetadata) {
      return 'inkscape';
    }

    // 包含变换矩阵的SVG
    if (features.hasTransforms) {
      return 'transformed';
    }

    // 复杂SVG（包含动画、渐变或自定义样式）
    if (features.hasAnimation || features.hasGradients || features.hasCustomStyles) {
      return 'complex';
    }

    // 简单图标SVG
    return 'simple';
  }

  /**
   * 生成处理建议
   */
  private static generateRecommendations(params: {
    type: SvgTypeInfo['type'];
    hasScript: boolean;
    hasCustomStyles: boolean;
    colorInfo: SvgTypeInfo['colorInfo'];
    viewBoxInfo: SvgTypeInfo['viewBoxInfo'];
    componentName?: string;
  }): SvgTypeInfo['recommendations'] {
    const { type, hasScript, hasCustomStyles, colorInfo, viewBoxInfo, componentName } = params;

    // 默认建议
    let shouldApplyThemeColor = true;
    let shouldPreserveOriginalStyles = false;
    let shouldForceViewBox = false;
    let shouldCleanAttributes = true;

    // 根据SVG类型调整建议
    switch (type) {
      case 'interactive':
        // 交互式SVG：完全保持原样
        shouldApplyThemeColor = false;
        shouldPreserveOriginalStyles = true;
        shouldCleanAttributes = false;
        break;

      case 'complex':
        // 复杂SVG：保持样式，但可能需要调整viewBox
        shouldApplyThemeColor = !colorInfo.shouldPreserveColors;
        shouldPreserveOriginalStyles = colorInfo.shouldPreserveColors;
        shouldForceViewBox = !viewBoxInfo.hasViewBox;
        break;

      case 'transformed':
        // 变换SVG：可能需要调整viewBox
        shouldApplyThemeColor = false;
        shouldPreserveOriginalStyles = true;
        shouldForceViewBox = !viewBoxInfo.hasViewBox || !viewBoxInfo.isStandardSize;
        break;

      case 'inkscape':
        // Inkscape SVG：清理元数据，但保持主要样式
        shouldApplyThemeColor = !hasCustomStyles;
        shouldPreserveOriginalStyles = hasCustomStyles;
        shouldCleanAttributes = true;
        shouldForceViewBox = !viewBoxInfo.hasViewBox;
        break;

      case 'simple':
      default:
        // 简单SVG：应用主题色
        shouldApplyThemeColor = true;
        shouldPreserveOriginalStyles = false;
        shouldForceViewBox = !viewBoxInfo.hasViewBox;
        break;
    }

    // 特殊组件类型的处理
    if (componentName) {
      // shape-开头的组件通常需要保持原有样式
      if (componentName.startsWith('shape-')) {
        shouldPreserveOriginalStyles = true;
        shouldApplyThemeColor = false;
      }

      // 小部件组件通常有复杂的颜色
      if (componentName.includes('valve') || componentName.includes('pump') || componentName.includes('tank')) {
        shouldPreserveOriginalStyles = true;
        shouldApplyThemeColor = false;
      }
    }

    return {
      shouldApplyThemeColor,
      shouldPreserveOriginalStyles,
      shouldForceViewBox,
      shouldCleanAttributes
    };
  }

  /**
   * 创建默认类型信息
   */
  private static createDefaultTypeInfo(): SvgTypeInfo {
    return {
      type: 'simple',
      hasScript: false,
      hasAnimation: false,
      hasGradients: false,
      hasTransforms: false,
      hasInkscapeMetadata: false,
      hasCustomStyles: false,
      viewBoxInfo: {
        hasViewBox: false,
        isStandardSize: false
      },
      colorInfo: {
        hasMultipleColors: false,
        hasFixedColors: false,
        shouldPreserveColors: false
      },
      recommendations: {
        shouldApplyThemeColor: true,
        shouldPreserveOriginalStyles: false,
        shouldForceViewBox: true,
        shouldCleanAttributes: true
      }
    };
  }

  /**
   * 便捷方法：检查是否应该应用主题色
   */
  static shouldApplyThemeColor(svgContent: string, componentName?: string): boolean {
    const typeInfo = this.detectSvgType(svgContent, componentName);
    return typeInfo.recommendations.shouldApplyThemeColor;
  }

  /**
   * 便捷方法：检查是否应该保持原有样式
   */
  static shouldPreserveOriginalStyles(svgContent: string, componentName?: string): boolean {
    const typeInfo = this.detectSvgType(svgContent, componentName);
    return typeInfo.recommendations.shouldPreserveOriginalStyles;
  }

  /**
   * 便捷方法：检查是否需要修复viewBox
   */
  static shouldForceViewBox(svgContent: string, componentName?: string): boolean {
    const typeInfo = this.detectSvgType(svgContent, componentName);
    return typeInfo.recommendations.shouldForceViewBox;
  }
}