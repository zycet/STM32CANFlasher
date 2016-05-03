using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BUAA;
using System.Reflection;

namespace BUAA.Flasher
{
    public class ConfigStruct
    {
        public string HexFile = "";
        public JobMaker.EraseOptEnum EraseOpt = JobMaker.EraseOptEnum.All;
        public string FlashSectionFile = "";

        public bool ShowReceiveSend = false;

        public int CANDeviceNo = 0;
        public uint CANPortNo = 0;
        public int CANBPS = 125000;
        public bool CANSendOnce = false;

        public bool JumpToFlash = false;

        public static int Parse(ConfigStruct Config, string[] args, ref string Msg)
        {
            int r = 0;

            if (args == null)
            {
                Msg = ("Args Null");
                return -1;
            }

            for (int i = 0; i < args.Length; i++)
            {
                try
                {
                    string[] ss = args[i].Split('=');
                    ss[0] = ss[0].ToLower();
                    if (ss.Length != 2)
                    {
                        Msg = ("Arg unable Accept:" + args[i]);
                        return -1;
                    }

                    r++;

                    if (ss[0].StartsWith("-hexfile"))
                    {
                        Config.HexFile = ss[1];
                    }
                    else if (ss[0].StartsWith("-eraseopt"))
                    {
                        Config.EraseOpt = (JobMaker.EraseOptEnum)Enum.Parse(typeof(JobMaker.EraseOptEnum), ss[1]);
                    }
                    else if (ss[0].StartsWith("-flashsectionfile"))
                    {
                        Config.FlashSectionFile = ss[1];
                    }
                    else if (ss[0].StartsWith("-showreceivesend"))
                    {
                        if (ss[1].ToLower() == "y" || ss[1] == "yes")
                            Config.ShowReceiveSend = true;
                        else
                            Config.ShowReceiveSend = false;
                    }
                    else
                    {
                        r--;
                        Msg = ("Arg unable Accept:" + args[i]);
                        return -1;
                    }

                }
                catch (Exception ee)
                {
                    Msg = ("Arg Parse Fail:" + args[i] + Environment.NewLine + ee.Message);
                    return -1;
                }
            }
            return r;
        }
    }
}
