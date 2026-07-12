import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 告警屏蔽 */
const button = "告警屏蔽";

/** 告警屏蔽规则（对应后端 AlarmMask 实体） */
export interface AlarmMaskItem {
  SnowId: number | string;
  /** 屏蔽对象类型(1:全局 2:租户 3:建筑 4:设备类型 5:单设备 6:告警等级) */
  MaskScopeType?: number;
  /** 屏蔽对象ID(租户/建筑/设备为ID,设备类型为编码,告警等级为等级名;全局为空) */
  ScopeId?: string;
  /** 屏蔽模式(1:永久 2:一次性时间段 3:周期性时间窗) */
  MaskMode?: number;
  /** 一次性起始时间(模式2专用) */
  StartTime?: string;
  /** 一次性结束时间(模式2专用) */
  EndTime?: string;
  /** 周期时间窗JSON(模式3专用) */
  TimeRanges?: string;
  /** 屏蔽动作(1:完全屏蔽不入库 2:静默入库打标不通知 3:降级) */
  MaskAction?: number;
  /** 降级目标等级(动作3专用) */
  DowngradeGrade?: string;
  /** 屏蔽原因 */
  Reason?: string;
  /** 操作人 */
  OperatorName?: string;
  /** 自动失效时间(空=不失效) */
  ExpireAt?: string;
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
  return http.request<ResultTable>("post", "/AlarmMask/GetListByPage", {
    data
  });
};

/** 批量保存（保存后屏蔽引擎热重载） */
export const saveBatch = (data?: object) => {
  storage.setItem("button", "保存" + button);
  return http.request<Result>("post", "/AlarmMask/SaveBatch", {
    data
  });
};

/** 根据主键删除（删除后屏蔽引擎热重载） */
export const deleteByPk = (_SnowId: number | string) => {
  storage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/AlarmMask/DeleteByPk", {
    params: { _SnowId }
  });
};

export default {
  getListByPage,
  saveBatch,
  deleteByPk
};
