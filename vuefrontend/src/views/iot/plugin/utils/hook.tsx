import { message } from "@/utils/message";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, computed, onMounted } from "vue";
import { ElTag } from "element-plus";
import type { QueryTableParams } from "@/api/type";
import {
  getListByPage,
  getConfigSchema,
  enablePlugin,
  saveConfig,
  deletePlugin,
  uploadPluginFile,
  scanPlugins,
  type SysPluginItem
} from "@/api/iot/plugin";

export function useSysPlugin(tableRef: Ref) {
  const form = reactive({
    keyword: "",
    pluginstatus: ""
  });
  const dataList = ref<SysPluginItem[]>([]);
  const loading = ref(true);
  const pagination = reactive<PaginationProps>({
    total: 0,
    pageSize: 10,
    pageSizes: [10, 20, 50, 100],
    currentPage: 1,
    background: true
  });

  /** 上传弹窗(zip内插件Guid决定登记/更新对象,新插件默认停用) */
  const uploadVisible = ref(false);
  const uploading = ref(false);
  const uploadFile = ref<File | null>(null);
  const scanning = ref(false);

  /** 配置弹窗(有Manifest走Schema动态表单,否则回落JSON文本编辑) */
  const configVisible = ref(false);
  const configLoading = ref(false);
  const configSaving = ref(false);
  const configRow = ref<SysPluginItem | null>(null);
  const configSchema = ref("");
  const configModel = reactive<Record<string, unknown>>({});
  const configJsonText = ref("");
  const schemaFormRef = ref();
  const schemaMode = computed(() => configSchema.value !== "");

  /** 清单详情弹窗 */
  const detailVisible = ref(false);
  const detailRow = ref<SysPluginItem | null>(null);
  const detailManifest = computed(() => {
    const raw = detailRow.value?.PluginManifest;
    if (!raw) return "";
    try {
      return JSON.stringify(JSON.parse(raw), null, 2);
    } catch {
      return raw;
    }
  });

  const columns = [
    {
      label: "序号",
      type: "index",
      width: 70,
      align: "center"
    },
    {
      label: "插件名称",
      prop: "PluginName",
      align: "left",
      minWidth: 130
    },
    {
      label: "类型",
      prop: "PluginType",
      align: "center",
      width: 100
    },
    {
      label: "版本",
      prop: "PluginVersion",
      align: "center",
      width: 80
    },
    {
      label: "状态",
      prop: "PluginStatus",
      align: "center",
      width: 80,
      cellRenderer: ({ row }) =>
        h(
          ElTag,
          { type: row.PluginStatus === 1 ? "success" : "info", effect: "light" },
          () => (row.PluginStatus === 1 ? "启用" : "停用")
        )
    },
    {
      label: "心跳",
      prop: "PluginHeartStatus",
      align: "center",
      width: 80,
      cellRenderer: ({ row }) =>
        row.PluginStatus !== 1
          ? h("span", "-")
          : h(
              ElTag,
              {
                type: row.PluginHeartStatus === 0 ? "success" : "danger",
                effect: "light"
              },
              () => (row.PluginHeartStatus === 0 ? "正常" : "异常")
            )
    },
    {
      label: "心跳时间",
      prop: "PluginHeartTime",
      align: "center",
      width: 160,
      formatter: row => row.PluginHeartTime || "-"
    },
    {
      label: "加载路径",
      prop: "PluginPath",
      align: "left",
      minWidth: 200,
      formatter: row => row.PluginPath || "-"
    },
    {
      label: "更新时间",
      prop: "UpdateTime",
      align: "center",
      width: 160,
      formatter: row => row.UpdateTime || "-"
    },
    {
      label: "操作",
      fixed: "right",
      width: 280,
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
        ParamName: "PluginName",
        ParamType: "like",
        ParamValue: form.keyword
      });
    }
    if (form.pluginstatus !== "") {
      params.sconlist.push({
        ParamName: "PluginStatus",
        ParamType: "=",
        ParamValue: form.pluginstatus
      });
    }
    const data = await getListByPage(params);
    if (data.Status) {
      dataList.value = JSON.parse(data.Result);
      pagination.total = data.Total;
    } else if (data.Message) {
      // 非超管被后端收口时给出明确原因,不留一张无声的空表
      message(data.Message, { type: "warning" });
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

  /** 启停切换(启用即加载启动,停用即Stop卸载,结果消息来自后端真实装卸结果) */
  async function handleEnable(row: SysPluginItem) {
    const next = row.PluginStatus === 1 ? 0 : 1;
    const data = await enablePlugin(row.PluginGuid, next);
    message(data.Message, { type: data.Status ? "success" : "error" });
    onSearch();
  }

  async function handleDelete(row: SysPluginItem) {
    const data = await deletePlugin(row.PluginGuid);
    message(data.Message, { type: data.Status ? "success" : "error" });
    onSearch();
  }

  function openUpload() {
    uploadFile.value = null;
    uploadVisible.value = true;
  }

  function onUploadChange(file: any) {
    uploadFile.value = file?.raw ?? null;
  }

  function onUploadRemove() {
    uploadFile.value = null;
  }

  async function handleUpload() {
    if (!uploadFile.value) {
      message("请先选择插件包(.zip或.dll)", { type: "warning" });
      return;
    }
    uploading.value = true;
    try {
      const data = await uploadPluginFile(uploadFile.value);
      // UploadPluginFile返回MetaData对象,真实状态嵌在信封Result内,双层防御解析
      let meta: any = null;
      if (typeof data.Result === "string" && data.Result) {
        try {
          meta = JSON.parse(data.Result);
        } catch {
          /* 保持信封状态兜底 */
        }
      } else if (data.Result && typeof data.Result === "object") {
        meta = data.Result;
      }
      const ok =
        meta && typeof meta.Status === "boolean" ? meta.Status : !!data.Status;
      const msg =
        meta?.Message || data.Message || (ok ? "上传成功" : "上传失败");
      message(msg, { type: ok ? "success" : "error" });
      if (ok) {
        uploadVisible.value = false;
        onSearch();
      }
    } finally {
      uploading.value = false;
    }
  }

  /** 扫描插件存储目录批量登记入库(返回MetaData,与上传同构双层解析) */
  async function handleScan() {
    scanning.value = true;
    try {
      const data = await scanPlugins();
      let meta: any = null;
      if (typeof data.Result === "string" && data.Result) {
        try {
          meta = JSON.parse(data.Result);
        } catch {
          /* 保持信封状态兜底 */
        }
      } else if (data.Result && typeof data.Result === "object") {
        meta = data.Result;
      }
      const ok =
        meta && typeof meta.Status === "boolean" ? meta.Status : !!data.Status;
      const msg =
        meta?.Message || data.Message || (ok ? "扫描完成" : "扫描失败");
      message(msg, { type: ok ? "success" : "error" });
      if (ok) onSearch();
    } finally {
      scanning.value = false;
    }
  }

  async function openConfig(row: SysPluginItem) {
    configRow.value = row;
    configSchema.value = "";
    configJsonText.value = row.PluginConfig ?? "";
    Object.keys(configModel).forEach(key => delete configModel[key]);
    configVisible.value = true;
    configLoading.value = true;
    try {
      const data = await getConfigSchema(row.PluginGuid);
      if (data.Status && data.Result) {
        let raw = data.Result as string;
        // 信封可能把schema字符串二次JSON编码,归一化成schema文本
        try {
          const once = JSON.parse(raw);
          if (typeof once === "string") raw = once;
        } catch {
          /* 已是schema文本 */
        }
        // 先用现有配置铺底,SchemaForm按schema补种缺省值并纠偏类型
        try {
          Object.assign(configModel, JSON.parse(row.PluginConfig || "{}"));
        } catch {
          /* 配置JSON损坏时按schema缺省起步 */
        }
        configSchema.value = raw;
      } else if (data.Message) {
        message(`${data.Message}已回落JSON文本编辑。`, { type: "warning" });
      }
    } finally {
      configLoading.value = false;
    }
  }

  async function handleSaveConfig() {
    const row = configRow.value;
    if (!row) return;
    let json = "";
    if (schemaMode.value) {
      if (schemaFormRef.value && !(await schemaFormRef.value.validate())) {
        message("请完善配置项", { type: "warning" });
        return;
      }
      json = JSON.stringify(configModel);
    } else {
      json = configJsonText.value.trim();
      if (!json) {
        message("配置内容不能为空", { type: "warning" });
        return;
      }
      try {
        JSON.parse(json);
      } catch {
        message("配置内容不是合法JSON", { type: "error" });
        return;
      }
    }
    configSaving.value = true;
    try {
      const data = await saveConfig(row.PluginGuid, json);
      message(data.Message, { type: data.Status ? "success" : "error" });
      if (data.Status) {
        configVisible.value = false;
        onSearch();
      }
    } finally {
      configSaving.value = false;
    }
  }

  function openDetail(row: SysPluginItem) {
    detailRow.value = row;
    detailVisible.value = true;
  }

  onMounted(() => {
    onSearch();
  });

  return {
    form,
    loading,
    columns,
    dataList,
    pagination,
    uploadVisible,
    uploading,
    uploadFile,
    scanning,
    handleScan,
    configVisible,
    configLoading,
    configSaving,
    configRow,
    configSchema,
    configModel,
    configJsonText,
    schemaFormRef,
    schemaMode,
    detailVisible,
    detailRow,
    detailManifest,
    onSearch,
    resetForm,
    handleSizeChange,
    handleCurrentChange,
    handleEnable,
    handleDelete,
    openUpload,
    onUploadChange,
    onUploadRemove,
    handleUpload,
    openConfig,
    handleSaveConfig,
    openDetail
  };
}
