# 节能率管理系统

## 概述

节能率管理系统是一个用于管理和分析设备节能策略效果的综合管理平台。通过对比有策略和无策略期间的能耗数据，计算节能率，为节能决策提供数据支持。

## 功能特性

### 🎯 核心功能
- **节能率计算**: 自动计算策略实施前后的节能效果
- **策略管理**: 支持多种工作模式的策略配置
- **数据对比**: 对比有策略和无策略期间的能耗数据
- **设备管理**: 支持多设备的批量策略管理
- **执行监控**: 实时监控策略执行状态和日志

### 📊 数据管理
1. **策略数据**: 策略开始/结束日期、策略内容JSON
2. **能耗数据**: 策略期间和无策略期间的能耗统计
3. **设备信息**: 设备ID集合管理
4. **执行日志**: 策略执行过程的详细记录

### 🔧 策略配置
支持多种工作模式：
- **调温模式** (0): 根据温度自动调节
- **人感模式** (1): 基于人员感应控制
- **温度模式** (2): 纯温度控制策略
- **时间模式** (3): 基于时间段控制
- **手动模式** (4): 手动控制模式
- **计量模式** (5): 基于计量数据控制
- **断电模式** (7): 断电节能策略
- **机房省电** (8): 机房专用省电模式
- **临时模式** (9): 临时节能策略

## API接口

### 1. 批量保存
```typescript
POST /Api/EnergySavingRate/SaveBatch
```

### 2. 删除记录
```typescript
POST /Api/EnergySavingRate/DeleteByPk?_SnowId={id}
```

### 3. 查询单条数据
```typescript
GET /Api/EnergySavingRate/GetInfoByPk?_SnowId={id}
```

### 4. 策略保存
```typescript
POST /Api/EnergySavingRate/StrategySave?_SnowId={id}
```

### 5. 分页查询
```typescript
POST /Api/EnergySavingRate/GetListByPage
```

## 数据结构

### 节能率数据
```typescript
interface EnergySavingRateData {
  SnowId?: number;              // 主键ID
  UnitId?: number;              // 单位ID
  DeviceIds: string;            // 设备ID集合(|分隔)
  StrategyStart: string;        // 策略开始日期
  StrategyEnd: string;          // 策略结束日期
  IsStrategy?: boolean;         // 策略下发情况
  StrategyJson: string;         // 策略内容JSON
  StrategyEnergy?: number;      // 策略总能耗
  StrategyEnergyJson: string;   // 策略日能耗JSON
  UnStrategyStart: string;      // 无策略开始日期
  UnStrategyEnd: string;        // 无策略结束日期
  UnIsStrategy?: boolean;       // 无策略下发情况
  UnStrategyEnergy?: number;    // 无策略总能耗
  UnStrategyEnergyJson: string; // 无策略日能耗JSON
  IsEnable?: boolean;           // 是否启用
  OperatorNum?: number;         // 执行步骤序号
  OperatorLog: string;          // 执行日志
}
```

### 策略参数
```typescript
interface StrategySaveParams {
  WorkModel: string;            // 工作模式
  RefrigStartTemp?: number;     // 制冷开启温度
  RefrigOpenTemp?: number;      // 制冷开机温度
  RefrigCloseTemp?: number;     // 制冷关机温度
  HotStartTemp?: number;        // 制热开启温度
  HotOpenTemp?: number;         // 制热开机温度
  HotCloseTemp?: number;        // 制热关机温度
  HumanNum?: number;            // 人感人数
  HumanTime?: number;           // 人感延时
  SummerTemp?: number;          // 夏季判断温度
  WinterTemp?: number;          // 冬季判断温度
  DeviceInfoList: DeviceInfo[]; // 设备信息集合
  TimeInfoList: TimeInfo[];     // 时间策略集合
}
```

## 使用方法

### 1. 访问页面
```
http://localhost:8848/energy-saving-rate
```

### 2. 测试页面
```
http://localhost:8848/test/energy-saving-rate
```

### 3. 基本操作

#### 查询数据
- 设置开始时间和结束时间
- 点击"搜索"按钮查询数据
- 支持重置查询条件

#### 新增记录
- 点击"新增"按钮
- 填写必要的设备信息和时间范围
- 保存记录

#### 编辑记录
- 点击表格中的"编辑"按钮
- 修改相关信息
- 保存更改

#### 策略设置
- 点击表格中的"策略"按钮
- 配置工作模式和温度参数
- 保存策略配置

#### 查看详情
- 点击表格中的"查看"按钮
- 查看完整的记录信息
- 包括策略内容和执行日志

## 节能率计算

节能率计算公式：
```
节能率 = (无策略总能耗 - 策略总能耗) / 无策略总能耗 × 100%
```

- **正值**: 表示节能效果，数值越大节能效果越好
- **负值**: 表示能耗增加，需要优化策略
- **零值**: 表示策略无明显效果

## 界面特性

### 🎨 用户界面
- **响应式设计**: 适配不同屏幕尺寸
- **数据表格**: 支持排序、分页、搜索
- **对话框**: 模态对话框进行数据编辑
- **状态标识**: 用颜色区分不同状态

### 📱 交互体验
- **实时反馈**: 操作结果即时提示
- **数据验证**: 表单数据有效性检查
- **确认操作**: 危险操作需要确认
- **加载状态**: 异步操作显示加载状态

## 注意事项

1. **设备ID格式**: 多个设备ID用"|"分隔
2. **日期格式**: 使用YYYY-MM-DD格式
3. **JSON格式**: 策略内容和日能耗数据使用JSON格式存储
4. **权限控制**: 需要相应的API访问权限
5. **数据备份**: 重要数据建议定期备份

## 开发说明

### 技术栈
- **Vue 3**: 前端框架
- **TypeScript**: 类型安全
- **Element Plus**: UI组件库
- **Day.js**: 日期处理

### 文件结构
```
src/
├── api/energySavingRate/
│   └── index.ts              # API接口定义
├── views/energySavingRate/
│   └── index.vue             # 主页面组件
└── views/test/
    └── EnergySavingRateTest.vue # 测试页面
```

### 扩展开发
- 可以添加图表展示功能
- 支持数据导出功能
- 增加批量操作功能
- 添加数据统计分析
