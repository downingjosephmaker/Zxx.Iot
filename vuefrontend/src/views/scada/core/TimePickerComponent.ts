import type { ScadaComponent } from '../types'

/**
 * 时间选择器类型定义
 */
export interface TimePickerConfig {
  name: string;
  title: string;
  icon: string;
  category: 'datetime' | 'date' | 'time' | 'range' | 'clock';
  defaultProps: Partial<ScadaComponent>;
  pickerOptions?: {
    format: string;
    valueFormat: string;
    editable: boolean;
    clearable: boolean;
  };
}

/**
 * 时间选择器组件注册表
 */
export const timePickerComponents: TimePickerConfig[] = [
  // 日期时间选择器
  {
    name: 'datetime-picker',
    title: '日期时间选择器',
    icon: 'ep:calendar',
    category: 'datetime',
    defaultProps: {
      type: 'datetime-picker',
      width: 200,
      height: 40,
      properties: {
        value: '',
        placeholder: '选择日期时间',
        format: 'YYYY-MM-DD HH:mm:ss',
        valueFormat: 'YYYY-MM-DD HH:mm:ss',
        editable: true,
        clearable: true,
        disabled: false,
        readonly: false,
        size: 'default', // large, default, small
        startPlaceholder: '开始日期',
        endPlaceholder: '结束日期',
        rangeSeparator: '至',
        shortcuts: true,
        disabledDate: null,
        cellClassName: '',
        defaultTime: null,
        defaultValue: null,
        popperClass: '',
        unlinkPanels: false,
        validateEvent: true
      }
    },
    pickerOptions: {
      format: 'YYYY-MM-DD HH:mm:ss',
      valueFormat: 'YYYY-MM-DD HH:mm:ss',
      editable: true,
      clearable: true
    }
  },
  
  // 日期选择器
  {
    name: 'date-picker',
    title: '日期选择器',
    icon: 'ep:date',
    category: 'date',
    defaultProps: {
      type: 'date-picker',
      width: 180,
      height: 40,
      properties: {
        value: '',
        placeholder: '选择日期',
        format: 'YYYY-MM-DD',
        valueFormat: 'YYYY-MM-DD',
        pickerType: 'date', // date, week, month, year, dates
        editable: true,
        clearable: true,
        disabled: false,
        readonly: false,
        size: 'default',
        shortcuts: true,
        disabledDate: null,
        firstDayOfWeek: 7,
        defaultValue: null,
        validateEvent: true
      }
    },
    pickerOptions: {
      format: 'YYYY-MM-DD',
      valueFormat: 'YYYY-MM-DD',
      editable: true,
      clearable: true
    }
  },
  
  // 时间选择器
  {
    name: 'time-picker',
    title: '时间选择器',
    icon: 'ep:timer',
    category: 'time',
    defaultProps: {
      type: 'time-picker',
      width: 150,
      height: 40,
      properties: {
        value: '',
        placeholder: '选择时间',
        format: 'HH:mm:ss',
        valueFormat: 'HH:mm:ss',
        editable: true,
        clearable: true,
        disabled: false,
        readonly: false,
        size: 'default',
        arrowControl: false,
        selectableRange: '',
        defaultValue: null,
        name: '',
        prefixIcon: '',
        clearIcon: '',
        disabledHours: null,
        disabledMinutes: null,
        disabledSeconds: null,
        hideDisabledOptions: false,
        validateEvent: true
      }
    },
    pickerOptions: {
      format: 'HH:mm:ss',
      valueFormat: 'HH:mm:ss',
      editable: true,
      clearable: true
    }
  },
  
  // 日期时间范围选择器
  {
    name: 'datetime-range-picker',
    title: '日期时间范围选择器',
    icon: 'ep:calendar-range',
    category: 'range',
    defaultProps: {
      type: 'datetime-range-picker',
      width: 350,
      height: 40,
      properties: {
        value: [],
        startPlaceholder: '开始日期时间',
        endPlaceholder: '结束日期时间',
        format: 'YYYY-MM-DD HH:mm:ss',
        valueFormat: 'YYYY-MM-DD HH:mm:ss',
        rangeSeparator: '至',
        editable: true,
        clearable: true,
        disabled: false,
        readonly: false,
        size: 'default',
        shortcuts: true,
        disabledDate: null,
        defaultTime: [],
        defaultValue: null,
        unlinkPanels: false,
        validateEvent: true
      }
    },
    pickerOptions: {
      format: 'YYYY-MM-DD HH:mm:ss',
      valueFormat: 'YYYY-MM-DD HH:mm:ss',
      editable: true,
      clearable: true
    }
  },
  
  // 实时时钟
  {
    name: 'digital-clock',
    title: '数字时钟',
    icon: 'ep:clock',
    category: 'clock',
    defaultProps: {
      type: 'digital-clock',
      width: 200,
      height: 60,
      properties: {
        format: 'YYYY-MM-DD HH:mm:ss',
        timezone: 'Asia/Shanghai',
        updateInterval: 1000,
        showDate: true,
        showTime: true,
        showSeconds: true,
        use24Hour: true,
        fontSize: 16,
        fontFamily: 'monospace',
        fontWeight: 'normal',
        color: '#303133',
        backgroundColor: 'transparent',
        borderColor: '#dcdfe6',
        borderWidth: 1,
        borderRadius: 4,
        textAlign: 'center',
        padding: '10px',
        shadow: false,
        animation: 'none', // none, blink, fade
        customFormat: '',
        showWeekday: false,
        showMilliseconds: false
      }
    }
  }
];

/**
 * 时间选择器组件管理器
 */
export class TimePickerComponentManager {
  private clockIntervals: Map<string, NodeJS.Timeout> = new Map();
  private timePickerInstances: Map<string, any> = new Map();
  
  /**
   * 创建时间选择器组件实例
   */
  createTimePickerComponent(component: ScadaComponent, container: HTMLElement): HTMLElement {
    const config = timePickerComponents.find(c => c.name === component.type);
    if (!config) {
      console.warn(`Unknown time picker component type: ${component.type}`);
      return this.createErrorElement('未知时间选择器类型');
    }
    
    const element = document.createElement('div');
    element.className = `time-picker-component ${component.type}`;
    element.style.cssText = `
      width: 100%;
      height: 100%;
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
    `;
    
    this.renderTimePickerComponent(element, config, component);
    container.appendChild(element);
    
    return element;
  }
  
  /**
   * 渲染时间选择器组件
   */
  private renderTimePickerComponent(element: HTMLElement, config: TimePickerConfig, component: ScadaComponent) {
    const properties = JSON.parse(component.properties || '{}');
    
    switch (config.category) {
      case 'datetime':
      case 'date':
      case 'time':
      case 'range':
        this.renderPickerInput(element, config, properties, component.id);
        break;
      case 'clock':
        this.renderDigitalClock(element, properties, component.id);
        break;
    }
  }
  
  /**
   * 渲染选择器输入框
   */
  private renderPickerInput(element: HTMLElement, config: TimePickerConfig, properties: any, componentId: string) {
    const input = document.createElement('input');
    input.type = 'text';
    input.placeholder = properties.placeholder || config.defaultProps.properties?.placeholder || '';
    input.value = Array.isArray(properties.value) ? properties.value.join(` ${properties.rangeSeparator || '至'} `) : properties.value || '';
    input.disabled = properties.disabled || false;
    input.readOnly = properties.readonly || false;
    
    // 设置样式
    const size = properties.size || 'default';
    const sizeMap = {
      large: { height: '40px', fontSize: '16px', padding: '0 15px' },
      default: { height: '32px', fontSize: '14px', padding: '0 11px' },
      small: { height: '24px', fontSize: '12px', padding: '0 7px' }
    };
    
    const sizeStyle = sizeMap[size as keyof typeof sizeMap] || sizeMap.default;
    
    input.style.cssText = `
      width: 100%;
      height: ${sizeStyle.height};
      font-size: ${sizeStyle.fontSize};
      padding: ${sizeStyle.padding};
      border: ${properties.borderWidth || 1}px solid ${properties.borderColor || '#dcdfe6'};
      border-radius: ${properties.borderRadius || 4}px;
      background-color: ${properties.backgroundColor || '#ffffff'};
      color: ${properties.color || '#606266'};
      outline: none;
      transition: border-color 0.2s ease;
      cursor: ${properties.disabled ? 'not-allowed' : 'pointer'};
      box-sizing: border-box;
    `;
    
    // 添加图标
    const iconContainer = document.createElement('div');
    iconContainer.style.cssText = `
      position: relative;
      width: 100%;
      height: 100%;
    `;
    
    const icon = document.createElement('i');
    icon.className = config.icon;
    icon.style.cssText = `
      position: absolute;
      right: 8px;
      top: 50%;
      transform: translateY(-50%);
      color: #c0c4cc;
      font-size: 14px;
      pointer-events: none;
    `;
    
    iconContainer.appendChild(input);
    iconContainer.appendChild(icon);
    
    // 添加清除按钮
    if (properties.clearable && !properties.disabled) {
      const clearBtn = document.createElement('i');
      clearBtn.className = 'ep-circle-close';
      clearBtn.style.cssText = `
        position: absolute;
        right: 24px;
        top: 50%;
        transform: translateY(-50%);
        color: #c0c4cc;
        font-size: 14px;
        cursor: pointer;
        display: none;
      `;
      
      clearBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        input.value = '';
        this.onTimePickerChange?.(componentId, '', config.category);
      });
      
      // 显示/隐藏清除按钮
      input.addEventListener('input', () => {
        clearBtn.style.display = input.value ? 'block' : 'none';
        icon.style.display = input.value ? 'none' : 'block';
      });
      
      iconContainer.appendChild(clearBtn);
    }
    
    // 添加焦点样式
    input.addEventListener('focus', () => {
      input.style.borderColor = '#409eff';
      input.style.boxShadow = '0 0 0 2px rgba(64, 158, 255, 0.2)';
    });
    
    input.addEventListener('blur', () => {
      input.style.borderColor = properties.borderColor || '#dcdfe6';
      input.style.boxShadow = 'none';
    });
    
    // 模拟日期选择器点击事件
    iconContainer.addEventListener('click', () => {
      if (!properties.disabled) {
        this.showDatePicker(componentId, config, properties, input);
      }
    });
    
    element.appendChild(iconContainer);
    this.timePickerInstances.set(componentId, { input, config, properties });
  }
  
  /**
   * 渲染数字时钟
   */
  private renderDigitalClock(element: HTMLElement, properties: any, componentId: string) {
    const clockElement = document.createElement('div');
    clockElement.className = 'digital-clock';
    
    clockElement.style.cssText = `
      width: 100%;
      height: 100%;
      display: flex;
      flex-direction: column;
      align-items: ${properties.textAlign || 'center'};
      justify-content: center;
      font-family: ${properties.fontFamily || 'monospace'};
      font-size: ${properties.fontSize || 16}px;
      font-weight: ${properties.fontWeight || 'normal'};
      color: ${properties.color || '#303133'};
      background-color: ${properties.backgroundColor || 'transparent'};
      border: ${properties.borderWidth || 1}px solid ${properties.borderColor || '#dcdfe6'};
      border-radius: ${properties.borderRadius || 4}px;
      padding: ${properties.padding || '10px'};
      box-sizing: border-box;
      ${properties.shadow ? 'box-shadow: 0 2px 4px rgba(0,0,0,0.1);' : ''}
    `;
    
    const timeDisplay = document.createElement('div');
    timeDisplay.className = 'time-display';
    
    const dateDisplay = document.createElement('div');
    dateDisplay.className = 'date-display';
    dateDisplay.style.cssText = `
      font-size: ${(properties.fontSize || 16) * 0.8}px;
      opacity: 0.8;
      margin-bottom: 4px;
    `;
    
    if (properties.showDate) {
      clockElement.appendChild(dateDisplay);
    }
    
    clockElement.appendChild(timeDisplay);
    
    // 启动时钟更新
    this.startClockUpdate(componentId, timeDisplay, dateDisplay, properties);
    
    element.appendChild(clockElement);
  }
  
  /**
   * 显示日期选择器（模拟）
   */
  private showDatePicker(componentId: string, config: TimePickerConfig, properties: any, input: HTMLInputElement) {
    // 这里应该集成实际的日期选择器库，比如 Element Plus 的 DatePicker
    // 为了演示，我们创建一个简单的模拟选择器
    
    const now = new Date();
    let selectedValue = '';
    
    switch (config.category) {
      case 'datetime':
        selectedValue = this.formatDate(now, 'YYYY-MM-DD HH:mm:ss');
        break;
      case 'date':
        selectedValue = this.formatDate(now, 'YYYY-MM-DD');
        break;
      case 'time':
        selectedValue = this.formatDate(now, 'HH:mm:ss');
        break;
      case 'range':
        const endTime = new Date(now.getTime() + 24 * 60 * 60 * 1000);
        selectedValue = `${this.formatDate(now, 'YYYY-MM-DD HH:mm:ss')} ${properties.rangeSeparator || '至'} ${this.formatDate(endTime, 'YYYY-MM-DD HH:mm:ss')}`;
        break;
    }
    
    input.value = selectedValue;
    this.onTimePickerChange?.(componentId, selectedValue, config.category);
  }
  
  /**
   * 启动时钟更新
   */
  private startClockUpdate(componentId: string, timeDisplay: HTMLElement, dateDisplay: HTMLElement, properties: any) {
    const updateClock = () => {
      const now = new Date();
      
      if (properties.showTime) {
        let timeFormat = '';
        if (properties.customFormat) {
          timeFormat = properties.customFormat;
        } else {
          timeFormat = properties.use24Hour ? 'HH:mm' : 'hh:mm A';
          if (properties.showSeconds) {
            timeFormat += properties.use24Hour ? ':ss' : ':ss';
          }
          if (properties.showMilliseconds) {
            timeFormat += '.SSS';
          }
        }
        
        timeDisplay.textContent = this.formatDate(now, timeFormat);
        
        // 添加动画效果
        if (properties.animation === 'blink' && now.getSeconds() % 2 === 0) {
          timeDisplay.style.opacity = '0.5';
          setTimeout(() => {
            timeDisplay.style.opacity = '1';
          }, 500);
        }
      }
      
      if (properties.showDate && dateDisplay) {
        let dateFormat = 'YYYY-MM-DD';
        if (properties.showWeekday) {
          dateFormat = 'dddd, ' + dateFormat;
        }
        
        dateDisplay.textContent = this.formatDate(now, dateFormat);
      }
    };
    
    // 初始更新
    updateClock();
    
    // 设置定时更新
    const interval = setInterval(updateClock, properties.updateInterval || 1000);
    this.clockIntervals.set(componentId, interval);
  }
  
  /**
   * 格式化日期（简单实现）
   */
  private formatDate(date: Date, format: string): string {
    const map: Record<string, string> = {
      'YYYY': date.getFullYear().toString(),
      'MM': (date.getMonth() + 1).toString().padStart(2, '0'),
      'DD': date.getDate().toString().padStart(2, '0'),
      'HH': date.getHours().toString().padStart(2, '0'),
      'mm': date.getMinutes().toString().padStart(2, '0'),
      'ss': date.getSeconds().toString().padStart(2, '0'),
      'SSS': date.getMilliseconds().toString().padStart(3, '0'),
      'hh': (date.getHours() % 12 || 12).toString().padStart(2, '0'),
      'A': date.getHours() >= 12 ? 'PM' : 'AM',
      'dddd': ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'][date.getDay()]
    };
    
    let result = format;
    Object.entries(map).forEach(([key, value]) => {
      result = result.replace(new RegExp(key, 'g'), value);
    });
    
    return result;
  }
  
  /**
   * 创建错误元素
   */
  private createErrorElement(message: string): HTMLElement {
    const errorEl = document.createElement('div');
    errorEl.style.cssText = `
      color: #f56c6c;
      font-size: 14px;
      text-align: center;
      padding: 20px;
    `;
    errorEl.innerHTML = `
      <div style="margin-bottom: 5px;">❌</div>
      <div>${message}</div>
    `;
    return errorEl;
  }
  
  /**
   * 更新时间选择器组件
   */
  updateTimePickerComponent(componentId: string, newProperties: any) {
    const instance = this.timePickerInstances.get(componentId);
    if (instance) {
      Object.assign(instance.properties, newProperties);
      
      if (instance.input) {
        // 更新输入框属性
        if (newProperties.placeholder !== undefined) {
          instance.input.placeholder = newProperties.placeholder;
        }
        if (newProperties.disabled !== undefined) {
          instance.input.disabled = newProperties.disabled;
        }
        if (newProperties.value !== undefined) {
          instance.input.value = newProperties.value;
        }
      }
    }
  }
  
  /**
   * 获取时间选择器值
   */
  getTimePickerValue(componentId: string): string | string[] {
    const instance = this.timePickerInstances.get(componentId);
    if (instance && instance.input) {
      const value = instance.input.value;
      if (instance.config.category === 'range' && value.includes(instance.properties.rangeSeparator || '至')) {
        return value.split(instance.properties.rangeSeparator || '至').map((v: string) => v.trim());
      }
      return value;
    }
    return '';
  }
  
  /**
   * 设置时间选择器值
   */
  setTimePickerValue(componentId: string, value: string | string[]) {
    const instance = this.timePickerInstances.get(componentId);
    if (instance && instance.input) {
      if (Array.isArray(value)) {
        instance.input.value = value.join(` ${instance.properties.rangeSeparator || '至'} `);
      } else {
        instance.input.value = value;
      }
      this.onTimePickerChange?.(componentId, value, instance.config.category);
    }
  }
  
  /**
   * 停止时钟更新
   */
  private stopClockUpdate(componentId: string) {
    const interval = this.clockIntervals.get(componentId);
    if (interval) {
      clearInterval(interval);
      this.clockIntervals.delete(componentId);
    }
  }
  
  /**
   * 销毁时间选择器组件
   */
  destroyTimePickerComponent(componentId: string) {
    this.stopClockUpdate(componentId);
    this.timePickerInstances.delete(componentId);
  }
  
  /**
   * 获取时间选择器配置
   */
  getTimePickerConfig(type: string): TimePickerConfig | undefined {
    return timePickerComponents.find(config => config.name === type);
  }
  
  /**
   * 按分类获取时间选择器
   */
  getTimePickersByCategory(category: string): TimePickerConfig[] {
    return timePickerComponents.filter(config => config.category === category);
  }
  
  /**
   * 初始化时间选择器管理器
   */
  initialize(container?: HTMLElement) {
    console.log('时间选择器组件管理器已初始化');
  }
  
  /**
   * 清理所有资源
   */
  destroy() {
    this.clockIntervals.forEach((interval) => {
      clearInterval(interval);
    });
    this.clockIntervals.clear();
    this.timePickerInstances.clear();
  }
  
  // 事件回调
  onTimePickerChange?: (componentId: string, value: string | string[], category: string) => void;
  onTimePickerFocus?: (componentId: string) => void;
  onTimePickerBlur?: (componentId: string) => void;
}