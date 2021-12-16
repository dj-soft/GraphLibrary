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
using DevExpress.XtraRichEdit.Layout;
using System.Diagnostics;
using DevExpress.Utils.Svg;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Extensions metody pro grafické třídy (z namespace System.Drawing)
    /// </summary>
    public static class DrawingExtensions
    {
        #region Control
        /// <summary>
        /// Vrátí IDisposable blok, který na svém počátku (při vyvolání této metody) provede control?.Parent.SuspendLayout(), 
        /// a na konci bloku (při Dispose) provede control?.Parent.ResumeLayout(false)
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static IDisposable ScopeSuspendParentLayout(this Control control)
        {
            return new ActionScope(
            (s) =>
            {   // OnBegin (Constructor):
                Control parent = control?.Parent;
                if (parent != null && !parent.IsDisposed)
                {
                    parent.SuspendLayout();
                }
                s.UserData = parent;
            },
            (s) =>
            {   // OnEnd (Dispose):
                Control parent = s.UserData as Control;
                if (parent != null && !parent.IsDisposed)
                {
                    parent.ResumeLayout(false);
                    parent.PerformLayout();
                }
                s.UserData = null;
            }
            );
        }
        /// <summary>
        /// Zajistí vložení daného controlu (this) do daného parenta, pokud tam není.
        /// Pokud by control před tím byl v nějakém jiném parentu (než je požadován), odebere jej tamodtud.
        /// <para/>
        /// Před změnou provede volitelně zhasnutí controlu.
        /// <para/>
        /// Informace: jde o extension metodu, a nijak jí nevadí, když je provedena "na objektu" který je null.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="parent"></param>
        /// <param name="hideControl">Před jakoukoli změnou nastavit <see cref="Control.Visible"/> = false. Pokud by ale ke změně nedošlo, nechá se Visible beze změny.</param>
        public static void AddControlToParent(this Control control, Control parent, bool hideControl = false)
        {
            if (control == null || parent == null) return;
            if (control.Parent != null && !Object.ReferenceEquals(control.Parent, parent))
            {   // Pokud mám parenta, a ten je jiný než má být:
                if (hideControl) control.Visible = false;
                control.Parent.Controls.Remove(control);
            }
            if (control.Parent == null)
            {   // Pokud nemám parenta:
                if (hideControl) control.Visible = false;
                parent.Controls.Add(control);
            }
        }
        /// <summary>
        /// Zajistí odebrání daného controlu z daného parenta, pokud tam je.
        /// Pokud je parent v parametru zadán, pak z něj control odebere pouze tehdy, pokud control je právě v tomto parentu.
        /// Pokud parent v parametru zadán není, pak daný control odebere z jakéhokoli parenta, poku v nějakém je.
        /// <para/>
        /// Před změnou provede volitelně zhasnutí controlu.
        /// <para/>
        /// Informace: jde o extension metodu, a nijak jí nevadí, když je provedena "na objektu" který je null.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="parent"></param>
        /// <param name="hideControl">Před jakoukoli změnou nastavit <see cref="Control.Visible"/> = false. Pokud by ale ke změně nedošlo, nechá se Visible beze změny.</param>
        public static void RemoveControlFromParent(this Control control, Control parent = null, bool hideControl = false)
        {
            if (control == null) return;
            if (hideControl) control.Visible = false;
            if (control.Parent != null && (parent == null || Object.ReferenceEquals(control.Parent, parent)))
                control.Parent.Controls.Remove(control);
        }
        /// <summary>
        /// Vrací defaultní ToString() = Type.Name + Control.Name
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static string GetTypeName(this Control control)
        {
            string text = control?.GetType().Name ?? "NULL";
            if (!String.IsNullOrEmpty(control?.Name))
                text += ": '" + control.Name + "'";
            return text;
        }
        /// <summary>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato metoda <see cref="IsSetVisible(Control)"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static bool IsSetVisible(this Control control)
        {
            if (control is null) return false;
            var getState = control.GetType().GetMethod("GetState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic);
            if (getState is null) return false;
            object visible = getState.Invoke(control, new object[] { (int)0x02  /*STATE_VISIBLE*/  });
            return (visible is bool ? (bool)visible : false);
        }
        /// <summary>
        /// Prohledá hierarchii controlů počínaje od this (včetně) směrem k Parentům.
        /// Každý prvek hierarchie otestuje daným filtrem, a pokud prvek vyhovuje, pak vrátí jeho selectovaný objekt.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control">this control, kde hledání začíná</param>
        /// <param name="filter">Podmínka: pokud této podmínce control vyhovuje, pak bude akceptován</param>
        /// <param name="selector">Selector: z nalezeného controlu vybere a vrátí hodnotu</param>
        /// <param name="skipThis">Přeskočit this control (true) / aplikovat filtr a případně akceptovat i this control (false)</param>
        /// <param name="result">Out nalezený výstup selectoru (to když výstupem metody je true)</param>
        /// <returns></returns>
        public static bool TrySearchUpForControl<T>(this Control control, Func<Control, bool> filter, Func<Control, T> selector, bool skipThis, out T result)
        {
            if (filter == null) throw new ArgumentNullException($"TrySearchUpForControl() error: filter is null.");
            if (selector == null) throw new ArgumentNullException($"TrySearchUpForControl() error: selector is null.");

            Control item = skipThis ? control?.Parent : control;     // Hledání začne na našem Parentu (true) / na this instanci (false)
            while (item != null)
            {
                if (filter(item))
                {
                    result = selector(item);
                    return true;
                }
                item = item.Parent;
            }
            result = default;
            return false;
        }
        /// <summary>
        /// Metoda vyhledá takový Child control v this controlu, který se nachází jako nejhlubší Child control na dané souřadnici.
        /// Souřadnice na vstupu je dána jako absolutní (v koordinátech Screen).
        /// <para/>
        /// Volitelně může být zadána podmínka <paramref name="searchForChild"/>: pokud je zadaná, pak hledání najde první výskyt child controlu, který vyhovuje dané podmínce.
        /// Je tedy možno tímto filtrem najít určitou komponentu na dané souřadnici, když hierarchie controlů je hluboká (Splittery, TabPages, panely, vnořené...),
        /// a my nehledáme nejhlubší Child control (kterým může být nějaký malý button nebo textový editor), ale hledáme například Grid nebo DataForm.
        /// </summary>
        /// <param name="control">V tomto controlu hledání začíná. I on musí být na zadané souřadnici. Pokud on sám vyhovuje filtru <paramref name="searchForChild"/>, bude vrácen.</param>
        /// <param name="screenTargetPoint">Souřadnice absolutní (Screen)</param>
        /// <param name="skip">Definice přeskakování controlů (mají li se ignorovat controly neviditelné, disablované anebo transparentní)</param>
        /// <param name="result">Výstup nalezeného Child controlu</param>
        /// <param name="searchForChild">Optional Podmínka pro hledání Childu</param>
        /// <returns></returns>
        public static bool TryGetChildAtPoint(this Control control, Point screenTargetPoint, GetChildAtPointSkip skip, out Control result, Func<Control, bool> searchForChild = null)
        {
            result = null;
            bool hasFilter = (searchForChild != null);
            for (int i = 0; i < 100; i++)
            {   // Timeout:
                if (control == null) break;
                Point controlPoint = control.PointToClient(screenTargetPoint);
                if (!control.ClientRectangle.Contains(controlPoint)) break;              // Daný bod se nenachází v klientské oblasti daného controlu = bod je mimo: skončíme, v result je poslední platný control (nebo null)
                result = control;
                if (hasFilter && searchForChild(control)) return true;                   // Nalezený Child vyhovuje filtru: result je nalezen a vracíme true, a dál do hloubky nehledáme.

                var child = control.GetChildAtPoint(controlPoint, skip);
                if (child == null || Object.ReferenceEquals(child, control)) break;      // Daný bod je sice v klientské oblasti daného controlu, ale na dané souřadnici není žádný Child control: skončíme, v result je aktuální control
                control = child;
            }

            // Pokud máme zadaný filtr, ale skončili jsme bez výsledku, pak nemůžeme vrátit nalezený nejhlubší Child,
            // protože on nevyhovuje danému filtru (to bychom byli bývali skončili už dříve:    return true; ):
            // Tedy "nenašli jsme nic":
            if (hasFilter) result = null;

            return (result != null);
        }
        /// <summary>
        /// Vrátí nejbližšího Parenta požadovaného typu pro this control.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control"></param>
        /// <returns></returns>
        public static T SearchForParentOfType<T>(this Control control) where T : Control
        {
            Control item = control?.Parent;                // Tímhle řádkem zajistím, že nebudu vracet vstupní objekt, i kdyby byl požadovaného typu = protože hledám Parenta, nikoli sebe.
            while (item != null)
            {
                if (item is T result) return result;
                item = item.Parent;
            }
            return null;
        }
        /// <summary>
        /// Korektně disposuje všechny Child prvky.
        /// </summary>
        /// <param name="control"></param>
        public static void DisposeContent(this Control control)
        {
            if (control == null || control.IsDisposed || control.Disposing) return;

            var childs = control.Controls.OfType<System.Windows.Forms.Control>().ToArray();
            foreach (var child in childs)
            {
                if (child == null || child.IsDisposed || child.Disposing) continue;

                if (child is DevExpress.XtraEditors.XtraScrollableControl xsc)
                {
                    xsc.AutoScroll = false;
                }
                if (child is System.Windows.Forms.ScrollableControl wsc)
                {
                    wsc.AutoScroll = false;
                }

                control.Controls.Remove(child);

                try { child.Dispose(); }
                catch { }
            }
        }
        /// <summary>
        /// Do this controlu vloží potřebné souřadnice, pokud jsou změněny.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="newBounds"></param>
        public static void SetBounds(this Control control, Rectangle newBounds)
        {
            if (control != null)
            {
                var oldBounds = control.Bounds;
                bool equalLocation = (newBounds.Location == oldBounds.Location);
                bool equalSize = (newBounds.Size == oldBounds.Size);
                if (!equalLocation && !equalSize)
                    control.Bounds = newBounds;
                else if (!equalSize)
                    control.Size = newBounds.Size;
                else if (!equalLocation)
                    control.Location = newBounds.Location;
            }
        }
        /// <summary>
        /// Do this controlu vloží zadanou velikost, pokud je změněna.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="newSize"></param>
        public static void SetSize(this Control control, Size newSize)
        {
            if (control != null)
            {
                var oldSize = control.Size;
                bool equalSize = (newSize == oldSize);
                if (!equalSize)
                    control.Size = newSize;
            }
        }
        /// <summary>
        /// Vrátí souřadnice prostoru, do kterého lze v this controlu pozicovat jeho Child.
        /// Jde o <see cref="Control.ClientSize"/> zmenšený o <see cref="Control.Padding"/>
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static Rectangle GetInnerBounds(this Control control)
        {
            if (control == null) return Rectangle.Empty;
            Size clientSize = control.ClientSize;
            Padding padding = control.Padding;
            int x = padding.Left;
            int y = padding.Top;
            int w = clientSize.Width - padding.Horizontal;
            int h = clientSize.Height - padding.Vertical;
            if (w <= 0 || h <= 0) return Rectangle.Empty;
            return new Rectangle(padding.Left, padding.Top, clientSize.Width - padding.Horizontal, clientSize.Height - padding.Vertical);
        }
        #endregion
        #region Invoke to GUI: run, get, set
        /// <summary>
        /// Metoda provede danou akci v GUI threadu.
        /// Pokud aktuální thread je GUI thread (tedy this control nepotřebuje invokaci), pak se akce provede nativně v Current threadu.
        /// Jinak se použije synchronní Invoke().
        /// </summary>
        /// <param name="control"></param>
        /// <param name="action"></param>
        public static void RunInGui(this Control control, Action action)
        {
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }
        /// <summary>
        /// Metoda vrátí hodnotu z GUI prvku, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T GetGuiValue<T>(this Control control, Func<T> reader)
        {
            if (control.InvokeRequired)
                return (T)control.Invoke(reader);
            else
                return reader();
        }
        /// <summary>
        /// Metoda vloží do GUI prvku danou hodnotu, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control"></param>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        public static void SetGuiValue<T>(this Control control, Action<T> writer, T value)
        {
            if (control.InvokeRequired)
                control.Invoke(writer, value);
            else
                writer(value);
        }
        #endregion
        #region Color: Shift
        /// <summary>
        /// Vrací danou barvu s daným posunutím
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="shift">Posunutí barvy</param>
        /// <returns></returns>
        public static Color Shift(this Color root, float shift)
        {
            float r = (float)root.R + shift;
            float g = (float)root.G + shift;
            float b = (float)root.B + shift;
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Vrací danou barvu s daným posunutím
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="shiftR">Posunutí barvy pro složku R</param>
        /// <param name="shiftG">Posunutí barvy pro složku G</param>
        /// <param name="shiftB">Posunutí barvy pro složku B</param>
        /// <returns></returns>
        public static Color Shift(this Color root, float shiftR, float shiftG, float shiftB)
        {
            float r = (float)root.R + shiftR;
            float g = (float)root.G + shiftG;
            float b = (float)root.B + shiftB;
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Vrací danou barvu s daným posunutím
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="shiftA">Posunutí barvy pro složku A</param>
        /// <param name="shiftR">Posunutí barvy pro složku R</param>
        /// <param name="shiftG">Posunutí barvy pro složku G</param>
        /// <param name="shiftB">Posunutí barvy pro složku B</param>
        /// <returns></returns>
        public static Color Shift(this Color root, float shiftA, float shiftR, float shiftG, float shiftB)
        {
            float a = (float)root.A + shiftA;
            float r = (float)root.R + shiftR;
            float g = (float)root.G + shiftG;
            float b = (float)root.B + shiftB;
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrací danou barvu s daným posunutím
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="shift">Posunutí barvy ve struktuře Color: jednotlivé složky nesou offset, kde hodnota 128 odpovídá posunu 0</param>
        /// <returns></returns>
        public static Color Shift(this Color root, Color shift)
        {
            float r = (float)(root.R + shift.R - 128);
            float g = (float)(root.G + shift.G - 128);
            float b = (float)(root.B + shift.B - 128);
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Vrací barvu dle daných složek, přičemž složky (a,r,g,b) omezuje do rozsahu 0 - 255.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static Color GetColor(float a, float r, float g, float b)
        {
            int ac = (a < 0f ? 0 : (a > 255f ? 255 : (int)a));
            int rc = (r < 0f ? 0 : (r > 255f ? 255 : (int)r));
            int gc = (g < 0f ? 0 : (g > 255f ? 255 : (int)g));
            int bc = (b < 0f ? 0 : (b > 255f ? 255 : (int)b));
            return Color.FromArgb(ac, rc, gc, bc);
        }
        #endregion
        #region Color: Change
        /// <summary>
        /// Změní barvu.
        /// Změna (Change) není posun (Shift): shift přičítá / odečítá hodnotu, ale změna hodnotu mění koeficientem.
        /// Pokud je hodnota složky například 170 a koeficient změny 0.25, pak výsledná hodnota je +25% od výchozí hodnoty směrem k maximu (255): 170 + 0.25 * (255 - 170).
        /// Obdobně změna dolů -70% z hodnoty 170 dá výsledek 170 - 0.7 * (170).
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="change">Změna složek</param>
        /// <returns></returns>
        public static Color ChangeColor(this Color root, float change)
        {
            float r = ChangeCC(root.R, change);
            float g = ChangeCC(root.G, change);
            float b = ChangeCC(root.B, change);
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Změní barvu.
        /// Změna (Change) není posun (Shift): shift přičítá / odečítá hodnotu, ale změna hodnotu mění koeficientem.
        /// Pokud je hodnota složky například 170 a koeficient změny 0.25, pak výsledná hodnota je +25% od výchozí hodnoty směrem k maximu (255): 170 + 0.25 * (255 - 170).
        /// Obdobně změna dolů -70% z hodnoty 170 dá výsledek 170 - 0.7 * (170).
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="changeR">Změna složky R</param>
        /// <param name="changeG">Změna složky R</param>
        /// <param name="changeB">Změna složky R</param>
        /// <returns></returns>
        public static Color ChangeColor(this Color root, float changeR, float changeG, float changeB)
        {
            float r = ChangeCC(root.R, changeR);
            float g = ChangeCC(root.G, changeG);
            float b = ChangeCC(root.B, changeB);
            return GetColor(root.A, r, g, b);
        }
        /// <summary>
        /// Vrátí složku změněnou koeficientem.
        /// </summary>
        /// <param name="colorComponent"></param>
        /// <param name="change"></param>
        /// <returns></returns>
        private static float ChangeCC(int colorComponent, float change)
        {
            float result = (float)colorComponent;
            if (change > 0f)
            {
                result = result + (change * (255f - result));
            }
            else if (change < 0f)
            {
                result = result - (-change * result);
            }
            return result;
        }
        #endregion
        #region Color: Morph
        /// <summary>
        /// Vrací barvu, která je výsledkem interpolace mezi barvou this a barvou other, 
        /// přičemž od barvy this se liší poměrem morph.
        /// Poměr (morph): 0=vrací se výchozí barva (this).
        /// Poměr (morph): 1=vrací se barva cílová (other).
        /// Poměr může být i větší než 1 (pak je výsledek ještě za cílovou barvou other),
        /// anebo může být záporný (pak výsledkem je barva na opačné straně než je other).
        /// Hodnota Alpha (=opacity = průhlednost) kanálu se přebírá z this barvy a Morphingem se nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="other">Cílová barva</param>
        /// <param name="morph">Poměr morph (0=vrátí this, 1=vrátí other, hodnota může být záporná i větší než 1f)</param>
        /// <returns></returns>
        public static Color Morph(this Color root, Color other, float morph)
        {
            if (morph == 0f) return root;
            float a = root.A;
            float r = GetMorph(root.R, other.R, morph);
            float g = GetMorph(root.G, other.G, morph);
            float b = GetMorph(root.B, other.B, morph);
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrací barvu, která je výsledkem interpolace mezi barvou this a barvou other, 
        /// přičemž od barvy this se liší poměrem morph.
        /// Poměr morph zde není zadán explicitně, ale je dán hodnotou Alpha kanálu v barvě other (kde 0 odpovídá morph = 0, a 255 odpovídá 1).
        /// Jinými slovy, barva this se transformuje do barvy other natolik výrazně, jak výrazně je barva other viditelná (neprůhledná).
        /// Nelze tedy provádět Morph na opačnou stranu (morph nebude nikdy záporné) ani s přesahem za cílovou barvu (morph nebude nikdy vyšší než 1).
        /// Poměr (Alpha kanál barvy other): 0=vrací se výchozí barva (this).
        /// Poměr (Alpha kanál barvy other): 255=vrací se barva cílová (other).
        /// Poměr tedy nemůže být menší než 0 nebo větší než 1 (255).
        /// Hodnota Alpha výsledné barvy (=opacity = průhlednost) se přebírá z Alpha kanálu this barvy, a tímto Morphingem se nijak nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="other">Cílová barva</param>
        /// <returns></returns>
        public static Color Morph(this Color root, Color other)
        {
            if (other.A == 0) return root;
            float morph = ((float)other.A) / 255f;
            float a = root.A;
            float r = GetMorph(root.R, other.R, morph);
            float g = GetMorph(root.G, other.G, morph);
            float b = GetMorph(root.B, other.B, morph);
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrátí složku barvy vzniklou morphingem = interpolací.
        /// </summary>
        /// <param name="root">Výchozí složka</param>
        /// <param name="other">Cílová složka</param>
        /// <param name="morph">Poměr morph (0=vrátí this, 1=vrátí other, hodnota může být záporná i větší než 1f)</param>
        /// <returns></returns>
        private static float GetMorph(float root, float other, float morph)
        {
            float dist = other - root;
            return root + morph * dist;
        }
        #endregion
        #region Color: Contrast
        /// <summary>
        /// Vrátí kontrastní barvu černou nebo bílou k barvě this.
        /// Tato metoda vrací barvu černou nebo bílou, která je dobře viditelná na pozadí dané barvy (this).
        /// Tato metoda pracuje s fyziologickým jasem každé složky barvy zvlášť (například složka G se jeví jasnější než B, složka R má svůj jas někde mezi nimi).
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <returns></returns>
        public static Color Contrast(this Color root)
        {
            // Vypočítám souhrnný jas všech složek se zohledněním koeficientu jejich barevného jasu:
            float rgb = 1.0f * (float)root.R +
                        1.4f * (float)root.G +
                        0.7f * (float)root.B;
            return (rgb >= 395 ? Color.Black : Color.White);      // Součet složek je 0 až 790.5, střed kontrastu je 1/2 = 395
        }
        /// <summary>
        /// Vrátí barvu, která je kontrastní vůči barvě this.
        /// Kontrastní barva leží o dané množství barvy směrem k protilehlé barvě (vždy na opačnou stranu od hodnoty 128), v každé složce zvlášť.
        /// Například ke složce s hodnotou 160 je kontrastní barvou o 32 hodnota (160-32) = 128, k hodnotě 100 o 32 je kontrastní (100+32) = 132.
        /// Tedy kontrastní barva k barvě ((rgb: 64,96,255), contrast=32) je barva: rgb(96,128,223) = (64+32, 96+32, 255-32).
        /// Složka A se nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="contrast">Míra kontrastu</param>
        /// <returns></returns>
        public static Color Contrast(this Color root, int contrast)
        {
            float a = root.A;
            float r = GetContrast(root.R, contrast);
            float g = GetContrast(root.G, contrast);
            float b = GetContrast(root.B, contrast);
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrátí barvu, která je kontrastní vůči barvě this.
        /// Kontrastní barva leží o dané množství barvy směrem k protilehlé barvě (vždy na opačnou stranu od hodnoty 128), v každé složce zvlášť.
        /// Například ke složce s hodnotou 160 je kontrastní barvou o 32 hodnota (160-32) = 128, k hodnotě 100 o 32 je kontrastní (100+32) = 132.
        /// Tedy kontrastní barva k barvě ((rgb: 64,96,255), contrast=32) je barva: rgb(96,128,223) = (64+32, 96+32, 255-32).
        /// Složka A se nemění.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="contrastR">Míra kontrastu ve složce R</param>
        /// <param name="contrastG">Míra kontrastu ve složce G</param>
        /// <param name="contrastB">Míra kontrastu ve složce B</param>
        /// <returns></returns>
        public static Color Contrast(this Color root, int contrastR, int contrastG, int contrastB)
        {
            float a = root.A;
            float r = GetContrast(root.R, contrastR);
            float g = GetContrast(root.G, contrastG);
            float b = GetContrast(root.B, contrastB);
            return GetColor(a, r, g, b);
        }
        /// <summary>
        /// Vrací kontrastní složku
        /// </summary>
        /// <param name="root"></param>
        /// <param name="contrast"></param>
        /// <returns></returns>
        private static float GetContrast(int root, int contrast)
        {
            return (root <= 128 ? root + contrast : root - contrast);
        }
        #endregion
        #region Color: GrayScale
        /// <summary>
        /// Vrátí danou barvu odbarvenou do černo-šedo-bílé stupnice.
        /// Hodnotu Alpha ponechává.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <returns></returns>
        public static Color GrayScale(this Color root)
        {
            // Vypočítám souhrnný jas všech složek se zohledněním koeficientu jejich barevného jasu:
            float rgb = 1.0f * (float)root.R +
                        1.4f * (float)root.G +
                        0.7f * (float)root.B;              // Součet složek je 0 až 790.5;
            int g = (int)(Math.Round((255f * (rgb / 790.5f)), 0));
            return Color.FromArgb(root.A, g, g, g);
        }
        /// <summary>
        /// Vrátí danou barvu odbarvenou do černo-šedo-bílé stupnice s daným poměrem odbarvení.
        /// Hodnotu Alpha ponechává.
        /// </summary>
        /// <param name="root">Výchozí barva</param>
        /// <param name="ratio">Poměr odbarvení</param>
        /// <returns></returns>
        public static Color GrayScale(this Color root, float ratio)
        {
            Color gray = root.GrayScale();
            return root.Morph(gray, ratio);
        }
        #endregion
        #region Color: Opacity
        /// <summary>
        /// Do dané barvy (this) vloží danou hodnotu Alpha (parametr opacity), výsledek vrátí.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="opacity">Průhlednost v hodnotě 0-255 (nebo null = neměnit)</param>
        /// <returns></returns>
        public static Color SetOpacity(this Color root, Int32? opacity)
        {
            if (!opacity.HasValue) return root;
            int alpha = (opacity.Value < 0 ? 0 : (opacity.Value > 255 ? 255 : opacity.Value));
            return Color.FromArgb(alpha, root);
        }
        /// <summary>
        /// Do dané barvy (this) vloží danou hodnotu Alpha (parametr opacityRatio), výsledek vrátí.
        /// Hodnota opacityRatio : Průhlednost v hodnotě ratio 0.0 ÷1.0 (nebo null = neměnit)
        /// </summary>
        /// <param name="root"></param>
        /// <param name="opacityRatio">Průhlednost v hodnotě ratio 0.0 ÷1.0 (nebo null = neměnit)</param>
        /// <returns></returns>
        public static Color SetOpacity(this Color root, float? opacityRatio)
        {
            if (!opacityRatio.HasValue) return root;
            return SetOpacity(root, (int)(255f * opacityRatio.Value));
        }
        /// <summary>
        /// Na danou barvu aplikuje všechny dodané hodnoty průhlednosti, při akceptování i původní průhlednosti.
        /// Aplikování se provádní vzájemným násobením hodnoty (opacity/255), což je poměr (ratio) průhlednosti.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="opacities"></param>
        /// <returns></returns>
        public static Color ApplyOpacity(this Color root, params Int32?[] opacities)
        {
            float alpha = _GetColorRatio(root.A);
            foreach (Int32? opacity in opacities)
            {
                if (opacity.HasValue)
                    alpha = alpha * _GetColorRatio(opacity.Value);
            }
            return SetOpacity(root, (int)(Math.Round(255f * alpha, 0)));
        }
        /// <summary>
        /// Vrací ratio { 0.00 až 1.00 } z hodnoty { 0 až 255 }.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static float _GetColorRatio(int value)
        {
            if (value < 0) return 0f;
            if (value >= 255) return 1f;
            return (float)value / 255f;
        }
        /// <summary>
        /// Na danou barvu aplikuje všechny dodané hodnoty průhlednosti, při akceptování i původní průhlednosti.
        /// Aplikování se provádní vzájemným násobením hodnoty (ratio), což je poměr (ratio) průhlednosti.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="ratios"></param>
        /// <returns></returns>
        public static Color ApplyOpacity(this Color root, params float?[] ratios)
        {
            float alpha = _GetColorRatio(root.A);
            foreach (float? ratio in ratios)
            {
                if (ratio.HasValue)
                    alpha = alpha * _GetColorRatio(ratio.Value);
            }
            return SetOpacity(root, (int)(Math.Round(255f * alpha, 0)));
        }
        /// <summary>
        /// Zarovná dané ratio do rozmezí { 0.00 až 1.00 }.
        /// </summary>
        /// <param name="ratio"></param>
        /// <returns></returns>
        private static float _GetColorRatio(float ratio)
        {
            return (ratio < 0f ? 0f : (ratio > 1f ? 1f : ratio));
        }
        /// <summary>
        /// Metoda vrátí novou instanci barvy this, kde její Alpha je nastavena na daný poměr (transparent) původní hodnoty.
        /// Tedy zadáním například: <see cref="Color.BlueViolet"/>.<see cref="CreateTransparent(Color, float)"/>(0.75f) 
        /// dojde k vytvoření a vrácení barvy s hodnotou Alpha = 75% = 192, od barvy BlueViolet (která je #FF8A2BE2), tedy výsledek bude #C08A2BE2.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        public static Color CreateTransparent(this Color root, float alpha)
        {
            int a = (int)(((float)root.A) * alpha);
            a = (a < 0 ? 0 : (a > 255 ? 255 : a));
            return Color.FromArgb(a, root.R, root.G, root.B);
        }
        #endregion
        #region Point, PointF: Add/Sub
        /// <summary>
        /// Returns a point = basePoint + addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="addPoint"></param>
        /// <returns></returns>
        public static Point Add(this Point basePoint, Point addPoint)
        {
            return new Point(basePoint.X + addPoint.X, basePoint.Y + addPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint + (addpoint X, Y)
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="addX"></param>
        /// <param name="addY"></param>
        /// <returns></returns>
        public static Point Add(this Point basePoint, int addX, int addY)
        {
            return new Point(basePoint.X + addX, basePoint.Y + addY);
        }
        /// <summary>
        /// Returns a point = basePoint + addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="addPoint"></param>
        /// <returns></returns>
        public static PointF Add(this PointF basePoint, PointF addPoint)
        {
            return new PointF(basePoint.X + addPoint.X, basePoint.Y + addPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint + addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="addPoint"></param>
        /// <returns></returns>
        public static PointF Add(this Point basePoint, PointF addPoint)
        {
            return new PointF((float)basePoint.X + addPoint.X, (float)basePoint.Y + addPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint - addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="subPoint"></param>
        /// <returns></returns>
        public static Point Sub(this Point basePoint, Point subPoint)
        {
            return new Point(basePoint.X - subPoint.X, basePoint.Y - subPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint - (subpoint X, Y)
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="subX"></param>
        /// <param name="subY"></param>
        /// <returns></returns>
        public static Point Sub(this Point basePoint, int subX, int subY)
        {
            return new Point(basePoint.X - subX, basePoint.Y - subY);
        }
        /// <summary>
        /// Returns a point = basePoint - addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="subPoint"></param>
        /// <returns></returns>
        public static PointF Sub(this PointF basePoint, PointF subPoint)
        {
            return new PointF(basePoint.X - subPoint.X, basePoint.Y - subPoint.Y);
        }
        /// <summary>
        /// Returns a point = basePoint - addpoint
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="subPoint"></param>
        /// <returns></returns>
        public static PointF Sub(this Point basePoint, PointF subPoint)
        {
            return new PointF((float)basePoint.X - subPoint.X, (float)basePoint.Y - subPoint.Y);
        }
        #endregion
        #region souřadnice: IsVisible()
        /// <summary>
        /// Vrátí true pokud this objekt může být svou velikostí viditelný (šířka a výška je větší než 0)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsVisible(this Size value) { return (value.Width > 0 && value.Height > 0); }
        /// <summary>
        /// Vrátí true pokud this objekt může být svou velikostí viditelný (šířka a výška je větší než 0)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsVisible(this SizeF value) { return (value.Width > 0f && value.Height > 0f); }
        /// <summary>
        /// Vrátí true pokud this objekt může být svou velikostí viditelný (šířka a výška je větší než 0)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsVisible(this Rectangle value) { return (value.Width > 0 && value.Height > 0); }
        /// <summary>
        /// Vrátí true pokud this objekt může být svou velikostí viditelný (šířka a výška je větší než 0)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsVisible(this RectangleF value) { return (value.Width > 0f && value.Height > 0f); }
        #endregion
        #region Size, SizeF, Rectangle, RectangleF: zooming
        /// <summary>
        /// Zvětší danou velikost o daný rozměr na každé straně = velikost se zvětší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="enlarge">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Enlarge(this SizeF size, float enlarge)
        { return new SizeF(size.Width + 2f * enlarge, size.Height + 2f * enlarge); }
        /// <summary>
        /// Zvětší danou velikost o daný rozměr na každé straně = velikost se zvětší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="enlargeWidth">Coefficient X.</param>
        /// <param name="enlargeHeight">Coefficient Y.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Enlarge(this SizeF size, float enlargeWidth, float enlargeHeight)
        { return new SizeF(size.Width + 2f * enlargeWidth, size.Height + 2f * enlargeHeight); }
        /// <summary>
        /// Zvětší danou velikost o daný rozměr na každé straně = velikost se zvětší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="enlarge">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Enlarge(this SizeF size, SizeF enlarge)
        { return new SizeF(size.Width + 2f * enlarge.Width, size.Height + 2f * enlarge.Height); }
        /// <summary>
        /// Zmenší danou velikost o daný rozměr na každé straně = velikost se zmenší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="reduce">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Reduce(this SizeF size, float reduce)
        { return new SizeF(size.Width - 2f * reduce, size.Height - 2f * reduce); }
        /// <summary>
        /// Zmenší danou velikost o daný rozměr na každé straně = velikost se zmenší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="reduceWidth">Coefficient X.</param>
        /// <param name="reduceHeight">Coefficient Y.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Reduce(this SizeF size, float reduceWidth, float reduceHeight)
        { return new SizeF(size.Width - 2f * reduceWidth, size.Height - 2f * reduceHeight); }
        /// <summary>
        /// Zmenší danou velikost o daný rozměr na každé straně = velikost se zmenší o dvojnásobek.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="reduce">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Reduce(this SizeF size, SizeF reduce)
        { return new SizeF(size.Width - 2f * reduce.Width, size.Height - 2f * reduce.Height); }

        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoom">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, decimal zoom)
        { return (new SizeF(size.Width * (float)zoom, size.Height * (float)zoom)); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoom">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, float zoom)
        { return new SizeF(size.Width * zoom, size.Height * zoom); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoom">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, double zoom)
        { return new SizeF(size.Width * (float)zoom, size.Height * (float)zoom); }
        /// <summary>
        /// Zmenší danou velikost daným poměrem.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="ratio">Ratio.</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF Divide(this SizeF size, decimal ratio)
        { return new SizeF(size.Width / (float)ratio, size.Height / (float)ratio); }
        /// <summary>
        /// Zmenší danou velikost daným poměrem.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="ratio">Ratio.</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF Divide(this SizeF size, float ratio)
        { return new SizeF(size.Width / ratio, size.Height / ratio); }
        /// <summary>
        /// Zmenší danou velikost daným poměrem.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="ratio">Ratio.</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF Divide(this SizeF size, double ratio)
        { return new SizeF(size.Width / (float)ratio, size.Height / (float)ratio); }

        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoomX">Coefficient.</param>
        /// <param name="zoomY">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, decimal zoomX, decimal zoomY)
        { return (new SizeF(size.Width * (float)zoomX, size.Height * (float)zoomX)); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoomX">Coefficient.</param>
        /// <param name="zoomY">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, float zoomX, float zoomY)
        { return new SizeF(size.Width * zoomX, size.Height * zoomY); }
        /// <summary>
        /// Zvětší danou velikost daným koeficientem.
        /// </summary>
        /// <param name="size">The SizeF structure to multiply.</param>
        /// <param name="zoomX">Coefficient.</param>
        /// <param name="zoomY">Coefficient.</param>
        /// <returns>A SizeF structure that is the result of the multiply operation.</returns>
        public static SizeF Multiply(this SizeF size, double zoomX, double zoomY)
        { return new SizeF(size.Width * (float)zoomX, size.Height * (float)zoomY); }

        /// <summary>
        /// Zmenší velikost this tak, aby se vešla do dané velikosti.
        /// Pokud je velikost menší, pak ji nezvětšuje.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="shrinkTo">Cílová velikost</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF ShrinkTo(this SizeF size, SizeF shrinkTo)
        { return new SizeF(size.Width < shrinkTo.Width ? size.Width : shrinkTo.Width, size.Height < shrinkTo.Height ? size.Height : shrinkTo.Height); }
        /// <summary>
        /// Zmenší velikost this tak, aby se vešla do dané velikosti a současně zachovala svůj původní poměr stran.
        /// Pokud je velikost menší, pak ji nezvětšuje.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="shrinkTo">Cílová velikost</param>
        /// <param name="preserveRatio">Zachovat poměr stran</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static SizeF ShrinkTo(this SizeF size, SizeF shrinkTo, bool preserveRatio)
        {
            if (size.Width <= shrinkTo.Width && size.Height <= shrinkTo.Height)
                return size;

            if (size.Width == 0 || size.Height == 0)
                preserveRatio = false;

            if (!preserveRatio)
                return new SizeF(size.Width < shrinkTo.Width ? size.Width : shrinkTo.Width, size.Height < shrinkTo.Height ? size.Height : shrinkTo.Height);

            if (shrinkTo.Width <= 0 || shrinkTo.Height <= 0)
                return SizeF.Empty;

            decimal shrinkX = (decimal)shrinkTo.Width / (decimal)size.Width;
            decimal shrinkY = (decimal)shrinkTo.Height / (decimal)size.Height;
            decimal shrink = (shrinkX < shrinkY ? shrinkX : shrinkY);

            return new SizeF((float)((decimal)size.Width * shrink), (float)((decimal)size.Height * shrink));
        }
        /// <summary>
        /// Zmenší velikost this tak, aby se vešla do dané velikosti.
        /// Pokud je velikost menší, pak ji nezvětšuje.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="shrinkTo">Cílová velikost</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static Size ShrinkTo(this Size size, Size shrinkTo)
        { return new Size(size.Width < shrinkTo.Width ? size.Width : shrinkTo.Width, size.Height < shrinkTo.Height ? size.Height : shrinkTo.Height); }
        /// <summary>
        /// Zmenší velikost this tak, aby se vešla do dané velikosti a současně zachovala svůj původní poměr stran.
        /// Pokud je velikost menší, pak ji nezvětšuje.
        /// </summary>
        /// <param name="size">The SizeF structure to divide.</param>
        /// <param name="shrinkTo">Cílová velikost</param>
        /// <param name="preserveRatio">Zachovat poměr stran</param>
        /// <returns>A SizeF structure that is the result of the divide operation.</returns>
        public static Size ShrinkTo(this Size size, Size shrinkTo, bool preserveRatio)
        {
            if (size.Width <= shrinkTo.Width && size.Height <= shrinkTo.Height)
                return size;

            if (size.Width == 0 || size.Height == 0)
                preserveRatio = false;

            if (!preserveRatio)
                return new Size(size.Width < shrinkTo.Width ? size.Width : shrinkTo.Width, size.Height < shrinkTo.Height ? size.Height : shrinkTo.Height);

            if (shrinkTo.Width <= 0 || shrinkTo.Height <= 0)
                return Size.Empty;

            decimal shrinkX = (decimal)shrinkTo.Width / (decimal)size.Width;
            decimal shrinkY = (decimal)shrinkTo.Height / (decimal)size.Height;
            decimal shrink = (shrinkX < shrinkY ? shrinkX : shrinkY);

            return new Size((int)((decimal)size.Width * shrink), (int)((decimal)size.Height * shrink));
        }
        /// <summary>
        /// Vytvoří a vrátí nový Rectangle, jehož velikost je do všech stran zvětšená o daný počet pixelů.
        /// Záporné číslo velikost zmenší.
        /// <para/>
        /// Například this Rectangle {50, 10, 30, 20} .Enlarge(+1) vrátí hodnotu: {49, 9, 32, 22}.
        /// <para/>
        /// Například this Rectangle {50, 10, 30, 20} .Enlarge(-1) vrátí hodnotu: {51, 11, 28, 18}.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="all">Změna aplikovaná na všechny strany</param>
        public static Rectangle Enlarge(this Rectangle r, int all)
        {
            return r.Enlarge(all, all, all, all);
        }
        /// <summary>
        /// Vytvoří a vrátí nový Rectangle, jehož velikost je do všech stran zvětšená o daný počet pixelů.
        /// Záporné číslo velikost zmenší.
        /// Například this Rectangle {50, 10, 30, 20} .Enlarge(1, 1, 1, 1) vrátí hodnotu: {49, 9, 32, 22}.
        /// Například this Rectangle {50, 10, 30, 20} .Enlarge(0, 0, -1, -1) vrátí hodnotu: {50, 10, 29, 19}.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="left">Zvětšení doleva (zmenší X a zvětší Width)</param>
        /// <param name="top">Zvětšení nahoru (zmenší Y a zvětší Height)</param>
        /// <param name="right">Zvětšení doprava (zvětší Width)</param>
        /// <param name="bottom">Zvětšení dolů (zvětší Height)</param>
        public static Rectangle Enlarge(this Rectangle r, int left, int top, int right, int bottom)
        {
            int x = r.X - left;
            int y = r.Y - top;
            int w = r.Width + left + right;
            int h = r.Height + top + bottom;
            return new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// Vytvoří a vrátí nový Rectangle, jehož velikost je do všech stran zvětšená o daný počet pixelů.
        /// Záporné číslo velikost zmenší.
        /// Například this RectangleF {50, 10, 30, 20} .Enlarge(1) vrátí hodnotu: {49, 9, 32, 22}.
        /// Například this RectangleF {50, 10, 30, 20} .Enlarge(-1) vrátí hodnotu: {51, 11, 28, 18}.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="all">Změna aplikovaná na všechny strany</param>
        public static RectangleF Enlarge(this RectangleF r, int all)
        {
            return r.Enlarge(all, all, all, all);
        }
        /// <summary>
        /// Create a new RectangleF, which is current rectangle enlarged by size specified for each side.
        /// For example: this RectangleF {50, 10, 30, 20} .Enlarge(1, 1, 1, 1) will be after: {49, 9, 32, 22}.
        /// For example: this RectangleF {50, 10, 30, 20} .Enlarge(0, 0, -1, -1) will be after: {50, 10, 29, 19}.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public static RectangleF Enlarge(this RectangleF r, float left, float top, float right, float bottom)
        {
            float x = r.X - left;
            float y = r.Y - top;
            float w = r.Width + left + right;
            float h = r.Height + top + bottom;
            return new RectangleF(x, y, w, h);
        }
        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou ta menší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Size Min(this Size one, Size other)
        {
            return new Size((one.Width < other.Width ? one.Width : other.Width),
                             (one.Height < other.Height ? one.Height : other.Height));
        }
        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou ta menší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="otherWidth"></param>
        /// <param name="otherHeight"></param>
        /// <returns></returns>
        public static Size Min(this Size one, int otherWidth, int otherHeight)
        {
            return new Size((one.Width < otherWidth ? one.Width : otherWidth),
                             (one.Height < otherHeight ? one.Height : otherHeight));
        }
        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou ta větší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Size Max(this Size one, Size other)
        {
            return new Size((one.Width > other.Width ? one.Width : other.Width),
                             (one.Height > other.Height ? one.Height : other.Height));
        }
        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou ta větší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="otherWidth"></param>
        /// <param name="otherHeight"></param>
        /// <returns></returns>
        public static Size Max(this Size one, int otherWidth, int otherHeight)
        {
            return new Size((one.Width > otherWidth ? one.Width : otherWidth),
                             (one.Height > otherHeight ? one.Height : otherHeight));
        }
        /// <summary>
        /// Vrátí Size, jejíž Width i Height jsou zarovnány do mezí Min - Max.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Size MinMax(this Size one, Size min, Size max)
        {
            return new Size((one.Width < min.Width ? min.Width : (one.Width > max.Width ? max.Width : one.Width)),
                             (one.Height < min.Height ? min.Height : (one.Height > max.Height ? max.Height : one.Height)));
        }

        /// <summary>
        /// Vrátí SizeF, jejíž Width i Height jsou ta menší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static SizeF Min(this SizeF one, SizeF other)
        {
            return new SizeF((one.Width < other.Width ? one.Width : other.Width),
                             (one.Height < other.Height ? one.Height : other.Height));
        }
        /// <summary>
        /// Vrátí SizeF, jejíž Width i Height jsou ta větší hodnota z this a max
        /// </summary>
        /// <param name="one"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static SizeF Max(this SizeF one, SizeF other)
        {
            return new SizeF((one.Width > other.Width ? one.Width : other.Width),
                             (one.Height > other.Height ? one.Height : other.Height));
        }
        /// <summary>
        /// Vrátí SizeF, jejíž Width i Height jsou zarovnány do mezí Min - Max.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static SizeF MinMax(this SizeF one, SizeF min, SizeF max)
        {
            return new SizeF((one.Width < min.Width ? min.Width : (one.Width > max.Width ? max.Width : one.Width)),
                             (one.Height < min.Height ? min.Height : (one.Height > max.Height ? max.Height : one.Height)));
        }
        #endregion
        #region Size: AlignTo
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Rectangle bounds, ContentAlignment alignment, bool cropSize)
        {
            SizeF realSize = sizeF;
            if (cropSize)
                realSize = sizeF.ShrinkTo(bounds.Size, false);
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <param name="preserveRatio"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Rectangle bounds, ContentAlignment alignment, bool cropSize, bool preserveRatio)
        {
            SizeF realSize = sizeF;
            if (cropSize)
                realSize = sizeF.ShrinkTo(bounds.Size, preserveRatio);
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Rectangle bounds, ContentAlignment alignment, bool cropSize)
        {
            Size realSize = size;
            if (cropSize)
                realSize = realSize.ShrinkTo(bounds.Size, false);
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <param name="preserveRatio"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Rectangle bounds, ContentAlignment alignment, bool cropSize, bool preserveRatio)
        {
            Size realSize = size;
            if (cropSize)
                realSize = realSize.ShrinkTo(bounds.Size, preserveRatio);
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Rectangle bounds, ContentAlignment alignment)
        {
            Size size = Size.Ceiling(sizeF);
            return size.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Rectangle bounds, ContentAlignment alignment)
        {
            int x = bounds.X;
            int y = bounds.Y;
            int w = bounds.Width - size.Width;
            int h = bounds.Height - size.Height;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x += w / 2;
                    break;
                case ContentAlignment.TopRight:
                    x += w;
                    break;
                case ContentAlignment.MiddleLeft:
                    y += h / 2;
                    break;
                case ContentAlignment.MiddleCenter:
                    x += w / 2;
                    y += h / 2;
                    break;
                case ContentAlignment.MiddleRight:
                    x += w;
                    y += h / 2;
                    break;
                case ContentAlignment.BottomLeft:
                    y += h;
                    break;
                case ContentAlignment.BottomCenter:
                    x += w / 2;
                    y += h;
                    break;
                case ContentAlignment.BottomRight:
                    x += w;
                    y += h;
                    break;
            }
            return new Rectangle(new Point(x, y), size);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <param name="addSize"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Point pivot, ContentAlignment alignment, Size addSize)
        {
            Rectangle bounds = AlignTo(sizeF, pivot, alignment);
            return new Rectangle(bounds.X, bounds.Y, bounds.Width + addSize.Width, bounds.Height + addSize.Height);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <param name="addSize"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Point pivot, ContentAlignment alignment, Size addSize)
        {
            Rectangle bounds = AlignTo(size, pivot, alignment);
            return new Rectangle(bounds.X, bounds.Y, bounds.Width + addSize.Width, bounds.Height + addSize.Height);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="sizeF"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this SizeF sizeF, Point pivot, ContentAlignment alignment)
        {
            Size size = Size.Ceiling(sizeF);
            return size.AlignTo(pivot, alignment);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Rectangle AlignTo(this Size size, Point pivot, ContentAlignment alignment)
        {
            int x = pivot.X;
            int y = pivot.Y;
            int w = size.Width;
            int h = size.Height;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x -= w / 2;
                    break;
                case ContentAlignment.TopRight:
                    x -= w;
                    break;
                case ContentAlignment.MiddleLeft:
                    y -= h / 2;
                    break;
                case ContentAlignment.MiddleCenter:
                    x -= w / 2;
                    y -= h / 2;
                    break;
                case ContentAlignment.MiddleRight:
                    x -= w;
                    y -= h / 2;
                    break;
                case ContentAlignment.BottomLeft:
                    y -= h;
                    break;
                case ContentAlignment.BottomCenter:
                    x -= w / 2;
                    y -= h;
                    break;
                case ContentAlignment.BottomRight:
                    x -= w;
                    y -= h;
                    break;
            }
            return new Rectangle(new Point(x, y), size);
        }
        /// <summary>
        /// Vrátí new Size, která bude mít shodný poměr jako výchozí, a bude mít danou Width
        /// </summary>
        /// <param name="size"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static Size ZoomToWidth(this Size size, int width)
        {
            if (size.Width <= 0) return size;
            double ratio = (double)width / (double)size.Width;
            return ZoomByRatio(size, ratio);
        }
        /// <summary>
        /// Vrátí new Size, která bude mít shodný poměr jako výchozí, a bude mít danou Height
        /// </summary>
        /// <param name="size"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Size ZoomToHeight(this Size size, int height)
        {
            if (size.Height <= 0) return size;
            double ratio = (double)height / (double)size.Height;
            return ZoomByRatio(size, ratio);
        }
        /// <summary>
        /// Vrátí new Size, která bude mít shodný poměr jako výchozí, a bude mít danou Height
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public static Size ZoomByRatio(this Size size, double ratio)
        {
            int width = (int)(Math.Round(ratio * (double)size.Width, 0));
            int height = (int)(Math.Round(ratio * (double)size.Height, 0));
            return new Size(width, height);
        }
        /// <summary>
        /// Vrátí new Size, která bude mít shodný poměr jako výchozí, a bude mít danou Height
        /// </summary>
        /// <param name="size"></param>
        /// <param name="ratioWidth"></param>
        /// <param name="ratioHeight"></param>
        /// <returns></returns>
        public static Size ZoomByRatio(this Size size, double ratioWidth, double ratioHeight)
        {
            int width = (int)(Math.Round(ratioWidth * (double)size.Width, 0));
            int height = (int)(Math.Round(ratioHeight * (double)size.Height, 0));
            return new Size(width, height);
        }
        #endregion
        #region SizeF: AlignTo
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="cropSize"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, RectangleF bounds, ContentAlignment alignment, bool cropSize)
        {
            SizeF realSize = size;
            if (cropSize)
            {
                if (realSize.Width > bounds.Width)
                    realSize.Width = bounds.Width;
                if (realSize.Height > bounds.Height)
                    realSize.Height = bounds.Height;
            }
            return realSize.AlignTo(bounds, alignment);
        }
        /// <summary>
        /// Zarovná prostor dané velikosti do daného hostitele v daném zarovnání
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, RectangleF bounds, ContentAlignment alignment)
        {
            float x = bounds.X;
            float y = bounds.Y;
            float w = bounds.Width - size.Width;
            float h = bounds.Height - size.Height;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x += w / 2f;
                    break;
                case ContentAlignment.TopRight:
                    x += w;
                    break;
                case ContentAlignment.MiddleLeft:
                    y += h / 2f;
                    break;
                case ContentAlignment.MiddleCenter:
                    x += w / 2f;
                    y += h / 2f;
                    break;
                case ContentAlignment.MiddleRight:
                    x += w;
                    y += h / 2f;
                    break;
                case ContentAlignment.BottomLeft:
                    y += h;
                    break;
                case ContentAlignment.BottomCenter:
                    x += w / 2f;
                    y += h;
                    break;
                case ContentAlignment.BottomRight:
                    x += w;
                    y += h;
                    break;
            }
            return new RectangleF(new PointF(x, y), size);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <param name="addSize"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, PointF pivot, ContentAlignment alignment, SizeF addSize)
        {
            RectangleF bounds = AlignTo(size, pivot, alignment);
            return new RectangleF(bounds.X, bounds.Y, bounds.Width + addSize.Width, bounds.Height + addSize.Height);
        }
        /// <summary>
        /// Zarovnat do prostoru
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pivot"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static RectangleF AlignTo(this SizeF size, PointF pivot, ContentAlignment alignment)
        {
            float x = pivot.X;
            float y = pivot.Y;
            float w = size.Width;
            float h = size.Height;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x -= w / 2f;
                    break;
                case ContentAlignment.TopRight:
                    x -= w;
                    break;
                case ContentAlignment.MiddleLeft:
                    y -= h / 2f;
                    break;
                case ContentAlignment.MiddleCenter:
                    x -= w / 2f;
                    y -= h / 2f;
                    break;
                case ContentAlignment.MiddleRight:
                    x -= w;
                    y -= h / 2f;
                    break;
                case ContentAlignment.BottomLeft:
                    y -= h;
                    break;
                case ContentAlignment.BottomCenter:
                    x -= w / 2f;
                    y -= h;
                    break;
                case ContentAlignment.BottomRight:
                    x -= w;
                    y -= h;
                    break;
            }
            return new RectangleF(new PointF(x, y), size);
        }
        /// <summary>
        /// Upraví this velikost tak, aby se vešla do dané souřadnice, zachová poměr stran a umístí výslednou velikost do daného prostoru dle daného zarovnání.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static RectangleF ZoomTo(this SizeF size, RectangleF bounds, ContentAlignment alignment)
        {
            float boundsW = bounds.Width;
            float boundsH = bounds.Height;
            float sizeW = size.Width;
            float sizeH = size.Height;
            if (boundsW <= 0f || boundsH <= 0f || sizeW <= 0f || sizeH <= 0f) return new RectangleF(bounds.Center(), SizeF.Empty);

            // Všechny velikosti jsou kladné, nejsou nulové:
            // Určíme zoom tak, abych danou velikost vynásobil zoomem a dostal cílovou velikost:
            float zoomW = boundsW / sizeW;
            float zoomH = boundsH / sizeH;
            float zoom = (zoomW < zoomH ? zoomW : zoomH);

            // A dál pokračuji algoritmem pro zarovnání vypočítané Zoomované velikosti:
            SizeF sizeZoom = new SizeF(zoom * sizeW, zoom * sizeH);
            return sizeZoom.AlignTo(bounds, alignment);
        }
        #endregion
        #region Rectangle: FromPoints, FromDim, FromCenter, End, GetVisualRange, GetSide, GetPoint
        /// <summary>
        /// Vrátí Rectangle, který je natažený mezi dvěma body, přičemž vzájemná pozice oněch dvou bodů může být libovolná.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static Rectangle FromPoints(this Point point1, Point point2)
        {
            int l = (point1.X < point2.X ? point1.X : point2.X);
            int t = (point1.Y < point2.Y ? point1.Y : point2.Y);
            int r = (point1.X > point2.X ? point1.X : point2.X);
            int b = (point1.Y > point2.Y ? point1.Y : point2.Y);
            return Rectangle.FromLTRB(l, t, r + 1, b + 1);
        }
        /// <summary>
        /// Vrátí Rectangle, který je nakreslený mezi souřadnicemi x1÷x2 a y1÷y2.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static Rectangle FromDim(int x1, int x2, int y1, int y2)
        {
            int l = (x1 < x2 ? x1 : x2);
            int t = (y1 < y2 ? y1 : y2);
            int r = (x1 > x2 ? x1 : x2);
            int b = (y1 > y2 ? y1 : y2);
            return Rectangle.FromLTRB(l, t, r + 1, b + 1);
        }
        /// <summary>
        /// Vrátí Rectangle, který je nakreslený mezi souřadnicemi x1÷x2 a y1÷y2.
        /// Pokud je vzdálenost mezi x1÷x2 nebo y1÷y2 menší než minDist, pak zachová vzdálenost minDist,
        /// a to tak, že v odpovídajícím směru upraví souřadnici x2 nebo y2. 
        /// Jako x2/y2 by tedy měla být zadána ta "pohyblivější".
        /// </summary>
        /// <returns></returns>
        public static Rectangle FromDim(int x1, int x2, int y1, int y2, int minDist)
        {
            // Úprava souřadnic minDist (kladné číslo) a x2,y2:
            if (minDist < 0) minDist = -minDist;
            if (x2 >= x1 && x2 - x1 < minDist)
                x2 = x1 + minDist;
            else if (x2 < x1 && x1 - x2 < minDist)
                x2 = x1 - minDist;
            if (y2 >= y1 && y2 - y1 < minDist)
                y2 = y1 + minDist;
            else if (y2 < y1 && y1 - y2 < minDist)
                y2 = y1 - minDist;

            int l = (x1 < x2 ? x1 : x2);
            int t = (y1 < y2 ? y1 : y2);
            int r = (x1 > x2 ? x1 : x2);
            int b = (y1 > y2 ? y1 : y2);
            return Rectangle.FromLTRB(l, t, r + 1, b + 1);
        }
        /// <summary>
        /// Vrátí Rectangle postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Point point, Size size)
        {
            Point location = new Point((point.X - size.Width / 2), (point.Y - size.Height / 2));
            return new Rectangle(location, size);
        }
        /// <summary>
        /// Vrátí Rectangle postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Point point, int width, int height)
        {
            Point location = new Point((point.X - width / 2), (point.Y - height / 2));
            return new Rectangle(location, new Size(width, height));
        }
        /// <summary>
        /// Vrátí Rectangle postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Point point, int size)
        {
            Point location = new Point((point.X - size / 2), (point.Y - size / 2));
            return new Rectangle(location, new Size(size, size));
        }
        /// <summary>
        /// Vrátí Rectangle ve velikosti (Size) this, postavený okolo daného středu
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Rectangle CreateRectangleFromCenter(this Size size, Point point)
        {
            Point location = new Point((point.X - size.Width / 2), (point.Y - size.Height / 2));
            return new Rectangle(location, size);
        }
        /// <summary>
        /// Vrátí bod uprostřed this Rectangle
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static Point Center(this Rectangle rectangle)
        {
            return new Point(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);
        }
        /// <summary>
        /// Vrátí bod na konci this Rectangle (opak Location) : (X + Width - 1, Y + Height - 1)
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static Point End(this Rectangle rectangle)
        {
            return new Point(rectangle.X + rectangle.Width - 1, rectangle.Y + rectangle.Height - 1);
        }
        /// <summary>
        /// Vrátí souřadnici z this rectangle dle požadované strany.
        /// Pokud je zadána hodnota Top, Right, Bottom nebo Left, pak vrací příslušnou souřadnici.
        /// Pokud je zadána hodnota None nebo nějaký součet stran, pak vrací null.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static Int32? GetSide(this Rectangle rectangle, RectangleSide edge)
        {
            switch (edge)
            {
                case RectangleSide.Top:
                    return rectangle.Top;
                case RectangleSide.Right:
                    return rectangle.Right;
                case RectangleSide.Bottom:
                    return rectangle.Bottom;
                case RectangleSide.Left:
                    return rectangle.Left;
            }
            return null;
        }
        /// <summary>
        /// Metoda vrátí určitý bod v daném Rectangle.
        /// Při nesprávném zadání strany může vrátit null.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static Point? GetPoint(this Rectangle rectangle, ContentAlignment alignment)
        {
            switch (alignment)
            {
                case ContentAlignment.TopLeft: return GetPoint(rectangle, RectangleSide.TopLeft);
                case ContentAlignment.TopCenter: return GetPoint(rectangle, RectangleSide.TopCenter);
                case ContentAlignment.TopRight: return GetPoint(rectangle, RectangleSide.TopRight);

                case ContentAlignment.MiddleLeft: return GetPoint(rectangle, RectangleSide.MiddleLeft);
                case ContentAlignment.MiddleCenter: return GetPoint(rectangle, RectangleSide.MiddleCenter);
                case ContentAlignment.MiddleRight: return GetPoint(rectangle, RectangleSide.MiddleRight);

                case ContentAlignment.BottomLeft: return GetPoint(rectangle, RectangleSide.BottomLeft);
                case ContentAlignment.BottomCenter: return GetPoint(rectangle, RectangleSide.BottomCenter);
                case ContentAlignment.BottomRight: return GetPoint(rectangle, RectangleSide.BottomRight);
            }
            return null;
        }
        /// <summary>
        /// Metoda vrátí určitý bod v daném Rectangle.
        /// Při nesprávném zadání strany může vrátit null.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Point? GetPoint(this Rectangle rectangle, RectangleSide position)
        {
            int? x = (position.HasFlag(RectangleSide.Left) ? rectangle.X :
                     (position.HasFlag(RectangleSide.Right) ? rectangle.Right :
                     (position.HasFlag(RectangleSide.CenterX) ? (rectangle.X + rectangle.Width / 2) : (int?)null)));
            int? y = (position.HasFlag(RectangleSide.Top) ? rectangle.Y :
                     (position.HasFlag(RectangleSide.Bottom) ? rectangle.Bottom :
                     (position.HasFlag(RectangleSide.CenterY) ? (rectangle.Y + rectangle.Height / 2) : (int?)null)));
            if (!(x.HasValue && y.HasValue)) return null;
            return new Point(x.Value, y.Value);
        }
        ///// <summary>
        ///// Vrátí rozsah { Begin, End } z this rectangle na požadované ose (orientaci).
        ///// Pokud je zadána hodnota axis = <see cref="Orientation.Horizontal"/>, pak je vrácen <see cref="Int32Range"/> s hodnotami X, Width, Right.
        ///// Pokud je zadána hodnota axis = <see cref="Orientation.Vertical"/>, pak je vrácen <see cref="Int32Range"/> s hodnotami Y, Height, Bottom.
        ///// Jinak se vrací null.
        ///// </summary>
        ///// <param name="rectangle"></param>
        ///// <param name="axis"></param>
        ///// <returns></returns>
        //public static Int32Range GetVisualRange(this Rectangle rectangle, Orientation axis)
        //{
        //    switch (axis)
        //    {
        //        case Orientation.Horizontal: return new Int32Range(rectangle.X, rectangle.Right);
        //        case Orientation.Vertical: return new Int32Range(rectangle.Y, rectangle.Bottom);
        //    }
        //    return null;
        //}
        #endregion
        #region RectangleF: FromPoints, FromDim, FromCenter
        /// <summary>
        /// Vrátí RectangleF, který je natažený mezi dvěma body, přičemž vzájemná pozice oněch dvou bodů může být libovolná.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static RectangleF FromPoints(this PointF point1, PointF point2)
        {
            float l = (point1.X < point2.X ? point1.X : point2.X);
            float t = (point1.Y < point2.Y ? point1.Y : point2.Y);
            float r = (point1.X > point2.X ? point1.X : point2.X);
            float b = (point1.Y > point2.Y ? point1.Y : point2.Y);
            return RectangleF.FromLTRB(l, t, r + 1f, b + 1f);
        }
        /// <summary>
        /// Vrátí RectangleF, který je nakreslený mezi souřadnicemi x1÷x2 a y1÷y2.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static RectangleF FromDim(float x1, float x2, float y1, float y2)
        {
            float l = (x1 < x2 ? x1 : x2);
            float t = (y1 < y2 ? y1 : y2);
            float r = (x1 > x2 ? x1 : x2);
            float b = (y1 > y2 ? y1 : y2);
            return RectangleF.FromLTRB(l, t, r + 1f, b + 1f);
        }
        /// <summary>
        /// Vrátí RectangleF, který je nakreslený mezi souřadnicemi x1÷x2 a y1÷y2.
        /// Pokud je vzdálenost mezi x1÷x2 nebo y1÷y2 menší než minDist, pak zachová vzdálenost minDist,
        /// a to tak, že v odpovídajícím směru upraví souřadnici x2 nebo y2. 
        /// Jako x2/y2 by tedy měla být zadána ta "pohyblivější".
        /// </summary>
        /// <returns></returns>
        public static RectangleF FromDim(float x1, float x2, float y1, float y2, float minDist)
        {
            // Úprava souřadnic minDist (kladné číslo) a x2,y2:
            if (minDist < 0f) minDist = -minDist;
            if (x2 >= x1 && x2 - x1 < minDist)
                x2 = x1 + minDist;
            else if (x2 < x1 && x1 - x2 < minDist)
                x2 = x1 - minDist;
            if (y2 >= y1 && y2 - y1 < minDist)
                y2 = y1 + minDist;
            else if (y2 < y1 && y1 - y2 < minDist)
                y2 = y1 - minDist;

            float l = (x1 < x2 ? x1 : x2);
            float t = (y1 < y2 ? y1 : y2);
            float r = (x1 > x2 ? x1 : x2);
            float b = (y1 > y2 ? y1 : y2);
            return RectangleF.FromLTRB(l, t, r + 1f, b + 1f);
        }
        /// <summary>
        /// Vrátí RectangleF postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static RectangleF CreateRectangleFromCenter(this PointF point, SizeF size)
        {
            PointF location = new PointF((point.X - size.Width / 2f), (point.Y - size.Height / 2f));
            return new RectangleF(location, size);
        }
        /// <summary>
        /// Vrátí RectangleF postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static RectangleF CreateRectangleFromCenter(this PointF point, float width, float height)
        {
            PointF location = new PointF((point.X - width / 2f), (point.Y - height / 2f));
            return new RectangleF(location, new SizeF(width, height));
        }
        /// <summary>
        /// Vrátí RectangleF postavený okolo středu this, v dané velikosti (size)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static RectangleF CreateRectangleFromCenter(this PointF point, float size)
        {
            PointF location = new PointF((point.X - size / 2f), (point.Y - size / 2f));
            return new RectangleF(location, new SizeF(size, size));
        }
        /// <summary>
        /// Vrátí RectangleF ve velikosti (Size) this, postavený okolo daného středu
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static RectangleF CreateRectangleFromCenter(this SizeF size, PointF point)
        {
            PointF location = new PointF((point.X - size.Width / 2f), (point.Y - size.Height / 2f));
            return new RectangleF(location, size);
        }
        /// <summary>
        /// Vrátí bod uprostřed this RectangleF
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <returns></returns>
        public static PointF Center(this RectangleF rectangleF)
        {
            return new PointF(rectangleF.X + rectangleF.Width / 2f, rectangleF.Y + rectangleF.Height / 2f);
        }
        /// <summary>
        /// Vrátí bod na konci this RectangleF (opak Location)
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <returns></returns>
        public static PointF End(this RectangleF rectangleF)
        {
            return new PointF(rectangleF.X + rectangleF.Width - 1f, rectangleF.Y + rectangleF.Height - 1f);
        }
        #endregion
        #region RectangleF: RelativePoint, AbsolutePoint
        /// <summary>
        /// Vrátí relativní pozici daného absolutního bodu (absolutePoint) vzhledem k this (RectangleF).
        /// Relativní pozice je v rozmezí 0 (na souřadnici Left nebo Top) až 1 (na souřadnici Right nebo Bottom).
        /// Relativní pozice může být menší než 0 (vlevo nebo nad this), nebo větší než 1 (vpravo nebo pod this).
        /// Tedy hodnoty 0 a 1 jsou na hraně this, hodnoty mezi 0 a 1 jsou uvnitř this, a hodnoty mimo jsou mimo this.
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public static PointF GetPointFRelative(this RectangleF rectangleF, PointF absolutePoint)
        {
            return new PointF(
                (float)_GetRelative(rectangleF.X, rectangleF.Right, absolutePoint.X),
                (float)_GetRelative(rectangleF.Y, rectangleF.Bottom, absolutePoint.Y));
        }
        /// <summary>
        /// Vrátí relativní pozici daného absolutního bodu 
        /// vzhledem k bodům begin (relativní pozice = 0) a end (relativní pozice = 1).
        /// Pokud mezi body begin a end je vzdálenost 0, pak vrací 0.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="absolute"></param>
        /// <returns></returns>
        private static decimal _GetRelative(float begin, float end, float absolute)
        {
            decimal offset = (decimal)(absolute - begin);
            decimal size = (decimal)(end - begin);
            if (size == 0m) return 0m;
            return offset / size;
        }
        /// <summary>
        /// Vrátí souřadnice bodu, který v this rectangle odpovídá dané relativní souřadnici.
        /// Relativní souřadnice vyjadřuje pozici bodu: hodnota 0=na pozici Left nebo Top, hodnota 1=na pozici Right nebo Bottom.
        /// Vrácený bod je vyjádřen v reálných (absolutních) hodnotách odpovídajících rectanglu this.
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="relativePoint"></param>
        /// <returns></returns>
        public static PointF GetPointFAbsolute(this RectangleF rectangleF, PointF relativePoint)
        {
            return new PointF(
                (float)_GetAbsolute(rectangleF.X, rectangleF.Right, (decimal)relativePoint.X),
                (float)_GetAbsolute(rectangleF.Y, rectangleF.Bottom, (decimal)relativePoint.Y));
        }
        /// <summary>
        /// Vrátí absolutní pozici daného relativního bodu 
        /// vzhledem k bodům begin (relativní pozice = 0) a end (relativní pozice = 1).
        /// Pokud mezi body begin a end je vzdálenost 0, pak vrací begin.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="relative"></param>
        /// <returns></returns>
        private static float _GetAbsolute(float begin, float end, decimal relative)
        {
            decimal size = (decimal)(end - begin);
            if (size == 0m) return begin;
            return begin + (float)(relative * size);
        }

        private static float _GetBeginFromRelative(float fix, float size, decimal relative)
        {
            return fix - (float)((decimal)size * relative);
        }
        #endregion
        #region RectangleF: MoveEdge, MovePoint
        /// <summary>
        /// Vrátí RectangleF, který vytvoří z this RectangleF, když jeho hranu (edge) posune na novou souřadnici
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="side"></param>
        /// <param name="dimension"></param>
        /// <returns></returns>
        public static RectangleF MoveEdge(this RectangleF rectangleF, RectangleSide side, float dimension)
        {
            float x1 = rectangleF.X;
            float x2 = rectangleF.Right;
            float y1 = rectangleF.Y;
            float y2 = rectangleF.Bottom;
            switch (side)
            {
                case RectangleSide.Top:
                    return FromDim(x1, x2, dimension, y2);
                case RectangleSide.Right:
                    return FromDim(x1, dimension, y1, y2);
                case RectangleSide.Bottom:
                    return FromDim(x1, x2, y1, dimension);
                case RectangleSide.Left:
                    return FromDim(dimension, x2, y1, y2);
            }
            return rectangleF;
        }
        /// <summary>
        /// Vrátí PointF, který leží na daném rohu this RectangleF
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="corner"></param>
        /// <returns></returns>
        public static PointF GetPoint(this RectangleF rectangleF, RectangleCorner corner)
        {
            float x1 = rectangleF.X;
            float x2 = rectangleF.Right;
            float y1 = rectangleF.Y;
            float y2 = rectangleF.Bottom;
            switch (corner)
            {
                case RectangleCorner.LeftTop:
                    return new PointF(x1, y1);
                case RectangleCorner.TopRight:
                    return new PointF(x2, y1);
                case RectangleCorner.RightBottom:
                    return new PointF(x2, y2);
                case RectangleCorner.BottomLeft:
                    return new PointF(x1, y2);
            }
            return PointF.Empty;
        }
        /// <summary>
        /// Vrátí RectangleF, který vytvoří z this RectangleF, když jeho bod (corner) posune na nové souřadnice
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="corner"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static RectangleF MovePoint(this RectangleF rectangleF, RectangleCorner corner, PointF point)
        {
            switch (corner)
            {
                case RectangleCorner.LeftTop:
                    return FromPoints(rectangleF.GetPoint(RectangleCorner.RightBottom), point);
                case RectangleCorner.TopRight:
                    return FromPoints(rectangleF.GetPoint(RectangleCorner.BottomLeft), point);
                case RectangleCorner.RightBottom:
                    return FromPoints(rectangleF.GetPoint(RectangleCorner.LeftTop), point);
                case RectangleCorner.BottomLeft:
                    return FromPoints(rectangleF.GetPoint(RectangleCorner.TopRight), point);
            }
            return rectangleF;
        }
        #endregion
        #region Rectangle, RectangleF: GetArea(), SummaryRectangle()
        /// <summary>
        /// Vrací true, pokud this Rectangle má obě velikosti (Width i Height) kladné, a tedy obsahuje nějaký reálný pixel ke kreslení.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static bool HasPixels(this Rectangle r)
        {
            return (r.Width > 0 && r.Height > 0);
        }
        /// <summary>
        /// Vrací plochu daného Rectangle.
        /// Pokud některý rozměr je nula nebo záporný, vrací 0 (neboť záporná plocha neexistuje, 
        /// a pokud by na vstupu byly dvě záporné hodnoty, dostal bych kladnou plochu ze záporného rozměru).
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static int GetArea(this Rectangle r)
        {
            return ((r.Width <= 0 || r.Height <= 0) ? 0 : r.Width * r.Height);
        }
        /// <summary>
        /// Vrací plochu daného Rectangle.
        /// Pokud některý rozměr je nula nebo záporný, vrací 0 (neboť záporná plocha neexistuje, 
        /// a pokud by na vstupu byly dvě záporné hodnoty, dostal bych kladnou plochu ze záporného rozměru).
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static float GetArea(this RectangleF r)
        {
            return ((r.Width <= 0f || r.Height <= 0f) ? 0f : r.Width * r.Height);
        }
        /// <summary>
        /// Vrací plochu daného Size.
        /// Pokud některý rozměr je nula nebo záporný, vrací 0 (neboť záporná plocha neexistuje, 
        /// a pokud by na vstupu byly dvě záporné hodnoty, dostal bych kladnou plochu ze záporného rozměru).
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static int GetArea(this Size r)
        {
            return ((r.Width <= 0 || r.Height <= 0) ? 0 : r.Width * r.Height);
        }
        /// <summary>
        /// Vrací plochu daného SizeF.
        /// Pokud některý rozměr je nula nebo záporný, vrací 0 (neboť záporná plocha neexistuje, 
        /// a pokud by na vstupu byly dvě záporné hodnoty, dostal bych kladnou plochu ze záporného rozměru).
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static float GetArea(this SizeF r)
        {
            return ((r.Width <= 0f || r.Height <= 0f) ? 0f : r.Width * r.Height);
        }
        /// <summary>
        /// Vrátí orientaci tohoto prostoru podle poměru šířky a výšky. Pokud šířka == výšce, pak vrací Horizontal.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Orientation GetOrientation(this Rectangle r)
        {
            return (r.Width >= r.Height ? Orientation.Horizontal : Orientation.Vertical);
        }
        /// <summary>
        /// Returns a Orientation of this Rectangle. When Width is equal or greater than Height, then returns Horizontal. Otherwise returns Vertica orientation.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Orientation GetOrientation(this RectangleF r)
        {
            return (r.Width >= r.Height ? Orientation.Horizontal : Orientation.Vertical);
        }
        /// <summary>
        /// Metoda vrátí vzdálenost daného bodu od nejbližšího bodu daného rectangle.
        /// Pokud bod leží uvnitř daného rectangle, vrací se 0 (nikoli záporné číslo, tato metoda neměří vnitřní vzdálenost).
        /// Metoda měří vnější vzdálenost mezi daným bodem a nejbližší hranou nebo vrcholem daného obdélníku.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static int GetOuterDistance(this Rectangle bounds, Point point)
        {
            int x = point.X;
            int y = point.Y;
            int l = bounds.Left;
            int t = bounds.Top;
            int r = bounds.Right;
            int b = bounds.Bottom;

            // Kde se nachází bod point vzhledem k prostoru bounds?
            //  11=vlevo nahoře, 12=vlevo uprostřed, 13=vlevo dole, 21=uprostřed nahoře, 22=uprostřed, 23=uprostřed dole; 31=vpravo nahoře, 32=vpravo uprostřed, 33=vpravo dole
            int relation = 10 * GetPosition(x, l, r) + GetPosition(y, t, b);
            int dx = 0;
            int dy = 0;
            switch (relation)
            {
                case 11:        // Vlevo, Nad
                    dx = l - x;
                    dy = t - y;
                    break;
                case 12:        // Vlevo, Uvnitř
                    dx = l - x;
                    break;
                case 13:        // Vlevo, Pod
                    dx = l - x;
                    dy = y - b;
                    break;
                case 21:        // Uvnitř, Nad
                    dy = t - y;
                    break;
                case 22:        // Uvnitř, Uvnitř
                    break;
                case 23:        // Uvnitř, Pod
                    dy = y - b;
                    break;
                case 31:        // Vpravo, Nad
                    dx = x - r;
                    dy = t - y;
                    break;
                case 32:        // Vpravo, Uvnitř
                    dx = x - r;
                    break;
                case 33:        // Vpravo, Pod
                    dx = x - r;
                    dy = y - b;
                    break;
            }
            if (dy == 0) return dx;
            if (dx == 0) return dy;
            int d = (int)Math.Ceiling(Math.Sqrt((double)(dx * dx + dy * dy)));
            return d;
        }
        /// <summary>
        /// Metoda vrátí nejkratší vzdálenost mezi dvěma rectangles
        /// </summary>
        /// <param name="test"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        public static int GetOuterDistance(this Rectangle test, Rectangle area)
        {
            if (test.IntersectsWith(area)) return 0;                // Pokud se plochy částečně nebo plně překrývají, vracím 0 = měřím vnější vzdálenost, nikoli vnitřní

            // Je zjevné, že obdélníky nemají nic společného - jdeme hledat jejich vzájemnou pozici a z ní určíme vzdálenost.
            // Protože nemají nic společného, pak pozice ve směru jedné osy (X nebo Y) může být pouze: Před - přes (a pak hledám pozici v druhé ose) - Za.
            int testL = test.Left;
            int testT = test.Top;
            int testR = test.Right;
            int testB = test.Bottom;
            int areaL = area.Left;
            int areaT = area.Top;
            int areaR = area.Right;
            int areaB = area.Bottom;

            // Kde se nachází obdélník bounds vzhledem k prostoru otherBounds?
            //  11=vlevo nahoře, 13=vlevo uprostřed, 15=vlevo dole, 31=uprostřed nahoře, 33=uprostřed, 35=uprostřed dole; 51=vpravo nahoře, 53=vpravo uprostřed, 55=vpravo dole
            int relation = 10 * GetPosition(testL, testR, areaL, areaR) + GetPosition(testT, testB, areaT, areaB);
            int dx = 0;
            int dy = 0;
            switch (relation)
            {
                case 11:        // Test je: Vlevo, Nad
                    dx = areaL - testR;
                    dy = areaT - testB;
                    break;
                case 12:        // Test je: Vlevo, Uvnitř
                case 13:
                case 14:
                    dx = areaL - testR;
                    break;
                case 15:        // Test je: Vlevo, Pod
                    dx = areaL - testR;
                    dy = testT - areaB;
                    break;
                case 21:        // Test je: Uvnitř, Nad
                case 31:
                case 41:
                    dy = areaT - testB;
                    break;
                case 22:        // Test je: Uvnitř, Uvnitř
                case 32:
                case 42:
                case 23:
                case 33:
                case 43:
                case 24:
                case 34:
                case 44:
                    break;
                case 25:        // Test je: Uvnitř, Pod
                case 35:
                case 45:
                    dy = testT - areaB;
                    break;
                case 51:        // Test je: Vpravo, Nad
                    dx = testL - areaR;
                    dy = areaT - testB;
                    break;
                case 52:        // Test je: Vpravo, Uvnitř
                case 53:
                case 54:
                    dx = testL - areaR;
                    break;
                case 55:        // Test je: Vpravo, Pod
                    dx = testL - areaR;
                    dy = testT - areaB;
                    break;
            }
            if (dy == 0) return dx;
            if (dx == 0) return dy;
            int d = (int)Math.Ceiling(Math.Sqrt((double)(dx * dx + dy * dy)));
            return d;
        }
        /// <summary>
        /// Vrátí pozici dané hodnoty vzhledem k rozmezí begin - end, jako hodnotu 1 = před, 2 = uvnitř, 3 = za
        /// </summary>
        /// <param name="value"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private static int GetPosition(int value, int begin, int end)
        {
            if (value < begin) return 1;
            if (value < end) return 2;
            return 3;
        }
        /// <summary>
        /// Vrátí pozici dané hodnoty (test) vzhledem k prostoru (area), jako hodnotu 1 = test je před area, 3 = je uvnitř, 5 = test je za area
        /// </summary>
        /// <param name="testBegin"></param>
        /// <param name="testEnd"></param>
        /// <param name="areaBegin"></param>
        /// <param name="areaEnd"></param>
        /// <returns></returns>
        private static int GetPosition(int testBegin, int testEnd, int areaBegin, int areaEnd)
        {
            if (testEnd <= areaBegin) return 1;
            if (testBegin < areaBegin && testEnd < areaEnd) return 2;
            if (testBegin >= areaBegin && testEnd <= areaEnd) return 3;
            if (testBegin < areaEnd) return 4;
            return 5;
        }
        /// <summary>
        /// Vrátí nový Rectangle, který má stejnou pozici středu (Center), ale je otočený o 90°: z výšky na šířku.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Rectangle Swap(this Rectangle r)
        {
            Point center = Center(r);
            Size size = Swap(r.Size);
            return center.CreateRectangleFromCenter(size);
        }
        /// <summary>
        /// Vrátí danou velikost otočenou o 90°: z výšky na šířku.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Size Swap(this Size size)
        {
            return new Size(size.Height, size.Width);
        }
        /// <summary>
        /// Vrátí nový Rectangle, který má stejnou pozici středu (Center), ale je otočený o 90°: z výšky na šířku.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static RectangleF Swap(this RectangleF r)
        {
            PointF center = Center(r);
            SizeF size = Swap(r.Size);
            return center.CreateRectangleFromCenter(size);
        }
        /// <summary>
        /// Vrátí danou velikost otočenou o 90°: z výšky na šířku.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static SizeF Swap(this SizeF size)
        {
            return new SizeF(size.Height, size.Width);
        }
        #endregion
        #region Rectangle a RectangleF: Add a Sub
        /// <summary>
        /// Vrací nový <see cref="Rectangle"/>, který je dán výchozím bodem plus daný přídavek, a výchozí velikostí.
        /// Jinými slovy původní <see cref="Rectangle"/> posune o X,Y.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle Add(this Rectangle r, int x, int y)
        {
            return new Rectangle(r.X + x, r.Y + y, r.Width, r.Height);
        }
        /// <summary>
        /// Vrací nový <see cref="Rectangle"/>, který je dán výchozím bodem plus daný přídavek, a výchozí velikostí.
        /// Jinými slovy původní <see cref="Rectangle"/> posune o X,Y.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle Add(this Rectangle r, Point point)
        {
            return new Rectangle(r.X + point.X, r.Y + point.Y, r.Width, r.Height);
        }
        /// <summary>
        /// Vrací nový <see cref="Rectangle"/>, který je dán aktuálním prostorem, zvětšeným o dané vnitřní okraje.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Rectangle Add(this Rectangle r, Padding padding)
        {
            return new Rectangle(r.X - padding.Left, r.Y - padding.Top, r.Width + padding.Horizontal, r.Height + padding.Vertical);
        }
        /// <summary>
        /// Returns a Rectangle?, which is this rectangle plus point (=new Rectangle?(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle? Add(this Rectangle? r, int x, int y)
        {
            return (r.HasValue ? (Rectangle?)(new Rectangle(r.Value.X + x, r.Value.Y + y, r.Value.Width, r.Value.Height)) : (Rectangle?)null);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle plus point (=new Rectangle(this.X + point.X, this.Y + point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle? Add(this Rectangle? r, Point? point)
        {
            return (r.HasValue && point.HasValue ? (Rectangle?)(new Rectangle(r.Value.Location.Add(point.Value), r.Value.Size)) : (Rectangle?)null);
        }
        /// <summary>
        /// Vrací nový <see cref="Rectangle"/>, který je dán aktuálním prostorem, zvětšeným o dané vnější okraje.
        /// Pokud Rectangle je null, vrací Rectangle daný pouze velikostí Padding, počínaje v bodě 0/0.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Rectangle Add(this Rectangle? r, Padding padding)
        {
            if (!r.HasValue) return new Rectangle(0, 0, padding.Horizontal, padding.Vertical);
            return Add(r.Value, padding);
        }
        /// <summary>
        /// Vrací nový <see cref="Rectangle"/>, který je dán aktuálním prostorem, zvětšeným o dané vnější okraje.
        /// Pokud Padding je null, vrací vstupní Rectangle.
        /// Pokud Rectangle je null a Padding není null, vrací Rectangle daný pouze velikostí Padding, počínaje v bodě 0/0.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Rectangle? Add(this Rectangle? r, Padding? padding)
        {
            if (!r.HasValue && !padding.HasValue) return null;
            if (!padding.HasValue) return r;
            if (!r.HasValue) return new Rectangle(0, 0, padding.Value.Horizontal, padding.Value.Vertical);
            return Add(r.Value, padding.Value);
        }
        /// <summary>
        /// Vrací nový <see cref="Rectangle"/>, který je dán výchozím bodem plus daný přídavek, a výchozí velikostí.
        /// Jinými slovy původní <see cref="Rectangle"/> posune o X,Y.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static RectangleF Add(this RectangleF r, float x, float y)
        {
            return new RectangleF(r.X + x, r.Y + y, r.Width, r.Height);
        }
        /// <summary>
        /// Vrací nový <see cref="Rectangle"/>, který je dán výchozím bodem plus daný přídavek, a výchozí velikostí.
        /// Jinými slovy původní <see cref="Rectangle"/> posune o X,Y.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static RectangleF Add(this RectangleF r, PointF point)
        {
            return new RectangleF(r.X + point.X, r.Y + point.Y, r.Width, r.Height);
        }

        /// <summary>
        /// Returns a Rectangle, which is this rectangle minus point (=new Rectangle(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle Sub(this Rectangle r, int x, int y)
        {
            return new Rectangle(r.X - x, r.Y - y, r.Width, r.Height);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle minus point (=new Rectangle(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle Sub(this Rectangle r, Point point)
        {
            return new Rectangle(r.Location.Sub(point), r.Size);
        }
        /// <summary>
        /// Vrací nový <see cref="Rectangle"/>, který je dán aktuálním prostorem, zmenšeným o dané vnitřní okraje.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Rectangle Sub(this Rectangle r, Padding padding)
        {
            return new Rectangle(r.X + padding.Left, r.Y + padding.Top, r.Width - padding.Horizontal, r.Height - padding.Vertical);
        }
        /// <summary>
        /// Returns a Rectangle?, which is this rectangle minus point (=new Rectangle?(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Rectangle? Sub(this Rectangle? r, int x, int y)
        {
            return (r.HasValue ? (Rectangle?)(new Rectangle(r.Value.X - x, r.Value.Y - y, r.Value.Width, r.Value.Height)) : (Rectangle?)null);
        }
        /// <summary>
        /// Returns a Rectangle?, which is this rectangle minus point (=new Rectangle?(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rectangle? Sub(this Rectangle? r, Point? point)
        {
            return (r.HasValue && point.HasValue ? (Rectangle?)(new Rectangle(r.Value.Location.Sub(point.Value), r.Value.Size)) : (Rectangle?)null);
        }
        /// <summary>
        /// Vrací nový <see cref="Rectangle"/>, který je dán aktuálním prostorem, zmenšeným o dané vnitřní okraje.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Rectangle? Sub(this Rectangle? r, Padding? padding)
        {
            if (!r.HasValue || !padding.HasValue) return null;
            return Sub(r.Value, padding.Value);
        }
        /// <summary>
        /// Returns a Rectangle, which is this rectangle minus point (=new Rectangle(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static RectangleF Sub(this RectangleF r, float x, float y)
        {
            return new RectangleF(r.X - x, r.Y - y, r.Width, r.Height);
        }
        /// <summary>
        /// Returns a RectangleF, which is this rectangle minus point (=new RectangleF(this.X - point.X, this.Y - point.Y, this.Width, this.Height))
        /// </summary>
        /// <param name="r"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static RectangleF Sub(this RectangleF r, PointF point)
        {
            return new RectangleF(r.Location.Sub(point), r.Size);
        }

        /// <summary>
        /// Vrátí Size, která je o (w, h) větší než aktuální Size. Umožní i zmenšit, pokud hodnoty jsou záporné. Umožní zmenšit do nuly, ale ne do záporné hodnoty.
        /// Záporné w nebo h zmenší Size.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static Size Add(this Size s, int w, int h)
        {
            int nw = s.Width + w;
            if (nw < 0) nw = 0;
            int nh = s.Height + h;
            if (nh < 0) nh = 0;
            return new Size(nw, nh);
        }
        /// <summary>
        /// Vrátí Size, která je o daný <paramref name="padding"/> větší než aktuální Size. 
        /// Umožní i zmenšit, pokud hodnoty jsou záporné. Umožní zmenšit do nuly, ale ne do záporné hodnoty.
        /// Záporné hodnoty v <paramref name="padding"/> zmenší Size.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Size Add(this Size s, Padding padding)
        {
            int nw = s.Width + padding.Horizontal;
            if (nw < 0) nw = 0;
            int nh = s.Height + padding.Vertical;
            if (nh < 0) nh = 0;
            return new Size(nw, nh);
        }
        /// <summary>
        /// Vrátí Size, která je o (w, h) menší než aktuální Size. Umožní zmenšit do nuly, ale ne do záporné hodnoty.
        /// Záporné x nebo y zvětší Size.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static Size Sub(this Size s, int w, int h)
        {
            int nw = s.Width - w;
            if (nw < 0) nw = 0;
            int nh = s.Height - h;
            if (nh < 0) nh = 0;
            return new Size(nw, nh);
        }
        /// <summary>
        /// Vrátí Size, která je o daný <paramref name="padding"/> menší než aktuální Size. 
        /// Umožní i zvětšit, pokud hodnoty jsou záporné. Umožní zmenšit do nuly, ale ne do záporné hodnoty.
        /// Záporné hodnoty v <paramref name="padding"/> zvětší Size.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Size Sub(this Size s, Padding padding)
        {
            int nw = s.Width - padding.Horizontal;
            if (nw < 0) nw = 0;
            int nh = s.Height - padding.Vertical;
            if (nh < 0) nh = 0;
            return new Size(nw, nh);
        }
        #endregion
        #region Rectangle: SummaryBounds
        /// <summary>
        /// Vrací <see cref="Rectangle"/>?, který je souhrnem všech <see cref="Rectangle"/>.
        /// Akceptuje i neviditelné Rectangle (který má Width nebo Height nula nebo záporné), i z nich střádá jejich souřadnice.
        /// Vrací null tehdy, když na vstupu nebude žádný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle? SummaryRectangle(params Rectangle[] items) { return _SummaryRectangle(items as IEnumerable<Rectangle>, false); }
        /// <summary>
        /// Vrací <see cref="Rectangle"/>?, který je souhrnem všech <see cref="Rectangle"/>.
        /// Akceptuje i neviditelné Rectangle (který má Width nebo Height nula nebo záporné), i z nich střádá jejich souřadnice.
        /// Vrací null tehdy, když na vstupu nebude žádný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle? SummaryRectangle(this IEnumerable<Rectangle> items) { return _SummaryRectangle(items as IEnumerable<Rectangle>, false); }
        /// <summary>
        /// Vrací <see cref="Rectangle"/>?, který je souhrnem těch <see cref="Rectangle"/>, které jsou viditelné.
        /// Viditelný = ten který má Width a Height kladné.
        /// Vrací null tehdy, když na vstupu nebude žádný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle? SummaryVisibleRectangle(params Rectangle[] items) { return _SummaryRectangle(items as IEnumerable<Rectangle>, true); }
        /// <summary>
        /// Vrací <see cref="Rectangle"/>?, který je souhrnem těch <see cref="Rectangle"/>, které jsou viditelné.
        /// Viditelný = ten který má Width a Height kladné.
        /// Vrací null tehdy, když na vstupu nebude žádný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle? SummaryVisibleRectangle(this IEnumerable<Rectangle> items) { return _SummaryRectangle(items as IEnumerable<Rectangle>, true); }
        /// <summary>
        /// Vrací <see cref="Rectangle"/>?, který je souhrnem zadaných <see cref="Rectangle"/>.
        /// Vrací null tehdy, když na vstupu nebude žádný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="onlyVisible"></param>
        /// <returns></returns>
        private static Rectangle? _SummaryRectangle(IEnumerable<Rectangle> items, bool onlyVisible)
        {
            int l = 0;
            int t = 0;
            int r = 0;
            int b = 0;
            bool empty = true;
            foreach (Rectangle item in items)
            {
                if (onlyVisible && (item.Width <= 0 || item.Height <= 0)) continue;

                if (empty)
                {
                    l = item.Left;
                    t = item.Top;
                    r = item.Right;
                    b = item.Bottom;
                    empty = false;
                }
                else
                {
                    if (l > item.Left) l = item.Left;
                    if (t > item.Top) t = item.Top;
                    if (r < item.Right) r = item.Right;
                    if (b < item.Bottom) b = item.Bottom;
                }
            }
            if (empty) return null;
            return Rectangle.FromLTRB(l, t, r, b);
        }

        /// <summary>
        /// Vrací <see cref="Rectangle"/>?, který je souhrnem všech zadaných <see cref="Rectangle"/>?, které nejsou null.
        /// Vrací null tehdy, když na vstupu nebude žádný platný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle? SummaryRectangle(params Rectangle?[] items) { return _SummaryNRectangle(items, false); }
        /// <summary>
        /// Vrací <see cref="Rectangle"/>?, který je souhrnem všech zadaných <see cref="Rectangle"/>?, které nejsou null.
        /// Vrací null tehdy, když na vstupu nebude žádný platný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle? SummaryRectangle(this IEnumerable<Rectangle?> items) { return _SummaryNRectangle(items, false); }
        /// <summary>
        /// Vrací <see cref="Rectangle"/>?, který je souhrnem všech zadaných <see cref="Rectangle"/>?, které nejsou null a mají Width i Height kladné.
        /// Vrací null tehdy, když na vstupu nebude žádný platný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle? SummaryVisibleRectangle(params Rectangle?[] items) { return _SummaryNRectangle(items, true); }
        /// <summary>
        /// Vrací <see cref="Rectangle"/>?, který je souhrnem všech zadaných <see cref="Rectangle"/>?, které nejsou null a mají Width i Height kladné.
        /// Vrací null tehdy, když na vstupu nebude žádný platný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Rectangle? SummaryVisibleRectangle(this IEnumerable<Rectangle?> items) { return _SummaryNRectangle(items, true); }
        /// <summary>
        /// Vrací <see cref="Rectangle"/>?, který je souhrnem všech zadaných <see cref="Rectangle"/>?.
        /// Vrací null tehdy, když na vstupu nebude žádný platný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="onlyVisible"></param>
        /// <returns></returns>
        private static Rectangle? _SummaryNRectangle(IEnumerable<Rectangle?> items, bool onlyVisible)
        {
            int l = 0;
            int t = 0;
            int r = 0;
            int b = 0;
            bool empty = true;
            foreach (Rectangle? itemN in items)
            {
                if (itemN.HasValue)
                {
                    Rectangle item = itemN.Value;
                    if (onlyVisible && (item.Width <= 0 || item.Height <= 0)) continue;

                    if (empty)
                    {
                        l = item.Left;
                        t = item.Top;
                        r = item.Right;
                        b = item.Bottom;
                        empty = false;
                    }
                    else
                    {
                        if (l > item.Left) l = item.Left;
                        if (t > item.Top) t = item.Top;
                        if (r < item.Right) r = item.Right;
                        if (b < item.Bottom) b = item.Bottom;
                    }
                }
            }
            if (empty) return null;
            return Rectangle.FromLTRB(l, t, r, b);
        }

        /// <summary>
        /// Vrací <see cref="RectangleF"/>, který je souhrnem všech zadaných <see cref="RectangleF"/>.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static RectangleF? SummaryRectangle(this IEnumerable<RectangleF> items)
        {
            float l = 0f;
            float t = 0f;
            float r = 0f;
            float b = 0f;
            bool empty = true;
            foreach (RectangleF item in items)
            {
                if (empty)
                {
                    l = item.Left;
                    t = item.Top;
                    r = item.Right;
                    b = item.Bottom;
                    empty = false;
                }
                else
                {
                    if (l > item.Left) l = item.Left;
                    if (t > item.Top) t = item.Top;
                    if (r < item.Right) r = item.Right;
                    if (b < item.Bottom) b = item.Bottom;
                }
            }
            return RectangleF.FromLTRB(l, t, r, b);
        }

        /// <summary>
        /// Vrací RectangleF, který je souhrnem všech zadaných Rectangle.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static RectangleF? SummaryRectangle(params RectangleF?[] items) { return _SummaryNRectangleF(items, false); }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem všech zadaných Rectangle.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static RectangleF? SummaryRectangle(this IEnumerable<RectangleF?> items) { return _SummaryNRectangleF(items, false); }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem všech zadaných Rectangle, které nejsou null a mají Width i Height kladné.
        /// Vrací null tehdy, když na vstupu nebude žádný platný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static RectangleF? SummaryVisibleRectangle(params RectangleF?[] items) { return _SummaryNRectangleF(items, true); }
        /// <summary>
        /// Vrací RectangleF, který je souhrnem všech zadaných Rectangle, které nejsou null a mají Width i Height kladné.
        /// Vrací null tehdy, když na vstupu nebude žádný platný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static RectangleF? SummaryVisibleRectangle(this IEnumerable<RectangleF?> items) { return _SummaryNRectangleF(items, true); }
        /// <summary>
        /// Vrací <see cref="Rectangle"/>?, který je souhrnem všech zadaných <see cref="Rectangle"/>?.
        /// Vrací null tehdy, když na vstupu nebude žádný platný prvek.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="onlyVisible"></param>
        /// <returns></returns>
        private static RectangleF? _SummaryNRectangleF(IEnumerable<RectangleF?> items, bool onlyVisible)
        {
            float l = 0;
            float t = 0;
            float r = 0;
            float b = 0;
            bool empty = true;
            foreach (RectangleF? itemN in items)
            {
                if (itemN.HasValue)
                {
                    RectangleF item = itemN.Value;
                    if (onlyVisible && (item.Width <= 0 || item.Height <= 0)) continue;

                    if (empty)
                    {
                        l = item.Left;
                        t = item.Top;
                        r = item.Right;
                        b = item.Bottom;
                        empty = false;
                    }
                    else
                    {
                        if (l > item.Left) l = item.Left;
                        if (t > item.Top) t = item.Top;
                        if (r < item.Right) r = item.Right;
                        if (b < item.Bottom) b = item.Bottom;
                    }
                }
            }
            if (empty) return null;
            return RectangleF.FromLTRB(l, t, r, b);
        }

        #endregion
        #region Rectangle a Point: FitInto()
        /// <summary>
        /// Zajistí, že this souřadnice budou umístěny do daného prostoru (disponibleBounds).
        /// Pokud daný prostor je menší, než velikost this, pak velikost this může být zmenšena, anebo this může přesahovat doprava/dolů, podle parametru shrinkToFit
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="disponibleBounds">Souřadnice prostoru, do něhož má být this souřadnice posunuta</param>
        /// <param name="shrinkToFit">true = pokud this má větší velikost než disponibleBounds, pak this bude zmenšeno / false = pak this bude přečnívat doprava / dolů.</param>
        /// <returns></returns>
        public static Rectangle FitInto(this Rectangle bounds, Rectangle disponibleBounds, bool shrinkToFit)
        {
            int dx = disponibleBounds.X;
            int dy = disponibleBounds.Y;
            int dw = disponibleBounds.Width;
            int dh = disponibleBounds.Height;
            int dr = dx + dw;
            int db = dy + dh;

            int x = bounds.X;
            int y = bounds.Y;
            int w = bounds.Width;
            int h = bounds.Height;

            if (x < dx) x = dx;
            if ((x + w) > dr)
            {
                x = dr - w;
                if (x < dx)
                {
                    x = dx;
                    if (shrinkToFit)
                        w = dw;
                }
            }

            if (y < dy) y = dy;
            if ((y + h) > db)
            {
                y = db - h;
                if (y < dy)
                {
                    y = dy;
                    if (shrinkToFit)
                        h = dh;
                }
            }

            return new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// Zajistí, že this souřadnice budou umístěny do daného prostoru (disponibleBounds).
        /// Pokud daný prostor je menší, než velikost this, pak velikost this může být zmenšena, anebo this může přesahovat doprava/dolů, podle parametru shrinkToFit
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="disponibleBounds">Souřadnice prostoru, do něhož má být this souřadnice posunuta</param>
        /// <param name="shrinkToFit">true = pokud this má větší velikost než disponibleBounds, pak this bude zmenšeno / false = pak this bude přečnívat doprava / dolů.</param>
        /// <returns></returns>
        public static RectangleF FitInto(this RectangleF bounds, RectangleF disponibleBounds, bool shrinkToFit)
        {
            float dx = disponibleBounds.X;
            float dy = disponibleBounds.Y;
            float dw = disponibleBounds.Width;
            float dh = disponibleBounds.Height;
            float dr = dx + dw;
            float db = dy + dh;

            float x = bounds.X;
            float y = bounds.Y;
            float w = bounds.Width;
            float h = bounds.Height;

            if (x < dx) x = dx;
            if ((x + w) > dr)
            {
                x = dr - w;
                if (x < dx)
                {
                    x = dx;
                    if (shrinkToFit)
                        w = dw;
                }
            }

            if (y < dy) y = dy;
            if ((y + h) > db)
            {
                y = db - h;
                if (y < dy)
                {
                    y = dy;
                    if (shrinkToFit)
                        h = dh;
                }
            }

            return new RectangleF(x, y, w, h);
        }
        /// <summary>
        /// Zajistí, že this souřadnice bodu bude umístěny do daného prostoru (disponibleBounds).
        /// Vrácený bod tedy bude nejblíže výchozímu bodu, v daném prostoru.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="disponibleBounds">Souřadnice prostoru, do něhož má být this souřadnice posunuta</param>
        /// <returns></returns>
        public static Point FitInto(this Point point, Rectangle disponibleBounds)
        {
            int dx = disponibleBounds.X;
            int dy = disponibleBounds.Y;
            int dw = disponibleBounds.Width;
            int dh = disponibleBounds.Height;
            int dr = dx + dw;
            int db = dy + dh;

            int x = point.X;
            int y = point.Y;

            if (x > dr) x = dr;
            if (x < dx) x = dx;
            if (y > db) y = db;
            if (y < dy) y = dy;

            return new Point(x, y);
        }
        /// <summary>
        /// Zajistí, že this souřadnice bodu bude umístěny do daného prostoru (disponibleBounds).
        /// Vrácený bod tedy bude nejblíže výchozímu bodu, v daném prostoru.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="disponibleBounds">Souřadnice prostoru, do něhož má být this souřadnice posunuta</param>
        /// <returns></returns>
        public static PointF FitInto(this PointF point, RectangleF disponibleBounds)
        {
            float dx = disponibleBounds.X;
            float dy = disponibleBounds.Y;
            float dw = disponibleBounds.Width;
            float dh = disponibleBounds.Height;
            float dr = dx + dw;
            float db = dy + dh;

            float x = point.X;
            float y = point.Y;

            if (x > dr) x = dr;
            if (x < dx) x = dx;
            if (y > db) y = db;
            if (y < dy) y = dy;

            return new PointF(x, y);
        }
        /// <summary>
        /// Metoda je určena typicky k tommu, aby souřadnice nově otevíraného okna byly zarovnány do viditelných souřadnic aktuálních monitorů.
        /// Na vstupu jsou souřadnice formuláře (okna) uložené typicky v předchozím běhu aplikace. 
        /// Nyní může být aplikace spuštěna na jiných monitorech (změna: Desktop - Notebook - Vzdálená plocha, atp).
        /// Je nutno zajistit, aby souřadnice byly správně umístěny do monitorů, kam uživatel vidí, aby mohl aplikaci přemístit, maximalizovat, zavřít atd.
        /// </summary>
        /// <param name="bounds">Souřadnice zadané (původní umístění okna)</param>
        /// <param name="toWorkingArea">Zarovnat do pracovní oblasti monitoru? Hodnota true (default) = do pracovní oblasti (=nikoli do TaskBaru atd)</param>
        /// <param name="toMultiMonitors">Může být výsledná souřadnice natažená přes více monitorů? Není to hezké, ale je to možné. Default = false.</param>
        /// <param name="shrinkToFit">Pokud jsou uloženy souřadnice větší než nynější monitory, dojde k zmenšení - hodnota true je default. 
        /// Hodnota false jen zarovná levý horní roh do viditelné pblasti, ale pravý dolní roh dovolí přesahovat mimo.</param>
        /// <returns></returns>
        public static Rectangle FitIntoMonitors(this Rectangle bounds, bool toWorkingArea = true, bool toMultiMonitors = false, bool shrinkToFit = true)
        {
            var monitorBounds = Screen.AllScreens.Select(s => (toWorkingArea ? s.WorkingArea : s.Bounds)).ToArray();   // Souřadnice monitorů pracovní / úplné
            if (monitorBounds.Length == 0) return bounds;                           // Není žádný monitor => není žádná legrace :-)
            if (monitorBounds.Any(s => s.Contains(bounds))) return bounds;          // Pokud se zadaná souřadnice už teď nachází zcela v některém monitoru, není co upravovat...
            if (monitorBounds.Length > 1 && toMultiMonitors)
            {   // Máme-li více monitorů, a je povoleno umístit souřadnice přes více monitorů:
                var summaryBounds = monitorBounds.SummaryVisibleRectangle().Value;  // Sumární souřadnice z více monitorů = nikdy nevrátí null
                return FitInto(bounds, summaryBounds, shrinkToFit);                 // Prostě souřadnici zarovnáme do sumárního prostoru.
            }
            // Souřadnice máme zarovnat do jednoho konkrétního monitoru. Ale - do kterého?
            var nearestMonitor = bounds.GetNearestBounds(monitorBounds).Value;      // Počet monitorů je nejméně 1, proto metoda GetNearestBounds() nevrátí null...
            return FitInto(bounds, nearestMonitor, shrinkToFit);                    // Prostě danou souřadnici zarovnáme do prostoru nejbližšího monitoru.
        }
        /// <summary>
        /// Metoda vrátí jednu ze souřadnic z pole <paramref name="otherBounds"/>, která je nejbližší k this souřadnici.
        /// Pokud pole <paramref name="otherBounds"/> je null nebo nic neobsahuje, vrací null.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="otherBounds"></param>
        /// <param name="searchOnlyIntersectBounds">false = hledá jen ty otherBounds, které mají kladný průsečík; true = když nenajdeme společný průsečík, budeme hledat i nejbližší vnější otherBounds</param>
        /// <returns></returns>
        public static Rectangle? GetNearestBounds(this Rectangle bounds, IEnumerable<Rectangle> otherBounds, bool searchOnlyIntersectBounds = false)
        {
            // Nejprve kontroly a zkratky:
            if (otherBounds == null) return null;
            var othBounds = otherBounds.ToArray();
            if (othBounds.Length == 0) return null;
            if (othBounds.Length == 1) return othBounds[0];          // Pokud je na vstupu jen jeden prvek, tak je to vždy ten správný :-)

            // Nejprve vyhledáme nějakou souřadnici, se kterou má náš Bounds něco málo společného = určíme průsečík (Intersect) a najdeme největší:
            Rectangle? nearestBounds = null;
            int maxPixels = 0;
            foreach (var othBound in othBounds)
            {
                int pixels = Rectangle.Intersect(bounds, othBound).GetArea();
                if (pixels > maxPixels)
                {
                    nearestBounds = othBound;
                    maxPixels = pixels;
                }
            }
            // Pokud máme výsledek, vrátíme jej. Pokud nemáme, a nemáme hledat externí otherBounds, vrátíme výsledek = null:
            if (nearestBounds.HasValue || searchOnlyIntersectBounds) return nearestBounds;

            // Dané bounds nemá s žádným otherBounds nic společného, vyhledáme tedy nejbližší vnější:
            int minDistance = -1;
            foreach (var othBound in othBounds)
            {
                int distance = GetOuterDistance(bounds, othBound);
                if (distance < 0) distance = 0;
                if (minDistance < 0 || distance < minDistance)
                {
                    nearestBounds = othBound;
                    minDistance = distance;
                }
            }
            return nearestBounds;
        }
        /// <summary>
        /// Vrátí true, pokud dané souřadnice <paramref name="testBounds"/> jsou zcela nebo zčásti viditelné v this prostoru.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="testBounds"></param>
        /// <param name="partial"></param>
        /// <returns></returns>
        public static bool Contains(this Rectangle bounds, Rectangle testBounds, bool partial)
        {
            if (!Contains(bounds.Left, bounds.Right, testBounds.Left, testBounds.Right, partial)) return false;
            if (!Contains(bounds.Top, bounds.Bottom, testBounds.Top, testBounds.Bottom, partial)) return false;
            return true;
        }
        /// <summary>
        /// Vrací true, pokud daný rozsah area (<paramref name="areaBegin"/> až <paramref name="areaEnd"/>) 
        /// plně nebo částečně (<paramref name="partial"/>)
        /// obsahuje daný rozsah test (<paramref name="testBegin"/> až <paramref name="testEnd"/>).
        /// </summary>
        /// <param name="areaBegin"></param>
        /// <param name="areaEnd"></param>
        /// <param name="testBegin"></param>
        /// <param name="testEnd"></param>
        /// <param name="partial"></param>
        /// <returns></returns>
        private static bool Contains(int areaBegin, int areaEnd, int testBegin, int testEnd, bool partial)
        {
            int compare(int a, int b) { return (a < b ? -1 : (a > b ? 1 : 0)); }         // anonymní metoda

            int areaC = compare(areaEnd, areaBegin);                 // +1 když area je kladné, 0 když nulové, -1 když záporné
            int testC = compare(testEnd, testBegin);                 // +1 když test je kladné, 0 když nulové, -1 když záporné
            int beginC = compare(testBegin, areaBegin);              // +1 když testBegin je větší než areaBegin, 0 když jsou shodné, -1 když testBegin je menší než areaBegin
            int endC = compare(testEnd, areaEnd);                    // +1 když testEnd je větší než areaEnd, 0 když jsou shodné, -1 když testEnd je menší než areaEnd

            if (partial)                                                       // Částečná viditelnost:
                return ((areaBegin < areaEnd && testBegin < testEnd) &&        //   Oblasti musí mít kladnou délku, a:
                       !(testEnd <= areaBegin || testBegin >= areaEnd));       //   pokud Test končí dřív, než začne Area, anebo Test začíná později než končí Area, pak Contains = false = nic není viditelné
            else                                                               // Úplná viditelnost:
                return ((areaBegin < areaEnd && testBegin < testEnd) &&        //   Oblasti musí mít kladnou délku, a:
                        (testBegin >= areaBegin && testEnd <= areaEnd));       //   test musí být zcela uvnitř area (test musí začínat v/za začátkem area, a současně musí končit před/v konci area)
        }
        #endregion
        #region Rectangle: GetBorders
        /// <summary>
        /// Metoda vrací souřadnice okrajů daného Rectangle.
        /// Tyto souřadnice lze poté vyplnit (Fill), a budou uvnitř daného Rectangle.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="thick"></param>
        /// <param name="sides"></param>
        /// <returns></returns>
        public static Rectangle[] GetBorders(this Rectangle r, int thick, params RectangleSide[] sides)
        {
            int count = sides.Length;
            Rectangle[] borders = new Rectangle[count];

            int x0 = r.X;
            int x1 = r.Right;
            int w = r.Width;
            int y0 = r.Y;
            int y1 = r.Bottom;
            int h = r.Height;
            int t = (thick >= 0 ? thick : 0);
            int tx = (t < w ? t : w);
            int ty = (t < h ? t : h);

            for (int i = 0; i < count; i++)
            {
                switch (sides[i])
                {
                    case RectangleSide.Left:
                        borders[i] = new Rectangle(x0, y0, tx, h);
                        break;
                    case RectangleSide.Top:
                        borders[i] = new Rectangle(x0, y0, w, ty);
                        break;
                    case RectangleSide.Right:
                        borders[i] = new Rectangle(x1 - tx, y0, tx, h);
                        break;
                    case RectangleSide.Bottom:
                        borders[i] = new Rectangle(x0, y1 - ty, w, ty);
                        break;

                    case RectangleSide.TopLeft:
                        borders[i] = new Rectangle(x0, y0, tx, ty);
                        break;
                    case RectangleSide.TopRight:
                        borders[i] = new Rectangle(x1 - tx, y0, tx, ty);
                        break;
                    case RectangleSide.BottomRight:
                        borders[i] = new Rectangle(x1 - tx, y1 - ty, tx, ty);
                        break;
                    case RectangleSide.BottomLeft:
                        borders[i] = new Rectangle(x0, y1 - ty, tx, ty);
                        break;
                }
            }

            return borders;
        }
        #endregion
        #region SvgImage
        /// <summary>
        /// Vrací stringový obsah daného SVG Image
        /// </summary>
        /// <param name="svgImage"></param>
        /// <returns></returns>
        public static string ToXmlString(this SvgImage svgImage)
        {
            if (svgImage == null) return null;
            byte[] content = null;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                svgImage.Save(ms);
                content = ms.ToArray();
            }
            return Encoding.UTF8.GetString(content);
        }
        #endregion
        #region ConvertToDpi
        /// <summary>
        /// Vrátí this <see cref="Point"/> transformovaný do cílového DPI
        /// </summary>
        /// <param name="designValue"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        public static Point ConvertToDpi(this Point designValue, int targetDpi)
        {
            return ConvertToDpi(designValue, DxComponent.DesignDpi, targetDpi);
        }
        /// <summary>
        /// Vrátí this <see cref="Point"/> transformovaný do cílového DPI
        /// </summary>
        /// <param name="designValue"></param>
        /// <param name="designDpi"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        public static Point ConvertToDpi(this Point designValue, int designDpi, int targetDpi)
        {
            if (!ConvertDpiIsChanged(designDpi, targetDpi, out var ratio)) return designValue;
            return Point.Round(new PointF(ratio * (float)designValue.X, ratio * (float)designValue.Y));
        }
        /// <summary>
        /// Vrátí this <see cref="Size"/> transformovaný do cílového DPI
        /// </summary>
        /// <param name="designValue"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        public static Size ConvertToDpi(this Size designValue, int targetDpi)
        {
            return ConvertToDpi(designValue, DxComponent.DesignDpi, targetDpi);
        }
        /// <summary>
        /// Vrátí this <see cref="Size"/> transformovaný do cílového DPI
        /// </summary>
        /// <param name="designValue"></param>
        /// <param name="designDpi"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        public static Size ConvertToDpi(this Size designValue, int designDpi, int targetDpi)
        {
            if (!ConvertDpiIsChanged(designDpi, targetDpi, out var ratio)) return designValue;
            return Size.Round(new SizeF(ratio * (float)designValue.Width, ratio * (float)designValue.Height));
        }
        /// <summary>
        /// Vrátí this <see cref="Rectangle"/> transformovaný do cílového DPI
        /// </summary>
        /// <param name="designValue"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        public static Rectangle ConvertToDpi(this Rectangle designValue, int targetDpi)
        {
            return ConvertToDpi(designValue, DxComponent.DesignDpi, targetDpi);
        }
        /// <summary>
        /// Vrátí this <see cref="Rectangle"/> transformovaný do cílového DPI
        /// </summary>
        /// <param name="designValue"></param>
        /// <param name="designDpi"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        public static Rectangle ConvertToDpi(this Rectangle designValue, int designDpi, int targetDpi)
        {
            if (!ConvertDpiIsChanged(designDpi, targetDpi, out var ratio)) return designValue;
            return Rectangle.Round(new RectangleF(ratio * (float)designValue.X, ratio * (float)designValue.Y, ratio * (float)designValue.Width, ratio * (float)designValue.Height));
        }
        /// <summary>
        /// Určí, zda existuje změna DPI mezi <paramref name="designDpi"/> a <paramref name="currentDpi"/>, vrátí true = změna a nastaví <paramref name="ratio"/> = koeficient změny.
        /// </summary>
        /// <param name="designDpi"></param>
        /// <param name="currentDpi"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        private static bool ConvertDpiIsChanged(int designDpi, int currentDpi, out float ratio)
        {
            ratio = 0f;
            if (designDpi <= 0 || currentDpi <= 0 || designDpi == currentDpi) return false;
            ratio = (float)currentDpi / (float)designDpi;
            return true;
        }
        #endregion
    }
}
