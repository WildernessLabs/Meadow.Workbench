using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Meadow.Workbench.ViewModels;

public partial class MeadowDirectory : IEnumerable<MeadowFileSystemEntry>
{
    private DirEntryEnumerator _enumerator;

    public string Name { get; set; }
    public List<MeadowFileSystemEntry> Directories { get; set; } = new();
    public List<MeadowFileSystemEntry> Files { get; set; } = new();

    public MeadowDirectory(string name)
    {
        _enumerator = new DirEntryEnumerator(this);
        Name = name;
    }

    public static MeadowDirectory LoadFrom(string localPath)
    {
        var md = new MeadowDirectory(localPath);

        var di = new DirectoryInfo(localPath);

        if (di.Exists)
        {
            foreach (var d in di.GetDirectories())
            {
                md.Directories.Add(new MeadowFolderEntry(d.Name));
            }
            foreach (var f in di.GetFiles())
            {
                md.Files.Add(new MeadowFileEntry(f.Name));
            }
        }

        return md;
    }

    public MeadowDirectory(string name, MeadowFileInfo[] contents)
    {
        _enumerator = new DirEntryEnumerator(this);
        /*
        foreach (var d in contents
            .Where(f => f.IsDirectory)
            .Select(i => new MeadowFolderEntry(i.Name)))
        {
            Directories.Add(d);
        }
        */
        Directories.AddRange(contents
            .Where(f => f.IsDirectory)
            .Select(i => new MeadowFolderEntry(i.Name)));
        Files.AddRange(contents
            .Where(f => !f.IsDirectory)
            .Select(i => new MeadowFileEntry(i.Name)));
        Name = name;
    }

    public IEnumerator<MeadowFileSystemEntry> GetEnumerator()
    {
        return _enumerator;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
