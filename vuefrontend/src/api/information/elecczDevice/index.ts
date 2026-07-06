import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备 */
const button = "设备";
const url = "/ElecczDevice";
/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", url + "/GetListByPage", {
    data
  });
};
/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>("post", url + "/DeleteByPk?elecid=" + id);
};

/** 查询单个*/
export const GetInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>("get", url + "/GetInfoByPk?_Id=" + id);
};

/** 批量保存/修改 */
export const saveBatch = (data?: any) => {
  if (data?.DeviceId) {
    localStorage.setItem("button", "新增" + button + "");
    return http.request<Result>("post", url + "/saveBatch", {
      data
    });
  } else {
    localStorage.setItem("button", "修改" + button + "");
    return http.request<Result>("post", url + "/saveBatch", {
      data
    });
  }
};

/** 保存设备阈值配置 */
export const SaveParamConfig = (data?: any) => {
  localStorage.setItem("button", "新增设备阈值");
  return http.request<Result>("post", "/HtkDevice/SaveParamConfig", {
    data
  });
};

export const SaveParamConfig2 = (data?: any) => {
  localStorage.setItem("button", "新增自定义报警阈值");
  return http.request<Result>("post", "/HtkDevice/SaveParamConfigByDev", {
    data
  });
};

/** 批量新增设备阈值 */
export const AddParamConfigBatch = (data?: any) => {
  localStorage.setItem("button", "批量新增设备阈值");
  return http.request<Result>("post", "/HtkDevice/AddParamConfigBatch", {
    data
  });
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
    `/HtkDevice/GetYuZhiBatchList?devicetype=${devicetype}`
  );
};
/** 设备类型 */
export const GetYuZhiDeviceType = () => {
  localStorage.setItem("button", "查询设备类型");
  return http.request<Result>("post", `/HtkDevice/GetYuZhiDeviceType`);
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

/** 插座通用控制 */
export const DevNetCommonSet = (data?: object) => {
  localStorage.setItem("button", "插座通用控制");
  return http.request<Result>("post", `/DeviceControl/DevNetCommonSet`, {
    data
  });
};

/** 插座系统参数*/
export const DevNetParam = (data?: object) => {
  localStorage.setItem("button", "插座系统参数");
  return http.request<Result>("post", `/DeviceControl/DevNetParam`, { data });
};

/** 插座其他系统参数 */
export const DevNetAirUser = (data?: object) => {
  localStorage.setItem("button", "插座其他系统参数");
  return http.request<Result>("post", `/DeviceControl/DevNetAirUser`, { data });
};

/** 插座闹钟策略 */
export const DevNetStrategy = (data?: object) => {
  localStorage.setItem("button", "插座闹钟策略");
  return http.request<Result>("post", `/DeviceControl/DevNetStrategy`, {
    data
  });
};

/** 插座预存电量 */
export const DevNetStoredEnergy = (data?: object) => {
  localStorage.setItem("button", "插座预存电量");
  return http.request<Result>("post", `/DeviceControl/DevNetStoredEnergy`, {
    data
  });
};

/** 插座倒计时 */
export const DevNetCountdown = (data?: object) => {
  localStorage.setItem("button", "插座倒计时");
  return http.request<Result>("post", `/DeviceControl/DevNetCountdown`, {
    data
  });
};

/** 查询插座闹钟策略 */
export const GetElecczClock = id => {
  localStorage.setItem("button", "查询插座闹钟策略");
  return http.request<Result>(
    "get",
    `/ElecczDevice/GetElecczClock?elecid=` + id
  );
};

export default {
  getListByPage,
  deleteByPk,
  GetInfoByPk,
  saveBatch,
  insert,
  update,
  GetMapValueByBid,
  GetTempList,
  GetTempParamTypeSelect,
  GetYuZhiDeviceList,
  GetYuZhiDeviceType,
  GetYuZhiByBid,
  SaveParamConfig,
  SaveParamConfig2,
  UpdateThresholdBatch,
  HtkDeviceparamAlarmconfig,
  AddParamConfigBatch,
  GetYuZhiBatchList,
  AirControl,
  AlarmconfigList,
  GetAirRealList,
  DevNetCommonSet,
  DevNetParam,
  DevNetAirUser,
  DevNetStrategy,
  DevNetStoredEnergy,
  DevNetCountdown,
  GetElecczClock
};
