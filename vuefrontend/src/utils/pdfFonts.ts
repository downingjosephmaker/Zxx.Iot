/**
 * PDF中文字体支持工具
 * 用于解决jsPDF中文字体显示问题
 */

// 导入基础字体
import { jsPDF } from "jspdf";

// 字体加载状态
let fontLoaded = false;
let fontBase64Cache: string | null = null;

/**
 * 将字体文件转换为Base64格式
 */
async function loadFontAsBase64(): Promise<string> {
  if (fontBase64Cache) {
    return fontBase64Cache;
  }

  try {
    // 使用相对路径加载字体文件
    const fontUrl = "/src/assets/iconfont/SourceHanSansCN-Normal.ttf";
    console.log("正在加载字体文件:", fontUrl);

    const response = await fetch(fontUrl);
    if (!response.ok) {
      throw new Error(
        `字体文件加载失败: ${response.status} ${response.statusText}`
      );
    }

    const arrayBuffer = await response.arrayBuffer();
    const uint8Array = new Uint8Array(arrayBuffer);
    let binaryString = "";

    // 分批处理大文件，避免内存问题
    const chunkSize = 8192;
    for (let i = 0; i < uint8Array.length; i += chunkSize) {
      const chunk = uint8Array.slice(i, i + chunkSize);
      for (let j = 0; j < chunk.length; j++) {
        binaryString += String.fromCharCode(chunk[j]);
      }
    }

    fontBase64Cache = btoa(binaryString);
    console.log("字体文件转换完成，大小:", fontBase64Cache.length);
    return fontBase64Cache;
  } catch (error) {
    console.error("字体加载失败:", error);
    throw error;
  }
}

/**
 * 为PDF添加中文字体支持
 * @param pdf jsPDF实例
 */
export async function addFont(pdf: jsPDF): Promise<void> {
  if (fontLoaded) {
    try {
      pdf.setFont("SourceHanSansCN");
      return;
    } catch (error) {
      console.warn("字体设置失败，重新加载字体:", error);
      fontLoaded = false;
    }
  }

  try {
    console.log("开始加载中文字体...");

    // 加载字体文件为Base64
    const fontBase64 = await loadFontAsBase64();

    // 添加字体到jsPDF
    pdf.addFileToVFS("SourceHanSansCN-Normal.ttf", fontBase64);
    pdf.addFont("SourceHanSansCN-Normal.ttf", "SourceHanSansCN", "normal");

    // 验证字体是否成功添加
    const fontList = pdf.getFontList();
    console.log("当前可用字体:", fontList);

    if (fontList.SourceHanSansCN) {
      // 设置为默认字体
      pdf.setFont("SourceHanSansCN");
      fontLoaded = true;
      console.log("中文字体加载并设置成功");
    } else {
      throw new Error("字体添加失败，未在字体列表中找到");
    }
  } catch (error) {
    console.error("中文字体加载失败，使用默认字体:", error);
    // 回退到默认字体
    pdf.setFont("helvetica");
    // 重置字体加载状态，以便下次重试
    fontLoaded = false;
    fontBase64Cache = null;
    throw error; // 重新抛出错误，让调用者知道字体加载失败
  }
}

/**
 * 创建支持中文的PDF实例
 * @returns 支持中文的PDF实例
 */
export async function createChinesePDF(options?: any): Promise<jsPDF> {
  // 创建PDF实例
  const pdf = new jsPDF(options);

  // 添加中文字体支持
  await addFont(pdf);

  return pdf;
}

/**
 * 处理中文文本，确保在PDF中正确显示
 * @param text 要处理的文本
 * @returns 处理后的文本
 */
export function processChinese(text: string): string {
  if (!text) return "";

  // 现在有了真正的中文字体支持，直接返回原文本
  return text;
}

/**
 * 解析颜色为RGB值
 */
function parseColor(color: string | number[]): {
  r: number;
  g: number;
  b: number;
} {
  if (Array.isArray(color)) {
    return { r: color[0], g: color[1], b: color[2] };
  }

  if (typeof color === "string") {
    const hex = color.replace("#", "").toUpperCase();
    let r: number, g: number, b: number;

    if (hex.length === 3) {
      // 3位十六进制 #FFF -> #FFFFFF
      r = parseInt(hex.charAt(0) + hex.charAt(0), 16);
      g = parseInt(hex.charAt(1) + hex.charAt(1), 16);
      b = parseInt(hex.charAt(2) + hex.charAt(2), 16);
    } else if (hex.length === 6) {
      // 6位十六进制 #FFFFFF
      r = parseInt(hex.substring(0, 2), 16);
      g = parseInt(hex.substring(2, 4), 16);
      b = parseInt(hex.substring(4, 6), 16);
    } else {
      // 默认黑色
      r = g = b = 0;
    }

    // 确保值在有效范围内
    r = Math.max(0, Math.min(255, r || 0));
    g = Math.max(0, Math.min(255, g || 0));
    b = Math.max(0, Math.min(255, b || 0));

    return { r, g, b };
  }

  // 默认黑色
  return { r: 0, g: 0, b: 0 };
}

/**
 * 使用Canvas渲染中文文本为图片
 */
function renderChineseTextToImage(
  text: string,
  options: {
    fontSize?: number;
    color?: string;
    fontFamily?: string;
    maxWidth?: number;
  } = {}
): string {
  const {
    fontSize = 12,
    color = "#000000",
    fontFamily = '"Microsoft YaHei", "SimSun", "PingFang SC", "Hiragino Sans GB", Arial, sans-serif',
    maxWidth = 500
  } = options;
  try {
    // 创建临时canvas
    const canvas = document.createElement("canvas");
    const ctx = canvas.getContext("2d");

    if (!ctx) {
      console.error("无法创建Canvas上下文");
      return "";
    }

    // 设置高DPI支持
    const dpr = window.devicePixelRatio || 1;

    // 设置字体
    ctx.font = `${fontSize}px ${fontFamily}`;
    ctx.fillStyle = color;
    ctx.textBaseline = "top";

    // 测量文本尺寸
    const metrics = ctx.measureText(text);
    const textWidth = Math.min(metrics.width, maxWidth);
    const textHeight = fontSize * 1.4; // 行高

    // 设置canvas尺寸（考虑DPI）
    canvas.width = (textWidth + 20) * dpr; // 添加一些边距
    canvas.height = (textHeight + 10) * dpr;
    canvas.style.width = textWidth + 20 + "px";
    canvas.style.height = textHeight + 10 + "px";

    // 缩放上下文以匹配DPI
    ctx.scale(dpr, dpr);

    // 重新设置字体（canvas尺寸改变后需要重新设置）
    ctx.font = `${fontSize}px ${fontFamily}`;
    ctx.fillStyle = color;
    ctx.textBaseline = "top";
    ctx.textAlign = "left";

    // 不绘制背景，保持透明
    // ctx.fillStyle = "#ffffff";
    // ctx.fillRect(0, 0, textWidth + 20, textHeight + 10);

    // 绘制文本
    ctx.fillStyle = color;
    ctx.fillText(text, 10, 5);

    // 返回base64图片数据
    const dataURL = canvas.toDataURL("image/png");
    console.log(
      `Canvas渲染完成: "${text}", 尺寸: ${canvas.width}x${canvas.height}`
    );
    return dataURL;
  } catch (error) {
    console.error("Canvas渲染出错:", error);
    return "";
  }
}

/**
 * 添加中文文本到PDF
 * @param pdf PDF实例
 * @param text 文本内容
 * @param x X坐标
 * @param y Y坐标
 * @param options 选项
 */
export async function addChineseText(
  pdf: jsPDF,
  text: string,
  x: number,
  y: number,
  options: {
    fontSize?: number;
    color?: string | number[];
    align?: "left" | "center" | "right";
  } = {}
): Promise<void> {
  const { fontSize = 12, color = "#000000", align = "left" } = options;

  // 检查是否包含中文字符
  const hasChinese = /[\u4e00-\u9fff]/.test(text);
  if (!hasChinese) {
    // 如果不包含中文，使用普通的text方法
    pdf.setFontSize(fontSize);
    const { r, g, b } = parseColor(color);
    pdf.setTextColor(r, g, b);
    pdf.text(text, x, y, { align });
    return;
  }

  // 对于中文文本，直接使用Canvas渲染方法（更可靠）
  try {
    console.log(`渲染中文文本: "${text}"`);
    const colorStr = Array.isArray(color)
      ? `rgb(${color[0]}, ${color[1]}, ${color[2]})`
      : color;
    const imageData = renderChineseTextToImage(text, {
      fontSize,
      color: colorStr
    });

    if (imageData) {
      // 计算图片尺寸（转换为mm）
      const imgWidth = Math.min(text.length * fontSize * 0.6, 150);
      const imgHeight = fontSize * 0.8;

      // 根据对齐方式调整X坐标
      let adjustedX = x;
      if (align === "center") {
        adjustedX = x - imgWidth / 2;
      } else if (align === "right") {
        adjustedX = x - imgWidth;
      }

      // 添加图片到PDF
      pdf.addImage(
        imageData,
        "PNG",
        adjustedX,
        y - imgHeight,
        imgWidth,
        imgHeight
      );
      console.log(`中文文本渲染成功: "${text}"`);
    } else {
      // 回退方案
      console.warn(`Canvas渲染失败，使用回退方案: "${text}"`);
      pdf.setFontSize(fontSize);
      pdf.text(`[${text}]`, x, y, { align });
    }
  } catch (canvasError) {
    console.error("Canvas渲染失败:", canvasError);
    // 最后的回退方案
    pdf.setFontSize(fontSize);
    pdf.text(`[${text}]`, x, y, { align });
  }
}

/**
 * 渲染表格到PDF
 * @param pdf PDF实例
 * @param data 表格数据
 * @param x X坐标
 * @param y Y坐标
 * @param options 选项
 */
export async function addChineseTable(
  pdf: jsPDF,
  data: { headers: string[]; rows: string[][] },
  x: number,
  y: number,
  options: {
    fontSize?: number;
    headerColor?: string | number[];
    headerBgColor?: string | number[];
    textColor?: string | number[];
    borderColor?: string | number[];
    rowHeight?: number;
    colWidths?: number[];
    alternateRowColor?: string | number[];
  } = {}
): Promise<number> {
  console.log("开始渲染表格:", data);

  const {
    fontSize = 10,
    headerColor = "#ffffff",
    headerBgColor = [54, 179, 126],
    textColor = "#333333",
    borderColor = [200, 200, 200],
    rowHeight = 8,
    colWidths = [],
    alternateRowColor = [248, 249, 250]
  } = options;

  let currentY = y + 2; // 增加表头上方间距
  const { headers, rows } = data;

  // 计算列宽
  const totalWidth = 150; // 总宽度
  const calculatedColWidths =
    colWidths.length > 0
      ? colWidths
      : headers.map(() => totalWidth / headers.length);

  // 绘制表头
  const headerBg = parseColor(headerBgColor);
  pdf.setFillColor(headerBg.r, headerBg.g, headerBg.b);
  const borderRgb = parseColor(borderColor);
  pdf.setDrawColor(borderRgb.r, borderRgb.g, borderRgb.b);

  // 绘制表头背景
  pdf.rect(x, currentY, totalWidth, rowHeight + 2, "F"); // 增加表头高度

  // 绘制表头边框
  pdf.rect(x, currentY, totalWidth, rowHeight + 2, "S");

  // 绘制表头列分隔线和文字
  let currentX = x;
  for (let i = 0; i < headers.length; i++) {
    // 绘制列分隔线（除了最后一列）
    if (i > 0) {
      pdf.line(currentX, currentY, currentX, currentY + rowHeight + 2);
    }

    // 计算文字居中位置
    const cellWidth = calculatedColWidths[i];
    const textX = currentX + cellWidth / 2;

    await addChineseText(
      pdf,
      headers[i],
      textX,
      currentY + (rowHeight + 2) * 0.8, // 调整文字垂直位置，增加系数使文字往下移动
      {
        fontSize: fontSize + 1, // 表头字体稍大
        color: headerColor,
        align: "center"
      }
    );

    currentX += cellWidth;
  }
  currentY += rowHeight + 4; // 增加表头与内容之间的间距

  // 绘制数据行
  for (let rowIndex = 0; rowIndex < rows.length; rowIndex++) {
    const row = rows[rowIndex];

    // 绘制交替行背景
    if (rowIndex % 2 === 0 && alternateRowColor) {
      const altColor = parseColor(alternateRowColor);
      pdf.setFillColor(altColor.r, altColor.g, altColor.b);
      pdf.rect(x, currentY, totalWidth, rowHeight, "F");
    }

    // 绘制边框
    pdf.setDrawColor(borderRgb.r, borderRgb.g, borderRgb.b);
    pdf.rect(x, currentY, totalWidth, rowHeight, "S");

    // 绘制数据行列分隔线和文字
    currentX = x;
    for (
      let colIndex = 0;
      colIndex < row.length && colIndex < headers.length;
      colIndex++
    ) {
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

      await addChineseText(
        pdf,
        row[colIndex],
        textX,
        currentY + rowHeight * 0.7,
        {
          fontSize,
          color: textColor,
          align: textAlign
        }
      );

      currentX += cellWidth;
    }

    currentY += rowHeight;
  }

  // 绘制外边框（加粗）
  pdf.setDrawColor(borderRgb.r, borderRgb.g, borderRgb.b);
  pdf.setLineWidth(1);
  const tableHeight =
    (headers.length > 0 ? 1 : 0) * (rowHeight + 2) +
    rows.length * rowHeight +
    2;
  pdf.rect(x, y + 2, totalWidth, tableHeight, "S");

  // 重置线宽
  pdf.setLineWidth(0.5);

  return currentY; // 返回表格结束后的Y坐标
}
/**
 * 为表格数据处理中文
 * @param data 表格数据
 * @returns 处理后的表格数据
 */
export function processTableData(data: any[]): any[] {
  return data.map(row => {
    if (Array.isArray(row)) {
      return row.map(cell => {
        if (typeof cell === "string") {
          return processChinese(cell);
        }
        return cell;
      });
    }
    return row;
  });
}

// 导出所有函数，确保named export和default export保持一致
const pdfFonts = {
  createChinesePDF,
  processChinese,
  processTableData,
  addFont,
  addChineseText,
  addChineseTable
};

export default pdfFonts;
