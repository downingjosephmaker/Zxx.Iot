<script setup lang="ts">
import { ref, watch, nextTick, onMounted, onBeforeUnmount } from "vue";
import * as echarts from "echarts";
import type { EChartsOption } from "echarts";
import dayjs from "dayjs";
import { message } from "@/utils/message";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import Search from "~icons/ep/search";
import { getListByPage, type DeviceTypeParamItem } from "@/api/iot/typeparam";
import {
  getDeviceHistory,
  type HistoryResultItem,
  type HistoryPointItem
} from "@/api/iot/monitor";

defineOptions({
  name: "DeviceHistoryTab"
});

const props = defineProps<{
  device: any | null;
}>();

/** 默认近24小时 */
function defaultRange(): [string, string] {
  return [
    dayjs().subtract(24, "hour").format("YYYY-MM-DD HH:mm:ss"),
    dayjs().format("YYYY-MM-DD HH:mm:ss")
  ];
}

const paramOptions = ref<DeviceTypeParamItem[]>([]);
const paramCode = ref("");
const timeRange = ref<[string, string]>(defaultRange());
const mode = ref<"auto" | "raw" | "hour">("auto");
const loading = ref(false);
/** 实际返回的聚合模式(auto由服务端裁决,以响应Mode为准) */
const resultMode = ref<"raw" | "hour" | null>(null);
const points = ref<HistoryPointItem[]>([]);

const chartRef = ref<HTMLElement | null>(null);
let chart: echarts.ECharts | null = null;

/** 按设备产品类型拉点表作为参数下拉 */
async function loadParams() {
  paramOptions.value = [];
  paramCode.value = "";
  if (!props.device?.DeviceTypeCode) return;
  const data = await getListByPage({
    page: 1,
    pagesize: 1000,
    sconlist: [
      {
        ParamName: "DeviceTypeCode",
        ParamType: "=",
        ParamValue: String(props.device.DeviceTypeCode)
      }
    ]
  });
  if (data.Status) {
    paramOptions.value = JSON.parse(data.Result);
  } else {
    message(data.Message, { type: "error" });
  }
}

function paramLabel(code: string): string {
  const p = paramOptions.value.find(item => item.ParamCode === code);
  return p ? `${p.ParamName}(${p.ParamCode})` : code;
}

function renderChart() {
  if (!chartRef.value) return;
  if (!chart) {
    chart = echarts.init(chartRef.value);
  }
  const isHour = resultMode.value === "hour";
  const option: EChartsOption = {
    tooltip: {
      trigger: "axis",
      formatter: (params: any) => {
        const p = Array.isArray(params) ? params[0] : params;
        const point = points.value[p.dataIndex];
        if (!point) return "";
        const lines = [
          dayjs(point.Ts).format("YYYY-MM-DD HH:mm:ss"),
          `${isHour ? "均值" : "值"}: ${point.Value ?? point.ValueStr ?? "-"}`
        ];
        if (isHour) {
          lines.push(`最小: ${point.Min ?? "-"}`);
          lines.push(`最大: ${point.Max ?? "-"}`);
          lines.push(`点数: ${point.Cnt ?? "-"}`);
        }
        return lines.join("<br/>");
      }
    },
    grid: {
      top: "40px",
      left: "3%",
      right: "4%",
      bottom: "48px",
      containLabel: true
    },
    xAxis: {
      type: "category",
      boundaryGap: false,
      data: points.value.map(p =>
        dayjs(p.Ts).format(isHour ? "MM-DD HH:00" : "MM-DD HH:mm:ss")
      )
    },
    yAxis: {
      type: "value",
      scale: true
    },
    dataZoom: [{ type: "inside" }, { type: "slider", bottom: 8 }],
    series: [
      {
        name: paramLabel(paramCode.value),
        type: "line",
        showSymbol: points.value.length <= 100,
        data: points.value.map(p => p.Value ?? null)
      }
    ]
  };
  // notMerge=true: 切换参数后旧series/dataZoom不残留
  chart.setOption(option, true);
  chart.resize();
}

async function onSearch() {
  if (!props.device?.DeviceId) return;
  if (!paramCode.value) {
    message("请先选择参数", { type: "warning" });
    return;
  }
  if (!timeRange.value || timeRange.value.length !== 2) {
    message("请选择时间范围", { type: "warning" });
    return;
  }
  loading.value = true;
  try {
    const data = await getDeviceHistory(
      props.device.DeviceId,
      paramCode.value,
      timeRange.value[0],
      timeRange.value[1],
      mode.value
    );
    if (!data.Status) {
      message(data.Message, { type: "error" });
      return;
    }
    const result: HistoryResultItem = JSON.parse(data.Result);
    resultMode.value = result.Mode;
    points.value = result.Points || [];
    if (points.value.length === 0) {
      chart?.clear();
      return;
    }
    // 图表容器由v-show控制,须等其可见后再init/resize
    await nextTick();
    renderChart();
  } finally {
    loading.value = false;
  }
}

function resetView() {
  points.value = [];
  resultMode.value = null;
  timeRange.value = defaultRange();
  mode.value = "auto";
  chart?.clear();
}

watch(
  () => props.device,
  () => {
    resetView();
    loadParams();
  },
  { immediate: true }
);

const resizeChart = () => {
  chart?.resize();
};

onMounted(() => {
  window.addEventListener("resize", resizeChart);
});

onBeforeUnmount(() => {
  window.removeEventListener("resize", resizeChart);
  if (chart) {
    chart.dispose();
    chart = null;
  }
});
</script>

<template>
  <div class="history-tab">
    <div class="toolbar">
      <el-select
        v-model="paramCode"
        filterable
        placeholder="选择参数"
        class="!w-[240px]"
      >
        <el-option
          v-for="item in paramOptions"
          :key="item.ParamCode"
          :label="`${item.ParamName}(${item.ParamCode})`"
          :value="item.ParamCode"
        />
      </el-select>
      <el-date-picker
        v-model="timeRange"
        type="datetimerange"
        range-separator="至"
        start-placeholder="开始时间"
        end-placeholder="结束时间"
        value-format="YYYY-MM-DD HH:mm:ss"
        class="!w-[380px]"
      />
      <el-select v-model="mode" class="!w-[120px]">
        <el-option label="自动" value="auto" />
        <el-option label="原始点" value="raw" />
        <el-option label="小时聚合" value="hour" />
      </el-select>
      <el-button
        type="primary"
        :icon="useRenderIcon(Search)"
        :loading="loading"
        :disabled="!device"
        @click="onSearch"
      >
        查询
      </el-button>
      <el-tag v-if="resultMode" type="info" effect="light">
        {{ resultMode === "hour" ? "小时聚合" : "原始点" }}
      </el-tag>
    </div>

    <div
      v-show="points.length > 0"
      ref="chartRef"
      v-loading="loading"
      class="history-chart"
    />
    <el-empty
      v-if="points.length === 0"
      :description="
        device ? '暂无数据，请选择参数与时间范围后查询' : '请先选择设备'
      "
      :image-size="90"
    />
  </div>
</template>

<style scoped lang="scss">
.toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
  margin-bottom: 12px;
}

.history-chart {
  width: 100%;
  height: 420px;
}
</style>
