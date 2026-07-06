<template>
  <div class="header-badges">
    <!-- MQTT状态 -->
    <!-- <mqtt-status /> -->
    <!-- 告警图标 -->
    <alarm-badge />
  </div>
</template>

<script lang="ts" setup>
import MqttStatus from "./MqttStatus.vue";
import AlarmBadge from "./AlarmBadge.vue";
</script>

<style scoped>
.header-badges {
  display: flex;
  align-items: center;
  gap: 10px;
}
</style>
