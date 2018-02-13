using System;

namespace NES_Emulator.NES
{
    public class Cpu
    {
        //レジスタ
        byte registerA; //8bit
        byte registerX; //8bit
        byte registerY; //8bit
        byte registerS; //8bit
        ushort programCounter; //16bit

        //フラグ
        byte nFlag; //演算結果のbit7の値
        byte vFlag; //演算結果がオーバーフローを起こしたらセット
        byte bFlag; //BRK発生時にセット, IRQ発生時にクリア
        byte iFlag; //0:IRQ許可, 1:IRQ禁止
        byte zFlag; //演算結果が0のときにセット
        byte cFlag; //キャリー発生時にセット

        /* CPUメモリマップ
         * アドレス        サイズ
         * 0x0000〜0x07FF 0x0800 WRAM
         * -> 0x0100〜0x01FF 0x0100 スタック
         * 0x2000〜0x2007 0x0008 PPUレジスタ
         * 0x4000〜0x401F 0x0020 APU I/O, PAD
         * 0x4020〜0x5FFF 0x1FE0 拡張ROM
         * 0x6000〜0x7FFF 0x2000 拡張RAM
         * 0x8000〜0xBFFF 0x4000 PRG-ROM
         * 0xC000〜0xFFFF 0x4000 PRG-ROM
         */
        byte[] CpuAddress;


        public Cpu()
        {
            CpuAddress = new byte[0x10000];
        }

        /// <summary>
        /// Pのゲッター
        /// </summary>
        /// <returns>P</returns>
        byte GetRegisterP()
        {
            return (byte)(nFlag * 0x80 + vFlag * 0x40 + 0x20 + bFlag * 0x10 + iFlag * 0x04 + zFlag * 0x02 + cFlag);
        }

        /// <summary>
        /// Pのセッター各bitをフラグに格納
        /// </summary>
        /// <param name="value">P</param>
        void SetRegisterP(byte value)
        {
            nFlag = (byte)(value >> 7);
            vFlag = (byte)((value << 1) >> 7);
            bFlag = (byte)((value << 3) >> 7);
            iFlag = (byte)((value << 5) >> 7);
            zFlag = (byte)((value << 6) >> 7);
            cFlag = (byte)((value << 7) >> 7);
        }

        /*------------------------------------------------------------
         * アドレッシング・モードここから
         * - 表記[N]はアドレスN
         * - IM8は8bit値IM16は16bit値
         -------------------------------------------------------------*/
        /// <summary>
        /// 即値
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort Immediate()
        {
            ushort address = ++programCounter;
            programCounter++;
            return address;
        }

        /// <summary>
        /// [IM8]
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort Zeropage()
        {
            ushort address = ++programCounter;
            programCounter++;
            return CpuAddress[address];
        }

        /// <summary>
        /// [IM8 + X]
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort ZeropageX()
        {
            ushort address = ++programCounter;
            programCounter++;
            return (ushort)(CpuAddress[address] + registerX);
        }

        /// <summary>
        /// [IM8 + Y]
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort ZeropageY()
        {
            ushort address = ++programCounter;
            programCounter++;
            return (ushort)(CpuAddress[address] + registerY);
        }

        /// <summary>
        /// [IM16]
        /// 2番目のバイトを下位アドレスに, 3番目のバイトを上位アドレスにする
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort Absolute()
        {
            ushort address = (ushort)(CpuAddress[programCounter + 2] * 0x100 + CpuAddress[programCounter + 1]);
            programCounter += 3;
            return address;
        }

        /// <summary>
        /// [IM16 + X]
        /// 2番目のバイトを下位アドレスに, 3番目のバイトを上位アドレスにする
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort AbsoluteX()
        {
            ushort address = (ushort)(CpuAddress[programCounter + 2] * 0x100 + CpuAddress[programCounter + 1]);
            programCounter += 3;
            return (ushort)(address + registerX);
        }

        /// <summary>
        /// [IM16 + Y]
        /// 2番目のバイトを下位アドレスに, 3番目のバイトを上位アドレスにする
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort AbsoluteY()
        {
            ushort address = (ushort)(CpuAddress[programCounter + 2] * 0x100 + CpuAddress[programCounter + 1]);
            programCounter += 3;
            return (ushort)(address + registerY);
        }

        /// <summary>
        /// [[IM8 + X]]
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort IndirectX()
        {
            return (ushort)CpuAddress[ZeropageX()];
        }

        /// <summary>
        /// [[IM8] + Y]
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort IndirectY()
        {
            return (ushort)CpuAddress[Zeropage() + registerY];
        }
        /*------------------------------------------------------------
         * アドレッシング・モードここまで
         -------------------------------------------------------------*/

        /*------------------------------------------------------------
         * 命令セットここから
         -------------------------------------------------------------*/
        /// <summary>
        /// メモリからAにロード
        /// N: ロードした値の最上位ビット
        /// Z: ロードコピーした値が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="value">実効アドレス</param>
        void LDA(int cycle, ushort address)
        {
            registerA = CpuAddress[address];
            FlagNandZ(registerA);
        }

        /// <summary>
        /// メモリからXにロード
        /// N: ロードした値の最上位ビット
        /// Z: ロードコピーした値が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="value">実効アドレス</param>
        void LDX(int cycle, ushort address)
        {
            registerX = CpuAddress[address];
            FlagNandZ(registerX);
        }

        /// <summary>
        /// メモリからYにロード
        /// N: ロードした値の最上位ビット
        /// Z: ロードコピーした値が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="value">実効アドレス</param>
        void LDY(int cycle, ushort address)
        {
            registerY = CpuAddress[address];
            FlagNandZ(registerY);
        }

        /// <summary>
        /// Aからメモリにストア
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void STA(int cycle, ushort address)
        {
            CpuAddress[address] = registerA;
        }

        /// <summary>
        /// Xからメモリにストア
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void STX(int cycle, ushort address)
        {
            CpuAddress[address] = registerX;
        }

        /// <summary>
        /// Yからメモリにストア
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void STY(int cycle, ushort address)
        {
            CpuAddress[address] = registerY;
        }

        /// <summary>
        /// AをXへコピー
        /// N: コピーした値の最上位ビット
        /// Z: コピーした値が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        void TAX(int cycle)
        {
            programCounter++;
            registerX = registerA;
            FlagNandZ(registerX);
        }

        /// <summary>
        /// AをYへコピー
        /// N: コピーした値の最上位ビット
        /// Z: コピーした値が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        void TAY(int cycle)
        {
            programCounter++;
            registerY = registerA;
            FlagNandZ(registerY);
        }

        /// <summary>
        /// SをXへコピー
        /// N: コピーした値の最上位ビット
        /// Z: コピーした値が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        void TSX(int cycle)
        {
            programCounter++;
            registerX = registerS;
            FlagNandZ(registerX);
        }

        /// <summary>
        /// XをAへコピー
        /// N: コピーした値の最上位ビット
        /// Z: コピーした値が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        void TXA(int cycle)
        {
            programCounter++;
            registerA = registerX;
            FlagNandZ(registerA);
        }

        /// <summary>
        /// XをSへコピー
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        void TXS(int cycle)
        {
            programCounter++;
            registerS = registerX;
        }

        /// <summary>
        /// YをAへコピー
        /// N: コピーした値の最上位ビット
        /// Z: コピーした値が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        void TYA(int cycle)
        {
            programCounter++;
            registerA = registerY;
            FlagNandZ(registerA);
        }


        void ADC(int cycle, ushort address)
        {
            registerA += (byte)(CpuAddress[address] + cFlag);
            FlagNandZ(registerA);
        }


        /// <summary>
        /// Aとメモリを論理AND演算
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void AND(int cycle, ushort address)
        {
            registerA &= CpuAddress[address];
            FlagNandZ(registerA);
        }

        /// <summary>
        /// Aを1回左シフト
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        void ASL(int cycle)
        {
            programCounter++;
            cFlag = (byte)(registerA >> 7);
            registerA <<= 1;
            FlagNandZ(registerA);
        }

        /// <summary>
        /// メモリを1回左シフト
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void ASL(int cycle, ushort address)
        {
            cFlag = (byte)(CpuAddress[address] >> 7);
            CpuAddress[address] <<= 1;
            FlagNandZ(CpuAddress[address]);
        }

        /// <summary>
        /// bitテストを行う
        /// N: メモリのbit7
        /// V: メモリのbit6
        /// Z: 結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void BIT(int cycle, ushort address)
        {
            nFlag = (byte)(CpuAddress[address] >> 7);
            vFlag = (byte)((CpuAddress[address] << 1) >> 7);
            if ((registerA & CpuAddress[address]) == 0) zFlag = 1;
        }

        /// <summary>
        /// 演算の結果によって, フラグをセット(A - メモリ)
        /// N: 減算結果の最上位ビット
        /// Z: 減算結果が0であるか
        /// C: 減算結果が正かゼロのときセット, 負のときクリア
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void CMP(int cycle, ushort address)
        {
            byte tmp = (byte)(registerA - CpuAddress[address]);
            ComparisonFlag(tmp);
        }

        /// <summary>
        /// 演算の結果によって, フラグをセット(X - メモリ)
        /// N: 減算結果の最上位ビット
        /// Z: 減算結果が0であるか
        /// C: 減算結果が正かゼロのときセット, 負のときクリア
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void CPX(int cycle, ushort address)
        {
            byte tmp = (byte)(registerX - CpuAddress[address]);
            ComparisonFlag(tmp);
        }

        /// <summary>
        /// 演算の結果によって, フラグをセット(Y - メモリ)
        /// N: 減算結果の最上位ビット
        /// Z: 減算結果が0であるか
        /// C: 減算結果が正かゼロのときセット, 負のときクリア
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void CPY(int cycle, ushort address)
        {
            byte tmp = (byte)(registerY - CpuAddress[address]);
            ComparisonFlag(tmp);
        }

        /// <summary>
        /// CMP CPX CPYのフラグ処理
        /// N: 減算結果の最上位ビット
        /// Z: 減算結果が0であるか
        /// C: 減算結果が正かゼロのときセット, 負のときクリア
        /// </summary>
        /// <param name="value">減算結果</param>
        void ComparisonFlag(byte value)
        {
            FlagNandZ(value);
            if (value >= 0)
            {
                cFlag = 1;
            }
            else
            {
                cFlag = 0;
            }
        }

        /// <summary>
        /// メモリをデクリメント
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="address">実効アドレス</param>
        void DEC(int cycle, ushort address)
        {
            CpuAddress[address]--;
            FlagNandZ(CpuAddress[address]);
        }

        /// <summary>
        /// Xをデクリメント
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="address">実効アドレス</param>
        void DEX(int cycle)
        {
            programCounter++;
            registerX--;
            FlagNandZ(registerX);
        }

        /// <summary>
        /// Yをデクリメント
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="address">実効アドレス</param>
        void DEY(int cycle)
        {
            programCounter++;
            registerY--;
            FlagNandZ(registerY);
        }

        /// <summary>
        /// Aとメモリを論理XOR演算
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void EOR(int cycle, ushort address)
        {
            registerA ^= CpuAddress[address];
            FlagNandZ(registerA);
        }

        /// <summary>
        /// メモリをインクリメント
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="address">実効アドレス</param>
        void INC(int cycle, ushort address)
        {
            CpuAddress[address]++;
            FlagNandZ(CpuAddress[address]);
        }

        /// <summary>
        /// Xをインクリメント
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="address">実効アドレス</param>
        void INX(int cycle)
        {
            programCounter++;
            registerX++;
            FlagNandZ(registerX);
        }

        /// <summary>
        /// Yをインクリメント
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="address">実効アドレス</param>
        void INY(int cycle)
        {
            programCounter++;
            registerX++;
            FlagNandZ(registerX);
        }

        /// <summary>
        /// Aを1回右シフト
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void LSR(int cycle)
        {
            programCounter++;
            cFlag = (byte)((registerA << 7) >> 7);
            registerA >>= 1;
            FlagNandZ(registerA);
        }

        /// <summary>
        /// メモリを1回右シフト
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void LSR(int cycle, ushort address)
        {
            cFlag = (byte)((CpuAddress[address] << 7) >> 7);
            CpuAddress[address] >>= 1;
            FlagNandZ(CpuAddress[address]);
        }

        /// <summary>
        /// Aとメモリを論理OR演算
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void ORA(int cycle, ushort address)
        {
            registerA |= CpuAddress[address];
            FlagNandZ(registerA);
        }

        /// <summary>
        /// Aを1回左ローテート
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        void ROL(int cycle)
        {
            programCounter++;
            byte tmp = (byte)(registerA >> 7);
            registerA = (byte)((registerA << 1) + cFlag);
            cFlag = tmp;
            FlagNandZ(registerA);
        }

        /// <summary>
        /// メモリを1回左ローテート
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void ROL(int cycle, ushort address)
        {
            byte tmp = (byte)(CpuAddress[address] >> 7);
            CpuAddress[address] = (byte)((CpuAddress[address] << 1) + cFlag);
            cFlag = tmp;
            FlagNandZ(CpuAddress[address]);
        }

        /// <summary>
        /// Aを1回右ローテート
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        void ROR(int cycle)
        {
            programCounter++;
            byte tmp = (byte)((registerA << 7) >> 7);
            registerA = (byte)((registerA >> 1) + nFlag * 0x80);
            cFlag = tmp;
            FlagNandZ(registerA);
        }

        /// <summary>
        /// メモリを1回右ローテート
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void ROR(int cycle, ushort address)
        {
            byte tmp = (byte)((CpuAddress[address] << 7) >> 7);
            CpuAddress[address] = (byte)((CpuAddress[address] >> 1) + nFlag * 0x80);
            cFlag = tmp;
            FlagNandZ(CpuAddress[address]);
        }

        void SBC(int cycle, ushort address)
        {

        }

        /// <summary>
        /// スタックにプッシュダウン
        /// PHA, PHP
        /// サイクル数3
        /// </summary>
        /// <param name="value">pushする値</param>
        void Push(byte value)
        {
            programCounter++;
            CpuAddress[0x0100 + registerS] = value;
            registerS--;
        }

        /// <summary>
        /// スタックからAにポップアップ
        /// サイクル数4
        /// N: POPした値の最上位ビット
        /// Z: POPした値が0であるか
        /// </summary>
        void PLA()
        {
            programCounter++;
            registerS++;
            registerA = CpuAddress[0x0100 + registerS];
            FlagNandZ(registerA);
        }

        /// <summary>
        /// スタックからPにポップアップ
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        void PLP()
        {
            programCounter++;
            registerS++;
            SetRegisterP(CpuAddress[0x0100 + registerS]);
        }

        /// <summary>
        /// アドレスへジャンプする
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="address">実効アドレス</param>
        void JMP(int cycle, ushort address)
        {
            programCounter = CpuAddress[address];
        }

        /// <summary>
        /// サブルーチンを呼び出す
        /// 元のPCを上位, 下位バイトの順にpushする
        /// この時保存するPCはJSRの最後のバイトアドレス
        /// サイクル数6
        /// </summary>
        /// <param name="address">実効アドレス</param>
        void JSR(ushort address)
        {
            ushort savePC = --programCounter;
            Push((byte)(savePC >> 8));
            Push((byte)((savePC << 8) >> 8));
            programCounter = CpuAddress[address];
        }

        /// <summary>
        /// サブルーチンから復帰する
        /// 下位, 上位バイトの順にpopする
        /// サイクル数6
        /// </summary>
        void RTS()
        {
            programCounter = (ushort)(CpuAddress[0x0100 + registerS + 2] * 0x0100 + CpuAddress[0x0100 + registerS + 1]);
            registerS += 2;
            programCounter++;
        }

        void RTI()
        {

        }

        /// <summary>
        /// ブランチ命令
        /// 次の命令を示すPCに2番目のバイトを加算した値を実効アドレスにする
        /// 2番目のバイトは-128〜+127の範囲, サイクル数2
        /// BCC, BCS, BEQ, BMI, BNE, BPL, BVC, BVS
        /// </summary>
        /// <param name="flag">If set to <c>true</c> flag.</param>
        void Branch(bool flag)
        {
            if (flag)
            {
                sbyte address;
                byte tmp = CpuAddress[programCounter + 1];
                if ((tmp >> 7) == 1)
                {
                    tmp ^= 0xff;
                    address = (sbyte)-(tmp + 1);
                }
                else
                {
                    address = (sbyte)tmp;
                }
                address += (sbyte)(programCounter + 2);
                programCounter = (byte)address;
            }
            else
            {
                programCounter += 2;
            }
        }


        /// <summary>
        /// フラグN, Zを決める
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// </summary>
        /// <param name="value">結果</param>
        void FlagNandZ(byte value)
        {
            if (value == 0) zFlag = 1;
            nFlag = (byte)(value >> 7);
        }

        /*------------------------------------------------------------
         * 命令セットここまで
         -------------------------------------------------------------*/

        public void Execute(byte opcode)
        {
            switch (opcode)
            {
                case 0xA9:
                    LDA(2, Immediate());
                    break;
                case 0xA5:
                    LDA(3, Zeropage());
                    break;
                case 0xB5:
                    LDA(4, ZeropageX());
                    break;
                case 0xAD:
                    LDA(4, Absolute());
                    break;
                case 0xBD:
                    LDA(4, AbsoluteX());
                    break;
                case 0xB9:
                    LDA(4, AbsoluteY());
                    break;
                case 0xA1:
                    LDA(6, IndirectX());
                    break;
                case 0xB1:
                    LDA(5, IndirectY());
                    break;
                case 0xA2:
                    LDX(2, Immediate());
                    break;
                case 0xA6:
                    LDX(3, Zeropage());
                    break;
                case 0xB6:
                    LDX(4, ZeropageY());
                    break;
                case 0xAE:
                    LDX(4, Absolute());
                    break;
                case 0xBE:
                    LDX(4, AbsoluteY());
                    break;
                case 0xA0:
                    LDY(2, Immediate());
                    break;
                case 0xA4:
                    LDY(3, Zeropage());
                    break;
                case 0xB4:
                    LDY(4, ZeropageX());
                    break;
                case 0xAC:
                    LDY(4, Absolute());
                    break;
                case 0xBC:
                    LDY(4, AbsoluteX());
                    break;
                case 0x85:
                    STA(3, Zeropage());
                    break;
                case 0x95:
                    STA(4, ZeropageX());
                    break;
                case 0x8D:
                    STA(4, Absolute());
                    break;
                case 0x9D:
                    STA(5, AbsoluteX());
                    break;
                case 0x99:
                    STA(5, AbsoluteY());
                    break;
                case 0x81:
                    STA(6, IndirectX());
                    break;
                case 0x91:
                    STA(6, IndirectY());
                    break;
                case 0x86:
                    STX(3, Zeropage());
                    break;
                case 0x96:
                    STX(4, ZeropageY());
                    break;
                case 0x8E:
                    STX(4, Absolute());
                    break;
                case 0x84:
                    STY(3, Zeropage());
                    break;
                case 0x94:
                    STY(4, ZeropageX());
                    break;
                case 0x8C:
                    STY(4, Absolute());
                    break;
                case 0xAA:
                    TAX(2);
                    break;
                case 0xA8:
                    TAY(2);
                    break;
                case 0xBA:
                    TSX(2);
                    break;
                case 0x8A:
                    TXA(2);
                    break;
                case 0x9A:
                    TXS(2);
                    break;
                case 0x98:
                    TYA(2);
                    break;
                case 0x69:
                    ADC(2, Immediate());
                    break;
                case 0x65:
                    ADC(3, Zeropage());
                    break;
                case 0x75:
                    ADC(4, ZeropageX());
                    break;
                case 0x6D:
                    ADC(4, Absolute());
                    break;
                case 0x7D:
                    ADC(4, AbsoluteX());
                    break;
                case 0x79:
                    ADC(4, AbsoluteY());
                    break;
                case 0x61:
                    ADC(6, IndirectX());
                    break;
                case 0x71:
                    ADC(5, IndirectY());
                    break;
                case 0x29:
                    AND(2, Immediate());
                    break;
                case 0x25:
                    AND(3, Zeropage());
                    break;
                case 0x35:
                    AND(4, ZeropageX());
                    break;
                case 0x2D:
                    AND(4, Absolute());
                    break;
                case 0x3D:
                    AND(4, AbsoluteX());
                    break;
                case 0x39:
                    AND(4, AbsoluteY());
                    break;
                case 0x21:
                    AND(6, IndirectX());
                    break;
                case 0x31:
                    AND(5, IndirectY());
                    break;
                case 0x0A:
                    ASL(2);
                    break;
                case 0x06:
                    ASL(5, Zeropage());
                    break;
                case 0x16:
                    ASL(6, ZeropageX());
                    break;
                case 0x0E:
                    ASL(6, Absolute());
                    break;
                case 0x1E:
                    ASL(7, AbsoluteX());
                    break;
                case 0x24:
                    BIT(3, Zeropage());
                    break;
                case 0x2C:
                    BIT(4, Absolute());
                    break;
                case 0xC9:
                    CMP(2, Immediate());
                    break;
                case 0xC5:
                    CMP(3, Zeropage());
                    break;
                case 0xD5:
                    CMP(4, ZeropageX());
                    break;
                case 0xCD:
                    CMP(4, Absolute());
                    break;
                case 0xDD:
                    CMP(4, AbsoluteX());
                    break;
                case 0xD9:
                    CMP(4, AbsoluteY());
                    break;
                case 0xC1:
                    CMP(6, IndirectX());
                    break;
                case 0xD1:
                    CMP(5, IndirectY());
                    break;
                case 0xE0:
                    CPX(2, Immediate());
                    break;
                case 0xE4:
                    CPX(3, Zeropage());
                    break;
                case 0xEC:
                    CPX(4, Absolute());
                    break;
                case 0xC0:
                    CPY(2, Immediate());
                    break;
                case 0xC4:
                    CPY(3, Zeropage());
                    break;
                case 0xCC:
                    CPY(4, Absolute());
                    break;
                case 0xC6:
                    DEC(5, Zeropage());
                    break;
                case 0xD6:
                    DEC(6, ZeropageX());
                    break;
                case 0xCE:
                    DEC(6, Absolute());
                    break;
                case 0xDE:
                    DEC(7, AbsoluteX());
                    break;
                case 0xCA:
                    DEX(2);
                    break;
                case 0x88:
                    DEY(2);
                    break;
                case 0x49:
                    EOR(2, Immediate());
                    break;
                case 0x45:
                    EOR(3, Zeropage());
                    break;
                case 0x55:
                    EOR(4, ZeropageX());
                    break;
                case 0x4D:
                    EOR(4, Absolute());
                    break;
                case 0x5D:
                    EOR(4, AbsoluteX());
                    break;
                case 0x59:
                    EOR(4, AbsoluteY());
                    break;
                case 0x41:
                    EOR(6, IndirectX());
                    break;
                case 0x51:
                    EOR(5, IndirectY());
                    break;
                case 0xE6:
                    INC(5, Zeropage());
                    break;
                case 0xF6:
                    INC(6, ZeropageX());
                    break;
                case 0xEE:
                    INC(6, Absolute());
                    break;
                case 0xFE:
                    INC(7, AbsoluteX());
                    break;
                case 0xE8:
                    INX(2);
                    break;
                case 0xC8:
                    INY(2);
                    break;
                case 0x4A:
                    LSR(2);
                    break;
                case 0x46:
                    LSR(5, Zeropage());
                    break;
                case 0x56:
                    LSR(6, ZeropageX());
                    break;
                case 0x4E:
                    LSR(6, Absolute());
                    break;
                case 0x5E:
                    LSR(7, AbsoluteX());
                    break;
                case 0x09:
                    ORA(2, Immediate());
                    break;
                case 0x05:
                    ORA(3, Zeropage());
                    break;
                case 0x15:
                    ORA(4, ZeropageX());
                    break;
                case 0x0D:
                    ORA(4, Absolute());
                    break;
                case 0x1D:
                    ORA(4, AbsoluteX());
                    break;
                case 0x19:
                    ORA(4, AbsoluteY());
                    break;
                case 0x01:
                    ORA(6, IndirectX());
                    break;
                case 0x11:
                    ORA(5, IndirectY());
                    break;
                case 0x2A:
                    ROL(2);
                    break;
                case 0x26:
                    ROL(5, Zeropage());
                    break;
                case 0x36:
                    ROL(6, ZeropageX());
                    break;
                case 0x2E:
                    ROL(6, Absolute());
                    break;
                case 0x3E:
                    ROL(7, AbsoluteX());
                    break;
                case 0x6A:
                    ROR(2);
                    break;
                case 0x66:
                    ROR(5, Zeropage());
                    break;
                case 0x76:
                    ROR(6, ZeropageX());
                    break;
                case 0x6E:
                    ROR(6, Absolute());
                    break;
                case 0x7E:
                    ROR(7, AbsoluteX());
                    break;
                case 0xE9:
                    SBC(2, Immediate());
                    break;
                case 0xE5:
                    SBC(3, Zeropage());
                    break;
                case 0xF5:
                    SBC(4, ZeropageX());
                    break;
                case 0xED:
                    SBC(4, Absolute());
                    break;
                case 0xFD:
                    SBC(4, AbsoluteX());
                    break;
                case 0xF9:
                    SBC(4, AbsoluteY());
                    break;
                case 0xE1:
                    SBC(6, IndirectX());
                    break;
                case 0xF1:
                    SBC(5, IndirectY());
                    break;
                case 0x48:
                    Push(registerA);
                    break;
                case 0x08:
                    Push(GetRegisterP());
                    break;
                case 0x68:
                    PLA();
                    break;
                case 0x28:
                    PLP();
                    break;
                case 0x4C:
                    JMP(3, Absolute());
                    break;
            }
        }
    }
}
