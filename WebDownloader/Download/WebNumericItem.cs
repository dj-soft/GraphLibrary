using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;

namespace Djs.Tools.WebDownloader.Download
{
    #region clas WebNumericItem
    /// <summary>
    /// Položka vzorce typu Numerický rozsah OD-DO
    /// </summary>
    public class WebNumericItem : WebItem
    {
        #region Konstrukce a data
        public WebNumericItem()
            : base()
        {
            this.Step = 1L;
        }
        public long RangeFrom { get; set; }
        public long RangeTo { get; set; }
        public int TextLength { get; set; }
        public long Step { get; set; }
        public long Value { get; set; }
        public override WebItemType Type { get { return WebItemType.NumericRange; } }
        public override bool Correct
        {
            get
            {
                return (this.RangeTo > this.RangeFrom && this.Step > 0L);
            }
        }
        public override string Text
        {
            get
            {
                long value = this.Value;
                value = (value > this.RangeTo ? this.RangeTo : (value < this.RangeFrom ? this.RangeFrom : value));
                string text = value.ToString();
                if (this.TextLength > 0 && this.TextLength < 100 && this.TextLength > text.Length)
                    text = text.PadLeft(this.TextLength, '0');
                return text;
            }
        }
        public override bool Increment()
        {
            bool result = false;
            long value = this.Value + this.Step;
            if (value < this.RangeFrom)
                value = this.RangeFrom;
            if (value > this.RangeTo)
            {
                value = this.RangeFrom;
                result = this.CarryOutIncrement;
            }
            this.Value = value;
            return result;
        }
        public override WebItem Clone
        {
            get
            {
                WebNumericItem clone = (WebNumericItem)this.MemberwiseClone();
                return clone;
            }
        }
        #endregion
        #region Parsování vzorku
        public static WebNumericItem Parse(string sample)
        {
            return Parse(sample, "{{x}}");
        }
        public static WebNumericItem Parse(string sample, string key)
        {
            WebNumericItem item = new WebNumericItem();
            if (String.IsNullOrEmpty(sample))
                return item;

            item.Key = key;
            item.RangeFrom = 0L;
            item.Step = 1L;

            long value;
            Int64.TryParse(sample, out value);
            item.Value = value;

            int length = sample.Length;
            item.TextLength = length;

            if (length > 12) length = 12;
            string rangeTo = "".PadLeft(length, '9');
            Int64.TryParse(rangeTo, out value);
            item.RangeTo = value;

            return item;
        }
        #endregion
        #region Load & Save
        protected override void OnSave(List<KeyValuePair<string, string>> list)
        {
            list.Add(new KeyValuePair<string, string>("Value", this.Value.ToString()));
            list.Add(new KeyValuePair<string, string>("Length", this.TextLength.ToString()));
            list.Add(new KeyValuePair<string, string>("From", this.RangeFrom.ToString()));
            list.Add(new KeyValuePair<string, string>("To", this.RangeTo.ToString()));
            list.Add(new KeyValuePair<string, string>("Step", this.Step.ToString()));
        }
        protected override void OnLoad(List<KeyValuePair<string, string>> list)
        {
            this.Value = GetValue(list, "Value", 0L);
            this.TextLength = GetValue(list, "Length", 0);
            this.RangeFrom = GetValue(list, "From", 0L);
            this.RangeTo = GetValue(list, "To", 0L);
            this.Step = GetValue(list, "Step", 0L);
        }
        #endregion
    }
    #endregion
    #region UI
    public class WebNumericPanel : WebItemPanel
    {
        #region Konstrukce
        public WebNumericPanel()
        {
            this.Init();
        }
        public WebNumericPanel(Rectangle bounds, ref int tabIndex)
        {
            this.Init();
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TabIndex = tabIndex++;
        }
        protected void Init()
        {
            this.SuspendLayout();

            int tabIndex = 0;
            int y = 3;
            int x = 205;
            int w = x - 9;
            this.InitCommon(x, ref y, ref tabIndex);
            
            this._ValueLbl = new WebLabel("Aktuální hodnota, délka:", new Rectangle(7, y + 4, w, 16), ref tabIndex);
            this._ValueTxt = new WebNumeric(0, 999999999999L, new Rectangle(x, y, 120, 24), ref tabIndex);
            this._LengthTxt = new WebNumeric(0, 999, new Rectangle(x + 126, y, 78, 24), ref tabIndex);
            y += this._LengthTxt.Height + 1;
            this._RangeLbl = new WebLabel("Rozmezí číslic od-do (včetně):", new Rectangle(7, y + 4, w, 16), ref tabIndex);
            this._RangeFromTxt = new WebNumeric(0, 999999999999L, new Rectangle(x, y, 120, 24), ref tabIndex);
            this._RangeToTxt = new WebNumeric(0, 999999999999L, new Rectangle(x + 126, y, 120, 24), ref tabIndex);
            y += this._RangeToTxt.Height + 1;
            this._StepLbl = new WebLabel("Přírůstek na jeden cyklus:", new Rectangle(7, y + 4, w, 16), ref tabIndex);
            this._StepTxt = new WebNumeric(0, 999999999999L, new Rectangle(x, y, 120, 24), ref tabIndex);
            y += this._StepTxt.Height + 1;

            // this.MinimumSize = new Size(520, y+6);
            this.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            // this.BackColor = Color.Wheat;

            ((System.ComponentModel.ISupportInitialize)(this._ValueTxt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._LengthTxt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._RangeFromTxt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._RangeToTxt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._StepTxt)).BeginInit();

            this.Controls.Add(this._ValueLbl);
            this.Controls.Add(this._ValueTxt);
            this.Controls.Add(this._LengthTxt);
            this.Controls.Add(this._RangeLbl);
            this.Controls.Add(this._RangeFromTxt);
            this.Controls.Add(this._RangeToTxt);
            this.Controls.Add(this._StepLbl);
            this.Controls.Add(this._StepTxt);

            this._ValueTxt.ValueChanged += new EventHandler(ValueChanged);
            this._LengthTxt.ValueChanged += new EventHandler(ValueChanged);
            this._RangeFromTxt.ValueChanged += new EventHandler(ValueChanged);
            this._RangeToTxt.ValueChanged += new EventHandler(ValueChanged);
            this._StepTxt.ValueChanged += new EventHandler(ValueChanged);
            
            ((System.ComponentModel.ISupportInitialize)(this._ValueTxt)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._LengthTxt)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._RangeFromTxt)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._RangeToTxt)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._StepTxt)).EndInit();

            this.ResumeLayout(false);
        }
        protected WebLabel _ValueLbl;
        protected WebNumeric _ValueTxt;
        protected WebNumeric _LengthTxt;
        protected WebLabel _RangeLbl;
        protected WebNumeric _RangeFromTxt;
        protected WebNumeric _RangeToTxt;
        protected WebLabel _StepLbl;
        protected WebNumeric _StepTxt;
        #endregion
        #region Data
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public WebNumericItem WebNumericItem { get { return this.WebItem as WebNumericItem; } set { this.WebItem = value; } }
        protected override void OnDataShow()
        {
            WebNumericItem data = this.WebNumericItem;
            if (data == null) return;

            StoreValuesToControl(data.Value, data.RangeFrom, data.RangeTo);
            this._LengthTxt.Value = data.TextLength;
            this._StepTxt.Value = data.Step;

            this._DataNowChanging = false;
        }
        protected override void OnDataCollect()
        {
            // Validace na vizuální úrovni:
            StoreValuesToControl((long)this._ValueTxt.Value, (long)this._RangeFromTxt.Value, (long)this._RangeToTxt.Value);

            WebNumericItem data = this.WebNumericItem;
            if (data == null) return;

            data.Value = (long)this._ValueTxt.Value;
            data.TextLength = (int)this._LengthTxt.Value;
            data.RangeFrom = (long)this._RangeFromTxt.Value;
            data.RangeTo = (long)this._RangeToTxt.Value;
            data.Step = (long)this._StepTxt.Value;
        }
        /// <summary>
        /// Zadané hodnoty vloží do UI controlů ve správném pořadí
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        protected void StoreValuesToControl(long value, long min, long max)
        {
            if (max < min) max = min;
            value = (value < min ? min : (value > max ? max : value));

            if (this._RangeFromTxt.Value != min) this._RangeFromTxt.Value = min;
            if (this._RangeToTxt.Value != max) this._RangeToTxt.Value = max;
            this._ValueTxt.SetValues(value, min, max);
        }
        #endregion
    }
    #endregion
}
