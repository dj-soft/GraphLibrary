using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// GTextEdit (=TextBox)
    /// </summary>
    public class GTextEdit : GTextObject
    {
        #region Konstruktor, privátní život
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GTextEdit()
        {
            this.BackgroundMode = DrawBackgroundMode.Solid;
            this.Is.Set(InteractiveProperties.Bit.DefaultMouseOverProperties
                      | InteractiveProperties.Bit.KeyboardInput
                      | InteractiveProperties.Bit.TabStop);
        }
        #endregion
        #region Vstupní události (Focus, Klávesnice, Myš)
        protected override void AfterStateChangedFocusEnter(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusEnter(e);
            this.AfterFocusEnterCursor(e);
        }
        protected override void AfterStateChangedFocusLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusLeave(e);
            this.AfterFocusLeaveCursor(e);
        }
        protected override void AfterStateChangedKeyPreview(GInteractiveChangeStateArgs e)
        {
            if (IsCursorMoveKey(e.KeyboardPreviewArgs))
                e.KeyboardPreviewArgs.IsInputKey = true;
            base.AfterStateChangedKeyPreview(e);
        }
        protected override void AfterStateChangedKeyPress(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedKeyPress(e);
            if (e.KeyboardPressEventArgs.KeyChar == 0x0d)
                this.Host?.SetFocusToNextItem(Data.Direction.Positive);
        }
        protected override void AfterStateChangedMouseLeftDown(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseLeftDown(e);
            this.AfterMouseDownCursor(e);
        }
        protected override void AfterStateChangedMouseOver(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseOver(e);
            if (CurrentMouseButtonIsLeft)
            {

            }
        }
        protected override void AfterStateChangedDragFrameBegin(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedDragFrameBegin(e);
        }
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedLeftClick(e);
            // this.CursorIndex = null;                       // Po MouseClick dojde k překreslení TextBoxu, a v metodě Draw se vyvolá FindCursorBounds();
            // this.MouseClickPoint = e.MouseAbsolutePoint;   //  tam se detekuje že není známá pozice kurzoru (CursorIndex = null), ale je známý bod myši (MouseClickPoint)...
        }
        #endregion
        #region Klávesnice základní
        #endregion
        #region Interaktivita - pohyb kurzoru (klávesy Home, End, Kurzor) a označování (Selection)
        /// <summary>
        /// Příchod Focusu do Textboxu může připravit pozici kurzoru i příznaky i Selected
        /// </summary>
        /// <param name="e"></param>
        protected void AfterFocusEnterCursor(GInteractiveChangeStateArgs e)
        {
            // Příchod Focusu do Textboxu nastaví příznak, že následující MouseDown(pokud přijde) se má chovat jinak, viz AfterMouseDownNone(GInteractiveChangeStateArgs)
            e.UserData = ConstantFocusEnter;
            // Při vstupu do textboxu neměníme pozici kurzoru CursorIndex. To proto, že 0 je default, a na 0 ji nastavujeme v AfterStateChangedFocusLeave.
            // Takže defaultně se po odchodu z textboxu a následném příchodu focusu zobrazí kurzor na pozici 0 (podle nastavení .
            // Pokud ale aplikační kód nastaví CursorIndex na konkrétní hodnotu, pak tato hodnota bude platná okamžitě (pokud textbox má focus) anebo po příchodu do prvku.
            EditorState.HasFocus = true;
        }
        /// <summary>
        /// Konstanta vkládaná do <see cref="GInteractiveChangeStateArgs.UserData"/> v události FocusEnter, detekovaná v události MouseDown.
        /// Výsledkem je odlišení situace, kdy MouseDown je provedeno jako prvotní vstup do TextBoxu anebo v již editovaném TextBoxu.
        /// </summary>
        protected const string ConstantFocusEnter = "FocusEnter";
        /// <summary>
        /// Odchod Focusu může resetovat pozici kurzoru
        /// </summary>
        /// <param name="e"></param>
        protected void AfterFocusLeaveCursor(GInteractiveChangeStateArgs e)
        {
            EditorStateData = EditorState.Data;
            EditorState.Dispose();
            _EditorState = null;

            // Pozice kurzoru: pokud si pozici NEMÁME pamatovat, tak ji nulujeme při LostFocus:
            if (!GTextEdit.SaveCursorPositionOnLostFocus)
                this._CursorIndex = 0;               // Defaultní chování: po opětovném návratu focusu do prvku bude kurzor na začátku.
        }
        /// <summary>
        /// Stisk myši může ovlivnit pozici kurzoru i Selected
        /// </summary>
        /// <param name="e"></param>
        protected void AfterMouseDownCursor(GInteractiveChangeStateArgs e)
        {   // Pozor, stav textboxu je: HasFocus = true, ale dosud nemusí být v tomto stavu vykreslen (=při prvním příchodu Focusu) (ačkoliv na druhou stranu focus už tu být může!), 
            //  takže nelze použít pole this.CharacterBounds = pole jednotlivých znaků a jejich pozic!!!
            // Postup: zde připravíme data, a v procesu kreslení bude vyvolána metoda AfterMouseCursorPrepare().
            // Odlišení, zda jde o MouseDown při aktivaci focusu (tzn. na TextBox bylo kliknuto, když neměl Focus) anebo o MouseDown v procesu editace textu (měl Focus) 
            //  se dá poznat podle hodnoty v e.UserData: v metodě this.AfterFocusEnterCursor() do e.UserData vkládáme string "FocusEnter",
            //  a tato hodnota se pak přenese i do eventu AfterStateChangedMouseLeftDown(), tedy sem.
            //  Pokud ale TextBox už focus má, pak se při stisku myši už událost AfterFocusEnterCursor() nevyvolá, a v proměnné e.UserData nebude nic.
            EditorState.AfterMouseCursorPrepare(e);
        }
        /// <summary>
        /// Metoda vrátí true, pokud daná klávesová kombinace bude interpretována jako nějaký pohyb kurzoru (samotný pohyb / označování se shiftem)
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual bool IsCursorMoveKey(PreviewKeyDownEventArgs args)
        {
            switch (args.KeyCode)                // KeyCode obsahuje čistou klávesu = bez modifikátoru (bez Ctrl, Shift, Alt)
            {
                case Keys.Home:
                case Keys.End:
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                    return true;
            }
            if (args.KeyData.HasFlag(Keys.Shift))
            { }
            return false;
        }

        /// <summary>
        /// Třída která slouží pro support editace v TextBoxu.
        /// Instance existuje pouze v době, kdy TextBox má Focus, při ztrátě focusu instance zaniká, a po návratu Focusu se vygeneruje nová.
        /// Šetří se tak výrazně paměť, protože TextBox v mimoeditační době nepotřebuje téměř žádné další proměnné nad rámec <see cref="InteractiveObject"/>.
        /// </summary>
        protected EditorStateInfo EditorState
        {
            get

            {
                if (_EditorState == null) _EditorState = new EditorStateInfo(this, EditorStateData);
                return _EditorState;
            }
        }
        private EditorStateInfo _EditorState;
        /// <summary>
        /// String, který uchovává klíčová data z <see cref="EditorStateInfo"/> v době, kdy textbox nemá focus a instance v <see cref="EditorState"/> je zahozena (v LostFocus).
        /// Z tohoto stringu je při návratu Focusu do this TextBoxu vytvořen (restorován) nový objekt <see cref="EditorStateInfo"/>.
        /// </summary>
        protected string EditorStateData;
        #endregion
        #region Vykreslení obsahu - základní
        /// <summary>
        /// Zajistí kreslení TextBoxu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            Rectangle innerBounds = this.GetInnerBounds(absoluteBounds);
            GTextEditDrawArgs drawArgs = new GTextEditDrawArgs(e, absoluteBounds, absoluteVisibleBounds, innerBounds, drawMode, this.HasFocus, this.InteractiveState, this);
            this.DetectOverlayBounds(drawArgs, OverlayText);    // Nastaví OverlayBounds a modifikuje TextBounds
            this.DrawBackground(drawArgs);                      // Background
            this.DrawOverlay(drawArgs, OverlayBackground);      // Grafika nad Backgroundem
            this.DrawText(drawArgs);                            // Text
            this.DrawOverlay(drawArgs, OverlayText);            // Grafika nad Textem
            this.DrawBorder(drawArgs);                          // Rámeček
        }
        /// <summary>
        /// Vrací souřadnice vnitřního prostoru po odečtení prostoru pro Border (0-1-2 pixely)
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        protected Rectangle GetInnerBounds(Rectangle bounds)
        {
            switch (this.BorderStyle.Value)
            {
                case BorderStyleType.None: return bounds;
                case BorderStyleType.Flat:
                case BorderStyleType.Effect3D: return bounds.Enlarge(-1);
                case BorderStyleType.Soft: return bounds.Enlarge(-2);
            }
            return bounds;
        }
        /// <summary>
        /// Metoda určí prostor pro vykreslení Overlay, a případně modifikuje prostor pro vlastní text zmenšený o prostor Overlay.
        /// Pokud Overlay není přítomen nebo není aktivní, pak vrátí null.
        /// </summary>
        /// <param name="drawArgs">Argumenty</param>
        /// <param name="overlay">Objekt Overlay (prakticky pouze <see cref="OverlayText"/>)</param>
        /// <returns></returns>
        protected void DetectOverlayBounds(GTextEditDrawArgs drawArgs, ITextEditOverlay overlay)
        {
            if (overlay == null) return;
            overlay.DetectOverlayBounds(drawArgs);
        }
        /// <summary>
        /// Vykreslí pozadí
        /// </summary>
        /// <param name="drawArgs">Argumenty</param>
        protected virtual void DrawBackground(GTextEditDrawArgs drawArgs)
        {
            this.DrawBackground(drawArgs.DrawArgs, drawArgs.InnerBounds, drawArgs.AbsoluteVisibleBounds, drawArgs.DrawMode, this.CurrentBackColor);
        }
        /// <summary>
        /// Vykreslí Overlay
        /// </summary>
        /// <param name="drawArgs">Argumenty</param>
        /// <param name="overlay">Objekt Overlay (<see cref="OverlayBackground"/> nebo <see cref="OverlayText"/>)</param>
        protected virtual void DrawOverlay(GTextEditDrawArgs drawArgs, ITextEditOverlay overlay)
        {
            overlay?.DrawOverlay(drawArgs);
        }
        /// <summary>
        /// Vykreslí border
        /// </summary>
        /// <param name="drawArgs">Argumenty</param>
        protected virtual void DrawBorder(GTextEditDrawArgs drawArgs)
        {
            switch (this.BorderStyle.Value)
            {
                case BorderStyleType.Flat:
                    GPainter.DrawBorder(drawArgs.Graphics, drawArgs.AbsoluteBounds, RectangleSide.All, null, this.CurrentBorderColor, 0f);
                    break;
                case BorderStyleType.Effect3D:
                    GPainter.DrawBorder(drawArgs.Graphics, drawArgs.AbsoluteBounds, RectangleSide.All, null, this.CurrentBorderColor, this.CurrentBorder3DEffect);
                    break;
                case BorderStyleType.Soft:
                    GPainter.DrawSoftBorder(drawArgs.Graphics, drawArgs.AbsoluteBounds, RectangleSide.All, this.CurrentBorderColor);
                    break;
            }
        }
        #endregion
        #region Vykreslení textu a kurzoru, na základě stavu editace
        /// <summary>
        /// Index pozice kurzoru.
        /// Hodnota <see cref="CursorIndex"/> je vždy v rozmezí 0 až Text.Length (včetně).
        /// <para/>
        /// Hodnota 0 = před prvním znakem textu = výchozí hodnota.
        /// Lze vložit hodnotu == délce textu, pak bude kurzor ZA posledním znakem textu.
        /// Hodnotu lze vložit před příchodem focusu do prvku i v době, kdy je focus v prvku.
        /// Po odchodu focusu z prvku je nastavena hodnota 0 = příští příchod Focusu je na počáteční pozici (ale lze změnit).
        /// </summary>
        public virtual int CursorIndex
        {
            get { int length = Text.Length; int index = _CursorIndex; return (index < 0 ? 0 : (index > length ? length : index)); }
            set { int length = Text.Length; int index = value; _CursorIndex = (index < 0 ? 0 : (index > length ? length : index)); if (this.HasFocus) Invalidate(); }
        }
        private int _CursorIndex = 0;

        /// <summary>
        /// Vykreslí obsah (text)
        /// </summary>
        /// <param name="drawArgs">Argumenty</param>
        protected virtual void DrawText(GTextEditDrawArgs drawArgs)
        {
            string text = this.Text;
            Rectangle textBounds = drawArgs.TextBounds;
            if (text.Length > textBounds.Width) text = text.Substring(0, textBounds.Width);
            using (GPainter.GraphicsClip(drawArgs.Graphics, textBounds))
            {
                textBounds.Width = 2048;
                StringFormatFlags stringFormat = StringFormatFlags.NoWrap | StringFormatFlags.LineLimit;
                ContentAlignment alignment = this.Alignment;
                if (!this.HasFocus || EditorState == null)
                {
                    // Bez Focusu = jen vypíšu text:
                    GPainter.DrawString(drawArgs.Graphics, text, this.CurrentFont, textBounds, alignment, color: this.CurrentTextColor, stringFormat: stringFormat);
                    return;
                }

                if (text.Length == 0) text = " ";              // Kvůli souřadnicím
                RectangleF[] charPositions = GPainter.DrawStringMeasureChars(drawArgs.Graphics, text, this.CurrentFont, textBounds, alignment, color: this.CurrentTextColor, stringFormat: stringFormat);
                EditorState.SetCharacterPositions(text, charPositions);
            }
            EditorState.MouseDownDataProcess();
            this.DrawSelectionAndCursor(drawArgs);

            /*

            Color[] overColors = new Color[] { Color.FromArgb(64, 220, 255, 220), Color.FromArgb(64, 255, 220, 220), Color.FromArgb(64, 220, 220, 255) };
            int alpha = 128;
            int loval = 160;
            int hival = 220;
            overColors = new Color[] { Color.FromArgb(alpha, loval, hival, loval), Color.FromArgb(alpha, hival, loval, loval), Color.FromArgb(alpha, loval, loval, hival) };
            int idx = 0;
            foreach (var charPos in charPositions)
            {
                Color color = overColors[idx % 3];
                e.Graphics.FillRectangle(Skin.Brush(color), charPos);
                idx++;
            }

            if (this.MouseClickPoint.HasValue)
            {
                RectangleF? cursor = FindCursorByPoint(this.MouseClickPoint.Value, charPositions);
                if (cursor.HasValue)
                    e.Graphics.FillRectangle(Brushes.Black, cursor.Value);
                Host.AnimationStart(this.ShowCursorTick);
            }
            */
        }
        /// <summary>
        /// Vykreslí Selection a Kurzor
        /// </summary>
        /// <param name="drawArgs"></param>
        protected virtual void DrawSelectionAndCursor(GTextEditDrawArgs drawArgs)
        {
            var cursorBounds = EditorState.CursorBounds;
            if (cursorBounds.HasValue)
                drawArgs.Graphics.FillRectangle(Brushes.Black, cursorBounds.Value);
        }
        /// <summary>
        /// Vykreslí blikání kurzoru
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected AnimationResult ShowCursorTick(AnimationArgs args)
        {


            return AnimationResult.Stop;
        }
        #endregion
        #region Public vlastnosti definující vzhled (Color, Border, Font)
        /// <summary>
        /// Defaultní barva pozadí.
        /// </summary>
        protected override Color BackColorDefault { get { return Skin.TextBox.BackColorEnabled; } }
        /// <summary>
        /// Barva pozadí Disabled (= Protected).
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? BackColorDisabled { get { return __BackColorDisabled ?? BackColorDisabledDefault; } set { __BackColorDisabled = value; Invalidate(); } }
        private Color? __BackColorDisabled = null;
        /// <summary>
        /// Defaultní barva pozadí Disabled (= Protected)
        /// </summary>
        protected virtual Color BackColorDisabledDefault { get { return Skin.TextBox.BackColorDisabled; } }
        /// <summary>
        /// Barva pozadí MouseOver.
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? BackColorMouseOver { get { return __BackColorMouseOver ?? BackColorMouseOverDefault; } set { __BackColorMouseOver = value; Invalidate(); } }
        private Color? __BackColorMouseOver = null;
        /// <summary>
        /// Defaultní barva pozadí MouseOver
        /// </summary>
        protected virtual Color BackColorMouseOverDefault { get { return Skin.TextBox.BackColorMouseOver; } }
        /// <summary>
        /// Barva pozadí Focused.
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? BackColorFocused { get { return __BackColorFocused ?? BackColorFocusedDefault; } set { __BackColorFocused = value; Invalidate(); } }
        private Color? __BackColorFocused = null;
        /// <summary>
        /// Defaultní barva pozadí Focused
        /// </summary>
        protected virtual Color BackColorFocusedDefault { get { return Skin.TextBox.BackColorFocused; } }
        /// <summary>
        /// Barva pozadí SelectedText.
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? BackColorSelectedText { get { return __BackColorSelectedText ?? BackColorSelectedTextDefault; } set { __BackColorSelectedText = value; Invalidate(); } }
        private Color? __BackColorSelectedText = null;
        /// <summary>
        /// Defaultní barva pozadí pozadí SelectedText
        /// </summary>
        protected virtual Color BackColorSelectedTextDefault { get { return Skin.TextBox.BackColorSelectedText; } }
        /// <summary>
        /// Aktuální barva pozadí
        /// </summary>
        protected Color CurrentBackColor
        {
            get
            {
                Color backColor;
                var state = this.InteractiveState;
                bool isDisabled = (!this.Enabled || this.ReadOnly);
                if (isDisabled) backColor = BackColorDisabled.Value;
                else if (this.HasFocus) backColor = BackColorFocused.Value;
                else if (state.HasFlag(GInteractiveState.FlagOver)) backColor = BackColorMouseOver.Value;
                else backColor = BackColor.Value;
                return backColor;
            }
        }

        /// <summary>
        /// Defaultní barva písma.
        /// </summary>
        protected override Color TextColorDefault { get { return Skin.TextBox.TextColorEnabled; } }
        /// <summary>
        /// Barva písma Disabled (= Protected).
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? TextColorDisabled { get { return __TextColorDisabled ?? TextColorDisabledDefault; } set { __TextColorDisabled = value; Invalidate(); } }
        private Color? __TextColorDisabled = null;
        /// <summary>
        /// Defaultní barva písma pasivní = read only (disabled).
        /// </summary>
        protected virtual Color TextColorDisabledDefault { get { return Skin.TextBox.TextColorDisabled; } }
        /// <summary>
        /// Barva písma MouseOver.
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? TextColorMouseOver { get { return __TextColorMouseOver ?? TextColorMouseOverDefault; } set { __TextColorMouseOver = value; Invalidate(); } }
        private Color? __TextColorMouseOver = null;
        /// <summary>
        /// Defaultní barva písma MouseOver
        /// </summary>
        protected virtual Color TextColorMouseOverDefault { get { return Skin.TextBox.TextColorMouseOver; } }
        /// <summary>
        /// Barva písma Focused.
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? TextColorFocused { get { return __TextColorFocused ?? TextColorFocusedDefault; } set { __TextColorFocused = value; Invalidate(); } }
        private Color? __TextColorFocused = null;
        /// <summary>
        /// Defaultní barva písma Focused
        /// </summary>
        protected virtual Color TextColorFocusedDefault { get { return Skin.TextBox.TextColorFocused; } }
        /// <summary>
        /// Barva písma SelectedText.
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? TextColorSelectedText { get { return __TextColorSelectedText ?? TextColorSelectedTextDefault; } set { __TextColorSelectedText = value; Invalidate(); } }
        private Color? __TextColorSelectedText = null;
        /// <summary>
        /// Defaultní barva písma SelectedText
        /// </summary>
        protected virtual Color TextColorSelectedTextDefault { get { return Skin.TextBox.TextColorSelectedText; } }
        /// <summary>
        /// Aktuální barva písma
        /// </summary>
        protected Color CurrentTextColor
        {
            get
            {
                Color textColor;
                var state = this.InteractiveState;
                bool isDisabled = (!this.Enabled || this.ReadOnly);
                if (isDisabled) textColor = TextColorDisabled.Value;
                else if (this.HasFocus) textColor = TextColorFocused.Value;
                else if (state.HasFlag(GInteractiveState.FlagOver)) textColor = TextColorMouseOver.Value;
                else textColor = TextColor.Value;
                return textColor;
            }
        }

        /// <summary>
        /// Typ rámečku textboxu. 
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public BorderStyleType? BorderStyle { get { return __BorderStyle ?? BorderStyleDefault; } set { __BorderStyle = value; Invalidate(); } }
        private BorderStyleType? __BorderStyle;
        /// <summary>
        /// Defaultní typ rámečku textboxu
        /// </summary>
        protected virtual BorderStyleType BorderStyleDefault { get { return Skin.TextBox.BorderStyle; } }
        /// <summary>
        /// Barva rámečku Enabled.
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? BorderColor { get { return __BorderColor ?? BorderColorDefault; } set { __BorderColor = value; Invalidate(); } }
        private Color? __BorderColor = null;
        /// <summary>
        /// Defaultní barva rámečku.
        /// </summary>
        protected virtual Color BorderColorDefault { get { return Skin.TextBox.BorderColor; } }
        /// <summary>
        /// Barva rámečku Disabled (= Protected).
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? BorderColorDisabled { get { return __BorderColorDisabled ?? BorderColorDisabledDefault; } set { __BorderColorDisabled = value; Invalidate(); } }
        private Color? __BorderColorDisabled = null;
        /// <summary>
        /// Defaultní barva rámečku pasivní = read only (disabled).
        /// </summary>
        protected virtual Color BorderColorDisabledDefault { get { return Skin.TextBox.BorderColor; } }
        /// <summary>
        /// Barva rámečku MouseOver.
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? BorderColorMouseOver { get { return __BorderColorMouseOver ?? BorderColorMouseOverDefault; } set { __BorderColorMouseOver = value; Invalidate(); } }
        private Color? __BorderColorMouseOver = null;
        /// <summary>
        /// Defaultní barva rámečku MouseOver
        /// </summary>
        protected virtual Color BorderColorMouseOverDefault { get { return Skin.TextBox.BorderColor; } }
        /// <summary>
        /// Barva rámečku Focused.
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? BorderColorFocused { get { return __BorderColorFocused ?? BorderColorFocusedDefault; } set { __BorderColorFocused = value; Invalidate(); } }
        private Color? __BorderColorFocused = null;
        /// <summary>
        /// Defaultní barva rámečku Focused
        /// </summary>
        protected virtual Color BorderColorFocusedDefault { get { return Skin.TextBox.BorderColorFocused; } }
        /// <summary>
        /// Barva rámečku Required. Hodnota Alpha kanálu určuje Morphing hodnotu z běžné barvy: 0=pro Required textbox se bude Border kreslit beze změny, 255=bude vždy použita čistá barva <see cref="BorderColorRequired"/>.
        /// Vhodná hodnota je 128 - 192, kdy Border částečně reaguje na Focus (přebírá například barvu <see cref="BorderColorMouseOver"/>).
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? BorderColorRequired { get { return __BorderColorRequired ?? BorderColorRequiredDefault; } set { __BorderColorRequired = value; Invalidate(); } }
        private Color? __BorderColorRequired = null;
        /// <summary>
        /// Defaultní barva rámečku Required
        /// </summary>
        protected virtual Color BorderColorRequiredDefault { get { return Skin.TextBox.BorderColorRequired; } }
        /// <summary>
        /// Barva rámečku Soft verze = když <see cref="BorderStyle"/> == <see cref="BorderStyleType.Soft"/>. Hodnota Alpha kanálu určuje Morphing hodnotu z běžné barvy: 0=pro Soft textbox se bude Border kreslit beze změny, 255=bude vždy použita čistá barva <see cref="BorderColorSoft"/>.
        /// Vhodná hodnota je 128 - 192, kdy Border částečně reaguje na Focus (přebírá například barvu <see cref="BorderColorMouseOver"/>).
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public Color? BorderColorSoft { get { return __BorderColorSoft ?? BorderColorSoftDefault; } set { __BorderColorSoft = value; Invalidate(); } }
        private Color? __BorderColorSoft = null;
        /// <summary>
        /// Defaultní barva rámečku Required
        /// </summary>
        protected virtual Color BorderColorSoftDefault { get { return Skin.TextBox.BorderColorSoft; } }
        /// <summary>
        /// Hodnota průhlednosti rámečku ve verzi Soft.
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// </summary>
        public float? BorderAlphaSoft { get { return __BorderAlphaSoft ?? BorderAlphaSoftDefault; } set { __BorderAlphaSoft = value; Invalidate(); } }
        private float? __BorderAlphaSoft = null;
        /// <summary>
        /// Defaultní barva rámečku Required
        /// </summary>
        protected virtual float BorderAlphaSoftDefault { get { return Skin.TextBox.BorderAlphaSoft; } }
        
        /// <summary>
        /// Obsahuje true pokud Border používá barvy Simple, false pokud používá barvy Soft
        /// </summary>
        protected bool BorderIsSimple { get { return (BorderStyle.Value != BorderStyleType.Soft); } }
        /// <summary>
        /// Aktuální barva okrajů.
        /// Obsahuje vliv všech faktorů: Disabed, MouseOver, Focus, Soft border, IsRequired.
        /// </summary>
        protected Color CurrentBorderColor
        {
            get
            {
                var state = this.InteractiveState;
                Color borderColor;
                bool isDisabled = (!this.Enabled || this.ReadOnly);
                if (isDisabled) borderColor = BorderColorDisabled.Value;
                else if (this.HasFocus) borderColor = BorderColorFocused.Value;
                else if (state.HasFlag(GInteractiveState.FlagOver)) borderColor = BorderColorMouseOver.Value;
                else borderColor = BorderColor.Value;

                if (BorderStyle.Value == BorderStyleType.Soft)
                    borderColor = borderColor.Morph(BorderColorSoft.Value);

                if (IsRequiredValue)
                    borderColor = borderColor.Morph(BorderColorRequired.Value);

                if (BorderStyle.Value == BorderStyleType.Soft)
                    borderColor = borderColor.CreateTransparent(BorderAlphaSoft.Value);

                return borderColor;
            }
        }
        /// <summary>
        /// Aktuální hodnota 3D efektu pro Border
        /// </summary>
        protected float CurrentBorder3DEffect
        {
            get
            {
                var state = this.InteractiveState;
                if (state.HasFlag(GInteractiveState.Disabled)) return 0f;
                if (this.HasFocus) return -0.45f;
                if (state.HasFlag(GInteractiveState.FlagOver)) return -0.25f;
                return -0.15f;
            }
        }
        #endregion
        #region Public členové
        /// <summary>
        /// Prvek může dostat focus při pohybu Tab / Ctrl+Tab
        /// </summary>
        public bool TabStop { get { return this.Is.TabStop; } set { this.Is.TabStop = value; } }
        /// <summary>
        /// Označit (selectovat) celý obsah textu po příchodu focusu do prvku? Platí i pro příchod myším kliknutím.
        /// Při čtení má vždy hodnotu. 
        /// Setovat lze null, pak bude čtena hodnota defaultní (to je výchozí stav) = <see cref="DefaultSelectAll"/>.
        /// </summary>
        public bool? SelectAllText
        {
            get { return (this.Is.SelectAllTextExplicit ? this.Is.SelectAllText : DefaultSelectAll); }
            set
            {
                this.Is.SelectAllTextExplicit = value.HasValue;      // Pokud je dodaná hodnota, pak je explicitní (Is.SelectAllTextExplicit je true)
                this.Is.SelectAllText = value ?? false;              // Pokud není daná hodnota, nastavíme Explicit = false, a hodnota (Is.SelectAllText) taky false
            }
        }
        /// <summary>
        /// Přídavné vykreslení přes Background, pod text
        /// </summary>
        public ITextEditOverlay OverlayBackground { get; set; }
        /// <summary>
        /// Přídavné vykreslení přes Text
        /// </summary>
        public ITextEditOverlay OverlayText { get; set; }
        /// <summary>
        /// Obsahuje true pro prvek, jehož hodnota má být zadaná, nikoli Empty.
        /// Pak se Border vykresluje barvou 
        /// </summary>
        public bool IsRequiredValue { get; set; }
        #endregion
        #region Static deklarace chování třídy - lze změnit v rámci celé aplikace
        /// <summary>
        /// Defaultní hodnota SelectAll = vybrat celý text po příchodu focusu do prvku
        /// </summary>
        public static bool DefaultSelectAll { get; set; } = true;
        /// <summary>
        /// Definice chování: při odchodu focusu z prvku a opětovném návratu docusu se má pamatovat pozice kurzoru?
        /// Default = false = chování jako v Green (Infragistic), Notepadu, Firefox, TotalCommander (Po změně focusu se kurzor nastaví na index 0),
        /// hodnota true = chování jako v Office, Visual studio, OpenOffice (Textbox si pamatuje pozici kurzoru)
        /// </summary>
        public static bool SaveCursorPositionOnLostFocus { get; set; } = false;
        /// <summary>
        /// Definice chování: při kliknutí pravou myší (=kontextové menu) se má přemístit kurzor stejně, jako při kliknutí levou myší?
        /// Default = false = chování jako v Green (Infragistic), Notepadu, Firefox, TotalCommander (RightClick nemění pozici kurzoru),
        /// hodnota true = chování jako v Office, Visual studio, OpenOffice (RightClick změní pozici kurzoru)
        /// </summary>
        public static bool ChangeCursorPositionOnRightMouse { get; set; } = false;
        /// <summary>
        /// Definice chování: při kliknutí levou myší se stisknutým Control se má označit celé slovo pod myší?
        /// Default = false = chování jako v Green (Infragistic), Notepadu, Firefox, TotalCommander (Control+Click neoznačí slovo),
        /// hodnota true = chování jako v Office, Visual studio, OpenOffice (Control+Click označí celé slovo)
        /// </summary>
        public static bool SelectWordOnControlMouse { get; set; } = false;
        #endregion
        #region class EditorState : proměnné a funkce pro editaci platné pouze při přítomnosti Focusu
        /// <summary>
        /// EditorState : proměnné a funkce pro editaci TextBoxu, platné pouze při přítomnosti Focusu
        /// </summary>
        protected class EditorStateInfo : IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="data"></param>
            public EditorStateInfo(GTextEdit owner, string data)
            {
                _Owner = owner;
                Data = data;
            }
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                _Owner = null;
                _Text = null;
                _Chars = null;
                _Lines = null;
            }
            private GTextEdit _Owner;
            private string _Text;
            /// <summary>
            /// Pole znaků
            /// </summary>
            private CharacterPositionInfo[] _Chars;
            /// <summary>
            /// Pole řádků, v pořadí shora dolů.
            /// Jeden prvek pole = jeden řádek.
            /// Jeden řádek = Tuple, kde Item1 = pozice řádku na souřadnici Y (Int32Range.Begin = Y, Int32Range.End = Bottom); a kde Item2 = pole znaků na tomto řádku, v pořadí zleva doprava.
            /// Pole generuje metoda <see cref="LinePositionInfo.CreateLines"/>
            /// </summary>
            private LinePositionInfo[] _Lines;
            /// <summary>
            /// Data určená k perzistenci po dobu mimo editace TextBoxu, k restorování nové instance <see cref="EditorStateInfo"/> do předchozího stavu.
            /// Má jít o malý balíček dat.
            /// </summary>
            public string Data
            {
                get
                {
                    return null;
                }
                set
                { }
            }
            /// <summary>
            /// Vloží do instance dodaný text, a z dodaných souřadnic znaků vytvoří interní pole informací pro hledání atd
            /// </summary>
            /// <param name="text"></param>
            /// <param name="charPositions"></param>
            public void SetCharacterPositions(string text, RectangleF[] charPositions)
            {
                _Text = text;
                _Chars = CharacterPositionInfo.CreateChars(text, charPositions);
                _Lines = LinePositionInfo.CreateLines(_Chars);
            }
            /// <summary>
            /// Metoda je volána po myším kliknutí, připraví data pro určení pozice kurzorum, SelectedRange a následné vykreslení 
            /// </summary>
            /// <param name="e"></param>
            public void AfterMouseCursorPrepare(GInteractiveChangeStateArgs e)
            {
                MouseDownIsFocusEnter = String.Equals(e.UserData as string, ConstantFocusEnter);
                MouseDownButtons = e.MouseButtons;
                MouseDownAbsoluteLocation = e.MouseAbsolutePoint;
                MouseDownModifierKeys = e.ModifierKeys;
            }
            /// <summary>
            /// Metoda resetuje data o myším kliknutí - volá se po jejich zpracování v metodě <see cref="MouseDownDataProcess()"/>
            /// </summary>
            public void AfterMouseCursorReset()
            {
                MouseDownIsFocusEnter = null;
                MouseDownButtons = null;
                MouseDownAbsoluteLocation = null;
                MouseDownModifierKeys = null;
            }
            /// <summary>
            /// Obsahuje true po většinu života (editor by neměl existovat, když textbox nemá focus)
            /// </summary>
            public bool HasFocus { get; set; }
            /// <summary>
            /// Eviduje stav MouseDown a FocusEnter, zachycený v metodě <see cref="AfterMouseDownCursor(GInteractiveChangeStateArgs)"/>:
            /// 1. null = výchozí hodnota = není MouseDown (platí i po zpracování hodnoty <see cref="MouseDownIsFocusEnter"/> v metodě <see cref="AfterMouseCursorPrepare()"/>)
            /// 2. false = je evidován MouseDown, ale v situaci, kdy už TextBox má focus (tzn. myš je stisknuta v již aktivním TextBoxu)
            /// 3. true = je evidován MouseDown, který přivedl Focus do TextBoxu odjinud = kliknuto zvenku do TextBoxu (pak je jiné chování)
            /// </summary>
            public bool? MouseDownIsFocusEnter { get; private set; }
            public MouseButtons? MouseDownButtons { get; private set; }
            public Point? MouseDownAbsoluteLocation { get; private set; }
            public Keys? MouseDownModifierKeys { get; private set; }

            public int CursorIndex { get; private set; }
            public Rectangle? CursorBounds { get; private set; }

            /// <summary>
            /// Zpracuje požadavky z kliknutí myši, uložené v metodě <see cref="AfterMouseCursorPrepare(GInteractiveChangeStateArgs)"/>, po proběhnutí vykreslení a uložení pozic znaků.
            /// Určí pozici kurzoru CursorPosition a rozsah výběru SelectedRange.
            /// Metoda je volána v procesu Draw. Po této metodě následuje vykreslení kurzoru.
            /// </summary>
            public void MouseDownDataProcess()
            {
                if (this.MouseDownAbsoluteLocation.HasValue)
                    this.CursorIndex = FindCursorIndex(this.MouseDownAbsoluteLocation.Value);
                this.CursorBounds = GetCursorBounds(this.CursorIndex);



                AfterMouseCursorReset();
            }
            /// <summary>
            /// Vrátí pozici kurzoru (index) odpovídající danému absolutnímu bodu. 
            /// Opírá se přitom o pozice znaků, které si objekt uložil v metodě <see cref="SetCharacterPositions(string, RectangleF[])"/>.
            /// </summary>
            /// <param name="absolutePoint"></param>
            /// <returns></returns>
            public int FindCursorIndex(Point absolutePoint)
            {
                if (this._Chars == null || this._Chars.Length == 0) return 0;

                bool isRightSide;
                CharacterPositionInfo charInfo = LinePositionInfo.SearchCharacterByPosition(_Lines, absolutePoint, out isRightSide);
                if (charInfo == null) return 0;

                int cursorPosition = charInfo.Index;
                if (isRightSide) cursorPosition++;

                return cursorPosition;
            }
            /// <summary>
            /// Metoda vrátí souřadnice, na které bude vykreslován kurzor, který je na dané pozici (indexu)
            /// </summary>
            /// <param name="cursorIndex"></param>
            /// <returns></returns>
            public Rectangle? GetCursorBounds(int cursorIndex)
            {
                int charLength = (this._Chars == null ? 0 : this._Chars.Length);
                if (charLength <= 0) return null;
                CharacterPositionInfo charInfo;
                if (cursorIndex < charLength)
                {
                    charInfo = this._Chars[cursorIndex];
                    return new Rectangle(charInfo.Bounds.X, charInfo.Bounds.Y, 1, charInfo.Bounds.Height);
                }
                charInfo = this._Chars[charLength - 1];
                return new Rectangle(charInfo.Bounds.Right, charInfo.Bounds.Y, 1, charInfo.Bounds.Height);
            }
        }
        #endregion
        #region class CharacterPositionInfo a LinePositionInfo : Třída, která udržuje informace o každém jednom znaku v rámci textboxu (index, znak, souřadnice)
        /// <summary>
        /// CharacterPositionInfo: Třída, která udržuje informace o každém jednom znaku v rámci textboxu (index, znak, souřadnice)
        /// </summary>
        protected class CharacterPositionInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="index"></param>
            /// <param name="content"></param>
            /// <param name="bounds"></param>
            public CharacterPositionInfo(int index, char content, Rectangle bounds)
            {
                this.Index = index;
                this.Content = content;
                this.Bounds = bounds;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"[{Index}] : '{Content}'; Bounds: ({Bounds.X},{Bounds.X},{Bounds.Width},{Bounds.Height})";
            }
            /// <summary>
            /// Index znaku v textu (počínaje 0)
            /// </summary>
            public int Index { get; private set; }
            /// <summary>
            /// Obsaz = znak
            /// </summary>
            public char Content { get; private set; }
            /// <summary>
            /// Souřadnice tohoto znaku, v absolutní hodnotě
            /// </summary>
            public Rectangle Bounds { get; private set; }
            /// <summary>
            /// Vytvoří a vrátí pole <see cref="CharacterPositionInfo"/> pro zadaný text a souřadnice znaků
            /// </summary>
            /// <param name="text"></param>
            /// <param name="charPositions"></param>
            /// <returns></returns>
            internal static CharacterPositionInfo[] CreateChars(string text, RectangleF[] charPositions)
            {
                if (charPositions == null) return null;
                char[] items = text.ToCharArray();
                int length = (items.Length < charPositions.Length ? items.Length : charPositions.Length);
                CharacterPositionInfo[] result = new CharacterPositionInfo[length];
                for (int i = 0; i < length; i++)
                {
                    char item = items[i];
                    RectangleF charPosition = charPositions[i];
                    result[i] = new CharacterPositionInfo(i, item, Rectangle.Ceiling(charPosition));
                }
                return result;
            }
        }
        /// <summary>
        /// LinePositionInfo: Třída, která udržuje informace o každém jednom řádku v rámci textboxu (index, rozsah na ose Y, pole znaků)
        /// </summary>
        protected class LinePositionInfo
        {
            /// <summary>
            /// Index řádku v textu (počínaje 0)
            /// </summary>
            public int Index { get { return _Index; } }
            /// <summary>
            /// Souřadnice tohoto řádku = souhrn souřadnic jednotlivých znaků
            /// </summary>
            public Rectangle Bounds { get { return _Bounds; } }
            /// <summary>
            /// Pole znaků v tomto řádku
            /// </summary>
            public CharacterPositionInfo[] Chars { get { return _Chars; } }
            #region Tvorba pole řádků, včetně privátních metod a proměnných
            /// <summary>
            /// Vytvoří a vrátí pole jednotlivých řádků
            /// </summary>
            /// <param name="chars"></param>
            /// <returns></returns>
            internal static LinePositionInfo[] CreateLines(CharacterPositionInfo[] chars)
            {
                List<LinePositionInfo> lines = new List<LinePositionInfo>();
                foreach (var c in chars)
                {
                    LinePositionInfo line = lines.FirstOrDefault(l => l._IsCharOnLine(c));
                    if (line == null)
                    {
                        line = new LinePositionInfo();
                        lines.Add(line);
                    }
                    line._AddChar(c);
                }
                if (lines.Count > 1) lines.Sort((a, b) => a._Top.CompareTo(b._Top));
                int index = 0;
                lines.ForEach(l => l._Finalize(ref index));
                return lines.ToArray();
            }
            private bool _IsCharOnLine(CharacterPositionInfo c)
            {
                return (c.Bounds.Height > 0 && c.Bounds.Bottom > _Top && c.Bounds.Top < _Bottom);
            }
            private void _AddChar(CharacterPositionInfo c)
            {
                Rectangle bounds = c.Bounds;
                bool isFirst = (_CharList.Count == 0);
                if (isFirst ||_Left > bounds.Left) _Left = bounds.Left;
                if (isFirst || _Right < bounds.Right) _Right = bounds.Right;
                if (isFirst || _Top > bounds.Top) _Top = bounds.Top;
                if (isFirst || _Bottom < bounds.Bottom) _Bottom = bounds.Bottom;
                _CharList.Add(c);
            }
            private void _Finalize(ref int index)
            {
                _Index = index++;
                _Bounds = Rectangle.FromLTRB(_Left, _Top, _Right, _Bottom);
                _Chars = _CharList.ToArray();
                _CharList = null;
            }
            private LinePositionInfo()
            {
                _Left = _Right = _Top = _Bottom = 0;
                _CharList = new List<CharacterPositionInfo>();
            }
            private int _Index;
            private int _Left;
            private int _Right;
            private int _Top;
            private int _Bottom;
            private Rectangle _Bounds;
            private List<CharacterPositionInfo> _CharList;
            private CharacterPositionInfo[] _Chars;
            #endregion
            #region Vyhledání řádku a znaku podle pozice
            /// <summary>
            /// Najde a vrátí znak, který nejblíže odpovídá danému bodu
            /// </summary>
            /// <param name="lines"></param>
            /// <param name="point"></param>
            /// <param name="isRightSide"></param>
            /// <returns></returns>
            public static CharacterPositionInfo SearchCharacterByPosition(LinePositionInfo[] lines, Point point, out bool isRightSide)
            {
                isRightSide = false;

                if (lines == null) return null;
                int lineCount = lines.Length;
                if (lineCount == 0) return null;

                // 1. Najdeme řádek:
                LinePositionInfo lineInfo = null;
                if (lineCount == 1 || point.Y < lines[0].Bounds.Bottom)
                {   // Zkratka:
                    lineInfo = lines[0];
                }
                else
                {   // Plné hledání:
                    for (int i = 0; i < lineCount; i++)
                    {   // Projdu všechny řádky:
                        lineInfo = lines[i];
                        if (i == (lineCount - 1)) break;             // Pokud jsem dosud nenašel, a mám poslední řádek, pak on je tím hledaným prvkem!
                        int y = lineInfo.Bounds.Bottom;              // Dolní Y našeho řádku (line)
                        int yn = lines[i + 1].Bounds.Top;            // Horní Y následujícího řádku [+1]
                        if (yn > y) y += ((yn - y) / 2);             // Pokud následující řádek je pod koncem našeho řádku (tj. je mezi nimi mezera), pak zpracuji i horní polovinu mezery!
                        if (point.Y < y) break;                      // Pokud hledaný bod leží nad dolním okrajem řádku (případně včetně půlmezery pod naším řádkem), pak hledaný řádek je on
                    }
                }

                // Znaky v řádku:
                CharacterPositionInfo[] chars = lineInfo.Chars;
                if (chars == null) return null;
                int charCount = chars.Length;
                if (charCount == 0) return null;

                // 2. V řádku najdeme znak:
                CharacterPositionInfo charInfo = null;
                if (charCount == 1 || point.X < chars[0].Bounds.Right)
                {   // Zkratka:
                    charInfo = chars[0];
                }
                else
                {   // Plné hledání:
                    for (int i = 0; i < charCount; i++)
                    {   // Projdu všechny znaky:
                        charInfo = chars[i];
                        if (i == (charCount - 1)) break;             // Pokud jsem dosud nenašel, a mám poslední znak, pak on je tím hledaným prvkem!
                        int x = charInfo.Bounds.Right;               // Pravé X našeho znaku
                        int xn = chars[i + 1].Bounds.Left;           // Levé X následujícího znaku [+1]
                        if (xn > x) x += ((xn - x) / 2);             // Pokud následující znak je pod koncem našeho znaku (tj. je mezi nimi mezera), pak zpracuji i levou polovinu mezery!
                        if (point.X < x) break;                      // Pokud hledaný bod leží před pravým okrajem znaku (případně včetně půlmezery za naším znakem), pak hledaný znak je on
                    }
                }

                // 3. Pravá polovina znaku:
                int half = charInfo.Bounds.X + (charInfo.Bounds.Width / 2);
                isRightSide = (point.X >= half);

                return charInfo;
            }
            #endregion
        }
        #endregion
    }
    #region class GTextEditDrawArgs : Argumenty pro kreslení
    /// <summary>
    /// Argumenty pro kreslení
    /// </summary>
    public class GTextEditDrawArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="drawArgs"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="innerBounds"></param>
        /// <param name="drawMode"></param>
        /// <param name="hasFocus"></param>
        /// <param name="interactiveState"></param>
        /// <param name="textEdit"></param>
        public GTextEditDrawArgs(GInteractiveDrawArgs drawArgs, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, Rectangle innerBounds, DrawItemMode drawMode, 
            bool hasFocus, GInteractiveState interactiveState, GTextEdit textEdit)
        {
            this.DrawArgs = drawArgs;
            this.AbsoluteBounds = absoluteBounds;
            this.AbsoluteVisibleBounds = absoluteVisibleBounds;
            this.InnerBounds = innerBounds;
            this.TextBounds = innerBounds;
            this.DrawMode = drawMode;
            this.HasFocus = hasFocus;
            this.InteractiveState = interactiveState;
            this.TextEdit = textEdit;
        }
        /// <summary>
        /// Data pro kreslení
        /// </summary>
        public GInteractiveDrawArgs DrawArgs { get; private set; }
        /// <summary>
        /// Grafika
        /// </summary>
        public Graphics Graphics { get { return this.DrawArgs.Graphics; } }
        /// <summary>
        /// Plné absolutní souřadnice
        /// </summary>
        public Rectangle AbsoluteBounds { get; private set; }
        /// <summary>
        /// Viditelné absolutní souřadnice
        /// </summary>
        public Rectangle AbsoluteVisibleBounds { get; private set; }
        /// <summary>
        /// Vnitřní souřadnice pro kreslení pozadí textboxu = pod text, i pod overlay, ale bez rámečku.
        /// </summary>
        public Rectangle InnerBounds { get; private set; }
        /// <summary>
        /// Vnitřní souřadnice pro kreslení textu, bez rámečku.
        /// Pokud overlay typu <see cref="GTextEdit.OverlayText"/> potřebuje, může tento prostor modifikovat = zmenšit tak, aby text nezasahoval např. do ikony Overlay.
        /// </summary>
        public Rectangle TextBounds { get; private set; }
        /// <summary>
        /// Prostor pro výhradní kreslení Overlay. Výchozí hodnota je null. Je plně ve správě objektu <see cref="GTextEdit.OverlayText"/>.
        /// </summary>
        public Rectangle? OverlayBounds { get; private set; }
        /// <summary>
        /// Režim kreslení
        /// </summary>
        public DrawItemMode DrawMode { get; private set; }
        /// <summary>
        /// Máme Focus
        /// </summary>
        public bool HasFocus { get; private set; }
        /// <summary>
        /// Interaktivní stav
        /// </summary>
        public GInteractiveState InteractiveState { get; private set; }
        /// <summary>
        /// Textový objekt
        /// </summary>
        public GTextEdit TextEdit { get; private set; }
        /// <summary>
        /// Vloží dané souřadnice <paramref name="overlayBounds"/> do <see cref="OverlayBounds"/>,
        /// a pokud bude zadán parametr <paramref name="textBounds"/> (tj. nebude null), bude vložen do <see cref="InnerBounds"/>.
        /// Používá se typicky z metody <see cref="ITextEditOverlay.DetectOverlayBounds(GTextEditDrawArgs)"/>.
        /// </summary>
        /// <param name="overlayBounds"></param>
        /// <param name="textBounds"></param>
        public void SetOverlayBounds(Rectangle? overlayBounds, Rectangle? textBounds = null)
        {
            this.OverlayBounds = overlayBounds;
            if (textBounds.HasValue) this.TextBounds = textBounds.Value;
        }
    }
    #endregion
}
