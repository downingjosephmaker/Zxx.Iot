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
    class="iframe-property-dialog"
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
                y="3"
                width="18"
                height="18"
                rx="2"
                stroke="currentColor"
                stroke-width="2"
              />
              <path
                d="M3 9h18"
                stroke="currentColor"
                stroke-width="2"
              />
              <circle cx="6" cy="6" r="1" fill="currentColor" />
              <circle cx="9" cy="6" r="1" fill="currentColor" />
              <circle cx="12" cy="6" r="1" fill="currentColor" />
            </svg>
          </div>
          <div class="header-content">
            <h3 class="header-title">内嵌网页配置</h3>
            <p class="header-subtitle">设置要嵌入的网页地址</p>
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

    <div class="iframe-dialog-content">
      <el-form :model="formData" label-width="110px" class="iframe-form">
        <el-alert
          title="使用说明"
          type="warning"
          :closable="false"
          style="margin-bottom: 16px"
        >
          <template #default>
            <div style="font-size: 12px; line-height: 1.6">
              <p style="margin: 0 0 6px 0">
                <strong>⚠️ 嵌入限制：</strong>大多数主流网站（Google、YouTube等）禁止被iframe嵌入
              </p>
              <p style="margin: 0">
                <strong>✅ 推荐使用：</strong>自己开发的网页、数据看板、图表等
              </p>
            </div>
          </template>
        </el-alert>

        <el-form-item label="网页URL" required>
          <el-input
            v-model="formData.url"
            placeholder="请输入网页URL，例如: https://www.example.com"
            clearable
          >
            <template #prepend>
              <el-icon><Link /></el-icon>
            </template>
            <template #append>
              <el-button @click="testUrl">
                <el-icon><View /></el-icon>
                测试
              </el-button>
            </template>
          </el-input>
        </el-form-item>

        <el-divider content-position="left">显示选项</el-divider>

        <el-form-item label="允许全屏">
          <el-switch v-model="formData.allowFullscreen" />
          <span style="margin-left: 10px; color: #909399; font-size: 12px">
            需要内嵌网页支持
          </span>
        </el-form-item>

        <el-divider content-position="left">安全设置</el-divider>

        <el-form-item label="启用沙箱">
          <el-switch v-model="formData.enableSandbox" />
          <span style="margin-left: 10px; color: #909399; font-size: 12px">
            限制iframe权限（可能影响网页功能）
          </span>
        </el-form-item>

        <el-form-item label="沙箱权限" v-if="formData.enableSandbox">
          <el-checkbox-group v-model="sandboxOptions">
            <el-checkbox label="allow-scripts">允许脚本</el-checkbox>
            <el-checkbox label="allow-same-origin">允许同源</el-checkbox>
            <el-checkbox label="allow-forms">允许表单</el-checkbox>
            <el-checkbox label="allow-popups">允许弹窗</el-checkbox>
            <el-checkbox label="allow-modals">允许模态框</el-checkbox>
          </el-checkbox-group>
          <div style="color: #e6a23c; font-size: 11px; margin-top: 5px">
            ⚠️ 启用沙箱可能导致某些网站无法正常工作
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
import { ref, reactive, watch, computed } from "vue";
import { ElMessage } from "element-plus";
import { Close, Check, Link, View, Lock } from "@element-plus/icons-vue";

// Props
const props = defineProps({
  visible: {
    type: Boolean,
    default: false
  },
  iframeComponent: {
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
  url: "https://www.example.com",
  allowFullscreen: true,
  enableSandbox: false,
  sandbox: ""
});

// 沙箱选项
const sandboxOptions = ref<string[]>([]);

// 监听沙箱选项变化，更新sandbox字符串
watch(sandboxOptions, (newVal) => {
  formData.value.sandbox = newVal.join(" ");
}, { deep: true });

// 监听enableSandbox变化
watch(() => formData.value.enableSandbox, (newVal) => {
  if (newVal && sandboxOptions.value.length === 0) {
    // 默认启用基本权限
    sandboxOptions.value = ["allow-scripts", "allow-same-origin"];
  }
});

// 监听对话框打开，初始化数据
watch(
  () => props.visible,
  (newVal) => {
    if (newVal && props.iframeComponent) {
      const props_data = props.iframeComponent.properties || {};

      formData.value = {
        url: props_data.url || "https://www.example.com",
        allowFullscreen: props_data.allowFullscreen !== false,
        enableSandbox: !!props_data.sandbox,
        sandbox: props_data.sandbox || ""
      };

      // 初始化沙箱选项
      if (props_data.sandbox) {
        sandboxOptions.value = props_data.sandbox.split(" ").filter(Boolean);
      } else {
        sandboxOptions.value = [];
      }
    }
  }
);

// 测试URL
const testUrl = () => {
  if (!formData.value.url) {
    ElMessage.warning("请先输入网页URL");
    return;
  }

  try {
    new URL(formData.value.url);

    // 直接在画布中的 iframe 组件里预览 URL
    if (props.iframeComponent) {
      const element = document.getElementById(props.iframeComponent.id);
      if (element) {
        const iframe = element.querySelector('iframe');
        if (iframe) {
          iframe.src = formData.value.url;
          ElMessage.success("URL 已在组件中加载，请查看画布预览");
        } else {
          ElMessage.error("未找到 iframe 元素");
        }
      } else {
        ElMessage.error("未找到组件元素");
      }
    } else {
      ElMessage.error("当前没有选中的 iframe 组件");
    }
  } catch (error) {
    ElMessage.error("URL格式不正确，请输入完整的URL（包括 http:// 或 https://）");
  }
};

// 保存配置
const handleSave = () => {
  if (!formData.value.url) {
    ElMessage.warning("请输入网页URL");
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
    allowFullscreen: formData.value.allowFullscreen,
    sandbox: formData.value.enableSandbox ? formData.value.sandbox : ""
  };

  emit("save-config", config);
  ElMessage.success("iframe配置已保存");
  dialogVisible.value = false;
};

// 关闭对话框
const handleClose = () => {
  dialogVisible.value = false;
};
</script>

<style scoped lang="scss">
// 对话框整体样式
.iframe-property-dialog {
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
.iframe-dialog-content {
  max-height: 60vh;
  overflow-y: auto;
}

// 表单样式
.iframe-form {
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

    .el-slider {
      padding: 0 12px;
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
