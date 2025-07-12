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
                bool isPageVisible = App.Settings.PasswordPageVisible;
                return new MenuAction[]
                {
                    new MenuAction()
                    {
                        Text = App.Messages.AppearanceMenuPasswordShowNowText,
                        ToolTip = App.Messages.AppearanceMenuPasswordShowNowToolTip,
                        Action = ActionType.ShowNow
                    },

                    new MenuAction()
                    {
                        Text = App.Messages.AppearanceMenuPasswordShowPageText,
                        ToolTip = App.Messages.AppearanceMenuPasswordShowPageToolTip,
                        Action = ActionType.ShowPage,
                        FontStyle = (isPageVisible ? FontStyle.Bold : FontStyle.Regular),
                        Image = (isPageVisible ? Properties.Resources.games_endturn_2_22 : null)
                    },

                    new MenuAction()
                    {
                        Text = App.Messages.AppearanceMenuPasswordHidePageText,
                        ToolTip = App.Messages.AppearanceMenuPasswordHidePageToolTip,
                        Action = ActionType.HidePage,
                        FontStyle = (!isPageVisible ? FontStyle.Bold : FontStyle.Regular),
                        Image = (!isPageVisible ? Properties.Resources.games_endturn_2_22 : null)
                    }
                };
            }
        }
        public class MenuAction : DataMenuItem
        {
            /// <summary>
            /// Akce, kterou provádí tento prvek menu
            /// </summary>
            public ActionType Action { get; set; }
            public override void Process()
            {
                switch (this.Action)
                {
                    case ActionType.ShowPage:
                        App.Settings.PasswordPageVisible = true;
                        break;
                    case ActionType.HidePage:
                        App.Settings.PasswordPageVisible = false;
                        break;
                }
            }
        }

        public enum ActionType
        {
            None,
            ShowNow,
            ShowPage,
            HidePage
        }
    }
}
