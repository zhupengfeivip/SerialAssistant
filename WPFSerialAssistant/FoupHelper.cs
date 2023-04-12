using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFSerialAssistant
{
    public class FoupHelper
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitCmd"></param>
        /// <returns></returns>
        public byte[] GetCmdBuff(string unitCmd)
        {
            // send/ Response code Sending: "00" is input at all time. Receiving: ”00”～”08” Response code
            string code = "00";
            // "00" is input at all time.
            string adr = "00";
            string cmd = code + adr + unitCmd;
            byte[] cmdBuff = Encoding.ASCII.GetBytes(cmd);
            byte sumByte = CheckSum(cmdBuff);
            string newCmd = code + adr + unitCmd + (sumByte & 0xFF).ToString("X2");
            // logger.debug("发送: {}", newCmd);
            cmdBuff = Encoding.ASCII.GetBytes(newCmd);

            using (MemoryStream ms = new MemoryStream(cmdBuff.Length + 2))
            {
                // 前导符SOH 固定 0x01
                ms.WriteByte(0x01);
                // 数据区
                ms.Write(cmdBuff,0,cmdBuff.Length);
                // 结束符 0x0D
                ms.WriteByte(0x0D);

                return ms.GetBuffer();
            }
        }


    }
}
