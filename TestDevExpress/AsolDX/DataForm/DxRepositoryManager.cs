using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WinDraw = System.Drawing;
using WinForm = System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    /// <summary>
    /// <see cref="DxRepositoryManager"/> : uchovává v sobě fyzické editorové prvky, typicky z namespace <see cref="DevExpress.XtraEditors"/>.
    /// Na vyžádání je poskytuje volajícímu, který si je umisťuje do panelu <see cref="DxDataFormContentPanel"/>, a to když je vyžadována interakce s uživatelem:
    /// MouseOn anebo Keyboard editační akce.
    /// Po odchodu myši nebo focusu z editačního prvku je tento prvek vykreslen do bitmapy a obsah bitmapy je uložen do interaktivního prvku <see cref="Data.DataFormCell"/>,
    /// odkud je pak průběžně vykreslován do grafického panelu <see cref="DxDataFormContentPanel"/> (řídí <see cref="Data.DataFormCell"/>).
    /// </summary>
    public class DxRepositoryManager
    {
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataFormPanel"></param>
        public DxRepositoryManager(DxDataFormPanel dataFormPanel)
        {
            __DataFormPanel = dataFormPanel;
            _InitRepository();
        }
        /// <summary>
        /// Panel dataformu
        /// </summary>
        internal DxDataFormPanel DataFormPanel { get { return __DataFormPanel; } } private DxDataFormPanel __DataFormPanel;
        #endregion
        #region Repository

        /// <summary>
        /// Inicializace repozitory editorů
        /// </summary>
        private void _InitRepository()
        {
            __RepositoryDict = new Dictionary<DxRepositoryEditorType, RepositoryItem>();
        }
        /// <summary>
        /// Úložiště prvků editorů
        /// </summary>
        private Dictionary<DxRepositoryEditorType, RepositoryItem> __RepositoryDict;

        #endregion
        #region subclass RepositoryItem
        /// <summary>
        /// Jeden druh controlu editoru a jeho fyzické instance
        /// </summary>
        private class RepositoryItem
        {
            public RepositoryItem(DxRepositoryManager repositoryManager, DxRepositoryEditorType editorType)
            {

            }
            public DxRepositoryManager RepositoryManager { get; set; }
            public DxRepositoryEditorType EditorType { get; set; }
            public WinForm.Control ControlPaint { get; set; }
        }
        #endregion
    }

    #region Enumy

    public enum DxRepositoryEditorType
    {
        None,
        Label,
        TextBox,
        EditBox,
        FileBox,
        CheckBox,
        RadioButton,
        Button,
        ComboListBox,
        SpinnerBox
    }
    #endregion
}
