using BUAA;
using BUAA.Device;
using BUAA.Misc;
using BUAA.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace STM32CANFlasher
{
    class Program
    {
        static void Main(string[] args)
        {
            //HexTest();

            READTest();
            ACKTest();
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

        private static void JE_OnStateChange(JobExecutor Executor, JobExecutor.JobEventType EventType, int JobNo, Job Job, string Msg)
        {
            Console.WriteLine(Job.Type + "@" + Msg);
        }

        private static void HexTest()
        {
            IntelHexLoader loader = new IntelHexLoader();
            AddressDataGroup<byte> dataGroup = new AddressDataGroup<byte>();
            IntelHexLoader.ResultEnum r = loader.Load("a.hex", dataGroup);
        }
    }
}
