import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, onMounted } from "vue";
import { ElMessage, ElTag } from "element-plus";
import dayjs from "dayjs";
import type {
  QueryTableParams,
  ProtocolScriptItem,
  ProtocolScriptHistoryItem,
  ProtocolScriptFormItemProps,
  ScriptRunResultItem
} from "./types";
import { SCRIPT_TEMPLATE } from "./types";
import {
  getListByPage,
  saveBatch,
  deleteByPk,
  getHistoryList,
  postDryRun
} from "@/api/iot/script";
import editForm from "../form.vue";

export function useProtocolScript(tableRef: Ref) {
  const ModuleTitle = "协议脚本";
  const form = reactive({
    keyword: "",
    typecode: ""
  });
  const formRef = ref();
  const dataList = ref<ProtocolScriptItem[]>([]);
  const loading = ref(true);
  const selectedNum = ref(0);
  const selectedRows = ref<ProtocolScriptItem[]>([]);
  const pagination = reactive<PaginationProps>({
    total: 0,
    pageSize: 10,
    pageSizes: [10, 20, 50, 100],
    currentPage: 1,
    background: true
  });

  /** 版本历史弹窗 */
  const historyVisible = ref(false);
  const historyLoading = ref(false);
  const historyList = ref<ProtocolScriptHistoryItem[]>([]);
  const historyScript = ref<ProtocolScriptItem>();
  const historyContent = ref("");

  /** 调试台弹窗 */
  const debugVisible = ref(false);
  const debugRunning = ref(false);
  const debugForm = reactive({
    ScriptId: "0" as number | string,
    ScriptName: "",
    ScriptContent: "",
    FuncName: "decode",
    InputHex: "",
    InputJson: "",
    ContextJson: ""
  });
  const debugResult = ref<ScriptRunResultItem>();

  const columns = [
    {
      label: "序号",
      type: "index",
      width: 70,
      align: "center"
    },
    {
      label: "脚本名称",
      prop: "ScriptName",
      align: "left",
      minWidth: 140
    },
    {
      label: "挂靠产品编码",
      prop: "DeviceTypeCode",
      align: "left",
      minWidth: 130
    },
    {
      label: "版本",
      prop: "Version",
      align: "center",
      width: 80,
      formatter: row => `v${row.Version}`
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
          () => (row.IsEnable ? "启用" : "禁用")
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
      width: 300,
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
        ParamName: "ScriptName",
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

  function handleSelectionChange(val: ProtocolScriptItem[]) {
    selectedNum.value = val.length;
    selectedRows.value = val;
    tableRef.value.setAdaptive();
  }

  function onSelectionCancel() {
    selectedNum.value = 0;
    selectedRows.value = [];
    tableRef.value.getTableRef().clearSelection();
  }

  async function openDialog(
    title = "新增",
    row?: ProtocolScriptItem,
    overridecontent?: string
  ) {
    const formData: ProtocolScriptFormItemProps = {
      title,
      SnowId: 0,
      ScriptName: "",
      DeviceTypeCode: "",
      ScriptContent: SCRIPT_TEMPLATE,
      Version: 1,
      SampleHex: "",
      SampleContext: "",
      IsEnable: false
    };
    if (row && row.SnowId) {
      const {
        SnowId, ScriptName, DeviceTypeCode, ScriptContent,
        Version, SampleHex, SampleContext, IsEnable
      } = row;
      Object.assign(formData, {
        SnowId,
        ScriptName,
        DeviceTypeCode: DeviceTypeCode ?? "",
        ScriptContent: overridecontent ?? ScriptContent ?? "",
        Version,
        SampleHex: SampleHex ?? "",
        SampleContext: SampleContext ?? "",
        IsEnable
      });
    }

    addDialog({
      title: `${title}${ModuleTitle}`,
      props: {
        formInline: formData
      },
      width: "760px",
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

  async function handleDelete(row: ProtocolScriptItem) {
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
      ElMessage.warning("请先选择要删除的脚本");
      return;
    }
    const results = await Promise.all(
      selectedRows.value.map(row => deleteByPk(row.SnowId.toString()))
    );
    const successCount = results.filter(result => result.Status).length;
    const failCount = results.length - successCount;
    message(
      `成功删除 ${successCount} 个脚本${failCount > 0 ? `，失败 ${failCount} 个` : ""}`,
      { type: failCount === 0 ? "success" : "warning" }
    );
    onSearch();
    onSelectionCancel();
  }

  /** 版本历史 */
  async function openHistory(row: ProtocolScriptItem) {
    historyScript.value = row;
    historyContent.value = "";
    historyVisible.value = true;
    historyLoading.value = true;
    const data = await getHistoryList(row.SnowId.toString());
    if (data.Status) {
      historyList.value = JSON.parse(data.Result);
    } else {
      message(data.Message, { type: "error" });
    }
    historyLoading.value = false;
  }

  function viewHistoryContent(row: ProtocolScriptHistoryItem) {
    historyContent.value = row.ScriptContent ?? "";
  }

  /** 以某历史版本内容发起修改（保存后成为新版本） */
  function editFromHistory(row: ProtocolScriptHistoryItem) {
    if (!historyScript.value) return;
    historyVisible.value = false;
    openDialog("修改", historyScript.value, row.ScriptContent ?? "");
  }

  /** 打开调试台（草稿内容优先，未保存也可调试） */
  function openDebug(row?: ProtocolScriptItem) {
    debugResult.value = undefined;
    debugForm.ScriptId = row?.SnowId?.toString() ?? "0";
    debugForm.ScriptName = row?.ScriptName ?? "（未保存草稿）";
    debugForm.ScriptContent = row?.ScriptContent ?? SCRIPT_TEMPLATE;
    debugForm.FuncName = "decode";
    debugForm.InputHex = row?.SampleHex ?? "";
    debugForm.InputJson = "";
    debugForm.ContextJson = row?.SampleContext ?? "";
    debugVisible.value = true;
  }

  /** 执行干跑 */
  async function runDebug() {
    debugRunning.value = true;
    debugResult.value = undefined;
    const data = await postDryRun({
      ScriptId: debugForm.ScriptId,
      ScriptContent: debugForm.ScriptContent,
      FuncName: debugForm.FuncName,
      InputHex: debugForm.InputHex,
      InputJson: debugForm.InputJson,
      ContextJson: debugForm.ContextJson
    });
    if (data.Status) {
      debugResult.value = JSON.parse(data.Result);
    } else {
      message(data.Message, { type: "error" });
    }
    debugRunning.value = false;
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
    historyVisible,
    historyLoading,
    historyList,
    historyContent,
    debugVisible,
    debugRunning,
    debugForm,
    debugResult,
    handleSizeChange,
    handleCurrentChange,
    handleSelectionChange,
    onSearch,
    resetForm,
    openDialog,
    handleDelete,
    onbatchDel,
    onSelectionCancel,
    openHistory,
    viewHistoryContent,
    editFromHistory,
    openDebug,
    runDebug
  };
}
