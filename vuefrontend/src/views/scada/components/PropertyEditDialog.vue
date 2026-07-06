<template>
  <el-dialog
    v-model="dialogVisible"
    width="900px"
    top="5vh"
    :close-on-click-modal="false"
    :close-on-press-escape="true"
    draggable
    align-center
    :show-close="false"
    class="property-edit-dialog"
  >
    <!-- 自定义头部 -->
    <template #header="{ close }">
      <div class="custom-dialog-header">
        <div class="header-left">
          <div class="header-icon">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path
                d="M12 2L13.09 8.26L20 9L13.09 9.74L12 16L10.91 9.74L4 9L10.91 8.26L12 2Z"
                fill="currentColor"
              />
              <path
                d="M19 15L20.09 17.26L23 18L20.09 18.74L19 21L17.91 18.74L15 18L17.91 17.26L19 15Z"
                fill="currentColor"
              />
              <path
                d="M5 9L6.09 11.26L9 12L6.09 12.74L5 15L3.91 12.74L1 12L3.91 11.26L5 9Z"
                fill="currentColor"
              />
            </svg>
          </div>
          <div class="header-content">
            <h3 class="header-title">属性编辑器</h3>
            <p class="header-subtitle" v-if="selectedComponent">
              {{
                selectedComponent.name || selectedComponent.type || "未命名组件"
              }}
            </p>
          </div>
        </div>
        <div class="header-right">
          <div class="header-actions">
            <el-button
              link
              size="small"
              class="action-btn minimize-btn"
              @click="minimizeDialog"
            >
              <el-icon><Minus /></el-icon>
            </el-button>
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
    <div v-if="selectedComponent" class="property-dialog-content">
      <el-tabs v-model="activeTab" type="border-card">
        <!-- SVG图标设置 -->
        <el-tab-pane label="SVG图标" name="shape">
          <SvgIconStyleConfig
            :selectedComponent="formData"
            @update-style="updateStyleData"
          />
        </el-tab-pane>

        <!-- 事件设置 -->
        <el-tab-pane label="事件设置" name="events">
          <div class="events-section">
            <!-- 快速添加事件 -->
            <div class="property-section">
              <div class="section-title">快速添加事件</div>
              <el-row :gutter="8">
                <el-col :span="6">
                  <el-button
                    type="primary"
                    size="small"
                    @click="addQuickEvent('click')"
                  >
                    <el-icon><Mouse /></el-icon>
                    点击
                  </el-button>
                </el-col>
                <el-col :span="6">
                  <el-button
                    type="primary"
                    size="small"
                    @click="addQuickEvent('hover')"
                  >
                    <el-icon><Position /></el-icon>
                    悬停
                  </el-button>
                </el-col>
                <el-col :span="6">
                  <el-button
                    type="primary"
                    size="small"
                    @click="addQuickEvent('valuechange')"
                  >
                    <el-icon><TrendCharts /></el-icon>
                    数值变化
                  </el-button>
                </el-col>
                <el-col :span="6">
                  <el-button
                    type="primary"
                    size="small"
                    @click="addQuickEvent('timer')"
                  >
                    <el-icon><Timer /></el-icon>
                    定时器
                  </el-button>
                </el-col>
              </el-row>
            </div>

            <!-- 事件列表 -->
            <div class="events-list">
              <div
                v-for="(event, eventIndex) in formData.events || []"
                :key="`event-${eventIndex}`"
                class="event-item"
              >
                <div class="event-header">
                  <div class="event-info">
                    <el-icon class="event-icon"><Operation /></el-icon>
                    <span class="event-name">{{
                      getEventTypeName(event.type)
                    }}</span>
                  </div>
                  <div class="event-controls">
                    <el-switch
                      :model-value="event.enabled"
                      size="small"
                      @change="val => toggleEventEnabled(eventIndex, val)"
                    />
                    <el-button
                      link
                      size="small"
                      @click="removeEvent(eventIndex)"
                    >
                      <el-icon><Delete /></el-icon>
                    </el-button>
                  </div>
                </div>

                <div v-if="event.enabled" class="event-config">
                  <!-- 事件类型和触发条件 -->
                  <el-row :gutter="16">
                    <el-col :span="12">
                      <el-form-item label="事件类型" size="small">
                        <el-select
                          :model-value="event.type"
                          style="width: 100%"
                          @change="val => updateEventType(eventIndex, val)"
                        >
                          <el-option label="点击事件" value="click" />
                          <el-option label="双击事件" value="dblclick" />
                          <el-option label="鼠标悬停" value="hover" />
                          <el-option label="鼠标离开" value="leave" />
                          <el-option label="数值变化" value="valuechange" />
                          <el-option label="定时器" value="timer" />
                        </el-select>
                      </el-form-item>
                    </el-col>
                    <el-col :span="12">
                      <el-form-item
                        label="触发条件"
                        size="small"
                        v-if="event.type === 'valuechange'"
                      >
                        <el-input
                          :model-value="event.condition || ''"
                          placeholder="如: value > 100"
                          @input="val => updateEventCondition(eventIndex, val)"
                        />
                      </el-form-item>
                      <el-form-item
                        label="间隔时间(ms)"
                        size="small"
                        v-if="event.type === 'timer'"
                      >
                        <el-input-number
                          :model-value="event.interval || 1000"
                          :min="100"
                          :max="60000"
                          @input="val => updateEventInterval(eventIndex, val)"
                        />
                      </el-form-item>
                    </el-col>
                  </el-row>

                  <!-- 执行动作 -->
                  <div class="actions-section">
                    <div class="section-header">
                      <span>执行动作</span>
                      <el-dropdown
                        @command="
                          command => addEventAction(eventIndex, command)
                        "
                      >
                        <el-button link size="small">
                          <el-icon><Plus /></el-icon>
                          添加动作
                        </el-button>
                        <template #dropdown>
                          <el-dropdown-menu>
                            <el-dropdown-item command="visibility"
                              >显示/隐藏</el-dropdown-item
                            >
                            <el-dropdown-item command="appearanceStyle"
                              >外观样式</el-dropdown-item
                            >
                            <el-dropdown-item command="svgIconStyle"
                              >SVG图标</el-dropdown-item
                            >
                            <el-dropdown-item command="move"
                              >位置移动</el-dropdown-item
                            >
                            <el-dropdown-item command="setValue"
                              >设置数值</el-dropdown-item
                            >
                            <el-dropdown-item command="dialog"
                              >弹出对话框</el-dropdown-item
                            >
                            <el-dropdown-item command="command"
                              >发送命令</el-dropdown-item
                            >
                          </el-dropdown-menu>
                        </template>
                      </el-dropdown>
                    </div>

                    <div
                      v-for="(action, actionIndex) in event.actions || []"
                      :key="`action-${actionIndex}`"
                      class="action-item"
                    >
                      <div class="action-header">
                        <el-icon class="action-icon"><Lightning /></el-icon>
                        <span>{{ getActionTypeName(action.type) }}</span>
                        <el-button
                          link
                          size="small"
                          @click="removeEventAction(eventIndex, actionIndex)"
                        >
                          <el-icon><Close /></el-icon>
                        </el-button>
                      </div>

                      <div class="action-config">
                        <!-- 外观样式动作 -->
                        <div
                          v-if="action.type === 'appearanceStyle'"
                          class="appearance-style-config"
                        >
                          <AppearanceStyleConfig
                            :selectedComponent="{ style: action.style }"
                            @update-style="
                              (property, value) =>
                                updateActionStyleProperty(
                                  eventIndex,
                                  actionIndex,
                                  property,
                                  value
                                )
                            "
                          />
                        </div>

                        <!-- 保持兼容旧的背景颜色变化动作 -->
                        <div
                          v-if="action.type === 'backgroundColorChange'"
                          class="color-change-config"
                        >
                          <el-form-item label="颜色类型" size="small">
                            <el-select
                              :model-value="action.colorType || 'solid'"
                              @change="
                                val =>
                                  updateActionProperty(
                                    eventIndex,
                                    actionIndex,
                                    'colorType',
                                    val
                                  )
                              "
                            >
                              <el-option label="纯色" value="solid" />
                              <el-option
                                label="线性渐变"
                                value="linear-gradient"
                              />
                              <el-option
                                label="径向渐变"
                                value="radial-gradient"
                              />
                              <el-option label="透明" value="transparent" />
                            </el-select>
                          </el-form-item>

                          <!-- 纯色设置 -->
                          <el-form-item
                            label="背景色"
                            size="small"
                            v-if="
                              action.colorType === 'solid' || !action.colorType
                            "
                          >
                            <el-color-picker
                              :model-value="action.backgroundColor || '#409eff'"
                              show-alpha
                              @change="
                                val =>
                                  updateActionProperty(
                                    eventIndex,
                                    actionIndex,
                                    'backgroundColor',
                                    val
                                  )
                              "
                            />
                          </el-form-item>

                          <!-- 渐变色设置 -->
                          <template
                            v-if="
                              action.colorType === 'linear-gradient' ||
                              action.colorType === 'radial-gradient'
                            "
                          >
                            <el-row :gutter="16">
                              <el-col :span="12">
                                <el-form-item label="起始色" size="small">
                                  <el-color-picker
                                    :model-value="
                                      action.gradientStart || '#409eff'
                                    "
                                    show-alpha
                                    @change="
                                      val =>
                                        updateActionProperty(
                                          eventIndex,
                                          actionIndex,
                                          'gradientStart',
                                          val
                                        )
                                    "
                                  />
                                </el-form-item>
                              </el-col>
                              <el-col :span="12">
                                <el-form-item label="结束色" size="small">
                                  <el-color-picker
                                    :model-value="
                                      action.gradientEnd || '#67c23a'
                                    "
                                    show-alpha
                                    @change="
                                      val =>
                                        updateActionProperty(
                                          eventIndex,
                                          actionIndex,
                                          'gradientEnd',
                                          val
                                        )
                                    "
                                  />
                                </el-form-item>
                              </el-col>
                            </el-row>
                            <el-form-item
                              label="渐变角度"
                              size="small"
                              v-if="action.colorType === 'linear-gradient'"
                            >
                              <el-slider
                                :model-value="action.gradientAngle || 0"
                                :min="0"
                                :max="360"
                                :step="1"
                                show-input
                                @input="
                                  val =>
                                    updateActionProperty(
                                      eventIndex,
                                      actionIndex,
                                      'gradientAngle',
                                      val
                                    )
                                "
                              />
                              <span class="unit">°</span>
                            </el-form-item>
                          </template>
                        </div>

                        <!-- SVG图标动作 -->
                        <div
                          v-if="action.type === 'svgIconStyle'"
                          class="svg-icon-style-config"
                        >
                          <SvgIconStyleConfig
                            :selectedComponent="{ style: action.style }"
                            @update-style="
                              (property, value) =>
                                updateActionStyleProperty(
                                  eventIndex,
                                  actionIndex,
                                  property,
                                  value
                                )
                            "
                          />
                        </div>

                        <!-- 保持兼容旧的SVG颜色变化动作 -->
                        <div
                          v-if="action.type === 'svgColorChange'"
                          class="svg-color-change-config"
                        >
                          <el-form-item label="SVG属性" size="small">
                            <el-select
                              :model-value="action.svgProperty || 'fill'"
                              @change="
                                val =>
                                  updateActionProperty(
                                    eventIndex,
                                    actionIndex,
                                    'svgProperty',
                                    val
                                  )
                              "
                            >
                              <el-option label="填充色" value="fill" />
                              <el-option label="描边色" value="stroke" />
                            </el-select>
                          </el-form-item>

                          <el-form-item label="颜色值" size="small">
                            <el-color-picker
                              :model-value="action.svgColor || null"
                              show-alpha
                              @change="
                                val =>
                                  updateActionProperty(
                                    eventIndex,
                                    actionIndex,
                                    'svgColor',
                                    val
                                  )
                              "
                            />
                          </el-form-item>

                          <el-form-item label="透明度" size="small">
                            <el-slider
                              :model-value="action.opacity || 1"
                              :min="0"
                              :max="1"
                              :step="0.01"
                              :precision="2"
                              show-input
                              @input="
                                val =>
                                  updateActionProperty(
                                    eventIndex,
                                    actionIndex,
                                    'opacity',
                                    val
                                  )
                              "
                            />
                          </el-form-item>
                        </div>

                        <!-- 其他动作配置保持不变 -->
                        <el-form-item
                          label="目标值"
                          size="small"
                          v-if="action.type === 'setValue'"
                        >
                          <el-input
                            :model-value="action.value || ''"
                            placeholder="要设置的数值"
                            @input="
                              val =>
                                updateActionValue(eventIndex, actionIndex, val)
                            "
                          />
                        </el-form-item>

                        <el-form-item
                          label="对话框内容"
                          size="small"
                          v-if="action.type === 'dialog'"
                        >
                          <el-input
                            :model-value="action.message || ''"
                            placeholder="对话框显示内容"
                            @input="
                              val =>
                                updateActionMessage(
                                  eventIndex,
                                  actionIndex,
                                  val
                                )
                            "
                          />
                        </el-form-item>

                        <el-form-item
                          label="移动距离"
                          size="small"
                          v-if="action.type === 'move'"
                        >
                          <el-row :gutter="8">
                            <el-col :span="12">
                              <el-input-number
                                :model-value="action.deltaX || 0"
                                placeholder="X轴"
                                size="small"
                                @input="
                                  val =>
                                    updateActionProperty(
                                      eventIndex,
                                      actionIndex,
                                      'deltaX',
                                      val
                                    )
                                "
                              />
                            </el-col>
                            <el-col :span="12">
                              <el-input-number
                                :model-value="action.deltaY || 0"
                                placeholder="Y轴"
                                size="small"
                                @input="
                                  val =>
                                    updateActionProperty(
                                      eventIndex,
                                      actionIndex,
                                      'deltaY',
                                      val
                                    )
                                "
                              />
                            </el-col>
                          </el-row>
                        </el-form-item>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </el-tab-pane>

        <!-- 数据绑定设置 -->
        <el-tab-pane label="数据绑定" name="databinding">
          <div class="databinding-section">
            <!-- ComponentBinding 配置 -->
            <div class="property-section">
              <div class="section-title">绑定配置</div>
              <el-form size="small" label-width="100px">
                <!-- 设备选择 -->
                <el-form-item label="设备">
                  <el-select
                    v-model="formData.deviceId"
                    placeholder="选择设备"
                    style="width: 100%"
                    @change="onDeviceChange"
                  >
                    <el-option
                      v-for="device in deviceList"
                      :key="device.id"
                      :label="device.name"
                      :value="device.id"
                    />
                    <template #empty>
                      <div class="empty-devices">
                        <p>暂无设备</p>
                        <el-button
                          type="primary"
                          size="small"
                          @click="$emit('open-dataset-dialog')"
                        >
                          配置数据源
                        </el-button>
                      </div>
                    </template>
                  </el-select>
                </el-form-item>

                <!-- 属性选择 -->
                <el-form-item label="设备属性" v-if="formData.deviceId">
                  <el-select
                    v-model="formData.paramcode"
                    placeholder="选择设备属性"
                    style="width: 100%"
                    @change="onParamcodeChange"
                  >
                    <el-option
                      v-for="attr in getCurrentDeviceAttributes()"
                      :key="attr.paramcode"
                      :label="`${attr.paramname} (${attr.paramcode})`"
                      :value="attr.paramcode"
                    >
                      <div
                        style="display: flex; justify-content: space-between"
                      >
                        <span>{{ attr.paramname }}</span>
                        <span style="color: #8492a6; font-size: 12px">{{
                          attr.unit || attr.type
                        }}</span>
                      </div>
                    </el-option>
                  </el-select>
                </el-form-item>

                <!-- 目标属性 -->
                <el-form-item label="目标属性" v-if="formData.paramcode">
                  <el-select
                    v-model="formData.targetProperty"
                    placeholder="选择目标属性"
                    style="width: 100%"
                    @change="updateBinding"
                  >
                    <el-option label="文本内容" value="text" />
                    <el-option label="数值" value="value" />
                    <el-option label="背景颜色" value="backgroundColor" />
                    <el-option label="边框颜色" value="borderColor" />
                  </el-select>
                  <div class="data-path-hint">
                    <span
                      >支持JSON路径表达式，如：data.temperature、items[0].name</span
                    >
                  </div>
                </el-form-item>
              </el-form>
            </div>
          </div>
        </el-tab-pane>
      </el-tabs>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleCancel">取消</el-button>
        <el-button type="primary" @click="handleConfirm">确定</el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue";
import { ElMessage } from "element-plus";
import SvgIconStyleConfig from "./SvgIconStyleConfig.vue";
import AppearanceStyleConfig from "./AppearanceStyleConfig.vue";
import {
  Operation,
  Delete,
  Plus,
  Lightning,
  Close,
  Mouse,
  Position,
  TrendCharts,
  Timer,
  Refresh,
  Minus
} from "@element-plus/icons-vue";

// 组件属性
interface Props {
  visible: boolean;
  selectedComponent?: any;
  deviceList?: any[];
  formData?: any;
}

const props = withDefaults(defineProps<Props>(), {
  visible: false,
  selectedComponent: null,
  deviceList: () => [],
  formData: () => ({})
});

// 组件事件
const emit = defineEmits<{
  "update:visible": [visible: boolean];
  "update:formData": [formData: any];
  "update-component-property": [property: string, value: any];
  "open-dataset-dialog": [];
}>();

// 响应式数据
const activeTab = ref("shape");
const previewKey = ref(0);
const originalFormData = ref(null); // 存储对话框打开时的原始数据

// 计算属性
const dialogVisible = computed({
  get: () => props.visible,
  set: value => emit("update:visible", value)
});

// 监听对话框打开状态，保存/恢复原始数据
watch(() => props.visible, (newVisible, oldVisible) => {
  if (newVisible && !oldVisible) {
    // 对话框打开时，保存原始数据的深拷贝
    console.log('对话框打开，保存原始数据');
    if (props.formData) {
      originalFormData.value = JSON.parse(JSON.stringify(props.formData));
      console.log('原始数据已保存:', {
        hasStyle: !!originalFormData.value.style,
        backgroundType: originalFormData.value.style?.backgroundType,
        backgroundColor: originalFormData.value.style?.backgroundColor,
        borderStyle: originalFormData.value.style?.borderStyle,
        styleKeys: originalFormData.value.style ? Object.keys(originalFormData.value.style) : []
      });
    } else {
      console.warn('警告：对话框打开时formData为空！');
    }
  }
}, { immediate: false });

// 监听formData变化，实时更新编辑状态
watch(() => props.formData, (newFormData, oldFormData) => {
  if (props.visible && newFormData) {
    console.log('表单数据变化（对话框打开）:', {
      hasStyle: !!newFormData.style,
      backgroundType: newFormData.style?.backgroundType,
      backgroundColor: newFormData.style?.backgroundColor
    });
  } else if (!props.visible && newFormData !== oldFormData) {
    console.warn('警告：对话框关闭后formData发生变化，这可能导致样式丢失！', {
      hasStyle: !!newFormData?.style,
      backgroundType: newFormData?.style?.backgroundType
    });
  }
}, { deep: true });

// 方法
const updateFormData = (property: string, value: any) => {
  const newFormData = { ...props.formData };
  newFormData[property] = value;
  emit("update:formData", newFormData);
  emit("update-component-property", property, value);
};

const updateStyleData = (property: string, value: any) => {
  // 使用深拷贝确保不会影响原始数据
  const newFormData = JSON.parse(JSON.stringify(props.formData));
  if (!newFormData.style) newFormData.style = {};

  // 更新指定属性
  newFormData.style[property] = value;

  console.log('PropertyEditDialog更新样式属性:', {
    property,
    value,
    完整style对象: newFormData.style,
    包含backgroundType: 'backgroundType' in newFormData.style,
    backgroundType值: newFormData.style.backgroundType
  });

  // 发送完整的formData更新
  emit("update:formData", newFormData);

  // 发送完整的style对象更新，统一由handleUpdateComponentProperty处理
  // 移除重复的DOM操作，避免多次调用applyStyleToElement
  emit("update-component-property", "style", newFormData.style);
};

const onDeviceChange = () => {
  updateBinding();
};

const onParamcodeChange = () => {
  updateBinding();
};

const updateBinding = () => {
  emit("update-component-property", "binding", {
    deviceId: props.formData.deviceId,
    paramcode: props.formData.paramcode,
    targetProperty: props.formData.targetProperty
  });
};

const getCurrentDeviceAttributes = () => {
  const device = props.deviceList?.find(d => d.id === props.formData.deviceId);
  return device ? device.attributes : [];
};

const getDeviceName = () => {
  const device = props.deviceList?.find(d => d.id === props.formData.deviceId);
  return device?.name || "未知设备";
};

const getParamcodeName = () => {
  const device = props.deviceList?.find(d => d.id === props.formData.deviceId);
  const attribute = device?.attributes?.find(
    (attr: any) => attr.paramcode === props.formData.paramcode
  );
  return attribute?.paramname || props.formData.paramcode;
};

const getEventTypeName = (type: string) => {
  const typeNames: Record<string, string> = {
    click: "点击事件",
    dblclick: "双击事件",
    hover: "鼠标悬停",
    leave: "鼠标离开",
    valuechange: "数值变化",
    timer: "定时器",
    custom: "自定义"
  };
  return typeNames[type] || type;
};

const getActionTypeName = (type: string) => {
  const typeNames: Record<string, string> = {
    visibility: "显示/隐藏",
    color: "颜色变化",
    backgroundColorChange: "背景颜色变化",
    svgColorChange: "SVG颜色变化",
    appearanceStyle: "外观样式",
    svgIconStyle: "SVG图标",
    move: "位置移动",
    dialog: "弹出对话框",
    command: "发送命令",
    setValue: "设置数值",
    sound: "播放声音",
    script: "执行脚本"
  };
  return typeNames[type] || type;
};

const toggleEventEnabled = (eventIndex: number, enabled: boolean) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events) newFormData.events = [];
  newFormData.events[eventIndex].enabled = enabled;

  // 如果禁用事件，并且该事件包含背景颜色变化动作，重置背景为透明
  if (!enabled && newFormData.events[eventIndex].actions) {
    const hasBackgroundColorAction = newFormData.events[
      eventIndex
    ].actions.some((action: any) => action.type === "backgroundColorChange");
    const hasAppearanceStyleAction = newFormData.events[
      eventIndex
    ].actions.some((action: any) => action.type === "appearanceStyle");
    const hasSvgIconStyleAction = newFormData.events[eventIndex].actions.some(
      (action: any) => action.type === "svgIconStyle"
    );

    if (hasBackgroundColorAction || hasAppearanceStyleAction) {
      console.log("禁用包含背景颜色变化动作的事件，重置背景为透明");

      // 重置组件背景样式为透明 - 完全清除所有背景相关属性
      if (!newFormData.style) newFormData.style = {};
      newFormData.style.backgroundType = "transparent";
      newFormData.style.backgroundColor = null;
      newFormData.style.gradientStart = null;
      newFormData.style.gradientEnd = null;
      newFormData.style.gradientAngle = null;
      newFormData.style.gradientShape = null;
      newFormData.style.backgroundImage = null;
      newFormData.style.backgroundRepeat = null;
      newFormData.style.backgroundSize = null;
      newFormData.style.backgroundPosition = null;

      // 触发样式更新
      emit("update-component-property", "style", newFormData.style);
    }

    if (hasSvgIconStyleAction) {
      console.log("禁用包含SVG图标样式动作的事件，重置SVG样式为默认");

      // 重置SVG样式为默认
      if (!newFormData.style) newFormData.style = {};
      newFormData.style.fill = "#409eff";
      newFormData.style.fillOpacity = 1;
      newFormData.style.stroke = "none";
      newFormData.style.strokeOpacity = 1;
      newFormData.style.strokeWidth = 1;
      newFormData.style.strokeDasharray = "none";

      // 触发样式更新
      emit("update-component-property", "style", newFormData.style);
    }
  }

  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
};

// 快速添加事件
const addQuickEvent = (eventType: string) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events) newFormData.events = [];

  const newEvent = {
    id: `event_${Date.now()}`,
    type: eventType,
    enabled: true,
    actions: []
  };

  // 根据事件类型设置默认参数
  if (eventType === "timer") {
    newEvent.interval = 1000;
  } else if (eventType === "valuechange") {
    newEvent.condition = "value > 0";
  }

  newFormData.events.push(newEvent);
  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
  ElMessage.success(`已添加${getEventTypeName(eventType)}事件`);
};

const removeEvent = (eventIndex: number) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events) return;

  // 获取要删除的事件
  const eventToRemove = newFormData.events[eventIndex];

  // 检查是否有背景颜色变化动作，如果有则重置背景
  if (eventToRemove && eventToRemove.actions) {
    const hasBackgroundColorAction = eventToRemove.actions.some(
      action => action.type === "backgroundColorChange"
    );
    const hasSvgColorAction = eventToRemove.actions.some(
      action => action.type === "svgColorChange"
    );
    const hasAppearanceStyleAction = eventToRemove.actions.some(
      action => action.type === "appearanceStyle"
    );
    const hasSvgIconStyleAction = eventToRemove.actions.some(
      action => action.type === "svgIconStyle"
    );

    if (hasBackgroundColorAction) {
      console.log("删除包含背景颜色变化动作的事件，重置背景为透明");

      // 重置组件背景样式为透明 - 完全清除所有背景相关属性
      if (!newFormData.style) newFormData.style = {};
      newFormData.style.backgroundType = "transparent";
      newFormData.style.backgroundColor = null; // 设置为null而不是'transparent'，避免保留颜色值
      newFormData.style.gradientStart = null;
      newFormData.style.gradientEnd = null;
      newFormData.style.gradientAngle = null;
      newFormData.style.gradientShape = null;
      // 清除图片背景相关属性
      newFormData.style.backgroundImage = null;
      newFormData.style.backgroundRepeat = null;
      newFormData.style.backgroundSize = null;
      newFormData.style.backgroundPosition = null;

      // 触发样式更新
      emit("update-component-property", "style", newFormData.style);
    }

    if (hasSvgColorAction) {
      console.log("删除包含SVG颜色变化动作的事件，重置SVG颜色为默认");

      // 重置SVG样式为默认
      if (!newFormData.style) newFormData.style = {};
      newFormData.style.fill = "#409eff";
      newFormData.style.fillOpacity = 1;
      newFormData.style.stroke = "none";
      newFormData.style.strokeOpacity = 1;

      // 触发样式更新
      emit("update-component-property", "style", newFormData.style);
    }

    if (hasAppearanceStyleAction) {
      console.log("删除包含外观样式动作的事件，重置样式为默认");

      // 重置外观样式为默认
      if (!newFormData.style) newFormData.style = {};
      newFormData.style.backgroundType = "transparent";
      newFormData.style.backgroundColor = null;
      newFormData.style.gradientStart = null;
      newFormData.style.gradientEnd = null;
      newFormData.style.gradientAngle = null;
      newFormData.style.gradientShape = null;
      newFormData.style.backgroundImage = null;
      newFormData.style.backgroundRepeat = null;
      newFormData.style.backgroundSize = null;
      newFormData.style.backgroundPosition = null;

      // 触发样式更新
      emit("update-component-property", "style", newFormData.style);
    }

    if (hasSvgIconStyleAction) {
      console.log("删除包含SVG图标样式动作的事件，重置SVG样式为默认");

      // 重置SVG样式为默认
      if (!newFormData.style) newFormData.style = {};
      newFormData.style.fill = "#409eff";
      newFormData.style.fillOpacity = 1;
      newFormData.style.stroke = "none";
      newFormData.style.strokeOpacity = 1;
      newFormData.style.strokeWidth = 1;
      newFormData.style.strokeDasharray = "none";

      // 触发样式更新
      emit("update-component-property", "style", newFormData.style);
    }
  }

  // 删除事件
  newFormData.events.splice(eventIndex, 1);
  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
};

const updateEventType = (eventIndex: number, type: string) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events) return;
  newFormData.events[eventIndex].type = type;
  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
};

const updateEventCondition = (eventIndex: number, condition: string) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events) return;
  newFormData.events[eventIndex].condition = condition;
  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
};

const updateEventInterval = (eventIndex: number, interval: number) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events) return;
  newFormData.events[eventIndex].interval = interval;
  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
};

// 改进的添加动作方法，直接指定动作类型
const addEventAction = (eventIndex: number, actionType: string) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events || !newFormData.events[eventIndex].actions) {
    if (!newFormData.events[eventIndex].actions) {
      newFormData.events[eventIndex].actions = [];
    }
  }

  const newAction = {
    id: `action_${Date.now()}`,
    type: actionType,
    delay: 0
  };

  // 根据动作类型设置默认值
  if (actionType === "color") {
    newAction.color = "#409eff";
  } else if (actionType === "backgroundColorChange") {
    newAction.colorType = "solid";
    newAction.backgroundColor = "#409eff";
  } else if (actionType === "svgColorChange") {
    newAction.svgProperty = "fill";
    newAction.svgColor = null;
    newAction.opacity = 1;
  } else if (actionType === "appearanceStyle") {
    newAction.style = {
      backgroundType: "solid",
      backgroundColor: "#409eff"
    };
  } else if (actionType === "svgIconStyle") {
    newAction.style = {
      fillType: "solid",
      fill: "#409eff",
      stroke: "none",
      strokeWidth: 1,
      fillOpacity: 1
    };
  } else if (actionType === "setValue") {
    newAction.value = "";
  } else if (actionType === "dialog") {
    newAction.message = "";
  } else if (actionType === "move") {
    newAction.deltaX = 0;
    newAction.deltaY = 0;
  }

  newFormData.events[eventIndex].actions.push(newAction);
  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
};

const removeEventAction = (eventIndex: number, actionIndex: number) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events || !newFormData.events[eventIndex].actions) return;

  // 获取要删除的动作
  const actionToRemove = newFormData.events[eventIndex].actions[actionIndex];

  // 如果删除的是背景颜色变化动作，重置背景为透明
  if (actionToRemove && actionToRemove.type === "backgroundColorChange") {
    console.log("删除背景颜色变化动作，重置背景为透明");

    // 重置组件背景样式为透明 - 完全清除所有背景相关属性
    if (!newFormData.style) newFormData.style = {};
    newFormData.style.backgroundType = "transparent";
    newFormData.style.backgroundColor = null; // 设置为null而不是'transparent'，避免保留颜色值
    newFormData.style.gradientStart = null;
    newFormData.style.gradientEnd = null;
    newFormData.style.gradientAngle = null;
    newFormData.style.gradientShape = null;
    // 清除图片背景相关属性
    newFormData.style.backgroundImage = null;
    newFormData.style.backgroundRepeat = null;
    newFormData.style.backgroundSize = null;
    newFormData.style.backgroundPosition = null;

    // 触发样式更新
    emit("update-component-property", "style", newFormData.style);
  }

  // 如果删除的是SVG颜色变化动作，重置SVG颜色
  if (actionToRemove && actionToRemove.type === "svgColorChange") {
    console.log("删除SVG颜色变化动作，重置SVG颜色为默认");

    // 重置SVG样式为默认
    if (!newFormData.style) newFormData.style = {};
    newFormData.style.fill = "#409eff"; // 默认填充色
    newFormData.style.fillOpacity = 1;
    newFormData.style.stroke = "none"; // 默认无描边
    newFormData.style.strokeOpacity = 1;

    // 触发样式更新
    emit("update-component-property", "style", newFormData.style);
  }

  // 如果删除的是SVG图标样式动作，重置SVG样式
  if (actionToRemove && actionToRemove.type === "svgIconStyle") {
    console.log("删除SVG图标样式动作，重置SVG样式为默认");

    // 重置SVG样式为默认
    if (!newFormData.style) newFormData.style = {};
    newFormData.style.fill = "#409eff"; // 默认填充色
    newFormData.style.fillOpacity = 1;
    newFormData.style.stroke = "none"; // 默认无描边
    newFormData.style.strokeOpacity = 1;
    newFormData.style.strokeWidth = 1;
    newFormData.style.strokeDasharray = "none";

    // 触发样式更新
    emit("update-component-property", "style", newFormData.style);
  }

  // 删除动作
  newFormData.events[eventIndex].actions.splice(actionIndex, 1);
  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
};

// 新增的动作更新方法
const updateActionValue = (
  eventIndex: number,
  actionIndex: number,
  value: any
) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events || !newFormData.events[eventIndex].actions) return;
  newFormData.events[eventIndex].actions[actionIndex].value = value;
  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
};

const updateActionColor = (
  eventIndex: number,
  actionIndex: number,
  color: string
) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events || !newFormData.events[eventIndex].actions) return;
  newFormData.events[eventIndex].actions[actionIndex].color = color;
  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
};

const updateActionMessage = (
  eventIndex: number,
  actionIndex: number,
  message: string
) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events || !newFormData.events[eventIndex].actions) return;
  newFormData.events[eventIndex].actions[actionIndex].message = message;
  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
};

// 新增的通用动作属性更新方法
const updateActionProperty = (
  eventIndex: number,
  actionIndex: number,
  property: string,
  value: any
) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events || !newFormData.events[eventIndex].actions) return;
  newFormData.events[eventIndex].actions[actionIndex][property] = value;
  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
};

// 新增的动作样式属性更新方法
const updateActionStyleProperty = (
  eventIndex: number,
  actionIndex: number,
  property: string,
  value: any
) => {
  const newFormData = { ...props.formData };
  if (!newFormData.events || !newFormData.events[eventIndex].actions) return;
  if (!newFormData.events[eventIndex].actions[actionIndex].style) {
    newFormData.events[eventIndex].actions[actionIndex].style = {};
  }
  newFormData.events[eventIndex].actions[actionIndex].style[property] = value;
  emit("update:formData", newFormData);
  emit("update-component-property", "events", newFormData.events);
};

// 动画预览相关方法
const getAnimationClass = () => {
  const animationType = props.formData?.style?.svgAnimation;
  return animationType && animationType !== "none"
    ? `svg-animation-${animationType}`
    : "";
};

const getAnimationStyle = () => {
  const style = props.formData?.style || {};
  if (!style.svgAnimation || style.svgAnimation === "none") return {};

  return {
    animationDuration: style.animationDuration || "2s",
    animationIterationCount: style.animationIterationCount || "infinite",
    animationTimingFunction: style.animationTimingFunction || "ease",
    animationDelay: style.animationDelay || "0s",
    animationPlayState: style.animationPlayStateOnHover ? "paused" : "running"
  };
};

const restartPreview = () => {
  previewKey.value++;
};

// 获取SVG滤镜样式
const getSvgFilterStyle = () => {
  const style = props.formData?.style || {};
  const filters = [];

  // 投影滤镜
  if (style.enableDropShadow) {
    filters.push("url(#drop-shadow)");
  }

  // 模糊滤镜
  if (style.svgBlur && style.svgBlur > 0) {
    filters.push("url(#blur)");
  }

  return filters.length > 0 ? filters.join(" ") : null;
};

const handleCancel = () => {
  // 取消时恢复原始数据，避免保存未确认的更改
  console.log('取消编辑，恢复原始数据');

  // 使用保存的原始数据恢复表单状态
  if (originalFormData.value) {
    const restoredData = JSON.parse(JSON.stringify(originalFormData.value));

    console.log('取消时恢复的原始数据:', {
      hasStyle: !!restoredData.style,
      style: restoredData.style,
      backgroundType: restoredData.style?.backgroundType,
      backgroundColor: restoredData.style?.backgroundColor
    });

    emit("update:formData", restoredData);

    // 同时恢复组件的关键属性，确保界面状态一致
    if (restoredData.style) {
      console.log('恢复样式数据到组件:', restoredData.style);
      emit("update-component-property", "style", restoredData.style);
    }
  }

  dialogVisible.value = false;
  ElMessage.info("已取消编辑");
};

const handleConfirm = () => {
  // 确认保存时，确保数据被正确同步
  console.log('PropertyEditDialog确认保存，同步表单数据到组件');

  if (props.selectedComponent && props.formData) {
    // 创建最终的表单数据深拷贝
    const finalFormData = JSON.parse(JSON.stringify(props.formData));

    console.log('PropertyEditDialog最终保存的表单数据:', {
      hasStyle: !!finalFormData.style,
      style: finalFormData.style,
      backgroundType: finalFormData.style?.backgroundType,
      backgroundColor: finalFormData.style?.backgroundColor,
      borderStyle: finalFormData.style?.borderStyle,
      borderColor: finalFormData.style?.borderColor,
      borderWidth: finalFormData.style?.borderWidth,
      // 🎯 添加动画相关属性的日志
      svgAnimation: finalFormData.style?.svgAnimation,
      animationSpeed: finalFormData.style?.animationSpeed,
      animationDuration: finalFormData.style?.animationDuration,
      animationIterationCount: finalFormData.style?.animationIterationCount,
      animationStaticValue: finalFormData.style?.animationStaticValue
    });

    // 发送最终的表单数据更新事件
    emit("update:formData", finalFormData);

    // 批量更新所有属性，避免多次调用导致重复渲染
    const updateBatch = [];

    // 样式属性
    if (finalFormData.style) {
      console.log('准备更新样式，完整样式对象:', finalFormData.style);
      updateBatch.push({ property: 'style', value: finalFormData.style });
    } else {
      console.warn('警告：finalFormData中没有style对象！');
    }

    // 事件配置
    if (finalFormData.events) {
      console.log('准备更新事件数据:', finalFormData.events.length, '个事件');
      updateBatch.push({ property: 'events', value: finalFormData.events });
    }

    // 其他重要属性 - 只更新真正改变的属性
    const importantProps = ['position', 'size', 'name', 'type', 'deviceId', 'paramcode', 'targetProperty'];
    importantProps.forEach(prop => {
      if (finalFormData[prop] !== undefined) {
        updateBatch.push({ property: prop, value: finalFormData[prop] });
      }
    });

    // 统一发送所有更新事件 - 注意：每个emit都会触发handleUpdateComponentProperty
    // 但由于我们已经优化了handleUpdateComponentProperty，不会导致重复DOM操作
    console.log(`准备批量更新${updateBatch.length}个属性`);
    updateBatch.forEach(({ property, value }) => {
      emit("update-component-property", property, value);
    });

    // 更新原始数据，下次打开时使用最新的数据
    originalFormData.value = finalFormData;
    console.log('PropertyEditDialog原始数据已更新，下次打开将使用此配置');
  }

  dialogVisible.value = false;
  ElMessage.success("属性配置已保存");
};

// 最小化对话框（暂时隐藏）
const minimizeDialog = () => {
  dialogVisible.value = false;
  ElMessage.info("对话框已最小化");
};

// 监听选中组件变化，更新activeTab
watch(
  () => props.selectedComponent,
  newComponent => {
    if (newComponent) {
      activeTab.value = "shape";
    }
  }
);
</script>

<style scoped lang="scss">
// 对话框整体样式
.property-edit-dialog {
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
      padding: 0;
      margin: 0;
    }
  }
}

// 自定义头部样式
.custom-dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  position: relative;
  overflow: hidden;

  // 添加动态背景效果
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

  // 添加光效
  &::after {
    content: "";
    position: absolute;
    top: -50%;
    left: -50%;
    width: 200%;
    height: 200%;
    background: radial-gradient(
      circle,
      rgba(255, 255, 255, 0.1) 0%,
      transparent 70%
    );
    animation: headerGlow 4s ease-in-out infinite;
    pointer-events: none;
  }

  .header-left {
    display: flex;
    align-items: center;
    gap: 16px;
    position: relative;
    z-index: 2;

    .header-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 48px;
      height: 48px;
      background: rgba(255, 255, 255, 0.15);
      border-radius: 12px;
      backdrop-filter: blur(10px);
      border: 1px solid rgba(255, 255, 255, 0.2);
      color: white;
      transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);

      &:hover {
        transform: translateY(-2px) rotate(15deg);
        box-shadow: 0 8px 25px rgba(255, 255, 255, 0.3);
        background: rgba(255, 255, 255, 0.2);
      }

      svg {
        width: 24px;
        height: 24px;
        filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.2));
        animation: iconPulse 3s ease-in-out infinite;
      }
    }

    .header-content {
      .header-title {
        margin: 0;
        font-size: 20px;
        font-weight: 700;
        color: white;
        letter-spacing: 0.5px;
        text-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
        background: linear-gradient(135deg, #ffffff 0%, #f0f0f0 100%);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
      }

      .header-subtitle {
        margin: 4px 0 0 0;
        font-size: 13px;
        color: rgba(255, 255, 255, 0.8);
        font-weight: 500;
        letter-spacing: 0.3px;
        text-shadow: 0 1px 4px rgba(0, 0, 0, 0.3);
        opacity: 0.9;
      }
    }
  }

  .header-right {
    position: relative;
    z-index: 2;

    .header-actions {
      display: flex;
      gap: 8px;

      .action-btn {
        width: 36px;
        height: 36px;
        border-radius: 10px;
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
          box-shadow: 0 6px 20px rgba(0, 0, 0, 0.3);
        }

        &.close-btn:hover {
          background: rgba(255, 107, 107, 0.8);
          border-color: rgba(255, 107, 107, 0.6);
        }

        .el-icon {
          font-size: 16px;
        }
      }
    }
  }
}

// 动画关键帧
@keyframes headerGlow {
  0%,
  100% {
    opacity: 0.3;
    transform: scale(1) rotate(0deg);
  }
  50% {
    opacity: 0.6;
    transform: scale(1.1) rotate(180deg);
  }
}

@keyframes iconPulse {
  0%,
  100% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.05);
    opacity: 0.9;
  }
}

.property-dialog-content {
  height: 70vh;
  overflow: hidden;

  :deep(.el-tabs--border-card) {
    height: 100%;
    border: none;
    box-shadow: none;

    .el-tabs__header {
      background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
      border-bottom: 2px solid #dee2e6;
      margin: 0;

      .el-tabs__nav-wrap {
        &::after {
          display: none;
        }
      }

      .el-tabs__item {
        border-left: 1px solid #dee2e6;
        border-top: 1px solid #dee2e6;
        border-right: none;
        border-bottom: none;
        background: #f8f9fa;
        color: #6c757d;
        font-weight: 500;
        padding: 0 24px;
        height: 48px;
        line-height: 48px;
        font-size: 14px;

        &:first-child {
          border-left: none;
        }

        &.is-active {
          background: #ffffff;
          color: #495057;
          border-bottom: 2px solid #667eea;
          font-weight: 600;
        }

        &:hover:not(.is-active) {
          background: #e9ecef;
          color: #495057;
        }
      }
    }

    .el-tabs__content {
      height: calc(100% - 48px);
      overflow-y: auto;
      padding: 20px;
      background: #ffffff;

      .el-tab-pane {
        height: 100%;
      }
    }
  }
}

.property-section {
  margin-bottom: 24px;
  padding: 16px;
  background: #ffffff;
  border: 1px solid #e9ecef;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  transition: all 0.3s ease;

  &:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    border-color: #ced4da;
  }

  &:last-child {
    margin-bottom: 0;
  }

  .section-title {
    font-size: 14px;
    font-weight: 600;
    color: #495057;
    margin-bottom: 16px;
    padding: 8px 12px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border-radius: 6px;
    display: flex;
    align-items: center;

    &::before {
      content: "";
      width: 4px;
      height: 16px;
      background: rgba(255, 255, 255, 0.8);
      border-radius: 2px;
      margin-right: 8px;
    }
  }

  :deep(.el-form-item) {
    margin-bottom: 16px;

    .el-form-item__label {
      font-size: 13px;
      color: #6c757d;
      font-weight: 500;
      line-height: 1.4;
    }

    .el-input,
    .el-select {
      .el-input__wrapper {
        border-radius: 6px;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
        transition: all 0.3s ease;

        &:hover {
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
        }

        &.is-focus {
          box-shadow: 0 0 0 2px rgba(102, 126, 234, 0.25);
        }
      }
    }
  }
}

.event-item,
.action-item {
  margin-bottom: 16px;
  padding: 12px;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  background: #fafafa;
}

.event-header,
.action-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;

  .event-info,
  .action-info {
    display: flex;
    align-items: center;
    gap: 8px;

    .event-icon,
    .action-icon {
      color: #409eff;
    }
  }

  .event-controls {
    display: flex;
    align-items: center;
    gap: 8px;
  }
}

.actions-section {
  margin-top: 12px;

  .section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 8px;
    font-weight: 600;
    color: #606266;
  }
}

.add-event-section {
  margin-top: 16px;
}

.empty-devices {
  text-align: center;
  padding: 16px;

  p {
    margin-bottom: 8px;
    color: #909399;
  }
}

.data-path-hint {
  margin-top: 4px;

  span {
    font-size: 12px;
    color: #909399;
  }
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 20px;

  .el-button {
    min-width: 80px;
    height: 36px;
    border-radius: 6px;
    font-weight: 500;

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
  }
}

// 动画预览相关样式
.animation-preview {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;

  .preview-container {
    padding: 20px;
    background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
    border-radius: 12px;
    border: 2px dashed #dee2e6;
    display: flex;
    justify-content: center;
    align-items: center;

    svg {
      transition: all 0.3s ease;

      &:hover {
        transform: scale(1.1);
      }
    }
  }

  .preview-controls {
    display: flex;
    gap: 8px;
  }
}

// SVG动画keyframes
@keyframes svg-animation-rotate {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

@keyframes svg-animation-pulse {
  0%,
  100% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.1);
    opacity: 0.8;
  }
}

@keyframes svg-animation-blink {
  0%,
  50%,
  100% {
    opacity: 1;
  }
  25%,
  75% {
    opacity: 0;
  }
}

@keyframes svg-animation-bounce {
  0%,
  20%,
  53%,
  80%,
  100% {
    transform: translateY(0);
  }
  40%,
  43% {
    transform: translateY(-15px);
  }
  70% {
    transform: translateY(-7px);
  }
  90% {
    transform: translateY(-3px);
  }
}

@keyframes svg-animation-shake {
  0%,
  100% {
    transform: translateX(0);
  }
  10%,
  30%,
  50%,
  70%,
  90% {
    transform: translateX(-3px);
  }
  20%,
  40%,
  60%,
  80% {
    transform: translateX(3px);
  }
}

@keyframes svg-animation-scale {
  0%,
  100% {
    transform: scale(1);
  }
  50% {
    transform: scale(1.2);
  }
}

@keyframes svg-animation-moveX {
  0%,
  100% {
    transform: translateX(0);
  }
  50% {
    transform: translateX(10px);
  }
}

@keyframes svg-animation-moveY {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-10px);
  }
}

@keyframes svg-animation-fade {
  0%,
  100% {
    opacity: 1;
  }
  50% {
    opacity: 0.3;
  }
}

// SVG动画类
.svg-animation-rotate {
  animation-name: svg-animation-rotate;
}
.svg-animation-pulse {
  animation-name: svg-animation-pulse;
}
.svg-animation-blink {
  animation-name: svg-animation-blink;
}
.svg-animation-bounce {
  animation-name: svg-animation-bounce;
}
.svg-animation-shake {
  animation-name: svg-animation-shake;
}
.svg-animation-scale {
  animation-name: svg-animation-scale;
}
.svg-animation-moveX {
  animation-name: svg-animation-moveX;
}
.svg-animation-moveY {
  animation-name: svg-animation-moveY;
}
.svg-animation-fade {
  animation-name: svg-animation-fade;
}

// 动作配置样式
.color-change-config,
.svg-color-change-config {
  padding: 12px;
  background: linear-gradient(135deg, #f8fafc 0%, #edf2f7 100%);
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  margin-top: 8px;

  .el-form-item {
    margin-bottom: 12px;

    &:last-child {
      margin-bottom: 0;
    }
  }

  .unit {
    margin-left: 8px;
    font-size: 12px;
    color: #64748b;
    font-weight: 500;
  }
}

.action-config {
  .el-form-item {
    margin-bottom: 12px;

    .el-form-item__label {
      font-size: 12px;
      color: #475569;
      font-weight: 500;
    }

    .el-input-number {
      width: 100%;
    }

    .el-color-picker {
      width: 100%;
    }
  }
}

.action-item {
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  background: linear-gradient(135deg, #ffffff 0%, #f8fafc 100%);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  transition: all 0.3s ease;

  &:hover {
    border-color: #cbd5e1;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  }

  .action-header {
    padding: 12px;
    border-bottom: 1px solid #f1f5f9;
    display: flex;
    align-items: center;
    justify-content: space-between;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border-radius: 7px 7px 0 0;

    .action-icon {
      color: white;
      margin-right: 8px;
    }

    span {
      font-weight: 600;
      font-size: 13px;
    }

    .el-button {
      color: rgba(255, 255, 255, 0.8);

      &:hover {
        color: white;
        background: rgba(255, 255, 255, 0.1);
      }
    }
  }

  .action-config {
    padding: 16px 12px;
  }
}
</style>
