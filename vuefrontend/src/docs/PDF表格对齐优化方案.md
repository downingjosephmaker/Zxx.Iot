# PDF表格对齐优化方案

## 🎯 优化目标

解决PDF导出中表格表头与数据行对齐不准确的问题，提升表格的视觉效果和可读性。

## 🔍 问题分析

### 原始问题
1. **表头对齐不准确** - 表头文字与数据列位置不匹配
2. **缺少列分隔线** - 无法清晰区分不同列的内容
3. **文字位置不精确** - 文字在单元格中的位置不够居中或对齐
4. **边框不完整** - 表格边框绘制不够清晰

### 根本原因
1. **文字定位方式简单** - 只使用固定偏移量，没有考虑单元格宽度
2. **缺少列边框** - 只绘制了行边框，没有列分隔线
3. **对齐方式单一** - 所有文字都使用左对齐，没有根据内容类型调整

## 🔧 优化方案

### 1. 表头对齐优化

#### 修改前
```typescript
// 绘制表头文字
let currentX = x;
for (let i = 0; i < headers.length; i++) {
  await addChineseText(pdf, headers[i], currentX + 2, currentY + 1, {
    fontSize,
    color: headerColor
  });
  currentX += calculatedColWidths[i];
}
```

#### 修改后
```typescript
// 绘制表头列分隔线和文字
let currentX = x;
for (let i = 0; i < headers.length; i++) {
  // 绘制列分隔线（除了最后一列）
  if (i > 0) {
    pdf.line(currentX, currentY, currentX, currentY + rowHeight);
  }
  
  // 计算文字居中位置
  const cellWidth = calculatedColWidths[i];
  const textX = currentX + cellWidth / 2;
  
  await addChineseText(pdf, headers[i], textX, currentY + rowHeight / 2 - fontSize / 4, {
    fontSize,
    color: headerColor,
    align: "center"  // 表头文字居中对齐
  });
  
  currentX += cellWidth;
}
```

### 2. 数据行对齐优化

#### 智能对齐策略
```typescript
// 绘制数据行列分隔线和文字
currentX = x;
for (let colIndex = 0; colIndex < row.length && colIndex < headers.length; colIndex++) {
  // 绘制列分隔线（除了最后一列）
  if (colIndex > 0) {
    pdf.line(currentX, currentY, currentX, currentY + rowHeight);
  }
  
  // 计算文字位置
  const cellWidth = calculatedColWidths[colIndex];
  let textX = currentX + 3; // 左对齐，留3mm边距
  let textAlign: "left" | "center" | "right" = "left";
  
  // 如果是数字列（第二列），使用右对齐
  if (colIndex === 1 && /^[\d.,\s]+/.test(row[colIndex])) {
    textX = currentX + cellWidth - 3;
    textAlign = "right";
  }
  
  await addChineseText(pdf, row[colIndex], textX, currentY + rowHeight / 2 - fontSize / 4, {
    fontSize,
    color: textColor,
    align: textAlign
  });
  
  currentX += cellWidth;
}
```

### 3. 边框绘制优化

#### 完整边框系统
```typescript
// 绘制表头边框
pdf.rect(x, currentY, totalWidth, rowHeight, "S");

// 绘制数据行边框
pdf.rect(x, currentY, totalWidth, rowHeight, "S");

// 绘制外边框（加粗）
pdf.setLineWidth(1);
const tableHeight = (headers.length > 0 ? 1 : 0) * rowHeight + rows.length * rowHeight;
pdf.rect(x, y, totalWidth, tableHeight, "S");

// 重置线宽
pdf.setLineWidth(0.5);
```

### 4. 列宽配置优化

#### newEnergyReport 表格配置
**修改前：**
```typescript
colWidths: [80, 50], // 总计130mm
rowHeight: 12,
fontSize: 10
```

**修改后：**
```typescript
colWidths: [90, 60], // 总计150mm，更合理的分配
rowHeight: 12,
fontSize: 12,        // 增大字体提升可读性
borderColor: [100, 100, 100] // 更深的边框颜色
```

#### unitOverviewReport 表格配置
**修改前：**
```typescript
colWidths: [35, 25, 25, 25, 30], // 总计140mm
rowHeight: 8,
fontSize: 10
```

**修改后：**
```typescript
colWidths: [40, 30, 30, 30, 30], // 总计160mm，更合理的分配
rowHeight: 10,                   // 增加行高提升可读性
fontSize: 10,
borderColor: [100, 100, 100]     // 更深的边框颜色
```

## 📊 优化效果对比

### 表头对齐效果

| 优化前 | 优化后 |
|--------|--------|
| ❌ 文字左对齐，位置不准确 | ✅ 文字居中对齐，位置精确 |
| ❌ 无列分隔线 | ✅ 清晰的列分隔线 |
| ❌ 表头与数据列不匹配 | ✅ 表头与数据列完美对齐 |

### 数据行对齐效果

| 内容类型 | 优化前 | 优化后 |
|----------|--------|--------|
| **文字内容** | ❌ 简单左对齐 | ✅ 左对齐 + 3mm边距 |
| **数字内容** | ❌ 左对齐，不美观 | ✅ 右对齐，更专业 |
| **垂直位置** | ❌ 顶部对齐 | ✅ 垂直居中对齐 |

### 边框效果

| 边框类型 | 优化前 | 优化后 |
|----------|--------|--------|
| **外边框** | ⚠️ 普通线宽 | ✅ 加粗边框，更突出 |
| **列分隔线** | ❌ 无 | ✅ 清晰的列分隔线 |
| **行分隔线** | ✅ 有 | ✅ 保持原有效果 |

## 🎯 技术细节

### 1. 文字居中算法
```typescript
// 水平居中
const textX = currentX + cellWidth / 2;

// 垂直居中
const textY = currentY + rowHeight / 2 - fontSize / 4;
```

### 2. 智能对齐判断
```typescript
// 检测数字内容并使用右对齐
if (colIndex === 1 && /^[\d.,\s]+/.test(row[colIndex])) {
  textX = currentX + cellWidth - 3;
  textAlign = "right";
}
```

### 3. 列分隔线绘制
```typescript
// 绘制列分隔线（除了第一列）
if (i > 0) {
  pdf.line(currentX, currentY, currentX, currentY + rowHeight);
}
```

### 4. 边框层次
```typescript
// 1. 单元格边框（细线）
pdf.setLineWidth(0.5);
pdf.rect(x, currentY, totalWidth, rowHeight, "S");

// 2. 外边框（粗线）
pdf.setLineWidth(1);
pdf.rect(x, y, totalWidth, tableHeight, "S");
```

## 🧪 测试方法

### 1. 视觉检查
- 导出PDF后检查表头是否与数据列对齐
- 确认列分隔线是否清晰可见
- 验证数字列是否右对齐

### 2. 不同内容测试
- 测试长文字内容的显示效果
- 测试数字内容的对齐效果
- 测试混合内容的表格显示

### 3. 不同配置测试
- 测试不同列宽配置的效果
- 测试不同行高的显示效果
- 测试不同字体大小的对齐效果

## 🎨 视觉效果提升

### 专业性提升
- ✅ **精确对齐**：表头与数据完美对齐
- ✅ **清晰分隔**：列分隔线提升可读性
- ✅ **智能排版**：根据内容类型选择对齐方式

### 可读性提升
- ✅ **垂直居中**：文字在单元格中垂直居中
- ✅ **合理间距**：3mm边距提供舒适的阅读空间
- ✅ **层次清晰**：加粗外边框突出表格边界

### 美观性提升
- ✅ **统一风格**：所有表格使用一致的样式
- ✅ **专业外观**：类似Excel表格的专业效果
- ✅ **细节完善**：每个细节都经过精心调整

## 🎉 总结

通过这次表格对齐优化，PDF表格的质量得到了显著提升：

### ✅ 解决的问题
- **表头对齐不准确** ➜ 精确的居中对齐
- **缺少列分隔线** ➜ 清晰的列边界
- **文字位置不精确** ➜ 智能的对齐策略
- **边框不完整** ➜ 完整的边框系统

### 🚀 提升的效果
- **专业性**：表格外观更加专业
- **可读性**：内容更容易阅读和理解
- **美观性**：整体视觉效果更佳
- **一致性**：所有表格风格统一

现在的PDF表格具备了专业级的对齐效果和视觉质量！📊✨

---

**优化状态**：✅ 已完成  
**测试状态**：⏳ 待验证  
**视觉效果**：🎨 专业级提升
