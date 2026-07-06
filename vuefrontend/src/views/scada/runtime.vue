<template>
  <div class="scada-runtime">
    <div
      class="runtime-canvas"
      :style="{
        width: canvasWidth + 'px',
        height: canvasHeight + 'px',
        backgroundColor: canvasBackgroundColor,
        backgroundImage: canvasBackgroundImage ? `url(${canvasBackgroundImage})` : 'none',
        transform: `scale(${canvasZoom / 100})`,
        transformOrigin: 'top left'
      }"
    >
      <!-- 渲染组件 -->
      <div
        v-for="comp in components"
        :key="comp.id"
        :id="comp.id"
        class="runtime-component"
        :style="{
          position: 'absolute',
          left: comp.position.x + 'px',
          top: comp.position.y + 'px',
          width: comp.size.width + 'px',
          height: comp.size.height + 'px',
          zIndex: comp.zIndex || 1
        }"
        v-html="comp.rendered"
      />
    </div>

    <!-- 全屏按钮 -->
    <el-button
      class="fullscreen-btn"
      circle
      @click="toggleFullscreen"
    >
      <el-icon><FullScreen /></el-icon>
    </el-button>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from "vue";
import { useRoute } from "vue-router";
import { ElMessage } from "element-plus";
import { FullScreen } from "@element-plus/icons-vue";
import scadaApi from "@/api/scada/project";
import { fuxaMqttService } from "./core/fuxaMqttService";

defineOptions({
  name: "ScadaRuntime"
});

const route = useRoute();
const projectId = route.params.id as string;

const canvasWidth = ref(1920);
const canvasHeight = ref(1080);
const canvasZoom = ref(100);
const canvasBackgroundColor = ref("#f5f5f5");
const canvasBackgroundImage = ref("");
const components = ref([]);
const loading = ref(false);

/**
 * 加载项目数据
 */
const loadProject = async () => {
  try {
    loading.value = true;

    // 1. 获取项目完整数据（基本信息 + ContentData 组态内容）
    const response = await scadaApi.getDataInfo(projectId);
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
    canvasBackgroundColor.value = projectJson.settings?.backgroundColor || "#f5f5f5";
    canvasBackgroundImage.value = projectJson.settings?.backgroundImage || "";

    // 4. 加载组件（简化渲染，只显示基本内容）
    components.value = (projectJson.components || []).map(comp => ({
      ...comp,
      rendered: renderComponent(comp)
    }));

    // 6. 连接MQTT（如果有设备配置）
    if (projectJson.devices?.length > 0) {
      await connectMqtt(projectJson.devices);
    }

    // 7. 自动缩放适配
    autoScale();

    // 8. 通知父页面加载完成
    notifyParent("SCADA_RUNTIME_LOADED", {
      projectId,
      componentCount: components.value.length
    });

    ElMessage.success("项目加载成功");
  } catch (error) {
    console.error("加载项目失败:", error);
    ElMessage.error("加载失败: " + (error as Error).message);
    notifyParent("SCADA_RUNTIME_ERROR", { error: (error as Error).message });
  } finally {
    loading.value = false;
  }
};

/**
 * 简化的组件渲染函数（运行时模式）
 */
const renderComponent = (comp: any): string => {
  switch (comp.type) {
    case "text":
      return `<div style="width:100%;height:100%;display:flex;align-items:center;justify-content:center;font-size:${comp.properties?.fontSize || 14}px;color:${comp.properties?.color || '#000'}">${comp.properties?.text || ""}</div>`;

    case "image":
      return `<img src="${comp.properties?.src || ""}" style="width:100%;height:100%;object-fit:${comp.properties?.objectFit || 'contain'}" />`;

    case "video":
      return `<video src="${comp.properties?.src || ""}" style="width:100%;height:100%" ${comp.properties?.autoplay ? "autoplay" : ""} ${comp.properties?.loop ? "loop" : ""} ${comp.properties?.controls ? "controls" : ""}></video>`;

    default:
      // 其他组件类型的基础渲染
      return `<div style="width:100%;height:100%;background:${comp.style?.backgroundColor || 'transparent'};border:${comp.style?.borderWidth || 0}px solid ${comp.style?.borderColor || '#ccc'}"></div>`;
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
});
</script>

<style scoped lang="scss">
.scada-runtime {
  width: 100%;
  height: 100vh;
  overflow: hidden;
  position: relative;
  background: #f5f5f5;
  display: flex;
  align-items: center;
  justify-content: center;

  .runtime-canvas {
    transition: transform 0.3s ease;
    position: relative;
    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  }

  .runtime-component {
    pointer-events: auto;
  }

  .fullscreen-btn {
    position: fixed;
    bottom: 20px;
    right: 20px;
    z-index: 1000;
    background: rgba(255, 255, 255, 0.9);

    &:hover {
      background: rgba(255, 255, 255, 1);
    }
  }
}
</style>
