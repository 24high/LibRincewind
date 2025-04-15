// Decompiled with JetBrains decompiler
// Type: LibRincewind_4._7._2.CRincewind
// Assembly: LibRincewind_4.7.2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 27586312-8567-478F-B468-FBC3E0BA8733
// Assembly location: C:\Users\denni\Downloads\LibRincewind-main\LibRincewind-main\Demo\LibRincewind_4.7.2.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;


namespace LibRincewind_4._7._2
{
    public class CRincewind
    {
        private IPlugin plugin;
        public byte[] IV;
        public static IRng Rng = null;

        public CRincewind(string _plugin, string _rng,int ivSize = 512)
        {
            foreach (Type exportedType in Assembly.LoadFile(_plugin).GetExportedTypes())
            {
                if (((IEnumerable<Type>)exportedType.GetInterfaces()).Contains<Type>(typeof(IPlugin)))
                    this.plugin = (IPlugin)Activator.CreateInstance(exportedType);
            }
            CRincewind.Rng = null;

            if (_rng != "")
            {
                foreach (Type exportedType in Assembly.LoadFile(_rng).GetExportedTypes())
                {
                    if (((IEnumerable<Type>)exportedType.GetInterfaces()).Contains<Type>(typeof(IRng)))
                        CRincewind.Rng = (IRng)Activator.CreateInstance(exportedType);
                }
            }



            this.IV = QRNG(ivSize);
        }

        public CCryptData encryptCCD(string toEncrypt, string password1, string password2, byte[] salt1, byte[] salt2)
        {
            byte[] randomKey = this.generateRandomKey(toEncrypt);
            byte[] seed = salt1;
            byte[] seed1 = salt2;
            byte[] bytes = Encoding.ASCII.GetBytes(toEncrypt.ToCharArray());
            for (int index = 0; index < randomKey.Length; ++index)
            {
                do
                {
                    bytes[index] = this.rotateByLeft(bytes[index], (int)randomKey[index]);
                }
                while (bytes[index] < (byte)36 || bytes[index] > (byte)126);
            }
            byte[] inArray1 = this.plugin.encrypt(bytes, password1, this.IV, seed, seed1);
            byte[] inArray2 = this.plugin.encrypt(randomKey, password2, this.IV, seed, seed1);
            return new CCryptData()
            {
                CryptedData = Convert.ToBase64String(inArray1),
                Key = Convert.ToBase64String(inArray2),
                Salt = Convert.ToBase64String(seed),
                Salt1 = Convert.ToBase64String(seed1),
                IV = this.IV
            };
        }

        public string encryptString(string toEncrypt, string password1, string password2, byte[] salt1, byte[] salt2)
        {
            CCryptData graph = this.encryptCCD(toEncrypt, password1, password2,salt1, salt2);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            MemoryStream serializationStream = new MemoryStream();
            binaryFormatter.Serialize((Stream)serializationStream, (object)graph);
            return Convert.ToBase64String(serializationStream.GetBuffer());
        }

        public string decryptCCD(CCryptData cryptData, string password1, string password2, byte[] salt1, byte[] salt2)
        {
            byte[] data1 = Convert.FromBase64String(cryptData.CryptedData);
            byte[] data2 = Convert.FromBase64String(cryptData.Key);
            byte[] numArray1 = this.plugin.decrypt(data1, password1, cryptData.IV, salt1,salt2);
            byte[] numArray2 = this.plugin.decrypt(data2, password2, cryptData.IV,salt1, salt2);
            byte[] numArray3 = numArray1;
            char[] chArray = new char[numArray3.Length];
            for (int index = 0; index < numArray2.Length; ++index)
            {
                int num = 0;
                do
                {
                    numArray3[index] = this.rotateByRight(numArray3[index], (int)numArray2[index]);
                    ++num;
                }
                while ((numArray3[index] < (byte)36 || numArray3[index] > (byte)126) && numArray3[index] > (byte)0 && num<20);
                chArray[index] = (char)numArray3[index];
            }
            return new string(chArray);
        }

        public string decryptString(string cryptDataB64, string password1, string password2, byte[] salt1, byte[] salt2)
        {
            return this.decryptCCD((CCryptData)new BinaryFormatter().Deserialize((Stream)new MemoryStream(Convert.FromBase64String(cryptDataB64))), password1, password2,salt1,salt2);
        }

        private byte[] generateRandomKey(string input)
        {
            byte[] randomKey = QRNG(input.Length);
            return randomKey;
        }

        public static byte[] QRNG(int bytes, int min = 0, int max = -1, object[] parameters = null)
        {
            byte[] bRet = new byte[bytes];
            if(Rng==null)
            {
                for (int i = 0; i < bytes; i++)
                {
                    byte b = (byte)new Random().Next(1, 254);
                    bRet[i] = b;
                    System.Threading.Thread.Sleep(new Random().Next(10, 200));
                }
            }
            else
            {
                bRet= Rng.genBytes(bytes,min,max,parameters);
            }
            return bRet;
        }
        
        private byte rotateByLeft(byte input, int delta)
        {
            delta %= 6;
            ++delta;
            byte num1 = input;
            for (int index = 0; index < delta; ++index)
            {
                byte num2 = (byte)((uint)(byte)((uint)num1 & 64U) >> 6);
                num1 = (byte)((uint)(byte)((uint)(byte)((uint)num1 & 63U) << 1) | (uint)num2);
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
                byte num2 = (byte)((uint)(byte)((uint)num1 & 1U) << 6);
                num1 = (byte)((uint)(byte)((uint)(byte)((uint)num1 & 126U) >> 1) | (uint)num2);
            }
            return num1;
        }
    }
}
