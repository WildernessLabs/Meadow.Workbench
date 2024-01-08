using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Meadow.Workbench.ViewModels;

public class MeadowDirectory : IEnumerable<MeadowDirEntry>
{
    public class DirEntryEnumerator : IEnumerator<MeadowDirEntry>
    {
        private MeadowDirectory _parent;
        private int _index;

        internal DirEntryEnumerator(MeadowDirectory parent)
        {
            _parent = parent;
            _index = -1;
        }

        public MeadowDirEntry Current
        {
            get
            {
                if (_index == 0)
                {
                    return MeadowFolderEntry.Previous;
                }
                else if (_index < _parent.Directories.Count + 1)
                {
                    return _parent.Directories[_index - 1];
                }
                else
                {
                    return _parent.Files[_index - _parent.Directories.Count - 1];
                }
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (_index < (_parent.Directories.Count + _parent.Files.Count))
            {
                _index++;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _index = -1;
        }
    }

    private DirEntryEnumerator _enumerator;

    public string Name { get; set; }
    public List<MeadowDirEntry> Directories { get; set; } = new();
    public List<MeadowDirEntry> Files { get; set; } = new();

    public MeadowDirectory(string name)
    {
        _enumerator = new DirEntryEnumerator(this);
        Name = name;
    }

    public MeadowDirectory(string name, MeadowFileInfo[] contents)
    {
        _enumerator = new DirEntryEnumerator(this);
        Directories.AddRange(contents
            .Where(f => f.IsDirectory)
            .Select(i => new MeadowFolderEntry(i.Name)));
        Files.AddRange(contents
            .Where(f => !f.IsDirectory)
            .Select(i => new MeadowFileEntry(i.Name)));
        Name = name;
    }

    public IEnumerator<MeadowDirEntry> GetEnumerator()
    {
        return _enumerator;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
