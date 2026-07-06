<script setup lang="ts">
import DetailLayout from "./DetailLayout.vue";
import FloorEnergyBase from "@/components/FloorEnergyBase/index.vue";

const handleTabChange = (tab: "day" | "month" | "year") => {
  console.log("驾驶舱视图切换到:", tab);
  // 这里可以添加驾驶舱特有的处理逻辑
};
</script>

<template>
  <DetailLayout>
    <FloorEnergyBase :is-admin="false" @tab-change="handleTabChange">
      <!-- 如果需要添加驾驶舱特有的内容，可以通过插槽添加 -->
      <template #additional-content>
        <!-- 驾驶舱页面特有的内容 -->
      </template>
    </FloorEnergyBase>
  </DetailLayout>
</template>

<style scoped>
/* 驾驶舱页面特有样式 */
</style>
