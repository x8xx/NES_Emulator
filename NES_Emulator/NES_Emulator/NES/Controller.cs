using System;
namespace NES_Emulator.NES
{
    public class Controller
    {
        

        public Controller()
        {
        }

        public byte ReadIoRegister(ushort address)
        {
            switch(address)
            {
                case 0x4016:
                    break;
                case 0x4017:
                    break;
            }
            return 0;
        }

    }
}
