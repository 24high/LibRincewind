// Decompiled with JetBrains decompiler
// Type: LibRincewindDemo_4._7._2.Form1
// Assembly: LibRincewindDemo_4.7.2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 41823D56-9D35-41B9-9885-0F8FA7853323
// Assembly location: C:\Users\denni\Downloads\LibRincewind-main\LibRincewind-main\Demo\LibRincewindDemo_4.7.2.exe

using LibRincewind_4._7._2;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        String SRng = "";
        String SEnc = "";
        private Label lblRotationsDec;
        private Label lblRotationsEnc;
        private Label label7;
        private Label label10;
        private Label label11;

        /// <summary>IV length handed to CRincewind. Must match what the selected plugin expects.</summary>
        int IVSize = 16;

    public Form1()
    {
        this.InitializeComponent();

        // radioButton2 (RC4Plus) is the checked one in the designer, so start there.
        // Selecting it here rather than duplicating the wiring keeps the two in step;
        // the old code built the cipher with an IV size of 64 that no radio button
        // ever produced again.
        SelectRC4Plus();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
    }

        private static string PluginPath(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }

        private void RebuildCipher()
        {
            this.libRincewind = new CRincewind(SEnc, SRng, IVSize);
        }

        private void SelectRC4Plus()
        {
            IVSize = 16;                       // RC4Plus stretches the IV as a PBKDF2 salt
            SEnc = PluginPath("LibRincewindPlugin_RC4Plus_4.7.2.dll");
            RebuildCipher();
        }

        private void SelectChaCha20()
        {
            IVSize = 96 / 8;                   // ChaCha20 nonces are exactly 96 bits
            SEnc = PluginPath("LibRincewindPlugin_ChaCha20_4.7.2.dll");
            RebuildCipher();
        }

        /// <summary>
        /// Renders a keystream as "mean // d0;d1;...". For a correct build the mean sits
        /// near 47, the midpoint of the 95-character alphabet — a visibly skewed mean would
        /// mean the digit stream is biased and the ciphertext leaks plaintext structure.
        /// </summary>
        private static string FormatKeystream(int[] digits)
        {
            if (digits == null || digits.Length == 0) return "";

            double sum = 0;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < digits.Length; i++)
            {
                sum += digits[i];
                if (i > 0) sb.Append(';');
                sb.Append(digits[i].ToString(CultureInfo.InvariantCulture));
            }
            return (sum / digits.Length).ToString("F1", CultureInfo.InvariantCulture) + " // " + sb;
        }

        /// <summary>Pulls the Argon2id cost out of an RW6 envelope for display.</summary>
        private static string KdfParamsOf(string envelope)
        {
            string[] parts = envelope.Split('.');
            return parts.Length == 6 ? parts[4] : "";
        }

        private void ShowCipherError(string caption, Exception ex)
        {
            MessageBox.Show(this, ex.Message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void button1_Click(object sender, EventArgs e)
    {
            string plaintext = this.textBox3.Text;

            // The cipher is a bijection on printable ASCII and nothing else. Ask before
            // encrypting rather than letting the library throw into an unhandled dialog.
            if (!CRincewind.IsSupported(plaintext))
            {
                MessageBox.Show(this,
                    "LibRincewind encrypts printable ASCII (U+0020..U+007E) only.\r\n\r\n" +
                    "Encoding anything else would add redundancy to the ciphertext, and " +
                    "redundancy is exactly the key check this cipher exists to remove.",
                    "Unsupported characters", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Salts and IV live inside the envelope; the library draws fresh ones per
                // message. The old code generated 256-byte salts here and threw them away.
                this.textBox4.Text = this.libRincewind.encryptString(plaintext, "", this.textBox2.Text);
            }
            catch (Exception ex)
            {
                ShowCipherError("Encryption failed", ex);
                return;
            }

            this.textBox5.Text = KdfParamsOf(this.textBox4.Text);
            lblRotationsEnc.Text = FormatKeystream(CRincewind.rotations);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Decrypt what is on screen, so the envelope can be edited by hand to watch a
            // tampered ciphertext decrypt to garbage without complaint.
            string envelope = this.textBox4.Text;
            if (string.IsNullOrEmpty(envelope))
            {
                MessageBox.Show(this, "Encrypt something first.", "Nothing to decrypt",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // A wrong password does not throw here: it returns a plausible printable
                // string of the right length. That is the whole point of the construction.
                this.textBox6.Text = this.libRincewind.decryptString(envelope, "", this.textBox2.Text);
            }
            catch (Exception ex)
            {
                ShowCipherError("Decryption failed", ex);
                return;
            }

            lblRotationsDec.Text = FormatKeystream(CRincewind.rotationsDec);
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
            this.label5.Text = "KDF parameters:";
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
            this.textBox5.ReadOnly = true;
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
            this.label8.Text = "Last candidate";
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
            this.groupBox3.Text = "Brute force: wrong passwords that look wrong";
            this.groupBox3.Enter += new System.EventHandler(this.groupBox3_Enter);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(67, 299);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(144, 25);
            this.label10.TabIndex = 27;
            this.label10.Text = "Keystream (dec)";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(66, 262);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(144, 25);
            this.label11.TabIndex = 26;
            this.label11.Text = "Keystream (enc)";
            // 
            // lblRotationsDec
            // 
            this.lblRotationsDec.AutoSize = true;
            this.lblRotationsDec.Location = new System.Drawing.Point(222, 299);
            this.lblRotationsDec.Name = "lblRotationsDec";
            this.lblRotationsDec.Size = new System.Drawing.Size(144, 25);
            this.lblRotationsDec.TabIndex = 25;
            this.lblRotationsDec.Text = "";
            this.lblRotationsDec.Click += new System.EventHandler(this.lblRotationsDec_Click);
            // 
            // lblRotationsEnc
            // 
            this.lblRotationsEnc.AutoSize = true;
            this.lblRotationsEnc.Location = new System.Drawing.Point(222, 262);
            this.lblRotationsEnc.Name = "lblRotationsEnc";
            this.lblRotationsEnc.Size = new System.Drawing.Size(144, 25);
            this.lblRotationsEnc.TabIndex = 24;
            this.lblRotationsEnc.Text = "";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(66, 218);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(102, 25);
            this.label7.TabIndex = 23;
            this.label7.Text = "Outside alphabet";
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
                SelectRC4Plus();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
                SelectChaCha20();
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }
        private volatile bool running = false;

        /// <summary>
        /// Brute-force panel: decrypt the current envelope under a stream of random wrong
        /// passwords and count how many of the results fall outside the printable alphabet.
        ///
        /// The counter is supposed to stay at 0/n forever. Every wrong password must yield a
        /// string that looks exactly as plausible as the right one, because there is no
        /// checksum, no MAC and no padding for an attacker to test against. A single hit
        /// would mean the ciphertext leaks a way to recognise the correct key.
        ///
        /// The rate you see is also the honest cost of a guess: one Argon2id pass over
        /// 64 MiB per candidate. There is no artificial delay in this loop.
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            if (running)
            {
                running = false;                 // the worker stops at the top of its next pass
                button3.Text = "Test";
                return;
            }

            string envelope = this.textBox4.Text;
            if (string.IsNullOrEmpty(envelope))
            {
                MessageBox.Show(this, "Encrypt something first, then run the test against it.",
                                "Nothing to attack", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            running = true;
            button3.Text = "Stop";

            Thread worker = new Thread(() => BruteForceLoop(envelope));
            worker.IsBackground = true;          // must not keep the process alive on close
            worker.Start();
        }

        private void BruteForceLoop(string envelope)
        {
            long tries = 0;
            long outsideAlphabet = 0;

            while (running)
            {
                string candidate;
                try
                {
                    candidate = this.libRincewind.decryptString(envelope, "", RandomString(10));
                }
                catch (Exception)
                {
                    break;                       // e.g. the plugin was swapped mid-run
                }

                tries++;
                if (!CRincewind.IsSupported(candidate))
                    outsideAlphabet++;

                int[] keystream = CRincewind.rotationsDec;
                long shownTries = tries, shownOutside = outsideAlphabet;
                try
                {
                    this.textBox8.Invoke(new Action(() =>
                    {
                        this.label9.Text = shownOutside.ToString(CultureInfo.InvariantCulture) + "/" +
                                           shownTries.ToString(CultureInfo.InvariantCulture);
                        this.textBox8.Text = candidate;
                        lblRotationsDec.Text = FormatKeystream(keystream);
                    }));
                }
                catch (Exception)
                {
                    break;                       // the form went away underneath us
                }
            }

            running = false;
            try
            {
                button3.Invoke(new Action(() => button3.Text = "Test"));
            }
            catch (Exception) { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            running = false;
            base.OnFormClosing(e);
        }

        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            // CRincewind.Rng is static, so the constructor below switches the entropy source
            // process-wide. Unchecking goes back to the OS CSPRNG.
            SRng = checkBox1.Checked ? PluginPath("LibRincewindRNG_QRNG-API.dll") : "";

            try
            {
                RebuildCipher();
            }
            catch (Exception ex)
            {
                // The quantum RNG is a network service. If it cannot be loaded, say so and
                // fall back visibly rather than quietly encrypting with something else.
                SRng = "";
                RebuildCipher();
                checkBox1.Checked = false;
                ShowCipherError("Quantum RNG unavailable — using the OS CSPRNG", ex);
            }
        }

        private void lblRotationsDec_Click(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }
    }
}
