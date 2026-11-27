import { execSync } from "node:child_process";
import { defineConfig, type DefaultTheme } from "vitepress";

interface ThemeConfig extends DefaultTheme.Config {
  imageTag: string;
}

const repo = "https://github.com/NullMonkeys/Wisp";

const version = (() => {
  try {
    return execSync("git describe --tags --abbrev=0 --exclude '*-*'", {
      encoding: "utf8",
      stdio: "pipe",
    }).trim();
  } catch {
    return undefined;
  }
})();

export default defineConfig<ThemeConfig>({
  title: "Wisp",
  description:
    "A small, self-hosted Discord bot for temporary voice channels and join roles.",
  lang: "en-US",
  cleanUrls: true,
  lastUpdated: true,
  markdown: {
    container: {
      tipLabel: "Tip",
      infoLabel: "Info",
      noteLabel: "Note",
      importantLabel: "Important",
      warningLabel: "Warning",
      cautionLabel: "Caution",
      dangerLabel: "Danger",
      detailsLabel: "Details",
    },
  },
  sitemap: {
    hostname: "https://wisp.docs.nullmonkeys.com",
  },
  head: [
    [
      "link",
      { rel: "icon", type: "image/png", sizes: "64x64", href: "/favicon.png" },
    ],
    ["link", { rel: "icon", type: "image/svg+xml", href: "/favicon.svg" }],
    ["link", { rel: "apple-touch-icon", href: "/apple-touch-icon.png" }],
  ],

  themeConfig: {
    imageTag: version?.replace(/^v/, "") ?? "latest",

    logo: {
      light: "/wisp-logo-l-nobg.webp",
      dark: "/wisp-logo-d-nobg.webp",
      alt: "Wisp",
    },

    nav: [
      { text: "Guide", link: "/guide/getting-started", activeMatch: "/guide/" },
      {
        text: "Features",
        link: "/features/temp-voice",
        activeMatch: "/features/",
      },
      {
        text: version ?? "Releases",
        items: [
          ...(version
            ? [
                {
                  text: `${version} release notes`,
                  link: `${repo}/releases/tag/${version}`,
                },
              ]
            : []),
          { text: "Changelog", link: `${repo}/releases` },
          { text: "Development", link: "/development" },
        ],
      },
    ],

    sidebar: [
      {
        text: "Guide",
        items: [
          { text: "Getting started", link: "/guide/getting-started" },
          { text: "Configuration", link: "/guide/configuration" },
          { text: "Running several instances", link: "/guide/scaling" },
        ],
      },
      {
        text: "Features",
        items: [
          { text: "Temporary voice channels", link: "/features/temp-voice" },
          { text: "Join roles", link: "/features/join-roles" },
        ],
      },
      {
        text: "Contributing",
        items: [{ text: "Development", link: "/development" }],
      },
    ],

    search: {
      provider: "local",
    },
    editLink: {
      pattern: `${repo}/edit/main/docs/:path`,
      text: "Edit this page on GitHub",
    },
    socialLinks: [{ icon: "github", link: repo }],
    footer: {
      message: "Released under the MIT License.",
    },
  },
});
