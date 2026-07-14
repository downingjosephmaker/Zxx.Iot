/**
 * “记住密码”本地可逆加密（浏览器原生 Web Crypto AES-GCM，无第三方依赖）
 *
 * 背景：登录页“记住密码”需在下次进入时把密码回填输入框，因此本质上必须能还原明文，
 * 密钥内嵌于前端属该功能的固有限制（无法做到服务端级别的不可逆保护）。本模块的目标是：
 *   1. 消除 localStorage 明文暴露——防 F12 / 共享机器 / 日志窥视直接读到密码；
 *   2. 阻断跨站密码复用攻击——落盘的是密文而非用户真实明文。
 * 需在安全上下文（HTTPS 或 localhost）下运行，本项目本地/隧道均满足。
 */

// 应用内嵌口令（派生 AES 密钥用；换值会使旧密文失效，需用户重新记住一次）
const SECRET = "Zxx.Iot::remember::v1";
const IV_LEN = 12;

async function deriveKey(): Promise<CryptoKey> {
  const digest = await crypto.subtle.digest(
    "SHA-256",
    new TextEncoder().encode(SECRET)
  );
  return crypto.subtle.importKey("raw", digest, { name: "AES-GCM" }, false, [
    "encrypt",
    "decrypt"
  ]);
}

function toBase64(bytes: Uint8Array): string {
  let bin = "";
  bytes.forEach(b => (bin += String.fromCharCode(b)));
  return btoa(bin);
}

function fromBase64(text: string): Uint8Array {
  return Uint8Array.from(atob(text), c => c.charCodeAt(0));
}

/** 加密明文；空串原样返回。不可用时抛错由调用方兜底。 */
export async function encryptRemember(plain: string): Promise<string> {
  if (!plain) return "";
  const key = await deriveKey();
  const iv = crypto.getRandomValues(new Uint8Array(IV_LEN));
  const cipher = await crypto.subtle.encrypt(
    { name: "AES-GCM", iv },
    key,
    new TextEncoder().encode(plain)
  );
  const merged = new Uint8Array(IV_LEN + cipher.byteLength);
  merged.set(iv, 0);
  merged.set(new Uint8Array(cipher), IV_LEN);
  return toBase64(merged);
}

/** 解密密文；失败（旧明文残留 / 篡改 / 换密钥）时返回空串，视为无记忆。 */
export async function decryptRemember(payload: string): Promise<string> {
  if (!payload) return "";
  try {
    const bytes = fromBase64(payload);
    const iv = bytes.slice(0, IV_LEN);
    const data = bytes.slice(IV_LEN);
    const key = await deriveKey();
    const plain = await crypto.subtle.decrypt(
      { name: "AES-GCM", iv },
      key,
      data
    );
    return new TextDecoder().decode(plain);
  } catch {
    return "";
  }
}
