using System;
using System.Drawing;
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
        private readonly CheckBox _chkDone = new CheckBox();
        private readonly NoteEntry _existing;

        public NoteEntry Entry { get; private set; }

        public EntryDialog(NoteEntry existing)
        {
            _existing = existing;
            Text = existing == null ? "Add entry" : "Edit entry";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(400, 350);
            Font = new Font("Segoe UI", 9f);
            BuildUi();
            if (existing != null) LoadFrom(existing);
        }

        private void BuildUi()
        {
            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 7;
            layout.Padding = new Padding(10);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(layout, 0, "Item *", _txtItem);
            AddRow(layout, 1, "Price", _txtPrice);
            AddRow(layout, 2, "Store", _txtStore);
            AddRow(layout, 3, "Qty", _txtQty);
            AddRow(layout, 4, "Tags", _txtTags);
            AddRow(layout, 5, "Notes", _txtNotes);

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _chkDone.Text = "Purchased / done";
            _chkDone.AutoSize = true;
            layout.Controls.Add(_chkDone, 1, 6);

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
    }
}
