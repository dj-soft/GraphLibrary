// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using WinDraw = System.Drawing;
using WinForm = System.Windows.Forms;

using DevExpress.XtraEditors;
using DevExpress.Utils.Extensions;
using DevExpress.XtraRichEdit.Model.History;
using Noris.Clients.Win.Components.AsolDX.DataForm.Data;


namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    #region DxDataFormPanel : panel pro zobrazení DataFormu
    /// <summary>
    /// <see cref="DxDataFormPanel"/> : virtuální grafický panel sloužící pro zobrazení řádků a prvků dle layoutu DataForm.
    /// </summary>
    public class DxDataFormPanel : DxVirtualPanel
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDataFormPanel()
        {
            _InitRows();
            _InitLayout();
            _InitContent();

            __Initialized = true;
        }
        /// <summary>
        /// Obsahuje true po skončení inicializace
        /// </summary>
        private bool __Initialized;
        #region Datové řádky
        /// <summary>
        /// Pole řádků zobrazených v formuláři
        /// </summary>
        public DataFormRows DataFormRows { get { return __DataFormRows; } }
        /// <summary>
        /// Inicializace dat řádků
        /// </summary>
        private void _InitRows()
        {
            __DataFormRows = new DataFormRows();
            __DataFormRows.CollectionChanged += _RowsChanged;
        }
        /// <summary>
        /// Po změně řádků (přidání, odebrání), nikoli po změně obsahu dat v řádku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _RowsChanged(object sender, EventArgs e)
        {
            DesignSizeInvalidate();
        }
        /// <summary>
        /// Fyzická kolekce řádků
        /// </summary>
        private DataFormRows __DataFormRows;
        #endregion
        #region Definice prvků layoutu jednotlivého řádku
        /// <summary>
        /// Definice vzhledu pro jednotlivý řádek: popisuje panely, záložky, prvky ve vnořené hierarchické podobě.
        /// Z této definice se následně generují jednotlivé interaktivní prvky pro jednotlivý řádek.
        /// </summary>
        public DataFormLayoutSet DataFormLayout { get { return __DataFormLayout; } }
        /// <summary>
        /// Inicializace dat layoutu
        /// </summary>
        private void _InitLayout()
        {
            __DataFormLayout = new DataFormLayoutSet();
            __DataFormLayout.CollectionChanged += _LayoutChanged;

            Padding = new WinForm.Padding(0);
        }
        /// <summary>
        /// Změna Padding vede ke změně <see cref="ContentDesignSize"/>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaddingChanged(EventArgs e)
        {
            DesignSizeInvalidate();
        }
        /// <summary>
        /// Definice vzhledu pro jednotlivý řádek
        /// </summary>
        private DataFormLayoutSet __DataFormLayout;
        /// <summary>
        /// Po změně definice layoutu (přidání, odebrání), nikoli po změně obsahu dat v prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LayoutChanged(object sender, EventArgs e)
        {
            DesignSizeInvalidate();
        }
        #endregion
        #region ContentPanel
        /// <summary>
        /// Inicializace Content panelu
        /// </summary>
        private void _InitContent()
        {
            __DataFormContent = new DxDataFormContentPanel();
            this.ContentPanel = __DataFormContent;
        }
        private DxDataFormContentPanel __DataFormContent;
        #endregion
        #region ContentDesignSize : velikost obsahu v designových pixelech
        /// <summary>
        /// Potřebná velikost obsahu v designových pixelech. Validovaná hodnota.
        /// </summary>
        public override WinDraw.Size? ContentDesignSize 
        {
            get
            {
                if (__Initialized && !__ContentDesignSize.HasValue)
                {
                    DesignSizeInvalidate();
                    __ContentDesignSize = _CalculateContentDesignSize();
                }
                return __ContentDesignSize;
            }
            set { }
        }
        /// <summary>
        /// Invaliduje celkovou velikost <see cref="ContentDesignSize"/>.
        /// Provádí se po změně řádků nebo definice designu.
        /// </summary>
        protected override void DesignSizeInvalidate()
        {
            __ContentDesignSize = null;
            base.DesignSizeInvalidate();
        }
        /// <summary>
        /// Vypočte a vrátí údaj: Potřebná velikost obsahu v designových pixelech.
        /// </summary>
        /// <returns></returns>
        private WinDraw.Size _CalculateContentDesignSize()
        {
            var padding = this.Padding;
            this.DataFormLayout.HostDesignSize = this.ContentPanelDesignSize;            // Do Layoutu vložím viditelnou velikost
            this.DataFormRows.OneRowDesignSize = this.DataFormLayout.DesignSize;         // Z Layoutu načtu velikost jednoho řádku a vložím do RowSetu
            var allRowsDesignSize = this.DataFormRows.ContentDesignSize;                 // Z RowSetu načtu velikost všech řádků
            return allRowsDesignSize.Add(padding);
        }
        /// <summary>
        /// Potřebná velikost obsahu v designových pixelech. Úložiště.
        /// </summary>
        private WinDraw.Size? __ContentDesignSize;
        #endregion
    }
    #endregion
    #region DxDataFormPanel : fyzický interaktivní panel pro zobrazení contentu DataFormu
    /// <summary>
    /// <see cref="DxDataFormContentPanel"/> : fyzický interaktivní panel pro zobrazení contentu DataFormu.
    /// Řeší grafické vykreslení prvků a řeší interaktivitu myši a klávesnice.
    /// Pro fyzické vykreslení obsahu prvku volá jeho vlastní metodu, to neřeší panel.
    /// </summary>
    public class DxDataFormContentPanel : DxInteractivePanel
    {
        #region Info ... Vnoření prvků, souřadné systémy, řádky
        /*
        Třída DxInteractivePanel (náš předek) v sobě hostuje interaktivní prvky IInteractiveItem uložené v DxInteractivePanel.Items, ty tvoří Root úroveň.
        Mohou to být Containery { Panely, Záložky } nebo samotné prvky { TextBox, Label, Combo, Picture, CheckBox, atd }.
        Containery v sobě mohou obsahovat další Containery nebo samotné prvky.
        Containery obsahují svoje Child prvky v IInteractiveItem.Items. Jejich souřadný systém je pak relativní k jejich Parentu.
        Scrollování je podporováno jen na úrovni celého DataFormu, ale nikoliv na úrovni Containeru (=Panel ani Záložka nebude mít ScrollBary).
        _LayoutChanged podporuje Zoom. Výchozí je vložen při tvorbě DataFormu anebo při změně Zoomu, podporován je i interaktivní Zoom: Ctrl+MouseWheel.
        Jde o dvě hodnoty Zoomu: systémový × lokální.
        Prvek IInteractiveItem definuje svoji pozici v property DesignBounds, kde je souřadnice daná v Design pixelech (bez vlivu Zoomu), relativně k Parentu, v rámci řádku.

        Řádky:
        Pole prvků DxInteractivePanel.Items definuje vzhled jednoho řádku.








        */
        #endregion
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDataFormContentPanel()
        {
           
        }
        #endregion
      
    }
    #endregion

}
