using System.Security.Cryptography;
using System.Text;
using CenBoCommon.Zxx;
using IotLog;
using IotModel;

namespace IotWebApi.Services
{
    /// <summary>
    /// 告警通知服务(§9.5:IsNote=1的告警按启用渠道逐一外发;
    /// 渠道四型——1邮件(POST到现有EmailUrl形式的外发接口)/2Webhook(原文JSON)/
    /// 3钉钉机器人(text消息,Secret非空时HMAC-SHA256加签)/4企微机器人(text消息)/5短信预留;
    /// 等级过滤按渠道配置;发送异步化不阻塞入库管道,失败只记日志;
    /// 升级链notify_escalation(未Ack渐进重复)待Ack状态机落地后接入)
    /// </summary>
    public class AlarmNotifyService
    {
        private const string Service_CATEGORY = "告警通知服务";

        /// <summary>
        /// 渠道缓存刷新周期
        /// </summary>
        private static readonly TimeSpan ConfigTtl = TimeSpan.FromSeconds(60);

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

        /// <summary>
        /// 渠道快照
        /// </summary>
        private volatile List<NotifyChannel> _channels = new();

        private DateTime _configTime = DateTime.MinValue;
        private readonly object _configLock = new();

        /// <summary>
        /// 外发一批告警(仅IsNote的告警调用;渠道逐一异步发送,不阻塞调用方)
        /// </summary>
        public void Notify(List<EventSignal> signals)
        {
            try
            {
                if (!signals.IsZxxAny()) return;
                EnsureConfig();
                var channels = _channels;
                if (!channels.IsZxxAny()) return;
                foreach (var signal in signals)
                {
                    foreach (var channel in channels)
                    {
                        if (!MatchGrade(channel, signal)) continue;
                        var ch = channel;
                        var sig = signal;
                        _ = Task.Run(() => SendAsync(ch, sig));
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"通知调度失败：{ex}", Service_CATEGORY);
            }
        }

        /// <summary>
        /// 清空渠道缓存(配置变更热重载)
        /// </summary>
        public void Reload()
        {
            lock (_configLock) { _configTime = DateTime.MinValue; }
        }

        /// <summary>
        /// 等级过滤(渠道GradeFilter逗号清单命中;空=全部通知;等级取EventContent前缀[等级])
        /// </summary>
        private static bool MatchGrade(NotifyChannel channel, EventSignal signal)
        {
            if (channel.GradeFilter.IsZxxNullOrEmpty()) return true;
            var grades = channel.GradeFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return grades.Any(t => signal.EventContent?.StartsWith($"[{t}]") == true);
        }

        /// <summary>
        /// 单渠道发送(按类型分发,失败只记日志)
        /// </summary>
        private async Task SendAsync(NotifyChannel channel, EventSignal signal)
        {
            try
            {
                if (channel.TargetUrl.IsZxxNullOrEmpty()) return;
                string text = $"{signal.EventTime} [{signal.UnitName}]{signal.DeviceName}：{signal.EventContent}";
                switch (channel.ChannelType)
                {
                    case 1:
                        // 邮件:对齐现有EmailUrl外发接口形式(接收人/主题/内容)
                        await PostJsonAsync(channel.TargetUrl, new
                        {
                            receivers = channel.Receivers ?? "",
                            subject = $"设备告警通知-{signal.DeviceName}",
                            content = text
                        }.ToJson());
                        break;
                    case 3:
                        // 钉钉机器人(Secret非空时按官方算法加签:timestamp+"\n"+secret的HMAC-SHA256)
                        string url = channel.TargetUrl;
                        if (!channel.Secret.IsZxxNullOrEmpty())
                        {
                            long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            string sign = DingTalkSign(timestamp, channel.Secret);
                            url = $"{url}&timestamp={timestamp}&sign={Uri.EscapeDataString(sign)}";
                        }
                        await PostJsonAsync(url, new { msgtype = "text", text = new { content = text } }.ToJson());
                        break;
                    case 4:
                        // 企微机器人
                        await PostJsonAsync(channel.TargetUrl, new { msgtype = "text", text = new { content = text } }.ToJson());
                        break;
                    case 5:
                        // 短信预留
                        LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"短信渠道[{channel.ChannelName}]预留未实现,告警[{signal.SnowId}]未外发", Service_CATEGORY);
                        break;
                    default:
                        // Webhook:告警原文JSON直发
                        await PostJsonAsync(channel.TargetUrl, signal.ToJson());
                        break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"渠道[{channel.ChannelName}]发送告警[{signal.SnowId}]失败：{ex.Message}", Service_CATEGORY);
            }
        }

        /// <summary>
        /// POST JSON(UTF-8)
        /// </summary>
        private static async Task PostJsonAsync(string url, string json)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// 钉钉机器人加签(HMAC-SHA256(timestamp+"\n"+secret)后Base64)
        /// </summary>
        private static string DingTalkSign(long timestamp, string secret)
        {
            var data = Encoding.UTF8.GetBytes($"{timestamp}\n{secret}");
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return Convert.ToBase64String(hmac.ComputeHash(data));
        }

        /// <summary>
        /// 渠道快照过期时整体重建(仅加载启用中的渠道)
        /// </summary>
        private void EnsureConfig()
        {
            if (DateTime.Now - _configTime <= ConfigTtl) return;
            lock (_configLock)
            {
                if (DateTime.Now - _configTime <= ConfigTtl) return;
                try
                {
                    _channels = (NotifyChannelDAO.Instance.GetList() ?? new List<NotifyChannel>())
                        .Where(t => t.IsEnable).ToList();
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"通知渠道加载失败：{ex}", Service_CATEGORY);
                }
                _configTime = DateTime.Now;
            }
        }
    }
}
