using System;
using System.IO;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Diagnostics;

namespace NES_Emulator.NES
{
    public class Renderer
    {
		const int HEADER_SIZE = 54;
		byte[] patternTable;
        byte[] gameScreen;
		byte[] nameTable;
		byte[] attrTable;
		Oam[] oamTable;
        class Oam
        {
			public int X { get; set; }
			public int Y { get; set; }
			public int TileID { get; set; } //タイルID
			public int PatternTable { get; set; } //パターンテーブル8x16のみ
			public bool HorizontalReverse { get; set; } //垂直反転
			public bool VerticalReverse { get; set; } //水平反転
			public bool Priority { get; set; } //優先度
			public int Palette { get; set; } //パレット
        }

		int screenOffset; //現在描画中のスクリーンの位置
		Dictionary<int, Oam> spriteRenderPosition; //描画書き込み時にスプライトの書き込み座標を一時的に保存しておく
		List<int>[] spritePosition; //spriteRenderPositionに保存している座標を保存

		MirrorType mirrorType;
        
		public int ScrollOffsetX { get; set; }
		public int ScrollOffsetY { get; set; }
		public byte[] Palette { get; set; } //パレット
		public byte[,,] Sprite { get; set; } //スプライト保存用
		public int BgPatternTable { get; set; } //BGのパターンテーブル
		public int SpritePatternTable { get; set; } //スプライトのパターンテーブル
		public bool IsBGVisible { get; set; } //BGを表示するかどうか
		public bool IsSpriteVisible { get; set; } //スプライトを表示するかどうか
		public int SpriteSize { get; set; } //スプライトサイズ
		public bool SpriteHit { get; set; } //0爆弾
		public int DisplayTable { get; set; } //画面に表示するテーブル
        public ImageSource GameScreen { get { return ImageSource.FromStream(() => new MemoryStream(gameScreen)); } }

		public Renderer(MirrorType mirrorType)
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
			SpriteSize = 8;
			Palette = new byte[0x20];
			patternTable = new byte[0x2000];
			nameTable = new byte[3840];
			attrTable = new byte[3840];
			screenOffset = HEADER_SIZE;
			oamTable = new Oam[0x40];
			spriteRenderPosition = new Dictionary<int, Oam>();
			spritePosition = new List<int>[0x40];
            this.mirrorType = mirrorType;
        }


        /// <summary>
        /// スクリーンの描画
		/// 1dotずつ描画
        /// </summary>
        /// <param name="x">読み込みx座標</param>
        /// <param name="y">読み込みy座標</param>
		public void RenderScreen(int x, int y)
		{
			byte[] colors = RenderSprite(x, y);
			for (int i = 2; i >= 0; i--)
				gameScreen[screenOffset++] = colors[i];
			gameScreen[screenOffset++] = 255;
			if (screenOffset >= gameScreen.Length)
				screenOffset = HEADER_SIZE;
		}

        /// <summary>
        /// BGの描画
        /// </summary>
        /// <returns>色の配列</returns>
		/// <param name="x">読み込みx座標</param>
        /// <param name="y">読み込みy座標</param>
        byte[] RenderBackGround(int x, int y)
		{
			switch(DisplayTable)
			{
				case 1:
					x += 256;
					break;
				case 2:
					y += 240;
					break;
				case 3:
					x += 256;
					y += 240;
					break;
			}
			x += ScrollOffsetX;
			y += ScrollOffsetY;

            if (x >= 512)
                x -= 512;
            if (y >= 480)
                y -= 480;

			int tableAddress = x / 8 + (y / 8) * 64;
            int column = x % 8;
            int line = y % 8;

			int paletteNumber = 4 * attrTable[tableAddress];
			int colorNumber = Sprite[nameTable[tableAddress] + BgPatternTable, line, column];
			if (colorNumber == 0)
				paletteNumber = 0;
			return paletteColors[Palette[paletteNumber + colorNumber]];

		}
        
        /// <summary>
        /// スプライトの描画
        /// </summary>
		/// <returns>色の配列</returns>
        /// <param name="x">読み込みx座標</param>
        /// <param name="y">読み込みy座標</param>
        byte[] RenderSprite(int x, int y)
		{
			int position = x + y * 256;         
			if (!spriteRenderPosition.ContainsKey(position))
				return RenderBackGround(x, y);

			if (spriteRenderPosition[position].TileID == 0 && spriteRenderPosition[position].PatternTable == 0)
			{
				SpriteHit = true;
				RenderBackGround(x, y);
			}
				SpriteHit = true;
               
			int spritePaletteCode = 4 * spriteRenderPosition[position].Palette; //スプライトパレット
			int spriteColorNumber = 0;
            try
            {
                if (spriteRenderPosition[position].HorizontalReverse)
                    spriteColorNumber = Sprite[spriteRenderPosition[position].TileID + spriteRenderPosition[position].PatternTable,
                                               y - spriteRenderPosition[position].Y, 7 - (x - spriteRenderPosition[position].X)]; //配色番号
                else if (spriteRenderPosition[position].VerticalReverse)
                    spriteColorNumber = Sprite[spriteRenderPosition[position].TileID + spriteRenderPosition[position].PatternTable,
                                               SpriteSize - 1 - (y - spriteRenderPosition[position].Y), x - spriteRenderPosition[position].X]; //配色番号
                else if (spriteRenderPosition[position].HorizontalReverse && spriteRenderPosition[position].VerticalReverse)
                    spriteColorNumber = Sprite[spriteRenderPosition[position].TileID + spriteRenderPosition[position].PatternTable,
                                               SpriteSize - 1 - (y - spriteRenderPosition[position].Y), 7 - (x - spriteRenderPosition[position].X)]; //配色番号
                else
                    spriteColorNumber = Sprite[spriteRenderPosition[position].TileID + spriteRenderPosition[position].PatternTable,
                                           y - spriteRenderPosition[position].Y, x - spriteRenderPosition[position].X]; //配色番号
            }
            catch (Exception) {}
			if (spriteColorNumber == 0) //0x3F10, 0x3F14, 0x3F18, 0x3F1Cは背景色
				return RenderBackGround(x, y);
			return paletteColors[Palette[0x10 + spritePaletteCode + spriteColorNumber]];
		}


        /// <summary>
        /// ネームテーブルへの書き込み
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="value">Value.</param>
		public void WriteNameTable(ushort address, byte value)
		{
            int tableAddress = 0;
            switch(mirrorType)
            {
                case MirrorType.HorizontalMirror:
                    if (address >= 0x2000 && address <= 0x23BF)
                    {
                        tableAddress = ((address - 0x2000) / 32) * 64 + address % 32;
                        nameTable[tableAddress] = value;
                        nameTable[tableAddress + 32] = value;
                    }
                    else if (address >= 0x2800 && address <= 0x2BBF)
                    {
                        tableAddress = ((address - 0x2800) / 32) * 64 + address % 32 + 64 * 30;
                        nameTable[tableAddress] = value;
                        nameTable[tableAddress + 32] = value;
                    }
                    break;
                case MirrorType.VerticalMirror:
                    if (address >= 0x2000 && address <= 0x23BF)
                    {
                        tableAddress = ((address - 0x2000) / 32) * 64 + address % 32;
                        nameTable[tableAddress] = value;
                        nameTable[tableAddress + 64 * 30] = value;
                    }
                    else if (address >= 0x2400 && address <= 0x27BF)
                    {
                        tableAddress = ((address - 0x2400) / 32) * 64 + address % 32 + 32;
                        nameTable[tableAddress] = value;
                        nameTable[tableAddress + 64 * 30] = value;
                    }
                    break;
            }
		}

        public void TestShowNameTable()
        {
            Debug.WriteLine("--------------------------------------------------------------");
            for (int i = 0; i < nameTable.Length;i++)
            {
                Debug.Write(nameTable[i]);
                if (0 == ((i + 1) % 64))
                    Debug.WriteLine("");
            }
            Debug.WriteLine("--------------------------------------------------------------");
        }

        /// <summary>
        /// 属性テーブルへの書き込み
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="value">Value.</param>
		public void WriteAttrTable(ushort address, byte value)
		{
            int tableAddress = 0;
            switch(mirrorType)
            {
                case MirrorType.HorizontalMirror:
                    if (address >= 0x23C0 && address <= 0x23FF)
                    {
                        tableAddress = ((address - 0x23C0) / 8) * 16 + address % 8;
                        tableAddress = 4 * (tableAddress % 16) + 256 * (tableAddress / 16);
                        AttrTableTypeToNameTableType(tableAddress, value);
                        AttrTableTypeToNameTableType(tableAddress + 32, value);
                    }
                    else if(address >= 0x2BC0 && address <= 0x2BFF)
                    {
                        tableAddress = ((address - 0x2BC0) / 8) * 16 + address % 8 + 8 * 16;
                        tableAddress = 4 * (tableAddress % 16) + 256 * (tableAddress / 16) - 128;
                        AttrTableTypeToNameTableType(tableAddress, value);
                        AttrTableTypeToNameTableType(tableAddress + 32, value);
                    }
                    break;
                case MirrorType.VerticalMirror:
                    if (address >= 0x23C0 && address <= 0x23FF)
                    {
                        tableAddress = ((address - 0x23C0) / 8) * 16 + address % 8;
                        tableAddress = 4 * (tableAddress % 16) + 256 * (tableAddress / 16);
                        AttrTableTypeToNameTableType(tableAddress, value);
                        AttrTableTypeToNameTableType(tableAddress + 1920 - 128, value);
                    }
                    else if (address >= 0x27C0 && address <= 0x27FF)
                    {
                        tableAddress = ((address - 0x27C0) / 8) * 16 + address % 8 + 8;
                        tableAddress = 4 * (tableAddress % 16) + 256 * (tableAddress / 16);
                        AttrTableTypeToNameTableType(tableAddress, value);
                        AttrTableTypeToNameTableType(tableAddress + 1920 - 128, value);
                    }
                    break;
            }
		}

		void AttrTableTypeToNameTableType(int tableAddress, byte value)
		{
            bool bottom = tableAddress + 128 >= 3840 && tableAddress + 128 < 3904; 
			int paletteNumber = GetPalette(value, 0);
            attrTable[tableAddress] = (byte)paletteNumber;
            attrTable[tableAddress + 1] = (byte)paletteNumber;
            attrTable[tableAddress + 64] = (byte)paletteNumber;
            attrTable[tableAddress + 65] = (byte)paletteNumber;
            paletteNumber = GetPalette(value, 1);
            attrTable[tableAddress + 2] = (byte)paletteNumber;
            attrTable[tableAddress + 3] = (byte)paletteNumber;
            attrTable[tableAddress + 66] = (byte)paletteNumber;
            attrTable[tableAddress + 67] = (byte)paletteNumber;
			if (!bottom)
			{
				paletteNumber = GetPalette(value, 2);
                attrTable[tableAddress + 128] = (byte)paletteNumber;
                attrTable[tableAddress + 129] = (byte)paletteNumber;
                attrTable[tableAddress + 192] = (byte)paletteNumber;
                attrTable[tableAddress + 193] = (byte)paletteNumber;
                paletteNumber = GetPalette(value, 3);
                attrTable[tableAddress + 130] = (byte)paletteNumber;
                attrTable[tableAddress + 131] = (byte)paletteNumber;
                attrTable[tableAddress + 194] = (byte)paletteNumber;
                attrTable[tableAddress + 195] = (byte)paletteNumber;
			}
            
		}
              
        
        /// <summary>
        /// oamメモリへの書き込み
        /// </summary>
        /// <param name="offset">Offset.</param>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="tile">Tile.</param>
        /// <param name="attr">Attr.</param>
        public void WriteOamTable(int offset, int x, int y, int tile, int attr)
		{
			if (spritePosition[offset] != null)
			{
				foreach (int position in spritePosition[offset])
				{
					spriteRenderPosition.Remove(position);
				}
				spritePosition[offset] = null;
			}
            
			int tileID = tile;
			int patternTableNumber = SpritePatternTable;
			if (SpriteSize > 8)
			{
				patternTableNumber = Nes.FetchBit(tileID, 0) * 256;
				tileID = 2 * (tileID >> 1);
			}
			bool verticalReverse = Nes.FetchBit(attr, 7) == 1;
			bool horizontalReverse = Nes.FetchBit(attr, 6) == 1;
			bool priority = Nes.FetchBit(attr, 6) == 0;
			int palette = Nes.FetchBit(attr, 1) * 2 + Nes.FetchBit(attr, 0);
			oamTable[offset] = new Oam
			{
				X = x,
				Y = y,
				TileID = tileID,
				PatternTable = patternTableNumber,
				HorizontalReverse = horizontalReverse,
				VerticalReverse = verticalReverse,
				Priority = priority,
				Palette = palette
			};

			spritePosition[offset] = new List<int>();
			for (int i = x + y * 256, k = 0; i < SpriteSize * 256 + y * 256 + x;i+=256, k++)
			{
				for (int j = 0; j < 8;j++)
				{
					if (!spriteRenderPosition.ContainsKey(i + j))
						spriteRenderPosition.Add(i + j, oamTable[offset]);
					else
						spriteRenderPosition[i + j] = oamTable[offset];

					spritePosition[offset].Add(i + j);
                 
					if (k >= 8)
						spriteRenderPosition[i + j].TileID++;
				}
			}
		}

		/// <summary>
        /// スプライトを読み込み保存
        /// </summary>
        public void LoadSprite()
        {
            int count = 0;
            for (int i = 0; i < Sprite.GetLength(0); i++)
            {
                for (int j = i * 16; j < i * 16 + 8; j++)
                {
					string highOrder = BinaryNumberConversion(patternTable[j + 8]);
					string lowOrder = BinaryNumberConversion(patternTable[j]);
                    for (int l = 0; l < 8; l++)
                    {
                        Sprite[i, count, l] = (byte)(int.Parse(highOrder[l].ToString()) * 2 + int.Parse(lowOrder[l].ToString()));
                    }
                    count++;
                }
                count = 0;
            }
        }


        /// <summary>
        /// パターンテーブルへの書き込み
        /// </summary>
        /// <param name="offset">Offset.</param>
        /// <param name="value">Value.</param>
		public void WritePatternTable(int offset, byte value)
		{
			patternTable[offset] = value;
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

		//パレットカラー
        byte[][] paletteColors = new byte[][]
        {
            new byte[]{ 0x75, 0x75, 0x75 }, new byte[]{ 0x27, 0x1B, 0x8F },
            new byte[]{ 0x00, 0x00, 0xAB }, new byte[]{ 0x47, 0x00, 0x9F },
            new byte[]{ 0x8F, 0x00, 0x77 }, new byte[]{ 0xAB, 0x00, 0x13 },
            new byte[]{ 0xA7, 0x00, 0x00 }, new byte[]{ 0x7F, 0x0B, 0x00 },
            new byte[]{ 0x43, 0x2F, 0x00 }, new byte[]{ 0x00, 0x47, 0x00 },
            new byte[]{ 0x00, 0x51, 0x00 }, new byte[]{ 0x00, 0x3F, 0x17 },
            new byte[]{ 0x1B, 0x3F, 0x5F }, new byte[]{ 0x00, 0x00, 0x00 },
            new byte[]{ 0x05, 0x05, 0x05 }, new byte[]{ 0x05, 0x05, 0x05 },

            new byte[]{ 0xBC, 0xBC, 0xBC }, new byte[]{ 0x00, 0x73, 0xEF },
            new byte[]{ 0x23, 0x3B, 0xEF }, new byte[]{ 0x83, 0x00, 0xF3 },
            new byte[]{ 0xBF, 0x00, 0xBF }, new byte[]{ 0xE7, 0x00, 0x5B },
            new byte[]{ 0xDB, 0x2B, 0x00 }, new byte[]{ 0xCB, 0x4F, 0x0F },
            new byte[]{ 0x8B, 0x73, 0x00 }, new byte[]{ 0x00, 0x97, 0x00 },
            new byte[]{ 0x00, 0xAB, 0x00 }, new byte[]{ 0x00, 0x93, 0x3B },
            new byte[]{ 0x00, 0x83, 0x8B }, new byte[]{ 0x11, 0x11, 0x11 },
            new byte[]{ 0x09, 0x09, 0x09 }, new byte[]{ 0x09, 0x09, 0x09 },

            new byte[]{ 0xFF, 0xFF, 0xFF }, new byte[]{ 0x3F, 0xBF, 0xFF },
            new byte[]{ 0x5F, 0x97, 0xFF }, new byte[]{ 0xA7, 0x8B, 0xFD },
            new byte[]{ 0xF7, 0x7B, 0xFF }, new byte[]{ 0xFF, 0x77, 0xB7 },
            new byte[]{ 0xFF, 0x77, 0x63 }, new byte[]{ 0xFF, 0x9B, 0x3B },
            new byte[]{ 0xF3, 0xBF, 0x3F }, new byte[]{ 0x83, 0xD3, 0x13 },
            new byte[]{ 0x4F, 0xDF, 0x4B }, new byte[]{ 0x58, 0xF8, 0x98 },
            new byte[]{ 0x00, 0xEB, 0xDB }, new byte[]{ 0x66, 0x66, 0x66 },
            new byte[]{ 0x0D, 0x0D, 0x0D }, new byte[]{ 0x0D, 0x0D, 0x0D },

            new byte[]{ 0xFF, 0xFF, 0xFF }, new byte[]{ 0xAB, 0xE7, 0xFF },
            new byte[]{ 0xC7, 0xD7, 0xFF }, new byte[]{ 0xD7, 0xCB, 0xFF },
            new byte[]{ 0xFF, 0xC7, 0xFF }, new byte[]{ 0xFF, 0xC7, 0xDB },
            new byte[]{ 0xFF, 0xBF, 0xB3 }, new byte[]{ 0xFF, 0xDB, 0xAB },
            new byte[]{ 0xFF, 0xE7, 0xA3 }, new byte[]{ 0xE3, 0xFF, 0xA3 },
            new byte[]{ 0xAB, 0xF3, 0xBF }, new byte[]{ 0xB3, 0xFF, 0xCF },
            new byte[]{ 0x9F, 0xFF, 0xF3 }, new byte[]{ 0xDD, 0xDD, 0xDD },
            new byte[]{ 0x11, 0x11, 0x11 }, new byte[]{ 0x11, 0x11, 0x11 }
        };

    }

}
