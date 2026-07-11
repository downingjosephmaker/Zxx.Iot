const Layout = () => import("@/layout/index.vue");

export default {
  path: "/iot",
  name: "IotManage",
  component: Layout,
  redirect: "/iot/center",
  meta: {
    icon: "ep:cpu",
    title: "物联管理",
    rank: 3
  },
  children: [
    {
      path: "/iot/center",
      name: "IotCenter",
      component: () => import("@/views/iot/center/index.vue"),
      meta: {
        title: "设备中心",
        showLink: true
      }
    },
    {
      path: "/iot/product",
      name: "IotProduct",
      component: () => import("@/views/iot/product/index.vue"),
      meta: {
        title: "产品类型",
        showLink: true
      }
    },
    {
      path: "/iot/typeparam",
      name: "IotTypeParam",
      component: () => import("@/views/iot/typeparam/index.vue"),
      meta: {
        title: "点表配置",
        showLink: true
      }
    },
    {
      path: "/iot/device",
      name: "IotDevice",
      component: () => import("@/views/iot/device/index.vue"),
      meta: {
        title: "设备管理",
        showLink: true
      }
    },
    {
      path: "/iot/monitor",
      name: "IotMonitor",
      component: () => import("@/views/iot/monitor/index.vue"),
      meta: {
        title: "实时监控",
        showLink: true
      }
    },
    {
      path: "/iot/strategy",
      name: "IotStrategy",
      component: () => import("@/views/iot/strategy/index.vue"),
      meta: {
        title: "采集推送策略",
        showLink: true
      }
    },
    {
      path: "/iot/alarm",
      name: "IotAlarm",
      component: () => import("@/views/iot/alarm/index.vue"),
      meta: {
        title: "告警中心",
        showLink: true
      }
    },
    {
      path: "/iot/alarmmask",
      name: "IotAlarmMask",
      component: () => import("@/views/iot/alarmmask/index.vue"),
      meta: {
        title: "告警屏蔽",
        showLink: true
      }
    },
    {
      path: "/iot/notify",
      name: "IotNotify",
      component: () => import("@/views/iot/notify/index.vue"),
      meta: {
        title: "通知渠道",
        showLink: true
      }
    },
    {
      path: "/iot/linkage",
      name: "IotLinkage",
      component: () => import("@/views/iot/linkage/index.vue"),
      meta: {
        title: "规则联动",
        showLink: true
      }
    },
    {
      path: "/iot/northbound",
      name: "IotNorthbound",
      component: () => import("@/views/iot/northbound/index.vue"),
      meta: {
        title: "北向转发",
        showLink: true
      }
    },
    {
      path: "/iot/script",
      name: "IotScript",
      component: () => import("@/views/iot/script/index.vue"),
      meta: {
        title: "协议脚本",
        showLink: true
      }
    },
    {
      path: "/iot/command",
      name: "IotCommand",
      component: () => import("@/views/iot/command/index.vue"),
      meta: {
        title: "产品命令",
        showLink: true
      }
    },
    {
      path: "/iot/plugin",
      name: "IotPlugin",
      component: () => import("@/views/iot/plugin/index.vue"),
      meta: {
        // 上传DLL等价远程代码执行,后端全端点仅超管可用(DenyIfNotSuperAdmin);
        // 前端roles值无登录侧实证,不设meta.roles以免误锁,非超管进入会收到后端明确拒绝
        title: "插件管理",
        showLink: true
      }
    }
  ]
} satisfies RouteConfigsTable;
