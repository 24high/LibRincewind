using System;
using System.Security.Cryptography;
using System.Text;
using NAudio.Wave;

public class CustomCSPRNG
{
    private const int HashSize = 32; // SHA-256 has a 32-byte output size
    private const int MinKeyLength = 16;

    private readonly byte[] _key;
    private byte[] _state;
    private readonly HMAC _hmac;
    private readonly RandomNumberGenerator _cryptoRng;

    public CustomCSPRNG(byte[] key)
    {
        if (key.Length < MinKeyLength) throw new ArgumentException($"Key must be at least {MinKeyLength} bytes long.");
        _key = new byte[key.Length];
        Array.Copy(key, _key, key.Length);
        _state = new byte[HashSize];
        _hmac = new HMACSHA256(_key);
        _cryptoRng = RandomNumberGenerator.Create();
        InitializeState();
    }

    private void InitializeState()
    {
        byte[] initialState = _hmac.ComputeHash(_key);
        Array.Copy(initialState, _state, _state.Length);
    }

    public byte[] GetBytes(int length)
    {
        byte[] randomBytes = new byte[length];
        int offset = 0;

        while (offset < length)
        {
            _state = _hmac.ComputeHash(_state);
            int bytesToCopy = Math.Min(length - offset, _state.Length);
            Array.Copy(_state, 0, randomBytes, offset, bytesToCopy);
            offset += bytesToCopy;
        }

        return randomBytes;
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

    public byte GetByte()
    {
        return GetRandomBytesFromAudio(1)[0];
    }
}

public class RC4Plus
{
    private const int SBoxSize = 256;
    private const int BlockSize = 16;

    private readonly byte[] S;
    private readonly byte[] encryptionKey;
    private readonly byte[] prngKey;
    private readonly CustomCSPRNG rng;

    public RC4Plus(byte[] key)
    {
        // Split the key into two separate keys for encryption and PRNG
        prngKey = new byte[key.Length / 2];
        encryptionKey = new byte[key.Length / 2];
        Array.Copy(key, 0, prngKey, 0, prngKey.Length);
        Array.Copy(key, prngKey.Length, encryptionKey, 0, encryptionKey.Length);

        rng = new CustomCSPRNG(prngKey);
        S = new byte[SBoxSize];
        Initialize();
    }

    private void Initialize()
    {
        int keyLength = encryptionKey.Length;

        for (int i = 0; i < SBoxSize; i++)
        {
            S[i] = (byte)i;
        }

        int j = 0;
        for (int i = 0; i < SBoxSize; i++)
        {
            j = (j + S[i] + encryptionKey[i % keyLength]) % SBoxSize;
            Swap(ref S[i], ref S[j]);
        }

        AdditionalMixing();
    }

    private void Swap(ref byte a, ref byte b)
    {
        byte temp = a;
        a = b;
        b = temp;
    }

    private void AdditionalMixing()
    {
        for (int i = 0; i < SBoxSize; i++)
        {
            int j = (i + S[i]) % SBoxSize;
            Swap(ref S[i], ref S[j]);
        }

        for (int i = 0; i < SBoxSize; i++)
        {
            int j = (j + S[i] + i) % SBoxSize;
            Swap(ref S[i], ref S[j]);
        }
    }

    public byte[] EncryptDecrypt(byte[] data)
    {
        int i = 0;
        int j = 0;
        byte[] result = new byte[data.Length];

        for (int k = 0; k < data.Length; k++)
        {
            i = (i + 1) % SBoxSize;
            j = (j + S[i]) % SBoxSize;
            Swap(ref S[i], ref S[j]);

            byte K = S[(S[i] + S[j]) % SBoxSize];
            result[k] = (byte)(data[k] ^ K);
        }

        return result;
    }

    public byte[] EncryptDecryptWithCTR(byte[] data, byte[] iv)
    {
        byte[] result = new byte[data.Length];
        byte[] counter = new byte[BlockSize];
        byte[] encryptedCounter;

        Array.Copy(iv, counter, iv.Length);

        for (int i = 0; i < data.Length; i += BlockSize)
        {
            encryptedCounter = EncryptDecrypt(counter);

            for (int j = 0; j < BlockSize && i + j < data.Length; j++)
            {
                result[i + j] = (byte)(data[i + j] ^ encryptedCounter[j]);
            }

            IncrementCounter(counter);
        }

        return result;
    }

    private void IncrementCounter(byte[] counter)
    {
        for (int i = counter.Length - 1; i >= 0; i--)
        {
            if (++counter[i] != 0) break;
        }
    }

    public static void Main()
    {
        string message = "Dies ist eine Nachricht, die verschlüsselt werden soll!";
        byte[] key = Encoding.UTF8.GetBytes("MeinGeheimesPasswortMeinGeheimesPasswort"); // Key must be double length

        byte[] iv = new byte[16];
        CustomCSPRNG rng = new CustomCSPRNG(key);
        iv = rng.GetRandomBytesFromAudio(16);

        RC4Plus rc4Plus = new RC4Plus(key);

        byte[] encrypted = rc4Plus.EncryptDecryptWithCTR(Encoding.UTF8.GetBytes(message), iv);
        Console.WriteLine("Verschlüsselt: " + Convert.ToBase64String(encrypted));

        byte[] decrypted = rc4Plus.EncryptDecryptWithCTR(encrypted, iv);
        Console.WriteLine("Entschlüsselt: " + Encoding.UTF8.GetString(decrypted));
    }
}
