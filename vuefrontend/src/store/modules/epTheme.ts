import { defineStore } from "pinia";
import { store, getConfig, responsiveStorageNameSpace } from "../utils";
import { storage } from "@/utils/storage";

export const useEpThemeStore = defineStore("pure-epTheme", {
  state: () => ({
    epThemeColor:
      storage.getItem<StorageConfigs>(`${responsiveStorageNameSpace()}layout`)
        ?.epThemeColor ?? getConfig().EpThemeColor,
    epTheme:
      storage.getItem<StorageConfigs>(`${responsiveStorageNameSpace()}layout`)
        ?.theme ?? getConfig().Theme
  }),
  getters: {
    getEpThemeColor(state) {
      return state.epThemeColor;
    },
    /** 用于mix导航模式下hamburger-svg的fill属性 */
    fill(state) {
      if (state.epTheme === "light") {
        return "#409eff";
      } else {
        return "#fff";
      }
    }
  },
  actions: {
    setEpThemeColor(newColor: string): void {
      const layout = storage.getItem<StorageConfigs>(
        `${responsiveStorageNameSpace()}layout`
      );
      this.epTheme = layout?.theme;
      this.epThemeColor = newColor;
      if (!layout) return;
      layout.epThemeColor = newColor;
      storage.setItem(`${responsiveStorageNameSpace()}layout`, layout);
    }
  }
});

export function useEpThemeStoreHook() {
  return useEpThemeStore(store);
}
