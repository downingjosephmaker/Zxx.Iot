<script setup>
import { ref, onMounted, onUnmounted, nextTick, reactive } from "vue";

// 组件名称
defineOptions({
  name: "EnergyDashboard"
});

// 响应式数据
const currentTime = ref("");
const weatherTemp = ref("18.9°C");

// 充电桩数据
const chargerData = reactive([
  {
    id: "桩#01",
    status: "充电中",
    power: "45.2kW",
    current: "67.8A",
    statusClass: "charging"
  },
  {
    id: "桩#02",
    status: "使用中",
    power: "38.9kW",
    current: "58.2A",
    statusClass: "in-use"
  },
  {
    id: "桩#03",
    status: "故障",
    power: "0kW",
    current: "0A",
    statusClass: "idle"
  },
  {
    id: "桩#04",
    status: "空闲",
    power: "0kW",
    current: "0A",
    statusClass: "idle"
  }
]);

// 告警数据
const alertData = reactive([
  {
    title: "充电桩#03通信故障",
    type: "critical",
    time: "2分钟前",
    description: "充电桩设备离线，请及时检查网络连接"
  },
  {
    title: "仓库B区用电异常",
    type: "warning",
    time: "15分钟前",
    description: "用电量超出正常范围，当前功率458kW"
  },
  {
    title: "光伏系统清洁提醒",
    type: "info",
    time: "1小时前",
    description: "建议进行光伏板清洁维护，提高发电效率"
  }
]);

// 储能系统数据
const batterySystem = reactive({
  level: 78,
  chargePower: "125.6 kW",
  dischargePower: "89.2 kW",
  timeRemaining: "4h 32m",
  temperature: "32.5°C"
});

// 设备状态数据
const deviceStatus = reactive([
  {
    name: "仓库A区",
    icon: "warehouse",
    status: "正常",
    color: "#00ff87",
    bgColor: "#00ff87"
  },
  {
    name: "仓库B区",
    icon: "warehouse",
    status: "异常",
    color: "#ffa726",
    bgColor: "#ffa726"
  },
  {
    name: "光伏系统",
    icon: "solar-panel",
    status: "运行中",
    color: "#00a2ff",
    bgColor: "#00a2ff"
  },
  {
    name: "储能系统",
    icon: "battery-three-quarters",
    status: "充电中",
    color: "#4fc3f7",
    bgColor: "#4fc3f7"
  }
]);

// 图表数据和实例
const chartInstances = reactive({
  energyChart: null,
  warehouseChart: null,
  solarChart: null
});

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
  water: false
});

// 方法：切换模块展开/收起状态
const toggleModule = moduleId => {
  const key = moduleId.replace("Module", "");
  if (moduleCollapsed.hasOwnProperty(key)) {
    moduleCollapsed[key] = !moduleCollapsed[key];

    // 模块收缩后需要重新调整图表大小
    nextTick(() => {
      // 这里如果使用ECharts等图表库，应该调用resize方法
      if (chartInstances[key + "Chart"]) {
        // chartInstances[key + "Chart"].resize();
        console.log(`Resizing ${key} chart`);
      }
    });
  }
};

// 模拟图表初始化
const initializeCharts = () => {
  // 这里应该是实际的图表初始化代码
  console.log("Charts initialized");
};

// 设置定时器并在组件卸载时清除
let timeTimer = null;

onMounted(() => {
  // 定时更新日期时间
  updateTime();
  timeTimer = setInterval(updateTime, 1000);

  // 添加动画延迟
  nextTick(() => {
    const elements = document.querySelectorAll(
      ".fade-in, .slide-in-left, .slide-in-right"
    );
    elements.forEach((el, index) => {
      el.style.animationDelay = index * 0.1 + "s";
    });

    // 初始化图表
    initializeCharts();
  });
});

onUnmounted(() => {
  // 清除定时器
  if (timeTimer) clearInterval(timeTimer);

  // 清除图表实例
  for (const key in chartInstances) {
    if (chartInstances[key]) {
      // 如果使用ECharts等图表库，应该调用dispose方法
      // chartInstances[key].dispose();
      console.log(`Disposing ${key}`);
    }
  }
});

// 处理告警点击
const handleAlertClick = alert => {
  console.log("告警点击：", alert.title);
};
</script>

<template>
  <div class="main-container">
    <!-- 顶部导航栏 -->
    <nav class="navbar">
      <div class="nav-brand">
        <div class="logo">
          <i class="fas fa-bolt" />
        </div>
        <h1 class="nav-title">嘉兴港野猫墩物流园区能源监管驾驶舱</h1>
      </div>
      <div class="nav-actions">
        <div class="time-display">
          <i class="fas fa-clock" />
          <span id="current-time">{{ currentTime }}</span>
        </div>
        <div class="weather-info">
          <i class="fas fa-sun" />
          <span>{{ weatherTemp }}</span>
        </div>
      </div>
    </nav>

    <!-- 主要指标卡片 -->
    <div class="indicators fade-in">
      <div class="indicator-card energy-gen">
        <div class="indicator-header">
          <div class="indicator-icon">
            <i class="fas fa-solar-panel" />
          </div>
          <div class="indicator-title">今日发电量</div>
        </div>
        <div class="indicator-value">1,245<small>kWh</small></div>
        <div class="indicator-change">
          <i class="fas fa-arrow-up" style="color: #00ff87" />
          <span style="color: #00ff87">12%</span>
          <span style="color: rgba(255, 255, 255, 0.6)">同比昨日</span>
        </div>
        <div class="data-flow" />
      </div>

      <div class="indicator-card energy-con">
        <div class="indicator-header">
          <div class="indicator-icon">
            <i class="fas fa-plug" />
          </div>
          <div class="indicator-title">今日用电量</div>
        </div>
        <div class="indicator-value">3,876<small>kWh</small></div>
        <div class="indicator-change">
          <i class="fas fa-arrow-up" style="color: #ff6b6b" />
          <span style="color: #ff6b6b">8%</span>
          <span style="color: rgba(255, 255, 255, 0.6)">同比昨日</span>
        </div>
        <div class="data-flow" />
      </div>

      <div class="indicator-card energy-diff">
        <div class="indicator-header">
          <div class="indicator-icon">
            <i class="fas fa-balance-scale" />
          </div>
          <div class="indicator-title">差值电量</div>
        </div>
        <div class="indicator-value">-2,631<small>kWh</small></div>
        <div class="indicator-change">
          <span style="color: rgba(255, 255, 255, 0.6)">自给率</span>
          <span style="color: #ffa726">32.1%</span>
        </div>
        <div class="data-flow" />
      </div>

      <div class="indicator-card carbon-total">
        <div class="indicator-header">
          <div class="indicator-icon">
            <i class="fas fa-smog" />
          </div>
          <div class="indicator-title">碳排总量</div>
        </div>
        <div class="indicator-value">2,456<small>kgCO₂</small></div>
        <div class="indicator-change">
          <span style="color: rgba(255, 255, 255, 0.6)">减碳量</span>
          <span style="color: #00ff87">1,024 kgCO₂</span>
        </div>
        <div class="data-flow" />
      </div>

      <div class="indicator-card carbon-save">
        <div class="indicator-header">
          <div class="indicator-icon">
            <i class="fas fa-leaf" />
          </div>
          <div class="indicator-title">减碳总量</div>
        </div>
        <div class="indicator-value">3,480<small>kgCO₂</small></div>
        <div class="indicator-change">
          <i class="fas fa-arrow-up" style="color: #00ff87" />
          <span style="color: #00ff87">15%</span>
          <span style="color: rgba(255, 255, 255, 0.6)">同比上月</span>
        </div>
        <div class="data-flow" />
      </div>
    </div>

    <!-- 左侧边栏 -->
    <div class="sidebar left slide-in-left">
      <div class="module" :class="{ collapsed: moduleCollapsed.energy }">
        <div class="module-header">
          <div class="module-title">
            <i class="fas fa-chart-line" />
            <span>能源生产与消耗</span>
          </div>
          <i
            class="fas fa-expand-arrows-alt"
            style="color: rgba(255, 255, 255, 0.6); cursor: pointer"
            @click="toggleModule('energyModule')"
          />
        </div>
        <div class="module-content">
          <div class="chart-container">
            <div class="wave-chart">
              <div class="wave-line" />
              <div class="wave-line" style="animation-delay: 0.5s; top: 40%" />
              <div class="wave-line" style="animation-delay: 1s; top: 60%" />
            </div>
          </div>
        </div>
      </div>

      <div class="module" :class="{ collapsed: moduleCollapsed.warehouse }">
        <div class="module-header">
          <div class="module-title">
            <i class="fas fa-warehouse" />
            <span>仓库用电监测</span>
          </div>
          <i
            class="fas fa-expand-arrows-alt"
            style="color: rgba(255, 255, 255, 0.6); cursor: pointer"
            @click="toggleModule('warehouseModule')"
          />
        </div>
        <div class="module-content">
          <div class="chart-container">
            <div
              style="
                display: flex;
                align-items: center;
                justify-content: space-around;
              "
            >
              <div class="circle-progress">
                <span>70%</span>
              </div>
              <div style="text-align: center">
                <div
                  style="font-size: 24px; color: #00a2ff; margin-bottom: 5px"
                >
                  348.5
                </div>
                <div style="font-size: 12px; color: rgba(255, 255, 255, 0.6)">
                  kWh
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="module" :class="{ collapsed: moduleCollapsed.solar }">
        <div class="module-header">
          <div class="module-title">
            <i class="fas fa-solar-panel" />
            <span>光伏发电监测</span>
          </div>
          <div style="display: flex; align-items: center; gap: 10px">
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
            >
              查看详情
            </button>
            <i
              class="fas fa-expand-arrows-alt"
              style="color: rgba(255, 255, 255, 0.6); cursor: pointer"
              @click="toggleModule('solarModule')"
            />
          </div>
        </div>
        <div class="module-content">
          <div class="chart-container">
            <div
              style="
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                gap: 10px;
              "
            >
              <div style="font-size: 32px; color: #00ff87">
                <i class="fas fa-sun" />
              </div>
              <div style="font-size: 18px; color: #00ff87">1,245 kWh</div>
              <div style="font-size: 12px; color: rgba(255, 255, 255, 0.6)">
                实时发电功率: 156.8 kW
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 右侧边栏 -->
    <div class="sidebar right slide-in-right">
      <div class="module" :class="{ collapsed: moduleCollapsed.alerts }">
        <div class="module-header">
          <div class="module-title">
            <i class="fas fa-exclamation-triangle" />
            <span>系统告警</span>
          </div>
          <i
            class="fas fa-expand-arrows-alt"
            style="color: rgba(255, 255, 255, 0.6); cursor: pointer"
            @click="toggleModule('alertsModule')"
          />
        </div>
        <div class="module-content">
          <div class="alert-list">
            <div
              v-for="(alert, index) in alertData"
              :key="index"
              :class="['alert-item', alert.type]"
              @click="handleAlertClick(alert)"
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

      <div class="module" :class="{ collapsed: moduleCollapsed.battery }">
        <div class="module-header">
          <div class="module-title">
            <i class="fas fa-battery-three-quarters" />
            <span>储能系统状态</span>
          </div>
          <i
            class="fas fa-expand-arrows-alt"
            style="color: rgba(255, 255, 255, 0.6); cursor: pointer"
            @click="toggleModule('batteryModule')"
          />
        </div>
        <div class="module-content">
          <div
            style="
              display: flex;
              justify-content: space-between;
              align-items: center;
              margin-bottom: 15px;
            "
          >
            <span style="font-size: 14px; color: rgba(255, 255, 255, 0.8)"
              >当前电量</span
            >
            <span style="font-size: 18px; color: #00a2ff; font-weight: bold"
              >{{ batterySystem.level }}%</span
            >
          </div>
          <div class="progress-bar" style="margin-bottom: 15px">
            <div
              class="progress-fill high"
              :style="`width: ${batterySystem.level}%`"
            />
          </div>
          <div
            style="
              display: grid;
              grid-template-columns: 1fr 1fr;
              gap: 10px;
              font-size: 12px;
            "
          >
            <div>
              <div style="color: rgba(255, 255, 255, 0.6)">充电功率</div>
              <div style="color: #00ff87; font-weight: bold">
                {{ batterySystem.chargePower }}
              </div>
            </div>
            <div>
              <div style="color: rgba(255, 255, 255, 0.6)">放电功率</div>
              <div style="color: #ff6b6b; font-weight: bold">
                {{ batterySystem.dischargePower }}
              </div>
            </div>
            <div>
              <div style="color: rgba(255, 255, 255, 0.6)">剩余时间</div>
              <div style="color: #ffa726; font-weight: bold">
                {{ batterySystem.timeRemaining }}
              </div>
            </div>
            <div>
              <div style="color: rgba(255, 255, 255, 0.6)">系统温度</div>
              <div style="color: #4fc3f7; font-weight: bold">
                {{ batterySystem.temperature }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 底部模块 -->
    <div class="bottom-modules fade-in">
      <div class="bottom-module small">
        <div class="module-header">
          <div class="module-title">
            <i class="fas fa-car-battery" />
            <span>充电桩状态</span>
          </div>
        </div>
        <div class="module-content">
          <div class="data-table-container">
            <table class="data-table">
              <thead>
                <tr>
                  <th>编号</th>
                  <th>状态</th>
                  <th>功率</th>
                  <th>电流</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(charger, index) in chargerData" :key="index">
                  <td>{{ charger.id }}</td>
                  <td>
                    <span :class="['status-badge', charger.statusClass]">{{
                      charger.status
                    }}</span>
                  </td>
                  <td>{{ charger.power }}</td>
                  <td>{{ charger.current }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <div class="bottom-module large">
        <div class="module-header">
          <div class="module-title">
            <i class="fas fa-chart-area" />
            <span>能耗趋势分析</span>
          </div>
          <div style="display: flex; gap: 10px">
            <button
              style="
                background: rgba(0, 162, 255, 0.2);
                border: 1px solid #00a2ff;
                color: #00a2ff;
                padding: 4px 12px;
                border-radius: 15px;
                font-size: 12px;
                cursor: pointer;
              "
            >
              今日
            </button>
            <button
              style="
                background: transparent;
                border: 1px solid rgba(255, 255, 255, 0.3);
                color: rgba(255, 255, 255, 0.7);
                padding: 4px 12px;
                border-radius: 15px;
                font-size: 12px;
                cursor: pointer;
              "
            >
              本周
            </button>
            <button
              style="
                background: transparent;
                border: 1px solid rgba(255, 255, 255, 0.3);
                color: rgba(255, 255, 255, 0.7);
                padding: 4px 12px;
                border-radius: 15px;
                font-size: 12px;
                cursor: pointer;
              "
            >
              本月
            </button>
          </div>
        </div>
        <div class="module-content">
          <div class="chart-container" style="height: 100px">
            <div
              style="
                display: flex;
                justify-content: space-between;
                align-items: end;
                height: 80px;
                padding: 0 20px;
              "
            >
              <div
                style="
                  display: flex;
                  flex-direction: column;
                  align-items: center;
                  gap: 5px;
                "
              >
                <div
                  style="
                    width: 20px;
                    height: 60px;
                    background: linear-gradient(
                      180deg,
                      #00ff87,
                      rgba(0, 255, 135, 0.3)
                    );
                    border-radius: 10px;
                  "
                />
                <span style="font-size: 10px; color: rgba(255, 255, 255, 0.6)"
                  >00:00</span
                >
              </div>
              <div
                style="
                  display: flex;
                  flex-direction: column;
                  align-items: center;
                  gap: 5px;
                "
              >
                <div
                  style="
                    width: 20px;
                    height: 45px;
                    background: linear-gradient(
                      180deg,
                      #00a2ff,
                      rgba(0, 162, 255, 0.3)
                    );
                    border-radius: 10px;
                  "
                />
                <span style="font-size: 10px; color: rgba(255, 255, 255, 0.6)"
                  >04:00</span
                >
              </div>
              <div
                style="
                  display: flex;
                  flex-direction: column;
                  align-items: center;
                  gap: 5px;
                "
              >
                <div
                  style="
                    width: 20px;
                    height: 75px;
                    background: linear-gradient(
                      180deg,
                      #ffa726,
                      rgba(255, 167, 38, 0.3)
                    );
                    border-radius: 10px;
                  "
                />
                <span style="font-size: 10px; color: rgba(255, 255, 255, 0.6)"
                  >08:00</span
                >
              </div>
              <div
                style="
                  display: flex;
                  flex-direction: column;
                  align-items: center;
                  gap: 5px;
                "
              >
                <div
                  style="
                    width: 20px;
                    height: 80px;
                    background: linear-gradient(
                      180deg,
                      #ff6b6b,
                      rgba(255, 107, 107, 0.3)
                    );
                    border-radius: 10px;
                  "
                />
                <span style="font-size: 10px; color: rgba(255, 255, 255, 0.6)"
                  >12:00</span
                >
              </div>
              <div
                style="
                  display: flex;
                  flex-direction: column;
                  align-items: center;
                  gap: 5px;
                "
              >
                <div
                  style="
                    width: 20px;
                    height: 70px;
                    background: linear-gradient(
                      180deg,
                      #4fc3f7,
                      rgba(79, 195, 247, 0.3)
                    );
                    border-radius: 10px;
                  "
                />
                <span style="font-size: 10px; color: rgba(255, 255, 255, 0.6)"
                  >16:00</span
                >
              </div>
              <div
                style="
                  display: flex;
                  flex-direction: column;
                  align-items: center;
                  gap: 5px;
                "
              >
                <div
                  style="
                    width: 20px;
                    height: 55px;
                    background: linear-gradient(
                      180deg,
                      #ab47bc,
                      rgba(171, 71, 188, 0.3)
                    );
                    border-radius: 10px;
                  "
                />
                <span style="font-size: 10px; color: rgba(255, 255, 255, 0.6)"
                  >20:00</span
                >
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="bottom-module small">
        <div class="module-header">
          <div class="module-title">
            <i class="fas fa-building" />
            <span>设备运行状态</span>
          </div>
        </div>
        <div class="module-content">
          <div
            style="
              display: grid;
              grid-template-columns: 1fr 1fr;
              gap: 15px;
              font-size: 12px;
            "
          >
            <div
              v-for="(device, index) in deviceStatus"
              :key="index"
              style="text-align: center"
            >
              <div
                style="
                  width: 40px;
                  height: 40px;
                  border-radius: 50%;
                  margin: 0 auto 8px;
                  display: flex;
                  align-items: center;
                  justify-content: center;
                "
                :style="`background: linear-gradient(45deg, ${device.bgColor}, rgba(${parseInt(device.bgColor.slice(1, 3), 16)}, ${parseInt(device.bgColor.slice(3, 5), 16)}, ${parseInt(device.bgColor.slice(5, 7), 16)}, 0.3))`"
              >
                <i
                  :class="`fas fa-${device.icon}`"
                  :style="`color: ${device.icon === 'warehouse' && device.status === '正常' ? '#003d20' : '#fff'}`"
                />
              </div>
              <div style="color: rgba(255, 255, 255, 0.6)">
                {{ device.name }}
              </div>
              <div :style="`color: ${device.color}; font-weight: bold`">
                {{ device.status }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 基本样式 */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

.main-container {
  font-family:
    "SF Pro Display",
    -apple-system,
    BlinkMacSystemFont,
    "Segoe UI",
    sans-serif;
  background: #0a0f1c;
  color: #ffffff;
  overflow: hidden;
  height: 100vh;
  position: relative;
}

/* 动态背景 */
.main-container::before {
  content: "";
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
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
  z-index: -1;
  animation: backgroundShift 20s ease-in-out infinite;
}

@keyframes backgroundShift {
  0%,
  100% {
    opacity: 1;
  }
  50% {
    opacity: 0.8;
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
  position: absolute;
  top: 10vh;
  left: 22vw;
  transform: translateX(-50%);
  display: flex;
  gap: 20px;
  z-index: 100;
}

.indicator-card {
  width: 200px;
  height: 140px;
  background: linear-gradient(
    135deg,
    rgba(0, 20, 40, 0.8) 0%,
    rgba(0, 40, 80, 0.6) 100%
  );
  backdrop-filter: blur(15px);
  -webkit-backdrop-filter: blur(15px);
  border: 1px solid rgba(0, 162, 255, 0.3);
  border-radius: 16px;
  padding: 20px;
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
  gap: 10px;
  margin-bottom: 10px;
}

.indicator-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
}

.indicator-title {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.7);
  font-weight: 500;
}

.indicator-value {
  font-size: 28px;
  font-weight: 700;
  margin-bottom: 5px;
}

.indicator-value small {
  font-size: 16px;
  margin-left: 5px;
}

.indicator-change {
  font-size: 11px;
  display: flex;
  align-items: center;
  gap: 4px;
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
  position: fixed;
  bottom: 20px;
  left: 20px;
  right: 20px;
  height: 160px;
  display: flex;
  gap: 20px;
  z-index: 50;
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
}

.bottom-module:hover {
  border-color: rgba(0, 162, 255, 0.6);
  box-shadow: 0 10px 40px rgba(0, 162, 255, 0.15);
}

.bottom-module.small {
  flex: 1;
  max-width: 300px;
}

.bottom-module.large {
  flex: 2;
  min-width: 500px;
}

/* 数据表格 */
.data-table {
  width: 100%;
  border-collapse: collapse;
}

.data-table th,
.data-table td {
  padding: 8px 12px;
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
  padding: 2px 8px;
  border-radius: 10px;
  font-size: 10px;
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
  .bottom-module.large {
    min-width: 400px;
  }
}

@media (max-width: 992px) {
  .indicators {
    position: relative;
    width: 100%;
    margin-bottom: 20px;
  }

  .sidebar {
    position: relative;
    width: 100%;
    flex-direction: row;
    height: auto;
  }

  .sidebar.left,
  .sidebar.right {
    position: relative;
    left: auto;
    right: auto;
    top: auto;
    bottom: auto;
  }

  .bottom-modules {
    position: relative;
    flex-direction: column;
    height: auto;
    bottom: auto;
  }

  .main-container {
    height: auto;
    overflow: auto;
    padding: 20px;
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
  position: fixed;
  top: 10vh;
  bottom: 200px;
  width: 350px;
  display: flex;
  flex-direction: column;
  gap: 20px;
  z-index: 50;
}

.sidebar.left {
  left: 20px;
}

.sidebar.right {
  right: 20px;
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
}

.module:hover {
  border-color: rgba(0, 162, 255, 0.6);
  box-shadow: 0 10px 40px rgba(0, 162, 255, 0.15);
}

.module-header {
  padding: 16px 20px;
  background: linear-gradient(
    135deg,
    rgba(0, 162, 255, 0.2) 0%,
    rgba(0, 120, 212, 0.1) 100%
  );
  border-bottom: 1px solid rgba(0, 162, 255, 0.2);
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.module-title {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 14px;
  font-weight: 600;
}

.module-title i {
  font-size: 16px;
  color: #00a2ff;
}

.module-content {
  padding: 20px;
  height: calc(100% - 60px);
  overflow-y: auto;
}

/* 图表容器 */
.chart-container {
  width: 100%;
  height: 200px;
  background: rgba(0, 0, 0, 0.2);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  overflow: hidden;
}

/* 模拟波形图 */
.wave-chart {
  width: 100%;
  height: 100%;
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
  gap: 12px;
}

.alert-item {
  background: rgba(0, 0, 0, 0.3);
  border-left: 4px solid;
  border-radius: 8px;
  padding: 12px;
  transition: all 0.3s ease;
}

.alert-item:hover {
  background: rgba(0, 0, 0, 0.5);
  transform: translateX(5px);
}

.alert-item.critical {
  border-left-color: #ff4757;
  background: linear-gradient(
    135deg,
    rgba(255, 71, 87, 0.1) 0%,
    rgba(0, 0, 0, 0.3) 100%
  );
}

.alert-item.warning {
  border-left-color: #ffa726;
  background: linear-gradient(
    135deg,
    rgba(255, 167, 38, 0.1) 0%,
    rgba(0, 0, 0, 0.3) 100%
  );
}

.alert-item.info {
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
}

.alert-badge.warning {
  background: #ffa726;
  color: white;
}

.alert-badge.info {
  background: #00a2ff;
  color: white;
}

.alert-time {
  font-size: 11px;
  color: rgba(255, 255, 255, 0.6);
}

.alert-desc {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.7);
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

/* 自定义滚动条样式 */
::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}

::-webkit-scrollbar-track {
  background: rgba(0, 0, 0, 0.1);
  border-radius: 3px;
}

::-webkit-scrollbar-thumb {
  background: linear-gradient(180deg, #00a2ff, #0078d4);
  border-radius: 3px;
  box-shadow: 0 0 10px rgba(0, 162, 255, 0.2);
}

::-webkit-scrollbar-thumb:hover {
  background: linear-gradient(180deg, #00a2ff, #00ff87);
  box-shadow: 0 0 15px rgba(0, 162, 255, 0.5);
}

/* 模块内部滚动条 */
.module-content::-webkit-scrollbar {
  width: 4px;
  height: 4px;
}

.module-content::-webkit-scrollbar-thumb {
  background: linear-gradient(180deg, #00a2ff, rgba(0, 162, 255, 0.5));
}

.module-content::-webkit-scrollbar-thumb:hover {
  background: linear-gradient(180deg, #00ff87, rgba(0, 255, 135, 0.5));
}

/* 表格部分滚动条 */
.data-table-container {
  max-height: 120px;
  overflow-y: auto;
}

.data-table-container::-webkit-scrollbar {
  width: 4px;
}

.data-table-container::-webkit-scrollbar-thumb {
  background: linear-gradient(180deg, #ffa726, rgba(255, 167, 38, 0.5));
}

@media (max-width: 992px) {
  .sidebar {
    position: relative;
    width: 100%;
    flex-direction: row;
    height: auto;
  }

  .sidebar.left,
  .sidebar.right {
    position: relative;
    left: auto;
    right: auto;
    top: auto;
    bottom: auto;
  }

  .bottom-modules {
    position: relative;
    flex-direction: column;
    height: auto;
    bottom: auto;
  }

  .main-container {
    height: auto;
    overflow: auto;
    padding: 20px;
  }
}
</style>
