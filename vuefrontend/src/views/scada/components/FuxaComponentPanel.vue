<template>
  <div class="fuxa-component-panel">
    <!-- 组件库标题和搜索 -->
    <div class="panel-header">
      <div class="panel-title">
        <el-icon><Grid /></el-icon>
        <span>FUXA组件库</span>
      </div>
      <div class="search-box">
        <el-input
          v-model="searchKeyword"
          placeholder="搜索组件..."
          size="small"
          clearable
          @input="handleSearch"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-input>
      </div>
    </div>

    <!-- 折叠面板组件分组 -->
    <div class="component-groups">
      <div
        v-for="group in filteredGroups"
        :key="group.key"
        class="component-group"
      >
        <!-- 分组标题 -->
        <div
          class="group-header"
          @click.stop="toggleGroup(group.key)"
          @mousedown.stop
        >
          <span class="group-title">{{ group.title }}</span>
          <span class="component-count">({{ group.components.length }})</span>
          <el-icon
            class="expand-icon"
            :class="{ expanded: expandedGroups.includes(group.key) }"
          >
            <ArrowRight />
          </el-icon>
        </div>

        <!-- 分组内容 -->
        <div v-show="expandedGroups.includes(group.key)" class="group-content">
          <div class="component-grid">
            <button
              v-for="component in group.components"
              :key="component.name"
              class="fuxa-component-button"
              :class="{
                active: selectedComponent?.name === component.name,
                disabled: component.disabled
              }"
              :title="`${component.title}\n${component.description || ''}`"
              :disabled="component.disabled"
              draggable="true"
              @dragstart="handleDragStart($event, component)"
              @click.stop="handleComponentActivate(component)"
              @dblclick.stop="handleAddComponent(component)"
            >
              <!-- 组件图标 - 组件库中使用img标签，性能更好 -->
              <div class="component-icon">
                <img
                  v-if="component.svgPath"
                  :src="component.svgPath"
                  :alt="component.title"
                  class="component-svg-icon"
                  width="28"
                  height="28"
                />
                <el-icon v-else-if="component.icon" :size="28">
                  <component :is="component.icon" />
                </el-icon>
                <div v-else class="default-icon">
                  <el-icon :size="28"><Box /></el-icon>
                </div>
              </div>
              <!-- 组件名称 -->
              <div class="component-name">{{ component.title }}</div>
              <!-- 组件标签 -->
              <!-- <div v-if="component.tags?.length" class="component-tags">
                <el-tag
                  v-for="tag in component.tags.slice(0, 2)"
                  :key="tag"
                  size="small"
                  effect="plain"
                >
                  {{ tag }}
                </el-tag>
              </div> -->
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 组件详情面板 -->
    <div v-if="selectedComponent" class="component-detail">
      <div class="detail-header">
        <h4>{{ selectedComponent.title }}</h4>
        <el-button size="small" @click="selectedComponent = null">
          <el-icon><Close /></el-icon>
        </el-button>
      </div>
      <div class="detail-content">
        <p v-if="selectedComponent.description">
          {{ selectedComponent.description }}
        </p>
        <div class="detail-properties">
          <h5>属性配置:</h5>
          <ul>
            <li v-for="prop in selectedComponent.properties" :key="prop.name">
              <strong>{{ prop.label }}:</strong> {{ prop.description }}
            </li>
          </ul>
        </div>
        <div class="detail-actions">
          <el-button
            type="primary"
            size="small"
            @click="handleAddComponent(selectedComponent)"
          >
            添加到画布
          </el-button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from "vue";
import { ElMessage } from "element-plus";

// 组件图标
import { Grid, Search, ArrowRight, Close, Box } from "@element-plus/icons-vue";

// 导入SVG图标组件和组件库映射
import SvgIcon from "./SvgIcon.vue";
import {
  regularComponents,
  controlComponents,
  shapeComponents,
  processEngineeringComponents,
  animationComponents,
  widgetComponents,
  resourceComponents,
  type ComponentIconMapping
} from "../core/fuxa-icon-mapping";

// 导入新的组件管理器
import { componentManager } from "../core/ComponentManager";

// 定义组件接口
interface FuxaComponentItem {
  name: string;
  title: string;
  description?: string;
  category: string;
  type: string;
  icon?: any;
  iconUrl?: string;
  svgPath?: string;
  tags?: string[];
  disabled?: boolean;
  width?: number;
  height?: number;
  properties: Array<{
    name: string;
    label: string;
    type: string;
    description: string;
    defaultValue?: any;
  }>;
}

// 定义组件分组接口
interface FuxaComponentGroup {
  key: string;
  title: string;
  components: FuxaComponentItem[];
}

// 响应式数据
const searchKeyword = ref("");
const selectedComponent = ref<FuxaComponentItem | null>(null);
const expandedGroups = ref<string[]>(["basic"]);

// 转换Vue别名路径为实际路径
const convertVuePath = (vuePath: string): string => {
  if (vuePath.startsWith("@/assets/svg/")) {
    // 在Vite中，静态资源路径需要使用new URL来正确处理
    try {
      const fileName = vuePath.replace("@/assets/svg/", "");
      return new URL(`/src/assets/svg/${fileName}`, import.meta.url).href;
    } catch (error) {
      console.warn("SVG路径转换失败:", vuePath, error);
      // 降级方案：直接使用相对路径
      return vuePath.replace("@/assets/svg/", "/src/assets/svg/");
    }
  }
  return vuePath;
};

// 转换已有组件库数据为FUXA格式
const convertComponentsToFuxaFormat = (
  components: ComponentIconMapping[],
  categoryKey: string,
  categoryTitle: string
): FuxaComponentGroup => {
  return {
    key: categoryKey,
    title: categoryTitle,
    components: components.map(comp => ({
      name: comp.name,
      title: comp.title || comp.name,
      description: comp.description || comp.title || comp.name,
      category: categoryKey,
      type: comp.name,
      svgPath: convertVuePath(comp.iconPath), // 转换Vue别名路径为实际路径
      width: 60,
      height: 60,
      tags: [categoryTitle, comp.category],
      properties: [
        {
          name: "width",
          label: "宽度",
          type: "number",
          description: "组件宽度",
          defaultValue: 60
        },
        {
          name: "height",
          label: "高度",
          type: "number",
          description: "组件高度",
          defaultValue: 60
        },
        {
          name: "x",
          label: "X坐标",
          type: "number",
          description: "X坐标位置",
          defaultValue: 0
        },
        {
          name: "y",
          label: "Y坐标",
          type: "number",
          description: "Y坐标位置",
          defaultValue: 0
        }
      ]
    }))
  };
};

// FUXA组件库定义 - 使用已有的组件库数据
const fuxaComponentGroups: FuxaComponentGroup[] = [
  convertComponentsToFuxaFormat(regularComponents, "basic", "常规"),
  convertComponentsToFuxaFormat(controlComponents, "controls", "控制"),
  convertComponentsToFuxaFormat(shapeComponents, "shapes", "图形"),
  convertComponentsToFuxaFormat(
    processEngineeringComponents,
    "industrial",
    "工业"
  ),
  convertComponentsToFuxaFormat(animationComponents, "animation", "动画"),
  convertComponentsToFuxaFormat(widgetComponents, "widget", "小部件"),
  convertComponentsToFuxaFormat(resourceComponents, "resource", "资源")
].filter(group => group.components.length > 0);

// 计算属性
const filteredGroups = computed(() => {
  if (!searchKeyword.value.trim()) {
    return fuxaComponentGroups;
  }

  const keyword = searchKeyword.value.toLowerCase();
  return fuxaComponentGroups
    .map(group => ({
      ...group,
      components: group.components.filter(
        comp =>
          comp.title.toLowerCase().includes(keyword) ||
          comp.description?.toLowerCase().includes(keyword) ||
          comp.tags?.some(tag => tag.toLowerCase().includes(keyword))
      )
    }))
    .filter(group => group.components.length > 0);
});

// 事件处理方法
const toggleGroup = (groupKey: string) => {
  console.log('=== toggleGroup 开始 ===');
  console.log('groupKey:', groupKey);
  console.log('当前展开的分组:', JSON.stringify(expandedGroups.value));

  const index = expandedGroups.value.indexOf(groupKey);
  console.log('groupKey在数组中的索引:', index);

  if (index > -1) {
    // 收缩：从数组中移除
    const newGroups = expandedGroups.value.filter(key => key !== groupKey);
    expandedGroups.value = newGroups;
    console.log('✅ 分组已收缩:', groupKey);
    console.log('剩余展开的分组:', JSON.stringify(newGroups));
  } else {
    // 展开：添加到数组
    const newGroups = [...expandedGroups.value, groupKey];
    expandedGroups.value = newGroups;
    console.log('✅ 分组已展开:', groupKey);
    console.log('当前展开的分组:', JSON.stringify(newGroups));
  }

  console.log('=== toggleGroup 结束 ===');
};

const handleSearch = () => {
  // 搜索时自动展开有结果的分组
  if (searchKeyword.value.trim()) {
    expandedGroups.value = filteredGroups.value.map(group => group.key);
  }
};

const handleComponentClick = (component: FuxaComponentItem) => {
  selectedComponent.value = component;
};

const handleDragStart = (event: DragEvent, component: FuxaComponentItem) => {
  if (!event.dataTransfer) return;

  // 设置拖拽数据
  const dragData = {
    type: "fuxa-component",
    component: component,
    source: "component-panel"
  };

  event.dataTransfer.setData("application/json", JSON.stringify(dragData));
  event.dataTransfer.setData("text/plain", component.name);
  event.dataTransfer.effectAllowed = "copy";

  // 设置拖拽图像
  const dragImage = createDragImage(component);
  event.dataTransfer.setDragImage(dragImage, 20, 20);

  ElMessage.info(`正在拖拽组件: ${component.title}`);
};

// 创建拖拽预览图像
const createDragImage = (component: FuxaComponentItem) => {
  const dragImage = document.createElement("div");
  dragImage.style.cssText = `
    position: absolute;
    top: -1000px;
    left: -1000px;
    width: 80px;
    height: 80px;
    background: #ffffff;
    border: 2px solid #409eff;
    border-radius: 6px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    font-size: 11px;
    color: #303133;
    box-shadow: 0 2px 8px rgba(64, 158, 255, 0.3);
  `;

  dragImage.innerHTML = `
    <div style="margin-bottom: 4px; color: #606266;">
      <i class="el-icon-${component.icon || "box"}" style="font-size: 28px;"></i>
    </div>
    <div>${component.title}</div>
  `;

  document.body.appendChild(dragImage);

  // 延迟移除
  setTimeout(() => {
    document.body.removeChild(dragImage);
  }, 100);

  return dragImage;
};

const handleAddComponent = (component: FuxaComponentItem) => {
  // 触发添加组件事件，由父组件处理
  emit("addComponent", component);
  ElMessage.success(`已添加组件: ${component.title}`);
};

// 处理组件点击激活模式
const handleComponentActivate = (component: FuxaComponentItem) => {
  // 触发组件激活事件，设置编辑器模式
  emit("activateComponent", component);
  ElMessage.info(`已激活组件模式: ${component.title}`);
};

// 组件事件
const emit = defineEmits<{
  addComponent: [component: FuxaComponentItem];
  selectComponent: [component: FuxaComponentItem];
  activateComponent: [component: FuxaComponentItem];
}>();

// 生命周期
onMounted(() => {
  const totalComponents = fuxaComponentGroups.reduce(
    (total, group) => total + group.components.length,
    0
  );
  console.log("FUXA组件库已加载，共", totalComponents, "个组件");
  console.log(
    "组件分组情况:",
    fuxaComponentGroups.map(group => ({
      title: group.title,
      count: group.components.length,
      samples: group.components.slice(0, 3).map(comp => ({
        name: comp.name,
        title: comp.title,
        svgPath: comp.svgPath
      }))
    }))
  );

  // 测试第一个组件的SVG路径
  if (
    fuxaComponentGroups.length > 0 &&
    fuxaComponentGroups[0].components.length > 0
  ) {
    const firstComponent = fuxaComponentGroups[0].components[0];
    console.log("测试第一个组件SVG路径:", {
      component: firstComponent.name,
      svgPath: firstComponent.svgPath
    });
  }
});
</script>

<style scoped lang="scss">
.fuxa-component-panel {
  height: 100%;
  display: flex;
  flex-direction: column;
  background: #ffffff;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  overflow: hidden;
}

.panel-header {
  padding: 12px;
  border-bottom: 1px solid #e4e7ed;
  background: #f8f9fa;

  .panel-title {
    display: flex;
    align-items: center;
    font-weight: 600;
    color: #303133;
    margin-bottom: 8px;

    .el-icon {
      margin-right: 6px;
      color: #409eff;
    }
  }

  .search-box {
    :deep(.el-input) {
      --el-input-border-color: #dcdfe6;

      .el-input__wrapper {
        border-radius: 16px;
      }
    }
  }
}

.component-groups {
  flex: 1;
  overflow-y: auto;
  padding: 8px 0;
}

.component-group {
  margin-bottom: 4px;

  .group-header {
    display: flex;
    align-items: center;
    padding: 8px 12px;
    cursor: pointer;
    background: #f8f9fa;
    border-top: 1px solid #e4e7ed;
    transition: all 0.2s;
    user-select: none; // 防止文字选中
    pointer-events: auto; // 确保能接收点击事件
    position: relative; // 确保z-index生效
    z-index: 1; // 确保在其他元素之上

    &:hover {
      background: #e9ecef;
    }

    // 增强点击反馈效果
    &:active {
      background: #dee2e6;
    }

    .group-title {
      flex: 1;
      font-weight: 500;
      color: #303133;
      cursor: pointer; // 确保文字区域也显示手型光标
    }

    .component-count {
      font-size: 12px;
      color: #909399;
      margin-right: 8px;
      cursor: pointer; // 确保计数区域也显示手型光标
    }

    .expand-icon {
      transition: transform 0.2s;
      color: #909399;
      cursor: pointer; // 确保图标区域也显示手型光标

      &.expanded {
        transform: rotate(90deg);
      }
    }
  }

  .group-content {
    padding: 8px 12px;
  }

  .component-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(80px, 1fr));
    gap: 8px;
  }
}

.fuxa-component-button {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 8px 4px;
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  background: #ffffff;
  cursor: pointer;
  transition: all 0.2s;
  min-height: 80px;
  position: relative;

  &:hover:not(.disabled) {
    border-color: #409eff;
    background: #f0f8ff;
    transform: translateY(-1px);
    box-shadow: 0 2px 8px rgba(64, 158, 255, 0.2);
  }

  &.active {
    border-color: #409eff;
    background: #ecf5ff;
  }

  &.disabled {
    opacity: 0.5;
    cursor: not-allowed;
    background: #f5f7fa;
  }

  .component-icon {
    margin-bottom: 4px;

    .el-icon {
      color: #606266;
    }

    .component-svg-icon {
      filter: brightness(0.8) contrast(1.1);
      transition: all 0.2s;
    }

    img {
      width: 28px;
      height: 28px;
      object-fit: contain;
    }

    .default-icon .el-icon {
      color: #c0c4cc;
    }
  }

  &:hover:not(.disabled) .component-icon .component-svg-icon {
    filter: brightness(1) contrast(1.2);
    transform: scale(1.05);
  }

  .component-name {
    font-size: 11px;
    color: #303133;
    text-align: center;
    line-height: 1.2;
    word-break: break-word;
    margin-bottom: 2px;
  }

  .component-tags {
    display: flex;
    flex-wrap: wrap;
    gap: 2px;
    justify-content: center;

    .el-tag {
      font-size: 10px;
      height: 16px;
      line-height: 14px;
      padding: 0 4px;
    }
  }
}

.component-detail {
  border-top: 1px solid #e4e7ed;
  background: #f8f9fa;
  max-height: 300px;
  overflow-y: auto;

  .detail-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 12px;
    border-bottom: 1px solid #e4e7ed;

    h4 {
      margin: 0;
      color: #303133;
      font-size: 14px;
    }
  }

  .detail-content {
    padding: 12px;

    p {
      margin: 0 0 12px 0;
      color: #606266;
      font-size: 13px;
      line-height: 1.4;
    }

    .detail-properties {
      margin-bottom: 12px;

      h5 {
        margin: 0 0 8px 0;
        color: #303133;
        font-size: 12px;
        font-weight: 600;
      }

      ul {
        margin: 0;
        padding-left: 16px;
        font-size: 12px;
        color: #606266;

        li {
          margin-bottom: 4px;
          line-height: 1.3;

          strong {
            color: #303133;
          }
        }
      }
    }

    .detail-actions {
      text-align: center;
    }
  }
}

// 滚动条样式
:deep(.component-groups::-webkit-scrollbar) {
  width: 6px;
}

:deep(.component-groups::-webkit-scrollbar-track) {
  background: #f1f1f1;
  border-radius: 3px;
}

:deep(.component-groups::-webkit-scrollbar-thumb) {
  background: #c1c1c1;
  border-radius: 3px;
}

:deep(.component-groups::-webkit-scrollbar-thumb:hover) {
  background: #a8a8a8;
}
</style>
