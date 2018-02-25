using System.Diagnostics;
namespace NES_Emulator.NES
{
    public class Rom
    {
        byte[] rom;
        byte[] _header;
        byte[] _programRom;
        byte[] _characterRom;

        public byte[] Header
        {
            get { return _header; }
            private set { _header = value; }
        }

        public byte[] ProgramRom
        {
            get { return _programRom; }
            private set { _programRom = value; }
        }

        public byte[] CharacterRom
        {
            get { return _characterRom; }
            private set { _characterRom = value; }
        }


        public Rom(byte[] rom)
        {
            this.rom = rom;
            Header = new byte[0x10];
            ProgramRom = new byte[rom[4] * 0x4000];
            CharacterRom = new byte[rom[5] * 0x2000];
            SpliteRom();
        }

        public void RomLoad()
        {

        }

        public void SpliteRom()
        {
            int hCount = 0, pCount = 0, cCount = 0;
            for (int i = 0; i < rom.GetLength(0); i++)
            {
                if (i < 0x10)
                {
                    Header[hCount] = rom[i];
                    hCount++;
                }
                else if (pCount < ProgramRom.Length)
                {
                    ProgramRom[pCount] = rom[i];
                    pCount++;
                }
                else if (cCount < CharacterRom.Length)
                {
                    CharacterRom[cCount] = rom[i];
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
            if (Header[0] == 0x4E && Header[1] == 0x45 && Header[2] == 0x53 && Header[3] == 0x1A)
            {
                return true;
            }
            return false;
        }

    }
}
