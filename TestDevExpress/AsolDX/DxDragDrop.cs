// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using System.Windows.Forms;
using System.Drawing;

using DevExpress.Utils;
using System.Drawing.Drawing2D;
using DevExpress.Pdf.Native;
using DevExpress.XtraPdfViewer;
using DevExpress.XtraEditors;
using DevExpress.Office.History;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region class DxDragDrop : controller pro řízení pro Drag and Drop uvnitř jednoho controlu (TreeNode, ListBox) i mezi různými controly
    /// <summary>
    /// <see cref="DxDragDrop"/> : controller pro řízení pro Drag and Drop uvnitř jednoho controlu (TreeNode, ListBox) i mezi různými controly.
    /// <para/>
    /// Způsob použití:
    /// 1. Uvnitř jednoho controlu (např. pro přemístění jednoho TreeNode na jinou pozici):
    /// - Control (zde <see cref="DxTreeViewListNative"/>) naimplementuje <see cref="IDxDragDropControl"/> a v konstruktoru si vytvoří instanci controlleru <see cref="DxDragDrop"/>
    /// </summary>
    public class DxDragDrop : IDisposable
    {
        #region Konstruktor, proměnné, Dispose, primární eventhandlery ze zdroje
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        public DxDragDrop(IDxDragDropControl owner)
        {
            _Owner = owner;
            DragFormInitProperties();
            DragButtons = MouseButtons.Left | MouseButtons.Right;
            DoDragReset();
            if (owner != null)
            {
                owner.MouseEnter += Source_MouseEnter;
                owner.MouseLeave += Source_MouseLeave;
                owner.MouseMove += Source_MouseMove;
                owner.MouseDown += Source_MouseDown;
                owner.MouseUp += Source_MouseUp;

                owner.QueryContinueDrag += Owner_QueryContinueDrag;
                owner.GiveFeedback += Owner_GiveFeedback;
                owner.DragEnter += Owner_DragEnter;
                owner.DragOver += Owner_DragOver;
                owner.DragLeave += Owner_DragLeave;
                owner.DragDrop += Owner_DragDrop;
            }
            else
            {
                throw new ArgumentNullException($"DxDragDrop() error : parameter 'source' is null.");
            }
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            var owner = _Owner;
            if (owner != null)
            {
                owner.MouseEnter -= Source_MouseEnter;
                owner.MouseLeave -= Source_MouseLeave;
                owner.MouseMove -= Source_MouseMove;
                owner.MouseDown -= Source_MouseDown;
                owner.MouseUp -= Source_MouseUp;

                owner.QueryContinueDrag -= Owner_QueryContinueDrag;
                owner.GiveFeedback -= Owner_GiveFeedback;
                owner.DragEnter -= Owner_DragEnter;
                owner.DragOver -= Owner_DragOver;
                owner.DragLeave -= Owner_DragLeave;
                owner.DragDrop -= Owner_DragDrop;
            }
            DoDragReset();
            _Owner = null;
        }
        /// <summary>
        /// Zdroj události DragDrop
        /// </summary>
        private IDxDragDropControl _Owner;
        /// <summary>
        /// Aktuální souřadnice myši relativně v prostoru controlu <see cref="_Owner"/>
        /// </summary>
        private Point? _SourceCurrentPoint
        {
            get
            {
                Point screenLocation = Control.MousePosition;
                var source = _Owner;
                if (source == null) return null;
                return source.PointToClient(screenLocation);
            }
        }
        #endregion
        #region Primární události navázané na události controlu Source
        /// <summary>
        /// Myš vstoupila na Source objekt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Source_MouseEnter(object sender, EventArgs e)
        {
            DoSourceMoveOver(DxDragDropState.MoveOver, _SourceCurrentPoint);
        }
        /// <summary>
        /// Myš se pohnula
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Source_MouseMove(object sender, MouseEventArgs e)
        {
            DoSourceSolveState(e.Button);
            switch (_State)
            {
                case DragStateType.None:
                case DragStateType.Over:
                    DoSourceMoveOver(DxDragDropState.MoveOver, e.Location);
                    break;
                case DragStateType.DownWait:
                    DoSourceDragStart(e.Location);
                    break;
                case DragStateType.DownDrag:
                    // Sem se běžně nedostaneme, protože za stavu DownDrag nedostává žádný Control události MouseMove ani MouseUp, ale DragDrop a další...
                    break;
            }
        }
        /// <summary>
        /// Myš provedla stisknutí tlačítka
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Source_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.None && DragButtons.HasFlag(e.Button))
                DoSourceMouseDown(e.Location);
        }
        /// <summary>
        /// Myš uvolnila stisknutí tlačítka
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Source_MouseUp(object sender, MouseEventArgs e)
        {
            switch (_State)
            {
                case DragStateType.None:
                case DragStateType.Over:
                    break;
                case DragStateType.DownWait:
                    DoDragCancel();
                    break;
                case DragStateType.DownDrag:
                    // Sem se běžně nedostaneme, protože za stavu DownDrag nedostává žádný Control události MouseMove ani MouseUp, ale DragDrop a další...
                    break;
            }
        }
        /// <summary>
        /// Myš opustila Source objekt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Source_MouseLeave(object sender, EventArgs e)
        {
            DoSourceMoveOver(DxDragDropState.None);
        }
        /// <summary>
        /// Source control zjišťuje příznaky pro Drag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Owner_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            if (sender is IDxDragDropControl dxControl && dxControl.DxDragDrop != null)
                DoDragSourceQueryContinueDrag(dxControl, e);
        }
        /// <summary>
        /// Source control může upravit kurzor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Owner_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (sender is IDxDragDropControl dxControl && dxControl.DxDragDrop != null)
                DoDragSourceGiveFeedback(dxControl, e);
        }
        /// <summary>
        /// Drag and Drop proces vstoupil na možný cíl pro Drag a Drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Owner_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is IDxDragDropControl dxControl && dxControl.DxDragDrop != null)
                DoDragTargetEnter(dxControl, e);
        }
        /// <summary>
        /// Drag and Drop proces se pohybuje nad možným cílem pro Drag a Drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Owner_DragOver(object sender, DragEventArgs e)
        {
            if (sender is IDxDragDropControl dxControl && dxControl.DxDragDrop != null)
                DoDragTargetDragOver(dxControl, e);
        }
        /// <summary>
        /// Drag and Drop proces opustil možný cíl pro Drag a Drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Owner_DragLeave(object sender, EventArgs e)
        {
            if (sender is IDxDragDropControl dxControl && dxControl.DxDragDrop != null)
                DoDragTargetLeave(dxControl, e);
        }
        /// <summary>
        /// Drag and Drop proces provádí Drop na daném cíli
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Owner_DragDrop(object sender, DragEventArgs e)
        {
            if (sender is IDxDragDropControl dxControl && dxControl.DxDragDrop != null)
                DoDragTargetDrop(dxControl, e);
        }

        #endregion
        #region Výkonné události

        // https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.control.dodragdrop?view=net-5.0

        /// <summary>
        /// Metoda řeší nepodchycené změny stavu, typicky při debugu (když debugujeme stav Drag a mezitím se uvolní tlačítko myši)
        /// </summary>
        /// <param name="buttons"></param>
        private void DoSourceSolveState(MouseButtons buttons)
        {
            if (buttons == MouseButtons.None && (_State == DragStateType.DownWait || _State == DragStateType.DownDrag))
                DoDragReset();
        }
        /// <summary>
        /// Pohyb myši nad Source objektem bez stisknutého tlačítka, anebo odchod myši ze Source objektu
        /// </summary>
        /// <param name="ddState"></param>
        /// <param name="sourcePoint"></param>
        private void DoSourceMoveOver(DxDragDropState ddState, Point? sourcePoint = null)
        {
            FillControlDragArgs();
            IDragArgs.SourceMouseLocation = sourcePoint;
            DoSourceCall(ddState);

            bool isOver = (ddState == DxDragDropState.MoveOver);
            if (_State == DragStateType.None && isOver) _State = DragStateType.Over;
            else if (_State == DragStateType.Over && !isOver) _State = DragStateType.None;
        }
        /// <summary>
        /// Po stisku tlačítka myši zachytíme souřadnici MouseDown a oblast StartSize, nastavíme stav DownWait.
        /// Nevoláme Source ani nehledáme Target.
        /// </summary>
        /// <param name="sourcePoint"></param>
        private void DoSourceMouseDown(Point sourcePoint)
        {
            FillControlDragArgs();
            _SourceStartPoint = sourcePoint;
            _SourceStartBounds = sourcePoint.CreateRectangleFromCenter(this._SourceStartSize);
            _State = DragStateType.DownWait;
            IDragArgs.SourceMouseLocation = sourcePoint;
            DoSourceCall(DxDragDropState.MouseDown);
        }
        /// <summary>
        /// Volá se po provedení pohybu myši za stavu DownWait, kdy očekáváme pohyb mimo prostor StartBounds.
        /// Detekuje pohyb mimo tento prostor a v tom případě zahajuje proces Drag.
        /// </summary>
        /// <param name="sourcePoint"></param>
        private void DoSourceDragStart(Point sourcePoint)
        {
            if (_SourceStartBounds.HasValue && _SourceStartBounds.Value.Contains(sourcePoint)) return;       // Pohyb je malý, jsme stále uvnitř prostoru StartBounds

            // Zde začíná Drag na objektu Source:
            FillControlDragArgs();
            _SourceStartBounds = null;
            _State = DragStateType.DownDrag;
            IDragArgs.SourceMouseLocation = _SourceStartPoint ?? sourcePoint;
            DoSourceCall(DxDragDropState.DragStart);
            if (DragArgs.SourceDragEnabled)
                DoDragStart();
            else
                DoDragCancel();
        }
        private void DoDragStart()
        {   // Synchronní metoda, v následujícím řádku proběhne kompletní proces Drag and Drop až do puštění myši:
            try
            {
                DragFormCreate();
                DragDropEffects result = _Owner.DoDragDrop(this, DragArgs.AllowedEffects);
                // V rámci metody _Owner.DoDragDrop() jsou volány níže uvedené metody, které řídí proces Drag and Drop...
            }
            finally
            {
                DragFormDispose();
            }

        }
        /* POŘADÍ UDÁLOSTÍ v procesu Drag and Drop:

            Poznámka: objekt Source je ten, na kterém Drag and Drop začal;
                      objekt Target je ten, nad kterým je nyní aktuálně myš (může to být tentýž jako Source, ale může to být jiný objekt)
        
          1. Zahájení procesu:            Source zavolá DoDragDrop()     =>  Source: OnQueryContinueDrag()  =>  Source: OnDragEnter()  =>  Source: OnGiveFeedback()
          2. Pohyb myši uvnitř controlu:  Source: OnQueryContinueDrag()  =>  Target: OnDragOver()   =>  Source: OnGiveFeedback()
          3. Odchod myši z controlu:      Source: OnQueryContinueDrag()  =>  Target: OnDragLeave()  =>  Source: OnGiveFeedback()
          4. Pohyb myši mimo control:     Source: OnQueryContinueDrag()
          5. Příchod myši na control:     Source: OnQueryContinueDrag()  =>  Target: OnDragEnter()  =>  Source: OnGiveFeedback()  =>  Target: OnDragOver()  =>  Source: OnGiveFeedback()
          6. Puštění myši (Drop):         Source: OnQueryContinueDrag()  =>  Target: OnDragDrop()   =>  Source: dokončení v metodě DoDragDrop()

            Metoda OnQueryContinueDrag() - je volána vždy na Source = to je organizátor procesu
                                         - je volána i při pohybu mimo jakýkoli target, ale nemá o tom aktuálně informaci, ta je až v OnGiveFeedback()
                                         - dostává argument e.Action: Continue = v průběhu procesu, nebo Drop = ukončení procesu, nebo Cancel = Escape
            Metoda OnGiveFeedback()      - je volána vždy na Source = to je organizátor procesu
                                         - nevolá se při pohybu mimo Target, nedá se tedy použít pro pozicování okna (protože pak by okno zatuhlo na posledním známém místě)
                                         - dostává argument e.Effect: None, když aktuálně není k dispozici cíl pro Drop
        */
        /// <summary>
        /// Metoda probíhá výhradně v Source objektu a dovoluje mu řídit proces
        /// </summary>
        /// <param name="dxControl"></param>
        /// <param name="e"></param>

        private void DoDragSourceQueryContinueDrag(IDxDragDropControl dxControl, QueryContinueDragEventArgs e)
        {
            // e.Action
            DragFormUpdate(IDragArgs.LastDragEffect != DragDropEffects.None);
        }
        /// <summary>
        /// Metoda probíhá výhradně v Source objektu a dovoluje mu řídit typicky kurzor
        /// </summary>
        /// <param name="dxControl"></param>
        /// <param name="e"></param>
        private void DoDragSourceGiveFeedback(IDxDragDropControl dxControl, GiveFeedbackEventArgs e)
        {
            bool isChange = (IDragArgs.LastDragEffect != e.Effect);
            IDragArgs.LastDragEffect = e.Effect;
            if (isChange)
                DragFormUpdate(IDragArgs.LastDragEffect != DragDropEffects.None);
        }


        private void DoDragTargetEnter(IDxDragDropControl dxControl, DragEventArgs e)
        {
        }

        private void DoDragTargetDragOver(IDxDragDropControl dxControl, DragEventArgs e)
        {
        }

        private void DoDragTargetLeave(IDxDragDropControl dxControl, EventArgs e)
        {
        }

        private void DoDragTargetDrop(IDxDragDropControl dxControl, DragEventArgs e)
        {
        }





        /// <summary>
        /// Volá se v procesu pohybu stisknuté myši mimo prostor Start = provádí se Drag
        /// </summary>
        private void DoTargetDragRun()
        {
            FillControlDragArgs();
            // DoTargetChange(target);
            DoSourceCall(DxDragDropState.DragMove);
            DoTargetCall(DxDragDropState.DragMove);
        }
        /// <summary>
        /// Volá se tehdy, když končí Drag and Drop jinak než předáním zdroje do cíle = cancel
        /// </summary>
        private void DoDragCancel()
        {
            FillControlDragArgs();
            DoSourceCall(DxDragDropState.DragCancel);
            DoTargetCall(DxDragDropState.DragCancel);
            DoDragEnd();
        }
        /// <summary>
        /// Volá se tehdy, když končí Drag and Drop upuštěním myši v režimu Drag, a přetahování je povoleno (<see cref="DxDragDropArgs.IsDragDropEnabled"/> je true).
        /// </summary>
        /// <param name="sourcePoint"></param>
        private void DoSourceDragDrop(Point sourcePoint)
        {
            FillControlDragArgs();
            DoSourceCall(DxDragDropState.DragDropAccept);
            DoTargetCall(DxDragDropState.DragDropAccept);
            DoDragEnd();
        }
        /// <summary>
        /// Ukončí proces Drag and Drop = pošle událost <see cref="DxDragDropState.DragEnd"/> a uvolní proměnné.
        /// </summary>
        private void DoDragEnd()
        {
            DoSourceCall(DxDragDropState.DragEnd);
            DoTargetCall(DxDragDropState.DragEnd);
            DoDragReset();
        }
        /// <summary>
        /// Do argumentu vloží aktuální pozici myši a klávesové modifikátory
        /// </summary>
        private void FillControlDragArgs()
        {
            IDragArgs.ModifierKeys = Control.ModifierKeys;
            IDragArgs.ScreenMouseLocation = Control.MousePosition;
        }
        /// <summary>
        /// Zajistí změnu a uložení objektu Target.
        /// Pokud dosavadní target byl znám, ale nový target je jiný, pak do stávajícího targetu pošle událost <see cref="DxDragDropState.DragCancel"/>.
        /// Do nového targetu neposílá nic.
        /// </summary>
        /// <param name="target"></param>
        private void DoTargetChange(IDxDragDropTarget target)
        {
            if (_CurrentTarget != null && (target == null || !Object.ReferenceEquals(_CurrentTarget, target)))
            {   // Máme uložen nějaký target z minulého kroku, ale nový target je jiný anebo žádný:
                //  pak původnímu targetu sdělíme, že jej už nepovažujeme za cíl Drag and Drop akce (pošleme stav DxDragDropState.DragCancel):
                IDragArgs.State = DxDragDropState.DragCancel;
                IDragArgs.TargetControl = _CurrentTarget;
                _CurrentTarget.DoDragTarget(_DragArgs);
                IDragArgs.TargetControl = null;
            }
            IDragArgs.TargetControl = target;
            _CurrentTarget = target;
        }
        /// <summary>
        /// Do argumentu vloží daný stav a zavolá Source objekt
        /// </summary>
        /// <param name="ddState"></param>
        private void DoSourceCall(DxDragDropState ddState)
        {
            IDragArgs.State = ddState;
            _Owner.DoDragSource(DragArgs);
        }
        /// <summary>
        /// Do argumentu vloží daný stav a zavolá Target objekt (pokud existuje a pokud je source povolen: <see cref="DxDragDropArgs.SourceDragEnabled"/> = true).
        /// </summary>
        /// <param name="ddState"></param>
        private void DoTargetCall(DxDragDropState ddState)
        {
            IDragArgs.State = ddState;
            if (_CurrentTarget != null && DragArgs.SourceDragEnabled)
                _CurrentTarget?.DoDragTarget(DragArgs);
        }
        /// <summary>
        /// Resetuje stavy. Volá se z konstruktoru, z Dispose, při řešení ztráty MouseDown stavu (typicky v debug) a při ukončení Drag procesu.
        /// </summary>
        private void DoDragReset()
        {
            DoTargetChange(null);
            IDragArgs.Reset();
            _State = DragStateType.None;
            _SourceStartPoint = null;
            _SourceStartBounds = null;
        }
        /// <summary>
        /// Argument typovaný na interface
        /// </summary>
        private IDxDragDropArgs IDragArgs { get { return DragArgs; } }
        /// <summary>
        /// Argument DragDrop s autoinicializací
        /// </summary>
        private DxDragDropArgs DragArgs
        {
            get
            {
                if (_DragArgs == null)
                    _DragArgs = new DxDragDropArgs(_Owner);
                return _DragArgs;
            }
        }
        #endregion
        #region Mini okno pro zobrazení informací o DragDrop
        private void DragFormCreate(string text, bool? enableHtml = null, bool? enabled = null)
        {
            Form form = new Form()
            {
                FormBorderStyle = FormBorderStyle.None,
                AllowTransparency = true,
                Size = new Size(250, 50),
                Text = "",
                StartPosition = FormStartPosition.Manual,
                Location = DragFormCurrentLocation,
                ShowInTaskbar = false,
                TopLevel = true
            };
            var label = new DxLabelControl() { Location = new Point(0, 0), Text = "<b>PŘESOUVÁME</b> položky:\r\n<i>1: první,\r\n2: druhá,\r\n3: třetí,\r\n4: pátá,</i>...", AllowHtmlString = true, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            label.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D;
            label.Padding = new Padding(3);
            label.Appearance.GradientMode = LinearGradientMode.Vertical;
            label.Appearance.Options.UseForeColor = true;
            label.Appearance.Options.UseBackColor = true;
            label.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            label.Appearance.TextOptions.HAlignment = HorzAlignment.Near;
            label.Appearance.Options.UseTextOptions = true;
            label.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;

            DragLabel = label;
            DragForm = form;

            form.Controls.Add(label);

            DragFormUpdateContent(text, enableHtml);
            DragFormUpdate(enabled ?? true);
            DragFormSetEnabledColors(enabled ?? true);

            form.Show();
        }
        /// <summary>
        /// Aktualizuje okno Drag form = textový obsah
        /// </summary>
        /// <param name="text"></param>
        /// <param name="enableHtml"></param>
        private void DragFormUpdateContent(string text, bool? enableHtml = null)
        {
            var form = DragForm;
            var label = DragLabel;
            if (form != null && label != null)
            {
                label.Text = text ?? "";
                if (enableHtml.HasValue) label.AllowHtmlString = enableHtml.Value;

                var optimalSize = label.CalcBestSize();
                var minSize = DragFormMinSize;
                var maxSize = DragFormMaxSize;
                int w = optimalSize.Width + 10;
                w = (w < minSize.Width ? minSize.Width : (w > maxSize.Width ? maxSize.Width : w));
                int h = optimalSize.Height + 10;
                h = (h < minSize.Width ? minSize.Height : (h > maxSize.Height ? maxSize.Height : h));

                label.Size = new Size(w, h);

                w += 2;
                h += 2;
                form.Size = new Size(w, h);
            }
        }
        /// <summary>
        /// Aktualizuje okno Drag form = pozice a barvy Enabled
        /// </summary>
        /// <param name="enabled"></param>
        private void DragFormUpdate(bool enabled)
        {
            var form = DragForm;
            if (form == null) return;
            
            var location = DragFormCurrentLocation;
            if (form.Location != location)
                form.Location = location;

            if (enabled != DragLabelEnabled)
                DragFormSetEnabledColors(enabled);
        }
        /// <summary>
        /// Nastaví barvy okna Drag form pro daný stav Enabled
        /// </summary>
        /// <param name="enabled"></param>
        private void DragFormSetEnabledColors(bool enabled)
        {
            var label = DragLabel;
            if (label != null)
            {
                label.Appearance.ForeColor = (enabled ? Color.Black : Color.DimGray);
                label.Appearance.BackColor = (enabled ? Color.FromArgb(250, 250, 240) : Color.LightGray);
                label.Appearance.BackColor2 = (enabled ? Color.FromArgb(245, 245, 225) : Color.LightGray);
            }
            DragLabelEnabled = enabled;
        }
        /// <summary>
        /// Zahodí okno Drag form
        /// </summary>
        private void DragFormDispose()
        {
            var form = DragForm;
            if (form == null) return;
            DragLabel = null;
            form.Hide();
            form.Close();
            form.Dispose();
            DragForm = null;
        }
        /// <summary>
        /// Aktuální patřičná pozice pro Drag okno
        /// </summary>
        private Point DragFormCurrentLocation
        {
            get
            {
                var point = Control.MousePosition;
                var offset = this.DragFormOffset;
                point.X -= offset.X;
                point.Y += offset.Y;
                return point;
            }
        }
        /// <summary>
        /// Inicializace defaultních hodnot pro okno DragForm
        /// </summary>
        private void DragFormInitProperties()
        {
            DragFormOffset = new Point(-30, 28);
            DragFormMinSize = new Size(150, 28);
            DragFormMaxSize = new Size(500, 160);
        }
        private bool DragLabelEnabled;
        private Form DragForm;
        private DxLabelControl DragLabel;
        /// <summary>
        /// Posun počátku Drag okna (=ToolTip při Drag and Drop) oproti pozici myši
        /// </summary>
        public Point DragFormOffset { get; set; }
        /// <summary>
        /// Minimální povolená velikost Drag okna (aby tam nebyla blecha)
        /// </summary>
        public Size DragFormMinSize { get; set; }
        /// <summary>
        /// Maximální povolená velikost Drag okna (aby tam nebyl slon)
        /// </summary>
        public Size DragFormMaxSize { get; set; }
        #endregion

        /// <summary>
        /// Stav procesu Drag and Drop
        /// </summary>
        private DragStateType _State;
        /// <summary>
        /// Průběžné parametry 
        /// </summary>
        private DxDragDropArgs _DragArgs;
        /// <summary>
        /// Souřadnice myši, zaznamenané v okamžiku LeftMouseDown, v prostoru Source controlu.
        /// </summary>
        private Point? _SourceStartPoint;
        /// <summary>
        /// Prostor okolo myši, určený v okamžiku LeftMouseDown, o velikosti <see cref="_SourceStartSize"/>.
        /// Pokud je myš stisknuta a pohybuje se v tomto prostoru, pak se nejedná o Drag and Drop, ale o chvění ruky s myší.
        /// </summary>
        private Rectangle? _SourceStartBounds;
        /// <summary>
        /// Posledně známý Target objekt
        /// </summary>
        private IDxDragDropTarget _CurrentTarget;
        /// <summary>
        /// Velikost prostoru, v němž se smí pohybovat myš, aniž by došlo k zahájení MouseDrag
        /// </summary>
        private Size _SourceStartSize { get { return System.Windows.Forms.SystemInformation.DragSize; } }
        private enum DragStateType { None, Over, DownWait, DownDrag }
        /// <summary>
        /// Buttony myši, které nastartují proces Drag and Drop.
        /// Default = <see cref="MouseButtons.Left"/> | <see cref="MouseButtons.Right"/>, tedy kterýkoli z těchto buttonů může zahájit proces.
        /// </summary>
        public MouseButtons DragButtons { get; set; }
    }
    /// <summary>
    /// Předpis pro prvek, který může být ZDROJEM anebo CÍLEM události Drag and Drop = v něm může být zahájeno přetažení prvku jinam nebo ukončeno tažení prvku odjinud.
    /// </summary>
    public interface IDxDragDropControl
    {
        /// <summary>
        /// Controller pro DxDragDrop v daném controlu
        /// </summary>
        DxDragDrop DxDragDrop { get; }
        /// <summary>
        /// Metoda volaná do objektu Source (zdroj Drag and Drop) při každé akci na straně zdroje.
        /// Předávaný argument <paramref name="args"/> je permanentní, dokud se myš pohybuje nad Source controlem nebo dokud probíhá Drag akce.
        /// </summary>
        /// <param name="args">Veškerá data o procesu Drag and Drop, permanentní po dobu výskytu myši nad Source objektem</param>
        void DoDragSource(DxDragDropArgs args);
        /// <summary>
        /// Metoda volaná do objektu Target (cíl Drag and Drop) při každé akci, pokud se myš nachází nad objektem který implementuje <see cref="IDxDragDropTarget"/>.
        /// Předávaný argument <paramref name="args"/> je permanentní, dokud se myš pohybuje nad Source controlem nebo dokud probíhá Drag akce.
        /// </summary>
        /// <param name="args">Veškerá data o procesu Drag and Drop, permanentní po dobu výskytu myši nad Source objektem</param>
        void DoDragTarget(DxDragDropArgs args);

        // Následující metody a eventy deklaruje každý Control, nemusí se explicitně implementovat. Ale v interface je potřebujeme mít deklarované pro práci v DxDragDrop:

        /// <summary>
        /// Nativní metoda Controlu.
        /// Vyvolá proces Drag, synchronní, počká na jeho dokončení.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="allowedEffects"></param>
        /// <returns></returns>
        DragDropEffects DoDragDrop(object data, DragDropEffects allowedEffects);
        /// <summary>
        /// Nativní metoda Controlu.
        /// Vrátí formulář, na němž je control umístěn.
        /// </summary>
        /// <returns></returns>
        Form FindForm();
        /// <summary>
        /// Nativní metoda Controlu.
        /// Převede souřadnici v prostoru Screen do prostoru Controlu (tedy Source controlu)
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <returns></returns>
        Point PointToClient(Point screenPoint);
        /// <summary>
        /// Nativní událost Controlu.
        /// </summary>
        event EventHandler MouseEnter;
        /// <summary>
        /// Nativní událost Controlu.
        /// </summary>
        event EventHandler MouseLeave;
        /// <summary>
        /// Nativní událost Controlu.
        /// </summary>
        event MouseEventHandler MouseMove;
        /// <summary>
        /// Nativní událost Controlu.
        /// </summary>
        event MouseEventHandler MouseDown;
        /// <summary>
        /// Nativní událost Controlu.
        /// </summary>
        event MouseEventHandler MouseUp;

        /// <summary>
        /// Nativní událost Controlu.
        /// Tato je vždy volána v Source controlu.
        /// </summary>
        event QueryContinueDragEventHandler QueryContinueDrag;
        /// <summary>
        /// Nativní událost Controlu.
        /// Tato je vždy volána v Source controlu.
        /// </summary>
        event GiveFeedbackEventHandler GiveFeedback;
        /// <summary>
        /// Nativní událost Controlu.
        /// Tato je volána v Source nebo Target controlu, podle toho kde je myš.
        /// </summary>
        event DragEventHandler DragEnter;
        /// <summary>
        /// Nativní událost Controlu.
        /// Tato je volána v Source nebo Target controlu, podle toho kde je myš.
        /// </summary>
        event DragEventHandler DragOver;
        /// <summary>
        /// Nativní událost Controlu.
        /// Tato je volána v Source nebo Target controlu, podle toho kde je myš.
        /// </summary>
        event EventHandler DragLeave;
        /// <summary>
        /// Nativní událost Controlu.
        /// Tato je volána v Source nebo Target controlu, podle toho kde je myš.
        /// </summary>
        event DragEventHandler DragDrop;
    }
    /// <summary>
    /// Předpis pro prvek, který může být CÍLEM události Drag and Drop = do něj může být přetažen prvek <see cref="IDxDragDropControl"/> nebo jeho součást.
    /// </summary>
    public interface IDxDragDropTarget
    {
        /// <summary>
        /// Metoda volaná do objektu Target (cíl Drag and Drop) při každé akci, pokud se myš nachází nad objektem který implementuje <see cref="IDxDragDropTarget"/>.
        /// Předávaný argument <paramref name="args"/> je permanentní, dokud se myš pohybuje nad Source controlem nebo dokud probíhá Drag akce.
        /// </summary>
        /// <param name="args">Veškerá data o procesu Drag and Drop, permanentní po dobu výskytu myši nad Source objektem</param>
        void DoDragTarget(DxDragDropArgs args);
    }
    /// <summary>
    /// Data předávaná v procesu Drag and Drop mezi prvkem Source a Target
    /// </summary>
    public class DxDragDropArgs : IDxDragDropArgs
    {
        #region Konstruktor a public property
        /// <summary>
        /// Konstruktor pro daný Source objekt
        /// </summary>
        /// <param name="sourceControl"></param>
        public DxDragDropArgs(IDxDragDropControl sourceControl)
        {
            this.SourceControl = sourceControl;
            this.SourceDragEnabled = true;
            this.TargetDropEnabled = false;
        }
        /// <summary>
        /// Reset
        /// </summary>
        private void Reset()
        {
            this.State = DxDragDropState.None;
            this.SourceDragEnabled = true;
            this.SourceMouseLocation = null;
            this.SourceObject = null;
            if (this.SourceImage != null)
            {
                this.SourceImage.Dispose();
                this.SourceImage = null;
            }
            this.ModifierKeys = Keys.None;
            this.SourceTag = null;
            this.TargetControl = null;
            this.TargetTag = null;
            this.TargetDropEnabled = false;
            this.LastDragEffect = DragDropEffects.None;
        }
        /// <summary>
        /// Stav procesu Drag and Drop = aktuální událost
        /// </summary>
        public DxDragDropState State { get; private set; }
        /// <summary>
        /// Aktuální souřadnice v absolutních hodnotách (vzhledem ke Screen)
        /// </summary>
        public Point ScreenMouseLocation { get; private set; }
        /// <summary>
        /// Souřadnice v prostoru zdrojového controlu, kde přetahování začalo
        /// </summary>
        public Point? SourceMouseLocation { get; private set; }
        /// <summary>
        /// Aktuálně stisknuté klávesy Ctrl + Shift + Alt
        /// </summary>
        public Keys ModifierKeys { get; private set; }
        /// <summary>
        /// Zdrojový objekt (ListBox, TreeView, atd)
        /// </summary>
        public IDxDragDropControl SourceControl { get; private set; }
        /// <summary>
        /// Zdrojový control nastavuje true / false podle toho, zda aktuální výchozí prvek může být přemístěn.
        /// Typicky ListBox nastaví true, pokud myš uchopila (v DragStart) existující Item v ListBoxu, nebo v TreeView myš uchopila TreeNode.
        /// Pokud uživatel zkusí přetahovat prázdný prostor v TreeNode, pak sem zdroj nastaví false a přetahování nemá smysl.
        /// </summary>
        public bool SourceDragEnabled { get; set; }
        /// <summary>
        /// Možnosti pro proces Drag (povolené efekty)
        /// </summary>
        public DragDropEffects AllowedEffects { get; set; }
        /// <summary>
        /// Jakýkoli objekt ze zdrojového controlu. Typicky to může být prvek ListBoxu nebo uzel TreeView, nebo ColumnHeader nebo cokoli jiného.
        /// </summary>
        public object SourceObject { get; set; }
        /// <summary>
        /// Libovolná data zdrojového objektu.
        /// </summary>
        public object SourceTag { get; set; }
        /// <summary>
        /// Volitelně obrázek zdrojového objektu.
        /// </summary>
        public Image SourceImage { get; set; }
        /// <summary>
        /// Cílový objekt přetahování = do kterého controlu ukazuje myš.
        /// </summary>
        public IDxDragDropTarget TargetControl { get; private set; }
        /// <summary>
        /// Cílový objekt <see cref="TargetControl"/> nastavuje true / false podle toho, zda může akceptovat Drag and Drop.
        /// </summary>
        public bool TargetDropEnabled { get; set; }
        /// <summary>
        /// Libovolná data cílového objektu. Hodnota <see cref="TargetTag"/> je zahozena po opuštění cílového objektu <see cref="TargetControl"/>.
        /// </summary>
        public object TargetTag { get; set; }
        /// <summary>
        /// Obsahuje true, pokud je možno povolit Drag and Drop akci (existuje cíl <see cref="TargetControl"/>, 
        /// je povolen Drag ze zdroje <see cref="SourceDragEnabled"/> a je povolen Drop v cíli <see cref="TargetDropEnabled"/>).
        /// </summary>
        public bool IsDragDropEnabled { get { return (TargetControl != null && SourceDragEnabled && TargetDropEnabled); } }
        /// <summary>
        /// Posledně známý DragDrop efekt
        /// </summary>
        public DragDropEffects LastDragEffect { get; private set; }
        #endregion
        #region Interní přístup k datům
        DxDragDropState IDxDragDropArgs.State { get { return this.State; } set { this.State = value; } }
        Point IDxDragDropArgs.ScreenMouseLocation { get { return this.ScreenMouseLocation; } set { this.ScreenMouseLocation = value; } }
        Point? IDxDragDropArgs.SourceMouseLocation { get { return this.SourceMouseLocation; } set { this.SourceMouseLocation = value; } }
        Keys IDxDragDropArgs.ModifierKeys { get { return this.ModifierKeys; } set { this.ModifierKeys = value; } }
        IDxDragDropTarget IDxDragDropArgs.TargetControl { get { return this.TargetControl; } set { this.TargetControl = value; } }
        DragDropEffects IDxDragDropArgs.LastDragEffect { get { return this.LastDragEffect; } set { this.LastDragEffect = value; } }
        void IDxDragDropArgs.Reset() { this.Reset(); }
        #endregion
    }
    /// <summary>
    /// Interface pro interní setování hodnot do <see cref="DxDragDropArgs"/>
    /// </summary>
    internal interface IDxDragDropArgs
    {
        /// <summary>
        /// Stav procesu Drag and Drop = aktuální událost
        /// </summary>
        DxDragDropState State { get; set; }
        Point ScreenMouseLocation { get; set; }
        Point? SourceMouseLocation { get; set; }
        /// <summary>
        /// Aktuálně stisknuté klávesy Ctrl + Shift + Alt
        /// </summary>
        Keys ModifierKeys { get; set; }
        IDxDragDropTarget TargetControl { get; set; }
        /// <summary>
        /// Posledně známý DragDrop efekt
        /// </summary>
        DragDropEffects LastDragEffect { get; set; }

        void Reset();
    }
    /// <summary>
    /// Stav procesu Drag and Drop, předávaný do Source i Target objektu.
    /// </summary>
    public enum DxDragDropState
    {
        /// <summary>
        /// Tato událost se předává do Source objektu v okamžiku, kdy myš opouští Source objekt a pohybuje se jinam, a neprobíhá Drag proces.
        /// </summary>
        None,
        /// <summary>
        /// Tato událost se předává do Source objektu v okamžiku, kdy se myš pohybuje nad Source objektem, ale není stisknuta.
        /// </summary>
        MoveOver,
        /// <summary>
        /// Tato událost se předává do Source objektu v okamžiku, kdy se myš stiskla, ale ještě nezačal Drag proces (pohyb stisknuté myši).
        /// Pak následuje událost <see cref="DragStart"/>.
        /// </summary>
        MouseDown,
        /// <summary>
        /// Tato událost se předává do Source objektu v okamžiku, kdy je stisknuta myš a následně je s ní pohnuto.
        /// Source objekt v tomto okamžiku rozhoduje, zda on sám nebo některý jeho prvek se bude přesouvat, 
        /// a měl by nastavit do argumentu <see cref="DxDragDropArgs.SourceDragEnabled"/> hodnotu true, pokud umožní provádět Drag daného zdroje.
        /// Pokud to nenastaví, pak se nebude řešit nic s Target (nebude se hledat a nebude tedy dostávat události), prostě Drag and Drop je neaktivní.
        /// </summary>
        DragStart,
        /// <summary>
        /// Tuto událost dostává Source i Target objekt, poté kdy byl nalezen Target. Source objekt tak může reagovat na případný Target.
        /// Každý z objektů může nastavit svůj příznak Enabled (<see cref="DxDragDropArgs.SourceDragEnabled"/> a <see cref="DxDragDropArgs.TargetDropEnabled"/>).
        /// </summary>
        DragMove,
        /// <summary>
        /// Tato událost se předává do Source i do Target objektu v okamžiku, kdy Source i Target objekt deklarují Enabled = true a uživatel pustil myš.
        /// Právě nyní se má provést požadovaná akce.
        /// </summary>
        DragDropAccept,
        /// <summary>
        /// Tato událost se předává do Source i do Target objektu v okamžiku, kdy proces Drag and Drop je zrušen = 
        /// myš je uvolněna a Target je null nebo některý Enabled je false.
        /// </summary>
        DragCancel,
        /// <summary>
        /// Tato událost se předává do Source i do Target objektu v okamžiku, kdy proces Drag and Drop je ukončen 
        /// (tedy po <see cref="DragDropAccept"/> i po <see cref="DragCancel"/>) jako závěrečná událost.
        /// </summary>
        DragEnd
    }
    #endregion
}
