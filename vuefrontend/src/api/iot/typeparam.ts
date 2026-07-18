import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 点表(设备类型参数) */
const button = "点表配置";

/** 状态值映射项（对应后端 Expand_ParamStatusValue） */
export interface StatusValueItem {
  StatusKey: number;
  StatusValue: string;
}

/** 设备类型参数（对应后端 DeviceTypeParamEntity，30+列点表配置） */
export interface DeviceTypeParamItem {
  SnowId: number | string;
  /** 所属产品类型编码 */
  DeviceTypeCode?: string;
  /** 设备路数(总路,1路/A,2路/B,3路/C) */
  SubChannel?: string;
  ParamCode?: string;
  ParamName?: string;
  /** 参数分类名称(电流/电压等,运行数据页分组) */
  ParamTypeName?: string;
  /** 参数地址(Modbus寄存器地址复用此列) */
  ParamAddr?: number;
  /** 修正公式(如 a*0.1) */
  ParamFormula?: string;
  /** 值类型(数值/状态) */
  ValueType?: string;
  /** 状态值集合JSON(服务端由ExpandStatusValues自动序列化) */
  StatusValues?: string;
  /** 状态值集合(结构化,写入时框架序列化进StatusValues) */
  ExpandStatusValues?: StatusValueItem[];
  ValueUnit?: string;
  ParamMaxValue?: number;
  ParamMinValue?: number;
  /** 最大跳变量(绝对差) */
  ParamChangeValue?: number;
  /** 合理范围过滤开关(越界丢弃) */
  RangeFilterEnable?: boolean;
  /** 幅度过滤开关 */
  AmplitudeFilterEnable?: boolean;
  /** 最大跳变百分比(0=不启用) */
  MaxAmplitudePercent?: number;
  /** 连续异常容错开关 */
  ContinuousFilterEnable?: boolean;
  /** 连续异常次数阈值(默认3) */
  MaxContinuousCount?: number;
  IsShow?: boolean;
  IsMainShow?: boolean;
  IsSet?: boolean;
  IsPeak?: boolean;
  IsReport?: boolean;
  DecimalDigit?: number;
  IsCustomAlarm?: boolean;
  /** 采集功能码(0:不采集,1/2/3/4:Modbus读区) */
  CollectFuncCode?: number;
  /** 采集数据类型(空=uint16) */
  CollectDataType?: string;
  /** 字节序(空=ABCD) */
  CollectByteOrder?: string;
  /** 位偏移(-1整字,>=0按位取布尔) */
  CollectBitOffset?: number;
  /** 占用寄存器数(0按类型推导,bcd/string须显式) */
  CollectRegLength?: number;
  /** 是否可写(FC03经FC06/16下发) */
  CollectWritable?: boolean;
  /** 采集节点标识(OPC UA NodeId) */
  CollectNodeId?: string;
  /** 是否告警源(非0即告警,由平台告警引擎裁决) */
  IsAlarmSource?: boolean;
  /** 告警源关联告警类型ID */
  AlarmConfigId?: number;
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
}

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/DeviceTypeParam/GetListByPage", {
    data
  });
};

/** 批量保存(SnowId=0新增) */
export const saveBatch = (data?: object) => {
  storage.setItem("button", "保存" + button);
  return http.request<Result>("post", "/DeviceTypeParam/SaveBatch", {
    data
  });
};

/** 根据主键删除 */
export const deleteByPk = (_SnowId: number | string) => {
  storage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/DeviceTypeParam/DeleteByPk", {
    params: { _SnowId }
  });
};

/** 点表JSON导入(组态ZtTypeJson格式;该产品类型已有点表时服务端拒绝,需先删除) */
export const paramAddByType = (file: File, typecode: string) => {
  storage.setItem("button", "导入" + button);
  const formData = new FormData();
  formData.append("file", file);
  return http.request<Result>("post", "/DeviceTypeParam/ParamAddByType", {
    params: { typecode },
    data: formData,
    headers: { "Content-Type": "multipart/form-data" }
  });
};

export default {
  getListByPage,
  saveBatch,
  deleteByPk,
  paramAddByType
};
