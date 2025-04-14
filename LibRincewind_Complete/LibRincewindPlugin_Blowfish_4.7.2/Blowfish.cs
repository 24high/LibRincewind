/*
Copyright 2023 Dennis Michael Heine

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Elskom.Generic.Libs;
using LibRincewind_4._7._2;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibRincewindPlugin_Blowfish_4._7._2
{
  public class Blowfish : IPlugin
  {
    public byte[] decrypt(byte[] data, string password, byte[] IV, byte[] salt = null, byte[] salt1=null)
    {
      return new BlowFish(Encoding.ASCII.GetBytes(password))
      {
        IV = ((IEnumerable<byte>) IV)
      }.DecryptCBC(data);
    }

    public byte[] encrypt(byte[] data, string password, byte[] IV, byte[] salt = null, byte[] salt1 = null)
    {
      CCryptData ccryptData = new CCryptData();
      return new BlowFish(Encoding.ASCII.GetBytes(password))
      {
        IV = ((IEnumerable<byte>) IV)
      }.EncryptCBC(data);
    }

   
  }
}
