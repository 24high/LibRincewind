/*
Copyright 2023 Dennis Michael Heine

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace LibRincewind_4._7._2
{
    /// <summary>
    /// Shared by all three IRng plugins. Linked into each plugin assembly rather than
    /// shipped as a fourth DLL, so a plugin stays a single self-contained file to deploy.
    ///
    /// WHY THE RAW SOURCE IS NEVER USED DIRECTLY
    /// -----------------------------------------
    /// A quantum RNG behind an HTTPS endpoint is somebody else's server. It sees every
    /// request, it knows exactly which bytes it handed you, and if it is compromised or
    /// simply broken it can hand you a constant. The same goes for a USB noise source
    /// with a failing diode. Feeding that straight into key material means trusting it
    /// completely.
    ///
    /// So the external bytes are never returned as-is. They are extracted and expanded
    /// against a fresh 32-byte seed from the OS CSPRNG:
    ///
    ///     prk      = HMAC-SHA256(key: localSeed, msg: externalEntropy | label)
    ///     block[i] = HMAC-SHA256(key: prk,       msg: BE64(i))
    ///
    /// This is HKDF. The result is unpredictable to the API operator, who does not know
    /// localSeed, and unpredictable to anyone who does not know the external bytes. The
    /// output can therefore never be *worse* than the OS CSPRNG alone, while a genuinely
    /// good external source still contributes everything it has. That is the only honest
    /// way to offer "use this exotic entropy source" as a feature.
    ///
    /// The original plugins returned the source's bytes verbatim, ignored min/max
    /// entirely, and silently left the tail of the buffer zero-filled whenever the source
    /// under-delivered. All three of those are fixed here.
    /// </summary>
    internal static class EntropyConditioner
    {
        /// <summary>
        /// Turns raw source bytes into <paramref name="num"/> bytes uniform over
        /// [min, max]. <paramref name="max"/> below 0 means 255.
        /// </summary>
        internal static byte[] Condition(byte[] external, string label, int num, int min, int max)
        {
            if (num < 0) throw new ArgumentOutOfRangeException("num", "Cannot generate a negative number of bytes.");
            if (max < 0) max = 255;
            if (min < 0 || max > 255 || min > max)
                throw new ArgumentOutOfRangeException("min", "Require 0 <= min <= max <= 255.");

            byte[] result = new byte[num];
            if (num == 0) return result;

            byte[] localSeed = new byte[32];
            using (var g = RandomNumberGenerator.Create()) g.GetBytes(localSeed);

            byte[] prk;
            using (var extract = new HMACSHA256(localSeed))
                prk = extract.ComputeHash(Concat(external ?? new byte[0], Encoding.ASCII.GetBytes(label)));
            Wipe(localSeed);

            int range = max - min + 1;
            // Largest multiple of range that fits in a byte; bytes at or above it are
            // discarded so the mapping stays exactly uniform. Plain b % range would make
            // the low end of the range more likely than the high end.
            int bound = (range == 256) ? 256 : (256 / range) * range;

            using (var expand = new HMACSHA256(prk))
            {
                byte[] counter = new byte[8];
                long i = 0;
                int filled = 0;

                while (filled < num)
                {
                    for (int b = 0; b < 8; b++) counter[b] = (byte)(i >> (8 * (7 - b)));
                    i++;

                    byte[] block = expand.ComputeHash(counter);
                    for (int b = 0; b < block.Length && filled < num; b++)
                    {
                        int v = block[b];
                        if (v < bound) result[filled++] = (byte)(min + (v % range));
                    }
                    Wipe(block);
                }
            }
            Wipe(prk);
            return result;
        }

        /// <summary>
        /// Resolves a plugin setting, most specific source first:
        /// the caller's <c>parameters</c> array, then app.config &lt;appSettings&gt;,
        /// then an environment variable, then <paramref name="fallback"/>.
        /// Returns null when nothing is configured and there is no fallback.
        /// </summary>
        internal static string Setting(object[] parameters, int index, string key, string fallback)
        {
            if (parameters != null && index < parameters.Length && parameters[index] != null)
            {
                string fromCaller = parameters[index] as string;
                if (!string.IsNullOrEmpty(fromCaller)) return fromCaller;
            }

            try
            {
                string fromConfig = ConfigurationManager.AppSettings[key];
                if (!string.IsNullOrEmpty(fromConfig)) return fromConfig;
            }
            catch (ConfigurationErrorsException)
            {
                // A malformed app.config must not silently decide our entropy source.
                throw;
            }

            string fromEnv = Environment.GetEnvironmentVariable(key.Replace('.', '_'));
            if (!string.IsNullOrEmpty(fromEnv)) return fromEnv;

            return fallback;
        }

        internal static int SettingInt(object[] parameters, int index, string key, int fallback)
        {
            string raw = Setting(parameters, index, key, null);
            if (string.IsNullOrEmpty(raw)) return fallback;

            int parsed;
            if (!int.TryParse(raw.Trim(), System.Globalization.NumberStyles.Integer,
                              System.Globalization.CultureInfo.InvariantCulture, out parsed))
                throw new ConfigurationErrorsException(key + " is not a valid integer: '" + raw + "'");
            return parsed;
        }

        /// <summary>
        /// How many raw bytes to pull from the source for a request of
        /// <paramref name="num"/> output bytes.
        ///
        /// At least 32, so even a one-byte request is backed by a full security level of
        /// source entropy. At most 1024, because the output is an HMAC-SHA256 expansion
        /// whose strength caps out at 256 bits — 8192 bits of seed is already far past the
        /// point where more source entropy buys anything. Without the cap, encrypting a
        /// megabyte would try to download a megabyte from the quantum API one HTTP request
        /// at a time, or pull it a byte at a time over a 9600-baud serial line.
        /// </summary>
        internal static int RawBytesNeeded(int num)
        {
            return Math.Min(1024, Math.Max(32, num));
        }

        internal static byte[] Concat(byte[] a, byte[] b)
        {
            byte[] r = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            return r;
        }

        internal static void Wipe(byte[] b)
        {
            if (b != null) Array.Clear(b, 0, b.Length);
        }

        /// <summary>Short, safe-to-log excerpt of a server response for error messages.</summary>
        internal static string Excerpt(string s)
        {
            if (s == null) return "<null>";
            s = s.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return s.Length <= 200 ? s : s.Substring(0, 200) + "...";
        }
    }
}
