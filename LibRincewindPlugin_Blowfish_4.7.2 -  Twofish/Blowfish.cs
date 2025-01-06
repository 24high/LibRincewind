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
            byte[] key = pdb.GetBytes(256 / 8);

            Twofish algorithm = new Twofish();
            MemoryStream inCipherTextStream = new MemoryStream(data);
            MemoryStream outPlainTextStream = new MemoryStream();

            var decryptor = algorithm.CreateDecryptor(key,IV);
            var csRead = new CryptoStream(inCipherTextStream, decryptor, CryptoStreamMode.Read);
            try
            {
                csRead.CopyTo(outPlainTextStream);
            }
            catch
            {
                outPlainTextStream = inCipherTextStream;
            }


            // create an encoder

            // we have to work backwards defining the last link in the chain first


            return outPlainTextStream.ToArray();

        }

        public byte[] encrypt(byte[] data, string password, byte[] IV)
        {

            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, IV);
            byte[] key = pdb.GetBytes(256 / 8);

            Twofish algorithm = new Twofish();
            System.IO.MemoryStream outCipherTextStream = new System.IO.MemoryStream();

            ICryptoTransform encode = new ToBase64Transform();

            //create Twofish Encryptor from this instance

            var encryptor = new Twofish().CreateEncryptor(key, IV);
            var csWrite = new CryptoStream(outCipherTextStream, encryptor, CryptoStreamMode.Write);
            new MemoryStream(data).CopyTo(csWrite);
            return outCipherTextStream.ToArray();
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
