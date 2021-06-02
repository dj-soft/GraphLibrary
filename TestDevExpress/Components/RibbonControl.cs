using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

using WinFormServices.Drawing;

using XB = DevExpress.XtraBars;
using XR = DevExpress.XtraBars.Ribbon;
using Noris.Clients.Win.Components;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress
{
    #region class FreezePanel : Panel, který umí zamrazit control na sobě hostovaný
    /// <summary>
    /// Panel, který umí zamrazit control na sobě hostovaný.
    /// Tento <see cref="FreezePanel"/> se "podsouvá" pod klientský control <see cref="ClientControl"/>, a hostuje jej "na sobě".
    /// V případě potřeby je možno nastavit <see cref="Freeze"/> = true, tím dojde k zachycení aktuálního vzhledu <see cref="ClientControl"/> do Bitmapy,
    /// tato bitmapa je vykreslena na this <see cref="FreezePanel"/>, následně je "zhasnut" klientský control (jeho "Visible" = false) a poté si s ním může aplikační kód dělat co chce.
    /// Na konci své práce dá aplikační kód <see cref="Freeze"/> = false, tím dojde k zobrazení klientského controlu (a odstranění Bitmapy).
    /// </summary>
    public class FreezePanel : Panel
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public FreezePanel()
        {
            _Freeze = false;
            BorderStyle = BorderStyle.None;
        }
        /// <summary>
        /// Hostovaný klientský control. Běžně si žije svým životem, občas je zmrazen.
        /// </summary>
        public Control ClientControl { get { return _ClientControl; } set { _SetClientControl(value); } } private Control _ClientControl;
        /// <summary>
        /// Stav zmrazení vzhledu klientského controlu.
        /// <para/>
        /// V případě potřeby je možno nastavit <see cref="Freeze"/> = true, tím dojde k zachycení aktuálního vzhledu <see cref="ClientControl"/> do Bitmapy,
        /// tato bitmapa je vykreslena na this <see cref="FreezePanel"/>, následně je "zhasnut" klientský control (jeho "Visible" = false) a poté si s ním může aplikační kód dělat co chce.
        /// Na konci své práce dá aplikační kód <see cref="Freeze"/> = false, tím dojde k zobrazení klientského controlu (a odstranění Bitmapy).
        /// </summary>
        public bool Freeze { get { return _Freeze; } set { _SetFreeze(value); } } private bool _Freeze;
        /// <summary>
        /// Vložení klientského controlu včetně odvázání eventů (ze starého) a navázání eventů (do nového) controlu
        /// </summary>
        /// <param name="clientControl"></param>
        private void _SetClientControl(Control clientControl)
        {
            if (_ClientControl != null)
                _UnregisterControl(_ClientControl);
            _ClientControl = clientControl;
            if (_ClientControl != null)
                _RegisterControl(_ClientControl);
        }
        /// <summary>
        /// Navázání daného klientského controlu
        /// </summary>
        /// <param name="clientControl"></param>
        private void _RegisterControl(Control clientControl)
        {
            if (clientControl is null) return;
            bool freeze = _Freeze;
            _Freeze = false;           // Tím potlačím eventhandlery
            clientControl.SizeChanged += ClientControl_SizeChanged;
            clientControl.DockChanged += ClientControl_DockChanged;
            clientControl.VisibleChanged += ClientControl_VisibleChanged;
            this.Dock = clientControl.Dock;
            this.Size = clientControl.Size;
            if (!this.Controls.Contains(clientControl))
                this.Controls.Add(clientControl);
            _Freeze = freeze;
        }
        /// <summary>
        /// Odvázání daného klientského controlu
        /// </summary>
        /// <param name="clientControl"></param>
        private void _UnregisterControl(Control clientControl)
        {
            if (clientControl is null) return;
            clientControl.SizeChanged -= ClientControl_SizeChanged;
            clientControl.DockChanged -= ClientControl_DockChanged;
            clientControl.VisibleChanged -= ClientControl_VisibleChanged;
            if (this.Controls.Contains(clientControl))
                this.Controls.Remove(clientControl);
        }


        private void ClientControl_VisibleChanged(object sender, EventArgs e)
        {
            if (!_Freeze)
                this.Visible = _ClientControl.Visible;
        }

        private void ClientControl_DockChanged(object sender, EventArgs e)
        {
            
        }

        private void ClientControl_SizeChanged(object sender, EventArgs e)
        {
            if (!_Freeze)
                this.Size = _ClientControl.Size;
        }
        /// <summary>
        /// Nastavení stavu zmrazení
        /// </summary>
        /// <param name="freeze"></param>
        private void _SetFreeze(bool freeze)
        {
            if (freeze == _Freeze) return;                 // Reaguji jen na změnu!
            if (_ClientControl is null) return;            // Pokud nemám klienta, není co řešit...
            if (freeze)
            {
                _Freeze = true;                            // Odteď se nepřenáší _ClientControl.Visible do this.Visible, a ani Size
                Size size = this.Size;
                Bitmap bitmap = new Bitmap(size.Width, size.Height);
                Rectangle targetBounds = new Rectangle(Point.Empty, size);
                _ClientControl.DrawToBitmap(bitmap, targetBounds);
                this.BackgroundImage = bitmap;
                _ClientControl.Visible = false;
            }
            else
            {
                _ClientControl.Refresh();
                _ClientControl.Visible = true;
                if (this.BackgroundImage != null)
                    this.BackgroundImage.Dispose();
                this.BackgroundImage = null;
                _Freeze = false;
            }
        }
    }
    #endregion
}
