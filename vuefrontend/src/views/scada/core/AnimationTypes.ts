/**
 * SVG动画类型定义和名称映射
 */

// 动画类型值
export type AnimationType =
  | 'none'
  | 'rotate'
  | 'pulse'
  | 'blink'
  | 'bounce'
  | 'shake'
  | 'scale'
  | 'moveX'
  | 'moveY'
  | 'fade'
  | 'liquidFill'
  | 'liquidDrain'
  | 'pipeFlow'
  | 'switchToggle';

// 动画类型选项接口
export interface AnimationOption {
  label: string;
  value: AnimationType;
  description?: string;
}

// 动画类型选项列表
export const animationOptions: AnimationOption[] = [
  { label: '无动画', value: 'none' },
  { label: '旋转', value: 'rotate', description: '围绕中心点旋转' },
  { label: '脉动', value: 'pulse', description: '缩放脉动效果' },
  { label: '闪烁', value: 'blink', description: '透明度闪烁' },
  { label: '弹跳', value: 'bounce', description: '上下弹跳' },
  { label: '摇摆', value: 'shake', description: '左右摇摆' },
  { label: '缩放', value: 'scale', description: '放大缩小' },
  { label: '左右移动', value: 'moveX', description: '水平移动' },
  { label: '上下移动', value: 'moveY', description: '垂直移动' },
  { label: '渐隐渐现', value: 'fade', description: '透明度渐变' },
  { label: '液体上涨', value: 'liquidFill', description: '容器液体逐渐上涨' },
  { label: '液体下降', value: 'liquidDrain', description: '容器液体逐渐下降' },
  { label: '管道流动', value: 'pipeFlow', description: '管道中流体流动效果' },
  { label: '开关切换', value: 'switchToggle', description: '开关状态切换动画' }
];

// 动画类型名称映射
export const animationTypeNames: Record<AnimationType, string> = {
  'none': '无动画',
  'rotate': '旋转',
  'pulse': '脉动',
  'blink': '闪烁',
  'bounce': '弹跳',
  'shake': '摇摆',
  'scale': '缩放',
  'moveX': '左右移动',
  'moveY': '上下移动',
  'fade': '渐隐渐现',
  'liquidFill': '液体上涨',
  'liquidDrain': '液体下降',
  'pipeFlow': '管道流动',
  'switchToggle': '开关切换'
};

/**
 * 获取动画类型的中文名称
 * @param value 动画类型值
 * @returns 中文名称，如果找不到则返回原值
 */
export function getAnimationTypeName(value: string | undefined | null): string {
  if (!value) return '无动画';
  return animationTypeNames[value as AnimationType] || value;
}

/**
 * 根据值获取动画选项对象
 * @param value 动画类型值
 * @returns 动画选项对象，如果找不到则返回默认值
 */
export function getAnimationOption(value: string | undefined | null): AnimationOption {
  const option = animationOptions.find(opt => opt.value === value);
  return option || animationOptions[0]; // 默认返回"无动画"
}
