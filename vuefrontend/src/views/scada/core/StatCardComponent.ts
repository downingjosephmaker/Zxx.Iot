/**
 * 统计数值卡组件（报表专用）
 *
 * 报表里"一个大数字 + 单位 + 标题"是最高频的表达（今日用电量、告警总数…），
 * 此前只能拿 LED 显示屏顶替——那是工业仪表语义，字体与观感都不对。
 * 本组件绑定数据集点位后由 applyPointValue 直接写值（见 main/utils-runtime.ts）。
 */
export interface StatCardConfig {
  /** 标题（数值上方的小字） */
  title?: string;
  /** 数值（绑点位后由运行态写入） */
  value?: string | number;
  /** 单位（数值右侧的小字） */
  unit?: string;
  /** 副标题/备注（数值下方） */
  caption?: string;
  titleColor?: string;
  valueColor?: string;
  backgroundColor?: string;
  borderColor?: string;
  borderRadius?: number;
  /** 数值字号；未设置时按卡片高度自适应 */
  valueFontSize?: number;
  align?: "left" | "center" | "right";
}

export class StatCardComponentManager {
  private instances: Map<string, HTMLElement> = new Map();

  /** 属性回落到默认值（组件面板拖出来时 properties 为空） */
  private normalize(component: any): StatCardConfig {
    const p = component.properties || {};
    return {
      title: p.title ?? "统计指标",
      value: p.value ?? "--",
      unit: p.unit ?? "",
      caption: p.caption ?? "",
      titleColor: p.titleColor ?? "#909399",
      valueColor: p.valueColor ?? "#303133",
      backgroundColor: p.backgroundColor ?? "#ffffff",
      borderColor: p.borderColor ?? "#e4e7ed",
      borderRadius: p.borderRadius ?? 6,
      valueFontSize: p.valueFontSize,
      align: p.align ?? "center"
    };
  }

  private paint(element: HTMLElement, component: any) {
    const c = this.normalize(component);
    const height = component.size?.height || 120;
    // 未指定字号时按卡片高度自适应，保证小卡片里数字不溢出
    const valueSize = c.valueFontSize || Math.max(18, Math.round(height * 0.32));

    element.style.cssText = `
      position: absolute;
      left: ${component.position?.x ?? 0}px;
      top: ${component.position?.y ?? 0}px;
      width: ${component.size?.width ?? 200}px;
      height: ${height}px;
      display: flex;
      flex-direction: column;
      align-items: ${c.align === "left" ? "flex-start" : c.align === "right" ? "flex-end" : "center"};
      justify-content: center;
      gap: 4px;
      box-sizing: border-box;
      padding: 10px 14px;
      background: ${c.backgroundColor};
      border: 1px solid ${c.borderColor};
      border-radius: ${c.borderRadius}px;
      overflow: hidden;
    `;

    element.innerHTML = `
      <div style="font-size:13px;color:${c.titleColor};line-height:1.2;">${c.title}</div>
      <div style="display:flex;align-items:baseline;gap:4px;">
        <span class="stat-card-value" style="font-size:${valueSize}px;font-weight:600;color:${c.valueColor};line-height:1.1;">${c.value}</span>
        <span class="stat-card-unit" style="font-size:${Math.max(12, Math.round(valueSize * 0.4))}px;color:${c.titleColor};">${c.unit}</span>
      </div>
      ${c.caption ? `<div style="font-size:12px;color:${c.titleColor};line-height:1.2;">${c.caption}</div>` : ""}
    `;
  }

  createStatCardComponent(component: any, container: HTMLElement): HTMLElement {
    const element = document.createElement("div");
    element.id = component.id;
    element.className = "fuxa-component stat-card-component";
    this.paint(element, component);
    container.appendChild(element);
    this.instances.set(component.id, element);
    return element;
  }

  updateStatCardComponent(componentId: string, properties: any): void {
    const element = this.instances.get(componentId) as HTMLElement;
    if (!element) return;
    // 只重绘内容，不重建元素——重建会丢失选中态与交互绑定
    this.paint(element, {
      id: componentId,
      properties,
      position: {
        x: parseFloat(element.style.left) || 0,
        y: parseFloat(element.style.top) || 0
      },
      size: {
        width: parseFloat(element.style.width) || 200,
        height: parseFloat(element.style.height) || 120
      }
    });
  }

  destroyStatCardComponent(componentId: string): void {
    const element = this.instances.get(componentId);
    if (element) {
      element.remove();
      this.instances.delete(componentId);
    }
  }
}

export const statCardManager = new StatCardComponentManager();
