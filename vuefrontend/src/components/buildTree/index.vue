<script setup lang="ts">
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import {
  ref,
  computed,
  watch,
  getCurrentInstance,
  onMounted,
  nextTick
} from "vue";

import Dept from "~icons/ri/git-branch-line";

import More2Fill from "~icons/ri/more-2-fill";
import OfficeBuilding from "~icons/ep/office-building";
import LocationCompany from "~icons/ep/add-location";
import ExpandIcon from "@/assets/svg/expand.svg?component";
import UnExpandIcon from "@/assets/svg/unexpand.svg?component";
import { cloneDeep } from "@pureadmin/utils";
import { message } from "@/utils/message";

interface Tree {
  id: number;
  name: string;
  label: string;
  highlight?: boolean;
  children?: Tree[];
}

const props = defineProps({
  treeLoading: Boolean,
  treeData: Array,
  defaultTreeData: Array<any>,
  level: {
    type: Number,
    default: 99
  },
  levelSelect: {
    type: Number,
    default: 0
  },
  checkedFlag: {
    type: Boolean,
    default: false
  },
  isOne: {
    type: Boolean,
    default: false
  },
  clickNode: {
    type: Boolean,
    default: true
  }
});

const latestTreeData = ref();
const treeRef = ref();
const buildId = ref();
const isExpand = ref(true);
const searchValue = ref("");
const highlightMap = ref({});
const { proxy } = getCurrentInstance();
const defaultProps = {
  children: "children",
  label: "BuildName"
};

const buttonClass = computed(() => {
  return [
    "!h-[20px]",
    "reset-margin",
    "!text-gray-500",
    "dark:!text-white",
    "dark:hover:!text-primary"
  ];
});
const lossTree = ref([]);

const formatTree = (tree, index) => {
  tree.forEach(item => {
    if (index == props.level) {
      const data = cloneDeep(item);
      lossTree.value.push(JSON.stringify(data));
    } else {
      if (item.children) formatTree(item.children, index + 1);
    }
  });
};

const formatTree1 = (tree, index) => {
  tree.forEach(item => {
    if (index == props.level) {
      item.children = [];
    } else {
      if (item.children) formatTree1(item.children, index + 1);
    }
  });
};

const getChildrenTreeByBuildId = id => {
  console.log(lossTree);
  return lossTree.value.find(item => {
    return JSON.parse(item).BuildId == id;
  });
};

onMounted(() => {
  latestTreeData.value = cloneDeep(props.treeData);
  nextTick(() => {
    formatTree(props.treeData, 1);
    formatTree1(latestTreeData.value, 1);
  });
});

const handleCheckChange = (checkedNodes: any) => {
  console.log(checkedNodes, "checkedNodes==============");
  treeRef.value.setCheckedNodes([checkedNodes]); // 选中已选中节点
  buildId.value = checkedNodes.BuildId;
};
const filterNode = (value: string, data: Tree) => {
  if (!value) return true;
  return data.label.includes(value);
};

function nodeClick(value: any) {
  if (!props.clickNode) {
    const node = this.$refs.treeRef.getNode(value);
    node.setChecked(!node.checked);
  }
  return;
}

function toggleRowExpansionAll(status: boolean) {
  isExpand.value = status;
  const nodes = (proxy.$refs["treeRef"] as any).store._getAllNodes();
  for (let i = 0; i < nodes.length; i++) {
    nodes[i].expanded = status;
  }
}

/** 重置部门树状态（选中状态、搜索框值、树初始化） */
function onTreeReset() {
  highlightMap.value = {};
  searchValue.value = "";
  toggleRowExpansionAll(true);
}

/** 重置部门树状态（选中状态、搜索框值、树初始化） */
function getTreeRef() {
  return treeRef.value;
}

function getBuildId() {
  return buildId.value;
}

watch(searchValue, val => {
  treeRef.value!.filter(val);
});

defineExpose({ onTreeReset, getTreeRef, getChildrenTreeByBuildId, getBuildId });
</script>

<template>
  <div
    v-loading="props.treeLoading"
    class="h-full bg-bg_color overflow-auto"
    :style="{ height: `50vh` }"
  >
    <el-tree
      ref="treeRef"
      :data="latestTreeData"
      node-key="BuildId"
      size="small"
      :props="defaultProps"
      default-expand-all
      :highlight-current="true"
      :expand-on-click-node="false"
      :check-on-click-node="clickNode"
      :default-expand-level="2"
      :filter-node-method="filterNode"
      :check-strictly="!clickNode"
      :default-checked-keys="defaultTreeData"
      @check="handleCheckChange"
      @node-click="nodeClick"
    >
      <template #default="{ node, data }">
        <span
          :class="[
            'pl-1',
            'pr-1',
            'rounded',
            'flex',
            'items-center',
            'select-none',
            'hover:text-primary',
            searchValue.trim().length > 0 &&
              node.label.includes(searchValue) &&
              'text-red-500',
            highlightMap[node.id]?.highlight ? 'dark:text-primary' : ''
          ]"
          :style="{
            color: highlightMap[node.id]?.highlight
              ? 'var(--el-color-primary)'
              : '',
            background: highlightMap[node.id]?.highlight
              ? 'var(--el-color-primary-light-7)'
              : 'transparent'
          }"
        >
          <IconifyIconOffline
            :icon="
              data.type === 1
                ? OfficeBuilding
                : data.type === 2
                  ? LocationCompany
                  : Dept
            "
          />
          {{ node.label }}
        </span>
      </template>
    </el-tree>
  </div>
</template>

<style lang="scss" scoped>
:deep(.el-divider) {
  margin: 0;
}

:deep(.el-tree) {
  --el-tree-node-hover-bg-color: transparent;
  height: 90%;
  overflow-y: auto;
}
</style>
import { ElSelect } from "element-plus";import { error } from "console";
