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
            if (_LastId > 2000000000) _LastId = 0;
            _Id = ++_LastId;

            __Owner = new WeakTarget<IDxDragDropControl>(owner);
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
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.GetType().Name + "." + _Id.ToString() + "; Owner: " + (Owner?.ToString() ?? "NULL");
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            var owner = Owner;
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
            __Owner = null;
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
            DoSourceMoveOver(DxDragDropEventType.MoveOver, _SourceCurrentPoint);
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
                    DoSourceMoveOver(DxDragDropEventType.MoveOver, e.Location);
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
            DoSourceMoveOver(DxDragDropEventType.None);
        }
        /// <summary>
        /// Source control zjišťuje příznaky pro Drag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Owner_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            if (sender is IDxDragDropControl dxSourceControl && dxSourceControl.DxDragDrop != null)
                DoDragSourceQueryContinueDrag(dxSourceControl, e);
        }
        /// <summary>
        /// Source control může upravit kurzor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Owner_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (sender is IDxDragDropControl dxSourceControl && dxSourceControl.DxDragDrop != null)
                DoDragSourceGiveFeedback(dxSourceControl, e);
        }
        /// <summary>
        /// Drag and Drop proces vstoupil na možný cíl pro Drag a Drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Owner_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is IDxDragDropControl dxTargetControl && dxTargetControl.DxDragDrop != null)
                DoDragTargetEnter(dxTargetControl, e);
        }
        /// <summary>
        /// Drag and Drop proces se pohybuje nad možným cílem pro Drag a Drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Owner_DragOver(object sender, DragEventArgs e)
        {
            if (sender is IDxDragDropControl dxTargetControl && dxTargetControl.DxDragDrop != null)
                DoDragTargetDragOver(dxTargetControl, e);
        }
        /// <summary>
        /// Drag and Drop proces opustil možný cíl pro Drag a Drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Owner_DragLeave(object sender, EventArgs e)
        {
            if (sender is IDxDragDropControl dxTargetControl && dxTargetControl.DxDragDrop != null)
                DoDragTargetLeave(dxTargetControl, e);
        }
        /// <summary>
        /// Drag and Drop proces provádí Drop na daném cíli
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Owner_DragDrop(object sender, DragEventArgs e)
        {
            if (sender is IDxDragDropControl dxTargetControl && dxTargetControl.DxDragDrop != null)
                DoDragTargetDrop(dxTargetControl, e);
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
        private void DoSourceMoveOver(DxDragDropEventType ddState, Point? sourcePoint = null)
        {
            FillControlDragArgs();
            IDragArgs.SourceMouseLocation = sourcePoint;
            DoSourceCall(ddState);

            bool isOver = (ddState == DxDragDropEventType.MoveOver);
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
            DoSourceCall(DxDragDropEventType.MouseDown);
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
            DoSourceCall(DxDragDropEventType.DragStart);
            if (DragArgs.SourceDragEnabled)
                DoDragStart();
            else
                DoDragCancel();
        }
        /// <summary>
        /// V této metodě proběhne celý proces Drag and Drop
        /// </summary>
        private void DoDragStart()
        {   // Synchronní metoda, v následujícím řádku proběhne kompletní proces Drag and Drop až do puštění myši:
            try
            {
                DragFormCreate(DragArgs.SourceText, DragArgs.SourceTextAllowHtml);
                // V rámci metody _Owner.DoDragDrop() jsou volány níže uvedené metody, které řídí proces Drag and Drop...
                DragDropEffects result = Owner.DoDragDrop(this, DragArgs.AllowedEffects);
                // Teprve až proces Drag and Drop skončí, dostane se řízení na tento řádek...
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
        /// Metoda probíhá výhradně v Source objektu a dovoluje mu řídit proces.
        /// Tato metoda je volána vždy jako první v každém kroku procesu Drag, 
        /// a to i tehdy když není známý cíl, např. i když jsme mimo okno aplikace.
        /// </summary>
        /// <param name="dxControl"></param>
        /// <param name="e"></param>
        private void DoDragSourceQueryContinueDrag(IDxDragDropControl dxControl, QueryContinueDragEventArgs e)
        {
            FillControlDragArgs(e);
            DoSourceCall(DxDragDropEventType.DragQuerySource);
            DragFormUpdate(IDragArgs.LastDragEffect != DragDropEffects.None);
            e.Action = DragArgs.DragAction;
        }
        /// <summary>
        /// Metoda probíhá výhradně v Source objektu a dovoluje mu řídit typicky kurzor.
        /// Tato metoda je volána jako poslední v jednom každém kroku procesu Drag, 
        /// kromě situace kdy Drag probíhá nad nepovoleným cílem (např. jsme mimo okno aplikace).
        /// </summary>
        /// <param name="dxControl"></param>
        /// <param name="e"></param>
        private void DoDragSourceGiveFeedback(IDxDragDropControl dxControl, GiveFeedbackEventArgs e)
        {
            FillControlDragArgs(e);
            DoSourceCall(DxDragDropEventType.DragGiveFeedbackSource);

            bool isChange = (IDragArgs.LastDragEffect != e.Effect);
            IDragArgs.LastDragEffect = e.Effect;
            if (isChange)
                DragFormUpdate(IDragArgs.LastDragEffect != DragDropEffects.None);

            e.UseDefaultCursors = DragArgs.DragUseDefaultCursors;
        }
        /// <summary>
        /// Proces Drag and Drop vstoupil na nějaký cíl
        /// Metoda je volána pro ten objekt, na kterém se aktuálně nachází myš = cíl procesu Drag and Drop.
        /// </summary>
        /// <param name="dxTargetControl">Cílový control, nad nímž se aktuálně pohybuje myš</param>
        /// <param name="e"></param>

        private void DoDragTargetEnter(IDxDragDropControl dxTargetControl, DragEventArgs e)
        {
            if (!TryGetDragSource(e, out DxDragDrop dxSourceDrag)) return;
            dxSourceDrag.DoDragSourceEnterToTarget(dxTargetControl, e);        // Pro přehlednost přejdu do instance DxDragDrop, náležející do Source controlu

            // Do instance DxDragDrop, patřící k Target controlu, uložíme odkaz na SourceControl, to kvůli události DoDragTargetLeave:
            var dxSourceControl = dxSourceDrag.Owner;
            dxTargetControl.DxDragDrop.__CurrentSource = (dxSourceControl != null ? new WeakTarget<IDxDragDropControl>(dxSourceControl) : null);
        }
        /// <summary>
        /// Proces Drag and Drop vstoupil na nějaký cíl
        /// Metoda je volána v instanci odpovídající Source controlu!
        /// </summary>
        /// <param name="dxTargetControl">Cílový control, nad nímž se aktuálně pohybuje myš</param>
        /// <param name="e"></param>
        private void DoDragSourceEnterToTarget(IDxDragDropControl dxTargetControl, DragEventArgs e)
        {
            FillControlDragArgs(e);
            DoTargetChange(dxTargetControl);                                   // Instance DxDragDrop, patřící k Source, si zde do sebe uloží Control Target
            DoSourceCall(DxDragDropEventType.DragEnterToTarget);
            DoTargetCall(DxDragDropEventType.DragEnterToTarget);
            e.Effect = DragArgs.CurrentEffect;
        }
        /// <summary>
        /// Proces Drag and Drop se pohybuje nad nějakým cílem
        /// Metoda je volána pro ten objekt, na kterém se aktuálně nachází myš = cíl procesu Drag and Drop.
        /// </summary>
        /// <param name="dxTargetControl">Cílový control, nad nímž se aktuálně pohybuje myš</param>
        /// <param name="e"></param>
        private void DoDragTargetDragOver(IDxDragDropControl dxTargetControl, DragEventArgs e)
        {
            if (!TryGetDragSource(e, out DxDragDrop dxSourceDrag)) return;

            dxSourceDrag.DoDragSourceDragOverTarget(dxTargetControl, e);       // Pro přehlednost přejdu do instance DxDragDrop, náležející do Source controlu
        }
        /// <summary>
        /// Proces Drag and Drop se pohybuje nad nějakým cílem
        /// Metoda je volána v instanci odpovídající Source controlu!
        /// </summary>
        /// <param name="dxTargetControl">Cílový control, nad nímž se aktuálně pohybuje myš</param>
        /// <param name="e"></param>
        private void DoDragSourceDragOverTarget(IDxDragDropControl dxTargetControl, DragEventArgs e)
        {
            FillControlDragArgs(e);
            DoTargetCall(DxDragDropEventType.DragMove);
            DoSourceCall(DxDragDropEventType.DragMove);
            e.Effect = DragArgs.CurrentEffect;
        }
        /// <summary>
        /// Proces Drag and Drop opustil dosavadní cíl
        /// Metoda je volána pro ten objekt, na kterém se dosud nacházela myš = bývalý cíl procesu Drag and Drop.
        /// </summary>
        /// <param name="dxTargetControl"></param>
        /// <param name="e"></param>
        private void DoDragTargetLeave(IDxDragDropControl dxTargetControl, EventArgs e)
        {
            // Tady jsme v instanci Target, a ta by měla (ve své instanci DxDragDrop) udržovat WeakReferenci na instanci Source, v .CurrentSource:
            var dxSourceControl = dxTargetControl.DxDragDrop?.CurrentSource;
            if (dxSourceControl == null) return;

            // Control Source by měl mít svoji řídící instanci DxDragDrop:
            DxDragDrop dxSourceDrag = dxSourceControl.DxDragDrop;
            if (dxSourceDrag == null) return;

            dxSourceDrag.DoDragSourceLeaveTarget(dxTargetControl, e);          // Pro přehlednost přejdu do instance DxDragDrop, náležející do Source controlu

            dxTargetControl.DxDragDrop.__CurrentSource = null;
        }
        /// <summary>
        /// Proces Drag and Drop opustil dosavadní cíl
        /// Metoda je volána v instanci odpovídající Source controlu!
        /// </summary>
        /// <param name="dxTargetControl"></param>
        /// <param name="e"></param>
        private void DoDragSourceLeaveTarget(IDxDragDropControl dxTargetControl, EventArgs e)
        {
            FillControlDragArgs();
            DoTargetCall(DxDragDropEventType.DragLeaveOfTarget);
            DoSourceCall(DxDragDropEventType.DragLeaveOfTarget);
            DoTargetChange(null);                                              // Instance DxDragDrop, patřící k Source, se odpojí od Controlu Target
        }
        /// <summary>
        /// Proces Drag and Drop je dokončen = došlo k Drop objektu do cíle.
        /// Metoda je volána pro ten objekt, na kterém se aktuálně nachází myš = cíl procesu Drag and Drop.
        /// </summary>
        /// <param name="dxTargetControl"></param>
        /// <param name="e"></param>
        private void DoDragTargetDrop(IDxDragDropControl dxTargetControl, DragEventArgs e)
        {
            if (!TryGetDragSource(e, out DxDragDrop dxSourceDrag)) return;

            dxSourceDrag.DoDragSourceTargetDrop(dxTargetControl, e);           // Pro přehlednost přejdu do instance DxDragDrop, náležející do Source controlu
        }
        /// <summary>
        /// Proces Drag and Drop je dokončen = došlo k Drop objektu do cíle.
        /// Metoda je volána v instanci odpovídající Source controlu!
        /// </summary>
        /// <param name="dxTargetControl"></param>
        /// <param name="e"></param>
        private void DoDragSourceTargetDrop(IDxDragDropControl dxTargetControl, DragEventArgs e)
        {
            FillControlDragArgs(e);
            DoSourceCall(DxDragDropEventType.DragDropAccept);
            DoTargetCall(DxDragDropEventType.DragDropAccept);
            e.Effect = DragArgs.CurrentEffect;
        }
        /// <summary>
        /// Volá se tehdy, když končí Drag and Drop jinak než předáním zdroje do cíle = cancel
        /// </summary>
        private void DoDragCancel()
        {
            FillControlDragArgs();
            DoSourceCall(DxDragDropEventType.DragCancel);
            DoTargetCall(DxDragDropEventType.DragCancel);
            DoDragEnd();
        }
        /// <summary>
        /// Ukončí proces Drag and Drop = pošle událost <see cref="DxDragDropEventType.DragEnd"/> a uvolní proměnné.
        /// </summary>
        private void DoDragEnd()
        {
            DoSourceCall(DxDragDropEventType.DragEnd);
            DoTargetCall(DxDragDropEventType.DragEnd);
            DoDragReset();
        }
        /// <summary>
        /// Do argumentu vloží data z argumentu <see cref="QueryContinueDragEventArgs.Action"/> a <see cref="QueryContinueDragEventArgs.EscapePressed"/>,
        /// a také aktuální pozici myši a klávesové modifikátory
        /// </summary>
        private void FillControlDragArgs(QueryContinueDragEventArgs e)
        {
            FillControlDragArgs();
            DragArgs.DragAction = e.Action;
            IDragArgs.DragEscapePressed = e.EscapePressed;
        }
        /// <summary>
        /// Do argumentu vloží data z argumentu <see cref="GiveFeedbackEventArgs.Effect"/> a <see cref="GiveFeedbackEventArgs.UseDefaultCursors"/>,
        /// a také aktuální pozici myši a klávesové modifikátory
        /// </summary>
        private void FillControlDragArgs(GiveFeedbackEventArgs e)
        {
            FillControlDragArgs();
            DragArgs.CurrentEffect = e.Effect;
            DragArgs.DragUseDefaultCursors = e.UseDefaultCursors;
        }
        /// <summary>
        /// Do argumentu vloží data z argumentu <see cref="DragEventArgs.AllowedEffect"/> a <see cref="DragEventArgs.Effect"/>,
        /// a také aktuální pozici myši a klávesové modifikátory
        /// </summary>
        private void FillControlDragArgs(DragEventArgs e)
        {
            FillControlDragArgs();
            DragArgs.AllowedEffects = e.AllowedEffect;
            DragArgs.CurrentEffect = e.Effect;
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
        /// Pokud dosavadní target byl znám, ale nový target je jiný, pak do stávajícího targetu pošle událost <see cref="DxDragDropEventType.DragCancel"/>.
        /// Do nového targetu neposílá nic.
        /// </summary>
        /// <param name="target"></param>
        private void DoTargetChange(IDxDragDropControl target)
        {
            var currentTarget = CurrentTarget;
            if (currentTarget != null && (target == null || !Object.ReferenceEquals(currentTarget, target)))
            {   // Máme uložen nějaký target z minulého kroku, ale nový target je jiný anebo žádný:
                //  pak původnímu targetu sdělíme, že jej už nepovažujeme za cíl Drag and Drop akce (pošleme stav DxDragDropState.DragCancel):
                IDragArgs.State = DxDragDropEventType.DragCancel;
                IDragArgs.TargetControl = currentTarget;
                currentTarget.DoDragTarget(_DragArgs);
                IDragArgs.TargetControl = null;
            }
            IDragArgs.TargetControl = target;
            __CurrentTarget = (target != null ? new WeakTarget<IDxDragDropControl>(target) : null);
        }
        /// <summary>
        /// Do argumentu vloží daný stav a zavolá Source objekt
        /// </summary>
        /// <param name="ddState"></param>
        private void DoSourceCall(DxDragDropEventType ddState)
        {
            IDragArgs.State = ddState;
            Owner.DoDragSource(DragArgs);
        }
        /// <summary>
        /// Do argumentu vloží daný stav a zavolá Target objekt (pokud existuje a pokud je source povolen: <see cref="DxDragDropArgs.SourceDragEnabled"/> = true).
        /// </summary>
        /// <param name="ddState"></param>
        private void DoTargetCall(DxDragDropEventType ddState)
        {
            IDragArgs.State = ddState;
            if (CurrentTarget != null && DragArgs.SourceDragEnabled)
                CurrentTarget?.DoDragTarget(DragArgs);
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
            __CurrentSource = null;
            __CurrentTarget = null;
        }
        /// <summary>
        /// Metoda najde zdrojový objekt <see cref="DxDragDrop"/>, který by měl být uložený v <see cref="DragEventArgs.Data"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="dxDragDrop"></param>
        /// <returns></returns>
        private bool TryGetDragSource(DragEventArgs e, out DxDragDrop dxDragDrop)
        {
            dxDragDrop = null;
            if (e.Data.GetDataPresent(typeof(DxDragDrop)))
                dxDragDrop = e.Data.GetData(typeof(DxDragDrop)) as DxDragDrop;
            return (dxDragDrop != null);
        }
        #endregion
        #region Data
        /// <summary>
        /// Buttony myši, které nastartují proces Drag and Drop.
        /// Default = <see cref="MouseButtons.Left"/> | <see cref="MouseButtons.Right"/>, tedy kterýkoli z těchto buttonů může zahájit proces.
        /// </summary>
        public MouseButtons DragButtons { get; set; }
        /// <summary>
        /// ID tohoto objektu
        /// </summary>
        private int _Id;
        /// <summary>
        /// ID posledně přidělené
        /// </summary>
        private static int _LastId = 0;
        /// <summary>
        /// Aktuální souřadnice myši relativně v prostoru controlu <see cref="Owner"/>
        /// </summary>
        private Point? _SourceCurrentPoint
        {
            get
            {
                Point screenLocation = Control.MousePosition;
                var source = Owner;
                if (source == null) return null;
                return source.PointToClient(screenLocation);
            }
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
                    _DragArgs = new DxDragDropArgs(Owner);
                return _DragArgs;
            }
        }
        /// <summary>
        /// Průběžné parametry 
        /// </summary>
        private DxDragDropArgs _DragArgs;
        /// <summary>
        /// Stav procesu Drag and Drop
        /// </summary>
        private DragStateType _State;
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
        /// Velikost prostoru, v němž se smí pohybovat myš, aniž by došlo k zahájení MouseDrag
        /// </summary>
        private Size _SourceStartSize { get { return System.Windows.Forms.SystemInformation.DragSize; } }
        /// <summary>
        /// Vlastník this instance = Control, který podporuje Drag and Drop
        /// </summary>
        private IDxDragDropControl Owner { get { return __Owner?.Target; } } private WeakTarget<IDxDragDropControl> __Owner;
        /// <summary>
        /// Posledně známý Source objekt, to když this je Target prvek.
        /// V průběhu Drag and Drop se mění.
        /// </summary>
        private IDxDragDropControl CurrentSource { get { return __CurrentSource?.Target; } } private WeakTarget<IDxDragDropControl> __CurrentSource;
        /// <summary>
        /// Posledně známý Target objekt, to když this je Source prvek.
        /// V průběhu Drag and Drop se mění.
        /// </summary>
        private IDxDragDropControl CurrentTarget { get { return __CurrentTarget?.Target; } } private WeakTarget<IDxDragDropControl> __CurrentTarget;
        /// <summary>
        /// Interní stavy procesu, řídí začátek = MouseDown, a Wait to Drag
        /// </summary>
        private enum DragStateType { None, Over, DownWait, DownDrag }



        #endregion
        #region Mini okno pro zobrazení informací o DragDrop
        /// <summary>
        /// Vytvoří okno DragForm, naplní jej dodanými daty, a rozsvítí jej. Okno je nemodální.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="enableHtml"></param>
        /// <param name="enabled"></param>
        private void DragFormCreate(string text, bool? enableHtml = null, bool? enabled = null)
        {
            _DragForm = new DxDragForm(this);
            _DragForm.InfoText = text;
            _DragForm.InfoTextEnableHtml = enableHtml ?? true;
            _DragForm.IsEnabled = enabled ?? true;
        }
        /// <summary>
        /// Aktualizuje okno Drag form = pozice a barvy podle požadovaného stavu Enabled.
        /// </summary>
        /// <param name="enabled"></param>
        private void DragFormUpdate(bool enabled)
        {
            var dragForm = _DragForm;
            if (dragForm == null) return;
            dragForm.IsEnabled = enabled;
            dragForm.Position = this.DragFormCurrentLocation;

            if (!dragForm.Visible)
                dragForm.Show();
        }
        /// <summary>
        /// Zahodí okno Drag form
        /// </summary>
        private void DragFormDispose()
        {
            var form = _DragForm;

            if (form == null) return;
            form.Hide();
            form.Close();
            form.Dispose();
            _DragForm = null;
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
                point.X += offset.X;
                point.Y += offset.Y;
                return point;
            }
        }
        /// <summary>
        /// Inicializace defaultních hodnot pro okno DragForm
        /// </summary>
        private void DragFormInitProperties()
        {
            DragFormOffset = new Point(-20, 35);
            DragFormMinSize = new Size(150, 45);
            DragFormMaxSize = new Size(500, 160);
            DragFormTextColor = Color.Black;
            DragFormTextColorDisabled = Color.Black;
            DragFormBackColor1 = Color.FromArgb(250, 250, 230);
            DragFormBackColor1Disabled = Color.FromArgb(240, 240, 240);
            DragFormBackColor2 = Color.FromArgb(240, 240, 190);
            DragFormBackColor2Disabled = Color.FromArgb(230, 230, 230);
        }
        /// <summary>
        /// Instance okna Drag Form, je platná jen v době, kdy je vidět (kdy probíhá Drag).
        /// </summary>
        private DxDragForm _DragForm;
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
        /// <summary>
        /// Barva textu Enabled
        /// </summary>
        public Color DragFormTextColor { get; set; }
        /// <summary>
        /// Barva textu Disabled
        /// </summary>
        public Color DragFormTextColorDisabled { get; set; }
        /// <summary>
        /// Barva pozadí nahoře Enabled
        /// </summary>
        public Color DragFormBackColor1 { get; set; }
        /// <summary>
        /// Barva pozadí nahoře Disabled
        /// </summary>
        public Color DragFormBackColor1Disabled { get; set; }
        /// <summary>
        /// Barva pozadí dole Enabled
        /// </summary>
        public Color DragFormBackColor2 { get; set; }
        /// <summary>
        /// Barva pozadí dole Disabled
        /// </summary>
        public Color DragFormBackColor2Disabled { get; set; }
        /// <summary>
        /// Formulář zobrazující informace v procesu Drag and Drop
        /// </summary>
        private class DxDragForm : Form
        {
            public DxDragForm(DxDragDrop owner)
            {
                this._Owner = owner;
                Initialize();
            }
            private void Initialize()
            {
                FormBorderStyle = FormBorderStyle.None;
                AllowTransparency = true;
                TransparencyKey = Color.Magenta;
                BackColor = Color.Magenta;
                Size = new Size(250, 50);
                Text = "";
                StartPosition = FormStartPosition.Manual;
                ShowInTaskbar = false;
                TopLevel = true;

                var label = new DxLabelControl() { Location = new Point(0, 0), Text = "", AllowHtmlString = true, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
                label.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D;
                label.Padding = new Padding(3);
                label.Appearance.GradientMode = LinearGradientMode.Vertical;
                label.Appearance.Options.UseForeColor = true;
                label.Appearance.Options.UseBackColor = true;
                label.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
                label.Appearance.TextOptions.HAlignment = HorzAlignment.Near;
                label.Appearance.Options.UseTextOptions = true;
                label.AutoEllipsis = true;
                label.AutoSizeMode = LabelAutoSizeMode.None;
                //             label.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;

                _InfoLabel = label;
                this.Controls.Add(label);
            }
            /// <summary>
            /// Dispose
            /// </summary>
            /// <param name="disposing"></param>
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                this._Owner = null;
                this._InfoLabel = null;
            }
            /// <summary>
            /// Aktualizuje okno Drag form = textový obsah
            /// </summary>
            /// <param name="titleText"></param>
            /// <param name="infoText"></param>
            /// <param name="allowHtmlString"></param>
            private void DragFormUpdateContent(string titleText, string infoText, bool allowHtmlString)
            {
                var owner = this._Owner;
                if (owner == null) return;

                var infoLabel = _InfoLabel;
                infoLabel.Text = infoText ?? "";

                if (infoLabel.AllowHtmlString != allowHtmlString) infoLabel.AllowHtmlString = allowHtmlString;

                var optimalSize = infoLabel.CalcBestSize();
                var minSize = owner.DragFormMinSize;
                var maxSize = owner.DragFormMaxSize;
                int w = optimalSize.Width + 10;
                w = (w < minSize.Width ? minSize.Width : (w > maxSize.Width ? maxSize.Width : w));
                int h = optimalSize.Height + 10;
                h = (h < minSize.Height ? minSize.Height : (h > maxSize.Height ? maxSize.Height : h));

                infoLabel.Size = new Size(w, h);
                this.Size = new Size(w, h);

                var formSize = this.Size;                  // Formulář si mohl upravit danou šířku / výšku, takže upravíme velikost labelu podle formuláře:
                if (formSize.Width > w || formSize.Height > h)
                    infoLabel.Size = formSize;

                _TitleText = titleText;
                _InfoText = infoText;
                _InfoTextEnableHtml = allowHtmlString;
            }
            /// <summary>
            /// Aktualizuje pozici okna Drag form
            /// </summary>
            /// <param name="position"></param>
            public void DragFormSetPosition(Point position)
            {
                if (this.Location != position)
                    this.Location = position;
                this._Position = position;
            }
            /// <summary>
            /// Nastaví barvy okna Drag form pro daný stav Enabled
            /// </summary>
            /// <param name="enabled"></param>
            private void DragFormSetEnabledColors(bool enabled)
            {
                var owner = this._Owner;
                if (owner == null) return;

                var label = _InfoLabel;
                label.Appearance.ForeColor = (enabled ? owner.DragFormTextColor : owner.DragFormTextColorDisabled);
                label.Appearance.BackColor = (enabled ? owner.DragFormBackColor1 : owner.DragFormBackColor1Disabled);
                label.Appearance.BackColor2 = (enabled ? owner.DragFormBackColor2 : owner.DragFormBackColor2Disabled);

                _IsEnabled = enabled;
            }
            /// <summary>
            /// Vlastník, obsahuje definici vzhledu okna
            /// </summary>
            private DxDragDrop _Owner;
            /// <summary>
            /// Label zobrazující informaci
            /// </summary>
            private DxLabelControl _InfoLabel;
            /// <summary>
            /// Pozice okna
            /// </summary>
            public Point Position { get { return _Position; } set { if (_Position != value) DragFormSetPosition(value); } }
            private Point _Position;
            /// <summary>
            /// Text titulku okna. 
            /// Aktuálně se nepoužívá.
            /// </summary>
            public string TitleText { get { return _TitleText; } set { if (!String.Equals(value, _TitleText)) DragFormUpdateContent(value, _InfoText, _InfoTextEnableHtml); } }
            private string _TitleText;
            /// <summary>
            /// Text informace okna.
            /// </summary>
            public string InfoText { get { return _InfoText; } set { if (!String.Equals(value, _InfoText)) DragFormUpdateContent(_TitleText, value, _InfoTextEnableHtml); } }
            private string _InfoText;
            /// <summary>
            /// Příznak, že <see cref="InfoText"/> může obsahovat HTML značky
            /// </summary>
            public bool InfoTextEnableHtml { get { return _InfoTextEnableHtml; } set { if (value != _InfoTextEnableHtml) DragFormUpdateContent(_TitleText, _InfoText, value); } }
            private bool _InfoTextEnableHtml;
            /// <summary>
            /// Vzhled okna podle možnosti Enabled procesu Drag and Drop (false = šedivé okno)
            /// </summary>
            public bool IsEnabled { get { return _IsEnabled; } set { if (value != _IsEnabled) DragFormSetEnabledColors(value); } }
            private bool _IsEnabled;
        }
        #endregion
    }
    #region interface IDxDragDropControl = Předpis pro prvek, který může být ZDROJEM anebo CÍLEM události Drag and Drop
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
        /// Povolené akce v daném controlu
        /// </summary>
        DxDragDropActionType DragDropActions { get; }
        /// <summary>
        /// Metoda volaná do objektu Source (zdroj Drag and Drop) při každé akci na straně zdroje.
        /// Předávaný argument <paramref name="args"/> je permanentní, dokud se myš pohybuje nad Source controlem nebo dokud probíhá Drag akce.
        /// </summary>
        /// <param name="args">Veškerá data o procesu Drag and Drop, permanentní po dobu výskytu myši nad Source objektem</param>
        void DoDragSource(DxDragDropArgs args);
        /// <summary>
        /// Metoda volaná do objektu Target (cíl Drag and Drop) při každé akci, pokud se myš nachází nad objektem který implementuje <see cref="IDxDragDropControl"/>.
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
    #endregion
    #region class DxDragDropArgs : Data předávaná v procesu Drag and Drop mezi prvkem Source a Target
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
            this.AllowedEffects = DragDropEffects.All | DragDropEffects.Link;
            this.SourceTextAllowHtml = true;
            this.SourceDragEnabled = true;
            this.TargetDropEnabled = false;
        }
        /// <summary>
        /// Reset
        /// </summary>
        private void Reset()
        {
            this.Event = DxDragDropEventType.None;
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
            this.TargetIndex = null;
            this.TargetTag = null;
            this.TargetDropEnabled = false;
            this.InsertIndex = null;
            this.LastDragEffect = DragDropEffects.None;
        }
        /// <summary>
        /// Stav procesu Drag and Drop = aktuální událost
        /// </summary>
        public DxDragDropEventType Event { get; private set; }
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
        /// Aktuální akce Drag
        /// </summary>
        public DragAction DragAction { get; set; }
        /// <summary>
        /// Byl stisknut Escape?
        /// </summary>
        public bool DragEscapePressed { get; private set; }
        /// <summary>
        /// Možnosti pro proces Drag (povolené efekty).
        /// Výchozí hodnota je <see cref="DragDropEffects.All"/> | <see cref="DragDropEffects.Link"/> = úplně všechny.
        /// Hodnota se převezme po události <see cref="DxDragDropEventType.DragStart"/> do procesu Drag, a následně je zde udržována.
        /// </summary>
        public DragDropEffects AllowedEffects { get; set; }
        /// <summary>
        /// Zvolený efekt pro aktuální krok, na vstupu do události je nastavena systémem, v události je možno ji upravit.
        /// </summary>
        public DragDropEffects CurrentEffect { get; set; }
        /// <summary>
        /// Source control předepisuje vlastní kurzor
        /// </summary>
        public bool DragUseDefaultCursors { get; set; }
        /// <summary>
        /// Jakýkoli objekt ze zdrojového controlu. Typicky to může být prvek ListBoxu nebo uzel TreeView, nebo ColumnHeader nebo cokoli jiného.
        /// </summary>
        public object SourceObject { get; set; }
        /// <summary>
        /// Libovolná data zdrojového objektu.
        /// </summary>
        public object SourceTag { get; set; }
        /// <summary>
        /// Text zobrazovaný v okně při Drag and Drop.
        /// Může obsahovat HTML Tagy typu DevExpress, viz <see cref="SourceTextAllowHtml"/>.
        /// </summary>
        public string SourceText { get; set; }
        /// <summary>
        /// Pokud obsahuje true (=default), pak text <see cref="SourceText"/> může obsahovat HTML Tagy typu DevExpress a text bude zobrazen formátovaný.
        /// Nastavte na false, pokud text <see cref="SourceText"/> obsahuje texty, které vypadají jako Tagy, ale ty mají být zobrazeny uživateli jako části textu.
        /// </summary>
        public bool SourceTextAllowHtml { get; set; }
        /// <summary>
        /// Volitelně obrázek zdrojového objektu.
        /// </summary>
        public Image SourceImage { get; set; }
        /// <summary>
        /// Cílový objekt přetahování = do kterého controlu ukazuje myš.
        /// </summary>
        public IDxDragDropControl TargetControl { get; private set; }
        /// <summary>
        /// Cílový objekt <see cref="TargetControl"/> nastavuje true / false podle toho, zda může akceptovat Drag and Drop.
        /// </summary>
        public bool TargetDropEnabled { get; set; }
        /// <summary>
        /// Index prvku v Target objektu, kam může být proveden Drop
        /// </summary>
        public IndexRatio TargetIndex { get; set; }
        /// <summary>
        /// Prostý index pro insert prvního prvku
        /// </summary>
        public int? InsertIndex { get; set; }
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
        #region Další podpora
        /// <summary>
        /// Obsahuje true v situaci, kdy <see cref="TargetControl"/> existuje. Může to být this, nebo jiný control, viz <see cref="TargetIsSource"/>.
        /// </summary>
        public bool TargetExists { get { return (this.TargetControl != null); } }
        /// <summary>
        /// Obsahuje true v situaci, kdy <see cref="TargetControl"/> existuje a je identický s <see cref="SourceControl"/>.
        /// Pak automaticky jde o přesun dat, nemělo by se jednat o kopii.
        /// </summary>
        public bool TargetIsSource { get { return (this.TargetExists && Object.ReferenceEquals(this.SourceControl, this.TargetControl)); } }
        /// <summary>
        /// Obsahuje doporučený efekt pro Drag and Drop na základě vztahu Zdroj a Cíl, a podle Modifier kláves.
        /// </summary>
        public DragDropEffects SuggestedDragDropEffect
        {
            get
            {
                DxDragDropActionType sourceActions = this.SourceControl?.DragDropActions ?? DxDragDropActionType.None;
                if (this.TargetIsSource)
                {
                    bool enableReorder = sourceActions.HasFlag(DxDragDropActionType.ReorderItems);
                    if (enableReorder) return DragDropEffects.Move;
                }
                else if (this.TargetExists)
                {
                    DxDragDropActionType targetActions = this.TargetControl?.DragDropActions ?? DxDragDropActionType.None;
                    bool targetEnableCopy = targetActions.HasFlag(DxDragDropActionType.ImportItemsInto);
                    if (targetEnableCopy)
                    {
                        bool sourceEnableCopy = sourceActions.HasFlag(DxDragDropActionType.CopyItemsFrom);
                        bool sourceEnableMove = sourceActions.HasFlag(DxDragDropActionType.MoveItemsFrom);
                        if (!sourceEnableCopy && !sourceEnableMove) return DragDropEffects.None;
                        if (sourceEnableCopy && !sourceEnableMove) return DragDropEffects.Copy;
                        if (!sourceEnableCopy && sourceEnableMove) return DragDropEffects.Move;
                        var modifier = this.ModifierKeys;
                        return (modifier == Keys.Control ? DragDropEffects.Move : DragDropEffects.Copy);
                    }
                }
                return DragDropEffects.None;
            }
        }
        #endregion
        #region Interní přístup k datům
        DxDragDropEventType IDxDragDropArgs.State { get { return this.Event; } set { this.Event = value; } }
        Point IDxDragDropArgs.ScreenMouseLocation { get { return this.ScreenMouseLocation; } set { this.ScreenMouseLocation = value; } }
        Point? IDxDragDropArgs.SourceMouseLocation { get { return this.SourceMouseLocation; } set { this.SourceMouseLocation = value; } }
        Keys IDxDragDropArgs.ModifierKeys { get { return this.ModifierKeys; } set { this.ModifierKeys = value; } }
        bool IDxDragDropArgs.DragEscapePressed { get { return this.DragEscapePressed; } set { this.DragEscapePressed = value; } }
        IDxDragDropControl IDxDragDropArgs.TargetControl { get { return this.TargetControl; } set { this.TargetControl = value; } }
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
        DxDragDropEventType State { get; set; }
        /// <summary>
        /// Absolutní souřadnice myši
        /// </summary>
        Point ScreenMouseLocation { get; set; }
        /// <summary>
        /// Lokální souřadnice myši v prostoru Source
        /// </summary>
        Point? SourceMouseLocation { get; set; }
        /// <summary>
        /// Aktuálně stisknuté klávesy Ctrl + Shift + Alt
        /// </summary>
        Keys ModifierKeys { get; set; }
        /// <summary>
        /// Byl stisknut Escape?
        /// </summary>
        bool DragEscapePressed { get; set; }
        /// <summary>
        /// Cílový control
        /// </summary>
        IDxDragDropControl TargetControl { get; set; }
        /// <summary>
        /// Posledně známý DragDrop efekt
        /// </summary>
        DragDropEffects LastDragEffect { get; set; }
        /// <summary>
        /// Vyvolá Reset argumentu = nulování dat do počátečního stavu
        /// </summary>
        void Reset();
    }
    /// <summary>
    /// Typy dostupných aktivit v procesu Drag and Drop
    /// </summary>
    [Flags]
    public enum DxDragDropActionType
    {
        /// <summary>
        /// Žádná aktivita
        /// </summary>
        None = 0,
        /// <summary>
        /// Přeuspořádat prvky v rámci jednoho controlu (např. přemístit prvky Listu nahoru/dolů)
        /// </summary>
        ReorderItems = 0x0001,
        /// <summary>
        /// Kopírovat prvky z this controlu (tento control slouží jako zdroj prvků)
        /// </summary>
        CopyItemsFrom = 0x0010,
        /// <summary>
        /// Přesouvat prvky z this controlu (tento control slouží jako zdroj prvků s možností odebrání prvků).
        /// Pokud je pro jeden control definováno současně <see cref="CopyItemsFrom"/> i <see cref="MoveItemsFrom"/>, pak konkrétní akci volí uživatel klávesou: Ctrl = Move, bez kláves = Copy
        /// </summary>
        MoveItemsFrom = 0x0020,
        /// <summary>
        /// Lze akceptovat prvky přesouvané odjinud do this controlu
        /// </summary>
        ImportItemsInto = 0x0080
    }
    /// <summary>
    /// Typ aktuální události v rámci procesu Drag and Drop, předávaný do Source i Target objektu.
    /// </summary>
    public enum DxDragDropEventType
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
        /// Tato událost se předává do Source objektu v okamžiku, kdy dochází k jakékoli akci, jako první událost.
        /// Tato událost se nepředává do Target objektu, v tuto chvíli ještě není určen.
        /// </summary>
        DragQuerySource,
        /// <summary>
        /// Tato událost se předává do Source objektu jako poslední událost v každém kroku.
        /// Source objekt může definovat vlastní kurzor.
        /// Tato událost se nepředává do Target objektu.
        /// </summary>
        DragGiveFeedbackSource,
        /// <summary>
        /// Tuto událost dostává Source i Target objekt (v tomto pořadí), poté kdy byl nalezen nějaký nový Target. 
        /// Událost je volána pouze jedenkrát při vstupu na Target.
        /// Source objekt tak může reagovat na případný Target.
        /// Každý z objektů může nastavit svůj příznak Enabled (<see cref="DxDragDropArgs.SourceDragEnabled"/> a <see cref="DxDragDropArgs.TargetDropEnabled"/>).
        /// </summary>
        DragEnterToTarget,
        /// <summary>
        /// Tuto událost dostává Target i Source objekt (v tomto pořadí), při každém pohybu myši nad Target objektem.
        /// Target objekt tak může reagovat na konkrétní pozici myši, a Source může reagovat na reakci Targetu 
        /// (protože Source jinak neví, co v prostoru Targetu znamená aktuální pozice myši).
        /// Každý z objektů může nastavit svůj příznak Enabled (<see cref="DxDragDropArgs.SourceDragEnabled"/> a <see cref="DxDragDropArgs.TargetDropEnabled"/>).
        /// </summary>
        DragMove,
        /// <summary>
        /// Tuto událost dostává Target i Source objekt (v tomto pořadí), při opuštění prostoru controlu Target.
        /// Oba objekty tak mohou reagovat, typicky úklidem svých proměnných používaných v události <see cref="DragMove"/>.
        /// </summary>
        DragLeaveOfTarget,
        /// <summary>
        /// Tato událost se předává do Source i do Target objektu (v tomto pořadí), v okamžiku, kdy Source i Target objekt deklarují Enabled = true a uživatel pustil myš.
        /// Právě nyní se má provést požadovaná akce.
        /// </summary>
        DragDropAccept,
        /// <summary>
        /// Tato událost se předává do Source i do Target objektu (v tomto pořadí), v okamžiku, kdy proces Drag and Drop je zrušen = 
        /// myš je uvolněna a Target je null nebo některý Enabled je false.
        /// </summary>
        DragCancel,
        /// <summary>
        /// Tato událost se předává do Source i do Target objektu (v tomto pořadí), v okamžiku, kdy proces Drag and Drop je ukončen 
        /// (tedy po <see cref="DragDropAccept"/> i po <see cref="DragCancel"/>) jako závěrečná událost.
        /// </summary>
        DragEnd
    }
    #endregion
    #region class IndexRatio : třída, pomáhající určit index prvku podle pozice myši, včetně vyhodnocení relativní pozice v rámci prvku a podkladu pro Scroll
    /// <summary>
    /// <see cref="IndexRatio"/> : třída, pomáhající určit index prvku podle pozice myši, včetně vyhodnocení relativní pozice v rámci prvku a podkladu pro Scroll
    /// </summary>
    public class IndexRatio
    {
        #region Konstruktor, tvorba, IsEquals
        /// <summary>
        /// Vypočítá a vrátí new instanci <see cref="IndexRatio"/> pro dané souřadnice, funkce a další parametry
        /// </summary>
        /// <param name="point">Souřadnice myši v prostoru klienta</param>
        /// <param name="clientBounds">Souřadnice klienta</param>
        /// <param name="indexSearch">Metoda, která pro daný bod vyhledá index prvku (v dané orientaci).</param>
        /// <param name="boundsSearch">Metoda, která pro daný index vrátí souřadnice prvku</param>
        /// <param name="count">Počet prvků celkem</param>
        /// <param name="orientation">Orientaců prvků, běžně <see cref="Orientation.Vertical"/> pro List</param>
        /// <returns></returns>
        public static IndexRatio Create(Point point, Rectangle clientBounds, Func<Point, int> indexSearch, Func<int, Rectangle?> boundsSearch, int? count = null, Orientation orientation = Orientation.Vertical)
        {
            IndexRatio indexRatio = new IndexRatio();
            indexRatio.Orientation = orientation;
            indexRatio.Point = point;
            indexRatio.SearchItem(clientBounds, indexSearch, boundsSearch, count);
            indexRatio.CalculateRatio();
            indexRatio.ScrollSuggestion = GetScrollSuggestion(orientation, clientBounds, point);
            return indexRatio;
        }
        /// <summary>
        /// Privátní konstruktor
        /// </summary>
        private IndexRatio() { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            float index = (float)Index + Ratio;
            return index.ToString();
        }
        /// <summary>
        /// Metoda vyhledá index aktivního prvku (prvek pod myší) s pomocí dodaných hledacích metod.
        /// </summary>
        /// <param name="clientBounds"></param>
        /// <param name="indexSearch"></param>
        /// <param name="boundsSearch"></param>
        /// <param name="count"></param>
        private void SearchItem(Rectangle clientBounds, Func<Point, int> indexSearch, Func<int, Rectangle?> boundsSearch, int? count)
        {
            int index = indexSearch(Point);
            IndexRatioPositionType position = (index >= 0 ? IndexRatioPositionType.OnItem : IndexRatioPositionType.None);

            if (index < 0)
            {   // Na dané souřadnici není k nalezení žádný prvek.
                // Pokud je 'aktivní' souřadnice (tj. Y pro Vertical, X pro Horizontal) blízko k počátku,
                //  zkusíme tuto souřadnici navýšit (jsme nahoře nebo vlevo těsně před prvním viditelným prvkem):
                index = SearchItemBegin(clientBounds, indexSearch, boundsSearch, count, 5);
                if (index >= 0)
                    position = IndexRatioPositionType.BeforeItem;
            }

            if (index < 0 && count.HasValue && count.Value > 0)
            {   // Na dané souřadnici není k nalezení žádný prvek, a ani nejsme u začátku prvku.
                // Možná zkusíme najít souřadnici posledního prvku, a pokud myš (Point) je skutečně za posledním prvkem, pak akceptujeme tento poslední prvek jako aktivní:
                int lastIndex = count.Value - 1;
                var lastBounds = boundsSearch(lastIndex);
                if (lastBounds.HasValue)
                {
                    if (IsVertical && Point.Y >= lastBounds.Value.Bottom)
                        index = lastIndex;
                    else if (IsHorizontal && Point.X >= lastBounds.Value.Right)
                        index = lastIndex;

                    if (index >= 0)
                        position = IndexRatioPositionType.AfterItem;
                }
            }
            Index = index;
            Position = position;
            Bounds = boundsSearch(Index);
        }
        /// <summary>
        /// Zkusí najít index prvku (pomocí metody <paramref name="indexSearch"/>) poblíž počátku controlu.
        /// Řeší tak situaci, kdy myš je na horním/levém okraji controlu, kde se ještě nenachází první viditelný prvek (např. v oblasti Border nebo Margin),
        /// ale po posunutí souřadnice aktivního bodu (<see cref="Point"/>) najdeme viditelný prvek.
        /// </summary>
        /// <param name="clientBounds"></param>
        /// <param name="indexSearch"></param>
        /// <param name="boundsSearch"></param>
        /// <param name="count"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        private int SearchItemBegin(Rectangle clientBounds, Func<Point, int> indexSearch, Func<int, Rectangle?> boundsSearch, int? count, int margin)
        {
            // Pokud (v aktuální orientaci) je pozice myši vzdálena více než (margin) od počátku controlu, pak zdejší metoda nemá nic hledat:
            if ((IsVertical && Point.Y > (clientBounds.Y + margin)) || (IsHorizontal && Point.X <= (clientBounds.X + margin))) return -1;

            int index = -1;
            for (int s = 1; s <= margin; s++)
            {
                Point point = (IsVertical ? new Point(Point.X, Point.Y + s) : new Point(Point.Y, Point.X + s));
                index = indexSearch(Point);
                if (index >= 0) break;
            }
            return index;
        }
        /// <summary>
        /// Na základě uložených hodnot <see cref="Orientation"/>, <see cref="Bounds"/> a <see cref="Point"/>
        /// vypočte a uloží hodnoty určující <see cref="Ratio"/> (tedy: <see cref="RatioA"/> a <see cref="RatioT"/>).
        /// </summary>
        private void CalculateRatio()
        {
            if (!Bounds.HasValue)
            {
                RatioA = 0;
                RatioT = 0;
            }
            else if (IsVertical)
            {
                RatioA = Point.Y - Bounds.Value.Y;
                RatioT = Bounds.Value.Height;
            }
            else
            {
                RatioA = Point.X - Bounds.Value.X;
                RatioT = Bounds.Value.Width;
            }
            if (RatioA > RatioT) RatioA = RatioT;
            if (RatioA < 0) RatioA = 0;
        }
        /// <summary>
        /// Orientace je Horizontal
        /// </summary>
        private bool IsHorizontal { get { return this.Orientation == Orientation.Horizontal; } }
        /// <summary>
        /// Orientace je Vertical
        /// </summary>
        private bool IsVertical { get { return this.Orientation == Orientation.Vertical; } }
        /// <summary>
        /// Vrátí true, pokud dané dvě instance jsou si rovny v obsahu (kontroluje se shodnost null, 
        /// a shodnost hodnot <see cref="Index"/>, <see cref="Ratio"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool IsEqual(IndexRatio a, IndexRatio b)
        {
            bool an = a is null;
            bool bn = b is null;
            if (an && bn) return true;           // Oba jsou null
            if (an != bn) return false;          // Jen jeden je null
            return (a.Index == b.Index && a.RatioA == b.RatioA && a.RatioT == b.RatioT);
        }
        #endregion
        #region GetScrollSuggestion()
        /// <summary>
        /// Vrátí doporučenou hodnotu AutoScroll pro danou orientaci, oblast klienta a pozici myši
        /// </summary>
        /// <param name="orientation"></param>
        /// <param name="clientBounds"></param>
        /// <param name="point"></param>
        /// <param name="scrollZone"></param>
        /// <param name="suggestionMin"></param>
        /// <param name="suggestionMax"></param>
        /// <returns></returns>
        public static int GetScrollSuggestion(Orientation orientation, Rectangle clientBounds, Point point, int scrollZone = 50, int suggestionMin = 4, int suggestionMax = 100)
        {
            if (orientation == Orientation.Vertical)
                return GetScrollSuggestion(clientBounds.Y, clientBounds.Bottom, point.Y);
            else
                return GetScrollSuggestion(clientBounds.X, clientBounds.Right, point.X);
        }
        /// <summary>
        /// Vrátí doporučenou hodnotu AutoScroll
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="position"></param>
        /// <param name="scrollZone"></param>
        /// <param name="suggestionMin"></param>
        /// <param name="suggestionMax"></param>
        /// <returns></returns>
        private static int GetScrollSuggestion(int begin, int end, int position, int scrollZone = 50, int suggestionMin = 4, int suggestionMax = 100)
        {
            int scrollZoneMax = (end - begin) / 4;                        // Pro Scroll vyhrazujeme nanejvýše první a poslední čtvrtinu.
            if (scrollZoneMax < 10) return 0;                             // Pokud je prvek tak malý (end - begin je menší než 40px), pak autoscroll neprovádím.
            if (scrollZone > scrollZoneMax) scrollZone = scrollZoneMax;   // Pokud by byl prvek 100px, pak pro Scroll oblast nemůže být větší než 25px.

            int scrollBegin = begin + scrollZone;
            if (position < scrollBegin) return GetScrollSuggestionOne(-1, scrollBegin - position, scrollZone, suggestionMin, suggestionMax);
            int scrollEnd = end - scrollZone;
            if (position > scrollEnd) return GetScrollSuggestionOne(1, position - scrollEnd, scrollZone, suggestionMin, suggestionMax);
            return 0;
        }
        /// <summary>
        /// Vrátí doporučenou hodnotu AutoScroll
        /// </summary>
        /// <param name="coefficient"></param>
        /// <param name="distance"></param>
        /// <param name="scrollZone"></param>
        /// <param name="suggestionMin"></param>
        /// <param name="suggestionMax"></param>
        /// <returns></returns>
        private static int GetScrollSuggestionOne(int coefficient, int distance, int scrollZone, int suggestionMin, int suggestionMax)
        {
            if (scrollZone <= 0) return 0;                                // Velikost oblasti, kde probíhá Scroll
            float ratio = (float)distance / (float)scrollZone;            // Relativní vzdálenost ku celé oblasti
            ratio = (ratio < 0f ? 0f : (ratio > 1f ? 1f : ratio));        // Do rozmezí 0-1
            int suggestion = suggestionMin + (int)(Math.Round(ratio * (float)(suggestionMax - suggestionMin), 0));
            return suggestion;
        }
        #endregion
        #region Podpora pro DragDrop a pro vykreslení
        /// <summary>
        /// Určí INSERT INDEX = na který index se mají insertovat prvky Drag and Drop tak, aby odpovídaly záměru uživatele.
        /// Insert index je budoucí (=výsledná) pozice (=index) prvního prvku, který bude insertován.
        /// <para/>
        /// Volitelně lze předat kolekci indexů prvků <paramref name="removeIndexes"/>, které se budou odebírat. Využije se při REORDER prvků.
        /// Mějme List obsahující prvky 0 až 9, z nichž prvky 2 a 3 přesouváme na jinou pozici pomocí Drag and Drop.
        /// Uživatel vybral jako cíl přesunu dolní polovinu prvku na indexu 6, chce tedy prvky 2 a 3 přemístit tak, aby prvek 2 byl za prvkem 6.
        /// Tato metoda se volá v době, kdy List obsahuje všechny prvky, protože v té době platí pozice a indexy zde uložené.
        /// Následovat bude odebrání prvků 2 a 3 (Items.Remove), a poté bude následovat Insert prvků na zde určený Insert index.
        /// Znamená to tedy, že Insert index musí ukazovat na bývalý prvek 7 (za prvkem 6), ale snížený o dva prvky, které jsou z pole odebrány (2 a 3).
        /// </summary>
        /// <param name="removeIndexes"></param>
        /// <returns></returns>
        public int? GetInsertIndex(IEnumerable<int> removeIndexes = null)
        {
            int insertIndex = this.Index;                  // Výchozí index = prvek, kam ukazuje myš. Pokud myš ukazuje na horní polovinu prvku, bude se insertovat na tento index.
            bool isAfter = (this.Ratio >= 0.5f);           // Pokud myš ukazuje na dolní polovinu prvku,
            if (isAfter) insertIndex++;                    //  bude se insertovat až za tento prvek = +1.
            if (removeIndexes != null)                     // Pokud jsou dány index prvků, které se budou odebírat:
            {
                int countBefore = (removeIndexes.Count(index => index < insertIndex));   // Tolik prvků, ležících před výchozí Insert pozicí, budeme odebírat
                insertIndex -= countBefore;
            }
            return insertIndex;
        }
        /// <summary>
        /// Metoda vrátí souřadnice pro linku, která reprezentuje prostor pro případný DragDrop
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public Rectangle? GetMarkLineBounds(int size = 2)
        {
            if (!this.Bounds.HasValue) return null;
            size = (size < 1 ? 1 : (size > 12 ? 12 : size));
            int offset = size / 2;
            var bounds = this.Bounds.Value;
            float ratio = Ratio;
            if (IsVertical)
            {
                int y = (ratio < 0.5f ? bounds.Y : bounds.Bottom);
                return new Rectangle(bounds.X, y - offset, bounds.Width, size);
            }
            else
            {
                int x = (ratio < 0.5f ? bounds.X : bounds.Right);
                return new Rectangle(x - offset, bounds.Y, size, bounds.Height);
            }
        }
        #endregion
        #region Instanční properties
        /// <summary>
        /// Orientace tohoto indexu, defaultní je <see cref="Orientation.Vertical"/>
        /// </summary>
        public Orientation Orientation { get; private set; }
        /// <summary>
        /// Souřadnice myši
        /// </summary>
        public Point Point { get; private set; }
        /// <summary>
        /// Index prvku, nad kterým se pohybuje myš
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// Pozice myši vzhledem k poli prvků
        /// </summary>
        public IndexRatioPositionType Position { get; private set; }
        /// <summary>
        /// Souřadnice prvku, nad kterým se pohybuje myš (smí být null)
        /// </summary>
        public Rectangle? Bounds { get; private set; }
        /// <summary>
        /// Podklad pro Ratio: počet pixelů pozice myši od počátku objektu
        /// </summary>
        private int RatioA;
        /// <summary>
        /// Podklad pro Ratio: počet pixelů celkem (Height nebo Width)
        /// </summary>
        private int RatioT;
        /// <summary>
        /// Poměr pozice myši k výšce prvku: 0.0 = na horním pixelu, 0.5 = uprostřed, 1.0 = na dolním pixelu. Může být i mimo rozsah 0 až 1.
        /// </summary>
        public float Ratio { get { return (RatioT <= 0 ? 0f : ((float)RatioA / (float)RatioT)); } }
        /// <summary>
        /// Doporučení pro Scroll:
        /// 0 = není třeba, hodnota větší nebo menší než 1 = je vhodno scrollovat tolik pixelů za 20ms (hodnota je v rozmezí 4 - 100).
        /// Záporná hodnota chce scrollovat k začátku (myš je nahoře/vlevo), kladná hodnota ke konci (myš je dole/vpravo).
        /// Hodnota závisí na vzdálenosti myši od okraje prostoru controlu: čím blíže k okraji, tím rychlejší scroll.
        /// Pokud je myš od okraje controlu vzdálena více než 50px, bude <see cref="ScrollSuggestion"/> = 0.
        /// </summary>
        public int ScrollSuggestion { get; private set; }
        #endregion
    }
    /// <summary>
    /// Umístění indexu vzhledem k prvkům
    /// </summary>
    public enum IndexRatioPositionType
    {
        /// <summary>Neurčeno</summary>
        None,
        /// <summary>Před prvky</summary>
        BeforeItem,
        /// <summary>V prostoru prvků</summary>
        OnItem,
        /// <summary>Za prvky</summary>
        AfterItem
    }
    #endregion
}
