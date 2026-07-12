import { http } from "@/utils/http";

/** 设备模拟（插件内嵌模拟能力，数据源复用平台点表） */
const base = "/Api/Simulator";

/** 取设备模拟元信息:点表快照+所属插件是否支持模拟+默认端口 */
export const getDeviceSimMeta = (deviceId: number) =>
  http.request("post", `${base}/GetDeviceSimMeta?deviceId=${deviceId}`);

/** 启动模拟(路由到设备所属插件) */
export const startSim = (data?: object) =>
  http.request("post", `${base}/StartSim`, { data });

/** 停止模拟 */
export const stopSim = (simId: string) =>
  http.request("post", `${base}/StopSim`, { data: { simId } });

/** 列出所有运行中模拟 */
export const listSims = () => http.request("post", `${base}/ListSims`);

/** 运行中注入/清除故障 */
export const injectFault = (data?: object) =>
  http.request("post", `${base}/InjectFault`, { data });

/** 启用/停用单设备采集(切IsCollection单字段+通知所属插件重建拓扑,即时生效) */
export const toggleCollection = (deviceId: number, isCollection: number) =>
  http.request<string>(
    "post",
    `/Api/DeviceInfo/ToggleCollection?deviceId=${deviceId}&isCollection=${isCollection}`
  );
