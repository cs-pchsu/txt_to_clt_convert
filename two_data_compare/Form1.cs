using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace two_data_compare
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string source_file = "mieliestronk_58000.txt";
            string[] lines = System.IO.File.ReadAllLines(source_file);
            string source_file_1 = "69903_data.txt";
            string[] lines_1 = System.IO.File.ReadAllLines(source_file_1);


            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"mieliestronk_only.txt"))
            {
                foreach (string voc in lines)
                {
                    bool is_exist = false;
                    foreach (string voc_1 in lines_1)
                    {
                        if (String.Compare(voc, voc_1, true) == 0)
                        {
                            is_exist = true;
                            break;
                        }
                    }
                    if (is_exist == false)
                        file.WriteLine(voc);
                }
            }



            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"69903_data_only.txt"))
            {
                foreach (string voc in lines_1)
                {
                    bool is_exist = false;
                    foreach (string voc_1 in lines)
                    {
                        if (String.Compare(voc, voc_1, true) == 0)
                        {
                            is_exist = true;
                            break;
                        }
                    }
                    if (is_exist == false)
                        file.WriteLine(voc);
                }
            }

            MessageBox.Show("OK");
        }
    }
}
