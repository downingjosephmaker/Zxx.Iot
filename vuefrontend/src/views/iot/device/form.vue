<script setup lang="ts">
import { ref } from "vue";
import type { DeviceFormProps } from "./utils/types";

defineOptions({
  name: "DeviceInfoForm"
});

const props = withDefaults(defineProps<DeviceFormProps>(), {
  formInline: () => ({
    title: "",
    DeviceId: 0,
    DeviceName: "",
    DeviceTypeCode: "",
    DeviceGuid: "",
    DeviceGateway: "",
    ParentId: 0,
    SortBorder: "",
    DeviceIp: "",
    DevicePort: 0,
    DeviceCom: 0,
    DeviceAdr: 0,
    IsCollection: 1,
    IsVirtual: 0,
    EnergyType: "其他",
    LineNum: "",
    DeviceIMEI: "",
    DeviceSim: "",
    CurrentTransformer: 1,
    VoltageTransformer: 1
  }),
  typeOptions: () => []
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

const energyTypeOptions = [
  "照明与插座用电",
  "空调用电",
  "动力用电",
  "特殊用电",
  "其他"
];

const rules = {
  DeviceName: [
    { required: true, message: "设备名称不能为空", trigger: "blur" }
  ],
  DeviceTypeCode: [
    { required: true, message: "产品类型不能为空", trigger: "change" }
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
    <el-divider content-position="left">基本信息</el-divider>

    <el-row :gutter="16">
      <el-col :span="12">
        <el-form-item label="设备名称" prop="DeviceName">
          <el-input
            v-model="formValue.DeviceName"
            placeholder="如 1号楼总表"
            maxlength="50"
            clearable
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="产品类型" prop="DeviceTypeCode">
          <el-tree-select
            v-model="formValue.DeviceTypeCode"
            :data="typeOptions"
            check-strictly
            :render-after-expand="false"
            default-expand-all
            filterable
            clearable
            placeholder="选择所属产品类型"
            class="w-full"
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="设备编号" prop="DeviceGuid">
          <el-input
            v-model="formValue.DeviceGuid"
            placeholder="全局唯一编号，如表号，留空不校验"
            maxlength="30"
            clearable
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="网关编号" prop="DeviceGateway">
          <el-input
            v-model="formValue.DeviceGateway"
            placeholder="DTU注册包注册ID匹配用"
            maxlength="30"
            clearable
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="上级设备ID" prop="ParentId">
          <el-input-number
            v-model="formValue.ParentId"
            :min="0"
            :step="1"
          />
          <span class="form-tip">0=顶级设备</span>
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="排序号" prop="SortBorder">
          <el-input
            v-model="formValue.SortBorder"
            placeholder="留空自动生成"
            maxlength="10"
            clearable
          />
        </el-form-item>
      </el-col>
    </el-row>

    <el-divider content-position="left">通讯配置</el-divider>

    <el-row :gutter="16">
      <el-col :span="12">
        <el-form-item label="设备IP" prop="DeviceIp">
          <el-input
            v-model="formValue.DeviceIp"
            placeholder="TCP直连模式的设备IP，拨入模式留空"
            maxlength="30"
            clearable
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="端口号" prop="DevicePort">
          <el-input-number
            v-model="formValue.DevicePort"
            :min="0"
            :max="65535"
            :step="1"
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="串口通道号" prop="DeviceCom">
          <el-input-number
            v-model="formValue.DeviceCom"
            :min="0"
            :step="1"
          />
          <span class="form-tip">串口采集时使用，0=非串口</span>
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="协议地址" prop="DeviceAdr">
          <el-input-number
            v-model="formValue.DeviceAdr"
            :min="0"
            :step="1"
          />
          <span class="form-tip">Modbus从站号/645表地址等</span>
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="是否采集" prop="IsCollection">
          <el-switch
            v-model="formValue.IsCollection"
            :active-value="1"
            :inactive-value="0"
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="虚拟设备" prop="IsVirtual">
          <el-switch
            v-model="formValue.IsVirtual"
            :active-value="1"
            :inactive-value="0"
          />
          <span class="form-tip">统计/汇总类非物理设备</span>
        </el-form-item>
      </el-col>
    </el-row>

    <el-divider content-position="left">拓展属性</el-divider>

    <el-row :gutter="16">
      <el-col :span="12">
        <el-form-item label="能耗类型" prop="EnergyType">
          <el-select v-model="formValue.EnergyType" class="w-full">
            <el-option
              v-for="item in energyTypeOptions"
              :key="item"
              :label="item"
              :value="item"
            />
          </el-select>
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="线路名称" prop="LineNum">
          <el-input
            v-model="formValue.LineNum"
            placeholder="如 1AL1"
            maxlength="30"
            clearable
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="设备IMEI" prop="DeviceIMEI">
          <el-input
            v-model="formValue.DeviceIMEI"
            placeholder="物联卡设备标识"
            maxlength="30"
            clearable
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="SIM卡ICCID" prop="DeviceSim">
          <el-input
            v-model="formValue.DeviceSim"
            placeholder="SIM卡标识"
            maxlength="30"
            clearable
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="CT变比" prop="CurrentTransformer">
          <el-input-number
            v-model="formValue.CurrentTransformer"
            :min="1"
            :step="1"
          />
          <span class="form-tip">电流互感器变比</span>
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="PT变比" prop="VoltageTransformer">
          <el-input-number
            v-model="formValue.VoltageTransformer"
            :min="1"
            :step="1"
          />
          <span class="form-tip">电压互感器变比</span>
        </el-form-item>
      </el-col>
    </el-row>
  </el-form>
</template>

<style scoped>
.form-tip {
  margin-left: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
</style>
