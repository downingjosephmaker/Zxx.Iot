import { emitter } from "@/utils/mitt";
import { downloadByData } from "@pureadmin/utils";
import { dayjs, ElNotification } from "element-plus";
import { h } from "vue";
import { utils, writeFile } from "xlsx";
import { message } from "./message";
import { getWapianUrl } from "@/config";

export default function useUtils() {
  // 配电图状态改变参数显示
  function revertFactoryJsonParameters(data: any, json: any) {
    if (!data || data === "") {
      return [];
    }
    const updateData: any = [];
    try {
      data.showData.forEach(item => {
        // const row = json?.find(it => it.title.includes("show" + item.device) && it.children.some(i => i.title == item.parameter[0].snowId));
        const row = json?.find(it => it.title.includes("show" + item.device));
        if (!row || row.children.length === 0) return;
        let isAlarm = false;
        const data = item.parameter.flatMap(it => {
          const index = row.children.findIndex(i => i.title == it.snowId);
          if (index === -1) return [];
          const child = row.children[index];
          if (!child || !child.props) return [];
          const propsLabelVal = child.props.label.val;
          const isStatus = propsLabelVal === "状态";
          const colorVal = isStatus ? "#fff" : it.isAlarm ? "#f00" : "#fff";
          const result = [
            {
              id: row.id,
              key: `children[${index}].props.value.val`,
              val: it.value || "--"
            },
            {
              id: row.id,
              key: `children[${index}].props.color.val`,
              val: colorVal
            }
          ];

          if (isStatus) {
            result.push({
              id: row.id,
              key: `children[${index}].props.label.val`,
              val: ""
            });
          }

          if (it.isAlarm) isAlarm = true;
          return result;
        });

        const stateRow = json?.find(i => i.title === "state." + item.device);
        if (stateRow) {
          data.push({
            id: stateRow.id,
            key:
              stateRow.tag === "circuit-circle"
                ? `props.fill.val`
                : `props.stroke.val`,
            val: isAlarm
              ? "#f00"
              : stateRow.tag === "circuit-circle"
                ? "#0f0"
                : "#fff"
          });
        }

        updateData.push(...data.filter(d => d.id !== ""));
      });

      data.stateData.forEach(item => {
        const row = json?.filter(it => it.title.includes("state"));
        const data = item.parameter.map(it => {
          const findRow = row?.find(i =>
            i.title.includes("state." + it.snowId)
          );
          if (!findRow || !findRow.tag)
            return { id: "", key: "props.state.val", val: false };
          let val = false;
          if (findRow.tag === "circuit-switch") {
            // val = item.parameter[0].value === "合闸";
            val = it.value === "合闸";
          }
          return {
            id: findRow.id,
            key: "props.state.val",
            val: !!val
          };
        });
        updateData.push(...data.filter(d => d.id !== ""));
      });
    } catch (error) {
      console.error("Error in revertFactoryJsonParameters:", error);
      return [];
    }
    return updateData;
  }
  function datasProcessingFun(data, data2) {
    const devD = {
      showData: [],
      stateData: [],
      alarmData: []
    };
    try {
      data.forEach(item => {
        const obj = {
          parameter: [],
          device: item.DeviceId
        };
        const obj2 = {
          parameter: [],
          device: item.DeviceId
        };
        // if (item.IsAlarm == 1) {
        //   devD.alarmData.push(item);
        // }
        item.ParamList.forEach(v => {
          const param = {
            snowId: v.ParamId,
            value: v.ParamValue + (v.ParamUnit || ""),
            isAlarm: item.IsAlarm !== 0
          };
          if (v.ValueType === "模拟量") {
            obj.parameter.push(param);
          } else {
            obj2.parameter.push(param);
          }
        });
        // 检查 parameter 是否为空
        if (obj.parameter.length > 0) {
          devD.showData.push(obj);
        }
        if (obj2.parameter.length > 0) {
          devD.stateData.push(obj2);
        }
      });
    } catch (error) {
      console.error("Error processing device data:", error);
    }
    return devD;
  }

  const formatParamsRoute = (newJson, json) => {
    return newJson;
    console.log(newJson, json);
    newJson.canvasCfg = json.canvasCfg;
    newJson.json.forEach(item => {
      item.binfo = json.json.find(it => it.id == item.id)?.binfo || item.binfo;
      item.props = json.json.find(it => it.id == item.id)?.props || item.props;
    });
    return newJson;
  };

  function doAlarmService(data) {
    let DataContent: any;
    if (!data.DataContent) {
      return;
    }
    // eslint-disable-next-line prefer-const
    DataContent = JSON.parse(data.DataContent);
    ElNotification({
      title:
        DataContent.AlarmLevel == 1
          ? alarmLevel1
          : DataContent.AlarmLevel == 2
            ? alarmLevel2
            : alarmLevel3,
      customClass: "z99999",
      message: (
        <div>
          <div>
            <label>报警类型</label>
            <span class="ml-1">
              {DataContent.AiarmType == 1
                ? "通讯状态"
                : DataContent.AiarmType == 2
                  ? "现场报警"
                  : "一级报警"}
            </span>
          </div>
          <div>
            <label>设备名称</label>
            <span class="ml-1">{DataContent.DeviceName}</span>
          </div>
          <div>
            <label>报警类型</label>
            <span class="ml-1">{DataContent.EventType}</span>
          </div>
          <div>
            <label>参数名称</label>
            <span class="ml-1">{DataContent.AlarmName}</span>
          </div>
          <div>
            <label>告警阈值</label>
            <span class="ml-1">{DataContent.AlarmName}</span>
          </div>
          <div>
            <label>告警内容</label>
            <span class="ml-1">{DataContent.AlarmValue}</span>
          </div>
          <div>
            <label>处理结果</label>
            <span class="ml-1">
              {DataContent.CheckResult == 1 ? "已处理" : "未处理"}
            </span>
          </div>
          <div>
            <label>处理备注</label>
            <span class="ml-1">{DataContent.CheckRemark}</span>
          </div>
        </div>
      ),
      position: "bottom-right",
      type:
        DataContent.AlarmLevel == 1
          ? "success"
          : DataContent.AlarmLevel == 2
            ? "warning"
            : "error",
      duration:
        DataContent.AlarmLevel == 1
          ? 5000
          : DataContent.AlarmLevel == 2
            ? 10000
            : 0
    });
  }

  function doBuildMapService() {
    console.log(11111111);
  }

  function getTimeFun(num) {
    return dayjs(new Date().getTime() - num * 24 * 60 * 60 * 1000).format(
      "YYYY-MM-DD 00:00:00"
    );
  }

  function getTimeFun2(num) {
    return dayjs(new Date().getTime() - num * 24 * 60 * 60 * 1000).format(
      "YYYY-MM-DD HH:mm:ss"
    );
  }

  function createSearchParam(name, type, value) {
    return {
      ParamName: name,
      ParamType: type,
      ParamValue: value,
      ParamSort: 0,
      ParamGroupName: "",
      GroupCondition: "",
      IsGroupFrist: true
    };
  }

  function exportFile(callback) {
    // downloadByData("res.data", "test-data.png");
  }

  const generateTitleList = (columns, parent = "") => {
    const titleList = [];
    const titleProp = [];
    columns.forEach(column => {
      if (column.children) {
        const childrenTitleList = generateTitleList(
          column.children,
          parent + column.label + " - "
        );
        titleList.push(...childrenTitleList[0]);
        titleProp.push(...childrenTitleList[1]);
      } else {
        if (column.label != "操作") {
          titleList.push(parent + column.label);
          titleProp.push(column.prop || "index");
        }
      }
    });
    return [titleList, titleProp];
  };
  const exportExcel = (columns, dataList, fileName) => {
    const res = [];
    const titleList = generateTitleList(columns);
    const formatDataList = (list, parentIndex = "") => {
      list.forEach((item, index) => {
        const arr = [];
        titleList[1].forEach(column => {
          if (column == "index") arr.push(parentIndex + (index + 1));
          else arr.push(item[column as string]);
        });
        res.push(arr);
        if (item.children?.length > 0) {
          formatDataList(item.children, parentIndex + (index + 1) + ".");
        }
      });
    };
    formatDataList(dataList);
    res.unshift(titleList[0]);
    const workSheet = utils.aoa_to_sheet(res);
    const workBook = utils.book_new();
    utils.book_append_sheet(workBook, workSheet, "数据报表");
    writeFile(workBook, fileName + ".xlsx");
    message("导出成功", {
      type: "success"
    });
  };

  function extractNumberFromTitle(title: string): string | "" {
    const regex = /.*\(([^()]*)\)/g;
    let match;
    let lastMatch = "";
    // 执行正则表达式的全局搜索
    while ((match = regex.exec(title)) !== null) {
      // 获取最后一次匹配的结果
      lastMatch = match[1];
    }
    return lastMatch ? lastMatch : "";
  }
  function downFile(data, onSuccess, onError) {
    // if (!data.Result) {
    //   loading.value = false;
    //   return;
    // }
    // loading.value = false;
    const baseUrl = getWapianUrl() + data.Result;
    const a = document.createElement("a"); // 创建一个<a></a>标签
    a.href = baseUrl; //重点（如测试发现下载文件不存在/找不到，检查路径）
    // a.download = name; // 设置下载文件文件名
    a.style.display = "none"; // 隐藏a标签
    document.body.appendChild(a); // 将a标签追加到文档对象中
    a.click(); // 模拟点击了a标签，会触发a标签的href的读取，浏览器就会自动下载了
    a.remove(); // 一次性的，用完就删除a标签
    // 检查文件是否成功下载
    const checkDownload = () => {
      const intervalId = setInterval(() => {
        const fileName = data.Result.split("/").pop();
        const downloadComplete = Array.from(
          document.querySelectorAll("a")
        ).some(link => link.href === baseUrl && link.download === fileName);

        if (downloadComplete) {
          clearInterval(intervalId);
          if (onSuccess) onSuccess();
        } else {
          // 如果超过一定时间仍未下载成功，视为失败
          const timeout = 5000; // 5秒超时
          setTimeout(() => {
            clearInterval(intervalId);
            if (onError) onError(new Error("文件下载超时"));
          }, timeout);
        }
      }, 500); // 每500毫秒检查一次
    };
    checkDownload();
  }
  return {
    datasProcessingFun,
    formatParamsRoute,
    exportFile,
    exportExcel,
    revertFactoryJsonParameters,
    createSearchParam,
    doAlarmService,
    doBuildMapService,
    getTimeFun,
    getTimeFun2,
    extractNumberFromTitle,
    downFile
  };
}
