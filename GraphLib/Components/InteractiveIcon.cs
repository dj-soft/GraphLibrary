using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Třída zahrnující sadu ikon, které jsou vykresleny do controlu podle interaktivního stavu
    /// </summary>
    public class InteractiveIcon
    {
        #region Standardní instanční proměnné
        /// <summary>
        /// Konsturktor
        /// </summary>
        public InteractiveIcon() { }
        /// <summary>
        /// Ikona pro běžný stav (ne Disabled, ne MouseOver, ne MouseDown) anebo ikona pro tyto jiné stavy, když není ikona pro konkrétní stav definovaná
        /// </summary>
        public Image ImageStandard { get; set; }
        /// <summary>
        /// Ikona pro stav Disabled
        /// </summary>
        public Image ImageDisabled { get; set; }
        /// <summary>
        /// Ikona pro stav MouseOver
        /// </summary>
        public Image ImageMouseOver { get; set; }
        /// <summary>
        /// Ikona pro stav MouseDown
        /// </summary>
        public Image ImageMouseDown { get; set; }
        /// <summary>
        /// Dynamicky získaná ikona - používá se pro standardní ikony, které čtou obrázek ze Skinu
        /// </summary>
        public Func<GInteractiveState, Image> ImageDynamic { get; set; }
        /// <summary>
        /// Vrací Image pro daný interaktivní stav objektu
        /// </summary>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        public Image GetImage(GInteractiveState interactiveState)
        {
            Image image = _GetStandardImage(interactiveState);
            if (image == null)
                ImageDynamic?.Invoke(interactiveState);
            if (image == null)
                image = (interactiveState.HasFlag(GInteractiveState.Disabled) ? ImageDisabled :
                        (interactiveState.HasFlag(GInteractiveState.FlagOver) ? ImageMouseOver :
                        (interactiveState.HasFlag(GInteractiveState.FlagDown) ? ImageMouseDown : null)));
            if (image == null)
                image = ImageStandard;

            return image;
        }
        #endregion
        #region Vykreslení ikony
        /// <summary>
        /// Vykreslí zdejší ikonu pro daný interaktivní stav
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="interactiveState"></param>
        /// <param name="drawAsShadow"></param>
        public void DrawIcon(Graphics graphics, Rectangle absoluteBounds, GInteractiveState interactiveState = GInteractiveState.Enabled, bool drawAsShadow = false)
        {
            Image image = GetImage(interactiveState);
            if (image == null) return;
            if (drawAsShadow)
                GPainter.DrawImage(graphics, absoluteBounds, image, 0.45f);
            else
                GPainter.DrawImage(graphics, absoluteBounds, image, interactiveState);
        }
        #endregion
        #region Statické instance pro standardní ikony: existuje jen jedna instance pro jeden typ standardní ikony, tato instance se navazuje do všech potřebných míst
        /// <summary>
        /// Ikona pro zobrazení vztahu na záznam
        /// </summary>
        public static InteractiveIcon RelationRecord { get { if (_RelationRecord == null) _RelationRecord = new InteractiveIcon(StandardIconType.RelationRecord); return _RelationRecord; } } private static InteractiveIcon _RelationRecord;
        /// <summary>
        /// Ikona pro zobrazení vztahu na dokument
        /// </summary>
        public static InteractiveIcon RelationDocument { get { if (_RelationDocument == null) _RelationDocument = new InteractiveIcon(StandardIconType.RelationDocument); return _RelationDocument; } } private static InteractiveIcon _RelationDocument;
        /// <summary>
        /// Ikona pro otevření složky
        /// </summary>
        public static InteractiveIcon OpenFolder { get { if (_OpenFolder == null) _OpenFolder = new InteractiveIcon(StandardIconType.OpenFolder); return _OpenFolder; } } private static InteractiveIcon _OpenFolder;
        /// <summary>
        /// Ikona pro otevření složky
        /// </summary>
        public static InteractiveIcon Calculator { get { if (_Calculator == null) _Calculator = new InteractiveIcon(StandardIconType.Calculator); return _Calculator; } } private static InteractiveIcon _Calculator;
        /// <summary>
        /// Ikona pro otevření složky
        /// </summary>
        public static InteractiveIcon Calendar { get { if (_Calendar == null) _Calendar = new InteractiveIcon(StandardIconType.Calendar); return _Calendar; } } private static InteractiveIcon _Calendar;
        /// <summary>
        /// Ikona pro DropDown
        /// </summary>
        public static InteractiveIcon DropDown { get { if (_DropDown == null) _DropDown = new InteractiveIcon(StandardIconType.DropDown); return _DropDown; } } private static InteractiveIcon _DropDown;
        #endregion
        #region Standardní ikony navázané na Skin
        /// <summary>
        /// Privátní konstruktor pro standardní ikonu - uloží si její typ, ale konkrétní obrázek se získává až v okamžiku potřeby = při kreslení, ze SKinu, tedy funguje dynamicky i po změně skinu
        /// </summary>
        /// <param name="iconType"></param>
        private InteractiveIcon(StandardIconType iconType) { _IconType = iconType; }
        /// <summary>
        /// Vrátí standardní ikonu = podle typu vrátí aktuálně platnou ikonu ze skinu, kde může být kdykoliv změněna a přitom se nemusí měnit obsah instance <see cref="InteractiveIcon"/>
        /// </summary>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        private Image _GetStandardImage(GInteractiveState interactiveState)
        {
            if (_IconType.HasValue)
            {
                switch (_IconType.Value)
                {
                    case StandardIconType.RelationRecord: return Skin.TextBox.IconRelationRecord;
                    case StandardIconType.RelationDocument: return Skin.TextBox.IconRelationDocument;
                    case StandardIconType.OpenFolder: return Skin.TextBox.IconOpenFolder;
                    case StandardIconType.Calculator: return Skin.TextBox.IconCalculator;
                    case StandardIconType.Calendar: return Skin.TextBox.IconCalendar;
                    case StandardIconType.DropDown: return Skin.TextBox.IconDropDown;
                }
            }
            return null;
        }
        /// <summary>
        /// Standardní ikona
        /// </summary>
        private StandardIconType? _IconType = null;
        /// <summary>
        /// Typy standardních ikon
        /// </summary>
        private enum StandardIconType
        {
            None = 0,
            RelationRecord,
            RelationDocument,
            OpenFolder,
            Calculator,
            Calendar,
            DropDown
        }
        #endregion
    }
}
