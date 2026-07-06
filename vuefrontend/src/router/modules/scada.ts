const Layout = () => import("@/layout/index.vue");

export default {
  path: "/projectscada",
  name: "ProjectScada",
  component: Layout,
  redirect: "/projectscada",
  meta: {
    icon: "ep:monitor",
    title: "组态管理",
    rank: 2
  },
  children: [
    {
      path: "/project",
      name: "Project",
      component: () => import("@/views/project/index.vue"),
      meta: {
        title: "项目管理",
        showLink: true
      }
    },
    {
      path: "/scada",
      name: "Scada",
      component: () => import("@/views/scada/index.vue"),
      meta: {
        title: "组态编辑器",
        showLink: true
      }
    }
  ]
} satisfies RouteConfigsTable;
