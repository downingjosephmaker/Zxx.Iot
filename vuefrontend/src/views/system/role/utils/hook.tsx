import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import { type Ref, h, ref, reactive, onMounted } from "vue";
import type { QueryTableParams } from "@/api/type";
import type { RoleItem, RoleFormItemProps } from "./types";
import {
  getListByPage,
  insert,
  update,
  deleteByPk
} from "@/api/system/role/index";
import editForm from "../form.vue";
import RoleAuth from "../RoleAuth.vue";

export function useSysRole(tableRef: Ref) {
  const ModuleTitle = "角色";
  const form = reactive({ keyword: "" });
  const formRef = ref();
  const authRef = ref();
  const dataList = ref<RoleItem[]>([]);
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
    { label: "角色名称", prop: "RoleName", align: "left", minWidth: 140 },
    {
      label: "角色描述",
      prop: "RoleDescribe",
      align: "left",
      minWidth: 180,
      showOverflowTooltip: true,
      formatter: row => row.RoleDescribe || "-"
    },
    {
      label: "归属",
      prop: "TenantId",
      align: "center",
      width: 110,
      formatter: row => (row.TenantId ? `租户#${row.TenantId}` : "平台共享")
    },
    { label: "操作", fixed: "right", width: 220, slot: "operation" }
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
        ParamName: "RoleName",
        ParamType: "like",
        ParamValue: form.keyword
      });
    }
    const data = await getListByPage(params);
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

  function openDialog(title = "新增", row?: RoleItem) {
    const formData: RoleFormItemProps = {
      title,
      RoleId: row?.RoleId ?? 0,
      RoleName: row?.RoleName ?? "",
      ParentId: row?.ParentId ?? 0,
      RoleDescribe: row?.RoleDescribe ?? "",
      SortBorder: row?.SortBorder ?? "",
      TreeLevel: row?.TreeLevel ?? 1,
      FullName: row?.FullName ?? "",
      FullCode: row?.FullCode ?? "",
      HasChild: row?.HasChild ?? false
    };
    addDialog({
      title: `${title}${ModuleTitle}`,
      props: { formInline: formData, roleList: dataList.value },
      width: "500px",
      draggable: true,
      closeOnClickModal: false,
      contentRenderer: () =>
        h(editForm, {
          formInline: formData,
          roleList: dataList.value,
          ref: formRef
        }),
      beforeSure: (done, { options }) => {
        const FormRef = formRef.value.getRef();
        const curData = { ...options.props.formInline };
        FormRef.validate(async valid => {
          if (!valid) return;
          delete curData.title;
          const isEdit = !!row;
          const data = isEdit ? await update(curData) : await insert(curData);
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

  function openAuth(row: RoleItem) {
    addDialog({
      title: `菜单授权 - ${row.RoleName}`,
      width: "600px",
      draggable: true,
      closeOnClickModal: false,
      contentRenderer: () =>
        h(RoleAuth, {
          roleId: row.RoleId,
          roleName: row.RoleName,
          ref: authRef
        }),
      beforeSure: async done => {
        const ok = await authRef.value.submit();
        if (ok) done();
      }
    });
  }

  async function handleDelete(row: RoleItem) {
    const data = await deleteByPk(String(row.RoleId));
    if (data.Status) {
      message("删除成功", { type: "success" });
      onSearch();
    } else {
      message(data.Message, { type: "error" });
    }
  }

  onMounted(onSearch);

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
    openAuth,
    handleDelete
  };
}
