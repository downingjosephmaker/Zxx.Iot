interface UserItem {
  UserId: number;
  UserUid: string;
  TrueName?: string;
  UserXb?: string;
  UserPhone?: string;
  RoleId: number;
  RoleName?: string;
  TenantId: number;
  IsEnable: number;
  LoginCount?: number;
  LastLoginTime?: string;
  UserRemark?: string;
}

interface RoleOption {
  RoleId: number;
  RoleName: string;
  TenantId: number;
}

interface TenantOption {
  TenantId: number;
  TenantName: string;
}

interface UserFormItemProps {
  title: string;
  UserId: number;
  UserUid: string;
  Password: string;
  TrueName: string;
  UserXb: string;
  UserPhone: string;
  RoleId: number;
  TenantId: number;
  IsEnable: number;
  UserRemark: string;
}

interface UserFormProps {
  formInline: UserFormItemProps;
  /** 是否新增(新增才填密码、才可选租户) */
  isCreate: boolean;
  /** 当前登录人是否平台超管(仅超管可为用户指定租户) */
  isSystem: boolean;
  tenantList: TenantOption[];
  roleList: RoleOption[];
}

export type {
  UserItem,
  RoleOption,
  TenantOption,
  UserFormItemProps,
  UserFormProps
};
