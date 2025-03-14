using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DjSoft.App.iCollect.Application;
using XmlSerial = DjSoft.App.iCollect.Data.XmlSerializer;

namespace DjSoft.App.iCollect.Collect
{
    /// <summary>
    /// <see cref="CollectionSet"/> : sada všech sbírek v evidenci.
    /// Jedna sbírka = <see cref="Collection"/>.
    /// </summary>
    public class CollectionSet
    {
        #region Singleton OnDemand
        private static CollectionSet _Instance
        {
            get
            {
                if (__Instance is null)
                {
                    lock (__Locker)
                    {
                        if (__Instance is null)
                        {
                            __Instance = new CollectionSet();
                            __Instance._Init();
                        }
                    }
                }
                return __Instance;
            }
        }
        private static CollectionSet __Instance;
        private static object __Locker = new object();
        #endregion
        #region Inicializace
        private CollectionSet()
        {
            __Collections = new List<Collection>();
        }
        private void _Init()
        {
            var dataPath = MainApp.AppDataPath;
            if (!MainApp.TryPrepareAppPath(dataPath, true)) return;

            var collections = new List<Collection>();

            var files = System.IO.Directory.GetFiles(dataPath, "*.dat");
            if (files.Length == 0)
                files = _CreateTestDataFiles(dataPath);

            foreach (var file in files)
            {
                var collection = Collection.LoadFromFile(file, false);
                if (collection != null)
                    collections.Add(collection);
            }
        }
        private List<Collection> __Collections;
        private Collection __ActiveCollection;
        #endregion
        #region Generátor sady ukázkových sbírkových souborů jako demo
        private static string[] _CreateTestDataFiles(string dataPath)
        {
            var result = new List<string>();
            
            result.Add(createDemo("Dolls.dat", () => Collection.DemoCollectionDoll, true));
            result.Add(createDemo("Book.dat", () => Collection.DemoCollectionBook, false));
            result.Add(createDemo("Movie.dat", () => Collection.DemoCollectionMovie, false));
            result.Add(createDemo("AudioCd.dat", () => Collection.DemoCollectionAudioCd, false));
            return result.ToArray();

            string createDemo(string fileName, Func<Collection> getData, bool setAsCurrent)
            {
                string fullName = System.IO.Path.Combine(dataPath, "Demo", fileName);
                if (!System.IO.File.Exists(fullName))
                {
                    var data = getData();
                    data.FileName = fullName;
                    data.Save();
                }

                if (setAsCurrent)
                    ActiveCollection = Collection.LoadFromFile(fullName, false);

                return fullName;
            }
        }
        #endregion
        #region Public data o sadě sbírek

        public static Collection[] Collections { get { return _Instance.__Collections.ToArray(); } }
        public static Collection ActiveCollection { get { return _Instance.__ActiveCollection; } set { _Instance.__ActiveCollection = value; } }
        #endregion
    }
}
