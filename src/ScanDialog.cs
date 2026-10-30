using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace EcoNotepad
{
    /// <summary>
    /// Confirmation dialog shown after an OCR recipe scan. The recipe name is
    /// editable, materials are listed in a grid, and only rows with Missing &gt; 0
    /// are added to the buy list on confirm.
    /// </summary>
    public class ScanDialog : Form
    {
        private readonly TextBox _txtRecipe = new TextBox();
        private readonly DataGridView _grid = new DataGridView();
        private readonly List<RecipeMaterial> _materials;

        public string RecipeName { get { return _txtRecipe.Text.Trim(); } }
        public List<RecipeMaterial> ToAdd { get; private set; }

        public ScanDialog(string recipeName, List<RecipeMaterial> materials, string mode)
        {
            _materials = materials ?? new List<RecipeMaterial>();
            Text = mode + " - recipe scan";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(500, 360);
            Font = new Font("Segoe UI", 9f);
            BuildUi();
            _txtRecipe.Text = recipeName ?? "";
        }

        private void BuildUi()
        {
            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(8, 4, 8, 4)
            };
            top.Controls.Add(new Label
            {
                Text = "Recipe:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            });
            _txtRecipe.Width = 320;
            top.Controls.Add(_txtRecipe);

            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false;
            _grid.MultiSelect = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.AutoGenerateColumns = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.BackgroundColor = Color.White;
            _grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

            AddGridColumn("Item", "Item", 60F, true);
            AddGridColumn("Required", "Required", 20F, true);
            AddGridColumn("Available", "Available", 20F, true);
            AddGridColumn("Missing", "Missing", 20F, false);

            _grid.DataSource = new BindingList<RecipeMaterial>(_materials);
            _grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_grid.IsCurrentCellDirty)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            var ok = new Button { Text = "Add to Buy List", Width = 130 };
            ok.Click += (s, e) => Confirm();
            var cancel = new Button { Text = "Cancel", Width = 80, DialogResult = DialogResult.Cancel };

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                Padding = new Padding(8, 6, 8, 6),
                FlowDirection = FlowDirection.RightToLeft
            };
            btnPanel.Controls.Add(cancel);
            btnPanel.Controls.Add(ok);

            Controls.Add(_grid);
            Controls.Add(top);
            Controls.Add(btnPanel);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void AddGridColumn(string prop, string header, float fillWeight, bool readOnly)
        {
            var c = new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop,
                HeaderText = header,
                FillWeight = fillWeight,
                ReadOnly = readOnly,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            _grid.Columns.Add(c);
        }

        private void Confirm()
        {
            _grid.EndEdit();
            ToAdd = new List<RecipeMaterial>();
            foreach (RecipeMaterial m in _materials)
            {
                if (m.Missing > 0) ToAdd.Add(m);
            }
            DialogResult = DialogResult.OK;
        }
    }
}
