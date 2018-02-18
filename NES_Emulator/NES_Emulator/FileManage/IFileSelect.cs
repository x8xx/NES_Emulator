using System;
namespace NES_Emulator.FileManage
{
    public interface IFileSelect
    {
        string GetText();
        void OpenDocumentBrowserView();
    }
}
