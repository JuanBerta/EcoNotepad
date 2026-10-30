using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace EcoNotepad
{
    public class AppSettings
    {
        public int X, Y, W, H;
        public double Opacity = 1.0;
        public bool ClickThrough;

        public bool HasBounds { get { return W > 0 && H > 0; } }
    }

    public static class DataStore
    {
        private static string _dataDir;

        private static string DataDir
        {
            get
            {
                if (_dataDir == null)
                {
                    string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    if (IsWritable(exeDir))
                    {
                        _dataDir = exeDir;
                    }
                    else
                    {
                        // exe inside a read-only location (e.g. Program Files): fall back to %APPDATA%
                        _dataDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "EcoNotepad");
                        Directory.CreateDirectory(_dataDir);
                    }
                }
                return _dataDir;
            }
        }

        private static bool IsWritable(string dir)
        {
            try
            {
                string probe = Path.Combine(dir, ".write_test");
                File.WriteAllText(probe, "x");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string NotesPath { get { return Path.Combine(DataDir, "notes.json"); } }
        public static string SettingsPath { get { return Path.Combine(DataDir, "settings.json"); } }

        public static List<NoteEntry> LoadNotes()
        {
            var list = new List<NoteEntry>();
            try
            {
                if (!File.Exists(NotesPath)) return list;
                object parsed = Json.Parse(File.ReadAllText(NotesPath, Encoding.UTF8));
                var arr = parsed as List<object>;
                if (arr == null) return list;
                foreach (object o in arr)
                {
                    var d = o as Dictionary<string, object>;
                    if (d == null) continue;
                    var e = new NoteEntry();
                    e.Id = Str(d, "Id");
                    if (string.IsNullOrEmpty(e.Id)) e.Id = Guid.NewGuid().ToString("N");
                    e.Item = Str(d, "Item");
                    e.Price = Str(d, "Price");
                    e.Store = Str(d, "Store");
                    e.Qty = Str(d, "Qty");
                    e.Tags = Str(d, "Tags");
                    e.Notes = Str(d, "Notes");
                    e.Done = Bool(d, "Done");
                    list.Add(e);
                }
            }
            catch
            {
                // corrupt file -> start fresh
            }
            return list;
        }

        public static void SaveNotes(List<NoteEntry> notes)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < notes.Count; i++)
            {
                NoteEntry e = notes[i];
                sb.Append("{\"Id\":").Append(Json.EncodeString(e.Id));
                sb.Append(",\"Item\":").Append(Json.EncodeString(e.Item));
                sb.Append(",\"Price\":").Append(Json.EncodeString(e.Price));
                sb.Append(",\"Store\":").Append(Json.EncodeString(e.Store));
                sb.Append(",\"Qty\":").Append(Json.EncodeString(e.Qty));
                sb.Append(",\"Tags\":").Append(Json.EncodeString(e.Tags));
                sb.Append(",\"Notes\":").Append(Json.EncodeString(e.Notes));
                sb.Append(",\"Done\":").Append(e.Done ? "true" : "false").Append('}');
                if (i < notes.Count - 1) sb.Append(',');
            }
            sb.Append(']');
            WriteWithFallback(NotesPath, sb.ToString());
        }

        public static AppSettings LoadSettings()
        {
            var s = new AppSettings();
            try
            {
                if (!File.Exists(SettingsPath)) return s;
                object parsed = Json.Parse(File.ReadAllText(SettingsPath, Encoding.UTF8));
                var d = parsed as Dictionary<string, object>;
                if (d == null) return s;
                s.X = Int(d, "X");
                s.Y = Int(d, "Y");
                s.W = Int(d, "W");
                s.H = Int(d, "H");
                s.Opacity = Dbl(d, "Opacity", 1.0);
                if (s.Opacity < 0.5 || s.Opacity > 1.0) s.Opacity = 1.0;
                s.ClickThrough = Bool(d, "ClickThrough");
            }
            catch
            {
            }
            return s;
        }

        public static void SaveSettings(AppSettings s)
        {
            string json = "{\"X\":" + s.X + ",\"Y\":" + s.Y + ",\"W\":" + s.W + ",\"H\":" + s.H +
                ",\"Opacity\":" + s.Opacity.ToString("0.00##", CultureInfo.InvariantCulture) +
                ",\"ClickThrough\":" + (s.ClickThrough ? "true" : "false") + "}";
            WriteWithFallback(SettingsPath, json);
        }

        private static void WriteWithFallback(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content, Encoding.UTF8);
            }
            catch (UnauthorizedAccessException)
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EcoNotepad");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, Path.GetFileName(path)), content, Encoding.UTF8);
            }
        }

        private static string Str(Dictionary<string, object> d, string key)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v) && v != null) return v.ToString();
            return "";
        }

        private static bool Bool(Dictionary<string, object> d, string key)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v) && v is bool) return (bool)v;
            return false;
        }

        private static int Int(Dictionary<string, object> d, string key)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v) && v is double) return (int)Math.Round((double)v);
            return 0;
        }

        private static double Dbl(Dictionary<string, object> d, string key, double def)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v) && v is double) return (double)v;
            return def;
        }
    }
}
