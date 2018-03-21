using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NES_Emulator.NES
{
    public class Ppu
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
         * 
         * oam[i, 0] = Sprite.y
         * oam[i, 1] = Sprite.tile
         * oam[i, 2] = Sprite.attr
         * oam[i, 3] = Sprite.x
         */
        byte[,] oam;

        byte[,,] sprite; //Sprite保存用
        byte[][] screen; //スクリーン

        int spriteSize; //スプライトのサイズ保存用 8 x spriteSizeとなる

        int bgPatternTable; //BGのパターンテーブル
        int spritePatternTable; //スプライトのパターンテーブル

        bool nmiInterrupt; //NMI割り込みを有効化するか
        bool verticalMirror; //true : 垂直ミラー, false : 水平ミラー
        bool isSpriteVisible; //スプライトを表示するかしないか
        bool isBgVisible; //BGを表示するかしないか

        byte oamAddr;  //0x2003 OAMADDR W スプライトメモリデータ 書き込むスプライト領域のアドレス
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

        public bool notificationScreenUpdate { get; set; } //1フレーム更新通知

        Nes nes;
        public Ppu(Nes nes)
        {
            ppuAddress = new byte[0x4000];
            oam = new byte[0xff, 4];
            this.nes = nes;

            sprite = new byte[nes.CharacterRimSize / 16, 8, 8];
            screen = new byte[61440][];

            spriteSize = 8;
            bgPatternTable = 0;
            spritePatternTable = 0;

            nmiInterrupt = false;
            verticalMirror = nes.verticalMirror;
            isSpriteVisible = true;
            isBgVisible = true;

            oamDataWriteCount = 0;
            scrollOffsetX = 0;
            scrollOffsetY = 0;
            ppuAddrWriteCount = 0;
            ppuAddressInc = 0x01;

            TotalPpuCycle = 0;
            RenderLine = 0;
        }


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
                case 2000:
                    nmiInterrupt = (value >> 7) == 1;
                    spriteSize = (value << 2) >> 7 * 8 + 8;
                    bgPatternTable = 512 * (value << 3) >> 7;
                    spritePatternTable = 512 * (value << 4) >> 7;
                    ppuAddressInc = (byte)(((value << 5) >> 7) * 31 + 1);
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
                    WriteMemory(address, value);
                    break;
                //読み書きするOAMアドレスを指定
                case 0x2003:
                    oamAddr = value;
                    break;
                //0x2003で指定したOAMアドレスにy, tile, attr, xの順に書き込む
                case 0x2004:
                    oam[oamAddr, oamDataWriteCount] = value;
                    oamDataWriteCount++;
                    if (oamDataWriteCount > 3) oamDataWriteCount = 0; 
                    break;
                //1回目の書き込みでx, 2回目の書き込みでyのスクロールオフセットを指定
                case 0x2005:
                    switch(ppuScrollWriteCount)
                    {
                        case 0:
                            scrollOffsetX = value;
                            ppuScrollWriteCount++;
                            break;
                        case 1:
                            scrollOffsetY = value;
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
                    for (int i = 0, j = value * 0x100; i < 0xff; i++, j += 4)
                    {
                        oam[i, 0] = nes.ReadCpuMemory((ushort)j);
                        oam[i, 1] = nes.ReadCpuMemory((ushort)(j + 1));
                        oam[i, 2] = nes.ReadCpuMemory((ushort)(j + 2));
                        oam[i, 3] = nes.ReadCpuMemory((ushort)(j + 3));
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
                    int vblankFlag = (RenderLine > 239 && RenderLine < 262) ? 1 : 0;
                    oamDataWriteCount = 0;
                    ppuAddrWriteCount = 0;
                    return (byte)(vblankFlag * 0x80);
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
                _totalPpuCycle = value;
                if (_totalPpuCycle >= 341 && RenderLine < 240)
                {
                    BgRenderScreen(scrollOffsetX, scrollOffsetY);
                    OamRenderScreen();
                    _totalPpuCycle -= 341;
                    nes.gameScreen.RenderScreen(screen, RenderLine - 1);
                }
                else if (_totalPpuCycle >= 341 && RenderLine >= 240)
                {
                    RenderLine++;
                }
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
                    _renderLine = 0;
                }

                if (RenderLine == 240 && nmiInterrupt)
                    nes.cpu.Nmi();
            }
        }


        /// <summary>
        /// PPUメモリに書き込み
        /// </summary>
        /// <param name="address">アドレス</param>
        /// <param name="value">値</param>
        public void WriteMemory(ushort address, byte value)
        {
            ppuAddress[address] = value;
            if (verticalMirror) //垂直ミラー
            {
                if (address >= 0x2000 && address <= 0x27FF)
                {
                    ppuAddress[address + 0x800] = value;
                }
                else if (address >= 0x2800 && address <= 0x2FFF)
                {
                    ppuAddress[address - 0x800] = value;
                }
            }
            else //水平ミラー
            {
                if ((address >= 0x2000 && address <= 0x23FF) || (address >= 0x2800 && address <= 0x2BFF))
                {
                    ppuAddress[address + 0x400] = value;
                }
                else if ((address >= 0x2400 && address <= 0x27FF) || (address >= 0x2C00 && address <= 0x2FFF))
                {
                    ppuAddress[address - 0x400] = value;
                }
            }

            switch (address) //パレットミラー
            {
                case 0x3F00:
                case 0x3F04:
                case 0x3F08:
                case 0x3F0C:
                    ppuAddress[address + 0x10] = value;
                    break;
            }
        }


        /// <summary>
        /// BGスクリーンを1ライン描画
        /// </summary>
        /// <param name="x">開始X座標</param>
        /// <param name="y">開始Y座標</param>
        void BgRenderScreen(int x, int y)
        {
            int renderLine = RenderLine + y; //読み込む列
            int initialNameTable = 0x2000; //読み込むネームテーブル
            int initialAttrTable = 0x23C0; //読み込む属性テーブル
            if (renderLine >= 240 && x < 256)
            {
                initialNameTable = 0x2800; //ネームテーブル2
                initialAttrTable = 0x2BC0; //属性テーブル2
                renderLine -= 240;
            }
            else if (renderLine < 240 && x >= 256)
            {
                initialNameTable = 0x2400; //ネームテーブル1
                initialAttrTable = 0x27C0; //属性テーブル1
                x -= 256;
            }
            else if (renderLine >= 240 && x >= 256)
            {
                initialNameTable = 0x2C00; //ネームテーブル3
                initialAttrTable = 0x2FC0; //属性テーブル3
                x -= 256;
                renderLine -= 240;
            }
            int nameTableNumber = initialNameTable + (renderLine / 8) * 32 + x / 8; //読み込むネームテーブルのアドレス
            int attrTableNumber = initialAttrTable + (renderLine / 16) * 32 + x / 16; //読み込む属性テーブルのアドレス

            int attrTablePaletteNumber = 0; //パレット内の読み込む色の番号
            int column = x; //現在の行

            int spriteLine = renderLine % 8; //今読み込んでるラインのスプライトの列
            int unitRenderLine = renderLine - (32 * (renderLine / 32));

            for (int i = RenderLine * 256;i < (RenderLine + 1) * 256;i++)
            {
                int unitColumn = column - (32 * (column / 32));

                //ネームテーブルの加算
                if ((column % 8) == 0 && column != 0) 
                {
                    nameTableNumber++;
                    if (nameTableNumber == (initialNameTable + 0x20))
                    {
                        nameTableNumber += 0x3E0;
                    }
                }

                //属性テーブルの加算
                if ((i % 32) == 0 && i != RenderLine * 256)
                {
                    attrTableNumber++;
                    if (attrTableNumber == (initialAttrTable + 0x10))
                    {
                        attrTableNumber += 0x3F0;
                    }
                }

                if (unitRenderLine < 16 && unitColumn < 16)
                {
                    attrTablePaletteNumber = 0;
                }
                else if (unitRenderLine < 16 && unitColumn >= 16)
                {
                    attrTablePaletteNumber = 1;
                }
                else if (unitRenderLine >= 16 && unitColumn < 16)
                {
                    attrTablePaletteNumber = 2;
                }
                else if (unitRenderLine >= 16 && unitColumn >= 16)
                {
                    attrTablePaletteNumber = 3;
                }
                screen[i] = paletteColors[ppuAddress[0x3F00 //パレットに保存してる値がpaletteColorsの添字
                                                         + 4 * GetPalette(ppuAddress[attrTableNumber], attrTablePaletteNumber) 
                                                         + sprite[bgPatternTable + ppuAddress[nameTableNumber], spriteLine, column - (8 * (column / 8))]]];
                column++;
            }
            RenderLine++;
        }


        /// <summary>
        /// スプライトを描画
        /// </summary>
        void OamRenderScreen()
        {
            for (int i = 0; i < 0xff; i++)
            {
                int x = oam[i, 3];
                int y = oam[i, 0] + 1;
                bool front = Nes.FetchBit(oam[i, 2], 5) == 0;
                if (RenderLine == y && front && y < 240 && y >= 0 && x < 240 && x >= 0 && isSpriteVisible)
                {
                    bool verticalReverse = Nes.FetchBit(oam[i, 2], 7) == 1;
                    bool horizontalReverse = Nes.FetchBit(oam[i, 2], 6) == 1;
                    int paletteNumber = Nes.FetchBit(oam[i, 2], 1) * 2 + Nes.FetchBit(oam[i, 2], 0);
                    int spriteTile = oam[0, 1];

                    if (spriteSize != 8) //8x16
                    {
                        spriteTile = 2 * (spriteTile >> 1) + Nes.FetchBit(spriteTile, 0) * 256;
                        for (int j = y * 256 + x * 8; j < y * 256 + x * 8 + 8; j++)
                        {
                            for (int k = 0; k < spriteSize; k++)
                            {
                                if (k < 8)
                                    screen[j + k * 256] = paletteColors[ppuAddress[0x3F10 + 4 * paletteNumber + sprite[spriteTile, k, j - (y * 256 + x * 8)]]];
                                if (k > 8)
                                    screen[j + k * 256] = paletteColors[ppuAddress[0x3F10 + 4 * paletteNumber + sprite[spriteTile + 1, k - k % 2, j - (y * 256 + x * 8)]]];
                            }
                        }
                    }
                    else //8x8
                    {
                        spriteTile += spritePatternTable;
                        for (int j = y * 256 + x * 8; j < y * 256 + x * 8 + 8; j++)
                        {
                            for (int k = 0; k < spriteSize; k++)
                            {
                                screen[j + k * 256] = paletteColors[ppuAddress[0x3F10 + 4 * paletteNumber + sprite[spriteTile, k, j - (y * 256 + x * 8)]]];
                            }
                        }
                    }
                }
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
        /// スプライトを読み込み保存
        /// </summary>
        public void LoadSprite()
        {
            int count = 0;
            for (int i = 0;i < sprite.GetLength(0);i++)
            {
                for (int j = i * 16; j < i * 16 + 8; j++)
                {
                    string highOrder = BinaryNumberConversion(ppuAddress[j + 8]);
                    string lowOrder = BinaryNumberConversion(ppuAddress[j]);
                    for (int l = 0;l < 8; l++)
                    {
                        sprite[i, count, l] = (byte)(int.Parse(highOrder[l].ToString()) * 2 + int.Parse(lowOrder[l].ToString()));
                    }
                    count++;
                }
                count = 0;
            }
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
                for (int i = 0;i < 8 - length;i++)
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
