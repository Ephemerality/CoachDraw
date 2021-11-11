using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoachDraw
{
    // https://stackoverflow.com/a/17546909
    public static class InputBoxExtension
    {
        public static DialogResult InputBox(this Form owner, string prompt, string title, out string value, string defaultValue = "")
        {
            var inputBox = new Form();

            var currentY = 5;

            var label = new Label
            {
                AutoSize = true,
                Text = prompt,
                Location = new Point(5, currentY)
            };
            inputBox.Controls.Add(label);

            var size = new Size(Math.Max(300, label.Width + 10), Math.Max(70, label.Height + 50));

            currentY += label.Height + 5;

            var textBox = new TextBox
            {
                Size = new Size(size.Width - 10, 23),
                Location = new Point(5, currentY),
                Text = defaultValue
            };
            inputBox.Controls.Add(textBox);

            currentY += textBox.Height + 5;

            var okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Name = "okButton",
                Size = new Size(75, 23),
                Text = "&OK",
                Location = new Point(size.Width - 80 - 80, currentY)
            };
            inputBox.Controls.Add(okButton);

            var cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Name = "cancelButton",
                Size = new Size(75, 23),
                Text = "&Cancel",
                Location = new Point(size.Width - 80, currentY)
            };
            inputBox.Controls.Add(cancelButton);

            currentY += okButton.Height + 5;

            size.Height = currentY;

            inputBox.AcceptButton = okButton;
            inputBox.CancelButton = cancelButton;
            inputBox.FormBorderStyle = FormBorderStyle.FixedDialog;
            inputBox.StartPosition = FormStartPosition.CenterParent;
            inputBox.ClientSize = size;
            inputBox.Text = title;

            var result = inputBox.ShowDialog(owner);
            value = textBox.Text;
            return result;
        }
    }
}