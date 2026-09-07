using LibRincewind_4._7._2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Quantum_Pass_Password_Manager
{    
    public partial class Form1 : Form
    {
        
        // ------------------------------------------------------------------- cipher --

        private static CRincewind cipher;

        /// <summary>
        /// The one cipher instance for the whole process.
        ///
        /// Constructing a CRincewind loads the plugin assembly and immediately draws an IV,
        /// and with the quantum RNG selected that draw is an HTTP round trip to the LFDR
        /// service. The previous code built a new instance inside every single read and
        /// write of <see cref="Passwords.encodedPassword"/>, so copying one password to the
        /// clipboard meant loading a DLL and hitting the network.
        /// </summary>
        private static CRincewind Cipher
        {
            get
            {
                if (cipher == null) cipher = CreateCipher();
                return cipher;
            }
        }

        private static CRincewind CreateCipher()
        {
            string chaCha20 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                           "LibRincewindPlugin_ChaCha20_4.7.2.dll");
            string quantumRng = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                             "LibRincewindRNG_QRNG-API.dll");
            try
            {
                return new CRincewind(chaCha20, quantumRng, 96 / 8);   // ChaCha20 nonce: 96 bits
            }
            catch (Exception ex)
            {
                // CRincewind deliberately refuses to substitute another entropy source
                // behind the caller's back, so a QRNG outage surfaces here as an exception.
                // Choosing the OS CSPRNG instead is the application's call to make, and it
                // is made visibly rather than silently.
                MessageBox.Show(
                    "The quantum RNG could not be reached. This session draws its salts and " +
                    "nonces from the operating system CSPRNG instead.\r\n\r\n" + ex.Message,
                    "Quantum RNG unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return new CRincewind(chaCha20, "", 96 / 8);
            }
        }

        private class Passwords
        {
            public String title = "";
            public String username = "";
            public String description = "";
            private String _encodedPassword = "";

            /// <summary>True once a password has actually been stored for this entry.</summary>
            public bool HasPassword { get { return !String.IsNullOrEmpty(_encodedPassword); } }

            public String encodedPassword
            {
                get
                {
                    // An entry that never had a password set holds no envelope, and
                    // decryptString would reject the empty string as malformed.
                    if (!HasPassword) return "";

                    string envelope = _encodedPassword;

                    // The master password reaches the cipher as two wipeable char arrays and
                    // is zeroed again when the callback returns; it never becomes a String.
                    //
                    // A wrong master password does not fail here: it returns a different but
                    // equally plausible password of the same length. That is the point of the
                    // library — nothing in the vault lets an attacker recognise the right
                    // master password, so nothing here can warn the user either.
                    return Globals.UseMasterPassword(
                        (half1, half2) => Cipher.decryptString(envelope, half1, half2));
                }
                set
                {
                    if (String.IsNullOrEmpty(value))
                    {
                        _encodedPassword = "";
                        return;
                    }

                    string plaintext = value;
                    _encodedPassword = Globals.UseMasterPassword(
                        (half1, half2) => Cipher.encryptString(plaintext, half1, half2));
                }
            }
        }

        private Dictionary<String,List<Passwords>> passwordDB=new Dictionary<String,List<Passwords>>();

        public Form1()
        {
            InitializeComponent();
            menuStrip1.Renderer = new CustomizedMenuRenderer();
        }

        /// <summary>
        /// Asks for the master password before anything can be stored under it.
        ///
        /// It used to be the literal "sdfiojfdguidsfdfg" compiled into this file, which made
        /// every vault readable to anyone holding the source. There is no verifier to check
        /// the answer against and there cannot be one: a stored check value is exactly the
        /// oracle that makes brute force work, so a typo is only discovered by the entries
        /// coming back as nonsense.
        /// </summary>
        private bool AskForMasterPassword()
        {
            string master = "";
            while (true)
            {
                if (Globals.PasswordBox("Quantum Pass",
                        "Master password (entries are unrecoverable without it):",
                        ref master) != DialogResult.OK)
                    return false;

                if (!String.IsNullOrEmpty(master))
                {
                    Globals.SetMasterPassword(master);
                    return true;
                }

                MessageBox.Show(this, "The master password cannot be empty.", "Quantum Pass",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
                    txtPassword.Text = "";
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
            if (!AskForMasterPassword())
                Close();
        }

        /// <summary>Resolves the entry the form is currently editing, or null.</summary>
        private Passwords SelectedEntry()
        {
            string category = (String)listBox1.SelectedItem;
            if (category == null || !passwordDB.ContainsKey(category)) return null;

            return passwordDB[category].FirstOrDefault(a => a.title == txtTitle.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Passwords entry = SelectedEntry();
            if (entry == null) return;

            if (txtPassword.Text != "")
            {
                // LibRincewind's ciphertext is drawn from the same 95 printable ASCII
                // characters as its plaintext, and it takes nothing else. Catch that here
                // rather than letting the library throw at the user.
                if (!CRincewind.IsSupported(txtPassword.Text))
                {
                    MessageBox.Show(this,
                        "Passwords may only use printable ASCII (space to ~).\r\n\r\n" +
                        "Encoding anything else would add redundancy to the stored record, " +
                        "and redundancy is the key check this vault exists to avoid.",
                        "Unsupported characters", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    entry.encodedPassword = txtPassword.Text;
                }
                catch (Exception ex)
                {
                    // Salts and nonces come from the entropy source, which for the quantum
                    // RNG is a remote service that can go away between two saves. Report it
                    // instead of losing the entry to an unhandled exception dialog.
                    MessageBox.Show(this, ex.Message, "Could not store the password",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            entry.username = textBox2.Text;
            entry.description = textBox3.Text;

            // Keep a revealed password on screen; only clear the field when it is masked
            // anyway, where leaving the text behind would be invisible and confusing.
            if (!chkShowPassword.Checked)
                txtPassword.Text = "";
        }

        /// <summary>Wipes the clipboard 7 seconds after a copy, so the password does not linger.</summary>
        private void thrd()
        {
            System.Threading.Thread.Sleep(7000);
            try
            {
                // Clipboard access has to happen on the UI thread; Invoke throws if the form
                // was closed in the meantime, and an unhandled exception on a worker thread
                // takes the process down.
                Invoke(new Action(() => Clipboard.Clear()));
            }
            catch (Exception) { }
        }
        // [STAThread] used to sit here; it only has an effect on an entry point and said
        // nothing. The clipboard calls need an STA thread, and they get one because this
        // handler runs on the UI thread, which Program.Main marks as STA.
        private void button4_Click(object sender, EventArgs e)
        {
            Passwords entry = SelectedEntry();
            if (entry == null || !entry.HasPassword)
            {
                MessageBox.Show(this, "No password stored for this entry yet.", "Quantum Pass",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string plaintext;
            try
            {
                plaintext = entry.encodedPassword;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Could not read the password",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!CopySensitiveToClipboard(plaintext))
                return;

            new System.Threading.Thread(thrd) { IsBackground = true }.Start();
        }

        /// <summary>
        /// Puts text on the clipboard while opting out of everything Windows would otherwise
        /// do with it behind the user's back.
        ///
        /// Clearing after 7 seconds is not enough on its own: since Windows 10 1809 the
        /// clipboard history (Win+V) and the cloud clipboard keep their own copies, and those
        /// survive Clipboard.Clear(). The three formats below are the documented opt-outs.
        /// </summary>
        private bool CopySensitiveToClipboard(string text)
        {
            DataObject data = new DataObject();
            data.SetText(text);

            // Each flag is a DWORD 0. A MemoryStream is what the clipboard expects here;
            // handing over a byte[] would be wrapped in a serialised object instead.
            foreach (string format in new[] { "ExcludeClipboardContentFromMonitorProcessing",
                                              "CanIncludeInClipboardHistory",
                                              "CanUploadToCloudClipboard" })
                data.SetData(format, new MemoryStream(new byte[] { 0, 0, 0, 0 }));

            try
            {
                // copy: false leaves the data owned by this process, so it dies with the
                // application instead of outliving it on the clipboard. Paste still works
                // for as long as Quantum Pass is running, which is the intended window.
                Clipboard.SetDataObject(data, false);
                return true;
            }
            catch (Exception ex)
            {
                // Another process can hold the clipboard open and make this fail.
                MessageBox.Show(this, ex.Message, "Could not copy to the clipboard",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Nothing did this before: the master password stayed in the SecureString until
            // the process image was torn down, and any derived key stayed in the library's
            // cache. Neither survives now.
            Globals.ClearMasterPass();
            CRincewind.ClearKeyCache();
            base.OnFormClosed(e);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            chkShowPassword.Checked = false;
            txtPassword.Text = "";
            txtTitle.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            panel1.Enabled=false;
            listBox2.SelectedIndex = -1;
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtTitle.Text = (String)listBox2.SelectedItem ?? "";

            Passwords entry = SelectedEntry();
            if (entry != null)
            {
                textBox2.Text = entry.username;
                textBox3.Text = entry.description;
            }

            // The field used to be filled with the literal string "*****" as a stand-in for
            // "unchanged". That was ambiguous — anyone typing five asterisks stored them as
            // their password. Empty now means unchanged; the real password only appears when
            // it is explicitly asked for.
            RefreshPasswordField();

            // Cancel clears the panel and then deselects, which fires this handler last.
            // Enabling unconditionally re-armed the panel with no entry behind it.
            panel1.Enabled = listBox2.SelectedItem != null;
        }

        private void txtPassword_Enter(object sender, EventArgs e)
        {
            // Used to wipe the field on focus, which would now also wipe a password the user
            // had just revealed or generated.
        }

        // ------------------------------------------------------- show / generate ---

        /// <summary>
        /// Fills the password box according to the "Show password" checkbox: the stored
        /// password in clear when it is ticked, empty otherwise.
        ///
        /// Revealing costs one Argon2id pass, so it happens on demand rather than on every
        /// selection change.
        /// </summary>
        private void RefreshPasswordField()
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;

            if (!chkShowPassword.Checked)
            {
                txtPassword.Text = "";
                return;
            }

            Passwords entry = SelectedEntry();
            if (entry == null || !entry.HasPassword)
            {
                txtPassword.Text = "";
                return;
            }

            try
            {
                txtPassword.Text = entry.encodedPassword;
            }
            catch (Exception ex)
            {
                txtPassword.Text = "";
                chkShowPassword.Checked = false;
                MessageBox.Show(this, ex.Message, "Could not read the password",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void chkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            // Do not throw away something the user typed or just generated.
            if (chkShowPassword.Checked && txtPassword.Text != "")
            {
                txtPassword.UseSystemPasswordChar = false;
                return;
            }

            RefreshPasswordField();
        }

        /// <summary>
        /// Generates a password drawn uniformly from the full 95 printable ASCII characters —
        /// the very alphabet the cipher works over.
        ///
        /// That match is the point, not a detail. Decrypting a record under a wrong master
        /// password yields a uniform string over those 95 characters, so a password from the
        /// same distribution is indistinguishable from a wrong guess. Restrict the generator
        /// to, say, letters and digits and real entries would never contain '#' or '%' while
        /// wrong guesses would — which hands an attacker a free filter worth roughly 900:1 on
        /// a 16-character password.
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            const int MinLength = 12;
            const int MaxLength = 256;

            string answer = "20";
            while (true)
            {
                if (Globals.InputBox("Generate password",
                        "Length in characters (" + MinLength + " to " + MaxLength + "):",
                        ref answer) != DialogResult.OK)
                    return;

                int length;
                if (!int.TryParse(answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out length) ||
                    length < MinLength || length > MaxLength)
                {
                    MessageBox.Show(this,
                        "Please enter a whole number between " + MinLength + " and " + MaxLength + ".",
                        "Generate password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                try
                {
                    txtPassword.Text = GeneratePassword(length);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Could not generate a password",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Show what was generated — an unreadable new password is not reviewable, and
                // it still has to be saved explicitly.
                chkShowPassword.Checked = true;
                txtPassword.UseSystemPasswordChar = false;
                return;
            }
        }

        private static string GeneratePassword(int length)
        {
            // Touching Cipher first so the configured entropy source (quantum RNG when it is
            // reachable) is the one that generates, not just the one that encrypts.
            if (Cipher == null) throw new InvalidOperationException("No cipher available.");

            // QRNG rejection-samples down to [32,126], so every one of the 95 characters is
            // exactly equally likely — modulo alone would favour the low end.
            byte[] raw = CRincewind.QRNG(length, CRincewind.AlphabetMin, CRincewind.AlphabetMax);
            try
            {
                char[] chars = new char[length];
                for (int i = 0; i < length; i++) chars[i] = (char)raw[i];
                return new string(chars);
            }
            finally
            {
                Array.Clear(raw, 0, raw.Length);
            }
        }
    }
}
