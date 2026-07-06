const chart1Options = {
  title: {},
  // color: ["#e7a900", "#12bb5d", "#ff4949", "#b4a3e9"],
  tooltip: {
    trigger: "axis"
  },
  legend: {},
  grid: {
    left: "3%",
    right: "7%",
    bottom: "5%",
    containLabel: true
  },
  toolbox: {
    show: true,
    iconStyle: {
      borderColor: "#000" // 图标默认颜色
    },
    emphasis: {
      iconStyle: {
        borderColor: "#000" // 图标hover颜色
      }
    },
    feature: {
      magicType: {
        type: ["line", "bar"],
        title: {
          line: "切换为折线图",
          bar: "切换为柱状图"
        }
      },
      saveAsImage: {
        backgroundColor: "#fff",
        title: "保存为图片"
      }
    }
  },
  xAxis: {
    type: "category",
    boundaryGap: true,
    data: [],
    axisTick: { show: false },
    axisLine: {
      show: true,
      lineStyle: {
        color: "#000"
      }
    }
  },
  yAxis: [
    {
      name: "kW.h",
      type: "value",
      axisLine: {
        show: true,
        lineStyle: {
          color: "#000"
        }
      },
      axisTick: { show: false },
      splitLine: {
        lineStyle: {
          color: "#0000"
        }
      },
      nameTextStyle: {
        color: "#000"
      }
    },
    {
      name: "%",
      type: "value",
      axisLine: {
        show: true,
        lineStyle: {
          color: "#000"
        }
      },
      axisTick: { show: false },
      max: "dataMax",
      splitLine: {
        lineStyle: {
          color: "#0000"
        }
      },
      nameTextStyle: {
        color: "#000"
      }
    }
  ],
  dataZoom: [],
  series: []
};
export { chart1Options };
