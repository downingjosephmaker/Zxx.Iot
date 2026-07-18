<script setup lang="ts">
import { ref } from "vue";
import type { DeviceTypeFormProps } from "./utils/types";

defineOptions({
  name: "DeviceTypeForm"
});

const props = withDefaults(defineProps<DeviceTypeFormProps>(), {
  formInline: () => ({
    title: "",
    TypeCode: "",
    TypeName: "",
    ParentId: "",
    SortBorder: "",
    IsEnable: true,
    HasChild: false,
    OfflineMinute: 0,
    SubChannels: 0,
    SbjgType: false,
    MqttKey: ""
  }),
  typeOptions: () => []
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

const rules = {
  TypeCode: [
    { required: true, message: "类型编码不能为空", trigger: "blur" },
    {
      pattern: /^[A-Za-z0-9_-]{1,30}$/,
      message: "编码仅限字母/数字/下划线/中划线，不超过30位",
      trigger: "blur"
    }
  ],
  TypeName: [
    { required: true, message: "类型名称不能为空", trigger: "blur" },
    {
      validator: (_rule: any, value: string, callback: any) => {
        // 竖线是FullName/FullCode的层级分隔符，名称中出现会破坏树结构
        value?.includes("|")
          ? callback(new Error("名称不能包含竖线|字符"))
          : callback();
      },
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
  <el-form
    ref="ruleFormRef"
    :model="formValue"
    :rules="rules"
    label-width="120px"
  >
    <el-form-item label="类型编码" prop="TypeCode">
      <el-input
        v-model="formValue.TypeCode"
        :disabled="formValue.title === '修改'"
        placeholder="唯一编码，如 dianbiao"
        maxlength="30"
        clearable
      />
    </el-form-item>

    <el-form-item label="类型名称" prop="TypeName">
      <el-input
        v-model="formValue.TypeName"
        placeholder="如 电表"
        maxlength="50"
        clearable
      />
    </el-form-item>

    <el-form-item label="上级类型" prop="ParentId">
      <el-tree-select
        v-model="formValue.ParentId"
        :data="typeOptions"
        check-strictly
        :render-after-expand="false"
        default-expand-all
        filterable
        clearable
        placeholder="留空为顶级类型"
        class="w-full"
      />
    </el-form-item>

    <el-form-item label="排序号" prop="SortBorder">
      <el-input
        v-model="formValue.SortBorder"
        placeholder="留空自动生成(如 A001)"
        maxlength="10"
        clearable
      />
    </el-form-item>

    <el-form-item label="是否启用" prop="IsEnable">
      <el-switch v-model="formValue.IsEnable" />
    </el-form-item>

    <el-divider content-position="left">拓展属性</el-divider>

    <el-form-item label="是否采集" prop="SbjgType">
      <el-switch v-model="formValue.SbjgType" />
      <span class="form-tip">物理采集类产品开启，虚拟/统计类关闭</span>
    </el-form-item>

    <el-form-item label="离线判断间隔" prop="OfflineMinute">
      <el-input-number
        v-model="formValue.OfflineMinute"
        :min="0"
        :max="14400"
        :step="1"
      />
      <span class="form-tip">分钟，0=不判断离线</span>
    </el-form-item>

    <el-form-item label="支路数量" prop="SubChannels">
      <el-input-number
        v-model="formValue.SubChannels"
        :min="0"
        :max="999"
        :step="1"
      />
    </el-form-item>

    <el-form-item label="Mqtt通讯Key" prop="MqttKey">
      <el-input
        v-model="formValue.MqttKey"
        placeholder="MQTT上行报文的类型Key，留空不启用"
        maxlength="50"
        clearable
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
