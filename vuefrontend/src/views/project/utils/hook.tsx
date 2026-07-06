import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import type { PaginationProps } from "@pureadmin/table";
import type { QueryTableParams } from "@/api/type";
import { type Ref, h, ref, reactive, onMounted } from "vue";
import { useRouter } from "vue-router";
import { ElMessage } from "element-plus";
import dayjs from "dayjs";
import type { ScadaProjectItem, ScadaProjectFormItemProps } from "./types";
import {
  getListByPage,
  deleteByPk,
  saveBatch,
  dashPublish,
  buildRuntimeUrl
} from "@/api/scada/project/index";
import editForm from "../form.vue";

export function useScadaProject(tableRef: Ref) {
  const router = useRouter();
  const ModuleTitle = "SCADA项目";
  const form = reactive({
    keyword: "",
    status: ""
  });
  const formRef = ref();
  const dataList = ref([]);
  const loading = ref(true);
  const selectedNum = ref(0);
  const selectedRows = ref<ScadaProjectItem[]>([]);
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
      width: 80,
      align: "center"
    },
    {
      label: "项目名称",
      prop: "ProjectName",
      align: "left",
      minWidth: 150
    },
    {
      label: "项目描述",
      prop: "ProjectDesc",
      align: "left",
      minWidth: 200,
      showOverflowTooltip: true
    },
    {
      label: "项目状态",
      prop: "ProjectStatus",
      align: "center",
      width: 100,
      formatter: row => {
        const statusMap = {
          0: { text: "未发布", type: "info" },
          1: { text: "已发布", type: "success" }
        };
        const status = statusMap[row.ProjectStatus] || {
          text: "未知",
          type: "warning"
        };
        return h(
          "el-tag",
          {
            type: status.type,
            effect: "light"
          },
          status.text
        );
      }
    },
    {
      label: "创建时间",
      prop: "CreateTime",
      align: "center",
      width: 160,
      formatter: row => {
        return row.CreateTime
          ? dayjs(row.CreateTime).format("YYYY-MM-DD HH:mm:ss")
          : "-";
      }
    },
    {
      label: "操作",
      fixed: "right",
      width: 300,
      slot: "operation"
    }
  ];

  async function handleDelete(row: ScadaProjectItem) {
    const data = await deleteByPk(row.SnowId.toString());
    if (data.Status) {
      message("删除成功", { type: "success" });
      onSearch();
    } else {
      message(data.Message, { type: "error" });
    }
  }

  // 发布/取消发布项目
  async function handlePublish(row: ScadaProjectItem) {
    try {
      // 切换状态：0->1 (发布), 1->0 (取消发布)
      const newStatus = row.ProjectStatus === 0 ? 1 : 0;
      const actionText = newStatus === 1 ? "发布" : "取消发布";

      const data = await dashPublish(
        row.SnowId,
        newStatus,
        newStatus === 1 ? buildRuntimeUrl(row.SnowId) : ""
      );
      if (data.Status) {
        message(`${actionText}成功`, { type: "success" });
        onSearch(); // 刷新列表
      } else {
        message(data.Message || `${actionText}失败`, { type: "error" });
      }
    } catch (error) {
      console.error("发布操作失败:", error);
      ElMessage.error("操作失败，请稍后重试");
    }
  }

  // 打开SCADA编辑器
  function openProject(row: ScadaProjectItem) {
    try {
      // 检查项目ID是否有效
      if (!row.SnowId) {
        ElMessage.warning("项目ID无效，无法打开编辑器");
        return;
      }

      // 使用动态路由导航到SCADA编辑器
      router.push({
        name: "ScadaFuxaEditor",
        params: { id: row.SnowId.toString() }
      });
      //router.push({
      //  path: `/scada/editor/${row.SnowId}`
      //});
    } catch (error) {
      console.error("打开编辑器失败:", error);
      ElMessage.error("打开编辑器失败，请稍后重试");
    }
  }

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

  /** 当CheckBox选择项发生变化时会触发该事件 */
  function handleSelectionChange(val: ScadaProjectItem[]) {
    console.log("🚀 ~ handleSelectionChange ~ val:", val);
    selectedNum.value = val.length;
    selectedRows.value = val;
    // 重置表格高度
    tableRef.value.setAdaptive();
  }

  /** 取消选择 */
  function onSelectionCancel() {
    selectedNum.value = 0;
    selectedRows.value = [];
    tableRef.value.getTableRef().clearSelection();
  }

  async function onSearch() {
    loading.value = true;
    const params: QueryTableParams = {
      page: pagination.currentPage,
      pagesize: pagination.pageSize,
      sconlist: []
    };

    if (form.keyword !== "") {
      params.sconlist.push({
        ParamName: "ProjectName",
        ParamType: "like",
        ParamValue: form.keyword
      });
    }

    // 如果有状态筛选，添加状态条件
    if (form.status !== "") {
      params.sconlist.push({
        ParamName: "ProjectStatus",
        ParamType: "=",
        ParamValue: form.status
      });
    }

    const data = await getListByPage(params);
    if (data.Status) {
      dataList.value = JSON.parse(data.Result);
      pagination.total = data.Total;
      pagination.pageSize = params.pagesize;
      pagination.currentPage = params.page;
    }
    setTimeout(() => {
      loading.value = false;
    }, 500);
  }

  const resetForm = formEl => {
    if (!formEl) return;
    formEl.resetFields();
    onSearch();
  };

  async function openDialog(title = "新增", row?: any) {
    const formData: ScadaProjectFormItemProps = {
      title,
      SnowId: 0,
      ProjectName: "",
      ProjectDesc: "",
      ProjectStatus: 0, // 0:未发布 1:发布
      Thumbnail: "",
      ProjectDefault: 0, // 0:未设置 1:默认
      ExpandJson: ""
    };

    if (row && row.SnowId) {
      Object.assign(formData, row);
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
      beforeCancel: done => {
        done(); // 直接关闭弹框，不做其他操作
      },
      contentRenderer: () =>
        h(editForm, { formInline: formData, ref: formRef }),
      beforeSure: (done, { options }) => {
        const FormRef = formRef.value.getRef();
        const curData = options.props.formInline;

        FormRef.validate(async valid => {
          if (valid) {
            delete curData.title;
            const data = await saveBatch([curData]);
            if (data.Status) {
              message(`${title}SCADA项目成功`, { type: "success" });
              done(); // 关闭弹框
              onSearch(); // 刷新表格数据
            } else {
              message(data.Message, { type: "error" });
            }
          } else {
            // 验证失败时提示用户
            message("表单验证失败，请检查输入", { type: "warning" });
            return false; // 阻止对话框关闭
          }
        });
      }
    });
  }

  onMounted(async () => {
    onSearch();
  });

  // 批量删除功能
  async function onbatchDel() {
    if (selectedRows.value.length === 0) {
      ElMessage.warning("请先选择要删除的项目");
      return;
    }

    try {
      const ids = selectedRows.value.map(row => row.SnowId.toString());
      const promises = ids.map(id => deleteByPk(id));
      const results = await Promise.all(promises);

      const successCount = results.filter(result => result.Status).length;
      const failCount = results.length - successCount;

      if (successCount > 0) {
        message(
          `成功删除 ${successCount} 个项目${failCount > 0 ? `，失败 ${failCount} 个` : ""}`,
          {
            type: successCount === results.length ? "success" : "warning"
          }
        );
        onSearch();
        onSelectionCancel();
      } else {
        message("删除失败", { type: "error" });
      }
    } catch (error) {
      console.error("批量删除失败:", error);
      message("批量删除失败，请稍后重试", { type: "error" });
    }
  }

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
    handlePublish,
    openProject,
    onSelectionCancel,
    onbatchDel
  };
}
