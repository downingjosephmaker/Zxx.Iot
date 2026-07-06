import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";

/** 任务调度管理 */
const button = "任务调度";

// ==================== 类型定义 ====================

/**
 * 任务调度数据类型定义
 */
export interface ScheduleJobData {
  // 系统字段
  CreateId?: number;
  CreateTime?: string;
  CreateName?: string;
  UpdateId?: number;
  UpdateTime?: string;
  UpdateName?: string;
  SnowId?: number;

  // 业务字段
  JobName: string;              // 作业名称
  JobGroupName: string;         // 作业组名
  JobClassName: string;         // 作业类称
  JobDescription: string;       // 作业描述
  TriggerType: number;          // 触发器类型(0:Cron触发器，1:简单触发器)
  JobCron: string;              // Cron表达式
  IntervalSeconds?: number;     // 简单触发器执行间隔(秒)
  JobLimit: number;             // 作业限制(0:全部，1:主程序，2:副程序)
  JobLog: number;               // 是否记录日志(0:是，1:否)
  ExecuteCount?: number;        // 已执行次数
  JobStatus: number;            // 作业状态(0:停止，1:运行中，2:暂停)
  PrevFireTime?: string;        // 上次执行时间
  NextFireTime?: string;        // 下次执行时间
  JobRemark: string;            // 备注
}

/**
 * 任务调度查询参数
 */
export interface ScheduleJobQueryParams extends QueryTableParams {
  JobName?: string;             // 作业名称
  JobStatus?: number;           // 作业状态
  JobGroupName?: string;        // 作业组名
}

/**
 * Cron表达式示例
 */
export interface CronExample {
  expression: string;           // Cron表达式
  description: string;          // 描述
}

// ==================== API函数 ====================

/**
 * 分页查询任务调度数据
 * @param params 查询参数
 * @returns 分页数据
 */
export const getListByPage = (params: ScheduleJobQueryParams) => {
  localStorage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/Api/ScheduleJob/GetListByPage", {
    data: params
  });
};

/**
 * 根据主键查询任务详情
 * @param snowid 任务ID
 * @returns 任务信息
 */
export const getInfoByPk = (snowid: number) => {
  localStorage.setItem("button", "查询" + button + "详情");
  return http.request<Result>("post", `/Api/ScheduleJob/GetInfoByPk?snowid=${snowid}`);
};

/**
 * 添加任务
 * @param data 任务数据
 * @returns 添加结果
 */
export const insertJob = (data: ScheduleJobData) => {
  localStorage.setItem("button", "新增" + button);
  return http.request<Result>("post", "/Api/ScheduleJob/Insert", {
    data
  });
};

/**
 * 修改任务
 * @param data 任务数据
 * @returns 修改结果
 */
export const updateJob = (data: ScheduleJobData) => {
  localStorage.setItem("button", "修改" + button);
  return http.request<Result>("post", "/Api/ScheduleJob/Update", {
    data
  });
};

/**
 * 删除任务
 * @param snowid 任务ID
 * @returns 删除结果
 */
export const deleteJob = (snowid: number) => {
  localStorage.setItem("button", "删除" + button);
  return http.request<Result>("post", `/Api/ScheduleJob/Delete?snowid=${snowid}`);
};

/**
 * 任务状态变更(停止/运行)
 * @param snowid 任务ID
 * @param actiontype 动作(0:停止1:运行)
 * @returns 操作结果
 */
export const jobQiTingChange = (snowid: number, actiontype: number) => {
  localStorage.setItem("button", actiontype === 0 ? "停止" + button : "运行" + button);
  return http.request<Result>("post", `/Api/ScheduleJob/JobQiTingChange?snowid=${snowid}&actiontype=${actiontype}`);
};

/**
 * 任务状态变更(暂停/运行)
 * @param snowid 任务ID
 * @param actiontype 动作(2:暂停1:运行)
 * @returns 操作结果
 */
export const jobZanHuiChange = (snowid: number, actiontype: number) => {
  localStorage.setItem("button", actiontype === 2 ? "暂停" + button : "恢复" + button);
  return http.request<Result>("post", `/Api/ScheduleJob/JobZanHuiChange?snowid=${snowid}&actiontype=${actiontype}`);
};

/**
 * 立即执行任务
 * @param snowid 任务ID
 * @returns 执行结果
 */
export const jobExecute = (snowid: number) => {
  localStorage.setItem("button", "执行" + button);
  return http.request<Result>("post", `/Api/ScheduleJob/JobExecute?snowid=${snowid}`);
};

/**
 * 获取Cron表达式示例
 * @returns Cron表达式示例列表
 */
export const getCronExamples = () => {
  localStorage.setItem("button", "获取Cron示例");
  return http.request<Result>("post", "/Api/ScheduleJob/GetCronExamples");
};

/**
 * 条件保存（新增或修改）
 * @param data 任务数据
 * @returns 保存结果
 */
export const saveJob = (data: ScheduleJobData) => {
  if (!data.SnowId || data.SnowId === 0) {
    return insertJob(data);
  } else {
    return updateJob(data);
  }
};

// ==================== 默认导出 ====================

export default {
  getListByPage,
  getInfoByPk,
  insertJob,
  updateJob,
  deleteJob,
  jobQiTingChange,
  jobZanHuiChange,
  jobExecute,
  getCronExamples,
  saveJob
};
