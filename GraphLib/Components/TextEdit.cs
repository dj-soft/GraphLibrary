using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        #region Myš
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedLeftClick(e);
            this.CursorIndex = null;                       // Po MouseClick dojde k překreslení TextBoxu, a v metodě Draw se vyvolá FindCursorBounds();
            this.MouseClickPoint = e.MouseAbsolutePoint;   //  tam se detekuje že není známá pozice kurzoru (CursorIndex = null), ale je známý bod myši (MouseClickPoint)...
        }
        #endregion
        #region Klávesnice
        protected override void AfterStateChangedKeyPreview(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedKeyPreview(e);
        }
        protected override void AfterStateChangedKeyPress(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedKeyPress(e);
            if (e.KeyboardEventArgs.KeyCode == System.Windows.Forms.Keys.Tab)
            { }
            // this.Parent
        }
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
            this.DrawBackground(drawArgs);                  // Background
            this.DrawOverlay(drawArgs, OverlayBackground);  // Grafika nad Backgroundem
            this.DrawText(drawArgs);                        // Text
            this.DrawOverlay(drawArgs, OverlayText);        // Grafika nad Textem
            this.DrawBorder(drawArgs);         // Rámeček
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
        /// <param name="overlay"></param>
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
        /// Vykreslí obsah (text)
        /// </summary>
        /// <param name="drawArgs">Argumenty</param>
        protected virtual void DrawText(GTextEditDrawArgs drawArgs)
        {
            string text = this.Text ?? "";
            StringFormatFlags stringFormat = StringFormatFlags.NoWrap | StringFormatFlags.LineLimit;
            if (!this.HasFocus)
            {
                // Bez Focusu = jen vypíšu text:
                GPainter.DrawString(drawArgs.Graphics, text, this.CurrentFont, drawArgs.InnerBounds, this.Alignment, color: this.CurrentTextColor, stringFormat: stringFormat);
                return;
            }

            if (text.Length == 0) text = " ";              // Kvůli souřadnicím
            RectangleF[] charPositions = GPainter.DrawStringMeasureChars(drawArgs.Graphics, text, this.CurrentFont, drawArgs.InnerBounds, this.Alignment, color: this.CurrentTextColor, stringFormat: stringFormat);
            this.CharacterBounds = CharacterPositionInfo.CreateArray(text, charPositions);
            this.CursorBounds = this.FindCursorBounds();

            if (this.CursorBounds.HasValue)
                drawArgs.Graphics.FillRectangle(Brushes.Black, this.CursorBounds.Value);

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
        protected override void AfterStateChangedFocusEnter(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusEnter(e);
        }
        protected override void AfterStateChangedFocusLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusLeave(e);
        }

        protected AnimationResult ShowCursorTick(AnimationArgs args)
        {


            return AnimationResult.Stop;
        }
       
        protected Rectangle? FindCursorBounds()
        {
            int? cursorIndex = this.CursorIndex;
            if (cursorIndex == null && this.MouseClickPoint.HasValue)
            {
                this.CursorIndex = this.FindCursorIndex(this.MouseClickPoint.Value);
                cursorIndex = this.CursorIndex;
            }
            if (!cursorIndex.HasValue) return null;
            int charLength = (this.CharacterBounds == null ? 0 : this.CharacterBounds.Length);
            if (charLength <= 0) return null;
            CharacterPositionInfo charInfo;
            if (cursorIndex.Value < charLength)
            {
                charInfo = this.CharacterBounds[cursorIndex.Value];
                return new Rectangle(charInfo.Bounds.X, charInfo.Bounds.Y, 1, charInfo.Bounds.Height);
            }
            charInfo = this.CharacterBounds[charLength - 1];
            return new Rectangle(charInfo.Bounds.Right, charInfo.Bounds.Y, 1, charInfo.Bounds.Height);
        }
        /// <summary>
        /// Vrátí pozici kurzoru odpovídající danému absolutnímu bodu
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        protected int FindCursorIndex(Point absolutePoint)
        {
            if (this.CharacterBounds == null || this.CharacterBounds.Length == 0) return 0;

            int count = this.CharacterBounds.Length;
            float x = absolutePoint.X;
            float y = absolutePoint.Y;
            int? found = null;
            for (int i = 0; i < count; i++)
            {
                Rectangle charBounds = this.CharacterBounds[i].Bounds;
                if (y < charBounds.Y && i == 0) { found = i; break; }     // Myší klik byl nad prvním řádkem => pozice kurzoru je na prvním znaku
                if (y >= charBounds.Bottom) continue;                     // Myší klik byl pod daným znakem => budeme hledat dál
                if (x < charBounds.X) { found = i; break; }               // Myší klik byl vlevo od aktuálního znaku => pozice kurzoru je na tomto znaku
                if (x >= charBounds.Right) continue;                      // Myší klik byl vpravo od aktuálního znaku => budeme hledat dál
                if (x <= (charBounds.X + (charBounds.Width / 2)))         // Myší klik byl v první polovině (na ose X) aktuálního znaku:
                    found = i;
                else
                    found = i + 1;
                break;
            }

            return (found.HasValue ? found.Value : 0);
        }
        /// <summary>
        /// Absolutní souřadnice myši, kde bylo kliknuto
        /// </summary>
        protected Point? MouseClickPoint { get; set; }
        /// <summary>
        /// Index pozice kurzoru.
        /// Hodnota null = dosud neurčeno.
        /// Hodnota 0 = před prvním znakem; hodnota za poslední pozicí <see cref="CharacterBounds"/> je přípustná.
        /// </summary>
        protected int? CursorIndex { get; set; }
        /// <summary>
        /// Souřadnice kurzoru
        /// </summary>
        protected Rectangle? CursorBounds { get; set; }
        /// <summary>
        /// Relativní souřadnice jednotlivých znaků
        /// </summary>
        protected CharacterPositionInfo[] CharacterBounds { get; set; }
        /// <summary>
        /// Třída, která udržuje informace o každém jednom znaku v rámci textboxu (index, znak, souřadnice)
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
            internal static CharacterPositionInfo[] CreateArray(string text, RectangleF[] charPositions)
            {
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
                if (state.HasFlag(GInteractiveState.Disabled)) backColor = BackColorDisabled.Value;
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
                if (state.HasFlag(GInteractiveState.Disabled)) textColor = TextColorDisabled.Value;
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
                if (state.HasFlag(GInteractiveState.Disabled)) borderColor = BorderColorDisabled.Value;
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
        /// Přídavné vykreslení přes Background, pod text
        /// </summary>
        public ITextEditOverlay OverlayBackground { get; set; }
        /// <summary>
        /// Přídavné vykreslení přes Text
        /// </summary>
        public ITextEditOverlay OverlayText { get; set; }
        /// <summary>
        /// Typ vztahu - pro správné vykreslování (linka podtržení)
        /// </summary>
        public TextRelationType RelationType { get; set; }
        /// <summary>
        /// Obsahuje true pro prvek, jehož hodnota má být zadaná, nikoli Empty.
        /// Pak se Border vykresluje barvou 
        /// </summary>
        public bool IsRequiredValue { get; set; }
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
        /// Vnitřní souřadnice textum, bez rámečku
        /// </summary>
        public Rectangle InnerBounds { get; private set; }
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
    }
    #endregion
    #region enum TextRelationType
    /// <summary>
    /// Typ vztahu - pro správné vykreslování (linka podtržení)
    /// </summary>
    public enum TextRelationType
    {
        /// <summary>
        /// Není vztah
        /// </summary>
        None = 0,
        /// <summary>
        /// Vztah na záznam (modrý)
        /// </summary>
        ToRecord,
        /// <summary>
        /// Vztah na dokument (žlutý)
        /// </summary>
        ToDocument
    }
    #endregion
}
