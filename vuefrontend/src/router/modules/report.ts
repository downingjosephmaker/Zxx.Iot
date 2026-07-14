const Layout = () => import("@/layout/index.vue");

export default {
  path: "/report",
  name: "ReportCenter",
  component: Layout,
  redirect: "/report/project",
  meta: {
    icon: "ep:document",
    title: "报表中心",
    rank: 4
  },
  children: [
    {
      // 与组态项目共用 views/project 页面与 scada 引擎，projectKind 决定读写 DashProject 一套接口
      path: "/report/project",
      name: "ReportProject",
      component: () => import("@/views/project/index.vue"),
      meta: {
        title: "报表项目",
        showLink: true,
        projectKind: "dash"
      }
    }
  ]
} satisfies RouteConfigsTable;
