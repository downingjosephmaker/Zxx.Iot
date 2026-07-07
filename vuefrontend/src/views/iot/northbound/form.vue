<script setup lang="ts">
import { ref, watch } from "vue";
import type { NorthboundSinkFormProps } from "./utils/types";

defineOptions({
  name: "NorthboundSinkForm"
});

const props = withDefaults(defineProps<NorthboundSinkFormProps>(), {
  formInline: () => ({
    title: "",
    SnowId: 0,
    SinkName: "",
    SinkType: 2,
    ConnConfig: "",
    ContentMode: 3,
    ScopeType: 0,
    ScopeJson: "",
    IsEnable: true,
    mqttConfig: {
      Host: "",
      Port: 1883,
      ClientId: "",
      UserName: "",
      Password: "",
      DataTopic: "iot/data/{deviceId}",
      EventTopic: "iot/event/{deviceId}"
    },
    httpConfig: {
      Url: "",
      HeadersJson: ""
    }
  })
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

const typeOptions = [
  { label: "MQTT", value: 1 },
  { label: "HTTP Webhook", value: 2 },
  { label: "Kafka（预留）", value: 3, disabled: true }
];

const contentOptions = [
  { label: "仅遥测", value: 1 },
  { label: "仅告警", value: 2 },
  { label: "遥测+告警", value: 3 }
];

const scopeOptions = [
  { label: "全部设备", value: 0 },
  { label: "按产品类型编码", value: 1 },
  { label: "按设备ID", value: 2 }
];

const rules = {
  SinkName: [{ required: true, message: "目的地名称不能为空", trigger: "blur" }]
};

const mqttHostRule = [
  { required: true, message: "Broker地址不能为空", trigger: "blur" }
];
const httpUrlRule = [
  { required: true, message: "目标URL不能为空", trigger: "blur" }
];

const validateJsonText = (text: string) => {
  if (!text || text.trim() === "") return true;
  try {
    JSON.parse(text);
    return true;
  } catch {
    return false;
  }
};

const headersRule = [
  {
    validator: (_rule: any, _value: any, callback: any) => {
      validateJsonText(formValue.value.httpConfig.HeadersJson)
        ? callback()
        : callback(new Error("请求头必须是合法的JSON对象"));
    },
    trigger: "blur"
  }
];

const scopeJsonRule = [
  {
    validator: (_rule: any, _value: any, callback: any) => {
      validateJsonText(formValue.value.ScopeJson || "")
        ? callback()
        : callback(new Error("范围清单必须是合法的JSON数组"));
    },
    trigger: "blur"
  }
];

/** 表单辅助字段变化时同步序列化回 ConnConfig（与后端 SinkMqttConfig/SinkHttpConfig 对齐） */
watch(
  () => [
    formValue.value.SinkType,
    formValue.value.mqttConfig,
    formValue.value.httpConfig
  ],
  () => {
    if (formValue.value.SinkType === 1) {
      formValue.value.ConnConfig = JSON.stringify(formValue.value.mqttConfig);
    } else {
      let headers = {};
      try {
        headers = formValue.value.httpConfig.HeadersJson
          ? JSON.parse(formValue.value.httpConfig.HeadersJson)
          : {};
      } catch {
        headers = {};
      }
      formValue.value.ConnConfig = JSON.stringify({
        Url: formValue.value.httpConfig.Url,
        Headers: headers
      });
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
    <el-form-item label="目的地名称" prop="SinkName">
      <el-input
        v-model="formValue.SinkName"
        placeholder="请输入目的地名称"
        maxlength="50"
        clearable
      />
    </el-form-item>

    <el-form-item label="目的地类型" prop="SinkType">
      <el-select v-model="formValue.SinkType" class="w-full">
        <el-option
          v-for="item in typeOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
          :disabled="item.disabled"
        />
      </el-select>
    </el-form-item>

    <el-form-item label="是否启用" prop="IsEnable">
      <el-switch v-model="formValue.IsEnable" />
    </el-form-item>

    <el-form-item label="转发内容" prop="ContentMode">
      <el-select v-model="formValue.ContentMode" class="w-full">
        <el-option
          v-for="item in contentOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <el-form-item label="推送范围" prop="ScopeType">
      <el-select v-model="formValue.ScopeType" class="w-full">
        <el-option
          v-for="item in scopeOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <el-form-item
      v-if="formValue.ScopeType !== 0"
      label="范围清单"
      prop="ScopeJson"
      :rules="scopeJsonRule"
    >
      <el-input
        v-model="formValue.ScopeJson"
        type="textarea"
        :rows="2"
        :placeholder="
          formValue.ScopeType === 1
            ? '产品类型编码JSON数组，如 [&quot;dianbiao&quot;,&quot;shuibiao&quot;]'
            : '设备ID JSON数组，如 [1001,1002]'
        "
      />
    </el-form-item>

    <template v-if="formValue.SinkType === 1">
      <el-divider content-position="left">MQTT 连接配置</el-divider>
      <el-form-item label="Broker地址" prop="mqttConfig.Host" :rules="mqttHostRule">
        <el-input
          v-model="formValue.mqttConfig.Host"
          placeholder="如 192.168.1.100"
          clearable
        />
      </el-form-item>
      <el-form-item label="端口" prop="mqttConfig.Port">
        <el-input-number
          v-model="formValue.mqttConfig.Port"
          :min="1"
          :max="65535"
          controls-position="right"
        />
      </el-form-item>
      <el-form-item label="客户端ID" prop="mqttConfig.ClientId">
        <el-input
          v-model="formValue.mqttConfig.ClientId"
          placeholder="留空自动生成"
          clearable
        />
      </el-form-item>
      <el-form-item label="账号" prop="mqttConfig.UserName">
        <el-input
          v-model="formValue.mqttConfig.UserName"
          placeholder="留空匿名连接"
          clearable
        />
      </el-form-item>
      <el-form-item label="密码" prop="mqttConfig.Password">
        <el-input
          v-model="formValue.mqttConfig.Password"
          type="password"
          show-password
          placeholder="留空匿名连接"
        />
      </el-form-item>
      <el-form-item label="遥测主题" prop="mqttConfig.DataTopic">
        <el-input
          v-model="formValue.mqttConfig.DataTopic"
          placeholder="iot/data/{deviceId}，{deviceId}为占位符"
        />
      </el-form-item>
      <el-form-item label="告警主题" prop="mqttConfig.EventTopic">
        <el-input
          v-model="formValue.mqttConfig.EventTopic"
          placeholder="iot/event/{deviceId}，{deviceId}为占位符"
        />
      </el-form-item>
    </template>

    <template v-if="formValue.SinkType === 2">
      <el-divider content-position="left">HTTP Webhook 配置</el-divider>
      <el-form-item label="目标URL" prop="httpConfig.Url" :rules="httpUrlRule">
        <el-input
          v-model="formValue.httpConfig.Url"
          placeholder="如 https://example.com/iot/webhook"
          clearable
        />
      </el-form-item>
      <el-form-item
        label="附加请求头"
        prop="httpConfig.HeadersJson"
        :rules="headersRule"
      >
        <el-input
          v-model="formValue.httpConfig.HeadersJson"
          type="textarea"
          :rows="3"
          placeholder='JSON对象（可选），如 {"Authorization":"Bearer xxx"}'
        />
      </el-form-item>
    </template>
  </el-form>
</template>
