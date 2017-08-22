using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using BUAA.Device;
using BUAA.Flasher;
using BUAA.Hibernate;
using BUAA.Misc;
using BUAA.Structure;

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

        private static void MakeFlashSection(string File, int BaseAddress, byte[] Sizes)
        {
            int n = Sizes.Length;
            JobMaker.FlashSectionStruct[] fss = new JobMaker.FlashSectionStruct[n];
            for (int i = 0; i < n; i++)
            {
                fss[i] = new JobMaker.FlashSectionStruct(BaseAddress, Sizes[i] * 1024);
                BaseAddress += Sizes[i] * 1024;
            }
            SerializerHelper.SerializerIn(fss, File);
            SerializerHelper.SerializerIn(fss, "stm32f4xx_1mb.xml");
        }

        private static void stm32f4xx_1mb_fss()
        {
            byte[] sizeFlash = { 16, 16, 16, 16, 64, 128, 128, 128, 128, 128, 128, 128 };
            MakeFlashSection("stm32f4xx_1mb.xml", 0x08000000, sizeFlash);
        }

        #endregion

        static void Main(string[] args)
        {
            //Config Parse
            WriteLine("===== Config Parse =====");
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
            WriteLine("===== Hex Load =====");
            AddressDataGroup<byte> dataGroup = new AddressDataGroup<byte>();
            try
            {
                IntelHexLoader hexLoader = new IntelHexLoader();
                IntelHexLoader.ResultEnum r = hexLoader.Load(config.HexFile, dataGroup);
                if (dataGroup.Groups.Count != 1)
                {
                    ErrorWriteLine("Hex Not Have One Data Section.");
                    return;
                }
                if (r != IntelHexLoader.ResultEnum.Success)
                {
                    ErrorWriteLine(r.ToString());
                    return;
                }
                WriteLine("Hex Data Size:" + dataGroup.Groups[0].Datas.Count);
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
                    WriteLine("===== FlashSection Load =====");
                    FlashSection = (JobMaker.FlashSectionStruct[])SerializerHelper.Deserialize(config.FlashSectionFile, typeof(JobMaker.FlashSectionStruct[]));
                    WriteLine("FlashSection Num:" + FlashSection.Length);
                }
                catch (Exception ee)
                {
                    ErrorWriteLine(ee.Message);
                    //return;
                }
            }

            //Job Make
            List<Job> jobs = new List<Job>();
            try
            {
                WriteLine("===== Job Make =====");
                JobMaker.EraseWrite(jobs, dataGroup, config.EraseOpt, FlashSection, true);

                if (config.JumpToFlash)
                {
                    JobMaker.Go(jobs, dataGroup.Groups[0].Address);
                }

                WriteLine("Job Num:" + jobs.Count);
            }
            catch (Exception ee)
            {
                ErrorWriteLine(ee.Message);
                return;
            }

            //CAN Open
            ICAN CANDev = null;
            try
            {
                WriteLine("===== CAN Open =====");
                string dllPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.CANDevFile);
                Assembly ass = Assembly.LoadFile(dllPath);
                Type tCAN = ass.GetType(config.CANDevClass);
                CANDev = (ICAN)Activator.CreateInstance(tCAN);
                bool rOpen = CANDev.Open(config.CANDeviceNo);
                if (!rOpen)
                {
                    ErrorWriteLine("CAN Open Fail.");
                    return;
                }
                bool rStart = CANDev.Start(config.CANPortNo, config.CANBPS, false, config.CANSendOnce, false);
                if (!rOpen)
                {
                    ErrorWriteLine("CAN Start Fail.");
                    return;
                }
                WriteLine("CAN Open Success");
            }
            catch (Exception ee)
            {
                ErrorWriteLine(ee.Message);
                return;
            }

            //JobExecutor Init
            WriteLine("===== JobExecutor Init =====");
            JobExecutor JE = new JobExecutor(CANDev, config.CANPortNo);
            JE.IsShowSendRecive = config.ShowReceiveSend;
            JE.TimeoutSpan = new TimeSpan(0, 0, config.Timeout);
            JE.TimeoutSpanErase = new TimeSpan(0, 0, config.TimeoutErase);
            JE.OnStateChange += JE_OnStateChange;
            JE.SetJob(jobs.ToArray());

            //Run
            WriteLine("===== JobExecutor Running =====");
            while (JE.IsRunning)
            {
                JE.BackgroundRun(false);
                Thread.Sleep(100);
            }
            WriteLine("End With:" + JE.JobsState);
            Console.ReadLine();
        }
    }
}
