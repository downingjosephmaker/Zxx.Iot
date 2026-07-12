import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 设备管理 */
const button = "设备";

/** 设备拓展属性（对应后端 Expand_DeviceInfo） */
export interface ExpandDeviceInfo {
  /** 硬件设备类型 */
  DeviceType?: number;
  /** 能耗类型(照明与插座用电/空调用电/动力用电/特殊用电/其他) */
  EnergyType?: string;
  /** 线路名称 */
  LineNum?: string;
  /** 设备标识(IMEI) */
  DeviceIMEI?: string;
  /** SIM标识(ICCID) */
  DeviceSim?: string;
  /** 关联视频id集合 */
  VideoIds?: string;
  /** 策略下发状态(运行时字段,编辑透传) */
  StrategySendStatus?: string;
  StrategySendTime?: string;
  /** 电流互感器变比 */
  CurrentTransformer?: number;
  /** 电压互感器变比 */
  VoltageTransformer?: number;
}

/** 设备（对应后端 DeviceFullInfo=DeviceInfoEntity+四个名称列） */
export interface DeviceInfoItem {
  /** 自增主键 */
  DeviceId: number;
  DeviceName?: string;
  DeviceTypeCode?: string;
  /** 类型全编码(由所选产品类型FullCode带出) */
  DeviceTypeFullCode?: string;
  /** 设备编号(全局唯一,服务端预检重复) */
  DeviceGuid?: string;
  /** 设备网关编号(DTU注册包匹配) */
  DeviceGateway?: string;
  BuildId?: number;
  DeptId?: number;
  DeviceIp?: string;
  DevicePort?: number;
  /** 串口通道号 */
  DeviceCom?: number;
  /** 设备协议地址 */
  DeviceAdr?: number;
  /** 是否采集(1采集/0不采集) */
  IsCollection?: number;
  /** 虚拟设备(1是/0否) */
  IsVirtual?: number;
  /** 运行时字段：最后在线时间 */
  LastOnlineTime?: string;
  /** 运行时字段：状态(2在线/1掉电/0离线) */
  DeviceState?: number;
  /** 运行时字段：告警状态(1告警/0正常) */
  DeviceAlarm?: number;
  /** 运行时字段：开关状态(1开/0关) */
  DeviceSwitch?: number;
  IconType?: string;
  SortBorder?: string;
  TreeLevel?: number;
  /** 上级设备ID(0=顶级) */
  ParentId?: number;
  FullName?: string;
  FullCode?: string;
  HasChild?: boolean;
  ExpandJson?: string;
  ExpandObject?: ExpandDeviceInfo;
  TenantId?: number;
  /** 列表增强列 */
  TenantName?: string;
  DeviceTypeName?: string;
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
}

/** 分页查询(返回含租户/类型名称的增强行) */
export const getListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/DeviceInfo/GetListByPage", {
    data
  });
};

/** 新增(服务端注入审计列与TenantId,DeviceGuid查重,树字段DAO计算) */
export const insert = (data?: object) => {
  storage.setItem("button", "新增" + button);
  return http.request<Result>("post", "/DeviceInfo/Insert", {
    data
  });
};

/** 修改(整行写回,运行时字段须透传) */
export const update = (data?: object) => {
  storage.setItem("button", "修改" + button);
  return http.request<Result>("post", "/DeviceInfo/Update", {
    data
  });
};

/** 删除(DAO按FullCode级联删除子设备) */
export const deleteByPk = (id: number) => {
  storage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/DeviceInfo/Delete", {
    params: { id }
  });
};

/** 下载导入模板(服务端刷新"设备类型"页签后返回静态相对路径,Result为纯字符串) */
export const downloadDeviceTemplate = () => {
  storage.setItem("button", "下载" + button + "导入模板");
  return http.request<Result>("post", "/DeviceInfo/DownloadDeviceTemplate");
};

/** Excel批量导入设备(单次上限100000行) */
export const deviceImport = (file: File) => {
  storage.setItem("button", "导入" + button);
  const formData = new FormData();
  formData.append("file", file);
  return http.request<Result>("post", "/DeviceInfo/DeviceImport", {
    data: formData,
    headers: { "Content-Type": "multipart/form-data" }
  });
};

export default {
  getListByPage,
  insert,
  update,
  deleteByPk,
  downloadDeviceTemplate,
  deviceImport
};
