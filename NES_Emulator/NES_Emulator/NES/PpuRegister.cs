using System;
namespace NES_Emulator.NES
{
    public class PpuRegister : Cpu
    {
        protected byte PpuCtrl { get; private set; } //0x2000 PPUCTRL W コントロールレジスタ1 割り込みなどPPUの設定
        protected byte PpuMask { get; private set; } //0x2001 PPUMASK W コントロールレジスタ2 背景イネーブルなどのPPU設定
        protected byte PpuStatus { get; private set; } //0x2002 PPUSTATUS R PPUステータス PPUステータス
        protected byte OamAddr { get; private set; } //0x2003 OAMADDR W スプライトメモリデータ 書き込むスプライト領域のアドレス
        protected byte OamData { get; private set; } //0x2004 OAMDATA RW デシマルモード スプライト領域のデータ
        protected byte PpuScroll { get; private set; } //0x2005 PPUSCROLL W 背景スクロールオフセット 背景スクロール値
        protected byte PpuAddr{ get; private set; } //0x2006 PPUADDR W PPUメモリアドレス 書き込むメモリ領域のアドレス
        protected byte PpuData{ get; private set; } //0x2007 PPUDATA RW PPUメモリデータ PPUメモリ領域のデータ

        int ppuAddrWriteCount; //0x2006のWrite回数を記録

        public PpuRegister()
        {
            ppuAddrWriteCount = 0;
        }

        protected override void WritePpuRegister(ushort address, byte value)
        {
            switch (address)
            {
                case 0x2000:
                    break;
                case 0x2001:
                    break;
                case 0x2002:
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
                            PpuAddr = (byte)(value * 0x100);
                            ppuAddrWriteCount++;
                            break;
                        case 1:
                            PpuAddr += value;
                            ppuAddrWriteCount = 0;
                            break;
                    }
                    break;
                case 0x2007:
                    PpuData = value;
                    PpuAddr += 0x01;
                    break;
            }
        }

        protected override void ReadPpuRegister(ushort address)
        {
            
        }
    }
}
