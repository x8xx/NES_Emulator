using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace NES_Emulator.NES
{
	public class Controller
	{
		Dictionary<ushort, int> controller; //IOレジスタ
		Dictionary<ushort, int> controllerWriteCount; //IOレジスタ書き込み回数
		Dictionary<ushort, int> controllerReadCount; //IOレジスタ読み込み回数
		Dictionary<ushort, bool[]> isControllerInput; //コントローラー入力状態
		bool waitKey; //key入力待ち
        
		public Controller()
		{
			controller = new Dictionary<ushort, int> { { 0x4016, 0 }, { 0x4017, 0 } };
			controllerWriteCount = new Dictionary<ushort, int> { { 0x4016, 0 }, { 0x4017, 0 } };
			controllerReadCount = new Dictionary<ushort, int> { { 0x4016, 0 }, { 0x4017, 0 } };
			isControllerInput = new Dictionary<ushort, bool[]> { { 0x4016, new bool[8] }, { 0x4017, new bool[8] } };
			waitKey = false;
		}
        
        /// <summary>
        /// レジスタ読み取り
        /// </summary>
        /// <returns><c>true</c>, 入力状態, <c>false</c> 入力なし.</returns>
        /// <param name="address">Address.</param>
		public bool ReadIoRegister(ushort address)
		{
			bool input = isControllerInput[address][controllerReadCount[address]];
			isControllerInput[address][controllerReadCount[address]] = false;
			if (input)
				waitKey = false;
			controllerReadCount[address]++;
			if (controllerReadCount[address] > 7)
				controllerReadCount[address] = 0;
			return input;
        }
        
        /// <summary>
        /// 0x4016, 0x4017に書き込み
        /// </summary>
		/// <param name="address">アドレス</param>
        /// <param name="value">値</param>
		public void WriteIoRegister(ushort address, byte value)
		{
			switch(controllerWriteCount[address])
			{
				case 0:
					controller[address] = value;
					controllerWriteCount[address]++;
					break;
				case 1:
					if (controller[address] == 1 && value == 0)
					{
						controllerReadCount[address] = 0;
						for (int i = 0; i < isControllerInput[address].Length; i++)
						{
							if (isControllerInput[address][i])
								continue;
							isControllerInput[address][i] = false;
						}
					}
					controllerWriteCount[address] = 0;
					break;
			}
		}
        
        /// <summary>
        /// Key入力
		/// 0, A
		/// 1, B
		/// 2, SELECT
		/// 3, START
		/// 4, UP
		/// 5, DOWN
		/// 6, LEFT
		/// 7, RIGHT
        /// </summary>
        /// <param name="player">1P or 2P</param>
        /// <param name="key">Key</param>
		public void InputKey(int player, int key)
		{
			if (!waitKey)
			{
				ushort address = (ushort)(0x4015 + player);
				isControllerInput[address][key] = true;
                waitKey = true;
			}
		}

		class Input
		{
			public bool isInput { get; set; }
			public bool isRead { get; set; }
		}
    }
}
