/*
Copyright 2023 Dennis Michael Heine

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// The keystream engine of the "RC4Plus" plugin.
///
/// DESPITE THE NAME, THIS IS NO LONGER RC4, AND ON PURPOSE.
/// -------------------------------------------------------
/// RC4's own keystream cannot be made secure — it has well-published classical
/// biases (Fluhrer–Mantin–Shamir key recovery, Mantin–Shamir P(z2=0)=2/256,
/// AlFardan et al. broadcast attacks). A cipher that is distinguishable today is
/// broken today, quantum computers or not, so "quantum-hardening RC4" is a
/// contradiction. The class name and assembly file name are kept only so the
/// existing plugin wiring (loaded by file name, instantiated by type name) keeps
/// working; the algorithm underneath has been replaced.
///
/// WHAT QUANTUM RESISTANCE MEANS FOR A SYMMETRIC CIPHER
/// ---------------------------------------------------
/// There is no "post-quantum stream cipher" the way there are post-quantum
/// signatures. The only quantum threat to a symmetric primitive is Grover's
/// algorithm, a quadratic speed-up on key search, and Grover is provably optimal
/// for black-box search. A key of k bits therefore keeps k/2 bits of security. So
/// "quantum resistant" here means one concrete, checkable thing: a security level
/// of at least 256 bits classically / 128 bits post-Grover, with no key or state
/// smaller than 256 bits anywhere in the pipeline. Shor's algorithm — the one that
/// destroys RSA and elliptic curves — needs an asymmetric problem to attack, and
/// this library has none, so it does not apply at all.
///
/// HEDGING AGAINST ATTACKS THAT DO NOT EXIST YET
/// --------------------------------------------
/// No one can prove a construction safe against cryptanalysis that has not been
/// invented. What is genuinely possible is a *combiner*: the keystream is the XOR
/// of two independent keystreams from two unrelated primitive families,
///
///     ks = AES-256-CTR(k_aes, iv_aes)  XOR  HMAC-SHA512-CTR(k_mac)
///          \___ substitution-permutation ___/     \___ Merkle-Damgard PRF ___/
///
/// A XOR of two keystreams is a secure generator if *at least one* of them is
/// secure (standard result). So if some future attack — quantum or classical —
/// structurally breaks AES, the HMAC-SHA512 stream still keeps the output
/// unpredictable, and vice versa. Both families would have to fall at once. This
/// is the same defence-in-depth reasoning behind hybrid post-quantum TLS
/// (X25519 + Kyber): not a guarantee against the unknown, which is impossible, but
/// the strongest hedge that is. Both halves are independently keyed via HKDF and
/// already sit at >=128-bit post-Grover on their own.
///
/// NOT AUTHENTICATED, BY DESIGN
/// ----------------------------
/// Like the rest of LibRincewind this is a bare keystream: no MAC, malleable,
/// deniable. Integrity protection is intentionally absent because it would be the
/// verifier that defeats the library's key-space obfuscation. See CRincewind.
///
/// BUGS IN THE ORIGINAL, NOW GONE
/// ------------------------------
///  * AdditionalRandomMixing indexed salt2[0..255] unconditionally, so any salt2
///    shorter than 256 bytes threw IndexOutOfRangeException. Because the rewritten
///    CRincewind passes 32-byte salts, the old code would have crashed on every
///    real use — the plugin only ever "worked" with the old 256-byte salts.
///  * DeriveKey generated a random salt when given none and then discarded it,
///    making that data undecryptable.
///  * AdditionalMixing ran 512 swap steps per output byte: ~113x slower than AES.
///  * The soundcard "white noise" RNG was quantum-cosplay and never actually fed
///    the keystream; it is removed.
/// </summary>
public class QuantumResistantRC4
{
    private const int MinKeyBytes = 32;                 // 256-bit floor -> 128-bit post-Grover
    private const string Label = "LibRincewind-RC4Plus/AES256CTR-xor-HMACSHA512CTR/v2";

    private readonly byte[] kAes;   // 32 bytes
    private readonly byte[] ivAes;  // 16 bytes, starting counter block
    private readonly byte[] kMac;   // 64 bytes

    /// <param name="key">Key material, at least 32 bytes. Callers that start from a
    /// human password must stretch it first (the IPlugin wrapper does).</param>
    /// <param name="salt">Diversifier folded into HKDF-Extract. Any length, may be
    /// null/empty (treated as all-zero, per HKDF).</param>
    /// <param name="nonce">Further diversifier folded into HKDF-Expand. Any length,
    /// may be null/empty. Two messages under one key MUST differ in salt or nonce,
    /// or they share a keystream.</param>
    public QuantumResistantRC4(byte[] key, byte[] salt, byte[] nonce)
    {
        if (key == null) throw new ArgumentNullException("key");
        if (key.Length < MinKeyBytes)
            throw new ArgumentException("Key must be at least " + MinKeyBytes +
                                        " bytes (256 bits) for post-quantum margin.", "key");

        // HKDF-SHA512. Extract concentrates the key material; Expand splits it into
        // two independent subkeys plus the AES starting counter, with the nonce and a
        // domain-separation label bound in so different messages never collide.
        byte[] prk = HkdfExtract(salt, key);
        try
        {
            byte[] info = Concat(Encoding.ASCII.GetBytes(Label), nonce ?? new byte[0]);
            byte[] okm = HkdfExpand(prk, info, 32 + 16 + 64);
            try
            {
                kAes = new byte[32]; Buffer.BlockCopy(okm, 0, kAes, 0, 32);
                ivAes = new byte[16]; Buffer.BlockCopy(okm, 32, ivAes, 0, 16);
                kMac = new byte[64]; Buffer.BlockCopy(okm, 48, kMac, 0, 64);
            }
            finally { Array.Clear(okm, 0, okm.Length); }
        }
        finally { Array.Clear(prk, 0, prk.Length); }
    }

    /// <summary>
    /// XORs the combined keystream into <paramref name="data"/>. Symmetric:
    /// encryption and decryption are the same call.
    /// </summary>
    public byte[] EncryptDecrypt(byte[] data)
    {
        if (data == null) throw new ArgumentNullException("data");
        int n = data.Length;
        byte[] result = new byte[n];
        if (n == 0) return result;

        byte[] aes = AesCtrKeystream(kAes, ivAes, n);
        byte[] mac = HmacCtrKeystream(kMac, n);
        try
        {
            for (int i = 0; i < n; i++)
                result[i] = (byte)(data[i] ^ aes[i] ^ mac[i]);
        }
        finally
        {
            Array.Clear(aes, 0, aes.Length);
            Array.Clear(mac, 0, mac.Length);
        }
        return result;
    }

    // ---- keystream A: AES-256 in counter mode (SPN family) ----------------------
    private static byte[] AesCtrKeystream(byte[] key, byte[] iv0, int n)
    {
        byte[] outp = new byte[n];
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.ECB;      // ECB of a counter == CTR keystream
            aes.Padding = PaddingMode.None;
            using (ICryptoTransform enc = aes.CreateEncryptor())
            {
                byte[] counter = (byte[])iv0.Clone();
                byte[] block = new byte[16];
                int pos = 0;
                while (pos < n)
                {
                    enc.TransformBlock(counter, 0, 16, block, 0);
                    int take = Math.Min(16, n - pos);
                    Buffer.BlockCopy(block, 0, outp, pos, take);
                    IncrementBE(counter);
                    pos += take;
                }
                Array.Clear(block, 0, block.Length);
            }
        }
        return outp;
    }

    // ---- keystream B: HMAC-SHA512 in counter mode (Merkle-Damgard family) --------
    private static byte[] HmacCtrKeystream(byte[] key, int n)
    {
        byte[] outp = new byte[n];
        using (var hmac = new HMACSHA512(key))
        {
            byte[] counter = new byte[8];
            long i = 0;
            int pos = 0;
            while (pos < n)
            {
                for (int b = 0; b < 8; b++) counter[b] = (byte)(i >> (8 * (7 - b)));
                byte[] block = hmac.ComputeHash(counter);
                int take = Math.Min(block.Length, n - pos);
                Buffer.BlockCopy(block, 0, outp, pos, take);
                Array.Clear(block, 0, block.Length);
                i++;
                pos += take;
            }
        }
        return outp;
    }

    // ---- HKDF-SHA512 (RFC 5869); not in the .NET 4.7.2 BCL, so implemented here ---
    private static byte[] HkdfExtract(byte[] salt, byte[] ikm)
    {
        byte[] key = (salt == null || salt.Length == 0) ? new byte[64] : salt;
        using (var hmac = new HMACSHA512(key))
            return hmac.ComputeHash(ikm);
    }

    private static byte[] HkdfExpand(byte[] prk, byte[] info, int length)
    {
        using (var hmac = new HMACSHA512(prk))
        {
            byte[] okm = new byte[length];
            byte[] t = new byte[0];
            int pos = 0;
            byte counter = 1;
            while (pos < length)
            {
                hmac.Initialize();
                byte[] input = new byte[t.Length + info.Length + 1];
                Buffer.BlockCopy(t, 0, input, 0, t.Length);
                Buffer.BlockCopy(info, 0, input, t.Length, info.Length);
                input[input.Length - 1] = counter;
                t = hmac.ComputeHash(input);

                int take = Math.Min(t.Length, length - pos);
                Buffer.BlockCopy(t, 0, okm, pos, take);
                pos += take;
                counter++;
            }
            return okm;
        }
    }

    private static void IncrementBE(byte[] counter)
    {
        for (int i = counter.Length - 1; i >= 0; i--)
            if (++counter[i] != 0) break;
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        byte[] r = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, r, 0, a.Length);
        Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
        return r;
    }
}
