<script setup lang="ts">
import { ref } from "vue";
import { type FormInstance } from "element-plus";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { PureTableBar } from "@/components/RePureTableBar";
import SchemaForm from "@/views/iot/components/SchemaForm.vue";
import { useSysPlugin } from "./utils/hook";
import Search from "~icons/ep/search";
import Refresh from "~icons/ep/refresh";
import Upload from "~icons/ep/upload";
import Setting from "~icons/ep/setting";
import Delete from "~icons/ep/delete";
import Document from "~icons/ep/document";
import VideoPlay from "~icons/ep/video-play";
import VideoPause from "~icons/ep/video-pause";

defineOptions({
  name: "IotPlugin"
});

const formRef = ref<FormInstance>();
const tableRef = ref();
const title = "插件";

const {
  form: searchForm,
  loading,
  columns,
  dataList,
  pagination,
  uploadVisible,
  uploading,
  uploadFile,
  configVisible,
  configLoading,
  configSaving,
  configRow,
  configSchema,
  configModel,
  configJsonText,
  schemaFormRef,
  schemaMode,
  detailVisible,
  detailRow,
  detailManifest,
  onSearch,
  resetForm,
  handleSizeChange,
  handleCurrentChange,
  handleEnable,
  handleDelete,
  openUpload,
  onUploadChange,
  onUploadRemove,
  handleUpload,
  openConfig,
  handleSaveConfig,
  openDetail
} = useSysPlugin(tableRef);
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
        <el-form-item label="插件名称" prop="keyword">
          <el-input
            v-model="searchForm.keyword"
            placeholder="请输入插件名称"
            clearable
            class="!w-[180px]"
          />
        </el-form-item>
        <el-form-item label="状态" prop="pluginstatus">
          <el-select
            v-model="searchForm.pluginstatus"
            placeholder="请选择状态"
            clearable
            class="!w-[150px]"
          >
            <el-option label="启用" value="1" />
            <el-option label="停用" value="0" />
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
            :icon="useRenderIcon(Upload)"
            @click="openUpload()"
          >
            上传{{ title }}
          </el-button>
        </template>
        <template v-slot="{ size, dynamicColumns }">
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
            @page-size-change="handleSizeChange"
            @page-current-change="handleCurrentChange"
          >
            <template #operation="{ row }">
              <el-button
                class="reset-margin"
                link
                type="primary"
                :size="size"
                :icon="useRenderIcon(Setting)"
                @click="openConfig(row)"
              >
                配置
              </el-button>
              <el-popconfirm
                :title="
                  row.PluginStatus === 1
                    ? `确定停用并卸载插件 ${row.PluginName} 吗？采集将立即停止`
                    : `确定启用并加载插件 ${row.PluginName} 吗？`
                "
                @confirm="handleEnable(row)"
              >
                <template #reference>
                  <el-button
                    class="reset-margin"
                    link
                    :type="row.PluginStatus === 1 ? 'warning' : 'success'"
                    :size="size"
                    :icon="
                      useRenderIcon(
                        row.PluginStatus === 1 ? VideoPause : VideoPlay
                      )
                    "
                  >
                    {{ row.PluginStatus === 1 ? "停用" : "启用" }}
                  </el-button>
                </template>
              </el-popconfirm>
              <el-button
                class="reset-margin"
                link
                type="primary"
                :size="size"
                :icon="useRenderIcon(Document)"
                @click="openDetail(row)"
              >
                详情
              </el-button>
              <el-popconfirm
                :title="`确定删除插件 ${row.PluginName} 吗？运行实例将被卸载`"
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
      v-model="uploadVisible"
      title="上传插件(zip整包或单DLL)"
      width="520px"
      destroy-on-close
    >
      <el-upload
        drag
        :auto-upload="false"
        :limit="1"
        accept=".zip,.dll"
        :on-change="onUploadChange"
        :on-remove="onUploadRemove"
      >
        <div class="el-upload__text">
          将插件包拖到此处，或<em>点击选择</em>
        </div>
        <template #tip>
          <div class="el-upload__tip">
            S7/OpcUa 等带依赖插件请上传发布目录打成的 zip（含依赖 DLL）；
            zip 内插件 Guid 决定登记/更新对象，新插件默认停用，
            已启用插件上传后即时热更新。
          </div>
        </template>
      </el-upload>
      <template #footer>
        <el-button @click="uploadVisible = false">取消</el-button>
        <el-button
          type="primary"
          :loading="uploading"
          :disabled="!uploadFile"
          @click="handleUpload"
        >
          上传
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="configVisible"
      :title="`插件配置 - ${configRow?.PluginName ?? ''}`"
      width="560px"
      destroy-on-close
    >
      <div v-loading="configLoading">
        <SchemaForm
          v-if="schemaMode"
          ref="schemaFormRef"
          :schema="configSchema"
          :model="configModel"
          label-width="130px"
        />
        <template v-else>
          <el-input
            v-model="configJsonText"
            type="textarea"
            :rows="10"
            class="json-editor"
            placeholder='插件配置JSON，如 {"DeviceTypeCodes":"dianbiao","HeartSecond":120}'
          />
          <p class="config-tip">
            该插件无自描述清单（旧插件需重新上传或加载一次），已回落 JSON
            文本编辑。
          </p>
        </template>
      </div>
      <template #footer>
        <el-button @click="configVisible = false">取消</el-button>
        <el-button
          type="primary"
          :loading="configSaving"
          :disabled="configLoading"
          @click="handleSaveConfig"
        >
          保存{{ configRow?.PluginStatus === 1 ? "（即时生效）" : "" }}
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="detailVisible"
      :title="`插件详情 - ${detailRow?.PluginName ?? ''}`"
      width="640px"
      destroy-on-close
    >
      <template v-if="detailRow">
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="插件Guid" :span="2">
            {{ detailRow.PluginGuid }}
          </el-descriptions-item>
          <el-descriptions-item label="名称">
            {{ detailRow.PluginName }}
          </el-descriptions-item>
          <el-descriptions-item label="版本">
            {{ detailRow.PluginVersion }}
          </el-descriptions-item>
          <el-descriptions-item label="类型">
            {{ detailRow.PluginType }}
          </el-descriptions-item>
          <el-descriptions-item label="模型路径">
            {{ detailRow.PluginModelPath || "-" }}
          </el-descriptions-item>
          <el-descriptions-item label="描述" :span="2">
            {{ detailRow.PluginDesc || "-" }}
          </el-descriptions-item>
          <el-descriptions-item label="加载路径" :span="2">
            {{ detailRow.PluginPath || "-" }}
          </el-descriptions-item>
        </el-descriptions>
        <el-divider content-position="left">自描述清单(Manifest)</el-divider>
        <pre v-if="detailManifest" class="manifest-view">{{
          detailManifest
        }}</pre>
        <el-text v-else type="info" size="small">
          无自描述清单（旧插件需重新上传或加载一次以回写 Manifest）。
        </el-text>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped lang="scss">
:deep(.el-button:focus-visible) {
  outline: none;
}

.search-form {
  :deep(.el-form-item) {
    margin-bottom: 12px;
  }
}

.json-editor :deep(textarea) {
  font-family: Consolas, Monaco, "Courier New", monospace;
  font-size: 13px;
  line-height: 1.5;
}

.config-tip {
  margin-top: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.manifest-view {
  max-height: 320px;
  padding: 12px;
  overflow: auto;
  font-size: 12px;
  background: var(--el-fill-color-light);
  border-radius: 6px;
  white-space: pre-wrap;
  word-break: break-all;
}
</style>
