<script setup lang="ts">
import { ref } from "vue";
import type { ProtocolScriptFormProps } from "./utils/types";
import { SCRIPT_TEMPLATE } from "./utils/types";

defineOptions({
  name: "ProtocolScriptForm"
});

const props = withDefaults(defineProps<ProtocolScriptFormProps>(), {
  formInline: () => ({
    title: "",
    SnowId: 0,
    ScriptName: "",
    DeviceTypeCode: "",
    ScriptContent: SCRIPT_TEMPLATE,
    Version: 1,
    SampleHex: "",
    SampleContext: "",
    IsEnable: false
  })
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

const rules = {
  ScriptName: [
    { required: true, message: "脚本名称不能为空", trigger: "blur" }
  ],
  DeviceTypeCode: [
    { required: true, message: "挂靠产品类型编码不能为空", trigger: "blur" }
  ],
  ScriptContent: [
    { required: true, message: "脚本内容不能为空", trigger: "blur" }
  ]
};

function getRef() {
  return ruleFormRef.value;
}

defineExpose({ getRef });
</script>

<template>
  <el-form
    ref="ruleFormRef"
    :model="formValue"
    :rules="rules"
    label-width="130px"
  >
    <el-form-item label="脚本名称" prop="ScriptName">
      <el-input
        v-model="formValue.ScriptName"
        placeholder="请输入脚本名称"
        maxlength="50"
        clearable
      />
    </el-form-item>

    <el-form-item label="挂靠产品编码" prop="DeviceTypeCode">
      <el-input
        v-model="formValue.DeviceTypeCode"
        placeholder="设备类型编码，该产品的非JSON载荷/透传帧用此脚本解析"
        maxlength="50"
        clearable
      />
    </el-form-item>

    <el-form-item label="是否启用" prop="IsEnable">
      <el-switch v-model="formValue.IsEnable" />
      <span class="form-tip">
        安全默认禁用；启用后运行时错误频发（5分钟100次）会被自动禁用并通知
      </span>
    </el-form-item>

    <el-form-item v-if="Number(formValue.SnowId) !== 0" label="当前版本">
      <el-tag effect="light">v{{ formValue.Version }}</el-tag>
      <span class="form-tip">保存后自动升为 v{{ formValue.Version + 1 }}</span>
    </el-form-item>

    <el-form-item label="脚本内容" prop="ScriptContent">
      <el-input
        v-model="formValue.ScriptContent"
        type="textarea"
        :rows="16"
        class="script-editor"
        placeholder="三段式JS：decode(frame, context) / encode(command, context) / splitFrames(buffer, context)"
      />
    </el-form-item>

    <el-form-item label="样例帧hex" prop="SampleHex">
      <el-input
        v-model="formValue.SampleHex"
        placeholder="调试台默认输入，如 01030400640065B8F0"
        maxlength="500"
        clearable
      />
    </el-form-item>

    <el-form-item label="样例上下文" prop="SampleContext">
      <el-input
        v-model="formValue.SampleContext"
        placeholder='调试台默认上下文JSON，如 {"deviceKey":"dtu001"}'
        maxlength="500"
        clearable
      />
    </el-form-item>
  </el-form>
</template>

<style scoped>
.form-tip {
  margin-left: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.script-editor :deep(textarea) {
  font-family: Consolas, Monaco, "Courier New", monospace;
  font-size: 13px;
  line-height: 1.5;
}
</style>
