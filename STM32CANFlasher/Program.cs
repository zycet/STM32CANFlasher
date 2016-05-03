﻿using BUAA;
using BUAA.Device;
using BUAA.Flasher;
using BUAA.Hibernate;
using BUAA.Misc;
using BUAA.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace STM32CANFlasher
{
    class Program
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

        #region Tools

        private static void ArgsShow(ConfigStruct Config)
        {
            Type t = typeof(ConfigStruct);
            FieldInfo[] fieldInfos = t.GetFields();
            for (int i = 0; i < fieldInfos.Length; i++)
            {
                WriteLine(fieldInfos[i].Name + ":" + fieldInfos[i].GetValue(Config));
            }
        }

        private static void JE_OnStateChange(JobExecutor Executor, JobExecutor.JobEventType EventType, int JobNo, Job Job, string Msg)
        {
            Console.WriteLine(Job.Type + "@" + Msg);
        }

        #endregion

        static void Main(string[] args)
        {
            //Config Parse
            ConfigStruct config = new ConfigStruct();
            string rString = "";
            int numArgAccept = ConfigStruct.Parse(config, args, ref rString); ;

            if (numArgAccept < 0)
            {
                ErrorWriteLine(rString);
                return;
            }
            WriteLine("Args Parsed Num:" + numArgAccept);
            ArgsShow(config);

            //Hex Load
            AddressDataGroup<byte> dataGroup = new AddressDataGroup<byte>();
            try
            {
                IntelHexLoader hexLoader = new IntelHexLoader();
                IntelHexLoader.ResultEnum r = hexLoader.Load(config.HexFile, dataGroup);
                if (r != IntelHexLoader.ResultEnum.Success)
                {
                    ErrorWriteLine(r.ToString());
                    return;
                }
            }
            catch (Exception ee)
            {
                ErrorWriteLine(ee.Message);
                return;
            }

            //FlashSection Load
            JobMaker.FlashSectionStruct[] FlashSection = null;
            if (config.FlashSectionFile != "")
            {
                try
                {
                    FlashSection = (JobMaker.FlashSectionStruct[])SerializerHelper.Deserialize(config.FlashSectionFile, typeof(JobMaker.FlashSectionStruct[]));
                }
                catch (Exception ee)
                {
                    ErrorWriteLine(ee.Message);
                    //return;
                }
            }

            //Make Job
            List<Job> jobs = new List<Job>();
            try
            {
                JobMaker.EraseWrite(jobs, dataGroup, config.EraseOpt, FlashSection, true);
            }
            catch (Exception ee)
            {
                ErrorWriteLine(ee.Message);
                return;
            }

            //CAN Open
            ICAN eCAN = null;
            try
            {
                eCAN = new ECANDev();
                bool rOpen = eCAN.Open(config.CANDeviceNo);
                if (!rOpen)
                {
                    ErrorWriteLine("CAN Open Fail.");
                    return;
                }
                bool rStart = eCAN.Start(config.CANPortNo, config.CANBPS, false, config.CANSendOnce, false);
                if (!rOpen)
                {
                    ErrorWriteLine("CAN Start Fail.");
                    return;
                }
            }
            catch (Exception ee)
            {
                ErrorWriteLine(ee.Message);
                return;
            }

            //JobExecutor Init
            JobExecutor JE = new JobExecutor(eCAN, config.CANPortNo);
            JE.IsShowSendRecive = config.ShowReceiveSend;
            JE.OnStateChange += JE_OnStateChange;
            JE.SetJob(jobs.ToArray());

            //Run
            while (JE.IsRunning)
            {
                JE.BackgroundRun(false);
                Thread.Sleep(100);
            }
        }
        
        #region Test

        static void Test()
        {
            //HexTest();
            //ACKTest();
            //READTest();
            //WRITE_READTest();
            //ERASETest();
            //AllTest();
        }

        private static void ERASETest()
        {
            ICAN eCAN = new ECANDev();
            eCAN.Open(0);
            eCAN.Start(0, 125000, false, true, false);

            JobExecutor JE = new JobExecutor(eCAN, 0);
            JE.OnStateChange += JE_OnStateChange;

            Job j = new Job(Job.JobType.Erase);
            j.DataSend = new byte[1];
            j.DataSend[0] = 0xFF;


            Job[] js = { j };
            JE.SetJob(js);

            while (true)
            {
                JE.BackgroundRun(false);
                Thread.Sleep(10);
            }
        }

        private static void WRITE_READTest()
        {
            ICAN eCAN = new ECANDev();
            eCAN.Open(0);
            eCAN.Start(0, 125000, false, true, false);

            JobExecutor JE = new JobExecutor(eCAN, 0);
            JE.OnStateChange += JE_OnStateChange;

            Job jWrite = new Job(Job.JobType.Write);
            jWrite.Address = 0x20000000 + 32 * 1024;
            jWrite.DataNum = 16;
            jWrite.DataSend = new byte[jWrite.DataNum];
            for (int i = 0; i < jWrite.DataNum; i++)
                jWrite.DataSend[i] = (byte)i;

            Job jRead = new Job(Job.JobType.Read);
            jRead.Address = 0x20000000 + 32 * 1024;
            jRead.DataNum = 16;

            Job[] js = { jWrite, jRead };
            JE.SetJob(js);

            while (true)
            {
                JE.BackgroundRun(false);
                Thread.Sleep(100);
            }
        }

        private static void READTest()
        {
            ICAN eCAN = new ECANDev();
            eCAN.Open(0);
            eCAN.Start(0, 125000, false, true, false);

            JobExecutor JE = new JobExecutor(eCAN, 0);
            JE.OnStateChange += JE_OnStateChange;

            Job j = new Job(Job.JobType.Read);
            j.Address = 0x8000000;
            j.DataNum = 16;
            Job[] js = { j };
            JE.SetJob(js);

            while (true)
            {
                JE.BackgroundRun(false);
                Thread.Sleep(100);
            }
        }

        private static void ACKTest()
        {
            ICAN eCAN = new ECANDev();
            eCAN.Open(0);
            eCAN.Start(0, 125000, false, true, false);

            JobExecutor JE = new JobExecutor(eCAN, 0);
            JE.OnStateChange += JE_OnStateChange;

            Job[] js = new Job[3];
            js[0] = new Job(Job.JobType.ACK);
            js[1] = new Job(Job.JobType.GetState);
            js[2] = new Job(Job.JobType.ACK);
            JE.SetJob(js);

            while (true)
            {
                JE.BackgroundRun(false);
                Thread.Sleep(100);
            }
        }

        private static void HexTest()
        {
            IntelHexLoader loader = new IntelHexLoader();
            AddressDataGroup<byte> dataGroup = new AddressDataGroup<byte>();
            IntelHexLoader.ResultEnum r = loader.Load("a.hex", dataGroup);
        }

        private static void AllTest()
        {
            IntelHexLoader loader = new IntelHexLoader();
            AddressDataGroup<byte> dataGroup = new AddressDataGroup<byte>();
            IntelHexLoader.ResultEnum r = loader.Load("led.hex", dataGroup);

            List<Job> jobs = new List<Job>();
            JobMaker.EraseWrite(jobs, dataGroup, JobMaker.EraseOptEnum.All, null, true);

            ICAN eCAN = new ECANDev();
            eCAN.Open(0);
            eCAN.Start(0, 125000, false, true, false);

            JobExecutor JE = new JobExecutor(eCAN, 0);
            JE.OnStateChange += JE_OnStateChange;

            JE.SetJob(jobs.ToArray());

            //Job[] js = { jobs[0], jobs[1], jobs[2] };
            //JE.SetJob(js);

            while (true)
            {
                JE.BackgroundRun(false);
                Thread.Sleep(100);
            }
        }

        #endregion
    }
}
