using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace Asol.Tools.WorkScheduler.Components
{
    #region FormMoveSupport
    /// <summary>
    /// Třída, která podporuje přesouvání formuláře za jeho tělo (a za jeho zvolené controly), nejen za titulek.
    /// Je tak možno přesouvat i formulář bez borderu.
    /// Použití: na formuláři se vytvoří instanční proměnná typu FormMoveSupport, do konstruktoru se předá hostitelský form.
    /// Tím se form (jeho vlastní plocha) stane přesouvatelná.
    /// Následně je možno zaregistrovat libovolný další control (metodou this.RegisterControl(control)),
    /// který se pak rovněž stane aktivním z hlediska přesouvání.
    /// </summary>
    public class FormMoveSupport
    {
        #region Public vrstva
        /// <summary>
        /// Konstruktor, předává se Form který se bude přesouvat.
        /// </summary>
        /// <param name="form"></param>
        public FormMoveSupport(Form form)
        {
            this._Form = form;
            this.RegisterControl(form);
        }
        /// <summary>
        /// Konstruktor, předává se Form který se bude přesouvat.
        /// Současně lze předat controly, které se mají zaregistrovat.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="controls"></param>
        public FormMoveSupport(Form form, params Control[] controls)
        {
            this._Form = form;
            this.RegisterControl(form);

            foreach (Control control in controls)
                RegisterControl(control);
        }
        /// <summary>
        /// Registrace libovolného controlu.
        /// This se zaháčkuje do jeho Mouse eventů a ošetří je tak, aby bylo možno jejich přesouváním přesouvat Form.
        /// </summary>
        /// <param name="control"></param>
        public void RegisterControl(Control control)
        {
            control.MouseDown += new MouseEventHandler(_MouseDown);
            control.MouseMove += new MouseEventHandler(_MouseMove);
            control.MouseUp += new MouseEventHandler(_MouseUp);
        }
        #endregion
        #region Privátní vrstva
        void _MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            Point fp = this._Form.Location;
            Point mp = Control.MousePosition;
            Point df = new Point(mp.X - fp.X, mp.Y - fp.Y);
            this._FormToMouseOffset = df;

            Size ds = SystemInformation.DragSize;
            this._InactiveMouseArea = new Rectangle(mp.X - ds.Width / 2, mp.Y - ds.Height / 2, ds.Width, ds.Height);

            this._Notificator = sender as IFormMoveNotification;
            this._NotifyState(FormMoveNotificationState.MouseDown);
        }
        void _MouseMove(object sender, MouseEventArgs e)
        {
            if (!this._IsMouseDown) return;

            Point mp = Control.MousePosition;
            if (this._IsWaitToMove)
            {
                if (this._InactiveMouseArea.Value.Contains(mp)) return;
                this._InactiveMouseArea = null;
                this._NotifyState(FormMoveNotificationState.BeginMove);
            }

            Point df = this._FormToMouseOffset.Value;
            Point fp = new Point(mp.X - df.X, mp.Y - df.Y);
            this._Form.Location = fp;

            this._NotifyState(FormMoveNotificationState.Move);
        }
        void _MouseUp(object sender, MouseEventArgs e)
        {
            this._NotifyState((this._IsMoveForm ? FormMoveNotificationState.DoneMove : FormMoveNotificationState.MouseUp));
            this._FormToMouseOffset = null;
            this._Notificator = null;
        }
        private void _NotifyState(FormMoveNotificationState state)
        {
            if (this._HasNotificator)
                this._Notificator.FormMoveNotification(this, new FormMoveNotificationArgs(state));
        }
        private Form _Form;
        private Point? _FormToMouseOffset;
        private Rectangle? _InactiveMouseArea;
        /// <summary>
        /// Jsme ve stavu, kdy je myš stisknuta. Tato property neřeší, zda se čeká na pohyb, nebo se pohybuje.
        /// </summary>
        private bool _IsMouseDown { get { return (this._FormToMouseOffset.HasValue); } }
        /// <summary>
        /// Jsme ve stavu, kdy je myš stisknuta ale ještě se nepohnula natolik, aby to zahájilo pohyb (čekáme na pohyb)
        /// </summary>
        private bool _IsWaitToMove { get { return (this._IsMouseDown && this._InactiveMouseArea.HasValue); } }
        /// <summary>
        /// Jsme ve stavu, kdy je myš stisknuta a pohybuje se tak daleko, že to pohybuje formulářem.
        /// </summary>
        private bool _IsMoveForm { get { return (this._IsMouseDown && !this._InactiveMouseArea.HasValue); } }
        private IFormMoveNotification _Notificator;
        private bool _HasNotificator { get { return (this._Notificator != null); } }
        #endregion
    }
    #endregion
    #region interface IFormMoveNotification + Args + State
    /// <summary>
    /// Interface, skrz který může správce pohybu formuláře FormMoveSupport 
    /// předávat informace o pohybu do toho objektu, který je zprostředkovatelem pohybu.
    /// Jinými slovy: pokud máme na formuláři button, který se zaregistroval jako control který umožňuje přesouvat formulář,
    /// pak tímto interfacem bude dostávat zprávy o pohybu (zahájení, průběh, konec).
    /// </summary>
    public interface IFormMoveNotification
    {
        /// <summary>
        /// Oznámení o změně stavu procesu přesouvání (začátek, průběh, konec)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void FormMoveNotification(object sender, FormMoveNotificationArgs args);
    }
    /// <summary>
    /// Informace o stavu pohybu formuláře skrze FormMoveSupport
    /// </summary>
    public class FormMoveNotificationArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="state"></param>
        public FormMoveNotificationArgs(FormMoveNotificationState state)
        {
            this.State = state;
        }
        /// <summary>
        /// Stav procesu přesouvání (začátek, průběh, konec)
        /// </summary>
        public FormMoveNotificationState State { get; private set; }
    }
    /// <summary>
    /// Stav pohybu formuláře skrze FormMoveSupport
    /// </summary>
    public enum FormMoveNotificationState
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Stiskla se myš, ale to ještě neznamená zahájení pohybu.
        /// </summary>
        MouseDown,
        /// <summary>
        /// Zvedla se myš, aniž by došlo k pohybu.
        /// </summary>
        MouseUp,
        /// <summary>
        /// Stisknutá myš se začala pohybovat nad rámec chvění.
        /// </summary>
        BeginMove,
        /// <summary>
        /// Probíhá přesun.
        /// </summary>
        Move,
        /// <summary>
        /// Končí přesun. Poté už nebude volána notifikace se stavem MouseUp.
        /// </summary>
        DoneMove
    }
    #endregion
}
