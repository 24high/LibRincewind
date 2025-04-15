using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibRincewind_4._7._2;

namespace LibRincewind_4._7._2
{
    public class Rng : IRng
    {
        private SerialPort comport = null;
        public byte[] genBytes(int num, int min, int max, object[] parameters)
        {
            byte[] data = new byte[num];            
            String comPort=(String)parameters[0];

            comport = new SerialPort(comPort);
            comport.Open();

            for (int i = 0; i < num; i++)
            {
                int b=0;
                do
                {
                    b = comport.ReadByte();                    

                    if(max>-1)
                    {
                        b = (byte)(((int)b) % max);
                    }

                    if (min > 0)
                    {
                        b = (byte) (min+(((int)b) - min));
                    }

                } while (b != -1);
                data[i] = (byte)b;
            }           
            comport.Close();
            return data;
        }
    }
}
