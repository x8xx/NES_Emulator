using System;
namespace NES_Emulator.NES
{
    public class PpuRegister
    {
        byte ppuCtrl; //0x2000 PPUCTRL W コントロールレジスタ1 割り込みなどPPUの設定
        byte ppuStatus; //0x2002 PPUSTATUS R PPUステータス PPUステータス
        byte oamAddr;  //0x2003 OAMADDR W スプライトメモリデータ 書き込むスプライト領域のアドレス
        byte oamData; //0x2004 OAMDATA RW デシマルモード スプライト領域のデータ
        byte ppuScroll; //0x2005 PPUSCROLL W 背景スクロールオフセット 背景スクロール値
        byte ppuAddr; //0x2006 PPUADDR W PPUメモリアドレス 書き込むメモリ領域のアドレス
        byte ppuData; //0x2007 PPUDATA RW PPUメモリデータ PPUメモリ領域のデータ

        int ppuAddrWriteCount; //0x2006のWrite回数を記録
        byte ppuAddressInc; //0x2006のインクリメントする大きさ

        Ppu ppu;
        public PpuRegister(Nes nes)
        {
            ppuAddrWriteCount = 0;
            ppuAddressInc = 0x01;
            ppu = nes.ppu;
        }

        public void WritePpuRegister(ushort address, byte value)
        {
            switch (address)
            {
                case 0x2000:
                    break;
                case 0x2001:
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
                            ppuAddr = (byte)(value * 0x100);
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
                    ppu.WriteMemory(ppuAddr, value);
                    ppuAddr += ppuAddressInc;
                    break;
            }
        }

        public void ReadPpuRegister(ushort address)
        {
            switch(address)
            {
                case 0x2002:
                    break;
                case 0x2004:
                    break;
                case 0x2007:
                    break;
            }
        }
    }
}
