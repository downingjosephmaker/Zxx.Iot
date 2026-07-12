<script setup lang="ts">
import { ref, onMounted } from "vue";
import type { TenantFormProps, TenantItem } from "./utils/types";
import { getUnitListByPage } from "@/api/system";

defineOptions({
  name: "TenantUnitForm"
});

const props = withDefaults(defineProps<TenantFormProps>(), {
  formInline: () => ({
    title: "",
    TenantId: 0,
    ParentId: 0,
    TreeLevel: 1,
    FullCode: "",
    FullName: "",
    HasChild: false,
    TenantName: "",
    Remark: ""
  })
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);
const parentOptions = ref<{ label: string; value: number }[]>([]);

const rules = {
  TenantName: [
    { required: true, message: "租户名称不能为空", trigger: "blur" }
  ]
};

onMounted(async () => {
  const data = await getUnitListByPage({
    page: 1,
    pagesize: 999,
    sconlist: []
  });
  if (data.Status) {
    const list: TenantItem[] = JSON.parse(data.Result);
    const selfMark = `|${formValue.value.TenantId}|`;
    parentOptions.value = [
      { label: "无（顶级租户）", value: 0 },
      ...list
        // 排除自身及自己的子孙（FullCode 祖先链含自身即为子孙），防止把树挂成环
        .filter(
          t =>
            formValue.value.TenantId === 0 ||
            !(t.FullCode ?? "").includes(selfMark)
        )
        .map(t => ({
          label: t.FullName || t.TenantName,
          value: t.TenantId
        }))
    ];
  }
});

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
    label-width="90px"
  >
    <el-form-item label="租户名称" prop="TenantName">
      <el-input
        v-model="formValue.TenantName"
        clearable
        placeholder="请输入租户名称"
      />
    </el-form-item>
    <el-form-item label="上级租户" prop="ParentId">
      <el-select
        v-model="formValue.ParentId"
        placeholder="请选择上级租户"
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
    <el-form-item label="备注" prop="Remark">
      <el-input
        v-model="formValue.Remark"
        type="textarea"
        :rows="3"
        placeholder="请输入备注"
      />
    </el-form-item>
  </el-form>
</template>
