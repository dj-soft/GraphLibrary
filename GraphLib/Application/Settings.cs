using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler
{
    public class Settings
    {


        #region Static deklarace chování třídy - lze změnit v rámci celé aplikace
        /// <summary>
        /// Defaultní hodnota SelectAll = vybrat celý text po příchodu focusu do prvku
        /// </summary>
        public static bool TextBoxSelectAll { get; set; } = true;
        /// <summary>
        /// Definice chování: při odchodu focusu z prvku a opětovném návratu docusu se má pamatovat pozice kurzoru?
        /// Default = false = chování jako v Green (Infragistic), Notepadu, Firefox, TotalCommander (Po změně focusu se kurzor nastaví na index 0),
        /// hodnota true = chování jako v Office, Visual studio, OpenOffice (Textbox si pamatuje pozici kurzoru)
        /// </summary>
        public static bool TextBoxSaveCursorPositionOnLostFocus { get; set; } = false;
        /// <summary>
        /// Definice chování: při kliknutí pravou myší (=kontextové menu) se má přemístit kurzor stejně, jako při kliknutí levou myší?
        /// Default = false = chování jako v Green (Infragistic), Notepadu, Firefox, TotalCommander (RightClick nemění pozici kurzoru),
        /// hodnota true = chování jako v Office, Visual studio, OpenOffice (RightClick změní pozici kurzoru)
        /// </summary>
        public static bool TextBoxChangeCursorPositionOnRightMouse { get; set; } = false;
        /// <summary>
        /// Definice chování: při kliknutí levou myší se stisknutým Control se má označit celé slovo pod myší?
        /// Default = false = chování jako v Green (Infragistic), Notepadu, Firefox, TotalCommander (Control+Click neoznačí slovo),
        /// hodnota true = chování jako v Office, Visual studio, OpenOffice (Control+Click označí celé slovo)
        /// </summary>
        public static bool TextBoxSelectWordOnControlMouse { get; set; } = true;
        /// <summary>
        /// Znaky, které akceptujeme jako vnitřní součást slova, rovnocenné písmenům a číslicím
        /// </summary>
        public static string CharactersAssumedAsWords { get; set; } = "_";
        #endregion
        /// <summary>
        /// TabStop zastaví na prvcích, které mají ReadOnly = true? Default = false: do ReadOnly (true) prvků se nechá dostat jen myší, ale ne klávesou TAB = ta je přeskočí.
        /// </summary>
        public static bool TabStopOnReadOnlyItems { get; set; } = false;
    }
}
