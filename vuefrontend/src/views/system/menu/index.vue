<script setup lang="ts">
import { ref } from "vue";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { PureTableBar } from "@/components/RePureTableBar";
import { useSysMenu } from "./utils/hook";
import AddFill from "~icons/ri/add-circle-line";
import Refresh from "~icons/ep/refresh";
import EditPen from "~icons/ep/edit-pen";
import Delete from "~icons/ep/delete";
import Menu from "~icons/ep/menu";

defineOptions({
  name: "SystemMenu"
});

const tableRef = ref();
const title = "菜单";

const { loading, columns, dataList, onSearch, openDialog, openBind, handleDelete } =
  useSysMenu();
</script>

<template>
  <div class="main">
    <div class="flex-1">
      <PureTableBar
        :title="title + '管理（全局菜单目录）'"
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
          <el-button :icon="useRenderIcon(Refresh)" @click="onSearch">
            刷新
          </el-button>
        </template>
        <template v-slot="{ size, dynamicColumns }">
          <pure-table
            ref="tableRef"
            adaptive
            row-key="MenuId"
            default-expand-all
            align-whole="left"
            table-layout="auto"
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
                :icon="useRenderIcon(EditPen)"
                @click="openDialog('修改', row)"
              >
                修改
              </el-button>
              <el-button
                class="reset-margin"
                link
                type="success"
                :size="size"
                :icon="useRenderIcon(Menu)"
                @click="openBind(row)"
              >
                绑定按钮
              </el-button>
              <el-popconfirm
                :title="`删除【${row.MenuName}】及其子菜单，确定吗？`"
                width="240"
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
:deep(.el-button:focus-visible) {
  outline: none;
}
</style>
