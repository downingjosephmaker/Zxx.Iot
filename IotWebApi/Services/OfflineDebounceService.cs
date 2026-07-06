using CenBoCommon.Zxx;
using IotLog;
using System.Collections.Concurrent;

namespace IotWebApi.Services
{
    /// <summary>
    /// 上下线判定服务(§7.5:推送不节流、判定要防抖——离线先进疑似中间态,
    /// 持续超过确认时长才正式判离线,期间恢复静默取消;上线即时生效但通知60秒限频;
    /// 网关连带合并待M3通道/网关拓扑落地后接入)
    /// </summary>
    public class OfflineDebounceService : IHostedService
    {
        private const string Service_CATEGORY = "上下线判定服务";

        /// <summary>
        /// 离线确认时长秒(疑似离线持续超过该时长才正式判离线,默认心跳周期60秒×3;
        /// M4告警引擎落地后改读AlarmConfirmSeconds配置)
        /// </summary>
        public const int ConfirmSeconds = 180;

        /// <summary>
        /// 同一设备上线通知最小间隔秒(配合flapping封禁,防抖动设备刷屏)
        /// </summary>
        public const int OnlineNotifySeconds = 60;

        /// <summary>
        /// 疑似离线扫描周期
        /// </summary>
        private static readonly TimeSpan ScanWindow = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 疑似离线中间态
        /// </summary>
        private class SuspectState
        {
            public int DeviceState;        // 目标状态(1掉电/0离线)
            public DateTime SuspectTime;   // 进入疑似态时刻
        }

        /// <summary>
        /// 疑似离线设备集合
        /// </summary>
        private readonly ConcurrentDictionary<int, SuspectState> _suspects = new();

        /// <summary>
        /// 设备最近一次上线通知时刻
        /// </summary>
        private readonly ConcurrentDictionary<int, DateTime> _lastOnlineNotify = new();

        /// <summary>
        /// 离线确认回调(确认清单由数据入库服务落库+推送)
        /// </summary>
        public Action<List<(int DeviceId, int DeviceState, string Reason)>> ConfirmHandler { get; set; }

        private CancellationTokenSource _cts;
        private Task _scanTask;

        #region 服务生命周期

        /// <summary>
        /// 启动疑似离线扫描
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _scanTask = Task.Run(() => ScanLoopAsync(_cts.Token));
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "上下线判定服务已启动", Service_CATEGORY);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止疑似离线扫描
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            if (_scanTask != null)
            {
                try { await _scanTask; } catch (OperationCanceledException) { }
            }
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "上下线判定服务已停止", Service_CATEGORY);
        }

        #endregion

        #region 判定入口

        /// <summary>
        /// 设备上线(即时生效;疑似离线期间恢复则静默取消不产生事件;
        /// 返回是否允许发上线通知,同设备60秒内只通知一次)
        /// </summary>
        public bool OnOnline(int deviceid)
        {
            _suspects.TryRemove(deviceid, out _);
            var now = DateTime.Now;
            if (_lastOnlineNotify.TryGetValue(deviceid, out var last) && (now - last).TotalSeconds < OnlineNotifySeconds)
            {
                return false;
            }
            _lastOnlineNotify[deviceid] = now;
            return true;
        }

        /// <summary>
        /// 设备疑似离线(进入中间态,不立即落库;持续超过确认时长由扫描循环正式判离线)
        /// </summary>
        public void OnSuspectOffline(int deviceid, int devicestate)
        {
            _suspects.TryAdd(deviceid, new SuspectState { DeviceState = devicestate, SuspectTime = DateTime.Now });
        }

        #endregion

        #region 确认扫描

        /// <summary>
        /// 扫描主循环(疑似离线超过确认时长的设备正式判离线并回调落库)
        /// </summary>
        private async Task ScanLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(ScanWindow, token);
                    var now = DateTime.Now;
                    var confirms = new List<(int DeviceId, int DeviceState, string Reason)>();
                    foreach (var pair in _suspects)
                    {
                        if ((now - pair.Value.SuspectTime).TotalSeconds < ConfirmSeconds) continue;
                        if (_suspects.TryRemove(pair.Key, out var state))
                        {
                            // 插件上报的状态丢失属原因2=无数据采集(链路在但设备超时无应答)
                            confirms.Add((pair.Key, state.DeviceState, "无数据采集(设备超时无应答)"));
                        }
                    }
                    if (confirms.IsZxxAny() && ConfirmHandler != null)
                    {
                        try
                        {
                            ConfirmHandler(confirms);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"离线确认{confirms.Count}台设备落库失败：{ex}", Service_CATEGORY);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 服务停止，正常退出
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"扫描循环意外退出：{ex}", Service_CATEGORY);
            }
        }

        #endregion
    }
}
