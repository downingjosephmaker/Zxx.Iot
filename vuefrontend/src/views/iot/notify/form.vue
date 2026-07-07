<script setup lang="ts">
import { computed, ref } from "vue";
import type { NotifyChannelFormProps } from "./utils/types";

defineOptions({
  name: "NotifyChannelForm"
});

const props = withDefaults(defineProps<NotifyChannelFormProps>(), {
  formInline: () => ({
    title: "",
    SnowId: 0,
    ChannelName: "",
    ChannelType: 2,
    TargetUrl: "",
    Secret: "",
    Receivers: "",
    GradeFilter: "",
    EscalationLevel: 0,
    IsEnable: true
  })
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

const typeOptions = [
  { label: "邮件", value: 1 },
  { label: "Webhook", value: 2 },
  { label: "钉钉机器人", value: 3 },
  { label: "企微机器人", value: 4 },
  { label: "短信（预留）", value: 5 }
];

const escalationOptions = [
  { label: "第一梯队（告警产生立即通知）", value: 0 },
  { label: "梯队1（15分钟未处理升级）", value: 1 },
  { label: "梯队2（30分钟未处理升级）", value: 2 },
  { label: "梯队3（60分钟未处理升级）", value: 3 }
];

const urlPlaceholder = computed(() => {
  switch (formValue.value.ChannelType) {
    case 1:
      return "邮件外发接口地址，POST {receivers,subject,content}";
    case 3:
      return "钉钉机器人Webhook，如 https://oapi.dingtalk.com/robot/send?access_token=xxx";
    case 4:
      return "企微机器人Webhook，如 https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=xxx";
    case 5:
      return "短信通道预留，当前仅记录日志";
    default:
      return "Webhook地址，POST告警JSON原文";
  }
});

const rules = {
  ChannelName: [
    { required: true, message: "渠道名称不能为空", trigger: "blur" }
  ],
  TargetUrl: [
    { required: true, message: "目标地址不能为空", trigger: "blur" }
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
    label-width="120px"
  >
    <el-form-item label="渠道名称" prop="ChannelName">
      <el-input
        v-model="formValue.ChannelName"
        placeholder="请输入渠道名称"
        maxlength="50"
        clearable
      />
    </el-form-item>

    <el-form-item label="渠道类型" prop="ChannelType">
      <el-select v-model="formValue.ChannelType" class="w-full">
        <el-option
          v-for="item in typeOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <el-form-item label="是否启用" prop="IsEnable">
      <el-switch v-model="formValue.IsEnable" />
    </el-form-item>

    <el-form-item label="目标地址" prop="TargetUrl">
      <el-input
        v-model="formValue.TargetUrl"
        :placeholder="urlPlaceholder"
        maxlength="300"
        clearable
      />
    </el-form-item>

    <el-form-item
      v-if="formValue.ChannelType === 3"
      label="加签密钥"
      prop="Secret"
    >
      <el-input
        v-model="formValue.Secret"
        type="password"
        show-password
        placeholder="钉钉机器人加签secret，留空不加签"
        maxlength="100"
      />
    </el-form-item>

    <el-form-item
      v-if="formValue.ChannelType === 1 || formValue.ChannelType === 5"
      label="接收人"
      prop="Receivers"
    >
      <el-input
        v-model="formValue.Receivers"
        :placeholder="
          formValue.ChannelType === 1
            ? '邮件收件人，逗号分隔'
            : '手机号，逗号分隔'
        "
        maxlength="300"
        clearable
      />
    </el-form-item>

    <el-form-item label="等级过滤" prop="GradeFilter">
      <el-input
        v-model="formValue.GradeFilter"
        placeholder="只通知命中等级，逗号分隔（如 严重,紧急）；留空=全部等级"
        maxlength="50"
        clearable
      />
    </el-form-item>

    <el-form-item label="升级梯队" prop="EscalationLevel">
      <el-select v-model="formValue.EscalationLevel" class="w-full">
        <el-option
          v-for="item in escalationOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>
  </el-form>
</template>
