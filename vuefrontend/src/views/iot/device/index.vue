<script setup lang="ts">
import { ref } from "vue";
import { type FormInstance } from "element-plus";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { PureTableBar } from "@/components/RePureTableBar";
import { useDeviceInfo } from "./utils/hook";
import SimDialog from "./sim-dialog.vue";
import AddFill from "~icons/ri/add-circle-line";
import Search from "~icons/ep/search";
import Refresh from "~icons/ep/refresh";
import EditPen from "~icons/ep/edit-pen";
import Delete from "~icons/ep/delete";
import Promotion from "~icons/ep/promotion";
import Upload from "~icons/ep/upload";
import VideoPlay from "~icons/ep/video-play";
import Switch from "~icons/ep/switch";

defineOptions({
  name: "IotDevice"
});

const formRef = ref<FormInstance>();
const tableRef = ref();
const title = "设备";

const {
  form: searchForm,
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
  openCommandDialog,
  openImportDialog,
  handleDelete,
  handleToggleCollection,
  simDialogRef,
  handleOpenSim,
  onbatchDel,
  onSelectionCancel
} = useDeviceInfo(tableRef);
</script>

<template>
  <div class="main">
    <div class="flex-1">
      <el-form
        ref="formRef"
        :inline="true"
        :model="searchForm"
        class="search-form bg-bg_color w-[99/100] pl-8 pt-[12px]"
      >
        <el-form-item label="设备名称" prop="keyword">
          <el-input
            v-model="searchForm.keyword"
            placeholder="请输入设备名称"
            clearable
            class="!w-[180px]"
            @keyup.enter="onSearch"
          />
        </el-form-item>
        <el-form-item label="产品编码" prop="typecode">
          <el-input
            v-model="searchForm.typecode"
            placeholder="产品类型编码"
            clearable
            class="!w-[160px]"
            @keyup.enter="onSearch"
          />
        </el-form-item>
        <el-form-item>
          <el-button
            type="primary"
            :icon="useRenderIcon(Search)"
            :loading="loading"
            @click="onSearch"
          >
            搜索
          </el-button>
          <el-button :icon="useRenderIcon(Refresh)" @click="resetForm(formRef)">
            重置
          </el-button>
        </el-form-item>
      </el-form>
      <PureTableBar
        :title="title + '管理'"
        :columns="columns"
        @refresh="onSearch"
      >
        <template #buttons>
          <el-button
            type="primary"
            :icon="useRenderIcon(AddFill)"
            @click="openDialog()"
          >
            新增{{ title }}
          </el-button>
          <el-button
            type="primary"
            plain
            :icon="useRenderIcon(Upload)"
            @click="openImportDialog()"
          >
            Excel导入
          </el-button>
        </template>
        <template v-slot="{ size, dynamicColumns }">
          <div
            v-if="selectedNum > 0"
            v-motion-fade
            class="bg-[var(--el-fill-color-light)] w-full h-[46px] mb-2 pl-4 flex items-center"
          >
            <div class="flex-auto">
              <span
                style="font-size: var(--el-font-size-base)"
                class="text-[rgba(42,46,54,0.5)] dark:text-[rgba(220,220,242,0.5)]"
              >
                已选 {{ selectedNum }} 项
              </span>
              <el-button type="primary" text @click="onSelectionCancel">
                取消选择
              </el-button>
            </div>
            <el-popconfirm
              title="删除设备将连同其子设备与点位配置一并删除，是否确认?"
              width="280"
              @confirm="onbatchDel"
            >
              <template #reference>
                <el-button type="danger" text class="mr-1">
                  批量删除
                </el-button>
              </template>
            </el-popconfirm>
          </div>
          <pure-table
            ref="tableRef"
            adaptive
            showOverflowTooltip
            align-whole="left"
            table-layout="auto"
            :loading="loading"
            :size="size"
            :data="dataList"
            :columns="dynamicColumns"
            :pagination="pagination"
            :paginationSmall="size === 'small' ? true : false"
            :header-cell-style="{
              background: 'var(--el-fill-color-light)',
              color: 'var(--el-text-color-primary)'
            }"
            @selection-change="handleSelectionChange"
            @page-size-change="handleSizeChange"
            @page-current-change="handleCurrentChange"
          >
            <template #operation="{ row }">
              <el-button
                class="reset-margin"
                link
                type="success"
                :size="size"
                :icon="useRenderIcon(Promotion)"
                @click="openCommandDialog(row)"
              >
                指令
              </el-button>
              <el-button
                class="reset-margin"
                link
                :type="row.IsCollection === 1 ? 'danger' : 'success'"
                :size="size"
                :icon="useRenderIcon(Switch)"
                @click="handleToggleCollection(row)"
              >
                {{ row.IsCollection === 1 ? "停采" : "启采" }}
              </el-button>
              <el-button
                class="reset-margin"
                link
                type="primary"
                :size="size"
                :icon="useRenderIcon(VideoPlay)"
                @click="handleOpenSim(row)"
              >
                模拟
              </el-button>
              <el-button
                class="reset-margin"
                link
                type="primary"
                :size="size"
                :icon="useRenderIcon(EditPen)"
                @click="openDialog('修改', row)"
              >
                修改
              </el-button>
              <el-popconfirm
                :title="`删除 ${row.DeviceName} 将连同其子设备与点位配置一并删除，确定吗？`"
                width="280"
                @confirm="handleDelete(row)"
              >
                <template #reference>
                  <el-button
                    class="reset-margin"
                    link
                    type="danger"
                    :size="size"
                    :icon="useRenderIcon(Delete)"
                  >
                    删除
                  </el-button>
                </template>
              </el-popconfirm>
            </template>
          </pure-table>
        </template>
      </PureTableBar>
    </div>
    <SimDialog ref="simDialogRef" />
  </div>
</template>

<style scoped lang="scss">
:deep(.el-dropdown-menu__item i) {
  margin: 0;
}

:deep(.el-button:focus-visible) {
  outline: none;
}

.search-form {
  :deep(.el-form-item) {
    margin-bottom: 12px;
  }
}
</style>
