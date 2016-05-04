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
            ACK, Read, Write, Erase, GetState, GetID, Go
        }

        public JobType Type = JobType.ACK;

        public int Address = 0;

        public int DataNum;

        public byte[] DataSend = null;

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
