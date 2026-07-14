<script setup lang="ts">
/**
 * 图标选择器 — 从全量 Remix Icon（ri:*，3100+）里可视化挑选。
 *
 * 图标集由 public/icons/ri.json 同源加载并 addCollection 注册（见 riCollection.ts），
 * 因此这里选中的任何图标在菜单/侧边栏都能离线渲染，不会走 CDN 漏图。
 *
 * 性能：3100+ 图标不一次性渲染，按 PAGE_SIZE 增量铺，滚动到底继续加载。
 */
import { ref, computed, watch, nextTick } from "vue";
import { ElDialog, ElInput, ElButton, ElEmpty, ElSkeleton } from "element-plus";
import { IconifyIconOffline } from "@/components/ReIcon";
import { ensureRiCollection } from "@/components/ReIcon/src/riCollection";

defineOptions({ name: "IconSelect" });

const props = defineProps<{
  /** 图标名，形如 ri:home-line；空串表示未设置 */
  modelValue?: string;
  placeholder?: string;
}>();
const emit = defineEmits<{ "update:modelValue": [val: string] }>();

const PAGE_SIZE = 120;

const visible = ref(false);
const loading = ref(false);
const keyword = ref("");
const allIcons = ref<string[]>([]);
const limit = ref(PAGE_SIZE);

const filtered = computed(() => {
  const kw = keyword.value.trim().toLowerCase();
  if (!kw) return allIcons.value;
  return allIcons.value.filter(n => n.includes(kw));
});
const shown = computed(() => filtered.value.slice(0, limit.value));

async function open() {
  visible.value = true;
  if (allIcons.value.length === 0) {
    loading.value = true;
    try {
      allIcons.value = await ensureRiCollection();
    } finally {
      loading.value = false;
    }
  }
  await nextTick();
}

function pick(name: string) {
  emit("update:modelValue", name);
  visible.value = false;
}

function clear() {
  emit("update:modelValue", "");
}

/** 滚动到底部继续铺图标 */
function onScroll(e: Event) {
  const el = e.target as HTMLElement;
  if (el.scrollTop + el.clientHeight >= el.scrollHeight - 40) {
    if (limit.value < filtered.value.length) limit.value += PAGE_SIZE;
  }
}

// 搜索变化时回到第一屏，避免残留上次的加载量
watch(keyword, () => (limit.value = PAGE_SIZE));
</script>

<template>
  <div class="icon-select">
    <div class="icon-trigger" @click="open">
      <IconifyIconOffline
        v-if="props.modelValue"
        :icon="props.modelValue"
        class="icon-preview"
      />
      <span v-else class="icon-preview icon-preview-empty">—</span>
      <span class="icon-name" :class="{ placeholder: !props.modelValue }">
        {{ props.modelValue || props.placeholder || "点击选择图标" }}
      </span>
      <ElButton
        v-if="props.modelValue"
        text
        size="small"
        class="icon-clear"
        @click.stop="clear"
      >
        清空
      </ElButton>
    </div>

    <ElDialog
      v-model="visible"
      title="选择图标"
      width="720px"
      :close-on-click-modal="true"
      append-to-body
    >
      <ElInput
        v-model="keyword"
        placeholder="搜索图标名，如 user / setting / video"
        clearable
        class="icon-search"
      />
      <div class="icon-count">
        共 {{ filtered.length }} 个图标{{
          shown.length < filtered.length
            ? `，已显示 ${shown.length} 个（滚动加载更多）`
            : ""
        }}
      </div>

      <ElSkeleton v-if="loading" :rows="6" animated class="icon-loading" />
      <ElEmpty v-else-if="filtered.length === 0" description="没有匹配的图标" />
      <div v-else class="icon-grid" @scroll="onScroll">
        <button
          v-for="name in shown"
          :key="name"
          type="button"
          class="icon-cell"
          :class="{ active: name === props.modelValue }"
          :title="name"
          @click="pick(name)"
        >
          <IconifyIconOffline :icon="name" class="cell-icon" />
          <span class="cell-name">{{ name.replace("ri:", "") }}</span>
        </button>
      </div>
    </ElDialog>
  </div>
</template>

<style scoped>
.icon-trigger {
  display: flex;
  gap: 10px;
  align-items: center;
  width: 100%;
  min-height: 32px;
  padding: 4px 10px;
  cursor: pointer;
  background: var(--el-fill-color-blank);
  border: 1px solid var(--el-border-color);
  border-radius: 4px;
  transition: border-color 0.2s;
}

.icon-trigger:hover {
  border-color: var(--el-color-primary);
}

.icon-preview {
  flex-shrink: 0;
  font-size: 18px;
  color: var(--el-text-color-primary);
}

.icon-preview-empty {
  color: var(--el-text-color-placeholder);
}

.icon-name {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  font-size: 13px;
  color: var(--el-text-color-primary);
  white-space: nowrap;
}

.icon-name.placeholder {
  color: var(--el-text-color-placeholder);
}

.icon-clear {
  flex-shrink: 0;
}

.icon-search {
  margin-bottom: 8px;
}

.icon-count {
  margin-bottom: 8px;
  font-size: 12px;
  color: var(--el-text-color-placeholder);
}

.icon-loading {
  padding: 8px;
}

.icon-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(96px, 1fr));
  gap: 8px;
  max-height: 420px;
  padding: 4px;
  overflow-y: auto;
}

.icon-cell {
  display: flex;
  flex-direction: column;
  gap: 6px;
  align-items: center;
  justify-content: center;
  min-height: 72px;
  padding: 10px 4px;
  cursor: pointer;
  background: var(--el-fill-color-blank);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  transition: all 0.15s;
}

.icon-cell:hover {
  background: var(--el-color-primary-light-9);
  border-color: var(--el-color-primary);
}

.icon-cell.active {
  background: var(--el-color-primary-light-9);
  border-color: var(--el-color-primary);
  box-shadow: 0 0 0 1px var(--el-color-primary) inset;
}

.cell-icon {
  font-size: 22px;
  color: var(--el-text-color-primary);
}

.cell-name {
  width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  font-size: 11px;
  line-height: 1.3;
  color: var(--el-text-color-secondary);
  text-align: center;
  white-space: nowrap;
}
</style>
