using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RUSBP.Helpers
{
    public static class Prompt
    {
        /// <summary>
        /// Muestra un inputbox para pedir el RecoveryPassword BitLocker (48 dígitos, guiones auto).
        /// </summary>
        public static bool ForRecoveryPassword(out string recoveryPassword)
        {
            recoveryPassword = "";

            Form prompt = new Form()
            {
                Width = 950,
                Height = 270,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Ingrese RecoveryPassword BitLocker",
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false,
                BackColor = Color.FromArgb(20, 23, 36),
            };

            Label label = new Label()
            {
                Left = 0,
                Top = 30,
                Width = 930,
                AutoSize = false,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Ingrese el RecoveryPassword del USB ROOT\n(48 dígitos, autoformato y validación al aceptar):"
            };

            TextBox textBox = new TextBox()
            {
                Left = 60,
                Top = 90,
                Width = 820,
                Font = new Font(FontFamily.GenericMonospace, 17),
                MaxLength = 55, // 48 dígitos + 7 guiones
                TabIndex = 0,
                BackColor = Color.FromArgb(31, 34, 51),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Solo números, guiones auto cada 6 dígitos
            textBox.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
            textBox.TextChanged += (s, e) =>
            {
                string raw = new string(textBox.Text.Where(char.IsDigit).ToArray());
                if (raw.Length > 48) raw = raw.Substring(0, 48);
                string formatted = string.Join("-", Enumerable.Range(0, 8)
                    .Select(i => raw.Skip(i * 6).Take(6)).Where(x => x.Any()).Select(x => new string(x.ToArray()))
                );
                if (textBox.Text != formatted)
                {
                    int selStart = textBox.SelectionStart;
                    textBox.Text = formatted;
                    // Ajusta caret
                    textBox.SelectionStart = Math.Min(selStart, textBox.Text.Length);
                }
            };

            Button okButton = new Button()
            {
                Text = "Aceptar",
                Left = 340,
                Top = 170,
                Width = 140,
                Height = 40,
                DialogResult = DialogResult.OK,
                TabIndex = 1,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 102, 200),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
            };
            okButton.FlatAppearance.BorderSize = 0;

            Button cancelButton = new Button()
            {
                Text = "Cancelar",
                Left = 510,
                Top = 170,
                Width = 140,
                Height = 40,
                DialogResult = DialogResult.Cancel,
                TabIndex = 2,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 38, 54),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
            };
            cancelButton.FlatAppearance.BorderSize = 0;

            prompt.Controls.Add(label);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(okButton);
            prompt.Controls.Add(cancelButton);

            prompt.AcceptButton = okButton;
            prompt.CancelButton = cancelButton;

            okButton.Click += (s, e) =>
            {
                string val = textBox.Text;
                if (!System.Text.RegularExpressions.Regex.IsMatch(val, @"^(\d{6}-){7}\d{6}$"))
                {
                    MessageBox.Show("El RecoveryPassword debe tener exactamente 48 dígitos (8 bloques de 6 separados por guiones)", "Formato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox.Focus();
                    prompt.DialogResult = DialogResult.None;
                }
            };

            prompt.StartPosition = FormStartPosition.CenterScreen;
            prompt.ShowIcon = false;

            var result = prompt.ShowDialog();
            if (result == DialogResult.OK)
            {
                recoveryPassword = textBox.Text;
                return true;
            }
            return false;
        }
    }
}
