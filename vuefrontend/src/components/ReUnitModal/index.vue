<script setup lang="ts">
import { ref, onMounted } from "vue";
import { handleTree } from "@/utils/tree";
import { SysAreaList } from "@/api/system";
import { GetQxListByPage } from "@/api/system";
import { storage } from "@/utils/storage";
import gsIcon from "@/assets/svg/gs2.svg?component";

interface Unit {
  UnitId: number;
  UnitName: string;
  UnitCode: string;
  UnitAddress: string;
}

interface Area {
  AreaId: number;
  ParentId: number | null;
  AreaName: string;
  children?: Area[];
}

const ruleFormRef = ref();

const visible = ref(false);
const treeRef = ref();
const areaTreeData = ref<Area[]>([]);
const currentUnits = ref<Unit[]>([]);
const selectedUnit = ref<Unit | null>(null);
const selectedUnit2 = ref<Unit | null>(null);
// 树配置
const defaultProps = {
  children: "children",
  label: "AreaName"
};

onMounted(() => {
  loadAreas();
  getUnitData("");
});

// 加载区域数据
async function loadAreas() {
  const res = await SysAreaList({
    page: 1,
    pagesize: 9999,
    sconlist: [
      {
        ParamName: "ExpandJson.IsDisplay",
        ParamType: "=",
        ParamValue: "true"
      }
    ]
  });
  if (res.Status && typeof res.Result === "string") {
    const areas = JSON.parse(res.Result);
    areaTreeData.value = handleTree(areas, "AreaId", "ParentId");
  }
}

// 点击节点加载单位数据
function handleNodeClick(node: Area) {
  getUnitData(node.AreaId);
}

async function getUnitData(ParamValue) {
  const datas = {
    page: 1,
    pageSize: 999,
    sconlist: [
      {
        ParamName: "AreaId",
        ParamType: "like",
        ParamValue: ParamValue
      }
    ]
  };
  await GetQxListByPage(datas).then(res => {
    if (res.Status) {
      currentUnits.value = JSON.parse(res.Result);
      if (!ParamValue) {
        const datas = storage.getItem("user-info");
        currentUnits.value.forEach((item: Unit) => {
          if (item.UnitName == datas.unitname) {
            selectedUnit2.value = item;
          }
        });
      }
    }
  });
}
// 选择单位
function selectUnit(unit: Unit) {
  selectedUnit.value = unit;
}

function getRef() {
  return selectedUnit.value;
}
defineExpose({ getRef });
</script>

<template>
  <div class="unit-modal-container">
    <!-- 左侧区域树 -->
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

    <!-- 右侧单位卡片 -->
    <div class="unit-card-list">
      <!-- 当前选中单位展示 -->
      <div v-if="selectedUnit2" class="selected-unit-info">
        <h4>当前单位</h4>
        <div class="selected-unit-card">
          <div class="icon-placeholder">
            <gsIcon class="w-full h-full" />
          </div>
          <div class="text-content">
            <div class="title">{{ selectedUnit2.UnitName }}</div>
            <div class="info">编号：{{ selectedUnit2.UnitCode }}</div>
            <div class="info">地址：{{ selectedUnit2.UnitAddress }}</div>
          </div>
        </div>
      </div>
      <el-row :gutter="20" class="mt-2">
        <el-col
          v-for="unit in currentUnits"
          :key="unit.UnitId"
          class="mt-4"
          :span="7"
        >
          <el-card
            shadow="hover"
            class="unit-card"
            :class="{ active: selectedUnit?.UnitId === unit.UnitId }"
            @click="selectUnit(unit)"
          >
            <div class="card-content">
              <!-- 图标占位 -->
              <div class="icon-placeholder">
                <!-- 此处可以插入图标组件或图片 -->
                <!-- 示例：使用 Element Plus 的 icon -->
                <!-- <el-icon><Building /></el-icon> -->
                <gsIcon class="w-full h-full" />
              </div>
              <div class="text-content">
                <div class="title">{{ unit.UnitName }}</div>
                <div class="info">编号：{{ unit.UnitCode }}</div>
                <div class="info">地址：{{ unit.UnitAddress }}</div>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </div>
  </div>
</template>

<style scoped lang="scss">
.unit-modal-container {
  display: flex;
  height: 600px;
  background-color: #ffffff; // 纯白背景
  border-radius: 8px;
  overflow: hidden !important;
}

.area-tree {
  width: 20%;
  padding: 10px;
  border-right: 1px solid #eee;
  height: 100%;
  background-color: #f5f7fa;
  border-radius: 6px 0 0 6px;

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
.unit-card-list {
  width: 80%;
  padding-left: 10px;
  height: 100%;
  background-color: #fff;
  border-radius: 0 6px 6px 0;
}

.unit-card {
  cursor: pointer;
  transition: all 0.3s ease;
  border-radius: 6px;
  &:hover {
    transform: translateY(-5px);
    box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
  }

  &.active {
    background-color: #e6f0ff;
    border-color: #409eff;
    box-shadow: 0 0 8px rgba(64, 158, 255, 0.3);
    transform: scale(1.02);
  }

  .card-content {
    display: flex;
    align-items: center;
    padding: 12px;
  }

  .icon-placeholder {
    width: 36px;
    height: 36px;
    margin-right: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 20px;
    color: #666;
    background-color: #f0f2f5;
    border-radius: 50%;
    flex-shrink: 0;
  }

  .text-content {
    flex-grow: 1;
  }

  .title {
    font-weight: bold;
    margin-bottom: 4px;
    color: #333;
  }

  .info {
    color: #777;
    font-size: 12px;
  }
  .title {
    font-weight: bold;
    margin-bottom: 6px;
    color: #333;
  }

  .info {
    color: #777;
    font-size: 12px;
  }
}

.selected-unit-info {
  padding: 16px;
  margin-bottom: 16px;
  background-color: #f8f9fa;
  border-radius: 6px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.05);
}

.selected-unit-card {
  display: flex;
  align-items: center;
  padding: 12px;
  background-color: #fff;
  border-radius: 6px;
  border: 1px solid #e4e7ed;

  .icon-placeholder {
    width: 36px;
    height: 36px;
    margin-right: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 20px;
    color: #666;
    background-color: #f0f2f5;
    border-radius: 50%;
    flex-shrink: 0;
  }

  .text-content {
    flex-grow: 1;
  }

  .title {
    font-weight: bold;
    margin-bottom: 4px;
    color: #333;
  }

  .info {
    color: #777;
    font-size: 12px;
  }
}
</style>
