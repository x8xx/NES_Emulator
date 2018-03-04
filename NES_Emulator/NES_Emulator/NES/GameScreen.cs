using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace NES_Emulator.NES
{
    public class GameScreen
    {
        const int headerSize = 54;
        byte[] screen;
        public MemoryStream ScreenMemoryStream { get; private set; }

        /// <summary>
        /// BMPを作成
        /// </summary>
        public GameScreen()
        {
            screen = new byte[245814];;

            using (MemoryStream memoryStream = new MemoryStream(screen))
            {
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    //BitmapFileHeader(14byte)
                    writer.Write(new char[] { 'B', 'M' });  //シグネチャ
                    writer.Write(245814); //ファイルサイズ
                    writer.Write((short)0); //予約領域
                    writer.Write((short)0); //予約領域
                    writer.Write(headerSize); //ピクセルまでのオフセット

                    //BitmapInfoHeader(40byte)
                    writer.Write(40); //ヘッダーサイズ
                    writer.Write(256); //width
                    writer.Write(-240); //height
                    writer.Write((short)1); //プレーン数
                    writer.Write((short)32); //ピクセルあたりのビット数
                    writer.Write(0); //圧縮形式
                    writer.Write(245760); //画像サイズ
                    writer.Write(0); //横方向の解像度
                    writer.Write(0); //縦方向の解像度
                    writer.Write(0); //色テーブルの色の数
                    writer.Write(0); //重要な色の数
                }
            }

            InitialScreen(); //画面の初期化
            ScreenMemoryStream = new MemoryStream(screen);
        }

        /// <summary>
        /// 画面の初期化(黒)
        /// </summary>
        public void InitialScreen()
        {
            for (int i = headerSize;i < screen.Length;i++)
            {
                screen[i] = 225;
            }
        }

        /// <summary>
        /// 画面の更新
        /// </summary>
        /// <param name="table">Table.</param>
        public void RenderScreen(byte[][] table)
        {
            for (int i = headerSize, j = 0;j < 61440;i += 4, j++)
            {
                Debug.WriteLine("RS: " + i);
                screen[i] = table[j][2];
                screen[i + 1] = table[j][1];
                screen[i + 2] = table[j][0];
                screen[i + 3] = 255;
            }
        }
    }
}
