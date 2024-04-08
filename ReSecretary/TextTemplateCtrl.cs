using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;

namespace ReSecretary
{
    public partial class TextTemplateCtrl : Form
    {



        public TextTemplateCtrl(IEnumerable<string> organs4Show)
        {
            InitializeComponent();

            radListView1.DoubleClick += radListView1_DoubleClick;
            this.radListView1.DataSource = organs4Show;
        }

        private void radListView1_DoubleClick(object sender, EventArgs e)
        {
            SelectedText = "";
            if (radListView1.SelectedItem != null)

                SelectedText = radListView1.SelectedItem.Text.ToString();
            this.Hide();
        }

        public string SelectedText { get; set; }
    }
}
