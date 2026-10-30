using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace EcoNotepad
{
    /// <summary>
    /// Suggests a store for the current item using four strategies:
    /// Nearest / Cheapest / Most Items / Balanced. Stores without coordinates can
    /// be filled in on the fly (X/Y/Z from the in-game /pos or F8 command).
    /// </summary>
    public class StoreSuggestDialog : Form
    {
        public string SelectedStore { get; set; } = "";
        public double SelectedPrice { get; set; } = -1;

        private readonly string _item;
        private readonly List<NoteEntry> _openEntries;
        private readonly double _px, _py, _pz;
        private List<StoreInfo> _stores;

        private readonly ListBox _list = new ListBox();
        private readonly List<SuggestOption> _options = new List<SuggestOption>();

        private readonly FlowLayoutPanel _coordPanel = new FlowLayoutPanel();
        private readonly TextBox _txtX = new TextBox();
        private readonly TextBox _txtY = new TextBox();
        private readonly TextBox _txtZ = new TextBox();
        private StoreInfo _pendingStore;

        private readonly FlowLayoutPanel _addPanel = new FlowLayoutPanel();
        private readonly TextBox _txtAddName = new TextBox();
        private readonly TextBox _txtAddX = new TextBox();
        private readonly TextBox _txtAddY = new TextBox();
        private readonly TextBox _txtAddZ = new TextBox();

        private class SuggestOption
        {
            public string Title;
            public string Reason;
            public StoreInfo Store;
            public double Price = -1;
            public bool Selectable = true;
        }

        public StoreSuggestDialog(string item, List<NoteEntry> openEntries, List<StoreInfo> stores,
            double px, double py, double pz)
        {
            _item = item;
            _openEntries = openEntries ?? new List<NoteEntry>();
            _stores = stores ?? new List<StoreInfo>();
            _px = px;
            _py = py;
            _pz = pz;

            Text = "Suggest Store";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(540, 470);
            Font = new Font("Segoe UI", 9f);
            BuildUi();
            Rebuild();
        }

        private void BuildUi()
        {
            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 6, 8, 4)
            };
            top.Controls.Add(new Label
            {
                Text = "Suggest store for: " + _item,
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold)
            });
            top.Controls.Add(new Label
            {
                Text = "   (用游戏内 /pos 或 F8 查看玩家坐标)",
                AutoSize = true,
                ForeColor = Color.Gray
            });

            _list.Dock = DockStyle.Fill;
            _list.DrawMode = DrawMode.OwnerDrawFixed;
            _list.ItemHeight = 38;
            _list.IntegralHeight = false;
            _list.BorderStyle = BorderStyle.FixedSingle;
            _list.DrawItem += OnDrawItem;
            _list.SelectedIndexChanged += (s, e) => OnSelectionChanged();
            _list.DoubleClick += (s, e) => UseSelection();

            // 底部：坐标录入面板（选中无坐标商店时显示）
            _coordPanel.Dock = DockStyle.Bottom;
            _coordPanel.AutoSize = true;
            _coordPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _coordPanel.Padding = new Padding(8, 4, 8, 4);
            _coordPanel.Visible = false;
            _coordPanel.Controls.Add(new Label
            {
                Text = "该商店未录入坐标，请输入 X/Y/Z：",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            });
            _txtX.Width = 55;
            _txtY.Width = 55;
            _txtZ.Width = 55;
            _coordPanel.Controls.Add(_txtX);
            _coordPanel.Controls.Add(_txtY);
            _coordPanel.Controls.Add(_txtZ);
            var btnSaveCoords = new Button { Text = "Save & Use", Width = 90 };
            btnSaveCoords.Click += (s, e) => SaveCoordsAndUse();
            _coordPanel.Controls.Add(btnSaveCoords);

            // 底部：添加新商店面板
            _addPanel.Dock = DockStyle.Bottom;
            _addPanel.AutoSize = true;
            _addPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _addPanel.Padding = new Padding(8, 4, 8, 4);
            _addPanel.Visible = false;
            _addPanel.Controls.Add(new Label
            {
                Text = "名称",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            });
            _txtAddName.Width = 120;
            _addPanel.Controls.Add(_txtAddName);
            _addPanel.Controls.Add(new Label { Text = "X", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
            _txtAddX.Width = 55;
            _addPanel.Controls.Add(_txtAddX);
            _addPanel.Controls.Add(new Label { Text = "Y", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
            _txtAddY.Width = 55;
            _addPanel.Controls.Add(_txtAddY);
            _addPanel.Controls.Add(new Label { Text = "Z", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
            _txtAddZ.Width = 55;
            _addPanel.Controls.Add(_txtAddZ);
            var btnSaveStore = new Button { Text = "Save store", Width = 90 };
            btnSaveStore.Click += (s, e) => SaveNewStore();
            _addPanel.Controls.Add(btnSaveStore);

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Padding = new Padding(8, 4, 8, 8),
                FlowDirection = FlowDirection.RightToLeft
            };
            var ok = new Button { Text = "Use This Store", Width = 110 };
            ok.Click += (s, e) => UseSelection();
            var cancel = new Button { Text = "Cancel", Width = 80, DialogResult = DialogResult.Cancel };
            var addBtn = new Button { Text = "Add store...", Width = 95 };
            addBtn.Click += (s, e) => ToggleAddPanel();
            btnPanel.Controls.Add(cancel);
            btnPanel.Controls.Add(ok);
            btnPanel.Controls.Add(addBtn);

            Controls.Add(btnPanel);
            Controls.Add(_coordPanel);
            Controls.Add(_addPanel);
            Controls.Add(top);
            Controls.Add(_list);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void Rebuild()
        {
            _options.Clear();
            BuildNearest();
            BuildCheapest();
            BuildMostItems();
            BuildBalanced();

            _list.Items.Clear();
            for (int i = 0; i < _options.Count; i++) _list.Items.Add(i);
            _pendingStore = null;
            _coordPanel.Visible = false;
            if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        }

        private void BuildNearest()
        {
            var withCoords = _stores.Where(s => s.HasCoords).ToList();
            if (withCoords.Count > 0)
            {
                StoreInfo best = withCoords.OrderBy(s => s.DistanceTo(_px, _pz)).First();
                _options.Add(new SuggestOption
                {
                    Title = "Nearest",
                    Reason = best.Name + "  —  平面距离 " + FormatDist(best.DistanceTo(_px, _pz)),
                    Store = best
                });
                return;
            }

            var withoutCoords = _stores.Where(s => !s.HasCoords).ToList();
            if (withoutCoords.Count > 0)
            {
                _options.Add(new SuggestOption
                {
                    Title = "Nearest",
                    Reason = withoutCoords[0].Name + "  —  未录入坐标，请现场录入 X/Y/Z",
                    Store = withoutCoords[0]
                });
                return;
            }

            _options.Add(new SuggestOption { Title = "Nearest", Reason = "尚未配置商店数据（stores.json）", Selectable = false });
        }

        private void BuildCheapest()
        {
            var withPrice = _stores.Where(s => s.GetPrice(_item) >= 0).ToList();
            if (withPrice.Count > 0)
            {
                StoreInfo best = withPrice.OrderBy(s => s.GetPrice(_item)).First();
                double p = best.GetPrice(_item);
                _options.Add(new SuggestOption
                {
                    Title = "Cheapest",
                    Reason = best.Name + "  —  " + _item + " " + p.ToString("0.##", CultureInfo.InvariantCulture) + "c",
                    Store = best,
                    Price = p
                });
                return;
            }

            _options.Add(new SuggestOption
            {
                Title = "Cheapest",
                Reason = "无商店录入 " + _item + " 的价格",
                Selectable = false
            });
        }

        private void BuildMostItems()
        {
            var items = _openEntries
                .Where(e => !string.IsNullOrWhiteSpace(e.Item))
                .Select(e => e.Item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (items.Count == 0)
            {
                _options.Add(new SuggestOption
                {
                    Title = "Most Items",
                    Reason = "购物清单为空（没有 Done=false 的条目）",
                    Selectable = false
                });
                return;
            }

            StoreInfo best = null;
            int bestCount = 0;
            foreach (StoreInfo s in _stores)
            {
                int c = s.CountItems(items);
                if (c > bestCount)
                {
                    bestCount = c;
                    best = s;
                }
            }

            if (best == null)
            {
                _options.Add(new SuggestOption
                {
                    Title = "Most Items",
                    Reason = "尚无商店价格数据（stores.json）",
                    Selectable = false
                });
                return;
            }

            _options.Add(new SuggestOption
            {
                Title = "Most Items",
                Reason = best.Name + "  —  覆盖 " + bestCount + "/" + items.Count + " 个待购物品",
                Store = best,
                Price = best.GetPrice(_item)
            });
        }

        private void BuildBalanced()
        {
            var withCoords = _stores.Where(s => s.HasCoords).ToList();
            if (withCoords.Count == 0)
            {
                var withoutCoords = _stores.Where(s => !s.HasCoords).ToList();
                if (withoutCoords.Count > 0)
                {
                    _options.Add(new SuggestOption
                    {
                        Title = "Balanced",
                        Reason = withoutCoords[0].Name + "  —  未录入坐标，请现场录入 X/Y/Z",
                        Store = withoutCoords[0]
                    });
                }
                else
                {
                    _options.Add(new SuggestOption
                    {
                        Title = "Balanced",
                        Reason = "尚未配置商店数据（stores.json）",
                        Selectable = false
                    });
                }
                return;
            }

            double minPrice = double.MaxValue, maxPrice = double.MinValue;
            foreach (StoreInfo s in _stores)
            {
                double p = s.GetPrice(_item);
                if (p >= 0)
                {
                    if (p < minPrice) minPrice = p;
                    if (p > maxPrice) maxPrice = p;
                }
            }
            bool anyPrice = minPrice <= maxPrice;

            double maxDist = 0;
            foreach (StoreInfo s in withCoords)
            {
                double d = s.DistanceTo(_px, _pz);
                if (d > maxDist) maxDist = d;
            }

            var items = _openEntries
                .Where(e => !string.IsNullOrWhiteSpace(e.Item))
                .Select(e => e.Item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            double maxCover = 0;
            foreach (StoreInfo s in _stores)
            {
                double c = s.CountItems(items);
                if (c > maxCover) maxCover = c;
            }

            StoreInfo best = null;
            double bestScore = -1;
            foreach (StoreInfo s in withCoords)
            {
                double priceScore = 0;
                double p = s.GetPrice(_item);
                if (p >= 0)
                    priceScore = (anyPrice && maxPrice > minPrice) ? (maxPrice - p) / (maxPrice - minPrice) : 1.0;

                double d = s.DistanceTo(_px, _pz);
                double distScore = maxDist > 0 ? 1.0 - d / maxDist : 1.0;

                double coverScore = maxCover > 0 ? s.CountItems(items) / maxCover : 0.0;

                double score = 0.40 * priceScore + 0.35 * distScore + 0.25 * coverScore;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = s;
                }
            }

            if (best == null)
            {
                _options.Add(new SuggestOption { Title = "Balanced", Reason = "无法评分", Selectable = false });
                return;
            }

            _options.Add(new SuggestOption
            {
                Title = "Balanced",
                Reason = best.Name + "  —  综合评分 " + bestScore.ToString("0.00", CultureInfo.InvariantCulture) + "（价格/距离/覆盖加权）",
                Store = best,
                Price = best.GetPrice(_item)
            });
        }

        private void OnSelectionChanged()
        {
            _addPanel.Visible = false;
            if (_list.SelectedIndex < 0 || _list.SelectedIndex >= _options.Count) return;

            SuggestOption o = _options[_list.SelectedIndex];
            if (!o.Selectable || o.Store == null)
            {
                _coordPanel.Visible = false;
                _pendingStore = null;
                return;
            }

            if (o.Store.HasCoords)
            {
                _coordPanel.Visible = false;
                _pendingStore = null;
            }
            else
            {
                _pendingStore = o.Store;
                _coordPanel.Visible = true;
                _txtX.Text = "0";
                _txtY.Text = "0";
                _txtZ.Text = "0";
            }
        }

        private void SaveCoordsAndUse()
        {
            if (_pendingStore == null) return;

            double x, y, z;
            if (!double.TryParse(_txtX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                !double.TryParse(_txtY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                !double.TryParse(_txtZ.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out z))
            {
                MessageBox.Show(this, "请输入有效的 X/Y/Z 数字坐标。", "Suggest Store",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _pendingStore.X = x;
            _pendingStore.Y = y;
            _pendingStore.Z = z;
            _pendingStore.HasCoords = true;
            DataStore.SaveStores(_stores);

            Rebuild();
            for (int i = 0; i < _options.Count; i++)
            {
                if (ReferenceEquals(_options[i].Store, _pendingStore))
                {
                    _list.SelectedIndex = i;
                    break;
                }
            }
            UseSelection();
        }

        private void SaveNewStore()
        {
            string name = _txtAddName.Text.Trim();
            if (name.Length == 0)
            {
                MessageBox.Show(this, "请输入商店名称。", "Suggest Store",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double x, y, z;
            if (!double.TryParse(_txtAddX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                !double.TryParse(_txtAddY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                !double.TryParse(_txtAddZ.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out z))
            {
                MessageBox.Show(this, "请输入有效的 X/Y/Z 数字坐标。", "Suggest Store",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var s = new StoreInfo { Name = name, X = x, Y = y, Z = z, HasCoords = true };
            _stores.Add(s);
            DataStore.SaveStores(_stores);
            _addPanel.Visible = false;
            Rebuild();
        }

        private void ToggleAddPanel()
        {
            _addPanel.Visible = !_addPanel.Visible;
            _coordPanel.Visible = false;
        }

        private void UseSelection()
        {
            if (_list.SelectedIndex < 0 || _list.SelectedIndex >= _options.Count) return;

            SuggestOption o = _options[_list.SelectedIndex];
            if (!o.Selectable)
            {
                MessageBox.Show(this, "该项不可用：" + o.Reason, "Suggest Store",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (o.Store != null && !o.Store.HasCoords)
            {
                MessageBox.Show(this, "该商店未录入坐标，请先在下方录入 X/Y/Z 并保存。", "Suggest Store",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedStore = o.Store != null ? o.Store.Name : "";
            SelectedPrice = o.Price;
            DialogResult = DialogResult.OK;
        }

        private static string FormatDist(double d)
        {
            return d < 1 ? "1m" : ((int)Math.Round(d)).ToString(CultureInfo.InvariantCulture) + "m";
        }

        private void OnDrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= _options.Count) return;

            SuggestOption o = _options[e.Index];
            bool selected = (e.State & DrawItemState.Selected) != 0;
            bool enabled = o.Selectable;

            Color titleColor = !enabled ? Color.Gray
                : selected ? SystemColors.HighlightText : SystemColors.ControlText;
            Color reasonColor = !enabled ? Color.Gray
                : selected ? Color.LightGray : Color.DimGray;

            using (var titleFont = new Font(Font, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, o.Title, titleFont,
                    new Rectangle(e.Bounds.Left + 4, e.Bounds.Top + 3, e.Bounds.Width - 8, 18),
                    titleColor, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(e.Graphics, o.Reason, Font,
                    new Rectangle(e.Bounds.Left + 4, e.Bounds.Top + 21, e.Bounds.Width - 8, 18),
                    reasonColor, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            }

            e.DrawFocusRectangle();
        }
    }
}
