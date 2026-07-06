using System.ComponentModel;

namespace IotWebApi.Areas.Control.Models
{
    /// <summary>
    /// 主动上报系统参数
    ///</summary>
    [DisplayName("主动上报系统参数")]
    public class NetAirParamUp
    {
        /// <summary>
		/// 唯一ID
		///</summary>
		[DisplayName("唯一ID")]
        public string Guid { get; set; }
        //系统参数
        /// <summary>
        /// 品牌代码(65535:无动作)
        ///</summary>
        [DisplayName("品牌代码(65535:无动作)")]
        public string BrandCode { get; set; }
        /// <summary>
        /// 电流阈值(65535:无动作)
        ///</summary>
        [DisplayName("电流阈值(65535:无动作)")]
        public int ElecRange { get; set; }
        /// <summary>
        /// 插座检测功率阈值(65535:无动作)
        ///</summary>
        [DisplayName("插座检测功率阈值(65535:无动作)")]
        public int CzCheckPower { get; set; }
        /// <summary>
        /// 温度选择(1.内部温度 2.外部温度65535:无动作)
        ///</summary>
        [DisplayName("温度选择(1.内部温度 2.外部温度65535:无动作)")]
        public int TempType { get; set; }
        /// <summary>
        /// 心跳周期
        ///</summary>
        [DisplayName("心跳周期")]
        public int HeartRange { get; set; }
        /// <summary>
        /// 电能清零(1:清零65535:无动作)
        ///</summary>
        [DisplayName("电能清零(1:清零65535:无动作)")]
        public int EnergyClear { get; set; }
        /// <summary>
        /// 关机限流最大值
        ///</summary>
        [DisplayName("关机限流最大值")]
        public int XlMaxElec { get; set; }
        /// <summary>
        /// 休眠模式
        ///</summary>
        [DisplayName("休眠模式")]
        public int SleepMode { get; set; }
        /// <summary>
        /// 预存电量
        ///</summary>
        [DisplayName("预存电量")]
        public int StoredEnergy { get; set; }
        /// <summary>
        /// 季节模式
        ///</summary>
        [DisplayName("季节模式")]
        public int AirSeason { get; set; }
        /// <summary>
        /// 允许空调开机
        ///</summary>
        [DisplayName("允许空调开机")]
        public int EnableOpen { get; set; }
        /// <summary>
        /// 插座监控
        ///</summary>
        [DisplayName("插座监控")]
        public int AirSocket { get; set; }

        //策略常规参数
        /// <summary>
        /// 人感延迟时间
        ///</summary>
        [DisplayName("人感延迟时间")]
        public int HumanTime { get; set; }
        /// <summary>
        /// 人感延迟人数
        /// </summary>
        [DisplayName("人感延迟人数")]
        public int HumanNum { get; set; }
        /// <summary>
        /// 制冷开启温度
        /// </summary>
        [DisplayName("制冷开启温度")]
        public int RefrigStartTemp { get; set; }
        /// <summary>
        /// 制热开启温度
        /// </summary>
        [DisplayName("制热开启温度")]
        public int HotStartTemp { get; set; }
        /// <summary>
        /// 制冷开机温度
        /// </summary>
        [DisplayName("制冷开机温度")]
        public int RefrigOpenTemp { get; set; }
        /// <summary>
        /// 制热开机温度
        /// </summary>
        [DisplayName("制热开机温度")]
        public int HotOpenTemp { get; set; }
        /// <summary>
        /// 制冷关机温度
        /// </summary>
        [DisplayName("制冷关机温度")]
        public int RefrigCloseTemp { get; set; }
        /// <summary>
        /// 制热关机温度
        /// </summary>
        [DisplayName("制热关机温度")]
        public int HotCloseTemp { get; set; }
        /// <summary>
        /// 工作模式
        /// </summary>
        [DisplayName("工作模式")]
        public int WorkModel { get; set; }
        /// <summary>
        /// 临时设定时间
        /// </summary>
        [DisplayName("临时设定时间")]
        public int TemporaryTime { get; set; }
        /// <summary>
        /// 夏季判断温度
        /// </summary>
        [DisplayName("夏季判断温度")]
        public int SummerTemp { get; set; }
        /// <summary>
        /// 冬季判断温度
        /// </summary>
        [DisplayName("冬季判断温度")]
        public int WinterTemp { get; set; }
        /// <summary>
        /// 温度模式开启温度使能
        /// </summary>
        [DisplayName("温度模式开启温度使能")]
        public int OpenTempEnable { get; set; }
        /// <summary>
        /// 温度使能运行方式
        /// </summary>
        [DisplayName("温度使能运行方式")]
        public int OperatModeTemp { get; set; }
        /// <summary>
        /// 温度模式开关机控制
        /// </summary>
        [DisplayName("温度模式开关机控制")]
        public int OpenCloseTemp { get; set; }
        /// <summary>
        /// 夏季开机温度值
        /// </summary>
        [DisplayName("夏季开机温度值")]
        public int SummerOpenTemp { get; set; }
        /// <summary>
        /// 冬季开机温度值
        /// </summary>
        [DisplayName("冬季开机温度值")]
        public int WinterOpenTemp { get; set; }
    }

    /// <summary>
    /// 接口调用参数配置结构
    /// </summary>
    public class NetAirParaAPI
    {
        /// <summary>
        /// 保存类型0:主动上报系统参数1:保存系统参数2:保存策略参数3:保存用户参数4:保存时间参数
        ///</summary>
        [DisplayName("保存类型0:主动上报系统参数1:保存系统参数2:保存策略参数3:保存用户参数4:保存时间参数")]
        public int eType { get; set; }
        /// <summary>
        /// guid
        ///</summary>
        [DisplayName("guid")]
        public string guid { get; set; }
        /// <summary>
        /// 工作模式
        ///</summary>
        [DisplayName("工作模式")]
        public string WorkModel { get; set; }
        /// <summary>
        /// 参数
        ///</summary>
        [DisplayName("参数")]
        public string data { get; set; }
    }
}
