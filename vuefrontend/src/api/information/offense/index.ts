import { http } from "@/utils/http";
import type {
  Result,
  Result2,
  ResultTable,
  QueryTableParams
} from "../../type";
/** 设备 */
const button = "从业人员";
const url = "/Staffinfo";
/** 分页查询 */
export const getListByPage = (data?: QueryTableParams) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ResultTable>("post", url + "/GetListByPage", {
    data
  });
};
/** 删除 */
export const deleteByPk = (id?: string) => {
  localStorage.setItem("button", "删除" + button + "");
  return http.request<Result>("post", url + "/DeleteByPk?_Uid=" + id);
};

/** 查询单个*/
export const GetInfoByPk = (id?: string) => {
  localStorage.setItem("button", "查询单个" + button + "");
  return http.request<Result>("get", url + "/GetInfoByPk?_CategoriesId=" + id);
};

/** 批量保存/修改 */
export const Insert = (data?: any) => {
  localStorage.setItem("button", "新增" + button + "");
  return http.request<Result>("post", url + "/Insert", {
    data
  });
};

export const Update = (data?: any) => {
  localStorage.setItem("button", "修改" + button + "");
  return http.request<Result>("post", url + "/Update", {
    data
  });
};

export const SaveBatch = (data?: any) => {
  localStorage.setItem("button", "新增" + button + "");
  return http.request<Result>("post", url + "/SaveBatch", {
    data
  });
};

export const UpdateZp = (data?: any) => {
  localStorage.setItem("button", "上传图片" + button + "");
  return http.request<Result>("post", url + "/UpdateZp", {
    data
  });
};

export const UpdateBookDuty = (data?: any) => {
  localStorage.setItem("button", "修改值班" + button + "");
  return http.request<Result>("post", url + "/UpdateBookDuty", {
    data
  });
};

export const GetZmmcList = () => {
  localStorage.setItem("button", "查询罪名" + button + "");
  return http.request<Result2>("post", url + "/GetZmmcList");
};

export default {
  getListByPage,
  deleteByPk,
  GetInfoByPk,
  Insert,
  UpdateZp,
  Update,
  SaveBatch,
  UpdateBookDuty,
  GetZmmcList
};
