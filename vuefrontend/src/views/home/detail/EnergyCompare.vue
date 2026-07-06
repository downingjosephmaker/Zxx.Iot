<script setup lang="ts">
import {
  ref,
  onMounted,
  reactive,
  watch,
  onUnmounted,
  defineExpose
} from "vue";
import { useRouter, useRoute } from "vue-router";
import * as echarts from "echarts";
import type { EChartsOption } from "echarts";
import api from "@/api/home/home";
import dayjs from "dayjs";
import {
  ElDatePicker,
  ElSelect,
  ElOption,
  ElTable,
  ElTableColumn,
  ElButton
} from "element-plus";
import DetailLayout from "./DetailLayout.vue";
import DashboardHeader from "./DashboardHeader.vue";

// 日期类型选项
const dateType = ref<"day" | "month" | "year">("month"); // 默认选择月
const dateOptions = [
  { label: "日", value: "day" },
  { label: "月", value: "month" },
  { label: "年", value: "year" }
];

// 设置是否需要日期选择器
const needDateSelector = true;

// 自定义时间
const customDateRange = ref<[Date, Date] | []>([]);
const isCustomDate = ref(false);

// 图表引用
const mainChartRef = ref<HTMLElement | null>(null);
const trendChartRef = ref<HTMLElement | null>(null);
const distributionChartRef = ref<HTMLElement | null>(null);
let mainChart: echarts.ECharts | null = null;
let trendChart: echarts.ECharts | null = null;
let distributionChart: echarts.ECharts | null = null;

// 能耗数据
interface TableDataItem {
  date: string;
  production: number;
  consumption: number;
  difference: number;
  selfSufficiency: string | number;
}
interface EnergyData {
  productToday: number;
  productYesterday: number;
  productRate: number;
  costToday: number;
  costYesterday: number;
  costRate: number;
  remainToday: number;
  remainRate: number;
  dates: string[];
  productData: number[];
  costData: number[];
  tableData: TableDataItem[];
}
const energyData = reactive<EnergyData>({
  productToday: 0,
  productYesterday: 0,
  productRate: 0,
  costToday: 0,
  costYesterday: 0,
  costRate: 0,
  remainToday: 0,
  remainRate: 0,
  dates: [],
  productData: [],
  costData: [],
  tableData: []
});

// 指标标题映射
const titleMapping = reactive({
  day: {
    product: "今日发电量",
    cost: "今日用电量",
    remain: "差值电量",
    compareText: "同比昨日"
  },
  month: {
    product: "本月发电量",
    cost: "本月用电量",
    remain: "差值电量",
    compareText: "同比上月"
  },
  year: {
    product: "本年发电量",
    cost: "本年用电量",
    remain: "差值电量",
    compareText: "同比去年"
  }
});

// 获取能源生产与消耗数据
const fetchEnergyData = async () => {
  try {
    // 根据日期类型设置不同的参数
    let dataTypeParam = 2; // 默认为日
    let startDate, endDate;

    if (isCustomDate.value && customDateRange.value.length === 2) {
      // 使用自定义日期
      startDate = dayjs(customDateRange.value[0]).format("YYYY-MM-DD");
      endDate = dayjs(customDateRange.value[1]).format("YYYY-MM-DD");
    } else {
      // 使用日期类型
      if (dateType.value === "day") {
        dataTypeParam = 2; //日
      } else if (dateType.value === "month") {
        dataTypeParam = 4; // 月
      } else if (dateType.value === "year") {
        dataTypeParam = 5; // 年
      }
    }

    const params: { DataType: number; StartDate?: string; EndDate?: string } = {
      DataType: dataTypeParam
    };

    if (startDate && endDate) {
      params.StartDate = startDate;
      params.EndDate = endDate;
    }

    // 调用接口获取数据
    const response = await api.GetEnergyProductCostLine(params);
    if (response && response.Status && response.Result) {
      // 解析返回的JSON字符串
      interface ChartData {
        ChartX: string[];
        ChartTuY: { ChartY: string[] }[];
      }
      const chartData: ChartData = JSON.parse(response.Result);

      // 清空旧数据
      energyData.dates = [];
      energyData.productData = [];
      energyData.costData = [];
      energyData.tableData = [];

      // 处理X轴日期数据
      if (chartData.ChartX && chartData.ChartX.length > 0) {
        energyData.dates = chartData.ChartX;
      }

      // 处理Y轴数据
      if (chartData.ChartTuY && chartData.ChartTuY.length > 0) {
        // 获取生产数据
        if (chartData.ChartTuY[0] && chartData.ChartTuY[0].ChartY) {
          energyData.productData = chartData.ChartTuY[0].ChartY.map(value =>
            parseFloat(value)
          );
        }

        // 获取消耗数据
        if (chartData.ChartTuY[1] && chartData.ChartTuY[1].ChartY) {
          energyData.costData = chartData.ChartTuY[1].ChartY.map(value =>
            parseFloat(value)
          );
        }
      }

      // 处理表格数据
      for (let i = 0; i < energyData.dates.length; i++) {
        energyData.tableData.push({
          date: energyData.dates[i],
          production: energyData.productData[i] || 0,
          consumption: energyData.costData[i] || 0,
          difference:
            (energyData.productData[i] || 0) - (energyData.costData[i] || 0),
          selfSufficiency: energyData.costData[i]
            ? (
                ((energyData.productData[i] || 0) /
                  (energyData.costData[i] || 1)) *
                100
              ).toFixed(1)
            : "0"
        });
      }

      // 计算今日/本月/本年数据
      if (energyData.productData.length > 0) {
        const recentIndex = energyData.productData.length - 1;
        energyData.productToday = energyData.productData[recentIndex];
        energyData.costToday = energyData.costData[recentIndex];
        energyData.remainToday = energyData.productToday - energyData.costToday;

        // 计算同比率
        if (recentIndex > 0) {
          const previousIndex = recentIndex - 1;
          energyData.productYesterday = energyData.productData[previousIndex];
          energyData.costYesterday = energyData.costData[previousIndex];

          // 计算同比增长率
          energyData.productRate = energyData.productYesterday
            ? ((energyData.productToday - energyData.productYesterday) /
                energyData.productYesterday) *
              100
            : 0;

          energyData.costRate = energyData.costYesterday
            ? ((energyData.costToday - energyData.costYesterday) /
                energyData.costYesterday) *
              100
            : 0;
        } else {
          energyData.productRate = 0;
          energyData.costRate = 0;
        }

        // 计算自给率
        energyData.remainRate = energyData.costToday
          ? (energyData.productToday / energyData.costToday) * 100
          : 0;
      }

      // 更新图表
      updateMainChart();
      updateTrendChart();
      updateDistributionChart();
    }
  } catch (error) {
    console.error("获取能源生产与消耗数据失败:", error);
  }
};

// 更新主图表
const updateMainChart = () => {
  if (!mainChartRef.value) return;

  if (!mainChart) {
    mainChart = echarts.init(mainChartRef.value);
  }

  // 设置图表配置
  const option: EChartsOption = {
    title: {
      text: "能源生产与消耗对比",
      left: "center",
      textStyle: {
        color: "#fff"
      }
    },
    tooltip: {
      trigger: "axis",
      axisPointer: {
        type: "cross",
        label: {
          backgroundColor: "#6a7985"
        }
      }
    },
    legend: {
      data: ["发电量", "用电量"],
      textStyle: {
        color: "#fff"
      },
      top: "30px"
    },
    grid: {
      top: "80px",
      left: "3%",
      right: "4%",
      bottom: "3%",
      containLabel: true
    },
    xAxis: {
      type: "category",
      boundaryGap: false,
      data: energyData.dates,
      axisLabel: {
        color: "#fff"
      }
    },
    yAxis: {
      type: "value",
      axisLabel: {
        formatter: "{value} kWh",
        color: "#fff"
      }
    },
    series: [
      {
        name: "发电量",
        type: "line",
        stack: "总量",
        data: energyData.productData,
        areaStyle: {},
        emphasis: {
          focus: "series"
        }
      },
      {
        name: "用电量",
        type: "line",
        stack: "总量",
        data: energyData.costData,
        areaStyle: {},
        emphasis: {
          focus: "series"
        }
      }
    ]
  };
  mainChart.setOption(option);
};

// 更新趋势图表
const updateTrendChart = () => {
  if (!trendChartRef.value) return;

  if (!trendChart) {
    trendChart = echarts.init(trendChartRef.value);
  }

  const option: EChartsOption = {
    title: {
      text: "生产/消耗趋势",
      left: "center",
      textStyle: {
        color: "#fff"
      }
    },
    tooltip: {
      trigger: "axis"
    },
    legend: {
      data: ["发电量", "用电量"],
      top: 30,
      textStyle: {
        color: "#fff"
      }
    },
    grid: {
      left: "3%",
      right: "4%",
      bottom: "3%",
      containLabel: true
    },
    xAxis: {
      type: "category",
      boundaryGap: false,
      data: energyData.dates,
      axisLabel: {
        color: "#fff"
      }
    },
    yAxis: {
      type: "value",
      axisLabel: {
        color: "#fff"
      }
    },
    series: [
      {
        name: "发电量",
        type: "line",
        data: energyData.productData,
        smooth: true,
        itemStyle: {
          color: "#00ffab"
        }
      },
      {
        name: "用电量",
        type: "line",
        data: energyData.costData,
        smooth: true,
        itemStyle: {
          color: "#ff6347"
        }
      }
    ]
  };
  trendChart.setOption(option);
};

// 更新分布图表
const updateDistributionChart = () => {
  if (!distributionChartRef.value) return;

  if (!distributionChart) {
    distributionChart = echarts.init(distributionChartRef.value);
  }
  const totalProduction = energyData.productData.reduce((a, b) => a + b, 0);
  const totalConsumption = energyData.costData.reduce((a, b) => a + b, 0);

  const option: EChartsOption = {
    title: {
      text: "生产/消耗分布",
      left: "center",
      textStyle: {
        color: "#fff"
      }
    },
    tooltip: {
      trigger: "item",
      formatter: "{a} <br/>{b}: {c} ({d}%)"
    },
    legend: {
      orient: "vertical",
      left: 10,
      data: ["总发电量", "总用电量"],
      textStyle: {
        color: "#fff"
      }
    },
    series: [
      {
        name: "能耗分布",
        type: "pie",
        radius: ["50%", "70%"],
        avoidLabelOverlap: false,
        label: {
          show: false,
          position: "center"
        },
        emphasis: {
          label: {
            show: true,
            fontSize: "30",
            fontWeight: "bold"
          }
        },
        labelLine: {
          show: false
        },
        data: [
          { value: totalProduction, name: "总发电量" },
          { value: totalConsumption, name: "总用电量" }
        ]
      }
    ]
  };
  distributionChart.setOption(option);
};

// 切换日期类型
const handleDateTypeChange = (newType: "day" | "month" | "year") => {
  if (newType === "custom") {
    isCustomDate.value = true;
  } else {
    isCustomDate.value = false;
    dateType.value = newType;
    fetchEnergyData();
  }
};

// 监听自定义日期变化
watch(customDateRange, newRange => {
  if (newRange && newRange.length === 2) {
    fetchEnergyData();
  }
});

// 导出为Excel
const exportToExcel = () => {
  // 动态创建表头
  const headers = "日期,发电量 (kWh),用电量 (kWh),差值 (kWh),自给率 (%)\\n";
  // 格式化数据为CSV
  const csvContent =
    "data:text/csv;charset=utf-8," +
    headers +
    energyData.tableData
      .map(
        e =>
          `${e.date},${e.production},${e.consumption},${e.difference},${e.selfSufficiency}`
      )
      .join("\\n");

  // 创建并下载CSV文件
  const encodedUri = encodeURI(csvContent);
  const link = document.createElement("a");
  link.setAttribute("href", encodedUri);
  link.setAttribute("download", "能耗对比数据.csv");
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
};

// 图表自适应
const resizeCharts = () => {
  if (mainChart) {
    mainChart.resize();
  }
  if (trendChart) {
    trendChart.resize();
  }
  if (distributionChart) {
    distributionChart.resize();
  }
};

// 初始化和销毁
onMounted(() => {
  fetchEnergyData();
  window.addEventListener("resize", resizeCharts);
});

onUnmounted(() => {
  window.removeEventListener("resize", resizeCharts);
  if (mainChart) {
    mainChart.dispose();
  }
  if (trendChart) {
    trendChart.dispose();
  }
  if (distributionChart) {
    distributionChart.dispose();
  }
});

defineExpose({
  refreshData: fetchEnergyData,
  handleDateTypeChange,
  needDateSelector
});
</script>

<template>
  <DetailLayout>
    <DashboardHeader title="能耗对比分析" subtitle="当期与历史同期用能对比分析">
      <template #stats>
        <div class="stat-box">
          <div class="stat-icon increase">
            <i class="el-icon-top" />
          </div>
          <div class="stat-content">
            <div class="stat-label">环比增长率</div>
            <div class="stat-value">+12.5%</div>
          </div>
        </div>
        <div class="stat-box">
          <div class="stat-icon decrease">
            <i class="el-icon-bottom" />
          </div>
          <div class="stat-content">
            <div class="stat-label">节能率</div>
            <div class="stat-value">-8.3%</div>
          </div>
        </div>
        <div class="stat-box">
          <div class="stat-icon normal">
            <i class="el-icon-data-analysis" />
          </div>
          <div class="stat-content">
            <div class="stat-label">总对比次数</div>
            <div class="stat-value">24</div>
          </div>
        </div>
      </template>

      <template #controls>
        <el-radio-group v-model="dateType" size="small" class="date-selector">
          <el-radio-button label="day">日</el-radio-button>
          <el-radio-button label="month">月</el-radio-button>
          <el-radio-button label="year">年</el-radio-button>
        </el-radio-group>

        <el-radio-group v-model="chartType" size="small" class="chart-selector">
          <el-radio-button label="bar">柱状图</el-radio-button>
          <el-radio-button label="line">折线图</el-radio-button>
        </el-radio-group>
      </template>
    </DashboardHeader>

    <div class="chart-container animated-card">
      <div class="chart-card">
        <div class="chart-header">
          <div class="chart-title-container">
            <span class="header-dot" />
            <h3 class="chart-title">能耗对比趋势</h3>
          </div>
          <div class="data-info">
            <span class="data-tag">单位: kWh</span>
          </div>
        </div>
        <div ref="chartRef" v-loading="loading" class="energy-chart" />
        <div class="corner-decorate corner-lt" />
        <div class="corner-decorate corner-rt" />
        <div class="corner-decorate corner-lb" />
        <div class="corner-decorate corner-rb" />
      </div>
    </div>

    <div class="stats-grid">
      <div class="stats-card animated-card">
        <div class="card-header">
          <div class="icon-container">
            <i class="el-icon-data-line" />
          </div>
          <h3>总能耗对比</h3>
        </div>
        <div class="card-content">
          <div class="compare-row">
            <span class="compare-label">本期能耗总量</span>
            <span class="compare-value"
              >{{ totalEnergy.current }} <small>kWh</small></span
            >
          </div>
          <div class="compare-row">
            <span class="compare-label">上期能耗总量</span>
            <span class="compare-value"
              >{{ totalEnergy.last }} <small>kWh</small></span
            >
          </div>
          <div class="compare-row highlight">
            <span class="compare-label">增长率</span>
            <span
              class="compare-value"
              :class="totalEnergy.rate > 0 ? 'increase' : 'decrease'"
            >
              {{ totalEnergy.rate > 0 ? "+" : "" }}{{ totalEnergy.rate }}%
              <el-icon
                ><component
                  :is="totalEnergy.rate > 0 ? 'el-icon-top' : 'el-icon-bottom'"
              /></el-icon>
            </span>
          </div>
        </div>
        <div class="corner-decorate corner-lt" />
        <div class="corner-decorate corner-rt" />
        <div class="corner-decorate corner-lb" />
        <div class="corner-decorate corner-rb" />
      </div>

      <div class="stats-card animated-card">
        <div class="card-header">
          <div class="icon-container">
            <i class="el-icon-aim" />
          </div>
          <h3>目标达成情况</h3>
        </div>
        <div class="card-content">
          <div class="compare-row">
            <span class="compare-label">目标能耗总量</span>
            <span class="compare-value"
              >{{ targetEnergy.target }} <small>kWh</small></span
            >
          </div>
          <div class="compare-row">
            <span class="compare-label">实际能耗总量</span>
            <span class="compare-value"
              >{{ targetEnergy.actual }} <small>kWh</small></span
            >
          </div>
          <div class="compare-row highlight">
            <span class="compare-label">达成率</span>
            <span
              class="compare-value"
              :class="targetEnergy.rate < 100 ? 'good' : 'bad'"
            >
              {{ targetEnergy.rate }}%
            </span>
          </div>
        </div>
        <div class="corner-decorate corner-lt" />
        <div class="corner-decorate corner-rt" />
        <div class="corner-decorate corner-lb" />
        <div class="corner-decorate corner-rb" />
      </div>

      <div class="stats-card animated-card">
        <div class="card-header">
          <div class="icon-container">
            <i class="el-icon-document" />
          </div>
          <h3>节能效果分析</h3>
        </div>
        <div class="card-content">
          <div class="compare-row">
            <span class="compare-label">节能量</span>
            <span class="compare-value"
              >{{ (energySaving.amount || 0).toFixed(2) }}
              <small>kWh</small></span
            >
          </div>
          <div class="compare-row">
            <span class="compare-label">碳减排量</span>
            <span class="compare-value"
              >{{ (energySaving.carbon || 0).toFixed(2) }}
              <small>kg</small></span
            >
          </div>
          <div class="compare-row highlight">
            <span class="compare-label">效益评估</span>
            <span class="compare-value good">
              {{ energySaving.rating }}
            </span>
          </div>
        </div>
        <div class="corner-decorate corner-lt" />
        <div class="corner-decorate corner-rt" />
        <div class="corner-decorate corner-lb" />
        <div class="corner-decorate corner-rb" />
      </div>
    </div>
  </DetailLayout>
</template>

<style scoped>
.chart-container {
  margin-bottom: 20px;
}

.chart-card {
  background: rgba(6, 24, 44, 0.85);
  border-radius: 4px;
  padding: 20px;
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.2);
  border: 1px solid rgba(0, 201, 255, 0.1);
  backdrop-filter: blur(10px);
  position: relative;
  overflow: hidden;
}

.chart-card::before {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(
    to right,
    rgba(0, 201, 255, 0.05),
    rgba(0, 201, 255, 0.3),
    rgba(0, 201, 255, 0.05)
  );
}

.corner-decorate {
  position: absolute;
  width: 10px;
  height: 10px;
}

.corner-lt {
  top: 0;
  left: 0;
  border-top: 2px solid rgba(0, 201, 255, 0.8);
  border-left: 2px solid rgba(0, 201, 255, 0.8);
}

.corner-rt {
  top: 0;
  right: 0;
  border-top: 2px solid rgba(0, 201, 255, 0.8);
  border-right: 2px solid rgba(0, 201, 255, 0.8);
}

.corner-lb {
  bottom: 0;
  left: 0;
  border-bottom: 2px solid rgba(0, 201, 255, 0.8);
  border-left: 2px solid rgba(0, 201, 255, 0.8);
}

.corner-rb {
  bottom: 0;
  right: 0;
  border-bottom: 2px solid rgba(0, 201, 255, 0.8);
  border-right: 2px solid rgba(0, 201, 255, 0.8);
}

.animated-card {
  transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
}

.animated-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 15px 30px rgba(0, 0, 0, 0.3);
  border-color: rgba(0, 201, 255, 0.3);
}

.animated-card::before {
  content: "";
  position: absolute;
  top: -2px;
  left: -2px;
  right: -2px;
  height: 2px;
  background: linear-gradient(
    90deg,
    transparent,
    rgba(0, 201, 255, 0.5),
    transparent
  );
  transform: translateX(-100%);
  transition: transform 0.6s ease-in-out;
}

.animated-card:hover::before {
  transform: translateX(100%);
}

.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
  position: relative;
  padding-bottom: 10px;
}

.chart-header:after {
  content: "";
  position: absolute;
  bottom: 0;
  left: 0;
  width: 100%;
  height: 1px;
  background: linear-gradient(
    90deg,
    transparent 0%,
    rgba(0, 201, 255, 0.2) 50%,
    transparent 100%
  );
}

.chart-title-container {
  display: flex;
  align-items: center;
}

.header-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: linear-gradient(to right, #00c9ff, #92fe9d);
  margin-right: 8px;
  box-shadow: 0 0 8px rgba(0, 201, 255, 0.5);
}

.chart-title {
  font-size: 18px;
  font-weight: 500;
  color: rgba(255, 255, 255, 0.9);
  margin: 0;
  text-shadow: 0 0 10px rgba(0, 201, 255, 0.3);
}

.energy-chart {
  height: 400px;
  width: 100%;
  transition: all 0.3s ease;
  position: relative;
}

.energy-chart::after {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: radial-gradient(
    circle at top right,
    rgba(0, 201, 255, 0.05),
    transparent 70%
  );
  pointer-events: none;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}

.stats-card {
  background: rgba(6, 24, 44, 0.85);
  border-radius: 4px;
  padding: 20px;
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.2);
  border: 1px solid rgba(0, 201, 255, 0.1);
  position: relative;
  overflow: hidden;
}

.card-header {
  display: flex;
  align-items: center;
  margin-bottom: 15px;
  padding-bottom: 10px;
  border-bottom: 1px solid rgba(0, 201, 255, 0.1);
}

.icon-container {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: rgba(0, 201, 255, 0.1);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 12px;
  color: rgba(0, 201, 255, 0.9);
  font-size: 20px;
  box-shadow: 0 0 15px rgba(0, 201, 255, 0.2);
}

.card-header h3 {
  font-size: 16px;
  color: rgba(255, 255, 255, 0.9);
  margin: 0;
  font-weight: 500;
}

.card-content {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.compare-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 10px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
}

.compare-row:last-child {
  border-bottom: none;
}

.compare-label {
  color: rgba(255, 255, 255, 0.7);
  font-size: 14px;
}

.compare-value {
  font-size: 18px;
  font-weight: 600;
  color: #fff;
  display: flex;
  align-items: center;
  gap: 5px;
}

.compare-value small {
  font-size: 12px;
  opacity: 0.7;
  font-weight: 400;
}

.compare-row.highlight {
  padding-top: 10px;
  position: relative;
}

.compare-row.highlight::after {
  content: "";
  position: absolute;
  bottom: 0;
  left: 0;
  width: 100%;
  height: 1px;
  background: linear-gradient(
    90deg,
    transparent,
    rgba(0, 201, 255, 0.2),
    transparent
  );
}

.increase {
  color: #ff5252 !important;
}

.decrease {
  color: #00c9a7 !important;
}

.normal {
  color: #00c9ff !important;
}

.good {
  color: #00c9a7 !important;
}

.bad {
  color: #ff5252 !important;
}

.data-info {
  display: flex;
  align-items: center;
}

.data-tag {
  background-color: rgba(0, 201, 255, 0.1);
  padding: 4px 10px;
  border-radius: 4px;
  font-size: 12px;
  color: rgba(255, 255, 255, 0.7);
  border: 1px solid rgba(0, 201, 255, 0.3);
}

.date-selector,
.chart-selector {
  margin-left: 10px;
}

.stat-box {
  display: flex;
  align-items: center;
  padding: 10px 15px;
  background: rgba(6, 24, 44, 0.7);
  border: 1px solid rgba(0, 201, 255, 0.1);
  border-radius: 4px;
  min-width: 140px;
}

.stat-icon {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 10px;
  font-size: 18px;
  background: rgba(0, 0, 0, 0.2);
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.6);
  margin-bottom: 4px;
}

.stat-value {
  font-size: 16px;
  font-weight: 600;
  color: #fff;
}

@media (max-width: 1200px) {
  .stats-grid {
    grid-template-columns: 1fr 1fr;
    gap: 15px;
  }
}

@media (max-width: 768px) {
  .stats-grid {
    grid-template-columns: 1fr;
  }

  .energy-chart {
    height: 300px;
  }

  .compare-value {
    font-size: 16px;
  }

  .compare-label {
    font-size: 13px;
  }
}
</style>
