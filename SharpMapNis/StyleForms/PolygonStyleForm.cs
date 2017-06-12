using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GISprojekat4.StyleForms
{
    public partial class PolygonStyleForm : MetroForm
    {
        public Color fillColor;
        public Color strokeColor;
        public HatchStyle hatchStyle;
        public bool setPattern = false;

        public PolygonStyleForm()
        {
            InitializeComponent();

            Array array = Enum.GetValues(typeof(HatchStyle));

            comboBoxPattern.Items.Add("None");
            foreach(HatchStyle a in array)
            {
                comboBoxPattern.Items.Add(a.ToString());
            }
                

            //
        }

        private void buttonFill_Click_1(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                buttonFill.BackColor = colorDialog1.Color;
                this.fillColor = colorDialog1.Color;

                //poisLayer.Style.PointColor = new SolidBrush(colorDialog1.Color);
                //mapBox1.Refresh();
            }
        }

        private void buttonStroke_Click_1(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                buttonStroke.BackColor = colorDialog1.Color;
                this.strokeColor = colorDialog1.Color;

                //poisLayer.Style.PointColor = new SolidBrush(colorDialog1.Color);
                //mapBox1.Refresh();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int index = comboBoxPattern.SelectedIndex;

            if(index > 0)
            {
                this.setPattern = true;
                this.hatchStyle = (HatchStyle)Enum.GetValues(typeof(HatchStyle)).GetValue(index);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
