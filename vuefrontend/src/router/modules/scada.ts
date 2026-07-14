const Layout = () => import("@/layout/index.vue");

export default {
  path: "/projectscada",
  name: "ProjectScada",
  component: Layout,
  redirect: "/project",
  meta: {
    icon: "ep:monitor",
    title: "组态管理",
    rank: 2
  },
  children: [
    {
      // 编辑器只能从项目列表打开（/scada/editor/:id，见 router/modules/remaining.ts）：
      // 原先另有一个无 :id 的 /scada 菜单入口，进得去但没有项目ID、保存必失败，已移除。
      path: "/project",
      name: "Project",
      component: () => import("@/views/project/index.vue"),
      meta: {
        title: "项目管理",
        showLink: true,
        projectKind: "scada"
      }
    }
  ]
} satisfies RouteConfigsTable;
