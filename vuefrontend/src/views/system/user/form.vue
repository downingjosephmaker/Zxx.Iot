<script setup lang="ts">
import { ref, computed } from "vue";
import type { UserFormProps } from "./utils/types";

defineOptions({
  name: "SysUserForm"
});

const props = withDefaults(defineProps<UserFormProps>(), {
  formInline: () => ({
    title: "",
    UserId: 0,
    UserUid: "",
    Password: "",
    TrueName: "",
    UserXb: "",
    UserPhone: "",
    RoleId: 0,
    TenantId: 0,
    IsEnable: 1,
    UserRemark: ""
  }),
  isCreate: true,
  isSystem: false,
  tenantList: () => [],
  roleList: () => []
});

const ruleFormRef = ref();
const formValue = ref(props.formInline);

/**
 * 角色下拉按所选租户过滤:只能挑该租户自建角色或平台共享角色(TenantId=0)。
 * 与后端 InsertUser/UpdateUser 的角色归属校验同一条规则,前端先挡一道。
 */
const roleOptions = computed(() =>
  props.roleList.filter(
    r => r.TenantId === 0 || r.TenantId === formValue.value.TenantId
  )
);

/** 换租户后原角色可能已不属于新租户,清掉避免提交越权组合 */
function onTenantChange() {
  formValue.value.RoleId = 0;
}

const tenantName = computed(
  () =>
    props.tenantList.find(t => t.TenantId === formValue.value.TenantId)
      ?.TenantName ?? `租户#${formValue.value.TenantId}`
);

const rules = {
  UserUid: [
    { required: true, message: "账号不能为空", trigger: "blur" },
    { min: 3, max: 50, message: "账号长度 3~50 位", trigger: "blur" }
  ],
  Password: [
    {
      required: props.isCreate,
      message: "密码不能为空",
      trigger: "blur"
    },
    { min: 6, max: 32, message: "密码长度 6~32 位", trigger: "blur" }
  ],
  RoleId: [
    {
      required: true,
      validator: (_rule, value, callback) => {
        if (!value) callback(new Error("请选择角色"));
        else callback();
      },
      trigger: "change"
    }
  ],
  UserPhone: [
    {
      pattern: /^1[3-9]\d{9}$/,
      message: "手机号格式不正确",
      trigger: "blur"
    }
  ]
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
    label-width="90px"
  >
    <el-form-item v-if="isCreate && isSystem" label="所属租户" prop="TenantId">
      <el-select
        v-model="formValue.TenantId"
        placeholder="请选择所属租户"
        class="w-full"
        filterable
        @change="onTenantChange"
      >
        <el-option
          v-for="item in tenantList"
          :key="item.TenantId"
          :label="item.TenantName"
          :value="item.TenantId"
        />
      </el-select>
    </el-form-item>
    <el-form-item v-else-if="!isCreate" label="所属租户">
      <el-input :model-value="tenantName" disabled />
    </el-form-item>

    <el-form-item label="账号" prop="UserUid">
      <el-input
        v-model="formValue.UserUid"
        :disabled="!isCreate"
        clearable
        placeholder="请输入登录账号"
      />
    </el-form-item>

    <el-form-item v-if="isCreate" label="密码" prop="Password">
      <el-input
        v-model="formValue.Password"
        type="password"
        show-password
        clearable
        placeholder="请输入初始密码"
      />
    </el-form-item>

    <el-form-item label="角色" prop="RoleId">
      <el-select
        v-model="formValue.RoleId"
        placeholder="请选择角色"
        class="w-full"
        filterable
      >
        <el-option
          v-for="item in roleOptions"
          :key="item.RoleId"
          :label="
            item.TenantId === 0 ? `${item.RoleName}（平台共享）` : item.RoleName
          "
          :value="item.RoleId"
        />
      </el-select>
    </el-form-item>

    <el-form-item label="姓名" prop="TrueName">
      <el-input
        v-model="formValue.TrueName"
        clearable
        placeholder="请输入姓名"
      />
    </el-form-item>

    <el-form-item label="性别" prop="UserXb">
      <el-radio-group v-model="formValue.UserXb">
        <el-radio value="男">男</el-radio>
        <el-radio value="女">女</el-radio>
      </el-radio-group>
    </el-form-item>

    <el-form-item label="手机号" prop="UserPhone">
      <el-input
        v-model="formValue.UserPhone"
        clearable
        placeholder="请输入手机号"
      />
    </el-form-item>

    <el-form-item label="备注" prop="UserRemark">
      <el-input
        v-model="formValue.UserRemark"
        type="textarea"
        :rows="2"
        placeholder="请输入备注"
      />
    </el-form-item>
  </el-form>
</template>
