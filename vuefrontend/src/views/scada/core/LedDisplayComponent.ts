/**
 * LED 显示组件管理器
 *
 * 功能特性:
 * - 支持数字、文字、符号显示
 * - 多行文本支持 (自动换行)
 * - 炫酷发光效果
 * - 多种动画: 闪烁、滚动、波浪、打字机
 * - 颜色自定义
 * - 数据绑定支持 (MQTT/API)
 *
 * 使用方式: 通过属性编辑器配置,不依赖 SVG 内部脚本
 */

import type { ScadaComponent } from './ComponentManager'

export interface LedDisplayConfig {
  // 显示内容
  text: string

  // 外观样式
  color: string              // LED 颜色
  backgroundColor: string    // 背景颜色
  fontSize: number           // 字体大小
  fontFamily: string         // 字体
  fontWeight: 'normal' | 'bold'

  // 发光效果
  glowEffect: boolean        // 是否启用发光
  glowIntensity: 'normal' | 'strong'  // 发光强度

  // 布局
  multiLine: boolean         // 多行模式
  lineHeight: number         // 行高倍数
  maxLines: number           // 最大行数
  alignment: 'left' | 'center' | 'right'  // 对齐方式
  padding: number            // 内边距

  // 动画效果
  animation: 'none' | 'blink' | 'scroll' | 'wave' | 'typewriter'
  animationSpeed: number     // 动画速度 (毫秒)

  // 七段数码管
  sevenSegmentMode: boolean  // 七段数码管模式

  // 格式化
  format: 'none' | 'time' | 'date' | 'number' | 'custom'
  customFormat?: string      // 自定义格式化函数
}

/**
 * LED 显示器组件注册表
 */
export const ledDisplayComponents = [
  {
    name: 'led-display',
    title: 'LED 显示屏',
    icon: 'ep:monitor',
    category: '控制',
    defaultProps: {
      type: 'led-display',
      width: 280,
      height: 80,
      properties: {
        text: '88:88:88',
        color: '#ff3333',
        backgroundColor: '#0a0a0a',
        fontSize: 42,
        fontFamily: "'Courier New', monospace",
        fontWeight: 'bold',
        glowEffect: true,
        glowIntensity: 'strong',
        multiLine: false,
        lineHeight: 1.2,
        maxLines: 3,
        alignment: 'left',
        padding: 10,
        animation: 'none',
        animationSpeed: 1000,
        sevenSegmentMode: false,
        format: 'none'
      }
    }
  }
]

/**
 * LED 显示组件管理器类
 */
export class LedDisplayComponentManager {
  private componentInstances: Map<string, LedDisplayInstance> = new Map()

  /**
   * 创建 LED 显示组件
   */
  createLedDisplayComponent(
    component: ScadaComponent,
    container: HTMLElement
  ): HTMLElement {
    console.log('💡 LedDisplayComponentManager.createLedDisplayComponent 调用:', {
      componentId: component.id,
      componentType: component.type,
      properties: component.properties,
      hasContainer: !!container
    })

    const svgElement = container.querySelector('svg') as SVGSVGElement
    if (!svgElement) {
      console.error('💡 LED Display: 找不到 SVG 元素', container)
      return container
    }

    // 支持两种配置方式：
    // 1. component.properties.ledConfig (完整配置对象)
    // 2. component.properties.text, component.properties.fontSize 等 (单独属性)
    const config: Partial<LedDisplayConfig> = component.properties?.ledConfig || component.properties || {}
    console.log('💡 创建 LED 实例，配置:', config)

    const instance = new LedDisplayInstance(svgElement, config)

    this.componentInstances.set(component.id, instance)
    console.log('💡 LED 实例已注册:', component.id, '当前实例数:', this.componentInstances.size)

    // 初始化显示
    instance.updateDisplay()

    return container
  }

  /**
   * 更新 LED 组件
   */
  updateLedDisplayComponent(componentId: string, properties: any): void {
    console.log('💡 LedDisplayComponentManager.updateLedDisplayComponent 调用:', {
      componentId,
      properties,
      hasInstance: this.componentInstances.has(componentId),
      instanceCount: this.componentInstances.size,
      allInstanceIds: Array.from(this.componentInstances.keys())
    })

    const instance = this.componentInstances.get(componentId)
    if (!instance) {
      console.error('💡 找不到 LED 实例:', componentId, '已注册的实例:', Array.from(this.componentInstances.keys()))
      return
    }

    // 支持两种更新方式：
    // 1. properties.ledConfig (完整配置对象)
    // 2. properties 本身就是配置 (单独属性更新)
    const config = properties.ledConfig || properties
    console.log('💡 调用 instance.updateConfig:', config)
    instance.updateConfig(config)
    console.log('💡 LED 实例更新完成')
  }

  /**
   * 销毁 LED 组件
   */
  destroyLedDisplayComponent(componentId: string): void {
    const instance = this.componentInstances.get(componentId)
    if (instance) {
      instance.destroy()
      this.componentInstances.delete(componentId)
    }
  }

  /**
   * 获取组件实例
   */
  getInstance(componentId: string): LedDisplayInstance | undefined {
    return this.componentInstances.get(componentId)
  }
}

/**
 * LED 显示组件实例类
 */
class LedDisplayInstance {
  private svgElement: SVGSVGElement
  private textElement: SVGTextElement
  private containerElement: SVGGElement
  private config: LedDisplayConfig
  private animationTimer: number | null = null
  private animationFrameId: number | null = null

  // 动画状态
  private scrollOffset: number = 0
  private blinkVisible: boolean = true
  private typewriterIndex: number = 0

  constructor(svgElement: SVGSVGElement, config: Partial<LedDisplayConfig> = {}) {
    this.svgElement = svgElement

    // 默认配置
    this.config = {
      text: '88:88:88',
      color: '#ff3333',
      backgroundColor: '#0a0a0a',
      fontSize: 42,
      fontFamily: "'Courier New', monospace",
      fontWeight: 'bold',
      glowEffect: true,
      glowIntensity: 'strong',
      multiLine: false,
      lineHeight: 1.2,
      maxLines: 3,
      alignment: 'left',
      padding: 10,
      animation: 'none',
      animationSpeed: 1000,
      sevenSegmentMode: false,
      format: 'none',
      ...config
    }

    // 获取 SVG 元素引用
    this.containerElement = svgElement.querySelector('#led-container') as SVGGElement
    this.textElement = svgElement.querySelector('#led-text') as SVGTextElement

    if (!this.containerElement || !this.textElement) {
      console.error('LED Display: 找不到必需的 SVG 元素')
    }
  }

  /**
   * 更新显示内容
   */
  updateDisplay(text?: string) {
    console.log('💡 LedDisplayInstance.updateDisplay 调用:', {
      text,
      hasTextElement: !!this.textElement,
      currentText: this.config.text,
      config: this.config
    })

    if (!this.textElement) {
      console.error('💡 textElement 不存在，无法更新显示')
      return
    }

    if (text !== undefined) {
      this.config.text = String(text)
    }

    // ✨ 同步 SVG 尺寸 (根据外层容器的实际尺寸)
    this.syncSvgSize()

    // ✨ 同步 textAlign 到 alignment (在渲染之前)
    const configAny = this.config as any
    if (configAny.textAlign) {
      console.log('💡 同步 textAlign 到 alignment:', configAny.textAlign)
      this.config.alignment = configAny.textAlign
    }

    // 格式化文本
    const formattedText = this.formatText(this.config.text)
    console.log('💡 格式化后的文本:', formattedText, '对齐方式:', this.config.alignment)

    // 停止旧动画
    this.stopAnimation()

    // 根据模式渲染
    if (this.config.sevenSegmentMode) {
      console.log('💡 使用七段数码管模式')
      this.renderSevenSegment(formattedText)
    } else if (this.config.multiLine) {
      console.log('💡 使用多行模式')
      this.renderMultiLine(formattedText)
    } else {
      console.log('💡 使用单行模式')
      this.renderSingleLine(formattedText)
    }

    // 应用样式
    this.applyStyles()

    // 启动动画
    this.startAnimation()

    console.log('💡 updateDisplay 完成')
  }

  /**
   * 渲染单行文本
   */
  private renderSingleLine(text: string) {
    // 清空子元素
    while (this.textElement.firstChild) {
      this.textElement.removeChild(this.textElement.firstChild)
    }

    // 设置文本内容
    this.textElement.textContent = text

    // 设置对齐
    const x = this.getAlignmentX()
    this.textElement.setAttribute('x', String(x))
    this.textElement.setAttribute('y', '0')

    if (this.config.alignment === 'center') {
      this.textElement.setAttribute('text-anchor', 'middle')
    } else if (this.config.alignment === 'right') {
      this.textElement.setAttribute('text-anchor', 'end')
    } else {
      this.textElement.setAttribute('text-anchor', 'start')
    }
  }

  /**
   * 渲染多行文本
   */
  private renderMultiLine(text: string) {
    // 清空子元素
    while (this.textElement.firstChild) {
      this.textElement.removeChild(this.textElement.firstChild)
    }

    // 分割文本
    const lines = text.split('\n').slice(0, this.config.maxLines)
    const lineHeight = this.config.fontSize * this.config.lineHeight
    const x = this.getAlignmentX()

    lines.forEach((line, index) => {
      const tspan = document.createElementNS('http://www.w3.org/2000/svg', 'tspan')
      tspan.textContent = line
      tspan.setAttribute('x', String(x))
      tspan.setAttribute('dy', index === 0 ? '0' : String(lineHeight))

      if (this.config.alignment === 'center') {
        tspan.setAttribute('text-anchor', 'middle')
      } else if (this.config.alignment === 'right') {
        tspan.setAttribute('text-anchor', 'end')
      } else {
        tspan.setAttribute('text-anchor', 'start')
      }

      this.textElement.appendChild(tspan)
    })
  }

  /**
   * 渲染七段数码管
   */
  private renderSevenSegment(text: string) {
    this.renderSingleLine(text)
    // 使用等宽字体模拟数码管效果
    this.textElement.setAttribute('font-family', "'Digital-7', 'Orbitron', monospace")
    this.textElement.setAttribute('letter-spacing', '0.1em')
  }

  /**
   * 应用样式
   */
  private applyStyles() {
    // 字体样式
    this.textElement.setAttribute('fill', this.config.color)
    this.textElement.setAttribute('font-size', String(this.config.fontSize))
    this.textElement.setAttribute('font-family', this.config.fontFamily)
    this.textElement.setAttribute('font-weight', this.config.fontWeight)

    // 通用文字属性支持
    const configAny = this.config as any

    // 文字装饰 (textDecoration)
    if (configAny.textDecoration) {
      this.textElement.setAttribute('text-decoration', configAny.textDecoration)
    }

    // ✨ 垂直对齐 (verticalAlign) - 通过 dominant-baseline 实现
    if (configAny.verticalAlign) {
      console.log('💡 应用 verticalAlign:', configAny.verticalAlign)
      // 根据用户反馈,需要反转映射
      const baselineMap: Record<string, string> = {
        'top': 'auto',                 // 顶部对齐 - 使用 auto 让文本显示在顶部
        'middle': 'middle',            // 居中对齐
        'bottom': 'hanging',           // 底部对齐 - 使用 hanging 让文本显示在底部
        'baseline': 'auto'             // 基线对齐 - 使用 auto
      }
      const svgBaseline = baselineMap[configAny.verticalAlign] || 'middle'
      console.log('💡 映射到 dominant-baseline:', svgBaseline)
      this.textElement.setAttribute('dominant-baseline', svgBaseline)
    }

    // 字母间距 (letterSpacing)
    if (configAny.letterSpacing !== undefined) {
      this.textElement.setAttribute('letter-spacing', String(configAny.letterSpacing))
    }

    // 发光效果
    if (this.config.glowEffect) {
      const filter = this.config.glowIntensity === 'strong'
        ? 'url(#led-glow-strong)'
        : 'url(#led-glow)'
      this.textElement.setAttribute('filter', filter)
    } else {
      this.textElement.removeAttribute('filter')
    }

    // 背景色
    const bgRect = this.svgElement.querySelector('rect')
    if (bgRect && !this.config.backgroundColor.startsWith('url(')) {
      bgRect.setAttribute('fill', this.config.backgroundColor)
    }
  }

  /**
   * 获取对齐位置 X 坐标
   */
  private getAlignmentX(): number {
    const svgWidth = parseInt(this.svgElement.getAttribute('viewBox')?.split(' ')[2] || '280')
    const usableWidth = svgWidth - this.config.padding * 2

    if (this.config.alignment === 'center') {
      return usableWidth / 2
    } else if (this.config.alignment === 'right') {
      return usableWidth
    }
    return 0
  }

  /**
   * 格式化文本
   */
  private formatText(text: string): string {
    switch (this.config.format) {
      case 'time':
        return this.formatTime(text)
      case 'date':
        return this.formatDate(text)
      case 'number':
        return this.formatNumber(text)
      case 'custom':
        return this.formatCustom(text)
      default:
        return text
    }
  }

  private formatTime(value: string): string {
    try {
      const date = value ? new Date(value) : new Date()
      const hours = String(date.getHours()).padStart(2, '0')
      const minutes = String(date.getMinutes()).padStart(2, '0')
      const seconds = String(date.getSeconds()).padStart(2, '0')
      return `${hours}:${minutes}:${seconds}`
    } catch {
      return value
    }
  }

  private formatDate(value: string): string {
    try {
      const date = value ? new Date(value) : new Date()
      const year = date.getFullYear()
      const month = String(date.getMonth() + 1).padStart(2, '0')
      const day = String(date.getDate()).padStart(2, '0')
      return `${year}-${month}-${day}`
    } catch {
      return value
    }
  }

  private formatNumber(value: string): string {
    const num = parseFloat(value)
    return isNaN(num) ? value : num.toLocaleString()
  }

  private formatCustom(value: string): string {
    if (!this.config.customFormat) return value
    try {
      const fn = new Function('value', `return ${this.config.customFormat}`)
      return String(fn(value))
    } catch (error) {
      console.error('LED Display: 自定义格式化函数错误', error)
      return value
    }
  }

  /**
   * 启动动画
   */
  private startAnimation() {
    switch (this.config.animation) {
      case 'blink':
        this.startBlinkAnimation()
        break
      case 'scroll':
        this.startScrollAnimation()
        break
      case 'wave':
        this.startWaveAnimation()
        break
      case 'typewriter':
        this.startTypewriterAnimation()
        break
    }
  }

  /**
   * 停止动画
   */
  private stopAnimation() {
    if (this.animationTimer) {
      clearInterval(this.animationTimer)
      this.animationTimer = null
    }

    if (this.animationFrameId) {
      cancelAnimationFrame(this.animationFrameId)
      this.animationFrameId = null
    }

    this.textElement.style.opacity = '1'
    this.textElement.removeAttribute('transform')
  }

  private startBlinkAnimation() {
    this.animationTimer = window.setInterval(() => {
      this.blinkVisible = !this.blinkVisible
      this.textElement.style.opacity = this.blinkVisible ? '1' : '0.2'
    }, this.config.animationSpeed)
  }

  private startScrollAnimation() {
    const speed = 100 / this.config.animationSpeed
    const svgWidth = parseInt(this.svgElement.getAttribute('viewBox')?.split(' ')[2] || '280')

    const scroll = () => {
      this.scrollOffset -= speed
      this.textElement.setAttribute('transform', `translate(${this.scrollOffset}, 0)`)

      const bbox = this.textElement.getBBox()
      if (this.scrollOffset < -bbox.width - 50) {
        this.scrollOffset = svgWidth
      }

      this.animationFrameId = requestAnimationFrame(scroll)
    }

    scroll()
  }

  private startWaveAnimation() {
    let time = 0
    const speed = 0.001 * (100 / this.config.animationSpeed)

    const wave = () => {
      time += speed

      if (this.config.multiLine) {
        const tspans = this.textElement.querySelectorAll('tspan')
        tspans.forEach((tspan, i) => {
          const offset = Math.sin(time + i * 0.5) * 5
          const currentDy = parseFloat(tspan.getAttribute('dy') || '0')
          tspan.setAttribute('dy', String(currentDy + offset))
        })
      } else {
        const offset = Math.sin(time) * 5
        this.textElement.setAttribute('transform', `translate(0, ${offset})`)
      }

      this.animationFrameId = requestAnimationFrame(wave)
    }

    wave()
  }

  private startTypewriterAnimation() {
    const fullText = this.config.text
    this.typewriterIndex = 0

    const type = () => {
      if (this.typewriterIndex <= fullText.length) {
        this.renderSingleLine(fullText.substring(0, this.typewriterIndex))
        this.typewriterIndex++
        this.animationTimer = window.setTimeout(type, this.config.animationSpeed / 10)
      } else {
        this.animationTimer = window.setTimeout(() => {
          this.typewriterIndex = 0
          type()
        }, this.config.animationSpeed * 2)
      }
    }

    type()
  }

  /**
   * 同步 SVG 尺寸
   * 根据外层容器的实际宽高,动态更新 SVG 的 viewBox 和背景 rect
   */
  private syncSvgSize() {
    // 从外层容器元素获取实际尺寸,而不是从SVG属性获取
    const container = this.svgElement.parentElement
    let width = 280
    let height = 80
    
    if (container) {
      // 使用 offsetWidth 和 offsetHeight 获取容器的实际渲染尺寸
      width = container.offsetWidth || 280
      height = container.offsetHeight || 80
    }

    console.log('💡 同步 SVG 尺寸:', { 
      width, 
      height, 
      hasContainer: !!container,
      containerWidth: container?.offsetWidth,
      containerHeight: container?.offsetHeight
    })

    // 更新 SVG 的 width 和 height 属性
    this.svgElement.setAttribute('width', String(width))
    this.svgElement.setAttribute('height', String(height))

    // 更新 viewBox
    this.svgElement.setAttribute('viewBox', `0 0 ${width} ${height}`)

    // 更新背景 rect
    const bgRect = this.svgElement.querySelector('rect') as SVGRectElement
    if (bgRect) {
      bgRect.setAttribute('width', String(width))
      bgRect.setAttribute('height', String(height))
      console.log('💡 更新背景 rect 尺寸:', { width, height })
    }

    // 更新 LED 容器的垂直居中位置
    if (this.containerElement) {
      const centerY = height / 2
      // 提取当前的 x 偏移量,保持不变
      const transform = this.containerElement.getAttribute('transform') || 'translate(10, 40)'
      const xMatch = transform.match(/translate\((\d+)/)
      const x = xMatch ? xMatch[1] : '10'
      this.containerElement.setAttribute('transform', `translate(${x}, ${centerY})`)
      console.log('💡 更新 LED 容器位置:', `translate(${x}, ${centerY})`)
    }
  }

  /**
   * 更新配置
   */
  updateConfig(config: Partial<LedDisplayConfig>) {
    console.log('💡 LedDisplayInstance.updateConfig 调用:', {
      oldConfig: {...this.config},
      newConfig: config
    })
    Object.assign(this.config, config)
    console.log('💡 配置已合并:', this.config)
    this.updateDisplay()
  }

  /**
   * 设置文本
   */
  setText(text: string) {
    this.updateDisplay(text)
  }

  /**
   * 设置颜色
   */
  setColor(color: string) {
    this.config.color = color
    this.applyStyles()
  }

  /**
   * 数据绑定接口 (供 MQTT/API 调用)
   */
  putValue(value: any) {
    this.updateDisplay(String(value))
  }

  /**
   * 获取当前值
   */
  getValue(): string {
    return this.config.text
  }

  /**
   * 销毁组件
   */
  destroy() {
    this.stopAnimation()
  }
}

/**
 * 创建 LED 显示组件管理器实例
 */
export const ledDisplayManager = new LedDisplayComponentManager()
