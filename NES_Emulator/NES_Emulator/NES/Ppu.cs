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
         *          y座標
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

        byte ppuStatus; //0x2002 PPUSTATUS R PPUステータス PPUステータス
        byte oamAddr;  //0x2003 OAMADDR W スプライトメモリデータ 書き込むスプライト領域のアドレス
        byte oamData; //0x2004 OAMDATA RW デシマルモード スプライト領域のデータ
        byte ppuScroll; //0x2005 PPUSCROLL W 背景スクロールオフセット 背景スクロール値
        ushort ppuAddr; //0x2006 PPUADDR W PPUメモリアドレス 書き込むメモリ領域のアドレス
        byte ppuData; //0x2007 PPUDATA RW PPUメモリデータ PPUメモリ領域のデータ
        int ppuAddrWriteCount; //0x2006のWrite回数を記録
        byte ppuAddressInc; //0x2006のインクリメントする大きさ

        int _totalPpuCycle; //PPUの合計サイクル数
        int _renderLine; //次に描画するlineを保持

        Nes nes;
        public Ppu(Nes nes)
        {
            ppuAddress = new byte[0x4000];
            this.nes = nes;

            sprite = new byte[nes.rom.CharacterRom.Length / 16, 8, 8];
            screen = new byte[61440][];

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
                 * 
                 * 0x2001 PPUMASK W コントロールレジスタ2 背景イネーブルなどPPUの設定
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
                case 0x2000:
                case 0x2001:
                    WriteMemory(address, value);
                    break;
                case 0x2003:
                    break;
                case 0x2004:
                    break;
                case 0x2005:
                    break;
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
                case 0x2007:
                    ppuData = value;
                    WriteMemory(ppuAddr, value);
                    ppuAddr += ppuAddressInc;
                    break;
            }
        }


        public void ReadPpuRegister(ushort address)
        {
            switch (address)
            {
                case 0x2002:
                    break;
                case 0x2004:
                    break;
                case 0x2007:
                    break;
            }
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
                    BgRenderScreen();
                    _totalPpuCycle -= 341;
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
                if (_renderLine == 240)
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
        void BgRenderScreen()
        {
            int nameTableNumber = 0x2000 + (RenderLine / 8) * 32; //読み込むネームテーブルのアドレス
            int attrTableNumber = 0x23C0; //読み込む属性テーブルのアドレス
            int attrTablePaletteNumber = 0; //パレット内の読み込む色の番号
            int column = 0; //現在の行
            int spriteLine = RenderLine % 8; //今読み込んでるラインのスプライトの列
            int unitRenderLine = RenderLine - (32 * (RenderLine / 32));

            for (int i = RenderLine * 256;i < (RenderLine + 1) * 256;i++)
            {
                int unitColumn = column - (32 * (column / 32));

                if ((column % 8) == 0 && column != 0) nameTableNumber++;
                if ((i % 32) == 0 && i != RenderLine * 256) attrTableNumber++;

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
