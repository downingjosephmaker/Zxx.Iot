import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 北向转发目的地 */
const button = "北向目的地";

/** 北向转发目的地（对应后端 NorthboundSink 实体） */
export interface NorthboundSinkItem {
  SnowId: number | string;
  /** 目的地名称 */
  SinkName?: string;
  /** 目的地类型(1:MQTT 2:HTTP Webhook 3:Kafka预留) */
  SinkType?: number;
  /** 连接配置JSON(MQTT=SinkMqttConfig,HTTP=SinkHttpConfig) */
  ConnConfig?: string;
  /** 转发内容(1:仅遥测 2:仅告警 3:遥测+告警) */
  ContentMode?: number;
  /** 推送范围(0:全部 1:按产品类型编码 2:按设备ID) */
  ScopeType?: number;
  /** 范围清单JSON(按产品=类型编码数组,按设备=设备ID数组) */
  ScopeJson?: string;
  /** 是否启用 */
  IsEnable?: boolean;
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
}

/** 队列水位快照（对应后端 NorthboundForwardService.SinkStatus） */
export interface SinkStatusItem {
  SinkId: number | string;
  SinkName: string;
  Online: boolean;
  MemoryBacklog: number;
  CacheBacklog: number;
  SentCount: number;
  FailCount: number;
}

/** 测试结果（对应后端 NorthboundForwardService.SinkTestResult） */
export interface SinkTestResultItem {
  Success: boolean;
  Message: string;
  SampleTopic: string;
  SamplePayload: string;
}

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/NorthboundSink/GetListByPage", {
    data
  });
};

/** 批量保存（保存后转发器热重载） */
export const saveBatch = (data?: object) => {
  storage.setItem("button", "保存" + button);
  return http.request<Result>("post", "/NorthboundSink/SaveBatch", {
    data
  });
};

/** 根据主键删除（删除后转发器热重载） */
export const deleteByPk = (_SnowId: number | string) => {
  storage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/NorthboundSink/DeleteByPk", {
    params: { _SnowId }
  });
};

/** 队列水位快照（每目的地:在线状态/内存积压/落盘积压/累计计数） */
export const getStatus = () => {
  storage.setItem("button", "查询" + button + "水位");
  return http.request<Result>("get", "/NorthboundSink/GetStatus");
};

/** 样例报文预览（干跑不发送） */
export const getSample = (_SnowId: number | string) => {
  storage.setItem("button", "预览" + button + "样例");
  return http.request<Result>("get", "/NorthboundSink/GetSample", {
    params: { _SnowId }
  });
};

/** 测试连接并实际发送一条样例报文 */
export const postTestSend = (_SnowId: number | string) => {
  storage.setItem("button", "测试" + button);
  return http.request<Result>("post", "/NorthboundSink/PostTestSend", {
    params: { _SnowId }
  });
};

export default {
  getListByPage,
  saveBatch,
  deleteByPk,
  getStatus,
  getSample,
  postTestSend
};
