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

namespace LibRincewind_4._7._2
{
    /// <summary>
    /// Entropy from the LFDR quantum random number service.
    ///
    /// The service's bytes are mixed with the OS CSPRNG rather than used directly — see
    /// <see cref="EntropyConditioner"/> for why that is not optional. Everything this
    /// plugin can get wrong now fails loudly instead of quietly returning zeros.
    ///
    /// CONFIGURATION (each entry: parameters[i], else app.config appSettings, else an
    /// environment variable with dots replaced by underscores, else the default)
    ///   [0] LibRincewind.Qrng.Url      default "https://lfdr.de/qrng_api/qrng?length={num}&amp;format=BINARY"
    ///   [1] LibRincewind.Qrng.Timeout  request timeout in ms, default 15000
    ///
    /// WHAT WAS BROKEN
    ///  * min and max were ignored completely, so the caller's requested range was a lie.
    ///    CRincewind.QRNG(n, 1, 254) got bytes outside [1,254] and now rejects them.
    ///  * The bit assembly `tmp |= value &lt;&lt; (7 - i)` assumed every token was exactly 8
    ///    characters; a shorter token silently produced a wrong byte.
    ///  * `ret.Substring(ret.IndexOf("[") + 1)` on a response with no '[' returns the whole
    ///    string, because IndexOf gives -1 — so an error page was parsed as if it were data.
    ///  * `bRet[counter++]` had no bound check: more tokens than requested threw
    ///    IndexOutOfRangeException, and fewer left the rest of the key material zero-filled.
    ///  * WebClient was never disposed and had no timeout.
    /// </summary>
    public class Rng : IRng
    {
        private const string DefaultUrl = "https://lfdr.de/qrng_api/qrng?length={num}&format=BINARY";

        public byte[] genBytes(int num, int min, int max, object[] parameters)
        {
            if (num < 0) throw new ArgumentOutOfRangeException("num", "Cannot generate a negative number of bytes.");
            if (num == 0) return new byte[0];

            int rawNeeded = EntropyConditioner.RawBytesNeeded(num);

            string url = EntropyConditioner
                .Setting(parameters, 0, "LibRincewind.Qrng.Url", DefaultUrl)
                .Replace("{num}", rawNeeded.ToString(CultureInfo.InvariantCulture));
            int timeoutMs = EntropyConditioner.SettingInt(parameters, 1, "LibRincewind.Qrng.Timeout", 15000);

            string response = Fetch(url, timeoutMs);
            byte[] raw = ParseBinaryArray(response);

            if (raw.Length < rawNeeded)
                throw new InvalidOperationException(
                    "Quantum RNG returned only " + raw.Length + " of the " + rawNeeded +
                    " bytes requested. Refusing to build key material from a short read. Response was: " +
                    EntropyConditioner.Excerpt(response));

            try
            {
                return EntropyConditioner.Condition(raw, "LibRincewind-QRNG-LFDR", num, min, max);
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
                        "Could not reach the quantum RNG service at " + url + ": " + ex.Message +
                        ". Point LibRincewind.Qrng.Url somewhere else, or pass \"\" as the RNG path " +
                        "to use the OS CSPRNG instead.", ex);
                }
            }
        }

        /// <summary>
        /// Parses a response of the shape <c>{... [01001011 11010010 ...] ...}</c> into bytes.
        /// Every token must be 1..8 binary digits; anything else means a malformed response,
        /// which is a reason to stop rather than something to skip past.
        /// </summary>
        internal static byte[] ParseBinaryArray(string response)
        {
            if (string.IsNullOrEmpty(response))
                throw new FormatException("Quantum RNG returned an empty response.");

            int open = response.IndexOf('[');
            int close = response.LastIndexOf(']');
            if (open < 0 || close < open)
                throw new FormatException(
                    "Quantum RNG response contains no [...] array; the endpoint most likely returned " +
                    "an error page. Response was: " + EntropyConditioner.Excerpt(response));

            string body = response.Substring(open + 1, close - open - 1);
            string[] tokens = body.Split(new[] { ' ', ',', '\t', '\r', '\n', '"' },
                                         StringSplitOptions.RemoveEmptyEntries);

            List<byte> bytes = new List<byte>(tokens.Length);
            foreach (string token in tokens)
            {
                if (token.Length < 1 || token.Length > 8)
                    throw new FormatException(
                        "Quantum RNG returned the token '" + token + "', which is not 1..8 binary digits. " +
                        "Response was: " + EntropyConditioner.Excerpt(response));

                int value = 0;
                foreach (char c in token)
                {
                    if (c != '0' && c != '1')
                        throw new FormatException(
                            "Quantum RNG returned the non-binary token '" + token + "'. Response was: " +
                            EntropyConditioner.Excerpt(response));

                    value = (value << 1) | (c - '0');   // left to right, no length assumption
                }
                bytes.Add((byte)value);
            }

            if (bytes.Count == 0)
                throw new FormatException(
                    "Quantum RNG returned an empty array. Response was: " + EntropyConditioner.Excerpt(response));

            return bytes.ToArray();
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
