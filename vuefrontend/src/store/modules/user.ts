import { defineStore } from "pinia";
import {
  type userType,
  store,
  router,
  resetRouter,
  routerArrays
} from "../utils";
import { storageSession } from "@pureadmin/utils";
import {
  type UserResult,
  type RefreshTokenResult,
  getLogin,
  getSSOLogin,
  refreshTokenApi
} from "@/api/user";
import { useMultiTagsStoreHook } from "./multiTags";
import { type DataInfo, setToken, removeToken, userKey } from "@/utils/auth";
import { message } from "@/utils/message";
import { LoginOut } from "@/api/user";
import { storage } from "@/utils/storage";
export const useUserStore = defineStore("pure-user", {
  state: (): userType => ({
    // 头像
    avatar: storage.getItem<DataInfo<number>>(userKey)?.avatar ?? "",
    // 用户名
    username: storage.getItem<DataInfo<number>>(userKey)?.username ?? "",
    // 单位名称
    unitname: storage.getItem<DataInfo<number>>(userKey)?.unitname ?? "",
    // 单位ID
    unitId: storage.getItem<DataInfo<number>>(userKey)?.unitId ?? "",
    // 昵称
    nickname: storage.getItem<DataInfo<number>>(userKey)?.nickname ?? "",
    // 页面级别权限
    roles: storage.getItem<DataInfo<number>>(userKey)?.roles ?? [],
    // 按钮级别权限
    permissions: storage.getItem<DataInfo<number>>(userKey)?.permissions ?? [],
    // 前端生成的验证码（按实际需求替换）
    verifyCode: "",
    // 判断登录页面显示哪个组件（0：登录（默认）、1：手机登录、2：二维码登录、3：注册、4：忘记密码）
    currentPage: 0,
    // 是否勾选了登录页的免登录
    isRemembered: false,
    // 登录页的免登录存储几天，默认7天
    loginDay: 7
  }),
  actions: {
    /** 存储头像 */
    SET_AVATAR(avatar: string) {
      this.avatar = avatar;
    },
    /** 存储用户名 */
    SET_USERNAME(username: string) {
      this.username = username;
    },

    /** 存储默认单位名称*/
    SET_UNITNAME(unitname: string) {
      this.unitname = unitname;
    },
    /** 存储单位ID */
    SET_UNITID(unitId: string) {
      this.unitId = unitId;
    },
    /** 存储昵称 */
    SET_NICKNAME(nickname: string) {
      this.nickname = nickname;
    },
    /** 存储角色 */
    SET_ROLES(roles: Array<string>) {
      this.roles = roles;
    },
    /** 存储按钮级别权限 */
    SET_PERMS(permissions: Array<string>) {
      this.permissions = permissions;
    },
    /** 存储前端生成的验证码 */
    SET_VERIFYCODE(verifyCode: string) {
      this.verifyCode = verifyCode;
    },
    /** 存储登录页面显示哪个组件 */
    SET_CURRENTPAGE(value: number) {
      this.currentPage = value;
    },
    /** 存储是否勾选了登录页的免登录 */
    SET_ISREMEMBERED(bool: boolean) {
      this.isRemembered = bool;
    },
    /** 设置登录页的免登录存储几天 */
    SET_LOGINDAY(value: number) {
      this.loginDay = Number(value);
    },
    /** 登入 */
    async loginByUsername(data) {
      return new Promise<UserResult>((resolve, reject) => {
        getLogin(data)
          .then(datas => {
            // if (data?.success) setToken(data.data);
            // resolve(data);
            if (datas.Status) {
              const result = JSON.parse(datas.Result);
              storage.setItem("token", result.LoginToken);

              // 创建包含UnitAllCount的token对象
              const tokenData = {
                /** 用户名 */
                username: result.UserName,
                /** 默认单位 */
                unitname: result.UnitName,
                /** 单位ID */
                unitId: result.UnitId || "",
                /** 当前登陆用户的角色 */
                roles: result.roles || [],
                /** `token` */
                accessToken: result.LoginToken,
                /** 用于调用刷新`accessToken`的接口时所需的`token` */
                refreshToken: result.LoginToken,
                /** `accessToken`的过期时间（格式'xxxx/xx/xx xx:xx:xx'） */
                expires: "",
                /** 单位总数 */
                UnitAllCount: result.UnitAllCount || 0
              };

              // 保存token数据
              setToken(tokenData);

              // 确保user-info中也包含UnitAllCount
              const userInfo = storage.getItem("user-info") || {};
              userInfo.UnitAllCount = result.UnitAllCount || 0;
              storage.setItem("user-info", userInfo);

              storageSession().setItem("is-system", {
                data: result.IsSystem
              });
              resolve(datas);
            } else {
              resolve(datas);
              message(datas.Message, { type: "error" });
            }
          })
          .catch(error => {
            reject(error);
          });
      });
    },
    /** 单点登录 */
    async ssoLogin(data) {
      return new Promise<UserResult>((resolve, reject) => {
        getSSOLogin(data)
          .then(datas => {
            if (datas.Status) {
              const result = JSON.parse(datas.Result);
              storage.setItem("token", result.LoginToken);
              // 创建包含UnitAllCount的token对象
              const tokenData = {
                /** 用户名 */
                username: result.UserName,
                /** 默认单位 */
                unitname: result.UnitName,
                /** 单位ID */
                unitId: result.UnitId || "",
                /** 当前登陆用户的角色 */
                roles: result.roles || [],
                /** `token` */
                accessToken: result.LoginToken,
                /** 用于调用刷新`accessToken`的接口时所需的`token` */
                refreshToken: result.LoginToken,
                /** `accessToken`的过期时间（格式'xxxx/xx/xx xx:xx:xx'） */
                expires: "",
                /** 单位总数 */
                UnitAllCount: result.UnitAllCount || 0
              };

              // 保存token数据
              setToken(tokenData);

              // 确保user-info中也包含UnitAllCount
              const userInfo = storage.getItem("user-info") || {};
              userInfo.UnitAllCount = result.UnitAllCount || 0;
              storage.setItem("user-info", userInfo);

              storageSession().setItem("is-system", {
                data: result.IsSystem
              });
              resolve(datas);
            } else {
              resolve(datas);
              message(datas.Message, { type: "error" });
            }
          })
          .catch(error => {
            reject(error);
          });
      });
    },
    /** 前端登出（不调用接口） */
    async logOut() {
      try {
        const datas = await LoginOut();
        if (datas.Status) {
          message(`已成功退出登录`, {
            type: "success"
          });
        } else {
          message(datas.Message, { type: "error" });
        }
      } catch (error) {
        console.error("登出接口调用失败:", error);
        message(`登出接口调用失败，将强制退出`, { type: "warning" });
      } finally {
        // 无论接口调用成功还是失败，都清理本地状态并跳转登录页
        this.username = "";
        this.unitname = "";
        this.roles = [];
        this.permissions = [];
        removeToken();
        useMultiTagsStoreHook().handleTags("equal", [...routerArrays]);
        resetRouter();
        router.push("/login");
      }
    },
    /** 前端登出（不调用接口） */
    logOut2() {
      this.username = "";
      this.unitname = "";
      this.roles = [];
      this.permissions = [];
      removeToken();
      useMultiTagsStoreHook().handleTags("equal", [...routerArrays]);
      resetRouter();
      router.push("/login");
    },
    GET_IS_SYSTEM() {
      return storageSession().getItem<any>("is-system")?.data ?? false;
    },
    /** 刷新`token` */
    async handRefreshToken(data) {
      return new Promise<RefreshTokenResult>((resolve, reject) => {
        refreshTokenApi(data)
          .then(data => {
            if (data) {
              setToken(data.data);
              resolve(data);
            }
          })
          .catch(error => {
            reject(error);
          });
      });
    }
  }
});

export function useUserStoreHook() {
  return useUserStore(store);
}
