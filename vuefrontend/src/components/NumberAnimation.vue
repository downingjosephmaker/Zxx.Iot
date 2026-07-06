<template>
  <span class="number-container">
    {{ displayValue }}
  </span>
</template>

<script setup>
import { ref, watch, onMounted } from "vue";

const props = defineProps({
  value: {
    type: Number,
    required: true,
    default: 0
  },
  duration: {
    type: Number,
    default: 1000
  },
  decimals: {
    type: Number,
    default: 0
  },
  easing: {
    type: String,
    default: "easeOutCubic",
    validator: value =>
      ["linear", "easeOutCubic", "easeInOutQuart"].includes(value)
  },
  locale: {
    type: String,
    default: "zh-CN"
  }
});

// 显示值
const displayValue = ref("0");

// 缓动函数
const easingFunctions = {
  linear: t => t,
  easeOutCubic: t => 1 - Math.pow(1 - t, 3),
  easeInOutQuart: t =>
    t < 0.5 ? 8 * t * t * t * t : 1 - Math.pow(-2 * t + 2, 4) / 2
};

// 动画函数
const animateValue = (start, end, duration) => {
  if (start === end) {
    displayValue.value = formatNumber(end);
    return;
  }

  let startTimestamp = null;
  const easingFunction = easingFunctions[props.easing];

  // 添加随机小变化，防止动画完全相同
  const randomOffset = Math.random() * 0.2 - 0.1; // -0.1到0.1的随机值
  const adjustedDuration = duration * (1 + randomOffset);

  const step = timestamp => {
    if (!startTimestamp) startTimestamp = timestamp;
    const elapsed = timestamp - startTimestamp;

    // 计算完成百分比
    const progress = Math.min(elapsed / adjustedDuration, 1);
    const easedProgress = easingFunction(progress);

    // 计算当前值
    const currentValue = start + (end - start) * easedProgress;

    // 更新显示值
    displayValue.value = formatNumber(currentValue);

    // 继续动画
    if (progress < 1) {
      window.requestAnimationFrame(step);
    } else {
      // 确保最终值是准确的
      displayValue.value = formatNumber(end);
    }
  };

  window.requestAnimationFrame(step);
};

// 格式化数字
const formatNumber = num => {
  return num.toLocaleString(props.locale, {
    minimumFractionDigits: props.decimals,
    maximumFractionDigits: props.decimals
  });
};

// 监听值变化
watch(
  () => props.value,
  (newVal, oldVal) => {
    if (newVal !== oldVal) {
      animateValue(oldVal, newVal, props.duration);
    }
  },
  { immediate: false }
);

// 初始化
onMounted(() => {
  // 初始显示，不带动画
  displayValue.value = formatNumber(props.value);

  // 启动首次动画效果
  if (props.value !== 0) {
    animateValue(0, props.value, props.duration);
  }
});
</script>

<style scoped>
.number-container {
  display: inline-block;
  font-family: "Roboto Mono", monospace;
  font-feature-settings: "tnum";
  position: relative;
  overflow: hidden;
}

.number-container::after {
  content: "";
  position: absolute;
  width: 100%;
  height: 100%;
  top: 0;
  left: 0;
  pointer-events: none;
  background-image: radial-gradient(
    circle,
    rgba(255, 255, 255, 0.3) 0%,
    transparent 70%
  );
  opacity: 0;
  transition: opacity 0.3s ease;
}

.number-container:hover::after {
  opacity: 0.2;
}
</style>
