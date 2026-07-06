const shortcutsMonth = [
  {
    text: "近3个月",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setMonth(start.getMonth() - 3);
      return [start, end];
    }
  },
  {
    text: "近6个月",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setMonth(start.getMonth() - 6);
      return [start, end];
    }
  },
  {
    text: "近一年",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setFullYear(start.getFullYear() - 1);
      return [start, end];
    }
  },
  {
    text: "今年以来",
    value: () => {
      const end = new Date();
      const start = new Date(new Date().getFullYear(), 0);
      return [start, end];
    }
  }
];
const shortcutsYear = [
  {
    text: "近三年",
    value: () => {
      const end = new Date(); // 结束日期为当前日期
      const start = new Date();
      start.setFullYear(start.getFullYear() - 3); // 开始日期为当前日期往前推3年
      return [start, end]; // 返回日期范围数组
    }
  },
  {
    text: "近五年",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setFullYear(start.getFullYear() - 5);
      return [start, end];
    }
  },
  {
    text: "近十年",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setFullYear(start.getFullYear() - 10);
      return [start, end];
    }
  }
];

const shortcutsWeek = [
  {
    text: "本周",
    value: () => {
      const today = new Date();
      // 获取当前是周几 (0是周日，1是周一，以此类推)
      const day = today.getDay();
      // 计算本周的第一天（周一）
      const diff = day === 0 ? 6 : day - 1; // 如果是周日，则减去6天，否则减去(day-1)天
      const start = new Date(today);
      start.setDate(today.getDate() - diff);
      // 本周的最后一天（周日）
      const end = new Date(start);
      end.setDate(start.getDate() + 6);
      return [start, end];
    }
  },
  {
    text: "上周",
    value: () => {
      const today = new Date();
      const day = today.getDay();
      const diff = day === 0 ? 6 : day - 1;
      // 上周一
      const start = new Date(today);
      start.setDate(today.getDate() - diff - 7);
      // 上周日
      const end = new Date(start);
      end.setDate(start.getDate() + 6);
      return [start, end];
    }
  },
  {
    text: "最近两周",
    value: () => {
      const today = new Date();
      const day = today.getDay();
      const diff = day === 0 ? 6 : day - 1;
      // 上周一
      const start = new Date(today);
      start.setDate(today.getDate() - diff - 7);
      // 本周日
      const end = new Date(today);
      end.setDate(today.getDate() - diff + 6);
      return [start, end];
    }
  },
  {
    text: "最近四周",
    value: () => {
      const today = new Date();
      const day = today.getDay();
      const diff = day === 0 ? 6 : day - 1;
      // 四周前的周一
      const start = new Date(today);
      start.setDate(today.getDate() - diff - 21);
      // 本周日
      const end = new Date(today);
      end.setDate(today.getDate() - diff + 6);
      return [start, end];
    }
  }
];

const shortcutsDay = [
  {
    text: "今天",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setTime(start.getTime());
      return [start, end];
    }
  },
  {
    text: "最近3天",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setTime(start.getTime() - 3600 * 1000 * 24 * 3);
      return [start, end];
    }
  },
  {
    text: "最近7天",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setTime(start.getTime() - 3600 * 1000 * 24 * 7);
      return [start, end];
    }
  },
  {
    text: "最近30天",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setTime(start.getTime() - 3600 * 1000 * 24 * 30);
      return [start, end];
    }
  },
  {
    text: "最近90天",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setTime(start.getTime() - 3600 * 1000 * 24 * 90);
      return [start, end];
    }
  }
];

const nextShortcuts = [
  {
    text: "后3天",
    value: () => {
      const start = new Date();
      const end = new Date();
      end.setDate(end.getDate() + 2);
      return [start, end];
    }
  },
  {
    text: "后7天",
    value: () => {
      const start = new Date();
      const end = new Date();
      end.setDate(end.getDate() + 7);
      return [start, end];
    }
  },
  {
    text: "后15天",
    value: () => {
      const start = new Date();
      const end = new Date();
      end.setDate(end.getDate() + 7);
      return [start, end];
    }
  },
  {
    text: "后30天",
    value: () => {
      const start = new Date();
      const end = new Date();
      end.setDate(end.getDate() + 7);
      return [start, end];
    }
  }
];
const disabledDate = (time: Date) => {
  return (
    time.getTime() > Date.now() ||
    time.getTime() < Date.now() - 365 * 24 * 60 * 60 * 1000
  );
};

const disabledMonth = time => {
  const currentDate = new Date();
  const currentYear = currentDate.getFullYear();
  const currentMonth = currentDate.getMonth();
  return (
    time.getFullYear() > currentYear ||
    (time.getFullYear() === currentYear && time.getMonth() > currentMonth)
  );
};
const disabledYear = (time: Date) => {
  const tenYearsAgo = new Date();
  tenYearsAgo.setFullYear(tenYearsAgo.getFullYear() - 10); // 十年前的今天

  // 如果日期在十年前的今天之前或者在未来，则禁止选择
  return (
    time < tenYearsAgo || // 禁止选择十年前之前的日期
    time > new Date() // 禁止选择未来的日期
  );
};

// disabledDate
const disabledYesterdayDate = date => {
  // 获取今天的日期
  const today = new Date();
  today.setHours(0, 0, 0, 0); // 清除时间部分
  return date < today; // 禁用今天之前的日期
};

/**
 * 日期格式化
 */
export function dateFormat(date) {
  let format = "yyyy-MM-dd hh:mm:ss";
  if (date !== "Invalid Date") {
    const o = {
      "M+": date.getMonth() + 1, //month
      "d+": date.getDate(), //day
      "h+": date.getHours(), //hour
      "m+": date.getMinutes(), //minute
      "s+": date.getSeconds(), //second
      "q+": Math.floor((date.getMonth() + 3) / 3), //quarter
      S: date.getMilliseconds() //millisecond
    };
    if (/(y+)/.test(format))
      format = format.replace(
        RegExp.$1,
        (date.getFullYear() + "").substr(4 - RegExp.$1.length)
      );
    for (const k in o)
      if (new RegExp("(" + k + ")").test(format))
        format = format.replace(
          RegExp.$1,
          RegExp.$1.length === 1
            ? o[k]
            : ("00" + o[k]).substr(("" + o[k]).length)
        );
    return format;
  }
  return "";
}
export {
  shortcutsMonth,
  shortcutsDay,
  disabledYesterdayDate,
  nextShortcuts,
  shortcutsYear,
  shortcutsWeek,
  disabledMonth,
  disabledDate,
  disabledYear
};
