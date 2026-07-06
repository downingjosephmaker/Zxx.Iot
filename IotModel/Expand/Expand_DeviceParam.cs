using SqlSugar;
using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备参数表拓展属性
    ///</summary>
    [DisplayName("设备参数表拓展属性")]
    [Expand]
    public class Expand_DeviceParam
    {
        /// <summary>
        /// 设备路数(总路,1路/A,2路/B,3路/C)
        ///</summary>
        [DisplayName("设备路数(总路,1路/A,2路/B,3路/C)")]
        public string SubChannel { get; set; } = "总路";
        /// <summary>
        /// 参数编码
        ///</summary>
        [DisplayName("参数编码")]
        public string ParamCode { get; set; }
        /// <summary>
        /// 参数名称
        ///</summary>
        [DisplayName("参数名称")]
        public string ParamName { get; set; }
        /// <summary>
        /// 参数分类名称
        ///</summary>
        [DisplayName("参数分类名称")]
        public string ParamTypeName { get; set; }
        /// <summary>
        /// 参数地址
        ///</summary>
        [DisplayName("参数地址")]
        public int ParamAddr { get; set; }
        /// <summary>
        /// 参数修正公式(a*1)
        ///</summary>
        [DisplayName("参数修正公式(a*1)")]
        public string ParamFormula { get; set; }
        /// <summary>
        /// 值类型(数值,状态,数位)
        ///</summary>
        [DisplayName("值类型(数值,状态,数位)")]
        public string ValueType { get; set; }
        /// <summary>
        /// 状态值集合
        ///</summary>
        [DisplayName("状态值集合")]
        public List<Expand_ParamStatusValue> StatusValues { get; set; } = new List<Expand_ParamStatusValue>();
        /// <summary>
        /// 值单位
        ///</summary>
        [DisplayName("值单位")]
        public string ValueUnit { get; set; }
        /// <summary>
        /// 最大合法值
        ///</summary>
        [DisplayName("最大合法值")]
        public decimal ParamMaxValue { get; set; }
        /// <summary>
        /// 最小合法值
        ///</summary>
        [DisplayName("最小合法值")]
        public decimal ParamMinValue { get; set; }
        /// <summary>
        /// 最大跳变量
        ///</summary>
        [DisplayName("最大跳变量")]
        public decimal ParamChangeValue { get; set; }
        /// <summary>
        /// 是否显示(0:否1:是)
        ///</summary>
        [DisplayName("是否显示(0:否1:是)")]
        public bool IsShow { get; set; }
        /// <summary>
        /// 是否配置(0:否1:是)
        ///</summary>
        [DisplayName("是否配置(0:否1:是)")]
        public bool IsSet { get; set; }
        /// <summary>
        /// 极值计算(0:否1:是)
        ///</summary>
        [DisplayName("极值计算(0:否1:是)")]
        public bool IsPeak { get; set; }
        /// <summary>
        /// 统计计算(0:否1:是)
        ///</summary>
        [DisplayName("统计计算(0:否1:是)")]
        public bool IsReport { get; set; }
        /// <summary>
        /// 采集时间
        ///</summary>
        [DisplayName("采集时间")]
        public string CollectTime { get; set; }
        /// <summary>
        /// 上次参数值
        ///</summary>
        [DisplayName("上次参数值")]
        public string ParamLastValue { get; set; }
        /// <summary>
        /// 参数值
        ///</summary>
        [DisplayName("参数值")]
        public string ParamValue { get; set; }
        /// <summary>
        /// 是否采集(1:采集;0:不采集)
        ///</summary>
        [DisplayName("是否采集(1:采集;0:不采集)")]
        public int IsCollection { get; set; }
        /// <summary>
        /// 是否告警(0:正常 1:告警)
        ///</summary>
        [DisplayName("是否告警(0:正常 1:告警)")]
        public int IsAlarm { get; set; }
        /// <summary>
        /// 小数显示位数
        ///</summary>
        [DisplayName("小数显示位数")]
        public int DecimalDigit { get; set; }
        /// <summary>
        /// 是否乘PT(0:否1:是)
        ///</summary>
        [DisplayName("是否乘Pt(0:否1:是)")]
        public bool IsPt { get; set; }
        /// <summary>
        /// 是否乘CT(0:否1:是)
        ///</summary>
        [DisplayName("是否乘Pt(0:否1:是)")]
        public bool IsCt { get; set; }
        /// <summary>
        /// 是否自定义告警显示(0:否1:是)
        ///</summary>
        [DisplayName("是否自定义告警显示(0:否1:是)")]
        public bool IsCustomAlarm { get; set; }
        /// <summary>
        /// 是否主显示(0:否1:是)
        ///</summary>
        [DisplayName("是否主显示(0:否1:是)")]
        public bool IsMainShow { get; set; }
    }
}