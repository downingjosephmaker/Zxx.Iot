import { http } from "@/utils/http";
import { type Result, ResultTable, QueryTableParams } from "../../type";
const button = "报警分析";
function getQuery(objectToSend) {
  let query = "";
  for (const key in objectToSend) {
    if (objectToSend.hasOwnProperty(key)) {
      const value = objectToSend[key];
      if (Array.isArray(value)) {
        value.forEach(item => {
          query += `${encodeURIComponent(key)}[]=${encodeURIComponent(item)}&`;
        });
      } else {
        query += `${encodeURIComponent(key)}=${encodeURIComponent(value)}&`;
      }
    }
  }
  query = query.slice(0, -1);
  return query ? "?" + query : "";
}
/** 查询单个*/
export const GetAlarmAnalysisOne = data => {
  localStorage.setItem("button", "查询" + button + "变电所报警排名");
  return http.request<Result>(
    "get",
    "/EventAlarm/GetAlarmAnalysisOne" + getQuery(data)
  );
};
export const GetAlarmAnalysisTwo = data => {
  localStorage.setItem(
    "button",
    "查询" + button + "变电所报警类型/报警等级排名"
  );
  return http.request<Result>(
    "get",
    "/EventAlarm/GetAlarmAnalysisTwo" + getQuery(data)
  );
};
export const GetAlarmAnalysisThree = data => {
  localStorage.setItem("button", "查询" + button + "变电所月报警统计");
  return http.request<Result>(
    "get",
    "/EventAlarm/GetAlarmAnalysisThree" + getQuery(data)
  );
};
export const GetAlarmAnalysisTop = () => {
  localStorage.setItem("button", "查询" + button + "页面顶部");
  return http.request<Result>("get", "/EventAlarm/GetAlarmAnalysisTop");
};
export default {
  GetAlarmAnalysisTop,
  GetAlarmAnalysisOne,
  GetAlarmAnalysisTwo,
  GetAlarmAnalysisThree
};
