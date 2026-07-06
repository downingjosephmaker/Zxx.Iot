# PDF中文字体支持使用说明

## 🎯 概述

本项目已集成了完整的PDF中文字体支持方案，使用思源黑体（SourceHanSansCN-Normal.ttf）解决PDF导出中文乱码问题。

## 📁 文件结构

```
src/
├── assets/iconfont/
│   └── SourceHanSansCN-Normal.ttf    # 中文字体文件
├── utils/
│   └── pdfFonts.ts                   # 中文字体支持工具
├── types/
│   └── assets.d.ts                   # 字体文件类型声明
└── views/reportForms/
    ├── pdfTest/                      # PDF测试页面
    └── unitOverviewReport/           # 示例报表页面
```

## 🚀 快速使用

### 1. 基础用法

```javascript
import { createChinesePDF, addChineseText } from '@/utils/pdfFonts';

// 创建支持中文的PDF
const pdf = await createChinesePDF();

// 添加中文文本
await addChineseText(pdf, '中文标题', 105, 20, {
  fontSize: 18,
  align: 'center',
  color: '#333333'
});

// 保存PDF
pdf.save('中文报表.pdf');
```

### 2. 在报表中使用

```javascript
// 在你的hook文件中导入
import { createChinesePDF, addChineseText } from '@/utils/pdfFonts';

async function exportPDFWithChinese() {
  try {
    // 创建PDF
    const pdf = await createChinesePDF();
    
    // 添加标题
    await addChineseText(pdf, '报表标题', 105, 20, {
      fontSize: 18,
      align: 'center'
    });
    
    // 添加内容
    await addChineseText(pdf, '报表内容...', 20, 40, {
      fontSize: 12
    });
    
    // 添加图表（如果有）
    if (chartInstance) {
      const chartImage = chartInstance.getDataURL({
        type: 'png',
        pixelRatio: 2,
        backgroundColor: '#fff'
      });
      pdf.addImage(chartImage, 'PNG', 20, 60, 170, 100);
    }
    
    pdf.save('报表.pdf');
  } catch (error) {
    console.error('PDF导出失败:', error);
  }
}
```

## 🔧 API 参考

### createChinesePDF(options?)

创建支持中文的PDF实例。

**参数:**
- `options` (可选): jsPDF配置选项

**返回:** Promise<jsPDF>

### addChineseText(pdf, text, x, y, options?)

向PDF添加中文文本。

**参数:**
- `pdf`: jsPDF实例
- `text`: 要添加的文本
- `x`: X坐标 (mm)
- `y`: Y坐标 (mm)
- `options`: 配置选项
  - `fontSize`: 字体大小 (默认: 12)
  - `color`: 文字颜色 (默认: '#000000')
  - `align`: 对齐方式 ('left' | 'center' | 'right', 默认: 'left')

### processChinese(text)

处理中文文本，确保正确显示。

**参数:**
- `text`: 要处理的文本

**返回:** string

## 🎨 示例页面

访问 `/reportForms/pdfTest` 查看完整的测试示例，包括：

1. 基础PDF导出（显示乱码对比）
2. 中文PDF导出（使用本地字体）
3. Canvas中文PDF（备用方案）

## 📝 注意事项

1. **字体加载**: 首次使用时会自动加载字体文件，可能需要几秒钟
2. **性能**: 字体文件较大（约10MB），建议在生产环境中优化加载
3. **兼容性**: 支持所有现代浏览器
4. **错误处理**: 如果字体加载失败，会自动回退到默认字体

## 🔍 故障排除

### 问题1: 字体加载失败
**解决方案**: 检查字体文件路径是否正确，确保 `SourceHanSansCN-Normal.ttf` 存在于 `src/assets/iconfont/` 目录

### 问题2: 中文仍显示乱码
**解决方案**: 确保使用 `addChineseText` 函数而不是 `pdf.text`，并等待字体加载完成

### 问题3: PDF文件过大
**解决方案**: 考虑使用压缩后的字体文件或按需加载字体子集

## 🚀 进阶用法

### 自定义字体颜色

```javascript
// 十六进制颜色
await addChineseText(pdf, '红色文字', 20, 40, {
  color: '#ff0000'
});

// RGB数组
await addChineseText(pdf, '蓝色文字', 20, 60, {
  color: [0, 0, 255]
});
```

### 多行文本处理

```javascript
const lines = ['第一行', '第二行', '第三行'];
let yPos = 40;

for (const line of lines) {
  await addChineseText(pdf, line, 20, yPos);
  yPos += 15; // 行间距
}
```

## 📞 技术支持

如有问题，请检查：
1. 控制台错误信息
2. 字体文件是否存在
3. 网络连接是否正常
4. 浏览器兼容性

---

**版本**: 1.0.0  
**更新时间**: 2024-08-08  
**维护者**: 开发团队
