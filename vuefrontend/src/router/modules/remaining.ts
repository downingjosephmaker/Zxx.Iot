import { $t } from "@/plugins/i18n";
const Layout = () => import("@/layout/index.vue");

export default [
  // ========== 系统基础路由 ==========
  {
    path: "/login",
    name: "Login",
    component: () => import("@/views/login/index.vue"),
    meta: {
      title: $t("menus.pureLogin"),
      showLink: false,
      rank: 101
    }
  },
  {
    path: "/redirect",
    component: Layout,
    meta: {
      title: $t("status.pureLoad"),
      showLink: false,
      rank: 102
    },
    children: [
      {
        path: "/redirect/:path(.*)",
        name: "Redirect",
        component: () => import("@/layout/redirect.vue")
      }
    ]
  },
  {
    path: "/empty",
    name: "Empty",
    component: () => import("@/views/empty/index.vue"),
    meta: {
      title: $t("menus.pureEmpty"),
      showLink: false,
      rank: 103
    }
  },

  // ========== 逃生舱：菜单管理的静态兜底入口 ==========
  // 侧边栏由数据库 sys_menu 驱动。万一菜单数据被改坏（路由地址写错、组件路径不存在、
  // 把「菜单管理」自己隐藏了），侧边栏就可能进不去任何页面，也就没法用界面把数据修回来。
  // 这条路由 showLink:false 不出现在侧边栏，超管随时可手敲 /rescue/menu 进去自救。
  //
  // 注意路径不能用 /system/menu：那会与数据库里 SystemMenu 菜单的动态路由撞 path，
  // 静态路由先注册会抢占匹配，导致侧边栏高亮与标签页错乱。故另开 /rescue 前缀。
  {
    path: "/rescue",
    name: "Rescue",
    component: () => import("@/layout/index.vue"),
    redirect: "/rescue/menu",
    meta: {
      title: "菜单管理",
      showLink: false,
      rank: 110
    },
    children: [
      {
        path: "/rescue/menu",
        name: "SystemMenuRescue",
        component: () => import("@/views/system/menu/index.vue"),
        meta: {
          title: "菜单管理（应急入口）",
          showLink: false
        }
      }
    ]
  },

  // ========== SCADA 组态系统隐藏路由（不显示在菜单中）==========
  // 全屏页（不套 Layout）：编辑器带项目ID打开、运行时页供发布后访问/iframe 嵌入

  {
    path: "/scada/editor/:id",
    name: "ScadaFuxaEditor",
    component: () => import("@/views/scada/index.vue"),
    meta: {
      title: "组态编辑器",
      showLink: false,
      rank: 123
    }
  },
  {
    path: "/scada/runtime/:id",
    name: "ScadaRuntime",
    component: () => import("@/views/scada/runtime.vue"),
    meta: {
      title: "组态运行时",
      showLink: false,
      rank: 124
    }
  }
] satisfies Array<RouteConfigsTable>;
