import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 产品类型(设备类型) */
const button = "产品类型";

/** 设备类型拓展属性（对应后端 Expand_DeviceType） */
export interface ExpandDeviceType {
  /** 离线判断间隔(分钟)，0=不判断 */
  OfflineMinute: number;
  /** 支路数量 */
  SubChannels: number;
  /** 是否采集 */
  SbjgType: boolean;
  /** Mqtt通讯Key */
  MqttKey: string;
}

/** 设备类型（对应后端 DeviceTypeEntity；FullCode/FullName/TreeLevel 由服务端计算） */
export interface DeviceTypeItem {
  /** 主键编码 */
  TypeCode: string;
  TypeName?: string;
  SortBorder?: string;
  TreeLevel?: number;
  ParentId?: string;
  FullName?: string;
  FullCode?: string;
  HasChild?: boolean;
  IsEnable?: boolean;
  /** 拓展属性JSON(服务端由ExpandObject自动序列化) */
  ExpandJson?: string;
  ExpandObject?: ExpandDeviceType;
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
  /** 前端树形构造字段 */
  children?: DeviceTypeItem[];
}

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/DeviceType/GetListByPage", {
    data
  });
};

/** 新增（服务端自动计算 FullCode/FullName/TreeLevel，SortBorder留空自动生成） */
export const insert = (data?: object) => {
  storage.setItem("button", "新增" + button);
  return http.request<Result>("post", "/DeviceType/Insert", {
    data
  });
};

/** 修改（变更上级时服务端级联更新整个子树） */
export const update = (data?: object) => {
  storage.setItem("button", "修改" + button);
  return http.request<Result>("post", "/DeviceType/Update", {
    data
  });
};

/** 删除（服务端级联删除该类型及其所有子类型、类型告警配置） */
export const deleteByPk = (id: string) => {
  storage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/DeviceType/Delete", {
    params: { id }
  });
};

export default {
  getListByPage,
  insert,
  update,
  deleteByPk
};
