using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BUAA
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
    }
}
