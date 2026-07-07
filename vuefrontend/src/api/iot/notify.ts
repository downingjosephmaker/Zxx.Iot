import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 通知渠道 */
const button = "通知渠道";

/** 通知渠道（对应后端 NotifyChannel 实体） */
export interface NotifyChannelItem {
  SnowId: number | string;
  /** 渠道名称 */
  ChannelName?: string;
  /** 渠道类型(1:邮件 2:Webhook 3:钉钉机器人 4:企微机器人 5:短信预留) */
  ChannelType?: number;
  /** 目标地址(邮件外发接口/Webhook地址/机器人Webhook) */
  TargetUrl?: string;
  /** 密钥(钉钉加签secret等,空=不加签) */
  Secret?: string;
  /** 接收人(邮件收件人/短信手机号,逗号分隔) */
  Receivers?: string;
  /** 告警等级过滤(逗号分隔,只通知命中等级;空=全部) */
  GradeFilter?: string;
  /** 升级梯队(0=第一梯队立即;1/2/3=未Ack未恢复按15/30/60分钟渐进升级) */
  EscalationLevel?: number;
  /** 是否启用 */
  IsEnable?: boolean;
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
  return http.request<ResultTable>("post", "/NotifyChannel/GetListByPage", {
    data
  });
};

/** 批量保存（保存后通知服务热重载） */
export const saveBatch = (data?: object) => {
  storage.setItem("button", "保存" + button);
  return http.request<Result>("post", "/NotifyChannel/SaveBatch", {
    data
  });
};

/** 根据主键删除（删除后通知服务热重载） */
export const deleteByPk = (_SnowId: number | string) => {
  storage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/NotifyChannel/DeleteByPk", {
    params: { _SnowId }
  });
};

export default {
  getListByPage,
  saveBatch,
  deleteByPk
};
