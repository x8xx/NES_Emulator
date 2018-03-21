using System;

namespace NES_Emulator.NES
{
    public class Nes
    {
        public Cpu cpu { get; private set; }
        public Ppu ppu { get; private set; }
        public GameScreen gameScreen { get; private set; }

        public int CharacterRimSize { get; set; }
        public bool verticalMirror { get; set; }

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
            if (!(romBinary[0] == 0x4E && romBinary[1] == 0x45 && romBinary[2] == 0x53 && romBinary[3] == 0x1A)) return false;
            CharacterRimSize = romBinary[5] * 0x2000;
            verticalMirror = (romBinary[6] % 2) != 0;

            cpu = new Cpu(this);
            ppu = new Ppu(this);

            int count = 0x10;
            for (int i = 0;i < romBinary[4] * 0x4000;i++, count++) //ProgramRom書き込み
            {
                cpu.WriteMemory((ushort)(0x8000 + i), romBinary[count]);
            }

            for (int i = 0; i < CharacterRimSize;i++, count++) //CharactarRom書き込み
            {
                ppu.WriteMemory((ushort)i, romBinary[count]);
            }

            ppu.LoadSprite();

            gameScreen = new GameScreen();
            return true;
        }

        int coun = 0;
        /// <summary>
        /// CPUの命令を実効
        /// </summary>
        public void OperatingCpu()
        {
            while(!ppu.notificationScreenUpdate)
            {
                if (coun > 46000)
                    //cpu.DebugWriteValue(coun);
                cpu.Execute();
                coun++;
            }
        }

        /// <summary>
        /// CPUメモリを読み込む
        /// </summary>
        /// <returns>値</returns>
        /// <param name="address">アドレス</param>
        public byte ReadCpuMemory(ushort address)
        {
            return cpu.ReadMemory(address);
        }


        /// <summary>
        /// nBitを取り出す
        /// </summary>
        /// <returns>The bit.</returns>
        /// <param name="value">Value.</param>
        /// <param name="n">n</param>
        public static byte FetchBit(int value, int n)
        {
            if ((value & (byte)Math.Pow(2, n)) != 0)
                return 1;
            else
                return 0;
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
    }
}
