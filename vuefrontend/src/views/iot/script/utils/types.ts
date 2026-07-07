// 协议脚本类型定义
import type { QueryTableParams } from "@/api/type";

export type { QueryTableParams };
export type {
  ProtocolScriptItem,
  ProtocolScriptHistoryItem,
  ScriptDryRunPara,
  ScriptRunResultItem
} from "@/api/iot/script";

export interface ProtocolScriptFormItemProps {
  title?: string;
  SnowId: string | number;
  ScriptName: string;
  DeviceTypeCode: string;
  ScriptContent: string;
  /** 版本号(库内自增,仅展示) */
  Version: number;
  SampleHex: string;
  SampleContext: string;
  /** 安全默认禁用,需显式启用 */
  IsEnable: boolean;
}

export interface ProtocolScriptFormProps {
  formInline: ProtocolScriptFormItemProps;
}

/** 新建脚本预填的三段式骨架 */
export const SCRIPT_TEMPLATE = `// 三段式协议脚本（decode 必备，encode/splitFrames 按需）
// frame/buffer 为 Uint8Array，context 为 {deviceKey, deviceId, now} 等模拟上下文

// 上行解码：返回 {telemetry:[{key:"参数编码", value:工程值}]}
function decode(frame, context) {
  var telemetry = [];
  // 示例：前两个字节大端拼成温度，缩放0.1
  // telemetry.push({ key: "temp", value: ((frame[0] << 8) | frame[1]) * 0.1 });
  return { telemetry: telemetry };
}

// 下行编码（可选）：command 为命令对象，返回下发帧的 hex 字符串
function encode(command, context) {
  return "";
}

// 帧定界（可选）：返回 {frames:[hex...], consumed:已消费字节数}
function splitFrames(buffer, context) {
  return { frames: [], consumed: 0 };
}
`;
