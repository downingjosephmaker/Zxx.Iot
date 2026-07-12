const Layout = () => import("@/layout/index.vue");

export default {
  path: "/system",
  name: "SystemManage",
  component: Layout,
  redirect: "/system/unit",
  meta: {
    icon: "ri:settings-3-line",
    title: "系统管理",
    rank: 9
  },
  children: [
    {
      path: "/system/unit",
      name: "SystemUnit",
      component: () => import("@/views/system/unit/index.vue"),
      meta: {
        title: "租户管理",
        showLink: true
      }
    }
  ]
} satisfies RouteConfigsTable;
