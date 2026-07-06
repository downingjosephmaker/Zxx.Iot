<script setup lang="ts">
import { ref, computed, watch, FunctionalComponent, SVGAttributes } from "vue";
import { ElMessage } from "element-plus";
import dayjs from "dayjs";
import { getDevEnergyDetail } from "@/api/monitor";
import type { DeviceSecondMonitor, ShowParam } from "@/api/monitor";
import BoltIcon from "~icons/material-symbols/bolt-outline";
import CounterIcon from "~icons/ic/round-format-list-numbered";
import WaterDropIcon from "~icons/mdi/water-drop";
import ClockIcon from "~icons/mdi/clock-outline";
import RoadIcon from "~icons/mdi/road-variant";
import AirconMeterIcon from "@/assets/img/db.png";
import WaterMeterIcon from "@/assets/img/sb.png";
import MachineIcon from "~icons/mdi/robot-industrial";
import ForkliftIcon from "@/assets/img/cc.png";
import FlowMachineIcon from "@/assets/img/lj.png";

defineOptions({
  name: "DeviceEnergyMonitorBase"
});

const props = defineProps({
  selectedDate: {
    type: String,
    default: () => dayjs().format("YYYY-MM-DD")
  },
  devtype: {
    type: Number,
    required: true
  },
  dark: {
    type: Boolean,
    default: false
  }
});

const loading = ref(false);
const monitorData = ref<DeviceSecondMonitor | null>(null);
const dayjsObj = dayjs;

const currentDate = computed(() =>
  dayjs(props.selectedDate).format("YYYY年MM月DD日")
);

const getStatIcon = (param: ShowParam) => {
  const paramName = param.ParamName?.toLowerCase() || "";
  const paramCode = param.ParamCode?.toLowerCase() || "";
  const unit = param.ValueUnit?.toLowerCase() || "";

  // Mileage-related params
  if (paramName.includes("里程")) {
    return RoadIcon;
  }

  // Duration-related params
  if (paramName.includes("时长")) {
    return ClockIcon;
  }

  // Water-related params
  if (
    paramName.includes("水") ||
    unit.includes("m³") ||
    unit.includes("t") ||
    paramCode.includes("water")
  ) {
    return WaterDropIcon;
  }

  // Count-related params
  if (
    paramName.includes("次") ||
    paramName.includes("count") ||
    paramCode.includes("count")
  ) {
    return CounterIcon;
  }

  // Default to energy for anything else, as it's the most common case
  return BoltIcon;
};

const iconColors = {
  d: { color: "#409eff", rgb: "64, 158, 255" },
  m: { color: "#e6a23c", rgb: "230, 162, 60" },
  y: { color: "#67c23a", rgb: "103, 194, 58" }
};

const fetchData = async () => {
  loading.value = true;
  monitorData.value = null;
  try {
    const response = await getDevEnergyDetail({
      targetdate: props.selectedDate,
      devtype: props.devtype
    });
    if (response && response.Result && typeof response.Result === "string") {
      monitorData.value = JSON.parse(response.Result);
    } else {
      ElMessage.info(response.Message || "返回数据格式不正确");
    }
  } catch (error) {
    ElMessage.error("获取监控数据失败");
    console.error(error);
  } finally {
    loading.value = false;
  }
};

watch(
  () => [props.selectedDate, props.devtype],
  () => {
    fetchData();
  },
  { immediate: true, deep: true }
);

defineExpose({
  fetchData
});

// 🔧 修改：过滤掉里程数相关的参数
const filterMileageParams = (params: ShowParam[]) => {
  return params.filter(param => {
    const paramName = param.ParamName?.toLowerCase() || "";
    return !paramName.includes("里程");
  });
};

const dTotalList = computed<ShowParam[]>(() =>
  filterMileageParams(monitorData.value?.DTotalList || [])
);
const mTotalList = computed<ShowParam[]>(() =>
  filterMileageParams(monitorData.value?.MTotalList || [])
);
const yTotalList = computed<ShowParam[]>(() =>
  filterMileageParams(monitorData.value?.YTotalList || [])
);

const getDeviceTypeProps = (deviceName: string) => {
  // For the Aircon Meter tab (devtype 2), all devices are considered aircon meters.
  if (props.devtype === 2) {
    return {
      type: "空调电表",
      color: "#409eff",
      icon: AirconMeterIcon,
      colorRgb: "64, 158, 255"
    };
  }

  // For the Water Meter tab (devtype 3), all devices are water meters.
  if (props.devtype === 3) {
    return {
      type: "水表",
      color: "#e6a23c",
      icon: WaterMeterIcon,
      colorRgb: "230, 162, 60"
    };
  }

  // For the Flow Machine tab (devtype 4), we can differentiate by name
  // as it might contain multiple machine types like forklifts.
  if (props.devtype === 4) {
    if (deviceName.includes("叉车")) {
      return {
        type: "叉车",
        color: "#f5b461",
        icon: ForkliftIcon,
        colorRgb: "245, 180, 97"
      };
    }
    // Default everything else on this tab to a Flow Machine.
    return {
      type: "电动流机",
      color: "#67c23a",
      icon: FlowMachineIcon,
      colorRgb: "103, 194, 58"
    };
  }

  // Fallback for any other devtype, though this shouldn't be reached.
  return {
    type: "其他设备",
    color: "#909399",
    icon: MachineIcon,
    colorRgb: "144, 147, 153"
  };
};

const processedDeviceData = computed(() => {
  if (!monitorData.value) return [];
  return monitorData.value.DeviceDetailList.map(device => {
    const params = new Map<
      string,
      {
        paramName: string;
        unit: string;
        values: { day: string; month: string; year: string };
      }
    >();
    const processList = (
      list: ShowParam[],
      period: "day" | "month" | "year"
    ) => {
      if (!list) return;
      // 🔧 修改：过滤掉里程数相关的参数
      const filteredList = filterMileageParams(list);
      filteredList.forEach(p => {
        if (!params.has(p.ParamCode)) {
          params.set(p.ParamCode, {
            paramName: p.ParamName,
            unit: p.ValueUnit,
            values: { day: "N/A", month: "N/A", year: "N/A" }
          });
        }
        const existingParam = params.get(p.ParamCode);
        if (existingParam) {
          existingParam.values[period] = p.ParamValue;
        }
      });
    };
    processList(device.DRealList, "day");
    processList(device.MRealList, "month");
    processList(device.YRealList, "year");
    return { ...device, processedParams: Array.from(params.values()) };
  });
});
</script>

<template>
  <div v-loading="loading" class="content-wrapper">
    <template v-if="monitorData && monitorData.DeviceDetailList">
      <!-- 总体数据 -->
      <div class="total-stats-grid">
        <div
          v-if="dTotalList.length > 0"
          class="stat-period-card"
          :class="{ 'is-dark': dark }"
          :style="{ 'animation-delay': '50ms' }"
        >
          <div class="stat-period-header">
            <div
              class="header-decorator"
              :style="{ '--decorator-color': iconColors.d.color }"
            />
            <h3 class="header-title">{{ currentDate }} 数据</h3>
          </div>
          <div class="stat-period-body">
            <div class="stat-data-grid">
              <div
                v-for="(stat, index) in dTotalList"
                :key="`d-${index}`"
                class="stat-data-item"
              >
                <div
                  class="stat-data-icon"
                  :style="{
                    '--icon-color': iconColors.d.color,
                    '--icon-color-rgb': iconColors.d.rgb
                  }"
                >
                  <component :is="getStatIcon(stat)" />
                </div>
                <div class="stat-data-info">
                  <div class="stat-data-name">{{ stat.ParamName }}</div>
                  <div class="stat-data-value">
                    <span>{{ stat.ParamValue }}</span>
                    <span class="stat-unit">{{ stat.ValueUnit }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div
          v-if="mTotalList.length > 0"
          class="stat-period-card"
          :class="{ 'is-dark': dark }"
          :style="{ 'animation-delay': '150ms' }"
        >
          <div class="stat-period-header">
            <div
              class="header-decorator"
              :style="{ '--decorator-color': iconColors.m.color }"
            />
            <h3 class="header-title">
              {{ dayjsObj(selectedDate).format("YYYY年MM月") }} 数据
            </h3>
          </div>
          <div class="stat-period-body">
            <div class="stat-data-grid">
              <div
                v-for="(stat, index) in mTotalList"
                :key="`m-${index}`"
                class="stat-data-item"
              >
                <div
                  class="stat-data-icon"
                  :style="{
                    '--icon-color': iconColors.m.color,
                    '--icon-color-rgb': iconColors.m.rgb
                  }"
                >
                  <component :is="getStatIcon(stat)" />
                </div>
                <div class="stat-data-info">
                  <div class="stat-data-name">{{ stat.ParamName }}</div>
                  <div class="stat-data-value">
                    <span>{{ stat.ParamValue }}</span>
                    <span class="stat-unit">{{ stat.ValueUnit }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div
          v-if="yTotalList.length > 0"
          class="stat-period-card"
          :class="{ 'is-dark': dark }"
          :style="{ 'animation-delay': '250ms' }"
        >
          <div class="stat-period-header">
            <div
              class="header-decorator"
              :style="{ '--decorator-color': iconColors.y.color }"
            />
            <h3 class="header-title">
              {{ dayjsObj(selectedDate).format("YYYY年") }} 数据
            </h3>
          </div>
          <div class="stat-period-body">
            <div class="stat-data-grid">
              <div
                v-for="(stat, index) in yTotalList"
                :key="`y-${index}`"
                class="stat-data-item"
              >
                <div
                  class="stat-data-icon"
                  :style="{
                    '--icon-color': iconColors.y.color,
                    '--icon-color-rgb': iconColors.y.rgb
                  }"
                >
                  <component :is="getStatIcon(stat)" />
                </div>
                <div class="stat-data-info">
                  <div class="stat-data-name">{{ stat.ParamName }}</div>
                  <div class="stat-data-value">
                    <span>{{ stat.ParamValue }}</span>
                    <span class="stat-unit">{{ stat.ValueUnit }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
      <!-- 设备详情 -->
      <div class="section-box device-details" :class="{ 'is-dark': dark }">
        <div class="section-header">
          <div class="title-icon" />
          <h2 style="color: #fff">设备详情</h2>
        </div>
        <el-row :gutter="20">
          <el-col
            v-for="(device, index) in processedDeviceData"
            :key="device.DeviceId"
            :xl="8"
            :lg="8"
            :md="12"
            :sm="24"
            :xs="24"
            class="device-col"
          >
            <div
              class="device-card"
              :class="{ 'is-dark': dark }"
              :style="{
                '--device-color': getDeviceTypeProps(device.DeviceName).color,
                '--device-color-rgb': (
                  getDeviceTypeProps(device.DeviceName) as any
                ).colorRgb,
                'animation-delay': `${index * 100 + 300}ms`
              }"
            >
              <div class="device-card-inner">
                <div class="device-card-header">
                  <span class="device-name">{{ device.DeviceName }}</span>
                  <div class="device-tags">
                    <el-tag
                      :color="getDeviceTypeProps(device.DeviceName).color"
                      effect="dark"
                      size="small"
                      >{{
                        (getDeviceTypeProps(device.DeviceName) as any).type
                      }}</el-tag
                    >
                  </div>
                </div>
                <div class="device-card-body">
                  <div class="device-icon-wrapper">
                    <component
                      :is="(getDeviceTypeProps(device.DeviceName) as any).icon"
                      v-if="
                        typeof (getDeviceTypeProps(device.DeviceName) as any)
                          .icon === 'object'
                      "
                      class="device-icon"
                    />
                    <img
                      v-else
                      :src="(getDeviceTypeProps(device.DeviceName) as any).icon"
                      alt="device icon"
                      class="device-icon is-image"
                    />
                  </div>
                  <div class="device-params-grid">
                    <div
                      v-for="(param, pIndex) in device.processedParams"
                      :key="pIndex"
                      class="param-group"
                    >
                      <div class="param-group-header">
                        <span>{{ param.paramName }}</span>
                        <span class="param-unit-header">{{ param.unit }}</span>
                      </div>
                      <div class="param-group-values">
                        <div class="param-value-item">
                          <span class="period">日</span>
                          <span class="value">{{ param.values.day }}</span>
                        </div>
                        <div class="param-value-item">
                          <span class="period">月</span>
                          <span class="value">{{ param.values.month }}</span>
                        </div>
                        <div class="param-value-item">
                          <span class="period">年</span>
                          <span class="value">{{ param.values.year }}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </el-col>
        </el-row>
      </div>
    </template>
    <el-empty v-else-if="!loading" description="暂无数据" />
  </div>
</template>

<style scoped>
/* Copied and adapted from ChargingPileMonitorBase, with minor adjustments */
.content-wrapper {
  display: flex;
  flex-direction: column;
  gap: 25px;
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

.section-box {
  background: rgba(255, 255, 255, 0.9);
  backdrop-filter: blur(10px);
  border-radius: 12px;
  padding: 25px;
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.05);
  border: 1px solid rgba(228, 231, 237, 0.8);
  transition:
    transform 0.3s,
    box-shadow 0.3s;
}
.section-box.is-dark {
  background: rgba(6, 24, 44, 0.8);
  border: 1px solid rgba(255, 255, 255, 0.08);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.2);
  backdrop-filter: blur(12px);
}
.section-box:hover {
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.07);
}
.section-header {
  display: flex;
  align-items: center;
  margin-bottom: 10px;
  position: relative;
  color: #fff;
}
.title-icon {
  width: 4px;
  height: 20px;
  background: linear-gradient(to bottom, #409eff, #67c23a);
  margin-right: 12px;
  border-radius: 4px;
  box-shadow: 0 0 10px rgba(64, 158, 255, 0.5);
}
.section-header h2 {
  font-size: 20px;
  font-weight: 500;
  color: #303133;
  margin: 0;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
}
.section-header.is-dark h2 {
  color: #fff;
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

/* 总体数据卡片样式增强 */
.total-stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 25px;
  margin-bottom: 10px;
  position: relative;
}

.stat-period-card {
  background: linear-gradient(145deg, #ffffff 0%, #f7f9fc 100%);
  border-radius: 16px;
  border: 1px solid #e9edf2;
  overflow: hidden;
  transition:
    transform 0.35s cubic-bezier(0.215, 0.61, 0.355, 1),
    box-shadow 0.35s cubic-bezier(0.215, 0.61, 0.355, 1);
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.05);
  position: relative;
  opacity: 0;
  animation: card-entry 0.7s cubic-bezier(0.23, 1, 0.32, 1) forwards;
}
.stat-period-card.is-dark {
  background: rgba(6, 24, 44, 0.8);
  border: 1px solid rgba(255, 255, 255, 0.08);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.2);
  backdrop-filter: blur(12px);
}
.stat-period-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 12px 28px rgba(0, 0, 0, 0.08);
}
.stat-period-card::before {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 4px;
  background: var(--decorator-color, #409eff);
  opacity: 0.9;
  box-shadow: 0 0 15px var(--decorator-color, #409eff);
}
.stat-period-card::after {
  content: "";
  position: absolute;
  inset: 0;
  background: radial-gradient(
    circle at 10% 10%,
    rgba(255, 255, 255, 0.7) 0%,
    rgba(255, 255, 255, 0) 30%
  );
  pointer-events: none;
}
.stat-period-card.is-dark .stat-period-header {
  background-color: rgba(255, 255, 255, 0.05);
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}
.stat-period-card.is-dark .header-title {
  color: #fff;
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
}
.stat-period-card.is-dark .stat-data-icon {
  background: linear-gradient(
    135deg,
    rgba(255, 255, 255, 0.1),
    rgba(0, 0, 0, 0.2)
  );
  border: 2px solid rgba(var(--icon-color-rgb), 0.4);
  box-shadow:
    0 0 15px rgba(var(--icon-color-rgb), 0.3),
    inset 0 2px 4px rgba(0, 0, 0, 0.2);
}
.stat-period-card.is-dark .stat-data-name {
  color: rgba(255, 255, 255, 0.7);
}
.stat-period-card.is-dark .stat-data-value {
  background: linear-gradient(45deg, #ffffff, #d0d0d0);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
  text-shadow: 0 2px 10px rgba(0, 0, 0, 0.2);
}
.stat-period-card.is-dark .stat-data-value .stat-unit {
  color: rgba(255, 255, 255, 0.7);
  -webkit-text-fill-color: rgba(255, 255, 255, 0.7);
}
.stat-period-header {
  display: flex;
  align-items: center;
  padding: 16px 22px;
  background-color: rgba(255, 255, 255, 0.6);
  border-bottom: 1px solid #f0f2f5;
  position: relative;
  z-index: 1;
}
.header-decorator {
  width: 4px;
  height: 18px;
  border-radius: 2px;
  background-color: var(--decorator-color);
  margin-right: 12px;
  box-shadow: 0 0 8px var(--decorator-color);
}
.header-title {
  font-size: 17px;
  font-weight: 600;
  color: #3d4a5e;
  margin: 0;
  text-shadow: 0 1px 1px rgba(255, 255, 255, 0.8);
}
.stat-period-body {
  padding: 15px 20px;
}
.stat-data-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(130px, 1fr));
  gap: 15px;
  width: 100%;
}
.stat-data-item {
  display: flex;
  align-items: center;
  padding: 10px;
  background-color: transparent;
  border-radius: 10px;
  transition:
    transform 0.25s ease,
    background-color 0.25s ease;
  border: 1px solid transparent;
  animation: fadeInUp 0.5s ease backwards;
}
.stat-data-item:hover {
  transform: translateY(-4px);
  background-color: #ffffff;
  border-color: #eef1f5;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.04);
}
.stat-period-card.is-dark .stat-data-item:hover {
  background-color: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.15);
}
.stat-data-item:hover .stat-data-icon {
  background: var(--icon-color, #409eff);
  color: #fff;
  transform: rotate(-15deg) scale(1.1);
  box-shadow:
    0 8px 15px rgba(var(--icon-color-rgb), 0.5),
    0 0 0 4px rgba(var(--icon-color-rgb), 0.2);
  border-color: transparent;
}

/* 设备卡片样式增强 */
.device-details {
  margin-top: 0;
}
.device-col {
  margin-bottom: 25px;
}
.device-card {
  background: #fff;
  border-radius: 12px;
  border: 1px solid #e4e7ed;
  transition:
    box-shadow 0.4s ease,
    transform 0.4s ease;
  overflow: hidden;
  position: relative;
  border-top: 4px solid var(--device-color, #409eff);
  height: 100%;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.03);
  opacity: 0;
  animation: card-entry 0.7s cubic-bezier(0.23, 1, 0.32, 1) forwards;
}
.device-card.is-dark {
  background: rgba(6, 24, 44, 0.8);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-top: 4px solid var(--device-color, #409eff);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.2);
  backdrop-filter: blur(12px);
}
.device-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 12px 28px rgba(0, 0, 0, 0.1);
}
.device-card.is-dark:hover {
  transform: translateY(-8px) scale(1.02);
  box-shadow:
    0 0 25px -5px rgba(var(--device-color-rgb), 0.6),
    0 15px 30px -10px rgba(0, 0, 0, 0.4);
  border-color: rgba(var(--device-color-rgb), 0.7);
  border-top-color: rgba(var(--device-color-rgb), 0.7);
}
.device-card::after {
  content: "";
  position: absolute;
  bottom: 0;
  left: 0;
  width: 100%;
  height: 4px;
  background: linear-gradient(
    to right,
    var(--device-color, #409eff),
    transparent
  );
  opacity: 0;
  transition: opacity 0.3s;
}
.device-card:hover::after {
  opacity: 1;
}
.device-card-inner {
  display: flex;
  flex-direction: column;
  padding: 20px;
  height: 100%;
  background-image: linear-gradient(
    to bottom right,
    rgba(255, 255, 255, 0.95),
    rgba(248, 250, 252, 0.92)
  );
  position: relative;
  z-index: 1;
}
.device-card.is-dark .device-card-inner {
  background-image: none;
}
.device-card-inner::before {
  content: "";
  position: absolute;
  top: -10%;
  left: -10%;
  width: 120%;
  height: 120%;
  background: radial-gradient(
    circle at top left,
    rgba(var(--device-color-rgb, 64, 158, 255), 0.03),
    transparent 70%
  );
  z-index: -1;
  opacity: 0.8;
  transition: opacity 0.4s;
  pointer-events: none;
}
.device-card:hover .device-card-inner::before {
  opacity: 1;
}
.device-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  position: relative;
  padding-bottom: 10px;
}
.device-card.is-dark .device-card-header::after {
  background: linear-gradient(
    to right,
    rgba(255, 255, 255, 0.2),
    rgba(255, 255, 255, 0.1),
    transparent 80%
  );
}
.device-card-header::after {
  content: "";
  position: absolute;
  left: 0;
  bottom: 0;
  width: 100%;
  height: 1px;
  background: linear-gradient(
    to right,
    rgba(var(--device-color-rgb), 0.3),
    rgba(var(--device-color-rgb), 0.1),
    transparent 80%
  );
}
.device-name {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  text-shadow: 0px 0px 1px rgba(0, 0, 0, 0.05);
  letter-spacing: 0.5px;
}
.device-card.is-dark .device-name {
  color: #fff;
}
.device-tags {
  display: flex;
  gap: 8px;
  flex-shrink: 0;
}
.device-tags :deep(.el-tag) {
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
  border: none;
  padding: 0 8px;
  height: 20px;
  line-height: 20px;
  font-weight: 500;
  letter-spacing: 0.5px;
  transform: translateY(0);
  transition:
    transform 0.3s,
    box-shadow 0.3s;
}
.device-card.is-dark .device-tags :deep(.el-tag) {
  background-color: rgba(255, 255, 255, 0.1) !important;
  color: #fff !important;
  border: 1px solid rgba(255, 255, 255, 0.2) !important;
}
.device-tags :deep(.el-tag):hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}
.device-card-body {
  display: flex;
  align-items: center;
  flex-grow: 1;
  margin-top: 16px;
  gap: 20px;
}
.device-icon-wrapper {
  margin-right: 0;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 120px;
  height: 165px;
  position: relative;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.12);
  transform: perspective(800px) rotateY(0deg);
  transition: all 0.5s ease;
  z-index: 1;
}
.device-card.is-dark .device-icon-wrapper {
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.25);
  filter: drop-shadow(0 0 8px rgba(var(--device-color-rgb), 0.4));
}
.device-icon-wrapper::before {
  content: "";
  position: absolute;
  inset: 0;
  background: linear-gradient(
    135deg,
    rgba(255, 255, 255, 0.4) 0%,
    rgba(255, 255, 255, 0) 100%
  );
  z-index: 2;
}
.device-icon-wrapper::after {
  content: "";
  position: absolute;
  bottom: 0;
  left: 0;
  width: 100%;
  height: 5px;
  background: var(--device-color, #409eff);
  filter: blur(2px);
  opacity: 0.7;
  z-index: 3;
}
.device-icon {
  width: 100%;
  height: 100%;
  object-fit: cover;
  object-position: center;
  display: block;
  transition: all 0.5s ease;
  filter: contrast(1.05) saturate(1.1);
  font-size: 80px;
  color: var(--device-color);
}
.device-icon.is-image {
  object-fit: contain;
  font-size: initial;
  color: initial;
}
.device-card:hover .device-icon-wrapper {
  box-shadow: 0 14px 30px rgba(0, 0, 0, 0.18);
  transform: perspective(800px) rotateY(5deg) translateY(-6px);
}
.device-card.is-dark:hover .device-icon-wrapper {
  box-shadow: 0 14px 30px rgba(0, 0, 0, 0.3);
  transform: perspective(800px) rotateY(0deg) translateY(-6px) scale(1.05);
  filter: drop-shadow(0 0 15px rgba(var(--device-color-rgb), 0.7));
}
.device-content-wrapper {
  flex-grow: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}
.device-params-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  flex-grow: 1;
  align-content: flex-start;
}
.param-group {
  background-color: transparent;
  border: 1px solid #f0f2f5;
  border-radius: 10px;
  padding: 12px;
  transition: all 0.3s ease;
  flex: 1 1 calc(50% - 6px);
  min-width: 140px;
}
.device-card.is-dark .param-group {
  border: 1px solid rgba(255, 255, 255, 0.15);
}
.param-group:hover {
  background-color: #fff;
  transform: none;
  box-shadow: 0 6px 15px rgba(0, 0, 0, 0.07);
  border-color: #e8ebf0;
}
.device-card.is-dark .param-group:hover {
  background-color: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.2);
}
.param-group-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-weight: 500;
  color: #495057;
  margin-bottom: 8px;
  font-size: 14px;
  padding-bottom: 8px;
  border-bottom: 1px dashed #e9ecef;
}
.device-card.is-dark .param-group-header {
  color: rgba(255, 255, 255, 0.85);
  border-bottom: 1px dashed rgba(255, 255, 255, 0.2);
}
.param-unit-header {
  font-size: 13px;
  color: #868e96;
  font-style: italic;
}
.device-card.is-dark .param-unit-header {
  color: rgba(255, 255, 255, 0.7);
}
.param-group-values {
  display: flex;
  flex-direction: column;
  gap: 5px;
  margin-top: 8px;
}
.param-value-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 4px 8px;
  border-radius: 6px;
  transition: background-color 0.25s ease;
}
.param-value-item:hover {
  background-color: rgba(var(--device-color-rgb), 0.08);
}
.device-card.is-dark .param-value-item:hover {
  background-color: rgba(255, 255, 255, 0.08);
}
.param-value-item .period {
  font-size: 13px;
  color: #6c757d;
  font-weight: 500;
}
.device-card.is-dark .param-value-item .period {
  color: rgba(255, 255, 255, 0.7);
}
.param-value-item .value {
  font-size: 15px;
  font-weight: 600;
  color: #343a40;
}
.device-card.is-dark .param-value-item .value {
  color: #fff;
}
@keyframes card-entry {
  from {
    opacity: 0;
    transform: translateY(40px) scale(0.95);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (max-width: 1200px) {
  .total-stats-grid {
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  }
  .stat-data-grid {
    grid-template-columns: 1fr;
  }
}
@media (max-width: 992px) {
  .device-col {
    width: 100%;
    flex: 0 0 100%;
  }
}

.stat-data-item + .stat-data-item {
  border-top: none;
}

.stat-data-icon {
  font-size: 26px;
  color: var(--icon-color, #409eff);
  margin-right: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 52px;
  height: 52px;
  background: linear-gradient(135deg, #ffffff, #f0f4f9);
  border-radius: 50%;
  transition: all 0.3s ease;
  flex-shrink: 0;
  border: 2px solid rgba(var(--icon-color-rgb), 0.3);
  box-shadow:
    inset 0 2px 4px rgba(0, 0, 0, 0.02),
    0 6px 12px rgba(var(--icon-color-rgb), 0.25);
  position: relative;
  overflow: hidden;
}

.stat-data-icon::before {
  content: "";
  position: absolute;
  inset: 0;
  background: radial-gradient(
    circle at 30% 30%,
    rgba(var(--icon-color-rgb), 0.1),
    transparent 70%
  );
  z-index: 0;
}

.stat-data-icon > * {
  position: relative;
  z-index: 1;
}

.stat-data-info {
  flex-grow: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.stat-data-name {
  font-size: 14px;
  font-weight: 500;
  color: #6a7689;
  margin-bottom: 5px;
}

.stat-data-value {
  font-size: 22px;
  font-weight: bold;
  color: #303133;
  transition: transform 0.3s ease;
}

.stat-data-item:hover .stat-data-value {
  transform: scale(1.05);
}

.stat-data-value .stat-unit {
  font-size: 12px;
  font-weight: normal;
  margin-left: 4px;
  color: #909399;
}
</style>
