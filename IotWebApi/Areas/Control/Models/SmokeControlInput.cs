namespace IotWebApi.Areas.Control.Models
{
    /// <summary>
    /// 烟感控制入参
    /// </summary>
    public class SmokeControlInput
    {
        /// <summary>
        /// 设备ID集合
        /// </summary>
        public List<int> DeviceIds { get; set; } = new();

        /// <summary>
        /// 1:取消消音 2:消音
        /// </summary>
        public int SoundType { get; set; }
    }
}
