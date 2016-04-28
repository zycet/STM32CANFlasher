using BUAA.Misc;
using BUAA.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STM32CANFlasher
{
    class Program
    {
        static void Main(string[] args)
        {
            IntelHexLoader loader = new IntelHexLoader();
            AddressDataGroup<byte> dataGroup = new AddressDataGroup<byte>();
            IntelHexLoader.ResultEnum r = loader.Load("a.hex", dataGroup);
        }
    }
}
