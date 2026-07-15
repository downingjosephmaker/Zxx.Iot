<script setup lang="ts">
import { ref, reactive, onMounted } from "vue";
import { useRouter, useRoute } from "vue-router";
import { ElMessage, ElMessageBox, type FormInstance } from "element-plus";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { PureTableBar } from "@/components/RePureTableBar";
import { useScadaProject } from "./utils/hook";
import { Auth } from "@/components/ReAuth";
import useUtils from "@/utils/utils";
import dayjs from "dayjs";
import type { ScadaProjectItem } from "./utils/types";
import type { ProjectKind } from "@/api/scada/project/index";
import AddFill from "~icons/ri/add-circle-line";
import Search from "~icons/ep/search";
import Refresh from "~icons/ep/refresh";
import EditPen from "~icons/ep/edit-pen";
import Delete from "~icons/ep/delete";

const router = useRouter();
const route = useRoute();
const formRef = ref<FormInstance>();
const tableRef = ref();
// 项目类型由路由 meta 决定：同一个页面同时服务组态项目与报表项目两个入口。
// meta.projectKind 由菜单 meta_json 下发；一旦缺失(前后端构建不同步 / 旧后端未平铺 extra /
// 授权菜单未带 meta)会静默塌成默认 scada，两个列表就"没分开"。此处按路由 path 兜底:
// 报表入口 path 恒以 /report 开头，据此回落到 dash，保证任何构建组合下两个列表都分离。
const kind: ProjectKind =
  (route.meta?.projectKind as ProjectKind) ??
  (route.path.startsWith("/report") ? "dash" : "scada");
const title = kind === "dash" ? "报表项目" : "组态项目";

// 使用hook获取表格管理功能
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
  handleDelete,
  handlePublish,
  openProject,
  onSelectionCancel,
  onbatchDel
} = useScadaProject(tableRef, kind);

// 导入项目功能
const importProject = () => {
  ElMessage.info("导入项目功能开发中，敬请期待");
};
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
        <el-form-item label="项目名称" prop="ProjectName">
          <el-input
            v-model="searchForm.keyword"
            placeholder="请输入项目名称"
            clearable
            class="!w-[180px]"
          />
        </el-form-item>
        <el-form-item label="项目状态" prop="status">
          <el-select
            v-model="searchForm.status"
            placeholder="请选择状态"
            clearable
            class="!w-[120px]"
          >
            <el-option label="未发布" value="0" />
            <el-option label="已发布" value="1" />
          </el-select>
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
            <el-popconfirm title="是否确认删除?" @confirm="onbatchDel">
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
                  type="primary"
                  :size="size"
                  :icon="useRenderIcon(EditPen)"
                  @click="openProject(row)"
                >
                  打开
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
                  :title="`确定要${row.ProjectStatus === 0 ? '发布' : '取消发布'}该项目吗？`"
                  @confirm="handlePublish(row)"
                >
                  <template #reference>
                    <el-button
                      class="reset-margin"
                      link
                      :type="row.ProjectStatus === 0 ? 'success' : 'warning'"
                      :size="size"
                      :icon="
                        useRenderIcon(
                          row.ProjectStatus === 0 ? 'ep:upload' : 'ep:download'
                        )
                      "
                    >
                      {{ row.ProjectStatus === 0 ? "发布" : "取消发布" }}
                    </el-button>
                  </template>
                </el-popconfirm>
            
                <el-popconfirm
                  :title="`确定要删除项目 ${row.ProjectName} 吗？`"
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
