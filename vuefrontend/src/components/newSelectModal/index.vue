<script setup lang="ts">
import { useRenderIcon } from "../ReIcon/src/hooks";
import { ref, onMounted, h } from "vue";
import Search from "~icons/ep/search";
import { addDialog } from "../ReDialog/index";
import buildTree from "../buildTree/index.vue";
import { storage } from "@/utils/storage";
// import eventBus from "@/utils/eventBus";
import { ElMessage } from "element-plus";
import { debounce } from "@pureadmin/utils";

const treeRef = ref();
const defaultTreeData2 = ref([]);
const treeData2 = ref([]);
const input3 = ref("");
const latestTreeData = ref();
// onBeforeMount(() => {
//   eventBus.off("getNewBuild");
// });

const updateUI2: any = debounce(
  (BuildId, BuildName) => {
    updateUI(BuildId, BuildName);
  },
  1000,
  true
);

onMounted(async () => {
  defaultTreeData2.value = [];
  // eventBus.on("getNewBuild", data => {
  //   if (
  //     data &&
  //     typeof data.BuildId === "number" &&
  //     typeof data.BuildName === "string"
  //   ) {
  //     updateUI2(data.BuildId, data.BuildName);
  //   } else {
  //     console.error("Invalid data received:", data);
  //   }
  // });

  const curBuild = storage.getItem("curBuild");
  if (curBuild) {
    try {
      const obj = JSON.parse(curBuild);
      if (
        obj &&
        typeof obj.BuildId === "number" &&
        typeof obj.BuildName === "string"
      ) {
        updateUI2(obj.BuildId, obj.BuildName);
        return;
      } else {
        console.error("Invalid curBuild object:", obj);
      }
    } catch (error) {
      console.error("Error parsing curBuild:", error);
    }
  }
  return;
});

function updateUI(buildId, buildName) {
  defaultTreeData2.value = [buildId];
  input3.value = buildName;
  childClick(defaultTreeData2.value);
  childItemClick({ BuildId: buildId, BuildName: buildName });
}
function findNodeByCurBuildId(tree, targetCurBuildId) {
  console.log("🚀 ~ findNodeByCurBuildId ~ tree:", tree);
  // 基础案例：遍历每一个节点
  for (const node of tree) {
    if (node.BuildId == targetCurBuildId) {
      return node;
    }
    // 如果当前节点有 children，则递归查找
    if (node.children && node.children.length > 0) {
      const result = findNodeByCurBuildId(node.children, targetCurBuildId);
      if (result) {
        return result;
      }
    }
  }
  // 如果没有找到匹配的节点，返回 null
  return null;
}
const props = defineProps({
  level: {
    type: Number,
    default: 99
  },

  checkedFlag: {
    type: Boolean,
    default: false
  },
  isAll: {
    type: Boolean,
    default: false
  },
  isKaiBi: {
    type: Boolean,
    default: true
  },
  buildType: {
    type: String,
    default: "0,1"
  },
  clickNode: {
    type: Boolean,
    default: true
  },
  checkBuildLevel: {
    //支持选择的等级 1 默认全都可选 2 是2、3可选，依次类推
    type: Number,
    default: 1
  },
  curBuildId: {
    // 组件默认传进来的建筑id,用于编辑的时候，显示当前的建筑id
    type: String,
    default: ""
  },
  defaultTreeData: Array<any>
});

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
        defaultTreeData: defaultTreeData2.value,
        level: props.level,
        checkedFlag: props.checkedFlag,
        clickNode: props.clickNode
      }),
    beforeSure: done => {
      const tree = treeRef.value.getTreeRef();
      const tree2 = tree.getCheckedNodes();
      if (tree2[0].ParentId == 0) {
        ElMessage({
          message: "请选择子项",
          type: "warning",
          customClass: "z99999"
        });
        return;
      }
      defaultTreeData2.value = [];
      if (props.checkedFlag) {
        // const arr = [];
        tree2.forEach(v => {
          defaultTreeData2.value.push(v.BuildId);
          defaultTreeData2.value.push(
            ...(treeRef.value.getChildrenTreeByBuildId(v.BuildId)
              ? JSON.parse(
                  treeRef.value.getChildrenTreeByBuildId(v.BuildId)
                )?.children?.map(item => item.BuildId) || []
              : [])
          );
        });
        // latestTreeData.value?.children.forEach(v => {
        //   if (defaultTreeData2.value.includes(v.BuildId)) {
        //     if (v.children && v.children.length > 1) {
        //       v.children.forEach(e => {
        //         defaultTreeData2.value.push(e.BuildId);
        //       });
        //     }
        //   }
        // });
        input3.value = tree2.map(v => v.BuildName).join(",");
        childClick(defaultTreeData2.value, tree2);
        // childItemClick(defaultTreeData2.value, tree2);
      } else {
        if (tree2[0].BuildLevel >= props.checkBuildLevel) {
          defaultTreeData2.value = tree2.value;
          input3.value = tree2[0].BuildName;
          childClick([tree2[0].BuildId], tree2);
          childItemClick(tree2[0], tree2);
        } else {
          return;
        }
      }
      done();
    }
  });
}

const emit = defineEmits(["changeElec", "childItemClick"]);
function childClick(value, nodes?: any) {
  emit("changeElec", value, nodes);
}
function childItemClick(value, nodes?: any) {
  emit("childItemClick", value, nodes);
}
</script>

<template>
  <el-form-item
    :label="
      (buildType == '0,1' ? '变电所' : buildType == '0,2' ? '水泵房' : '建筑') +
      '名称'
    "
  >
    <div @click="handleMenu()">
      <el-input
        v-model="input3"
        class="input-with-select"
        :placeholder="
          '请选择' +
          (buildType == '0,1'
            ? '变电所'
            : buildType == '0,2'
              ? '水泵房'
              : '建筑')
        "
        readonly
        style="width: 255px"
      >
        <template #append>
          <el-button :icon="useRenderIcon(Search)" />
        </template>
      </el-input>
    </div>
  </el-form-item>
</template>
<style lang="scss" scoped></style>
