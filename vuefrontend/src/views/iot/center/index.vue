<script setup lang="ts">
import { ref, computed, watch, onMounted, h } from "vue";
import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import Refresh from "~icons/ep/refresh";
import Plus from "~icons/ep/plus";
import {
  getListByPage as getTypeList,
  insert as insertType
} from "@/api/iot/devicetype";
import {
  getListByPage as getDeviceList,
  insert as insertDevice
} from "@/api/iot/device";
import productForm from "@/views/iot/product/form.vue";
import deviceForm from "@/views/iot/device/form.vue";
import commandSend from "@/views/iot/device/command-send.vue";
import ProductInfoTab from "./components/ProductInfoTab.vue";
import ProductPointsTab from "./components/ProductPointsTab.vue";
import ProductCommandsTab from "./components/ProductCommandsTab.vue";
import StrategyTab from "./components/StrategyTab.vue";
import AlarmTab from "./components/AlarmTab.vue";
import DeviceInfoTab from "./components/DeviceInfoTab.vue";
import RealtimeTab from "./components/RealtimeTab.vue";
import HistoryTab from "./components/HistoryTab.vue";

defineOptions({
  name: "IotCenter"
});

/** 树节点(产品/设备双态) */
interface CenterNode {
  key: string;
  label: string;
  kind: "product" | "device";
  raw: any;
  children?: CenterNode[];
}

interface TreeOption {
  value: string;
  label: string;
  disabled?: boolean;
  children?: TreeOption[];
}

const treeRef = ref();
const treeLoading = ref(false);
const filterText = ref("");
const treeData = ref<CenterNode[]>([]);
const typeList = ref<any[]>([]);
const deviceList = ref<any[]>([]);
const currentNode = ref<CenterNode | null>(null);
const productTab = ref("info");
const deviceTab = ref("info");
const dialogFormRef = ref();
const commandRef = ref();

const STATE_TEXT = { 2: "在线", 1: "掉电", 0: "离线" } as const;

const productScope = computed(() =>
  currentNode.value?.kind === "product"
    ? { kind: "product" as const, typecode: currentNode.value.raw.TypeCode }
    : null
);
const deviceScope = computed(() =>
  currentNode.value?.kind === "device"
    ? {
        kind: "device" as const,
        typecode: currentNode.value.raw.DeviceTypeCode ?? "",
        deviceId: currentNode.value.raw.DeviceId
      }
    : null
);
/** 选中设备/选中产品的类型编码(新增设备预填) */
const selectedTypeCode = computed(() =>
  currentNode.value?.kind === "product"
    ? currentNode.value.raw.TypeCode
    : (currentNode.value?.raw.DeviceTypeCode ?? "")
);

async function loadTree() {
  treeLoading.value = true;
  try {
    const [typeRes, devRes] = await Promise.all([
      getTypeList({ page: 1, pagesize: 10000, sconlist: [] }),
      getDeviceList({ page: 1, pagesize: 10000, sconlist: [] })
    ]);
    typeList.value = typeRes.Status ? JSON.parse(typeRes.Result) : [];
    deviceList.value = devRes.Status ? JSON.parse(devRes.Result) : [];
    treeData.value = buildCenterTree();
    refreshCurrentNode();
  } finally {
    treeLoading.value = false;
  }
}

/** 产品按ParentId建层级(孤儿提根),设备按DeviceTypeCode挂到产品节点下 */
function buildCenterTree(): CenterNode[] {
  const prodMap = new Map<string, CenterNode>();
  typeList.value.forEach(t =>
    prodMap.set(t.TypeCode, {
      key: `p:${t.TypeCode}`,
      label: `${t.TypeName}(${t.TypeCode})`,
      kind: "product",
      raw: t,
      children: []
    })
  );
  const roots: CenterNode[] = [];
  prodMap.forEach(node => {
    const parent = node.raw.ParentId ? prodMap.get(node.raw.ParentId) : null;
    if (parent) parent.children!.push(node);
    else roots.push(node);
  });
  deviceList.value.forEach(d => {
    const p = prodMap.get(d.DeviceTypeCode);
    if (!p) return;
    p.children!.push({
      key: `d:${d.DeviceId}`,
      label: d.DeviceName ?? `设备${d.DeviceId}`,
      kind: "device",
      raw: d
    });
  });
  const sortNodes = (nodes: CenterNode[]) => {
    nodes.sort((a, b) =>
      a.kind === b.kind
        ? String(a.raw.SortBorder ?? "").localeCompare(
            String(b.raw.SortBorder ?? "")
          )
        : a.kind === "product"
          ? -1
          : 1
    );
    nodes.forEach(n => {
      if (n.children?.length) sortNodes(n.children);
      else delete n.children;
    });
  };
  sortNodes(roots);
  return roots;
}

/** 刷新后按key重定位选中节点的最新raw(编辑保存后名称/状态即时跟上;节点被删则清选) */
function refreshCurrentNode() {
  const node = currentNode.value;
  if (!node) return;
  const raw =
    node.kind === "product"
      ? typeList.value.find(t => `p:${t.TypeCode}` === node.key)
      : deviceList.value.find(d => `d:${d.DeviceId}` === node.key);
  currentNode.value = raw ? { ...node, raw } : null;
}

function filterNode(value: string, data: CenterNode) {
  return !value || data.label.includes(value);
}

watch(filterText, val => treeRef.value?.filter(val));

function onNodeChange(data: CenterNode) {
  currentNode.value = data;
}

/** 产品类型树下拉选项(设备表单/产品表单共用;excludeFullCode=编辑防成环,新增不需要) */
function buildTypeOptions(): TreeOption[] {
  const map = new Map<string, TreeOption & { parent?: string }>();
  typeList.value.forEach(t =>
    map.set(t.TypeCode, {
      value: t.TypeCode,
      label: `${t.TypeName}(${t.TypeCode})`,
      parent: t.ParentId,
      children: []
    })
  );
  const roots: TreeOption[] = [];
  map.forEach(node => {
    if (node.parent && map.has(node.parent)) {
      map.get(node.parent)!.children!.push(node);
    } else {
      roots.push(node);
    }
  });
  const prune = (nodes: TreeOption[]) => {
    nodes.forEach(n => {
      delete (n as any).parent;
      if (n.children!.length) prune(n.children!);
      else delete n.children;
    });
  };
  prune(roots);
  return roots;
}

/** 新增产品(选中产品节点时预填为其子产品;组装与保存对齐product页hook,FullCode等由服务端重算) */
function openAddProduct() {
  const parentCode =
    currentNode.value?.kind === "product" ? currentNode.value.raw.TypeCode : "";
  const formData = {
    title: "新增",
    TypeCode: "",
    TypeName: "",
    ParentId: parentCode,
    SortBorder: "",
    IsEnable: true,
    HasChild: false,
    OfflineMinute: 0,
    SubChannels: 0,
    SbjgType: false,
    MqttKey: ""
  };
  const typeOptions = buildTypeOptions();
  addDialog({
    title: "新增产品类型",
    props: { formInline: formData },
    width: "600px",
    draggable: true,
    closeOnClickModal: false,
    contentRenderer: () =>
      h(productForm, { formInline: formData, typeOptions, ref: dialogFormRef }),
    beforeSure: (done, { options }) => {
      const FormRef = dialogFormRef.value.getRef();
      const curData = { ...options.props.formInline };
      FormRef.validate(async valid => {
        if (!valid) {
          message("表单验证失败，请检查输入", { type: "warning" });
          return;
        }
        const expandObject = {
          OfflineMinute: Number(curData.OfflineMinute) || 0,
          SubChannels: Number(curData.SubChannels) || 0,
          SbjgType: curData.SbjgType,
          MqttKey: curData.MqttKey ?? ""
        };
        const data = await insertType({
          TypeCode: curData.TypeCode,
          TypeName: curData.TypeName,
          ParentId: curData.ParentId ?? "",
          SortBorder: curData.SortBorder ?? "",
          HasChild: curData.HasChild ?? false,
          IsEnable: curData.IsEnable,
          ExpandObject: expandObject,
          ExpandJson: JSON.stringify(expandObject)
        });
        if (data.Status) {
          message("新增产品类型成功", { type: "success" });
          done();
          loadTree();
        } else {
          message(data.Message, { type: "error" });
        }
      });
    }
  });
}

/** 新增设备(产品类型预填为选中节点,免手输编码;组装与保存对齐device页hook) */
function openAddDevice() {
  const typecode = selectedTypeCode.value;
  const expandRest = {
    EnergyType: "其他",
    LineNum: "",
    DeviceIMEI: "",
    DeviceSim: "",
    CurrentTransformer: 1,
    VoltageTransformer: 1
  };
  const formData = {
    title: "新增",
    DeviceId: 0,
    DeviceName: "",
    DeviceTypeCode: typecode,
    DeviceGuid: "",
    DeviceGateway: "",
    ParentId: 0,
    SortBorder: "",
    DeviceIp: "",
    DevicePort: 0,
    DeviceCom: 0,
    DeviceAdr: 0,
    IsCollection: 1,
    IsVirtual: 0,
    EnergyType: "其他",
    LineNum: "",
    DeviceIMEI: "",
    DeviceSim: "",
    CurrentTransformer: 1,
    VoltageTransformer: 1,
    passthrough: {
      DeviceState: 0,
      DeviceAlarm: 0,
      DeviceSwitch: 0,
      LastOnlineTime: "",
      IconType: "",
      HasChild: false,
      IsVirtual: 0,
      TenantId: 0,
      CreateId: 0,
      CreateTime: "",
      CreateName: "",
      expandRest
    }
  };
  const typeOptions = buildTypeOptions();
  addDialog({
    title: "新增设备",
    props: { formInline: formData },
    width: "760px",
    draggable: true,
    closeOnClickModal: false,
    contentRenderer: () =>
      h(deviceForm, { formInline: formData, typeOptions, ref: dialogFormRef }),
    beforeSure: (done, { options }) => {
      const FormRef = dialogFormRef.value.getRef();
      const curData = { ...options.props.formInline };
      FormRef.validate(async valid => {
        if (!valid) {
          message("表单验证失败，请检查输入", { type: "warning" });
          return;
        }
        const pass = curData.passthrough ?? {};
        const expandObject = {
          ...(pass.expandRest ?? {}),
          EnergyType: curData.EnergyType,
          LineNum: curData.LineNum ?? "",
          DeviceIMEI: curData.DeviceIMEI ?? "",
          DeviceSim: curData.DeviceSim ?? "",
          CurrentTransformer: Number(curData.CurrentTransformer) || 1,
          VoltageTransformer: Number(curData.VoltageTransformer) || 1
        };
        const devtype = typeList.value.find(
          t => t.TypeCode === curData.DeviceTypeCode
        );
        const data = await insertDevice({
          DeviceId: curData.DeviceId,
          DeviceName: curData.DeviceName,
          DeviceTypeCode: curData.DeviceTypeCode,
          DeviceTypeFullCode: devtype?.FullCode ?? "",
          DeviceGuid: curData.DeviceGuid ?? "",
          DeviceGateway: curData.DeviceGateway ?? "",
          ParentId: Number(curData.ParentId) || 0,
          SortBorder: curData.SortBorder ?? "",
          DeviceIp: curData.DeviceIp ?? "",
          DevicePort: Number(curData.DevicePort) || 0,
          DeviceCom: Number(curData.DeviceCom) || 0,
          DeviceAdr: Number(curData.DeviceAdr) || 0,
          IsCollection: curData.IsCollection ?? 1,
          IsVirtual: curData.IsVirtual ?? 0,
          DeviceState: pass.DeviceState,
          DeviceAlarm: pass.DeviceAlarm,
          DeviceSwitch: pass.DeviceSwitch,
          LastOnlineTime: pass.LastOnlineTime,
          IconType: pass.IconType,
          HasChild: pass.HasChild,
          TenantId: pass.TenantId,
          CreateId: pass.CreateId,
          CreateTime: pass.CreateTime,
          CreateName: pass.CreateName,
          ExpandObject: expandObject,
          ExpandJson: JSON.stringify(expandObject)
        });
        if (data.Status) {
          message("新增设备成功", { type: "success" });
          done();
          loadTree();
        } else {
          message(data.Message, { type: "error" });
        }
      });
    }
  });
}

onMounted(() => {
  loadTree();
});
</script>

<template>
  <div class="main center-page">
    <div class="center-aside bg-bg_color">
      <div class="aside-toolbar">
        <el-input
          v-model="filterText"
          placeholder="搜索产品/设备"
          clearable
          size="small"
        />
        <el-button
          size="small"
          :icon="useRenderIcon(Refresh)"
          circle
          title="刷新"
          @click="loadTree"
        />
      </div>
      <div class="aside-actions">
        <el-button
          size="small"
          type="primary"
          plain
          :icon="useRenderIcon(Plus)"
          @click="openAddProduct"
        >
          产品
        </el-button>
        <el-button
          size="small"
          type="success"
          plain
          :icon="useRenderIcon(Plus)"
          :disabled="!selectedTypeCode"
          title="预填选中产品的类型编码"
          @click="openAddDevice"
        >
          设备
        </el-button>
      </div>
      <el-tree
        ref="treeRef"
        v-loading="treeLoading"
        class="center-tree"
        :data="treeData"
        node-key="key"
        highlight-current
        :expand-on-click-node="false"
        :filter-node-method="filterNode"
        @current-change="onNodeChange"
      >
        <template #default="{ data }">
          <span class="tree-node">
            <span
              v-if="data.kind === 'device'"
              class="state-dot"
              :class="`state-${data.raw.DeviceState ?? 0}`"
              :title="STATE_TEXT[data.raw.DeviceState] ?? '离线'"
            />
            <span class="node-label">{{ data.label }}</span>
            <el-tag
              v-if="data.kind === 'device' && data.raw.DeviceAlarm === 1"
              type="danger"
              size="small"
              effect="light"
            >
              告警
            </el-tag>
          </span>
        </template>
      </el-tree>
    </div>

    <div class="center-main bg-bg_color">
      <el-empty
        v-if="!currentNode"
        description="请选择左侧产品或设备节点"
        :image-size="120"
      />

      <template v-else-if="currentNode.kind === 'product'">
        <div class="main-header">
          <span class="header-title">
            {{ currentNode.raw.TypeName }}
            <el-tag size="small" effect="plain" class="ml-2">
              {{ currentNode.raw.TypeCode }}
            </el-tag>
          </span>
        </div>
        <el-tabs v-model="productTab">
          <el-tab-pane label="产品信息" name="info">
            <ProductInfoTab :product="currentNode.raw" @changed="loadTree" />
          </el-tab-pane>
          <el-tab-pane label="点表" name="points" lazy>
            <ProductPointsTab :typecode="currentNode.raw.TypeCode" />
          </el-tab-pane>
          <el-tab-pane label="产品命令" name="commands" lazy>
            <ProductCommandsTab :typecode="currentNode.raw.TypeCode" />
          </el-tab-pane>
          <el-tab-pane label="策略" name="strategy" lazy>
            <StrategyTab v-if="productScope" :scope="productScope" />
          </el-tab-pane>
          <el-tab-pane label="告警" name="alarm" lazy>
            <AlarmTab v-if="productScope" :scope="productScope" />
          </el-tab-pane>
        </el-tabs>
      </template>

      <template v-else>
        <div class="main-header">
          <span class="header-title">
            {{ currentNode.raw.DeviceName }}
            <el-tag
              size="small"
              :type="
                currentNode.raw.DeviceState === 2
                  ? 'success'
                  : currentNode.raw.DeviceState === 1
                    ? 'warning'
                    : 'info'
              "
              effect="light"
              class="ml-2"
            >
              {{ STATE_TEXT[currentNode.raw.DeviceState] ?? "离线" }}
            </el-tag>
          </span>
        </div>
        <el-tabs v-model="deviceTab">
          <el-tab-pane label="设备信息" name="info">
            <DeviceInfoTab :device="currentNode.raw" @changed="loadTree" />
          </el-tab-pane>
          <el-tab-pane label="实时数据" name="realtime" lazy>
            <RealtimeTab :device="currentNode.raw" />
          </el-tab-pane>
          <el-tab-pane label="历史曲线" name="history" lazy>
            <HistoryTab :device="currentNode.raw" />
          </el-tab-pane>
          <el-tab-pane label="策略" name="strategy" lazy>
            <StrategyTab v-if="deviceScope" :scope="deviceScope" />
          </el-tab-pane>
          <el-tab-pane label="告警" name="alarm" lazy>
            <AlarmTab v-if="deviceScope" :scope="deviceScope" />
          </el-tab-pane>
          <el-tab-pane label="指令下发" name="command" lazy>
            <div class="command-pane">
              <commandSend
                :key="currentNode.raw.DeviceId"
                ref="commandRef"
                :device-id="currentNode.raw.DeviceId"
                :device-name="currentNode.raw.DeviceName"
                :device-type-code="currentNode.raw.DeviceTypeCode ?? ''"
              />
              <div class="command-footer">
                <el-button
                  type="primary"
                  :loading="commandRef?.sending"
                  @click="commandRef?.onSend()"
                >
                  下发
                </el-button>
              </div>
            </div>
          </el-tab-pane>
        </el-tabs>
      </template>
    </div>
  </div>
</template>

<style scoped lang="scss">
.center-page {
  display: flex;
  gap: 12px;
  align-items: stretch;
  height: calc(100vh - 130px);
}

.center-aside {
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  width: 300px;
  padding: 12px;
  overflow: hidden;
  border-radius: 6px;
}

.aside-toolbar {
  display: flex;
  gap: 8px;
  margin-bottom: 8px;
}

.aside-actions {
  display: flex;
  gap: 8px;
  margin-bottom: 8px;

  :deep(.el-button + .el-button) {
    margin-left: 0;
  }
}

.center-tree {
  flex: 1;
  overflow: auto;
}

.tree-node {
  display: inline-flex;
  gap: 6px;
  align-items: center;
  min-width: 0;

  .node-label {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}

.state-dot {
  flex-shrink: 0;
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &.state-2 {
    background: var(--el-color-success);
  }

  &.state-1 {
    background: var(--el-color-warning);
  }

  &.state-0 {
    background: var(--el-color-info-light-5);
  }
}

.center-main {
  flex: 1;
  min-width: 0;
  padding: 12px 16px;
  overflow: auto;
  border-radius: 6px;
}

.main-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 4px;

  .header-title {
    font-size: 16px;
    font-weight: 600;
  }
}

.command-pane {
  max-width: 640px;
}

.command-footer {
  margin-top: 12px;
  text-align: right;
}
</style>
