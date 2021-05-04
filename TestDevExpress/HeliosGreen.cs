using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using WinFormServices.Drawing;

#region Noris classes and support
namespace Noris.Clients.Controllers
{
    /// <summary>
    /// Simulace Green
    /// </summary>
    public class DataFormDataSourceFacade
    {
        /// <summary>
        /// Simulace Green
        /// </summary>
        public class TabsFacade
        {
            /// <summary>
            /// Simulace Green
            /// </summary>
            public class LabeledTabInfo
            {
                public string Name;
                public string Label;
            }
        }
    }
}

namespace Noris.Clients.Common
{
    public class SupportScaling
    {
        public static int GetScaledValue(int designValue) { return designValue; }
    }
}

namespace Noris.Clients.Win.Components
{
    public interface IEscapeHandler
    {
        bool HandleEscapeKey();
    }
}


namespace Noris.Clients.Win.Components
{
    /// <summary>
    /// Simulace Green
    /// </summary>
    internal interface IInfragisticsDevExpressSkinableSupport
    {
        void DevexpressSkinChanged(DevExpressToInfragisticsAppearanceConverter.StyleChangedEventArgs arg);
    }
    /// <summary>
    /// Rozhraní pro objekt, který si sám dokáže disposovat svoje controly.
    /// Metoda <see cref="IDisposableContainer.DisposeControls()"/> se pak vyvolá 
    /// v procesu <see cref="Globals.DisposeControls(Control, bool, Control, ContainerControl, bool)"/>
    /// namísto toho, aby se volala metoda <see cref="IDisposable.Dispose()"/> jednotlivých controlů.
    /// </summary>
    public interface IDisposableContainer
    {
        /// <summary>
        /// Objekt si má sám disposovat svoje controly a zahodit je z pole <see cref="Control.Controls"/>.
        /// Objekt ale nemá provádět Dispose sám sebe, od toho je metoda <see cref="IDisposable.Dispose()"/>
        /// </summary>
        void DisposeControls();
    }
    /// <summary>
    /// Simulace Green
    /// </summary>
    internal sealed class DevExpressToInfragisticsAppearanceConverter
    {
        public sealed class StyleChangedEventArgs { }
    }

    /// <summary>
    /// Simulace Green
    /// </summary>
    public class ComponentConnector
    {
        /// <summary>
        /// Simulace Green
        /// </summary>
        public static GraphicsCache GraphicsCache { get { if (_GraphicsCache is null) _GraphicsCache = new GraphicsCache(); return _GraphicsCache; } }
        private static GraphicsCache _GraphicsCache;
        /// <summary>
        /// Zobrazí varování
        /// </summary>
        /// <param name="message"></param>
        public static void ShowWarningToDeveloper(string message)
        {
            if (!System.Diagnostics.Debugger.IsAttached) return;
            MessageBox.Show(Host.Owner, message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
        }
        /// <summary>
        /// Simulace Green
        /// </summary>
        public static AppHost Host { get { if (_Host == null) _Host = new AppHost(); return _Host; } }
        private static AppHost _Host;
    }
    public class AppHost
    {
        public bool InvokeRequired { get { return (AnyControl?.InvokeRequired ?? false); } }
        public void Invoke(Delegate method)
        {
            var control = AnyControl;
            if (control != null)
            {
                if (control.InvokeRequired)
                    control.Invoke(method);
                else
                    method.DynamicInvoke();
            }
        }
        public void Invoke(Delegate method, params object[] args)
        {
            var control = AnyControl;
            if (control != null)
            {
                if (control.InvokeRequired)
                    control.Invoke(method);
                else
                    method.DynamicInvoke(args);
            }
        }
        public Control Owner { get { return AnyControl; } }
        protected Control AnyControl { get { return Form.ActiveForm; } }
        public event EventHandler InteractiveZoomChanged;
    }
    /// <summary>
    /// Simulace Green
    /// </summary>
    public class GraphicsCache
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GraphicsCache()
        {
            _ImageList = new ImageList();
        }
        /// <summary>
        /// Zoom
        /// </summary>
        public decimal CurrentZoom { get { return 1m; } }
        /// <summary>
        /// Simulace Green
        /// </summary>
        /// <param name="sizeOrDefault"></param>
        /// <returns></returns>
        public ImageList GetImageList(UserGraphicsSize? sizeOrDefault = null)
        {
            return _ImageList;
        }
        private ImageList _ImageList;
        /// <summary>
        /// Simulace Green
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="size"></param>
        /// <param name="caption"></param>
        public Image GetResourceContent(string imageName, UserGraphicsSize size, string caption = null)
        {
            if (String.IsNullOrEmpty(imageName)) return null;
            if (!_ImageList.Images.ContainsKey(imageName))
            {
                var image = TestDevExpress.Properties.Resources.ResourceManager.GetObject(imageName) as Image;
                if (!(image is null))
                    _ImageList.Images.Add(imageName, image);
            }
            if (!_ImageList.Images.ContainsKey(imageName)) return null;
            return _ImageList.Images[imageName];
        }
        /// <summary>
        /// Simulace Green
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="size"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public int GetResourceIndex(string imageName, UserGraphicsSize size, string caption = null)
        {
            Image image = GetResourceContent(imageName, size, caption);
            if (image is null) return -1;
            return _ImageList.Images.IndexOfKey(imageName);
        }
    }
    /// <summary>
    /// Simulace Green
    /// </summary>
    public class UiSynchronizationHelper
    {
        /// <summary> Executes the event on a different thread (UI marshaling), and waits for the result. </summary>
        /// <exception cref="Exception"> Thrown when an exception error condition occurs. </exception>
        /// <typeparam name="TEventArgs"> Type of the event arguments. </typeparam>
        /// <param name="sender">              Source of the event. </param>
        /// <param name="eventArgs">           T event information. </param>
        /// <param name="invokeHandler">       The invoke handler. </param>
        /// <param name="beforeInvokeHandler"> (Optional) the before invoke handler. </param>
        public static void InvokeEvent<TEventArgs>(object sender, TEventArgs eventArgs, EventHandler<TEventArgs> invokeHandler, Action beforeInvokeHandler = null)
            where TEventArgs : EventArgs
        {
            if (ComponentConnector.Host.InvokeRequired)
            {// marshaling invoke
                if (beforeInvokeHandler != null) beforeInvokeHandler();
                ComponentConnector.Host.Invoke(new Action<object, TEventArgs>((s, e) =>
                {
                    try
                    {
                        invokeHandler(s, e); //Invoke handler method
                    }
                    catch (Exception ex)
                    {// store inner call stack to exception Data for future processing
                        _propagateExeption(ex);
                        throw;
                    }
                }), new[] { sender, eventArgs });
            }
            else
            {// direct call
                invokeHandler(sender, eventArgs); // invoke handler method
            }
        }
        /// <summary>
        /// Invoke
        /// </summary>
        /// <typeparam name="TSender"></typeparam>
        /// <typeparam name="TArgument"></typeparam>
        /// <param name="sender"></param>
        /// <param name="argument"></param>
        /// <param name="invokeHandler"></param>
        /// <param name="beforeInvokeHandler"></param>
        public static void Invoke<TSender, TArgument>(TSender sender, TArgument argument, Action<TSender, TArgument> invokeHandler, Action beforeInvokeHandler = null)
        {
            if (ComponentConnector.Host.InvokeRequired)
            {// marshaling invoke
                if (beforeInvokeHandler != null) beforeInvokeHandler();
                ComponentConnector.Host.Invoke(new Action<TSender, TArgument>((s, a) =>
                {
                    try
                    {
                        invokeHandler(s, a); //Invoke handler method
                    }
                    catch (Exception ex)
                    {// store inner call stack to exception Data for future processing
                        _propagateExeption(ex);
                        throw;
                    }
                }), new object[] { sender, argument });
            }
            else
            {// direct call
                invokeHandler(sender, argument); // invoke handler method
            }
        }
        private static void _propagateExeption(Exception ex)
        {
            if (ex.Data.Contains(InvokeExceptionDataKey) == false)
            {
                ex.Data.Add(InvokeExceptionDataKey, ex.StackTrace);
            }
            else
            {
                ex.Data[InvokeExceptionDataKey] = ex.Data[InvokeExceptionDataKey] + ex.StackTrace;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public const string InvokeExceptionDataKey = "InnerInvokeStack";
    }
}
namespace WinFormServices
{
    /// <summary>
    /// Simulace Green
    /// </summary>
    public class KeyboardHelper
    {
        /// <summary>
        /// Simulace Green
        /// </summary>
        /// <param name="hotKey"></param>
        /// <returns></returns>
        public static DevExpress.XtraBars.BarShortcut GetShortcutFromServerHotKey(string hotKey) { return new DevExpress.XtraBars.BarShortcut(Keys.None); }

    }
}
namespace WinFormServices.Drawing
{
    /// <summary>
    /// Simulace Green
    /// </summary>
    public enum UserGraphicsSize 
    {
        /// <summary>None</summary>
        None,
        /// <summary>Small</summary>
        Small,
        /// <summary>Medium</summary>
        Medium,
        /// <summary>Large</summary>
        Large
    }
}

#endregion
