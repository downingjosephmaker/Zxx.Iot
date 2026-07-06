using System.ComponentModel;

namespace IotWebApi.Areas.Device.Models
{
    /// <summary>
    /// 设备策略查询参数
    /// 遵循 DeviceMonitorSearch 模式：平铺分页字段 + 业务字段，方法内手动构造 ActionPara。
    /// 策略内容条件统一使用中文输入(后端按中文匹配设备的解析结果)，前端无需关心底层编码。
    /// </summary>
    public class StrategyQueryPara
    {
        /// <summary>
        /// 当前页
        ///</summary>
        [DisplayName("当前页")]
        public int page { get; set; } = 1;

        /// <summary>
        /// 每页记录数
        ///</summary>
        [DisplayName("每页记录数")]
        public int pagesize { get; set; } = 20;

        // ===== 必传条件 =====

        /// <summary>
        /// 设备类型编码(必传)
        ///</summary>
        [DisplayName("设备类型编码")]
        public string TypeCode { get; set; }

        // ===== 常规设备条件 =====

        /// <summary>
        /// 设备名称(模糊查询)
        /// </summary>
        [DisplayName("设备名称")]
        public string DevName { get; set; }

        /// <summary>
        /// 建筑ID
        ///</summary>
        [DisplayName("建筑ID")]
        public int BuildId { get; set; }

        /// <summary>
        /// 部门ID
        ///</summary>
        [DisplayName("部门ID")]
        public int DeptId { get; set; }

        /// <summary>
        /// 设备状态(-1:全部 2:在线 1:掉电 0:离线)
        ///</summary>
        [DisplayName("设备状态")]
        [DefaultValue("-1")]
        public int DeviceState { get; set; } = -1;

        // ===== 策略内容条件(额外查询，统一传中文)=====

        /// <summary>
        /// 通用模糊查询关键字(匹配常规策略/时间策略/定时任务的描述内容，命中任一即可)
        /// 如输入"周一"可查到时间策略含周一的设备；输入"26℃"可查到含该温度的策略。
        ///</summary>
        [DisplayName("策略内容关键字")]
        public string Keyword { get; set; }

        /// <summary>
        /// 工作模式(中文，多选逗号分隔，如"调温,人感"，匹配任一)
        /// 取值：调温/人感/温度/时间/手动/计量/断电/机房省电/临时(VRF: 调温/温度/时间/定时)
        ///</summary>
        [DisplayName("工作模式")]
        public string WorkModel { get; set; }

        /// <summary>
        /// 季节选择(中文，如"夏季"或"冬季")
        ///</summary>
        [DisplayName("季节选择")]
        public string AirSeason { get; set; }

        /// <summary>
        /// 是否有时间策略(-1:全部 1:有 0:无)
        ///</summary>
        [DisplayName("是否有时间策略")]
        [DefaultValue("-1")]
        public int HasTiming { get; set; } = -1;

        /// <summary>
        /// 是否有定时任务(-1:全部 1:有 0:无)
        ///</summary>
        [DisplayName("是否有定时任务")]
        [DefaultValue("-1")]
        public int HasTask { get; set; } = -1;

        /// <summary>
        /// 是否已配置策略(-1:全部 0:未配置 1:已配置)
        ///</summary>
        [DisplayName("是否已配置策略")]
        [DefaultValue("-1")]
        public int HasStrategy { get; set; } = -1;
    }
}
