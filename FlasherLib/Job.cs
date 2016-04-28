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

        public JobState State;

        public enum JobType
        {
            Read, Write, Erase, Ping, GetState, GetID, Go
        }

        public JobType Type;

        public int Address;

        public int DataNum;

        public byte[] Data;
    }
}
