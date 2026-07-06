using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 空调信息
    ///</summary>
    [DisplayName("空调信息")]
    public class NetAirConInfo
    {
        /// <summary>
        /// 设备ID
        ///</summary>
        [DisplayName("设备ID")]
        public int DeviceId { get; set; }
    }

    /// <summary>
    /// 空调控制运行状态
    ///</summary>
    [DisplayName("空调控制运行状态")]
    public class NetAirConRun
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();
        /// <summary>
        /// 控制模式(0:自动关 1:自动开 2:强制关 3:强制开 4:复位 255:无动作)
        ///</summary>
        [DisplayName("控制模式(0:自动关 1:自动开 2:强制关 3:强制开 4:复位 255:无动作)")]
        [DefaultValue("255")]
        public int AirSwitch { get; set; } = 255;
        /// <summary>
        /// 空调模式(0:自动 1:制冷 2:除湿 3:送风 4:制热 255:无动作)
        ///</summary>
        [DisplayName("空调模式(0:自动 1:制冷 2:除湿 3:送风 4:制热 255:无动作)")]
        [DefaultValue("255")]
        public int AirModel { get; set; } = 255;
        /// <summary>
        /// 模式设定温度 16-30
        ///</summary>
        [DisplayName("模式设定温度 16-30")]
        [DefaultValue("255")]
        public int AirModelTemp { get; set; } = 255;
        /// <summary>
        /// 空调风速(0:自动1:1档2:2档3:3档255:无动作)
        ///</summary>
        [DisplayName("空调风速(0:自动 1:1档 2:2档 3:3档 255:无动作)")]
        [DefaultValue("255")]
        public int AirSpeed { get; set; } = 255;
        /// <summary>
        /// 空调风向(0:自动摆风1:手动摆风255:无动作)
        ///</summary>
        [DisplayName("空调风向(0:自动摆风 1:手动摆风 255:无动作)")]
        [DefaultValue("255")]
        public int AirDirection { get; set; } = 255;
        /// <summary>
        /// 面板锁定(0:解锁 1:锁定255:无动作)
        ///</summary>
        [DisplayName("面板锁定(0:解锁 1:锁定 255:无动作)")]
        [DefaultValue("255")]
        public int AirKeyLock { get; set; } = 255;
    }

    /// <summary>
    /// 系统参数
    ///</summary>
    [DisplayName("系统参数")]
    public class NetAirParam
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();
        /// <summary>
        /// 品牌代码(65535:无动作)
        ///</summary>
        [DisplayName("品牌代码(65535:无动作)")]
        [DefaultValue("65535")]
        public string BrandCode { get; set; }
        /// <summary>
        /// 电流阈值(65535:无动作)
        ///</summary>
        [DisplayName("电流阈值(65535:无动作)")]
        [DefaultValue("65535")]
        public int ElecRange { get; set; }
        /// <summary>
        /// 插座检测功率阈值(65535:无动作)
        ///</summary>
        [DisplayName("插座检测功率阈值(65535:无动作)")]
        [DefaultValue("65535")]
        public int CzCheckPower { get; set; }
        /// <summary>
        /// 温度选择(1.内部温度 2.外部温度 255:无动作)
        ///</summary>
        [DisplayName("温度选择(1.内部温度 2.外部温度 255:无动作)")]
        [DefaultValue("255")]
        public int TempType { get; set; }
        /// <summary>
        /// 心跳周期(分)
        ///</summary>
        [DisplayName("心跳周期(分)")]
        public int HeartRange { get; set; }
        /// <summary>
        /// 电能清零(1:清零 255:无动作)
        ///</summary>
        [DisplayName("电能清零(1:清零 255:无动作)")]
        [DefaultValue("255")]
        public int EnergyClear { get; set; }
        /// <summary>
        /// 关机限流最大值(*10A)
        ///</summary>
        [DisplayName("关机限流最大值(*10A)")]
        [DefaultValue("65535")]
        public int XlMaxElec { get; set; }
    }

    /// <summary>
    /// 用户参数
    ///</summary>
    [DisplayName("用户参数")]
    public class NetAirUser
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();
        /// <summary>
        /// 休眠模式(0:不开启 1:开启 255:无动作)
        ///</summary>
        [DisplayName("休眠模式(0:不开启 1:开启 255:无动作)")]
        [DefaultValue("255")]
        public int SleepMode { get; set; } = 255;
        /// <summary>
        /// 预存电量(0-0xfffffffe,0xffffffff:无动作)
        ///</summary>
        [DisplayName("预存电量(0-65534,0xffffffff:无动作)")]
        [DefaultValue(0xffffffff)]
        public uint StoredEnergy { get; set; } = 0xffffffff;
        ///// <summary>
        ///// 预存电量(0-65534,65535:无动作)
        /////</summary>
        //[DisplayName("预存电量(0-65534,65535:无动作)")]
        //[DefaultValue("65535")]
        //public int StoredEnergy { get; set; }
        /// <summary>
        /// 季节选择(0:夏季 1:冬季65535:无动作)
        ///</summary>
        [DisplayName("季节选择(0:夏季 1:冬季 255:无动作)")]
        [DefaultValue("255")]
        public int AirSeason { get; set; } = 255;
        /// <summary>
        /// 开机使能(0:不允许开机 1:按策略执行 2:不执行策略 255:无动作)
        ///</summary>
        [DisplayName("开机使能(0:不允许开机 1:按策略执行 2:不执行策略 255:无动作)")]
        [DefaultValue("255")]
        public int EnableOpen { get; set; } = 255;
        /// <summary>
        /// 插座监控(0:不启用 1:启用 255:无动作)
        ///</summary>
        [DisplayName("插座监控(0:不启用 1:启用255:无动作)")]
        [DefaultValue("255")]
        public int AirSocket { get; set; } = 255;
    }

    /// <summary>
    /// 空调策略(常规)
    ///</summary>
    [DisplayName("空调策略(常规)")]
    public class AirGeneral
    {
        /// <summary>
        /// 工作模式(0:调温1:人感2:温度3:时间4:手动5:计量7:断电8:机房省电9:临时) 多模式逗号隔开
        ///</summary>
        [DisplayName("工作模式(0:调温1:人感2:温度3:时间4:手动5:计量7:断电8:机房省电9:临时) 多模式逗号隔开")]
        public string WorkModel { get; set; }
        /// <summary>
        /// 制冷开启温度
        ///</summary>
        [DisplayName("制冷开启温度")]
        public int RefrigStartTemp { get; set; }
        /// <summary>
        /// 制冷开机温度
        ///</summary>
        [DisplayName("制冷开机温度")]
        public int RefrigOpenTemp { get; set; }
        /// <summary>
        /// 制冷关机温度
        ///</summary>
        [DisplayName("制冷关机温度")]
        public int RefrigCloseTemp { get; set; }
        /// <summary>
        /// 制热开启温度
        ///</summary>
        [DisplayName("制热开启温度")]
        public int HotStartTemp { get; set; }
        /// <summary>
        /// 制热开机温度
        ///</summary>
        [DisplayName("制热开机温度")]
        public int HotOpenTemp { get; set; }
        /// <summary>
        /// 制热关机温度
        ///</summary>
        [DisplayName("制热关机温度")]
        public int HotCloseTemp { get; set; }
        /// <summary>
        /// 人感人数
        ///</summary>
        [DisplayName("人感人数")]
        [DefaultValue("255")]
        public int HumanNum { get; set; }
        /// <summary>
        /// 人感延时
        ///</summary>
        [DisplayName("人感延时")]
        [DefaultValue("65535")]
        public int HumanTime { get; set; }
        /// <summary>
        /// 临时设定时间
        ///</summary>
        [DisplayName("临时设定时间")]
        public int TemporaryTime { get; set; }
        /// <summary>
        /// 夏季判断温度 20~35
        ///</summary>
        [DisplayName("夏季判断温度 20~35")]
        public int SummerTemp { get; set; }
        /// <summary>
        /// 冬季判断温度 0~20
        ///</summary>
        [DisplayName("冬季判断温度 0~20")]
        public int WinterTemp { get; set; }
        /// <summary>
        /// 温度模式开启温度使能 0x00,关闭；0x01，开启，控制器默认值为0x01
        ///</summary>
        [DisplayName("温度模式开启温度使能 0x00,关闭；0x01，开启，控制器默认值为0x01")]
        [DefaultValue("255")]
        public int OpenTempEnable { get; set; }
        /// <summary>
        /// 温度使能运行方式 0x01,按天；0x02，按时段
        ///</summary>
        [DisplayName("温度使能运行方式 0x01,按天；0x02，按时段")]
        [DefaultValue("255")]
        public int OperatModeTemp { get; set; }
        /// <summary>
        /// 温度模式开关机控制:0x01,制冷/制热开机控制；0x02，制冷/制热关机控制；0x03，制冷/制热开关机控制
        ///</summary>
        [DisplayName("温度模式开关机控制:0x01,制冷/制热开机控制；0x02，制冷/制热关机控制；0x03，制冷/制热开关机控制")]
        [DefaultValue("255")]
        public int OpenCloseTemp { get; set; }
        /// <summary>
        /// 夏季开机温度值(16~31)
        ///</summary>
        [DisplayName("夏季开机温度值(16~31)")]
        public int SummerOpenTemp { get; set; }
        /// <summary>
        /// 冬季开机温度值(16~31)
        ///</summary>
        [DisplayName("冬季开机温度值(16~31)")]
        public int WinterOpenTemp { get; set; }
        /// <summary>
        /// 调温模式判断依据(0：环境温度，1：设置温度)
        ///</summary>
        [DisplayName("调温模式判断依据(0：环境温度，1：设置温度)")]
        [DefaultValue("0")]
        public int TempModeJudge { get; set; }
    }

    /// <summary>
    /// 时间策略
    /// </summary>
    public class NetAirTimeInfo
    {
        /// <summary>
        /// 日期类型(1、2、3、4、5、6、7)
        ///</summary>
        [DisplayName("日期类型(1、2、3、4、5、6、7)")]
        public int DayType { get; set; }
        /// <summary>
        /// 时段序号(1-4)
        ///</summary>
        [DisplayName("时段序号(1-4)")]
        public int TimeNum { get; set; }
        /// <summary>
        /// 人感(0:不启用 1:启用)
        ///</summary>
        [DisplayName("人感(0:不启用 1:启用)")]
        [DefaultValue("255")]
        public int IsHuman { get; set; }
        /// <summary>
        /// 开始时
        ///</summary>
        [DisplayName("开始时")]
        public int StartHour { get; set; }
        /// <summary>
        /// 开始分
        ///</summary>
        [DisplayName("开始分")]
        public int StartMinute { get; set; }
        /// <summary>
        /// 结束时
        ///</summary>
        [DisplayName("结束时")]
        public int EndHour { get; set; }
        /// <summary>
        /// 结束分
        ///</summary>
        [DisplayName("结束分")]
        public int EndMinute { get; set; }
    }

    /// <summary>
    /// 空调策略(常规+时间)
    ///</summary>
    [DisplayName("空调策略(常规+时间)")]
    public class NetAirStrategy : AirGeneral
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();

        /// <summary>
        /// 时间策略集合
        ///</summary>
        [DisplayName("时间策略集合")]
        public List<NetAirTimeInfo> TimeInfoList { get; set; } = new List<NetAirTimeInfo>();
    }


    /// <summary>
    /// 其他通用控制
    ///</summary>
    [DisplayName("其他通用控制")]
    public class NetAirCommonSet
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();

        /// <summary>
        /// 服务ID
        /// 读取常规参数(8003)
        /// 读取时间参数(8004)
        /// 读取系统参数(8005)
        /// 读取用户参数(8006)
        /// 读取设备状态(8008)
        /// 远程重启(8007)
        /// VRFV4读取控制器状态(8010)
        /// VRFV4读取空调内机状态(8011)
        /// VRFV4读取全部策略参数(8017)
        ///</summary>
        [DisplayName("服务ID：读取常规参数(8003)读取时间参数(8004)读取系统参数(8005)读取用户参数(8006)读取设备状态(8008)远程重启(8007)VRFV4读取控制器状态(8010)VRFV4读取空调内机状态(8011)VRFV4读取全部策略参数(8017)")]
        public int DatasetId { get; set; }
    }

    /// <summary>
    /// 网关锁定
    ///</summary>
    [DisplayName("网关锁定")]
    public class NetAirConLock
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();
        /// <summary>
        /// 控制模式(0:关闭1:开启 255:无动作)
        ///</summary>
        [DisplayName("控制模式(0:关闭1:开启 255:无动作)")]
        [DefaultValue("255")]
        public int AirSwitch { get; set; } = 255;
        /// <summary>
        /// 空调模式(0:自动1:制冷2:除湿3:送风4:制热255:无动作)
        ///</summary>
        [DisplayName("空调模式(0:自动1:制冷2:除湿3:送风4:制热255:无动作)")]
        [DefaultValue("255")]
        public int AirModel { get; set; } = 255;
        /// <summary>
        /// 模式设定温度
        ///</summary>
        [DisplayName("模式设定温度")]
        [DefaultValue("255")]
        public int AirModelTemp { get; set; } = 255;
        /// <summary>
        /// 空调风速(0:自动1:1档2:2档3:3档255:无动作)
        ///</summary>
        [DisplayName("空调风速(0:自动1:1档2:2档3:3档255:无动作)")]
        [DefaultValue("255")]
        public int AirSpeed { get; set; } = 255;
        /// <summary>
        /// 锁定(0:解锁 1:锁定)
        ///</summary>
        [DisplayName("锁定(0:解锁 1:锁定)")]
        [DefaultValue("255")]
        public int AirKeyLock { get; set; } = 255;
    }

    /// <summary>
    /// FTP更新程序
    ///</summary>
    [DisplayName("FTP更新程序")]
    public class NetAirFtpUpdate
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();

        /// <summary>
        /// 文件名
        ///</summary>
        [DisplayName("文件名")]
        public string FileName { get; set; }

        /// <summary>
        /// 文件和校验值
        ///</summary>
        [DisplayName("文件和校验值")]
        public int BinCheck { get; set; }
    }

    /// <summary>
    /// VRFV4系统参数
    ///</summary>
    [DisplayName("VRFV4系统参数")]
    public class VRFV4Param
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();
        /// <summary>
        /// 季节选择(0:夏季 1:冬季65535:无动作)
        ///</summary>
        [DisplayName("季节选择(0:夏季 1:冬季 255:无动作)")]
        [DefaultValue("255")]
        public int AirSeason { get; set; }
        /// <summary>
        /// 心跳周期(分)
        ///</summary>
        [DisplayName("心跳周期(分)")]
        [DefaultValue("65535")]
        public int HeartRange { get; set; }
        /// <summary>
        /// 运行数据心跳周期(分)
        ///</summary>
        [DisplayName("运行数据心跳周期(分)")]
        [DefaultValue("65535")]
        public int RunRange { get; set; }
        /// <summary>
        /// 夏季判断温度(℃)
        ///</summary>
        [DisplayName("夏季判断温度(℃)")]
        [DefaultValue("255")]
        public int SummerTemp { get; set; }
        /// <summary>
        /// 冬季判断温度(℃)
        ///</summary>
        [DisplayName("冬季判断温度(℃)")]
        [DefaultValue("255")]
        public int WinterTemp { get; set; }
    }

    /// <summary>
    /// 用户参数
    ///</summary>
    [DisplayName("用户参数")]
    public class VRFV4User
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();
        /// <summary>
        /// 开机使能(0:不允许开机 1:按策略执行 2:不执行策略 255:无动作)
        ///</summary>
        [DisplayName("开机使能(0:不允许开机 1:按策略执行 2:不执行策略 255:无动作)")]
        [DefaultValue("255")]
        public int EnableOpen { get; set; }
    }

    /// <summary>
    /// 空调策略(常规)
    ///</summary>
    [DisplayName("空调策略(常规)")]
    public class VRFV4AirGeneral
    {
        /// <summary>
        /// 夏季开机温度值(16~31)
        ///</summary>
        [DisplayName("夏季开机温度值(16~31)")]
        [DefaultValue("255")]
        public int SummerOpenTemp { get; set; }
        /// <summary>
        /// 冬季开机温度值(16~31)
        ///</summary>
        [DisplayName("冬季开机温度值(16~31)")]
        [DefaultValue("255")]
        public int WinterOpenTemp { get; set; }
        /// <summary>
        /// 温度模式开启温度使能 0x00,关闭；0x01，开启，控制器默认值为0x01
        ///</summary>
        [DisplayName("温度模式开启温度使能 0x00,关闭；0x01，开启，控制器默认值为0x01")]
        [DefaultValue("255")]
        public int OpenTempEnable { get; set; }
        /// <summary>
        /// 制冷开启温度
        ///</summary>
        [DisplayName("制冷开启温度")]
        [DefaultValue("255")]
        public int RefrigStartTemp { get; set; }
        /// <summary>
        /// 制热开启温度
        ///</summary>
        [DisplayName("制热开启温度")]
        [DefaultValue("255")]
        public int HotStartTemp { get; set; }
        /// <summary>
        /// 制冷开机温度
        ///</summary>
        [DisplayName("制冷开机温度")]
        [DefaultValue("255")]
        public int RefrigOpenTemp { get; set; }
        /// <summary>
        /// 制冷关机温度
        ///</summary>
        [DisplayName("制冷关机温度")]
        [DefaultValue("255")]
        public int RefrigCloseTemp { get; set; }
        /// <summary>
        /// 制热开机温度
        ///</summary>
        [DisplayName("制热开机温度")]
        [DefaultValue("255")]
        public int HotOpenTemp { get; set; }
        /// <summary>
        /// 制热关机温度
        ///</summary>
        [DisplayName("制热关机温度")]
        [DefaultValue("255")]
        public int HotCloseTemp { get; set; }
        /// <summary>
        /// 温度模式开关机控制:0x01,制冷/制热开机控制；0x02，制冷/制热关机控制；0x03，制冷/制热开关机控制
        ///</summary>
        [DisplayName("温度模式开关机控制:0x01,制冷/制热开机控制；0x02，制冷/制热关机控制；0x03，制冷/制热开关机控制")]
        [DefaultValue("255")]
        public int OpenCloseTemp { get; set; }
        /// <summary>
        /// 工作模式(0:调温1:温度2:时间3:定时) 多模式逗号隔开
        ///</summary>
        [DisplayName("工作模式(0:调温1:温度2:时间3:定时) 多模式逗号隔开")]
        [DefaultValue("255")]
        public string WorkModel { get; set; }
        /// <summary>
        /// 时间段数量
        /// </summary>
        [DisplayName("时间段数量")]
        public int TimesNum { get; set; }
        /// <summary>
        /// 进入时间段自动开机 0x00,关闭；0x01，开启
        /// </summary>
        [DisplayName("进入时间段自动开机 0x00,关闭；0x01，开启")]
        [DefaultValue("255")]
        public int AutoOpen { get; set; }
        /// <summary>
        /// 定时任务数
        /// </summary>
        [DisplayName("定时任务数")]
        public int TaskCount { get; set; }
    }

    /// <summary>
    /// 时间策略
    /// </summary>
    public class VRFV4TimeInfo
    {
        /// <summary>
        /// 使能标志： 0表示不启用，1表示启用
        ///</summary>
        [DisplayName("使能标志： 0表示不启用，1表示启用")]
        public int enable { get; set; }
        /// <summary>
        /// 生效日：位0表示周一，位6表示周日，相应位为1表示生效
        ///</summary>
        [DisplayName("生效日：位0表示周一，位6表示周日，相应位为1表示生效")]
        public int weeks { get; set; }
        /// <summary>
        /// 开始时
        ///</summary>
        [DisplayName("开始时")]
        public int startHour { get; set; }
        /// <summary>
        /// 开始分
        ///</summary>
        [DisplayName("开始分")]
        public int startMinute { get; set; }
        /// <summary>
        /// 结束时
        ///</summary>
        [DisplayName("结束时")]
        public int endHour { get; set; }
        /// <summary>
        /// 结束分
        ///</summary>
        [DisplayName("结束分")]
        public int endMinute { get; set; }
    }

    /// <summary>
    /// 定时策略
    /// </summary>
    public class VRFV4TaskInfo
    {
        /// <summary>
        /// 使能标志： 0表示不启用，1表示启用
        ///</summary>
        [DisplayName("使能标志： 0表示不启用，1表示启用")]
        [DefaultValue("255")]
        public int enable { get; set; }
        /// <summary>
        /// 生效日：位0表示周一，位6表示周日，相应位为1表示生效
        ///</summary>
        [DisplayName("生效日：位0表示周一，位6表示周日，相应位为1表示生效")]
        [DefaultValue("255")]
        public int weeks { get; set; }
        /// <summary>
        /// 开始时
        ///</summary>
        [DisplayName("开始时")]
        [DefaultValue("255")]
        public int startHour { get; set; }
        /// <summary>
        /// 开始分
        ///</summary>
        [DisplayName("开始分")]
        [DefaultValue("255")]
        public int startMin { get; set; }
        /// <summary>
        /// 开关 0:关 1:开
        /// </summary>
        [DisplayName("开关")]
        [DefaultValue("255")]
        public int airSwitch { get; set; }
        /// <summary>
        /// 模式:0自动 1制热 2制冷 4送风 8除湿
        ///</summary>
        [DisplayName("模式")]
        [DefaultValue("255")]
        public int airModel { get; set; }
        /// <summary>
        /// 设置温度
        /// </summary>
        [DisplayName("设置温度")]
        [DefaultValue("255")]
        public int airTemp { get; set; }
        /// <summary>
        /// 风速 0:自动 1:低风 2:中风 3:高速
        ///</summary>
        [DisplayName("风速")]
        [DefaultValue("255")]
        public int airSpeed { get; set; }
    }

    /// <summary>
    /// 空调策略(常规+时间+定时任务)
    ///</summary>
    [DisplayName("空调策略(常规+时间+定时任务)")]
    public class VRFV4Strategy : VRFV4AirGeneral
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();

        /// <summary>
        /// 时间策略集合
        ///</summary>
        [DisplayName("时间策略集合")]
        public List<VRFV4TimeInfo> TimeInfoList { get; set; } = new List<VRFV4TimeInfo>();

        /// <summary>
        /// 定时策略集合
        ///</summary>
        [DisplayName("定时策略集合")]
        public List<VRFV4TaskInfo> TaskInfoList { get; set; } = new List<VRFV4TaskInfo>();
    }

    /// <summary>
    /// 4G风机盘管控制运行状态
    ///</summary>
    [DisplayName("4G风机盘管控制运行状态")]
    public class NetFJPGConRun
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();
        /// <summary>
        /// 开关状态(0：关机1：开机255:无动作)
        ///</summary>
        [DisplayName("开关状态(0：关机1：开机255:无动作)")]
        [DefaultValue("255")]
        public int AirSwitch { get; set; } = 255;
        /// <summary>
        /// 运行模式(0：冷风2：暖风3：睡眠255:无动作)
        ///</summary>
        [DisplayName("运行模式(0：冷风2：暖风3：睡眠255:无动作)")]
        [DefaultValue("255")]
        public int AirModel { get; set; } = 255;
        /// <summary>
        /// 风速(0：自动1：低速2：中速3：高速255:无动作)
        ///</summary>
        [DisplayName("风速(0：自动1：低速2：中速3：高速255:无动作)")]
        [DefaultValue("255")]
        public int AirSpeed { get; set; } = 255;
        /// <summary>
        /// 模式设定温度 16-30
        ///</summary>
        [DisplayName("模式设定温度 16-30")]
        [DefaultValue("255")]
        public int AirModelTemp { get; set; } = 255;
        /// <summary>
        /// 定时开使能(0-关闭 1-开启 255:无动作)
        ///</summary>
        [DisplayName("定时开使能(0-关闭 1-开启 255:无动作)")]
        [DefaultValue("255")]
        public int onEnable { get; set; } = 255;
        /// <summary>
        /// 定时开时间-小时 0-23
        /// </summary>
        [DisplayName("定时开时间-小时 0-23")]
        [DefaultValue("255")]
        public int onTimeHour { get; set; }
        /// <summary>
        /// 定时开时间-分钟 0-59
        ///</summary>
        [DisplayName("定时开时间-分钟 0-59")]
        [DefaultValue("255")]
        public int onTimeMin { get; set; }
        /// <summary>
        /// 定时关使能(0-关闭 1-开启 255:无动作)
        ///</summary>
        [DisplayName("定时关使能(0-关闭 1-开启 255:无动作)")]
        [DefaultValue("255")]
        public int offEnable { get; set; } = 255;
        /// <summary>
        /// 定时关时间-小时 0-23
        /// </summary>
        [DisplayName("定时关时间-小时 0-23")]
        [DefaultValue("255")]
        public int offTimeHour { get; set; }
        /// <summary>
        /// 定时关时间-分钟 0-59
        ///</summary>
        [DisplayName("定时关时间-分钟 0-59")]
        [DefaultValue("255")]
        public int offTimeMin { get; set; }
        /// <summary>
        /// 空调锁定(0：解锁 1：锁定255:无动作)
        ///</summary>
        [DisplayName("空调锁定(0：解锁 1：锁定255:无动作)")]
        [DefaultValue("255")]
        public int airLock { get; set; } = 255;
        /// <summary>
        /// 设备激活状态(0：未激活 1：激活255:无动作)
        ///</summary>
        [DisplayName("设备激活状态(0：未激活 1：激活255:无动作)")]
        [DefaultValue("255")]
        public int activation { get; set; } = 255;
    }

    /// <summary>
    /// 风机盘管通用控制
    ///</summary>
    [DisplayName("其他通用控制")]
    public class NetFJPGCommonSet
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();

        /// <summary>
        /// 服务ID
        /// 读取常规参数(8007)
        /// 读取备用参数(8008)
        /// 读取定时策略(8015)
        /// 读取设备状态(8010)
        /// 远程重启(8009)
        ///</summary>
        [DisplayName("服务ID：读取常规参数(8007)读取备用参数(8008)读取定时策略(8015)读取设备状态(8010)远程重启(8009)")]
        public int DatasetId { get; set; }
    }

    /// <summary>
    /// 风机盘管策略(常规+时间)
    ///</summary>
    [DisplayName("风机盘管策略(常规+时间)")]
    public class FJPGStrategy : NetFJPGGeneral
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();

        /// <summary>
        /// 时间策略集合
        ///</summary>
        [DisplayName("时间策略集合")]
        public List<NetFJPGSeasonTimeInfo> TimeInfoList { get; set; } = new List<NetFJPGSeasonTimeInfo>();

        /// <summary>
        /// 定时策略集合
        ///</summary>
        [DisplayName("定时策略集合")]
        public List<NetFJPGTaskInfo> TaskInfoList { get; set; } = new List<NetFJPGTaskInfo>();
    }

    /// <summary>
    /// 风机盘管策略(常规)
    ///</summary>
    [DisplayName("风机盘管策略(常规)")]
    public class NetFJPGGeneral
    {
        /// <summary>
        /// 人感延时(0-99)
        ///</summary>
        [DisplayName("人感延时(0-99)")]
        [DefaultValue("255")]
        public int humanTime { get; set; }
        /// <summary>
        /// 风机回温差(0-99)
        ///</summary>
        [DisplayName("风机回温差(0-99)")]
        [DefaultValue("255")]
        public int fanTemp { get; set; }
        /// <summary>
        /// 水阀回温差(0-99)
        ///</summary>
        [DisplayName("水阀回温差(0-99)")]
        [DefaultValue("255")]
        public int waterTemp { get; set; }
        /// <summary>
        /// 风速低档界(0-99)
        ///</summary>
        [DisplayName("风速低档界(0-99)")]
        [DefaultValue("255")]
        public int fanMin { get; set; }
        /// <summary>
        /// 风速fanMax档界(0-99)
        ///</summary>
        [DisplayName("风速fanMax档界(0-99)")]
        [DefaultValue("255")]
        public int fanMax { get; set; }
        /// <summary>
        /// 工作模式(0:温差1:风区2:时间3:制热锁定4:制冷锁定5:时段共享6:低档7:中档8:高档) 多模式逗号隔开
        ///</summary>
        [DisplayName("工作模式(0:温差1:风区2:时间3:制热锁定4:制冷锁定5:时段共享6:低档7:中档8:高档) 多模式逗号隔开")]
        [DefaultValue("65535")]
        public string workModel { get; set; }
        /// <summary>
        /// 季节使能(0:春季1:夏季2:秋季3:冬季) 多模式逗号隔开
        ///</summary>
        [DisplayName("季节使能(0:春季1:夏季2:秋季3:冬季) 多模式逗号隔开")]
        [DefaultValue("255")]
        public string seasonEnable { get; set; }
    }

    public class NetFJPGSeasonTimeInfo
    {
        /// <summary>
        /// 季节
        ///</summary>
        [DisplayName("季节")]
        public int season { get; set; }
        /// <summary>
        /// 月份
        ///</summary>
        [DisplayName("月份")]
        public int month { get; set; }
        /// <summary>
        /// 时间策略集合
        ///</summary>
        [DisplayName("时间策略集合")]
        public List<NetFJPGTimeInfo> times { get; set; }
    }

    /// <summary>
    /// 风机盘管时间策略
    /// </summary>
    public class NetFJPGTimeInfo
    {
        /// <summary>
        /// 日期类型(1、2、3、4、5、6、7)
        ///</summary>
        [DisplayName("日期类型(1、2、3、4、5、6、7)")]
        public int DayType { get; set; }
        /// <summary>
        /// 时段序号(1-4)
        ///</summary>
        [DisplayName("时段序号(1-4)")]
        public int TimeNum { get; set; }
        /// <summary>
        /// 设定温度(0-99，255：无效)
        ///</summary>
        [DisplayName("设定温度(0-99，255：无效)")]
        [DefaultValue("255")]
        public int setTemp { get; set; }
        /// <summary>
        /// 锁定温度(0-99，255：无效)
        ///</summary>
        [DisplayName("锁定温度(0-99，255：无效)")]
        [DefaultValue("255")]
        public int lockTemp { get; set; }
        /// <summary>
        /// 环境锁定(0-99，255：无效)
        ///</summary>
        [DisplayName("环境锁定(0-99，255：无效)")]
        [DefaultValue("255")]
        public int envirLock { get; set; }
        /// <summary>
        /// 开始时
        ///</summary>
        [DisplayName("开始时")]
        public int startHour { get; set; }
        /// <summary>
        /// 开始分
        ///</summary>
        [DisplayName("开始分")]
        public int startMinute { get; set; }
        /// <summary>
        /// 结束时
        ///</summary>
        [DisplayName("结束时")]
        public int endHour { get; set; }
        /// <summary>
        /// 结束分
        ///</summary>
        [DisplayName("结束分")]
        public int endMinute { get; set; }
        /// <summary>
        /// 运行模式(0:冷风 2:暖风 3:睡眠 255:无效)
        ///</summary>
        [DisplayName("运行模式(0:冷风 2:暖风 3:睡眠 255:无效)")]
        [DefaultValue("255")]
        public int airModel { get; set; }
        /// <summary>
        /// 运行参数(bit0:锁定 bit1:风速自动 bit2:环境温度 bit3:定时关机 bit4:定时开机 bit5:自动关机 255:无效)
        ///</summary>
        [DisplayName("运行参数(bit0:锁定 bit1:风速自动 bit2:环境温度 bit3:定时关机 bit4:定时开机 bit5:自动关机 255:无效)")]
        [DefaultValue("255")]
        public int airParams { get; set; }
        /// <summary>
        /// 自动关机间隔(0-255)
        ///</summary>
        [DisplayName("自动关机间隔(0-255)")]
        [DefaultValue("255")]
        public int shutRange { get; set; }
    }

    /// <summary>
    /// 风机盘管定时策略
    /// </summary>
    public class NetFJPGTaskInfo
    {
        /// <summary>
        /// 日期类型(1、2、3、4、5、6、7)
        ///</summary>
        [DisplayName("日期类型(1、2、3、4、5、6、7)")]
        public int DayType { get; set; }
        /// <summary>
        /// 时段序号(1-4)
        ///</summary>
        [DisplayName("时段序号(1-4)")]
        public int TimeNum { get; set; }
        /// <summary>
        /// 开始时
        ///</summary>
        [DisplayName("开始时")]
        [DefaultValue("255")]
        public int startHour { get; set; }
        /// <summary>
        /// 开始分
        ///</summary>
        [DisplayName("开始分")]
        [DefaultValue("255")]
        public int startMin { get; set; }
        /// <summary>
        /// 开关 0:关 1:开
        /// </summary>
        [DisplayName("开关")]
        [DefaultValue("255")]
        public int airSwitch { get; set; }
        /// <summary>
        /// 模式:0冷风 2暖风 3睡眠
        ///</summary>
        [DisplayName("模式")]
        [DefaultValue("255")]
        public int airModel { get; set; }
        /// <summary>
        /// 设置温度
        /// </summary>
        [DisplayName("设置温度")]
        [DefaultValue("255")]
        public int airTemp { get; set; }
        /// <summary>
        /// 风速 0:自动 1:低风 2:中风 3:高速
        ///</summary>
        [DisplayName("风速")]
        [DefaultValue("255")]
        public int airSpeed { get; set; }
    }

    /// <summary>
    /// Asl灯控制运行状态
    ///</summary>
    [DisplayName("Asl灯控制运行状态")]
    public class NetAslConRun
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();
        /// <summary>
        /// 控制模式(1:开启0:关闭)
        ///</summary>
        [DisplayName("控制模式(1:开启0:关闭)")]
        [DefaultValue("255")]
        public int DeviceSwitch { get; set; } = 255;
    }

    /// <summary>
    /// Asl灯控策略(定时任务)
    ///</summary>
    [DisplayName("Asl灯控策略(定时任务)")]
    public class ClockAslStrategy
    {
        /// <summary>
        /// 空调信息集合
        ///</summary>
        [DisplayName("空调信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();

        /// <summary>
        /// 定时策略集合
        ///</summary>
        [DisplayName("定时策略集合")]
        public List<ClockSetting> TaskInfoList { get; set; } = new List<ClockSetting>();
    }

    /// <summary>
    /// 时钟设置
    /// </summary>
    public class ClockSetting
    {
        /// <summary>
        /// 时钟编号
        /// </summary>
        [DefaultValue("1")]
        public int ClockNum { get; set; }

        /// <summary>
        /// 星期配置（7位字符串，每位对应一天，0=不启用，1=启用）
        /// </summary>
        [DefaultValue("")]
        public string ClockWeek { get; set; }

        /// <summary>
        /// 控制类型 1:定时开关，2：定时开，3：定时关
        /// </summary>
        [DefaultValue("3")]
        public int ControlType { get; set; }

        /// <summary>
        /// 开启小时
        /// </summary>
        [DefaultValue("255")]
        public int ClockOpenHour { get; set; }

        /// <summary>
        /// 开启分钟
        /// </summary>
        [DefaultValue("255")]
        public int ClockOpenMinute { get; set; }

        /// <summary>
        /// 关闭小时
        /// </summary>
        [DefaultValue("255")]
        public int ClockCloseHour { get; set; }

        /// <summary>
        /// 关闭分钟
        /// </summary>
        [DefaultValue("255")]
        public int ClockCloseMinute { get; set; }
    }

    /// <summary>
    /// 热水器控制运行状态
    ///</summary>
    [DisplayName("热水器控制运行状态")]
    public class NetRSQConRun
    {
        /// <summary>
        /// 设备信息集合
        ///</summary>
        [DisplayName("设备信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();
        /// <summary>
        /// 控制模式(1:开启0:关闭255:无动作)
        ///</summary>
        [DisplayName("控制模式(1:开启0:关闭255:无动作)")]
        [DefaultValue("255")]
        public int DeviceSwitch { get; set; } = 255;
    }

    /// <summary>
    /// 集水井控制运行状态
    ///</summary>
    [DisplayName("集水井控制运行状态")]
    public class NetJSJConRun
    {
        /// <summary>
        /// 设备信息集合
        ///</summary>
        [DisplayName("设备信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();
        /// <summary>
        /// 一号水泵控制(1:开启0:关闭255:无动作)
        ///</summary>
        [DisplayName("一号水泵控制(1:开启0:关闭255:无动作)")]
        [DefaultValue("255")]
        public int Pump1Control { get; set; } = 255;
        /// <summary>
        /// 二号水泵控制(1:开启0:关闭255:无动作)
        ///</summary>
        [DisplayName("二号水泵控制(1:开启0:关闭255:无动作)")]
        [DefaultValue("255")]
        public int Pump2Control { get; set; } = 255;
    }

    /// <summary>
    /// 通风控制箱控制运行状态
    ///</summary>
    [DisplayName("通风控制箱控制运行状态")]
    public class NetTFConRun
    {
        /// <summary>
        /// 设备信息集合
        ///</summary>
        [DisplayName("设备信息集合")]
        public List<NetAirConInfo> DeviceInfoList { get; set; } = new List<NetAirConInfo>();
        /// <summary>
        /// 排风阀控制(1:开启0:关闭255:无动作)
        ///</summary>
        [DisplayName("排风阀控制(1:开启0:关闭255:无动作)")]
        [DefaultValue("255")]
        public int EA_DMPControl { get; set; } = 255;
        /// <summary>
        /// 新风阀控制(1:开启0:关闭255:无动作)
        ///</summary>
        [DisplayName("新风阀控制(1:开启0:关闭255:无动作)")]
        [DefaultValue("255")]
        public int FA_DMPControl { get; set; } = 255;
        /// <summary>
        /// 新风支阀控制(1:开启0:关闭255:无动作)
        ///</summary>
        [DisplayName("新风支阀控制(1:开启0:关闭255:无动作)")]
        [DefaultValue("255")]
        public int BR_DMPControl { get; set; } = 255;
    }
}
