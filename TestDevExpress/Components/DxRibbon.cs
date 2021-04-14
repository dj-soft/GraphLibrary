// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;

using DevExpress.Utils;


namespace Noris.Clients.Win.Components.AsolDX
{
    #region DxRibbon
    /// <summary>
    /// Potomek Ribbonu
    /// </summary>
    public class DxRibbonControl : DevExpress.XtraBars.Ribbon.RibbonControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxRibbonControl()
        {
        }
        /// <summary>
        /// Vykreslení controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.PaintAfter(e);
        }
        #region Souřadnice oblasti Ribbonu kde jsou aktuálně buttony
        /// <summary>
        /// Souřadnice oblasti Ribbonu, kde jsou aktuálně buttony
        /// </summary>
        public Rectangle ButtonsBounds
        {
            get { return GetInnerBounds(50); }
        }
        /// <summary>
        /// Určí a vrátí prostor, v němž se reálně nacházejí buttony a další prvky uvnitř Ribbonu.
        /// Řeší tedy aktuální skin, vzhled, umístění Ribbonu (on někdy zastává funkci Titlebar okna) atd.
        /// </summary>
        /// <param name="itemCount"></param>
        /// <returns></returns>
        private Rectangle GetInnerBounds(int itemCount = 50)
        {
            if (itemCount < 5) itemCount = 5;
            int c = 0;
            int l = 0;
            int t = 0;
            int r = 0;
            int b = 0;
            foreach (DevExpress.XtraBars.Ribbon.RibbonPage page in this.Pages)
            {
                foreach (DevExpress.XtraBars.Ribbon.RibbonPageGroup group in page.Groups)
                {
                    foreach (var itemLink in group.ItemLinks)
                    {
                        if (itemLink is DevExpress.XtraBars.BarButtonItemLink link)
                        {
                            var bounds = link.Bounds;             // Bounds = relativně v Ribbonu, ScreenBounds = absolutně v monitoru
                            if (bounds.Left > 0 && bounds.Top > 0 && bounds.Width > 0 && bounds.Height > 0)
                            {
                                if (c == 0)
                                {
                                    l = bounds.Left;
                                    t = bounds.Top;
                                    r = bounds.Right;
                                    b = bounds.Bottom;
                                }
                                else
                                {
                                    if (bounds.Left < l) l = bounds.Left;
                                    if (bounds.Top < t) t = bounds.Top;
                                    if (bounds.Right > r) r = bounds.Right;
                                    if (bounds.Bottom > b) b = bounds.Bottom;
                                }
                                c++;

                                if (c > itemCount) break;
                            }
                        }
                    }
                    if (c > itemCount) break;
                }
                if (c > itemCount) break;
            }

            Rectangle clientBounds = this.ClientRectangle;
            int cr = clientBounds.Right - 6;
            if (r < cr) r = cr;
            return Rectangle.FromLTRB(l, t, r, b);
        }
        #endregion
        #region Ikonka vpravo
        /// <summary>
        /// Ikona vpravo pro velký Ribbon
        /// </summary>
        public Image ImageRightFull { get { return _ImageRightFull; } set { _ImageRightFull = value; this.Refresh(); } }
        private Image _ImageRightFull;
        /// <summary>
        /// Ikona vpravo pro malý Ribbon
        /// </summary>
        public Image ImageRightMini { get { return _ImageRightMini; } set { _ImageRightMini = value; this.Refresh(); } }
        private Image _ImageRightMini;
        /// <summary>
        /// Vykreslí ikonu vpravo
        /// </summary>
        /// <param name="e"></param>
        private void PaintAfter(PaintEventArgs e)
        {
            OnPaintImageRightBefore(e);

            bool isSmallRibbon = (this.CommandLayout == DevExpress.XtraBars.Ribbon.CommandLayout.Simplified);
            Image image = GetImageRight(isSmallRibbon);
            if (image == null) return;
            Size imageNativeSize = image.Size;
            if (imageNativeSize.Width <= 0 || imageNativeSize.Height <= 0) return;

            Rectangle buttonsBounds = ButtonsBounds;
            int imageHeight = (isSmallRibbon ? 24 : 48);
            float ratio = (float)imageNativeSize.Width / (float)imageNativeSize.Height;
            int imageWidth = (int)(ratio * (float)imageHeight);

            Rectangle imageBounds = new Rectangle(buttonsBounds.Right - 6 - imageWidth, buttonsBounds.Y + 4, imageWidth, imageHeight);
            e.Graphics.DrawImage(image, imageBounds);

            OnPaintImageRightAfter(e);
        }
        /// <summary>
        /// Metoda vrátí vhodný obrázek pro obrázek vpravo pro aktuální velikost. 
        /// Může vrátit null.
        /// </summary>
        /// <param name="isSmallRibbon"></param>
        /// <returns></returns>
        private Image GetImageRight(bool isSmallRibbon)
        {
            if (!isSmallRibbon && _ImageRightFull != null) return _ImageRightFull;
            if (isSmallRibbon && _ImageRightMini != null) return _ImageRightMini;
            if (_ImageRightFull != null) return _ImageRightFull;
            return _ImageRightMini;
        }
        /// <summary>
        /// Provede se před vykreslením obrázku vpravo v ribbonu
        /// </summary>
        protected virtual void OnPaintImageRightBefore(PaintEventArgs e)
        {
            PaintImageRightBefore?.Invoke(this, e);
        }
        /// <summary>
        /// Volá se před vykreslením obrázku vpravo v ribbonu
        /// </summary>
        public event EventHandler<PaintEventArgs> PaintImageRightBefore;
        /// <summary>
        /// Provede se po vykreslení obrázku vpravo v ribbonu
        /// </summary>
        protected virtual void OnPaintImageRightAfter(PaintEventArgs e)
        {
            PaintImageRightAfter?.Invoke(this, e);
        }
        /// <summary>
        /// Volá se po vykreslení obrázku vpravo v ribbonu
        /// </summary>
        public event EventHandler<PaintEventArgs> PaintImageRightAfter;
        #endregion
    }
    #endregion
}
