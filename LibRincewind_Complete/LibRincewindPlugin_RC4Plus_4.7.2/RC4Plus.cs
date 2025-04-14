
using System;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using NAudio.Wave;
using System.Threading;

public class RC4PlusImproved
{
    private const int SBoxSize = 256;
    private const int BlockSize = 16;
    private const int DropBytes = 3072;
    private readonly byte[] S = new byte[SBoxSize];
    private readonly byte[] encryptionKey;
    private readonly SecureCSPRNG rng;

    /// <summary>
    /// Erzeugt eine neue Instanz von RC4PlusImproved.
    /// </summary>
    /// <param name="key">Der geheime Schlüssel (mindestens 32 Bytes).</param>
    /// <param name="salt">Salt-Wert. Falls null oder leer, wird ein neues Salt erzeugt.</param>
    public RC4PlusImproved(byte[] key, byte[] salt, byte[] salt1, byte[] salt2)
    {
        if (key.Length < 32)
            throw new ArgumentException("Key must be at least 32 bytes long.");

        
        // Ableitung des Schlüssels mit Argon2id
        encryptionKey = DeriveKey(key, salt, 32);
        rng = new SecureCSPRNG();

        // Initialisiere die S-Box mit Werten 0 bis 255
        for (int i = 0; i < SBoxSize; i++)
        {
            S[i] = (byte)i;
        }

        Initialize(salt2);
    }

    /// <summary>
    /// Führt die Initialisierung und Permutation der S-Box durch.
    /// </summary>
    private void Initialize(byte[] salt2)
    {
        int keyLength = encryptionKey.Length;
        int j = 0;

        // Erste Permutation (Key-Scheduling)
        for (int i = 0; i < SBoxSize; i++)
        {
            j = (j + S[i] + encryptionKey[i % keyLength]) % SBoxSize;
            Swap(ref S[i], ref S[j]);
        }

        // Dropping-Phase: Schwächung von Anfangskorrelationen
        for (int k = 0; k < DropBytes * 2; k++)
        {
            int i = (k + 1) % SBoxSize;
            j = (j + S[i]) % SBoxSize;
            Swap(ref S[i], ref S[j]);
        }

        // Zusätzliche Durchmischung der S-Box
        AdditionalMixing();

        // Optional: Zusätzliche zufällige Durchmischung (falls benötigt)
        AdditionalRandomMixing(salt2);
    }

    /// <summary>
    /// Tauscht zwei Byte-Werte.
    /// </summary>
    private void Swap(ref byte a, ref byte b)
    {
        byte temp = a;
        a = b;
        b = temp;
    }

    /// <summary>
    /// Führt zusätzliche deterministische Durchmischungsschritte der S-Box durch.
    /// </summary>
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

    /// <summary>
    /// Führt eine zusätzliche zufallsbasierte Durchmischung der S-Box durch.
    /// </summary>
    private void AdditionalRandomMixing(byte[] salt2)
    {
        byte[] randomBytes = salt2;//rng.GetBytes(SBoxSize);
        int j = 0;
        for (int i = 0; i < SBoxSize; i++)
        {
            j = (j + S[i] + randomBytes[i]) % SBoxSize;
            Swap(ref S[i], ref S[j]);
        }
    }

    /// <summary>
    /// Verschlüsselt oder entschlüsselt Daten (symmetrische Operation).
    /// </summary>
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

    /// <summary>
    /// Leitet einen Schlüssel mit Argon2id ab.
    /// </summary>
    private static byte[] DeriveKey(byte[] password, byte[] salt, int keyLength)
    {
        // Absicherung: Falls kein Salt übergeben wird, neues Salt generieren.
        if (salt == null || salt.Length == 0)
        {
            salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
        }

        using (var argon2 = new Argon2id(password))
        {
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = 8;
            argon2.MemorySize = 65536; // Speichergröße in KB
            argon2.Iterations = 4;
            return argon2.GetBytes(keyLength);
        }
    }

    /// <summary>
    /// Eine Klasse zur Erzeugung kryptographisch starker Zufallszahlen, die zusätzlich weißes Rauschen von der Soundkarte bezieht.
    /// </summary>
    public class SecureCSPRNG
    {
        private readonly RandomNumberGenerator rng;

        public SecureCSPRNG()
        {
            rng = RandomNumberGenerator.Create();
        }

        /// <summary>
        /// Liefert ein Byte-Array der angegebenen Länge.
        /// </summary>
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

        /// <summary>
        /// Erfasst Daten von der Soundkarte, um zusätzliches Rauschen zu generieren.
        /// </summary>
        private byte[] GetWhiteNoiseFromSoundCard(int length)
        {
            byte[] buffer = new byte[length];
            using (var waveIn = new WaveInEvent())
            {
                waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
                waveIn.DataAvailable += (s, e) =>
                {
                    int bytesToCopy = Math.Min(length, e.BytesRecorded);
                    Array.Copy(e.Buffer, 0, buffer, 0, bytesToCopy);
                    waveIn.StopRecording();
                };
                waveIn.StartRecording();
                // Kurze Wartezeit, um Daten zu erfassen
                Thread.Sleep(100);
            }
            return buffer;
        }
    }

}