using System;

namespace NES_Emulator.NES
{
    public class TestGenerateSprite
    {
        byte[] characterRom;
        int pointer;

        public TestGenerateSprite(byte[] romData)
        {
            Rom rom = new Rom(romData);
            rom.SpliteRom();
            characterRom = rom.CharacterRom;
            pointer = 0;
        }

        public void GetSprite()
        {
            for (int i = pointer;i < pointer + 8;i++)
            {
                while()
                {
                    
                }
            }
            pointer += 16;
        }
    }
}
