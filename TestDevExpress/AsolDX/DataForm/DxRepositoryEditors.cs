// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using SysIO = System.IO;
using WinDraw = System.Drawing;
using WinForm = System.Windows.Forms;
using DxDData = Noris.Clients.Win.Components.AsolDX.DataForm.Data;
using DxeEdit = DevExpress.XtraEditors;
using DxeCont = DevExpress.XtraEditors.Controls;
using System.Drawing;
using System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    // Sada tříd konkrétních editorů (Label, TextBox, EditBox, Button, .......)

    #region Label
    /// <summary>
    /// Label
    /// </summary>
    internal class DxRepositoryEditorLabel : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        internal DxRepositoryEditorLabel(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        internal override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.Label; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.DirectPaint; } }
        /// <summary>
        /// Vrátí true, pokud daný prvek má aktuálně používat nativní Control (tedy nebude se keslit jako Image),
        /// a to dle aktuálního interaktivním stavu objektu a s přihlédnutím ke stavu <paramref name="isDisplayed"/>.
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        /// <param name="isUrgent">false = běžný dotaz, vrátí true když prvek má myš nebo má klávesový Focus; true = je velká nouze, vrátí true (=potřebuje NativeControl) jen tehdy, když prvek má klávesový Focus (vrátí false když má jen myš)</param>
        /// <returns></returns>
        protected override bool NeedUseNativeControl(IPaintItemData paintData, bool isDisplayed, bool isUrgent)
        {
            return false;                        // Label nepotřebuje nativní control nikdy...
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek má vykreslovat svoje data (=kreslit pomocí metod např. <see cref="DxRepositoryEditor.CreateImageData(IPaintItemData, PaintDataEventArgs, Rectangle)"/>), 
        /// a to dle aktuálního interaktivním stavu objektu a s přihlédnutím ke stavu <paramref name="isDisplayed"/>.
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="isDisplayed">Prvek je ve viditelné části panelu.Pokud je false, pak se vykreslování nemusí provádět, a případný dočasný nativní control se může odebrat.</param>
        /// <returns></returns>
        protected override bool NeedPaintItem(IPaintItemData paintData, bool isDisplayed)
        {
            return (isDisplayed);                // Label bude evykreslovat tehdy, když je ve viditelné oblasti
        }
        /// <summary>
        /// Potomek zde přímo do cílové grafiky vykreslí obsah prvku, bez cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected override void PaintImageDirect(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            string text = GetItemLabel(paintData);
            if (!String.IsNullOrEmpty(text))
            {
                var font = DxComponent.GetFontDefault(targetDpi: pdea.InteractivePanel.CurrentDpi);
                var color = DxComponent.GetSkinColor(SkinElementColor.CommonSkins_ControlText) ?? WinDraw.Color.Violet;
                var alignment = this.GetItemContent<WinDraw.ContentAlignment>(paintData, Data.DxDataFormProperty.LabelAlignment, WinDraw.ContentAlignment.MiddleLeft);
                DxComponent.DrawText(pdea.Graphics, text, font, controlBounds, color, alignment);
            }
        }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            string text = GetItemLabel(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);

            return CreateKey(text, controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor));
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            // Některé controly (a DxeEdit.LabelControl mezi nimi) mají tu nectnost, že první vykreslení bitmapy v jejich životě je chybné.
            //  Nejde o první vykreslení do jedné Graphics, jde o první vykreslení po konstruktoru.
            //  Projevuje se to tak, že do Bitmapy vykreslí pozadí zcela černé v celé ploše mimo text Labelu!
            // Proto detekujeme první použití (kdy na začátku je __EditorPaint = null), a v tom případě vygenerujeme tu úplně první bitmapu naprázdno.
            bool isFirstUse = (__EditorPaint is null);

            __EditorPaint ??= _CreateNativeControl(false, pdea.InteractivePanel);
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);

            // První vygenerovanou bitmapu v životě Labelu vytvoříme a zahodíme, není pěkná...
            if (isFirstUse) CreateBitmapData(__EditorPaint, pdea.Graphics);

            // Teprve ne-první bitmapy jsou OK:
            return CreateBitmapData(__EditorPaint, pdea.Graphics);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return null;
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.LabelControl, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <param name="isInteractive">Vytvořit interaktovní control: navázat do něj zdejší eventhandlery</param>
        /// <param name="parent">Parent control, do něhož je vytvořený Control vložen. Pouze pro EditorPaint control. Přebírá z něj barvu Background.</param>
        /// <returns></returns>
        private DxeEdit.LabelControl _CreateNativeControl(bool isInteractive, WinForm.Control parent = null)
        {
            var control = new DxeEdit.LabelControl() { Location = new WinDraw.Point(25, -200) };
            // control.ResetBackColor();
            control.LineStyle = WinDraw.Drawing2D.DashStyle.Solid;
            control.LineVisible = true;
            control.LineLocation = DxeEdit.LineLocation.Bottom;
            control.LineColor = WinDraw.Color.DarkBlue;
            control.LineOrientation = DxeEdit.LabelLineOrientation.Horizontal;
            control.BorderStyle = DxeCont.BorderStyles.Office2003;
            if (parent != null)
                parent.Controls.Add(control);

            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.LabelControl control, WinDraw.Rectangle controlBounds)
        {   //  Ono po pravdě, protože 'CacheMode' je DirectPaint, a protože NeedUseNativeControl vrací vždy false, 
            // tak je jisto, že Label nikdy nebude používat nativní Control => neprojdeme tudy:
            control.Text = GetItemLabel(paintData);
            control.Size = controlBounds.Size;
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        /// <summary>
        /// Nativní control používaný pro kreslení
        /// </summary>
        DxeEdit.LabelControl __EditorPaint;
    }
    #endregion
    #region TextBox
    /// <summary>
    /// TextBox
    /// </summary>
    internal class DxRepositoryEditorTextBox : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        internal DxRepositoryEditorTextBox(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        internal override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.TextBox; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCacheWithItemImage; } }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);

            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor));
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= _CreateNativeControl(false, pdea.InteractivePanel);
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint, pdea.Graphics);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.TextEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <param name="isInteractive">Vytvořit interaktovní control: navázat do něj zdejší eventhandlery</param>
        /// <param name="parent">Parent control, do něhož je vytvořený Control vložen. Pouze pro EditorPaint control. Přebírá z něj barvu Background.</param>
        /// <returns></returns>
        private DxeEdit.TextEdit _CreateNativeControl(bool isInteractive, WinForm.Control parent = null)
        {
            var control = new DxeEdit.TextEdit() { Location = new WinDraw.Point(25, -200) };
            if (parent != null)
                parent.Controls.Add(control);
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.TextEdit control, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            control.EditValue = value;
            StorePairOriginalData(dataPair, paintData, value);
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.TextEdit control)
        {
            control.CausesValidation = true;
            control.EditValueChanged += _NativeControlEditValueChanged;
            control.Validating += _NativeControlValidating;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v TextEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _NativeControlEditValueChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            // Toto je průběžně volaný event, v procesu editace, a nemá valného významu:
            if (sender is DxeEdit.TextEdit control)
                this.TryStorePairCurrentEditingValue(control, control.EditValue);
        }
        /// <summary>
        /// Eventhandler při validaci zadané hodnoty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _NativeControlValidating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.TextEdit control)
            {   // Toto je event volaný při ukončení editace
                bool isValidated = TryStoreValidatingValue(control, control.EditValue, false, out var cancelInfo);
                if (!isValidated)
                {   // Že bych vrátil původní hodnotu?
                    control.EditValue = cancelInfo.OriginalValue;
                    e.Cancel = true;
                }
            }
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        /// <summary>
        /// Nativní control používaný pro kreslení
        /// </summary>
        DxeEdit.TextEdit __EditorPaint;
    }
    #endregion
    #region TextBoxButton
    /// <summary>
    /// TextBoxButton
    /// </summary>
    internal class DxRepositoryEditorTextBoxButton : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        internal DxRepositoryEditorTextBoxButton(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        internal override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.TextBoxButton; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);
            var buttons = this.GetItemContent<DxDData.TextBoxButtonProperties>(paintData, Data.DxDataFormProperty.TextBoxButtons, null);
            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor), buttons?.Key);
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= _CreateNativeControl(false, pdea.InteractivePanel);
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint, pdea.Graphics);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.ButtonEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <param name="isInteractive">Vytvořit interaktovní control: navázat do něj zdejší eventhandlery</param>
        /// <param name="parent">Parent control, do něhož je vytvořený Control vložen. Pouze pro EditorPaint control. Přebírá z něj barvu Background.</param>
        /// <returns></returns>
        private DxeEdit.ButtonEdit _CreateNativeControl(bool isInteractive, WinForm.Control parent = null)
        {
            var control = new DxeEdit.ButtonEdit() { Location = new WinDraw.Point(25, -200) };
            if (parent != null)
                parent.Controls.Add(control);
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.ButtonEdit control, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            control.EditValue = value;
            StorePairOriginalData(dataPair, paintData, value);
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);

            control.Properties.Buttons.Clear();
            var buttons = this.GetItemContent<DxDData.TextBoxButtonProperties>(paintData, Data.DxDataFormProperty.TextBoxButtons, null);
            if (buttons != null)
            {
                control.Properties.ButtonsStyle = buttons.BorderStyle;
                control.Properties.Buttons.AddRange(buttons.CreateDxButtons());
            }
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.ButtonEdit control)
        {
            control.CausesValidation = true;
            control.EditValueChanged += _ControlEditValueChanged;
            control.ButtonClick += _NativeControlButtonClick;
            control.Validating += _NativeControlValidating;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v ButtonEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlEditValueChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            // Toto je průběžně volaný event, v procesu editace, a nemá valného významu:
            if (sender is DxeEdit.ButtonEdit control)
                this.TryStorePairCurrentEditingValue(control, control.EditValue);
        }
        /// <summary>
        /// Eventhandler při validaci zadané hodnoty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _NativeControlValidating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ButtonEdit control)
            {   // Toto je event volaný při ukončení editace
                bool isValidated = TryStoreValidatingValue(control, control.EditValue, false, out var cancelInfo);
                if (!isValidated)
                {   // Že bych vrátil původní hodnotu?
                    control.EditValue = cancelInfo.OriginalValue;
                    e.Cancel = true;
                }
            }
        }
        /// <summary>
        /// Eventhandler po kliknutí na button v ButtonEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlButtonClick(object sender, DxeCont.ButtonPressedEventArgs e)
        {
            if (SuppressNativeEvents) return;
            this.RunDataFormAction(sender as WinForm.Control, DxDData.DxDataFormAction.ButtonClick, e.Button.Tag as string);
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        /// <summary>
        /// Nativní control používaný pro kreslení
        /// </summary>
        DxeEdit.ButtonEdit __EditorPaint;
    }
    #endregion
    #region EditBox
    /// <summary>
    /// EditBox
    /// </summary>
    internal class DxRepositoryEditorEditBox : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        internal DxRepositoryEditorEditBox(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        internal override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.TextBox; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Potomek v této metodě dostává informaci o stisknuté klávese, a může ovlivnit, zda tuto klávesu dostane DataForm k vyhodnocení, zda jde o klávesu posouvající Focus.
        /// Pokud potomek zde vrátí true, pak on sám bude zpracovávat tuto klávesu a nechce, aby ji dostal někdo jiný.
        /// Typicky EditBox chce zpracovat klávesu Enter (= nový řádek v textu) i kurzorové klávesy nahoru/dolů. 
        /// Stejně tak CheckBox chce zpracovat klávesu Enter (= změna stavu Checked).
        /// <para/>
        /// Bázová třída vrací false = všechny běžné klávesy mohou být vyhodnoceny v DataFormu a mohou přemístit Focus.
        /// </summary>
        /// <param name="dataPair"></param>
        /// <param name="keyArgs"></param>
        /// <returns></returns>
        protected override bool NeedProcessKeyDown(ControlDataPair dataPair, KeyEventArgs keyArgs)
        {
            // Vrátím true pro klávesu Enter; tu chci zpracovat privátně v editoru a ne ji odesílat do DataFormu pro možný posun Focusu:
            return (keyArgs.KeyData == Keys.Enter);
        }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            // Pokud zobrazený text bude delší než 50 znaků, pak nebudu vytvářet klíč.
            // Důsledkem toho bude, že tento konkrétní prvek nebude ukládat svoji bitmapu do cache obrázků v DxRepositoryManager 
            //  (metody TryGetCacheImageData a AddImageDataToCache), ale bude bitmapu obrázku ukládat privátně do IPaintItemData.ImageData.
            // Důvod: u dlouhých textů v tomto typu editoru (který je určen právě pro zobrazení dlouhých textů!) bychom generovali velice dlouhý Key, 
            //  a málokdy by se na jednom DataFormu sešly dva EditBoxy se shodným klíčem (=textem). 
            // Proto považuji za vhodné ukládat do cache víceméně jen prázdné EditBoxy (bez textu) anebo s krátkým textem, kdy jeho Key nebude dlouhý 
            //  a je šance na opakované využití stejného obrázku pro víc controlů (např. pro zobrazené texty: OK, Odmítnuto, Dořešit, ...)
            // Při uložení obrázku do Cache se ukládá nejen obrázek, ale i jeho (dlouhý) string klíč a long ID (=režie), kdežto při uložení do ImageData se ukládá jen obrázek.
            string value = GetItemValue(paintData) as string;
            if (value != null && value.Length > 24) return null;

            // Text je krátký, vytvoříme Key a návazně uložíme obrázek do Cache:
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);

            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor));
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= _CreateNativeControl(false, pdea.InteractivePanel);
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint, pdea.Graphics);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.MemoEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <param name="isInteractive">Vytvořit interaktovní control: navázat do něj zdejší eventhandlery</param>
        /// <param name="parent">Parent control, do něhož je vytvořený Control vložen. Pouze pro EditorPaint control. Přebírá z něj barvu Background.</param>
        /// <returns></returns>
        private DxeEdit.MemoEdit _CreateNativeControl(bool isInteractive, WinForm.Control parent = null)
        {
            var control = new DxeEdit.MemoEdit() { Location = new WinDraw.Point(25, -200) };
            // control.ResetBackColor();
            if (parent != null)
                parent.Controls.Add(control);
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.MemoEdit control, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            control.EditValue = value;
            StorePairOriginalData(dataPair, paintData, value);
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.MemoEdit control)
        {
            control.CausesValidation = true;
            control.EditValueChanged += _NativeControlEditValueChanged;
            control.Validating += _NativeControlValidating;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v MemoEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _NativeControlEditValueChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            // Toto je průběžně volaný event, v procesu editace, a nemá valného významu:
            if (sender is DxeEdit.MemoEdit control)
                this.TryStorePairCurrentEditingValue(control, control.EditValue);
        }
        /// <summary>
        /// Eventhandler při validaci zadané hodnoty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _NativeControlValidating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.MemoEdit control)
            {   // Toto je event volaný při ukončení editace
                bool isValidated = TryStoreValidatingValue(control, control.EditValue, false, out var cancelInfo);
                if (!isValidated)
                {   // Že bych vrátil původní hodnotu?
                    control.EditValue = cancelInfo.OriginalValue;
                    e.Cancel = true;
                }
            }
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        /// <summary>
        /// Nativní control používaný pro kreslení
        /// </summary>
        DxeEdit.MemoEdit __EditorPaint;
    }
    #endregion
    #region CheckEdit
    /// <summary>
    /// CheckEdit
    /// </summary>
    internal class DxRepositoryEditorCheckEdit : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        internal DxRepositoryEditorCheckEdit(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        internal override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.CheckEdit; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Potomek v této metodě dostává informaci o stisknuté klávese, a může ovlivnit, zda tuto klávesu dostane DataForm k vyhodnocení, zda jde o klávesu posouvající Focus.
        /// Pokud potomek zde vrátí true, pak on sám bude zpracovávat tuto klávesu a nechce, aby ji dostal někdo jiný.
        /// Typicky EditBox chce zpracovat klávesu Enter (= nový řádek v textu) i kurzorové klávesy nahoru/dolů. 
        /// Stejně tak CheckBox chce zpracovat klávesu Enter (= změna stavu Checked).
        /// <para/>
        /// Bázová třída vrací false = všechny běžné klávesy mohou být vyhodnoceny v DataFormu a mohou přemístit Focus.
        /// </summary>
        /// <param name="dataPair"></param>
        /// <param name="keyArgs"></param>
        /// <returns></returns>
        protected override bool NeedProcessKeyDown(ControlDataPair dataPair, KeyEventArgs keyArgs)
        {
            // Vrátím true pro klávesu Enter; tu chci zpracovat privátně v editoru a ne ji odesílat do DataFormu pro možný posun Focusu:
            return (keyArgs.KeyData == Keys.Enter);
        }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);
            var buttons = this.GetItemContent<DxDData.TextBoxButtonProperties>(paintData, Data.DxDataFormProperty.TextBoxButtons, null);
            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor), buttons?.Key);
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= _CreateNativeControl(false, pdea.InteractivePanel);
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);

            // var image = __EditorPaint.Properties.Appearance.GetImage();
            // image?.Save(@"c:\DavidPrac\CheckEdit.png", WinDraw.Imaging.ImageFormat.Png);
            // __EditorPaint.


            return CreateBitmapData(__EditorPaint, pdea.Graphics, null, WinDraw.Color.LightCoral);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.CheckEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <param name="isInteractive">Vytvořit interaktovní control: navázat do něj zdejší eventhandlery</param>
        /// <param name="parent">Parent control, do něhož je vytvořený Control vložen. Pouze pro EditorPaint control. Přebírá z něj barvu Background.</param>
        /// <returns></returns>
        private DxeEdit.CheckEdit _CreateNativeControl(bool isInteractive, WinForm.Control parent = null)
        {
            var control = new DxeEdit.CheckEdit() { Location = new WinDraw.Point(25, -200) };
            // Konstantní nastavení controlu:
            control.Properties.AutoHeight = false;
            control.Properties.AutoWidth = false;
            control.Properties.CheckBoxOptions.Style = DxeCont.CheckBoxStyle.SvgCheckBox1;
            control.Properties.GlyphAlignment = DevExpress.Utils.HorzAlignment.Near;
            if (parent != null)
                parent.Controls.Add(control);
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.CheckEdit control, WinDraw.Rectangle controlBounds)
        {
            bool value = GetItemValue(paintData, false);
            control.Checked = value;
            StorePairOriginalData(dataPair, paintData, value);
            control.Size = controlBounds.Size;
            string label = GetItemLabel(paintData);
            control.Properties.Caption = label;
            control.Properties.DisplayValueUnchecked = GetItemContent(paintData, DxDData.DxDataFormProperty.CheckBoxLabelFalse, label);
            control.Properties.DisplayValueChecked = GetItemContent(paintData, DxDData.DxDataFormProperty.CheckBoxLabelTrue, label);
            control.Properties.BorderStyle = GetItemCheckBoxBorderStyle(paintData);
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.CheckEdit control)
        {
            control.CheckedChanged += _NativeControlCheckedChanged;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v CheckEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlCheckedChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.CheckEdit control)
            {   // Toto je event volaný při ukončení editace
                bool isValidated = TryStoreValidatingValue(control, control.Checked, true, out var cancelInfo);
                if (!isValidated)
                {   // Že bych vrátil původní hodnotu?
                    control.Checked = (bool)cancelInfo.OriginalValue;
                }
            }
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        /// <summary>
        /// Nativní control používaný pro kreslení
        /// </summary>
        DxeEdit.CheckEdit __EditorPaint;
    }
    #endregion
    #region ToggleSwitch
    /// <summary>
    /// ToggleSwitch
    /// </summary>
    internal class DxRepositoryEditorToggleSwitch : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        internal DxRepositoryEditorToggleSwitch(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        internal override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.ToggleSwitch; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);
            var buttons = this.GetItemContent<DxDData.TextBoxButtonProperties>(paintData, Data.DxDataFormProperty.TextBoxButtons, null);
            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor), buttons?.Key);
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= _CreateNativeControl(false, pdea.InteractivePanel);
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint, pdea.Graphics);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.ToggleSwitch, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <param name="isInteractive">Vytvořit interaktovní control: navázat do něj zdejší eventhandlery</param>
        /// <param name="parent">Parent control, do něhož je vytvořený Control vložen. Pouze pro EditorPaint control. Přebírá z něj barvu Background.</param>
        /// <returns></returns>
        private DxeEdit.ToggleSwitch _CreateNativeControl(bool isInteractive, WinForm.Control parent = null)
        {
            var control = new DxeEdit.ToggleSwitch() { Location = new WinDraw.Point(25, -200) };
            // Konstantní nastavení controlu:
            control.Properties.AutoHeight = false;
            control.Properties.AutoWidth = false;
            control.Properties.ShowText = true;
            if (parent != null)
                parent.Controls.Add(control);
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.ToggleSwitch control, WinDraw.Rectangle controlBounds)
        {
            bool value = GetItemValue(paintData, false);
            control.IsOn = value;
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemCheckBoxBorderStyle(paintData);
            control.Properties.EditorToThumbWidthRatio = GetItemContent(paintData, DxDData.DxDataFormProperty.ToggleSwitchRatio, 2.5f);
            control.Properties.OffText = GetItemContent(paintData, DxDData.DxDataFormProperty.CheckBoxLabelFalse, "Off");
            control.Properties.OnText = GetItemContent(paintData, DxDData.DxDataFormProperty.CheckBoxLabelTrue, "On");
            control.Properties.GlyphAlignment = GetItemIconHorzizontalAlignment(paintData);
            // control.ReadOnly = true;
            // control.Enabled = true;
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.ToggleSwitch control)
        {
            control.Toggled += _NativeControlToggled;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v ToggleSwitch controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlToggled(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ToggleSwitch control)
            {   // Toto je event volaný při ukončení editace
                bool isValidated = TryStoreValidatingValue(control, control.IsOn, false, out var cancelInfo);
                if (!isValidated)
                {   // Že bych vrátil původní hodnotu?
                    control.IsOn = (bool)cancelInfo.OriginalValue;
                }
            }
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        /// <summary>
        /// Nativní control používaný pro kreslení
        /// </summary>
        DxeEdit.ToggleSwitch __EditorPaint;
    }
    #endregion
    #region ComboListBox
    /// <summary>
    /// ComboListBox : <see cref="DxeEdit.ComboBoxEdit"/>
    /// </summary>
    internal class DxRepositoryEditorComboListBox : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        internal DxRepositoryEditorComboListBox(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        internal override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.ComboListBox; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);
            var buttons = this.GetItemContent<DxDData.TextBoxButtonProperties>(paintData, Data.DxDataFormProperty.TextBoxButtons, null);
            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor), buttons?.Key);
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= _CreateNativeControl(false, pdea.InteractivePanel);
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint, pdea.Graphics);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.ComboBoxEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <param name="isInteractive">Vytvořit interaktovní control: navázat do něj zdejší eventhandlery</param>
        /// <param name="parent">Parent control, do něhož je vytvořený Control vložen. Pouze pro EditorPaint control. Přebírá z něj barvu Background.</param>
        /// <returns></returns>
        private DxeEdit.ComboBoxEdit _CreateNativeControl(bool isInteractive, WinForm.Control parent = null)
        {
            var control = new DxeEdit.ComboBoxEdit() { Location = new WinDraw.Point(25, -200) };
            if (parent != null)
                parent.Controls.Add(control);
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.ComboBoxEdit control, WinDraw.Rectangle controlBounds)
        {
            var value = GetItemValue(paintData);
            _FillNativeComboItems(paintData, control, value);
            StorePairOriginalData(dataPair, paintData, value);
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);
        }
        /// <summary>
        /// Najde data pro položky ComboBoxu, vygeneruje je a naplní i aktuálně vybranou hodnotu.
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="control"></param>
        /// <param name="value"></param>
        private void _FillNativeComboItems(IPaintItemData paintData, DxeEdit.ComboBoxEdit control, object value)
        {
            int selectedIndex = -1;

            try
            {
                control.Properties.Items.BeginUpdate();
                control.Properties.Items.Clear();

                var comboItems = this.GetItemContent<DxDData.ImageComboBoxProperties>(paintData, Data.DxDataFormProperty.ComboBoxItems, null);
                if (comboItems != null)
                {
                    control.Properties.AllowDropDownWhenReadOnly = ConvertToDxBoolean(comboItems.AllowDropDownWhenReadOnly);
                    control.Properties.AutoComplete = comboItems.AutoComplete;
                    control.Properties.ImmediatePopup = comboItems.ImmediatePopup;
                    control.Properties.PopupSizeable = comboItems.PopupSizeable;
                    control.Properties.ShowDropDown = DxeCont.ShowDropDown.SingleClick;
                    control.Properties.ShowPopupShadow = true;
                    control.Properties.Sorted = false;

                    var dxComboItems = comboItems.CreateDxComboItems();
                    control.Properties.Items.AddRange(dxComboItems);

                    selectedIndex = comboItems.GetIndexOfValue(value);
                }
            }
            finally
            {
                control.Properties.Items.EndUpdate();
            }

            control.SelectedIndex = selectedIndex;
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.ComboBoxEdit control)
        {
            control.Properties.AllowMouseWheel = false;
            control.EditValueChanged += _NativeControlEditValueChanged;
            control.Properties.QueryPopUp += _NativeControlQueryPopUp;
            control.LostFocus += _NativeControlLostFocus;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v ButtonEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _NativeControlEditValueChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            // Toto je event volaný po každé změně, v procesu editace, ale v Combo reagujeme ihned:
            if (sender is DxeEdit.ComboBoxEdit control)
            {
                var dxItem = control.SelectedItem;
                if (dxItem != null && dxItem is DxDData.ImageComboBoxProperties.Item item)
                {   // Je vybraná konkrétní položka?
                    var value = item.Value;
                    bool isValidated = TryStoreValidatingValue(control, value, false, out var cancelInfo);
                    if (!isValidated)
                    {   // Že bych vrátil původní hodnotu?
                        // control.EditValue = cancelInfo.OriginalValue;
                    }
                }
                else
                {   // Není vybraná konkrétní položka, ale je vepsán kus textu...
                    var editValue = control.EditValue;
                    // Co s tím?
                }
            }
        }
        /// <summary>
        /// Eventhandler před pokusem o otevření Popup okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlQueryPopUp(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ComboBoxEdit control && TryGetPaintData(control, out var paintData))
            {
                if (paintData.LayoutItem.TryGetContent<WinDraw.Size>(DxDData.DxDataFormProperty.ComboPopupFormSize, out var size) && size.Width > 50 && size.Height > 50)
                    control.Properties.PopupFormSize = size;
            }
        }
        /// <summary>
        /// Eventhandler po odchodu z prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlLostFocus(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ComboBoxEdit control)
            {
                // Uložím si velikost Popup okna:
                if (TryGetPaintData(control, out var paintData))
                {
                    var size = control.GetPopupEditForm()?.Size;                   // Funguje   (na rozdíl od : control.Properties.PopupFormSize)
                    if (size.HasValue && size.Value.Width > 50 && size.Value.Height > 50)
                        paintData.LayoutItem.SetContent<WinDraw.Size>(DxDData.DxDataFormProperty.ComboPopupFormSize, size.Value);
                }
            }
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        /// <summary>
        /// Nativní control používaný pro kreslení
        /// </summary>
        DxeEdit.ComboBoxEdit __EditorPaint;
    }
    #endregion
    #region ImageComboListBox
    /// <summary>
    /// ImageComboListBox : <see cref="DxeEdit.ImageComboBoxEdit"/>
    /// </summary>
    internal class DxRepositoryEditorImageComboListBox : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        internal DxRepositoryEditorImageComboListBox(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        internal override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.ImageComboListBox; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);
            var buttons = this.GetItemContent<DxDData.TextBoxButtonProperties>(paintData, Data.DxDataFormProperty.TextBoxButtons, null);
            return CreateKey(value?.ToString(), controlBounds.Size, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor), buttons?.Key);
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= _CreateNativeControl(false, pdea.InteractivePanel);
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint, pdea.Graphics);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds"></param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.ImageComboBoxEdit, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <param name="isInteractive">Vytvořit interaktovní control: navázat do něj zdejší eventhandlery</param>
        /// <param name="parent">Parent control, do něhož je vytvořený Control vložen. Pouze pro EditorPaint control. Přebírá z něj barvu Background.</param>
        /// <returns></returns>
        private DxeEdit.ImageComboBoxEdit _CreateNativeControl(bool isInteractive, WinForm.Control parent = null)
        {
            var control = new DxeEdit.ImageComboBoxEdit() { Location = new WinDraw.Point(25, -200) };
            if (parent != null)
                parent.Controls.Add(control);
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        private void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.ImageComboBoxEdit control, WinDraw.Rectangle controlBounds)
        {
            object value = GetItemValue(paintData);
            _FillNativeComboItems(paintData, control, value);
            StorePairOriginalData(dataPair, paintData, value);
            control.Size = controlBounds.Size;
            control.Properties.BorderStyle = GetItemBorderStyle(paintData);
        }
        /// <summary>
        /// Najde data pro položky ComboBoxu, vygeneruje je a naplní i aktuálně vybranou hodnotu.
        /// </summary>
        /// <param name="paintData"></param>
        /// <param name="control"></param>
        /// <param name="value"></param>
        private void _FillNativeComboItems(IPaintItemData paintData, DxeEdit.ImageComboBoxEdit control, object value)
        {
            int selectedIndex = -1;

            try
            {
                control.Properties.Items.BeginUpdate();
                control.Properties.Items.Clear();

                var comboItems = this.GetItemContent<DxDData.ImageComboBoxProperties>(paintData, Data.DxDataFormProperty.ComboBoxItems, null);
                if (comboItems != null)
                {
                    control.Properties.AllowDropDownWhenReadOnly = ConvertToDxBoolean(comboItems.AllowDropDownWhenReadOnly);
                    control.Properties.AutoComplete = comboItems.AutoComplete;
                    control.Properties.ImmediatePopup = comboItems.ImmediatePopup;
                    control.Properties.PopupSizeable = comboItems.PopupSizeable;
                    control.Properties.ShowDropDown = DxeCont.ShowDropDown.SingleClick;
                    control.Properties.ShowPopupShadow = true;
                    control.Properties.Sorted = false;

                    var dxComboItems = comboItems.CreateDxImageComboItems(out var imageList);
                    control.Properties.SmallImages = imageList;
                    control.Properties.Items.AddRange(dxComboItems);

                    selectedIndex = comboItems.GetIndexOfValue(value);
                }
            }
            finally
            {
                control.Properties.Items.EndUpdate();
            }

            control.SelectedIndex = selectedIndex;
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.ImageComboBoxEdit control)
        {
            control.Properties.AllowMouseWheel = false;
            control.CausesValidation = true;
            control.EditValueChanged += _ControlEditValueChanged;
            control.Properties.QueryPopUp += _NativeControlQueryPopUp;
            control.Properties.BeforePopup += _NativeControlBeforePopup;
            control.LostFocus += _NativeControlLostFocus;
        }
        /// <summary>
        /// Eventhandler po změně hodnoty v ButtonEdit controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlEditValueChanged(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            // Toto je event volaný po každé změně, v procesu editace, ale v Combo reagujeme ihned:
            if (sender is DxeEdit.ImageComboBoxEdit control)
            {
                var dxItem = control.SelectedItem;
                if (dxItem != null && dxItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem dxImageItem)
                {   // Je vybraná konkrétní položka?
                    var value = dxImageItem.Value;
                    bool isValidated = TryStoreValidatingValue(control, value, false, out var cancelInfo);
                    if (!isValidated)
                    {   // Že bych vrátil původní hodnotu?
                        // control.EditValue = cancelInfo.OriginalValue;
                    }
                }
            }
        }
        /// <summary>
        /// Eventhandler před pokusem o otevření Popup okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlQueryPopUp(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ImageComboBoxEdit control && TryGetPaintData(control, out var paintData))
            {
                if (paintData.LayoutItem.TryGetContent<WinDraw.Size>(DxDData.DxDataFormProperty.ComboPopupFormSize, out var size) && size.Width > 50 && size.Height > 50)
                    control.Properties.PopupFormSize = size;
            }
        }
        /// <summary>
        /// Eventhandler před otevřením Popup okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlBeforePopup(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;
        }
        /// <summary>
        /// Eventhandler po odchodu z prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _NativeControlLostFocus(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.ImageComboBoxEdit control && TryGetPaintData(control, out var paintData))
            {
                var size = control.GetPopupEditForm()?.Size;                   // Funguje   (na rozdíl od : control.Properties.PopupFormSize)
                if (size.HasValue && size.Value.Width > 50 && size.Value.Height > 50)
                    paintData.LayoutItem.SetContent<WinDraw.Size>(DxDData.DxDataFormProperty.ComboPopupFormSize, size.Value);
            }
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        /// <summary>
        /// Nativní control používaný pro kreslení
        /// </summary>
        DxeEdit.ImageComboBoxEdit __EditorPaint;
    }
    #endregion
    #region Button
    /// <summary>
    /// Button
    /// </summary>
    internal class DxRepositoryEditorButton : DxRepositoryEditor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="repositoryManager"></param>
        internal DxRepositoryEditorButton(DxRepositoryManager repositoryManager) : base(repositoryManager)
        {
        }
        /// <summary>
        /// Typ editoru
        /// </summary>
        internal override DxRepositoryEditorType EditorType { get { return DxRepositoryEditorType.Button; } }
        /// <summary>
        /// Režim práce s cache
        /// </summary>
        protected override EditorCacheMode CacheMode { get { return EditorCacheMode.ManagerCache; } }
        /// <summary>
        /// Pro daný prvek (jeho typ a obsah, a velikost) vygeneruje jednoznačný String klíč, popisující Bitmapu uloženou v Cache
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override string CreateKey(IPaintItemData paintData, WinDraw.Rectangle controlBounds)
        {
            string text = GetItemLabel(paintData);
            var iconName = this.GetItemContent(paintData, Data.DxDataFormProperty.IconName, "");
            WinDraw.FontStyle fontStyle = GetItemContent(paintData, Data.DxDataFormProperty.FontStyle, WinDraw.FontStyle.Regular);
            float sizeRatio = GetItemContent(paintData, Data.DxDataFormProperty.FontSizeRatio, 0f);
            WinDraw.Color fontColor = GetItemContent(paintData, Data.DxDataFormProperty.TextColor, WinDraw.Color.Empty);

            return CreateKey(text, controlBounds.Size, iconName, ToKey(fontStyle), ToKey(sizeRatio), ToKey(fontColor));
        }
        /// <summary>
        /// Pro daný prvek vygeneruje data jeho bitmapy
        /// </summary>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="pdea">Grafika pro kreslení</param>
        /// <param name="controlBounds">Cílové souřadnice</param>
        /// <returns></returns>
        protected override byte[] CreateImageData(IPaintItemData paintData, PaintDataEventArgs pdea, WinDraw.Rectangle controlBounds)
        {
            __EditorPaint ??= _CreateNativeControl(false, pdea.InteractivePanel);
            _FillNativeControl(null, paintData, __EditorPaint, controlBounds);
            return CreateBitmapData(__EditorPaint, pdea.Graphics);
        }
        /// <summary>
        /// Potomek zde vrátí nativní control
        /// </summary>
        /// <returns></returns>
        protected override WinForm.Control CreateNativeControl()
        {
            return _CreateNativeControl(true);
        }
        /// <summary>
        /// Potomek zde do controlu naplní všechna potřebná data.
        /// Je předán ten control, který byl vytvořen v metodě <see cref="CreateNativeControl()"/>.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected override void FillNativeControl(ControlDataPair dataPair, WinDraw.Rectangle controlBounds)
        {
            this._FillNativeControl(dataPair, dataPair.PaintData, dataPair.NativeControl as DxeEdit.SimpleButton, controlBounds);
        }
        /// <summary>
        /// Metoda vytvoří a vrátí new instanci nativního controlu. 
        /// Nastaví ji defaultní vzhled, nevkládá do ní hodnoty závislé na konkrétnbím prvku.
        /// Eventhandlery registruje pokud <paramref name="isInteractive"/> je true (hodnota false značí control vytvářený jen pro kreslení).
        /// </summary>
        /// <param name="isInteractive">Vytvořit interaktovní control: navázat do něj zdejší eventhandlery</param>
        /// <param name="parent">Parent control, do něhož je vytvořený Control vložen. Pouze pro EditorPaint control. Přebírá z něj barvu Background.</param>
        /// <returns></returns>
        private DxeEdit.SimpleButton _CreateNativeControl(bool isInteractive, WinForm.Control parent = null)
        {
            var control = new DxeEdit.SimpleButton() { Location = new WinDraw.Point(25, -200) };
            if (parent != null)
                parent.Controls.Add(control);
            if (isInteractive)
                _PrepareInteractive(control);
            return control;
        }
        /// <summary>
        /// Do dodaného controlu vloží data, která tento typ editoru řeší. Data získá z dodaného prvku.
        /// </summary>
        /// <param name="dataPair">Párová informace: data o buňce + nativní control</param>
        /// <param name="paintData">Data konkrétního prvku</param>
        /// <param name="control">Cílový control</param>
        /// <param name="controlBounds">Souřadnice v koordinátech Controlu, kde má být přítomen fyzický Control</param>
        protected void _FillNativeControl(ControlDataPair dataPair, IPaintItemData paintData, DxeEdit.SimpleButton control, WinDraw.Rectangle controlBounds)
        {
            control.Text = GetItemLabel(paintData);
            control.Size = controlBounds.Size;

            if (paintData.TryGetContent(DxDData.DxDataFormProperty.ButtonPaintStyle, out DxeCont.PaintStyles paintStyle))
                control.PaintStyle = paintStyle;

            var iconName = this.GetItemContent(paintData, Data.DxDataFormProperty.IconName, "");
            if (iconName != null)
            {
                DxComponent.ApplyImage(control.ImageOptions, iconName);
                control.ImageOptions.ImageToTextAlignment = DxeEdit.ImageAlignToText.LeftCenter;
            }
        }
        /// <summary>
        /// Daný control připraví pro interaktivní práci: nastaví potřebné vlastnosti a naváže zdejší eventhandlery, které potřebuje k práci.
        /// </summary>
        /// <param name="control"></param>
        private void _PrepareInteractive(DxeEdit.SimpleButton control)
        {
            control.Click += _ControlButtonClick;
        }
        /// <summary>
        /// Eventhandler po kliknutí na button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ControlButtonClick(object sender, EventArgs e)
        {
            if (SuppressNativeEvents) return;

            if (sender is DxeEdit.SimpleButton control && TryGetPaintData(control, out var paintData))
            {
                var actionInfo = new DataFormActionInfo(paintData as DxDData.DataFormCell, DxDData.DxDataFormAction.ButtonClick);
                this.DataForm.OnInteractiveAction(actionInfo);
            }
        }
        /// <summary>
        /// Dispose objektu
        /// </summary>
        protected override void OnDispose()
        {
            if (__EditorPaint != null)
            {
                __EditorPaint.Dispose();
                __EditorPaint = null;
            }
        }
        /// <summary>
        /// Nativní control používaný pro kreslení
        /// </summary>
        DxeEdit.SimpleButton __EditorPaint;
    }
    #endregion
}
