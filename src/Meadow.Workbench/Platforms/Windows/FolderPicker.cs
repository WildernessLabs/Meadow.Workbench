using WindowsFolderPicker = Windows.Storage.Pickers.FolderPicker;

namespace Meadow.Workbench.Platforms.Windows
{
    public class FolderPicker : IFolderPicker
    {
        public async Task<string> PickFolder(string? startLocation)
        {
            var folderPicker = new WindowsFolderPicker();
            // Might be needed to make it work on Windows 10
            folderPicker.FileTypeFilter.Add("*");

            // Get the current window's HWND by passing in the Window object
            var hwnd = ((MauiWinUIWindow)App.Current.Windows[0].Handler.PlatformView).WindowHandle;

            // Associate the HWND with the file picker
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            //            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

            var result = await folderPicker.PickSingleFolderAsync();

            return result?.Path;
        }
    }
}