<template>
  <div class="alarm-badge">
    <el-badge
      :value="unprocessedCount"
      :max="99"
      :hidden="unprocessedCount === 0"
      type="danger"
    >
      <el-button
        type="danger"
        :icon="Bell"
        circle
        size="small"
        @click="showAlarmDrawer"
      />
    </el-badge>

    <!-- 告警抽屉 -->
    <el-drawer
      v-model="drawerVisible"
      title="告警中心"
      size="500px"
      direction="rtl"
      :before-close="closeDrawer"
    >
      <template #header>
        <div class="drawer-header">
          <div class="drawer-title">
            <span>告警中心</span>
            <el-tag
              v-if="unprocessedCount > 0"
              type="danger"
              size="small"
              class="unprocessed-tag"
            >
              {{ unprocessedCount }}条未处理
            </el-tag>
          </div>
        </div>
      </template>

      <!-- 告警列表 -->
      <div v-if="filteredAlarms.length > 0" class="alarm-list">
        <el-scrollbar max-height="calc(100vh - 250px)">
          <div
            v-for="alarm in filteredAlarms"
            :key="alarm.id"
            class="alarm-item"
            @click="showAlarmDetail(alarm)"
          >
            <div class="alarm-icon" :class="'level-' + alarm.level">
              <el-icon><Warning /></el-icon>
            </div>
            <div class="alarm-content">
              <div class="alarm-title">
                <span class="alarm-level" :class="`level-${alarm.level}`">{{
                  getLevelText(alarm.level)
                }}</span>
                <span class="alarm-message">{{ alarm.message }}</span>
                <el-tag type="danger" size="small" class="unprocessed-label"
                  >未处理</el-tag
                >
              </div>
              <div class="alarm-info">
                <div class="alarm-time">
                  <el-icon><Clock /></el-icon>
                  {{ formatTime(getAlarmTime(alarm)) }}
                </div>
                <div class="alarm-type">{{ alarm.type }}</div>
              </div>
              <div
                v-if="alarm.detail && alarm.detail.DeviceName"
                class="alarm-device"
              >
                <el-icon><Monitor /></el-icon>
                {{ alarm.detail.DeviceName }}
              </div>
            </div>
          </div>
        </el-scrollbar>
      </div>
      <div v-else class="empty-alarm">
        <el-empty description="暂无未处理告警" :image-size="100">
          <template #description>
            <p>当前没有需要处理的告警</p>
          </template>
        </el-empty>
      </div>

      <!-- 底部操作区 -->
      <div class="drawer-footer">
        <el-button type="primary" @click="goToAlarmRecordPage"
          >查看全部告警</el-button
        >
      </div>
    </el-drawer>

    <!-- 告警详情对话框 -->
    <el-dialog
      v-model="detailDialogVisible"
      :title="
        selectedAlarm ? `告警详情 - ${selectedAlarm.message}` : '告警详情'
      "
      width="1200px"
      append-to-body
    >
      <div v-if="selectedAlarm" class="alarm-detail">
        <!-- 处理状态标签 -->
        <div v-if="selectedAlarm.detail" class="alarm-status-banner">
          <el-tag
            :type="
              selectedAlarm.detail.CheckResult === '未处理'
                ? 'danger'
                : 'success'
            "
            size="large"
            effect="dark"
            class="status-tag"
          >
            {{ selectedAlarm.detail.CheckResult || "未处理" }}
          </el-tag>
          <div
            v-if="selectedAlarm.detail.CheckResult !== '未处理'"
            class="process-info"
          >
            处理时间: {{ selectedAlarm.detail.ProcessTime || "未知" }}
            <br />
            处理人员: {{ selectedAlarm.detail.ProcessUser || "未知" }}
          </div>
        </div>

        <el-descriptions :column="1" border>
          <el-descriptions-item label="告警ID">{{
            selectedAlarm.id
          }}</el-descriptions-item>
          <el-descriptions-item label="告警类型">{{
            selectedAlarm.type
          }}</el-descriptions-item>
          <el-descriptions-item label="告警级别">
            <el-tag :type="getLevelType(selectedAlarm.level)">
              {{ getLevelText(selectedAlarm.level) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="告警内容">{{
            selectedAlarm.message
          }}</el-descriptions-item>
          <el-descriptions-item label="告警时间">
            <div class="time-display">
              <el-icon><Clock /></el-icon>
              {{ formatTime(getAlarmTime(selectedAlarm)) }}
            </div>
          </el-descriptions-item>
          <el-descriptions-item label="告警详情">
            <div
              v-if="selectedAlarm.detail && selectedAlarm.detail.DeviceName"
              class="device-alarm-detail"
            >
              <el-descriptions :column="2" border size="small">
                <el-descriptions-item label="设备ID">{{
                  selectedAlarm.detail.DeviceId
                }}</el-descriptions-item>
                <el-descriptions-item label="设备名称">{{
                  selectedAlarm.detail.DeviceName
                }}</el-descriptions-item>
                <el-descriptions-item label="设备类型">{{
                  selectedAlarm.detail.DeviceTypeCode
                }}</el-descriptions-item>
                <el-descriptions-item label="告警内容">{{
                  selectedAlarm.detail.AlarmValue
                }}</el-descriptions-item>
              </el-descriptions>
            </div>
            <div v-else class="detail-json">
              <pre>{{ JSON.stringify(selectedAlarm.detail, null, 2) }}</pre>
            </div>
          </el-descriptions-item>
        </el-descriptions>

        <!-- 添加处理按钮 -->
        <div class="dialog-footer">
          <el-button @click="detailDialogVisible = false">关闭</el-button>
          <!-- <el-button
            v-if="
              selectedAlarm.detail &&
              selectedAlarm.detail.CheckResult === '未处理'
            "
            type="primary"
            @click="handleProcessAlarm(selectedAlarm)"
          >
            标记为已处理
          </el-button> -->
          <el-button
            v-if="
              selectedAlarm.detail &&
              selectedAlarm.detail.CheckResult === '未处理'
            "
            type="success"
            @click="goToAlarmRecordPage"
          >
            转到告警记录页面处理
          </el-button>
        </div>
      </div>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted, onUnmounted, watch } from "vue";
import { Bell, Warning, Clock, Monitor } from "@element-plus/icons-vue";
import { emitter } from "@/utils/mitt";
import { ElMessageBox, ElMessage, ElNotification } from "element-plus";
import { alarmService } from "@/utils/alarmService";
import { useRouter } from "vue-router";
import { getListByPage, saveRoleBatch } from "@/api/report/alarmLog";
import { throttle } from "@pureadmin/utils";

// 定义告警项接口
interface AlarmItem {
  id: string;
  type: string;
  level: number;
  message: string;
  timestamp: number;
  detail?: any;
}

// 使用ref存储状态，而不是直接从store获取
const alarms = ref<AlarmItem[]>([]);
// 删除未读状态计数
// const unreadCount = ref(0);

// 未处理告警数量
const unprocessedCount = ref(0);
const loadingUnprocessed = ref(false);
// 最新告警
const latestAlarm = ref<AlarmItem | null>(null);

// 存储最近处理过的告警ID，用于去重
const recentlyProcessedAlarms = new Set<string>();

// 获取路由实例
const router = useRouter();

// 监听告警服务初始化状态
const alarmServiceInitialized = ref(false);

// 抽屉控制
const drawerVisible = ref(false);
// 告警详情控制
const detailDialogVisible = ref(false);
const selectedAlarm = ref(null);

// 修改为集成未处理告警
const filteredAlarms = computed(() => {
  // 只显示未处理告警
  return alarms.value.filter(
    alarm =>
      alarm.detail &&
      (alarm.detail.CheckResult === "未处理" ||
        alarm.detail.CheckResult === "0" ||
        alarm.detail.CheckResult === 0)
  );
});

// 显示告警抽屉
const showAlarmDrawer = () => {
  drawerVisible.value = true;
  // 移除标记已读功能
};

// 关闭抽屉
const closeDrawer = () => {
  drawerVisible.value = false;
};

// 格式化时间
const formatTime = timestamp => {
  if (!timestamp) return "未知时间";

  let date;
  try {
    // 尝试转换各种格式的时间
    if (typeof timestamp === "string") {
      // 尝试解析字符串时间格式
      date = new Date(timestamp);
    } else if (typeof timestamp === "number") {
      // 处理时间戳
      date = new Date(timestamp);
    } else {
      // 其他情况返回当前时间
      date = new Date();
    }

    // 检查日期是否有效
    if (isNaN(date.getTime())) {
      console.warn("无效的时间格式:", timestamp);
      date = new Date(); // 使用当前时间作为后备
    }
  } catch (error) {
    console.error("时间格式化错误:", error);
    date = new Date(); // 使用当前时间作为后备
  }

  return date.toLocaleString("zh-CN", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit"
  });
};

// 获取告警显示时间
const getAlarmTime = alarm => {
  // 按优先级获取时间：detail.EventTime > timestamp > 当前时间
  if (alarm.detail && alarm.detail.EventTime) {
    return alarm.detail.EventTime;
  } else if (alarm.timestamp) {
    return alarm.timestamp;
  } else {
    return Date.now();
  }
};

// 格式化告警详情
const formatAlarmDetail = detail => {
  if (!detail) return "无详细信息";

  // 检查是否为MQTT设备告警
  if (detail.DeviceName && detail.AlarmValue) {
    return {
      设备ID: detail.DeviceId || "未知",
      设备名称: detail.DeviceName || "未知",
      设备类型: detail.DeviceTypeCode || "未知",
      告警内容: detail.AlarmValue || "未知",
      告警时间: detail.EventTime || formatTime(Date.now())
    };
  }

  // 其他类型的告警，返回原始数据
  return detail;
};

// 清空所有告警
const clearAllAlarms = () => {
  alarms.value = [];
  latestAlarm.value = null;
  // 刷新未处理告警数量
  fetchUnprocessedAlarms();
  console.log("已清空所有告警");
  // 通知其他组件
  emitter.emit("alarmsCleared");
};

// 标记所有告警为已处理
const markAllAsProcessed = () => {
  // 处理所有未处理告警
  alarms.value.forEach(alarm => {
    if (
      alarm.detail &&
      (alarm.detail.CheckResult === "未处理" ||
        alarm.detail.CheckResult === "0" ||
        alarm.detail.CheckResult === 0)
    ) {
      alarm.detail.CheckResult = "已处理";
      alarm.detail.ProcessTime = getCurrentFormattedTime();
      alarm.detail.ProcessUser = "当前用户";
    }
  });

  // 刷新未处理告警数量
  fetchUnprocessedAlarms();
  console.log("已将所有告警标记为已处理");

  // 通知其他组件
  emitter.emit("unprocessedAlarmCountUpdated", 0);
};

// 跳转到告警记录页面
const goToAlarmRecordPage = () => {
  // 关闭抽屉
  drawerVisible.value = false;
  // 跳转到告警记录页面，传递查询参数
  router.push({
    path: "/record/alarmLog/index",
    query: {
      status: "未处理"
    }
  });
};

// 查看告警详情
const showAlarmDetail = alarm => {
  selectedAlarm.value = alarm;
  // 移除标记已读功能
  // 确保告警详情中包含正确的CheckResult字段
  if (alarm.detail) {
    // 统一处理状态值格式，确保以字符串形式显示
    if (alarm.detail.CheckResult === 0 || alarm.detail.CheckResult === "0") {
      alarm.detail.CheckResult = "未处理";
    } else if (
      alarm.detail.CheckResult === 1 ||
      alarm.detail.CheckResult === "1"
    ) {
      alarm.detail.CheckResult = "已处理";
    } else if (
      alarm.detail.CheckResult === 2 ||
      alarm.detail.CheckResult === "2"
    ) {
      alarm.detail.CheckResult = "已自动恢复";
    }
  }

  detailDialogVisible.value = true;
};

// 获取告警级别文本
const getLevelText = level => {
  switch (level) {
    case 1:
      return "普通"; // 原来可能有些地方显示为"信息"，统一改为"普通"
    case 2:
      return "严重";
    case 3:
      return "事故";
    default:
      return "普通";
  }
};

// 获取告警级别对应的类型
const getLevelType = level => {
  switch (level) {
    case 1:
      return "info";
    case 2:
      return "warning";
    case 3:
      return "danger";
    default:
      return "info";
  }
};

// 监听新的告警
const handleNewAlarm = alarm => {
  console.log("收到新的告警:", alarm);
  // 当收到新告警时，重新获取未处理告警数量
  fetchUnprocessedAlarms();
};

// 将fetchUnprocessedAlarms函数使用节流包装，限制5秒内只能调用一次
const fetchUnprocessedAlarmsThrottled = throttle(async () => {
  try {
    loadingUnprocessed.value = true;
    // 构建查询参数，查询未处理的告警
    const params = {
      page: 1,
      pagesize: 20, // 获取最新的20条未处理告警数据
      sconlist: [
        {
          ParamName: "CheckResult",
          ParamType: "=",
          ParamValue: "未处理"
        },
        {
          ParamName: "EventTime", // 报警时间字段
          ParamSort: 2 // 2表示倒序
        }
      ]
    };

    console.log("开始发送未处理告警请求...");
    const response = await getListByPage(params);
    console.log("收到未处理告警API响应:", response);
    if (response && response.Status) {
      // 设置未处理告警数量，使用接口返回的Total字段
      unprocessedCount.value = response.Total || 0;
      console.log("未处理告警数量(接口返回):", unprocessedCount.value);
      // 如果有告警数据，处理并添加到告警列表中
      if (response.Result) {
        try {
          const alarmData = JSON.parse(response.Result);
          if (Array.isArray(alarmData) && alarmData.length > 0) {
            console.log("获取到未处理告警数据:", alarmData.length, "条");
            // 将告警数据转换为本地告警格式并添加到告警列表
            const formattedAlarms = alarmData.map(item => {
              return {
                id:
                  item.SnowId ||
                  `alarm-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
                type: item.DeviceTypeName || "设备告警",
                level: getAlarmLevel(item.AlarmGrade),
                message: `${item.DeviceName || "未知设备"}: ${item.AlarmValue || item.EventType || "未知告警"}`,
                timestamp: new Date(item.EventTime || Date.now()).getTime(),
                detail: {
                  ...item,
                  CheckResult: "未处理"
                }
              };
            });

            // 更新告警列表
            if (formattedAlarms.length > 0) {
              alarms.value = formattedAlarms;
            }
          }
        } catch (error) {
          console.error("解析告警数据失败:", error);
        }
      }
    } else {
      console.warn(
        "获取未处理告警失败:",
        response?.Status === false ? "请求失败" : "未知错误"
      );
    }
  } catch (error) {
    console.error("获取未处理告警出错:", error);
  } finally {
    loadingUnprocessed.value = false;
  }
}, 5000); // 5秒节流

// 创建一个非节流版本用于首次加载，确保首次加载一定会执行
const fetchUnprocessedAlarmsImmediate = async () => {
  try {
    console.log("直接执行未处理告警请求(非节流版)...");
    loadingUnprocessed.value = true;
    // 构建查询参数，查询未处理的告警
    const params = {
      page: 1,
      pagesize: 20, // 获取最新的20条未处理告警数据
      sconlist: [
        {
          ParamName: "CheckResult",
          ParamType: "=",
          ParamValue: "未处理"
        },
        {
          ParamName: "EventTime", // 报警时间字段
          ParamSort: 2 // 2表示倒序
        }
      ]
    };

    const response = await getListByPage(params);
    console.log("首次加载收到未处理告警API响应:", response);
    if (response && response.Status) {
      // 设置未处理告警数量，使用接口返回的Total字段
      unprocessedCount.value = response.Total || 0;
      console.log("首次加载未处理告警数量(接口返回):", unprocessedCount.value);
      // 如果有告警数据，处理并添加到告警列表中
      if (response.Result) {
        try {
          const alarmData = JSON.parse(response.Result);
          if (Array.isArray(alarmData) && alarmData.length > 0) {
            console.log(
              "首次加载获取到未处理告警数据:",
              alarmData.length,
              "条"
            );
            // 将告警数据转换为本地告警格式并添加到告警列表
            const formattedAlarms = alarmData.map(item => {
              return {
                id:
                  item.SnowId ||
                  `alarm-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
                type: item.DeviceTypeName || "设备告警",
                level: getAlarmLevel(item.AlarmGrade),
                message: `${item.DeviceName || "未知设备"}: ${item.AlarmValue || item.EventType || "未知告警"}`,
                timestamp: new Date(item.EventTime || Date.now()).getTime(),
                detail: {
                  ...item,
                  CheckResult: "未处理"
                }
              };
            });

            // 更新告警列表
            if (formattedAlarms.length > 0) {
              alarms.value = formattedAlarms;
            }
          }
        } catch (error) {
          console.error("解析告警数据失败:", error);
        }
      }
    } else {
      console.warn(
        "获取未处理告警失败:",
        response?.Status === false ? "请求失败" : "未知错误"
      );
    }
  } catch (error) {
    console.error("获取未处理告警出错:", error);
  } finally {
    loadingUnprocessed.value = false;
  }
};

// 保留原始函数，但内部调用节流版本
const fetchUnprocessedAlarms = async () => {
  console.log(
    "fetchUnprocessedAlarms 被调用，时间:",
    new Date().toLocaleTimeString()
  );
  fetchUnprocessedAlarmsThrottled();
};

// 根据告警级别字符串获取数值
const getAlarmLevel = grade => {
  if (!grade) return 1;

  switch (grade.toString()) {
    case "1":
    case "普通":
      return 1;
    case "2":
    case "严重":
      return 2;
    case "3":
    case "事故":
      return 3;
    default:
      // 兼容旧的告警级别名称
      if (["一般", "低级"].includes(grade.toString())) {
        return 1; // 普通
      } else if (["中级"].includes(grade.toString())) {
        return 2; // 严重
      } else if (["高级", "紧急"].includes(grade.toString())) {
        return 3; // 事故
      }
      return 1; // 默认普通级别
  }
};

// 监听告警服务的初始化状态
watch(
  () => alarmServiceInitialized.value,
  initialized => {
    if (initialized) {
      console.log("告警服务已初始化，时间:", new Date().toLocaleTimeString());
      // 不再在这里调用fetchUnprocessedAlarms，因为onMounted中已经调用过了
      // 仅设置定时器，不重复获取数据
      if (!refreshTimer) {
        refreshTimer = window.setInterval(() => {
          fetchUnprocessedAlarms();
        }, 60000);
      }
    }
  }
);

// 定时刷新未处理告警数量的定时器
let refreshTimer: number | null = null;

// 获取当前格式化时间
const getCurrentFormattedTime = () => {
  const now = new Date();
  return now.toLocaleString("zh-CN", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit"
  });
};

// 统一处理MQTT告警数据的函数
const handleMqttAlarmEvent = data => {
  console.log(`[Alarm] 收到MQTT告警事件，来源: ${data?.source || "未知"}`);
  // 为数据添加来源标记，方便调试
  if (data && typeof data === "object") {
    data.source = data.source || "mqtt-event";
  }
  parseMqttAlarmData(data);
};

// 处理MQTT告警数据
const parseMqttAlarmData = (data: any) => {
  try {
    // 为数据添加唯一标识，用于去重
    const dataId =
      typeof data === "object"
        ? JSON.stringify(data).slice(0, 100) // 使用前100个字符作为简单标识
        : String(data);
    const dataHash = `mqtt-${Date.now()}-${dataId}`;

    // 检查是否已经处理过相同数据
    if (recentlyProcessedAlarms.has(dataHash)) {
      console.log(
        `[Alarm] 跳过重复的MQTT数据: ${dataHash.substring(0, 20)}...`
      );
      return;
    }

    // 标记数据已处理
    recentlyProcessedAlarms.add(dataHash);

    console.log(data);
    // 记录接收到的数据类型
    console.log(
      `[Alarm] 处理MQTT告警数据: ${Array.isArray(data) ? "数组" : typeof data}`
    );

    // 检查是否已处理过的消息
    if (Array.isArray(data)) {
      // 过滤掉已处理过的消息
      const unprocessedItems = data.filter(
        item =>
          item && typeof item === "object" && item._mqttProcessed === false
      );
      if (unprocessedItems.length === 0) {
        console.log("[Alarm] 所有消息已被处理过或无效，跳过处理");
        return;
      }
      // 标记所有消息为已处理
      data.forEach(item => {
        if (item && typeof item === "object") {
          item._mqttProcessed = true;
        }
      });
      // 找出所有告警数据（OptType=1）和恢复数据（OptType=2）
      const alarmMessages = unprocessedItems.filter(
        item => item.DataType === 3 && item.OptType === 1
      );
      const recoveryMessages = unprocessedItems.filter(
        item => item.DataType === 3 && item.OptType === 2
      );

      console.log(
        `[Alarm] 找到${alarmMessages.length}条告警，${recoveryMessages.length}条恢复`
      );
      // 处理快速离线恢复的情况
      if (alarmMessages.length > 0 && recoveryMessages.length > 0) {
        // 尝试查找匹配的告警和恢复消息对
        const matchedPairs = findMatchingAlarmRecoveryPairs(
          alarmMessages,
          recoveryMessages
        );
        console.log(`[Alarm] 找到${matchedPairs.length}对匹配的告警-恢复消息`);
        // 处理匹配到的告警-恢复对
        matchedPairs.forEach(pair => {
          const { alarm, recovery } = pair;

          // 解析告警和恢复的DataContent
          let alarmContent, recoveryContent;
          try {
            alarmContent =
              typeof alarm.DataContent === "string"
                ? JSON.parse(alarm.DataContent)
                : alarm.DataContent;

            recoveryContent =
              typeof recovery.DataContent === "string"
                ? JSON.parse(recovery.DataContent)
                : recovery.DataContent;

            // 计算告警持续时间（毫秒）
            const alarmTime = new Date(alarmContent.EventTime).getTime();
            const recoveryTime = new Date(recoveryContent.EventTime).getTime();
            const durationMs = recoveryTime - alarmTime;
            const durationSeconds = Math.floor(durationMs / 1000);

            // 创建包含恢复信息的告警对象
            const alarmData = {
              id: `mqtt-alarm-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
              type: alarmContent.DeviceTypeCode || "设备告警",
              // 设置告警级别
              level: parseAlarmLevel(alarmContent),
              message: `${alarmContent.DeviceName || "未知设备"}: ${alarmContent.AlarmValue || "未知告警"} (已恢复)`,
              timestamp: new Date(
                alarmContent.EventTime || Date.now()
              ).getTime(),
              detail: {
                ...alarmContent,
                rawAlarm: alarm,
                rawRecovery: recovery,
                isAutoRecovered: true,
                recoveryTime: recoveryContent.EventTime,
                durationSeconds: durationSeconds,
                CheckResult: "已自动恢复", // 标记为已自动恢复
                AlarmValue: `${alarmContent.AlarmValue || "未知告警"} (已恢复，持续${durationSeconds}秒)`
              }
            };

            // 发送告警，但如果持续时间太短（小于3秒），则降低告警级别
            if (durationSeconds < 3) {
              // 快速恢复的告警降低一个级别，但最低为1
              alarmData.level = Math.max(1, alarmData.level - 1);
              alarmData.detail.AlarmValue += " [短暂告警]";
            }

            // 处理告警
            console.log(
              `[Alarm] 处理自动恢复告警: ${alarmData.message}, 持续时间: ${durationSeconds}秒`
            );
            handleMqttAlarm(alarmData);

            // 标记两条消息都已处理
            alarm._processed = true;
            recovery._processed = true;
          } catch (error) {
            console.error("[Alarm] 处理告警-恢复对失败:", error);
          }
        });

        // 移除已处理的告警和恢复消息
        const processedAlarmIds = matchedPairs.map(pair => pair.alarm._msgId);
        const processedRecoveryIds = matchedPairs.map(
          pair => pair.recovery._msgId
        );

        // 过滤掉已处理的消息，只处理剩余的消息
        const remainingAlarms = alarmMessages.filter(
          item => !processedAlarmIds.includes(item._msgId)
        );
        const remainingRecoveries = recoveryMessages.filter(
          item => !processedRecoveryIds.includes(item._msgId)
        );

        // 处理剩余的告警消息
        processRemainingMessages(remainingAlarms, remainingRecoveries);
      } else {
        // 没有匹配的告警-恢复对，按常规方式处理所有消息
        processRemainingMessages(alarmMessages, recoveryMessages);
      }
    } else {
      // 检查非数组格式的消息是否已处理
      if (data && typeof data === "object") {
        if (data._mqttProcessed === true) {
          console.log("[Alarm] 该消息已被处理过，跳过处理");
          return;
        }
        // 标记消息为已处理
        data._mqttProcessed = true;
        // 尝试直接处理非数组格式的数据
        console.log("[Alarm] 处理非数组格式的告警数据");
        const alarmData = {
          id: `mqtt-alarm-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
          type: "MQTT告警",
          level: 3, // 修改为严重级别
          message: `系统告警: ${JSON.stringify(data).substring(0, 50)}...`,
          timestamp: Date.now(),
          detail: {
            ...data,
            CheckResult: "未处理"
          }
        };
        handleMqttAlarm(alarmData);
      } else {
        console.log("[Alarm] 忽略无效的告警数据");
      }
    }
  } catch (error) {
    console.error("[Alarm] 处理MQTT告警数据时出错:", error);
  }
};

// 查找匹配的告警和恢复消息对
const findMatchingAlarmRecoveryPairs = (alarmMessages, recoveryMessages) => {
  const pairs = [];
  const MAX_TIME_DIFF_MS = 5 * 60 * 1000; // 5分钟，超过这个时间差的不会被认为是配对的告警-恢复

  // 遍历所有告警消息
  for (const alarm of alarmMessages) {
    let alarmContent;
    try {
      alarmContent =
        typeof alarm.DataContent === "string"
          ? JSON.parse(alarm.DataContent)
          : alarm.DataContent;

      // 获取告警时间
      const alarmTime = new Date(
        alarmContent.EventTime || Date.now()
      ).getTime();

      // 寻找匹配的恢复消息
      const matchingRecovery = recoveryMessages.find(recovery => {
        try {
          const recoveryContent =
            typeof recovery.DataContent === "string"
              ? JSON.parse(recovery.DataContent)
              : recovery.DataContent;

          // 获取恢复时间
          const recoveryTime = new Date(
            recoveryContent.EventTime || Date.now()
          ).getTime();

          // 计算时间差
          const timeDiff = recoveryTime - alarmTime;

          // 匹配条件：
          // 1. 设备ID相同
          // 2. 设备类型相同
          // 3. 恢复时间晚于告警时间
          // 4. 时间差不超过阈值
          return (
            alarmContent.DeviceId === recoveryContent.DeviceId &&
            alarmContent.DeviceTypeCode === recoveryContent.DeviceTypeCode &&
            timeDiff > 0 && // 恢复时间必须晚于告警时间
            timeDiff <= MAX_TIME_DIFF_MS // 时间差不能太大
          );
        } catch (error) {
          console.error("[Alarm] 解析恢复消息时出错:", error);
          return false;
        }
      });

      // 如果找到匹配的恢复消息
      if (matchingRecovery) {
        console.log(
          `[Alarm] 找到匹配的告警-恢复对: 设备ID=${alarmContent.DeviceId}, 设备名称=${alarmContent.DeviceName}`
        );
        pairs.push({ alarm, recovery: matchingRecovery });
      }
    } catch (error) {
      console.error("[Alarm] 解析告警内容失败:", error);
    }
  }

  console.log(`[Alarm] 共找到 ${pairs.length} 对匹配的告警-恢复消息`);
  return pairs;
};

// 处理剩余消息
const processRemainingMessages = (alarmMessages, recoveryMessages) => {
  // 处理剩余的告警消息
  alarmMessages.forEach(item => {
    if (item.DataType === 3 && item.DataContent) {
      console.log(`[Alarm] 处理告警消息: OptType=${item.OptType}`);

      // 尝试解析DataContent字段
      let dataContent;
      try {
        dataContent =
          typeof item.DataContent === "string"
            ? JSON.parse(item.DataContent)
            : item.DataContent;

        // 构建告警对象
        const alarmData = {
          id: `mqtt-alarm-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
          type: dataContent.DeviceTypeCode || "设备告警",
          // 使用parseAlarmLevel函数解析告警级别
          level: parseAlarmLevel(dataContent),
          message: `${dataContent.DeviceName || "未知设备"}: ${dataContent.AlarmValue || "未知告警"}`,
          timestamp: new Date(dataContent.EventTime || Date.now()).getTime(),
          detail: {
            ...dataContent,
            rawData: item,
            CheckResult: "未处理" // 添加未处理状态
          }
        };

        // 处理告警
        console.log(`[Alarm] 传递告警到handleMqttAlarm: ${alarmData.message}`);
        handleMqttAlarm(alarmData);
      } catch (error) {
        console.error("[Alarm] 解析DataContent失败:", error);
      }
    }
  });

  // 处理剩余的恢复消息 - 仅记录日志，不显示通知
  recoveryMessages.forEach(item => {
    if (item.DataType === 3 && item.DataContent) {
      console.log(
        `[Alarm] 收到恢复消息: OptType=${item.OptType}，但不单独显示通知`
      );

      // 尝试解析DataContent字段，仅记录日志
      let dataContent;
      try {
        dataContent =
          typeof item.DataContent === "string"
            ? JSON.parse(item.DataContent)
            : item.DataContent;

        // 记录日志，但不发送通知
        console.log(
          `[Alarm] 设备 ${dataContent.DeviceName || "未知设备"} 的告警 ${dataContent.AlarmValue || "未知告警"} 已恢复，不单独显示通知`
        );

        // 可以将恢复信息添加到告警列表，但不触发通知
        if (latestAlarm.value && latestAlarm.value.detail) {
          // 检查是否是同一设备的最新告警
          if (
            latestAlarm.value.detail.DeviceId === dataContent.DeviceId &&
            latestAlarm.value.detail.DeviceTypeCode ===
              dataContent.DeviceTypeCode &&
            !latestAlarm.value.detail.isAutoRecovered
          ) {
            // 更新最新告警的状态
            latestAlarm.value.detail.CheckResult = "已自动恢复";
            latestAlarm.value.detail.isAutoRecovered = true;
            latestAlarm.value.detail.recoveryTime = dataContent.EventTime;
            console.log(
              `[Alarm] 已将最新告警 ${latestAlarm.value.message} 标记为已恢复`
            );
          }
        }
      } catch (error) {
        console.error("[Alarm] 解析恢复消息DataContent失败:", error);
      }
    }
  });
};

// 解析告警级别
const parseAlarmLevel = (data: any): number => {
  try {
    // 优先使用明确的级别字段
    if (data.level) {
      const level = parseInt(data.level);
      // 确保级别在1-3范围内
      return Math.min(Math.max(level, 1), 3);
    }

    if (data.AlarmLevel) {
      const level = parseInt(data.AlarmLevel);
      // 确保级别在1-3范围内
      return Math.min(Math.max(level, 1), 3);
    }

    // 根据告警内容判断级别
    if (data.AlarmValue) {
      // 离线告警通常是最严重的
      if (data.AlarmValue.includes("离线")) {
        return 3; // 严重
      }
      // 异常或报警通常是中等级别
      if (
        data.AlarmValue.includes("异常") ||
        data.AlarmValue.includes("报警")
      ) {
        return 2; // 事故
      }
    }

    // 没有特殊情况，默认为普通告警
    return 1;
  } catch (error) {
    console.error("[Alarm] 解析告警级别失败:", error);
    return 1; // 出错时默认为普通
  }
};

// 处理MQTT告警
const handleMqttAlarm = (alarm: AlarmItem) => {
  try {
    console.log(`[Alarm] 处理MQTT告警: ${alarm.message}, 级别: ${alarm.level}`);

    // 检查是否是重复告警
    if (recentlyProcessedAlarms.has(alarm.id)) {
      console.log(`[Alarm] 跳过重复告警: ${alarm.id}`);
      return;
    }

    // 添加到告警列表
    alarms.value.unshift(alarm);

    // 限制列表长度，保持最近的200条告警
    if (alarms.value.length > 200) {
      alarms.value = alarms.value.slice(0, 200);
    }

    // 更新最新告警
    latestAlarm.value = alarm;

    // 更新未处理告警数量
    if (
      alarm.detail &&
      (alarm.detail.CheckResult === "未处理" ||
        alarm.detail.CheckResult === "0" ||
        alarm.detail.CheckResult === 0)
    ) {
      // 重新获取未处理告警，而不是本地增加计数
      fetchUnprocessedAlarms();
    }

    // 将告警ID添加到最近处理的告警集合中
    recentlyProcessedAlarms.add(alarm.id);

    // 如果集合太大，移除最旧的项
    if (recentlyProcessedAlarms.size > 100) {
      const iterator = recentlyProcessedAlarms.values();
      recentlyProcessedAlarms.delete(iterator.next().value);
    }

    // 发送全局事件
    emitter.emit("newAlarm", alarm);
    // 对高级别告警显示通知
    if (alarm.level >= 3) {
      showAlarmNotification(alarm);
    }
  } catch (error) {
    console.error("[Alarm] 处理告警数据失败:", error);
  }
};

// 显示告警通知
const showAlarmNotification = (alarm: AlarmItem) => {
  // 检查是否是仅恢复的消息，如果是则不显示通知
  if (alarm.detail && alarm.detail.isRecoveryOnly) {
    console.log("[Alarm] 跳过显示恢复消息的通知");
    return;
  }

  // 检查是否是离线告警，如果是离线告警则不显示通知
  if (
    alarm.message &&
    typeof alarm.message === "string" &&
    (alarm.message.includes("离线") ||
      (alarm.detail &&
        alarm.detail.AlarmValue &&
        typeof alarm.detail.AlarmValue === "string" &&
        alarm.detail.AlarmValue.includes("离线")))
  ) {
    console.log("[Alarm] 跳过显示离线告警的通知:", alarm.message);
    return;
  }

  console.log("[Alarm] 准备显示告警通知:", alarm.message);

  try {
    // 检查是否是自动恢复的告警
    const isAutoRecovered =
      alarm.detail &&
      (alarm.detail.isAutoRecovered ||
        alarm.detail.CheckResult === "已自动恢复");

    // 获取告警时间
    const alarmTime =
      alarm.detail?.EventTime || new Date(alarm.timestamp).toLocaleString();

    // 构建通知标题
    let title = `${getLevelText(alarm.level)}告警 - ${alarm.type} [${alarmTime}]`;

    // 对于自动恢复的告警，修改标题
    if (isAutoRecovered) {
      // 如果持续时间很短，可以显示为"短暂离线"
      const durationSeconds = alarm.detail.durationSeconds;
      if (durationSeconds !== undefined && durationSeconds < 10) {
        title = `短暂离线通知 - ${alarm.type} [${alarmTime}]`;
      } else {
        title = `${getLevelText(alarm.level)}告警(已自动恢复) - ${alarm.type} [${alarmTime}]`;
      }
    }

    // 构建通知内容
    let message = alarm.message;
    if (alarm.detail && alarm.detail.DeviceName) {
      // 基本信息
      let msgContent = `<b>设备:</b> ${alarm.detail.DeviceName}\n<b>内容:</b> ${alarm.detail.AlarmValue || alarm.message}`;

      // 添加时间信息（加粗并突出显示）
      msgContent += `\n<b>时间:</b> <span style="color: #E6A23C;">${alarmTime}</span>`;

      // 对于自动恢复的告警，添加持续时间
      if (isAutoRecovered && alarm.detail.durationSeconds !== undefined) {
        // 如果持续时间小于60秒，显示秒数
        if (alarm.detail.durationSeconds < 60) {
          msgContent += `\n<b>持续时间:</b> ${alarm.detail.durationSeconds}秒`;
        }
        // 如果持续时间大于60秒但小于3600秒(1小时)，显示分钟数
        else if (alarm.detail.durationSeconds < 3600) {
          const minutes = Math.floor(alarm.detail.durationSeconds / 60);
          const seconds = alarm.detail.durationSeconds % 60;
          msgContent += `\n<b>持续时间:</b> ${minutes}分${seconds}秒`;
        }
        // 如果持续时间大于1小时，显示小时数
        else {
          const hours = Math.floor(alarm.detail.durationSeconds / 3600);
          const minutes = Math.floor(
            (alarm.detail.durationSeconds % 3600) / 60
          );
          msgContent += `\n<b>持续时间:</b> ${hours}小时${minutes}分钟`;
        }

        msgContent += `\n<b>恢复时间:</b> ${alarm.detail.recoveryTime || "未知"}`;
      }

      message = msgContent;
    }

    // 确定通知类型
    let notificationType = getLevelType(alarm.level);
    // 自动恢复的告警使用不同的通知类型
    if (isAutoRecovered) {
      // 如果是短暂告警（小于3秒），使用info类型
      if (
        alarm.detail.durationSeconds !== undefined &&
        alarm.detail.durationSeconds < 3
      ) {
        notificationType = "info";
      } else if (alarm.level >= 4) {
        // 高级别告警自动恢复，使用warning类型
        notificationType = "warning";
      }
    }

    console.log(`[Alarm] 显示${notificationType}类型通知: ${title}`);
    // 显示通知
    ElNotification({
      title,
      message,
      type: notificationType,
      duration: isAutoRecovered ? 8000 : 10000, // 自动恢复的告警显示时间稍短
      position: "top-right",
      zIndex: 9999,
      showClose: true,
      dangerouslyUseHTMLString: true, // 使用HTML以便格式化消息
      onClick: () => {
        // 点击通知跳转到告警中心
        console.log("[Alarm] 通知被点击，打开告警中心");
        emitter.emit("openPanel", "alarm-center");
      }
    } as any); // 添加类型断言解决TypeScript类型错误

    console.log("[Alarm] 告警通知已显示");
  } catch (error) {
    console.error("[Alarm] 显示告警通知失败:", error);

    // 尝试使用备用方式显示通知
    try {
      // 获取告警时间
      const alarmTime =
        alarm.detail?.EventTime || new Date(alarm.timestamp).toLocaleString();

      ElNotification({
        title: `系统告警 [${alarmTime}]`,
        message: alarm.message || "收到新告警",
        type: "error",
        duration: 10000
      } as any); // 添加类型断言解决TypeScript类型错误
      console.log("[Alarm] 使用备用方式显示通知成功");
    } catch (backupError) {
      console.error("[Alarm] 备用通知方式也失败:", backupError);
    }
  }
};

// 清空所有告警
const clearAll = () => {
  ElMessageBox.confirm("确定要清空所有告警记录吗？此操作不可恢复", "警告", {
    confirmButtonText: "确定",
    cancelButtonText: "取消",
    type: "warning"
  })
    .then(() => {
      clearAllAlarms();
      // 如果alarmService存在，也清空其中的告警
      if (alarmService && typeof alarmService.clearAlarms === "function") {
        alarmService.clearAlarms();
      }
    })
    .catch(() => {
      // 取消操作
    });
};

// 处理未处理告警数量更新事件
const handleUnprocessedAlarmCountUpdated = (count: number) => {
  // unprocessedCount.value = count;
};

onMounted(() => {
  console.log("AlarmBadge组件已挂载，时间:", new Date().toLocaleTimeString());
  // 使用非节流版本立即获取数据
  fetchUnprocessedAlarmsImmediate();

  // 延迟初始化，确保全局事件总线已经准备好
  setTimeout(() => {
    // 检查告警服务是否已初始化
    try {
      // 移除对alarmStore的引用
      // const alarmStore = useAlarmStoreHook();

      if (alarmService && typeof alarmService.getUnreadCount === "function") {
        alarmServiceInitialized.value = true;
      } else {
        console.warn("告警服务尚未准备好，稍后将重试");
        // 再次延迟尝试
        setTimeout(() => {
          alarmServiceInitialized.value = true;
        }, 1000);
      }

      // 移除alarmStore初始化
      // if (alarmStore && typeof alarmStore.initAlarmListener === "function") {
      //   alarmStore.initAlarmListener();
      //   console.log("告警存储监听器已初始化");
      // }

      // 这里使用节流版本定期更新数据
      refreshTimer = window.setInterval(() => {
        // 只需调用fetchUnprocessedAlarms即可，它会同时更新告警列表和数量
        fetchUnprocessedAlarms();
      }, 60000);
    } catch (error) {
      console.error("初始化告警状态失败:", error);
    }

    // 监听新的告警
    emitter.on("newAlarm", handleNewAlarm);
    // 使用统一的处理函数监听MQTT告警事件
    emitter.on("changAlarm", handleMqttAlarmEvent);
    emitter.on("mqttAlarmData", handleMqttAlarmEvent);

    // 监听打开告警面板事件
    emitter.on("openPanel", panel => {
      if (panel === "alarm-center") {
        showAlarmDrawer();
      }
    });

    // 监听未处理告警数量更新事件
    emitter.on(
      "unprocessedAlarmCountUpdated",
      handleUnprocessedAlarmCountUpdated
    );
  }, 500);
});

onUnmounted(() => {
  // 移除事件监听
  emitter.off("newAlarm", handleNewAlarm);
  // 移除MQTT告警事件监听
  emitter.off("changAlarm");
  emitter.off("mqttAlarmData");
  emitter.off(
    "unprocessedAlarmCountUpdated",
    handleUnprocessedAlarmCountUpdated
  );

  // 使用更安全的方式移除 openPanel 事件监听
  try {
    emitter.all.delete("openPanel");
  } catch (error) {
    console.error("移除 openPanel 事件监听失败:", error);
  }

  console.log("AlarmBadge组件已卸载，移除了所有事件监听");

  // 清除定时器
  if (refreshTimer) {
    clearInterval(refreshTimer);
    refreshTimer = null;
  }

  // 清空最近处理的告警集合
  recentlyProcessedAlarms.clear();
});
</script>

<style scoped>
.alarm-badge {
  display: inline-block;
  margin: 0 10px;
}

.drawer-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.drawer-title {
  display: flex;
  align-items: center;
  gap: 8px;
}

.unprocessed-tag {
  margin-left: 8px;
}

.drawer-actions {
  display: flex;
  justify-content: space-between;
  margin-bottom: 15px;
  padding: 0 10px;
}

.drawer-footer {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  padding: 16px;
  background-color: #fff;
  text-align: center;
  border-top: 1px solid #ebeef5;
  box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.05);
}

.alarm-list {
  padding: 0 0 70px 0; /* 为底部操作区留出空间 */
}

.no-alarms {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 200px;
}

.alarm-item {
  padding: 12px 16px;
  border-bottom: 1px solid #ebeef5;
  display: flex;
  align-items: flex-start;
  transition: background-color 0.3s;
  cursor: pointer;
}

.alarm-item:hover {
  background-color: #f5f7fa;
}

/* 移除未读告警样式 */
/* .alarm-item.unread {
  background-color: rgba(255, 242, 232, 0.6);
}

.alarm-item.unread:hover {
  background-color: rgba(255, 242, 232, 0.8);
} */

.alarm-icon {
  margin-right: 16px;
  font-size: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.alarm-icon.level-1 {
  color: #909399; /* 普通告警 - 灰色 */
}

.alarm-icon.level-2 {
  color: #e6a23c; /* 事故告警 - 橙色 */
}

.alarm-icon.level-3 {
  color: #f56c6c; /* 严重告警 - 红色 */
  animation: blink 1s infinite;
}

.alarm-icon.level-4 {
  color: #f56c6c;
}

.alarm-icon.level-5 {
  color: #f56c6c;
  animation: blink 1s infinite;
}

.alarm-content {
  flex: 1;
  overflow: hidden;
}

.alarm-title {
  font-weight: bold;
  margin-bottom: 5px;
  line-height: 1.4;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.alarm-level {
  padding: 0 6px;
  margin-right: 6px;
  border-radius: 4px;
  font-size: 12px;
  display: inline-block;
}

.alarm-level.level-1 {
  background-color: #f0f2f5;
  color: #606266;
  border: 1px solid #dcdfe6;
}

.alarm-level.level-2 {
  background-color: #fdf6ec;
  color: #e6a23c;
  border: 1px solid #faecd8;
}

.alarm-level.level-3 {
  background-color: #fef0f0;
  color: #f56c6c;
  border: 1px solid #fde2e2;
}

.unprocessed-label {
  margin-left: 8px;
  font-size: 10px;
  height: 18px;
  line-height: 16px;
  padding: 0 4px;
}

.alarm-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}

.alarm-time {
  font-size: 12px;
  color: #909399;
  margin-bottom: 4px;
  display: flex;
  align-items: center;
  gap: 4px;
}

.alarm-type {
  font-size: 12px;
  color: #606266;
  padding: 2px 6px;
  background: #f5f7fa;
  border-radius: 4px;
  display: inline-block;
}

/* 移除告警操作按钮样式 */
/* .alarm-actions {
  margin-left: 10px;
  display: flex;
  align-items: center;
} */

.detail-json {
  max-height: 300px;
  overflow: auto;
  background-color: #f8f8f8;
  padding: 10px;
  border-radius: 4px;
}

.detail-json pre {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-all;
}

.device-alarm-detail {
  margin-top: 10px;
}

.device-alarm-detail :deep(.el-descriptions__label) {
  width: 80px;
  font-weight: bold;
  color: #606266;
}

.device-alarm-detail :deep(.el-descriptions__content) {
  color: #303133;
}

.alarm-device {
  font-size: 12px;
  color: #606266;
  display: flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 4px;
}

@keyframes blink {
  0% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
  100% {
    opacity: 1;
  }
}

.dialog-footer {
  margin-top: 20px;
  text-align: right;
}

.filter-container {
  padding: 0 10px;
  margin-bottom: 15px;
  display: flex;
  justify-content: center;
}

.empty-alarm {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 200px;
}

.time-display {
  display: flex;
  align-items: center;
  gap: 5px;
  color: #606266;
}

.alarm-status-banner {
  padding: 10px;
  margin-bottom: 15px;
  border-radius: 4px;
  background-color: #f8f9fa;
  display: flex;
  align-items: center;
}

.status-tag {
  font-size: 14px;
  padding: 8px 16px;
  margin-right: 15px;
}

.process-info {
  font-size: 13px;
  color: #606266;
}
</style>
