import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, onMounted } from "vue";
import type { QueryTableParams } from "@/api/type";
import type { ButtonItem, ButtonFormItemProps } from "./types";
import { getButtonList, SaveButtonBatch, deleteButtonByPk } from "@/api/system";
import editForm from "../form.vue";

export function useSysButton(tableRef: Ref) {
  const ModuleTitle = "按钮";
  const form = reactive({ keyword: "" });
  const formRef = ref();
  const dataList = ref<ButtonItem[]>([]);
  const loading = ref(true);
  const pagination = reactive<PaginationProps>({
    total: 0,
    pageSize: 10,
    pageSizes: [10, 20, 50, 100],
    currentPage: 1,
    background: true
  });

  const columns: TableColumnList = [
    { label: "序号", type: "index", width: 70, align: "center" },
    { label: "按钮名称", prop: "ButtonName", align: "left", minWidth: 120 },
    { label: "按钮编码", prop: "ButtonCode", align: "left", minWidth: 160 },
    {
      label: "类型",
      prop: "ButtonType",
      align: "center",
      width: 100,
      formatter: ({ ButtonType }) =>
        ButtonType === 2 ? "表单按钮" : "页面按钮"
    },
    {
      label: "排序",
      prop: "ButtonSort",
      align: "center",
      width: 80,
      formatter: row => row.ButtonSort ?? 0
    },
    {
      label: "备注",
      prop: "ButtonRemark",
      align: "left",
      minWidth: 140,
      showOverflowTooltip: true,
      formatter: row => row.ButtonRemark || "-"
    },
    { label: "操作", fixed: "right", width: 160, slot: "operation" }
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
        ParamName: "ButtonName",
        ParamType: "like",
        ParamValue: form.keyword
      });
    }
    const data = await getButtonList(params);
    if (data.Status) {
      dataList.value = JSON.parse(data.Result);
      pagination.total = data.Total;
    } else if (data.Message) {
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

  function openDialog(title = "新增", row?: ButtonItem) {
    const formData: ButtonFormItemProps = {
      title,
      ButtonId: row?.ButtonId ?? 0,
      ButtonCode: row?.ButtonCode ?? "",
      ButtonName: row?.ButtonName ?? "",
      ButtonHtml: row?.ButtonHtml ?? "",
      ButtonSort: row?.ButtonSort ?? 0,
      ButtonRemark: row?.ButtonRemark ?? "",
      ButtonType: row?.ButtonType ?? 1
    };
    addDialog({
      title: `${title}${ModuleTitle}`,
      props: { formInline: formData },
      width: "500px",
      draggable: true,
      closeOnClickModal: false,
      contentRenderer: () => h(editForm, { formInline: formData, ref: formRef }),
      beforeSure: (done, { options }) => {
        const FormRef = formRef.value.getRef();
        const curData = { ...options.props.formInline };
        FormRef.validate(async valid => {
          if (!valid) return;
          delete curData.title;
          const data = await SaveButtonBatch([curData]);
          if (data.Status) {
            message(`${title}${ModuleTitle}成功`, { type: "success" });
            done();
            onSearch();
          } else {
            message(data.Message, { type: "error" });
          }
        });
      }
    });
  }

  async function handleDelete(row: ButtonItem) {
    const data = await deleteButtonByPk(String(row.ButtonId));
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
    pagination,
    handleSizeChange,
    handleCurrentChange,
    onSearch,
    resetForm,
    openDialog,
    handleDelete
  };
}
