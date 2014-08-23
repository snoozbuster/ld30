using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LD30
{
    public partial class NameForm : Form
    {
        public string Filename { get; private set; }

        private string[] names;

        public NameForm()
        {
            InitializeComponent();

            string[] paths = Directory.GetFiles(Program.SavePath, "*.wld", SearchOption.TopDirectoryOnly);
            names = new string[paths.Length];
            for(int i = 0; i < names.Length; i++)
                names[i] = Path.GetFileNameWithoutExtension(paths[i]);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Regex r = new Regex("^[a-zA-Z0-9_-]*$");
            if(textBox1.Text.Length > 0 && !names.Contains(textBox1.Text) && r.IsMatch(textBox1.Text))
                button1.Enabled = true;
            else
                button1.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Filename = textBox1.Text;
            Hide();
        }
    }
}
