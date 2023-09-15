using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    public class ChildItems<TParent, TItem> : IList<TItem>, IEnumerable<TItem>
        where TParent : class
        where TItem : class, IChildOfParent<TParent> 
    {
        #region Konstruktory
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        public ChildItems(TParent parent)
        {
            __Parent = parent;
            __List = new List<TItem>();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="capacity"></param>
        public ChildItems(TParent parent, int capacity)
        {
            __Parent = parent;
            __List = new List<TItem>(capacity);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="items"></param>
        public ChildItems(TParent parent, IEnumerable<TItem> items)
        {
            __Parent = parent;
            _SetParents(items);
            __List = new List<TItem>(items);
        }
        /// <summary>
        /// Parent, navazujeme jej do prvků
        /// </summary>
        private TParent __Parent;
        /// <summary>
        /// List prvků
        /// </summary>
        private List<TItem> __List;
        /// <summary>
        /// Do všech prvků vloží parenta
        /// </summary>
        /// <param name="items"></param>
        private void _SetParents(IEnumerable<TItem> items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item != null)
                        item.Parent = __Parent;
                }
            }
        }
        /// <summary>
        /// Do daného prvku vloží parenta
        /// </summary>
        /// <param name="item"></param>
        private void _SetParent(TItem item)
        {
            if (item != null)
                item.Parent = __Parent;
        }
        /// <summary>
        /// Z daného prvku odebere parenta
        /// </summary>
        /// <param name="item"></param>
        private void _RemoveParent(TItem item)
        {
            if (item != null)
                item.Parent = null;
        }
        #endregion
        #region Přidané funkce
        /// <summary>
        /// Přidá dodané prvky
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<TItem> items)
        {
            _SetParents(items);
            __List.AddRange(items);
        }
        /// <summary>
        /// Přidá dodané prvky
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(params TItem[] items)
        {
            _SetParents(items);
            __List.AddRange(items);
        }
        #endregion
        #region Interfaces IList, IEnumerable
        /// <summary>
        /// Prvek na daném indexu
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TItem this[int index] { get { return __List[index]; } set { _SetParent(value); __List[index] = value; } }
        /// <summary>
        /// Počet prvků
        /// </summary>
        public int Count { get { return __List.Count; } }
        /// <summary>
        /// Je ReadOnly
        /// </summary>
        public bool IsReadOnly { get { return false; } }
        /// <summary>
        /// Přidá prvek
        /// </summary>
        /// <param name="item"></param>
        public void Add(TItem item)
        {
            _SetParent(item);
            __List.Add(item);
        }
        /// <summary>
        /// Smaže celou kolekci
        /// </summary>
        public void Clear()
        {
            __List.ForEach(i => _RemoveParent(i));
            __List.Clear();
        }
        /// <summary>
        /// Obsahuje daný prvek?
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(TItem item)
        {
            return __List.Contains(item);
        }
        /// <summary>
        /// Kopíruje obsah do daného pole
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(TItem[] array, int arrayIndex)
        {
            __List.CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Index prvku
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(TItem item)
        {
            return __List.IndexOf(item);
        }
        /// <summary>
        /// Vloží prvek na danou pozici
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, TItem item)
        {
            _SetParent(item);
            __List.Insert(index, item);
        }
        /// <summary>
        /// Odebere daný prvek
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(TItem item)
        {
            bool isRemoved = __List.Remove(item);
            if (isRemoved)
                _RemoveParent(item);
            return isRemoved;
        }
        /// <summary>
        /// Odebere prvek z indexu
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            var item = __List[index];
            __List.RemoveAt(index);
            _RemoveParent(item);
        }
        /// <summary>
        /// Vrátí enumerátor
        /// </summary>
        /// <returns></returns>
        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
        {
            return __List.GetEnumerator();
        }
        /// <summary>
        /// Vrátí enumerátor
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return __List.GetEnumerator();
        }
        #endregion
    }

    public interface IChildOfParent<TParent>
    {
        TParent Parent { get; set; }
    }
}
