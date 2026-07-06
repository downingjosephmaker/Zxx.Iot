import { http } from "@/utils/http";
import type { Result, ResultTable } from "../../type";

// VRF-V4空调用户参数 请求体类型
export interface NetAirConInfo {
  DeviceId: number; // 设备ID
}
export interface VRFV4User {
  // 空调信息集合
  DeviceInfoList: Array<NetAirConInfo>;
  // 开机使能(0:不允许开机 1:按策略执行 2:不执行策略 255:无动作)
  EnableOpen: number;
}

// VRF-V4空调策略设置 请求体类型
export interface VRFV4TimeInfo {
  enable: number; // 使能标志： 0表示不启用，1表示启用
  weeks: string; // 生效日：位运算值的字符串格式
  weekDays: string; // 生效日：逗号分隔的字符串格式，如"0,2,4"
  startHour: number; // 开始时
  startMinute: number; // 开始分
  endHour: number; // 结束时
  endMinute: number; // 结束分
}

export interface VRFV4TaskInfo {
  enable: number; // 使能标志： 0表示不启用，1表示启用 默认255
  weeks: string; // 生效日：位运算值的字符串格式
  weekDays: string; // 生效日：逗号分隔的字符串格式，如"0,2,4"
  startHour: number; // 开始时 默认255
  startMin: number; // 开始分 默认255
  airSwitch: number; // 开关 默认255
  airModel: number; // 模式 默认255
  airTemp: number; // 设置温度 默认255
  airSpeed: number; // 风速 默认255
}

export interface VRFV4Strategy {
  // 工作模式(0:调温1:温度2:时间3:定时) 多模式逗号隔开
  WorkModel: string;
  // 温度控制参数
  SummerOpenTemp: number; // 夏季开机温度值(16~31) 默认255
  WinterOpenTemp: number; // 冬季开机温度值(16~31) 默认255
  OpenTempEnable: number; // 温度模式开启温度使能 默认255
  // 制冷温度参数
  RefrigStartTemp: number; // 制冷开启温度 默认255
  RefrigOpenTemp: number; // 制冷开机温度 默认255
  RefrigCloseTemp: number; // 制冷关机温度 默认255
  // 制热温度参数
  HotStartTemp: number; // 制热开启温度 默认255
  HotOpenTemp: number; // 制热开机温度 默认255
  HotCloseTemp: number; // 制热关机温度 默认255
  // 温度模式开关机控制
  OpenCloseTemp: number; // 默认255
  // 时间控制参数
  TimesNum: number; // 时间段数量
  AutoOpen: number; // 进入时间段自动开机 默认255
  TaskCount: number; // 定时任务数
  // 设备和策略信息
  DeviceInfoList: Array<NetAirConInfo>; // 空调信息集合
  TimeInfoList: Array<VRFV4TimeInfo>; // 时间策略集合
  TaskInfoList: Array<VRFV4TaskInfo>; // 定时策略集合
}

export const DevNetStrategy = (data?: object) => {
  localStorage.setItem("button", "空调策略控制");
  return http.request<Result>("post", "/AirControl/DevNetStrategy", { data });
};

export const DevNetRun = (data?: object) => {
  localStorage.setItem("button", "空调设备控制");
  return http.request<Result>("post", "/AirControl/DevNetRun", { data });
};

export const DevNetParam = (data?: object) => {
  localStorage.setItem("button", "空调系统参数设置");
  return http.request<Result>("post", "/AirControl/DevNetParam", { data });
};

// VRF-V4空调 用户参数设置（POST）
export const DevVRFAirUser = (data: VRFV4User) => {
  localStorage.setItem("button", "VRF空调用户参数设置");
  return http.request<Result>("post", "/AirControl/DevVRFAirUser", { data });
};

// VRF-V4空调 策略设置（POST）
export const DevVRFStrategy = (data: VRFV4Strategy) => {
  localStorage.setItem("button", "VRF空调策略设置");
  return http.request<Result>("post", "/AirControl/DevVRFStrategy", { data });
};

export const DevNetAirUser = (data?: object) => {
  localStorage.setItem("button", "空调用户参数设置");
  return http.request<Result>("post", "/AirControl/DevNetAirUser", { data });
};

export const DevNetAirFtpUpdate = (data?: object) => {
  localStorage.setItem("button", "空调固件升级");
  return http.request<Result>("post", "/AirControl/DevNetAirFtpUpdate", {
    data
  });
};

export const DevNetCommonSet = (data?: object) => {
  localStorage.setItem("button", "其他通用控制");
  return http.request<Result>("post", "/AirControl/DevNetCommonSet", {
    data
  });
};
export const DxKzqControlParam = (data?: object) => {
  localStorage.setItem("button", "参数设置");
  return http.request<Result>("post", "/EquipControl/DxKzqControlParam", {
    data
  });
};
export const DxKzqControlTime = (data?: object) => {
  localStorage.setItem("button", "云端定时");
  return http.request<Result>("post", "/EquipControl/DxKzqControlTime", {
    data
  });
};

export const DxKzqControlCommonSet = (data?: object) => {
  localStorage.setItem("button", "其他通用控制");
  return http.request<Result>("post", "/EquipControl/DxKzqControlCommonSet", {
    data
  });
};

export const DxKzqControlFtpUpdate = (data?: object) => {
  localStorage.setItem("button", "固件升级");
  return http.request<Result>("post", "/EquipControl/DxKzqControlFtpUpdate", {
    data
  });
};

// 关联设备
export const DeviceRelevance = (data?: object) => {
  localStorage.setItem("button", "设备关联");
  return http.request<Result>("post", "/DeviceRelevance/SaveBatch", {
    data
  });
};
//查询关联设备
export const DeviceRelevanceList = (data?: object) => {
  localStorage.setItem("button", "设备关联");
  return http.request<ResultTable>("post", "/DeviceRelevance/GetListByPage", {
    data
  });
};

//删除关联设备
export const DeviceRelevanceDelete = (SnowId: string) => {
  localStorage.setItem("button", "删除设备关联");
  return http.request<Result>("post", "/DeviceRelevance/DeleteByPk", {
    params: { _SnowId: SnowId }
  });
};

//根据主键查询单条关联设备数据
export const DeviceRelevanceGetInfo = (SnowId: string) => {
  localStorage.setItem("button", "查询设备关联");
  return http.request<Result>("get", "/DeviceRelevance/GetInfoByPk", {
    params: { _SnowId: SnowId }
  });
};

//智慧插座
export const DevControlSocketRun = (data?: object) => {
  localStorage.setItem("button", "插座设备控制");
  return http.request<Result>("post", "/CD500A/DevControl_SocketRun", {
    data
  });
};

//智慧插座
export const DevControlSwitchRun = (data?: object) => {
  localStorage.setItem("button", "插座设备控制");
  return http.request<Result>("post", "/CD500A/DevControl_SwitchRun", {
    data
  });
};

//人感参数设置
export const DevControlHumanConfig = (data?: object) => {
  localStorage.setItem("button", "人感参数设置");
  return http.request<ResultTable>("post", "/CD500A/DevControl_HumanConfig", {
    data
  });
};

//VRF-V4网关参数设置
export const DevVRFParam = (data?: object) => {
  localStorage.setItem("button", "VRF网关参数设置");
  return http.request<Result>("post", "/AirControl/DevVRFParam", {
    data
  });
};

//智慧电气控制器
export const DxKzqControlRun = (data?: object) => {
  localStorage.setItem("button", "设备控制");
  return http.request<Result>("post", "/EquipControl/DxKzqControlRun", {
    data
  });
};

export default {
  DxKzqControlFtpUpdate,
  DxKzqControlCommonSet,
  DxKzqControlParam,
  DxKzqControlTime,
  DevNetStrategy,
  DevNetCommonSet,
  DevNetAirFtpUpdate,
  DevVRFAirUser,
  DevVRFStrategy,
  DevNetAirUser,
  DevNetParam,
  DevNetRun,

  DeviceRelevance,
  DeviceRelevanceList,
  DeviceRelevanceDelete,
  DeviceRelevanceGetInfo,

  DevControlHumanConfig,
  DevVRFParam,

  DxKzqControlRun,

  DevControlSocketRun,
  DevControlSwitchRun
};
