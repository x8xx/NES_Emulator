using System.Collections.Generic;

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
         * => 0x0100〜0x01FF 0x0100 スタック
         * 0x2000〜0x2007 0x0008 PPUレジスタ
         * 0x4000〜0x401F 0x0020 APU I/O, PAD
         * 0x4020〜0x5FFF 0x1FE0 拡張ROM
         * 0x6000〜0x7FFF 0x2000 拡張RAM
         * 0x8000〜0xBFFF 0x4000 PRG-ROM
         * 0xC000〜0xFFFF 0x4000 PRG-ROM
         */
        byte[] cpuAddress;
        int totalCpuCycle; //合計サイクル数

        Nes nes;
        public Cpu(Nes nes)
        {
            cpuAddress = new byte[0x10000];
            this.nes = nes;
            totalCpuCycle = 0;
        }

        public void WriteMemory(ushort address, byte value)
        {
            if (address >= 0x2000 && address <= 0x2007) nes.ppuRegister.WritePpuRegister(address, value);
            cpuAddress[address] = value;
        }

        public byte ReadMemory(ushort address)
        {
            return 0x00;
        }

        public void CycleInc(int cycle)
        {
            totalCpuCycle += cycle;
            nes.ppu.TotalPpuCycle += 3 * cycle;
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
            return cpuAddress[address];
        }

        /// <summary>
        /// [IM8 + X]
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort ZeropageX()
        {
            ushort address = ++programCounter;
            programCounter++;
            return (ushort)(cpuAddress[address] + registerX);
        }

        /// <summary>
        /// [IM8 + Y]
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort ZeropageY()
        {
            ushort address = ++programCounter;
            programCounter++;
            return (ushort)(cpuAddress[address] + registerY);
        }

        /// <summary>
        /// [IM16]
        /// 2番目のバイトを下位アドレスに, 3番目のバイトを上位アドレスにする
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort Absolute()
        {
            ushort address = (ushort)(cpuAddress[programCounter + 2] * 0x100 + cpuAddress[programCounter + 1]);
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
            ushort address = (ushort)(cpuAddress[programCounter + 2] * 0x100 + cpuAddress[programCounter + 1]);
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
            ushort address = (ushort)(cpuAddress[programCounter + 2] * 0x100 + cpuAddress[programCounter + 1]);
            programCounter += 3;
            return (ushort)(address + registerY);
        }

        /// <summary>
        /// [[IM8 + X]]
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort IndirectX()
        {
            return (ushort)cpuAddress[ZeropageX()];
        }

        /// <summary>
        /// [[IM8] + Y]
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort IndirectY()
        {
            return (ushort)cpuAddress[Zeropage() + registerY];
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
        void LoadToRegister(ushort address, ref byte register)
        {
            register = cpuAddress[address];
            FlagNandZ(register);
        }


        /// <summary>
        /// レジスタからメモリにストア
        /// STA, STX, STY
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="address">実効アドレス</param>
        /// <param name="register">レジスタ</param>
        void StoreToMemory(ushort address, ref byte register)
        {
            cpuAddress[address] = register;
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


        void ADC(ushort address)
        {
            registerA += (byte)(cpuAddress[address] + cFlag);
            FlagNandZ(registerA);
        }


        /// <summary>
        /// Aとメモリを論理AND演算
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void AND(ushort address)
        {
            registerA &= cpuAddress[address];
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
        void ASL(ref byte value)
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
        void BIT(ushort address)
        {
            nFlag = (byte)(cpuAddress[address] >> 7);
            vFlag = (byte)((cpuAddress[address] << 1) >> 7);
            if ((registerA & cpuAddress[address]) == 0) zFlag = 1;
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
        void Comparison(ushort address, ref byte register)
        {
            byte tmp = (byte)(register - cpuAddress[address]);
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
        void DEC(ushort address)
        {
            cpuAddress[address]--;
            FlagNandZ(cpuAddress[address]);
        }

        /// <summary>
        /// Aとメモリを論理XOR演算
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void EOR(ushort address)
        {
            registerA ^= cpuAddress[address];
            FlagNandZ(registerA);
        }

        /// <summary>
        /// メモリをインクリメント
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="address">実効アドレス</param>
        void INC(ushort address)
        {
            cpuAddress[address]++;
            FlagNandZ(cpuAddress[address]);
        }

        /// <summary>
        /// メモリを1回右シフト
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        /// <param name="cycle">サイクル数.</param>
        /// <param name="address">実効アドレス</param>
        void LSR(ref byte value)
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
        void ORA(ushort address)
        {
            registerA |= cpuAddress[address];
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
        void ROL(ref byte value)
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
        void ROR(ref byte value)
        {
            byte tmp = (byte)((value << 7) >> 7);
            value = (byte)((value >> 1) + nFlag * 0x80);
            cFlag = tmp;
            FlagNandZ(value);
        }

        void SBC(ushort address)
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
            cpuAddress[0x0100 + registerS] = value;
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
            registerA = cpuAddress[0x0100 + registerS];
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
            SetRegisterP(cpuAddress[0x0100 + registerS]);
        }

        /// <summary>
        /// アドレスへジャンプする
        /// </summary>
        /// <param name="cycle">サイクル数</param>
        /// <param name="address">実効アドレス</param>
        void JMP(ushort address)
        {
            programCounter = cpuAddress[address];
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
                byte tmp = cpuAddress[programCounter + 1];
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
                    LoadToRegister(Immediate(), ref registerA);
                    break;
                case 0xA5:
                    LoadToRegister(Zeropage(), ref registerA);
                    break;
                case 0xB5:
                    LoadToRegister(ZeropageX(), ref registerA);
                    break;
                case 0xAD:
                    LoadToRegister(Absolute(), ref registerA);
                    break;
                case 0xBD:
                    LoadToRegister(AbsoluteX(), ref registerA);
                    break;
                case 0xB9:
                    LoadToRegister(AbsoluteY(), ref registerA);
                    break;
                case 0xA1:
                    LoadToRegister(IndirectX(), ref registerA);
                    break;
                case 0xB1:
                    LoadToRegister(IndirectY(), ref registerA);
                    break;
                case 0xA2:
                    LoadToRegister(Immediate(), ref registerX);
                    break;
                case 0xA6:
                    LoadToRegister(Zeropage(), ref registerX);
                    break;
                case 0xB6:
                    LoadToRegister(ZeropageY(), ref registerX);
                    break;
                case 0xAE:
                    LoadToRegister(Absolute(), ref registerX);
                    break;
                case 0xBE:
                    LoadToRegister(AbsoluteY(), ref registerX);
                    break;
                case 0xA0:
                    LoadToRegister(Immediate(), ref registerY);
                    break;
                case 0xA4:
                    LoadToRegister(Zeropage(), ref registerY);
                    break;
                case 0xB4:
                    LoadToRegister(ZeropageX(), ref registerY);
                    break;
                case 0xAC:
                    LoadToRegister(Absolute(), ref registerY);
                    break;
                case 0xBC:
                    LoadToRegister(AbsoluteX(), ref registerY);
                    break;
                case 0x85:
                    StoreToMemory(Zeropage(), ref registerA);
                    break;
                case 0x95:
                    StoreToMemory(ZeropageX(), ref registerA);
                    break;
                case 0x8D:
                    StoreToMemory(Absolute(), ref registerA);
                    break;
                case 0x9D:
                    StoreToMemory(AbsoluteX(), ref registerA);
                    break;
                case 0x99:
                    StoreToMemory(AbsoluteY(), ref registerA);
                    break;
                case 0x81:
                    StoreToMemory(IndirectX(), ref registerA);
                    break;
                case 0x91:
                    StoreToMemory(IndirectY(), ref registerA);
                    break;
                case 0x86:
                    StoreToMemory(Zeropage(), ref registerX);
                    break;
                case 0x96:
                    StoreToMemory(ZeropageY(), ref registerX);
                    break;
                case 0x8E:
                    StoreToMemory(Absolute(), ref registerX);
                    break;
                case 0x84:
                    StoreToMemory(Zeropage(), ref registerY);
                    break;
                case 0x94:
                    StoreToMemory(ZeropageX(), ref registerY);
                    break;
                case 0x8C:
                    StoreToMemory(Absolute(), ref registerY);
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
                    ADC(Immediate());
                    break;
                case 0x65:
                    ADC(Zeropage());
                    break;
                case 0x75:
                    ADC(ZeropageX());
                    break;
                case 0x6D:
                    ADC(Absolute());
                    break;
                case 0x7D:
                    ADC(AbsoluteX());
                    break;
                case 0x79:
                    ADC(AbsoluteY());
                    break;
                case 0x61:
                    ADC(IndirectX());
                    break;
                case 0x71:
                    ADC(IndirectY());
                    break;
                case 0x29:
                    AND(Immediate());
                    break;
                case 0x25:
                    AND(Zeropage());
                    break;
                case 0x35:
                    AND(ZeropageX());
                    break;
                case 0x2D:
                    AND(Absolute());
                    break;
                case 0x3D:
                    AND(AbsoluteX());
                    break;
                case 0x39:
                    AND(AbsoluteY());
                    break;
                case 0x21:
                    AND(IndirectX());
                    break;
                case 0x31:
                    AND(IndirectY());
                    break;
                case 0x0A:
                    ASL(ref registerA);
                    programCounter++;
                    break;
                case 0x06:
                    ASL(ref cpuAddress[Zeropage()]);
                    break;
                case 0x16:
                    ASL(ref cpuAddress[ZeropageX()]);
                    break;
                case 0x0E:
                    ASL(ref cpuAddress[Absolute()]);
                    break;
                case 0x1E:
                    ASL(ref cpuAddress[AbsoluteX()]);
                    break;
                case 0x24:
                    BIT(Zeropage());
                    break;
                case 0x2C:
                    BIT(Absolute());
                    break;
                case 0xC9:
                    Comparison(Immediate(), ref registerA);
                    break;
                case 0xC5:
                    Comparison(Zeropage(), ref registerA);
                    break;
                case 0xD5:
                    Comparison(ZeropageX(), ref registerA);
                    break;
                case 0xCD:
                    Comparison(Absolute(), ref registerA);
                    break;
                case 0xDD:
                    Comparison(AbsoluteX(), ref registerA);
                    break;
                case 0xD9:
                    Comparison(AbsoluteY(), ref registerA);
                    break;
                case 0xC1:
                    Comparison(IndirectX(), ref registerA);
                    break;
                case 0xD1:
                    Comparison(IndirectY(), ref registerA);
                    break;
                case 0xE0:
                    Comparison(Immediate(), ref registerX);
                    break;
                case 0xE4:
                    Comparison(Zeropage(), ref registerX);
                    break;
                case 0xEC:
                    Comparison(Absolute(), ref registerX);
                    break;
                case 0xC0:
                    Comparison(Immediate(), ref registerY);
                    break;
                case 0xC4:
                    Comparison(Zeropage(), ref registerY);
                    break;
                case 0xCC:
                    Comparison(Absolute(), ref registerY);
                    break;
                case 0xC6:
                    DEC(Zeropage());
                    break;
                case 0xD6:
                    DEC(ZeropageX());
                    break;
                case 0xCE:
                    DEC(Absolute());
                    break;
                case 0xDE:
                    DEC(AbsoluteX());
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
                    EOR(Immediate());
                    break;
                case 0x45:
                    EOR(Zeropage());
                    break;
                case 0x55:
                    EOR(ZeropageX());
                    break;
                case 0x4D:
                    EOR(Absolute());
                    break;
                case 0x5D:
                    EOR(AbsoluteX());
                    break;
                case 0x59:
                    EOR(AbsoluteY());
                    break;
                case 0x41:
                    EOR(IndirectX());
                    break;
                case 0x51:
                    EOR(IndirectY());
                    break;
                case 0xE6:
                    INC(Zeropage());
                    break;
                case 0xF6:
                    INC(ZeropageX());
                    break;
                case 0xEE:
                    INC(Absolute());
                    break;
                case 0xFE:
                    INC(AbsoluteX());
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
                    LSR(ref registerA);
                    programCounter++;
                    break;
                case 0x46:
                    LSR(ref cpuAddress[Zeropage()]);
                    break;
                case 0x56:
                    LSR(ref cpuAddress[ZeropageX()]);
                    break;
                case 0x4E:
                    LSR(ref cpuAddress[Absolute()]);
                    break;
                case 0x5E:
                    LSR(ref cpuAddress[AbsoluteX()]);
                    break;
                case 0x09:
                    ORA(Immediate());
                    break;
                case 0x05:
                    ORA(Zeropage());
                    break;
                case 0x15:
                    ORA(ZeropageX());
                    break;
                case 0x0D:
                    ORA(Absolute());
                    break;
                case 0x1D:
                    ORA(AbsoluteX());
                    break;
                case 0x19:
                    ORA(AbsoluteY());
                    break;
                case 0x01:
                    ORA(IndirectX());
                    break;
                case 0x11:
                    ORA(IndirectY());
                    break;
                case 0x2A:
                    ROL(ref registerA);
                    programCounter++;
                    break;
                case 0x26:
                    ROL(ref cpuAddress[Zeropage()]);
                    break;
                case 0x36:
                    ROL(ref cpuAddress[ZeropageX()]);
                    break;
                case 0x2E:
                    ROL(ref cpuAddress[Absolute()]);
                    break;
                case 0x3E:
                    ROL(ref cpuAddress[AbsoluteX()]);
                    break;
                case 0x6A:
                    ROR(ref registerA);
                    programCounter++;
                    break;
                case 0x66:
                    ROR(ref cpuAddress[Zeropage()]);
                    break;
                case 0x76:
                    ROR(ref cpuAddress[ZeropageX()]);
                    break;
                case 0x6E:
                    ROR(ref cpuAddress[Absolute()]);
                    break;
                case 0x7E:
                    ROR(ref cpuAddress[AbsoluteX()]);
                    break;
                case 0xE9:
                    SBC(Immediate());
                    break;
                case 0xE5:
                    SBC(Zeropage());
                    break;
                case 0xF5:
                    SBC(ZeropageX());
                    break;
                case 0xED:
                    SBC(Absolute());
                    break;
                case 0xFD:
                    SBC(AbsoluteX());
                    break;
                case 0xF9:
                    SBC(AbsoluteY());
                    break;
                case 0xE1:
                    SBC(IndirectX());
                    break;
                case 0xF1:
                    SBC(IndirectY());
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
                    JMP(Absolute());
                    break;
                case 0x6C:
                    JMP(cpuAddress[Absolute()]);
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
                    programCounter = cpuAddress[Absolute()];
                    break;
                /*
                 * サブルーチンから復帰する
                 * 下位, 上位バイトの順にpopする
                 * RTS サイクル数6
                 */
                case 0x60:
                    programCounter = (ushort)(cpuAddress[0x0100 + registerS + 2] * 0x0100 + cpuAddress[0x0100 + registerS + 1]);
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
            CycleInc(Nes.cpuCycle[opcode]);
        }
    }
}
