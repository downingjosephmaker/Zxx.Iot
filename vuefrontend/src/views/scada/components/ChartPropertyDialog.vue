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
    class="chart-property-dialog"
    @close="handleClose"
  >
    <!-- 自定义头部 -->
    <template #header="{ close }">
      <div class="custom-dialog-header">
        <div class="header-left">
          <div class="header-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
              <path
                d="M3 3v18h18"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
              />
              <path
                d="M7 16l4-8 4 4 4-6"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
          </div>
          <div class="header-content">
            <h3 class="header-title">图表属性配置</h3>
            <p class="header-subtitle">配置图表的数据源、样式和展示效果</p>
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

    <div class="chart-dialog-content">
      <el-tabs v-model="activeTab" type="border-card">
        <!-- 基本信息标签页 -->
        <el-tab-pane label="基本信息" name="basic">
          <el-form :model="formData" label-width="100px" class="chart-form">
            <el-form-item label="图表标题">
              <el-input
                v-model="formData.title"
                placeholder="请输入图表标题"
                clearable
              />
            </el-form-item>

            <el-form-item label="图表类型">
              <el-select
                v-model="formData.type"
                placeholder="请选择图表类型"
                @change="handleChartTypeChange"
                style="width: 100%"
              >
                <el-option-group label="基础图表">
                  <el-option label="📈 折线图" value="line">
                    <div class="chart-option">
                      <span class="chart-icon">📈</span>
                      <div class="chart-info">
                        <div class="chart-name">折线图</div>
                        <div class="chart-desc">适用于趋势分析</div>
                      </div>
                    </div>
                  </el-option>
                  <el-option label="📊 柱状图" value="bar">
                    <div class="chart-option">
                      <span class="chart-icon">📊</span>
                      <div class="chart-info">
                        <div class="chart-name">柱状图</div>
                        <div class="chart-desc">适用于数据对比</div>
                      </div>
                    </div>
                  </el-option>
                  <el-option label="🥧 饼图" value="pie">
                    <div class="chart-option">
                      <span class="chart-icon">🥧</span>
                      <div class="chart-info">
                        <div class="chart-name">饼图</div>
                        <div class="chart-desc">适用于占比展示</div>
                      </div>
                    </div>
                  </el-option>
                  <el-option label="🌊 面积图" value="area">
                    <div class="chart-option">
                      <span class="chart-icon">🌊</span>
                      <div class="chart-info">
                        <div class="chart-name">面积图</div>
                        <div class="chart-desc">适用于累积趋势</div>
                      </div>
                    </div>
                  </el-option>
                </el-option-group>

                <el-option-group label="专业图表">
                  <el-option label="⚡ 仪表盘" value="gauge">
                    <div class="chart-option">
                      <span class="chart-icon">⚡</span>
                      <div class="chart-info">
                        <div class="chart-name">仪表盘</div>
                        <div class="chart-desc">适用于单一指标</div>
                      </div>
                    </div>
                  </el-option>
                  <el-option label="🎯 雷达图" value="radar">
                    <div class="chart-option">
                      <span class="chart-icon">🎯</span>
                      <div class="chart-info">
                        <div class="chart-name">雷达图</div>
                        <div class="chart-desc">适用于多维评估</div>
                      </div>
                    </div>
                  </el-option>
                  <el-option label="💧 漏斗图" value="funnel">
                    <div class="chart-option">
                      <span class="chart-icon">💧</span>
                      <div class="chart-info">
                        <div class="chart-name">漏斗图</div>
                        <div class="chart-desc">适用于转化分析</div>
                      </div>
                    </div>
                  </el-option>
                  <el-option label="💎 散点图" value="scatter">
                    <div class="chart-option">
                      <span class="chart-icon">💎</span>
                      <div class="chart-info">
                        <div class="chart-name">散点图</div>
                        <div class="chart-desc">适用于分布分析</div>
                      </div>
                    </div>
                  </el-option>
                  <el-option label="📉 K线图" value="candlestick">
                    <div class="chart-option">
                      <span class="chart-icon">📉</span>
                      <div class="chart-info">
                        <div class="chart-name">K线图</div>
                        <div class="chart-desc">适用于金融数据</div>
                      </div>
                    </div>
                  </el-option>
                </el-option-group>
              </el-select>
            </el-form-item>

            <el-form-item label="图表主题">
              <div class="theme-preset-grid">
                <!-- 基础主题 -->
                <el-row :gutter="6">
                  <el-col :span="8">
                    <div
                      class="theme-preset-btn"
                      :class="{ 'active': formData.theme === 'default' }"
                      @click="formData.theme = 'default'"
                    >
                      <div class="theme-icon">🎨</div>
                      <div class="theme-name">默认</div>
                      <div class="theme-colors">
                        <span class="color-dot" style="background: #5470c6"></span>
                        <span class="color-dot" style="background: #91cc75"></span>
                        <span class="color-dot" style="background: #fac858"></span>
                      </div>
                    </div>
                  </el-col>
                  <el-col :span="8">
                    <div
                      class="theme-preset-btn"
                      :class="{ 'active': formData.theme === 'dark' }"
                      @click="formData.theme = 'dark'"
                    >
                      <div class="theme-icon">🌙</div>
                      <div class="theme-name">深色</div>
                      <div class="theme-colors">
                        <span class="color-dot" style="background: #c23531"></span>
                        <span class="color-dot" style="background: #2f4554"></span>
                        <span class="color-dot" style="background: #61a0a8"></span>
                      </div>
                    </div>
                  </el-col>
                  <el-col :span="8">
                    <div
                      class="theme-preset-btn"
                      :class="{ 'active': formData.theme === 'light' }"
                      @click="formData.theme = 'light'"
                    >
                      <div class="theme-icon">☀️</div>
                      <div class="theme-name">浅色</div>
                      <div class="theme-colors">
                        <span class="color-dot" style="background: #5470c6"></span>
                        <span class="color-dot" style="background: #91cc75"></span>
                        <span class="color-dot" style="background: #fac858"></span>
                      </div>
                    </div>
                  </el-col>
                </el-row>

                <!-- 渐变主题 -->
                <el-row :gutter="6" style="margin-top: 6px">
                  <el-col :span="8">
                    <div
                      class="theme-preset-btn"
                      :class="{ 'active': formData.theme === 'tech-blue' }"
                      @click="formData.theme = 'tech-blue'"
                    >
                      <div class="theme-icon">🎭</div>
                      <div class="theme-name">科技蓝</div>
                      <div class="theme-colors">
                        <span class="color-dot" style="background: #4facfe"></span>
                        <span class="color-dot" style="background: #00f2fe"></span>
                        <span class="color-dot" style="background: #43e97b"></span>
                      </div>
                    </div>
                  </el-col>
                  <el-col :span="8">
                    <div
                      class="theme-preset-btn"
                      :class="{ 'active': formData.theme === 'pink-dream' }"
                      @click="formData.theme = 'pink-dream'"
                    >
                      <div class="theme-icon">🌸</div>
                      <div class="theme-name">粉色梦幻</div>
                      <div class="theme-colors">
                        <span class="color-dot" style="background: #f093fb"></span>
                        <span class="color-dot" style="background: #f5576c"></span>
                        <span class="color-dot" style="background: #fa709a"></span>
                      </div>
                    </div>
                  </el-col>
                  <el-col :span="8">
                    <div
                      class="theme-preset-btn"
                      :class="{ 'active': formData.theme === 'nature-green' }"
                      @click="formData.theme = 'nature-green'"
                    >
                      <div class="theme-icon">🌿</div>
                      <div class="theme-name">自然绿</div>
                      <div class="theme-colors">
                        <span class="color-dot" style="background: #11998e"></span>
                        <span class="color-dot" style="background: #38ef7d"></span>
                        <span class="color-dot" style="background: #43e97b"></span>
                      </div>
                    </div>
                  </el-col>
                </el-row>

                <el-row :gutter="6" style="margin-top: 6px">
                  <el-col :span="8">
                    <div
                      class="theme-preset-btn"
                      :class="{ 'active': formData.theme === 'sunset-orange' }"
                      @click="formData.theme = 'sunset-orange'"
                    >
                      <div class="theme-icon">🌅</div>
                      <div class="theme-name">日落橙</div>
                      <div class="theme-colors">
                        <span class="color-dot" style="background: #ff6b6b"></span>
                        <span class="color-dot" style="background: #ff8e53"></span>
                        <span class="color-dot" style="background: #ffd93d"></span>
                      </div>
                    </div>
                  </el-col>
                  <el-col :span="8">
                    <div
                      class="theme-preset-btn"
                      :class="{ 'active': formData.theme === 'ocean-blue' }"
                      @click="formData.theme = 'ocean-blue'"
                    >
                      <div class="theme-icon">🌊</div>
                      <div class="theme-name">海洋蓝</div>
                      <div class="theme-colors">
                        <span class="color-dot" style="background: #0093E9"></span>
                        <span class="color-dot" style="background: #80D0C7"></span>
                        <span class="color-dot" style="background: #13547a"></span>
                      </div>
                    </div>
                  </el-col>
                  <el-col :span="8">
                    <div
                      class="theme-preset-btn"
                      :class="{ 'active': formData.theme === 'business' }"
                      @click="formData.theme = 'business'"
                    >
                      <div class="theme-icon">💼</div>
                      <div class="theme-name">商务</div>
                      <div class="theme-colors">
                        <span class="color-dot" style="background: #2c3e50"></span>
                        <span class="color-dot" style="background: #34495e"></span>
                        <span class="color-dot" style="background: #7f8c8d"></span>
                      </div>
                    </div>
                  </el-col>
                </el-row>

                <!-- 专业主题 -->
                <el-row :gutter="6" style="margin-top: 6px">
                  <el-col :span="8">
                    <div
                      class="theme-preset-btn"
                      :class="{ 'active': formData.theme === 'minimal-bw' }"
                      @click="formData.theme = 'minimal-bw'"
                    >
                      <div class="theme-icon">🎯</div>
                      <div class="theme-name">极简</div>
                      <div class="theme-colors">
                        <span class="color-dot" style="background: #000000"></span>
                        <span class="color-dot" style="background: #666666"></span>
                        <span class="color-dot" style="background: #cccccc"></span>
                      </div>
                    </div>
                  </el-col>
                  <el-col :span="8">
                    <div
                      class="theme-preset-btn"
                      :class="{ 'active': formData.theme === 'heatmap' }"
                      @click="formData.theme = 'heatmap'"
                    >
                      <div class="theme-icon">🔥</div>
                      <div class="theme-name">热力</div>
                      <div class="theme-colors">
                        <span class="color-dot" style="background: #313695"></span>
                        <span class="color-dot" style="background: #fee090"></span>
                        <span class="color-dot" style="background: #d73027"></span>
                      </div>
                    </div>
                  </el-col>
                  <el-col :span="8">
                    <div
                      class="theme-preset-btn"
                      :class="{ 'active': formData.theme === 'rainbow' }"
                      @click="formData.theme = 'rainbow'"
                    >
                      <div class="theme-icon">🌈</div>
                      <div class="theme-name">彩虹</div>
                      <div class="theme-colors">
                        <span class="color-dot" style="background: #FF0080"></span>
                        <span class="color-dot" style="background: #FFD700"></span>
                        <span class="color-dot" style="background: #1E90FF"></span>
                      </div>
                    </div>
                  </el-col>
                </el-row>
              </div>
            </el-form-item>

            <el-form-item label="启用动画">
              <el-switch v-model="formData.enableAnimation" />
              <span style="margin-left: 10px; color: #909399; font-size: 12px">
                平滑的动画效果
              </span>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 数据源标签页 -->
        <el-tab-pane label="数据绑定" name="datasource">
          <el-form :model="formData" label-width="100px" class="chart-form">
            <el-alert
              title="提示"
              type="info"
              :closable="false"
              style="margin-bottom: 20px"
            >
              请先在"数据集"模块中配置数据源，然后在此处选择绑定
            </el-alert>

            <el-form-item label="绑定数据集">
              <el-select
                v-model="formData.datasetId"
                placeholder="请选择数据集"
                clearable
                @change="handleDatasetChange"
              >
                <el-option
                  v-for="dataset in datasetList"
                  :key="dataset.id"
                  :label="`${dataset.name} (${getDatasetTypeName(dataset.type)})`"
                  :value="dataset.id"
                >
                  <span>{{ dataset.name }}</span>
                  <span style="float: right; color: #8492a6; font-size: 12px">
                    {{ getDatasetTypeName(dataset.type) }}
                  </span>
                </el-option>
              </el-select>
            </el-form-item>

            <el-form-item v-if="!formData.datasetId" label="">
              <el-button type="primary" @click="openDatasetPanel">
                <el-icon><Plus /></el-icon>
                配置数据集
              </el-button>
            </el-form-item>

            <div v-if="formData.datasetId" class="data-config-section">
              <el-form-item label="数据路径">
                <el-input
                  v-model="formData.dataPath"
                  placeholder="如: data.items 或 data[0].value"
                  clearable
                >
                  <template #append>
                    <el-button @click="testDataPath">测试</el-button>
                  </template>
                </el-input>
                <div style="color: #909399; font-size: 12px; margin-top: 5px">
                  用于从数据集返回结果中提取图表数据
                </div>
              </el-form-item>

              <el-form-item label="刷新间隔">
                <el-input-number
                  v-model="formData.refreshInterval"
                  :min="1000"
                  :step="1000"
                  :max="60000"
                />
                <span style="margin-left: 10px; color: #909399">毫秒</span>
              </el-form-item>

              <el-form-item label="当前绑定">
                <el-tag type="success">
                  {{ getCurrentDatasetName() }}
                </el-tag>
              </el-form-item>
            </div>
          </el-form>
        </el-tab-pane>

        <!-- 样式配置标签页 -->
        <el-tab-pane label="样式配置" name="style">
          <el-form :model="formData" label-width="100px" class="chart-form">
            <el-form-item label="图表宽度">
              <el-slider
                v-model="formData.width"
                :min="200"
                :max="1000"
                :step="10"
                show-input
              />
            </el-form-item>

            <el-form-item label="图表高度">
              <el-slider
                v-model="formData.height"
                :min="150"
                :max="800"
                :step="10"
                show-input
              />
            </el-form-item>

            <el-form-item label="显示图例">
              <el-switch v-model="formData.showLegend" />
            </el-form-item>

            <el-form-item v-if="formData.showLegend" label="图例位置">
              <el-select v-model="formData.legendPosition" placeholder="选择位置">
                <el-option label="顶部" value="top" />
                <el-option label="底部" value="bottom" />
                <el-option label="左侧" value="left" />
                <el-option label="右侧" value="right" />
              </el-select>
            </el-form-item>

            <el-form-item label="显示工具栏">
              <el-switch v-model="formData.showToolbox" />
            </el-form-item>

            <el-form-item label="显示网格">
              <el-switch v-model="formData.showGrid" />
            </el-form-item>

            <el-form-item label="数据缩放">
              <el-switch v-model="formData.enableDataZoom" />
              <span style="margin-left: 10px; color: #909399; font-size: 12px">
                启用滚动缩放和拖拽平移
              </span>
            </el-form-item>

            <el-form-item label="平滑曲线">
              <el-switch v-model="formData.smoothLine" />
              <span style="margin-left: 10px; color: #909399; font-size: 12px">
                折线图和面积图使用平滑曲线
              </span>
            </el-form-item>

            <el-form-item label="颜色方案">
              <el-radio-group v-model="formData.colorScheme">
                <el-radio label="default">默认</el-radio>
                <el-radio label="warm">暖色调</el-radio>
                <el-radio label="cool">冷色调</el-radio>
                <el-radio label="vibrant">鲜艳</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 预览标签页 -->
        <el-tab-pane label="预览" name="preview">
          <div class="chart-preview-container">
            <div class="preview-header">
              <div class="preview-info">
                <el-tag type="primary" effect="dark" size="large">
                  {{ getChartTypeName(formData.type) }}
                </el-tag>
                <el-tag type="success" effect="plain">
                  主题: {{ formData.theme }}
                </el-tag>
                <el-tag type="warning" effect="plain">
                  配色: {{ formData.colorScheme }}
                </el-tag>
                <el-tag type="info" effect="plain">
                  尺寸: {{ formData.width }}×{{ formData.height }}
                </el-tag>
              </div>
              <el-button-group size="small">
                <el-button @click="initPreviewChart">
                  <el-icon><Refresh /></el-icon>
                  刷新
                </el-button>
                <el-button @click="fullscreenPreview = !fullscreenPreview">
                  <el-icon><FullScreen /></el-icon>
                  {{ fullscreenPreview ? '退出全屏' : '全屏预览' }}
                </el-button>
              </el-button-group>
            </div>

            <div
              class="preview-chart-wrapper"
              :class="{ 'fullscreen': fullscreenPreview }"
              @click="handleFullscreenClick"
            >
              <!-- 全屏模式下的退出按钮 -->
              <el-button
                v-if="fullscreenPreview"
                class="fullscreen-exit-btn"
                type="info"
                size="large"
                circle
                @click.stop="fullscreenPreview = false"
              >
                <el-icon><Close /></el-icon>
              </el-button>

              <div
                ref="previewChartRef"
                class="preview-chart"
                :style="{
                  width: formData.width + 'px',
                  height: formData.height + 'px',
                  maxWidth: '100%'
                }"
                @click.stop
              />

              <div class="preview-loading" v-if="previewLoading">
                <el-icon class="is-loading"><Loading /></el-icon>
                <span>加载中...</span>
              </div>
            </div>

            <div class="preview-footer">
              <div class="footer-left">
                <el-icon><InfoFilled /></el-icon>
                <span>这是基于示例数据的预览效果，实际数据需要绑定数据源</span>
              </div>
              <div class="footer-right">
                <el-text type="info" size="small">
                  动画: {{ formData.enableAnimation ? '已启用' : '已禁用' }}
                </el-text>
              </div>
            </div>
          </div>
        </el-tab-pane>
      </el-tabs>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleClose">取消</el-button>
        <el-button type="primary" @click="handlePreview">
          <el-icon><View /></el-icon>
          预览
        </el-button>
        <el-button type="success" @click="handleSave">
          <el-icon><Check /></el-icon>
          保存
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch, nextTick, computed } from "vue";
import { ElMessage } from "element-plus";
import { Plus, View, Check, Close, Refresh, FullScreen, Loading, InfoFilled } from "@element-plus/icons-vue";
import * as echarts from "echarts";

// Props
const props = defineProps({
  visible: {
    type: Boolean,
    default: false
  },
  chartComponent: {
    type: Object,
    default: null
  },
  datasetList: {
    type: Array,
    default: () => []
  }
});

// Emits
const emit = defineEmits(["update:visible", "save-config", "open-dataset-panel"]);

// 响应式数据
const dialogVisible = computed({
  get: () => props.visible,
  set: (val) => emit("update:visible", val)
});

const activeTab = ref("basic");
const previewChartRef = ref<HTMLElement>();
let previewChartInstance: echarts.ECharts | null = null;
const fullscreenPreview = ref(false);
const previewLoading = ref(false);

// 表单数据
const formData = ref({
  type: "line",
  title: "数据趋势图",
  datasetId: "",
  dataPath: "data",
  refreshInterval: 5000,
  theme: "default",
  width: 500,
  height: 350,
  showLegend: true,
  legendPosition: "bottom",
  showToolbox: true,
  showGrid: true,
  enableAnimation: true,
  enableDataZoom: false,
  smoothLine: true,
  colorScheme: "default"
});

// 颜色方案
const colorSchemes = {
  default: ['#5470c6', '#91cc75', '#fac858', '#ee6666', '#73c0de', '#3ba272', '#fc8452', '#9a60b4', '#ea7ccc'],
  warm: ['#ff6b6b', '#f06595', '#cc5de8', '#845ef7', '#5c7cfa', '#339af0', '#22b8cf', '#20c997', '#51cf66'],
  cool: ['#1890ff', '#13c2c2', '#52c41a', '#faad14', '#f5222d', '#722ed1', '#eb2f96', '#fa8c16', '#a0d911'],
  vibrant: ['#ff0080', '#7928ca', '#ff0080', '#ff4d4f', '#faad14', '#52c41a', '#1890ff', '#722ed1', '#eb2f96']
};

// ECharts主题配置
const chartThemes = {
  'default': {
    color: null, // 使用默认配色
    backgroundColor: 'transparent'
  },
  'dark': {
    color: ['#c23531', '#2f4554', '#61a0a8', '#d48265', '#91c7ae', '#749f83'],
    backgroundColor: '#100C2A',
    textStyle: { color: '#ffffff' }
  },
  'light': {
    color: ['#5470c6', '#91cc75', '#fac858', '#ee6666', '#73c0de', '#3ba272'],
    backgroundColor: '#ffffff',
    textStyle: { color: '#333333' }
  },
  'tech-blue': {
    color: ['#4facfe', '#00f2fe', '#43e97b', '#38f9d7', '#fa709a', '#fee140'],
    backgroundColor: 'rgba(0, 0, 0, 0.02)'
  },
  'pink-dream': {
    color: ['#f093fb', '#f5576c', '#fa709a', '#fee140', '#30cfd0', '#a8edea'],
    backgroundColor: 'rgba(255, 240, 245, 0.3)'
  },
  'nature-green': {
    color: ['#11998e', '#38ef7d', '#43e97b', '#38f9d7', '#17ead9', '#6078ea'],
    backgroundColor: 'rgba(240, 255, 244, 0.3)'
  },
  'sunset-orange': {
    color: ['#ff6b6b', '#ff8e53', '#ffd93d', '#6bcf7f', '#4d96ff', '#a78bfa'],
    backgroundColor: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)'
  },
  'ocean-blue': {
    color: ['#0093E9', '#80D0C7', '#13547a', '#009ffd', '#2a2a72', '#00c9ff'],
    backgroundColor: 'rgba(0, 147, 233, 0.05)'
  },
  'business': {
    color: ['#2c3e50', '#34495e', '#7f8c8d', '#95a5a6', '#bdc3c7', '#ecf0f1'],
    backgroundColor: '#f8f9fa',
    textStyle: { color: '#2c3e50', fontFamily: 'Arial, sans-serif' }
  },
  'minimal-bw': {
    color: ['#000000', '#333333', '#666666', '#999999', '#cccccc', '#eeeeee'],
    backgroundColor: '#ffffff',
    textStyle: { color: '#000000', fontFamily: 'Helvetica, Arial, sans-serif' }
  },
  'heatmap': {
    color: ['#313695', '#4575b4', '#74add1', '#abd9e9', '#fee090', '#fdae61', '#f46d43', '#d73027', '#a50026'],
    backgroundColor: '#fafafa'
  },
  'rainbow': {
    color: ['#FF0080', '#FF8C00', '#FFD700', '#00FF00', '#00CED1', '#1E90FF', '#9370DB', '#FF1493'],
    backgroundColor: 'rgba(255, 255, 255, 0.95)'
  }
};

// 主题选项（用于UI展示）
const themeOptions = [
  { label: '默认主题', value: 'default', icon: '🎨', colors: ['#5470c6', '#91cc75', '#fac858'], category: 'basic' },
  { label: '深色主题', value: 'dark', icon: '🌙', colors: ['#c23531', '#2f4554', '#61a0a8'], category: 'basic' },
  { label: '浅色主题', value: 'light', icon: '☀️', colors: ['#5470c6', '#91cc75', '#fac858'], category: 'basic' },
  { label: '科技蓝', value: 'tech-blue', icon: '🎭', colors: ['#4facfe', '#00f2fe', '#43e97b'], category: 'gradient' },
  { label: '粉色梦幻', value: 'pink-dream', icon: '🌸', colors: ['#f093fb', '#f5576c', '#fa709a'], category: 'gradient' },
  { label: '自然绿', value: 'nature-green', icon: '🌿', colors: ['#11998e', '#38ef7d', '#43e97b'], category: 'gradient' },
  { label: '日落橙', value: 'sunset-orange', icon: '🌅', colors: ['#ff6b6b', '#ff8e53', '#ffd93d'], category: 'gradient' },
  { label: '海洋蓝', value: 'ocean-blue', icon: '🌊', colors: ['#0093E9', '#80D0C7', '#13547a'], category: 'gradient' },
  { label: '商务风格', value: 'business', icon: '💼', colors: ['#2c3e50', '#34495e', '#7f8c8d'], category: 'professional' },
  { label: '极简黑白', value: 'minimal-bw', icon: '🎯', colors: ['#000000', '#666666', '#cccccc'], category: 'professional' },
  { label: '热力图谱', value: 'heatmap', icon: '🔥', colors: ['#313695', '#fee090', '#d73027'], category: 'professional' },
  { label: '彩虹渐变', value: 'rainbow', icon: '🌈', colors: ['#FF0080', '#FFD700', '#1E90FF'], category: 'professional' }
];

// 监听对话框打开，初始化数据
watch(
  () => props.visible,
  (newVal) => {
    if (newVal && props.chartComponent?.chartConfig) {
      formData.value = {
        ...formData.value,
        ...props.chartComponent.chartConfig
      };
    }
  }
);

// 监听activeTab变化，如果切换到预览标签页则初始化预览
watch(activeTab, (newTab) => {
  if (newTab === "preview") {
    nextTick(() => {
      initPreviewChart();
    });
  }
});

// 监听图表类型、主题、尺寸变化，自动刷新预览
watch([() => formData.value.type, () => formData.value.theme, () => formData.value.width, () => formData.value.height, () => formData.value.colorScheme], () => {
  if (activeTab.value === "preview") {
    nextTick(() => {
      initPreviewChart();
    });
  }
});

// 监听全屏状态变化，添加/移除ESC键监听
watch(fullscreenPreview, (isFullscreen) => {
  if (isFullscreen) {
    // 进入全屏时添加ESC键监听
    document.addEventListener('keydown', handleEscapeKey);
  } else {
    // 退出全屏时移除ESC键监听
    document.removeEventListener('keydown', handleEscapeKey);
  }
});

// 处理ESC键退出全屏
const handleEscapeKey = (event: KeyboardEvent) => {
  if (event.key === 'Escape' && fullscreenPreview.value) {
    fullscreenPreview.value = false;
  }
};

// 处理点击背景退出全屏
const handleFullscreenClick = (event: MouseEvent) => {
  // 只有点击背景区域（不是图表）才退出全屏
  if (fullscreenPreview.value && event.target === event.currentTarget) {
    fullscreenPreview.value = false;
  }
};

// 获取图表类型推荐尺寸
const getChartRecommendedSize = (chartType: string) => {
  const sizeMap = {
    'line': { width: 500, height: 300 },      // 折线图：中等宽度
    'bar': { width: 500, height: 300 },       // 柱状图：中等宽度
    'pie': { width: 450, height: 450 },       // 饼图：正方形更好
    'area': { width: 500, height: 300 },      // 面积图：中等宽度
    'gauge': { width: 450, height: 350 },     // 仪表盘：宽度略大于高度，适合半圆形仪表
    'radar': { width: 500, height: 500 },     // 雷达图：需要更大的正方形空间
    'funnel': { width: 450, height: 500 },    // 漏斗图：需要更高
    'scatter': { width: 500, height: 400 },   // 散点图：中等偏宽
    'candlestick': { width: 600, height: 400 } // K线图：需要更宽
  };

  return sizeMap[chartType] || { width: 400, height: 300 }; // 默认尺寸
};

// 图表类型变化处理
const handleChartTypeChange = () => {
  // 自动调整为推荐尺寸
  const recommendedSize = getChartRecommendedSize(formData.value.type);
  formData.value.width = recommendedSize.width;
  formData.value.height = recommendedSize.height;

  ElMessage.info(`已切换到${getChartTypeName(formData.value.type)}，尺寸已自动调整为 ${recommendedSize.width}×${recommendedSize.height}`);

  if (activeTab.value === "preview") {
    nextTick(() => {
      initPreviewChart();
    });
  }
};

// 数据集变化处理
const handleDatasetChange = () => {
  const dataset = props.datasetList.find(
    (ds: any) => ds.id === formData.value.datasetId
  );
  if (!dataset) return;

  ElMessage.success(`已选择数据集: ${dataset.name}`);
};

// 打开数据集配置面板
const openDatasetPanel = () => {
  emit("open-dataset-panel");
};

// 获取数据集类型名称
const getDatasetTypeName = (type: string) => {
  const names = {
    api: "API",
    mqtt: "MQTT",
    static: "静态数据"
  };
  return names[type] || type;
};

// 获取当前绑定的数据集名称
const getCurrentDatasetName = () => {
  const dataset = props.datasetList.find(
    (ds: any) => ds.id === formData.value.datasetId
  );
  return dataset ? dataset.name : "未选择";
};

// 测试数据路径
const testDataPath = () => {
  ElMessage.info("数据路径测试功能待实现");
};

// 初始化预览图表
const initPreviewChart = () => {
  if (!previewChartRef.value) return;

  // 销毁旧的实例
  if (previewChartInstance) {
    previewChartInstance.dispose();
  }

  // 创建新实例
  previewChartInstance = echarts.init(previewChartRef.value);

  // 生成配置并渲染
  const options = generateChartOptions();
  previewChartInstance.setOption(options);

  // 启用响应式
  window.addEventListener('resize', () => {
    previewChartInstance?.resize();
  });
};

// 生成图表配置（使用示例数据进行预览）
const generateChartOptions = () => {
  const colors = colorSchemes[formData.value.colorScheme] || colorSchemes.default;
  const themeConfig = chartThemes[formData.value.theme] || {};

  // 获取主题文本颜色，优先使用主题配置，否则根据背景色自动判断
  const getTextColor = () => {
    if (themeConfig.textStyle?.color) return themeConfig.textStyle.color;
    if (formData.value.theme === 'dark') return '#ffffff';
    return '#333333';
  };

  const textColor = getTextColor();

  const baseConfig = {
    color: themeConfig.color || colors,
    backgroundColor: themeConfig.backgroundColor || 'transparent',
    textStyle: themeConfig.textStyle || { color: textColor },
    title: {
      text: formData.value.title,
      left: "center",
      top: 10,
      textStyle: {
        fontSize: 16,
        fontWeight: 600,
        color: textColor,
        ...(themeConfig.textStyle?.fontFamily && { fontFamily: themeConfig.textStyle.fontFamily })
      }
    },
    tooltip: {
      trigger: formData.value.type === 'pie' ? 'item' : 'axis',
      backgroundColor: 'rgba(50, 50, 50, 0.9)',
      borderColor: '#333',
      borderWidth: 0,
      textStyle: {
        color: '#fff'
      },
      axisPointer: {
        type: 'shadow',
        shadowStyle: {
          color: 'rgba(150, 150, 150, 0.1)'
        }
      }
    },
    legend: formData.value.showLegend ? {
      [formData.value.legendPosition]: 10,
      left: ['left', 'right'].includes(formData.value.legendPosition) ? formData.value.legendPosition : 'center',
      orient: ['left', 'right'].includes(formData.value.legendPosition) ? 'vertical' : 'horizontal',
      textStyle: {
        fontSize: 12,
        color: textColor
      },
      itemWidth: 25,
      itemHeight: 14
    } : undefined,
    dataZoom: formData.value.enableDataZoom && !['pie', 'gauge', 'radar', 'funnel'].includes(formData.value.type) ? [
      {
        type: 'slider',
        show: true,
        start: 0,
        end: 100,
        height: 20,
        bottom: 10
      },
      {
        type: 'inside',
        start: 0,
        end: 100
      }
    ] : undefined,
    toolbox: formData.value.showToolbox ? {
      feature: {
        saveAsImage: {
          title: '保存为图片',
          pixelRatio: 2
        },
        dataView: {
          title: '数据视图',
          readOnly: true
        },
        restore: {
          title: '还原'
        }
      },
      right: 20,
      top: 10
    } : undefined,
    grid: formData.value.showGrid ? {
      left: '3%',
      right: '4%',
      bottom: '10%',
      top: '15%',
      containLabel: true
    } : undefined,
    animationDuration: formData.value.enableAnimation ? 1000 : 0,
    animationEasing: 'cubicOut'
  };

  // 根据图表类型生成不同的配置和数据
  switch (formData.value.type) {
    case 'line':
      return generateLineChart(baseConfig);
    case 'bar':
      return generateBarChart(baseConfig);
    case 'pie':
      return generatePieChart(baseConfig);
    case 'area':
      return generateAreaChart(baseConfig);
    case 'gauge':
      return generateGaugeChart(baseConfig);
    case 'radar':
      return generateRadarChart(baseConfig);
    case 'funnel':
      return generateFunnelChart(baseConfig);
    case 'scatter':
      return generateScatterChart(baseConfig);
    case 'candlestick':
      return generateCandlestickChart(baseConfig);
    default:
      return generateLineChart(baseConfig);
  }
};

// 折线图配置
const generateLineChart = (baseConfig: any) => {
  const textColor = baseConfig.textStyle?.color || '#666';

  return {
    ...baseConfig,
    xAxis: {
      type: 'category',
      data: ['1月', '2月', '3月', '4月', '5月', '6月', '7月'],
      axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
      axisLabel: { color: textColor }
    },
    yAxis: {
      type: 'value',
      axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
      axisLabel: { color: textColor },
      splitLine: { lineStyle: { color: textColor, opacity: 0.1 } }
    },
    series: [
      {
        name: '销售额',
        type: 'line',
        data: [152, 188, 245, 298, 342, 389, 425],
        smooth: formData.value.smoothLine,
        lineStyle: { width: 3 },
        symbolSize: 8,
        areaStyle: {
          opacity: 0.1
        },
        markPoint: {
          data: [
            { type: 'max', name: '最大值' },
            { type: 'min', name: '最小值' }
          ]
        },
        markLine: {
          data: [{ type: 'average', name: '平均值' }]
        }
      },
      {
        name: '利润',
        type: 'line',
        data: [95, 118, 156, 189, 217, 248, 272],
        smooth: formData.value.smoothLine,
        lineStyle: { width: 3 },
        symbolSize: 8,
        areaStyle: {
          opacity: 0.1
        }
      },
      {
        name: '成本',
        type: 'line',
        data: [57, 70, 89, 109, 125, 141, 153],
        smooth: formData.value.smoothLine,
        lineStyle: { width: 2, type: 'dashed' },
        symbolSize: 6
      }
    ]
  };
};

// 柱状图配置
const generateBarChart = (baseConfig: any) => {
  const textColor = baseConfig.textStyle?.color || '#666';

  return {
    ...baseConfig,
    xAxis: {
      type: 'category',
      data: ['智能手机', '平板电脑', '笔记本', '台式机', '智能手表', '耳机'],
      axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
      axisLabel: { color: textColor, rotate: 20 }
    },
    yAxis: {
      type: 'value',
      axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
      axisLabel: { color: textColor },
      splitLine: { lineStyle: { color: textColor, opacity: 0.1 } },
      name: '销量（千台）'
    },
    series: [
      {
        name: '2023年',
        type: 'bar',
        data: [452, 328, 615, 398, 287, 534],
        barWidth: '35%',
        itemStyle: {
          borderRadius: [8, 8, 0, 0],
          shadowColor: 'rgba(0, 0, 0, 0.1)',
          shadowBlur: 10
        },
        label: {
          show: true,
          position: 'top',
          formatter: '{c}千台'
        }
      },
      {
        name: '2024年',
        type: 'bar',
        data: [528, 389, 687, 445, 356, 612],
        barWidth: '35%',
        itemStyle: {
          borderRadius: [8, 8, 0, 0],
          shadowColor: 'rgba(0, 0, 0, 0.1)',
          shadowBlur: 10
        },
        label: {
          show: true,
          position: 'top',
          formatter: '{c}千台'
        }
      }
    ]
  };
};

// 饼图配置
const generatePieChart = (baseConfig: any) => {
  return {
    ...baseConfig,
    tooltip: {
      trigger: 'item',
      formatter: '{a} <br/>{b}: {c} ({d}%)'
    },
    series: [
      {
        name: '市场份额',
        type: 'pie',
        radius: ['40%', '70%'],
        center: ['50%', '50%'],
        data: [
          { value: 3850, name: '移动端' },
          { value: 2635, name: 'PC端' },
          { value: 1548, name: '平板' },
          { value: 892, name: '智能TV' },
          { value: 475, name: '其他' }
        ],
        emphasis: {
          itemStyle: {
            shadowBlur: 15,
            shadowOffsetX: 0,
            shadowColor: 'rgba(0, 0, 0, 0.5)'
          },
          label: {
            show: true,
            fontSize: 14,
            fontWeight: 'bold'
          }
        },
        label: {
          formatter: '{b}\n{d}%',
          fontSize: 12,
          lineHeight: 18
        },
        labelLine: {
          length: 15,
          length2: 8,
          smooth: true
        },
        itemStyle: {
          borderRadius: 10,
          borderColor: '#fff',
          borderWidth: 3
        }
      }
    ]
  };
};

// 面积图配置
const generateAreaChart = (baseConfig: any) => {
  const textColor = baseConfig.textStyle?.color || '#666';
  const primaryColor = baseConfig.color?.[0] || '#5470c6';

  return {
    ...baseConfig,
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: ['周一', '周二', '周三', '周四', '周五', '周六', '周日'],
      axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
      axisLabel: { color: textColor }
    },
    yAxis: {
      type: 'value',
      axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
      axisLabel: { color: textColor },
      splitLine: { lineStyle: { color: textColor, opacity: 0.1 } }
    },
    series: [
      {
        name: '网站访问量',
        type: 'line',
        data: [1250, 1420, 1380, 1650, 2100, 2350, 2280],
        smooth: formData.value.smoothLine,
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: `${primaryColor}80` },
            { offset: 1, color: `${primaryColor}0D` }
          ])
        },
        lineStyle: { width: 3 },
        markArea: {
          data: [
            [
              {
                name: '周末高峰',
                xAxis: '周六'
              },
              {
                xAxis: '周日'
              }
            ]
          ],
          itemStyle: {
            color: 'rgba(255, 173, 177, 0.2)'
          }
        }
      }
    ]
  };
};

// 仪表盘配置
const generateGaugeChart = (baseConfig: any) => {
  return {
    ...baseConfig,
    series: [
      {
        name: '指标',
        type: 'gauge',
        radius: '80%',
        startAngle: 200,
        endAngle: -20,
        min: 0,
        max: 100,
        splitNumber: 10,
        progress: {
          show: true,
          width: 18,
          itemStyle: {
            color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
              { offset: 0, color: '#5470c6' },
              { offset: 1, color: '#91cc75' }
            ])
          }
        },
        axisLine: {
          lineStyle: {
            width: 18,
            color: [[1, '#e0e0e0']]
          }
        },
        axisTick: {
          show: true,
          splitNumber: 5,
          lineStyle: {
            width: 2,
            color: '#999'
          }
        },
        splitLine: {
          length: 12,
          lineStyle: {
            width: 3,
            color: '#999'
          }
        },
        axisLabel: {
          distance: 25,
          color: '#666',
          fontSize: 12
        },
        anchor: {
          show: true,
          showAbove: true,
          size: 20,
          itemStyle: {
            borderWidth: 8,
            borderColor: '#5470c6'
          }
        },
        title: {
          show: false
        },
        detail: {
          valueAnimation: true,
          fontSize: 36,
          fontWeight: 'bold',
          offsetCenter: [0, '50%'],
          formatter: '{value}%',
          color: '#5470c6'
        },
        data: [
          { value: 83.6, name: '项目完成率' }
        ]
      }
    ]
  };
};

// 雷达图配置
const generateRadarChart = (baseConfig: any) => {
  return {
    ...baseConfig,
    radar: {
      indicator: [
        { name: '产品质量', max: 100 },
        { name: '服务态度', max: 100 },
        { name: '价格优势', max: 100 },
        { name: '物流速度', max: 100 },
        { name: '售后服务', max: 100 },
        { name: '用户体验', max: 100 }
      ],
      shape: 'circle',
      splitNumber: 4,
      name: {
        textStyle: {
          color: '#666',
          fontSize: 12
        }
      },
      splitLine: {
        lineStyle: {
          color: '#ddd'
        }
      },
      splitArea: {
        show: true,
        areaStyle: {
          color: ['rgba(114, 172, 209, 0.05)', 'rgba(114, 172, 209, 0.1)']
        }
      },
      axisLine: {
        lineStyle: {
          color: '#ddd'
        }
      }
    },
    series: [
      {
        name: '评分',
        type: 'radar',
        data: [
          {
            value: [92, 88, 85, 90, 86, 91],
            name: '本月得分',
            itemStyle: {
              color: '#5470c6'
            },
            areaStyle: {
              color: 'rgba(84, 112, 198, 0.3)'
            },
            lineStyle: {
              width: 2
            }
          },
          {
            value: [85, 92, 78, 88, 90, 87],
            name: '上月得分',
            itemStyle: {
              color: '#91cc75'
            },
            areaStyle: {
              color: 'rgba(145, 204, 117, 0.3)'
            },
            lineStyle: {
              width: 2
            }
          }
        ]
      }
    ]
  };
};

// 漏斗图配置
const generateFunnelChart = (baseConfig: any) => {
  return {
    ...baseConfig,
    series: [
      {
        name: '转化漏斗',
        type: 'funnel',
        left: '10%',
        top: 60,
        bottom: 60,
        width: '80%',
        min: 0,
        max: 100,
        minSize: '0%',
        maxSize: '100%',
        sort: 'descending',
        gap: 2,
        label: {
          show: true,
          position: 'inside',
          formatter: '{b}: {c}%',
          color: '#fff',
          fontSize: 14
        },
        labelLine: {
          length: 10,
          lineStyle: {
            width: 1,
            type: 'solid'
          }
        },
        itemStyle: {
          borderColor: '#fff',
          borderWidth: 2
        },
        emphasis: {
          label: {
            fontSize: 16
          }
        },
        data: [
          { value: 10000, name: '网站访问' },
          { value: 6500, name: '浏览商品' },
          { value: 3200, name: '加入购物车' },
          { value: 1500, name: '提交订单' },
          { value: 980, name: '完成支付' }
        ]
      }
    ]
  };
};

// 散点图配置
const generateScatterChart = (baseConfig: any) => {
  const textColor = baseConfig.textStyle?.color || '#666';

  // 生成更真实的价格-销量数据（价格越高，销量通常越低，但有一定波动）
  const productData = [];
  for (let i = 0; i < 60; i++) {
    const price = 10 + Math.random() * 290; // 价格范围 10-300
    const baseSales = 1000 - price * 2.5; // 基础销量与价格成反比
    const sales = Math.max(50, baseSales + (Math.random() - 0.5) * 400); // 添加随机波动
    productData.push([price, Math.round(sales)]);
  }

  return {
    ...baseConfig,
    title: {
      ...baseConfig.title,
      subtext: '价格与销量关系分析',
      subtextStyle: {
        color: textColor,
        fontSize: 12
      }
    },
    xAxis: {
      type: 'value',
      name: '价格(元)',
      nameLocation: 'middle',
      nameGap: 30,
      nameTextStyle: {
        color: textColor,
        fontSize: 12
      },
      axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
      axisLabel: {
        color: textColor,
        formatter: '¥{value}'
      },
      splitLine: { lineStyle: { color: textColor, opacity: 0.1 } }
    },
    yAxis: {
      type: 'value',
      name: '销量(件)',
      nameLocation: 'middle',
      nameGap: 40,
      nameTextStyle: {
        color: textColor,
        fontSize: 12
      },
      axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
      axisLabel: {
        color: textColor,
        formatter: '{value}'
      },
      splitLine: { lineStyle: { color: textColor, opacity: 0.1 } }
    },
    series: [
      {
        name: '产品',
        type: 'scatter',
        symbolSize: 12,
        data: productData,
        itemStyle: {
          shadowBlur: 10,
          shadowColor: 'rgba(84, 112, 198, 0.5)',
          shadowOffsetY: 3,
          opacity: 0.8
        },
        emphasis: {
          itemStyle: {
            shadowBlur: 15,
            opacity: 1
          }
        }
      }
    ]
  };
};

// K线图配置
const generateCandlestickChart = (baseConfig: any) => {
  const textColor = baseConfig.textStyle?.color || '#666';

  return {
    ...baseConfig,
    title: {
      ...baseConfig.title,
      subtext: '股票价格走势',
      subtextStyle: {
        color: textColor,
        fontSize: 12
      }
    },
    tooltip: {
      trigger: 'axis',
      axisPointer: {
        type: 'cross'
      },
      backgroundColor: 'rgba(50, 50, 50, 0.9)',
      borderColor: '#333',
      borderWidth: 0,
      textStyle: {
        color: '#fff'
      },
      formatter: function (params: any) {
        const data = params[0].data;
        return `${params[0].name}<br/>
                开盘: ${data[1]}<br/>
                收盘: ${data[2]}<br/>
                最低: ${data[3]}<br/>
                最高: ${data[4]}`;
      }
    },
    xAxis: {
      type: 'category',
      data: ['2024/12/16', '2024/12/17', '2024/12/18', '2024/12/19', '2024/12/20', '2024/12/23', '2024/12/24', '2024/12/25', '2024/12/26', '2024/12/27'],
      axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
      axisLabel: {
        color: textColor,
        rotate: 30,
        fontSize: 11
      },
      splitLine: {
        show: false
      }
    },
    yAxis: {
      type: 'value',
      name: '股价(元)',
      nameTextStyle: {
        color: textColor,
        fontSize: 12
      },
      scale: true,
      axisLine: { lineStyle: { color: textColor, opacity: 0.3 } },
      axisLabel: {
        color: textColor,
        formatter: '¥{value}'
      },
      splitLine: { lineStyle: { color: textColor, opacity: 0.1 } }
    },
    series: [
      {
        name: 'K线',
        type: 'candlestick',
        data: [
          [45.2, 48.6, 44.8, 49.2],  // [开盘, 收盘, 最低, 最高]
          [48.5, 46.3, 45.9, 49.1],
          [46.2, 47.8, 45.5, 48.5],
          [47.9, 45.2, 44.6, 48.3],
          [45.1, 49.5, 44.9, 50.2],
          [49.6, 52.3, 49.2, 53.1],
          [52.2, 50.8, 50.1, 53.5],
          [50.7, 53.8, 50.3, 54.6],
          [53.9, 51.2, 50.8, 54.8],
          [51.3, 54.5, 51.0, 55.3]
        ],
        itemStyle: {
          color: '#ec0000',      // 阳线颜色（收盘>开盘）
          color0: '#00da3c',     // 阴线颜色（收盘<开盘）
          borderColor: '#ec0000',
          borderColor0: '#00da3c',
          borderWidth: 1.5
        },
        emphasis: {
          itemStyle: {
            borderWidth: 2
          }
        }
      }
    ],
    dataZoom: [
      {
        type: 'inside',
        start: 0,
        end: 100
      },
      {
        show: true,
        type: 'slider',
        bottom: 10,
        start: 0,
        end: 100,
        height: 20
      }
    ]
  };
};

// 预览
const handlePreview = () => {
  activeTab.value = "preview";
  nextTick(() => {
    initPreviewChart();
  });
};

// 保存配置
const handleSave = () => {
  emit("save-config", { ...formData.value });
  ElMessage.success("图表配置已保存");
  dialogVisible.value = false;
};

// 关闭对话框
const handleClose = () => {
  dialogVisible.value = false;
};

// 辅助函数
const getChartTypeName = (type: string) => {
  const names = {
    line: "折线图",
    bar: "柱状图",
    pie: "饼图",
    area: "面积图",
    gauge: "仪表盘",
    radar: "雷达图",
    funnel: "漏斗图",
    scatter: "散点图",
    candlestick: "K线图"
  };
  return names[type] || type;
};
</script>

<style scoped lang="scss">
// 图表选项样式
.chart-option {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 4px 0;

  .chart-icon {
    font-size: 24px;
    line-height: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    min-width: 28px;
    height: 28px;
  }

  .chart-info {
    flex: 1;
    display: flex;
    flex-direction: column;
    justify-content: center;

    .chart-name {
      font-size: 14px;
      font-weight: 500;
      color: #303133;
      margin-bottom: 2px;
      line-height: 1.4;
    }

    .chart-desc {
      font-size: 12px;
      color: #909399;
      line-height: 1.4;
    }
  }
}

// 对话框整体样式
.chart-property-dialog {
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
.chart-dialog-content {
  height: 65vh;
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
        padding: 0 20px;
        height: 40px;
        line-height: 40px;
        font-size: 13px;

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
      height: calc(100% - 40px);
      overflow-y: auto;
      padding: 20px;
      background: #ffffff;

      .el-tab-pane {
        height: 100%;
      }
    }
  }
}

// 表单样式
.chart-form {
  max-width: 700px;

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

// 主题预设网格
.theme-preset-grid {
  width: 100%;

  .theme-preset-btn {
    position: relative;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 8px 4px;
    background: #ffffff;
    border: 1.5px solid #dcdfe6;
    border-radius: 4px;
    cursor: pointer;
    transition: all 0.2s ease;
    min-height: 70px;

    &:hover {
      border-color: #409eff;
      background: #f0f7ff;
      transform: translateY(-1px);
      box-shadow: 0 2px 8px rgba(64, 158, 255, 0.15);
    }

    &.active {
      border-color: #409eff;
      border-width: 2px;
      background: linear-gradient(135deg, #f0f7ff 0%, #e6f4ff 100%);
      box-shadow: 0 0 0 2px rgba(64, 158, 255, 0.2);

      &::after {
        content: '✓';
        position: absolute;
        top: 2px;
        right: 2px;
        width: 16px;
        height: 16px;
        background: #409eff;
        color: white;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 10px;
        font-weight: bold;
        line-height: 1;
      }
    }

    .theme-icon {
      font-size: 22px;
      line-height: 1;
      margin-bottom: 4px;
    }

    .theme-name {
      font-size: 11px;
      font-weight: 500;
      color: #303133;
      margin-bottom: 4px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 100%;
    }

    .theme-colors {
      display: flex;
      gap: 3px;
      justify-content: center;
      align-items: center;

      .color-dot {
        width: 10px;
        height: 10px;
        border-radius: 50%;
        border: 1px solid rgba(255, 255, 255, 0.8);
        box-shadow: 0 1px 2px rgba(0, 0, 0, 0.2);
      }
    }
  }
}

// 数据配置区域
.data-config-section {
  margin-top: 20px;
  padding: 16px;
  background: linear-gradient(135deg, #f8fafc 0%, #edf2f7 100%);
  border-radius: 8px;
  border: 1px solid #e2e8f0;
}

// 预览容器
.chart-preview-container {
  display: flex;
  flex-direction: column;
  min-height: 500px;
  background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
  border-radius: 12px;
  padding: 20px;

  .preview-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    padding: 16px;
    background: linear-gradient(135deg, rgba(255, 255, 255, 0.9) 0%, rgba(255, 255, 255, 0.7) 100%);
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);

    .preview-info {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
      align-items: center;
    }
  }

  .preview-chart-wrapper {
    position: relative;
    display: flex;
    justify-content: center;
    align-items: center;
    flex: 1;
    min-height: 350px;
    background: #ffffff;
    border-radius: 12px;
    padding: 20px;
    box-shadow:
      0 4px 12px rgba(0, 0, 0, 0.08),
      0 0 0 1px rgba(0, 0, 0, 0.05);
    transition: all 0.3s ease;

    &:hover {
      box-shadow:
        0 8px 24px rgba(0, 0, 0, 0.12),
        0 0 0 1px rgba(102, 126, 234, 0.2);
    }

    &.fullscreen {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      z-index: 9999;
      border-radius: 0;
      background: #ffffff;
      padding: 40px;

      .preview-chart {
        max-width: 90vw !important;
        max-height: 80vh !important;
        width: auto !important;
        height: auto !important;
      }
    }

    .preview-chart {
      transition: all 0.3s ease;
    }

    .fullscreen-exit-btn {
      position: absolute;
      top: 20px;
      right: 20px;
      z-index: 10000;
      width: 48px;
      height: 48px;
      background: rgba(0, 0, 0, 0.6);
      border: 2px solid rgba(255, 255, 255, 0.3);
      color: white;
      backdrop-filter: blur(10px);
      transition: all 0.3s ease;

      &:hover {
        background: rgba(0, 0, 0, 0.8);
        border-color: rgba(255, 255, 255, 0.5);
        transform: scale(1.1);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
      }

      .el-icon {
        font-size: 24px;
      }
    }

    .preview-loading {
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 10px;
      color: #667eea;
      font-size: 14px;

      .el-icon {
        font-size: 32px;
      }
    }
  }

  .preview-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: 16px;
    padding: 12px 16px;
    background: linear-gradient(135deg, rgba(255, 255, 255, 0.95) 0%, rgba(255, 255, 255, 0.85) 100%);
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);

    .footer-left {
      display: flex;
      align-items: center;
      gap: 8px;
      color: #606266;
      font-size: 13px;

      .el-icon {
        color: #409eff;
      }
    }

    .footer-right {
      display: flex;
      align-items: center;
      gap: 8px;
    }
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

    &.el-button--success {
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      border: none;

      &:hover {
        background: linear-gradient(135deg, #059669 0%, #047857 100%);
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);
      }
    }

    .el-icon {
      margin-right: 4px;
    }
  }
}
</style>
