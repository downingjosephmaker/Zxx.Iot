<script setup lang="ts">
import { ref, reactive, computed } from "vue";
import { message } from "@/utils/message";
import { ElMessageBox } from "element-plus";
import SchemaForm from "@/views/iot/components/SchemaForm.vue";
import {
  getListByTypeCode,
  sendCommand,
  type ProductCommandItem
} from "@/api/iot/command";

defineOptions({
  name: "DeviceCommandSend"
});

interface Props {
  /** 目标设备ID */
  deviceId: number;
  /** 设备名称，仅展示 */
  deviceName: string;
  /** 设备所属产品类型编码，拉命令清单 */
  deviceTypeCode: string;
}

const props = defineProps<Props>();

const loading = ref(false);
const sending = ref(false);
const commandList = ref<ProductCommandItem[]>([]);
const selectedId = ref<string>("");
/** 动态表单值(SchemaForm按选中命令的ParamSchema补种缺省值、清理游离键) */
const formModel = reactive<Record<string, unknown>>({});
const schemaFormRef = ref();

const selectedCommand = computed(() =>
  commandList.value.find(c => String(c.SnowId) === selectedId.value)
);

/** 按 ConTemplate 的 {参数名} 占位填充表单值，生成最终下行内容 */
function buildConContent(): string {
  const tpl = selectedCommand.value?.ConTemplate?.trim();
  if (!tpl) {
    // 无模板时直接把表单值作为 JSON 下发
    return JSON.stringify(formModel);
  }
  return tpl.replace(/\{(\w+)\}/g, (_m, key) => {
    const val = formModel[key];
    return val === undefined || val === null ? "" : String(val);
  });
}

async function loadCommands() {
  if (!props.deviceTypeCode) {
    message("设备未设置产品类型，无可用命令", { type: "warning" });
    return;
  }
  loading.value = true;
  try {
    const data = await getListByTypeCode(props.deviceTypeCode);
    if (data.Status) {
      commandList.value = JSON.parse(data.Result);
      if (commandList.value.length) {
        selectedId.value = String(commandList.value[0].SnowId);
      }
    } else {
      message(data.Message, { type: "error" });
    }
  } finally {
    loading.value = false;
  }
}

async function doSend() {
  const cmd = selectedCommand.value;
  if (!cmd) {
    message("请先选择命令", { type: "warning" });
    return;
  }
  // 有 schema 字段时先校验动态表单
  if (schemaFormRef.value && !(await schemaFormRef.value.validate())) {
    message("请完善命令参数", { type: "warning" });
    return;
  }
  const conContent = buildConContent();
  sending.value = true;
  try {
    const data = await sendCommand({
      CommandId: cmd.SnowId,
      DeviceIds: [props.deviceId],
      ConContent: conContent
    });
    if (data.Status) {
      message(data.Message || "下发成功", { type: "success" });
    } else {
      message(data.Message || "下发失败", { type: "error" });
    }
  } finally {
    sending.value = false;
  }
}

/** NeedConfirm 命令下发前二次确认 */
async function onSend() {
  const cmd = selectedCommand.value;
  if (cmd?.NeedConfirm) {
    try {
      await ElMessageBox.confirm(
        `命令「${cmd.CommandName}」为高危操作，确认向设备「${props.deviceName}」下发吗？`,
        "二次确认",
        { type: "warning", confirmButtonText: "确认下发", cancelButtonText: "取消" }
      );
    } catch {
      return;
    }
  }
  await doSend();
}

loadCommands();

defineExpose({ onSend, sending });
</script>

<template>
  <div v-loading="loading" class="command-send">
    <el-form label-width="110px">
      <el-form-item label="目标设备">
        <el-tag type="info" effect="light">
          {{ deviceName }}（ID: {{ deviceId }}）
        </el-tag>
      </el-form-item>

      <el-form-item label="选择命令">
        <el-select
          v-model="selectedId"
          placeholder="请选择要下发的命令"
          class="w-full"
        >
          <el-option
            v-for="item in commandList"
            :key="item.SnowId"
            :label="item.CommandName"
            :value="String(item.SnowId)"
          >
            <span>{{ item.CommandName }}</span>
            <el-tag
              v-if="item.NeedConfirm"
              type="danger"
              effect="light"
              size="small"
              class="ml-2"
            >
              高危
            </el-tag>
            <span class="cmd-class">{{ item.ClassName }}</span>
          </el-option>
        </el-select>
      </el-form-item>
    </el-form>

    <el-empty
      v-if="!loading && commandList.length === 0"
      description="该产品类型下暂无启用的命令"
      :image-size="80"
    />

    <template v-if="selectedCommand">
      <el-divider content-position="left">命令参数</el-divider>
      <SchemaForm
        ref="schemaFormRef"
        :schema="selectedCommand.ParamSchema || ''"
        :model="formModel"
        label-width="110px"
      >
        <template #empty>
          <el-text type="info" size="small">
            该命令无参数，可直接下发。
          </el-text>
        </template>
      </SchemaForm>

      <el-divider content-position="left">下行内容预览</el-divider>
      <el-input
        :model-value="buildConContent()"
        type="textarea"
        :rows="3"
        readonly
        class="preview"
      />
    </template>
  </div>
</template>

<style scoped>
.cmd-class {
  margin-left: 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.preview :deep(textarea) {
  font-family: Consolas, Monaco, "Courier New", monospace;
  font-size: 13px;
}
</style>
