using System;
using UnityEngine;

namespace NOIChaos
{
    public static class FontLibrary
    {
        private static Font cachedFont;
        private static Font cachedMonospaceFont;

        public static Font GetChineseFont()
        {
            if (cachedFont != null)
            {
                return cachedFont;
            }

            string[] preferredNames =
            {
                "PingFang SC",
                "PingFangSC-Regular",
                "Hiragino Sans GB",
                "Heiti SC",
                "Microsoft YaHei",
                "Microsoft YaHei UI",
                "Noto Sans CJK SC",
                "Source Han Sans SC",
                "Arial Unicode MS"
            };

            string[] installed = Font.GetOSInstalledFontNames();
            for (int i = 0; i < preferredNames.Length; i++)
            {
                for (int j = 0; j < installed.Length; j++)
                {
                    if (string.Equals(installed[j], preferredNames[i], StringComparison.OrdinalIgnoreCase))
                    {
                        cachedFont = Font.CreateDynamicFontFromOSFont(installed[j], 44);
                        return cachedFont;
                    }
                }
            }

            cachedFont = Font.CreateDynamicFontFromOSFont(installed, 44);
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return cachedFont;
        }

        public static Font GetMonospaceFont()
        {
            if (cachedMonospaceFont != null)
            {
                return cachedMonospaceFont;
            }

            string[] preferredNames = { "Menlo", "SF Mono", "Monaco", "Consolas", "Courier New" };
            string[] installed = Font.GetOSInstalledFontNames();
            for (int i = 0; i < preferredNames.Length; i++)
            {
                for (int j = 0; j < installed.Length; j++)
                {
                    if (string.Equals(installed[j], preferredNames[i], StringComparison.OrdinalIgnoreCase))
                    {
                        cachedMonospaceFont = Font.CreateDynamicFontFromOSFont(installed[j], 18);
                        return cachedMonospaceFont;
                    }
                }
            }

            cachedMonospaceFont = GetChineseFont();
            return cachedMonospaceFont;
        }
    }
}
