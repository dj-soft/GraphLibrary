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
                return new MenuAction[]
                {
                    new MenuAction()
                    {
                        Text = App.Messages.AppearanceMenuPasswordShowNowText,
                        ToolTip = App.Messages.AppearanceMenuPasswordShowNowToolTip,
                        Action = ActionType.ShowNow
                    },

                };
            }
        }
        public class MenuAction : DataMenuItem
        {
            public ActionType Action { get; set; }
            public override void Process()
            {
                base.Process();
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
