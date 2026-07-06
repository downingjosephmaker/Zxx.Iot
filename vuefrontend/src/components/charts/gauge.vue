<script setup lang="ts">
import { UtilsEChartsOption, useDark, useECharts } from "@pureadmin/utils";
import { type PropType, ref, computed, watch, nextTick } from "vue";
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
// const { setOptions } = useECharts(chartRef, {
//   theme
// });//
const ChartHook = useECharts(chartRef);
const { setOptions } = ChartHook;
watch(
  () => props,
  async () => {
    await nextTick();
    setOptions(props.options);
    if (props.isSquare) {
      chartRef.value &&
        (chartRef.value.style.height = chartRef.value.clientWidth + "px");
    }
  },
  {
    deep: true,
    immediate: true
  }
);
function getChartHook() {
  return ChartHook;
}
defineExpose({ getChartHook });
</script>

<template>
  <div ref="chartRef" style="width: 100%; height: 100%" />
</template>
