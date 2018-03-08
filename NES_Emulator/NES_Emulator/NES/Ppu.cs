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

        bool nmiInterrupt; //NMI割り込みを有効化するか

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

        Nes nes;
        public Ppu(Nes nes)
        {
            ppuAddress = new byte[0x4000];
            oam = new byte[0xff, 4];
            this.nes = nes;

            sprite = new byte[nes.rom.CharacterRom.Length / 16, 8, 8];
            screen = new byte[61440][];

            spriteSize = 8;

            nmiInterrupt = false;

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
                    spriteSize = (value << 2) >> 7;
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
                case 0x2004:
                    break;
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
                    _totalPpuCycle -= 341;
                }
                else if (_totalPpuCycle >= 341 && RenderLine >= 240)
                {
                    RenderLine++;
                }
            }
        }


        /// <summary>
        /// 次に描画するラインを保存する
        /// 240ライン描画できたら画像化する
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
                    nes.gameScreen.RenderScreen(screen);
                    _renderLine = 0;
                }
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
        }


        /// <summary>
        /// BGスクリーンを1ライン描画
        /// </summary>
        /// <param name="x">開始X座標</param>
        /// <param name="y">開始Y座標</param>
        void BgRenderScreen(int x, int y)
        {
            int nameTableNumber = 0x2000 + (RenderLine / 8) * 32 + x / 8; //読み込むネームテーブルのアドレス
            nameTableNumber = GetNameTableNumber(nameTableNumber);
            int attrTableNumber = 0x23C0 + (RenderLine / 16) * 32 + x / 16; //読み込む属性テーブルのアドレス
            attrTableNumber = GetAttrTableNumber(attrTableNumber);
            int attrTablePaletteNumber = 0; //パレット内の読み込む色の番号
            int column = 0; //現在の行
            int spriteLine = RenderLine % 8; //今読み込んでるラインのスプライトの列
            int unitRenderLine = RenderLine - (32 * (RenderLine / 32));

            for (int i = RenderLine * 256;i < (RenderLine + 1) * 256;i++)
            {
                int unitColumn = column - (32 * (column / 32));

                if ((column % 8) == 0 && column != 0) nameTableNumber++;
                nameTableNumber = GetNameTableNumber(nameTableNumber);
                if ((i % 32) == 0 && i != RenderLine * 256) attrTableNumber++;
                attrTableNumber = GetAttrTableNumber(attrTableNumber);

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

                screen[i] = Nes.paletteColors[ppuAddress[0x3F00 //パレットに保存してる値がpaletteColorsの添字
                                                         + 4 * GetPalette(ppuAddress[attrTableNumber], attrTablePaletteNumber) 
                                                         + sprite[ppuAddress[nameTableNumber], spriteLine, column - (8 * (column / 8))]]];
                column++;
            }
            RenderLine++;
        }

        int GetNameTableNumber(int nameTableNumber)
        {
            if (0x2780 > nameTableNumber && 0x23C0 <= nameTableNumber)
            {
                nameTableNumber = 0x2400 + nameTableNumber - 0x23C0;
            }
            else if(0x2B40 > nameTableNumber && 0x2780 <= nameTableNumber)
            {
                nameTableNumber = 0x2800 + nameTableNumber - 0x2780;
            }
            else if(0x2F00 > nameTableNumber && 0x2B40 <= nameTableNumber)
            {
                nameTableNumber = 0x2C00 + nameTableNumber - 0x2B40;
            }
            return nameTableNumber;
        }

        int GetAttrTableNumber(int attrTableNumber)
        {
            if (0x2440 > attrTableNumber && 0x2400 <= attrTableNumber)
            {
                attrTableNumber = 0x27C0 + attrTableNumber - 0x2400;
            }
            else if(0x2480 > attrTableNumber && 0x2440 <= attrTableNumber)
            {
                attrTableNumber = 0x2BC0 + attrTableNumber - 0x2440;
            }
            else if(0x24C0 > attrTableNumber && 0x2480 <= attrTableNumber)
            {
                attrTableNumber = 0x2FC0 + attrTableNumber - 0x2480;
            }
            return attrTableNumber;
        }

        /// <summary>
        /// スプライトを描画
        /// </summary>
        void OamRenderScreen()
        {
            for (int i = 0;i < 0xff;i++)
            {
                int x = oam[i, 3], y = oam[i, 0] + 1;
                int spriteIndex;
                bool verticalReverse = (oam[i, 2] >> 7) == 1;
                bool horizontalReverse = ((oam[i, 2] << 1) >> 7) == 1;
                bool front = ((oam[i, 2] << 2) >> 7) == 0;
                int paletteNumber = ((oam[i, 2] << 6) >> 7) * 2 + ((oam[i, 2] << 7) >> 7);
                if(front)
                {
                    
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
                    string highOrder = BinaryNumberConversion(nes.rom.CharacterRom[j + 8]);
                    string lowOrder = BinaryNumberConversion(nes.rom.CharacterRom[j]);
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
    }
}
