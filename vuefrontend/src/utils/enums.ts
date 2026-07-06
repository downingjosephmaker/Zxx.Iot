/**
 * 枚举
 */

/** 通过value寻找枚举 */
export const findByValue = (enumSet, value) => {
  return toList(enumSet).find(e => e.value == value);
};

/** 转成列表 */
export const toList = enumSet => {
  const list = [];
  Object.keys(enumSet).forEach(function (key) {
    list.push({ ...enumSet[key] });
  });
  return list;
};

/** 告警等级 **/
export const ALARM_LEVEL = {
  base: { value: 1, label: "一般" },
  secondary: { value: 2, label: "次要" },
  important: { value: 3, label: "重要" },
  emergent: { value: 4, label: "紧急" }
};

/** 告警类型 **/
export const ALARM_TYPE = {
  accidentAlarm: { value: 1, label: "通讯状态" },
  preAlertalarm: { value: 2, label: "现场报警" },
  preTestAlertalarm: { value: 3, label: "一级报警" }
};

/** 环境类型 **/
export const ENV_TYPE = {
  accidentAlarm: { value: 1, label: "SF₆气体监测" }
  // preAlertalarm: { value: 2, label: "局部放电" },
  // preTestAlertalarm: { value: 3, label: "电缆沟积水" }
};

/** 筛选日期类型 **/
export const DATE_TYPE = {
  hour: { value: 0, label: "按时" },
  day: { value: 1, label: "按日" },
  weeks: { value: 2, label: "按周" },
  month: { value: 3, label: "按月" },
  year: { value: 4, label: "按年" }
};

/** 提醒日期类型 **/
export const REMINDER_TIME = {
  oneDay: { value: 1, label: "一天一次" },
  twoDay: { value: 2, label: "两天一次" },
  threeDay: { value: 3, label: "三天一次" },
  fourDay: { value: 4, label: "四天一次" },
  fiveDay: { value: 5, label: "五天一次" },
  sixDay: { value: 6, label: "六天一次" },
  sevenDay: { value: 7, label: "七天一次" }
};

/** 直流屏 **/
export const DC_SCREEN = {
  hmdy: { value: 1, label: "合母电压" },
  kmdy: { value: 2, label: "控母电压" },
  dczdl: { value: 3, label: "电池总电流" },
  kmdl: { value: 4, label: "控母电流" },
  jlydy: { value: 5, label: "交流一路相电压" },
  jledy: { value: 6, label: "交流二路相电压" }
};

/** 直流屏 **/
export const SHIELD_TYPE = {
  custom: { value: 0, label: "自定义时间段" },
  weed: { value: 1, label: "按周屏蔽" },
  forever: { value: 2, label: "永久屏蔽" }
};

/** 直流屏 **/
export const FILE_TYPE = {
  // custom: { value: 0, label: "无" },
  weed: { value: 1, label: "图片" },
  forever: { value: 2, label: "文档" }
};
