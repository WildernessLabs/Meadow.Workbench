namespace Meadow.Workbench.ViewModels;

public class AppInfo
{
    private FileInfo _fi;

    public AppInfo(DirectoryInfo di)
    {
        // see if there's an App.exe in the folder
        _fi = di.GetFiles("App.dll").FirstOrDefault();

        if (_fi == null)
        {
            throw new Exception("Invalid Location");
        }

        // app name is the project name - look in the folder above "bin"
        var projectFolder = di.FullName.Substring(0, di.FullName.IndexOf("\\bin"));
        var projectFile = Directory.GetFiles(projectFolder, "*proj").FirstOrDefault();

        Name = Path.GetFileNameWithoutExtension(projectFile);
    }

    public string FullName => _fi.FullName;
    public string Name { get; init; }
    public DateTime LastChanged => _fi.LastWriteTime;
}
