using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

namespace NES_Emulator.NES
{
	public class Cpu : Unit
    {
        //レジスタ
        byte registerA; //8bit
        byte registerX; //8bit
        byte registerY; //8bit
        byte registerS; //8bit
        ushort programCounter; //16bit

		readonly ushort INITIAL_PROGRAM_COUNTER;

        //フラグ
        bool nFlag; //演算結果のbit7の値
        bool vFlag; //演算結果がオーバーフローを起こしたらセット
        bool bFlag; //BRK発生時にセット, IRQ発生時にクリア
        bool iFlag; //false : IRQ許可, true : IRQ禁止
        bool zFlag; //演算結果が0のときにセット
        bool cFlag; //キャリー発生時にセット

        /* CPUメモリマップ
         * アドレス        サイズ
         * 0x0000〜0x07FF 0x0800 WRAM
         * => 0x0000〜0x00FF ゼロページ
         * => 0x0100〜0x01FF 0x0100 スタック
         * 0x2000〜0x2007 0x0008 PPUレジスタ
         * 0x4000〜0x401F 0x0020 APU I/O, PAD
         * 0x4020〜0x5FFF 0x1FE0 拡張ROM
         * 0x6000〜0x7FFF 0x2000 拡張RAM
         * 0x8000〜0xBFFF 0x4000 PRG-ROM
         * 0xC000〜0xFFFF 0x4000 PRG-ROM
         */
        byte[] cpuAddress;
        
		public Cpu(Nes nes, int programRomSize) : base(nes)
        {
			unitName = GetType().Name;
            cpuAddress = new byte[0x10000];
			INITIAL_PROGRAM_COUNTER = (ushort)(0x10000 - programRomSize);
			programCounter = 0x8000; //PC初期化
            //フラグ初期化
            nFlag = false;
            vFlag = false;
            bFlag = false;
            iFlag = false;
            zFlag = false;
            cFlag = false;
            registerS = 0xff;
            this.nes = nes;
        }


        public void DebugWriteValue(int count)
        {
            Debug.WriteLine(cpuAddress[0]);
			if (false)
			{
				Debug.WriteLine(count + "=>");
				Debug.WriteLine("A: {0}, X: {1}, Y: {2}, PC: {3}", Convert.ToString(registerA, 16), Convert.ToString(registerX, 16), Convert.ToString(registerY, 16), Convert.ToString(programCounter, 16));
				Debug.WriteLine("N: {0}, V: {1}, B: {2}, I: {3}, Z: {4}, C: {5}", nFlag, vFlag, bFlag, iFlag, zFlag, cFlag);
			}
        }

        public void CheckIrq()
        {
            if (!iFlag)
            {
                
            }
        }


        /// <summary>
        /// アドレスに書き込み
        /// </summary>
        /// <param name="address">アドレス</param>
        /// <param name="value">値</param>
        public void WriteMemory(ushort address, byte value)
        {
            if (address >= 0x2000 && address <= 0x401F)
                nes.WriteIoRegister(address, value);
            cpuAddress[address] = value;
        }


        /// <summary>
        /// アドレスの値を読み込み
        /// </summary>
        /// <returns>値</returns>
        /// <param name="address">アドレス</param>
        public byte ReadMemory(ushort address)
        {
            if (address == 0x2002 || address == 0x2007 || address == 0x4016 || address == 0x4017)
                return nes.ReadIoRegister(address);
            return cpuAddress[address];
        }

        /// <summary>
        /// Pのゲッター
        /// </summary>
        /// <returns>P</returns>
        byte GetRegisterP()
        {
            return (byte)(Convert.ToInt32(nFlag) * 0x80 + 
                          Convert.ToInt32(vFlag) * 0x40 + 0x20 +
                          Convert.ToInt32(bFlag) * 0x10 + 
                          Convert.ToInt32(iFlag) * 0x04 + 
                          Convert.ToInt32(zFlag) * 0x02 + 
                          Convert.ToInt32(cFlag));
        }

        /// <summary>
        /// Pのセッター各bitをフラグに格納
        /// </summary>
        /// <param name="value">P</param>
        void SetRegisterP(byte value)
        {
            nFlag = Nes.FetchBit(value, 7) != 0;
            vFlag = Nes.FetchBit(value, 6) != 0;
            bFlag = Nes.FetchBit(value, 4) != 0;
            iFlag = Nes.FetchBit(value, 2) != 0;
            zFlag = Nes.FetchBit(value, 1) != 0;
            cFlag = Nes.FetchBit(value, 0) != 0;
        }

        int c;
        /// <summary>
        /// NMI割り込み
        /// 上位バイト下位バイトレジスタPの順にPush
        /// </summary>
        public void Nmi()
        {
            /*c++;
            if (c == 23)
                nes.ppu.ShowTest();
            /*for (int i = 0; i < 10; i++)
                Debug.Write(Convert.ToString(cpuAddress[i], 16) + ", ");
            Debug.WriteLine(Convert.ToString(registerA, 16) + ", " + Convert.ToString(registerX, 16) + ", " + Convert.ToString(registerY, 16));
            Debug.WriteLine("-------------------------------------------------------------------------------------------");*/
            Push((byte)(programCounter >> 8));
            Push((byte)((programCounter << 8) >> 8));
            Push(GetRegisterP());
            programCounter = (ushort)(ReadMemory(0xFFFB) * 0x100 + ReadMemory(0xFFFA));
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
            //Debug.WriteLine("[{0}]", address);
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
            //Debug.WriteLine("[{0}]", cpuAddress[address]);
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
            //Debug.WriteLine("[{0}]", cpuAddress[address] + registerX);
            return (byte)(cpuAddress[address] + registerX);
        }

        /// <summary>
        /// [IM8 + Y]
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort ZeropageY()
        {
            ushort address = ++programCounter;
            programCounter++;
            //Debug.WriteLine("[{0}]", cpuAddress[address] + registerY);
            return (byte)(cpuAddress[address] + registerY);
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
            //Debug.WriteLine("[{0}]", address);
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
            //Debug.WriteLine("[{0}]", address + registerX);
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
            //Debug.WriteLine("[{0}]", address + registerY);
            return (ushort)(address + registerY);
        }

        /// <summary>
        /// [[IM8 + X]]
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort IndirectX()
        {
            byte tmp = (byte)(ReadMemory(programCounter) + registerX);
            programCounter++;
            ushort address = ReadMemory(tmp);
            tmp++;
            address |= (ushort)(ReadMemory(tmp) << 8);
            return address;
        }

        /// <summary>
        /// [[IM8] + Y]
        /// </summary>
        /// <returns>実効アドレス</returns>
        ushort IndirectY()
        {
            //Debug.WriteLine("[{0}]", ReadMemory((ushort)(ReadMemory((ushort)(programCounter + 1)) + 1)) * 0x100 + ReadMemory(ReadMemory((ushort)(programCounter + 1))) + registerY);
            ushort address = (ushort)(ReadMemory((ushort)(ReadMemory((ushort)(programCounter + 1)) + 1)) * 0x100 + ReadMemory(ReadMemory((ushort)(programCounter + 1))) + registerY);
            programCounter += 2;
            return address;
            /*
            byte tmp = ReadMemory(programCounter);
            programCounter++;
            ushort tmp2 = ReadMemory(tmp);
            tmp++;
            tmp2 |= (ushort)(ReadMemory(tmp) << 8);
            Debug.WriteLine(tmp2 + registerY);
            return (ushort)(tmp2 + registerY);*/
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
        /// <param name="address">実効アドレス</param>
        /// <param name="register">レジスタ</param>
        void LoadToRegister(ushort address, ref byte register)
        {
            register = ReadMemory(address);
            FlagNandZ(register);
        }

        /// <summary>
        /// XをSへコピー
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

        /// <summary>
        /// 加算
        /// A = A + M + C
        /// N: 結果の最上位ビット
        /// V: 符号付きオーバーフローが発生したか
        /// Z: 結果が0であるか
        /// C: 繰り上がりが発生したら 1, さもなくば 0
        /// </summary>
        /// <param name="address">実効アドレス</param>
        void ADC(ushort address)
        {
            int sum = registerA + ReadMemory(address) + Convert.ToInt32(cFlag);
            registerA = (byte)sum;
            FlagNandZ(registerA);
            vFlag = ((((registerA ^ ReadMemory(address)) & 0x80) != 0) && (((registerA ^ sum) & 0x80)) != 0);
            cFlag = sum > 0xff;
        }


        /// <summary>
        /// Aとメモリを論理AND演算
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="address">実効アドレス</param>
        void AND(ushort address)
        {
            registerA &= ReadMemory(address);
            FlagNandZ(registerA);
        }

        /// <summary>
        /// メモリを1回左シフト
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        void ASL(ref byte value)
        {
            cFlag = (value >> 7) == 1;
            value <<= 1;
            FlagNandZ(value);
        }

        /// <summary>
        /// bitテストを行う
        /// N: メモリのbit7
        /// V: メモリのbit6
        /// Z: 結果が0であるか
        /// </summary>
        /// <param name="address">実効アドレス</param>
        void BIT(ushort address)
        {
            byte value = ReadMemory(address);
            nFlag = Nes.FetchBit(value, 7) != 0;
            vFlag = Nes.FetchBit(value, 6) != 0;
            zFlag = (registerA & value) == 0;
        }

        /// <summary>
        /// 演算の結果によって, フラグをセット(レジスタ - メモリ)
        /// N: 減算結果の最上位ビット
        /// Z: 減算結果が0であるか
        /// C: 減算結果が正かゼロのときセット, 負のときクリア
        /// CMP, CPX, CPY
        /// </summary>
        /// <param name="address">実効アドレス</param>
        void Comparison(ushort address, ref byte register)
		{
            int tmp = register - ReadMemory(address);
            FlagNandZ(tmp);
            cFlag = tmp >= 0;
        }

        /// <summary>
        /// メモリをデクリメント
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="address">実効アドレス</param>
        void DEC(ushort address)
        {
            cpuAddress[address]--;
            FlagNandZ(ReadMemory(address));
        }

        /// <summary>
        /// Aとメモリを論理XOR演算
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="address">実効アドレス</param>
        void EOR(ushort address)
        {
            registerA ^= ReadMemory(address);
            FlagNandZ(registerA);
        }

        /// <summary>
        /// メモリをインクリメント
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="address">実効アドレス</param>
        void INC(ushort address)
        {
            cpuAddress[address]++;
            FlagNandZ(ReadMemory(address));
        }

        /// <summary>
        /// メモリを1回右シフト
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        void LSR(ref byte value)
        {
            cFlag = Nes.FetchBit(value, 0) != 0;
            value >>= 1;
            FlagNandZ(value);
        }

        /// <summary>
        /// Aとメモリを論理OR演算
        /// N: 演算結果の最上位ビット
        /// Z: 演算結果が0であるか
        /// </summary>
        /// <param name="address">実効アドレス</param>
        void ORA(ushort address)
        {
            registerA |= ReadMemory(address);
            FlagNandZ(registerA);
        }

        /// <summary>
        /// メモリを1回左ローテート
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        void ROL(ref byte value)
        {
            byte tmp = Nes.FetchBit(value, 7);
            value = (byte)((value << 1) + Convert.ToInt32(cFlag));
            cFlag = tmp != 0;
            FlagNandZ(value);
        }

        /// <summary>
        /// メモリを1回右ローテート
        /// N: 結果の最上位ビット
        /// Z: 結果が0であるか
        /// C: はみ出たビット
        /// </summary>
        void ROR(ref byte value)
        {
            byte tmp = Nes.FetchBit(value, 0);
            value = (byte)((value >> 1) + Convert.ToInt32(cFlag) * 0x80);
            cFlag = tmp != 0;
            FlagNandZ(value);
        }

        /// <summary>
        /// 減算
        /// A = A - M - !C
        /// N: 結果の最上位ビット
        /// V: 符号付きオーバーフローが発生したか
        /// Z: 結果が0であるか
        /// C: 繰り下がりが発生したら 0, さもなくば 1
        /// </summary>
        /// <param name="address">実効アドレス</param>
        void SBC(ushort address)
        {
            cFlag = registerA >= ReadMemory(address) + (Convert.ToInt32(cFlag) ^ 1);
            byte sub = (byte)(registerA - ReadMemory(address) - (Convert.ToInt32(cFlag) ^ 1));
            if (registerA < ReadMemory(address) + (Convert.ToInt32(cFlag) ^ 1))
                sub++;
            registerA = sub;
            FlagNandZ(registerA);
            vFlag = (!(((registerA ^ ReadMemory(address)) & 0x80) != 0) && (((registerA ^ sub) & 0x80)) != 0);
            //cFlag = sub >= 0;
        }

        /// <summary>
        /// スタックにプッシュダウン
        /// PHA, PHP
        /// </summary>
        /// <param name="value">pushする値</param>
        void Push(byte value)
        {
            WriteMemory((ushort)(0x100 + registerS), value);
            registerS--;
        }

        /// <summary>
        /// アドレスへジャンプする
        /// </summary>
        /// <param name="address">実効アドレス</param>
        void JMP(ushort address)
        {
            programCounter = address;
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
                programCounter = (ushort)(address + programCounter + 2);
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
        void FlagNandZ(int value)
        {
            zFlag = value == 0;
            nFlag = Nes.FetchBit(value, 7) != 0;
        }

        /// <summary>
        /// フラグクリア
        /// </summary>
        /// <param name="flag">Flag.</param>
        void ClearFlag(ref bool flag)
        {
            flag = false;
            programCounter++;
        }

        /// <summary>
        /// フラグセット
        /// </summary>
        /// <param name="flag">Flag.</param>
        void SetFlag(ref bool flag)
        {
            flag = true;
            programCounter++;
        }

        /*------------------------------------------------------------
         * 命令セットここまで
         -------------------------------------------------------------*/

        /// <summary>
        /// 命令を実効
        /// </summary>
		public int Execute()
        {
            //Debug.WriteLine(Convert.ToString(programCounter, 16));
            byte opcode = cpuAddress[programCounter];
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
                    WriteMemory(Zeropage(), registerA);
                    break;
                case 0x95:
                    WriteMemory(ZeropageX(), registerA);
                    break;
                case 0x8D:
                    WriteMemory(Absolute(), registerA);
                    break;
                case 0x9D:
                    WriteMemory(AbsoluteX(), registerA);
                    break;
                case 0x99:
                    WriteMemory(AbsoluteY(), registerA);
                    break;
                case 0x81:
                    WriteMemory(IndirectX(), registerA);
                    break;
                case 0x91:
                    WriteMemory(IndirectY(), registerA);
                    break;
                case 0x86:
                    WriteMemory(Zeropage(), registerX);
                    break;
                case 0x96:
                    WriteMemory(ZeropageY(), registerX);
                    break;
                case 0x8E:
                    WriteMemory(Absolute(), registerX);
                    break;
                case 0x84:
                    WriteMemory(Zeropage(), registerY);
                    break;
                case 0x94:
                    WriteMemory(ZeropageX(), registerY);
                    break;
                case 0x8C:
                    WriteMemory(Absolute(), registerY);
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
                 */
                case 0x88:
                    programCounter++;
                    registerY--;
                    FlagNandZ(registerY);
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
                    programCounter++;
                    break;
                case 0x08:
                    Push(GetRegisterP());
                    programCounter++;
                    break;
                /*
                 * PLA
                 * スタックからAにポップアップ
                 * N: POPした値の最上位ビット
                 * Z: POPした値が0であるか
                 */
                case 0x68:
                    programCounter++;
                    registerS++;
                    registerA = cpuAddress[0x0100 + registerS];
                    FlagNandZ(registerA);
                    break;
                /*
                 * スタックからPにポップアップ
                 */
                case 0x28:
                    programCounter++;
                    registerS++;
                    SetRegisterP(cpuAddress[0x0100 + registerS]);
                    break;
                case 0x4C:
                    JMP(Absolute());
                    break;
                case 0x6C:
                    JMP((ushort)(ReadMemory((ushort)(ReadMemory((ushort)(programCounter + 1)) + 1)) * 0x100 + ReadMemory(ReadMemory((ushort)(programCounter + 1)))));
                    break;
                /*
                 * サブルーチンを呼び出す
                 * 元のPCを上位, 下位バイトの順にpushする
                 * この時保存するPCはJSRの最後のバイトアドレス
                 * JSR
                 */
                case 0x20:
                    ushort savePC = (ushort)(programCounter + 2);
                    Push((byte)(savePC >> 8));
                    Push((byte)((savePC << 8) >> 8));
                    programCounter = Absolute();
                    break;
                /*
                 * サブルーチンから復帰する
                 * 下位, 上位バイトの順にpopする
                 * RTS
                 */
                case 0x60:
                    programCounter = (ushort)(cpuAddress[0x0100 + registerS + 2] * 0x0100 + cpuAddress[0x0100 + registerS + 1]);
                    registerS += 2;
                    programCounter++;
                    break;
                /*
                 * 割り込みハンドラから復帰
                 * RTI
                 */
                case 0x40:
                    registerS++;
                    SetRegisterP(cpuAddress[0x0100 + registerS]);
                    programCounter = (ushort)(ReadMemory((ushort)(0x100 + registerS + 2)) * 0x100 + ReadMemory((ushort)(0x100 + registerS + 1)));
                    registerS += 2;
                    break;
                case 0x90:
                    Branch(!cFlag);
                    break;
                case 0xB0:
                    Branch(cFlag);
                    break;
                case 0xF0:
                    Branch(zFlag);
                    break;
                case 0x30:
                    Branch(nFlag);
                    break;
                case 0xD0:
                    Branch(!zFlag);
                    break;
                case 0x10:
                    Branch(!nFlag);
                    break;
                case 0x50:
                    Branch(!vFlag);
                    break;
                case 0x70:
                    Branch(vFlag);
                    break;
                case 0x18:
                    ClearFlag(ref cFlag);
                    break;
                case 0xD8:
                    programCounter++;
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
                 * BRK
                 */
                case 0x00:
                    Push((byte)(programCounter >> 8));
                    Push((byte)((programCounter << 8) >> 8));
                    Push(GetRegisterP());
                    programCounter = (ushort)(ReadMemory(0xFFFF) * 0x100 + ReadMemory(0xFFFE));
                    break;
                /*
                 * 空の命令を実効
                 * NOP
                 */
                case 0xEA:
                    programCounter++;
                    break;
            }
			return cpuCycle[opcode];
        }

		//CPUサイクル数
		static int[] cpuCycle =
        {
            /*0x00*/ 7, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 4, 4, 6, 6,
            /*0x10*/ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
            /*0x20*/ 6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 4, 4, 6, 6,
            /*0x30*/ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
            /*0x40*/ 6, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 3, 4, 6, 6,
            /*0x50*/ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
            /*0x60*/ 6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 5, 4, 6, 6,
            /*0x70*/ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
            /*0x80*/ 2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
            /*0x90*/ 2, 6, 2, 6, 4, 4, 4, 4, 2, 4, 2, 5, 5, 4, 5, 5,
            /*0xA0*/ 2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
            /*0xB0*/ 2, 5, 2, 5, 4, 4, 4, 4, 2, 4, 2, 4, 4, 4, 4, 4,
            /*0xC0*/ 2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
            /*0xD0*/ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
            /*0xE0*/ 2, 6, 3, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
            /*0xF0*/ 2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        };
    }
}
