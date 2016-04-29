using BUAA.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BUAA
{
    public class JobExecutor
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

        #region CAN

        ICAN CANDevice;

        uint CANPortIndex;

        #endregion

        #region Init

        public JobExecutor(ICAN CANDevice, uint CANPortIndex)
        {
            this.CANDevice = CANDevice;
            this.CANPortIndex = CANPortIndex;
        }

        #endregion

        #region State

        Job[] Jobs;
        int _NoRunning;
        int _StepRunning;

        public bool IsRunning
        {
            get
            {
                if (Jobs == null)
                    return false;
                if (_NoRunning < 0)
                    return false;
                if (_NoRunning >= Jobs.Length)
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
                return Jobs[_NoRunning];
            }
        }

        public int NoRunning
        {
            get
            {
                return _NoRunning;
            }
        }

        #endregion

        #region Event

        public enum JobEventType
        {
            Normal, NACK, UnknowACK, Timeout, Abort
        }

        public delegate void StateChange(JobExecutor Executor, JobEventType EventType, int JobNo, Job Job, string Msg);

        public event StateChange OnStateChange;

        #endregion

        #region Action

        public bool SetJob(Job[] Jobs)
        {
            if (IsRunning)
            {
                return false;
            }
            else
            {
                this.Jobs = Jobs;
                _NoRunning = 0;
                _StepRunning = 0;
                return true;
            }
        }

        public bool SetJob(Job Job)
        {
            Job[] jobs = { Job };
            return SetJob(jobs);
        }

        public Job[] GetJob()
        {
            return Jobs;
        }

        public void BackgroundRun(bool IsUserAbort)
        {
            Executor(IsUserAbort);

            CANMessage[] canMessages = new CANMessage[32];
            int num = CANDevice.Receive(CANPortIndex, canMessages);
            for (int i = 0; i < num; i++)
            {
                Executor(canMessages[i]);
                Executor();
            }

            Executor();
        }

        #endregion

        #region Executor

        public void Executor(bool IsUserAbort)
        {
            lock (this)
            {
                if (IsRunning)
                {
                    if (IsUserAbort == true)
                    {
                        RunningNext(JobEventType.Abort, "User Abort");
                    }
                }
            }
        }

        public void Executor(CANMessage CANMessage)
        {
            lock (this)
            {
                if (!IsRunning)
                    return;

                if (CANMessage.ExternFlag)
                    return;

                Job j = JobRunning;
                if (j.Type == Job.JobType.ACK)
                {
                    if (_StepRunning == 1)
                    {
                        if (CheckCANMessage(CANMessage, ID_ACK, 1) && CANMessage.Data[0] == 0x1F)
                        {
                            RunningNext();
                        }
                        else
                        {
                            ErrorWriteLine("Unknow Message:" + CANMessage.ToString());
                        }
                    }
                }
                else if (j.Type == Job.JobType.GetState)
                {
                    if (_StepRunning == 1)
                    {
                        if (CheckCANMessage(CANMessage, ID_GV, 1) && CANMessage.Data[0] == DA_ACK)
                        {
                            _StepRunning++;
                        }
                        else
                        {
                            ErrorWriteLine("Unknow Message:" + CANMessage.ToString());
                        }
                    }
                    else if (_StepRunning == 2)
                    {
                        if (CheckCANMessage(CANMessage, ID_GV, 1))
                        {
                            j.DataReceive = new byte[3];
                            j.DataReceive[0] = CANMessage.Data[0];
                            _StepRunning++;
                        }
                        else
                        {
                            ErrorWriteLine("Unknow Message:" + CANMessage.ToString());
                        }

                    }
                    else if (_StepRunning == 3)
                    {
                        if (CheckCANMessage(CANMessage, ID_GV, 2))
                        {
                            j.DataReceive[1] = CANMessage.Data[0];
                            j.DataReceive[2] = CANMessage.Data[1];
                            _StepRunning++;
                        }
                        else
                        {
                            ErrorWriteLine("Unknow Message:" + CANMessage.ToString());
                        }
                    }
                    else if (_StepRunning == 4)
                    {
                        if (CheckCANMessage(CANMessage, ID_GV, 1) && CANMessage.Data[0] == DA_ACK)
                        {
                            RunningNext();
                        }
                        else
                        {
                            ErrorWriteLine("Unknow Message:" + CANMessage.ToString());
                        }
                    }
                }
            }
        }


        public void Executor()
        {
            lock (this)
            {
                if (!IsRunning)
                    return;
                Job j = JobRunning;
                if (j.Type == Job.JobType.ACK)
                {
                    if (_StepRunning == 0)
                    {
                        SendCANCmd(ID_ACK);
                        _StepRunning++;
                    }
                    else
                    {
                        TimeoutCheck();
                    }
                }
                else if (j.Type == Job.JobType.GetState)
                {
                    if (_StepRunning == 0)
                    {
                        SendCANCmd(ID_GV);
                        _StepRunning++;
                    }
                    else
                    {
                        TimeoutCheck();
                    }
                }
            }
        }

        #endregion

        #region Const

        public const uint ID_ACK = 0x79;
        public const uint ID_GV = 0x01;

        public const byte DA_ACK = 0x79;

        #endregion

        #region Send

        void SendCANCmd(uint ID)
        {
            CANMessage message = new CANMessage(ID, false);
            CANMessage[] messages = { message };
            CANDevice.Send(CANPortIndex, messages);
            lastSendTime = DateTime.Now;
        }

        public void SendCANCmdACK()
        {
            SendCANCmd(ID_ACK);
        }

        #endregion

        #region Check

        DateTime lastSendTime;
        public TimeSpan TimeoutSpan = new TimeSpan(0, 0, 5);

        void TimeoutCheck()
        {
            if (DateTime.Now - lastSendTime > TimeoutSpan)
            {
                RunningNext(JobEventType.Timeout, "Timeout");
            }
        }

        static bool CheckCANMessage(CANMessage CANMessage, uint ID, int DataLen)
        {
            if (CANMessage.ID != ID)
                return false;
            if (CANMessage.DataLen != DataLen)
                return false;
            return true;
        }

        #endregion

        #region ChangeState
        
        void RunningNext()
        {
            RunningNext(JobEventType.Normal, JobRunning.Type.ToString() + " Done");
        }

        void RunningNext(JobEventType Type, string Message)
        {
            int lastNoCunning = _NoRunning;
            Job lastJobRunning = JobRunning;

            if (Type == JobEventType.Normal)
            {
                lastJobRunning.State = Job.JobState.Done;
                if (_NoRunning < Jobs.Length - 1)
                {
                    _NoRunning++;
                }
                else
                {
                    _NoRunning = -1;
                }
            }
            else
            {
                lastJobRunning.State = Job.JobState.Fail;
                _NoRunning = -1;
            }
            _StepRunning = 0;

            OnStateChange?.Invoke(this, Type, lastNoCunning, lastJobRunning, Message);
        }

        #endregion
    }
}
