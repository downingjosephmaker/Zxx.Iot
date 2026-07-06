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
    class="led-display-property-dialog"
    @close="handleClose"
  >
    <!-- 自定义头部 -->
    <template #header="{ close }">
      <div class="custom-dialog-header">
        <div class="header-left">
          <div class="header-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
              <rect x="2" y="6" width="20" height="12" rx="2" stroke="currentColor" stroke-width="2"/>
              <text x="12" y="14" text-anchor="middle" fill="currentColor" font-size="8" font-weight="bold">88</text>
            </svg>
          </div>
          <div class="header-content">
            <h3 class="header-title">LED 显示屏配置</h3>
            <p class="header-subtitle">炫酷 LED 显示效果,支持数字/文字/多行</p>
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

    <div class="led-display-dialog-content">
      <el-tabs v-model="activeTab" class="led-tabs">
        <!-- 内容设置 -->
        <el-tab-pane label="内容设置" name="content">
          <el-form :model="formData" label-width="120px" class="led-form">
            <el-form-item label="显示内容" required>
              <el-input
                v-model="formData.text"
                type="textarea"
                :rows="3"
                placeholder="输入要显示的文本&#10;支持多行文本 (按Enter换行)"
                clearable
              />
              <div class="form-tip">
                💡 提示: 可以输入数字、文字、符号,支持多行显示
              </div>
            </el-form-item>

            <el-form-item label="格式化">
              <el-select v-model="formData.format" placeholder="选择格式化方式">
                <el-option label="无格式化" value="none"/>
                <el-option label="时间格式 (HH:mm:ss)" value="time"/>
                <el-option label="日期格式 (YYYY-MM-DD)" value="date"/>
                <el-option label="数字格式 (千分位)" value="number"/>
                <el-option label="自定义函数" value="custom"/>
              </el-select>
            </el-form-item>

            <el-form-item
              v-if="formData.format === 'custom'"
              label="自定义函数"
            >
              <el-input
                v-model="formData.customFormat"
                placeholder="例如: value.toUpperCase()"
                clearable
              >
                <template #prepend>value =></template>
              </el-input>
              <div class="form-tip">
                ⚠️ JavaScript 表达式,可使用 value 变量
              </div>
            </el-form-item>

            <el-divider/>

            <el-form-item label="多行模式">
              <el-switch v-model="formData.multiLine"/>
              <span class="form-label-tip">启用后支持多行文本显示</span>
            </el-form-item>

            <template v-if="formData.multiLine">
              <el-form-item label="最大行数">
                <el-input-number
                  v-model="formData.maxLines"
                  :min="1"
                  :max="10"
                  :step="1"
                />
              </el-form-item>

              <el-form-item label="行高倍数">
                <el-slider
                  v-model="formData.lineHeight"
                  :min="1"
                  :max="2"
                  :step="0.1"
                  show-input
                />
              </el-form-item>

              <el-form-item label="文本对齐">
                <el-radio-group v-model="formData.alignment">
                  <el-radio-button label="left">左对齐</el-radio-button>
                  <el-radio-button label="center">居中</el-radio-button>
                  <el-radio-button label="right">右对齐</el-radio-button>
                </el-radio-group>
              </el-form-item>
            </template>
          </el-form>
        </el-tab-pane>

        <!-- 外观样式 -->
        <el-tab-pane label="外观样式" name="appearance">
          <el-form :model="formData" label-width="120px" class="led-form">
            <el-form-item label="LED 颜色">
              <el-color-picker
                v-model="formData.color"
                show-alpha
                :predefine="predefineColors"
              />
              <span class="color-preview" :style="{ color: formData.color }">
                {{ formData.text || '88:88:88' }}
              </span>
            </el-form-item>

            <el-form-item label="背景颜色">
              <el-color-picker
                v-model="formData.backgroundColor"
                show-alpha
              />
            </el-form-item>

            <el-divider/>

            <el-form-item label="字体大小">
              <el-slider
                v-model="formData.fontSize"
                :min="12"
                :max="120"
                :step="1"
                show-input
              />
            </el-form-item>

            <el-form-item label="字体">
              <el-select v-model="formData.fontFamily">
                <el-option label="Courier New (等宽)" value="'Courier New', monospace"/>
                <el-option label="Arial" value="'Arial', sans-serif"/>
                <el-option label="Verdana" value="'Verdana', sans-serif"/>
                <el-option label="Digital (数码管)" value="'Digital-7', 'Orbitron', monospace"/>
                <el-option label="微软雅黑" value="'Microsoft YaHei', sans-serif"/>
              </el-select>
            </el-form-item>

            <el-form-item label="字重">
              <el-radio-group v-model="formData.fontWeight">
                <el-radio-button label="normal">正常</el-radio-button>
                <el-radio-button label="bold">粗体</el-radio-button>
              </el-radio-group>
            </el-form-item>

            <el-divider/>

            <el-form-item label="发光效果">
              <el-switch v-model="formData.glowEffect"/>
              <span class="form-label-tip">启用 LED 发光模糊效果</span>
            </el-form-item>

            <el-form-item v-if="formData.glowEffect" label="发光强度">
              <el-radio-group v-model="formData.glowIntensity">
                <el-radio-button label="normal">普通</el-radio-button>
                <el-radio-button label="strong">强烈</el-radio-button>
              </el-radio-group>
            </el-form-item>

            <el-form-item label="七段数码管">
              <el-switch v-model="formData.sevenSegmentMode"/>
              <span class="form-label-tip">使用数码管字体样式</span>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 动画效果 -->
        <el-tab-pane label="动画效果" name="animation">
          <el-form :model="formData" label-width="120px" class="led-form">
            <el-alert
              title="动画说明"
              type="info"
              :closable="false"
              style="margin-bottom: 16px"
            >
              <div style="font-size: 12px; line-height: 1.8">
                <p style="margin: 4px 0">✨ <strong>闪烁</strong>: LED 明暗交替</p>
                <p style="margin: 4px 0">🚀 <strong>滚动</strong>: 文本从右向左滚动</p>
                <p style="margin: 4px 0">🌊 <strong>波浪</strong>: 文字上下波动</p>
                <p style="margin: 4px 0">⌨️ <strong>打字机</strong>: 逐字显示效果</p>
              </div>
            </el-alert>

            <el-form-item label="动画类型">
              <el-select v-model="formData.animation" placeholder="选择动画效果">
                <el-option label="无动画" value="none"/>
                <el-option label="闪烁效果" value="blink"/>
                <el-option label="滚动播放" value="scroll"/>
                <el-option label="波浪效果" value="wave"/>
                <el-option label="打字机效果" value="typewriter"/>
              </el-select>
            </el-form-item>

            <el-form-item
              v-if="formData.animation !== 'none'"
              label="动画速度"
            >
              <el-slider
                v-model="formData.animationSpeed"
                :min="100"
                :max="5000"
                :step="100"
                :marks="{ 100: '快', 1000: '中', 5000: '慢' }"
                show-input
              />
              <div class="form-tip">单位: 毫秒 (ms)</div>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 预览 -->
        <el-tab-pane label="预览" name="preview">
          <div class="preview-section">
            <div class="preview-title">实时预览</div>
            <div
              class="led-preview-container"
              :style="{
                backgroundColor: formData.backgroundColor
              }"
            >
              <div
                class="led-preview-text"
                :class="{
                  'glow-effect': formData.glowEffect,
                  'glow-strong': formData.glowIntensity === 'strong',
                  'multi-line': formData.multiLine
                }"
                :style="{
                  color: formData.color,
                  fontSize: formData.fontSize + 'px',
                  fontFamily: formData.fontFamily,
                  fontWeight: formData.fontWeight,
                  textAlign: formData.alignment,
                  lineHeight: formData.lineHeight
                }"
              >
                {{ formData.text || '88:88:88' }}
              </div>
            </div>

            <div class="preview-info">
              <el-descriptions :column="2" size="small" border>
                <el-descriptions-item label="显示文本">
                  {{ formData.text || '88:88:88' }}
                </el-descriptions-item>
                <el-descriptions-item label="字体大小">
                  {{ formData.fontSize }}px
                </el-descriptions-item>
                <el-descriptions-item label="LED颜色">
                  <div class="color-box" :style="{ backgroundColor: formData.color }"/>
                  {{ formData.color }}
                </el-descriptions-item>
                <el-descriptions-item label="动画">
                  {{ animationNames[formData.animation] }}
                </el-descriptions-item>
              </el-descriptions>
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
import type { LedDisplayConfig } from '../core/LedDisplayComponent'

// Props
const props = defineProps<{
  modelValue: boolean
  componentData?: any
}>()

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'save', config: LedDisplayConfig): void
}>()

// 响应式数据
const dialogVisible = ref(props.modelValue)
const activeTab = ref('content')

// 表单数据
const formData = reactive<LedDisplayConfig>({
  text: '88:88:88',
  color: '#ff3333',
  backgroundColor: '#0a0a0a',
  fontSize: 42,
  fontFamily: "'Courier New', monospace",
  fontWeight: 'bold',
  glowEffect: true,
  glowIntensity: 'strong',
  multiLine: false,
  lineHeight: 1.2,
  maxLines: 3,
  alignment: 'left',
  padding: 10,
  animation: 'none',
  animationSpeed: 1000,
  sevenSegmentMode: false,
  format: 'none'
})

// 预定义颜色
const predefineColors = [
  '#ff3333', // 红色 LED
  '#33ff33', // 绿色 LED
  '#3333ff', // 蓝色 LED
  '#ffff33', // 黄色 LED
  '#ff33ff', // 紫色 LED
  '#33ffff', // 青色 LED
  '#ffffff', // 白色 LED
  '#ff8800'  // 橙色 LED
]

// 动画名称映射
const animationNames: Record<string, string> = {
  none: '无动画',
  blink: '闪烁',
  scroll: '滚动',
  wave: '波浪',
  typewriter: '打字机'
}

// 监听对话框显示状态
watch(
  () => props.modelValue,
  (val) => {
    dialogVisible.value = val
    if (val && props.componentData) {
      // 加载现有配置
      Object.assign(formData, props.componentData.ledConfig || {})
    }
  }
)

watch(dialogVisible, (val) => {
  emit('update:modelValue', val)
})

/**
 * 保存配置
 */
const handleSave = () => {
  emit('save', { ...formData })
  dialogVisible.value = false
}

/**
 * 重置表单
 */
const handleReset = () => {
  Object.assign(formData, {
    text: '88:88:88',
    color: '#ff3333',
    backgroundColor: '#0a0a0a',
    fontSize: 42,
    fontFamily: "'Courier New', monospace",
    fontWeight: 'bold',
    glowEffect: true,
    glowIntensity: 'strong',
    multiLine: false,
    lineHeight: 1.2,
    maxLines: 3,
    alignment: 'left',
    padding: 10,
    animation: 'none',
    animationSpeed: 1000,
    sevenSegmentMode: false,
    format: 'none'
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
.led-display-property-dialog {
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

.led-display-dialog-content {
  padding: 0 20px 20px 20px;

  .led-tabs {
    :deep(.el-tabs__content) {
      max-height: 50vh;
      overflow-y: auto;
      padding: 16px 0;
    }
  }
}

.led-form {
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

  .color-preview {
    margin-left: 12px;
    font-size: 20px;
    font-weight: bold;
    text-shadow: 0 0 10px currentColor;
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

  .led-preview-container {
    padding: 30px;
    border-radius: 8px;
    border: 2px solid #333;
    margin-bottom: 20px;
    min-height: 150px;
    display: flex;
    align-items: center;
    justify-content: center;

    .led-preview-text {
      font-family: 'Courier New', monospace;
      font-weight: bold;
      white-space: pre-wrap;
      word-break: break-all;

      &.glow-effect {
        text-shadow: 0 0 10px currentColor,
                     0 0 20px currentColor;
      }

      &.glow-strong {
        text-shadow: 0 0 10px currentColor,
                     0 0 20px currentColor,
                     0 0 30px currentColor,
                     0 0 40px currentColor;
      }

      &.multi-line {
        white-space: pre-wrap;
      }
    }
  }

  .preview-info {
    .color-box {
      display: inline-block;
      width: 16px;
      height: 16px;
      border-radius: 4px;
      border: 1px solid #ddd;
      vertical-align: middle;
      margin-right: 8px;
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
