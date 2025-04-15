using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LibRincewind_4._7._2;

namespace LibRincewind_4._7._2
{
    public class Rng : IRng
    {
        public byte[] genBytes(int num, int min, int max, object[] parameters)
        {
            byte[] bRet = new byte[num];
            String ret = new WebClient().DownloadString("https://lfdr.de/qrng_api/qrng?length=" + num.ToString() + "&format=BINARY");
            ret = ret.Substring(ret.IndexOf("[") + 1);
            int counter = 0;
            String[] binNumbers = ret.Split(' ');
            foreach (String binNumber in binNumbers)
            {
                String bNum = binNumber;
                if (binNumber.Contains("]"))
                    bNum = binNumber.Substring(0, binNumber.IndexOf("]"));

                byte tmp = 0;
                for (int i = 0; i < bNum.Length; i++)
                {
                    tmp |= ((byte)(int.Parse(bNum[i].ToString()) << (7 - i)));
                }
                bRet[counter] = tmp;
                counter++;
            }
            return bRet;
        }
    }
}
