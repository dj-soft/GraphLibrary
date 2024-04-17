using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;
using Noris.Clients.Win.Components;


namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy okno dialogů <see cref="Noris.Clients.Win.Components.DialogForm"/>
    /// </summary>
    [RunFormInfo(groupText: "Testovací okna", buttonText: "DialogForm", buttonOrder: 40, buttonImage: "svgimages/richedit/inserttextbox.svg", buttonToolTip: "Otevře okno pro testování dialogů")]
    public class DxDialogTestForm : DxRibbonForm
    {
        protected override void DxMainContentPrepare()
        {
            // Přidej buttony, jejich Text je zobrazen uživateli a současně je klíčem do metody _Click:
            DxComponent.CreateDxSimpleButton(40, 20, 280, 37, this.DxMainPanel, "Dialog [ OK ]", _Click);
            DxComponent.CreateDxSimpleButton(40, 60, 280, 37, this.DxMainPanel, "Dialog [ OK ] / Center", _Click);
            DxComponent.CreateDxSimpleButton(40, 100, 280, 37, this.DxMainPanel, "Dialog s detailem", _Click);
            DxComponent.CreateDxSimpleButton(40, 140, 280, 37, this.DxMainPanel, "XXXX", _Click);
            DxComponent.CreateDxSimpleButton(40, 180, 280, 37, this.DxMainPanel, "XXXX", _Click);
            DxComponent.CreateDxSimpleButton(40, 220, 280, 37, this.DxMainPanel, "XXXX", _Click);
        }
        private void _Click(object sender, EventArgs e) 
        {
            if (!(sender is DxSimpleButton button)) return;
            string text = button.Text;
            if (string.IsNullOrEmpty(text)) return;

            DialogArgs dialogArgs = new DialogArgs() { SystemIcon = DialogSystemIcon.None };

            string resourceCopy1 = "devav/actions/copy.svg";
            string resourceCopy2 = "images/xaf/templatesv2images/action_copy.svg";
            string resourceAltx1 = "svgimages/diagramicons/showprintpreview.svg";

            switch (text)
            {
                case "Dialog [ OK ]":
                    dialogArgs.Title = "Dialog [OK]; " + Randomizer.GetSentences(3, 7, 4, 6);
                    dialogArgs.SystemIcon = DialogSystemIcon.Information;
                    dialogArgs.PrepareButtons(DialogResult.OK);
                    dialogArgs.MessageText = Randomizer.GetSentences(4, 8, 3, 12);
                    dialogArgs.StatusBarCtrlCImage = resourceCopy1;
                    break;
                case "Dialog [ OK ] / Center":
                    dialogArgs.Title = text;
                    dialogArgs.SystemIcon = DialogSystemIcon.Information;
                    dialogArgs.PrepareButtons(DialogResult.OK);
                    dialogArgs.MessageText = "Jistě, pane ministře.";
                    dialogArgs.AutoCenterSmallText = true;
                    dialogArgs.ButtonsAlignment = AlignContentToSide.Center;
                    dialogArgs.StatusBarCtrlCText = null;
                    dialogArgs.StatusBarVisible = false;
                    break;
                case "Dialog s detailem":
                    dialogArgs.Title = text;
                    dialogArgs.SystemIcon = DialogSystemIcon.Information;
                    dialogArgs.PrepareButtons(DialogResult.OK);
                    dialogArgs.MessageText = Randomizer.GetSentences(4, 8, 3, 12);
                    dialogArgs.AltMessageText = Randomizer.GetSentences(4, 12, 12, 24);
                    dialogArgs.ButtonsAlignment = AlignContentToSide.Begin;
                    dialogArgs.StatusBarCtrlCImage = resourceCopy1;
                    dialogArgs.StatusBarAltMsgButtonImage = resourceAltx1;
                    break;
                default:
                    AsolDX.News.PdfPrinter.PrintArgs pa = new AsolDX.News.PdfPrinter.PrintArgs();
                    pa.PageRange = "-3, 5, 12 - 15,  21....25; 22, 48, 47-";
                    var sett = pa.CreateSettings(50);
                    var pgnm = sett.PageNumbers;

                    string fileName = _GetRandomPdf();
                    AsolDX.News.PdfPrinter.PrintWithProcess(fileName, pa);

                    break;
            }


            if (dialogArgs.SystemIcon != DialogSystemIcon.None)
            {
                dialogArgs.Owner = this;
                var result = DialogForm.ShowDialog(dialogArgs);

            }
        }
        private static string _GetRandomPdf()
        {
            string fileName;
            if (tryGetRandomPdfFrom("D:\\TiskPDF\\", out fileName)) return fileName;
            if (tryGetRandomPdfFrom("C:\\TiskPDF\\", out fileName)) return fileName;

            return null;

            bool tryGetRandomPdfFrom(string path, out string result)
            {
                if (System.IO.Directory.Exists(path))
                {
                    var files = System.IO.Directory.GetFiles(path, "*.pdf");
                    if (files.Length > 0) 
                    {
                        result = Randomizer.GetItem(files);
                        return true;
                    }
                }
                result = null;
                return false;
            }
        }
    }
}
