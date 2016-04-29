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

        //Executor Process Message 
        public void Executor(CANMessage CANMessage)
        {
            lock (this)
            {
                if (!IsRunning)
                    return;

                if (CANMessage.ExternFlag)
                    return;

                Job j = JobRunning;

                //ACK
                if (j.Type == Job.JobType.ACK)
                {
                    if (_StepRunning == 1)
                    {
                        if (CheckCANMessage(CANMessage, ID_ACK, DA_NACK))
                        {
                            RunningNext();
                        }
                        else
                        {
                            ErrorWriteLine("Unknow Message:" + CANMessage.ToString());
                        }
                    }
                }
                //READ
                else if (j.Type == Job.JobType.Read)
                {
                    int m = (int)Math.Ceiling(j.DataNum / 8f);
                    if (_StepRunning == 1)//ACK
                    {
                        if (CheckCANMessage(CANMessage, ID_READ, DA_ACK))
                        {
                            _StepRunning++;
                        }
                        else
                        {
                            ErrorWriteLine("Unknow Message:" + CANMessage.ToString());
                        }
                    }
                    else if (_StepRunning >= 2 && _StepRunning <= m + 1)//DATA
                    {
                        if (CheckCANMessage(CANMessage, ID_READ) && CANMessage.DataLen > 0)
                        {
                            if (_StepRunning == 2)
                            {
                                j.DataReceive = new byte[j.DataNum];
                            }
                            Array.Copy(CANMessage.Data, 0, j.DataReceive, (_StepRunning - 2) * CANMessage.DataLenMax, CANMessage.DataLen);
                            _StepRunning++;
                        }
                        else
                        {
                            ErrorWriteLine("Unknow Message:" + CANMessage.ToString());
                        }
                    }
                    else if (_StepRunning == m + 2)//ACK
                    {
                        if (CheckCANMessage(CANMessage, ID_READ, DA_ACK))
                        {
                            RunningNext();
                        }
                        else
                        {
                            ErrorWriteLine("Unknow Message:" + CANMessage.ToString());
                        }
                    }
                    else
                    {
                        TimeoutCheck();
                    }
                }
                //WRITE
                else if (j.Type == Job.JobType.Write)
                {
                    int m = (int)Math.Ceiling(j.DataNum / 8f);
                    if (_StepRunning % 2 == 1 && _StepRunning <= 2 * m + 1)
                    {
                        if (CheckCANMessage(CANMessage, ID_WRITE, DA_ACK))
                        {
                            _StepRunning++;
                        }
                        else
                        {
                            ErrorWriteLine("Unknow Message:" + CANMessage.ToString());
                        }
                    }
                    else if (_StepRunning == 2 * m + 3)
                    {
                        if (CheckCANMessage(CANMessage, ID_WRITE, DA_ACK))
                        {
                            RunningNext();
                        }
                        else
                        {
                            ErrorWriteLine("Unknow Message:" + CANMessage.ToString());
                        }
                    }
                }
                //GV
                else if (j.Type == Job.JobType.GetState)
                {
                    if (_StepRunning == 1)
                    {
                        if (CheckCANMessage(CANMessage, ID_GV, DA_ACK))
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
                        if (CheckCANMessage(CANMessage, ID_GV, DA_ACK))
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

        //Executor Process State&Time 
        public void Executor()
        {
            lock (this)
            {
                if (!IsRunning)
                    return;
                Job j = JobRunning;
                //ACK
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
                //READ
                else if (j.Type == Job.JobType.Read)
                {
                    if (_StepRunning == 0)
                    {
                        byte[] data = new byte[5];
                        j.AddressTo(data, 0);
                        data[4] = (byte)(j.DataNum - 1);
                        SendCANCmdData(ID_READ, data);
                        _StepRunning++;
                    }
                    else
                    {
                        TimeoutCheck();
                    }
                }
                //WRITE
                else if (j.Type == Job.JobType.Read)
                {
                    int m = (int)Math.Ceiling(j.DataNum / 8f);
                    if (_StepRunning == 0)
                    {
                        byte[] data = new byte[5];
                        j.AddressTo(data, 0);
                        data[4] = (byte)(j.DataNum - 1);
                        SendCANCmdData(ID_WRITE, data);
                        _StepRunning++;
                    }
                    else if (_StepRunning % 2 == 0 && _StepRunning <= 2 * m + 2)
                    {
                        int n = (_StepRunning - 2) / 2;
                        int len = n = j.DataNum - n * 8;
                        if (len > 8)
                            len = 8;

                        byte[] data = new byte[len];
                        Array.Copy(j.DataSend, n * 8, data, 0, len);

                        SendCANCmdData(ID_WRITEDATA, data);
                        _StepRunning++;
                    }
                    else
                    {
                        TimeoutCheck();
                    }
                }
                //GV
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
        public const uint ID_NACK = 0x1F;
        public const uint ID_GV = 0x01;
        public const uint ID_READ = 0x011;
        public const uint ID_WRITE = 0x031;
        public const uint ID_WRITEDATA = 0x04;

        public const byte DA_ACK = 0x79;
        public const byte DA_NACK = 0x1F;

        #endregion

        #region Send

        void SendCANCmdData(uint ID, byte[] Data)
        {
            CANMessage message = new CANMessage(ID, false, Data);
            CANMessage[] messages = { message };
            CANDevice.Send(CANPortIndex, messages);
            lastSendTime = DateTime.Now;
        }

        void SendCANCmd(uint ID)
        {
            SendCANCmdData(ID, null);
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

        static bool CheckCANMessage(CANMessage CANMessage, uint ID)
        {
            if (CANMessage.ID != ID)
                return false;
            return true;
        }

        static bool CheckCANMessage(CANMessage CANMessage, uint ID, int DataLen)
        {
            if (CANMessage.ID != ID)
                return false;
            if (CANMessage.DataLen != DataLen)
                return false;
            return true;
        }

        static bool CheckCANMessage(CANMessage CANMessage, uint ID, byte Data0)
        {
            if (CANMessage.ID != ID)
                return false;
            if (CANMessage.DataLen != 1)
                return false;
            if (CANMessage.Data[0] != Data0)
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
