using System.Collections.Concurrent;
using CenBoCommon.Zxx;
using IotDriverCore;
using IotModel;

namespace IotWebApi.Services
{
    /// <summary>
    /// JS协议脚本管理服务(§6.4:沙箱缓存按脚本版本热切换,升级新实例替换旧实例;
    /// 试运行走独立临时沙箱干跑无副作用,不影响运行时缓存;
    /// 脚本默认禁用,GetSandbox只对启用脚本供给运行时)
    /// </summary>
    public class ProtocolScriptService
    {
        /// <summary>
        /// 沙箱缓存条目(版本变化即重建)
        /// </summary>
        private class CacheEntry
        {
            public int Version;
            public ScriptSandbox Sandbox;
        }

        /// <summary>脚本ID→沙箱缓存</summary>
        private readonly ConcurrentDictionary<long, CacheEntry> _cache = new();

        /// <summary>
        /// 取启用脚本的运行时沙箱(未启用/不存在/编译失败返回null由调用方降级;版本变化热切换)
        /// </summary>
        public ScriptSandbox GetSandbox(long scriptid)
        {
            var script = ProtocolScriptDAO.Instance.GetOneBy(t => t.SnowId == scriptid);
            if (script == null || !script.IsEnable) return null;
            var entry = _cache.GetOrAdd(scriptid, _ => new CacheEntry { Version = -1 });
            if (entry.Version != script.Version)
            {
                lock (entry)
                {
                    if (entry.Version != script.Version)
                    {
                        entry.Sandbox = new ScriptSandbox(script.ScriptContent ?? "");
                        entry.Version = script.Version;
                    }
                }
            }
            return entry.Sandbox is { Ready: true } ? entry.Sandbox : null;
        }

        /// <summary>
        /// 按产品类型编码取启用脚本的沙箱(§6.5:非JSON载荷按产品挂脚本解码)
        /// </summary>
        public ScriptSandbox GetSandboxByTypeCode(string typecode)
        {
            if (typecode.IsZxxNullOrEmpty()) return null;
            var script = (ProtocolScriptDAO.Instance.GetList()?.Cast<ProtocolScript>() ?? Enumerable.Empty<ProtocolScript>())
                .FirstOrDefault(t => t.IsEnable && string.Equals(t.DeviceTypeCode, typecode, StringComparison.OrdinalIgnoreCase));
            return script == null ? null : GetSandbox(script.SnowId);
        }

        /// <summary>
        /// 试运行干跑(临时沙箱即用即弃;草稿内容优先,为空时按脚本ID取库内内容——
        /// 不校验IsEnable,编辑调试阶段脚本通常尚未启用)
        /// </summary>
        public ScriptRunResult DryRun(long scriptid, string scriptcontent, string funcname, string inputhex, string inputjson, string contextjson)
        {
            string content = scriptcontent;
            if (content.IsZxxNullOrEmpty() && scriptid > 0)
            {
                content = ProtocolScriptDAO.Instance.GetOneBy(t => t.SnowId == scriptid)?.ScriptContent ?? "";
            }
            if (content.IsZxxNullOrEmpty())
            {
                return new ScriptRunResult { FuncName = funcname ?? "", Error = "脚本内容为空" };
            }
            var sandbox = new ScriptSandbox(content);
            return (funcname ?? "").Trim().ToLowerInvariant() switch
            {
                "encode" => sandbox.RunEncode(inputjson, contextjson),
                "splitframes" => sandbox.RunSplitFrames(inputhex, contextjson),
                _ => sandbox.RunDecode(inputhex, contextjson)
            };
        }

        /// <summary>
        /// 清空沙箱缓存(保存/删除脚本后调用,下次取用按新版本重建)
        /// </summary>
        public void Reload() => _cache.Clear();
    }
}
