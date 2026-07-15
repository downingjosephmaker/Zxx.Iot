<template>
  <el-dialog
    v-model="dialogVisible"
    width="640px"
    top="10vh"
    :close-on-click-modal="false"
    :close-on-press-escape="true"
    draggable
    align-center
    :show-close="false"
    class="stat-card-property-dialog"
    @close="handleClose"
  >
    <!-- 自定义头部 -->
    <template #header="{ close }">
      <div class="custom-dialog-header">
        <div class="header-left">
          <div class="header-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
              <rect x="3" y="5" width="18" height="14" rx="2" stroke="currentColor" stroke-width="2"/>
              <text x="7" y="16" font-size="9" font-weight="700" fill="currentColor">88</text>
            </svg>
          </div>
          <div class="header-content">
            <h3 class="header-title">统计数值卡配置</h3>
            <p class="header-subtitle">大数字 + 单位 + 标题,绑定点位后由运行态写入数值</p>
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

    <div class="stat-card-dialog-content">
      <el-tabs v-model="activeTab" class="card-tabs">
        <!-- 内容设置 -->
        <el-tab-pane label="内容设置" name="content">
          <el-form :model="formData" label-width="110px" class="card-form">
            <el-form-item label="标题">
              <el-input v-model="formData.title" placeholder="数值上方的小字,如:今日用电量" clearable/>
            </el-form-item>
            <el-form-item label="数值(占位)">
              <el-input v-model="formData.value" placeholder="绑定点位后由运行态写入,此处仅编辑期占位" clearable/>
              <div class="form-tip">💡 运行态会用绑定的点位值覆盖此处</div>
            </el-form-item>
            <el-form-item label="单位">
              <el-input v-model="formData.unit" placeholder="数值右侧的小字,如:kWh" clearable/>
            </el-form-item>
            <el-form-item label="副标题">
              <el-input v-model="formData.caption" placeholder="数值下方的备注(可选)" clearable/>
            </el-form-item>
            <el-form-item label="对齐方式">
              <el-radio-group v-model="formData.align">
                <el-radio-button label="left">左对齐</el-radio-button>
                <el-radio-button label="center">居中</el-radio-button>
                <el-radio-button label="right">右对齐</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 卡片样式 -->
        <el-tab-pane label="卡片样式" name="style">
          <el-form :model="formData" label-width="110px" class="card-form">
            <el-form-item label="标题颜色">
              <el-color-picker v-model="formData.titleColor" show-alpha/>
            </el-form-item>
            <el-form-item label="数值颜色">
              <el-color-picker v-model="formData.valueColor" show-alpha/>
            </el-form-item>
            <el-form-item label="背景颜色">
              <el-color-picker v-model="formData.backgroundColor" show-alpha/>
            </el-form-item>
            <el-form-item label="边框颜色">
              <el-color-picker v-model="formData.borderColor" show-alpha/>
            </el-form-item>
            <el-form-item label="圆角半径">
              <el-slider v-model="formData.borderRadius" :min="0" :max="50" :step="1" show-input/>
            </el-form-item>
            <el-form-item label="数值字号">
              <el-input-number v-model="formData.valueFontSize" :min="0" :max="200" :step="2"/>
              <span class="form-label-tip">0 = 按卡片高度自适应</span>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 预览 -->
        <el-tab-pane label="预览" name="preview">
          <div class="preview-section">
            <div class="preview-title">实时预览</div>
            <div
              class="stat-preview-container"
              :style="{
                alignItems:
                  formData.align === 'left'
                    ? 'flex-start'
                    : formData.align === 'right'
                      ? 'flex-end'
                      : 'center',
                background: formData.backgroundColor,
                borderColor: formData.borderColor,
                borderRadius: formData.borderRadius + 'px'
              }"
            >
              <div class="p-title" :style="{ color: formData.titleColor }">
                {{ formData.title || '统计指标' }}
              </div>
              <div class="p-value-row">
                <span
                  class="p-value"
                  :style="{
                    color: formData.valueColor,
                    fontSize: (formData.valueFontSize || 40) + 'px'
                  }"
                >
                  {{ formData.value || '--' }}
                </span>
                <span
                  v-if="formData.unit"
                  class="p-unit"
                  :style="{ color: formData.titleColor }"
                >
                  {{ formData.unit }}
                </span>
              </div>
              <div
                v-if="formData.caption"
                class="p-caption"
                :style="{ color: formData.titleColor }"
              >
                {{ formData.caption }}
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
import type { StatCardConfig } from '../core/StatCardComponent'

const props = defineProps<{
  visible: boolean
  statCardComponent?: any
}>()

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void
  (e: 'save-config', config: StatCardConfig): void
}>()

const dialogVisible = ref(props.visible)
const activeTab = ref('content')

/** 默认值与 StatCardComponent.normalize 的回落保持一致 */
const defaults = (): StatCardConfig => ({
  title: '统计指标',
  value: '--',
  unit: '',
  caption: '',
  titleColor: '#909399',
  valueColor: '#303133',
  backgroundColor: '#ffffff',
  borderColor: '#e4e7ed',
  borderRadius: 6,
  valueFontSize: 0,
  align: 'center'
})

const formData = reactive<StatCardConfig>(defaults())

// 监听对话框显示状态：打开时载入组件已有配置
watch(
  () => props.visible,
  val => {
    dialogVisible.value = val
    if (val && props.statCardComponent) {
      Object.assign(formData, defaults(), props.statCardComponent.properties || {})
    }
  }
)

watch(dialogVisible, val => {
  emit('update:visible', val)
})

const handleSave = () => {
  emit('save-config', { ...formData })
  dialogVisible.value = false
}

const handleReset = () => {
  Object.assign(formData, defaults())
}

const handleClose = () => {
  dialogVisible.value = false
}
</script>

<style scoped lang="scss">
.stat-card-property-dialog {
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
      background: linear-gradient(135deg, #409eff 0%, #1d6fd6 100%);
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

.stat-card-dialog-content {
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

.preview-section {
  .preview-title {
    font-size: 14px;
    font-weight: 600;
    margin-bottom: 12px;
    color: #303133;
  }

  .stat-preview-container {
    display: flex;
    flex-direction: column;
    justify-content: center;
    gap: 4px;
    min-height: 160px;
    padding: 10px 14px;
    border: 1px solid #e4e7ed;
    border-style: solid;

    .p-title {
      font-size: 13px;
      line-height: 1.2;
    }

    .p-value-row {
      display: flex;
      align-items: baseline;
      gap: 4px;

      .p-value {
        font-weight: 600;
        line-height: 1.1;
      }

      .p-unit {
        font-size: 14px;
      }
    }

    .p-caption {
      font-size: 12px;
      line-height: 1.2;
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
