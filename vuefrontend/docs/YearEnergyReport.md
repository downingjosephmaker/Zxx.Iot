# 年度能源报告组件 (YearEnergyReport)

## 概述

年度能源报告组件是基于月度能源报告组件开发的年度数据分析工具，用于展示和分析全年的能源使用情况，包括发电量、用电量、用水量、用氢量等多项指标的年度统计和同比分析。

## 功能特性

### 🎯 核心功能
- **年度数据展示**: 展示全年各项能源指标的汇总数据
- **同比分析**: 与去年同期数据进行对比分析
- **智能总结**: 基于数据自动生成年度能源使用总结
- **优化建议**: 提供年度节能优化建议和改进方案
- **可视化展示**: 通过进度条、卡片等形式直观展示数据

### 📊 数据指标
1. **年总发电量** - 全年光伏发电总量
2. **年总用电量** - 全年用电消耗总量
3. **年总用水量** - 全年用水消耗总量
4. **年总用氢量** - 全年氢能使用总量
5. **年均电力自给率** - 全年平均电力自给能力
6. **年碳排放总量** - 全年碳排放估算

### 🔧 技术特性
- **Vue 3 Composition API** - 现代化的响应式开发
- **TypeScript** - 类型安全的开发体验
- **Element Plus** - 企业级UI组件库
- **响应式设计** - 适配多种屏幕尺寸
- **深色主题** - 专业的能源监控界面风格

## API接口

### GetYearEnergyReport
```typescript
/**
 * 获取年度能耗报表数据
 * @param firstday 日期 格式：YYYY-MM-DD (年度报告使用年份的1月1日)
 * @returns 年度能耗报表数据
 */
export const GetYearEnergyReport = (firstday: string) => {
  return http.request<ReportResponse>(
    "get",
    `/ReportForm/GetYearEnergyReport?firstday=${firstday}`
  );
};
```

### 数据结构
```typescript
interface YearEnergyReportData {
  Peroid: string;           // 报告期间，如"2024年"
  Profile: EnergyProfile[]; // 能源概览数据
  Detail: EnergyDetail[];   // 详细对比数据
  ElecRemain: ElecRemain;   // 电力自给率数据
  ElecYOYTop: ElecTop[];    // 年度同比Top设备
  CO2Cost: CO2Cost[];       // 碳排放估算
}
```

## 使用方法

### 1. 基本使用
```vue
<template>
  <YearEnergyReport />
</template>

<script setup>
import YearEnergyReport from "@/views/home/detail/YearEnergyReport.vue";
</script>
```

### 2. 通过路由访问
```typescript
// 跳转到年度报告页面
router.push({
  path: "/YearEnergyReport",
  query: { date: "2024-01-01" }
});
```

### 3. 组件方法调用
```vue
<template>
  <YearEnergyReport ref="yearReportRef" />
  <el-button @click="refreshData">刷新数据</el-button>
</template>

<script setup>
import { ref } from "vue";

const yearReportRef = ref();

const refreshData = () => {
  yearReportRef.value?.refreshData();
};
</script>
```

## 组件属性

### 暴露的方法
- `refreshData()` - 刷新年度数据
- `handleDateTypeChange(type)` - 处理日期类型变更
- `handlePrevYear()` - 跳转到全屏年度报告
- `needDateSelector` - 是否需要日期选择器
- `showPrevYearBtn` - 是否显示上年分析报告按钮
- `currentDate` - 当前选择的日期

## 路由配置

```typescript
// 年度报告详情页
{
  path: "/YearEnergyReport",
  name: "YearEnergyReport",
  component: () => import("@/views/home/detail/YearEnergyReport.vue"),
  meta: {
    title: "能耗年度分析报告",
    showLink: false
  }
}

// 年度报告全屏页
{
  path: "/YearEnergyReportFullscreen",
  name: "YearEnergyReportFullscreen",
  component: () => import("@/views/home/detail/YearEnergyReport.vue"),
  meta: {
    title: "能耗年度分析报告",
    showLink: false
  }
}
```

## 样式定制

组件使用深色主题设计，主要颜色：
- 主色调：`#1976d2` (蓝色)
- 背景色：`rgba(13, 41, 71, 0.1)` (深蓝色透明)
- 文字色：`#e0f2ff` (浅蓝色)
- 强调色：`#4fc3f7` (亮蓝色)

## 开发调试

### 模拟数据
组件内置了模拟数据功能，在开发环境下如果API返回失败会自动使用模拟数据：

```typescript
// 开发环境下自动启用模拟数据
if (process.env.NODE_ENV !== "production") {
  setMockData();
}
```

### 测试页面
访问 `/test/year-energy-report` 可以查看组件的测试页面。

## 注意事项

1. **日期格式**: 年度报告使用年份的1月1日作为参数，如`2024-01-01`
2. **数据兼容**: 与月度报告使用相同的API接口结构，确保数据兼容性
3. **性能优化**: 大数据量时建议使用分页或虚拟滚动
4. **错误处理**: 组件内置了完善的错误处理和降级机制

## 更新日志

### v1.0.0 (2024-01-20)
- 初始版本发布
- 基于月度报告组件开发
- 支持年度数据展示和分析
- 集成智能总结和建议功能
