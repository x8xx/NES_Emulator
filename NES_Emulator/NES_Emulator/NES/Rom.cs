using System.Diagnostics;
namespace NES_Emulator.NES
{
    public class Rom
    {
        byte[] romBinary; //Romファイル

        public byte[] Header { get; private set; }
        public byte[] ProgramRom { get; private set; }
        public byte[] CharacterRom { get; private set; }

        public Rom(byte[] romBinary)
        {
            this.romBinary = romBinary;
            Header = new byte[0x10];
            ProgramRom = new byte[romBinary[4] * 0x4000];
            CharacterRom = new byte[romBinary[5] * 0x2000];
        }

        /// <summary>
        /// Romファイルをヘッダー, プログラムRom, キャラクターRomに分割
        /// </summary>
        public void SpliteRom()
        {
            int hCount = 0, pCount = 0, cCount = 0;
            for (int i = 0; i < romBinary.GetLength(0); i++)
            {
                if (i < 0x10)
                {
                    Header[hCount] = romBinary[i];
                    hCount++;
                }
                else if (pCount < ProgramRom.Length)
                {
                    ProgramRom[pCount] = romBinary[i];
                    pCount++;
                }
                else if (cCount < CharacterRom.Length)
                {
                    CharacterRom[cCount] = romBinary[i];
                    cCount++;
                }
            }
        }

        /// <summary>
        /// NESのRomか判別
        /// </summary>
        /// <returns><c>true</c>, NESファイル, <c>false</c> 違う</returns>
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
