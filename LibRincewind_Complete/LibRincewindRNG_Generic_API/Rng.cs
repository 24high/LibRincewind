/*
Copyright 2023 Dennis Michael Heine

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace LibRincewind_4._7._2
{
    /// <summary>
    /// Entropy from an arbitrary HTTP endpoint that returns decimal numbers, described by
    /// a URL template and a regular expression.
    ///
    /// Whatever the endpoint returns is mixed with the OS CSPRNG rather than used directly
    /// — see <see cref="EntropyConditioner"/>. That matters more here than anywhere else in
    /// the library, because this plugin will happily point at any server you name.
    ///
    /// CONFIGURATION (each entry: parameters[i], else app.config appSettings, else an
    /// environment variable with dots replaced by underscores)
    ///   [0] LibRincewind.GenericRng.Url       template, e.g.
    ///       "https://example.org/int?n={num}&amp;min={min}&amp;max={max}"
    ///   [1] LibRincewind.GenericRng.NumToken  placeholder for the count,   default "{num}"
    ///   [2] LibRincewind.GenericRng.MinToken  placeholder for the minimum, default "{min}"
    ///   [3] LibRincewind.GenericRng.MaxToken  placeholder for the maximum, default "{max}"
    ///   [4] LibRincewind.GenericRng.Regex     one number per hit,          default "\d{1,3}"
    ///                                         a capture group, if present, wins over the whole match
    ///   [5] LibRincewind.GenericRng.Timeout   request timeout in ms,       default 15000
    ///
    /// There is deliberately no default URL: a plugin that silently invents an entropy
    /// source is worse than one that tells you it is not configured.
    ///
    /// WHAT WAS BROKEN
    ///  * `(String)parameters[0]` was dereferenced unconditionally, but CRincewind.QRNG
    ///    declares `object[] parameters = null` and no caller ever passed one — so every
    ///    call from the library threw NullReferenceException. This plugin could never have
    ///    run in the shipped configuration.
    ///  * The min/max placeholders went into the URL but the response was never checked
    ///    against them, so a server ignoring the range silently poisoned the output.
    ///  * `bRet[counter++]` had no bound check, and a regex that under-matched left the
    ///    rest of the buffer zero-filled — silent weak entropy, the worst failure mode.
    ///  * `byte.Parse` threw OverflowException on any match above 255 instead of reporting
    ///    a misconfigured regex.
    ///  * WebClient was never disposed and had no timeout.
    /// </summary>
    public class Rng : IRng
    {
        public byte[] genBytes(int num, int min, int max, object[] parameters)
        {
            if (num < 0) throw new ArgumentOutOfRangeException("num", "Cannot generate a negative number of bytes.");
            if (num == 0) return new byte[0];

            string urlTemplate = EntropyConditioner.Setting(parameters, 0, "LibRincewind.GenericRng.Url", null);
            if (string.IsNullOrEmpty(urlTemplate))
                throw new InvalidOperationException(
                    "The generic RNG plugin has no URL configured. Set the appSettings key " +
                    "\"LibRincewind.GenericRng.Url\", or the LibRincewind_GenericRng_Url environment " +
                    "variable, or pass it as parameters[0]. This plugin has no default endpoint on purpose.");

            string numToken = EntropyConditioner.Setting(parameters, 1, "LibRincewind.GenericRng.NumToken", "{num}");
            string minToken = EntropyConditioner.Setting(parameters, 2, "LibRincewind.GenericRng.MinToken", "{min}");
            string maxToken = EntropyConditioner.Setting(parameters, 3, "LibRincewind.GenericRng.MaxToken", "{max}");
            string pattern  = EntropyConditioner.Setting(parameters, 4, "LibRincewind.GenericRng.Regex", @"\d{1,3}");
            int timeoutMs   = EntropyConditioner.SettingInt(parameters, 5, "LibRincewind.GenericRng.Timeout", 15000);

            int rawNeeded = EntropyConditioner.RawBytesNeeded(num);

            // The endpoint is always asked for full-range bytes. Narrowing to [min,max] is
            // the conditioner's job, so a server that ignores the range cannot bias us.
            string url = urlTemplate
                .Replace(numToken, rawNeeded.ToString(CultureInfo.InvariantCulture))
                .Replace(minToken, "0")
                .Replace(maxToken, "255");

            string response = Fetch(url, timeoutMs);
            byte[] raw = ParseNumbers(response, pattern);

            if (raw.Length < rawNeeded)
                throw new InvalidOperationException(
                    "Generic RNG endpoint yielded only " + raw.Length + " of the " + rawNeeded +
                    " values requested. Refusing to build key material from a short read. Check the " +
                    "URL template and the regex. Response was: " + EntropyConditioner.Excerpt(response));

            try
            {
                return EntropyConditioner.Condition(raw, "LibRincewind-GenericRNG", num, min, max);
            }
            finally
            {
                EntropyConditioner.Wipe(raw);
            }
        }

        /// <summary>Overridable so the parser and the conditioner can be tested without a network.</summary>
        protected virtual string Fetch(string url, int timeoutMs)
        {
            using (var client = new TimeoutWebClient(timeoutMs))
            {
                try
                {
                    return client.DownloadString(url);
                }
                catch (WebException ex)
                {
                    throw new InvalidOperationException(
                        "Could not reach the generic RNG endpoint at " + url + ": " + ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// Extracts one number per regex hit, using capture group 1 when the pattern defines
        /// one and the whole match otherwise. Every hit must be an integer in [0,255]; a hit
        /// outside that range means the pattern is matching the wrong part of the document,
        /// and quietly clamping it would be inventing entropy.
        /// </summary>
        internal static byte[] ParseNumbers(string response, string pattern)
        {
            if (string.IsNullOrEmpty(response))
                throw new FormatException("Generic RNG endpoint returned an empty response.");

            MatchCollection matches;
            try
            {
                matches = Regex.Matches(response, pattern, RegexOptions.None, TimeSpan.FromSeconds(5));
            }
            catch (ArgumentException ex)
            {
                throw new FormatException("LibRincewind.GenericRng.Regex is not a valid pattern: " + ex.Message, ex);
            }
            catch (RegexMatchTimeoutException ex)
            {
                throw new FormatException(
                    "LibRincewind.GenericRng.Regex took too long against the response; it is probably " +
                    "backtracking catastrophically. Simplify the pattern.", ex);
            }

            List<byte> values = new List<byte>(matches.Count);
            foreach (Match m in matches)
            {
                string text = (m.Groups.Count > 1 && m.Groups[1].Success) ? m.Groups[1].Value : m.Value;

                int v;
                if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                    throw new FormatException(
                        "The regex matched '" + text + "', which is not an integer. Response was: " +
                        EntropyConditioner.Excerpt(response));

                if (v < 0 || v > 255)
                    throw new FormatException(
                        "The regex matched " + v + ", outside the byte range 0..255. The pattern is " +
                        "probably matching something other than the random values. Response was: " +
                        EntropyConditioner.Excerpt(response));

                values.Add((byte)v);
            }

            if (values.Count == 0)
                throw new FormatException(
                    "The regex matched nothing in the response. Response was: " +
                    EntropyConditioner.Excerpt(response));

            return values.ToArray();
        }

        private sealed class TimeoutWebClient : WebClient
        {
            private readonly int _timeoutMs;
            internal TimeoutWebClient(int timeoutMs) { _timeoutMs = timeoutMs; }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest r = base.GetWebRequest(address);
                if (r != null) r.Timeout = _timeoutMs;
                return r;
            }
        }
    }
}
