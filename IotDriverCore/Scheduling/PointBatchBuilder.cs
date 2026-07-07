namespace IotDriverCore
{
    /// <summary>
    /// 点位自动合包器(§6.2:按从站→功能码→地址排序,合并连续/近邻地址为限长区段;
    /// 空洞容忍阈值可配,gaptolerance=0即"禁止跨洞"——部分设备对未定义地址整段回异常)
    /// </summary>
    public static class PointBatchBuilder
    {
        /// <summary>
        /// 把点表合包为最少物理读批次
        /// </summary>
        /// <param name="points">点位清单</param>
        /// <param name="maxlength">单批次最大长度(Modbus寄存器上限125/0x7D)</param>
        /// <param name="gaptolerance">相邻点位间可容忍的空洞长度(读回丢弃间隙比多发一帧便宜)</param>
        public static List<PointBatch> Build(IEnumerable<DriverPoint> points, int maxlength = 125, int gaptolerance = 8)
        {
            var result = new List<PointBatch>();
            foreach (var group in points.GroupBy(t => (t.SlaveAddr, t.FuncCode)))
            {
                PointBatch? current = null;
                foreach (var point in group.OrderBy(t => t.Address))
                {
                    int pointlen = Math.Max(1, point.Length);
                    if (current != null)
                    {
                        int gap = point.Address - (current.StartAddress + current.TotalLength);
                        int newlength = point.Address + pointlen - current.StartAddress;
                        if (gap <= gaptolerance && newlength <= maxlength)
                        {
                            current.TotalLength = Math.Max(current.TotalLength, newlength);
                            current.Points.Add(point);
                            continue;
                        }
                    }
                    current = new PointBatch
                    {
                        SlaveAddr = point.SlaveAddr,
                        FuncCode = point.FuncCode,
                        StartAddress = point.Address,
                        TotalLength = pointlen,
                        Points = { point }
                    };
                    result.Add(current);
                }
            }
            return result;
        }
    }
}
