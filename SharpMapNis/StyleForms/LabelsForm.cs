using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using MetroFramework.Forms;

namespace GISprojekat4.StyleForms
{
    public partial class LabelsForm : MetroForm
    {
        public string tableName;

        public string columnName;

        public Color color;

        public float verticalOffset;
        public float horizontalOffset;

        public float fontSize;
        public string fontFamily;

        public LabelsForm(string tableName)
        {
            this.tableName = tableName;
            InitializeComponent();


            FontFamily[] fontFamilies = FontFamily.Families;
            foreach(FontFamily ff in fontFamilies)
            {
                this.comboBox1.Items.Add(ff.Name);
            }
        }

        private void LabelsForm_Load(object sender, EventArgs e)
        {
            string connstring = String.Format("Server={0};Port={1};" +
            "User Id={2};Password={3};Database={4};",
            "127.0.0.1", "5432", "postgres",
            "admin", "serbia");

        // Connect to a PostgreSQL database
        NpgsqlConnection conn = new NpgsqlConnection(connstring);
            conn.Open();            

        // Define a query
        NpgsqlCommand command = new NpgsqlCommand("SELECT column_name from information_schema.columns where table_name = '" + this.tableName + "'" , conn);

            // Execute the query and obtain a result set
            NpgsqlDataReader dr = command.ExecuteReader();

            // Output rows
            while (dr.Read())
                this.labelComboBox1.Items.Add(dr[0]);

            conn.Close();

            // this.labelComboBox1.Items.Add
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.columnName = (string)this.labelComboBox1.SelectedItem;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonColor_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                buttonColor.BackColor = colorDialog1.Color;
                this.color = colorDialog1.Color;

                this.verticalOffset = (float)numericUpDown1.Value;
                this.horizontalOffset = (float)numericUpDown2.Value;

                this.fontSize = (float)numericUpDown3.Value;
                this.fontFamily = comboBox1.SelectedItem.ToString();
                //poisLayer.Style.PointColor = new SolidBrush(colorDialog1.Color);
                //mapBox1.Refresh();
            }
        }
    }
}
