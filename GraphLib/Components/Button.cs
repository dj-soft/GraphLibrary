using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Obyčejné tlačítko
    /// </summary>
    public class Button : TextObject
    {
        #region Konstruktor a proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Button()
        {
            this.BackgroundMode = DrawBackgroundMode.Solid;
            this.Is.Set(InteractiveProperties.Bit.DefaultMouseOverProperties
                      | InteractiveProperties.Bit.KeyboardInput
                      | InteractiveProperties.Bit.TabStop);
        }
        #endregion
        #region Vizuální styl objektu
        /// <summary>
        /// Styl tohoto konkrétního buttonu. 
        /// Zahrnuje veškeré vizuální vlastnosti.
        /// Výchozí je null, pak se styl přebírá z <see cref="StyleParent"/>, anebo společný z <see cref="Styles.Button"/>.
        /// Prvním čtením této property se vytvoří new instance. Lze ji tedy kdykoliv přímo použít.
        /// <para/>
        /// Do této property se typicky vkládá new instance, která řeší vzhled jednoho konkrétního prvku.
        /// </summary>
        public ButtonStyle Style { get { if (_Style == null) _Style = new ButtonStyle(); return _Style; } set { _Style = null; } }
        private ButtonStyle _Style;
        /// <summary>
        /// Obsahuje true tehdy, když <see cref="Style"/> je pro tento objekt deklarován.
        /// </summary>
        public bool HasStyle { get { return (_Style != null && !_Style.IsEmpty); } }
        /// <summary>
        /// Společný styl, deklarovaný pro více buttonů.
        /// Zde je reference na tuto instanci. 
        /// Modifikace hodnot v této instanci se projeví ve všech ostatních textboxech, které ji sdílejí.
        /// Výchozí je null, pak se styl přebírá společný z <see cref="Styles.Button"/>.
        /// <para/>
        /// Do této property se typicky vkládá odkaz na instanci, která je primárně uložena jinde, a řeší vzhled ucelené skupiny prvků.
        /// </summary>
        public ButtonStyle StyleParent { get; set; }
        /// <summary>
        /// Aktuální styl, nikdy není null. 
        /// Obsahuje <see cref="Style"/> ?? <see cref="StyleParent"/> ?? <see cref="Styles.Button"/>.
        /// </summary>
        protected IButtonStyle StyleCurrent { get { return (this._Style ?? this.StyleParent ?? Styles.Button); } }
        #endregion
        #region Public vlastnosti
        /// <summary>
        /// Druh buttonu
        /// </summary>
        public ButtonType ButtonType
        {
            get { return _ButtonType; }
            set
            {
                if (value != _ButtonType)
                {
                    _ButtonType = value;
                    Invalidate();
                }
            }
        }
        private ButtonType _ButtonType;
        /// <summary>
        /// true pokud jsem Checkovací
        /// </summary>
        protected bool IsCheckBox { get { var bt = _ButtonType; return (bt == ButtonType.ButtonCheck || bt == ButtonType.CheckBox); } }
        /// <summary>
        /// Obsahuje true, pokud this button je typu CheckBox, a pokud je zaškrtnutý.
        /// Pokud není CheckBox, vždy obsahuje false.
        /// Lze vložit hodnotu, ta se ale projeví pouze pokud typ buttonu je CheckBox.
        /// <para/>
        /// Po vložení změněné hodnoty do ChekcBoxu dojde k provedení události <see cref="IsCheckedChanged"/>.
        /// Argument události obsahuje změněnou hodnotu a důvod změny, po změně pomocí property obsahuje { args.EventSource = <see cref="Data.EventSourceType.ApplicationCode"/> }.
        /// </summary>
        public bool IsChecked
        {
            get { return (IsCheckBox && _IsChecked); }
            set
            {
                bool oldValue = _IsChecked;
                if (value != oldValue)
                {   // Hodnotu si uložím (po změně) i když nejsem CheckBox, protože uživatel ze mě může udělat CheckBox až v dalším kroku:
                    _IsChecked = value;
                    if (IsCheckBox)
                    {
                        // Pokud jsem CheckBox, volám událost a překreslení:
                        CallIsCheckedChanged(oldValue, value, Data.EventSourceType.ApplicationCode);
                        Invalidate();
                    }
                }
            }
        }
        private bool _IsChecked;
        #endregion
        #region Interaktivita
        /// <summary>
        /// Provádí se při jakékoli interaktivní změně
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChanged(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChanged(e);

            switch (e.ChangeState)
            {
                case GInteractiveChangeState.KeyboardKeyDown:
                    if (IsKeyToActivateButton(e.KeyboardEventArgs))
                        this.ButtonClickDown(false);
                    break;
                case GInteractiveChangeState.KeyboardKeyUp:
                    if (IsPressedActiveKey)
                        this.ButtonClickUp(false);
                    break;
                case GInteractiveChangeState.LeftDown:
                    this.ButtonClickDown(true);
                    break;
                case GInteractiveChangeState.LeftUp:
                    if (IsPressedActiveMouse)
                        this.ButtonClickUp(true);
                    break;

            }
        }
        /// <summary>
        /// Po stisku aktivační klávesy nebo levé myši
        /// </summary>
        /// <param name="fromMouse"></param>
        protected void ButtonClickDown(bool fromMouse)
        {
            if (fromMouse)
                IsPressedActiveMouse = true;
            else
                IsPressedActiveKey = true;

            if (IsCheckBox)
            {
                bool oldValue = _IsChecked;
                _IsChecked = !oldValue;
                CallIsCheckedChanged(oldValue, !oldValue, Data.EventSourceType.InteractiveChanged);
            }
            CallButtonClick();
        }
        /// <summary>
        /// Po uvolnění klávesy nebo myši
        /// </summary>
        /// <param name="fromMouse"></param>
        protected void ButtonClickUp(bool fromMouse)
        {
            if (fromMouse)
                IsPressedActiveMouse = false;
            else
                IsPressedActiveKey = false;
        }
        /// <summary>
        /// Zavolá metodu <see cref="OnButtonClick(EventArgs)"/> a event <see cref="ButtonClick"/>.
        /// </summary>
        protected void CallButtonClick()
        {
            EventArgs args = EventArgs.Empty;
            OnButtonClick(args);
            ButtonClick?.Invoke(this, args);
        }
        /// <summary>
        /// Bylo kliknuto na button. Je volána i po změně stavu Checked!
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnButtonClick(EventArgs args) { }
        /// <summary>
        /// Událost, kdy bylo kliknuto na button. Je volána i při změně stavu Checked!
        /// V době této události je hodnota <see cref="IsChecked"/> již změněna.
        /// Pořadí událostí je: <see cref="IsCheckedChanged"/>, <see cref="ButtonClick"/>.
        /// </summary>
        public event EventHandler ButtonClick;
        /// <summary>
        /// Zavolá metodu <see cref="OnIsCheckedChanged(GPropertyEventArgs{bool})"/> a event <see cref="IsCheckedChanged"/>.
        /// </summary>
        protected void CallIsCheckedChanged(bool oldValue, bool newValue, Data.EventSourceType eventSource)
        {
            GPropertyEventArgs<bool> args = new GPropertyEventArgs<bool>(newValue, eventSource);
            OnIsCheckedChanged(args);
            IsCheckedChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Bylo kliknuto na button. Je volána i po změně stavu Checked!
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnIsCheckedChanged(GPropertyEventArgs<bool> args) { }
        /// <summary>
        /// Událost, kdy došlo ke změně hodnoty <see cref="IsChecked"/>. Hodnota původní i nová je v argumentu.
        /// V argumentu je i důvod změny EventSource: 
        /// <see cref="Data.EventSourceType.ApplicationCode"/> po změně hodnoty z aplikačního kódu (setováním do property); 
        /// <see cref="Data.EventSourceType.InteractiveChanged"/> po interaktivní změně hodnoty uživatelem.
        /// Pořadí událostí je: <see cref="IsCheckedChanged"/>, <see cref="ButtonClick"/>.
        /// </summary>
        public event GPropertyEventHandler<bool> IsCheckedChanged;
        /// <summary>
        /// Vrátí true, pokud daná klávesa může vést k aktivaci buttonu
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected bool IsKeyToActivateButton(System.Windows.Forms.KeyEventArgs args)
        {
            return (args.Modifiers == System.Windows.Forms.Keys.None &&
                    (args.KeyCode == System.Windows.Forms.Keys.Enter ||
                     args.KeyCode == System.Windows.Forms.Keys.Space));
        }
        /// <summary>
        /// Obsahuje true v době, kdy je zmáčknutá aktivační klávesa prvku (Enter nebo Mezerník)
        /// </summary>
        protected bool IsPressedActiveKey { get; set; }
        /// <summary>
        /// Obsahuje true v době, kdy je zmáčknuté aktivační tlačítko myši (hlavní)
        /// </summary>
        protected bool IsPressedActiveMouse { get; set; }
        #endregion
        #region Kreslení, Current vlastnosti
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            var style = this.StyleCurrent;
            var borderType = style.BorderType;
            int borderWidth = Painter.GetBorderWidth(borderType);
            var interactiveState = VisualInteractiveState;
            var backColor = style.GetBackColor(interactiveState);
            var borderColor = style.GetBorderColor(interactiveState);
            Rectangle backBounds = absoluteBounds.Enlarge(-borderWidth);
            Painter.DrawAreaBase(e.Graphics, backBounds, backColor, System.Windows.Forms.Orientation.Horizontal, interactiveState);
            Painter.DrawBorder(e.Graphics, absoluteBounds, borderColor, borderType, interactiveState);
        }
        /// <summary>
        /// Obsahuje vizuální interaktivní stav. Vychází z <see cref="InteractiveObject.InteractiveState"/>, ale má od něj určité odchylky, které vedou ke korektnímu zobrazení:
        /// Pokud má být button "zmáčknutý" (viz <see cref="IsButtonVisualPressed"/>), pak přidáme příznak stylu <see cref="GInteractiveState.FlagDrag"/>.
        /// Knihovna stylů na to pro <see cref="IButtonStyle"/> reaguje tak, že aplikuje barvu <see cref="IButtonStyle.BackColorPressed"/>, pro ostatní barvy pak hodnoty typu MouseDown.
        /// </summary>
        protected GInteractiveState VisualInteractiveState
        {
            get
            {
                GInteractiveState state = this.InteractiveState;
                bool isCheckBox = IsCheckBox;
                if ((isCheckBox && IsChecked) || (!isCheckBox && (IsPressedActiveKey || IsPressedActiveMouse)))
                    state |= GInteractiveState.FlagDrag;
                return state;
            }
        }
        /// <summary>
        /// Obsahuje true, pokud button má vypadat jako zmáčknutý.
        /// </summary>
        protected bool IsButtonVisualPressed
        {
            get
            {
                bool isCheckBox = IsCheckBox;
                return ((isCheckBox && IsChecked) || (!isCheckBox && (IsPressedActiveKey || IsPressedActiveMouse)));
            }
        }
        protected override Color TextColorCurrent
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        protected override FontInfo FontCurrent
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
    /// <summary>
    /// Druh buttonu
    /// </summary>
    public enum ButtonType
    {
        /// <summary>
        /// Standardní tlačítko
        /// </summary>
        DefaultButton = 0,
        /// <summary>
        /// Tlačítko s funkcí CheckBox: jedno zmáčknutí = zůstane dole (, druhé zmáčknutí = uvolní se
        /// </summary>
        ButtonCheck,
        CheckBox
    }
}
