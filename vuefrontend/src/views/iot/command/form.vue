<script setup lang="ts">
import { ref, computed } from "vue";
import type { ProductCommandFormProps } from "./utils/types";
import {
  getSupportedCommands,
  type PluginCommandInfo
} from "@/api/iot/plugin";
import ProductTreeSelect from "@/views/iot/components/ProductTreeSelect.vue";

defineOptions({
  name: "ProductCommandForm"
});

const props = withDefaults(defineProps<ProductCommandFormProps>(), {
  formInline: () => ({
    title: "",
    SnowId: 0,
    DeviceTypeCode: "",
    CommandName: "",
    ClassName: "",
    ParamSchema: "",
    ConTemplate: "",
    NeedConfirm: false,
    IsEnable: true
  })
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

/** 已加载插件声明的下行控制类型(B-1.4单源白名单,替代硬编码;拉取失败时保留手输兜底) */
const supportedCommands = ref<PluginCommandInfo[]>([]);
getSupportedCommands().then(data => {
  if (data.Status && data.Result) {
    try {
      supportedCommands.value = JSON.parse(data.Result);
    } catch {
      /* 数据异常时保持手输兜底 */
    }
  }
});

const classNameOptions = computed(() => {
  const seen = new Set<string>();
  return supportedCommands.value.filter(item =>
    seen.has(item.ClassName) ? false : (seen.add(item.ClassName), true)
  );
});

const rules = {
  DeviceTypeCode: [
    { required: true, message: "产品类型编码不能为空", trigger: "blur" }
  ],
  CommandName: [
    { required: true, message: "命令名称不能为空", trigger: "blur" }
  ],
  ClassName: [
    { required: true, message: "下行控制类型不能为空", trigger: "change" }
  ]
};

const validateJsonText = (text: string) => {
  if (!text || text.trim() === "") return true;
  try {
    JSON.parse(text);
    return true;
  } catch {
    return false;
  }
};

const paramSchemaRule = [
  {
    validator: (_rule: any, _value: any, callback: any) => {
      validateJsonText(formValue.value.ParamSchema)
        ? callback()
        : callback(new Error("参数Schema必须是合法JSON"));
    },
    trigger: "blur"
  }
];

const conTemplateRule = [
  {
    validator: (_rule: any, _value: any, callback: any) => {
      validateJsonText(formValue.value.ConTemplate)
        ? callback()
        : callback(new Error("下行内容模板必须是合法JSON"));
    },
    trigger: "blur"
  }
];

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
    <el-form-item label="产品类型" prop="DeviceTypeCode">
      <ProductTreeSelect
        v-model="formValue.DeviceTypeCode"
        placeholder="选择所属产品类型"
      />
    </el-form-item>

    <el-form-item label="命令名称" prop="CommandName">
      <el-input
        v-model="formValue.CommandName"
        placeholder="展示给操作者的命令名，如 阀门开关"
        maxlength="50"
        clearable
      />
    </el-form-item>

    <el-form-item label="下行控制类型" prop="ClassName">
      <el-select
        v-model="formValue.ClassName"
        placeholder="插件侧ClassName"
        filterable
        allow-create
        class="w-full"
      >
        <el-option
          v-for="item in classNameOptions"
          :key="item.ClassName"
          :label="item.ClassName"
          :value="item.ClassName"
        >
          <span>{{ item.ClassName }}</span>
          <span class="option-desc">
            {{ item.Description }}（{{ item.PluginName }}）
          </span>
        </el-option>
      </el-select>
    </el-form-item>

    <el-form-item label="二次确认" prop="NeedConfirm">
      <el-switch v-model="formValue.NeedConfirm" />
      <span class="form-tip">阀控等高危命令建议开启，下发前弹窗确认</span>
    </el-form-item>

    <el-form-item label="是否启用" prop="IsEnable">
      <el-switch v-model="formValue.IsEnable" />
    </el-form-item>

    <el-form-item
      label="参数Schema"
      prop="ParamSchema"
      :rules="paramSchemaRule"
    >
      <el-input
        v-model="formValue.ParamSchema"
        type="textarea"
        :rows="5"
        class="json-editor"
        placeholder='JSON Schema，前端动态表单渲染依据。示例：
{"type":"object","properties":{"action":{"title":"动作","type":"string","enum":["开","关"]}},"required":["action"]}'
      />
    </el-form-item>

    <el-form-item
      label="下行内容模板"
      prop="ConTemplate"
      :rules="conTemplateRule"
    >
      <el-input
        v-model="formValue.ConTemplate"
        type="textarea"
        :rows="4"
        class="json-editor"
        placeholder='ConContent JSON模板，表单值按 {参数名} 占位填充。示例：
{"ParamCode":"valve","ParamValue":"{action}"}'
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

.json-editor :deep(textarea) {
  font-family: Consolas, Monaco, "Courier New", monospace;
  font-size: 13px;
  line-height: 1.5;
}
</style>
