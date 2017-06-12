namespace GISprojekat4.StyleForms
{
    partial class PolygonStyleForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.buttonFill = new System.Windows.Forms.Button();
            this.buttonStroke = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.comboBoxPattern = new MetroFramework.Controls.MetroComboBox();
            this.metroLabel1 = new MetroFramework.Controls.MetroLabel();
            this.SuspendLayout();
            // 
            // buttonFill
            // 
            this.buttonFill.Location = new System.Drawing.Point(24, 74);
            this.buttonFill.Name = "buttonFill";
            this.buttonFill.Size = new System.Drawing.Size(225, 23);
            this.buttonFill.TabIndex = 0;
            this.buttonFill.Text = "Fill Color";
            this.buttonFill.UseVisualStyleBackColor = true;
            this.buttonFill.Click += new System.EventHandler(this.buttonFill_Click_1);
            // 
            // buttonStroke
            // 
            this.buttonStroke.Location = new System.Drawing.Point(23, 103);
            this.buttonStroke.Name = "buttonStroke";
            this.buttonStroke.Size = new System.Drawing.Size(225, 23);
            this.buttonStroke.TabIndex = 1;
            this.buttonStroke.Text = "Stroke Color";
            this.buttonStroke.UseVisualStyleBackColor = true;
            this.buttonStroke.Click += new System.EventHandler(this.buttonStroke_Click_1);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(24, 199);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(107, 23);
            this.button3.TabIndex = 2;
            this.button3.Text = "OK";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(156, 199);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(93, 23);
            this.button4.TabIndex = 3;
            this.button4.Text = "Cancel";
            this.button4.UseVisualStyleBackColor = true;
            // 
            // comboBoxPattern
            // 
            this.comboBoxPattern.FormattingEnabled = true;
            this.comboBoxPattern.ItemHeight = 23;
            this.comboBoxPattern.Location = new System.Drawing.Point(23, 151);
            this.comboBoxPattern.Name = "comboBoxPattern";
            this.comboBoxPattern.Size = new System.Drawing.Size(225, 29);
            this.comboBoxPattern.TabIndex = 4;
            this.comboBoxPattern.UseSelectable = true;
            // 
            // metroLabel1
            // 
            this.metroLabel1.AutoSize = true;
            this.metroLabel1.Location = new System.Drawing.Point(23, 129);
            this.metroLabel1.Name = "metroLabel1";
            this.metroLabel1.Size = new System.Drawing.Size(53, 19);
            this.metroLabel1.TabIndex = 5;
            this.metroLabel1.Text = "Pattern:";
            // 
            // PolygonStyleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.metroLabel1);
            this.Controls.Add(this.comboBoxPattern);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.buttonStroke);
            this.Controls.Add(this.buttonFill);
            this.Name = "PolygonStyleForm";
            this.Text = "Polygon Style";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Button buttonFill;
        private System.Windows.Forms.Button buttonStroke;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private MetroFramework.Controls.MetroComboBox comboBoxPattern;
        private MetroFramework.Controls.MetroLabel metroLabel1;
    }
}