using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GISprojekat4.StyleForms
{
    public partial class DotStyleForm : MetroForm
    {
        public bool setSymbol = true;
        public Image pointSymbol;
        public Color pointColor;
        public string symbolUri;

        public DotStyleForm()
        {
            InitializeComponent();

            this.Text = "Point Style";

            this.button4.Enabled = false;
            symbolUri = "none";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK) 
            {
                string file = openFileDialog1.FileName;

                try
                {


                    Image image = Image.FromFile(file, true);
                    this.pictureBox1.Image = image;

                    this.pointSymbol = image;

                    symbolUri = file;
                }
                catch (IOException)
                {
                }
            }
        
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                button4.BackColor = colorDialog1.Color;
                this.pointColor = colorDialog1.Color;
                this.setSymbol = false;
                this.symbolUri = "none";
                //poisLayer.Style.PointColor = new SolidBrush(colorDialog1.Color);
                //mapBox1.Refresh();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            //if(sender.CheckState )
            bool check = this.checkBox1.Checked;
            if (check)
            {
               // this.comboBox1.Enabled = true;
                this.pictureBox1.Enabled = true;
                this.button3.Enabled = true;
                this.button4.Enabled = false;
            }
            else
            {
              //  this.comboBox1.Enabled = false;
                this.pictureBox1.Enabled = false;
                this.button3.Enabled = false;
                this.button4.Enabled = true;
            }

        }


    }
}
