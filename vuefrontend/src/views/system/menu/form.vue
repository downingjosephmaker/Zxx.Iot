<script setup lang="ts">
import { ref } from "vue";
import IconSelect from "@/components/IconSelect/index.vue";
import type { MenuFormProps } from "./utils/types";

defineOptions({
  name: "SysMenuForm"
});

const props = withDefaults(defineProps<MenuFormProps>(), {
  formInline: () => ({
    title: "",
    MenuId: "",
    MenuCode: "",
    MenuName: "",
    ParentId: "0",
    MenuUrl: "",
    Component: "",
    MenuIcon: "",
    MetaJson: "",
    IsShowLink: 1,
    SortBorder: "",
    TreeLevel: 1,
    FullName: "",
    FullCode: "",
    HasChild: false
  }),
  menuTree: () => []
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

// 上级菜单树：顶级 + 现有菜单，排除自身（避免挂到自己下形成环）
const parentTree = ref([
  { MenuId: "0", MenuName: "顶级菜单（根）", children: [] },
  ...pruneSelf(props.menuTree, props.formInline.MenuId)
]);

function pruneSelf(nodes: any[], selfId: string): any[] {
  if (!selfId) return nodes;
  return nodes
    .filter(n => n.MenuId !== selfId)
    .map(n => ({
      ...n,
      children: n.children ? pruneSelf(n.children, selfId) : undefined
    }));
}

const rules = {
  MenuName: [{ required: true, message: "菜单名称不能为空", trigger: "blur" }],
  MenuCode: [{ required: true, message: "菜单编码不能为空", trigger: "blur" }],
  MetaJson: [
    {
      // 格式写错会让这条菜单的自定义 meta 被后端静默忽略，在这里先挡住
      validator: (_rule: any, value: string, callback: any) => {
        if (!value) return callback();
        try {
          const parsed = JSON.parse(value);
          if (
            typeof parsed !== "object" ||
            parsed === null ||
            Array.isArray(parsed)
          ) {
            return callback(
              new Error('必须是 JSON 对象，如 {"projectKind":"scada"}')
            );
          }
          callback();
        } catch {
          callback(new Error('不是合法 JSON，如 {"projectKind":"scada"}'));
        }
      },
      trigger: "blur"
    }
  ]
};

function getRef() {
  return ruleFormRef.value;
}

defineExpose({ getRef });
</script>

<template>
  <el-form
    ref="ruleFormRef"
    :model="formValue"
    :rules="rules"
    label-width="100px"
  >
    <el-form-item label="菜单名称" prop="MenuName">
      <el-input
        v-model="formValue.MenuName"
        clearable
        placeholder="侧边栏显示名，如 设备管理"
      />
    </el-form-item>
    <el-form-item label="菜单编码" prop="MenuCode">
      <el-input
        v-model="formValue.MenuCode"
        clearable
        placeholder="路由 name，须与前端路由一致，如 IotDevice"
      />
    </el-form-item>
    <el-form-item label="上级菜单" prop="ParentId">
      <el-tree-select
        v-model="formValue.ParentId"
        :data="parentTree"
        :props="{ label: 'MenuName', children: 'children' }"
        node-key="MenuId"
        check-strictly
        :render-after-expand="false"
        placeholder="选择上级菜单"
        class="w-full"
      />
    </el-form-item>
    <el-form-item label="路由地址" prop="MenuUrl">
      <el-input
        v-model="formValue.MenuUrl"
        clearable
        placeholder="路由 path，如 /iot/device"
      />
    </el-form-item>
    <el-form-item label="组件路径" prop="Component">
      <el-input
        v-model="formValue.Component"
        clearable
        placeholder="相对 src/views，如 iot/device/index.vue；目录节点留空"
      />
    </el-form-item>
    <el-form-item label="菜单图标" prop="MenuIcon">
      <IconSelect
        v-model="formValue.MenuIcon"
        placeholder="点击选择图标（子菜单可留空）"
      />
    </el-form-item>
    <el-form-item label="排序" prop="SortBorder">
      <el-input v-model="formValue.SortBorder" placeholder="数字，越小越靠前" />
    </el-form-item>
    <el-form-item label="是否显示" prop="IsShowLink">
      <el-switch
        v-model="formValue.IsShowLink"
        :active-value="1"
        :inactive-value="0"
      />
    </el-form-item>
    <el-form-item label="附加 meta" prop="MetaJson">
      <el-input
        v-model="formValue.MetaJson"
        type="textarea"
        :rows="2"
        placeholder='选填。合并进路由 meta 的自定义字段，如 {"projectKind":"scada"}'
      />
    </el-form-item>
  </el-form>
</template>
