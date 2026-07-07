<script setup lang="ts">
import { ref } from "vue";
import { type FormInstance } from "element-plus";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { PureTableBar } from "@/components/RePureTableBar";
import { useDeviceType } from "./utils/hook";
import AddFill from "~icons/ri/add-circle-line";
import Search from "~icons/ep/search";
import Refresh from "~icons/ep/refresh";
import EditPen from "~icons/ep/edit-pen";
import Delete from "~icons/ep/delete";

defineOptions({
  name: "IotProduct"
});

const formRef = ref<FormInstance>();
const title = "产品类型";

const {
  form: searchForm,
  loading,
  columns,
  dataList,
  onSearch,
  resetForm,
  openDialog,
  handleDelete
} = useDeviceType();
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
        <el-form-item label="关键字" prop="keyword">
          <el-input
            v-model="searchForm.keyword"
            placeholder="类型名称/编码"
            clearable
            class="!w-[200px]"
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
        </template>
        <template v-slot="{ size, dynamicColumns }">
          <pure-table
            adaptive
            showOverflowTooltip
            align-whole="left"
            row-key="TypeCode"
            default-expand-all
            :loading="loading"
            :size="size"
            :data="dataList"
            :columns="dynamicColumns"
            :header-cell-style="{
              background: 'var(--el-fill-color-light)',
              color: 'var(--el-text-color-primary)'
            }"
          >
            <template #operation="{ row }">
              <el-button
                class="reset-margin"
                link
                type="primary"
                :size="size"
                :icon="useRenderIcon(AddFill)"
                @click="openDialog('新增', undefined, row)"
              >
                子类
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
                :title="`删除 ${row.TypeName} 将连同其所有子类型一并删除，确定吗？`"
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
