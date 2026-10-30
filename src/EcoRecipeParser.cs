using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EcoNotepad
{
    /// <summary>A single material line extracted from the recipe tooltip.</summary>
    public class RecipeMaterial
    {
        public string Item { get; set; }
        public int Required { get; set; }
        public int Available { get; set; }
        public int Missing { get; set; }

        public RecipeMaterial()
        {
        }

        public RecipeMaterial(string item, int required, int available)
        {
            Item = item;
            Required = required;
            Available = available;
            Missing = required - available;
        }
    }

    /// <summary>Result of parsing one OCR recipe scan.</summary>
    public class RecipeParseResult
    {
        public bool Success;
        public string Error;
        public string RecipeName;
        public List<RecipeMaterial> Materials = new List<RecipeMaterial>();
    }

    /// <summary>
    /// Parses OCR text captured from the Eco recipe tooltip panel.
    /// Expected lines:
    ///   Crafting 1 Sweet Jerky of 50.
    ///   · 49 Raw Meat required (1 available in storage).
    /// </summary>
    public static class EcoRecipeParser
    {
        private static readonly Regex RecipeNameRx =
            new Regex(@"Crafting\s+\d+\s+(.+?)\s+of\s+\d+",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 宽松材料行：N <Item> required（OCR 可能把一行切成多行，故先把全部行合并成全文再匹配）
        private static readonly Regex MaterialRx = new Regex(
            @"(\d+)\s+([A-Za-z][A-Za-z \-]*?)\s+required",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // available 数量单独匹配：required 与 available 之间允许更多字符、available 后不要求固定结尾
        private static readonly Regex AvailableRx = new Regex(
            @"(\d+)\s+available",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static RecipeParseResult Parse(List<string> lines)
        {
            var result = new RecipeParseResult();
            // 把 OCR 切碎的行合并为全文：跨行断开的 "required ... (N available" 也能匹配
            string full = lines == null ? "" : string.Join(" ", lines);

            Match rm = RecipeNameRx.Match(full);
            string recipeName = rm.Success ? rm.Groups[1].Value.Trim() : null;

            foreach (Match m in MaterialRx.Matches(full))
            {
                int required;
                if (!int.TryParse(m.Groups[1].Value, out required)) continue;

                string item = CleanItem(m.Groups[2].Value);
                if (item.Length == 0) continue;
                // 跳过配方标题行（如 "Crafting 1 Sweet Jerky of 50."）的伪匹配
                if (item.IndexOf(" of ", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (item.IndexOf("Crafting", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                int available = FindAvailable(full, m.Index + m.Length);
                result.Materials.Add(new RecipeMaterial(item, required, available));
            }

            if (result.Materials.Count == 0)
            {
                result.Success = false;
                result.Error =
                    "未找到 required 行。请确认悬停的是「制作面板/配方提示」（应含 \"For Remaining X Repetitions\" 与 " +
                    "\"N required (M available in storage)\" 行），而不是物品详情卡（Crafted At / Used in 格式没有 required 行）；" +
                    "提示区域也请不要被 EcoNotepad 窗口遮挡。";
                result.RecipeName = recipeName ?? "";
                return result;
            }

            result.Success = true;
            result.RecipeName = string.IsNullOrEmpty(recipeName) ? "Unknown Recipe" : recipeName;
            return result;
        }

        /// <summary>
        /// 在 required 匹配位置之后寻找 available 数量；优先在小窗口内找以避免跨材料错配，
        /// 找不到时再回退到全文第一个 available。
        /// </summary>
        private static int FindAvailable(string text, int start)
        {
            int windowLen = Math.Min(160, text.Length - start);
            if (windowLen > 0)
            {
                Match m = AvailableRx.Match(text, start, windowLen);
                if (m.Success)
                {
                    int v;
                    if (int.TryParse(m.Groups[1].Value, out v)) return v;
                }
            }

            Match fallback = AvailableRx.Match(text);
            if (fallback.Success)
            {
                int v;
                if (int.TryParse(fallback.Groups[1].Value, out v)) return v;
            }
            return 0;
        }

        /// <summary>Keeps only letters / spaces / hyphens and collapses whitespace.</summary>
        private static string CleanItem(string raw)
        {
            if (raw == null) return "";
            string s = Regex.Replace(raw, @"[^A-Za-z \-]", "");
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return s;
        }
    }
}
