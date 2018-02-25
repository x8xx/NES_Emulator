using System.IO;
using System.Collections.Generic;

namespace NES_Emulator.NES
{
    public class GameScreen
    {
        const int headerSize = 54;
        byte[] screen;
        MemoryStream _screenMemoryStream;
        public MemoryStream ScreenMemoryStream
        {
            get { return _screenMemoryStream; }
            private set { _screenMemoryStream = value; }
        }

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
                screen[i] = 0;
            }
        }

        /// <summary>
        /// 画面の更新
        /// </summary>
        /// <param name="table">Table.</param>
        public void UpdateScreen(byte[][] table)
        {
            for (int i = 0;i < table.Length;i += 4)
            {
                screen[i + headerSize] = table[i][2];
                screen[i + headerSize + 1] = table[i][1];
                screen[i + headerSize + 2] = table[i][0];
                screen[i + headerSize + 3] = 255;
            }
        }
    }
}