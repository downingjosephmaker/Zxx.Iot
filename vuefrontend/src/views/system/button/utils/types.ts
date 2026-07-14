interface ButtonItem {
  ButtonId: number;
  ButtonCode: string;
  ButtonName: string;
  ButtonHtml?: string;
  ButtonSort?: number;
  ButtonRemark?: string;
  /** 1:页面按钮 2:表单按钮 */
  ButtonType?: number;
  UpdateTime?: string;
}

interface ButtonFormItemProps {
  title: string;
  ButtonId: number;
  ButtonCode: string;
  ButtonName: string;
  ButtonHtml: string;
  ButtonSort: number;
  ButtonRemark: string;
  ButtonType: number;
}

interface ButtonFormProps {
  formInline: ButtonFormItemProps;
}

export type { ButtonItem, ButtonFormItemProps, ButtonFormProps };
