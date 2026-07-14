<template>
  <div class="scada-runtime">
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

    <!-- 全屏按钮 -->
    <el-button class="fullscreen-btn" circle @click="toggleFullscreen">
      <el-icon><FullScreen /></el-icon>
    </el-button>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from "vue";
import { useRoute } from "vue-router";
import { ElMessage } from "element-plus";
import { FullScreen } from "@element-plus/icons-vue";
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

defineOptions({
  name: "ScadaRuntime"
});

const route = useRoute();
const projectId = route.params.id as string;
/** 组态项目与报表项目共用运行态，kind 决定从哪套接口读项目 */
const projectKind: ProjectKind =
  route.query.kind === "dash" ? "dash" : "scada";
const projectApi = createProjectApi(projectKind);

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
    const response = await projectApi.getDataInfo(projectId);
    if (!response.Status || !response.Result) {
      throw new Error("获取项目数据失败");
    }
    const info = JSON.parse(response.Result);
    if (!info.ContentData) {
      throw new Error("项目暂无组态内容，请先在编辑器中保存");
    }

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
      projectId,
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

onMounted(async () => {
  await loadProject();
  window.addEventListener("resize", autoScale);
});

onUnmounted(() => {
  window.removeEventListener("resize", autoScale);
  fuxaMqttService.disconnect();
  iotRunner?.stop();
  iotRunner = null;
  stopChartComponents(components);
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

  .runtime-canvas {
    position: relative;
    box-shadow: 0 2px 12px rgb(0 0 0 / 10%);
    transition: transform 0.3s ease;
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
