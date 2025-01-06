/*
Copyright 2023 Dennis Michael Heine

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using LibRincewind_4._7._2;
using ManyMonkeys.Cryptography;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Paddings;
using System.Diagnostics;

namespace LibRincewindPlugin_Blowfish_4._7._2
{
  public class Blowfish : IPlugin
  {
        public byte[] decrypt(byte[] data, string password, byte[] IV)
        {

            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, IV);
            byte[] key = pdb.GetBytes(64 / 8);


            List<byte> result = new List<byte>();
            for (int i = 0; i < data.Length / 16; i++)
            {
                List<byte> ret = new List<byte>();
                for (int z = i * 16; z < (i * 16) + 16; z++)
                    ret.Add(data[z]);
                TwofishEncryption rijndael = new TwofishEncryption(key.Length, ref key, ref IV, CipherMode.CBC, TwofishBase.EncryptionDirection.Decrypting);
                byte[] outputBuffer = new byte[ret.ToArray().Length];
                rijndael.TransformBlock(ret.ToArray(), 0, ret.ToArray().Length, outputBuffer, 0);
                result.AddRange(rijndael.TransformFinalBlock(outputBuffer, 0, outputBuffer.Length));
            }
            return result.ToArray();

        }

        public byte[] encrypt(byte[] data, string password, byte[] IV)
        {

            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, IV);
            byte[] key = pdb.GetBytes(64 / 8);


            List<byte> result = new List<byte>();
            for (int i = 0; i < data.Length / 16; i++)
            {
                List<byte> ret = new List<byte>();
                for (int z = i * 16; z < (i * 16) + 16; z++)
                    ret.Add(data[z]);
                TwofishEncryption rijndael = new TwofishEncryption(key.Length, ref key, ref IV, CipherMode.CBC, TwofishBase.EncryptionDirection.Encrypting);
                byte[] outputBuffer = new byte[ret.ToArray().Length];
                rijndael.TransformBlock(ret.ToArray(), 0, ret.ToArray().Length, outputBuffer, 0);
                result.AddRange(rijndael.TransformFinalBlock(outputBuffer, 0, outputBuffer.Length));
            }
            return result.ToArray();
        }

        public byte[] generateIV(int length)
    {
      byte[] iv = new byte[length];
      for (int index = 0; index < length; index++)
      {
                do
                {
                    long ticks = DateTime.Now.Ticks;
                    iv[index] = (byte)new Random((int)ticks).Next(1, (int)byte.MaxValue);
                    System.Threading.Thread.Sleep(new Random((int)DateTime.Now.Ticks).Next(0, 50));
                } while (iv[index] == 0);
      }
      return iv;
    }
  }
}
