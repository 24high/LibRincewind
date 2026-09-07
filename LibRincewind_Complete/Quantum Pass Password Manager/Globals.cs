using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Security;
using System.Drawing;
using System.Windows.Forms;

namespace Quantum_Pass_Password_Manager
{
    public static class Globals
    {

        // Private: nothing outside this class has any business reading the raw store, and it
        // used to be public static, reachable from every line of code in the process.
        private static readonly SecureString _MasterPass = new SecureString();

        /// <summary>Zeroes the stored master password. Called when the vault closes.</summary>
        public static void ClearMasterPass()
        {
            _MasterPass.Clear();
        }

        /// <summary>
        /// Stores the master password.
        ///
        /// The incoming string cannot be wiped, and there is no way around that with a plain
        /// WinForms TextBox: its text is a String before we ever see it. This is now the only
        /// unerasable copy, made once at the prompt — it used to be joined by three more on
        /// every single encrypt and decrypt.
        /// </summary>
        public static void SetMasterPassword(string value)
        {
            _MasterPass.Clear();
            if (value == null) return;

            foreach (char c in value)
                _MasterPass.AppendChar(c);
        }

        /// <summary>
        /// Hands the master password to <paramref name="use"/> as two wipeable character
        /// arrays — the two halves CRincewind expects — and zeroes them again afterwards.
        ///
        /// This replaces a String-returning property. .NET strings are immutable, the GC does
        /// not zero what it collects, and a compaction may move a string and leave the old
        /// copy behind, so a String password is unerasable by construction. On this path the
        /// password exists only as char[] and as the BSTR, both of which are zeroed in a
        /// finally block, so nothing survives the call.
        /// </summary>
        public static T UseMasterPassword<T>(Func<char[], char[], T> use)
        {
            if (use == null) throw new ArgumentNullException("use");

            char[] all = ToCharArray(_MasterPass);
            char[] half1 = null, half2 = null;
            try
            {
                int split = all.Length / 2;
                half1 = new char[split];
                half2 = new char[all.Length - split];
                Array.Copy(all, 0, half1, 0, half1.Length);
                Array.Copy(all, split, half2, 0, half2.Length);

                return use(half1, half2);
            }
            finally
            {
                Wipe(all);
                Wipe(half1);
                Wipe(half2);
            }
        }

        /// <summary>
        /// Reads a SecureString into a char[] without going through Marshal.PtrToStringBSTR,
        /// which would allocate exactly the String this whole exercise is avoiding.
        /// </summary>
        private static char[] ToCharArray(SecureString s)
        {
            IntPtr bstr = Marshal.SecureStringToBSTR(s);
            try
            {
                char[] chars = new char[s.Length];
                for (int i = 0; i < chars.Length; i++)
                    chars[i] = (char)Marshal.ReadInt16(bstr, i * 2);
                return chars;
            }
            finally
            {
                Marshal.ZeroFreeBSTR(bstr);
            }
        }

        private static void Wipe(char[] c)
        {
            if (c != null) Array.Clear(c, 0, c.Length);
        }


        /// <summary>
        /// InputBox with the entry masked. Used for the master password, which must not be
        /// readable over the user's shoulder.
        /// </summary>
        public static DialogResult PasswordBox(string title, string promptText, ref string value)
        {
            return InputBox(title, promptText, ref value, true);
        }

        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            return InputBox(title, promptText, ref value, false);
        }

        private static DialogResult InputBox(string title, string promptText, ref string value, bool masked)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;
            textBox.UseSystemPasswordChar = masked;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

    }
}
