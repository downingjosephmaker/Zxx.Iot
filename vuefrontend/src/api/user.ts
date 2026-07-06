import { http } from "@/utils/http";
export type UserResult = {
  Status: boolean;
  Result: string;
  Message: string;
  data: {
    /** 头像 */
    avatar: string;
    /** 用户名 */
    username: string;
    /** 昵称 */
    nickname: string;
    /** 当前登录用户的角色 */
    roles: Array<string>;
    /** 按钮级别权限 */
    permissions: Array<string>;
    /** `token` */
    accessToken: string;
    /** 用于调用刷新`accessToken`的接口时所需的`token` */
    refreshToken: string;
    /** `accessToken`的过期时间（格式'xxxx/xx/xx xx:xx:xx'） */
    expires: Date;
  };
};

export type RefreshTokenResult = {
  success: boolean;
  data: {
    /** `token` */
    accessToken: string;
    /** 用于调用刷新`accessToken`的接口时所需的`token` */
    refreshToken: string;
    /** `accessToken`的过期时间（格式'xxxx/xx/xx xx:xx:xx'） */
    expires: Date;
  };
};

export type UserInfo = {
  /** 头像 */
  avatar: string;
  /** 用户名 */
  username: string;
  /** 昵称 */
  nickname: string;
  /** 邮箱 */
  email: string;
  /** 联系电话 */
  phone: string;
  /** 简介 */
  description: string;
};

export type UserInfoResult = {
  success: boolean;
  data: UserInfo;
};

type ResultTable = {
  success: boolean;
  data?: {
    /** 列表数据 */
    list: Array<any>;
    /** 总条目数 */
    total?: number;
    /** 每页显示条目个数 */
    pageSize?: number;
    /** 当前页数 */
    currentPage?: number;
  };
};

/** 登录 */
export const getLogin = (data?: object) => {
  localStorage.setItem("button", "登录");
  return http.request<UserResult>("post", "/Login/LoginUserFun", {
    data
  });
};

/** 单点登录 */
export const getSSOLogin = (data?: object) => {
  localStorage.setItem("button", "单点登录");
  return http.request<UserResult>("get", "/Login/LoginJtddFun", {
    params: data
  });
};
/** 刷新`token` */
export const refreshTokenApi = (data?: object) => {
  localStorage.setItem("button", "刷新token");
  return http.request<RefreshTokenResult>(
    "post",
    "/Sysusertoken/GetRefreshToken",
    { data }
  );
};

/** 登出接口` */
export const LoginOut = (data?: object) => {
  localStorage.setItem("button", "退出登录");
  return http.request<UserResult>("post", "/Login/LoginOut", {
    data
  });
};

/** 账户设置-个人信息 */
export const getMine = (data?: object) => {
  return http.request<UserInfoResult>("get", "/mine", { data });
};

/** 账户设置-个人安全日志 */
export const getMineLogs = (data?: object) => {
  return http.request<ResultTable>("get", "/mine-logs", { data });
};
