namespace Meadow.Workbench.ViewModels;

public class MeadowFolderEntry : MeadowFileSystemEntry
{
    private static MeadowFolderEntry? _previous;

    public MeadowFolderEntry(string name)
        : this()
    {
        Name = name;
    }

    public MeadowFolderEntry()
    {
        Icon = "/Assets/folder.svg";
    }

    public static MeadowFolderEntry Previous
    {
        get
        {
            return _previous ??= new MeadowFolderEntry
            {
                Name = ".."
            };
        }
    }
}
