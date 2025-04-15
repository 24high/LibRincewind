using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibRincewind_4._7._2;

namespace LibRincewind_4._7._2
{
    public class Rng: IRng
    {
        public byte[] genBytes(int num, int min, int max, object[] parameters)
        {
            String url=(String)parameters[0];
            String numToken= (String)parameters[1];
            String minToken= (String)parameters[2];
            String maxToken= (String)parameters[3];
            String returnRegEx= (String)parameters[4];

            url = url.Replace(numToken, num.ToString());
            url = url.Replace(minToken, min.ToString());
            url = url.Replace(maxToken, max.ToString());

            byte[] bRet = new byte[num];
            String ret = new WebClient().DownloadString(url);

            var m1 = Regex.Matches(ret, returnRegEx);

            int counter = 0;

            foreach (Match mNumber in m1)
            {
                String bNum = mNumber.Value;
                bRet[counter] = byte.Parse(bNum);
                counter++;
            }
            return bRet;
        }
    }
}
