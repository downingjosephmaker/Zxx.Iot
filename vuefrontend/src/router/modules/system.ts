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
    },
    {
      path: "/system/menu",
      name: "SystemMenu",
      component: () => import("@/views/system/menu/index.vue"),
      meta: {
        title: "菜单管理",
        showLink: true
      }
    },
    {
      path: "/system/role",
      name: "SystemRole",
      component: () => import("@/views/system/role/index.vue"),
      meta: {
        title: "角色授权",
        showLink: true
      }
    },
    {
      path: "/system/button",
      name: "SystemButton",
      component: () => import("@/views/system/button/index.vue"),
      meta: {
        title: "按钮管理",
        showLink: true
      }
    }
  ]
} satisfies RouteConfigsTable;
