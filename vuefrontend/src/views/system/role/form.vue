<script setup lang="ts">
import { ref } from "vue";
import type { RoleFormProps } from "./utils/types";

defineOptions({
  name: "SysRoleForm"
});

const props = withDefaults(defineProps<RoleFormProps>(), {
  formInline: () => ({
    title: "",
    RoleId: 0,
    RoleName: "",
    ParentId: 0,
    RoleDescribe: "",
    SortBorder: "",
    TreeLevel: 1,
    FullName: "",
    FullCode: "",
    HasChild: false
  }),
  roleList: () => []
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

const parentOptions = ref<{ label: string; value: number }[]>([
  { label: "无（顶级角色）", value: 0 },
  ...props.roleList
    .filter(r => r.RoleId !== props.formInline.RoleId)
    .map(r => ({ label: r.RoleName, value: r.RoleId }))
]);

const rules = {
  RoleName: [{ required: true, message: "角色名称不能为空", trigger: "blur" }]
};

function getRef() {
  return ruleFormRef.value;
}

defineExpose({ getRef });
</script>

<template>
  <el-form ref="ruleFormRef" :model="formValue" :rules="rules" label-width="90px">
    <el-form-item label="角色名称" prop="RoleName">
      <el-input
        v-model="formValue.RoleName"
        clearable
        placeholder="请输入角色名称"
      />
    </el-form-item>
    <el-form-item label="上级角色" prop="ParentId">
      <el-select
        v-model="formValue.ParentId"
        placeholder="请选择上级角色"
        class="w-full"
        filterable
      >
        <el-option
          v-for="item in parentOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>
    <el-form-item label="角色描述" prop="RoleDescribe">
      <el-input
        v-model="formValue.RoleDescribe"
        type="textarea"
        :rows="3"
        placeholder="请输入角色描述"
      />
    </el-form-item>
  </el-form>
</template>
