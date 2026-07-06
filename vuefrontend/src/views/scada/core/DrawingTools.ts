import {
  shapeComponents,
  regularComponents,
  type ComponentIconMapping
} from "./fuxa-icon-mapping";

/**
 * 绘图组件接口 - 只包含绘图工具所需的基本属性
 */
export interface DrawingComponent {
  id: string;
  type: string;
  x: number;
  y: number;
  width: number;
  height: number;
  properties?: Record<string, any> | string;
}

/**
 * 绘图工具类型定义 - 简化后只包含绘图交互配置
 */
export interface DrawingTool {
  name: string;
  title: string;
  icon: string;
  cursor: string;
  category: "shapes" | "lines" | "text" | "connectors";
  defaultProps: Partial<DrawingComponent>;
  drawingOptions?: {
    strokeWidth: number;
    strokeColor: string;
    fillColor: string;
    lineDash: number[];
  };
}

/**
 * 默认绘图选项
 */
const DEFAULT_DRAWING_OPTIONS = {
  strokeWidth: 2,
  strokeColor: "#409eff",
  fillColor: "transparent",
  lineDash: []
};

/**
 * 从fuxa-icon-mapping.ts中获取可绘制的图形工具
 * 直接使用regularComponents和shapeComponents，通过图标路径是否为SVG文件来判断
 */
const getDrawableShapes = (): ComponentIconMapping[] => {
  // 从常规组件中筛选适合绘图的基础工具（排除复杂的UI控件）
  const drawableRegular = regularComponents.filter(comp => {
    // 只包含基础绘图工具，排除复杂的UI组件
    const basicDrawingTools = [
      "select",
      "text",
      "line",
      "path",
      "image",
      "rect",
      "circle",
      "ellipse"
    ];
    return basicDrawingTools.includes(comp.name);
  });

  // 从形状组件中筛选出基础图形（排除复杂的流程图符号）
  const drawableShapes = shapeComponents.filter(comp => {
    // 只包含基础几何形状
    const basicShapes = [
      "shape-rectangle",
      "shape-circle",
      "shape-triangle",
      "shape-diamond",
      "shape-pentagon",
      "shape-octagon",
      "shape-heart",
      "shape-drop",
      "shape-cross",
      "shape-arrow",
      "shape-doublearrow"
    ];
    return basicShapes.includes(comp.name);
  });

  return [...drawableRegular, ...drawableShapes];
};

/**
 * 将ComponentIconMapping转换为DrawingTool
 */
const mapToDrawingTool = (comp: ComponentIconMapping): DrawingTool => {
  // 根据组件名称确定类别和默认属性
  const getToolConfig = (name: string) => {
    switch (name) {
      case "line":
        return {
          category: "lines" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "line",
            width: 100,
            height: 2,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              startX: 0,
              startY: 0,
              endX: 100,
              endY: 0
            }
          }
        };

      case "text":
        return {
          category: "text" as const,
          cursor: "text",
          defaultProps: {
            type: "text",
            width: 120,
            height: 40,
            properties: {
              text: "文本框",
              fontSize: 14,
              fontFamily: "Arial",
              fontWeight: "normal",
              color: "#303133",
              textAlign: "left",
              backgroundColor: "transparent",
              borderColor: "#409eff",
              borderWidth: 2,
              padding: 8
            }
          }
        };

      case "rect":
      case "rectangle":
        return {
          category: "shapes" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "rectangle",
            width: 100,
            height: 80,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              fillColor: "transparent",
              cornerRadius: 0
            }
          }
        };

      case "circle":
        return {
          category: "shapes" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "circle",
            width: 80,
            height: 80,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              fillColor: "transparent"
            }
          }
        };

      case "ellipse":
        return {
          category: "shapes" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "ellipse",
            width: 120,
            height: 80,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              fillColor: "transparent"
            }
          }
        };

      case "diamond":
        return {
          category: "shapes" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "diamond",
            width: 80,
            height: 80,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              fillColor: "transparent"
            }
          }
        };

      case "shape-rectangle":
        return {
          category: "shapes" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "shape-rectangle",
            width: 100,
            height: 80,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              fillColor: "transparent"
            }
          }
        };

      case "shape-circle":
        return {
          category: "shapes" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "shape-circle",
            width: 80,
            height: 80,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              fillColor: "transparent"
            }
          }
        };

      case "shape-triangle":
        return {
          category: "shapes" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "shape-triangle",
            width: 80,
            height: 80,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              fillColor: "transparent"
            }
          }
        };

      case "shape-diamond":
        return {
          category: "shapes" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "shape-diamond",
            width: 80,
            height: 80,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              fillColor: "transparent"
            }
          }
        };

      case "shape-pentagon":
        return {
          category: "shapes" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "pentagon",
            width: 80,
            height: 80,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              fillColor: "transparent",
              sides: 5
            }
          }
        };

      case "shape-octagon":
        return {
          category: "shapes" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "octagon",
            width: 80,
            height: 80,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              fillColor: "transparent",
              sides: 8
            }
          }
        };

      case "shape-heart":
        return {
          category: "shapes" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "heart",
            width: 80,
            height: 80,
            properties: {
              strokeWidth: 2,
              strokeColor: "#f56c6c",
              fillColor: "transparent"
            }
          }
        };

      case "shape-doublearrow":
        return {
          category: "lines" as const,
          cursor: "crosshair",
          defaultProps: {
            type: "doubleArrow",
            width: 100,
            height: 20,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              fillColor: "#409eff",
              arrowSize: 8
            }
          }
        };

      default:
        return {
          category: "shapes" as const,
          cursor: "crosshair",
          defaultProps: {
            type: name,
            width: 80,
            height: 80,
            properties: {
              strokeWidth: 2,
              strokeColor: "#409eff",
              fillColor: "transparent"
            }
          }
        };
    }
  };

  const config = getToolConfig(comp.name);

  return {
    name: comp.name,
    title: comp.title,
    icon: comp.iconPath, // 直接使用SVG路径
    cursor: config.cursor,
    category: config.category,
    defaultProps: config.defaultProps,
    drawingOptions: DEFAULT_DRAWING_OPTIONS
  };
};

/**
 * 绘图工具注册表 - 从fuxa-icon-mapping.ts动态生成
 */
export const drawingTools: DrawingTool[] =
  getDrawableShapes().map(mapToDrawingTool);

/**
 * 绘图管理器
 */
export class DrawingToolManager {
  /**
   * 初始化绘图管理器
   */
  initialize(container: HTMLElement | HTMLCanvasElement) {
    // 初始化逻辑已简化，不再使用 Canvas 绘图
    console.log('DrawingToolManager 已初始化');
  }

  /**
   * 设置绘图工具（保留接口兼容性）
   */
  setCurrentTool(toolName: string) {
    // 不再使用 Canvas 绘图模式
  }

  /**
   * 启用绘图模式（保留接口兼容性）
   */
  enableDrawingMode() {
    // 不再使用 Canvas 绘图模式
  }

  /**
   * 禁用绘图模式（保留接口兼容性）
   */
  disableDrawingMode() {
    // 不再使用 Canvas 绘图模式
  }

  /**
   * 检查是否为绘图工具
   */
  isDrawingTool(componentType: string): boolean {
    // 只检查真正的绘图工具类型 - text是唯一需要特殊处理的
    if (componentType === "text") {
      return true;
    }

    return drawingTools.some(tool => {
      const nameMatch = tool.name === componentType;
      const typeMatch = tool.defaultProps.type === componentType;
      return nameMatch || typeMatch;
    });
  }


  /**
   * 创建绘图组件
   */
  createDrawingComponent(
    component: DrawingComponent,
    container: HTMLElement
  ): HTMLElement {
    console.log("createDrawingComponent 调用，组件类型:", component.type);
    console.log("createDrawingComponent 收到的组件数据:", component);
    console.log("createDrawingComponent 容器信息:", {
      width: container.offsetWidth,
      height: container.offsetHeight,
      className: container.className
    });

    // 获取组件属性
    const toolConfig = drawingTools.find(
      tool =>
        tool.name === component.type ||
        tool.defaultProps.type === component.type
    );
    const defaultProps = toolConfig?.defaultProps?.properties || {};

    let properties: Record<string, any> = {};
    if (component.properties) {
      if (typeof component.properties === "string") {
        try {
          properties = JSON.parse(component.properties);
        } catch (e) {
          properties = {};
        }
      } else {
        properties = component.properties;
      }
    }

    // 合并默认属性和组件属性
    const mergedProps = Object.assign({}, defaultProps || {}, properties);

    // 文本组件特殊处理
    if (component.type === "text") {
      console.log(
        "createDrawingComponent 识别为文本组件，调用 createTextElement"
      );
      const textElement = this.createTextElement(component, mergedProps);
      console.log("createDrawingComponent 文本元素创建完成，实际尺寸:", {
        width: textElement.offsetWidth,
        height: textElement.offsetHeight,
        style: textElement.style.cssText
      });
      container.appendChild(textElement);
      return textElement;
    }

    // 创建标准的div容器，这样可以与现有的组件系统集成
    const element = document.createElement("div");
    element.id = component.id;
    element.className = "fuxa-component drawing-component";
    element.style.cssText = `
      position: absolute;
      left: ${component.x || 0}px;
      top: ${component.y || 0}px;
      width: ${component.width || 100}px;
      height: ${component.height || 100}px;
      cursor: pointer;
      user-select: none;
      z-index: 10;
      box-sizing: border-box;
    `;

    // 创建SVG来绘制形状，这样更容易缩放和样式化
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.style.cssText = `
      width: 100%;
      height: 100%;
      pointer-events: none;
    `;
    const width = component.width || 100;
    const height = component.height || 100;
    svg.setAttribute("viewBox", `0 0 ${width} ${height}`);

    const strokeColor = mergedProps.strokeColor || "#409eff";
    const fillColor = mergedProps.fillColor || "transparent";
    const strokeWidth = mergedProps.strokeWidth || 2;

    // 少数需要硬编码SVG的简单几何组件（大多数组件都应该用SvgManager加载SVG文件）
    const hardcodedSvgComponents = [
      "line",
      "polyline",
      "bezierCurve",
      "dashedLine"
    ];

    // 默认使用SvgManager加载真实SVG文件（标准处理方式）
    if (
      !hardcodedSvgComponents.includes(component.type) &&
      component.type !== "text"
    ) {
      // 导入SvgManager来加载SVG文件内容
      import("./SvgManager")
        .then(({ svgManager }) => {
          const svgContainer = svgManager.createInlineSvg(component.type, {
            width: width,
            height: height,
            fillColor: fillColor !== "transparent" ? fillColor : undefined,
            strokeColor: strokeColor,
            strokeWidth: strokeWidth
          });
          // 替换默认的svg
          element.innerHTML = "";
          element.appendChild(svgContainer);
        })
        .catch(() => {
          // 如果加载失败，使用硬编码SVG作为后备
          this.createSVGShape(
            svg,
            component.type,
            width,
            height,
            mergedProps,
            strokeColor,
            fillColor,
            strokeWidth
          );
          element.appendChild(svg);
        });
    }
    // 少数简单几何组件使用硬编码SVG
    else if (hardcodedSvgComponents.includes(component.type)) {
      this.createSVGShape(
        svg,
        component.type,
        width,
        height,
        mergedProps,
        strokeColor,
        fillColor,
        strokeWidth
      );
      element.appendChild(svg);
    }

    container.appendChild(element);
    return element;
  }

  /**
   * 创建SVG形状
   */
  private createSVGShape(
    svg: SVGSVGElement,
    type: string,
    width: number,
    height: number,
    properties: any,
    strokeColor: string,
    fillColor: string,
    strokeWidth: number
  ) {
    switch (type) {
      case "rectangle":
        const rect = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "rect"
        );
        rect.setAttribute("x", "1");
        rect.setAttribute("y", "1");
        rect.setAttribute("width", (width - 2).toString());
        rect.setAttribute("height", (height - 2).toString());
        rect.setAttribute("stroke", strokeColor);
        rect.setAttribute("stroke-width", strokeWidth.toString());
        rect.setAttribute("fill", fillColor);
        rect.setAttribute("rx", (properties.cornerRadius || 0).toString());
        svg.appendChild(rect);
        break;

      case "circle":
        const circle = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "circle"
        );
        const radius = Math.min(width, height) / 2 - strokeWidth;
        circle.setAttribute("cx", (width / 2).toString());
        circle.setAttribute("cy", (height / 2).toString());
        circle.setAttribute("r", radius.toString());
        circle.setAttribute("stroke", strokeColor);
        circle.setAttribute("stroke-width", strokeWidth.toString());
        circle.setAttribute("fill", fillColor);
        svg.appendChild(circle);
        break;

      case "ellipse":
        const ellipse = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "ellipse"
        );
        ellipse.setAttribute("cx", (width / 2).toString());
        ellipse.setAttribute("cy", (height / 2).toString());
        ellipse.setAttribute("rx", (width / 2 - strokeWidth).toString());
        ellipse.setAttribute("ry", (height / 2 - strokeWidth).toString());
        ellipse.setAttribute("stroke", strokeColor);
        ellipse.setAttribute("stroke-width", strokeWidth.toString());
        ellipse.setAttribute("fill", fillColor);
        svg.appendChild(ellipse);
        break;

      case "line":
        const line = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "line"
        );
        line.setAttribute("x1", "0");
        line.setAttribute("y1", (height / 2).toString());
        line.setAttribute("x2", width.toString());
        line.setAttribute("y2", (height / 2).toString());
        line.setAttribute("stroke", strokeColor);
        line.setAttribute("stroke-width", strokeWidth.toString());
        svg.appendChild(line);
        break;

      case "polyline":
        if (properties.points && properties.points.length > 1) {
          const polyline = document.createElementNS(
            "http://www.w3.org/2000/svg",
            "polyline"
          );
          const points = properties.points
            .map((p: any) => `${p.x},${p.y}`)
            .join(" ");
          polyline.setAttribute("points", points);
          polyline.setAttribute("stroke", strokeColor);
          polyline.setAttribute("stroke-width", strokeWidth.toString());
          polyline.setAttribute("fill", "none");
          svg.appendChild(polyline);
        }
        break;

      case "text":
        // 文本组件由专门的createTextElement方法处理，这里不进行SVG渲染
        break;

      case "pentagon":
      case "hexagon":
        const polygon = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "polygon"
        );
        const sides = properties.sides || (type === "pentagon" ? 5 : 6);
        const polyCenterX = width / 2;
        const polyCenterY = height / 2;
        const polyRadius = Math.min(width, height) / 2 - strokeWidth;

        let polygonPoints = "";
        for (let i = 0; i < sides; i++) {
          const angle = (i * 2 * Math.PI) / sides - Math.PI / 2;
          const x = polyCenterX + polyRadius * Math.cos(angle);
          const y = polyCenterY + polyRadius * Math.sin(angle);
          polygonPoints += `${x},${y} `;
        }

        polygon.setAttribute("points", polygonPoints.trim());
        polygon.setAttribute("stroke", strokeColor);
        polygon.setAttribute("stroke-width", strokeWidth.toString());
        polygon.setAttribute("fill", fillColor);
        svg.appendChild(polygon);
        break;

      case "star":
        const star = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "polygon"
        );
        const starPoints = properties.points || 5;
        const outerRadius = Math.min(width, height) / 2 - strokeWidth;
        const innerRadius = outerRadius * (properties.innerRadius || 0.5);
        const starCenterX = width / 2;
        const starCenterY = height / 2;

        let starPointsStr = "";
        for (let i = 0; i < starPoints * 2; i++) {
          const angle = (i * Math.PI) / starPoints - Math.PI / 2;
          const radius = i % 2 === 0 ? outerRadius : innerRadius;
          const x = starCenterX + radius * Math.cos(angle);
          const y = starCenterY + radius * Math.sin(angle);
          starPointsStr += `${x},${y} `;
        }

        star.setAttribute("points", starPointsStr.trim());
        star.setAttribute("stroke", strokeColor);
        star.setAttribute("stroke-width", strokeWidth.toString());
        star.setAttribute("fill", fillColor);
        svg.appendChild(star);
        break;

      case "diamond":
        const diamond = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "polygon"
        );
        const diamondPoints = `${width / 2},${strokeWidth} ${width - strokeWidth},${height / 2} ${width / 2},${height - strokeWidth} ${strokeWidth},${height / 2}`;
        diamond.setAttribute("points", diamondPoints);
        diamond.setAttribute("stroke", strokeColor);
        diamond.setAttribute("stroke-width", strokeWidth.toString());
        diamond.setAttribute("fill", fillColor);
        svg.appendChild(diamond);
        break;

      case "roundedRect":
        const roundedRect = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "rect"
        );
        roundedRect.setAttribute("x", "1");
        roundedRect.setAttribute("y", "1");
        roundedRect.setAttribute("width", (width - 2).toString());
        roundedRect.setAttribute("height", (height - 2).toString());
        roundedRect.setAttribute("stroke", strokeColor);
        roundedRect.setAttribute("stroke-width", strokeWidth.toString());
        roundedRect.setAttribute("fill", fillColor);
        roundedRect.setAttribute(
          "rx",
          (properties.cornerRadius || 10).toString()
        );
        roundedRect.setAttribute(
          "ry",
          (properties.cornerRadius || 10).toString()
        );
        svg.appendChild(roundedRect);
        break;

      case "bezierCurve":
        const bezier = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "path"
        );
        const startPoint = properties.startPoint || { x: 0, y: height / 2 };
        const endPoint = properties.endPoint || { x: width, y: height / 2 };
        const cp1 = properties.controlPoint1 || { x: width * 0.33, y: 0 };
        const cp2 = properties.controlPoint2 || { x: width * 0.67, y: height };

        const pathData = `M ${startPoint.x},${startPoint.y} C ${cp1.x},${cp1.y} ${cp2.x},${cp2.y} ${endPoint.x},${endPoint.y}`;
        bezier.setAttribute("d", pathData);
        bezier.setAttribute("stroke", strokeColor);
        bezier.setAttribute("stroke-width", strokeWidth.toString());
        bezier.setAttribute("fill", "none");
        svg.appendChild(bezier);
        break;

      case "dashedLine":
        const dashedLine = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "line"
        );
        dashedLine.setAttribute("x1", "0");
        dashedLine.setAttribute("y1", (height / 2).toString());
        dashedLine.setAttribute("x2", width.toString());
        dashedLine.setAttribute("y2", (height / 2).toString());
        dashedLine.setAttribute("stroke", strokeColor);
        dashedLine.setAttribute("stroke-width", strokeWidth.toString());
        dashedLine.setAttribute(
          "stroke-dasharray",
          (properties.dashPattern || [5, 5]).join(",")
        );
        svg.appendChild(dashedLine);
        break;

      case "doubleArrow":
        // 线条
        const doubleLine = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "line"
        );
        doubleLine.setAttribute("x1", "10");
        doubleLine.setAttribute("y1", (height / 2).toString());
        doubleLine.setAttribute("x2", (width - 10).toString());
        doubleLine.setAttribute("y2", (height / 2).toString());
        doubleLine.setAttribute("stroke", strokeColor);
        doubleLine.setAttribute("stroke-width", strokeWidth.toString());
        svg.appendChild(doubleLine);

        // 左箭头
        const leftArrow = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "polygon"
        );
        const doubleArrowSize = properties.arrowSize || 8;
        const doubleCenterY = height / 2;
        leftArrow.setAttribute(
          "points",
          `0,${doubleCenterY} ${doubleArrowSize},${doubleCenterY - doubleArrowSize / 2} ${doubleArrowSize},${doubleCenterY + doubleArrowSize / 2}`
        );
        leftArrow.setAttribute("fill", fillColor);
        svg.appendChild(leftArrow);

        // 右箭头
        const rightArrow = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "polygon"
        );
        rightArrow.setAttribute(
          "points",
          `${width},${doubleCenterY} ${width - doubleArrowSize},${doubleCenterY - doubleArrowSize / 2} ${width - doubleArrowSize},${doubleCenterY + doubleArrowSize / 2}`
        );
        rightArrow.setAttribute("fill", fillColor);
        svg.appendChild(rightArrow);
        break;

      case "pencil":
        // 这个复杂组件使用外部SVG文件，暂时显示占位符
        const placeholder = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "rect"
        );
        placeholder.setAttribute("x", "1");
        placeholder.setAttribute("y", "1");
        placeholder.setAttribute("width", (width - 2).toString());
        placeholder.setAttribute("height", (height - 2).toString());
        placeholder.setAttribute("stroke", strokeColor);
        placeholder.setAttribute("stroke-width", strokeWidth.toString());
        placeholder.setAttribute("fill", "none");
        placeholder.setAttribute("stroke-dasharray", "3,3");
        svg.appendChild(placeholder);

        // 添加文本标识
        const text = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "text"
        );
        text.setAttribute("x", (width / 2).toString());
        text.setAttribute("y", (height / 2).toString());
        text.setAttribute("text-anchor", "middle");
        text.setAttribute("dominant-baseline", "middle");
        text.setAttribute("font-size", "8");
        text.setAttribute("fill", strokeColor);
        text.textContent = type.toUpperCase();
        svg.appendChild(text);
        break;

      case "speechBubble":
        const bubble = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "path"
        );
        const cornerRadius = properties.cornerRadius || 8;
        const tailPos = properties.tailPosition || "bottom-left";

        let bubblePath = `M ${cornerRadius},0
                         L ${width - cornerRadius},0
                         Q ${width},0 ${width},${cornerRadius}
                         L ${width},${height * 0.7 - cornerRadius}
                         Q ${width},${height * 0.7} ${width - cornerRadius},${height * 0.7}`;

        // 添加尾巴
        if (tailPos === "bottom-left") {
          bubblePath += ` L ${width * 0.3},${height * 0.7}
                         L ${width * 0.15},${height}
                         L ${width * 0.25},${height * 0.7}`;
        }

        bubblePath += ` L ${cornerRadius},${height * 0.7}
                       Q 0,${height * 0.7} 0,${height * 0.7 - cornerRadius}
                       L 0,${cornerRadius}
                       Q 0,0 ${cornerRadius},0 Z`;

        bubble.setAttribute("d", bubblePath);
        bubble.setAttribute("stroke", strokeColor);
        bubble.setAttribute("stroke-width", strokeWidth.toString());
        bubble.setAttribute("fill", fillColor);
        svg.appendChild(bubble);
        break;

      case "flowchartStart":
        const startShape = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "ellipse"
        );
        startShape.setAttribute("cx", (width / 2).toString());
        startShape.setAttribute("cy", (height / 2).toString());
        startShape.setAttribute("rx", (width / 2 - strokeWidth).toString());
        startShape.setAttribute("ry", (height / 2 - strokeWidth).toString());
        startShape.setAttribute("stroke", strokeColor);
        startShape.setAttribute("stroke-width", strokeWidth.toString());
        startShape.setAttribute("fill", fillColor);
        svg.appendChild(startShape);
        break;

      case "flowchartProcess":
        const processShape = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "rect"
        );
        processShape.setAttribute("x", "1");
        processShape.setAttribute("y", "1");
        processShape.setAttribute("width", (width - 2).toString());
        processShape.setAttribute("height", (height - 2).toString());
        processShape.setAttribute("stroke", strokeColor);
        processShape.setAttribute("stroke-width", strokeWidth.toString());
        processShape.setAttribute("fill", fillColor);
        svg.appendChild(processShape);
        break;

      case "flowchartDecision":
        const decision = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "polygon"
        );
        const decisionPoints = `${width / 2},${strokeWidth} ${width - strokeWidth},${height / 2} ${width / 2},${height - strokeWidth} ${strokeWidth},${height / 2}`;
        decision.setAttribute("points", decisionPoints);
        decision.setAttribute("stroke", strokeColor);
        decision.setAttribute("stroke-width", strokeWidth.toString());
        decision.setAttribute("fill", fillColor);
        svg.appendChild(decision);
        break;

      case "connector":
        const connectorLine = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "line"
        );
        connectorLine.setAttribute("x1", "0");
        connectorLine.setAttribute("y1", (height / 2).toString());
        connectorLine.setAttribute("x2", width.toString());
        connectorLine.setAttribute("y2", (height / 2).toString());
        connectorLine.setAttribute("stroke", strokeColor);
        connectorLine.setAttribute("stroke-width", strokeWidth.toString());

        // 添加虚线效果
        if (properties.connectionType === "dashed") {
          connectorLine.setAttribute("stroke-dasharray", "5,5");
        }
        svg.appendChild(connectorLine);

        // 连接器末端
        if (properties.endConnector === "arrow") {
          const endArrow = document.createElementNS(
            "http://www.w3.org/2000/svg",
            "polygon"
          );
          const endY = height / 2;
          endArrow.setAttribute(
            "points",
            `${width},${endY} ${width - 8},${endY - 4} ${width - 8},${endY + 4}`
          );
          endArrow.setAttribute("fill", strokeColor);
          svg.appendChild(endArrow);
        }
        break;

      // 新增支持的形状类型
      case "drop":
      case "shape-drop":
        const drop = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "path"
        );
        const dropPath = `M ${width / 2},${height * 0.1}
                         C ${width * 0.2},${height * 0.1} ${width * 0.1},${height * 0.4} ${width * 0.1},${height * 0.6}
                         C ${width * 0.1},${height * 0.8} ${width * 0.3},${height * 0.9} ${width / 2},${height * 0.9}
                         C ${width * 0.7},${height * 0.9} ${width * 0.9},${height * 0.8} ${width * 0.9},${height * 0.6}
                         C ${width * 0.9},${height * 0.4} ${width * 0.8},${height * 0.1} ${width / 2},${height * 0.1} Z`;
        drop.setAttribute("d", dropPath);
        drop.setAttribute("stroke", strokeColor);
        drop.setAttribute("stroke-width", strokeWidth.toString());
        drop.setAttribute("fill", fillColor);
        svg.appendChild(drop);
        break;

      case "cross":
      case "shape-cross":
        // 使用线条绘制十字，更接近实际icon
        const crossH = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "line"
        );
        crossH.setAttribute("x1", (width * 0.2).toString());
        crossH.setAttribute("y1", (height / 2).toString());
        crossH.setAttribute("x2", (width * 0.8).toString());
        crossH.setAttribute("y2", (height / 2).toString());
        crossH.setAttribute("stroke", strokeColor);
        crossH.setAttribute("stroke-width", (strokeWidth * 2).toString());
        crossH.setAttribute("stroke-linecap", "round");
        svg.appendChild(crossH);

        const crossV = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "line"
        );
        crossV.setAttribute("x1", (width / 2).toString());
        crossV.setAttribute("y1", (height * 0.2).toString());
        crossV.setAttribute("x2", (width / 2).toString());
        crossV.setAttribute("y2", (height * 0.8).toString());
        crossV.setAttribute("stroke", strokeColor);
        crossV.setAttribute("stroke-width", (strokeWidth * 2).toString());
        crossV.setAttribute("stroke-linecap", "round");
        svg.appendChild(crossV);
        break;

      case "octagon":
      case "shape-octagon":
        const octagon = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "polygon"
        );
        const octaSides = properties.sides || 8;
        const octaCenterX = width / 2;
        const octaCenterY = height / 2;
        const octaRadius = Math.min(width, height) / 2 - strokeWidth;

        let octaPoints = "";
        for (let i = 0; i < octaSides; i++) {
          const angle = (i * 2 * Math.PI) / octaSides - Math.PI / 2;
          const x = octaCenterX + octaRadius * Math.cos(angle);
          const y = octaCenterY + octaRadius * Math.sin(angle);
          octaPoints += `${x},${y} `;
        }

        octagon.setAttribute("points", octaPoints.trim());
        octagon.setAttribute("stroke", strokeColor);
        octagon.setAttribute("stroke-width", strokeWidth.toString());
        octagon.setAttribute("fill", fillColor);
        svg.appendChild(octagon);
        break;

      case "minus":
        const minusLine = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "line"
        );
        const minusY = height / 2;
        const minusPadding = width * 0.2; // 左右留20%的边距
        minusLine.setAttribute("x1", minusPadding.toString());
        minusLine.setAttribute("y1", minusY.toString());
        minusLine.setAttribute("x2", (width - minusPadding).toString());
        minusLine.setAttribute("y2", minusY.toString());
        minusLine.setAttribute("stroke", strokeColor);
        minusLine.setAttribute("stroke-width", (strokeWidth * 3).toString()); // 使用更粗的线条
        minusLine.setAttribute("stroke-linecap", "round");
        svg.appendChild(minusLine);
        break;

      default:
        // 对于未知的图形类型，绘制一个默认的矩形
        const defaultRect = document.createElementNS(
          "http://www.w3.org/2000/svg",
          "rect"
        );
        defaultRect.setAttribute("x", "1");
        defaultRect.setAttribute("y", "1");
        defaultRect.setAttribute("width", (width - 2).toString());
        defaultRect.setAttribute("height", (height - 2).toString());
        defaultRect.setAttribute("stroke", strokeColor);
        defaultRect.setAttribute("stroke-width", strokeWidth.toString());
        defaultRect.setAttribute("fill", fillColor);
        defaultRect.setAttribute("stroke-dasharray", "5,5"); // 用虚线表示未知类型
        svg.appendChild(defaultRect);
        break;
    }
  }

  /**
   * 更新绘图组件
   */
  updateDrawingComponent(componentId: string, properties: any) {
    // 更新组件属性
  }

  /**
   * 销毁绘图组件
   */
  destroyDrawingComponent(componentId: string) {
    // 清理组件资源
  }

  /**
   * 清理资源
   */
  destroy() {
    // 清理管理器资源
  }

  // 事件回调
  onDrawingComplete?: (componentId: string, shapeData: any) => void;

  /**
   * 创建文本元素DOM
   */
  private createTextElement(
    component: DrawingComponent,
    properties: any
  ): HTMLElement {
    console.log("创建文本元素，组件数据:", component);
    console.log("组件尺寸:", {
      width: component.width,
      height: component.height
    });

    const element = document.createElement("div");
    element.id = component.id;
    element.className = "fuxa-component text-component";
    element.contentEditable = "true";

    // 强制使用固定尺寸，防止继承问题
    const width = 120;
    const height = 40;

    console.log("设置文本元素样式，强制固定尺寸:", { width, height });

    // 使用更强的样式重置，防止CSS继承问题
    element.style.cssText = `
      position: absolute !important;
      left: ${component.x || 0}px !important;
      top: ${component.y || 0}px !important;
      width: ${width}px !important;
      height: ${height}px !important;
      max-width: ${width}px !important;
      max-height: ${height}px !important;
      min-width: ${width}px !important;
      min-height: ${height}px !important;
      flex: none !important;
      flex-grow: 0 !important;
      flex-shrink: 0 !important;
      flex-basis: auto !important;
      cursor: text !important;
      user-select: text !important;
      z-index: 10 !important;
      box-sizing: border-box !important;
      font-size: ${properties.fontSize || 14}px !important;
      font-family: ${properties.fontFamily || "Arial"} !important;
      font-weight: ${properties.fontWeight || "normal"} !important;
      color: ${properties.color || "#303133"} !important;
      text-align: ${properties.textAlign || "left"} !important;
      background-color: ${properties.backgroundColor || "transparent"} !important;
      border: ${properties.borderWidth || 2}px ${properties.borderStyle || "solid"} ${properties.borderColor || "#409eff"} !important;
      padding: ${properties.padding || 8}px !important;
      display: flex !important;
      align-items: ${this.getVerticalAlignment(properties.verticalAlign)} !important;
      overflow: hidden !important;
      word-wrap: break-word !important;
      outline: none !important;
      /* 重置可能影响尺寸的属性 */
      margin: 0 !important;
      transform: none !important;
      scale: none !important;
      /* 防止继承父容器的尺寸 */
      contain: layout style !important;
    `;

    // 设置文本内容
    element.textContent = properties.text || "文本框";

    // 添加事件监听器
    this.addTextEventListeners(element);

    // 强制重新计算尺寸（DOM操作）
    element.offsetWidth; // 强制回流
    element.offsetHeight;

    console.log("createTextElement 创建完成，元素信息:", {
      id: element.id,
      className: element.className,
      offsetWidth: element.offsetWidth,
      offsetHeight: element.offsetHeight,
      computedStyles: {
        width: element.style.width,
        height: element.style.height,
        position: element.style.position,
        display: element.style.display
      }
    });

    // 添加调试监控，观察元素添加到DOM后的变化
    const observer = new MutationObserver(mutations => {
      mutations.forEach(mutation => {
        if (
          mutation.type === "attributes" &&
          mutation.attributeName === "style"
        ) {
          console.log("文本元素样式发生变化:", {
            elementId: element.id,
            newWidth: element.offsetWidth,
            newHeight: element.offsetHeight,
            styleWidth: element.style.width,
            styleHeight: element.style.height
          });
        }
      });
    });

    // 开始观察
    observer.observe(element, {
      attributes: true,
      attributeFilter: ["style", "class"]
    });

    // 5秒后停止观察（防止内存泄漏）
    setTimeout(() => {
      observer.disconnect();
    }, 5000);

    return element;
  }

  /**
   * 获取垂直对齐方式
   */
  private getVerticalAlignment(verticalAlign?: string): string {
    switch (verticalAlign) {
      case "top":
        return "flex-start";
      case "bottom":
        return "flex-end";
      case "middle":
      default:
        return "center";
    }
  }

  /**
   * 添加文本组件事件监听器
   */
  private addTextEventListeners(element: HTMLElement) {
    // 获得焦点时高亮边框
    element.addEventListener("focus", () => {
      element.style.borderColor = "#67c23a";
      element.style.borderWidth = "2px";
      element.style.outline = "none";
    });

    // 失去焦点时恢复边框
    element.addEventListener("blur", () => {
      element.style.borderColor = "#409eff";
      element.style.borderWidth = "2px";
    });

    // 鼠标悬停效果
    element.addEventListener("mouseenter", () => {
      if (document.activeElement !== element) {
        element.style.borderColor = "#85ce61";
      }
    });

    element.addEventListener("mouseleave", () => {
      if (document.activeElement !== element) {
        element.style.borderColor = "#409eff";
      }
    });

    // 防止默认的拖拽行为
    element.addEventListener("dragstart", e => {
      e.preventDefault();
    });

    // 双击选中全部文本
    element.addEventListener("dblclick", () => {
      const selection = window.getSelection();
      const range = document.createRange();
      range.selectNodeContents(element);
      selection?.removeAllRanges();
      selection?.addRange(range);
    });
  }
}
