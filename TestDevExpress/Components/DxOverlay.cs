using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;

namespace TestDevExpress.Components
{
    #region DirectWindowGraphics
    /// <summary>
    /// Graphics pro kreslení nad formulář
    /// </summary>
    public class DirectWindowGraphics : WindowGraphics, IDisposable
    {
        #region Konstruktor instance
        /// <summary>
        /// Konstruktor. Používejme v using patternu. Používejme jen pro jedno kreslení.
        /// </summary>
        /// <param name="ownerControl">Typicky Form, může být i jiný control</param>
        /// <param name="coordinates">Souřadný systém a clip</param>
        public DirectWindowGraphics(Control ownerControl, CoordinateType coordinates = CoordinateType.ControlClient)
            : base(ownerControl, coordinates)
        { }
        #endregion
        /// <summary>
        /// Instance grafiky pro kreslení. Vytváří se až při prvním použití.
        /// </summary>
        public override Graphics Graphics
        {
            get
            {
                if (ScreenGraphics == null)
                {
                    CreateScreenGraphics();
                }
                return ScreenGraphics;
            }
        }
    }
    #endregion
    #region BufferedWindowGraphic
    /// <summary>
    /// Graphics pro kreslení nad formulář
    /// </summary>
    public class BufferedWindowGraphic : WindowGraphics
    {
        #region Konstruktor instance
        /// <summary>
        /// Konstruktor. Používejme v using patternu. Používejme jen pro jedno kreslení.
        /// </summary>
        /// <param name="ownerControl">Typicky Form, může být i jiný control</param>
        /// <param name="coordinates">Souřadný systém a clip</param>
        public BufferedWindowGraphic(Control ownerControl, CoordinateType coordinates = CoordinateType.ControlClient)
            : base(ownerControl, coordinates)
        { }
        #endregion
        #region Graphics a její tvorba
        /// <summary>
        /// Instance grafiky pro kreslení. Vytváří se až při prvním použití.
        /// </summary>
        public override Graphics Graphics
        {
            get
            {
                if (_GraphicsData == null)
                {
                    CreateScreenGraphics();
                    CreateBufferedGraphics();
                }
                return LayerGraphics;
            }
        }
        /// <summary>
        /// Vytvoří bufferovanou grafiku
        /// </summary>
        protected void CreateBufferedGraphics()
        {
            Size size = this.ScreenArea.Size;
            if (size.Width <= 0) size.Width = 1;
            if (size.Height <= 0) size.Height = 1;

            this._GraphicsContext = BufferedGraphicsManager.Current;
            this._GraphicsContext.MaximumBuffer = new Size(size.Width + 1, size.Height + 1);

            this._GraphicsData = this._GraphicsContext.Allocate(this.OwnerControl.CreateGraphics(), new Rectangle(new Point(0, 0), size));

            Point empty = Point.Empty;
            Rectangle controlBounds = this.OwnerControl.Bounds;
            Bitmap bmp = new Bitmap(controlBounds.Width, controlBounds.Height);
            Rectangle target = new Rectangle(empty, controlBounds.Size);
            this.OwnerControl.DrawToBitmap(bmp, target);

            Rectangle screenArea = this.ScreenArea;
            Rectangle sourceBounds = new Rectangle(screenArea.X - controlBounds.X, screenArea.Y - controlBounds.Y, screenArea.Width, screenArea.Height);
            Rectangle targetBounds = new Rectangle(empty, screenArea.Size);

            this._GraphicsData.Graphics.DrawImage(bmp, targetBounds, sourceBounds, GraphicsUnit.Pixel);
        }
        /// <summary>
        /// Vykreslí svůj obsah do dané cílové Graphics, typicky při kopírování obsahu mezi vrstvami, 
        /// a při kreslení Controlu (skládají se jednotlivé vrstvy).
        /// </summary>
        /// <param name="targetGraphics"></param>
        public void RenderTo(Graphics targetGraphics)
        {
            targetGraphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            this._GraphicsData.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            this._GraphicsData.Render(targetGraphics);
        }
        /// <summary>
        /// Zajistí vyrenderovní bufferované grafiky do Screenu
        /// </summary>
        protected override void ApplyScreenGraphics()
        {
            RenderTo(ScreenGraphics);
        }
        /// <summary>
        /// Uvolní svoje resources při Dispose
        /// </summary>
        protected override void DisposeGraphics()
        {
            if (this._GraphicsContext != null)
                TryRun(() => this._GraphicsContext.Dispose());
            this._GraphicsContext = null;

            if (this._GraphicsData != null)
                TryRun(() => this._GraphicsData.Dispose());
            this._GraphicsData = null;
        }
        /// <summary>
        /// Objekt Graphics, který dovoluje kreslit motivy do této vrstvy
        /// </summary>
        protected Graphics LayerGraphics { get { return this._GraphicsData.Graphics; } }
        /// <summary>
        /// Controll mechanism for buffered graphics
        /// </summary>
        private BufferedGraphicsContext _GraphicsContext;
        /// <summary>
        /// Content of graphic buffer
        /// </summary>
        private BufferedGraphics _GraphicsData;

        #endregion
    }
    #endregion
    #region WindowGraphics
    /// <summary>
    /// Graphics pro kreslení nad formulář
    /// </summary>
    public abstract class WindowGraphics : IDisposable
    {
        #region Static : verze Windows
        /// <summary>
        /// Static constructor that checks the windows version
        /// </summary>
        static WindowGraphics()
        {
            uint ver = GetVersion();
            if (ver < 0x80000000) // What I am doing?!! See the Windows SDK Documentation
                VersionIs9xMe = false;
            else
                VersionIs9xMe = true;
        }
        /// <summary>
        /// Obsahuje true pro Windows verze 95, 98, Milenium
        /// </summary>
        protected static readonly bool VersionIs9xMe;
        #endregion
        #region Konstruktor instance
        /// <summary>
        /// Konstruktor. Používejme v using patternu. Používejme jen pro jedno kreslení.
        /// </summary>
        /// <param name="ownerControl">Typicky Form, může být i jiný control</param>
        /// <param name="coordinates">Souřadný systém a clip</param>
        public WindowGraphics(Control ownerControl, CoordinateType coordinates = CoordinateType.ControlClient)
        {
            OwnerControl = ownerControl;
            Coordinates = coordinates;
        }
        /// <summary>
        /// Control který nás vlastní
        /// </summary>
        protected Control OwnerControl;
        /// <summary>
        /// Typ koordinátů
        /// </summary>
        protected CoordinateType Coordinates;
        #endregion
        #region Public prvek Graphics
        /// <summary>
        /// Instance grafiky pro kreslení. Vytváří se až při prvním použití.
        /// </summary>
        public abstract Graphics Graphics { get; }
        /// <summary>
        /// Zajistí vyrenderovní bufferované grafiky do Screenu
        /// </summary>
        protected virtual void ApplyScreenGraphics() { }
        /// <summary>
        /// Uvolní svoje resources při Dispose
        /// </summary>
        protected virtual void DisposeGraphics() { }
        #endregion
        #region Graphics a její tvorba a využití
        /// <summary>
		/// Creates the graphics. This functin is automatically called when
		/// you use the Graphics property for the first time.
		/// </summary>
		protected void CreateScreenGraphics()
        {
            if (ScreenGraphics != null)
                DestroyGraphics(false);

            _ScreenDC = GetDC(IntPtr.Zero);                                    // DeviceContext pro celou obrazovku (Screen)

            var controlHandle = OwnerControl.Handle;

            switch (Coordinates)
            {
                case CoordinateType.ControlClient:
                    _ControlRegion = GetVisibleRgn(controlHandle);             // VisibleRegion = co z controlu je viditelno
                    SelectClipRgn(_ScreenDC, _ControlRegion);                  // Clip na ClientBounds & VisibleRegion

                    Rectangle absClientBounds = ClientAbsoluteBounds;

                    // Specialita starších Windows:
                    if (VersionIs9xMe)
                        OffsetClipRgn(_ScreenDC, absClientBounds.X, absClientBounds.Y);

                    // Posunout souřadný systém:
                    SetWindowOrgEx(_ScreenDC, -absClientBounds.X, -absClientBounds.Y, IntPtr.Zero);

                    ScreenArea = absClientBounds;
                    ScreenGraphics = Graphics.FromHdc(_ScreenDC);
                    Rectangle clientClip = new Rectangle(0, 0, absClientBounds.Width, absClientBounds.Height);
                    ScreenGraphics.IntersectClip(clientClip);

                    break;

                case CoordinateType.ScreenControl:
                    _ControlRegion = GetVisibleRgn(controlHandle); // obtain visible clipping region for the window
                    SelectClipRgn(_ScreenDC, _ControlRegion); // clip the DC with the region

                    _ControlRect = new ControlRect();
                    GetWindowRect(controlHandle, _ControlRect);
                    ScreenArea = Rectangle.FromLTRB(_ControlRect.left, _ControlRect.top, _ControlRect.right, _ControlRect.bottom);

                    // Specialita starších Windows:
                    if (VersionIs9xMe)
                        OffsetClipRgn(_ScreenDC, _ControlRect.left, _ControlRect.top);

                    // Posunout souřadný systém:
                    SetWindowOrgEx(_ScreenDC, -_ControlRect.left, -_ControlRect.top, IntPtr.Zero);

                    ScreenGraphics = Graphics.FromHdc(_ScreenDC);
                    break;

                case CoordinateType.AllScreen:
                    ScreenGraphics = Graphics.FromHdc(_ScreenDC);
                    ScreenArea = Rectangle.Truncate(ScreenGraphics.ClipBounds);
                    break;

            }
        }
        /// <summary>
        /// Oblast Control.ClientRectangle v absolutních koordinátech
        /// </summary>
        protected Rectangle ClientAbsoluteBounds
        {
            get
            {
                var clientBounds = OwnerControl.ClientRectangle;
                var clientOrigin = OwnerControl.PointToScreen(Point.Empty);
                return new Rectangle(clientOrigin, clientBounds.Size);
            }
        }
        /// <summary>
        /// Gets the visible clipping region for the window.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <returns>Handle to the clipping region.</returns>
        protected IntPtr GetVisibleRgn(IntPtr hWnd)
        {
            IntPtr hrgn, hdc;
            hrgn = CreateRectRgn(0, 0, 0, 0);
            hdc = GetWindowDC(hWnd);
            int res = GetRandomRgn(hdc, hrgn, 4); // the value of SYSRGN is 4. Refer to Windows SDK Documentation.
            ReleaseDC(hWnd, hdc);
            return hrgn;
        }
        private IntPtr _ScreenDC;
        private IntPtr _ControlRegion;
        private ControlRect _ControlRect;
        /// <summary>
        /// Absolutní souřadnice aktivního výstupního prostoru
        /// </summary>
        protected Rectangle ScreenArea;
        /// <summary>
        /// Grafika typu Screen
        /// </summary>
        protected Graphics ScreenGraphics;
        #endregion
        #region Dispose
        /// <summary>
        /// Dispose objektu
        /// </summary>
        public void Dispose()
        {
            DestroyGraphics(true);
            OwnerControl = null;
        }
        /// <summary>
        /// Uvolnění zdrojů
        /// </summary>
        /// <param name="applyContent">Před Dispose grafiky <see cref="ScreenGraphics"/> zavolat metodu <see cref="ApplyScreenGraphics()"/> </param>
        protected virtual void DestroyGraphics(bool applyContent)
        {
            if (ScreenGraphics != null)
            {
                if (applyContent)
                    this.ApplyScreenGraphics();
                ScreenGraphics.Dispose();
                ScreenGraphics = null;
            }

            this.DisposeGraphics();

            if (_ScreenDC != IntPtr.Zero)
            {
                ReleaseDC(IntPtr.Zero, _ScreenDC);
                _ScreenDC = IntPtr.Zero;
            }

            if (_ControlRegion != IntPtr.Zero)
            {
                DeleteObject(_ControlRegion);
                _ControlRegion = IntPtr.Zero;
            }
        }
        #endregion
        #region Interní třídy a DllImports
        /// <summary>
        /// Metoda vyvolá danou akci v try-catch bloku, případnou chybu zapíše do trace a pokud je Debug režim, tak ji i ohlásí.
        /// </summary>
        /// <param name="action"></param>
        protected static void TryRun(Action action)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        class ControlRect
        {
            public int left, top, right, bottom;
        }
        [DllImport("user32")]
        private static extern int GetWindowRect(IntPtr hwnd, [Out] ControlRect lpRect);
        [DllImport("user32")]
        private static extern IntPtr GetWindowDC(IntPtr hwnd);
        [DllImport("user32")]
        private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32")]
        private static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [DllImport("gdi32")]
        private static extern int GetRandomRgn(IntPtr hdc, IntPtr hrgn, int iNum);
        [DllImport("gdi32")]
        private static extern IntPtr CreateRectRgn(int X1, int Y1, int X2, int Y2);
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr hObject);
        [DllImport("gdi32")]
        private static extern int SelectClipRgn(IntPtr hdc, IntPtr hRgn);
        [DllImport("kernel32")]
        private static extern uint GetVersion();
        [DllImport("gdi32")]
        private static extern int OffsetClipRgn(IntPtr hdc, int x, int y);
        /// <summary>
        /// Nastaví souřadnice bodu 0/0
        /// </summary>
        /// <param name="hdc"></param>
        /// <param name="nX"></param>
        /// <param name="nY"></param>
        /// <param name="lpPoint"></param>
        /// <returns></returns>
        [DllImport("gdi32")]
        protected static extern int SetWindowOrgEx(IntPtr hdc, int nX, int nY, IntPtr lpPoint);
        #endregion
    }
    /// <summary>
    /// Souřadný systém grafiky
    /// </summary>
    public enum CoordinateType
    {
        /// <summary>
        /// Vnitřní klientský prostor controlu, na souřadnici 0/0 se nachází první pixel jeho Child controlů
        /// </summary>
        ControlClient,
        /// <summary>
        /// Souřadnice klientské = Screen, oříznuté na plný prostor controlu (u formuláře obsahuje i stíny vedle formuláře)
        /// </summary>
        ScreenControl,
        /// <summary>
        /// Souřadnice absolutní = Screen, neoříznuté
        /// </summary>
        AllScreen
    }
    #endregion
}
