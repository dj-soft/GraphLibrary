using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    internal class Passwords
    {
        public static MenuAction[] MenuActions
        {
            get
            {
                var passwordMode = App.Settings.PasswordPageMode;
                return new MenuAction[]
                {
                    new MenuAction()
                    {
                        Text = App.Messages.AppearanceMenuPasswordShowNowText,
                        ToolTip = App.Messages.AppearanceMenuPasswordShowNowToolTip,
                        Code = PasswordPageShowMode.ShowNow,
                        Action = PasswordPageShowMode.ShowNow,
                        FontStyle = (passwordMode == PasswordPageShowMode.ShowNow? FontStyle.Bold : FontStyle.Regular)
                    },

                    new MenuAction()
                    {
                        Text = App.Messages.AppearanceMenuPasswordShowPageText,
                        ToolTip = App.Messages.AppearanceMenuPasswordShowPageToolTip,
                        Code = PasswordPageShowMode.ShowPage,
                        Action = PasswordPageShowMode.ShowPage,
                        FontStyle = (passwordMode == PasswordPageShowMode.ShowNow ? FontStyle.Bold : FontStyle.Regular)
                        // Image = (isPageVisible ? Properties.Resources.games_endturn_2_22 : null)
                    },

                    new MenuAction()
                    {
                        Text = App.Messages.AppearanceMenuPasswordHidePageText,
                        ToolTip = App.Messages.AppearanceMenuPasswordHidePageToolTip,
                        Code = PasswordPageShowMode.HidePage,
                        Action = PasswordPageShowMode.HidePage,
                        FontStyle = (passwordMode == PasswordPageShowMode.HidePage ? FontStyle.Bold : FontStyle.Regular)
                        // Image = (!isPageVisible ? Properties.Resources.games_endturn_2_22 : null)
                    }
                };
            }
        }
        public class MenuAction : DataMenuItem
        {
            /// <summary>
            /// Akce, kterou provádí tento prvek menu
            /// </summary>
            public PasswordPageShowMode Action { get; set; }
            public override void Process()
            {
                App.Settings.PasswordPageMode = this.Action;
            }
        }

    }
    /// <summary>
    /// Režim zobrazení okna s hesly
    /// </summary>
    public enum PasswordPageShowMode
    {
        None,
        ShowNow,
        ShowPage,
        HidePage
    }
}

namespace DjSoft.Tools.ProgramLauncher
{
    #region Část Settings, která ukládá a načítá pozici a stav formulářů
    partial class Settings
    {

        /// <summary>
        /// Viditelnost stránky "Hesla"
        /// </summary>
        [Data.PropertyName("passwordpagevisible")]
        public Data.PasswordPageShowMode PasswordPageMode { get { return __PasswordPageMode; } set { __PasswordPageMode = value; SetChanged(); } } private Data.PasswordPageShowMode __PasswordPageMode;
    }
    #endregion
}