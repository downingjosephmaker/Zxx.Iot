<script setup lang="ts">
import { computed } from "vue";
import { getReportUrl } from "@/config";
import { getToken } from "@/utils/auth";

defineOptions({
  name: "LowCodeReport"
});

// JimuReport 旁挂服务地址（platform-config.json 的 ReportUrl，空=未部署）；
// token 以查询参数带入，报表服务端 JmReportTokenServiceI 回调 /Api/ReportDataset/VerifyToken 验真
const frameSrc = computed(() => {
  const base = getReportUrl();
  if (!base) return "";
  const token = getToken()?.accessToken ?? "";
  return `${base}${base.includes("?") ? "&" : "?"}token=${encodeURIComponent(token)}`;
});
</script>

<template>
  <div class="lowcode-report">
    <el-empty
      v-if="!frameSrc"
      description="未配置报表服务地址，请在 platform-config.json 中设置 ReportUrl（JimuReport 服务地址）"
    />
    <iframe v-else :src="frameSrc" class="report-frame" />
  </div>
</template>

<style scoped>
.lowcode-report {
  width: 100%;
  height: calc(100vh - 130px);
}

.report-frame {
  width: 100%;
  height: 100%;
  border: none;
}
</style>
