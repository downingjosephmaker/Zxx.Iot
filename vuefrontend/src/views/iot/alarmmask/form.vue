<script setup lang="ts">
import { computed, ref } from "vue";
import type { AlarmMaskFormProps } from "./utils/types";

defineOptions({
  name: "AlarmMaskForm"
});

const props = withDefaults(defineProps<AlarmMaskFormProps>(), {
  formInline: () => ({
    title: "",
    SnowId: 0,
    MaskScopeType: 5,
    ScopeId: "",
    MaskMode: 2,
    StartTime: "",
    EndTime: "",
    TimeRanges: "",
    MaskAction: 2,
    DowngradeGrade: "",
    Reason: "",
    OperatorName: "",
    ExpireAt: "",
    IsEnable: true
  })
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

const scopeOptions = [
  { label: "全局", value: 1 },
  { label: "单位", value: 2 },
  { label: "建筑", value: 3 },
  { label: "设备类型", value: 4 },
  { label: "单设备", value: 5 },
  { label: "告警等级", value: 6 }
];

const modeOptions = [
  { label: "永久", value: 1 },
  { label: "一次性时间段", value: 2 },
  { label: "周期性时间窗", value: 3 }
];

const actionOptions = [
  { label: "完全屏蔽（不入库）", value: 1 },
  { label: "静默（入库打标不通知，默认）", value: 2 },
  { label: "降级（改写告警等级）", value: 3 }
];

const scopeIdPlaceholder = computed(() => {
  switch (formValue.value.MaskScopeType) {
    case 2:
      return "单位ID";
    case 3:
      return "建筑ID";
    case 4:
      return "设备类型编码，如 dianbiao";
    case 5:
      return "设备ID";
    case 6:
      return "告警等级名，如 严重";
    default:
      return "";
  }
});

const rules = {
  Reason: [{ required: true, message: "屏蔽原因不能为空", trigger: "blur" }]
};

const scopeIdRule = [
  { required: true, message: "屏蔽对象不能为空", trigger: "blur" }
];
const startTimeRule = [
  { required: true, message: "起始时间不能为空", trigger: "change" }
];
const endTimeRule = [
  { required: true, message: "结束时间不能为空", trigger: "change" }
];
const downgradeRule = [
  { required: true, message: "降级目标等级不能为空", trigger: "blur" }
];

const timeRangesRule = [
  {
    validator: (_rule: any, _value: any, callback: any) => {
      const text = formValue.value.TimeRanges || "";
      if (text.trim() === "") {
        callback(new Error("周期模式必须配置时间窗"));
        return;
      }
      try {
        const parsed = JSON.parse(text);
        Array.isArray(parsed)
          ? callback()
          : callback(new Error("时间窗必须是JSON数组"));
      } catch {
        callback(new Error("时间窗必须是合法的JSON数组"));
      }
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
    label-width="120px"
  >
    <el-form-item label="屏蔽对象类型" prop="MaskScopeType">
      <el-select v-model="formValue.MaskScopeType" class="w-full">
        <el-option
          v-for="item in scopeOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <el-form-item
      v-if="formValue.MaskScopeType !== 1"
      label="屏蔽对象"
      prop="ScopeId"
      :rules="scopeIdRule"
    >
      <el-input
        v-model="formValue.ScopeId"
        :placeholder="scopeIdPlaceholder"
        maxlength="50"
        clearable
      />
    </el-form-item>

    <el-form-item label="屏蔽模式" prop="MaskMode">
      <el-select v-model="formValue.MaskMode" class="w-full">
        <el-option
          v-for="item in modeOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <template v-if="formValue.MaskMode === 2">
      <el-form-item label="起始时间" prop="StartTime" :rules="startTimeRule">
        <el-date-picker
          v-model="formValue.StartTime"
          type="datetime"
          placeholder="选择起始时间"
          format="YYYY-MM-DD HH:mm:ss"
          value-format="YYYY-MM-DD HH:mm:ss"
          class="w-full"
        />
      </el-form-item>
      <el-form-item label="结束时间" prop="EndTime" :rules="endTimeRule">
        <el-date-picker
          v-model="formValue.EndTime"
          type="datetime"
          placeholder="选择结束时间"
          format="YYYY-MM-DD HH:mm:ss"
          value-format="YYYY-MM-DD HH:mm:ss"
          class="w-full"
        />
      </el-form-item>
    </template>

    <el-form-item
      v-if="formValue.MaskMode === 3"
      label="周期时间窗"
      prop="TimeRanges"
      :rules="timeRangesRule"
    >
      <el-input
        v-model="formValue.TimeRanges"
        type="textarea"
        :rows="3"
        placeholder='JSON数组，如 [{"Days":[1,2,3,4,5],"Start":"09:00","End":"18:00"}]（星期日=0）'
      />
    </el-form-item>

    <el-form-item label="屏蔽动作" prop="MaskAction">
      <el-select v-model="formValue.MaskAction" class="w-full">
        <el-option
          v-for="item in actionOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <el-form-item
      v-if="formValue.MaskAction === 3"
      label="降级目标等级"
      prop="DowngradeGrade"
      :rules="downgradeRule"
    >
      <el-input
        v-model="formValue.DowngradeGrade"
        placeholder="如 提示"
        maxlength="20"
        clearable
      />
    </el-form-item>

    <el-form-item label="屏蔽原因" prop="Reason">
      <el-input
        v-model="formValue.Reason"
        type="textarea"
        :rows="2"
        maxlength="200"
        show-word-limit
        placeholder="如：3号楼空调检修（2026-07-10前）"
      />
    </el-form-item>

    <el-form-item label="操作人" prop="OperatorName">
      <el-input
        v-model="formValue.OperatorName"
        placeholder="留空则不记录"
        maxlength="50"
        clearable
      />
    </el-form-item>

    <el-form-item label="自动失效时间" prop="ExpireAt">
      <el-date-picker
        v-model="formValue.ExpireAt"
        type="datetime"
        placeholder="到期自动恢复，留空=不失效"
        format="YYYY-MM-DD HH:mm:ss"
        value-format="YYYY-MM-DD HH:mm:ss"
        class="w-full"
      />
    </el-form-item>

    <el-form-item label="是否启用" prop="IsEnable">
      <el-switch v-model="formValue.IsEnable" />
    </el-form-item>
  </el-form>
</template>
