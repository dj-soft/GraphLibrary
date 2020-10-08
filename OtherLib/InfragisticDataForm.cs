using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

using DW = System.Drawing;
using WF = System.Windows.Forms;

using IG = Infragistics.Win.UltraWinGrid;

namespace Asol.Tools.WorkScheduler.DevExpressTest
{
    public class IfManager
    {
        public static WF.Control CreateWinFormDataFormControl() { return new InfragisticDataFormWrap(); }
        public static void AddDataFormItems(WF.Control control, int countX, int countY)
        {
            if (control is InfragisticDataFormWrap)
                (control as InfragisticDataFormWrap).AddDataFormItems(countX, countY);
        }
    }
    /// <summary>
    /// Obálka okolo <see cref="InfragisticDataForm"/>. 
    /// Třída <see cref="InfragisticDataForm"/> je <see cref="IG.UltraGrid"/>, a ten nemá dostupný <see cref="WF.ScrollableControl.AutoScroll"/>
    /// </summary>
    internal class InfragisticDataFormWrap : WF.ScrollableControl
    {
        public InfragisticDataFormWrap()
        {
            _DataForm = new InfragisticDataForm() { Location = new DW.Point(0, 0) };
            this.Controls.Add(_DataForm);
            this.AutoScroll = true;
        }
        private InfragisticDataForm _DataForm;
        internal void AddDataFormItems(int countX, int countY)
        {
            this.SuspendLayout();

            _DataForm.AddDataFormItems(countX, countY);

            this.ResumeLayout(true);
        }
    }
    /// <summary>
    /// Přímý potomek <see cref="IG.UltraGrid"/>
    /// </summary>
    internal class InfragisticDataForm : IG.UltraGrid
    {
        public InfragisticDataForm()
        {
        }
        int _CountX;
        int _CountY;
        DataTable _DataTable;

        internal void AddDataFormItems(int countX, int countY)
        {
            try
            {
                _CountX = countX;
                _CountY = countY;
                _DataTable = CreateDataTable(countX, countY);

                this.DataSource = _DataTable;              // Tady dojde k vyvolání OnInitializeLayout() => CreateLayout() => CreateGridLayout()
                this.DataBind();
            }
            catch (Exception exc)
            {
                WF.MessageBox.Show(exc.Message, exc.GetType().FullName, WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Error);
            }
        }

        protected override void OnInitializeLayout(IG.InitializeLayoutEventArgs e)
        {
            base.OnInitializeLayout(e);
            CreateLayout(e.Layout);
        }

        private void CreateLayout(IG.UltraGridLayout layout)
        {
            this.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.BeginUpdate();

            CreateGridLayout(this, layout, _CountX, _CountY);

            this.EndUpdate();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(true);
        }
        #region Tvorba DataTable
        private static DataTable CreateDataTable(int countX, int countY)
        {
            var dataTable = new DataTable();
            int index = 0;
            for (int rowIdx = 0; rowIdx < countY; rowIdx++)
                for (int colIdx = 0; colIdx < countX; colIdx++)
                    dataTable.Columns.Add(CreateColumn(index++, rowIdx, colIdx));

            dataTable.Rows.Add(CreateRowValues(countX, countY));

            return dataTable;
        }
        private static DataColumn CreateColumn(int index, int rowIdx, int colIdx)
        {
            DataColumn column = new DataColumn(CreateColumnName(rowIdx, colIdx), typeof(string));
            column.Caption = $"Item {index}";
            return column;
        }
        private static object[] CreateRowValues(int countX, int countY)
        {
            var rand = new Random();
            string[] texts = new string[] { "VFA-2019", "VP", "DOC-INT", "PRJ", "VYD", "IUD-2019", "MAT", "ZFO", "ČJ" };
            string[] suffixes = new string[] { "vyřešit ihned", "porada nutná", "rozhoduje pouze šéf", "neřešit, založit", "utajený dokument", "Extra délka doplňkového textu, přesahující 32 znaků" };

            int count = countX * countY;
            object[] values = new object[count];
            int index = 0;
            for (int rowIdx = 0; rowIdx < countY; rowIdx++)
                for (int colIdx = 0; colIdx < countX; colIdx++)
                {
                    values[index] = CreateRowValue(index, rowIdx, colIdx, texts, suffixes, rand);
                    index++;
                }

            return values;
        }
        private static string CreateRowValue(int index, int rowIdx, int colIdx, string[] texts, string[] suffixes, Random rand)
        {
            string value = texts[rand.Next(texts.Length)] + "_" + rand.Next(10000, 99999).ToString();
            if (rand.Next(10) <= 3) value += ": " + suffixes[rand.Next(suffixes.Length)];
            return value;
        }
        private static string CreateColumnName(int rowIdx, int colIdx)
        {
            return $"column_{rowIdx}_{colIdx}";
        }
        #endregion
        #region Tvorba layoutu

        private static IG.UltraGridLayout CreateGridLayout(IG.UltraGrid grid, IG.UltraGridLayout layout, int countX, int countY)
        {
            if (layout == null) layout = grid.DisplayLayout.Clone();
            layout.Key = "FreeFormLayout";
            layout.Grid?.BeginUpdate();

            IG.UltraGridBand gridBand = layout?.Bands[0] ?? grid.Rows.Band;

            ActivateFreeForm(grid, gridBand);

            #region Souřadnice a velikosti

            int maxR = 0;
            int maxB = 0;

            int itemX0 = 6;
            int itemY0 = 6;
            int itemXS = 3;
            int itemYS = 3;
            int itemW = 220;
            int itemH = 25;
            int itemX = itemX0;
            int itemY = itemY0;

            int labelW = 100;
            int labelH = 18;
            DW.Size labelSize = new DW.Size(labelW, labelH);

            int textW = 110;
            int textH = 22;
            DW.Size textSize = new DW.Size(textW, textH);
            #endregion

            int tabIndex = 0;
            for (int rowIdx = 0; rowIdx < countY; rowIdx++)
            {
                itemX = itemX0;
                for (int colIdx = 0; colIdx < countX; colIdx++)
                {
                    DW.Rectangle itemBounds = new DW.Rectangle(itemX, itemY, itemW, itemH);

                    string columnName = CreateColumnName(rowIdx, colIdx);
                    IG.UltraGridColumn column = gridBand.Columns[columnName];
                    column.TabIndex = ++tabIndex;

                    var colLayout = column.RowLayoutColumnInfo;
                    colLayout.OriginX = itemBounds.X;
                    colLayout.OriginY = itemBounds.Y;
                    colLayout.SpanX = textSize.Width;
                    colLayout.SpanY = textSize.Height;
                    colLayout.PreferredCellSize = textSize; //  itemBounds.Size;

                    colLayout.LabelPosition = IG.LabelPosition.Left;
                    colLayout.PreferredLabelSize = labelSize;
                    colLayout.ActualLabelSize = labelSize;
                    colLayout.LabelSpan = labelW;

                    if (maxR < itemBounds.Right) maxR = itemBounds.Right;
                    if (maxB < itemBounds.Bottom) maxB = itemBounds.Bottom;

                    itemX = itemX + itemW + itemXS;
                }
                itemY = itemY + itemH + itemYS;
            }

            grid.Size = new DW.Size(maxR + itemX0, maxB + itemY0);

            layout.ViewStyle = Infragistics.Win.UltraWinGrid.ViewStyle.SingleBand;
            layout.Grid?.EndUpdate();

            if (layout.Rows.Count > 0)
                grid.ActiveRow = layout.Rows[0];

            return layout;
        }
        private static void ActivateFreeForm(IG.UltraGrid grid, IG.UltraGridBand gridBand)
        {
            
            gridBand.RowLayoutStyle = IG.RowLayoutStyle.ColumnLayout;
            gridBand.RowLayoutLabelPosition = Infragistics.Win.UltraWinGrid.LabelPosition.Left;
            gridBand.RowLayoutLabelStyle = Infragistics.Win.UltraWinGrid.RowLayoutLabelStyle.WithCellData;

            //gridband override
            gridBand.Override.BorderStyleRow = Infragistics.Win.UIElementBorderStyle.None;

            //grid layout
            grid.DisplayLayout.UseScrollWindow = IG.UseScrollWindow.Both;

            grid.DisplayLayout.MaxBandDepth = 1;
            grid.DisplayLayout.NewBandLoadStyle = IG.NewBandLoadStyle.Hide;
            grid.DisplayLayout.NewColumnLoadStyle = IG.NewColumnLoadStyle.Hide;
            grid.DisplayLayout.RowConnectorStyle = IG.RowConnectorStyle.None;
            grid.DisplayLayout.BorderStyle = Infragistics.Win.UIElementBorderStyle.None;
            //MAF: u freeform mame problemy s vertikalni  scrollbarou, proto vypnu scrolovani a zapnu rucni layoutovani (v evente OnLayout)
            //grid.DisplayLayout.ScrollStyle = Infragistics.Win.UltraWinGrid.ScrollStyle.Immediate;
            //grid.DisplayLayout.ScrollBounds = Infragistics.Win.UltraWinGrid.ScrollBounds.ScrollToFill;
            grid.DisplayLayout.Scrollbars = IG.Scrollbars.None; //Scrollbars.Automatic;

            //layout override
            grid.DisplayLayout.Override.CellPadding = 0;
            grid.DisplayLayout.Override.CellSpacing = 1;
            grid.DisplayLayout.Override.BorderStyleHeader = Infragistics.Win.UIElementBorderStyle.None;
            grid.DisplayLayout.Override.HeaderStyle = Infragistics.Win.HeaderStyle.XPThemed; //podpora pro hezke zobrazeni tlacitek na WinXP
            grid.DisplayLayout.Override.HeaderAppearance = new Infragistics.Win.Appearance() { };
            //grid.DisplayLayout.Override.BorderStyleCell = UIElementBorderStyle.Dotted;
            grid.DisplayLayout.Override.RowSelectors = Infragistics.Win.DefaultableBoolean.False;
            grid.DisplayLayout.Override.AllowColMoving = IG.AllowColMoving.NotAllowed;
            //grid.DisplayLayout.Override.AllowColSizing = AllowColSizing.None;
            grid.DisplayLayout.Override.AllowColSwapping = IG.AllowColSwapping.NotAllowed;
            grid.DisplayLayout.Override.AllowAddNew = IG.AllowAddNew.No;

            grid.DisplayLayout.Override.CellAppearance = new Infragistics.Win.Appearance() { };
            grid.DisplayLayout.Override.CellClickAction = Infragistics.Win.UltraWinGrid.CellClickAction.EditAndSelectText;   // Infragistics.Win.UltraWinGrid.CellClickAction.CellSelect 
            grid.DisplayLayout.Override.SelectedCellAppearance = new Infragistics.Win.Appearance() { };

            grid.DisplayLayout.Override.AllowMultiCellOperations = Infragistics.Win.UltraWinGrid.AllowMultiCellOperation.Copy; //dch 3.8.2006 - umozni kopirovat data z formulare
            // grid.DisplayLayout.Override.AllowMultiCellOperations = AllowMultiCellOperation.None;

            // JR 23891 - freeform nebude mit trideni na click na header
            grid.DisplayLayout.Override.HeaderClickAction = Infragistics.Win.UltraWinGrid.HeaderClickAction.Select;
        }
        #endregion
    }
}
