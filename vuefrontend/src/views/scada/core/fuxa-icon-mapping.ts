// FUXA组件库图标映射配置
// 基于 FUXA 的组件库HTML和CSS文件创建的图标映射表

export interface ComponentIconMapping {
  name: string;
  title: string;
  iconPath: string;
  category: string;
  description?: string;
}

// 常规分组
export const regularComponents: ComponentIconMapping[] = [
  {
    name: "select",
    title: "选择工具",
    iconPath: "@/assets/svg/select-pointer.svg",
    category: "常规"
  },
  {
    name: "text",
    title: "文本工具",
    iconPath: "@/assets/svg/text.svg",
    category: "常规"
  },
  {
    name: "text-card",
    title: "文本卡片",
    iconPath: "@/assets/svg/text-card.svg",
    category: "常规",
    description: "多行文本卡片,支持标题和正文,适合信息展示"
  },
  {
    name: "stat-card",
    title: "统计数值卡",
    iconPath: "@/assets/svg/text-card.svg",
    category: "常规",
    description: "大数字+单位+标题,绑点位显示统计指标,报表常用"
  },
  {
    name: "line",
    title: "直线工具",
    iconPath: "@/assets/svg/line.svg",
    category: "常规"
  },
  {
    name: "path",
    title: "路径工具",
    iconPath: "@/assets/svg/path.svg",
    category: "常规"
  },
  {
    name: "image",
    title: "图像工具",
    iconPath: "@/assets/svg/image.svg",
    category: "常规"
  },
  {
    name: "pencil",
    title: "铅笔工具",
    iconPath: "@/assets/svg/pencil.svg",
    category: "常规"
  },
  {
    name: "rect",
    title: "矩形工具",
    iconPath: "@/assets/svg/rect.svg",
    category: "常规"
  },
  {
    name: "circle",
    title: "圆圈工具",
    iconPath: "@/assets/svg/circle.svg",
    category: "常规"
  },
  {
    name: "ellipse",
    title: "椭圆工具",
    iconPath: "@/assets/svg/ellipse.svg",
    category: "常规"
  },
  // 新增的形状绘画工具
  {
    name: "triangle",
    title: "三角形",
    iconPath: "@/assets/svg/triangle.svg",
    category: "常规"
  },
  {
    name: "arrow",
    title: "箭头",
    iconPath: "@/assets/svg/arrow.svg",
    category: "常规"
  },
  {
    name: "double-arrow",
    title: "双向箭头",
    iconPath: "@/assets/svg/double-arrow.svg",
    category: "常规"
  },
  {
    name: "cloud",
    title: "云朵",
    iconPath: "@/assets/svg/cloud.svg",
    category: "常规"
  },
  {
    name: "chat-bubble",
    title: "对话框",
    iconPath: "@/assets/svg/chat-bubble.svg",
    category: "常规"
  },
  {
    name: "lightning",
    title: "闪电",
    iconPath: "@/assets/svg/lightning.svg",
    category: "常规"
  },
  {
    name: "cross",
    title: "叉号",
    iconPath: "@/assets/svg/cross.svg",
    category: "常规"
  },
  {
    name: "checkmark",
    title: "对勾",
    iconPath: "@/assets/svg/checkmark.svg",
    category: "常规"
  },
  {
    name: "info",
    title: "信息",
    iconPath: "@/assets/svg/info.svg",
    category: "常规"
  },
  {
    name: "backspace",
    title: "退格",
    iconPath: "@/assets/svg/backspace.svg",
    category: "常规"
  },
  {
    name: "minus",
    title: "减号",
    iconPath: "@/assets/svg/minus.svg",
    category: "常规"
  },
  {
    name: "speaker",
    title: "扬声器",
    iconPath: "@/assets/svg/speaker.svg",
    category: "常规"
  },
  {
    name: "search",
    title: "搜索",
    iconPath: "@/assets/svg/search.svg",
    category: "常规"
  },
  {
    name: "puzzle",
    title: "拼图",
    iconPath: "@/assets/svg/puzzle.svg",
    category: "常规"
  },
  {
    name: "heart",
    title: "心形",
    iconPath: "@/assets/svg/heart.svg",
    category: "常规"
  },
  {
    name: "bell",
    title: "铃铛",
    iconPath: "@/assets/svg/bell.svg",
    category: "常规"
  }
];

// 控制组件
export const controlComponents: ComponentIconMapping[] = [
  {
    name: "button",
    title: "按钮",
    iconPath: "@/assets/svg/button.svg",
    category: "控制"
  },
  {
    name: "progress-v",
    title: "进度条",
    iconPath: "@/assets/svg/progress-v.svg",
    category: "控制"
  },
  {
    name: "led-display",
    title: "LED 显示屏",
    iconPath: "@/assets/svg/led-display.svg",
    category: "控制",
    description: "炫酷 LED 显示屏,支持数字/文字/多行文本,带发光效果和动画"
  },
  {
    name: "thermometer",
    title: "温度计",
    iconPath: "@/assets/svg/thermometer.svg",
    category: "控制"
  },
  {
    name: "pipe",
    title: "管道",
    iconPath: "@/assets/svg/pipe.svg",
    category: "控制"
  },
  {
    name: "switch",
    title: "开关",
    iconPath: "@/assets/svg/switch.svg",
    category: "控制"
  },
  {
    name: "unified-chart",
    title: "统一图表",
    iconPath: "@/assets/svg/chart.svg",
    category: "控制",
    description: "支持折线图、条形图和饼图的统一图表组件，默认为折线图"
  },
  {
    name: "table",
    title: "表",
    iconPath: "@/assets/svg/table.svg",
    category: "控制"
  },
  {
    name: "iframe",
    title: "内嵌网页",
    iconPath: "@/assets/svg/iframe.svg",
    category: "控制"
  },
  {
    name: "paint-board",
    title: "画板",
    iconPath: "@/assets/svg/paint-board.svg",
    category: "控制",
    description: "提供完整的绘图工具集,支持画笔、形状、文本等功能"
  },
  {
    name: "video",
    title: "视频播放器",
    iconPath: "@/assets/svg/video.svg",
    category: "控制",
    description: "用于播放视频文件，支持常见格式（MP4、WebM等）"
  },
  {
    name: "webcam",
    title: "实时摄像头",
    iconPath: "@/assets/svg/webcam.svg",
    category: "控制",
    description: "实时视频流监控，支持RTSP、RTMP、HLS、WebRTC等协议"
  }
];

// 图形分组
export const shapeComponents: ComponentIconMapping[] = [
  {
    name: "shape-rectangle",
    title: "矩形",
    iconPath: "@/assets/svg/shape-rectangle.svg",
    category: "图形"
  },
  {
    name: "shape-circle",
    title: "圆形",
    iconPath: "@/assets/svg/shape-circle.svg",
    category: "图形"
  },
  {
    name: "shape-diamond",
    title: "菱形",
    iconPath: "@/assets/svg/shape-diamond.svg",
    category: "图形"
  },
  {
    name: "shape-triangle",
    title: "三角形",
    iconPath: "@/assets/svg/shape-triangle.svg",
    category: "图形"
  },
  {
    name: "shape-halfcircle",
    title: "半圆",
    iconPath: "@/assets/svg/shape-halfcircle.svg",
    category: "图形"
  },
  {
    name: "shape-delay",
    title: "",
    iconPath: "@/assets/svg/shape-delay.svg",
    category: "图形"
  },
  {
    name: "shape-looplimit",
    title: "",
    iconPath: "@/assets/svg/shape-looplimit.svg",
    category: "图形"
  },
  {
    name: "shape-prepara",
    title: "",
    iconPath: "@/assets/svg/shape-prepara.svg",
    category: "图形"
  },
  {
    name: "shape-trape",
    title: "梯形",
    iconPath: "@/assets/svg/shape-trape.svg",
    category: "图形"
  },
  {
    name: "shape-offpage",
    title: "",
    iconPath: "@/assets/svg/shape-offpage.svg",
    category: "图形"
  },
  {
    name: "shape-parallelogram",
    title: "平行四边形",
    iconPath: "@/assets/svg/shape-parallelogram.svg",
    category: "图形"
  },
  {
    name: "shape-maninput",
    title: "",
    iconPath: "@/assets/svg/shape-maninput.svg",
    category: "图形"
  },
  {
    name: "shape-pentagon",
    title: "五边形",
    iconPath: "@/assets/svg/shape-pentagon.svg",
    category: "图形"
  },
  {
    name: "shape-octagon",
    title: "八边形",
    iconPath: "@/assets/svg/shape-octagon.svg",
    category: "图形"
  },
  {
    name: "shape-ticket",
    title: "",
    iconPath: "@/assets/svg/shape-ticket.svg",
    category: "图形"
  },
  {
    name: "shape-display",
    title: "",
    iconPath: "@/assets/svg/shape-display.svg",
    category: "图形"
  },
  {
    name: "shape-or2",
    title: "",
    iconPath: "@/assets/svg/shape-or2.svg",
    category: "图形"
  },
  {
    name: "shape-vor",
    title: "",
    iconPath: "@/assets/svg/shape-vor.svg",
    category: "图形"
  },
  {
    name: "shape-tape",
    title: "",
    iconPath: "@/assets/svg/shape-tape.svg",
    category: "图形"
  },
  {
    name: "shape-docu",
    title: "",
    iconPath: "@/assets/svg/shape-docu.svg",
    category: "图形"
  },
  {
    name: "shape-cloud",
    title: "",
    iconPath: "@/assets/svg/shape-cloud.svg",
    category: "图形"
  },
  {
    name: "shape-or",
    title: "",
    iconPath: "@/assets/svg/shape-or.svg",
    category: "图形"
  },
  {
    name: "shape-switch",
    title: "",
    iconPath: "@/assets/svg/shape-switch.svg",
    category: "图形"
  },
  {
    name: "shape-star4",
    title: "四角星",
    iconPath: "@/assets/svg/shape-star4.svg",
    category: "图形"
  },
  {
    name: "shape-poval",
    title: "椭圆形",
    iconPath: "@/assets/svg/shape-poval.svg",
    category: "图形"
  },
  {
    name: "shape-drop",
    title: "水滴",
    iconPath: "@/assets/svg/shape-drop.svg",
    category: "图形"
  },
  {
    name: "shape-heart",
    title: "心形",
    iconPath: "@/assets/svg/shape-heart.svg",
    category: "图形"
  },
  {
    name: "shape-nosymbol",
    title: "",
    iconPath: "@/assets/svg/shape-nosymbol.svg",
    category: "图形"
  },
  {
    name: "shape-cylinder",
    title: "圆柱体",
    iconPath: "@/assets/svg/shape-cylinder.svg",
    category: "图形"
  },
  {
    name: "shape-cone",
    title: "圆锥体",
    iconPath: "@/assets/svg/shape-cone.svg",
    category: "图形"
  },
  {
    name: "shape-cross",
    title: "十字",
    iconPath: "@/assets/svg/shape-cross.svg",
    category: "图形"
  },
  {
    name: "shape-corner",
    title: "角",
    iconPath: "@/assets/svg/shape-corner.svg",
    category: "图形"
  },
  {
    name: "shape-tee",
    title: "T形",
    iconPath: "@/assets/svg/shape-tee.svg",
    category: "图形"
  },
  {
    name: "shape-arrow",
    title: "箭头",
    iconPath: "@/assets/svg/shape-arrow.svg",
    category: "图形"
  },
  {
    name: "shape-doublearrow",
    title: "双箭头",
    iconPath: "@/assets/svg/shape-doublearrow.svg",
    category: "图形"
  },
  {
    name: "shape-rectindi",
    title: "矩形指示器",
    iconPath: "@/assets/svg/shape-rectindi.svg",
    category: "图形"
  },
  {
    name: "shape-circleindi",
    title: "圆形指示器",
    iconPath: "@/assets/svg/shape-circleindi.svg",
    category: "图形"
  },
  {
    name: "shape-radiuskorner",
    title: "圆角",
    iconPath: "@/assets/svg/shape-radiuskorner.svg",
    category: "图形"
  },
  {
    name: "shape-circlehalf",
    title: "半圆形",
    iconPath: "@/assets/svg/shape-circlehalf.svg",
    category: "图形"
  }
];

// 工艺工程组件
export const processEngineeringComponents: ComponentIconMapping[] = [
  // 泵类 (Pumps)
  {
    name: "centrifugal",
    title: "离心泵",
    iconPath: "@/assets/svg/centrifugal.svg",
    category: "工业"
  },
  {
    name: "diaph",
    title: "隔膜泵",
    iconPath: "@/assets/svg/diaph.svg",
    category: "工业"
  },
  {
    name: "pumphidra",
    title: "液压泵",
    iconPath: "@/assets/svg/pumphidra.svg",
    category: "工业"
  },
  {
    name: "pumpjet",
    title: "喷射泵",
    iconPath: "@/assets/svg/pumpjet.svg",
    category: "工业"
  },
  {
    name: "pumpgear",
    title: "齿轮泵",
    iconPath: "@/assets/svg/pumpgear.svg",
    category: "工业"
  },
  {
    name: "motor-simb",
    title: "电机符号",
    iconPath: "@/assets/svg/motor-simb.svg",
    category: "工业"
  },
  {
    name: "pumpturbi",
    title: "涡轮泵",
    iconPath: "@/assets/svg/pumpturbi.svg",
    category: "工业"
  },
  {
    name: "pumpcentri1",
    title: "离心泵1",
    iconPath: "@/assets/svg/pumpcentri1.svg",
    category: "工业"
  },
  {
    name: "pumpvacuum",
    title: "真空泵",
    iconPath: "@/assets/svg/pumpvacuum.svg",
    category: "工业"
  },
  {
    name: "pumpcentri2",
    title: "离心泵2",
    iconPath: "@/assets/svg/pumpcentri2.svg",
    category: "工业"
  },
  {
    name: "pumpscreew",
    title: "螺杆泵",
    iconPath: "@/assets/svg/pumpscreew.svg",
    category: "工业"
  },
  {
    name: "pumpblower",
    title: "鼓风机",
    iconPath: "@/assets/svg/pumpblower.svg",
    category: "工业"
  },
  {
    name: "pumpgear2",
    title: "齿轮泵2",
    iconPath: "@/assets/svg/pumpgear2.svg",
    category: "工业"
  },
  {
    name: "pumphorizo",
    title: "水平泵",
    iconPath: "@/assets/svg/pumphorizo.svg",
    category: "工业"
  },
  {
    name: "pumpscreew2",
    title: "螺杆泵2",
    iconPath: "@/assets/svg/pumpscreew2.svg",
    category: "工业"
  },
  {
    name: "pumpperis",
    title: "蠕动泵",
    iconPath: "@/assets/svg/pumpperis.svg",
    category: "工业"
  },
  {
    name: "pumpfeed",
    title: "给料泵",
    iconPath: "@/assets/svg/pumpfeed.svg",
    category: "工业"
  },

  // 压缩机类 (Compressors)
  {
    name: "compressor-void",
    title: "真空压缩机",
    iconPath: "@/assets/svg/compressor-void.svg",
    category: "工业"
  },
  {
    name: "compressor-fan",
    title: "风扇压缩机",
    iconPath: "@/assets/svg/compressor-fan.svg",
    category: "工业"
  },
  {
    name: "compressor-piston",
    title: "活塞压缩机",
    iconPath: "@/assets/svg/compressor-piston.svg",
    category: "工业"
  },
  {
    name: "compring",
    title: "环形压缩机",
    iconPath: "@/assets/svg/compring.svg",
    category: "工业"
  },
  {
    name: "compejector",
    title: "喷射压缩机",
    iconPath: "@/assets/svg/compejector.svg",
    category: "工业"
  },
  {
    name: "compdiaph",
    title: "隔膜压缩机",
    iconPath: "@/assets/svg/compdiaph.svg",
    category: "工业"
  },
  {
    name: "comprotary",
    title: "旋转压缩机",
    iconPath: "@/assets/svg/comprotary.svg",
    category: "工业"
  },
  {
    name: "compscrew",
    title: "螺杆压缩机",
    iconPath: "@/assets/svg/compscrew.svg",
    category: "工业"
  },
  {
    name: "compair",
    title: "空气压缩机",
    iconPath: "@/assets/svg/compair.svg",
    category: "工业"
  },
  {
    name: "compreci",
    title: "往复压缩机",
    iconPath: "@/assets/svg/compreci.svg",
    category: "工业"
  },
  {
    name: "compreci2",
    title: "往复压缩机2",
    iconPath: "@/assets/svg/compreci2.svg",
    category: "工业"
  },
  {
    name: "compsilence",
    title: "静音压缩机",
    iconPath: "@/assets/svg/compsilence.svg",
    category: "工业"
  },
  {
    name: "compturbo",
    title: "涡轮压缩机",
    iconPath: "@/assets/svg/compturbo.svg",
    category: "工业"
  },
  {
    name: "comprotary2",
    title: "旋转压缩机2",
    iconPath: "@/assets/svg/comprotary2.svg",
    category: "工业"
  },
  {
    name: "compring2",
    title: "环形压缩机2",
    iconPath: "@/assets/svg/compring2.svg",
    category: "工业"
  },

  // 阀门类 (Valves)
  {
    name: "valve-ax",
    title: "轴向阀",
    iconPath: "@/assets/svg/valve-ax.svg",
    category: "工业"
  },
  {
    name: "valve-bx",
    title: "B型阀",
    iconPath: "@/assets/svg/valve-bx.svg",
    category: "工业"
  },
  {
    name: "valve-cx",
    title: "C型阀",
    iconPath: "@/assets/svg/valve-cx.svg",
    category: "工业"
  },

  // 换热器类 (Heat Exchangers)
  {
    name: "exchanger-tube",
    title: "管式换热器",
    iconPath: "@/assets/svg/exchanger-tube.svg",
    category: "工业"
  },
  {
    name: "exchanger-heat",
    title: "热交换器",
    iconPath: "@/assets/svg/exchanger-heat.svg",
    category: "工业"
  },
  {
    name: "exchanger1",
    title: "换热器1",
    iconPath: "@/assets/svg/exchanger1.svg",
    category: "工业"
  },
  {
    name: "exchanger2",
    title: "换热器2",
    iconPath: "@/assets/svg/exchanger2.svg",
    category: "工业"
  },
  {
    name: "exchanger3",
    title: "换热器3",
    iconPath: "@/assets/svg/exchanger3.svg",
    category: "工业"
  },
  {
    name: "exchanger4",
    title: "换热器4",
    iconPath: "@/assets/svg/exchanger4.svg",
    category: "工业"
  },
  {
    name: "exchanger5",
    title: "换热器5",
    iconPath: "@/assets/svg/exchanger5.svg",
    category: "工业"
  },
  {
    name: "exchanger6",
    title: "换热器6",
    iconPath: "@/assets/svg/exchanger6.svg",
    category: "工业"
  },
  {
    name: "exchanger7",
    title: "换热器7",
    iconPath: "@/assets/svg/exchanger7.svg",
    category: "工业"
  },
  {
    name: "exchanger8",
    title: "换热器8",
    iconPath: "@/assets/svg/exchanger8.svg",
    category: "工业"
  },
  {
    name: "exchanger9",
    title: "换热器9",
    iconPath: "@/assets/svg/exchanger9.svg",
    category: "工业"
  },
  {
    name: "exchanger-filter",
    title: "过滤换热器",
    iconPath: "@/assets/svg/exchanger-filter.svg",
    category: "工业"
  },

  // 搅拌器类 (Agitators)
  {
    name: "agitator-prop",
    title: "螺旋桨搅拌器",
    iconPath: "@/assets/svg/agitator-prop.svg",
    category: "工业"
  },
  {
    name: "agitator-turbo",
    title: "涡轮搅拌器",
    iconPath: "@/assets/svg/agitator-turbo.svg",
    category: "工业"
  },
  {
    name: "agitator-disc",
    title: "圆盘搅拌器",
    iconPath: "@/assets/svg/agitator-disc.svg",
    category: "工业"
  },
  {
    name: "agitator-paddle",
    title: "桨式搅拌器",
    iconPath: "@/assets/svg/agitator-paddle.svg",
    category: "工业"
  },

  // 离心机类 (Centrifuges)
  {
    name: "centrifuge1",
    title: "离心机1",
    iconPath: "@/assets/svg/centrifuge1.svg",
    category: "工业"
  },
  {
    name: "centrifuge2",
    title: "离心机2",
    iconPath: "@/assets/svg/centrifuge2.svg",
    category: "工业"
  },
  {
    name: "centrifuge3",
    title: "离心机3",
    iconPath: "@/assets/svg/centrifuge3.svg",
    category: "工业"
  },
  {
    name: "centrifuge4",
    title: "离心机4",
    iconPath: "@/assets/svg/centrifuge4.svg",
    category: "工业"
  },

  // 破碎机类 (Crushers)
  {
    name: "crusher1",
    title: "破碎机1",
    iconPath: "@/assets/svg/crusher1.svg",
    category: "工业"
  },
  {
    name: "crusher2",
    title: "破碎机2",
    iconPath: "@/assets/svg/crusher2.svg",
    category: "工业"
  },
  {
    name: "crusher3",
    title: "破碎机3",
    iconPath: "@/assets/svg/crusher3.svg",
    category: "工业"
  },
  {
    name: "crusher4",
    title: "破碎机4",
    iconPath: "@/assets/svg/crusher4.svg",
    category: "工业"
  },
  {
    name: "crusher5",
    title: "破碎机5",
    iconPath: "@/assets/svg/crusher5.svg",
    category: "工业"
  },
  {
    name: "crusher6",
    title: "破碎机6",
    iconPath: "@/assets/svg/crusher6.svg",
    category: "工业"
  },
  {
    name: "crusher7",
    title: "破碎机7",
    iconPath: "@/assets/svg/crusher7.svg",
    category: "工业"
  },

  // 干燥器类 (Dryers)
  {
    name: "drier1",
    title: "干燥器1",
    iconPath: "@/assets/svg/drier1.svg",
    category: "工业"
  },
  {
    name: "drier2",
    title: "干燥器2",
    iconPath: "@/assets/svg/drier2.svg",
    category: "工业"
  },
  {
    name: "drier3",
    title: "干燥器3",
    iconPath: "@/assets/svg/drier3.svg",
    category: "工业"
  },
  {
    name: "drier4",
    title: "干燥器4",
    iconPath: "@/assets/svg/drier4.svg",
    category: "工业"
  },
  {
    name: "drier5",
    title: "干燥器5",
    iconPath: "@/assets/svg/drier5.svg",
    category: "工业"
  },

  // 喷嘴类 (Nozzles)
  {
    name: "nozzle",
    title: "喷嘴",
    iconPath: "@/assets/svg/nozzle.svg",
    category: "工业"
  },
  {
    name: "nozzle2",
    title: "喷嘴2",
    iconPath: "@/assets/svg/nozzle2.svg",
    category: "工业"
  },

  // 给料器类 (Feeders)
  {
    name: "feeder",
    title: "给料器",
    iconPath: "@/assets/svg/feeder.svg",
    category: "工业"
  },
  {
    name: "feeder2",
    title: "给料器2",
    iconPath: "@/assets/svg/feeder2.svg",
    category: "工业"
  },
  {
    name: "feeder3",
    title: "给料器3",
    iconPath: "@/assets/svg/feeder3.svg",
    category: "工业"
  },

  // 过滤器类 (Filters)
  {
    name: "filter2",
    title: "过滤器2",
    iconPath: "@/assets/svg/filter2.svg",
    category: "工业"
  },
  {
    name: "filter3",
    title: "过滤器3",
    iconPath: "@/assets/svg/filter3.svg",
    category: "工业"
  },

  // 管件类 (Fittings)
  {
    name: "fitting1",
    title: "管件1",
    iconPath: "@/assets/svg/fitting1.svg",
    category: "工业"
  },
  {
    name: "fitting2",
    title: "管件2",
    iconPath: "@/assets/svg/fitting2.svg",
    category: "工业"
  },
  {
    name: "fitting3",
    title: "管件3",
    iconPath: "@/assets/svg/fitting3.svg",
    category: "工业"
  },
  {
    name: "fitting4",
    title: "管件4",
    iconPath: "@/assets/svg/fitting4.svg",
    category: "工业"
  },
  {
    name: "fitting5",
    title: "管件5",
    iconPath: "@/assets/svg/fitting5.svg",
    category: "工业"
  },
  {
    name: "fitting6",
    title: "管件6",
    iconPath: "@/assets/svg/fitting6.svg",
    category: "工业"
  },
  {
    name: "fitting7",
    title: "管件7",
    iconPath: "@/assets/svg/fitting7.svg",
    category: "工业"
  },
  {
    name: "fitting8",
    title: "管件8",
    iconPath: "@/assets/svg/fitting8.svg",
    category: "工业"
  },
  {
    name: "fitting9",
    title: "管件9",
    iconPath: "@/assets/svg/fitting9.svg",
    category: "工业"
  },
  {
    name: "fitting10",
    title: "管件10",
    iconPath: "@/assets/svg/fitting10.svg",
    category: "工业"
  },
  {
    name: "fitting11",
    title: "管件11",
    iconPath: "@/assets/svg/fitting11.svg",
    category: "工业"
  },
  {
    name: "fitting12",
    title: "管件12",
    iconPath: "@/assets/svg/fitting12.svg",
    category: "工业"
  },
  {
    name: "fitting13",
    title: "管件13",
    iconPath: "@/assets/svg/fitting13.svg",
    category: "工业"
  },

  // 杂项设备 (Miscellaneous)
  {
    name: "misc1",
    title: "杂项设备1",
    iconPath: "@/assets/svg/misc1.svg",
    category: "工业"
  },
  {
    name: "misc2",
    title: "杂项设备2",
    iconPath: "@/assets/svg/misc2.svg",
    category: "工业"
  },
  {
    name: "misc3",
    title: "杂项设备3",
    iconPath: "@/assets/svg/misc3.svg",
    category: "工业"
  },
  {
    name: "misc4",
    title: "杂项设备4",
    iconPath: "@/assets/svg/misc4.svg",
    category: "工业"
  },
  {
    name: "misc5",
    title: "杂项设备5",
    iconPath: "@/assets/svg/misc5.svg",
    category: "工业"
  },
  {
    name: "misc6",
    title: "杂项设备6",
    iconPath: "@/assets/svg/misc6.svg",
    category: "工业"
  },
  {
    name: "misc7",
    title: "杂项设备7",
    iconPath: "@/assets/svg/misc7.svg",
    category: "工业"
  },
  {
    name: "misc8",
    title: "杂项设备8",
    iconPath: "@/assets/svg/misc8.svg",
    category: "工业"
  },

  // 储罐类 (Tanks)
  {
    name: "tank1",
    title: "储罐1",
    iconPath: "@/assets/svg/tank1.svg",
    category: "工业"
  },
  {
    name: "tank2",
    title: "储罐2",
    iconPath: "@/assets/svg/tank2.svg",
    category: "工业"
  },
  {
    name: "tank3",
    title: "储罐3",
    iconPath: "@/assets/svg/tank3.svg",
    category: "工业"
  },
  {
    name: "tank4",
    title: "储罐4",
    iconPath: "@/assets/svg/tank4.svg",
    category: "工业"
  },
  {
    name: "tank5",
    title: "储罐5",
    iconPath: "@/assets/svg/tank5.svg",
    category: "工业"
  },
  {
    name: "tank6",
    title: "储罐6",
    iconPath: "@/assets/svg/tank6.svg",
    category: "工业"
  },
  {
    name: "tank7",
    title: "储罐7",
    iconPath: "@/assets/svg/tank7.svg",
    category: "工业"
  },
  {
    name: "tank8",
    title: "储罐8",
    iconPath: "@/assets/svg/tank8.svg",
    category: "工业"
  },
  {
    name: "tank9",
    title: "储罐9",
    iconPath: "@/assets/svg/tank9.svg",
    category: "工业"
  },

  // 管道类 (Piping)
  {
    name: "pipi1",
    title: "管道1",
    iconPath: "@/assets/svg/pipi1.svg",
    category: "工业"
  },
  {
    name: "pipi2",
    title: "管道2",
    iconPath: "@/assets/svg/pipi2.svg",
    category: "工业"
  },
  {
    name: "pipi3",
    title: "管道3",
    iconPath: "@/assets/svg/pipi3.svg",
    category: "工业"
  },
  {
    name: "pipi4",
    title: "管道4",
    iconPath: "@/assets/svg/pipi4.svg",
    category: "工业"
  },
  {
    name: "pipi5",
    title: "管道5",
    iconPath: "@/assets/svg/pipi5.svg",
    category: "工业"
  },
  {
    name: "pipi6",
    title: "管道6",
    iconPath: "@/assets/svg/pipi6.svg",
    category: "工业"
  },
  {
    name: "pipi7",
    title: "管道7",
    iconPath: "@/assets/svg/pipi7.svg",
    category: "工业"
  }
];

// 动画组件
export const animationComponents: ComponentIconMapping[] = [
  {
    name: "anim-eli",
    title: "容器动画",
    iconPath: "@/assets/svg/anim-eli.svg",
    category: "动画"
  },
  {
    name: "anim-piston",
    title: "扇形动画",
    iconPath: "@/assets/svg/anim-piston.svg",
    category: "动画"
  }
];

// 小部件组件
export const widgetComponents: ComponentIconMapping[] = [
  {
    name: "draggingIndicatorExample",
    title: "拖拽指示器",
    iconPath: "@/assets/svg/draggingIndicatorExample.svg",
    category: "小部件"
  },
  {
    name: "blower3",
    title: "鼓风机3",
    iconPath: "@/assets/svg/blower3.svg",
    category: "小部件"
  },
  {
    name: "blower3_flipped",
    title: "翻转鼓风机3",
    iconPath: "@/assets/svg/blower3_flipped.svg",
    category: "小部件"
  },
  {
    name: "blower4",
    title: "鼓风机4",
    iconPath: "@/assets/svg/blower4.svg",
    category: "小部件"
  },
  {
    name: "blower4_flipped",
    title: "翻转鼓风机4",
    iconPath: "@/assets/svg/blower4_flipped.svg",
    category: "小部件"
  },
  {
    name: "checkvalve_closed",
    title: "关闭止回阀",
    iconPath: "@/assets/svg/checkvalve_closed.svg",
    category: "小部件"
  },
  {
    name: "checkvalve_closed_flipped",
    title: "翻转关闭止回阀",
    iconPath: "@/assets/svg/checkvalve_closed_flipped.svg",
    category: "小部件"
  },
  {
    name: "checkvalve_open",
    title: "开启止回阀",
    iconPath: "@/assets/svg/checkvalve_open.svg",
    category: "小部件"
  },
  {
    name: "checkvalve_open_flipped",
    title: "翻转开启止回阀",
    iconPath: "@/assets/svg/checkvalve_open_flipped.svg",
    category: "小部件"
  },
  {
    name: "compressor3",
    title: "压缩机3",
    iconPath: "@/assets/svg/compressor3.svg",
    category: "小部件"
  },
  {
    name: "compressor6",
    title: "压缩机6",
    iconPath: "@/assets/svg/compressor6.svg",
    category: "小部件"
  },
  {
    name: "condensor_3d_h1",
    title: "3D冷凝器H1",
    iconPath: "@/assets/svg/condensor_3d_h1.svg",
    category: "小部件"
  },
  {
    name: "conveyor",
    title: "传送带",
    iconPath: "@/assets/svg/conveyor.svg",
    category: "小部件"
  },
  {
    name: "conveyor4",
    title: "传送带4",
    iconPath: "@/assets/svg/conveyor4.svg",
    category: "小部件"
  },
  {
    name: "conveyor_20degdecline",
    title: "20度下降传送带",
    iconPath: "@/assets/svg/conveyor_20degdecline.svg",
    category: "小部件"
  },
  {
    name: "conveyor_20degincline",
    title: "20度上升传送带",
    iconPath: "@/assets/svg/conveyor_20degincline.svg",
    category: "小部件"
  },
  {
    name: "cyclonicseparator",
    title: "旋风分离器",
    iconPath: "@/assets/svg/cyclonicseparator.svg",
    category: "小部件"
  },
  {
    name: "fanblades",
    title: "风扇叶片",
    iconPath: "@/assets/svg/fanblades.svg",
    category: "小部件"
  },
  {
    name: "flange_3d_1",
    title: "3D法兰1",
    iconPath: "@/assets/svg/flange_3d_1.svg",
    category: "小部件"
  },
  {
    name: "flowmeter",
    title: "流量计",
    iconPath: "@/assets/svg/flowmeter.svg",
    category: "小部件"
  },
  {
    name: "flowmeter1",
    title: "流量计1",
    iconPath: "@/assets/svg/flowmeter1.svg",
    category: "小部件"
  },
  {
    name: "gaugedial_3d_h1",
    title: "3D仪表盘H1",
    iconPath: "@/assets/svg/gaugedial_3d_h1.svg",
    category: "小部件"
  },
  {
    name: "generator_1",
    title: "发电机1",
    iconPath: "@/assets/svg/generator_1.svg",
    category: "小部件"
  },
  {
    name: "heatexchanger_pipetopipe",
    title: "管对管热交换器",
    iconPath: "@/assets/svg/heatexchanger_pipetopipe.svg",
    category: "小部件"
  },
  {
    name: "heatexchanger_shelltube",
    title: "壳管式热交换器",
    iconPath: "@/assets/svg/heatexchanger_shelltube.svg",
    category: "小部件"
  },
  {
    name: "heatexchanger_singlepipetopipe",
    title: "单管对管热交换器",
    iconPath: "@/assets/svg/heatexchanger_singlepipetopipe.svg",
    category: "小部件"
  },
  {
    name: "meter_3d_h1",
    title: "3D计量表H1",
    iconPath: "@/assets/svg/meter_3d_h1.svg",
    category: "小部件"
  },
  {
    name: "meter_3d_h2",
    title: "3D计量表H2",
    iconPath: "@/assets/svg/meter_3d_h2.svg",
    category: "小部件"
  },
  {
    name: "mixer1",
    title: "混合器1",
    iconPath: "@/assets/svg/mixer1.svg",
    category: "小部件"
  },
  {
    name: "motor",
    title: "电机",
    iconPath: "@/assets/svg/motor.svg",
    category: "小部件"
  },
  {
    name: "motor1",
    title: "电机1",
    iconPath: "@/assets/svg/motor1.svg",
    category: "小部件"
  },
  {
    name: "motor1_flipped",
    title: "翻转电机1",
    iconPath: "@/assets/svg/motor1_flipped.svg",
    category: "小部件"
  },
  {
    name: "motor_wshaft",
    title: "带轴电机",
    iconPath: "@/assets/svg/motor_wshaft.svg",
    category: "小部件"
  },
  {
    name: "piperedirect1",
    title: "管道转向器1",
    iconPath: "@/assets/svg/piperedirect1.svg",
    category: "小部件"
  },
  {
    name: "piperedirect1_flipped",
    title: "翻转管道转向器1",
    iconPath: "@/assets/svg/piperedirect1_flipped.svg",
    category: "小部件"
  },
  {
    name: "pipe_3d_elbow",
    title: "3D弯管",
    iconPath: "@/assets/svg/pipe_3d_elbow.svg",
    category: "小部件"
  },
  {
    name: "pipe_3d_elbow2",
    title: "3D弯管2",
    iconPath: "@/assets/svg/pipe_3d_elbow2.svg",
    category: "小部件"
  },
  {
    name: "pipe_3d_h1",
    title: "3D管道H1",
    iconPath: "@/assets/svg/pipe_3d_h1.svg",
    category: "小部件"
  },
  {
    name: "pipe_3d_h2",
    title: "3D管道H2",
    iconPath: "@/assets/svg/pipe_3d_h2.svg",
    category: "小部件"
  },
  {
    name: "pipe_3d_h3",
    title: "3D管道H3",
    iconPath: "@/assets/svg/pipe_3d_h3.svg",
    category: "小部件"
  },
  {
    name: "pipe_3d_heatexchange1",
    title: "3D热交换管道1",
    iconPath: "@/assets/svg/pipe_3d_heatexchange1.svg",
    category: "小部件"
  },
  {
    name: "pipe_3d_heatexchange2",
    title: "3D热交换管道2",
    iconPath: "@/assets/svg/pipe_3d_heatexchange2.svg",
    category: "小部件"
  },
  {
    name: "pipe_3d_heatexchange3",
    title: "3D热交换管道3",
    iconPath: "@/assets/svg/pipe_3d_heatexchange3.svg",
    category: "小部件"
  },
  {
    name: "pipe_3d_tjunction",
    title: "3D三通管",
    iconPath: "@/assets/svg/pipe_3d_tjunction.svg",
    category: "小部件"
  },
  {
    name: "pressure_regulator",
    title: "压力调节器",
    iconPath: "@/assets/svg/pressure_regulator.svg",
    category: "小部件"
  },
  {
    name: "pulsationdampener",
    title: "脉动阻尼器",
    iconPath: "@/assets/svg/pulsationdampener.svg",
    category: "小部件"
  },
  {
    name: "pump1",
    title: "泵1",
    iconPath: "@/assets/svg/pump1.svg",
    category: "小部件"
  },
  {
    name: "pump2",
    title: "泵2",
    iconPath: "@/assets/svg/pump2.svg",
    category: "小部件"
  },
  {
    name: "pump2_flipped",
    title: "翻转泵2",
    iconPath: "@/assets/svg/pump2_flipped.svg",
    category: "小部件"
  },
  {
    name: "pumphead_profile_3d",
    title: "3D泵头剖面",
    iconPath: "@/assets/svg/pumphead_profile_3d.svg",
    category: "小部件"
  },
  {
    name: "pump_3d_180",
    title: "3D 180度泵",
    iconPath: "@/assets/svg/pump_3d_180.svg",
    category: "小部件"
  },
  {
    name: "pump_3d_180_flipped",
    title: "翻转3D 180度泵",
    iconPath: "@/assets/svg/pump_3d_180_flipped.svg",
    category: "小部件"
  },
  {
    name: "pump_3d_90_1",
    title: "3D 90度泵1",
    iconPath: "@/assets/svg/pump_3d_90_1.svg",
    category: "小部件"
  },
  {
    name: "pump_3d_90_1_flipped",
    title: "翻转3D 90度泵1",
    iconPath: "@/assets/svg/pump_3d_90_1_flipped.svg",
    category: "小部件"
  },
  {
    name: "pump_3d_example",
    title: "3D泵示例",
    iconPath: "@/assets/svg/pump_3d_example.svg",
    category: "小部件"
  },
  {
    name: "pump_3d_straight1",
    title: "3D直泵1",
    iconPath: "@/assets/svg/pump_3d_straight1.svg",
    category: "小部件"
  },
  {
    name: "pump_3d_straight1_flipped",
    title: "翻转3D直泵1",
    iconPath: "@/assets/svg/pump_3d_straight1_flipped.svg",
    category: "小部件"
  },
  {
    name: "reducer_3d_1",
    title: "3D减压器1",
    iconPath: "@/assets/svg/reducer_3d_1.svg",
    category: "小部件"
  },
  {
    name: "reducer_3d_2",
    title: "3D减压器2",
    iconPath: "@/assets/svg/reducer_3d_2.svg",
    category: "小部件"
  },
  {
    name: "sensor_rtd",
    title: "RTD传感器",
    iconPath: "@/assets/svg/sensor_rtd.svg",
    category: "小部件"
  },
  {
    name: "sensor_rtd_silhouette",
    title: "RTD传感器轮廓",
    iconPath: "@/assets/svg/sensor_rtd_silhouette.svg",
    category: "小部件"
  },
  {
    name: "strainer_basket",
    title: "篮式过滤器",
    iconPath: "@/assets/svg/strainer_basket.svg",
    category: "小部件"
  },
  {
    name: "strainer_basket_r",
    title: "篮式过滤器R",
    iconPath: "@/assets/svg/strainer_basket_r.svg",
    category: "小部件"
  },
  {
    name: "strainer_wye",
    title: "Y型过滤器",
    iconPath: "@/assets/svg/strainer_wye.svg",
    category: "小部件"
  },
  {
    name: "strainer_wye_r",
    title: "Y型过滤器R",
    iconPath: "@/assets/svg/strainer_wye_r.svg",
    category: "小部件"
  },
  {
    name: "tank_3d_medium_rtop1",
    title: "3D中型储罐RTOP1",
    iconPath: "@/assets/svg/tank_3d_medium_rtop1.svg",
    category: "小部件"
  },
  {
    name: "tank_3d_v1",
    title: "3D储罐V1",
    iconPath: "@/assets/svg/tank_3d_v1.svg",
    category: "小部件"
  },
  {
    name: "tank_3d_v2",
    title: "3D储罐V2",
    iconPath: "@/assets/svg/tank_3d_v2.svg",
    category: "小部件"
  },
  {
    name: "tank_3d_v4",
    title: "3D储罐V4",
    iconPath: "@/assets/svg/tank_3d_v4.svg",
    category: "小部件"
  },
  {
    name: "tank_3d_v5",
    title: "3D储罐V5",
    iconPath: "@/assets/svg/tank_3d_v5.svg",
    category: "小部件"
  },
  {
    name: "tank_3d_v6",
    title: "3D储罐V6",
    iconPath: "@/assets/svg/tank_3d_v6.svg",
    category: "小部件"
  },
  {
    name: "tank_3d_v7",
    title: "3D储罐V7",
    iconPath: "@/assets/svg/tank_3d_v7.svg",
    category: "小部件"
  },
  {
    name: "trucking",
    title: "卡车运输",
    iconPath: "@/assets/svg/trucking.svg",
    category: "小部件"
  },
  {
    name: "turbine1",
    title: "涡轮机1",
    iconPath: "@/assets/svg/turbine1.svg",
    category: "小部件"
  },
  {
    name: "turbine2",
    title: "涡轮机2",
    iconPath: "@/assets/svg/turbine2.svg",
    category: "小部件"
  },
  {
    name: "turbinerounded",
    title: "圆形涡轮机",
    iconPath: "@/assets/svg/turbinerounded.svg",
    category: "小部件"
  },
  {
    name: "valve_1path_down",
    title: "单路向下阀",
    iconPath: "@/assets/svg/valve_1path_down.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_3way1",
    title: "3D三通阀1",
    iconPath: "@/assets/svg/valve_3d_3way1.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_3way1_nopipe",
    title: "3D三通阀1(无管道)",
    iconPath: "@/assets/svg/valve_3d_3way1_nopipe.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_4way1",
    title: "3D四通阀1",
    iconPath: "@/assets/svg/valve_3d_4way1.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_4way1_nopipe",
    title: "3D四通阀1(无管道)",
    iconPath: "@/assets/svg/valve_3d_4way1_nopipe.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_angled1",
    title: "3D角度阀1",
    iconPath: "@/assets/svg/valve_3d_angled1.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_angled1_nopipe",
    title: "3D角度阀1(无管道)",
    iconPath: "@/assets/svg/valve_3d_angled1_nopipe.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_angled2",
    title: "3D角度阀2",
    iconPath: "@/assets/svg/valve_3d_angled2.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_angled2_nopipe",
    title: "3D角度阀2(无管道)",
    iconPath: "@/assets/svg/valve_3d_angled2_nopipe.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_common1",
    title: "3D通用阀1",
    iconPath: "@/assets/svg/valve_3d_common1.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_common2",
    title: "3D通用阀2",
    iconPath: "@/assets/svg/valve_3d_common2.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_common2_nopipe",
    title: "3D通用阀2(无管道)",
    iconPath: "@/assets/svg/valve_3d_common2_nopipe.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_common3",
    title: "3D通用阀3",
    iconPath: "@/assets/svg/valve_3d_common3.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_common4",
    title: "3D通用阀4",
    iconPath: "@/assets/svg/valve_3d_common4.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_common4_nopipe",
    title: "3D通用阀4(无管道)",
    iconPath: "@/assets/svg/valve_3d_common4_nopipe.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_h1",
    title: "3D阀H1",
    iconPath: "@/assets/svg/valve_3d_h1.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_h2_closed",
    title: "3D阀H2(关闭)",
    iconPath: "@/assets/svg/valve_3d_h2_closed.svg",
    category: "小部件"
  },
  {
    name: "valve_3d_h2_open",
    title: "3D阀H2(开启)",
    iconPath: "@/assets/svg/valve_3d_h2_open.svg",
    category: "小部件"
  },
  {
    name: "valve_circle",
    title: "圆形阀",
    iconPath: "@/assets/svg/valve_circle.svg",
    category: "小部件"
  },
  {
    name: "analogIndicatorExample",
    title: "模拟指示器示例",
    iconPath: "@/assets/svg/analogIndicatorExample.svg",
    category: "小部件"
  },
  {
    name: "timeDisplayHHmmSS",
    title: "时间显示器(HH:mm:SS)",
    iconPath: "@/assets/svg/timeDisplayHHmmSS.svg",
    category: "小部件"
  },
  {
    name: "timeDisplayHHmmSSsss",
    title: "时间显示器(HH:mm:SS.sss)",
    iconPath: "@/assets/svg/timeDisplayHHmmSSsss.svg",
    category: "小部件"
  },
  {
    name: "circularTankLevel-v2",
    title: "圆形储罐液位V2",
    iconPath: "@/assets/svg/circularTankLevel-v2.svg",
    category: "小部件"
  },
  {
    name: "squareTankLevel-v2",
    title: "方形储罐液位V2",
    iconPath: "@/assets/svg/squareTankLevel-v2.svg",
    category: "小部件"
  }
];

// 资源组件
export const resourceComponents: ComponentIconMapping[] = [
  {
    name: "P1",
    title: "管道",
    iconPath: "@/assets/svg/P1.svg",
    category: "资源"
  },
  {
    name: "Fl",
    title: "圆桶",
    iconPath: "@/assets/svg/Fl.svg",
    category: "资源"
  }
];

// 合并所有组件映射
export const allComponentMappings: ComponentIconMapping[] = [
  ...regularComponents,
  ...controlComponents,
  ...shapeComponents,
  ...processEngineeringComponents,
  ...animationComponents,
  ...widgetComponents,
  ...resourceComponents
];

// 按分组获取组件
export const getComponentsByCategory = (
  category: string
): ComponentIconMapping[] => {
  return allComponentMappings.filter(comp => comp.category === category);
};

// 获取所有分组
export const getAllCategories = (): string[] => {
  return Array.from(new Set(allComponentMappings.map(comp => comp.category)));
};

// 根据名称查找组件
export const findComponentByName = (
  name: string
): ComponentIconMapping | undefined => {
  return allComponentMappings.find(comp => comp.name === name);
};
