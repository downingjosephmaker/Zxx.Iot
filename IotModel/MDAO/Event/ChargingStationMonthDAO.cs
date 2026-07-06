namespace IotModel
{
    public sealed partial class ChargingStationMonthDAO : FullEntityContext<ChargingStationMonthEntity>
    {
        private static ChargingStationMonthDAO instance;
        public static ChargingStationMonthDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ChargingStationMonthDAO();
                }
                return instance;
            }
        }

    }
}