using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Meadow.Workbench.ViewModels;

public partial class DeviceInfoViewModel : ViewModelBase
{
    private FileDetails? _remoteSelected;
    private ObservableCollection<FileDetails> _remoteFiles = new();

    public class FileDetails
    {
        public string FileName { get; set; }
        public string FileDate { get; set; } = "1/1/2001";
        public string FileSize { get; set; } = "123456";
    }

    public FileDetails? RemoteSelectedFile
    {
        get => _remoteSelected;
        set => this.RaiseAndSetIfChanged(ref _remoteSelected, value);
    }

    public ObservableCollection<FileDetails> RemoteFiles
    {
        get => _remoteFiles;
    }

    public ICommand RefreshRemoteFilesCommand
    {
        get => new Command(() =>
        {
            RemoteFiles.Clear();

            RemoteFiles.AddRange(
                new FileDetails[] {
                    new FileDetails
                    {
                        FileName = "File1.dll"
                    },
                    new FileDetails
                    {
                        FileName = "File2.dll"
                    },
                    new FileDetails
                    {
                        FileName = "File3.dll"
                    },
                    new FileDetails
                    {
                        FileName = "File4.dll"
                    },
                });

            /*

            Application.Current.Dispatcher.Dispatch(() =>
            {
                try
                {
                    LocalFirmwareVersions.AddRange(newItems);
                    LocalFirmwareVersions.RemoveMany(removedItems);
                    CheckForNewFirmware();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
            */
        });
    }

}
