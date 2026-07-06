<script setup lang="ts">
import { useRenderIcon } from "../ReIcon/src/hooks";
import { ref, onMounted, h, watch } from "vue";
import tree from "./tree.vue";
import Search from "~icons/ep/search";
import { addDialog } from "../ReDialog/index";
import { handleTree } from "@/utils/tree";
import buildTree from "../buildTree/index.vue";
import { getListByPage } from "@/api/system/build";
import {
  GetReprotList,
  GetRunListByBuild,
  GetPeakList,
  GetSubList
} from "@/api/information/equipment";
import { cloneDeep } from "@pureadmin/utils";
import { QueryTableParams } from "@/api/type";
import { GetTempList } from "@/api/equipmentManagement/equipment";
import {
  GetMasterTypeList,
  GetHistoryTypes,
  GetReportTypes
} from "@/api/system/equipmentType";
import { useRoute } from "vue-router";

const treeRef = ref();
const treeRef2 = ref();
const latestTreeData = ref();
// 设备查询模式下选中的建筑
const defaultTreeData = ref([]);
// 建筑查询模式下选中的建筑
const selectedBuildingId = ref("");
const selectedBuildingName = ref("");
const treeData = ref([]);
const treeData2 = ref([]);
const treeLoading = ref(false);
const input3 = ref("");
const displayType = ref("device");
const whObj = ref({
  height: "70vh"
});
// 查询类型选项
const queryTypeOptions = [
  { value: "device", label: "设备查询" },
  { value: "building", label: "建筑查询" }
  // 后续可以添加更多选项，如：
  // { value: "department", label: "部门查询" }
];

// 设备类型筛选相关
const deviceTypeOptions = ref([]); // 设备类型选项列表
const selectedDeviceType = ref("zndb"); // 选中的设备类型
const deviceTypeLoading = ref(false); // 设备类型加载状态
const props = defineProps({
  level: {
    type: Number,
    default: 99
  },
  levelSelect: {
    type: Number,
    default: 3
  },
  buildType: {
    type: String,
    default: ""
  },
  temperature: {
    type: Boolean,
    default: false
  },
  hideQueryType: {
    type: Boolean,
    default: false
  },
  defaultQueryType: {
    type: String,
    default: "device" // 默认为设备查询
  },
  apiType: {
    type: Number,
    default: 1
  }
});
onMounted(async () => {
  // 加载设备类型数据
  await loadDeviceTypes();
  // 加载建筑数据
  await loadBuildingData();
  // 如果有指定默认查询类型，则使用指定的类型
  if (props.defaultQueryType && props.defaultQueryType !== displayType.value) {
    displayType.value = props.defaultQueryType;
    handleDisplayTypeChange(props.defaultQueryType);
  }
  // 否则根据初始显示类型决定显示内容
  else if (displayType.value === "building") {
    handleDisplayTypeChange("building");
  }
  if (props.level == 5) {
    selectedDeviceType.value = "zhkt";
    whObj.value.height = "77vh";
  }
  if (props.level == 6) {
    whObj.value.height = "58vh";
  }
});
// 加载设备类型列表
const loadDeviceTypes = async () => {
  deviceTypeLoading.value = true;
  try {
    const route = useRoute();
    const params = {
      menucode: route.name as string
    };
    let response;
    if (props.apiType == 1) {
      response = await GetReportTypes("");
    } else if (props.apiType == 2) {
      response = await GetHistoryTypes("");
    }
    if (response && response.Status) {
      const types = JSON.parse(response.Result);
      deviceTypeOptions.value = types.map(type => ({
        value: type.DeviceTypeCode,
        label: type.DeviceTypeName
      }));
    } else {
      console.error("获取设备类型列表失败");
    }
  } catch (error) {
    console.error("加载设备类型出错:", error);
  } finally {
    deviceTypeLoading.value = false;
  }
};

// 监听默认查询类型的变化
watch(
  () => props.defaultQueryType,
  newType => {
    if (newType && newType !== displayType.value) {
      displayType.value = newType;
      handleDisplayTypeChange(newType);
    }
  }
);

const loadBuildingData = async () => {
  const params: QueryTableParams = {
    page: 1,
    pagesize: 999,
    sconlist: [
      {
        ParamName: "BuildCategory",
        ParamType: "in",
        ParamValue: props.buildType
      }
    ]
  };
  const data = await getListByPage(params);
  let newData = JSON.parse(data.Result);
  console.log(newData);
  treeData2.value = handleTree(newData, "BuildId", "ParentId");
  latestTreeData.value = cloneDeep(treeData2.value);

  if (props.level == 2) {
    treeData2.value[0].children.forEach((e, index) => {
      if (index === 0) {
        defaultTreeData.value.push(e.BuildId);
        input3.value = e.BuildName;
        getDeviceByBuildId(e.BuildId);
      }
    });
    treeData.value = latestTreeData.value[0].children[0].children;
  } else if (
    props.level == 3 &&
    treeData2.value &&
    treeData2.value.length > 0
  ) {
    if (treeData2.value[0].children) {
      treeData2.value[0].children[0].children.forEach((e, index) => {
        if (index === 0) {
          defaultTreeData.value.push(e.BuildId);
          input3.value = e.BuildName;
          getDeviceByBuildId(e.BuildId);
        }
      });
      treeData.value = latestTreeData.value[0].children[0].children[0].children;
    } else {
      defaultTreeData.value.push(treeData2.value[0].BuildId);
      input3.value = treeData2.value[0].BuildName;
      getDeviceByBuildId(treeData2.value[0].BuildId);
    }
  } else {
    defaultTreeData.value.push(treeData2.value[0].BuildId);
    input3.value = treeData2.value[0].BuildName;
    getDeviceByBuildId(treeData2.value[0].BuildId);
  }
};

async function handleMenu() {
  addDialog({
    props: {
      formInline: {}
    },
    width: "40%",
    draggable: true,
    fullscreenIcon: true,
    closeOnClickModal: false,
    contentRenderer: () =>
      h(buildTree, {
        ref: treeRef,
        treeData: treeData2.value,
        defaultTreeData: defaultTreeData.value,
        level: props.level,
        levelSelect: props.levelSelect
      }),
    beforeSure: done => {
      const tree = treeRef.value.getTreeRef();
      const tree2 = tree.getCheckedNodes();

      // 只在设备查询模式下更新选中的建筑
      defaultTreeData.value = [tree2[0].BuildId];
      input3.value = tree2[0].BuildName;

      // 根据当前显示类型决定是否自动加载设备
      if (displayType.value === "device") {
        // 如果当前是设备视图，加载设备并触发选中事件
        getDeviceByBuildId(tree2[0].BuildId, 1);
      }
      done();
    }
  });
}

const getDeviceByBuildId = async (id, type = 1) => {
  // 显示加载状态
  treeLoading.value = true;

  try {
    let result;
    let params = {
      buildid: id,
      devicetype: selectedDeviceType.value || ""
    };
    // 调用API获取设备数据
    if (props.apiType == 1) {
      whObj.value.height = "56vh";
      result = await GetReprotList(params);
    } else if (props.apiType == 3) {
      result = await GetPeakList(params);
    } else if (props.apiType == 4) {
      result = await GetSubList(id);
    } else {
      whObj.value.height = "72vh";
      result = await GetRunListByBuild(params);
    }
    treeData.value = [];
    if (!result.Status) {
      console.error("获取设备数据失败");
      return;
    }
    // 解析返回的数据
    const data = JSON.parse(result.Result);
    if (data.length === 0) {
      console.log("该建筑下没有设备数据");
      return;
    }
    // 处理设备数据，添加id属性
    data.forEach(device => {
      device.id = device.DeviceId;
    });
    // 更新设备数据
    treeData.value = data;
    // 根据类型参数决定后续行为
    // type=1: 加载设备并触发选中事件（用于设备视图）
    // type=2: 只加载设备数据，不触发选中事件（用于建筑视图）
    if (type === 1 && data.length > 0) {
      // 确保设备数据包含设备类型信息
      const firstDevice = {
        ...data[0],
        DeviceTypeCode: data[0].DeviceTypeCode || selectedDeviceType.value
      };

      console.log("自动选择第一个设备:", firstDevice);

      // 使用setTimeout确保DOM更新完成后再触发事件
      setTimeout(() => {
        // 选中第一个设备，并标识为设备查询
        childClick(firstDevice, "device");
      }, 100);
    }
  } catch (error) {
    console.error("获取设备数据出错:", error);
  } finally {
    // 隐藏加载状态
    treeLoading.value = false;
  }
};

// 处理设备类型变更
const handleDeviceTypeChange = async () => {
  if (defaultTreeData.value.length > 0) {
    console.log("设备类型切换:", selectedDeviceType.value);
    console.log("当前设备类型选项:", deviceTypeOptions.value);
    // 重新加载设备数据，应用设备类型筛选
    await getDeviceByBuildId(defaultTreeData.value[0], 1);
  }
};

// 监听设备类型变化
watch(selectedDeviceType, (newType, oldType) => {
  console.log("设备类型变化:", { oldType, newType });
  if (newType !== oldType) {
    handleDeviceTypeChange();
  }
});

// 处理切换到设备查询模式
const handleDeviceTypeChange2 = () => {
  // 如果有选中的建筑，加载该建筑下的设备
  if (defaultTreeData.value.length > 0) {
    // 传入参数 1 表示加载设备后自动选择第一个设备
    getDeviceByBuildId(defaultTreeData.value[0], 1);
  } else if (latestTreeData.value && latestTreeData.value.length > 0) {
    // 如果没有选中的建筑，则选择第一个建筑
    const firstBuildId = latestTreeData.value[0].BuildId;
    defaultTreeData.value = [firstBuildId];
    input3.value = latestTreeData.value[0].BuildName;
    // 加载该建筑下的设备并自动选择第一个设备
    getDeviceByBuildId(firstBuildId, 1);
  }
};

const handleDisplayTypeChange = type => {
  // 记录之前的显示类型
  const prevType = displayType.value;
  // 更新显示类型
  displayType.value = type;

  // 根据不同的查询类型处理
  switch (type) {
    // 建筑查询模式
    case "building":
      handleBuildingTypeChange();
      break;
    // 设备查询模式
    case "device":
      handleDeviceTypeChange2();
      break;
    // 为未来可能的查询类型预留扩展点
    // case "department":
    //   handleDepartmentTypeChange();
    //   break;
    default:
      console.log("未知的查询类型:", type);
      break;
  }
};

// 处理切换到建筑查询模式
const handleBuildingTypeChange = () => {
  // 处理建筑树节点，确保每个节点都有id属性
  const processTreeNodes = nodes => {
    if (!nodes) return;
    nodes.forEach(node => {
      node.id = node.BuildId;
      if (node.children && node.children.length > 0) {
        processTreeNodes(node.children);
      }
    });
  };

  // 克隆建筑树数据并处理节点
  const buildingTree = cloneDeep(latestTreeData.value);
  processTreeNodes(buildingTree);

  // 更新树数据为建筑树
  treeData.value = buildingTree;

  // 默认选择第一个建筑节点（如果存在）
  if (buildingTree && buildingTree.length > 0) {
    const firstBuilding = buildingTree[0];
    selectedBuildingId.value = firstBuilding.BuildId;
    selectedBuildingName.value = firstBuilding.BuildName;

    // 通过 changeElec 方法传出去，并标识为建筑查询
    emit("changeElec", firstBuilding, { queryType: "building" });
  }
};

const emit = defineEmits(["changeElec"]);
function childClick(value, queryType = null) {
  // 使用传入的查询类型或当前显示类型
  const currentType = queryType || displayType.value;

  console.log("childClick 被调用:", {
    value,
    queryType,
    currentType,
    selectedDeviceType: selectedDeviceType.value
  });

  // 根据不同的查询类型处理节点点击事件
  switch (currentType) {
    // 设备查询模式
    case "device":
      // 确保设备数据包含设备类型信息
      const deviceData = {
        ...value,
        DeviceTypeCode: value.DeviceTypeCode || selectedDeviceType.value
      };

      // 返回设备数据，并标识为设备查询，同时传递设备类型信息
      emit("changeElec", deviceData, {
        queryType: "device",
        deviceType: selectedDeviceType.value
      });
      break;
    // 建筑查询模式
    case "building":
      if (value.BuildId) {
        // 使用单独的变量存储建筑查询模式下选中的建筑
        selectedBuildingId.value = value.BuildId;
        selectedBuildingName.value = value.BuildName;
        // 直接返回建筑数据，并标识为建筑查询
        emit("changeElec", value, { queryType: "building" });
      }
      break;
    // 为未来可能的查询类型预留扩展点
    // case "department":
    //   // 处理部门节点点击事件
    //   if (value.DepartmentId) {
    //     // 直接返回部门数据，并标识为部门查询
    //     emit("changeElec", value, { queryType: "department" });
    //   }
    //   break;
    default:
      console.log("未知的查询类型:", currentType);
      break;
  }
}
</script>

<template>
  <div class="leftBox">
    <div class="searchBox">
      <!-- 查询类型选择，根据 hideQueryType 属性控制显示或隐藏 -->
      <div v-if="!props.hideQueryType" class="query-type-container">
        <el-radio-group
          v-model="displayType"
          size="default"
          class="query-type-radio"
          @change="handleDisplayTypeChange"
        >
          <el-radio
            v-for="item in queryTypeOptions"
            :key="item.value"
            :label="item.value"
            class="custom-radio"
          >
            {{ item.label }}
          </el-radio>
        </el-radio-group>
      </div>

      <!-- 建筑选择框，只在设备视图显示 -->
      <div v-if="displayType === 'device'" class="select-container">
        <span class="label-text">建筑</span>
        <div class="input-container" @click="handleMenu()">
          <el-input
            v-model="input3"
            class="input-with-select"
            placeholder="请选择建筑"
            readonly
          >
            <template #append>
              <el-button :icon="useRenderIcon(Search)" />
            </template>
          </el-input>
        </div>
      </div>

      <!-- 设备类型筛选，只在设备查询模式下显示 -->
      <div
        v-if="displayType === 'device' && level != 5"
        class="select-container"
      >
        <span class="label-text">设备类型</span>
        <el-select
          v-model="selectedDeviceType"
          size="default"
          placeholder="请选择设备类型"
          class="device-type-select"
          :loading="deviceTypeLoading"
          @change="handleDeviceTypeChange"
        >
          <el-option
            v-for="item in deviceTypeOptions"
            :key="item.value"
            :label="`${item.label} (${item.value})`"
            :value="item.value"
          />
        </el-select>
      </div>
      <!-- 建筑查询模式下显示当前选中的建筑 -->
      <div
        v-if="displayType === 'building' && selectedBuildingName"
        class="selected-building-info"
      >
        <span class="label-text">当前选中：</span>
        <span class="building-name">{{ selectedBuildingName }}</span>
      </div>
    </div>
    <div class="treeBox">
      <tree
        ref="treeRef2"
        class="mr-2"
        :treeData="treeData"
        :whObj="whObj"
        :treeLoading="treeLoading"
        :autoSelect="false"
        @treeSelect="childClick"
      />
    </div>
  </div>
</template>

<style lang="scss" scoped>
:deep {
  .pl-8 {
    padding-left: 0px;
  }
}

.leftBox {
  height: calc(100vh - 98px);
  background-color: #fff;
  border: 1px solid #e2e2e2;
}

.searchBox {
  padding: 15px;
  width: 100% !important;
  background-color: #ffffff;
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.query-type-container {
  display: flex;
  justify-content: center;
  margin-bottom: 5px;
}

.query-type-radio {
  width: 100%;
  display: flex;
  justify-content: space-around;

  :deep(.el-radio) {
    margin-right: 0;
    height: 36px;

    .el-radio__label {
      font-size: 14px;
      font-weight: 500;
    }
  }
}

.custom-radio {
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  padding: 5px 15px;
  transition: all 0.3s;

  &:hover {
    border-color: var(--el-color-primary);
  }

  &:has(.el-radio__input.is-checked) {
    background-color: var(--el-color-primary-light-9);
    border-color: var(--el-color-primary);
  }
}

.device-type-select {
  width: 70%;
}

.select-container {
  display: flex;
  gap: 8px;
}

.selected-building-info {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.building-name {
  color: var(--el-color-primary);
  font-weight: 500;
  font-size: 14px;
}

.label-text {
  line-height: 30px;
  color: #000;
  font-size: 14px;
  font-weight: 500;
}

.input-container {
  width: 80%;
}

.view-tip {
  padding: 10px;
  background-color: #f5f7fa;
  border-radius: 4px;
  text-align: center;
  color: #606266;
  font-size: 14px;
}
</style>
