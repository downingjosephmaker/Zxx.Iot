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

// import Dept from "~icons/ri/git-branch-line";
// import Reset from "~icons/ri/restart-line";
import More2Fill from "~icons/ri/more-2-fill";
import OfficeBuilding from "~icons/ep/office-building";
import LocationCompany from "~icons/ep/add-location";
import Building from "~icons/ep/school";
import ExpandIcon from "@/assets/svg/expand.svg?component";
import UnExpandIcon from "@/assets/svg/unexpand.svg?component";
import { message } from "@/utils/message";
interface Tree {
  id: number;
  DeviceId?: string | number;
  DeviceName?: string;
  BuildName?: string;
  highlight?: boolean;
  children?: Tree[];
  [key: string]: any; // 允许其他任意属性
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
  isMore: {
    type: Boolean,
    default: false
  },
  clickNode: {
    type: Boolean,
    default: true
  },
  // 控制是否自动选择第一个节点（避免重复选择）
  autoSelect: {
    type: Boolean,
    default: false
  },
  whObj: {
    type: Object,
    default: () => ({
      height: "70vh"
    })
  }
});

const emit = defineEmits(["treeSelect"]);

const treeRef = ref();
const isExpand = ref(true);
const searchValue = ref("");
const highlightMap = ref({});
const currentNode = ref(null); // 当前选中的节点
const { proxy } = getCurrentInstance();
const defaultProps = {
  children: "children",
  label: data => {
    if (data.DeviceName) return data.DeviceName;
    if (data.BuildName) return data.BuildName;
    if (data.DeviceId) return `设备-${data.DeviceId}`;
    if (data.id) return `节点-${data.id}`;
    return "未命名节点";
  }
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

const filterNode = (value: string, data: Tree) => {
  if (!value) return true;
  const nodeName = data.DeviceName || data.BuildName || "";
  return nodeName.includes(value);
};

// 处理节点点击事件
function nodeClick(data: Tree, node: any) {
  // 检查是否满足选择条件
  if (props.levelSelect == 3 && data.BuildLevel != 3) {
    message("请选择3级节点", { type: "error" });
    return;
  }

  // 设置当前选中节点
  currentNode.value = data;

  // 获取当前节点类型，如果有 DeviceId 则为设备，否则为建筑
  const queryType = "DeviceId" in data ? "device" : "building";

  // 触发选择事件，传递查询类型
  emit("treeSelect", data, queryType);
}

function toggleRowExpansionAll(status) {
  isExpand.value = status;
  const nodes = (proxy.$refs["treeRef"] as any).store._getAllNodes();
  for (let i = 0; i < nodes.length; i++) {
    nodes[i].expanded = status;
  }
}

/** 重置树状态（搜索框值、树初始化） */
function onTreeReset() {
  highlightMap.value = {};
  searchValue.value = "";
  currentNode.value = null;
  toggleRowExpansionAll(true);
}

// 默认选中第一个节点
const selectFirstNode = () => {
  if (!props.treeData || props.treeData.length === 0) return;

  nextTick(() => {
    const firstNode = props.treeData[0] as Tree;
    if (firstNode) {
      currentNode.value = firstNode;
      // 检查节点类型，如果有DeviceId属性，则为设备，否则为建筑
      const queryType = "DeviceId" in firstNode ? "device" : "building";
      emit("treeSelect", firstNode, queryType);
    }
  });
};

// 监听树数据变化，当树数据加载完成且没有选中节点时，自动选择第一个节点
// 移除immediate和onMounted自动选择，避免重复触发
watch(
  () => props.treeData,
  newVal => {
    // 只有当显式请求自动选择时才执行
    if (props.autoSelect && newVal && newVal.length > 0 && !currentNode.value) {
      selectFirstNode();
    }
  },
  { deep: true }
);

watch(searchValue, val => {
  console.log(val);
  treeRef.value!.filter(val);
});

defineExpose({ onTreeReset });
</script>

<template>
  <div v-loading="props.treeLoading" class="h-full bg-bg_color overflow-auto">
    <div class="flex items-center h-[34px]">
      <el-input
        v-model="searchValue"
        class="ml-2"
        size="small"
        :placeholder="'请输入名称'"
        clearable
      >
        <template #suffix>
          <el-icon class="el-input__icon">
            <IconifyIconOffline
              v-show="searchValue.length === 0"
              icon="search"
            />
          </el-icon>
        </template>
      </el-input>
      <el-dropdown :hide-on-click="false">
        <IconifyIconOffline
          class="w-[28px] cursor-pointer"
          width="18px"
          :icon="More2Fill"
        />
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item>
              <el-button
                :class="buttonClass"
                link
                type="primary"
                :icon="useRenderIcon(isExpand ? ExpandIcon : UnExpandIcon)"
                @click="toggleRowExpansionAll(isExpand ? false : true)"
              >
                {{ isExpand ? "折叠全部" : "展开全部" }}
              </el-button>
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>
    <el-divider />
    <el-tree
      ref="treeRef"
      :data="props.treeData"
      node-key="id"
      size="small"
      :props="defaultProps"
      :current-node-key="currentNode?.id"
      highlight-current
      default-expand-all
      :style="'height:' + whObj.height"
      :expand-on-click-node="false"
      :filter-node-method="filterNode"
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
            currentNode?.id === node.key ? 'tree-node-selected' : '',
            searchValue.trim().length > 0 &&
              node.label.includes(searchValue) &&
              'text-red-500',
            highlightMap[node.id]?.highlight ? 'dark:text-primary' : ''
          ]"
          :style="{
            color:
              highlightMap[node.id]?.highlight || currentNode?.id === node.key
                ? 'var(--el-color-primary)'
                : '',
            background:
              highlightMap[node.id]?.highlight || currentNode?.id === node.key
                ? 'var(--el-color-primary-light-8)'
                : 'transparent'
          }"
        >
          <IconifyIconOffline
            :icon="
              data.DeviceId
                ? data.type === 1
                  ? OfficeBuilding
                  : data.type === 2
                    ? LocationCompany
                    : ''
                : Building
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
  // height: 100%;
  overflow: hidden;
  overflow-y: auto;

  /* 增强树节点选中时的样式 */
  .el-tree-node.is-current > .el-tree-node__content {
    background-color: var(--el-color-primary-light-9) !important;
    font-weight: bold;
    border-radius: 4px;
  }
}

.tree-node-selected {
  font-weight: bold !important;
  border-left: 2px solid var(--el-color-primary) !important;
  padding-left: 4px !important;
}
</style>
