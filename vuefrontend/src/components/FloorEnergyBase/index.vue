<template>
  <div class="floor-energy-base" :class="{ 'admin-mode': isAdmin }">
    <!-- 总能耗统计卡片组 -->
    <div class="stats-cards">
      <div class="stat-card day-card" :class="{ 'admin-card': isAdmin }">
        <div class="stat-icon day">
          <el-icon :size="isAdmin ? 32 : 40" class="animated-icon">
            <component
              :is="
                useRenderIcon(isAdmin ? 'mdi:chart-line' : 'mdi:weather-sunny')
              "
            />
          </el-icon>
        </div>
        <div class="stat-info">
          <div class="stat-label">日总用电量</div>
          <div class="stat-value animate-number">
            {{ totals.day.toFixed(1) }} <span>kWh</span>
          </div>
          <div v-if="isAdmin" class="admin-stat-trend">
            <span
              class="trend-value"
              :class="trendData.day >= 0 ? 'up' : 'down'"
            >
              {{ trendData.day >= 0 ? "+" : "" }}{{ trendData.day.toFixed(1) }}%
            </span>
            <span class="trend-text">较昨日</span>
          </div>
        </div>
        <div class="stat-chart">
          <div class="trend-line up" />
        </div>
        <div class="stat-decoration" />
      </div>

      <div class="stat-card month-card" :class="{ 'admin-card': isAdmin }">
        <div class="stat-icon month">
          <el-icon :size="isAdmin ? 32 : 40" class="animated-icon">
            <component
              :is="
                useRenderIcon(
                  isAdmin ? 'mdi:chart-arc' : 'mdi:calendar-month-outline'
                )
              "
            />
          </el-icon>
        </div>
        <div class="stat-info">
          <div class="stat-label">月总用电量</div>
          <div class="stat-value animate-number">
            {{ totals.month.toFixed(1) }} <span>kWh</span>
          </div>
          <div v-if="isAdmin" class="admin-stat-trend">
            <span
              class="trend-value"
              :class="trendData.month >= 0 ? 'up' : 'down'"
            >
              {{ trendData.month >= 0 ? "+" : ""
              }}{{ trendData.month.toFixed(1) }}%
            </span>
            <span class="trend-text">较上月</span>
          </div>
        </div>
        <div class="stat-chart">
          <div class="trend-line down" />
        </div>
        <div class="stat-decoration" />
      </div>

      <div class="stat-card year-card" :class="{ 'admin-card': isAdmin }">
        <div class="stat-icon year">
          <el-icon :size="isAdmin ? 32 : 40" class="animated-icon">
            <component
              :is="useRenderIcon(isAdmin ? 'mdi:chart-bar' : 'mdi:chart-bar')"
            />
          </el-icon>
        </div>
        <div class="stat-info">
          <div class="stat-label">年总用电量</div>
          <div class="stat-value animate-number">
            {{ totals.year.toFixed(1) }} <span>kWh</span>
          </div>
          <div v-if="isAdmin" class="admin-stat-trend">
            <span
              class="trend-value"
              :class="trendData.year >= 0 ? 'up' : 'down'"
            >
              {{ trendData.year >= 0 ? "+" : ""
              }}{{ trendData.year.toFixed(1) }}%
            </span>
            <span class="trend-text">较去年</span>
          </div>
        </div>
        <div class="stat-chart">
          <div class="trend-line up" />
        </div>
        <div class="stat-decoration" />
      </div>
    </div>

    <!-- 加载状态 -->
    <div v-if="loading" class="loading-container">
      <el-icon class="is-loading"><Loading /></el-icon>
      <span>加载中...</span>
    </div>

    <!-- 所有楼层卡片区域 -->
    <template v-else>
      <div class="time-section">
        <div class="time-title">
          <div class="time-icon day">
            <el-icon :size="20"
              ><component :is="useRenderIcon('mdi:chart-line')"
            /></el-icon>
          </div>
          <h3>楼层用电数据</h3>
        </div>
        <div
          class="floor-cards-container"
          :class="{ 'admin-cards-container': isAdmin }"
        >
          <template v-if="combinedData.length > 0">
            <FloorEnergyCard
              v-for="(floorItem, index) in combinedData.slice(0, limit)"
              :key="`floor-${floorItem.name}-${index}`"
              :floor-data="{
                name: floorItem.name,
                power: floorItem.dayPower,
                rate: floorItem.dayRate
              }"
              :index="index"
              :color-index="index"
              :is-active="false"
              :is-admin="isAdmin"
              :day-power="floorItem.dayPower"
              :day-rate="floorItem.dayRate"
              :month-power="floorItem.monthPower"
              :year-power="floorItem.yearPower"
              :custom-name="floorItem.name"
            />
          </template>

          <div v-if="combinedData.length === 0" class="no-data">
            <el-icon><WarningFilled /></el-icon>
            <span>暂无数据</span>
          </div>
        </div>
      </div>
    </template>

    <!-- 插槽用于各场景特定内容 -->
    <slot name="additional-content" />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, PropType, onMounted } from "vue";
import FloorEnergyCard from "@/views/home/detail/FloorEnergyCard.vue";
import { GetFloorEnergyBar } from "@/api/home/home";
import { Loading, WarningFilled } from "@element-plus/icons-vue";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";

// API返回数据接口
interface ApiResponseData {
  ChartX: string[];
  ChartTuY: {
    ChartTuLi: string;
    ChartTuLiId: string;
    ChartY: string[];
  }[];
}

// API响应结构
interface ApiResponse {
  Message: string;
  Result: string; // JSON字符串
  Status: boolean;
  Timestamp: string;
  Total: number;
}

interface FloorDataItem {
  name: string;
  power: number;
  rate: number;
}

interface FloorDataByTime {
  totalPower: number;
  tableData: FloorDataItem[];
}

interface FloorDataState {
  day: FloorDataByTime;
  month: FloorDataByTime;
  year: FloorDataByTime;
}

// 合并后的楼层数据接口
interface CombinedFloorItem {
  name: string;
  dayPower: number;
  dayRate: number;
  monthPower: number;
  monthRate: number;
  yearPower: number;
  yearRate: number;
}

defineProps({
  // 支持外部传入数据或使用内部默认数据
  providedData: {
    type: Object as PropType<FloorDataState>,
    required: false
  },
  title: {
    type: String,
    default: "楼层用电数据"
  },
  dark: {
    type: Boolean,
    default: true
  },
  limit: {
    type: Number,
    default: 8
  },
  isAdmin: {
    type: Boolean,
    default: true
  }
});

defineEmits(["tab-change"]);

const loading = ref(true);
const combinedData = ref<CombinedFloorItem[]>([]);
const totals = reactive({
  day: 0,
  month: 0,
  year: 0
});
const trendData = reactive({
  day: 0,
  month: 0,
  year: 0
});

/**
 * 集中处理API数据，并合并成最终的楼层列表
 */
const processAndCombineAllData = (apiResults: {
  day: ApiResponseData | null;
  month: ApiResponseData | null;
  year: ApiResponseData | null;
  prevDay: Map<string, number> | null;
}) => {
  const floorDataMap = new Map<string, Partial<CombinedFloorItem>>();

  // 辅助函数，用于解析单个时间维度的数据
  const processSingleType = (
    data: ApiResponseData | null,
    type: "day" | "month" | "year"
  ) => {
    if (!data || !data.ChartX || !data.ChartTuY) return;
    const currentData = data.ChartTuY.find(item => item.ChartTuLiId === "fcur");
    if (!currentData || !currentData.ChartY) return;

    data.ChartX.forEach((name, index) => {
      const power = parseFloat(currentData.ChartY[index]) || 0;
      if (power <= 0) return; // 提前过滤掉无用电的楼层

      if (!floorDataMap.has(name)) {
        floorDataMap.set(name, { name });
      }
      const floor = floorDataMap.get(name);
      if (type === "day") floor.dayPower = power;
      else if (type === "month") floor.monthPower = power;
      else if (type === "year") floor.yearPower = power;
    });
  };

  processSingleType(apiResults.day, "day");
  processSingleType(apiResults.month, "month");
  processSingleType(apiResults.year, "year");

  // 从Map转换回数组，并填充缺失数据
  const result: CombinedFloorItem[] = Array.from(floorDataMap.values())
    .filter(item => item.dayPower && item.dayPower > 0) // 确保有日用电数据
    .map(item => ({
      name: item.name,
      dayPower: item.dayPower || 0,
      dayRate: 0, // 稍后计算
      monthPower: item.monthPower || 0,
      monthRate: 0,
      yearPower: item.yearPower || 0,
      yearRate: 0
    }));

  // 重新计算总用电量
  const dayTotal = result.reduce((sum, item) => sum + item.dayPower, 0);
  const monthTotal = result.reduce((sum, item) => sum + item.monthPower, 0);
  const yearTotal = result.reduce((sum, item) => sum + item.yearPower, 0);

  totals.day = dayTotal;
  totals.month = monthTotal;
  totals.year = yearTotal;

  // 计算各楼层占比
  result.forEach(item => {
    item.dayRate = dayTotal > 0 ? (item.dayPower / dayTotal) * 100 : 0;
    item.monthRate = monthTotal > 0 ? (item.monthPower / monthTotal) * 100 : 0;
    item.yearRate = yearTotal > 0 ? (item.yearPower / yearTotal) * 100 : 0;
  });

  // 按日用电量从高到低排序
  result.sort((a, b) => b.dayPower - a.dayPower);
  combinedData.value = result;

  // --- 计算环比 ---
  // 日环比
  if (apiResults.prevDay && dayTotal > 0) {
    let prevDayTotal = 0;
    result.forEach(item => {
      prevDayTotal += apiResults.prevDay.get(item.name) || 0;
    });
    if (prevDayTotal > 0) {
      trendData.day = ((dayTotal - prevDayTotal) / prevDayTotal) * 100;
    } else {
      trendData.day = 0; // 如果前一天数据为0，则环比为0
    }
  } else {
    trendData.day = 0; // 没有前一天数据时
  }

  // 月环比 (注意：这里仍然是旧的随机逻辑，需要替换为真实数据)
  if (monthTotal > 0) {
    const prevMonth = monthTotal * (0.95 + Math.random() * 0.2); // 占位符
    trendData.month = ((monthTotal - prevMonth) / prevMonth) * 100;
  }

  // 年环比 (注意：这里仍然是旧的随机逻辑，需要替换为真实数据)
  if (yearTotal > 0) {
    const prevYear = yearTotal * (0.85 + Math.random() * 0.3); // 占位符
    trendData.year = ((yearTotal - prevYear) / prevYear) * 100;
  }

  console.log("数据处理和合并完成:", {
    totals,
    trends: trendData,
    combinedData: combinedData.value
  });
};

// 并行获取所有楼层用电数据
const fetchAllFloorEnergyData = async () => {
  loading.value = true;
  try {
    console.log("开始并行获取楼层用电数据...");

    const [dayRes, monthRes, yearRes] = await Promise.all([
      GetFloorEnergyBar({ DataType: 2 }), // Day
      GetFloorEnergyBar({ DataType: 4 }), // Month
      GetFloorEnergyBar({ DataType: 5 }) // Year
    ]);

    console.log("API数据返回:", { dayRes, monthRes, yearRes });

    const parseResult = (res: ApiResponse): ApiResponseData | null => {
      if (res && res.Status && res.Result) {
        try {
          return typeof res.Result === "string"
            ? JSON.parse(res.Result)
            : res.Result;
        } catch (e) {
          console.error("解析API Result失败:", e);
          return null;
        }
      }
      console.warn("API返回格式不正确或状态失败:", res);
      return null;
    };

    const dayData = parseResult(dayRes);
    const monthData = parseResult(monthRes);
    const yearData = parseResult(yearRes);

    let prevDayMap: Map<string, number> | null = null;
    if (dayData && dayData.ChartTuY) {
      const prevDayData = dayData.ChartTuY.find(
        item => item.ChartTuLiId === "fpre"
      );
      if (prevDayData && prevDayData.ChartY && dayData.ChartX) {
        prevDayMap = new Map();
        dayData.ChartX.forEach((name, index) => {
          prevDayMap.set(name, parseFloat(prevDayData.ChartY[index]) || 0);
        });
      }
    }

    processAndCombineAllData({
      day: dayData,
      month: monthData,
      year: yearData,
      prevDay: prevDayMap
    });
  } catch (error) {
    console.error("获取楼层用电数据失败", error);
    // 发生错误时重置数据
    combinedData.value = [];
    totals.day = 0;
    totals.month = 0;
    totals.year = 0;
  } finally {
    loading.value = false;
  }
};

// 刷新数据
const refreshData = () => {
  console.log("手动刷新数据");
  fetchAllFloorEnergyData();
};

// 当组件挂载时一次性获取所有数据
onMounted(() => {
  console.log("组件挂载，获取初始数据");
  fetchAllFloorEnergyData();
});
</script>

<style scoped>
/* 基础样式 */
.floor-energy-base {
  display: flex;
  flex-direction: column;
  gap: 25px;
  padding: 5px;
  animation: fadeIn 0.6s ease-out;
  color: #fff;
}

/* 管理后台模式 */
.floor-energy-base.admin-mode {
  color: #303133;
  background-color: #f5f7fa;
  padding: 20px;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.05);
  border: 1px solid #ebeef5;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* 节标题样式 */
.section-title {
  display: flex;
  align-items: center;
  margin-bottom: 0;
  position: relative;
}

.section-title h2 {
  font-size: 22px;
  color: #fff;
  margin: 0;
  font-weight: 500;
  position: relative;
  padding-left: 15px;
}

.title-icon {
  width: 4px;
  height: 22px;
  background: linear-gradient(to bottom, #00c9ff, #92fe9d);
  border-radius: 2px;
  margin-right: 10px;
}

/* 统计卡片样式 */
.stats-cards {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
  margin-bottom: 10px;
}

/* 后台模式统计卡片样式 */
.admin-mode .stats-cards {
  gap: 16px;
  margin-bottom: 20px;
}

.stat-card {
  background: rgba(6, 24, 44, 0.75);
  border-radius: 12px;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 15px;
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.08);
  backdrop-filter: blur(10px);
  transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
  position: relative;
  overflow: hidden;
}

/* 后台管理卡片样式 */
.admin-card {
  background: #ffffff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  border: 1px solid #ebeef5;
  border-radius: 8px;
  transition: all 0.25s ease;
  position: relative;
}

/* 后台卡片顶部彩色指示条 */
.admin-mode .day-card {
  border-top: 3px solid #409eff;
  border-left: none;
}

.admin-mode .month-card {
  border-top: 3px solid #67c23a;
  border-left: none;
}

.admin-mode .year-card {
  border-top: 3px solid #e6a23c;
  border-left: none;
}

/* 后台图标样式 */
.admin-mode .stat-icon {
  background: #f5f7fa;
  box-shadow: none;
  border: 1px solid #ebeef5;
}

.admin-mode .stat-icon::after {
  display: none;
}

.admin-mode .stat-icon.day {
  color: #409eff;
  background: rgba(64, 158, 255, 0.1);
  box-shadow: none;
}

.admin-mode .stat-icon.month {
  color: #67c23a;
  background: rgba(103, 194, 58, 0.1);
  box-shadow: none;
}

.admin-mode .stat-icon.year {
  color: #e6a23c;
  background: rgba(230, 162, 60, 0.1);
  box-shadow: none;
}

.admin-stat-trend {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  margin-top: 8px;
  padding: 4px 8px;
  background-color: #f5f7fa;
  border-radius: 4px;
  width: fit-content;
}

.trend-value {
  font-weight: 600;
}

.trend-value.up {
  color: #67c23a;
}

.trend-value.down {
  color: #f56c6c;
}

.trend-text {
  color: #909399;
}

.stat-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 15px 30px rgba(0, 0, 0, 0.3);
  border-color: rgba(255, 255, 255, 0.2);
}

.admin-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
}

.admin-mode .day-card:hover {
  border-top-color: #409eff;
  box-shadow: 0 8px 15px rgba(64, 158, 255, 0.15);
}

.admin-mode .month-card:hover {
  border-top-color: #67c23a;
  box-shadow: 0 8px 15px rgba(103, 194, 58, 0.15);
}

.admin-mode .year-card:hover {
  border-top-color: #e6a23c;
  box-shadow: 0 8px 15px rgba(230, 162, 60, 0.15);
}

.stat-decoration {
  position: absolute;
  top: 0;
  right: 0;
  width: 150px;
  height: 150px;
  background-color: rgba(255, 255, 255, 0.03);
  border-radius: 50%;
  transform: translate(30%, -30%);
  pointer-events: none;
}

.stat-chart {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 80px;
  height: 40px;
  padding: 10px;
  opacity: 0.5;
}

.trend-line {
  position: relative;
  height: 100%;
  width: 100%;
}

.trend-line.up::before {
  content: "";
  position: absolute;
  height: 100%;
  width: 100%;
  background: linear-gradient(
    90deg,
    transparent 0%,
    rgba(0, 255, 135, 0.2) 50%,
    rgba(0, 255, 135, 0.5) 100%
  );
  clip-path: polygon(
    0 70%,
    20% 50%,
    40% 60%,
    60% 40%,
    80% 30%,
    100% 10%,
    100% 100%,
    0 100%
  );
}

.trend-line.down::before {
  content: "";
  position: absolute;
  height: 100%;
  width: 100%;
  background: linear-gradient(
    90deg,
    transparent 0%,
    rgba(255, 97, 97, 0.2) 50%,
    rgba(255, 97, 97, 0.5) 100%
  );
  clip-path: polygon(
    0 30%,
    20% 40%,
    40% 30%,
    60% 50%,
    80% 60%,
    100% 80%,
    100% 100%,
    0 100%
  );
}

.day-card {
  border-left: 4px solid #00c9ff;
}

.month-card {
  border-left: 4px solid #00ff87;
}

.year-card {
  border-left: 4px solid #ffd33d;
}

.stat-icon {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  filter: drop-shadow(0 4px 6px rgba(0, 0, 0, 0.2));
  position: relative;
}

.stat-icon::after {
  content: "";
  position: absolute;
  width: 100%;
  height: 100%;
  border-radius: 50%;
  background: inherit;
  opacity: 0.4;
  filter: blur(8px);
  z-index: -1;
  transform: scale(1.2);
}

.stat-icon.day {
  background: linear-gradient(
    135deg,
    rgba(0, 162, 255, 0.3),
    rgba(0, 201, 255, 0.1)
  );
  color: #00c9ff;
  box-shadow: 0 0 15px rgba(0, 201, 255, 0.3);
}

.stat-icon.month {
  background: linear-gradient(
    135deg,
    rgba(0, 255, 135, 0.3),
    rgba(0, 255, 135, 0.1)
  );
  color: #00ff87;
  box-shadow: 0 0 15px rgba(0, 255, 135, 0.3);
}

.stat-icon.year {
  background: linear-gradient(
    135deg,
    rgba(255, 197, 61, 0.3),
    rgba(255, 197, 61, 0.1)
  );
  color: #ffd33d;
  box-shadow: 0 0 15px rgba(255, 197, 61, 0.3);
}

.stat-info {
  flex: 1;
  z-index: 1;
  position: relative;
}

.stat-label {
  font-size: 15px;
  color: rgba(255, 255, 255, 0.7);
  margin-bottom: 8px;
}

.stat-value {
  font-size: 28px;
  font-weight: 600;
  background: linear-gradient(45deg, #ffffff, #d0d0d0);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  text-shadow: 0 2px 10px rgba(0, 0, 0, 0.2);
}

.animate-number {
  animation: pulseValue 2s infinite alternate;
}

@keyframes pulseValue {
  0% {
    opacity: 0.9;
  }
  100% {
    opacity: 1;
    transform: scale(1.03);
  }
}

.stat-value span {
  font-size: 16px;
  opacity: 0.7;
  margin-left: 3px;
  font-weight: 400;
}

/* 控制栏 */
.control-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  background: rgba(6, 24, 44, 0.75);
  border-radius: 12px;
  padding: 15px 20px;
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.2);
  border: 1px solid rgba(0, 201, 255, 0.3);
  position: relative;
}

/* 后台管理控制栏样式 */
.admin-control-bar {
  background: #ffffff;
  border: 1px solid #ebeef5;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.05);
  padding: 12px 16px;
  margin-bottom: 16px;
}

.admin-control-bar::after {
  display: none;
}

.admin-title-icon {
  background: linear-gradient(to bottom, #409eff, #67c23a);
  height: 20px;
}

.admin-mode .control-bar {
  margin-top: 5px;
}

.admin-mode .section-title h2 {
  color: #303133;
}

.admin-mode .stat-label {
  color: #606266;
}

.admin-mode .stat-value {
  background: none;
  -webkit-text-fill-color: initial;
  color: #303133;
  text-shadow: none;
}

.admin-controls {
  display: flex;
  gap: 10px;
  margin-left: 15px;
}

.control-bar::after {
  content: "";
  position: absolute;
  bottom: 0;
  left: 0;
  width: 100%;
  height: 1px;
  background: linear-gradient(
    to right,
    transparent,
    rgba(0, 201, 255, 0.5),
    transparent
  );
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

.chart-controls {
  display: flex;
  align-items: center;
}

.tab-selector {
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}

/* 楼层卡片容器 */
.floor-cards-container {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
  padding: 10px 0;
  max-height: 100%;
  width: 100%;
}

/* 后台管理卡片容器样式 */
.admin-cards-container {
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
  padding: 10px 0;
  position: relative;
}

/* 后台卡片阴影叠加效果 */
.admin-cards-container:after {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  box-shadow: inset 0 0 20px rgba(0, 0, 0, 0.03);
  pointer-events: none;
  border-radius: 8px;
  z-index: -1;
}

/* 适配移动端 */
@media (max-width: 1400px) {
  .floor-cards-container {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 1200px) {
  .floor-cards-container {
    grid-template-columns: repeat(2, 1fr);
  }

  .stats-cards {
    grid-template-columns: repeat(3, 1fr);
  }

  .control-bar {
    flex-direction: row;
    align-items: center;
    gap: 10px;
  }
}

@media (max-width: 768px) {
  .floor-cards-container {
    grid-template-columns: 1fr;
  }

  .stats-cards {
    grid-template-columns: 1fr;
  }

  .stat-value {
    font-size: 22px;
  }
}

/* 后台模式楼层卡片样式额外优化 */
.admin-mode .floor-cards-container {
  margin-top: 10px;
}

.admin-mode .stat-card {
  padding: 16px;
}

.admin-mode .stat-value {
  font-size: 24px;
  margin-bottom: 5px;
  font-weight: 500;
  transition: all 0.2s ease;
}

.admin-mode .stat-value span {
  color: #909399;
  margin-left: 4px;
}

.admin-mode .stat-card:hover .stat-value {
  color: #409eff;
}

.admin-mode .stat-card:hover .stat-icon {
  transform: scale(1.1);
}

/* 改进后台模式图标 */
.admin-mode .stat-icon {
  transition: transform 0.3s ease;
  width: 48px;
  height: 48px;
}

/* 更好的后台模式卡片标签 */
.admin-mode .tab-selector {
  background-color: #ffffff;
  border-radius: 4px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

/* 添加背景区分度 */
.admin-mode {
  background-image: linear-gradient(
    to bottom right,
    rgba(240, 244, 248, 0.5),
    rgba(255, 255, 255, 1)
  );
}

/* 加载中状态 */
.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 200px;
  gap: 15px;
}

.loading-container .el-icon {
  font-size: 32px;
  color: var(--el-color-primary);
}

.loading-container span {
  font-size: 14px;
  color: var(--el-text-color-secondary);
}

/* 无数据状态 */
.no-data {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 200px;
  gap: 10px;
  grid-column: 1 / -1;
  border: 1px dashed #dcdfe6;
  border-radius: 8px;
  background-color: #fafafa;
}

.no-data .el-icon {
  font-size: 32px;
  color: var(--el-color-warning);
}

.no-data span {
  font-size: 14px;
  color: var(--el-text-color-secondary);
}

/* 时间区域标题 */
.time-section {
  margin-top: 20px;
  margin-bottom: 10px;
}

.time-title {
  display: flex;
  align-items: center;
  margin-bottom: 15px;
  padding-left: 10px;
  border-left: 4px solid transparent;
}

.time-title h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 500;
}

.time-icon {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 10px;
}

.time-icon.day {
  background: rgba(0, 201, 255, 0.2);
  color: #00c9ff;
}

.time-title:has(.time-icon.day) {
  border-left-color: #00c9ff;
}

.time-icon.month {
  background: rgba(0, 255, 135, 0.2);
  color: #00ff87;
}

.time-title:has(.time-icon.month) {
  border-left-color: #00ff87;
}

.time-icon.year {
  background: rgba(255, 197, 61, 0.2);
  color: #ffd33d;
}

.time-title:has(.time-icon.year) {
  border-left-color: #ffd33d;
}

.admin-mode .time-icon.day {
  background: rgba(64, 158, 255, 0.1);
  color: #409eff;
}

.admin-mode .time-title:has(.time-icon.day) {
  border-left-color: #409eff;
}

.admin-mode .time-icon.month {
  background: rgba(103, 194, 58, 0.1);
  color: #67c23a;
}

.admin-mode .time-title:has(.time-icon.month) {
  border-left-color: #67c23a;
}

.admin-mode .time-icon.year {
  background: rgba(230, 162, 60, 0.1);
  color: #e6a23c;
}

.admin-mode .time-title:has(.time-icon.year) {
  border-left-color: #e6a23c;
}

.animated-icon {
  animation: pulseIcon 3s ease-in-out infinite;
}

@keyframes pulseIcon {
  0% {
    transform: scale(1);
    filter: drop-shadow(0 0 4px currentColor);
  }
  50% {
    transform: scale(1.15);
    filter: drop-shadow(0 0 10px currentColor);
  }
  100% {
    transform: scale(1);
    filter: drop-shadow(0 0 4px currentColor);
  }
}
</style>
