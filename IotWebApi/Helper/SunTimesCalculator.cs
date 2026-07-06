using System;

namespace IotWebApi.Helper
{
    /// <summary>
    /// 基于球面天文学简化算法的日出日落计算器
    /// 纬度影响日出日落的主要规律，误差通常在2分钟以内
    /// </summary>
    public static class SunTimesCalculator
    {
        /// <summary>
        /// 计算日出日落、天亮天黑时间（北京时间 UTC+8）
        /// 日出日落 = 太阳中心在地平线下0.833°（大气折射+视半径修正）
        /// 天亮天黑 = 民用晨光始/昏影终（太阳在地平线下6°）
        /// </summary>
        /// <param name="latitude">纬度（正为北）</param>
        /// <param name="longitude">经度（正为东）</param>
        /// <param name="date">日期</param>
        /// <returns>包含日出、日落、天亮、天黑时间的元组</returns>
        public static (DateTime? Sunrise, DateTime? Sunset, DateTime? Dawn, DateTime? Dusk) Calculate(double latitude, double longitude, DateTime date)
        {
            if (latitude == 0 && longitude == 0)
                return (null, null, null, null);

            // 北京时间所在时区中心经度
            double timezoneLng = 120.0;
            DateTime baseDate = date.Date;

            // 日序 N（1月1日=1）
            int N = date.DayOfYear;

            // ① 太阳赤纬角 δ（弧度）
            double rad = Math.PI / 180.0;
            double delta = 0.4093 * Math.Sin(rad * (360.0 / 365.0) * (N - 81));

            double latRad = latitude * rad;

            // ② 计算12 GetById时角 H（度）
            // 日出日落：天顶角 = 90.833°
            var sunriseHA = CalcHourAngle(latRad, delta, 90.833, rad);
            var sunsetHA = CalcHourAngle(latRad, delta, 90.833, rad);
            // 天亮天黑：天顶角 = 96°
            var dawnHA = CalcHourAngle(latRad, delta, 96.0, rad);
            var duskHA = CalcHourAngle(latRad, delta, 96.0, rad);

            if (!sunriseHA.HasValue || !sunsetHA.HasValue)
                return (null, null, null, null);

            // ③ 地方真太阳时（小时）
            double sunriseLocal = 12.0 - sunriseHA.Value / 15.0;
            double sunsetLocal = 12.0 + sunsetHA.Value / 15.0;
            double dawnLocal = dawnHA.HasValue ? 12.0 - dawnHA.Value / 15.0 : sunriseLocal;
            double duskLocal = duskHA.HasValue ? 12.0 + duskHA.Value / 15.0 : sunsetLocal;

            // ④ 修正到北京时间
            double lngCorrection = (timezoneLng - longitude) / 15.0; // 经度修正（小时）

            // 均时差（时差方程）
            double B = rad * (360.0 / 365.0) * (N - 81);
            double EoT = 9.87 * Math.Sin(2 * B) - 7.53 * Math.Cos(B) - 1.5 * Math.Sin(B);
            double eotHours = EoT / 60.0; // 分钟转小时

            DateTime sr = baseDate.AddHours(sunriseLocal + lngCorrection + eotHours);
            DateTime ss = baseDate.AddHours(sunsetLocal + lngCorrection + eotHours);
            DateTime d = baseDate.AddHours(dawnLocal + lngCorrection + eotHours);
            DateTime dk = baseDate.AddHours(duskLocal + lngCorrection + eotHours);

            return (sr, ss, d, dk);
        }

        /// <summary>
        /// 计算时角 H（度）
        /// </summary>
        private static double? CalcHourAngle(double latRad, double deltaRad, double zenithDeg, double rad)
        {
            double cosHA = -Math.Tan(latRad) * Math.Tan(deltaRad)
                         - Math.Cos(zenithDeg * rad) / (Math.Cos(latRad) * Math.Cos(deltaRad));

            if (cosHA > 1.0) return null; // 极夜
            if (cosHA < -1.0) return null; // 极昼

            return Math.Acos(cosHA) / rad; // 返回度
        }
    }
}
