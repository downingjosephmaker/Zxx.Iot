<script setup lang="ts">
import { ref, onMounted } from "vue";
import { message } from "@/utils/message";
import {
  getButtonList,
  GetButtonListByMenuId,
  SaveMenuBtnBatch
} from "@/api/system";

const props = defineProps<{ menuId: string; menuName: string }>();

interface Btn {
  ButtonId: number;
  ButtonCode: string;
  ButtonName: string;
}

const loading = ref(true);
const allButtons = ref<Btn[]>([]);
const checked = ref<number[]>([]);

async function load() {
  loading.value = true;
  const [allRes, boundRes] = await Promise.all([
    getButtonList({ page: 1, pagesize: 999, sconlist: [] }),
    GetButtonListByMenuId(props.menuId)
  ]);
  if (allRes.Status) allButtons.value = JSON.parse(allRes.Result);
  if (boundRes.Status && boundRes.Result) {
    const bound: Btn[] = JSON.parse(boundRes.Result);
    checked.value = bound.map(b => b.ButtonId);
  }
  loading.value = false;
}

async function submit() {
  // SaveMenuBtnBatch 按 menuId 先删后插，故空勾选=清空绑定；须至少带一条含 menuId 的记录承载 menuId
  const list =
    checked.value.length > 0
      ? checked.value.map((bid, i) => ({
          MenuId: props.menuId,
          ButtonId: bid,
          MbSort: i
        }))
      : [{ MenuId: props.menuId, ButtonId: 0, MbSort: 0 }];
  const data = await SaveMenuBtnBatch(list);
  message(data.Message, { type: data.Status ? "success" : "error" });
  return data.Status;
}

defineExpose({ submit });
onMounted(load);
</script>

<template>
  <div v-loading="loading">
    <p class="tip">
      为菜单【{{ menuName }}】勾选可用按钮；这些按钮再经角色授权后，
      对应用户的 v-perms 才放行。
    </p>
    <el-checkbox-group v-model="checked" class="btn-grid">
      <el-checkbox
        v-for="b in allButtons"
        :key="b.ButtonId"
        :value="b.ButtonId"
        :label="b.ButtonId"
        border
      >
        {{ b.ButtonName }}
        <span class="code">{{ b.ButtonCode }}</span>
      </el-checkbox>
    </el-checkbox-group>
    <el-empty
      v-if="!loading && allButtons.length === 0"
      description="暂无按钮，请先到「按钮管理」新增"
    />
  </div>
</template>

<style scoped>
.tip {
  margin-bottom: 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
.btn-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.btn-grid :deep(.el-checkbox) {
  margin-right: 0;
}
.code {
  margin-left: 6px;
  font-size: 12px;
  color: var(--el-text-color-placeholder);
}
</style>
