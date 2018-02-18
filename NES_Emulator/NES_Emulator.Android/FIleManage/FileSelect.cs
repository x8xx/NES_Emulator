using System;
using NES_Emulator.FileManage;

namespace NES_Emulator.Droid.FileManage
{
    public class FileSelect : IFileSelect
    {
        public string GetText()
        {
            return "Hello Android";
        }

        public void OpenDocumentBrowserView()
        {
            
        }

    }
}
