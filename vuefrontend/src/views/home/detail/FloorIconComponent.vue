<template>
  <div class="floor-icon-wrapper" :style="iconStyles">
    <svg
      xmlns="http://www.w3.org/2000/svg"
      :width="`${size}`"
      :height="`${size}`"
      viewBox="0 0 48 48"
      :fill="fillColor"
      :stroke="color"
      :stroke-width="`${strokeWidth}`"
    >
      <template v-if="floorNum === 1">
        <rect x="6" y="26" width="36" height="16" rx="2" stroke-width="2" />
        <path
          d="M10 26V20C10 19.4477 10.4477 19 11 19H37C37.5523 19 38 19.4477 38 20V26"
          stroke-width="1.5"
        />
        <path d="M24 42V26" stroke-width="1.5" />
        <rect
          x="20"
          y="34"
          width="8"
          height="6"
          rx="1"
          :fill="color"
          :fill-opacity="fillOpacity"
          opacity="0.5"
        />
        <path
          d="M21 19V13.5C21 12.6716 21.6716 12 22.5 12H25.5C26.3284 12 27 12.6716 27 13.5V19"
          stroke-width="1.5"
        />
      </template>

      <template v-else-if="floorNum === 2">
        <rect x="6" y="26" width="36" height="16" rx="2" stroke-width="2" />
        <rect
          x="10"
          y="10"
          width="28"
          height="16"
          rx="2"
          stroke-width="2"
          opacity="0.8"
        />
        <path d="M24 42V26" stroke-width="1.5" />
        <path d="M20 26V18" stroke-width="1.5" />
        <path d="M28 26V18" stroke-width="1.5" />
        <rect
          x="20"
          y="34"
          width="8"
          height="6"
          rx="1"
          :fill="color"
          :fill-opacity="fillOpacity"
          opacity="0.5"
        />
      </template>

      <template v-else-if="floorNum === 3">
        <rect x="6" y="30" width="36" height="12" rx="2" stroke-width="2" />
        <rect
          x="10"
          y="18"
          width="28"
          height="12"
          rx="2"
          stroke-width="2"
          opacity="0.8"
        />
        <rect
          x="14"
          y="6"
          width="20"
          height="12"
          rx="2"
          stroke-width="2"
          opacity="0.6"
        />
        <path d="M24 42V30" stroke-width="1.5" />
        <path d="M20 30V18" stroke-width="1.5" />
        <path d="M28 30V18" stroke-width="1.5" />
        <path d="M24 18V6" stroke-width="1.5" />
        <rect
          x="20"
          y="36"
          width="8"
          height="4"
          rx="1"
          :fill="color"
          :fill-opacity="fillOpacity"
          opacity="0.5"
        />
      </template>

      <template v-else-if="floorNum === 4">
        <rect x="6" y="32" width="36" height="10" rx="2" stroke-width="2" />
        <rect
          x="10"
          y="22"
          width="28"
          height="10"
          rx="2"
          stroke-width="2"
          opacity="0.8"
        />
        <rect
          x="14"
          y="12"
          width="20"
          height="10"
          rx="2"
          stroke-width="2"
          opacity="0.6"
        />
        <rect
          x="18"
          y="2"
          width="12"
          height="10"
          rx="2"
          stroke-width="2"
          opacity="0.4"
        />
        <path d="M24 42V2" stroke-width="1.5" />
        <rect
          x="20"
          y="37"
          width="8"
          height="3"
          rx="1"
          :fill="color"
          :fill-opacity="fillOpacity"
          opacity="0.5"
        />
      </template>

      <template v-else-if="floorNum === 5">
        <path
          d="M42 42H6C4.89543 42 4 41.1046 4 40V38C4 36.8954 4.89543 36 6 36H42C43.1046 36 44 36.8954 44 38V40C44 41.1046 43.1046 42 42 42Z"
          stroke-width="2"
        />
        <path
          d="M38 36H10C8.89543 36 8 35.1046 8 34V32C8 30.8954 8.89543 30 10 30H38C39.1046 30 40 30.8954 40 32V34C40 35.1046 39.1046 36 38 36Z"
          stroke-width="2"
          opacity="0.9"
        />
        <path
          d="M34 30H14C12.8954 30 12 29.1046 12 28V26C12 24.8954 12.8954 24 14 24H34C35.1046 24 36 24.8954 36 26V28C36 29.1046 35.1046 30 34 30Z"
          stroke-width="2"
          opacity="0.7"
        />
        <path
          d="M30 24H18C16.8954 24 16 23.1046 16 22V20C16 18.8954 16.8954 18 18 18H30C31.1046 18 32 18.8954 32 20V22C32 23.1046 31.1046 24 30 24Z"
          stroke-width="2"
          opacity="0.5"
        />
        <path
          d="M26 18H22C20.8954 18 20 17.1046 20 16V14C20 12.8954 20.8954 12 22 12H26C27.1046 12 28 12.8954 28 14V16C28 17.1046 27.1046 18 26 18Z"
          stroke-width="2"
          opacity="0.3"
        />
        <path d="M24 42V12" stroke-width="1.5" />
        <rect
          x="20"
          y="37"
          width="8"
          height="3"
          rx="1"
          :fill="color"
          :fill-opacity="fillOpacity"
          opacity="0.5"
        />
      </template>

      <template v-else>
        <!-- 默认图标 (六楼及以上) -->
        <path
          d="M42 42H6C4.89543 42 4 41.1046 4 40V38C4 36.8954 4.89543 36 6 36H42C43.1046 36 44 36.8954 44 38V40C44 41.1046 43.1046 42 42 42Z"
          stroke-width="2"
        />
        <path
          d="M38 36H10C8.89543 36 8 35.1046 8 34V32C8 30.8954 8.89543 30 10 30H38C39.1046 30 40 30.8954 40 32V34C40 35.1046 39.1046 36 38 36Z"
          stroke-width="2"
          opacity="0.85"
        />
        <path
          d="M34 30H14C12.8954 30 12 29.1046 12 28V26C12 24.8954 12.8954 24 14 24H34C35.1046 24 36 24.8954 36 26V28C36 29.1046 35.1046 30 34 30Z"
          stroke-width="2"
          opacity="0.7"
        />
        <path
          d="M30 24H18C16.8954 24 16 23.1046 16 22V20C16 18.8954 16.8954 18 18 18H30C31.1046 18 32 18.8954 32 20V22C32 23.1046 31.1046 24 30 24Z"
          stroke-width="2"
          opacity="0.55"
        />
        <path
          d="M26 18H22C20.8954 18 20 17.1046 20 16V14C20 12.8954 20.8954 12 22 12H26C27.1046 12 28 12.8954 28 14V16C28 17.1046 27.1046 18 26 18Z"
          stroke-width="2"
          opacity="0.4"
        />
        <path
          d="M24 18H24C22.8954 18 22 17.1046 22 16V8C22 6.89543 22.8954 6 24 6H24C25.1046 6 26 6.89543 26 8V16C26 17.1046 25.1046 18 24 18Z"
          stroke-width="2"
          opacity="0.25"
        />
        <path d="M24 42V6" stroke-width="1.5" />
        <rect
          x="20"
          y="37"
          width="8"
          height="3"
          rx="1"
          :fill="color"
          :fill-opacity="fillOpacity"
          opacity="0.5"
        />
      </template>
    </svg>
    <div v-if="showLabel" class="floor-icon-label">{{ floorLabel }}</div>
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";

interface Props {
  floor: string;
  color?: string;
  size?: number | string;
  strokeWidth?: number | string;
  glowEffect?: boolean;
  showLabel?: boolean;
  isActive?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  color: "#00c9ff",
  size: 48,
  strokeWidth: 2,
  glowEffect: true,
  showLabel: false,
  isActive: false
});

const floorNum = computed(() => {
  if (!props.floor) return 0;
  const match = props.floor.match(/-?\\d+/);
  return match ? parseInt(match[0], 10) : 0;
});

const floorLabel = computed(() => {
  if (!props.floor) return "";
  if (props.floor.includes("B") || props.floor.includes("负")) {
    return `B${Math.abs(floorNum.value)}`;
  }
  return `${floorNum.value}F`;
});

const iconStyles = computed(() => ({
  filter: props.glowEffect ? `drop-shadow(0 0 8px ${props.color})` : "none",
  transform: props.isActive ? "scale(1.1)" : "scale(1)"
}));

const fillColor = computed(() =>
  props.isActive ? props.color : "transparent"
);

const fillOpacity = computed(() => (props.isActive ? 0.3 : 0.5));
</script>

<style scoped>
.floor-icon-wrapper {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  color: var(--icon-color, currentColor);
  transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
  position: relative;
}

.floor-icon-wrapper:hover {
  transform: scale(1.05);
}

.floor-icon-wrapper svg {
  width: 100%;
  height: 100%;
  max-width: 100%;
  max-height: 100%;
  fill: none;
}

.floor-icon-label {
  display: none; /* 隐藏标签 */
  font-size: 0.8em;
  margin-top: 8px;
  font-weight: 500;
  opacity: 0.9;
  text-shadow: 0 0 5px rgba(0, 0, 0, 0.3);
}
</style>
