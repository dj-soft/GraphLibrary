﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Asol.Tools.WorkScheduler.Data;
using System.Drawing.Drawing2D;

namespace Asol.Tools.WorkScheduler.Components
{
    #region interface IInteractiveItem a IInteractiveParent
    /// <summary>
    /// Definuje vlastnosti třídy, která může být interaktivní a vykreslovaná v InteractiveControlu
    /// </summary>
    public interface IInteractiveItem : IInteractiveParent
    {
        /// <summary>
        /// Relativní souřadnice this prvku v rámci parenta.
        /// Toto jsou souřadnice objektu ve Standardní vrstvě.
        /// Pokud je objekt přetahován na jiné místo, pak tyto souřadnice jsou v <see cref="BoundsInteractive"/>.
        /// Absolutní souřadnice mohou být určeny pomocí třídy <see cref="BoundsInfo"/>.
        /// </summary>
        Rectangle Bounds { get; set; }
        /// <summary>
        /// Relativní souřadnice this prvku v rámci parenta.
        /// Toto jsou souřadnice objektu v Interaktivní vrstvě v době procesu Drag and Drop.
        /// </summary>
        Rectangle? BoundsInteractive { get; }
        /// <summary>
        /// Přídavek k this.Bounds, který určuje přesah aktivity tohoto prvku do jeho okolí.
        /// <para/>
        /// Kladné hodnoty v Padding zvětšují aktivní plochu nad rámec this.Bounds, záporné aktivní plochu zmenšují.
        /// <para/>
        /// Aktivní souřadnice prvku tedy jsou this.Bounds.Add(this.ActivePadding), kde Add() je extension metoda.
        /// </summary>
        Padding? InteractivePadding { get; set; }
        /// <summary>
        /// Okraje (dovnitř this prvku od <see cref="Bounds"/>), uvnitř těchto okrajů se nachází prostor pro klientské prvky.
        /// </summary>
        Padding? ClientBorder { get; set; }
        /// <summary>
        /// Souhrn veškerých vlastností a stylu tohoto prvku.
        /// </summary>
        InteractiveProperties Is { get; }
        /// <summary>
        /// Order for this item
        /// </summary>
        ZOrder ZOrder { get; }
        /// <summary>
        /// Pořadí při procházení pomocí Tab.
        /// Null = použije se nativní pořadí v poli <see cref="IInteractiveParent.Childs"/>.
        /// Kombinace prvků s Null a s hodnotou: nejprve se projdou pvky s hodnotou, a po nich prvky s null v nativním pořadí.
        /// </summary>
        int? TabOrder { get; }
        /// <summary>
        /// Libovolný popisný údaj
        /// </summary>
        object Tag { get; set; }
        /// <summary>
        /// Libovolný datový údaj
        /// </summary>
        object UserData { get; set; }
        /// <summary>
        /// Obsahuje vrstvy, do nichž se tento objekt kreslí standardně.
        /// Tuto hodnotu vloží metoda <see cref="InteractiveObject.Repaint()"/> do <see cref="RepaintToLayers"/>.
        /// Vrstva <see cref="GInteractiveDrawLayer.None"/> není vykreslována (objekt je tedy vždy neviditelný), ale na rozdíl od <see cref="InteractiveProperties.Visible"/> je takový objekt interaktivní.
        /// </summary>
        GInteractiveDrawLayer StandardDrawToLayer { get; }
        /// <summary>
        /// Obsahuje všechny vrstvy grafiky, do kterých chce být tento prvek právě nyní (při nejbližším kreslení) vykreslován.
        /// Po vykreslení je nastaveno na <see cref="GInteractiveDrawLayer.None"/>.
        /// </summary>
        GInteractiveDrawLayer RepaintToLayers { get; set; }
        /// <summary>
        /// Vrátí true, pokud daný prvek je aktivní na dané souřadnici.
        /// Souřadnice je v koordinátech Parenta prvku, je tedy srovnatelná s <see cref="IInteractiveItem.Bounds"/>.
        /// Pokud prvek má nepravidelný tvar, musí testovat tento tvar v této své metodě explicitně.
        /// </summary>
        /// <param name="relativePoint">Bod, který testujeme, v koordinátech srovnatelných s <see cref="IInteractiveItem.Bounds"/></param>
        /// <returns></returns>
        Boolean IsActiveAtPoint(Point relativePoint);
        /// <summary>
        /// Tato metoda je volaná po každé interaktivní změně na prvku.
        /// </summary>
        /// <param name="e"></param>
        void AfterStateChanged(GInteractiveChangeStateArgs e);
        /// <summary>
        /// Aktuální stav interaktivity
        /// </summary>
        GInteractiveState InteractiveState { get; }
        /// <summary>
        /// Tato metoda je volaná postupně pro jednotlivé fáze akce Drag and Drop.
        /// </summary>
        /// <param name="e"></param>
        void DragAction(GDragActionArgs e);
        /// <summary>
        /// Metoda je volaná pro vykreslení obsahu tohoto prvku.
        /// Pokud prvek má nějaké potomstvo (Childs), pak se this prvek nestará o jejich vykreslení, to zajistí jádro.
        /// Jádro detekuje naše <see cref="IInteractiveParent.Childs"/>, a postupně volá jejich vykreslení (od prvního po poslední).
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds);
        /// <summary>
        /// Pokud je zde true, pak v procesu kreslení prvku je po standardním vykreslení this prvku <see cref="Draw(GInteractiveDrawArgs, Rectangle, Rectangle)"/> 
        /// a po standardním vykreslení všech <see cref="IInteractiveParent.Childs"/> prvků ještě vyvolána metoda <see cref="DrawOverChilds(GInteractiveDrawArgs, Rectangle, Rectangle)"/> pro this prvek.
        /// </summary>
        bool NeedDrawOverChilds { get; }
        /// <summary>
        /// Tato metoda je volaná pro prvek, který má nastaveno <see cref="NeedDrawOverChilds"/> == true, poté když tento prvek byl vykreslen, a následně byly vykresleny jeho <see cref="IInteractiveParent.Childs"/>.
        /// Umožňuje tedy kreslit "nad" svoje <see cref="IInteractiveParent.Childs"/> (tj. počmárat je).
        /// Tento postup se používá typicky jen pro zobrazení překryvného textu přes <see cref="IInteractiveParent.Childs"/> prvky, které svůj text nenesou.
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        void DrawOverChilds(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds);
        /// <summary>
        /// Do této property je vkládáno true po výběru prvku, a false po zrušení výběru.
        /// </summary>
        Boolean IsSelected { get; set; }
        /// <summary>
        /// Je aktuálně zarámován (pro budoucí selectování)?
        /// Zarámovaný prvek (v procesu hromadného označování myší SelectFrame) má <see cref="IsFramed"/> = true, ale hodnotu <see cref="IsSelected"/> má beze změn.
        /// Teprve na konci procesu SelectFrame se pro dotčené objekty (které mají <see cref="IsFramed"/> = true) nastaví i <see cref="IsSelected"/> = true.
        /// </summary>
        Boolean IsFramed { get; set; }
        /// <summary>
        /// Je prvek "Aktivován" (nějakou aplikační akcí)?
        /// Aktivovaný prvek není ani Selectovaný, ani Framovaný. Změna <see cref="IsSelected"/> okolních prvků nijak nezmění <see cref="IsActivated"/>.
        /// Aktivovaný prvek se v podstatě permanentně zobrazuje jako výraznější než okolní prvky, například proto, že je problematický nebo odpovídá nějakému jinému zadání.
        /// </summary>
        Boolean IsActivated { get; set; }
    }
    /// <summary>
    /// Interface definuje členy pro typ, který je parentem interaktivního prvků.
    /// Tím může být jak jiný interaktivní prvek, anebo základní interaktivní Control.
    /// </summary>
    public interface IInteractiveParent
    {
        /// <summary>
        /// Jednoznačné ID prvku. Je přiřazeno v konstruktoru a nemění se po celý život instance.
        /// </summary>
        UInt32 Id { get; }
        /// <summary>
        /// Reference na Hosta, což je GrandParent všech prvků.
        /// </summary>
        InteractiveControl Host { get; }
        /// <summary>
        /// Parent tohoto prvku. Může to být i přímo control InteractiveControl.
        /// Pouze v případě, kdy this je <see cref="InteractiveControl"/>, pak <see cref="Parent"/> je null.
        /// </summary>
        IInteractiveParent Parent { get; set; }
        /// <summary>
        /// Pole mých vlastních potomků. Jejich Parentem je this.
        /// Jejich souřadnice jsou relativní ke zdejšímu souřadnému systému.
        /// </summary>
        IEnumerable<IInteractiveItem> Childs { get; }
        /// <summary>
        /// Velikost prostoru pro Childs prvky
        /// </summary>
        Size ClientSize { get; }
        /// <summary>
        /// Zajistí vykreslení sebe a svých Childs, volitelně i svého parenta, do svých standardních vrstev
        /// </summary>
        void Repaint();
        /// <summary>
        /// Zajistí vykreslení sebe a svých Childs, volitelně i svého parenta, do dané vrstvy
        /// </summary>
        /// <param name="repaintLayers">Dané vrstvy ke kreslení</param>
        void Repaint(GInteractiveDrawLayer repaintLayers);
    }
    /// <summary>
    /// Interface definuje specifické prvky hostitelského Controlu, k nimž není přímý přístup přes třídu InteractiveControl
    /// </summary>
    public interface IInteractiveHost
    {
        /// <summary>
        /// Prvek, který má aktuálně focus. Je nastaven po doběhnutí událostí <see cref="GInteractiveChangeState.KeyboardFocusEnter"/> a <see cref="GInteractiveChangeState.MouseEnter"/>
        /// </summary>
        IInteractiveItem FocusedItem { get; set; }
        /// <summary>
        /// Prvek, který měl focus nedávno. Je nastaven po doběhnutí událostí <see cref="GInteractiveChangeState.KeyboardFocusLeave"/> a <see cref="GInteractiveChangeState.MouseLeave"/>, 
        /// v době kdy je do <see cref="IInteractiveHost.FocusedItem"/> vkládáno null.
        /// Slouží např. k obnově focusu při vrácení, nebo k určení, odkud přešel focus do určitého nového prvku.
        /// </summary>
        IInteractiveItem FocusedItemPrevious { get; set; }
        /// <summary>
        /// Zajistí přechod focusu od aktuálního prvku (pokud je) do daného prvku.
        /// </summary>
        /// <param name="focusedItem"></param>
        void SetFocusToItem(IInteractiveItem focusedItem);
        /// <summary>
        /// Zajistí zobrazení daného prvku v AutoScroll containeru
        /// </summary>
        /// <param name="item"></param>
        void SetAutoScrollContainerToItem(IInteractiveItem item);
    }
    /// <summary>
    /// Interface, který umožní child prvku číst a měnit rozměry některého svého hostitele.
    /// Technicky to nemusí být jeho přímý Parent ve smyslu vztahu Parent - Child, může to být i Parent jeho Parenta.
    /// Praktické využití je mezi grafem umístěným v buňce Gridu, kde jeho IVisualParent může být řádek (a nikoli buňka).
    /// Interface pak dovoluje grafu požádat o změnu výšky řádku (setováním <see cref="IVisualParent.ClientHeight"/>, 
    /// kdy na to řádek grafu reaguje nastavením své výšky podle svých pravidel (minimální a maximální povolená výška řádku).
    /// Následně si graf načte zpět výšku parenta (již s korekcemi) a této výšce přizpůsobí svoje vnitřní souřadnice.
    /// </summary>
    public interface IVisualParent
    {
        /// <summary>
        /// Šířka klientského prosturu uvnitř parenta
        /// </summary>
        int ClientWidth { get; set; }
        /// <summary>
        /// Výška klientského prosturu uvnitř parenta
        /// </summary>
        int ClientHeight { get; set; }
    }
    #endregion
    #region interface IDrawItem
    /// <summary>
    /// Interface, definující přítomnost metody, která zajistí vykreslení obsahu libovolného prvku.
    /// </summary>
    public interface IDrawItem
    {
        /// <summary>
        /// Metoda pro vykreslení obsahu prvku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="boundsAbsolute"></param>
        void Draw(GInteractiveDrawArgs e, Rectangle boundsAbsolute);
    }
    #endregion
    #region class InteractiveProperties : Bit storage for Interactive Properties of IInteractiveItem objects.
    /// <summary>
    /// InteractiveProperties : Bitové úložiště pro různé Boolean Properties pro IInteractiveItem objekt.
    /// Hodnoty bitů lze setovat i číst.
    /// Čtení i ukládání hodnoty lze převést na dynamické = lze zadat přiměřenou metodu, 
    /// která konkrétní hodnotu pro konkrétní prvek vrátí/uloží odjinud, než ze statického úložiště.
    /// Tak například pro konkrétní typ prvku lze hodnotu <see cref="Selectable"/> číst (nebo i zkombinovat) z jejích vnitřních dat tak, 
    /// že se do objektu <see cref="InteractiveProperties"/> zadá metoda <see cref="GetSelectable"/>, pak tato metoda vrací hodnotu true/false
    /// podle stavu konkrétního objektu. Metody typu Get* vždy dostávají jeden parametr <see cref="Boolean"/> = hodnotu uloženou ve statickém bitovém úložišti.
    /// Konkrétní metody Get* tuto hodnou mohou ignorovat, nebo vrátit, nebo k ní přihlédnout.
    /// <para/>
    /// Pokud některá bitová property nemá odpovídající sadu Get/Set metod, není problém je doplnit.
    /// </summary>
    public class InteractiveProperties : BitStorage
    {
        #region Konstruktory, hromadné setování
        /// <summary>
        /// Konstruktor
        /// </summary>
        public InteractiveProperties() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="bits"></param>
        public InteractiveProperties(Bit bits) : base((uint)bits) { }
        /// <summary>
        /// Vloží danou hodnotu
        /// </summary>
        /// <param name="bits"></param>
        public void Set(Bit bits)
        {
            this.Value = (uint)bits;
        }
        #endregion
        #region Konkrétní properties
        /// <summary>
        /// Je prvek interaktivní?
        /// </summary>
        public bool Interactive { get { return this.GetBitValue((uint)Bit.Interactive, GetInteractive); } set { this.SetBitValue((uint)Bit.Interactive, value, SetInteractive); } }
        /// <summary>
        /// Funkce, která vrací explicitní hodnotu <see cref="Interactive"/>
        /// </summary>
        public Func<bool, bool> GetInteractive;
        /// <summary>
        /// Akce, která setuje hodnotu <see cref="Interactive"/> nad rámec základní třídy
        /// </summary>
        public Action<bool> SetInteractive;

        /// <summary>
        /// Visible?
        /// </summary>
        public bool Visible { get { return this.GetBitValue((uint)Bit.Visible, GetVisible); } set { this.SetBitValue((uint)Bit.Visible, value, SetVisible); } }
        /// <summary>
        /// Funkce, která vrací explicitní hodnotu <see cref="Visible"/>
        /// </summary>
        public Func<bool, bool> GetVisible;
        /// <summary>
        /// Akce, která setuje hodnotu <see cref="Visible"/> nad rámec základní třídy
        /// </summary>
        public Action<bool> SetVisible;

        /// <summary>
        /// Je tento prvek Enabled?
        /// Do prvku, který NENÍ Enabled, nelze vstoupit Focusem (ani provést DoubleClick ani na ikoně / overlay).
        /// </summary>
        public bool Enabled { get { return this.GetBitValue((uint)Bit.Enabled, GetEnabled); } set { this.SetBitValue((uint)Bit.Enabled, value, SetEnabled); } }
        /// <summary>
        /// Funkce, která vrací explicitní hodnotu <see cref="Enabled"/>
        /// </summary>
        public Func<bool, bool> GetEnabled;
        /// <summary>
        /// Akce, která setuje hodnotu <see cref="Enabled"/> nad rámec základní třídy
        /// </summary>
        public Action<bool> SetEnabled;

        /// <summary>
        /// Je tento prvek ReadOnly?
        /// Do prvku, který JE ReadOnly, lze vstoupit Focusem, lze provést DoubleClick včetně ikony / overlay.
        /// Ale nelze prvek editovat, a má vzhled prvku který není Enabled (=typicky má šedou barvu a nereaguje vizuálně na myš).
        /// </summary>
        public bool ReadOnly { get { return this.GetBitValue((uint)Bit.ReadOnly, GetReadOnly); } set { this.SetBitValue((uint)Bit.ReadOnly, value, SetReadOnly); } }
        /// <summary>
        /// Funkce, která vrací explicitní hodnotu <see cref="ReadOnly"/>
        /// </summary>
        public Func<bool, bool> GetReadOnly;
        /// <summary>
        /// Akce, která setuje hodnotu <see cref="ReadOnly"/> nad rámec základní třídy
        /// </summary>
        public Action<bool> SetReadOnly;

        /// <summary>
        /// OnPhysicalBounds? 
        /// <para/>
        /// Hodnota true znamená, že souřadnice (Bounds) tohoto prvku jsou vázané na fyzické souřadnice Parenta.
        ///  Toto nastavení se používá pouze u controlů, které reprezentují Scrollbary v AutoScroll containerech,
        ///  protože ty jsou vázané na fyzické souřadnice svého containeru (jsou vpravo a dole), 
        ///  a nemají se pohybovat na virtuální ploše spolu s běžnými Child prvky.
        /// <para/>
        /// Defaultní hodnota je false, znamená že this prvek má souřadnice (Bounds) vztažené k virtuálnímu souřadnému systému.
        ///  Takový control je pak scrollován uvnitř Parent containeru podle pravidel AutoScroll.
        ///  Jde o běžné uživatelské controly.
        /// </summary>
        public bool OnPhysicalBounds { get { return this.GetBitValue((uint)Bit.OnPhysicalBounds, GetOnPhysicalBounds); } set { this.SetBitValue((uint)Bit.OnPhysicalBounds, value, SetOnPhysicalBounds); } }
        /// <summary>
        /// Funkce, která vrací explicitní hodnotu <see cref="Enabled"/>
        /// </summary>
        public Func<bool, bool> GetOnPhysicalBounds;
        /// <summary>
        /// Akce, která setuje hodnotu <see cref="OnPhysicalBounds"/> nad rámec základní třídy
        /// </summary>
        public Action<bool> SetOnPhysicalBounds;

        /// <summary>
        /// Je aktuálně označen (Checked)?
        /// </summary>
        public virtual bool Checked { get { return this.GetBitValue((uint)Bit.Checked, GetChecked); } set { this.SetBitValue((uint)Bit.Checked, value, SetChecked); } }
        /// <summary>
        /// Funkce, která vrací explicitní hodnotu <see cref="Checked"/>
        /// </summary>
        public Func<bool, bool> GetChecked;
        /// <summary>
        /// Akce, která setuje hodnotu <see cref="Checked"/> nad rámec základní třídy
        /// </summary>
        public Action<bool> SetChecked;

        /// <summary>
        /// Lze selectovat?
        /// </summary>
        public virtual bool Selectable { get { return this.GetBitValue((uint)Bit.Selectable, GetSelectable); } set { this.SetBitValue((uint)Bit.Selectable, value, SetSelectable); } }
        /// <summary>
        /// Funkce, která vrací explicitní hodnotu <see cref="Selectable"/>
        /// </summary>
        public Func<bool, bool> GetSelectable;
        /// <summary>
        /// Akce, která setuje hodnotu <see cref="Selectable"/> nad rámec základní třídy
        /// </summary>
        public Action<bool> SetSelectable;

        /// <summary>
        /// Může se na tomto prvku zahájit akce SelectFrame?
        /// </summary>
        public virtual bool SelectParent { get { return this.GetBitValue((uint)Bit.SelectParent, GetSelectParent); } set { this.SetBitValue((uint)Bit.SelectParent, value, SetSelectParent); } }
        /// <summary>
        /// Funkce, která vrací explicitní hodnotu <see cref="SelectParent"/>
        /// </summary>
        public Func<bool, bool> GetSelectParent;
        /// <summary>
        /// Akce, která setuje hodnotu <see cref="SelectParent"/> nad rámec základní třídy
        /// </summary>
        public Action<bool> SetSelectParent;

        /// <summary>
        /// Hold mouse?
        /// </summary>
        public bool HoldMouse { get { return this.GetBitValue((uint)Bit.HoldMouse); } set { this.SetBitValue((uint)Bit.HoldMouse, value); } }
        /// <summary>
        /// Prvek má myš
        /// </summary>
        public bool HasMouse { get { return this.GetBitValue((uint)Bit.HasMouse); } set { this.SetBitValue((uint)Bit.HasMouse, value); } }
        /// <summary>
        /// Prvek má focus
        /// </summary>
        public bool HasFocus { get { return this.GetBitValue((uint)Bit.HasFocus); } set { this.SetBitValue((uint)Bit.HasFocus, value); } }
        /// <summary>
        /// Suppressed events?
        /// </summary>
        public bool SuppressEvents { get { return this.GetBitValue((uint)Bit.SuppressEvents); } set { this.SetBitValue((uint)Bit.SuppressEvents, value); } }
     
        /// <summary>
        /// Prvek je obecně myšoaktivní
        /// </summary>
        public bool MouseActive { get { return this.GetBitValue((uint)Bit.MouseActive); } set { this.SetBitValue((uint)Bit.MouseActive, value); } }
       
        /// <summary>
        /// Prvek chce dostávat i eventy o každém pohybu myši nad prvkem (MouseOver)
        /// </summary>
        public bool MouseMoveOver { get { return this.GetBitValue((uint)Bit.MouseMoveOver, GetMouseMoveOver); } set { this.SetBitValue((uint)Bit.MouseMoveOver, value, SetMouseMoveOver); } }
        /// <summary>
        /// Funkce, která vrací explicitní hodnotu <see cref="MouseMoveOver"/>
        /// </summary>
        public Func<bool, bool> GetMouseMoveOver;
        /// <summary>
        /// Akce, která setuje hodnotu <see cref="MouseMoveOver"/> nad rámec základní třídy
        /// </summary>
        public Action<bool> SetMouseMoveOver;

        /// <summary>
        /// Prvek může dostávat Mouse Click eventy
        /// </summary>
        public bool MouseClick { get { return this.GetBitValue((uint)Bit.MouseClick); } set { this.SetBitValue((uint)Bit.MouseClick, value); } }
        
        /// <summary>
        /// Prvek může dostávat Mouse DoubleClick eventy
        /// </summary>
        public bool MouseDoubleClick { get { return this.GetBitValue((uint)Bit.MouseDoubleClick); } set { this.SetBitValue((uint)Bit.MouseDoubleClick, value); } }
        
        /// <summary>
        /// Prvek může dostávat Mouse LongClick eventy
        /// </summary>
        public bool MouseLongClick { get { return this.GetBitValue((uint)Bit.MouseLongClick); } set { this.SetBitValue((uint)Bit.MouseLongClick, value); } }

        /// <summary>
        /// Prvek může být přemísťován pomocí Drag and Drop
        /// </summary>
        public bool MouseDragMove { get { return this.GetBitValue((uint)Bit.MouseDragMove, GetMouseDragMove); } set { this.SetBitValue((uint)Bit.MouseDragMove, value, SetMouseDragMove); } }
        /// <summary>
        /// Funkce, která vrací explicitní hodnotu <see cref="MouseDragMove"/>
        /// </summary>
        public Func<bool, bool> GetMouseDragMove;
        /// <summary>
        /// Akce, která setuje hodnotu <see cref="MouseDragMove"/> nad rámec základní třídy
        /// </summary>
        public Action<bool> SetMouseDragMove;

        /// <summary>
        /// Prvek může být interaktivně upravován ve směru X (šířka) pomocí Drag and Drop
        /// </summary>
        public bool MouseDragResizeX { get { return this.GetBitValue((uint)Bit.MouseDragResizeX); } set { this.SetBitValue((uint)Bit.MouseDragResizeX, value); } }
   
        /// <summary>
        /// Prvek může být interaktivně upravován ve směru Y (výška) pomocí Drag and Drop
        /// </summary>
        public bool MouseDragResizeY { get { return this.GetBitValue((uint)Bit.MouseDragResizeY); } set { this.SetBitValue((uint)Bit.MouseDragResizeY, value); } }
     
        /// <summary>
        /// Při procesu Drag and Drop se má vykreslovat Ghost do vrstvy Interactive. 
        /// Ve vrstvě Standard zůstane objekt vykreslen v plné barvě.
        /// </summary>
        public bool DrawDragMoveGhostInteractive { get { return this.GetBitValue((uint)Bit.DrawDragMoveGhostInteractive); } set { this.SetBitValue((uint)Bit.DrawDragMoveGhostInteractive, value); } }
      
        /// <summary>
        /// Při procesu Drag and Drop se má vykreslit Ghost do vrstvy Standard. 
        /// Ve vrstvě Interactive bude objekt vykreslován v plné barvě.
        /// </summary>
        public bool DrawDragMoveGhostStandard { get { return this.GetBitValue((uint)Bit.DrawDragMoveGhostStandard); } set { this.SetBitValue((uint)Bit.DrawDragMoveGhostStandard, value); } }
    
        /// <summary>
        /// Prvek může dostávat klávesnicové události.
        /// </summary>
        public bool KeyboardInput { get { return this.GetBitValue((uint)Bit.KeyboardInput); } set { this.SetBitValue((uint)Bit.KeyboardInput, value); } }
        /// <summary>
        /// Prvek může dostat focus při pohybu Tab / Ctrl+Tab
        /// </summary>
        public bool TabStop { get { return this.GetBitValue((uint)Bit.TabStop); } set { this.SetBitValue((uint)Bit.TabStop, value); } }

        /// <summary>
        /// Prvek si má při příchodu Focusu v události <see cref="GInteractiveChangeState.KeyboardFocusEnter"/> = metodě <see cref="InteractiveObject.SetAutoScrollContainerToThisItem(GInteractiveChangeStateArgs)"/> sám zajistit AutoScroll tak, aby byl viditelný?
        /// Defaultně = true, lze vypnout.
        /// </summary>
        public bool AutoScrollToShow { get { return this.GetBitValue((uint)Bit.AutoScrollToShow); } set { this.SetBitValue((uint)Bit.AutoScrollToShow, value); } }

        /// <summary>
        /// Tento prvek je aktuálně cílem v procesu Drag and Move.
        /// Hodnotu do této property do konkrétního prvku <see cref="IInteractiveItem"/> vkládá control <see cref="InteractiveControl"/> v průběhu akce Drag and Move.
        /// Prvek na tuto hodnotu může reagovat zvýrazněním vykreslení.
        /// </summary>
        public bool ActiveTarget { get { return this.GetBitValue((uint)Bit.ActiveTarget); } set { this.SetBitValue((uint)Bit.ActiveTarget, value); } }

        #endregion
        #region Enum Bit a jeho defaultní hodnoty
        /// <summary>
        /// Bity odpovídající jednotlivým hodnotám
        /// </summary>
        [Flags]
        public enum Bit : uint
        {
            /// <summary>Žádný bit</summary>
            None = 0,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="Interactive"/></summary>
            Interactive = 0x00000001,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="Visible"/></summary>
            Visible = 0x00000002,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="Enabled"/></summary>
            Enabled = 0x00000004,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="ReadOnly"/></summary>
            ReadOnly = 0x00000008,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="KeyboardInput"/></summary>
            KeyboardInput = 0x00000010,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="TabStop"/></summary>
            TabStop = 0x00000020,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="Selectable"/></summary>
            Selectable = 0x00000040,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="SelectParent"/></summary>
            SelectParent = 0x00000080,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="Checked"/></summary>
            Checked = 0x00000100,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="HoldMouse"/></summary>
            HoldMouse = 0x00000200,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="HasMouse"/></summary>
            HasMouse = 0x00000400,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="HasFocus"/></summary>
            HasFocus = 0x00000800,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="MouseActive"/></summary>
            MouseActive = 0x00001000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="MouseMoveOver"/></summary>
            MouseMoveOver = 0x00002000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="MouseClick"/></summary>
            MouseClick = 0x00004000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="MouseDoubleClick"/></summary>
            MouseDoubleClick = 0x00008000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="MouseLongClick"/></summary>
            MouseLongClick = 0x00010000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="MouseDragMove"/></summary>
            MouseDragMove = 0x00020000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="MouseDragResizeX"/></summary>
            MouseDragResizeX = 0x00040000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="MouseDragResizeY"/></summary>
            MouseDragResizeY = 0x00080000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="DrawDragMoveGhostInteractive"/></summary>
            DrawDragMoveGhostInteractive = 0x00100000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="DrawDragMoveGhostStandard"/></summary>
            DrawDragMoveGhostStandard = 0x00200000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="SuppressEvents"/></summary>
            SuppressEvents = 0x00400000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="ActiveTarget"/></summary>
            ActiveTarget = 0x00800000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="OnPhysicalBounds"/></summary>
            OnPhysicalBounds = 0x01000000,
            /// <summary>nevyužito</summary>
            Void6 = 0x02000000,
            /// <summary>nevyužito</summary>
            Void7 = 0x04000000,
            /// <summary>Konkrétní jeden bit pro odpovídající vlastnost <see cref="AutoScrollToShow"/></summary>
            AutoScrollToShow = 0x08000000,
            /// <summary>nevyužito</summary>
            Void3 = 0x10000000,
            /// <summary>nevyužito</summary>
            Void4 = 0x20000000,
            /// <summary>nevyužito</summary>
            Void5 = 0x40000000,

            /// <summary>
            /// Defaultní sada pro běžný prvek běžně aktivní na myš, vyjma <see cref="MouseMoveOver"/>. 
            /// Má nastaveno <see cref="TabStop"/> a <see cref="AutoScrollToShow"/>.
            /// Nemá žádné Drag vlastnosti. 
            /// </summary>
            DefaultMouseProperties = Interactive | Visible | Enabled | MouseActive | MouseClick | MouseDoubleClick | MouseLongClick | TabStop | AutoScrollToShow,
            /// <summary>
            /// Defaultní sada pro běžný prvek běžně aktivní na myš, včetně <see cref="MouseMoveOver"/>. 
            /// Nemá žádné Drag vlastnosti.
            /// </summary>
            DefaultMouseOverProperties = DefaultMouseProperties | MouseMoveOver
        }
        /// <summary>
        /// Defaultní hodnota pro bezparametrický konstruktor = <see cref="Bit.DefaultMouseProperties"/>
        /// </summary>
        protected override uint DefaultValue
        {
            get { return ((uint)Bit.DefaultMouseProperties) ; }
        }
        #endregion
    }
    #endregion
    #region Delegates and EventArgs
    /// <summary>
    /// Delegate for handlers of interactive event in InteractiveControl
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GInteractiveChangeStateHandler(object sender, GInteractiveChangeStateArgs e);
    /// <summary>
    /// Delegate for handlers of drawing event in InteractiveControl
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GInteractiveDrawHandler(object sender, GInteractiveDrawArgs e);
    /// <summary>
    /// Delegát pro handlery události, kdy došlo k nějaké akci na určitém objektu v InteractiveControl
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GPropertyEventHandler<T>(object sender, GPropertyEventArgs<T> e);
    /// <summary>
    /// Delegate for handlers for Drawing into User Area
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GUserDrawHandler(object sender, GUserDrawArgs e);
    /// <summary>
    /// Data pro eventhandler navázaný na událost na určitém objektu v InteractiveControl
    /// </summary>
    public class GPropertyEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="eventSource"></param>
        /// <param name="interactiveArgs"></param>
        public GPropertyEventArgs(T value, EventSourceType eventSource = EventSourceType.InteractiveChanged, GInteractiveChangeStateArgs interactiveArgs = null)
        {
            this.Value = value;
            this.ValueNew = value;
            this.EventSource = eventSource;
            this.InteractiveArgs = interactiveArgs;
            this.Cancel = false;
        }
        /// <summary>
        /// Hodnota
        /// </summary>
        public T Value { get; private set; }
        /// <summary>
        /// Hodnota upravená aplikačním kódem.
        /// Výchozí stav = shodný s <see cref="Value"/>.
        /// </summary>
        public T ValueNew { get; set; }
        /// <summary>
        /// Zdroj události
        /// </summary>
        public EventSourceType EventSource { get; private set; }
        /// <summary>
        /// Data o interaktivní události
        /// </summary>
        public GInteractiveChangeStateArgs InteractiveArgs { get; private set; }
        /// <summary>
        /// Obsahuje true pokud <see cref="InteractiveArgs"/> obsahuje data.
        /// </summary>
        public bool HasInteractiveArgs { get { return (this.InteractiveArgs != null); } }
        /// <summary>
        /// Požadavek aplikačního kódu na zrušení návazností této akce
        /// Výchozí hodnota je false.
        /// </summary>
        public bool Cancel { get; set; }
    }
    /// <summary>
    /// Data pro handlery interaktivních událostí v InteractiveControl
    /// </summary>
    public class GInteractiveChangeStateArgs : EventArgs
    {
        #region Konstruktory
        /// <summary>
        /// Konstruktor pro událost pocházející z myši
        /// </summary>
        /// <param name="boundsInfo">Souřadný systém položky <see cref="CurrentItem"/>, včetně souřadnic absolutních a reference na konkrétní prvek</param>
        /// <param name="changeState">Typ události = změna stavu</param>
        /// <param name="targetState">Nový stav prvku po této změně.</param>
        /// <param name="searchItemMethod">Metoda, která pro danou absolutní souřadnici vyhledá konkrétní prvek. Parametr 1 = absolutní souřadnice; Parametr 2 = požadavek na hledání i Disabled prvků (true hledá i Disabled); Výstup = prvek na dané souřadnici, na nejvyšší pozici v hierarchii i v ose Z.</param>
        /// <param name="mouseAbsolutePoint"></param>
        /// <param name="mouseRelativePoint">Coordinate of mouse relative to CurrentItem.ActiveBounds.Location. Can be a null (in case when ExistsItem is false).</param>
        /// <param name="dragOriginBounds">Original area before current Drag operacion begun (in DragMove events)</param>
        /// <param name="dragToBounds">Target area during Drag operation (in DragMove event)</param>
        /// <param name="targetPosition">Target prvek při Drag and Move operacích</param>
        internal GInteractiveChangeStateArgs(BoundsInfo boundsInfo, GInteractiveChangeState changeState, GInteractiveState targetState, 
            Func<Point, bool, IInteractiveItem> searchItemMethod, Point? mouseAbsolutePoint, Point? mouseRelativePoint,
            Rectangle? dragOriginBounds, Rectangle? dragToBounds, ActivePositionInfo targetPosition)
              : this()
        {
            this.BoundsInfo = boundsInfo;
            this.ChangeState = changeState;
            this.TargetState = targetState;
            this.SearchItemMethod = searchItemMethod;
            this.MouseAbsolutePoint = mouseAbsolutePoint;
            this.MouseRelativePoint = mouseRelativePoint;
            this.DragMoveOriginBounds = dragOriginBounds;
            this.DragMoveToBounds = dragToBounds;
            this.DragMoveToTarget = targetPosition;
        }
        /// <summary>
        /// Konstruktor pro událost pocházející z klávesnice
        /// </summary>
        /// <param name="boundsInfo">Souřadný systém položky <see cref="CurrentItem"/>, včetně souřadnic absolutních a reference na konkrétní prvek</param>
        /// <param name="changeState">Typ události = změna stavu</param>
        /// <param name="targetState">Nový stav prvku po této změně.</param>
        /// <param name="searchItemMethod">Metoda, která pro danou absolutní souřadnici vyhledá konkrétní prvek. Parametr 1 = absolutní souřadnice; Parametr 2 = požadavek na hledání i Disabled prvků (true hledá i Disabled); Výstup = prvek na dané souřadnici, na nejvyšší pozici v hierarchii i v ose Z.</param>
        /// <param name="previewArgs">Keyboard Preview Data</param>
        /// <param name="keyArgs">Keyboard Events Data</param>
        /// <param name="keyPressArgs">Keyboard KeyPress data</param>
        public GInteractiveChangeStateArgs(BoundsInfo boundsInfo, GInteractiveChangeState changeState, GInteractiveState targetState, 
            Func<Point, bool, IInteractiveItem> searchItemMethod, PreviewKeyDownEventArgs previewArgs, KeyEventArgs keyArgs, KeyPressEventArgs keyPressArgs)
            : this()
        {
            this.BoundsInfo = boundsInfo;
            this.ChangeState = changeState;
            this.TargetState = targetState;
            this.SearchItemMethod = searchItemMethod;
            this.KeyboardPreviewArgs = previewArgs;
            this.KeyboardEventArgs = keyArgs;
            this.KeyboardPressEventArgs = keyPressArgs;
        }
        /// <summary>
        /// Konstruktor pro událost nepocházející ani z myši, ani z klávesnice, anebo pro událost MouseEnter a MouseLeave controlu.
        /// </summary>
        /// <param name="boundsInfo">Souřadný systém položky <see cref="CurrentItem"/>, včetně souřadnic absolutních a reference na konkrétní prvek</param>
        /// <param name="changeState">Typ události = změna stavu</param>
        /// <param name="targetState">Nový stav prvku po této změně.</param>
        /// <param name="searchItemMethod">Metoda, která pro danou absolutní souřadnici vyhledá konkrétní prvek. Parametr 1 = absolutní souřadnice; Parametr 2 = požadavek na hledání i Disabled prvků (true hledá i Disabled); Výstup = prvek na dané souřadnici, na nejvyšší pozici v hierarchii i v ose Z.</param>
        /// <param name="leaveItem">Prvek, který opouštíme</param>
        /// <param name="enterItem">Prvek, do kterého vstupujeme</param>
        public GInteractiveChangeStateArgs(BoundsInfo boundsInfo, GInteractiveChangeState changeState, GInteractiveState targetState, 
            Func<Point, bool, IInteractiveItem> searchItemMethod,
            IInteractiveItem leaveItem, IInteractiveItem enterItem)
               : this()
        {
            this.BoundsInfo = boundsInfo;
            this.ChangeState = changeState;
            this.TargetState = targetState;
            this.SearchItemMethod = searchItemMethod;
            this.LeaveItem = leaveItem;
            this.EnterItem = enterItem;
        }
        /// <summary>
        /// Konstruktor pouze pro inicializaci proměnných
        /// </summary>
        protected GInteractiveChangeStateArgs()
        {
            this.BoundsInfo = null;
            this.ChangeState = GInteractiveChangeState.None;
            this.TargetState = GInteractiveState.None;
            this.SearchItemMethod = null;
            this.MouseAbsolutePoint = null;
            this.MouseRelativePoint = null;
            this.DragMoveOriginBounds = null;
            this.DragMoveToBounds = null;
            this.ModifierKeys = System.Windows.Forms.Control.ModifierKeys;
            this.MouseButtons = System.Windows.Forms.Control.MouseButtons;
            this.KeyboardPreviewArgs = null;
            this.KeyboardEventArgs = null;
            this.KeyboardPressEventArgs = null;
            this.ActionIsSolved = false;
        }
        #endregion
        #region Data o události (read-only)
        /// <summary>
        /// Souřadný systém položky <see cref="CurrentItem"/>, včetně souřadnic absolutních a reference na konkrétní prvek
        /// </summary>
        public BoundsInfo BoundsInfo { get; protected set; }
        /// <summary>
        /// Obsahuje true, pokud <see cref="CurrentItem"/> je nalezen.
        /// Poněvadž <see cref="CurrentItem"/> je interface (tedy může to být i struct), pak je vhodnější netestovat: if (<see cref="CurrentItem"/> != null).
        /// </summary>
        public bool ExistsItem { get { return (this.BoundsInfo != null); } }
        /// <summary>
        /// Aktivní prvek.
        /// </summary>
        public IInteractiveItem CurrentItem { get { return (this.BoundsInfo != null ? this.BoundsInfo.CurrentItem : null); } }
        /// <summary>
        /// Typ události = změny stavu
        /// </summary>
        public GInteractiveChangeState ChangeState { get; protected set; }
        /// <summary>
        /// Stav, který bude platit po změně stavu
        /// </summary>
        public GInteractiveState TargetState { get; protected set; }
        /// <summary>
        /// Metoda, která pro danou absolutní souřadnici vyhledá konkrétní prvek.
        /// Parametr 1 = absolutní souřadnice;
        /// Parametr 2 = požadavek na hledání i Disabled prvků (true hledá i Disabled);
        /// Výstup = prvek na dané souřadnici, na nejvyšší pozici v hierarchii i v ose Z.
        /// </summary>
        protected Func<Point, bool, IInteractiveItem> SearchItemMethod;
        /// <summary>
        /// Absolutní souřadnice myši v koordinátech Controlu.
        /// Může být null, pokud se akce nevztahuje k myši anebo pokud <see cref="ExistsItem"/> je false.
        /// </summary>
        public Point? MouseAbsolutePoint { get; protected set; }
        /// <summary>
        /// Relativní souřadnice myši v koordinátech aktivního prvku <see cref="CurrentItem"/>.
        /// Může být null, pokud se akce nevztahuje k myši anebo pokud <see cref="ExistsItem"/> je false.
        /// </summary>
        public Point? MouseRelativePoint { get; protected set; }
        /// <summary>
        /// Souřadnice prvku výchozí před zahájením akce Drag and Drop, relativní koordináty. 
        /// Je vyplněno pouze v Drag and Drop událostech, jinak je null.
        /// </summary>
        public Rectangle? DragMoveOriginBounds { get; protected set; }
        /// <summary>
        /// Souřadnice prvku cílová v průběhu akce Drag and Drop, relativní koordináty. 
        /// Jedná se o souřadnice odpovídající pohybu myši; prvek sám může svoje cílové souřadnice modifikovat s ohledem na svoje vlastní pravidla.
        /// Je vyplněno pouze v Drag and Drop událostech, jinak je null.
        /// </summary>
        public Rectangle? DragMoveToBounds { get; protected set; }
        /// <summary>
        /// Prvek, nad kterým se nyní pohybuje myš v procesu Drag and Move (při akci DragStep).
        /// </summary>
        internal ActivePositionInfo DragMoveToTarget { get; private set; }
        /// <summary>
        /// Prvek, který má být vysvícen jako Aktivní cíl v procesu Drag and Move.
        /// Výchozí hodnota je null. Aplikační kód může určit potenciální cílový objekt pro Drop akci, a tento objekt vložit do této property.
        /// Control <see cref="InteractiveControl"/> následně pro tento prvek nastaví jeho hodnotu Active, a prvek by se pak měl zobrazit zvýrazněný.
        /// Je nastavena hodnota <see cref="IInteractiveItem.Is"/>.ActiveTarget = true / false a je zajištěn Repaint() prvku.
        /// </summary>
        public IInteractiveItem DragMoveActiveItem { get; set; }
        /// <summary>
        /// Prostor, ve kterém může probíhat výběr pomocí zarámování (DragFrame).
        /// Prostor je smysluplné nastavit pouze v eventu, kdy <see cref="ChangeState"/> == <see cref="GInteractiveChangeState.LeftDragFrameBegin"/> (nebo <see cref="GInteractiveChangeState.RightDragFrameBegin"/>.
        /// V jiných eventech jej sice lze nastavit, ale hodnota bude zahozena.
        /// </summary>
        public Rectangle? DragFrameWorkArea { get; set; }
        /// <summary>
        /// Stav kláves v okamžiku události (a to včetně události myši)
        /// </summary>
        public System.Windows.Forms.Keys ModifierKeys { get; protected set; }
        /// <summary>
        /// Stav stisknutých tlačítek myši v okamžiku události (a to včetně události klávesnice)
        /// </summary>
        public System.Windows.Forms.MouseButtons MouseButtons { get; protected set; }
        /// <summary>
        /// Keyboard Preview data
        /// </summary>
        public PreviewKeyDownEventArgs KeyboardPreviewArgs { get; protected set; }
        /// <summary>
        /// Keyboard events data
        /// </summary>
        public KeyEventArgs KeyboardEventArgs { get; protected set; }
        /// <summary>
        /// Keyboard KeyPress data
        /// </summary>
        public KeyPressEventArgs KeyboardPressEventArgs { get; protected set; }
        /// <summary>
        /// Prvek, který opouštíme. 
        /// Má význam pouze v událostech MouseLeave a MouseEnter, a KeyboardLeave a KeyboardEnter.
        /// </summary>
        public IInteractiveItem LeaveItem { get; protected set; }
        /// <summary>
        /// Prvek, do kterého vstupujeme.
        /// Má význam pouze v událostech MouseLeave a MouseEnter, a KeyboardLeave a KeyboardEnter.
        /// </summary>
        public IInteractiveItem EnterItem { get; protected set; }
        #endregion
        #region Find Item at location (explicit, current)
        /// <summary>
        /// Item, which BoundAbsolute is on (this.MouseCurrentAbsolutePoint).
        /// </summary>
        public IInteractiveItem ItemAtCurrentMousePoint { get { return this._ItemAtCurrentMousePointGet(); } }
        /// <summary>
        /// Returns _ItemAtCurrentMousePointResult (on first calling perform search).
        /// </summary>
        /// <returns></returns>
        private IInteractiveItem _ItemAtCurrentMousePointGet()
        {
            if (!this._ItemAtCurrentMousePointSearched)
            {
                if (this.MouseAbsolutePoint.HasValue)
                    this._ItemAtCurrentMousePointResult = this.FindItemAtPoint(this.MouseAbsolutePoint.Value);
                this._ItemAtCurrentMousePointSearched = true;
            }
            return this._ItemAtCurrentMousePointResult;
        }
        /// <summary>
        /// Item at MouseCurrentAbsolutePoint, after search
        /// </summary>
        private IInteractiveItem _ItemAtCurrentMousePointResult;
        /// <summary>
        /// Search for _ItemAtCurrentMousePointResult was processed?
        /// </summary>
        private bool _ItemAtCurrentMousePointSearched;
        /// <summary>
        /// Tato metoda najde první Top-Most objekt, který se nachází na dané absolutní souřadnici.
        /// Může vrátit null, když tam nic není.
        /// Tato metoda "nevidí" prvky, které mají IsDisabled = true.
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public IInteractiveItem FindItemAtPoint(Point absolutePoint)
        {
            return this.FindItemAtPoint(absolutePoint, false);
        }
        /// <summary>
        /// Tato metoda najde první Top-Most objekt, který se nachází na dané absolutní souřadnici.
        /// Může vrátit null, když tam nic není.
        /// Tato metoda akceptuje prvky, které mají IsDisabled = true, pokud má parametr "withDisabled" = true.
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <param name="withDisabled">true = najde i IsDisabled prvky</param>
        /// <returns></returns>
        public IInteractiveItem FindItemAtPoint(Point absolutePoint, bool withDisabled)
        {
            if (this.SearchItemMethod == null) return null;
            return this.SearchItemMethod(absolutePoint, withDisabled);
        }
        #endregion
        #region Výstupy - z EventHandleru do Controlu
        /// <summary>
        /// User defined point during Drag operation.
        /// User (an IInteractiveItem) can set any point in event LeftDragBegin / RightDragBegin;
        /// then InteractiveControl will be calculated appropriate moved point during Drag, 
        /// and this "dragged" point coordinates are stored to this property (UserDragPoint) before call event LeftDragMove / RightDragMove.
        /// When user in event DragBegin does not set any location (null value), then in event DragMove will be in this property null value.
        /// For other events this property does not have any meaning.
        /// </summary>
        public Point? UserDragPoint { get; set; }
        /// <summary>
        /// Změnit kurzor na tento typ. null = beze změny.
        /// </summary>
        public SysCursorType? RequiredCursorType { get; set; }
        /// <summary>
        /// Po tomto eventu se má překreslit úplně celý Control.
        /// Lze nastavit pouze na hodnotu true; pokud je jednou true, pak už nelze shodit na false.
        /// Není to ale ideální řešení, protože jeden maličký control nemůže vědět, jak velký je rozsah toho, co chce překreslit.
        /// Smysl to dává typicky při změně jazyka, změně barevné palety a Zoomu, atd.
        /// Optimální je dát Repaint() na tom prvku, který se má znovu vykreslit. Prvek sám si může tuto událost zpracovat (overridovat metodu Repaint()) a zajistit Repaint i pro své sousedící prvky.
        /// </summary>
        public bool RepaintAllItems { get { return this._RepaintAllItems; } set { if (value) this._RepaintAllItems = true; } } private bool _RepaintAllItems;
        /// <summary>
        /// Data pro tooltip.
        /// Tuto property lze setovat, nebo ji lze rovnou naplnit (je autoinicializační).
        /// </summary>
        public ToolTipData ToolTipData { get { if (this._ToolTipData == null) this._ToolTipData = new ToolTipData(); return this._ToolTipData; } set { this._ToolTipData = value; } } private ToolTipData _ToolTipData;
        /// <summary>
        /// Obsahuje true pokud je přítomen objekt <see cref="ToolTipData"/>. 
        /// Ten je přítomen po jeho vložení nebo po jeho použití. Ve výchozím stavu je false.
        /// Vedle toho existuje property <see cref="ToolTipIsValid"/>, která říká, že ToolTip existuje a obsahuje něco k vykreslení.
        /// </summary>
        internal bool HasToolTipData { get { return (this._ToolTipData != null); } }
        /// <summary>
        /// Obsahuje true pokud <see cref="ToolTipData"/> obsahuje platná data pro vykreslení tooltipu.
        /// </summary>
        public bool ToolTipIsValid { get { return (this._ToolTipData != null && this._ToolTipData.IsValid); } }
        /// <summary>
        /// Metoda by měla nastavit true, pokud danou operaci vyřeší.
        /// Při akci typu <see cref="GInteractiveChangeState.WheelUp"/> a <see cref="GInteractiveChangeState.WheelDown"/> se testuje, zda <see cref="ActionIsSolved"/> je true.
        /// Pokud není, pak se stejná událost pošle i do Parent objektů.
        /// </summary>
        public bool ActionIsSolved { get; set; }
        /// <summary>
        /// Aplikační kód sem může vložit kontextové menu k aktuální akci.
        /// Může být k libovolné akci, typické je to k akci <see cref="GInteractiveChangeState.GetContextMenu"/>.
        /// </summary>
        public ToolStripDropDownMenu ContextMenu { get; set; }
        /// <summary>
        /// Zcela libovolná UserData, přenášená v rámci jedné fyzické události mezi všemi metodami
        /// </summary>
        public object UserData { get; set; }
        #endregion
    }
    /// <summary>
    /// Data for handlers of drag item events (drag process, drag drop; drag this object on another object, or drag another object on this object)
    /// </summary>
    public class GDragActionArgs : EventArgs
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="changeArgs"></param>
        /// <param name="dragAction"></param>
        /// <param name="mouseDownAbsolutePoint"></param>
        /// <param name="mouseCurrentAbsolutePoint"></param>
        public GDragActionArgs(GInteractiveChangeStateArgs changeArgs, DragActionType dragAction, Point mouseDownAbsolutePoint, Point? mouseCurrentAbsolutePoint)
        {
            this._ChangeArgs = changeArgs;
            this.DragAction = dragAction;
            this.MouseDownAbsolutePoint = mouseDownAbsolutePoint;
            this.MouseCurrentAbsolutePoint = mouseCurrentAbsolutePoint;
        }
        private GInteractiveChangeStateArgs _ChangeArgs;
        #endregion
        #region Public properties - vstupní ze controlu (read-only)
        /// <summary>
        /// Kompletní data o interaktivní akci
        /// </summary>
        public GInteractiveChangeStateArgs ChangeArgs { get { return this._ChangeArgs; } }
        /// <summary>
        /// Typ události = změny stavu
        /// </summary>
        public GInteractiveChangeState ChangeState { get { return this._ChangeArgs.ChangeState; } }
        /// <summary>
        /// Stav, který bude platit po změně stavu
        /// </summary>
        public GInteractiveState TargetState { get { return this._ChangeArgs.TargetState; } }
        /// <summary>
        /// Data pro tooltip.
        /// Tuto property lze setovat, nebo ji lze rovnou naplnit (je autoinicializační).
        /// </summary>
        public ToolTipData ToolTipData { get { return this._ChangeArgs.ToolTipData; } set { this._ChangeArgs.ToolTipData = value; } }
        /// <summary>
        /// Souřadnice prvku výchozí před zahájením akce Drag and Drop, relativní koordináty. 
        /// </summary>
        public Rectangle? DragOriginRelativeBounds { get { return this._ChangeArgs.DragMoveOriginBounds; } }
        /// <summary>
        /// Souřadnice prvku cílová v průběhu akce Drag and Drop, relativní koordináty. 
        /// Jedná se o souřadnice odpovídající pohybu myši; prvek sám může svoje cílové souřadnice modifikovat s ohledem na svoje vlastní pravidla.
        /// </summary>
        public Rectangle? DragToRelativeBounds { get { return this._ChangeArgs.DragMoveToBounds; } }
        /// <summary>
        /// Souřadnice prvku cílová v průběhu akce Drag and Drop, absolutní koordináty. 
        /// Jedná se o souřadnice odpovídající pohybu myši; prvek sám může svoje cílové souřadnice modifikovat s ohledem na svoje vlastní pravidla.
        /// </summary>
        public Rectangle? DragToAbsoluteBounds
        {
            get
            {
                Rectangle? dragToRelativeBounds = this.DragToRelativeBounds;
                BoundsInfo boundsInfo = this.BoundsInfo;
                if (boundsInfo == null || !dragToRelativeBounds.HasValue) return null;
                return boundsInfo.GetAbsoluteBounds(dragToRelativeBounds.Value);
            }
        }
        /// <summary>
        /// Aktivní prvek.
        /// </summary>
        public IInteractiveItem CurrentItem { get { return this._ChangeArgs.CurrentItem; } }
        /// <summary>
        /// Souřadný systém položky <see cref="CurrentItem"/>, včetně souřadnic absolutních a reference na konkrétní prvek
        /// </summary>
        public BoundsInfo BoundsInfo { get { return this._ChangeArgs.BoundsInfo; } }

        /// <summary>
        /// Typ aktuální akce
        /// </summary>
        public DragActionType DragAction { get; protected set; }
        /// <summary>
        /// Absolutní souřadnice myši, kde byla stisknuta.
        /// </summary>
        public Point MouseDownAbsolutePoint { get; protected set; }
        /// <summary>
        /// Absolutní souřadnice myši, kde se nachází nyní.
        /// Může být null pouze při akci <see cref="DragAction"/> == <see cref="DragActionType.DragThisCancel"/>.
        /// </summary>
        public Point? MouseCurrentAbsolutePoint { get; set; }
        #endregion
        #region Find Item at location (explicit, current)
        /// <summary>
        /// Prvek, který je přetahován
        /// </summary>
        public IInteractiveItem DragSourceItem { get { return this._ChangeArgs.ItemAtCurrentMousePoint; } }
        /// <summary>
        /// Prvek, nad kterým se nachází myš při přetahování
        /// </summary>
        public IInteractiveItem DragTargetItem { get { return this._ChangeArgs.DragMoveToTarget?.ActiveItem; } }
        /// <summary>
        /// Prvek, který má být vysvícen jako Aktivní cíl v procesu Drag and Move.
        /// Výchozí hodnota je null. Aplikační kód může určit potenciální cílový objekt pro Drop akci, a tento objekt vložit do této property.
        /// Control <see cref="InteractiveControl"/> následně pro tento prvek nastaví jeho hodnotu Active, a prvek by se pak měl zobrazit zvýrazněný.
        /// Je nastavena hodnota <see cref="IInteractiveItem.Is"/>.ActiveTarget = true / false a je zajištěn Repaint() prvku.
        /// </summary>
        public IInteractiveItem DragActiveItem { get { return this._ChangeArgs.DragMoveActiveItem; } set { this._ChangeArgs.DragMoveActiveItem = value; } }
        /// <summary>
        /// Tato metoda najde první Top-Most objekt, který se nachází na dané absolutní souřadnici.
        /// Může vrátit null, když tam nic není.
        /// Tato metoda "nevidí" prvky, které mají IsDisabled = true.
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public IInteractiveItem FindItemAtPoint(Point absolutePoint)
        {
            return this._ChangeArgs.FindItemAtPoint(absolutePoint);
        }
        /// <summary>
        /// Tato metoda najde první Top-Most objekt, který se nachází na dané absolutní souřadnici.
        /// Může vrátit null, když tam nic není.
        /// Tato metoda akceptuje prvky, které mají IsDisabled = true, pokud má parametr "withDisabled" = true.
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <param name="withDisabled">true = najde i IsDisabled prvky</param>
        /// <returns></returns>
        public IInteractiveItem FindItemAtPoint(Point absolutePoint, bool withDisabled)
        {
            return this._ChangeArgs.FindItemAtPoint(absolutePoint, withDisabled);
        }
        #endregion
        #region Results (from event to control)
        /// <summary>
        /// Required Cursor type. Null = default. Control detect change from curent state, CursorType can be set to required value everytime.
        /// Object can set cursor by own state.
        /// </summary>
        public SysCursorType? RequiredCursorType { get { return this._ChangeArgs.RequiredCursorType; } set { this._ChangeArgs.RequiredCursorType = value; } }
        /// <summary>
        /// Object can enable / disable dragging.
        /// </summary>
        public bool? DragDisable { get; set; }
        #endregion
    }
    /// <summary>
    /// Data pro obsluhu kreslení prvků ve třídě InteractiveControl
    /// </summary>
    public class GInteractiveDrawArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="graphics">Instance třídy Graphics pro kreslení</param>
        /// <param name="size">Velikost prostoru pro kreslení</param>
        /// <param name="drawLayer">Vrstva, která se bude kreslit do této vrstvy</param>
        public GInteractiveDrawArgs(Graphics graphics, Size size, GInteractiveDrawLayer drawLayer)
        {
            this.Graphics = graphics;
            this.GraphicsBounds = new Rectangle(0, 0, size.Width, size.Height);
            this.DrawLayer = drawLayer;
            this.IsStandardLayer = (drawLayer == GInteractiveDrawLayer.Standard);
            this.CurrentVisibleClip = null;
            this._ResetLayerFlag = ((int)drawLayer ^ 0xFFFF);
        }
        /// <summary>
        /// Instance třídy Graphics pro kreslení
        /// </summary>
        public Graphics Graphics { get; private set; }
        /// <summary>
        /// Výchozí souřadnice grafiky, pro kreslení do neomezeného prostoru (tyto souřadnice reprezentují viditelnou oblast WinForm controlu).
        /// Tato hodnota se nezmění žádným Clipem.
        /// </summary>
        public Rectangle GraphicsBounds { get; private set; }
        /// <summary>
        /// Vrstva, která se bude kreslit do této vrstvy
        /// </summary>
        public GInteractiveDrawLayer DrawLayer { get; private set; }
        /// <summary>
        /// true pokud se pro tuto vrstvu má používat Clip = jde o Standard vrstvu.
        /// Pro ostatní vrstvy se bude jakýkoli pokus Clip grafiky ignorovat.
        /// </summary>
        public bool IsStandardLayer { get; private set; }
        /// <summary>
        /// Prostor, do kterého je oříznut výstup grafiky (Clip) pro kreslení aktuálního prvku.
        /// Jedná se o absolutní souřadnice v rámci Controlu Host.
        /// Jde o prostor, který je průnikem prostorů všech Parentů daného prvku = tady do tohoto prostoru se prvek smí vykreslit, aniž by "utekl ze svého parenta" někam mimo.
        /// Tento prostor tedy typicky neobsahuje Clip na souřadnice kresleného prvku (Item.Bounds).
        /// </summary>
        public Rectangle? CurrentVisibleClip { get; private set; }
        /// <summary>
        /// Pro daný prvek v jeho RepaintToLayers zruší požadavek na kreslení do vrstvy, která se právě zde kreslí.
        /// Volá se typicky po dokončení Draw().
        /// </summary>
        /// <param name="item"></param>
        public void ResetLayerFlagForItem(IInteractiveItem item)
        {
            item.RepaintToLayers = (GInteractiveDrawLayer)((int)item.RepaintToLayers & this._ResetLayerFlag);
        }
        private int _ResetLayerFlag;
        /// <summary>
        /// Vrátí průsečík aktuálního this.AbsoluteVisibleClip (jde o absolutní souřadnice viditelného prostoru pro kreslení aktuálního prvku)
        /// s daným prostorem (absoluteBounds).
        /// Tato metoda vrací průsečík i pro jiné než Standard layers.
        /// </summary>
        /// <param name="absoluteClipBounds"></param>
        /// <returns></returns>
        public Rectangle GetClip(Rectangle absoluteClipBounds)
        {
            Rectangle clip = Rectangle.Intersect(this.GraphicsBounds, absoluteClipBounds);
            Rectangle? currentClip = this.CurrentVisibleClip;
            if (currentClip.HasValue)
                clip = Rectangle.Intersect(clip, currentClip.Value);
            return clip;
        }
        /// <summary>
        /// Ořízne plochu, do které bude kreslit aktuální Graphics, jen na průsečík aktuálního this.ClipBounds s danými souřadnicemi.
        /// Tato metoda provádí Clip pouze tehdy, když se kreslí do Standard layer (když this.IsStandardLayer je true), 
        /// pokud není specifikován parametr allLayers = true.
        /// Parametr permanent říká, zda toto oříznutí má být bráno jako trvalé pro aktuální kreslený prvek, tím se ovlivní chování po nějakém následujícím volání téže metody.
        /// Příklad (pro jednoduchost v 1D hodnotách):
        /// Mějme souřadnice hosta { 0 - 100 }, pro aktuální control je systémem nastaven ClipBounds { 50 - 80 }, což je pozice jeho Parenta.
        /// Následně voláme GraphicsClipWith() pro oblast { 60 - 70 } s parametrem permanent = false, následné kreslení se provede jen do oblasti { 60 - 70 }.
        /// Pokud poté voláme GraphicsClipWith() pro oblast { 50 - 65 } s parametrem permanent = false, pak se následné kreslení provede do oblasti { 50 - 65 }.
        /// Jiná situace je, pokud první volání GraphicsClipWith() pro oblast { 60 - 70 } provedeme s parametrem permanent = true. Následné kreslení proběhne do oblasti { 60 - 70 }, to je logické.
        /// Pokud ale po tomto prvním Clipu s permanent = true voláme druhý Clip pro oblast { 50 - 65 }, provede se druhý clip proti výsledku prvního clipu (neboť ten je permanentní), a výsledek bude { 60 - 65 }.
        /// </summary>
        /// <param name="absoluteClipBounds">Absolutní souřadnice clipu</param>
        /// <param name="permanent">Toto oříznutí je trvalé pro aktuální prvek</param>
        /// <param name="allLayers">Provést Clip i pro jiné vrstvy než Standard</param>
        public void GraphicsClipWith(Rectangle absoluteClipBounds, bool permanent = false, bool allLayers = false)
        {
            if (!this.IsStandardLayer && !allLayers) return;

            Rectangle currentClip = this.GetClip(absoluteClipBounds);
            if (permanent)
                this.CurrentVisibleClip = currentClip;
            this.Graphics.SetClip(currentClip);
        }
        /// <summary>
        /// Obsahuje true, pokud this má definovaný nějaký Clip = <see cref="CurrentVisibleClip"/> má hodnotu.
        /// </summary>
        public bool HasClip { get { return this.CurrentVisibleClip.HasValue; } }
        /// <summary>
        /// Obsahuje true, pokud this nemá žádný Clip (tj. <see cref="HasClip"/> je false), anebo má nějaký Clip, a ten má nenulovou velikost.
        /// </summary>
        public bool HasVisibleGraphics
        {
            get
            {
                if (!this.HasClip) return true;                      // Nemáme Clip = je viditelné vše
                Rectangle clip = this.CurrentVisibleClip.Value;      // AbsoluteVisibleClip obsahuje Interesct požadovaných CLipů a rozměrů grafiky!
                return (clip.Width > 0 && clip.Height > 0);          // Máme Clip: a ten má viditelnou oblast?
            }
        }
        /// <summary>
        /// Zruší Clip v this objektu i v grafice
        /// </summary>
        public void ResetClip()
        {
            this.CurrentVisibleClip = null;
            this.Graphics.ResetClip();
        }
    }
    /// <summary>
    /// Argument for UserDraw event
    /// </summary>
    public class GUserDrawArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="graphics">An Graphics object to draw on</param>
        /// <param name="drawLayer">Layer, currently drawed</param>
        /// <param name="userAbsoluteBounds">Area to draw (VisibleAbsoluteBounds), where item is drawed, in coordinates absolute to Host control (InteractiveControl)</param>
        public GUserDrawArgs(Graphics graphics, GInteractiveDrawLayer drawLayer, Rectangle userAbsoluteBounds)
        {
            this.Graphics = graphics;
            this.DrawLayer = drawLayer;
            this.UserAbsoluteBounds = userAbsoluteBounds;
        }
        /// <summary>
        /// An Graphics object to draw on.
        /// Graphics is clipped to this.UserAbsoluteBounds, thus cannot draw outside this area.
        /// </summary>
        public Graphics Graphics { get; private set; }
        /// <summary>
        /// Layer, currently drawed.
        /// </summary>
        public GInteractiveDrawLayer DrawLayer { get; private set; }
        /// <summary>
        /// Area to draw (VisibleAbsoluteBounds), where item is drawed, in coordinates absolute to Host control (InteractiveControl).
        /// Graphics is clipped to this.UserAbsoluteBounds, thus cannot draw outside this area.
        /// </summary>
        public Rectangle UserAbsoluteBounds { get; private set; }
    }
    /// <summary>
    /// Předpis pro handler události MousePaint
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GInteractiveMousePaintHandler(object sender, GInteractiveMousePaintArgs e);
    /// <summary>
    /// Argumenty pro události MousePaint
    /// </summary>
    public class GInteractiveMousePaintArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="interactiveChange"></param>
        /// <param name="currentActiveItem"></param>
        /// <param name="startActiveItem"></param>
        /// <param name="mousePaintInfo"></param>
        /// <param name="toolTipData"></param>
        internal GInteractiveMousePaintArgs(GInteractiveChangeState interactiveChange, ActivePositionInfo currentActiveItem, ActivePositionInfo startActiveItem, MousePaintInfo mousePaintInfo, ToolTipData toolTipData)
        {
            this._InteractiveChange = interactiveChange;
            this._CurrentActiveItem = currentActiveItem;
            this._StartActiveItem = startActiveItem;
            if (mousePaintInfo != null)
            {   // Aktualizuji souřadnice pro kreslení:
                mousePaintInfo.StartPoint = this.StartPoint;
                mousePaintInfo.EndPoint = this.CurrentPoint;
            }
            this._MousePaintInfo = mousePaintInfo;
            this._ToolTipData = toolTipData;
        }
        private GInteractiveChangeState _InteractiveChange;
        private ActivePositionInfo _CurrentActiveItem;
        private ActivePositionInfo _StartActiveItem;
        private MousePaintInfo _MousePaintInfo;
        private ToolTipData _ToolTipData;
        /// <summary>
        /// Interaktivní stav
        /// </summary>
        public GInteractiveChangeState InteractiveChange { get { return this._InteractiveChange; } }
        /// <summary>
        /// Prvek, nad kterým se nacházela myš v době startu kreslení. Může být null, při pohybu nad controlem.
        /// </summary>
        public IInteractiveItem StartItem { get { return this._StartActiveItem?.ActiveItem; } }
        /// <summary>
        /// Absolutní souřadnice bodu, kde se nacházela myš v době startu kreslení. Může být null, při pohybu nad controlem.
        /// </summary>
        public Point? StartPoint { get { return this._StartActiveItem?.MouseAbsolutePoint; } }
        /// <summary>
        /// Prvek, nad kterým se aktuálně nachází myš, a to jak při pohybu nad controlem, tak při kreslení. 
        /// </summary>
        public IInteractiveItem CurrentItem { get { return this._CurrentActiveItem.ActiveItem; } }
        /// <summary>
        /// Absolutní souřadnice bodu, kde se aktuálně nachází myš
        /// </summary>
        public Point CurrentPoint { get { return this._CurrentActiveItem.MouseAbsolutePoint; } }
        /// <summary>
        /// Aktuální stav kláves
        /// </summary>
        public Keys CurrentModifierKeys { get { return this._CurrentActiveItem.CurrentModifierKeys; } }
        /// <summary>
        /// Data pro kreslení objektu.
        /// V režimu <see cref="InteractiveChange"/> == <see cref="GInteractiveChangeState.MouseOver"/> nemá význam, je null.
        /// V režimu kreslení je objekt inicializován na defaultní, a po všechny jednotlivé kroky kreslení je perzistentní.
        /// </summary>
        public MousePaintInfo PaintInfo { get { return this._MousePaintInfo; } set { this._MousePaintInfo = value; } }
        /// <summary>
        /// Data do tooltipu
        /// </summary>
        public ToolTipData ToolTipData { get { if (this._ToolTipData == null) this._ToolTipData = new ToolTipData(); return this._ToolTipData; } set { this._ToolTipData = value; } }
        /// <summary>
        /// Máme data do ToolTipu?
        /// </summary>
        public bool HasToolTipData { get { return (this._ToolTipData != null); } }
        /// <summary>
        /// Do této property eventhandler nastaví true / false podle toho, zda aktuální operace je / není povolená.
        /// Výchozí hodnota = false
        /// </summary>
        public bool IsEnabled { get; set; }
        /// <summary>
        /// Do této property eventhandler vloží požadovaný typ kurzoru.
        /// Pokud zde bude null (=výchozí hodnota), pak bude kurzor výchozí.
        /// </summary>
        public SysCursorType? CursorType { get; set; }
    }
    /// <summary>
    /// Definice kresleného obrazu
    /// </summary>
    public class MousePaintInfo
    {
        /// <summary>
        /// Defaultní obrazec = typ Curve, 5px, Yellow/DarkMagenta, Round lineCaps
        /// </summary>
        public static MousePaintInfo Default
        {
            get
            {
                MousePaintInfo paintInfo = new MousePaintInfo()
                {
                    ObjectType = MousePaintObjectType.SCurveVertical,
                    LineWidth = 5,
                    LineColor = Color.Yellow,
                    FillColor = Color.DarkMagenta,
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                return paintInfo;
            }
        }
        /// <summary>
        /// Druh kresleného objektu
        /// </summary>
        public MousePaintObjectType ObjectType { get; set; }
        /// <summary>
        /// Šířka čáry v pixelech
        /// </summary>
        public int LineWidth { get; set; }
        /// <summary>
        /// Barva čáry, null = nekreslí se
        /// </summary>
        public Color? LineColor { get; set; }
        /// <summary>
        /// Barva výplně (pro Rectangle a Ellipse), null = nekreslí se.
        /// Pro typ objektu Čára je zde barva stínování okolo čáry.
        /// </summary>
        public Color? FillColor { get; set; }
        /// <summary>
        /// Tvar zahájení linky
        /// </summary>
        public LineCap StartCap { get; set; }
        /// <summary>
        /// Tvar zakončení linky
        /// </summary>
        public LineCap EndCap { get; set; }
        /// <summary>
        /// Absolutní souřadnice bodu, kde má kresba počátek.
        /// Výchozí hodnota odpovídá reálné souřadnici v GUI, ale eventhandler může souřadnici korigovat, nebo dokonce prohodit s <see cref="EndPoint"/>.
        /// </summary>
        public Point? StartPoint { get; set; }
        /// <summary>
        /// Absolutní souřadnice bodu, kde má kresba konec.
        /// Výchozí hodnota odpovídá reálné souřadnici v GUI, ale eventhandler může souřadnici korigovat, nebo dokonce prohodit s <see cref="StartPoint"/>.
        /// </summary>
        public Point? EndPoint { get; set; }
        /// <summary>
        /// Obrázek
        /// </summary>
        public Image Image { get; set; }
        /// <summary>
        /// Libovolná User data
        /// </summary>
        public object UserData { get; set; }
        /// <summary>
        /// Vzájemně prohodí souřadnice <see cref="StartPoint"/> a <see cref="EndPoint"/>
        /// </summary>
        public void ExchangePoints()
        {
            Point? startPoint = this.StartPoint;
            Point? endPoint = this.EndPoint;
            this.StartPoint = endPoint;
            this.EndPoint = startPoint;
        }
    }
    /// <summary>
    /// Druh kresleného objektu v controlu, <see cref="InteractiveControl.MousePaintEnabled"/>
    /// </summary>
    public enum MousePaintObjectType
    {
        /// <summary>
        /// Nekreslí se nic
        /// </summary>
        None = 0,
        /// <summary>
        /// Přímá rovná čára z bodu Start do bodu End
        /// </summary>
        StraightLine,
        /// <summary>
        /// Esíčková křivka z bodu Start do bodu End, zleva doprava
        /// </summary>
        SCurveVertical,
        /// <summary>
        /// Esíčková křivka z bodu Start do bodu End, nahoru / dolů
        /// </summary>
        SCurveHorizontal,
        /// <summary>
        /// Rovná, lomená křivka, vodorovná : z bodu Start doprava/doleva, v polovině pak nahoru/dolů, a nakonec doprava/doleva do End
        /// </summary>
        ZigZagHorizontal,
        /// <summary>
        /// Rovná, lomená křivka, svislá : z bodu Start nahoru/dolů, v polovině pak doprava/doleva, a nakonec nahoru/dolů do End
        /// </summary>
        ZigZagVertical,
        /// <summary>
        /// Lomená čára optimální:
        /// Pokud konec má souřadnici X nižší než začátek (= časově zpětná), pak je vykreslena <see cref="ZigZagVertical"/> = nejprve dolů, pak zpátky, a pak zase dolů;
        /// Pokud konec má X rovno nebo vyšší, pak je vykreslena <see cref="ZigZagHorizontal"/> = nejprve doprava, pak nahoru/dolů, a pak doprava.
        /// </summary>
        ZigZagOptimal,
        /// <summary>
        /// Obdélník
        /// </summary>
        Rectangle,
        /// <summary>
        /// Elipsa
        /// </summary>
        Ellipse,
        /// <summary>
        /// Obrázek
        /// </summary>
        Image,
        /// <summary>
        /// Sám uživatel kreslí
        /// </summary>
        UserDraw
    }
    #endregion
    #region Enums ZOrder, GInteractiveState, GInteractiveChangeState, GInteractiveDrawLayer, DragResponseType, ProcessAction, EventSourceType
    /// <summary>
    /// Z-Order position of item.
    /// Order from bottom to top is: OnBackground - BelowStandard - Standard - AboveStandard - OnTop.
    /// </summary>
    public enum ZOrder : int
    {
        /// <summary>
        /// Standard position
        /// </summary>
        Standard = 0,
        /// <summary>
        /// Above all items in Standard order
        /// </summary>
        AboveStandard = 10,
        /// <summary>
        /// On top over all other order
        /// </summary>
        OnTop = 20,
        /// <summary>
        /// Below all items in Standard order
        /// </summary>
        BelowStandard = -10,
        /// <summary>
        /// On background below all other order
        /// </summary>
        OnBackground = -20
    }
    /// <summary>
    /// State of item by mouse activity. Not change of state (eg. Click, DragBegin and so on), only static status (Dragging, MouseDown, MouseOver, ...)
    /// </summary>
    [Flags]
    public enum GInteractiveState
    {
        /// <summary>
        /// Neurčeno, běžně se nevyskytuje
        /// </summary>
        None = 0,
        /// <summary>
        /// Povoleno, bez přítomnosti myši, připraveno k akci
        /// </summary>
        Enabled = 0x0001,
        /// <summary>
        /// Disabled, bez aktivity myši. Disabled nemůže dostat kurzor.
        /// </summary>
        Disabled = 0x0002,
        /// <summary>
        /// ReadOnly, bez aktivity myši. ReadOnly může dostat kurzor, ale nemůže změnit hodnotu.
        /// </summary>
        ReadOnly = 0x0004,
        /// <summary>
        /// S Focusem, bez ohledu na stav myši
        /// </summary>
        Focused = 0x0008,
        /// <summary>
        /// Příznak pohybu myši nad prvkem
        /// </summary>
        FlagOver = 0x0010,
        /// <summary>
        /// Příznak stisknutého tlačítka myši, ale dosud bez jejího pohybu, bez rozlišení tlačítka myši
        /// </summary>
        FlagDown = 0x0100,
        /// <summary>
        /// Příznak stavu Drag and Drop, bez rozlišení tlačítka myši
        /// </summary>
        FlagDrag = 0x0200,
        /// <summary>
        /// Příznak stavu SelectFrame, bez rozlišení tlačítka myši
        /// </summary>
        FlagFrame = 0x0400,
        /// <summary>
        /// Příznak levého (hlavního) tlačítka myši, bez rozlišení akce myši
        /// </summary>
        FlagLeftMouse = 0x1000,
        /// <summary>
        /// Příznak prostředního tlačítka myši, bez rozlišení akce myši
        /// </summary>
        FlagMiddleMouse = 0x2000,
        /// <summary>
        /// Příznak pravého (kontextového) tlačítka myši, bez rozlišení akce myši
        /// </summary>
        FlagRightMouse = 0x4000,
        /// <summary>
        /// Pohyb myši nad prvkem
        /// </summary>
        MouseOver = FlagOver,
        /// <summary>
        /// Levá myš stisknutá, bez pohybu
        /// </summary>
        LeftDown = FlagLeftMouse | FlagDown,
        /// <summary>
        /// Pravá myš stisknutá, bez pohybu
        /// </summary>
        RightDown = FlagRightMouse | FlagDown,
        /// <summary>
        /// Drag and Drop levou myší
        /// </summary>
        LeftDrag = FlagLeftMouse | FlagDrag,
        /// <summary>
        /// Drag and Drop pravou myší
        /// </summary>
        RightDrag = FlagRightMouse | FlagDrag,
        /// <summary>
        /// Označování FrameSelect levou myší
        /// </summary>
        LeftFrame = FlagLeftMouse | FlagFrame,
        /// <summary>
        /// Označování FrameSelect pravou myší
        /// </summary>
        RightFrame = FlagRightMouse | FlagFrame
    }
    /// <summary>
    /// State and Change of state by mouse activity, this is: static state and change of state from one static state to another.
    /// </summary>
    public enum GInteractiveChangeState
    {
        /// <summary>
        /// Žádná změna
        /// </summary>
        None = 0,
        /// <summary>
        /// Myš vstoupila nad prvek
        /// </summary>
        MouseEnter,
        /// <summary>
        /// Myš se pohybuje nad prvkem. 
        /// Akce se volá pouze pokud vlastnosti objektu mají nastaveno <see cref="InteractiveProperties.MouseMoveOver"/> == true.
        /// </summary>
        MouseOver,
        /// <summary>
        /// Myš se pohybuje nad prvkem, prvek není Enabled.
        /// Akce se volá pouze pokud vlastnosti objektu mají nastaveno <see cref="InteractiveProperties.MouseMoveOver"/> == true.
        /// </summary>
        MouseOverDisabled,
        /// <summary>
        /// Myš opustila prvek.
        /// </summary>
        MouseLeave,
        /// <summary>
        /// Stisknutí levého (hlavního) tlačítka myši.
        /// Po této akci může následovat akce Drag and Drop anebo Select Frame. 
        /// Anebo prosté zvednutí myši <see cref="LeftUp"/>,
        /// následované <see cref="LeftDoubleClick"/>, nebo <see cref="LeftLongClick"/>, nebo (<see cref="LeftClick"/> nebo <see cref="LeftClickSelect"/>), podle stylu kliknutí a vlastností.
        /// </summary>
        LeftDown,
        /// <summary>
        /// Zvednutí levého (hlavního) tlačítka myši.
        /// Tato událost je volána jen tehdy, když neprobíhal proces Drag and Drop a ani Select Frame.
        /// Po této události okamžitě bude volána jedna z akcí: 
        /// <see cref="LeftDoubleClick"/>, nebo <see cref="LeftLongClick"/>, nebo <see cref="LeftClickSelect"/> a <see cref="LeftClick"/>,
        /// podle stylu kliknutí a vlastností.
        /// </summary>
        LeftUp,
        /// <summary>
        /// Dvojclick levého (hlavního) tlačítka myši.
        /// Před touto událostí je vyvolána událost <see cref="LeftUp"/>.
        /// Jde o druhý click v krátké době po sobě.
        /// </summary>
        LeftDoubleClick,
        /// <summary>
        /// Dlouhý click levého (hlavního) tlačítka myši.
        /// Před touto událostí je vyvolána událost <see cref="LeftUp"/>.
        /// Dlouhý click levého tlačítka může sloužit k vyvolání kontextového menu stejně jako standardní akce <see cref="RightClick"/>,
        /// proto ihned po akci <see cref="LeftLongClick"/> je volána další akce: <see cref="GetContextMenu"/>.
        /// </summary>
        LeftLongClick,
        /// <summary>
        /// Click levým (hlavním) tlačítkem myši.
        /// Před touto událostí je vyvolána událost <see cref="LeftUp"/>,
        /// a pokud objekt má nastaveno <see cref="InteractiveProperties.Selectable"/> == true, 
        /// tak těsně před <see cref="LeftClick"/> proběhne i <see cref="LeftClickSelect"/>.
        /// </summary>
        LeftClick,

        /// <summary>
        /// Změna hodnoty <see cref="IInteractiveItem.IsSelected"/> pomocí levého tlačítka myši.
        /// Provádí se před akcí <see cref="LeftClick"/>, pro objekt který má nastaveno <see cref="InteractiveProperties.Selectable"/> == true.
        /// Teprve poté na tomto objektu proběhne akce <see cref="LeftClick"/>.
        /// Hodnota <see cref="IInteractiveItem.IsSelected"/> je již změněna, proběhla i metoda Repaint() na objektu.
        /// Aplikace nemusí již nijak reagovat, ale může.
        /// Před touto událostí je vyvolána událost <see cref="LeftUp"/>.
        /// </summary>
        LeftClickSelect,

        /// <summary>
        /// Událost je volána v okamžiku, kdy je jisté, že začíná proces DragMove na levé myši.
        /// Ihned po této události je volána událost <see cref="LeftDragMoveStep"/>.
        /// Tato událost je volána i následně při každém zaregistrovaném pohybu myši.
        /// Akce DragMove může být ukončena událostí <see cref="LeftDragMoveDone"/> (při běžném puštění myši), anebo <see cref="LeftDragMoveCancel"/> při stisku Escape.
        /// Na závěr je vždy vyvolána událost <see cref="LeftDragMoveEnd"/>.
        /// </summary>
        LeftDragMoveBegin,
        /// <summary>
        /// Událost je volána po každém kroku pohybu myši při procesu DragMove.
        /// Před tím je vyvolána událost <see cref="LeftDragMoveBegin"/>.
        /// Akce DragMove může být ukončena událostí <see cref="LeftDragMoveDone"/> (při běžném puštění myši), anebo <see cref="LeftDragMoveCancel"/> při stisku Escape.
        /// Na závěr je vždy vyvolána událost <see cref="LeftDragMoveEnd"/>.
        /// </summary>
        LeftDragMoveStep,
        /// <summary>
        /// Událost je volána po stisknutí Escape v procesu DragMove.
        /// Prvek se bude vracet na svoji původní pozici (ta je předána v args).
        /// Po tomto eventu bude volán event LeftDragEnd (immediatelly), ale nebude volán event LeftUp.
        /// </summary>
        LeftDragMoveCancel,
        /// <summary>
        /// Událost je volána na konci procesu DragMove, po zvednutí myši, pokud nedošlo k Cancel.
        /// Prvek je nyní umístěn na novou pozici.
        /// Po tomto eventu bude volán event <see cref="LeftDragMoveEnd"/> (okamžitě), ale nebude volán event LeftUp.
        /// </summary>
        LeftDragMoveDone,
        /// <summary>
        /// Událost je volána na konci procesu DragMove, a to jak po <see cref="LeftDragMoveDone"/>, tak po <see cref="LeftDragMoveCancel"/>.
        /// Úkolem je provedení společného úklidu po DragMove procesu.
        /// </summary>
        LeftDragMoveEnd,

        /// <summary>
        /// Výběr prvků stylem Drag and Frame, levá myš, začátek akce
        /// </summary>
        LeftDragFrameBegin,
        /// <summary>
        /// Výběr prvků stylem Drag and Frame, průběh akce
        /// </summary>
        LeftDragFrameSelect,
        /// <summary>
        /// Výběr prvků stylem Drag and Frame, konec akce
        /// </summary>
        LeftDragFrameDone,

        /// <summary>
        /// Stisknutí pravého (vedlejšího) tlačítka myši.
        /// Po této akci může následovat akce Drag and Drop anebo Select Frame. 
        /// Anebo prosté zvednutí myši <see cref="RightUp"/>,
        /// následované <see cref="RightDoubleClick"/>, nebo <see cref="RightLongClick"/>, nebo <see cref="RightClick"/>, podle stylu kliknutí.
        /// </summary>
        RightDown,
        /// <summary>
        /// Zvednutí pravého (vedlejšího) tlačítka myši.
        /// Tato událost je volána jen tehdy, když neprobíhal proces Drag and Drop a ani Select Frame.
        /// Po této události okamžitě bude volána jedna z akcí: 
        /// <see cref="RightDoubleClick"/>, nebo <see cref="RightLongClick"/>, nebo <see cref="RightClick"/>, podle stylu kliknutí.
        /// </summary>
        RightUp,
        /// <summary>
        /// Dvojclick pravého (vedlejšího) tlačítka myši.
        /// Před touto událostí je vyvolána událost <see cref="RightUp"/>.
        /// Jde o druhý click v krátké době po sobě.
        /// </summary>
        RightDoubleClick,
        /// <summary>
        /// Dlouhý click pravého (vedlejšího) tlačítka myši.
        /// Před touto událostí je vyvolána událost <see cref="RightUp"/>.
        /// Dlouhý click levého tlačítka může sloužit k vyvolání kontextového menu stejně jako standardní akce <see cref="RightClick"/>,
        /// proto ihned po akci <see cref="RightLongClick"/> je volána další akce: <see cref="GetContextMenu"/>.
        /// </summary>
        RightLongClick,
        /// <summary>
        /// Click pravým (vedlejším) tlačítkem myši.
        /// Před touto událostí je vyvolána událost <see cref="RightUp"/>.
        /// </summary>
        RightClick,

        /// <summary>
        /// Událost je volána v okamžiku, kdy je jisté, že začíná akce DragMove na pravé myši.
        /// </summary>
        RightDragMoveBegin,
        /// <summary>
        /// Událost je volána po každém kroku pohybu DragMove.
        /// </summary>
        RightDragMoveStep,
        /// <summary>
        /// Událost je volána po stisknutí Escape v procesu DragMove.
        /// Prvek se bude vracet na svoji původní pozici (ta je předána v args).
        /// Po tomto eventu bude volán event RightDragEnd (immediatelly), ale nebude volán event RightUp.
        /// </summary>
        RightDragMoveCancel,
        /// <summary>
        /// Událost je volána na konci procesu DragMove, po zvednutí myši, pokud nedošlo k Cancel.
        /// Prvek je nyní umístěn na novou pozici.
        /// Po tomto eventu bude volán event RightDragEnd (immediatelly), ale nebude volán event RightUp.
        /// </summary>
        RightDragMoveDone,
        /// <summary>
        /// Událost je volána na konci procesu DragMove, a to jak po Cancel, tak po Done.
        /// Úkolem je provedení společného úklidu po DragMove procesu.
        /// </summary>
        RightDragMoveEnd,

        /// <summary>
        /// Výběr prvků stylem Drag and Frame, pravá myš, začátek akce
        /// </summary>
        RightDragFrameBegin,
        /// <summary>
        /// Výběr prvků stylem Drag and Frame, pravá myš, průběh akce
        /// </summary>
        RightDragFrameSelect,
        /// <summary>
        /// Výběr prvků stylem Drag and Frame, pravá myš, konec akce
        /// </summary>
        RightDragFrameDone,

        /// <summary>
        /// Vstup klávesnicového focusu do prvku
        /// </summary>
        KeyboardFocusEnter,
        /// <summary>
        /// Klávesnicová akce: KeyPreview
        /// Je volána pro všechny klávesy, před akcí KeyDown
        /// </summary>
        KeyboardKeyPreview,
        /// <summary>
        /// Klávesnicová akce: KeyDown
        /// </summary>
        KeyboardKeyDown,
        /// <summary>
        /// Klávesnicová akce: KeyUp
        /// </summary>
        KeyboardKeyUp,
        /// <summary>
        /// Klávesnicová akce: KeyPress
        /// </summary>
        KeyboardKeyPress,
        /// <summary>
        /// Odchod klávesnicového focusu z prvku
        /// </summary>
        KeyboardFocusLeave,

        /// <summary>
        /// Kolečko myši, směr nahoru: v dokumentu směr k nižším číslům řádku
        /// </summary>
        WheelUp,
        /// <summary>
        /// Kolečko myši, směr dolů: v dokumentu směr k vyšším číslům řádku
        /// </summary>
        WheelDown,

        /// <summary>
        /// Získání kontextového menu
        /// </summary>
        GetContextMenu
    }
    /// <summary>
    /// Layers to draw
    /// </summary>
    [Flags]
    public enum GInteractiveDrawLayer : int
    {
        /// <summary>
        /// This layer is not drawed. Objects on this layer are not visible.
        /// </summary>
        None = 0,
        /// <summary>
        /// Standard layer (static image)
        /// </summary>
        Standard = 1,
        /// <summary>
        /// Interactive layer (image from static layer, during drag operation)
        /// </summary>
        Interactive = 2,
        /// <summary>
        /// Dynamic layer (lines above standard and interactive layers)
        /// </summary>
        Dynamic = 4
    }
    /// <summary>
    /// Akce volané v průběhu Drag and Drop pro konkrétní objekt
    /// </summary>
    public enum DragActionType : int
    {
        /// <summary>
        /// Nic
        /// </summary>
        None,
        /// <summary>
        /// Začátek přetahování daného objektu
        /// </summary>
        DragThisStart,
        /// <summary>
        /// Průběh přetahování daného objektu
        /// </summary>
        DragThisMove,
        /// <summary>
        /// Zrušení přetahování daného objektu (ESCAPE při stisknuté myši)
        /// </summary>
        DragThisCancel,
        /// <summary>
        /// Umístění přetahovaného objektu na nové místo = uvolnění tlačítka myši bez předchozího ESCAPE
        /// </summary>
        DragThisDrop,
        /// <summary>
        /// Dokončení přetahování daného objektu = jak stornovaného, tak platného (společná akce pro úklid)
        /// </summary>
        DragThisEnd,
        /// <summary>
        /// Nad this objektem se právě přetahuje jiný objekt
        /// </summary>
        DragAnotherMove,
        /// <summary>
        /// Do this objektu byl umístěn jiný objekt (na přemisťovaném objektu proběhla akce <see cref="DragActionType.DragThisDrop"/>.
        /// </summary>
        DragAnotherDrop
    }
    /// <summary>
    /// Type of response to drag event on object
    /// </summary>
    public enum DragResponseType
    {
        /// <summary>
        /// No response
        /// </summary>
        None,
        /// <summary>
        /// Response after DragEnd
        /// </summary>
        AfterDragEnd,
        /// <summary>
        /// Response on each change in DragMove
        /// </summary>
        InDragMove
    }
    /// <summary>
    /// Action to be taken during SetValue() method
    /// </summary>
    [Flags]
    public enum ProcessAction
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None = 0,
        /// <summary>
        /// Recalc and store new Value
        /// </summary>
        RecalcValue = 0x0001,
        /// <summary>
        /// Recalc and store new Scale
        /// </summary>
        RecalcScale = 0x0002,
        /// <summary>
        /// Recalc Inner Data (Arrangement, CurrentSet from Scale on Axis; InnerBounds on ScrollBar; and so on)
        /// </summary>
        RecalcInnerData = 0x0004,
        /// <summary>
        /// Prepare Inner Items (Ticks, SubItems) from Value and other data (Arrangement, CurrentSet on Axis; SubItems on ScrollBar; and so on)
        /// </summary>
        PrepareInnerItems = 0x0008,
        /// <summary>
        /// Call all events about current changing of property (during Drag process)
        /// </summary>
        CallChangingEvents = 0x0010,
        /// <summary>
        /// Call all events about change of property
        /// </summary>
        CallChangedEvents = 0x0020,
        /// <summary>
        /// Call event for Synchronize slave objects
        /// </summary>
        CallSynchronizeSlave = 0x0100,
        /// <summary>
        /// Call Draw events
        /// </summary>
        CallDraw = 0x0200,
        /// <summary>
        /// Všechny akce po změně hodnoty (nikoli ale CallChangingEvents!)
        /// </summary>
        ChangeAll = RecalcValue | RecalcScale | RecalcInnerData | PrepareInnerItems | CallChangedEvents | CallSynchronizeSlave | CallDraw,

        /// <summary>
        /// Combined value for Silent SetBounds: (RecalcValue | PrepareInnerItems), not CallChangedEvents nor CallDraw
        /// </summary>
        SilentBoundActions = RecalcValue | PrepareInnerItems,
        /// <summary>
        /// Combined value for Silent SetValue: (RecalcValue | RecalcInnerData | PrepareInnerItems), not CallChangedEvents nor CallDraw
        /// </summary>
        SilentValueActions = RecalcValue | RecalcScale | RecalcInnerData | PrepareInnerItems,
        /// <summary>
        /// Action during Drag an value interactively. 
        /// Contain only RecalcValue and CallChangingEvents action (RecalcValue = Align value to ValueRange, CallChangingEvents = change is in process).
        /// Do not contain actions: RecalcValue nor RecalcInnerData, PrepareInnerItems, CallDraw.
        /// </summary>
        DragValueActions = RecalcValue | CallChangingEvents,
        /// <summary>
        /// Combined value for Silent SetValue: (RecalcValue | RecalcInnerData | PrepareInnerItems) | CallChangedEvents, not RecalcInnerData + PrepareInnerItems
        /// </summary>
        SilentValueDrawActions = SilentValueActions | CallDraw
    }
    /// <summary>
    /// Režim překreslení objektu Parent při překreslení objektu this.
    /// V naprosté většině případů není při překreslení this objektu zapotřebí překreslovat objekt Parent.
    /// Exitují výjimky, typicky pokud aktuální objekt používá průhlednost (ve své BackColor nebo jinou cestou ne-kreslené pozadí), 
    /// pak může být zapotřebí vykreslit nejprve Parent objekt, a teprve na něj this objekt.
    /// </summary>
    public enum RepaintParentMode
    {
        /// <summary>
        /// Překreslení this objektu nevyžaduje předchozí vykreslení Parenta
        /// </summary>
        None,
        /// <summary>
        /// Překreslení this objektu vyžaduje nejprve vykreslení Parenta, pokud this.BackColor má hodnotu A menší než 255 (tzn. naše pozadí je trochu nebo úplně průhledné)
        /// </summary>
        OnBackColorAlpha,
        /// <summary>
        /// Překreslení this objektu bezpodmínečně vyžaduje nejprve vykreslení Parenta
        /// </summary>
        Always
    }
    /// <summary>
    /// Výsledek jednoho kroku animace
    /// </summary>
    [Flags]
    public enum AnimationResult
    {
        /// <summary>
        /// Nic není třeba (ani kreslit, ani ukončit animaci)
        /// </summary>
        None = 0,
        /// <summary>
        /// Je třeba vykreslit prvek
        /// </summary>
        DrawItem = 1,
        /// <summary>
        /// Je třeba vykreslit všechny prvky
        /// </summary>
        DrawAll = 2,
        /// <summary>
        /// Aktuální animační funkci je možno ukončit (vyřadit z animačního vlákna)
        /// </summary>
        Stop = 4
    }
    #endregion
}
