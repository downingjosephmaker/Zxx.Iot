<script setup lang="ts">
import { ref, onMounted, h } from "vue";
import { ChangeUserTokenByUnit } from "@/api/system";
import { storage } from "@/utils/storage";
import { addDialog } from "@/components/ReDialog";
import ReUnitModal from "@/components/ReUnitModal/index.vue";
import { message } from "@/utils/message";
import { setToken } from "@/utils/auth";

// Fix TypeScript errors by adding proper interface for user-info
interface UserInfo {
  unitname: string;
  refreshToken: string;
  UnitAllCount?: number;
  [key: string]: any;
}

const screenIcon = ref();
const unitData = ref([]);
const UnitName = ref("");
const isHovered = ref(false);
const showUnitSelector = ref(false);

const currentUnitInfo = ref<UserInfo | null>(null);
const formRef = ref();
const showModalFun = () => {
  addDialog({
    title: `单位切换`,
    props: {},
    width: "80%",
    draggable: true,
    fullscreenIcon: true,
    closeOnClickModal: false,
    contentRenderer: () => h(ReUnitModal, { ref: formRef }),
    beforeSure: async (done, { options }) => {
      const FormRefz = formRef.value.getRef();
      UnitName.value = FormRefz.UnitName;
      await ChangeUserTokenByUnit(FormRefz.UnitId).then(res => {
        if (res.Status) {
          // Fix TypeScript error by casting to our interface
          const dataz = storage.getItem("user-info") as UserInfo;
          dataz.unitname = FormRefz.UnitName;
          dataz.refreshToken = JSON.parse(res.Result);
          setToken({
            unitname: FormRefz.UnitName,
            refreshToken: JSON.parse(res.Result),
            accessToken: JSON.parse(res.Result),
            expires: "" as any // Cast to any to avoid Date type error
          });
          storage.setItem("user-info", dataz);
          storage.setItem("token", JSON.parse(res.Result));
          message(res.Message, { type: "success" });
          done(); // 关闭弹框
          window.location.reload();
        } else {
          message(res.Message, { type: "error" });
        }
      });
    }
  });
};

onMounted(() => {
  // Fix TypeScript error by casting to our interface
  const dataz = storage.getItem("user-info") as UserInfo;
  if (dataz && dataz.unitname) {
    UnitName.value = dataz.unitname;
  }

  // 检查UnitAllCount，如果大于1则显示组件，否则隐藏
  if (dataz && dataz.UnitAllCount && dataz.UnitAllCount > 1) {
    showUnitSelector.value = true;
  } else {
    showUnitSelector.value = false;
  }
});
</script>

<template>
  <div
    v-if="showUnitSelector"
    class="unit-selector"
    @click="showModalFun"
    @mouseenter="isHovered = true"
    @mouseleave="isHovered = false"
  >
    <div class="unit-icon">
      <el-icon>
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          :style="{ fill: 'currentColor' }"
        >
          <path
            d="M12 7V3H2v18h20V7H12zM6 19H4v-2h2v2zm0-4H4v-2h2v2zm0-4H4V9h2v2zm0-4H4V5h2v2zm4 12H8v-2h2v2zm0-4H8v-2h2v2zm0-4H8V9h2v2zm0-4H8V5h2v2zm10 12h-8v-2h2v-2h-2v-2h2v-2h-2V9h8v10zm-2-8h-2v2h2v-2zm0 4h-2v2h2v-2z"
          />
        </svg>
      </el-icon>
    </div>
    <div class="unit-content">
      <span class="unit-label">当前单位</span>
      <div class="unit-value-container">
        <span class="unit-value">{{ UnitName || "请选择单位" }}</span>
        <el-icon class="arrow-icon" :class="{ 'is-active': isHovered }">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 24 24"
            :style="{ fill: 'currentColor' }"
          >
            <path d="M7 10l5 5 5-5z" />
          </svg>
        </el-icon>
      </div>
    </div>
  </div>
</template>

<style scoped>
.unit-selector {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  border-radius: 8px;
  background: var(--el-bg-color-overlay, #fff);
  box-shadow: var(--el-box-shadow-light);
  transition: all 0.3s ease;
  cursor: pointer;
  position: relative;
  width: 200px;
  overflow: hidden;
  border: 1px solid var(--el-border-color-light);
}

.unit-selector:hover {
  box-shadow: var(--el-box-shadow);
  transform: translateY(-1px);
  border-color: var(--el-color-primary-light-5);
}

.unit-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: 8px;
  background: var(--el-color-primary-light-9);
  color: var(--el-color-primary);
  font-size: 18px;
}

.unit-content {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-width: 0;
}

.unit-label {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-bottom: 2px;
}

.unit-value-container {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.unit-value {
  font-size: 14px;
  font-weight: 500;
  color: var(--el-text-color-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: calc(100% - 20px);
}

.arrow-icon {
  width: 16px;
  height: 16px;
  transition: transform 0.3s ease;
}

.arrow-icon.is-active {
  transform: rotate(180deg);
}

/* Dark mode compatibility */
html.dark .unit-icon {
  background: var(--el-color-primary-light-8);
}

/* Responsive adjustments */
@media (max-width: 768px) {
  .unit-selector {
    max-width: 180px;
  }

  .unit-icon {
    width: 32px;
    height: 32px;
  }
}
</style>
