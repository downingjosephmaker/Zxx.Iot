<script setup lang="ts">
import { h, ref, reactive, watch, onMounted } from "vue";
import { ElTag } from "element-plus";
import type { PaginationProps } from "@pureadmin/table";
import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import type { QueryTableParams } from "@/api/type";
import {
  getListByPage,
  saveBatch,
  deleteByPk,
  type ProductCommandItem
} from "@/api/iot/command";
import type { ProductCommandFormItemProps } from "@/views/iot/command/utils/types";
import editForm from "@/views/iot/command/form.vue";
import AddFill from "~icons/ri/add-circle-line";
import EditPen from "~icons/ep/edit-pen";
import Delete from "~icons/ep/delete";

defineOptions({
  name: "ProductCommandsTab"
});

const props = defineProps<{
  /** 所属产品类型编码，为空时不发起查询 */
  typecode: string;
}>();

const ModuleTitle = "产品命令";
const formRef = ref();
const loading = ref(false);
const dataList = ref<ProductCommandItem[]>([]);
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
    label: "操作",
    fixed: "right",
    width: 160,
    slot: "operation"
  }
];

async function onSearch() {
  if (!props.typecode) {
    dataList.value = [];
    pagination.total = 0;
    return;
  }
  loading.value = true;
  const params: QueryTableParams = {
    page: pagination.currentPage,
    pagesize: pagination.pageSize,
    sconlist: [
      {
        ParamName: "DeviceTypeCode",
        ParamType: "=",
        ParamValue: props.typecode
      }
    ]
  };
  const data = await getListByPage(params);
  if (data.Status) {
    dataList.value = JSON.parse(data.Result);
    pagination.total = data.Total;
  } else {
    message(data.Message, { type: "error" });
  }
  loading.value = false;
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

async function openDialog(title = "新增", row?: ProductCommandItem) {
  const formData: ProductCommandFormItemProps = {
    title,
    SnowId: 0,
    DeviceTypeCode: props.typecode,
    CommandName: "",
    ClassName: "",
    ParamSchema: "",
    ConTemplate: "",
    NeedConfirm: false,
    IsEnable: true
  };
  if (row && row.SnowId) {
    const {
      SnowId,
      DeviceTypeCode,
      CommandName,
      ClassName,
      ParamSchema,
      ConTemplate,
      NeedConfirm,
      IsEnable
    } = row;
    Object.assign(formData, {
      SnowId,
      DeviceTypeCode: DeviceTypeCode ?? props.typecode,
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

watch(
  () => props.typecode,
  () => {
    pagination.currentPage = 1;
    onSearch();
  }
);

onMounted(() => {
  onSearch();
});
</script>

<template>
  <div class="product-commands-tab">
    <div class="mb-2 flex justify-end">
      <el-button
        type="primary"
        :icon="useRenderIcon(AddFill)"
        :disabled="!props.typecode"
        @click="openDialog()"
      >
        新增命令
      </el-button>
    </div>
    <el-empty
      v-if="!loading && dataList.length === 0"
      :description="props.typecode ? '该产品暂无命令' : '请先选择产品'"
    />
    <pure-table
      v-else
      showOverflowTooltip
      align-whole="left"
      table-layout="auto"
      :loading="loading"
      :data="dataList"
      :columns="columns"
      :pagination="pagination"
      :header-cell-style="{
        background: 'var(--el-fill-color-light)',
        color: 'var(--el-text-color-primary)'
      }"
      @page-size-change="handleSizeChange"
      @page-current-change="handleCurrentChange"
    >
      <template #operation="{ row }">
        <el-button
          class="reset-margin"
          link
          type="primary"
          :icon="useRenderIcon(EditPen)"
          @click="openDialog('修改', row)"
        >
          修改
        </el-button>
        <el-popconfirm
          :title="`确定要删除命令 ${row.CommandName} 吗？`"
          @confirm="handleDelete(row)"
        >
          <template #reference>
            <el-button
              class="reset-margin"
              link
              type="danger"
              :icon="useRenderIcon(Delete)"
            >
              删除
            </el-button>
          </template>
        </el-popconfirm>
      </template>
    </pure-table>
  </div>
</template>

<style scoped>
.product-commands-tab {
  padding: 4px 0;
}
</style>
