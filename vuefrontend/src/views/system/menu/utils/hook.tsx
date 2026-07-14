import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import { IconifyIconOffline } from "@/components/ReIcon";
import { type Ref, h, ref, onMounted } from "vue";
import type { MenuItem, MenuFormItemProps } from "./types";
import {
  getMenuTreeList,
  insertMenu,
  updateMenu,
  deleteMenu
} from "@/api/system";
import editForm from "../form.vue";
import MenuBtnBind from "../MenuBtnBind.vue";

/** 扁平菜单列表按 ParentId 组装成树（根 ParentId==="0"） */
function buildTree(flat: MenuItem[]): MenuItem[] {
  const map = new Map<string, MenuItem>();
  flat.forEach(m => map.set(m.MenuId, { ...m, children: [] }));
  const roots: MenuItem[] = [];
  map.forEach(node => {
    const parent = map.get(node.ParentId);
    if (parent && node.ParentId !== "0") parent.children!.push(node);
    else roots.push(node);
  });
  const sortRec = (nodes: MenuItem[]) => {
    nodes.sort((a, b) => Number(a.SortBorder || 0) - Number(b.SortBorder || 0));
    nodes.forEach(n => n.children && n.children.length && sortRec(n.children));
    nodes.forEach(n => {
      if (n.children && n.children.length === 0) delete n.children;
    });
  };
  sortRec(roots);
  return roots;
}

export function useSysMenu() {
  const ModuleTitle = "菜单";
  const formRef = ref();
  const bindRef = ref();
  const dataList = ref<MenuItem[]>([]);
  const flatList = ref<MenuItem[]>([]);
  const loading = ref(true);

  const columns: TableColumnList = [
    { label: "菜单名称", prop: "MenuName", align: "left", minWidth: 200 },
    { label: "编码(路由name)", prop: "MenuCode", align: "left", minWidth: 150 },
    { label: "路由地址", prop: "MenuUrl", align: "left", minWidth: 160 },
    {
      label: "图标",
      prop: "MenuIcon",
      align: "center",
      width: 150,
      cellRenderer: ({ row }) =>
        row.MenuIcon
          ? h(
              "div",
              {
                style:
                  "display:flex;align-items:center;justify-content:center;gap:6px"
              },
              [
                h(IconifyIconOffline, {
                  icon: row.MenuIcon,
                  style: "font-size:18px"
                }),
                h(
                  "span",
                  {
                    style: "font-size:12px;color:var(--el-text-color-secondary)"
                  },
                  row.MenuIcon
                )
              ]
            )
          : h("span", { style: "color:var(--el-text-color-placeholder)" }, "-")
    },
    {
      label: "显示",
      prop: "IsShowLink",
      align: "center",
      width: 80,
      cellRenderer: ({ row }) =>
        h(
          "span",
          { style: { color: row.IsShowLink === 0 ? "#f56c6c" : "#67c23a" } },
          row.IsShowLink === 0 ? "隐藏" : "显示"
        )
    },
    {
      label: "排序",
      prop: "SortBorder",
      align: "center",
      width: 70,
      formatter: row => row.SortBorder || "-"
    },
    { label: "操作", fixed: "right", width: 220, slot: "operation" }
  ];

  async function onSearch() {
    loading.value = true;
    // MenuParam.MenuCode/MenuName 在后端非空(Nullable下被当必填),须带空串否则 400
    const data = await getMenuTreeList({
      page: 1,
      pagesize: 999,
      MenuCode: "",
      MenuName: ""
    });
    if (data.Status) {
      flatList.value = JSON.parse(data.Result);
      dataList.value = buildTree(flatList.value);
    } else if (data.Message) {
      message(data.Message, { type: "warning" });
    }
    loading.value = false;
  }

  function openDialog(title = "新增", row?: MenuItem) {
    const formData: MenuFormItemProps = {
      title,
      MenuId: row?.MenuId ?? "",
      MenuCode: row?.MenuCode ?? "",
      MenuName: row?.MenuName ?? "",
      ParentId: row?.ParentId ?? "0",
      MenuUrl: row?.MenuUrl ?? "",
      Component: row?.Component ?? "",
      MenuIcon: row?.MenuIcon ?? "",
      MetaJson: row?.MetaJson ?? "",
      IsShowLink: row?.IsShowLink ?? 1,
      SortBorder: row?.SortBorder ?? "",
      TreeLevel: row?.TreeLevel ?? 1,
      FullName: row?.FullName ?? "",
      FullCode: row?.FullCode ?? "",
      HasChild: row?.HasChild ?? false
    };
    addDialog({
      title: `${title}${ModuleTitle}`,
      props: { formInline: formData, menuTree: dataList.value },
      width: "560px",
      draggable: true,
      closeOnClickModal: false,
      contentRenderer: () =>
        h(editForm, {
          formInline: formData,
          menuTree: dataList.value,
          ref: formRef
        }),
      beforeSure: (done, { options }) => {
        const FormRef = formRef.value.getRef();
        const curData = { ...options.props.formInline };
        FormRef.validate(async valid => {
          if (!valid) return;
          delete curData.title;
          const isEdit = !!row;
          const data = isEdit
            ? await updateMenu(curData)
            : await insertMenu(curData);
          if (data.Status) {
            message(`${title}${ModuleTitle}成功`, { type: "success" });
            done();
            onSearch();
          } else {
            message(data.Message, { type: "error" });
          }
        });
      }
    });
  }

  function openBind(row: MenuItem) {
    addDialog({
      title: `绑定按钮 - ${row.MenuName}`,
      width: "560px",
      draggable: true,
      closeOnClickModal: false,
      contentRenderer: () =>
        h(MenuBtnBind, {
          menuId: row.MenuId,
          menuName: row.MenuName,
          ref: bindRef
        }),
      beforeSure: async done => {
        const ok = await bindRef.value.submit();
        if (ok) done();
      }
    });
  }

  async function handleDelete(row: MenuItem) {
    const data = await deleteMenu(row.MenuId);
    if (data.Status) {
      message("删除成功", { type: "success" });
      onSearch();
    } else {
      message(data.Message, { type: "error" });
    }
  }

  onMounted(onSearch);

  return {
    loading,
    columns,
    dataList,
    onSearch,
    openDialog,
    openBind,
    handleDelete
  };
}
