<template>
  <el-dialog
    v-model="dialogVisible"
    width="700px"
    top="8vh"
    :close-on-click-modal="false"
    draggable
    align-center
    class="thermometer-property-dialog"
    @close="handleClose"
  >
    <!-- 自定义头部 -->
    <template #header="{ close }">
      <div class="custom-dialog-header">
        <div class="header-left">
          <div class="header-icon">🌡️</div>
          <div class="header-content">
            <h3 class="header-title">温度计属性配置</h3>
            <p class="header-subtitle">配置温度计的外观、刻度和液体动画效果</p>
          </div>
        </div>
        <div class="header-right">
          <el-button link size="small" class="action-btn" @click="close">
            <el-icon><Close /></el-icon>
          </el-button>
        </div>
      </div>
    </template>

    <div class="dialog-content">
      <el-form :model="formData" label-width="100px" class="thermometer-form">
        <!-- 基本配置 -->
        <div class="form-section">
          <div class="section-title">基本配置</div>

          <el-form-item label="方向">
            <el-radio-group v-model="formData.options.orientation">
              <el-radio label="vertical">
                <span style="display: inline-flex; align-items: center; gap: 4px;">
                  <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                    <rect x="6" y="2" width="4" height="12" rx="2"/>
                  </svg>
                  垂直
                </span>
              </el-radio>
              <el-radio label="horizontal">
                <span style="display: inline-flex; align-items: center; gap: 4px;">
                  <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                    <rect x="2" y="6" width="12" height="4" rx="2"/>
                  </svg>
                  水平
                </span>
              </el-radio>
            </el-radio-group>
          </el-form-item>

          <el-form-item label="范围">
            <el-row :gutter="12">
              <el-col :span="12">
                <el-input-number
                  v-model="formData.options.range.min"
                  :step="1"
                  controls-position="right"
                  style="width: 100%"
                  placeholder="最小值"
                />
              </el-col>
              <el-col :span="12">
                <el-input-number
                  v-model="formData.options.range.max"
                  :step="1"
                  controls-position="right"
                  style="width: 100%"
                  placeholder="最大值"
                />
              </el-col>
            </el-row>
          </el-form-item>

          <el-form-item label="步长">
            <el-input-number
              v-model="formData.options.step"
              :min="0.01"
              :step="0.1"
              :precision="2"
              controls-position="right"
              style="width: 100%"
            />
            <div class="form-tip">数值变化的最小单位</div>
          </el-form-item>

          <el-form-item label="当前值">
            <el-input-number
              v-model="formData.value"
              :min="formData.options.range.min"
              :max="formData.options.range.max"
              :step="formData.options.step"
              controls-position="right"
              style="width: 100%"
            />
          </el-form-item>
        </div>

        <!-- 颜色配置 -->
        <div class="form-section">
          <div class="section-title">颜色配置</div>

          <el-form-item label="容器背景色">
            <div class="color-picker-group">
              <el-color-picker v-model="formData.options.shape.baseColor" />
              <el-input
                v-model="formData.options.shape.baseColor"
                placeholder="#FAFAFA"
                style="flex: 1"
              >
                <template #prepend>
                  <span style="color: #909399;">#</span>
                </template>
              </el-input>
            </div>
          </el-form-item>

          <el-form-item label="液体颜色">
            <div class="color-picker-group">
              <el-color-picker v-model="formData.options.shape.connectColor" />
              <el-input
                v-model="formData.options.shape.connectColor"
                placeholder="#4CAF50"
                style="flex: 1"
              >
                <template #prepend>
                  <span style="color: #909399;">#</span>
                </template>
              </el-input>
            </div>
            <div class="form-tip">支持根据数值自动改变颜色 (低温=绿色, 中温=黄色, 高温=红色)</div>
          </el-form-item>

          <el-form-item label="刻度颜色">
            <div class="color-picker-group">
              <el-color-picker v-model="formData.options.shape.handleColor" />
              <el-input
                v-model="formData.options.shape.handleColor"
                placeholder="#666666"
                style="flex: 1"
              >
                <template #prepend>
                  <span style="color: #909399;">#</span>
                </template>
              </el-input>
            </div>
          </el-form-item>

          <!-- 预设颜色方案 -->
          <el-form-item label="预设方案">
            <div class="color-preset-grid">
              <div
                v-for="preset in colorPresets"
                :key="preset.name"
                class="color-preset-item"
                @click="applyColorPreset(preset)"
              >
                <div class="preset-colors">
                  <span class="color-dot" :style="{ background: preset.base }"></span>
                  <span class="color-dot" :style="{ background: preset.connect }"></span>
                  <span class="color-dot" :style="{ background: preset.handle }"></span>
                </div>
                <div class="preset-name">{{ preset.name }}</div>
              </div>
            </div>
          </el-form-item>
        </div>

        <!-- 刻度配置 -->
        <div class="form-section">
          <div class="section-title">刻度配置</div>

          <el-form-item label="显示刻度">
            <el-switch v-model="showPips" @change="handleShowPipsChange" />
            <span style="margin-left: 10px; color: #909399; font-size: 12px">
              显示刻度线和数值标签
            </span>
          </el-form-item>

          <template v-if="showPips">
            <el-form-item label="刻度值">
              <el-select
                v-model="pipsPreset"
                placeholder="选择预设或自定义"
                @change="handlePipsPresetChange"
                style="width: 100%"
              >
                <el-option label="3刻度 (0, 50, 100)" value="3" />
                <el-option label="5刻度 (0, 25, 50, 75, 100)" value="5" />
                <el-option label="11刻度 (0, 10, 20...100)" value="11" />
                <el-option label="自定义" value="custom" />
              </el-select>
            </el-form-item>

            <el-form-item v-if="pipsPreset === 'custom'" label="自定义刻度">
              <el-input
                v-model="customPipsInput"
                placeholder="用逗号分隔，如: 0,25,50,75,100"
                @blur="handleCustomPipsBlur"
              />
              <div class="form-tip">请输入数值，用逗号分隔</div>
            </el-form-item>

            <el-form-item label="刻度颜色">
              <div class="color-picker-group">
                <el-color-picker v-model="formData.options.marker.color" />
                <el-input
                  v-model="formData.options.marker.color"
                  placeholder="#666666"
                  style="flex: 1"
                >
                  <template #prepend>
                    <span style="color: #909399;">#</span>
                  </template>
                </el-input>
              </div>
            </el-form-item>

            <el-form-item label="字体大小">
              <el-slider
                v-model="formData.options.marker.fontSize"
                :min="10"
                :max="24"
                :step="1"
                show-input
              />
            </el-form-item>
          </template>
        </div>

        <!-- 提示框配置 -->
        <div class="form-section">
          <div class="section-title">提示框配置</div>

          <el-form-item label="提示框">
            <el-radio-group v-model="formData.options.tooltip.type">
              <el-radio label="none">不显示</el-radio>
              <el-radio label="show">始终显示</el-radio>
            </el-radio-group>
          </el-form-item>

          <template v-if="formData.options.tooltip.type === 'show'">
            <el-form-item label="小数位数">
              <el-input-number
                v-model="formData.options.tooltip.decimals"
                :min="0"
                :max="4"
                :step="1"
                controls-position="right"
                style="width: 100%"
              />
            </el-form-item>

            <el-form-item label="背景颜色">
              <div class="color-picker-group">
                <el-color-picker v-model="formData.options.tooltip.background" />
                <el-input
                  v-model="formData.options.tooltip.background"
                  placeholder="#333333"
                  style="flex: 1"
                >
                  <template #prepend>
                    <span style="color: #909399;">#</span>
                  </template>
                </el-input>
              </div>
            </el-form-item>

            <el-form-item label="文字颜色">
              <div class="color-picker-group">
                <el-color-picker v-model="formData.options.tooltip.color" />
                <el-input
                  v-model="formData.options.tooltip.color"
                  placeholder="#FFFFFF"
                  style="flex: 1"
                >
                  <template #prepend>
                    <span style="color: #909399;">#</span>
                  </template>
                </el-input>
              </div>
            </el-form-item>
          </template>
        </div>
      </el-form>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleClose">取消</el-button>
        <el-button type="primary" @click="handleSave">
          <el-icon><Check /></el-icon>
          保存配置
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue";
import { ElMessage } from "element-plus";
import { Check, Close } from "@element-plus/icons-vue";

// Props
const props = defineProps({
  visible: {
    type: Boolean,
    default: false
  },
  thermometerComponent: {
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
  value: 50,
  options: {
    orientation: 'vertical',
    direction: 'ltr',
    fontFamily: 'Sans-serif',
    shape: {
      baseColor: '#FAFAFA',
      connectColor: '#3FB8AF',
      handleColor: '#CFF'
    },
    marker: {
      color: '#666666',
      subWidth: 5,
      subHeight: 1,
      fontSize: 14,
      divHeight: 2,
      divWidth: 15
    },
    range: {
      min: 0,
      max: 100
    },
    step: 1,
    pips: {
      mode: 'values',
      values: [0, 50, 100],
      density: 4
    },
    tooltip: {
      type: 'none',
      decimals: 0,
      background: '#333333',
      color: '#FFFFFF',
      fontSize: 11
    }
  }
});

// 刻度显示开关
const showPips = ref(true);
const pipsPreset = ref('3');
const customPipsInput = ref('0,50,100');

// 颜色预设方案
const colorPresets = [
  { name: '默认', base: '#FAFAFA', connect: '#3FB8AF', handle: '#CFF' },
  { name: '科技蓝', base: '#E3F2FD', connect: '#2196F3', handle: '#64B5F6' },
  { name: '活力橙', base: '#FFF3E0', connect: '#FF9800', handle: '#FFB74D' },
  { name: '清新绿', base: '#E8F5E9', connect: '#4CAF50', handle: '#81C784' },
  { name: '优雅紫', base: '#F3E5F5', connect: '#9C27B0', handle: '#BA68C8' },
  { name: '深邃黑', base: '#ECEFF1', connect: '#263238', handle: '#546E7A' }
];

// 监听对话框打开，初始化数据
watch(
  () => props.visible,
  (newVal) => {
    if (newVal && props.thermometerComponent?.properties) {
      formData.value = {
        value: props.thermometerComponent.properties.value || 50,
        options: {
          ...formData.value.options,
          ...props.thermometerComponent.properties.options
        }
      };

      // 初始化刻度显示状态
      const pipsValues = props.thermometerComponent.properties.options?.pips?.values;
      showPips.value = !!(pipsValues && pipsValues.length > 0);

      if (showPips.value) {
        const len = pipsValues.length;
        if (len === 3 && pipsValues[1] === 50) pipsPreset.value = '3';
        else if (len === 5) pipsPreset.value = '5';
        else if (len === 11) pipsPreset.value = '11';
        else {
          pipsPreset.value = 'custom';
          customPipsInput.value = pipsValues.join(',');
        }
      }
    }
  }
);

// 应用颜色预设
const applyColorPreset = (preset: any) => {
  formData.value.options.shape.baseColor = preset.base;
  formData.value.options.shape.connectColor = preset.connect;
  formData.value.options.shape.handleColor = preset.handle;
  ElMessage.success(`已应用 ${preset.name} 配色方案`);
};

// 处理刻度显示开关
const handleShowPipsChange = (value: boolean) => {
  if (!value) {
    formData.value.options.pips.values = [];
  } else {
    handlePipsPresetChange('3');
  }
};

// 处理刻度预设变化
const handlePipsPresetChange = (preset: string) => {
  const min = formData.value.options.range.min;
  const max = formData.value.options.range.max;
  const range = max - min;

  switch (preset) {
    case '3':
      formData.value.options.pips.values = [min, min + range / 2, max];
      break;
    case '5':
      formData.value.options.pips.values = [
        min,
        min + range * 0.25,
        min + range * 0.5,
        min + range * 0.75,
        max
      ];
      break;
    case '11':
      formData.value.options.pips.values = Array.from(
        { length: 11 },
        (_, i) => min + (range / 10) * i
      );
      break;
    case 'custom':
      // 自定义，不做任何操作
      break;
  }
};

// 处理自定义刻度输入
const handleCustomPipsBlur = () => {
  try {
    const values = customPipsInput.value
      .split(',')
      .map(v => parseFloat(v.trim()))
      .filter(v => !isNaN(v))
      .sort((a, b) => a - b);

    if (values.length > 0) {
      formData.value.options.pips.values = values;
      ElMessage.success(`已设置 ${values.length} 个刻度值`);
    } else {
      ElMessage.warning('请输入有效的数值');
    }
  } catch (error) {
    ElMessage.error('刻度值格式错误');
  }
};

// 保存配置
const handleSave = () => {
  // 验证范围
  if (formData.value.options.range.min >= formData.value.options.range.max) {
    ElMessage.error('最小值必须小于最大值');
    return;
  }

  // 验证当前值
  const { min, max } = formData.value.options.range;
  if (formData.value.value < min || formData.value.value > max) {
    ElMessage.error(`当前值必须在 ${min} 到 ${max} 之间`);
    return;
  }

  emit("save-config", { ...formData.value });
  ElMessage.success("温度计配置已保存");
  dialogVisible.value = false;
};

// 关闭对话框
const handleClose = () => {
  dialogVisible.value = false;
};
</script>

<style scoped lang="scss">
.thermometer-property-dialog {
  :deep(.el-dialog) {
    border-radius: 12px;
    overflow: hidden;

    .el-dialog__header {
      padding: 0;
      margin: 0;
    }

    .el-dialog__body {
      padding: 0;
    }
  }
}

.custom-dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;

    .header-icon {
      font-size: 28px;
      filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.2));
    }

    .header-content {
      .header-title {
        margin: 0;
        font-size: 16px;
        font-weight: 600;
        color: white;
        text-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
      }

      .header-subtitle {
        margin: 2px 0 0 0;
        font-size: 12px;
        color: rgba(255, 255, 255, 0.8);
      }
    }
  }

  .header-right {
    .action-btn {
      width: 32px;
      height: 32px;
      border-radius: 8px;
      background: rgba(255, 255, 255, 0.1);
      color: white;

      &:hover {
        background: rgba(255, 255, 255, 0.2);
      }
    }
  }
}

.dialog-content {
  max-height: 65vh;
  overflow-y: auto;
  padding: 20px;
}

.thermometer-form {
  .form-section {
    margin-bottom: 24px;
    padding-bottom: 20px;
    border-bottom: 1px solid #eee;

    &:last-child {
      border-bottom: none;
      margin-bottom: 0;
    }

    .section-title {
      font-size: 14px;
      font-weight: 600;
      color: #303133;
      margin-bottom: 16px;
      display: flex;
      align-items: center;
      gap: 8px;

      &::before {
        content: '';
        width: 3px;
        height: 14px;
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        border-radius: 2px;
      }
    }
  }

  :deep(.el-form-item) {
    margin-bottom: 18px;

    .el-form-item__label {
      font-size: 13px;
      color: #606266;
      font-weight: 500;
    }
  }

  .form-tip {
    font-size: 12px;
    color: #909399;
    margin-top: 4px;
  }
}

.color-picker-group {
  display: flex;
  align-items: center;
  gap: 12px;

  .el-color-picker {
    flex-shrink: 0;
  }
}

.color-preset-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 10px;

  .color-preset-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 10px 6px;
    border: 1.5px solid #dcdfe6;
    border-radius: 6px;
    cursor: pointer;
    transition: all 0.2s;

    &:hover {
      border-color: #667eea;
      background: #f5f7fa;
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(102, 126, 234, 0.15);
    }

    .preset-colors {
      display: flex;
      gap: 6px;
      margin-bottom: 8px;

      .color-dot {
        width: 20px;
        height: 20px;
        border-radius: 50%;
        border: 2px solid #fff;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.15);
      }
    }

    .preset-name {
      font-size: 11px;
      color: #606266;
      font-weight: 500;
    }
  }
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 20px;
  border-top: 1px solid #eee;
  background: #f8f9fa;

  .el-button {
    min-width: 90px;
    height: 36px;
    border-radius: 6px;
    font-weight: 500;

    &.el-button--primary {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border: none;

      &:hover {
        background: linear-gradient(135deg, #5a67d8 0%, #6b46c1 100%);
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
      }
    }
  }
}
</style>
