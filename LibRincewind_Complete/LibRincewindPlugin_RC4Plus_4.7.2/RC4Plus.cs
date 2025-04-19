using System;
using System.Linq;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using NAudio.Wave;
using System.Threading;

public class QuantumResistantRC4
{
 private const int SBoxSize =256;
 private const int DropBytes =8192;
 private readonly byte[] S = new byte[SBoxSize];
 private readonly byte[] encryptionKey;
 private readonly SecureCSPRNG rng;

 public QuantumResistantRC4(byte[] key, byte[] salt, byte[] salt2)
 {
 if (key.Length <64)
 throw new ArgumentException("Key must be at least64 bytes long.");

 encryptionKey = DeriveKey(key, salt,64);
 rng = new SecureCSPRNG();

 for (int i =0; i < SBoxSize; i++)
 {
 S[i] = (byte)i;
 }

 Initialize(salt2);
 }

 private void Initialize(byte[] salt2)
 {
 RunKSA(encryptionKey);
 RunKSA(encryptionKey.Reverse().ToArray());
 RunKSA(encryptionKey.Select(b => (byte)(b ^0xFF)).ToArray());

 int j =0;
 for (int k =0; k < DropBytes *2; k++)
 {
 int i = (k +1) % SBoxSize;
 j = (j + S[i]) % SBoxSize;
 Swap(ref S[i], ref S[j]);
 }

 AdditionalMixing();
 AdditionalRandomMixing(salt2);
 }

 private void RunKSA(byte[] key)
 {
 int j =0;
 int keyLength = key.Length;

 for (int i =0; i < SBoxSize; i++)
 {
 j = (j + S[i] + key[i % keyLength]) % SBoxSize;
 Swap(ref S[i], ref S[j]);
 }
 }

 private void Swap(ref byte a, ref byte b)
 {
 byte temp = a;
 a = b;
 b = temp;
 }

 private void AdditionalMixing()
 {
 int j =0;
 for (int i =0; i < SBoxSize; i++)
 {
 j = (i + S[i]) % SBoxSize;
 Swap(ref S[i], ref S[j]);
 }

 for (int i =0; i < SBoxSize; i++)
 {
 j = (j + S[i] + i) % SBoxSize;
 Swap(ref S[i], ref S[j]);
 }
 }

 private void AdditionalRandomMixing(byte[] randomBytes)
 {
 int j =0;
 for (int i =0; i < SBoxSize; i++)
 {
 j = (j + S[i] + randomBytes[i]) % SBoxSize;
 Swap(ref S[i], ref S[j]);
 }
 }

 public byte[] EncryptDecrypt(byte[] data)
 {
 int i =0;
 int j =0;
 byte[] result = new byte[data.Length];

 for (int k =0; k < data.Length; k++)
 {
 i = (i +1) % SBoxSize;
 j = (j + S[i]) % SBoxSize;
 Swap(ref S[i], ref S[j]);
 byte K = S[(S[i] + S[j]) % SBoxSize];
 result[k] = (byte)(data[k] ^ K);

 AdditionalMixing();
 
 }

 return result;
 }

 private static byte[] DeriveKey(byte[] password, byte[] salt, int keyLength)
 {
 if (salt == null || salt.Length ==0)
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
 argon2.DegreeOfParallelism =8;
 argon2.MemorySize =65536;
 argon2.Iterations =4;
 return argon2.GetBytes(keyLength);
 }
 }

 public class SecureCSPRNG
 {
 private readonly RandomNumberGenerator rng;

 public SecureCSPRNG()
 {
 rng = RandomNumberGenerator.Create();
 }

 public byte[] GetBytes(int length)
 {
 byte[] data = new byte[length];
 rng.GetBytes(data);

 byte[] whiteNoise = GetWhiteNoiseFromSoundCard(length);
 for (int i =0; i < length; i++)
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
 waveIn.WaveFormat = new WaveFormat(44100,16,1);
 waveIn.DataAvailable += (s, e) =>
 {
 int bytesToCopy = Math.Min(length, e.BytesRecorded);
 Array.Copy(e.Buffer,0, buffer,0, bytesToCopy);
 waveIn.StopRecording();
 };
 waveIn.StartRecording();
 Thread.Sleep(100);
 }
 return buffer;
 }
 }
}