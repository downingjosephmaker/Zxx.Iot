import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../type";
import { storage } from "@/utils/storage";

/** 规则联动 */
const button = "规则联动";

/** 规则联动（对应后端 LinkageRule 实体） */
export interface LinkageRuleItem {
  SnowId: number | string;
  /** 规则名称 */
  RuleName?: string;
  /** 触发类型(1:点位变化 2:告警产生 3:告警恢复 4:定时cron 5:设备上线 6:设备离线) */
  TriggerType?: number;
  /** 触发设备ID(0=任意设备) */
  TriggerDeviceId?: number;
  /** 触发参数编码(点位变化型限定参数,空=任意) */
  TriggerParamCode?: string;
  /** 触发cron表达式(定时型专用) */
  TriggerCron?: string;
  /** 条件表达式(空=恒真) */
  ConditionFormula?: string;
  /** 生效时间窗JSON(空=全天) */
  TimeRanges?: string;
  /** 动作类型(1:下发命令 2:写虚拟点位 3:发通知 4:Webhook) */
  ActionType?: number;
  /** 动作配置JSON */
  ActionConfig?: string;
  /** 冷却秒数 */
  CooldownSeconds?: number;
  /** 是否启用 */
  IsEnable?: boolean;
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
}

/** 漏斗指标（对应后端 RuleLinkageService.RuleMetrics，键为规则SnowId字符串） */
export interface RuleMetricsItem {
  Matched: number;
  Passed: number;
  Failed: number;
  ActionOk: number;
  ActionFail: number;
}

/** 试运行结果（对应后端 RuleLinkageService.LinkageDryRunResult） */
export interface LinkageDryRunResultItem {
  /** 规则是否存在且启用 */
  Found: boolean;
  RuleName: string;
  /** 当前是否在生效时间窗内 */
  InWindow: boolean;
  /** 条件表达式变量快照(最新值缓存缺失的变量不出现) */
  Variables: Record<string, number>;
  /** 条件表达式当前求值结果(空条件=恒真) */
  ConditionPass: boolean;
  /** 冷却剩余秒数(0=可执行) */
  CooldownRemainSeconds: number;
  ActionType: number;
}

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  storage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/LinkageRule/GetListByPage", {
    data
  });
};

/** 批量保存（保存后引擎热重载） */
export const saveBatch = (data?: object) => {
  storage.setItem("button", "保存" + button);
  return http.request<Result>("post", "/LinkageRule/SaveBatch", {
    data
  });
};

/** 根据主键删除（删除后引擎热重载） */
export const deleteByPk = (_SnowId: number | string) => {
  storage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/LinkageRule/DeleteByPk", {
    params: { _SnowId }
  });
};

/** 漏斗指标快照（每规则matched/passed/failed/action计数，进程内累计） */
export const getMetrics = () => {
  storage.setItem("button", "查询" + button + "指标");
  return http.request<Result>("get", "/LinkageRule/GetMetrics");
};

/** 规则试运行（干跑无副作用：评估时间窗/条件/冷却，不执行动作） */
export const getDryRun = (_SnowId: number | string, deviceid = 0) => {
  storage.setItem("button", "试运行" + button);
  return http.request<Result>("get", "/LinkageRule/GetDryRun", {
    params: { _SnowId, deviceid }
  });
};

export default {
  getListByPage,
  saveBatch,
  deleteByPk,
  getMetrics,
  getDryRun
};
