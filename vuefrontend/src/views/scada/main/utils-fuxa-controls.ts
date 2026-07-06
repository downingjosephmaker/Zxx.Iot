/**
 * FUXA 风格控制组件实现
 * 参考 FUXA 源码: I:/ProjectSynthesis/EnergyPlatform/FUXA/client/src/app/gauges/controls
 *
 * 包含以下组件:
 * - Slider: noUiSlider 风格滑块 (svg-ext-html_slider)
 * - GaugeProgress: 垂直进度条 (svg-ext-gauge_progress / progress-v)
 * - GaugeSemaphore: 信号灯/指示灯 (svg-ext-gauge_semaphore / semaphore)
 * - HtmlBag: 仪表盘 (svg-ext-html_bag / bag)
 */

const parseNumericValue = (value: any): number => {
  if (typeof value === 'number') {
    return Number.isFinite(value) ? value : 0;
  }
  if (typeof value === 'string') {
    const numeric = parseFloat(value.replace(/[^0-9+\-.,]/g, ''));
    return Number.isNaN(numeric) ? 0 : numeric;
  }
  return 0;
};

const clampGaugeValue = (value: number, min: number, max: number): number => {
  if (!Number.isFinite(value)) {
    return min;
  }
  if (min === max) {
    return min;
  }
  return Math.min(Math.max(value, min), max);
};

const formatGaugeDisplay = (value: number, decimals: number, unit: string): string => {
  const fixedValue = Number.isFinite(value) ? value.toFixed(Math.max(decimals, 0)) : '0';
  return unit ? `${fixedValue}${unit}` : fixedValue;
};

const isSolidColor = (color: string | null | undefined): boolean => {
  if (!color) return false;
  const normalized = color.trim().toLowerCase();
  return normalized.startsWith('#') || normalized.startsWith('rgb') || normalized.startsWith('hsl');
};

// ========================================
// Slider 组件 - FUXA noUiSlider 风格 (更新版)
// ========================================

/**
 * 初始化 Slider 组件的默认属性 - 参考 NgxNouisliderOptions
 */
export const initializeFuxaSliderComponent = (componentInstance: any) => {
  componentInstance.properties = {
    variableId: '',
    value: 50,
    // noUiSlider 配置选项
    options: {
      orientation: 'vertical',    // vertical 或 horizontal
      direction: 'ltr',            // ltr 或 rtl
      fontFamily: 'Sans-serif',
      // 形状配置
      shape: {
        baseColor: '#cdcdcd',      // 轨道背景色
        connectColor: '#262c3b',   // 填充色
        handleColor: '#3f4964'     // 手柄颜色
      },
      // 刻度配置
      marker: {
        color: '#333333',          // 刻度颜色
        subWidth: 5,               // 次刻度宽度
        subHeight: 1,              // 次刻度高度
        fontSize: 14,              // 刻度字体大小
        divHeight: 1,              // 主刻度高度（更细）
        divWidth: 8                // 主刻度宽度（更短）
      },
      // 范围配置
      range: {
        min: 0,
        max: 100
      },
      step: 1,                     // 步长
      // 刻度值配置
      pips: {
        mode: 'values',
        values: [0, 25, 50, 75, 100],  // 默认5个刻度
        density: 4
      },
      // 提示框配置
      tooltip: {
        type: 'none',              // none, hide, show
        decimals: 0,
        background: '#FFF',
        color: '#000',
        fontSize: 12
      }
    }
  };

  componentInstance.style = {
    backgroundColor: 'transparent'
  };

  console.log('🎚️ 初始化 FUXA Slider 组件');
};

/**
 * 创建 FUXA Slider 组件 DOM 元素
 */
export const createFuxaSliderElement = (
  component: any,
  canvasContent: Element,
  setupComponentInteractions: (element: HTMLElement, component: any) => void
) => {
  const props = component.properties || {};
  const opts = props.options || {};
  const isVertical = opts.orientation === 'vertical';

  const container = document.createElement('div');
  container.id = component.id;
  container.className = 'fuxa-component thermometer-component';
  container.style.cssText = `
    position: absolute;
    left: ${component.position.x}px;
    top: ${component.position.y}px;
    width: ${component.size.width}px;
    height: ${component.size.height}px;
    font-family: ${opts.fontFamily || 'Sans-serif'};
  `;

  // 创建滑块容器
  const sliderPanel = document.createElement('div');
  sliderPanel.id = `D-SLI_${component.id}`;
  sliderPanel.className = 'noUi-target noUi-ltr ' + (isVertical ? 'noUi-vertical' : 'noUi-horizontal');
  sliderPanel.style.cssText = `
    width: 100%;
    height: 100%;
    position: relative;
    background: transparent;
  `;

  // 创建滑块基础结构
  const baseDiv = document.createElement('div');
  baseDiv.className = 'noUi-base';
  baseDiv.style.cssText = `
    width: 100%;
    height: 100%;
    position: relative;
    padding: ${isVertical ? '15px 0' : '0 15px'};
  `;

  // 创建连接层(轨道) - FUXA风格：更细、扁平
  const connectsDiv = document.createElement('div');
  connectsDiv.className = 'noUi-connects';
  const trackSize = isVertical ? '8px' : '8px';  // 细轨道
  connectsDiv.style.cssText = isVertical ? `
    position: absolute;
    left: 25%;
    top: 15px;
    bottom: 15px;
    width: ${trackSize};
    height: calc(100% - 30px);
    transform: translateX(-50%);
    background: ${opts.shape?.baseColor || '#CDCDCD'};
    border-radius: 4px;
  ` : `
    position: absolute;
    top: 30%;
    left: 15px;
    right: 15px;
    width: calc(100% - 30px);
    height: ${trackSize};
    transform: translateY(-50%);
    background: ${opts.shape?.baseColor || '#CDCDCD'};
    border-radius: 4px;
  `;

  // 创建填充部分 - FUXA风格：扁平、纯色
  const connectDiv = document.createElement('div');
  connectDiv.className = 'noUi-connect';
  const value = props.value || 50;
  const min = opts.range?.min || 0;
  const max = opts.range?.max || 100;
  const percent = ((value - min) / (max - min)) * 100;

  const connectColor = opts.shape?.connectColor || '#262C3B';
  connectDiv.style.cssText = isVertical ? `
    position: absolute;
    bottom: 0;
    left: 0;
    width: 100%;
    height: ${percent}%;
    background: ${connectColor};
    border-radius: 4px;
    transition: height 0.2s ease;
  ` : `
    position: absolute;
    left: 0;
    top: 0;
    width: ${percent}%;
    height: 100%;
    background: ${connectColor};
    border-radius: 4px;
    transition: width 0.2s ease;
  `;

  connectsDiv.appendChild(connectDiv);

  // 创建刻度层
  if (opts.pips && opts.pips.values && opts.pips.values.length > 0) {
    const pipsDiv = document.createElement('div');
    pipsDiv.className = 'noUi-pips noUi-pips-' + opts.orientation;
    pipsDiv.style.cssText = `
      position: absolute;
      left: 0;
      top: 0;
      width: 100%;
      height: 100%;
      pointer-events: none;
    `;

    opts.pips.values.forEach((pipValue: number) => {
      const pipPercent = ((pipValue - min) / (max - min)) * 100;

      // 创建刻度线
      const markerDiv = document.createElement('div');
      markerDiv.className = 'noUi-marker noUi-marker-' + opts.orientation;

      if (isVertical) {
        // 垂直模式：刻度放在轨道右侧
        // 轨道定位：top: 15px, height: calc(100% - 30px)
        // 轨道实际范围：从 15px (顶部) 到 calc(100% - 15px) (底部)
        // pipPercent: 0% -> 底部, 100% -> 顶部
        // 公式：top = 15px + (100% - 30px) * (100 - pipPercent) / 100
        markerDiv.style.cssText = `
          position: absolute;
          left: 30%;
          top: calc(15px + (100% - 30px) * ${(100 - pipPercent) / 100});
          width: ${opts.marker?.divWidth || 8}px;
          height: ${opts.marker?.divHeight || 1}px;
          background: ${opts.marker?.color || '#333333'};
          transform: translateX(10px);
          border-radius: 0.5px;
        `;
      } else {
        // 水平模式：刻度放在轨道下方
        // 轨道定位：top: 30%, left: 15px, width: calc(100% - 30px)
        // 公式：left = 15px + (100% - 30px) * pipPercent / 100
        markerDiv.style.cssText = `
          position: absolute;
          top: 30%;
          left: calc(15px + (100% - 30px) * ${pipPercent / 100});
          width: ${opts.marker?.divHeight || 1}px;
          height: ${opts.marker?.divWidth || 8}px;
          background: ${opts.marker?.color || '#333333'};
          transform: translateY(10px);
          border-radius: 0.5px;
        `;
      }

      pipsDiv.appendChild(markerDiv);

      // 创建刻度值标签
      const valueDiv = document.createElement('div');
      valueDiv.className = 'noUi-value noUi-value-' + opts.orientation;
      valueDiv.textContent = pipValue.toString();

      if (isVertical) {
        // 垂直模式：标签放在刻度线右侧
        // 与刻度线使用相同的定位公式
        valueDiv.style.cssText = `
          position: absolute;
          left: 30%;
          top: calc(15px + (100% - 30px) * ${(100 - pipPercent) / 100});
          transform: translateX(${(opts.marker?.divWidth || 8) + 12}px) translateY(-50%);
          font-size: ${opts.marker?.fontSize || 14}px;
          font-weight: 400;
          color: ${opts.marker?.color || '#333333'};
          white-space: nowrap;
          font-family: ${opts.fontFamily || 'Sans-serif'};
        `;
      } else {
        // 水平模式：标签放在刻度线下方
        // 与刻度线使用相同的定位公式
        valueDiv.style.cssText = `
          position: absolute;
          top: 30%;
          left: calc(15px + (100% - 30px) * ${pipPercent / 100});
          transform: translateY(${(opts.marker?.divWidth || 8) + 12}px) translateX(-50%);
          font-size: ${opts.marker?.fontSize || 14}px;
          font-weight: 400;
          color: ${opts.marker?.color || '#333333'};
          white-space: nowrap;
          font-family: ${opts.fontFamily || 'Sans-serif'};
        `;
      }

      pipsDiv.appendChild(valueDiv);
    });

    baseDiv.appendChild(pipsDiv);
  }

  // 创建手柄
  const originDiv = document.createElement('div');
  originDiv.className = 'noUi-origin';

  if (isVertical) {
    originDiv.style.cssText = `
      position: absolute;
      left: 25%;
      top: ${100 - percent}%;
      transform: translate(-50%, -50%);
      z-index: 2;
    `;
  } else {
    originDiv.style.cssText = `
      position: absolute;
      top: 30%;
      left: ${percent}%;
      transform: translate(-50%, -50%);
      z-index: 2;
    `;
  }

  const handleDiv = document.createElement('div');
  handleDiv.className = 'noUi-handle noUi-handle-lower';
  const handleColor = opts.shape?.handleColor || '#3F4964';
  // FUXA风格：圆角矩形手柄，垂直时高、水平时宽
  handleDiv.style.cssText = isVertical ? `
    width: 26px;
    height: 30px;
    background: ${handleColor};
    border-radius: 8px;
    cursor: grab;
    box-shadow: 0 2px 8px rgba(0,0,0,0.28);
    position: relative;
  ` : `
    width: 30px;
    height: 26px;
    background: ${handleColor};
    border-radius: 8px;
    cursor: grab;
    box-shadow: 0 2px 8px rgba(0,0,0,0.28);
    position: relative;
  `;

  // 添加提示框 - 美化样式
  if (opts.tooltip && opts.tooltip.type === 'show') {
    const tooltipDiv = document.createElement('div');
    tooltipDiv.className = 'noUi-tooltip';
    tooltipDiv.textContent = value.toFixed(opts.tooltip.decimals || 0);
    tooltipDiv.style.cssText = `
      position: absolute;
      bottom: calc(100% + 8px);
      left: 50%;
      transform: translateX(-50%);
      background: ${opts.tooltip.background || '#333333'};
      color: ${opts.tooltip.color || '#FFFFFF'};
      font-size: ${opts.tooltip.fontSize || 11}px;
      font-weight: 600;
      padding: 4px 10px;
      border-radius: 6px;
      box-shadow: 0 3px 8px rgba(0,0,0,0.3);
      white-space: nowrap;
      pointer-events: none;
      font-family: ${opts.fontFamily || 'Sans-serif'};
    `;

    // 添加小三角形箭头
    const arrow = document.createElement('div');
    arrow.style.cssText = `
      position: absolute;
      top: 100%;
      left: 50%;
      transform: translateX(-50%);
      width: 0;
      height: 0;
      border-left: 5px solid transparent;
      border-right: 5px solid transparent;
      border-top: 5px solid ${opts.tooltip.background || '#333333'};
    `;
    tooltipDiv.appendChild(arrow);
    handleDiv.appendChild(tooltipDiv);
  }

  originDiv.appendChild(handleDiv);

  // 组装结构
  baseDiv.appendChild(connectsDiv);
  baseDiv.appendChild(originDiv);
  sliderPanel.appendChild(baseDiv);
  container.appendChild(sliderPanel);

  // 添加拖拽交互
  let isDragging = false;
  let startPos = 0;
  let startValue = value;

  const onMouseDown = (e: MouseEvent) => {
    isDragging = true;
    startPos = isVertical ? e.clientY : e.clientX;
    startValue = props.value || 50;
    handleDiv.style.cursor = 'grabbing';
    e.preventDefault();
    e.stopPropagation();
  };

  const onMouseMove = (e: MouseEvent) => {
    if (!isDragging) return;

    const currentPos = isVertical ? e.clientY : e.clientX;
    const delta = currentPos - startPos;
    const containerSize = isVertical ? component.size.height : component.size.width;
    const valueRange = max - min;
    const deltaValue = isVertical ? -(delta / containerSize) * valueRange : (delta / containerSize) * valueRange;

    let newValue = startValue + deltaValue;
    newValue = Math.max(min, Math.min(max, newValue));

    // 按步长对齐
    const step = opts.step || 1;
    newValue = Math.round(newValue / step) * step;

    component.properties.value = newValue;

    // 更新 UI
    const newPercent = ((newValue - min) / (max - min)) * 100;

    if (isVertical) {
      originDiv.style.top = `${100 - newPercent}%`;
      connectDiv.style.height = `${newPercent}%`;
    } else {
      originDiv.style.left = `${newPercent}%`;
      connectDiv.style.width = `${newPercent}%`;
    }

    // 更新提示框
    const tooltip = handleDiv.querySelector('.noUi-tooltip') as HTMLElement;
    if (tooltip) {
      tooltip.textContent = newValue.toFixed(opts.tooltip?.decimals || 0);
    }

    e.preventDefault();
  };

  const onMouseUp = () => {
    if (!isDragging) return;
    isDragging = false;
    handleDiv.style.cursor = 'grab';
  };

  handleDiv.addEventListener('mousedown', onMouseDown);
  document.addEventListener('mousemove', onMouseMove);
  document.addEventListener('mouseup', onMouseUp);

  // 点击轨道直接跳转
  connectsDiv.addEventListener('click', (e: MouseEvent) => {
    e.stopPropagation();
    const rect = connectsDiv.getBoundingClientRect();

    let clickPercent;
    if (isVertical) {
      const clickY = e.clientY - rect.top;
      clickPercent = 100 - (clickY / rect.height) * 100;
    } else {
      const clickX = e.clientX - rect.left;
      clickPercent = (clickX / rect.width) * 100;
    }

    let newValue = min + (clickPercent / 100) * (max - min);
    const step = opts.step || 1;
    newValue = Math.round(newValue / step) * step;
    newValue = Math.max(min, Math.min(max, newValue));

    component.properties.value = newValue;
    const newPercent = ((newValue - min) / (max - min)) * 100;

    if (isVertical) {
      originDiv.style.top = `${100 - newPercent}%`;
      connectDiv.style.height = `${newPercent}%`;
    } else {
      originDiv.style.left = `${newPercent}%`;
      connectDiv.style.width = `${newPercent}%`;
    }

    const tooltip = handleDiv.querySelector('.noUi-tooltip') as HTMLElement;
    if (tooltip) {
      tooltip.textContent = newValue.toFixed(opts.tooltip?.decimals || 0);
    }
  });

  setupComponentInteractions(container, component);
  canvasContent.appendChild(container);

  return container;
};

/**
 * 更新 FUXA Slider 组件外观
 */
export const updateFuxaSliderAppearance = (component: any, element: HTMLElement) => {
  if (!element) return;

  const props = component.properties || {};
  const opts = props.options || {};
  const isVertical = opts.orientation === 'vertical';

  element.style.left = `${component.position.x}px`;
  element.style.top = `${component.position.y}px`;
  element.style.width = `${component.size.width}px`;
  element.style.height = `${component.size.height}px`;

  const connectsDiv = element.querySelector('.noUi-connects') as HTMLElement;
  const connectDiv = element.querySelector('.noUi-connect') as HTMLElement;
  const originDiv = element.querySelector('.noUi-origin') as HTMLElement;
  const handleDiv = element.querySelector('.noUi-handle') as HTMLElement;

  if (!connectsDiv || !connectDiv || !originDiv || !handleDiv) return;

  // 更新颜色
  connectsDiv.style.background = opts.shape?.baseColor || '#cdcdcd';
  connectDiv.style.background = opts.shape?.connectColor || '#262c3b';
  handleDiv.style.background = opts.shape?.handleColor || '#3f4964';

  // 更新值和位置
  const value = props.value || 50;
  const min = opts.range?.min || 0;
  const max = opts.range?.max || 100;
  const percent = ((value - min) / (max - min)) * 100;

  if (isVertical) {
    originDiv.style.top = `${100 - percent}%`;
    connectDiv.style.height = `${percent}%`;
  } else {
    originDiv.style.left = `${percent}%`;
    connectDiv.style.width = `${percent}%`;
  }

  // 更新提示框
  const tooltip = handleDiv.querySelector('.noUi-tooltip') as HTMLElement;
  if (tooltip) {
    tooltip.textContent = value.toFixed(opts.tooltip?.decimals || 0);
  }
};

// ========================================
// GaugeProgress 组件 - 垂直进度条 (progress-v)
// ========================================
// 参考 FUXA: 简单的 SVG 进度条,上白下蓝

/**
 * 初始化 GaugeProgress 组件的默认属性
 */
export const initializeGaugeProgressComponent = (componentInstance: any) => {
  const defaultRanges = [
    {
      min: 0,
      max: 100,
      color: '#3B82F6',   // 主填充渐变基色
      stroke: '#94A3B8',
      text: '',
      style: [true, true] // [显示min/max标签, 显示value标签]
    }
  ];

  componentInstance.properties = {
    ...componentInstance.properties,
    variableId: componentInstance.properties?.variableId || '',
    value: componentInstance.properties?.value ?? 68,
    min: componentInstance.properties?.min ?? defaultRanges[0].min,
    max: componentInstance.properties?.max ?? defaultRanges[0].max,
    unit: componentInstance.properties?.unit ?? '%',
    label: componentInstance.properties?.label ?? 'TARGET',
    decimals: componentInstance.properties?.decimals ?? 0,
    ranges: componentInstance.properties?.ranges?.length
      ? componentInstance.properties.ranges
      : defaultRanges
  };

  componentInstance.style = {
    ...componentInstance.style,
    backgroundColor: componentInstance.style?.backgroundColor || 'default', // default 表示使用渐变轨道
    borderColor: componentInstance.style?.borderColor || '#CBD5F5',
    borderWidth: componentInstance.style?.borderWidth ?? 0.18,
    labelColor: componentInstance.style?.labelColor || '#1E293B',
    showLabel: componentInstance.style?.showLabel ?? true,
    // 🎯 初始化动画静态值为 SVG 默认值 30%
    animationStaticValue: componentInstance.style?.animationStaticValue ?? 30,
    svgAnimation: componentInstance.style?.svgAnimation || 'none'
  };

  console.log('📊 初始化 GaugeProgress 组件 (SVG)，animationStaticValue:', componentInstance.style.animationStaticValue);
};

/**
 * 创建 GaugeProgress 组件 DOM 元素 - SVG 实现
 * FUXA 风格: 简单的上白下蓝进度条
 */
export const createGaugeProgressElement = (
  component: any,
  canvasContent: Element,
  setupComponentInteractions: (element: HTMLElement, component: any) => void
) => {
  const props = component.properties || {};
  const firstRange = props.ranges?.[0] || { min: 0, max: 100, color: '#3B82F6', style: [true, true] };

  const min = typeof props.min === 'number' ? props.min : firstRange.min ?? 0;
  const max = typeof props.max === 'number' ? props.max : firstRange.max ?? 100;
  const decimals = typeof props.decimals === 'number' ? props.decimals : 0;
  const unit = typeof props.unit === 'string' ? props.unit : '%';

  const parsedValue = parseNumericValue(props.value);
  const clampedValue = clampGaugeValue(parsedValue, min, max);

  // 主容器
  const container = document.createElement('div');
  container.id = component.id;
  container.className = 'fuxa-component gaugeprogress-component';
  container.style.cssText = `
    position: absolute;
    left: ${component.position.x}px;
    top: ${component.position.y}px;
    width: ${component.size.width}px;
    height: ${component.size.height}px;
  `;
  container.dataset.gaugeMin = min.toString();
  container.dataset.gaugeMax = max.toString();
  container.dataset.gaugeUnit = unit;
  container.dataset.gaugeDecimals = decimals.toString();
  container.dataset.componentType = component.type;
  container.dataset.gaugeValue = clampedValue.toString();
  (container as any).__componentRef = component;

  // 创建 SVG 元素
  const svgNS = 'http://www.w3.org/2000/svg';
  const svg = document.createElementNS(svgNS, 'svg');
  svg.setAttribute('width', '100%');
  svg.setAttribute('height', '100%');
  svg.setAttribute('viewBox', '0 0 21 15');
  svg.setAttribute('preserveAspectRatio', 'none');
  svg.style.cssText = 'display: block;';

  // 定义渐变与阴影
  const gradientSuffix = component.id.replace(/[^a-zA-Z0-9_-]/g, '');
  const trackGradientId = `gauge-track-${gradientSuffix}`;
  const fillGradientId = `gauge-fill-${gradientSuffix}`;
  const highlightGradientId = `gauge-highlight-${gradientSuffix}`;
  const shadowFilterId = `gauge-shadow-${gradientSuffix}`;

  const defs = document.createElementNS(svgNS, 'defs');

  const trackGradient = document.createElementNS(svgNS, 'linearGradient');
  trackGradient.id = trackGradientId;
  trackGradient.setAttribute('x1', '0%');
  trackGradient.setAttribute('y1', '0%');
  trackGradient.setAttribute('x2', '0%');
  trackGradient.setAttribute('y2', '100%');
  [
    { offset: '0%', color: '#F8FAFC' },
    { offset: '100%', color: '#E2E8F0' }
  ].forEach(stopCfg => {
    const stop = document.createElementNS(svgNS, 'stop');
    stop.setAttribute('offset', stopCfg.offset);
    stop.setAttribute('stop-color', stopCfg.color);
    trackGradient.appendChild(stop);
  });

  const fillGradient = document.createElementNS(svgNS, 'linearGradient');
  fillGradient.id = fillGradientId;
  fillGradient.setAttribute('x1', '0%');
  fillGradient.setAttribute('y1', '100%');
  fillGradient.setAttribute('x2', '0%');
  fillGradient.setAttribute('y2', '0%');
  [
    { offset: '0%', color: '#1E3A8A' },
    { offset: '45%', color: '#2563EB' },
    { offset: '100%', color: '#60A5FA' }
  ].forEach(stopCfg => {
    const stop = document.createElementNS(svgNS, 'stop');
    stop.setAttribute('offset', stopCfg.offset);
    stop.setAttribute('stop-color', stopCfg.color);
    fillGradient.appendChild(stop);
  });

  const highlightGradient = document.createElementNS(svgNS, 'linearGradient');
  highlightGradient.id = highlightGradientId;
  highlightGradient.setAttribute('x1', '0%');
  highlightGradient.setAttribute('y1', '0%');
  highlightGradient.setAttribute('x2', '100%');
  highlightGradient.setAttribute('y2', '0%');
  [
    { offset: '0%', color: '#FFFFFF', opacity: '0.45' },
    { offset: '60%', color: '#FFFFFF', opacity: '0.08' },
    { offset: '100%', color: '#FFFFFF', opacity: '0' }
  ].forEach(stopCfg => {
    const stop = document.createElementNS(svgNS, 'stop');
    stop.setAttribute('offset', stopCfg.offset);
    stop.setAttribute('stop-color', stopCfg.color);
    stop.setAttribute('stop-opacity', stopCfg.opacity);
    highlightGradient.appendChild(stop);
  });

  const shadowFilter = document.createElementNS(svgNS, 'filter');
  shadowFilter.id = shadowFilterId;
  shadowFilter.setAttribute('x', '-60%');
  shadowFilter.setAttribute('y', '-20%');
  shadowFilter.setAttribute('width', '220%');
  shadowFilter.setAttribute('height', '160%');
  const dropShadow = document.createElementNS(svgNS, 'feDropShadow');
  dropShadow.setAttribute('dx', '0');
  dropShadow.setAttribute('dy', '0.4');
  dropShadow.setAttribute('stdDeviation', '0.6');
  dropShadow.setAttribute('flood-color', '#1D4ED8');
  dropShadow.setAttribute('flood-opacity', '0.35');
  shadowFilter.appendChild(dropShadow);

  defs.appendChild(trackGradient);
  defs.appendChild(fillGradient);
  defs.appendChild(highlightGradient);
  defs.appendChild(shadowFilter);
  svg.appendChild(defs);

  // 基础几何参数
  const rectX = 6.3;
  const rectY = 0.6;
  const rectWidth = 8.5;
  const rectHeight = 13.8;

  // 背景轨道
  const trackId = `A-GXP_${component.id}`;
  const rectBase = document.createElementNS(svgNS, 'rect');
  rectBase.id = trackId;
  rectBase.setAttribute('x', rectX.toString());
  rectBase.setAttribute('y', rectY.toString());
  rectBase.setAttribute('width', rectWidth.toString());
  rectBase.setAttribute('height', rectHeight.toString());
  rectBase.setAttribute('rx', '1.8');
  rectBase.setAttribute('ry', '1.8');
  rectBase.setAttribute('data-gradient-id', trackGradientId);
  const trackBackground = component.style?.backgroundColor;
  const useCustomTrackColor = trackBackground && trackBackground !== 'default' && trackBackground !== 'transparent';
  rectBase.setAttribute(
    'fill',
    useCustomTrackColor ? trackBackground : `url(#${trackGradientId})`
  );
  rectBase.setAttribute('stroke', component.style?.borderColor || '#CBD5F5');
  rectBase.setAttribute('stroke-width', (component.style?.borderWidth ?? 0.18).toString());
  svg.appendChild(rectBase);

  // 填充矩形
  const fillId = `B-GXP_${component.id}`;
  const rectFill = document.createElementNS(svgNS, 'rect');
  rectFill.id = fillId;
  rectFill.setAttribute('x', rectX.toString());
  rectFill.setAttribute('width', rectWidth.toString());
  rectFill.setAttribute('rx', '1.4');
  rectFill.setAttribute('ry', '1.4');
  rectFill.setAttribute('data-gradient-id', fillGradientId);
  rectFill.setAttribute(
    'fill',
    firstRange.color && firstRange.color !== 'default'
      ? firstRange.color
      : `url(#${fillGradientId})`
  );
  rectFill.setAttribute('stroke', 'none');
  rectFill.setAttribute('stroke-width', '0');
  rectFill.setAttribute('filter', `url(#${shadowFilterId})`);

  const fillHeight = rectHeight * ((clampedValue - min) / (max - min || 1));
  const fillY = rectY + rectHeight - fillHeight;
  rectFill.setAttribute('y', fillY.toString());
  rectFill.setAttribute('height', Math.max(fillHeight, 0).toString());
  svg.appendChild(rectFill);

  // 高光效果
  const highlightPath = document.createElementNS(svgNS, 'path');
  highlightPath.setAttribute('d', 'M7.2,1.2 h2.1 v12.7 c-0.8,-0.4 -1.4,-0.5 -2.1,-1 z');
  highlightPath.setAttribute('fill', `url(#${highlightGradientId})`);
  svg.appendChild(highlightPath);

  // 刻度
  const marksGroup = document.createElementNS(svgNS, 'g');
  marksGroup.id = `A-GXP_MARKS_${component.id}`;
  marksGroup.setAttribute('fill', '#94A3B8');
  marksGroup.setAttribute('opacity', '0.85');
  const markCount = 5;
  for (let i = 0; i < markCount; i += 1) {
    const ratio = markCount === 1 ? 0 : i / (markCount - 1);
    const markY = rectY + ratio * rectHeight;
    const mark = document.createElementNS(svgNS, 'rect');
    mark.setAttribute('x', '15.2');
    mark.setAttribute('y', markY.toFixed(2));
    mark.setAttribute('width', '1.2');
    mark.setAttribute('height', '0.24');
    mark.setAttribute('rx', '0.12');
    marksGroup.appendChild(mark);
  }
  svg.appendChild(marksGroup);

  // 底部数值标签
  const valueText = document.createElementNS(svgNS, 'text');
  valueText.id = `C-GXP_${component.id}`;
  const valueTextX = rectX + rectWidth / 2;
  const valueTextY = rectY + rectHeight / 2;
  valueText.setAttribute('x', valueTextX.toFixed(2));
  valueText.setAttribute('y', valueTextY.toFixed(2));
  valueText.setAttribute('text-anchor', 'middle');
  valueText.setAttribute('dominant-baseline', 'middle');
  valueText.setAttribute('font-size', '2.2');
  valueText.setAttribute('font-family', 'Inter, Arial, sans-serif');
  valueText.setAttribute('font-weight', '600');
  valueText.setAttribute('fill', component.style?.labelColor || '#0F172A');
  valueText.textContent = formatGaugeDisplay(clampedValue, decimals, unit);
  svg.appendChild(valueText);

  container.appendChild(svg);

  setupComponentInteractions(container, component);
  canvasContent.appendChild(container);

  return container;
};

/**
 * 更新 GaugeProgress 组件外观和数值 - SVG 版本
 * 参考 FUXA: gauge-progress.component.ts processValue()
 */
export const updateGaugeProgressAppearance = (component: any, element: HTMLElement) => {
  if (!element) return;

  const props = component.properties || {};
  const firstRange = props.ranges?.[0] || { min: 0, max: 100, color: '#3B82F6', style: [true, true] };
  const min = typeof props.min === 'number' ? props.min : firstRange.min ?? 0;
  const max = typeof props.max === 'number' ? props.max : firstRange.max ?? 100;
  const decimals = typeof props.decimals === 'number' ? props.decimals : 0;
  const unit = typeof props.unit === 'string' ? props.unit : '%';

  // 更新容器位置和元数据
  element.style.left = `${component.position.x}px`;
  element.style.top = `${component.position.y}px`;
  element.style.width = `${component.size.width}px`;
  element.style.height = `${component.size.height}px`;
  element.dataset.gaugeMin = min.toString();
  element.dataset.gaugeMax = max.toString();
  element.dataset.gaugeUnit = unit;
  element.dataset.gaugeDecimals = decimals.toString();

  // 数值处理
  const rawValue = props.value;
  let numericValue = parseNumericValue(rawValue);
  numericValue = clampGaugeValue(numericValue, min, max);
  element.dataset.gaugeValue = numericValue.toString();
  if (component.properties) {
    component.properties.value = numericValue;
  }

  const ranges = props.ranges || [];
  const activeRange =
    ranges.find((item: any) => numericValue >= item.min && numericValue <= item.max) || firstRange;

  // 获取 SVG 矩形元素
  const rectBase = element.querySelector(`#A-GXP_${component.id}`) as SVGRectElement;
  const rectFill = element.querySelector(`#B-GXP_${component.id}`) as SVGRectElement;
  const valueText = element.querySelector(`#C-GXP_${component.id}`) as SVGTextElement;

  if (rectBase && rectFill) {
    // 获取背景矩形的尺寸 - FUXA 算法
    const heightBase = parseFloat(rectBase.getAttribute('height') || '0');
    const yBase = parseFloat(rectBase.getAttribute('y') || '0');

    const rangeSpan = max - min || 1;
    const targetHeight = heightBase * ((numericValue - min) / rangeSpan);
    const targetY = yBase + heightBase - targetHeight;

    if (!Number.isNaN(targetHeight)) {
      // 使用 SVG 动画实现液体上涨效果
      const currentY = parseFloat(rectFill.getAttribute('y') || (yBase + heightBase).toString());
      const currentHeight = parseFloat(rectFill.getAttribute('height') || '0');

      // 创建或更新 animate 元素 - y 坐标动画
      let animateY = rectFill.querySelector('animate[attributeName="y"]');
      if (!animateY) {
        animateY = document.createElementNS('http://www.w3.org/2000/svg', 'animate');
        animateY.setAttribute('attributeName', 'y');
        animateY.setAttribute('dur', '0.8s');  // 动画持续时间
        animateY.setAttribute('fill', 'freeze');  // 动画结束后保持最终状态
        animateY.setAttribute('calcMode', 'spline');  // 使用贝塞尔曲线
        animateY.setAttribute('keySplines', '0.4 0.0 0.2 1');  // 缓动函数: ease-out
        animateY.setAttribute('keyTimes', '0;1');
        rectFill.appendChild(animateY);
      }
      animateY.setAttribute('from', currentY.toString());
      animateY.setAttribute('to', targetY.toString());
      (animateY as any).beginElement();  // 启动动画

      // 创建或更新 animate 元素 - height 动画
      let animateHeight = rectFill.querySelector('animate[attributeName="height"]');
      if (!animateHeight) {
        animateHeight = document.createElementNS('http://www.w3.org/2000/svg', 'animate');
        animateHeight.setAttribute('attributeName', 'height');
        animateHeight.setAttribute('dur', '0.8s');  // 动画持续时间
        animateHeight.setAttribute('fill', 'freeze');
        animateHeight.setAttribute('calcMode', 'spline');
        animateHeight.setAttribute('keySplines', '0.4 0.0 0.2 1');  // 缓动函数: ease-out
        animateHeight.setAttribute('keyTimes', '0;1');
        rectFill.appendChild(animateHeight);
      }
      animateHeight.setAttribute('from', currentHeight.toString());
      animateHeight.setAttribute('to', targetHeight.toString());
      (animateHeight as any).beginElement();  // 启动动画

      // 立即更新属性(作为动画的fallback)
      rectFill.setAttribute('y', targetY.toString());
      rectFill.setAttribute('height', targetHeight.toString());

      // 更新颜色 - 带渐变动画
      const fillGradientId = rectFill.getAttribute('data-gradient-id');
      const defaultFill = fillGradientId ? `url(#${fillGradientId})` : (firstRange.color || '#3B82F6');
      const targetColor = activeRange?.color && activeRange.color !== 'default'
        ? activeRange.color
        : defaultFill;

      if (targetColor) {
        const currentColor = rectFill.getAttribute('fill') || defaultFill;
        if (currentColor !== targetColor) {
          // 创建颜色渐变动画
          if (isSolidColor(currentColor) && isSolidColor(targetColor)) {
            let animateColor = rectFill.querySelector('animate[attributeName="fill"]');
            if (!animateColor) {
              animateColor = document.createElementNS('http://www.w3.org/2000/svg', 'animate');
              animateColor.setAttribute('attributeName', 'fill');
              animateColor.setAttribute('dur', '0.5s');
              animateColor.setAttribute('fill', 'freeze');
              rectFill.appendChild(animateColor);
            }
            animateColor.setAttribute('from', currentColor);
            animateColor.setAttribute('to', targetColor);
            (animateColor as any).beginElement();
          } else {
            // 移除无法动画的渐变动画节点
            const animateColor = rectFill.querySelector('animate[attributeName="fill"]');
            if (animateColor) {
              animateColor.remove();
            }
          }
        }
        rectFill.setAttribute('fill', targetColor);
      }
    }
  }

  // 更新背景矩形样式
  if (rectBase) {
    const trackGradientId = rectBase.getAttribute('data-gradient-id');
    const defaultTrack = trackGradientId ? `url(#${trackGradientId})` : '#F1F5F9';
    const trackBackground = component.style?.backgroundColor;
    const useCustomTrackColor = trackBackground && trackBackground !== 'default' && trackBackground !== 'transparent';
    const backgroundFill = useCustomTrackColor ? trackBackground : defaultTrack;

    rectBase.setAttribute('fill', backgroundFill);
    rectBase.setAttribute('stroke', component.style?.borderColor || '#CBD5F5');
    rectBase.setAttribute('stroke-width', (component.style?.borderWidth ?? 0.18).toString());
  }

  if (valueText) {
    valueText.textContent = formatGaugeDisplay(numericValue, decimals, unit);
    valueText.setAttribute('fill', component.style?.labelColor || '#1E293B');
  }
};

// ========================================
// GaugeSemaphore 组件 - 信号灯/指示灯 (semaphore)
// ========================================

/**
 * 初始化 GaugeSemaphore 组件的默认属性
 */
export const initializeGaugeSemaphoreComponent = (componentInstance: any) => {
  componentInstance.properties = {
    variableId: '',
    value: 0,
    // 范围配置 - 不同值范围显示不同颜色
    ranges: [
      { min: 0, max: 0, color: '#808080' },      // 灰色 - 未激活
      { min: 1, max: 1, color: '#00FF00' }       // 绿色 - 激活
    ],
    bitmask: null  // 位掩码(可选)
  };

  componentInstance.style = {
    backgroundColor: 'transparent',
    borderColor: '#333333',
    borderWidth: 2,
    borderRadius: '50%'  // 圆形
  };

  console.log('🚦 初始化 GaugeSemaphore 组件');
};

/**
 * 创建 GaugeSemaphore 组件 DOM 元素
 */
export const createGaugeSemaphoreElement = (
  component: any,
  canvasContent: Element,
  setupComponentInteractions: (element: HTMLElement, component: any) => void
) => {
  const props = component.properties || {};
  const container = document.createElement('div');
  container.id = component.id;
  container.className = 'fuxa-component gaugesemaphore-component';
  container.style.cssText = `
    position: absolute;
    left: ${component.position.x}px;
    top: ${component.position.y}px;
    width: ${component.size.width}px;
    height: ${component.size.height}px;
    display: flex;
    align-items: center;
    justify-content: center;
  `;

  // 创建信号灯圆圈
  const circle = document.createElement('div');
  circle.className = 'semaphore-circle';

  // 根据初始值设置颜色
  let initialColor = '#808080';
  if (props.ranges && props.ranges.length > 0) {
    const range = props.ranges.find((r: any) => (props.value || 0) >= r.min && (props.value || 0) <= r.max);
    if (range) {
      initialColor = range.color;
    }
  }

  circle.style.cssText = `
    width: 100%;
    height: 100%;
    border-radius: ${component.style?.borderRadius || '50%'};
    background-color: ${initialColor};
    border: ${component.style?.borderWidth || 2}px solid ${component.style?.borderColor || '#333333'};
    box-shadow: inset 0 -5px 15px rgba(0,0,0,0.4), 0 5px 15px rgba(0,0,0,0.3);
    transition: background-color 0.3s ease;
  `;

  container.appendChild(circle);
  setupComponentInteractions(container, component);
  canvasContent.appendChild(container);

  return container;
};

/**
 * 更新 GaugeSemaphore 组件外观和状态
 */
export const updateGaugeSemaphoreAppearance = (component: any, element: HTMLElement) => {
  if (!element) return;

  const props = component.properties || {};

  element.style.left = `${component.position.x}px`;
  element.style.top = `${component.position.y}px`;
  element.style.width = `${component.size.width}px`;
  element.style.height = `${component.size.height}px`;

  const circle = element.querySelector('.semaphore-circle') as HTMLElement;
  if (circle) {
    // 根据当前值更新颜色
    let color = '#808080';
    const value = props.value || 0;

    if (props.ranges && props.ranges.length > 0) {
      const range = props.ranges.find((r: any) => value >= r.min && value <= r.max);
      if (range && range.color) {
        color = range.color;
      }
    }

    circle.style.backgroundColor = color;
    circle.style.borderRadius = component.style?.borderRadius || '50%';
    circle.style.border = `${component.style?.borderWidth || 2}px solid ${component.style?.borderColor || '#333333'}`;
  }
};

// ========================================
// HtmlBag 组件 - 仪表盘 (bag)
// ========================================

/**
 * 初始化 HtmlBag 组件的默认属性
 */
export const initializeHtmlBagComponent = (componentInstance: any) => {
  componentInstance.properties = {
    variableId: '',
    value: 0,
    min: 0,
    max: 100,
    // 仪表盘类型
    gaugeType: 'radial',  // radial, semi, arch
    // 显示选项
    showValue: true,
    showMinMax: true,
    unit: '',
    fractionDigits: 0
  };

  componentInstance.style = {
    backgroundColor: 'transparent',
    // 仪表盘颜色配置
    dialColor: '#eeeeee',        // 表盘背景色
    needleColor: '#ff0000',      // 指针颜色
    valueColor: '#000000',       // 值文字颜色
    minColor: '#666666',         // 最小值颜色
    maxColor: '#666666'          // 最大值颜色
  };

  console.log('🎯 初始化 HtmlBag 组件');
};

/**
 * 创建 HtmlBag 组件 DOM 元素
 * 注意: 这是简化版本,完整的仪表盘需要使用 Canvas 或 SVG 绘制
 */
export const createHtmlBagElement = (
  component: any,
  canvasContent: Element,
  setupComponentInteractions: (element: HTMLElement, component: any) => void
) => {
  const props = component.properties || {};
  const container = document.createElement('div');
  container.id = component.id;
  container.className = 'fuxa-component htmlbag-component';
  container.style.cssText = `
    position: absolute;
    left: ${component.position.x}px;
    top: ${component.position.y}px;
    width: ${component.size.width}px;
    height: ${component.size.height}px;
    background-color: ${component.style?.backgroundColor || 'transparent'};
  `;

  // 创建仪表盘容器
  const gaugeContainer = document.createElement('div');
  gaugeContainer.id = `D-BAG_${component.id}`;
  gaugeContainer.className = 'gauge-container';
  gaugeContainer.style.cssText = `
    width: 100%;
    height: 100%;
    position: relative;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    border-radius: 50%;
    background: radial-gradient(circle, ${component.style?.dialColor || '#eeeeee'} 0%, #cccccc 100%);
    border: 3px solid #999999;
    box-shadow: inset 0 0 15px rgba(0,0,0,0.2), 0 3px 10px rgba(0,0,0,0.3);
  `;

  // 创建值显示
  const valueDisplay = document.createElement('div');
  valueDisplay.style.cssText = `
    font-size: ${Math.min(component.size.width, component.size.height) / 4}px;
    font-weight: bold;
    color: ${component.style?.valueColor || '#000000'};
    text-align: center;
  `;

  let displayValue = (props.value || 0).toFixed(props.fractionDigits || 0);
  if (props.unit) {
    displayValue += props.unit;
  }
  valueDisplay.textContent = displayValue;

  // 创建最小/最大值标签
  if (props.showMinMax) {
    const minLabel = document.createElement('div');
    minLabel.style.cssText = `
      position: absolute;
      bottom: 10px;
      left: 10px;
      font-size: 10px;
      color: ${component.style?.minColor || '#666666'};
    `;
    minLabel.textContent = props.min !== undefined ? props.min.toString() : '0';
    gaugeContainer.appendChild(minLabel);

    const maxLabel = document.createElement('div');
    maxLabel.style.cssText = `
      position: absolute;
      bottom: 10px;
      right: 10px;
      font-size: 10px;
      color: ${component.style?.maxColor || '#666666'};
    `;
    maxLabel.textContent = props.max !== undefined ? props.max.toString() : '100';
    gaugeContainer.appendChild(maxLabel);
  }

  gaugeContainer.appendChild(valueDisplay);
  container.appendChild(gaugeContainer);

  setupComponentInteractions(container, component);
  canvasContent.appendChild(container);

  return container;
};

/**
 * 更新 HtmlBag 组件外观和数值
 */
export const updateHtmlBagAppearance = (component: any, element: HTMLElement) => {
  if (!element) return;

  const props = component.properties || {};

  element.style.left = `${component.position.x}px`;
  element.style.top = `${component.position.y}px`;
  element.style.width = `${component.size.width}px`;
  element.style.height = `${component.size.height}px`;

  const gaugeContainer = element.querySelector('.gauge-container') as HTMLElement;
  if (gaugeContainer) {
    // 更新值显示
    const valueDisplay = gaugeContainer.querySelector('div:not([style*="position: absolute"])') as HTMLElement;
    if (valueDisplay) {
      let displayValue = (props.value || 0).toFixed(props.fractionDigits || 0);
      if (props.unit) {
        displayValue += props.unit;
      }
      valueDisplay.textContent = displayValue;
      valueDisplay.style.fontSize = `${Math.min(component.size.width, component.size.height) / 4}px`;
    }
  }
};
