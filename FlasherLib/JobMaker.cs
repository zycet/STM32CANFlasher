using BUAA.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BUAA.Flasher
{
    public class JobMaker
    {
        public struct FlashSectionStruct
        {
            public int Address;
            public int Size;

            public FlashSectionStruct(int Address, int Size)
            {
                this.Address = Address;
                this.Size = Size;
            }

            public bool IsInSection(int Address)
            {
                return (Address >= this.Address && Address < this.Address + this.Size);
            }
        }

        public enum EraseOptEnum
        {
            None,
            OnlyUsed,
            All
        }

        public static int EraseWrite(List<Job> Jobs, AddressDataGroup<byte> DataGroup, EraseOptEnum EraseOpt, FlashSectionStruct[] FlashSection, bool IsACKFirst)
        {
            if (EraseOpt == EraseOptEnum.OnlyUsed)
            {
                if (FlashSection == null || FlashSection.Length == 0)
                {
                    throw new ArgumentException("FlashSection not Available.");
                }
            }

            int num = 0;

            if (IsACKFirst)
            {
                Job j = new Job(Job.JobType.ACK);
                Jobs.Add(j);
                num++;
            }

            if (EraseOpt == EraseOptEnum.None)
            { }
            else if (EraseOpt == EraseOptEnum.OnlyUsed)
            {
                num += Erase(Jobs, DataGroup, FlashSection);
            }
            else if (EraseOpt == EraseOptEnum.All)
            {
                num += Erase(Jobs);
            }

            num += Write(Jobs, DataGroup);

            return num;
        }

        public static int Write(List<Job> Jobs, AddressDataGroup<byte> DataGroup)
        {
            if (DataGroup.Groups.Count != 1)
            {
                throw new ArgumentException("DataGroup must only have 1 Group.");
            }

            int count = DataGroup.Groups[0].Datas.Count;

            int num = 0;
            int p = 0;

            while (p < count)
            {
                int len = count - p;
                if (len > 256)
                    len = 256;

                byte[] bs = new byte[len];
                DataGroup.Groups[0].Datas.CopyTo(p, bs, 0, len);

                Job j = new Job(Job.JobType.Write, bs);
                j.Address = DataGroup.Groups[0].Address + p;
                Jobs.Add(j);

                p += 256;
                num++;
            }
            return num;
        }

        public static int Erase(List<Job> Jobs, AddressDataGroup<byte> DataGroup, FlashSectionStruct[] FlashSection)
        {
            if (DataGroup.Groups.Count != 1)
            {
                throw new ArgumentException("DataGroup must only have 1 Group.");
            }

            int dataAddress = DataGroup.Groups[0].Address;
            int dataSize = DataGroup.Groups[0].Datas.Count;

            List<byte> pageNos = new List<byte>();

            bool isDataStartIn = false;
            bool isDataEndIn = false;

            for (int i = 0; i < FlashSection.Length; i++)
            {
                if (FlashSection[i].IsInSection(dataAddress))
                    isDataStartIn = true;
                if (FlashSection[i].IsInSection(dataAddress + dataSize - 1))
                    isDataEndIn = true;

                if (FlashSection[i].Address > dataAddress + dataSize - 1)
                { }
                else if (FlashSection[i].Address + FlashSection[i].Size - 1 < dataAddress)
                { }
                else
                {
                    pageNos.Add((byte)i);
                }
            }

            if (isDataStartIn = false || isDataEndIn == false)
            {
                throw new ArgumentException("DataGroup not in FlashSection Range.");
            }

            return Erase(Jobs, pageNos.ToArray());
        }

        public static int Erase(List<Job> Jobs, byte[] PageNos)
        {
            int p = 0;
            int n = 0;
            while (p < PageNos.Length)
            {
                int len = PageNos.Length - p;
                if (len > 8)
                    len = 8;
                byte[] bs = new byte[len];
                Array.Copy(PageNos, p, bs, 0, len);

                Job j = new Job(Job.JobType.Erase, bs);
                Jobs.Add(j);

                p += len;
                n++;
            }
            return n;
        }

        public static int Erase(List<Job> Jobs)
        {
            byte[] bs = { 0xFF };
            Job j = new Job(Job.JobType.Erase, bs);
            Jobs.Add(j);
            return 1;
        }
    }
}
