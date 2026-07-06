import { http } from "@/utils/http";
import type { Result, QueryTableParams } from "../type";

/**
 * GetDataChartBy的参数
 * 根据条件曲线查询
 *DataChartSelect{
description:	
数据曲线查询

StartTime	string
example: 2025-05-30 18:14:45
开始时间

EndTime	string
example: 2025-05-30 18:14:45
结束时间

DeviceIds	[
nullable: true
设备ID集合

[...]]
BuildId	integer($int32)
建筑ID

DeptId	integer($int32)
部门D

UnitId	integer($int32)
单位ID

ParamCodes	[
nullable: true
参数编号集合

string]
ParamTypeName	string
nullable: true
example:
参数类别

DataSort	integer($int32)
排序(0:倒序 1:正序)

}
 */
export const GetDataChartBy = (data?: QueryTableParams) => {
  localStorage.setItem("button", "根据条件曲线查询");
  return http.request<Result>("post", "/EventHistory/GetDataChartBy", {
    data
  });
};

/**
 * GetDataTableBy的参数
 * 根据条件表格查询DataTableSelect{
description:	
数据查询(表格分页)

StartTime	string
example: 2025-05-30 18:14:45
开始时间

EndTime	string
example: 2025-05-30 18:14:45
结束时间

DeviceIds	[
nullable: true
设备ID集合

[...]]
BuildId	integer($int32)
建筑ID

DeptId	integer($int32)
部门D

UnitId	integer($int32)
单位ID

ParamCodes	[
nullable: true
参数编号集合

[...]]
ParamTypeName	string
nullable: true
example:
参数类别

DataSort	integer($int32)
排序(0:倒序 1:正序)

page	integer($int32)
example: 1
页码

pagesize	integer($int32)
example: 20
行数

}
 * GetDataTableBy接口返回的格式

DataChart{
description:	
曲线数据(多曲线)

ChartX	[
nullable: true
图表X轴值

string]
ChartTuY	[
nullable: true
图表Y轴值

DataChartChild{
description:	
图表Y轴值

ChartTuLi	string
nullable: true
example:
图表图例(名称)

ChartTuLiId	string
nullable: true
example:
图表图例ID(或编码)

ChartY	[
nullable: true
图表Y轴值

string]
}]
}
 */
export const GetDataTableBy = (data?: QueryTableParams) => {
  localStorage.setItem("button", "根据条件表格查询");
  return http.request<Result>("post", "/EventHistory/GetDataTableBy", {
    data
  });
};

/**
 * GetParamTypeSelect的接口返回格式
 * 根据条件表格查询
 * [ParamTypeInfo{
description:	
设备参数分类下拉框

ParamIds	[
nullable: true
参数集合

ParamInfoCode{
description:	
参数下拉框

DeviceTypeCode	string
nullable: true
example:
设备类型编码

ParamCode	string
nullable: true
example:
参数编码

ParamName	string
nullable: true
example:
参数名称

ParamUnit	string
nullable: true
example:
参数单位

}]
ParamTypeName	string
nullable: true
example:
参数分类名称

}]
 */
export const GetParamTypeSelect = (typecode?: any) => {
  localStorage.setItem("button", "根据条件表格查询");
  return http.request<Result>(
    "get",
    "/DeviceTypeParam/GetParamTypeSelect?typecode=" + typecode
  );
};

// GetParamTypeSelectBy;

export default {
  GetDataChartBy,
  GetDataTableBy,
  GetParamTypeSelect
};
