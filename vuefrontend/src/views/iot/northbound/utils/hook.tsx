import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, onMounted } from "vue";
import { ElMessage, ElTag } from "element-plus";
import dayjs from "dayjs";
import type {
  QueryTableParams,
  NorthboundSinkItem,
  NorthboundSinkFormItemProps
} from "./types";
import {
  getListByPage,
  saveBatch,
  deleteByPk,
  getStatus,
  getSample,
  postTestSend,
  type SinkStatusItem,
  type SinkTestResultItem
} from "@/api/iot/northbound";
import editForm from "../form.vue";

const typeMap = {
  1: { text: "MQTT", type: "success" },
  2: { text: "HTTP Webhook", type: "primary" },
  3: { text: "Kafka", type: "info" }
} as const;

const contentMap = {
  1: "仅遥测",
  2: "仅告警",
  3: "遥测+告警"
} as const;

const scopeMap = {
  0: "全部设备",
  1: "按产品",
  2: "按设备"
} as const;

export function useNorthboundSink(tableRef: Ref) {
  const ModuleTitle = "北向目的地";
  const form = reactive({
    keyword: "",
    sinktype: ""
  });
  const formRef = ref();
  const dataList = ref([]);
  const loading = ref(true);
  const selectedNum = ref(0);
  const selectedRows = ref<NorthboundSinkItem[]>([]);
  const pagination = reactive<PaginationProps>({
    total: 0,
    pageSize: 10,
    pageSizes: [10, 20, 50, 100],
    currentPage: 1,
    background: true
  });

  /** 队列水位弹窗 */
  const statusVisible = ref(false);
  const statusLoading = ref(false);
  const statusList = ref<SinkStatusItem[]>([]);

  /** 样例报文弹窗 */
  const sampleVisible = ref(false);
  const sampleData = ref<SinkTestResultItem>();

  const columns = [
    {
      label: "序号",
      type: "index",
      width: 70,
      align: "center"
    },
    {
      label: "目的地名称",
      prop: "SinkName",
      align: "left",
      minWidth: 140
    },
    {
      label: "类型",
      prop: "SinkType",
      align: "center",
      width: 130,
      cellRenderer: ({ row }) => {
        const item = typeMap[row.SinkType] ?? { text: "未知", type: "warning" };
        return h(ElTag, { type: item.type, effect: "light" }, () => item.text);
      }
    },
    {
      label: "转发内容",
      prop: "ContentMode",
      align: "center",
      width: 100,
      formatter: row => contentMap[row.ContentMode] ?? "-"
    },
    {
      label: "推送范围",
      prop: "ScopeType",
      align: "center",
      width: 100,
      formatter: row => scopeMap[row.ScopeType] ?? "-"
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
        ParamName: "SinkName",
        ParamType: "like",
        ParamValue: form.keyword
      });
    }
    if (form.sinktype !== "") {
      params.sconlist.push({
        ParamName: "SinkType",
        ParamType: "=",
        ParamValue: form.sinktype
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

  function handleSelectionChange(val: NorthboundSinkItem[]) {
    selectedNum.value = val.length;
    selectedRows.value = val;
    tableRef.value.setAdaptive();
  }

  function onSelectionCancel() {
    selectedNum.value = 0;
    selectedRows.value = [];
    tableRef.value.getTableRef().clearSelection();
  }

  /** 从行数据的ConnConfig解析出表单辅助字段 */
  function parseConnConfig(formData: NorthboundSinkFormItemProps, row?: any) {
    if (!row?.ConnConfig) return;
    try {
      const config = JSON.parse(row.ConnConfig);
      if (row.SinkType === 1) {
        Object.assign(formData.mqttConfig, config);
      } else {
        formData.httpConfig.Url = config.Url ?? "";
        formData.httpConfig.HeadersJson =
          config.Headers && Object.keys(config.Headers).length > 0
            ? JSON.stringify(config.Headers)
            : "";
      }
    } catch {
      // 配置JSON损坏时按空白表单处理
    }
  }

  async function openDialog(title = "新增", row?: NorthboundSinkItem) {
    const formData: NorthboundSinkFormItemProps = {
      title,
      SnowId: 0,
      SinkName: "",
      SinkType: 2,
      ConnConfig: "",
      ContentMode: 3,
      ScopeType: 0,
      ScopeJson: "",
      IsEnable: true,
      mqttConfig: {
        Host: "",
        Port: 1883,
        ClientId: "",
        UserName: "",
        Password: "",
        DataTopic: "iot/data/{deviceId}",
        EventTopic: "iot/event/{deviceId}"
      },
      httpConfig: {
        Url: "",
        HeadersJson: ""
      }
    };
    if (row && row.SnowId) {
      const { SnowId, SinkName, SinkType, ConnConfig, ContentMode, ScopeType, ScopeJson, IsEnable } = row;
      Object.assign(formData, {
        SnowId,
        SinkName,
        SinkType,
        ConnConfig,
        ContentMode,
        ScopeType,
        ScopeJson: ScopeJson ?? "",
        IsEnable
      });
      parseConnConfig(formData, row);
    }

    addDialog({
      title: `${title}${ModuleTitle}`,
      props: {
        formInline: formData
      },
      width: "640px",
      draggable: true,
      fullscreenIcon: true,
      closeOnClickModal: false,
      contentRenderer: () => h(editForm, { formInline: formData, ref: formRef }),
      beforeSure: (done, { options }) => {
        const FormRef = formRef.value.getRef();
        const curData = { ...options.props.formInline };
        FormRef.validate(async valid => {
          if (valid) {
            // 剔除表单辅助字段，ConnConfig已由form.vue的watch序列化
            delete curData.title;
            delete curData.mqttConfig;
            delete curData.httpConfig;
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

  async function handleDelete(row: NorthboundSinkItem) {
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
      ElMessage.warning("请先选择要删除的目的地");
      return;
    }
    const results = await Promise.all(
      selectedRows.value.map(row => deleteByPk(row.SnowId.toString()))
    );
    const successCount = results.filter(result => result.Status).length;
    const failCount = results.length - successCount;
    message(
      `成功删除 ${successCount} 个目的地${failCount > 0 ? `，失败 ${failCount} 个` : ""}`,
      { type: failCount === 0 ? "success" : "warning" }
    );
    onSearch();
    onSelectionCancel();
  }

  /** 队列水位快照 */
  async function openStatus() {
    statusVisible.value = true;
    statusLoading.value = true;
    const data = await getStatus();
    if (data.Status) {
      statusList.value = JSON.parse(data.Result);
    } else {
      message(data.Message, { type: "error" });
    }
    statusLoading.value = false;
  }

  /** 样例报文预览（干跑不发送） */
  async function openSample(row: NorthboundSinkItem) {
    const data = await getSample(row.SnowId.toString());
    if (data.Status) {
      sampleData.value = JSON.parse(data.Result);
      sampleVisible.value = true;
    } else {
      message(data.Message, { type: "error" });
    }
  }

  /** 测试连接并实发一条样例报文 */
  async function handleTestSend(row: NorthboundSinkItem) {
    const data = await postTestSend(row.SnowId.toString());
    if (data.Status) {
      const result: SinkTestResultItem = JSON.parse(data.Result);
      message(result.Message || (result.Success ? "测试发送成功" : "测试发送失败"), {
        type: result.Success ? "success" : "error"
      });
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
    selectedNum,
    pagination,
    statusVisible,
    statusLoading,
    statusList,
    sampleVisible,
    sampleData,
    handleSizeChange,
    handleCurrentChange,
    handleSelectionChange,
    onSearch,
    resetForm,
    openDialog,
    handleDelete,
    onbatchDel,
    onSelectionCancel,
    openStatus,
    openSample,
    handleTestSend
  };
}
