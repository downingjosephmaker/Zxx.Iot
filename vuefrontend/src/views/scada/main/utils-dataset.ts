/**
 * utils-dataset.ts - 数据集辅助函数
 *
 * index.vue 已超 100KB 上限，数据集相关新逻辑收敛于此，主文件只留委托行。
 */
import type { Ref } from "vue";
import { getDeviceLatest } from "@/api/iot/monitor";

/**
 * 项目加载后把持久化的 datasets 回灌到编辑器数据集列表。
 * 修复：datasetList 初始为 mock，保存后刷新页面数据集即丢。
 */
export const rehydrateDatasetList = (
  projectData: Ref<any>,
  datasetList: Ref<any[]>
) => {
  const saved = projectData.value?.datasets;
  if (Array.isArray(saved) && saved.length) {
    datasetList.value = [...saved];
  }
};

/**
 * IoT数据集预览：拉设备全部点位最新值，按数据集勾选点位过滤。
 * 返回 {ParamCode: {name, value, unit, ts}} 结构，供绑定预览面板展示。
 */
export const previewIotDataset = async (dataset: any) => {
  const data = await getDeviceLatest(dataset.deviceId);
  if (!data.Status) {
    throw new Error(data.Message || "查询设备最新值失败");
  }
  const points = JSON.parse(data.Result) as {
    ParamCode: string;
    ParamName?: string;
    Value?: number | null;
    ValueStr?: string | null;
    Ts?: string;
  }[];
  const metaMap = new Map<string, { ParamName?: string; ValueUnit?: string }>(
    (dataset.points || []).map((p: any) => [p.ParamCode, p])
  );
  const filtered = metaMap.size
    ? points.filter(p => metaMap.has(p.ParamCode))
    : points;
  const result: Record<string, unknown> = {};
  filtered.forEach(p => {
    result[p.ParamCode] = {
      name: p.ParamName || metaMap.get(p.ParamCode)?.ParamName || p.ParamCode,
      value: p.ValueStr ?? p.Value ?? null,
      unit: metaMap.get(p.ParamCode)?.ValueUnit || "",
      ts: p.Ts
    };
  });
  return result;
};
