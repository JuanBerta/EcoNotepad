using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace EcoNotepad
{
    public class EntryDialog : Form
    {
        private readonly TextBox _txtItem = new TextBox();
        private readonly TextBox _txtPrice = new TextBox();
        private readonly TextBox _txtStore = new TextBox();
        private readonly TextBox _txtQty = new TextBox();
        private readonly TextBox _txtTags = new TextBox();
        private readonly TextBox _txtNotes = new TextBox();
        private readonly TextBox _txtPosX = new TextBox();
        private readonly TextBox _txtPosY = new TextBox();
        private readonly TextBox _txtPosZ = new TextBox();
        private readonly CheckBox _chkDone = new CheckBox();
        private readonly Button _btnSuggest = new Button { Text = "Suggest Store", Width = 110 };
        private readonly NoteEntry _existing;
        private readonly List<NoteEntry> _allEntries;
        private readonly AppSettings _settings;

        public NoteEntry Entry { get; private set; }

        public EntryDialog(NoteEntry existing, List<NoteEntry> allEntries)
        {
            _existing = existing;
            _allEntries = allEntries;
            _settings = DataStore.LoadSettings();
            Text = existing == null ? "Add entry" : "Edit entry";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 430);
            Font = new Font("Segoe UI", 9f);
            BuildUi();
            if (existing != null) LoadFrom(existing);
            _txtPosX.Text = _settings.PlayerX.ToString("0.###", CultureInfo.InvariantCulture);
            _txtPosY.Text = _settings.PlayerY.ToString("0.###", CultureInfo.InvariantCulture);
            _txtPosZ.Text = _settings.PlayerZ.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private void BuildUi()
        {
            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 8;
            layout.Padding = new Padding(10);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(layout, 0, "Item *", _txtItem);
            AddRow(layout, 1, "Price", _txtPrice);
            AddRow(layout, 3, "Qty", _txtQty);
            AddRow(layout, 4, "Tags", _txtTags);
            AddRow(layout, 5, "Notes", _txtNotes);

            // Store 行：文本框 + Suggest Store 按钮
            var storePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            _txtStore.Width = 250;
            _btnSuggest.Click += (s, e) => OnSuggestStore();
            storePanel.Controls.Add(_txtStore);
            storePanel.Controls.Add(_btnSuggest);
            layout.Controls.Add(new Label
            {
                Text = "Store",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill
            }, 0, 2);
            layout.Controls.Add(storePanel, 1, 2);

            // 我的位置：X/Y/Z + 提示（游戏内 /pos 或 F8 查看）
            var posPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            _txtPosX.Width = 55;
            _txtPosY.Width = 55;
            _txtPosZ.Width = 55;
            posPanel.Controls.Add(new Label { Text = "X", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
            posPanel.Controls.Add(_txtPosX);
            posPanel.Controls.Add(new Label { Text = "Y", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
            posPanel.Controls.Add(_txtPosY);
            posPanel.Controls.Add(new Label { Text = "Z", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
            posPanel.Controls.Add(_txtPosZ);
            posPanel.Controls.Add(new Label
            {
                Text = "用游戏内 /pos 或 F8 查看坐标",
                AutoSize = true,
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft
            });
            layout.Controls.Add(new Label
            {
                Text = "My pos X/Y/Z",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill
            }, 0, 6);
            layout.Controls.Add(posPanel, 1, 6);

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _chkDone.Text = "Purchased / done";
            _chkDone.AutoSize = true;
            layout.Controls.Add(_chkDone, 1, 7);

            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
            ok.Click += (s, e) => ValidateAndClose();

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Padding = new Padding(10, 0, 10, 8),
                FlowDirection = FlowDirection.RightToLeft
            };
            btnPanel.Controls.Add(cancel);
            btnPanel.Controls.Add(ok);

            Controls.Add(btnPanel);
            Controls.Add(layout);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void AddRow(TableLayoutPanel layout, int row, string label, TextBox box)
        {
            var lbl = new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
            layout.Controls.Add(lbl, 0, row);

            box.Dock = DockStyle.Fill;
            if (row == 5)
            {
                box.Multiline = true;
                box.ScrollBars = ScrollBars.Vertical;
            }
            layout.Controls.Add(box, 1, row);
        }

        private void LoadFrom(NoteEntry e)
        {
            _txtItem.Text = e.Item;
            _txtPrice.Text = e.Price;
            _txtStore.Text = e.Store;
            _txtQty.Text = e.Qty;
            _txtTags.Text = e.Tags;
            _txtNotes.Text = e.Notes;
            _chkDone.Checked = e.Done;
        }

        private void ValidateAndClose()
        {
            if (string.IsNullOrWhiteSpace(_txtItem.Text))
            {
                MessageBox.Show(this, "Item name is required.", "EcoNotepad",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 持久化玩家坐标到 settings（游戏内 /pos 或 F8 查看）
            double px, py, pz;
            TryParsePos(out px, out py, out pz);
            _settings.PlayerX = px;
            _settings.PlayerY = py;
            _settings.PlayerZ = pz;
            DataStore.SaveSettings(_settings);

            var e = _existing ?? new NoteEntry();
            e.Item = _txtItem.Text.Trim();
            e.Price = _txtPrice.Text.Trim();
            e.Store = _txtStore.Text.Trim();
            e.Qty = _txtQty.Text.Trim();
            e.Tags = _txtTags.Text.Trim();
            e.Notes = _txtNotes.Text.Trim();
            e.Done = _chkDone.Checked;
            Entry = e;
            DialogResult = DialogResult.OK;
        }

        private void OnSuggestStore()
        {
            string item = _txtItem.Text.Trim();
            if (item.Length == 0)
            {
                MessageBox.Show(this, "Item name is required before suggesting a store.",
                    "EcoNotepad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double px, py, pz;
            TryParsePos(out px, out py, out pz);
            var stores = DataStore.LoadStores();
            var open = _allEntries == null
                ? new List<NoteEntry>()
                : _allEntries.Where(e => !e.Done).ToList();

            using (var dlg = new StoreSuggestDialog(item, open, stores, px, py, pz))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _txtStore.Text = dlg.SelectedStore;
                    if (dlg.SelectedPrice >= 0)
                        _txtPrice.Text = dlg.SelectedPrice.ToString("0.##", CultureInfo.InvariantCulture);
                }
            }
        }

        private void TryParsePos(out double px, out double py, out double pz)
        {
            double.TryParse(_txtPosX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out px);
            double.TryParse(_txtPosY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out py);
            double.TryParse(_txtPosZ.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out pz);
        }
    }
}
