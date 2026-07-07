import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, onMounted } from "vue";
import { ElMessage, ElMessageBox, ElTag } from "element-plus";
import dayjs from "dayjs";
import type {
  QueryTableParams,
  LinkageRuleItem,
  LinkageRuleFormItemProps
} from "./types";
import {
  getListByPage,
  saveBatch,
  deleteByPk,
  getMetrics,
  getDryRun,
  type RuleMetricsItem,
  type LinkageDryRunResultItem
} from "@/api/iot/linkage";
import editForm from "../form.vue";

const triggerMap = {
  1: { text: "点位变化", type: "primary" },
  2: { text: "告警产生", type: "danger" },
  3: { text: "告警恢复", type: "success" },
  4: { text: "定时cron", type: "warning" },
  5: { text: "设备上线", type: "success" },
  6: { text: "设备离线", type: "info" }
} as const;

const actionMap = {
  1: { text: "下发命令", type: "danger" },
  2: { text: "写虚拟点位", type: "warning" },
  3: { text: "发通知", type: "primary" },
  4: { text: "Webhook", type: "info" }
} as const;

/** 指标弹窗行（后端字典键=规则SnowId字符串，联表当前页规则名） */
export interface MetricsRow extends RuleMetricsItem {
  RuleId: string;
  RuleName: string;
}

export function useLinkageRule(tableRef: Ref) {
  const ModuleTitle = "规则联动";
  const form = reactive({
    keyword: "",
    triggertype: ""
  });
  const formRef = ref();
  const dataList = ref<LinkageRuleItem[]>([]);
  const loading = ref(true);
  const selectedNum = ref(0);
  const selectedRows = ref<LinkageRuleItem[]>([]);
  const pagination = reactive<PaginationProps>({
    total: 0,
    pageSize: 10,
    pageSizes: [10, 20, 50, 100],
    currentPage: 1,
    background: true
  });

  /** 漏斗指标弹窗 */
  const metricsVisible = ref(false);
  const metricsLoading = ref(false);
  const metricsList = ref<MetricsRow[]>([]);

  /** 干跑结果弹窗 */
  const dryrunVisible = ref(false);
  const dryrunData = ref<LinkageDryRunResultItem>();

  const columns = [
    {
      label: "序号",
      type: "index",
      width: 70,
      align: "center"
    },
    {
      label: "规则名称",
      prop: "RuleName",
      align: "left",
      minWidth: 140
    },
    {
      label: "触发",
      prop: "TriggerType",
      align: "center",
      width: 110,
      cellRenderer: ({ row }) => {
        const item = triggerMap[row.TriggerType] ?? {
          text: "未知",
          type: "warning"
        };
        return h(ElTag, { type: item.type, effect: "light" }, () => item.text);
      }
    },
    {
      label: "动作",
      prop: "ActionType",
      align: "center",
      width: 120,
      cellRenderer: ({ row }) => {
        const item = actionMap[row.ActionType] ?? {
          text: "未知",
          type: "warning"
        };
        return h(ElTag, { type: item.type, effect: "light" }, () => item.text);
      }
    },
    {
      label: "条件表达式",
      prop: "ConditionFormula",
      align: "left",
      minWidth: 180,
      formatter: row => row.ConditionFormula || "（恒真）"
    },
    {
      label: "冷却(秒)",
      prop: "CooldownSeconds",
      align: "right",
      width: 90
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
      width: 240,
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
        ParamName: "RuleName",
        ParamType: "like",
        ParamValue: form.keyword
      });
    }
    if (form.triggertype !== "") {
      params.sconlist.push({
        ParamName: "TriggerType",
        ParamType: "=",
        ParamValue: form.triggertype
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

  function handleSelectionChange(val: LinkageRuleItem[]) {
    selectedNum.value = val.length;
    selectedRows.value = val;
    tableRef.value.setAdaptive();
  }

  function onSelectionCancel() {
    selectedNum.value = 0;
    selectedRows.value = [];
    tableRef.value.getTableRef().clearSelection();
  }

  /** 从行数据的ActionConfig解析出表单辅助字段 */
  function parseActionConfig(formData: LinkageRuleFormItemProps, row?: any) {
    if (!row?.ActionConfig) return;
    try {
      const config = JSON.parse(row.ActionConfig);
      switch (row.ActionType) {
        case 1:
          formData.cmdConfig.PluginGuid = config.PluginGuid ?? "";
          formData.cmdConfig.ClassName = config.ClassName ?? "";
          formData.cmdConfig.ConContent = config.ConContent ?? "";
          formData.cmdConfig.DeviceIdsText = Array.isArray(config.DeviceIds)
            ? config.DeviceIds.join(",")
            : "";
          break;
        case 2:
          Object.assign(formData.vpConfig, config);
          break;
        case 4:
          Object.assign(formData.webhookConfig, config);
          break;
        default:
          Object.assign(formData.notifyConfig, config);
          break;
      }
    } catch {
      // 配置JSON损坏时按空白表单处理
    }
  }

  async function openDialog(title = "新增", row?: LinkageRuleItem) {
    const formData: LinkageRuleFormItemProps = {
      title,
      SnowId: 0,
      RuleName: "",
      TriggerType: 1,
      TriggerDeviceId: 0,
      TriggerParamCode: "",
      TriggerCron: "",
      ConditionFormula: "",
      TimeRanges: "",
      ActionType: 3,
      ActionConfig: "",
      CooldownSeconds: 60,
      IsEnable: true,
      cmdConfig: { PluginGuid: "", ClassName: "", ConContent: "", DeviceIdsText: "" },
      vpConfig: { DeviceId: 0, ParamCode: "", ParamValue: "" },
      notifyConfig: { Content: "" },
      webhookConfig: { Url: "", Body: "" }
    };
    if (row && row.SnowId) {
      const {
        SnowId, RuleName, TriggerType, TriggerDeviceId, TriggerParamCode,
        TriggerCron, ConditionFormula, TimeRanges, ActionType, ActionConfig,
        CooldownSeconds, IsEnable
      } = row;
      Object.assign(formData, {
        SnowId,
        RuleName,
        TriggerType,
        TriggerDeviceId,
        TriggerParamCode: TriggerParamCode ?? "",
        TriggerCron: TriggerCron ?? "",
        ConditionFormula: ConditionFormula ?? "",
        TimeRanges: TimeRanges ?? "",
        ActionType,
        ActionConfig,
        CooldownSeconds,
        IsEnable
      });
      parseActionConfig(formData, row);
    }

    addDialog({
      title: `${title}${ModuleTitle}`,
      props: {
        formInline: formData
      },
      width: "680px",
      draggable: true,
      fullscreenIcon: true,
      closeOnClickModal: false,
      contentRenderer: () => h(editForm, { formInline: formData, ref: formRef }),
      beforeSure: (done, { options }) => {
        const FormRef = formRef.value.getRef();
        const curData = { ...options.props.formInline };
        FormRef.validate(async valid => {
          if (valid) {
            // 剔除表单辅助字段，ActionConfig已由form.vue的watch序列化
            delete curData.title;
            delete curData.cmdConfig;
            delete curData.vpConfig;
            delete curData.notifyConfig;
            delete curData.webhookConfig;
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

  async function handleDelete(row: LinkageRuleItem) {
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
      ElMessage.warning("请先选择要删除的规则");
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

  /** 漏斗指标快照（联表当前页规则名，不在当前页的显示规则ID） */
  async function openMetrics() {
    metricsVisible.value = true;
    metricsLoading.value = true;
    const data = await getMetrics();
    if (data.Status) {
      const dict: Record<string, RuleMetricsItem> = JSON.parse(data.Result);
      metricsList.value = Object.entries(dict).map(([ruleId, metrics]) => ({
        RuleId: ruleId,
        RuleName:
          dataList.value.find(rule => rule.SnowId.toString() === ruleId)
            ?.RuleName ?? `规则#${ruleId}`,
        ...metrics
      }));
    } else {
      message(data.Message, { type: "error" });
    }
    metricsLoading.value = false;
  }

  /** 试运行（输入模拟触发设备ID后干跑，无副作用） */
  async function handleDryRun(row: LinkageRuleItem) {
    let deviceid = 0;
    try {
      const { value } = await ElMessageBox.prompt(
        "输入模拟触发设备ID（条件裸参数编码按此设备取最新值，0=不代入）",
        `试运行：${row.RuleName}`,
        {
          inputValue: row.TriggerDeviceId?.toString() ?? "0",
          inputPattern: /^\d+$/,
          inputErrorMessage: "请输入非负整数"
        }
      );
      deviceid = parseInt(value, 10);
    } catch {
      return; // 用户取消
    }
    const data = await getDryRun(row.SnowId.toString(), deviceid);
    if (data.Status) {
      dryrunData.value = JSON.parse(data.Result);
      dryrunVisible.value = true;
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
    metricsVisible,
    metricsLoading,
    metricsList,
    dryrunVisible,
    dryrunData,
    handleSizeChange,
    handleCurrentChange,
    handleSelectionChange,
    onSearch,
    resetForm,
    openDialog,
    handleDelete,
    onbatchDel,
    onSelectionCancel,
    openMetrics,
    handleDryRun
  };
}
