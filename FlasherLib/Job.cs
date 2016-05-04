using System;

namespace BUAA.Flasher
{
    public class Job
    {
        public enum JobState
        {
            Idle, Running, Done, Fail
        }

        public JobState State = JobState.Idle;

        public enum JobType
        {
            ACK, Read, Write, Erase, GetState, Go
        }

        /// <summary>
        /// 标记不同任务类型(不同类型任务其他数据项意义不同)
        /// </summary>
        public JobType Type = JobType.ACK;

        /// <summary>
        /// Read/Write/Go时需填写为目标地址
        /// </summary>
        public int Address = 0;

        /// <summary>
        /// Read时为需读取数据长度,不可大于256.
        /// </summary>
        public int DataNum;

        /// <summary>
        /// Write,时为待写入数据长度,不可大于256.Erase时为待擦除PageNo,如为0xFF则表示全片擦除.
        /// </summary>
        public byte[] DataSend = null;

        /// <summary>
        /// Read时为读取的数据.GetState时Data[0]为Bootloader版本号,Data[1~2]为读写保护标示.
        /// </summary>
        public byte[] DataReceive = null;

        public Job()
        { }

        public Job(JobType Type)
        {
            this.Type = Type;
        }

        public Job(JobType Type, byte[] DataSend)
        {
            this.Type = Type;
            this.DataSend = DataSend;
        }

        /// <summary>
        /// 将32Bit地址值以高位优先的方式写入4Byte数组
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Offset"></param>
        public void AddressTo(byte[] Buffer, int Offset)
        {
            byte[] bs = BitConverter.GetBytes(Address);
            int n = bs.Length;
            for (int i = 0; i < n; i++)
            {
                Buffer[Offset + i] = bs[n - i - 1];
            }
        }
    }
}
