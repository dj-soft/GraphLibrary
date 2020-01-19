using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Asol.Tools.WorkScheduler.Components;

namespace Asol.Tools.WorkScheduler.GameComponents
{
    /// <summary>
    /// GameControl
    /// </summary>
    public class GameControl : GControlLayered
    {
        #region Inicializace
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GameControl()
        {
            this.Init();
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        protected void Init()
        {
            _CameraInit();
            _DrawInit();
        }
        #endregion
        #region Camera
        private void _CameraInit()
        {
            this.Camera = new GameCamera(this);
        }
        /// <summary>
        /// Kamera, která vše kreslí
        /// </summary>
        public GameCamera Camera { get; private set; }
        #endregion
        #region Draw
        /// <summary>
        /// Inicializuje subsystém Draw
        /// </summary>
        private void _DrawInit()
        {
            this.PrepareLayers("Standard", "Interactive", "Dynamic", "Overlay");
            this._Items = new List<GameItem>[] { new List<GameItem>(), new List<GameItem>(), new List<GameItem>() };
        }
        /// <summary>
        /// Zajistí kreslení controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintLayers(LayeredPaintEventArgs e)
        {
            bool drawAll = true;
            try
            {
                for (int layer = 0; layer < this._Items.Length; layer++)
                {
                    var items = this._Items[layer];
                    var drawItems = (drawAll ? items.ToArray() : items.Where(i => i.NeedDraw).ToArray());
                    bool drawBase = (layer == 0 && drawAll);
                    if (drawBase || drawItems.Length > 0)
                    {
                        if (drawBase)
                            base.OnPaintLayers(e);

                        Graphics graphics = e.GetGraphicsForLayer(layer, true);
                        using (GPainter.GraphicsUseSmooth(graphics))
                        {
                            foreach (var drawItem in drawItems)
                                this.Camera.DrawItem(graphics, drawItem);
                        }
                    }
                }
            }
            catch { }
        }
        private List<GameItem>[] _Items;
        #endregion
        public List<GameItem> GameItems { get { return this._Items[0]; } }
    }
}
