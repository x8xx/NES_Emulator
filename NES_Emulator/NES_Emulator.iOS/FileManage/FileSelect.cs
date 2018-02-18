using System;
using NES_Emulator.FileManage;
using UIKit;
using CloudKit;
using Foundation;

namespace NES_Emulator.iOS.FileManage
{
    public class FileSelect : IFileSelect
    {
        public string GetText()
        {
            return "test";
        }

        public void OpenDocumentBrowserView()
        {
            var nsmdq = new NSMetadataQuery();

        }

        void DidPickDocument(object sender, UIDocumentPickedEventArgs e)
        {
            
        }

        void WasCancelled(object sender, EventArgs e)
        {
            
        }

    }
}
