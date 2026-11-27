import { h } from "vue";
import type { Theme } from "vitepress";
import DefaultTheme from "vitepress/theme-without-fonts";
import "@fontsource-variable/geist";
import "@fontsource-variable/geist-mono";
import "./style.css";

export default {
  extends: DefaultTheme,
  Layout: () => {
    return h(DefaultTheme.Layout, null);
  },
  enhanceApp({ app, siteData }) {
    Object.defineProperty(app.config.globalProperties, "$theme", {
      get: () => siteData.value.themeConfig,
    });
  },
} satisfies Theme;
