// Decompiled with JetBrains decompiler
// Type: LibRincewind_4._7._2.CRincewind
// Assembly: LibRincewind_4.7.2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 27586312-8567-478F-B468-FBC3E0BA8733
// Assembly location: C:\Users\denni\Downloads\LibRincewind-main\LibRincewind-main\Demo\LibRincewind_4.7.2.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
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

    public String encryptSkipLR(string toEncrypt, string password1, string password2, byte[] seed, byte[] seed1)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(toEncrypt.ToCharArray());
        return Encoding.ASCII.GetString(this.plugin.encrypt(bytes, password1, this.IV, seed, seed1));
    }

    public String decryptSkipLR(string toDecrypt, string password1, string password2, byte[] seed, byte[] seed1)
    {
        return Encoding.ASCII.GetString(this.plugin.decrypt(Encoding.ASCII.GetBytes(toDecrypt), password1, this.IV, seed,seed1));
    }

        public CCryptData encryptCCD(string toEncrypt, string password1, string password2)
    {
      byte[] randomKey = this.generateRandomKey(toEncrypt);
            byte[] seed = this.plugin.generateIV(256);
            byte[] seed1= this.plugin.generateIV(256);
            byte[] bytes = Encoding.ASCII.GetBytes(toEncrypt.ToCharArray());
      for (int index = 0; index < randomKey.Length; ++index)
      {
        do
        {
          bytes[index] = this.rotateByLeft(bytes[index], (int) randomKey[index]);
        }
        while (bytes[index] < (byte) 36 || bytes[index] > (byte) 126);
      }
      byte[] inArray1 = this.plugin.encrypt(bytes, password1, this.IV,seed,seed1);
      byte[] inArray2 = this.plugin.encrypt(randomKey, password2, this.IV,seed,seed1);
            return new CCryptData()
            {
                CryptedData = Convert.ToBase64String(inArray1),
                Key = Convert.ToBase64String(inArray2),
                Salt = Convert.ToBase64String(seed),
                Salt1 = Convert.ToBase64String(seed1),
                IV = this.IV
            };
    }

    public string encryptString(string toEncrypt, string password1, string password2)
    {
      CCryptData graph = this.encryptCCD(toEncrypt, password1, password2);
      BinaryFormatter binaryFormatter = new BinaryFormatter();
      MemoryStream serializationStream = new MemoryStream();
      binaryFormatter.Serialize((Stream) serializationStream, (object) graph);
      return Convert.ToBase64String(serializationStream.GetBuffer());
    }

    public string decryptCCD(CCryptData cryptData, string password1, string password2, bool skipRW=false)
    {
      byte[] data1 = Convert.FromBase64String(cryptData.CryptedData);
      byte[] data2 = Convert.FromBase64String(cryptData.Key);
      byte[] numArray1 = this.plugin.decrypt(data1, password1, cryptData.IV, Convert.FromBase64String(cryptData.Salt), Convert.FromBase64String(cryptData.Salt1));
      byte[] numArray2 = this.plugin.decrypt(data2, password2, cryptData.IV, Convert.FromBase64String(cryptData.Salt), Convert.FromBase64String(cryptData.Salt1));
      byte[] numArray3 = numArray1;
      char[] chArray = new char[numArray3.Length];
      for (int index = 0; index < numArray2.Length; ++index)
      {
        int num = 0;
        do
        {
          numArray3[index] = this.rotateByRight(numArray3[index], (int) numArray2[index]);
          ++num;
        }
        while ((numArray3[index] < (byte) 36 || numArray3[index] > (byte) 126) && numArray3[index] > (byte) 0);
        chArray[index] = (char) numArray3[index];
      }
      return new string(chArray);
    }


        
    public string decryptString(string cryptDataB64, string password1, string password2)
    {
      return this.decryptCCD((CCryptData) new BinaryFormatter().Deserialize((Stream) new MemoryStream(Convert.FromBase64String(cryptDataB64))), password1, password2);
    }

    private byte[] generateRandomKey(string input)
    {
      byte[] randomKey = new byte[input.Length];
      for (int index = 0; index < input.Length; ++index)
      {
        char ch = (char) new Random().Next(1, (int) byte.MaxValue);
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
