interface RoleItem {
  RoleId: number;
  RoleName: string;
  ParentId?: number;
  RoleDescribe?: string;
  SortBorder?: string;
  TreeLevel?: number;
  FullName?: string;
  FullCode?: string;
  HasChild?: boolean;
  TenantId?: number;
  UpdateTime?: string;
}

interface RoleFormItemProps {
  title: string;
  RoleId: number;
  RoleName: string;
  ParentId: number;
  RoleDescribe: string;
  SortBorder: string;
  TreeLevel: number;
  FullName: string;
  FullCode: string;
  HasChild: boolean;
}

interface RoleFormProps {
  formInline: RoleFormItemProps;
  roleList: RoleItem[];
}

export type { RoleItem, RoleFormItemProps, RoleFormProps };
