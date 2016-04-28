using BUAA.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BUAA
{
    public class JobExecutor
    {
        ICAN CANDevice;

        uint CANPortIndex;

        public JobExecutor(ICAN CANDevice, uint CANPortIndex)
        {
            this.CANDevice = CANDevice;
            this.CANPortIndex = CANPortIndex;

        }

        public void BackgroundRun(bool IsUserAbort)
        {
            CANMessage[] messages = new CANMessage[32];
            int num = CANDevice.Receive(CANPortIndex, messages);
        }

        Job[] Jobs;
        int _NoJobRunning;
        int _Step_Running;

        public bool IsRunning
        {
            get
            {
                if (Jobs == null)
                    return false;
                if (_NoJobRunning < 0)
                    return false;
                if (_NoJobRunning >= Jobs.Length)
                    return false;
                return true;
            }
        }

        public Job JobRunning
        {
            get
            {
                if (IsRunning == false)
                    return null;
                return Jobs[_NoJobRunning];
            }
        }

        public int NoRunning
        {
            get
            {
                return _NoJobRunning;
            }
        }

        public enum JobEventType
        {
            Normal, NACK, UnknowACK, Timeout, Abort
        }

        public event Action<JobExecutor, JobEventType, int, string> OnStateChange;

        public bool SetJobs(Job[] Jobs)
        {
            if (IsRunning)
            {
                return false;
            }
            else
            {
                this.Jobs = Jobs;
                _NoJobRunning = 0;
                _Step_Running = 0;
                return true;
            }
        }

        public void Executor(bool IsUserAbort)
        {
            if (IsRunning)
            {
                if (IsUserAbort == true)
                {
                    int lastNoCunning = _NoJobRunning;
                    _NoJobRunning = -1;
                    JobRunning.State = Job.JobState.Fail;
                    OnStateChange?.Invoke(this, JobEventType.Abort, lastNoCunning, "User Abort");
                    return;
                }
            }
        }

        public void Executor(CANMessage CANMessage)
        {
            if (!IsRunning)
                return;

        }

        public void Executor()
        {
            if (!IsRunning)
                return;
        }
    }
}
