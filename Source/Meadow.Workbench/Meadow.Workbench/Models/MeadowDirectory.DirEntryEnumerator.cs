using System.Collections;
using System.Collections.Generic;

namespace Meadow.Workbench.ViewModels;

public partial class MeadowDirectory
{
    public class DirEntryEnumerator : IEnumerator<MeadowFileSystemEntry>
    {
        private MeadowDirectory _parent;
        private int _index;

        internal DirEntryEnumerator(MeadowDirectory parent)
        {
            _parent = parent;
            _index = -1;
        }

        public MeadowFileSystemEntry Current
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
}
