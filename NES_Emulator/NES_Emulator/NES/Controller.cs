using System;
using System.Collections.Generic;
using System.Linq;

namespace NES_Emulator.NES
{
	public class Controller
	{
		Dictionary<ushort, int> controller;
		Dictionary<ushort, int> controllerWriteCount;
		Dictionary<ushort, int> controllerReadCount;
		Dictionary<ushort, bool[]> isControllerInput;

		public Controller()
		{
			controller = new Dictionary<ushort, int> { { 0x4016, 0 }, { 0x4017, 0 } };
			controllerWriteCount = new Dictionary<ushort, int> { { 0x4016, 0 }, { 0x4017, 0 } };
			controllerReadCount = new Dictionary<ushort, int> { { 0x4016, 0 }, { 0x4017, 0 } };
			isControllerInput = new Dictionary<ushort, bool[]> { { 0x4016, new bool[8] }, { 0x4017, new bool[8] } };
		}
        
        /// <summary>
        /// レジスタ読み取り
        /// </summary>
        /// <returns><c>true</c>, if io register was  read, <c>false</c> otherwise.</returns>
        /// <param name="address">Address.</param>
		public bool ReadIoRegister(ushort address)
		{
			bool input = isControllerInput[address][controllerReadCount[address]];
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
						isControllerInput[address].All(flag => false);
					}
					controllerWriteCount[address] = 0;
					break;
			}
		}

        /// <summary>
        /// Key入力
        /// </summary>
        /// <param name="player">1P or 2P</param>
        /// <param name="key">Key</param>
		public void InputKey(int player, string key)
		{
			ushort address = (ushort)(0x4015 + player);
			switch(key)
			{
				case "A":
					isControllerInput[address][0] = true;
					break;
				case "B":
					isControllerInput[address][1] = true;
					break;
				case "SELECT":
					isControllerInput[address][2] = true;
					break;
				case "START":
					isControllerInput[address][3] = true;
					break;
				case "UP":
					isControllerInput[address][4] = true;
					break;
				case "DOWN":
					isControllerInput[address][5] = true;
					break;
				case "LEFT":
					isControllerInput[address][6] = true;
					break;
				case "RIGHT":
					isControllerInput[address][7] = true;
					break;
			}
		}
    }
}
