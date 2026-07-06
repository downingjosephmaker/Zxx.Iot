<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, defineExpose } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import DetailLayout from "./DetailLayout.vue";
import api from "@/api/reportForms/monthEnergyReport";
import dayjs from "dayjs";

// 导入图标组件
import OfficeBuilding from "~icons/ep/office-building";
import SolarEnergy from "~icons/ep/sunny";
import Lightning from "~icons/ep/lightning";
import Watermelon from "~icons/ep/watermelon";
import MagicStick from "~icons/ep/magic-stick";
import DataLine from "~icons/ep/data-line";
import PartlyCloudy from "~icons/ep/partly-cloudy";
import InfoFilled from "~icons/ep/info-filled";
import TrendCharts from "~icons/ep/trend-charts";
import PieChart from "~icons/ep/pie-chart";
import Histogram from "~icons/ep/histogram";
import CaretTop from "~icons/ep/caret-top";
import CaretBottom from "~icons/ep/caret-bottom";
import Medal from "~icons/ep/medal";
import Calendar from "~icons/ep/calendar";

// 组件名称
defineOptions({
  name: "MonthEnergyReport"
});

// 路由实例
const route = useRoute();
const router = useRouter();

// 表单引用
const listFormRef = ref();
// 报表容器引用
const reportContainerRef = ref();

// 日期类型默认为月
const dateType = ref<"day" | "month" | "year">("month");

// 加载状态
const loading = ref(false);

// 调试信息
const debugInfo = ref<any>(null);

// Top设备显示类型：'QOQ' 环比，'YOY' 同比
const topDeviceType = ref<"QOQ" | "YOY">("QOQ");

// 表单数据
const listForm = ref({
  date: dayjs().startOf("month").format("YYYY-MM-DD") // 默认使用当月1号
});
// 报表数据
const reportData = ref<any>(null);

// 手动设置一个示例数据 - 用于调试
const setMockData = () => {
  const currentPeriod = dayjs(listForm.value.date).format("YYYY年MM月");
  console.log("设置模拟数据，期间:", currentPeriod);
  const mockData = {
    Peroid: currentPeriod,
    Profile: [
      {
        Target: "总发电量",
        CurV: "55030",
        YOY: "--%",
        QOQ: "--%",
        Unit: "kWh",
        Desc: "光伏系统运行良好"
      },
      {
        Target: "总用电量",
        CurV: "32435",
        YOY: "--%",
        QOQ: "--%",
        Unit: "kWh",
        Desc: "主要增长来自1号配电间堆高车充电桩2"
      },
      {
        Target: "总用水量",
        CurV: "102",
        YOY: "--%",
        QOQ: "--%",
        Unit: "吨",
        Desc: "节水措施初见成效"
      },
      {
        Target: "总用氢量",
        CurV: "29659",
        YOY: "--%",
        QOQ: "--%",
        Unit: "kg",
        Desc: "绿色能源利用增加"
      },
      {
        Target: "电力自给率",
        CurV: "169.7%",
        YOY: "--%",
        QOQ: "--%",
        Unit: "%",
        Desc: "电力自给能力持续提升"
      },
      {
        Target: "碳排放估算",
        CurV: "16713.8",
        YOY: "--%",
        QOQ: "--%",
        Unit: "kgCO₂e",
        Desc: "节能减排效果明显"
      }
    ],
    ElecRemain: {
      Product: "55030",
      Cost: "32435",
      Rate: "169.7%",
      YOY: "--%",
      QOQ: "--%",
      ProductEffect: "96.9%"
    },
    Detail: [
      {
        Target: "总发电量",
        CurV: "55030",
        PreY: "50000",
        SubY: "5030",
        YOY: "10%",
        PreQ: "45000",
        SubQ: "10030",
        QOQ: "22%"
      },
      {
        Target: "总用电量",
        CurV: "32435",
        PreY: "30000",
        SubY: "2435",
        YOY: "8%",
        PreQ: "28000",
        SubQ: "4435",
        QOQ: "16%"
      },
      {
        Target: "总用水量",
        CurV: "102",
        PreY: "90",
        SubY: "12",
        YOY: "13%",
        PreQ: "85",
        SubQ: "17",
        QOQ: "14%"
      },
      {
        Target: "总用氢量",
        CurV: "29659",
        PreY: "28000",
        SubY: "1659",
        YOY: "6%",
        PreQ: "27000",
        SubQ: "2659",
        QOQ: "6%"
      },
      {
        Target: "电力自给率",
        CurV: "169.7%",
        PreY: "150%",
        SubY: "19.7%",
        YOY: "13%",
        PreQ: "140%",
        SubQ: "29.7%",
        QOQ: "14%"
      },
      {
        Target: "碳排放估算",
        CurV: "16713.8",
        PreY: "15000",
        SubY: "1713.8",
        YOY: "11%",
        PreQ: "14500",
        SubQ: "2213.8",
        QOQ: "12%"
      }
    ],
    ElecQOQTop: [
      {
        DevName: "1号配电间补偿柜",
        Rate: "--%"
      },
      {
        DevName: "1号配电间配电室ALE箱",
        Rate: "--%"
      },
      {
        DevName: "1号配电间配电室AL箱",
        Rate: "--%"
      },
      {
        DevName: "1号配电间备用50",
        Rate: "--%"
      },
      {
        DevName: "1号配电间1#-2#高杆灯",
        Rate: "--%"
      }
    ],
    ElecYOYTop: [
      {
        DevName: "综合楼ZAL（中控室配电箱）",
        Rate: "--%"
      },
      {
        DevName: "综合楼KTZ2（空调总电）",
        Rate: "--%"
      },
      {
        DevName: "综合楼KT14（4楼内机空调）",
        Rate: "--%"
      },
      {
        DevName: "综合楼KT13（3楼内机空调）",
        Rate: "--%"
      },
      {
        DevName: "综合楼KT12(2楼内机空调）",
        Rate: "--%"
      }
    ],
    CO2Cost: [
      {
        Energy: "32435",
        Fact: "0.515",
        CO2: "16713.8"
      }
    ]
  };
  reportData.value = mockData;
  console.log("已设置模拟数据:", mockData);
};

// 获取报表数据
const fetchReportData = async () => {
  // 确保使用当月1号作为参数
  const requestDate = dayjs(listForm.value.date)
    .startOf("month")
    .format("YYYY-MM-DD");
  console.log(
    "开始获取报表数据，月份:",
    dayjs(listForm.value.date).format("YYYY年MM月"),
    "参数日期:",
    requestDate
  );
  try {
    loading.value = true;
    // 修复API响应类型错误
    const response = await api.GetMonthEnergyReport(requestDate);
    const data = response; // 直接使用返回值
    console.log("API返回原始数据:", data);

    // 保存调试信息
    debugInfo.value = data;

    if (data && data.Result) {
      try {
        // 解析JSON字符串为对象
        const parsedData = JSON.parse(data.Result);
        console.log("解析后的报表数据:", parsedData);

        // 确认数据结构并赋值
        reportData.value = parsedData;

        // 检查关键数据是否存在
        console.log("期间:", reportData.value.Peroid);
        console.log("Profile数据:", reportData.value.Profile);
        console.log("电力自给率:", reportData.value.ElecRemain?.Rate);
      } catch (parseError) {
        console.error("解析报表数据失败:", parseError);
        reportData.value = null;

        // 尝试设置模拟数据（仅用于调试）
        if (process.env.NODE_ENV !== "production") {
          console.warn("将使用模拟数据进行调试");
          setMockData();
        }
      }
    } else {
      console.warn("API返回的数据中没有Result字段或Result为空");
      reportData.value = null;

      // 如果API没有返回数据，使用模拟数据（仅用于调试）
      if (process.env.NODE_ENV !== "production") {
        console.warn("将使用模拟数据进行调试");
        setMockData();
      }
    }
  } catch (error) {
    console.error("获取月度能耗报表数据失败:", error);
    reportData.value = null;
  } finally {
    loading.value = false;
    console.log("数据加载完成，reportData:", reportData.value);
  }
};

// 重新尝试获取数据
const retryFetchData = () => {
  console.log("重新尝试获取数据");
  fetchReportData();
};

// 重置表单
const resetForm = formRef => {
  if (!formRef) return;
  formRef.resetFields();
  listForm.value.date = dayjs().startOf("month").format("YYYY-MM-DD");
  fetchReportData();
};

// 导出Excel
const exportExcel = () => {
  // 实现Excel导出功能
  console.log("导出Excel");
};

// 导出PDF
const exportPDF = () => {
  // 实现PDF导出功能
  console.log("导出PDF");
};

// 打开全屏大屏页面
const handlePrevMonth = () => {
  router.push({
    path: "/MonthEnergyReportFullscreen"
    // 移除日期参数，让全屏页面也使用当前月份
  });
};

// 计算电力自给率进度
const selfSufficiencyRate = computed(() => {
  if (!reportData.value?.ElecRemain?.Rate) return 0;
  const rate = reportData.value.ElecRemain.Rate.replace("%", "");
  return parseFloat(rate) > 100 ? 100 : parseFloat(rate);
});

// 获取当前显示的Top设备列表
const currentTopDevices = computed(() => {
  if (!reportData.value) return [];

  if (topDeviceType.value === "QOQ") {
    return reportData.value.ElecQOQTop || [];
  } else {
    return reportData.value.ElecYOYTop || [];
  }
});

// 切换Top设备显示类型
const switchTopDeviceType = (type: "QOQ" | "YOY") => {
  topDeviceType.value = type;
};

// 获取KPI图标
const getKpiIcon = (index: number) => {
  const icons = [
    SolarEnergy, // 总发电量
    Lightning, // 总用电量
    Watermelon, // 总用水量
    MagicStick, // 总用氢量
    DataLine, // 电力自给率
    PartlyCloudy // 碳排放估算
  ];
  return icons[index] || DataLine;
};

// 获取标签类型
const getTagType = (value: string) => {
  if (!value || value === "--%") return "info";
  return !value.includes("-") ? "success" : "danger";
};

// 生成能源使用总结
const energySummary = computed(() => {
  if (!reportData.value) return [];

  // 检查数据是否足够生成有意义的总结
  const data = reportData.value;
  const hasYoyData = data.Detail?.some(item => item.YOY && item.YOY !== "--%");
  const hasQoqData = data.Detail?.some(item => item.QOQ && item.QOQ !== "--%");

  // 如果没有同比或环比数据，返回空数组，前端会显示"数据暂时不够"
  if (!hasYoyData && !hasQoqData) {
    return [];
  }

  const summaries = [];

  // 电力自给率总结
  if (data.ElecRemain && data.ElecRemain.Rate) {
    const rate = parseFloat(data.ElecRemain.Rate.replace("%", ""));
    if (rate > 100) {
      summaries.push({
        icon: "solar-energy",
        type: "positive",
        text: `电力自给率达到${data.ElecRemain.Rate}，发电量超过用电需求，能源自给自足情况良好。`
      });
    } else if (rate >= 80) {
      summaries.push({
        icon: "solar-energy",
        type: "positive",
        text: `电力自给率达到${data.ElecRemain.Rate}，接近能源自给自足。`
      });
    } else {
      summaries.push({
        icon: "solar-energy",
        type: "negative",
        text: `电力自给率为${data.ElecRemain.Rate}，还需进一步提高发电能力。`
      });
    }
  }

  // 总发电量总结
  if (data.Profile && data.Profile[0]) {
    const elecGen = data.Profile[0];
    if (elecGen.YOY && elecGen.YOY !== "--%") {
      const isGrowth = !elecGen.YOY.includes("-");
      summaries.push({
        icon: "data-line",
        type: isGrowth ? "positive" : "negative",
        text: `总发电量${isGrowth ? "增长" : "下降"}${elecGen.YOY.replace("-", "")}，${elecGen.Desc || "发电表现" + (isGrowth ? "良好" : "有所下滑")}`
      });
    }
  }

  // 总用电量总结
  if (data.Profile && data.Profile[1]) {
    const elecUse = data.Profile[1];
    if (elecUse.YOY && elecUse.YOY !== "--%") {
      const isGrowth = !elecUse.YOY.includes("-");
      summaries.push({
        icon: "lightning",
        type: !isGrowth ? "positive" : "warning", // 用电量减少是正面的
        text: `总用电量${isGrowth ? "增长" : "下降"}${elecUse.YOY.replace("-", "")}，${elecUse.Desc || "能源利用" + (isGrowth ? "需要关注" : "情况改善")}`
      });
    }
  }

  // 用水情况总结
  if (data.Profile && data.Profile[2]) {
    const waterUse = data.Profile[2];
    if (waterUse.YOY && waterUse.YOY !== "--%") {
      const isGrowth = !waterUse.YOY.includes("-");
      summaries.push({
        icon: "watermelon",
        type: !isGrowth ? "positive" : "warning", // 用水量减少是正面的
        text: `总用水量${isGrowth ? "增长" : "下降"}${waterUse.YOY.replace("-", "")}，${waterUse.Desc || "水资源利用" + (isGrowth ? "需要优化" : "表现良好")}`
      });
    }
  }

  // 碳排放总结
  if (data.Profile && data.Profile[5]) {
    const carbon = data.Profile[5];
    if (carbon.YOY && carbon.YOY !== "--%") {
      const isGrowth = !carbon.YOY.includes("-");
      summaries.push({
        icon: "partly-cloudy",
        type: !isGrowth ? "positive" : "negative",
        text: `碳排放量${isGrowth ? "增长" : "降低"}${carbon.YOY.replace("-", "")}，${carbon.Desc || "碳减排工作" + (isGrowth ? "面临挑战" : "成效显著")}`
      });
    }
  }

  // 如果有高耗能设备信息
  if (data.ElecQOQTop && data.ElecQOQTop.length > 0) {
    summaries.push({
      icon: "histogram",
      type: "warning",
      text: `环比增长最高的设备为"${data.ElecQOQTop[0].DevName}"，建议重点监控能耗变化。`
    });
  }

  return summaries;
});

// 生成节能优化建议
const energySuggestions = computed(() => {
  if (!reportData.value) return [];

  // 检查数据是否足够生成有意义的建议
  const data = reportData.value;
  const hasYoyData = data.Detail?.some(item => item.YOY && item.YOY !== "--%");
  const hasQoqData = data.Detail?.some(item => item.QOQ && item.QOQ !== "--%");

  // 如果没有充分的数据，返回一个特殊提示
  if (!hasYoyData && !hasQoqData) {
    return [
      {
        priority: "info",
        priorityText: "提示",
        title: "数据采集中",
        description:
          "当前暂无足够的历史数据生成有意义的能源优化建议，建议持续收集至少1-2个月的数据后再查看。"
      }
    ];
  }

  const suggestions = [];

  // 基于电力自给率的建议
  if (data.ElecRemain && data.ElecRemain.Rate) {
    const rate = parseFloat(data.ElecRemain.Rate.replace("%", ""));
    if (rate < 80) {
      suggestions.push({
        priority: "warning",
        priorityText: "重要",
        title: "提高光伏发电效能",
        description:
          "当前电力自给率偏低，建议扩大光伏设备覆盖面积或优化现有光伏设备的维护管理。"
      });
    } else if (rate > 100) {
      suggestions.push({
        priority: "success",
        priorityText: "建议",
        title: "多余电力利用优化",
        description:
          "当前发电量超过用电量，建议考虑电力存储系统或与电网互联，最大化利用绿色能源。"
      });
    }
  }

  // 基于用电设备的建议
  if (data.ElecQOQTop && data.ElecQOQTop.length > 0) {
    // 高耗能设备优化建议
    const topDevice = data.ElecQOQTop[0];
    if (topDevice.Rate && topDevice.Rate !== "--%") {
      const isGrowth = !topDevice.Rate.includes("-");
      if (isGrowth) {
        suggestions.push({
          priority: "danger",
          priorityText: "紧急",
          title: `优化"${topDevice.DevName}"能耗`,
          description: `该设备能耗环比增长${topDevice.Rate}，建议检查设备运行状态，实施能效改进措施。`
        });
      }
    }
  }

  // 用水优化建议
  if (data.Profile && data.Profile[2]) {
    const waterUse = data.Profile[2];
    if (waterUse.YOY && waterUse.YOY !== "--%") {
      const isGrowth = !waterUse.YOY.includes("-");
      if (isGrowth) {
        suggestions.push({
          priority: "warning",
          priorityText: "重要",
          title: "水资源利用优化",
          description:
            "用水量同比增长，建议实施节水措施，检查用水系统是否存在泄漏，优化水资源回收利用。"
        });
      }
    }
  }

  // 碳排放优化建议
  if (data.Profile && data.Profile[5]) {
    const carbon = data.Profile[5];
    if (carbon.YOY && carbon.YOY !== "--%") {
      const isGrowth = !carbon.YOY.includes("-");
      if (isGrowth) {
        suggestions.push({
          priority: "danger",
          priorityText: "紧急",
          title: "碳排放降低策略",
          description:
            "碳排放量同比增加，建议优化能源结构，增加清洁能源使用比例，实施碳中和路径规划。"
        });
      } else {
        suggestions.push({
          priority: "info",
          priorityText: "参考",
          title: "碳减排经验推广",
          description:
            "当前碳减排成效显著，建议总结减排经验，并推广至其他区域或设施。"
        });
      }
    }
  }

  // 能效优化通用建议
  suggestions.push({
    priority: "info",
    priorityText: "参考",
    title: "能源监测系统优化",
    description:
      "建议进一步完善能源监测系统，实现全流程能耗数据分析，为节能减排决策提供更精准支持。"
  });

  // 季节性建议
  const month = dayjs(listForm.value.date).month() + 1;
  if (month >= 6 && month <= 9) {
    suggestions.push({
      priority: "warning",
      priorityText: "季节性",
      title: "夏季节能策略",
      description:
        "夏季用电高峰期，建议优化空调使用策略，合理设置温度，避免能源浪费。"
    });
  } else if (month >= 11 || month <= 2) {
    suggestions.push({
      priority: "warning",
      priorityText: "季节性",
      title: "冬季节能策略",
      description: "冬季供暖期间，建议优化供暖系统运行参数，确保能源高效利用。"
    });
  }

  return suggestions;
});

// 获取变化类样式
const getChangeClass = (value: string) => {
  if (!value || value === "--%") return "change-neutral";
  return !value.includes("-") ? "change-positive" : "change-negative";
};

// 格式化百分比
const formatPercentage = (value: string) => {
  if (!value || value === "--%") return "--";
  return value;
};

// 处理日期类型变更
const handleDateTypeChange = type => {
  dateType.value = type;
  // 根据类型调整日期
  if (type === "month") {
    listForm.value.date = dayjs().startOf("month").format("YYYY-MM-DD");
    fetchReportData();
  }
};

// 刷新数据
const refreshData = () => {
  fetchReportData();
};

// 生命周期钩子
onMounted(() => {
  // 移除URL参数影响，始终使用当前月份
  // 确保使用当前月份的1号
  listForm.value.date = dayjs().startOf("month").format("YYYY-MM-DD");
  console.log(
    "使用当前月份:",
    dayjs().format("YYYY年MM月"),
    "日期:",
    listForm.value.date
  );

  // 设置获取数据的超时保护
  setTimeout(() => {
    if (!reportData.value) {
      console.warn("数据加载超时，尝试设置模拟数据");
      setMockData();
    }
  }, 5000);

  fetchReportData();
});

// 暴露给父组件的方法和属性
defineExpose({
  refreshData,
  handleDateTypeChange,
  handlePrevMonth,
  needDateSelector: true, // 是否需要日期选择器
  showPrevMonthBtn: true, // 是否显示上月分析报告按钮
  currentDate: computed(() => listForm.value.date) // 当前日期
});

// 不再需要限制日期选择为每月1号，因为使用了月份选择器

// 优化同比环比显示函数
const formatRateWithArrow = (value: string) => {
  if (!value || value === "--%") return "--";

  const isPositive = !value.includes("-");
  const icon = isPositive ? "↑" : "↓";
  const colorClass = isPositive ? "rate-up" : "rate-down";

  return `<span class="${colorClass}">${icon} ${value}</span>`;
};

// 格式化日期为月份格式
const formatMonthDate = (date: string) => {
  if (!date) return "";
  return dayjs(date).format("YYYY年MM月");
};

// 动态生成指标描述文本
const generateDescription = (item: any, index: number) => {
  if (!item) return "";

  // 如果已有描述且不需要动态生成，直接使用原描述
  if (item.Desc && !item.dynamicDesc) return item.Desc;

  const value = item.CurV;
  const target = item.Target;
  const yoy = item.YOY !== "--%" ? item.YOY : null;
  const qoq = item.QOQ !== "--%" ? item.QOQ : null;

  // 根据指标类型生成不同描述
  switch (index) {
    case 0: // 总发电量
      return `${formatMonthDate(listForm.value.date)}发电量${value}${item.Unit}${yoy ? "，同比" + (yoy.includes("-") ? "下降" : "增长") + yoy.replace("-", "") : ""}`;

    case 1: // 总用电量
      return `${formatMonthDate(listForm.value.date)}用电量${value}${item.Unit}${yoy ? "，同比" + (yoy.includes("-") ? "下降" : "增长") + yoy.replace("-", "") : ""}`;

    case 2: // 总用水量
      return `${formatMonthDate(listForm.value.date)}用水量${value}${item.Unit}${yoy ? "，同比" + (yoy.includes("-") ? "下降" : "增长") + yoy.replace("-", "") : ""}`;

    case 3: // 总用氢量
      return `${formatMonthDate(listForm.value.date)}用氢量${value}${item.Unit}${yoy ? "，同比" + (yoy.includes("-") ? "下降" : "增长") + yoy.replace("-", "") : ""}`;

    case 4: // 电力自给率
      return `总发电量÷总用电量×100%=${value}`;

    case 5: // 碳排放估算
      return `${formatMonthDate(listForm.value.date)}碳排放量${value}${item.Unit}${yoy ? "，同比" + (yoy.includes("-") ? "下降" : "增长") + yoy.replace("-", "") : ""}`;

    default:
      return item.Desc || "";
  }
};

// 处理日期变更时，自动设置为当月1号
const handleDateChange = val => {
  if (val) {
    // 确保日期是当月1号
    listForm.value.date = dayjs(val).startOf("month").format("YYYY-MM-DD");
    console.log("选择的月份更新为:", listForm.value.date);
    fetchReportData();
  }
};
</script>

<template>
  <DetailLayout>
    <div
      ref="reportContainerRef"
      v-loading="loading"
      element-loading-text="正在加载报表数据..."
      element-loading-background="rgba(0, 0, 0, 0.7)"
      class="report-content month-energy-report-container dark-theme"
    >
      <!-- 月份选择器 -->
      <el-card shadow="hover" class="month-picker-card">
        <div class="picker-container">
          <div class="picker-label">
            <el-icon><calendar /></el-icon>
            <span>选择月份：</span>
          </div>
          <el-date-picker
            v-model="listForm.date"
            type="month"
            format="YYYY年MM月"
            value-format="YYYY-MM-DD"
            placeholder="选择月份"
            :disabled="loading"
            class="month-picker"
            @change="handleDateChange"
          />
          <el-button
            type="primary"
            :icon="useRenderIcon('ep/refresh')"
            :loading="loading"
            @click="refreshData"
          >
            刷新数据
          </el-button>
        </div>
      </el-card>

      <transition name="fade">
        <div v-if="reportData" class="report-body">
          <!-- 数据摘要卡片 -->
          <el-row :gutter="16" class="data-summary">
            <el-col :span="12">
              <!-- 数据摘要卡片 - 简化版 -->
              <div class="enhanced-summary-section">
                <div class="stats-grid">
                  <div class="stat-card primary">
                    <div class="stat-icon-wrapper">
                      <el-icon class="stat-icon"><solar-energy /></el-icon>
                    </div>
                    <div class="stat-content">
                      <div class="stat-label">总发电量</div>
                      <div class="stat-value">
                        {{ reportData.Profile[0]?.CurV || "0" }}
                      </div>
                      <div class="stat-unit">
                        {{ reportData.Profile[0]?.Unit || "kWh" }}
                      </div>
                    </div>
                    <div class="stat-decoration" />
                  </div>

                  <div class="stat-card secondary">
                    <div class="stat-icon-wrapper">
                      <el-icon class="stat-icon"><lightning /></el-icon>
                    </div>
                    <div class="stat-content">
                      <div class="stat-label">总用电量</div>
                      <div class="stat-value">
                        {{ reportData.Profile[1]?.CurV || "0" }}
                      </div>
                      <div class="stat-unit">
                        {{ reportData.Profile[1]?.Unit || "kWh" }}
                      </div>
                    </div>
                    <div class="stat-decoration" />
                  </div>

                  <div class="stat-card tertiary">
                    <div class="stat-icon-wrapper">
                      <el-icon class="stat-icon"><watermelon /></el-icon>
                    </div>
                    <div class="stat-content">
                      <div class="stat-label">总用水量</div>
                      <div class="stat-value">
                        {{ reportData.Profile[2]?.CurV || "0" }}
                      </div>
                      <div class="stat-unit">
                        {{ reportData.Profile[2]?.Unit || "吨" }}
                      </div>
                    </div>
                    <div class="stat-decoration" />
                  </div>

                  <div class="stat-card quaternary">
                    <div class="stat-icon-wrapper">
                      <el-icon class="stat-icon"><magic-stick /></el-icon>
                    </div>
                    <div class="stat-content">
                      <div class="stat-label">总用氢量</div>
                      <div class="stat-value">
                        {{ reportData.Profile[3]?.CurV || "0" }}
                      </div>
                      <div class="stat-unit">
                        {{ reportData.Profile[3]?.Unit || "kg" }}
                      </div>
                    </div>
                    <div class="stat-decoration" />
                  </div>
                </div>
              </div>
            </el-col>

            <el-col :span="12">
              <el-card shadow="hover" class="summary-kpi-card">
                <div class="self-sufficiency-header">
                  <div class="header-left">
                    <el-icon class="icon"><data-line /></el-icon>
                    <span>电力自给率</span>
                  </div>
                  <div class="header-right">
                    <el-tag type="success" effect="dark">
                      {{ reportData.ElecRemain?.Rate || "0%" }}
                    </el-tag>
                  </div>
                </div>
                <el-progress
                  :percentage="selfSufficiencyRate"
                  :stroke-width="12"
                  :format="() => reportData.ElecRemain?.Rate || '0%'"
                  status="success"
                  class="sufficiency-progress"
                />

                <div class="energy-formula">
                  <div class="formula-container">
                    <div class="formula-part">
                      <div class="formula-label">总发电量</div>
                      <div class="formula-value positive">
                        {{ reportData.ElecRemain?.Product || "0 kWh" }}
                      </div>
                    </div>
                    <div class="formula-operator">÷</div>
                    <div class="formula-part">
                      <div class="formula-label">总用电量</div>
                      <div class="formula-value negative">
                        {{ reportData.ElecRemain?.Cost || "0 kWh" }}
                      </div>
                    </div>
                    <div class="formula-operator">×</div>
                    <div class="formula-part">
                      <div class="formula-label">百分比</div>
                      <div class="formula-value multiplier">100%</div>
                    </div>
                    <div class="formula-operator">=</div>
                    <div class="formula-part">
                      <div class="formula-label">电力自给率</div>
                      <div class="formula-value result">
                        {{ reportData.ElecRemain?.Rate || "0%" }}
                      </div>
                    </div>
                  </div>
                </div>
              </el-card>
            </el-col>
          </el-row>

          <!-- 核心指标概览 -->
          <div class="dashboard-section-header">
            <div class="section-line" />
            <div class="section-title highlight-section">
              <el-icon class="section-icon highlight-icon"
                ><data-line
              /></el-icon>
              <span>核心指标概览</span>
            </div>
          </div>

          <!-- 核心指标概览 - 使用卡片组布局 -->
          <div class="kpi-cards">
            <el-row :gutter="12">
              <el-col
                v-for="(item, index) in reportData.Profile"
                :key="index"
                :xs="24"
                :sm="12"
                :md="8"
                :lg="8"
                :xl="8"
              >
                <el-card
                  shadow="hover"
                  class="kpi-card"
                  :class="`kpi-theme-${index}`"
                >
                  <div class="kpi-card-content">
                    <div class="kpi-left-section">
                      <div class="kpi-icon-area">
                        <el-icon :class="`icon-${index}`">
                          <component :is="getKpiIcon(index)" />
                        </el-icon>
                      </div>
                      <div class="kpi-data-area">
                        <div class="kpi-title">
                          <span>{{ item.Target }}</span>
                        </div>
                        <div class="kpi-value">{{ item.CurV }}</div>
                        <div class="kpi-meta">
                          <div class="kpi-unit">{{ item.Unit }}</div>
                          <div class="kpi-changes">
                            <el-tooltip content="同比变化" placement="top">
                              <el-tag
                                size="small"
                                effect="plain"
                                :type="getTagType(item.YOY)"
                              >
                                <el-icon><trend-charts /></el-icon>
                                <span>{{ formatPercentage(item.YOY) }}</span>
                              </el-tag>
                            </el-tooltip>
                            <el-tooltip content="环比变化" placement="top">
                              <el-tag
                                size="small"
                                effect="plain"
                                :type="getTagType(item.QOQ)"
                              >
                                <el-icon><data-line /></el-icon>
                                <span>{{ formatPercentage(item.QOQ) }}</span>
                              </el-tag>
                            </el-tooltip>
                          </div>
                        </div>
                      </div>
                    </div>
                    <div class="kpi-right-section">
                      <div class="kpi-desc">
                        <div class="desc-icon">
                          <el-icon><info-filled /></el-icon>
                        </div>
                        <div class="desc-text">
                          {{ generateDescription(item, index) }}
                        </div>
                      </div>
                    </div>
                  </div>
                </el-card>
              </el-col>
            </el-row>
          </div>

          <!-- 详细数据分析 -->
          <div class="dashboard-section-header">
            <div class="section-line" />
            <div class="section-title">
              <el-icon class="section-icon"><pie-chart /></el-icon>
              <span>详细数据分析</span>
            </div>
          </div>

          <!-- 详细数据分析表格 -->
          <div class="table-header">
            <div class="table-title">
              <el-icon><histogram /></el-icon>
              <span>能源使用同环比数据</span>
            </div>
          </div>

          <!-- 修改表格表头样式 -->
          <el-table
            :data="reportData.Detail || []"
            border
            stripe
            size="small"
            highlight-current-row
            style="width: 100%"
            class="compact-table"
            :header-cell-style="{
              background:
                'linear-gradient(to bottom, rgba(30, 85, 140, 0.8), rgba(25, 75, 130, 0.7))',
              color: '#e0f7ff',
              fontWeight: 'bold',
              borderBottom: '2px solid rgba(64, 158, 255, 0.4)',
              fontSize: '14px'
            }"
          >
            <el-table-column prop="Target" label="指标" min-width="120">
              <template #default="{ row }">
                <div class="indicator-cell">
                  <el-icon v-if="row.Target.includes('发电量')">
                    <solar-energy />
                  </el-icon>
                  <el-icon v-else-if="row.Target.includes('用电量')">
                    <lightning />
                  </el-icon>
                  <el-icon v-else-if="row.Target.includes('用水量')">
                    <watermelon />
                  </el-icon>
                  <el-icon v-else-if="row.Target.includes('用氢量')">
                    <magic-stick />
                  </el-icon>
                  <span>{{ row.Target }}</span>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="CurV" label="本期值" min-width="100">
              <template #default="{ row }">
                <span class="emphasis">{{ row.CurV }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="PreY" label="去年同期值" min-width="100" />
            <el-table-column prop="SubY" label="同比增减值" min-width="100" />
            <el-table-column prop="YOY" label="同比增减率" min-width="100">
              <template #default="{ row }">
                <div class="change-cell" :class="getChangeClass(row.YOY)">
                  <div v-html="formatRateWithArrow(row.YOY)" />
                  <div v-if="row.YOY !== '--%'" class="change-detail">
                    <small
                      >{{ formatMonthDate(listForm.date) }} vs 去年同期</small
                    >
                  </div>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="PreQ" label="上期值" min-width="100" />
            <el-table-column prop="SubQ" label="环比增减值" min-width="100" />
            <el-table-column prop="QOQ" label="环比增减率" min-width="100">
              <template #default="{ row }">
                <div class="change-cell" :class="getChangeClass(row.QOQ)">
                  <div v-html="formatRateWithArrow(row.QOQ)" />
                  <div v-if="row.QOQ !== '--%'" class="change-detail">
                    <small>{{ formatMonthDate(listForm.date) }} vs 上月</small>
                  </div>
                </div>
              </template>
            </el-table-column>
          </el-table>

          <!-- 用电Top5设备 -->
          <div class="dashboard-section-header">
            <div class="section-line" />
            <div class="section-title">
              <el-icon class="section-icon"><medal /></el-icon>
              <span>用电量Top5设备</span>
              <div class="title-controls">
                <el-radio-group
                  v-model="topDeviceType"
                  size="small"
                  @change="switchTopDeviceType"
                >
                  <el-radio-button label="QOQ">环比</el-radio-button>
                  <el-radio-button label="YOY">同比</el-radio-button>
                </el-radio-group>
              </div>
            </div>
          </div>

          <div
            class="top-devices-grid"
            :data-count="Math.min(currentTopDevices.length, 5)"
          >
            <div
              v-for="(device, index) in currentTopDevices.slice(0, 5)"
              :key="index"
              class="device-item"
            >
              <el-card shadow="hover" class="device-card">
                <div class="device-rank">
                  <div class="rank-badge" :class="`rank-${index + 1}`">
                    {{ index + 1 }}
                  </div>
                </div>
                <div class="device-info">
                  <div class="device-name" :title="device.DevName">
                    {{ device.DevName }}
                  </div>
                  <div class="device-change">
                    <span>{{
                      topDeviceType === "QOQ" ? "环比:" : "同比:"
                    }}</span>
                    <div
                      class="change-value"
                      v-html="formatRateWithArrow(device.Rate)"
                    />
                  </div>
                </div>
              </el-card>
            </div>
          </div>
          <!-- 总结与建议 -->
          <div class="dashboard-section-header">
            <div class="section-line" />
            <div class="section-title highlight-section">
              <el-icon class="section-icon highlight-icon"
                ><info-filled
              /></el-icon>
              <span>总结与建议</span>
            </div>
          </div>

          <el-row :gutter="16" class="summary-suggestions">
            <el-col :span="12">
              <el-card shadow="hover" class="summary-card">
                <template #header>
                  <div class="card-header">
                    <el-icon><trend-charts /></el-icon>
                    <span>能源使用总结</span>
                  </div>
                </template>
                <div class="summary-content">
                  <template v-if="energySummary.length > 0">
                    <ul class="summary-list">
                      <li v-for="(item, index) in energySummary" :key="index">
                        <el-icon :class="item.type"
                          ><component :is="item.icon"
                        /></el-icon>
                        <span>{{ item.text }}</span>
                      </li>
                    </ul>
                  </template>
                  <el-empty
                    v-else
                    description="暂无足够的历史数据生成总结，建议持续收集数据"
                  />
                </div>
              </el-card>
            </el-col>
            <el-col :span="12">
              <el-card shadow="hover" class="suggestion-card">
                <template #header>
                  <div class="card-header">
                    <el-icon><magic-stick /></el-icon>
                    <span>节能优化建议</span>
                  </div>
                </template>
                <div class="suggestion-content">
                  <template v-if="energySuggestions.length > 0">
                    <ul class="suggestion-list">
                      <li
                        v-for="(item, index) in energySuggestions"
                        :key="index"
                      >
                        <div class="suggestion-item">
                          <div class="suggestion-title">
                            <el-tag
                              :type="item.priority"
                              size="small"
                              effect="dark"
                            >
                              {{ item.priorityText }}
                            </el-tag>
                            <span>{{ item.title }}</span>
                          </div>
                          <div class="suggestion-desc">
                            {{ item.description }}
                          </div>
                        </div>
                      </li>
                    </ul>
                  </template>
                  <el-empty
                    v-else
                    description="暂无足够的历史数据生成建议，需要更多数据进行分析"
                  />
                </div>
              </el-card>
            </el-col>
          </el-row>

          <!-- 碳排放估算 -->
          <div class="dashboard-section-header">
            <div class="section-line" />
            <div class="section-title">
              <el-icon class="section-icon"><partly-cloudy /></el-icon>
              <span>碳排放估算</span>
            </div>
          </div>

          <!-- 碳排放估算表格也应用相同的表头样式 -->
          <el-table
            :data="reportData.CO2Cost || []"
            border
            stripe
            size="small"
            highlight-current-row
            style="width: 100%"
            class="compact-table"
            :header-cell-style="{
              background:
                'linear-gradient(to bottom, rgba(30, 85, 140, 0.8), rgba(25, 75, 130, 0.7))',
              color: '#e0f7ff',
              fontWeight: 'bold',
              borderBottom: '2px solid rgba(64, 158, 255, 0.4)',
              fontSize: '14px'
            }"
          >
            <el-table-column label="用电量(kWh)" min-width="120">
              <template #default="{ row }">
                <span>{{ row.Energy }}</span>
              </template>
            </el-table-column>
            <el-table-column label="排放系数(kg/kWh)" min-width="120">
              <template #default="{ row }">
                <span>{{ row.Fact }}</span>
              </template>
            </el-table-column>
            <el-table-column label="碳排放量(kg)" min-width="120">
              <template #default="{ row }">
                <span class="emphasis">{{ row.CO2 }}</span>
              </template>
            </el-table-column>
          </el-table>

          <!-- 更多报表内容可以根据需要添加 -->
          <!-- 这里可以添加图表、表格等组件 -->
        </div>
        <div v-else class="no-data">
          <el-empty description="暂无数据" />
        </div>
      </transition>
    </div>
  </DetailLayout>
</template>

<style lang="scss" scoped>
.report-content {
  padding: 20px;
  height: 100%;
  overflow: auto;
  background-color: rgba(13, 41, 71, 0.1);
  color: #e0f2ff;
}

.month-picker-card {
  margin-bottom: 20px;
  background: linear-gradient(
    135deg,
    rgba(25, 118, 210, 0.2),
    rgba(13, 71, 161, 0.1)
  );
  border: 1px solid rgba(64, 158, 255, 0.2);
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  transition: all 0.3s ease;

  &:hover {
    box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
    border-color: rgba(64, 158, 255, 0.4);
  }

  :deep(.el-card__body) {
    padding: 15px;
  }
}

.picker-container {
  display: flex;
  align-items: center;
  gap: 15px;
}

.picker-label {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #e0f2ff;
  font-weight: 500;

  .el-icon {
    font-size: 18px;
    color: #64b5f6;
  }
}

.month-picker {
  width: 180px;

  :deep(.el-input__wrapper) {
    background-color: rgba(15, 40, 70, 0.4);
    box-shadow: 0 0 0 1px rgba(64, 158, 255, 0.25) inset;

    input {
      color: #e0f2ff;

      &::placeholder {
        color: rgba(144, 202, 249, 0.5);
      }
    }

    &:hover {
      box-shadow: 0 0 0 1px rgba(64, 158, 255, 0.5) inset;
    }

    &.is-focus {
      box-shadow: 0 0 0 1px #1976d2 inset;
      background-color: rgba(20, 45, 80, 0.4);
    }
  }
}

.report-body {
  animation: fadeIn 0.5s ease-in-out;
}

.data-summary {
  margin-bottom: 20px;

  .el-col {
    display: flex;

    .enhanced-summary-section,
    .summary-kpi-card {
      flex: 1;
    }
  }
}

/* 美化后的数据摘要样式 */
.enhanced-summary-section {
  height: 100%;
  margin-bottom: 20px;
  background: linear-gradient(
    135deg,
    rgba(13, 41, 71, 0.95),
    rgba(25, 118, 210, 0.1)
  );
  border-radius: 16px;
  padding: 20px;
  border: 2px solid rgba(64, 158, 255, 0.3);
  box-shadow:
    0 6px 24px rgba(0, 0, 0, 0.25),
    inset 0 1px 0 rgba(255, 255, 255, 0.1);
  position: relative;
  overflow: hidden;
  display: flex;
  flex-direction: column;

  &::before {
    content: "";
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(
      45deg,
      transparent 0%,
      rgba(64, 158, 255, 0.05) 50%,
      transparent 100%
    );
    animation: shimmer 3s infinite ease-in-out;
    pointer-events: none;
  }
}

@keyframes shimmer {
  0%,
  100% {
    opacity: 0;
  }
  50% {
    opacity: 1;
  }
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
  flex: 1;
  align-content: center;
}

.stat-card {
  background: linear-gradient(
    135deg,
    rgba(25, 118, 210, 0.15),
    rgba(13, 71, 161, 0.1)
  );
  border-radius: 12px;
  padding: 16px;
  border: 1px solid rgba(64, 158, 255, 0.2);
  position: relative;
  overflow: hidden;
  transition: all 0.3s ease;

  &:hover {
    transform: translateY(-5px);
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
    border-color: rgba(64, 158, 255, 0.5);
  }

  .stat-icon-wrapper {
    width: 40px;
    height: 40px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    margin-bottom: 12px;

    .stat-icon {
      font-size: 20px;
      color: #ffffff;
    }
  }

  .stat-content {
    .stat-label {
      font-size: 12px;
      color: #90caf9;
      margin-bottom: 6px;
      font-weight: 500;
    }

    .stat-value {
      font-size: 24px;
      font-weight: 700;
      color: #e0f7ff;
      margin-bottom: 4px;
      text-shadow: 0 0 8px rgba(224, 247, 255, 0.3);
    }

    .stat-unit {
      font-size: 12px;
      color: #64b5f6;
      font-weight: 500;
    }
  }

  .stat-decoration {
    position: absolute;
    top: -50%;
    right: -50%;
    width: 100px;
    height: 100px;
    border-radius: 50%;
    opacity: 0.1;
  }

  &.primary {
    .stat-icon-wrapper {
      background: linear-gradient(135deg, #43a047, #2e7d32);
      box-shadow: 0 0 15px rgba(67, 160, 71, 0.4);
    }
    .stat-decoration {
      background: linear-gradient(135deg, #43a047, #2e7d32);
    }
  }

  &.secondary {
    .stat-icon-wrapper {
      background: linear-gradient(135deg, #1976d2, #0d47a1);
      box-shadow: 0 0 15px rgba(25, 118, 210, 0.4);
    }
    .stat-decoration {
      background: linear-gradient(135deg, #1976d2, #0d47a1);
    }
  }

  &.tertiary {
    .stat-icon-wrapper {
      background: linear-gradient(135deg, #00acc1, #006064);
      box-shadow: 0 0 15px rgba(0, 172, 193, 0.4);
    }
    .stat-decoration {
      background: linear-gradient(135deg, #00acc1, #006064);
    }
  }

  &.quaternary {
    .stat-icon-wrapper {
      background: linear-gradient(135deg, #7b1fa2, #4a148c);
      box-shadow: 0 0 15px rgba(123, 31, 162, 0.4);
    }
    .stat-decoration {
      background: linear-gradient(135deg, #7b1fa2, #4a148c);
    }
  }
}

.debug-card {
  margin-bottom: 20px;
  background-color: rgba(0, 20, 40, 0.5);
  border: 1px solid rgba(255, 193, 7, 0.3);
  color: #e0f2ff;

  .debug-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-weight: bold;
    color: #ffc107;
  }

  .debug-content {
    pre {
      background: rgba(0, 0, 0, 0.2);
      padding: 10px;
      border-radius: 4px;
      overflow-x: auto;
      color: #80cbc4;
      font-size: 12px;
    }
  }
}

.self-sufficiency-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 16px;
    font-weight: 500;

    .icon {
      font-size: 18px;
    }
  }
}

.calculation-formula {
  display: flex;
  justify-content: center;
  align-items: center;
  margin: 15px 0 10px;
  padding: 8px 12px;
  background: rgba(13, 71, 161, 0.2);
  border-radius: 6px;
  border: 1px dashed rgba(64, 158, 255, 0.4);

  .formula-text {
    font-size: 14px;
    color: rgba(224, 242, 255, 0.9);
  }

  .formula-result {
    font-size: 15px;
    font-weight: 600;
    color: #81c784;
    margin-left: 5px;
  }
}

.sufficiency-progress {
  margin: 15px 0;

  :deep(.el-progress-bar__inner) {
    background: linear-gradient(90deg, #43a047, #2e7d32);
    box-shadow: 0 0 10px rgba(67, 160, 71, 0.3);
  }

  :deep(.el-progress__text) {
    font-size: 14px !important;
    color: #e0f2ff;
    font-weight: bold;
  }
}

.energy-formula {
  margin-top: 20px;
  padding: 10px;
  background: rgba(13, 71, 161, 0.15);
  border-radius: 8px;
  border: 1px dashed rgba(64, 158, 255, 0.3);

  .formula-container {
    display: flex;
    align-items: center;
    justify-content: center;
    flex-wrap: wrap;
    gap: 8px;
  }

  .formula-part {
    text-align: center;
    background: rgba(25, 118, 210, 0.15);
    padding: 6px 12px;
    border-radius: 6px;
    min-width: 80px;

    .formula-label {
      font-size: 12px;
      color: #90caf9;
      margin-bottom: 4px;
    }

    .formula-value {
      font-size: 16px;
      font-weight: 600;

      &.positive {
        color: #81c784;
      }

      &.negative {
        color: #ef5350;
      }

      &.result {
        color: #ffb74d;
        font-size: 18px;
        text-shadow: 0 0 10px rgba(255, 183, 77, 0.4);
      }

      &.multiplier {
        color: #64b5f6;
        font-weight: 600;
      }
    }
  }

  .formula-operator {
    font-size: 20px;
    color: #64b5f6;
    font-weight: bold;
    margin: 0 4px;
    display: flex;
    align-items: center;
    justify-content: center;
    width: 24px;
    height: 24px;
  }
}

.no-data {
  height: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  color: #64b5f6;
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

/* 修复原有的分割线样式 */
:deep(.el-divider) {
  background-color: transparent !important;
  --el-border-color-light: rgba(64, 158, 255, 0.2);
  margin: 24px 0 16px;
}

.divider-content {
  display: flex;
  align-items: center;
  gap: 10px;
  color: #4fc3f7;
  font-size: 16px;
  font-weight: 500;
  height: 100%;

  .el-icon {
    font-size: 20px;
    color: #4fc3f7;
    background: linear-gradient(
      135deg,
      rgba(25, 118, 210, 0.2),
      rgba(13, 71, 161, 0.1)
    );
    width: 28px;
    height: 28px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 0 10px rgba(79, 195, 247, 0.2);

    /* 图标脉冲动画 */
    animation: iconPulse 2s infinite ease-in-out;
  }

  span {
    background: linear-gradient(to right, #64b5f6, #4fc3f7, #29b6f6);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    font-weight: 600;
    text-shadow: 0 0 5px rgba(79, 195, 247, 0.3);
  }
}

@keyframes iconPulse {
  0%,
  100% {
    box-shadow: 0 0 8px rgba(79, 195, 247, 0.3);
  }
  50% {
    box-shadow: 0 0 15px rgba(79, 195, 247, 0.5);
  }
}

.kpi-cards {
  margin: 20px 0;
}

.kpi-card {
  margin-bottom: 16px;
  background: linear-gradient(
    135deg,
    rgba(25, 118, 210, 0.2),
    rgba(13, 71, 161, 0.1)
  );
  border: 1px solid rgba(64, 158, 255, 0.2);
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  transition: all 0.3s ease;
  height: 100%; // 确保卡片高度一致

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
    border-color: rgba(64, 158, 255, 0.4);
  }

  :deep(.el-card__body) {
    padding: 15px;
    height: calc(100% - 30px); // 减去padding
    display: flex;
    flex-direction: column;
  }

  &:hover .kpi-right-section {
    border-left-color: rgba(64, 158, 255, 0.4);

    .desc-icon .el-icon {
      background-color: rgba(25, 118, 210, 0.25);
    }
  }
}

.kpi-card-content {
  display: flex;
  gap: 0;
  height: 100%;
  justify-content: space-between;
}

.kpi-left-section {
  display: flex;
  gap: 15px;
  min-width: 150px;
  width: 48%;
}

.kpi-right-section {
  display: flex;
  flex-direction: column;
  justify-content: center;
  width: 52%;
  border-left: 1px solid rgba(64, 158, 255, 0.25);
  padding-left: 15px;
  margin-left: 10px;
}

.kpi-icon-area {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 48px;
  height: 48px;
  margin-right: 12px;
  border-radius: 50%;
  background: linear-gradient(
    135deg,
    rgba(25, 118, 210, 0.2),
    rgba(13, 71, 161, 0.1)
  );
  box-shadow: 0 0 15px rgba(64, 158, 255, 0.2);

  .el-icon {
    font-size: 24px;
    color: #4fc3f7;

    /* 确保SVG图标正确显示 */
    svg {
      width: 24px;
      height: 24px;
      display: block;
      margin: auto;
      color: inherit;
      fill: currentColor;
    }
  }
}

.kpi-data-area {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0; // 防止内容溢出
  padding-right: 5px;
}

.kpi-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  color: #90caf9;
  margin-bottom: 5px;

  .el-icon {
    font-size: 14px;
    color: #64b5f6;
  }
}

.kpi-value {
  font-size: 20px;
  font-weight: 600;
  color: #e0f2ff;
  margin-bottom: 5px;
}

.kpi-desc {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  font-size: 13px;
  color: rgba(224, 242, 255, 0.9);
  line-height: 1.4;

  .desc-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    margin-top: 2px;

    .el-icon {
      color: #64b5f6;
      background-color: rgba(25, 118, 210, 0.15);
      border-radius: 50%;
      padding: 2px;
      width: 18px;
      height: 18px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
  }

  .desc-text {
    flex: 1;
    display: -webkit-box;
    -webkit-line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
    text-overflow: ellipsis;
  }
}

.kpi-meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: auto;
}

.kpi-unit {
  font-size: 12px;
  color: #64b5f6;
}

.kpi-changes {
  display: flex;
  gap: 6px;
}

.kpi-theme-0 .kpi-icon-area {
  background: linear-gradient(135deg, #43a047, #2e7d32);
  box-shadow: 0 0 15px rgba(67, 160, 71, 0.3);
}

.kpi-theme-1 .kpi-icon-area {
  background: linear-gradient(135deg, #1976d2, #0d47a1);
  box-shadow: 0 0 15px rgba(25, 118, 210, 0.3);
}

.kpi-theme-2 .kpi-icon-area {
  background: linear-gradient(135deg, #00acc1, #006064);
  box-shadow: 0 0 15px rgba(0, 172, 193, 0.3);
}

.kpi-theme-3 .kpi-icon-area {
  background: linear-gradient(135deg, #7b1fa2, #4a148c);
  box-shadow: 0 0 15px rgba(123, 31, 162, 0.3);
}

.kpi-theme-4 .kpi-icon-area {
  background: linear-gradient(135deg, #f57c00, #e65100);
  box-shadow: 0 0 15px rgba(245, 124, 0, 0.3);
}

.kpi-theme-5 .kpi-icon-area {
  background: linear-gradient(135deg, #546e7a, #263238);
  box-shadow: 0 0 15px rgba(84, 110, 122, 0.3);
}

.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: 20px 0 15px;
  padding: 0 10px;
}

.table-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 16px;
  color: #e0f2ff;
  font-weight: 500;

  .el-icon {
    font-size: 18px;
    color: #64b5f6;
  }
}

.compact-table {
  margin-bottom: 20px;

  :deep(.el-table) {
    background-color: rgba(10, 30, 55, 0.3) !important;
    border: 1px solid rgba(30, 144, 255, 0.3);
    border-radius: 8px;
    overflow: hidden;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
  }

  :deep(.el-table__inner-wrapper::before) {
    display: none;
  }

  :deep(.el-table__header) {
    border: none;

    th.el-table__cell {
      background: linear-gradient(
        180deg,
        rgba(30, 75, 120, 0.6),
        rgba(20, 55, 90, 0.7)
      ) !important;
      color: #e0f7ff;
      font-weight: 600;
      padding: 12px 0;
      border-bottom: 2px solid rgba(64, 158, 255, 0.3);
      border-right: 1px solid rgba(64, 158, 255, 0.2);

      .cell {
        font-size: 14px;
      }
    }
  }

  :deep(.el-table__row) {
    background-color: rgba(15, 35, 60, 0.4) !important;
    color: #e0f2ff;
    transition: all 0.3s ease;

    &:hover > td {
      background-color: rgba(25, 118, 210, 0.25) !important;
    }

    td {
      border-bottom: 1px solid rgba(64, 158, 255, 0.15);
      transition: all 0.3s ease;
      padding: 10px 0;
    }
  }

  :deep(.el-table--striped .el-table__body tr.el-table__row--striped td) {
    background-color: rgba(20, 45, 75, 0.4) !important;
  }

  :deep(.el-table__empty-block) {
    background-color: rgba(13, 41, 71, 0.3);

    .el-table__empty-text {
      color: #64b5f6;
    }
  }

  :deep(.el-table__cell) {
    background-color: transparent;
  }

  :deep(.el-table--border .el-table__cell) {
    border-right: 1px solid rgba(64, 158, 255, 0.15);
  }

  :deep(.el-table--border::after),
  :deep(.el-table--border::before),
  :deep(.el-table__border-left-patch),
  :deep(.el-table__border-right-patch) {
    background-color: rgba(64, 158, 255, 0.2);
  }

  /* 添加表格验证相关样式 */
  :deep(.el-form-item__error) {
    color: #ff7e82;
    background-color: rgba(255, 107, 107, 0.15);
    border-radius: 4px;
    padding: 2px 6px;
    font-weight: 500;
    box-shadow: 0 2px 5px rgba(255, 107, 107, 0.2);
  }

  :deep(.el-form-item.is-error .el-input__wrapper) {
    box-shadow: 0 0 0 1px #ff6b6b inset !important;
    background-color: rgba(255, 107, 107, 0.05);
  }

  :deep(.cell .el-tag--danger) {
    background-color: rgba(255, 87, 87, 0.15);
    border-color: rgba(255, 87, 87, 0.3);
    color: #ff8589;
  }

  :deep(.cell .el-tag--success) {
    background-color: rgba(84, 184, 67, 0.15);
    border-color: rgba(84, 184, 67, 0.3);
    color: #91d97c;
  }

  :deep(.el-table .cell) {
    color: #e0f2ff;
    font-size: 13px;
    line-height: 1.5;
  }

  :deep(.el-table__footer-wrapper) {
    background-color: rgba(20, 50, 80, 0.5) !important;

    td {
      background-color: rgba(20, 50, 80, 0.5) !important;
      color: #e0f2ff;
      border-bottom: 1px solid rgba(64, 158, 255, 0.2);
      font-weight: 600;
    }
  }

  :deep(.el-loading-mask) {
    background-color: rgba(0, 20, 40, 0.8);

    .el-loading-spinner {
      .path {
        stroke: #409eff;
      }

      .el-loading-text {
        color: #e0f2ff;
        font-size: 14px;
      }
    }
  }
}

// 增强表格内控件的深色主题样式
:deep(.el-table) {
  .el-input__wrapper {
    background-color: rgba(13, 41, 71, 0.4);
    box-shadow: 0 0 0 1px rgba(64, 158, 255, 0.2) inset;

    input {
      color: #e0f2ff;

      &::placeholder {
        color: rgba(144, 202, 249, 0.5);
      }
    }

    &:hover {
      box-shadow: 0 0 0 1px rgba(64, 158, 255, 0.4) inset;
    }

    &.is-focus {
      box-shadow: 0 0 0 1px #409eff inset;
    }
  }

  .el-select__popper {
    background-color: rgba(13, 41, 71, 0.95);
    border: 1px solid rgba(64, 158, 255, 0.2);

    .el-select-dropdown__item {
      color: #e0f2ff;

      &.hover,
      &:hover {
        background-color: rgba(64, 158, 255, 0.15);
      }

      &.selected {
        background-color: rgba(64, 158, 255, 0.25);
        color: #409eff;
      }
    }
  }

  .el-checkbox__input {
    .el-checkbox__inner {
      background-color: rgba(13, 41, 71, 0.4);
      border-color: rgba(64, 158, 255, 0.4);
    }

    &.is-checked {
      .el-checkbox__inner {
        background-color: #409eff;
      }
    }
  }
}

.indicator-cell {
  display: flex;
  align-items: center;
  gap: 8px;

  .el-icon {
    font-size: 16px;
    color: #64b5f6;
    background: rgba(25, 118, 210, 0.15);
    width: 24px;
    height: 24px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  span {
    font-weight: 500;
  }
}

// 修改强调样式，使其更加突出
.emphasis {
  font-weight: 600;
  color: #4fc3f7;
  text-shadow: 0 0 8px rgba(79, 195, 247, 0.4);
  font-size: 14px;
}

// 修改变化单元格样式
.change-cell {
  .el-tag {
    display: flex;
    align-items: center;
    gap: 4px;
    border-radius: 4px;
    padding: 4px 8px;

    .el-icon {
      background: rgba(255, 255, 255, 0.15);
      border-radius: 50%;
      padding: 2px;
      font-size: 12px;
      height: 16px;
      width: 16px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    &.el-tag--success {
      background: linear-gradient(
        135deg,
        rgba(103, 194, 58, 0.2),
        rgba(103, 194, 58, 0.1)
      );
      border: 1px solid rgba(103, 194, 58, 0.4);
      box-shadow: 0 0 10px rgba(103, 194, 58, 0.2);
      color: #aed581;

      .el-icon {
        color: #c5e1a5;
      }
    }

    &.el-tag--danger {
      background: linear-gradient(
        135deg,
        rgba(245, 108, 108, 0.2),
        rgba(245, 108, 108, 0.1)
      );
      border: 1px solid rgba(245, 108, 108, 0.4);
      box-shadow: 0 0 10px rgba(245, 108, 108, 0.2);
      color: #ff8a80;

      .el-icon {
        color: #ffab91;
      }
    }

    &.el-tag--info {
      background: linear-gradient(
        135deg,
        rgba(96, 125, 139, 0.2),
        rgba(96, 125, 139, 0.1)
      );
      border: 1px solid rgba(96, 125, 139, 0.4);
      color: #b0bec5;

      .el-icon {
        color: #cfd8dc;
      }
    }
  }
}

/* 使用CSS Grid替代el-row布局 */
.top-devices-grid {
  display: grid;
  gap: 12px;
  margin-bottom: 20px;

  /* 根据设备数量动态设置列数 */
  &[data-count="1"] {
    grid-template-columns: 1fr;
  }

  &[data-count="2"] {
    grid-template-columns: repeat(2, 1fr);
  }

  &[data-count="3"] {
    grid-template-columns: repeat(3, 1fr);
  }

  &[data-count="4"] {
    grid-template-columns: repeat(4, 1fr);
  }

  &[data-count="5"] {
    grid-template-columns: repeat(5, 1fr);
  }

  /* 默认情况下最多5列 */
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
}

.device-item {
  display: flex;

  .device-card {
    flex: 1;
    width: 100%;
  }
}

.device-card {
  background: linear-gradient(
    135deg,
    rgba(25, 118, 210, 0.2),
    rgba(13, 71, 161, 0.1)
  );
  border: 1px solid rgba(64, 158, 255, 0.2);
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  transition: all 0.3s ease;
  height: 100%;
  min-width: 0; // 允许flex收缩

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
    border-color: rgba(64, 158, 255, 0.4);
  }

  :deep(.el-card__body) {
    padding: 15px;
    display: flex;
    align-items: center;
    gap: 15px;
  }

  .device-change {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 8px;

    span {
      color: rgba(224, 242, 255, 0.7);
      font-size: 12px;
    }

    .change-value {
      font-size: 14px;
    }
  }
}

.device-rank {
  .rank-badge {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 18px;
    font-weight: 600;
    color: white;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
  }

  .rank-1 {
    background: linear-gradient(135deg, #ffd700, #ff9800);
    box-shadow: 0 0 15px rgba(255, 215, 0, 0.4);
  }

  .rank-2 {
    background: linear-gradient(135deg, #e0e0e0, #9e9e9e);
    box-shadow: 0 0 12px rgba(224, 224, 224, 0.4);
  }

  .rank-3 {
    background: linear-gradient(135deg, #d7995b, #8d6e63);
    box-shadow: 0 0 12px rgba(215, 153, 91, 0.4);
  }
}

.device-info {
  flex: 1;

  .device-name {
    font-size: 13px;
    color: #e0f2ff;
    margin-bottom: 5px;
    line-height: 1.3;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
    text-overflow: ellipsis;
    min-height: 32px;
    word-break: break-all;
  }

  .device-change {
    display: flex;
    align-items: center;
    gap: 5px;
    font-size: 12px;
    color: #90caf9;
  }
}

/* 总结与建议部分样式 */
.summary-suggestions {
  margin-bottom: 20px;
}

.summary-card,
.suggestion-card {
  height: 100%;
  background: linear-gradient(
    135deg,
    rgba(25, 118, 210, 0.2),
    rgba(13, 71, 161, 0.1)
  );
  border: 1px solid rgba(64, 158, 255, 0.2);
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  color: #e0f2ff;
  transition: all 0.3s ease;

  &:hover {
    box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
    border-color: rgba(64, 158, 255, 0.4);
  }

  :deep(.el-card__body) {
    padding: 15px;
    max-height: 300px;
    overflow-y: auto;

    &::-webkit-scrollbar {
      width: 6px;
    }

    &::-webkit-scrollbar-track {
      background: rgba(13, 71, 161, 0.1);
      border-radius: 3px;
    }

    &::-webkit-scrollbar-thumb {
      background: rgba(64, 158, 255, 0.4);
      border-radius: 3px;
    }
  }

  :deep(.el-card__header) {
    padding: 12px 15px;
    background: linear-gradient(
      135deg,
      rgba(30, 85, 140, 0.8),
      rgba(25, 75, 130, 0.7)
    );
    border-bottom: 1px solid rgba(64, 158, 255, 0.3);

    .card-header {
      display: flex;
      align-items: center;
      gap: 10px;
      color: #e0f7ff;
      font-weight: 600;
      font-size: 15px;

      .el-icon {
        font-size: 18px;
        background: rgba(255, 255, 255, 0.15);
        border-radius: 50%;
        width: 24px;
        height: 24px;
        display: flex;
        align-items: center;
        justify-content: center;
      }
    }
  }
}

.summary-content,
.suggestion-content {
  .summary-list,
  .suggestion-list {
    list-style: none;
    padding: 0;
    margin: 0;

    li {
      margin-bottom: 12px;
      padding-left: 8px;
      border-left: 2px solid rgba(64, 158, 255, 0.4);
      display: flex;
      align-items: flex-start;
      gap: 10px;

      &:last-child {
        margin-bottom: 0;
      }

      .el-icon {
        font-size: 16px;
        background: rgba(25, 118, 210, 0.15);
        width: 24px;
        height: 24px;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;

        &.positive {
          color: #67c23a;
          background: rgba(103, 194, 58, 0.15);
        }

        &.negative {
          color: #f56c6c;
          background: rgba(245, 108, 108, 0.15);
        }

        &.warning {
          color: #e6a23c;
          background: rgba(230, 162, 60, 0.15);
        }
      }

      span {
        flex: 1;
        line-height: 1.5;
      }
    }
  }
}

.suggestion-item {
  width: 100%;

  .suggestion-title {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 6px;

    span {
      font-weight: 500;
      font-size: 14px;
    }

    .el-tag {
      padding: 0 6px;
      height: 20px;
      line-height: 18px;
      white-space: nowrap;
      font-size: 11px;
      font-weight: 600;
    }
  }

  .suggestion-desc {
    color: rgba(224, 242, 255, 0.8);
    font-size: 13px;
    line-height: 1.5;
    padding-left: 5px;
  }
}

/* 添加高亮分割线样式 */
.highlight-section {
  .highlight-icon {
    background: linear-gradient(135deg, #1976d2, #64b5f6);
    box-shadow:
      0 0 15px rgba(64, 158, 255, 0.6),
      inset 0 0 8px rgba(255, 255, 255, 0.3);
    animation: iconPulseHighlight 2s infinite ease-in-out;
  }

  span {
    font-size: 17px;
    font-weight: 700;
    text-shadow: 0 0 10px rgba(64, 158, 255, 0.7);
  }
}

@keyframes iconPulseHighlight {
  0%,
  100% {
    box-shadow:
      0 0 15px rgba(64, 158, 255, 0.6),
      inset 0 0 8px rgba(255, 255, 255, 0.3);
  }
  50% {
    box-shadow:
      0 0 20px rgba(64, 158, 255, 0.8),
      inset 0 0 12px rgba(255, 255, 255, 0.5);
  }
}

/* 自定义分隔标题样式 */
.dashboard-section-header {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 32px 0 24px;
  height: 50px;

  .section-line {
    position: absolute;
    top: 50%;
    left: 0;
    right: 0;
    height: 2px;
    background: linear-gradient(
      90deg,
      rgba(13, 71, 161, 0.1),
      rgba(64, 158, 255, 0.7),
      rgba(13, 71, 161, 0.1)
    );
    box-shadow: 0 0 12px rgba(64, 158, 255, 0.4);
    border-radius: 2px;
    z-index: 1;
  }

  .section-title {
    position: relative;
    display: flex;
    align-items: center;
    gap: 12px;
    margin-left: 30px;
    padding: 0 20px;
    height: 40px;
    background: linear-gradient(
      135deg,
      rgba(13, 71, 161, 0.9),
      rgba(25, 118, 210, 0.7)
    );
    border-radius: 6px;
    border: 1px solid rgba(64, 158, 255, 0.5);
    box-shadow:
      0 4px 15px rgba(0, 0, 0, 0.3),
      inset 0 0 10px rgba(64, 158, 255, 0.2),
      0 0 20px rgba(64, 158, 255, 0.3);
    z-index: 2;

    /* 标题文字闪光效果 */
    &::after {
      content: "";
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: linear-gradient(
        135deg,
        transparent 0%,
        rgba(79, 195, 247, 0.2) 50%,
        transparent 100%
      );
      animation: titleShine 3s infinite ease-in-out;
      border-radius: 5px;
      pointer-events: none;
    }

    .section-icon {
      font-size: 18px;
      color: #e0f7ff;
      background: rgba(64, 158, 255, 0.4);
      width: 28px;
      height: 28px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow:
        0 0 10px rgba(64, 158, 255, 0.4),
        inset 0 0 5px rgba(255, 255, 255, 0.2);

      /* 图标脉冲动画 */
      animation: iconPulse 2s infinite ease-in-out;

      /* 确保图标居中显示 */
      svg {
        width: 16px;
        height: 16px;
        display: block;
        margin: auto;
        color: #e0f7ff;
        fill: currentColor;
      }
    }

    span {
      color: #e0f7ff;
      font-size: 16px;
      font-weight: 600;
      text-shadow: 0 0 8px rgba(64, 158, 255, 0.5);
      letter-spacing: 1px;
    }

    .title-controls {
      margin-left: auto;
      position: relative;
      z-index: 3;

      :deep(.el-radio-group) {
        .el-radio-button__inner {
          background-color: rgba(15, 40, 70, 0.4);
          border-color: rgba(64, 158, 255, 0.3);
          color: #90caf9;
          font-size: 12px;
          padding: 6px 12px;

          &:hover {
            background-color: rgba(25, 118, 210, 0.2);
            border-color: rgba(64, 158, 255, 0.5);
            color: #e0f2ff;
          }
        }

        .el-radio-button__original-radio:checked + .el-radio-button__inner {
          background: linear-gradient(135deg, #1976d2, #0d47a1);
          border-color: #1976d2;
          color: #ffffff;
          font-weight: 600;
          box-shadow:
            0 0 15px rgba(25, 118, 210, 0.6),
            0 2px 8px rgba(0, 0, 0, 0.3),
            inset 0 1px 0 rgba(255, 255, 255, 0.2);
          transform: translateY(-1px);
          position: relative;

          &::before {
            content: "";
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: linear-gradient(
              135deg,
              transparent,
              rgba(255, 255, 255, 0.1),
              transparent
            );
            border-radius: inherit;
            animation: selectedShine 2s infinite ease-in-out;
          }
        }
      }
    }
  }

  .section-controls {
    position: relative;
    z-index: 2;

    :deep(.el-radio-group) {
      .el-radio-button__inner {
        background-color: rgba(15, 40, 70, 0.4);
        border-color: rgba(64, 158, 255, 0.3);
        color: #90caf9;
        font-size: 12px;
        padding: 6px 12px;

        &:hover {
          background-color: rgba(25, 118, 210, 0.2);
          border-color: rgba(64, 158, 255, 0.5);
          color: #e0f2ff;
        }
      }

      .el-radio-button__original-radio:checked + .el-radio-button__inner {
        background-color: rgba(25, 118, 210, 0.6);
        border-color: #1976d2;
        color: #ffffff;
        box-shadow: 0 0 8px rgba(25, 118, 210, 0.4);
      }
    }
  }
}

/* 高亮分隔标题样式 */
.highlight-section {
  background: linear-gradient(
    135deg,
    rgba(25, 118, 210, 0.95),
    rgba(30, 136, 229, 0.8)
  );
  border: 1px solid rgba(100, 181, 246, 0.6);
  box-shadow:
    0 4px 15px rgba(0, 0, 0, 0.3),
    inset 0 0 15px rgba(100, 181, 246, 0.3),
    0 0 25px rgba(100, 181, 246, 0.4);

  .highlight-icon {
    background: linear-gradient(135deg, #1976d2, #64b5f6);
    box-shadow:
      0 0 15px rgba(64, 158, 255, 0.6),
      inset 0 0 8px rgba(255, 255, 255, 0.3);
    animation: iconPulseHighlight 2s infinite ease-in-out;
  }

  span {
    font-size: 17px;
    font-weight: 700;
    text-shadow: 0 0 10px rgba(64, 158, 255, 0.7);
  }
}

@keyframes titleShine {
  0% {
    background-position: -200px 0;
  }
  50% {
    background-position: 200px 0;
  }
  100% {
    background-position: -200px 0;
  }
}

@keyframes iconPulse {
  0%,
  100% {
    box-shadow: 0 0 8px rgba(79, 195, 247, 0.3);
  }
  50% {
    box-shadow: 0 0 15px rgba(79, 195, 247, 0.5);
  }
}

@keyframes selectedShine {
  0% {
    opacity: 0;
    transform: translateX(-100%);
  }
  50% {
    opacity: 1;
  }
  100% {
    opacity: 0;
    transform: translateX(100%);
  }
}

@keyframes iconPulseHighlight {
  0%,
  100% {
    box-shadow:
      0 0 15px rgba(64, 158, 255, 0.6),
      inset 0 0 8px rgba(255, 255, 255, 0.3);
  }
  50% {
    box-shadow:
      0 0 20px rgba(64, 158, 255, 0.8),
      inset 0 0 12px rgba(255, 255, 255, 0.5);
  }
}

/* 添加增减率样式 */
.rate-up {
  color: #67c23a;
  font-weight: 600;
}

.rate-down {
  color: #f56c6c;
  font-weight: 600;
}

.change-detail {
  font-size: 11px;
  opacity: 0.7;
  margin-top: 2px;
}

.change-cell {
  display: flex;
  flex-direction: column;
  align-items: center;

  &.change-positive {
    color: #67c23a;
  }

  &.change-negative {
    color: #f56c6c;
  }

  &.change-neutral {
    color: #909399;
  }
}
</style>

<style lang="scss">
/* 全局表格样式增强 */
/* 深色主题下拾取器和下拉菜单的全局样式 */
.el-select__popper,
.el-picker__popper,
.el-dropdown-menu {
  background-color: rgba(15, 40, 70, 0.95) !important;
  border: 1px solid rgba(64, 158, 255, 0.25) !important;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4) !important;
  backdrop-filter: blur(10px);

  .el-select-dropdown__item,
  .el-dropdown-menu__item {
    color: #e0f7ff !important;

    &:hover,
    &.hover {
      background-color: rgba(64, 158, 255, 0.2) !important;
    }

    &.selected,
    &.is-active {
      background-color: rgba(64, 158, 255, 0.3) !important;
      color: #4fc3f7 !important;
      font-weight: 500;
    }
  }

  .el-date-table td {
    &.available:hover {
      color: #4fc3f7 !important;
    }

    &.current:not(.disabled) span {
      background-color: #1976d2 !important;
      color: #ffffff !important;
      box-shadow: 0 0 10px rgba(25, 118, 210, 0.5);
    }

    &.today span {
      color: #4fc3f7 !important;
      font-weight: bold;
    }
  }

  .el-date-picker__header-label,
  .el-date-range-picker__header div,
  .el-date-picker__time-header {
    color: #e0f7ff !important;
  }

  .el-picker-panel__icon-btn {
    color: #64b5f6 !important;

    &:hover {
      color: #4fc3f7 !important;
    }
  }

  .el-date-table th {
    color: #90caf9 !important;
    border-bottom: 1px solid rgba(64, 158, 255, 0.25) !important;
  }
}

/* 调整表单控件的全局样式 */
.month-energy-report-container {
  .el-table {
    --el-table-border-color: rgba(64, 158, 255, 0.25);
    --el-table-header-background-color: transparent;
    --el-table-tr-background-color: transparent;
    --el-table-tr-hover-background-color: rgba(25, 118, 210, 0.25);
    --el-table-fixed-box-shadow: 0 0 10px rgba(0, 0, 0, 0.2);

    &::before {
      background-color: transparent;
    }
  }

  .el-form-item__label {
    color: #e0f7ff !important;
    font-weight: 500;
  }

  .el-input__wrapper,
  .el-textarea__wrapper {
    background-color: rgba(15, 40, 70, 0.4) !important;
    box-shadow: 0 0 0 1px rgba(64, 158, 255, 0.25) inset !important;

    input,
    textarea {
      color: #e0f7ff !important;

      &::placeholder {
        color: rgba(144, 202, 249, 0.5) !important;
      }
    }

    &:hover {
      box-shadow: 0 0 0 1px rgba(64, 158, 255, 0.5) inset !important;
    }

    &.is-focus {
      box-shadow: 0 0 0 1px #1976d2 inset !important;
      background-color: rgba(20, 45, 80, 0.4) !important;
    }
  }

  /* 表格特殊效果 */
  .compact-table {
    position: relative;

    &::after {
      content: "";
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      pointer-events: none;
      border-radius: 8px;
      box-shadow: inset 0 0 20px rgba(25, 118, 210, 0.1);
    }
  }
}

/* 添加更强力的覆盖，消除表格中的白色 */
.month-energy-report-container {
  /* existing styles */

  /* 强制覆盖表格颜色 */
  :deep(.el-table) {
    background-color: rgba(15, 40, 70, 0.4) !important;
    color: #e0f7ff !important;

    .el-table__header-wrapper {
      th.el-table__cell {
        background-color: rgba(30, 85, 140, 0.8) !important;
        background-image: linear-gradient(
          to bottom,
          rgba(30, 85, 140, 0.8),
          rgba(25, 75, 130, 0.7)
        ) !important;
        color: #e0f7ff !important;
      }
    }

    .el-table__body-wrapper {
      background-color: rgba(15, 40, 70, 0.3) !important;

      tr.el-table__row {
        background-color: rgba(15, 35, 60, 0.4) !important;

        td.el-table__cell {
          background-color: transparent !important;
        }

        &:hover > td.el-table__cell {
          background-color: rgba(25, 118, 210, 0.25) !important;
        }
      }

      tr.el-table__row--striped {
        td.el-table__cell {
          background-color: rgba(20, 45, 75, 0.4) !important;
        }
      }
    }

    .el-table__cell {
      border-right: 1px solid rgba(64, 158, 255, 0.15) !important;
    }

    .el-table__footer-wrapper {
      tbody td.el-table__cell {
        background-color: rgba(20, 50, 80, 0.5) !important;
        color: #e0f7ff !important;
      }
    }

    /* 确保固定列也有正确的颜色 */
    .el-table__fixed-right,
    .el-table__fixed {
      .el-table__fixed-header-wrapper,
      .el-table__fixed-body-wrapper {
        background-color: transparent !important;

        th.el-table__cell {
          background-color: rgba(30, 85, 140, 0.8) !important;
          background-image: linear-gradient(
            to bottom,
            rgba(30, 85, 140, 0.8),
            rgba(25, 75, 130, 0.7)
          ) !important;
        }

        .el-table__row,
        .el-table__row td.el-table__cell {
          background-color: rgba(15, 35, 60, 0.4) !important;
        }

        .el-table__row--striped td.el-table__cell {
          background-color: rgba(20, 45, 75, 0.4) !important;
        }
      }
    }

    /* 表格遮罩 */
    .el-loading-mask {
      background-color: rgba(15, 40, 70, 0.7) !important;
    }
  }
}

/* 完全覆盖所有表格样式 */
.compact-table.el-table--border,
.compact-table.el-table--group {
  border-color: rgba(64, 158, 255, 0.25) !important;
}

/* 全局覆盖表格白色背景 */
.el-table__body tr.current-row > td.el-table__cell {
  background-color: rgba(25, 118, 210, 0.3) !important;
}

/* 使表格中所有白色元素变为透明 */
.el-table::before,
.el-table::after,
.el-table tbody tr:hover > td,
.el-table tbody td.hover-row,
.el-table .el-table__body tr.hover-row > td {
  background-color: transparent !important;
}

/* 处理表格溢出和边缘问题 */
.el-table::before,
.el-table::after {
  height: 0 !important;
  background-color: transparent !important;
}

.el-table__inner-wrapper::before {
  height: 0 !important;
  background-color: transparent !important;
}

/* 添加全局深色主题的表格样式 - 包括溢出部分 */
.month-energy-report-container .el-table {
  --el-table-border: 1px solid rgba(64, 158, 255, 0.25) !important;
  --el-table-border-color: rgba(64, 158, 255, 0.25) !important;
  --el-table-text-color: #e0f7ff !important;
  --el-table-header-text-color: #e0f7ff !important;
  --el-table-row-hover-bg-color: rgba(25, 118, 210, 0.25) !important;
  --el-mask-color: rgba(15, 40, 70, 0.7) !important;
}

/* 修复表格分页器可能的白色背景 */
.el-pagination {
  --el-pagination-bg-color: transparent !important;
  --el-pagination-button-color: #e0f7ff !important;
  --el-pagination-button-bg-color: rgba(15, 40, 70, 0.4) !important;
  --el-pagination-button-disabled-color: #909399 !important;
  --el-pagination-button-disabled-bg-color: rgba(15, 40, 70, 0.2) !important;
  --el-pagination-hover-color: #4fc3f7 !important;
}

/* 修复空表格状态 */
.el-empty {
  --el-empty-fill-color-0: rgba(20, 50, 80, 0.5) !important;
  --el-empty-fill-color-1: rgba(15, 40, 70, 0.5) !important;
  --el-empty-fill-color-2: rgba(25, 75, 120, 0.5) !important;
  --el-empty-description-color: #64b5f6 !important;
}

/* 修复表格阴影和边缘效果 */
.compact-table {
  border-radius: 8px !important;
  overflow: hidden !important;
  border: 1px solid rgba(64, 158, 255, 0.25) !important;
  box-shadow:
    0 0 20px rgba(0, 0, 0, 0.15),
    0 0 5px rgba(64, 158, 255, 0.1) !important;
}

/* 使用!important加强深色主题覆盖 */
body .dark-theme {
  .el-table {
    background-color: rgba(15, 40, 70, 0.4) !important;
    color: #e0f7ff !important;

    .el-table__cell {
      background-color: transparent !important;
    }

    .el-table__body tr.el-table__row {
      background-color: rgba(15, 35, 60, 0.4) !important;

      &:hover {
        background-color: rgba(25, 118, 210, 0.25) !important;

        td {
          background-color: rgba(25, 118, 210, 0.25) !important;
        }
      }

      &.el-table__row--striped td {
        background-color: rgba(20, 45, 75, 0.4) !important;
      }
    }

    /* 修复表格垂直和水平边框 */
    &.el-table--border {
      &::after {
        background-color: rgba(64, 158, 255, 0.25) !important;
      }

      .el-table__inner-wrapper {
        &::after {
          background-color: rgba(64, 158, 255, 0.25) !important;
        }
      }

      .el-table__cell {
        border-right: 1px solid rgba(64, 158, 255, 0.15) !important;
        border-bottom: 1px solid rgba(64, 158, 255, 0.15) !important;
      }
    }

    /* 表格单元格背景颜色强制覆盖 */
    .el-table__body-wrapper .el-table__body td.el-table__cell {
      background-color: transparent !important;
    }
  }

  /* 覆盖Element Plus表格样式 - 通过更高特异性 */
  .el-table,
  .el-table__header,
  .el-table__body,
  .el-table__footer {
    border-collapse: separate !important;
    border-spacing: 0 !important;
  }

  /* 修复表格外观边框线 */
  table.el-table__body {
    border-color: rgba(64, 158, 255, 0.15) !important;
  }

  /* 修复表格头与表格体之间的白色线条 */
  .el-table th.el-table__cell.is-leaf {
    border-bottom: 2px solid rgba(64, 158, 255, 0.3) !important;
  }
}

/* 全局样式覆盖：确保表格所有元素深色化 */
.dark-theme .el-table,
.dark-theme .el-table__header-wrapper,
.dark-theme .el-table__body-wrapper,
.dark-theme .el-table__footer-wrapper,
.dark-theme .el-table__header,
.dark-theme .el-table__body,
.dark-theme .el-table__footer {
  background-color: transparent !important;
}

.dark-theme .el-table thead th.el-table__cell {
  background-color: rgba(30, 85, 140, 0.8) !important;
  background-image: linear-gradient(
    to bottom,
    rgba(30, 85, 140, 0.8),
    rgba(25, 75, 130, 0.7)
  ) !important;
  color: #e0f7ff !important;
}

.dark-theme .el-table .el-table__body tr.el-table__row td.el-table__cell {
  background-color: rgba(15, 35, 60, 0.4) !important;
  color: #e0f7ff !important;
}

.dark-theme
  .el-table
  .el-table__body
  tr.el-table__row--striped
  td.el-table__cell {
  background-color: rgba(20, 45, 75, 0.4) !important;
}

.dark-theme .el-table .el-table__body tr.el-table__row:hover td.el-table__cell {
  background-color: rgba(25, 118, 210, 0.25) !important;
}

/* 确保表格边缘无白色 */
.dark-theme .el-table::before,
.dark-theme .el-table::after,
.dark-theme .el-table .el-table__inner-wrapper::before {
  background-color: transparent !important;
}

/* 修复可能出现的边框白线 */
.dark-theme .el-table--border,
.dark-theme .el-table--group {
  border-color: rgba(64, 158, 255, 0.25) !important;
}

.dark-theme .el-table__border-left-patch,
.dark-theme .el-table__border-right-patch {
  background-color: rgba(30, 85, 140, 0.8) !important;
}

/* 最后保险 - 为任何可能漏掉的表格相关元素设置透明背景 */
.dark-theme [class*="el-table"] {
  background-color: transparent !important;
}

/* 移除el-divider全局样式，已被自定义组件替代 */
.dark-theme :deep(.el-divider),
.dark-theme :deep(.el-divider__text) {
  display: none !important;
}

/* 删除原有的el-divider样式，已被替换为自定义组件 */

@keyframes iconPulse {
  0%,
  100% {
    box-shadow: 0 0 8px rgba(79, 195, 247, 0.3);
  }
  50% {
    box-shadow: 0 0 15px rgba(79, 195, 247, 0.5);
  }
}

.summary-kpi-card {
  height: 100%;
  background: linear-gradient(
    135deg,
    rgba(25, 118, 210, 0.2),
    rgba(13, 71, 161, 0.1)
  );
  border: 1px solid rgba(64, 158, 255, 0.2);
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  color: #e0f2ff;
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;

  &:hover {
    box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
    border-color: rgba(64, 158, 255, 0.4);
  }

  :deep(.el-card__body) {
    padding: 16px;
    flex: 1;
    display: flex;
    flex-direction: column;
  }
}

.self-sufficiency-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 14px;
    font-weight: 500;

    .icon {
      font-size: 16px;
    }
  }
}

.sufficiency-progress {
  margin: 12px 0;

  :deep(.el-progress-bar__inner) {
    background: linear-gradient(90deg, #43a047, #2e7d32);
    box-shadow: 0 0 10px rgba(67, 160, 71, 0.3);
  }

  :deep(.el-progress__text) {
    font-size: 14px !important;
    color: #e0f2ff;
    font-weight: bold;
  }
}

.energy-formula {
  margin-top: 16px;
  padding: 8px;
  background: rgba(13, 71, 161, 0.15);
  border-radius: 6px;
  border: 1px dashed rgba(64, 158, 255, 0.3);
  flex: 1;
  display: flex;
  align-items: center;

  .formula-container {
    display: flex;
    align-items: center;
    justify-content: center;
    flex-wrap: wrap;
    gap: 6px;
    width: 100%;
  }

  .formula-part {
    text-align: center;
    background: rgba(25, 118, 210, 0.15);
    padding: 4px 8px;
    border-radius: 4px;
    min-width: 60px;

    .formula-label {
      font-size: 10px;
      color: #90caf9;
      margin-bottom: 2px;
    }

    .formula-value {
      font-size: 12px;
      font-weight: 600;

      &.positive {
        color: #81c784;
      }

      &.negative {
        color: #ef5350;
      }

      &.result {
        color: #ffb74d;
        font-size: 14px;
        text-shadow: 0 0 8px rgba(255, 183, 77, 0.4);
      }

      &.multiplier {
        color: #64b5f6;
        font-weight: 600;
      }
    }
  }

  .formula-operator {
    font-size: 16px;
    color: #64b5f6;
    font-weight: bold;
    margin: 0 3px;
    display: flex;
    align-items: center;
    justify-content: center;
    width: 20px;
    height: 20px;
  }
}
</style>
