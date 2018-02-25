using System;
using Xamarin.Forms;

namespace NES_Emulator.NES
{
    public static class GameScreen
    {
        public static BoxView[,] GameScreenPixel;

        public static void InitialGameScreen()
        {
            GameScreenPixel = new BoxView[256, 240];
            for (int i = 0; i < 256 ;i++)
            {
                for (int j = 0; j < 240; j++)
                {
                    GameScreenPixel[i, j] = new BoxView {BackgroundColor = Color.Red};
                }
            }
        }
    }
}
