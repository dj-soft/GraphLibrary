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
                      | InteractiveProperties.Bit.KeyboardInput);
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
        #endregion
        #region Vykreslení obsahu - základní
        /// <summary>
        /// Zajistí krslení TextBoxu
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="drawMode"></param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            Rectangle innerBounds = this.GetInnerBounds(absoluteBounds);
            this.DrawBackground(e, absoluteBounds, absoluteVisibleBounds, innerBounds, drawMode);     // Background
            this.DrawRelation(e, absoluteBounds, absoluteVisibleBounds, innerBounds, drawMode);       // Podtržení od vztahu
            this.DrawText(e, absoluteBounds, absoluteVisibleBounds, innerBounds, drawMode);           // Text
            this.DrawBorder(e, absoluteBounds, absoluteVisibleBounds, innerBounds, drawMode);         // Rámeček
        }
        /// <summary>
        /// Vykreslí pozadí
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="innerBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawBackground(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, Rectangle innerBounds, DrawItemMode drawMode)
        {
            this.DrawBackground(e, innerBounds, absoluteVisibleBounds, drawMode, this.CurrentBackColor);
        }
        /// <summary>
        /// Vykreslí linku podtržení vztahu podle <see cref="RelationType"/>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="innerBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawRelation(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, Rectangle innerBounds, DrawItemMode drawMode)
        {
            var relationType = this.RelationType;
            if (relationType == TextRelationType.ToRecord || relationType == TextRelationType.ToDocument)
                GPainter.DrawRelationLine(e.Graphics, innerBounds, false, (relationType == TextRelationType.ToDocument));
        }
        /// <summary>
        /// Vykreslí obsah (text)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="innerBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawText(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, Rectangle innerBounds, DrawItemMode drawMode)
        {
            string text = this.Text;
            if (this.HasFocus)
                // Máme Focus = budeme řešit Kurzor:
                this.DrawTextFocus(e, absoluteBounds, absoluteVisibleBounds, innerBounds, drawMode);
            else if (!String.IsNullOrEmpty(text))
                // Bez Focusu = jen vypíšu text:
                GPainter.DrawString(e.Graphics, text, this.Font, innerBounds, this.Alignment, color: this.ForeColor);
        }
        /// <summary>
        /// Vykreslí border
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="innerBounds"></param>
        /// <param name="drawMode"></param>
        protected virtual void DrawBorder(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, Rectangle innerBounds, DrawItemMode drawMode)
        {
            switch (this.BorderStyle.Value)
            {
                case BorderStyleType.Flat:
                    GPainter.DrawBorder(e.Graphics, absoluteBounds, RectangleSide.All, null, this.CurrentBorderColor, 0f);
                    break;
                case BorderStyleType.Effect3D:
                    GPainter.DrawBorder(e.Graphics, absoluteBounds, RectangleSide.All, null, this.CurrentBorderColor, this.CurrentBorder3DEffect);
                    break;
                case BorderStyleType.Soft:
                    GPainter.DrawSoftBorder(e.Graphics, absoluteBounds, RectangleSide.All, this.CurrentSoftBorderColor);
                    break;
            }
        }
        #endregion
        #region Vykreslení textu a kurzoru, na základě stavu editace

        protected void DrawTextFocus(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, Rectangle innerBounds, DrawItemMode drawMode)
        {
            string text = this.Text ?? "";
            if (text.Length == 0) text = " ";              // Kvůli souřadnicím
            RectangleF[] charPositions = GPainter.DrawStringMeasureChars(e.Graphics, text, this.Font, innerBounds, this.Alignment, color: this.ForeColor);
            this.CharacterBounds = CharacterPositionInfo.CreateArray(text, charPositions);
            this.CursorBounds = this.FindCursorBounds();

            if (this.CursorBounds.HasValue)
                e.Graphics.FillRectangle(Brushes.Black, this.CursorBounds.Value);

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
        #region Support pro kreslení obsahu - aktuální barvy (Inner, Border, Back, Fore) podle stavu interaktivity
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
        /// Aktuální barva okrajů
        /// </summary>
        protected Color CurrentBorderColor
        {
            get
            {
                var state = this.InteractiveState;
                if (state.HasFlag(GInteractiveState.Disabled)) return Skin.TextBox.PassiveBorderColor;
                if (this.HasFocus) return Skin.TextBox.ActiveBorderColor;
                if (state.HasFlag(GInteractiveState.FlagOver)) return Skin.TextBox.ActiveBorderColor;
                return Skin.TextBox.PassiveBorderColor;
            }
        }
        /// <summary>
        /// Aktuální barva okrajů
        /// </summary>
        protected Color CurrentSoftBorderColor
        {
            get
            {
                var state = this.InteractiveState;
                if (state.HasFlag(GInteractiveState.Disabled)) return Skin.TextBox.PassiveSoftBorderColor;
                if (this.HasFocus) return Skin.TextBox.ActiveSoftBorderColor;
                if (state.HasFlag(GInteractiveState.FlagOver)) return Skin.TextBox.ActiveSoftBorderColor;
                return Skin.TextBox.PassiveSoftBorderColor;
            }
        }
        /// <summary>
        /// Aktuální barva pozadí
        /// </summary>
        protected Color CurrentBackColor
        {
            get
            {
                var state = this.InteractiveState;
                if (state.HasFlag(GInteractiveState.Disabled)) return Skin.TextBox.DisabledBackColor;
                if (this.HasFocus) return Skin.TextBox.ActiveBackColor;
                if (state.HasFlag(GInteractiveState.FlagOver)) return Skin.TextBox.MouseOverBackColor;
                return Skin.TextBox.EnabledBackColor;
            }
        }
        /// <summary>
        /// Aktuální barva písma
        /// </summary>
        protected Color CurrentTextColor
        {
            get
            {
                var state = this.InteractiveState;
                if (state.HasFlag(GInteractiveState.Disabled)) return Skin.TextBox.DisabledForeColor;
                if (this.HasFocus) return Skin.TextBox.ActiveForeColor;
                if (state.HasFlag(GInteractiveState.FlagOver)) return Skin.TextBox.MouseOverForeColor;
                return Skin.TextBox.EnabledForeColor;
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
        /// Typ vztahu - pro správné vykreslování (linka podtržení)
        /// </summary>
        public TextRelationType RelationType { get; set; }
        /// <summary>
        /// Typ borderu
        /// </summary>
        public BorderStyleType xBorderStyle { get; set; }


        /// <summary>
        /// Aktuální typ rámečku textboxu. Při čtení má vždy hodnotu (nikdy není null).
        /// Dokud není explicitně nastavena hodnota, vrací se hodnota <see cref="BorderStyleDefault"/>.
        /// Lze setovat konkrétní explicitní hodnotu, anebo hodnotu null = tím se resetuje na barvu defaultní <see cref="BorderStyleDefault"/>.
        /// </summary>
        public BorderStyleType? BorderStyle
        {
            get { return this.__BorderStyle ?? this.BorderStyleDefault; }
            set { this.__BorderStyle = value; }
        }
        private BorderStyleType? __BorderStyle;
        /// <summary>
        /// Defaultní písmo
        /// </summary>
        public virtual BorderStyleType BorderStyleDefault { get { return Skin.TextBox.BorderStyle; } }

        #endregion
    }
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
