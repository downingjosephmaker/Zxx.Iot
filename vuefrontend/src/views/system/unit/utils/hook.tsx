import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, onMounted } from "vue";
import dayjs from "dayjs";
import type { QueryTableParams, TenantItem, TenantFormItemProps } from "./types";
import {
  getUnitListByPage,
  saveUnitBatch,
  deleteUnitByPk
} from "@/api/system";
import editForm from "../form.vue";

export function useTenantUnit(tableRef: Ref) {
  const ModuleTitle = "租户";
  const form = reactive({
    keyword: ""
  });
  const formRef = ref();
  const dataList = ref<TenantItem[]>([]);
  const loading = ref(true);
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
      label: "租户名称",
      prop: "TenantName",
      align: "left",
      minWidth: 140
    },
    {
      label: "租户ID",
      prop: "TenantId",
      align: "center",
      width: 90
    },
    {
      label: "层级全称",
      prop: "FullName",
      align: "left",
      minWidth: 200,
      showOverflowTooltip: true,
      formatter: row => row.FullName || row.TenantName
    },
    {
      label: "备注",
      prop: "Remark",
      align: "left",
      minWidth: 140,
      showOverflowTooltip: true,
      formatter: row => row.Remark || "-"
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
        ParamName: "TenantName",
        ParamType: "like",
        ParamValue: form.keyword
      });
    }
    const data = await getUnitListByPage(params);
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

  async function openDialog(title = "新增", row?: TenantItem) {
    // 编辑时整行透传：FullCode/TreeLevel/HasChild 等树字段由后端 DAO 维护，
    // 但更新走整行写回，缺字段会被清零，故必须原样带上
    const formData: TenantFormItemProps = {
      title,
      TenantId: row?.TenantId ?? 0,
      ParentId: row?.ParentId ?? 0,
      TreeLevel: row?.TreeLevel ?? 1,
      FullCode: row?.FullCode ?? "",
      FullName: row?.FullName ?? "",
      HasChild: row?.HasChild ?? false,
      TenantName: row?.TenantName ?? "",
      Remark: row?.Remark ?? ""
    };

    addDialog({
      title: `${title}${ModuleTitle}`,
      props: {
        formInline: formData
      },
      width: "520px",
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
            const data = await saveUnitBatch([curData]);
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

  async function handleDelete(row: TenantItem) {
    const data = await deleteUnitByPk({ _TenantId: row.TenantId });
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
