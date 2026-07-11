using IotSimulator.Core.Scenario;

namespace IotSimulator
{
    /// <summary>
    /// 协议模拟器控制台宿主(dotnet run -- --scenario path.json 起场景;
    /// 收发帧hex摘要打印到控制台,Ctrl+C优雅退出;
    /// 参数解析朴素args遍历,不引System.CommandLine——KISS)
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string? scenarioPath = ParseScenarioPath(args);
            if (scenarioPath == null)
            {
                Console.WriteLine("用法: dotnet run --project IotSimulator -- --scenario <场景文件路径>");
                Console.WriteLine("示例: dotnet run --project IotSimulator -- --scenario scenarios/dlt645-dtu-3meters.json");
                return 1;
            }

            ScenarioModel scenario;
            try
            {
                scenario = ScenarioLoader.Load(scenarioPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"场景加载失败: {ex.Message}");
                return 1;
            }

            Console.WriteLine($"====== 协议模拟器 ======");
            Console.WriteLine($"场景    : {scenario.Name}");
            Console.WriteLine($"协议    : {scenario.Protocol}");
            Console.WriteLine($"传输    : {scenario.Transport.Mode}  {scenario.Transport.Host}:{scenario.Transport.Port}");
            Console.WriteLine($"设备数  : {scenario.Devices.Count}");
            Console.WriteLine($"========================");

            using var runner = new ScenarioRunner(scenario, Log);
            try
            {
                runner.Start();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"场景启动失败: {ex.Message}");
                return 1;
            }

            using var exit = new ManualResetEventSlim(false);
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;  // 拦截默认终止,走优雅退出
                Console.WriteLine();
                Log("收到退出信号，正在停止场景...");
                exit.Set();
            };

            Log("场景运行中，按 Ctrl+C 退出。");
            exit.Wait();
            runner.Stop();
            Log("场景已停止。");
            return 0;
        }

        /// <summary>
        /// 朴素解析 --scenario 参数(支持 --scenario=path 与 --scenario path 两种写法)
        /// </summary>
        private static string? ParseScenarioPath(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.StartsWith("--scenario=", StringComparison.OrdinalIgnoreCase))
                    return arg["--scenario=".Length..];
                if (arg.Equals("--scenario", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    return args[i + 1];
            }
            return null;
        }

        /// <summary>
        /// 带时间戳的控制台日志
        /// </summary>
        private static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
    }
}
