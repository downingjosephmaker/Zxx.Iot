<script setup lang="ts">
import DetailLayout from "./DetailLayout.vue";
import ChargingPileMonitorBase from "@/components/ChargingPileMonitorBase/index.vue";

defineOptions({
  name: "ChargingPileMonitorCockpit"
});
</script>

<template>
  <DetailLayout title="充电桩监控（驾驶舱）">
    <ChargingPileMonitorBase :dark="true" />
  </DetailLayout>
</template>
