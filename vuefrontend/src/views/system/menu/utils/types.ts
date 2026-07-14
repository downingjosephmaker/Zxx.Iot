interface MenuItem {
  MenuId: string;
  MenuCode: string;
  MenuName: string;
  ParentId: string;
  MenuUrl?: string;
  /** 组件路径(相对 src/views)，目录节点留空 */
  Component?: string;
  MenuIcon?: string;
  /** 附加路由 meta(JSON 字符串)，如 {"projectKind":"scada"} */
  MetaJson?: string;
  IsShowLink?: number;
  SortBorder?: string;
  TreeLevel?: number;
  FullName?: string;
  FullCode?: string;
  HasChild?: boolean;
  children?: MenuItem[];
}

interface MenuFormItemProps {
  title: string;
  MenuId: string;
  MenuCode: string;
  MenuName: string;
  ParentId: string;
  MenuUrl: string;
  Component: string;
  MenuIcon: string;
  MetaJson: string;
  IsShowLink: number;
  SortBorder: string;
  TreeLevel: number;
  FullName: string;
  FullCode: string;
  HasChild: boolean;
}

interface MenuFormProps {
  formInline: MenuFormItemProps;
  menuTree: MenuItem[];
}

export type { MenuItem, MenuFormItemProps, MenuFormProps };
