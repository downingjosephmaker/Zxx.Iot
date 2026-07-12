import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, onMounted } from "vue";
import { ElMessage, ElTag } from "element-plus";
import dayjs from "dayjs";
import type {
  QueryTableParams,
  AlarmMaskItem,
  AlarmMaskFormItemProps
} from "./types";
import { getListByPage, saveBatch, deleteByPk } from "@/api/iot/alarmmask";
import editForm from "../form.vue";

const scopeMap = {
  1: "全局",
  2: "租户",
  3: "建筑",
  4: "设备类型",
  5: "单设备",
  6: "告警等级"
} as const;

const modeMap = {
  1: { text: "永久", type: "danger" },
  2: { text: "一次性", type: "warning" },
  3: { text: "周期窗", type: "primary" }
} as const;

const actionMap = {
  1: { text: "完全屏蔽", type: "danger" },
  2: { text: "静默", type: "warning" },
  3: { text: "降级", type: "info" }
} as const;

export function useAlarmMask(tableRef: Ref) {
  const ModuleTitle = "告警屏蔽";
  const form = reactive({
    keyword: "",
    scopetype: ""
  });
  const formRef = ref();
  const dataList = ref<AlarmMaskItem[]>([]);
  const loading = ref(true);
  const selectedNum = ref(0);
  const selectedRows = ref<AlarmMaskItem[]>([]);
  const pagination = reactive<PaginationProps>({
    total: 0,
    pageSize: 10,
    pageSizes: [10, 20, 50, 100],
    currentPage: 1,
    background: true
  });

  const columns = [
    {
      label: "序号",
      type: "index",
      width: 70,
      align: "center"
    },
    {
      label: "屏蔽对象",
      prop: "MaskScopeType",
      align: "left",
      minWidth: 140,
      formatter: row => {
        const scope = scopeMap[row.MaskScopeType] ?? "未知";
        return row.MaskScopeType === 1 ? scope : `${scope}：${row.ScopeId}`;
      }
    },
    {
      label: "模式",
      prop: "MaskMode",
      align: "center",
      width: 90,
      cellRenderer: ({ row }) => {
        const item = modeMap[row.MaskMode] ?? { text: "未知", type: "warning" };
        return h(ElTag, { type: item.type, effect: "light" }, () => item.text);
      }
    },
    {
      label: "动作",
      prop: "MaskAction",
      align: "center",
      width: 100,
      cellRenderer: ({ row }) => {
        const item = actionMap[row.MaskAction] ?? {
          text: "未知",
          type: "warning"
        };
        return h(ElTag, { type: item.type, effect: "light" }, () => item.text);
      }
    },
    {
      label: "屏蔽原因",
      prop: "Reason",
      align: "left",
      minWidth: 160,
      showOverflowTooltip: true
    },
    {
      label: "自动失效",
      prop: "ExpireAt",
      align: "center",
      width: 160,
      formatter: row => row.ExpireAt || "不失效"
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
      width: 160,
      slot: "operation"
    }
  ];

  async function onSearch() {
    loading.value = true;
    const params: QueryTableParams = {
      page: pagination.currentPage,
      pagesize: pagination.pageSize,
      sconlist: []
    };
    if (form.keyword !== "") {
      params.sconlist.push({
        ParamName: "Reason",
        ParamType: "like",
        ParamValue: form.keyword
      });
    }
    if (form.scopetype !== "") {
      params.sconlist.push({
        ParamName: "MaskScopeType",
        ParamType: "=",
        ParamValue: form.scopetype
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

  function handleSelectionChange(val: AlarmMaskItem[]) {
    selectedNum.value = val.length;
    selectedRows.value = val;
    tableRef.value.setAdaptive();
  }

  function onSelectionCancel() {
    selectedNum.value = 0;
    selectedRows.value = [];
    tableRef.value.getTableRef().clearSelection();
  }

  async function openDialog(title = "新增", row?: AlarmMaskItem) {
    const formData: AlarmMaskFormItemProps = {
      title,
      SnowId: 0,
      MaskScopeType: 5,
      ScopeId: "",
      MaskMode: 2,
      StartTime: "",
      EndTime: "",
      TimeRanges: "",
      MaskAction: 2,
      DowngradeGrade: "",
      Reason: "",
      OperatorName: "",
      ExpireAt: "",
      IsEnable: true
    };
    if (row && row.SnowId) {
      const {
        SnowId, MaskScopeType, ScopeId, MaskMode, StartTime, EndTime,
        TimeRanges, MaskAction, DowngradeGrade, Reason, OperatorName,
        ExpireAt, IsEnable
      } = row;
      Object.assign(formData, {
        SnowId,
        MaskScopeType,
        ScopeId: ScopeId ?? "",
        MaskMode,
        StartTime: StartTime ?? "",
        EndTime: EndTime ?? "",
        TimeRanges: TimeRanges ?? "",
        MaskAction,
        DowngradeGrade: DowngradeGrade ?? "",
        Reason: Reason ?? "",
        OperatorName: OperatorName ?? "",
        ExpireAt: ExpireAt ?? "",
        IsEnable
      });
    }

    addDialog({
      title: `${title}${ModuleTitle}`,
      props: {
        formInline: formData
      },
      width: "620px",
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

  async function handleDelete(row: AlarmMaskItem) {
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
      ElMessage.warning("请先选择要删除的屏蔽规则");
      return;
    }
    const results = await Promise.all(
      selectedRows.value.map(row => deleteByPk(row.SnowId.toString()))
    );
    const successCount = results.filter(result => result.Status).length;
    const failCount = results.length - successCount;
    message(
      `成功删除 ${successCount} 条规则${failCount > 0 ? `，失败 ${failCount} 条` : ""}`,
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
    handleDelete,
    onbatchDel,
    onSelectionCancel
  };
}
