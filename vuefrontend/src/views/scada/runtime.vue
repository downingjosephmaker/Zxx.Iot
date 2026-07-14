<template>
  <div class="scada-runtime" :class="{ 'is-report': isReport }">
    <!-- 报表外壳：查询条件栏 + 导出。组态项目不显示（它是常驻实时大屏，无查询与导出语义） -->
    <div v-if="isReport" class="report-toolbar">
      <el-date-picker
        v-model="queryRange"
        type="datetimerange"
        range-separator="至"
        start-placeholder="开始时间"
        end-placeholder="结束时间"
        value-format="YYYY-MM-DD HH:mm:ss"
        :shortcuts="rangeShortcuts"
      />
      <el-button type="primary" :loading="querying" @click="handleQuery">
        查询
      </el-button>
      <div class="toolbar-right">
        <el-button @click="handlePrint">打印</el-button>
        <el-button @click="handleExportExcel">导出 Excel</el-button>
        <el-button :loading="exporting" @click="handleExportPdf">
          导出 PDF
        </el-button>
      </div>
    </div>

    <div
      ref="canvasRef"
      class="runtime-canvas"
      :style="{
        width: canvasWidth + 'px',
        height: canvasHeight + 'px',
        backgroundColor: canvasBackgroundColor,
        backgroundImage: canvasBackgroundImage
          ? `url(${canvasBackgroundImage})`
          : 'none',
        transform: `scale(${canvasZoom / 100})`,
        transformOrigin: 'top left'
      }"
    >
      <!-- 组件由渲染器命令式挂载到此容器(与编辑器同一条创建链路) -->
      <div class="canvas-content" />
    </div>

    <!-- 全屏按钮（报表以打印/导出为主，不需要全屏大屏） -->
    <el-button
      v-if="!isReport"
      class="fullscreen-btn"
      circle
      @click="toggleFullscreen"
    >
      <el-icon><FullScreen /></el-icon>
    </el-button>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from "vue";
import { useRoute } from "vue-router";
import { ElMessage } from "element-plus";
import { FullScreen } from "@element-plus/icons-vue";
import dayjs from "dayjs";
import { createProjectApi, type ProjectKind } from "@/api/scada/project";
import { fuxaMqttService } from "./core/fuxaMqttService";
import { IotDatasetRunner, type IotPointValue } from "./core/DatasetRuntime";
import {
  createRuntimeRenderer,
  applyPointValue,
  startChartComponents,
  stopChartComponents,
  type RuntimeRenderer
} from "./main/utils-runtime";
import {
  applyQueryRange,
  printReport,
  exportTablesToExcel,
  exportCanvasToPdf
} from "./main/utils-report";

defineOptions({
  name: "ScadaRuntime"
});

const route = useRoute();

/**
 * 组态项目与报表项目共用同一个运行态路由(/scada/runtime/:id)，仅 query.kind 不同。
 * 两者间跳转时 Vue 会复用组件实例、不重跑 setup，因此这些必须是 computed 而非一次性常量，
 * 并 watch 路由变化重新装载——否则从报表跳到组态会挂着报表工具栏、还读错后端接口。
 */
const projectId = computed(() => route.params.id as string);
const projectKind = computed<ProjectKind>(() =>
  route.query.kind === "dash" ? "dash" : "scada"
);
const projectApi = computed(() => createProjectApi(projectKind.value));
/** 报表：带查询条件栏与导出；组态：常驻实时大屏 */
const isReport = computed(() => projectKind.value === "dash");

const canvasRef = ref<HTMLElement | null>(null);
const canvasWidth = ref(1920);
const canvasHeight = ref(1080);
const canvasZoom = ref(100);
const canvasBackgroundColor = ref("#f5f5f5");
const canvasBackgroundImage = ref("");
const loading = ref(false);

/** 组件为普通数组：运行态由渲染器直接操作 DOM，无需响应式（响应式重渲会毁掉图表实例与动画） */
let components: any[] = [];
const datasetList = ref<any[]>([]);
let renderer: RuntimeRenderer | null = null;
let iotRunner: IotDatasetRunner | null = null;

/**
 * 加载项目并渲染
 */
const loadProject = async () => {
  try {
    loading.value = true;

    // 1. 获取项目完整数据（基本信息 + ContentData 组态内容）
    const response = await projectApi.value.getDataInfo(projectId.value);
    if (!response.Status || !response.Result) {
      throw new Error("获取项目数据失败");
    }
    const info = JSON.parse(response.Result);
    if (!info.ContentData) {
      throw new Error("项目暂无组态内容，请先在编辑器中保存");
    }
    projectName.value =
      info.ProjectName || (isReport.value ? "报表" : "组态");

    // 2. 解析组态内容（编辑器保存结构：{ settings, components, devices, datasets }）
    const projectJson = JSON.parse(info.ContentData);

    // 3. 设置画布
    canvasWidth.value = projectJson.settings?.canvasWidth || 1920;
    canvasHeight.value = projectJson.settings?.canvasHeight || 1080;
    canvasBackgroundColor.value =
      projectJson.settings?.backgroundColor || "#f5f5f5";
    canvasBackgroundImage.value = projectJson.settings?.backgroundImage || "";

    components = projectJson.components || [];
    datasetList.value = projectJson.datasets || [];

    // 报表默认查询最近24小时（用户可在条件栏改后重查）
    if (isReport.value) {
      queryRange.value = [
        fmt(dayjs().subtract(1, "day").toDate()),
        fmt(new Date())
      ];
      applyQueryRange(
        datasetList.value,
        queryRange.value[0],
        queryRange.value[1]
      );
    }

    // 4. 渲染组件（复用编辑器创建链路，交互为空实现）
    await nextTick();
    renderer = createRuntimeRenderer(canvasRef);
    components.forEach(comp => renderer!.render(comp));

    // 5. 图表接线（取数 + 定时刷新）
    await startChartComponents(components, datasetList, renderer);

    // 6. 启动IoT点位数据集（最新值铺底+SignalR增量，驱动绑定组件刷新）
    const iotDatasets = datasetList.value.filter((d: any) => d.type === "iot");
    if (iotDatasets.length) {
      iotRunner = new IotDatasetRunner(iotDatasets, applyDatasetValues);
      iotRunner.start();
    }

    // 7. 连接MQTT（如果有设备配置）
    if (projectJson.devices?.length > 0) {
      await connectMqtt(projectJson.devices);
    }

    // 8. 自动缩放适配
    autoScale();

    // 9. 通知父页面加载完成
    notifyParent("SCADA_RUNTIME_LOADED", {
      projectId: projectId.value,
      componentCount: components.length
    });
  } catch (error) {
    console.error("加载项目失败:", error);
    ElMessage.error("加载失败: " + (error as Error).message);
    notifyParent("SCADA_RUNTIME_ERROR", { error: (error as Error).message });
  } finally {
    loading.value = false;
  }
};

/**
 * IoT数据集值到达：刷新绑定该数据集的组件。
 * 组件绑定形状 comp.dataBinding = { datasetId, dataPath|paramCode }，dataPath=点位编码。
 */
const applyDatasetValues = (
  datasetId: string,
  values: Record<string, IotPointValue>
) => {
  if (!renderer) return;
  components.forEach(comp => {
    const binding = comp.dataBinding;
    if (!binding || binding.datasetId !== datasetId) return;
    const code = binding.dataPath || binding.paramCode;
    const v = code ? values[code] : undefined;
    if (v === undefined) return;
    applyPointValue(comp, v.value, v.unit || "", renderer!);
  });
};

/* ─────────────── 报表运行态：查询条件与导出 ─────────────── */

const projectName = ref("报表");
const querying = ref(false);
const exporting = ref(false);
/** 默认查询区间：最近 24 小时 */
const queryRange = ref<[string, string]>(["", ""]);

const fmt = (d: Date) => dayjs(d).format("YYYY-MM-DD HH:mm:ss");

const rangeShortcuts = [
  {
    text: "最近24小时",
    value: () => [dayjs().subtract(1, "day").toDate(), new Date()]
  },
  {
    text: "最近7天",
    value: () => [dayjs().subtract(7, "day").toDate(), new Date()]
  },
  {
    text: "最近30天",
    value: () => [dayjs().subtract(30, "day").toDate(), new Date()]
  }
];

/** 按查询条件栏的时间区间重新出数（历史数据集按该区间取，图表随之刷新） */
const handleQuery = async () => {
  if (!renderer) return;
  const [start, end] = queryRange.value;
  if (!start || !end) {
    ElMessage.warning("请先选择查询时间范围");
    return;
  }
  querying.value = true;
  try {
    stopChartComponents(components);
    applyQueryRange(datasetList.value, start, end);
    await startChartComponents(components, datasetList, renderer);
  } finally {
    querying.value = false;
  }
};

const handlePrint = () => {
  if (canvasRef.value) printReport(canvasRef.value);
};

const handleExportExcel = () => {
  if (!canvasRef.value) return;
  const ok = exportTablesToExcel(canvasRef.value, projectName.value);
  if (!ok) ElMessage.warning("报表中没有表格组件，无可导出的数据");
};

const handleExportPdf = async () => {
  if (!canvasRef.value) return;
  exporting.value = true;
  try {
    await exportCanvasToPdf(canvasRef.value, projectName.value);
  } catch (error) {
    ElMessage.error("导出PDF失败: " + (error as Error).message);
  } finally {
    exporting.value = false;
  }
};

/**
 * 连接MQTT
 */
const connectMqtt = async (devices: any[]) => {
  try {
    await fuxaMqttService.connect();

    // 订阅所有设备的主题
    devices.forEach(device => {
      if (device.type === "mqtt" && device.enabled) {
        device.connection.topics?.forEach((topic: string) => {
          fuxaMqttService.subscribe(topic, 0);
        });
      }
    });
  } catch (error) {
    console.warn("MQTT连接失败:", error);
  }
};

/**
 * 全屏切换
 */
const toggleFullscreen = () => {
  const elem = document.querySelector(".scada-runtime") as HTMLElement;
  if (!document.fullscreenElement) {
    elem.requestFullscreen();
  } else {
    document.exitFullscreen();
  }
};

/**
 * 自动缩放适配
 */
const autoScale = () => {
  // 报表保持 100% 原尺寸：缩放会让打印与 PDF 导出失真，页面改为可滚动
  if (isReport.value) return;

  const container = document.querySelector(".scada-runtime") as HTMLElement;
  if (!container) return;

  const scaleX = container.clientWidth / canvasWidth.value;
  const scaleY = container.clientHeight / canvasHeight.value;
  canvasZoom.value = Math.min(scaleX, scaleY) * 100;
};

/**
 * 与父页面通信（iframe场景）
 */
const notifyParent = (type: string, data: any) => {
  if (window.parent !== window) {
    window.parent.postMessage({ type, data }, "*");
  }
};

/** 卸下当前项目：停订阅与定时器、清空已挂载的组件 DOM */
const teardown = () => {
  iotRunner?.stop();
  iotRunner = null;
  stopChartComponents(components);
  fuxaMqttService.disconnect();
  const content = canvasRef.value?.querySelector(".canvas-content");
  if (content) content.innerHTML = "";
  components = [];
  datasetList.value = [];
  renderer = null;
};

onMounted(async () => {
  await loadProject();
  window.addEventListener("resize", autoScale);
});

/**
 * 组态与报表共用本路由，互相跳转时 Vue 复用组件实例、不重跑 setup，
 * 必须在此重新装载，否则会残留上一个项目的组件与订阅。
 */
watch(
  () => route.fullPath,
  async () => {
    teardown();
    await loadProject();
  }
);

onUnmounted(() => {
  window.removeEventListener("resize", autoScale);
  teardown();
});
</script>

<style scoped lang="scss">
.scada-runtime {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100vh;
  overflow: hidden;
  background: #f5f5f5;

  /* 报表：条件栏在上、画布原尺寸可滚动（不缩放，保证打印与PDF清晰） */
  &.is-report {
    flex-direction: column;
    align-items: stretch;
    justify-content: flex-start;
    overflow: auto;
  }

  .report-toolbar {
    position: sticky;
    top: 0;
    z-index: 20;
    display: flex;
    flex-shrink: 0;
    gap: 12px;
    align-items: center;
    padding: 12px 16px;
    background: #fff;
    box-shadow: 0 1px 4px rgb(0 0 0 / 8%);

    .toolbar-right {
      display: flex;
      gap: 8px;
      margin-left: auto;
    }
  }

  .runtime-canvas {
    position: relative;
    flex-shrink: 0;
    box-shadow: 0 2px 12px rgb(0 0 0 / 10%);
    transition: transform 0.3s ease;
  }

  &.is-report .runtime-canvas {
    margin: 16px auto;
  }

  /* 组件挂载点：编辑器渲染链路以 .canvas-content 为容器，组件绝对定位于此 */
  .canvas-content {
    position: relative;
    width: 100%;
    height: 100%;
  }

  .fullscreen-btn {
    position: fixed;
    right: 20px;
    bottom: 20px;
    z-index: 1000;
    background: rgb(255 255 255 / 90%);

    &:hover {
      background: rgb(255 255 255 / 100%);
    }
  }
}
</style>
