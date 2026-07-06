<template>
  <div class="svg-icon-style-config">
    <!-- SVG动画效果 -->
    <div class="property-section">
      <div class="section-title">动画效果</div>
      <el-form size="small" label-width="100px">
        <el-form-item label="动画类型">
          <el-select
            :model-value="selectedComponent?.style?.svgAnimation || 'none'"
            @change="val => updateStyle('svgAnimation', val)"
          >
            <el-option
              v-for="option in animationOptions"
              :key="option.value"
              :label="option.label"
              :value="option.value"
            />
          </el-select>
        </el-form-item>
        <el-row
          v-if="selectedComponent?.style?.svgAnimation !== 'none'"
          :gutter="16"
        >
          <el-col :span="12">
            <el-form-item label="动画时长">
              <el-select
                :model-value="
                  selectedComponent?.style?.animationDuration || '2s'
                "
                @change="val => updateStyle('animationDuration', val)"
              >
                <el-option label="0.5秒" value="0.5s" />
                <el-option label="1秒" value="1s" />
                <el-option label="2秒" value="2s" />
                <el-option label="3秒" value="3s" />
                <el-option label="5秒" value="5s" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="循环方式">
              <el-select
                :model-value="
                  selectedComponent?.style?.animationIterationCount ||
                  'infinite'
                "
                @change="val => updateStyle('animationIterationCount', val)"
              >
                <el-option label="无限循环" value="infinite" />
                <el-option label="1次" value="1" />
                <el-option label="2次" value="2" />
                <el-option label="3次" value="3" />
                <el-option label="5次" value="5" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <!-- 目标值参数 - 适用于所有情况，始终显示 -->
        <el-row :gutter="16">
          <el-col :span="24">
            <el-form-item label="目标值(0-100)">
              <el-slider
                :model-value="animationStaticValue"
                :min="0"
                :max="100"
                :step="1"
                show-input
                @input="val => updateStyle('animationStaticValue', val)"
              />
              <span style="margin-left: 8px; color: #909399; font-size: 12px">
                %
              </span>
            </el-form-item>
          </el-col>
        </el-row>
        <!-- 管道流动方向控制 - 仅在选择pipeFlow动画时显示 -->
        <el-row
          v-if="selectedComponent?.style?.svgAnimation === 'pipeFlow'"
          :gutter="16"
        >
          <el-col :span="12">
            <el-form-item label="流动方向">
              <el-select
                :model-value="
                  selectedComponent?.style?.pipeFlowDirection || 'forward'
                "
                @change="val => updateStyle('pipeFlowDirection', val)"
              >
                <el-option label="正向流动" value="forward" />
                <el-option label="反向流动" value="backward" />
                <el-option label="双向流动" value="bidirectional" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <!-- 开关状态控制 - 仅在选择switchToggle动画时显示 -->
        <el-row
          v-if="selectedComponent?.style?.svgAnimation === 'switchToggle'"
          :gutter="16"
        >
          <el-col :span="12">
            <el-form-item label="开关状态">
              <el-switch
                :model-value="
                  selectedComponent?.style?.switchState === 'on'
                "
                active-text="开启"
                inactive-text="关闭"
                @change="val => updateStyle('switchState', val ? 'on' : 'off')"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态颜色">
              <el-color-picker
                :model-value="
                  selectedComponent?.style?.switchState === 'on'
                    ? (selectedComponent?.style?.switchOnColor || '#67c23a')
                    : (selectedComponent?.style?.switchOffColor || '#909399')
                "
                show-alpha
                @change="val => updateSwitchStateColor(val)"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row
          v-if="selectedComponent?.style?.svgAnimation !== 'none'"
          :gutter="16"
        >
          <el-col :span="12">
            <el-form-item label="动画曲线">
              <el-select
                :model-value="
                  selectedComponent?.style?.animationTimingFunction || 'ease'
                "
                @change="val => updateStyle('animationTimingFunction', val)"
              >
                <el-option label="缓动" value="ease" />
                <el-option label="线性" value="linear" />
                <el-option label="缓入" value="ease-in" />
                <el-option label="缓出" value="ease-out" />
                <el-option label="缓入缓出" value="ease-in-out" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="延迟时间">
              <el-input-number
                :model-value="
                  parseFloat(selectedComponent?.style?.animationDelay || '0')
                "
                :min="0"
                :max="10"
                :step="0.1"
                @input="val => updateStyle('animationDelay', val + 's')"
              />
              <span style="margin-left: 8px">秒</span>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item
          v-if="selectedComponent?.style?.svgAnimation !== 'none'"
          label="悬停暂停"
        >
          <el-switch
            :model-value="
              selectedComponent?.style?.animationPlayStateOnHover || false
            "
            @change="val => updateStyle('animationPlayStateOnHover', val)"
          />
        </el-form-item>
      </el-form>
    </div>

    <!-- 描边样式 -->
    <div class="property-section">
      <div class="section-title">描边样式</div>
      <el-form size="small" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="描边颜色">
              <el-color-picker
                :model-value="selectedComponent?.style?.stroke || '#000000'"
                show-alpha
                @change="val => updateStyle('stroke', val)"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="描边宽度">
              <el-input-number
                :model-value="selectedComponent?.style?.strokeWidth || 1"
                :min="0"
                :max="20"
                :step="0.5"
                controls-position="right"
                @input="val => updateStyle('strokeWidth', val)"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="描边样式">
          <el-select
            :model-value="selectedComponent?.style?.strokeDasharray || 'none'"
            @change="val => updateStyle('strokeDasharray', val)"
          >
            <el-option label="实线" value="none" />
            <el-option label="虚线" value="5,5" />
            <el-option label="点线" value="2,2" />
            <el-option label="点划线" value="5,5,2,5" />
          </el-select>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="线帽样式">
              <el-select
                :model-value="
                  selectedComponent?.style?.strokeLinecap || 'round'
                "
                @change="val => updateStyle('strokeLinecap', val)"
              >
                <el-option label="圆头" value="round" />
                <el-option label="方头" value="square" />
                <el-option label="平头" value="butt" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="连接样式">
              <el-select
                :model-value="
                  selectedComponent?.style?.strokeLinejoin || 'round'
                "
                @change="val => updateStyle('strokeLinejoin', val)"
              >
                <el-option label="圆角" value="round" />
                <el-option label="尖角" value="miter" />
                <el-option label="斜角" value="bevel" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="描边透明度">
          <el-slider
            :model-value="selectedComponent?.style?.strokeOpacity || 1"
            :min="0"
            :max="1"
            :step="0.01"
            :precision="2"
            show-input
            @input="val => updateStyle('strokeOpacity', val)"
          />
        </el-form-item>
      </el-form>
    </div>

    <!-- 滤镜效果 -->
    <div class="property-section">
      <div class="section-title">滤镜效果</div>
      <el-form size="small" label-width="100px">
        <el-form-item label="启用投影">
          <el-switch
            :model-value="selectedComponent?.style?.enableDropShadow || false"
            @change="val => updateStyle('enableDropShadow', val)"
          />
        </el-form-item>
        <template v-if="selectedComponent?.style?.enableDropShadow">
          <el-form-item label="投影颜色">
            <el-color-picker
              :model-value="
                selectedComponent?.style?.dropShadowColor || 'rgba(0,0,0,0.3)'
              "
              show-alpha
              @change="val => updateStyle('dropShadowColor', val)"
            />
          </el-form-item>
          <el-row :gutter="16">
            <el-col :span="8">
              <el-form-item label="偏移X">
                <el-input-number
                  :model-value="
                    selectedComponent?.style?.dropShadowOffsetX || 2
                  "
                  :step="1"
                  controls-position="right"
                  @input="val => updateStyle('dropShadowOffsetX', val)"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="偏移Y">
                <el-input-number
                  :model-value="
                    selectedComponent?.style?.dropShadowOffsetY || 2
                  "
                  :step="1"
                  controls-position="right"
                  @input="val => updateStyle('dropShadowOffsetY', val)"
                />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="模糊半径">
                <el-input-number
                  :model-value="selectedComponent?.style?.dropShadowBlur || 4"
                  :min="0"
                  :max="20"
                  controls-position="right"
                  @input="val => updateStyle('dropShadowBlur', val)"
                />
              </el-form-item>
            </el-col>
          </el-row>
        </template>
        <el-form-item label="SVG模糊">
          <div class="flex w-[100%]">
            <el-slider
              :model-value="selectedComponent?.style?.svgBlur || 0"
              :min="0"
              :max="10"
              :step="0.1"
              show-input
              @input="val => updateStyle('svgBlur', val)"
            />
            <span style="margin-left: 8px">px</span>
          </div>
        </el-form-item>
      </el-form>
    </div>

    <!-- SVG图标效果 -->
    <div class="property-section">
      <div class="section-title">图标效果</div>
      <el-form size="small" label-width="100px">
        <el-form-item label="启用图标效果">
          <el-switch
            :model-value="selectedComponent?.style?.svgStyleEnabled ?? false"
            active-text="启用"
            inactive-text="禁用"
            @change="val => updateStyle('svgStyleEnabled', val)"
          />
        </el-form-item>

        <template v-if="selectedComponent?.style?.svgStyleEnabled">
          <el-form-item label="填充类型">
            <el-select
              :model-value="selectedComponent?.style?.fillType || 'solid'"
              @change="handleFillTypeChange"
            >
              <el-option label="纯色" value="solid" />
              <el-option label="透明" value="transparent" />
              <el-option label="线性渐变" value="linear-gradient" />
              <el-option label="径向渐变" value="radial-gradient" />
            </el-select>
          </el-form-item>

          <!-- 纯色填充 -->
          <el-form-item
            v-if="(selectedComponent?.style?.fillType || 'solid') === 'solid'"
            label="填充颜色"
          >
            <el-color-picker
              :model-value="
                selectedComponent?.style?.fill ||
                selectedComponent?.style?.svgColor ||
                (selectedComponent?.style?.fillType === 'solid'
                  ? '#409eff'
                  : null)
              "
              show-alpha
              @change="handleFillColorChange"
            />
          </el-form-item>

          <!-- 线性渐变 -->
          <div v-if="selectedComponent?.style?.fillType === 'linear-gradient'">
            <el-form-item label="起始色">
              <el-color-picker
                :model-value="
                  selectedComponent?.style?.fillGradientStart || '#409eff'
                "
                show-alpha
                @change="updateStyle('fillGradientStart', $event)"
              />
            </el-form-item>
            <el-form-item label="结束色">
              <el-color-picker
                :model-value="
                  selectedComponent?.style?.fillGradientEnd || '#67c23a'
                "
                show-alpha
                @change="updateStyle('fillGradientEnd', $event)"
              />
            </el-form-item>
            <el-form-item label="渐变角度">
              <el-slider
                :model-value="selectedComponent?.style?.fillGradientAngle || 0"
                :min="0"
                :max="360"
                :step="1"
                show-input
                @input="updateStyle('fillGradientAngle', $event)"
              />
              <span class="unit">°</span>
            </el-form-item>
          </div>

          <!-- 径向渐变 -->
          <div v-if="selectedComponent?.style?.fillType === 'radial-gradient'">
            <el-form-item label="中心色">
              <el-color-picker
                :model-value="
                  selectedComponent?.style?.fillGradientStart || '#409eff'
                "
                show-alpha
                @change="updateStyle('fillGradientStart', $event)"
              />
            </el-form-item>
            <el-form-item label="边缘色">
              <el-color-picker
                :model-value="
                  selectedComponent?.style?.fillGradientEnd || '#67c23a'
                "
                show-alpha
                @change="updateStyle('fillGradientEnd', $event)"
              />
            </el-form-item>
            <el-form-item label="形状">
              <el-select
                :model-value="
                  selectedComponent?.style?.fillGradientShape || 'circle'
                "
                @change="updateStyle('fillGradientShape', $event)"
              >
                <el-option label="圆形" value="circle" />
                <el-option label="椭圆" value="ellipse" />
              </el-select>
            </el-form-item>
          </div>

          <!-- 填充透明度 -->
          <el-form-item label="填充透明度">
            <el-slider
              :model-value="selectedComponent?.style?.fillOpacity || 1"
              :min="0"
              :max="1"
              :step="0.01"
              :precision="2"
              show-input
              @input="updateStyle('fillOpacity', $event)"
            />
          </el-form-item>
        </template>
      </el-form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { watch, nextTick, computed } from "vue";
import { animationOptions, getAnimationTypeName } from "../core/AnimationTypes";
import { svgManager } from "../core/SvgManager";
import { SvgTypeDetector } from "../core/SvgTypeDetector";

interface Props {
  selectedComponent?: any;
}

interface Emits {
  (e: "update-style", property: string, value: any): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 🎯 计算属性：获取目标值，确保响应式更新
const animationStaticValue = computed(() => {
  return props.selectedComponent?.style?.animationStaticValue !== undefined
    ? props.selectedComponent.style.animationStaticValue
    : 30;
});

// 记录SVG初始状态
let originalSvgState: Map<string, any> = new Map();

// 记录SVG初始状态
const recordOriginalSvgState = () => {
  if (!props.selectedComponent?.id) return;

  const element = document.getElementById(props.selectedComponent.id);
  if (!element) return;

  const svgElement = element.querySelector("svg");
  if (!svgElement) return;

  const fillableElements = svgElement.querySelectorAll(
    "path, circle, rect, ellipse, polygon, polyline"
  );

  originalSvgState.clear();
  fillableElements.forEach((el, index) => {
    const svgEl = el as SVGElement;
    const elementId = `${props.selectedComponent.id}_${index}`;

    // 记录原始属性（从SVG文件中的属性，不是style）
    const originalState = {
      fill: svgEl.getAttribute("fill") || svgEl.style.fill || "currentColor",
      fillOpacity:
        svgEl.getAttribute("fill-opacity") || svgEl.style.fillOpacity || "1",
      stroke: svgEl.getAttribute("stroke") || svgEl.style.stroke || "none",
      strokeOpacity:
        svgEl.getAttribute("stroke-opacity") ||
        svgEl.style.strokeOpacity ||
        "1",
      strokeWidth:
        svgEl.getAttribute("stroke-width") || svgEl.style.strokeWidth || "1"
    };

    originalSvgState.set(elementId, originalState);
  });
};

// 恢复SVG原始状态
const restoreOriginalSvgState = () => {
  if (!props.selectedComponent?.id) return;

  // 🌊 如果启用了管道流动动画，不恢复原始状态
  // 因为管道流动需要保持 stroke-dasharray 和动画属性
  if (props.selectedComponent?.style?.svgAnimation === 'pipeFlow') {
    console.log("🌊 管道流动动画已启用，跳过恢复原始状态");
    return;
  }

  // 🔘 如果启用了开关切换动画，不恢复原始状态
  // 因为开关切换需要保持 transition 过渡属性
  if (props.selectedComponent?.style?.svgAnimation === 'switchToggle') {
    console.log("🔘 开关切换动画已启用，跳过恢复原始状态");
    return;
  }

  // 🎯 如果启用了进度条滑动动画，不恢复原始状态
  // 因为进度条动画需要通过JavaScript动态修改SVG元素
  if (props.selectedComponent?.style?.svgAnimation === 'progressSlide') {
    console.log("🎯 进度条滑动动画已启用，跳过恢复原始状态");
    return;
  }

  // 🌊 如果启用了液体动画，不恢复原始状态
  // 因为液体动画需要通过JavaScript动态修改SVG元素
  if (props.selectedComponent?.style?.svgAnimation === 'liquidFill' ||
      props.selectedComponent?.style?.svgAnimation === 'liquidDrain') {
    console.log("🌊 液体动画已启用，跳过恢复原始状态");
    return;
  }

  const element = document.getElementById(props.selectedComponent.id);
  if (!element) return;

  const svgElement = element.querySelector("svg");
  if (!svgElement) return;

  const fillableElements = svgElement.querySelectorAll(
    "path, circle, rect, ellipse, polygon, polyline"
  );

  console.log("恢复SVG原始状态");

  fillableElements.forEach((el, index) => {
    const svgEl = el as SVGElement;
    const elementId = `${props.selectedComponent.id}_${index}`;
    const originalState = originalSvgState.get(elementId);

    if (originalState) {
      // 清除style属性，恢复原始属性
      svgEl.style.fill = "";
      svgEl.style.fillOpacity = "";
      svgEl.style.stroke = "";
      svgEl.style.strokeOpacity = "";
      svgEl.style.strokeWidth = "";

      // 如果原始状态没有这些属性，就完全清除
      console.log(`恢复元素 ${index} 到原始状态:`, originalState);
    }
  });

  // 清除渐变定义
  const defs = svgElement.querySelector("defs");
  if (defs) {
    const gradients = defs.querySelectorAll('[id^="gradient_"]');
    gradients.forEach(gradient => gradient.remove());
    if (defs.children.length === 0) {
      defs.remove();
    }
  }
};

// 初始化SVG样式配置
const initializeSvgVisibility = () => {
  if (!props.selectedComponent?.id) return;

  const component = props.selectedComponent;

  // 先记录原始状态
  recordOriginalSvgState();

  console.log('初始化SVG样式配置:', {
    id: component.id,
    svgStyleEnabled: component.style?.svgStyleEnabled,
    fillType: component.style?.fillType,
    fill: component.style?.fill,
    fillOpacity: component.style?.fillOpacity
  });

  // 如果是第一次编辑组件（没有设置过svgStyleEnabled），则默认为禁用状态
  if (component.style?.svgStyleEnabled === undefined) {
    console.log("首次编辑SVG组件，默认禁用样式效果，保持原始颜色");
    // 不emit事件，避免覆盖formData，只在本地处理
    return;
  }

  // 如果SVG样式效果未启用，恢复原始样式
  // 但如果是开关切换动画或管道流动动画，不恢复（它们不依赖svgStyleEnabled）
  if (!component.style?.svgStyleEnabled) {
    // 检查是否有特殊动画
    const hasSpecialAnimation = component.style?.svgAnimation === 'switchToggle' ||
                                component.style?.svgAnimation === 'pipeFlow';
    if (!hasSpecialAnimation) {
      console.log("SVG样式效果已禁用，恢复原始样式");
      restoreOriginalSvgState();
    } else {
      console.log(`特殊动画 ${component.style.svgAnimation} 已启用，保持当前状态`);
    }
    return;
  }

  // 只有在明确启用时才应用样式
  if (component.style?.svgStyleEnabled && component.style?.fillType) {
    console.log("SVG样式已启用，应用现有样式:", {
      fillType: component.style.fillType,
      fill: component.style.fill,
      fillGradientStart: component.style.fillGradientStart,
      fillGradientEnd: component.style.fillGradientEnd
    });
    // 延迟应用样式，确保DOM已经渲染
    nextTick(() => {
      applySvgStyleToDom("fillType", component.style.fillType);
    });
  }
};

// 清除SVG自定义样式，恢复原始状态
const clearSvgCustomStyles = () => {
  console.log("清除SVG自定义样式，恢复原始状态");
  // 直接调用恢复原始状态的方法
  restoreOriginalSvgState();
};

const updateStyle = (property: string, value: any) => {
  emit("update-style", property, value);
  // 如果是svgStyleEnabled属性变化，不在这里处理，让watcher处理
  if (property === "svgStyleEnabled") {
    // 只发送事件，让watcher处理DOM操作
    return true;
  }
  // 如果是开关状态变化，不在这里处理，让watcher处理
  else if (property === "switchState") {
    // 只发送事件，让watcher处理DOM操作和颜色应用
    return true;
  }
  // 如果是开关颜色变化，立即应用
  else if (property === "switchOnColor" || property === "switchOffColor") {
    // 已经在 updateSwitchStateColor 中处理了，这里只需要发送事件
    return true;
  }
  else {
    // 直接应用SVG样式到DOM元素
    applySvgStyleToDom(property, value);
  }

  // 如果是复合操作，返回true以支持链式调用
  return true;
};

// 直接应用SVG样式到DOM元素
const applySvgStyleToDom = (property: string, value: any) => {
  if (!props.selectedComponent?.id) return;

  // 如果SVG样式效果未启用，直接恢复原始状态
  // 但如果是开关切换动画或管道流动动画，不恢复（它们不依赖svgStyleEnabled）
  if (!props.selectedComponent?.style?.svgStyleEnabled) {
    const hasSpecialAnimation = props.selectedComponent?.style?.svgAnimation === 'switchToggle' ||
                                props.selectedComponent?.style?.svgAnimation === 'pipeFlow';
    if (!hasSpecialAnimation) {
      restoreOriginalSvgState();
    }
    return;
  }

  const element = document.getElementById(props.selectedComponent.id);
  if (!element) return;

  const svgElement = element.querySelector("svg");
  if (!svgElement) return;

  // 🎨 检测是否为 interactive 类型的 SVG (包含 JavaScript 脚本控制颜色)
  // 这类 SVG 需要自己控制颜色,不应该被外部样式覆盖
  const svgContent = svgElement.outerHTML;
  const svgTypeInfo = SvgTypeDetector.detectSvgType(svgContent);

  if (svgTypeInfo.type === 'interactive') {
    console.log('🎨 检测到 interactive 类型 SVG，跳过 fillType/fill 样式应用，让脚本控制颜色');
    // 对于 interactive SVG，完全跳过颜色相关的样式应用
    // 只允许非颜色相关的属性(如描边宽度、透明度等)
    if (property === 'fillType' || property === 'fill' || property === 'svgColor' ||
        property === 'fillGradientStart' || property === 'fillGradientEnd' ||
        property === 'fillGradientAngle' || property === 'fillGradientShape') {
      console.log(`⏭️ 跳过 interactive SVG 的颜色属性应用: ${property}`);
      return; // 完全跳过颜色相关的样式应用
    }
  }

  // 🎨 检测组件类型，针对特殊组件只更新液体/进度条部分
  const componentType = props.selectedComponent?.type;
  let fillableElements: NodeListOf<Element>;

  if (componentType && componentType.includes('Tank')) {
    // 🌊 Tank 组件：只选择液体相关元素
    const waterShape = svgElement.querySelector('#waterShape');
    if (waterShape) {
      fillableElements = svgElement.querySelectorAll('#waterShape') as NodeListOf<Element>;
      console.log(`🌊 Tank组件 [${componentType}]：只更新液体元素 waterShape`);
    } else {
      fillableElements = svgElement.querySelectorAll('path, circle, rect, ellipse, polygon, polyline');
    }
  } else if (componentType && componentType.includes('progress')) {
    // 🎯 进度条组件：只选择进度条填充元素
    const progressFill = svgElement.querySelector('#A-GXP_FILL');
    if (progressFill) {
      fillableElements = svgElement.querySelectorAll('#A-GXP_FILL') as NodeListOf<Element>;
      console.log(`🎯 进度条组件 [${componentType}]：只更新进度条填充元素 A-GXP_FILL`);
    } else {
      fillableElements = svgElement.querySelectorAll('path, circle, rect, ellipse, polygon, polyline');
    }
  } else {
    // 普通组件：选择所有可填充元素
    fillableElements = svgElement.querySelectorAll(
      "path, circle, rect, ellipse, polygon, polyline"
    );
  }

  console.log("直接应用SVG样式:", {
    property,
    value,
    componentType,
    elementsCount: fillableElements.length
  });

  // 根据样式属性类型进行处理
  switch (property) {
    case "fillType":
      handleFillTypeUpdate(svgElement, fillableElements, value);
      break;
    case "fill":
    case "svgColor":
      handleColorUpdate(fillableElements, value);
      break;
    case "fillGradientStart":
    case "fillGradientEnd":
    case "fillGradientAngle":
    case "fillGradientShape":
      handleGradientUpdate(svgElement, fillableElements);
      break;
    case "fillOpacity":
      handleOpacityUpdate(fillableElements, value);
      break;
  }
};

const handleFillTypeUpdate = (
  svgElement: SVGSVGElement,
  fillableElements: NodeListOf<Element>,
  fillType: string
) => {
  const component = props.selectedComponent;
  if (!component) return;

  // 如果SVG样式效果未启用，直接清除样式并返回
  if (!component.style?.svgStyleEnabled) {
    console.log("SVG样式效果未启用，清除所有自定义样式");
    fillableElements.forEach(el => {
      (el as SVGElement).style.fill = "";
      (el as SVGElement).style.fillOpacity = "";
    });
    return;
  }

  console.log("处理fillType更新:", fillType);

  switch (fillType) {
    case "transparent":
      // 🌊 透明模式在管道流动时也设置为none
      fillableElements.forEach(el => {
        if (component.style?.svgAnimation === 'pipeFlow') {
          (el as SVGElement).style.stroke = "none";
          (el as SVGElement).style.strokeOpacity = "0";
        } else {
          (el as SVGElement).style.fill = "none";
          (el as SVGElement).style.fillOpacity = "0";
        }
      });
      break;

    case "solid":
      const solidColor =
        component.style?.fill || component.style?.svgColor || "#409eff";
      const opacity = component.style?.fillOpacity || 1;

      // 🌊 如果启用了管道流动动画，设置stroke而不是fill
      if (component.style?.svgAnimation === 'pipeFlow') {
        console.log("🌊 管道流动模式：设置stroke颜色而非fill");
        fillableElements.forEach(el => {
          (el as SVGElement).style.stroke = solidColor;
          (el as SVGElement).style.strokeOpacity = opacity.toString();
          // 不设置fill，保持fill="none"
        });
      } else {
        // 普通模式：设置fill颜色
        fillableElements.forEach(el => {
          (el as SVGElement).style.fill = solidColor;
          (el as SVGElement).style.fillOpacity = opacity.toString();
        });
      }
      break;

    case "linear-gradient":
    case "radial-gradient":
      createAndApplyGradient(svgElement, fillableElements, fillType);
      break;

    case null:
    case undefined:
    case "":
      // 如果fillType为null/undefined/空，清除自定义样式，恢复SVG原始状态
      console.log("fillType为空，清除自定义样式，恢复SVG原始状态");
      fillableElements.forEach(el => {
        if (component.style?.svgAnimation === 'pipeFlow') {
          (el as SVGElement).style.stroke = "";
          (el as SVGElement).style.strokeOpacity = "";
        } else {
          (el as SVGElement).style.fill = "";
          (el as SVGElement).style.fillOpacity = "";
        }
      });
      break;

    default:
      // 其他未知fillType，也恢复原始状态
      console.log("未知fillType，恢复SVG原始状态");
      fillableElements.forEach(el => {
        if (component.style?.svgAnimation === 'pipeFlow') {
          (el as SVGElement).style.stroke = "";
          (el as SVGElement).style.strokeOpacity = "";
        } else {
          (el as SVGElement).style.fill = "";
          (el as SVGElement).style.fillOpacity = "";
        }
      });
      break;
  }
};

const handleColorUpdate = (
  fillableElements: NodeListOf<Element>,
  color: any
) => {
  if (!color) return;

  // 如果SVG样式效果未启用，直接恢复原始状态
  // 但如果是特殊动画（开关切换、管道流动），不恢复
  if (!props.selectedComponent?.style?.svgStyleEnabled) {
    const hasSpecialAnimation = props.selectedComponent?.style?.svgAnimation === 'switchToggle' ||
                                props.selectedComponent?.style?.svgAnimation === 'pipeFlow';
    if (!hasSpecialAnimation) {
      restoreOriginalSvgState();
    }
    return;
  }

  console.log("处理颜色更新:", color);

  // 🌊 如果启用了管道流动动画，更新stroke而不是fill
  if (props.selectedComponent?.style?.svgAnimation === 'pipeFlow') {
    fillableElements.forEach(el => {
      (el as SVGElement).style.stroke = color;
    });
  } else {
    fillableElements.forEach(el => {
      (el as SVGElement).style.fill = color;
    });
  }
};

const handleGradientUpdate = (
  svgElement: SVGSVGElement,
  fillableElements: NodeListOf<Element>
) => {
  // 如果SVG样式效果未启用，直接恢复原始状态
  if (!props.selectedComponent?.style?.svgStyleEnabled) {
    restoreOriginalSvgState();
    return;
  }

  const fillType = props.selectedComponent?.style?.fillType;
  if (fillType === "linear-gradient" || fillType === "radial-gradient") {
    createAndApplyGradient(svgElement, fillableElements, fillType);
  }
};

const handleOpacityUpdate = (
  fillableElements: NodeListOf<Element>,
  opacity: number
) => {
  // 如果SVG样式效果未启用，直接恢复原始状态
  if (!props.selectedComponent?.style?.svgStyleEnabled) {
    restoreOriginalSvgState();
    return;
  }

  console.log("处理透明度更新:", opacity);

  // 🌊 如果启用了管道流动动画，更新strokeOpacity而不是fillOpacity
  if (props.selectedComponent?.style?.svgAnimation === 'pipeFlow') {
    fillableElements.forEach(el => {
      (el as SVGElement).style.strokeOpacity = opacity.toString();
    });
  } else {
    fillableElements.forEach(el => {
      (el as SVGElement).style.fillOpacity = opacity.toString();
    });
  }
};

const createAndApplyGradient = (
  svgElement: SVGSVGElement,
  fillableElements: NodeListOf<Element>,
  type: string
) => {
  const component = props.selectedComponent;
  if (!component) return;

  // 如果SVG样式效果未启用，直接恢复原始状态
  if (!component.style?.svgStyleEnabled) {
    restoreOriginalSvgState();
    return;
  }

  const gradientId = `gradient_${component.id}_${type}`;
  const startColor = component.style?.fillGradientStart || "#409eff";
  const endColor = component.style?.fillGradientEnd || "#67c23a";
  const opacity = component.style?.fillOpacity || 1;

  console.log("创建渐变:", { type, startColor, endColor, gradientId });

  // 创建或更新defs
  let defs = svgElement.querySelector("defs");
  if (!defs) {
    defs = document.createElementNS("http://www.w3.org/2000/svg", "defs");
    svgElement.insertBefore(defs, svgElement.firstChild);
  }

  // 移除现有渐变
  const existingGradient = defs.querySelector(`#${gradientId}`);
  if (existingGradient) {
    existingGradient.remove();
  }

  // 创建新渐变
  let gradient;
  if (type === "linear-gradient") {
    gradient = document.createElementNS(
      "http://www.w3.org/2000/svg",
      "linearGradient"
    );
    const angle = component.style?.fillGradientAngle || 0;
    const radians = (angle * Math.PI) / 180;
    const x2 = Math.cos(radians);
    const y2 = Math.sin(radians);

    gradient.setAttribute("x1", "0%");
    gradient.setAttribute("y1", "0%");
    gradient.setAttribute("x2", `${(x2 + 1) * 50}%`);
    gradient.setAttribute("y2", `${(y2 + 1) * 50}%`);
  } else {
    gradient = document.createElementNS(
      "http://www.w3.org/2000/svg",
      "radialGradient"
    );
    gradient.setAttribute("cx", "50%");
    gradient.setAttribute("cy", "50%");
    gradient.setAttribute("r", "50%");
  }

  gradient.setAttribute("id", gradientId);

  // 创建停止点
  const stop1 = document.createElementNS("http://www.w3.org/2000/svg", "stop");
  stop1.setAttribute("offset", "0%");
  stop1.setAttribute("stop-color", startColor);
  stop1.setAttribute("stop-opacity", opacity.toString());

  const stop2 = document.createElementNS("http://www.w3.org/2000/svg", "stop");
  stop2.setAttribute("offset", "100%");
  stop2.setAttribute("stop-color", endColor);
  stop2.setAttribute("stop-opacity", opacity.toString());

  gradient.appendChild(stop1);
  gradient.appendChild(stop2);
  defs.appendChild(gradient);

  // 应用渐变到元素
  const gradientUrl = `url(#${gradientId})`;

  // 🌊 如果启用了管道流动动画，应用到stroke而不是fill
  if (component.style?.svgAnimation === 'pipeFlow') {
    console.log("🌊 管道流动模式：应用渐变到stroke");
    fillableElements.forEach(el => {
      (el as SVGElement).style.stroke = gradientUrl;
      (el as SVGElement).style.strokeOpacity = "1";
      // 不设置fill，保持fill="none"
    });
  } else {
    // 普通模式：应用到fill
    fillableElements.forEach(el => {
      (el as SVGElement).style.fill = gradientUrl;
      (el as SVGElement).style.fillOpacity = "1"; // 渐变通过stop-opacity控制
    });
  }
};

// 处理填充类型变化
const handleFillTypeChange = (value: string) => {
  console.log("填充类型变化:", {
    newType: value,
    componentId: props.selectedComponent?.id
  });

  // 首先更新填充类型
  updateStyle("fillType", value);

  // 根据新类型清理不相关的属性
  switch (value) {
    case "solid":
      // 纯色：清除渐变相关属性
      updateStyle("fillGradientStart", null);
      updateStyle("fillGradientEnd", null);
      updateStyle("fillGradientAngle", null);
      updateStyle("fillGradientShape", null);
      // 如果没有填充色，设置默认值
      if (
        !props.selectedComponent?.style?.fill &&
        !props.selectedComponent?.style?.svgColor
      ) {
        updateStyle("fill", "#409eff");
      }
      break;
    case "transparent":
      // 透明：清除所有填充相关属性
      updateStyle("fill", null);
      updateStyle("svgColor", null);
      updateStyle("fillGradientStart", null);
      updateStyle("fillGradientEnd", null);
      updateStyle("fillGradientAngle", null);
      updateStyle("fillGradientShape", null);
      break;
    case "linear-gradient":
    case "radial-gradient":
      // 渐变：清除纯色属性，设置默认渐变值
      updateStyle("fill", null);
      updateStyle("svgColor", null);
      // 设置默认渐变色
      if (!props.selectedComponent?.style?.fillGradientStart) {
        updateStyle("fillGradientStart", "#409eff");
      }
      if (!props.selectedComponent?.style?.fillGradientEnd) {
        updateStyle("fillGradientEnd", "#67c23a");
      }
      if (
        value === "linear-gradient" &&
        !props.selectedComponent?.style?.fillGradientAngle
      ) {
        updateStyle("fillGradientAngle", 0);
      }
      if (
        value === "radial-gradient" &&
        !props.selectedComponent?.style?.fillGradientShape
      ) {
        updateStyle("fillGradientShape", "circle");
      }
      break;
  }
};

// 处理填充颜色变化
const handleFillColorChange = (value: any) => {
  console.log("填充颜色变化:", {
    value: value,
    type: typeof value,
    isNull: value === null,
    isUndefined: value === undefined
  });

  // 确保有效的颜色值
  if (value === null || value === undefined || value === "") {
    updateStyle("fill", null);
    updateStyle("svgColor", null);
  } else {
    // 同时更新fill和svgColor以保持兼容性
    updateStyle("fill", value);
    updateStyle("svgColor", value);
  }
};

// 处理开关状态颜色变化
const updateSwitchStateColor = (value: any) => {
  console.log("开关状态颜色变化:", {
    value: value,
    currentState: props.selectedComponent?.style?.switchState
  });

  if (!value) return;

  // 根据当前状态更新对应的颜色
  const isOn = props.selectedComponent?.style?.switchState === 'on';
  if (isOn) {
    updateStyle('switchOnColor', value);
  } else {
    updateStyle('switchOffColor', value);
  }

  // 立即应用颜色到DOM
  applySwitchStateColor(value, isOn);
};

// 应用开关状态颜色到DOM
const applySwitchStateColor = (color: string, isOn: boolean) => {
  if (!props.selectedComponent?.id) return;

  const element = document.getElementById(props.selectedComponent.id);
  if (!element) return;

  const svgElement = element.querySelector("svg");
  if (!svgElement) return;

  // 🔘 使用 SvgManager 的 updateSwitchState 方法来更新开关状态
  // 这样可以保证过渡效果不会被清除
  const newState = isOn ? 'on' : 'off';
  const onColor = props.selectedComponent?.style?.switchOnColor || '#67c23a';
  const offColor = props.selectedComponent?.style?.switchOffColor || '#909399';

  svgManager.updateSwitchState(svgElement as SVGSVGElement, newState, onColor, offColor);

  console.log(`🔘 应用开关${isOn ? '开启' : '关闭'}状态，颜色:`, color);
};

// 处理SVG样式启用/禁用变化
const handleSvgStyleEnabledChange = (enabled: boolean) => {
  console.log("处理SVG样式启用/禁用变化:", enabled);

  if (!enabled) {
    // 禁用时，恢复SVG原始状态
    console.log("禁用SVG样式效果，恢复原始状态");
    restoreOriginalSvgState();
  } else {
    // 启用时，检查是否有现有样式配置
    console.log("启用SVG样式效果");
    const component = props.selectedComponent;

    if (!component?.style?.fillType) {
      // 没有现有样式，设置默认纯色样式
      console.log("没有现有样式，设置默认纯色样式");
      emit("update-style", "fillType", "solid");
      emit("update-style", "fill", "#409eff");
      emit("update-style", "fillOpacity", 1);

      // 延迟应用样式，确保state更新后再应用
      nextTick(() => {
        applySvgStyleToDom("fillType", "solid");
      });
    } else {
      // 有现有样式，直接应用
      console.log("应用现有样式:", component.style.fillType);
      applySvgStyleToDom("fillType", component.style.fillType);
    }
  }
};

// 监听选中组件变化，在DOM更新后检查SVG可见性
watch(
  () => props.selectedComponent,
  newComponent => {
    if (newComponent) {
      nextTick(() => {
        initializeSvgVisibility();
      });
    }
  },
  { immediate: true }
);

// 监听svgStyleEnabled状态变化
watch(
  () => props.selectedComponent?.style?.svgStyleEnabled,
  (newEnabled, oldEnabled) => {
    console.log("监听到SVG样式启用状态变化:", {
      old: oldEnabled,
      new: newEnabled,
      component: props.selectedComponent?.id
    });

    if (newEnabled !== oldEnabled) {
      nextTick(() => {
        if (newEnabled === true || newEnabled === false) {
          console.log("执行SVG样式状态处理:", newEnabled);
          handleSvgStyleEnabledChange(newEnabled);
        }
      });
    }
  }
);

// 监听组件样式的所有变化，确保DOM同步
watch(
  () => props.selectedComponent?.style,
  (newStyle, oldStyle) => {
    if (newStyle && props.selectedComponent?.id) {
      console.log("监听到组件样式变化:", {
        component: props.selectedComponent.id,
        svgStyleEnabled: newStyle.svgStyleEnabled,
        fillType: newStyle.fillType
      });

      // 如果SVG样式被禁用，恢复原始状态
      if (newStyle.svgStyleEnabled === false) {
        nextTick(() => {
          console.log("样式监听器: 恢复原始状态");
          restoreOriginalSvgState();
        });
      }
      // 如果SVG样式被启用且有fillType，应用样式
      else if (newStyle.svgStyleEnabled === true && newStyle.fillType) {
        nextTick(() => {
          console.log("样式监听器: 应用样式", newStyle.fillType);
          applySvgStyleToDom("fillType", newStyle.fillType);
        });
      }
    }
  },
  { deep: true }
);

// 监听开关状态变化
watch(
  () => props.selectedComponent?.style?.switchState,
  (newState, oldState) => {
    if (newState !== oldState && props.selectedComponent?.style?.svgAnimation === 'switchToggle') {
      console.log("🔘 开关状态变化:", {
        old: oldState,
        new: newState,
        component: props.selectedComponent?.id
      });

      nextTick(() => {
        const isOn = newState === 'on';
        const color = isOn
          ? (props.selectedComponent?.style?.switchOnColor || '#67c23a')
          : (props.selectedComponent?.style?.switchOffColor || '#909399');

        applySwitchStateColor(color, isOn);
      });
    }
  }
);

// 🎯 监听 animationStaticValue 变化，用于调试
watch(
  () => props.selectedComponent?.style?.animationStaticValue,
  (newValue, oldValue) => {
    console.log("🎯 animationStaticValue 变化:", {
      old: oldValue,
      new: newValue,
      component: props.selectedComponent?.id,
      componentType: props.selectedComponent?.type
    });
  }
);

// 🎯 监听 animationStaticValue computed 属性变化
watch(
  animationStaticValue,
  (newValue, oldValue) => {
    console.log("🎯 animationStaticValue computed 变化:", {
      old: oldValue,
      new: newValue
    });
  }
);
</script>

<style scoped>
.svg-icon-style-config {
  width: 100%;
}

.property-section {
  margin-bottom: 16px;
  padding: 12px;
  border-radius: 16px;
  background: linear-gradient(135deg, #ffffff 0%, #f8fafc 100%);
  border: 1px solid rgba(102, 126, 234, 0.1);
  box-shadow: 0 8px 32px rgba(102, 126, 234, 0.08);
  position: relative;
  max-width: 100%;
  box-sizing: border-box;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.property-section::before {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  border-radius: 16px;
  padding: 2px;
  background: linear-gradient(135deg, #667eea, #764ba2, #ff6b6b, #ffa726);
  background-size: 200% 200%;
  mask:
    linear-gradient(#fff 0 0) content-box,
    linear-gradient(#fff 0 0);
  mask-composite: xor;
  -webkit-mask-composite: xor;
  animation: borderGlow 4s ease-in-out infinite;
  opacity: 0;
  transition: opacity 0.3s ease;
}

.property-section:hover::before {
  opacity: 0.3;
}

.section-title {
  font-size: 14px;
  font-weight: 700;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 12px;
  padding: 8px 12px;
  background-color: rgba(102, 126, 234, 0.05);
  border-radius: 12px;
  border: none;
  position: relative;
  display: flex;
  align-items: center;
}

.section-title::before {
  content: "⚡";
  font-size: 16px;
  margin-right: 8px;
  animation: pulse 2s infinite;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.section-title::after {
  content: "";
  position: absolute;
  left: 0;
  bottom: 0;
  width: 100%;
  height: 2px;
  background: linear-gradient(90deg, #667eea, #764ba2);
  border-radius: 1px;
  transform: scaleX(0);
  transition: transform 0.3s ease;
}

.section-title:hover::after {
  transform: scaleX(1);
}

@keyframes borderGlow {
  0%,
  100% {
    background-position: 0% 50%;
    opacity: 0.3;
  }
  25% {
    background-position: 100% 50%;
    opacity: 0.8;
  }
  50% {
    background-position: 200% 50%;
    opacity: 1;
  }
  75% {
    background-position: 300% 50%;
    opacity: 0.8;
  }
}

@keyframes pulse {
  0% {
    transform: scale(1);
    opacity: 0.8;
  }
  50% {
    transform: scale(1.1);
    opacity: 1;
  }
  100% {
    transform: scale(1);
    opacity: 0.8;
  }
}
</style>
