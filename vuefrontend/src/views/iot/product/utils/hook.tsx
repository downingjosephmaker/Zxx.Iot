import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import { h, ref, reactive, onMounted } from "vue";
import { ElTag } from "element-plus";
import dayjs from "dayjs";
import type {
  QueryTableParams,
  DeviceTypeItem,
  ExpandDeviceType,
  DeviceTypeFormItemProps,
  TreeSelectOption
} from "./types";
import { getListByPage, insert, update, deleteByPk } from "@/api/iot/devicetype";
import editForm from "../form.vue";

const DEFAULT_EXPAND: ExpandDeviceType = {
  OfflineMinute: 0,
  SubChannels: 0,
  SbjgType: false,
  MqttKey: ""
};

export function useDeviceType() {
  const ModuleTitle = "产品类型";
  const form = reactive({
    keyword: ""
  });
  const formRef = ref();
  const dataList = ref<DeviceTypeItem[]>([]);
  /** 全量平铺数据，供关键字过滤与上级下拉树复用 */
  const allList = ref<DeviceTypeItem[]>([]);
  const loading = ref(true);

  const columns = [
    {
      label: "类型名称",
      prop: "TypeName",
      align: "left",
      minWidth: 220
    },
    {
      label: "类型编码",
      prop: "TypeCode",
      align: "left",
      minWidth: 130
    },
    {
      label: "排序号",
      prop: "SortBorder",
      align: "center",
      width: 90
    },
    {
      label: "层级",
      prop: "TreeLevel",
      align: "center",
      width: 70
    },
    {
      label: "是否采集",
      prop: "ExpandObject.SbjgType",
      align: "center",
      width: 90,
      cellRenderer: ({ row }) =>
        h(
          ElTag,
          {
            type: row.ExpandObject?.SbjgType ? "success" : "info",
            effect: "light"
          },
          () => (row.ExpandObject?.SbjgType ? "采集" : "不采集")
        )
    },
    {
      label: "离线判断",
      prop: "ExpandObject.OfflineMinute",
      align: "center",
      width: 100,
      formatter: row =>
        row.ExpandObject?.OfflineMinute > 0
          ? `${row.ExpandObject.OfflineMinute}分钟`
          : "-"
    },
    {
      label: "启用",
      prop: "IsEnable",
      align: "center",
      width: 80,
      cellRenderer: ({ row }) =>
        h(
          ElTag,
          { type: row.IsEnable ? "success" : "info", effect: "light" },
          () => (row.IsEnable ? "启用" : "停用")
        )
    },
    {
      label: "更新时间",
      prop: "UpdateTime",
      align: "center",
      width: 160,
      formatter: row =>
        row.UpdateTime ? dayjs(row.UpdateTime).format("YYYY-MM-DD HH:mm:ss") : "-"
    },
    {
      label: "操作",
      fixed: "right",
      width: 230,
      slot: "operation"
    }
  ];

  /** 读取拓展属性，兼容服务端返回ExpandObject或仅ExpandJson两种形态 */
  function parseExpand(row: DeviceTypeItem): ExpandDeviceType {
    if (row.ExpandObject) return { ...DEFAULT_EXPAND, ...row.ExpandObject };
    try {
      return row.ExpandJson
        ? { ...DEFAULT_EXPAND, ...JSON.parse(row.ExpandJson) }
        : { ...DEFAULT_EXPAND };
    } catch {
      return { ...DEFAULT_EXPAND };
    }
  }

  /** 平铺列表按ParentId构造树，孤儿节点(父级缺失)提升为根 */
  function buildTree(list: DeviceTypeItem[]): DeviceTypeItem[] {
    const map = new Map<string, DeviceTypeItem>();
    list.forEach(t => map.set(t.TypeCode, { ...t, children: [] }));
    const roots: DeviceTypeItem[] = [];
    map.forEach(node => {
      if (node.ParentId && map.has(node.ParentId)) {
        map.get(node.ParentId)!.children!.push(node);
      } else {
        roots.push(node);
      }
    });
    const sortTree = (nodes: DeviceTypeItem[]) => {
      nodes.sort((a, b) =>
        (a.SortBorder || "").localeCompare(b.SortBorder || "")
      );
      nodes.forEach(n => {
        if (n.children!.length) sortTree(n.children!);
        else delete n.children;
      });
    };
    sortTree(roots);
    return roots;
  }

  async function onSearch() {
    loading.value = true;
    const params: QueryTableParams = {
      page: 1,
      pagesize: 10000,
      sconlist: []
    };
    const data = await getListByPage(params);
    if (data.Status) {
      const list: DeviceTypeItem[] = JSON.parse(data.Result);
      list.forEach(t => (t.ExpandObject = parseExpand(t)));
      allList.value = list;
      const kw = form.keyword.trim();
      dataList.value = kw
        ? list.filter(
            t => t.TypeName?.includes(kw) || t.TypeCode.includes(kw)
          )
        : buildTree(list);
    }
    loading.value = false;
  }

  const resetForm = formEl => {
    if (!formEl) return;
    formEl.resetFields();
    onSearch();
  };

  /** 上级类型下拉树；编辑时按FullCode前缀禁选自身及其子孙，防止成环 */
  function buildSelectTree(excludeFullCode?: string): TreeSelectOption[] {
    const toOption = (node: DeviceTypeItem): TreeSelectOption => ({
      value: node.TypeCode,
      label: `${node.TypeName}(${node.TypeCode})`,
      disabled: excludeFullCode
        ? (node.FullCode ?? "").startsWith(excludeFullCode)
        : false,
      children: node.children?.map(toOption)
    });
    return buildTree(allList.value).map(toOption);
  }

  /**
   * 打开新增/修改弹窗
   * @param title 新增|修改
   * @param row 修改时的当前行
   * @param parent 新增子类时的父级行，预填ParentId
   */
  async function openDialog(
    title = "新增",
    row?: DeviceTypeItem,
    parent?: DeviceTypeItem
  ) {
    const expand = row ? parseExpand(row) : { ...DEFAULT_EXPAND };
    const formData: DeviceTypeFormItemProps = {
      title,
      TypeCode: row?.TypeCode ?? "",
      TypeName: row?.TypeName ?? "",
      ParentId: row?.ParentId ?? parent?.TypeCode ?? "",
      SortBorder: row?.SortBorder ?? "",
      IsEnable: row?.IsEnable ?? true,
      HasChild: row?.HasChild ?? false,
      OfflineMinute: expand.OfflineMinute,
      SubChannels: expand.SubChannels,
      SbjgType: expand.SbjgType,
      MqttKey: expand.MqttKey
    };
    const typeOptions = buildSelectTree(
      title === "修改" ? row?.FullCode : undefined
    );

    addDialog({
      title: `${title}${ModuleTitle}`,
      props: {
        formInline: formData
      },
      width: "600px",
      draggable: true,
      fullscreenIcon: true,
      closeOnClickModal: false,
      contentRenderer: () =>
        h(editForm, { formInline: formData, typeOptions, ref: formRef }),
      beforeSure: (done, { options }) => {
        const FormRef = formRef.value.getRef();
        const curData = { ...options.props.formInline };
        FormRef.validate(async valid => {
          if (valid) {
            const expandObject: ExpandDeviceType = {
              OfflineMinute: Number(curData.OfflineMinute) || 0,
              SubChannels: Number(curData.SubChannels) || 0,
              SbjgType: curData.SbjgType,
              MqttKey: curData.MqttKey ?? ""
            };
            // FullCode/FullName/TreeLevel由服务端DAO重算，无需上送
            const payload = {
              TypeCode: curData.TypeCode,
              TypeName: curData.TypeName,
              ParentId: curData.ParentId ?? "",
              SortBorder: curData.SortBorder ?? "",
              HasChild: curData.HasChild ?? false,
              IsEnable: curData.IsEnable,
              ExpandObject: expandObject,
              ExpandJson: JSON.stringify(expandObject)
            };
            const data =
              title === "新增" ? await insert(payload) : await update(payload);
            if (data.Status) {
              message(`${title}${ModuleTitle}成功`, { type: "success" });
              done();
              onSearch();
            } else {
              message(data.Message, { type: "error" });
            }
          } else {
            message("表单验证失败，请检查输入", { type: "warning" });
          }
        });
      }
    });
  }

  async function handleDelete(row: DeviceTypeItem) {
    const data = await deleteByPk(row.TypeCode);
    if (data.Status) {
      message("删除成功", { type: "success" });
      onSearch();
    } else {
      message(data.Message, { type: "error" });
    }
  }

  onMounted(() => {
    onSearch();
  });

  return {
    form,
    loading,
    columns,
    dataList,
    onSearch,
    resetForm,
    openDialog,
    handleDelete
  };
}
