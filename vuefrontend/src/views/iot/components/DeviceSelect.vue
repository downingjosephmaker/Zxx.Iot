<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { getListByPage, type DeviceInfoItem } from "@/api/iot/device";

defineOptions({
  name: "DeviceSelect"
});

const props = withDefaults(
  defineProps<{
    modelValue: string | number | null;
    /** true=值为String(DeviceId)（策略ScopeId场景），false=number（上级设备ID场景） */
    stringValue?: boolean;
    /** 头部固定"顶级设备"选项(值0，上级设备场景) */
    topLevel?: boolean;
    /** 排除的设备ID(编辑设备时防自挂) */
    excludeId?: number;
    placeholder?: string;
  }>(),
  {
    stringValue: false,
    topLevel: false,
    excludeId: 0,
    placeholder: "输入设备名称搜索"
  }
);

const emit = defineEmits<{
  (e: "update:modelValue", v: string | number): void;
}>();

interface DeviceOption {
  value: string | number;
  label: string;
}

const options = ref<DeviceOption[]>([]);
const loading = ref(false);

const emptyValue = computed(() => (props.stringValue ? "" : 0));

/** 顶级设备0是合法选中值，仅空串/null视作未选 */
const inner = computed<string | number | undefined>({
  get: () =>
    props.modelValue === null || props.modelValue === ""
      ? undefined
      : props.modelValue,
  set: v => emit("update:modelValue", v ?? emptyValue.value)
});

function toValue(id: number): string | number {
  return props.stringValue ? String(id) : id;
}

function toOptions(list: DeviceInfoItem[]): DeviceOption[] {
  return list
    .filter(d => !props.excludeId || d.DeviceId !== props.excludeId)
    .map(d => ({
      value: toValue(d.DeviceId),
      label: `${d.DeviceName}（${d.DeviceId}）`
    }));
}

/** 已选项并入新选项，防远程搜索换页后label丢失 */
function mergeSelected(next: DeviceOption[]): DeviceOption[] {
  const cur = props.modelValue;
  if (cur !== null && cur !== "" && cur !== 0 && !next.some(o => o.value === cur)) {
    const kept = options.value.find(o => o.value === cur);
    if (kept) next.unshift(kept);
  }
  if (props.topLevel && !next.some(o => o.value === 0)) {
    next.unshift({ value: 0, label: "顶级设备" });
  }
  return next;
}

async function search(kw: string) {
  loading.value = true;
  try {
    const data = await getListByPage({
      page: 1,
      pagesize: 50,
      sconlist: kw
        ? [{ ParamName: "DeviceName", ParamType: "like", ParamValue: kw }]
        : []
    });
    if (data.Status) {
      options.value = mergeSelected(toOptions(JSON.parse(data.Result)));
    }
  } finally {
    loading.value = false;
  }
}

/** 编辑回显：当前值不在首页选项中时按DeviceId精确查兜底 */
async function echoSelected() {
  const cur = props.modelValue;
  if (cur === null || cur === "" || cur === 0 || cur === "0") return;
  if (options.value.some(o => o.value === cur)) return;
  const data = await getListByPage({
    page: 1,
    pagesize: 1,
    sconlist: [{ ParamName: "DeviceId", ParamType: "=", ParamValue: String(cur) }]
  });
  const list: DeviceInfoItem[] = data.Status ? JSON.parse(data.Result) : [];
  const opt = list.length
    ? toOptions(list)[0]
    : { value: cur, label: `设备${cur}（已不存在）` };
  if (opt && !options.value.some(o => o.value === cur)) {
    options.value.unshift(opt);
  }
}

onMounted(async () => {
  await search("");
  await echoSelected();
});
</script>

<template>
  <el-select
    v-model="inner"
    filterable
    remote
    :remote-method="search"
    :loading="loading"
    clearable
    :placeholder="props.placeholder"
    class="w-full"
  >
    <el-option
      v-for="item in options"
      :key="item.value"
      :label="item.label"
      :value="item.value"
    />
  </el-select>
</template>
