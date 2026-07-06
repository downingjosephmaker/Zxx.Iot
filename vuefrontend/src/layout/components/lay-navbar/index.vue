<script setup lang="ts">
import { useNav } from "@/layout/hooks/useNav";
import { ref, onMounted, onUnmounted, watch, computed } from "vue";
import LaySearch from "../lay-search/index.vue";
import LayNotice from "../lay-notice/index.vue";
import LayNavMix from "../lay-sidebar/NavMix.vue";
import { useTranslationLang } from "@/layout/hooks/useTranslationLang";
import LaySidebarFullScreen from "../lay-sidebar/components/SidebarFullScreen.vue";
import LaySidebarBreadCrumb from "../lay-sidebar/components/SidebarBreadCrumb.vue";
import LaySidebarTopCollapse from "../lay-sidebar/components/SidebarTopCollapse.vue";
import LaySidebarUnit from "../lay-sidebar/components/SidebaUnit.vue";
import HeaderBadges from "@/components/HeaderBadges.vue";
import { useRouter } from "vue-router";
import { useMqttClient, MqttConnectionState } from "@/plugins/mqtt";
import { alarmService } from "@/utils/alarmService";
import { ElMessage, ElMessageBox } from "element-plus";
import { emitter } from "@/utils/mitt";
import { useUserStoreHook } from "@/store/modules/user";
import { useAlarmStoreHook } from "@/store/modules/alarm";

import GlobalizationIcon from "@/assets/svg/globalization.svg?component";
import AccountSettingsIcon from "~icons/ri/user-settings-line";
import LogoutCircleRLine from "~icons/ri/logout-circle-r-line";
import Setting from "~icons/ri/settings-3-line";
import Check from "~icons/ep/check";
import Password from "~icons/ri/lock-password-line";
import DashboardIcon from "~icons/ri/dashboard-3-line";
import BellIcon from "~icons/ep/bell";

// MQTT客户端和告警服务
const mqttClient = useMqttClient();
const mqttInitialized = ref(false);
const alarmInitialized = ref(false);
const connectionAttempts = ref(0);
const maxRetries = 3;
const mqttStatus = ref<string>(MqttConnectionState.DISCONNECTED);
const mqttConnected = ref<boolean>(false);
const showMqttStatus = ref<boolean>(false);

// 获取用户存储，用于监听单位ID变化
const userStore = useUserStoreHook();
const currentUnitId = ref(userStore.unitId || "");

// 获取告警存储
// const alarmStore = useAlarmStoreHook() as any;

// 告警未读数量
const unreadAlarmCount = computed(() => {
  try {
    return alarmService.getUnreadCount?.() || 0;
  } catch (error) {
    console.error("获取未读告警数量失败:", error);
    return 0;
  }
});
const hasUnreadAlarms = computed(() => unreadAlarmCount.value > 0);

// 监听单位ID变化，自动重连MQTT
watch(
  () => userStore.unitId,
  async (newUnitId, oldUnitId) => {
    if (newUnitId !== oldUnitId && mqttInitialized.value) {
      currentUnitId.value = newUnitId || "";
      console.log(
        `单位ID已变更: ${oldUnitId} -> ${newUnitId}，正在重新连接MQTT...`
      );
      ElMessage.info("单位已切换，正在重新连接MQTT服务...");

      // 延迟执行重连，确保单位ID已更新
      setTimeout(async () => {
        try {
          const success = await mqttClient.reconnect();
          if (success) {
            ElMessage.success("MQTT重连成功，已更新订阅主题");
          } else {
            ElMessage.error("MQTT重连失败，请手动刷新页面");
          }
        } catch (error) {
          console.error("MQTT重连错误:", error);
          ElMessage.error("MQTT重连过程中发生错误");
        }
      }, 500);
    }
  }
);

// 初始化MQTT和告警服务
const initServices = async () => {
  // 清空重试计数
  connectionAttempts.value = 0;

  // 延迟初始化，确保DOM完全加载
  setTimeout(async () => {
    try {
      await connectMqtt();
      // 如果MQTT连接成功，再初始化告警服务
      if (mqttInitialized.value && !alarmInitialized.value) {
        await initAlarmService();
      }
    } catch (error) {
      console.error("服务初始化失败:", error);
      ElMessage.error(
        `服务初始化失败: ${error instanceof Error ? error.message : "未知错误"}`
      );
    }
  }, 1000);
};

// MQTT连接逻辑
const connectMqtt = async () => {
  if (mqttInitialized.value) {
    return true;
  }

  try {
    console.log("尝试连接MQTT服务...");
    const connected = await mqttClient.connect();
    if (connected) {
      console.log("MQTT连接成功");
      mqttInitialized.value = true;
      // ElMessage.success("MQTT服务连接成功");
      return true;
    } else {
      // 连接失败，尝试重连
      return retryConnection("连接失败");
    }
  } catch (error) {
    console.error("MQTT连接发生错误:", error);
    // 连接错误，尝试重连
    return retryConnection(error instanceof Error ? error.message : "连接错误");
  }
};

// 重试连接
const retryConnection = async (reason: string) => {
  connectionAttempts.value++;

  if (connectionAttempts.value <= maxRetries) {
    ElMessage.warning(
      `MQTT连接失败(${reason})，正在重试 (${connectionAttempts.value}/${maxRetries})...`
    );
    // 等待一段时间后重试
    await new Promise(resolve => setTimeout(resolve, 2000));
    return connectMqtt();
  } else {
    console.error(
      `MQTT连接失败，已达到最大重试次数 (${maxRetries})，原因: ${reason}`
    );
    ElMessage.error(`MQTT服务连接失败: ${reason}，请稍后手动刷新页面重试`);
    return false;
  }
};

// 初始化告警服务
const initAlarmService = async () => {
  if (alarmInitialized.value) {
    return;
  }

  try {
    console.log("初始化告警服务...");
    // 使用修改后的init方法检查初始化是否成功
    const success = alarmService.init();

    if (success) {
      alarmInitialized.value = true;
      console.log("告警服务初始化完成");
      // 不再单独监听MQTT告警数据，现在由告警服务统一处理
      // emitter.on("mqttAlarmData", alarmData => {
      //   console.log("收到MQTT告警数据:", alarmData);
      //   // 处理告警数据
      //   if (alarmData && typeof alarmData === "object") {
      //     try {
      //       // 这里根据实际告警数据结构处理
      //       alarmService.processAlarmData(alarmData);
      //     } catch (error) {
      //       console.error("处理MQTT告警数据失败:", error);
      //     }
      //   }
      // });
    } else {
      console.error("告警服务初始化失败，尝试重新初始化");

      // 尝试重新初始化，直接使用告警存储
      try {
        const alarmStore = useAlarmStoreHook();
        if (!alarmStore.isInitialized) {
          alarmStore.initAlarmListener();
          alarmInitialized.value = true;
          console.log("告警监听器直接初始化成功");
        }
      } catch (err) {
        console.error("直接初始化告警监听器失败:", err);
        ElMessage.warning("告警服务初始化失败，部分功能可能不可用");
      }
    }
  } catch (error) {
    console.error("告警服务初始化失败:", error);
    ElMessage.warning(
      `告警服务初始化失败: ${error instanceof Error ? error.message : "未知错误"}，部分功能可能不可用`
    );
  }
};

// 在组件挂载后初始化服务
onMounted(() => {
  console.log("导航栏组件已挂载，准备初始化服务...");
  initServices();
});

// 在组件卸载时清理资源
onUnmounted(() => {
  console.log("导航栏组件已卸载，清理资源...");

  // 移除MQTT状态变化监听器
  emitter.off("mqttStatusChange");

  // 如果有其他监听器，也应该在这里移除

  // 断开MQTT连接
  if (mqttInitialized.value) {
    try {
      mqttClient.disconnect();
      console.log("MQTT连接已断开");
    } catch (error) {
      console.error("断开MQTT连接失败:", error);
    }
  }
});

const router = useRouter();

const {
  layout,
  device,
  logout,
  onPanel,
  pureApp,
  username,
  userAvatar,
  avatarsStyle,
  updatePassword,
  toggleSideBar,
  toAccountSettings,
  getDropdownItemStyle,
  getDropdownItemClass
} = useNav();

const { t, locale, translationCh, translationEn } = useTranslationLang();

// 跳转到驾驶舱页面
const toCockpit = () => {
  router.push("/cockpit");
};

// 监听MQTT状态变化
emitter.on("mqttStatusChange", (status: string) => {
  mqttStatus.value = status;
  mqttConnected.value = status === MqttConnectionState.CONNECTED;
  console.log("MQTT状态变化:", status);
});

// 打开告警面板
const openAlarmPanel = () => {
  // 使用事件总线触发打开告警面板
  emitter.emit("openPanel", "alarm-center");

  // 如果有未读告警，标记为已读
  if (hasUnreadAlarms.value) {
    try {
      alarmService.markAllAsRead?.();
    } catch (error) {
      console.error("标记告警已读失败:", error);
    }
  }
};
</script>

<template>
  <div class="navbar bg-[#fff] shadow-xs shadow-[rgba(0,21,41,0.08)]">
    <LaySidebarTopCollapse
      v-if="device === 'mobile'"
      class="hamburger-container"
      :is-active="pureApp.sidebar.opened"
      @toggleClick="toggleSideBar"
    />

    <LaySidebarBreadCrumb
      v-if="layout !== 'mix' && device !== 'mobile'"
      class="breadcrumb-container"
    />

    <LayNavMix v-if="layout === 'mix'" />

    <div v-if="layout === 'vertical'" class="vertical-header-right">
      <!-- 驾驶舱按钮 -->
      <div class="cockpit-btn navbar-bg-hover" @click="toCockpit">
        <el-icon><IconifyIconOffline :icon="DashboardIcon" /></el-icon>
        <span>能源驾驶舱</span>
      </div>
      <!-- 当前单位 -->
      <LaySidebarUnit />
      <!-- MQTT状态和告警组件 -->
      <HeaderBadges />
      <!-- MQTT状态测试 -->
      <!-- <el-tooltip
        effect="dark"
        :content="`MQTT状态: ${mqttStatus}`"
        placement="bottom"
      >
        <div class="nav-tool-item">
          <IconifyIconOffline
            icon="link"
            width="16"
            class="nav-svg-icon"
            :color="mqttConnected ? '#67C23A' : '#F56C6C'"
          />
        </div>
      </el-tooltip> -->

      <!-- 菜单搜索 -->
      <LaySearch id="header-search" />
      <!-- 国际化 -->
      <!-- <el-dropdown id="header-translation" trigger="click">
        <GlobalizationIcon
          class="navbar-bg-hover w-[40px] h-[48px] p-[11px] cursor-pointer outline-hidden"
        />
        <template #dropdown>
          <el-dropdown-menu class="translation">
            <el-dropdown-item
              :style="getDropdownItemStyle(locale, 'zh')"
              :class="['dark:text-white!', getDropdownItemClass(locale, 'zh')]"
              @click="translationCh"
            >
              <IconifyIconOffline
                v-show="locale === 'zh'"
                class="check-zh"
                :icon="Check"
              />
              简体中文
            </el-dropdown-item>
            <el-dropdown-item
              :style="getDropdownItemStyle(locale, 'en')"
              :class="['dark:text-white!', getDropdownItemClass(locale, 'en')]"
              @click="translationEn"
            >
              <span v-show="locale === 'en'" class="check-en">
                <IconifyIconOffline :icon="Check" />
              </span>
              English
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown> -->

      <!-- 全屏 -->
      <LaySidebarFullScreen id="full-screen" />
      <!-- 消息通知 -->
      <!-- <LayNotice id="header-notice" /> -->
      <!-- 退出登录 -->
      <el-dropdown trigger="click">
        <span class="el-dropdown-link navbar-bg-hover select-none">
          <img :src="userAvatar" :style="avatarsStyle" />
          <p v-if="username" class="dark:text-white">{{ username }}</p>
        </span>
        <template #dropdown>
          <el-dropdown-menu class="logout">
            <el-dropdown-item @click="updatePassword">
              <IconifyIconOffline :icon="Password" style="margin: 5px" />
              修改密码
            </el-dropdown-item>
            <el-dropdown-item @click="toAccountSettings">
              <IconifyIconOffline
                :icon="AccountSettingsIcon"
                style="margin: 5px"
              />
              {{ t("buttons.pureAccountSettings") }}
            </el-dropdown-item>
            <el-dropdown-item @click="logout">
              <IconifyIconOffline
                :icon="LogoutCircleRLine"
                style="margin: 5px"
              />
              {{ t("buttons.pureLoginOut") }}
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
      <span
        class="set-icon navbar-bg-hover"
        :title="t('buttons.pureOpenSystemSet')"
        @click="onPanel"
      >
        <IconifyIconOffline :icon="Setting" />
      </span>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.navbar {
  width: 100%;
  height: 48px;
  overflow: hidden;

  .hamburger-container {
    float: left;
    height: 100%;
    line-height: 48px;
    cursor: pointer;
  }

  .vertical-header-right {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    min-width: 280px;
    height: 48px;
    color: #000000d9;

    .cockpit-btn {
      display: flex;
      align-items: center;
      height: 48px;
      padding: 0 15px;
      cursor: pointer;
      transition: all 0.3s;
      margin-right: 10px;
      border-radius: 4px;
      color: #fff;
      background-color: var(--el-color-primary);
      box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
      position: relative;
      overflow: hidden;
      font-weight: 600;

      &::after {
        content: "";
        position: absolute;
        top: -50%;
        left: -60%;
        width: 20%;
        height: 200%;
        background: rgba(255, 255, 255, 0.3);
        transform: rotate(30deg);
        transition: all 0.85s;
      }

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);

        &::after {
          left: 120%;
        }
      }

      &:active {
        transform: translateY(1px);
      }

      .el-icon {
        margin-right: 8px;
        font-size: 20px;
        filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.2));
      }

      span {
        font-size: 15px;
        letter-spacing: 0.5px;
      }
    }

    .el-dropdown-link {
      display: flex;
      align-items: center;
      justify-content: space-around;
      height: 48px;
      padding: 10px;
      color: #000000d9;
      cursor: pointer;

      p {
        font-size: 14px;
      }

      img {
        width: 22px;
        height: 22px;
        border-radius: 50%;
      }
    }

    .simulate-alarm {
      display: flex;
      align-items: center;
      height: 32px;
      margin: 0 8px;
      padding: 0 10px;
      border-radius: 4px;
      background-color: #f2f6fc;
      color: var(--el-color-primary);
      transition: all 0.3s;
      cursor: pointer;
      border: 1px solid #dcdfe6;

      &:hover {
        background-color: #ecf5ff;
        color: var(--el-color-primary-light-3);
      }

      &:active {
        transform: scale(0.98);
      }

      .simulate-text {
        margin-left: 4px;
        font-size: 12px;
      }
    }
  }

  .breadcrumb-container {
    float: left;
    margin-left: 16px;
  }
}

.translation {
  ::v-deep(.el-dropdown-menu__item) {
    padding: 5px 40px;
  }

  .check-zh {
    position: absolute;
    left: 20px;
  }

  .check-en {
    position: absolute;
    left: 20px;
  }
}

.logout {
  width: 120px;

  ::v-deep(.el-dropdown-menu__item) {
    display: inline-flex;
    flex-wrap: wrap;
    min-width: 100%;
  }
}

.mqtt-status {
  font-size: 12px;
  margin-left: 4px;
  padding: 2px 6px;
  border-radius: 10px;
  background-color: var(--el-color-info-light-9);
}

.nav-tool-item {
  display: flex;
  align-items: center;
  cursor: pointer;
  padding: 0 10px;
  height: 100%;
}

.alarm-badge {
  padding: 0 6px;
  border-radius: 4px;
  transition: background-color 0.3s;

  &:hover {
    background-color: #f5f5f5;
  }

  &:active {
    background-color: #e8e8e8;
    transform: scale(0.98);
  }
}

.alarm-badge-item {
  height: 100%;
  display: flex;
  align-items: center;

  :deep(.el-badge__content) {
    z-index: 1000;
  }
}
</style>
