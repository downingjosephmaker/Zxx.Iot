<script setup lang="ts">
import { UtilsEChartsOption, useDark, useECharts } from "@pureadmin/utils";
import {
  type PropType,
  ref,
  computed,
  watch,
  nextTick,
  onMounted,
  onUnmounted
} from "vue";
import "echarts-liquidfill";

const props = defineProps({
  options: {
    type: Object as PropType<UtilsEChartsOption>,
    default: () => {}
  },
  isSquare: {
    type: Boolean,
    default: false
  }
});

const { isDark } = useDark();
const theme = computed(() => (isDark.value ? "dark" : "light"));
const chartRef = ref();
const ChartHook = useECharts(chartRef);
const { setOptions } = ChartHook;

// 单独监听options变化
watch(
  () => props.options,
  async () => {
    await nextTick();
    updateOptions();
  },
  { deep: true, immediate: true }
);

// 监听主题变化
watch(
  () => theme.value,
  () => {
    updateOptions();
  }
);

// 监听isSquare变化
watch(
  () => props.isSquare,
  () => {
    updateChartSize();
  }
);

// 只添加一次resize监听器
onMounted(() => {
  window.addEventListener("resize", updateChart);
  updateChartSize();
});

onUnmounted(() => {
  window.removeEventListener("resize", updateChart);
});

function updateOptions() {
  // 合并主题相关的配置
  const options = { ...props.options };

  // 根据主题调整配置
  if (isDark.value) {
    // 为暗色主题调整颜色等配置
    if (options.backgroundColor === undefined) {
      options.backgroundColor = "rgba(0, 0, 0, 0.2)";
    }
    // 其他暗色主题下的配置调整...
  }

  setOptions(options);
}

function updateChartSize() {
  if (props.isSquare && chartRef.value) {
    chartRef.value.style.height = chartRef.value.clientWidth + "px";
  }
}

function updateChart() {
  if (chartRef.value) {
    const chart = ChartHook.getInstance();
    chart && chart.resize();
  }
}

function getChartHook() {
  return ChartHook;
}

defineExpose({ getChartHook, updateChart });
</script>

<template>
  <div ref="chartRef" style="width: 100%; height: 100%" />
</template>
