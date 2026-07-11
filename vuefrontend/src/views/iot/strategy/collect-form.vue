<script setup lang="ts">
import { ref, watch } from "vue";
import type { CollectStrategyFormProps } from "./utils/types";
import ProductTreeSelect from "../components/ProductTreeSelect.vue";
import DeviceSelect from "../components/DeviceSelect.vue";

defineOptions({
  name: "CollectStrategyForm"
});

const props = withDefaults(defineProps<CollectStrategyFormProps>(), {
  formInline: () => ({
    title: "",
    SnowId: 0,
    TenantId: 0,
    ScopeType: 1,
    ScopeId: "",
    ParamCode: "",
    CollectCycleMs: null,
    CollectCron: "",
    ReportCycleMs: null
  })
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

const scopeOptions = [
  { label: "产品（默认）", value: 1 },
  { label: "设备（覆盖产品）", value: 2 },
  { label: "点位（覆盖设备）", value: 3 }
];

// 切换挂靠层级时清空挂靠对象，防止跨层级旧值残留(产品编码≠设备ID)
watch(
  () => formValue.value.ScopeType,
  () => {
    formValue.value.ScopeId = "";
  }
);

const rules = {
  ScopeId: [{ required: true, message: "挂靠对象不能为空", trigger: "blur" }]
};

const paramCodeRule = [
  { required: true, message: "点位级必须指定参数编码", trigger: "blur" }
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
    <el-form-item label="挂靠层级" prop="ScopeType">
      <el-select v-model="formValue.ScopeType" class="w-full">
        <el-option
          v-for="item in scopeOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <el-form-item label="挂靠对象" prop="ScopeId">
      <ProductTreeSelect
        v-if="formValue.ScopeType === 1"
        v-model="formValue.ScopeId"
      />
      <DeviceSelect v-else v-model="formValue.ScopeId" string-value />
    </el-form-item>

    <el-form-item
      v-if="formValue.ScopeType === 3"
      label="参数编码"
      prop="ParamCode"
      :rules="paramCodeRule"
    >
      <el-input
        v-model="formValue.ParamCode"
        placeholder="点位级覆盖的目标参数编码"
        maxlength="100"
        clearable
      />
    </el-form-item>

    <el-divider content-position="left">
      策略字段（留空=未设置，运行时回落下级）
    </el-divider>

    <el-form-item label="采集周期(毫秒)" prop="CollectCycleMs">
      <el-input-number
        v-model="formValue.CollectCycleMs"
        :min="100"
        :step="1000"
        controls-position="right"
        placeholder="留空未设置"
      />
    </el-form-item>

    <el-form-item label="采集cron" prop="CollectCron">
      <el-input
        v-model="formValue.CollectCron"
        placeholder="低频场景用，如 0 0/30 * * * ?；设置后优先于采集周期"
        maxlength="50"
        clearable
      />
    </el-form-item>

    <el-form-item label="上报周期(毫秒)" prop="ReportCycleMs">
      <el-input-number
        v-model="formValue.ReportCycleMs"
        :min="100"
        :step="1000"
        controls-position="right"
        placeholder="留空未设置"
      />
      <span class="form-tip">与采集解耦：采集可快、上报可慢</span>
    </el-form-item>
  </el-form>
</template>

<style scoped>
.form-tip {
  margin-left: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
</style>
