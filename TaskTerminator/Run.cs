using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskTerminator
{
    public partial class Run : Form
    {
        public Run()
        {
            InitializeComponent();
        }
        public String command;

        private void buttonRun_Click(object sender, EventArgs e)
        {
            command = textBox.Text;
            this.DialogResult = DialogResult.OK;
        }

        private void buttonRunAdmin_Click(object sender, EventArgs e)
        {
            command = textBox.Text;
            this.DialogResult = DialogResult.Yes;
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                buttonRun_Click(sender, e);
            }
        }
    }
}
