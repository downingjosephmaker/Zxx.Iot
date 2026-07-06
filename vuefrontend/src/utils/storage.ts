/**
 * 统一的本地存储工具
 * 自动添加命名空间前缀，避免命名冲突
 */

import { storageLocal } from "@pureadmin/utils";
import { responsiveStorageNameSpace } from "@/config";

/**
 * 获取带命名空间的存储键名
 */
const getStorageKey = (key: string): string => {
  return `${responsiveStorageNameSpace()}${key}`;
};

/**
 * 统一的存储操作对象
 */
export const storage = {
  /**
   * 设置本地存储数据
   * @param key 存储键名（会自动添加命名空间前缀）
   * @param value 存储值
   */
  setItem<T = any>(key: string, value: T): void {
    storageLocal().setItem(getStorageKey(key), value);
  },

  /**
   * 获取本地存储数据
   * @param key 存储键名（会自动添加命名空间前缀）
   * @returns 存储的值
   */
  getItem<T = any>(key: string): T | null {
    return storageLocal().getItem<T>(getStorageKey(key));
  },

  /**
   * 移除本地存储数据
   * @param key 存储键名（会自动添加命名空间前缀）
   */
  removeItem(key: string): void {
    storageLocal().removeItem(getStorageKey(key));
  },

  /**
   * 清空所有本地存储数据（仅清除带命名空间的数据）
   */
  clear(): void {
    const prefix = responsiveStorageNameSpace();
    const keys = Object.keys(localStorage);
    keys.forEach(key => {
      if (key.startsWith(prefix)) {
        localStorage.removeItem(key);
      }
    });
  },

  /**
   * 获取所有带命名空间的存储键名
   * @returns 所有键名数组
   */
  keys(): string[] {
    const prefix = responsiveStorageNameSpace();
    const keys = Object.keys(localStorage);
    return keys
      .filter(key => key.startsWith(prefix))
      .map(key => key.replace(prefix, ""));
  },

  /**
   * 检查是否存在指定键名的存储数据
   * @param key 存储键名
   * @returns 是否存在
   */
  hasItem(key: string): boolean {
    return storageLocal().getItem(getStorageKey(key)) !== null;
  },

  /**
   * 获取存储数据的大小（字节）
   * @param key 存储键名
   * @returns 数据大小（字节）
   */
  getItemSize(key: string): number {
    const value = storageLocal().getItem(getStorageKey(key));
    if (!value) return 0;
    return new Blob([JSON.stringify(value)]).size;
  },

  /**
   * 获取所有存储数据的总大小（字节）
   * @returns 总大小（字节）
   */
  getTotalSize(): number {
    const prefix = responsiveStorageNameSpace();
    const keys = Object.keys(localStorage);
    let totalSize = 0;
    keys.forEach(key => {
      if (key.startsWith(prefix)) {
        const value = localStorage.getItem(key);
        if (value) {
          totalSize += new Blob([value]).size;
        }
      }
    });
    return totalSize;
  }
};

/**
 * 默认导出
 */
export default storage;

/**
 * 使用示例：
 *
 * import { storage } from "@/utils/storage";
 *
 * // 设置数据
 * storage.setItem("user-info", { name: "张三", age: 25 });
 *
 * // 获取数据
 * const userInfo = storage.getItem<UserInfo>("user-info");
 *
 * // 移除数据
 * storage.removeItem("user-info");
 *
 * // 清空所有数据
 * storage.clear();
 *
 * // 检查是否存在
 * if (storage.hasItem("token")) {
 *   console.log("Token exists");
 * }
 *
 * // 获取所有键名
 * const keys = storage.keys();
 * console.log("所有存储键名:", keys);
 */
