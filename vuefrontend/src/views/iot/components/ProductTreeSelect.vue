<script setup lang="ts">
import { onMounted, ref } from "vue";
import { getListByPage, type DeviceTypeItem } from "@/api/iot/devicetype";

defineOptions({
  name: "ProductTreeSelect"
});

const props = withDefaults(
  defineProps<{
    modelValue: string;
    placeholder?: string;
  }>(),
  { placeholder: "选择产品类型" }
);

const emit = defineEmits<{
  (e: "update:modelValue", v: string): void;
}>();

interface TreeOption {
  value: string;
  label: string;
  children?: TreeOption[];
}

const treeData = ref<TreeOption[]>([]);

/** 平铺清单按ParentId构树，孤儿节点(父级缺失)提升为根 */
function buildTree(list: DeviceTypeItem[]): TreeOption[] {
  const nodeMap = new Map<string, TreeOption>();
  list.forEach(t =>
    nodeMap.set(t.TypeCode, {
      value: t.TypeCode,
      label: `${t.TypeName}(${t.TypeCode})`,
      children: []
    })
  );
  const roots: TreeOption[] = [];
  list.forEach(t => {
    const node = nodeMap.get(t.TypeCode)!;
    if (t.ParentId && nodeMap.has(t.ParentId)) {
      nodeMap.get(t.ParentId)!.children!.push(node);
    } else {
      roots.push(node);
    }
  });
  nodeMap.forEach(n => {
    if (!n.children!.length) delete n.children;
  });
  return roots;
}

onMounted(async () => {
  const data = await getListByPage({ page: 1, pagesize: 10000, sconlist: [] });
  if (data.Status) treeData.value = buildTree(JSON.parse(data.Result));
});
</script>

<template>
  <el-tree-select
    :model-value="props.modelValue || undefined"
    :data="treeData"
    check-strictly
    :render-after-expand="false"
    default-expand-all
    filterable
    clearable
    :placeholder="props.placeholder"
    class="w-full"
    @update:model-value="(v: string) => emit('update:modelValue', v ?? '')"
  />
</template>
