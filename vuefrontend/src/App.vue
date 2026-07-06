<template>
  <el-config-provider :locale="currentLocale">
    <router-view />
    <ReDialog />
    <ReDrawer />
  </el-config-provider>
</template>

<script lang="ts">
import { defineComponent, onMounted, computed } from "vue";
import { checkVersion } from "version-rocket";
import { ElConfigProvider } from "element-plus";
import { useGlobal } from "@pureadmin/utils";
import { ReDialog } from "@/components/ReDialog";
import { ReDrawer } from "@/components/ReDrawer";
import en from "element-plus/es/locale/lang/en";
import zhCn from "element-plus/es/locale/lang/zh-cn";
import plusEn from "plus-pro-components/es/locale/lang/en";
import plusZhCn from "plus-pro-components/es/locale/lang/zh-cn";
import { leftAsideStore } from "maotu";
export default defineComponent({
  name: "app",
  components: {
    [ElConfigProvider.name]: ElConfigProvider,
    ReDialog,
    ReDrawer
  },
  setup() {
    const { $storage } = useGlobal<GlobalPropertiesApi>();

    // 计算当前语言环境
    const currentLocale = computed(() => {
      return $storage.locale?.locale === "zh"
        ? { ...zhCn, ...plusZhCn }
        : { ...en, ...plusEn };
    });

    return {
      currentLocale
    };
  },
  beforeCreate() {
    const { version, name: title } = __APP_INFO__.pkg;
    const { VITE_PUBLIC_PATH, MODE } = import.meta.env;
    // https://github.com/guMcrey/version-rocket/blob/main/README.zh-CN.md#api
    if (MODE === "production") {
      // 版本实时更新检测，只作用于线上环境
      checkVersion(
        // config
        {
          // 5分钟检测一次版本
          pollingTime: 300000,
          localPackageVersion: version,
          originVersionFileUrl: `${location.origin}${VITE_PUBLIC_PATH}version.json`
        },
        // options
        {
          title,
          description: "检测到新版本",
          buttonText: "立即更新"
        }
      );
    }
    let imgConfigData = [];
    for (let i = 0; i < 7; i++) {
      imgConfigData.push({
        id: i + 1,
        title: `图片${i + 1}`,
        type: "img",
        thumbnail: `/imgs/${i + 1}.png`, //支持远程地址,也支持svg，只是不能变色之类的了，不过会保留矢量属性
        props: {}
      });
    }
    leftAsideStore.registerConfig("图片", imgConfigData);
  }
});
</script>
