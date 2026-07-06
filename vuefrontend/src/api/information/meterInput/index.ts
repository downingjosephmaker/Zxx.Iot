import { http } from "@/utils/http";
import type { ResultTable } from "../../type";
import type { Sconlist } from "../../type";

/** MeterInputSearch类型 */
export interface MeterInputSearch {
  /** 开始时间 */
  starttime?: string;
  /** 结束时间 */
  endtime?: string;
  /** 当前页 */
  page: number;
  /** 每页记录数 */
  pagesize: number;
  /** 建筑ID */
  BuildId?: number;
  /** 部门ID */
  DeptId?: number;
  /** 设备名称 */
  DevName?: string;
  /** 设备大类编号 */
  DevMasterTypeCode?: string;
  /** 设备类型集合 */
  DevTypeCodeList?: string[];
  /** 查询条件列表 */
  sconlist?: Sconlist[];
}

/** 设备 */
const button = "抄表录入";
const url = "/MeterInput";
/** 分页查询 */
export const GetListByPage = (data?: MeterInputSearch) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", url + "/GetListByPage", {
    data
  });
};

// [MeterInputModel{
// description:
// 抄表录入结构

// DeviceId	integer($int32)
// 设备id

// MeterTime	string
// nullable: true
// example: 2025-06-13 08:55:41
// 抄表时间

// MeterValue	string
// nullable: true
// example:
// 抄表值

// MeterRunTime	string
// nullable: true
// example: 2025-06-13 08:55:41
// 总工作时间h--设备类型=liuji有效

// }]
export const InputElecWaterMeter = (data?: MeterInputSearch) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", url + "/InputElecWaterMeter", {
    data
  });
};

// [MeterInputModel{
// description:
// 抄表录入结构
// DeviceId	integer($int32)
// 设备id
// MeterTime	string
// nullable: true
// example: 2025-06-13 08:55:41
// 抄表时间
// MeterValue	string
// nullable: true
// example:
// 抄表值
// MeterRunTime	string
// nullable: true
// example: 2025-06-13 08:55:41
// 总工作时间h--设备类型=liuji有效

// }]
export const InputGuangFuMeter = (data?: MeterInputSearch) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", url + "/InputGuangFuMeter", {
    data
  });
};
// [MeterInputModel{
// description:
// 抄表录入结构
// DeviceId	integer($int32)
// 设备id
// MeterTime	string
// nullable: true
// example: 2025-06-13 08:55:41
// 抄表时间
// MeterValue	string
// nullable: true
// example:
// 抄表值
// MeterRunTime	string
// nullable: true
// example: 2025-06-13 08:55:41
// 总工作时间h--设备类型=liuji有效
// }]
export const InputLiuJiMeter = (data?: MeterInputSearch) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", url + "/InputLiuJiMeter", {
    data
  });
};

export default {
  GetListByPage,
  InputElecWaterMeter,
  InputGuangFuMeter,
  InputLiuJiMeter
};
