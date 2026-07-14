/**
 * 全量 Remix Icon（ri:*）图标集本地化生成器
 *
 * 背景：菜单图标由用户在「菜单管理」里自由配置，图标名存在数据库里，
 * 源码扫描永远扫不到 DB 里的图标名 → 这些图标在运行时会走 CDN（api.unisvg.com），
 * 内网/断网即漏图。
 *
 * 本脚本从本地 node_modules/@iconify/json 抽出完整 ri 图标集（3000+），
 * 剥掉 info/categories 等元数据后输出为 public/icons/ri.json。
 * 运行时由 src/components/ReIcon/src/riCollection.ts 同源 fetch + addCollection 注册，
 * 于是任意 ri:* 图标（含 DB 里配的）全部命中本地注册表，彻底不触发 CDN。
 *
 * 放 public/ 而非 src/：不进 JS 包、不拖慢首屏解析，浏览器可独立缓存。
 *
 * 用法：node build/gen-ri-iconset.mjs（已挂 package.json 的 predev / prebuild 钩子）
 */
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { resolve, dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, "..");
const SRC_JSON = join(
  ROOT,
  "node_modules",
  "@iconify",
  "json",
  "json",
  "ri.json"
);
const OUT_DIR = join(ROOT, "public", "icons");
const OUT_FILE = join(OUT_DIR, "ri.json");

const raw = JSON.parse(readFileSync(SRC_JSON, "utf8"));

// 只保留 Iconify addCollection 渲染所必需的字段，丢掉 info/categories/themes 等元数据
const collection = {
  prefix: raw.prefix,
  icons: raw.icons,
  aliases: raw.aliases ?? {},
  width: raw.width ?? 24,
  height: raw.height ?? 24
};

mkdirSync(OUT_DIR, { recursive: true });
writeFileSync(OUT_FILE, JSON.stringify(collection), "utf8");

const count =
  Object.keys(collection.icons).length + Object.keys(collection.aliases).length;
const kb = (Buffer.byteLength(JSON.stringify(collection)) / 1024).toFixed(1);
console.log(
  `[gen-ri-iconset] 已生成 public/icons/ri.json：${count} 个图标，${kb} KB`
);
