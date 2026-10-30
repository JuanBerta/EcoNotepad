using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EcoNotepad
{
    public class MainForm : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private readonly List<NoteEntry> _entries = new List<NoteEntry>();
        private BindingList<NoteEntry> _view;
        private DataGridView _grid;
        private TextBox _searchBox;
        private Button _btnAdd;
        private Button _btnClickThrough;
        private Button _btnOpacity;
        private Button _btnHide;
        private Button _btnAddMissing;
        private Button _btnPlanRecipe;
        private Label _lblHotkeys;
        private NotifyIcon _tray;
        private HotkeyManager _hotkeys;
        private AppSettings _settings;
        private double _opacityValue = 1.0;
        private bool _clickThrough;
        private bool _suppressSave;
        private int _doneColIndex = -1;

        public MainForm()
        {
            _settings = DataStore.LoadSettings();
            Icon = LoadAppIcon();
            BuildUi();
            ApplySettings();
            LoadEntries();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyExStyles();
            _hotkeys = new HotkeyManager(Handle);
            if (!_hotkeys.Register())
            {
                _lblHotkeys.Text =
                    "Hotkeys unavailable (already in use?)  -  right-click a row for edit/delete";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveData();
            if (WindowState == FormWindowState.Normal) SaveSettingsNow();
            if (_hotkeys != null) _hotkeys.Dispose();
            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
            }
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HotkeyManager.IdToggleVisible)
                {
                    ToggleVisible();
                    return;
                }
                if (id == HotkeyManager.IdToggleClickThrough)
                {
                    ToggleClickThrough();
                    return;
                }
                if (id == HotkeyManager.IdScan)
                {
                    OnScan("Recipe Scan");
                    return;
                }
            }
            base.WndProc(ref m);
        }

        private void BuildUi()
        {
            Text = "EcoNotepad";
            Font = new Font("Segoe UI", 9f);
            // 最小尺寸需容纳：顶部按钮换行后全部可见、7 列表头完整、底部热键提示完整换行
            MinimumSize = new Size(460, 400);
            TopMost = true;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.Sizable;

            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(4, 3, 4, 3)
            };

            _searchBox = new TextBox { Width = 150 };
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            _btnAdd = new Button { Text = "Add", Width = 60 };
            _btnAdd.Click += (s, e) => OnAdd();

            _btnAddMissing = new Button { Text = "Add Missing", Width = 95 };
            _btnAddMissing.Click += (s, e) => OnScan("Add Missing");

            _btnPlanRecipe = new Button { Text = "Plan Recipe", Width = 95 };
            _btnPlanRecipe.Click += (s, e) => OnScan("Plan Recipe");

            _btnClickThrough = new Button { Width = 130 };
            _btnClickThrough.Click += (s, e) => ToggleClickThrough();

            _btnOpacity = new Button { Width = 90 };
            _btnOpacity.Click += (s, e) => CycleOpacity();

            _btnHide = new Button { Text = "Hide", Width = 50 };
            _btnHide.Click += (s, e) => Hide();

            top.Controls.Add(_searchBox);
            top.Controls.Add(_btnAdd);
            top.Controls.Add(_btnAddMissing);
            top.Controls.Add(_btnPlanRecipe);
            top.Controls.Add(_btnClickThrough);
            top.Controls.Add(_btnOpacity);
            top.Controls.Add(_btnHide);

            var bottomPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(4, 2, 4, 2)
            };

            _lblHotkeys = new Label
            {
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                MaximumSize = new Size(400, 0),
                Text = "Ctrl+Shift+N show/hide   Ctrl+Shift+T click-through   Ctrl+Shift+F9 scan recipe   Right-click row = edit/delete"
            };
            bottomPanel.Controls.Add(_lblHotkeys);

            // 窗口宽度变化时同步更新底部提示的换行宽度上限，保证完整可见
            Resize += (s, e) =>
            {
                _lblHotkeys.MaximumSize = new Size(Math.Max(120, ClientSize.Width - 16), 0);
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
            };

            AddTextColumn("Item", 60F, 100);
            AddTextColumn("Price", 30F, 45);
            AddTextColumn("Store", 45F, 50);
            AddTextColumn("Qty", 20F, 40);
            AddTextColumn("Tags", 40F, 50);
            AddTextColumn("Notes", 60F, 60);
            var done = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Done",
                HeaderText = "Done",
                FillWeight = 18F,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 40
            };
            _doneColIndex = _grid.Columns.Add(done);

            _grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_grid.IsCurrentCellDirty)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.CellValueChanged += OnCellValueChanged;
            _grid.CellEndEdit += (s, e) => SaveData();
            _grid.CellFormatting += OnCellFormatting;
            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex != _doneColIndex) EditSelected();
            };
            _grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete)
                {
                    DeleteSelected();
                    e.Handled = true;
                }
            };
            _grid.CellMouseDown += OnGridCellMouseDown;

            var menu = new ContextMenuStrip();
            menu.Items.Add("Edit...", null, (s, e) => EditSelected());
            menu.Items.Add("Delete", null, (s, e) => DeleteSelected());
            _grid.ContextMenuStrip = menu;

            Controls.Add(_grid);
            Controls.Add(bottomPanel);
            Controls.Add(top);

            _tray = new NotifyIcon();
            _tray.Icon = LoadAppIcon();
            _tray.Text = "EcoNotepad - market notes overlay";
            _tray.Visible = true;
            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show / hide", null, (s, e) => ToggleVisible());
            trayMenu.Items.Add("Exit", null, (s, e) => Close());
            _tray.ContextMenuStrip = trayMenu;
            _tray.DoubleClick += (s, e) => ToggleVisible();

            UpdateClickThroughButton();
            UpdateOpacityButton();
        }

        private static Icon LoadAppIcon()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            if (File.Exists(path))
            {
                return new Icon(path);
            }
            try
            {
                var exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                return exeIcon ?? SystemIcons.Application;
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private void AddTextColumn(string prop, float fillWeight, int minWidth)
        {
            var c = new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop,
                HeaderText = prop,
                FillWeight = fillWeight,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = minWidth
            };
            _grid.Columns.Add(c);
        }

        private void ApplySettings()
        {
            if (_settings.HasBounds &&
                IsOnScreen(new Rectangle(_settings.X, _settings.Y, _settings.W, _settings.H)))
            {
                Bounds = new Rectangle(_settings.X, _settings.Y, _settings.W, _settings.H);
            }
            else
            {
                Size = new Size(480, 540);
                var wa = Screen.PrimaryScreen.WorkingArea;
                Location = new Point(wa.Right - Width - 20, wa.Bottom - Height - 60);
            }

            _clickThrough = _settings.ClickThrough;
            _opacityValue = _settings.Opacity;
            Opacity = _opacityValue;
        }

        private static bool IsOnScreen(Rectangle r)
        {
            foreach (Screen s in Screen.AllScreens)
            {
                if (s.WorkingArea.IntersectsWith(r)) return true;
            }
            return false;
        }

        private void LoadEntries()
        {
            _suppressSave = true;
            _entries.Clear();
            _entries.AddRange(DataStore.LoadNotes());
            ApplyFilter();
            _suppressSave = false;
        }

        private void ApplyFilter()
        {
            string q = _searchBox.Text.Trim();
            List<NoteEntry> src = string.IsNullOrEmpty(q)
                ? _entries
                : _entries.Where(e =>
                    Contains(e.Item, q) || Contains(e.Store, q) || Contains(e.Tags, q) ||
                    Contains(e.Notes, q) || Contains(e.Price, q) || Contains(e.Qty, q)).ToList();
            _view = new BindingList<NoteEntry>(src);
            _grid.DataSource = _view;
        }

        private static bool Contains(string s, string q)
        {
            return s != null && s.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnAdd()
        {
            using (var dlg = new EntryDialog(null, _entries))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Entry != null)
                {
                    _entries.Add(dlg.Entry);
                    ApplyFilter();
                    SaveData();
                    SelectEntry(dlg.Entry);
                }
            }
        }

        private void OnScan(string mode)
        {
            try
            {
                List<string> lines = OcrService.RecognizeScreen(this);
                RecipeParseResult parsed = EcoRecipeParser.Parse(lines);
                if (!parsed.Success)
                {
                    MessageBox.Show(this, parsed.Error, mode,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                using (var dlg = new ScanDialog(parsed.RecipeName, parsed.Materials, mode))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    List<RecipeMaterial> toAdd = dlg.ToAdd;
                    if (toAdd == null || toAdd.Count == 0) return;
                    NoteEntry last = null;
                    foreach (RecipeMaterial m in toAdd)
                    {
                        var e = new NoteEntry
                        {
                            Item = m.Item,
                            Qty = m.Missing.ToString(),
                            Tags = "OCR",
                            Notes = "auto from recipe scan",
                            Store = ""
                        };
                        _entries.Add(e);
                        last = e;
                    }
                    ApplyFilter();
                    SaveData();
                    if (last != null) SelectEntry(last);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "OCR scan failed:\n" + ex.Message, mode,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditSelected()
        {
            NoteEntry e = CurrentEntry();
            if (e == null) return;
            using (var dlg = new EntryDialog(e, _entries))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Entry != null)
                {
                    _grid.Refresh();
                    SaveData();
                }
            }
        }

        private void DeleteSelected()
        {
            NoteEntry e = CurrentEntry();
            if (e == null) return;
            _entries.Remove(e);
            ApplyFilter();
            SaveData();
        }

        private NoteEntry CurrentEntry()
        {
            if (_grid.CurrentRow == null) return null;
            return _grid.CurrentRow.DataBoundItem as NoteEntry;
        }

        private void SelectEntry(NoteEntry e)
        {
            for (int i = 0; i < _view.Count; i++)
            {
                if (ReferenceEquals(_view[i], e))
                {
                    if (_grid.Rows.Count > i)
                    {
                        _grid.ClearSelection();
                        _grid.Rows[i].Selected = true;
                        _grid.CurrentCell = _grid.Rows[i].Cells[1];
                        _grid.FirstDisplayedScrollingRowIndex = i;
                    }
                    break;
                }
            }
        }

        private void SaveData()
        {
            if (_suppressSave) return;
            DataStore.SaveNotes(_entries);
        }

        private void SaveSettingsNow()
        {
            _settings.X = Bounds.X;
            _settings.Y = Bounds.Y;
            _settings.W = Bounds.Width;
            _settings.H = Bounds.Height;
            _settings.Opacity = _opacityValue;
            _settings.ClickThrough = _clickThrough;
            DataStore.SaveSettings(_settings);
        }

        private void OnCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.ColumnIndex == _doneColIndex) SaveData();
        }

        private void OnCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = _grid.Rows[e.RowIndex];
            var ne = row.DataBoundItem as NoteEntry;
            if (ne != null && ne.Done)
            {
                e.CellStyle.ForeColor = Color.Gray;
            }
        }

        private void OnGridCellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                _grid.ClearSelection();
                _grid.Rows[e.RowIndex].Selected = true;
                _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[1];
            }
        }

        private void ToggleVisible()
        {
            if (Visible)
            {
                Hide();
            }
            else
            {
                // make it interactive when you summon it
                if (_clickThrough) ToggleClickThrough();
                Show();
                TopMost = true;
                Activate();
            }
        }

        private void ToggleClickThrough()
        {
            _clickThrough = !_clickThrough;
            ApplyExStyles();
            UpdateClickThroughButton();
            SaveSettingsNow();
        }

        private void UpdateClickThroughButton()
        {
            _btnClickThrough.Text = _clickThrough ? "Click-through: ON" : "Click-through: OFF";
        }

        private void ApplyExStyles()
        {
            if (!IsHandleCreated) return;
            int ex = GetWindowLong(Handle, GWL_EXSTYLE);
            if (_clickThrough)
                ex |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
            else
                ex &= ~WS_EX_TRANSPARENT;
            SetWindowLong(Handle, GWL_EXSTYLE, ex);
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }

        private void CycleOpacity()
        {
            if (_opacityValue >= 1.0) _opacityValue = 0.85;
            else if (_opacityValue >= 0.85) _opacityValue = 0.65;
            else _opacityValue = 1.0;
            Opacity = _opacityValue;
            UpdateOpacityButton();
            SaveSettingsNow();
        }

        private void UpdateOpacityButton()
        {
            _btnOpacity.Text = "Opacity " + (int)Math.Round(_opacityValue * 100) + "%";
        }
    }
}
