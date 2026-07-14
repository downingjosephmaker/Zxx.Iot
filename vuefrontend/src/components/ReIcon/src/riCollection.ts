/**
 * 全量 Remix Icon（ri:*）图标集的运行时注册。
 *
 * 数据来自 public/icons/ri.json（由 build/gen-ri-iconset.mjs 从本地 @iconify/json 生成，
 * 随产物一起部署，同源加载）。注册后任意 ri:* 图标——包括菜单管理里配进数据库、
 * 源码扫描器看不见的那些——都命中本地注册表，不再回落 CDN。
 *
 * 幂等：并发调用共享同一个 Promise，只 fetch 一次。
 */
import { addCollection } from "@iconify/vue";

export interface RiCollection {
  prefix: string;
  icons: Record<string, { body: string }>;
  aliases?: Record<string, { parent: string }>;
  width?: number;
  height?: number;
}

let loading: Promise<string[]> | null = null;
let iconNames: string[] = [];

/** 已注册的全部 ri 图标名（形如 ri:home-line）；未加载完成时为空数组 */
export const getRiIconNames = () => iconNames;

/** 加载并注册全量 ri 图标集，返回图标名列表。重复调用复用同一次请求。 */
export function ensureRiCollection(): Promise<string[]> {
  if (loading) return loading;

  loading = fetch(`${import.meta.env.BASE_URL}icons/ri.json`)
    .then(res => {
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      return res.json() as Promise<RiCollection>;
    })
    .then(collection => {
      addCollection(collection);
      iconNames = [
        ...Object.keys(collection.icons ?? {}),
        ...Object.keys(collection.aliases ?? {})
      ]
        .map(name => `${collection.prefix}:${name}`)
        .sort();
      return iconNames;
    })
    .catch(err => {
      // 加载失败不阻断业务：图标退回 CDN/占位，控制台留痕即可
      console.error(
        "[riCollection] 全量 ri 图标集加载失败，图标可能显示为空白",
        err
      );
      loading = null;
      return [];
    });

  return loading;
}
