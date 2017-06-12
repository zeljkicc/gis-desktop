using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GISprojekat4.StyleForms
{
    public partial class LineStyleForm : MetroForm
    {
        public System.Drawing.Drawing2D.DashStyle lineStyle;
        public float lineWidth;
        public Color lineColor;

        public LineStyleForm()
        {
            InitializeComponent();

            this.domainUpDown1.Items.Add("Dash");
            this.domainUpDown1.Items.Add("DashDot");
            this.domainUpDown1.Items.Add("DashDotDot");
            this.domainUpDown1.Items.Add("Dot");
            this.domainUpDown1.Items.Add("Solid");

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                button1.BackColor = colorDialog1.Color;
                this.lineColor = colorDialog1.Color;

                //poisLayer.Style.PointColor = new SolidBrush(colorDialog1.Color);
                //mapBox1.Refresh();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // this.ReturnValue1 = "Something";
            //  this.ReturnValue2 = DateTime.Now.ToString(); //example


            switch ((string)this.domainUpDown1.SelectedItem)
            {
                case "Dash":
                    this.lineStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    break;
                case "DashDot":
                    this.lineStyle = System.Drawing.Drawing2D.DashStyle.DashDot;
                    break;
                case "DashDotDot":
                    this.lineStyle = System.Drawing.Drawing2D.DashStyle.DashDotDot;
                    break;
                case "Dot":
                    this.lineStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                    break;
                case "Solid":
                    this.lineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    break;
            }

            this.lineWidth = (float)this.numericUpDown1.Value;
            
            this.DialogResult = DialogResult.OK;
            this.Close();        
        }
    }
}
