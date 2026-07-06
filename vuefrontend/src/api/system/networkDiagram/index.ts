import { http } from "@/utils/http";
import type { Result } from "../../type";

/** 查询拓扑图信息 */
export const GetAutoMapDataByBuild = (id?: string) => {
  localStorage.setItem("button", "查询拓扑图信息");
  return http.request<Result>(
    "get",
    "/DeviceInfo/GetAutoMapDataByBuild?buildid=" + id
  );
};

/** 保存拓扑图配置 */
export const SaveTopologyData = (buildId: string, data: any) => {
  localStorage.setItem("button", "保存拓扑图配置");
  return http.request<Result>("post", "/DeviceInfo/SaveTopologyData", {
    data: {
      buildId,
      topologyData: data
    }
  });
};

/** 获取拓扑图配置 */
export const GetTopologyConfig = (buildId: string) => {
  localStorage.setItem("button", "获取拓扑图配置");
  return http.request<Result>(
    "get",
    `/DeviceInfo/GetTopologyConfig?buildId=${buildId}`
  );
};

export default {
  GetAutoMapDataByBuild,
  SaveTopologyData,
  GetTopologyConfig
};
