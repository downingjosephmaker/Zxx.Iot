import { http } from "@/utils/http";
/** 设备 */
const button = "驾驶舱";

/** 首页tab接口 */
export const GetCurrentEnergy = (data?: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<any>("post", "/Monitor/GetCurrentEnergy", {
    data
  });
};
//能源生产与消耗接口
export const GetEnergyProductCostLine = (data?: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<any>("post", "/Monitor/GetEnergyProductCostLine", {
    data
  });
};
//仓库用电监测接口
export const GetWarehouseEnergyBar = (data?: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<any>("post", "/Monitor/GetWarehouseEnergyBar", {
    data
  });
};
//氢能监测接口
export const GetQingNengLine = (data?: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<any>("post", "/Monitor/GetQingNengLine", {
    data
  });
};
//叉车/堆高车/汽车充电桩监测接口
export const GetScraptEnergyBar = (data?: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<any>("post", "/Monitor/GetScraptEnergyBar", {
    data
  });
};
//园区用水量趋势接口
export const GetWaterEnergyLine = (data?: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<any>("post", "/Monitor/GetWaterEnergyLine", {
    data
  });
};

//楼层用电对比接口
export const GetFloorEnergyBar = (data?: any) => {
  console.log("调用楼层用电对比接口:", data);
  localStorage.setItem("button", "查询" + button + "");
  return http
    .request<any>("post", "/Monitor/GetFloorEnergyBar", {
      data
    })
    .then(response => {
      console.log("楼层用电对比接口返回:", response);
      return response;
    })
    .catch(error => {
      console.error("楼层用电对比接口错误:", error);
      throw error;
    });
};

//流机接口
export const GetLiujiEnergyLine = (data?: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<any>("post", "/Monitor/GetLiujiEnergyLine", {
    data
  });
};

//楼层空调接口
export const GetAirEnergyBar = (data?: any) => {
  localStorage.setItem("button", "查询" + button + "");
  return http.request<any>("post", "/Monitor/GetAirEnergyBar", {
    data
  });
};

export default {
  GetCurrentEnergy,
  GetEnergyProductCostLine,
  GetWarehouseEnergyBar,
  GetQingNengLine,
  GetScraptEnergyBar,
  GetWaterEnergyLine,
  GetFloorEnergyBar,
  GetLiujiEnergyLine,
  GetAirEnergyBar
};
