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
        /// メモリからレジスタにロード
        /// N: ロードした値の最上位ビット
        /// Z: ロードコピーした値が0であるか
        /// LDA, LDX, LDY
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="address">実効アドレス</param>
        /// <param name="register">レジスタ</param>
        void LoadToRegister(int cycle, ushort address, ref byte register)
        {
            register = CpuAddress[address];
            FlagNandZ(register);
        }


        /// <summary>
        /// レジスタからメモリにストア
        /// STA, STX, STY
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="address">実効アドレス</param>
        /// <param name="register">レジスタ</param>
        void StoreToMemory(int cycle, ushort address, ref byte register)
        {
            CpuAddress[address] = register;
        }

        /// <summary>
        /// XをSへコピー
        /// サイクル数2
        /// </summary>
        void TXS()
        {
            programCounter++;
            registerS = registerX;
        }

        /// <summary>
        /// レジスタをコピー
        /// N: コピーした値の最上位ビット
        /// Z: コピーした値が0であるか
        /// TAX, TAY, TSX, TXA, TYA サイクル数2
        /// </summary>
        /// <param name="copySource">コピー元</param>
        /// <param name="copyTarget">コピー先</param>
        void CopyRegister(ref byte copySource, ref byte copyTarget)
        {
            programCounter++;
            copyTarget = copySource;
            FlagNandZ(copyTarget);
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
        /// メモリを1回左シフト
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void ASL(int cycle, ref byte value)
        {
            cFlag = (byte)(value >> 7);
            value <<= 1;
            FlagNandZ(value);
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
        /// 演算の結果によって, フラグをセット(レジスタ - メモリ)
        /// N: 減算結果の最上位ビット
        /// Z: 減算結果が0であるか
        /// C: 減算結果が正かゼロのときセット, 負のときクリア
        /// CMP, CPX, CPY
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void Comparison(int cycle, ushort address, ref byte register)
        {
            byte tmp = (byte)(register - CpuAddress[address]);
            FlagNandZ(tmp);
            if (tmp >= 0)
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
        /// メモリを1回右シフト
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void LSR(int cycle, ref byte value)
        {
            cFlag = (byte)((value << 7) >> 7);
            value >>= 1;
            FlagNandZ(value);
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
        /// メモリを1回左ローテート
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">値</param>
        void ROL(int cycle, ref byte value)
        {
            byte tmp = (byte)(value >> 7);
            value = (byte)((value << 1) + cFlag);
            cFlag = tmp;
            FlagNandZ(value);
        }

        /// <summary>
        /// メモリを1回右ローテート
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">値</param>
        void ROR(int cycle, ref byte value)
        {
            byte tmp = (byte)((value << 7) >> 7);
            value = (byte)((value >> 1) + nFlag * 0x80);
            cFlag = tmp;
            FlagNandZ(value);
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

        /// <summary>
        /// フラグクリア
        /// </summary>
        /// <param name="flag">Flag.</param>
        void ClearFlag(ref byte flag)
        {
            flag = 0;
            programCounter++;
        }

        /// <summary>
        /// フラグセット
        /// </summary>
        /// <param name="flag">Flag.</param>
        void SetFlag(ref byte flag)
        {
            flag = 1;
            programCounter++;
        }

        /*------------------------------------------------------------
         * 命令セットここまで
         -------------------------------------------------------------*/

        /// <summary>
        /// 命令を実効
        /// </summary>
        /// <param name="opcode">コード</param>
        public void Execute(byte opcode)
        {
            switch (opcode)
            {
                case 0xA9:
                    LoadToRegister(2, Immediate(), ref registerA);
                    break;
                case 0xA5:
                    LoadToRegister(3, Zeropage(), ref registerA);
                    break;
                case 0xB5:
                    LoadToRegister(4, ZeropageX(), ref registerA);
                    break;
                case 0xAD:
                    LoadToRegister(4, Absolute(), ref registerA);
                    break;
                case 0xBD:
                    LoadToRegister(4, AbsoluteX(), ref registerA);
                    break;
                case 0xB9:
                    LoadToRegister(4, AbsoluteY(), ref registerA);
                    break;
                case 0xA1:
                    LoadToRegister(6, IndirectX(), ref registerA);
                    break;
                case 0xB1:
                    LoadToRegister(5, IndirectY(), ref registerA);
                    break;
                case 0xA2:
                    LoadToRegister(2, Immediate(), ref registerX);
                    break;
                case 0xA6:
                    LoadToRegister(3, Zeropage(), ref registerX);
                    break;
                case 0xB6:
                    LoadToRegister(4, ZeropageY(), ref registerX);
                    break;
                case 0xAE:
                    LoadToRegister(4, Absolute(), ref registerX);
                    break;
                case 0xBE:
                    LoadToRegister(4, AbsoluteY(), ref registerX);
                    break;
                case 0xA0:
                    LoadToRegister(2, Immediate(), ref registerY);
                    break;
                case 0xA4:
                    LoadToRegister(3, Zeropage(), ref registerY);
                    break;
                case 0xB4:
                    LoadToRegister(4, ZeropageX(), ref registerY);
                    break;
                case 0xAC:
                    LoadToRegister(4, Absolute(), ref registerY);
                    break;
                case 0xBC:
                    LoadToRegister(4, AbsoluteX(), ref registerY);
                    break;
                case 0x85:
                    StoreToMemory(3, Zeropage(), ref registerA);
                    break;
                case 0x95:
                    StoreToMemory(4, ZeropageX(), ref registerA);
                    break;
                case 0x8D:
                    StoreToMemory(4, Absolute(), ref registerA);
                    break;
                case 0x9D:
                    StoreToMemory(5, AbsoluteX(), ref registerA);
                    break;
                case 0x99:
                    StoreToMemory(5, AbsoluteY(), ref registerA);
                    break;
                case 0x81:
                    StoreToMemory(6, IndirectX(), ref registerA);
                    break;
                case 0x91:
                    StoreToMemory(6, IndirectY(), ref registerA);
                    break;
                case 0x86:
                    StoreToMemory(3, Zeropage(), ref registerX);
                    break;
                case 0x96:
                    StoreToMemory(4, ZeropageY(), ref registerX);
                    break;
                case 0x8E:
                    StoreToMemory(4, Absolute(), ref registerX);
                    break;
                case 0x84:
                    StoreToMemory(3, Zeropage(), ref registerY);
                    break;
                case 0x94:
                    StoreToMemory(4, ZeropageX(), ref registerY);
                    break;
                case 0x8C:
                    StoreToMemory(4, Absolute(), ref registerY);
                    break;
                case 0xAA:
                    CopyRegister(ref registerA, ref registerX);
                    break;
                case 0xA8:
                    CopyRegister(ref registerA, ref registerY);
                    break;
                case 0xBA:
                    CopyRegister(ref registerS, ref registerX);
                    break;
                case 0x8A:
                    CopyRegister(ref registerX, ref registerA);
                    break;
                case 0x9A:
                    TXS();
                    break;
                case 0x98:
                    CopyRegister(ref registerY, ref registerA);
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
                    ASL(2, ref registerA);
                    programCounter++;
                    break;
                case 0x06:
                    ASL(5, ref CpuAddress[Zeropage()]);
                    break;
                case 0x16:
                    ASL(6, ref CpuAddress[ZeropageX()]);
                    break;
                case 0x0E:
                    ASL(6, ref CpuAddress[Absolute()]);
                    break;
                case 0x1E:
                    ASL(7, ref CpuAddress[AbsoluteX()]);
                    break;
                case 0x24:
                    BIT(3, Zeropage());
                    break;
                case 0x2C:
                    BIT(4, Absolute());
                    break;
                case 0xC9:
                    Comparison(2, Immediate(), ref registerA);
                    break;
                case 0xC5:
                    Comparison(3, Zeropage(), ref registerA);
                    break;
                case 0xD5:
                    Comparison(4, ZeropageX(), ref registerA);
                    break;
                case 0xCD:
                    Comparison(4, Absolute(), ref registerA);
                    break;
                case 0xDD:
                    Comparison(4, AbsoluteX(), ref registerA);
                    break;
                case 0xD9:
                    Comparison(4, AbsoluteY(), ref registerA);
                    break;
                case 0xC1:
                    Comparison(6, IndirectX(), ref registerA);
                    break;
                case 0xD1:
                    Comparison(5, IndirectY(), ref registerA);
                    break;
                case 0xE0:
                    Comparison(2, Immediate(), ref registerX);
                    break;
                case 0xE4:
                    Comparison(3, Zeropage(), ref registerX);
                    break;
                case 0xEC:
                    Comparison(4, Absolute(), ref registerX);
                    break;
                case 0xC0:
                    Comparison(2, Immediate(), ref registerY);
                    break;
                case 0xC4:
                    Comparison(3, Zeropage(), ref registerY);
                    break;
                case 0xCC:
                    Comparison(4, Absolute(), ref registerY);
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
                /*
                 * Xをデクリメント
                 * N: 演算結果の最上位ビット
                 * Z: 演算結果が0であるか
                 * サイクル数2
                 */
                case 0xCA:
                    programCounter++;
                    registerX--;
                    FlagNandZ(registerX);
                    break;
                /*
                 * Yをデクリメント
                 * N: 演算結果の最上位ビット
                 * Z: 演算結果が0であるか
                 * サイクル数2
                 */
                case 0x88:
                    programCounter++;
                    registerY--;
                    FlagNandZ(registerX);
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
                /*
                 * Xをインクリメント
                 * N: 演算結果の最上位ビット
                 * Z: 演算結果が0であるか
                 * サイクル数2
                 */
                case 0xE8:
                    programCounter++;
                    registerX++;
                    FlagNandZ(registerX);
                    break;
                /*
                 * Yをインクリメント
                 * N: 演算結果の最上位ビット
                 * Z: 演算結果が0であるか
                 * サイクル数2
                 */
                case 0xC8:
                    programCounter++;
                    registerY++;
                    FlagNandZ(registerY);
                    break;
                case 0x4A:
                    LSR(2, ref registerA);
                    programCounter++;
                    break;
                case 0x46:
                    LSR(5, ref CpuAddress[Zeropage()]);
                    break;
                case 0x56:
                    LSR(6, ref CpuAddress[ZeropageX()]);
                    break;
                case 0x4E:
                    LSR(6, ref CpuAddress[Absolute()]);
                    break;
                case 0x5E:
                    LSR(7, ref CpuAddress[AbsoluteX()]);
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
                    ROL(2, ref registerA);
                    programCounter++;
                    break;
                case 0x26:
                    ROL(5, ref CpuAddress[Zeropage()]);
                    break;
                case 0x36:
                    ROL(6, ref CpuAddress[ZeropageX()]);
                    break;
                case 0x2E:
                    ROL(6, ref CpuAddress[Absolute()]);
                    break;
                case 0x3E:
                    ROL(7, ref CpuAddress[AbsoluteX()]);
                    break;
                case 0x6A:
                    ROR(2, ref registerA);
                    programCounter++;
                    break;
                case 0x66:
                    ROR(5, ref CpuAddress[Zeropage()]);
                    break;
                case 0x76:
                    ROR(6, ref CpuAddress[ZeropageX()]);
                    break;
                case 0x6E:
                    ROR(6, ref CpuAddress[Absolute()]);
                    break;
                case 0x7E:
                    ROR(7, ref CpuAddress[AbsoluteX()]);
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
                case 0x6C:
                    JMP(5, CpuAddress[Absolute()]);
                    break;
                /*
                 * サブルーチンを呼び出す
                 * 元のPCを上位, 下位バイトの順にpushする
                 * この時保存するPCはJSRの最後のバイトアドレス
                 * JSR サイクル数6
                 */
                case 0x20:
                    ushort savePC = --programCounter;
                    Push((byte)(savePC >> 8));
                    Push((byte)((savePC << 8) >> 8));
                    programCounter = CpuAddress[Absolute()];
                    break;
                /*
                 * サブルーチンから復帰する
                 * 下位, 上位バイトの順にpopする
                 * RTS サイクル数6
                 */
                case 0x60:
                    programCounter = (ushort)(CpuAddress[0x0100 + registerS + 2] * 0x0100 + CpuAddress[0x0100 + registerS + 1]);
                    registerS += 2;
                    programCounter++;
                    break;
                case 0x40:
                    RTI();
                    break;
                case 0x90:
                    Branch(cFlag == 0);
                    break;
                case 0xB0:
                    Branch(cFlag == 1);
                    break;
                case 0xF0:
                    Branch(zFlag == 1);
                    break;
                case 0x30:
                    Branch(nFlag == 1);
                    break;
                case 0xD0:
                    Branch(zFlag == 0);
                    break;
                case 0x10:
                    Branch(nFlag == 0);
                    break;
                case 0x50:
                    Branch(vFlag == 0);
                    break;
                case 0x70:
                    Branch(vFlag == 1);
                    break;
                case 0x18:
                    ClearFlag(ref cFlag);
                    break;
                case 0x58:
                    ClearFlag(ref iFlag);
                    break;
                case 0xB8:
                    ClearFlag(ref vFlag);
                    break;
                case 0x38:
                    SetFlag(ref cFlag);
                    break;
                case 0x78:
                    SetFlag(ref iFlag);
                    break;
                /*
                 * ソフトウェア割り込みを起こす
                 * BRK サイクル数7
                 */
                case 0x00:
                    break;
                /*
                 * 空の命令を実効
                 * NOP サイクル数2
                 */
                case 0xEA:
                    programCounter++;
                    break;
            }
        }
    }
}