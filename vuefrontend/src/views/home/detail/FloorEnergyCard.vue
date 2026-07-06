<template>
  <div
    class="floor-card"
    :class="{ 'is-active': isActive, 'admin-card': isAdmin }"
    :style="{
      '--animation-delay': `${index * 0.15}s`,
      '--primary-color': primaryColor,
      '--secondary-color': secondaryColor
    }"
  >
    <!-- 卡片头部 -->
    <div class="floor-card-header">
      <div class="floor-icon-container">
        <FloorIconComponent
          :floor="floorData.name"
          :color="primaryColor"
          :is-active="isActive"
          :glow-effect="true"
          :size="40"
        />
      </div>
      <div class="floor-title">
        <span class="floor-name">{{ customName || floorData.name }}</span>
        <span class="floor-rank">#{{ index + 1 }}</span>
      </div>
    </div>

    <!-- 卡片内容 -->
    <div class="floor-card-content">
      <!-- 进度条 -->
      <div class="progress-section">
        <div class="progress-bar">
          <div
            class="progress-fill"
            :style="{ width: `${dayRate || floorData.rate}%` }"
          />
          <div
            class="progress-glow"
            :style="{
              width: `${dayRate || floorData.rate}%`,
              background: `radial-gradient(ellipse at right, ${primaryColor}, transparent 70%)`
            }"
          />
        </div>
        <div class="progress-text">
          日占比:
          <span class="progress-value"
            >{{ formatNumber(dayRate || floorData.rate) }}%</span
          >
        </div>
      </div>
      <!-- 用电数据 -->
      <div class="floor-data-grid">
        <div class="data-item active day-item">
          <div class="data-icon day">
            <span>日</span>
          </div>
          <div class="data-content">
            <div class="data-label">日用电</div>
            <div class="data-value">
              <span class="animate-value">
                {{ formatNumber(dayPower) }}
              </span>
              <span class="unit">kWh</span>
            </div>
          </div>
        </div>

        <div class="data-item active month-item">
          <div class="data-icon month">
            <span>月</span>
          </div>
          <div class="data-content">
            <div class="data-label">月用电</div>
            <div class="data-value">
              <span class="animate-value">
                {{ formatNumber(monthPower) }}
              </span>
              <span class="unit">kWh</span>
            </div>
          </div>
        </div>

        <div class="data-item active year-item">
          <div class="data-icon year">
            <span>年</span>
          </div>
          <div class="data-content">
            <div class="data-label">年用电</div>
            <div class="data-value">
              <span class="animate-value">
                {{ formatNumber(yearPower) }}
              </span>
              <span class="unit">kWh</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 装饰效果 -->
    <div class="card-decorations">
      <div class="card-corner card-corner-tl" />
      <div class="card-corner card-corner-tr" />
      <div class="card-corner card-corner-bl" />
      <div class="card-corner card-corner-br" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, watch } from "vue";
import FloorIconComponent from "./FloorIconComponent.vue";

interface FloorData {
  name: string;
  power: number;
  rate: number;
}

interface CombinedFloorData extends FloorData {
  dayPower?: number;
  dayRate?: number;
  monthPower?: number;
  yearPower?: number;
}

interface Props {
  floorData: CombinedFloorData;
  customName?: string;
  index: number;
  timeType?: "day" | "month" | "year";
  colorIndex?: number;
  isActive?: boolean;
  isAdmin?: boolean;
  dayPower?: number;
  dayRate?: number;
  monthPower?: number;
  yearPower?: number;
}

const props = withDefaults(defineProps<Props>(), {
  customName: "",
  timeType: "day",
  colorIndex: 0,
  isActive: false,
  isAdmin: false,
  dayPower: 0,
  dayRate: 0,
  monthPower: 0,
  yearPower: 0
});

// 组件加载时输出传入的数据
onMounted(() => {
  console.log(
    `FloorEnergyCard 加载 - 名称:${props.floorData.name}, 日:${props.dayPower}, 月:${props.monthPower}, 年:${props.yearPower}`
  );
});

// 监听数据变化
watch(
  () => props.floorData,
  newData => {
    console.log(
      `FloorEnergyCard 数据更新 - 名称:${newData.name}, 日:${props.dayPower}, 月:${props.monthPower}, 年:${props.yearPower}`
    );
  },
  { deep: true }
);

// 楼层卡片颜色方案
const colorSchemes = [
  ["rgba(0, 201, 255, 0.8)", "rgba(146, 254, 157, 0.8)"], // 蓝绿
  ["rgba(252, 92, 125, 0.8)", "rgba(106, 130, 251, 0.8)"], // 粉紫
  ["rgba(255, 197, 61, 0.8)", "rgba(255, 84, 44, 0.8)"], // 黄橙
  ["rgba(58, 97, 234, 0.8)", "rgba(138, 43, 226, 0.8)"], // 蓝紫
  ["rgba(14, 174, 87, 0.8)", "rgba(12, 116, 117, 0.8)"], // 绿青
  ["rgba(246, 79, 89, 0.8)", "rgba(196, 113, 245, 0.8)"], // 红紫
  ["rgba(25, 177, 246, 0.8)", "rgba(5, 110, 196, 0.8)"], // 亮蓝深蓝
  ["rgba(255, 153, 102, 0.8)", "rgba(255, 94, 98, 0.8)"] // 橙红
];

// 根据索引获取卡片主色
const primaryColor = computed(() => {
  const colorIndex = props.colorIndex >= 0 ? props.colorIndex : props.index;
  return colorSchemes[colorIndex % colorSchemes.length][0];
});

// 根据索引获取卡片辅助色
const secondaryColor = computed(() => {
  const colorIndex = props.colorIndex >= 0 ? props.colorIndex : props.index;
  return colorSchemes[colorIndex % colorSchemes.length][1];
});

// 格式化数字，保留一位小数
const formatNumber = (num: number): string => {
  if (typeof num !== "number" || isNaN(num)) {
    return "0.0";
  }
  return num.toFixed(1);
};
</script>

<style scoped>
/* 卡片基础样式 */
.floor-card {
  position: relative;
  border-radius: 12px; /* 圆角边框 */
  display: flex;
  flex-direction: column;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.2); /* 卡片阴影效果 */
  border: 1px solid rgba(255, 255, 255, 0.1); /* 边框样式 */
  backdrop-filter: blur(10px); /* 背景模糊效果 */
  transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1); /* 平滑过渡效果 */
  overflow: hidden;
  animation: fadeInUp 0.8s ease-out both; /* 淡入上升动画 */
  animation-delay: var(--animation-delay, 0s); /* 错开每个卡片的动画启动时间 */
  background: linear-gradient(
    to bottom,
    rgba(15, 35, 55, 0.95),
    rgba(6, 24, 44, 0.95)
  ); /* 背景渐变 */
  height: 100%;
  padding: 15px;
  width: 100%;
  min-width: 0;
}

/* 后台管理卡片样式 */
.admin-card {
  background: #fff !important;
  border: 1px solid #ebeef5 !important;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1) !important;
}

/* 卡片淡入上升动画 */
@keyframes fadeInUp {
  0% {
    opacity: 0;
    transform: translateY(20px); /* 开始时位置偏下且透明 */
  }
  100% {
    opacity: 1;
    transform: translateY(0); /* 结束时恢复到正常位置且完全不透明 */
  }
}

/* 卡片悬停和激活状态样式 */
.floor-card:hover,
.floor-card.is-active {
  transform: translateY(-3px); /* 上浮效果 */
  box-shadow:
    0 8px 20px rgba(0, 0, 0, 0.3),
    0 0 15px var(--primary-color, rgba(0, 201, 255, 0.3)); /* 增强阴影和发光效果 */
  border-color: var(
    --primary-color,
    rgba(0, 201, 255, 0.3)
  ); /* 边框颜色变为主题色 */
}

/* 后台模式卡片悬停效果 */
.admin-card:hover,
.admin-card.is-active {
  transform: translateY(-2px); /* 轻微上浮效果 */
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15) !important; /* 轻微增强阴影 */
  border-color: #409eff !important; /* 边框颜色变为主题色 */
}

/* 卡片顶部边框装饰 */
.floor-card::before {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 3px;
  background: linear-gradient(
    to right,
    var(--primary-color),
    var(--secondary-color)
  ); /* 顶部彩色渐变条 */
  box-shadow: 0 0 10px rgba(0, 201, 255, 0.5); /* 顶部发光效果 */
}

/* 卡片头部样式 */
.floor-card-header {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 15px;
}

/* 图标容器样式 */
.floor-icon-container {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* 标题区域样式 */
.floor-title {
  flex: 1;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

/* 楼层名称样式 */
.floor-name {
  font-size: 16px;
  font-weight: 500;
  color: #fff;
  text-shadow: 0 0 10px rgba(0, 0, 0, 0.5); /* 文字阴影 */
  background: linear-gradient(
    to right,
    var(--primary-color),
    var(--secondary-color)
  ); /* 文字渐变色 */
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent; /* 使文本透明，显示背景渐变 */
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: calc(100% - 30px);
}

/* 后台模式楼层名称 */
.admin-card .floor-name {
  color: #303133;
  text-shadow: none;
  background: none;
  -webkit-text-fill-color: #303133;
}

/* 楼层排名样式 */
.floor-rank {
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  font-weight: bold;
  color: #fff;
  background: linear-gradient(
    135deg,
    var(--primary-color, #00c9ff),
    var(--secondary-color, #00ff87)
  ); /* 排名背景渐变 */
  border-radius: 50%; /* 圆形 */
  box-shadow: 0 0 10px rgba(0, 201, 255, 0.3); /* 发光效果 */
}

/* 卡片内容区域 */
.floor-card-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 15px;
}

/* 进度条区域 */
.progress-section {
  margin-bottom: 15px;
}

/* 后台模式进度条区域 */
.admin-card .progress-section {
  margin: 15px 0;
}

/* 进度条容器 */
.progress-bar {
  position: relative;
  height: 6px;
  background: rgba(255, 255, 255, 0.1); /* 进度条背景 */
  border-radius: 3px;
  overflow: hidden;
  margin-bottom: 5px;
}

/* 后台模式进度条容器 */
.admin-card .progress-bar {
  height: 8px;
  background: #f0f2f5;
  border-radius: 4px;
  margin-bottom: 8px;
}

/* 进度条填充部分 */
.progress-fill {
  height: 100%;
  background: linear-gradient(
    to right,
    var(--primary-color),
    var(--secondary-color)
  ); /* 进度条渐变填充 */
  border-radius: 3px;
  transition: width 1s ease-out; /* 平滑过渡动画 */
}

/* 后台模式进度条填充部分 */
.admin-card .progress-fill {
  background: linear-gradient(to right, #409eff, #67c23a);
  border-radius: 4px;
}

/* 进度条发光效果 */
.progress-glow {
  position: absolute;
  top: 0;
  left: 0;
  height: 100%;
  transition: width 1s ease-out;
  opacity: 0.6;
}

/* 进度文字 */
.progress-text {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.7);
  text-align: right;
}

/* 后台模式进度文字 */
.admin-card .progress-text {
  color: #606266;
  font-size: 13px;
}

/* 进度值样式 */
.progress-value {
  color: #fff;
  font-weight: 500;
}

/* 后台模式进度值样式 */
.admin-card .progress-value {
  color: #409eff;
  font-weight: 600;
}

/* 数据网格布局 - 日/月/年用电量横排显示 */
.floor-data-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr); /* 三列等宽布局 */
  gap: 10px;
  margin-top: 8px;
}

/* 后台模式数据网格样式 */
.admin-card .floor-data-grid {
  gap: 12px;
  margin-top: 15px;
}

/* 数据项样式 */
.data-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.08);
  transition: all 0.3s ease;
  border: 1px solid rgba(255, 255, 255, 0.05);
}

/* 后台模式数据项样式 */
.admin-card .data-item {
  background: #f5f7fa;
  border: 1px solid #ebeef5;
  padding: 10px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.03);
}

/* 数据项悬停和激活状态 */
.data-item:hover,
.data-item.active {
  background: rgba(255, 255, 255, 0.12);
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}

/* 后台模式数据项悬停和激活状态 */
.admin-card .data-item:hover,
.admin-card .data-item.active {
  background: #ffffff;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  border-color: #d0d7de;
  transform: translateY(-2px);
}

.admin-card .data-item.active {
  border-color: #409eff;
  background: linear-gradient(to bottom, #ecf5ff, #ffffff);
}

/* 数据图标基础样式 */
.data-icon {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  font-weight: 600;
  flex-shrink: 0;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.2);
}

/* 后台模式数据图标 */
.admin-card .data-icon {
  width: 36px;
  height: 36px;
  font-size: 18px;
  background: #ffffff;
  border: 1px solid #ebeef5;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

/* 日用电图标样式 */
.data-icon.day {
  background: rgba(0, 201, 255, 0.2);
  color: #00c9ff;
}

.admin-card .data-icon.day {
  background: #ecf5ff;
  color: #409eff;
  box-shadow: none;
}

/* 月用电图标样式 */
.data-icon.month {
  background: rgba(0, 255, 135, 0.2);
  color: #00ff87;
}

.admin-card .data-icon.month {
  background: #f0f9eb;
  color: #67c23a;
  box-shadow: none;
}

/* 年用电图标样式 */
.data-icon.year {
  background: rgba(255, 197, 61, 0.2);
  color: #ffd33d;
}

.admin-card .data-icon.year {
  background: #fdf6ec;
  color: #e6a23c;
  box-shadow: none;
}

/* 数据内容区域样式 */
.data-content {
  flex: 1;
  min-width: 0;
  overflow: hidden;
}

/* 数据标签样式 */
.data-label {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.6);
  margin-bottom: 3px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  font-weight: 600;
}

/* 后台模式数据标签 */
.admin-card .data-label {
  color: #909399;
  font-weight: 600;
}

/* 数据值样式 */
.data-value {
  font-size: 16px;
  font-weight: 700;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  color: white;
  letter-spacing: 0.5px;
}

/* 后台模式数据值 */
.admin-card .data-value {
  color: #303133;
  font-size: 16px;
}

/* 数值动画效果 */
.animate-value {
  animation: pulseValue 2s infinite alternate;
  text-shadow: 0 0 8px rgba(255, 255, 255, 0.6);
  background: linear-gradient(45deg, #ffffff, #d0d0d0);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.admin-card .animate-value {
  animation: pulseValueAdmin 2s infinite alternate;
  background: linear-gradient(45deg, #303133, #606266);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.day-item .animate-value {
  background: linear-gradient(45deg, #ffffff, #00c9ff);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.month-item .animate-value {
  background: linear-gradient(45deg, #ffffff, #00ff87);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.year-item .animate-value {
  background: linear-gradient(45deg, #ffffff, #ffd33d);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.admin-card .day-item .animate-value {
  background: linear-gradient(45deg, #303133, #409eff);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.admin-card .month-item .animate-value {
  background: linear-gradient(45deg, #303133, #67c23a);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.admin-card .year-item .animate-value {
  background: linear-gradient(45deg, #303133, #e6a23c);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

/* 数值脉动动画定义 */
@keyframes pulseValue {
  0% {
    opacity: 0.9;
    text-shadow: 0 0 6px rgba(255, 255, 255, 0.4);
  }
  100% {
    opacity: 1;
    text-shadow: 0 0 12px var(--primary-color);
    transform: scale(1.05);
  }
}

@keyframes pulseValueAdmin {
  0% {
    opacity: 0.9;
    text-shadow: 0 0 2px rgba(0, 0, 0, 0.2);
  }
  100% {
    opacity: 1;
    text-shadow: 0 0 4px rgba(0, 0, 0, 0.1);
    transform: scale(1.03);
  }
}

/* 单位样式 */
.data-value .unit {
  font-size: 12px;
  font-weight: 400;
  opacity: 0.9;
  margin-left: 2px;
}

/* 装饰效果 */
.card-decorations {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
}

/* 卡片角落装饰基础样式 */
.card-corner {
  position: absolute;
  width: 6px;
  height: 6px;
  border: 1px solid var(--primary-color, rgba(0, 201, 255, 0.5));
}

.card-corner-tl {
  top: 6px;
  left: 6px;
  border-right: none;
  border-bottom: none;
}

.card-corner-tr {
  top: 6px;
  right: 6px;
  border-left: none;
  border-bottom: none;
}

.card-corner-bl {
  bottom: 6px;
  left: 6px;
  border-right: none;
  border-top: none;
}

.card-corner-br {
  bottom: 6px;
  right: 6px;
  border-left: none;
  border-top: none;
}

/* 响应式样式调整 */
@media (max-width: 1400px) {
  .floor-data-grid {
    gap: 8px;
  }

  .data-item {
    padding: 8px;
  }

  .data-icon {
    width: 28px;
    height: 28px;
    font-size: 13px;
  }

  .data-value {
    font-size: 15px;
  }
}

@media (max-width: 1200px) {
  .floor-data-grid {
    gap: 6px;
  }

  .data-item {
    padding: 7px;
  }

  .data-value {
    font-size: 14px;
  }

  .data-label {
    font-size: 11px;
  }

  .data-icon {
    width: 26px;
    height: 26px;
  }
}

/* 屏幕较小时，保持横向布局但调整大小 */
@media (max-width: 992px) {
  .floor-data-grid {
    grid-template-columns: repeat(3, 1fr);
    gap: 5px;
  }

  .data-item {
    padding: 6px;
  }

  .data-icon {
    width: 24px;
    height: 24px;
    font-size: 12px;
  }

  .data-label {
    font-size: 10px;
    margin-bottom: 2px;
  }

  .data-value {
    font-size: 13px;
  }

  .data-value .unit {
    font-size: 10px;
  }
}

/* 移动设备尺寸 */
@media (max-width: 768px) {
  .floor-data-grid {
    grid-template-columns: repeat(3, 1fr);
    gap: 5px;
  }

  .data-item {
    padding: 5px;
  }

  .data-icon {
    width: 22px;
    height: 22px;
    font-size: 10px;
  }

  .data-value {
    font-size: 12px;
  }

  .data-label {
    font-size: 9px;
  }

  .data-value .unit {
    font-size: 9px;
  }
}

/* 数据项特定类型样式 */
.day-item {
  border-left: 3px solid #00c9ff;
}

.month-item {
  border-left: 3px solid #00ff87;
}

.year-item {
  border-left: 3px solid #ffd33d;
}

.admin-card .day-item {
  border-left: 3px solid #409eff;
}

.admin-card .month-item {
  border-left: 3px solid #67c23a;
}

.admin-card .year-item {
  border-left: 3px solid #e6a23c;
}
</style>
