using Microsoft.AspNetCore.Mvc;

namespace IotWebApi
{
    public class ControllerBaseApi : ControllerBase
    {
        /// <summary>
        /// 分页查询总行数
        /// </summary>
        public int TotalCount = 1;
        /// <summary>
        /// 提示字符串
        /// </summary>
        public string Message { get; set; } = "";
        /// <summary>
        /// 提示字符串
        /// </summary>
        public bool Status { get; set; } = true;

        /// <summary>
        /// 康慈租户能耗计算时需排除的设备名称关键字（总表、热水回水、热水进水）
        /// </summary>
        protected static readonly string[] KangciExcludedDeviceNames = { "总表", "热水回水", "热水进水" };

        /// <summary>
        /// 判断当前登录租户是否为"康慈"
        /// </summary>
        /// <returns></returns>
        protected bool IsKangciTenant()
        {
            try
            {
                var optmdl = Request.GetToken();
                return optmdl != null && optmdl.TenantName != null && optmdl.TenantName.Contains("康慈");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断设备名称是否属于康慈租户能耗计算需排除的设备（总表、热水回水、热水进水）
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <returns></returns>
        protected static bool IsKangciExcludedDeviceName(string deviceName)
        {
            return deviceName != null && KangciExcludedDeviceNames.Any(name => deviceName.Contains(name));
        }
    }
}
