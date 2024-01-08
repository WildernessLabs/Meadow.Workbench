namespace Meadow.Workbench.ViewModels;

public abstract class MeadowDirEntry
{
    public string Name { get; set; }
    public string? Icon { get; set; }
}

public class MeadowFileEntry : MeadowDirEntry
{
    public MeadowFileEntry()
    {
    }

    public MeadowFileEntry(string name)
    {
        Name = name;
    }
}

public class MeadowFolderEntry : MeadowDirEntry
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
