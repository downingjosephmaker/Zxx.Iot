<template>
  <div class="property-panel">
    <div class="panel-header">
      <h4>属性面板</h4>
      <el-button
        v-if="selectedComponent"
        link
        size="small"
        @click="clearSelectedComponent"
      >
        <el-icon><Close /></el-icon>
      </el-button>
    </div>

    <div class="panel-content">
      <div v-if="!selectedComponent" class="no-selection">
        <el-empty description="请选择一个组件来编辑属性" />
      </div>
      <div v-else class="property-form">
        <el-collapse v-model="activeCollapse" accordion>
          <!-- 基本信息 -->
          <el-collapse-item title="基本信息" name="basic">
            <el-form size="small" label-width="80px">
              <el-form-item label="组件名称">
                <el-input
                  :model-value="selectedComponent.name"
                  @input="updateProperty('name', $event)"
                  @change="updateComponentProperty"
                />
              </el-form-item>
              <el-form-item label="组件类型">
                <el-input :model-value="selectedComponent.type" disabled />
              </el-form-item>
              <el-form-item label="组件ID">
                <el-input :model-value="selectedComponent.id" disabled />
              </el-form-item>
            </el-form>
            <el-form size="small">
              <!-- 位置 -->
              <div class="property-section">
                <div class="section-title">位置</div>
                <el-row :gutter="8">
                  <el-col :span="12">
                    <el-form-item label="X坐标">
                      <el-input-number
                        :model-value="getDirectComponentX()"
                        @update:model-value="setComponentX"
                        :min="-9999"
                        :step="1"
                        controls-position="right"
                        size="small"
                      >
                        <template #suffix>
                          <span>px</span>
                        </template>
                      </el-input-number>
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="Y坐标">
                      <el-input-number
                        :model-value="getDirectComponentY()"
                        @update:model-value="setComponentY"
                        :min="-9999"
                        :step="1"
                        controls-position="right"
                        size="small"
                      >
                        <template #suffix>
                          <span>px</span>
                        </template>
                      </el-input-number>
                    </el-form-item>
                  </el-col>
                </el-row>
                <el-row :gutter="8" class="mt-2">
                  <el-col :span="8">
                    <el-button size="small" @click="adjustPosition('x', -1)">
                      <el-icon><ArrowLeft /></el-icon>
                      ←
                    </el-button>
                  </el-col>
                  <el-col :span="8">
                    <el-button size="small" @click="adjustPosition('y', -1)">
                      <el-icon><ArrowUp /></el-icon>
                      ↑
                    </el-button>
                  </el-col>
                  <el-col :span="8">
                    <el-button size="small" @click="adjustPosition('x', 1)">
                      <el-icon><ArrowRight /></el-icon>
                      →
                    </el-button>
                  </el-col>
                </el-row>
                <el-row :gutter="4" style="margin-top: 4px">
                  <el-col :span="8">
                    <el-button size="small" @click="adjustPosition('x', -10)">
                      <el-icon><DArrowLeft /></el-icon>
                      -10
                    </el-button>
                  </el-col>
                  <el-col :span="8">
                    <el-button size="small" @click="adjustPosition('y', 1)">
                      <el-icon><ArrowDown /></el-icon>
                      ↓
                    </el-button>
                  </el-col>
                  <el-col :span="8">
                    <el-button size="small" @click="adjustPosition('x', 10)">
                      <el-icon><DArrowRight /></el-icon>
                      +10
                    </el-button>
                  </el-col>
                </el-row>
              </div>

              <!-- 尺寸 -->
              <div class="property-section">
                <div class="section-title">尺寸</div>
                <el-row :gutter="8">
                  <el-col :span="12">
                    <el-form-item label="宽度">
                      <el-input-number
                        v-model="editableComponentWidth"
                        :min="1"
                        :step="1"
                        controls-position="right"
                        size="small"
                        @change="() => handleSizeChange()"
                      >
                        <template #suffix>
                          <span>px</span>
                        </template>
                      </el-input-number>
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="高度">
                      <el-input-number
                        v-model="editableComponentHeight"
                        :min="1"
                        :step="1"
                        controls-position="right"
                        size="small"
                        @change="() => handleSizeChange()"
                      >
                        <template #suffix>
                          <span>px</span>
                        </template>
                      </el-input-number>
                    </el-form-item>
                  </el-col>
                </el-row>
                <el-row :gutter="8" class="mt-2">
                  <el-col :span="12">
                    <el-form-item label="缩放比例">
                      <el-input-number
                        :model-value="getComponentScale()"
                        :min="0.1"
                        :max="10"
                        :step="0.1"
                        :precision="1"
                        controls-position="right"
                        size="small"
                        @input="handleScaleChange($event)"
                        @change="updateComponentTransform"
                      >
                        <template #suffix>
                          <span>倍</span>
                        </template>
                      </el-input-number>
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="保持比例">
                      <el-switch
                        :model-value="getComponentLockAspectRatio()"
                        @change="handleAspectRatioChange($event)"
                      />
                    </el-form-item>
                  </el-col>
                </el-row>
                <el-row :gutter="4" class="mt-2">
                  <el-col :span="6">
                    <el-button size="small" @click="scaleComponent(0.5)">
                      50%
                    </el-button>
                  </el-col>
                  <el-col :span="6">
                    <el-button size="small" @click="scaleComponent(1.0)">
                      100%
                    </el-button>
                  </el-col>
                  <el-col :span="6">
                    <el-button size="small" @click="scaleComponent(1.5)">
                      150%
                    </el-button>
                  </el-col>
                  <el-col :span="6">
                    <el-button size="small" @click="scaleComponent(2.0)">
                      200%
                    </el-button>
                  </el-col>
                </el-row>
                <el-row :gutter="4" class="mt-2">
                  <el-col :span="8">
                    <el-button size="small" @click="setSizePreset('small')">
                      小型
                    </el-button>
                  </el-col>
                  <el-col :span="8">
                    <el-button size="small" @click="setSizePreset('medium')">
                      中型
                    </el-button>
                  </el-col>
                  <el-col :span="8">
                    <el-button size="small" @click="setSizePreset('large')">
                      大型
                    </el-button>
                  </el-col>
                </el-row>
                <el-row :gutter="4" class="mt-2">
                  <el-col :span="12">
                    <el-button size="small" @click="fitToContent()">
                      <el-icon><Crop /></el-icon>
                      适应内容
                    </el-button>
                  </el-col>
                  <el-col :span="12">
                    <el-button size="small" @click="resetSize()">
                      <el-icon><RefreshLeft /></el-icon>
                      重置尺寸
                    </el-button>
                  </el-col>
                </el-row>
              </div>

              <!-- 变换 -->
              <div class="property-section">
                <div class="section-title">变换</div>
                <el-row :gutter="8" class="mt-2">
                  <el-col :span="24">
                    <el-form-item label="旋转角度">
                      <el-input-number
                        :model-value="getComponentRotation()"
                        :min="-360"
                        :max="360"
                        :step="1"
                        controls-position="right"
                        size="small"
                        @input="handleRotationChange($event)"
                        @change="updateComponentTransform"
                      >
                        <template #suffix>
                          <span>°</span>
                        </template>
                      </el-input-number>
                    </el-form-item>
                  </el-col>
                </el-row>
                <el-row :gutter="4" class="mt-2">
                  <el-col :span="6" class="mt-2">
                    <el-button size="small" @click="rotateComponent(-90)">
                      <el-icon><RefreshLeft /></el-icon>
                      -90°
                    </el-button>
                  </el-col>
                  <el-col :span="6">
                    <el-button size="small" @click="rotateComponent(-15)">
                      -15°
                    </el-button>
                  </el-col>
                  <el-col :span="6">
                    <el-button size="small" @click="rotateComponent(15)">
                      +15°
                    </el-button>
                  </el-col>
                  <el-col :span="6">
                    <el-button size="small" @click="rotateComponent(90)">
                      <el-icon><RefreshRight /></el-icon>
                      +90°
                    </el-button>
                  </el-col>
                </el-row>
                <el-row :gutter="4" class="mt-2">
                  <el-col :span="12">
                    <el-button size="small" @click="setRotation(0)">
                      <el-icon><Refresh /></el-icon>
                      重置旋转
                    </el-button>
                  </el-col>
                  <el-col :span="12">
                    <el-button size="small" @click="setRotation(180)">
                      <el-icon><Sort /></el-icon>
                      翻转180°
                    </el-button>
                  </el-col>
                </el-row>
                <el-row :gutter="8" class="mt-2">
                  <el-col :span="12">
                    <el-form-item label="翻转水平">
                      <el-switch
                        :model-value="getComponentFlipHorizontal()"
                        @change="handleFlipHorizontalChange($event)"
                      />
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="翻转垂直">
                      <el-switch
                        :model-value="getComponentFlipVertical()"
                        @change="handleFlipVerticalChange($event)"
                      />
                    </el-form-item>
                  </el-col>
                </el-row>
                <el-row :gutter="4" class="mt-2">
                  <el-col :span="12">
                    <el-button size="small" @click="toggleFlip('horizontal')">
                      <el-icon><Sort /></el-icon>
                      水平翻转
                    </el-button>
                  </el-col>
                  <el-col :span="12">
                    <el-button size="small" @click="toggleFlip('vertical')">
                      <el-icon><Sort /></el-icon>
                      垂直翻转
                    </el-button>
                  </el-col>
                </el-row>
                <el-row :gutter="8" class="mt-2">
                  <el-col :span="12">
                    <el-form-item label="倾斜X">
                      <el-input-number
                        :model-value="getComponentSkewX()"
                        :min="-45"
                        :max="45"
                        :step="1"
                        controls-position="right"
                        size="small"
                        @input="handleSkewXChange($event)"
                        @change="updateComponentTransform"
                      >
                        <template #suffix>
                          <span>°</span>
                        </template>
                      </el-input-number>
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="倾斜Y">
                      <el-input-number
                        :model-value="getComponentSkewY()"
                        :min="-45"
                        :max="45"
                        :step="1"
                        controls-position="right"
                        size="small"
                        @input="handleSkewYChange($event)"
                        @change="updateComponentTransform"
                      >
                        <template #suffix>
                          <span>°</span>
                        </template>
                      </el-input-number>
                    </el-form-item>
                  </el-col>
                </el-row>
                <el-row :gutter="8" class="mt-2">
                  <el-col :span="12">
                    <el-form-item label="变换原点X">
                      <el-select
                        :model-value="getComponentOriginX()"
                        size="small"
                        @change="handleOriginXChange($event)"
                      >
                        <el-option label="左" value="left" />
                        <el-option label="中" value="center" />
                        <el-option label="右" value="right" />
                      </el-select>
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="变换原点Y">
                      <el-select
                        :model-value="getComponentOriginY()"
                        size="small"
                        @change="handleOriginYChange($event)"
                      >
                        <el-option label="上" value="top" />
                        <el-option label="中" value="center" />
                        <el-option label="下" value="bottom" />
                      </el-select>
                    </el-form-item>
                  </el-col>
                </el-row>
                <el-row :gutter="4" class="mt-2">
                  <el-col :span="8">
                    <el-button size="small" @click="resetFlips()">
                      <el-icon><Refresh /></el-icon>
                      重置翻转
                    </el-button>
                  </el-col>
                  <el-col :span="8">
                    <el-button
                      size="small"
                      type="warning"
                      @click="resetTransforms()"
                    >
                      <el-icon><RefreshLeft /></el-icon>
                      重置变换
                    </el-button>
                  </el-col>
                  <el-col :span="8">
                    <el-button
                      size="small"
                      type="success"
                      @click="copyTransform()"
                    >
                      <el-icon><CopyDocument /></el-icon>
                      复制变换
                    </el-button>
                  </el-col>
                </el-row>
              </div>

              <!-- 布局控制（仅对容器组件显示） -->
              <div v-if="isContainerComponent()" class="property-section">
                <div class="section-title">布局</div>
                <el-row :gutter="8">
                  <el-col :span="12">
                    <el-form-item label="内边距">
                      <el-input-number
                        :model-value="getComponentPadding()"
                        :min="0"
                        :max="50"
                        :step="1"
                        controls-position="right"
                        @input="updateProperty('padding', $event)"
                        @change="updateComponentTransform"
                      >
                        <template #suffix>
                          <span>px</span>
                        </template>
                      </el-input-number>
                    </el-form-item>
                  </el-col>
                  <el-col :span="12">
                    <el-form-item label="外边距">
                      <el-input-number
                        :model-value="getComponentMargin()"
                        :min="0"
                        :max="50"
                        :step="1"
                        controls-position="right"
                        @input="updateProperty('margin', $event)"
                        @change="updateComponentTransform"
                      >
                        <template #suffix>
                          <span>px</span>
                        </template>
                      </el-input-number>
                    </el-form-item>
                  </el-col>
                </el-row>
                <el-form-item label="溢出处理">
                  <el-select
                    :model-value="getComponentOverflow()"
                    @change="updateProperty('overflow', $event)"
                  >
                    <el-option label="可见" value="visible" />
                    <el-option label="隐藏" value="hidden" />
                    <el-option label="滚动" value="scroll" />
                    <el-option label="自动" value="auto" />
                  </el-select>
                </el-form-item>
              </div>

              <!-- 对齐控制 -->
              <div class="property-section">
                <div class="section-title">对齐方式</div>
                <el-row :gutter="4">
                  <el-col :span="8">
                    <el-button size="small" @click="alignComponent('left')">
                      <el-icon><Back /></el-icon>
                      左对齐
                    </el-button>
                  </el-col>
                  <el-col :span="8">
                    <el-button size="small" @click="alignComponent('center')">
                      <el-icon><Operation /></el-icon>
                      居中
                    </el-button>
                  </el-col>
                  <el-col :span="8">
                    <el-button size="small" @click="alignComponent('right')">
                      <el-icon><Right /></el-icon>
                      右对齐
                    </el-button>
                  </el-col>
                </el-row>
                <el-row :gutter="4" style="margin-top: 8px">
                  <el-col :span="8">
                    <el-button size="small" @click="alignComponent('top')">
                      <el-icon><Top /></el-icon>
                      顶对齐
                    </el-button>
                  </el-col>
                  <el-col :span="8">
                    <el-button size="small" @click="alignComponent('middle')">
                      <el-icon><Operation /></el-icon>
                      垂直居中
                    </el-button>
                  </el-col>
                  <el-col :span="8">
                    <el-button size="small" @click="alignComponent('bottom')">
                      <el-icon><Bottom /></el-icon>
                      底对齐
                    </el-button>
                  </el-col>
                </el-row>
              </div>
            </el-form>
          </el-collapse-item>

          <!-- 文字属性 -->
          <el-collapse-item v-if="hasTextProperty" title="文字属性" name="text">
            <el-form size="small" label-width="80px">
              <el-form-item label="显示文字">
                <!-- LED 和文本卡片组件支持多行文本 -->
                <el-input
                  v-if="selectedComponent?.type === 'led-display' || selectedComponent?.type === 'text-card'"
                  type="textarea"
                  :model-value="getComponentText()"
                  placeholder="请输入文字内容（支持换行）"
                  :rows="3"
                  @input="updateText('text', $event)"
                />
                <!-- 其他组件单行文本 -->
                <el-input
                  v-else
                  :model-value="getComponentText()"
                  placeholder="请输入文字内容"
                  @input="updateText('text', $event)"
                />
              </el-form-item>
              <el-form-item label="字体大小">
                <el-input-number
                  :model-value="getComponentFontSize()"
                  :min="8"
                  :max="100"
                  :step="1"
                  controls-position="right"
                  @input="updateText('fontSize', $event)"
                >
                  <template #suffix>
                    <span>px</span>
                  </template>
                </el-input-number>
              </el-form-item>
              <el-form-item label="字体粗细">
                <el-select
                  :model-value="getComponentFontWeight()"
                  @change="updateText('fontWeight', $event)"
                >
                  <el-option label="正常" value="normal" />
                  <el-option label="粗体" value="bold" />
                  <el-option label="细体" value="lighter" />
                  <el-option label="特粗" value="bolder" />
                  <el-option label="100" value="100" />
                  <el-option label="200" value="200" />
                  <el-option label="300" value="300" />
                  <el-option label="400" value="400" />
                  <el-option label="500" value="500" />
                  <el-option label="600" value="600" />
                  <el-option label="700" value="700" />
                  <el-option label="800" value="800" />
                  <el-option label="900" value="900" />
                </el-select>
              </el-form-item>
              <el-form-item label="水平对齐">
                <el-select
                  :model-value="getComponentTextAlign()"
                  @change="updateText('textAlign', $event)"
                >
                  <el-option label="左对齐" value="left" />
                  <el-option label="居中" value="center" />
                  <el-option label="右对齐" value="right" />
                  <el-option label="两端对齐" value="justify" />
                </el-select>
              </el-form-item>
              <el-form-item label="垂直对齐">
                <el-select
                  :model-value="getComponentVerticalAlign()"
                  @change="updateText('verticalAlign', $event)"
                >
                  <el-option label="顶部对齐" value="top" />
                  <el-option label="居中对齐" value="middle" />
                  <el-option label="底部对齐" value="bottom" />
                  <!-- 基线对齐对文本卡片等多行文本组件意义不大,已移除 -->
                </el-select>
              </el-form-item>
              <el-form-item label="文字颜色">
                <el-color-picker
                  :model-value="getComponentColor()"
                  show-alpha
                  @change="updateText('color', $event)"
                />
              </el-form-item>
              <el-form-item label="文字装饰">
                <el-select
                  :model-value="getComponentTextDecoration()"
                  @change="updateText('textDecoration', $event)"
                >
                  <el-option label="无装饰" value="none" />
                  <el-option label="下划线" value="underline" />
                  <el-option label="上划线" value="overline" />
                  <el-option label="删除线" value="line-through" />
                </el-select>
              </el-form-item>
            </el-form>
          </el-collapse-item>

          <!-- LED 特效配置 (独立折叠项) -->
          <el-collapse-item
            v-if="selectedComponent?.type === 'led-display'"
            title="💡 LED 特效"
            name="led-effects"
          >
            <el-form size="small" label-width="80px">
              <!-- 发光效果 -->
              <el-form-item label="发光效果">
                <el-switch
                  :model-value="getLedGlowEffect()"
                  @change="updateText('glowEffect', $event)"
                />
              </el-form-item>

              <el-form-item v-if="getLedGlowEffect()" label="发光强度">
                <el-select
                  :model-value="getLedGlowIntensity()"
                  @change="updateText('glowIntensity', $event)"
                >
                  <el-option label="正常" value="normal" />
                  <el-option label="强烈" value="strong" />
                </el-select>
              </el-form-item>

              <!-- 动画效果 -->
              <el-form-item label="动画效果">
                <el-select
                  :model-value="getLedAnimation()"
                  @change="updateText('animation', $event)"
                >
                  <el-option label="无动画" value="none" />
                  <el-option label="闪烁" value="blink" />
                  <el-option label="滚动" value="scroll" />
                  <el-option label="波浪" value="wave" />
                  <el-option label="打字机" value="typewriter" />
                </el-select>
              </el-form-item>

              <el-form-item v-if="getLedAnimation() !== 'none'" label="动画速度">
                <el-input-number
                  :model-value="getLedAnimationSpeed()"
                  :min="100"
                  :max="5000"
                  :step="100"
                  controls-position="right"
                  @change="updateText('animationSpeed', $event)"
                >
                  <template #suffix>
                    <span>ms</span>
                  </template>
                </el-input-number>
              </el-form-item>

              <!-- 文本格式化 -->
              <el-form-item label="格式化">
                <el-select
                  :model-value="getLedFormat()"
                  @change="updateText('format', $event)"
                >
                  <el-option label="无格式" value="none" />
                  <el-option label="时间 (HH:mm:ss)" value="time" />
                  <el-option label="日期 (YYYY-MM-DD)" value="date" />
                  <el-option label="数字格式化" value="number" />
                </el-select>
              </el-form-item>

              <!-- 多行模式 -->
              <el-form-item label="多行模式">
                <el-switch
                  :model-value="getLedMultiLine()"
                  @change="updateText('multiLine', $event)"
                />
              </el-form-item>

              <el-form-item v-if="getLedMultiLine()" label="行高倍数">
                <el-input-number
                  :model-value="getLedLineHeight()"
                  :min="1"
                  :max="3"
                  :step="0.1"
                  :precision="1"
                  controls-position="right"
                  @change="updateText('lineHeight', $event)"
                />
              </el-form-item>

              <el-form-item v-if="getLedMultiLine()" label="最大行数">
                <el-input-number
                  :model-value="getLedMaxLines()"
                  :min="1"
                  :max="10"
                  :step="1"
                  controls-position="right"
                  @change="updateText('maxLines', $event)"
                />
              </el-form-item>

              <!-- 背景颜色 -->
              <el-form-item label="背景颜色">
                <el-color-picker
                  :model-value="getLedBackgroundColor()"
                  show-alpha
                  @change="updateText('backgroundColor', $event)"
                />
              </el-form-item>
            </el-form>
          </el-collapse-item>

          <!-- 📝 文本卡片配置 (独立折叠项) -->
          <el-collapse-item
            v-if="selectedComponent?.type === 'text-card'"
            title="📝 卡片配置"
            name="text-card-config"
          >
            <el-form size="small" label-width="80px">
              <!-- 多行模式 -->
              <el-form-item label="多行模式">
                <el-switch
                  :model-value="getTextCardMultiLine()"
                  @change="updateTextCardConfig('multiLine', $event)"
                />
              </el-form-item>

              <el-form-item v-if="getTextCardMultiLine()" label="最大行数">
                <el-input-number
                  :model-value="getTextCardMaxLines()"
                  :min="1"
                  :max="20"
                  :step="1"
                  controls-position="right"
                  @change="updateTextCardConfig('maxLines', $event)"
                />
              </el-form-item>

              <el-form-item v-if="getTextCardMultiLine()" label="行高倍数">
                <el-input-number
                  :model-value="getTextCardLineHeight()"
                  :min="1"
                  :max="3"
                  :step="0.1"
                  :precision="1"
                  controls-position="right"
                  @change="updateTextCardConfig('lineHeight', $event)"
                />
              </el-form-item>
            </el-form>
          </el-collapse-item>

          <!-- 外观样式 -->
          <el-collapse-item title="外观样式" name="style">
            <AppearanceStyleConfig
              :selectedComponent="selectedComponent"
              @update-style="updateStyle"
            />
          </el-collapse-item>

          <!-- 图表配置 -->
          <el-collapse-item
            v-if="hasChartProperty"
            title="图表配置"
            name="chart"
          >
            <el-form size="small" label-width="80px">
              <!-- 基本配置 -->
              <div class="property-section">
                <div class="section-title">基本配置</div>
                <el-form-item label="图表类型">
                  <el-select
                    :model-value="selectedComponent.chartConfig?.type || 'line'"
                    @change="updateChartConfig('type', $event)"
                  >
                    <el-option label="折线图" value="line" />
                    <el-option label="柱状图" value="bar" />
                    <el-option label="饼图" value="pie" />
                    <el-option label="面积图" value="area" />
                    <el-option label="散点图" value="scatter" />
                  </el-select>
                </el-form-item>
                <el-form-item label="图表标题">
                  <el-input
                    :model-value="selectedComponent.chartConfig?.title || ''"
                    placeholder="请输入图表标题"
                    @input="updateChartConfig('title', $event)"
                  />
                </el-form-item>
                <el-form-item label="显示图例">
                  <el-switch
                    :model-value="
                      selectedComponent.chartConfig?.showLegend !== false
                    "
                    @change="updateChartConfig('showLegend', $event)"
                  />
                </el-form-item>
              </div>

              <!-- 数据源配置 -->
              <div class="property-section">
                <div class="section-title">数据源</div>
                <el-form-item label="数据源类型">
                  <el-select
                    :model-value="
                      selectedComponent.chartConfig?.dataSource || 'static'
                    "
                    @change="updateChartConfig('dataSource', $event)"
                  >
                    <el-option label="静态数据" value="static" />
                    <el-option label="数据集" value="dataset" />
                    <el-option label="MQTT订阅" value="mqtt" />
                  </el-select>
                </el-form-item>

                <!-- 数据集配置 -->
                <div
                  v-if="selectedComponent.chartConfig?.dataSource === 'dataset'"
                >
                  <el-form-item label="选择数据集">
                    <el-button
                      type="primary"
                      size="small"
                      @click="openDatasetDialog"
                    >
                      <el-icon><Coin /></el-icon>
                      配置数据集
                    </el-button>
                  </el-form-item>
                  <div
                    v-if="selectedComponent.dataBinding"
                    class="binding-info"
                  >
                    <el-tag type="success" size="small"> 已绑定数据集 </el-tag>
                  </div>
                </div>

                <!-- MQTT配置 -->
                <div
                  v-if="selectedComponent.chartConfig?.dataSource === 'mqtt'"
                >
                  <el-form-item label="MQTT主题">
                    <el-input
                      :model-value="
                        selectedComponent.chartConfig?.mqttConfig?.topic || ''
                      "
                      placeholder="sensors/data"
                      @input="updateMqttConfig('topic', $event)"
                    />
                  </el-form-item>
                  <el-form-item label="数据路径">
                    <el-input
                      :model-value="
                        selectedComponent.chartConfig?.mqttConfig?.dataPath ||
                        ''
                      "
                      placeholder="value"
                      @input="updateMqttConfig('dataPath', $event)"
                    />
                  </el-form-item>
                </div>
              </div>

              <!-- 样式配置 -->
              <div class="property-section">
                <div class="section-title">图表样式</div>
                <el-form-item label="主题">
                  <el-select
                    :model-value="
                      selectedComponent.chartConfig?.theme || 'default'
                    "
                    @change="updateChartConfig('theme', $event)"
                  >
                    <el-option label="默认" value="default" />
                    <el-option label="深色" value="dark" />
                    <el-option label="浅色" value="light" />
                  </el-select>
                </el-form-item>
                <el-form-item label="刷新间隔">
                  <el-input-number
                    :model-value="
                      selectedComponent.chartConfig?.refreshInterval || 5000
                    "
                    :min="1000"
                    :step="1000"
                    controls-position="right"
                    @input="updateChartConfig('refreshInterval', $event)"
                  />
                  <span class="unit">ms</span>
                </el-form-item>
              </div>
            </el-form>
          </el-collapse-item>
        </el-collapse>
      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from "vue";
import { ElMessage } from "element-plus";
import AppearanceStyleConfig from "./AppearanceStyleConfig.vue";
import {
  Box,
  Close,
  Back,
  Right,
  Top,
  Bottom,
  Operation,
  CopyDocument,
  Delete,
  Position,
  FullScreen,
  RefreshRight,
  ZoomIn,
  ArrowLeft,
  ArrowUp,
  ArrowRight,
  ArrowDown,
  DArrowLeft,
  DArrowRight,
  Crop,
  RefreshLeft,
  Refresh,
  Sort,
  Setting,
  Coin
} from "@element-plus/icons-vue";
// 组件属性
interface Props {
  selectedComponent?: any;
  deviceList?: any[];
}

const props = withDefaults(defineProps<Props>(), {
  selectedComponent: null,
  deviceList: () => []
});

// 组件事件
const emit = defineEmits<{
  "update-property": [property: string, value: any];
  "update-position": [axis: string, value: number];
  "update-size": [dimension: string, value: number];
  "update-style": [property: string, value: any];
  "update-component-property": [key: string, value: any];
  "update-component-transform": [];
  "update-text": [property: string, value: any];
  "align-component": [alignment: string];
  "clear-selected-component": [];
  "update-chart-config": [property: string, value: any];
  "open-dataset-dialog": [];
}>();

// 响应式数据
const activeCollapse = ref("basic");

// 计算属性

const hasTextProperty = computed(() => {
  // 检查组件是否有文字相关属性
  const component = props.selectedComponent;
  if (!component) return false;

  // 图表组件通常不需要文字属性面板，因为它们有自己的配置面板
  const chartTypes = [
    "chart",
    "line-chart",
    "bar-chart",
    "pie-chart",
    "area-chart",
    "scatter-chart"
  ];
  if (chartTypes.includes(component.type)) return false;

  // 其他组件显示文字属性面板
  return true;
});

const hasChartProperty = computed(() => {
  // 检查组件是否为图表类型
  const component = props.selectedComponent;
  if (!component) return false;

  // 根据组件类型判断是否显示图表配置
  return [
    "chart",
    "line-chart",
    "bar-chart",
    "pie-chart",
    "area-chart",
    "scatter-chart"
  ].includes(component.type);
});

// 画板配置已移至画板内部工具栏,不再需要属性面板配置

// 改为计算属性以确保响应性
const componentX = computed(() => {
  if (!props.selectedComponent) return 0;
  const component = props.selectedComponent;

  // 尝试多种可能的数据结构，并添加updated属性作为响应式触发器
  const x =
    component.position?.x ??
    component.x ??
    component.transform?.x ??
    component.style?.left ??
    component.left ??
    0;

  // 强制访问所有可能的响应式属性来确保依赖追踪
  const positionX = component.position?.x;
  const directX = component.x;
  const updated = component.updated;

  console.log("计算组件X坐标:", x, "组件数据:", {
    id: component.id,
    positionX: positionX,
    x: directX,
    updated: updated
  });
  return x;
});

const componentY = computed(() => {
  if (!props.selectedComponent) return 0;
  const component = props.selectedComponent;

  // 尝试多种可能的数据结构，并添加lastPositionUpdate属性作为响应式触发器
  const y =
    component.position?.y ??
    component.y ??
    component.transform?.y ??
    component.style?.top ??
    component.top ??
    0;

  // 强制访问所有可能的响应式属性来确保依赖追踪
  const positionY = component.position?.y;
  const directY = component.y;
  const updated = component.updated;

  console.log("计算组件Y坐标:", y, "组件数据:", {
    id: component.id,
    positionY: positionY,
    y: directY,
    updated: updated
  });
  return y;
});

// 保持向后兼容的函数
const getComponentX = () => componentX.value;
const getComponentY = () => componentY.value;

// 创建响应式的位置追踪
const forceUpdateTrigger = ref(0);

// 直接获取组件位置的新函数（绕过复杂的计算属性）
const getDirectComponentX = () => {
  if (!props.selectedComponent) return 0;
  const component = props.selectedComponent;
  // 访问强制更新触发器
  void forceUpdateTrigger.value;
  // 强制访问组件的updated属性来触发重新渲染
  void component.updated;

  const x = component.position?.x ?? component.x ?? 0;
  console.log("直接获取X坐标:", x, "组件ID:", component.id);
  return x;
};

const getDirectComponentY = () => {
  if (!props.selectedComponent) return 0;
  const component = props.selectedComponent;
  // 访问强制更新触发器
  void forceUpdateTrigger.value;
  // 强制访问组件的updated属性来触发重新渲染
  void component.updated;

  const y = component.position?.y ?? component.y ?? 0;
  console.log("直接获取Y坐标:", y, "组件ID:", component.id);
  return y;
};

const setComponentX = (value: number) => {
  console.log("设置X坐标:", value);
  updatePosition("x", value);
};

const setComponentY = (value: number) => {
  console.log("设置Y坐标:", value);
  updatePosition("y", value);
};

// 监听组件的updated属性变化，强制刷新位置显示
watch(
  () => props.selectedComponent?.updated,
  () => {
    if (props.selectedComponent) {
      console.log("检测到组件位置更新，强制刷新位置显示");
      forceUpdateTrigger.value++;
    }
  },
  { immediate: true }
);

// 改为计算属性以确保响应性
const componentWidth = computed(() => {
  if (!props.selectedComponent) return 60;
  const component = props.selectedComponent;

  // 尝试多种可能的数据结构，并添加lastSizeUpdate属性作为响应式触发器
  let width =
    component.size?.width ??
    component.width ??
    component.transform?.width ??
    component.w ??
    60;

  // 如果从 style 中获取值，需要转换为数字
  if (width === 60 && component.style?.width) {
    const styleWidth = parseFloat(component.style.width);
    if (!isNaN(styleWidth)) {
      width = styleWidth;
    }
  }

  // 显式访问updated属性来确保响应式更新
  void component.updated;

  console.log("计算组件宽度:", width, "组件数据:", {
    id: component.id,
    sizeWidth: component.size?.width,
    width: component.width,
    updated: component.updated
  });
  return width;
});

const componentHeight = computed(() => {
  if (!props.selectedComponent) return 60;
  const component = props.selectedComponent;

  // 尝试多种可能的数据结构，并添加lastSizeUpdate属性作为响应式触发器
  let height =
    component.size?.height ??
    component.height ??
    component.transform?.height ??
    component.h ??
    60;

  // 如果从 style 中获取值，需要转换为数字
  if (height === 60 && component.style?.height) {
    const styleHeight = parseFloat(component.style.height);
    if (!isNaN(styleHeight)) {
      height = styleHeight;
    }
  }

  // 显式访问updated属性来确保响应式更新
  void component.updated;

  console.log("计算组件高度:", height, "组件数据:", {
    id: component.id,
    sizeHeight: component.size?.height,
    height: component.height,
    updated: component.updated
  });
  return height;
});

// 保持向后兼容的函数
const getComponentWidth = () => componentWidth.value;
const getComponentHeight = () => componentHeight.value;

// 创建本地响应式变量用于v-model绑定
const localComponentX = ref(0);
const localComponentY = ref(0);
const localComponentWidth = ref(60);
const localComponentHeight = ref(60);

// 添加调试信息
console.log("初始化本地变量:", {
  localComponentX: localComponentX.value,
  localComponentY: localComponentY.value,
  localComponentWidth: localComponentWidth.value,
  localComponentHeight: localComponentHeight.value
});

// 监听组件变化，同步到本地变量
watch(
  componentX,
  newValue => {
    console.log("同步X坐标到本地变量:", newValue);
    localComponentX.value = newValue;
  },
  { immediate: true }
);

watch(
  componentY,
  newValue => {
    console.log("同步Y坐标到本地变量:", newValue);
    localComponentY.value = newValue;
  },
  { immediate: true }
);

watch(
  componentWidth,
  newValue => {
    console.log("同步宽度到本地变量:", newValue);
    localComponentWidth.value = newValue;
  },
  { immediate: true }
);

watch(
  componentHeight,
  newValue => {
    console.log("同步高度到本地变量:", newValue);
    localComponentHeight.value = newValue;
  },
  { immediate: true }
);

// 处理位置变化
const handlePositionChange = (axis: string, value: number) => {
  console.log(`位置变化: ${axis} = ${value}`);
  updatePosition(axis, value);
};

// 创建可编辑的计算属性，使用getter和setter
const editableComponentX = computed({
  get() {
    const value = componentX.value;
    console.log("获取X坐标值:", value);
    return value;
  },
  set(value: number) {
    console.log("设置X坐标值:", value);
    updatePosition("x", value);
  }
});

const editableComponentY = computed({
  get() {
    const value = componentY.value;
    console.log("获取Y坐标值:", value);
    return value;
  },
  set(value: number) {
    console.log("设置Y坐标值:", value);
    updatePosition("y", value);
  }
});

const editableComponentWidth = computed({
  get() {
    const value = componentWidth.value;
    console.log("获取宽度值:", value);
    return value;
  },
  set(value: number) {
    console.log("设置宽度值:", value);
    updateSize("width", value);
  }
});

const editableComponentHeight = computed({
  get() {
    const value = componentHeight.value;
    console.log("获取高度值:", value);
    return value;
  },
  set(value: number) {
    console.log("设置高度值:", value);
    updateSize("height", value);
  }
});

// 获取变换属性的方法
const getComponentRotation = () => {
  if (!props.selectedComponent) return 0;
  const component = props.selectedComponent;

  // 尝试多种可能的数据结构
  return (
    component.rotation ??
    component.transform?.rotation ??
    component.rotate ??
    component.style?.transform?.match(/rotate\((-?\d+(?:\.\d+)?)deg\)/)?.[1] ??
    0
  );
};

const getComponentScale = () => {
  if (!props.selectedComponent) return 1;
  const component = props.selectedComponent;

  // 尝试多种可能的数据结构
  return (
    component.scale ??
    component.transform?.scale ??
    component.scaleX ??
    component.style?.transform?.match(/scale\((-?\d+(?:\.\d+)?)\)/)?.[1] ??
    1
  );
};

const getComponentFlipHorizontal = () => {
  if (!props.selectedComponent) return false;
  const component = props.selectedComponent;

  // 尝试多种可能的数据结构
  return (
    component.flipHorizontal ??
    component.transform?.flipHorizontal ??
    component.flipX ??
    false
  );
};

const getComponentFlipVertical = () => {
  if (!props.selectedComponent) return false;
  const component = props.selectedComponent;

  // 尝试多种可能的数据结构
  return (
    component.flipVertical ??
    component.transform?.flipVertical ??
    component.flipY ??
    false
  );
};

const getComponentLockAspectRatio = () => {
  if (!props.selectedComponent) return false;
  const component = props.selectedComponent;

  return (
    component.lockAspectRatio ??
    component.aspectRatio ??
    component.keepRatio ??
    false
  );
};

const getComponentUniformScale = () => {
  if (!props.selectedComponent) return false;
  const component = props.selectedComponent;

  return component.uniformScale ?? component.proportionalScale ?? false;
};

const getComponentSkewX = () => {
  if (!props.selectedComponent) return 0;
  const component = props.selectedComponent;

  return (
    component.skewX ??
    component.transform?.skewX ??
    component.style?.transform?.match(/skewX\((-?\d+(?:\.\d+)?)deg\)/)?.[1] ??
    0
  );
};

const getComponentSkewY = () => {
  if (!props.selectedComponent) return 0;
  const component = props.selectedComponent;

  return (
    component.skewY ??
    component.transform?.skewY ??
    component.style?.transform?.match(/skewY\((-?\d+(?:\.\d+)?)deg\)/)?.[1] ??
    0
  );
};

const getComponentOriginX = () => {
  if (!props.selectedComponent) return "center";
  const component = props.selectedComponent;

  return (
    component.originX ??
    component.transformOriginX ??
    component.style?.transformOriginX ??
    "center"
  );
};

const getComponentOriginY = () => {
  if (!props.selectedComponent) return "center";
  const component = props.selectedComponent;

  return (
    component.originY ??
    component.transformOriginY ??
    component.style?.transformOriginY ??
    "center"
  );
};

// 获取布局属性的方法
const getComponentPadding = () => {
  if (!props.selectedComponent) return 0;
  const component = props.selectedComponent;

  return component.padding ?? component.style?.padding ?? 0;
};

const getComponentMargin = () => {
  if (!props.selectedComponent) return 0;
  const component = props.selectedComponent;

  return component.margin ?? component.style?.margin ?? 0;
};

const getComponentOverflow = () => {
  if (!props.selectedComponent) return "visible";
  const component = props.selectedComponent;

  return component.overflow ?? component.style?.overflow ?? "visible";
};

// 方法
const updateProperty = (property: string, value: any) => {
  emit("update-property", property, value);
};

const updatePosition = (axis: string, value: number) => {
  emit("update-position", axis, value);
};

const updateSize = (dimension: string, value: number) => {
  emit("update-size", dimension, value);
};

const updateStyle = (property: string, value: any) => {
  console.log("PropertyPanel updateStyle 调用:", {
    property: property,
    value: value,
    componentId: props.selectedComponent?.id
  });
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
      updateStyle("backgroundColor", "transparent");
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
      // 渐变：清除纯色和图片属性，设置默认渐变值
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
      // 图片：清除纯色和渐变属性
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
    updateStyle("backgroundColor", "transparent");
  } else {
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

        // 如果透明度为0，设置为1（完全不透明）
        if (alpha === 0) {
          const newColor = `rgba(${r}, ${g}, ${b}, 1)`;
          console.log("修正透明度为1:", newColor);
          updateStyle("backgroundColor", newColor);
          return;
        }
      }
    }

    updateStyle("backgroundColor", value);
  }
};

// 获取组件的文本内容，特殊处理按钮组件和 LED 组件
const getComponentText = () => {
  if (!props.selectedComponent) return '';

  // 🔲 按钮组件的文本存储在 properties.text 中
  if (props.selectedComponent.type === 'button') {
    return props.selectedComponent.properties?.text || '';
  }

  // 💡 LED 显示组件的文本存储在 properties.text 中
  if (props.selectedComponent.type === 'led-display') {
    return props.selectedComponent.properties?.text || '88:88:88';
  }

  // 其他组件的文本存储在 text 属性中
  return props.selectedComponent.text || '';
};

// 获取组件的字体大小
const getComponentFontSize = () => {
  if (!props.selectedComponent) return 14;

  if (props.selectedComponent.type === 'button') {
    return props.selectedComponent.properties?.fontSize || 14;
  }

  // 💡 LED 显示组件
  if (props.selectedComponent.type === 'led-display') {
    return props.selectedComponent.properties?.fontSize || 42;
  }

  return props.selectedComponent.fontSize || 14;
};

// 获取组件的字体粗细
const getComponentFontWeight = () => {
  if (!props.selectedComponent) return 'normal';

  if (props.selectedComponent.type === 'button') {
    return props.selectedComponent.properties?.fontWeight || 'normal';
  }

  // 💡 LED 显示组件
  if (props.selectedComponent.type === 'led-display') {
    return props.selectedComponent.properties?.fontWeight || 'bold';
  }

  return props.selectedComponent.fontWeight || 'normal';
};

// 获取组件的文本对齐
const getComponentTextAlign = () => {
  if (!props.selectedComponent) return 'center';

  if (props.selectedComponent.type === 'button') {
    return props.selectedComponent.properties?.textAlign || 'center';
  }

  return props.selectedComponent.textAlign || 'center';
};

// 获取组件的文字颜色
const getComponentColor = () => {
  if (!props.selectedComponent) return '#303133';

  if (props.selectedComponent.type === 'button') {
    return props.selectedComponent.properties?.color || '#ffffff';
  }

  // 💡 LED 显示组件
  if (props.selectedComponent.type === 'led-display') {
    return props.selectedComponent.properties?.color || '#ff3333';
  }

  return props.selectedComponent.color || '#303133';
};

// 🔲 获取组件垂直对齐
const getComponentVerticalAlign = () => {
  if (!props.selectedComponent) return 'middle';

  if (props.selectedComponent.type === 'button') {
    return props.selectedComponent.properties?.verticalAlign || 'middle';
  }

  return props.selectedComponent.verticalAlign || 'middle';
};

// 🔲 获取组件文字装饰
const getComponentTextDecoration = () => {
  if (!props.selectedComponent) return 'none';

  if (props.selectedComponent.type === 'button') {
    return props.selectedComponent.properties?.textDecoration || 'none';
  }

  return props.selectedComponent.textDecoration || 'none';
};

// 💡 LED 组件专属 getter 函数
const getLedGlowEffect = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'led-display') return true;
  return props.selectedComponent.properties?.glowEffect !== undefined
    ? props.selectedComponent.properties.glowEffect
    : true;
};

const getLedGlowIntensity = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'led-display') return 'strong';
  return props.selectedComponent.properties?.glowIntensity || 'strong';
};

const getLedAnimation = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'led-display') return 'none';
  return props.selectedComponent.properties?.animation || 'none';
};

const getLedAnimationSpeed = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'led-display') return 1000;
  return props.selectedComponent.properties?.animationSpeed || 1000;
};

const getLedFormat = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'led-display') return 'none';
  return props.selectedComponent.properties?.format || 'none';
};

const getLedMultiLine = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'led-display') return false;
  return props.selectedComponent.properties?.multiLine || false;
};

const getLedLineHeight = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'led-display') return 1.2;
  return props.selectedComponent.properties?.lineHeight || 1.2;
};

const getLedMaxLines = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'led-display') return 3;
  return props.selectedComponent.properties?.maxLines || 3;
};

const getLedBackgroundColor = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'led-display') return '#0a0a0a';
  return props.selectedComponent.properties?.backgroundColor || '#0a0a0a';
};

// 📝 文本卡片组件专属 getter 函数
const getTextCardMultiLine = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return true;
  return props.selectedComponent.properties?.textCardConfig?.multiLine !== undefined
    ? props.selectedComponent.properties.textCardConfig.multiLine
    : true;
};

const getTextCardMaxLines = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return 10;
  return props.selectedComponent.properties?.textCardConfig?.maxLines || 10;
};

const getTextCardLineHeight = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return 1.6;
  return props.selectedComponent.properties?.textCardConfig?.lineHeight || 1.6;
};

const getTextCardBackgroundColor = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return '#ffffff';
  return props.selectedComponent.properties?.textCardConfig?.backgroundColor || '#ffffff';
};

const getTextCardBorderColor = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return '#d1d5db';
  return props.selectedComponent.properties?.textCardConfig?.borderColor || '#d1d5db';
};

const getTextCardBorderWidth = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return 1;
  return props.selectedComponent.properties?.textCardConfig?.borderWidth !== undefined
    ? props.selectedComponent.properties.textCardConfig.borderWidth
    : 1;
};

const getTextCardBorderRadius = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return 4;
  return props.selectedComponent.properties?.textCardConfig?.borderRadius !== undefined
    ? props.selectedComponent.properties.textCardConfig.borderRadius
    : 4;
};

const getTextCardShadowEnabled = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return true;
  return props.selectedComponent.properties?.textCardConfig?.shadow?.enabled !== undefined
    ? props.selectedComponent.properties.textCardConfig.shadow.enabled
    : true;
};

const getTextCardShadowColor = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return 'rgba(0,0,0,0.1)';
  return props.selectedComponent.properties?.textCardConfig?.shadow?.color || 'rgba(0,0,0,0.1)';
};

const getTextCardShadowOffsetX = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return 2;
  return props.selectedComponent.properties?.textCardConfig?.shadow?.offsetX !== undefined
    ? props.selectedComponent.properties.textCardConfig.shadow.offsetX
    : 2;
};

const getTextCardShadowOffsetY = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return 2;
  return props.selectedComponent.properties?.textCardConfig?.shadow?.offsetY !== undefined
    ? props.selectedComponent.properties.textCardConfig.shadow.offsetY
    : 2;
};

const getTextCardShadowBlur = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return 4;
  return props.selectedComponent.properties?.textCardConfig?.shadow?.blur !== undefined
    ? props.selectedComponent.properties.textCardConfig.shadow.blur
    : 4;
};

const getTextCardPadding = () => {
  if (!props.selectedComponent || props.selectedComponent.type !== 'text-card') return 16;
  return props.selectedComponent.properties?.textCardConfig?.padding !== undefined
    ? props.selectedComponent.properties.textCardConfig.padding
    : 16;
};

// 更新文本卡片配置
const updateTextCardConfig = (property: string, value: any) => {
  emit("update-text-card-config", property, value);
};

// 更新文本卡片阴影配置
const updateTextCardShadow = (property: string, value: any) => {
  emit("update-text-card-shadow", property, value);
};

const updateText = (property: string, value: any) => {
  emit("update-text", property, value);
};

const updateComponentProperty = (key?: string, value?: any) => {
  if (key && value !== undefined) {
    emit("update-component-property", key, value);
  } else {
    // 变换属性更新
    emit("update-component-transform");
  }
};

const updateComponentTransform = () => {
  emit("update-component-transform");
};

const updateShadowEffect = (enabled: boolean) => {
  const shadowValue = enabled ? "0 2px 8px rgba(0,0,0,0.1)" : "";
  updateStyle("boxShadow", shadowValue);
};

const alignComponent = (alignment: string) => {
  emit("align-component", alignment);
};

const clearSelectedComponent = () => {
  emit("clear-selected-component");
};

const updateChartConfig = (property: string, value: any) => {
  emit("update-chart-config", property, value);
};

const updateMqttConfig = (property: string, value: any) => {
  emit("update-chart-config", `mqttConfig.${property}`, value);
};

const openDatasetDialog = () => {
  emit("open-dataset-dialog");
};

// 画板配置已移至画板内部工具栏

// 检查是否为容器组件
const isContainerComponent = () => {
  if (!props.selectedComponent) return false;

  // 定义容器类型的组件 - 这些组件通常需要布局相关的样式属性
  const containerTypes = [
    "container",
    "panel",
    "group",
    "frame",
    "layout",
    "div",
    "section",
    "wrapper",
    "box",
    "card",
    "modal",
    "dialog",
    "drawer",
    "sidebar",
    "tab-panel",
    "collapse-panel",
    "accordion-panel"
  ];
  return containerTypes.includes(
    props.selectedComponent.type?.toLowerCase() || ""
  );
};

// 检查是否为SVG组件
const isSvgComponent = () => {
  if (!props.selectedComponent) return false;

  // 定义SVG类型的组件
  const svgTypes = [
    "svg",
    "path",
    "circle",
    "rect",
    "ellipse",
    "line",
    "polyline",
    "polygon",
    "icon",
    "shape",
    "vector",
    "graphic",
    "symbol",
    "drawing"
  ];
  return svgTypes.includes(props.selectedComponent.type?.toLowerCase() || "");
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
          fillType: "solid",
          fill: "#409eff",
          stroke: "#303133",
          strokeWidth: 1,
          strokeDasharray: "none",
          fillOpacity: 1,
          strokeOpacity: 1
        },
        classic: {
          fillType: "solid",
          fill: "#67c23a",
          stroke: "#6c757d",
          strokeWidth: 2,
          strokeDasharray: "none",
          fillOpacity: 0.8,
          strokeOpacity: 1
        },
        minimal: {
          fillType: "transparent",
          fill: null,
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
    // 交互样式
    "cursor",
    "userSelect",
    "pointerEvents",
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
};

// 根据组件类型适配样式功能
const getComponentTypeInfo = () => {
  if (!props.selectedComponent) return null;

  const componentType = props.selectedComponent.type?.toLowerCase() || "";

  // 分类不同的组件类型
  const typeCategories = {
    // 图表类组件
    chart: [
      "chart",
      "line-chart",
      "bar-chart",
      "pie-chart",
      "area-chart",
      "scatter-chart"
    ],
    // 文本类组件
    text: ["text", "label", "title", "paragraph", "span", "heading"],
    // 输入类组件
    input: [
      "input",
      "textarea",
      "select",
      "checkbox",
      "radio",
      "switch",
      "slider"
    ],
    // 按钮类组件
    button: ["button", "link", "tab"],
    // 容器类组件
    container: [
      "container",
      "panel",
      "group",
      "frame",
      "layout",
      "div",
      "section",
      "wrapper",
      "box",
      "card",
      "modal",
      "dialog",
      "drawer",
      "sidebar",
      "tab-panel",
      "collapse-panel",
      "accordion-panel"
    ],
    // 媒体类组件
    media: ["image", "icon", "avatar", "video", "audio"],
    // 指示器类组件
    indicator: ["progress", "loading", "badge", "tag", "alert", "notification"]
  };

  // 查找组件属于哪个分类
  for (const [category, types] of Object.entries(typeCategories)) {
    if (types.includes(componentType)) {
      return { type: componentType, category };
    }
  }

  // 默认归类为通用组件
  return { type: componentType, category: "general" };
};

// 尺寸变化处理函数
const handleSizeChange = () => {
  console.log("尺寸变化:", {
    width: editableComponentWidth.value,
    height: editableComponentHeight.value
  });

  // 如果开启保持比例，需要相应调整
  if (getComponentLockAspectRatio() && props.selectedComponent) {
    // 计算原始宽高比
    const originalWidth =
      props.selectedComponent.originalWidth || componentWidth.value;
    const originalHeight =
      props.selectedComponent.originalHeight || componentHeight.value;
    const aspectRatio = originalWidth / originalHeight;

    // 根据宽度调整高度，或根据高度调整宽度（以最后修改的为准）
    console.log("保持比例调整，宽高比:", aspectRatio);
  }

  updateSize("width", editableComponentWidth.value);
  updateSize("height", editableComponentHeight.value);
  updateComponentTransform();
};

// 保持比例变化处理
const handleAspectRatioChange = (value: boolean) => {
  console.log("保持比例变化:", value);

  // 保存原始尺寸用于比例计算
  if (value && props.selectedComponent) {
    props.selectedComponent.originalWidth = componentWidth.value;
    props.selectedComponent.originalHeight = componentHeight.value;
    console.log("保存原始尺寸:", {
      originalWidth: props.selectedComponent.originalWidth,
      originalHeight: props.selectedComponent.originalHeight
    });
  }

  updateProperty("lockAspectRatio", value);
  updateComponentTransform();
};

// 缩放变化处理
const handleScaleChange = (value: number) => {
  console.log("缩放变化:", value);
  updateProperty("scale", value);
  updateComponentTransform();
};

// 旋转变化处理
const handleRotationChange = (value: number) => {
  console.log("旋转变化:", value);
  updateProperty("rotation", value);
  updateComponentTransform();
};

// 翻转变化处理
const handleFlipHorizontalChange = (value: boolean) => {
  console.log("水平翻转变化:", value);
  updateProperty("flipHorizontal", value);
  updateComponentTransform();
};

const handleFlipVerticalChange = (value: boolean) => {
  console.log("垂直翻转变化:", value);
  updateProperty("flipVertical", value);
  updateComponentTransform();
};

// 倾斜变化处理
const handleSkewXChange = (value: number) => {
  console.log("X倾斜变化:", value);
  updateProperty("skewX", value);
  updateComponentTransform();
};

const handleSkewYChange = (value: number) => {
  console.log("Y倾斜变化:", value);
  updateProperty("skewY", value);
  updateComponentTransform();
};

// 变换原点变化处理
const handleOriginXChange = (value: string) => {
  console.log("X原点变化:", value);
  updateProperty("originX", value);
  updateComponentTransform();
};

const handleOriginYChange = (value: string) => {
  console.log("Y原点变化:", value);
  updateProperty("originY", value);
  updateComponentTransform();
};

// 新增的变换操作方法
const adjustPosition = (axis: string, delta: number) => {
  if (!props.selectedComponent) return;

  const currentValue =
    axis === "x" ? editableComponentX.value : editableComponentY.value;
  const newValue = currentValue + delta;

  if (axis === "x") {
    editableComponentX.value = newValue;
  } else {
    editableComponentY.value = newValue;
  }

  updateComponentTransform();
};

const setSizePreset = (preset: string) => {
  if (!props.selectedComponent) return;

  const presets = {
    small: { width: 60, height: 60 },
    medium: { width: 120, height: 120 },
    large: { width: 200, height: 200 }
  };

  const size = presets[preset as keyof typeof presets];
  if (size) {
    editableComponentWidth.value = size.width;
    editableComponentHeight.value = size.height;
    updateComponentTransform();
  }
};

const fitToContent = () => {
  if (!props.selectedComponent) return;

  // 根据内容自动调整大小的逻辑
  // 这里可以根据组件类型实现不同的自适应逻辑
  const component = props.selectedComponent;

  if (component.type === "text" || component.type === "label") {
    // 文本组件根据文字长度调整
    const textLength = (component.text || "").length;
    const fontSize = component.fontSize || 14;
    editableComponentWidth.value = Math.max(textLength * fontSize * 0.6, 50);
    editableComponentHeight.value = fontSize + 10;
  } else {
    // 其他组件使用默认逻辑
    editableComponentWidth.value = 100;
    editableComponentHeight.value = 100;
  }

  updateComponentTransform();
};

const resetSize = () => {
  if (!props.selectedComponent) return;

  editableComponentWidth.value = 100;
  editableComponentHeight.value = 100;
  updateComponentTransform();
};

const rotateComponent = (angle: number) => {
  if (!props.selectedComponent) return;

  const currentRotation = getComponentRotation();
  const newRotation = (currentRotation + angle) % 360;

  console.log("旋转组件:", {
    current: currentRotation,
    delta: angle,
    new: newRotation
  });
  handleRotationChange(newRotation);
};

const setRotation = (angle: number) => {
  if (!props.selectedComponent) return;

  console.log("设置旋转角度:", angle);
  handleRotationChange(angle);
};

const scaleComponent = (scale: number) => {
  if (!props.selectedComponent) return;

  console.log("设置缩放比例:", scale);
  handleScaleChange(scale);
};

const toggleFlip = (direction: string) => {
  if (!props.selectedComponent) return;

  if (direction === "horizontal") {
    const current = getComponentFlipHorizontal();
    console.log("切换水平翻转:", { current, new: !current });
    handleFlipHorizontalChange(!current);
  } else if (direction === "vertical") {
    const current = getComponentFlipVertical();
    console.log("切换垂直翻转:", { current, new: !current });
    handleFlipVerticalChange(!current);
  }
};

const resetFlips = () => {
  if (!props.selectedComponent) return;

  console.log("重置翻转");
  handleFlipHorizontalChange(false);
  handleFlipVerticalChange(false);
};

const resetTransforms = () => {
  if (!props.selectedComponent) return;

  console.log("重置所有变换");
  // 重置所有变换属性到默认值
  handleRotationChange(0);
  handleScaleChange(1);
  handleSkewXChange(0);
  handleSkewYChange(0);
  handleFlipHorizontalChange(false);
  handleFlipVerticalChange(false);
  handleOriginXChange("center");
  handleOriginYChange("center");
};

const copyTransform = () => {
  if (!props.selectedComponent) return;

  const component = props.selectedComponent;

  // 创建变换数据副本
  const transformData = {
    x: getComponentX(),
    y: getComponentY(),
    width: getComponentWidth(),
    height: getComponentHeight(),
    rotation: getComponentRotation(),
    scale: getComponentScale(),
    skewX: getComponentSkewX(),
    skewY: getComponentSkewY(),
    flipHorizontal: getComponentFlipHorizontal(),
    flipVertical: getComponentFlipVertical(),
    originX: getComponentOriginX(),
    originY: getComponentOriginY()
  };

  // 保存到剪贴板或者全局状态
  try {
    navigator.clipboard.writeText(JSON.stringify(transformData));
    // 可以添加成功提示
    console.log("变换属性已复制到剪贴板");
  } catch (err) {
    console.error("复制失败:", err);
  }
};
</script>

<style scoped lang="scss">
.property-panel {
  width: 100%;
  height: 100%;
  background: var(--el-bg-color);
  border-left: 1px solid var(--el-border-color);
  display: flex;
  flex-direction: column;
}

.panel-header {
  padding: 16px;
  border-bottom: 1px solid var(--el-border-color);
  background: #fafafa;
  display: flex;
  justify-content: space-between;
  align-items: center;

  h4 {
    margin: 0;
    font-size: 14px;
    font-weight: 600;
    color: #303133;
  }
}

.panel-content {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  position: relative;

  /* 优化滚动性能 */
  -webkit-overflow-scrolling: touch;
  scroll-behavior: smooth;

  /* 自定义滚动条样式 */
  &::-webkit-scrollbar {
    width: 8px;
  }

  &::-webkit-scrollbar-track {
    background: #f1f1f1;
    border-radius: 4px;
  }

  &::-webkit-scrollbar-thumb {
    background: #c1c1c1;
    border-radius: 4px;

    &:hover {
      background: #a8a8a8;
    }
  }

  /* 确保子元素不会导致水平滚动 */
  * {
    box-sizing: border-box;
  }

  .property-form {
    min-height: 100%;
    padding-bottom: 20px; /* 防止内容贴底 */

    :deep(.el-collapse) {
      border: none;

      .el-collapse-item {
        border-bottom: 1px solid var(--el-border-color-lighter);
        margin-bottom: 0; /* 防止外边距累积 */

        &:last-child {
          border-bottom: none;
        }

        .el-collapse-item__header {
          height: 56px;
          line-height: 56px;
          padding-left: 20px;
          background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
          border: none;
          border-radius: 12px 12px 0 0;
          font-weight: 700;
          color: white;
          font-size: 15px;
          position: relative;
          box-shadow: 0 4px 20px rgba(102, 126, 234, 0.3);
          transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
          margin-bottom: 2px;

          /* 添加炫酷的光晕效果 */
          &::before {
            content: "";
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: linear-gradient(
              135deg,
              rgba(255, 255, 255, 0.2) 0%,
              rgba(255, 255, 255, 0) 100%
            );
            border-radius: 12px 12px 0 0;
            opacity: 0;
            transition: opacity 0.3s ease;
          }

          &:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 30px rgba(102, 126, 234, 0.5);

            &::before {
              opacity: 1;
            }
          }

          &.is-active {
            background: linear-gradient(135deg, #ff6b6b 0%, #ffa726 100%);
            box-shadow: 0 6px 25px rgba(255, 107, 107, 0.4);
            transform: translateY(-1px);

            &::before {
              opacity: 1;
            }
          }

          /* 添加闪烁动画 */
          &.is-active::after {
            content: "✨";
            position: absolute;
            right: 20px;
            top: 50%;
            transform: translateY(-50%);
            animation: sparkle 2s infinite ease-in-out;
          }
        }

        .el-collapse-item__wrap {
          border: none;
          border-radius: 0 0 12px 12px;
          overflow: hidden;
          box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);

          .el-collapse-item__content {
            padding: 20px;
            background: linear-gradient(135deg, #f8fafc 0%, #edf2f7 100%);
            position: relative;
            width: 100%;
            box-sizing: border-box;

            /* 添加微妙的纹理效果 */
            &::before {
              content: "";
              position: absolute;
              top: 0;
              left: 0;
              right: 0;
              height: 2px;
              background: linear-gradient(
                90deg,
                #667eea,
                #764ba2,
                #ff6b6b,
                #ffa726
              );
              background-size: 200% 100%;
              animation: rainbow 3s linear infinite;
            }

            /* 内容区域动画 */
            .property-section {
              animation: slideInUp 0.6s ease-out;
            }
          }
        }
      }
    }

    /* 优化表单元素，防止滚动卡顿 */
    :deep(.el-form-item) {
      margin-bottom: 12px;

      &:last-child {
        margin-bottom: 0;
      }
    }

    /* 确保所有输入控件不会超出容器 */
    :deep(.el-input, .el-select, .el-color-picker) {
      width: 100%;
      max-width: 100%;
    }
  }
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

  /* 添加炫酷的边框光效 */
  &::before {
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

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 12px 48px rgba(102, 126, 234, 0.15);

    &::before {
      opacity: 1;
    }
  }

  &:last-child {
    margin-bottom: 0;
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

    /* 添加图标前缀 */
    &::before {
      content: "⚡";
      font-size: 16px;
      margin-right: 8px;
      animation: pulse 2s infinite;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }

    /* 添加发光效果 */
    &::after {
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

    &:hover::after {
      transform: scaleX(1);
    }
  }

  // 样式预设网格布局
  .preset-grid {
    .el-button {
      width: 100%;
      border-radius: 8px;
      font-weight: 500;
      transition: all 0.3s ease;

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      }

      &.el-button--primary {
        background: linear-gradient(135deg, #409eff 0%, #1976d2 100%);
        border: none;
      }

      &.el-button--success {
        background: linear-gradient(135deg, #67c23a 0%, #4caf50 100%);
        border: none;
      }

      &.el-button--warning {
        background: linear-gradient(135deg, #e6a23c 0%, #ff9800 100%);
        border: none;
      }

      &.el-button--danger {
        background: linear-gradient(135deg, #f56c6c 0%, #e53e3e 100%);
        border: none;
      }

      &.el-button--info {
        background: linear-gradient(135deg, #909399 0%, #6c757d 100%);
        border: none;
      }
    }
  }

  // 变换组样式
  .transform-group {
    margin-bottom: 20px;
    padding: 16px;
    background: linear-gradient(135deg, #fafbfc 0%, #f4f6f8 100%);
    border-radius: 8px;
    border: 1px solid #e8ecf0;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);

    &:last-child {
      margin-bottom: 0;
    }

    .group-title {
      display: flex;
      align-items: center;
      font-size: 14px;
      font-weight: 600;
      color: #1976d2;
      margin-bottom: 12px;
      padding-bottom: 8px;
      border-bottom: 2px solid #e3f2fd;

      .el-icon {
        margin-right: 8px;
        font-size: 16px;
      }
    }
  }

  .coordinate-display {
    margin-bottom: 12px;
    padding: 8px;
    background: linear-gradient(135deg, #f6f9fc 0%, #e3f2fd 100%);
    border-radius: 6px;
    border: 1px solid #e1f5fe;
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 4px;

    .el-tag {
      font-size: 11px;
      font-weight: 500;
      border-radius: 4px;

      &.el-tag--info {
        background: linear-gradient(135deg, #2196f3 0%, #1976d2 100%);
        border-color: #1976d2;
        color: white;
      }

      &.el-tag--success {
        background: linear-gradient(135deg, #4caf50 0%, #2e7d32 100%);
        border-color: #2e7d32;
        color: white;
      }
    }
  }

  .quick-actions {
    margin-top: 16px;
    padding: 12px;
    background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
    border-radius: 6px;
    border: 1px solid #dee2e6;

    .action-title {
      font-size: 12px;
      font-weight: 600;
      color: #495057;
      margin-bottom: 8px;
      text-align: center;
    }

    .el-button {
      width: 100%;
      font-size: 11px;
      padding: 6px 4px;
      height: 28px;
      border-radius: 4px;
      transition: all 0.3s ease;

      .el-icon {
        margin-right: 2px;
        font-size: 12px;
      }

      &.el-button--primary {
        background: linear-gradient(135deg, #007bff 0%, #0056b3 100%);
        border-color: transparent;

        &:hover {
          transform: translateY(-1px);
          box-shadow: 0 2px 8px rgba(0, 123, 255, 0.3);
        }
      }

      &.el-button--success {
        background: linear-gradient(135deg, #28a745 0%, #1e7e34 100%);
        border-color: transparent;

        &:hover {
          transform: translateY(-1px);
          box-shadow: 0 2px 8px rgba(40, 167, 69, 0.3);
        }
      }

      &.el-button--danger {
        background: linear-gradient(135deg, #dc3545 0%, #bd2130 100%);
        border-color: transparent;

        &:hover {
          transform: translateY(-1px);
          box-shadow: 0 2px 8px rgba(220, 53, 69, 0.3);
        }
      }
    }
  }
}

.advanced-controls {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;

  .el-button {
    flex: 1;
  }
}

.binding-info {
  padding: 12px;
  background: #f0f9ff;
  border-radius: 6px;
  border: 1px solid #e3f2fd;
  margin-top: 16px;

  .binding-detail {
    margin: 8px 0;
    font-size: 12px;
    color: #606266;
  }
}

.component-properties {
  margin-top: 16px;

  .section-title {
    font-size: 13px;
    font-weight: 600;
    color: #606266;
    margin-bottom: 12px;
    padding: 6px 12px;
    background: #f0f2f5;
    border-radius: 4px;
    border-left: 3px solid #1976d2;
  }
}

.no-selection {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

// 全局样式重写
:deep(.el-form-item) {
  margin-bottom: 12px;

  .el-form-item__label {
    font-size: 13px;
    color: #606266;
    font-weight: 500;
    line-height: 1.4;
  }

  .el-input,
  .el-input-number {
    .el-input__wrapper {
      border-radius: 6px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      transition: all 0.3s ease;

      &:hover {
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
      }

      &.is-focus {
        box-shadow: 0 0 0 2px rgba(25, 118, 210, 0.25);
      }
    }
  }

  .el-input__inner {
    width: 20px;
  }
  .el-button {
    border-radius: 6px;
    font-size: 12px;
    padding: 4px 8px;
  }

  .unit {
    margin-left: 8px;
    font-size: 12px;
    color: #909399;
  }
}

// 对齐按钮特殊样式
.property-section {
  .el-row .el-button {
    width: 100%;
    font-size: 11px;
    padding: 6px 4px;

    .el-icon {
      margin-right: 2px;
    }
  }
}

/* 炫酷动画效果 */
@keyframes sparkle {
  0%,
  100% {
    transform: translateY(-50%) scale(1);
    opacity: 0.8;
  }
  50% {
    transform: translateY(-50%) scale(1.2);
    opacity: 1;
  }
}

@keyframes rainbow {
  0% {
    background-position: 0% 50%;
  }
  50% {
    background-position: 100% 50%;
  }
  100% {
    background-position: 0% 50%;
  }
}

@keyframes slideInUp {
  0% {
    transform: translateY(20px);
    opacity: 0;
  }
  100% {
    transform: translateY(0);
    opacity: 1;
  }
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

@keyframes glow {
  0%,
  100% {
    box-shadow: 0 0 20px rgba(102, 126, 234, 0.3);
  }
  50% {
    box-shadow: 0 0 30px rgba(102, 126, 234, 0.6);
  }
}

/* 响应式适配 */
@media (max-width: 768px) {
  .property-panel {
    .panel-content {
      .property-form {
        :deep(.el-collapse) {
          .el-collapse-item {
            .el-collapse-item__header {
              height: 48px;
              line-height: 48px;
              font-size: 14px;
              padding-left: 16px;
            }
          }
        }
      }
    }
  }

  .property-section {
    padding: 14px;
    border-radius: 12px;

    .section-title {
      font-size: 13px;
      padding: 8px 12px;

      &::before {
        font-size: 14px;
      }
    }
  }
}
</style>
