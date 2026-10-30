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
            new Regex(@"Crafting\s+\d+\s+(.+?)\s+of\s+\d+", RegexOptions.Compiled);

        private static readonly Regex MaterialRx =
            new Regex(@"(\d+)\s+(.+?)\s+required\s*\((\d+)\s+available",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static RecipeParseResult Parse(List<string> lines)
        {
            var result = new RecipeParseResult();
            string recipeName = null;

            if (lines != null)
            {
                foreach (string line in lines)
                {
                    if (recipeName == null)
                    {
                        Match rm = RecipeNameRx.Match(line);
                        if (rm.Success) recipeName = rm.Groups[1].Value.Trim();
                    }

                    foreach (Match m in MaterialRx.Matches(line))
                    {
                        int required;
                        int available;
                        if (!int.TryParse(m.Groups[1].Value, out required)) continue;
                        if (!int.TryParse(m.Groups[3].Value, out available)) continue;

                        string item = CleanItem(m.Groups[2].Value);
                        if (item.Length == 0) continue;

                        result.Materials.Add(new RecipeMaterial(item, required, available));
                    }
                }
            }

            if (result.Materials.Count == 0)
            {
                result.Success = false;
                result.Error = "未找到 required 行，请确保游戏内悬停提示可见";
                result.RecipeName = recipeName ?? "";
                return result;
            }

            result.Success = true;
            result.RecipeName = string.IsNullOrEmpty(recipeName) ? "Unknown Recipe" : recipeName;
            return result;
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
