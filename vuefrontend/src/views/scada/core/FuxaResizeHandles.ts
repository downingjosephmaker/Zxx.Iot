import { ref, reactive, computed } from "vue";
import type { FuxaComponent } from "@/types/fuxa";

/**
 * 调整方向类型
 */
export type ResizeDirection = "nw" | "n" | "ne" | "w" | "e" | "sw" | "s" | "se";

/**
 * 调整手柄配置接口
 */
export interface ResizeHandleConfig {
  size: number;
  color: string;
  borderColor: string;
  borderWidth: number;
  borderRadius: number;
  hoverColor: string;
  activeColor: string;
}

/**
 * 调整状态接口
 */
export interface ResizeState {
  isResizing: boolean;
  direction: ResizeDirection | null;
  startPosition: { x: number; y: number };
  startSize: { width: number; height: number };
  startComponentPosition: { x: number; y: number };
}

/**
 * 约束配置接口
 */
export interface ResizeConstraints {
  minWidth: number;
  minHeight: number;
  maxWidth?: number;
  maxHeight?: number;
  aspectRatio?: number;
  snapToGrid?: boolean;
  gridSize?: number;
}

/**
 * FUXA调整大小手柄管理器
 * 提供组件的调整大小功能
 */
export class FuxaResizeHandles {
  private _config: ResizeHandleConfig = {
    size: 8,
    color: "#409eff",
    borderColor: "#ffffff",
    borderWidth: 1,
    borderRadius: 2,
    hoverColor: "#337ecc",
    activeColor: "#2d6fbd"
  };

  private _state = reactive<ResizeState>({
    isResizing: false,
    direction: null,
    startPosition: { x: 0, y: 0 },
    startSize: { width: 0, height: 0 },
    startComponentPosition: { x: 0, y: 0 }
  });

  private _activeElement: HTMLElement | null = null;
  private _activeComponent: FuxaComponent | null = null;
  private _constraints: ResizeConstraints | null = null;
  private _canvasZoom = ref(100);
  private _onResizeCallbacks: Array<(component: FuxaComponent, dimensions: { width: number; height: number; x: number; y: number }) => void> = [];
  private _onResizeEndCallbacks: Array<(component: FuxaComponent, dimensions: { width: number; height: number; x: number; y: number }) => void> = [];

  constructor(config?: Partial<ResizeHandleConfig>) {
    if (config) {
      this._config = { ...this._config, ...config };
    }
    this.initializeGlobalEventListeners();
  }

  // ==================== 公共属性 ====================

  /**
   * 获取当前状态
   */
  get state(): ResizeState {
    return this._state;
  }

  /**
   * 设置画布缩放
   */
  set canvasZoom(zoom: number) {
    this._canvasZoom.value = Math.max(10, Math.min(500, zoom));
  }

  get canvasZoom(): number {
    return this._canvasZoom.value;
  }

  // ==================== 手柄管理 ====================

  /**
   * 为元素添加调整大小手柄
   */
  public addResizeHandles(
    element: HTMLElement,
    component: FuxaComponent,
    constraints?: ResizeConstraints
  ): void {
    // 移除已存在的手柄
    this.removeResizeHandles(element);

    // 存储组件信息
    this._activeElement = element;
    this._activeComponent = component;
    this._constraints = constraints || this.getDefaultConstraints(component);

    // 创建手柄容器
    const handleContainer = document.createElement('div');
    handleContainer.className = 'resize-handles-container';
    handleContainer.style.cssText = `
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      pointer-events: none;
      z-index: 1;
    `;

    // 创建所有方向的手柄
    const directions: ResizeDirection[] = [
      "nw",
      "n",
      "ne",
      "w",
      "e",
      "sw",
      "s",
      "se"
    ];

    directions.forEach(direction => {
      const handle = this.createResizeHandle(direction);
      this.attachResizeEvents(handle, direction);
      handleContainer.appendChild(handle);
    });

    element.appendChild(handleContainer);

    // 添加元素标识
    element.classList.add("fuxa-resizable");
  }

  /**
   * 移除调整大小手柄
   */
  public removeResizeHandles(element: HTMLElement): void {
    const handles = element.querySelectorAll(".fuxa-resize-handle");
    handles.forEach(handle => handle.remove());
    element.classList.remove("fuxa-resizable");
  }

  /**
   * 创建调整手柄
   */
  private createResizeHandle(direction: ResizeDirection): HTMLElement {
    const handle = document.createElement("div");
    handle.className = `fuxa-resize-handle fuxa-resize-${direction}`;
    handle.style.cssText = this.generateHandleStyle(direction);

    // 添加悬停效果
    handle.addEventListener("mouseenter", () => {
      if (!this._state.isResizing) {
        handle.style.backgroundColor = this._config.hoverColor;
      }
    });

    handle.addEventListener("mouseleave", () => {
      if (!this._state.isResizing) {
        handle.style.backgroundColor = this._config.color;
      }
    });

    return handle;
  }

  /**
   * 生成手柄样式
   */
  private generateHandleStyle(direction: ResizeDirection): string {
    const position = this.getHandlePosition(direction);
    const cursor = this.getResizeCursor(direction);

    return `
      position: absolute;
      width: ${this._config.size}px;
      height: ${this._config.size}px;
      background: ${this._config.color};
      border: ${this._config.borderWidth}px solid ${this._config.borderColor};
      border-radius: ${this._config.borderRadius}px;
      cursor: ${cursor};
      z-index: 2;
      opacity: 0;
      transition: all 0.2s ease;
      pointer-events: auto;
      ${position}
    `;
  }

  /**
   * 获取手柄位置样式
   */
  private getHandlePosition(direction: ResizeDirection): string {
    const offset = Math.floor(this._config.size / 2);

    const positions: Record<ResizeDirection, string> = {
      nw: `top: -${offset}px; left: -${offset}px;`,
      n: `top: -${offset}px; left: 50%; transform: translateX(-50%);`,
      ne: `top: -${offset}px; right: -${offset}px;`,
      w: `top: 50%; left: -${offset}px; transform: translateY(-50%);`,
      e: `top: 50%; right: -${offset}px; transform: translateY(-50%);`,
      sw: `bottom: -${offset}px; left: -${offset}px;`,
      s: `bottom: -${offset}px; left: 50%; transform: translateX(-50%);`,
      se: `bottom: -${offset}px; right: -${offset}px;`
    };

    return positions[direction];
  }

  /**
   * 获取调整大小光标
   */
  private getResizeCursor(direction: ResizeDirection): string {
    const cursors: Record<ResizeDirection, string> = {
      nw: "nw-resize",
      n: "n-resize",
      ne: "ne-resize",
      w: "w-resize",
      e: "e-resize",
      sw: "sw-resize",
      s: "s-resize",
      se: "se-resize"
    };

    return cursors[direction];
  }

  // ==================== 事件处理 ====================

  /**
   * 附加调整事件
   */
  private attachResizeEvents(
    handle: HTMLElement,
    direction: ResizeDirection
  ): void {
    handle.addEventListener("mousedown", e => {
      this.startResize(e, direction);
    });
  }

  /**
   * 开始调整大小
   */
  private startResize(e: MouseEvent, direction: ResizeDirection): void {
    if (!this._activeElement || !this._activeComponent) return;

    e.stopPropagation();
    e.preventDefault();

    // 设置调整状态
    this._state.isResizing = true;
    this._state.direction = direction;
    this._state.startPosition = { x: e.clientX, y: e.clientY };
    // 获取组件尺寸和位置（兼容不同数据结构）
    const componentDimensions = this.getElementDimensions(this._activeElement, this._activeComponent);
    this._state.startSize = {
      width: componentDimensions.width,
      height: componentDimensions.height
    };
    this._state.startComponentPosition = {
      x: componentDimensions.x,
      y: componentDimensions.y
    };

    // 设置活动状态样式
    const handles = this._activeElement.querySelectorAll(".fuxa-resize-handle");
    handles.forEach(h => {
      (h as HTMLElement).style.backgroundColor = this._config.activeColor;
    });

    // 添加文档级事件监听
    document.addEventListener("mousemove", this.handleMouseMove);
    document.addEventListener("mouseup", this.handleMouseUp);

    // 设置文档光标
    document.body.style.cursor = this.getResizeCursor(direction);
    document.body.style.userSelect = "none";

    console.log(`开始调整大小: ${direction}`);
  }

  /**
   * 处理鼠标移动
   */
  private handleMouseMove = (e: MouseEvent): void => {
    if (
      !this._state.isResizing ||
      !this._activeElement ||
      !this._activeComponent ||
      !this._constraints
    )
      return;

    const deltaX = e.clientX - this._state.startPosition.x;
    const deltaY = e.clientY - this._state.startPosition.y;
    const scale = this._canvasZoom.value / 100;

    const scaledDeltaX = deltaX / scale;
    const scaledDeltaY = deltaY / scale;

    const newDimensions = this.calculateNewDimensions(
      this._state.direction!,
      scaledDeltaX,
      scaledDeltaY,
      this._state.startSize,
      this._state.startComponentPosition
    );

    // 应用约束
    const constrainedDimensions = this.applyConstraints(newDimensions);

    // 更新DOM元素
    this.updateElementSize(this._activeElement, constrainedDimensions);

    // 触发调整回调，传递最新的尺寸数据
    this._onResizeCallbacks.forEach(callback => {
      callback(this._activeComponent!, constrainedDimensions);
    });
  };

  /**
   * 处理鼠标释放
   */
  private handleMouseUp = (): void => {
    if (!this._state.isResizing || !this._activeComponent) return;

    // 更新组件数据
    const element = this._activeElement!;
    const rect = element.getBoundingClientRect();
    const parentRect = element.parentElement?.getBoundingClientRect();

    if (parentRect) {
      this._activeComponent.x =
        (rect.left - parentRect.left) / (this._canvasZoom.value / 100);
      this._activeComponent.y =
        (rect.top - parentRect.top) / (this._canvasZoom.value / 100);
    }

    this._activeComponent.width =
      parseInt(element.style.width) || this._activeComponent.width;
    this._activeComponent.height =
      parseInt(element.style.height) || this._activeComponent.height;
    this._activeComponent.updated = new Date().toISOString();

    // 重置状态
    this._state.isResizing = false;
    this._state.direction = null;

    // 清理样式
    document.body.style.cursor = "";
    document.body.style.userSelect = "";

    // 重置手柄样式
    const handles = element.querySelectorAll(".fuxa-resize-handle");
    handles.forEach(h => {
      (h as HTMLElement).style.backgroundColor = this._config.color;
    });

    // 移除文档级事件监听
    document.removeEventListener("mousemove", this.handleMouseMove);
    document.removeEventListener("mouseup", this.handleMouseUp);

    // 重新添加手柄（因为尺寸可能发生变化）
    if (this._activeComponent && this._constraints) {
      this.addResizeHandles(element, this._activeComponent, this._constraints);
    }

    // 触发调整结束回调，传递最终的尺寸数据
    const finalDimensions = {
      width: parseInt(element.style.width) || this._activeComponent.width || 60,
      height: parseInt(element.style.height) || this._activeComponent.height || 60,
      x: this._activeComponent.x || 0,
      y: this._activeComponent.y || 0
    };
    this._onResizeEndCallbacks.forEach(callback => {
      callback(this._activeComponent!, finalDimensions);
    });

    console.log("调整大小完成");
  };

  // ==================== 尺寸计算 ====================

  /**
   * 计算新的尺寸和位置
   */
  private calculateNewDimensions(
    direction: ResizeDirection,
    deltaX: number,
    deltaY: number,
    startSize: { width: number; height: number },
    startPosition: { x: number; y: number }
  ): { width: number; height: number; x: number; y: number } {
    let newWidth = startSize.width;
    let newHeight = startSize.height;
    let newX = startPosition.x;
    let newY = startPosition.y;

    // 水平调整
    if (direction.includes("e")) {
      newWidth = startSize.width + deltaX;
    } else if (direction.includes("w")) {
      newWidth = startSize.width - deltaX;
      newX = startPosition.x + deltaX;
    }

    // 垂直调整
    if (direction.includes("s")) {
      newHeight = startSize.height + deltaY;
    } else if (direction.includes("n")) {
      newHeight = startSize.height - deltaY;
      newY = startPosition.y + deltaY;
    }

    return { width: newWidth, height: newHeight, x: newX, y: newY };
  }

  /**
   * 应用尺寸约束
   */
  private applyConstraints(dimensions: {
    width: number;
    height: number;
    x: number;
    y: number;
  }): typeof dimensions {
    if (!this._constraints) return dimensions;

    let { width, height, x, y } = dimensions;

    // 最小尺寸约束
    width = Math.max(width, this._constraints.minWidth);
    height = Math.max(height, this._constraints.minHeight);

    // 最大尺寸约束
    if (this._constraints.maxWidth) {
      width = Math.min(width, this._constraints.maxWidth);
    }
    if (this._constraints.maxHeight) {
      height = Math.min(height, this._constraints.maxHeight);
    }

    // 长宽比约束
    if (this._constraints.aspectRatio) {
      const currentRatio = width / height;
      if (Math.abs(currentRatio - this._constraints.aspectRatio) > 0.01) {
        if (currentRatio > this._constraints.aspectRatio) {
          width = height * this._constraints.aspectRatio;
        } else {
          height = width / this._constraints.aspectRatio;
        }
      }
    }

    // 网格对齐
    if (this._constraints.snapToGrid && this._constraints.gridSize) {
      const grid = this._constraints.gridSize;
      width = Math.round(width / grid) * grid;
      height = Math.round(height / grid) * grid;
      x = Math.round(x / grid) * grid;
      y = Math.round(y / grid) * grid;
    }

    return { width, height, x, y };
  }

  /**
   * 获取元素尺寸信息
   */
  private getElementDimensions(element: HTMLElement, component: FuxaComponent): {
    width: number;
    height: number;
    x: number;
    y: number;
  } {
    // 兼容原始 JS 版本的数据结构
    // 优先使用组件数据，避免DOM尺寸导致的问题
    const width = (component as any).size?.width || component.width || Math.max(element.offsetWidth, 60);
    const height = (component as any).size?.height || component.height || Math.max(element.offsetHeight, 60);
    const x = (component as any).position?.x || component.x || element.offsetLeft;
    const y = (component as any).position?.y || component.y || element.offsetTop;

    console.log('FuxaResizeHandles.getElementDimensions:', {
      componentId: component.id,
      componentSizeWidth: (component as any).size?.width,
      componentWidth: component.width,
      elementOffsetWidth: element.offsetWidth,
      finalWidth: width,
      componentSizeHeight: (component as any).size?.height,
      componentHeight: component.height,
      elementOffsetHeight: element.offsetHeight,
      finalHeight: height
    });

    return { width, height, x, y };
  }

  /**
   * 更新元素尺寸
   */
  private updateElementSize(
    element: HTMLElement,
    dimensions: { width: number; height: number; x: number; y: number }
  ): void {
    // 兼容原始 JS 版本的数据结构
    if (this._activeComponent) {
      if ((this._activeComponent as any).size) {
        (this._activeComponent as any).size.width = dimensions.width;
        (this._activeComponent as any).size.height = dimensions.height;
      } else {
        this._activeComponent.width = dimensions.width;
        this._activeComponent.height = dimensions.height;
      }
      
      if ((this._activeComponent as any).position) {
        (this._activeComponent as any).position.x = dimensions.x;
        (this._activeComponent as any).position.y = dimensions.y;
      } else {
        this._activeComponent.x = dimensions.x;
        this._activeComponent.y = dimensions.y;
      }
    }
    
    // 更新DOM样式
    element.style.width = Math.round(dimensions.width) + "px";
    element.style.height = Math.round(dimensions.height) + "px";
    element.style.left = Math.round(dimensions.x) + "px";
    element.style.top = Math.round(dimensions.y) + "px";
  }

  /**
   * 获取默认约束
   */
  private getDefaultConstraints(component: FuxaComponent): ResizeConstraints {
    // 根据组件类型返回不同的默认约束
    switch (component.type) {
      case "image":
        return { minWidth: 50, minHeight: 50 };
      case "pie-chart":
      case "gauge-chart":
        return { minWidth: 200, minHeight: 200, aspectRatio: 1 };
      case "bar-chart":
      case "line-chart":
        return { minWidth: 300, minHeight: 200 };
      case "time-picker":
        return { minWidth: 120, minHeight: 40 };
      case "text":
        return { minWidth: 20, minHeight: 20 };
      case "rectangle":
      case "circle":
        return { minWidth: 10, minHeight: 10 };
      case "line":
        return { minWidth: 10, minHeight: 2 };
      default:
        return { minWidth: 20, minHeight: 20 };
    }
  }

  // ==================== 显示控制 ====================

  /**
   * 显示调整手柄
   */
  public showResizeHandles(element: HTMLElement): void {
    const handles = element.querySelectorAll(
      ".fuxa-resize-handle"
    ) as NodeListOf<HTMLElement>;
    handles.forEach(handle => {
      handle.style.opacity = "1";
    });
  }

  /**
   * 隐藏调整手柄
   */
  public hideResizeHandles(element: HTMLElement): void {
    const handles = element.querySelectorAll(
      ".fuxa-resize-handle"
    ) as NodeListOf<HTMLElement>;
    handles.forEach(handle => {
      handle.style.opacity = "0";
    });
  }

  /**
   * 切换调整手柄显示状态
   */
  public toggleResizeHandles(element: HTMLElement, show?: boolean): void {
    const shouldShow =
      show !== undefined
        ? show
        : !element.querySelector('.fuxa-resize-handle[style*="opacity: 1"]');

    if (shouldShow) {
      this.showResizeHandles(element);
    } else {
      this.hideResizeHandles(element);
    }
  }

  // ==================== 事件回调 ====================

  /**
   * 添加调整回调
   */
  public onResize(callback: (component: FuxaComponent, dimensions: { width: number; height: number; x: number; y: number }) => void): () => void {
    this._onResizeCallbacks.push(callback);

    // 返回取消函数
    return () => {
      const index = this._onResizeCallbacks.indexOf(callback);
      if (index > -1) {
        this._onResizeCallbacks.splice(index, 1);
      }
    };
  }

  /**
   * 添加调整结束回调
   */
  public onResizeEnd(callback: (component: FuxaComponent, dimensions: { width: number; height: number; x: number; y: number }) => void): () => void {
    this._onResizeEndCallbacks.push(callback);

    // 返回取消函数
    return () => {
      const index = this._onResizeEndCallbacks.indexOf(callback);
      if (index > -1) {
        this._onResizeEndCallbacks.splice(index, 1);
      }
    };
  }

  // ==================== 全局事件 ====================

  /**
   * 初始化全局事件监听器
   */
  private initializeGlobalEventListeners(): void {
    // 监听选择事件
    document.addEventListener("fuxa:component:selected", (e: any) => {
      const { element, component } = e.detail;
      if (element && component) {
        // 隐藏其他元素的手柄
        document.querySelectorAll(".fuxa-resizable").forEach(el => {
          if (el !== element) {
            this.hideResizeHandles(el as HTMLElement);
          }
        });

        // 显示当前元素的手柄
        this.showResizeHandles(element);
      }
    });

    // 监听取消选择事件
    document.addEventListener("fuxa:component:deselected", () => {
      document.querySelectorAll(".fuxa-resizable").forEach(el => {
        this.hideResizeHandles(el as HTMLElement);
      });
    });

    // 按Esc键取消调整
    document.addEventListener("keydown", e => {
      if (e.key === "Escape" && this._state.isResizing) {
        this.cancelResize();
      }
    });
  }

  /**
   * 取消调整
   */
  private cancelResize(): void {
    if (
      !this._state.isResizing ||
      !this._activeElement ||
      !this._activeComponent
    )
      return;

    // 恢复原始尺寸
    this.updateElementSize(this._activeElement, {
      width: this._state.startSize.width,
      height: this._state.startSize.height,
      x: this._state.startComponentPosition.x,
      y: this._state.startComponentPosition.y
    });

    // 触发鼠标释放处理
    this.handleMouseUp();

    console.log("调整大小已取消");
  }

  // ==================== 配置管理 ====================

  /**
   * 更新配置
   */
  public updateConfig(config: Partial<ResizeHandleConfig>): void {
    this._config = { ...this._config, ...config };
  }

  /**
   * 获取配置
   */
  public getConfig(): ResizeHandleConfig {
    return { ...this._config };
  }

  // ==================== 公共接口 ====================

  /**
   * 检查是否正在调整大小
   */
  public isResizing(): boolean {
    return this._state.isResizing;
  }

  /**
   * 获取当前调整方向
   */
  public getCurrentDirection(): ResizeDirection | null {
    return this._state.direction;
  }

  /**
   * 销毁调整手柄管理器
   */
  public dispose(): void {
    // 移除全局事件监听器
    document.removeEventListener("mousemove", this.handleMouseMove);
    document.removeEventListener("mouseup", this.handleMouseUp);

    // 清理状态
    this._state.isResizing = false;
    this._state.direction = null;
    this._activeElement = null;
    this._activeComponent = null;
    this._constraints = null;

    // 清理回调
    this._onResizeCallbacks = [];
    this._onResizeEndCallbacks = [];

    console.log("调整手柄管理器已清理");
  }
}

// 导出单例实例
export const fuxaResizeHandles = new FuxaResizeHandles();

// 导出类型
export type { ResizeHandleConfig, ResizeState, ResizeConstraints };

// 导出便捷函数
export const addResizeHandles = (element: HTMLElement, component: FuxaComponent, constraints?: ResizeConstraints) => {
  return fuxaResizeHandles.addResizeHandles(element, component, constraints);
};

export const removeResizeHandles = (element: HTMLElement) => {
  return fuxaResizeHandles.removeResizeHandles(element);
};
