import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备 */
/** 分页查询 */
const button = "告警类型";
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button);
  return http.request<ResultTable>("post", "/AlarmConfig/GetListByPage", {
    data
  });
};

/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button);
  return http.request<Result>("post", "/AlarmConfig/DeleteByPk?_Id=" + id);
};
/** 查询单个*/
export const GetInfoByPk = (id?: any) => {
  localStorage.setItem("button", "查询单个" + button);
  return http.request<Result>("get", "/AlarmConfig/GetInfoByPk?_Id=" + id);
};
/** 批量保存/修改 
 * [AlarmConfig{
description:	
告警类型管理

CreateId	integer($int32)
创建用户ID

CreateTime	string
nullable: true
example: 2025-06-13 08:55:41
创建时间

CreateName	string
nullable: true
example:
创建用户名称

UpdateId	integer($int32)
修改用户ID

UpdateTime	string
nullable: true
example: 2025-06-13 08:55:41
修改时间

UpdateName	string
nullable: true
example: 2025-06-13 08:55:41
修改用户名称

Id	integer($int32)
告警类型ID

EventType	string
nullable: true
example:
事件类型

AlarmGrade	string
nullable: true
example:
报警等级

AlarmType	string
nullable: true
example:
报警类型

ExampleFormula	string
nullable: true
example:
参考公式

TextTemplate	string
nullable: true
example:
文字模板

IsLimit	integer($int32)
是否越限(0:否 1:是)

IsNote	boolean
是否通知(0:否 1:是)

}]
*/
export const SaveBatch = (data?: object) => {
  localStorage.setItem("button", "保存" + button);
  return http.request<Result>("post", "/AlarmConfig/SaveBatch", {
    data
  });
};

/** 获取告警等级 */
export const GetDisAlarmGrade = () => {
  return http.request<Result>("get", "/AlarmConfig/GetDisAlarmGrade");
};

export default {
  getListByPage,
  deleteByPk,
  GetInfoByPk,
  SaveBatch,
  GetDisAlarmGrade
};
