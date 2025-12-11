using DrugCatalog_ver2.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DrugCatalog_ver2.Forms
{
    public partial class DosageCalculatorForm : Form
    {
        private readonly List<Drug> _drugs;

        // Элементы управления
        private ComboBox comboBoxDrug;
        private Label labelStockValue;
        private NumericUpDown numericDose;
        private NumericUpDown numericFreq;
        private Label labelResultDays;
        private Label labelResultDate;
        private Panel resultPanel;

        public DosageCalculatorForm(List<Drug> drugs)
        {
            _drugs = drugs;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = Locale.Get("TitleCalc");
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Font = new Font("Microsoft Sans Serif", 10f);

            CreateControls();
            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            int y = 20;
            int labelW = 150;
            int inputW = 250;
            int x = 30;

            // 1. Выбор лекарства
            var lblDrug = new Label { Text = Locale.Get("ColDrug") + ":", Location = new Point(x, y), Size = new Size(labelW, 25), Font = new Font(this.Font, FontStyle.Bold) };
            comboBoxDrug = new ComboBox { Location = new Point(x + labelW, y), Size = new Size(inputW, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            comboBoxDrug.Items.AddRange(_drugs.OrderBy(d => d.Name).Select(d => d.Name).ToArray());
            comboBoxDrug.SelectedIndexChanged += (s, e) => Recalculate();

            this.Controls.Add(lblDrug);
            this.Controls.Add(comboBoxDrug);
            y += 50;

            // Группировка параметров
            var grpParams = new GroupBox
            {
                Text = Locale.Get("GrpParams"),
                Location = new Point(20, y),
                Size = new Size(440, 150)
            };

            int gy = 30;

            // Текущий остаток
            var lblStock = new Label { Text = Locale.Get("LblCurrentStock"), Location = new Point(20, gy), Size = new Size(180, 25) };
            labelStockValue = new Label { Text = "0", Location = new Point(220, gy), Size = new Size(200, 25), Font = new Font(this.Font, FontStyle.Bold), ForeColor = Color.SteelBlue };
            grpParams.Controls.Add(lblStock);
            grpParams.Controls.Add(labelStockValue);
            gy += 40;

            // Разовая доза
            var lblDose = new Label { Text = Locale.Get("LblSingleDose"), Location = new Point(20, gy), Size = new Size(180, 25) };
            numericDose = new NumericUpDown { Location = new Point(220, gy), Size = new Size(80, 25), Minimum = 0, Maximum = 100, DecimalPlaces = 1, Value = 1 };
            numericDose.ValueChanged += (s, e) => Recalculate();
            grpParams.Controls.Add(lblDose);
            grpParams.Controls.Add(numericDose);
            gy += 40;

            // Частота в день
            var lblFreq = new Label { Text = Locale.Get("LblDailyFreq"), Location = new Point(20, gy), Size = new Size(180, 25) };
            numericFreq = new NumericUpDown { Location = new Point(220, gy), Size = new Size(80, 25), Minimum = 1, Maximum = 24, Value = 3 };
            numericFreq.ValueChanged += (s, e) => Recalculate();
            grpParams.Controls.Add(lblFreq);
            grpParams.Controls.Add(numericFreq);

            this.Controls.Add(grpParams);
            y += 170;

            // Группировка результата
            var grpResult = new GroupBox
            {
                Text = Locale.Get("GrpResult"),
                Location = new Point(20, y),
                Size = new Size(440, 120)
            };

            resultPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.LightGray };

            labelResultDays = new Label
            {
                Text = "---",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft Sans Serif", 14f, FontStyle.Bold)
            };

            labelResultDate = new Label
            {
                Text = "",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft Sans Serif", 10f, FontStyle.Italic)
            };

            resultPanel.Controls.Add(labelResultDate);
            resultPanel.Controls.Add(labelResultDays);
            grpResult.Controls.Add(resultPanel);

            this.Controls.Add(grpResult);

            var btnClose = new Button { Text = Locale.Get("Close"), Location = new Point(180, 370), Size = new Size(120, 35) };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            if (comboBoxDrug.Items.Count > 0) comboBoxDrug.SelectedIndex = 0;
        }

        private void Recalculate()
        {
            if (string.IsNullOrEmpty(comboBoxDrug.Text)) return;

            var drug = _drugs.FirstOrDefault(d => d.Name == comboBoxDrug.Text);
            if (drug == null) return;

            // --- ИСПРАВЛЕНИЕ ЗДЕСЬ ---
            // Было: drug.DosageUnit (мг)
            // Стало: drug.Form (Таблетки, Ампулы и т.д.)
            labelStockValue.Text = $"{drug.Quantity} {drug.Form}";

            decimal quantity = drug.Quantity;
            decimal dosePerTake = numericDose.Value;
            decimal timesPerDay = numericFreq.Value;

            if (quantity <= 0)
            {
                SetResult(Locale.Get("MsgEmptyStock"), "", Color.LightCoral);
                return;
            }

            if (dosePerTake <= 0)
            {
                SetResult(Locale.Get("MsgForever"), "", Color.LightGreen);
                return;
            }

            decimal dailyConsumption = dosePerTake * timesPerDay;
            decimal daysLasting = quantity / dailyConsumption;

            int fullDays = (int)Math.Floor(daysLasting);

            Color statusColor;
            if (fullDays < 3) statusColor = Color.LightCoral;
            else if (fullDays < 7) statusColor = Color.LightYellow;
            else statusColor = Color.LightGreen;

            DateTime endDate = DateTime.Now.AddDays(fullDays);

            string daysText = string.Format(Locale.Get("MsgEnoughFor"), fullDays);
            string dateText = string.Format(Locale.Get("MsgEndDate"), endDate.ToString("dd MMMM yyyy"));

            SetResult(daysText, dateText, statusColor);
        }

        private void SetResult(string mainText, string subText, Color color)
        {
            labelResultDays.Text = mainText;
            labelResultDate.Text = subText;
            resultPanel.BackColor = color;
        }
    }
}