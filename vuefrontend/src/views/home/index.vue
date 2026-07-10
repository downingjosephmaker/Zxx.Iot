<script setup>
import { ref, onMounted, onUnmounted, nextTick, reactive, watch } from "vue";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { useMenu } from "./utils/hook";
import { useRouter } from "vue-router";
import dayjs from "dayjs";
// 导入图标
import boltIcon from "~icons/material-symbols/bolt";
import clockIcon from "~icons/ep/clock";
import sunnyIcon from "~icons/ep/sunny";
import solarPanelIcon from "~icons/material-symbols/solar-power";
import plugIcon from "~icons/ri/plug-line";
import leafIcon from "~icons/material-symbols/eco";
import scaleBalanceIcon from "~icons/ri/scales-line";
import cloudSmogIcon from "~icons/ri/cloud-windy-line";
import batteryIcon from "~icons/material-symbols/battery-charging-full";
import chartLineIcon from "~icons/ri/line-chart-line";
import warehouseIcon from "~icons/material-symbols/warehouse";
import warningIcon from "~icons/ep/warning";
import carBatteryIcon from "~icons/ri/battery-2-charge-line";
import chartAreaIcon from "~icons/ri/bar-chart-2-line";
import buildingIcon from "~icons/ep/office-building";
import expandIcon from "~icons/ep/full-screen";
import arrowUpIcon from "~icons/ep/arrow-up";
import arrowDownIcon from "~icons/ep/arrow-down";
import settingsIcon from "~icons/ep/setting";
import calendarIcon from "~icons/ep/calendar";
import waterIcon from "~icons/ri/water-flash-line";
import parameterIcon from "~icons/ep/data-line"; // 添加参数图标
import refresh from "~icons/material-symbols/refresh";
import NumberAnimation from "@/components/NumberAnimation.vue"; // 导入数字动画组件
import * as echarts from "echarts";

// 组件名称
defineOptions({
  name: "EnergyDashboard"
});

// 获取路由实例
const router = useRouter();
// 从 useMenu 中获取所有必要的变量和函数
const {
  energyCompareChartRef,
  hydrogenChartRef,
  warehouseChartRef,
  waterChartRef,
  solarChartRef,
  forkliftChargerChartRef,
  floorEnergyChartRef,
  airEnergyChartRef,
  alertData,
  // 新增从 hook 中解构的变量和函数
  dateType,
  dateOptions,
  energyData,
  titleMapping,
  handleDateTypeChange,
  chargerMonitorData,
  switchChargerMonitorType,
  alertLoading,
  valueUpdateFlags,
  fetchEnergyData,
  fetchAlarmList,
  // 添加刷新所有数据的函数
  refreshAllData,
  // 演示模式相关
  demoModeStore
} = useMenu();

// 响应式数据
const currentTime = ref("");
const timeInterval = ref(null);

// 演示日期选择器相关
const selectedDemoDate = ref("");

// 方法：更新日期时间
const updateTime = () => {
  const now = new Date();
  const options = {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    weekday: "long"
  };
  currentTime.value = now.toLocaleDateString("zh-CN", options);
};

// 模块展开/收起状态
const moduleCollapsed = reactive({
  energy: false,
  warehouse: false,
  solar: false,
  alerts: false,
  floor: false,
  water: false,
  air: false
});

// 方法：切换模块展开/收起状态
const toggleModule = moduleId => {
  const key = moduleId.replace("Module", "");
  moduleCollapsed[key] = !moduleCollapsed[key];
};

// 演示日期选择器相关方法
const disabledDate = time => {
  // 只允许选择2025年8月的日期
  const date = new Date(time);
  const year = date.getFullYear();
  const month = date.getMonth() + 1;
  return year !== 2025 || month !== 8;
};

const handleDemoDateChange = value => {
  if (value) {
    const date = new Date(value);
    demoModeStore.setDemoDateByDate(date);
    // 刷新数据以显示新选择日期的数据
    refreshAllData();
  }
};

// 自动刷新数据的间隔时间（毫秒）
const AUTO_REFRESH_INTERVAL = 20000; // 每20秒刷新一次

// 定时刷新任务
let refreshInterval = null;

// 监听模式变化
watch(
  () => demoModeStore.currentMode,
  newValue => {
    console.log("🚀 ~ 模式已切换:", newValue, demoModeStore.currentModeText);
    if (newValue === "demo") {
      // 切换到演示模式时，同步日期选择器的值
      selectedDemoDate.value = `${demoModeStore.demoYear}-${demoModeStore.demoMonth}-${demoModeStore.demoDay}`;
    }
    // 刷新数据
    refreshAllData();
  }
);

// 监听演示日期变化
watch(
  () => [
    demoModeStore.demoYear,
    demoModeStore.demoMonth,
    demoModeStore.demoDay
  ],
  () => {
    if (demoModeStore.isDemoMode) {
      selectedDemoDate.value = `${demoModeStore.demoYear}-${demoModeStore.demoMonth}-${demoModeStore.demoDay}`;
    }
  }
);

// 在组件挂载时开始每分钟定时刷新
onMounted(() => {
  updateTime();
  // 初始化演示模式状态
  demoModeStore.initDemoMode();

  // 初始化演示日期选择器的值
  selectedDemoDate.value = `${demoModeStore.demoYear}-${demoModeStore.demoMonth}-${demoModeStore.demoDay}`;

  // 更新时间的定时任务
  timeInterval.value = setInterval(updateTime, 1000);

  // 初始化加载数据
  refreshAllData();

  // 启动定时刷新任务
  refreshInterval = setInterval(() => {
    // 实际项目中使用真实数据
    fetchEnergyData();
    fetchAlarmList(); // 添加获取告警数据的调用
    // 为了演示动画效果，这里使用随机数据
  }, AUTO_REFRESH_INTERVAL);
});

onUnmounted(() => {
  clearInterval(timeInterval.value);
  clearInterval(refreshInterval);
  // 清除所有的事件监听器
  window.removeEventListener("resize", () => {
    console.log("Removed resize listener");
  });
});

const goQingNeng = () => {
  // 根据日期类型设置不同的参数
  let type = 1; // 默认为日
  if (dateType.value === "month") {
    type = 3; // 月
  } else if (dateType.value === "year") {
    type = 4; // 年
  }

  // 使用dayjs获取当前日期
  const now = dayjs();
  let startDate, endDate;

  // 根据日期类型设置不同的时间范围
  if (dateType.value === "day") {
    // 日：查询当天
    startDate = now.startOf("day").format("YYYY-MM-DD HH:mm:ss");
    endDate = now.endOf("day").format("YYYY-MM-DD HH:mm:ss");
  } else if (dateType.value === "month") {
    // 月：查询本月
    startDate = now.startOf("month").format("YYYY-MM-DD HH:mm:ss");
    endDate = now.endOf("month").format("YYYY-MM-DD HH:mm:ss");
  } else if (dateType.value === "year") {
    // 年：查询当年
    startDate = now.startOf("year").format("YYYY-MM-DD HH:mm:ss");
    endDate = now.endOf("year").format("YYYY-MM-DD HH:mm:ss");
  }
  const url = `http://47.100.49.118:8090/vehicle-statistic?customerName=嘉兴大森物流有限公司&startDate=${startDate}&endDate=${endDate}&types=${type}`;
  console.log("🚀 ~ goQingNeng ~ url:", url);
  window.open(url, "_blank");
};

// 暂停和恢复告警滚动
let scrollInterval = null;
const alertContainerRef = ref(null);
const alertListRef = ref(null);

const pauseScroll = () => {
  if (scrollInterval) {
    clearInterval(scrollInterval);
    scrollInterval = null;
  }
};

const resumeScroll = () => {
  if (!scrollInterval && alertContainerRef.value) {
    startScrolling();
  }
};

const startScrolling = () => {
  if (alertContainerRef.value && alertListRef.value) {
    scrollInterval = setInterval(() => {
      const container = alertListRef.value;
      const content = alertContainerRef.value;
      if (container && content) {
        if (content.offsetHeight > container.offsetHeight) {
          if (
            container.scrollTop + container.offsetHeight >=
            content.offsetHeight
          ) {
            // 滚动到底部，重新开始
            container.scrollTop = 0;
          } else {
            // 继续滚动
            container.scrollTop += 1;
          }
        }
      }
    }, 50);
  }
};

const goToMonthEnergyReport = () => {
  // 修改获取上月日期的代码，确保获取的是上月1号的日期
  const prevMonth = dayjs()
    .subtract(1, "month")
    .startOf("month")
    .format("YYYY-MM-DD");
  // 跳转到月度能源报告二级页面
  router.push({
    path: "/MonthEnergyReport",
    query: { date: prevMonth }
  });
};

const goToYearEnergyReport = () => {
  // 获取当年1月1日的日期
  const currentYear = dayjs().startOf("year").format("YYYY-MM-DD");
  // 跳转到年度能源报告二级页面
  router.push({
    path: "/YearEnergyReport",
    query: { date: currentYear }
  });
};
</script>

<template>
  <div class="main-content">
    <!-- 顶部导航栏 -->
    <nav class="navbar">
      <div class="nav-brand">
        <div class="logo">
          <el-icon><component :is="useRenderIcon(boltIcon)" /></el-icon>
        </div>
        <h1 class="nav-title">综合能源管控平台</h1>
      </div>
      <div class="nav-actions">
        <div class="report-btn" @click="goToMonthEnergyReport">
          <el-icon><component :is="useRenderIcon(calendarIcon)" /></el-icon>
          <span>月度分析报告</span>
        </div>
        <div class="report-btn" @click="goToYearEnergyReport">
          <el-icon><component :is="useRenderIcon(calendarIcon)" /></el-icon>
          <span>年度分析报告</span>
        </div>
        <div class="date-type-selector">
          <el-icon><component :is="useRenderIcon(calendarIcon)" /></el-icon>
          <div class="date-type-options">
            <span
              v-for="option in dateOptions"
              :key="option.value"
              :class="['date-option', { active: dateType === option.value }]"
              @click="handleDateTypeChange(option.value)"
            >
              {{ option.label }}
            </span>
          </div>
        </div>
        <div class="time-display">
          <el-icon><component :is="useRenderIcon(clockIcon)" /></el-icon>
          <span id="current-time">{{ currentTime }}</span>
        </div>

        <!-- 双模式切换按钮 -->
        <div
          class="mode-btn"
          :class="{
            realtime: demoModeStore.currentMode === 'realtime',
            demo: demoModeStore.currentMode === 'demo'
          }"
          @click="demoModeStore.switchToNextMode()"
        >
          <el-icon
            ><component :is="useRenderIcon('ep:data-analysis')"
          /></el-icon>
          <span>{{ demoModeStore.currentModeText }}</span>
          <span v-if="demoModeStore.currentMode === 'demo'" class="mode-date">{{
            dateType === "day"
              ? demoModeStore.demoDateText
              : demoModeStore.monthText
          }}</span>
        </div>

        <!-- 演示日期选择器 -->
        <div
          v-if="demoModeStore.currentMode === 'demo' && dateType === 'day'"
          class="demo-date-picker"
        >
          <el-date-picker
            v-model="selectedDemoDate"
            type="date"
            placeholder="选择演示日期"
            size="small"
            :disabled-date="disabledDate"
            format="MM月DD日"
            value-format="YYYY-MM-DD"
            @change="handleDemoDateChange"
          />
        </div>

        <div class="admin-btn" @click="router.push('/energyCA')">
          <el-icon><component :is="useRenderIcon(settingsIcon)" /></el-icon>
          <span>进入后台</span>
        </div>
      </div>
    </nav>
    <div class="content">
      <div class="dashboard-container">
        <!-- 深色渐变背景层 -->
        <div class="map-background" />
        <!-- 侧边栏和底部模块容器 -->
        <div class="dashboard-layout">
          <!-- 左侧边栏 -->
          <div class="sidebar-content left">
            <div class="sidebar left slide-in-left">
              <div
                class="module"
                :class="{ collapsed: moduleCollapsed.energy }"
              >
                <div class="module-header">
                  <div class="module-title">
                    <el-icon
                      ><component :is="useRenderIcon(chartLineIcon)"
                    /></el-icon>
                    <span
                      >能源生产与消耗 ({{
                        dateType === "day"
                          ? "日"
                          : dateType === "month"
                            ? "月"
                            : "年"
                      }})</span
                    >
                  </div>
                </div>
                <div class="module-content no-scroll">
                  <div
                    ref="energyCompareChartRef"
                    class="chart-container min-h-[165px]!"
                  />
                </div>
              </div>
              <div
                class="module"
                :class="{ collapsed: moduleCollapsed.warehouse }"
              >
                <div class="module-header">
                  <div class="module-title">
                    <el-icon
                      ><component :is="useRenderIcon(warehouseIcon)"
                    /></el-icon>
                    <span
                      >仓库用电监测 ({{
                        dateType === "day"
                          ? "日"
                          : dateType === "month"
                            ? "月"
                            : "年"
                      }})</span
                    >
                  </div>
                </div>
                <div class="module-content">
                  <div ref="warehouseChartRef" class="chart-container" />
                </div>
              </div>
            </div>
          </div>
          <!-- 中间内容区域 - 占位 -->
          <div class="center-space">
            <div class="indicators-wrapper">
              <div class="indicators fade-in">
                <div class="indicator-card energy-gen">
                  <div class="indicator-header">
                    <div class="indicator-icon">
                      <el-icon
                        ><component :is="useRenderIcon(solarPanelIcon)"
                      /></el-icon>
                    </div>
                    <div class="indicator-title">
                      {{ titleMapping[dateType].product }}
                    </div>
                  </div>
                  <div
                    class="indicator-value energy-gen-value"
                    :class="{ 'value-updated': valueUpdateFlags.productToday }"
                  >
                    <number-animation
                      :value="energyData.productToday"
                      :decimals="0"
                    />
                    <small>kWh</small>
                  </div>
                  <div class="indicator-change">
                    <span style="color: rgba(255, 255, 255, 0.6)">{{
                      titleMapping[dateType].compareText
                    }}</span>
                    <el-icon
                      :style="{
                        color:
                          energyData.productRate >= 0 ? '#00ff87' : '#ff6b6b'
                      }"
                    >
                      <component
                        :is="
                          useRenderIcon(
                            energyData.productRate >= 0
                              ? arrowUpIcon
                              : arrowDownIcon
                          )
                        "
                      />
                    </el-icon>
                    <span
                      :style="{
                        color:
                          energyData.productRate >= 0 ? '#00ff87' : '#ff6b6b'
                      }"
                    >
                      {{ energyData.productRate }}%
                    </span>
                  </div>
                  <div class="data-flow" />
                </div>
                <div class="indicator-card energy-con">
                  <div class="indicator-header">
                    <div class="indicator-icon">
                      <el-icon
                        ><component :is="useRenderIcon(plugIcon)"
                      /></el-icon>
                    </div>
                    <div class="indicator-title">
                      {{ titleMapping[dateType].cost }}
                    </div>
                  </div>
                  <div
                    class="indicator-value energy-con-value"
                    :class="{ 'value-updated': valueUpdateFlags.costToday }"
                  >
                    <span style="white-space: nowrap; display: inline-block">
                      <number-animation
                        :value="energyData.costToday"
                        :decimals="0"
                      />
                      <small>kWh</small>
                    </span>
                  </div>
                  <div class="indicator-change">
                    <span style="color: rgba(255, 255, 255, 0.6)">{{
                      titleMapping[dateType].compareText
                    }}</span>
                    <el-icon
                      :style="{
                        color: energyData.costRate >= 0 ? '#ff6b6b' : '#00ff87'
                      }"
                    >
                      <component
                        :is="
                          useRenderIcon(
                            energyData.costRate >= 0
                              ? arrowUpIcon
                              : arrowDownIcon
                          )
                        "
                      />
                    </el-icon>
                    <span
                      :style="{
                        color: energyData.costRate >= 0 ? '#ff6b6b' : '#00ff87'
                      }"
                    >
                      {{ energyData.costRate }}%
                    </span>
                  </div>
                  <div class="data-flow" />
                </div>
                <div class="indicator-card energy-diff">
                  <div class="indicator-header">
                    <div class="indicator-icon">
                      <el-icon
                        ><component :is="useRenderIcon(scaleBalanceIcon)"
                      /></el-icon>
                    </div>
                    <div class="indicator-title">
                      {{ titleMapping[dateType].remain }}
                    </div>
                  </div>
                  <div
                    class="indicator-value energy-diff-value"
                    :class="{ 'value-updated': valueUpdateFlags.remainToday }"
                  >
                    <number-animation
                      :value="energyData.remainToday"
                      :decimals="0"
                    />
                    <small>kWh</small>
                  </div>
                  <div class="indicator-change">
                    <div
                      style="
                        display: flex;
                        justify-content: space-between;
                        align-items: center;
                        width: 100%;
                      "
                    >
                      <el-tooltip
                        content="自给率 = 发电量 / 用电量 x 100%"
                        placement="top"
                        effect="light"
                      >
                        <div style="display: flex; align-items: center">
                          <span style="color: rgba(255, 255, 255, 0.6)"
                            >自给率</span
                          >
                          <span style="color: #ffa726; margin-left: 4px"
                            >{{ energyData.remainRate }}%</span
                          >
                        </div>
                      </el-tooltip>
                      <div style="display: flex; align-items: center">
                        <span style="color: rgba(255, 255, 255, 0.6)"
                          >发电效率</span
                        >
                        <span style="color: #4fc3f7; margin-left: 4px"
                          >{{ energyData.productEffect }}%</span
                        >
                      </div>
                    </div>
                  </div>
                  <div class="data-flow" />
                </div>
                <div class="indicator-card carbon-total">
                  <div class="indicator-header">
                    <div class="indicator-icon">
                      <el-icon
                        ><component :is="useRenderIcon(cloudSmogIcon)"
                      /></el-icon>
                    </div>
                    <div class="indicator-title">
                      {{ titleMapping[dateType].co2Cost }}
                    </div>
                  </div>
                  <div
                    class="indicator-value carbon-total-value"
                    :class="{ 'value-updated': valueUpdateFlags.co2CostToday }"
                  >
                    <!-- <number-animation
                      :value="energyData.co2CostToday"
                      :decimals="0"
                    /> -->
                    <number-animation :value="0" :decimals="0" />
                    <small style="display: inline-block; margin-left: 4px"
                      >kgCO₂e</small
                    >
                  </div>
                  <div class="indicator-change">
                    <span style="color: rgba(255, 255, 255, 0.6)">减碳量</span>
                    <span style="color: #00ff87"
                      >{{ energyData.co2RemainToday }} kgCO₂e</span
                    >
                  </div>
                  <div class="data-flow" />
                </div>
                <div class="indicator-card carbon-save">
                  <div class="indicator-header">
                    <div class="indicator-icon">
                      <el-icon
                        ><component :is="useRenderIcon(leafIcon)"
                      /></el-icon>
                    </div>
                    <div class="indicator-title">
                      {{ titleMapping[dateType].co2Remain }}
                    </div>
                  </div>
                  <div
                    class="indicator-value carbon-save-value"
                    :class="{
                      'value-updated': valueUpdateFlags.co2RemainToday
                    }"
                    style="white-space: nowrap"
                  >
                    <number-animation
                      :value="energyData.co2RemainToday"
                      :decimals="0"
                    /><small>kgCO₂e</small>
                  </div>
                  <div class="indicator-change">
                    <span style="color: rgba(255, 255, 255, 0.6)">环比</span>
                    <el-icon
                      :style="{
                        color:
                          energyData.co2RemainRate >= 0 ? '#00ff87' : '#ff6b6b'
                      }"
                    >
                      <component
                        :is="
                          useRenderIcon(
                            energyData.co2RemainRate >= 0
                              ? arrowUpIcon
                              : arrowDownIcon
                          )
                        "
                      />
                    </el-icon>
                    <span
                      :style="{
                        color:
                          energyData.co2RemainRate >= 0 ? '#00ff87' : '#ff6b6b'
                      }"
                    >
                      {{ energyData.co2RemainRate }}%
                    </span>
                  </div>
                  <div class="data-flow" />
                </div>
                <div class="indicator-card water-usage">
                  <div class="indicator-header">
                    <div class="indicator-icon">
                      <el-icon
                        ><component :is="useRenderIcon(waterIcon)"
                      /></el-icon>
                    </div>
                    <div class="indicator-title">
                      {{ titleMapping[dateType].water }}
                    </div>
                  </div>
                  <div
                    class="indicator-value water-usage-value"
                    :class="{ 'value-updated': valueUpdateFlags.waterToday }"
                  >
                    <number-animation
                      :value="energyData.waterToday"
                      :decimals="0"
                    />
                    <small>吨</small>
                  </div>
                  <div class="indicator-change">
                    <span style="color: rgba(255, 255, 255, 0.6)">节水量</span>
                    <el-icon
                      :style="{
                        color: energyData.waterRate >= 0 ? '#ff6b6b' : '#00ff87'
                      }"
                    >
                      <component :is="useRenderIcon(arrowUpIcon)" />
                    </el-icon>
                    <span
                      :style="{
                        color: energyData.waterRate >= 0 ? '#ff6b6b' : '#00ff87'
                      }"
                    >
                      {{ Math.abs(energyData.remainWaterToday).toFixed(1) }}
                    </span>
                  </div>
                  <div class="data-flow" />
                </div>
              </div>
            </div>
          </div>
          <!-- 右侧边栏 -->
          <div class="sidebar-content right">
            <div class="sidebar right slide-in-right">
              <div
                class="module"
                :class="{ collapsed: moduleCollapsed.alerts }"
              >
                <div class="module-header">
                  <div class="module-title">
                    <el-icon
                      ><component :is="useRenderIcon(warningIcon)"
                    /></el-icon>
                    <span>告警中心</span>
                  </div>
                  <div class="module-actions">
                    <el-tooltip content="刷新告警数据" placement="top">
                      <el-button
                        type="text"
                        :loading="alertLoading"
                        style="
                          color: rgba(255, 255, 255, 0.6);
                          margin-right: 8px;
                        "
                        @click="fetchAlarmList"
                      >
                        <el-icon><refresh /></el-icon>
                      </el-button>
                    </el-tooltip>
                  </div>
                </div>
                <div class="module-content">
                  <div
                    ref="alertListRef"
                    class="alert-list"
                    style="
                      scroll-behavior: auto;
                      background: transparent;
                      transition: background-color 0.3s ease;
                    "
                    @mouseenter="pauseScroll"
                    @mouseleave="resumeScroll"
                  >
                    <div
                      v-if="alertLoading && alertData.length === 0"
                      class="alert-loading"
                    >
                      <el-skeleton :rows="3" animated />
                    </div>
                    <div v-else-if="alertData.length === 0" class="alert-empty">
                      <el-empty description="暂无告警数据" :image-size="60" />
                    </div>
                    <div v-else ref="alertContainerRef" class="alert-container">
                      <div
                        v-for="(alert, index) in alertData"
                        :key="index"
                        :class="['alert-item', alert.type]"
                        @click="router.push('/record/alarmLog/index')"
                      >
                        <div class="alert-header">
                          <div class="alert-title">
                            <span :class="['alert-badge', alert.type]">{{
                              alert.type === "critical"
                                ? "严重"
                                : alert.type === "warning"
                                  ? "警告"
                                  : "普通"
                            }}</span>
                            {{ alert.title }}
                          </div>
                          <div class="alert-time">{{ alert.time }}</div>
                        </div>
                        <div class="alert-desc">{{ alert.description }}</div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div class="module" :class="{ collapsed: moduleCollapsed.water }">
                <div class="module-header">
                  <div class="module-title">
                    <el-icon
                      ><component :is="useRenderIcon(waterIcon)"
                    /></el-icon>
                    <span
                      >园区用水量趋势 ({{
                        dateType === "day"
                          ? "日"
                          : dateType === "month"
                            ? "月"
                            : "年"
                      }})</span
                    >
                  </div>
                  <!-- 添加查看详情按钮 -->
                  <div class="module-actions">
                    <el-button
                      size="small"
                      type="primary"
                      style="padding: 4px 8px; font-size: 12px"
                      @click="
                        router.push({
                          path: '/DeviceEnergyMonitorCockpit',
                          query: { devtype: 3 }
                        })
                      "
                    >
                      查看详情
                    </el-button>
                  </div>
                </div>
                <div class="module-content no-scroll">
                  <div
                    ref="waterChartRef"
                    class="chart-container min-h-[165px]!"
                  />
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 底部模块 -->
        <div class="bottom-modules-container">
          <div class="bottom-modules fade-in">
            <!-- 第一行布局：25% - 50% - 25% -->
            <div class="bottom-row">
              <div class="bottom-module small">
                <div class="module-header">
                  <div class="module-title">
                    <el-icon
                      ><component :is="useRenderIcon(carBatteryIcon)"
                    /></el-icon>
                    <span
                      >叉车/堆高车/汽车充电桩监测 ({{
                        dateType === "day"
                          ? "日"
                          : dateType === "month"
                            ? "月"
                            : "年"
                      }})</span
                    >
                  </div>
                  <div class="module-actions">
                    <div class="data-type-selector">
                      <span
                        :class="[
                          'data-type-option',
                          {
                            active:
                              chargerMonitorData.currentType === 'ChargingCount'
                          }
                        ]"
                        @click="switchChargerMonitorType('ChargingCount')"
                      >
                        充电次数
                      </span>
                      <span
                        :class="[
                          'data-type-option',
                          {
                            active:
                              chargerMonitorData.currentType ===
                              'ChargingEnergy'
                          }
                        ]"
                        @click="switchChargerMonitorType('ChargingEnergy')"
                      >
                        用电量
                      </span>
                    </div>
                    <!-- 添加查看详情按钮 -->
                    <el-button
                      size="small"
                      type="primary"
                      style="
                        padding: 4px 8px;
                        margin-left: 8px;
                        font-size: 12px;
                      "
                      @click="router.push('/ChargingPileMonitorCockpit')"
                    >
                      查看详情
                    </el-button>
                  </div>
                </div>
                <div class="module-content">
                  <div ref="forkliftChargerChartRef" class="chart-container" />
                </div>
              </div>
              <div class="bottom-module small">
                <div class="module-header">
                  <div class="module-title">
                    <el-icon
                      ><component :is="useRenderIcon(buildingIcon)"
                    /></el-icon>
                    <span
                      >楼层用电对比 ({{
                        dateType === "day"
                          ? "日"
                          : dateType === "month"
                            ? "月"
                            : "年"
                      }})</span
                    >
                  </div>
                  <!-- 添加查看详情按钮 -->
                  <div class="module-actions">
                    <el-button
                      size="small"
                      type="primary"
                      style="padding: 4px 8px; font-size: 12px"
                      @click="router.push('/FloorEnergy')"
                    >
                      查看详情
                    </el-button>
                  </div>
                </div>
                <div class="module-content">
                  <div
                    ref="floorEnergyChartRef"
                    class="chart-container min-h-[200px]!"
                  />
                </div>
              </div>
            </div>
            <!-- 第二行布局：50% - 50% -->
            <div class="bottom-row">
              <div
                class="bottom-module medium"
                :class="{ collapsed: moduleCollapsed.solar }"
              >
                <div class="module-header">
                  <div class="module-title">
                    <el-icon
                      ><component :is="useRenderIcon(solarPanelIcon)"
                    /></el-icon>
                    <span
                      >流机监测 ({{
                        dateType === "day"
                          ? "日"
                          : dateType === "month"
                            ? "月"
                            : "年"
                      }})</span
                    >
                  </div>
                  <!-- 添加查看详情按钮 -->
                  <div class="module-actions">
                    <el-button
                      size="small"
                      type="primary"
                      style="padding: 4px 8px; font-size: 12px"
                      @click="
                        router.push({
                          path: '/DeviceEnergyMonitorCockpit',
                          query: { devtype: 4 }
                        })
                      "
                    >
                      查看详情
                    </el-button>
                  </div>
                </div>
                <div class="module-content">
                  <div
                    ref="solarChartRef"
                    class="chart-container min-h-[200px]!"
                  />
                </div>
              </div>
              <!-- 楼层空调能耗 -->
              <div
                class="bottom-module medium"
                :class="{ collapsed: moduleCollapsed.air }"
              >
                <div class="module-header">
                  <div class="module-title">
                    <el-icon
                      ><component
                        :is="useRenderIcon('material-symbols-light:ac-unit')"
                    /></el-icon>
                    <span
                      >楼层空调能耗 ({{
                        dateType === "day"
                          ? "日"
                          : dateType === "month"
                            ? "月"
                            : "年"
                      }})</span
                    >
                  </div>
                  <!-- 添加查看详情按钮 -->
                  <div class="module-actions">
                    <el-button
                      size="small"
                      type="primary"
                      style="padding: 4px 8px; font-size: 12px"
                      @click="
                        router.push({
                          path: '/DeviceEnergyMonitorCockpit',
                          query: { devtype: 2 }
                        })
                      "
                    >
                      查看详情
                    </el-button>
                  </div>
                </div>
                <div class="module-content">
                  <div ref="airEnergyChartRef" class="chart-container" />
                </div>
              </div>
              <div
                class="bottom-module medium"
                :class="{ collapsed: moduleCollapsed.solar }"
              >
                <div class="module-header">
                  <div class="module-title">
                    <el-icon
                      ><component :is="useRenderIcon(solarPanelIcon)"
                    /></el-icon>
                    <span
                      >氢能监测 ({{
                        dateType === "day"
                          ? "日"
                          : dateType === "month"
                            ? "月"
                            : "年"
                      }})</span
                    >
                  </div>
                  <div class="module-actions">
                    <button
                      style="
                        background: #00a2ff;
                        border: none;
                        color: white;
                        padding: 4px 8px;
                        border-radius: 4px;
                        font-size: 12px;
                        cursor: pointer;
                      "
                      @click="goQingNeng"
                    >
                      查看详情
                    </button>
                  </div>
                </div>
                <div class="module-content">
                  <div
                    ref="hydrogenChartRef"
                    class="chart-container min-h-[200px]!"
                  />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
    <!-- 右侧面板 -->
    <div class="right-panel">
      <!-- 添加右侧面板内容 -->
    </div>
  </div>
</template>

<style lang="scss" scoped>
/* 基本样式 */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

/* 添加图标尺寸样式 */
.nav-actions .el-icon {
  font-size: 18px;
}

.indicator-icon .el-icon {
  font-size: 22px;
}

.indicator-change .el-icon {
  font-size: 14px;
}

.module-title .el-icon {
  font-size: 20px;
}

.module-header .el-icon[style*="cursor: pointer"] {
  font-size: 18px;
}

.device-icon .el-icon {
  font-size: 20px;
}

/* 主容器 */
.main-content {
  font-family:
    "SF Pro Display",
    -apple-system,
    BlinkMacSystemFont,
    "Segoe UI",
    sans-serif;
  color: #ffffff;
  width: 100%;
  overflow: hidden;
  overflow-y: auto;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background:
    radial-gradient(
      circle at 20% 50%,
      rgba(0, 162, 255, 0.15) 0%,
      transparent 50%
    ),
    radial-gradient(
      circle at 80% 20%,
      rgba(255, 0, 150, 0.1) 0%,
      transparent 50%
    ),
    radial-gradient(
      circle at 40% 80%,
      rgba(0, 255, 135, 0.1) 0%,
      transparent 50%
    ),
    linear-gradient(135deg, #0a0f1c 0%, #1a1f3a 50%, #0f1a2e 100%);
}

.content {
  flex: 1;
  position: relative;
  /* scroll-behavior: smooth; */
}

.dashboard-container {
  display: flex;
  flex-direction: column;
  padding: 24px;
  min-height: calc(100vh - 60px); /* 减去navbar高度 */
  scroll-behavior: smooth;
  position: relative;
  width: 100%;
  height: 100%;
  overflow: hidden;
}

.dashboard-layout {
  display: flex;
  flex: 1;
  height: auto !important;
  max-height: 54vh;
}

.sidebar-content {
  width: 20vw;
  flex-shrink: 0;
  height: auto;
  min-height: 40vh;
}

.sidebar-content.left {
  padding-right: 12px;
}

.sidebar-content.right {
  padding-left: 12px;
}

.center-space {
  flex: 1;
  padding: 0 12px;
}

/* 底部模块容器 */
.bottom-modules-container {
  margin-top: 24px;
  padding-bottom: 24px;
  flex-shrink: 0;
}

/* 指标卡片容器 */
.indicators-wrapper {
  display: flex;
  justify-content: center;
  margin-bottom: 20px;
  max-width: 100%;
  overflow: hidden;
}

@media (max-width: 1400px) {
  .indicators {
    gap: 10px;
  }

  .indicator-card {
    width: 150px;
    height: 125px;
    padding: 14px;
  }

  .indicator-value {
    font-size: 20px;
  }
}

@media (max-width: 1200px) {
  .indicators {
    gap: 8px;
  }

  .indicator-card {
    width: 140px;
    height: 120px;
    padding: 12px;
  }

  .indicator-icon {
    width: 26px;
    height: 26px;
  }

  .indicator-value {
    font-size: 18px;
  }
}

@media (max-width: 1400px) {
  .nav-btn {
    padding: 5px 10px;
    font-size: 12px;
  }

  .sub-page-nav {
    gap: 5px;
  }
}

@media (max-width: 1200px) {
  .nav-btn span {
    display: none;
  }

  .nav-btn {
    padding: 6px;
  }

  .nav-btn .el-icon {
    margin-right: 0;
    font-size: 18px;
  }
}

@media (max-width: 992px) {
  .indicators {
    flex-wrap: wrap;
    justify-content: center;
  }

  .indicator-card {
    width: 160px;
    height: 130px;
    padding: 14px;
  }

  .sub-page-nav {
    position: absolute;
    top: 70px;
    left: 50%;
    transform: translateX(-50%);
    background: rgba(0, 20, 40, 0.8);
    border-radius: 20px;
    padding: 8px;
    z-index: 1001;
    box-shadow: 0 4px 15px rgba(0, 0, 0, 0.2);
    border: 1px solid rgba(0, 162, 255, 0.3);
  }
}

/* 顶部导航栏 */
.navbar {
  height: 60px;
  background: linear-gradient(
    135deg,
    rgba(0, 20, 40, 0.95) 0%,
    rgba(0, 40, 80, 0.9) 100%
  );
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border-bottom: 1px solid rgba(0, 162, 255, 0.3);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 30px;
  position: relative;
  z-index: 1000;
  box-shadow: 0 4px 30px rgba(0, 162, 255, 0.1);
}

.navbar::after {
  content: "";
  position: absolute;
  bottom: 0;
  left: 0;
  width: 100%;
  height: 2px;
  background: linear-gradient(90deg, transparent, #00a2ff, transparent);
  animation: scanLine 3s linear infinite;
}

@keyframes scanLine {
  0% {
    transform: translateX(-100%);
  }
  100% {
    transform: translateX(100%);
  }
}

.nav-brand {
  display: flex;
  align-items: center;
  gap: 15px;
}

.nav-brand .logo {
  width: 40px;
  height: 40px;
  background: linear-gradient(45deg, #00a2ff, #0078d4);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%,
  100% {
    box-shadow: 0 0 20px rgba(0, 162, 255, 0.5);
  }
  50% {
    box-shadow: 0 0 30px rgba(0, 162, 255, 0.8);
  }
}

.nav-title {
  font-size: 22px;
  font-weight: 700;
  background: linear-gradient(45deg, #00a2ff, #ffffff, #00ff87);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  text-shadow: 0 0 30px rgba(0, 162, 255, 0.5);
}

.nav-actions {
  display: flex;
  align-items: center;
  gap: 20px;
}

.date-type-selector {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  background: rgba(0, 162, 255, 0.1);
  border: 1px solid rgba(0, 162, 255, 0.3);
  border-radius: 20px;
  font-size: 14px;
}

.date-type-options {
  display: flex;
  align-items: center;
  gap: 2px;
}

.date-option {
  padding: 4px 10px;
  border-radius: 14px;
  cursor: pointer;
  transition: all 0.3s ease;
  font-size: 13px;
}

.date-option:hover {
  background: rgba(0, 162, 255, 0.2);
}

.date-option.active {
  background: linear-gradient(135deg, #00a2ff, #0078d4);
  color: white;
  box-shadow: 0 2px 6px rgba(0, 162, 255, 0.3);
}

.time-display {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 16px;
  background: rgba(0, 162, 255, 0.1);
  border: 1px solid rgba(0, 162, 255, 0.3);
  border-radius: 20px;
  font-size: 14px;
}

.weather-info {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
}

.sub-page-nav {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-right: 5px;
}

.nav-btn {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 6px 12px;
  background: linear-gradient(
    135deg,
    rgba(0, 100, 192, 0.6),
    rgba(0, 60, 132, 0.6)
  );
  color: #fff;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s ease;
  border: 1px solid rgba(0, 153, 255, 0.2);
  border-radius: 18px;
  backdrop-filter: blur(10px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
}

.nav-btn:hover {
  background: linear-gradient(
    135deg,
    rgba(0, 120, 212, 0.8),
    rgba(0, 80, 152, 0.8)
  );
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.18);
  border: 1px solid rgba(0, 183, 255, 0.4);
}

.mode-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  border-radius: 20px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}

/* 实时模式样式 */
.mode-btn.realtime {
  background: linear-gradient(135deg, #6b46c1, #8b5cf6);
  box-shadow: 0 2px 10px rgba(107, 70, 193, 0.3);
}

.mode-btn.realtime:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 15px rgba(139, 92, 246, 0.5);
}

/* 演示模式样式 */
.mode-btn.demo {
  background: linear-gradient(135deg, #10b981, #059669);
  box-shadow: 0 2px 10px rgba(16, 185, 129, 0.3);
  animation: pulse-demo 2s ease-in-out infinite;
}

.mode-btn.demo:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 15px rgba(16, 185, 129, 0.5);
}

.demo-date {
  font-size: 12px;
  opacity: 0.9;
  margin-left: 4px;
  padding: 2px 6px;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 8px;
}

.demo-date-picker {
  display: flex;
  align-items: center;
  margin-left: 12px;
}

.demo-date-picker :deep(.el-input__wrapper) {
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 16px;
  box-shadow: none;
  transition: all 0.3s ease;
}

.demo-date-picker :deep(.el-input__wrapper:hover) {
  border-color: rgba(255, 255, 255, 0.5);
  background: rgba(255, 255, 255, 0.15);
}

.demo-date-picker :deep(.el-input__wrapper.is-focus) {
  border-color: #10b981;
  background: rgba(255, 255, 255, 0.2);
  box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2);
}

.demo-date-picker :deep(.el-input__inner) {
  color: #ffffff;
  font-size: 12px;
  text-align: center;
}

.demo-date-picker :deep(.el-input__inner::placeholder) {
  color: rgba(255, 255, 255, 0.6);
}

.demo-date-picker :deep(.el-input__suffix) {
  color: rgba(255, 255, 255, 0.8);
}

.demo-mode-type-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: linear-gradient(135deg, #f59e0b, #d97706);
  border-radius: 16px;
  font-size: 12px;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(245, 158, 11, 0.3);
  margin-left: 8px;
}

.demo-mode-type-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(245, 158, 11, 0.5);
  background: linear-gradient(135deg, #fbbf24, #f59e0b);
}

.demo-mode-type-btn .el-icon {
  font-size: 14px;
}

@keyframes pulse-demo {
  0%,
  100% {
    box-shadow: 0 2px 10px rgba(16, 185, 129, 0.3);
  }
  50% {
    box-shadow: 0 4px 20px rgba(16, 185, 129, 0.6);
  }
}

.admin-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background: linear-gradient(135deg, #0078d4, #005bb5);
  border-radius: 20px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 10px rgba(0, 120, 212, 0.3);
}

.admin-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 15px rgba(0, 162, 255, 0.5);
}

.report-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background: linear-gradient(135deg, #00a2ff, #0078d4);
  border-radius: 20px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 10px rgba(0, 162, 255, 0.3);
}

.report-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 15px rgba(0, 162, 255, 0.5);
}

/* 添加动画类 */
.fade-in {
  animation: fadeIn 0.8s ease-out forwards;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.slide-in-left {
  animation: slideInLeft 0.6s ease-out forwards;
}

@keyframes slideInLeft {
  from {
    opacity: 0;
    transform: translateX(-50px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.slide-in-right {
  animation: slideInRight 0.6s ease-out forwards;
}

@keyframes slideInRight {
  from {
    opacity: 0;
    transform: translateX(50px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

/* 主要指标卡片 */
.indicators {
  display: flex;
  gap: 12px;
  justify-content: center;
  z-index: 100;
  flex-wrap: nowrap;
}

.indicator-card {
  width: 160px;
  height: 130px;
  background: linear-gradient(
    135deg,
    rgba(0, 20, 40, 0.8) 0%,
    rgba(0, 40, 80, 0.6) 100%
  );
  backdrop-filter: blur(15px);
  -webkit-backdrop-filter: blur(15px);
  border: 1px solid rgba(0, 162, 255, 0.3);
  border-radius: 16px;
  padding: 12px;
  position: relative;
  overflow: hidden;
  transition: all 0.3s ease;
}

.indicator-card:hover {
  transform: translateY(-5px);
  border-color: rgba(0, 162, 255, 0.6);
  box-shadow: 0 10px 40px rgba(0, 162, 255, 0.2);
}

.indicator-card::before {
  content: "";
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(
    90deg,
    transparent,
    rgba(255, 255, 255, 0.1),
    transparent
  );
  transition: left 0.5s;
}

.indicator-card:hover::before {
  left: 100%;
}

.indicator-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.indicator-icon {
  width: 28px;
  height: 28px;
  border-radius: 7px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
}

.indicator-title {
  font-size: 11px;
  color: rgba(255, 255, 255, 0.7);
  font-weight: 500;
}

.indicator-value {
  font-size: 22px;
  font-weight: 700;
  margin-bottom: 5px;
}

.indicator-value small {
  font-size: 12px;
  margin-left: 3px;
}

.indicator-change {
  font-size: 11px;
  display: flex;
  align-items: center;
  gap: 3px;
}

/* 不同类型指标的颜色 */
.indicator-card.energy-gen .indicator-icon {
  background: linear-gradient(45deg, #00ff87, #60efff);
  color: #003d20;
}
.indicator-card.energy-gen .indicator-value {
  color: #00ff87;
}

.indicator-card.energy-con .indicator-icon {
  background: linear-gradient(45deg, #ff6b6b, #ffa726);
  color: #fff;
}
.indicator-card.energy-con .indicator-value {
  color: #ff6b6b;
}

.indicator-card.energy-diff .indicator-icon {
  background: linear-gradient(45deg, #4fc3f7, #29b6f6);
  color: #fff;
}
.indicator-card.energy-diff .indicator-value {
  color: #4fc3f7;
}

.indicator-card.carbon-total .indicator-icon {
  background: linear-gradient(45deg, #ab47bc, #e1bee7);
  color: #fff;
}
.indicator-card.carbon-total .indicator-value {
  color: #ab47bc;
}

.indicator-card.carbon-save .indicator-icon {
  background: linear-gradient(45deg, #66bb6a, #a5d6a7);
  color: #fff;
}
.indicator-card.carbon-save .indicator-value {
  color: #66bb6a;
}

.indicator-card.water-usage .indicator-icon {
  background: linear-gradient(45deg, #29b6f6, #81d4fa);
  color: #fff;
}
.indicator-card.water-usage .indicator-value {
  color: #29b6f6;
}

/* 数据流动画 */
.data-flow {
  position: absolute;
  width: 100%;
  height: 2px;
  background: linear-gradient(90deg, transparent, #00ff87, transparent);
  bottom: 0;
  left: -100%;
  animation: dataFlow 2s linear infinite;
}

@keyframes dataFlow {
  0% {
    left: -100%;
  }
  100% {
    left: 100%;
  }
}

/* 底部模块 */
.bottom-modules {
  display: flex;
  flex-direction: column;
  gap: 24px;
  z-index: 50;
  width: 100%;
}

.bottom-row {
  display: flex;
  gap: 24px;
  width: 100%;
}

.bottom-module {
  background: linear-gradient(
    135deg,
    rgba(0, 20, 40, 0.85) 0%,
    rgba(0, 40, 80, 0.7) 100%
  );
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border: 1px solid rgba(0, 162, 255, 0.3);
  border-radius: 16px;
  overflow: hidden;
  transition: all 0.3s ease;
  position: relative;
  display: flex;
  flex-direction: column;
  min-height: 300px;
}

.bottom-module:hover {
  border-color: rgba(0, 162, 255, 0.6);
  box-shadow: 0 10px 40px rgba(0, 162, 255, 0.15);
}

.bottom-module.small {
  flex: 1;
  width: 25%;
  min-width: 280px;
}

.bottom-module.medium {
  flex: 1;
  width: 50%;
  min-width: 400px;
}

.bottom-module.large {
  flex: 2;
  width: 50%;
  min-width: 450px;
}

/* 数据表格 */
.data-table {
  width: 100%;
  border-collapse: collapse;
}

.data-table th,
.data-table td {
  padding: 10px 14px;
  text-align: left;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  font-size: 12px;
}

.data-table th {
  background: rgba(0, 0, 0, 0.3);
  color: rgba(255, 255, 255, 0.8);
  font-weight: 600;
}

.data-table tr:hover {
  background: rgba(0, 162, 255, 0.1);
}

.status-badge {
  padding: 3px 10px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: bold;
}

.status-badge.charging {
  background: #00ff87;
  color: #003d20;
}

.status-badge.idle {
  background: rgba(255, 255, 255, 0.2);
  color: rgba(255, 255, 255, 0.7);
}

.status-badge.in-use {
  background: #00a2ff;
  color: white;
}

.status-badge.closed {
  background: rgba(255, 255, 255, 0.1);
  color: rgba(255, 255, 255, 0.5);
}

/* 响应式设计更新 */
@media (max-width: 1200px) {
  .bottom-row {
    flex-wrap: wrap;
  }

  .bottom-module.small,
  .bottom-module.medium,
  .bottom-module.large {
    width: 100%;
    flex: 1 1 100%;
  }

  .sidebar-content {
    width: 300px;
  }

  .indicators {
    flex-wrap: wrap;
    justify-content: center;
  }
}

@media (max-width: 992px) {
  .dashboard-layout {
    flex-direction: column;
  }

  .sidebar-content {
    width: 100%;
    height: auto;
    margin-bottom: 24px;
  }

  .sidebar-content.left {
    padding-right: 0;
  }

  .sidebar-content.right {
    padding-left: 0;
  }

  .center-space {
    padding: 0;
  }
}

@media (max-width: 768px) {
  .indicators {
    flex-direction: column;
    align-items: center;
  }

  .indicator-card {
    width: 100%;
    max-width: 280px;
  }

  .bottom-module {
    min-height: 250px;
  }

  .dashboard-container {
    padding: 16px;
  }
}

/* 补充模块收缩/展开样式 */
.module.collapsed {
  max-height: 60px;
}

.module.collapsed .module-content {
  display: none;
}

.module.collapsed .module-header {
  border-bottom: none;
}

/* 侧边栏模块 */
.sidebar {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
  height: 100%;
  z-index: 50;
}

.module {
  background: linear-gradient(
    135deg,
    rgba(0, 20, 40, 0.85) 0%,
    rgba(0, 40, 80, 0.7) 100%
  );
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border: 1px solid rgba(0, 162, 255, 0.3);
  border-radius: 16px;
  overflow: hidden;
  flex: 1;
  transition: all 0.3s ease;
  position: relative;
  display: flex;
  flex-direction: column;
}

.module:hover {
  border-color: rgba(0, 162, 255, 0.6);
  box-shadow: 0 10px 40px rgba(0, 162, 255, 0.15);
}

.module-header {
  padding: 10px 12px;
  background: linear-gradient(
    135deg,
    rgba(0, 162, 255, 0.2) 0%,
    rgba(0, 120, 212, 0.1) 100%
  );
  border-bottom: 1px solid rgba(0, 162, 255, 0.2);
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-shrink: 0;
}

.module-title {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 15px;
  font-weight: 600;
}

.module-title i {
  font-size: 16px;
  color: #00a2ff;
}

.module-content {
  flex: 1;
  overflow-y: auto;
  display: flex;
}

/* 去掉特定图表的滚动条 */
.module-content.no-scroll {
  overflow: hidden;
}

/* 图表容器 */
.chart-container {
  width: 100%;
  flex: 1;
  background: rgba(0, 0, 0, 0.2);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  overflow: hidden;
  min-height: 150px !important ;
  cursor: default; /* 改为默认光标，不再显示为可点击 */
  transition: all 0.3s ease;
}

.chart-container:hover {
  box-shadow: 0 0 15px rgba(0, 162, 255, 0.5);
}

/* 表格容器 */
.data-table-container {
  /* width: 100%; */
  max-height: 240px;
  overflow-y: auto;
  background: rgba(0, 0, 0, 0.1);
  border-radius: 10px;
  padding: 5px;
}

/* 效率标签样式 */
.efficiency-badge {
  padding: 3px 10px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: bold;
}

.efficiency-badge.high {
  background: #00ff87;
  color: #003d20;
}

.efficiency-badge.medium {
  background: #ffa726;
  color: #4d2800;
}

.efficiency-badge.low {
  background: #ff4757;
  color: white;
}

/* 模拟波形图 */
.wave-chart {
  width: 100%;
  height: 100%;
  min-height: 200px;
  position: relative;
  overflow: hidden;
}

.wave-line {
  position: absolute;
  width: 200%;
  height: 2px;
  background: linear-gradient(
    90deg,
    transparent,
    #00ff87,
    #00a2ff,
    transparent
  );
  top: 50%;
  left: -100%;
  animation: waveMove 3s linear infinite;
}

@keyframes waveMove {
  0% {
    left: -100%;
  }
  100% {
    left: 100%;
  }
}

/* 环形进度条 */
.circle-progress {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  background: conic-gradient(
    #00ff87 0deg 252deg,
    rgba(255, 255, 255, 0.1) 252deg 360deg
  );
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
}

.circle-progress::before {
  content: "";
  width: 60px;
  height: 60px;
  border-radius: 50%;
  background: #0a0f1c;
  position: absolute;
}

.circle-progress span {
  font-size: 14px;
  font-weight: bold;
  color: #00ff87;
  z-index: 1;
}

/* 告警列表 */
.alert-list {
  display: flex;
  flex-direction: column;
  width: 100%;
  max-height: 300px;
  overflow-y: auto;
  overflow-x: hidden;
  padding-right: 5px;
  position: relative;
  will-change: scroll-position;
  -webkit-overflow-scrolling: touch;
  background-color: transparent !important;
  color: rgba(255, 255, 255, 0.8);
}

/* 添加告警容器样式 */
.alert-container {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-bottom: 12px;
  will-change: transform;
}

.alert-list::-webkit-scrollbar {
  width: 4px;
  background-color: transparent;
}

.alert-list::-webkit-scrollbar-track {
  background: transparent;
}

.alert-list::-webkit-scrollbar-thumb {
  background: rgba(0, 162, 255, 0.3);
  border-radius: 10px;
}

.alert-list:hover::-webkit-scrollbar-thumb {
  background: rgba(0, 162, 255, 0.8);
}

.alert-item,
.alert-item-clone {
  background: rgba(0, 0, 0, 0.3);
  border-left: 4px solid;
  border-radius: 8px;
  padding: 12px;
  transition: all 0.3s ease;
  animation: alertFadeIn 0.5s ease-out forwards;
  transform-origin: top center;
  will-change: transform, opacity;
}

/* 克隆项特殊样式 */
.alert-item-clone {
  animation: none;
}

@keyframes alertFadeIn {
  from {
    opacity: 0;
    transform: translateY(-10px) scale(0.95);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

.alert-item:hover,
.alert-item-clone:hover {
  background: rgba(0, 0, 0, 0.5);
  transform: translateX(5px) scale(1.02);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
}

.alert-item.critical,
.alert-item-clone.critical {
  border-left-color: #ff4757;
  background: linear-gradient(
    135deg,
    rgba(255, 71, 87, 0.1) 0%,
    rgba(0, 0, 0, 0.3) 100%
  );
}

.alert-item.warning,
.alert-item-clone.warning {
  border-left-color: #ffa726;
  background: linear-gradient(
    135deg,
    rgba(255, 167, 38, 0.1) 0%,
    rgba(0, 0, 0, 0.3) 100%
  );
}

.alert-item.info,
.alert-item-clone.info {
  border-left-color: #00a2ff;
  background: linear-gradient(
    135deg,
    rgba(0, 162, 255, 0.1) 0%,
    rgba(0, 0, 0, 0.3) 100%
  );
}

.alert-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.alert-title {
  font-size: 13px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 8px;
}

.alert-badge {
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 10px;
  font-weight: bold;
}

.alert-badge.critical {
  background: #ff4757;
  color: white;
  box-shadow: 0 0 8px rgba(255, 71, 87, 0.5);
}

.alert-badge.warning {
  background: #ffa726;
  color: white;
  box-shadow: 0 0 8px rgba(255, 167, 38, 0.5);
}

.alert-badge.info {
  background: #00a2ff;
  color: white;
  box-shadow: 0 0 8px rgba(0, 162, 255, 0.5);
}

.alert-time {
  font-size: 11px;
  color: rgba(255, 255, 255, 0.6);
}

.alert-desc {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.7);
  line-height: 1.4;
}

/* 进度条 */
.progress-bar {
  width: 100%;
  height: 6px;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 3px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  border-radius: 3px;
  transition: width 0.3s ease;
}

.progress-fill.high {
  background: linear-gradient(90deg, #00ff87, #60efff);
}

.progress-fill.medium {
  background: linear-gradient(90deg, #ffa726, #ffcc02);
}

.progress-fill.low {
  background: linear-gradient(90deg, #ff4757, #ff6b6b);
}

/* 自定义滚动条样式 - 全局 */
::-webkit-scrollbar {
  width: 5px;
  height: 5px;
  transition: opacity 0.3s ease;
  opacity: 0;
  background-color: transparent !important;
}

::-webkit-scrollbar-track {
  background: rgba(0, 0, 0, 0.1);
  border-radius: 10px;
}

::-webkit-scrollbar-thumb {
  background: linear-gradient(
    180deg,
    rgba(0, 162, 255, 0.5),
    rgba(0, 120, 212, 0.5)
  );
  border-radius: 10px;
  transition: background 0.3s ease;
}

::-webkit-scrollbar-thumb:hover {
  background: linear-gradient(
    180deg,
    rgba(0, 162, 255, 1),
    rgba(0, 120, 212, 1)
  );
}

/* 主容器滚动条 */
.main-content::-webkit-scrollbar,
.content::-webkit-scrollbar,
.dashboard-container::-webkit-scrollbar {
  width: 8px;
  opacity: 0;
}

.main-content::-webkit-scrollbar-track,
.content::-webkit-scrollbar-track,
.dashboard-container::-webkit-scrollbar-track {
  background: rgba(0, 0, 0, 0.05);
  border-radius: 0;
}

.main-content::-webkit-scrollbar-thumb,
.content::-webkit-scrollbar-thumb,
.dashboard-container::-webkit-scrollbar-thumb {
  background: linear-gradient(
    180deg,
    rgba(0, 162, 255, 0.3),
    rgba(0, 255, 135, 0.3)
  );
  border-radius: 8px;
  border: 2px solid rgba(0, 20, 40, 0.8);
}

.main-content:hover::-webkit-scrollbar-thumb,
.content:hover::-webkit-scrollbar-thumb,
.dashboard-container:hover::-webkit-scrollbar-thumb {
  background: linear-gradient(
    180deg,
    rgba(0, 162, 255, 0.8),
    rgba(0, 255, 135, 0.8)
  );
}

/* 鼠标悬停区域时显示滚动条 */
.main-content:hover::-webkit-scrollbar,
.content:hover::-webkit-scrollbar,
.dashboard-container:hover::-webkit-scrollbar,
.module-content:hover::-webkit-scrollbar,
.data-table-container:hover::-webkit-scrollbar,
.sidebar:hover::-webkit-scrollbar {
  opacity: 1;
}

/* 模块内部滚动条 */
.module-content::-webkit-scrollbar {
  width: 4px;
  height: 4px;
  opacity: 0;
}

.module-content::-webkit-scrollbar-track {
  background: rgba(0, 0, 0, 0.05);
  margin: 4px 0;
}

.module-content::-webkit-scrollbar-thumb {
  background: linear-gradient(
    180deg,
    rgba(0, 162, 255, 0.3),
    rgba(0, 162, 255, 0.2)
  );
  border-radius: 10px;
}

.module-content::-webkit-scrollbar-thumb:hover {
  background: linear-gradient(
    180deg,
    rgba(0, 162, 255, 0.8),
    rgba(0, 255, 135, 0.4)
  );
}

/* 添加非Webkit浏览器的支持 */
* {
  scrollbar-width: thin; /* Firefox */
  scrollbar-color: rgba(0, 162, 255, 0.5) rgba(0, 0, 0, 0.1); /* Firefox */
}

.main-content,
.content,
.dashboard-container {
  -ms-overflow-style: none; /* IE and Edge */
  scrollbar-width: auto; /* Firefox */
  scrollbar-color: rgba(0, 162, 255, 0.5) rgba(0, 0, 0, 0.05); /* Firefox */
}

/* 添加滚动容器的平滑滚动并优化性能 */
html,
body,
.main-content,
.content,
.dashboard-container,
.module-content,
.data-table-container,
.sidebar {
  scroll-behavior: smooth;
  will-change: scroll-position;
}

/* 数据类型选择器样式 */
.data-type-selector {
  display: flex;
  align-items: center;
  gap: 8px;
}

.data-type-option {
  padding: 4px 10px;
  font-size: 12px;
  background: rgba(0, 0, 0, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.3s ease;
}

.data-type-option:hover {
  background: rgba(0, 162, 255, 0.2);
  border-color: rgba(0, 162, 255, 0.3);
}

.data-type-option.active {
  background: linear-gradient(135deg, #00a2ff, #0078d4);
  color: white;
  border-color: transparent;
  box-shadow: 0 2px 6px rgba(0, 162, 255, 0.3);
}

/* 效率标签样式 */
.efficiency-badge {
  padding: 3px 10px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: bold;
}

/* 地图背景层 */
.map-background {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  z-index: 0;
  opacity: 1; /* 完全不透明 */
  pointer-events: auto; /* 启用鼠标交互 */
  background:
    radial-gradient(
      circle at 20% 50%,
      rgba(0, 162, 255, 0.15) 0%,
      transparent 50%
    ),
    radial-gradient(
      circle at 80% 20%,
      rgba(255, 0, 150, 0.1) 0%,
      transparent 50%
    ),
    radial-gradient(
      circle at 40% 80%,
      rgba(0, 255, 135, 0.1) 0%,
      transparent 50%
    ),
    linear-gradient(135deg, #0a0f1c 0%, #1a1f3a 50%, #0f1a2e 100%);
}

/* 地图标记点样式 */
.map-decoration-marker {
  .marker-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    background-color: #fff;
    box-shadow: 0 0 10px rgba(255, 255, 255, 0.8);
    transition: all 0.3s ease;

    &.normal {
      background-color: #4fc3f7;
      box-shadow: 0 0 10px rgba(79, 195, 247, 0.8);
    }

    &.offline {
      background-color: #9e9e9e;
      box-shadow: 0 0 10px rgba(158, 158, 158, 0.8);
    }

    &.alarm {
      background-color: #ff5252;
      box-shadow: 0 0 10px rgba(255, 82, 82, 0.8);
      animation: blink 1s infinite alternate;
    }
  }

  .marker-label {
    position: absolute;
    bottom: -20px;
    left: 50%;
    transform: translateX(-50%);
    white-space: nowrap;
    color: #fff;
    font-size: 12px;
    font-weight: bold;
    text-shadow: 0 0 5px rgba(0, 0, 0, 0.8);
    background: rgba(0, 0, 0, 0.5);
    padding: 2px 8px;
    border-radius: 10px;
    opacity: 0;
    transition: all 0.3s ease;
  }

  &:hover {
    .marker-dot {
      transform: scale(1.5);
      box-shadow: 0 0 20px rgba(255, 255, 255, 1);
    }

    .marker-label {
      opacity: 1;
      bottom: -25px;
    }
  }

  &.normal {
    .marker-dot {
      background-color: #4fc3f7;
    }
  }

  &.offline {
    .marker-dot {
      background-color: #9e9e9e;
    }
  }

  &.alarm {
    .marker-dot {
      background-color: #ff5252;
    }
  }

  &.building .marker-dot {
    background-color: #4fc3f7;
  }
  &.solar .marker-dot {
    background-color: #ffeb3b;
  }
  &.warehouse .marker-dot {
    background-color: #ff9800;
  }
  &.charging .marker-dot {
    background-color: #4caf50;
  }
  &.water .marker-dot {
    background-color: #2196f3;
  }
  /* 添加新的设备类型样式 */
  &.zhkt .marker-dot {
    background-color: #00bcd4; /* 智慧空调 */
  }
  &.znzm .marker-dot {
    background-color: #ffc107; /* 智能照明 */
  }
  &.znms .marker-dot {
    background-color: #9c27b0; /* 智能门锁 */
  }
  &.znmj .marker-dot {
    background-color: #673ab7; /* 智能门禁 */
  }
  &.znsc .marker-dot {
    background-color: #03a9f4; /* 智能水池 */
  }
  &.znbj .marker-dot {
    background-color: #f44336; /* 智能报警 */
  }
}

/* SVG 图标样式 */
:deep(.my-custom-pin) {
  background: transparent;
  border: none;
  .marker-label {
    color: #fff;
    font-size: 12px;
    font-weight: bold;
    text-shadow: 0 0 5px rgba(0, 0, 0, 0.8);
    background: rgba(0, 0, 0, 0.5);
    padding: 2px 8px;
    border-radius: 10px;
    text-align: center;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    margin: 0 auto;
    opacity: 0.9;
    transition: all 0.3s ease;
  }
  &:hover .marker-label {
    opacity: 1;
    transform: translateY(2px);
    background: rgba(0, 0, 0, 0.7);
  }
}

:deep(.svg-icon-container) {
  display: flex;
  justify-content: center;
  align-items: center;
  border-radius: 50%;
  position: relative;
  border: 2px solid #ffffff;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.2);
  background-color: #fff;
  transition:
    transform 0.2s,
    box-shadow 0.2s;
  z-index: 2;

  &:hover {
    transform: scale(1.1);
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.3);
  }

  &.normal {
    border-color: #67c23a;
    .svg-icon {
      color: #67c23a;
    }
  }

  &.warning {
    border-color: #e6a23c;
    .svg-icon {
      color: #e6a23c;
    }
  }

  &.danger {
    border-color: #f56c6c;
    .svg-icon {
      color: #f56c6c;
    }
  }

  &.offline {
    border-color: #909399;
    .svg-icon {
      color: #909399;
    }
  }

  &.blink {
    animation: blink 1s infinite alternate;
  }
}

:deep(.svg-icon) {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 100%;
  height: 100%;
  padding: 4px;
}

:deep(.marker-bottom-text) {
  font-size: 12px;
  white-space: nowrap;
  color: #333;
  text-shadow:
    0 0 3px #fff,
    0 0 3px #fff,
    0 0 3px #fff;
  font-weight: bold;
  overflow: hidden;
  text-overflow: ellipsis;
  margin: 0 auto;
  max-width: 150px;
}

:deep(.paramList) {
  margin-top: 5px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 3px;
  max-width: 150px;
  /* 添加滚动条样式 */
  scrollbar-width: thin;
  scrollbar-color: rgba(0, 162, 255, 0.5) rgba(0, 0, 0, 0.1);
}

:deep(.paramList::-webkit-scrollbar) {
  width: 4px;
}

:deep(.paramList::-webkit-scrollbar-track) {
  background: rgba(0, 0, 0, 0.1);
  border-radius: 10px;
}

:deep(.paramList::-webkit-scrollbar-thumb) {
  background: rgba(0, 162, 255, 0.3);
  border-radius: 10px;
}

:deep(.paramList:hover::-webkit-scrollbar-thumb) {
  background: rgba(0, 162, 255, 0.8);
}

:deep(.selected-param) {
  font-size: 11px;
  background-color: rgba(255, 255, 255, 0.8);
  border-radius: 4px;
  padding: 2px 5px;
  display: flex;
  justify-content: space-between;
  width: 100%;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

:deep(.param-name) {
  color: #666;
  margin-right: 5px;
}

:deep(.param-value) {
  color: #333;
  font-weight: bold;
}

/* 脉冲效果 */
:deep(.pulse-circle) {
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0% {
    opacity: 0.7;
    transform: scale(0.8);
  }
  50% {
    opacity: 0.3;
    transform: scale(1.2);
  }
  100% {
    opacity: 0.7;
    transform: scale(0.8);
  }
}

@keyframes blink {
  0% {
    opacity: 1;
  }
  50% {
    opacity: 0.3;
  }
  100% {
    opacity: 1;
  }
}

/* 标记点弹出窗口样式 */
:deep(.marker-popup) {
  min-width: 200px;
  max-width: 300px;
  padding: 10px;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}

:deep(.marker-popup-header) {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #eee;
  padding-bottom: 8px;
  margin-bottom: 8px;

  h3 {
    margin: 0;
    font-size: 16px;
    color: #333;
  }
}

:deep(.status-badge) {
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: bold;
  color: white;

  &.normal {
    background-color: #67c23a;
  }

  &.warning {
    background-color: #e6a23c;
  }

  &.danger {
    background-color: #f56c6c;
  }

  &.offline {
    background-color: #909399;
  }
}

:deep(.marker-popup-content) {
  margin-bottom: 10px;
}

:deep(.params-table) {
  width: 100%;
  border-collapse: collapse;

  td {
    padding: 4px;
    border-bottom: 1px solid #f5f5f5;
    font-size: 12px;

    &:first-child {
      color: #666;
    }

    &:last-child {
      text-align: right;
      font-weight: bold;
      color: #333;
    }
  }

  tr:last-child td {
    border-bottom: none;
  }
}

:deep(.marker-popup-footer) {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

:deep(.popup-btn) {
  padding: 4px 8px;
  border-radius: 4px;
  border: 1px solid #dcdfe6;
  background: #f5f7fa;
  font-size: 12px;
  cursor: pointer;
  transition: all 0.3s;

  &:hover {
    background: #ecf5ff;
    color: #409eff;
    border-color: #c6e2ff;
  }
}

/* 过渡动画 */
.slide-fade-enter-active,
.slide-fade-leave-active {
  transition: all 0.3s ease;
}

.slide-fade-enter-from,
.slide-fade-leave-to {
  transform: translateX(20px);
  opacity: 0;
}

/* 添加开关样式的参数切换按钮 */
.param-toggle-switch {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background: rgba(0, 162, 255, 0.1);
  border: 1px solid rgba(0, 162, 255, 0.3);
  border-radius: 20px;
  font-size: 14px;
}

/* 开关样式 */
.switch {
  position: relative;
  display: inline-block;
  width: 40px;
  height: 20px;
}

.switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(255, 255, 255, 0.2);
  transition: 0.4s;
}

.slider:before {
  position: absolute;
  content: "";
  height: 16px;
  width: 16px;
  left: 2px;
  bottom: 2px;
  background-color: white;
  transition: 0.4s;
}

input:checked + .slider {
  background-color: #00a2ff;
}

input:focus + .slider {
  box-shadow: 0 0 1px #00a2ff;
}

input:checked + .slider:before {
  transform: translateX(20px);
}

/* 圆形滑块 */
.slider.round {
  border-radius: 34px;
}

.slider.round:before {
  border-radius: 50%;
}

/* 告警相关样式 */
.alert-loading,
.alert-empty {
  padding: 10px;
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 150px;
  width: 100%; /* 确保填满容器 */
  /* 设置一个与主题匹配的深色半透明背景，彻底杜绝白色闪烁 */
  background-color: rgba(13, 27, 48, 0.7) !important;
  border-radius: 8px; /* 添加圆角以匹配父容器 */
  backdrop-filter: blur(5px) !important;
  -webkit-backdrop-filter: blur(5px) !important;
}

.alert-loading .el-skeleton {
  width: 100%;
  background-color: transparent !important;
}

/* 确保骨架屏所有元素都是透明背景 */
.alert-loading .el-skeleton * {
  background-color: transparent !important;
}

.alert-loading .el-skeleton .el-skeleton__item {
  background: rgba(255, 255, 255, 0.1) !important;
  border-radius: 4px;
}

/* 覆盖骨架屏动画效果，防止白色闪烁 */
:deep(.el-skeleton.is-animated .el-skeleton__item::after) {
  background: linear-gradient(
    90deg,
    rgba(255, 255, 255, 0) 0%,
    rgba(255, 255, 255, 0.05) 50%,
    rgba(255, 255, 255, 0) 100%
  ) !important;
}

.alert-empty .el-empty {
  padding: 10px 0;
  background-color: transparent !important;
}

/* 确保所有内部元素都是透明背景 */
.alert-empty .el-empty * {
  background-color: transparent !important;
}

.alert-empty .el-empty__description {
  color: rgba(255, 255, 255, 0.7);
}

.alert-empty .el-empty__image {
  filter: brightness(1.5);
}

.module-actions {
  display: flex;
  align-items: center;
}

/* 告警列表 */
.alert-list {
  display: flex;
  flex-direction: column;
  width: 100%;
  max-height: 300px;
  overflow-y: auto;
  overflow-x: hidden;
  padding-right: 5px;
  position: relative;
  will-change: scroll-position;
  -webkit-overflow-scrolling: touch;
  background-color: transparent !important;
  color: rgba(255, 255, 255, 0.8);
}

/* 添加告警容器样式 */
.alert-container {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-bottom: 12px;
  will-change: transform;
}

.alert-list::-webkit-scrollbar {
  width: 4px;
  background-color: transparent;
}

.alert-list::-webkit-scrollbar-track {
  background: transparent;
}

.alert-list::-webkit-scrollbar-thumb {
  background: rgba(0, 162, 255, 0.3);
  border-radius: 10px;
}

.alert-list:hover::-webkit-scrollbar-thumb {
  background: rgba(0, 162, 255, 0.8);
}

.alert-item,
.alert-item-clone {
  background: rgba(0, 0, 0, 0.3);
  border-left: 4px solid;
  border-radius: 8px;
  padding: 12px;
  transition: all 0.3s ease;
  animation: alertFadeIn 0.5s ease-out forwards;
  transform-origin: top center;
  will-change: transform, opacity;
}

/* 数据值特效样式 */
@keyframes number-change {
  0% {
    transform: translateY(0);
    opacity: 0.5;
  }
  50% {
    transform: translateY(-10px);
    opacity: 0;
  }
  51% {
    transform: translateY(10px);
    opacity: 0;
  }
  100% {
    transform: translateY(0);
    opacity: 1;
  }
}

@keyframes number-shine {
  0% {
    text-shadow:
      0 0 5px rgba(255, 255, 255, 0),
      0 0 10px rgba(255, 255, 255, 0);
  }
  50% {
    text-shadow:
      0 0 10px rgba(255, 255, 255, 0.8),
      0 0 20px rgba(255, 255, 255, 0.4);
  }
  100% {
    text-shadow:
      0 0 5px rgba(255, 255, 255, 0),
      0 0 10px rgba(255, 255, 255, 0);
  }
}

@keyframes number-pulse {
  0% {
    transform: scale(1);
  }
  50% {
    transform: scale(1.05);
  }
  100% {
    transform: scale(1);
  }
}

@keyframes number-glow {
  0% {
    filter: brightness(1);
  }
  50% {
    filter: brightness(1.3);
  }
  100% {
    filter: brightness(1);
  }
}

@keyframes number-3d {
  0% {
    text-shadow:
      1px 1px 0 rgba(0, 0, 0, 0.3),
      2px 2px 0 rgba(0, 0, 0, 0.2);
    transform: translateZ(0px);
  }
  50% {
    text-shadow:
      2px 2px 0 rgba(0, 0, 0, 0.6),
      4px 4px 0 rgba(0, 0, 0, 0.3);
    transform: translateZ(5px);
  }
  100% {
    text-shadow:
      1px 1px 0 rgba(0, 0, 0, 0.3),
      2px 2px 0 rgba(0, 0, 0, 0.2);
    transform: translateZ(0px);
  }
}

.indicator-value {
  position: relative;
  transform-style: preserve-3d;
  perspective: 1000px;
  transition: all 0.3s ease;

  &.value-updated {
    animation:
      number-shine 1.2s ease-out,
      number-pulse 0.8s ease-out,
      number-glow 1.5s ease-in-out;
  }
}

.indicator-value.energy-gen-value,
.indicator-value.energy-con-value,
.indicator-value.energy-diff-value,
.indicator-value.carbon-total-value,
.indicator-value.carbon-save-value,
.indicator-value.water-usage-value {
  animation: number-3d 4s infinite ease-in-out;
}

/* 增强各指标卡片的数值样式 */
.indicator-card.energy-gen .indicator-value {
  color: #00ff87;
  text-shadow: 0 0 10px rgba(0, 255, 135, 0.4);
}

.indicator-card.energy-con .indicator-value {
  color: #ff6b6b;
  text-shadow: 0 0 10px rgba(255, 107, 107, 0.4);
}

.indicator-card.energy-diff .indicator-value {
  color: #4fc3f7;
  text-shadow: 0 0 10px rgba(79, 195, 247, 0.4);
}

.indicator-card.carbon-total .indicator-value {
  color: #ab47bc;
  text-shadow: 0 0 10px rgba(171, 71, 188, 0.4);
}

.indicator-card.carbon-save .indicator-value {
  color: #66bb6a;
  text-shadow: 0 0 10px rgba(102, 187, 106, 0.4);
}

.indicator-card.water-usage .indicator-value {
  color: #29b6f6;
  text-shadow: 0 0 10px rgba(41, 182, 246, 0.4);
}

/* 数字跳动效果 */
.number-animate {
  display: inline-block;
  transform-origin: center bottom;
}

.number-animate.animate {
  animation: number-jump 0.5s cubic-bezier(0.175, 0.885, 0.32, 1.275);
}

@keyframes number-jump {
  0% {
    transform: translateY(0) scale(1);
  }
  30% {
    transform: translateY(-15px) scale(1.2);
  }
  60% {
    transform: translateY(5px) scale(0.8);
  }
  100% {
    transform: translateY(0) scale(1);
  }
}

.map-params-toggle {
  position: fixed;
  top: 80px;
  right: 20px;
  background-color: rgba(0, 20, 40, 0.6);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(66, 165, 245, 0.3);
  border-radius: 12px;
  padding: 10px 15px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 180px;
  z-index: 990;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  transition: all 0.3s ease;
}

.map-params-toggle:hover {
  background-color: rgba(0, 20, 40, 0.8);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
  transform: translateY(-2px);
}

.map-params-toggle .toggle-label {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #ffffff;
  font-size: 14px;
  font-weight: 500;
}

.map-params-toggle .toggle-label .el-icon {
  color: #42a5f5;
}

.map-params-toggle .el-switch {
  transform: scale(0.9);
}

@media (max-width: 768px) {
  .map-params-toggle {
    width: 160px;
    top: 70px;
    right: 10px;
    padding: 8px 12px;
  }
}

/* 测试按钮样式 */
.test-button-container {
  position: fixed;
  bottom: 20px;
  right: 20px;
  z-index: 1000;
}
</style>
