using System;
namespace NES_Emulator.NES
{
    public class PpuRegister
    {
        byte _ppuCtrl; //0x2000 PPUCTRL W コントロールレジスタ1 割り込みなどPPUの設定
        byte _ppuMask; //0x2001 PPUMASK W コントロールレジスタ2 背景イネーブルなどのPPU設定
        byte _ppuStatus; //0x2002 PPUSTATUS R PPUステータス PPUステータス
        byte _oamAddr; //0x2003 OAMADDR W スプライトメモリデータ 書き込むスプライト領域のアドレス
        byte _oamData; //0x2004 OAMDATA RW デシマルモード スプライト領域のデータ
        byte _ppuScroll; //0x2005 PPUSCROLL W 背景スクロールオフセット 背景スクロール値
        byte _ppuAddr; //0x2006 PPUADDR W PPUメモリアドレス 書き込むメモリ領域のアドレス
        byte _ppuData; //0x2007 PPUDATA RW PPUメモリデータ PPUメモリ領域のデータ


        public PpuRegister(Cpu cpu)
        {
            
        }

        public byte PpuCtrl
        {
            get { return _ppuCtrl; }
            private set { _ppuCtrl = value; }
        }

        public byte PpuMask
        {
            get { return _ppuMask; }
            private set { _ppuMask = value; }
        }

        public byte PpuStatus
        {
            get { return _ppuStatus; }
            private set { _ppuStatus = value; }
        }

        public byte OamAddr
        {
            get { return _oamAddr; }
            private set { _oamAddr = value; }
        }

        public byte OamData
        {
            get { return _oamData; }
            private set { _oamData = value; }
        }

        public byte PpuScroll
        {
            get { return _ppuScroll; }
            private set { _ppuScroll = value; }
        }

        public byte PpuAddr
        {
            get { return _ppuAddr; }
            private set { _ppuAddr = value; }
        }

        public byte PpuData
        {
            get { return _ppuData; }
            private set { _ppuData = value; }
        }

    }
}
