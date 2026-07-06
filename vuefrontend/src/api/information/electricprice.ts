/**
 * 电价管理API接口
 */

import { http } from "@/utils/http";
import type {
  ElectricPrice,
  QueryTableParams,
  ApiResponse,
  BatchSaveParams,
  DeleteParams,
  GetInfoParams
} from "@/views/information/electricprice/utils/types";

const BASE_URL = "/Api/ElectricPrice";

/**
 * 批量保存电价信息
 * @param data 电价信息数组
 * @returns Promise<ApiResponse>
 */
export const saveBatch = (data: ElectricPrice[]): Promise<ApiResponse> => {
  return http.post(`${BASE_URL}/SaveBatch`, data);
};

/**
 * 根据ID删除电价信息
 * @param params 删除参数
 * @returns Promise<ApiResponse>
 */
export const deleteByPk = (params: DeleteParams): Promise<ApiResponse> => {
  return http.post(`${BASE_URL}/DeleteByPk`, null, {
    params
  });
};

/**
 * 根据ID查询电价信息
 * @param params 查询参数
 * @returns Promise<ApiResponse<ElectricPrice>>
 */
export const getInfoByPk = (params: GetInfoParams): Promise<ApiResponse<ElectricPrice>> => {
  return http.get(`${BASE_URL}/GetInfoByPk`, {
    params
  });
};

/**
 * 根据条件查询分页数据
 * @param data 查询参数
 * @returns Promise<ApiResponse<ElectricPrice[]>>
 */
export const getListByPage = (data: QueryTableParams): Promise<ApiResponse<ElectricPrice[]>> => {
  return http.post(`${BASE_URL}/GetListByPage`, data);
};

/**
 * 根据浙江省电价信息添加当前单位数据
 * @returns Promise<ApiResponse>
 */
export const initElectricPrice = (): Promise<ApiResponse> => {
  return http.post(`${BASE_URL}/InitElectricPrice`);
};

// 导出所有API
export default {
  saveBatch,
  deleteByPk,
  getInfoByPk,
  getListByPage,
  initElectricPrice
};
