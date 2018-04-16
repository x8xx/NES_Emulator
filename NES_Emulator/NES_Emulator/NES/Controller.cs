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
		Dictionary<ushort, Input[]> isControllerInput; //コントローラー入力状態
		bool waitKey; //key入力待ち
        
		public Controller()
		{
			controller = new Dictionary<ushort, int> { { 0x4016, 0 }, { 0x4017, 0 } };
			controllerWriteCount = new Dictionary<ushort, int> { { 0x4016, 0 }, { 0x4017, 0 } };
			controllerReadCount = new Dictionary<ushort, int> { { 0x4016, 0 }, { 0x4017, 0 } };
			isControllerInput = new Dictionary<ushort, Input[]> { { 0x4016, new Input[8] }, { 0x4017, new Input[8] } };
			for (int i = 0; i < 8; i++)
			{
				isControllerInput[0x4016][i] = new Input() { isInput = false, isRead = true };
				isControllerInput[0x4017][i] = new Input() { isInput = false, isRead = true };
			}
			waitKey = false;
		}
        
        /// <summary>
        /// レジスタ読み取り
        /// </summary>
        /// <returns><c>true</c>, 入力状態, <c>false</c> 入力なし.</returns>
        /// <param name="address">Address.</param>
		public bool ReadIoRegister(ushort address)
		{
			bool input = isControllerInput[address][controllerReadCount[address]].isInput;
			isControllerInput[address][controllerReadCount[address]].isRead = true;
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
						for (int i = 0; i < isControllerInput[address].Length; i++)
						{
							if (!isControllerInput[address][i].isRead)
								break;
							Debug.WriteLine(isControllerInput[address][i].isInput);
							isControllerInput[address][i].isInput = false;
							isControllerInput[address][i].isRead = false;
						}
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
			if (!waitKey)
			{
				ushort address = (ushort)(0x4015 + player);
                switch (key)
                {
                    case "A":
                        isControllerInput[address][0].isInput = true;
						isControllerInput[address][0].isRead = false;
                        break;
                    case "B":
                        isControllerInput[address][1].isInput = true;
						isControllerInput[address][1].isRead = false;
                        break;
                    case "SELECT":
                        isControllerInput[address][2].isInput = true;
						isControllerInput[address][2].isRead = false;
                        break;
                    case "START":
                        isControllerInput[address][3].isInput = true;
						isControllerInput[address][3].isRead = false;
                        break;
                    case "UP":
                        isControllerInput[address][4].isInput = true;
						isControllerInput[address][4].isRead = false;
                        break;
                    case "DOWN":
                        isControllerInput[address][5].isInput = true;
						isControllerInput[address][5].isRead = false;
                        break;
                    case "LEFT":
                        isControllerInput[address][6].isInput = true;
						isControllerInput[address][6].isRead = false;
                        break;
                    case "RIGHT":
                        isControllerInput[address][7].isInput = true;
						isControllerInput[address][7].isRead = false;
                        break;
                }
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
