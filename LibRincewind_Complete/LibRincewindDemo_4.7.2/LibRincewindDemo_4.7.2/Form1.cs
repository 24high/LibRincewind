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
    private Label label2;
    private Label label3;
    private Button button1;
    private Label label4;
    private Label label5;
    private Label label6;
    private Button button2;
    private TextBox textBox2;
    private TextBox textBox3;
    private TextBox textBox4;
    private TextBox textBox5;
        private TextBox textBox6;
        private GroupBox groupBox1;
        private RadioButton radioButton4;
        private RadioButton radioButton2;
        private Button button3;
        private Label label8;
        private TextBox textBox8;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private Label label9;
        private CheckBox checkBox1;
        bool useRC4 = false;
        String SRng = "";
        String SEnc = "";
        private Label lblRotationsDec;
        private Label lblRotationsEnc;
        private Label label7;
        private Label label10;
        private Label label11;
        int IVSize = 8;
    public Form1()
    {
        this.InitializeComponent();
        IVSize = 16;
        SEnc = AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindPlugin_RC4Plus_4.7.2.dll";
        this.libRincewind = new CRincewind(SEnc, SRng, 64);
    }

    private void Form1_Load(object sender, EventArgs e)
    {
    }
        byte[] salt = null;
        byte[] salt1 = null;
        String ccryptData = "";
        private void button1_Click(object sender, EventArgs e)
    {
            salt = CRincewind.QRNG(256);

            salt1 = CRincewind.QRNG(256);
            //ccryptData = this.libRincewind.encryptCCD(this.textBox3.Text, this.textBox1.Text, this.textBox2.Text,salt,salt1);
            ccryptData = this.libRincewind.encryptString(textBox3.Text, "", textBox2.Text);
      this.textBox4.Text = ccryptData;
            lblRotationsEnc.Text = "";
            int gesRotation = 0;
            foreach (int rotation in CRincewind.rotations)
            {
                gesRotation += rotation;
                lblRotationsEnc.Text += rotation.ToString() + ";";
            }
            gesRotation = gesRotation / CRincewind.rotations.Count();
            lblRotationsEnc.Text = gesRotation.ToString() + "//" + lblRotationsEnc.Text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CRincewind libRincewind = this.libRincewind;
            CCryptData cryptData = new CCryptData();
            cryptData.CryptedData = this.textBox4.Text;
            cryptData.Key = this.textBox5.Text;
            cryptData.IV = this.libRincewind.IV;

            string text2 = this.textBox2.Text;
            this.textBox6.Text = libRincewind.decryptString(ccryptData, "", text2);
            lblRotationsDec.Text = "";
            float gesRotations = 0;
            foreach (int rotation in CRincewind.rotationsDec)
            {
                gesRotations += (float) rotation;
                lblRotationsDec.Text += rotation.ToString() + ";";
            }
            gesRotations = gesRotations / CRincewind.rotationsDec.Count(); ;
            lblRotationsDec.Text = gesRotations.ToString() + "//" + lblRotationsDec.Text;
        }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.components != null)
        this.components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.radioButton4 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.button3 = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox8 = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.lblRotationsDec = new System.Windows.Forms.Label();
            this.lblRotationsEnc = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(112, 25);
            this.label2.TabIndex = 1;
            this.label2.Text = "Password:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 103);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(175, 25);
            this.label3.TabIndex = 2;
            this.label3.Text = "String to encrypt:";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(474, 147);
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
            this.label4.Location = new System.Drawing.Point(18, 213);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(115, 25);
            this.label4.TabIndex = 4;
            this.label4.Text = "Encrypted:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 248);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(163, 25);
            this.label5.TabIndex = 5;
            this.label5.Text = "Encryption Key:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(18, 280);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(157, 25);
            this.label6.TabIndex = 6;
            this.label6.Text = "Decrypted text:";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(474, 318);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(125, 38);
            this.button2.TabIndex = 7;
            this.button2.Text = "Decrypt";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(201, 43);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(400, 31);
            this.textBox2.TabIndex = 9;
            this.textBox2.UseSystemPasswordChar = true;
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(199, 100);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(400, 31);
            this.textBox3.TabIndex = 10;
            // 
            // textBox4
            // 
            this.textBox4.Location = new System.Drawing.Point(199, 207);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(400, 31);
            this.textBox4.TabIndex = 11;
            // 
            // textBox5
            // 
            this.textBox5.Location = new System.Drawing.Point(199, 244);
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new System.Drawing.Size(400, 31);
            this.textBox5.TabIndex = 12;
            // 
            // textBox6
            // 
            this.textBox6.Location = new System.Drawing.Point(199, 281);
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
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(616, 225);
            this.groupBox1.TabIndex = 16;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Algorithm";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(301, 174);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(266, 29);
            this.checkBox1.TabIndex = 4;
            this.checkBox1.Text = "Use Quantum RNG API";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // radioButton4
            // 
            this.radioButton4.AutoSize = true;
            this.radioButton4.Location = new System.Drawing.Point(225, 118);
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
            this.radioButton2.Checked = true;
            this.radioButton2.Location = new System.Drawing.Point(225, 83);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(127, 29);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "RC4Plus";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton2_CheckedChanged);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(227, 30);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(160, 74);
            this.button3.TabIndex = 17;
            this.button3.Text = "Test";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(63, 179);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(181, 25);
            this.label8.TabIndex = 19;
            this.label8.Text = "With Librincewind";
            // 
            // textBox8
            // 
            this.textBox8.Location = new System.Drawing.Point(261, 176);
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
            this.groupBox2.Controls.Add(this.button2);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.button1);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Location = new System.Drawing.Point(22, 247);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(605, 388);
            this.groupBox2.TabIndex = 22;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Encrypt/Decrypt";
            this.groupBox2.Enter += new System.EventHandler(this.groupBox2_Enter);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.label11);
            this.groupBox3.Controls.Add(this.lblRotationsDec);
            this.groupBox3.Controls.Add(this.lblRotationsEnc);
            this.groupBox3.Controls.Add(this.label7);
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.textBox8);
            this.groupBox3.Controls.Add(this.label8);
            this.groupBox3.Controls.Add(this.button3);
            this.groupBox3.Location = new System.Drawing.Point(22, 665);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(599, 365);
            this.groupBox3.TabIndex = 23;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Test failures";
            this.groupBox3.Enter += new System.EventHandler(this.groupBox3_Enter);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(67, 299);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(144, 25);
            this.label10.TabIndex = 27;
            this.label10.Text = "Rotations dec";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(66, 262);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(144, 25);
            this.label11.TabIndex = 26;
            this.label11.Text = "Rotations enc";
            // 
            // lblRotationsDec
            // 
            this.lblRotationsDec.AutoSize = true;
            this.lblRotationsDec.Location = new System.Drawing.Point(222, 299);
            this.lblRotationsDec.Name = "lblRotationsDec";
            this.lblRotationsDec.Size = new System.Drawing.Size(144, 25);
            this.lblRotationsDec.TabIndex = 25;
            this.lblRotationsDec.Text = "Rotations dec";
            this.lblRotationsDec.Click += new System.EventHandler(this.lblRotationsDec_Click);
            // 
            // lblRotationsEnc
            // 
            this.lblRotationsEnc.AutoSize = true;
            this.lblRotationsEnc.Location = new System.Drawing.Point(222, 262);
            this.lblRotationsEnc.Name = "lblRotationsEnc";
            this.lblRotationsEnc.Size = new System.Drawing.Size(144, 25);
            this.lblRotationsEnc.TabIndex = 24;
            this.lblRotationsEnc.Text = "Rotations enc";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(66, 218);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(102, 25);
            this.label7.TabIndex = 23;
            this.label7.Text = "Error rate";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(256, 218);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(0, 25);
            this.label9.TabIndex = 22;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(654, 1052);
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


        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                IVSize = 64;
                SEnc = AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindPlugin_RC4Plus_4.7.2.dll";
                this.libRincewind = new CRincewind(SEnc, SRng, 16);
                
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
            {
                IVSize = 96 / 8;
                SEnc = AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindPlugin_ChaCha20_4.7.2.dll";
                this.libRincewind = new CRincewind(SEnc, SRng, 96 / 8);
            }
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
                    
                        String LRDec = libRincewind.decryptString(ccryptData, pass1, pass2);
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
                            lblRotationsDec.Text = "";
                            float gesRotations = 0;
                            foreach (int rotation in CRincewind.rotationsDec)
                            {
                                gesRotations += (float)rotation;
                                lblRotationsDec.Text += rotation.ToString() + ";";
                            }
                            gesRotations = gesRotations / CRincewind.rotationsDec.Count();
                            lblRotationsDec.Text = gesRotations.ToString() + "//" + lblRotationsDec.Text;
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
            if (checkBox1.Checked)
            {
                SRng=AppDomain.CurrentDomain.BaseDirectory + "\\LibRincewindRNG_QRNG-API.dll";
            }
            else
            {
                SRng = "";
            }
            this.libRincewind = new CRincewind(SEnc, SRng, IVSize);
        }

        private void lblRotationsDec_Click(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }
    }
}
