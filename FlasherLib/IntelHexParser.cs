using System;
using System.IO;

namespace BUAA.Misc
{
    public class IntelHexParser
    {
        #region Data Struct

        public enum HexRecordType
        {
            Data = 0,
            EOF = 1,
            ExtenSegAdd = 2,
            StartSegAdd = 3,
            ExtenLinAdd = 4,
            StartLinAdd = 5,
        }

        public struct HexRecord
        {
            public int ByteCount
            {
                get
                {
                    return Data == null ? 0 : Data.Length;
                }
            }

            public int Address;

            public HexRecordType Type;

            public byte[] Data;

            public byte CheckSum;
        }

        public enum ResultEnum
        {
            Success, FlagWrong, LengthWrong, NumWrong, TypeWrong, FormatWrong, SumWrong
        }

        #endregion

        public ResultEnum FileCheck_Keil(string FileName, out int ErrorLineNo)
        {
            using (StreamReader sr = new StreamReader(FileName, System.Text.Encoding.ASCII))
            {
                return FileCheck_Keil(sr, out ErrorLineNo);
            }
        }

        public ResultEnum FileCheck_Keil(StreamReader SR, out int ErrorLineNo)
        {
            HexRecord record = new HexRecord();
            ResultEnum result = ResultEnum.Success;
            ErrorLineNo = -1;
            int no = 0;
            while (!SR.EndOfStream)
            {
                string s = SR.ReadLine();
                result = Parse(s, out record);
                if (result != ResultEnum.Success)
                {
                    ErrorLineNo = no;
                    return result;
                }
                if (no == 0)
                {
                    if (record.Type != HexRecordType.ExtenLinAdd)
                    {
                        ErrorLineNo = no;
                        return ResultEnum.FormatWrong;
                    }
                }
                else
                {
                    if (record.Type == HexRecordType.Data)
                    {
                        //continue;
                    }
                    else if (record.Type == HexRecordType.EOF)
                    {
                        break;
                    }
                    else
                    {
                        ErrorLineNo = no;
                        return ResultEnum.FormatWrong;
                    }
                }

                no++;
            }
            if (record.Type != HexRecordType.EOF || !SR.EndOfStream)
            {
                ErrorLineNo = no;
                return ResultEnum.FormatWrong;
            }
            return ResultEnum.Success;
        }

        public ResultEnum Parse(string RecordString, out HexRecord Record)
        {
            Record = new HexRecord();

            int offset = 0;

            if (RecordString == null)
                return ResultEnum.LengthWrong;
            if (RecordString.Length < 11)
                return ResultEnum.LengthWrong;
            if (RecordString.Length % 2 != 1)
                return ResultEnum.LengthWrong;

            if (NumCheck(RecordString, 1) == false)
                return ResultEnum.NumWrong;

            if (SumCheck(RecordString, 1) == false)
                return ResultEnum.SumWrong;

            //Start ':'
            if (RecordString[offset++] != ':')
                return ResultEnum.FlagWrong;

            //Byte Count
            int byteCount = HexByteParse(RecordString, offset); offset += 2;
            if (RecordString.Length - 11 != byteCount * 2)
                return ResultEnum.LengthWrong;

            //Address
            byte[] address = HexStringParse(RecordString, offset, 2); offset += 4;
            Record.Address = BitConverter.ToUInt16(address, 0);

            //Type
            byte type = HexByteParse(RecordString, offset); offset += 2;
            if (Enum.IsDefined(typeof(HexRecordType), (int)type) == false)
                return ResultEnum.TypeWrong;
            Record.Type = (HexRecordType)type;

            //Data
            Record.Data = HexStringParse(RecordString, offset, byteCount); offset += 2 * byteCount;

            //ExState
            if (Record.Type == HexRecordType.Data)
            {
                if (_OffsetAddress + _ByteCount == Record.Address)
                    _IsAddressJump = false;
                else
                    _IsAddressJump = true;

                _OffsetAddress = Record.Address;
                _ByteCount = Record.ByteCount;
            }
            else if (Record.Type == HexRecordType.ExtenSegAdd)
            {
                _IsAddressJump = true;
                _BaseAddress = (Record.Data[0] * 256 + Record.Data[1]) * 16;
                _OffsetAddress = 0;
                _ByteCount = 0;
            }
            else if (Record.Type == HexRecordType.StartSegAdd)
            {
                _IsAddressJump = true;
                _BaseAddress = (Record.Data[0] * 256 + Record.Data[1]) * 16;
                _OffsetAddress = 0;
                _ByteCount = 0;
            }
            else if (Record.Type == HexRecordType.ExtenLinAdd)
            {
                _IsAddressJump = true;
                _BaseAddress = (Record.Data[0] * 256 + Record.Data[1]) * 65536;
                _OffsetAddress = 0;
                _ByteCount = 0;
            }
            else if (Record.Type == HexRecordType.StartLinAdd)
            {
                _IsAddressJump = true;
                _BaseAddress = (Record.Data[0] * 256 + Record.Data[1]) * 65536;
                _OffsetAddress = 0;
                _ByteCount = 0;
            }
            else if (Record.Type == HexRecordType.EOF)
            {
                _IsAddressJump = false;
                _BaseAddress = 0;
                _OffsetAddress = 0;
                _ByteCount = 0;
            }
            return ResultEnum.Success;
        }

        #region Tools

        bool NumCheck(string S, int Offset)
        {
            for (int i = Offset; i < S.Length; i++)
            {
                char c = S[i];
                if (c >= '0' && c <= '9')
                { continue; }
                else if (c >= 'A' && c <= 'F')
                { continue; }
                else if (c >= 'a' && c <= 'f')
                { continue; }
                else
                { return false; }
            }
            return true;
        }

        bool SumCheck(string S, int Offset)
        {
            byte sum = 0;
            while (Offset < S.Length - 2)
            {
                sum += HexByteParse(S, Offset);
                Offset += 2;
            }
            sum = (byte)(1 + (~sum));
            return sum == HexByteParse(S, Offset);
        }

        byte HexByteParse(string S, int Offset)
        {
            return byte.Parse(S.Substring(Offset, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
        }

        byte[] HexStringParse(string S, int Offset, int ByteNum)
        {
            byte[] data = new byte[ByteNum];
            for (int i = 0; i < ByteNum; i++)
            {
                data[ByteNum - i - 1] = HexByteParse(S, Offset + i * 2);
            }
            return data;
        }

        #endregion

        #region ExState

        int _BaseAddress;

        public int BaseAddress
        {
            get { return _BaseAddress; }
        }

        int _OffsetAddress;

        public int OffsetAddress
        {
            get { return _OffsetAddress; }
        }

        public int Address
        {
            get { return BaseAddress + OffsetAddress; }
        }

        int _ByteCount;
        bool _IsAddressJump;

        public bool IsAddressJump
        {
            get { return _IsAddressJump; }
        }

        #endregion
    }
}
