<script setup lang="ts">
import { h, ref, reactive, watch } from "vue";
import { message } from "@/utils/message";
import { addDialog, closeDialog } from "@/components/ReDialog";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import type { QueryTableParams } from "@/api/type";
import {
  getListByPage,
  saveBatch,
  deleteByPk,
  type DeviceTypeParamItem,
  type StatusValueItem
} from "@/api/iot/typeparam";
import type { DeviceTypeParamFormItemProps } from "@/views/iot/typeparam/utils/types";
import editForm from "@/views/iot/typeparam/form.vue";
import jsonImport from "@/views/iot/typeparam/json-import.vue";
import AddFill from "~icons/ri/add-circle-line";
import Upload from "~icons/ep/upload";
import EditPen from "~icons/ep/edit-pen";
import Delete from "~icons/ep/delete";

defineOptions({
  name: "ProductPointsTab"
});

const props = defineProps<{
  /** 产品类型编码，点表按此隔离 */
  typecode: string;
}>();

const ModuleTitle = "点位";

/** Modbus采集功能码显示映射(0=不采集) */
const FUNC_CODE_LABELS: Record<number, string> = {
  0: "不采集",
  1: "FC01线圈",
  2: "FC02离散",
  3: "FC03保持",
  4: "FC04输入"
};

const loading = ref(false);
const dataList = ref<DeviceTypeParamItem[]>([]);
const formRef = ref();
const importRef = ref();
const pagination = reactive({
  total: 0,
  pageSize: 10,
  currentPage: 1
});

/** 读取状态值集合，兼容结构化ExpandStatusValues与仅StatusValues串两形态 */
function parseStatusValues(row?: DeviceTypeParamItem): StatusValueItem[] {
  if (row?.ExpandStatusValues?.length) return [...row.ExpandStatusValues];
  try {
    return row?.StatusValues ? JSON.parse(row.StatusValues) : [];
  } catch {
    return [];
  }
}

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

function handleSizeChange() {
  pagination.currentPage = 1;
  onSearch();
}

function handleCurrentChange() {
  onSearch();
}

function openDialog(title = "新增", row?: DeviceTypeParamItem) {
  const formData: DeviceTypeParamFormItemProps = {
    title,
    SnowId: row?.SnowId ?? 0,
    DeviceTypeCode: row?.DeviceTypeCode ?? props.typecode,
    SubChannel: row?.SubChannel ?? "总路",
    ParamCode: row?.ParamCode ?? "",
    ParamName: row?.ParamName ?? "",
    ParamTypeName: row?.ParamTypeName ?? "",
    ParamAddr: row?.ParamAddr ?? 0,
    ParamFormula: row?.ParamFormula ?? "",
    ValueType: row?.ValueType || "数值",
    ExpandStatusValues: parseStatusValues(row),
    ValueUnit: row?.ValueUnit ?? "",
    DecimalDigit: row?.DecimalDigit ?? 2,
    ParamMaxValue: row?.ParamMaxValue ?? 0,
    ParamMinValue: row?.ParamMinValue ?? 0,
    ParamChangeValue: row?.ParamChangeValue ?? 0,
    RangeFilterEnable: row?.RangeFilterEnable ?? false,
    AmplitudeFilterEnable: row?.AmplitudeFilterEnable ?? false,
    MaxAmplitudePercent: row?.MaxAmplitudePercent ?? 0,
    ContinuousFilterEnable: row?.ContinuousFilterEnable ?? false,
    MaxContinuousCount: row?.MaxContinuousCount ?? 3,
    IsShow: row?.IsShow ?? true,
    IsMainShow: row?.IsMainShow ?? false,
    IsSet: row?.IsSet ?? false,
    IsPeak: row?.IsPeak ?? false,
    IsReport: row?.IsReport ?? false,
    IsMapDefault: row?.IsMapDefault ?? false,
    IsPt: row?.IsPt ?? false,
    IsCt: row?.IsCt ?? false,
    IsCustomAlarm: row?.IsCustomAlarm ?? false,
    CollectFuncCode: row?.CollectFuncCode ?? 0,
    CollectDataType: row?.CollectDataType ?? "",
    CollectByteOrder: row?.CollectByteOrder ?? "",
    CollectBitOffset: row?.CollectBitOffset ?? -1,
    CollectRegLength: row?.CollectRegLength ?? 0,
    CollectWritable: row?.CollectWritable ?? false,
    CollectNodeId: row?.CollectNodeId ?? "",
    IsAlarmSource: row?.IsAlarmSource ?? false,
    AlarmConfigId: row?.AlarmConfigId ?? 0
  };

  addDialog({
    title: `${title}${ModuleTitle}`,
    props: {
      formInline: formData
    },
    width: "860px",
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
          // 数值型清空状态值集合(空列表使服务端将StatusValues写空)
          if (curData.ValueType !== "状态") curData.ExpandStatusValues = [];
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

/** 打开点表JSON导入弹窗（组态ZtTypeJson格式,产品编码预填当前产品） */
function openImportDialog() {
  addDialog({
    title: "JSON导入点表",
    width: "560px",
    draggable: true,
    closeOnClickModal: false,
    contentRenderer: () =>
      h(jsonImport, { ref: importRef, typecode: props.typecode }),
    footerButtons: [
      {
        label: "关闭",
        text: true,
        bg: true,
        btnClick: ({ dialog: { options, index } }) => {
          closeDialog(options, index);
        }
      },
      {
        label: "导入",
        type: "primary",
        text: true,
        bg: true,
        btnClick: async ({ dialog: { options, index } }) => {
          const ok = await importRef.value?.onImport();
          if (ok) {
            closeDialog(options, index);
            onSearch();
          }
        }
      }
    ]
  });
}

async function handleDelete(row: DeviceTypeParamItem) {
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
  },
  { immediate: true }
);
</script>

<template>
  <div class="points-tab">
    <el-empty
      v-if="!props.typecode"
      description="请先选择产品"
      :image-size="80"
    />
    <template v-else>
      <div class="toolbar">
        <el-button
          type="primary"
          :icon="useRenderIcon(AddFill)"
          @click="openDialog()"
        >
          新增点位
        </el-button>
        <el-button
          type="primary"
          plain
          :icon="useRenderIcon(Upload)"
          @click="openImportDialog()"
        >
          JSON导入
        </el-button>
        <span class="point-count">共 {{ pagination.total }} 个点位</span>
      </div>

      <el-table v-loading="loading" :data="dataList" border stripe>
        <el-table-column
          prop="ParamCode"
          label="参数编码"
          min-width="120"
          show-overflow-tooltip
        />
        <el-table-column
          prop="ParamName"
          label="参数名称"
          min-width="140"
          show-overflow-tooltip
        />
        <el-table-column label="值类型" width="90" align="center">
          <template #default="{ row }">
            <el-tag
              :type="row.ValueType === '状态' ? 'warning' : 'primary'"
              effect="light"
              size="small"
            >
              {{ row.ValueType || "数值" }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="功能码" width="100" align="center">
          <template #default="{ row }">
            {{
              FUNC_CODE_LABELS[row.CollectFuncCode ?? 0] ??
              `FC${row.CollectFuncCode}`
            }}
          </template>
        </el-table-column>
        <el-table-column
          prop="ParamAddr"
          label="地址"
          width="80"
          align="center"
        />
        <el-table-column label="单位" width="80" align="center">
          <template #default="{ row }">
            {{ row.ValueUnit || "-" }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
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
              :title="`确定要删除点位 ${row.ParamName} 吗？`"
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
        </el-table-column>
        <template #empty>
          <el-empty description="该产品暂无点表参数" :image-size="80" />
        </template>
      </el-table>

      <el-pagination
        v-model:current-page="pagination.currentPage"
        v-model:page-size="pagination.pageSize"
        class="pagination"
        background
        layout="total, sizes, prev, pager, next, jumper"
        :total="pagination.total"
        :page-sizes="[10, 20, 50, 100]"
        @size-change="handleSizeChange"
        @current-change="handleCurrentChange"
      />
    </template>
  </div>
</template>

<style scoped lang="scss">
.toolbar {
  display: flex;
  align-items: center;
  margin-bottom: 12px;

  .point-count {
    margin-left: auto;
    font-size: 13px;
    color: var(--el-text-color-secondary);
  }
}

.pagination {
  justify-content: flex-end;
  margin-top: 12px;
}
</style>
