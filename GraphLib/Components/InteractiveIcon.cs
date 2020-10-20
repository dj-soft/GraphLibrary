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
        /// Vrací Image pro daný interaktivní stav objektu
        /// </summary>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        public Image GetImage(GInteractiveState interactiveState)
        {
            Image image = (interactiveState.HasFlag(GInteractiveState.Disabled) ? ImageDisabled :
                          (interactiveState.HasFlag(GInteractiveState.FlagOver) ? ImageMouseOver :
                          (interactiveState.HasFlag(GInteractiveState.FlagDown) ? ImageMouseDown : null)));
            if (image == null) image = ImageStandard;
            return image;
        }
        #endregion
        #region Statické property pro konkrétní situace
        /// <summary>
        /// Ikona pro zobrazení vztahu na záznam
        /// </summary>
        public static InteractiveIcon RelationRecord { get { if (_RelationRecord == null) _RelationRecord = _CreateRelationRecord(); return _RelationRecord; } }
        private static InteractiveIcon _CreateRelationRecord()
        {
            InteractiveIcon icon = new InteractiveIcon()
            {
                ImageStandard = Skin.TextBox.IconRelationRecord
            };
            return icon;
        }
        private static InteractiveIcon _RelationRecord;
        /// <summary>
        /// Ikona pro zobrazení vztahu na dokument
        /// </summary>
        public static InteractiveIcon RelationDocument { get { if (_RelationDocument == null) _RelationDocument = _CreateRelationDocument(); return _RelationDocument; } }
        private static InteractiveIcon _CreateRelationDocument()
        {
            InteractiveIcon icon = new InteractiveIcon()
            {
                ImageStandard = Skin.TextBox.IconRelationDocument
            };
            return icon;
        }
        private static InteractiveIcon _RelationDocument;
        #endregion
    }
}
