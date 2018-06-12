using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NES_Emulator.NES
{
	public abstract class Unit
    {
		protected Nes nes;
		protected string unitName;
        
		protected Unit(Nes nes)
		{
			this.nes = nes;
		}


		//public abstract void Execute();
    }
}
