using System;
using Xamarin.Forms;

namespace NES_Emulator.Controllers
{
    public class LongTapButton : Button
    {
        public event EventHandler LongTap;

        public LongTapButton()
        {
        }

        public void OnLongTap()
        {
            if (LongTap != null)
            {
                LongTap(this, new EventArgs());
            }
        }
    }
}
