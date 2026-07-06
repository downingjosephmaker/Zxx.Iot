<template>
  <el-dialog
    v-model="dialogVisible"
    width="700px"
    top="10vh"
    :close-on-click-modal="false"
    :close-on-press-escape="true"
    draggable
    align-center
    :show-close="false"
    class="table-property-dialog"
    @close="handleClose"
  >
    <!-- 自定义头部 -->
    <template #header="{ close }">
      <div class="custom-dialog-header">
        <div class="header-left">
          <div class="header-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
              <rect
                x="3"
                y="6"
                width="18"
                height="3"
                fill="currentColor"
                opacity="0.3"
              />
              <rect x="3" y="10" width="18" height="3" fill="currentColor" />
              <rect
                x="3"
                y="14"
                width="18"
                height="3"
                fill="currentColor"
                opacity="0.3"
              />
              <rect x="3" y="18" width="18" height="3" fill="currentColor" />
            </svg>
          </div>
          <div class="header-content">
            <h3 class="header-title">表格配置</h3>
            <p class="header-subtitle">设置表格样式和数据源</p>
          </div>
        </div>
        <div class="header-right">
          <div class="header-actions">
            <el-button
              link
              size="small"
              class="action-btn close-btn"
              @click="close"
            >
              <el-icon><Close /></el-icon>
            </el-button>
          </div>
        </div>
      </div>
    </template>

    <div class="table-dialog-content">
      <el-tabs v-model="activeTab" class="config-tabs">
        <!-- 基本配置 -->
        <el-tab-pane label="基本配置" name="basic">
          <el-form :model="formData" label-width="100px" class="table-form">
            <el-form-item label="表格标题">
              <el-input
                v-model="formData.title"
                placeholder="请输入表格标题"
                clearable
              />
            </el-form-item>

            <el-form-item label="显示边框">
              <el-switch v-model="formData.border" />
              <span style="margin-left: 10px; color: #909399; font-size: 12px">
                显示表格边框线
              </span>
            </el-form-item>

            <el-form-item label="斑马纹">
              <el-switch v-model="formData.stripe" />
              <span style="margin-left: 10px; color: #909399; font-size: 12px">
                交替显示行背景色
              </span>
            </el-form-item>

            <el-form-item label="表格大小">
              <el-select v-model="formData.size" placeholder="选择表格大小">
                <el-option label="大" value="large" />
                <el-option label="默认" value="default" />
                <el-option label="小" value="small" />
              </el-select>
            </el-form-item>

            <el-form-item label="高亮当前行">
              <el-switch v-model="formData.highlightCurrentRow" />
            </el-form-item>

            <el-form-item label="显示表头">
              <el-switch v-model="formData.showHeader" />
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 列配置 -->
        <el-tab-pane label="列配置" name="columns">
          <div class="columns-config">
            <el-button
              type="primary"
              size="small"
              style="margin-bottom: 16px"
              @click="addColumn"
            >
              <el-icon><Plus /></el-icon>
              添加列
            </el-button>

            <div
              v-for="(column, index) in formData.columns"
              :key="index"
              class="column-item"
            >
              <div class="column-header">
                <span class="column-index">列 {{ index + 1 }}</span>
                <el-button
                  type="danger"
                  size="small"
                  link
                  @click="removeColumn(index)"
                >
                  <el-icon><Delete /></el-icon>
                  删除
                </el-button>
              </div>

              <el-form :model="column" label-width="80px" size="small">
                <el-form-item label="列标题">
                  <el-input v-model="column.label" placeholder="列标题" />
                </el-form-item>

                <el-form-item label="字段名">
                  <el-input v-model="column.prop" placeholder="数据字段名" />
                </el-form-item>

                <el-form-item label="列宽">
                  <el-input-number
                    v-model="column.width"
                    :min="0"
                    :max="1000"
                    placeholder="自动"
                  />
                </el-form-item>

                <el-form-item label="对齐">
                  <el-select v-model="column.align" placeholder="对齐方式">
                    <el-option label="左对齐" value="left" />
                    <el-option label="居中" value="center" />
                    <el-option label="右对齐" value="right" />
                  </el-select>
                </el-form-item>

                <el-form-item label="固定列">
                  <el-select v-model="column.fixed" placeholder="不固定" clearable>
                    <el-option label="固定左侧" value="left" />
                    <el-option label="固定右侧" value="right" />
                  </el-select>
                </el-form-item>

                <el-form-item label="可排序">
                  <el-switch v-model="column.sortable" />
                </el-form-item>
              </el-form>
            </div>

            <el-empty
              v-if="formData.columns.length === 0"
              description="暂无列配置，请添加列"
              :image-size="100"
            />
          </div>
        </el-tab-pane>

        <!-- 数据配置 -->
        <el-tab-pane label="数据配置" name="data">
          <el-form :model="formData" label-width="100px" class="table-form">
            <el-alert
              title="数据源说明"
              type="info"
              :closable="false"
              style="margin-bottom: 16px"
            >
              <template #default>
                <div style="font-size: 12px; line-height: 1.6">
                  <p style="margin: 0 0 6px 0">
                    <strong>📊 数据绑定：</strong>从"数据集"面板选择数据源
                  </p>
                  <p style="margin: 0">
                    <strong>💡 提示：</strong>数据集需返回数组格式的数据
                  </p>
                </div>
              </template>
            </el-alert>

            <el-form-item label="数据集">
              <el-select
                v-model="formData.datasetId"
                placeholder="请选择数据集"
                clearable
              >
                <el-option
                  v-for="dataset in datasetList"
                  :key="dataset.id"
                  :label="dataset.name"
                  :value="dataset.id"
                />
              </el-select>
              <el-button
                type="primary"
                link
                style="margin-left: 10px"
                @click="openDatasetPanel"
              >
                <el-icon><Setting /></el-icon>
                管理数据集
              </el-button>
            </el-form-item>

            <el-form-item label="数据路径">
              <el-input
                v-model="formData.dataPath"
                placeholder="如: data.list 或留空使用根数据"
                clearable
              >
                <template #prepend>
                  <el-icon><Connection /></el-icon>
                </template>
              </el-input>
              <div style="color: #909399; font-size: 11px; margin-top: 5px">
                数据在响应中的路径，如 data.records
              </div>
            </el-form-item>

            <el-divider content-position="left">分页设置</el-divider>

            <el-form-item label="启用分页">
              <el-switch v-model="formData.pagination.enabled" />
            </el-form-item>

            <template v-if="formData.pagination.enabled">
              <el-form-item label="每页条数">
                <el-input-number
                  v-model="formData.pagination.pageSize"
                  :min="1"
                  :max="100"
                />
              </el-form-item>

              <el-form-item label="总条数路径">
                <el-input
                  v-model="formData.pagination.totalPath"
                  placeholder="如: data.total"
                  clearable
                />
              </el-form-item>
            </template>

            <el-divider content-position="left">刷新设置</el-divider>

            <el-form-item label="自动刷新">
              <el-switch v-model="formData.autoRefresh" />
            </el-form-item>

            <el-form-item v-if="formData.autoRefresh" label="刷新间隔">
              <el-input-number
                v-model="formData.refreshInterval"
                :min="1000"
                :step="1000"
              />
              <span style="margin-left: 10px; color: #909399; font-size: 12px">
                毫秒
              </span>
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <!-- 样式配置 -->
        <el-tab-pane label="样式配置" name="style">
          <el-form :model="formData" label-width="120px" class="table-form">
            <el-form-item label="表头背景色">
              <el-color-picker v-model="formData.headerBgColor" show-alpha />
            </el-form-item>

            <el-form-item label="表头文字颜色">
              <el-color-picker v-model="formData.headerTextColor" show-alpha />
            </el-form-item>

            <el-form-item label="行背景色">
              <el-color-picker v-model="formData.rowBgColor" show-alpha />
            </el-form-item>

            <el-form-item label="斑马纹颜色">
              <el-color-picker v-model="formData.stripeBgColor" show-alpha />
            </el-form-item>

            <el-form-item label="边框颜色">
              <el-color-picker v-model="formData.borderColor" show-alpha />
            </el-form-item>

            <el-form-item label="悬停背景色">
              <el-color-picker v-model="formData.hoverBgColor" show-alpha />
            </el-form-item>
          </el-form>
        </el-tab-pane>
      </el-tabs>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="handleClose">取消</el-button>
        <el-button type="primary" @click="handleSave">
          <el-icon><Check /></el-icon>
          保存
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue";
import { ElMessage } from "element-plus";
import {
  Close,
  Check,
  Plus,
  Delete,
  Setting,
  Connection
} from "@element-plus/icons-vue";

// Props
const props = defineProps({
  visible: {
    type: Boolean,
    default: false
  },
  tableComponent: {
    type: Object,
    default: null
  },
  datasetList: {
    type: Array,
    default: () => []
  }
});

// Emits
const emit = defineEmits(["update:visible", "save-config", "open-dataset-panel"]);

// 响应式数据
const dialogVisible = computed({
  get: () => props.visible,
  set: val => emit("update:visible", val)
});

const activeTab = ref("basic");

// 表单数据
const formData = ref({
  title: "数据表格",
  border: true,
  stripe: true,
  size: "default",
  highlightCurrentRow: true,
  showHeader: true,
  columns: [
    { label: "序号", prop: "id", width: 80, align: "center", sortable: false, fixed: "" },
    { label: "名称", prop: "name", width: 0, align: "left", sortable: false, fixed: "" },
    { label: "状态", prop: "status", width: 100, align: "center", sortable: false, fixed: "" }
  ],
  datasetId: "",
  dataPath: "",
  pagination: {
    enabled: true,
    pageSize: 10,
    totalPath: "total"
  },
  autoRefresh: false,
  refreshInterval: 5000,
  headerBgColor: "#f5f7fa",
  headerTextColor: "#606266",
  rowBgColor: "#ffffff",
  stripeBgColor: "#fafafa",
  borderColor: "#ebeef5",
  hoverBgColor: "#f5f7fa"
});

// 监听对话框打开，初始化数据
watch(
  () => props.visible,
  newVal => {
    if (newVal && props.tableComponent) {
      const config = props.tableComponent.tableConfig || {};

      formData.value = {
        title: config.title || "数据表格",
        border: config.border !== false,
        stripe: config.stripe !== false,
        size: config.size || "default",
        highlightCurrentRow: config.highlightCurrentRow !== false,
        showHeader: config.showHeader !== false,
        columns: config.columns || [
          { label: "序号", prop: "id", width: 80, align: "center", sortable: false, fixed: "" },
          { label: "名称", prop: "name", width: 0, align: "left", sortable: false, fixed: "" },
          { label: "状态", prop: "status", width: 100, align: "center", sortable: false, fixed: "" }
        ],
        datasetId: config.datasetId || "",
        dataPath: config.dataPath || "",
        pagination: {
          enabled: config.pagination?.enabled !== false,
          pageSize: config.pagination?.pageSize || 10,
          totalPath: config.pagination?.totalPath || "total"
        },
        autoRefresh: config.autoRefresh === true,
        refreshInterval: config.refreshInterval || 5000,
        headerBgColor: config.headerBgColor || "#f5f7fa",
        headerTextColor: config.headerTextColor || "#606266",
        rowBgColor: config.rowBgColor || "#ffffff",
        stripeBgColor: config.stripeBgColor || "#fafafa",
        borderColor: config.borderColor || "#ebeef5",
        hoverBgColor: config.hoverBgColor || "#f5f7fa"
      };
    }
  }
);

// 添加列
const addColumn = () => {
  formData.value.columns.push({
    label: `列${formData.value.columns.length + 1}`,
    prop: `field${formData.value.columns.length + 1}`,
    width: 0,
    align: "left",
    sortable: false,
    fixed: ""
  });
};

// 删除列
const removeColumn = (index: number) => {
  if (formData.value.columns.length <= 1) {
    ElMessage.warning("至少保留一列");
    return;
  }
  formData.value.columns.splice(index, 1);
};

// 打开数据集面板
const openDatasetPanel = () => {
  emit("open-dataset-panel");
};

// 保存配置
const handleSave = () => {
  if (formData.value.columns.length === 0) {
    ElMessage.warning("请至少配置一列");
    return;
  }

  // 验证列配置
  for (const column of formData.value.columns) {
    if (!column.label) {
      ElMessage.warning("请填写所有列的标题");
      return;
    }
    if (!column.prop) {
      ElMessage.warning("请填写所有列的字段名");
      return;
    }
  }

  // 构建配置对象
  const config = {
    title: formData.value.title,
    border: formData.value.border,
    stripe: formData.value.stripe,
    size: formData.value.size,
    highlightCurrentRow: formData.value.highlightCurrentRow,
    showHeader: formData.value.showHeader,
    columns: formData.value.columns.map(col => ({
      label: col.label,
      prop: col.prop,
      width: col.width || undefined,
      align: col.align,
      sortable: col.sortable,
      fixed: col.fixed || undefined
    })),
    datasetId: formData.value.datasetId,
    dataPath: formData.value.dataPath,
    pagination: {
      enabled: formData.value.pagination.enabled,
      pageSize: formData.value.pagination.pageSize,
      totalPath: formData.value.pagination.totalPath
    },
    autoRefresh: formData.value.autoRefresh,
    refreshInterval: formData.value.refreshInterval,
    headerBgColor: formData.value.headerBgColor,
    headerTextColor: formData.value.headerTextColor,
    rowBgColor: formData.value.rowBgColor,
    stripeBgColor: formData.value.stripeBgColor,
    borderColor: formData.value.borderColor,
    hoverBgColor: formData.value.hoverBgColor
  };

  emit("save-config", config);
  ElMessage.success("表格配置已保存");
  dialogVisible.value = false;
};

// 关闭对话框
const handleClose = () => {
  dialogVisible.value = false;
};
</script>

<style scoped lang="scss">
// 对话框整体样式
.table-property-dialog {
  :deep(.el-dialog) {
    border-radius: 16px;
    overflow: hidden;
    box-shadow:
      0 20px 60px rgba(0, 0, 0, 0.15),
      0 8px 30px rgba(0, 0, 0, 0.1);
    border: 1px solid rgba(255, 255, 255, 0.2);

    .el-dialog__header {
      padding: 0;
      margin: 0;
      border-bottom: none;
    }

    .el-dialog__body {
      padding: 0;
      margin: 0;
    }
  }
}

// 自定义头部样式
.custom-dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  position: relative;
  overflow: hidden;

  &::before {
    content: "";
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(
      135deg,
      rgba(255, 255, 255, 0.1) 0%,
      rgba(255, 255, 255, 0.05) 100%
    );
    pointer-events: none;
  }

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
    position: relative;
    z-index: 2;

    .header-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      background: rgba(255, 255, 255, 0.15);
      border-radius: 10px;
      backdrop-filter: blur(10px);
      border: 1px solid rgba(255, 255, 255, 0.2);
      color: white;
      transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 6px 20px rgba(255, 255, 255, 0.25);
        background: rgba(255, 255, 255, 0.2);
      }

      svg {
        width: 20px;
        height: 20px;
        filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.2));
      }
    }

    .header-content {
      .header-title {
        margin: 0;
        font-size: 16px;
        font-weight: 600;
        color: white;
        letter-spacing: 0.3px;
        text-shadow: 0 2px 6px rgba(0, 0, 0, 0.25);
      }

      .header-subtitle {
        margin: 2px 0 0 0;
        font-size: 12px;
        color: rgba(255, 255, 255, 0.75);
        font-weight: 400;
        letter-spacing: 0.2px;
        text-shadow: 0 1px 3px rgba(0, 0, 0, 0.25);
      }
    }
  }

  .header-right {
    position: relative;
    z-index: 2;

    .header-actions {
      display: flex;
      gap: 6px;

      .action-btn {
        width: 32px;
        height: 32px;
        border-radius: 8px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: rgba(255, 255, 255, 0.1);
        border: 1px solid rgba(255, 255, 255, 0.2);
        color: rgba(255, 255, 255, 0.9);
        backdrop-filter: blur(10px);
        transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);

        &:hover {
          background: rgba(255, 255, 255, 0.2);
          color: white;
          transform: translateY(-2px);
          box-shadow: 0 6px 20px rgba(0, 0, 0, 0.25);
        }

        &.close-btn:hover {
          background: rgba(255, 107, 107, 0.8);
          border-color: rgba(255, 107, 107, 0.6);
        }

        .el-icon {
          font-size: 14px;
        }
      }
    }
  }
}

// 内容区域
.table-dialog-content {
  max-height: 65vh;
  overflow-y: auto;
  padding: 20px;
}

// 标签页样式
.config-tabs {
  :deep(.el-tabs__header) {
    margin-bottom: 20px;
  }

  :deep(.el-tabs__nav-wrap::after) {
    background-color: #e4e7ed;
  }
}

// 表单样式
.table-form {
  :deep(.el-form-item) {
    margin-bottom: 20px;

    .el-form-item__label {
      font-size: 13px;
      color: #6c757d;
      font-weight: 500;
      line-height: 1.5;
    }

    .el-input,
    .el-select {
      .el-input__wrapper {
        border-radius: 6px;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
        transition: all 0.3s ease;

        &:hover {
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
        }

        &.is-focus {
          box-shadow: 0 0 0 2px rgba(102, 126, 234, 0.2);
        }
      }
    }
  }
}

// 列配置样式
.columns-config {
  .column-item {
    background: #f8f9fa;
    border: 1px solid #e9ecef;
    border-radius: 8px;
    padding: 16px;
    margin-bottom: 16px;
    transition: all 0.3s ease;

    &:hover {
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      border-color: #667eea;
    }

    .column-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
      padding-bottom: 8px;
      border-bottom: 1px solid #dee2e6;

      .column-index {
        font-weight: 600;
        color: #495057;
        font-size: 14px;
      }
    }
  }
}

// 分隔线
:deep(.el-divider) {
  margin: 20px 0;

  .el-divider__text {
    font-size: 13px;
    font-weight: 500;
    color: #6c757d;
  }
}

// 底部按钮
.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 20px;
  border-top: 1px solid #e9ecef;
  background: #f8f9fa;

  .el-button {
    min-width: 80px;
    height: 36px;
    border-radius: 6px;
    font-weight: 500;
    font-size: 13px;

    &:first-child {
      background: #6c757d;
      border-color: #6c757d;
      color: white;

      &:hover {
        background: #5a6268;
        border-color: #545b62;
      }
    }

    &.el-button--primary {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border: none;

      &:hover {
        background: linear-gradient(135deg, #5a67d8 0%, #6b46c1 100%);
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
      }
    }

    .el-icon {
      margin-right: 4px;
    }
  }
}
</style>
