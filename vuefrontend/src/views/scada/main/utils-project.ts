/**
 * 项目保存/加载/导入/导出/自动保存相关工具函数
 */
import { ref, reactive, type Ref } from "vue";
import { ElMessage, ElMessageBox } from "element-plus";
import type { Router } from "vue-router";
import html2canvas from "html2canvas";
import scadaApi, { type ScadaProjectInfo } from "@/api/scada/project";

/**
 * 生成画布截图(Base64格式)
 * @param canvasElement 画布DOM元素
 * @param maxWidth 最大宽度(默认400)
 * @param maxHeight 最大高度(默认300)
 * @returns Base64字符串(不包含data:image前缀)
 */
export const captureCanvasScreenshot = async (
  canvasElement: HTMLElement,
  maxWidth: number = 400,
  maxHeight: number = 300
): Promise<{ base64: string; imageType: string }> => {
  try {
    const canvas = await html2canvas(canvasElement, {
      backgroundColor: null,
      scale: 0.5,
      logging: false,
      useCORS: true,
      // 使用 foreignObject 渲染以避免不支持的颜色函数解析
      foreignObjectRendering: true,
      // 在克隆文档阶段对样式进行清理，替换 oklch 颜色以避免解析错误
      onclone: clonedDoc => {
        try {
          const styleEls = clonedDoc.querySelectorAll("style");
          styleEls.forEach(styleEl => {
            const txt = styleEl.textContent || "";
            if (txt.includes("oklch(")) {
              // 将不支持的 oklch(...) 颜色函数替换为安全的十六进制颜色
              styleEl.textContent = txt.replace(/oklch\([^)]*\)/g, "#000000");
            }
          });
        } catch (e) {
          // 清理过程允许失败，不影响截图
          console.warn("onclone 样式清理警告:", e);
        }
      }
    });
    let width = canvas.width;
    let height = canvas.height;
    const ratio = Math.min(maxWidth / width, maxHeight / height, 1);
    width = Math.floor(width * ratio);
    height = Math.floor(height * ratio);
    const tempCanvas = document.createElement("canvas");
    tempCanvas.width = width;
    tempCanvas.height = height;
    const ctx = tempCanvas.getContext("2d");
    if (ctx) {
      ctx.drawImage(canvas, 0, 0, width, height);
    }
    const base64Data = tempCanvas.toDataURL("image/jpeg", 0.8);
    const base64 = base64Data.replace(/^data:image\/jpeg;base64,/, "");
    return { base64, imageType: "jpg" };
  } catch (error) {
    console.error("画布截图失败:", error);
    throw new Error("生成缩略图失败");
  }
};

/**
 * 保存项目
 * - projectInfo 就是 ScadaProjectInfo（项目基本信息）
 * - projectData 就是 ContentData（画布配置和组件数据）
 */
export const saveProject = async (
  loading: Ref<boolean>,
  projectInfo: Ref<ScadaProjectInfo>,
  projectData: any,
  isSaved: Ref<boolean>
) => {
  try {
    loading.value = true;
    // 1. 检查项目ID
    if (!projectInfo.value.SnowId) {
      ElMessage.error("请先从项目列表打开项目");
      return;
    }

    // 2. 生成缩略图
    const canvasElement = document.querySelector(
      ".canvas-content"
    ) as HTMLElement;
    let thumbnailUrl = projectInfo.value.Thumbnail || "";

    if (canvasElement) {
      try {
        const { base64, imageType } =
          await captureCanvasScreenshot(canvasElement);
        const datas = {
          Base64String: base64,
          ImageType: imageType,
          ImageName: ""
        };
        const uploadRes = await scadaApi.uploadBase64Image(datas);
        if (uploadRes.Status && uploadRes.Result) {
          thumbnailUrl = uploadRes.Message || "";
        }
      } catch (error) {
        console.warn("生成缩略图失败，继续保存:", error);
      }
    }

    // 3. 直接保存 projectData（包含 settings、components、devices、datasets）
    const response = await scadaApi.saveProjectData({
      ProjectId: projectInfo.value.SnowId,
      ContentData: JSON.stringify(projectData.value, null, 2),
      Thumbnail: thumbnailUrl
    });

    if (response.Status) {
      isSaved.value = true;
      ElMessage.success("项目保存成功");
    } else {
      ElMessage.error(response.Message || "保存失败");
    }
  } catch (error) {
    console.error("保存项目失败:", error);
    ElMessage.error("保存失败: " + (error as Error).message);
  } finally {
    loading.value = false;
  }
};
/**
 * 从数据库加载项目
 * - 获取 ScadaProjectInfo（项目基本信息）
 * - 解析 ContentData 到 projectData（画布配置和组件数据）
 */
export const loadProject = async (
  projectId: string,
  loading: Ref<boolean>,
  projectInfo: Ref<ScadaProjectInfo>,
  projectData: any,
  isSaved: Ref<boolean>,
  redrawCanvas: () => void,
  nextTick: any
) => {
  try {
    loading.value = true;

    // 1. 调用getDataInfo获取项目完整数据
    const response = await scadaApi.getDataInfo(projectId);
    if (!response.Status || !response.Result) {
      ElMessage.error("项目不存在或加载失败");
      return;
    }

    const data = JSON.parse(response.Result);
    // 2. projectInfo 直接就是 ScadaProjectInfo
    projectInfo.value = {
      SnowId: data.SnowId,
      ProjectName: data.ProjectName,
      ProjectDesc: data.ProjectDesc,
      ProjectStatus: data.ProjectStatus,
      Thumbnail: data.Thumbnail,
      UnitId: data.UnitId,
      ExpandJson: data.ExpandJson,
      CreateId: data.CreateId,
      CreateTime: data.CreateTime,
      CreateName: data.CreateName,
      UpdateId: data.UpdateId,
      UpdateTime: data.UpdateTime,
      UpdateName: data.UpdateName
    };

    // 3. 解析 ContentData 到 projectData
    if (data.ContentData) {
      try {
        projectData.value = JSON.parse(data.ContentData);
      } catch (error) {
        console.error("解析项目数据失败:", error);
        ElMessage.warning("项目数据格式异常，使用默认配置");
        projectData.value = { ...DEFAULT_PROJECT_DATA };
      }
    }

    // 4. 渲染画布
    await nextTick();
    redrawCanvas();
    isSaved.value = true;
    const componentCount = projectData.value.components?.length || 0;
    ElMessage.success(`项目加载成功 (组件数: ${componentCount})`);
  } catch (error) {
    console.error("加载项目失败:", error);
    ElMessage.error("加载失败: " + (error as Error).message);

    // 失败时初始化默认数据
    projectData.value = { ...DEFAULT_PROJECT_DATA };
  } finally {
    loading.value = false;
  }
};

/**
 * 初始化新项目
 * 将所有配置重置为默认值
 */
export const initializeNewProject = (
  projectInfo: Ref<ScadaProjectInfo>,
  projectData: any,
  isSaved: Ref<boolean>
) => {
  // 重置项目基本信息
  projectInfo.value = {
    SnowId: "",
    ProjectName: "未命名项目",
    ProjectDesc: "",
    ProjectStatus: 0,
    UnitId: 0
  };

  // 重置项目数据为默认值
  projectData.value = { ...DEFAULT_PROJECT_DATA };

  // 标记为未保存
  isSaved.value = false;
};

/**
 * 处理图片上传并创建组件
 */
export const handleImageUpload = async (
  event: Event,
  projectInfo: any,
  loading: Ref<boolean>,
  addComponentToCanvas: (component: any) => void,
  position?: { x: number; y: number }
) => {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (!file) return;

  // 验证文件类型
  if (!file.type.startsWith("image/")) {
    ElMessage.error("请选择图片文件");
    return;
  }

  // 验证文件大小（10MB）
  if (file.size > 10 * 1024 * 1024) {
    ElMessage.error("图片大小不能超过10MB");
    return;
  }

  // 如果项目未保存，先保存项目
  if (!projectInfo.value.SnowId) {
    ElMessage.warning("请先保存项目后再上传资源");
    return;
  }

  try {
    loading.value = true;
    // 上传到服务器
    const response = await scadaApi.uploadResource(
      file,
      projectInfo.value.SnowId,
      "image"
    );
    if (response.success) {
      const resourceData = response.data;
      // 创建图片元素获取尺寸
      const img = new Image();
      img.onload = () => {
        // 计算显示尺寸
        const maxSize = 300;
        let displayWidth = img.width;
        let displayHeight = img.height;

        if (displayWidth > maxSize || displayHeight > maxSize) {
          const ratio = Math.min(
            maxSize / displayWidth,
            maxSize / displayHeight
          );
          displayWidth = Math.round(displayWidth * ratio);
          displayHeight = Math.round(displayHeight * ratio);
        }

        // 创建图片组件
        const imageComponent = {
          id: `image_${Date.now()}`,
          type: "image",
          name: resourceData.fileName,
          position: position || { x: 100, y: 100 },
          size: { width: displayWidth, height: displayHeight },
          properties: {
            src: resourceData.url, // 使用服务器返回的URL
            alt: resourceData.fileName,
            objectFit: "contain"
          },
          imageData: {
            fileName: resourceData.fileName,
            filePath: resourceData.filePath,
            url: resourceData.url,
            size: resourceData.size,
            originalWidth: img.width,
            originalHeight: img.height
          }
        };

        addComponentToCanvas(imageComponent);
        ElMessage.success("图片上传成功");
      };

      img.onerror = () => {
        ElMessage.error("图片加载失败");
      };

      // 设置图片源（使用服务器URL）
      img.src = resourceData.url;
    } else {
      ElMessage.error(response.message || "上传失败");
    }
  } catch (error) {
    ElMessage.error("上传失败: " + (error as Error).message);
  } finally {
    loading.value = false;
  }
};

/**
 * 处理视频上传并创建组件
 */
export const handleVideoUpload = async (
  event: Event,
  projectInfo: any,
  loading: Ref<boolean>,
  addComponentToCanvas: (component: any) => void,
  position?: { x: number; y: number }
) => {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (!file) return;

  // 验证文件类型
  if (!file.type.startsWith("video/")) {
    ElMessage.error("请选择视频文件");
    return;
  }

  // 验证文件大小（100MB）
  if (file.size > 100 * 1024 * 1024) {
    ElMessage.error("视频大小不能超过100MB");
    return;
  }

  if (!projectInfo.value.SnowId) {
    ElMessage.warning("请先保存项目后再上传资源");
    return;
  }

  try {
    loading.value = true;

    const response = await scadaApi.uploadResource(
      file,
      projectInfo.value.SnowId,
      "video"
    );

    if (response.success) {
      const resourceData = response.data;

      // 创建视频组件
      const videoComponent = {
        id: `video_${Date.now()}`,
        type: "video",
        name: resourceData.fileName,
        position: position || { x: 100, y: 100 },
        size: { width: 400, height: 300 },
        properties: {
          src: resourceData.url,
          autoplay: false,
          loop: false,
          controls: true,
          muted: false
        },
        videoData: {
          fileName: resourceData.fileName,
          filePath: resourceData.filePath,
          url: resourceData.url,
          size: resourceData.size
        }
      };

      addComponentToCanvas(videoComponent);
      ElMessage.success("视频上传成功");
    } else {
      ElMessage.error(response.message || "上传失败");
    }
  } catch (error) {
    ElMessage.error("上传失败: " + (error as Error).message);
  } finally {
    loading.value = false;
  }
};

/**
 * 处理SVG上传
 */
export const handleSvgUpload = async (
  event: Event,
  projectInfo: any,
  loading: Ref<boolean>
) => {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (!file) return;

  // 验证文件类型
  if (!file.name.toLowerCase().endsWith(".svg")) {
    ElMessage.error("请选择SVG文件");
    return;
  }

  // 验证文件大小（5MB）
  if (file.size > 5 * 1024 * 1024) {
    ElMessage.error("SVG文件大小不能超过5MB");
    return;
  }

  if (!projectInfo.value.SnowId) {
    ElMessage.warning("请先保存项目后再上传资源");
    return;
  }

  try {
    loading.value = true;

    const response = await scadaApi.uploadResource(
      file,
      projectInfo.value.SnowId,
      "svg"
    );

    if (response.success) {
      ElMessage.success(`SVG上传成功：${response.data.fileName}`);
      // SVG上传后可以在组件库中使用
    } else {
      ElMessage.error(response.message || "上传失败");
    }
  } catch (error) {
    ElMessage.error("上传失败: " + (error as Error).message);
  } finally {
    loading.value = false;
  }
};

/**
 * 创建自动保存配置
 */
export const createAutoSaveConfig = () => {
  return reactive({
    enabled: true,
    interval: 5 * 60 * 1000, // 5分钟
    timerId: null as number | null
  });
};

/**
 * 启动自动保存
 */
export const startAutoSave = (
  autoSaveConfig: any,
  isSaved: Ref<boolean>,
  projectInfo: any,
  saveProjectFn: () => void
) => {
  if (!autoSaveConfig.enabled) return;

  // 清除旧定时器
  if (autoSaveConfig.timerId) {
    clearInterval(autoSaveConfig.timerId);
  }

  // 启动新定时器
  autoSaveConfig.timerId = setInterval(() => {
    // 只有在有未保存的修改且项目已有ID时才自动保存
    if (!isSaved.value && projectInfo.value.SnowId) {
      console.log("自动保存中...");
      saveProjectFn();
    }
  }, autoSaveConfig.interval) as unknown as number;
};

/**
 * 停止自动保存
 */
export const stopAutoSave = (autoSaveConfig: any) => {
  if (autoSaveConfig.timerId) {
    clearInterval(autoSaveConfig.timerId);
    autoSaveConfig.timerId = null;
  }
};

/**
 * 监听离开页面前提示
 */
export const handleBeforeUnload = (isSaved: Ref<boolean>) => {
  return (event: BeforeUnloadEvent) => {
    if (!isSaved.value) {
      event.preventDefault();
      event.returnValue = "当前项目有未保存的修改，确定要离开吗？";
      return event.returnValue;
    }
  };
};

/**
 * 发布/取消发布项目
 * @param projectId 项目ID
 * @param status 项目状态(0:草稿 1:发布)
 */
export const publishProject = async (projectId: string, status: number) => {
  try {
    const runtimeUrl = status === 1 ? scadaApi.buildRuntimeUrl(projectId) : "";
    const response = await scadaApi.dashPublish(projectId, status, runtimeUrl);
    if (response.Status) {
      ElMessage.success(status === 1 ? "项目发布成功" : "取消发布成功");
      return true;
    } else {
      ElMessage.error(response.Message || "操作失败");
      return false;
    }
  } catch (error) {
    console.error("发布项目失败:", error);
    ElMessage.error("操作失败: " + (error as Error).message);
    return false;
  }
};
/**
 * 默认项目数据结构
 * 用于新项目初始化和加载失败时的回退
 */
export const DEFAULT_PROJECT_DATA = {
  settings: {
    canvasWidth: 1920,
    canvasHeight: 1080,
    gridSize: 20,
    showGrid: true,
    enableSnap: true,
    backgroundColor: "#f5f5f5",
    backgroundImage: ""
  },
  components: [], // 画布上的所有组件
  devices: [], // MQTT设备列表
  datasets: [] // 数据集配置
};

/**
 * 获取项目ID from route params
 * @param route Vue Router route object
 * @returns Ref<string> 项目ID,如果路由参数不存在则返回空字符串
 */
export const getProjectId = (route: any) => {
  // 确保 route 和 route.params 存在
  const id = route?.params?.id || "";
  return ref(id as string);
};
