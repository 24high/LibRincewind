/*
Copyright 2023 Dennis Michael Heine

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;

namespace LibRincewind_4._7._2
{
    /// <summary>
    /// LibRincewind with the password DTE wired in — the combination a password
    /// manager should actually use.
    ///
    /// Plain <see cref="CRincewind"/> hides the key space only when its plaintext is
    /// already uniform over the printable alphabet. A stored password is not: it is a
    /// word and a year, so the right key produces something recognisable and every
    /// wrong key produces line noise. Running the password through
    /// <see cref="PasswordDte"/> first removes that tell — wrong keys then decode to
    /// other plausible passwords instead of to garbage.
    ///
    /// Measured on this construction (32 human passwords, 400 wrong guesses each,
    /// scored by a bigram model trained on a disjoint password corpus):
    ///
    ///     without the DTE   true password ranked #1 in 31 of 32 trials
    ///     with the DTE      true password ranked #1 in  0 of 32 trials
    ///
    /// See LibRincewindHoney/README.md for the full numbers, including the residual
    /// signal that remains when the vault's password mix differs from the DTE's model.
    ///
    /// Everything else about the library is unchanged, including the deliberate
    /// absence of integrity protection: this is confidentiality and deniability, not
    /// tamper detection.
    /// </summary>
    public sealed class HoneyVault
    {
        private readonly CRincewind core;

        /// <param name="pluginPath">Path to an IPlugin DLL, or "" for the built-in AES-CTR keystream.</param>
        /// <param name="rngPath">Path to an IRng DLL, or "" for the OS CSPRNG (the recommended default).</param>
        /// <param name="ivSize">IV length; must match the chosen plugin (ChaCha20: 12, RC4Plus: 16).</param>
        public HoneyVault(string pluginPath, string rngPath, int ivSize)
        {
            core = new CRincewind(pluginPath ?? "", rngPath ?? "", ivSize);
        }

        /// <summary>Built-in AES-CTR keystream over the OS CSPRNG.</summary>
        public HoneyVault() : this("", "", 16) { }

        /// <summary>
        /// Encrypts one stored password into a self-contained envelope. The envelope is
        /// the same length for every password, so it leaks nothing about how long the
        /// password is.
        /// </summary>
        public string Protect(string password, string key1, string key2)
        {
            if (password == null) throw new ArgumentNullException("password");
            return core.encryptString(PasswordDte.Encode(password), key1, key2);
        }

        /// <summary>
        /// Decrypts an envelope. Like the rest of the library this never signals a wrong
        /// key: a wrong key returns a different, plausible-looking password rather than
        /// an error. The caller cannot tell the difference, and neither can an attacker
        /// — that is the entire point.
        /// </summary>
        public string Reveal(string envelope, string key1, string key2)
        {
            if (envelope == null) throw new ArgumentNullException("envelope");
            return PasswordDte.Decode(core.decryptString(envelope, key1, key2));
        }
    }
}
