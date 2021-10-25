using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noris.Clients.Win.Components.AsolDX
{
#if CompileTestDevExpressQ
    /// <summary>
    /// Adapter na systém TestDevExpress
    /// </summary>
    internal class CurrentAdapter : ISystemAdapter
    {
        event EventHandler ISystemAdapter.InteractiveZoomChanged { add { } remove { } }
        decimal ISystemAdapter.ZoomRatio { get { return 1.0m; } }
        string ISystemAdapter.GetMessage(string messageCode, params object[] parameters) { return null; }
        System.Windows.Forms.ImageList ISystemAdapter.GetResourceImageList(ResourceImageSizeType sizeType) { return null; }
        System.Drawing.Image ISystemAdapter.GetResourceImage(string resourceName, ResourceImageSizeType sizeType, string caption) { return null; }
        int ISystemAdapter.GetResourceIndex(string iconName, ResourceImageSizeType sizeType, string caption) { return -1; }
        System.Windows.Forms.Keys ISystemAdapter.GetShortcutKeys(string shortCut) { return System.Windows.Forms.Keys.None; }
    }

    /// <summary>
    /// Rozhraní předepisuje metodu <see cref="HandleEscapeKey()"/>, která umožní řešit klávesu Escape v rámci systému
    /// </summary>
    public interface IEscapeKeyHandler
    {
        /// <summary>
        /// Systém zaregistroval klávesu Escape; a ptá se otevřených oken, které ji chce vyřešit...
        /// </summary>
        /// <returns></returns>
        bool HandleEscapeKey();
    }


#else

    /// <summary>
    /// Adapter na systém TestDevExpress
    /// </summary>
    internal class CurrentAdapter : ISystemAdapter
    {
        event EventHandler ISystemAdapter.InteractiveZoomChanged { add { ComponentConnector.Host.InteractiveZoomChanged += value; } remove { ComponentConnector.Host.InteractiveZoomChanged -= value; } }
        decimal ISystemAdapter.ZoomRatio { get { return ((decimal)Common.SupportScaling.GetScaledValue(100000)) / 100000m; } }
        string ISystemAdapter.GetMessage(string messageCode, params object[] parameters) { return ASOL.Framework.Shared.Localization.Message.GetMessage(messageCode, parameters); }
        System.Drawing.Image ISystemAdapter.GetResourceImage(string resourceName, ResourceImageSizeType sizeType, string caption) { return ComponentConnector.GraphicsCache.GetResourceContent(resourceName, ConvertTo(sizeType), caption); }
        System.Windows.Forms.ImageList ISystemAdapter.GetResourceImageList(ResourceImageSizeType sizeType) { return ComponentConnector.GraphicsCache.GetImageList(ConvertTo(sizeType)); }
        int ISystemAdapter.GetResourceIndex(string iconName, ResourceImageSizeType sizeType, string caption) { return ComponentConnector.GraphicsCache.GetResourceIndex(iconName, ConvertTo(sizeType), caption); }
        System.Windows.Forms.Keys ISystemAdapter.GetShortcutKeys(string shortCut) { return WinFormServices.KeyboardHelper.GetShortcutFromServerHotKey(shortCut); }

        private static WinFormServices.Drawing.UserGraphicsSize ConvertTo(ResourceImageSizeType sizeType)
        {
            return (sizeType == ResourceImageSizeType.None ? WinFormServices.Drawing.UserGraphicsSize.None :
                   (sizeType == ResourceImageSizeType.Small ? WinFormServices.Drawing.UserGraphicsSize.Small :
                   (sizeType == ResourceImageSizeType.Medium ? WinFormServices.Drawing.UserGraphicsSize.Medium :
                   (sizeType == ResourceImageSizeType.Large ? WinFormServices.Drawing.UserGraphicsSize.Large : WinFormServices.Drawing.UserGraphicsSize.None))));
        }
    }
    /// <summary>
    /// Rozhraní předepisuje metodu <see cref="HandleEscapeKey()"/>, která umožní řešit klávesu Escape v rámci systému
    /// </summary>
    public interface IEscapeKeyHandler : Noris.Clients.Win.Components.IEscapeHandler
    {
        /// <summary>
        /// Systém zaregistroval klávesu Escape; a ptá se otevřených oken, které ji chce vyřešit...
        /// </summary>
        /// <returns></returns>
        new bool HandleEscapeKey();
    }

#endif


    internal interface ISystemAdapter 
    {
        /// <summary>
        /// Událost, kdy systém změní Zoom
        /// </summary>
        event EventHandler InteractiveZoomChanged;
        /// <summary>
        /// Aktuálně platný Zoom, kde 1.0 = 100%; 1.25 = 125% atd
        /// </summary>
        decimal ZoomRatio { get; }
        /// <summary>
        /// Lokalizace daného stringu a parametrů
        /// </summary>
        /// <param name="messageCode"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        string GetMessage(string messageCode, params object[] parameters);
        /// <summary>
        /// Vrátí ImageList pro danou velikost
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        System.Windows.Forms.ImageList GetResourceImageList(ResourceImageSizeType sizeType);
        /// <summary>
        /// Vrátí Image pro daný název a velikost. Výstupem má být bitmapa.
        /// Pokud daná ikona neexistuje, a je dán parametr <paramref name="caption"/>, pak ikonu vygeneruje (z počátečních dvou písmen).
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="sizeType"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        System.Drawing.Image GetResourceImage(string resourceName, ResourceImageSizeType sizeType, string caption);
        /// <summary>
        /// Vrátí index ikony v dané velikosti.
        /// Pokud daná ikona neexistuje, a je dán parametr <paramref name="caption"/>, pak ikonu vygeneruje (z počátečních dvou písmen).
        /// </summary>
        /// <param name="iconName"></param>
        /// <param name="sizeType"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        int GetResourceIndex(string iconName, ResourceImageSizeType sizeType, string caption);
        /// <summary>
        /// Vrací klávesovou zkratku pro daný string, typicky na vstupu je "Ctrl+C", na výstupu je <see cref="System.Windows.Forms.Keys.Control"/> | <see cref="System.Windows.Forms.Keys.C"/>
        /// </summary>
        /// <param name="shortCut"></param>
        /// <returns></returns>
        System.Windows.Forms.Keys GetShortcutKeys(string shortCut);
    }
    /// <summary>
    /// Tato třída reprezentuje adapter na systémové metody.
    /// Prvky této třídy jsou volány z různých míst komponent AsolDX a jejich úkolem je převolat odpovídající metody konkrétního systému.
    /// K tomu účelu si zdejší třída vytvoří interní instanci konkrétního systému (zde NephriteAdapter).
    /// <para/>
    /// Komponenty volají statické metody <see cref="SystemAdapter"/>, ty uvnitř převolají odpovídající svoje instanční abstratní metody, 
    /// které jsou implementované v konkrétním potomku adapteru.
    /// </summary>
    internal abstract class SystemAdapter
    {
        #region Zásuvný modul pro konkrétní systém
        /// <summary>
        /// Aktuální adapter
        /// </summary>
        protected static ISystemAdapter Current
        {
            get
            {
                if (__Current == null)
                {
                    lock (__Lock)
                    {
                        if (__Current == null)
                            __Current = __CreateAdapter();
                    }
                }
                return __Current;
            }
        }
        /// <summary>
        /// Tato metoda vygeneruje a vrátí konkrétní adapter
        /// </summary>
        /// <returns></returns>
        private static ISystemAdapter __CreateAdapter() { return new CurrentAdapter(); }
        private static ISystemAdapter __Current;
        private static object __Lock = new object();
        #endregion
        #region Služby pro komponenty, předpis abstraktních metod pro konkrétního potomka
        /// <summary>
        /// Událost, kdy systém změní Zoom
        /// </summary>
        public static event EventHandler InteractiveZoomChanged { add { Current.InteractiveZoomChanged += value; } remove { Current.InteractiveZoomChanged -= value; } }
        /// <summary>
        /// Aktuálně platný Zoom, kde 1.0 = 100%; 1.25 = 125% atd
        /// </summary>
        public static decimal ZoomRatio { get { return Current.ZoomRatio; } }
        /// <summary>
        /// Lokalizace daného stringu a parametrů
        /// </summary>
        /// <param name="messageCode"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string GetMessage(string messageCode, params object[] parameters) { return Current.GetMessage(messageCode, parameters); }
        /// <summary>
        /// Vrátí ImageList pro danou velikost
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static System.Windows.Forms.ImageList GetResourceImageList(ResourceImageSizeType sizeType) { return Current.GetResourceImageList(sizeType); }
        /// <summary>
        /// Vrátí Image pro daný název a velikost. Výstupem má být bitmapa.
        /// Pokud daná ikona neexistuje, a je dán parametr <paramref name="caption"/>, pak ikonu vygeneruje (z počátečních dvou písmen).
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="sizeType"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static System.Drawing.Image GetResourceImage(string resourceName, ResourceImageSizeType sizeType, string caption = null) { return Current.GetResourceImage(resourceName, sizeType, caption); }
        /// <summary>
        /// Vrátí index ikony v dané velikosti.
        /// Pokud daná ikona neexistuje, a je dán parametr <paramref name="caption"/>, pak ikonu vygeneruje (z počátečních dvou písmen).
        /// </summary>
        /// <param name="iconName"></param>
        /// <param name="sizeType"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static int GetResourceIndex(string iconName, ResourceImageSizeType sizeType, string caption = null) { return Current.GetResourceIndex(iconName, sizeType, caption); }
        /// <summary>
        /// Vrací klávesovou zkratku pro daný string, typicky na vstupu je "Ctrl+C", na výstupu je <see cref="System.Windows.Forms.Keys.Control"/> | <see cref="System.Windows.Forms.Keys.C"/>
        /// </summary>
        /// <param name="shortCut"></param>
        /// <returns></returns>
        public static System.Windows.Forms.Keys GetShortcutKeys(string shortCut) { return Current.GetShortcutKeys(shortCut); }
        #endregion
    }
    /// <summary>
    /// Velikost obrázku typu Bitmapa
    /// </summary>
    public enum ResourceImageSizeType
    {
        /// <summary>None</summary>
        None = 0,
        /// <summary>Small</summary>
        Small,
        /// <summary>Medium</summary>
        Medium,
        /// <summary>Large</summary>
        Large
    }
}
