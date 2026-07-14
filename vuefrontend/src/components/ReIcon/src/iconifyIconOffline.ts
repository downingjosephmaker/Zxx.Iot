import { h, defineComponent } from "vue";
// 改 @iconify/vue/dist/offline → @iconify/vue：已注册的图标仍走本地（addIcon/addCollection 共享同一注册表），
// 未注册的自动 CDN fallback，避免菜单里配的 ri:* 图标全都要手动预注册
import { Icon as IconifyIcon, addIcon } from "@iconify/vue";

// Iconify Icon在Vue里本地使用（用于内网环境）
export default defineComponent({
  name: "IconifyIconOffline",
  components: { IconifyIcon },
  props: {
    icon: {
      default: null
    }
  },
  render() {
    // 仅当 icon 是有效的 {name, body} 对象时才入注册表；旧 offline 包对参数容忍，新 @iconify/vue 严格要求 name=string
    if (
      typeof this.icon === "object" &&
      this.icon &&
      typeof (this.icon as any).name === "string"
    ) {
      addIcon((this.icon as any).name, this.icon as any);
    }
    const attrs = this.$attrs;
    if (typeof this.icon === "string") {
      return h(
        IconifyIcon,
        {
          icon: this.icon,
          "aria-hidden": false,
          style: attrs?.style
            ? Object.assign(attrs.style, { outline: "none" })
            : { outline: "none" },
          ...attrs
        },
        {
          default: () => []
        }
      );
    } else {
      return h(
        this.icon,
        {
          "aria-hidden": false,
          style: attrs?.style
            ? Object.assign(attrs.style, { outline: "none" })
            : { outline: "none" },
          ...attrs
        },
        {
          default: () => []
        }
      );
    }
  }
});
