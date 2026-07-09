using CenBoCommon.Zxx;
using System;
using System.Collections.Generic;

namespace IotModel
{
    public sealed partial class DeviceComfortDAO : DbContext<DeviceComfort>
    {
        private static DeviceComfortDAO instance;
        public static DeviceComfortDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeviceComfortDAO();
                }
                return instance;
            }
        }

        public override void Init()
        {
            try
            {
                DateTime time = DateTime.Now;
                List<DeviceComfort> comfortlist = new List<DeviceComfort>();
                DeviceComfort xiaji = new DeviceComfort
                {
                    SnowId = SnowModel.Instance.NewId(),
                    ComfortName = "夏季",
                    EnvirHumidity = 50,
                    ComfortFormula = "(1.818*T+18.18)*(0.88+0.002*H)+(T-32)/(45-T)+8.6",
                    MonthFormula = "M > 3 AND M < 11",
                    CreateId = 1,
                    CreateTime = time.ToDateTimeString(),
                    CreateName = "开发管理员",
                    UpdateId = 1,
                    UpdateTime = time.ToDateTimeString(),
                    UpdateName = "开发管理员",
                    TenantId = 1
                };
                comfortlist.Add(xiaji);
                DeviceComfort dongxiaji = new DeviceComfort
                {
                    SnowId = SnowModel.Instance.NewId(),
                    ComfortName = "冬季",
                    EnvirHumidity = 40,
                    ComfortFormula = "(1.818*T+18.18)*(0.88+0.002*H)+(T-32)/(45-T)+18.2",
                    MonthFormula = "(M >= 1 AND M <= 3) OR (M >= 11 AND M <= 12)",
                    CreateId = 1,
                    CreateTime = time.ToDateTimeString(),
                    CreateName = "开发管理员",
                    UpdateId = 1,
                    UpdateTime = time.ToDateTimeString(),
                    UpdateName = "开发管理员",
                    TenantId = 1
                };
                comfortlist.Add(dongxiaji);
                InsertRange(comfortlist);
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
            }
        }

    }
}