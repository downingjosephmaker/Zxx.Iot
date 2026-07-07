<script setup lang="ts">
import { ref, watch } from "vue";
import type { LinkageRuleFormProps } from "./utils/types";

defineOptions({
  name: "LinkageRuleForm"
});

const props = withDefaults(defineProps<LinkageRuleFormProps>(), {
  formInline: () => ({
    title: "",
    SnowId: 0,
    RuleName: "",
    TriggerType: 1,
    TriggerDeviceId: 0,
    TriggerParamCode: "",
    TriggerCron: "",
    ConditionFormula: "",
    TimeRanges: "",
    ActionType: 3,
    ActionConfig: "",
    CooldownSeconds: 60,
    IsEnable: true,
    cmdConfig: { PluginGuid: "", ClassName: "", ConContent: "", DeviceIdsText: "" },
    vpConfig: { DeviceId: 0, ParamCode: "", ParamValue: "" },
    notifyConfig: { Content: "" },
    webhookConfig: { Url: "", Body: "" }
  })
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

const triggerOptions = [
  { label: "点位变化", value: 1 },
  { label: "告警产生", value: 2 },
  { label: "告警恢复", value: 3 },
  { label: "定时cron", value: 4 },
  { label: "设备上线", value: 5 },
  { label: "设备离线", value: 6 }
];

const actionOptions = [
  { label: "下发命令", value: 1 },
  { label: "写虚拟点位", value: 2 },
  { label: "发通知", value: 3 },
  { label: "调用Webhook", value: 4 }
];

const rules = {
  RuleName: [{ required: true, message: "规则名称不能为空", trigger: "blur" }]
};

const cronRule = [
  { required: true, message: "定时触发必须填写cron表达式", trigger: "blur" }
];
const classNameRule = [
  { required: true, message: "控制类名不能为空", trigger: "blur" }
];
const paramCodeRule = [
  { required: true, message: "参数编码不能为空", trigger: "blur" }
];
const contentRule = [
  { required: true, message: "通知内容不能为空", trigger: "blur" }
];
const urlRule = [{ required: true, message: "目标URL不能为空", trigger: "blur" }];

const validateJsonText = (text: string) => {
  if (!text || text.trim() === "") return true;
  try {
    JSON.parse(text);
    return true;
  } catch {
    return false;
  }
};

const timeRangesRule = [
  {
    validator: (_rule: any, _value: any, callback: any) => {
      validateJsonText(formValue.value.TimeRanges || "")
        ? callback()
        : callback(new Error("时间窗必须是合法的JSON数组"));
    },
    trigger: "blur"
  }
];

/** 表单辅助字段变化时同步序列化回 ActionConfig（与后端四个动作模型对齐） */
watch(
  () => [
    formValue.value.ActionType,
    formValue.value.cmdConfig,
    formValue.value.vpConfig,
    formValue.value.notifyConfig,
    formValue.value.webhookConfig
  ],
  () => {
    switch (formValue.value.ActionType) {
      case 1: {
        const deviceIds = (formValue.value.cmdConfig.DeviceIdsText || "")
          .split(/[,，\s]+/)
          .map(item => parseInt(item, 10))
          .filter(item => !Number.isNaN(item) && item > 0);
        formValue.value.ActionConfig = JSON.stringify({
          PluginGuid: formValue.value.cmdConfig.PluginGuid,
          ClassName: formValue.value.cmdConfig.ClassName,
          ConContent: formValue.value.cmdConfig.ConContent,
          DeviceIds: deviceIds
        });
        break;
      }
      case 2:
        formValue.value.ActionConfig = JSON.stringify(formValue.value.vpConfig);
        break;
      case 4:
        formValue.value.ActionConfig = JSON.stringify(
          formValue.value.webhookConfig
        );
        break;
      default:
        formValue.value.ActionConfig = JSON.stringify(
          formValue.value.notifyConfig
        );
        break;
    }
  },
  { deep: true, immediate: true }
);

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
    <el-form-item label="规则名称" prop="RuleName">
      <el-input
        v-model="formValue.RuleName"
        placeholder="请输入规则名称"
        maxlength="50"
        clearable
      />
    </el-form-item>

    <el-form-item label="是否启用" prop="IsEnable">
      <el-switch v-model="formValue.IsEnable" />
    </el-form-item>

    <el-divider content-position="left">触发</el-divider>

    <el-form-item label="触发类型" prop="TriggerType">
      <el-select v-model="formValue.TriggerType" class="w-full">
        <el-option
          v-for="item in triggerOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <el-form-item
      v-if="formValue.TriggerType !== 4"
      label="触发设备ID"
      prop="TriggerDeviceId"
    >
      <el-input-number
        v-model="formValue.TriggerDeviceId"
        :min="0"
        controls-position="right"
      />
      <span class="form-tip">0 = 任意设备</span>
    </el-form-item>

    <el-form-item
      v-if="formValue.TriggerType === 1"
      label="触发参数编码"
      prop="TriggerParamCode"
    >
      <el-input
        v-model="formValue.TriggerParamCode"
        placeholder="留空表示任意参数变化都触发"
        clearable
      />
    </el-form-item>

    <el-form-item
      v-if="formValue.TriggerType === 4"
      label="cron表达式"
      prop="TriggerCron"
      :rules="cronRule"
    >
      <el-input
        v-model="formValue.TriggerCron"
        placeholder="如 0 0 8 * * ?（每天8点）"
        clearable
      />
    </el-form-item>

    <el-divider content-position="left">条件</el-divider>

    <el-form-item label="条件表达式" prop="ConditionFormula">
      <el-input
        v-model="formValue.ConditionFormula"
        type="textarea"
        :rows="2"
        maxlength="300"
        placeholder="留空=恒真。裸参数编码取触发设备点位，如 temp > 35；跨设备用 d{设备ID}_{参数编码}，如 d1001_door == 1"
      />
    </el-form-item>

    <el-form-item
      label="生效时间窗"
      prop="TimeRanges"
      :rules="timeRangesRule"
    >
      <el-input
        v-model="formValue.TimeRanges"
        type="textarea"
        :rows="2"
        placeholder='留空=全天。JSON数组，如 [{"Days":[1,2,3,4,5],"Start":"09:00","End":"18:00"}]（星期日=0）'
      />
    </el-form-item>

    <el-form-item label="冷却秒数" prop="CooldownSeconds">
      <el-input-number
        v-model="formValue.CooldownSeconds"
        :min="0"
        :max="86400"
        controls-position="right"
      />
      <span class="form-tip">条件持续满足时的最短再执行间隔，防连发</span>
    </el-form-item>

    <el-divider content-position="left">动作</el-divider>

    <el-form-item label="动作类型" prop="ActionType">
      <el-select v-model="formValue.ActionType" class="w-full">
        <el-option
          v-for="item in actionOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <template v-if="formValue.ActionType === 1">
      <el-form-item
        label="控制类名"
        prop="cmdConfig.ClassName"
        :rules="classNameRule"
      >
        <el-input
          v-model="formValue.cmdConfig.ClassName"
          placeholder="白名单控制类型，如 netmodbuswrite"
          clearable
        />
      </el-form-item>
      <el-form-item label="控制内容" prop="cmdConfig.ConContent">
        <el-input
          v-model="formValue.cmdConfig.ConContent"
          type="textarea"
          :rows="2"
          placeholder='控制内容JSON，如 {"ParamCode":"switch","ParamValue":"1"}'
        />
      </el-form-item>
      <el-form-item label="目标插件Guid" prop="cmdConfig.PluginGuid">
        <el-input
          v-model="formValue.cmdConfig.PluginGuid"
          placeholder="留空广播全部已加载插件"
          clearable
        />
      </el-form-item>
      <el-form-item label="目标设备清单" prop="cmdConfig.DeviceIdsText">
        <el-input
          v-model="formValue.cmdConfig.DeviceIdsText"
          placeholder="设备ID逗号分隔，如 1001,1002；留空=触发设备"
          clearable
        />
      </el-form-item>
    </template>

    <template v-if="formValue.ActionType === 2">
      <el-form-item label="目标设备ID" prop="vpConfig.DeviceId">
        <el-input-number
          v-model="formValue.vpConfig.DeviceId"
          :min="0"
          controls-position="right"
        />
        <span class="form-tip">0 = 触发设备</span>
      </el-form-item>
      <el-form-item
        label="虚拟参数编码"
        prop="vpConfig.ParamCode"
        :rules="paramCodeRule"
      >
        <el-input
          v-model="formValue.vpConfig.ParamCode"
          placeholder="写入最新值缓存与遥测管道的参数编码"
          clearable
        />
      </el-form-item>
      <el-form-item label="写入值" prop="vpConfig.ParamValue">
        <el-input
          v-model="formValue.vpConfig.ParamValue"
          placeholder="数值进value，其余进value_str"
          clearable
        />
      </el-form-item>
    </template>

    <template v-if="formValue.ActionType === 3">
      <el-form-item
        label="通知内容"
        prop="notifyConfig.Content"
        :rules="contentRule"
      >
        <el-input
          v-model="formValue.notifyConfig.Content"
          type="textarea"
          :rows="3"
          placeholder="发往notify_channel第一梯队渠道的文本内容"
        />
      </el-form-item>
    </template>

    <template v-if="formValue.ActionType === 4">
      <el-form-item
        label="目标URL"
        prop="webhookConfig.Url"
        :rules="urlRule"
      >
        <el-input
          v-model="formValue.webhookConfig.Url"
          placeholder="如 https://example.com/hook"
          clearable
        />
      </el-form-item>
      <el-form-item label="请求体" prop="webhookConfig.Body">
        <el-input
          v-model="formValue.webhookConfig.Body"
          type="textarea"
          :rows="3"
          placeholder="POST的JSON请求体；留空发默认 {rule,time}"
        />
      </el-form-item>
    </template>
  </el-form>
</template>

<style scoped>
.form-tip {
  margin-left: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
</style>
