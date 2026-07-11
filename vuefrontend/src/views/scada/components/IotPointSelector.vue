<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import {
  getListByPage as getDevicePage,
  type DeviceInfoItem
} from "@/api/iot/device";
import { getListByPage as getParamPage } from "@/api/iot/typeparam";
import type {
  IotDatasetPoint,
  IotDatasetConfig
} from "../core/DatasetRuntime";

defineOptions({
  name: "IotPointSelector"
});

/** 父级(DatasetPanel)持有的响应式配置对象，本组件直接写回其字段 */
const props = defineProps<{
  config: Pick<
    IotDatasetConfig,
    | "deviceId"
    | "deviceName"
    | "deviceTypeCode"
    | "mode"
    | "points"
    | "historyHours"
    | "historyMode"
  > & { deviceId: number | null };
}>();

const deviceOptions = ref<DeviceInfoItem[]>([]);
const deviceLoading = ref(false);
const paramOptions = ref<IotDatasetPoint[]>([]);
const paramLoading = ref(false);

async function searchDevices(kw: string) {
  deviceLoading.value = true;
  try {
    const data = await getDevicePage({
      page: 1,
      pagesize: 50,
      sconlist: kw
        ? [{ ParamName: "DeviceName", ParamType: "like", ParamValue: kw }]
        : []
    });
    if (data.Status) {
      const list: DeviceInfoItem[] = JSON.parse(data.Result);
      // 已选设备并入选项，防远程搜索换词后label丢失
      const cur = props.config.deviceId;
      if (cur && !list.some(d => d.DeviceId === cur)) {
        const kept = deviceOptions.value.find(d => d.DeviceId === cur);
        if (kept) list.unshift(kept);
      }
      deviceOptions.value = list;
    }
  } finally {
    deviceLoading.value = false;
  }
}

/** 编辑回显：当前值不在首页选项中时按DeviceId精确查兜底 */
async function echoDevice() {
  const cur = props.config.deviceId;
  if (!cur || deviceOptions.value.some(d => d.DeviceId === cur)) return;
  const data = await getDevicePage({
    page: 1,
    pagesize: 1,
    sconlist: [
      { ParamName: "DeviceId", ParamType: "=", ParamValue: String(cur) }
    ]
  });
  if (data.Status) {
    const list: DeviceInfoItem[] = JSON.parse(data.Result);
    if (list.length) deviceOptions.value.unshift(list[0]);
  }
}

async function loadParams() {
  paramOptions.value = [];
  if (!props.config.deviceTypeCode) return;
  paramLoading.value = true;
  try {
    const data = await getParamPage({
      page: 1,
      pagesize: 1000,
      sconlist: [
        {
          ParamName: "DeviceTypeCode",
          ParamType: "=",
          ParamValue: props.config.deviceTypeCode
        }
      ]
    });
    if (data.Status) {
      const list = JSON.parse(data.Result) as {
        ParamCode: string;
        ParamName?: string;
        ValueUnit?: string;
      }[];
      paramOptions.value = list.map(p => ({
        ParamCode: p.ParamCode,
        ParamName: p.ParamName,
        ValueUnit: p.ValueUnit
      }));
    }
  } finally {
    paramLoading.value = false;
  }
}

function onDeviceChange(id: number | null) {
  const item = deviceOptions.value.find(d => d.DeviceId === id);
  props.config.deviceName = item?.DeviceName || "";
  props.config.deviceTypeCode = item?.DeviceTypeCode || "";
  props.config.points = [];
  loadParams();
}

/** 点位多选与config.points({ParamCode,ParamName,ValueUnit}[])的双向映射 */
const selectedCodes = computed<string[]>({
  get: () => (props.config.points || []).map(p => p.ParamCode),
  set: codes => {
    props.config.points = codes.map(
      code =>
        paramOptions.value.find(p => p.ParamCode === code) ??
        props.config.points?.find(p => p.ParamCode === code) ?? {
          ParamCode: code
        }
    );
  }
});

onMounted(async () => {
  await searchDevices("");
  await echoDevice();
  if (props.config.deviceTypeCode) await loadParams();
});
</script>

<template>
  <el-form label-width="100px" size="small">
    <el-form-item label="绑定设备" required>
      <el-select
        :model-value="props.config.deviceId ?? undefined"
        filterable
        remote
        :remote-method="searchDevices"
        :loading="deviceLoading"
        clearable
        placeholder="输入设备名称搜索"
        style="width: 100%"
        @update:model-value="
          (v: number | undefined) => {
            props.config.deviceId = v ?? null;
            onDeviceChange(v ?? null);
          }
        "
      >
        <el-option
          v-for="item in deviceOptions"
          :key="item.DeviceId"
          :label="`${item.DeviceName}（${item.DeviceTypeName || item.DeviceTypeCode}）`"
          :value="item.DeviceId"
        />
      </el-select>
    </el-form-item>

    <el-form-item label="绑定点位" required>
      <el-select
        v-model="selectedCodes"
        multiple
        filterable
        collapse-tags
        collapse-tags-tooltip
        :loading="paramLoading"
        :placeholder="
          props.config.deviceId ? '选择要绑定的点位(可多选)' : '请先选择设备'
        "
        style="width: 100%"
      >
        <el-option
          v-for="item in paramOptions"
          :key="item.ParamCode"
          :label="`${item.ParamName || item.ParamCode}（${item.ParamCode}${item.ValueUnit ? '，' + item.ValueUnit : ''}）`"
          :value="item.ParamCode"
        />
      </el-select>
      <div style="margin-top: 4px; font-size: 12px; color: #909399">
        点位来自该设备产品类型的点表(device_type_param)
      </div>
    </el-form-item>

    <el-form-item label="数据模式">
      <el-radio-group
        :model-value="props.config.mode"
        @update:model-value="(v: 'realtime' | 'history') => (props.config.mode = v)"
      >
        <el-radio-button value="realtime">实时(最新值+推送)</el-radio-button>
        <el-radio-button value="history">历史曲线</el-radio-button>
      </el-radio-group>
    </el-form-item>

    <template v-if="props.config.mode === 'history'">
      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="回看时长(时)">
            <el-input-number
              :model-value="props.config.historyHours"
              :min="1"
              :max="720"
              :step="1"
              style="width: 100%"
              @update:model-value="
                (v: number | undefined) => (props.config.historyHours = v || 24)
              "
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="聚合方式">
            <el-select
              :model-value="props.config.historyMode"
              style="width: 100%"
              @update:model-value="
                (v: 'auto' | 'raw' | 'hour') => (props.config.historyMode = v)
              "
            >
              <el-option label="自动(≤48h原始点,否则小时聚合)" value="auto" />
              <el-option label="原始点(30天保留窗内)" value="raw" />
              <el-option label="小时聚合" value="hour" />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>
    </template>
  </el-form>
</template>
