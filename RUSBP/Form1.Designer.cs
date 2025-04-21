// Form1.Designer.cs
namespace BloqueoYUsbDetectionApp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(640, 480);
            this.Name = "Form1";
            this.Text = "Detección y Bloqueo de USB";
            this.ResumeLayout(false);
        }
    }
}
