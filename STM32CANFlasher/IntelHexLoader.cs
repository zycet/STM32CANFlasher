using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BUAA.Misc;
using BUAA.Structure;
using System.IO;

namespace STM32CANFlasher
{
    public class IntelHexLoader
    {
        #region Log

        static void Write(string s)
        {
            Console.Write(s);
        }

        static void WriteLine(string s)
        {
            Write(s + Environment.NewLine);
        }

        static void WriteLine()
        {
            Write(Environment.NewLine);
        }

        static void ErrorWrite(string s)
        {
            Console.Error.Write(s);
        }

        static void ErrorWriteLine(string s)
        {
            ErrorWrite(s + Environment.NewLine);
        }

        static void ErrorWriteLine()
        {
            ErrorWrite(Environment.NewLine);
        }

        #endregion

        public enum ResultEnum
        {
            Success, FlagWrong, LengthWrong, NumWrong, TypeWrong, FormatWrong, SumWrong, StructWrong
        }

        public ResultEnum Load(string FileName, AddressDataGroup<byte> DataGroup)
        {
            using (StreamReader sr = new StreamReader(FileName, System.Text.Encoding.ASCII))
            {
                return Load(sr, DataGroup);
            }
        }

        public ResultEnum Load(StreamReader SR, AddressDataGroup<byte> DataGroup)
        {
            IntelHexParser hexParser = new IntelHexParser();
            IntelHexParser.HexRecord record = new IntelHexParser.HexRecord();
            IntelHexParser.ResultEnum result = IntelHexParser.ResultEnum.Success;

            int no = 0;
            while (!SR.EndOfStream)
            {
                string s = SR.ReadLine();
                result = hexParser.Parse(s, out record);
                if (result != IntelHexParser.ResultEnum.Success)
                {

                    ErrorWriteLine(string.Format("HexString Parse Fail#{0}:{1}", no, s));
                    return (ResultEnum)result;
                    //return ResultEnum.StructWrong;
                }
                if (no == 0)
                {
                    if (record.Type != IntelHexParser.HexRecordType.ExtenLinAdd)//起始地址
                    {
                        ErrorWriteLine(string.Format("Hex File not Begin with ExtenLinAdd Record#{0}:{1}", no, s));
                        return ResultEnum.StructWrong;
                    }
                }
                else
                {
                    if (record.Type == IntelHexParser.HexRecordType.Data)//数据正常
                    { }
                    else if (record.Type == IntelHexParser.HexRecordType.StartLinAdd)//忽略最后的地址设定命令
                    { }
                    else if (record.Type == IntelHexParser.HexRecordType.EOF)//数据结束
                    {
                        break;
                    }
                    else//无法支持的类型
                    {
                        ErrorWriteLine(string.Format("HexString Type not Support#{0}:{1}", no, s));
                        return ResultEnum.FormatWrong;
                    }
                }
                if (record.Type == IntelHexParser.HexRecordType.Data)
                {
                    DataGroup.Insert(hexParser.Address, record.Data);
                }
                no++;
            }
            if (!SR.EndOfStream)//未读取到文件结束
            {
                ErrorWriteLine(string.Format("Parse End before EOF#{0}", no));
            }
            if (DataGroup.Groups.Count != 1)
            {
                ErrorWriteLine(string.Format("Hex File not 1 Data Section:{0}", DataGroup.Groups.Count));
                return ResultEnum.StructWrong;
            }
            return ResultEnum.Success;
        }
    }
}
