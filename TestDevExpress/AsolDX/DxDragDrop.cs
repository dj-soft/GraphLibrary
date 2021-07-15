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
    /// - Control (zde <see cref="DxTreeViewListNative"/>) naimplementuje <see cref="IDxDragDropSource"/> a v konstruktoru si vytvoří instanci controlleru <see cref="DxDragDrop"/>
    /// </summary>
    public class DxDragDrop : IDisposable
    {
        #region Konstruktor, proměnné, Dispose, primární eventhandlery ze zdroje
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="source"></param>
        public DxDragDrop(IDxDragDropSource source)
        {
            _Source = source;
            DragButtons = MouseButtons.Left | MouseButtons.Right;
            DoDragReset();
            if (source != null)
            {
                source.MouseEnter += Source_MouseEnter;
                source.MouseLeave += Source_MouseLeave;
                source.MouseMove += Source_MouseMove;
                source.MouseDown += Source_MouseDown;
                source.MouseUp += Source_MouseUp;
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
            var source = _Source;
            if (source != null)
            {
                source.MouseEnter -= Source_MouseEnter;
                source.MouseLeave -= Source_MouseLeave;
                source.MouseMove -= Source_MouseMove;
                source.MouseDown -= Source_MouseDown;
                source.MouseUp -= Source_MouseUp;
            }
            DoDragReset();
            _Source = null;
        }
        /// <summary>
        /// Zdroj události DragDrop
        /// </summary>
        private IDxDragDropSource _Source;
        /// <summary>
        /// Aktuální souřadnice myši relativně v prostoru controlu <see cref="_Source"/>
        /// </summary>
        private Point? _SourceCurrentPoint
        {
            get
            {
                Point screenLocation = Control.MousePosition;
                var source = _Source;
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
                    DoTargetDragRun();
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
                    DoSourceDragCancel(e.Location);
                    break;
                case DragStateType.DownDrag:
                    if (!this.DragArgs.IsDragDropEnabled)
                        DoSourceDragCancel(e.Location);
                    else
                        DoSourceDragDrop(e.Location);
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
            IDragArgs.ModifierKeys = Control.ModifierKeys;
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
            _SourceStartPoint = sourcePoint;
            _SourceStartBounds = sourcePoint.CreateRectangleFromCenter(this._SourceStartSize);
            _State = DragStateType.DownWait;
            IDragArgs.ModifierKeys = Control.ModifierKeys;
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
            _SourceStartBounds = null;
            _State = DragStateType.DownDrag;
            IDragArgs.ModifierKeys = Control.ModifierKeys;
            IDragArgs.SourceMouseLocation = _SourceStartPoint ?? sourcePoint;
            DoSourceCall(DxDragDropState.DragStart);
            DoTargetDragRun();
        }
        /// <summary>
        /// Volá se v procesu pohybu stisknuté myši mimo prostor Start = provádí se Drag
        /// </summary>
        private void DoTargetDragRun()
        {
            Point screenTargetPoint = Control.MousePosition;

            IDragArgs.ModifierKeys = Control.ModifierKeys;
            IDragArgs.ScreenMouseLocation = screenTargetPoint;
            TrySearchForTarget(screenTargetPoint, out IDxDragDropTarget target);
            DoTargetChange(target);
            DoSourceCall(DxDragDropState.DragMove);
            DoTargetCall(DxDragDropState.DragMove);
        }
        /// <summary>
        /// Volá se tehdy, když končí Drag and Drop jinak než předáním zdroje do cíle = cancel
        /// </summary>
        /// <param name="sourcePoint"></param>
        private void DoSourceDragCancel(Point sourcePoint)
        {
            IDragArgs.ModifierKeys = Control.ModifierKeys;
            IDragArgs.ScreenMouseLocation = Control.MousePosition;
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
            IDragArgs.ModifierKeys = Control.ModifierKeys;
            IDragArgs.ScreenMouseLocation = Control.MousePosition;
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
            _Source.DoDragSource(DragArgs);
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
        /// Najde cílový control na dané souřadnici
        /// </summary>
        /// <param name="screenTargetPoint"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private bool TrySearchForTarget(Point screenTargetPoint, out IDxDragDropTarget target)
        {
            target = null;
            Form form = _Source.FindForm();
            if (form != null)
            {
                if (form.TryGetChildAtPoint(screenTargetPoint, GetChildAtPointSkip.Invisible, out Control child) && child.TrySearchUpForControl<IDxDragDropTarget>(c => c is IDxDragDropTarget, c => c as IDxDragDropTarget, out target))
                    return true;
            }
            return false;
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
                    _DragArgs = new DxDragDropArgs(_Source);
                return _DragArgs;
            }
        }
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
    /// Předpis pro prvek, který může být ZDROJEM události Drag and Drop = v něm může být zahájeno přetažení prvku jinam (do <see cref="IDxDragDropTarget"/>).
    /// </summary>
    public interface IDxDragDropSource
    {
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
    /// Předpis pro prvek, který může být CÍLEM události Drag and Drop = do něj může být přetažen prvek <see cref="IDxDragDropSource"/> nebo jeho součást.
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
        public DxDragDropArgs(IDxDragDropSource sourceControl)
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
        public IDxDragDropSource SourceControl { get; private set; }
        /// <summary>
        /// Zdrojový control nastavuje true / false podle toho, zda aktuální výchozí prvek může být přemístěn.
        /// Typicky ListBox nastaví true, pokud myš uchopila (v DragStart) existující Item v ListBoxu, nebo v TreeView myš uchopila TreeNode.
        /// Pokud uživatel zkusí přetahovat prázdný prostor v TreeNode, pak sem zdroj nastaví false a přetahování nemá smysl.
        /// </summary>
        public bool SourceDragEnabled { get; set; }
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
        #endregion
        #region Interní přístup k datům
        DxDragDropState IDxDragDropArgs.State { get { return this.State; } set { this.State = value; } }
        Point IDxDragDropArgs.ScreenMouseLocation { get { return this.ScreenMouseLocation; } set { this.ScreenMouseLocation = value; } }
        Point? IDxDragDropArgs.SourceMouseLocation { get { return this.SourceMouseLocation; } set { this.SourceMouseLocation = value; } }
        Keys IDxDragDropArgs.ModifierKeys { get { return this.ModifierKeys; } set { this.ModifierKeys = value; } }
        IDxDragDropTarget IDxDragDropArgs.TargetControl { get { return this.TargetControl; } set { this.TargetControl = value; } }
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
