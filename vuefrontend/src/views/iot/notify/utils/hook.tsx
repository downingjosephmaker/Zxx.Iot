import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, onMounted } from "vue";
import { ElMessage, ElTag } from "element-plus";
import dayjs from "dayjs";
import type {
  QueryTableParams,
  NotifyChannelItem,
  NotifyChannelFormItemProps
} from "./types";
import { getListByPage, saveBatch, deleteByPk } from "@/api/iot/notify";
import editForm from "../form.vue";

const typeMap = {
  1: { text: "邮件", type: "primary" },
  2: { text: "Webhook", type: "info" },
  3: { text: "钉钉机器人", type: "success" },
  4: { text: "企微机器人", type: "success" },
  5: { text: "短信(预留)", type: "warning" }
} as const;

export function useNotifyChannel(tableRef: Ref) {
  const ModuleTitle = "通知渠道";
  const form = reactive({
    keyword: "",
    channeltype: ""
  });
  const formRef = ref();
  const dataList = ref<NotifyChannelItem[]>([]);
  const loading = ref(true);
  const selectedNum = ref(0);
  const selectedRows = ref<NotifyChannelItem[]>([]);
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
      label: "渠道名称",
      prop: "ChannelName",
      align: "left",
      minWidth: 140
    },
    {
      label: "类型",
      prop: "ChannelType",
      align: "center",
      width: 120,
      cellRenderer: ({ row }) => {
        const item = typeMap[row.ChannelType] ?? {
          text: "未知",
          type: "warning"
        };
        return h(ElTag, { type: item.type, effect: "light" }, () => item.text);
      }
    },
    {
      label: "目标地址",
      prop: "TargetUrl",
      align: "left",
      minWidth: 200,
      showOverflowTooltip: true
    },
    {
      label: "等级过滤",
      prop: "GradeFilter",
      align: "center",
      width: 110,
      formatter: row => row.GradeFilter || "全部"
    },
    {
      label: "升级梯队",
      prop: "EscalationLevel",
      align: "center",
      width: 100,
      formatter: row =>
        row.EscalationLevel === 0 ? "立即" : `梯队${row.EscalationLevel}`
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
        ParamName: "ChannelName",
        ParamType: "like",
        ParamValue: form.keyword
      });
    }
    if (form.channeltype !== "") {
      params.sconlist.push({
        ParamName: "ChannelType",
        ParamType: "=",
        ParamValue: form.channeltype
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

  function handleSelectionChange(val: NotifyChannelItem[]) {
    selectedNum.value = val.length;
    selectedRows.value = val;
    tableRef.value.setAdaptive();
  }

  function onSelectionCancel() {
    selectedNum.value = 0;
    selectedRows.value = [];
    tableRef.value.getTableRef().clearSelection();
  }

  async function openDialog(title = "新增", row?: NotifyChannelItem) {
    const formData: NotifyChannelFormItemProps = {
      title,
      SnowId: 0,
      ChannelName: "",
      ChannelType: 2,
      TargetUrl: "",
      Secret: "",
      Receivers: "",
      GradeFilter: "",
      EscalationLevel: 0,
      IsEnable: true
    };
    if (row && row.SnowId) {
      const {
        SnowId, ChannelName, ChannelType, TargetUrl,
        Secret, Receivers, GradeFilter, EscalationLevel, IsEnable
      } = row;
      Object.assign(formData, {
        SnowId,
        ChannelName,
        ChannelType,
        TargetUrl: TargetUrl ?? "",
        Secret: Secret ?? "",
        Receivers: Receivers ?? "",
        GradeFilter: GradeFilter ?? "",
        EscalationLevel,
        IsEnable
      });
    }

    addDialog({
      title: `${title}${ModuleTitle}`,
      props: {
        formInline: formData
      },
      width: "600px",
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

  async function handleDelete(row: NotifyChannelItem) {
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
      ElMessage.warning("请先选择要删除的渠道");
      return;
    }
    const results = await Promise.all(
      selectedRows.value.map(row => deleteByPk(row.SnowId.toString()))
    );
    const successCount = results.filter(result => result.Status).length;
    const failCount = results.length - successCount;
    message(
      `成功删除 ${successCount} 个渠道${failCount > 0 ? `，失败 ${failCount} 个` : ""}`,
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
