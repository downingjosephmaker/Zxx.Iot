<script setup lang="ts">
import { ref, onMounted } from "vue";
import { handleTree } from "@/utils/tree";
import { getListByPage } from "@/api/system/build";
const props = defineProps({
  formInline: () => ({
    unitData: []
  })
});
const emit = defineEmits(["changeElec"]);
interface Area {
  BuildId: number;
  ParentId: number | null;
  BuildName: string;
  children?: Area[];
}

const treeRef = ref();
const areaTreeData = ref([]);
// 树配置
const defaultProps = {
  children: "children",
  label: "BuildName"
};

onMounted(() => {
  loadAreas();
});

// 点击节点加载单位数据
function handleNodeClick(node: Area) {
  emit("changeElec", node.BuildId);
}

// 加载区域数据
async function loadAreas() {
  const res = await getListByPage({
    page: 1,
    pagesize: 9999
  });
  if (res.Status && typeof res.Result === "string") {
    const areas = JSON.parse(res.Result);
    areaTreeData.value = handleTree(areas, "BuildId", "ParentId");
    emit("changeElec", areaTreeData.value[0].BuildId);
  }
}
</script>

<template>
  <div class="area-tree">
    <el-scrollbar>
      <el-tree
        ref="treeRef"
        node-key="AreaId"
        :props="defaultProps"
        :data="areaTreeData"
        :default-expand-all="true"
        @node-click="handleNodeClick"
      />
    </el-scrollbar>
  </div>
</template>

<style scoped lang="scss">
.area-tree {
  width: 100%;
  padding: 10px;
  border-right: 1px solid #eee;
  height: 88vh;
  background-color: #f5f7fa;
  border-radius: 6px 0 0 6px;
  overflow: hidden;
  overflow-y: auto;

  :deep(.el-tree) {
    background-color: transparent;
    font-size: 14px;
    color: #333;

    .el-tree-node__content {
      height: 36px;
      line-height: 36px;
      padding-left: 10px;
      border-radius: 4px;
      transition: all 0.2s ease;

      &:hover {
        background-color: #eef3fc;
      }
    }

    .el-tree-node__children {
      .el-tree-node__content {
        padding-left: 30px;
      }
    }

    // 当前选中节点样式
    .el-tree-node.is-current > .el-tree-node__content {
      background-color: #dbe9ff;
      color: #1a73e8;
      font-weight: bold;
    }
  }
}
</style>
