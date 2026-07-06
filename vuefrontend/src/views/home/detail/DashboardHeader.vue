<template>
  <nav class="navbar">
    <div class="nav-brand">
      <div class="logo" @click="goBack">
        <el-icon><component :is="useRenderIcon(boltIcon)" /></el-icon>
      </div>
      <h1 class="nav-title">{{ title }}</h1>
    </div>
    <div class="nav-actions">
      <div class="time-display">
        <el-icon><component :is="useRenderIcon(clockIcon)" /></el-icon>
        <span id="current-time">{{ currentTime }}</span>
      </div>
      <div class="back-btn" @click="goBack">
        <el-icon><component :is="useRenderIcon(arrowLeft)" /></el-icon>
        <span>返回大屏</span>
      </div>
    </div>
  </nav>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import dayjs from "dayjs";

// 导入图标
import arrowLeft from "~icons/ep/arrow-left";
import clockIcon from "~icons/ep/clock";
import calendarIcon from "~icons/ep/calendar";
import refresh from "~icons/material-symbols/refresh";
import boltIcon from "~icons/material-symbols/bolt";

interface Props {
  title?: string;
  showDateSelector?: boolean;
  dateType?: "day" | "month" | "year";
  dateOptions?: { label: string; value: string }[];
  showPrevMonthBtn?: boolean;
  currentDate?: string;
}

const props = withDefaults(defineProps<Props>(), {
  title: "详情页",
  showDateSelector: true,
  dateType: "month",
  dateOptions: () => [
    { label: "日", value: "day" },
    { label: "月", value: "month" },
    { label: "年", value: "year" }
  ],
  showPrevMonthBtn: false,
  currentDate: ""
});

type EmitEvents = {
  (e: "update:dateType", type: "day" | "month" | "year"): void;
  (e: "refresh"): void;
  (e: "prevMonth"): void;
};
const emit = defineEmits<EmitEvents>();

const route = useRoute();
const router = useRouter();
const currentTime = ref("");
const timeInterval = ref<any>(null);

// 方法：更新日期时间
const updateTime = () => {
  const now = new Date();
  const options: Intl.DateTimeFormatOptions = {
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

// 返回上一页
const goBack = () => {
  router.push("/cockpit");
};

// 跳转到上月分析报告
const goToPrevMonth = () => {
  // 如果提供了当前日期，则基于此计算上个月
  const baseDate = props.currentDate || dayjs().format("YYYY-MM-DD");
  // 修改获取上月日期的代码，确保获取的是上月1号的日期
  const prevMonth = dayjs(baseDate)
    .subtract(1, "month")
    .startOf("month")
    .format("YYYY-MM-DD");

  // 跳转到上月报告
  router.push({
    path: "/MonthEnergyReportFullscreen",
    query: { date: prevMonth }
  });

  // 触发自定义事件
  emit("prevMonth");
};

// 处理日期类型变更
const handleDateTypeChange = (type: "day" | "month" | "year") => {
  emit("update:dateType", type);
};

// 刷新数据
const refreshData = () => {
  emit("refresh");
};

// 在组件挂载时开始每秒定时刷新时间
onMounted(() => {
  updateTime();
  timeInterval.value = setInterval(updateTime, 1000);
});

// 组件卸载时清理资源
onUnmounted(() => {
  clearInterval(timeInterval.value);
});
</script>

<style scoped>
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
  margin-bottom: 24px;
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
  cursor: pointer;
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

.back-btn,
.refresh-btn,
.prev-month-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  border-radius: 20px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.3s ease;
}

.back-btn {
  background: linear-gradient(
    135deg,
    rgba(255, 0, 0, 0.1),
    rgba(255, 0, 0, 0.2)
  );
  border: 1px solid rgba(255, 0, 0, 0.3);
  color: rgba(255, 255, 255, 0.9);
}

.back-btn:hover {
  background: linear-gradient(
    135deg,
    rgba(255, 0, 0, 0.2),
    rgba(255, 0, 0, 0.3)
  );
  transform: translateY(-2px);
}

.prev-month-btn {
  background: linear-gradient(
    135deg,
    rgba(0, 162, 255, 0.1),
    rgba(0, 162, 255, 0.2)
  );
  border: 1px solid rgba(0, 162, 255, 0.3);
  color: rgba(255, 255, 255, 0.9);
}

.prev-month-btn:hover {
  background: linear-gradient(
    135deg,
    rgba(0, 162, 255, 0.2),
    rgba(0, 162, 255, 0.3)
  );
  transform: translateY(-2px);
}

.refresh-btn {
  background: linear-gradient(
    135deg,
    rgba(0, 255, 135, 0.1),
    rgba(0, 255, 135, 0.2)
  );
  border: 1px solid rgba(0, 255, 135, 0.3);
  color: rgba(255, 255, 255, 0.9);
}

.refresh-btn:hover {
  background: linear-gradient(
    135deg,
    rgba(0, 255, 135, 0.2),
    rgba(0, 255, 135, 0.3)
  );
  transform: translateY(-2px);
}
</style>
