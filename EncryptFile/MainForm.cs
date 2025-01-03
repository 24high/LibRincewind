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

namespace EncryptFile
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                String password = textBox3.Text;
                byte[] file = System.IO.File.ReadAllBytes(dlg.FileName);
                textBox2.Text = dlg.FileName;

                int lenPart1 = (int)Math.Floor((float)password.Length / 2.0f);                

                String pw1=password.Substring(0, lenPart1);
                String pw2=password.Substring(lenPart1);

                String ret=new LibRincewind_4._7._2.CRincewind(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\LibRincewindPlugin_Blowfish_4.7.2.dll",16).encryptString(System.Convert.ToBase64String(file),pw1,pw2);

                SaveFileDialog dlg2 = new SaveFileDialog();
                if(dlg2.ShowDialog() == DialogResult.OK)
                {
                    String fileWrite=dlg2.FileName;
                    System.IO.File.WriteAllText(fileWrite, ret);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                String password = textBox3.Text;
                String file = System.IO.File.ReadAllText(dlg.FileName);
                textBox2.Text = dlg.FileName;

                int lenPart1 = (int)Math.Floor((float)password.Length / 2.0f);

                String pw1 = password.Substring(0, lenPart1);
                String pw2 = password.Substring(lenPart1);

                String ret = new LibRincewind_4._7._2.CRincewind(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)+"\\LibRincewindPlugin_Blowfish_4.7.2.dll",16).decryptString(file, pw1, pw2);

                SaveFileDialog dlg2 = new SaveFileDialog();
                if (dlg2.ShowDialog() == DialogResult.OK)
                {
                    String fileWrite = dlg2.FileName;
                    System.IO.File.WriteAllBytes(fileWrite, System.Convert.FromBase64String(ret));
                }
            }
        }
    }
}
