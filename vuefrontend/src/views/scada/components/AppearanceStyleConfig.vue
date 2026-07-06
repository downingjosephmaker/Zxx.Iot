<template>
  <div class="appearance-style-config">
    <el-form size="small" label-width="80px">
      <!-- 背景样式 -->
      <div class="property-section">
        <div class="section-title">背景</div>
        <el-form-item label="背景类型">
          <el-select
            :model-value="selectedComponent?.style?.backgroundType || 'solid'"
            @change="handleBackgroundTypeChange"
          >
            <el-option label="纯色" value="solid" />
            <el-option label="透明" value="transparent" />
            <el-option label="线性渐变" value="linear-gradient" />
            <el-option label="径向渐变" value="radial-gradient" />
            <el-option label="图片" value="image" />
          </el-select>
        </el-form-item>

        <!-- 纯色背景 -->
        <el-form-item
          v-if="
            (selectedComponent?.style?.backgroundType || 'solid') === 'solid'
          "
          label="背景色"
        >
          <el-color-picker
            :model-value="
              selectedComponent?.style?.backgroundColor ||
              (selectedComponent?.style?.backgroundType === 'solid'
                ? 'rgba(255, 255, 255, 1)'
                : 'transparent')
            "
            show-alpha
            @change="handleBackgroundColorChange"
            @input="handleBackgroundColorChange"
          />
        </el-form-item>

        <!-- 线性渐变 -->
        <div
          v-if="selectedComponent?.style?.backgroundType === 'linear-gradient'"
        >
          <el-form-item label="起始色">
            <el-color-picker
              :model-value="
                selectedComponent?.style?.gradientStart || '#409eff'
              "
              show-alpha
              @change="updateStyle('gradientStart', $event)"
            />
          </el-form-item>
          <el-form-item label="结束色">
            <el-color-picker
              :model-value="selectedComponent?.style?.gradientEnd || '#67c23a'"
              show-alpha
              @change="updateStyle('gradientEnd', $event)"
            />
          </el-form-item>
          <el-form-item label="渐变角度">
            <el-slider
              :model-value="selectedComponent?.style?.gradientAngle || 0"
              :min="0"
              :max="360"
              :step="1"
              show-input
              @input="updateStyle('gradientAngle', $event)"
            />
            <span class="unit">°</span>
          </el-form-item>
        </div>

        <!-- 径向渐变 -->
        <div
          v-if="selectedComponent?.style?.backgroundType === 'radial-gradient'"
        >
          <el-form-item label="中心色">
            <el-color-picker
              :model-value="
                selectedComponent?.style?.gradientStart || '#409eff'
              "
              show-alpha
              @change="updateStyle('gradientStart', $event)"
            />
          </el-form-item>
          <el-form-item label="边缘色">
            <el-color-picker
              :model-value="selectedComponent?.style?.gradientEnd || '#67c23a'"
              show-alpha
              @change="updateStyle('gradientEnd', $event)"
            />
          </el-form-item>
          <el-form-item label="形状">
            <el-select
              :model-value="selectedComponent?.style?.gradientShape || 'circle'"
              @change="updateStyle('gradientShape', $event)"
            >
              <el-option label="圆形" value="circle" />
              <el-option label="椭圆" value="ellipse" />
            </el-select>
          </el-form-item>
        </div>

        <!-- 背景图片 -->
        <div v-if="selectedComponent?.style?.backgroundType === 'image'">
          <el-form-item label="图片URL">
            <el-input
              :model-value="selectedComponent?.style?.backgroundImage || ''"
              placeholder="请输入图片URL"
              @input="updateStyle('backgroundImage', $event)"
            />
          </el-form-item>
          <el-form-item label="重复方式">
            <el-select
              :model-value="
                selectedComponent?.style?.backgroundRepeat || 'no-repeat'
              "
              @change="updateStyle('backgroundRepeat', $event)"
            >
              <el-option label="不重复" value="no-repeat" />
              <el-option label="重复" value="repeat" />
              <el-option label="水平重复" value="repeat-x" />
              <el-option label="垂直重复" value="repeat-y" />
            </el-select>
          </el-form-item>
          <el-form-item label="尺寸">
            <el-select
              :model-value="selectedComponent?.style?.backgroundSize || 'cover'"
              @change="updateStyle('backgroundSize', $event)"
            >
              <el-option label="覆盖" value="cover" />
              <el-option label="包含" value="contain" />
              <el-option label="拉伸" value="100% 100%" />
              <el-option label="原始" value="auto" />
            </el-select>
          </el-form-item>
          <el-form-item label="位置">
            <el-select
              :model-value="
                selectedComponent?.style?.backgroundPosition || 'center'
              "
              @change="updateStyle('backgroundPosition', $event)"
            >
              <el-option label="居中" value="center" />
              <el-option label="左上" value="top left" />
              <el-option label="顶部" value="top" />
              <el-option label="右上" value="top right" />
              <el-option label="左侧" value="left" />
              <el-option label="右侧" value="right" />
              <el-option label="左下" value="bottom left" />
              <el-option label="底部" value="bottom" />
              <el-option label="右下" value="bottom right" />
            </el-select>
          </el-form-item>
        </div>
      </div>

      <!-- 边框样式 -->
      <div class="property-section">
        <div class="section-title">边框</div>
        <el-form-item label="边框样式">
          <el-select
            :model-value="selectedComponent?.style?.borderStyle || 'none'"
            @change="handleBorderStyleChange"
          >
            <el-option label="无边框" value="none" />
            <el-option label="实线" value="solid" />
            <el-option label="虚线" value="dashed" />
            <el-option label="点线" value="dotted" />
            <el-option label="双线" value="double" />
          </el-select>
        </el-form-item>
        <el-form-item label="边框色">
          <el-color-picker
            :model-value="selectedComponent?.style?.borderColor || '#d9d9d9'"
            show-alpha
            @change="updateStyle('borderColor', $event)"
          />
        </el-form-item>
        <el-form-item label="边框宽度">
          <el-input-number
            :model-value="selectedComponent?.style?.borderWidth || 1"
            :min="0"
            :max="20"
            :step="1"
            controls-position="right"
            @input="updateStyle('borderWidth', $event)"
          />
          <span class="unit">px</span>
        </el-form-item>
        <el-form-item label="圆角">
          <el-input-number
            :model-value="selectedComponent?.style?.borderRadius || 0"
            :min="0"
            :max="100"
            :step="1"
            controls-position="right"
            @input="updateStyle('borderRadius', $event)"
          />
          <span class="unit">px</span>
        </el-form-item>
      </div>

      <!-- SVG样式 -->
      <div v-if="isSvgComponent()" class="property-section">
        <div class="section-title">SVG样式</div>
        <el-form-item label="填充色">
          <el-color-picker
            :model-value="selectedComponent?.style?.fill || '#000000'"
            show-alpha
            @change="updateStyle('fill', $event)"
          />
        </el-form-item>
        <el-form-item label="描边色">
          <el-color-picker
            :model-value="selectedComponent?.style?.stroke || 'none'"
            show-alpha
            @change="updateStyle('stroke', $event)"
          />
        </el-form-item>
        <el-form-item label="描边宽度">
          <el-input-number
            :model-value="selectedComponent?.style?.strokeWidth || 1"
            :min="0"
            :max="20"
            :step="0.5"
            :precision="1"
            controls-position="right"
            @input="updateStyle('strokeWidth', $event)"
          />
          <span class="unit">px</span>
        </el-form-item>
        <el-form-item label="描边样式">
          <el-select
            :model-value="selectedComponent?.style?.strokeDasharray || 'none'"
            @change="updateStyle('strokeDasharray', $event)"
          >
            <el-option label="实线" value="none" />
            <el-option label="虚线" value="5,5" />
            <el-option label="点线" value="2,2" />
            <el-option label="点划线" value="5,5,2,5" />
          </el-select>
        </el-form-item>
        <el-form-item label="线帽样式">
          <el-select
            :model-value="selectedComponent?.style?.strokeLinecap || 'butt'"
            @change="updateStyle('strokeLinecap', $event)"
          >
            <el-option label="方形" value="butt" />
            <el-option label="圆形" value="round" />
            <el-option label="方形延伸" value="square" />
          </el-select>
        </el-form-item>
        <el-form-item label="连接样式">
          <el-select
            :model-value="selectedComponent?.style?.strokeLinejoin || 'miter'"
            @change="updateStyle('strokeLinejoin', $event)"
          >
            <el-option label="尖角" value="miter" />
            <el-option label="圆角" value="round" />
            <el-option label="平角" value="bevel" />
          </el-select>
        </el-form-item>
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
        <el-form-item label="描边透明度">
          <el-slider
            :model-value="selectedComponent?.style?.strokeOpacity || 1"
            :min="0"
            :max="1"
            :step="0.01"
            :precision="2"
            show-input
            @input="updateStyle('strokeOpacity', $event)"
          />
        </el-form-item>
      </div>

      <!-- 阴影效果 -->
      <div class="property-section">
        <div class="section-title">阴影</div>
        <el-form-item label="启用阴影">
          <el-switch
            :model-value="selectedComponent?.style?.enableShadow || false"
            @change="updateStyle('enableShadow', $event)"
          />
        </el-form-item>
        <div v-if="selectedComponent?.style?.enableShadow">
          <el-form-item label="阴影类型">
            <el-select
              :model-value="selectedComponent?.style?.shadowType || 'box'"
              @change="updateStyle('shadowType', $event)"
            >
              <el-option label="盒阴影" value="box" />
              <el-option label="文字阴影" value="text" />
            </el-select>
          </el-form-item>
          <el-form-item label="阴影色">
            <el-color-picker
              :model-value="
                selectedComponent?.style?.shadowColor || 'rgba(0,0,0,0.3)'
              "
              show-alpha
              @change="updateStyle('shadowColor', $event)"
            />
          </el-form-item>
          <el-form-item label="水平偏移">
            <el-input-number
              :model-value="selectedComponent?.style?.shadowOffsetX || 4"
              :min="-20"
              :max="20"
              :step="1"
              controls-position="right"
              @input="updateStyle('shadowOffsetX', $event)"
            />
            <span class="unit">px</span>
          </el-form-item>
          <el-form-item label="垂直偏移">
            <el-input-number
              :model-value="selectedComponent?.style?.shadowOffsetY || 4"
              :min="-20"
              :max="20"
              :step="1"
              controls-position="right"
              @input="updateStyle('shadowOffsetY', $event)"
            />
            <span class="unit">px</span>
          </el-form-item>
          <el-form-item label="模糊半径">
            <el-input-number
              :model-value="selectedComponent?.style?.shadowBlur || 8"
              :min="0"
              :max="50"
              :step="1"
              controls-position="right"
              @input="updateStyle('shadowBlur', $event)"
            />
            <span class="unit">px</span>
          </el-form-item>
          <el-form-item
            v-if="selectedComponent?.style?.shadowType === 'box'"
            label="扩散半径"
          >
            <el-input-number
              :model-value="selectedComponent?.style?.shadowSpread || 0"
              :min="-20"
              :max="20"
              :step="1"
              controls-position="right"
              @input="updateStyle('shadowSpread', $event)"
            />
            <span class="unit">px</span>
          </el-form-item>
          <el-form-item
            v-if="selectedComponent?.style?.shadowType === 'box'"
            label="内阴影"
          >
            <el-switch
              :model-value="selectedComponent?.style?.shadowInset || false"
              @change="updateStyle('shadowInset', $event)"
            />
          </el-form-item>
        </div>
      </div>

      <!-- 视觉效果 -->
      <div class="property-section">
        <div class="section-title">视觉效果</div>
        <el-form-item label="透明度">
          <el-slider
            :model-value="selectedComponent?.style?.opacity || 1"
            :min="0"
            :max="1"
            :step="0.01"
            :precision="2"
            show-input
            @input="updateStyle('opacity', $event)"
          />
        </el-form-item>
        <el-form-item label="模糊效果">
          <el-slider
            :model-value="selectedComponent?.style?.blur || 0"
            :min="0"
            :max="10"
            :step="0.1"
            :precision="1"
            show-input
            @input="updateStyle('blur', $event)"
          />
          <span class="unit">px</span>
        </el-form-item>
        <el-form-item label="亮度">
          <el-slider
            :model-value="selectedComponent?.style?.brightness || 1"
            :min="0"
            :max="2"
            :step="0.1"
            :precision="1"
            show-input
            @input="updateStyle('brightness', $event)"
          />
        </el-form-item>
        <el-form-item label="对比度">
          <el-slider
            :model-value="selectedComponent?.style?.contrast || 1"
            :min="0"
            :max="2"
            :step="0.1"
            :precision="1"
            show-input
            @input="updateStyle('contrast', $event)"
          />
        </el-form-item>
        <el-form-item label="饱和度">
          <el-slider
            :model-value="selectedComponent?.style?.saturate || 1"
            :min="0"
            :max="2"
            :step="0.1"
            :precision="1"
            show-input
            @input="updateStyle('saturate', $event)"
          />
        </el-form-item>
        <el-form-item label="色相旋转">
          <el-slider
            :model-value="selectedComponent?.style?.hueRotate || 0"
            :min="0"
            :max="360"
            :step="1"
            show-input
            @input="updateStyle('hueRotate', $event)"
          />
          <span class="unit">°</span>
        </el-form-item>
        <el-form-item label="反色">
          <el-slider
            :model-value="selectedComponent?.style?.invert || 0"
            :min="0"
            :max="1"
            :step="0.1"
            :precision="1"
            show-input
            @input="updateStyle('invert', $event)"
          />
        </el-form-item>
        <el-form-item label="褐色滤镜">
          <el-slider
            :model-value="selectedComponent?.style?.sepia || 0"
            :min="0"
            :max="1"
            :step="0.1"
            :precision="1"
            show-input
            @input="updateStyle('sepia', $event)"
          />
        </el-form-item>
        <el-form-item label="灰度">
          <el-slider
            :model-value="selectedComponent?.style?.grayscale || 0"
            :min="0"
            :max="1"
            :step="0.1"
            :precision="1"
            show-input
            @input="updateStyle('grayscale', $event)"
          />
        </el-form-item>
      </div>

      <!-- 样式预设 -->
      <div class="property-section">
        <div class="section-title">样式预设</div>
        <div class="preset-grid">
          <!-- 第一行：经典预设 -->
          <el-row :gutter="6">
            <el-col :span="8">
              <el-button
                size="small"
                type="primary"
                @click="applyPresetStyle('modern')"
                :style="{ fontSize: '11px', padding: '6px 4px' }"
              >
                🎨 现代
              </el-button>
            </el-col>
            <el-col :span="8">
              <el-button
                size="small"
                @click="applyPresetStyle('classic')"
                :style="{ fontSize: '11px', padding: '6px 4px' }"
              >
                📚 经典
              </el-button>
            </el-col>
            <el-col :span="8">
              <el-button
                size="small"
                @click="applyPresetStyle('minimal')"
                :style="{ fontSize: '11px', padding: '6px 4px' }"
              >
                ✨ 简约
              </el-button>
            </el-col>
          </el-row>

          <!-- 第二行：特殊效果预设 -->
          <el-row :gutter="6" style="margin-top: 6px">
            <el-col :span="8">
              <el-button
                size="small"
                type="success"
                @click="applyPresetStyle('neon')"
                :style="{ fontSize: '11px', padding: '6px 4px' }"
              >
                💫 霓虹
              </el-button>
            </el-col>
            <el-col :span="8">
              <el-button
                size="small"
                type="warning"
                @click="applyPresetStyle('glass')"
                :style="{ fontSize: '11px', padding: '6px 4px' }"
              >
                🔮 玻璃
              </el-button>
            </el-col>
            <el-col :span="8">
              <el-button
                size="small"
                @click="applyPresetStyle('gradient')"
                :style="{ fontSize: '11px', padding: '6px 4px' }"
              >
                🌈 渐变
              </el-button>
            </el-col>
          </el-row>

          <!-- 第三行：功能预设 -->
          <el-row :gutter="4" style="margin-top: 6px">
            <el-col :span="8">
              <el-button
                size="small"
                type="info"
                @click="copyCurrentStyle()"
                :style="{ fontSize: '10px', padding: '4px 2px' }"
              >
                📋 复制
              </el-button>
            </el-col>
            <el-col :span="8">
              <el-button
                size="small"
                type="success"
                @click="pasteStyle()"
                :style="{ fontSize: '10px', padding: '4px 2px' }"
              >
                📌 粘贴
              </el-button>
            </el-col>
            <el-col :span="8">
              <el-button
                size="small"
                type="danger"
                @click="resetStyle()"
                :style="{ fontSize: '10px', padding: '4px 2px' }"
              >
                🧹 清空
              </el-button>
            </el-col>
          </el-row>
        </div>
      </div>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { nextTick } from "vue";
import { ElMessage } from "element-plus";

interface Props {
  selectedComponent?: any;
}

interface Emits {
  (e: "update-style", property: string, value: any): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 判断是否是SVG组件
const isSvgComponent = () => {
  if (!props.selectedComponent?.type) return false;
  const svgTypes = [
    "svg",
    "icon",
    "shape",
    "vector",
    "graphic",
    "symbol",
    "drawing"
  ];
  return svgTypes.includes(props.selectedComponent.type?.toLowerCase() || "");
};

const updateStyle = (property: string, value: any) => {
  emit("update-style", property, value);
};

// 专门处理背景类型变化
const handleBackgroundTypeChange = (value: string) => {
  console.log("背景类型变化:", {
    newType: value,
    componentId: props.selectedComponent?.id
  });

  // 首先更新背景类型
  updateStyle("backgroundType", value);

  // 根据新类型清理不相关的属性
  switch (value) {
    case "solid":
      // 纯色：清除渐变和图片相关属性
      updateStyle("gradientStart", null);
      updateStyle("gradientEnd", null);
      updateStyle("gradientAngle", null);
      updateStyle("gradientShape", null);
      updateStyle("backgroundImage", null);
      updateStyle("backgroundRepeat", null);
      updateStyle("backgroundSize", null);
      updateStyle("backgroundPosition", null);
      // 如果没有背景色，设置默认白色
      if (!props.selectedComponent?.style?.backgroundColor) {
        updateStyle("backgroundColor", "rgba(255, 255, 255, 1)");
      }
      break;
    case "transparent":
      // 透明：清除所有背景相关属性
      updateStyle("backgroundColor", null);
      updateStyle("gradientStart", null);
      updateStyle("gradientEnd", null);
      updateStyle("gradientAngle", null);
      updateStyle("gradientShape", null);
      updateStyle("backgroundImage", null);
      updateStyle("backgroundRepeat", null);
      updateStyle("backgroundSize", null);
      updateStyle("backgroundPosition", null);
      break;
    case "linear-gradient":
    case "radial-gradient":
      // 渐变：清除纯色和图片相关属性
      updateStyle("backgroundColor", null);
      updateStyle("backgroundImage", null);
      updateStyle("backgroundRepeat", null);
      updateStyle("backgroundSize", null);
      updateStyle("backgroundPosition", null);
      // 设置默认渐变色
      if (!props.selectedComponent?.style?.gradientStart) {
        updateStyle("gradientStart", "#409eff");
      }
      if (!props.selectedComponent?.style?.gradientEnd) {
        updateStyle("gradientEnd", "#67c23a");
      }
      if (
        value === "linear-gradient" &&
        !props.selectedComponent?.style?.gradientAngle
      ) {
        updateStyle("gradientAngle", 0);
      }
      if (
        value === "radial-gradient" &&
        !props.selectedComponent?.style?.gradientShape
      ) {
        updateStyle("gradientShape", "circle");
      }
      break;
    case "image":
      // 图片：清除纯色和渐变相关属性
      updateStyle("backgroundColor", null);
      updateStyle("gradientStart", null);
      updateStyle("gradientEnd", null);
      updateStyle("gradientAngle", null);
      updateStyle("gradientShape", null);
      // 设置默认图片属性
      if (!props.selectedComponent?.style?.backgroundRepeat) {
        updateStyle("backgroundRepeat", "no-repeat");
      }
      if (!props.selectedComponent?.style?.backgroundSize) {
        updateStyle("backgroundSize", "cover");
      }
      if (!props.selectedComponent?.style?.backgroundPosition) {
        updateStyle("backgroundPosition", "center");
      }
      break;
  }

  // 强制重新应用样式和重绘画布
  if (props.selectedComponent) {
    // 触发样式更新
    nextTick(() => {
      // 确保DOM更新后再次应用样式
      console.log("背景类型切换完成，强制更新组件样式");
    });
  }
};

// 专门处理边框样式变化
const handleBorderStyleChange = (value: string) => {
  console.log("边框样式变化:", {
    newStyle: value,
    componentId: props.selectedComponent?.id
  });

  // 更新边框样式
  updateStyle("borderStyle", value);

  // 如果设置边框样式不是none，确保有默认的边框宽度和颜色
  if (value !== "none") {
    // 如果没有边框宽度，设置默认值
    if (!props.selectedComponent?.style?.borderWidth) {
      updateStyle("borderWidth", 1);
    }
    // 如果没有边框颜色，设置默认值
    if (!props.selectedComponent?.style?.borderColor) {
      updateStyle("borderColor", "#d9d9d9");
    }
  } else {
    // 如果设置为none，清除边框宽度（但保留颜色设置）
    updateStyle("borderWidth", 0);
  }
};

// 专门处理背景颜色变化
const handleBackgroundColorChange = (value: any) => {
  console.log("背景颜色变化:", {
    value: value,
    type: typeof value,
    isNull: value === null,
    isUndefined: value === undefined
  });

  // 确保有效的颜色值
  if (value === null || value === undefined || value === "") {
    // 如果颜色为空，将背景类型设置为透明
    updateStyle("backgroundType", "transparent");
    updateStyle("backgroundColor", null);
    return;
  }

  // 检查是否是透明色
  if (value === "transparent" || value === "rgba(0, 0, 0, 0)") {
    console.log("检测到透明色，更新背景类型为transparent");
    updateStyle("backgroundType", "transparent");
    updateStyle("backgroundColor", null);
    return;
  }

  // 检查是否是rgba格式且透明度为0
  if (typeof value === "string" && value.includes("rgba")) {
    const rgbaMatch = value.match(
      /rgba\((\d+),\s*(\d+),\s*(\d+),\s*([\d.]+)\)/
    );
    if (rgbaMatch) {
      const [, r, g, b, a] = rgbaMatch;
      const alpha = parseFloat(a);

      console.log("RGBA颜色分析:", {
        原始值: value,
        红: r,
        绿: g,
        蓝: b,
        透明度: alpha
      });

      // 如果透明度为0，将背景类型设置为透明
      if (alpha === 0) {
        console.log("透明度为0，将背景类型设置为transparent");
        updateStyle("backgroundType", "transparent");
        updateStyle("backgroundColor", null);
        return;
      }
    }
  }

  // 有效的纯色，确保背景类型为solid
  if (props.selectedComponent?.style?.backgroundType !== "solid") {
    console.log("设置背景颜色时，自动将背景类型设为solid");
    updateStyle("backgroundType", "solid");
  }
  updateStyle("backgroundColor", value);
};

// 应用预设样式
const applyPresetStyle = (preset: string) => {
  if (!props.selectedComponent) return;

  // 根据组件类型使用不同的预设样式
  const isSvg = isSvgComponent();

  const presetStyles: Record<string, any> = isSvg
    ? {
        // SVG组件预设
        modern: {
          fill: "#409eff",
          stroke: "#303133",
          strokeWidth: 1,
          strokeDasharray: "none",
          fillOpacity: 1,
          strokeOpacity: 1
        },
        classic: {
          fill: "#67c23a",
          stroke: "#6c757d",
          strokeWidth: 2,
          strokeDasharray: "none",
          fillOpacity: 0.8,
          strokeOpacity: 1
        },
        minimal: {
          fill: "transparent",
          stroke: "#909399",
          strokeWidth: 1,
          strokeDasharray: "none",
          fillOpacity: 0,
          strokeOpacity: 0.7
        }
      }
    : {
        // 普通组件预设
        modern: {
          backgroundType: "solid",
          backgroundColor: "#ffffff",
          borderStyle: "solid",
          borderColor: "#e1e5e9",
          borderWidth: 1,
          borderRadius: 12,
          enableShadow: true,
          shadowType: "box",
          shadowColor: "rgba(0, 0, 0, 0.08)",
          shadowOffsetX: 4,
          shadowOffsetY: 8,
          shadowBlur: 16,
          shadowSpread: -2,
          shadowInset: false,
          opacity: 1
        },
        classic: {
          backgroundType: "solid",
          backgroundColor: "#f8f9fa",
          borderStyle: "solid",
          borderColor: "#6c757d",
          borderWidth: 2,
          borderRadius: 6,
          enableShadow: true,
          shadowType: "box",
          shadowColor: "rgba(0, 0, 0, 0.15)",
          shadowOffsetX: 2,
          shadowOffsetY: 4,
          shadowBlur: 8,
          shadowSpread: 0,
          shadowInset: false,
          opacity: 1
        },
        minimal: {
          backgroundType: "solid",
          backgroundColor: "rgba(255, 255, 255, 0.8)",
          borderStyle: "solid",
          borderColor: "rgba(200, 200, 200, 0.3)",
          borderWidth: 1,
          borderRadius: 2,
          enableShadow: true,
          shadowType: "box",
          shadowColor: "rgba(0, 0, 0, 0.05)",
          shadowOffsetX: 1,
          shadowOffsetY: 1,
          shadowBlur: 3,
          shadowSpread: 0,
          shadowInset: false,
          opacity: 0.95
        },
        neon: {
          backgroundType: "solid",
          backgroundColor: "#001122",
          borderStyle: "solid",
          borderColor: "#00ffff",
          borderWidth: 2,
          borderRadius: 8,
          enableShadow: true,
          shadowType: "box",
          shadowColor: "rgba(0, 255, 255, 0.5)",
          shadowOffsetX: 0,
          shadowOffsetY: 0,
          shadowBlur: 20,
          shadowSpread: 2,
          shadowInset: false,
          opacity: 1
        },
        glass: {
          backgroundType: "solid",
          backgroundColor: "rgba(255, 255, 255, 0.1)",
          borderStyle: "solid",
          borderColor: "rgba(255, 255, 255, 0.2)",
          borderWidth: 1,
          borderRadius: 16,
          enableShadow: true,
          shadowType: "box",
          shadowColor: "rgba(0, 0, 0, 0.1)",
          shadowOffsetX: 4,
          shadowOffsetY: 8,
          shadowBlur: 32,
          shadowSpread: 0,
          shadowInset: false,
          opacity: 1
        },
        gradient: {
          backgroundType: "linear-gradient",
          gradientStart: "#667eea",
          gradientEnd: "#764ba2",
          gradientAngle: 135,
          borderStyle: "none",
          borderWidth: 0,
          borderRadius: 20,
          enableShadow: true,
          shadowType: "box",
          shadowColor: "rgba(102, 126, 234, 0.4)",
          shadowOffsetX: 4,
          shadowOffsetY: 8,
          shadowBlur: 20,
          shadowSpread: 0,
          shadowInset: false,
          opacity: 1
        }
      };

  const style = presetStyles[preset];
  if (style) {
    Object.keys(style).forEach(key => {
      updateStyle(key, style[key]);
    });
  }
};

// 复制当前组件样式
const copyCurrentStyle = () => {
  if (!props.selectedComponent?.style) {
    ElMessage.warning("当前组件没有样式可复制");
    return;
  }

  // 创建样式配置对象，只包含可复制的样式属性
  const styleConfig = {
    backgroundType: props.selectedComponent.style.backgroundType,
    backgroundColor: props.selectedComponent.style.backgroundColor,
    gradientStart: props.selectedComponent.style.gradientStart,
    gradientEnd: props.selectedComponent.style.gradientEnd,
    gradientAngle: props.selectedComponent.style.gradientAngle,
    gradientShape: props.selectedComponent.style.gradientShape,
    borderStyle: props.selectedComponent.style.borderStyle,
    borderColor: props.selectedComponent.style.borderColor,
    borderWidth: props.selectedComponent.style.borderWidth,
    borderRadius: props.selectedComponent.style.borderRadius,
    enableShadow: props.selectedComponent.style.enableShadow,
    shadowType: props.selectedComponent.style.shadowType,
    shadowColor: props.selectedComponent.style.shadowColor,
    shadowOffsetX: props.selectedComponent.style.shadowOffsetX,
    shadowOffsetY: props.selectedComponent.style.shadowOffsetY,
    shadowBlur: props.selectedComponent.style.shadowBlur,
    shadowSpread: props.selectedComponent.style.shadowSpread,
    shadowInset: props.selectedComponent.style.shadowInset,
    opacity: props.selectedComponent.style.opacity
  };

  // 移除undefined值
  const cleanConfig = Object.fromEntries(
    Object.entries(styleConfig).filter(([_, value]) => value !== undefined)
  );

  try {
    // 复制到剪贴板
    navigator.clipboard.writeText(JSON.stringify(cleanConfig, null, 2));

    console.log("样式配置已复制:", cleanConfig);
    ElMessage.success(
      `已复制样式配置 (${Object.keys(cleanConfig).length}个属性)`
    );
  } catch (error) {
    console.error("复制样式失败:", error);
    ElMessage.error("复制样式失败，请检查浏览器权限");
  }
};

// 粘贴样式功能
const pasteStyle = async () => {
  if (!props.selectedComponent) {
    ElMessage.warning("请先选择一个组件");
    return;
  }

  try {
    // 从剪贴板读取内容
    const clipboardText = await navigator.clipboard.readText();

    if (!clipboardText.trim()) {
      ElMessage.warning("剪贴板为空");
      return;
    }

    // 尝试解析JSON
    let styleConfig;
    try {
      styleConfig = JSON.parse(clipboardText);
    } catch (parseError) {
      ElMessage.error("剪贴板内容不是有效的样式配置");
      return;
    }

    // 验证是否是样式配置对象
    const validStyleKeys = [
      "backgroundType",
      "backgroundColor",
      "gradientStart",
      "gradientEnd",
      "gradientAngle",
      "gradientShape",
      "borderStyle",
      "borderColor",
      "borderWidth",
      "borderRadius",
      "enableShadow",
      "shadowType",
      "shadowColor",
      "shadowOffsetX",
      "shadowOffsetY",
      "shadowBlur",
      "shadowSpread",
      "shadowInset",
      "opacity"
    ];

    const hasValidKeys = Object.keys(styleConfig).some(key =>
      validStyleKeys.includes(key)
    );

    if (!hasValidKeys) {
      ElMessage.error("剪贴板内容不包含有效的样式配置");
      return;
    }

    // 应用样式配置
    let appliedCount = 0;
    Object.entries(styleConfig).forEach(([key, value]) => {
      if (
        validStyleKeys.includes(key) &&
        value !== null &&
        value !== undefined
      ) {
        updateStyle(key, value);
        appliedCount++;
      }
    });

    console.log("粘贴样式成功:", styleConfig);
    ElMessage.success(`样式粘贴成功 (应用${appliedCount}个属性)`);
  } catch (error) {
    console.error("粘贴样式失败:", error);
    ElMessage.error("粘贴失败，请检查浏览器权限或剪贴板内容");
  }
};

// 重置样式 (清空所有样式，恢复默认)
const resetStyle = () => {
  if (!props.selectedComponent) return;

  const defaultStyles = [
    // 背景相关
    "backgroundColor",
    "backgroundType",
    "backgroundImage",
    "backgroundRepeat",
    "backgroundSize",
    "backgroundPosition",
    "gradientStart",
    "gradientEnd",
    "gradientAngle",
    "gradientShape",
    // 边框相关
    "borderStyle",
    "borderColor",
    "borderWidth",
    "borderRadius",
    // 阴影相关
    "enableShadow",
    "shadowType",
    "shadowColor",
    "shadowOffsetX",
    "shadowOffsetY",
    "shadowBlur",
    "shadowSpread",
    "shadowInset",
    // 视觉效果
    "opacity",
    "blur",
    "brightness",
    "contrast",
    "saturate",
    "hueRotate",
    "invert",
    "sepia",
    "grayscale",
    // SVG样式
    "fill",
    "stroke",
    "strokeWidth",
    "strokeDasharray",
    "strokeLinecap",
    "strokeLinejoin",
    "fillOpacity",
    "strokeOpacity"
  ];

  defaultStyles.forEach(property => {
    updateStyle(property, null);
  });

  ElMessage.success("样式已重置");
};
</script>

<style scoped>
.appearance-style-config {
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

.unit {
  margin-left: 8px;
  font-size: 12px;
  color: #909399;
  font-weight: 500;
}

/* 样式预设网格布局 */
.preset-grid .el-button {
  width: 100%;
  border-radius: 8px;
  font-weight: 500;
  transition: all 0.3s ease;
}

.preset-grid .el-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.preset-grid .el-button--primary {
  background: linear-gradient(135deg, #409eff 0%, #1976d2 100%);
  border: none;
}

.preset-grid .el-button--success {
  background: linear-gradient(135deg, #67c23a 0%, #4caf50 100%);
  border: none;
}

.preset-grid .el-button--warning {
  background: linear-gradient(135deg, #e6a23c 0%, #ff9800 100%);
  border: none;
}

.preset-grid .el-button--danger {
  background: linear-gradient(135deg, #f56c6c 0%, #e53e3e 100%);
  border: none;
}

.preset-grid .el-button--info {
  background: linear-gradient(135deg, #909399 0%, #6c757d 100%);
  border: none;
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
