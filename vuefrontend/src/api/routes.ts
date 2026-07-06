import { http } from "@/utils/http";
type Result = {
  Status: boolean;
  Result: string; // Array<any>
};

export const getAsyncRoutes = (isLimit: number) => {
  localStorage.setItem("button", "查询菜单路由");
  return http.request<Result>(
    "post",
    "/Sysmenu/GetMenuTree?islimit=" + isLimit
  );
};
