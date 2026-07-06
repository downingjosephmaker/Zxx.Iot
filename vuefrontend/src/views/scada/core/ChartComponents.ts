/**
 * 图表组件管理器
 * 提供ECharts图表组件的创建、更新和管理功能
 */

export interface ChartComponentConfig {
  name: string
  label: string
  type: 'line' | 'bar' | 'pie' | 'gauge' | 'radar'
  icon?: string
  defaultWidth?: number
  defaultHeight?: number
}

/**
 * 图表组件定义列表
 */
export const chartComponents: ChartComponentConfig[] = [
  {
    name: 'chart-line',
    label: '折线图',
    type: 'line',
    defaultWidth: 400,
    defaultHeight: 300
  },
  {
    name: 'chart-bar',
    label: '柱状图',
    type: 'bar',
    defaultWidth: 400,
    defaultHeight: 300
  },
  {
    name: 'chart-pie',
    label: '饼图',
    type: 'pie',
    defaultWidth: 400,
    defaultHeight: 300
  },
  {
    name: 'chart-gauge',
    label: '仪表盘',
    type: 'gauge',
    defaultWidth: 300,
    defaultHeight: 300
  },
  {
    name: 'chart-radar',
    label: '雷达图',
    type: 'radar',
    defaultWidth: 400,
    defaultHeight: 400
  }
]

/**
 * 图表组件管理器类
 * 负责创建、更新和销毁ECharts图表组件实例
 */
export class ChartComponentManager {
  private chartInstances: Map<string, any> = new Map()
  private updateIntervals: Map<string, number> = new Map()

  /**
   * 初始化管理器
   */
  initialize(container?: HTMLElement) {
    console.log('ChartComponentManager initialized')
  }

  /**
   * 创建图表组件
   */
  createChartComponent(component: any, container: HTMLElement): HTMLElement {
    const chartDiv = document.createElement('div')
    chartDiv.className = 'chart-component'
    chartDiv.style.width = `${component.size?.width || 400}px`
    chartDiv.style.height = `${component.size?.height || 300}px`
    chartDiv.style.background = '#ffffff'
    chartDiv.style.border = '1px solid #e4e7ed'
    chartDiv.style.borderRadius = '4px'
    chartDiv.style.display = 'flex'
    chartDiv.style.alignItems = 'center'
    chartDiv.style.justifyContent = 'center'
    chartDiv.style.color = '#909399'
    chartDiv.style.fontSize = '14px'

    chartDiv.textContent = '图表组件（需要配置数据源）'

    container.appendChild(chartDiv)
    this.chartInstances.set(component.id, chartDiv)

    return chartDiv
  }

  /**
   * 更新图表组件
   */
  updateChartComponent(componentId: string, properties: any) {
    const chartElement = this.chartInstances.get(componentId)
    if (!chartElement) return

    // 更新尺寸
    if (properties.size) {
      chartElement.style.width = `${properties.size.width}px`
      chartElement.style.height = `${properties.size.height}px`
    }
  }

  /**
   * 销毁图表组件
   */
  destroyChartComponent(componentId: string) {
    // 清除更新定时器
    const intervalId = this.updateIntervals.get(componentId)
    if (intervalId) {
      clearInterval(intervalId)
      this.updateIntervals.delete(componentId)
    }

    // 移除图表实例
    this.chartInstances.delete(componentId)
  }

  /**
   * 开始实时数据更新
   */
  startRealTimeUpdates() {
    console.log('Chart real-time updates started')
  }

  /**
   * 停止实时数据更新
   */
  stopRealTimeUpdates() {
    this.updateIntervals.forEach((intervalId) => {
      clearInterval(intervalId)
    })
    this.updateIntervals.clear()
    console.log('Chart real-time updates stopped')
  }

  /**
   * 图表数据更新回调
   */
  onChartDataUpdate?: (componentId: string, data: any) => void

  /**
   * 清理所有资源
   */
  destroy() {
    this.stopRealTimeUpdates()
    this.chartInstances.clear()
  }
}
