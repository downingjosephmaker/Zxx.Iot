/**
 * 按钮组件工具函数
 * 包含按钮组件的初始化、创建、渲染和样式更新逻辑
 */

/**
 * 初始化按钮组件的默认属性和样式
 * @param componentInstance 组件实例
 */
export const initializeButtonComponent = (componentInstance: any) => {
  // 初始化按钮特定属性
  componentInstance.properties = {
    text: '按钮',          // 按钮文本
    fontSize: 14,
    fontFamily: 'Arial',
    fontWeight: 'normal',
    color: '#ffffff',      // 文字颜色
    textAlign: 'center',
    verticalAlign: 'middle', // 垂直对齐
    textDecoration: 'none',  // 文字装饰
    // 按钮状态样式
    normalStyle: {
      backgroundColor: '#409eff',
      borderColor: '#409eff',
      borderWidth: 1,
      borderRadius: 4
    },
    hoverStyle: {
      backgroundColor: '#66b1ff',
      borderColor: '#66b1ff'
    },
    activeStyle: {
      backgroundColor: '#3a8ee6',
      borderColor: '#3a8ee6'
    },
    disabledStyle: {
      backgroundColor: '#a0cfff',
      borderColor: '#a0cfff',
      cursor: 'not-allowed'
    },
    disabled: false,       // 是否禁用
    buttonType: 'primary'  // 按钮类型: primary, success, warning, danger, info
  };

  componentInstance.style = {
    backgroundColor: '#409eff',
    borderColor: '#409eff',
    borderWidth: 1,
    borderRadius: 4
  };

  // 事件配置（通过属性面板配置）
  componentInstance.events = [];

  console.log('🔲 创建按钮组件，初始化默认样式');
};

/**
 * 创建按钮DOM元素
 * @param component 组件配置
 * @param canvasContent 画布容器
 * @param setupComponentInteractions 组件交互设置函数
 * @returns 创建的按钮元素
 */
export const createButtonElement = (
  component: any,
  canvasContent: Element,
  setupComponentInteractions: (element: HTMLElement, component: any) => void
) => {
  const element = document.createElement('button');
  element.id = component.id;
  element.className = 'fuxa-component button-component';
  element.type = 'button'; // 防止表单提交

  // 应用按钮样式
  const normalStyle = component.properties?.normalStyle || {};

  // 🔲 计算水平对齐值（flex 布局使用 justify-content）
  const textAlign = component.properties?.textAlign || 'center';
  const justifyContent = textAlign === 'left' ? 'flex-start' :
                        textAlign === 'right' ? 'flex-end' :
                        textAlign === 'justify' ? 'space-between' : 'center';

  // 🔲 计算垂直对齐值（flex 布局使用 align-items）
  const verticalAlign = component.properties?.verticalAlign || 'middle';
  const alignItems = verticalAlign === 'top' ? 'flex-start' :
                     verticalAlign === 'bottom' ? 'flex-end' : 'center';

  element.style.cssText = `
    position: absolute;
    left: ${component.position.x}px;
    top: ${component.position.y}px;
    width: ${component.size.width}px;
    height: ${component.size.height}px;
    background: ${normalStyle.backgroundColor || '#409eff'};
    border: ${normalStyle.borderWidth || 1}px solid ${normalStyle.borderColor || '#409eff'};
    border-radius: ${normalStyle.borderRadius || 4}px;
    color: ${component.properties?.color || '#ffffff'};
    font-size: ${component.properties?.fontSize || 14}px;
    font-family: ${component.properties?.fontFamily || 'Arial'};
    font-weight: ${component.properties?.fontWeight || 'normal'};
    text-decoration: ${component.properties?.textDecoration || 'none'};
    cursor: ${component.properties?.disabled ? 'not-allowed' : 'pointer'};
    user-select: none;
    z-index: 10;
    transition: none;
    display: flex;
    align-items: ${alignItems};
    justify-content: ${justifyContent};
    outline: none !important;
    opacity: ${component.properties?.disabled ? 0.6 : 1};
  `;

  // 设置按钮文本
  element.textContent = component.properties?.text || '按钮';

  // 禁用状态
  if (component.properties?.disabled) {
    element.disabled = true;
  }

  // 🔲 不再使用鼠标悬停和点击效果，已移除所有样式变化监听器

  // 存储原始边框样式（用于选中效果）
  element.setAttribute('data-original-border-color', normalStyle.borderColor || '#409eff');
  element.setAttribute('data-original-border-width', (normalStyle.borderWidth || 1) + 'px');

  // 🔲 存储原始阴影样式（用于选中状态时组合显示）
  if (component.style?.enableShadow) {
    const shadowOffsetX = component.style.shadowOffsetX || 0;
    const shadowOffsetY = component.style.shadowOffsetY || 4;
    const shadowBlur = component.style.shadowBlur || 8;
    const shadowSpread = component.style.shadowSpread || 0;
    const shadowColor = component.style.shadowColor || 'rgba(0, 0, 0, 0.1)';
    const shadowInset = component.style.shadowInset ? 'inset' : '';
    const boxShadow = `${shadowInset} ${shadowOffsetX}px ${shadowOffsetY}px ${shadowBlur}px ${shadowSpread}px ${shadowColor}`.trim();
    element.setAttribute('data-original-shadow', boxShadow);
  } else {
    element.setAttribute('data-original-shadow', 'none');
  }

  // 设置组件交互（会自动关联事件系统）
  setupComponentInteractions(element, component);

  canvasContent.appendChild(element);

  return element;
};

/**
 * 更新按钮外观
 * 当按钮属性通过属性面板修改后，调用此函数更新按钮的视觉样式
 * @param component 组件配置
 * @param element 按钮DOM元素
 */
export const updateButtonAppearance = (component: any, element: HTMLElement) => {
  if (!element) return;

  // 🔲 获取按钮的属性配置
  const properties = component.properties || {};
  const normalStyle = properties.normalStyle || {};
  const hoverStyle = properties.hoverStyle || {};
  const activeStyle = properties.activeStyle || {};
  const disabledStyle = properties.disabledStyle || {};

  // 🔲 更新按钮文本内容
  const buttonText = properties.text || '按钮';
  if (element.textContent !== buttonText) {
    element.textContent = buttonText;
  }

  // 🔲 更新文本样式（使用 setProperty 强制应用）
  element.style.setProperty('color', properties.color || '#ffffff', 'important');
  element.style.setProperty('font-size', (properties.fontSize || 14) + 'px', 'important');
  element.style.setProperty('font-family', properties.fontFamily || 'Arial', 'important');
  element.style.setProperty('font-weight', properties.fontWeight || 'normal', 'important');

  // 🔲 水平对齐：按钮使用 flex 布局，需要用 justify-content 而不是 text-align
  const textAlign = properties.textAlign || 'center';
  const justifyValue = textAlign === 'left' ? 'flex-start' :
                       textAlign === 'right' ? 'flex-end' :
                       textAlign === 'justify' ? 'space-between' : 'center';
  element.style.setProperty('justify-content', justifyValue, 'important');

  // 🔲 添加文字装饰（下划线等）
  if (properties.textDecoration) {
    element.style.setProperty('text-decoration', properties.textDecoration, 'important');
  } else {
    element.style.setProperty('text-decoration', 'none', 'important');
  }

  // 🔲 处理垂直对齐（按钮使用 flex 布局，需要用 align-items）
  if (properties.verticalAlign) {
    const alignValue = properties.verticalAlign === 'top' ? 'flex-start' :
                      properties.verticalAlign === 'bottom' ? 'flex-end' : 'center';
    element.style.setProperty('align-items', alignValue, 'important');
  } else {
    element.style.setProperty('align-items', 'center', 'important');
  }

  console.log('🔲 强制更新文本样式:', {
    color: properties.color,
    fontSize: properties.fontSize,
    fontFamily: properties.fontFamily,
    fontWeight: properties.fontWeight,
    textAlign: properties.textAlign,
    justifyContent: justifyValue,
    textDecoration: properties.textDecoration,
    verticalAlign: properties.verticalAlign
  });

  // 🔲 更新按钮的 normal 状态样式
  // 优先从 component.style 读取（外观面板的修改），如果没有则从 normalStyle 读取，最后使用默认值
  const bgColor = component.style?.backgroundColor || normalStyle.backgroundColor || '#409eff';
  const borderColor = component.style?.borderColor || normalStyle.borderColor || '#409eff';
  const borderWidth = component.style?.borderWidth ?? normalStyle.borderWidth ?? 1;
  const borderRadius = component.style?.borderRadius ?? normalStyle.borderRadius ?? 4;

  element.style.setProperty('background-color', bgColor, 'important');

  // 🔲 只更新边框属性，不使用 !important，避免覆盖选中状态的边框
  // 选中状态使用 box-shadow 而不是边框，所以边框可以正常设置
  element.style.setProperty('border-color', borderColor);
  element.style.setProperty('border-width', borderWidth + 'px');
  element.style.setProperty('border-style', 'solid');
  element.style.setProperty('border-radius', borderRadius + 'px', 'important');

  // 🔲 应用阴影效果（如果启用）
  if (component.style?.enableShadow) {
    const shadowOffsetX = component.style.shadowOffsetX || 0;
    const shadowOffsetY = component.style.shadowOffsetY || 4;
    const shadowBlur = component.style.shadowBlur || 8;
    const shadowSpread = component.style.shadowSpread || 0;
    const shadowColor = component.style.shadowColor || 'rgba(0, 0, 0, 0.1)';
    const shadowInset = component.style.shadowInset ? 'inset' : '';

    const boxShadow = `${shadowInset} ${shadowOffsetX}px ${shadowOffsetY}px ${shadowBlur}px ${shadowSpread}px ${shadowColor}`.trim();

    // 🔲 保存用户设置的阴影到 data 属性(用于选中状态时组合显示)
    element.setAttribute('data-original-shadow', boxShadow);

    // 🔲 如果当前组件被选中,需要组合用户阴影和选中指示器阴影
    if (element.classList.contains('selected')) {
      const combinedShadow = `${boxShadow}, 0 0 0 2px rgba(64, 158, 255, 0.4)`;
      element.style.setProperty('box-shadow', combinedShadow);
      console.log('🔲 应用组合阴影(用户+选中):', combinedShadow);
    } else {
      element.style.setProperty('box-shadow', boxShadow);
      console.log('🔲 应用用户阴影:', boxShadow);
    }
  } else {
    // 清除阴影效果
    element.setAttribute('data-original-shadow', 'none');

    // 🔲 如果当前组件被选中,仍然需要显示选中指示器阴影
    if (element.classList.contains('selected')) {
      element.style.setProperty('box-shadow', '0 0 0 2px rgba(64, 158, 255, 0.4)');
      console.log('🔲 无用户阴影,仅应用选中指示器阴影');
    } else {
      element.style.setProperty('box-shadow', 'none');
      console.log('🔲 清除所有阴影效果');
    }
  }

  console.log('🔲 应用边框样式:', {
    bgColor,
    borderColor,
    borderWidth,
    borderRadius,
    来源: {
      backgroundColor: component.style?.backgroundColor ? 'component.style' : (normalStyle.backgroundColor ? 'normalStyle' : '默认值'),
      borderColor: component.style?.borderColor ? 'component.style' : (normalStyle.borderColor ? 'normalStyle' : '默认值'),
      borderWidth: component.style?.borderWidth !== undefined ? 'component.style' : (normalStyle.borderWidth !== undefined ? 'normalStyle' : '默认值'),
      borderRadius: component.style?.borderRadius !== undefined ? 'component.style' : (normalStyle.borderRadius !== undefined ? 'normalStyle' : '默认值')
    }
  });

  // 🔲 同步边框样式到 normalStyle（确保数据一致性）
  if (!component.properties.normalStyle) {
    component.properties.normalStyle = {};
  }
  component.properties.normalStyle.backgroundColor = bgColor;
  component.properties.normalStyle.borderColor = borderColor;
  component.properties.normalStyle.borderWidth = borderWidth;
  component.properties.normalStyle.borderRadius = borderRadius;

  // 🔲 更新禁用状态
  const isDisabled = properties.disabled || false;
  (element as HTMLButtonElement).disabled = isDisabled;
  element.style.cursor = isDisabled ? 'not-allowed' : 'pointer';
  element.style.opacity = isDisabled ? '0.6' : '1';

  // 🔲 如果按钮被禁用，应用禁用样式
  if (isDisabled && Object.keys(disabledStyle).length > 0) {
    if (disabledStyle.backgroundColor) {
      element.style.backgroundColor = disabledStyle.backgroundColor;
    }
    if (disabledStyle.borderColor) {
      element.style.borderColor = disabledStyle.borderColor;
    }
  }

  // 🔲 存储原始边框样式（用于选中效果）
  element.setAttribute('data-original-border-color', normalStyle.borderColor || '#409eff');
  element.setAttribute('data-original-border-width', (normalStyle.borderWidth || 1) + 'px');

  // 注意：不要克隆和替换节点！这会破坏 setupComponentInteractions 设置的所有事件监听器
  // createButtonElement 中已经设置了动态读取 properties 的事件监听器，会自动应用新样式

  // 🔲 更新修改时间
  component.updated = new Date().toISOString();

  console.log('🔲 更新按钮外观:', {
    text: buttonText,
    disabled: isDisabled,
    normalStyle,
    hoverStyle,
    activeStyle
  });
};

/**
 * 获取按钮预设样式配置
 * @param buttonType 按钮类型：primary, success, warning, danger, info
 * @returns 预设样式配置
 */
export const getButtonPresetStyles = (buttonType: string) => {
  const presets: Record<string, any> = {
    primary: {
      normalStyle: {
        backgroundColor: '#409eff',
        borderColor: '#409eff'
      },
      hoverStyle: {
        backgroundColor: '#66b1ff',
        borderColor: '#66b1ff'
      },
      activeStyle: {
        backgroundColor: '#3a8ee6',
        borderColor: '#3a8ee6'
      }
    },
    success: {
      normalStyle: {
        backgroundColor: '#67c23a',
        borderColor: '#67c23a'
      },
      hoverStyle: {
        backgroundColor: '#85ce61',
        borderColor: '#85ce61'
      },
      activeStyle: {
        backgroundColor: '#5daf34',
        borderColor: '#5daf34'
      }
    },
    warning: {
      normalStyle: {
        backgroundColor: '#e6a23c',
        borderColor: '#e6a23c'
      },
      hoverStyle: {
        backgroundColor: '#ebb563',
        borderColor: '#ebb563'
      },
      activeStyle: {
        backgroundColor: '#cf9236',
        borderColor: '#cf9236'
      }
    },
    danger: {
      normalStyle: {
        backgroundColor: '#f56c6c',
        borderColor: '#f56c6c'
      },
      hoverStyle: {
        backgroundColor: '#f78989',
        borderColor: '#f78989'
      },
      activeStyle: {
        backgroundColor: '#dd6161',
        borderColor: '#dd6161'
      }
    },
    info: {
      normalStyle: {
        backgroundColor: '#909399',
        borderColor: '#909399'
      },
      hoverStyle: {
        backgroundColor: '#a6a9ad',
        borderColor: '#a6a9ad'
      },
      activeStyle: {
        backgroundColor: '#82848a',
        borderColor: '#82848a'
      }
    }
  };

  return presets[buttonType] || presets.primary;
};
