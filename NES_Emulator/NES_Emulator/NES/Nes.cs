using System;
namespace NES_Emulator.NES
{
    public class Nes
    {
        public Cpu cpu { get; private set; }
        public PpuRegister ppuRegister { get; private set; }
        public Ppu ppu { get; private set; }

        public Nes(Cpu cpu, PpuRegister ppuRegister, Ppu ppu)
        {
            this.cpu = cpu;
            this.ppuRegister = ppuRegister;
            this.ppu = ppu;
        }
    }
}
