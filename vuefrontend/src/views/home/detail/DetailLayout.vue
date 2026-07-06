<script setup lang="ts">
import { useRoute } from "vue-router";
import { ref, onMounted, watch, nextTick } from "vue";
import DashboardHeader from "./DashboardHeader.vue";

const route = useRoute();
const detailContent = ref<any>(null);
const slotRef = ref<HTMLElement | null>(null);

const title = ref("");
const dateType = ref<"day" | "month" | "year">("month");
const showDateSelector = ref(true);
const dateOptions = ref([
  { label: "日", value: "day" },
  { label: "月", value: "month" },
  { label: "年", value: "year" }
]);

// 新增属性，控制是否显示上月分析报告按钮
const showPrevMonthBtn = ref(false);
const currentDate = ref("");

// 根据路由参数设置标题
onMounted(async () => {
  title.value = route.meta.title || "详情页";

  // 等待DOM更新
  await nextTick();

  // 通过$el获取组件实例
  if (
    slotRef.value &&
    slotRef.value.children &&
    slotRef.value.children.length > 0
  ) {
    const childComponent =
      slotRef.value.children[0].__vueParentComponent?.subTree?.component
        ?.exposed;
    if (childComponent) {
      detailContent.value = childComponent;
      console.log("获取到子组件实例:", childComponent);

      // 检查子组件是否需要日期选择器
      if (typeof detailContent.value.needDateSelector !== "undefined") {
        showDateSelector.value = detailContent.value.needDateSelector;
      }

      // 检查子组件是否需要显示上月分析报告按钮
      if (typeof detailContent.value.showPrevMonthBtn !== "undefined") {
        showPrevMonthBtn.value = detailContent.value.showPrevMonthBtn;
      }

      // 获取当前日期
      if (typeof detailContent.value.currentDate !== "undefined") {
        currentDate.value = detailContent.value.currentDate;
      }
    } else {
      console.warn("无法获取子组件实例");
    }
  } else {
    console.warn("无法找到子组件元素");
  }
});

// 刷新页面数据，由子组件实现具体逻辑
const refreshData = () => {
  // 触发子组件的refreshData方法
  if (
    detailContent.value &&
    typeof detailContent.value.refreshData === "function"
  ) {
    console.log("调用子组件refreshData方法");
    detailContent.value.refreshData();
  }
};

// 日期类型变更时触发子组件的方法
const handleDateTypeChange = (newType: "day" | "month" | "year") => {
  dateType.value = newType;
  if (
    detailContent.value &&
    typeof detailContent.value.handleDateTypeChange === "function"
  ) {
    console.log("调用子组件handleDateTypeChange方法:", newType);
    detailContent.value.handleDateTypeChange(newType);
  }
};

// 处理上月分析报告事件
const handlePrevMonth = () => {
  if (
    detailContent.value &&
    typeof detailContent.value.handlePrevMonth === "function"
  ) {
    console.log("调用子组件handlePrevMonth方法");
    detailContent.value.handlePrevMonth();
  }
};

// 监听路由变化，更新标题
watch(
  () => route.meta.title,
  newTitle => {
    if (newTitle) {
      title.value = newTitle as string;
    }
  }
);
</script>

<template>
  <div class="detail-container">
    <div class="detail-wrapper">
      <!-- 使用新的共用头部组件 -->
      <DashboardHeader
        :title="title"
        :dateType="dateType"
        :dateOptions="dateOptions"
        :showDateSelector="showDateSelector"
        :showPrevMonthBtn="showPrevMonthBtn"
        :currentDate="currentDate"
        @update:dateType="handleDateTypeChange"
        @refresh="refreshData"
        @prevMonth="handlePrevMonth"
      />

      <!-- 内容区域，更现代化的样式 -->
      <div class="detail-content-wrapper">
        <div class="detail-content">
          <div ref="slotRef">
            <slot />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.detail-container {
  width: 100%;
  min-height: 100vh;
  background: linear-gradient(135deg, #0a0f1c 0%, #1a1f3a 50%, #0f1a2e 100%);
  background-attachment: fixed;
  color: #fff;
  overflow-x: hidden;
}

.detail-wrapper {
  height: 100%;
}

.detail-content-wrapper {
  position: relative;
  z-index: 1;
  min-height: calc(100vh - 125px);
}

.detail-content {
  border-radius: 12px;
  padding: 20px;
  min-height: calc(100vh - 140px);
  background: rgba(255, 255, 255, 0.03);
  backdrop-filter: blur(5px);
  -webkit-backdrop-filter: blur(5px);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.05);
  animation: fadeIn 0.5s ease-in-out;
  overflow: hidden;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* 添加动态背景效果，类似大屏 */
.detail-container::before {
  content: "";
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
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
    );
  z-index: 0;
}
</style>
