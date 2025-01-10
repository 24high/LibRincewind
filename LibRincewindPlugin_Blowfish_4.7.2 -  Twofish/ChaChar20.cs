using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using NAudio.Wave;

namespace LibRincewindPlugin_Blowfish_4._7._2
{

    public class ChaCha20
    {
        private const int BlockSize = 512;  // ChaCha20 Blockgröße ist 64 Byte
        private const int StateSize = 16;  // ChaCha20 Zustand ist 16 Wörter (512 Bit)
        private static readonly HashSet<string> UsedNonces = new HashSet<string>();

        private readonly byte[] _key;
        private readonly byte[] _nonce;
        private uint[] _state;

        public ChaCha20(byte[] key, byte[] nonce)
        {
            if (key.Length != 32) throw new ArgumentException("Key must be 256 bits.");
            if (nonce.Length != 12) throw new ArgumentException("Nonce must be 96 bits.");

            // Absichern gegen erneuten Gebrauch der Nonce
            string nonceString = BitConverter.ToString(nonce);
            if (UsedNonces.Contains(nonceString))
            {
                throw new InvalidOperationException("Nonce has already been used. Each nonce must be unique.");
            }
            UsedNonces.Add(nonceString);

            _key = new byte[key.Length];
            Array.Copy(key, _key, key.Length);
            _nonce = new byte[nonce.Length];
            Array.Copy(nonce, _nonce, nonce.Length);

            _state = new uint[StateSize];
            InitializeState();
        }

        private void InitializeState()
        {
            _state[0] = 0x61707865;  // "expand 32-byte k"
            _state[1] = 0x3320646e;  // "k" "expand"
            _state[2] = 0x79622d32;  // " 32-byte" "key"
            _state[3] = 0x6b206574;  // "key" 32-bytes

            for (int i = 0; i < 8; i++)
            {
                _state[i + 4] = BitConverter.ToUInt32(_key, i * 4);
            }

            _state[12] = BitConverter.ToUInt32(_nonce, 0);
            _state[13] = BitConverter.ToUInt32(_nonce, 4);
            _state[14] = BitConverter.ToUInt32(_nonce, 8);
            _state[15] = 0;  // Block counter, initialisiert mit 0
        }

        private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
        {
            a += b; d ^= a; d = (d << 16) | (d >> (32 - 16));
            c += d; b ^= c; b = (b << 12) | (b >> (32 - 12));
            a += b; d ^= a; d = (d << 8) | (d >> (32 - 8));
            c += d; b ^= c; b = (b << 7) | (b >> (32 - 7));
        }

        private void Salsa20Block(ref byte[] output)
        {
            uint[] workingState = new uint[StateSize];
            Array.Copy(_state, workingState, StateSize);

            for (int i = 0; i < 20; i += 2)
            {
                QuarterRound(ref workingState[0], ref workingState[4], ref workingState[8], ref workingState[12]);
                QuarterRound(ref workingState[1], ref workingState[5], ref workingState[9], ref workingState[13]);
                QuarterRound(ref workingState[2], ref workingState[6], ref workingState[10], ref workingState[14]);
                QuarterRound(ref workingState[3], ref workingState[7], ref workingState[11], ref workingState[15]);

                QuarterRound(ref workingState[0], ref workingState[5], ref workingState[10], ref workingState[15]);
                QuarterRound(ref workingState[1], ref workingState[6], ref workingState[11], ref workingState[12]);
                QuarterRound(ref workingState[2], ref workingState[7], ref workingState[8], ref workingState[13]);
                QuarterRound(ref workingState[3], ref workingState[4], ref workingState[9], ref workingState[14]);
            }

            for (int i = 0; i < StateSize; i++)
            {
                uint val = workingState[i] + _state[i];
                byte[] bytes = BitConverter.GetBytes(val);
                Array.Copy(bytes, 0, output, i * 4, 4);
            }
        }

        public byte[] GetBytes(int length)
        {
            byte[] output = new byte[length];
            byte[] block = new byte[BlockSize];
            int offset = 0;
            int remaining = length;

            while (remaining > 0)
            {
                Salsa20Block(ref block);

                int blockSize = Math.Min(remaining, BlockSize);
                Array.Copy(block, 0, output, offset, blockSize);
                offset += blockSize;
                remaining -= blockSize;

                // Increment block counter
                _state[12]++;
                if (_state[12] == 0)
                {
                    _state[13]++;
                    if (_state[13] == 0)
                    {
                        _state[14]++;
                        if (_state[14] == 0)
                        {
                            _state[15]++;
                        }
                    }
                }
            }

            return output;
        }

        public byte[] EncryptDecrypt(byte[] data)
        {
            byte[] keystream = GetBytes(data.Length);
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ keystream[i]);
            }
            return result;
        }
    }

    public class AudioCSPRNG
    {
        private readonly RandomNumberGenerator _cryptoRng;

        public AudioCSPRNG()
        {
            _cryptoRng = RandomNumberGenerator.Create();
        }

        public byte[] GetRandomBytesFromAudio(int length)
        {
            byte[] randomBytes = new byte[length];
            byte[] audioBytes = new byte[length];
            byte[] cryptoBytes = new byte[length];

            using (var waveIn = new WaveInEvent())
            {
                waveIn.DeviceNumber = 0;
                waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
                waveIn.BufferMilliseconds = 50;

                waveIn.DataAvailable += (sender, e) =>
                {
                    int bytesToCopy = Math.Min(length, e.BytesRecorded);
                    Array.Copy(e.Buffer, 0, audioBytes, 0, bytesToCopy);
                };

                waveIn.StartRecording();
                System.Threading.Thread.Sleep(500);
                waveIn.StopRecording();
            }

            _cryptoRng.GetBytes(cryptoBytes);

            for (int i = 0; i < length; i++)
            {
                randomBytes[i] = (byte)(audioBytes[i] ^ cryptoBytes[i]);
            }

            return randomBytes;
        }
    }

    public class Program
    {
        public static void Main()
        {
            string message = "Dies ist eine Nachricht, die verschlüsselt werden soll!";
            byte[] key = Encoding.UTF8.GetBytes("MeinGeheimesPasswortMeinGeheimesPasswort"); // 256-bit Key (32 bytes)

            // Erzeuge eine zufällige Nonce aus der Audioquelle
            AudioCSPRNG audioRng = new AudioCSPRNG();
            byte[] nonce = audioRng.GetRandomBytesFromAudio(12); // 96-bit nonce (12 bytes)

            ChaCha20 chacha20 = new ChaCha20(key, nonce);

            byte[] encrypted = chacha20.EncryptDecrypt(Encoding.UTF8.GetBytes(message));
            Console.WriteLine("Verschlüsselt: " + Convert.ToBase64String(encrypted));

            byte[] decrypted = chacha20.EncryptDecrypt(encrypted);
            Console.WriteLine("Entschlüsselt: " + Encoding.UTF8.GetString(decrypted));
        }
    }
}
