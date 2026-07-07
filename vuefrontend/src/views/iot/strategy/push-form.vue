<script setup lang="ts">
import { computed, ref } from "vue";
import type { PushStrategyFormProps } from "./utils/types";

defineOptions({
  name: "PushStrategyForm"
});

const props = withDefaults(defineProps<PushStrategyFormProps>(), {
  formInline: () => ({
    title: "",
    SnowId: 0,
    UnitId: 0,
    ScopeType: 1,
    ScopeId: "",
    ParamCode: "",
    ReportMode: null,
    DeadbandType: null,
    DeadbandValue: null,
    MinPushIntervalMs: null,
    MaxSilentMs: null,
    DebounceIgnoreKeys: ""
  })
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

const scopeOptions = [
  { label: "产品（默认）", value: 1 },
  { label: "设备（覆盖产品）", value: 2 },
  { label: "点位（覆盖设备）", value: 3 }
];

const modeOptions = [
  { label: "收到即报", value: 1 },
  { label: "变化上报", value: 2 },
  { label: "定时上报", value: 3 },
  { label: "变化上报+最大静默兜底", value: 4 }
];

const deadbandOptions = [
  { label: "严格不等", value: 0 },
  { label: "绝对死区", value: 1 },
  { label: "百分比死区", value: 2 }
];

const scopeIdPlaceholder = computed(() =>
  formValue.value.ScopeType === 1 ? "设备类型编码，如 dianbiao" : "设备ID"
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
    label-width="140px"
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
      <el-input
        v-model="formValue.ScopeId"
        :placeholder="scopeIdPlaceholder"
        maxlength="50"
        clearable
      />
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

    <el-form-item label="推送模式" prop="ReportMode">
      <el-select
        v-model="formValue.ReportMode"
        placeholder="留空未设置"
        clearable
        class="w-full"
      >
        <el-option
          v-for="item in modeOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <el-form-item label="死区类型" prop="DeadbandType">
      <el-select
        v-model="formValue.DeadbandType"
        placeholder="留空未设置"
        clearable
        class="w-full"
      >
        <el-option
          v-for="item in deadbandOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <el-form-item
      v-if="formValue.DeadbandType === 1 || formValue.DeadbandType === 2"
      label="死区值"
      prop="DeadbandValue"
    >
      <el-input-number
        v-model="formValue.DeadbandValue"
        :min="0"
        :precision="4"
        :step="0.1"
        controls-position="right"
      />
      <span class="form-tip">
        {{ formValue.DeadbandType === 2 ? "百分比（如 5 = 5%）" : "绝对值" }}
      </span>
    </el-form-item>

    <el-form-item label="最小推送间隔(毫秒)" prop="MinPushIntervalMs">
      <el-input-number
        v-model="formValue.MinPushIntervalMs"
        :min="0"
        :step="1000"
        controls-position="right"
        placeholder="留空未设置"
      />
      <span class="form-tip">节流窗口内多次变化只推最新一条</span>
    </el-form-item>

    <el-form-item label="最大静默周期(毫秒)" prop="MaxSilentMs">
      <el-input-number
        v-model="formValue.MaxSilentMs"
        :min="0"
        :step="10000"
        controls-position="right"
        placeholder="留空未设置"
      />
      <span class="form-tip">值不变超过此时长也强制推一条兜底</span>
    </el-form-item>

    <el-form-item label="关键属性点位" prop="DebounceIgnoreKeys">
      <el-input
        v-model="formValue.DebounceIgnoreKeys"
        type="textarea"
        :rows="2"
        maxlength="500"
        placeholder="参数编码用 | 分隔，如 switch|alarm_state；这些点位变化立即冲刷，不参与合并节流"
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
</style>
