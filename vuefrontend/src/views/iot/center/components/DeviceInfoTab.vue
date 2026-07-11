<script setup lang="ts">
import { h, ref, computed } from "vue";
import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import editForm from "../../device/form.vue";
import type {
  DeviceFormItemProps,
  TreeSelectOption
} from "../../device/utils/types";
import {
  update,
  type DeviceInfoItem,
  type ExpandDeviceInfo
} from "@/api/iot/device";
import {
  getListByPage as getTypeList,
  type DeviceTypeItem
} from "@/api/iot/devicetype";
import type { QueryTableParams } from "@/api/type";

defineOptions({
  name: "DeviceInfoTab"
});

const props = defineProps<{ device: DeviceInfoItem | null }>();
const emit = defineEmits<{ (e: "changed"): void }>();

const formRef = ref();
/** 产品类型平铺表，供表单树下拉与FullCode回填 */
const typeList = ref<DeviceTypeItem[]>([]);

/** 设备状态显示映射(2在线/1掉电/0离线) */
const STATE_TAGS: Record<
  number,
  { type: "success" | "warning" | "info"; label: string }
> = {
  2: { type: "success", label: "在线" },
  1: { type: "warning", label: "掉电" },
  0: { type: "info", label: "离线" }
};

const stateTag = computed(
  () => STATE_TAGS[props.device?.DeviceState ?? 0] ?? STATE_TAGS[0]
);

const DEFAULT_EXPAND: ExpandDeviceInfo = {
  DeviceType: 0,
  EnergyType: "其他",
  LineNum: "",
  DeviceIMEI: "",
  DeviceSim: "",
  VideoIds: "",
  StrategySendStatus: "未下发",
  StrategySendTime: "",
  CurrentTransformer: 1,
  VoltageTransformer: 1
};

function parseExpand(row?: DeviceInfoItem): ExpandDeviceInfo {
  if (row?.ExpandObject) return { ...DEFAULT_EXPAND, ...row.ExpandObject };
  try {
    return row?.ExpandJson
      ? { ...DEFAULT_EXPAND, ...JSON.parse(row.ExpandJson) }
      : { ...DEFAULT_EXPAND };
  } catch {
    return { ...DEFAULT_EXPAND };
  }
}

async function loadTypeList() {
  const params: QueryTableParams = { page: 1, pagesize: 10000, sconlist: [] };
  const data = await getTypeList(params);
  if (data.Status) typeList.value = JSON.parse(data.Result);
}

/** 产品类型树下拉选项 */
function buildTypeOptions(): TreeSelectOption[] {
  const map = new Map<string, TreeSelectOption & { parent?: string }>();
  typeList.value.forEach(t =>
    map.set(t.TypeCode, {
      value: t.TypeCode,
      label: `${t.TypeName}(${t.TypeCode})`,
      parent: t.ParentId,
      children: []
    })
  );
  const roots: TreeSelectOption[] = [];
  map.forEach(node => {
    if (node.parent && map.has(node.parent)) {
      map.get(node.parent)!.children!.push(node);
    } else {
      roots.push(node);
    }
  });
  const prune = (nodes: TreeSelectOption[]) => {
    nodes.forEach(n => {
      delete (n as any).parent;
      if (n.children!.length) prune(n.children!);
      else delete n.children;
    });
  };
  prune(roots);
  return roots;
}

async function openEditDialog() {
  const row = props.device;
  if (!row) return;
  if (!typeList.value.length) await loadTypeList();
  const expand = parseExpand(row);
  const formData: DeviceFormItemProps = {
    title: "修改",
    DeviceId: row.DeviceId,
    DeviceName: row.DeviceName ?? "",
    DeviceTypeCode: row.DeviceTypeCode ?? "",
    DeviceGuid: row.DeviceGuid ?? "",
    DeviceGateway: row.DeviceGateway ?? "",
    ParentId: row.ParentId ?? 0,
    SortBorder: row.SortBorder ?? "",
    DeviceIp: row.DeviceIp ?? "",
    DevicePort: row.DevicePort ?? 0,
    DeviceCom: row.DeviceCom ?? 0,
    DeviceAdr: row.DeviceAdr ?? 0,
    IsCollection: row.IsCollection ?? 1,
    IsVirtual: row.IsVirtual ?? 0,
    EnergyType: expand.EnergyType ?? "其他",
    LineNum: expand.LineNum ?? "",
    DeviceIMEI: expand.DeviceIMEI ?? "",
    DeviceSim: expand.DeviceSim ?? "",
    CurrentTransformer: expand.CurrentTransformer ?? 1,
    VoltageTransformer: expand.VoltageTransformer ?? 1,
    // Update整行写回，运行时字段与未编辑字段原样透传防清零
    passthrough: {
      DeviceState: row.DeviceState ?? 0,
      DeviceAlarm: row.DeviceAlarm ?? 0,
      DeviceSwitch: row.DeviceSwitch ?? 0,
      LastOnlineTime: row.LastOnlineTime ?? "",
      IconType: row.IconType ?? "",
      HasChild: row.HasChild ?? false,
      IsVirtual: row.IsVirtual ?? 0,
      TenantId: row.TenantId ?? 0,
      CreateId: row.CreateId ?? 0,
      CreateTime: row.CreateTime ?? "",
      CreateName: row.CreateName ?? "",
      expandRest: expand
    }
  };
  const typeOptions = buildTypeOptions();

  addDialog({
    title: "修改设备",
    props: {
      formInline: formData
    },
    width: "760px",
    draggable: true,
    fullscreenIcon: true,
    closeOnClickModal: false,
    contentRenderer: () =>
      h(editForm, {
        formInline: formData,
        typeOptions,
        ref: formRef
      }),
    beforeSure: (done, { options }) => {
      const FormRef = formRef.value.getRef();
      const curData = { ...options.props.formInline } as DeviceFormItemProps;
      FormRef.validate(async valid => {
        if (valid) {
          const pass = curData.passthrough ?? {};
          const expandObject: ExpandDeviceInfo = {
            ...(pass.expandRest as ExpandDeviceInfo),
            EnergyType: curData.EnergyType,
            LineNum: curData.LineNum ?? "",
            DeviceIMEI: curData.DeviceIMEI ?? "",
            DeviceSim: curData.DeviceSim ?? "",
            CurrentTransformer: Number(curData.CurrentTransformer) || 1,
            VoltageTransformer: Number(curData.VoltageTransformer) || 1
          };
          // 所选产品类型的FullCode回填DeviceTypeFullCode
          const devtype = typeList.value.find(
            t => t.TypeCode === curData.DeviceTypeCode
          );
          const payload = {
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
          };
          const data = await update(payload);
          if (data.Status) {
            message("修改设备成功", { type: "success" });
            done();
            emit("changed");
          } else {
            message(data.Message, { type: "error" });
          }
        } else {
          message("表单验证失败，请检查输入", { type: "warning" });
        }
      });
    }
  });
}
</script>

<template>
  <div v-if="device" class="device-info-tab">
    <div class="tab-header">
      <el-button type="primary" @click="openEditDialog">编辑设备</el-button>
    </div>
    <el-descriptions :column="2" border>
      <el-descriptions-item label="设备名称">
        {{ device.DeviceName || "-" }}
      </el-descriptions-item>
      <el-descriptions-item label="设备ID">
        {{ device.DeviceId }}
      </el-descriptions-item>
      <el-descriptions-item label="设备编号">
        {{ device.DeviceGuid || "-" }}
      </el-descriptions-item>
      <el-descriptions-item label="产品类型">
        {{ device.DeviceTypeName || device.DeviceTypeCode || "-" }}
      </el-descriptions-item>
      <el-descriptions-item label="设备IP">
        {{ device.DeviceIp || "-" }}
      </el-descriptions-item>
      <el-descriptions-item label="端口号">
        {{ device.DevicePort || "-" }}
      </el-descriptions-item>
      <el-descriptions-item label="串口通道号">
        {{ device.DeviceCom ?? "-" }}
      </el-descriptions-item>
      <el-descriptions-item label="协议地址">
        {{ device.DeviceAdr ?? "-" }}
      </el-descriptions-item>
      <el-descriptions-item label="状态">
        <el-tag :type="stateTag.type" effect="light">
          {{ stateTag.label }}
        </el-tag>
      </el-descriptions-item>
      <el-descriptions-item label="告警">
        <el-tag
          :type="device.DeviceAlarm === 1 ? 'danger' : 'success'"
          effect="light"
        >
          {{ device.DeviceAlarm === 1 ? "告警" : "正常" }}
        </el-tag>
      </el-descriptions-item>
      <el-descriptions-item label="最后在线">
        {{ device.LastOnlineTime || "-" }}
      </el-descriptions-item>
      <el-descriptions-item label="是否采集">
        <el-tag
          :type="device.IsCollection === 1 ? 'success' : 'info'"
          effect="light"
        >
          {{ device.IsCollection === 1 ? "采集" : "停采" }}
        </el-tag>
      </el-descriptions-item>
      <el-descriptions-item label="虚拟设备">
        {{ device.IsVirtual === 1 ? "是" : "否" }}
      </el-descriptions-item>
      <el-descriptions-item label="上级设备ID">
        {{ device.ParentId ?? 0 }}
      </el-descriptions-item>
    </el-descriptions>
  </div>
  <el-empty v-else description="请选择设备节点" />
</template>

<style scoped>
.tab-header {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 12px;
}
</style>
