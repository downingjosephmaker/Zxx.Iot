const Layout = () => import("@/layout/index.vue");

export default {
  path: "/report",
  name: "ReportCenter",
  component: Layout,
  redirect: "/report/lowcode",
  meta: {
    icon: "ep:document",
    title: "报表中心",
    rank: 4
  },
  children: [
    {
      path: "/report/lowcode",
      name: "LowCodeReport",
      component: () => import("@/views/report/lowcode.vue"),
      meta: {
        title: "低代码报表",
        showLink: true
      }
    }
  ]
} satisfies RouteConfigsTable;
