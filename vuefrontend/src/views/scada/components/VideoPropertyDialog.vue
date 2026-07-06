<template>
  <el-dialog
    v-model="dialogVisible"
    width="600px"
    top="15vh"
    :close-on-click-modal="false"
    :close-on-press-escape="true"
    draggable
    align-center
    :show-close="false"
    class="video-property-dialog"
    @close="handleClose"
  >
    <!-- 自定义头部 -->
    <template #header="{ close }">
      <div class="custom-dialog-header">
        <div class="header-left">
          <div class="header-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
              <rect
                x="3"
                y="5"
                width="18"
                height="14"
                rx="2"
                stroke="currentColor"
                stroke-width="2"
              />
              <polygon
                points="10,9 10,15 15,12"
                fill="currentColor"
              />
            </svg>
          </div>
          <div class="header-content">
            <h3 class="header-title">视频配置</h3>
            <p class="header-subtitle">设置视频播放参数</p>
          </div>
        </div>
        <div class="header-right">
          <div class="header-actions">
            <el-button
              link
              size="small"
              class="action-btn close-btn"
              @click="close"
            >
              <el-icon><Close /></el-icon>
            </el-button>
          </div>
        </div>
      </div>
    </template>

    <div class="video-dialog-content">
      <el-form :model="formData" label-width="110px" class="video-form">
        <el-alert
          title="支持格式"
          type="info"
          :closable="false"
          style="margin-bottom: 16px"
        >
          <template #default>
            <div style="font-size: 12px; line-height: 1.6">
              <p style="margin: 0 0 6px 0">
                <strong>✅ 支持：</strong>MP4、WebM、Ogg 等主流视频格式
              </p>
              <p style="margin: 0">
                <strong>📝 提示：</strong>推荐使用 MP4 格式以获得最佳兼容性
              </p>
            </div>
          </template>
        </el-alert>

        <el-form-item label="视频URL" required>
          <el-input
            v-model="formData.url"
            placeholder="请输入视频URL，例如: https://example.com/video.mp4"
            clearable
          >
            <template #prepend>
              <el-icon><VideoPlay /></el-icon>
            </template>
            <template #append>
              <el-button @click="testVideo">
                <el-icon><View /></el-icon>
                测试
              </el-button>
            </template>
          </el-input>
        </el-form-item>

        <el-form-item label="封面图URL">
          <el-input
            v-model="formData.poster"
            placeholder="视频封面图片URL（可选）"
            clearable
          >
            <template #prepend>
              <el-icon><Picture /></el-icon>
            </template>
          </el-input>
        </el-form-item>

        <el-divider content-position="left">播放控制</el-divider>

        <el-form-item label="显示控制条">
          <el-switch v-model="formData.controls" />
          <span style="margin-left: 10px; color: #909399; font-size: 12px">
            显示播放/暂停、进度条等控制按钮
          </span>
        </el-form-item>

        <el-form-item label="自动播放">
          <el-switch v-model="formData.autoplay" />
          <span style="margin-left: 10px; color: #909399; font-size: 12px">
            页面加载后自动播放（可能需要静音）
          </span>
        </el-form-item>

        <el-form-item label="循环播放">
          <el-switch v-model="formData.loop" />
          <span style="margin-left: 10px; color: #909399; font-size: 12px">
            视频结束后自动重新开始
          </span>
        </el-form-item>

        <el-form-item label="静音播放">
          <el-switch v-model="formData.muted" />
          <span style="margin-left: 10px; color: #909399; font-size: 12px">
            自动播放时通常需要静音
          </span>
        </el-form-item>

        <el-divider content-position="left">高级选项</el-divider>

        <el-form-item label="预加载">
          <el-select v-model="formData.preload" placeholder="选择预加载策略">
            <el-option label="无 - 不预加载" value="none" />
            <el-option label="元数据 - 仅预加载元数据" value="metadata" />
            <el-option label="自动 - 预加载整个视频" value="auto" />
          </el-select>
          <div style="color: #909399; font-size: 11px; margin-top: 5px">
            建议使用"元数据"以平衡性能和用户体验
          </div>
        </el-form-item>
      </el-form>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleClose">取消</el-button>
        <el-button type="primary" @click="handleSave">
          <el-icon><Check /></el-icon>
          保存
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue";
import { ElMessage } from "element-plus";
import { Close, Check, VideoPlay, View, Picture } from "@element-plus/icons-vue";

// Props
const props = defineProps({
  visible: {
    type: Boolean,
    default: false
  },
  videoComponent: {
    type: Object,
    default: null
  }
});

// Emits
const emit = defineEmits(["update:visible", "save-config"]);

// 响应式数据
const dialogVisible = computed({
  get: () => props.visible,
  set: (val) => emit("update:visible", val)
});

// 表单数据
const formData = ref({
  url: "",
  poster: "",
  controls: true,
  autoplay: false,
  loop: false,
  muted: false,
  preload: "metadata"
});

// 监听对话框打开，初始化数据
watch(
  () => props.visible,
  (newVal) => {
    if (newVal && props.videoComponent) {
      const props_data = props.videoComponent.properties || {};

      formData.value = {
        url: props_data.url || "",
        poster: props_data.poster || "",
        controls: props_data.controls !== false,
        autoplay: props_data.autoplay === true,
        loop: props_data.loop === true,
        muted: props_data.muted === true,
        preload: props_data.preload || "metadata"
      };
    }
  }
);

// 测试视频
const testVideo = () => {
  if (!formData.value.url) {
    ElMessage.warning("请先输入视频URL");
    return;
  }

  try {
    new URL(formData.value.url);

    // 直接在画布中的 video 组件里预览 URL
    if (props.videoComponent) {
      const element = document.getElementById(props.videoComponent.id);
      if (element) {
        const video = element.querySelector('video');
        if (video) {
          video.src = formData.value.url;
          if (formData.value.poster) {
            video.poster = formData.value.poster;
          }
          video.load();
          ElMessage.success("视频已在组件中加载，请查看画布预览");
        } else {
          ElMessage.error("未找到 video 元素");
        }
      } else {
        ElMessage.error("未找到组件元素");
      }
    } else {
      ElMessage.error("当前没有选中的视频组件");
    }
  } catch (error) {
    ElMessage.error("URL格式不正确，请输入完整的URL（包括 http:// 或 https://）");
  }
};

// 保存配置
const handleSave = () => {
  if (!formData.value.url) {
    ElMessage.warning("请输入视频URL");
    return;
  }

  try {
    new URL(formData.value.url);
  } catch (error) {
    ElMessage.error("URL格式不正确，请输入完整的URL（包括 http:// 或 https://）");
    return;
  }

  // 构建配置对象
  const config = {
    url: formData.value.url,
    poster: formData.value.poster,
    controls: formData.value.controls,
    autoplay: formData.value.autoplay,
    loop: formData.value.loop,
    muted: formData.value.muted,
    preload: formData.value.preload
  };

  emit("save-config", config);
  ElMessage.success("视频配置已保存");
  dialogVisible.value = false;
};

// 关闭对话框
const handleClose = () => {
  dialogVisible.value = false;
};
</script>

<style scoped lang="scss">
// 对话框整体样式
.video-property-dialog {
  :deep(.el-dialog) {
    border-radius: 16px;
    overflow: hidden;
    box-shadow:
      0 20px 60px rgba(0, 0, 0, 0.15),
      0 8px 30px rgba(0, 0, 0, 0.1);
    border: 1px solid rgba(255, 255, 255, 0.2);

    .el-dialog__header {
      padding: 0;
      margin: 0;
      border-bottom: none;
    }

    .el-dialog__body {
      padding: 20px;
      margin: 0;
    }
  }
}

// 自定义头部样式
.custom-dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  position: relative;
  overflow: hidden;

  &::before {
    content: "";
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(
      135deg,
      rgba(255, 255, 255, 0.1) 0%,
      rgba(255, 255, 255, 0.05) 100%
    );
    pointer-events: none;
  }

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
    position: relative;
    z-index: 2;

    .header-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      background: rgba(255, 255, 255, 0.15);
      border-radius: 10px;
      backdrop-filter: blur(10px);
      border: 1px solid rgba(255, 255, 255, 0.2);
      color: white;
      transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 6px 20px rgba(255, 255, 255, 0.25);
        background: rgba(255, 255, 255, 0.2);
      }

      svg {
        width: 20px;
        height: 20px;
        filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.2));
      }
    }

    .header-content {
      .header-title {
        margin: 0;
        font-size: 16px;
        font-weight: 600;
        color: white;
        letter-spacing: 0.3px;
        text-shadow: 0 2px 6px rgba(0, 0, 0, 0.25);
      }

      .header-subtitle {
        margin: 2px 0 0 0;
        font-size: 12px;
        color: rgba(255, 255, 255, 0.75);
        font-weight: 400;
        letter-spacing: 0.2px;
        text-shadow: 0 1px 3px rgba(0, 0, 0, 0.25);
      }
    }
  }

  .header-right {
    position: relative;
    z-index: 2;

    .header-actions {
      display: flex;
      gap: 6px;

      .action-btn {
        width: 32px;
        height: 32px;
        border-radius: 8px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: rgba(255, 255, 255, 0.1);
        border: 1px solid rgba(255, 255, 255, 0.2);
        color: rgba(255, 255, 255, 0.9);
        backdrop-filter: blur(10px);
        transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);

        &:hover {
          background: rgba(255, 255, 255, 0.2);
          color: white;
          transform: translateY(-2px);
          box-shadow: 0 6px 20px rgba(0, 0, 0, 0.25);
        }

        &.close-btn:hover {
          background: rgba(255, 107, 107, 0.8);
          border-color: rgba(255, 107, 107, 0.6);
        }

        .el-icon {
          font-size: 14px;
        }
      }
    }
  }
}

// 内容区域
.video-dialog-content {
  max-height: 60vh;
  overflow-y: auto;
}

// 表单样式
.video-form {
  :deep(.el-form-item) {
    margin-bottom: 20px;

    .el-form-item__label {
      font-size: 13px;
      color: #6c757d;
      font-weight: 500;
      line-height: 1.5;
    }

    .el-input,
    .el-select {
      .el-input__wrapper {
        border-radius: 6px;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
        transition: all 0.3s ease;

        &:hover {
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
        }

        &.is-focus {
          box-shadow: 0 0 0 2px rgba(102, 126, 234, 0.2);
        }
      }
    }
  }
}

// 分隔线
:deep(.el-divider) {
  margin: 20px 0;

  .el-divider__text {
    font-size: 13px;
    font-weight: 500;
    color: #6c757d;
  }
}

// 底部按钮
.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 20px;
  border-top: 1px solid #e9ecef;
  background: #f8f9fa;

  .el-button {
    min-width: 80px;
    height: 36px;
    border-radius: 6px;
    font-weight: 500;
    font-size: 13px;

    &:first-child {
      background: #6c757d;
      border-color: #6c757d;
      color: white;

      &:hover {
        background: #5a6268;
        border-color: #545b62;
      }
    }

    &.el-button--primary {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border: none;

      &:hover {
        background: linear-gradient(135deg, #5a67d8 0%, #6b46c1 100%);
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
      }
    }

    .el-icon {
      margin-right: 4px;
    }
  }
}
</style>
