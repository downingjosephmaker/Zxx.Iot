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
    class="text-card-property-dialog"
    @close="handleClose"
  >
    <!-- 自定义头部 -->
    <template #header="{ close }">
      <div class="custom-dialog-header">
        <div class="header-left">
          <div class="header-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
              <rect x="3" y="5" width="18" height="14" rx="2" stroke="currentColor" stroke-width="2"/>
              <line x1="6" y1="9" x2="12" y2="9" stroke="currentColor" stroke-width="2"/>
              <line x1="6" y1="13" x2="18" y2="13" stroke="currentColor" stroke-width="1"/>
              <line x1="6" y1="16" x2="16" y2="16" stroke="currentColor" stroke-width="1"/>
            </svg>
          </div>
          <div class="header-content">
            <h3 class="header-title">文本卡片配置</h3>
            <p class="header-subtitle">多行文本卡片,支持丰富的文本格式配置</p>
          </div>
        </div>
        <div class="header-right">
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
    </template>

    <div class="text-card-dialog-content">
      <el-tabs v-model="activeTab" class="card-tabs">
        <!-- 内容设置 -->
        <el-tab-pane label="内容设置" name="content">
          <el-form :model="formData" label-width="120px" class="card-form">
            <el-form-item label="文本内容" required>
              <el-input
                v-model="formData.content"
                type="textarea"
                :rows="6"
                placeholder="输入文本内容&#10;支持多行文本 (按Enter换行)"
                clearable
              />
              <div class="form-tip">
                💡 提示: 可以输入多行文本,使用回车键换行
              </div>
            </el-form-item>

            <el-divider/>

            <el-form-item label="多行模式">
              <el-switch v-model="formData.multiLine"/>
              <span class="form-label-tip">启用后支持多行文本显示和配置</span>
            </el-form-item>

            <template v-if="formData.multiLine">
              <el-form-item label="最大行数">
                <el-input-number
                  v-model="formData.maxLines"
                  :min="1"
                  :max="20"
                  :step="1"
                />
                <span class="form-label-tip">超出行数的内容将被截断</span>
              </el-form-item>

              <el-form-item label="行高倍数">
                <el-slider
                  v-model="formData.lineHeight"
                  :min="1"
                  :max="3"
                  :step="0.1"
                  :marks="{ 1: '紧凑', 1.6: '正常', 2.5: '宽松' }"
                  show-input
                />
                <div class="form-tip">
                  控制行与行之间的垂直间距
                </div>
              </el-form-item>
            </template>
          </el-form>
        </el-tab-pane>

        <!-- 卡片样式 -->
        <el-tab-pane label="卡片样式" name="cardStyle">
          <el-form :model="formData" label-width="120px" class="card-form">
            <el-form-item label="背景颜色">
              <el-color-picker v-model="formData.backgroundColor" show-alpha/>
            </el-form-item>

            <el-form-item label="边框颜色">
              <el-color-picker v-model="formData.borderColor" show-alpha/>
            </el-form-item>

            <el-form-item label="边框宽度">
              <el-slider
                v-model="formData.borderWidth"
                :min="0"
                :max="10"
                :step="0.5"
                show-input
              />
            </el-form-item>

            <el-form-item label="圆角半径">
              <el-slider
                v-model="formData.borderRadius"
                :min="0"
                :max="50"
                :step="1"
                show-input
              />
            </el-form-item>

            <el-divider/>

            <el-form-item label="启用阴影">
              <el-switch v-model="formData.shadow.enabled"/>
            </el-form-item>

            <template v-if="formData.shadow.enabled">
              <el-form-item label="阴影颜色">
                <el-color-picker v-model="formData.shadow.color" show-alpha/>
              </el-form-item>

              <el-form-item label="阴影偏移X">
                <el-slider
                  v-model="formData.shadow.offsetX"
                  :min="-20"
                  :max="20"
                  :step="1"
                  show-input
                />
              </el-form-item>

              <el-form-item label="阴影偏移Y">
                <el-slider
                  v-model="formData.shadow.offsetY"
                  :min="-20"
                  :max="20"
                  :step="1"
                  show-input
                />
              </el-form-item>

              <el-form-item label="阴影模糊">
                <el-slider
                  v-model="formData.shadow.blur"
                  :min="0"
                  :max="30"
                  :step="1"
                  show-input
                />
              </el-form-item>
            </template>
          </el-form>
        </el-tab-pane>

        <!-- 文本样式 -->
        <el-tab-pane label="文本样式" name="textStyle">
          <el-form :model="formData" label-width="120px" class="card-form">
            <el-form-item label="字体">
              <el-select v-model="formData.textStyle.fontFamily">
                <el-option label="微软雅黑" value="'Microsoft YaHei', sans-serif"/>
                <el-option label="宋体" value="'SimSun', serif"/>
                <el-option label="黑体" value="'SimHei', sans-serif"/>
                <el-option label="Arial" value="'Arial', sans-serif"/>
                <el-option label="Courier New" value="'Courier New', monospace"/>
              </el-select>
            </el-form-item>

            <el-form-item label="字号">
              <el-slider
                v-model="formData.textStyle.fontSize"
                :min="10"
                :max="48"
                :step="1"
                show-input
              />
            </el-form-item>

            <el-form-item label="字体粗细">
              <el-radio-group v-model="formData.textStyle.fontWeight">
                <el-radio-button label="normal">正常</el-radio-button>
                <el-radio-button label="bold">粗体</el-radio-button>
              </el-radio-group>
            </el-form-item>

            <el-form-item label="文字颜色">
              <el-color-picker v-model="formData.textStyle.color" show-alpha/>
            </el-form-item>

            <el-form-item label="文本对齐">
              <el-radio-group v-model="formData.textStyle.textAlign">
                <el-radio-button label="left">左对齐</el-radio-button>
                <el-radio-button label="center">居中</el-radio-button>
                <el-radio-button label="right">右对齐</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 布局设置 -->
        <el-tab-pane label="布局设置" name="layout">
          <el-form :model="formData" label-width="120px" class="card-form">
            <el-form-item label="内边距">
              <el-slider
                v-model="formData.padding"
                :min="0"
                :max="50"
                :step="2"
                show-input
              />
              <div class="form-tip">
                卡片内容与边框之间的距离
              </div>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 预览 -->
        <el-tab-pane label="预览" name="preview">
          <div class="preview-section">
            <div class="preview-title">实时预览</div>
            <div
              class="card-preview-container"
              :style="{
                backgroundColor: formData.backgroundColor,
                borderColor: formData.borderColor,
                borderWidth: formData.borderWidth + 'px',
                borderRadius: formData.borderRadius + 'px',
                borderStyle: 'solid',
                padding: formData.padding + 'px',
                boxShadow: formData.shadow.enabled
                  ? `${formData.shadow.offsetX}px ${formData.shadow.offsetY}px ${formData.shadow.blur}px ${formData.shadow.color}`
                  : 'none'
              }"
            >
              <div
                class="preview-content-text"
                :style="{
                  fontFamily: formData.textStyle.fontFamily,
                  fontSize: formData.textStyle.fontSize + 'px',
                  fontWeight: formData.textStyle.fontWeight,
                  color: formData.textStyle.color,
                  lineHeight: formData.lineHeight,
                  textAlign: formData.textStyle.textAlign,
                  whiteSpace: 'pre-wrap'
                }"
              >
                {{ formData.content || '这是一段文本内容\n可以支持多行显示' }}
              </div>
            </div>
          </div>
        </el-tab-pane>
      </el-tabs>
    </div>

    <!-- 底部按钮 -->
    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleReset">重置</el-button>
        <el-button type="primary" @click="handleSave">确定</el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { Close } from '@element-plus/icons-vue'
import type { TextCardConfig } from '../core/TextCardComponent'

// Props
const props = defineProps<{
  visible: boolean
  textCardComponent?: any
}>()

// Emits
const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void
  (e: 'save-config', config: TextCardConfig): void
}>()

// 响应式数据
const dialogVisible = ref(props.visible)
const activeTab = ref('content')

// 表单数据
const formData = reactive<TextCardConfig>({
  content: '这是一段文本内容\n可以支持多行显示',
  multiLine: true,
  maxLines: 10,
  lineHeight: 1.6,

  backgroundColor: '#ffffff',
  borderColor: '#d1d5db',
  borderWidth: 1,
  borderRadius: 4,

  shadow: {
    enabled: true,
    color: 'rgba(0,0,0,0.1)',
    offsetX: 2,
    offsetY: 2,
    blur: 4
  },

  textStyle: {
    fontFamily: "'Microsoft YaHei', sans-serif",
    fontSize: 14,
    fontWeight: 'normal',
    color: '#333333',
    textAlign: 'left'
  },

  padding: 16
})

// 监听对话框显示状态
watch(
  () => props.visible,
  (val) => {
    dialogVisible.value = val
    if (val && props.textCardComponent) {
      // 加载现有配置
      const existingConfig = props.textCardComponent.properties?.textCardConfig || props.textCardComponent.properties || {}
      Object.assign(formData, existingConfig)
    }
  }
)

watch(dialogVisible, (val) => {
  emit('update:visible', val)
})

/**
 * 保存配置
 */
const handleSave = () => {
  emit('save-config', { ...formData })
  dialogVisible.value = false
}

/**
 * 重置表单
 */
const handleReset = () => {
  Object.assign(formData, {
    content: '这是一段文本内容\n可以支持多行显示',
    multiLine: true,
    maxLines: 10,
    lineHeight: 1.6,

    backgroundColor: '#ffffff',
    borderColor: '#d1d5db',
    borderWidth: 1,
    borderRadius: 4,

    shadow: {
      enabled: true,
      color: 'rgba(0,0,0,0.1)',
      offsetX: 2,
      offsetY: 2,
      blur: 4
    },

    textStyle: {
      fontFamily: "'Microsoft YaHei', sans-serif",
      fontSize: 14,
      fontWeight: 'normal',
      color: '#333333',
      textAlign: 'left'
    },

    padding: 16
  })
}

/**
 * 关闭对话框
 */
const handleClose = () => {
  dialogVisible.value = false
}
</script>

<style scoped lang="scss">
.text-card-property-dialog {
  :deep(.el-dialog__body) {
    padding: 0;
    max-height: 70vh;
    overflow: hidden;
  }
}

.custom-dialog-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;

    .header-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border-radius: 10px;
      color: white;
    }

    .header-content {
      .header-title {
        font-size: 16px;
        font-weight: 600;
        margin: 0 0 4px 0;
        color: #303133;
      }

      .header-subtitle {
        font-size: 12px;
        color: #909399;
        margin: 0;
      }
    }
  }

  .header-right {
    .action-btn {
      padding: 8px;

      &:hover {
        color: #409eff;
      }
    }
  }
}

.text-card-dialog-content {
  padding: 0 20px 20px 20px;

  .card-tabs {
    :deep(.el-tabs__content) {
      max-height: 50vh;
      overflow-y: auto;
      padding: 16px 0;
    }
  }
}

.card-form {
  .form-tip {
    font-size: 12px;
    color: #909399;
    margin-top: 4px;
  }

  .form-label-tip {
    margin-left: 8px;
    font-size: 12px;
    color: #909399;
  }
}

// 预览区域
.preview-section {
  .preview-title {
    font-size: 14px;
    font-weight: 600;
    margin-bottom: 12px;
    color: #303133;
  }

  .card-preview-container {
    min-height: 200px;
    max-height: 400px;
    overflow: auto;

    .preview-content-text {
      word-wrap: break-word;
    }
  }
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 20px;
  border-top: 1px solid #ebeef5;
}
</style>
