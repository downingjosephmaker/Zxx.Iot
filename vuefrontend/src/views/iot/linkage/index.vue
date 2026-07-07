<script setup lang="ts">
import { ref } from "vue";
import { type FormInstance } from "element-plus";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { PureTableBar } from "@/components/RePureTableBar";
import { useLinkageRule } from "./utils/hook";
import AddFill from "~icons/ri/add-circle-line";
import Search from "~icons/ep/search";
import Refresh from "~icons/ep/refresh";
import EditPen from "~icons/ep/edit-pen";
import Delete from "~icons/ep/delete";
import VideoPlay from "~icons/ep/video-play";
import DataAnalysis from "~icons/ep/data-analysis";

defineOptions({
  name: "IotLinkage"
});

const formRef = ref<FormInstance>();
const tableRef = ref();
const title = "规则联动";

const {
  form: searchForm,
  loading,
  columns,
  dataList,
  selectedNum,
  pagination,
  metricsVisible,
  metricsLoading,
  metricsList,
  dryrunVisible,
  dryrunData,
  handleSizeChange,
  handleCurrentChange,
  handleSelectionChange,
  onSearch,
  resetForm,
  openDialog,
  handleDelete,
  onbatchDel,
  onSelectionCancel,
  openMetrics,
  handleDryRun
} = useLinkageRule(tableRef);
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
        <el-form-item label="规则名称" prop="keyword">
          <el-input
            v-model="searchForm.keyword"
            placeholder="请输入规则名称"
            clearable
            class="!w-[180px]"
          />
        </el-form-item>
        <el-form-item label="触发类型" prop="triggertype">
          <el-select
            v-model="searchForm.triggertype"
            placeholder="请选择触发类型"
            clearable
            class="!w-[150px]"
          >
            <el-option label="点位变化" value="1" />
            <el-option label="告警产生" value="2" />
            <el-option label="告警恢复" value="3" />
            <el-option label="定时cron" value="4" />
            <el-option label="设备上线" value="5" />
            <el-option label="设备离线" value="6" />
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
          <el-button :icon="useRenderIcon(DataAnalysis)" @click="openMetrics()">
            漏斗指标
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
                @click="openDialog('修改', row)"
              >
                修改
              </el-button>
              <el-button
                class="reset-margin"
                link
                type="warning"
                :size="size"
                :icon="useRenderIcon(VideoPlay)"
                @click="handleDryRun(row)"
              >
                试运行
              </el-button>
              <el-popconfirm
                :title="`确定要删除规则 ${row.RuleName} 吗？`"
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

    <el-dialog
      v-model="metricsVisible"
      title="规则漏斗指标（进程内累计）"
      width="760px"
      destroy-on-close
    >
      <el-table v-loading="metricsLoading" :data="metricsList" border stripe>
        <el-table-column prop="RuleName" label="规则" min-width="150" />
        <el-table-column
          prop="Matched"
          label="触发命中"
          width="100"
          align="right"
        />
        <el-table-column
          prop="Passed"
          label="条件通过"
          width="100"
          align="right"
        />
        <el-table-column
          prop="Failed"
          label="条件未过"
          width="100"
          align="right"
        />
        <el-table-column
          prop="ActionOk"
          label="动作成功"
          width="100"
          align="right"
        />
        <el-table-column
          prop="ActionFail"
          label="动作失败"
          width="100"
          align="right"
        />
      </el-table>
      <p v-if="metricsList.length === 0 && !metricsLoading" class="metrics-empty">
        暂无指标数据（规则触发后开始累计，服务重启清零）
      </p>
    </el-dialog>

    <el-dialog
      v-model="dryrunVisible"
      title="试运行结果（干跑，未执行动作）"
      width="560px"
      destroy-on-close
    >
      <template v-if="dryrunData">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="规则">
            {{ dryrunData.RuleName || "-" }}
          </el-descriptions-item>
          <el-descriptions-item label="存在且启用">
            <el-tag :type="dryrunData.Found ? 'success' : 'danger'" effect="light">
              {{ dryrunData.Found ? "是" : "否" }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="时间窗内">
            <el-tag
              :type="dryrunData.InWindow ? 'success' : 'info'"
              effect="light"
            >
              {{ dryrunData.InWindow ? "是" : "否" }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="条件求值">
            <el-tag
              :type="dryrunData.ConditionPass ? 'success' : 'danger'"
              effect="light"
            >
              {{ dryrunData.ConditionPass ? "通过" : "未过" }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="冷却剩余">
            {{ dryrunData.CooldownRemainSeconds }} 秒
          </el-descriptions-item>
        </el-descriptions>
        <template v-if="Object.keys(dryrunData.Variables || {}).length > 0">
          <p class="dryrun-vars-title">条件变量快照</p>
          <el-table
            :data="
              Object.entries(dryrunData.Variables).map(([key, value]) => ({
                key,
                value
              }))
            "
            border
            size="small"
            max-height="240"
          >
            <el-table-column prop="key" label="变量" min-width="150" />
            <el-table-column prop="value" label="当前值" width="140" align="right" />
          </el-table>
        </template>
      </template>
    </el-dialog>
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

.metrics-empty {
  margin-top: 12px;
  color: var(--el-text-color-secondary);
  text-align: center;
}

.dryrun-vars-title {
  margin: 12px 0 8px;
  font-weight: 600;
}
</style>
