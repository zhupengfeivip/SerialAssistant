using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFSerialAssistant
{
    public class SpindleHelper
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private byte CheckSum(byte[] bytes)
        {
            long sumValue = 0;
            foreach (byte b in bytes)
                sumValue += b;

            return (byte)(sumValue);
        }





    }
}
