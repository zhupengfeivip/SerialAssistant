using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WPFSerialAssistant
{
    public static class Utilities
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytesBuffer"></param>
        /// <param name="mode"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string BytesToText(List<byte> bytesBuffer, ReceiveMode mode, Encoding encoding)
        {
            string result = "";

            if (mode == ReceiveMode.Character)
                return encoding.GetString(bytesBuffer.ToArray<byte>());

            foreach (var item in bytesBuffer)
            {
                switch (mode)
                {
                    case ReceiveMode.Hex:
                        result += item.ToString("X2") + " ";
                        break;
                    case ReceiveMode.Decimal:
                        result += Convert.ToString(item, 10) + " ";
                        break;
                    case ReceiveMode.Octal:
                        result += Convert.ToString(item, 8) + " ";
                        break;
                    case ReceiveMode.Binary:
                        result += Convert.ToString(item, 2).PadLeft(8, '0') + " ";
                        break;
                    default:
                        break;
                }
            }
            result = result.TrimEnd();

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="mode"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string ToSpecifiedText(string text, SendMode mode, Encoding encoding)
        {
            string result = "";
            switch (mode)
            {
                case SendMode.Character:
                    {
                        text = text.Trim();

                        // 转换成字节
                        List<byte> src = new List<byte>();

                        string[] grp = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var item in grp)
                        {
                            src.Add(Convert.ToByte(item, 16));
                        }

                        // 转换成字符串
                        result = encoding.GetString(src.ToArray<byte>());
                        break;
                    }
                case SendMode.Hex:
                    {
                        byte[] byteStr = encoding.GetBytes(text.ToCharArray());

                        foreach (var item in byteStr)
                        {
                            result += Convert.ToString(item, 16).ToUpper() + " ";
                        }
                        break;
                    }
                default:
                    break;
            }

            return result.Trim();
        }




    }
}
