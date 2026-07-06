<script setup lang="ts">
import { ref, onMounted, watch, computed } from "vue";
import dayjs from "dayjs";
import DetailLayout from "./DetailLayout.vue";
import DeviceEnergyMonitorBase from "@/components/DeviceEnergyMonitorBase/index.vue";
import { useRoute } from "vue-router";

// 导入图标
import AirconIcon from "~icons/mdi/air-conditioner";
import WaterIcon from "~icons/mdi/water";
import MachineIcon from "~icons/mdi/robot-industrial";

defineOptions({
  name: "DeviceEnergyMonitorCockpit"
});

const route = useRoute();
const title = route.meta?.title ?? "设备能耗监测";

const selectedDate = ref(dayjs().format("YYYY-MM-DD"));
const activeTab = ref(2); // Default to "空调电表"
const showAnimation = ref(false);

// 设备类型配置
const deviceTypes = [
  { id: 2, name: "空调电表", icon: AirconIcon, color: "#409eff" },
  { id: 3, name: "水表", icon: WaterIcon, color: "#e6a23c" },
  { id: 4, name: "电动流机", icon: MachineIcon, color: "#67c23a" }
];

// 获取当前激活的设备类型信息
const activeDeviceType = computed(() => {
  return (
    deviceTypes.find(type => type.id === activeTab.value) || deviceTypes[0]
  );
});

// 格式化日期显示
const formattedDate = computed(() => {
  return dayjs(selectedDate.value).format("YYYY年MM月DD日");
});

onMounted(() => {
  const devtypeFromQuery = Number(route.query.devtype);
  if (devtypeFromQuery && [2, 3, 4].includes(devtypeFromQuery)) {
    activeTab.value = devtypeFromQuery;
  }

  // 添加进场动画
  setTimeout(() => {
    showAnimation.value = true;
  }, 100);
});

// 监听标签切换，添加过渡效果
watch(activeTab, () => {
  showAnimation.value = false;
  setTimeout(() => {
    showAnimation.value = true;
  }, 300);
});
</script>

<template>
  <DetailLayout :title="title">
    <template #header-controls>
      <div class="date-selector-wrapper">
        <div class="date-display">
          <span class="date-label">监测日期:</span>
          <span class="date-value">{{ formattedDate }}</span>
        </div>
        <el-date-picker
          v-model="selectedDate"
          type="date"
          placeholder="选择日期"
          format="YYYY/MM/DD"
          value-format="YYYY-MM-DD"
          :clearable="false"
          class="custom-date-picker"
        />
      </div>
    </template>

    <div class="cockpit-content">
      <!-- 设备类型选择器 -->
      <div class="device-tabs-container">
        <div class="device-tabs">
          <div
            v-for="device in deviceTypes"
            :key="device.id"
            class="device-tab"
            :class="{ active: activeTab === device.id }"
            :style="{ '--tab-color': device.color }"
            @click="activeTab = device.id"
          >
            <div class="tab-icon">
              <el-icon><component :is="device.icon" /></el-icon>
            </div>
            <span class="tab-name">{{ device.name }}</span>
          </div>
        </div>
      </div>

      <!-- 内容区域 -->
      <div class="tab-content-container">
        <div
          class="tab-content"
          :class="{ 'show-animation': showAnimation }"
          :style="{ '--active-color': activeDeviceType.color }"
        >
          <!-- <div class="content-header">
            <div class="header-icon">
              <el-icon><component :is="activeDeviceType.icon" /></el-icon>
            </div>
            <h2 class="header-title">{{ activeDeviceType.name }}监测数据</h2>
          </div> -->

          <div class="content-body">
            <DeviceEnergyMonitorBase
              :key="activeTab"
              :selected-date="selectedDate"
              :devtype="activeTab"
              :dark="true"
            />
          </div>
        </div>
      </div>
    </div>
  </DetailLayout>
</template>

<style lang="scss" scoped>
.cockpit-content {
  padding: 0 10px;
  height: 100%;
  display: flex;
  flex-direction: column;
  animation: fadeIn 0.5s ease-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

.cockpit-tabs {
  flex-shrink: 0;
  margin-bottom: 20px;
  position: relative;
  z-index: 1;

  &::after {
    content: "";
    position: absolute;
    bottom: 0;
    left: 0;
    width: 100%;
    height: 1px;
    background: linear-gradient(
      90deg,
      rgba(255, 255, 255, 0.4),
      rgba(255, 255, 255, 0.1),
      transparent
    );
    z-index: 0;
  }

  :deep(.el-tabs__header) {
    margin: 0;
    border-bottom: 1px solid rgba(255, 255, 255, 0.2);
    position: relative;
    overflow: hidden;

    &::before {
      content: "";
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.2);
      border-radius: 8px;
      z-index: -1;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      backdrop-filter: blur(5px);
    }
  }

  :deep(.el-tabs__nav) {
    border: none;
  }

  :deep(.el-tabs__item) {
    color: rgba(255, 255, 255, 0.7);
    font-size: 16px;
    padding: 0 20px;
    border: none;
    transition:
      color 0.3s,
      text-shadow 0.3s,
      background-color 0.3s;
    position: relative;
    overflow: hidden;
    margin: 0 4px;

    &::before {
      content: "";
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: linear-gradient(
        135deg,
        rgba(255, 255, 255, 0.1),
        transparent
      );
      opacity: 0;
      transition: opacity 0.3s;
    }

    &:hover {
      color: #fff;
      text-shadow: 0 0 10px rgba(64, 158, 255, 0.5);

      &::before {
        opacity: 1;
      }
    }
  }

  :deep(.el-tabs__item.is-active) {
    color: #409eff;
    text-shadow: 0 0 10px rgba(64, 158, 255, 0.8);
    background-color: rgba(64, 158, 255, 0.1);
    border-bottom: 2px solid #409eff;

    &::after {
      content: "";
      position: absolute;
      bottom: 0;
      left: 0;
      width: 100%;
      height: 2px;
      background: linear-gradient(90deg, transparent, #409eff, transparent);
      animation: glowingLine 2s infinite;
    }
  }

  :deep(.el-tabs__active-bar) {
    display: none;
  }
}

@keyframes glowingLine {
  0%,
  100% {
    box-shadow: 0 0 5px rgba(64, 158, 255, 0.5);
  }
  50% {
    box-shadow: 0 0 15px rgba(64, 158, 255, 0.8);
  }
}

.tab-content {
  flex-grow: 1;
  overflow-y: auto;
  border-radius: 12px;
  background: rgba(0, 0, 0, 0.2);
  backdrop-filter: blur(8px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
  border: 1px solid rgba(255, 255, 255, 0.05);
  padding: 0;
  position: relative;
  animation: slideUp 0.5s ease-out;

  &::before {
    content: "";
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background:
      radial-gradient(
        circle at 20% 30%,
        rgba(64, 158, 255, 0.1),
        transparent 70%
      ),
      radial-gradient(
        circle at 80% 70%,
        rgba(103, 194, 58, 0.1),
        transparent 70%
      );
    pointer-events: none;
    z-index: 0;
  }

  // Transparent scrollbar for dark theme
  &::-webkit-scrollbar {
    width: 6px;
  }
  &::-webkit-scrollbar-track {
    background: transparent;
  }
  &::-webkit-scrollbar-thumb {
    background: rgba(255, 255, 255, 0.2);
    border-radius: 3px;

    &:hover {
      background: rgba(255, 255, 255, 0.3);
    }
  }
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

// 日期选择器样式
.date-selector-wrapper {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgba(0, 0, 0, 0.2);
  border-radius: 12px;
  padding: 4px 10px;
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
}

.date-display {
  display: flex;
  align-items: center;
  gap: 8px;
}

.date-label {
  color: rgba(255, 255, 255, 0.6);
  font-size: 14px;
}

.date-value {
  color: #fff;
  font-size: 16px;
  font-weight: 500;
}

:deep(.custom-date-picker) {
  .el-input__wrapper {
    background: rgba(0, 0, 0, 0.2);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow: none !important;
    &.is-focus {
      border-color: var(--el-color-primary);
    }

    .el-input__inner {
      color: #fff;
    }
  }

  .el-input__prefix-inner {
    color: rgba(255, 255, 255, 0.7);
  }
}

// 设备类型选择器样式
.device-tabs-container {
  margin-bottom: 8px;
}

.device-tabs {
  display: flex;
  gap: 8px;
  padding: 5px;
  border-radius: 12px;
  background: rgba(0, 0, 0, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.05);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
}

.device-tab {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  overflow: hidden;
  color: rgba(255, 255, 255, 0.7);

  &:hover {
    background: rgba(255, 255, 255, 0.05);
    color: #fff;
  }

  &.active {
    background: linear-gradient(
      135deg,
      rgba(var(--tab-color-rgb), 0.2) 0%,
      rgba(var(--tab-color-rgb), 0.1) 100%
    );
    color: var(--tab-color);
    box-shadow: 0 0 15px rgba(var(--tab-color-rgb), 0.3);

    &::after {
      content: "";
      position: absolute;
      bottom: 0;
      left: 0;
      width: 100%;
      height: 3px;
      background: var(--tab-color);
      box-shadow: 0 0 10px var(--tab-color);
    }

    .tab-icon {
      background: var(--tab-color);
      color: #fff;
      box-shadow: 0 0 10px rgba(var(--tab-color-rgb), 0.5);
    }
  }

  .tab-icon {
    width: 28px;
    height: 28px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 6px;
    background: rgba(255, 255, 255, 0.1);
    transition: all 0.3s ease;
    font-size: 16px;
  }

  .tab-name {
    font-size: 15px;
    font-weight: 500;
    letter-spacing: 0.5px;
  }
}

// 添加CSS变量转换
.device-tab {
  --tab-color-rgb: v-bind(
    'activeTab === 2 ? "64, 158, 255" : activeTab === 3 ? "230, 162, 60" : "103, 194, 58"'
  );
}

.tab-content {
  --active-color-rgb: v-bind(
    'activeTab === 2 ? "64, 158, 255" : activeTab === 3 ? "230, 162, 60" : "103, 194, 58"'
  );
}

.content-header {
  display: flex;
  align-items: center;
  gap: 15px;
  padding: 15px 25px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  position: relative;
  z-index: 1;
  overflow: hidden;

  &::before {
    content: "";
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(
      90deg,
      rgba(var(--active-color-rgb), 0.3) 0%,
      rgba(var(--active-color-rgb), 0.1) 40%,
      rgba(0, 0, 0, 0.2) 100%
    );
    z-index: -1;
  }

  &::after {
    content: "";
    position: absolute;
    top: -50%;
    left: -50%;
    width: 200%;
    height: 200%;
    background: radial-gradient(
      circle at 30% 50%,
      rgba(var(--active-color-rgb), 0.2),
      transparent 50%
    );
    opacity: 0.7;
    z-index: -2;
  }

  .header-icon {
    width: 48px;
    height: 48px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 12px;
    background: linear-gradient(
      135deg,
      var(--active-color) 0%,
      rgba(var(--active-color-rgb), 0.7) 100%
    );
    color: #fff;
    font-size: 24px;
    box-shadow:
      0 0 20px rgba(var(--active-color-rgb), 0.4),
      inset 0 0 10px rgba(255, 255, 255, 0.3);
    position: relative;
    overflow: hidden;
    animation: pulse 2s infinite ease-in-out;

    &::before {
      content: "";
      position: absolute;
      top: -10px;
      left: -10px;
      right: -10px;
      bottom: -10px;
      background: linear-gradient(
        135deg,
        rgba(255, 255, 255, 0.4),
        transparent 60%
      );
      transform: rotate(35deg) translateX(-60px);
      z-index: 0;
    }

    :deep(svg) {
      filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.3));
      position: relative;
      z-index: 1;
    }
  }

  .header-title {
    font-size: 20px;
    font-weight: 600;
    color: #fff;
    margin: 0;
    letter-spacing: 0.5px;
    text-shadow:
      0 0 10px rgba(var(--active-color-rgb), 0.5),
      0 2px 4px rgba(0, 0, 0, 0.3);
    position: relative;

    &::after {
      content: "";
      position: absolute;
      bottom: -5px;
      left: 0;
      width: 40px;
      height: 2px;
      background: var(--active-color);
      box-shadow: 0 0 10px rgba(var(--active-color-rgb), 0.5);
      border-radius: 2px;
    }
  }
}

.content-body {
  padding: 10px;
  position: relative;
  z-index: 1;
}

@keyframes pulse {
  0%,
  100% {
    box-shadow:
      0 0 20px rgba(var(--active-color-rgb), 0.4),
      inset 0 0 10px rgba(255, 255, 255, 0.3);
  }
  50% {
    box-shadow:
      0 0 30px rgba(var(--active-color-rgb), 0.6),
      inset 0 0 15px rgba(255, 255, 255, 0.5);
  }
}
</style>
