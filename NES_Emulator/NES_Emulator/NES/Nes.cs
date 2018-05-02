using System;
using System.Threading.Tasks;

namespace NES_Emulator.NES
{
    public class Nes
    {
        public Cpu cpu { get; private set; }
        public Ppu ppu { get; private set; }
        public GameScreen gameScreen { get; private set; }
		public Controller controller { get; private set; }

        public int CharacterRimSize { get; set; }
        public bool verticalMirror { get; set; }

        /// <summary>
        /// 電源ON
        /// 各クラスのインスタンス生成
        /// </summary>
        /// <returns><c>true</c>, 起動成功 <c>false</c> 起動失敗</returns>
        /// <param name="romBinary">Rom binary.</param>
        public bool PowerOn(byte[] romBinary)
        {
            if (!(romBinary[0] == 0x4E && romBinary[1] == 0x45 && romBinary[2] == 0x53 && romBinary[3] == 0x1A))
				return false;
            CharacterRimSize = romBinary[5] * 0x2000;
            verticalMirror = (romBinary[6] % 2) != 0;

            cpu = new Cpu(this);
            ppu = new Ppu(this);
			controller = new Controller();

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
				 /*Task.Run(() =>
				{
					//if (coun > 10000)
                        //cpu.DebugWriteValue(coun);
					cpu.Execute();
                    coun++;
				});*/
				cpu.Execute();
                coun++;
				//if (coun > 10000)
                //cpu.DebugWriteValue(coun);
                //cpu.Execute();
                //coun++;
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
        /// IOレジスタ書き込み
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="value">Value.</param>
        public void WriteIoRegister(ushort address, byte value)
        {
            if ((address >= 0x2000 && address <= 0x2007) || address == 0x4014)
                ppu.WritePpuRegister(address, value);
            if (address == 0x4016 || address == 0x4017)
                controller.WriteIoRegister(address, value);
        }


        /// <summary>
        /// IOレジスタ読み込み
        /// </summary>
        /// <returns>The io register.</returns>
        /// <param name="address">Address.</param>
        public byte ReadIoRegister(ushort address)
        {
            if (address == 0x2002 || address == 0x2007)
                return ppu.ReadPpuRegister(address);
            if (address == 0x4016 || address == 0x4017)
                return (byte)(controller.ReadIoRegister(address) ? 1 : 0);
            return 0x00;
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
    }
}
