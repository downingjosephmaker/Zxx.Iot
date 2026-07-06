import { http } from "@/utils/http";

/** 单位概览报表 */
const button = "单位概览报表";

// 定义接口响应类型
interface ReportResponse {
  Result: any;
  Total?: number;
  Success?: boolean;
  Code?: number;
  Message?: string;
}

/**
 * 获取光伏设备实时参数
 * 返回格式：
 * [http://192.168.0.162:12223/swagger/All/swagger.json#/components/schemas/GuangFuRealGuangFuReal{
description:	
光伏实时数据

energyD	string
nullable: true
example:
今日发电量

energyM	string
nullable: true
example:
月发电量

energyY	string
nullable: true
example:
年发电量

energy	string
nullable: true
example:
总发电量

effect	string
nullable: true
example:
发电效率

co2	string
nullable: true
example:
减碳量

}]
 */
export const GetGfReal = () => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ReportResponse>("get", "/NewEnergy/GetGfReal");
};

/**
 * 光伏发电量(日/月/年)
 * 参数：
 * Name	Description
firstday
string
(query)
日、(月/年)1号

2025-06-25 10:45:10
datatype
integer($int32)
(query)
1=日,2=月,3=年

Default value : 1
 * 返回格式：
{
  "Result": "{\"ChartX\":[],\"ChartTuY\":[{\"ChartTuLi\":\"总发电量（度）\",\"ChartTuLiId\":\"60034\",\"ChartY\":[]}]}",
  "Timestamp": "2025-06-25 10:57:34",
  "Status": true,
  "Message": "信息处理成功。",
  "Total": 1
}


 */
export const GetGfOne = (data: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ReportResponse>("get", "/NewEnergy/GetGfOne", {
    params: data
  });
};

/**
 * （日）光伏发电效率
 * 参数：Name	Description
 * firstday
 * string
 * 日期
 * 返回格式：
 *{
  "Result": "{\"ChartX\":[],\"ChartTuY\":[{\"ChartTuLi\":\"发电效率（%）\",\"ChartTuLiId\":\"60034\",\"ChartY\":[]}]}",
  "Timestamp": "2025-06-25 10:56:28",
  "Status": true,
  "Message": "信息处理成功。",
  "Total": 1
}
 * 
 */
export const GetEneryTwo = (data: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<ReportResponse>("get", "/NewEnergy/GetEneryTwo", {
    params: data
  });
};

export default {
  GetGfReal,
  GetGfOne,
  GetEneryTwo
};
