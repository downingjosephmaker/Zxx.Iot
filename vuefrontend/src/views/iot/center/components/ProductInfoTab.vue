<script setup lang="ts">
import { h, ref, computed } from "vue";
import { message } from "@/utils/message";
import { addDialog } from "@/components/ReDialog";
import type { QueryTableParams } from "@/api/type";
import {
  getListByPage,
  update,
  type DeviceTypeItem,
  type ExpandDeviceType
} from "@/api/iot/devicetype";
import type {
  DeviceTypeFormItemProps,
  TreeSelectOption
} from "@/views/iot/product/utils/types";
import editForm from "@/views/iot/product/form.vue";

defineOptions({
  name: "ProductInfoTab"
});

const props = defineProps<{
  product: DeviceTypeItem | null;
}>();

const emit = defineEmits<{
  (e: "changed"): void;
}>();

const formRef = ref();

const DEFAULT_EXPAND: ExpandDeviceType = {
  OfflineMinute: 0,
  SubChannels: 0,
  SbjgType: false,
  MqttKey: ""
};

/** 读取拓展属性，兼容服务端返回ExpandObject或仅ExpandJson两种形态 */
function parseExpand(row: DeviceTypeItem): ExpandDeviceType {
  if (row.ExpandObject) return { ...DEFAULT_EXPAND, ...row.ExpandObject };
  try {
    return row.ExpandJson
      ? { ...DEFAULT_EXPAND, ...JSON.parse(row.ExpandJson) }
      : { ...DEFAULT_EXPAND };
  } catch {
    return { ...DEFAULT_EXPAND };
  }
}

const expand = computed<ExpandDeviceType>(() =>
  props.product ? parseExpand(props.product) : { ...DEFAULT_EXPAND }
);

/** 平铺列表按ParentId构造树，孤儿节点(父级缺失)提升为根 */
function buildTree(list: DeviceTypeItem[]): DeviceTypeItem[] {
  const map = new Map<string, DeviceTypeItem>();
  list.forEach(t => map.set(t.TypeCode, { ...t, children: [] }));
  const roots: DeviceTypeItem[] = [];
  map.forEach(node => {
    if (node.ParentId && map.has(node.ParentId)) {
      map.get(node.ParentId)!.children!.push(node);
    } else {
      roots.push(node);
    }
  });
  const sortTree = (nodes: DeviceTypeItem[]) => {
    nodes.sort((a, b) => (a.SortBorder || "").localeCompare(b.SortBorder || ""));
    nodes.forEach(n => {
      if (n.children!.length) sortTree(n.children!);
      else delete n.children;
    });
  };
  sortTree(roots);
  return roots;
}

/** 上级类型下拉树；按FullCode前缀禁选自身及其子孙，防止成环 */
function buildSelectTree(
  list: DeviceTypeItem[],
  excludeFullCode?: string
): TreeSelectOption[] {
  const toOption = (node: DeviceTypeItem): TreeSelectOption => ({
    value: node.TypeCode,
    label: `${node.TypeName}(${node.TypeCode})`,
    disabled: excludeFullCode
      ? (node.FullCode ?? "").startsWith(excludeFullCode)
      : false,
    children: node.children?.map(toOption)
  });
  return buildTree(list).map(toOption);
}

/** 编辑产品：复用产品类型管理的表单弹窗，保存走 DeviceType/Update */
async function openEditDialog() {
  const row = props.product;
  if (!row) return;
  const title = "修改";

  // 弹窗前拉全量类型构建上级下拉树(树页面数据不在本组件内)
  const params: QueryTableParams = {
    page: 1,
    pagesize: 10000,
    sconlist: []
  };
  const listData = await getListByPage(params);
  if (!listData.Status) {
    message(listData.Message, { type: "error" });
    return;
  }
  const allList: DeviceTypeItem[] = JSON.parse(listData.Result);
  const typeOptions = buildSelectTree(allList, row.FullCode);

  const curExpand = parseExpand(row);
  const formData: DeviceTypeFormItemProps = {
    title,
    TypeCode: row.TypeCode,
    TypeName: row.TypeName ?? "",
    ParentId: row.ParentId ?? "",
    SortBorder: row.SortBorder ?? "",
    IsEnable: row.IsEnable ?? true,
    HasChild: row.HasChild ?? false,
    OfflineMinute: curExpand.OfflineMinute,
    SubChannels: curExpand.SubChannels,
    SbjgType: curExpand.SbjgType,
    MqttKey: curExpand.MqttKey
  };

  addDialog({
    title: `${title}产品`,
    props: {
      formInline: formData
    },
    width: "600px",
    draggable: true,
    fullscreenIcon: true,
    closeOnClickModal: false,
    contentRenderer: () =>
      h(editForm, { formInline: formData, typeOptions, ref: formRef }),
    beforeSure: (done, { options }) => {
      const FormRef = formRef.value.getRef();
      const curData = { ...options.props.formInline };
      FormRef.validate(async valid => {
        if (valid) {
          const expandObject: ExpandDeviceType = {
            OfflineMinute: Number(curData.OfflineMinute) || 0,
            SubChannels: Number(curData.SubChannels) || 0,
            SbjgType: curData.SbjgType,
            MqttKey: curData.MqttKey ?? ""
          };
          // FullCode/FullName/TreeLevel由服务端DAO重算，无需上送
          const payload = {
            TypeCode: curData.TypeCode,
            TypeName: curData.TypeName,
            ParentId: curData.ParentId ?? "",
            SortBorder: curData.SortBorder ?? "",
            HasChild: curData.HasChild ?? false,
            IsEnable: curData.IsEnable,
            ExpandObject: expandObject,
            ExpandJson: JSON.stringify(expandObject)
          };
          const data = await update(payload);
          if (data.Status) {
            message(`${title}产品成功`, { type: "success" });
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
  <div v-if="product">
    <div class="mb-3 flex justify-end">
      <el-button type="primary" @click="openEditDialog">编辑产品</el-button>
    </div>
    <el-descriptions :column="2" border>
      <el-descriptions-item label="类型名称">
        {{ product.TypeName || "-" }}
      </el-descriptions-item>
      <el-descriptions-item label="类型编码">
        {{ product.TypeCode }}
      </el-descriptions-item>
      <el-descriptions-item label="上级类型">
        {{ product.ParentId || "顶级" }}
      </el-descriptions-item>
      <el-descriptions-item label="排序号">
        {{ product.SortBorder || "-" }}
      </el-descriptions-item>
      <el-descriptions-item label="是否启用">
        <el-tag :type="product.IsEnable ? 'success' : 'info'" effect="light">
          {{ product.IsEnable ? "启用" : "停用" }}
        </el-tag>
      </el-descriptions-item>
      <el-descriptions-item label="完整编码">
        {{ product.FullCode || "-" }}
      </el-descriptions-item>
      <el-descriptions-item label="是否采集">
        <el-tag :type="expand.SbjgType ? 'success' : 'info'" effect="light">
          {{ expand.SbjgType ? "采集" : "不采集" }}
        </el-tag>
      </el-descriptions-item>
      <el-descriptions-item label="离线判断间隔">
        {{ expand.OfflineMinute > 0 ? `${expand.OfflineMinute}分钟` : "不判断" }}
      </el-descriptions-item>
      <el-descriptions-item label="支路数量">
        {{ expand.SubChannels ?? 0 }}
      </el-descriptions-item>
      <el-descriptions-item label="Mqtt通讯Key">
        {{ expand.MqttKey || "-" }}
      </el-descriptions-item>
    </el-descriptions>
  </div>
  <el-empty v-else description="请选择产品节点" />
</template>
