// Decompiled with JetBrains decompiler
// Type: LibRincewindDemo_4._7._2.Form1
// Assembly: LibRincewindDemo_4.7.2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 41823D56-9D35-41B9-9885-0F8FA7853323
// Assembly location: C:\Users\denni\Downloads\LibRincewind-main\LibRincewind-main\Demo\LibRincewindDemo_4.7.2.exe

using LibRincewind_4._7._2;
using System;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;


namespace LibRincewindDemo_4._7._2
{
  public class Form1 : Form
  {
    private CRincewind libRincewind = (CRincewind) null;
    private IContainer components = (IContainer) null;
    private Label label1;
    private Label label2;
    private Label label3;
    private Button button1;
    private Label label4;
    private Label label5;
    private Label label6;
    private Button button2;
    private TextBox textBox1;
    private TextBox textBox2;
    private TextBox textBox3;
    private TextBox textBox4;
    private TextBox textBox5;
        private TextBox textBox6;
        private GroupBox groupBox1;
        private RadioButton radioButton1;
        private RadioButton radioButton4;
        private RadioButton radioButton2;
        private Button button3;
        private Label label7;
        private Label label8;
        private TextBox textBox7;
        private TextBox textBox8;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private Label label9;
        private CheckBox checkBox1;
        bool useRC4 = false;
    public Form1()
    {
      this.InitializeComponent();
      if(!useRC4)
        this.libRincewind = new CRincewind(AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindPlugin_Blowfish_4.7.2.dll", 8);
      else
                this.libRincewind = new CRincewind(AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindPlugin_RC4Plus_4.7.2.dll", 8);
            
    }

    private void Form1_Load(object sender, EventArgs e)
    {
    }
        byte[] salt = null;
        byte[] salt1 = null;
        private void button1_Click(object sender, EventArgs e)
    {
            salt = CRincewind.QRNG(256);

            salt1 = CRincewind.QRNG(256);
            CCryptData ccryptData = this.libRincewind.encryptCCD(this.textBox3.Text, this.textBox1.Text, this.textBox2.Text,salt,salt1);
      this.textBox4.Text = ccryptData.CryptedData;
      this.textBox5.Text = ccryptData.Key;
        }

    private void button2_Click(object sender, EventArgs e)
    {
      CRincewind libRincewind = this.libRincewind;
      CCryptData cryptData = new CCryptData();
      cryptData.CryptedData = this.textBox4.Text;
      cryptData.Key = this.textBox5.Text;
      cryptData.IV = this.libRincewind.IV;
        

            string text1 = this.textBox1.Text;
      string text2 = this.textBox2.Text;
      this.textBox6.Text = libRincewind.decryptCCD(cryptData, text1, text2, salt, salt1);
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.components != null)
        this.components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioButton4 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.button3 = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox7 = new System.Windows.Forms.TextBox();
            this.textBox8 = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label9 = new System.Windows.Forms.Label();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 44);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(130, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Password 1:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 79);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(130, 25);
            this.label2.TabIndex = 1;
            this.label2.Text = "Password 2:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 154);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(175, 25);
            this.label3.TabIndex = 2;
            this.label3.Text = "String to encrypt:";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(474, 214);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(125, 38);
            this.button1.TabIndex = 3;
            this.button1.Text = "Encrypt";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 273);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(115, 25);
            this.label4.TabIndex = 4;
            this.label4.Text = "Encrypted:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 308);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(163, 25);
            this.label5.TabIndex = 5;
            this.label5.Text = "Encryption Key:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(18, 340);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(157, 25);
            this.label6.TabIndex = 6;
            this.label6.Text = "Decrypted text:";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(474, 378);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(125, 38);
            this.button2.TabIndex = 7;
            this.button2.Text = "Decrypt";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(199, 44);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(400, 31);
            this.textBox1.TabIndex = 8;
            this.textBox1.UseSystemPasswordChar = true;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(201, 81);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(400, 31);
            this.textBox2.TabIndex = 9;
            this.textBox2.UseSystemPasswordChar = true;
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(199, 158);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(400, 31);
            this.textBox3.TabIndex = 10;
            // 
            // textBox4
            // 
            this.textBox4.Location = new System.Drawing.Point(199, 267);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(400, 31);
            this.textBox4.TabIndex = 11;
            // 
            // textBox5
            // 
            this.textBox5.Location = new System.Drawing.Point(199, 304);
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new System.Drawing.Size(400, 31);
            this.textBox5.TabIndex = 12;
            // 
            // textBox6
            // 
            this.textBox6.Location = new System.Drawing.Point(199, 341);
            this.textBox6.Name = "textBox6";
            this.textBox6.ReadOnly = true;
            this.textBox6.Size = new System.Drawing.Size(400, 31);
            this.textBox6.TabIndex = 13;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBox1);
            this.groupBox1.Controls.Add(this.radioButton4);
            this.groupBox1.Controls.Add(this.radioButton2);
            this.groupBox1.Controls.Add(this.radioButton1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(616, 225);
            this.groupBox1.TabIndex = 16;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Algorithm";
            // 
            // radioButton4
            // 
            this.radioButton4.AutoSize = true;
            this.radioButton4.Location = new System.Drawing.Point(225, 139);
            this.radioButton4.Name = "radioButton4";
            this.radioButton4.Size = new System.Drawing.Size(145, 29);
            this.radioButton4.TabIndex = 3;
            this.radioButton4.Text = "ChaCha20";
            this.radioButton4.UseVisualStyleBackColor = true;
            this.radioButton4.CheckedChanged += new System.EventHandler(this.radioButton4_CheckedChanged);
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(225, 104);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(127, 29);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.Text = "RC4Plus";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton2_CheckedChanged);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Location = new System.Drawing.Point(225, 69);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(123, 29);
            this.radioButton1.TabIndex = 0;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "Blowfish";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(227, 29);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(160, 74);
            this.button3.TabIndex = 17;
            this.button3.Text = "Test";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(65, 145);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(211, 25);
            this.label7.TabIndex = 18;
            this.label7.Text = "Without Librincewind";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(325, 145);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(181, 25);
            this.label8.TabIndex = 19;
            this.label8.Text = "With Librincewind";
            // 
            // textBox7
            // 
            this.textBox7.Location = new System.Drawing.Point(70, 173);
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new System.Drawing.Size(206, 31);
            this.textBox7.TabIndex = 20;
            // 
            // textBox8
            // 
            this.textBox8.Location = new System.Drawing.Point(330, 173);
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new System.Drawing.Size(176, 31);
            this.textBox8.TabIndex = 21;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBox6);
            this.groupBox2.Controls.Add(this.textBox5);
            this.groupBox2.Controls.Add(this.textBox4);
            this.groupBox2.Controls.Add(this.textBox3);
            this.groupBox2.Controls.Add(this.textBox2);
            this.groupBox2.Controls.Add(this.textBox1);
            this.groupBox2.Controls.Add(this.button2);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.button1);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Location = new System.Drawing.Point(22, 247);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(605, 429);
            this.groupBox2.TabIndex = 22;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Encrypt/Decrypt";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.textBox8);
            this.groupBox3.Controls.Add(this.textBox7);
            this.groupBox3.Controls.Add(this.label8);
            this.groupBox3.Controls.Add(this.label7);
            this.groupBox3.Controls.Add(this.button3);
            this.groupBox3.Location = new System.Drawing.Point(22, 686);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(604, 243);
            this.groupBox3.TabIndex = 23;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Test failures";
            this.groupBox3.Enter += new System.EventHandler(this.groupBox3_Enter);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(367, 215);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(70, 25);
            this.label9.TabIndex = 22;
            this.label9.Text = "label9";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(350, 174);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(266, 29);
            this.checkBox1.TabIndex = 4;
            this.checkBox1.Text = "Use Quantum RNG API";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(660, 951);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form1";
            this.Text = "LibRincewind Demo (c) 2023 Dennis M. Heine";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

    }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton1.Checked)
                this.libRincewind = new CRincewind(AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindPlugin_Blowfish_4.7.2.dll", 8);

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton2.Checked)
                this.libRincewind = new CRincewind(AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindPlugin_RC4Plus_4.7.2.dll", 16);

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton4.Checked)
                this.libRincewind = new CRincewind(AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindPlugin_ChaCha20_4.7.2.dll", 96/8);

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }
        bool running = true;

        public byte[] generateIV(int length)
        {
            return CRincewind.QRNG(length);
        }
        private void button3_Click(object sender, EventArgs e)
        {
            if (running)
            {
                button3.Text = "Test";
                running = false;
            }
            else
            {
                button3.Text = "Stop";
                new System.Threading.Thread(() =>
                {
                    long tries = 0;
                    long errors = 0;
                    running = true;
                    while (running)
                    {
                        String pass1 = RandomString(10);
                        String pass2 = RandomString(10);

                        CRincewind libRincewind = this.libRincewind;
                        CCryptData cryptData = new CCryptData();
                        cryptData.CryptedData = this.textBox4.Text;
                        cryptData.Key = this.textBox5.Text;
                        cryptData.IV = this.libRincewind.IV;
                        cryptData.Salt = Convert.ToBase64String(CRincewind.QRNG(256));
                        cryptData.Salt1 = Convert.ToBase64String(CRincewind.QRNG(256));
                        String LRDec = libRincewind.decryptCCD(cryptData, pass1, pass2, Convert.FromBase64String(cryptData.Salt), Convert.FromBase64String(cryptData.Salt1));
                        this.textBox8.Invoke(new Action(() =>
                        {
                            tries++;
                            for (int i = 0; i < LRDec.Length; i++)
                                if (LRDec[i] < 36 || LRDec[i] > 126)
                                {
                                    errors++;
                                    break;
                                }
                            this.label9.Text=errors.ToString()+"/"+tries.ToString();
                            this.textBox8.Text = LRDec;
                        }));
                       
                       
                        System.Threading.Thread.Sleep(500);
                    }
                    ;
                }).Start();
            }
        }

        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static bool QRNG = false;
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            CRincewind.UseQRNG = checkBox1.Checked;
        }
    }
}
