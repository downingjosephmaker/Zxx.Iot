<template>
  <div class="mqtt-status">
    <el-tag :type="statusType" effect="dark" size="small">
      MQTT: {{ status }}
    </el-tag>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, computed, onMounted, onUnmounted } from "vue";
import { emitter } from "@/utils/mitt";
import { useMqttClient, MqttConnectionState } from "@/plugins/mqtt";

export default defineComponent({
  name: "MqttStatus",
  setup() {
    const status = ref<string>(MqttConnectionState.DISCONNECTED);
    const mqttClient = useMqttClient();

    // 状态颜色映射
    const statusType = computed(() => {
      switch (status.value) {
        case MqttConnectionState.CONNECTED:
          return "success";
        case MqttConnectionState.CONNECTING:
          return "warning";
        case MqttConnectionState.RECONNECTING:
          return "warning";
        case MqttConnectionState.DISCONNECTED:
        default:
          return "danger";
      }
    });

    // 监听MQTT状态变化
    const handleStatusChange = (newStatus: string) => {
      status.value = newStatus;
    };

    onMounted(() => {
      // 获取当前状态
      status.value = mqttClient.getStatus();

      // 订阅状态变化事件
      emitter.on("mqttStatusChange", handleStatusChange);
      // 移除自动连接逻辑，由导航栏组件统一管理
    });

    onUnmounted(() => {
      // 移除事件监听
      emitter.off("mqttStatusChange", handleStatusChange);
    });

    return {
      status,
      statusType
    };
  }
});
</script>

<style scoped>
.mqtt-status {
  display: inline-flex;
  align-items: center;
  margin-left: 10px;
}
</style>
