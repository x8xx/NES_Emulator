using System;
using System.Collections.Generic;
namespace NES_Emulator.NES
{
    public class Ppu
    {
        /*PPUメモリマップ
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

        /* OAM (Object Attribute Memory)
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

        byte[][] screen;
        public IReadOnlyList<byte[]> Screen
        {
            get { return screen; }
        }

        int _totalPpuCycle; //PPUの合計サイクル数
        int renderLine; //次に描画するlineを保持
        int spriteLine; //描画中スプライトをどの列まで描画したかを保持

        Nes nes;
        public Ppu(Nes nes)
        {
            ppuAddress = new byte[0x4000];
            this.nes = nes;

            sprite = new byte[nes.rom.CharacterRom.Length / 16, 8, 8];
            screen = new byte[61440][];

            TotalPpuCycle = 0;
            renderLine = 0;
            spriteLine = 0;
        }

        public int TotalPpuCycle
        {
            get { return _totalPpuCycle; }
            set
            {
                _totalPpuCycle = value;
                if (_totalPpuCycle >= 341)
                {
                    BgRenderScreen();
                    _totalPpuCycle -= 341;
                }
            }
        }

        public void WriteMemory(ushort address, byte value)
        {
            ppuAddress[address] = value;
        }

        void BgRenderScreen()
        {
            int nameTableNumber = 0x2000;
            int attrTableNumber = 0x23C0;
            int attrTablePaletteNumber = 0;
            int column = 0;
            int unitRenderLine = renderLine - (32 * (renderLine / 32));
            if ((renderLine % 8) == 0) nameTableNumber += (renderLine / 8) * 8;

            for (int i = renderLine * 256;i < (renderLine + 1) * 256;i++)
            {
                int unitColumn = column - (32 * (column / 32));

                if ((i % 8) == 0) nameTableNumber++;
                if ((i % 32) == 0) attrTableNumber++;

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

                screen[i] = Nes.paletteColors[ppuAddress[0x3F00 + 4 * GetPalette(ppuAddress[attrTableNumber], attrTablePaletteNumber) + sprite[ppuAddress[nameTableNumber], spriteLine, column - (8 * (column / 8))]]];
                column++;
            }
            spriteLine++;
            if (spriteLine > 7) spriteLine = 0;
            renderLine++;
        }

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
                    string highOrder = BinaryNumberConversion(rom.CharacterRom[j + 8]);
                    string lowOrder = BinaryNumberConversion(rom.CharacterRom[j]);
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
        /// 8bit変換
        /// </summary>
        /// <returns>8bit</returns>
        /// <param name="originNumber">元値</param>
        string BinaryNumberConversion(byte originNumber)
        {
            string convertNumber = Convert.ToString(originNumber, 2);
            if (convertNumber.Length < 8)
            {
                for (int i = 0;i < 8 - convertNumber.Length;i++)
                {
                    convertNumber = convertNumber.Insert(0, "0");
                }
            }
            return convertNumber;
        }
    }
}
