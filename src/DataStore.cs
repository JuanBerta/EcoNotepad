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
        public double PlayerX, PlayerY, PlayerZ;

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
        public static string StoresPath { get { return Path.Combine(DataDir, "stores.json"); } }

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
                s.PlayerX = Dbl(d, "PlayerX", 0);
                s.PlayerY = Dbl(d, "PlayerY", 0);
                s.PlayerZ = Dbl(d, "PlayerZ", 0);
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
                ",\"ClickThrough\":" + (s.ClickThrough ? "true" : "false") +
                ",\"PlayerX\":" + s.PlayerX.ToString("0.###", CultureInfo.InvariantCulture) +
                ",\"PlayerY\":" + s.PlayerY.ToString("0.###", CultureInfo.InvariantCulture) +
                ",\"PlayerZ\":" + s.PlayerZ.ToString("0.###", CultureInfo.InvariantCulture) + "}";
            WriteWithFallback(SettingsPath, json);
        }

        public static List<StoreInfo> LoadStores()
        {
            var list = new List<StoreInfo>();
            try
            {
                if (!File.Exists(StoresPath)) return list;
                object parsed = Json.Parse(File.ReadAllText(StoresPath, Encoding.UTF8));
                var arr = parsed as List<object>;
                if (arr == null) return list;
                foreach (object o in arr)
                {
                    var d = o as Dictionary<string, object>;
                    if (d == null) continue;
                    var s = new StoreInfo();
                    s.Name = Str(d, "Name");
                    s.HasCoords = Bool(d, "HasCoords") || d.ContainsKey("X");
                    s.X = Dbl(d, "X", 0);
                    s.Y = Dbl(d, "Y", 0);
                    s.Z = Dbl(d, "Z", 0);
                    object pv;
                    if (d.TryGetValue("Prices", out pv))
                    {
                        var pd = pv as Dictionary<string, object>;
                        if (pd != null)
                        {
                            foreach (var kv in pd)
                            {
                                if (kv.Value is double) s.Prices[kv.Key] = (double)kv.Value;
                            }
                        }
                    }
                    list.Add(s);
                }
            }
            catch
            {
                // corrupt file -> start fresh
            }
            return list;
        }

        public static void SaveStores(List<StoreInfo> stores)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < stores.Count; i++)
            {
                StoreInfo s = stores[i];
                sb.Append("{\"Name\":").Append(Json.EncodeString(s.Name));
                sb.Append(",\"HasCoords\":").Append(s.HasCoords ? "true" : "false");
                sb.Append(",\"X\":").Append(s.X.ToString("0.###", CultureInfo.InvariantCulture));
                sb.Append(",\"Y\":").Append(s.Y.ToString("0.###", CultureInfo.InvariantCulture));
                sb.Append(",\"Z\":").Append(s.Z.ToString("0.###", CultureInfo.InvariantCulture));
                sb.Append(",\"Prices\":{");
                int j = 0;
                foreach (var kv in s.Prices)
                {
                    if (j > 0) sb.Append(',');
                    sb.Append(Json.EncodeString(kv.Key)).Append(':')
                        .Append(kv.Value.ToString("0.###", CultureInfo.InvariantCulture));
                    j++;
                }
                sb.Append("}}");
                if (i < stores.Count - 1) sb.Append(',');
            }
            sb.Append(']');
            WriteWithFallback(StoresPath, sb.ToString());
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
