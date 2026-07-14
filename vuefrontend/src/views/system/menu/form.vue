<script setup lang="ts">
import { ref } from "vue";
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
    MenuIcon: "",
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
  MenuCode: [{ required: true, message: "菜单编码不能为空", trigger: "blur" }]
};

function getRef() {
  return ruleFormRef.value;
}

defineExpose({ getRef });
</script>

<template>
  <el-form ref="ruleFormRef" :model="formValue" :rules="rules" label-width="100px">
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
    <el-form-item label="菜单图标" prop="MenuIcon">
      <el-input
        v-model="formValue.MenuIcon"
        clearable
        placeholder="图标名，如 ep:menu"
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
  </el-form>
</template>
