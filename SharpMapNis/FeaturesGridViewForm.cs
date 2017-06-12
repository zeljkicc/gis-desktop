using MetroFramework.Forms;
using SharpMap.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GISprojekat4
{
    public partial class FeaturesGridViewForm : MetroForm
    {
        public FeaturesGridViewForm(FeatureDataTable data)
        {
            InitializeComponent();

            this.dataGridView1.DataSource = data;
        }
    }
}
