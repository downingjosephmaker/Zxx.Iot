<script setup lang="ts">
import { ref } from "vue";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import AddFill from "~icons/ri/add-circle-line";
import Delete from "~icons/ep/delete";
import type { DeviceTypeParamFormProps } from "./utils/types";

defineOptions({
  name: "DeviceTypeParamForm"
});

const props = withDefaults(defineProps<DeviceTypeParamFormProps>(), {
  formInline: () => ({
    title: "",
    SnowId: 0,
    DeviceTypeCode: "",
    SubChannel: "总路",
    ParamCode: "",
    ParamName: "",
    ParamTypeName: "",
    ParamAddr: 0,
    ParamFormula: "",
    ValueType: "数值",
    ExpandStatusValues: [],
    ValueUnit: "",
    DecimalDigit: 2,
    ParamMaxValue: 0,
    ParamMinValue: 0,
    ParamChangeValue: 0,
    RangeFilterEnable: false,
    AmplitudeFilterEnable: false,
    MaxAmplitudePercent: 0,
    ContinuousFilterEnable: false,
    MaxContinuousCount: 3,
    IsShow: true,
    IsMainShow: false,
    IsSet: false,
    IsPeak: false,
    IsReport: false,
    IsMapDefault: false,
    IsPt: false,
    IsCt: false,
    IsCustomAlarm: false,
    CollectFuncCode: 0,
    CollectDataType: "",
    CollectByteOrder: "",
    CollectBitOffset: -1,
    CollectRegLength: 0,
    CollectWritable: false,
    CollectNodeId: "",
    IsAlarmSource: false,
    AlarmConfigId: 0
  })
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

const subChannelOptions = ["总路", "1路", "2路", "3路"];

const funcCodeOptions = [
  { value: 0, label: "0 不采集" },
  { value: 1, label: "1 FC01读线圈" },
  { value: 2, label: "2 FC02读离散输入" },
  { value: 3, label: "3 FC03读保持寄存器" },
  { value: 4, label: "4 FC04读输入寄存器" }
];

const dataTypeOptions = [
  "int16",
  "uint16",
  "int32",
  "uint32",
  "int64",
  "float32",
  "float64",
  "bcd",
  "string",
  "bool"
];

const byteOrderOptions = ["ABCD", "CDAB", "BADC", "DCBA"];

const rules = {
  DeviceTypeCode: [
    { required: true, message: "产品类型编码不能为空", trigger: "blur" }
  ],
  ParamCode: [
    { required: true, message: "参数编码不能为空", trigger: "blur" }
  ],
  ParamName: [
    { required: true, message: "参数名称不能为空", trigger: "blur" }
  ]
};

function addStatusValue() {
  const list = formValue.value.ExpandStatusValues;
  const nextKey = list.length
    ? Math.max(...list.map(t => t.StatusKey)) + 1
    : 0;
  list.push({ StatusKey: nextKey, StatusValue: "" });
}

function removeStatusValue(index: number) {
  formValue.value.ExpandStatusValues.splice(index, 1);
}

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
    <el-divider content-position="left">基本信息</el-divider>

    <el-row :gutter="16">
      <el-col :span="12">
        <el-form-item label="产品类型编码" prop="DeviceTypeCode">
          <el-input
            v-model="formValue.DeviceTypeCode"
            placeholder="如 dianbiao"
            maxlength="30"
            clearable
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="设备路数" prop="SubChannel">
          <el-select
            v-model="formValue.SubChannel"
            filterable
            allow-create
            class="w-full"
          >
            <el-option
              v-for="item in subChannelOptions"
              :key="item"
              :label="item"
              :value="item"
            />
          </el-select>
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="参数编码" prop="ParamCode">
          <el-input
            v-model="formValue.ParamCode"
            placeholder="如 energy"
            maxlength="50"
            clearable
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="参数名称" prop="ParamName">
          <el-input
            v-model="formValue.ParamName"
            placeholder="如 正向有功电能"
            maxlength="50"
            clearable
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="参数分类" prop="ParamTypeName">
          <el-input
            v-model="formValue.ParamTypeName"
            placeholder="如 电流/电压/用能，运行页分组"
            maxlength="30"
            clearable
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="值类型" prop="ValueType">
          <el-radio-group v-model="formValue.ValueType">
            <el-radio-button value="数值">数值</el-radio-button>
            <el-radio-button value="状态">状态</el-radio-button>
          </el-radio-group>
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="值单位" prop="ValueUnit">
          <el-input
            v-model="formValue.ValueUnit"
            placeholder="如 kWh"
            maxlength="10"
            clearable
          />
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="小数位数" prop="DecimalDigit">
          <el-input-number
            v-model="formValue.DecimalDigit"
            :min="0"
            :max="6"
            :step="1"
          />
        </el-form-item>
      </el-col>
      <el-col :span="24">
        <el-form-item label="修正公式" prop="ParamFormula">
          <el-input
            v-model="formValue.ParamFormula"
            placeholder="如 a*0.1（a=原始值），留空不修正"
            maxlength="50"
            clearable
          />
        </el-form-item>
      </el-col>
    </el-row>

    <el-form-item
      v-if="formValue.ValueType === '状态'"
      label="状态值集合"
      prop="ExpandStatusValues"
    >
      <div class="status-values w-full">
        <div
          v-for="(item, index) in formValue.ExpandStatusValues"
          :key="index"
          class="status-row"
        >
          <el-input-number
            v-model="item.StatusKey"
            :min="-32768"
            :max="65535"
            :step="1"
            class="!w-[130px]"
          />
          <el-input
            v-model="item.StatusValue"
            placeholder="状态文字，如 开"
            maxlength="20"
            class="!w-[200px]"
          />
          <el-button
            link
            type="danger"
            :icon="useRenderIcon(Delete)"
            @click="removeStatusValue(index)"
          />
        </div>
        <el-button
          link
          type="primary"
          :icon="useRenderIcon(AddFill)"
          @click="addStatusValue"
        >
          添加状态值
        </el-button>
      </div>
    </el-form-item>

    <el-divider content-position="left">采集配置</el-divider>

    <el-row :gutter="16">
      <el-col :span="12">
        <el-form-item label="采集功能码" prop="CollectFuncCode">
          <el-select v-model="formValue.CollectFuncCode" class="w-full">
            <el-option
              v-for="item in funcCodeOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>
        </el-form-item>
      </el-col>
      <el-col :span="12">
        <el-form-item label="参数地址" prop="ParamAddr">
          <el-input-number
            v-model="formValue.ParamAddr"
            :min="0"
            :max="1000000"
            :step="1"
          />
          <span class="form-tip">Modbus寄存器地址复用此列</span>
        </el-form-item>
      </el-col>
      <template v-if="formValue.CollectFuncCode > 0">
        <el-col :span="12">
          <el-form-item label="数据类型" prop="CollectDataType">
            <el-select
              v-model="formValue.CollectDataType"
              placeholder="留空=uint16"
              clearable
              class="w-full"
            >
              <el-option
                v-for="item in dataTypeOptions"
                :key="item"
                :label="item"
                :value="item"
              />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="字节序" prop="CollectByteOrder">
            <el-select
              v-model="formValue.CollectByteOrder"
              placeholder="留空=ABCD"
              clearable
              class="w-full"
            >
              <el-option
                v-for="item in byteOrderOptions"
                :key="item"
                :label="item"
                :value="item"
              />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="位偏移" prop="CollectBitOffset">
            <el-input-number
              v-model="formValue.CollectBitOffset"
              :min="-1"
              :max="63"
              :step="1"
            />
            <span class="form-tip">-1整字取值，≥0按位取布尔</span>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="占用寄存器数" prop="CollectRegLength">
            <el-input-number
              v-model="formValue.CollectRegLength"
              :min="0"
              :max="125"
              :step="1"
            />
            <span class="form-tip">0按类型推导，bcd/string须显式</span>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="是否可写" prop="CollectWritable">
            <el-switch v-model="formValue.CollectWritable" />
            <span class="form-tip">FC03保持寄存器经FC06/16下发</span>
          </el-form-item>
        </el-col>
      </template>
      <el-col :span="24">
        <el-form-item label="采集节点标识" prop="CollectNodeId">
          <el-input
            v-model="formValue.CollectNodeId"
            placeholder='OPC UA NodeId，如 ns=2;s=Demo.Tag，字符串寻址协议专用'
            maxlength="200"
            clearable
          />
        </el-form-item>
      </el-col>
    </el-row>

    <el-divider content-position="left">数据过滤</el-divider>

    <el-row :gutter="16">
      <el-col :span="24">
        <el-form-item label="合理范围过滤" prop="RangeFilterEnable">
          <el-switch v-model="formValue.RangeFilterEnable" />
          <span class="form-tip">越界值直接丢弃</span>
        </el-form-item>
      </el-col>
      <template v-if="formValue.RangeFilterEnable">
        <el-col :span="12">
          <el-form-item label="最小合法值" prop="ParamMinValue">
            <el-input-number
              v-model="formValue.ParamMinValue"
              :precision="2"
              :step="1"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="最大合法值" prop="ParamMaxValue">
            <el-input-number
              v-model="formValue.ParamMaxValue"
              :precision="2"
              :step="1"
            />
          </el-form-item>
        </el-col>
      </template>
      <el-col :span="24">
        <el-form-item label="幅度过滤" prop="AmplitudeFilterEnable">
          <el-switch v-model="formValue.AmplitudeFilterEnable" />
          <span class="form-tip">相对前值跳变过大丢弃</span>
        </el-form-item>
      </el-col>
      <template v-if="formValue.AmplitudeFilterEnable">
        <el-col :span="12">
          <el-form-item label="最大跳变量" prop="ParamChangeValue">
            <el-input-number
              v-model="formValue.ParamChangeValue"
              :min="0"
              :precision="2"
              :step="1"
            />
            <span class="form-tip">绝对差，0=不启用</span>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="最大跳变百分比" prop="MaxAmplitudePercent">
            <el-input-number
              v-model="formValue.MaxAmplitudePercent"
              :min="0"
              :precision="2"
              :step="1"
            />
            <span class="form-tip">%，0=不启用</span>
          </el-form-item>
        </el-col>
        <el-col :span="24">
          <el-form-item label="连续异常容错" prop="ContinuousFilterEnable">
            <el-switch v-model="formValue.ContinuousFilterEnable" />
            <span class="form-tip">连续N次幅度异常认定真实阶跃接受该值</span>
          </el-form-item>
        </el-col>
        <el-col v-if="formValue.ContinuousFilterEnable" :span="12">
          <el-form-item label="连续异常阈值" prop="MaxContinuousCount">
            <el-input-number
              v-model="formValue.MaxContinuousCount"
              :min="2"
              :max="100"
              :step="1"
            />
          </el-form-item>
        </el-col>
      </template>
    </el-row>

    <el-divider content-position="left">显示与统计</el-divider>

    <el-row :gutter="16">
      <el-col :span="8">
        <el-form-item label="是否显示" prop="IsShow">
          <el-switch v-model="formValue.IsShow" />
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item label="是否主显示" prop="IsMainShow">
          <el-switch v-model="formValue.IsMainShow" />
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item label="是否可配置" prop="IsSet">
          <el-switch v-model="formValue.IsSet" />
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item label="极值计算" prop="IsPeak">
          <el-switch v-model="formValue.IsPeak" />
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item label="统计计算" prop="IsReport">
          <el-switch v-model="formValue.IsReport" />
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item label="电子图默认" prop="IsMapDefault">
          <el-switch v-model="formValue.IsMapDefault" />
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item label="是否乘PT" prop="IsPt">
          <el-switch v-model="formValue.IsPt" />
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item label="是否乘CT" prop="IsCt">
          <el-switch v-model="formValue.IsCt" />
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item label="自定义告警显示" prop="IsCustomAlarm">
          <el-switch v-model="formValue.IsCustomAlarm" />
        </el-form-item>
      </el-col>
    </el-row>

    <el-divider content-position="left">告警源</el-divider>

    <el-row :gutter="16">
      <el-col :span="12">
        <el-form-item label="是否告警源" prop="IsAlarmSource">
          <el-switch v-model="formValue.IsAlarmSource" />
          <span class="form-tip">非0即告警，平台告警引擎裁决</span>
        </el-form-item>
      </el-col>
      <el-col v-if="formValue.IsAlarmSource" :span="12">
        <el-form-item label="告警类型ID" prop="AlarmConfigId">
          <el-input-number
            v-model="formValue.AlarmConfigId"
            :min="0"
            :step="1"
          />
          <span class="form-tip">alarm_config字典，继承等级/通知/防抖</span>
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

.status-values {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.status-row {
  display: flex;
  gap: 8px;
  align-items: center;
}
</style>
