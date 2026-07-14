interface MenuItem {
  MenuId: string;
  MenuCode: string;
  MenuName: string;
  ParentId: string;
  MenuUrl?: string;
  MenuIcon?: string;
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
  MenuIcon: string;
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
