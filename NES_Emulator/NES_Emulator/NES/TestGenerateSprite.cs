using System;
using System.Diagnostics;
using System.IO;
using Xamarin.Forms;

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

        byte[] buffer;
        public byte[] GenerateImage()
        {
            int width = 8, height = 8;
            int numPixels = width * height;
            int numPixelBytes = 4 * numPixels;
            int fileSize = 54 + numPixelBytes;
            buffer = new byte[fileSize];

            using(MemoryStream memoryStream = new MemoryStream(buffer))
            {
                using(BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    writer.Write(new char[]{ 'B', 'M' });
                    writer.Write(fileSize);
                    writer.Write((short)0);
                    writer.Write((short)0);
                    writer.Write(54);

                    writer.Write(40);
                    writer.Write(width);
                    writer.Write(-height);
                    writer.Write((short)1);
                    writer.Write((short)32);
                    writer.Write(0);
                    writer.Write(numPixelBytes);
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(0);

                }
            }

            int k = 50, count = 0;
            int index = 54;
            for (int i = k * 16; i < k * 16 + 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (sprite[k, count, j] != 0)
                    {
                        Color color = Color.Black;
                        buffer[index] = (byte)(int)(255 * color.B);
                        buffer[index + 1] = (byte)(int)(255 * color.G);
                        buffer[index + 2] = (byte)(int)(255 * color.R);
                        buffer[index + 3] = 255;
                        Debug.Write("■");
                    }
                    else
                    {
                        Color color = Color.White;
                        buffer[index] = (byte)(int)(255 * color.B);
                        buffer[index + 1] = (byte)(int)(255 * color.G);
                        buffer[index + 2] = (byte)(int)(255 * color.R);
                        buffer[index + 3] = 255;
                        Debug.Write("□");
                    }
                    index += 4;
                }
                Debug.WriteLine("");
                count++;
            }
            return buffer;
        }

    }
}
