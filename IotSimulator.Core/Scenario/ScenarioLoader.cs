using System.Text.Json;

namespace IotSimulator.Core.Scenario
{
    /// <summary>
    /// 场景加载器(System.Text.Json;camelCase字段忽略大小写,兼容注释与尾逗号)
    /// </summary>
    public static class ScenarioLoader
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>
        /// 从文件加载场景(文件不存在/解析失败抛异常由调用方处理)
        /// </summary>
        public static ScenarioModel Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"场景文件不存在:{path}");
            var json = File.ReadAllText(path);
            var model = JsonSerializer.Deserialize<ScenarioModel>(json, Options)
                        ?? throw new InvalidDataException($"场景文件解析为空:{path}");
            return model;
        }

        /// <summary>
        /// 从JSON字符串加载(单测/内嵌用)
        /// </summary>
        public static ScenarioModel Parse(string json)
        {
            return JsonSerializer.Deserialize<ScenarioModel>(json, Options)
                   ?? throw new InvalidDataException("场景JSON解析为空。");
        }
    }
}
