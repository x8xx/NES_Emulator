using System;
using System.Threading.Tasks;

namespace NES_Emulator.NES
{
    public class Nes
    {
        public Cpu cpu { get; private set; }
        public PpuRegister ppuRegister { get; private set; }
        public Ppu ppu { get; private set; }
        public Rom rom { get; private set; }
        public GameScreen gameScreen { get; private set; }

        public Nes()
        {
            
        }

        /// <summary>
        /// 電源ON
        /// 各クラスのインスタンス生成
        /// </summary>
        /// <returns><c>true</c>, 起動成功 <c>false</c> 起動失敗</returns>
        /// <param name="romBinary">Rom binary.</param>
        public bool PowerOn(byte[] romBinary)
        {
            rom = new Rom(romBinary);
            if (!rom.IsJudgmentNesRom()) return false;

            cpu = new Cpu(this);
            ppuRegister = new PpuRegister(this);
            ppu = new Ppu(this);

            rom.SpliteRom();
            for (int i = 0;i < rom.ProgramRom.Length;i++)
            {
                cpu.WriteMemory((ushort)(0x8000 + i), rom.ProgramRom[i]);
            }

            gameScreen = new GameScreen();
            return true;
        }

        /// <summary>
        /// CPUの命令を実効
        /// </summary>
        public void OperatingCpu()
        {
            cpu.Execute();
        }

        //CPUサイクル数
        public static int[] cpuCycle = new int[]
        {
            /*0x00*/ 7, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 4, 4, 6, 6,
            /*0x10*/ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
            /*0x20*/ 6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 4, 4, 6, 6,
            /*0x30*/ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
            /*0x40*/ 6, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 3, 4, 6, 6,
            /*0x50*/ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
            /*0x60*/ 6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 5, 4, 6, 6,
            /*0x70*/ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
            /*0x80*/ 2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
            /*0x90*/ 2, 6, 2, 6, 4, 4, 4, 4, 2, 4, 2, 5, 5, 4, 5, 5,
            /*0xA0*/ 2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
            /*0xB0*/ 2, 5, 2, 5, 4, 4, 4, 4, 2, 4, 2, 4, 4, 4, 4, 4,
            /*0xC0*/ 2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
            /*0xD0*/ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
            /*0xE0*/ 2, 6, 3, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
            /*0xF0*/ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        };

        //パレットカラー
        public static byte[][] paletteColors = new byte[][]
        {
            new byte[]{ 0x75, 0x75, 0x75 }, new byte[]{ 0x27, 0x1B, 0x8F }, 
            new byte[]{ 0x00, 0x00, 0xAB }, new byte[]{ 0x47, 0x00, 0x9F }, 
            new byte[]{ 0x8F, 0x00, 0x77 }, new byte[]{ 0xAB, 0x00, 0x13 }, 
            new byte[]{ 0xA7, 0x00, 0x00 }, new byte[]{ 0x7F, 0x0B, 0x00 }, 
            new byte[]{ 0x43, 0x2F, 0x00 }, new byte[]{ 0x00, 0x47, 0x00 }, 
            new byte[]{ 0x00, 0x51, 0x00 }, new byte[]{ 0x00, 0x3F, 0x17 }, 
            new byte[]{ 0x1B, 0x3F, 0x5F }, new byte[]{ 0x00, 0x00, 0x00 }, 
            new byte[]{ 0x05, 0x05, 0x05 }, new byte[]{ 0x05, 0x05, 0x05 },

            new byte[]{ 0xBC, 0xBC, 0xBC }, new byte[]{ 0x00, 0x73, 0xEF },
            new byte[]{ 0x23, 0x3B, 0xEF }, new byte[]{ 0x83, 0x00, 0xF3 },
            new byte[]{ 0xBF, 0x00, 0xBF }, new byte[]{ 0xE7, 0x00, 0x5B },
            new byte[]{ 0xDB, 0x2B, 0x00 }, new byte[]{ 0xCB, 0x4F, 0x0F },
            new byte[]{ 0x8B, 0x73, 0x00 }, new byte[]{ 0x00, 0x97, 0x00 },
            new byte[]{ 0x00, 0xAB, 0x00 }, new byte[]{ 0x00, 0x93, 0x3B },
            new byte[]{ 0x00, 0x83, 0x8B }, new byte[]{ 0x11, 0x11, 0x11 },
            new byte[]{ 0x09, 0x09, 0x09 }, new byte[]{ 0x09, 0x09, 0x09 },

            new byte[]{ 0xFF, 0xFF, 0xFF }, new byte[]{ 0x3F, 0xBF, 0xFF },
            new byte[]{ 0x5F, 0x97, 0xFF }, new byte[]{ 0xA7, 0x8B, 0xFD },
            new byte[]{ 0xF7, 0x7B, 0xFF }, new byte[]{ 0xFF, 0x77, 0xB7 },
            new byte[]{ 0xFF, 0x77, 0x63 }, new byte[]{ 0xFF, 0x9B, 0x3B },
            new byte[]{ 0xF3, 0xBF, 0x3F }, new byte[]{ 0x83, 0xD3, 0x13 },
            new byte[]{ 0x4F, 0xDF, 0x4B }, new byte[]{ 0x58, 0xF8, 0x98 },
            new byte[]{ 0x00, 0xEB, 0xDB }, new byte[]{ 0x66, 0x66, 0x66 },
            new byte[]{ 0x0D, 0x0D, 0x0D }, new byte[]{ 0x0D, 0x0D, 0x0D },

            new byte[]{ 0xFF, 0xFF, 0xFF }, new byte[]{ 0xAB, 0xE7, 0xFF },
            new byte[]{ 0xC7, 0xD7, 0xFF }, new byte[]{ 0xD7, 0xCB, 0xFF },
            new byte[]{ 0xFF, 0xC7, 0xFF }, new byte[]{ 0xFF, 0xC7, 0xDB },
            new byte[]{ 0xFF, 0xBF, 0xB3 }, new byte[]{ 0xFF, 0xDB, 0xAB },
            new byte[]{ 0xFF, 0xE7, 0xA3 }, new byte[]{ 0xE3, 0xFF, 0xA3 },
            new byte[]{ 0xAB, 0xF3, 0xBF }, new byte[]{ 0xB3, 0xFF, 0xCF },
            new byte[]{ 0x9F, 0xFF, 0xF3 }, new byte[]{ 0xDD, 0xDD, 0xDD },
            new byte[]{ 0x11, 0x11, 0x11 }, new byte[]{ 0x11, 0x11, 0x11 }
        };


    }
}
