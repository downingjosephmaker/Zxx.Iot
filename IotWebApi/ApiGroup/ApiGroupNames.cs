namespace IotWebApi
{
    /// <summary>
    /// 系统分组枚举值
    /// </summary>
    public enum ApiGroupNames
    {
        [GroupInfo(PrimaryKey = "All", Title = "平台全部接口", Description = "平台全部接口")]
        All = 0,
        [GroupInfo(PrimaryKey = "Admin", Title = "权限模块", Description = "权限模块")]
        Admin = 1,
        [GroupInfo(PrimaryKey = "Basic", Title = "基础模块", Description = "基础模块")]
        Basic = 5,
        [GroupInfo(PrimaryKey = "Device", Title = "设备管理模块", Description = "设备管理模块")]
        Device = 10,
        [GroupInfo(PrimaryKey = "Event", Title = "记录模块模块", Description = "记录模块模块")]
        Event = 25,
        [GroupInfo(PrimaryKey = "Scada", Title = "组态大屏模块", Description = "组态大屏模块")]
        Scada = 26,
        [GroupInfo(PrimaryKey = "ThirdIot", Title = "三方IOT模块", Description = "三方IOT模块")]
        ThirdIot = 30,
        [GroupInfo(PrimaryKey = "Service", Title = "服务调用模块", Description = "服务调用模块")]
        Service = 60,
        [GroupInfo(PrimaryKey = "Control", Title = "设备控制模块", Description = "设备控制模块")]
        Control = 70,
        [GroupInfo(PrimaryKey = "Monitor", Title = "驾驶舱模块", Description = "驾驶舱模块")]
        Monitor = 500,

    }
}
