import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, onMounted } from "vue";
import { ElMessage, ElTag } from "element-plus";
import dayjs from "dayjs";
import type {
  QueryTableParams,
  ProductCommandItem,
  ProductCommandFormItemProps
} from "./types";
import { getListByPage, saveBatch, deleteByPk } from "@/api/iot/command";
import editForm from "../form.vue";

export function useProductCommand(tableRef: Ref) {
  const ModuleTitle = "产品命令";
  const form = reactive({
    keyword: "",
    typecode: ""
  });
  const formRef = ref();
  const dataList = ref<ProductCommandItem[]>([]);
  const loading = ref(true);
  const selectedNum = ref(0);
  const selectedRows = ref<ProductCommandItem[]>([]);
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
      label: "产品编码",
      prop: "DeviceTypeCode",
      align: "left",
      minWidth: 120
    },
    {
      label: "命令名称",
      prop: "CommandName",
      align: "left",
      minWidth: 130
    },
    {
      label: "控制类型",
      prop: "ClassName",
      align: "left",
      minWidth: 140
    },
    {
      label: "二次确认",
      prop: "NeedConfirm",
      align: "center",
      width: 90,
      cellRenderer: ({ row }) =>
        h(
          ElTag,
          { type: row.NeedConfirm ? "danger" : "info", effect: "light" },
          () => (row.NeedConfirm ? "需确认" : "直发")
        )
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
        ParamName: "CommandName",
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

  function handleSelectionChange(val: ProductCommandItem[]) {
    selectedNum.value = val.length;
    selectedRows.value = val;
    tableRef.value.setAdaptive();
  }

  function onSelectionCancel() {
    selectedNum.value = 0;
    selectedRows.value = [];
    tableRef.value.getTableRef().clearSelection();
  }

  async function openDialog(title = "新增", row?: ProductCommandItem) {
    const formData: ProductCommandFormItemProps = {
      title,
      SnowId: 0,
      DeviceTypeCode: "",
      CommandName: "",
      ClassName: "",
      ParamSchema: "",
      ConTemplate: "",
      NeedConfirm: false,
      IsEnable: true
    };
    if (row && row.SnowId) {
      const {
        SnowId, DeviceTypeCode, CommandName, ClassName,
        ParamSchema, ConTemplate, NeedConfirm, IsEnable
      } = row;
      Object.assign(formData, {
        SnowId,
        DeviceTypeCode: DeviceTypeCode ?? "",
        CommandName: CommandName ?? "",
        ClassName: ClassName ?? "",
        ParamSchema: ParamSchema ?? "",
        ConTemplate: ConTemplate ?? "",
        NeedConfirm,
        IsEnable
      });
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

  async function handleDelete(row: ProductCommandItem) {
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
      ElMessage.warning("请先选择要删除的命令");
      return;
    }
    const results = await Promise.all(
      selectedRows.value.map(row => deleteByPk(row.SnowId.toString()))
    );
    const successCount = results.filter(result => result.Status).length;
    const failCount = results.length - successCount;
    message(
      `成功删除 ${successCount} 条命令${failCount > 0 ? `，失败 ${failCount} 条` : ""}`,
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
