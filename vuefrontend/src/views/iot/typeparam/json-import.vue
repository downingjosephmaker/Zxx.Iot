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
import { paramAddByType } from "@/api/iot/typeparam";
import UploadFilled from "~icons/ep/upload-filled";

defineOptions({
  name: "TypeParamJsonImport"
});

const props = defineProps<{
  /** 预填的产品类型编码(取自搜索栏) */
  typecode?: string;
}>();

const typecode = ref(props.typecode ?? "");
const uploadRef = ref<UploadInstance>();
const selectedFile = ref<File | null>(null);
const importing = ref(false);

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

/** 执行导入，成功返回true供外层关闭弹窗并刷新列表 */
async function onImport(): Promise<boolean> {
  if (typecode.value.trim() === "") {
    message("请填写产品类型编码", { type: "warning" });
    return false;
  }
  if (!selectedFile.value) {
    message("请先选择要导入的JSON点表文件", { type: "warning" });
    return false;
  }
  importing.value = true;
  try {
    const data = await paramAddByType(selectedFile.value, typecode.value.trim());
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
    <el-alert type="warning" :closable="false" show-icon class="mb-3">
      <template #title>
        支持组态点表JSON(含metadata节点)；该产品类型已有点表时将拒绝导入，需先删除原有参数
      </template>
    </el-alert>
    <el-form label-width="110px">
      <el-form-item label="产品类型编码" required>
        <el-input
          v-model="typecode"
          placeholder="点表将导入到该产品类型下"
          clearable
        />
      </el-form-item>
      <el-form-item label="点表JSON文件" required>
        <el-upload
          ref="uploadRef"
          drag
          class="w-full"
          :auto-upload="false"
          :limit="1"
          accept=".json"
          :on-change="onFileChange"
          :on-remove="onFileRemove"
          :on-exceed="onExceed"
        >
          <el-icon class="el-icon--upload"><UploadFilled /></el-icon>
          <div class="el-upload__text">
            将 .json 文件拖到此处，或<em>点击选择</em>
          </div>
        </el-upload>
      </el-form-item>
    </el-form>
  </div>
</template>
