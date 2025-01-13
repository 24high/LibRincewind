using System;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using NAudio.Wave;

public class RC4PlusImproved
{
    private const int SBoxSize = 256;
    private const int BlockSize = 16;
    private const int DropBytes = 3072;
    private readonly byte[] S;
    private readonly byte[] encryptionKey;
    private readonly SecureCSPRNG rng;

    public class SecureCSPRNG
    {
        private RandomNumberGenerator rng;

        public SecureCSPRNG()
        {
            rng = RandomNumberGenerator.Create();
        }

        public byte[] GetBytes(int length)
        {
            byte[] data = new byte[length];
            rng.GetBytes(data);

            // Weißes Rauschen von der Soundkarte hinzufügen
            byte[] whiteNoise = GetWhiteNoiseFromSoundCard(length);
            for (int i = 0; i < length; i++)
            {
                data[i] ^= whiteNoise[i];
            }

            return data;
        }

        private byte[] GetWhiteNoiseFromSoundCard(int length)
        {
            byte[] buffer = new byte[length];

            using (var waveIn = new WaveInEvent())
            {
                waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
                waveIn.DataAvailable += (s, e) =>
                {
                    Array.Copy(e.Buffer, buffer, length);
                    waveIn.StopRecording();
                };
                waveIn.StartRecording();
                System.Threading.Thread.Sleep(100); // Kurz warten, um genügend Daten zu erfassen
            }

            return buffer;
        }
    }

    public RC4PlusImproved(byte[] key)
    {
        if (key.Length < 32)
            throw new ArgumentException("Key must be at least 32 bytes long.");

        // Zufälliges Salt generieren
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Schlüssel derivieren
        encryptionKey = DeriveKey(key, salt, 32);
        this.rng = new SecureCSPRNG();

        S = new byte[SBoxSize];
        Initialize();
    }

    private void Initialize()
    {
        int keyLength = encryptionKey.Length;

        // Initialisiere die S-Box mit kryptographisch sicheren Zufallszahlen
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(S);
        }

        int j = 0;
        for (int i = 0; i < SBoxSize; i++)
        {
            j = (j + S[i] + encryptionKey[i % keyLength]) % SBoxSize;
            Swap(ref S[i], ref S[j]);
        }

        // Key-Scheduling Drop-Bytes (zur Schwächung von Anfangskorrelationen)
        for (int k = 0; k < DropBytes * 2; k++)
        {
            int i = (k + 1) % SBoxSize;
            j = (j + S[i]) % SBoxSize;
            Swap(ref S[i], ref S[j]);
        }

        // Zusätzliche Durchmischung der S-Box
        AdditionalMixing();

        // Stärkere Durchmischung durch zusätzlichen Entropieeinschluss
        AdditionalRandomMixing();
    }


    private void Swap(ref byte a, ref byte b)
    {
        byte temp = a;
        a = b;
        b = temp;
    }

    private void AdditionalMixing()
    {
        int j = 0;
        for (int i = 0; i < SBoxSize; i++)
        {
            j = (i + S[i]) % SBoxSize;
            Swap(ref S[i], ref S[j]);
        }
        
        for (int i = 0; i < SBoxSize; i++)
        {
            j = (j + S[i] + i) % SBoxSize;
            Swap(ref S[i], ref S[j]);
        }
    }

    // Zusätzliche Mischung mit zufälliger Entropie
    private void AdditionalRandomMixing()
    {
        byte[] randomBytes = rng.GetBytes(SBoxSize);
        int j = 0;

        for (int i = 0; i < SBoxSize; i++)
        {
            j = (j + S[i] + randomBytes[i]) % SBoxSize;
            Swap(ref S[i], ref S[j]);
        }
    }

public class Argon2idHasher
{
    private readonly byte[] password;
    private readonly byte[] salt;
    private readonly int degreeOfParallelism;
    private readonly int memorySize;
    private readonly int iterations;

    public Argon2idHasher(byte[] password, byte[] salt, int degreeOfParallelism = 8, int memorySize = 65536, int iterations = 4)
    {
        this.password = password ?? throw new ArgumentNullException(nameof(password));
        this.salt = salt ?? throw new ArgumentNullException(nameof(salt));
        this.degreeOfParallelism = degreeOfParallelism;
        this.memorySize = memorySize;
        this.iterations = iterations;
    }

    public byte[] GetBytes(int keyLength)
    {
        using (var argon2 = new Argon2id(password))
        {
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = degreeOfParallelism;
            argon2.MemorySize = memorySize;
            argon2.Iterations = iterations;

            return argon2.GetBytes(keyLength);
        }
    }
}

private static byte[] DeriveKey(byte[] password, byte[] salt, int keyLength)
    {
        if (salt == null || salt.Length == 0)
        {
            salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
        }

        var argon2 = new  Argon2id(password)
        {
            Salt = salt,
            DegreeOfParallelism = 8,
            MemorySize = 65536, // Erhöhte Speicheranforderung
            Iterations = 4
        };

        return argon2.GetBytes(keyLength);
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

    private void IncrementCounter(byte[] counter)
    {
        for (int i = counter.Length - 1; i >= 0; i--)
        {
            if (++counter[i] != 0)
                break;
        }
    }
}
