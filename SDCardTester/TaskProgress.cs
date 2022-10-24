using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DjSoft.Tools.SDCardTester
{
    /// <summary>
    /// Třída, která umožní hlavnímu oknu aplikace prezentovat progress v taskbaru Windows.
    /// <para/>
    /// Její instance se vytváří s předáním reference na MainForm aplikace, uloží si WeakReferenci.<br/>
    /// MainForm aplikace pak musí volat zdejší metodu <see cref="FormWndProc(ref Message)"/>, jinak progres nebued komunikovat.<br/>
    /// Následně může aplikace setovat stav progresu <see cref="ProgressState"/> a jeho hodnoty <see cref="ProgressValue"/> a <see cref="ProgressMaximum"/>, tím budou zobrazovány požadované hodnoty.
    /// </summary>
    public class TaskProgress
    {
        #region Inicializace, proměnné
        /// <summary>
        /// Konstruktor. Je třeba předat referenci na Main Form aplikace, progress si uloží WeakReference na okno.
        /// MainForm aplikace pak musí ze své metody <see cref="Form.WndProc(ref Message)"/> volat zdejší metodu <see cref="FormWndProc(ref Message)"/>, jinak progres nebude komunikovat.<br/>
        /// Následně může aplikace setovat stav progresu <see cref="ProgressState"/> a jeho hodnoty <see cref="ProgressValue"/> a <see cref="ProgressMaximum"/>, tím budou zobrazovány požadované hodnoty.
        /// </summary>
        /// <param name="mainForm"></param>
        public TaskProgress(Form mainForm)
        {
            _Init(mainForm);
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        /// <param name="mainForm"></param>
        private void _Init(Form mainForm)
        {
            __IsTaskbarButtonCreated = -1;
            __ProgressState = ThumbnailProgressState.NoProgress;
            __ProgressValue = 0;
            __ProgressMaximum = 100;
            Version osVersion = Environment.OSVersion.Version;
            __IsValid = ((osVersion.Major == 6 && osVersion.Minor > 0) || (osVersion.Major > 6));
            if (__IsValid)
            {
                _MainForm = mainForm;
                __IsTaskbarButtonCreated = RegisterWindowMessage(@"TaskbarButtonCreated");
            }
        }
        /// <summary>
        /// (Weakreference) na owner Form
        /// </summary>
        private Form _MainForm
        {
            get { return (__MainForm != null && __MainForm.TryGetTarget(out var form) ? form : null); }
            set { __MainForm = (value != null ? new WeakReference<Form>(value) : null); }

        }
        /// <summary>
        /// OnDemand instance <see cref="ITaskbarList3"/>
        /// </summary>
        private ITaskbarList3 _CTaskbarList
        {
            get
            {
                if (__CTaskbarList == null)
                {
                    lock (this)
                    {
                        if (__CTaskbarList == null)
                        {
                            __CTaskbarList = (ITaskbarList3)new CTaskbarList();
                            __CTaskbarList.HrInit();
                        }
                    }
                }
                return __CTaskbarList;
            }
        }
        private WeakReference<Form> __MainForm;
        private ITaskbarList3 __CTaskbarList = null;
        private bool __IsValid;
        private int __IsTaskbarButtonCreated;
        private ThumbnailProgressState __ProgressState;
        private int __ProgressValue;
        private int __ProgressMaximum;
        #endregion
        #region Public rozhraní
        /// <summary>
        /// Obsahuje true, když prvek je platný.
        /// V tom případě do něj musí volající Form předávat svoji událost WndProc do metody <see cref="FormWndProc(ref Message)"/>.
        /// </summary>
        public bool IsValid { get { return __IsValid; } }
        /// <summary>
        /// Tuto metodu musí vyvolat OwnerForm ze své metody <see cref="Form.WndProc(ref Message)"/>
        /// </summary>
        /// <param name="m"></param>
        public void FormWndProc(ref Message m)
        {
            if (__IsValid && m.Msg == __IsTaskbarButtonCreated)
                // if taskbar button created or recreated, update progress status
                _RefreshProgress();
        }
        /// <summary>
        /// Status progresu = odpovídá barvě
        /// </summary>
        public ThumbnailProgressState ProgressState
        {
            get { return __ProgressState; }
            set
            {
                if (value != __ProgressState)
                {
                    __ProgressState = value;
                    _RefreshProgress(true, true);
                }
            }
        }
        /// <summary>
        /// Hodnota progresu.
        /// Musí být v rozsahu 0 až <see cref="ProgressMaximum"/>.
        /// </summary>
        public int ProgressValue
        {
            get { return __ProgressValue; }
            set
            {
                int progressValue = (value < 0 ? 0 : (value > __ProgressMaximum ? __ProgressMaximum : value));
                if (progressValue != __ProgressValue)
                {
                    __ProgressValue = progressValue;
                    _RefreshProgress(false, true);
                }
            }
        }
        /// <summary>
        /// Hodnota progresu.
        /// Musí být v rozsahu 0 až 1.
        /// Pokud bude setována hodnota nižší, než je aktuální <see cref="ProgressValue"/>, tak bude <see cref="ProgressValue"/> snížena na toto nově zadané maximum.
        /// </summary>
        public int ProgressMaximum
        {
            get { return __ProgressMaximum; }
            set
            {
                int progressMaximum = (value < 1 ? 1 : value);
                if (progressMaximum != __ProgressMaximum)
                {
                    if (__ProgressValue > progressMaximum)
                        __ProgressValue = progressMaximum;
                    __ProgressMaximum = progressMaximum;
                    _RefreshProgress(true, false);
                }
            }
        }
        #endregion
        #region Privátní komunikace na Progress, DLL importy
        /// <summary>
        /// Zajistí vepsání hodnot <see cref="__ProgressState"/>, <see cref="__ProgressValue"/> a <see cref="__ProgressMaximum"/> do <see cref="_CTaskbarList"/>
        /// </summary>
        /// <param name="setState"></param>
        /// <param name="setValue"></param>
        private void _RefreshProgress(bool setState = true, bool setValue = true)
        {
            if (__IsValid)
            {
                var form = _MainForm;
                var cTaskbarList = _CTaskbarList;
                if (form != null && cTaskbarList != null)
                {
                    if (setState)
                        cTaskbarList.SetProgressState(form.Handle, __ProgressState);
                    if (setValue && (__ProgressState == ThumbnailProgressState.Normal || __ProgressState == ThumbnailProgressState.Paused || __ProgressState == ThumbnailProgressState.Error))
                        cTaskbarList.SetProgressValue(form.Handle, (ulong)__ProgressValue, (ulong)__ProgressMaximum);
                }
            }
        }
        [DllImport("user32.dll")]
        internal static extern int RegisterWindowMessage(string message);

        [ComImportAttribute()]
        [GuidAttribute("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface ITaskbarList3
        {
            // ITaskbarList
            [PreserveSig]
            void HrInit();
            [PreserveSig]
            void AddTab(IntPtr hwnd);
            [PreserveSig]
            void DeleteTab(IntPtr hwnd);
            [PreserveSig]
            void ActivateTab(IntPtr hwnd);
            [PreserveSig]
            void SetActiveAlt(IntPtr hwnd);

            // ITaskbarList2
            [PreserveSig]
            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

            // ITaskbarList3
            void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);
            void SetProgressState(IntPtr hwnd, ThumbnailProgressState tbpFlags);
        }
        #endregion
    }
    [GuidAttribute("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterfaceAttribute(ClassInterfaceType.None)]
    [ComImportAttribute()]
    internal class CTaskbarList { }

    /// <summary>
    /// Stavy progresu v taskbaru Windows
    /// </summary>
    public enum ThumbnailProgressState
    {
        /// <summary>
        /// No progress is displayed.<br>
        /// yourFormName.Value is ignored.</br> </summary>
        NoProgress = 0,
        /// <summary>
        /// Normal progress is displayed.<br>
        /// The bar is GREEN.</br> </summary>
        Normal = 0x2,
        /// <summary>
        /// The operation is paused.<br>
        /// The bar is YELLOW.</br></summary>
        Paused = 0x8,
        /// <summary>
        /// An error occurred.<br>
        /// The bar is RED.</br> </summary>
        Error = 0x4,
        /// <summary>
        /// The progress is indeterminate.<br>
        /// Marquee style bar (constant scroll).</br> </summary>
        Indeterminate = 0x1
    }

}
