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
import { encryptRemember, decryptRemember } from "@/utils/secureRemember";
import { getSignalRUrl } from "@/config";
import md5 from "md5";
import JSEncrypt from "jsencrypt";
import { useUserStoreHook } from "@/store/modules/user";
import { initRouter, getTopMenu } from "@/router/utils";
import { bg, avatar, illustration } from "./utils/static";
import { ReImageVerify } from "@/components/ReImageVerify";
import {
  ref,
  toRaw,
  reactive,
  watch,
  computed,
  onMounted,
  onBeforeUnmount,
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

// 内置物联网主题 Logo（发光节点连线徽标，无外链）
const iotLogo = `
<svg viewBox="0 0 64 64" fill="none" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="lgLogo" x1="0" y1="0" x2="64" y2="64">
      <stop offset="0" stop-color="#22d3ee"/><stop offset="1" stop-color="#3b82f6"/>
    </linearGradient>
  </defs>
  <g stroke="url(#lgLogo)" stroke-width="2.4" fill="#0a1330" stroke-linecap="round">
    <path d="M32 20 32 26 M45 40 38 35 M19 40 26 35" fill="none"/>
    <circle cx="32" cy="14" r="4.6"/><circle cx="50" cy="45" r="4.6"/><circle cx="14" cy="45" r="4.6"/>
  </g>
  <circle cx="32" cy="32" r="7.5" fill="url(#lgLogo)"/>
</svg>`;

// 内置物联网拓扑插画（云-网关-中枢-传感器节点+数据流动画）
const iotIllustration = `
<svg viewBox="0 0 600 480" fill="none" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <radialGradient id="hubGlow" cx="50%" cy="50%" r="50%">
      <stop offset="0" stop-color="#22d3ee" stop-opacity="0.85"/>
      <stop offset="1" stop-color="#22d3ee" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="nodeF" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#38bdf8"/><stop offset="1" stop-color="#3b82f6"/>
    </linearGradient>
  </defs>

  <!-- 连线（底层静态 + 上层数据流动画） -->
  <g stroke="#2b6cb0" stroke-width="1.6" opacity="0.55">
    <line x1="300" y1="240" x2="300" y2="96"/>
    <line x1="300" y1="240" x2="120" y2="150"/>
    <line x1="300" y1="240" x2="480" y2="150"/>
    <line x1="300" y1="240" x2="120" y2="360"/>
    <line x1="300" y1="240" x2="480" y2="360"/>
    <line x1="300" y1="240" x2="300" y2="412"/>
  </g>
  <g stroke="#7dd3fc" stroke-width="2.2" stroke-linecap="round" stroke-dasharray="2 15">
    <line x1="300" y1="240" x2="300" y2="96"><animate attributeName="stroke-dashoffset" from="0" to="-68" dur="1.5s" repeatCount="indefinite"/></line>
    <line x1="300" y1="240" x2="120" y2="150"><animate attributeName="stroke-dashoffset" from="0" to="-68" dur="1.8s" begin="0.2s" repeatCount="indefinite"/></line>
    <line x1="300" y1="240" x2="480" y2="150"><animate attributeName="stroke-dashoffset" from="0" to="-68" dur="1.7s" begin="0.4s" repeatCount="indefinite"/></line>
    <line x1="300" y1="240" x2="120" y2="360"><animate attributeName="stroke-dashoffset" from="0" to="-68" dur="1.9s" begin="0.1s" repeatCount="indefinite"/></line>
    <line x1="300" y1="240" x2="480" y2="360"><animate attributeName="stroke-dashoffset" from="0" to="-68" dur="1.6s" begin="0.5s" repeatCount="indefinite"/></line>
    <line x1="300" y1="240" x2="300" y2="412"><animate attributeName="stroke-dashoffset" from="0" to="-68" dur="2s" begin="0.3s" repeatCount="indefinite"/></line>
  </g>

  <!-- 云端 -->
  <g transform="translate(300,60)" stroke="#7dd3fc" stroke-width="2" fill="#0e2148">
    <path d="M-26 8 a15 15 0 0 1 4 -29 a20 20 0 0 1 38 6 a13 13 0 0 1 -3 26 Z"/>
    <path d="M-8 -2 l0 8 M0 -6 l0 12 M8 -2 l0 8" stroke="#bfe9ff" stroke-linecap="round"/>
  </g>

  <!-- 中枢 -->
  <circle cx="300" cy="240" r="66" fill="url(#hubGlow)"/>
  <circle cx="300" cy="240" r="30" stroke="#38bdf8" stroke-width="1.6" opacity="0.5">
    <animate attributeName="r" values="30;54;30" dur="3s" repeatCount="indefinite"/>
    <animate attributeName="opacity" values="0.5;0;0.5" dur="3s" repeatCount="indefinite"/>
  </circle>
  <circle cx="300" cy="240" r="26" fill="#0e1f45" stroke="url(#nodeF)" stroke-width="2.4"/>
  <g transform="translate(300,240)" stroke="#bfe9ff" stroke-width="1.8" fill="none" stroke-linecap="round">
    <rect x="-8" y="-8" width="16" height="16" rx="3.5" fill="#0a1734"/>
    <path d="M-8 -3 h-4 M-8 3 h-4 M8 -3 h4 M8 3 h4 M-3 -8 v-4 M3 -8 v-4 M-3 8 v4 M3 8 v4"/>
  </g>

  <!-- 周边设备节点 -->
  <g stroke="#7dd3fc" stroke-width="2" fill="#0e2148">
    <!-- 温度传感器 -->
    <g transform="translate(120,150)"><circle r="22"/><g stroke="#bfe9ff" fill="none" stroke-linecap="round"><rect x="-2.6" y="-10" width="5.2" height="12" rx="2.6"/><circle cx="0" cy="4" r="4.4" fill="#bfe9ff" stroke="none"/></g></g>
    <!-- 网关 -->
    <g transform="translate(480,150)"><circle r="22"/><g stroke="#bfe9ff" fill="none" stroke-linecap="round"><path d="M-9 -3 a12 12 0 0 1 18 0"/><path d="M-5 2 a7 7 0 0 1 10 0"/></g><circle cx="480" cy="0" r="0"/><circle cx="0" cy="7" r="1.8" fill="#bfe9ff" stroke="none"/></g>
    <!-- 电表 -->
    <g transform="translate(120,360)"><circle r="22"/><g stroke="#bfe9ff" fill="none" stroke-linecap="round"><path d="M-8 5 a9 9 0 0 1 16 0"/><line x1="0" y1="5" x2="5" y2="-3"/></g></g>
    <!-- 水表/阀门 -->
    <g transform="translate(480,360)"><circle r="22"/><path d="M0 -10 C6 -1 7 4 0 8 C-7 4 -6 -1 0 -10 Z" stroke="#bfe9ff" fill="none"/></g>
    <!-- 设备/服务器 -->
    <g transform="translate(300,412)"><circle r="22"/><g stroke="#bfe9ff" fill="none" stroke-linecap="round"><rect x="-8" y="-8" width="16" height="6.5" rx="1.6"/><rect x="-8" y="1.5" width="16" height="6.5" rx="1.6"/><circle cx="4.5" cy="-4.7" r="1" fill="#bfe9ff" stroke="none"/><circle cx="4.5" cy="4.8" r="1" fill="#bfe9ff" stroke="none"/></g></g>
  </g>
</svg>`;

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

// 背景粒子网络（物联网节点漂移+近邻连线，纯 canvas 无外链）
const bgCanvas = ref<HTMLCanvasElement | null>(null);
let rafId = 0;
let cleanupCanvas: (() => void) | null = null;
const setupParticleNetwork = () => {
  const canvas = bgCanvas.value;
  if (!canvas) return;
  const ctx = canvas.getContext("2d");
  if (!ctx) return;
  let w = 0;
  let h = 0;
  const nodes: { x: number; y: number; vx: number; vy: number; r: number }[] =
    [];
  const resize = () => {
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    w = canvas.clientWidth;
    h = canvas.clientHeight;
    canvas.width = w * dpr;
    canvas.height = h * dpr;
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  };
  resize();
  const count = Math.min(72, Math.round((w * h) / 26000));
  for (let i = 0; i < count; i++) {
    nodes.push({
      x: Math.random() * w,
      y: Math.random() * h,
      vx: (Math.random() - 0.5) * 0.35,
      vy: (Math.random() - 0.5) * 0.35,
      r: Math.random() * 1.6 + 0.6
    });
  }
  const draw = () => {
    ctx.clearRect(0, 0, w, h);
    for (const n of nodes) {
      n.x += n.vx;
      n.y += n.vy;
      if (n.x < 0 || n.x > w) n.vx *= -1;
      if (n.y < 0 || n.y > h) n.vy *= -1;
    }
    for (let i = 0; i < nodes.length; i++) {
      for (let j = i + 1; j < nodes.length; j++) {
        const a = nodes[i];
        const b = nodes[j];
        const dist = Math.hypot(a.x - b.x, a.y - b.y);
        if (dist < 138) {
          ctx.strokeStyle = `rgba(56,189,248,${(1 - dist / 138) * 0.5})`;
          ctx.lineWidth = 0.6;
          ctx.beginPath();
          ctx.moveTo(a.x, a.y);
          ctx.lineTo(b.x, b.y);
          ctx.stroke();
        }
      }
    }
    ctx.shadowColor = "rgba(34,211,238,0.9)";
    ctx.shadowBlur = 8;
    ctx.fillStyle = "rgba(125,211,252,0.9)";
    for (const n of nodes) {
      ctx.beginPath();
      ctx.arc(n.x, n.y, n.r, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.shadowBlur = 0;
    rafId = requestAnimationFrame(draw);
  };
  draw();
  window.addEventListener("resize", resize);
  cleanupCanvas = () => {
    cancelAnimationFrame(rafId);
    window.removeEventListener("resize", resize);
  };
};

onMounted(async () => {
  setupParticleNetwork();
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
    ruleForm.password = await decryptRemember(storage.getItem("password") || "");
  }
  connection.value = await startConnection();
  if (ruleForm.username) {
    ruleForm.isRemember = true;
  }
});

onBeforeUnmount(() => {
  cleanupCanvas?.();
  window.document.removeEventListener("keypress", onkeypress);
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
              // 密码加密后再落盘，杜绝 localStorage 明文
              encryptRemember(ruleForm.password).then(enc =>
                storage.setItem("password", enc)
              );
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
  <div class="iot-login select-none">
    <canvas ref="bgCanvas" class="iot-canvas" />
    <div class="iot-vignette" />
    <div class="iot-topbar">
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
    <div class="iot-stage">
      <!-- 左侧：物联网拓扑插画 + 品牌语 -->
      <section class="iot-hero">
        <div class="iot-hero-illus" v-html="iotIllustration" />
        <div class="iot-hero-copy">
          <p class="iot-kicker">Smart IoT Platform</p>
          <h1 class="iot-hero-title">智慧物联网中台</h1>
          <p class="iot-hero-sub">设备接入 · 实时遥测 · 智能告警 · 组态大屏</p>
          <div class="iot-stats">
            <span class="iot-chip"><i />多协议采集</span>
            <span class="iot-chip"><i />边缘联动</span>
            <span class="iot-chip"><i />可视组态</span>
          </div>
        </div>
      </section>

      <!-- 右侧：玻璃拟态登录卡 -->
      <section class="iot-panel">
        <div class="iot-card">
          <div class="iot-brand">
            <span class="iot-logo" v-html="iotLogo" />
          <Motion>
            <h2 class="outline-hidden">
              <TypeIt
                :options="{ strings: [title], cursor: false, speed: 100 }"
              />
            </h2>
          </Motion>
          </div>
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
                  class="iot-login-btn w-full mt-4!"
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
      </section>
    </div>
    <footer class="iot-footer">
      版权所有 ©浙江圣博创新科技有限公司&nbsp;&nbsp;Tel 400-8500-198
    </footer>
  </div>
</template>

<style scoped>
@import url("@/style/login.css");
</style>

<style lang="scss" scoped>
/* Element Plus 内部元素（非组件根，拿不到 scoped data-v）须用 :deep 才能命中 —— 登录卡深色适配 */
.iot-login {
  :deep(.el-form-item) {
    margin-bottom: 22px;
  }

  :deep(.el-input__wrapper) {
    padding: 4px 14px;
    background: rgba(255, 255, 255, 0.04);
    border-radius: 12px;
    box-shadow: 0 0 0 1px rgba(120, 160, 220, 0.22) inset;
    transition:
      box-shadow 0.25s ease,
      background 0.25s ease;

    &:hover {
      box-shadow: 0 0 0 1px rgba(56, 189, 248, 0.45) inset;
    }

    &.is-focus {
      background: rgba(56, 189, 248, 0.06);
      box-shadow:
        0 0 0 1px rgba(56, 189, 248, 0.7) inset,
        0 0 18px -2px rgba(34, 211, 238, 0.5);
    }
  }

  :deep(.el-input__inner) {
    height: 44px;
    color: #eaf5ff;
    caret-color: #38bdf8;

    &::placeholder {
      color: #6f83a8;
    }
  }

  :deep(.el-input__prefix),
  :deep(.el-input__suffix) {
    color: #6f9ad0;

    .el-icon {
      color: #6f9ad0;
    }
  }

  :deep(.el-checkbox__label) {
    color: #a7bde0;
  }

  :deep(.el-checkbox__inner) {
    background: rgba(255, 255, 255, 0.05);
    border-color: rgba(120, 160, 220, 0.5);
  }

  :deep(.el-checkbox__input.is-checked .el-checkbox__inner) {
    background: #22d3ee;
    border-color: #22d3ee;
  }
}

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
