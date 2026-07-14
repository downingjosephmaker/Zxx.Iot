/**
 * 报表运行态能力：查询区间下发、打印、导出 Excel / PDF。
 *
 * 依赖全部取自项目已有包（xlsx / jspdf / html2canvas / print-js），不新增任何依赖。
 * 这些能力只属于报表项目；组态项目的运行态不加载本模块的行为。
 */
import * as XLSX from "xlsx";
import jsPDF from "jspdf";
import html2canvas from "html2canvas";
import printJS from "print-js";

/**
 * 把查询条件栏选定的时间区间下发给历史数据集。
 * 写入 queryStart/queryEnd 后重新取数即按该区间出数（见 utils4.fetchChartData）。
 */
export function applyQueryRange(
  datasets: any[],
  start: Date | string,
  end: Date | string
) {
  datasets.forEach(ds => {
    if (ds.type === "iot" && ds.mode === "history") {
      ds.queryStart = typeof start === "string" ? start : start.toISOString();
      ds.queryEnd = typeof end === "string" ? end : end.toISOString();
    }
  });
}

/** 打印报表画布（只打画布，不打工具栏） */
export function printReport(canvas: HTMLElement) {
  printJS({
    printable: canvas,
    type: "html",
    targetStyles: ["*"],
    scanStyles: false
  });
}

/**
 * 导出画布内所有表格组件为 Excel（一个表格一个 sheet）。
 * 直接读渲染后的 <table> DOM——它就是用户所见的最终结果，无需再走一遍数据管线。
 */
export function exportTablesToExcel(canvas: HTMLElement, fileName: string) {
  const tables = canvas.querySelectorAll("table");
  if (!tables.length) return false;

  const book = XLSX.utils.book_new();
  tables.forEach((table, i) => {
    const sheet = XLSX.utils.table_to_sheet(table);
    XLSX.utils.book_append_sheet(book, sheet, `表格${i + 1}`);
  });
  XLSX.writeFile(book, `${fileName}.xlsx`);
  return true;
}

/**
 * html2canvas 的 CSS 解析器不认 `oklch()`，遇到就抛
 * "Attempting to parse an unsupported color function"。
 * 页面里唯一用到它的是 Tailwind v4 `@layer theme` 里的调色板变量声明（--color-*），
 * 组态/报表画布的组件并不引用这些变量，因此在**克隆文档**里把它们替换掉不影响导出视觉。
 * （原文档不受影响；项目里的画布缩略图截图早已采用同样处理。）
 */
const stripOklch = (clonedDoc: Document) => {
  clonedDoc.querySelectorAll("style").forEach(styleEl => {
    const css = styleEl.textContent || "";
    if (css.includes("oklch(")) {
      styleEl.textContent = css.replace(/oklch\([^)]*\)/g, "#000000");
    }
  });
};

/** 导出报表画布为 PDF（按画布长宽比选纸张方向，整页铺满） */
export async function exportCanvasToPdf(
  canvas: HTMLElement,
  fileName: string
) {
  const shot = await html2canvas(canvas, {
    backgroundColor: "#ffffff",
    scale: 2,
    useCORS: true,
    logging: false,
    onclone: stripOklch
  });
  const landscape = shot.width >= shot.height;
  const pdf = new jsPDF({
    orientation: landscape ? "landscape" : "portrait",
    unit: "pt",
    format: "a4"
  });
  const pageW = pdf.internal.pageSize.getWidth();
  const pageH = pdf.internal.pageSize.getHeight();
  // 等比缩放贴合页面并居中
  const ratio = Math.min(pageW / shot.width, pageH / shot.height);
  const w = shot.width * ratio;
  const h = shot.height * ratio;
  pdf.addImage(
    shot.toDataURL("image/jpeg", 0.92),
    "JPEG",
    (pageW - w) / 2,
    (pageH - h) / 2,
    w,
    h
  );
  pdf.save(`${fileName}.pdf`);
}
