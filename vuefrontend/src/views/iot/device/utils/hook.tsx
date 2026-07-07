import { message } from "@/utils/message";
import { addDialog, closeDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, onMounted } from "vue";
import { ElMessage, ElTag } from "element-plus";
import type {
  QueryTableParams,
  DeviceInfoItem,
  ExpandDeviceInfo,
  DeviceFormItemProps,
  TreeSelectOption
} from "./types";
import { getListByPage, insert, update, deleteByPk } from "@/api/iot/device";
import {
  getListByPage as getTypeList,
  type DeviceTypeItem
} from "@/api/iot/devicetype";
import editForm from "../form.vue";
import commandSend from "../command-send.vue";

/** 设备状态显示映射(2在线/1掉电/0离线) */
const STATE_TAGS: Record<number, { type: "success" | "warning" | "info"; label: string }> = {
  2: { type: "success", label: "在线" },
  1: { type: "warning", label: "掉电" },
  0: { type: "info", label: "离线" }
};

const DEFAULT_EXPAND: ExpandDeviceInfo = {
  DeviceType: 0,
  EnergyType: "其他",
  LineNum: "",
  DeviceIMEI: "",
  DeviceSim: "",
  VideoIds: "",
  StrategySendStatus: "未下发",
  StrategySendTime: "",
  CurrentTransformer: 1,
  VoltageTransformer: 1
};

export function useDeviceInfo(tableRef: Ref) {
  const ModuleTitle = "设备";
  const form = reactive({
    keyword: "",
    typecode: ""
  });
  const formRef = ref();
  const dataList = ref<DeviceInfoItem[]>([]);
  const loading = ref(true);
  const selectedNum = ref(0);
  const selectedRows = ref<DeviceInfoItem[]>([]);
  /** 产品类型平铺表，供表单树下拉与FullCode回填 */
  const typeList = ref<DeviceTypeItem[]>([]);
  const pagination = reactive<PaginationProps>({
    total: 0,
    pageSize: 10,
    pageSizes: [10, 20, 50, 100],
    currentPage: 1,
    background: true
  });

  const columns = [
    {
      type: "selection",
      width: 40,
      align: "center"
    },
    {
      label: "ID",
      prop: "DeviceId",
      align: "center",
      width: 70
    },
    {
      label: "设备名称",
      prop: "DeviceName",
      align: "left",
      minWidth: 140
    },
    {
      label: "产品类型",
      prop: "DeviceTypeName",
      align: "left",
      minWidth: 110,
      formatter: row => row.DeviceTypeName || row.DeviceTypeCode || "-"
    },
    {
      label: "设备编号",
      prop: "DeviceGuid",
      align: "left",
      minWidth: 120
    },
    {
      label: "网关编号",
      prop: "DeviceGateway",
      align: "left",
      minWidth: 100,
      formatter: row => row.DeviceGateway || "-"
    },
    {
      label: "通讯地址",
      prop: "DeviceIp",
      align: "left",
      minWidth: 130,
      formatter: row =>
        row.DeviceIp
          ? `${row.DeviceIp}${row.DevicePort ? ":" + row.DevicePort : ""}`
          : row.DeviceCom
            ? `COM${row.DeviceCom}`
            : "-"
    },
    {
      label: "协议地址",
      prop: "DeviceAdr",
      align: "center",
      width: 85
    },
    {
      label: "采集",
      prop: "IsCollection",
      align: "center",
      width: 70,
      cellRenderer: ({ row }) =>
        h(
          ElTag,
          {
            type: row.IsCollection === 1 ? "success" : "info",
            effect: "light"
          },
          () => (row.IsCollection === 1 ? "采集" : "停采")
        )
    },
    {
      label: "状态",
      prop: "DeviceState",
      align: "center",
      width: 90,
      cellRenderer: ({ row }) => {
        const state = STATE_TAGS[row.DeviceState ?? 0] ?? STATE_TAGS[0];
        const tags = [
          h(ElTag, { type: state.type, effect: "light" }, () => state.label)
        ];
        if (row.DeviceAlarm === 1) {
          tags.push(
            h(
              ElTag,
              { type: "danger", effect: "light", class: "ml-1" },
              () => "告警"
            )
          );
        }
        return h("span", tags);
      }
    },
    {
      label: "最后在线",
      prop: "LastOnlineTime",
      align: "center",
      width: 160,
      formatter: row => row.LastOnlineTime || "-"
    },
    {
      label: "操作",
      fixed: "right",
      width: 220,
      slot: "operation"
    }
  ];

  function parseExpand(row?: DeviceInfoItem): ExpandDeviceInfo {
    if (row?.ExpandObject) return { ...DEFAULT_EXPAND, ...row.ExpandObject };
    try {
      return row?.ExpandJson
        ? { ...DEFAULT_EXPAND, ...JSON.parse(row.ExpandJson) }
        : { ...DEFAULT_EXPAND };
    } catch {
      return { ...DEFAULT_EXPAND };
    }
  }

  async function loadTypeList() {
    const data = await getTypeList({ page: 1, pagesize: 10000, sconlist: [] });
    if (data.Status) typeList.value = JSON.parse(data.Result);
  }

  /** 产品类型树下拉选项 */
  function buildTypeOptions(): TreeSelectOption[] {
    const map = new Map<string, TreeSelectOption & { parent?: string }>();
    typeList.value.forEach(t =>
      map.set(t.TypeCode, {
        value: t.TypeCode,
        label: `${t.TypeName}(${t.TypeCode})`,
        parent: t.ParentId,
        children: []
      })
    );
    const roots: TreeSelectOption[] = [];
    map.forEach(node => {
      if (node.parent && map.has(node.parent)) {
        map.get(node.parent)!.children!.push(node);
      } else {
        roots.push(node);
      }
    });
    const prune = (nodes: TreeSelectOption[]) => {
      nodes.forEach(n => {
        delete (n as any).parent;
        if (n.children!.length) prune(n.children!);
        else delete n.children;
      });
    };
    prune(roots);
    return roots;
  }

  async function onSearch() {
    loading.value = true;
    const params: QueryTableParams = {
      page: pagination.currentPage,
      pagesize: pagination.pageSize,
      sconlist: []
    };
    if (form.keyword !== "") {
      params.sconlist.push({
        ParamName: "DeviceName",
        ParamType: "like",
        ParamValue: form.keyword
      });
    }
    if (form.typecode !== "") {
      params.sconlist.push({
        ParamName: "DeviceTypeCode",
        ParamType: "like",
        ParamValue: form.typecode
      });
    }
    const data = await getListByPage(params);
    if (data.Status) {
      dataList.value = JSON.parse(data.Result);
      pagination.total = data.Total;
    }
    loading.value = false;
  }

  const resetForm = formEl => {
    if (!formEl) return;
    formEl.resetFields();
    onSearch();
  };

  function handleSizeChange(val: number) {
    if (pagination.pageSize !== val) {
      pagination.pageSize = val;
      onSearch();
    }
  }

  function handleCurrentChange(val: number) {
    if (pagination.currentPage !== val) {
      pagination.currentPage = val;
      onSearch();
    }
  }

  function handleSelectionChange(val: DeviceInfoItem[]) {
    selectedNum.value = val.length;
    selectedRows.value = val;
    tableRef.value.setAdaptive();
  }

  function onSelectionCancel() {
    selectedNum.value = 0;
    selectedRows.value = [];
    tableRef.value.getTableRef().clearSelection();
  }

  async function openDialog(title = "新增", row?: DeviceInfoItem) {
    const expand = parseExpand(row);
    const formData: DeviceFormItemProps = {
      title,
      DeviceId: row?.DeviceId ?? 0,
      DeviceName: row?.DeviceName ?? "",
      DeviceTypeCode: row?.DeviceTypeCode ?? "",
      DeviceGuid: row?.DeviceGuid ?? "",
      DeviceGateway: row?.DeviceGateway ?? "",
      ParentId: row?.ParentId ?? 0,
      BuildId: row?.BuildId ?? 0,
      DeptId: row?.DeptId ?? 0,
      SortBorder: row?.SortBorder ?? "",
      DeviceIp: row?.DeviceIp ?? "",
      DevicePort: row?.DevicePort ?? 0,
      DeviceCom: row?.DeviceCom ?? 0,
      DeviceAdr: row?.DeviceAdr ?? 0,
      IsCollection: row?.IsCollection ?? 1,
      IsVirtual: row?.IsVirtual ?? 0,
      EnergyType: expand.EnergyType ?? "其他",
      LineNum: expand.LineNum ?? "",
      DeviceIMEI: expand.DeviceIMEI ?? "",
      DeviceSim: expand.DeviceSim ?? "",
      CurrentTransformer: expand.CurrentTransformer ?? 1,
      VoltageTransformer: expand.VoltageTransformer ?? 1,
      // Update整行写回，运行时字段与未编辑字段原样透传防清零
      passthrough: {
        DeviceState: row?.DeviceState ?? 0,
        DeviceAlarm: row?.DeviceAlarm ?? 0,
        DeviceSwitch: row?.DeviceSwitch ?? 0,
        LastOnlineTime: row?.LastOnlineTime ?? "",
        IconType: row?.IconType ?? "",
        HasChild: row?.HasChild ?? false,
        IsVirtual: row?.IsVirtual ?? 0,
        UnitId: row?.UnitId ?? 0,
        CreateId: row?.CreateId ?? 0,
        CreateTime: row?.CreateTime ?? "",
        CreateName: row?.CreateName ?? "",
        expandRest: expand
      }
    };
    const typeOptions = buildTypeOptions();

    addDialog({
      title: `${title}${ModuleTitle}`,
      props: {
        formInline: formData
      },
      width: "760px",
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
            const pass = curData.passthrough ?? {};
            const expandObject: ExpandDeviceInfo = {
              ...(pass.expandRest as ExpandDeviceInfo),
              EnergyType: curData.EnergyType,
              LineNum: curData.LineNum ?? "",
              DeviceIMEI: curData.DeviceIMEI ?? "",
              DeviceSim: curData.DeviceSim ?? "",
              CurrentTransformer: Number(curData.CurrentTransformer) || 1,
              VoltageTransformer: Number(curData.VoltageTransformer) || 1
            };
            // 所选产品类型的FullCode回填DeviceTypeFullCode
            const devtype = typeList.value.find(
              t => t.TypeCode === curData.DeviceTypeCode
            );
            const payload = {
              DeviceId: curData.DeviceId,
              DeviceName: curData.DeviceName,
              DeviceTypeCode: curData.DeviceTypeCode,
              DeviceTypeFullCode: devtype?.FullCode ?? "",
              DeviceGuid: curData.DeviceGuid ?? "",
              DeviceGateway: curData.DeviceGateway ?? "",
              ParentId: Number(curData.ParentId) || 0,
              BuildId: Number(curData.BuildId) || 0,
              DeptId: Number(curData.DeptId) || 0,
              SortBorder: curData.SortBorder ?? "",
              DeviceIp: curData.DeviceIp ?? "",
              DevicePort: Number(curData.DevicePort) || 0,
              DeviceCom: Number(curData.DeviceCom) || 0,
              DeviceAdr: Number(curData.DeviceAdr) || 0,
              IsCollection: curData.IsCollection ?? 1,
              IsVirtual: curData.IsVirtual ?? 0,
              DeviceState: pass.DeviceState,
              DeviceAlarm: pass.DeviceAlarm,
              DeviceSwitch: pass.DeviceSwitch,
              LastOnlineTime: pass.LastOnlineTime,
              IconType: pass.IconType,
              HasChild: pass.HasChild,
              UnitId: pass.UnitId,
              CreateId: pass.CreateId,
              CreateTime: pass.CreateTime,
              CreateName: pass.CreateName,
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

  const commandRef = ref();

  /** 打开指令下发弹窗（消费产品命令白名单 + ParamSchema 动态表单） */
  function openCommandDialog(row: DeviceInfoItem) {
    if (!row.DeviceTypeCode) {
      message("该设备未设置产品类型，无法下发命令", { type: "warning" });
      return;
    }
    addDialog({
      title: `指令下发 - ${row.DeviceName}`,
      width: "620px",
      draggable: true,
      closeOnClickModal: false,
      contentRenderer: () =>
        h(commandSend, {
          ref: commandRef,
          deviceId: row.DeviceId,
          deviceName: row.DeviceName ?? "",
          deviceTypeCode: row.DeviceTypeCode ?? ""
        }),
      footerButtons: [
        {
          label: "关闭",
          text: true,
          bg: true,
          btnClick: ({ dialog: { options, index } }) => {
            closeDialog(options, index);
          }
        },
        {
          label: "下发",
          type: "primary",
          text: true,
          bg: true,
          btnClick: () => {
            commandRef.value?.onSend();
          }
        }
      ]
    });
  }

  async function handleDelete(row: DeviceInfoItem) {
    const data = await deleteByPk(row.DeviceId);
    if (data.Status) {
      message("删除成功", { type: "success" });
      onSearch();
    } else {
      message(data.Message, { type: "error" });
    }
  }

  async function onbatchDel() {
    if (selectedRows.value.length === 0) {
      ElMessage.warning("请先选择要删除的设备");
      return;
    }
    const results = await Promise.all(
      selectedRows.value.map(row => deleteByPk(row.DeviceId))
    );
    const successCount = results.filter(result => result.Status).length;
    const failCount = results.length - successCount;
    message(
      `成功删除 ${successCount} 台设备${failCount > 0 ? `，失败 ${failCount} 台` : ""}`,
      { type: failCount === 0 ? "success" : "warning" }
    );
    onSearch();
    onSelectionCancel();
  }

  onMounted(() => {
    loadTypeList();
    onSearch();
  });

  return {
    form,
    loading,
    columns,
    dataList,
    selectedNum,
    pagination,
    handleSizeChange,
    handleCurrentChange,
    handleSelectionChange,
    onSearch,
    resetForm,
    openDialog,
    openCommandDialog,
    handleDelete,
    onbatchDel,
    onSelectionCancel
  };
}
