namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 运营月统计类
    /// </summary>
    public class XjByGdYtj
    {
        /// <summary>
        /// 巡检-月总数
        /// </summary>
        public int XjTotal { get; set; } = 0;
        /// <summary>
        /// 巡检-待执行数
        /// </summary>
        public int XjDzx { get; set; } = 0;
        /// <summary>
        /// 巡检-已完成数
        /// </summary>
        public int XjYwc { get; set; } = 0;
        /// <summary>
        /// 巡检-已超时数
        /// </summary>
        public int XjYcs { get; set; } = 0;
        /// <summary>
        /// 巡检-完成率
        /// </summary>
        public string XjWcl { get; set; }
        /// <summary>
        /// 巡检-处理时长
        /// </summary>
        public double XjAvgDuration { get; set; }

        /// <summary>
        /// 保养-月总数
        /// </summary>
        public int ByTotal { get; set; } = 0;
        /// <summary>
        /// 保养-待执行数
        /// </summary>
        public int ByDzx { get; set; } = 0;
        /// <summary>
        /// 保养-已完成数
        /// </summary>
        public int ByYwc { get; set; } = 0;
        /// <summary>
        /// 保养-已超时数
        /// </summary>
        public int ByYcs { get; set; } = 0;
        /// <summary>
        /// 保养-完成率
        /// </summary>
        public string ByWcl { get; set; }
        /// <summary>
        /// 保养-异常率
        /// </summary>
        public string ByYichangLv { get; set; }

        /// <summary>
        /// 工单-月总数
        /// </summary>
        public int GdTotal { get; set; } = 0;
        /// <summary>
        /// 工单-待执行数
        /// </summary>
        public int GdDzx { get; set; } = 0;
        /// <summary>
        /// 工单-已完成数
        /// </summary>
        public int GdYwc { get; set; } = 0;
        /// <summary>
        /// 工单-已超时数
        /// </summary>
        public int GdYcs { get; set; } = 0;
        /// <summary>
        /// 工单-完成率
        /// </summary>
        public string GdWcl { get; set; }
        /// <summary>
        /// 工单-处理时长
        /// </summary>
        public double GdAvgDuration { get; set; }
        /// <summary>
        /// 工单-响应时长
        /// </summary>
        public double GdAvgResponse { get; set; }
    }

    /// <summary>
    /// 运行监测总览
    /// </summary>
    public class DeviceTypeFx
    {
        /// <summary>
        /// 空调(zhkt)
        /// </summary>
        public DeviceTypeItem1 KongTiaoItem { get; set; } = new DeviceTypeItem1();
        /// <summary>
        /// 照明(zhdk)
        /// </summary>
        public DeviceTypeItem1 ZhaoMingItem { get; set; } = new DeviceTypeItem1();
        /// <summary>
        /// 电表(zndb)
        /// </summary>
        public DeviceTypeItem2 DianBiaoItem { get; set; } = new DeviceTypeItem2();
        /// <summary>
        /// 水表(znsb)
        /// </summary>
        public DeviceTypeItem2 ShuiBiaoItem { get; set; } = new DeviceTypeItem2();
        /// <summary>
        /// 空气质量(zhcgq)
        /// </summary>
        public DeviceTypeItem2 KongQiLiangItem { get; set; } = new DeviceTypeItem2();

        /// <summary>
        /// 电梯(zhdt)
        /// </summary>
        public DeviceTypeItem3 DianTiItem { get; set; } = new DeviceTypeItem3();
        /// <summary>
        /// 热水(znrsq)
        /// </summary>
        public DeviceTypeItem3 ReShuiQiItem { get; set; } = new DeviceTypeItem3();
        /// <summary>
        /// 集水井(JiShuiJin)
        /// </summary>
        public DeviceTypeItem3 JiShuiJingItem { get; set; } = new DeviceTypeItem3();
        /// <summary>
        /// 风机(FanControl)
        /// </summary>
        public DeviceTypeItem3 FengJiItem { get; set; } = new DeviceTypeItem3();

        /// <summary>
        /// 燃气(zhrq)
        /// </summary>
        public decimal RanQi { get; set; }
    }

    /// <summary>
    /// 运行总览子项
    /// </summary>
    public class DeviceTypeFxBase
    {
        /// <summary>
        /// 在线率
        /// </summary>
        public decimal ZaiXianLv { get; set; }

        /// <summary>
        /// 异常率
        /// </summary>
        public decimal YiChangLv { get; set; }
    }

    /// <summary>
    /// 空调、照明
    /// </summary>
    public class DeviceTypeItem1 : DeviceTypeFxBase
    {
        /// <summary>
        /// 运行率
        /// </summary>
        public decimal YunXingLv { get; set; }
    }

    /// <summary>
    /// 水电表
    /// </summary>
    public class DeviceTypeItem2 : DeviceTypeFxBase
    {
        /// <summary>
        /// 能耗读数
        /// </summary>
        public decimal NengHaoDuShu { get; set; }
    }

    /// <summary>
    /// 电梯、热水、集水井、风机
    /// </summary>
    public class DeviceTypeItem3
    {
        /// <summary>
        /// 正常数
        /// </summary>
        public int ZhengChangShu { get; set; }
        /// <summary>
        /// 运行数
        /// </summary>
        public int YunXingShu { get; set; }
        /// <summary>
        /// 异常数
        /// </summary>
        public int YiChangShu { get; set; }
    }

}
