import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import { storageSession } from "@pureadmin/utils";
import type { PaginationProps } from "@pureadmin/table";
import { h, ref, reactive, onMounted } from "vue";
import type { QueryTableParams } from "@/api/type";
import type {
  UserItem,
  RoleOption,
  TenantOption,
  UserFormItemProps
} from "./types";
import {
  getListByPage,
  insert,
  update,
  deleteById,
  toggleEnable,
  resetPwd,
  getRoleList,
  getTenantList
} from "@/api/system/user";
import editForm from "../form.vue";

export function useSysUser() {
  const ModuleTitle = "用户";
  const form = reactive({ keyword: "" });
  const formRef = ref();
  const dataList = ref<UserItem[]>([]);
  const roleList = ref<RoleOption[]>([]);
  const tenantList = ref<TenantOption[]>([]);
  const loading = ref(true);
  /** 平台超管:可为用户指定任意租户,列表也显示租户列 */
  const isSystem = !!storageSession().getItem<any>("is-system")?.data;
  const pagination = reactive<PaginationProps>({
    total: 0,
    pageSize: 10,
    pageSizes: [10, 20, 50, 100],
    currentPage: 1,
    background: true
  });

  const tenantName = (id: number) =>
    tenantList.value.find(t => t.TenantId === id)?.TenantName ??
    (id ? `租户#${id}` : "平台");

  // 非超管只看得到自己租户的人,租户列没有信息量
  const tenantColumn: TableColumnList = isSystem
    ? [
        {
          label: "所属租户",
          prop: "TenantId",
          align: "center",
          width: 130,
          formatter: (row: UserItem) => tenantName(row.TenantId)
        }
      ]
    : [];

  const columns: TableColumnList = [
    { label: "序号", type: "index", width: 70, align: "center" },
    { label: "账号", prop: "UserUid", align: "left", minWidth: 120 },
    {
      label: "姓名",
      prop: "TrueName",
      align: "left",
      minWidth: 100,
      formatter: row => row.TrueName || "-"
    },
    ...tenantColumn,
    {
      label: "角色",
      prop: "RoleName",
      align: "center",
      width: 130,
      formatter: row => row.RoleName || "-"
    },
    {
      label: "手机号",
      prop: "UserPhone",
      align: "center",
      width: 130,
      formatter: row => row.UserPhone || "-"
    },
    {
      label: "状态",
      prop: "IsEnable",
      align: "center",
      width: 90,
      slot: "enable"
    },
    {
      label: "最后登录",
      prop: "LastLoginTime",
      align: "center",
      width: 170,
      formatter: row => row.LastLoginTime || "从未登录"
    },
    { label: "操作", fixed: "right", width: 250, slot: "operation" }
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
        ParamName: "UserUid",
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

  /** 可分配角色全集(后端已按当前登录人的角色层级裁过);表单内再按所选租户过滤 */
  async function loadRoles() {
    const data = await getRoleList();
    if (data.Status && data.Result) roleList.value = JSON.parse(data.Result);
  }

  /** 租户列表:超管建号时选租户 + 列表租户列显示名称 */
  async function loadTenants() {
    if (!isSystem) return;
    const data = await getTenantList({
      page: 1,
      pagesize: 1000,
      sconlist: []
    });
    if (data.Status && data.Result) tenantList.value = JSON.parse(data.Result);
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

  function openDialog(title = "新增", row?: UserItem) {
    const isCreate = !row;
    const formData: UserFormItemProps = {
      title,
      UserId: row?.UserId ?? 0,
      UserUid: row?.UserUid ?? "",
      Password: "",
      TrueName: row?.TrueName ?? "",
      UserXb: row?.UserXb ?? "男",
      UserPhone: row?.UserPhone ?? "",
      RoleId: row?.RoleId ?? 0,
      TenantId: row?.TenantId ?? 0,
      IsEnable: row?.IsEnable ?? 1,
      UserRemark: row?.UserRemark ?? ""
    };
    addDialog({
      title: `${title}${ModuleTitle}`,
      width: "520px",
      draggable: true,
      closeOnClickModal: false,
      contentRenderer: () =>
        h(editForm, {
          formInline: formData,
          isCreate,
          isSystem,
          tenantList: tenantList.value,
          roleList: roleList.value,
          ref: formRef
        }),
      beforeSure: done => {
        const FormRef = formRef.value.getRef();
        FormRef.validate(async valid => {
          if (!valid) return;
          const curData: any = { ...formData };
          delete curData.title;
          if (!isCreate) delete curData.Password;
          const data = isCreate ? await insert(curData) : await update(curData);
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

  async function handleDelete(row: UserItem) {
    const data = await deleteById(row.UserId);
    if (data.Status) {
      message("删除成功", { type: "success" });
      onSearch();
    } else {
      message(data.Message, { type: "error" });
    }
  }

  async function handleToggleEnable(row: UserItem) {
    const data = await toggleEnable(row.UserId);
    if (data.Status) {
      message(row.IsEnable === 1 ? "已禁用" : "已启用", { type: "success" });
    } else {
      message(data.Message, { type: "error" });
    }
    // 无论成败都以后端为准刷新,避免开关状态与库不一致
    onSearch();
  }

  async function handleResetPwd(row: UserItem) {
    const data = await resetPwd(row.UserId);
    if (data.Status) {
      message(`账号【${row.UserUid}】密码已重置为初始密码`, {
        type: "success"
      });
    } else {
      message(data.Message, { type: "error" });
    }
  }

  onMounted(async () => {
    await Promise.all([loadTenants(), loadRoles()]);
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
    handleDelete,
    handleToggleEnable,
    handleResetPwd
  };
}
