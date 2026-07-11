using IotDriverCore;
using Xunit;

namespace IotTests
{
    /// <summary>
    /// PointBatchBuilder合包逻辑单测(连续/近邻合并/空洞容忍/跨从站分组/最大长度截断)
    /// </summary>
    public class PointBatchBuilderTests
    {
        private static DriverPoint P(int slave, byte func, int addr, int len = 1) =>
            new() { SlaveAddr = slave, FuncCode = func, Address = addr, Length = len };

        [Fact]
        public void 连续地址_合并为单批次()
        {
            var points = new[] { P(1, 3, 0), P(1, 3, 1), P(1, 3, 2) };
            var batches = PointBatchBuilder.Build(points, 125, 8);
            Assert.Single(batches);
            Assert.Equal(0, batches[0].StartAddress);
            Assert.Equal(3, batches[0].TotalLength);
            Assert.Equal(3, batches[0].Points.Count);
        }

        [Fact]
        public void 空洞在容忍内_合并()
        {
            var points = new[] { P(1, 3, 0), P(1, 3, 5) };  // 间隔4,容忍8
            var batches = PointBatchBuilder.Build(points, 125, 8);
            Assert.Single(batches);
            Assert.Equal(0, batches[0].StartAddress);
            Assert.Equal(6, batches[0].TotalLength);  // 0..5共6
        }

        [Fact]
        public void 空洞超容忍_拆分批次()
        {
            var points = new[] { P(1, 3, 0), P(1, 3, 50) };  // 间隔49,容忍8
            var batches = PointBatchBuilder.Build(points, 125, 8);
            Assert.Equal(2, batches.Count);
        }

        [Fact]
        public void 禁止跨洞_零容忍()
        {
            var points = new[] { P(1, 3, 0), P(1, 3, 1), P(1, 3, 3) };
            var batches = PointBatchBuilder.Build(points, 125, 0);
            // 0,1连续合一批;3与1间有洞(gap=2>0)→单独一批
            Assert.Equal(2, batches.Count);
        }

        [Fact]
        public void 超最大长度_拆分()
        {
            var points = new[] { P(1, 3, 0), P(1, 3, 100) };  // 跨度101>maxlength100
            var batches = PointBatchBuilder.Build(points, 100, 200);
            Assert.Equal(2, batches.Count);
        }

        [Fact]
        public void 不同从站_分组独立()
        {
            var points = new[] { P(1, 3, 0), P(2, 3, 0) };
            var batches = PointBatchBuilder.Build(points, 125, 8);
            Assert.Equal(2, batches.Count);
        }

        [Fact]
        public void 不同功能码_分组独立()
        {
            var points = new[] { P(1, 3, 0), P(1, 4, 0) };
            var batches = PointBatchBuilder.Build(points, 125, 8);
            Assert.Equal(2, batches.Count);
        }

        [Fact]
        public void 多寄存器点位_总长按占用累计()
        {
            var points = new[] { P(1, 3, 0, 2), P(1, 3, 2, 2) };  // 各占2寄存器
            var batches = PointBatchBuilder.Build(points, 125, 8);
            Assert.Single(batches);
            Assert.Equal(4, batches[0].TotalLength);
        }

        [Fact]
        public void 乱序地址_排序后合包()
        {
            var points = new[] { P(1, 3, 2), P(1, 3, 0), P(1, 3, 1) };
            var batches = PointBatchBuilder.Build(points, 125, 8);
            Assert.Single(batches);
            Assert.Equal(0, batches[0].StartAddress);
        }

        [Fact]
        public void 空输入_空结果()
        {
            var batches = PointBatchBuilder.Build(Array.Empty<DriverPoint>());
            Assert.Empty(batches);
        }
    }
}
