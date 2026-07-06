<template>
  <div
    :class="['svg-icon-wrapper', customClass]"
    :style="iconStyle"
    v-bind="$attrs"
    ref="svgContainer"
  >
    <!-- 内联SVG将在这里动态渲染 -->
  </div>
</template>

<script setup lang="ts">
import { computed, CSSProperties, ref, onMounted, watch } from 'vue';
import { svgManager } from '../core/SvgManager';

interface Props {
  name?: string;
  size?: string | number;
  color?: string;
  customClass?: string;
  animation?: 'rotate' | 'pulse' | 'blink' | 'bounce' | 'shake' | 'none';
  animationSpeed?: 'slow' | 'normal' | 'fast';
  hoverEffect?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  size: '20px',
  color: 'currentColor',
  customClass: '',
  name: '',
  animation: 'none',
  animationSpeed: 'normal',
  hoverEffect: true
});

const svgContainer = ref<HTMLElement>();

// 计算图标样式
const iconStyle = computed((): CSSProperties => {
  const size = typeof props.size === 'number' ? `${props.size}px` : props.size;
  return {
    width: size,
    height: size,
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center'
  };
});

// 渲染内联SVG - 简化为只调用SvgManager
const renderInlineSvg = () => {
  if (!svgContainer.value || !props.name) return;

  try {
    // 清空容器
    svgContainer.value.innerHTML = '';

    // 直接使用SvgManager创建SVG，传递所有选项
    const svgElement = svgManager.createInlineSvg(props.name, {
      width: typeof props.size === 'number' ? props.size : parseInt(props.size) || 20,
      height: typeof props.size === 'number' ? props.size : parseInt(props.size) || 20,
      fillColor: props.color !== 'currentColor' ? props.color : undefined,
      animation: props.animation,
      animationSpeed: props.animationSpeed,
      hoverEffect: props.hoverEffect,
      className: props.customClass
    });

    // 将SvgManager创建的元素添加到容器
    svgContainer.value.appendChild(svgElement);
  } catch (error) {
    console.warn('SvgIcon 渲染失败:', props.name, error);
    // 显示错误占位符
    if (svgContainer.value) {
      svgContainer.value.innerHTML = `
        <div style="
          width: 100%;
          height: 100%;
          display: flex;
          align-items: center;
          justify-content: center;
          color: #999;
          font-size: 12px;
        ">
          ⚠
        </div>
      `;
    }
  }
};

// 监听属性变化
watch([() => props.name, () => props.color, () => props.size], () => {
  renderInlineSvg();
});

// 组件挂载后渲染SVG
onMounted(() => {
  renderInlineSvg();
});
</script>

<style scoped lang="scss">
.svg-icon-wrapper {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  
  .svg-icon {
    width: 100%;
    height: 100%;
    object-fit: contain;
    filter: brightness(1.1) contrast(1.1);
    transition: all 0.2s ease;
  }
  
  &:hover .svg-icon {
    filter: brightness(1.3) contrast(1.2);
    transform: scale(1.05);
  }
}
</style>