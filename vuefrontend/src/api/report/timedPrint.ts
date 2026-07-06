import { http } from "@/utils/http";
import type { ResultLC } from "@/api/type";

/** 模板 */
const button = "定时生成报表";
/** 定时生成报表列表 */
export const getListByPage = (data: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>(
    "get",
    `/dcJobTask/task/list?page=${data.page}&size=${data.size}&jobName=${data.jobName}`
  );
  // return http.request<ResultLC>("get"," /dcJobTask/task/list",{
  //   data
  // }
  // );
};

/** 根据id 获取报表详情 */
export const getTaskById = id => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>("get", "/dcJobTask/task/" + id);
};
/** 报表新增 */
export const addTaskList = data => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>("post", "/dcJobTask/task", {
    data
  });
};
/** 报表修改 */
export const updateTaskList = data => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>("post", "/dcJobTask/task/update", {
    data
  });
};
/** 报表删除 */
export const delTaskList = id => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultLC>("post", "/dcJobTask/task/delete/" + id);
};

export default {
  getListByPage,
  getTaskById,
  addTaskList,
  updateTaskList,
  delTaskList
};
