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
       
		public void Run()
		{
			int count = 0;
			nes.ProcessInitialize(unitName);
			while(nes.DrawingFrame)
			{
				if (nes.SyncControll(count))
				{
					count++;
					Debug.WriteLine(unitName + count);
					Execute();
					nes.ProcessComplete(unitName, count);
				}
			}
		}

		public abstract void Execute();
    }
}
