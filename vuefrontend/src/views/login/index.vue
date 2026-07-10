<script setup lang="ts">
import { useI18n } from "vue-i18n";
import Motion from "./utils/motion";
import { useRouter } from "vue-router";
import { message } from "@/utils/message";
import { loginRules } from "./utils/rule";
import TypeIt from "@/components/ReTypeit";
import { debounce } from "@pureadmin/utils";
import { useNav } from "@/layout/hooks/useNav";
import { useEventListener } from "@vueuse/core";
import type { FormInstance } from "element-plus";
import { $t, transformI18n } from "@/plugins/i18n";
import { operates, thirdParty } from "./utils/enums";
import { useLayout } from "@/layout/hooks/useLayout";
import LoginPhone from "./components/LoginPhone.vue";
import LoginRegist from "./components/LoginRegist.vue";
import LoginUpdate from "./components/LoginUpdate.vue";
import LoginQrCode from "./components/LoginQrCode.vue";
import { storage } from "@/utils/storage";
import { getSignalRUrl } from "@/config";
import md5 from "md5";
import JSEncrypt from "jsencrypt";
import { useUserStoreHook } from "@/store/modules/user";
import { initRouter, getTopMenu } from "@/router/utils";
import { bg, login, avatar, illustration } from "./utils/static";
import { ReImageVerify } from "@/components/ReImageVerify";
import {
  ref,
  toRaw,
  reactive,
  watch,
  computed,
  onMounted,
  nextTick
} from "vue";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { useTranslationLang } from "@/layout/hooks/useTranslationLang";
import { useDataThemeChange } from "@/layout/hooks/useDataThemeChange";
import {
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel
} from "@microsoft/signalr";

import dayIcon from "@/assets/svg/day.svg?component";
import darkIcon from "@/assets/svg/dark.svg?component";
import globalization from "@/assets/svg/globalization.svg?component";
import Lock from "~icons/ri/lock-fill";
import Check from "~icons/ep/check";
import User from "~icons/ri/user-3-fill";
import Keyhole from "~icons/ri/shield-keyhole-line";

defineOptions({
  name: "Login"
});
const PUBLIC_KEY =
  "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDR79EzLf9vlig9TIxCEnpOKgAoed6f0l+JDQO03eSi0rybQeUh/Oi0TF1wySvqEUhY/HbBFnZr2/JqrPTnlNfCOiTlGI47BXsndN1E5AG/wj8MUkZ5p3PMMrvqN+Zyf1MYDc7K4Eub0oGLwUgM9LHXVltKPLzazPZS5EkszYBetQIDAQAB";
const imgCode = ref("");
const router = useRouter();
const loading = ref(false);
const disabled = ref(false);
const ruleFormRef = ref<FormInstance>();
const currentPage = computed(() => {
  return useUserStoreHook().currentPage;
});

const { t } = useI18n();
const { initStorage } = useLayout();
initStorage();
const { dataTheme, overallStyle, dataThemeChange } = useDataThemeChange();
dataThemeChange(overallStyle.value);
const { title, getDropdownItemStyle, getDropdownItemClass } = useNav();
const { locale, translationCh, translationEn } = useTranslationLang();
const ruleForm = reactive({
  username: "",
  password: "",
  verifyCode: "",
  isRemember: false
});
// 混合加密函数
const hybridEncrypt = (password: string) => {
  const md5Hash = md5(password); // 生成MD5哈希
  console.log("🚀 ~ hybridEncrypt ~ md5Hash:", md5Hash);
  const encryptor = new JSEncrypt();
  encryptor.setPublicKey(PUBLIC_KEY);
  return encryptor.encrypt(md5Hash) || ""; // 对哈希值进行RSA加密
};
// 创建 SignalR 连接
const connection = ref(null);
onMounted(async () => {
  window.document.addEventListener("keypress", onkeypress);
  const hash = window.location.hash;
  const hashParts = hash.split("?");
  if (hashParts.length > 1) {
    const queryParams = new URLSearchParams(hashParts[1]);
    var username = queryParams.get("username");
    var password = queryParams.get("password");
    var ssoData = queryParams.get("data");

    // 检查是否是单点登录
    if (ssoData) {
      console.log("检测到单点登录参数:", ssoData);
      await handleSSOLogin(ssoData);
      return;
    }
  }

  if (username && password) {
    ruleForm.username = username;
    ruleForm.password = password;
    // 自动调用登录方法
    await onLogin(ruleFormRef.value);
  } else {
    ruleForm.username = storage.getItem("username") || "";
    ruleForm.password = storage.getItem("password") || "";
  }
  connection.value = await startConnection();
  if (ruleForm.username) {
    ruleForm.isRemember = true;
  }
});
const onLogin = async (formEl: FormInstance | undefined) => {
  if (!formEl) return;
  await formEl.validate(valid => {
    if (valid) {
      loading.value = true;
      useUserStoreHook()
        .loginByUsername({
          UserUid: ruleForm.username,
          UserPwd: hybridEncrypt(ruleForm.password),
          SourceType: 1
        })
        .then(res => {
          if (res.Status) {
            if (ruleForm.isRemember) {
              storage.setItem("username", ruleForm.username);
              storage.setItem("password", ruleForm.password);
            } else {
              storage.removeItem("username");
              storage.removeItem("password");
            }
            const Result = JSON.parse(res.Result);
            if (
              connection.value &&
              connection.value.state === HubConnectionState.Connected
            ) {
              connection.value
                .invoke("UserLogin", Result.LoginToken)
                .catch(err => console.error(err));
            }
            // 获取后端路由
            disabled.value = true;
            initRouter()
              .then(() => {
                // 使用 nextTick 确保路由完全初始化
                nextTick(() => {
                  router
                    .push("/project")
                    .then(() => {
                      message("登录成功", { type: "success" });
                    })
                    .catch(err => {
                      console.error("路由跳转失败:", err);
                      // 如果项目管理路由失败，回退到组态编辑器页面
                      router.push("/scada").catch(() => {
                        // 兜底：刷新到项目管理页
                        window.location.href = "/#/project";
                      });
                    });
                });
              })
              .finally(() => (disabled.value = false));
          } else {
            loading.value = false;
          }
        })
        .finally(() => (loading.value = false));
    }
  });
};

// 处理单点登录
const handleSSOLogin = async (ssoData: string) => {
  try {
    loading.value = true;
    console.log("开始单点登录，参数:", ssoData);

    const res = await useUserStoreHook().ssoLogin({ data: ssoData });

    if (res.Status) {
      const Result = JSON.parse(res.Result);
      // SignalR连接处理
      if (
        connection.value &&
        connection.value.state === HubConnectionState.Connected
      ) {
        connection.value
          .invoke("UserLogin", Result.LoginToken)
          .catch((err: any) => console.error("SignalR登录失败:", err));
      }

      // 获取后端路由并跳转
      disabled.value = true;
      initRouter()
        .then(() => {
          router.push("/Cockpit");
          message("单点登录成功", { type: "success" });
        })
        .finally(() => (disabled.value = false));
    } else {
      message(res.Message || "单点登录失败", { type: "error" });
      loading.value = false;
    }
  } catch (error) {
    console.error("单点登录出错:", error);
    message("单点登录出错，请重试", { type: "error" });
    loading.value = false;
  }
};

/** 使用公共函数，避免`removeEventListener`失效 */
function onkeypress({ code }: KeyboardEvent) {
  if (code === "Enter") {
    onLogin(ruleFormRef.value);
  }
}
const startConnection = async () => {
  const connection = new HubConnectionBuilder()
    .withUrl(getSignalRUrl(), {
      skipNegotiation: true,
      transport: HttpTransportType.WebSockets
    })
    .configureLogging(LogLevel.Information)
    .build();
  connection.keepAliveIntervalInMilliseconds = 60 * 1000;
  connection.serverTimeoutInMilliseconds = 130 * 1000;
  try {
    await connection.start();
    // 注册事件处理器
    connection.on("ReceiveAction", (action, data) => {
      console.log(`Received action: ${action} with data:`, data);
      // 可以在这里处理服务器返回的数据
    });
    // 添加连接状态监听器
    connection.onclose(async () => {
      setTimeout(startConnection, 5000); // 重新连接
    });
  } catch (err) {
    setTimeout(startConnection, 5000);
  }
  return connection;
};

const immediateDebounce: any = debounce(
  formRef => onLogin(formRef),
  1000,
  true
);

useEventListener(document, "keydown", ({ code }) => {
  if (
    ["Enter", "NumpadEnter"].includes(code) &&
    !disabled.value &&
    !loading.value
  )
    immediateDebounce(ruleFormRef.value);
});

watch(imgCode, value => {
  useUserStoreHook().SET_VERIFYCODE(value);
});
</script>

<template>
  <div class="select-none">
    <img :src="login" class="wave" />
    <div class="flex-c absolute right-5 top-3">
      <!-- 主题 -->
      <el-switch
        v-model="dataTheme"
        inline-prompt
        :active-icon="dayIcon"
        :inactive-icon="darkIcon"
        @change="dataThemeChange"
      />
      <!-- 国际化 -->
      <!-- <el-dropdown trigger="click">
        <globalization
          class="hover:text-primary hover:bg-[transparent]! w-[20px] h-[20px] ml-1.5 cursor-pointer outline-hidden duration-300"
        />
        <template #dropdown>
          <el-dropdown-menu class="translation">
            <el-dropdown-item
              :style="getDropdownItemStyle(locale, 'zh')"
              :class="['dark:text-white!', getDropdownItemClass(locale, 'zh')]"
              @click="translationCh"
            >
              <IconifyIconOffline
                v-show="locale === 'zh'"
                class="check-zh"
                :icon="Check"
              />
              简体中文
            </el-dropdown-item>
            <el-dropdown-item
              :style="getDropdownItemStyle(locale, 'en')"
              :class="['dark:text-white!', getDropdownItemClass(locale, 'en')]"
              @click="translationEn"
            >
              <span v-show="locale === 'en'" class="check-en">
                <IconifyIconOffline :icon="Check" />
              </span>
              English
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown> -->
    </div>
    <div class="login-container">
      <div class="img">
        <!-- <component :is="toRaw(illustration)" /> -->
      </div>
      <div class="login-box">
        <div class="login-form">
          <!-- <avatar class="avatar" /> -->
          <Motion>
            <h2 class="outline-hidden" style="color: #fff">
              <TypeIt
                :options="{ strings: [title], cursor: false, speed: 100 }"
              />
            </h2>
          </Motion>
          <el-form
            v-if="currentPage === 0"
            ref="ruleFormRef"
            :model="ruleForm"
            :rules="loginRules"
            size="large"
          >
            <Motion :delay="100">
              <el-form-item
                :rules="[
                  {
                    required: true,
                    message: transformI18n($t('login.pureUsernameReg')),
                    trigger: 'blur'
                  }
                ]"
                prop="username"
              >
                <el-input
                  v-model="ruleForm.username"
                  clearable
                  :placeholder="t('login.pureUsername')"
                  :prefix-icon="useRenderIcon(User)"
                />
              </el-form-item>
            </Motion>
            <Motion :delay="150">
              <el-form-item prop="password">
                <el-input
                  v-model="ruleForm.password"
                  clearable
                  show-password
                  :placeholder="t('login.purePassword')"
                  :prefix-icon="useRenderIcon(Lock)"
                />
              </el-form-item>
            </Motion>
            <!-- <Motion :delay="200">
              <el-form-item prop="verifyCode">
                <el-input
                  v-model="ruleForm.verifyCode"
                  clearable
                  :placeholder="t('login.pureVerifyCode')"
                  :prefix-icon="useRenderIcon(Keyhole)"
                >
                  <template v-slot:append>
                    <ReImageVerify v-model:code="imgCode" />
                  </template>
                </el-input>
              </el-form-item>
            </Motion> -->
            <Motion :delay="250">
              <el-form-item>
                <div class="w-full h-[20px] flex justify-between items-center">
                  <div />
                  <el-checkbox v-model="ruleForm.isRemember">
                    <span class="flex text-white"> 记住密码 </span>
                  </el-checkbox>
                  <!-- <el-button
                    link
                    type="primary"
                    @click="useUserStoreHook().SET_CURRENTPAGE(4)"
                  >
                    {{ t("login.pureForget") }}
                  </el-button> -->
                </div>
                <el-button
                  class="w-full mt-4!"
                  size="default"
                  type="primary"
                  :loading="loading"
                  :disabled="disabled"
                  @click="onLogin(ruleFormRef)"
                >
                  {{ t("login.pureLogin") }}
                </el-button>
              </el-form-item>
            </Motion>
            <!-- <Motion :delay="300">
              <el-form-item>
                <div class="w-full h-[20px] flex justify-between items-center">
                  <el-button
                    v-for="(item, index) in operates"
                    :key="index"
                    class="w-full mt-4!"
                    size="default"
                    @click="useUserStoreHook().SET_CURRENTPAGE(index + 1)"
                  >
                    {{ t(item.title) }}
                  </el-button>
                </div>
              </el-form-item>
            </Motion> -->
          </el-form>
          <!-- <Motion v-if="currentPage === 0" :delay="350">
            <el-form-item>
              <el-divider>
                <p class="text-gray-500 text-xs">
                  {{ t("login.pureThirdLogin") }}
                </p>
              </el-divider>
              <div class="w-full flex justify-evenly">
                <span
                  v-for="(item, index) in thirdParty"
                  :key="index"
                  :title="t(item.title)"
                >
                  <IconifyIconOnline
                    :icon="`ri:${item.icon}-fill`"
                    width="20"
                    class="cursor-pointer text-gray-500 hover:text-blue-400"
                  />
                </span>
              </div>
            </el-form-item> 
          </Motion>-->
          <!-- 手机号登录 -->
          <LoginPhone v-if="currentPage === 1" />
          <!-- 二维码登录 -->
          <LoginQrCode v-if="currentPage === 2" />
          <!-- 注册 -->
          <LoginRegist v-if="currentPage === 3" />
          <!-- 忘记密码 -->
          <LoginUpdate v-if="currentPage === 4" />
        </div>
      </div>
    </div>
    <div
      class="w-full flex-c absolute bottom-3 text-sm text-[rgba(220,220,242,0.8)] dark:text-[rgba(220,220,242,0.8)]"
    >
      版权所有 ©浙江圣博创新科技有限公司 Tel 400-8500-198
      <!-- <a
        class="hover:text-primary"
        href="https://github.com/pure-admin"
        target="_blank"
      >
        &nbsp;{{ title }}
      </a> -->
    </div>
  </div>
</template>

<style scoped>
@import url("@/style/login.css");
</style>

<style lang="scss" scoped>
:deep(.el-input-group__append, .el-input-group__prepend) {
  padding: 0;
}

.translation {
  ::v-deep(.el-dropdown-menu__item) {
    padding: 5px 40px;
  }

  .check-zh {
    position: absolute;
    left: 20px;
  }

  .check-en {
    position: absolute;
    left: 20px;
  }
}
</style>
