import type { ScadaComponent } from "../types";

/**
 * 图片类型定义
 */
export interface ImageConfig {
  name: string;
  title: string;
  icon: string;
  category: "upload" | "url" | "symbol" | "background";
  defaultProps: Partial<ScadaComponent>;
  uploadConfig?: {
    accept: string;
    maxSize: number; // bytes
    multiple: boolean;
  };
}

/**
 * 图片组件注册表
 */
export const imageComponents: ImageConfig[] = [
  // 图片上传
  {
    name: "image-upload",
    title: "图片上传",
    icon: "ep:picture",
    category: "upload",
    defaultProps: {
      type: "image-upload",
      width: 200,
      height: 150,
      properties: {
        src: "",
        alt: "图片",
        fit: "contain", // contain, cover, fill, scale-down, none
        alignment: "center",
        borderRadius: 0,
        opacity: 1,
        filter: "",
        placeholder: "点击上传图片",
        allowReplace: true,
        showPlaceholder: true,
        backgroundColor: "#f5f7fa",
        borderColor: "#dcdfe6",
        borderWidth: 1,
        borderStyle: "dashed"
      }
    },
    uploadConfig: {
      accept: "image/*",
      maxSize: 5 * 1024 * 1024, // 5MB
      multiple: false
    }
  },

  // URL图片
  {
    name: "image-url",
    title: "URL图片",
    icon: "ep:link",
    category: "url",
    defaultProps: {
      type: "image-url",
      width: 200,
      height: 150,
      properties: {
        src: "",
        alt: "图片",
        fit: "contain",
        alignment: "center",
        borderRadius: 0,
        opacity: 1,
        filter: "",
        crossOrigin: "anonymous",
        loading: "lazy",
        placeholder: "请输入图片URL",
        fallbackImage: "",
        showError: true,
        backgroundColor: "#f5f7fa",
        borderColor: "#dcdfe6",
        borderWidth: 1
      }
    }
  },

  // 符号图标
  {
    name: "image-symbol",
    title: "符号图标",
    icon: "ep:medal",
    category: "symbol",
    defaultProps: {
      type: "image-symbol",
      width: 64,
      height: 64,
      properties: {
        symbolType: "warning", // warning, error, success, info, question
        color: "#409eff",
        backgroundColor: "transparent",
        borderRadius: 50,
        opacity: 1,
        size: "medium", // small, medium, large
        style: "filled", // filled, outlined
        animation: "none", // none, pulse, spin, shake
        showBadge: false,
        badgeText: "",
        badgeColor: "#f56c6c"
      }
    }
  },

  // 背景图片
  {
    name: "image-background",
    title: "背景图片",
    icon: "ep:picture-filled",
    category: "background",
    defaultProps: {
      type: "image-background",
      width: 400,
      height: 300,
      properties: {
        src: "",
        fit: "cover",
        position: "center center",
        repeat: "no-repeat", // repeat, repeat-x, repeat-y, no-repeat
        attachment: "scroll", // scroll, fixed, local
        opacity: 1,
        overlay: false,
        overlayColor: "rgba(0,0,0,0.3)",
        parallax: false,
        parallaxSpeed: 0.5,
        filter: "",
        blendMode: "normal"
      }
    }
  }
];

/**
 * 图片组件管理器
 */
export class ImageComponentManager {
  private uploadedImages: Map<string, string> = new Map(); // componentId -> base64
  private imageCache: Map<string, HTMLImageElement> = new Map(); // url -> image

  /**
   * 创建图片组件实例
   */
  createImageComponent(
    component: ScadaComponent,
    container: HTMLElement
  ): HTMLElement {
    const config = imageComponents.find(c => c.name === component.type);
    if (!config) {
      console.warn(`Unknown image component type: ${component.type}`);
      return this.createErrorElement("未知图片类型");
    }

    const element = document.createElement("div");
    element.className = `image-component ${component.type}`;
    element.style.cssText = `
      width: 100%;
      height: 100%;
      position: relative;
      overflow: hidden;
      display: flex;
      align-items: center;
      justify-content: center;
    `;

    this.renderImageComponent(element, config, component);
    container.appendChild(element);

    return element;
  }

  /**
   * 渲染图片组件
   */
  private renderImageComponent(
    element: HTMLElement,
    config: ImageConfig,
    component: ScadaComponent
  ) {
    const properties = JSON.parse(component.properties || "{}");

    switch (config.category) {
      case "upload":
        this.renderUploadImage(element, config, properties, component.id);
        break;
      case "url":
        this.renderUrlImage(element, properties);
        break;
      case "symbol":
        this.renderSymbolImage(element, properties);
        break;
      case "background":
        this.renderBackgroundImage(element, properties);
        break;
    }
  }

  /**
   * 渲染上传图片组件
   */
  private renderUploadImage(
    element: HTMLElement,
    config: ImageConfig,
    properties: any,
    componentId: string
  ) {
    const uploadedSrc = this.uploadedImages.get(componentId) || properties.src;

    if (uploadedSrc) {
      // 显示已上传的图片
      const img = document.createElement("img");
      img.src = uploadedSrc;
      img.alt = properties.alt || "图片";
      img.style.cssText = `
        max-width: 100%;
        max-height: 100%;
        object-fit: ${properties.fit || "contain"};
        border-radius: ${properties.borderRadius || 0}px;
        opacity: ${properties.opacity || 1};
        filter: ${properties.filter || ""};
      `;

      if (properties.allowReplace) {
        img.style.cursor = "pointer";
        img.addEventListener("click", () =>
          this.handleImageUpload(componentId, config)
        );
      }

      element.appendChild(img);
    } else {
      // 显示上传占位符
      this.renderUploadPlaceholder(element, config, properties, componentId);
    }
  }

  /**
   * 渲染上传占位符
   */
  private renderUploadPlaceholder(
    element: HTMLElement,
    config: ImageConfig,
    properties: any,
    componentId: string
  ) {
    element.style.cssText += `
      background-color: ${properties.backgroundColor || "#f5f7fa"};
      border: ${properties.borderWidth || 1}px ${properties.borderStyle || "dashed"} ${properties.borderColor || "#dcdfe6"};
      cursor: pointer;
      transition: all 0.3s ease;
    `;

    const placeholder = document.createElement("div");
    placeholder.style.cssText = `
      text-align: center;
      color: #909399;
      font-size: 14px;
      padding: 20px;
    `;

    placeholder.innerHTML = `
      <div style="margin-bottom: 10px;">
        <i class="ep-upload" style="font-size: 24px;"></i>
      </div>
      <div>${properties.placeholder || "点击上传图片"}</div>
      <div style="font-size: 12px; margin-top: 5px; color: #c0c4cc;">
        支持 JPG、PNG、GIF 格式，大小不超过 5MB
      </div>
    `;

    element.appendChild(placeholder);

    // 添加点击上传事件
    element.addEventListener("click", () =>
      this.handleImageUpload(componentId, config)
    );

    // 添加拖拽上传支持
    this.setupDragAndDrop(element, componentId, config);

    // 添加悬停效果
    element.addEventListener("mouseenter", () => {
      element.style.borderColor = "#409eff";
      element.style.backgroundColor = "#f0f9ff";
    });

    element.addEventListener("mouseleave", () => {
      element.style.borderColor = properties.borderColor || "#dcdfe6";
      element.style.backgroundColor = properties.backgroundColor || "#f5f7fa";
    });
  }

  /**
   * 渲染URL图片
   */
  private renderUrlImage(element: HTMLElement, properties: any) {
    if (properties.src) {
      const img = document.createElement("img");
      img.src = properties.src;
      img.alt = properties.alt || "图片";
      img.crossOrigin = properties.crossOrigin || "anonymous";
      img.loading = properties.loading || "lazy";

      img.style.cssText = `
        max-width: 100%;
        max-height: 100%;
        object-fit: ${properties.fit || "contain"};
        border-radius: ${properties.borderRadius || 0}px;
        opacity: ${properties.opacity || 1};
        filter: ${properties.filter || ""};
      `;

      // 处理加载错误
      img.onerror = () => {
        if (properties.fallbackImage) {
          img.src = properties.fallbackImage;
        } else if (properties.showError) {
          element.innerHTML = this.createErrorElement("图片加载失败").outerHTML;
        }
      };

      element.appendChild(img);
    } else {
      element.innerHTML = `
        <div style="color: #909399; font-size: 14px; text-align: center;">
          ${properties.placeholder || "请输入图片URL"}
        </div>
      `;
    }
  }

  /**
   * 渲染符号图标
   */
  private renderSymbolImage(element: HTMLElement, properties: any) {
    const symbolMap = {
      warning: "⚠️",
      error: "❌",
      success: "✅",
      info: "ℹ️",
      question: "❓"
    };

    const symbol =
      symbolMap[properties.symbolType as keyof typeof symbolMap] || "❓";
    const sizeMap = { small: "24px", medium: "32px", large: "48px" };
    const size = sizeMap[properties.size as keyof typeof sizeMap] || "32px";

    element.style.cssText += `
      background-color: ${properties.backgroundColor || "transparent"};
      border-radius: ${properties.borderRadius || 0}px;
      opacity: ${properties.opacity || 1};
    `;

    const symbolEl = document.createElement("div");
    symbolEl.style.cssText = `
      font-size: ${size};
      color: ${properties.color || "#409eff"};
      position: relative;
      display: inline-block;
    `;

    symbolEl.textContent = symbol;

    // 添加动画
    if (properties.animation && properties.animation !== "none") {
      symbolEl.style.animation = this.getAnimationCSS(properties.animation);
    }

    // 添加徽章
    if (properties.showBadge && properties.badgeText) {
      const badge = document.createElement("div");
      badge.style.cssText = `
        position: absolute;
        top: -5px;
        right: -5px;
        background: ${properties.badgeColor || "#f56c6c"};
        color: white;
        border-radius: 10px;
        padding: 2px 6px;
        font-size: 12px;
        min-width: 18px;
        text-align: center;
      `;
      badge.textContent = properties.badgeText;
      symbolEl.appendChild(badge);
    }

    element.appendChild(symbolEl);
  }

  /**
   * 渲染背景图片
   */
  private renderBackgroundImage(element: HTMLElement, properties: any) {
    if (properties.src) {
      element.style.cssText += `
        background-image: url('${properties.src}');
        background-size: ${properties.fit || "cover"};
        background-position: ${properties.position || "center center"};
        background-repeat: ${properties.repeat || "no-repeat"};
        background-attachment: ${properties.attachment || "scroll"};
        opacity: ${properties.opacity || 1};
        filter: ${properties.filter || ""};
        mix-blend-mode: ${properties.blendMode || "normal"};
      `;

      // 添加遮罩层
      if (properties.overlay) {
        const overlay = document.createElement("div");
        overlay.style.cssText = `
          position: absolute;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          background: ${properties.overlayColor || "rgba(0,0,0,0.3)"};
          pointer-events: none;
        `;
        element.appendChild(overlay);
      }
    } else {
      element.innerHTML = `
        <div style="color: #909399; font-size: 14px; text-align: center;">
          背景图片未设置
        </div>
      `;
    }
  }

  /**
   * 处理图片上传
   */
  private handleImageUpload(componentId: string, config: ImageConfig) {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = config.uploadConfig?.accept || "image/*";
    input.multiple = config.uploadConfig?.multiple || false;

    input.onchange = event => {
      const files = (event.target as HTMLInputElement).files;
      if (files && files.length > 0) {
        this.processUploadedFile(files[0], componentId, config);
      }
    };

    input.click();
  }

  /**
   * 处理上传的文件
   */
  private processUploadedFile(
    file: File,
    componentId: string,
    config: ImageConfig
  ) {
    // 检查文件大小
    const maxSize = config.uploadConfig?.maxSize || 5 * 1024 * 1024;
    if (file.size > maxSize) {
      alert(`文件大小不能超过 ${Math.round(maxSize / 1024 / 1024)}MB`);
      return;
    }

    // 检查文件类型
    if (!file.type.startsWith("image/")) {
      alert("请选择图片文件");
      return;
    }

    // 读取文件为base64
    const reader = new FileReader();
    reader.onload = e => {
      const base64 = e.target?.result as string;
      this.uploadedImages.set(componentId, base64);

      // 触发组件更新
      this.updateImageComponent(componentId, { src: base64 });

      // 触发上传完成事件
      this.onImageUploaded?.(componentId, file, base64);
    };

    reader.readAsDataURL(file);
  }

  /**
   * 设置拖拽上传
   */
  private setupDragAndDrop(
    element: HTMLElement,
    componentId: string,
    config: ImageConfig
  ) {
    element.addEventListener("dragover", e => {
      e.preventDefault();
      element.style.borderColor = "#409eff";
      element.style.backgroundColor = "#f0f9ff";
    });

    element.addEventListener("dragleave", e => {
      e.preventDefault();
      element.style.borderColor = "#dcdfe6";
      element.style.backgroundColor = "#f5f7fa";
    });

    element.addEventListener("drop", e => {
      e.preventDefault();
      element.style.borderColor = "#dcdfe6";
      element.style.backgroundColor = "#f5f7fa";

      const files = e.dataTransfer?.files;
      if (files && files.length > 0) {
        this.processUploadedFile(files[0], componentId, config);
      }
    });
  }

  /**
   * 获取动画CSS
   */
  private getAnimationCSS(animation: string): string {
    const animations = {
      pulse: "pulse 2s infinite",
      spin: "spin 2s linear infinite",
      shake: "shake 0.5s ease-in-out infinite"
    };

    return animations[animation as keyof typeof animations] || "";
  }

  /**
   * 创建错误元素
   */
  private createErrorElement(message: string): HTMLElement {
    const errorEl = document.createElement("div");
    errorEl.style.cssText = `
      color: #f56c6c;
      font-size: 14px;
      text-align: center;
      padding: 20px;
    `;
    errorEl.innerHTML = `
      <div style="margin-bottom: 5px;">❌</div>
      <div>${message}</div>
    `;
    return errorEl;
  }

  /**
   * 更新图片组件
   */
  updateImageComponent(componentId: string, newProperties: any) {
    // 这里应该触发组件重新渲染
    // 具体实现依赖于组件更新机制
    console.log("Updating image component:", componentId, newProperties);
  }

  /**
   * 获取上传的图片
   */
  getUploadedImage(componentId: string): string | undefined {
    return this.uploadedImages.get(componentId);
  }

  /**
   * 删除上传的图片
   */
  removeUploadedImage(componentId: string) {
    this.uploadedImages.delete(componentId);
  }

  /**
   * 清理缓存
   */
  clearCache() {
    this.imageCache.clear();
  }

  /**
   * 预加载图片
   */
  preloadImage(url: string): Promise<HTMLImageElement> {
    return new Promise((resolve, reject) => {
      if (this.imageCache.has(url)) {
        resolve(this.imageCache.get(url)!);
        return;
      }

      const img = new Image();
      img.onload = () => {
        this.imageCache.set(url, img);
        resolve(img);
      };
      img.onerror = reject;
      img.src = url;
    });
  }

  /**
   * 初始化图片组件管理器
   */
  initialize(container?: HTMLElement) {
    console.log("图片组件管理器已初始化");
  }

  /**
   * 清理资源
   */
  destroy() {
    this.uploadedImages.clear();
    this.clearCache();
  }

  // 事件回调
  onImageUploaded?: (componentId: string, file: File, base64: string) => void;
  onImageError?: (componentId: string, error: string) => void;
}
