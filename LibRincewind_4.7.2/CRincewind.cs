/*
Copyright 2023 Dennis Michael Heine

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace LibRincewind_4._7._2
{
  public class CRincewind
  {
    private IPlugin plugin;
    public byte[] IV;

    public CRincewind(string _plugin, int ivSize = 512)
    {
      foreach (Type exportedType in Assembly.LoadFile(_plugin).GetExportedTypes())
      {
        if (((IEnumerable<Type>) exportedType.GetInterfaces()).Contains<Type>(typeof (IPlugin)))
          this.plugin = (IPlugin) Activator.CreateInstance(exportedType);
      }
      this.IV = this.plugin.generateIV(ivSize);
    }

    public CCryptData encryptCCD(string toEncrypt, string password1, string password2, String password3)
    {
              byte[] randomKey = this.generateRandomKey(toEncrypt);
              byte[] randomSeed=this.generateRandomKey(toEncrypt);              
              byte[] bytes = Encoding.ASCII.GetBytes(toEncrypt.ToCharArray());

              byte[] encryptedKey=new byte[randomKey.Length];

              

              for (int index = 0; index < randomKey.Length; ++index)
              {
                bool fail = false;
                int num = 0;
                do
                {
                    num = 0;
                  
                    
                    
                    byte orig = bytes[index];
                  
                        do
                        {
                            bytes[index] = this.rotateByLeft(bytes[index], (int)encryptedKey[index]);
                            if (((bytes[index] < (byte)32 || bytes[index] > (byte)126) && bytes[index] != 255 && bytes[index] != 0))
                            {
                                num++;
                                if(num>=255)
                                    fail = true;
                            }
                        }
                        while (((bytes[index] < (byte)32 || bytes[index] > (byte)126) || num >= 255));
                    



                    /*
                                        num = 0;
                                    ((tmp < (byte)32 || tmp > (byte)126) && tmp != 255 && tmp != 0);

                                        //  }while(fail);
                                        bytes[index] = tmp;
                                        bool found = false;
                                        do
                                        {
                                            if ((this.rotateByLeft(bytes[index], encryptedKey[index]) >= 21 &&
                                                (this.rotateByLeft(bytes[index], encryptedKey[index]) <= 126)))
                                            {
                                                bytes[index] = this.rotateByLeft(bytes[index], encryptedKey[index]);
                                                found = true;
                                            }
                                            else
                                            {
                                                randomSeed[index] = (byte)new Random().Next(1, 254);
                                                encryptedKey[index] = (byte)(randomKey[index] ^ randomSeed[index]);
                                            }
                                        }
                                        while (!found && ( bytes[index]!=255 && bytes[index]!=0));*/
                    if(fail)
                    {
                        bytes[index] = orig;
                        encryptedKey[index] = (byte) new Random((int)DateTime.Now.Ticks).Next(1, 24);
                        fail = false;
                    }
                } while (num >= 255);
            }

            byte[] inArray1;
            byte[] dec;
            bool err = false;
            do
            {
                inArray1 = this.plugin.encrypt(bytes, password1, this.IV);
                dec = this.plugin.decrypt(inArray1, password1, IV);
                err = false;                
                //for (int i = 0; i < dec.Length; ++i)
                  // if (dec[i] != bytes[i])
                    //   err = true;
            } while (err);

            byte[] seedEnc = this.plugin.encrypt(randomSeed, password3, this.IV);

             byte[] inArray2;
            byte[] dec1;
            do
            {
                inArray2 = this.plugin.encrypt(randomKey, password2, this.IV);
                //dec1 = this.plugin.decrypt(inArray2, password2, IV).Take(randomKey.Length).ToArray();
                err = false;
                //for (int i = 0; i < dec1.Length; ++i)
                  //  if (dec1[i] != randomKey[i])
                  //      err = true;
            } while (err);

            return new CCryptData()
            {
                CryptedData = Convert.ToBase64String(inArray1),
                Key = Convert.ToBase64String(inArray2),
                IV = this.IV,
                Salt = Convert.ToBase64String(seedEnc)
            };
    }

    public string encryptString(string toEncrypt, string password1, string password2, String password3)
    {
      CCryptData graph = this.encryptCCD(toEncrypt, password1, password2, password3);
      BinaryFormatter binaryFormatter = new BinaryFormatter();
      MemoryStream serializationStream = new MemoryStream();
      binaryFormatter.Serialize((Stream) serializationStream, (object) graph);
      return Convert.ToBase64String(serializationStream.GetBuffer());
    }

    public string decryptCCD(CCryptData cryptData, string password1, string password2, String password3)
    {
      byte[] data1 = Convert.FromBase64String(cryptData.CryptedData);
      byte[] data2 = Convert.FromBase64String(cryptData.Key);
      byte[] seed = Convert.FromBase64String(cryptData.Salt);

      byte[] numArray1 = this.plugin.decrypt(data1, password1, cryptData.IV);
      byte[] numArray2 = this.plugin.decrypt(data2, password2, cryptData.IV);
      byte[] seedDec = this.plugin.decrypt(seed, password3, cryptData.IV);
      byte[] numArray3 = numArray1;


            char[] chArray = new char[numArray3.Length];
      for (int index = 0; index < numArray2.Length; ++index)
            {
                int num = 0;
                do
                {
                    num++;
                    byte tmp = 0;
                    bool found = false;
                    numArray3[index] = (byte)(rotateByRight((byte)(numArray3[index]), (byte)(numArray2[index] ^ seedDec[index])));
                } while ((numArray3[index] < 32 || numArray3[index] > 126) && numArray3[index]!=0 && numArray3[index]!=255 && num <255);
                chArray[index] = (char)numArray3[index];
      }
      return new string(chArray);
    }

    public string decryptString(string cryptDataB64, string password1, string password2, String password3)
    {
      return this.decryptCCD((CCryptData) new BinaryFormatter().Deserialize((Stream) new MemoryStream(Convert.FromBase64String(cryptDataB64))), password1, password2, password3);
    }

    private byte[] generateRandomKey(string input)
    {
      byte[] randomKey = new byte[input.Length];
      for (int index = 0; index < input.Length; ++index)
      {
        char ch = (char) new Random().Next(1, 254);
        randomKey[index] = (byte) ch;
      }
      return randomKey;
    }

    private byte rotateByLeft(byte input, int delta)
    {
      delta %= 6;
      ++delta;
      byte num1 = input;
      for (int index = 0; index < delta; ++index)
      {
        byte num2 = (byte) ((uint) (byte) ((uint) num1 & 64U) >> 6);
        num1 = (byte) ((uint) (byte) ((uint) (byte) ((uint) num1 & 63U) << 1) | (uint) num2);
      }
      return num1;
    }

    private byte rotateByRight(byte input, int delta)
    {
      delta %= 6;
      ++delta;
      byte num1 = input;
      for (int index = 0; index < delta; ++index)
      {
        byte num2 = (byte) ((uint) (byte) ((uint) num1 & 1U) << 6);
        num1 = (byte) ((uint) (byte) ((uint) (byte) ((uint) num1 & 126U) >> 1) | (uint) num2);
      }
      return num1;
    }
  }
}
