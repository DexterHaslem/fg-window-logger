using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForegroundLogger_Managed
{
    internal class StorageManager
    {
        private IsolatedStorageFile _isolatedStorage = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
        
        public IEnumerable<string> GetAllLogFiles()
        {
            return _isolatedStorage.GetFileNames();
        }
   }
}
