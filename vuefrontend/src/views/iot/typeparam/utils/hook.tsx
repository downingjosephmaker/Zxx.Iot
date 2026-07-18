import { message } from "@/utils/message";
import { addDialog, closeDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, onMounted } from "vue";
import { ElMessage, ElTag } from "element-plus";
import dayjs from "dayjs";
import type {
  QueryTableParams,
  DeviceTypeParamItem,
  StatusValueItem,
  DeviceTypeParamFormItemProps
} from "./types";
import { getListByPage, saveBatch, deleteByPk } from "@/api/iot/typeparam";
import editForm from "../form.vue";
import jsonImport from "../json-import.vue";

/** Modbus采集功能码显示映射(0=不采集) */
const FUNC_CODE_LABELS: Record<number, string> = {
  0: "不采集",
  1: "FC01线圈",
  2: "FC02离散",
  3: "FC03保持",
  4: "FC04输入"
};

export function useDeviceTypeParam(tableRef: Ref) {
  const ModuleTitle = "点表参数";
  const form = reactive({
    typecode: "",
    keyword: ""
  });
  const formRef = ref();
  const dataList = ref<DeviceTypeParamItem[]>([]);
  const loading = ref(true);
  const selectedNum = ref(0);
  const selectedRows = ref<DeviceTypeParamItem[]>([]);
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
      label: "序号",
      type: "index",
      width: 60,
      align: "center"
    },
    {
      label: "产品编码",
      prop: "DeviceTypeCode",
      align: "left",
      minWidth: 110
    },
    {
      label: "路数",
      prop: "SubChannel",
      align: "center",
      width: 70
    },
    {
      label: "参数编码",
      prop: "ParamCode",
      align: "left",
      minWidth: 110
    },
    {
      label: "参数名称",
      prop: "ParamName",
      align: "left",
      minWidth: 120
    },
    {
      label: "分类",
      prop: "ParamTypeName",
      align: "center",
      width: 90
    },
    {
      label: "值类型",
      prop: "ValueType",
      align: "center",
      width: 80,
      cellRenderer: ({ row }) =>
        h(
          ElTag,
          {
            type: row.ValueType === "状态" ? "warning" : "primary",
            effect: "light"
          },
          () => row.ValueType || "数值"
        )
    },
    {
      label: "单位",
      prop: "ValueUnit",
      align: "center",
      width: 70
    },
    {
      label: "功能码",
      prop: "CollectFuncCode",
      align: "center",
      width: 95,
      formatter: row =>
        FUNC_CODE_LABELS[row.CollectFuncCode ?? 0] ?? `FC${row.CollectFuncCode}`
    },
    {
      label: "地址",
      prop: "ParamAddr",
      align: "center",
      width: 75
    },
    {
      label: "数据类型",
      prop: "CollectDataType",
      align: "center",
      width: 90,
      formatter: row =>
        row.CollectFuncCode > 0 ? row.CollectDataType || "uint16" : "-"
    },
    {
      label: "显示",
      prop: "IsShow",
      align: "center",
      width: 70,
      cellRenderer: ({ row }) =>
        h(
          ElTag,
          { type: row.IsShow ? "success" : "info", effect: "light" },
          () => (row.IsShow ? "是" : "否")
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
      width: 160,
      slot: "operation"
    }
  ];

  /** 读取状态值集合，兼容结构化ExpandStatusValues与仅StatusValues串两形态 */
  function parseStatusValues(row?: DeviceTypeParamItem): StatusValueItem[] {
    if (row?.ExpandStatusValues?.length) return [...row.ExpandStatusValues];
    try {
      return row?.StatusValues ? JSON.parse(row.StatusValues) : [];
    } catch {
      return [];
    }
  }

  async function onSearch() {
    loading.value = true;
    const params: QueryTableParams = {
      page: pagination.currentPage,
      pagesize: pagination.pageSize,
      sconlist: []
    };
    if (form.typecode !== "") {
      params.sconlist.push({
        ParamName: "DeviceTypeCode",
        ParamType: "like",
        ParamValue: form.typecode
      });
    }
    if (form.keyword !== "") {
      params.sconlist.push({
        ParamName: "ParamName",
        ParamType: "like",
        ParamValue: form.keyword
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

  function handleSelectionChange(val: DeviceTypeParamItem[]) {
    selectedNum.value = val.length;
    selectedRows.value = val;
    tableRef.value.setAdaptive();
  }

  function onSelectionCancel() {
    selectedNum.value = 0;
    selectedRows.value = [];
    tableRef.value.getTableRef().clearSelection();
  }

  async function openDialog(title = "新增", row?: DeviceTypeParamItem) {
    const formData: DeviceTypeParamFormItemProps = {
      title,
      SnowId: row?.SnowId ?? 0,
      DeviceTypeCode: row?.DeviceTypeCode ?? form.typecode ?? "",
      SubChannel: row?.SubChannel ?? "总路",
      ParamCode: row?.ParamCode ?? "",
      ParamName: row?.ParamName ?? "",
      ParamTypeName: row?.ParamTypeName ?? "",
      ParamAddr: row?.ParamAddr ?? 0,
      ParamFormula: row?.ParamFormula ?? "",
      ValueType: row?.ValueType || "数值",
      ExpandStatusValues: parseStatusValues(row),
      ValueUnit: row?.ValueUnit ?? "",
      DecimalDigit: row?.DecimalDigit ?? 2,
      ParamMaxValue: row?.ParamMaxValue ?? 0,
      ParamMinValue: row?.ParamMinValue ?? 0,
      ParamChangeValue: row?.ParamChangeValue ?? 0,
      RangeFilterEnable: row?.RangeFilterEnable ?? false,
      AmplitudeFilterEnable: row?.AmplitudeFilterEnable ?? false,
      MaxAmplitudePercent: row?.MaxAmplitudePercent ?? 0,
      ContinuousFilterEnable: row?.ContinuousFilterEnable ?? false,
      MaxContinuousCount: row?.MaxContinuousCount ?? 3,
      IsShow: row?.IsShow ?? true,
      IsMainShow: row?.IsMainShow ?? false,
      IsSet: row?.IsSet ?? false,
      IsPeak: row?.IsPeak ?? false,
      IsReport: row?.IsReport ?? false,
      IsCustomAlarm: row?.IsCustomAlarm ?? false,
      CollectFuncCode: row?.CollectFuncCode ?? 0,
      CollectDataType: row?.CollectDataType ?? "",
      CollectByteOrder: row?.CollectByteOrder ?? "",
      CollectBitOffset: row?.CollectBitOffset ?? -1,
      CollectRegLength: row?.CollectRegLength ?? 0,
      CollectWritable: row?.CollectWritable ?? false,
      CollectNodeId: row?.CollectNodeId ?? "",
      IsAlarmSource: row?.IsAlarmSource ?? false,
      AlarmConfigId: row?.AlarmConfigId ?? 0
    };

    addDialog({
      title: `${title}${ModuleTitle}`,
      props: {
        formInline: formData
      },
      width: "860px",
      draggable: true,
      fullscreenIcon: true,
      closeOnClickModal: false,
      contentRenderer: () => h(editForm, { formInline: formData, ref: formRef }),
      beforeSure: (done, { options }) => {
        const FormRef = formRef.value.getRef();
        const curData = { ...options.props.formInline };
        FormRef.validate(async valid => {
          if (valid) {
            delete curData.title;
            // 数值型清空状态值集合(空列表使服务端将StatusValues写空)
            if (curData.ValueType !== "状态") curData.ExpandStatusValues = [];
            const data = await saveBatch([curData]);
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

  const importRef = ref();

  /** 打开点表JSON导入弹窗（组态ZtTypeJson格式,产品编码预填搜索栏值） */
  function openImportDialog() {
    addDialog({
      title: "JSON导入点表",
      width: "560px",
      draggable: true,
      closeOnClickModal: false,
      contentRenderer: () =>
        h(jsonImport, { ref: importRef, typecode: form.typecode }),
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
          label: "导入",
          type: "primary",
          text: true,
          bg: true,
          btnClick: async ({ dialog: { options, index } }) => {
            const ok = await importRef.value?.onImport();
            if (ok) {
              closeDialog(options, index);
              onSearch();
            }
          }
        }
      ]
    });
  }

  async function handleDelete(row: DeviceTypeParamItem) {
    const data = await deleteByPk(row.SnowId.toString());
    if (data.Status) {
      message("删除成功", { type: "success" });
      onSearch();
    } else {
      message(data.Message, { type: "error" });
    }
  }

  async function onbatchDel() {
    if (selectedRows.value.length === 0) {
      ElMessage.warning("请先选择要删除的参数");
      return;
    }
    const results = await Promise.all(
      selectedRows.value.map(row => deleteByPk(row.SnowId.toString()))
    );
    const successCount = results.filter(result => result.Status).length;
    const failCount = results.length - successCount;
    message(
      `成功删除 ${successCount} 条参数${failCount > 0 ? `，失败 ${failCount} 条` : ""}`,
      { type: failCount === 0 ? "success" : "warning" }
    );
    onSearch();
    onSelectionCancel();
  }

  onMounted(() => {
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
    openImportDialog,
    handleDelete,
    onbatchDel,
    onSelectionCancel
  };
}
