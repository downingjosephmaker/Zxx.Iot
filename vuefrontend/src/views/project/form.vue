<script setup lang="ts">
import { ref, onMounted } from "vue";
import { ElForm, ElMessage } from "element-plus";
import { ScadaProjectFormProps } from "./utils/types";
import { Plus } from "@element-plus/icons-vue";

defineOptions({
  name: "ScadaProjectForm"
});

const props = withDefaults(defineProps<ScadaProjectFormProps>(), {
  formInline: () => ({
    title: "",
    SnowId: "",
    ProjectName: "",
    ProjectDesc: "",
    ProjectStatus: 0,
    Thumbnail: "",
    ProjectDefault: 0,
    ExpandJson: ""
  })
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

// 状态选项
const statusOptions = [
  { label: "未发布", value: 0 },
  { label: "已发布", value: 1 }
];

// 默认状态选项
const defaultOptions = [
  { label: "未设置", value: 0 },
  { label: "默认项目", value: 1 }
];

// 表单验证规则
const rules = {
  ProjectName: [
    { required: true, message: "项目名称不能为空", trigger: "blur" },
    { min: 2, max: 50, message: "项目名称长度在 2 到 50 个字符", trigger: "blur" }
  ],
  ProjectStatus: [
    { required: true, message: "项目状态不能为空", trigger: "change" }
  ]
};

// 缩略图上传相关
const handleThumbnailSuccess = (response: any, file: any) => {
  formValue.value.Thumbnail = response.url || URL.createObjectURL(file.raw);
  ElMessage.success("缩略图上传成功");
};

const beforeThumbnailUpload = (file: any) => {
  const isImage = file.type.startsWith('image/');
  const isLt2M = file.size / 1024 / 1024 < 2;

  if (!isImage) {
    ElMessage.error("只能上传图片文件!");
    return false;
  }
  if (!isLt2M) {
    ElMessage.error("上传图片大小不能超过 2MB!");
    return false;
  }
  return true;
};

// 移除缩略图
const removeThumbnail = () => {
  formValue.value.Thumbnail = "";
};

function getRef() {
  return ruleFormRef.value;
}

defineExpose({ getRef });
</script>

<template>
  <el-form
    ref="ruleFormRef"
    :model="formValue"
    :rules="rules"
    label-width="120px"
    class="demo-ruleForm"
  >
    <el-form-item label="项目名称" prop="ProjectName">
      <el-input
        v-model="formValue.ProjectName"
        placeholder="请输入项目名称"
        clearable
      />
    </el-form-item>

    <el-form-item label="项目描述" prop="ProjectDesc">
      <el-input
        v-model="formValue.ProjectDesc"
        type="textarea"
        :rows="3"
        placeholder="请输入项目描述"
        maxlength="200"
        show-word-limit
      />
    </el-form-item>

    <el-form-item label="项目状态" prop="ProjectStatus">
      <el-select
        v-model="formValue.ProjectStatus"
        placeholder="请选择项目状态"
        class="w-full"
      >
        <el-option
          v-for="item in statusOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <el-form-item label="默认项目" prop="ProjectDefault">
      <el-select
        v-model="formValue.ProjectDefault"
        placeholder="请选择是否为默认项目"
        class="w-full"
      >
        <el-option
          v-for="item in defaultOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
    </el-form-item>

    <el-form-item label="项目缩略图" prop="Thumbnail">
      <div class="thumbnail-upload">
        <el-upload
          v-if="!formValue.Thumbnail"
          class="thumbnail-uploader"
          action="#"
          :show-file-list="false"
          :on-success="handleThumbnailSuccess"
          :before-upload="beforeThumbnailUpload"
          :auto-upload="false"
        >
          <el-icon class="thumbnail-uploader-icon"><Plus /></el-icon>
          <div class="upload-text">点击上传缩略图</div>
        </el-upload>
        <div v-else class="thumbnail-preview">
          <img :src="formValue.Thumbnail" class="thumbnail-image" />
          <div class="thumbnail-overlay">
            <el-button
              type="danger"
              size="small"
              @click="removeThumbnail"
            >
              移除
            </el-button>
          </div>
        </div>
      </div>
      <div class="upload-tip">
        只能上传jpg/png文件，且不超过2MB
      </div>
    </el-form-item>

    <el-form-item label="扩展配置" prop="ExpandJson">
      <el-input
        v-model="formValue.ExpandJson"
        type="textarea"
        :rows="4"
        placeholder="请输入JSON格式的扩展配置（可选）"
      />
    </el-form-item>
  </el-form>
</template>

<style scoped>
.thumbnail-upload {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
}

.thumbnail-uploader {
  border: 1px dashed var(--el-border-color);
  border-radius: 6px;
  cursor: pointer;
  position: relative;
  overflow: hidden;
  transition: var(--el-transition-duration-fast);
  width: 120px;
  height: 120px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.thumbnail-uploader:hover {
  border-color: var(--el-color-primary);
}

.thumbnail-uploader-icon {
  font-size: 28px;
  color: #8c939d;
  margin-bottom: 8px;
}

.upload-text {
  color: #8c939d;
  font-size: 12px;
  text-align: center;
}

.thumbnail-preview {
  position: relative;
  width: 120px;
  height: 120px;
  border-radius: 6px;
  overflow: hidden;
}

.thumbnail-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.thumbnail-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: opacity 0.3s;
}

.thumbnail-preview:hover .thumbnail-overlay {
  opacity: 1;
}

.upload-tip {
  margin-top: 8px;
  font-size: 12px;
  color: var(--el-text-color-regular);
}
</style>