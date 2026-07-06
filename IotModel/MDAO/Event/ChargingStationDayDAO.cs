namespace IotModel
{
    public sealed partial class ChargingStationDayDAO : FullEntityContext<ChargingStationDayEntity>
    {
        private static ChargingStationDayDAO instance;
        public static ChargingStationDayDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ChargingStationDayDAO();
                }
                return instance;
            }
        }

    }
}