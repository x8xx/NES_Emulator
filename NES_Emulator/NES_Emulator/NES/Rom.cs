using System;
namespace NES_Emulator.NES
{
    public class Rom
    {
        byte[] rom;
        byte[] header;
        byte[] programRom;
        byte[] characterRom;

        public Rom(byte[] rom)
        {
            this.rom = rom;
            header = new byte[0x10];
            programRom = new byte[rom[4] * 0x4000];
            characterRom = new byte[rom[5] * 0x2000];
        }

        public void RomLoad()
        {

        }

        public void SpliteRom()
        {
            int hCount = 0, pCount = 0, cCount = 0;
            for (int i = 0; i < rom.Length; i++)
            {
                if (i < 0x10)
                {
                    header[hCount] = rom[i];
                    hCount++;
                }
                else if (i < programRom.Length)
                {
                    programRom[pCount] = rom[i];
                    pCount++;
                }
                else
                {
                    characterRom[cCount] = rom[i];
                    cCount++;
                }
            }
        }

        /// <summary>
        /// NESのRomか判別
        /// </summary>
        /// <returns><c>true</c>, if judgment NESR om was ised, <c>false</c> otherwise.</returns>
        public bool IsJudgmentNESRom()
        {
            if (header[0] == 0x4E && header[1] == 0x45 && header[2] == 0x53 && header[3] == 0x1A)
            {
                return true;
            }
            return false;
        }

    }
}
