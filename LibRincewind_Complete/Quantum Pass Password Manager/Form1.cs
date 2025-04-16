using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Quantum_Pass_Password_Manager
{    
    public unsafe partial class Form1 : Form
    {
        
        private class Passwords
        {
            public String title = "";
            public String username = "";
            public String description = "";
            private String _encodedPassword = "";
            public String encodedPassword
            {
                get
                {
                    LibRincewind_4._7._2.CRincewind cRincewind = new LibRincewind_4._7._2.CRincewind(AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindPlugin_ChaCha20_4.7.2.dll", AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindRNG_QRNG-API.dll", 96 / 8);                    
                    return cRincewind.decryptString(_encodedPassword, Globals.MasterPass.Substring(0, Globals.MasterPass.Length / 2), Globals.MasterPass.Substring(Globals.MasterPass.Length / 2));
                }
                set
                {
                    LibRincewind_4._7._2.CRincewind cRincewind = new LibRincewind_4._7._2.CRincewind(AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindPlugin_ChaCha20_4.7.2.dll", AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindRNG_QRNG-API.dll", 96 / 8);
                    _encodedPassword= cRincewind.encryptString(value, Globals.MasterPass.Substring(0, Globals.MasterPass.Length / 2), Globals.MasterPass.Substring(Globals.MasterPass.Length / 2));
                }
            }
        }

        private Dictionary<String,List<Passwords>> passwordDB=new Dictionary<String,List<Passwords>>();
        
        public Form1()
        {
            InitializeComponent();
            menuStrip1.Renderer = new CustomizedMenuRenderer();
            Globals.MasterPass = "sdfiojfdguidsfdfg";
        }

        public class CustomizedMenuRenderer : ToolStripRenderer
        {
            ArrayList noHighlights = new ArrayList();

            public void addDisableHighlights(string menuItemName)
            {
                noHighlights.Add(menuItemName);
            }
            public void removeDisableHighlights(string menuItemName)
            {
                noHighlights.Remove(menuItemName);
            }


            protected override void OnRenderToolStripBackground(
             ToolStripRenderEventArgs e)
            {
                SolidBrush brush = new SolidBrush(Color.Black);
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
                brush.Dispose();
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                bool showHighlight = true;

                showHighlight = !noHighlights.Contains(e.Item.Name);


                if (e.Item.Selected && showHighlight)
                {
                    {
                        Rectangle rect = e.Item.ContentRectangle;
                        rect.X = e.Item.Padding.Left;
                        rect.Y = e.Item.Padding.Top;
                        rect.X -= 2;
                        rect.Y -= 5;
                        rect.Width -= 14;
                        rect.Height += 8;

                        SolidBrush brush = new SolidBrush(Color.Black);
                        e.Graphics.FillRectangle(brush, rect);

                        Pen outline = new Pen(Color.FromArgb(128, 128, 255), 1);
                        e.Graphics.DrawRectangle(outline, rect);
                    }
                }
            }
        }
        private void PasswordsToolStripMenuItem_BackColorChanged(object sender, EventArgs e)
        {
            
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void passwordsToolStripMenuItem_Paint(object sender, PaintEventArgs e)
        {
            

        }

        private void passwordsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            passwordsToolStripMenuItem.BackColor = Color.Black;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Close();   
        }

        private void panel2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void label8_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void addToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            String password = "";
            if (listBox1.SelectedItem != null)
            {
                if (Globals.InputBox("Add Password", "Please enter a title for the password.", ref password) == DialogResult.OK)
                {
                    listBox2.Items.Add(password);
                    listBox2.SelectedItem = password;
                    txtTitle.Text = password;
                    textBox2.Text = "";
                    textBox3.Text = "";
                    txtPassword.Text = "*****";
                    panel1.Enabled = true;
                    passwordDB[(String)listBox1.SelectedItem].Add(new Passwords() { title = password});
                }                
            }
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String category = "";
            if(Globals.InputBox("Add Category", "Please enter the category name.", ref category)==DialogResult.OK)
            {
                listBox1.Items.Add(category);
                listBox1.SelectedItem = category;
                passwordDB.Add(category, new List<Passwords>());
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(txtPassword.Text!="")
                passwordDB[(String)listBox1.SelectedItem].Where(a => a.title == txtTitle.Text).First().encodedPassword = txtPassword.Text;
            passwordDB[(String)listBox1.SelectedItem].Where(a => a.title == txtTitle.Text).First().username=textBox2.Text;
            passwordDB[(String)listBox1.SelectedItem].Where(a => a.title == txtTitle.Text).First().description=textBox3.Text;
            txtPassword.Text = "";
        }

        [STAThread]
        private void thrd()
        {
                System.Threading.Thread.Sleep(7000);
                Invoke(new Action(() =>{
                    Clipboard.Clear();
                }));

        }
        [STAThread]
        private void button4_Click(object sender, EventArgs e)
        {
            //Globals.MasterPass = "dfguujjjj";
            Clipboard.SetText(passwordDB[(String)listBox1.SelectedItem].Where(a => a.title == txtTitle.Text).First().encodedPassword);
            new System.Threading.Thread(thrd).Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            txtPassword.Text = "";
            txtTitle.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            panel1.Enabled=false;
            listBox2.SelectedIndex = -1;
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtPassword.Text = "*****";
            txtTitle.Text = (String)listBox2.SelectedItem;
            if (passwordDB[(String)listBox1.SelectedItem].Where(a => a.title == txtTitle.Text).Count() > 0)
            {
                textBox2.Text = passwordDB[(String)listBox1.SelectedItem].Where(a => a.title == txtTitle.Text).First().username;
                textBox3.Text = passwordDB[(String)listBox1.SelectedItem].Where(a => a.title == txtTitle.Text).First().description;
            }
            panel1.Enabled = true;
        }

        private void txtPassword_Enter(object sender, EventArgs e)
        {
            txtPassword.Text = "";
        }
    }
}
