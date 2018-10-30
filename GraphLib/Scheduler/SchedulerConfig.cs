using Noris.LCS.Base.WorkScheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    /// <summary>
    /// Třída pro uchování různých nastavení primárně na úrovni lokálního počítače.
    /// Jde víceméně o vlastnosti uživatelské, ne datové, jako je Zoom celého GUI, Zoom časové osy, nastavení přichytávání prvků při jejich pohybu, atd...
    /// </summary>
    public class SchedulerConfig
    {
        #region Konstrukce, načtení, uložení
        /// <summary>
        /// Konstruktor, privátní, určený pro <see cref="Persist.Deserialize(string)"/>
        /// </summary>
        private SchedulerConfig()
        {   // Tímto konstruktorem projde i deserializace objektu v rámci Persistor
        }
        /// <summary>
        /// Konstruktor pro standardní použití, provede i načtení dat.
        /// Lze zadat soubor = null, pak se použije defaultní. Ale musí se volat tento konstruktor.
        /// </summary>
        /// <param name="configFile"></param>
        public SchedulerConfig(string configFile)
            : this()
        {
            this._LoadFrom(configFile);
        }
        /// <summary>
        /// Načte data z dodaného souboru
        /// </summary>
        /// <param name="configFile"></param>
        private void _LoadFrom(string configFile)
        {
            if (String.IsNullOrEmpty(configFile))
                configFile = _CONFIG_FILE_NAME;
            if (!configFile.Contains("\\"))
                configFile = System.IO.Path.Combine(Application.App.AppLocalDataPath, configFile);

            if (System.IO.File.Exists(configFile))
                Persist.LoadTo(configFile, this);

            this._ConfigFile = configFile;
        }
        /// <summary>
        /// Uloží aktuální stav objektu do svého souboru
        /// </summary>
        private void _Save()
        {
            string configFile = this._ConfigFile;
            if (String.IsNullOrEmpty(configFile)) return;
            string data = Persist.Serialize(this);
            Application.App.TryRunBgr(() => System.IO.File.WriteAllText(configFile, data, Encoding.UTF8));
        }
        /// <summary>
        /// Fullname konfiguračního souboru
        /// </summary>
        private string _ConfigFile;
        /// <summary>
        /// Default holé jméno konfiguračního souboru.
        /// Ukládá se do adresáře <see cref="Application.App.AppLocalDataPath"/>.
        /// </summary>
        private const string _CONFIG_FILE_NAME = "WorkScheduler.setting";
        #endregion
        #region Public property
        /// <summary>
        /// Zoom na časové ose
        /// </summary>
        public ToolbarSystemItem TimeAxisZoom
        {
            get { return (ToolbarSystemItem)(this._TimeAxisZoom & ToolbarSystemItem.TimeAxisZoomAll); }
            set { this._SetValue((ToolbarSystemItem)(value & ToolbarSystemItem.TimeAxisZoomAll), ref this._TimeAxisZoom); }
        }
        private ToolbarSystemItem _TimeAxisZoom = ToolbarSystemItem.TimeAxisZoomWorkWeek;
        /// <summary>
        /// Při pohybu prvků v grafu jej přichytávat k blízkým prvkům
        /// </summary>
        public bool MoveItemSnapToNearItems
        {
            get { return this._MoveItemSnapToNearItems; }
            set { this._SetValue(value, ref this._MoveItemSnapToNearItems); }
        }
        private bool _MoveItemSnapToNearItems = true;
        /// <summary>
        /// Vzdálenost v pixelech, na kterou se budou vyhledávat sousední prvky pro přichytávání k blízkým prvkům
        /// </summary>
        public int MoveItemSnapDistanceToNearItems
        {
            get { return this._MoveItemSnapDistanceToNearItems; }
            set { this._SetValue(value, 0, 50, ref this._MoveItemSnapDistanceToNearItems); }
        }
        private int _MoveItemSnapDistanceToNearItems = 10;
        /// <summary>
        /// Při pohybu prvků v grafu jej přichytávat k původnímu času prvku
        /// </summary>
        public bool MoveItemSnapToOriginalTime
        {
            get { return this._MoveItemSnapToOriginalTime; }
            set { this._SetValue(value, ref this._MoveItemSnapToOriginalTime); }
        }
        private bool _MoveItemSnapToOriginalTime = true;
        /// <summary>
        /// Při najetí myší zobrazovat vztahy v rámci celého postupu, nejen nejbližší sousední položky
        /// </summary>
        public bool GuiEditShowLinkWholeTask
        {
            get { return this._GuiEditShowLinkWholeTask; }
            set { this._SetValue(value, ref this._GuiEditShowLinkWholeTask); }
        }
        private bool _GuiEditShowLinkWholeTask = true;
        /// <summary>
        /// Vztahy zobrazovat jako křivky (výchozí: zobrazovat jako rovné čáry)
        /// </summary>
        public bool GuiEditShowLinkAsSCurve
        {
            get { return this._GuiEditShowLinkAsSCurve; }
            set { this._SetValue(value, ref this._GuiEditShowLinkAsSCurve); }
        }
        private bool _GuiEditShowLinkAsSCurve = true;
        /// <summary>
        /// Počet pixelů vzdálenosti od původního času, kdy se k tomuto původnímu času bude prvek přichytávat při pohybu na TOM SAMÉM grafu
        /// </summary>
        public int MoveItemSnapDistanceToOriginalTimeOnSameGraph
        {
            get { return this._MoveItemSnapDistanceToOriginalTimeOnSameGraph; }
            set { this._SetValue(value, 0, 50, ref this._MoveItemSnapDistanceToOriginalTimeOnSameGraph); }
        }
        private int _MoveItemSnapDistanceToOriginalTimeOnSameGraph = 5;
        /// <summary>
        /// Počet pixelů vzdálenosti od původního času, kdy se k tomuto původnímu času bude prvek přichytávat při pohybu na JINÉM grafu
        /// </summary>
        public int MoveItemSnapDistanceToOriginalTimeOnOtherGraph
        {
            get { return this._MoveItemSnapDistanceToOriginalTimeOnOtherGraph; }
            set { this._SetValue(value, 0, 50, ref this._MoveItemSnapDistanceToOriginalTimeOnOtherGraph); }
        }
        private int _MoveItemSnapDistanceToOriginalTimeOnOtherGraph = 15;
        /// <summary>
        /// Při pohybu prvků v grafu jej přichytávat k nejbližšímu zaokrouhlenému času
        /// </summary>
        public bool MoveItemSnapToNearRoundTime
        {
            get { return this._MoveItemSnapToNearRoundTime; }
            set { this._SetValue(value, ref this._MoveItemSnapToNearRoundTime); }
        }
        private bool _MoveItemSnapToNearRoundTime = true;

        /// <summary>
        /// Velikost prvku (šířka v pixelech), kdy považujeme vhodné přichytávat vždy na Begin.
        /// Menší prvky budou přichytávány na jejich Begin, i když je myš při přetahování na jejich fyzickém konci.
        /// Větší prvky umožní přichytávat i na jejich End, pokud je myš blíže ke konci prvku.
        /// </summary>
        public int MoveItemDetectSideMinSize
        {
            get { return this._MoveItemDetectSideMinSize; }
            set { this._SetValue(value, 0, 50, ref this._MoveItemDetectSideMinSize); }
        }
        private int _MoveItemDetectSideMinSize = 15;
        /// <summary>
        /// Poměrná část prvku, která se považuje za Begin, při určení strany přichytávání podle pozice myši na prvku.
        /// Pokud bude myš např. přesně v polovině prvku, bude se přichytávat čas Begin k sousedním prvkům k jejich času End.
        /// Na pozici <see cref="MoveItemDetectSideRatio"/> prvku se pozice láme mezi Begin a End.
        /// </summary>
        public float MoveItemDetectSideRatio
        {
            get { return this._MoveItemDetectSideRatio; }
            set { this._SetValue(value, 0f, 1f, ref this._MoveItemDetectSideRatio); }
        }
        private float _MoveItemDetectSideRatio = 0.60f;
        #endregion
        #region Ukládání hodnoty, detekce změny, automatické uložení konfigurace
        private void _SetValue(bool value, ref bool storage)
        {
            if (value == storage) return;
            storage = value;
            this._Save();
        }
        private void _SetValue(int value, int minValue, int maxValue, ref int storage)
        {
            value = (value < minValue ? minValue : (value > maxValue ? maxValue : value));
            this._SetValue(value, ref storage);
        }
        private void _SetValue(int value, ref int storage)
        {
            if (value == storage) return;
            storage = value;
            this._Save();
        }
        private void _SetValue(float value, float minValue, float maxValue, ref float storage)
        {
            value = (value < minValue ? minValue : (value > maxValue ? maxValue : value));
            this._SetValue(value, ref storage);
        }
        private void _SetValue(float value, ref float storage)
        {
            if (value == storage) return;
            storage = value;
            this._Save();
        }
        private void _SetValue(string value, ref string storage)
        {
            if (value == storage) return;
            storage = value;
            this._Save();
        }
        private void _SetValue(ToolbarSystemItem value, ref ToolbarSystemItem storage)
        {
            if (value == storage) return;
            storage = value;
            this._Save();
        }
        #endregion
    }
}
