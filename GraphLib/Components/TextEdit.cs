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
    //    Co dalšího dodělat?
    //  1. Pohyb kurzoru => detekce kurzoru mimo viditelnou oblast => nastavení Shiftu pro automatický posun obsahu
    //  2. Alignment
    //   OK : 3. PasswordChar
    //  4. Napojení Text <=> Value <=> DataTable / Array
    //  5. Umožnit jednotlivé znaky textu podtrhávat / barevně odlišit
    //  6. Otestovat MultiLine, doplnit MultiLine SelectBounds


    /// <summary>
    /// GTextEdit (=TextBox)
    /// </summary>
    public class GTextEdit : GTextObject, ITextEditInternal
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
        #region Vstupní události (Focus, Klávesnice, Myš) a jejich předání do Editoru
        /// <summary>
        /// Při vstupu focusu do prvku
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedFocusEnter(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusEnter(e);
            OnCreateEditor();
            EditorState.EventFocusEnter(e);
        }
        /// <summary>
        /// Při odchodu focusu z prvku
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedFocusLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusLeave(e);
            EditorState.EventFocusLeave(e);
            OnReleaseEditor();
            OnEditorEnds();
        }
        /// <summary>
        /// Test klávesy
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedKeyPreview(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedKeyPreview(e);
            EditorState.EventKeyPreview(e);
        }
        /// <summary>
        /// Po stisknutí klávesy
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedKeyDown(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedKeyDown(e);
            EditorState.EventKeyDown(e);
        }
        /// <summary>
        /// Po zmáčknutí a uvolnění znaku
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedKeyPress(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedKeyPress(e);
            EditorState.EventKeyPress(e);
        }
        /// <summary>
        /// Po levém kliknutí
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseLeftDown(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseLeftDown(e);
            if (IsRightIconClicked(e)) OnRightIconClick(e);          // Kliknutí na Right ikonu
            else if (IsOverlayTextClicked(e)) OnOverlayTextClick(e); // nebo kliknutí na OverlayText
            else EditorState.EventMouseLeftDown(e);                  // jinak jde o kliknutí do textového políčka
        }
        /// <summary>
        /// Při pohybu myši: může řídit označování výběru pomocí myši
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseOver(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseOver(e);
            if (this.HasEditorState && e.MouseButtons == MouseButtons.Left)
                EditorState.EventMouseLeftOver(e);
        }
        /// <summary>
        /// Při zvednutí myši: ukončí označování výběru pomocí myši
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseLeftUp(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseLeftUp(e);
            EditorState.EventMouseLeftUp(e);
        }
        /// <summary>
        /// Po levém double-kliknutí
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftDoubleClick(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedLeftDoubleClick(e);
            // DoubleClick se nevolá, když se klikne na aktivní prvky (Right ikonu nebo OverlayText), tam se volá prostý klik (a ten už proběhl při MouseLeftDown):
            bool isOnActiveItems = (IsRightIconClicked(e) || IsOverlayTextClicked(e));
            if (!isOnActiveItems)
                OnTextDoubleClick(e);
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
        public TextEditorController EditorState
        {
            get
            {
                if (_EditorState == null && this.HasFocus) OnCreateEditor();
                return _EditorState;
            }
            set { _EditorState = value; }
        }
        /// <summary>
        /// Obsahuje true pokud existuje <see cref="EditorState"/>
        /// </summary>
        protected bool HasEditorState { get { return (_EditorState != null); } }
        private TextEditorController _EditorState;
        /// <summary>
        /// Metoda, která má vytvořit editor do property <see cref="EditorState"/>.
        /// Vyvolá event <see cref="CreateEditor"/>, a pokud ten nevytvoří editor, pak metoda vytvoří vlastní defaultní editor.
        /// </summary>
        protected virtual void OnCreateEditor()
        {
            CreateEditor?.Invoke(this, EventArgs.Empty);                       // Event?
            if (_EditorState != null) return;                                  // Pokud je hotovo (tj. eventhandler vytvořil editor vlastními silami), pak je hotovo :-)
            _EditorState = new TextEditorController(this, EditorStateData);    // Vygenerujeme defaultní editor
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
        /// String, který uchovává klíčová data z <see cref="TextEditorController"/> v době, kdy textbox nemá focus a instance v <see cref="EditorState"/> je zahozena (v LostFocus).
        /// Z tohoto stringu je při návratu Focusu do this TextBoxu vytvořen (restorován) nový objekt <see cref="TextEditorController"/>.
        /// </summary>
        protected string EditorStateData;
        /// <summary>
        /// Prostor pro persistenci dat externího editoru
        /// </summary>
        public object EditorUserData { get; set; }
        #endregion
        #region Speciální klávesy
        /// <summary>
        /// Metoda detekuje speciální klávesy typické pro <see cref="GTextEdit"/> a zpracuje je.
        /// Vyvolá odpovídající eventhandler.
        /// Vrací true = klávesa byla zpracována.
        /// </summary>
        /// <param name="args"></param>
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
            ITextBoxStyle style = this.StyleCurrent;
            TextBoxBorderType borderType = style.BorderType;
            int borderWidth = GPainter.GetBorderWidth(borderType);
            int textMargin = style.TextMargin;
            FontInfo font = style.Font;
            int fontHeight = FontManagerInfo.GetFontHeight(font);
            DrawCheckHeight(ref absoluteBounds, ref absoluteVisibleBounds, fontHeight, textMargin, borderWidth);
            Rectangle innerBounds = absoluteBounds.Enlarge(-borderWidth);
            Rectangle textBounds = innerBounds.Enlarge(-textMargin);

            GTextEditDrawArgs drawArgs = new GTextEditDrawArgs(e, absoluteBounds, absoluteVisibleBounds, innerBounds, textBounds, 
                style, font, borderType, borderWidth, textMargin, fontHeight, 
                drawMode, this.HasFocus, this.InteractiveState, this);

            this.DetectRightIconBounds(drawArgs);               // Nastaví RightIconBounds a modifikuje TextBounds
            this.DetectOverlayBounds(drawArgs, OverlayText);    // Nastaví OverlayBounds a modifikuje TextBounds
            this.DrawBackground(drawArgs);                      // Background
            this.DrawOverlay(drawArgs, OverlayBackground);      // Grafika nad Backgroundem
            this.DrawText(drawArgs);                            // Text
            this.DrawRightIcon(drawArgs);                       // Ikona vpravo
            this.DrawOverlay(drawArgs, OverlayText);            // Grafika nad Textem
            this.DrawBorder(drawArgs);                          // Rámeček
        }
        /// <summary>
        /// Zajistí, že this objekt bude mít výšku odpovídající aktuálnímu stylu
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        /// <param name="fontHeight"></param>
        /// <param name="textMargin"></param>
        /// <param name="borderWidth"></param>
        protected virtual void DrawCheckHeight(ref Rectangle absoluteBounds, ref Rectangle absoluteVisibleBounds, int fontHeight, int textMargin, int borderWidth)
        {
            if (this.Multiline) return;
            int height = (fontHeight + 2 * (textMargin + borderWidth));
            if (absoluteBounds.Height == height) return;

            Rectangle bounds = this.Bounds;
            bounds.Height = height;
            this.SetBounds(bounds, ProcessAction.None, EventSourceType.ApplicationCode);

            absoluteBounds.Height = height;
        }
        /// <summary>
        /// Vykreslí pozadí
        /// </summary>
        /// <param name="drawArgs">Argumenty</param>
        protected virtual void DrawBackground(GTextEditDrawArgs drawArgs)
        {
            Color backColor = drawArgs.Style.GetBackColor(drawArgs.InteractiveState);
            this.DrawBackground(drawArgs.DrawArgs, drawArgs.InnerBounds, drawArgs.AbsoluteVisibleBounds, drawArgs.DrawMode, backColor);
        }
        /// <summary>
        /// Vykreslí border
        /// </summary>
        /// <param name="drawArgs">Argumenty</param>
        protected virtual void DrawBorder(GTextEditDrawArgs drawArgs)
        {
            if (drawArgs.BorderWidth == 0) return;
            Color borderColor = drawArgs.Style.GetBorderColor(drawArgs.InteractiveState);
            GPainter.DrawBorder(drawArgs.Graphics, drawArgs.AbsoluteBounds, borderColor, drawArgs.BorderType, drawArgs.InteractiveState);
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
            Rectangle textBounds = drawArgs.TextBounds;
            this.TextBounds = textBounds;
            if (!textBounds.HasPixels()) return;

            string text = this.Text;
            Graphics graphics = drawArgs.Graphics;
            FontInfo fontInfo = this.FontCurrent;
            Point textShift = this.TextShift;
            Color? backColorStd = null;
            Color? fontColorStd = drawArgs.Style.GetTextColor(drawArgs.InteractiveState);
            Color? backColorSel = drawArgs.Style.BackColorSelectedText;
            Color? fontColorSel = drawArgs.Style.TextColorSelectedText;
            HorizontalAlignment alignment = this.Alignment;
            if (HasFocus && HasEditorState)
            {   // S Focusem a editorem: 
                CheckCharPositions(drawArgs, true);                            // Provedu před GraphicsClip(), aby měření fontu nebylo omezeno
                EditorState.MouseDownDataProcess();                            // Provedu před kreslením, může se zde určit oblast SelectionRange
                Int32Range selectionRange = this.SelectionRangeNormalised;
                bool hasSelectionRange = (selectionRange != null && selectionRange.Size != 0);
                using (GPainter.GraphicsUseText(drawArgs.Graphics, textBounds))
                {
                    foreach (var charPosition in CharPositions)
                    {
                        bool isSelected = (hasSelectionRange && selectionRange.Contains(charPosition.Index, false));
                        charPosition.DrawText(graphics, fontInfo, textBounds, textShift, backColor: (!isSelected ? backColorStd : backColorSel), fontColor: (!isSelected ? fontColorStd : fontColorSel));
                    }
                    DrawCursor(drawArgs);                                      // Vykreslím kurzor
                }
            }
            else
            {   // Bez Focusu nebo bez editoru = jen vypíšu text:
                CheckCharPositions(drawArgs, false);                           // Provedu před GraphicsClip(), aby měření fontu nebylo omezeno
                using (GPainter.GraphicsUseText(drawArgs.Graphics, textBounds))
                {
                    foreach (var charPosition in CharPositions)
                    {
                        charPosition.DrawText(graphics, fontInfo, textBounds, textShift, backColor: backColorStd, fontColor: fontColorStd);
                    }
                }
            }
        }
        /// <summary>
        /// Vykreslí Kurzor. Volá se výhradně v době, kdy máme Focus a Editor.
        /// </summary>
        /// <param name="drawArgs"></param>
        protected virtual void DrawCursor(GTextEditDrawArgs drawArgs)
        {
            CursorBounds = EditorState.GetCursorBounds();
            if (!CursorBounds.HasValue) return;

            GPainter.GraphicsSetSharp(drawArgs.Graphics);                      // Ostré okraje, aby byl kurzor správný
            drawArgs.Graphics.FillRectangle(Skin.Brush(Color.Black), CursorBounds.Value);
        }
        /// <summary>
        /// Souřadnice okénka pro vypisování textu, absolutní koordináty
        /// </summary>
        protected Rectangle TextBounds { get; set; }
        /// <summary>
        /// Souřadnice ikony v overlay (pouze "horní" overlay = <see cref="OverlayText"/>), kde bude overlay aktivní, absolutní koordináty
        /// </summary>
        protected Rectangle? OverlayTextBounds { get; set; }
        /// <summary>
        /// Posunutí obsahu textu proti souřadnici.
        /// Zde je uložena souřadnice relativního bodu v textu, který je zobrazen v levém horním rohu textovho okénka.
        /// Tedy: pokud je <see cref="TextShift"/> = (25, 0), pak v TextBoxu bude zobrazen první řádek (Y=0), ale až např. čtvrtý znak, jehož X = 25.
        /// Poznámka: souřadnice v <see cref="TextShift"/> by neměly být záporné, protože pak by obsah textu byl zobrazen odsunutý doprava/dolů.
        /// </summary>
        protected Point TextShift { get; set; }
        /// <summary>
        /// Souřadnice kurzoru. Jsou určeny při jeho vykreslení v procesu kreslení celého textu. Lze je použít pro animaci blikání kurzoru.
        /// </summary>
        protected Rectangle? CursorBounds { get; set; }
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
        #region Funkční prostor vpravo (ikona, dropdown)
        /// <summary>
        /// Ikona vykreslovaná v pravém (horním) rohu, ve velikosti výšky řádku. Ikona nedostává focus, ten vždy zůstává v this TextBoxu.
        /// Ikona je interaktivní, kliknutí na ikonu je předáno do eventu <see cref="RightIconClick"/>.
        /// Ikona může být: vztah, kalkulačka, dropdown, výběr souboru...
        /// Hodnota null = žádná ikona, žádná událost.
        /// </summary>
        public InteractiveIcon RightActiveIcon { get { return _RightActiveIcon; } set { _RightActiveIcon = value; Invalidate(); } } private InteractiveIcon _RightActiveIcon;
        /// <summary>
        /// Nastaví <see cref="RightIconBounds"/> a modifikuje <see cref="GTextEditDrawArgs.TextBounds"/>
        /// </summary>
        /// <param name="drawArgs"></param>
        protected void DetectRightIconBounds(GTextEditDrawArgs drawArgs)
        {
            if (RightActiveIcon == null)
            {
                RightIconBounds = null;
                return;
            }

            int iconSize = drawArgs.FontHeight + 2 * drawArgs.TextMargin;
            Rectangle innerBounds = drawArgs.InnerBounds;
            if (iconSize > innerBounds.Height) iconSize = innerBounds.Height;
            Rectangle rightIconBounds = new Rectangle(innerBounds.Right - iconSize, innerBounds.Y, iconSize, iconSize);
            drawArgs.RightIconBounds = rightIconBounds;
            RightIconBounds = rightIconBounds;

            Rectangle textBounds = drawArgs.TextBounds;
            drawArgs.TextBounds = new Rectangle(textBounds.X, textBounds.Y, rightIconBounds.X - textBounds.X, textBounds.Height);
        }
        /// <summary>
        /// Vykreslí ikonu
        /// </summary>
        /// <param name="drawArgs"></param>
        protected void DrawRightIcon(GTextEditDrawArgs drawArgs)
        {
            if (RightActiveIcon == null || !drawArgs.RightIconBounds.HasValue) return;
            bool asShadow = !(drawArgs.HasFocus || drawArgs.InteractiveState.HasFlag(GInteractiveState.FlagOver));
            RightActiveIcon.DrawIcon(drawArgs.Graphics, drawArgs.RightIconBounds.Value, this.InteractiveState, asShadow);
        }
        /// <summary>
        /// Detekuje, zda bylo kliknuto na ikonu vpravo <see cref="RightActiveIcon"/> = do prostoru <see cref="RightIconBounds"/>, vrací true / false. 
        /// Nevyvolává <see cref="OnRightIconClick"/>!
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual bool IsRightIconClicked(GInteractiveChangeStateArgs e)
        {
            if (RightActiveIcon == null || !RightIconBounds.HasValue) return false;
            return RightIconBounds.Value.Contains(e.MouseAbsolutePoint.Value);
        }
        /// <summary>
        /// Prostor klikací ikony vpravo
        /// </summary>
        protected Rectangle? RightIconBounds { get; set; }
        #endregion
        #region Overlay - kreslení, detekce prostoru a detekce kliknutí
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
            this.OverlayTextBounds = drawArgs.OverlayBounds;
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
        /// Detekuje, zda bylo kliknuto na overlay <see cref="OverlayText"/> = do prostoru <see cref="OverlayTextBounds"/>, vrací true / false. 
        /// Nevyvolává <see cref="OnOverlayTextClick"/>!
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual bool IsOverlayTextClicked(GInteractiveChangeStateArgs e)
        {
            var overlayText = OverlayText;
            if (overlayText == null || !OverlayTextBounds.HasValue) return false;
            return OverlayTextBounds.Value.Contains(e.MouseAbsolutePoint.Value);
        }
        #endregion
        #region Výška řádku a výška textboxu: defaultní, aktuální. Abstract overrides. Zajištění správné výšky objektu.
        /// <summary>
        /// Obsahuje výšku řádku textu, bez okrajů <see cref="TextBorderStyle.TextMargin"/> a bez borderu <see cref="TextBorderStyle.BorderType"/>.
        /// </summary>
        public int OneTextLineHeightCurrent
        {
            get
            {
                ITextBoxStyle style = this.StyleCurrent;
                FontInfo font = style.Font;
                return FontManagerInfo.GetFontHeight(font);
            }
        }
        /// <summary>
        /// Optimální výška textboxu pro správné zobrazení jednořádkového textu.
        /// Výška zahrnuje aktuální velikost okrajů dle <see cref="BorderStyle"/> plus vnitřní okraj <see cref="TextBorderStyle.TextMargin"/> plus výšku řádku text <see cref="OneTextLineHeightCurrent"/>.
        /// </summary>
        public int SingleLineOptimalHeightCurrent
        {
            get
            {
                ITextBoxStyle style = this.StyleCurrent;
                TextBoxBorderType borderType = style.BorderType;
                int borderWidth = GPainter.GetBorderWidth(borderType);
                int textMargin = style.TextMargin;
                FontInfo font = style.Font;
                int fontHeight = FontManagerInfo.GetFontHeight(font);
                return (fontHeight + 2 * (textMargin + borderWidth));
            }
        }
        /// <summary>
        /// V této metodě může potomek změnit (ref) souřadnice, na které je objekt právě umisťován.
        /// Tato metoda je volána při Bounds.set().
        /// Tato metoda typicky koriguje velikost vkládaného prostoru podle vlastností potomka.
        /// Typickým příkladem je <see cref="GTextEdit"/>, který upravuje výšku objektu podle nastavení <see cref="GTextEdit.Multiline"/> a parametrů stylu.
        /// Bázová třída <see cref="InteractiveObject"/> nedělá nic.
        /// </summary>
        /// <param name="bounds"></param>
        protected override void ValidateBounds(ref Rectangle bounds)
        {
            if (this.Multiline) return;
            int height = SingleLineOptimalHeightCurrent;
            if (bounds.Height != height)
                bounds.Height = height;
        }
        /// <summary>
        /// Aktuální typ rámečku. Změnit lze přes styl: <see cref="Style"/> nebo <see cref="StyleParent"/> nebo <see cref="Styles.TextBox"/>.
        /// </summary>
        public TextBoxBorderType BorderTypeCurrent { get { return this.StyleCurrent.BorderType; } }
        /// <summary>
        /// Zde potomek deklaruje barvu písma
        /// </summary>
        protected override Color TextColorCurrent
        {
            get
            {
                ITextBoxStyle style = this.StyleCurrent;
                Color fontColorStd = style.GetTextColor(this.InteractiveState);
                return style.TextColor;
            }
        }
        /// <summary>
        /// Zde potomek deklaruje typ písma
        /// </summary>
        protected override FontInfo FontCurrent
        {
            get
            {
                ITextBoxStyle style = this.StyleCurrent;
                return style.Font;
            }
        }
        #endregion
        #region Analýza textu a fontu na pozice znaků
        /// <summary>
        /// Metoda zajistí, že v poli <see cref="CharPositions"/> budou platná data pro vykreslení aktuálního textu v aktuálním fontu.
        /// Pozor, souřadnice jsou relativní k počátku prostoru <see cref="GTextEditDrawArgs.TextBounds"/>, případné posuny (při posunu obsahu TextBoxu) je nutno dopočítat následně.
        /// </summary>
        /// <param name="drawArgs">Data pro kreslení</param>
        /// <param name="forEditing">true = pro editaci / false = pouze pro vykreslení</param>
        protected void CheckCharPositions(GTextEditDrawArgs drawArgs, bool forEditing)
        {
            if (!IsCharPositionsValid(drawArgs, forEditing))
                // Něco se liší od poslední analýzy => provedeme analýzu novou:
                AnalyseCharPositions(drawArgs, forEditing);
        }
        /// <summary>
        /// Na základě aktuálních dat vypočítá a uloží souřadnice znaků.
        /// </summary>
        /// <param name="drawArgs">Data pro kreslení</param>
        /// <param name="forEditing">true = pro editaci / false = pouze pro vykreslení</param>
        protected void AnalyseCharPositions(GTextEditDrawArgs drawArgs, bool forEditing)
        {
            string text = Text;
            FontInfo currentFont = FontCurrent;
            bool isPasswordActive = IsPasswordActive;
            bool multiline = Multiline;
            bool wordWrap = WordWrap;
            int? width = (wordWrap ? (int?)drawArgs.TextBounds.Width : (int?)null);
            Rectangle visibleBounds = new Rectangle(Point.Empty, drawArgs.TextBounds.Size);
            char? passwordChar = (isPasswordActive ? PasswordChar : (char?)null);
            FontMeasureParams parameters = new FontMeasureParams() { Origin = new Point(0, 0), Multiline = multiline, Width = width, WrapWord = true, PasswordChar = passwordChar, CreateLineInfo = forEditing };
            var characters = FontManagerInfo.GetCharInfo(text, drawArgs.Graphics, currentFont.Font, parameters);
            if (!forEditing)
            {   // Pokud připravujeme data jen pro kreslení a ne pro editaci, pak si uschováme pouze souřadnice VIDITELNÝCH znaků:
                characters = characters.Where(c => c.IsVisibleInBounds(visibleBounds)).ToArray();
            }
            _CharPositions = characters;
            _AnalysedText = text;
            _AnalysedFontKey = currentFont.Key;
            _AnalysedVisibleBounds = visibleBounds;
            _AnalysedTextWidth = width ?? 0;
            _AnalysedIsPasswordActive = isPasswordActive;
            _AnalysedMultiline = multiline;
            _AnalysedWordWrap = wordWrap;
            _AnalysedForEditing = forEditing;
        }
        /// <summary>
        /// Vrátí true, pokud aktuální pozice znaků v poli <see cref="CharPositions"/> jsou platné pro aktuální stav.
        /// </summary>
        protected bool IsCharPositionsValid(GTextEditDrawArgs drawArgs, bool forEditing)
        {
            return (CharPositions != null &&
                   String.Equals(Text, _AnalysedText, StringComparison.InvariantCulture) &&
                   String.Equals(FontCurrent.Key, _AnalysedFontKey, StringComparison.InvariantCulture) &&
                   forEditing == _AnalysedForEditing &&
                   IsPasswordActive == _AnalysedIsPasswordActive &&
                   Multiline == _AnalysedMultiline &&
                   WordWrap == _AnalysedWordWrap &&
                   (WordWrap ? drawArgs.TextBounds.Width == _AnalysedTextWidth : true));
        }
        /// <summary>
        /// Při ukončení editace
        /// </summary>
        protected void OnEditorEnds()
        {
            if (IsPasswordProtect && IsPasswordVisible) IsPasswordVisible = false;                      // Provede mj. invalidaci => znovuvykreslení
            Rectangle visibleBounds = _AnalysedVisibleBounds;
            _CharPositions.ForEachItem(c => c.RemoveLine());                                            // Zrušit instance řádků včetně všech referencí
            _CharPositions = _CharPositions.Where(c => c.IsVisibleInBounds(visibleBounds)).ToArray();   // Ponecháme si jen viditelné znaky
            _AnalysedForEditing = false;
        }
        /// <summary>
        /// Pole znaků, jejich pozic a souřadnic pro kreslení znaků i vykreslení pozadí.
        /// Souřadnice jsou relativní = začínají na bodu 0/0, při vykreslování je možné je jako celek posouvat a tím scrollovat textem.
        /// </summary>
        protected CharPositionInfo[] CharPositions { get { return _CharPositions; } }
        /// <summary>
        /// Pole znaků, jejich pozic a souřadnic pro kreslení znaků i vykreslení pozadí.
        /// </summary>
        private CharPositionInfo[] _CharPositions;
        /// <summary>
        /// Stávající analyzovaný text v poli <see cref="CharPositions"/>
        /// </summary>
        private string _AnalysedText;
        /// <summary>
        /// Font, pro který byl analyzován text v poli <see cref="CharPositions"/>
        /// </summary>
        private string _AnalysedFontKey;
        /// <summary>
        /// Viditelné souřadnice textu
        /// </summary>
        private Rectangle _AnalysedVisibleBounds;
        /// <summary>
        /// Hodnota <see cref="GTextEditDrawArgs.TextBounds"/>.Width, pro kterou byl analyzován text v poli <see cref="CharPositions"/>
        /// </summary>
        private int _AnalysedTextWidth;
        /// <summary>
        /// Hodnota <see cref="IsPasswordActive"/>, pro kterou byl analyzován text v poli <see cref="CharPositions"/>
        /// </summary>
        private bool _AnalysedIsPasswordActive { get { return Properties.GetBitValue(BitAnalysedIsPasswordActive); } set { Properties.SetBitValue(BitAnalysedIsPasswordActive, value); } }
        /// <summary>
        /// Hodnota <see cref="Multiline"/>, pro kterou byl analyzován text v poli <see cref="CharPositions"/>
        /// </summary>
        private bool _AnalysedMultiline { get { return Properties.GetBitValue(BitAnalysedMultiline); } set { Properties.SetBitValue(BitAnalysedMultiline, value); } }
        /// <summary>
        /// Hodnota <see cref="WordWrap"/>, pro kterou byl analyzován text v poli <see cref="CharPositions"/>
        /// </summary>
        private bool _AnalysedWordWrap { get { return Properties.GetBitValue(BitAnalysedWordWrap); } set { Properties.SetBitValue(BitAnalysedWordWrap, value); } }
        /// <summary>
        /// Analyzovaný text je pro editaci?
        /// </summary>
        private bool _AnalysedForEditing { get { return Properties.GetBitValue(BitAnalysedForEditing); } set { Properties.SetBitValue(BitAnalysedForEditing, value); } }
        #endregion
        #region Vzhled objektu
        /// <summary>
        /// Styl tohoto konkrétního textboxu. 
        /// Zahrnuje veškeré vizuální vlastnosti.
        /// Výchozí je null, pak se styl přebírá z <see cref="StyleParent"/>, anebo společný z <see cref="Styles.TextBox"/>.
        /// <para/>
        /// Do této property se typicky vkládá new instance, která řeší vzhled jednoho konkrétního prvku.
        /// </summary>
        public TextBoxStyle Style { get; set; }
        /// <summary>
        /// Společný styl, deklarovaný pro více textboxů. 
        /// Zde je reference na tuto instanci. 
        /// Modifikace hodnot v této instanci se projeví ve všech ostatních textboxech, které ji sdílejí.
        /// Výchozí je null, pak se styl přebírá společný z <see cref="Styles.TextBox"/>.
        /// <para/>
        /// Do této property se typicky vkládá odkaz na instanci, která je primárně uložena jinde, a řeší vzhled ucelené skupiny prvků.
        /// </summary>
        public TextBoxStyle StyleParent { get; set; }
        /// <summary>
        /// Aktuální styl, nikdy není null. 
        /// Obsahuje <see cref="Style"/> ?? <see cref="StyleParent"/> ?? <see cref="Styles.TextBox"/>.
        /// </summary>
        protected ITextBoxStyle StyleCurrent { get { return (this.Style ?? this.StyleParent ?? Styles.TextBox); } }
        #endregion
        #region Zastaralé
        /*

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
        protected override Color CurrentBackColor
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
        protected override Color CurrentTextColor
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
        /// Vnitřní okraj mezi Border a Textem, počet pixelů.
        /// Při čtení má vždy hodnotu (nikdy není null). Lze setovat požadovanou hodnotu. Lze setovat hodnotu null, tím se nastaví defaultní hodnota podle aktuálního skinu (výchozí stav).
        /// Platné hodnoty jsou 0 - 16.
        /// </summary>
        public int? TextMargin { get { return __TextMargin ?? TextMarginDefault; } set { __TextMargin = (value.HasValue ? (int?)(value.Value < 0 ? 0 : (value.Value > 16 ? 16 : value.Value)) : value); Invalidate(); } }
        private int? __TextMargin;
        /// <summary>
        /// Defaultní hodnota Vnitřní okraj mezi Border a Textem, počet pixelů.
        /// </summary>
        protected virtual int TextMarginDefault { get { return Skin.TextBox.TextMargin; } }
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
        */
        #endregion
        #region Public vlastnosti definující chování (TabStop, ...)
        /// <summary>
        /// Prvek může dostat focus při pohybu Tab / Ctrl+Tab
        /// </summary>
        public bool TabStop { get { return this.Is.TabStop; } set { this.Is.TabStop = value; } }
        /// <summary>
        /// Zástupný znak pro zobrazování hesla, null = default = běžné zobrazení.
        /// Hodnota může být zadaná trvale, a poté lze pomocí vlastnosti <see cref="IsPasswordVisible"/> na true nastavit, že heslo bude viditelné (typicky pomocí RightIcon)
        /// </summary>
        public char? PasswordChar { get; set; }
        /// <summary>
        /// Obsahuje true, pokud this TextBox je v režimu Password (pak je omezeno kopírování do schránky)
        /// </summary>
        protected bool IsPasswordProtect { get { return (PasswordChar.HasValue); } }
        /// <summary>
        /// Obsahuje true, pokud this TextBox aktuálně zobrazuje znaky PasswordChar (tj. jsou zadané, a nejsou potlačené)
        /// </summary>
        protected bool IsPasswordActive { get { return (IsPasswordProtect && !IsPasswordVisible); } }
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
        public Int32Range SelectionRange
        {
            get { return _SelectionRange; }
            set
            {
                if (value == null)
                    _SelectionRange = null;
                else
                {
                    int length = this.Text.Length;
                    int begin = value.Begin;
                    begin = (begin < 0 ? 0 : (begin > length ? length : begin));         // Begin i End smí být v rozsahu { 0 až Length }
                    int end = value.End;
                    end = (end < 0 ? 0 : (end > length ? length : end));                 // End může být menší než Begin!
                    _SelectionRange = new Int32Range(begin, end);
                }
                Invalidate();
            }
        }
        private Int32Range _SelectionRange;
        /// <summary>
        /// Rozsah vybraného (označeného) textu. Může být NULL. Normalised značí, že Begin je menší nebo rovno End, 
        /// na rozdíl od <see cref="SelectionRange"/> kde Begin může být větší než End - protože Begin je pozice, kde výběr začal, kdežto End je aktuální pozice konce výběru = to může být i vlevo.
        /// Pokud velikost (Size) je 0, pak <see cref="SelectionRangeNormalised"/> je null.
        /// <para/>
        /// Pozor, hodnota smí být NULL!
        /// </summary>
        protected Int32Range SelectionRangeNormalised
        {
            get
            {
                Int32Range selectionRange = _SelectionRange;
                if (selectionRange == null || selectionRange.Size == 0) return null;
                if (selectionRange.Size > 0) return selectionRange;
                return new Int32Range(selectionRange.End, selectionRange.Begin);
            }
        }
        /// <summary>
        /// Znaky textu včetně pozic
        /// </summary>
        CharPositionInfo[] ITextEditInternal.CharPositions { get { return _CharPositions; } }
        /// <summary>
        /// Oblast výběru, která při vložení hodnoty neprovádí kontroly (rychlost), přebírí přímo instanci, a neprovádí invalidaci
        /// </summary>
        Int32Range ITextEditInternal.SelectionRangeInternal { get { return _SelectionRange; } set { _SelectionRange = value; } }
        /// <summary>
        /// Souřadnice okénka pro vypisování textu, absolutní koordináty
        /// </summary>
        Rectangle ITextEditInternal.TextBounds { get { return TextBounds; } }
        /// <summary>
        /// Posunutí obsahu textu proti souřadnici.
        /// Zde je uložena souřadnice relativního bodu v textu, který je zobrazen v levém horním rohu textovho okénka.
        /// Tedy: pokud je <see cref="TextShift"/> = (25, 0), pak v TextBoxu bude zobrazen první řádek (Y=0), ale až např. čtvrtý znak, jehož X = 25.
        /// Poznámka: souřadnice v <see cref="TextShift"/> by neměly být záporné, protože pak by obsah textu byl zobrazen odsunutý doprava/dolů.
        /// </summary>
        Point ITextEditInternal.TextShift { get { return TextShift; } set { TextShift = value; } }
        /// <summary>
        /// Přídavné vykreslení přes Background, pod text
        /// </summary>
        public ITextEditOverlay OverlayBackground { get { return _OverlayBackground; } set { _OverlayBackground = value; Invalidate(); } } private ITextEditOverlay _OverlayBackground;
        /// <summary>
        /// Přídavné vykreslení přes Text
        /// </summary>
        public ITextEditOverlay OverlayText { get { return _OverlayText; } set { _OverlayText = value; Invalidate(); } } private ITextEditOverlay _OverlayText;
        /// <summary>
        /// Zarovnání textu, výchozí = vlevo
        /// </summary>
        public HorizontalAlignment Alignment
        {
            get
            {   // Použiju bitové úložiště, šetřím paměť
                if (Properties.GetBitValue(BitAlignmentCenter)) return HorizontalAlignment.Center;
                if (Properties.GetBitValue(BitAlignmentRight)) return HorizontalAlignment.Right;
                return HorizontalAlignment.Left;
            }
            set
            {
                Properties.SetBitValue(BitAlignmentCenter, (value == HorizontalAlignment.Center));
                Properties.SetBitValue(BitAlignmentRight, (value == HorizontalAlignment.Right));
                Invalidate();
            }
        }
        /// <summary>
        /// Je povolen víceřádkový text? Tedy například Poznámka...
        /// </summary>
        public bool Multiline { get { return Properties.GetBitValue(BitMultiline); } set { Properties.SetBitValue(BitMultiline, value); Invalidate(); } }
        /// <summary>
        /// Pokud je povolen víceřádkový text, má se text automaticky zalamovat podle šířky Textboxu na pozicích slova?
        /// </summary>
        public bool WordWrap { get { return Properties.GetBitValue(BitWordWrap); } set { Properties.SetBitValue(BitWordWrap, value); Invalidate(); } }
        /// <summary>
        /// Obsahuje true pro prvek, jehož hodnota má být zadaná, nikoli Empty.
        /// Pak se Border vykresluje barvou 
        /// </summary>
        public bool IsRequiredValue { get { return Properties.GetBitValue(BitIsRequiredValue); } set { Properties.SetBitValue(BitIsRequiredValue, value); Invalidate(); } }
        /// <summary>
        /// Nastavením na true lze provést zobrazení čitelného hesla, pokud je zadán znak <see cref="PasswordChar"/>.
        /// Výchozí hodnota je false = pokud je zadán znak <see cref="PasswordChar"/>, pak je heslo nečitelné.
        /// Při odchodu focusu z tohoto textboxu je <see cref="IsPasswordVisible"/> nastaveno na false.
        /// </summary>
        public bool IsPasswordVisible { get { return Properties.GetBitValue(BitIsPasswordVisible); } set { Properties.SetBitValue(BitIsPasswordVisible, value); Invalidate(); } }
        /// <summary>
        /// Označit (selectovat) celý obsah textu po příchodu focusu do prvku? Platí i pro příchod myším kliknutím.
        /// Při čtení má vždy hodnotu. 
        /// Setovat lze null, pak bude čtena hodnota defaultní (to je výchozí stav) = <see cref="Settings.TextBoxSelectAll"/>.
        /// </summary>
        public bool? SelectAllText
        {
            get { return (Properties.GetBitValue(SelectAllTextExplicit) ? Properties.GetBitValue(BitSelectAllText) : Settings.TextBoxSelectAll); }
            set
            {
                Properties.SetBitValue(SelectAllTextExplicit, value.HasValue); // Pokud je dodaná hodnota, pak je explicitní (SelectAllTextExplicit je true)
                Properties.SetBitValue(BitSelectAllText, (value ?? false));    //   .. pokud není dodaná hodnota, nastavíme Explicit = false, a hodnota (SelectAllText) taky false
            }
        }
        #endregion
        #region BitStorage Properties a hodnoty bitů
        /// <summary>Bit pro hodnotu Multiline</summary>
        protected const UInt32 BitMultiline = 0x00000001;
        /// <summary>Bit pro hodnotu WordWrap</summary>
        protected const UInt32 BitWordWrap = 0x00000002;
        /// <summary>Bit pro hodnotu IsRequiredValue</summary>
        protected const UInt32 BitIsRequiredValue = 0x00000004;
        /// <summary>Bit pro hodnotu Alignment = Center</summary>
        protected const UInt32 BitAlignmentCenter = 0x00000010;
        /// <summary>Bit pro hodnotu Alignment = Right</summary>
        protected const UInt32 BitAlignmentRight = 0x00000020;
        /// <summary>Bit pro hodnotu IsPasswordVisible</summary>
        protected const UInt32 BitIsPasswordVisible = 0x00000100;
        /// <summary>Bit pro hodnotu _AnalysedIsPasswordActive</summary>
        protected const UInt32 BitAnalysedIsPasswordActive = 0x00010000;
        /// <summary>Bit pro hodnotu _AnalysedMultiline</summary>
        protected const UInt32 BitAnalysedMultiline = 0x00020000;
        /// <summary>Bit pro hodnotu _AnalysedWordWrap</summary>
        protected const UInt32 BitAnalysedWordWrap = 0x00040000;
        /// <summary>Bit pro hodnotu _AnalysedForEditing</summary>
        protected const UInt32 BitAnalysedForEditing = 0x00080000;
        /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="SelectAllText"/></summary>
        protected const UInt32 BitSelectAllText = 0x02000000;
        /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="SelectAllTextExplicit"/></summary>
        protected const UInt32 SelectAllTextExplicit = 0x04000000;
        /// <summary>
        /// Úložiště bitových hodnot
        /// </summary>
        protected TextProperties Properties { get { if (_Properties == null) _Properties = new TextProperties(); return _Properties; } }
        private TextProperties _Properties;
        #endregion
        #region Public eventy
        /// <summary>
        /// Vyvolá event <see cref="RightIconClick"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnRightIconClick(GInteractiveChangeStateArgs args) { RightIconClick?.Invoke(this, args); }
        /// <summary>
        /// Událost vyvolaná po kliknutí na funkční ikonu vpravo
        /// </summary>
        public event GInteractiveChangeStateHandler RightIconClick;
        /// <summary>
        /// Vyvolá event <see cref="OverlayTextClick"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnOverlayTextClick(GInteractiveChangeStateArgs args) { OverlayTextClick?.Invoke(this, args); }
        /// <summary>
        /// Událost vyvolaná po kliknutí na <see cref="OverlayText"/>
        /// </summary>
        public event GInteractiveChangeStateHandler OverlayTextClick;
        /// <summary>
        /// Vyvolá event <see cref="TextDoubleClick"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnTextDoubleClick(GInteractiveChangeStateArgs args) { TextDoubleClick?.Invoke(this, args); }
        /// <summary>
        /// Událost vyvolaná po double-kliknutí do textu
        /// </summary>
        public event GInteractiveChangeStateHandler TextDoubleClick;
        #endregion
        #region Text, Value a DataBinding (napojení na data)
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
    }
    #region interface ITextEditInternal : Rozhraní pro interní přístup do TextBoxu v procesu editace
    /// <summary>
    /// <see cref="ITextEditInternal"/> : Rozhraní pro interní přístup do TextBoxu v procesu editace
    /// </summary>
    public interface ITextEditInternal
    {
        /// <summary>
        /// Znaky textu včetně pozic
        /// </summary>
        CharPositionInfo[] CharPositions { get; }
        /// <summary>
        /// Oblast výběru, která při vložení hodnoty neprovádí kontroly (rychlost), přebírí přímo instanci, a neprovádí invalidaci
        /// </summary>
        Int32Range SelectionRangeInternal { get; set; }
        /// <summary>
        /// Souřadnice okénka pro vypisování textu, absolutní koordináty
        /// </summary>
        Rectangle TextBounds { get; }
        /// <summary>
        /// Posunutí obsahu textu proti souřadnici.
        /// Zde je uložena souřadnice relativního bodu v textu, který je zobrazen v levém horním rohu textovho okénka.
        /// Tedy: pokud je <see cref="TextShift"/> = (25, 0), pak v TextBoxu bude zobrazen první řádek (Y=0), ale až např. čtvrtý znak, jehož X = 25.
        /// Poznámka: souřadnice v <see cref="TextShift"/> by neměly být záporné, protože pak by obsah textu byl zobrazen odsunutý doprava/dolů.
        /// </summary>
        Point TextShift { get; set; }
    }
    #endregion
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
        /// <param name="textBounds"></param>
        /// <param name="style"></param>
        /// <param name="font"></param>
        /// <param name="borderType"></param>
        /// <param name="borderWidth"></param>
        /// <param name="textMargin"></param>
        /// <param name="fontHeight"></param>
        /// <param name="drawMode"></param>
        /// <param name="hasFocus"></param>
        /// <param name="interactiveState"></param>
        /// <param name="textEdit"></param>
        public GTextEditDrawArgs(GInteractiveDrawArgs drawArgs, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, Rectangle innerBounds, Rectangle textBounds,
            ITextBoxStyle style, FontInfo font, TextBoxBorderType borderType, int borderWidth, int textMargin, int fontHeight, 
            DrawItemMode drawMode, 
            bool hasFocus, GInteractiveState interactiveState, GTextEdit textEdit)
        {
            this.DrawArgs = drawArgs;
            this.AbsoluteBounds = absoluteBounds;
            this.AbsoluteVisibleBounds = absoluteVisibleBounds;
            this.InnerBounds = innerBounds;
            this.TextBounds = textBounds;
            this.Style = style;
            this.Font = font;
            this.BorderType = borderType;
            this.BorderWidth = borderWidth;
            this.TextMargin = textMargin;
            this.FontHeight = fontHeight;
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
        public Rectangle TextBounds { get; set; }
        /// <summary>
        /// Kompletní styl pro kreslení
        /// </summary>
        public ITextBoxStyle Style { get; set; }
        /// <summary>
        /// Informace o fontu, již vyhodnocený ze stylu
        /// </summary>
        public FontInfo Font { get; set; }
        /// <summary>
        /// Typ rámečku, již vyhodnocený ze stylu
        /// </summary>
        public TextBoxBorderType BorderType { get; set; }
        /// <summary>
        /// Počet pixelů rámečku, již vyhodnocený ze stylu
        /// </summary>
        public int BorderWidth { get; set; }
        /// <summary>
        /// Počet pixelů okraje mezi rámečkem a textem, již vyhodnocený ze stylu
        /// </summary>
        public int TextMargin { get; set; }
        /// <summary>
        /// Výška jednoho textového řádku = výška písma
        /// </summary>
        public int FontHeight { get; private set; }
        /// <summary>
        /// Prostor pro výhradní kreslení RightIcon. Výchozí hodnota je null. Nastavuje <see cref="GTextEdit"/> v části pro ikonu, zmenšuje přitom prostor <see cref="TextBounds"/>.
        /// </summary>
        public Rectangle? RightIconBounds { get; set; }
        /// <summary>
        /// Prostor pro výhradní kreslení Overlay. Výchozí hodnota je null. Je plně ve správě objektu <see cref="GTextEdit.OverlayText"/>.
        /// </summary>
        public Rectangle? OverlayBounds { get; set; }
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
    #region class TextEditorController : proměnné a funkce pro editaci platné pouze při přítomnosti Focusu
    /// <summary>
    /// <see cref="TextEditorController"/> : proměnné a funkce pro editaci TextBoxu.
    /// V jednom čase existuje nejvýše jedna intance, navázaná na ten <see cref="GTextEdit"/>, který má zrovna Focus.
    /// Může existovat tisíc vizuálních <see cref="GTextEdit"/>, ale nelze jich současně editovat víc než jeden.
    /// Controller <see cref="TextEditorController"/> obsahuje řadu proměnných, a je zbytečné je mít v paměti všechny i v době, kdy neprobíhá editace.
    /// </summary>
    public class TextEditorController : IDisposable
    {
        #region Konsturktor, základní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="data"></param>
        public TextEditorController(GTextEdit owner, string data)
        {
            _Owner = owner;
            Data = data;
            UndoRedoInit();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _Owner = null;
        }
        /// <summary>
        /// Vlastník = TextBox
        /// </summary>
        private GTextEdit _Owner;
        #endregion
        #region Editační property, reference na TextBox, výběr, pozice kurzoru, text z TextBoxu
        /// <summary>
        /// Vlastník = TextBox
        /// </summary>
        protected GTextEdit Owner { get { return _Owner; } }
        /// <summary>
        /// Vlastník = TextBox, přetypovaný na <see cref="ITextEditInternal"/> pro přístup do interních dat
        /// </summary>
        protected ITextEditInternal IOwner { get { return _Owner as ITextEditInternal; } }
        /// <summary>
        /// Znaky textu včetně pozic
        /// </summary>
        protected CharPositionInfo[] CharPositions { get { return IOwner.CharPositions; } }
        /// <summary>
        /// Obsahuje true pokud máme kladný počet znaků v poli <see cref="CharPositions"/>
        /// </summary>
        protected bool HasCharPositions { get { return (CharPositions != null && CharPositions.Length > 0); } }
        /// <summary>
        /// Obsahuje true pokud daný text lze editovat, false pokud je Disabled nebo ReadOnly.
        /// </summary>
        protected bool IsEditable { get { return (!_Owner.ReadOnly && _Owner.Enabled); } }
        /// <summary>
        /// Obsahuje true, pokud this TextBox je v režimu Password (pak je omezeno kopírování do schránky)
        /// </summary>
        protected bool IsPasswordProtect { get { return (_Owner.PasswordChar.HasValue); } }
        /// <summary>
        /// Obsahuje true, pokud this TextBox aktuálně zobrazuje znaky PasswordChar (tj. jsou zadané, a nejsou potlačené)
        /// </summary>
        protected bool IsPasswordActive { get { return (IsPasswordProtect && !_Owner.IsPasswordVisible); } }
        /// <summary>
        /// Data určená k perzistenci po dobu mimo editace TextBoxu, k restorování nové instance <see cref="TextEditorController"/> do předchozího stavu.
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
        /// Majitel povoluje víceřádkový text?
        /// </summary>
        protected bool Multiline { get { return this._Owner.Multiline; } }
        /// <summary>
        /// Majitel povoluje WordWrap?
        /// </summary>
        protected bool WordWrap { get { return this._Owner.WordWrap; } }
        /// <summary>
        /// Pozice kurzoru = před kterým znakem kurzor stojí.
        /// Hodnota 0 = kurzor je na začátku textu = před prvním znakem.
        /// Hodnota <see cref="Text"/>.Length = kurzor je na konci textu = za posledním znakem
        /// </summary>
        public int CursorIndex { get; private set; }
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
        protected Int32Range SelectionRange  { get { return IOwner.SelectionRangeInternal; } set { IOwner.SelectionRangeInternal = value; } }
        /// <summary>
        /// Obsahuje true, pokud výběr <see cref="SelectionRange"/> něco obsahuje a je třeba s ním pracovat (není null a jeho velikost není 0)
        /// </summary>
        protected bool SelectionRangeExists { get { var r = SelectionRange; return (r != null && r.Size != 0); } }
        /// <summary>
        /// Označený text. Pokud <see cref="SelectionRangeExists"/> je false, pak <see cref="SelectedText"/> je prázdný string.
        /// Čtená hodnota není nikdy null.
        /// Hodnotu lze vložit. Vložením se odstraní aktuálně označený text (pokud je označen). Pokud bude vloženo null nebo prázdný text, bude provedeno jen odstranění současného textu <see cref="SelectedText"/>.
        /// </summary>
        protected string SelectedText
        {
            get { return GetSelectedText(); }
            set
            {
                Int32Range deletionRange = (SelectionRangeExists ? SelectionRange : new Int32Range(this.CursorIndex, this.CursorIndex));
                UndoRedoAddCurrentBeforeChange();
                this.CursorIndex = DeleteInsertText(deletionRange, value);
                this.SelectionRange = null;
                this.Invalidate();
            }
        }
        /// <summary>
        /// Zajistí, že this objekt bude při nejbližší možnosti překrelsen. Neprovádí překreslení okamžitě, na rozdíl od metody <see cref="InteractiveObject.Refresh()"/>.
        /// </summary>
        protected void Invalidate()
        {
            this._Owner.Invalidate();
        }
        #endregion
        #region Vstupní body z TextBoxu v procesu kreslení obsahu: převzetí pozice znaků, zpracování dat o MouseDown podle pozice znaků
        /// <summary>
        /// Zpracuje požadavky z kliknutí myši, uložené v metodě <see cref="EventMouseLeftDown(GInteractiveChangeStateArgs)"/>, po proběhnutí vykreslení a po uložení pozic znaků.
        /// Určí pozici kurzoru CursorPosition a rozsah výběru SelectedRange.
        /// Metoda je volána v procesu Draw. Po této metodě následuje vykreslení kurzoru.
        /// </summary>
        public void MouseDownDataProcess()
        {
            if (!MouseDownNeedProcess) return;
            MouseDownNeedProcess = false;

            // Máme data o stisku myši:
            Point relativePoint = GetRelativePoint(MouseDownPoint).Value;
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
                else if (modifiers == Keys.Control && Settings.TextBoxSelectWordOnControlMouse)
                {   // Byla stisknuta levá myš; buď jako příchod do prvku (FocusEnter) ale bez SelectAllText; anebo bez příchodu focusu. 
                    // Je stisknut Control (bez Shiftu) a ten Control se má interpretovat jako "Označ slovo pod myší" => jdeme na to:
                    bool isRightSide;
                    int cursorIndex = SearchCharIndex(relativePoint, out isRightSide);
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
                    int cursorIndex = SearchCharIndex(relativePoint);
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
                    this.CursorIndex = SearchCharIndex(relativePoint);
                }
            }
            else if (MouseDownButtons.HasValue && MouseDownButtons.Value == MouseButtons.Right)
            {   // Pravá myš => Kontextové menu: pokud je povoleno v proměnné ChangeCursorPositionOnRightMouse, a není označen žádný text,
                //  pak se nastaví kurzor podle pozice myši, ale neprovádí se žádná jiná funkcionalita.
                //  Pokud je nějaký text označen, nechává se beze změny jak SelectionRange, tak CursorIndex = aby se k tomu mohlo vztahovat kontextové menu:
                if (Settings.TextBoxChangeCursorPositionOnRightMouse && !this.SelectionRangeExists)
                {
                    this.CursorIndex = SearchCharIndex(relativePoint);
                }
            }
        }
        /// <summary>
        /// Zpracuje událost, kdy je myš přetahována přes text = označování textu myší
        /// </summary>
        /// <param name="absolutePoint"></param>
        protected void MouseDragDataProcess(Point absolutePoint)
        {
            Point relativePoint = GetRelativePoint(absolutePoint).Value;
            int cursorIndex = SearchCharIndex(relativePoint);
            Int32Range selectionRange = SelectionRange;
            if (selectionRange != null)
                // Máme již z dřívějška uložen nějaký výběr: ponecháme jeho Begin a změníme End:
                selectionRange = new Int32Range(selectionRange.Begin, cursorIndex);
            else
                // Dosud nebyl výběr: vezmeme jako počátek nového výběru dosavadní kurzor:
                selectionRange = new Int32Range(this.CursorIndex, cursorIndex);
            this.SelectionRange = selectionRange;
            this.CursorIndex = cursorIndex;
            this.Invalidate();
        }
        /// <summary>
        /// Metoda resetuje data o myším kliknutí - volá se po uvolnění myši (MouseUp).
        /// </summary>
        protected void MouseDownDataReset()
        {
            MouseDownIsFocusEnter = null;
            MouseDownButtons = null;
            MouseDownPoint = null;
            MouseDownModifierKeys = null;
        }
        /// <summary>
        /// Obsahuje true po většinu života (editor by neměl existovat, když textbox nemá focus)
        /// </summary>
        protected bool HasFocus { get; private set; }
        /// <summary>
        /// Obsahuje true po stisku myši, kdy je třeba zpracovat data o pozici myši vzhledem k pozicím znaků.
        /// Po tomto zpracování je nastaveno na false.
        /// </summary>
        protected bool MouseDownNeedProcess { get; private set; }
        /// <summary>
        /// Eviduje stav MouseDown a FocusEnter, zachycený v metodě <see cref="EventMouseLeftDown(GInteractiveChangeStateArgs)"/>:
        /// 1. null = výchozí hodnota = není MouseDown (platí i po zpracování hodnoty <see cref="MouseDownIsFocusEnter"/> v metodě <see cref="EventMouseLeftDown(GInteractiveChangeStateArgs)"/>)
        /// 2. false = je evidován MouseDown, ale v situaci, kdy už TextBox má focus (tzn. myš je stisknuta v již aktivním TextBoxu)
        /// 3. true = je evidován MouseDown, který přivedl Focus do TextBoxu odjinud = kliknuto zvenku do TextBoxu (pak je jiné chování)
        /// </summary>
        protected bool? MouseDownIsFocusEnter { get; private set; }
        /// <summary>
        /// Buttony myši stisknuté při MouseDown
        /// </summary>
        protected MouseButtons? MouseDownButtons { get; private set; }
        /// <summary>
        /// Absolutní souřadnice myši při MouseDown (absolutní - vzhledem k Host controlu).
        /// </summary>
        protected Point? MouseDownPoint { get; private set; }
        /// <summary>
        /// Modifikační klávesy aktivní při MouseDown
        /// </summary>
        protected Keys? MouseDownModifierKeys { get; private set; }
        #endregion
        #region Protected vyhledání pozice a souřadnic znaku, kurzoru, výběru
        /// <summary>
        /// Vrátí pozici kurzoru (index) odpovídající danému relativnímu bodu. 
        /// Opírá se přitom o pozice znaků <see cref="ITextEditInternal.CharPositions"/>.
        /// <para/>
        /// Tato varianta metody vrátí index následujícího znaku v případě, kdy daný bod bude na pravém okraji určitého znaku.
        /// Naproti tomu přetížení s out parametrem bool isRightSide neposune nalezený index doprava, ale vrátí informaci o umístění bodu v pravé polovině znaku.
        /// </summary>
        /// <param name="relativePoint">Zadaný relativní bod, k němuž hledáme znak na nejbližší pozici</param>
        /// <returns></returns>
        public int SearchCharIndex(Point relativePoint)
        {
            if (!this.HasCharPositions) return 0;

            bool isRightSide;
            int cursorPosition = SearchCharIndex(relativePoint, out isRightSide);
            if (isRightSide) cursorPosition++;

            return cursorPosition;
        }
        /// <summary>
        /// Vrátí pozici kurzoru (index) odpovídající danému relativnímu bodu. 
        /// Opírá se přitom o pozice znaků <see cref="ITextEditInternal.CharPositions"/>.
        /// <para/>
        /// Tato varianta metody vrátí přesnou pozici kurzoru na nalezeném znaku i když daný bod bude na jeho pravém okraji, 
        /// nastaví však informaci o pravé polovině do out parametru <paramref name="isRightSide"/>.
        /// </summary>
        /// <param name="relativePoint">Zadaný relativní bod, k němuž hledáme znak na nejbližší pozici</param>
        /// <param name="isRightSide">Out: zadaný bod leží v pravé polovině nalezeného znaku</param>
        /// <returns></returns>
        public int SearchCharIndex(Point relativePoint, out bool isRightSide)
        {
            isRightSide = false;
            if (!this.HasCharPositions) return 0;

            CharPositionInfo charInfo = LinePositionInfo.SearchCharacterByPosition(CharPositions, relativePoint, out isRightSide);
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
            if (TrySearchNearWord(CharPositions, charIndex, out wordRange, isRightSide)) return wordRange;
            return null;
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice, na které bude vykreslován kurzor na své aktuální pozici (indexu).
        /// Pokud neexistují znaky, pak vrátí null.
        /// <para/>
        /// Souřadnice jsou vráceny v absolutních koordinátech = v rámci daného prostoru <see cref="ITextEditInternal.TextBounds"/>, případně s posunutím <see cref="ITextEditInternal.TextShift"/>.
        /// Pokud jsou výsledné souřadnice mimo prostor <see cref="ITextEditInternal.TextBounds"/>, pak výstupem je null = kurzor se nekreslí.
        /// </summary>
        /// <returns></returns>
        public Rectangle? GetCursorBounds()
        {
            return GetCursorBounds(CursorIndex);
        }
        /// <summary>
        /// Metoda vrátí souřadnice, na které bude vykreslován kurzor, který je na dané pozici (indexu).
        /// Pokud neexistují znaky, pak vrátí null.
        /// Pokud je zadána pozice kurzoru mimo rozsah znaků, vrátí pozici na nejbližším okraji krajního znaku.
        /// <para/>
        /// Souřadnice jsou vráceny v absolutních koordinátech = v rámci daného prostoru <see cref="ITextEditInternal.TextBounds"/>, případně s posunutím <see cref="ITextEditInternal.TextShift"/>.
        /// Pokud jsou výsledné souřadnice mimo prostor <see cref="ITextEditInternal.TextBounds"/>, pak výstupem je null = kurzor se nekreslí.
        /// </summary>
        /// <param name="cursorIndex"></param>
        /// <returns></returns>
        public Rectangle? GetCursorBounds(int cursorIndex)
        {
            Rectangle? cursorBounds = GetRelativeCursorBounds(cursorIndex);
            if (!cursorBounds.HasValue) return null;
            cursorBounds = GetAbsoluteBounds(cursorBounds);

            Rectangle textBounds = IOwner.TextBounds;
            if (!textBounds.IntersectsWith(cursorBounds.Value)) return null;
            return cursorBounds;
        }
        /// <summary>
        /// Metoda nastaví <see cref="ITextEditInternal.TextShift"/> tak, aby byl zobrazen kurzor.
        /// </summary>
        public void SetTextShiftForCursorIndex()
        {
            SetTextShiftForCursorIndex(CursorIndex);
        }
        /// <summary>
        /// Metoda nastaví <see cref="ITextEditInternal.TextShift"/> tak, aby byl zobrazen kurzor.
        /// </summary>
        public void SetTextShiftForCursorIndex(int cursorIndex)
        {
            Rectangle? cursorBounds = GetRelativeCursorBounds(cursorIndex);
            if (!cursorBounds.HasValue) return;

            Rectangle textBounds = IOwner.TextBounds;
            Point textShift = IOwner.TextShift;




        }
        /// <summary>
        /// Metoda vrátí relativní souřadnice kurzoru v koordinátech textu, bez posunu <see cref="ITextEditInternal.TextBounds"/> a <see cref="ITextEditInternal.TextShift"/>.
        /// Může vrátit null, pokud neexistují žádné znaky.
        /// </summary>
        /// <param name="cursorIndex"></param>
        /// <returns></returns>
        protected Rectangle? GetRelativeCursorBounds(int cursorIndex)
        {
            int charLength = (this.CharPositions != null ? this.CharPositions.Length : 0);
            if (charLength <= 0) return null;

            // Najdeme znak, před kterým se nachází kurzor:
            CharPositionInfo charInfo;
            Rectangle cursorBounds;
            if (cursorIndex < charLength)
            {
                charInfo = this.CharPositions[(cursorIndex >= 0 ? cursorIndex : 0)];
                cursorBounds = new Rectangle(charInfo.TextBounds.X, charInfo.TextBounds.Y, 1, charInfo.TextBounds.Height);
            }
            else
            {   // Kurzor se nachází za posledním znakem:
                charInfo = this.CharPositions[charLength - 1];
                cursorBounds = new Rectangle(charInfo.TextBounds.Right, charInfo.TextBounds.Y, 1, charInfo.TextBounds.Height);
            }

            return cursorBounds;
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
            int charLength = (this.CharPositions != null ? this.CharPositions.Length : 0);
            if (charLength <= 0) return null;

            int begin = (selectionRange.Begin < selectionRange.End ? selectionRange.Begin : selectionRange.End);        // Index znaku, kde výběr začíná
            begin = (begin < 0 ? 0 : (begin >= charLength ? charLength - 1 : begin));                                   // Počátek je v rozmezí 0 (včetně) až charLength-1 (včetně)
            int end = (selectionRange.End > selectionRange.Begin ? selectionRange.End : selectionRange.Begin);          // End = index prvního znaku, kde už Selection není. 
            end = (end < 0 ? 0 : (end > charLength ? charLength : end)) - 1;                                            // end = index posledního znaku zahrnutého v Area (end = End - 1)
            if (end < begin) return null;                                                                               // end může být == begin, pak je v Selection ten jediný znak

            var charBegin = CharPositions[begin];
            var charEnd = CharPositions[end];

            if (charBegin.Line.RowIndex == charEnd.Line.RowIndex)
            {   // Zkratka pro jednořádkové texty:
                RectangleF bounds = RectangleF.FromLTRB(charBegin.TextBounds.Left, charBegin.Line.TextBounds.Top, charEnd.TextBounds.Right, charBegin.Line.TextBounds.Bottom);
                D2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddRectangle(bounds);
                return path;
            }

            #warning TODO GetSelectionArea pro Multirows není naprogramován !!!

            return null;

        }
        #endregion
        #region Převody souřadnic Absolutní / Relativní
        /// <summary>
        /// Metoda vrátí relativní souřadnici daného absolutního bodu (relativní = souřadnice znaků textu)
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        protected Point? GetRelativePoint(Point? absolutePoint)
        {
            if (!absolutePoint.HasValue) return null;
            Point textPoint = IOwner.TextBounds.Location;
            Point textShift = IOwner.TextShift;
            return absolutePoint.Value.Sub(textPoint).Add(textShift);
        }
        /// <summary>
        /// Metoda vrátí relativní souřadnici daného absolutního prostoru (relativní = souřadnice znaků textu)
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <returns></returns>
        protected Rectangle? GetRelativeBounds(Rectangle? absoluteBounds)
        {
            if (!absoluteBounds.HasValue) return null;
            Point? relativeLocation = GetRelativePoint(absoluteBounds.Value.Location);
            return new Rectangle(relativeLocation.Value, absoluteBounds.Value.Size);
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnici daného relativního bodu (relativní = souřadnice znaků textu)
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <returns></returns>
        protected Point? GetAbsolutePoint(Point? relativePoint)
        {
            if (!relativePoint.HasValue) return null;
            Point textPoint = IOwner.TextBounds.Location;
            Point textShift = IOwner.TextShift;
            return relativePoint.Value.Add(textPoint).Sub(textShift);
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnici daného relativního prostoru (relativní = souřadnice znaků textu)
        /// </summary>
        /// <param name="relativeBounds"></param>
        /// <returns></returns>
        protected Rectangle? GetAbsoluteBounds(Rectangle? relativeBounds)
        {
            if (!relativeBounds.HasValue) return null;
            Point? absoluteLocation = GetAbsolutePoint(relativeBounds.Value.Location);
            return new Rectangle(absoluteLocation.Value, relativeBounds.Value.Size);
        }
        #endregion
        #region Navázání editoru na interaktivní události TextBoxu
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
                int length = Text.Length;
                CursorIndex = length;
                SelectionRange = new Int32Range(0, length);
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
            if (!Settings.TextBoxSaveCursorPositionOnLostFocus)
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
                ProcessKeyFunction(args) ||
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
                ProcessKeyText(args))
                args.Handled = true;

            IsKeyHandled |= args.Handled;
        }
        /// <summary>
        /// Metoda je volána po myším kliknutí, připraví data pro určení pozice kurzorum, SelectedRange a následné vykreslení.
        /// Po myším kliknutí následuje nové vykreslení TextBoxu, tedy jeho metoda <see cref="GTextEdit.DrawText(GTextEditDrawArgs)"/>, 
        /// z které se provede nová analýza pozic znaků v <see cref="ITextEditInternal.CharPositions"/>.
        /// Následně se pak volá zdejší metoda <see cref="MouseDownDataProcess()"/>, která na základě dat MouseDown určí pozici kurzoru a SelectedRange.
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
            MouseDownPoint = e.MouseAbsolutePoint;
            MouseDownModifierKeys = e.ModifierKeys;
            MouseDownNeedProcess = true;
        }
        /// <summary>
        /// Metodu volá TextBox při pohybu myši, optimálně tehdy když je stisknuta levá myš.
        /// Editor dokáže provést označení části textu pomocí "přetažení myší".
        /// </summary>
        /// <param name="e"></param>
        public void EventMouseLeftOver(GInteractiveChangeStateArgs e)
        {
            var mouseDownPoint = MouseDownPoint;
            var mouseDownButtons = MouseDownButtons;
            if (!(mouseDownPoint.HasValue && mouseDownButtons.HasValue && e.MouseAbsolutePoint.HasValue)) return;      // Nemáme dostatečná data
            if (mouseDownButtons.Value != MouseButtons.Left) return;                                                   // Není stisknuta levá myš
            var distance = e.MouseAbsolutePoint.Value.Sub(mouseDownPoint.Value);
            if (distance.X >= -3 && distance.X <= 3 && distance.Y >= -3 && distance.Y <= 3) return;                    // Je evidován pohyb menší než malý

            // Máme rozpoznáno: Levé tlačítko myši + Nenulový pohyb myši => najdeme cílovou pozici kurzoru
            MouseDragDataProcess(e.MouseAbsolutePoint.Value);
        }
        /// <summary>
        /// Metodu volá TextBox při zvednutí levé myši, ukončí tak režim označování textu pomocí "přetažení myší".
        /// </summary>
        /// <param name="e"></param>
        public void EventMouseLeftUp(GInteractiveChangeStateArgs e)
        {
            MouseDownDataReset();
        }
        /// <summary>
        /// Konstanta vkládaná do <see cref="GInteractiveChangeStateArgs.UserData"/> v události FocusEnter, detekovaná v události MouseDown.
        /// Výsledkem je odlišení situace, kdy MouseDown je provedeno jako prvotní vstup do TextBoxu anebo v již editovaném TextBoxu.
        /// </summary>
        protected const string ConstantFocusEnter = "FocusEnter";
        #endregion
        #region Klávesnice (detekce typu => kurzor, delete, text)
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
        /// Detekuje a zpracuje kurzorové klávesy, včetně modifikátorů Shift, Control. 
        /// Vrátí true pokud klávesu zpracoval, false pokud to nebyla zdejší klávesa.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual bool ProcessKeyCursor(KeyEventArgs args)
        {
            if (this.CharPositions == null) return false;
            int textLength = this.CharPositions.Length;
            int cursorIndex = CursorIndex;
            cursorIndex = (cursorIndex < 0 ? 0 : (cursorIndex > textLength ? textLength : cursorIndex));     // Maximální hodnota cursorIndex je textLength (=> za posledním znakem)
            bool cursorOnEnd = (cursorIndex >= textLength);                                                  // true = kurzor je na konci textu = za posledním znakem
            CharPositionInfo charInfo = (textLength > 0 ? this.CharPositions[!cursorOnEnd ? cursorIndex : (cursorIndex - 1)] : null);

            Int32Range selectionRange = SelectionRange;
            int selectionBegin = (selectionRange != null ? selectionRange.Begin : cursorIndex);

            switch (args.KeyData)                               // KeyData obsahuje kompletní klávesu = včetně modifikátoru (Ctrl, Shift, Alt)
            {
                // HOME:
                case Keys.Home:                                 // Na začátek aktuálního řádku
                    cursorIndex = (charInfo != null ? charInfo.Line.FirstChar.Index : 0);
                    selectionRange = null;
                    break;
                case Keys.Shift | Keys.Home:                    // Na začátek aktuálního řádku, označit od dosavadní pozice
                    cursorIndex = (charInfo != null ? charInfo.Line.FirstChar.Index : 0);
                    selectionRange = new Int32Range(selectionBegin, cursorIndex);
                    break;
                case Keys.Control | Keys.Home:                  // Na úplný začátek textu
                    cursorIndex = 0;
                    selectionRange = null;
                    break;
                case Keys.Shift | Keys.Control | Keys.Home:     // Na úplný začátek textu, označit od dosavadní pozice
                    cursorIndex = 0;
                    selectionRange = new Int32Range(selectionBegin, cursorIndex);
                    break;

                // END:
                case Keys.End:                                  // Na konec aktuálního řádku
                    cursorIndex = (charInfo != null && charInfo.Line.HasNextLine ? charInfo.Line.LastChar.Index : textLength);
                    selectionRange = null;
                    break;
                case Keys.Shift | Keys.End:                     // Na konec aktuálního řádku, označit od dosavadní pozice
                    cursorIndex = (charInfo != null && charInfo.Line.HasNextLine ? charInfo.Line.LastChar.Index : textLength);
                    selectionRange = new Int32Range(selectionBegin, cursorIndex);
                    break;
                case Keys.Control | Keys.End:                   // Na úplný konec textu
                    cursorIndex = textLength;
                    selectionRange = null;
                    break;
                case Keys.Shift | Keys.Control | Keys.End:      // Na úplný konec textu
                    cursorIndex = textLength;
                    selectionRange = new Int32Range(selectionBegin, cursorIndex);
                    break;

                // LEFT:
                case Keys.Left:                                 // O znak doleva; zrušit Selection = i když nebude žádný pohyb!
                    if (cursorIndex > 0)
                        cursorIndex--;
                    selectionRange = null;
                    break;
                case Keys.Shift | Keys.Left:                    // O znak doleva, rozšířit Selection
                    if (cursorIndex > 0)
                        cursorIndex--;
                    selectionRange = new Int32Range(selectionBegin, cursorIndex);
                    break;
                case Keys.Control | Keys.Left:                  // Doleva na začátek aktuálního slova; pokud jsem na začátku slova - tak k dalšímu doleva; vždy zrušit Selection
                    TrySearchWordBegin(this.CharPositions, Direction.Negative, ref cursorIndex, false);
                    selectionRange = null;
                    break;
                case Keys.Shift | Keys.Control | Keys.Left:     // Doleva na začátek aktuálního slova; pokud jsem na začátku slova - tak k dalšímu doleva; rozšířit Selection
                    TrySearchWordBegin(this.CharPositions, Direction.Negative, ref cursorIndex, false);
                    selectionRange = new Int32Range(selectionBegin, cursorIndex);
                    break;

                // RIGHT:
                case Keys.Right:                                // O znak doprava; zrušit Selection = i když nebude žádný pohyb!
                    if (cursorIndex < textLength)
                        cursorIndex++;
                    selectionRange = null;
                    break;
                case Keys.Shift | Keys.Right:                   // O znak doprava, rozšířit Selection
                    if (cursorIndex < textLength)
                        cursorIndex++;
                    selectionRange = new Int32Range(selectionBegin, cursorIndex);
                    break;
                case Keys.Control | Keys.Right:                 // Doprava na začátek (Begin!) dalšího slova; pokud jsem na začátku slova - tak k dalšímu doprava; vždy zrušit Selection
                    TrySearchWordBegin(this.CharPositions, Direction.Positive, ref cursorIndex, false);
                    selectionRange = null;
                    break;
                case Keys.Shift | Keys.Control | Keys.Right:    // Doprava na začátek (Begin!) dalšího slova; pokud jsem na začátku slova - tak k dalšímu doprava; rozšířit Selection
                    TrySearchWordBegin(this.CharPositions, Direction.Positive, ref cursorIndex, false);
                    selectionRange = new Int32Range(selectionBegin, cursorIndex);
                    break;

                // Řádky...
                case Keys.Up:
                case Keys.Down:
                    return true;

                // SELECT ALL:
                case Keys.Control | Keys.A:
                    cursorIndex = textLength;
                    selectionRange = new Int32Range(0, textLength);
                    break;

                default:
                    return false;
            }
            CursorIndex = cursorIndex;
            SelectionRange = selectionRange;
            SetTextShiftForCursorIndex(cursorIndex);
            this.Invalidate();
            return true;
        }
        /// <summary>
        /// Zpracuje klávesu Delete nebo BackSpace, včetně modifikátorů Shift, Control. 
        /// Vrátí true pokud klávesu zpracoval, false pokud to nebyla zdejší klávesa.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual bool ProcessKeyBackspaceDel(KeyEventArgs args)
        {
            if (!IsEditable) return false;

            int cursorIndex = CursorIndex;
            Int32Range selectionRange = SelectionRange;
            if (selectionRange != null && selectionRange.Size == 0) selectionRange = null;
            int selectionStart = (selectionRange != null ? (selectionRange.Begin < selectionRange.End ? selectionRange.Begin : selectionRange.End) : 0);
            Int32Range deletionRange = null;
            int textLength = (this.CharPositions != null ? this.CharPositions.Length : 0);
            string undoRedoName = null;
            switch (args.KeyData)
            {
                case Keys.Back:                                  // Jeden Backspace = Smaže označený blok nebo jeden znak vlevo
                case Keys.Shift | Keys.Back:                     // Shift + BackSpace = stejné jako Backspace
                    if (selectionRange != null)
                    {
                        cursorIndex = selectionStart;
                        deletionRange = selectionRange;
                    }
                    else if (cursorIndex > 0)
                    {
                        cursorIndex--;
                        deletionRange = new Int32Range(cursorIndex, cursorIndex + 1);
                    }
                    undoRedoName = "Backspace";
                    break;
                case Keys.Control | Keys.Back:                   // Control + Backspace = Smaže doleva slovo od kurzoru k začátku slova, včetně výběru

                    // b 123 456 78 789 

                    break;
                case Keys.Delete:                                // Delete = Smaže označený blok nebo znak vpravo od kurzoru
                    if (selectionRange != null)
                    {
                        cursorIndex = selectionStart;
                        deletionRange = selectionRange;
                    }
                    else if (cursorIndex < textLength)
                    {
                        deletionRange = new Int32Range(cursorIndex, cursorIndex + 1);
                    }
                    undoRedoName = "Delete";
                    break;
                case Keys.Shift | Keys.Delete:                   // Shift + Delete = Smaže označený blok nebo celý řádek kde je kurzor
                    undoRedoName = "ShiftDelete";
                    break;
                case Keys.Control | Keys.Delete:                 // Control + Delete = Smaže označený blok nebo slovo od kurzoru doprava
                    undoRedoName = "ControlDelete";
                    break;
                default:
                    return false;
            }
            UndoRedoActionAdd(undoRedoName);
            DeleteInsertText(deletionRange);
            CursorIndex = cursorIndex;
            SelectionRange = null;
            this.Invalidate();
            return true;
        }
        /// <summary>
        /// Zpracuje funkční klávesy Ctrl+F, Ctrl+H, ...
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual bool ProcessKeyFunction(KeyEventArgs args)
        {
            switch (args.KeyData)                               // KeyData obsahuje kompletní klávesu = včetně modifikátoru (Ctrl, Shift, Alt)
            {
                // Find:
                case Keys.Control | Keys.F:
                    break;

                default:
                    return false;
            }

            return true;
        }
        /// <summary>
        /// Detekuje a zpracuje speciální klávesy pomocí TextBoxu.
        /// Vrátí true pokud klávesu zpracoval, false pokud to nebyla zdejší klávesa.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual bool ProcessKeySpecial(KeyEventArgs args)
        {
            Owner.SpecialKeyAction(args);
            return args.Handled;
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
                UndoRedoActionAddOneChar(c);
                string value = c.ToString();
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
        /// <summary>
        /// Vrátí aktuálně označený text, nebo text nacházející se v daném rozsahu.
        /// </summary>
        /// <param name="selectionRange"></param>
        /// <returns></returns>
        protected string GetSelectedText(Int32Range selectionRange = null)
        {
            if (selectionRange == null) selectionRange = this.SelectionRange;
            if (selectionRange == null) return "";

            // Parametr selectionRange může mít Begin vyšší než End = přípustný stav, kdy selectionRange odpovídá výběru v Textboxu: Begin je začátek výběru a může být i vpravo, End je konec výběru = vlevo:
            int begin = (selectionRange.Begin < selectionRange.End ? selectionRange.Begin : selectionRange.End);
            int end = (selectionRange.Begin > selectionRange.End ? selectionRange.Begin : selectionRange.End);

            string text = this.Text;
            int length = text.Length;
            if (begin < 0) begin = 0;
            if (end > length) end = length;
            if (end <= begin) return "";
            return text.Substring(begin, end - begin);
        }
        /// <summary>
        /// Odstraní část aktuálního textu danou výběrem, a místo toho vloží nový text (pokud není null).
        /// Pokud by bylo zadáno <paramref name="deletionRange"/> = null, nedělá nic - protože neví, kam text vložit.
        /// <para/>
        /// Metoda vrací novou pozici kurzoru po vložení textu = na konci vloženého textu.
        /// </summary>
        /// <param name="deletionRange"></param>
        /// <param name="insertText"></param>
        /// <returns>Pozice kurzoru za koncem vloženého textu, nebo za koncem odstraněného textu.</returns>
        protected int DeleteInsertText(Int32Range deletionRange, string insertText = null)
        {
            if (deletionRange == null) return 0;
            // Parametr deletionRange může mít Begin vyšší než End = přípustný stav, kdy deletionRange odpovídá výběru v Textboxu: Begin je začátek výběru a může být i vpravo, End je konec výběru = vlevo:
            int begin = (deletionRange.Begin < deletionRange.End ? deletionRange.Begin : deletionRange.End);
            int end = (deletionRange.Begin > deletionRange.End ? deletionRange.Begin : deletionRange.End);

            // A pokračujeme standardní cestou:
            return DeleteInsertText(begin, end - begin, insertText);
        }
        /// <summary>
        /// Odstraní část aktuálního textu danou pozicí počátku a délkou odstraněného textu, a místo toho vloží nový text (pokud není null).
        /// <para/>
        /// Metoda vrací novou pozici kurzoru po vložení textu = na konci vloženého textu.
        /// </summary>
        /// <param name="begin">Pozice počátku odstranění</param>
        /// <param name="removeLength">Délka odstraněného textu. Pokud bude 0 (nebo záporné), neodstraní se nic.</param>
        /// <param name="insertText">Vložený text. Vloží se pouze pokud není null a jeho délka je kladná.</param>
        /// <returns>Pozice kurzoru za koncem vloženého textu, nebo za koncem odstraněného textu.</returns>
        protected int DeleteInsertText(int begin, int removeLength, string insertText = null)
        {
            int end = begin + (removeLength < 0 ? 0 : removeLength);
            string text = this.Text;
            int length = text.Length;
            string textL = (begin > 0 ? text.Substring(0, (begin < length ? begin : length)) : "");
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
        #region Clipboard
        /// <summary>
        /// Detekuje a zpracuje klávesy Ctrl+C, Ctrl+X, Ctrl+V = Clipboard.
        /// Pokud prvek není editovatelný: pokud detekuje Ctrl+X, provede jako Ctrl+C. Pokud detekuje Ctrl+V, neprovede ji.
        /// Vrátí true pokud klávesu zpracoval (i když by ji neprovedl = needitovatelý prvek a Ctrl+V), false pokud to nebyla zdejší clipboardová klávesa.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual bool ProcessKeyClipboard(KeyEventArgs args)
        {
            switch (args.KeyData)
            {
                case Keys.Control | Keys.C:
                    ClipboardCopy();
                    break;
                case Keys.Control | Keys.X:
                    if (IsEditable)
                        ClipboardCut();
                    else
                        ClipboardCopy();
                    break;
                case Keys.Control | Keys.V:
                    if (IsEditable)
                        ClipboardPaste();
                    break;
                default:
                    return false;
            }
            this.Invalidate();
            return true;
        }
        /// <summary>
        /// Provede Clipboard: Copy
        /// </summary>
        protected void ClipboardCopy()
        {
            if (IsPasswordProtect) return;                 // Pokud reprezentuji heslo, pak jej nelze kopírovat
            string text = SelectedText;
            if (text.Length > 0) WinClipboard.SetText(text);
        }
        /// <summary>
        /// Provede Clipboard: Cut
        /// </summary>
        protected void ClipboardCut()
        {
            if (IsPasswordProtect) return;                 // Pokud reprezentuji heslo, pak jej nelze kopírovat
            string text = SelectedText;
            if (text.Length > 0)
            {
                WinClipboard.SetText(text);
                SelectedText = "";
            }
        }
        /// <summary>
        /// Provede Clipboard: Paste
        /// </summary>
        protected void ClipboardPaste()
        {
            string text = null;
            if (WinClipboard.ContainsText()) text = WinClipboard.GetText();
            else if (WinClipboard.ContainsFileDropList()) text = WinClipboard.GetFileDropList()[0];
            if (text != null) this.SelectedText = text;
        }
        #endregion
        #region Undo + Redo
        /// <summary>
        /// Detekuje a zpracuje klávesy Undo / Redo.
        /// Vrátí true pokud klávesu zpracoval, false pokud to nebyla zdejší klávesa.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual bool ProcessKeyUndoRedo(KeyEventArgs args)
        {
            if (!IsEditable) return false;
            switch (args.KeyData)
            {
                case Keys.Control | Keys.Z:
                    UndoRedoActionUndo();
                    break;
                case Keys.Control | Keys.Y:
                    UndoRedoActionRedo();
                    break;
                default:
                    return false;
            }
            this.Owner.Invalidate();
            return true;
        }
        /// <summary>
        /// Inicializace UndoRedo
        /// </summary>
        protected void UndoRedoInit()
        {
            UndoRedoPosition = 0;
            UndoRedoSteps = new List<UndoRedoStep>();
            UndoRedoLastTime = DateTime.Now;
        }
        /// <summary>
        /// Přidá další akci do UndoRedo zásobníku, pokud daná klávesa je významná, anebo pokud od předchozího přidání uběhla doba delší než <see cref="UndoRedoAddEverySeconds"/> sekund
        /// </summary>
        /// <param name="c"></param>
        protected void UndoRedoActionAddOneChar(char c)
        {
            int count = UndoRedoStepCount;
            bool addAction = (count == 0 || SelectionRangeExists || UndoRedoIsTimeForNewStep || c == ' ' || Char.IsPunctuation(c));
            if (addAction)
                UndoRedoActionAdd("Editace");
        }
        /// <summary>
        /// Přidá další akci do UndoRedo zásobníku, pokud dosud není nic uloženo, anebo pokud se liší text v textboxu od textu na aktuální pozici zásobníku UndoRedo.
        /// Volá se typicky před změnou hodnoty textu (vložení z Clipboardu, vložené do <see cref="SelectedText"/>, atd.)
        /// </summary>
        protected void UndoRedoAddCurrentBeforeChange()
        {
            int count = UndoRedoStepCount;
            int position = UndoRedoPosition;
            int last = (position > 0 ? (position <= count ? position - 1 : count - 1) : -1);       // last: ukazuje na poslední zachycený platný stav textu;   last = (position-1); pokud by last bylo větší než count, pak last = (count-1). Pokud by count nebo position bylo 0, pak last = -1.
            string text = Text;
            bool addAction = (count == 0 || position == 0 || (last >= 0 && text != UndoRedoSteps[last].Text));         // Přidat aktuální stav: pokud žádný není, nebo jsme dali Undo na začátek, nebo poslední platný stav má jiný text
            if (addAction)
                UndoRedoActionAdd("Stav před změnou");
        }
        /// <summary>
        /// Přidá další akci do UndoRedo zásobníku, pokud dosud není nic uloženo, anebo pokud aktuální pozice je "na konci zásobníku" a pokud se liší text v textboxu od textu na poslední pozici zásobníku UndoRedo.
        /// Volá se typicky před provedením Undo, aby v zásobníku byl na poslední pozici aktuální stav před Undo (což nemusí být, protože do zásobníku neukládáme editaci po dokončení, ale před provedením větší akce)
        /// </summary>
        protected void UndoRedoAddCurrentBeforeUndo()
        {
            int count = UndoRedoStepCount;
            int position = UndoRedoPosition;
            string text = Text;
            bool addAction = (count == 0 || (count > 0 && position >= count && text != UndoRedoSteps[count - 1].Text));// Pouze když aktuální pozice je za koncem listu, a poslední pozice obsahuje jiný text než aktuální
            if (addAction)
                UndoRedoActionAdd("Stav před Undo");
        }
        /// <summary>
        /// Přidá další akci do UnoRedo zásobníku
        /// </summary>
        /// <param name="name"></param>
        protected void UndoRedoActionAdd(string name = null)
        {
            // Pokud jsme nedávno provedli nějaké kroky Undo (pak pozice UndoRedoPosition je menší než počet záznamů v UndoRedoSteps),
            //  pak těch několik pozic Undo na konci seznamu odstraníme = již nepůjde udělat Redo na tyto hodnoty = jde o "slepou větev vývoje editace":
            if (UndoRedoPosition < UndoRedoStepCount)
                UndoRedoSteps.RemoveRange(UndoRedoPosition, UndoRedoStepCount - UndoRedoPosition);

            // Pokud máme více než (MAX) kroků editace, pak několik nejstarších kroků odstraníme (zásobník není nekonečný):
            if (UndoRedoStepCount >= UndoRedoStepMaximum)
                UndoRedoSteps.RemoveRange(0, UndoRedoStepRemove);

            // Přidáme nový krok a aktualizujeme pointer:
            UndoRedoStep step = new UndoRedoStep(this.Text, name, this.CursorIndex, this.SelectionRange);
            UndoRedoSteps.Add(step);
            UndoRedoPosition = UndoRedoStepCount;
            UndoRedoLastTime = DateTime.Now;
        }
        /// <summary>
        /// Obsahuje true, pokud je možno provést Undo
        /// </summary>
        protected bool UndoRedoIsEnabledUndo { get { return (this.UndoRedoPosition > 0); } }
        /// <summary>
        /// Provede krok Undo. Vrátí true pokud byl proveden = je třeba provést Invalidate().
        /// </summary>
        /// <returns></returns>
        protected bool UndoRedoActionUndo()
        {
            if (!UndoRedoIsEnabledUndo) return false;
            int undoRedoPosition = UndoRedoPosition - 1;
            UndoRedoAddCurrentBeforeUndo();
            UndoRedoPosition = undoRedoPosition;
            var step = UndoRedoSteps[undoRedoPosition];
            return UndoRedoActionApplyStep(step);
        }
        /// <summary>
        /// Obsahuje true, pokud je možno provést Redo
        /// </summary>
        protected bool UndoRedoIsEnabledRedo { get { return (this.UndoRedoPosition < (UndoRedoStepCount - 1)); } }
        /// <summary>
        /// Provede krok Redo. Vrátí true pokud byl proveden = je třeba provést Invalidate().
        /// </summary>
        /// <returns></returns>
        protected bool UndoRedoActionRedo()
        {
            if (!UndoRedoIsEnabledRedo) return false;
            var step = UndoRedoSteps[UndoRedoPosition];
            UndoRedoPosition = UndoRedoPosition + 1;
            return UndoRedoActionApplyStep(step);
        }
        /// <summary>
        /// Promítne stav editace z uloženého kroku do this textboxu
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        protected bool UndoRedoActionApplyStep(UndoRedoStep step)
        {
            if (step == null) return false;
            if (step.IsEqualTo(this.Text, this.CursorIndex, this.SelectionRange)) return false;

            this.Text = step.Text;
            this.CursorIndex = step.CursorIndex;
            this.SelectionRange = step.SelectionRange?.Clone;
            return true;
        }
        /// <summary>
        /// Pozice UndoRedo, na kterou se bude ukládat následující krok v metodě <see cref="UndoRedoActionAdd(string)"/>, nebo na které leží pozice pro Redo.
        /// Výchozí hodnota = 0.
        /// <para/>
        /// Postupy UndoRedo:
        /// 1. Incializace: <see cref="UndoRedoSteps"/> je prázdný, <see cref="UndoRedoPosition"/> obsahuje 0;
        /// 2. Při stisku Delete nebo Backspace nebo Ctrl+V nebo CtrlX (a další): před provedením změny hodnoty se zavolá <see cref="UndoRedoActionAdd(string)"/>, kde se zachytí aktuální stav
        /// 2a. Provede se kontrola <see cref="UndoRedoPosition"/> proti počtu záznamů v listu <see cref="UndoRedoSteps"/>, případně se nadpočetné záznamy odstraní;
        /// 2b. Vezme se hodnota Text, pozice kurzoru a oblast výběru a vytvoří se z nich instance <see cref="UndoRedoStep"/>;
        /// 2c. Do Listu <see cref="UndoRedoSteps"/> se přidá instance <see cref="UndoRedoStep"/> a do <see cref="UndoRedoPosition"/> se vloží počet záznamů pole <see cref="UndoRedoSteps"/>
        /// 3. Při akci Undo se sníží o 1 hodnota <see cref="UndoRedoPosition"/>, vyzvedne se záznam z tohoto indexu z pole <see cref="UndoRedoSteps"/> a aplikuje se do textboxu;
        /// 3a. Po akci Undo bude tedy <see cref="UndoRedoPosition"/> ukazovat do Listu <see cref="UndoRedoSteps"/> na záznam
        /// 4. Při akci Redo se zvýší o 1 hodnota <see cref="UndoRedoPosition"/>, vyzvedne se záznam z tohoto indexu z pole <see cref="UndoRedoSteps"/> a aplikuje se do textboxu;
        /// </summary>
        protected int UndoRedoPosition { get; private set; }
        /// <summary>
        /// Zaznamenané kroky editace. 
        /// Po dosažení počtu 100 kroků bude 10 nejstarších odebráno.
        /// </summary>
        protected List<UndoRedoStep> UndoRedoSteps { get; private set; }
        /// <summary>
        /// Počet zaznamenaných kroků editace. 
        /// </summary>
        protected int UndoRedoStepCount { get { return UndoRedoSteps.Count; } }
        /// <summary>
        /// Čas, kdy byla do UndoRedo zásobníku naposledy přidán záznam
        /// </summary>
        protected DateTime UndoRedoLastTime { get; private set; }
        /// <summary>
        /// Obsahuje true, když je nejvyšší čas na přidání dalšího kroku do zásobníku UndoRedo
        /// </summary>
        protected bool UndoRedoIsTimeForNewStep
        {
            get
            {
                TimeSpan time = DateTime.Now - UndoRedoLastTime;
                return (time.TotalSeconds >= (double)UndoRedoAddEverySeconds);
            }
        }
        /// <summary>
        /// Nejvyšší počet kroků UndoRedo, po jeho překročení některé nejstarší odstraníme
        /// </summary>
        protected const int UndoRedoStepMaximum = 256;
        /// <summary>
        /// Počet kroků k odstranění při překročení <see cref="UndoRedoStepMaximum"/>
        /// </summary>
        protected const int UndoRedoStepRemove = 32;
        /// <summary>
        /// Počet sekund, po kterých se do UndoRedo zásobníku přidá další záznam po stisku klávesy
        /// </summary>
        protected const int UndoRedoAddEverySeconds = 15;
        /// <summary>
        /// Jeden krok editace
        /// </summary>
        protected class UndoRedoStep
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="text"></param>
            /// <param name="name"></param>
            /// <param name="cursorIndex"></param>
            /// <param name="selectionRange"></param>
            public UndoRedoStep(string text, string name, int cursorIndex, Int32Range selectionRange)
            {
                this.Text = text;
                this.Name = name;
                this.CursorIndex = cursorIndex;
                this.SelectionRange = (selectionRange?.Clone ?? null);
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Name: {Name}; Text: {Text}";
            }
            /// <summary>
            /// Vrátí true, pokud this instance obsahuje shodné hodnoty jako jsou předané
            /// </summary>
            /// <param name="text"></param>
            /// <param name="cursorIndex"></param>
            /// <param name="selectionRange"></param>
            /// <returns></returns>
            public bool IsEqualTo(string text, int cursorIndex, Int32Range selectionRange)
            {
                if (!String.Equals(this.Text, text, StringComparison.InvariantCulture)) return false;
                if (this.CursorIndex != cursorIndex) return false;
                string s0 = this.SelectionRange?.ToString() ?? "";
                string s1 = selectionRange?.ToString() ?? "";
                if (s0 != s1) return false;
                return true;
            }
            /// <summary>
            /// Text v editoru
            /// </summary>
            public string Text { get; private set; }
            /// <summary>
            /// Jméno editačního kroku
            /// </summary>
            public string Name { get; private set; }
            /// <summary>
            /// Pozice kurzoru
            /// </summary>
            public int CursorIndex { get; private set; }
            /// <summary>
            /// Oblast výběru
            /// </summary>
            public Int32Range SelectionRange { get; private set; }
        }
        #endregion
        #region Vyhledání slova (začátek, konec, celé slovo)
        /// <summary>
        /// Zkusí najít začátek dalšího slova
        /// </summary>
        /// <param name="chars">Pole znaků. Nesmí být null, to by měla testovat volající metoda (z důvodu rychlosti při opakovaných privátních voláních)</param>
        /// <param name="direction">Směr hledání:
        /// <see cref="Direction.Positive"/> = doprava v textu (na vyšší index);
        /// <see cref="Direction.Negative"/> = doleva v textu (na nižší index);
        /// <see cref="Direction.None"/> = nikam = pouze otestovat danou pozici, a vrátit výsledek</param>
        /// <param name="index">Vstupní / Výstupní index</param>
        /// <param name="acceptCurrentIndex">Akceptovat takový okraj, který se nachází právě na vstupní zadané pozici indexu? 
        /// Default = true = ano: pokud hledáme začátek slova, a na daném indexu je začátek slova, pak se vrátí true a dodaný index se nezmění;
        /// false = ne: pokud na výchozí dané pozici je začátek slova, nebude akceptován a budeme hledat následující</param>
        /// <returns></returns>
        internal static bool TrySearchWordBegin(CharPositionInfo[] chars, Direction direction, ref int index, bool acceptCurrentIndex = true)
        {
            if (chars == null) { index = -1; return false; }               // Není vstup - není slovo
            return _TrySearchWordEdge(chars, direction, true, ref index, acceptCurrentIndex);
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
        /// <param name="chars">Pole znaků. Nesmí být null, to by měla testovat volající metoda (z důvodu rychlosti při opakovaných privátních voláních)</param>
        /// <param name="direction">Směr hledání:
        /// <see cref="Direction.Positive"/> = doprava v textu (na vyšší index);
        /// <see cref="Direction.Negative"/> = doleva v textu (na nižší index);
        /// <see cref="Direction.None"/> = nikam = pouze otestovat danou pozici, a vrátit výsledek</param>
        /// <param name="index">Vstupní / Výstupní index</param>
        /// <param name="acceptCurrentIndex">Akceptovat takový okraj, který se nachází právě na vstupní zadané pozici indexu? 
        /// Default = true = ano: pokud hledáme začátek slova, a na daném indexu je začátek slova, pak se vrátí true a dodaný index se nezmění;
        /// false = ne: pokud na výchozí dané pozici je začátek slova, nebude akceptován a budeme hledat následující</param>
        /// <returns></returns>
        internal static bool TrySearchWordEnd(CharPositionInfo[] chars, Direction direction, ref int index, bool acceptCurrentIndex = true)
        {
            if (chars == null) { index = -1; return false; }               // Není vstup - není slovo
            return _TrySearchWordEdge(chars, direction, false, ref index, acceptCurrentIndex);
        }
        /// <summary>
        /// Metoda vrátí true, pokud daná pozice (index) ukazuje právě na začátek slova = první písmeno (nebo číslice) za mezerou (nebo jiným znakem)
        /// </summary>
        /// <param name="chars">Pole znaků</param>
        /// <param name="index">Pozice</param>
        /// <returns></returns>
        internal static bool IsBeginWord(CharPositionInfo[] chars, int index)
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
        internal static bool IsEndWord(CharPositionInfo[] chars, int index)
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
        internal static bool TrySearchNearWord(CharPositionInfo[] chars, int index, out Int32Range wordRange, bool? isRightSide = null)
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
        /// <param name="acceptCurrentIndex">Akceptovat takový okraj, který se nachází právě na vstupní zadané pozici indexu? 
        /// Default = true = ano: pokud hledáme začátek slova, a na daném indexu je začátek slova, pak se vrátí true a dodaný index se nezmění;
        /// false = ne: pokud na výchozí dané pozici je začátek slova, nebude akceptován a budeme hledat následující</param>
        /// <returns>true = nalezeno / false = nenalezeno</returns>
        private static bool _TrySearchWordEdge(CharPositionInfo[] chars, Direction direction, bool searchBeginText, ref int index, bool acceptCurrentIndex = true)
        {
            int length = chars.Length;
            if (length == 0) return false;

            int currIndex = index;
            if (currIndex > length) currIndex = length;
            if (currIndex < 0) currIndex = 0;
            char zero = (char)0;
            bool isFirstLoop = true;

            bool run = true;
            while (run)
            {   // Bude alespoň jeden cyklus na vyhodnocení zadané pozice, protože text má délku alespoň jeden znak!
                int prevIndex = currIndex - 1;
                char prevChar = ((prevIndex >= 0 && prevIndex < length) ? chars[prevIndex].Content : zero);        // Znak PŘED daným indexem (nebo 0)
                char currChar = ((currIndex >= 0 && currIndex < length) ? chars[currIndex].Content : zero);        // Znak ZA daným indexem (nebo 0)

                // Pokud předchozí znak a aktuální znak dává požadovanou sekvenci (Mezera => Znak; anebo Znak => Mezera), pak máme nalezenou hranici slova a mezery:
                // Řešíme jen tehdy, když akceptujeme hledaný výskyt i na vstupní zadané pozici, anebo když už nejsme na první pozici!
                if ((acceptCurrentIndex || !isFirstLoop) && _IsWordEdge(prevChar, currChar, searchBeginText))
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
                isFirstLoop = false;
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
                !prevIsLetter && nextIsLetter :
                 prevIsLetter && !nextIsLetter);
        }
        /// <summary>
        /// Vrátí true, pokud dodaný znak je písmeno uvnitř slova (písmeno nebo číslice) nebo akceptované znaky <see cref="Settings.CharactersAssumedAsWords"/>;
        /// vrátí false pokud jde o mezeru nebo nevýznamný oddělovač (operátory, 
        /// </summary>
        /// <param name="charText"></param>
        /// <returns></returns>
        private static bool _IsCharText(char charText)
        {
            if (Char.IsLetterOrDigit(charText)) return true;
            if (Settings.CharactersAssumedAsWords != null && Settings.CharactersAssumedAsWords.IndexOf(charText) >= 0) return true;
            return false;
        }
        #endregion
    }
    #endregion
    #region class TextProperties : BitStorage pro textové vlastnosti typu Boolean
    /// <summary>
    /// TextProperties : BitStorage pro textové vlastnosti typu Boolean
    /// Hodnoty bitů lze setovat i číst.
    /// Čtení i ukládání hodnoty lze převést na dynamické = lze zadat přiměřenou metodu, 
    /// která konkrétní hodnotu pro konkrétní prvek vrátí/uloží odjinud, než ze statického úložiště.
    /// </summary>
    public class TextProperties : BitStorage
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TextProperties() : base() { }
        /// <summary>
        /// Implicitní výchozí hodnota
        /// </summary>
        protected override uint DefaultValue { get { return (uint)0; } }
    }
    #endregion
}
