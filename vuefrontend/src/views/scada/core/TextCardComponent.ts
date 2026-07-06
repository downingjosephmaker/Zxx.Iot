/**
 * 文本卡片组件管理器
 *
 * 功能特性:
 * - 支持多行文本显示
 * - 多行模式配置 (行高倍数、最大行数)
 * - 使用 HTML div 渲染,不使用 SVG
 * - 样式配置通过外观样式面板管理
 *
 * 使用方式: 通过属性编辑器配置,适用于静态文本内容展示
 */

import type { ScadaComponent } from './ComponentManager'

/**
 * 文本卡片配置接口
 */
export interface TextCardConfig {
  // ===== 文本内容 =====
  content: string                  // 文本内容 (支持 \n 换行)
  multiLine: boolean               // 是否启用多行模式
  maxLines: number                 // 最大显示行数 (1-20, 默认 10)
  lineHeight: number               // 行高倍数 (1.0-3.0, 默认 1.6)

  // ===== 卡片样式 (已移到外观样式面板) =====
  backgroundType: 'solid' | 'linear-gradient' | 'radial-gradient' | 'transparent'  // 背景类型
  backgroundColor: string          // 背景颜色 (纯色模式)
  gradientStartColor?: string      // 渐变起始颜色
  gradientEndColor?: string        // 渐变结束颜色
  gradientAngle?: number           // 渐变角度 (0-360度)
  borderStyle: string              // 边框样式 (solid, dashed, dotted, double, none)
  borderColor: string              // 边框颜色
  borderWidth: number              // 边框宽度 (px)
  borderRadius: number             // 圆角半径 (px)

  // 阴影配置
  shadow: {
    enabled: boolean               // 是否启用阴影
    color: string                  // 阴影颜色
    offsetX: number                // X轴偏移 (px)
    offsetY: number                // Y轴偏移 (px)
    blur: number                   // 模糊半径 (px)
  }

  // ===== 文本样式 =====
  textStyle: {
    fontFamily: string             // 字体
    fontSize: number               // 字号 (px)
    fontWeight: 'normal' | 'bold'  // 粗细
    color: string                  // 颜色
    textAlign: 'left' | 'center' | 'right'  // 水平对齐方式
    verticalAlign: 'top' | 'middle' | 'bottom'  // 垂直对齐方式
    textDecoration: 'none' | 'underline' | 'overline' | 'line-through'  // 文字装饰
  }

  // ===== 布局配置 =====
  padding: number                  // 内边距 (px)
}

/**
 * 默认配置
 */
const DEFAULT_TEXT_CARD_CONFIG: TextCardConfig = {
  content: '',
  multiLine: true,
  maxLines: 10,
  lineHeight: 1.6,

  backgroundType: 'solid',
  backgroundColor: '#ffffff',
  gradientStartColor: '#409eff',
  gradientEndColor: '#67c23a',
  gradientAngle: 0,
  borderStyle: 'solid',
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
    textAlign: 'left',
    verticalAlign: 'top',
    textDecoration: 'none'
  },

  padding: 16
}

/**
 * 文本卡片组件管理器类
 */
export class TextCardComponentManager {
  private componentInstances: Map<string, TextCardInstance> = new Map()

  /**
   * 创建文本卡片组件
   */
  createTextCardComponent(
    component: ScadaComponent,
    container: HTMLElement
  ): HTMLElement {
    console.log('📝 TextCardComponentManager.createTextCardComponent 调用:', {
      componentId: component.id,
      componentType: component.type,
      properties: component.properties,
      hasContainer: !!container
    })

    // 创建组件包装容器
    const componentDiv = document.createElement('div')
    componentDiv.id = component.id
    componentDiv.className = 'fuxa-component text-card-component'
    componentDiv.style.cssText = `
      position: absolute;
      left: ${component.position?.x || 0}px;
      top: ${component.position?.y || 0}px;
      width: ${component.size?.width || 300}px;
      height: ${component.size?.height || 200}px;
      cursor: pointer;
      user-select: none;
      overflow: hidden;
    `

    // 创建内容 div
    const contentDiv = document.createElement('div')
    contentDiv.className = 'text-card-content'
    contentDiv.style.cssText = `
      width: 100%;
      height: 100%;
      box-sizing: border-box;
      white-space: pre-wrap;
      word-wrap: break-word;
      overflow: hidden;
    `
    componentDiv.appendChild(contentDiv)

    // 将组件添加到画布
    container.appendChild(componentDiv)

    // 创建组件实例
    const config = component.properties?.textCardConfig || component.properties || {}
    console.log('📝 创建 TextCard 实例，配置:', config)

    const instance = new TextCardInstance(contentDiv, config)
    this.componentInstances.set(component.id, instance)
    console.log('📝 TextCard 实例已注册:', component.id, '当前实例数:', this.componentInstances.size)

    // 初始化显示
    instance.updateDisplay()

    return componentDiv
  }

  /**
   * 更新文本卡片组件
   */
  updateTextCardComponent(componentId: string, properties: any): void {
    console.log('📝 TextCardComponentManager.updateTextCardComponent 调用:', {
      componentId,
      properties,
      hasInstance: this.componentInstances.has(componentId)
    })

    const instance = this.componentInstances.get(componentId)
    if (!instance) {
      console.error('📝 找不到 TextCard 实例:', componentId)
      return
    }

    const config = properties.textCardConfig || properties
    console.log('📝 调用 instance.updateConfig:', config)
    instance.updateConfig(config)
  }

  /**
   * 销毁文本卡片组件
   */
  destroyTextCardComponent(componentId: string): void {
    const instance = this.componentInstances.get(componentId)
    if (instance) {
      instance.destroy()
      this.componentInstances.delete(componentId)
    }
  }

  /**
   * 获取组件实例
   */
  getInstance(componentId: string): TextCardInstance | undefined {
    return this.componentInstances.get(componentId)
  }
}

/**
 * 文本卡片组件实例类
 */
class TextCardInstance {
  private contentDiv: HTMLDivElement
  private config: TextCardConfig

  constructor(contentDiv: HTMLDivElement, config: Partial<TextCardConfig> = {}) {
    this.contentDiv = contentDiv
    this.config = { ...DEFAULT_TEXT_CARD_CONFIG, ...config }

    if (!this.contentDiv) {
      console.error('📝 TextCard: contentDiv 不存在')
    }
  }

  /**
   * 更新显示内容
   */
  updateDisplay(): void {
    console.log('📝 TextCardInstance.updateDisplay 调用:', {
      config: this.config
    })

    if (!this.contentDiv) {
      console.error('📝 contentDiv 不存在，无法更新显示')
      return
    }

    // 应用卡片样式
    this.applyCardStyles()

    // 渲染内容
    this.renderContent()

    console.log('📝 updateDisplay 完成')
  }

  /**
   * 应用卡片样式
   */
  private applyCardStyles(): void {
    const {
      backgroundType, backgroundColor, gradientStartColor, gradientEndColor, gradientAngle,
      borderStyle, borderColor, borderWidth, borderRadius,
      shadow, textStyle, padding, lineHeight
    } = this.config

    // 应用背景 (支持纯色、渐变、透明)
    if (backgroundType === 'linear-gradient') {
      // 线性渐变
      const angle = gradientAngle !== undefined ? gradientAngle : 0
      const startColor = gradientStartColor || '#409eff'
      const endColor = gradientEndColor || '#67c23a'
      this.contentDiv.style.background = `linear-gradient(${angle}deg, ${startColor}, ${endColor})`
    } else if (backgroundType === 'radial-gradient') {
      // 径向渐变
      const startColor = gradientStartColor || '#409eff'
      const endColor = gradientEndColor || '#67c23a'
      this.contentDiv.style.background = `radial-gradient(circle, ${startColor}, ${endColor})`
    } else if (backgroundType === 'transparent') {
      // 透明背景
      this.contentDiv.style.background = 'transparent'
    } else {
      // 纯色背景 (默认)
      this.contentDiv.style.background = backgroundColor || '#ffffff'
    }

    // 应用边框 (支持实线、虚线、点线、双线、无边框)
    const effectiveBorderStyle = borderStyle || 'solid'
    if (effectiveBorderStyle === 'none' || borderWidth === 0) {
      this.contentDiv.style.border = 'none'
    } else {
      this.contentDiv.style.border = `${borderWidth}px ${effectiveBorderStyle} ${borderColor}`
    }

    this.contentDiv.style.borderRadius = `${borderRadius}px`
    this.contentDiv.style.padding = `${padding}px`

    // 应用阴影
    if (shadow.enabled) {
      this.contentDiv.style.boxShadow = `${shadow.offsetX}px ${shadow.offsetY}px ${shadow.blur}px ${shadow.color}`
    } else {
      this.contentDiv.style.boxShadow = 'none'
    }

    // 应用文本样式
    this.contentDiv.style.fontFamily = textStyle.fontFamily
    this.contentDiv.style.fontSize = `${textStyle.fontSize}px`
    this.contentDiv.style.fontWeight = textStyle.fontWeight
    this.contentDiv.style.color = textStyle.color
    this.contentDiv.style.textAlign = textStyle.textAlign
    this.contentDiv.style.lineHeight = String(lineHeight)
    this.contentDiv.style.textDecoration = textStyle.textDecoration

    // 应用垂直对齐
    this.contentDiv.style.display = 'flex'
    this.contentDiv.style.flexDirection = 'column'

    switch (textStyle.verticalAlign) {
      case 'top':
        this.contentDiv.style.justifyContent = 'flex-start'
        break
      case 'bottom':
        this.contentDiv.style.justifyContent = 'flex-end'
        break
      case 'middle':
      default:
        this.contentDiv.style.justifyContent = 'center'
        break
    }

    // 确保文本换行正常显示
    this.contentDiv.style.whiteSpace = 'pre-wrap'
    this.contentDiv.style.wordWrap = 'break-word'
  }

  /**
   * 渲染内容 (多行)
   */
  private renderContent(): void {
    const { content, multiLine, maxLines } = this.config

    // 清空现有内容
    this.contentDiv.innerHTML = ''

    if (!content) {
      return
    }

    // 分割文本为行
    const allLines = content.split('\n')
    const lines = multiLine
      ? allLines.slice(0, maxLines)  // 多行模式: 限制最大行数
      : [allLines.join(' ')]  // 单行模式: 合并所有行

    // 创建文本包装元素 (用于垂直对齐)
    const textWrapper = document.createElement('div')
    textWrapper.style.cssText = `
      white-space: pre-wrap;
      word-wrap: break-word;
      width: 100%;
    `
    textWrapper.textContent = lines.join('\n')

    // 添加到容器
    this.contentDiv.appendChild(textWrapper)
  }

  /**
   * 更新配置
   */
  updateConfig(config: Partial<TextCardConfig>): void {
    console.log('📝 TextCardInstance.updateConfig 调用:', {
      oldConfig: { ...this.config },
      newConfig: config
    })

    // 深度合并阴影配置
    if (config.shadow) {
      this.config.shadow = { ...this.config.shadow, ...config.shadow }
      delete config.shadow
    }

    // 深度合并文本样式
    if (config.textStyle) {
      this.config.textStyle = { ...this.config.textStyle, ...config.textStyle }
      delete config.textStyle
    }

    // 合并其他配置
    Object.assign(this.config, config)

    console.log('📝 配置已合并:', this.config)
    this.updateDisplay()
  }

  /**
   * 销毁组件
   */
  destroy(): void {
    // 清理资源 (如果需要)
    console.log('📝 TextCard 实例销毁')
  }
}

/**
 * 创建文本卡片组件管理器实例
 */
export const textCardManager = new TextCardComponentManager()
