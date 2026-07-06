import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "./type";

/** mqtt */
/** 获取mqtt */
export const GetMqtt = (data?: object) => {
  localStorage.setItem("button", "查询mqtt");
  return http.request<Result>("get", "/AdminMqttparam/GetMqtt", { data });
};
/** 菜单 */
/** 获取菜单列表 */
export const getMenuTreeList = (data?: object) => {
  localStorage.setItem("button", "获取菜单列表");
  return http.request<Result>("post", "/Sysmenu/GetListByParam", { data });
};

/** 新增菜单 */
export const insertMenu = (data?: object) => {
  localStorage.setItem("button", "新增菜单");
  return http.request<Result>("post", "/Sysmenu/Insert", { data });
};

/** 修改菜单 */
export const updateMenu = (data?: object) => {
  localStorage.setItem("button", "修改菜单");
  return http.request<Result>("post", "/Sysmenu/Update", { data });
};
/** 删除菜单 */
export const deleteMenu = (data?: object) => {
  localStorage.setItem("button", "删除菜单");
  return http.request<Result>("post", "/Sysmenu/Delete?id=" + data);
};

/** 菜单绑定按钮 */
export const SaveMenuBtnBatch = (data?: object) => {
  localStorage.setItem("button", "菜单绑定按钮");
  return http.request<Result>("post", "/Sysmenu/SaveMenuBtnBatch", { data });
};

/** 查询菜单绑定按钮 */
export const GetButtonListByMenuId = (id?: string) => {
  localStorage.setItem("button", "查询菜单绑定按钮");
  return http.request<Result>(
    "post",
    "/Sysmenu/GetButtonListByMenuId?_MenuId=" + id
  );
};
/** 刷新token */
export const ChangeUserToken = () => {
  localStorage.setItem("button", "刷新token");
  return http.request<any>("get", "/Sysuser/ChangeUserToken");
};

/** 用户 */
/** 新增用户 */
export const insertUser = (data?: object) => {
  localStorage.setItem("button", "新增用户");
  return http.request<Result>("post", "/Sysuser/InsertUser", {
    data
  });
};

/** 分页查询用户 */
export const queryUser = (data?: object) => {
  localStorage.setItem("button", "分页查询用户");
  return http.request<ResultTable>("post", "/Sysuser/GetListByPage", {
    data
  });
};

/** 修改用户 */
export const updateUser = (data?: object) => {
  localStorage.setItem("button", "修改用户");
  return http.request<Result>("post", "/Sysuser/UpdateUser", {
    data
  });
};
/** 用户默认企业配置 */
export const postSetUnit = (data?: object) => {
  localStorage.setItem("button", "用户默认企业配置");
  return http.request<Result>("post", "/Sysuser/PostSetUnit", {
    data
  });
};

/** 修改用户启用状态 */
export const enableUser = (id?: string) => {
  localStorage.setItem("button", "修改用户启用状态");
  return http.request<Result>("post", "/Sysuser/EnableUser?userid=" + id);
};

/** 删除用户 */
export const deleteUser = (id?: string) => {
  localStorage.setItem("button", "删除用户");
  return http.request<Result>("post", "/Sysuser/DeleteUser?userid=" + id);
};

/** 修改密码 */
export const editUserPwd = (data?: any) => {
  localStorage.setItem("button", "修改密码");
  return http.request<Result>(
    "post",
    `/Sysuser/PostEditPwd?OldPwd=${data.oldpwd}&NewPwd=${data.newpwd}`,
    {
      data
    }
  );
};

/** 管理员重置密码密码 */
export const resetPwd = (id?: number) => {
  localStorage.setItem("button", "管理员重置密码密码");
  return http.request<Result>("post", "/Sysuser/PostResetPwd?userid=" + id);
};

/** 角色 */
/** 分页查询角色 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "分页查询角色");
  return http.request<ResultTable>("post", "/Sysrole/GetListByPage", {
    data
  });
};
// 用户角色
export const getUserRoleList = () => {
  localStorage.setItem("button", "用户角色列表");
  return http.request<ResultTable>("get", "/Sysuser/GetRoleList");
};

/** 删除角色 */
export const deleteRoleByPk = (data?: object) => {
  localStorage.setItem("button", "删除角色");
  return http.request<Result>(
    "post",
    "/Sysrole/DeleteByPk?_RoleId=" + data["_RoleId"],
    {
      data
    }
  );
};
/** 查询单个角色 */
export const GetInfoByPk = (data?: object) => {
  localStorage.setItem("button", "查询单个角色");
  return http.request<Result>("get", "/Sysrole/GetInfoByPk", {
    data
  });
};
/** 批量保存/修改角色 */
export const saveRoleBatch = (data?: object) => {
  localStorage.setItem("button", " 批量保存/修改角色");
  return http.request<Result>("post", "/Sysrole/SaveBatch", {
    data
  });
};

/** 单位管理 */

/** 分页查询单位 */
export const getUnitListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "分页查询单位");
  return http.request<ResultTable>("post", "/Basicunitinfo/GetListByPage", {
    data
  });
};
/** 查询自己有权限的单位 */
export const GetQxListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "分页查询单位");
  return http.request<ResultTable>("post", "/Basicunitinfo/GetQxListByPage", {
    data
  });
};
/** 切换单位 */
export const ChangeUserTokenByUnit = unitid => {
  localStorage.setItem("button", "切换单位");
  return http.request<Result>(
    "get",
    "/Sysuser/ChangeUserTokenByUnit?unitid=" + unitid
  );
};
/** 删除单位 */
export const deleteUnitByPk = (data?: object) => {
  localStorage.setItem("button", "删除单位");
  return http.request<Result>(
    "post",
    "/Basicunitinfo/DeleteByPk?_UnitId=" + data["_UnitId"],
    {
      data
    }
  );
};
/** 查询单个单位*/
export const GetUnitInfoByPk = (data?: object) => {
  localStorage.setItem("button", "查询单个单位");
  return http.request<Result>("get", "/Basicunitinfo/GetInfoByPk", {
    data
  });
};
/** 批量保存/修改单位 */
export const saveUnitBatch = (data?: object) => {
  localStorage.setItem("button", "批量保存/修改单位");
  return http.request<Result>("post", "/Basicunitinfo/SaveBatch", {
    data
  });
};
/** 修改角色菜单权限 */
export const saveRoleMenuBtns = (data?: object) => {
  localStorage.setItem("button", "修改角色菜单权限");
  return http.request<Result>("post", "/Sysmenu/SaveRoleMenuBtns", {
    data
  });
};
/** 获取角色菜单权限 */
export const getRoleMenuBtns = (id?: string) => {
  localStorage.setItem("button", "获取角色菜单权限");
  return http.request<Result>("post", "/Sysmenu/Get?roleid=" + id);
};
/** 修改用户建筑权限 */
export const SaveRelatedBatch = (data?: object) => {
  localStorage.setItem("button", "修改用户建筑权限");
  return http.request<Result>("post", "/Sysuser/SaveRelatedBatch", { data });
};

/** 按钮管理 */

/** 获取按钮列表 */
export const getButtonList = (data?: object) => {
  localStorage.setItem("button", "获取按钮列表");
  return http.request<Result>("post", "/Sysbutton/GetListByPage", { data });
};
/** 新增修改按钮 */
export const SaveButtonBatch = (data?: object) => {
  localStorage.setItem("button", "修改新增修改按钮");
  return http.request<Result>("post", "/Sysbutton/SaveBatch", { data });
};

/** 删除按钮 */
export const deleteButtonByPk = (id?: string) => {
  localStorage.setItem("button", "删除按钮");
  return http.request<Result>("post", "/Sysbutton/DeleteByPk?_ButtonId=" + id);
};
// 获取区域列表;
export const SysAreaList = (data?: object) => {
  localStorage.setItem("button", "获取区域列表");
  return http.request<Result>("post", "/SysArea/GetListByPage", { data });
};
