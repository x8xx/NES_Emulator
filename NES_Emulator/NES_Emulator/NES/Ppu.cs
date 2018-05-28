using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Xamarin.Forms;

namespace NES_Emulator.NES
{
	public class Ppu : Unit
    {
        /*
         * PPUメモリマップ
         * アドレス        サイズ
         * 0x0000～0x0FFF 0x1000 パターンテーブル0
         * 0x1000～0x1FFF 0x1000 パターンテーブル1
         * 0x2000～0x23BF 0x03C0 ネームテーブル0
         * 0x23C0～0x23FF 0x0040 属性テーブル0
         * 0x2400～0x27BF 0x03C0 ネームテーブル1
         * 0x27C0～0x27FF 0x0040 属性テーブル1
         * 0x2800～0x2BBF 0x03C0 ネームテーブル2
         * 0x2BC0～0x2BFF 0x0040 属性テーブル2
         * 0x2C00～0x2FBF 0x03C0 ネームテーブル3
         * 0x2FC0～0x2FFF 0x0040 属性テーブル3
         * 0x3000～0x3EFF        0x2000-0x2EFFのミラー
         * 0x3F00～0x3F0F 0x0010 バックグラウンドパレット
         * 0x3F10～0x3F1F 0x0010 スプライトパレット
         * => 0x3F00 背景色
         *    0x3F01〜0x3F03 バックグラウンドパレット0
         *    0x3F04 空き領域
         *    0x3F05〜0x3F07 バックグラウンドパレット1
         *    0x3F08 空き領域
         *    0x3F09〜0x3F0B バックグラウンドパレット2
         *    0x3F0C 空き領域
         *    0x3F0D〜0x3F0F バックグラウンドパレット3
         *    0x3F10 0x3F00のミラー
         *    0x3F11〜0x3F13 スプライトパレット0
         *    0x3F14 0x3F04のミラー
         *    0x3F15〜0x3F17 スプライトパレット1
         *    0x3F18 0x3F08のミラー
         *    0x3F19〜0x3F1B スプライトパレット2
         *    0x3F1C 0x3F0Cのミラー
         *    0x3F1D〜0x3F1F スプライトパレット3
         * 0x3F20～0x3FFF        0x3F00-0x3F1Fのミラー
         */
        byte[] ppuAddress;

		byte[] patternTable; //パターンテーブル
		byte[] nameTable; //ネームテーブル
		byte[] attrTable; //属性テーブル
		byte[] palette; //パレット

        /* 
         * OAM (Object Attribute Memory)
         *  スプライト管理メモリ
         *  Size : 256byte
         *  1スプライト4byteの構造体 64個のスプライトを保持
         *  スプライト構造
         *      Sprite.y
         *          y座標 (実際の座標は+1)
         *      Sprite.tile
         *          スプライトSizeが8x8の場合タイルIDそのもの
         *          スプライトSizeが8x16の場合は以下のようになる
         *          bit 76543210
         *              TTTTTTTP
         *          P : パターンテーブル選択 0:0x0000, 1:0x1000
         *          T : スプライト上半分のタイルIDを2*Tとし, 下半分を2*T+1とする
         *      Sprite.attr
         *          bit 76543210
         *              VHP...CC
         *          V : 垂直反転
         *          H : 水平反転
         *          P : 優先度(0:前面, 1:背面)
         *          C : パレット
         *      Sprite.x
         *          x座標
         */
        Oam[] oam;
		Oam[] oamTable;
		class Oam
        {
            public byte y;
            public byte x;
            public byte tile;
            public byte attr;
			public int currentLine;
        }

        byte[,,] sprite; //Sprite保存用
        byte[][] screen; //スクリーン

        int spriteSize; //スプライトのサイズ保存用 8 x spriteSizeとなる

        int bgPatternTable; //BGのパターンテーブル
        int spritePatternTable; //スプライトのパターンテーブル

        bool nmiInterrupt; //NMI割り込みを有効化するか
        bool spriteHit; //0爆弾
        bool verticalMirror; //true : 垂直ミラー, false : 水平ミラー
        bool isSpriteVisible; //スプライトを表示するかしないか
        bool isBgVisible; //BGを表示するかしないか

        byte oamAddr;  //0x2003 OAMADDR W スプライトメモリデータ 書き込むスプライト領域のアドレス
		byte[] tempOamData; //0, y 1, tile 2, attr 3, x
        ushort ppuAddr; //0x2006 PPUADDR W PPUメモリアドレス 書き込むメモリ領域のアドレス
        byte ppuData; //0x2007 PPUDATA RW PPUメモリデータ PPUメモリ領域のデータ
        int oamDataWriteCount; //0x2004のwrite回数を記録
        int ppuScrollWriteCount; //0x2005のwrite回数を記録
        int scrollOffsetX; //X方向へのスクロール
        int scrollOffsetY; //Y方向へのスクロール
        int ppuAddrWriteCount; //0x2006のWrite回数を記録
        byte ppuAddressInc; //0x2006のインクリメントする大きさ

        int _totalPpuCycle; //PPUの合計サイクル数
        int _renderLine; //次に描画するlineを保持
        bool vBlank; //VBlank中か

		const int headerSize = 54;
        byte[] gameScreen;
        //public ImageSource GameScreen { get { return ImageSource.FromStream(() => new MemoryStream(gameScreen)); } }
		public ImageSource GameScreen { get { return renderer.GameScreen; } }
		Renderer renderer;

        public bool notificationScreenUpdate { get; set; } //1フレーム更新通知
        
		public Ppu(Nes nes, int characterRomSize) : base(nes)
        {
			unitName = GetType().Name;
            ppuAddress = new byte[0x4000];
            oam = new Oam[0x40];
            this.nes = nes;

			nameTable = new byte[0xF00];
			attrTable = new byte[0x100];
			oamTable = new Oam[61440];
            
            screen = new byte[61440][];

            spriteSize = 8;
            bgPatternTable = 0;
            spritePatternTable = 0;

            nmiInterrupt = false;
            verticalMirror = nes.verticalMirror;
            isSpriteVisible = true;
            isBgVisible = true;

            oamDataWriteCount = 0;
			tempOamData = new byte[4];
            scrollOffsetX = 0;
            scrollOffsetY = 0;
            ppuAddrWriteCount = 0;
            ppuAddressInc = 0x01;

            TotalPpuCycle = 0;
            RenderLine = 0;
            vBlank = false;

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

			renderer = new Renderer();
			renderer.Sprite = new byte[nes.CharacterRimSize / 16, 8, 8];
        }

		public void RenderScreen(/*byte[][] table, int renderLine*/)
        {
			/*for (int i = headerSize + renderLine * 256 * 4, j = renderLine * 256; j < (renderLine + 1) * 256; i += 4, j++)
            {
				gameScreen[i] = table[j][2];
				gameScreen[i + 1] = table[j][1];
                gameScreen[i + 2] = table[j][0];
				gameScreen[i + 3] = 255;
            }*/
			for (int x = 0; x < 256; x++)
				renderer.RenderScreen(x, RenderLine - 1);
        }


		public override void Execute()
        {
			if (_totalPpuCycle >= 341 && RenderLine < 240)
            {
                while (_totalPpuCycle >= 341)
                {
                    RenderLine++;
                    _totalPpuCycle -= 341;
					//RenderScreen(screen, RenderLine - 1);
					//RenderScreen(scrollOffsetX, scrollOffsetY);
					//Renderer(scrollOffsetX, scrollOffsetY);
					RenderScreen();
                }
            }
            else if (_totalPpuCycle >= 341 && RenderLine >= 240)
            {
                RenderLine++;
            }
        }

        /// <summary>
        /// PPUレジスタの書き込み
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="value">Value.</param>
        public void WritePpuRegister(ushort address, byte value)
        {
            switch (address)
            {
                /*
                 * 0x2000 PPUCTRL W コントロールレジスタ1 割り込みなどPPUの設定
                 * bit 76543210
                 *     VPHBSINN
                 * 
                 * V : VBLANK 開始時に NMI 割り込みを発生 (0:off, 1:on)
                 * P : P: PPU マスター/スレーブ
                 * H : スプライトサイズ (0:8*8, 1:8*16)
                 * B : BG パターンテーブル (0:$0000, 1:$1000)
                 * S : スプライトパターンテーブル (0:$0000, 1:$1000)
                 * I : PPU アドレスインクリメント (0:+1, 1:+32) - VRAM 上で +1 は横方向、+32 は縦方向
                 * N : ネームテーブル (0:$2000, 1:$2400, 2:$2800, 3:$2C00)
                 */
                case 0x2000:
                    nmiInterrupt = Nes.FetchBit(value, 7) == 1;
                    ppuAddressInc = (byte)(Nes.FetchBit(value, 2) * 31 + 1);

					renderer.SpriteSize = Nes.FetchBit(value, 5) * 8 + 8;
					renderer.BgPatternTable = 256 * Nes.FetchBit(value, 4);
					renderer.SpritePatternTable = 256 * Nes.FetchBit(value, 3);
                    break;
                /* 0x2001 PPUMASK W コントロールレジスタ2 背景イネーブルなどPPUの設定
                * bit 76543210
                *     BGRsbMmG
                * 
                * B : 色強調(青)
                * G : 色強調(緑)
                * R : 色強調(赤)
                * s : スプライト描画(0:off, 1:on)
                * b : BG 描画 (0:off, 1:on)
                * M : 画面左端 8px でスプライトクリッピング (0:有効, 1:無効)
                * m : 画面左端 8px で BG クリッピング (0:有効, 1:無効)
                * G : 0:カラー, 1:モノクロ
                */
                case 0x2001:               
					renderer.IsSpriteVisible = Nes.FetchBit(value, 4) != 1;
					renderer.IsBGVisible = Nes.FetchBit(value, 3) != 1;
                    break;
                //読み書きするOAMアドレスを指定
                case 0x2003:
                    oamAddr = value;
                    break;
                //0x2003で指定したOAMアドレスにy, tile, attr, xの順に書き込む
                case 0x2004:
                    oamDataWriteCount++;
					tempOamData[oamDataWriteCount - 1] = value;
					if (oamDataWriteCount == 4)
					{
						renderer.WriteOamTable(oamAddr, tempOamData[3], tempOamData[0], tempOamData[1], tempOamData[2]);
						oamDataWriteCount = 0;
					}
                    break;
                //1回目の書き込みでx, 2回目の書き込みでyのスクロールオフセットを指定
                case 0x2005:
                    switch (ppuScrollWriteCount)
                    {
                        case 0:
							//scrollOffsetX = value;
							renderer.ScrollOffsetX = value;
                            ppuScrollWriteCount++;
                            break;
                        case 1:
							//scrollOffsetY = value;
							renderer.ScrollOffsetY = value;
                            ppuScrollWriteCount = 0;
                            break;
                    }
                    break;
                //1回目の書き込みで上位バイト, 2回目の書き込みで下位バイトを設定
                case 0x2006:
                    switch (ppuAddrWriteCount)
                    {
                        case 0:
                            ppuAddr = (ushort)(value * 0x100);
                            ppuAddrWriteCount++;
                            break;
                        case 1:
                            ppuAddr += value;
                            ppuAddrWriteCount = 0;
                            break;
                    }
                    break;
                //ppuAddrのアドレスに書き込み
                case 0x2007:
                    ppuData = value;
                    WriteMemory(ppuAddr, value);
                    ppuAddr += ppuAddressInc;
                    break;
                //OMAへDMA転送
                case 0x4014:
                    for (int i = 0, j = value * 0x100; i < 0x40; i++, j += 4)
					{
						/*byte x = nes.ReadCpuMemory((ushort)(j + 3));
						byte y = nes.ReadCpuMemory((ushort)j);
						byte tile = nes.ReadCpuMemory((ushort)(j + 1));
						byte attr = nes.ReadCpuMemory((ushort)(j + 2));
						oamTable[x + 256 * y] = new Oam() { y = y, tile = tile, attr = attr, x = x, currentLine = 0 };*/
						Debug.WriteLine(1);
						renderer.WriteOamTable(i, nes.ReadCpuMemory((ushort)(j + 3)), nes.ReadCpuMemory((ushort)j), nes.ReadCpuMemory((ushort)(j + 1)), nes.ReadCpuMemory((ushort)(j + 2)));
						Debug.WriteLine(2);
					}
					break;
            }
        }


        public byte ReadPpuRegister(ushort address)
        {
            switch (address)
            {
                /*
                 * 0x2002 PPUSTATUS R PPUステータス PPUステータス
                 * bit 76543210
                 *     VSO.....
                 * 
                 * V : VBLANKフラグ
                 * S : Sprite 0 hit
                 * O : スプライトオーバーフラグ
                 * 
                 * 読み取り後VBLANKフラグが下りる
                 * 0x2005, 0x2006の書き込み状態がリセット
                 */
                case 0x2002:
                    int vblankFlag = (vBlank) ? 1 : 0;
                    int sprite0Hit = (spriteHit) ? 1 : 0;
                    vBlank = false;
                    oamDataWriteCount = 0;
                    ppuAddrWriteCount = 0;
                    return (byte)(vblankFlag * 0x80 + sprite0Hit * 0x40);
                case 0x2007:
                    break;
            }
            return 0x00;
        }


        /// <summary>
        /// PPUの合計サイクル数を保持
        /// サイクル数が341以上になったとき1ライン描画する
        /// </summary>
        /// <value>The total ppu cycle.</value>
        public int TotalPpuCycle
        {
            get { return _totalPpuCycle; }
            set
            {
                _totalPpuCycle += value;
            }
        }


        /// <summary>
        /// 次に描画するラインを保存する
        /// 262ライン以上でラインを0にする
        /// </summary>
        /// <value>The render line.</value>
        int RenderLine
        {
            get { return _renderLine; }
            set
            {
                _renderLine = value;
                if (_renderLine > 261)
                {
                    notificationScreenUpdate = true;
					nes.DrawingFrame = false;
                    _renderLine = 0;
                }

                if (RenderLine == 240 && nmiInterrupt)
                    nes.cpu.Nmi();
                else if (RenderLine == 240 && !nmiInterrupt)
                    vBlank = true;
                else if (RenderLine == 0)
                    vBlank = false;
            }
        }


        /// <summary>
        /// PPUメモリに書き込み
        /// </summary>
        /// <param name="address">アドレス</param>
        /// <param name="value">値</param>
        public void WriteMemory(ushort address, byte value)
        {
			if (address >= 0 && address <= 0x1FFF)
				renderer.WritePatternTable(address, value);
			else if ((address >= 0x2000 && address <= 0x23BF) || (address >= 0x2400 && address <= 0x27BF)
				|| (address >= 0x2800 && address <= 0x2BBF) || (address >= 0x2C00 && address <= 0x2FBF))
				renderer.WriteNameTable(address, value);
			else if ((address >= 0x23C0 && address <= 0x23FF) || (address >= 0x27C0 && address <= 0x27FF)
					 || (address >= 0x2BC0 && address <= 0x2BFF) || (address >= 0x2FC0 && address <= 0x2FFF))
				renderer.WriteAttrTable(address, value);
			else if (address >= 0x3F00 && address <= 0x3F1F)
			{
				address -= 0x3F00;
				renderer.Palette[address] = value;
				switch (address) //パレットミラー
                {
                    case 0x3F00:
                    case 0x3F04:
                    case 0x3F08:
                    case 0x3F0C:
						renderer.Palette[address + 0x10] = value;
                        break;
                    case 0x3F10:
                    case 0x3F14:
                    case 0x3F18:
                    case 0x3F1C:
						renderer.Palette[address - 0x10] = value;
                        break;
                }
			}
        }
      

        /// <summary>
        /// スプライトを読み込み保存
        /// </summary>
        public void LoadSprite()
        {
			renderer.LoadSprite();
        }

    }
}
