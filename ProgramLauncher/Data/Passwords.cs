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
                        
                    },


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
