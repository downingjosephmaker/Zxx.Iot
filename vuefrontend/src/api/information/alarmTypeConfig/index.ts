import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备 */
/** 分页查询 */
const button = "告警相关配置";
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button);
  return http.request<ResultTable>(
    "post",
    "/DeviceTypeAlarmConfig/GetListByPage",
    {
      data
    }
  );
};
export const DelYuZhiBatch = data => {
  localStorage.setItem("button", "删除设备类型告警设置记录");
  return http.request<Result>("post", `/DeviceTypeAlarmConfig/DelYuZhiBatch`, {
    data
  });
};

/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button);
  return http.request<Result>(
    "post",
    "/DeviceTypeAlarmConfig/DeleteByPk?_SnowId=" + id
  );
};
/** 查询单个*/
export const GetInfoByPk = (id?: any) => {
  localStorage.setItem("button", "查询单个" + button);
  return http.request<Result>("get", "/AlarmConfig/GetInfoByPk?_Id=" + id);
};
/** 批量保存/修改 */
export const AddBatch = (data?: object, isUpdate = false) => {
  localStorage.setItem("button", "保存" + button);
  return http.request<Result>("post", "/DeviceTypeAlarmConfig/AddBatch", {
    data
  });
};

/** 参数列表 */
export const deviceParamList = (data?: object) => {
  localStorage.setItem("button", "获取设备参数列表");
  return http.request<Result>("post", "/DeviceParam/GetListByPage", {
    data
  });
};

export default {
  getListByPage,
  DelYuZhiBatch,
  deleteByPk,
  GetInfoByPk,
  AddBatch,
  deviceParamList
};
