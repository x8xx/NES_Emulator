using System;
using System.Diagnostics;
using System.IO;

namespace NES_Emulator.NES
{
    public class TestGenerateSprite
    {
        byte[] characterRom;
        byte[,,] sprite;

        public TestGenerateSprite(byte[] romData)
        {
            Rom rom = new Rom(romData);
            rom.SpliteRom();
            characterRom = rom.CharacterRom;
            sprite = new byte[characterRom.Length / 16, 8, 8];
        }

        public void GetSprite()
        {
            int count = 0;
            for (int k = 0;k < sprite.GetLength(0);k++)
            {
                for (int i = k*16; i < k*16 + 8; i++)
                {
                    string highOrder = Convert10to2(characterRom[i + 8]);
                    string lowOrder = Convert10to2(characterRom[i]);
                    for (int j = 0; j < 8; j++)
                    {
                        //Debug.WriteLine("k="+k+"i="+i+"j="+j);
                        sprite[k, count, j] = (byte)(int.Parse(highOrder[j].ToString()) * 2 + int.Parse(lowOrder[j].ToString()));
                    }
                    count++;
                }
                count = 0;
            }
        }

        public string Convert10to2(byte num)
        {
            string result = "";
            int count = 0;
            while(num != 0)
            {
                int i = num % 2;
                num /= 2;
                result = result.Insert(0, i.ToString());
                count++;
            }
            for (int i = count; i < 8;i++)
            {
                result = result.Insert(0, "0");
            }
            return result;
        }

        public void  Convert2to16()
        {
            
        }

        public void PrintSprit()
        {
            int count = 0;
            for (int k = 0; k < sprite.GetLength(0); k++)
            {
                Debug.WriteLine(k);
                for (int i = k * 16; i < k * 16 + 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        if (sprite[k, count, j] != 0)
                        {
                            Debug.Write("■");
                        }
                        else
                        {
                            Debug.Write("□");
                        }

                    }
                    Debug.WriteLine("");
                    count++;
                }
                count = 0;
                Debug.WriteLine("\r\n");
            }
        }

        public byte[] GenerateImage()
        {
            byte[] image = new byte[64 * 3];
            int k = 50, count = 0, u = 0;;
            for (int i = k * 16; i < k * 16 + 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (sprite[k, count, j] != 0)
                    {
                        image[u] = 0x00;
                        image[u + 1] = 0x00;
                        image[u + 2] = 0x00;
                        Debug.Write("■");
                    }
                    else
                    {
                        image[u] = 0xff;
                        image[u + 1] = 0xff;
                        image[u + 2] = 0xff;
                        Debug.Write("□");
                    }

                }
                u += 4;
                Debug.WriteLine("");
                count++;
            }
            return image;
        }

    }
}
