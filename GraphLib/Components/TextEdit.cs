using System;
using System.Collections.Generic;
using System.Drawing;
using D2D = System.Drawing.Drawing2D;
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
            OnCreateEditor();
            EditorState.EventFocusEnter(e);
        }
        protected override void AfterStateChangedFocusLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusLeave(e);
            EditorState.EventFocusLeave(e);
            OnReleaseEditor();
        }
        protected override void AfterStateChangedKeyPreview(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedKeyPreview(e);
            EditorState.EventKeyPreview(e);
        }
        protected override void AfterStateChangedKeyDown(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedKeyDown(e);
            EditorState.EventKeyDown(e);
        }
        protected override void AfterStateChangedKeyPress(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedKeyPress(e);
            EditorState.EventKeyPress(e);
        }
        protected override void AfterStateChangedMouseLeftDown(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseLeftDown(e);
            EditorState.EventMouseLeftDown(e);
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
        #region EditorState : Instance editoru
        /// <summary>
        /// Třída která slouží pro support editace v TextBoxu.
        /// Instance existuje pouze v době, kdy TextBox má Focus, při ztrátě focusu instance zaniká, a po návratu Focusu se vygeneruje instance nová.
        /// Šetří se tak výrazně paměť, protože TextBox v mimoeditační době nepotřebuje téměř žádné další proměnné nad rámec <see cref="InteractiveObject"/>.
        /// V době, kdy hodnota <see cref="InteractiveObject.HasFocus"/> je false je <see cref="EditorState"/> = null.
        /// Vytváření editoru probíhá automaticky v případě potřeby, v metodě <see cref="OnCreateEditor()"/>, spolu s eventem <see cref="CreateEditor"/>.
        /// </summary>
        public EditorStateInfo EditorState
        {
            get
            {
                if (_EditorState == null && this.HasFocus) OnCreateEditor();
                return _EditorState;
            }
            set { _EditorState = value; }
        }
        private EditorStateInfo _EditorState;
        /// <summary>
        /// Metoda, která má vytvořit editor do property <see cref="EditorState"/>.
        /// Vyvolá event <see cref="CreateEditor"/>, a pokud ten nevytvoří editor, pak metoda vytvoří vlastní defaultní editor.
        /// </summary>
        protected virtual void OnCreateEditor()
        {
            CreateEditor?.Invoke(this, EventArgs.Empty);                       // Event?
            if (_EditorState != null) return;                                  // Pokud je hotovo (tj. eventhandler vytvořil editor vlastními silami), pak je hotovo :-)
            _EditorState = new EditorStateInfo(this, EditorStateData);         // Vygenerujeme defaultní editor
        }
        /// <summary>
        /// Událost, kdy TextBox potřebuje editor, nemá jej a bude si jej vytvářet.
        /// Pokud uživatelský kód chce dodat vlastní editor, pak v tomto eventu jej vytvoří a vloží do property <see cref="EditorState"/>. 
        /// V tom případě se nebude vytvářet editor defaultní.
        /// Uživatelský kód může persistovat svoje data v property <see cref="EditorUserData"/>.
        /// </summary>
        public event EventHandler CreateEditor;
        /// <summary>
        /// Metoda, která uvolňuje editor z paměti. Před tím z něj může uložit data.
        /// </summary>
        protected virtual void OnReleaseEditor()
        {
            EditorStateData = _EditorState?.Data;
            ReleaseEditor?.Invoke(this, EventArgs.Empty);                      // Event?
            if (_EditorState != null)
                _EditorState.Dispose();
            _EditorState = null;
        }
        /// <summary>
        /// Událost, kdy TextBox zahazuje editor (ten ještě existuje), protože odchází Focus.
        /// Editor existuje pouze v době přítomnosti Focusu.
        /// Pokud uživatelský kód chce uložit extra data ze svého editoru, pak v tomto eventu má možnost.
        /// Pro uložení je vhodné použít property <see cref="EditorUserData"/>.
        /// </summary>
        public event EventHandler ReleaseEditor;
        /// <summary>
        /// String, který uchovává klíčová data z <see cref="EditorStateInfo"/> v době, kdy textbox nemá focus a instance v <see cref="EditorState"/> je zahozena (v LostFocus).
        /// Z tohoto stringu je při návratu Focusu do this TextBoxu vytvořen (restorován) nový objekt <see cref="EditorStateInfo"/>.
        /// </summary>
        protected string EditorStateData;
        /// <summary>
        /// Prostor pro persistenci dat externího editoru
        /// </summary>
        public object EditorUserData { get; set; }
        #endregion
        #region Clipboard
        public void ClipboardAction(ClipboardActionType action)
        { }
        public void UndoRedoAction(UndoRedoActionType action)
        { }
        public virtual void SpecialKeyAction(KeyEventArgs args)
        { }
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
                if (HasFocus && EditorState != null)
                {   // Mám Focus a mám i editor = získám souřadnice znaků, vykreslím je, zpracuji údaje z myši a vykreslím výběr a kurzor:
                    if (text.Length == 0) text = " ";              // Kvůli souřadnicím
                    RectangleF[] charPositions = GPainter.DrawStringMeasureChars(drawArgs.Graphics, text, this.CurrentFont, textBounds, alignment, color: this.CurrentTextColor, stringFormat: stringFormat);
                    EditorState.SetCharacterPositions(text, charPositions);
                    EditorState.MouseDownDataProcess();
                    DrawSelectionAndCursor(drawArgs);
                }
                else
                {   // Bez Focusu nebo bez editoru = jen vypíšu text:
                    GPainter.DrawString(drawArgs.Graphics, text, this.CurrentFont, textBounds, alignment, color: this.CurrentTextColor, stringFormat: stringFormat);
                }
            }

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
            using (var selectionArea = this.EditorState.GetSelectionArea())
            {   // selectionArea je GraphicsPath, IDisposable:
                if (selectionArea != null)
                    drawArgs.Graphics.FillPath(Skin.Brush(Color.LightBlue, 128), selectionArea);
            }

            var cursorBounds = EditorState.GetCursorBounds();
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
        #region Public vlastnosti definující chování
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
        #region Text, Value a napojení na data
        /// <summary>
        /// Vykreslovaný text.
        /// Pokud bude vložena hodnota null, bude se číst jako prázdný string.
        /// </summary>
        public override string Text
        {
            get
            {
                return base.Text;
            }

            set
            {
                base.Text = value;
            }
        }
        /// <summary>
        /// Hodnota uložená v TextBoxu
        /// </summary>
        public object Value
        {
            get
            {
                return (DataSourceValid ? _DataSourceTable.Rows[_DataSourceRowIndex][_DataSourceColumnName] : _Value);
            }
            set
            {
                if (DataSourceValid)
                    _DataSourceTable.Rows[_DataSourceRowIndex][_DataSourceColumnName] = value;
                _Value = value;
                Invalidate();
            }
        }
        private object _Value;
        /// <summary>
        /// Datový zdroj = tabulka
        /// </summary>
        public System.Data.DataTable DataSourceTable
        {
            get { return _DataSourceTable; }
            set { _DataSourceTable = value; Invalidate(); }
        }
        private System.Data.DataTable _DataSourceTable;
        /// <summary>
        /// Datový zdroj = index řádku
        /// </summary>
        public int DataSourceRowIndex
        {
            get { return _DataSourceRowIndex; }
            set { _DataSourceRowIndex = value; Invalidate(); }
        }
        private int _DataSourceRowIndex;
        /// <summary>
        /// /// <summary>
        /// Datový zdroj = název sloupce v tabulce
        /// </summary>
        /// </summary>
        public string DataSourceColumnName
        {
            get { return _DataSourceColumnName; }
            set { _DataSourceColumnName = value; Invalidate(); }
        }
        private string _DataSourceColumnName;
        /// <summary>
        /// Aktuální hodnota v odpovídající buňce tabulky v datovém zdroji.
        /// Pokud zdroj není platný, čtení i zápis vyhodí chybu.
        /// </summary>
        public object DataSourceValue
        {
            get
            {
                if (!DataSourceValid)
                    throw new InvalidOperationException("GTextEdit.DataSourceValue get error: DataSource is not Valid.");
                return _DataSourceTable.Rows[_DataSourceRowIndex][_DataSourceColumnName];
            }
            set
            {
                if (!DataSourceValid)
                    throw new InvalidOperationException("GTextEdit.DataSourceValue set error: DataSource is not Valid.");
                _DataSourceTable.Rows[_DataSourceRowIndex][_DataSourceColumnName] = value;
            }
        }
        /// <summary>
        /// Obsahuje true, pokud datový zdroj je platný (tzn. lze načíst hodnotu z tabulky: object value = <see cref="DataSourceTable"/>.Rows[<see cref="DataSourceRowIndex"/>][<see cref="DataSourceColumnName"/>].
        /// </summary>
        protected bool DataSourceValid
        {
            get
            {
                if (_DataSourceTable == null || _DataSourceRowIndex < 0 || String.IsNullOrEmpty(_DataSourceColumnName)) return false;
                if (_DataSourceRowIndex >= _DataSourceTable.Rows.Count) return false;
                if (!_DataSourceTable.Columns.Contains(_DataSourceColumnName)) return false;
                return true;
            }
        }
        #endregion
        #region class EditorState : proměnné a funkce pro editaci platné pouze při přítomnosti Focusu
        /// <summary>
        /// EditorState : proměnné a funkce pro editaci TextBoxu, platné pouze při přítomnosti Focusu
        /// </summary>
        public class EditorStateInfo : IDisposable
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
            /// <summary>
            /// Vlastník = TextBox
            /// </summary>
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
            /// Vlastník = TextBox
            /// </summary>
            protected GTextEdit Owner { get { return _Owner; } }
            /// <summary>
            /// Obsahuje true pokud daný text lze editovat, false pokud je Disabled nebo ReadOnly.
            /// </summary>
            protected bool IsEditable { get { return (!_Owner.ReadOnly && _Owner.Enabled); } }
            /// <summary>
            /// Zajistí, že this objekt bude při nejbližší možnosti překrelsen. Neprovádí překreslení okamžitě, na rozdíl od metody <see cref="InteractiveObject.Refresh()"/>.
            /// </summary>
            protected void Invalidate()
            {
                this._Owner.Invalidate();
            }
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
            /// Hodnota <see cref="GTextObject.Text"/>. Tato hodnota nikdy není null.
            /// </summary>
            protected string Text { get { return this._Owner.Text; } set { this._Owner.Text = value; } }
            /// <summary>
            /// Hodnota <see cref="GTextEdit.SelectAllText"/>.Value
            /// </summary>
            protected bool SelectAllText { get { return this._Owner.SelectAllText.Value; } }
            /// <summary>
            /// Rozsah vybraného (označeného) textu. Může být NULL.
            /// Pokud zobrazený text má 4 znaky "0123", a <see cref="SelectionRange"/> = { Begin: 1; End: 3; }, pak jsou označeny znaky "12".
            /// Pokud zobrazený text má 4 znaky "0123", a <see cref="SelectionRange"/> = { Begin: 0; End: 4; }, pak jsou označeny znaky "0123".
            /// <para/>
            /// Pozor, pořadí Begin a End může být i opačné = záporné: { Begin = 4; End = 0 }!
            /// Z hlediska vykreslení je to shodné jako { 0, 4 }, ale rozdál je v modifikaci hodnoty pomocí kurzoru nebo myši:
            /// Begin je místo, kde uživatel začal označovat, od něj může jít doprava i doleva, mění se End, proto může být End menší než Begin.
            /// Můžeme například myší kliknout na pozici <see cref="CursorIndex"/> = 10, stisknout myš, a pak kliknout myší na pozici 4 a <see cref="SelectionRange"/> bude { Begin = 10, End = 4 }.
            /// <para/>
            /// Pozor, hodnota smí být NULL!
            /// </summary>
            protected Int32Range SelectionRange
            {
                get { return _SelectionRange; }
                private set { _SelectionRange = value; }
            }
            private Int32Range _SelectionRange;
            /// <summary>
            /// Obsahuje true, pokud je výběr prázdný (tzn. <see cref="SelectionRange"/> je null, nebo jeho Size je 0)
            /// </summary>
            protected bool SelectionRangeIsEmpty { get { var r = SelectionRange; return (r == null || r.Size == 0); } }
            /// <summary>
            /// Vloží do instance dodaný text a dodané souřadnice, a z dodaných souřadnic znaků vytvoří interní pole informací pro hledání znaků i řádků i pro jejich následné vykreslování
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
            /// Zpracuje požadavky z kliknutí myši, uložené v metodě <see cref="EventMouseLeftDown(GInteractiveChangeStateArgs)"/>, po proběhnutí vykreslení a po uložení pozic znaků.
            /// Určí pozici kurzoru CursorPosition a rozsah výběru SelectedRange.
            /// Metoda je volána v procesu Draw. Po této metodě následuje vykreslení kurzoru.
            /// </summary>
            public void MouseDownDataProcess()
            {
                if (MouseDownAbsoluteLocation.HasValue)
                {   // Máme data o stisku myši:
                    bool isFocusEnter = (MouseDownIsFocusEnter.HasValue && MouseDownIsFocusEnter.Value);
                    MouseButtons mouseButtons = MouseDownButtons ?? MouseButtons.None;
                    Keys modifiers = MouseDownModifierKeys ?? Keys.None;
                    if (mouseButtons == MouseButtons.Left)
                    {   // Levá = běžná myš:
                        if (isFocusEnter && SelectAllText)
                        {   // Byla stisknuta levá myš, a tímto stiskem myši vstupuje Focus do TextBoxu => jde o první klik v objektu, pak v případě SelectAllText = true je akce jednoznačná:
                            // Vybereme celý text: naplnění SelectionRange bylo provedeno v metodě this.FocusEnter(), tady jen určíme pozici kurzoru = na konci textu:
                            CursorIndex = Text.Length;
                        }
                        else if (modifiers == Keys.Control && SelectWordOnControlMouse)
                        {   // Byla stisknuta levá myš myš; buď jako příchod do prvku (FocusEnter) ale bez SelectAllText; anebo bez příchodu focusu. 
                            // Je stisknut Control (bez Shiftu) a ten Control se má interpretovat jako "Označ slovo pod myší" => jdeme na to:
                            bool isRightSide;
                            int cursorIndex = SearchCharIndex(MouseDownAbsoluteLocation.Value, out isRightSide);
                            Int32Range wordRange = SearchNearWord(cursorIndex, isRightSide);
                            if (wordRange != null)
                            {
                                this.SelectionRange = wordRange;
                                this.CursorIndex = wordRange.End;
                            }
                            else
                            {
                                this.SelectionRange = null;
                                this.CursorIndex = cursorIndex;
                            }
                        }
                        else if (modifiers == Keys.Shift)
                        {   // Byla stisknuta myš; buď jako příchod do prvku (FocusEnter) ale bez SelectAllText; anebo bez příchodu focusu. 
                            // Je stisknut Shift => měli bychom označit část textu od počátku výběru / nebo od pozice stávajícího kurzoru do pozice myši:
                            int cursorIndex = SearchCharIndex(MouseDownAbsoluteLocation.Value);
                            Int32Range selectionRange = SelectionRange;
                            if (selectionRange != null)
                                // Máme již z dřívějška uložen nějaký výběr: ponecháme jeho Begin a změníme End:
                                selectionRange = new Int32Range(selectionRange.Begin, cursorIndex);
                            else
                                // Dosud nebyl výběr: vezmeme jako počátek nového výběru dosavadní kurzor:
                                selectionRange = new Int32Range(this.CursorIndex, cursorIndex);
                            this.SelectionRange = selectionRange;
                            this.CursorIndex = cursorIndex;
                        }
                        else
                        {   // Byla stisknuta myš; buď jako příchod do prvku (FocusEnter) ale bez SelectAllText; anebo bez příchodu focusu. 
                            // Není stisknuto nic => zrušíme případný výběr Selection, a najdeme pozici kurzoru podle pozice myši:
                            this.SelectionRange = null;
                            this.CursorIndex = SearchCharIndex(this.MouseDownAbsoluteLocation.Value);
                        }
                    }
                    else if (MouseDownButtons.HasValue && MouseDownButtons.Value == MouseButtons.Right)
                    {   // Pravá myš => Kontextové menu: pokud je povoleno v proměnné ChangeCursorPositionOnRightMouse, a není označen žádný text,
                        //  pak se nastaví kurzor podle pozice myši, ale neprovádí se žádná jiná funkcionalita.
                        //  Pokud je nějaký text označen, nechává se beze změny jak SelectionRange, tak CursorIndex = aby se k tomu mohlo vztahovat kontextové menu:
                        if (ChangeCursorPositionOnRightMouse && this.SelectionRangeIsEmpty)
                        {
                            this.CursorIndex = SearchCharIndex(this.MouseDownAbsoluteLocation.Value);
                        }
                    }
                }

                MouseDownDataReset();
            }
            /// <summary>
            /// Metoda resetuje data o myším kliknutí - volá se po jejich zpracování v metodě <see cref="MouseDownDataProcess()"/>
            /// </summary>
            private void MouseDownDataReset()
            {
                MouseDownIsFocusEnter = null;
                MouseDownButtons = null;
                MouseDownAbsoluteLocation = null;
                MouseDownModifierKeys = null;
            }
            /// <summary>
            /// Obsahuje true po většinu života (editor by neměl existovat, když textbox nemá focus)
            /// </summary>
            public bool HasFocus { get; private set; }
            /// <summary>
            /// Eviduje stav MouseDown a FocusEnter, zachycený v metodě <see cref="EventMouseLeftDown(GInteractiveChangeStateArgs)"/>:
            /// 1. null = výchozí hodnota = není MouseDown (platí i po zpracování hodnoty <see cref="MouseDownIsFocusEnter"/> v metodě <see cref="EventMouseLeftDown(GInteractiveChangeStateArgs)"/>)
            /// 2. false = je evidován MouseDown, ale v situaci, kdy už TextBox má focus (tzn. myš je stisknuta v již aktivním TextBoxu)
            /// 3. true = je evidován MouseDown, který přivedl Focus do TextBoxu odjinud = kliknuto zvenku do TextBoxu (pak je jiné chování)
            /// </summary>
            public bool? MouseDownIsFocusEnter { get; private set; }
            /// <summary>
            /// Buttony myši stisknuté při MouseDown
            /// </summary>
            public MouseButtons? MouseDownButtons { get; private set; }
            /// <summary>
            /// Absolutní souřadnice myši při MouseDown (absolutní - vzhledem k Host controlu) 
            /// </summary>
            public Point? MouseDownAbsoluteLocation { get; private set; }
            /// <summary>
            /// Modifikační klávesy aktivní při MouseDown
            /// </summary>
            public Keys? MouseDownModifierKeys { get; private set; }
            /// <summary>
            /// Pozice kurzoru = před kterým znakem kurzor stojí.
            /// Hodnota 0 = kurzor je na začátku textu = před prvním znakem.
            /// Hodnota <see cref="Text"/>.Length = kurzor je na konci textu = za posledním znakem
            /// </summary>
            public int CursorIndex { get; private set; }

            /// <summary>
            /// Vrátí pozici kurzoru (index) odpovídající danému absolutnímu bodu. 
            /// Opírá se přitom o pozice znaků, které si objekt uložil v metodě <see cref="SetCharacterPositions(string, RectangleF[])"/>.
            /// <para/>
            /// Tato varianta metody vrátí index následujícího znaku v případě, kdy daný bod bude na pravém okraji určitého znaku.
            /// Naproti tomu přetížení s out parametrem bool isRightSide neposune nalezený index doprava, ale vrátí informaci o umístění bodu v pravé polovině znaku.
            /// </summary>
            /// <param name="point">Zadaný bod, k němuž hledáme znak na nejbližší pozici</param>
            /// <returns></returns>
            public int SearchCharIndex(Point point)
            {
                if (this._Chars == null || this._Chars.Length == 0) return 0;

                bool isRightSide;
                int cursorPosition = SearchCharIndex(point, out isRightSide);
                if (isRightSide) cursorPosition++;

                return cursorPosition;
            }
            /// <summary>
            /// Vrátí pozici kurzoru (index) odpovídající danému absolutnímu bodu. 
            /// Opírá se přitom o pozice znaků, které si objekt uložil v metodě <see cref="SetCharacterPositions(string, RectangleF[])"/>.
            /// <para/>
            /// Tato varianta metody vrátí přesnou pozici kurzoru na nalezeném znaku i když daný bod bude na jeho pravém okraji, 
            /// nastaví však informaci o pravé polovině do out parametru <paramref name="isRightSide"/>.
            /// </summary>
            /// <param name="point">Zadaný bod, k němuž hledáme znak na nejbližší pozici</param>
            /// <param name="isRightSide">Out: zadaný bod leží v pravé polovině nalezeného znaku</param>
            /// <returns></returns>
            public int SearchCharIndex(Point point, out bool isRightSide)
            {
                isRightSide = false;
                if (this._Chars == null || this._Chars.Length == 0) return 0;

                CharacterPositionInfo charInfo = LinePositionInfo.SearchCharacterByPosition(_Lines, point, out isRightSide);
                if (charInfo == null) return 0;

                int cursorPosition = charInfo.Index;
                return cursorPosition;
            }
            /// <summary>
            /// V aktuálním textu najde a vrátí rozsah slova, ve kterém se nachází kurzor na dané pozici.
            /// Tedy: je dán index znaku, před kterým se nachází kurzor.
            /// Tato metoda najde počátek a konec slova, ve kterém se nachází daný znak.
            /// Může vrátit null, pokud text neobsahuje žádné slovo.
            /// </summary>
            /// <param name="charIndex"></param>
            /// <param name="isRightSide"></param>
            /// <returns></returns>
            public Int32Range SearchNearWord(int charIndex, bool? isRightSide = null)
            {
                Int32Range wordRange;
                if (TrySearchNearWord(_Chars, charIndex, out wordRange, isRightSide)) return wordRange;
                return null;
            }
            /// <summary>
            /// Metoda vrátí souřadnice, na které bude vykreslován kurzor na své aktuální pozici (indexu).
            /// Pokud neexistují znaky, pak vrátí null.
            /// </summary>
            /// <returns></returns>
            public Rectangle? GetCursorBounds()
            {
                return GetCursorBounds(this.CursorIndex);
            }
            /// <summary>
            /// Metoda vrátí souřadnice, na které bude vykreslován kurzor, který je na dané pozici (indexu).
            /// Pokud neexistují znaky, pak vrátí null.
            /// Pokud je zadána pozice kurzoru mimo rozsah znaků, vrátí pozici na nejbližším okraji krajního znaku.
            /// </summary>
            /// <param name="cursorIndex"></param>
            /// <returns></returns>
            public Rectangle? GetCursorBounds(int cursorIndex)
            {
                int charLength = (this._Chars != null ? this._Chars.Length : 0);
                if (charLength <= 0) return null;
                CharacterPositionInfo charInfo;
                if (cursorIndex < charLength)
                {
                    charInfo = this._Chars[(cursorIndex >= 0 ? cursorIndex : 0)];
                    return new Rectangle(charInfo.Bounds.X, charInfo.Bounds.Y, 1, charInfo.Bounds.Height);
                }
                charInfo = this._Chars[charLength - 1];
                return new Rectangle(charInfo.Bounds.Right, charInfo.Bounds.Y, 1, charInfo.Bounds.Height);
            }
            /// <summary>
            /// Metoda vrátí souřadnice, které odpovídají aktuálně vybraným znakům. Metoda může vrátit null.
            /// </summary>
            /// <returns></returns>
            public D2D.GraphicsPath GetSelectionArea() { return GetSelectionArea(this.SelectionRange); }
            /// <summary>
            /// Metoda vrátí souřadnice, které odpovídají vybraným znakům v daném rozsahu. Metoda může vrátit null.
            /// </summary>
            /// <param name="selectionRange">Rozsah vybraných znaků</param>
            /// <returns></returns>
            public D2D.GraphicsPath GetSelectionArea(Int32Range selectionRange)
            {
                if (selectionRange == null || selectionRange.Size == 0) return null;
                int charLength = (this._Chars != null ? this._Chars.Length : 0);
                if (charLength <= 0) return null;

                int begin = (selectionRange.Begin < selectionRange.End ? selectionRange.Begin : selectionRange.End);        // Index znaku, kde výběr začíná
                begin = (begin < 0 ? 0 : (begin >= charLength ? charLength - 1 : begin));                                   // Počátek je v rozmezí 0 (včetně) až charLength-1 (včetně)
                int end = (selectionRange.End > selectionRange.Begin ? selectionRange.End : selectionRange.Begin);          // End = index prvního znaku, kde už Selection není. 
                end = (end < 0 ? 0 : (end > charLength ? charLength : end)) - 1;                                            // end = index posledního znaku zahrnutého v Area (end = End - 1)
                if (end < begin) return null;                                                                               // end může být == begin, pak je v Selection ten jediný znak

                var charBegin = _Chars[begin];
                var charEnd = _Chars[end];

                if (charBegin.Line.Index == charEnd.Line.Index)
                {   // Zkratka pro jednořádkové texty:
                    RectangleF bounds = RectangleF.FromLTRB(charBegin.Bounds.Left, charBegin.Line.Bounds.Top, charEnd.Bounds.Right, charBegin.Line.Bounds.Bottom);
                    D2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddRectangle(bounds);
                    return path;
                }


                return null;

            }
            #region Navázání editoru na události TextBoxu
            /// <summary>
            /// Je voláno při vstupu Focusu do TextBoxu. Zajistí SelectAll, pokud má být provedeno.
            /// </summary>
            public virtual void EventFocusEnter(GInteractiveChangeStateArgs e)
            {
                // Příchod Focusu do Textboxu nastaví příznak v argumentu (e.UserData), že následující MouseDown (pokud přijde) se má chovat jinak, viz EventMouseLeftDown(GInteractiveChangeStateArgs)
                e.UserData = ConstantFocusEnter;                     // Příznak využívaný v události EventMouseDown, informuje o tom, že MouseDown je společný s FocusEnter
                HasFocus = true;
                if (this.SelectAllText)
                {
                    SelectionRange = new Int32Range(0, Text.Length);
                }

                // Při vstupu do textboxu neměníme pozici kurzoru CursorIndex. To proto, že 0 je default, a na 0 ji nastavujeme v EventFocusLeave.
                // Takže defaultně se po odchodu z textboxu a následném příchodu focusu zobrazí kurzor na pozici 0 (podle nastavení).
                // Pokud ale aplikační kód nastaví CursorIndex na konkrétní hodnotu, pak tato hodnota bude platná okamžitě (pokud textbox má focus) anebo po příchodu do prvku.
            }
            /// <summary>
            /// Je voláno při odchodu Focusu z TextBoxu
            /// </summary>
            /// <param name="e"></param>
            public virtual void EventFocusLeave(GInteractiveChangeStateArgs e)
            {
                HasFocus = false;

                // Pozice kurzoru: pokud si pozici NEMÁME pamatovat, tak ji nulujeme při LostFocus:
                if (!GTextEdit.SaveCursorPositionOnLostFocus)
                    this.CursorIndex = 0;                            // Defaultní chování: po opětovném návratu focusu do prvku bude kurzor na začátku.
            }
            /// <summary>
            /// Je voláno pro zjištění, zda stisknutá klávesa bude zpracovávána v controlu
            /// </summary>
            /// <param name="e"></param>
            public virtual void EventKeyPreview(GInteractiveChangeStateArgs e)
            {
                IsKeyHandled = false;
                var args = e.KeyboardPreviewArgs;
                if (IsCursorKey(args))
                    args.IsInputKey = true;
            }
            /// <summary>
            /// Je voláno po KeyDown klávesy
            /// </summary>
            /// <param name="e"></param>
            public virtual void EventKeyDown(GInteractiveChangeStateArgs e)
            {
                var args = e.KeyboardEventArgs;
                if (IsKeyHandled ||
                    ProcessKeyCursor(args) ||
                    ProcessKeyBackspaceDel(args) ||
                    ProcessKeyClipboard(args) ||
                    ProcessKeyUndoRedo(args) ||
                    ProcessKeySpecial(args))
                    args.Handled = true;

                IsKeyHandled |= args.Handled;
            }
            /// <summary>
            /// Je voláno po stisku a uvolnění klávesy
            /// </summary>
            /// <param name="e"></param>
            public virtual void EventKeyPress(GInteractiveChangeStateArgs e)
            {
                var args = e.KeyboardPressEventArgs;
                if (IsKeyHandled ||
                    ProcessKeyFocus(args) ||
                    ProcessKeyText(args))
                    args.Handled = true;

                IsKeyHandled |= args.Handled;
            }





            /// <summary>
            /// Metoda je volána po myším kliknutí, připraví data pro určení pozice kurzorum, SelectedRange a následné vykreslení.
            /// Po myším kliknutí následuje nové vykreslení TextBoxu, tedy jeho metoda <see cref="GTextEdit.DrawText(GTextEditDrawArgs)"/>, 
            /// z které se vyvolá zdejší metoda <see cref="SetCharacterPositions(string, RectangleF[])"/> = kde se uloží pozice jednotlivých znaků i řádků,
            /// a následně se pak volá zdejší metoda <see cref="MouseDownDataProcess()"/>, která na základě dat MouseDown určí pozici kurzoru a SelectedRange.
            /// Pokud je ale TextBox vykreslován bez předchozí události MouseDown, pak metoda <see cref="MouseDownDataProcess()"/> nebude nic zpracovávat...
            /// </summary>
            /// <param name="e"></param>
            public virtual void EventMouseLeftDown(GInteractiveChangeStateArgs e)
            {   // Pozor, stav textboxu je: HasFocus = true, ale dosud nemusí být v tomto stavu vykreslen (=při prvním příchodu Focusu) (ačkoliv na druhou stranu focus už tu být může!), 
                //  takže nelze použít pole this.CharacterBounds = pole jednotlivých znaků a jejich pozic!!!
                // Postup: zde připravíme data, a v procesu kreslení bude vyvolána metoda AfterMouseCursorPrepare().
                // Odlišení, zda jde o MouseDown při aktivaci focusu (tzn. na TextBox bylo kliknuto, když neměl Focus) anebo o MouseDown v procesu editace textu (měl Focus) 
                //  se dá poznat podle hodnoty v e.UserData: v metodě this.AfterFocusEnterCursor() do e.UserData vkládáme string "FocusEnter",
                //  a tato hodnota se pak přenese i do eventu AfterStateChangedMouseLeftDown(), tedy sem.
                //  Pokud ale TextBox už focus má, pak se při stisku myši už událost AfterFocusEnterCursor() nevyvolá, a v proměnné e.UserData nebude nic.
                MouseDownIsFocusEnter = String.Equals(e.UserData as string, ConstantFocusEnter);
                MouseDownButtons = e.MouseButtons;
                MouseDownAbsoluteLocation = e.MouseAbsolutePoint;
                MouseDownModifierKeys = e.ModifierKeys;
            }
            /// <summary>
            /// Konstanta vkládaná do <see cref="GInteractiveChangeStateArgs.UserData"/> v události FocusEnter, detekovaná v události MouseDown.
            /// Výsledkem je odlišení situace, kdy MouseDown je provedeno jako prvotní vstup do TextBoxu anebo v již editovaném TextBoxu.
            /// </summary>
            protected const string ConstantFocusEnter = "FocusEnter";
            #endregion
            #region Klávesnice
            /// <summary>
            /// Metoda vrátí true, pokud daná klávesová kombinace bude interpretována jako nějaký pohyb kurzoru (samotný pohyb / označování se shiftem)
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            protected bool IsCursorKey(PreviewKeyDownEventArgs args)
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
                return false;
            }
            /// <summary>
            /// Detekuje a zpracuje kurzorové klávesy i kombinace
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            protected virtual bool ProcessKeyCursor(KeyEventArgs args)
            {
                int cursorIndex = CursorIndex;
                Int32Range selectionRange = SelectionRange;
                int selectionBegin = (selectionRange != null ? selectionRange.Begin : cursorIndex);
                int textLength = (this._Chars != null ? this._Chars.Length : 0);
                CharacterPositionInfo charInfo = null;
                if (this._Chars != null && cursorIndex >= 0 && cursorIndex < textLength)
                    charInfo = this._Chars[cursorIndex];

                switch (args.KeyData)                      // KeyData obsahuje kompletní klávesu = včetně modifikátoru (Ctrl, Shift, Alt)
                {
                    case Keys.Home:                        // Na začátek aktuálního řádku
                        cursorIndex = (charInfo != null ? charInfo.Line.Chars[0].Index : 0);
                        selectionRange = null;
                        break;
                    case Keys.Control | Keys.Home:         // Na úplný začátek textu
                        cursorIndex = 0;
                        selectionRange = null;
                        break;
                    case Keys.Shift | Keys.Home:           // Na začátek aktuálního řádku, označit od dosavadní pozice
                        cursorIndex = (charInfo != null ? charInfo.Line.Chars[0].Index : 0);
                        selectionRange = new Int32Range(selectionBegin, cursorIndex);
                        break;

                    case Keys.End:
                        cursorIndex = textLength;
                        selectionRange = null;
                        break;

                    case Keys.Left:
                        if (cursorIndex > 0)
                            cursorIndex--;
                        selectionRange = null;
                        break;

                    case Keys.Shift | Keys.Left:
                        if (cursorIndex > 0)
                            cursorIndex--;
                        selectionRange = new Int32Range(selectionBegin, cursorIndex);
                        break;

                    case Keys.Right:
                        if (cursorIndex < textLength)
                            cursorIndex++;
                        selectionRange = null;
                        break;

                    case Keys.Shift | Keys.Right:
                        if (cursorIndex < textLength)
                            cursorIndex++;
                        selectionRange = new Int32Range(selectionBegin, cursorIndex);
                        break;

                    case Keys.Up:
                    case Keys.Down:
                        return true;

                    case Keys.Control | Keys.A:
                        cursorIndex = 0;
                        selectionRange = new Int32Range(cursorIndex, textLength);
                        break;

                    default:
                        return false;
                }
                CursorIndex = cursorIndex;
                SelectionRange = selectionRange;
                this.Invalidate();
                return true;
            }
            protected virtual bool ProcessKeyBackspaceDel(KeyEventArgs args)
            {
                if (!IsEditable) return false;

                int cursorIndex = CursorIndex;
                Int32Range selectionRange = SelectionRange;
                if (selectionRange != null && selectionRange.Size == 0) selectionRange = null;
                Int32Range deletionRange = null;
                int textLength = (this._Chars != null ? this._Chars.Length : 0);

                switch (args.KeyData)
                {
                    case Keys.Back:                                  // Jeden Backspace = Smaže označený blok nebo jeden znak vlevo
                    case Keys.Shift | Keys.Back:                     // Shift + BackSpace = stejné jako Backspace
                        if (selectionRange != null)
                            deletionRange = selectionRange;
                        else if (cursorIndex > 0)
                        {
                            cursorIndex--;
                            deletionRange = new Int32Range(cursorIndex, cursorIndex + 1);
                        }
                        break;
                    case Keys.Control | Keys.Back:                   // Control + Backspace = Smaže doleva slovo od kurzoru k začátku slova, včetně výběru

                        // b 123 456 78 789 

                        break;
                    case Keys.Delete:                                // Delete = Smaže označený blok nebo znak vpravo od kurzoru
                        if (selectionRange != null)
                            deletionRange = selectionRange;
                        else if (cursorIndex < textLength)
                        {
                            deletionRange = new Int32Range(cursorIndex, cursorIndex + 1);
                        }
                        break;
                    case Keys.Shift | Keys.Delete:                   // Shift + Delete = Smaže označený blok nebo celý řádek kde je kurzor
                    case Keys.Control | Keys.Delete:                 // Control + Delete = Smaže označený blok nebo slovo od kurzoru doprava
                        break;
                    default:
                        return false;
                }
                DeleteInsertText(deletionRange);
                CursorIndex = cursorIndex;
                SelectionRange = null;
                this.Invalidate();
                return true;
            }
            /// <summary>
            /// Detekuje a zpracuje klávesy Ctrl+C, Ctrl+X, Ctrl+V = Clipboard
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            protected virtual bool ProcessKeyClipboard(KeyEventArgs args)
            {
                switch (args.KeyData)
                {
                    case Keys.Control | Keys.C:
                        Owner.ClipboardAction(ClipboardActionType.Copy);
                        break;
                    case Keys.Control | Keys.X:
                        if (IsEditable)
                            Owner.ClipboardAction(ClipboardActionType.Cut);
                        else
                            Owner.ClipboardAction(ClipboardActionType.Copy);
                        break;
                    case Keys.Control | Keys.V:
                        if (IsEditable)
                            Owner.ClipboardAction(ClipboardActionType.Paste);
                        break;
                    default:
                        return false;
                }
                this.Invalidate();
                return true;
            }
            /// <summary>
            /// Detekuje a zpracuje klávesy Undo / Redo
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            protected virtual bool ProcessKeyUndoRedo(KeyEventArgs args)
            {
                if (!IsEditable) return false;
                switch (args.KeyData)
                {
                    case Keys.Control | Keys.Z:
                        Owner.UndoRedoAction(UndoRedoActionType.Undo);
                        break;
                    case Keys.Control | Keys.Y:
                        Owner.UndoRedoAction(UndoRedoActionType.Redo);
                        break;
                    default:
                        return false;
                }
                this.Owner.Invalidate();
                return true;
            }
            /// <summary>
            /// Detekuje a zpracuje speciální klávesy pomocí TextBoxu
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            protected virtual bool ProcessKeySpecial(KeyEventArgs args)
            {
                Owner.SpecialKeyAction(args);
                return args.Handled;
            }
            /// <summary>
            /// Detekuje a zpracuje focusové klávesy
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            protected virtual bool ProcessKeyFocus(KeyPressEventArgs args)
            {
                if (args.KeyChar == 0x0d)
                {
                    this._Owner.Host?.SetFocusToNextItem(Direction.Positive);
                    return true;
                }

                return false;
            }
            /// <summary>
            /// Detekuje a zpracuje standardní textové klávesy
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            protected virtual bool ProcessKeyText(KeyPressEventArgs args)
            {
                if (!IsEditable) return false;

                char c = args.KeyChar;
                if (Char.IsLetterOrDigit(c) ||
                    Char.IsSeparator(c) ||
                    Char.IsPunctuation(c) ||
                    Char.IsSymbol(c) ||
                    Char.IsWhiteSpace(c))
                {
                    string value = args.KeyChar.ToString();

                    int cursorIndex = CursorIndex;
                    Int32Range selectionRange = SelectionRange;
                    if (selectionRange != null && selectionRange.Size == 0) selectionRange = null;

                    if (selectionRange != null)
                        cursorIndex = DeleteInsertText(selectionRange, value);
                    else
                        cursorIndex = DeleteInsertText(cursorIndex, 0, value);

                    CursorIndex = cursorIndex;
                    SelectionRange = null;
                    Invalidate();
                    return true;
                }
                return false;
            }
            protected int DeleteInsertText(Int32Range deletionRange, string insertText = null)
            {
                if (deletionRange == null) return 0;
                int begin = (deletionRange.Begin < deletionRange.End ? deletionRange.Begin : deletionRange.End);
                int end = (deletionRange.Begin > deletionRange.End ? deletionRange.Begin : deletionRange.End);
                string text = this.Text;
                int length = text.Length;
                string textL = (begin > 0 ? text.Substring(0, (begin < length ? begin : length)) : "");
                string textR = (end < length ? text.Substring(end) : "");
                bool isInsert = (insertText != null && insertText.Length > 0);
                text = textL + (isInsert ? insertText : "") + textR;
                this.Text = text;
                return textL.Length + (isInsert ? insertText.Length : 0);
            }
            protected int DeleteInsertText(int begin, int removeLength, string insertText = null)
            {
                string text = this.Text;
                int length = text.Length;
                string textL = (begin > 0 ? text.Substring(0, (begin < length ? begin : length)) : "");
                int end = begin + (removeLength < 0 ? 0 : removeLength);
                string textR = (end < length ? text.Substring(end) : "");
                bool isInsert = (insertText != null && insertText.Length > 0);
                text = textL + (isInsert ? insertText : "") + textR;
                this.Text = text;
                return textL.Length + (isInsert ? insertText.Length : 0);
            }
            /// <summary>
            /// Klávesa je vyřešena?
            /// Na false je nastaveno v KeyPreview, na true je nastaveno po zpracování klávesy v konkrétním algoritmu.
            /// Pokud je true, nebude klávesa předávána do žádné další metody.
            /// </summary>
            protected bool IsKeyHandled;
            #endregion
            #region Vyhledání slova (začátek, konec, celé slovo)
            /// <summary>
            /// Zkusí najít začátek dalšího slova
            /// </summary>
            /// <param name="chars"></param>
            /// <param name="direction"></param>
            /// <param name="index"></param>
            /// <returns></returns>
            internal static bool TrySearchWordBegin(CharacterPositionInfo[] chars, Direction direction, ref int index)
            {
                if (chars == null) { index = -1; return false; }               // Není vstup - není slovo
                return _TrySearchWordEdge(chars, direction, true, ref index);
            }
            /// <summary>
            /// Zkusí najít konec aktuálního slova.
            /// Konec slova je index první mezery (nebo jiného znaku) za písmenem (nebo číslicí).
            /// Hledat lze doleva (<paramref name="direction"/> = <see cref="Direction.Negative"/>) i doprava (<paramref name="direction"/> = <see cref="Direction.Positive"/>).
            /// Pokud je zadán směr <paramref name="direction"/> = <see cref="Direction.None"/>, pak se pouze otestuje zadaný index a vrátí se true/false, ale nehledá se žádní sousední pozice.
            /// Pokud vstupní index ukazuje právě na takovou pozici, bude tato pozice i vrácena = neprovádí se navýšení indexu před hledáním.
            /// Pokud vstupní index je menší než 0, a směr hledání bude <see cref="Direction.Positive"/>, začne hledání na indexu 0. 
            /// Pokud vstupní index je na konci textu nebo za koncem, a směr hledání bude <see cref="Direction.Positive"/>, a text končí slovem, bude vrácen index za posledním znakem slova = i za posledním znakem celého textu.
            /// </summary>
            /// <param name="chars"></param>
            /// <param name="direction"></param>
            /// <param name="index"></param>
            /// <returns></returns>
            internal static bool TrySearchWordEnd(CharacterPositionInfo[] chars, Direction direction, ref int index)
            {
                if (chars == null) { index = -1; return false; }               // Není vstup - není slovo
                return _TrySearchWordEdge(chars, direction, false, ref index);
            }
            /// <summary>
            /// Metoda vrátí true, pokud daná pozice (index) ukazuje právě na začátek slova = první písmeno (nebo číslice) za mezerou (nebo jiným znakem)
            /// </summary>
            /// <param name="chars">Pole znaků</param>
            /// <param name="index">Pozice</param>
            /// <returns></returns>
            internal static bool IsBeginWord(CharacterPositionInfo[] chars, int index)
            {
                if (chars == null) return false;                              // Není vstup - není slovo
                int idx = index;
                return _TrySearchWordEdge(chars, Direction.None, true, ref idx);
            }
            /// <summary>
            /// Metoda vrátí true, pokud daná pozice (index) ukazuje právě na konec slova = první mezera (nebo jiný znak) za písmenem (nebo číslicí)
            /// </summary>
            /// <param name="chars">Pole znaků</param>
            /// <param name="index">Pozice</param>
            /// <returns></returns>
            internal static bool IsEndWord(CharacterPositionInfo[] chars, int index)
            {
                if (chars == null) return false;                              // Není vstup - není slovo
                int idx = index;
                return _TrySearchWordEdge(chars, Direction.None, false, ref idx);
            }
            /// <summary>
            /// Metoda se pokusí najít nejbližší slovo k dané pozici.
            /// Pokud daná pozice ukazuje na písmeno (nebo číslicí), pak najde počátek a konec daného slova.
            /// Pokud pozice je mezi dvěma slovy, pak najde to bližší slovo (před/za indexem) a to vrátí.
            /// Pokud daná pozice je před prvním slovem, vrátí první slovo. Stejně pokud je za posledním slovem, vrátí poslední slovo.
            /// </summary>
            /// <param name="chars"></param>
            /// <param name="index"></param>
            /// <param name="wordRange"></param>
            /// <param name="isRightSide"></param>
            /// <returns></returns>
            internal static bool TrySearchNearWord(CharacterPositionInfo[] chars, int index, out Int32Range wordRange, bool? isRightSide = null)
            {
                wordRange = null;

                if (chars == null) return false;                               // Není vstup - není slovo
                int length = chars.Length;
                if (length == 0) return false;                                 // Není ani písmeno - není ani slovo

                int currIndex = index;
                if (currIndex >= length) currIndex = length - 1;
                if (currIndex < 0) currIndex = 0;

                int beginIndex = currIndex;
                int endIndex = currIndex;
                bool beginFound, endFound;

                if (_IsCharText(chars[currIndex].Content))
                {   // Na daném indexu JE znak slova => najděme jeho POČÁTEK (doleva od dané pozice) a KONEC (doprava):
                    beginFound = _TrySearchWordEdge(chars, Direction.Negative, true, ref beginIndex);
                    endFound = _TrySearchWordEdge(chars, Direction.Positive, false, ref endIndex);
                    if (beginFound && endFound)
                    {
                        wordRange = new Int32Range(beginIndex, endIndex);
                        return true;
                    }
                    // Sem se z logiky věci nikdy nedostanu:
                    throw new InvalidOperationException("EditorStateInfo.TrySearchWord() error 'IndexOnTextNotFoundWord'");
                }

                // Na daném indexu NENÍ znak slova, podíváme se na nejbližší konec předešlého a na začátek následujícího slova:
                endFound = _TrySearchWordEdge(chars, Direction.Negative, false, ref endIndex);          // Hledáme pozici první mezery za předešlým slovem před currIndex = konec předešlého slova
                beginFound = _TrySearchWordEdge(chars, Direction.Positive, true, ref beginIndex);       // Hledáme pozici prvního znaku v následujícím slovu za currIndex = začátek následujícího slova

                // Nyní určíme, zda existuje slovo PŘED i ZA indexem, a které je blíže, anebo které je jediné...
                if (!beginFound && !endFound) return false;                                             // Neexistuje žádné slovo ani vlevo, ani vpravo...

                // Pokud máme nalezeno slovo PŘED i ZA indexem, pak určíme, které je blíže k indexu (započítaje v to i informaci isRightSide):
                Direction nearSide = Direction.None;
                if (beginFound && endFound)
                {
                    int beforeDistance = currIndex - endIndex;                                          // Vzdálenost od konce předchozího slova k pozici kurzoru
                    int afterDistance = beginIndex - currIndex - 1;                                     // Vzdálenost od pozice kurzoru k začátku následujícího slova
                    nearSide = (beforeDistance < afterDistance ? Direction.Negative :                   // Pokud VLEVO je menší vzdálenost než VPRAVO, pak nearSide je Negative
                               (afterDistance < beforeDistance ? Direction.Positive :                   // Pokud VPRAVOje menší vzdálenost než VLEVO , pak nearSide je Positive
                               (isRightSide.HasValue && !isRightSide.Value ? Direction.Negative : Direction.Positive)));  // Pokud je ZADANÝ příznak pravé strany, a NENÍ naplněn, pak nearSide je Negative, jinak je Positive = doprava
                }

                if (beginFound && (!endFound || (endFound && nearSide == Direction.Positive)))
                {   // Existuje slovo ZA indexem, a (neexistuje slovo PŘED indexem, anebo existuje, ale bližší slovo je to VPRAVO):
                    endIndex = beginIndex;
                    endFound = _TrySearchWordEdge(chars, Direction.Positive, false, ref endIndex);      // Najděme konec slova za indexem počátku tohoto slova
                    if (endFound)
                    {
                        wordRange = new Int32Range(beginIndex, endIndex);
                        return true;
                    }
                    // Sem se z logiky věci nikdy nedostanu:
                    throw new InvalidOperationException("EditorStateInfo.TrySearchWord() error 'WordAfterIndexNotFound'");
                }

                // Najdeme slovo PŘED indexem:
                if (endFound)
                {
                    beginIndex = endIndex;
                    beginFound = _TrySearchWordEdge(chars, Direction.Negative, true, ref beginIndex);   // Najděme začátek slova před indexem konce tohoto slova
                    if (beginFound)
                    {
                        wordRange = new Int32Range(beginIndex, endIndex);
                        return true;
                    }
                    // Sem se z logiky věci nikdy nedostanu:
                    throw new InvalidOperationException("EditorStateInfo.TrySearchWord() error 'WordBeforeIndexNotFound'");
                }

                // Sem se z logiky věci nikdy nedostanu:
                throw new InvalidOperationException("EditorStateInfo.TrySearchWord() error 'WordNotFound'");
            }
            /// <summary>
            /// Metoda zkusí najít nejbližší (doprava / doleva) okraj slova (začátek / konec), počínaje daným indexem.
            /// </summary>
            /// <param name="chars">Pole znaků. Nesmí být null, to by měla testovat volající metoda (z důvodu rychlosti při opakovaných privátních voláních)</param>
            /// <param name="direction">Směr hledání:
            /// <see cref="Direction.Positive"/> = doprava v textu (na vyšší index);
            /// <see cref="Direction.Negative"/> = doleva v textu (na nižší index);
            /// <see cref="Direction.None"/> = nikam = pouze otestovat danou pozici (podle <paramref name="searchBeginText"/>), a vrátit výsledek</param>
            /// <param name="searchBeginText">true: hledám přechod z mezery do textu / false: hledám přechod z textu do mezery</param>
            /// <param name="index">Vstupní / Výstupní index</param>
            /// <returns>true = nalezeno / false = nenalezeno</returns>
            private static bool _TrySearchWordEdge(CharacterPositionInfo[] chars, Direction direction, bool searchBeginText, ref int index)
            {
                int length = chars.Length;
                if (length == 0) return false;

                int currIndex = index;
                if (currIndex > length) currIndex = length;
                if (currIndex < 0) currIndex = 0;
                char zero = (char)0;

                bool run = true;
                while (run)
                {   // Bude alespoň jeden cyklus na vyhodnocení zadané pozice, protože text má délku alespoň jeden znak!
                    int prevIndex = currIndex - 1;
                    char prevChar = ((prevIndex >= 0 && prevIndex < length) ? chars[prevIndex].Content : zero);        // Znak PŘED daným indexem (nebo 0)
                    char currChar = ((currIndex >= 0 && currIndex < length) ? chars[currIndex].Content : zero);        // Znak ZA daným indexem (nebo 0)

                    // Pokud předchozí znak a aktuální znak dává požadovanou sekvenci (Mezera => Znak; anebo Znak => Mezera), pak máme nalezenou hranici slova a mezery:
                    if (_IsWordEdge(prevChar, currChar, searchBeginText))
                    {
                        index = currIndex;
                        return true;
                    }

                    // Jděme na další index, nebo skončeme:
                    switch (direction)
                    {
                        case Direction.Positive:
                            if (currIndex >= length) run = false; else currIndex++;
                            break;
                        case Direction.Negative:
                            if (currIndex < 0) run = false; else currIndex--;
                            break;
                        default:
                            run = false;
                            break;
                    }
                }

                // Hodnota "ref index" v případě, že jsme nenalezli: podle požadovaného směru = buď za koncem, nebo před začátkem, nebo beze změny:
                index = (direction == Direction.Positive ? length : (direction == Direction.Negative ? -1 : index));
                return false;
            }
            /// <summary>
            /// Pro <paramref name="searchBeginText"/> = true : Vrátí true, když znak <paramref name="prevChar"/> není písmeno ani číslice, a současně znak <paramref name="nextChar"/> = je písmeno nebo číslice.
            /// <para/>
            /// Pro <paramref name="searchBeginText"/> = false : Vrátí true, když znak <paramref name="prevChar"/> je písmeno nebo číslice, a současně znak <paramref name="nextChar"/> = není písmeno ani číslice.
            /// </summary>
            /// <param name="prevChar">Předchozí znak</param>
            /// <param name="nextChar">Navazující znak</param>
            /// <param name="searchBeginText">Hledat začátek slova (true) / konec slova (false)</param>
            /// <returns></returns>
            private static bool _IsWordEdge(char prevChar, char nextChar, bool searchBeginText)
            {
                bool prevIsLetter = _IsCharText(prevChar);
                bool nextIsLetter = _IsCharText(nextChar);
                return (searchBeginText ?
                    !prevIsLetter &&  nextIsLetter :
                     prevIsLetter && !nextIsLetter);
            }
            /// <summary>
            /// Vrátí true, pokud dodaný znak je písmeno uvnitř slova (písmeno nebo číslice) nebo akceptované znaky <see cref="GTextEdit.CharactersAssumedAsWords"/>;
            /// vrátí false pokud jde o mezeru nebo nevýznamný oddělovač (operátory, 
            /// </summary>
            /// <param name="charText"></param>
            /// <returns></returns>
            private static bool _IsCharText(char charText)
            {
                if (Char.IsLetterOrDigit(charText)) return true;
                if (CharactersAssumedAsWords != null && CharactersAssumedAsWords.IndexOf(charText) >= 0) return true;
                return false;
            }
            #endregion
        }
        #endregion
        #region class CharacterPositionInfo a LinePositionInfo : Třída, která udržuje informace o každém jednom znaku v rámci textboxu (index, znak, souřadnice)
        /// <summary>
        /// CharacterPositionInfo: Třída, která udržuje informace o každém jednom znaku v rámci textboxu (index, znak, souřadnice)
        /// </summary>
        public class CharacterPositionInfo
        {
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"[{Index}] : '{Content}'; Line: {Line?.Index}; Bounds: ({Bounds.X},{Bounds.Y},{Bounds.Width},{Bounds.Height})";
            }
            /// <summary>
            /// Index znaku v textu (počínaje 0)
            /// </summary>
            public int Index { get; private set; }
            /// <summary>
            /// Obsah = znak
            /// </summary>
            public char Content { get; private set; }
            /// <summary>
            /// Souřadnice tohoto znaku, v absolutní hodnotě
            /// </summary>
            public Rectangle Bounds { get; private set; }
            /// <summary>
            /// Řádek, na kterém se znak nachází
            /// </summary>
            public LinePositionInfo Line { get; set; }
            /// <summary>
            /// Vytvoří a vrátí pole <see cref="CharacterPositionInfo"/> pro zadaný text a virtuální souřadnice znaků, pouze pro testovací účely.
            /// Souřadnice znaků jsou v jednom nebo více řádcích, velikost jednoho znaku je fixní, daná parametrem <paramref name="charSize"/>.
            /// Pokud je zadán parametr <paramref name="maxX"/>, pak je text rozdělen do více řádků v šířce nejvýše zadané.
            /// </summary>
            /// <param name="text"></param>
            /// <param name="charSize"></param>
            /// <param name="maxX"></param>
            /// <returns></returns>
            internal static CharacterPositionInfo[] CreateChars(string text, SizeF charSize, float? maxX = null)
            {
                if (text == null) return null;
                int length = text.Length;
                CharacterPositionInfo[] result = new CharacterPositionInfo[length];
                if (length == 0) return result;                      // Zkratka ven

                float x = 0f;
                float y = 0f;
                float w = charSize.Width;
                if (w < 1f) w = 1f;
                float h = charSize.Height;
                if (h < 1f) h = 1f;
                bool hasMaxX = (maxX.HasValue && maxX.Value > 0f);
                for (int i = 0; i < length; i++)
                {
                    char item = text[i];
                    float r = x + w;
                    if (hasMaxX && x > 0f && r > maxX.Value)
                    {   // Přejdeme na nový řádek:
                        x = 0f;
                        y = y + h;
                    }
                    RectangleF charPosition = new RectangleF(x, y, w, h);
                    x += w;
                    result[i] = new CharacterPositionInfo(i, item, Rectangle.Ceiling(charPosition));
                }
                return result;
            }
            /// <summary>
            /// Vytvoří a vrátí pole <see cref="CharacterPositionInfo"/> pro zadaný text a souřadnice znaků
            /// </summary>
            /// <param name="text"></param>
            /// <param name="charPositions"></param>
            /// <returns></returns>
            internal static CharacterPositionInfo[] CreateChars(string text, RectangleF[] charPositions)
            {
                if (text == null) return null;
                if (charPositions == null) return null;
                int length = (text.Length < charPositions.Length ? text.Length : charPositions.Length);
                CharacterPositionInfo[] result = new CharacterPositionInfo[length];
                for (int i = 0; i < length; i++)
                {
                    char item = text[i];
                    RectangleF charPosition = charPositions[i];
                    result[i] = new CharacterPositionInfo(i, item, Rectangle.Ceiling(charPosition));
                }
                return result;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="index"></param>
            /// <param name="content"></param>
            /// <param name="bounds"></param>
            private CharacterPositionInfo(int index, char content, Rectangle bounds)
            {
                this.Index = index;
                this.Content = content;
                this.Bounds = bounds;
            }
        }
        /// <summary>
        /// LinePositionInfo: Třída, která udržuje informace o každém jednom řádku v rámci textboxu (index, rozsah na ose Y, pole znaků)
        /// </summary>
        public class LinePositionInfo
        {
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string content = Chars.ToOneString("", c => c.Content.ToString());
                return $"[{Index}] : '{content}'; Bounds: ({Bounds.X},{Bounds.Y},{Bounds.Width},{Bounds.Height})";
            }
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
                if (chars == null) return null;
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
            /// <summary>
            /// Privátní konstruktor
            /// </summary>
            private LinePositionInfo()
            {
                _Left = _Right = _Top = _Bottom = 0;
                _CharList = new List<CharacterPositionInfo>();
            }
            /// <summary>
            /// Vrátí true, pokud se daný znak nachází v prostoru tohoto řádku (na ose Y). Akceptuje i částečné překrytí.
            /// </summary>
            /// <param name="c"></param>
            /// <returns></returns>
            private bool _IsCharOnLine(CharacterPositionInfo c)
            {
                return (c.Bounds.Height > 0 && c.Bounds.Bottom > _Top && c.Bounds.Top < _Bottom);
            }
            /// <summary>
            /// Přidá daný znak do this řádku, přičte jeho souřadnice do souřadnic řádku (pro první znak v řádku: souřadnice převezme z dodaného znaku).
            /// </summary>
            /// <param name="c"></param>
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
            /// <summary>
            /// Finalizuje řádek: nastaví <see cref="Index"/> řádku; určí souřadnice <see cref="Bounds"/>; zkompletuje pole <see cref="Chars"/>; 
            /// a do znaků obsažených v tomto řádku <see cref="Chars"/> vepíše referenci na řádek.
            /// </summary>
            /// <param name="index"></param>
            private void _Finalize(ref int index)
            {
                _Index = index++;
                _Bounds = Rectangle.FromLTRB(_Left, _Top, _Right, _Bottom);
                _Chars = _CharList.ToArray();
                _CharList = null;
                _Chars.ForEachItem(c => { c.Line = this; });
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
            /// <param name="lines">Pole řádků textu</param>
            /// <param name="point">Zadaný bod, k němuž hledáme znak na nejbližší pozici</param>
            /// <param name="isRightSide">Out: zadaný bod leží v pravé polovině nalezeného znaku</param>
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
        public static bool SelectWordOnControlMouse { get; set; } = true;
        /// <summary>
        /// Znaky, které akceptujeme jako vnitřní součást slova, rovnocenné písmenům a číslicím
        /// </summary>
        public static string CharactersAssumedAsWords { get; set; } = "_";
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
    #region enum ClipboardActionType, UndoRedoActionType
    /// <summary>
    /// Druh akce s Clipboardem
    /// </summary>
    public enum ClipboardActionType
    {
        /// <summary>
        /// Žádná akce
        /// </summary>
        None,
        /// <summary>
        /// Copy
        /// </summary>
        Copy,
        /// <summary>
        /// Cut
        /// </summary>
        Cut,
        /// <summary>
        /// Paste
        /// </summary>
        Paste
    }
    /// <summary>
    /// Druh akce se zásobníkem Undo/Redo
    /// </summary>
    public enum UndoRedoActionType
    {
        /// <summary>
        /// Žádná akce
        /// </summary>
        None,
        /// <summary>
        /// Odvolat poslední akci a vrátit se o krok zpět
        /// </summary>
        Undo,
        /// <summary>
        /// Znovu provést poslední akci
        /// </summary>
        Redo
    }
    #endregion
}
