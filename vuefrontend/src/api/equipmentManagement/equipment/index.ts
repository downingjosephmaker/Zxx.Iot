import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备 */
const button = "设备";
/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/HtkDevice/GetListByPageAll", {
    data
  });
};
/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>("post", "/HtkDevice/DeleteByPk?_DeviceId=" + id);
};

export const deleteByPk2 = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>(
    "post",
    "/DeviceAlarmConfig/DeleteByPk?_SnowId=" + id
  );
};

/** 查询单个*/
export const GetInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>("get", "/HtkDevice/GetInfoByPk?_DeviceId=" + id);
};
/** 批量保存/修改 */
export const saveBatch = (data?: any) => {
  if (data?.DeviceId) {
    localStorage.setItem("button", "新增" + button + "");
    return http.request<Result>("post", "/HtkDevice/UpdateDevice", {
      data
    });
  } else {
    localStorage.setItem("button", "修改" + button + "");
    return http.request<Result>("post", "/HtkDevice/InsertDevice", {
      data
    });
  }
};

/** 保存设备阈值配置 */
export const SaveParamConfig = (data?: any) => {
  localStorage.setItem("button", "新增设备阈值");
  return http.request<Result>("post", "/DeviceAlarmConfig/Save", {
    data
  });
};
// "/HtkDeviceparamAlarmconfig/SaveParamConfigByDev",

export const SaveParamConfig2 = (data?: any) => {
  localStorage.setItem("button", "新增自定义报警阈值");
  return http.request<Result>("post", "/DeviceAlarmConfig/Save", {
    data
  });
};

/** 批量新增设备阈值 */
export const AddParamConfigBatch = (data?: any) => {
  localStorage.setItem("button", "批量新增设备阈值");
  return http.request<Result>(
    "post",
    "/HtkDeviceparamAlarmconfig/AddParamConfigBatch",
    {
      data
    }
  );
};
/** 删除设备阈值*/
export const HtkDeviceparamAlarmconfig = (id?: string) => {
  localStorage.setItem("button", "删除设备参数阈值");
  return http.request<Result>(
    "post",
    "/HtkDeviceparamAlarmconfig/DeleteByPk?_SnowId=" + id
  );
};

/** 组合告警 */
export const AlarmconfigList = (data?: any) => {
  localStorage.setItem("button", "组合告警列表");
  return http.request<Result>(
    "post",
    "/HtkDeviceparamAlarmconfig/GetListByPage",
    {
      data
    }
  );
};

/** 新增 */
export const insert = (data?: object) => {
  localStorage.setItem("button", "新增" + button + "");
  return http.request<Result>("post", "/HtkDevice/InsertDevice", {
    data
  });
};
/** 修改 */
export const update = (data?: object) => {
  localStorage.setItem("button", "修改" + button + "");
  return http.request<Result>("post", "/HtkDevice/UpdateDevice", {
    data
  });
};
/** 根据建筑id查询电路图 */
export const GetMapValueByBid = (id?: any) => {
  localStorage.setItem("button", "查询" + button + "根据建筑id查询电路图");
  return http.request<Result>(
    "get",
    "/HtkDevice/GetMapValueByBid?buildid=" + id
  );
};
/** 根据建筑ID和数据类型查询温度设备 */
export const GetTempList = (id?: any, datacategory = 1) => {
  localStorage.setItem("button", "根据建筑ID和数据类型查询温度设备");
  return http.request<Result>(
    "post",
    `/HtkDevice/GetTempList?buildid=${id}&datacategory=${datacategory}`
  );
};
/** 查询电力测温设备参数类别下拉框 */
export const GetTempParamTypeSelect = () => {
  localStorage.setItem("button", "查询电力测温设备参数类别下拉框");
  return http.request<Result>("get", `/HtkDeviceparam/GetTempParamTypeSelect`);
};

/** 设备阈值管理列表 */
export const GetYuZhiDeviceList = data => {
  localStorage.setItem("button", "查询设备阈值管理列表");
  return http.request<Result>("post", `/HtkDevice/GetYuZhiDeviceList`, {
    data
  });
};

/** 设备类型查询设备参数 */
export const GetYuZhiBatchList = devicetype => {
  localStorage.setItem("button", "查询设备阈值管理列表");
  return http.request<Result>(
    "post",
    `/DeviceTypeAlarmConfig/GetListByPage?devicetype=${devicetype}`
  );
};

export const DelYuZhiBatch = data => {
  localStorage.setItem("button", "删除设备类型告警设置记录");
  return http.request<Result>("post", `/DeviceTypeAlarmConfig/DelYuZhiBatch`, {
    data
  });
};

/** 设备阈值查询 */
export const GetYuZhiByBid = id => {
  localStorage.setItem("button", "设备阈值查询");
  return http.request<Result>(
    "get",
    `/HtkDeviceparam/GetYuZhiByBid?deviceid=${id}`
  );
};
/** 设备阈值保存 */
export const UpdateThresholdBatch = (data?: object) => {
  localStorage.setItem("button", "设备阈值保存");
  return http.request<Result>("post", `/HtkDeviceparam/UpdateThresholdBatch`, {
    data
  });
};
/** 获取空调实时数据 */
export const GetAirRealList = id => {
  localStorage.setItem("button", "获取空调实时数据");
  return http.request<Result>("get", `/HtkDevice/GetAirRealList?buildId=${id}`);
};
/** 空调控制 */
export const AirControl = (data?: object) => {
  localStorage.setItem("button", "空调控制");
  return http.request<Result>("post", `/DeviceControl/AirControl`, { data });
};
/** 设备查询 */
export const GetAlarmConfigDeviceList = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>(
    "post",
    "/DeviceAlarmConfig/GetAlarmConfigDeviceList",
    {
      data
    }
  );
};

// 查询组合参数配置
export const GetMoreAlarmConfigList = (id?: any) => {
  localStorage.setItem("button", "查询" + button);
  return http.request<ResultTable>(
    "post",
    "/DeviceAlarmConfig/GetMoreAlarmConfigList?deviceId=" + id
  );
};
// 查询参数告警配置
export const GetAlarmConfigParamList = (id?: any) => {
  localStorage.setItem("button", "查询参数告警配置");
  return http.request<ResultTable>(
    "post",
    "/DeviceAlarmConfig/GetAlarmConfigParamList?deviceId=" + id
  );
};
// 查询参数阈值
export const GetParamAlarmConfigList = (id?: any) => {
  localStorage.setItem("button", "查询参数阈值" + button);
  return http.request<ResultTable>(
    "post",
    "/DeviceAlarmConfig/GetParamAlarmConfigList?" + id
  );
};

export default {
  getListByPage,
  deleteByPk,
  deleteByPk2,
  GetInfoByPk,
  saveBatch,
  insert,
  update,
  GetMapValueByBid,
  GetTempList,
  GetTempParamTypeSelect,
  GetYuZhiDeviceList,
  GetYuZhiByBid,
  SaveParamConfig,
  SaveParamConfig2,
  UpdateThresholdBatch,
  HtkDeviceparamAlarmconfig,
  AddParamConfigBatch,
  GetYuZhiBatchList,
  AirControl,
  AlarmconfigList,
  GetAlarmConfigDeviceList,
  GetMoreAlarmConfigList,
  GetAlarmConfigParamList,
  DelYuZhiBatch,
  GetAirRealList
};
