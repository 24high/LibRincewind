/*
Copyright 2023 Dennis Michael Heine

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Globalization;
using System.IO.Ports;

namespace LibRincewind_4._7._2
{
    /// <summary>
    /// Entropy from a hardware noise source on a serial port.
    ///
    /// The device's bytes are mixed with the OS CSPRNG rather than used directly — see
    /// <see cref="EntropyConditioner"/>. A noise diode that has drifted or died does not
    /// announce itself; it just starts returning something less random than it used to.
    /// Mixing means such a failure degrades the output to "as good as the OS CSPRNG"
    /// instead of to "predictable".
    ///
    /// CONFIGURATION (each entry: parameters[i], else app.config appSettings, else an
    /// environment variable with dots replaced by underscores, else the default)
    ///   [0] LibRincewind.HardwareRng.Port      e.g. "COM3" or "/dev/ttyUSB0" — required
    ///   [1] LibRincewind.HardwareRng.BaudRate  default 9600
    ///   [2] LibRincewind.HardwareRng.Timeout   read timeout in ms, default 10000
    ///
    /// WHAT WAS BROKEN
    ///  * `do { b = comport.ReadByte(); ... } while (b != -1);` never terminated.
    ///    SerialPort.ReadByte() returns 0..255 and throws TimeoutException when nothing
    ///    arrives; it never returns -1. The loop condition was inverted, so the call spun
    ///    until the read timed out and this plugin could never produce a single byte.
    ///  * `b = (byte)(min + (((int)b) - min));` is the identity function — the min clamp
    ///    did nothing at all.
    ///  * `b = (byte)(((int)b) % max);` is off by one (it can never produce max itself) and
    ///    biased towards low values, and it ran only when max > -1, so the documented
    ///    "-1 means full range" convention was inverted.
    ///  * The port lived in a field, was never disposed, and leaked on any exception —
    ///    leaving the device locked for every later call.
    /// </summary>
    public class Rng : IRng
    {
        public byte[] genBytes(int num, int min, int max, object[] parameters)
        {
            if (num < 0) throw new ArgumentOutOfRangeException("num", "Cannot generate a negative number of bytes.");
            if (num == 0) return new byte[0];

            string portName = EntropyConditioner.Setting(parameters, 0, "LibRincewind.HardwareRng.Port", null);
            if (string.IsNullOrEmpty(portName))
                throw new InvalidOperationException(
                    "The hardware RNG plugin has no serial port configured. Set the appSettings key " +
                    "\"LibRincewind.HardwareRng.Port\" (e.g. \"COM3\"), or the " +
                    "LibRincewind_HardwareRng_Port environment variable, or pass it as parameters[0].");

            int baud = EntropyConditioner.SettingInt(parameters, 1, "LibRincewind.HardwareRng.BaudRate", 9600);
            int timeoutMs = EntropyConditioner.SettingInt(parameters, 2, "LibRincewind.HardwareRng.Timeout", 10000);

            int rawNeeded = EntropyConditioner.RawBytesNeeded(num);
            byte[] raw = ReadRaw(portName, baud, timeoutMs, rawNeeded);

            if (raw == null || raw.Length < rawNeeded)
                throw new InvalidOperationException(
                    "Hardware RNG on " + portName + " delivered only " +
                    (raw == null ? 0 : raw.Length) + " of the " + rawNeeded +
                    " bytes requested. Refusing to build key material from a short read.");

            try
            {
                return EntropyConditioner.Condition(raw, "LibRincewind-HardwareRNG", num, min, max);
            }
            finally
            {
                EntropyConditioner.Wipe(raw);
            }
        }

        /// <summary>
        /// Reads exactly <paramref name="count"/> bytes from the port. Overridable so the
        /// conditioning path can be tested without hardware attached.
        /// </summary>
        protected virtual byte[] ReadRaw(string portName, int baudRate, int timeoutMs, int count)
        {
            byte[] buffer = new byte[count];

            using (var port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One))
            {
                port.ReadTimeout = timeoutMs;
                port.Handshake = Handshake.None;

                try
                {
                    port.Open();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "Could not open serial port " + portName + " at " + baudRate + " baud: " + ex.Message, ex);
                }

                // Read() returns whatever has arrived so far, which is usually fewer bytes
                // than asked for, so it has to be called in a loop until the buffer is full.
                int filled = 0;
                while (filled < count)
                {
                    int read;
                    try
                    {
                        read = port.Read(buffer, filled, count - filled);
                    }
                    catch (TimeoutException ex)
                    {
                        throw new InvalidOperationException(
                            "Hardware RNG on " + portName + " stopped sending after " + filled + " of " +
                            count + " bytes (timeout " + timeoutMs.ToString(CultureInfo.InvariantCulture) +
                            " ms). Check the device.", ex);
                    }

                    if (read <= 0)
                        throw new InvalidOperationException(
                            "Hardware RNG on " + portName + " closed the stream after " + filled +
                            " of " + count + " bytes.");

                    filled += read;
                }
            }
            return buffer;
        }
    }
}
