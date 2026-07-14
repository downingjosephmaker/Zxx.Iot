<script setup lang="ts">
import { ref } from "vue";
import type { ButtonFormProps } from "./utils/types";

defineOptions({
  name: "SysButtonForm"
});

const props = withDefaults(defineProps<ButtonFormProps>(), {
  formInline: () => ({
    title: "",
    ButtonId: 0,
    ButtonCode: "",
    ButtonName: "",
    ButtonHtml: "",
    ButtonSort: 0,
    ButtonRemark: "",
    ButtonType: 1
  })
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

const rules = {
  ButtonName: [{ required: true, message: "按钮名称不能为空", trigger: "blur" }],
  ButtonCode: [
    { required: true, message: "按钮编码不能为空", trigger: "blur" },
    {
      pattern: /^[A-Za-z][A-Za-z0-9_.:-]*$/,
      message: "编码以字母开头，仅含字母数字及 _ . : -",
      trigger: "blur"
    }
  ]
};

function getRef() {
  return ruleFormRef.value;
}

defineExpose({ getRef });
</script>

<template>
  <el-form ref="ruleFormRef" :model="formValue" :rules="rules" label-width="90px">
    <el-form-item label="按钮名称" prop="ButtonName">
      <el-input
        v-model="formValue.ButtonName"
        clearable
        placeholder="展示名，如 新增/导出"
      />
    </el-form-item>
    <el-form-item label="按钮编码" prop="ButtonCode">
      <el-input
        v-model="formValue.ButtonCode"
        clearable
        placeholder="鉴权标识，如 btn.add（v-perms 判据）"
      />
    </el-form-item>
    <el-form-item label="按钮类型" prop="ButtonType">
      <el-radio-group v-model="formValue.ButtonType">
        <el-radio :value="1">页面按钮</el-radio>
        <el-radio :value="2">表单按钮</el-radio>
      </el-radio-group>
    </el-form-item>
    <el-form-item label="排序" prop="ButtonSort">
      <el-input-number v-model="formValue.ButtonSort" :min="0" :max="9999" />
    </el-form-item>
    <el-form-item label="备注" prop="ButtonRemark">
      <el-input
        v-model="formValue.ButtonRemark"
        type="textarea"
        :rows="2"
        placeholder="用途说明"
      />
    </el-form-item>
  </el-form>
</template>
