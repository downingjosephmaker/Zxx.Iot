import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";

/** 用户 */

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "分页查询用户");
  return http.request<ResultTable>("post", "/Sysuser/GetListByPage", {
    data
  });
};

/** 新增 */
export const insert = (data?: object) => {
  localStorage.setItem("button", "新增用户");
  return http.request<Result>("post", "/Sysuser/InsertUser", {
    data
  });
};

/** 修改 */
export const update = (data?: object) => {
  localStorage.setItem("button", "修改用户");
  return http.request<Result>("post", "/Sysuser/UpdateUser", {
    data
  });
};

/** 删除 */
export const deleteById = (id: number) => {
  localStorage.setItem("button", "删除用户");
  return http.request<Result>("post", "/Sysuser/DeleteUser?userid=" + id);
};

/** 启用/禁用切换 */
export const toggleEnable = (id: number) => {
  localStorage.setItem("button", "修改用户启用状态");
  return http.request<Result>("post", "/Sysuser/EnableUser?userid=" + id);
};

/** 管理员重置密码(重置为后端固定的初始密码) */
export const resetPwd = (id: number) => {
  localStorage.setItem("button", "重置用户密码");
  return http.request<Result>("post", "/Sysuser/PostResetPwd?userid=" + id);
};

/** 可分配角色列表。tenantId >= 0 时只返回该租户自建角色与平台共享角色 */
export const getRoleList = (tenantId = -1) => {
  localStorage.setItem("button", "可分配角色列表");
  return http.request<ResultTable>(
    "get",
    "/Sysuser/GetRoleList?tenantId=" + tenantId
  );
};

/** 租户列表(超管为用户指定所属租户时用) */
export const getTenantList = (data?: QueryTableParams) => {
  localStorage.setItem("button", "分页查询租户");
  return http.request<ResultTable>("post", "/TenantInfo/GetListByPage", {
    data
  });
};

export default {
  getListByPage,
  insert,
  update,
  deleteById,
  toggleEnable,
  resetPwd,
  getRoleList,
  getTenantList
};
