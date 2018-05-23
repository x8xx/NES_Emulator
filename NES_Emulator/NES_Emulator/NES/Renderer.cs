using System;
using System.IO;
using Xamarin.Forms;

namespace NES_Emulator.NES
{
    public class Renderer
    {
		const int HEADER_SIZE = 54;
        byte[] gameScreen;
		byte[] nameTable;
		byte[] attrTable;
        
		public int BgPatternTable { get; set; } 
		public int SpritePatternTable { get; set; }
		public int SpriteSize { get; set; }
		public Mirror ScreenMirror { get; set; }
        public ImageSource GameScreen { get { return ImageSource.FromStream(() => new MemoryStream(gameScreen)); } }

        public Renderer()
        {
			//BMP画像生成
            gameScreen = new byte[245814];
            using (MemoryStream memoryStream = new MemoryStream(gameScreen))
            {
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    //BitmapFileHeader(14byte)
                    writer.Write(new char[] { 'B', 'M' });  //シグネチャ
                    writer.Write(245814); //ファイルサイズ
                    writer.Write((short)0); //予約領域
                    writer.Write((short)0); //予約領域
					writer.Write(HEADER_SIZE); //ピクセルまでのオフセット

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
			nameTable = new byte[3840];
			attrTable = new byte[3840];
        }

		public void RenderScreen(int x, int y)
		{

		}

        void RenderBackGround()
		{

		}

        void RenderSprite()
		{

		}

		public void WriteNameTable(ushort address, byte value)
		{
			int tableAddress = 0;
			if (address >= 0x2000 && address <= 0x23BF)
				tableAddress = ((address - 0x2000) / 32) * 64 + address % 32;
			else if (address >= 0x2400 && address <= 0x27BF)
			{
				switch (ScreenMirror)
                {
                    case Mirror.HorizontalMirror:
						tableAddress =  ((address - 0x2000 - 0x40) / 32) * 64 + address % 32 + 64 * 30;
                        break;
                    case Mirror.VerticalMirror:
						tableAddress = ((address - 0x2000 - 0x40) / 32) * 64 + address % 32 + 32;
                        break;
                }
			}
			nameTable[tableAddress] = value;
			switch (ScreenMirror)
			{
				case Mirror.HorizontalMirror:
					nameTable[tableAddress + 32] = value;
					break;
				case Mirror.VerticalMirror:
					nameTable[tableAddress + 64 * 30] = value;
					break;
			}
		}

        public void WriteAttrTable(ushort address, byte value)
		{
			int tableAddress = 0;
            if (address >= 0x23C0 && address <= 0x23FF)
                tableAddress = ((address - 0x23C0) / 8) * 16 + address % 8;
            else if (address >= 0x2400 && address <= 0x27BF)
            {
                switch (ScreenMirror)
                {
                    case Mirror.HorizontalMirror:
                        tableAddress = ((address - 0x23C0 - 0x3C0) / 8) * 16 + address % 8 + 16 * 30;
                        break;
                    case Mirror.VerticalMirror:
                        tableAddress = ((address - 0x2000 - 0x3C0) / 32) * 64 + address % 32 + 32;
                        break;
                }
            }
            nameTable[tableAddress] = value;
            switch (ScreenMirror)
            {
                case Mirror.HorizontalMirror:
                    nameTable[tableAddress + 32] = value;
                    break;
                case Mirror.VerticalMirror:
                    nameTable[tableAddress + 64 * 30] = value;
                    break;
            }
			for (int i = 0; i < 4; i++)
			{

			}
		}

		/// <summary>
        /// 4つあるパレットの内どのパレットを使うか決める
        /// bit 76543210
        /// 位置  3 2 1 0
        /// </summary>
        /// <returns>パレット番号</returns>
        /// <param name="value">属性テーブルの値</param>
        /// <param name="order">属性テーブルのどの位置の値か</param>
        int GetPalette(byte value, int order)
        {
            string binaryNumber = BinaryNumberConversion(value);
            return int.Parse(binaryNumber[order * 2 + 1].ToString()) + int.Parse(binaryNumber[order * 2].ToString());
        }

		/// <summary>
        /// 8bit2進数の文字列に変換
        /// </summary>
        /// <returns>8bit</returns>
        /// <param name="originNumber">元値</param>
        string BinaryNumberConversion(byte originNumber)
        {
            string convertNumber = Convert.ToString(originNumber, 2);
            int length = convertNumber.Length;
            if (length < 8)
            {
                for (int i = 0; i < 8 - length; i++)
                {
                    convertNumber = convertNumber.Insert(0, "0");
                }
            }
            return convertNumber;
        }

    }

	public enum Mirror
	{
		HorizontalMirror, //水平ミラー
		VerticalMirror //垂直ミラー
	}

}
