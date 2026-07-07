<script setup lang="ts">
import { ref } from "vue";
import { type FormInstance } from "element-plus";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { PureTableBar } from "@/components/RePureTableBar";
import { useProtocolScript } from "./utils/hook";
import AddFill from "~icons/ri/add-circle-line";
import Search from "~icons/ep/search";
import Refresh from "~icons/ep/refresh";
import EditPen from "~icons/ep/edit-pen";
import Delete from "~icons/ep/delete";
import VideoPlay from "~icons/ep/video-play";
import Clock from "~icons/ep/clock";

defineOptions({
  name: "IotScript"
});

const formRef = ref<FormInstance>();
const tableRef = ref();
const title = "协议脚本";

const {
  form: searchForm,
  loading,
  columns,
  dataList,
  selectedNum,
  pagination,
  historyVisible,
  historyLoading,
  historyList,
  historyContent,
  debugVisible,
  debugRunning,
  debugForm,
  debugResult,
  handleSizeChange,
  handleCurrentChange,
  handleSelectionChange,
  onSearch,
  resetForm,
  openDialog,
  handleDelete,
  onbatchDel,
  onSelectionCancel,
  openHistory,
  viewHistoryContent,
  editFromHistory,
  openDebug,
  runDebug
} = useProtocolScript(tableRef);
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
        <el-form-item label="脚本名称" prop="keyword">
          <el-input
            v-model="searchForm.keyword"
            placeholder="请输入脚本名称"
            clearable
            class="!w-[180px]"
          />
        </el-form-item>
        <el-form-item label="产品编码" prop="typecode">
          <el-input
            v-model="searchForm.typecode"
            placeholder="挂靠产品类型编码"
            clearable
            class="!w-[160px]"
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
          <el-button :icon="useRenderIcon(VideoPlay)" @click="openDebug()">
            调试台
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
                @click="openDebug(row)"
              >
                调试
              </el-button>
              <el-button
                class="reset-margin"
                link
                type="info"
                :size="size"
                :icon="useRenderIcon(Clock)"
                @click="openHistory(row)"
              >
                历史
              </el-button>
              <el-popconfirm
                :title="`确定要删除脚本 ${row.ScriptName} 吗？（连带删除版本历史）`"
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
      v-model="debugVisible"
      :title="`脚本调试台：${debugForm.ScriptName}（干跑，无副作用）`"
      width="900px"
      top="4vh"
      destroy-on-close
    >
      <el-form label-width="100px">
        <el-form-item label="脚本内容">
          <el-input
            v-model="debugForm.ScriptContent"
            type="textarea"
            :rows="12"
            class="script-editor"
            placeholder="草稿内容优先于库内内容，未保存也可调试"
          />
        </el-form-item>
        <el-form-item label="调试函数">
          <el-radio-group v-model="debugForm.FuncName">
            <el-radio-button value="decode">decode 上行解码</el-radio-button>
            <el-radio-button value="encode">encode 下行编码</el-radio-button>
            <el-radio-button value="splitFrames">splitFrames 帧定界</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item
          v-if="debugForm.FuncName !== 'encode'"
          label="输入帧hex"
        >
          <el-input
            v-model="debugForm.InputHex"
            placeholder="如 01030400640065B8F0"
            clearable
          />
        </el-form-item>
        <el-form-item v-if="debugForm.FuncName === 'encode'" label="命令JSON">
          <el-input
            v-model="debugForm.InputJson"
            type="textarea"
            :rows="2"
            placeholder='如 {"paramCode":"switch","paramValue":"1"}'
          />
        </el-form-item>
        <el-form-item label="上下文JSON">
          <el-input
            v-model="debugForm.ContextJson"
            placeholder='模拟上下文，如 {"deviceKey":"dtu001"}'
            clearable
          />
        </el-form-item>
        <el-form-item>
          <el-button
            type="primary"
            :loading="debugRunning"
            :icon="useRenderIcon(VideoPlay)"
            @click="runDebug"
          >
            运行
          </el-button>
        </el-form-item>
      </el-form>

      <template v-if="debugResult">
        <el-divider content-position="left">
          运行结果
          <el-tag
            :type="debugResult.Success ? 'success' : 'danger'"
            effect="light"
            class="ml-2"
          >
            {{ debugResult.Success ? "成功" : "失败" }}
          </el-tag>
          <span class="debug-elapsed">{{ debugResult.ElapsedMs }} ms</span>
        </el-divider>
        <p v-if="debugResult.Error" class="debug-error">
          {{ debugResult.Error }}
        </p>
        <template v-if="debugResult.ResultJson">
          <p class="debug-section-title">返回值</p>
          <pre class="debug-pre">{{ debugResult.ResultJson }}</pre>
        </template>
        <template v-if="debugResult.ConsoleLogs?.length > 0">
          <p class="debug-section-title">
            console 输出（{{ debugResult.ConsoleLogs.length }} 条）
          </p>
          <pre class="debug-pre debug-console">{{
            debugResult.ConsoleLogs.join("\n")
          }}</pre>
        </template>
      </template>
    </el-dialog>

    <el-dialog
      v-model="historyVisible"
      title="脚本版本历史"
      width="760px"
      destroy-on-close
    >
      <el-table
        v-loading="historyLoading"
        :data="historyList"
        border
        stripe
        max-height="260"
        highlight-current-row
        @current-change="viewHistoryContent"
      >
        <el-table-column label="版本" width="80" align="center">
          <template #default="{ row }">v{{ row.Version }}</template>
        </el-table-column>
        <el-table-column prop="CreateTime" label="保存时间" width="170" />
        <el-table-column prop="CreateName" label="保存人" min-width="100" />
        <el-table-column label="操作" width="150" align="center">
          <template #default="{ row }">
            <el-button link type="primary" @click="editFromHistory(row)">
              以此版本编辑
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <template v-if="historyContent">
        <p class="debug-section-title">版本内容（点击行查看）</p>
        <pre class="debug-pre">{{ historyContent }}</pre>
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

.script-editor :deep(textarea) {
  font-family: Consolas, Monaco, "Courier New", monospace;
  font-size: 13px;
  line-height: 1.5;
}

.debug-elapsed {
  margin-left: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.debug-error {
  margin-bottom: 8px;
  color: var(--el-color-danger);
}

.debug-section-title {
  margin: 8px 0 4px;
  font-weight: 600;
}

.debug-pre {
  max-height: 240px;
  padding: 10px;
  overflow: auto;
  font-size: 12px;
  background: var(--el-fill-color-light);
  border-radius: 6px;
  white-space: pre-wrap;
  word-break: break-all;
}

.debug-console {
  color: var(--el-text-color-regular);
}
</style>
