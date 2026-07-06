/**
 * 路径工具 - 独立的路径绘制工具，支持连续多段直线绘制
 * 特性：点击添加节点，每个节点显示小圆圈，双击完成绘制
 */

import { fuxaResizeHandles, type ResizeConstraints } from './FuxaResizeHandles'

/**
 * 路径组件接口
 */
export interface PathComponent {
  id: string;
  type: 'path';
  x: number;
  y: number;
  width: number;
  height: number;
  points: { x: number; y: number }[];
  properties: {
    strokeColor: string;
    strokeWidth: number;
    nodeColor: string;
    nodeSize: number;
    showNodes: boolean;
  };
}

/**
 * 路径工具类
 */
export class PathTool {
  private canvas: HTMLCanvasElement | null = null;
  private ctx: CanvasRenderingContext2D | null = null;
  private container: HTMLElement | null = null;

  // 绘制状态
  private isDrawing = false;
  private currentPoints: { x: number; y: number }[] = [];
  private previewPoint: { x: number; y: number } | null = null;

  // 编辑状态
  private isEditingPath = false;
  private currentEditingPathId: string | null = null;
  private selectedPathId: string | null = null;

  // 配置
  private config = {
    strokeColor: '#409eff',
    strokeWidth: 2,
    nodeColor: '#409eff',
    nodeSize: 6
  };

  // 事件回调
  public onPathComplete?: (path: PathComponent) => void;
  public onPathSelected?: (pathId: string) => void;
  public onPathUpdated?: (pathId: string, updates: Partial<PathComponent>) => void;
  public onPathDeleted?: (pathId: string) => void;

  /**
   * 初始化路径工具
   */
  initialize(container: HTMLElement) {
    this.container = container;
    this.setupCanvas();
    this.setupEventListeners();
  }

  /**
   * 设置画布
   */
  private setupCanvas() {
    if (!this.container) return;

    // 查找或创建画布
    let canvas = this.container.querySelector('.path-canvas') as HTMLCanvasElement;
    if (!canvas) {
      canvas = document.createElement('canvas');
      canvas.className = 'path-canvas';
      canvas.style.cssText = `
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        pointer-events: auto;
        z-index: 5;
        cursor: crosshair;
        display: none;
      `;
      this.container.appendChild(canvas);
    }

    this.canvas = canvas;
    this.ctx = canvas.getContext('2d');
    this.updateCanvasSize();
  }

  /**
   * 更新画布尺寸
   */
  private updateCanvasSize() {
    if (!this.canvas || !this.container) return;

    const rect = this.container.getBoundingClientRect();
    this.canvas.width = rect.width || 800;
    this.canvas.height = rect.height || 600;
  }

  /**
   * 设置事件监听器
   */
  private setupEventListeners() {
    if (!this.canvas) return;

    this.canvas.addEventListener('mousedown', this.handleMouseDown.bind(this));
    this.canvas.addEventListener('mousemove', this.handleMouseMove.bind(this));
    this.canvas.addEventListener('dblclick', this.handleDoubleClick.bind(this));
    this.canvas.addEventListener('contextmenu', this.handleRightClick.bind(this));
  }

  /**
   * 开始路径绘制
   */
  startDrawing() {
    if (!this.canvas) return;

    this.isDrawing = true;
    this.currentPoints = [];
    this.previewPoint = null;

    this.canvas.style.display = 'block';
    console.log('PathTool: 开始路径绘制');
  }

  /**
   * 停止路径绘制
   */
  stopDrawing() {
    if (!this.canvas) return;

    this.isDrawing = false;
    this.currentPoints = [];
    this.previewPoint = null;

    this.canvas.style.display = 'none';
    this.clearCanvas();
    console.log('PathTool: 停止路径绘制');
  }

  /**
   * 鼠标按下事件
   */
  private handleMouseDown(event: MouseEvent) {
    if (!this.isDrawing || !this.canvas) return;

    // 只处理左键点击
    if (event.button !== 0) return;

    const rect = this.canvas.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;

    // 添加新节点
    this.currentPoints.push({ x, y });
    this.redrawCanvas();

    console.log(`PathTool: 添加节点${this.currentPoints.length} (${Math.round(x)}, ${Math.round(y)})`);

    // 给出操作提示
    if (this.currentPoints.length === 1) {
      console.log('PathTool: 继续点击添加节点，右键或双击完成绘制');
    }
  }

  /**
   * 鼠标移动事件
   */
  private handleMouseMove(event: MouseEvent) {
    if (!this.isDrawing || !this.canvas || this.currentPoints.length === 0) return;

    const rect = this.canvas.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;

    // 更新预览点
    this.previewPoint = { x, y };
    this.redrawCanvas();
  }

  /**
   * 双击事件 - 完成路径绘制
   */
  private handleDoubleClick(event: MouseEvent) {
    if (!this.isDrawing || this.currentPoints.length < 2) return;

    this.finishPath();
  }

  /**
   * 右键点击事件 - 完成路径绘制
   */
  private handleRightClick(event: MouseEvent) {
    event.preventDefault(); // 阻止默认右键菜单

    if (!this.isDrawing) return;

    // 如果至少有2个点，完成路径绘制
    if (this.currentPoints.length >= 2) {
      this.finishPath();
    } else {
      // 如果点数不足，取消绘制
      this.stopDrawing();
      console.log('PathTool: 路径点数不足，取消绘制');
    }
  }

  /**
   * 完成路径绘制
   */
  private finishPath() {
    if (this.currentPoints.length < 2) {
      console.warn('PathTool: 路径至少需要2个节点');
      return;
    }

    // 计算边界框
    const bounds = this.calculateBounds(this.currentPoints);

    // 创建路径组件
    const pathComponent: PathComponent = {
      id: `path_${Date.now()}`,
      type: 'path',
      x: bounds.minX - 10,
      y: bounds.minY - 10,
      width: bounds.width + 20,
      height: bounds.height + 20,
      points: [...this.currentPoints],
      properties: {
        strokeColor: this.config.strokeColor,
        strokeWidth: this.config.strokeWidth,
        nodeColor: this.config.nodeColor,
        nodeSize: this.config.nodeSize,
        showNodes: true
      }
    };

    // 触发完成事件，让外部系统处理组件创建和添加
    if (this.onPathComplete) {
      this.onPathComplete(pathComponent);
    }

    // 停止绘制
    this.stopDrawing();

    console.log(`PathTool: 完成路径绘制，共${this.currentPoints.length}个节点，总长度${Math.round(this.calculatePathLength())}px`);
  }

  /**
   * 计算路径总长度
   */
  private calculatePathLength(): number {
    if (this.currentPoints.length < 2) return 0;

    let totalLength = 0;
    for (let i = 1; i < this.currentPoints.length; i++) {
      const prev = this.currentPoints[i - 1];
      const curr = this.currentPoints[i];
      const dx = curr.x - prev.x;
      const dy = curr.y - prev.y;
      totalLength += Math.sqrt(dx * dx + dy * dy);
    }
    return totalLength;
  }

  /**
   * 计算点集的边界框
   */
  private calculateBounds(points: { x: number; y: number }[]) {
    if (points.length === 0) {
      return { minX: 0, minY: 0, width: 0, height: 0 };
    }

    let minX = points[0].x;
    let minY = points[0].y;
    let maxX = points[0].x;
    let maxY = points[0].y;

    points.forEach(point => {
      minX = Math.min(minX, point.x);
      minY = Math.min(minY, point.y);
      maxX = Math.max(maxX, point.x);
      maxY = Math.max(maxY, point.y);
    });

    return {
      minX,
      minY,
      width: maxX - minX,
      height: maxY - minY
    };
  }

  /**
   * 重绘画布
   */
  private redrawCanvas() {
    if (!this.ctx || !this.canvas) return;

    // 清除画布
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

    if (this.currentPoints.length === 0) return;

    // 绘制路径线条
    this.drawPathLines();

    // 绘制节点
    this.drawNodes();

    // 绘制预览线段
    if (this.previewPoint && this.currentPoints.length > 0) {
      this.drawPreviewLine();
    }
  }

  /**
   * 绘制路径线条
   */
  private drawPathLines() {
    if (!this.ctx || this.currentPoints.length < 2) return;

    this.ctx.save();
    this.ctx.strokeStyle = this.config.strokeColor;
    this.ctx.lineWidth = this.config.strokeWidth;

    this.ctx.beginPath();
    this.ctx.moveTo(this.currentPoints[0].x, this.currentPoints[0].y);

    for (let i = 1; i < this.currentPoints.length; i++) {
      this.ctx.lineTo(this.currentPoints[i].x, this.currentPoints[i].y);
    }

    this.ctx.stroke();
    this.ctx.restore();
  }

  /**
   * 绘制节点圆圈
   */
  private drawNodes() {
    if (!this.ctx) return;

    this.ctx.save();
    this.ctx.fillStyle = this.config.nodeColor;
    this.ctx.strokeStyle = '#ffffff';
    this.ctx.lineWidth = 2;

    this.currentPoints.forEach(point => {
      this.ctx!.beginPath();
      this.ctx!.arc(point.x, point.y, this.config.nodeSize, 0, 2 * Math.PI);
      this.ctx!.fill();
      this.ctx!.stroke();
    });

    this.ctx.restore();
  }

  /**
   * 绘制预览线段
   */
  private drawPreviewLine() {
    if (!this.ctx || !this.previewPoint || this.currentPoints.length === 0) return;

    const lastPoint = this.currentPoints[this.currentPoints.length - 1];

    this.ctx.save();
    this.ctx.strokeStyle = this.config.strokeColor + '80';
    this.ctx.lineWidth = this.config.strokeWidth;
    this.ctx.setLineDash([5, 5]);

    this.ctx.beginPath();
    this.ctx.moveTo(lastPoint.x, lastPoint.y);
    this.ctx.lineTo(this.previewPoint.x, this.previewPoint.y);
    this.ctx.stroke();

    // 绘制预览节点
    this.ctx.fillStyle = this.config.nodeColor + '80';
    this.ctx.strokeStyle = '#ffffff';
    this.ctx.lineWidth = 2;
    this.ctx.setLineDash([]);

    this.ctx.beginPath();
    this.ctx.arc(this.previewPoint.x, this.previewPoint.y, this.config.nodeSize, 0, 2 * Math.PI);
    this.ctx.fill();
    this.ctx.stroke();

    this.ctx.restore();
  }

  /**
   * 清除画布
   */
  private clearCanvas() {
    if (!this.ctx || !this.canvas) return;
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
  }

  /**
   * 创建标准路径组件
   */
  createPathComponent(pathComponent: PathComponent, container?: HTMLElement): HTMLElement {
    const element = document.createElement('div');
    element.id = pathComponent.id;
    element.className = 'fuxa-component path-component';
    element.setAttribute('data-component-type', 'path');

    // 设置位置和尺寸
    element.style.cssText = `
      position: absolute;
      left: ${pathComponent.x}px;
      top: ${pathComponent.y}px;
      width: ${pathComponent.width}px;
      height: ${pathComponent.height}px;
      pointer-events: auto;
      z-index: 10;
      cursor: pointer;
      user-select: none;
      box-sizing: border-box;
      border: 2px solid transparent;
      transition: border-color 0.2s ease;
    `;

    // 存储组件数据到元素上
    (element as any)._pathComponent = pathComponent;

    // 添加标准组件事件
    this.setupStandardComponentEvents(element, pathComponent);

    // 创建SVG内容
    this.renderPathSVG(element, pathComponent);

    // 添加到容器
    if (container) {
      container.appendChild(element);
    }

    return element;
  }

  /**
   * 渲染路径SVG内容
   */
  private renderPathSVG(element: HTMLElement, pathComponent: PathComponent) {
    // 清空现有内容
    element.innerHTML = '';

    // 创建SVG
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('width', '100%');
    svg.setAttribute('height', '100%');
    svg.setAttribute('viewBox', `0 0 ${pathComponent.width} ${pathComponent.height}`);
    svg.style.overflow = 'visible';
    svg.style.pointerEvents = 'none'; // 让事件穿透到父元素

    // 转换点坐标到相对坐标
    const relativePoints = pathComponent.points.map(point => ({
      x: point.x - pathComponent.x,
      y: point.y - pathComponent.y
    }));

    // 创建路径线条
    if (relativePoints.length >= 2) {
      const polyline = document.createElementNS('http://www.w3.org/2000/svg', 'polyline');
      const points = relativePoints.map(p => `${p.x},${p.y}`).join(' ');

      polyline.setAttribute('points', points);
      polyline.setAttribute('stroke', pathComponent.properties.strokeColor);
      polyline.setAttribute('stroke-width', pathComponent.properties.strokeWidth.toString());
      polyline.setAttribute('fill', 'none');
      polyline.setAttribute('stroke-linecap', 'round');
      polyline.setAttribute('stroke-linejoin', 'round');

      svg.appendChild(polyline);
    }

    // 根据showNodes属性决定是否显示节点
    if (pathComponent.properties.showNodes) {
      relativePoints.forEach((point, index) => {
        const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
        circle.setAttribute('cx', point.x.toString());
        circle.setAttribute('cy', point.y.toString());
        circle.setAttribute('r', pathComponent.properties.nodeSize.toString());
        circle.setAttribute('data-point-index', index.toString());

        if (this.isEditingPath) {
          // 编辑模式：实心圆，可拖拽
          circle.setAttribute('fill', pathComponent.properties.nodeColor);
          circle.setAttribute('stroke', '#ffffff');
          circle.setAttribute('stroke-width', '2');
          circle.style.cursor = 'grab';
          circle.style.pointerEvents = 'auto';

          // 为节点添加拖拽功能
          this.addNodeDragEvents(circle, pathComponent, index);
        } else {
          // 普通模式：空心圆，不可拖拽
          circle.setAttribute('fill', 'none');
          circle.setAttribute('stroke', pathComponent.properties.nodeColor);
          circle.setAttribute('stroke-width', '2');
          circle.style.pointerEvents = 'none';
        }

        svg.appendChild(circle);
      });
    }

    element.appendChild(svg);
  }

  /**
   * 检查是否正在绘制
   */
  isActive(): boolean {
    return this.isDrawing;
  }

  /**
   * 更新配置
   */
  updateConfig(config: Partial<typeof this.config>) {
    Object.assign(this.config, config);
  }

  /**
   * 窗口大小变化处理
   */
  resize() {
    this.updateCanvasSize();
  }

  /**
   * 设置标准组件事件
   */
  private setupStandardComponentEvents(element: HTMLElement, pathComponent: PathComponent) {
    // 点击选中 - 与编辑器选择系统集成
    element.addEventListener('click', (e) => {
      e.stopPropagation();

      // 触发全局选择事件，让编辑器系统处理
      const selectEvent = new CustomEvent('fuxa:component:select', {
        detail: { element, component: pathComponent }
      });
      document.dispatchEvent(selectEvent);

      this.selectPathComponent(element, pathComponent.id);
    });

    // 双击编辑
    element.addEventListener('dblclick', (e) => {
      e.stopPropagation();
      this.enterPathEditMode(pathComponent.id);
    });

    // 右键菜单
    element.addEventListener('contextmenu', (e) => {
      e.preventDefault();
      e.stopPropagation();
      this.showStandardContextMenu(e, pathComponent);
    });

    // 悬停效果
    element.addEventListener('mouseenter', () => {
      if (!element.classList.contains('selected')) {
        element.style.borderColor = '#409eff80';
      }
    });

    element.addEventListener('mouseleave', () => {
      if (!element.classList.contains('selected')) {
        element.style.borderColor = 'transparent';
      }
    });

    // 启用拖拽移动功能，让路径组件可以像其他组件一样移动
    this.setupComponentDrag(element, pathComponent);
  }

  /**
   * 设置组件拖拽
   */
  private setupComponentDrag(element: HTMLElement, pathComponent: PathComponent) {
    let isDragging = false;
    let dragOffset = { x: 0, y: 0 };

    element.addEventListener('mousedown', (e) => {
      // 如果在编辑模式下或者右键，不处理拖拽
      if (this.isEditingPath || e.button !== 0) return;

      e.stopPropagation();
      isDragging = true;

      const rect = element.getBoundingClientRect();
      dragOffset.x = e.clientX - rect.left;
      dragOffset.y = e.clientY - rect.top;

      element.style.cursor = 'grabbing';

      const handleMouseMove = (e: MouseEvent) => {
        if (!isDragging) return;

        const container = element.parentElement;
        if (!container) return;

        const containerRect = container.getBoundingClientRect();
        const newX = e.clientX - containerRect.left - dragOffset.x;
        const newY = e.clientY - containerRect.top - dragOffset.y;

        // 更新位置
        element.style.left = `${newX}px`;
        element.style.top = `${newY}px`;

        // 更新路径组件数据
        const oldX = pathComponent.x;
        const oldY = pathComponent.y;
        const deltaX = newX - oldX;
        const deltaY = newY - oldY;

        pathComponent.x = newX;
        pathComponent.y = newY;

        // 同时移动所有路径点
        pathComponent.points = pathComponent.points.map(point => ({
          x: point.x + deltaX,
          y: point.y + deltaY
        }));

        // 重新渲染
        this.renderPathSVG(element, pathComponent);
      };

      const handleMouseUp = () => {
        isDragging = false;
        element.style.cursor = 'pointer';

        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);

        // 触发更新回调
        if (this.onPathUpdated) {
          this.onPathUpdated(pathComponent.id, pathComponent);
        }
      };

      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
    });
  }

  /**
   * 旧的交互事件方法（已废弃）
   */
  private setupInteractionEvents(element: HTMLElement, pathComponent: PathComponent) {
    let isDragging = false;
    let dragOffset = { x: 0, y: 0 };

    // 点击选中
    element.addEventListener('click', (e) => {
      e.stopPropagation();
      this.selectPath(element, pathComponent.id);
    });

    // 双击编辑
    element.addEventListener('dblclick', (e) => {
      e.stopPropagation();
      this.enterEditMode(element, pathComponent);
    });

    // 拖拽功能
    element.addEventListener('mousedown', (e) => {
      if (e.button !== 0) return; // 只处理左键

      e.stopPropagation();
      isDragging = true;

      const rect = element.getBoundingClientRect();
      dragOffset.x = e.clientX - rect.left;
      dragOffset.y = e.clientY - rect.top;

      element.style.cursor = 'grabbing';

      const handleMouseMove = (e: MouseEvent) => {
        if (!isDragging) return;

        const container = element.parentElement;
        if (!container) return;

        const containerRect = container.getBoundingClientRect();
        const newX = e.clientX - containerRect.left - dragOffset.x;
        const newY = e.clientY - containerRect.top - dragOffset.y;

        // 更新位置
        element.style.left = `${newX}px`;
        element.style.top = `${newY}px`;

        // 更新路径组件数据
        pathComponent.x = newX;
        pathComponent.y = newY;
      };

      const handleMouseUp = () => {
        isDragging = false;
        element.style.cursor = 'pointer';

        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);

        // 触发更新回调
        if (this.onPathUpdated) {
          this.onPathUpdated(pathComponent.id, {
            x: pathComponent.x,
            y: pathComponent.y
          });
        }
      };

      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
    });

    // 右键菜单
    element.addEventListener('contextmenu', (e) => {
      e.preventDefault();
      e.stopPropagation();
      this.showContextMenu(e, pathComponent);
    });

    // 悬停效果
    element.addEventListener('mouseenter', () => {
      if (!element.classList.contains('selected')) {
        element.style.borderColor = '#409eff80';
      }
    });

    element.addEventListener('mouseleave', () => {
      if (!element.classList.contains('selected')) {
        element.style.borderColor = 'transparent';
      }
    });
  }

  /**
   * 为节点添加拖拽事件
   */
  private addNodeDragEvents(circle: SVGCircleElement, pathComponent: PathComponent, pointIndex: number) {
    let isDragging = false;

    circle.addEventListener('mousedown', (e) => {
      e.stopPropagation();
      isDragging = true;
      circle.style.cursor = 'grabbing';

      const svg = circle.ownerSVGElement;
      if (!svg) return;

      const handleMouseMove = (e: MouseEvent) => {
        if (!isDragging) return;

        const rect = svg.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        // 更新节点位置
        circle.setAttribute('cx', x.toString());
        circle.setAttribute('cy', y.toString());

        // 更新路径数据中的点坐标
        const absoluteX = pathComponent.x + x;
        const absoluteY = pathComponent.y + y;
        pathComponent.points[pointIndex] = { x: absoluteX, y: absoluteY };

        // 更新路径线条
        this.updatePathLine(svg, pathComponent);
      };

      const handleMouseUp = () => {
        isDragging = false;
        circle.style.cursor = 'grab';

        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);

        // 触发更新回调
        if (this.onPathUpdated) {
          this.onPathUpdated(pathComponent.id, { points: pathComponent.points });
        }
      };

      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
    });
  }

  /**
   * 更新路径线条
   */
  private updatePathLine(svg: SVGElement, pathComponent: PathComponent) {
    const polyline = svg.querySelector('polyline');
    if (!polyline) return;

    const relativePoints = pathComponent.points.map(point => ({
      x: point.x - pathComponent.x,
      y: point.y - pathComponent.y
    }));

    const points = relativePoints.map(p => `${p.x},${p.y}`).join(' ');
    polyline.setAttribute('points', points);
  }

  /**
   * 选中路径组件（新版本）
   */
  private selectPathComponent(element: HTMLElement, pathId: string) {
    // 清除所有组件的选中状态
    document.querySelectorAll('.fuxa-component.selected').forEach(el => {
      el.classList.remove('selected');
      (el as HTMLElement).style.borderColor = 'transparent';
      // 移除调整手柄
      fuxaResizeHandles.removeResizeHandles(el as HTMLElement);
    });

    // 设置当前选中
    element.classList.add('selected');
    element.style.borderColor = '#409eff';

    // 获取路径组件数据
    const pathComponent = (element as any)._pathComponent as PathComponent;
    if (pathComponent) {
      // 添加调整手柄
      const constraints: ResizeConstraints = {
        minWidth: 20,
        minHeight: 20
      };
      fuxaResizeHandles.addResizeHandles(element, pathComponent as any, constraints);
      fuxaResizeHandles.showResizeHandles(element);
    }

    this.selectedPathId = pathId;

    // 触发选中回调
    if (this.onPathSelected) {
      this.onPathSelected(pathId);
    }

    // 触发全局选中事件
    const customEvent = new CustomEvent('fuxa:component:selected', {
      detail: { element, component: pathComponent }
    });
    document.dispatchEvent(customEvent);
  }

  /**
   * 进入路径编辑模式
   */
  private enterPathEditMode(pathId: string) {
    this.isEditingPath = true;
    this.currentEditingPathId = pathId;

    const element = document.getElementById(pathId);
    const pathComponent = element ? (element as any)._pathComponent as PathComponent : null;

    if (pathComponent) {
      // 显示节点进行编辑
      pathComponent.properties.showNodes = true;
      this.renderPathSVG(element!, pathComponent);

      console.log(`进入路径编辑模式: ${pathId}`);
    }
  }

  /**
   * 退出路径编辑模式
   */
  public exitPathEditMode() {
    if (!this.currentEditingPathId) return;

    const element = document.getElementById(this.currentEditingPathId);
    const pathComponent = element ? (element as any)._pathComponent as PathComponent : null;

    if (pathComponent) {
      // 隐藏节点
      pathComponent.properties.showNodes = false;
      this.renderPathSVG(element!, pathComponent);
    }

    this.isEditingPath = false;
    this.currentEditingPathId = null;

    console.log('退出路径编辑模式');
  }

  /**
   * 显示标准右键菜单
   */
  private showStandardContextMenu(e: MouseEvent, pathComponent: PathComponent) {
    const menu = document.createElement('div');
    menu.className = 'fuxa-context-menu';
    menu.style.cssText = `
      position: fixed;
      left: ${e.clientX}px;
      top: ${e.clientY}px;
      background: white;
      border: 1px solid #dcdfe6;
      border-radius: 4px;
      box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
      padding: 4px 0;
      z-index: 9999;
      min-width: 120px;
      font-size: 14px;
    `;

    const menuItems = [
      { label: '编辑路径', action: () => this.enterPathEditMode(pathComponent.id) },
      { label: '复制', action: () => this.copyPathComponent(pathComponent) },
      { label: '删除', action: () => this.deletePathComponent(pathComponent.id) },
      { label: '置于顶层', action: () => this.moveToTop(pathComponent.id) },
      { label: '置于底层', action: () => this.moveToBottom(pathComponent.id) },
      { label: '属性', action: () => this.showPathProperties(pathComponent) }
    ];

    menuItems.forEach(item => {
      const menuItem = document.createElement('div');
      menuItem.textContent = item.label;
      menuItem.style.cssText = `
        padding: 8px 16px;
        cursor: pointer;
        color: #606266;
        transition: background-color 0.2s;
      `;

      menuItem.addEventListener('mouseenter', () => {
        menuItem.style.backgroundColor = '#f5f7fa';
      });

      menuItem.addEventListener('mouseleave', () => {
        menuItem.style.backgroundColor = 'transparent';
      });

      menuItem.addEventListener('click', () => {
        item.action();
        document.body.removeChild(menu);
      });

      menu.appendChild(menuItem);
    });

    document.body.appendChild(menu);

    // 点击其他地方关闭菜单
    const closeMenu = (e: MouseEvent) => {
      if (!menu.contains(e.target as Node)) {
        document.body.removeChild(menu);
        document.removeEventListener('click', closeMenu);
      }
    };

    setTimeout(() => {
      document.addEventListener('click', closeMenu);
    }, 100);
  }

  /**
   * 复制路径组件
   */
  private copyPathComponent(pathComponent: PathComponent) {
    const copiedPath: PathComponent = {
      ...pathComponent,
      id: `path_${Date.now()}`,
      x: pathComponent.x + 20,
      y: pathComponent.y + 20,
      points: pathComponent.points.map(point => ({
        x: point.x + 20,
        y: point.y + 20
      }))
    };

    // 创建新组件
    this.createPathComponent(copiedPath);

    if (this.onPathComplete) {
      this.onPathComplete(copiedPath);
    }
  }

  /**
   * 删除路径组件
   */
  private deletePathComponent(pathId: string) {
    if (confirm('确定要删除这个路径吗？')) {
      const element = document.getElementById(pathId);
      if (element) {
        // 移除调整手柄
        fuxaResizeHandles.removeResizeHandles(element);

        // 移除元素
        if (element.parentNode) {
          element.parentNode.removeChild(element);
        }
      }

      // 清除编辑状态
      if (this.currentEditingPathId === pathId) {
        this.exitPathEditMode();
      }

      if (this.onPathDeleted) {
        this.onPathDeleted(pathId);
      }
    }
  }

  /**
   * 移到顶层
   */
  private moveToTop(pathId: string) {
    const element = document.getElementById(pathId);
    if (element && element.parentElement) {
      element.style.zIndex = '1000';
    }
  }

  /**
   * 移到底层
   */
  private moveToBottom(pathId: string) {
    const element = document.getElementById(pathId);
    if (element && element.parentElement) {
      element.style.zIndex = '1';
    }
  }

  /**
   * 选中路径（旧版本，已废弃）
   */
  private selectPath(element: HTMLElement, pathId: string) {
    // 清除其他选中状态
    document.querySelectorAll('.path-component.selected').forEach(el => {
      el.classList.remove('selected');
      (el as HTMLElement).style.borderColor = 'transparent';
    });

    // 设置当前选中
    element.classList.add('selected');
    element.style.borderColor = '#409eff';

    // 触发选中回调
    if (this.onPathSelected) {
      this.onPathSelected(pathId);
    }
  }

  /**
   * 进入编辑模式
   */
  private enterEditMode(element: HTMLElement, pathComponent: PathComponent) {
    // 显示所有节点，允许编辑
    pathComponent.properties.showNodes = true;
    this.updatePathDisplay(element, pathComponent);

    console.log(`进入路径编辑模式: ${pathComponent.id}`);
  }

  /**
   * 显示右键菜单
   */
  private showContextMenu(e: MouseEvent, pathComponent: PathComponent) {
    const menu = document.createElement('div');
    menu.className = 'path-context-menu';
    menu.style.cssText = `
      position: fixed;
      left: ${e.clientX}px;
      top: ${e.clientY}px;
      background: white;
      border: 1px solid #dcdfe6;
      border-radius: 4px;
      box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
      padding: 4px 0;
      z-index: 9999;
      min-width: 120px;
    `;

    const menuItems = [
      { label: '编辑路径', action: () => this.enterEditMode(document.getElementById(pathComponent.id)!, pathComponent) },
      { label: '复制', action: () => this.copyPath(pathComponent) },
      { label: '删除', action: () => this.deletePath(pathComponent.id) },
      { label: '属性', action: () => this.showPathProperties(pathComponent) }
    ];

    menuItems.forEach(item => {
      const menuItem = document.createElement('div');
      menuItem.textContent = item.label;
      menuItem.style.cssText = `
        padding: 8px 16px;
        cursor: pointer;
        font-size: 14px;
        color: #606266;
        transition: background-color 0.2s;
      `;

      menuItem.addEventListener('mouseenter', () => {
        menuItem.style.backgroundColor = '#f5f7fa';
      });

      menuItem.addEventListener('mouseleave', () => {
        menuItem.style.backgroundColor = 'transparent';
      });

      menuItem.addEventListener('click', () => {
        item.action();
        document.body.removeChild(menu);
      });

      menu.appendChild(menuItem);
    });

    document.body.appendChild(menu);

    // 点击其他地方关闭菜单
    const closeMenu = (e: MouseEvent) => {
      if (!menu.contains(e.target as Node)) {
        document.body.removeChild(menu);
        document.removeEventListener('click', closeMenu);
      }
    };

    setTimeout(() => {
      document.addEventListener('click', closeMenu);
    }, 100);
  }

  /**
   * 复制路径
   */
  private copyPath(pathComponent: PathComponent) {
    const copiedPath: PathComponent = {
      ...pathComponent,
      id: `path_${Date.now()}`,
      x: pathComponent.x + 20,
      y: pathComponent.y + 20
    };

    if (this.onPathComplete) {
      this.onPathComplete(copiedPath);
    }
  }

  /**
   * 删除路径
   */
  private deletePath(pathId: string) {
    if (confirm('确定要删除这个路径吗？')) {
      const element = document.getElementById(pathId);
      if (element && element.parentNode) {
        element.parentNode.removeChild(element);
      }

      if (this.onPathDeleted) {
        this.onPathDeleted(pathId);
      }
    }
  }

  /**
   * 显示路径属性
   */
  private showPathProperties(pathComponent: PathComponent) {
    console.log('显示路径属性面板', pathComponent);
    // 这里可以触发属性面板显示
    if (this.onPathSelected) {
      this.onPathSelected(pathComponent.id);
    }
  }

  /**
   * 更新路径显示
   */
  private updatePathDisplay(element: HTMLElement, pathComponent: PathComponent) {
    // 重新创建SVG内容
    const svg = element.querySelector('svg');
    if (svg) {
      svg.innerHTML = '';

      // 转换点坐标到相对坐标
      const relativePoints = pathComponent.points.map(point => ({
        x: point.x - pathComponent.x,
        y: point.y - pathComponent.y
      }));

      // 重新创建路径线条
      if (relativePoints.length >= 2) {
        const polyline = document.createElementNS('http://www.w3.org/2000/svg', 'polyline');
        const points = relativePoints.map(p => `${p.x},${p.y}`).join(' ');

        polyline.setAttribute('points', points);
        polyline.setAttribute('stroke', pathComponent.properties.strokeColor);
        polyline.setAttribute('stroke-width', pathComponent.properties.strokeWidth.toString());
        polyline.setAttribute('fill', 'none');
        polyline.setAttribute('stroke-linecap', 'round');
        polyline.setAttribute('stroke-linejoin', 'round');

        svg.appendChild(polyline);
      }

      // 重新创建节点
      if (pathComponent.properties.showNodes) {
        relativePoints.forEach((point, index) => {
          const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
          circle.setAttribute('cx', point.x.toString());
          circle.setAttribute('cy', point.y.toString());
          circle.setAttribute('r', pathComponent.properties.nodeSize.toString());
          circle.setAttribute('fill', pathComponent.properties.nodeColor);
          circle.setAttribute('stroke', '#ffffff');
          circle.setAttribute('stroke-width', '2');
          circle.setAttribute('data-point-index', index.toString());
          circle.style.cursor = 'grab';

          this.addNodeDragEvents(circle, pathComponent, index);
          svg.appendChild(circle);
        });
      }
    }
  }

  /**
   * 公共API：选中路径组件
   */
  selectPathById(pathId: string) {
    const element = document.getElementById(pathId);
    if (element) {
      this.selectPathComponent(element, pathId);
    }
  }

  /**
   * 公共API：更新路径组件属性
   */
  updatePathComponent(pathId: string, updates: Partial<PathComponent>) {
    const element = document.getElementById(pathId);
    if (!element) return;

    const pathComponent = (element as any)._pathComponent as PathComponent;
    if (!pathComponent) return;

    // 更新属性
    Object.assign(pathComponent, updates);
    if (updates.properties) {
      Object.assign(pathComponent.properties, updates.properties);
    }

    // 更新DOM元素
    if (updates.x !== undefined || updates.y !== undefined) {
      element.style.left = `${pathComponent.x}px`;
      element.style.top = `${pathComponent.y}px`;
    }
    if (updates.width !== undefined || updates.height !== undefined) {
      element.style.width = `${pathComponent.width}px`;
      element.style.height = `${pathComponent.height}px`;
    }

    // 更新组件数据
    (element as any)._pathComponent = pathComponent;

    // 重新渲染
    this.renderPathSVG(element, pathComponent);
  }

  /**
   * 公共API：获取路径组件数据
   */
  getPathComponent(pathId: string): PathComponent | null {
    const element = document.getElementById(pathId);
    if (!element) return null;
    return (element as any)._pathComponent as PathComponent || null;
  }

  /**
   * 从DOM元素获取路径组件数据
   */
  private getPathComponentFromElement(element: HTMLElement): PathComponent | null {
    // 这里需要从元素中恢复路径数据
    // 在实际项目中，通常会将数据存储在全局状态管理中
    // 这里提供一个基本的实现示例
    const svg = element.querySelector('svg');
    if (!svg) return null;

    const polyline = svg.querySelector('polyline');
    if (!polyline) return null;

    const pointsStr = polyline.getAttribute('points');
    if (!pointsStr) return null;

    // 解析点数据
    const relativePoints = pointsStr.split(' ').map(pointStr => {
      const [x, y] = pointStr.split(',').map(Number);
      return { x, y };
    });

    const absolutePoints = relativePoints.map(point => ({
      x: point.x + parseInt(element.style.left),
      y: point.y + parseInt(element.style.top)
    }));

    return {
      id: element.id,
      type: 'path',
      x: parseInt(element.style.left),
      y: parseInt(element.style.top),
      width: parseInt(element.style.width),
      height: parseInt(element.style.height),
      points: absolutePoints,
      properties: {
        strokeColor: polyline.getAttribute('stroke') || '#409eff',
        strokeWidth: parseInt(polyline.getAttribute('stroke-width') || '2'),
        nodeColor: '#409eff',
        nodeSize: 6,
        showNodes: svg.querySelectorAll('circle').length > 0
      }
    };
  }

  /**
   * 公共API：删除路径组件
   */
  deletePathById(pathId: string) {
    this.deletePathComponent(pathId);
  }

  /**
   * 公共API：获取所有路径组件
   */
  getAllPathComponents(): PathComponent[] {
    const elements = document.querySelectorAll('.path-component');
    const components: PathComponent[] = [];

    elements.forEach(element => {
      const component = (element as any)._pathComponent as PathComponent;
      if (component) {
        components.push(component);
      }
    });

    return components;
  }

  /**
   * 公共API：清除所有选中状态
   */
  clearSelection() {
    document.querySelectorAll('.path-component.selected').forEach(el => {
      el.classList.remove('selected');
      (el as HTMLElement).style.borderColor = 'transparent';
    });
  }

  /**
   * 公共API：添加新的路径点
   */
  addPathPoint(pathId: string, point: { x: number; y: number }, insertIndex?: number) {
    const element = document.getElementById(pathId);
    if (!element) return;

    const pathComponent = this.getPathComponentFromElement(element);
    if (!pathComponent) return;

    if (insertIndex !== undefined && insertIndex >= 0 && insertIndex <= pathComponent.points.length) {
      pathComponent.points.splice(insertIndex, 0, point);
    } else {
      pathComponent.points.push(point);
    }

    // 重新计算边界框
    const bounds = this.calculateBounds(pathComponent.points);
    pathComponent.x = bounds.minX - 10;
    pathComponent.y = bounds.minY - 10;
    pathComponent.width = bounds.width + 20;
    pathComponent.height = bounds.height + 20;

    // 更新显示
    element.style.left = `${pathComponent.x}px`;
    element.style.top = `${pathComponent.y}px`;
    element.style.width = `${pathComponent.width}px`;
    element.style.height = `${pathComponent.height}px`;

    this.updatePathDisplay(element, pathComponent);

    // 触发更新回调
    if (this.onPathUpdated) {
      this.onPathUpdated(pathComponent.id, pathComponent);
    }
  }

  /**
   * 公共API：删除路径点
   */
  removePathPoint(pathId: string, pointIndex: number) {
    const element = document.getElementById(pathId);
    if (!element) return;

    const pathComponent = this.getPathComponentFromElement(element);
    if (!pathComponent || pointIndex < 0 || pointIndex >= pathComponent.points.length) return;

    // 至少保留2个点
    if (pathComponent.points.length <= 2) {
      console.warn('路径至少需要保留2个点');
      return;
    }

    pathComponent.points.splice(pointIndex, 1);

    // 重新计算边界框
    const bounds = this.calculateBounds(pathComponent.points);
    pathComponent.x = bounds.minX - 10;
    pathComponent.y = bounds.minY - 10;
    pathComponent.width = bounds.width + 20;
    pathComponent.height = bounds.height + 20;

    // 更新显示
    element.style.left = `${pathComponent.x}px`;
    element.style.top = `${pathComponent.y}px`;
    element.style.width = `${pathComponent.width}px`;
    element.style.height = `${pathComponent.height}px`;

    this.updatePathDisplay(element, pathComponent);

    // 触发更新回调
    if (this.onPathUpdated) {
      this.onPathUpdated(pathComponent.id, pathComponent);
    }
  }

  /**
   * 创建路径SVG元素（向后兼容）
   */
  createPathSVG(pathComponent: PathComponent): HTMLElement {
    return this.createPathComponent(pathComponent);
  }

  /**
   * 销毁工具
   */
  destroy() {
    this.stopDrawing();

    // 清理编辑状态
    this.exitPathEditMode();

    if (this.canvas && this.canvas.parentNode) {
      this.canvas.parentNode.removeChild(this.canvas);
    }

    this.canvas = null;
    this.ctx = null;
    this.container = null;
  }
}

// 导出单例实例
export const pathTool = new PathTool();