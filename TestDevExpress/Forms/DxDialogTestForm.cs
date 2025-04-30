using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;
using Noris.Clients.Win.Components;
using TestDevExpress.Components;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy okno dialogů <see cref="Noris.Clients.Win.Components.DialogForm"/>
    /// </summary>
    [RunTestForm(groupText: "Testovací okna", buttonText: "DialogForm", buttonOrder: 40, buttonImage: "svgimages/richedit/inserttextbox.svg", buttonToolTip: "Otevře okno pro testování dialogů")]
    public class DxDialogTestForm : DxRibbonForm
    {
        protected override void DxMainContentPrepare()
        {
            Counter = (Counter % 6) + 1;
            this.Text = $"DialogBox tester [{Counter}]";

            // Přidej buttony, jejich Text je zobrazen uživateli a současně je klíčem do metody _Click:
            DxComponent.CreateDxSimpleButton(40, 20, 280, 37, this.DxMainPanel, "Dialog [ OK ]", _Click, tag: "DialogOK");
            DxComponent.CreateDxSimpleButton(40, 60, 280, 37, this.DxMainPanel, "Dialog [ OK ] / Center", _Click, tag: "DialogOKCenter");
            DxComponent.CreateDxSimpleButton(40, 100, 280, 37, this.DxMainPanel, "Dialog s detailem", _Click, tag: "DialogDetail");
            DxComponent.CreateDxSimpleButton(40, 140, 280, 37, this.DxMainPanel, "Ano / Ne", _Click, tag: "AnoNe");
            DxComponent.CreateDxSimpleButton(40, 180, 280, 37, this.DxMainPanel, "v poho / zapomeň", _Click, tag: "VPohoZapom");
            // DxComponent.CreateDxSimpleButton(40, 220, 280, 37, this.DxMainPanel, "XXXX", _Click, tag: "DialogOK");


            __ResultLabel = DxComponent.CreateDxLabel(40, 500, 300, this.DxMainPanel, "", LabelStyleType.MainTitle);

        }
        protected override void DxMainContentDoLayout(bool isSizeChanged)
        {
            if (__ResultLabel != null)
                __ResultLabel.Bounds = new System.Drawing.Rectangle(40, this.DxMainPanel.ClientSize.Height - 12 - __ResultLabel.Height, 500, __ResultLabel.Height);

        }
        private string _ResultText
        {
            get
            {
                if (__ResultLabel != null)
                    return __ResultLabel.Text;
                return null;
            }
            set
            {
                if (__ResultLabel != null)
                    __ResultLabel.Text = value;
            }
        }
        private DxLabelControl __ResultLabel;
        private static int Counter = 0;
        private void _Click(object sender, EventArgs e) 
        {
            _ResultText = "";

            if (!(sender is DxSimpleButton button)) return;
            if (!(button.Tag is string)) return;
            string command = button.Tag as string;
            if (string.IsNullOrEmpty(command)) return;
            string title = button.Text;

            DialogArgs dialogArgs = new DialogArgs() { SystemIcon = DialogSystemIcon.None };

            string resourceCopy1 = "devav/actions/copy.svg";
            string resourceCopy2 = "images/xaf/templatesv2images/action_copy.svg";
            string resourceAltx1 = "svgimages/diagramicons/showprintpreview.svg";

            switch (command)
            {
                case "DialogOK":
                    dialogArgs.Title = title;
                    dialogArgs.SystemIcon = DialogSystemIcon.Information;
                    dialogArgs.PrepareButtons(DialogResult.OK);
                    dialogArgs.MessageText = Randomizer.GetSentences(4, 8, 3, 12);
                    dialogArgs.StatusBarCtrlCImage = resourceCopy1;
                    break;
                case "DialogOKCenter":
                    dialogArgs.Title = title;
                    dialogArgs.SystemIcon = DialogSystemIcon.Information;
                    dialogArgs.PrepareButtons(DialogResult.OK);
                    dialogArgs.MessageText = "Jistě, pane ministře.";
                    dialogArgs.AutoCenterSmallText = true;
                    dialogArgs.ButtonsAlignment = AlignContentToSide.Center;
                    dialogArgs.StatusBarCtrlCText = null;
                    dialogArgs.StatusBarVisible = false;
                    break;
                case "DialogDetail":
                    dialogArgs.Title = title;
                    dialogArgs.SystemIcon = DialogSystemIcon.Information;
                    dialogArgs.PrepareButtons(DialogResult.OK);
                    dialogArgs.MessageText = Randomizer.GetSentences(4, 8, 3, 12);
                    dialogArgs.AltMessageText = Randomizer.GetSentences(4, 12, 12, 24);
                    dialogArgs.ButtonsAlignment = AlignContentToSide.Begin;
                    dialogArgs.StatusBarCtrlCImage = resourceCopy1;
                    dialogArgs.StatusBarAltMsgButtonImage = resourceAltx1;
                    break;
                case "AnoNe":
                    dialogArgs.Title = title;
                    dialogArgs.SystemIcon = DialogSystemIcon.Question;
                    dialogArgs.PrepareButtons(DialogResult.Yes, DialogResult.No);
                    dialogArgs.MessageText = Randomizer.GetSentences(4, 8, 3, 12);
                    dialogArgs.AltMessageText = Randomizer.GetSentences(4, 12, 12, 24);
                    dialogArgs.ButtonsAlignment = AlignContentToSide.Center;
                    dialogArgs.StatusBarCtrlCImage = resourceCopy1;
                    dialogArgs.StatusBarAltMsgButtonImage = resourceAltx1;
                    break;
                case "VPohoZapom":
                    dialogArgs.Title = title;
                    dialogArgs.SystemIcon = DialogSystemIcon.Question;
                    dialogArgs.Buttons.Add(new DialogArgs.ButtonInfo() { Text = "v pohodě", IsImplicitButton = true, IsInitialButton = true, ActiveKey = Keys.A, ResultValue = 1, StatusBarText = "Tohle je v pohodě" });
                    dialogArgs.Buttons.Add(new DialogArgs.ButtonInfo() { Text = "na to zapomeň", IsImplicitButton = false, IsInitialButton = false, ActiveKey = Keys.N, ResultValue = 2, StatusBarText = "Tudy cesta nevede" });
                    dialogArgs.MessageText = Randomizer.GetSentences(4, 8, 3, 12);
                    dialogArgs.ButtonsAlignment = AlignContentToSide.Center;
                    dialogArgs.StatusBarCtrlCImage = resourceCopy1;
                    dialogArgs.StatusBarAltMsgButtonImage = resourceAltx1;
                    break;

                default:
                    /*
                    var printers = PdfPrinter.InstalledPrinters;
                    var defPrinter = PdfPrinter.DefaultPrinterName;

                    var pa = new PdfPrinter.PrintArgs();
                    pa.PageRange = "-3, 5, 12 - 15,  21....25; 22, 48, 47-";
                    var sett = pa.CreateSettings(50);
                    var pgnm = sett.PageNumbers;

                    string fileName = _GetRandomPdf();
                    // PdfPrinter.PrintWithProcess(fileName, pa);
                    PdfPrinter.PrintWithControl(fileName, pa);
                    */
                    break;
            }


            if (dialogArgs.SystemIcon != DialogSystemIcon.None)
            {
                dialogArgs.Owner = this;
                var result = DialogForm.ShowDialog(dialogArgs);

                _ResultText = (result is null ? "" : "ResultDialog: " +  result.ToString());
            }
        }
        private static string _GetRandomPdf()
        {
            string fileName;
            if (tryGetRandomPdfFrom("D:\\TiskPDF\\", out fileName)) return fileName;
            if (tryGetRandomPdfFrom("C:\\DavidPriv\\PdfPrint\\2024Doc\\", out fileName)) return fileName;
            if (tryGetRandomPdfFrom("C:\\DavidPriv\\PdfPrint\\2023\\", out fileName)) return fileName;

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
