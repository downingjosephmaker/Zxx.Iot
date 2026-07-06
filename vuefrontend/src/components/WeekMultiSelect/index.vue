<template>
  <div class="week-multi-select">
    <div class="selected-weeks">
      <el-tag
        v-for="(week, index) in selectedWeeks"
        :key="index"
        closable
        class="week-tag"
        @close="removeWeek(index)"
      >
        {{ formatWeekDisplay(week) }}
      </el-tag>
      <el-button
        v-if="selectedWeeks.length > 0"
        size="small"
        type="primary"
        plain
        @click="clearAll"
      >
        清空
      </el-button>
    </div>

    <div class="select-area">
      <el-date-picker
        v-model="currentWeek"
        type="week"
        format="第 ww 周"
        value-format="YYYY-MM-DD"
        :clearable="true"
        :shortcuts="shortcuts"
        :disabled-date="disabledDate"
        placeholder="选择周"
        @change="handleWeekChange"
      />
      <el-button
        v-if="currentWeek"
        size="small"
        type="primary"
        :disabled="isWeekSelected(currentWeek)"
        @click="addWeek"
      >
        添加
      </el-button>
    </div>

    <div class="weeks-range-display">
      <div
        v-for="(week, index) in formattedWeekRanges"
        :key="index"
        class="week-range-item"
      >
        {{ week }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue";
import dayjs from "dayjs";
import weekOfYear from "dayjs/plugin/weekOfYear";
import isoWeek from "dayjs/plugin/isoWeek";

// 导入周处理插件
dayjs.extend(weekOfYear);
dayjs.extend(isoWeek);

interface WeekData {
  date: string; // 选中的日期
  startDate: Date; // 周开始日期
  endDate: Date; // 周结束日期
}

const props = defineProps({
  modelValue: {
    type: Array,
    default: () => []
  },
  shortcuts: {
    type: Array,
    default: () => []
  },
  disabledDate: {
    type: Function,
    default: () => false
  }
});

const emit = defineEmits(["update:modelValue", "change"]);

// 当前选中的周
const currentWeek = ref<string | null>(null);

// 已选择的周
const selectedWeeks = ref<WeekData[]>([]);

// 监听 modelValue 变化
watch(
  () => props.modelValue,
  newVal => {
    if (Array.isArray(newVal) && newVal.length > 0) {
      // 如果外部传入的值变化，更新内部状态
      selectedWeeks.value = newVal.map(date => {
        const weekStart = dayjs(date).startOf("week").toDate();
        const weekEnd = dayjs(date).endOf("week").toDate();
        return {
          date,
          startDate: weekStart,
          endDate: weekEnd
        };
      });
    } else {
      selectedWeeks.value = [];
    }
  },
  { immediate: true }
);

// 格式化周显示
const formatWeekDisplay = (week: WeekData) => {
  const weekNum = dayjs(week.date).week();
  const year = dayjs(week.date).year();
  return `${year}年第${weekNum}周`;
};

// 格式化周范围
const formattedWeekRanges = computed(() => {
  return selectedWeeks.value.map(week => {
    return `${formatWeekDisplay(week)}: ${dayjs(week.startDate).format("YYYY年MM月DD日")} 至 ${dayjs(week.endDate).format("YYYY年MM月DD日")}`;
  });
});

// 处理周变更
const handleWeekChange = value => {
  if (!value) return;
  currentWeek.value = value;
};

// 添加周
const addWeek = () => {
  if (!currentWeek.value) return;

  // 检查是否已经选择了该周
  if (isWeekSelected(currentWeek.value)) return;

  const selectedDate = dayjs(currentWeek.value);
  const weekStart = selectedDate.startOf("week").toDate();
  const weekEnd = selectedDate.endOf("week").toDate();

  const newWeek: WeekData = {
    date: currentWeek.value,
    startDate: weekStart,
    endDate: weekEnd
  };

  selectedWeeks.value.push(newWeek);
  updateModelValue();

  // 清空当前选择
  currentWeek.value = null;
};

// 移除周
const removeWeek = (index: number) => {
  selectedWeeks.value.splice(index, 1);
  updateModelValue();
};

// 清空所有选择
const clearAll = () => {
  selectedWeeks.value = [];
  updateModelValue();
};

// 检查周是否已被选择
const isWeekSelected = (date: string) => {
  return selectedWeeks.value.some(week => {
    const weekNum = dayjs(week.date).week();
    const year = dayjs(week.date).year();
    const currentWeekNum = dayjs(date).week();
    const currentYear = dayjs(date).year();

    return weekNum === currentWeekNum && year === currentYear;
  });
};

// 更新模型值
const updateModelValue = () => {
  const dates = selectedWeeks.value.map(week => week.date);
  emit("update:modelValue", dates);

  const weekRanges = selectedWeeks.value.map(week => ({
    start: week.startDate,
    end: week.endDate,
    date: week.date
  }));
  emit("change", weekRanges);
};
</script>

<style lang="scss" scoped>
.week-multi-select {
  display: flex;
  flex-direction: column;
  gap: 10px;

  .selected-weeks {
    display: flex;
    flex-wrap: wrap;
    gap: 5px;
    margin-bottom: 10px;

    .week-tag {
      margin-right: 5px;
    }
  }

  .select-area {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .weeks-range-display {
    margin-top: 5px;

    .week-range-item {
      margin-bottom: 4px;
      color: var(--el-text-color-secondary);
      font-size: 13px;
    }
  }
}
</style>
