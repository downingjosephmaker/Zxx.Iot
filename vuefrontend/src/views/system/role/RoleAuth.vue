<script setup lang="ts">
import { ref, onMounted } from "vue";
import { message } from "@/utils/message";
import { getMenuTree, getRoleMenuBtns, saveRoleMenuBtns } from "@/api/system";

const props = defineProps<{ roleId: number; roleName: string }>();

interface TreeNode {
  key: string;
  label: string;
  type: "menu" | "btn";
  children?: TreeNode[];
}

const loading = ref(true);
const treeRef = ref();
const treeData = ref<TreeNode[]>([]);
const defaultChecked = ref<string[]>([]);
const treeProps = { label: "label", children: "children" };

// menuId -> parentMenuId，保存时向上补齐祖先菜单可见性（否则子菜单在 GetMenuTree 递归里显示不出）
const parentMap = new Map<string, string>();
const validKeys = new Set<string>();

function transform(nodes: any[], parentId: string): TreeNode[] {
  return nodes.map(n => {
    parentMap.set(n.menuid, parentId);
    validKeys.add(n.menuid);
    const btnChildren: TreeNode[] = (n.meta?.btns ?? []).map((b: any) => {
      const key = `btn::${n.menuid}::${b.ButtonId}`;
      validKeys.add(key);
      return { key, label: `按钮·${b.ButtonName}`, type: "btn" as const };
    });
    const childMenus = n.children ? transform(n.children, n.menuid) : [];
    const node: TreeNode = {
      key: n.menuid,
      label: n.meta?.title || n.name,
      type: "menu",
      children: [...btnChildren, ...childMenus]
    };
    if (node.children!.length === 0) delete node.children;
    return node;
  });
}

async function load() {
  loading.value = true;
  const [treeRes, grantRes] = await Promise.all([
    getMenuTree(2),
    getRoleMenuBtns(String(props.roleId))
  ]);
  if (treeRes.Status && treeRes.Result) {
    treeData.value = transform(JSON.parse(treeRes.Result), "0");
  }
  if (grantRes.Status && grantRes.Result) {
    const grants: { MenuId: string; ButtonId: number }[] = JSON.parse(
      grantRes.Result
    );
    const keys: string[] = [];
    grants.forEach(g => {
      const k =
        g.ButtonId > 0 ? `btn::${g.MenuId}::${g.ButtonId}` : g.MenuId;
      // 仅回显菜单节点与按钮叶子；ButtonId==0 的菜单可见性行还原为菜单节点勾选
      if (validKeys.has(k)) keys.push(k);
    });
    defaultChecked.value = keys;
  }
  loading.value = false;
}

async function submit() {
  const checked: string[] = treeRef.value.getCheckedKeys() as string[];
  const menuGrants = new Set<string>();
  const rows: { RoleId: number; MenuId: string; ButtonId: number }[] = [];
  checked.forEach(k => {
    if (k.startsWith("btn::")) {
      const [, menuId, btnId] = k.split("::");
      rows.push({ RoleId: props.roleId, MenuId: menuId, ButtonId: Number(btnId) });
      menuGrants.add(menuId);
    } else {
      menuGrants.add(k);
    }
  });
  // 补齐祖先菜单可见性
  [...menuGrants].forEach(mid => {
    let p = parentMap.get(mid);
    while (p && p !== "0") {
      menuGrants.add(p);
      p = parentMap.get(p);
    }
  });
  menuGrants.forEach(mid =>
    rows.push({ RoleId: props.roleId, MenuId: mid, ButtonId: 0 })
  );
  // 去重(MenuId+ButtonId)
  const seen = new Set<string>();
  const uniq = rows.filter(r => {
    const key = r.MenuId + "_" + r.ButtonId;
    return seen.has(key) ? false : (seen.add(key), true);
  });
  if (uniq.length === 0) {
    message("请至少勾选一个菜单再保存", { type: "warning" });
    return false;
  }
  const data = await saveRoleMenuBtns(uniq);
  message(data.Message, { type: data.Status ? "success" : "error" });
  return data.Status;
}

defineExpose({ submit });
onMounted(load);
</script>

<template>
  <div v-loading="loading">
    <p class="tip">
      为角色【{{ roleName }}】勾选可见菜单与可用按钮（独立勾选）。
      保存后，该角色下用户登录即按此过滤左侧菜单、v-perms 放行按钮。
    </p>
    <el-tree
      ref="treeRef"
      :data="treeData"
      :props="treeProps"
      node-key="key"
      show-checkbox
      check-strictly
      default-expand-all
      :default-checked-keys="defaultChecked"
      class="auth-tree"
    >
      <template #default="{ data }">
        <span :class="data.type === 'btn' ? 'btn-node' : 'menu-node'">
          {{ data.label }}
        </span>
      </template>
    </el-tree>
    <el-empty
      v-if="!loading && treeData.length === 0"
      description="暂无菜单，请先到「菜单管理」建立菜单目录"
    />
  </div>
</template>

<style scoped>
.tip {
  margin-bottom: 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
.auth-tree {
  max-height: 52vh;
  overflow: auto;
}
.btn-node {
  color: var(--el-color-primary);
  font-size: 13px;
}
.menu-node {
  font-weight: 500;
}
</style>
