import { http } from "@/utils/http";
import type { Result, ResultTable, QueryTableParams } from "../../type";
/** 设备 */
const button = "控制记录";

/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", "/GuestEventSign/GetListByPage", {
    data
  });
};
/** 批量保存/修改 */
export const SaveBatch = (data?: any) => {
  localStorage.setItem("button", "新增" + button + "");
  return http.request<Result>("post", "/GuestEventSign/SaveBatch", {
    data
  });
};

export const PostApplyTemplate = (data?: QueryTableParams) => {
  localStorage.setItem("button", "下载导入模板");
  return http.request<ResultTable>(
    "post",
    "/GuestEventSign/DownloadGuestEventSignTemplate",
    {
      data
    }
  );
};
export default {
  getListByPage,
  SaveBatch,
  PostApplyTemplate
};
