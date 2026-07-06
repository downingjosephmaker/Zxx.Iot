<template>
  <el-dialog
    v-model="dialogVisible"
    width="700px"
    top="10vh"
    :close-on-click-modal="false"
    :close-on-press-escape="true"
    draggable
    align-center
    :show-close="false"
    class="webcam-property-dialog"
    @close="handleClose"
  >
    <!-- 自定义头部 -->
    <template #header="{ close }">
      <div class="custom-dialog-header">
        <div class="header-left">
          <div class="header-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
              <rect
                x="4"
                y="4"
                width="16"
                height="12"
                rx="2"
                stroke="currentColor"
                stroke-width="2"
              />
              <circle cx="12" cy="10" r="3" stroke="currentColor" stroke-width="2" />
              <circle cx="17" cy="7" r="1" fill="currentColor" />
            </svg>
          </div>
          <div class="header-content">
            <h3 class="header-title">摄像头配置</h3>
            <p class="header-subtitle">设置实时视频流参数</p>
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

    <div class="webcam-dialog-content">
      <el-form :model="formData" label-width="120px" class="webcam-form">
        <el-alert
          title="协议支持"
          type="info"
          :closable="false"
          style="margin-bottom: 16px"
        >
          <template #default>
            <div style="font-size: 12px; line-height: 1.6">
              <p style="margin: 0 0 6px 0">
                <strong>✅ 推荐：</strong>HLS (m3u8)、HTTP/HTTPS 直接流
              </p>
              <p style="margin: 0">
                <strong>📝 提示：</strong>RTSP、RTMP 需要服务器端转码支持
              </p>
            </div>
          </template>
        </el-alert>

        <!-- 基本设置 -->
        <el-form-item label="摄像头名称" required>
          <el-input
            v-model="formData.name"
            placeholder="例如: 车间1号摄像头"
            clearable
          >
            <template #prepend>
              <el-icon><Edit /></el-icon>
            </template>
          </el-input>
        </el-form-item>

        <el-form-item label="视频流地址" required>
          <el-input
            v-model="formData.streamUrl"
            placeholder="例如: https://example.com/live.m3u8"
            clearable
          >
            <template #prepend>
              <el-icon><VideoCamera /></el-icon>
            </template>
            <template #append>
              <el-button @click="testStream">
                <el-icon><View /></el-icon>
                测试
              </el-button>
            </template>
          </el-input>
        </el-form-item>

        <el-form-item label="协议类型">
          <el-select v-model="formData.protocol" placeholder="选择协议类型">
            <el-option label="HLS (推荐)" value="hls">
              <span>HLS (推荐)</span>
              <span style="float: right; color: #8492a6; font-size: 12px">m3u8</span>
            </el-option>
            <el-option label="HTTP/HTTPS" value="http">
              <span>HTTP/HTTPS</span>
              <span style="float: right; color: #8492a6; font-size: 12px">直接流</span>
            </el-option>
            <el-option label="WebRTC" value="webrtc">
              <span>WebRTC</span>
              <span style="float: right; color: #f56c6c; font-size: 12px">需配置</span>
            </el-option>
            <el-option label="RTSP" value="rtsp">
              <span>RTSP</span>
              <span style="float: right; color: #f56c6c; font-size: 12px">需转码</span>
            </el-option>
            <el-option label="RTMP" value="rtmp">
              <span>RTMP</span>
              <span style="float: right; color: #f56c6c; font-size: 12px">需转码</span>
            </el-option>
          </el-select>
        </el-form-item>

        <el-divider content-position="left">视频设置</el-divider>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="视频宽度">
              <el-input-number
                v-model="formData.width"
                :min="320"
                :max="1920"
                :step="10"
                controls-position="right"
                style="width: 100%"
              />
              <span style="margin-left: 8px; color: #909399; font-size: 12px">px</span>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="视频高度">
              <el-input-number
                v-model="formData.height"
                :min="240"
                :max="1080"
                :step="10"
                controls-position="right"
                style="width: 100%"
              />
              <span style="margin-left: 8px; color: #909399; font-size: 12px">px</span>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="视频质量">
              <el-select v-model="formData.quality" placeholder="选择质量">
                <el-option label="自动" value="auto" />
                <el-option label="超清" value="ultra" />
                <el-option label="高清" value="high" />
                <el-option label="标清" value="medium" />
                <el-option label="流畅" value="low" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="帧率">
              <el-input-number
                v-model="formData.fps"
                :min="1"
                :max="60"
                :step="5"
                controls-position="right"
                style="width: 100%"
              />
              <span style="margin-left: 8px; color: #909399; font-size: 12px">fps</span>
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="适应方式">
          <el-select v-model="formData.objectFit" placeholder="选择适应方式">
            <el-option label="适应容器" value="contain" />
            <el-option label="覆盖容器" value="cover" />
            <el-option label="填充容器" value="fill" />
            <el-option label="原始尺寸" value="none" />
          </el-select>
        </el-form-item>

        <el-divider content-position="left">认证设置</el-divider>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="用户名">
              <el-input
                v-model="formData.username"
                placeholder="认证用户名（可选）"
                clearable
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="密码">
              <el-input
                v-model="formData.password"
                type="password"
                placeholder="认证密码（可选）"
                clearable
                show-password
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="API令牌">
          <el-input
            v-model="formData.token"
            placeholder="API令牌（可选）"
            clearable
          />
        </el-form-item>

        <el-divider content-position="left">播放控制</el-divider>

        <el-form-item label="自动播放">
          <el-switch v-model="formData.autoPlay" />
          <span style="margin-left: 10px; color: #909399; font-size: 12px">
            页面加载后自动连接并播放
          </span>
        </el-form-item>

        <el-form-item label="自动重连">
          <el-switch v-model="formData.autoReconnect" />
          <span style="margin-left: 10px; color: #909399; font-size: 12px">
            连接断开后自动重新连接
          </span>
        </el-form-item>

        <el-row :gutter="16" v-if="formData.autoReconnect">
          <el-col :span="12">
            <el-form-item label="重连间隔">
              <el-input-number
                v-model="formData.reconnectInterval"
                :min="1000"
                :max="30000"
                :step="1000"
                controls-position="right"
                style="width: 100%"
              />
              <span style="margin-left: 8px; color: #909399; font-size: 12px">ms</span>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="最大重连次数">
              <el-input-number
                v-model="formData.reconnectMaxAttempts"
                :min="1"
                :max="100"
                :step="1"
                controls-position="right"
                style="width: 100%"
              />
              <span style="margin-left: 8px; color: #909399; font-size: 12px">次</span>
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="缓冲区大小">
          <el-slider
            v-model="formData.bufferSize"
            :min="0.5"
            :max="10"
            :step="0.5"
            :show-tooltip="true"
            :format-tooltip="(val) => `${val}秒`"
          />
        </el-form-item>

        <el-divider content-position="left">功能开关</el-divider>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="启用快照">
              <el-switch v-model="formData.enableSnapshot" />
              <span style="margin-left: 10px; color: #909399; font-size: 12px">
                允许截取画面
              </span>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="启用全屏">
              <el-switch v-model="formData.enableFullscreen" />
              <span style="margin-left: 10px; color: #909399; font-size: 12px">
                允许全屏播放
              </span>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="显示控制条">
              <el-switch v-model="formData.enableControls" />
              <span style="margin-left: 10px; color: #909399; font-size: 12px">
                显示播放控制
              </span>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="启用云台">
              <el-switch v-model="formData.enablePTZ" />
              <span style="margin-left: 10px; color: #909399; font-size: 12px">
                云台控制功能
              </span>
            </el-form-item>
          </el-col>
        </el-row>

        <el-divider content-position="left">显示设置</el-divider>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="显示时间戳">
              <el-switch v-model="formData.showTimestamp" />
              <span style="margin-left: 10px; color: #909399; font-size: 12px">
                叠加时间信息
              </span>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="显示码率">
              <el-switch v-model="formData.showBitrate" />
              <span style="margin-left: 10px; color: #909399; font-size: 12px">
                显示网络信息
              </span>
            </el-form-item>
          </el-col>
        </el-row>
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
import { Close, Check, VideoCamera, View, Edit } from "@element-plus/icons-vue";

// Props
const props = defineProps({
  visible: {
    type: Boolean,
    default: false
  },
  webcamComponent: {
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
  name: "摄像头1",
  streamUrl: "",
  protocol: "hls",
  width: 640,
  height: 480,
  quality: "auto",
  fps: 25,
  objectFit: "contain",
  username: "",
  password: "",
  token: "",
  autoPlay: true,
  autoReconnect: true,
  reconnectInterval: 3000,
  reconnectMaxAttempts: 10,
  bufferSize: 1,
  enableSnapshot: true,
  enableFullscreen: true,
  enableControls: true,
  enablePTZ: false,
  showTimestamp: false,
  showBitrate: false
});

// 监听对话框打开，初始化数据
watch(
  () => props.visible,
  (newVal) => {
    if (newVal && props.webcamComponent) {
      const props_data = props.webcamComponent.properties || {};

      formData.value = {
        name: props_data.name || "摄像头1",
        streamUrl: props_data.streamUrl || "",
        protocol: props_data.protocol || "hls",
        width: props_data.width || 640,
        height: props_data.height || 480,
        quality: props_data.quality || "auto",
        fps: props_data.fps || 25,
        objectFit: props_data.objectFit || "contain",
        username: props_data.username || "",
        password: props_data.password || "",
        token: props_data.token || "",
        autoPlay: props_data.autoPlay !== false,
        autoReconnect: props_data.autoReconnect !== false,
        reconnectInterval: props_data.reconnectInterval || 3000,
        reconnectMaxAttempts: props_data.reconnectMaxAttempts || 10,
        bufferSize: props_data.bufferSize || 1,
        enableSnapshot: props_data.enableSnapshot !== false,
        enableFullscreen: props_data.enableFullscreen !== false,
        enableControls: props_data.enableControls !== false,
        enablePTZ: props_data.enablePTZ === true,
        showTimestamp: props_data.showTimestamp === true,
        showBitrate: props_data.showBitrate === true
      };
    }
  }
);

// 测试视频流
const testStream = () => {
  if (!formData.value.streamUrl) {
    ElMessage.warning("请先输入视频流地址");
    return;
  }

  try {
    new URL(formData.value.streamUrl);
    ElMessage.success("视频流地址格式正确，可以保存配置后查看效果");
  } catch (error) {
    ElMessage.error("URL格式不正确，请输入完整的URL（包括 http:// 或 https://）");
  }
};

// 保存配置
const handleSave = () => {
  if (!formData.value.streamUrl) {
    ElMessage.warning("请输入视频流地址");
    return;
  }

  try {
    new URL(formData.value.streamUrl);
  } catch (error) {
    ElMessage.error("URL格式不正确，请输入完整的URL（包括 http:// 或 https://）");
    return;
  }

  // 构建配置对象
  const config = {
    name: formData.value.name,
    streamUrl: formData.value.streamUrl,
    protocol: formData.value.protocol,
    width: formData.value.width,
    height: formData.value.height,
    quality: formData.value.quality,
    fps: formData.value.fps,
    objectFit: formData.value.objectFit,
    username: formData.value.username,
    password: formData.value.password,
    token: formData.value.token,
    autoPlay: formData.value.autoPlay,
    autoReconnect: formData.value.autoReconnect,
    reconnectInterval: formData.value.reconnectInterval,
    reconnectMaxAttempts: formData.value.reconnectMaxAttempts,
    bufferSize: formData.value.bufferSize,
    enableSnapshot: formData.value.enableSnapshot,
    enableFullscreen: formData.value.enableFullscreen,
    enableControls: formData.value.enableControls,
    enablePTZ: formData.value.enablePTZ,
    showTimestamp: formData.value.showTimestamp,
    showBitrate: formData.value.showBitrate
  };

  emit("save-config", config);
  ElMessage.success("摄像头配置已保存");
  dialogVisible.value = false;
};

// 关闭对话框
const handleClose = () => {
  dialogVisible.value = false;
};
</script>

<style scoped lang="scss">
// 对话框整体样式
.webcam-property-dialog {
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

// 自定义头部样式 - 使用蓝绿渐变
.custom-dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  background: linear-gradient(135deg, #00b4db 0%, #0083b0 100%);
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
.webcam-dialog-content {
  max-height: 65vh;
  overflow-y: auto;
}

// 表单样式
.webcam-form {
  :deep(.el-form-item) {
    margin-bottom: 20px;

    .el-form-item__label {
      font-size: 13px;
      color: #6c757d;
      font-weight: 500;
      line-height: 1.5;
    }

    .el-input,
    .el-select,
    .el-input-number {
      .el-input__wrapper {
        border-radius: 6px;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
        transition: all 0.3s ease;

        &:hover {
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
        }

        &.is-focus {
          box-shadow: 0 0 0 2px rgba(0, 180, 219, 0.2);
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
      background: linear-gradient(135deg, #00b4db 0%, #0083b0 100%);
      border: none;

      &:hover {
        background: linear-gradient(135deg, #00a3cc 0%, #007399 100%);
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(0, 180, 219, 0.3);
      }
    }

    .el-icon {
      margin-right: 4px;
    }
  }
}
</style>
