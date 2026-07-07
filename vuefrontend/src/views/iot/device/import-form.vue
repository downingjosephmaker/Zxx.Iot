<script setup lang="ts">
import { ref } from "vue";
import { genFileId } from "element-plus";
import type {
  UploadFile,
  UploadInstance,
  UploadProps,
  UploadRawFile
} from "element-plus";
import { message } from "@/utils/message";
import { getApiUrl } from "@/config";
import { deviceImport, downloadDeviceTemplate } from "@/api/iot/device";
import UploadFilled from "~icons/ep/upload-filled";
import Download from "~icons/ep/download";

defineOptions({
  name: "DeviceImportForm"
});

const uploadRef = ref<UploadInstance>();
const selectedFile = ref<File | null>(null);
const importing = ref(false);
const downloading = ref(false);

function onFileChange(uploadFile: UploadFile) {
  selectedFile.value = (uploadFile.raw as File) ?? null;
}

function onFileRemove() {
  selectedFile.value = null;
}

/** 超出limit=1时用新文件替换旧文件 */
const onExceed: UploadProps["onExceed"] = files => {
  uploadRef.value!.clearFiles();
  const file = files[0] as UploadRawFile;
  file.uid = genFileId();
  uploadRef.value!.handleStart(file);
};

/** 下载导入模板（服务端刷新"设备类型"页签后返回静态相对路径） */
async function onDownloadTemplate() {
  downloading.value = true;
  try {
    const data = await downloadDeviceTemplate();
    if (data.Status && data.Result) {
      // 模板由静态文件中间件对外服务，基地址=API地址去掉末尾/Api
      const base = getApiUrl().replace(/\/Api\/?$/i, "");
      const path = String(data.Result).replace(/\\/g, "/").replace(/^\//, "");
      window.open(`${base}/${path}`, "_blank");
    } else {
      message(data.Message || "模板不存在", { type: "error" });
    }
  } finally {
    downloading.value = false;
  }
}

/** 执行导入，成功返回true供外层关闭弹窗并刷新列表 */
async function onImport(): Promise<boolean> {
  if (!selectedFile.value) {
    message("请先选择要导入的Excel文件", { type: "warning" });
    return false;
  }
  importing.value = true;
  try {
    const data = await deviceImport(selectedFile.value);
    if (data.Status) {
      message(data.Message || "导入成功", { type: "success" });
      return true;
    }
    message(data.Message || "导入失败", { type: "error" });
    return false;
  } finally {
    importing.value = false;
  }
}

defineExpose({ onImport, importing });
</script>

<template>
  <div>
    <el-alert type="info" :closable="false" show-icon class="mb-3">
      <template #title>
        请使用导入模板填写设备信息，"设备类型(请勿更改)"页签由系统生成
      </template>
    </el-alert>
    <el-upload
      ref="uploadRef"
      drag
      :auto-upload="false"
      :limit="1"
      accept=".xlsx"
      :on-change="onFileChange"
      :on-remove="onFileRemove"
      :on-exceed="onExceed"
    >
      <el-icon class="el-icon--upload"><UploadFilled /></el-icon>
      <div class="el-upload__text">将 .xlsx 文件拖到此处，或<em>点击选择</em></div>
      <template #tip>
        <div class="el-upload__tip">仅支持.xlsx格式，单次不超过100000行</div>
      </template>
    </el-upload>
    <div class="mt-2">
      <el-button
        type="primary"
        link
        :loading="downloading"
        @click="onDownloadTemplate"
      >
        <el-icon class="mr-1"><Download /></el-icon>
        下载导入模板
      </el-button>
    </div>
  </div>
</template>
