using System.IO;
using System.Collections.Generic;

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

            ScreenMemoryStream = new MemoryStream(screen);
        }

        /// <summary>
        /// 画面の更新
        /// </summary>
        /// <param name="table">Table.</param>
        public void RenderScreen(byte[][] table, int renderLine)
        {
            for (int i = headerSize + renderLine * 256 * 4, j = renderLine * 256;j < (renderLine + 1) * 256;i += 4, j++)
            {
                screen[i] = table[j][2];
                screen[i + 1] = table[j][1];
                screen[i + 2] = table[j][0];
                screen[i + 3] = 255;
            }
            ScreenMemoryStream.Dispose();
            ScreenMemoryStream = new MemoryStream(screen);
        }
    }
}
