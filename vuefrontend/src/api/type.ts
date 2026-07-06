export type Result = {
  Status: boolean;
  Result?: any;
  Message?: any;
  Timestamp: string;
  Total: number;
};

export type Result2 = [];
export type uploadType = {
  file: FormData;
};

export type ResultLC = {
  code: number;
  rows?: any;
  msg?: string;
  total?: number;
  data?: any;
};

export type ResultDB = {
  code: number;
  data?: any;
  message?: string;
  path?: string;
};

export type ResultTable = {
  Status?: boolean;
  /** 列表数据 */
  Result?: any;
  /** 总条目数 */
  Total?: number;
  Message?: string;
};

/**
 * 报表响应类型
 */
export type ReportResponse = {
  Status: boolean;
  Result: string; // 可能是JSON字符串，需要解析
  Message: string;
  Timestamp: string;
  Total: number;
};

export type Sconlist = {
  ParamName?: string; // 参数表字段
  ParamType?: string; // 查询规则(=|!=|>|>=|<|<=|in|notin|like|isnull|sort)
  ParamValue?: any; // 参数值
  ParamSort?: number; // 排序(0:不处理 1:正序 2:倒序)
  ParamGroupName?: string; // 参数分组表字段
  GroupCondition?: string; // 分组内部条件(and,or,null)
  IsGroupFrist?: boolean; // 是否为起始字段
  ApplyState?: any;
};

export type QueryTableParams = {
  DeviceId?: string;
  PlayDeptCode?: string; // 部门编码
  IsEnable?: boolean; // 是否启用
  starttime?: string; // 开始时间 "2024-02-23 14:47:55"
  endtime?: string; // 结束时间 "2024-02-28 14:47:55"
  page?: number; // 页码 1
  pageNum?: number;
  pagesize?: number; // 页数 20
  DeviceIds?: any;
  DevId?: any;
  buildid?: any;
  typecode?: any;
  ParamTypeName?: any;
  ParamCount?: any;
  sconlist?: Array<Sconlist>;
  Status?: any;
  typeId?: any;
  ApplyState?: any;
};

export type QueryTableParams2 = {
  starttime?: string; // 开始时间 "2024-02-23 14:47:55"
  endtime?: string; // 结束时间 "2024-02-28 14:47:55"
  DeviceIds?: any;
  DevId?: any;
  buildid?: any;
  typeid?: any;
  ParamTypeName?: any;
  ParamCount?: any;
  sconlist?: Array<Sconlist>;
  pageNum: number;
  pageSize: number;
};

export interface DeviceMonitorSearch {
  /** 建筑ID */
  BuildId: number;
  /** 部门ID */
  DeptId: number;
  /** 设备名称，可为空 */
  DevName?: string;
  /** 设备类型编码 */
  DevMasterTypeCode: string;
  /** 设备类型集合，可为空，默认1 */
  DevTypeCodeList?: number[];
  /** 在线状态(0:离线 1:掉电 2:在线 3:异常)，默认-1 */
  DevLine?: number;
  /** 开关状态(1:关闭 2:开启 3:强关 4:强开)，默认-1 */
  DevSwitch?: number;
  /** 当前页 */
  page: number;
  /** 每页记录数 */
  pagesize: number;
}
