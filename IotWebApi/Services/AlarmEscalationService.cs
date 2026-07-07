using CenBoCommon.Zxx;
using IotLog;
using IotModel;
using System.Collections.Concurrent;

namespace IotWebApi.Services
{
    /// <summary>
    /// 告警升级链服务(§9.5:告警产生立即通知第一梯队(通知服务承接),
    /// 未Ack未恢复按15/30/60分钟渐进重复并升级下一梯队,Ack或恢复自动中断;
    /// 每步到期重读EventAlarm行判定中断(已处理/已恢复/行删除),无需跨服务取消接线;
    /// 时刻表走完即终止;跟踪表存内存,重启后在途升级链丢失但告警本体不受影响)
    /// </summary>
    public class AlarmEscalationService : IHostedService
    {
        private const string Service_CATEGORY = "告警升级链";

        /// <summary>
        /// 渐进时刻表(分钟,自告警产生起算;第N步升级至EscalationLevel&lt;=N的渠道)
        /// </summary>
        private static readonly int[] StepMinutes = { 15, 30, 60 };

        /// <summary>
        /// 扫描周期
        /// </summary>
        private static readonly TimeSpan ScanWindow = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 升级链跟踪态
        /// </summary>
        private class TrackState
        {
            public EventSignal Signal;    // 通知内容快照
            public DateTime FireTime;     // 告警产生时刻
            public int Step;              // 已完成步数(0=尚未升级)
        }

        /// <summary>
        /// 在途升级链(EventAlarm主键→跟踪态)
        /// </summary>
        private readonly ConcurrentDictionary<long, TrackState> _tracks = new();

        /// <summary>
        /// 告警通知服务(梯队外发承接)
        /// </summary>
        private readonly AlarmNotifyService _notifyService;

        private CancellationTokenSource _cts;
        private Task _scanTask;

        public AlarmEscalationService(AlarmNotifyService notifyService)
        {
            _notifyService = notifyService;
        }

        #region 服务生命周期

        /// <summary>
        /// 启动升级扫描
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _scanTask = Task.Run(() => ScanLoopAsync(_cts.Token));
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "告警升级链服务已启动", Service_CATEGORY);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止升级扫描
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            if (_scanTask != null)
            {
                try { await _scanTask; } catch (OperationCanceledException) { }
            }
            LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "告警升级链服务已停止", Service_CATEGORY);
        }

        #endregion

        /// <summary>
        /// 登记升级链(数据入库服务在第一梯队通知后调用,仅IsNote且未静默的新告警;
        /// 同一活动告警重复登记以最新为准,链自新时刻重新起算)
        /// </summary>
        public void Track(long alarmid, EventSignal signal)
        {
            if (alarmid <= 0 || signal == null) return;
            _tracks[alarmid] = new TrackState { Signal = signal, FireTime = DateTime.Now, Step = 0 };
        }

        /// <summary>
        /// 扫描主循环(到期步:重读告警行,Ack/恢复/行删除即中断,否则重复低梯队并升级新梯队)
        /// </summary>
        private async Task ScanLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(ScanWindow, token);
                    var now = DateTime.Now;
                    foreach (var pair in _tracks)
                    {
                        var state = pair.Value;
                        if (state.Step >= StepMinutes.Length)
                        {
                            _tracks.TryRemove(pair.Key, out _);
                            continue;
                        }
                        if ((now - state.FireTime).TotalMinutes < StepMinutes[state.Step]) continue;
                        try
                        {
                            var alarm = EventAlarmDAO.Instance.GetOneBy(t => t.SnowId == pair.Key);
                            if (alarm == null || alarm.CheckResult == "已处理" || alarm.IsRestore == "已恢复")
                            {
                                _tracks.TryRemove(pair.Key, out _); // Ack或恢复自动中断后续梯队
                                continue;
                            }
                            state.Step++;
                            _notifyService.NotifyEscalation(state.Signal, state.Step);
                            if (state.Step >= StepMinutes.Length) _tracks.TryRemove(pair.Key, out _); // 链走完终止
                        }
                        catch (Exception ex)
                        {
                            LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName,
                                $"告警[{pair.Key}]升级链第{state.Step + 1}步执行失败：{ex}", Service_CATEGORY);
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
    }
}
