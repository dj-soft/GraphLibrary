// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using DevExpress.Utils.Drawing;
using DevExpress.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{

    #region MouseState : stav myši v rámci interaktivních prvků
    /// <summary>
    /// Stav myši
    /// </summary>
    public class MouseState
    {
        /// <summary>
        /// Vrátí aktuální stav myši pro daný Control
        /// </summary>
        /// <param name="control"></param>
        /// <param name="isLeave"></param>
        /// <returns></returns>
        public static MouseState CreateCurrent(Control control, bool? isLeave = null)
        {
            DateTime time = DateTime.Now;
            Point locationAbsolute = Control.MousePosition;
            MouseButtons buttons = Control.MouseButtons;
            Keys modifierKeys = Control.ModifierKeys;
            Point locationNative = control.PointToClient(locationAbsolute);
            // Pokud isLeave je true, pak jsme volání z MouseLeave a jsme tedy mimo Control:
            bool isOnControl = (isLeave.HasValue && isLeave.Value ? false : control.ClientRectangle.Contains(locationNative));
            Point locationVirtual = locationNative;
            if (control is DxVirtualPanel virtualControl) locationVirtual = virtualControl.GetVirtualPoint(locationNative);
            return new MouseState(time, locationNative, locationVirtual, locationAbsolute, buttons, modifierKeys, isOnControl);
        }
        /// <summary>
        /// Vrátí stav myši pro dané hodnoty
        /// </summary>
        /// <param name="time"></param>
        /// <param name="LocationControl">Souřadnice myši v koordinátech controlu (nativní prostor)</param>
        /// <param name="locationVirtual">Souřadnice myši v koordinátech virtuálního prostoru vrámci Controlu</param>
        /// <param name="locationAbsolute">Souřadnice myši v absolutních koordinátech (<see cref="Control.MousePosition"/>)</param>
        /// <param name="buttons">Stisknuté buttony</param>
        /// <param name="modifierKeys">Stav kláves Control, Alt, Shift</param>
        /// <param name="isOnControl">true pokud myš se nachází fyzicky nad Controlem</param>
        public MouseState(DateTime time, Point LocationControl, Point locationVirtual, Point locationAbsolute, MouseButtons buttons, Keys modifierKeys, bool isOnControl)
        {
            __Time = time;
            __LocationControl = LocationControl;
            __LocationVirtual = locationVirtual;
            __LocationAbsolute = locationAbsolute;
            __Buttons = buttons;
            __ModifierKeys = modifierKeys;
            __IsOnControl = isOnControl;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Buttons: {__Buttons}; LocationNative: {__LocationControl}";
        }
        private DateTime __Time;
        private Point __LocationControl;
        private Point __LocationVirtual;
        private Point __LocationAbsolute;
        private MouseButtons __Buttons;
        private Keys __ModifierKeys;
        private bool __IsOnControl;
        private InteractiveItem __InteractiveItem;
        /// <summary>
        /// Čas akce myši; důležité pro případný doubleclick
        /// </summary>
        public DateTime Time { get { return __Time; } }
        /// <summary>
        /// Souřadnice myši v koordinátech controlu (nativní prostor)
        /// </summary>
        public Point LocationControl { get { return __LocationControl; } }
        /// <summary>
        /// Souřadnice myši ve virtuálním prostoru  koordinátech controlu (nativní prostor)
        /// </summary>
        public Point LocationVirtual { get { return __LocationVirtual; } }
        /// <summary>
        /// Souřadnice myši v koordinátech absolutních (Screen)
        /// </summary>
        public Point LocationAbsolute { get { return __LocationAbsolute; } }
        /// <summary>
        /// Stav buttonů myši
        /// </summary>
        public MouseButtons Buttons { get { return __Buttons; } }
        /// <summary>
        /// Stav kláves Control, Alt, Shift
        /// </summary>
        public Keys ModifierKeys { get { return __ModifierKeys; } }
        /// <summary>
        /// Ukazatel myši se nachází nad controlem?
        /// </summary>
        public bool IsOnControl { get { return __IsOnControl; } }
        /// <summary>
        /// Prvek na pozici myši
        /// </summary>
        public InteractiveItem InteractiveItem { get { return __InteractiveItem; } set { __InteractiveItem = value; } }
    }
    #endregion
}
