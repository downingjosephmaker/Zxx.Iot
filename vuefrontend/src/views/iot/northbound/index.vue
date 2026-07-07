<script setup lang="ts">
import { ref } from "vue";
import { type FormInstance } from "element-plus";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { PureTableBar } from "@/components/RePureTableBar";
import { useNorthboundSink } from "./utils/hook";
import AddFill from "~icons/ri/add-circle-line";
import Search from "~icons/ep/search";
import Refresh from "~icons/ep/refresh";
import EditPen from "~icons/ep/edit-pen";
import Delete from "~icons/ep/delete";
import View from "~icons/ep/view";
import Position from "~icons/ep/position";
import Odometer from "~icons/ep/odometer";

defineOptions({
  name: "IotNorthbound"
});

const formRef = ref<FormInstance>();
const tableRef = ref();
const title = "北向目的地";

const {
  form: searchForm,
  loading,
  columns,
  dataList,
  selectedNum,
  pagination,
  statusVisible,
  statusLoading,
  statusList,
  sampleVisible,
  sampleData,
  handleSizeChange,
  handleCurrentChange,
  handleSelectionChange,
  onSearch,
  resetForm,
  openDialog,
  handleDelete,
  onbatchDel,
  onSelectionCancel,
  openStatus,
  openSample,
  handleTestSend
} = useNorthboundSink(tableRef);
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
        <el-form-item label="目的地名称" prop="keyword">
          <el-input
            v-model="searchForm.keyword"
            placeholder="请输入目的地名称"
            clearable
            class="!w-[180px]"
          />
        </el-form-item>
        <el-form-item label="类型" prop="sinktype">
          <el-select
            v-model="searchForm.sinktype"
            placeholder="请选择类型"
            clearable
            class="!w-[150px]"
          >
            <el-option label="MQTT" value="1" />
            <el-option label="HTTP Webhook" value="2" />
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
          <el-button :icon="useRenderIcon(Odometer)" @click="openStatus()">
            队列水位
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
                type="primary"
                :size="size"
                :icon="useRenderIcon(View)"
                @click="openSample(row)"
              >
                样例
              </el-button>
              <el-popconfirm
                :title="`确定向 ${row.SinkName} 实发一条样例报文吗？`"
                @confirm="handleTestSend(row)"
              >
                <template #reference>
                  <el-button
                    class="reset-margin"
                    link
                    type="warning"
                    :size="size"
                    :icon="useRenderIcon(Position)"
                  >
                    测试
                  </el-button>
                </template>
              </el-popconfirm>
              <el-popconfirm
                :title="`确定要删除目的地 ${row.SinkName} 吗？`"
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
      v-model="statusVisible"
      title="转发队列水位"
      width="720px"
      destroy-on-close
    >
      <el-table v-loading="statusLoading" :data="statusList" border stripe>
        <el-table-column prop="SinkName" label="目的地" min-width="130" />
        <el-table-column label="在线" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.Online ? 'success' : 'danger'" effect="light">
              {{ row.Online ? "在线" : "离线" }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column
          prop="MemoryBacklog"
          label="内存积压"
          width="100"
          align="right"
        />
        <el-table-column
          prop="CacheBacklog"
          label="落盘积压"
          width="100"
          align="right"
        />
        <el-table-column
          prop="SentCount"
          label="累计发送"
          width="100"
          align="right"
        />
        <el-table-column
          prop="FailCount"
          label="累计失败"
          width="100"
          align="right"
        />
      </el-table>
    </el-dialog>

    <el-dialog
      v-model="sampleVisible"
      title="样例报文预览（干跑，不实际发送）"
      width="640px"
      destroy-on-close
    >
      <template v-if="sampleData">
        <p v-if="sampleData.SampleTopic" class="sample-topic">
          主题：{{ sampleData.SampleTopic }}
        </p>
        <pre class="sample-payload">{{ sampleData.SamplePayload }}</pre>
        <p v-if="!sampleData.Success" class="sample-error">
          {{ sampleData.Message }}
        </p>
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

.sample-topic {
  margin-bottom: 8px;
  font-weight: 600;
}

.sample-payload {
  max-height: 360px;
  padding: 12px;
  overflow: auto;
  font-size: 12px;
  background: var(--el-fill-color-light);
  border-radius: 6px;
  white-space: pre-wrap;
  word-break: break-all;
}

.sample-error {
  margin-top: 8px;
  color: var(--el-color-danger);
}
</style>
