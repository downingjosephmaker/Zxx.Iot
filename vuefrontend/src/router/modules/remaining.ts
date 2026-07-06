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
