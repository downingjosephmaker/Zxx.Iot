import * as echarts from "echarts";
import { ref, reactive, watch, onMounted, onUnmounted } from "vue";
import api from "@/api/home/home";
import { getListByPage } from "@/api/report/alarmLog";
import dayjs from "dayjs";
import { useDemoModeStoreHook } from "@/store/modules/demoMode";

export function useMenu() {
  // 演示模式store
  const demoModeStore = useDemoModeStoreHook();

  // 响应式数据
  const currentDate = ref("");
  const energyTimer = ref(null);
  // 日期类型选择
  const dateType = ref("month"); // 默认选择日
  const dateOptions = [
    { label: "日", value: "day" },
    { label: "月", value: "month" },
    { label: "年", value: "year" }
  ];

  // 数值更新标志，用于触发动画效果
  const valueUpdateFlags = reactive({
    productToday: false,
    costToday: false,
    remainToday: false,
    co2CostToday: false,
    co2RemainToday: false,
    waterToday: false
  });

  // 触发更新动画的函数
  const triggerValueUpdate = key => {
    valueUpdateFlags[key] = true;
    // 重置标志，准备下次更新
    setTimeout(() => {
      valueUpdateFlags[key] = false;
    }, 1500); // 时间略长于动画时间
  };

  // 能耗数据
  const energyData = reactive({
    productToday: 0,
    productYesterday: 0,
    productRate: 0,
    costToday: 0,
    costYesterday: 0,
    costRate: 0,
    remainToday: 0,
    remainRate: 0,
    productEffect: 0, // 添加发电效率字段
    co2CostToday: 0,
    co2CostYesterday: 0,
    co2CostRate: 0,
    co2RemainToday: 0,
    co2RemainYesterday: 0,
    co2RemainRate: 0,
    waterToday: 0,
    waterYesterday: 0,
    remainWaterToday: 0,
    waterRate: 0,
    h2Today: 0,
    h2Yesterday: 0,
    h2Rate: 0
  });

  // 指标标题映射
  const titleMapping = reactive({
    day: {
      product: "今日发电量",
      cost: "今日用电量",
      remain: "差值电量",
      co2Cost: "今日碳排总量",
      co2Remain: "今日减碳总量",
      water: "昨日用水量",
      compareText: "环比"
    },
    month: {
      product: "本月发电量",
      cost: "本月用电量",
      remain: "差值电量",
      co2Cost: "本月碳排总量",
      co2Remain: "本月减碳总量",
      water: "本月用水量",
      compareText: "环比"
    },
    year: {
      product: "本年发电量",
      cost: "本年用电量",
      remain: "差值电量",
      co2Cost: "本年碳排总量",
      co2Remain: "本年减碳总量",
      water: "本年用水量",
      compareText: "环比"
    }
  });

  // 能耗生产与消耗数据
  const energyProductCostData = reactive({
    dates: [],
    productData: [],
    costData: []
  });

  // 仓库用电监测数据
  const warehouseEnergyData = reactive({
    names: [],
    values: []
  });

  // 氢能监测数据
  const hydrogenData = reactive({
    dates: [],
    values: []
  });

  // 充电桩监测数据
  const chargerMonitorData = reactive({
    dates: [],
    forkliftData: {
      energy: [],
      count: []
    },
    stackerData: {
      energy: [],
      count: []
    },
    carData: {
      energy: [],
      count: []
    },
    currentType: "ChargingCount" // 默认显示充电次数
  });

  // 园区用水量趋势数据
  const waterEnergyData = reactive({
    dates: [],
    values: []
  });

  // 楼层用电对比图表数据
  const floorChartData = reactive({
    names: [],
    powerValues: [],
    rateValues: [],
    legendData: [] // 添加legendData字段存储图例数据
  });

  // 楼层空调能耗数据
  const airEnergyData = reactive({
    dates: [],
    values: []
  });

  // 系统告警数据相关变量和函数
  const alertData = reactive([]);
  const alertLoading = ref(false);
  const alertTotal = ref(0);
  const alertQueryParams = reactive({
    page: 1,
    pagesize: 10,
    starttime: dayjs().startOf("day").format("YYYY-MM-DD HH:mm:ss"), // 开始时间默认为当天开始
    endtime: dayjs().endOf("day").format("YYYY-MM-DD HH:mm:ss"), // 结束时间默认为当天结束
    sconlist: [
      {
        ParamName: "EventTime", // 报警时间字段
        ParamType: "sort",
        ParamSort: 2, // 2表示倒序
        ParamValue: ""
      },
      {
        ParamName: "CheckResult",
        ParamType: "=",
        ParamValue: "未处理"
      },
      {
        ParamName: "EventType",
        ParamType: "!=",
        ParamValue: "离线"
      }
    ]
  });

  // 基础告警类型
  const _alertTypes = {
    critical: { label: "严重", color: "#ff4757" },
    warning: { label: "警告", color: "#ffa726" },
    info: { label: "普通", color: "#00a2ff" } // 确保"信息"统一显示为"普通"
  };
  // 告警标题模板
  const _alertTemplates = [];

  // 告警时间选项
  const _timeOptions = [
    "刚刚",
    "1分钟前",
    "5分钟前",
    "10分钟前",
    "15分钟前",
    "30分钟前",
    "1小时前",
    "2小时前"
  ];
  // 初始告警数据
  const initialAlerts = [];

  // 设置初始告警数据
  alertData.push(...initialAlerts);
  // 获取系统告警列表
  const fetchAlarmList = async () => {
    alertLoading.value = true;
    // 先清空数据，以便显示加载骨架
    alertData.splice(0, alertData.length);
    try {
      // 使用统一的参数获取方法（支持三种模式）
      let queryParams = { ...alertQueryParams };
      queryParams = demoModeStore.getDemoAlarmParams(
        queryParams,
        dateType.value
      );
      console.log(
        `${demoModeStore.currentModeText} - 告警数据请求参数:`,
        queryParams
      );

      const { Status, Result, Total } = await getListByPage(queryParams);
      if (Status) {
        const datas = JSON.parse(Result);
        // 清空当前告警数据
        // alertData.splice(0, alertData.length);
        // 转换API返回的告警数据格式为UI需要的格式
        datas.forEach(item => {
          alertData.push({
            title: `${item.DeviceName}${item.EventType}`,
            type: mapAlarmLevel(item.AlarmGrade),
            time: formatAlarmTime(item.EventTime),
            description: `${item.BuildName}${item.DeptName}${item.DeviceTypeName}`,
            originalData: item // 保留原始数据，以便后续处理
          });
        });
        console.log("🚀 ~ fetchAlarmList ~ alertData:", alertData);
        alertTotal.value = Total || 0;
      } else {
        // 如果API返回错误或数据为空，使用默认告警数据
        useDefaultAlerts();
      }
    } catch (error) {
      console.error("获取告警数据失败:", error);
      // 发生错误时使用默认告警数据
      useDefaultAlerts();
    } finally {
      alertLoading.value = false;
    }
  };

  // 将告警级别映射为UI组件需要的类型
  const mapAlarmLevel = level => {
    // 根据后端返回的告警级别映射到前端显示类型
    // 后端级别: 1=普通, 2=事故/警告, 3=严重
    switch (level) {
      case "严重":
        return "critical";
      case "事故":
        return "warning";
      case "信息": // 处理可能存在的"信息"告警级别
        return "info"; // 映射到显示为"普通"的类型
      default:
        return "info"; // 默认使用info类型，对应显示为"普通"
    }
  };

  // 格式化告警时间为相对时间
  const formatAlarmTime = timeString => {
    if (!timeString) return "未知时间";

    try {
      const alarmTime = new Date(timeString);
      const now = new Date();
      const diffMs = now.getTime() - alarmTime.getTime();

      // 转换为相对时间描述
      if (diffMs < 60000) {
        // 小于1分钟
        return "刚刚";
      } else if (diffMs < 3600000) {
        // 小于1小时
        return `${Math.floor(diffMs / 60000)}分钟前`;
      } else if (diffMs < 86400000) {
        // 小于1天
        return `${Math.floor(diffMs / 3600000)}小时前`;
      } else {
        return `${Math.floor(diffMs / 86400000)}天前`;
      }
    } catch (e) {
      console.error("时间格式化错误:", e);
      return timeString; // 如果格式化失败，返回原始时间字符串
    }
  };

  // 使用默认告警数据（当API请求失败时）
  const useDefaultAlerts = () => {
    // 清空当前告警数据并添加默认数据
    alertData.splice(0, alertData.length);
    initialAlerts.forEach(alert => alertData.push(alert));

    alertTotal.value = initialAlerts.length;
  };

  // 获取能源生产与消耗对比数据
  const fetchEnergyProductCostLine = async () => {
    try {
      // 根据日期类型设置不同的参数
      let dataTypeParam = 2; // 默认为日

      if (dateType.value === "month") {
        dataTypeParam = 4; // 月
      } else if (dateType.value === "year") {
        dataTypeParam = 5; // 年
      }

      let params = {
        DataType: dataTypeParam
      };

      // 使用统一的参数获取方法（支持三种模式）
      params = demoModeStore.getDemoApiParams(params, dateType.value);

      console.log(
        `${demoModeStore.currentModeText} - 能源生产消耗数据请求参数:`,
        params
      );

      // 调用接口获取数据
      const response = await api.GetEnergyProductCostLine(params);
      if (response && response.Status && response.Result) {
        // 解析返回的JSON字符串
        const chartData = JSON.parse(response.Result);

        // 清空旧数据
        energyProductCostData.dates = [];
        energyProductCostData.productData = [];
        energyProductCostData.costData = [];

        // 处理X轴日期数据
        if (chartData.ChartX && chartData.ChartX.length > 0) {
          energyProductCostData.dates = chartData.ChartX;
        }

        // 处理Y轴数据
        if (
          chartData.ChartTuY &&
          chartData.ChartTuY.length >= 2 &&
          chartData.ChartTuY[0].ChartY &&
          chartData.ChartTuY[1].ChartY
        ) {
          // 获取生产数据
          energyProductCostData.productData = chartData.ChartTuY[0].ChartY.map(
            value => parseFloat(value)
          );
          // 获取消耗数据
          energyProductCostData.costData = chartData.ChartTuY[1].ChartY.map(
            value => parseFloat(value)
          );
        }

        // 更新图表
        updateEnergyCompareChart();
      }
    } catch (error) {
      console.error("获取能源生产与消耗对比数据失败:", error);
    }
  };

  // 更新能源生产与消耗对比图表
  const updateEnergyCompareChart = () => {
    if (energyCompareChartRef.value) {
      // 获取已存在的图表实例或创建新实例
      let chartInstance = echarts.getInstanceByDom(energyCompareChartRef.value);
      // 如果没有实例，则初始化一个
      if (!chartInstance) {
        chartInstance = echarts.init(energyCompareChartRef.value);
      }

      // 设置图表配置
      chartInstance.setOption({
        tooltip: {
          trigger: "axis",
          axisPointer: {
            type: "cross",
            label: {
              backgroundColor: "#6a7985"
            }
          }
        },
        legend: {
          data: ["光伏发电", "园区用电"],
          textStyle: {
            color: "#fff"
          },
          top: 30
        },
        grid: {
          left: "3%",
          right: "4%",
          bottom: "3%",
          containLabel: true
        },
        xAxis: {
          type: "category",
          boundaryGap: false,
          data: energyProductCostData.dates,
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          },
          axisLabel: {
            color: "rgba(255,255,255,0.7)",
            rotate: 45,
            fontSize: 10
          }
        },
        yAxis: {
          type: "value",
          name: "kWh",
          nameTextStyle: {
            color: "rgba(255,255,255,0.7)"
          },
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          },
          axisLabel: {
            color: "rgba(255,255,255,0.7)"
          },
          splitLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.1)"
            }
          }
        },
        series: [
          {
            name: "光伏发电",
            type: "line",
            smooth: true,
            showSymbol: false,
            lineStyle: {
              width: 3,
              color: "#00ff87", // 使用UI中绿色系表示生产
              shadowColor: "rgba(0, 255, 135, 0.5)",
              shadowBlur: 10
            },
            areaStyle: {
              color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                { offset: 0, color: "rgba(0, 255, 135, 0.5)" },
                { offset: 1, color: "rgba(0, 255, 135, 0.05)" }
              ])
            },
            symbol: "circle",
            symbolSize: 6,
            emphasis: {
              focus: "series",
              itemStyle: {
                color: "#ffffff",
                borderColor: "#00ff87",
                borderWidth: 2
              }
            },
            data: energyProductCostData.productData,
            markPoint: {
              data: [
                { type: "max", name: "最大值" },
                { type: "min", name: "最小值" }
              ]
            },
            z: 10
          },
          {
            name: "园区用电",
            type: "line",
            smooth: true,
            showSymbol: false,
            lineStyle: {
              width: 3,
              color: "#ff6b6b", // 使用UI中红色系表示消耗
              shadowColor: "rgba(255, 107, 107, 0.5)",
              shadowBlur: 10
            },
            areaStyle: {
              color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                { offset: 0, color: "rgba(255, 107, 107, 0.5)" },
                { offset: 1, color: "rgba(255, 107, 107, 0.05)" }
              ])
            },
            symbol: "circle",
            symbolSize: 6,
            emphasis: {
              focus: "series",
              itemStyle: {
                color: "#ffffff",
                borderColor: "#ff6b6b",
                borderWidth: 2
              }
            },
            markPoint: {
              data: [
                { type: "max", name: "最大值" },
                { type: "min", name: "最小值" }
              ]
            },
            markLine: {
              silent: true,
              lineStyle: {
                color: "rgba(255,255,255,0.3)"
              },
              data: [
                {
                  yAxis: 1500,
                  label: {
                    formatter: "平衡线: 1500kWh",
                    position: "start",
                    color: "rgba(255,255,255,0.7)"
                  }
                }
              ]
            },
            data: energyProductCostData.costData
          }
        ]
      });

      // 添加窗口大小调整监听
      window.addEventListener("resize", () => {
        chartInstance && chartInstance.resize();
      });
    }
  };

  // 获取仓库用电监测数据
  const fetchWarehouseEnergyBar = async () => {
    try {
      // 根据日期类型设置不同的参数
      let dataTypeParam = 2; // 默认为日

      if (dateType.value === "month") {
        dataTypeParam = 4; // 月
      } else if (dateType.value === "year") {
        dataTypeParam = 5; // 年
      }

      let params = {
        DataType: dataTypeParam
      };

      // 使用统一的参数获取方法（支持三种模式）
      params = demoModeStore.getDemoApiParams(params, dateType.value);
      console.log(
        `${demoModeStore.currentModeText} - 仓库用电数据请求参数:`,
        params
      );

      // 调用接口获取数据
      const response = await api.GetWarehouseEnergyBar(params);
      if (response && response.Status && response.Result) {
        // 解析返回的JSON字符串
        const chartData = JSON.parse(response.Result);
        // 清空旧数据
        warehouseEnergyData.names = [];
        warehouseEnergyData.values = [];

        // 处理X轴日期数据
        if (chartData.ChartX && chartData.ChartX.length > 0) {
          warehouseEnergyData.names = chartData.ChartX;
        }

        // 处理Y轴数据
        if (
          chartData.ChartTuY &&
          chartData.ChartTuY.length > 0 &&
          chartData.ChartTuY[0].ChartY
        ) {
          // 获取仓库用电数据
          warehouseEnergyData.values = chartData.ChartTuY[0].ChartY.map(value =>
            parseFloat(value)
          );
        }

        // 更新图表
        updateWarehouseChart();
      }
    } catch (error) {
      console.error("获取仓库用电监测数据失败:", error);
    }
  };

  // 更新仓库用电监测图表
  const updateWarehouseChart = () => {
    if (warehouseChartRef.value) {
      // 获取已存在的图表实例或创建新实例
      let chartInstance = echarts.getInstanceByDom(warehouseChartRef.value);
      // 如果没有实例，则初始化一个
      if (!chartInstance) {
        chartInstance = echarts.init(warehouseChartRef.value);
      }

      // 设置图表配置
      chartInstance.setOption({
        tooltip: {
          trigger: "axis",
          axisPointer: {
            type: "shadow"
          }
        },
        grid: {
          top: "18%",
          left: "3%",
          right: "4%",
          bottom: "3%",
          containLabel: true
        },
        xAxis: {
          type: "category",
          data: warehouseEnergyData.names,
          axisLabel: {
            color: "rgba(255,255,255,0.7)",
            rotate: 0
          },
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          }
        },
        yAxis: {
          type: "value",
          name: "kWh",
          nameTextStyle: {
            color: "rgba(255,255,255,0.7)"
          },
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          },
          axisLabel: {
            color: "rgba(255,255,255,0.7)"
          },
          splitLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.1)"
            }
          }
        },
        series: [
          {
            name: "用电量",
            type: "bar",
            barWidth: "50%",
            data: warehouseEnergyData.values,
            itemStyle: {
              color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                { offset: 0, color: "#41d0fd" },
                { offset: 1, color: "#275fe4" }
              ]),
              borderRadius: [6, 6, 0, 0]
            },
            emphasis: {
              itemStyle: {
                color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                  { offset: 0, color: "#41d0fd" },
                  { offset: 1, color: "#275fe4" }
                ]),
                borderRadius: [6, 6, 0, 0],
                opacity: 0.9,
                shadowBlur: 15,
                shadowColor: "rgba(65, 208, 253, 0.5)"
              }
            },
            label: {
              show: true,
              position: "top",
              formatter: "{c} kWh",
              color: "rgba(255, 255, 255, 0.7)"
            }
          }
        ]
      });
    }
  };

  // 获取氢能监测数据
  const fetchQingNengLine = async () => {
    try {
      // 根据日期类型设置不同的参数
      let dataTypeParam = 2; // 默认为日

      if (dateType.value === "month") {
        dataTypeParam = 4; // 月
      } else if (dateType.value === "year") {
        dataTypeParam = 5; // 年
      }

      let params = {
        DataType: dataTypeParam
      };

      // 使用统一的参数获取方法（支持三种模式）
      params = demoModeStore.getDemoApiParams(params, dateType.value);
      console.log(
        `${demoModeStore.currentModeText} - 氢能数据请求参数:`,
        params
      );

      // 调用接口获取数据
      const response = await api.GetQingNengLine(params);
      // 更新氢能监测数据
      if (response && response.Status && response.Result) {
        // 解析返回的JSON字符串
        const chartData = JSON.parse(response.Result);

        // 清空旧数据
        hydrogenData.dates = [];
        hydrogenData.values = [];

        // 处理X轴日期数据
        if (chartData.ChartX && chartData.ChartX.length > 0) {
          hydrogenData.dates = chartData.ChartX;
        }

        // 处理Y轴数据
        if (
          chartData.ChartTuY &&
          chartData.ChartTuY.length > 0 &&
          chartData.ChartTuY[0].ChartY
        ) {
          // 获取氢气产量数据
          hydrogenData.values = chartData.ChartTuY[0].ChartY.map(value =>
            parseFloat(value)
          );
        }
        // 更新图表
        updateHydrogenChart();
      }
    } catch (error) {
      console.error("获取氢能监测数据失败:", error);
    }
  };

  // 更新氢能监测图表
  const updateHydrogenChart = () => {
    if (hydrogenChartRef.value) {
      // 获取已存在的图表实例或创建新实例
      let chartInstance = echarts.getInstanceByDom(hydrogenChartRef.value);
      // 如果没有实例，则初始化一个
      if (!chartInstance) {
        chartInstance = echarts.init(hydrogenChartRef.value);
      }
      // 计算平均值
      const average =
        hydrogenData.values.length > 0
          ? hydrogenData.values.reduce((sum, value) => sum + value, 0) /
            hydrogenData.values.length
          : 0;

      chartInstance.setOption({
        tooltip: {
          trigger: "axis",
          axisPointer: {
            type: "shadow"
          }
        },
        title: {
          // text: "日均产量: " + average.toFixed(1) + " Nm³",
          textStyle: {
            color: "#fff",
            fontSize: 14
          },
          left: 10,
          top: 10
        },
        grid: {
          left: "3%",
          right: "4%",
          bottom: "3%",
          containLabel: true
        },
        xAxis: {
          type: "category",
          data: hydrogenData.dates,
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          },
          axisLabel: {
            color: "rgba(255,255,255,0.7)"
          }
        },
        yAxis: {
          type: "value",
          name: "kg",
          nameTextStyle: {
            color: "rgba(255,255,255,0.7)"
          },
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          },
          axisLabel: {
            color: "rgba(255,255,255,0.7)"
          },
          splitLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.1)"
            }
          }
        },
        series: [
          {
            name: "用氢量",
            type: "line",
            data: hydrogenData.values,
            smooth: true,
            showSymbol: false,
            lineStyle: {
              width: 3,
              color: "#00a2ff",
              shadowColor: "rgba(0, 162, 255, 0.5)",
              shadowBlur: 10
            },
            areaStyle: {
              color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                { offset: 0, color: "rgba(0, 162, 255, 0.5)" },
                { offset: 1, color: "rgba(0, 162, 255, 0.1)" }
              ])
            },
            itemStyle: {
              color: "#00a2ff"
            },
            markPoint: {
              data: [
                { type: "max", name: "最大值" },
                { type: "min", name: "最小值" }
              ]
            },
            markLine: {
              silent: true,
              lineStyle: {
                color: "rgba(255,255,255,0.3)"
              },
              data: [
                {
                  yAxis: average,
                  label: {
                    formatter: "平均值: " + average.toFixed(1) + " Nm³",
                    position: "start",
                    color: "rgba(255,255,255,0.7)"
                  }
                }
              ]
            }
          }
        ]
      });

      // 添加窗口大小调整监听（只保留一个简化的监听器）
      window.addEventListener("resize", () => {
        if (hydrogenChartRef.value) {
          const chart = echarts.getInstanceByDom(hydrogenChartRef.value);
          chart && chart.resize();
        }
      });
    }
  };

  // 获取充电桩监测数据
  const fetchScraptEnergyBar = async () => {
    try {
      // 根据日期类型设置不同的参数
      let dataTypeParam = 2; // 默认为日

      if (dateType.value === "month") {
        dataTypeParam = 4; // 月
      } else if (dateType.value === "year") {
        dataTypeParam = 5; // 年
      }

      // 当前选择的参数类型
      const paramCode = chargerMonitorData.currentType;

      let params = {
        DataType: dataTypeParam,
        ParamCodes: [paramCode]
      };

      // 使用统一的参数获取方法（支持三种模式）
      params = demoModeStore.getDemoApiParams(params, dateType.value);
      console.log(
        `${demoModeStore.currentModeText} - 充电桩数据请求参数:`,
        params
      );

      // 调用接口获取数据
      const response = await api.GetScraptEnergyBar(params);
      if (response && response.Status && response.Result) {
        // 解析返回的JSON字符串
        const chartData = JSON.parse(response.Result);
        // 清空旧数据
        chargerMonitorData.dates = [];
        chargerMonitorData.forkliftData.energy = [];
        chargerMonitorData.forkliftData.count = [];
        chargerMonitorData.stackerData.energy = [];
        chargerMonitorData.stackerData.count = [];
        chargerMonitorData.carData.energy = [];
        chargerMonitorData.carData.count = [];

        // 处理X轴日期数据
        if (chartData.ChartX && chartData.ChartX.length > 0) {
          chargerMonitorData.dates = chartData.ChartX;
        }

        // 处理Y轴数据
        // 适配新的数据格式
        if (chartData.ChartTuY && chartData.ChartTuY.length > 0) {
          chartData.ChartTuY.forEach(series => {
            if (series.ChartTuLiId === "scrapt") {
              // 叉车数据
              if (paramCode == "ChargingCount") {
                chargerMonitorData.forkliftData.count = series.ChartY.map(
                  value => parseFloat(value)
                );
              } else {
                chargerMonitorData.forkliftData.energy = series.ChartY.map(
                  value => parseFloat(value)
                );
              }
              // 充电次数数据如果有需要可以在此处赋值
            } else if (series.ChartTuLiId === "stack") {
              // 堆高车数据
              if (paramCode == "ChargingCount") {
                chargerMonitorData.stackerData.count = series.ChartY.map(
                  value => parseFloat(value)
                );
              } else {
                chargerMonitorData.stackerData.energy = series.ChartY.map(
                  value => parseFloat(value)
                );
              }
            } else if (series.ChartTuLiId === "car") {
              // 汽车数据
              if (paramCode == "ChargingCount") {
                chargerMonitorData.carData.count = series.ChartY.map(value =>
                  parseFloat(value)
                );
              } else {
                chargerMonitorData.carData.energy = series.ChartY.map(value =>
                  parseFloat(value)
                );
              }
            }
          });
        }
        // 更新图表
        updateChargerMonitorChart();
      }
    } catch (error) {
      console.error("获取充电桩监测数据失败:", error);
    }
  };

  // 更新充电桩监测图表
  const updateChargerMonitorChart = () => {
    if (forkliftChargerChartRef.value) {
      // 根据当前选择的类型获取对应数据
      const forkliftData =
        chargerMonitorData.currentType === "ChargingCount"
          ? chargerMonitorData.forkliftData.count
          : chargerMonitorData.forkliftData.energy;

      const stackerData =
        chargerMonitorData.currentType === "ChargingCount"
          ? chargerMonitorData.stackerData.count
          : chargerMonitorData.stackerData.energy;

      const carData =
        chargerMonitorData.currentType === "ChargingCount"
          ? chargerMonitorData.carData.count
          : chargerMonitorData.carData.energy;

      // 设置Y轴名称
      const yAxisName =
        chargerMonitorData.currentType === "ChargingCount"
          ? "充电次数"
          : "用电量(kWh)";

      // 设置提示框单位
      const _tooltipUnit =
        chargerMonitorData.currentType === "ChargingCount" ? "次" : "kWh";

      // 获取已存在的图表实例或创建新实例
      let chartInstance = echarts.getInstanceByDom(
        forkliftChargerChartRef.value
      );
      // 如果没有实例，则初始化一个
      if (!chartInstance) {
        chartInstance = echarts.init(forkliftChargerChartRef.value);
      }

      // 设置图表配置
      chartInstance.setOption({
        tooltip: {
          trigger: "axis"
          // axisPointer: {
          //   type: "shadow"
          // },
          // backgroundColor: "rgba(50,50,50,0.9)",
          // borderColor: "#333",
          // borderWidth: 1,
          // textStyle: {
          //   color: "#fff",
          //   fontSize: 13
          // },
          // z: 100000,
          // formatter: function (params) {
          //   let result = `${params[0].axisValueLabel}<br/>`;
          //   params.forEach(param => {
          //     const marker = `<span style="display:inline-block;margin-right:4px;border-radius:10px;width:10px;height:10px;background-color:${param.color};"></span>`;
          //     // 根据当前显示的类型决定单位
          //     result += `${marker}${param.seriesName}: <span style="font-weight:bold;font-size:14px;color:${param.color}">${param.value}${tooltipUnit}</span><br/>`;
          //   });
          //   return result;
          // }
        },
        legend: {
          data: ["叉车", "堆高车", "汽车"],
          textStyle: {
            color: "#fff"
          }
        },
        grid: {
          left: "3%",
          right: "4%",
          bottom: "3%",
          containLabel: true
        },
        xAxis: {
          type: "category",
          data: chargerMonitorData.dates,
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          },
          axisLabel: {
            color: "rgba(255,255,255,0.7)",
            rotate: 45,
            fontSize: 10
          }
        },
        yAxis: {
          data: yAxisName,
          type: "value",
          name: "用电量(kWh)",
          nameTextStyle: {
            color: "rgba(255,255,255,0.7)"
          },
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          },
          axisLabel: {
            color: "rgba(255,255,255,0.7)"
          },
          splitLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.1)"
            }
          }
        },
        series: [
          {
            name: "叉车",
            type: "bar",
            barWidth: "20%",
            data: forkliftData,
            itemStyle: {
              color: "#4ecdc4"
            },
            label: {
              show: true,
              position: "top",
              color: "#4ecdc4",
              fontSize: 12,
              formatter: "{c}"
            }
          },
          {
            name: "堆高车",
            type: "bar",
            barWidth: "20%",
            data: stackerData,
            itemStyle: {
              color: "#ff6b6b"
            },
            label: {
              show: true,
              position: "top",
              color: "#ff6b6b",
              fontSize: 12,
              formatter: "{c}"
            }
          },
          {
            name: "汽车",
            type: "bar",
            barWidth: "20%",
            data: carData,
            itemStyle: {
              color: "#ffe66d"
            },
            label: {
              show: true,
              position: "top",
              color: "#ffe66d",
              fontSize: 12,
              formatter: "{c}"
            }
          }
        ]
      });

      // 添加窗口大小调整监听
      window.addEventListener("resize", () => {
        if (forkliftChargerChartRef.value) {
          const chart = echarts.getInstanceByDom(forkliftChargerChartRef.value);
          chart && chart.resize();
        }
      });
    }
  };

  // 切换充电桩监测数据类型
  const switchChargerMonitorType = type => {
    chargerMonitorData.currentType = type;
    // 重新获取充电桩监测数据
    fetchScraptEnergyBar();
  };

  // 获取园区用水量趋势数据
  const fetchWaterEnergyLine = async () => {
    try {
      // 根据日期类型设置不同的参数
      let dataTypeParam = 2; // 默认为日

      if (dateType.value === "month") {
        dataTypeParam = 4; // 月
      } else if (dateType.value === "year") {
        dataTypeParam = 5; // 年
      }

      let params = {
        DataType: dataTypeParam
      };

      // 使用统一的参数获取方法（支持三种模式）
      params = demoModeStore.getDemoApiParams(params, dateType.value);
      console.log(
        `${demoModeStore.currentModeText} - 用水量数据请求参数:`,
        params
      );

      const response = await api.GetWaterEnergyLine(params);
      if (response && response.Status && response.Result) {
        // 解析返回的JSON字符串
        const chartData = JSON.parse(response.Result);
        // 清空旧数据
        waterEnergyData.dates = [];
        waterEnergyData.values = [];

        // 处理X轴日期数据
        if (chartData.ChartX && chartData.ChartX.length > 0) {
          waterEnergyData.dates = chartData.ChartX;
        }

        // 处理Y轴数据
        if (
          chartData.ChartTuY &&
          chartData.ChartTuY.length > 0 &&
          chartData.ChartTuY[0].ChartY
        ) {
          // 获取用水量数据
          waterEnergyData.values = chartData.ChartTuY[0].ChartY.map(value =>
            parseFloat(value)
          );
        }
        // 更新图表
        updateWaterEnergyChart();
      }
    } catch (error) {
      console.error("获取园区用水量趋势数据失败:", error);
    }
  };

  // 更新园区用水量趋势图表
  const updateWaterEnergyChart = () => {
    if (waterChartRef.value) {
      // 获取已存在的图表实例或创建新实例
      let chartInstance = echarts.getInstanceByDom(waterChartRef.value);
      // 如果没有实例，则初始化一个
      if (!chartInstance) {
        chartInstance = echarts.init(waterChartRef.value);
      }
      // 确定X轴数据
      const xAxisData =
        waterEnergyData.dates.length > 0
          ? waterEnergyData.dates
          : ["00:00", "06:00", "12:00", "18:00", "24:00"];
      // 设置图表配置
      chartInstance.setOption({
        tooltip: {
          trigger: "axis",
          axisPointer: {
            type: "cross"
          }
        },
        grid: {
          top: "20%",
          left: "3%",
          right: "4%",
          bottom: "3%",
          containLabel: true
        },
        xAxis: {
          type: "category",
          boundaryGap: false,
          data: xAxisData,
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          },
          axisLabel: {
            color: "rgba(255,255,255,0.7)",
            rotate: 45,
            fontSize: 10
          }
        },
        yAxis: {
          type: "value",
          name: "吨",
          nameTextStyle: {
            color: "rgba(255,255,255,0.7)"
          },
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          },
          axisLabel: {
            color: "rgba(255,255,255,0.7)"
          },
          splitLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.1)"
            }
          }
        },
        series: [
          {
            name: "用水量",
            type: "line",
            smooth: true,
            showSymbol: false,
            lineStyle: {
              width: 3,
              color: "#36cfc9",
              shadowColor: "rgba(54, 207, 201, 0.5)",
              shadowBlur: 10
            },
            areaStyle: {
              color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                { offset: 0, color: "rgba(0, 221, 255, 0.6)" },
                { offset: 1, color: "rgba(77, 119, 255, 0.1)" }
              ])
            },
            symbol: "circle",
            symbolSize: 8,
            itemStyle: {
              color: "#36cfc9"
            },
            data: waterEnergyData.values,
            markPoint: {
              data: [
                { type: "max", name: "最大值" },
                { type: "min", name: "最小值" }
              ]
            }
          }
        ]
      });
      // 添加窗口大小调整监听（只保留一个简化的监听器）
      window.addEventListener("resize", () => {
        if (waterChartRef.value) {
          const chart = echarts.getInstanceByDom(waterChartRef.value);
          chart && chart.resize();
        }
      });
    }
  };

  // 获取楼层用电对比数据
  const fetchFloorEnergyData = async () => {
    try {
      // 根据日期类型设置不同的参数
      let dataTypeParam = 2; // 默认为日

      if (dateType.value === "month") {
        dataTypeParam = 4; // 月
      } else if (dateType.value === "year") {
        dataTypeParam = 5; // 年
      }

      let params = {
        DataType: dataTypeParam
      };

      // 使用统一的参数获取方法（支持三种模式）
      params = demoModeStore.getDemoApiParams(params, dateType.value);
      console.log(
        `${demoModeStore.currentModeText} - 楼层用电数据请求参数:`,
        params
      );

      // 调用接口获取数据
      const response = await api.GetFloorEnergyBar(params);
      if (response && response.Status && response.Result) {
        // 解析返回的JSON字符串
        const chartData = JSON.parse(response.Result);
        // 清空旧数据
        floorChartData.names = [];
        floorChartData.powerValues = [];
        floorChartData.rateValues = [];
        floorChartData.legendData = []; // 清空图例数据
        // 处理X轴楼层名称数据
        if (chartData.ChartX && chartData.ChartX.length > 0) {
          floorChartData.names = chartData.ChartX;
        }
        // 处理Y轴数据
        if (chartData.ChartTuY && chartData.ChartTuY.length > 0) {
          // 遍历数据系列
          chartData.ChartTuY.forEach(series => {
            // 保存图例名称
            floorChartData.legendData.push(series.ChartTuLi);

            if (series.ChartTuLiId === "fcur") {
              // 今日用电量数据
              floorChartData.powerValues = series.ChartY.map(value =>
                parseFloat(value)
              );
            } else if (series.ChartTuLiId === "fpre") {
              // 昨日用电量数据 (用于对比)
              floorChartData.rateValues = series.ChartY.map(value =>
                parseFloat(value)
              );
            }
          });
        }
        // 更新图表
        updateFloorEnergyChart();
      }
    } catch (error) {
      console.error("获取楼层用电对比数据失败:", error);
    }
  };

  // 更新楼层用电对比图表
  const updateFloorEnergyChart = () => {
    if (floorEnergyChartRef.value) {
      // 使用接口返回的数据
      const names = floorChartData.names;
      const todayValues = floorChartData.powerValues;
      const yesterdayValues = floorChartData.rateValues; // 临时模拟历史数据
      // 使用动态图例数据，如果没有则使用默认值
      const legendData =
        floorChartData.legendData.length > 0
          ? floorChartData.legendData
          : ["今日用电量", "昨日用电量"];
      // 获取已存在的图表实例或创建新实例
      let chartInstance = echarts.getInstanceByDom(floorEnergyChartRef.value);
      // 如果没有实例，则初始化一个
      if (!chartInstance) {
        chartInstance = echarts.init(floorEnergyChartRef.value);
      }

      // 设置图表配置
      chartInstance.setOption({
        tooltip: {
          trigger: "axis",
          axisPointer: {
            type: "shadow"
          }
        },
        legend: {
          data: legendData,
          textStyle: {
            color: "#fff"
          }
        },
        grid: {
          left: "3%",
          right: "4%",
          bottom: "3%",
          containLabel: true
        },
        xAxis: {
          type: "category",
          data: names,
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          },
          axisLabel: {
            color: "rgba(255,255,255,0.7)",
            rotate: 45,
            fontSize: 10
          }
        },
        yAxis: {
          type: "value",
          name: "kWh",

          nameTextStyle: {
            color: "rgba(255,255,255,0.7)"
          },
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          },
          axisLabel: {
            color: "rgba(255,255,255,0.7)"
          },
          splitLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.1)"
            }
          }
        },
        series: [
          {
            name: legendData[0],
            type: "bar",
            data: todayValues,
            barWidth: "40%",
            itemStyle: {
              color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
                { offset: 0, color: "#00c6fb" },
                { offset: 1, color: "#005bea" }
              ])
            }
          },
          {
            name: legendData[1],
            type: "bar",
            data: yesterdayValues,
            barWidth: "40%",
            itemStyle: {
              color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
                { offset: 0, color: "#a1c4fd" },
                { offset: 1, color: "#5e87db" }
              ])
            }
          }
        ]
      });
    }
  };

  // 获取当前能耗数据
  const fetchEnergyData = async () => {
    try {
      // 根据日期类型设置不同的参数
      let dataTypeParam = 2; // 默认为日
      if (dateType.value === "month") {
        dataTypeParam = 4; // 月
      } else if (dateType.value === "year") {
        dataTypeParam = 5; // 年
      }

      let params = {
        DataType: dataTypeParam
      };

      // 使用统一的参数获取方法（支持三种模式）
      params = demoModeStore.getDemoApiParams(params, dateType.value);

      console.log(
        `${demoModeStore.currentModeText} - 能耗数据请求参数:`,
        params
      );

      // 调用接口获取数据
      const response = await api.GetCurrentEnergy(params);
      // 更新能耗数据
      if (response && response.Status && response.Result) {
        const data = JSON.parse(response.Result);
        // 保存旧值以便比较
        const oldValues = {
          productToday: energyData.productToday,
          costToday: energyData.costToday,
          remainToday: energyData.remainToday,
          co2CostToday: energyData.co2CostToday,
          co2RemainToday: energyData.co2RemainToday,
          waterToday: energyData.waterToday
        };
        // 更新数据
        energyData.productToday = data.ProductToday || 0;
        energyData.productYesterday = data.ProductYesterday || 0;
        energyData.productRate = data.ProductRate || 0;
        energyData.costToday = data.CostToday || 0;
        energyData.costYesterday = data.CostYesterday || 0;
        energyData.costRate = data.CostRate || 0;
        energyData.remainToday = data.RemainToday || 0;

        // 确保自给率有值，如果API没有返回则手动计算
        if (data.RemainRate !== undefined && data.RemainRate !== null) {
          energyData.remainRate = parseFloat(data.RemainRate);
        } else if (energyData.costToday > 0) {
          // 自给率 = 发电量 / 用电量 x 100%
          energyData.remainRate =
            (energyData.productToday / energyData.costToday) * 100;
        } else {
          energyData.remainRate = 0;
        }
        // 添加发电效率字段处理
        energyData.productEffect = data.ProductEffect || 0;
        energyData.co2CostToday = data.CO2CostToday || 0;
        energyData.co2CostYesterday = data.CO2CostYesterday || 0;
        energyData.co2CostRate = data.CO2CostRate || 0;
        energyData.co2RemainToday = data.CO2RemainToday || 0;
        energyData.co2RemainYesterday = data.CO2RemainYesterday || 0;
        energyData.co2RemainRate = data.CO2RemainRate || 0;
        energyData.waterToday = data.WaterToday || 0;
        energyData.waterYesterday = data.WaterYesterday || 0;
        energyData.waterRate = data.WaterRate || 0;
        energyData.remainWaterToday = data.RemainWaterToday || 0;
        energyData.h2Today = data.H2Today || 0;
        energyData.h2Yesterday = data.H2Yesterday || 0;
        energyData.h2Rate = data.H2Rate || 0;

        // 检查值是否发生变化，触发动画效果
        if (energyData.productToday !== oldValues.productToday) {
          triggerValueUpdate("productToday");
        }
        if (energyData.costToday !== oldValues.costToday) {
          triggerValueUpdate("costToday");
        }
        if (energyData.remainToday !== oldValues.remainToday) {
          triggerValueUpdate("remainToday");
        }
        if (energyData.co2CostToday !== oldValues.co2CostToday) {
          triggerValueUpdate("co2CostToday");
        }
        if (energyData.co2RemainToday !== oldValues.co2RemainToday) {
          triggerValueUpdate("co2RemainToday");
        }
        if (energyData.waterToday !== oldValues.waterToday) {
          triggerValueUpdate("waterToday");
        }
      }
    } catch (error) {
      console.error("获取能耗数据失败:", error);
    }
  };

  // 流机能源数据获取
  const fetchLiujiEnergyLine = async () => {
    try {
      // 根据日期类型设置不同的参数
      let dataTypeParam = 4; // 默认为日

      if (dateType.value === "month") {
        dataTypeParam = 4; // 月
      } else if (dateType.value === "year") {
        dataTypeParam = 5; // 年
      }

      let params = {
        DataType: dataTypeParam
      };

      // 使用统一的参数获取方法（支持三种模式）
      params = demoModeStore.getDemoApiParams(params, dateType.value);
      console.log(
        `${demoModeStore.currentModeText} - 流机数据请求参数:`,
        params
      );

      const response = await api.GetLiujiEnergyLine(params);
      if (response && response.Status && response.Result) {
        // 解析返回的JSON字符串
        const chartData = JSON.parse(response.Result);
        // 清空旧数据
        _liujiEnergyData.names = [];
        _liujiEnergyData.energyValues = [];
        _liujiEnergyData.runtimeValues = [];

        // 处理X轴数据
        if (chartData.ChartX && chartData.ChartX.length > 0) {
          _liujiEnergyData.names = chartData.ChartX;
        }

        // 处理Y轴数据 - 适应新的API格式
        if (chartData.ChartTuY && chartData.ChartTuY.length > 0) {
          // 获取总电耗数据
          const energySeries = chartData.ChartTuY.find(
            series =>
              series.ChartTuLiId === "energy" || series.ChartTuLi === "总电耗"
          );

          if (energySeries && energySeries.ChartY) {
            _liujiEnergyData.energyValues = energySeries.ChartY.map(value =>
              parseFloat(value || "0")
            );
          }

          // 获取总工作时间数据（如果存在）
          const runtimeSeries = chartData.ChartTuY.find(
            series =>
              series.ChartTuLiId === "runtime" ||
              series.ChartTuLi === "总工作时长"
          );

          if (runtimeSeries && runtimeSeries.ChartY) {
            _liujiEnergyData.runtimeValues = runtimeSeries.ChartY.map(value =>
              parseFloat(value || "0")
            );
          } else {
            // 如果没有工作时间数据，设置为空数组
            _liujiEnergyData.runtimeValues = [];
          }
        }

        // 更新流机图表
        updateLiujiChart();
      }
    } catch (error) {
      console.error("获取流机能源数据失败:", error);
    }
  };

  // 更新流机能源图表
  const updateLiujiChart = () => {
    if (solarChartRef.value) {
      // 获取已存在的图表实例或创建新实例
      let chartInstance = echarts.getInstanceByDom(solarChartRef.value);
      // 如果没有实例，则初始化一个
      if (!chartInstance) {
        chartInstance = echarts.init(solarChartRef.value);
      }

      // 准备图表配置
      const option = {
        tooltip: {
          trigger: "axis",
          axisPointer: {
            type: "shadow"
          }
        },
        legend: {
          data: ["总用电量"],
          textStyle: {
            color: "#fff"
          }
        },
        grid: {
          left: "3%",
          right: "4%",
          bottom: "3%",
          containLabel: true
        },
        xAxis: [
          {
            type: "category",
            data: _liujiEnergyData.names,
            axisPointer: {
              type: "shadow"
            },
            axisLine: {
              lineStyle: {
                color: "rgba(255,255,255,0.3)"
              }
            },
            axisLabel: {
              color: "rgba(255,255,255,0.7)",
              rotate: 45,
              fontSize: 10
            }
          }
        ],
        yAxis: [
          {
            type: "value",
            name: "用电量(kWh)",
            nameTextStyle: {
              color: "rgba(255,255,255,0.7)"
            },
            min: 0,
            axisLine: {
              lineStyle: {
                color: "rgba(255,255,255,0.3)"
              }
            },
            axisLabel: {
              color: "rgba(255,255,255,0.7)",
              formatter: "{value}"
            },
            splitLine: {
              lineStyle: {
                color: "rgba(255,255,255,0.1)"
              }
            }
          }
        ],
        series: [
          {
            name: "总用电量",
            type: "bar",
            barWidth: "40%",
            data: _liujiEnergyData.energyValues,
            itemStyle: {
              color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                { offset: 0, color: "#00ff87" },
                { offset: 1, color: "#0078d4" }
              ])
            }
          }
        ]
      };

      // 如果有工作时间数据，添加到图表中
      if (
        _liujiEnergyData.runtimeValues &&
        _liujiEnergyData.runtimeValues.length > 0
      ) {
        // 添加工作时间到图例
        option.legend.data.push("总工作时长");

        // 添加第二个Y轴
        option.yAxis.push({
          type: "value",
          name: "工作时长(h)",
          nameTextStyle: {
            color: "rgba(255,255,255,0.7)"
          },
          min: 0,
          axisLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.3)"
            }
          },
          axisLabel: {
            color: "rgba(255,255,255,0.7)",
            formatter: "{value}"
          },
          splitLine: {
            lineStyle: {
              color: "rgba(255,255,255,0.1)"
            }
          }
        });

        // 添加工作时间系列
        option.series.push({
          name: "总工作时间",
          type: "line",
          yAxisIndex: 1,
          data: _liujiEnergyData.runtimeValues,
          symbol: "circle",
          symbolSize: 8,
          showSymbol: false,
          lineStyle: {
            width: 3,
            color: "#ffa726",
            shadowColor: "rgba(255, 167, 38, 0.5)",
            shadowBlur: 10
          },
          itemStyle: {
            color: "#ffa726"
          },
          markPoint: {
            data: [
              { type: "max", name: "最大值" },
              { type: "min", name: "最小值" }
            ]
          }
        });
      }

      // 设置图表配置
      chartInstance.setOption(option);

      // 添加窗口大小调整监听
      window.addEventListener("resize", () => {
        if (solarChartRef.value) {
          chartInstance.resize();
        }
      });
    }
  };

  // 获取楼层空调能耗数据
  const fetchAirEnergyLine = async () => {
    try {
      // 根据日期类型设置不同的参数
      let dataTypeParam = 2; // 默认为日

      if (dateType.value === "month") {
        dataTypeParam = 4; // 月
      } else if (dateType.value === "year") {
        dataTypeParam = 5; // 年
      }

      let params = {
        DataType: dataTypeParam
      };

      // 使用统一的参数获取方法（支持三种模式）
      params = demoModeStore.getDemoApiParams(params, dateType.value);
      console.log(
        `${demoModeStore.currentModeText} - 空调能耗数据请求参数:`,
        params
      );

      const response = await api.GetAirEnergyBar(params);
      if (response.Status) {
        const data = JSON.parse(response.Result);
        airEnergyData.dates = data.ChartX;
        airEnergyData.values = data.ChartTuY[0]?.ChartY || [];
        // 在数据获取后更新图表
        updateAirEnergyChart();
      }
    } catch (error) {
      console.error("获取楼层空调能耗数据失败:", error);
    }
  };

  // 更新楼层空调能耗图表
  const updateAirEnergyChart = () => {
    if (airEnergyChartRef.value) {
      const chart = echarts.init(airEnergyChartRef.value);
      const option = {
        title: {
          textStyle: {
            color: "#fff",
            fontSize: 16
          },
          left: "center"
        },
        tooltip: {
          trigger: "axis",
          formatter: "{b}<br/>{a}: {c} kWh",
          backgroundColor: "rgba(0, 20, 40, 0.85)",
          borderColor: "rgba(0, 162, 255, 0.5)",
          borderWidth: 1,
          textStyle: {
            color: "#fff"
          }
        },
        grid: {
          left: "3%",
          right: "4%",
          bottom: "3%",
          containLabel: true
        },
        xAxis: {
          type: "category",
          boundaryGap: true,
          data: airEnergyData.dates,
          axisLine: {
            lineStyle: {
              color: "#8392A2"
            }
          },
          axisLabel: {
            interval: 0,
            rotate: 45,
            color: "rgba(255,255,255,0.7)",
            fontSize: 10
          }
        },
        yAxis: {
          type: "value",
          name: "kWh",
          axisLine: {
            lineStyle: {
              color: "#8392A2"
            }
          },
          splitLine: {
            lineStyle: {
              color: "rgba(131, 146, 162, 0.2)"
            }
          }
        },
        series: [
          {
            name: "空调能耗",
            type: "bar",
            data: airEnergyData.values,
            barWidth: "50%",
            itemStyle: {
              color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                { offset: 0, color: "#00E5FF" },
                { offset: 1, color: "rgba(0, 129, 255, 0.7)" }
              ]),
              borderRadius: [5, 5, 0, 0]
            },
            markPoint: {
              data: [
                { type: "max", name: "最大值" },
                { type: "min", name: "最小值" }
              ],
              label: {
                color: "#fff"
              },
              itemStyle: {
                color: "rgba(0, 229, 255, 1)"
              }
            }
          }
        ]
      };
      chart.setOption(option);
    }
  };

  // 日期类型变更处理
  const handleDateTypeChange = type => {
    dateType.value = type;
    // 根据选择的日期类型更新数据
    fetchAlarmList();
    fetchEnergyData();
    fetchEnergyProductCostLine();
    fetchWarehouseEnergyBar();
    fetchQingNengLine();
    fetchScraptEnergyBar();
    fetchWaterEnergyLine();
    fetchFloorEnergyData();
    fetchLiujiEnergyLine();
    fetchAirEnergyLine();
  };

  // DOM引用
  const energyCompareChartRef = ref(null);
  const warehouseChartRef = ref(null);
  const waterChartRef = ref(null);
  const solarChartRef = ref(null);
  const forkliftChargerChartRef = ref(null);
  const floorEnergyChartRef = ref(null);
  const hydrogenChartRef = ref(null);
  const liujiChartRef = ref(null);
  const airEnergyChartRef = ref(null);

  // 流机数据
  const _liujiEnergyData = reactive({
    names: [],
    energyValues: [], // 总电耗数据
    runtimeValues: [] // 总工作时间数据
  });

  // 监听日期类型变化，获取各种数据
  watch(dateType, () => {
    fetchAlarmList();
    fetchEnergyData();
    fetchEnergyProductCostLine();
    fetchWarehouseEnergyBar();
    fetchQingNengLine();
    fetchScraptEnergyBar();
    fetchWaterEnergyLine();
    fetchFloorEnergyData();
    fetchLiujiEnergyLine();
    fetchAirEnergyLine();
  });

  // 在组件挂载时获取数据
  onMounted(() => {
    // 获取当前日期
    const now = new Date();
    currentDate.value = `${now.getFullYear()}-${
      now.getMonth() + 1
    }-${now.getDate()}`;

    // 设置定时器，每分钟刷新一次能耗数据
    energyTimer.value = setInterval(() => {
      fetchAlarmList();
      fetchEnergyData();
      fetchEnergyProductCostLine();
      fetchWarehouseEnergyBar();
      fetchQingNengLine();
      fetchScraptEnergyBar();
      fetchWaterEnergyLine();
      fetchFloorEnergyData();
      fetchLiujiEnergyLine();
      fetchAirEnergyLine();
    }, 180000);

    // 组件卸载时清除定时器
    onUnmounted(() => {
      clearInterval(energyTimer.value);
    });
  });
  // 更新现有数据并刷新图表
  const refreshAllData = () => {
    // 获取所有数据
    fetchAlarmList();
    fetchEnergyData();
    fetchEnergyProductCostLine();
    fetchWarehouseEnergyBar();
    fetchQingNengLine();
    fetchScraptEnergyBar();
    fetchWaterEnergyLine();
    fetchFloorEnergyData();
    fetchLiujiEnergyLine();
    fetchAirEnergyLine();
  };

  // 从hook中导出只需要的函数和数据
  return {
    // 图表引用
    energyCompareChartRef,
    hydrogenChartRef,
    warehouseChartRef,
    waterChartRef,
    solarChartRef,
    forkliftChargerChartRef,
    floorEnergyChartRef,
    liujiChartRef,
    airEnergyChartRef,
    // 数据获取函数
    fetchEnergyData,
    fetchEnergyProductCostLine,
    fetchWarehouseEnergyBar,
    fetchQingNengLine,
    fetchScraptEnergyBar,
    fetchWaterEnergyLine,
    fetchFloorEnergyData,
    fetchLiujiEnergyLine,
    fetchAirEnergyLine,
    fetchAlarmList,
    refreshAllData,
    // 数据切换函数
    handleDateTypeChange,
    switchChargerMonitorType,
    // 数据对象
    energyData,
    dateType,
    dateOptions,
    titleMapping,
    // 告警相关
    alertData,
    alertLoading,
    // 数值更新动画标志
    valueUpdateFlags,
    // 充电桩监测数据
    chargerMonitorData,
    // 演示模式相关
    demoModeStore
  };
}
