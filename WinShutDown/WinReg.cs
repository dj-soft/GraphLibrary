using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.IO;

namespace DjSoft.Support.WinShutDown
{
	/// <summary>
	/// Třída pro snadný přístup k Windows registru, pro ověřování instalačních informací a ukládání konfigurací
	/// </summary>
	public class WinReg
	{
		#region Public metody
        #region Exists(value), Exists(Folder)
        /// <summary>
        /// Detekuje, zda existuje klíč daného jména. Vrací true = klíč existuje / false = klíč neexistuje.
        /// </summary>
        /// <param name="folder">Složka</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <returns>Hodnota true = klíč existuje / false = klíč neexistuje</returns>
        public static bool ValueExists(WinRegFolder folder, string keyName)
        {
            return _ValueExists(folder, keyName);
        }
        /// <summary>
        /// Detekuje, zda existuje podsložka daného jména. Vrací true = podsložka existuje / false = podsložka neexistuje.
        /// </summary>
        /// <param name="folder">Složka, v jejímž rámci testuji.</param>
        /// <param name="subKeyName">Název podsložky, kterou testuji. Musí být zadán</param>
        /// <returns>Hodnota true = klíč existuje / false = klíč neexistuje</returns>
        public static bool SubKeyExists(WinRegFolder folder, string subKeyName)
        {
            return _SubKeyExists(folder, subKeyName);
        }
        #endregion
        #region Read Value Names, Read SubKey Names
        /// <summary>
        /// Načte soupis datových klíčů z dané složky Win registru.
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <returns>Seznam názvů hodnot v dané složce</returns>
        public static List<string> GetValueNames(WinRegFolder folder)
        {
            return _GetValueNames(folder);
        }
        /// <summary>
        /// Načte soupis podsložek z dané složky Win registru.
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <returns>Seznam názvů podsložek v dané složce</returns>
        public static List<string> GetSubKeyNames(WinRegFolder folder)
        {
            return _GetSubKeyNames(folder);
        }
        #endregion
		#region Read & Write string plain
		/// <summary>
		/// Načte řetězec z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Nalezený string / Default hodnota (null)</returns>
        public static string ReadString(WinRegFolder folder, string keyName)
		{
            return _ReadString(folder, keyName, null);
		}
		/// <summary>
		/// Načte řetězec z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezený string / Default hodnota</returns>
        public static string ReadString(WinRegFolder folder, string keyName, string defValue)
		{
            return _ReadString(folder, keyName, defValue);
		}
        /// <summary>
        /// Načte řetězec z Windows registru
        /// </summary>
        /// <param name="register">Otevřený klíč</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <returns>Nalezený string / Default hodnota (null)</returns>
        public static string ReadString(RegistryKey register, string keyName)
        {
            return _ReadString(register, keyName, null);
        }
        /// <summary>
        /// Načte řetězec z Windows registru
        /// </summary>
        /// <param name="register">Otevřený klíč</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <param name="defValue">Default hodnota</param>
        /// <returns>Nalezený string / Default hodnota</returns>
        public static string ReadString(RegistryKey register, string keyName, string defValue)
        {
            return _ReadString(register, keyName, defValue);
        }
        /// <summary>
		/// Do registru zapíše hodnotu typu string.
		/// </summary>
		/// <param name="folder">Složka, může být ""</param>
		/// <param name="keyName">Název hodnoty</param>
		/// <param name="value">Hodnota, může být i null.</param>
        public static void WriteString(WinRegFolder folder, string keyName, string value)
		{
			_CheckValueNotNull(value, keyName);
			_WriteValue(folder, keyName, value, RegistryValueKind.String);
		}
        /// <summary>
        /// Do registru zapíše hodnotu typu string.
        /// </summary>
        /// <param name="register">Otevřený klíč</param>
        /// <param name="keyName">Název hodnoty</param>
        /// <param name="value">Hodnota, může být i null.</param>
        public static void WriteString(RegistryKey register, string keyName, string value)
        {
            _CheckValueNotNull(value, keyName);
            _WriteValue(register, keyName, value, RegistryValueKind.String);
        }
        #endregion
        #region Read & Write string crypted
        /// <summary>
		/// Načte řetězec z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Nalezený string / Default hodnota (null)</returns>
        public static string ReadStringCrypt(WinRegFolder folder, string keyName)
		{
            return _ReadStringCrypt(folder, keyName, null);
		}
		/// <summary>
		/// Načte řetězec z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezený string / Default hodnota</returns>
        public static string ReadStringCrypt(WinRegFolder folder, string keyName, string defValue)
		{
            return _ReadStringCrypt(folder, keyName, defValue);
		}
        /// <summary>
        /// Načte řetězec z Windows registru
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <returns>Nalezený string / Default hodnota (null)</returns>
        public static string ReadStringCrypt(RegistryKey register, string keyName)
        {
            return _ReadStringCrypt(register, keyName, null);
        }
        /// <summary>
        /// Načte řetězec z Windows registru
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <param name="defValue">Default hodnota</param>
        /// <returns>Nalezený string / Default hodnota</returns>
        public static string ReadStringCrypt(RegistryKey register, string keyName, string defValue)
        {
            return _ReadStringCrypt(register, keyName, defValue);
        }
        /// <summary>
		/// Do registru zapíše hodnotu typu string, zakryptovanou.
		/// </summary>
		/// <param name="folder">Složka, může být ""</param>
		/// <param name="keyName">Název hodnoty</param>
		/// <param name="value">Hodnota, může být i null.</param>
        public static void WriteStringCrypt(WinRegFolder folder, string keyName, string value)
		{
			_CheckValueNotNull(value, keyName);
			byte[] crypt = Crypt.Encrypt(value);
			_WriteValue(folder, keyName, crypt, RegistryValueKind.Binary);
		}
        /// <summary>
        /// Do registru zapíše hodnotu typu string, zakryptovanou.
        /// </summary>
        /// <param name="folder">Složka, může být ""</param>
        /// <param name="keyName">Název hodnoty</param>
        /// <param name="value">Hodnota, může být i null.</param>
        public static void WriteStringCrypt(RegistryKey register, string keyName, string value)
        {
            _CheckValueNotNull(value, keyName);
            byte[] crypt = Crypt.Encrypt(value);
            _WriteValue(register, keyName, crypt, RegistryValueKind.Binary);
        }
        #endregion
        #region Read & Write Boolean
        /// <summary>
		/// Načte hodnotu Bool z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Nalezená hodnota bool / Default hodnota (false)</returns>
        public static bool ReadBool(WinRegFolder folder, string keyName)
		{
            return ReadBool(folder, keyName, false);
		}
		/// <summary>
		/// Načte hodnotu Bool z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezená hodnota bool / Default hodnota (false)</returns>
        public static bool ReadBool(WinRegFolder folder, string keyName, bool defValue)
		{
            int saved = _ReadInt32(folder, keyName, -1);
			return (saved == -1 ? defValue : (saved != 0));
		}
        /// <summary>
        /// Načte hodnotu Bool z Windows registru
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <returns>Nalezená hodnota bool / Default hodnota (false)</returns>
        public static bool ReadBool(RegistryKey register, string keyName)
        {
            return ReadBool(register, keyName, false);
        }
        /// <summary>
        /// Načte hodnotu Bool z Windows registru
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <param name="defValue">Default hodnota</param>
        /// <returns>Nalezená hodnota bool / Default hodnota (false)</returns>
        public static bool ReadBool(RegistryKey register, string keyName, bool defValue)
        {
            int saved = _ReadInt32(register, keyName, -1);
            return (saved == -1 ? defValue : (saved != 0));
        }
        /// <summary>
		/// Do registru zapíše hodnotu typu Int32.
		/// </summary>
		/// <param name="folder">Složka, může být ""</param>
		/// <param name="keyName">Název hodnoty</param>
		/// <param name="value">Hodnota.</param>
        public static void WriteBool(WinRegFolder folder, string keyName, bool value)
		{
			int saved = (value ? 1 : 0);                                 // Ukládáme hodnotu Int32: false = 0; true = 1;
			_WriteValue(folder, keyName, saved, RegistryValueKind.DWord);
		}
        /// <summary>
        /// Do registru zapíše hodnotu typu Int32.
        /// </summary>
        /// <param name="folder">Složka, může být ""</param>
        /// <param name="keyName">Název hodnoty</param>
        /// <param name="value">Hodnota.</param>
        public static void WriteBool(RegistryKey register, string keyName, bool value)
        {
            int saved = (value ? 1 : 0);                                 // Ukládáme hodnotu Int32: false = 0; true = 1;
            _WriteValue(register, keyName, saved, RegistryValueKind.DWord);
        }
        #endregion
        #region Read & Write Integer
        /// <summary>
		/// Načte číslo Int32 z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Nalezené číslo / Default hodnota (0)</returns>
        public static int ReadInt32(WinRegFolder folder, string keyName)
		{
            return _ReadInt32(folder, keyName, 0);
		}
		/// <summary>
		/// Načte číslo Int32 z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezený string / Default hodnota</returns>
        public static int ReadInt32(WinRegFolder folder, string keyName, int defValue)
		{
            return _ReadInt32(folder, keyName, defValue);
		}
        /// <summary>
        /// Načte číslo Int32 z Windows registru
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <returns>Nalezené číslo / Default hodnota (0)</returns>
        public static int ReadInt32(RegistryKey register, string keyName)
        {
            return _ReadInt32(register, keyName, 0);
        }
        /// <summary>
        /// Načte číslo Int32 z Windows registru
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <param name="defValue">Default hodnota</param>
        /// <returns>Nalezený string / Default hodnota</returns>
        public static int ReadInt32(RegistryKey register, string keyName, int defValue)
        {
            return _ReadInt32(register, keyName, defValue);
        }
        /// <summary>
		/// Do registru zapíše hodnotu typu Int32.
		/// </summary>
		/// <param name="folder">Složka, může být ""</param>
		/// <param name="keyName">Název hodnoty</param>
		/// <param name="value">Hodnota.</param>
        public static void WriteInt32(WinRegFolder folder, string keyName, int value)
		{
			_WriteValue(folder, keyName, value, RegistryValueKind.DWord);
		}
        /// <summary>
        /// Do registru zapíše hodnotu typu Int32.
        /// </summary>
        /// <param name="folder">Složka, může být ""</param>
        /// <param name="keyName">Název hodnoty</param>
        /// <param name="value">Hodnota.</param>
        public static void WriteInt32(RegistryKey register, string keyName, int value)
        {
            _WriteValue(register, keyName, value, RegistryValueKind.DWord);
        }
        #endregion
        #region Read & Write Long
        /// <summary>
		/// Načte číslo Int64 z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Nalezené číslo / Default hodnota (0)</returns>
        public static long ReadInt64(WinRegFolder folder, string keyName)
		{
            return _ReadInt64(folder, keyName, 0);
		}
		/// <summary>
		/// Načte číslo Int64 z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezené číslo / Default hodnota</returns>
        public static long ReadInt64(WinRegFolder folder, string keyName, long defValue)
		{
            return _ReadInt64(folder, keyName, defValue);
		}
        /// <summary>
        /// Načte číslo Int64 z Windows registru
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <returns>Nalezené číslo / Default hodnota (0)</returns>
        public static long ReadInt64(RegistryKey register, string keyName)
        {
            return _ReadInt64(register, keyName, 0);
        }
        /// <summary>
        /// Načte číslo Int64 z Windows registru
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <param name="defValue">Default hodnota</param>
        /// <returns>Nalezené číslo / Default hodnota</returns>
        public static long ReadInt64(RegistryKey register, string keyName, long defValue)
        {
            return _ReadInt64(register, keyName, defValue);
        }
        /// <summary>
		/// Do registru zapíše hodnotu typu Int64.
		/// </summary>
		/// <param name="folder">Složka, může být ""</param>
		/// <param name="keyName">Název hodnoty</param>
		/// <param name="value">Hodnota.</param>
        public static void WriteInt32(WinRegFolder folder, string keyName, long value)
		{
            _WriteValue(folder, keyName, value, RegistryValueKind.QWord);
		}
        /// <summary>
        /// Do registru zapíše hodnotu typu Int64.
        /// </summary>
        /// <param name="folder">Složka, může být ""</param>
        /// <param name="keyName">Název hodnoty</param>
        /// <param name="value">Hodnota.</param>
        public static void WriteInt32(RegistryKey register, string keyName, long value)
        {
            _WriteValue(register, keyName, value, RegistryValueKind.QWord);
        }
        #endregion
        #region Read & Write Binary
        /// <summary>
		/// Načte binární data z Windows registru do pole byte[]
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Nalezené číslo / Default hodnota (0)</returns>
        public static byte[] ReadBinary(WinRegFolder folder, string keyName)
		{
            return _ReadBinary(folder, keyName, null);
		}
		/// <summary>
		/// Načte binární data z Windows registru do pole byte[]
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezené číslo / Default hodnota</returns>
        public static byte[] ReadBinary(WinRegFolder folder, string keyName, byte[] defValue)
		{
			return _ReadBinary(folder, keyName, defValue);
		}
        /// <summary>
        /// Načte binární data z Windows registru do pole byte[]
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <returns>Nalezené číslo / Default hodnota (0)</returns>
        public static byte[] ReadBinary(RegistryKey register, string keyName)
        {
            return _ReadBinary(register, keyName, null);
        }
        /// <summary>
        /// Načte binární data z Windows registru do pole byte[]
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <param name="defValue">Default hodnota</param>
        /// <returns>Nalezené číslo / Default hodnota</returns>
        public static byte[] ReadBinary(RegistryKey register, string keyName, byte[] defValue)
        {
            return _ReadBinary(register, keyName, defValue);
        }
        /// <summary>
		/// Do registru zapíše binární data z pole byte[]
		/// </summary>
		/// <param name="folder">Složka, může být ""</param>
		/// <param name="keyName">Název hodnoty</param>
		/// <param name="value">Hodnota, pole byte[].</param>
        public static void WriteBinary(WinRegFolder folder, string keyName, byte[] value)
		{
			_WriteValue(folder, keyName, value, RegistryValueKind.Binary);
		}
        /// <summary>
        /// Do registru zapíše binární data z pole byte[]
        /// </summary>
        /// <param name="folder">Složka, může být ""</param>
        /// <param name="keyName">Název hodnoty</param>
        /// <param name="value">Hodnota, pole byte[].</param>
        public static void WriteBinary(RegistryKey register, string keyName, byte[] value)
        {
            _WriteValue(register, keyName, value, RegistryValueKind.Binary);
        }
        #endregion
		#region Delete SubKey, Delete Value
		/// <summary>
		/// Vymaže složku z Windows registru
		/// </summary>
		/// <param name="folder">Složka, v níž bude mazání probíhat (tedy ne ta, kterou chceme mazat)</param>
		/// <param name="subKeyName">Jméno podsložky, kterou budeme mazat</param>
        public static void DeleteSubKey(WinRegFolder folder, string subKeyName)
		{
            _DeleteSubKey(folder, subKeyName);
		}
        /// <summary>
        /// Vymaže složku z Windows registru
        /// </summary>
        /// <param name="folder">Složka, v níž bude mazání probíhat (tedy ne ta, kterou chceme mazat)</param>
        /// <param name="subKeyName">Jméno podsložky, kterou budeme mazat</param>
        public static void DeleteSubKey(RegistryKey register, string subKeyName)
        {
            _DeleteSubKey(register, subKeyName);
        }
        /// <summary>
        /// Vymaže hodnotu z Windows registru
        /// </summary>
        /// <param name="folder">Složka</param>
        /// <param name="keyName">Jméno hodnoty</param>
        public static void DeleteValue(WinRegFolder folder, string keyName)
        {
            _DeleteValue(folder, keyName);
        }
        /// <summary>
        /// Vymaže hodnotu z Windows registru
        /// </summary>
        /// <param name="folder">Složka</param>
        /// <param name="keyName">Jméno hodnoty</param>
        public static void DeleteValue(RegistryKey register, string keyName)
        {
            _DeleteValue(register, keyName);
        }
        #endregion
        #region OpenKey, CurrentSystemRegistryView
        /// <summary>
        /// Otevře a vrátí daný klíč registru. Pro čtení (nebo při zadání druhého parametru forWrite = true) i pro zápis.
        /// Volající má daný klíč použív v using() patternu, aby byl spolehlivě zavřen.
        /// Volající může z klíče číst nebo psát buď přímo, nebo pomocí zdejších metod.
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static RegistryKey OpenKey(WinRegFolder folder)
        {
            return _OpenKey(folder, false);
        }
        /// <summary>
        /// Otevře a vrátí daný klíč registru. Pro čtení nebo pro zápis.
        /// Volající má daný klíč použív v using() patternu, aby byl spolehlivě zavřen.
        /// Volající může z klíče číst nebo psát buď přímo, nebo pomocí zdejších metod.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="forWrite"></param>
        /// <returns></returns>
        public static RegistryKey OpenKey(WinRegFolder folder, bool forWrite)
        {
            return _OpenKey(folder, forWrite);
        }
        /// <summary>
        /// Aktuální RegistryView odvozené od verze operačního systému (Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32)
        /// </summary>
        public static RegistryView CurrentSystemRegistryView { get { return (Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32); } }
        #endregion
        #endregion
        #region privátní fyzická práce s registrem - správa klíčů (open, create)
        /// <summary>
		/// Otevře aktuální segment registru, základnovou cestu (není součástí parametru "folder") a v něm najde a otevře danou podsložku.
		/// Pokud není podsložka zadaná, vrací aplikační root.
		/// Pokud je složka zadaná a neexistuje, bude vytvořena a pak vrácena.
		/// Funguje i víceúrovňově, tj. dokáže zpracovat i vnořené cesty.
		/// </summary>
		/// <param name="folder">Název složky registru. Pokud neexistuje, bude vytvořena.</param>
		/// <param name="forWrite">Požadavek na otevření (true) pro čtení i zápis  /  false = jen pro čtení. Týká se jen poslední úrovně SubKeys.</param>
		/// <returns>RegistryKey odpovídající požadovanému názvu. Pokud neexistuje, bude vytvořena.</returns>
        private static RegistryKey _OpenKey(WinRegFolder folder, bool forWrite)
		{
            folder.CheckKeyName();

            RegistryKey regRoot = Microsoft.Win32.RegistryKey.OpenBaseKey(folder.Hive, folder.View);     // například: HKEY_LOCAL_MACHINE[Registry64]
            RegistryKey regPrev = regRoot;                 // Předešlá otevřená úroveň registru, například:           "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Office\"       (tato úroveň existuje, v ní existuje i následující úroveň)
            RegistryKey regOpen = regRoot;                 // Aktuálně zpracovávaná úroveň registru, například:       "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Office\15.0\"  (tato úroveň existuje, testujeme v ní existenci další úrovně)
            string fldOpen = null;                         // Aktuálně zpracovávaná složka, název, například:         "15.0"     (název nodu otevřeného v regOpen)

            // Rozdělení dané cesty na jednotlivé složky ("SOFTWARE\Microsoft\Office\15.0\InstallRoot" => "SOFTWARE", "Microsoft", "Office", "15.0", "InstallRoot"):
            List<string> folders = new List<string>(folder.Folder.Split('\\'));

            // Otevřeme / vytvoříme postupně každou úroveň, přičemž parametr "forWrite" aplikujeme jen na poslední úroveň:
            int last = folders.Count - 1;
            for (int i = 0; i <= last; i++)
            {
                if (regOpen == null)
                    break;

                string fldNext = folders[i];               // Postupně názvy nodů (nikoli celá cesta), tedy pro výše uvedený příklad postupně texty: "SOFTWARE", "Microsoft", "Office", "15.0", "InstallRoot", ...
                if (fldNext.Trim().Length == 0) continue;  // Některé zápisy klíčů obsahují prázdnou složku ("SOFTWARE\Microsoft\Office\\Install"), prázdné složky ignorujeme
                bool isLastItem = (i == last);
                bool isForWrite = (isLastItem && forWrite);                // Vstupní požadavek "forWrite" je platný jen na poslední úroveň ("InstallRoot").

                bool existsItem = _SubKeyExistInKey(regOpen, fldNext);     // Ověří, zda node, který budeme otevírat, existuje
                if (!existsItem)
                {   // Pokud neexistuje určitá složka:
                    if (!forWrite)
                    {   // A pokud nemáme složku otevírat pro zápis, pak vrátíme null (tj. ze složky chceme něco číst, a složka neexistuje => nemá cenu ji vytvářet):
                        regOpen = null;
                        break;
                    }

                    // Složka neexistuje, a my ji máme otevřít pro zápis? Následující složku (fldNext) vytvoříme tak, 
                    //  že v rámci předešlého klíče (regPrev) znovu otevřeme náš node (fldOpen), ale nyní s možností zápisu = abychom do něj mohli potřebnou složku vytvořit:
                    try
                    {
                        using (RegistryKey wriOpen = _OpenKeyWst(folder, regPrev, fldOpen))
                        {
                            wriOpen.CreateSubKey(fldNext);
                            wriOpen.Flush();
                            wriOpen.Close();
                        }
                    }
                    catch (System.Security.SecurityException exc)
                    {
                        _ThrowSysError("Nemáte oprávnění pro tvorbu registrů.", exc);
                    }
                    catch (System.UnauthorizedAccessException exc)
                    {
                        _ThrowSysError("Nemáte oprávnění pro přístup k registrům.", exc);
                    }
                    catch (System.Exception exc)
                    {
                        _ThrowSysError("Chyba při tvorbě záznamu v registru.", exc);
                    }
                }
                regPrev = regOpen;                         // do regPrev dám aktuální klíč (v příštím kole bude "minulý"), následně do aktuálního (regOpen) otevřu klíč s názvem "fldNext":

                RegistryKeyPermissionCheck permission = (isForWrite ? RegistryKeyPermissionCheck.ReadWriteSubTree : RegistryKeyPermissionCheck.ReadSubTree);
                regOpen = regOpen.OpenSubKey(fldNext, permission);
                fldOpen = fldNext;                         // Zapamatuji si název klíče, který jsem otevřel
            }

            return regOpen;
		}
        /// <summary>
        /// Otevře klíč registru s možností ReadWriteSubTree.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="regPrev"></param>
        /// <param name="fldOpen"></param>
        /// <returns></returns>
        private static RegistryKey _OpenKeyWst(WinRegFolder folder, RegistryKey regPrev, string fldOpen)
        {
            // Standardní cesta, kdy máme v proměnné (regPrev) otevřen klíč například "HKLM\SOFTWARE\Microsoft\Office\Word" 
            //  a v něm máme otevřít pro zápisy další existující klíč (fldOpen) = "Addins":
            if (regPrev != null && fldOpen != null)
                return regPrev.OpenSubKey(fldOpen, RegistryKeyPermissionCheck.ReadWriteSubTree);

            // Mimořádná cesta, když máme otevřít pro zápis root klíč (což možná ani nepůjde?):
            // tj. pokud nemáme zadáno fldOpen, pak musíme pro zápis otevřít root hive:
            return Microsoft.Win32.RegistryKey.OpenBaseKey(folder.Hive, folder.View);     // například: HKEY_LOCAL_MACHINE[Registry64]
        }
		/// <summary>
		/// Ověří správnost hodnoty před zápisem do registru.
		/// </summary>
		/// <param name="value">Zapisovaná hodnota</param>
		/// <param name="keyName">Jméno klíče pro chybovou hlášku</param>
		private static void _CheckValueNotNull(object value, string keyName)
		{
			if (value == null)
				_ThrowSysError("Chybně zadaná hodnota string pro zápis do registru (" + keyName + " = null).");
		}
		/// <summary>
		/// Zjistí, zda existuje složka daného jména (funguje jednoúrovňově !)
		/// Názvy složek nejsou case-sensitive, porovnává se Lower()
		/// </summary>
		/// <param name="regKey">Registr, současná otevřená úroveň</param>
		/// <param name="folder">Hledaná složka</param>
		/// <returns>true = existuje   /   false = neexistuje</returns>
		private static bool _SubKeyExistInKey(RegistryKey regKey, string folder)
		{
			List<string> subKeyNames = _GetSubKeyNames(regKey);
            return subKeyNames.Any(n => String.Equals(n, folder, StringComparison.InvariantCultureIgnoreCase));
		}
		/// <summary>
		/// Zjistí, zda v dané úrovni registru existuje hodnota daného jména.
		/// Názvy hodnot nejsou case-sensitive, porovnává se Lower()
		/// </summary>
		/// <param name="regKey">Registr, současná otevřená úroveň</param>
		/// <param name="value">Hledaná hodnota</param>
		/// <returns>true = existuje   /   false = neexistuje</returns>
		private static bool _ValueExistInKey(RegistryKey regKey, string value)
		{
			List<string> valueNames = _GetValueNames(regKey) ; // , true);
			return valueNames.Any(n => String.Equals(n, value, StringComparison.InvariantCultureIgnoreCase));
		}
        /// <summary>
        /// Vymaže složku z Windows registru
        /// Privátní VÝKONNÁ metoda.
        /// </summary>
        /// <param name="folder">Složka, v níž bude mazání probíhat (tedy ne ta, kterou chceme mazat)</param>
        /// <param name="subKeyName">Jméno podsložky, kterou budeme mazat</param>
        private static void _DeleteSubKey(WinRegFolder folder, string subKeyName)
        {
            using (RegistryKey regKey = _OpenKey(folder, true))
            {
                _DeleteSubKey(regKey, subKeyName);
            }
        }
        /// <summary>
        /// Vymaže složku z Windows registru
        /// Privátní VÝKONNÁ metoda.
        /// </summary>
        /// <param name="folder">Složka, v níž bude mazání probíhat (tedy ne ta, kterou chceme mazat)</param>
        /// <param name="subKeyName">Jméno podsložky, kterou budeme mazat</param>
        private static void _DeleteSubKey(RegistryKey regKey, string subKeyName)
        {
            if (regKey != null)
                regKey.DeleteSubKeyTree(subKeyName);
        }
        #endregion
        #region privátní fyzická práce s registrem - správa hodnot (read, write, delete)
        /// <summary>
		/// Detekuje, zda existuje klíč daného jména. Vrací true = klíč existuje / false = klíč neexistuje.
		/// Privátní výkonná metoda.
		/// </summary>
		/// <param name="folder">Složka</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Hodnota true = klíč existuje / false = klíč neexistuje</returns>
        private static bool _ValueExists(WinRegFolder folder, string keyName)
		{
			bool exists = false;
            using (RegistryKey regKey = _OpenKey(folder, false))
			{
                exists = _ValueExists(regKey, keyName);
			}
			return exists;
		}
        /// <summary>
        /// Detekuje, zda existuje klíč daného jména. Vrací true = klíč existuje / false = klíč neexistuje.
        /// Privátní výkonná metoda.
        /// </summary>
        /// <param name="folder">Složka</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <returns>Hodnota true = klíč existuje / false = klíč neexistuje</returns>
        private static bool _ValueExists(RegistryKey regKey, string keyName)
        {
            bool exists = false;
            if (regKey != null)
				exists = _ValueExistInKey(regKey, keyName);
            return exists;
        }
        /// <summary>
		/// Detekuje, zda existuje podsložka daného jména. Vrací true = podsložka existuje / false = podsložka neexistuje.
		/// </summary>
		/// <param name="folder">Složka, v jejímž rámci testuji.</param>
		/// <param name="subKeyName">Název podsložky, kterou testuji. Musí být zadán</param>
		/// <returns>Hodnota true = klíč existuje / false = klíč neexistuje</returns>
        private static bool _SubKeyExists(WinRegFolder folder, string subKeyName)
		{
			bool exists = false;
            using (RegistryKey regKey = _OpenKey(folder, false))
			{
                exists = _SubKeyExists(regKey, subKeyName);
			}
			return exists;
		}
        /// <summary>
        /// Detekuje, zda existuje podsložka daného jména. Vrací true = podsložka existuje / false = podsložka neexistuje.
        /// </summary>
        /// <param name="folder">Složka, v jejímž rámci testuji.</param>
        /// <param name="subKeyName">Název podsložky, kterou testuji. Musí být zadán</param>
        /// <returns>Hodnota true = klíč existuje / false = klíč neexistuje</returns>
        private static bool _SubKeyExists(RegistryKey regKey, string subKeyName)
        {
            bool exists = false;
            if (regKey != null)
                exists = _SubKeyExistInKey(regKey, subKeyName);
            return exists;
        }
        /// <summary>
		/// Načte soupis datových klíčů z dané složky Win registru.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <returns>Seznam názvů hodnot v dané složce</returns>
        private static List<string> _GetValueNames(WinRegFolder folder)
		{
			List<string> result = new List<string>();
            using (RegistryKey regKey = _OpenKey(folder, false))
			{
                result.AddRange(_GetValueNames(regKey));
			}
			return result;
		}
        /// <summary>
        /// Načte soupis datových klíčů z dané složky Win registru.
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <returns>Seznam názvů hodnot v dané složce</returns>
        private static List<string> _GetValueNames(RegistryKey regKey)
        {
            List<string> result = new List<string>();
            if (regKey != null)
                result.AddRange(regKey.GetValueNames());
            return result;
        }
        /// <summary>
		/// Načte soupis podsložek z dané složky Win registru.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <returns>Seznam názvů podsložek v dané složce</returns>
        private static List<string> _GetSubKeyNames(WinRegFolder folder)
		{
			List<string> result = new List<string>();
            using (RegistryKey regKey = _OpenKey(folder, false))
			{
                result.AddRange(_GetSubKeyNames(regKey));
			}
			return result;
		}
        /// <summary>
        /// Načte soupis podsložek z dané složky Win registru.
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <returns>Seznam názvů podsložek v dané složce</returns>
        private static List<string> _GetSubKeyNames(RegistryKey regKey)
        {
            List<string> result = new List<string>();
            if (regKey != null)
                result.AddRange(regKey.GetSubKeyNames());
            return result;
        }
        /// <summary>
		/// Načte řetězec z Windows registru.
		/// Privátní výkonná metoda.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezený string / Default hodnota</returns>
        private static string _ReadString(WinRegFolder folder, string keyName, string defValue)
		{
			string result = defValue;
            using (RegistryKey regKey = _OpenKey(folder, false))
			{
                result = _ReadString(regKey, keyName, defValue);
			}
			return result;
		}
		/// <summary>
		/// Načte řetězec z Windows registru.
		/// Privátní výkonná metoda.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezený string / Default hodnota</returns>
        private static string _ReadString(RegistryKey regKey, string keyName, string defValue)
		{
			string result = defValue;
            if (regKey != null)
            {
                if (_ValueExistInKey(regKey, keyName))
                {
                    RegistryValueKind valueKind = regKey.GetValueKind(keyName);
                    if (valueKind == RegistryValueKind.String)
                        result = (string)regKey.GetValue(keyName, defValue, RegistryValueOptions.None);
                    if (valueKind == RegistryValueKind.ExpandString)
                        result = (string)regKey.GetValue(keyName, defValue, RegistryValueOptions.None);
                }
            }
			return result;
		}
		/// <summary>
		/// Načte zakryptovaný řetězec z Windows registru.
		/// Privátní výkonná metoda.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezený string / Default hodnota</returns>
        private static string _ReadStringCrypt(WinRegFolder folder, string keyName, string defValue)
		{
			string result = defValue;
            using (RegistryKey regKey = _OpenKey(folder, false))
			{
                result = _ReadStringCrypt(regKey, keyName, defValue);
			}
			return result;
		}
        /// <summary>
        /// Načte zakryptovaný řetězec z Windows registru.
        /// Privátní výkonná metoda.
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <param name="defValue">Default hodnota</param>
        /// <returns>Nalezený string / Default hodnota</returns>
        private static string _ReadStringCrypt(RegistryKey regKey, string keyName, string defValue)
        {
            string result = defValue;
            if (regKey != null)
            {
                if (_ValueExistInKey(regKey, keyName))
                {
                    RegistryValueKind valueKind = regKey.GetValueKind(keyName);
                    if (valueKind == RegistryValueKind.Binary)
                    {
                        byte[] buffer = (byte[])regKey.GetValue(keyName, null);
                        if (buffer != null)
                            result = Crypt.Decrypt(buffer);
                    }
                }
            }
            return result;
        }
		/// <summary>
		/// Načte číslo Int32 z Windows registru.
		/// Privátní výkonná metoda.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezený string / Default hodnota</returns>
        private static int _ReadInt32(WinRegFolder folder, string keyName, int defValue)
		{
			int result = defValue;
            using (RegistryKey regKey = _OpenKey(folder, false))
			{
                result = _ReadInt32(regKey, keyName, defValue);
			}
			return result;
		}
        /// <summary>
        /// Načte číslo Int32 z Windows registru.
        /// Privátní výkonná metoda.
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <param name="defValue">Default hodnota</param>
        /// <returns>Nalezený string / Default hodnota</returns>
        private static int _ReadInt32(RegistryKey regKey, string keyName, int defValue)
        {
            int result = defValue;
            if (regKey != null)
            {
                if (_ValueExistInKey(regKey, keyName))
                {
                    RegistryValueKind valueKind = regKey.GetValueKind(keyName);
                    if (valueKind == RegistryValueKind.DWord)
                        result = (int)regKey.GetValue(keyName, defValue);
                }
            }
            return result;
        }
        /// <summary>
		/// Načte číslo Int64 z Windows registru.
		/// Privátní výkonná metoda.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezené číslo / Default hodnota</returns>
        private static long _ReadInt64(WinRegFolder folder, string keyName, long defValue)
		{
			long result = defValue;
            using (RegistryKey regKey = _OpenKey(folder, false))
			{
                result = _ReadInt64(regKey, keyName, defValue);
			}
			return result;
		}
        /// <summary>
        /// Načte číslo Int64 z Windows registru.
        /// Privátní výkonná metoda.
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <param name="defValue">Default hodnota</param>
        /// <returns>Nalezené číslo / Default hodnota</returns>
        private static long _ReadInt64(RegistryKey regKey, string keyName, long defValue)
        {
            long result = defValue;
            if (regKey != null)
            {
                if (_ValueExistInKey(regKey, keyName))
                {
                    RegistryValueKind valueKind = regKey.GetValueKind(keyName);
                    if (valueKind == RegistryValueKind.QWord)
                        result = (long)regKey.GetValue(keyName, defValue);
                }
            }
            return result;
        }
        /// <summary>
		/// Načte binární data z Windows registru do pole byte[].
		/// Privátní výkonná metoda.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezené číslo / Default hodnota</returns>
        private static byte[] _ReadBinary(WinRegFolder folder, string keyName, byte[] defValue)
		{
			byte[] result = defValue;
            using (RegistryKey regKey = _OpenKey(folder, false))
			{
                result = _ReadBinary(regKey, keyName, defValue);
			}
			return result;
		}
        /// <summary>
        /// Načte binární data z Windows registru do pole byte[].
        /// Privátní výkonná metoda.
        /// </summary>
        /// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
        /// <param name="keyName">Název klíče, musí být zadán</param>
        /// <param name="defValue">Default hodnota</param>
        /// <returns>Nalezené číslo / Default hodnota</returns>
        private static byte[] _ReadBinary(RegistryKey regKey, string keyName, byte[] defValue)
        {
            byte[] result = defValue;
            if (regKey != null)
            {
                if (_ValueExistInKey(regKey, keyName))
                {
                    RegistryValueKind valueKind = regKey.GetValueKind(keyName);
                    if (valueKind == RegistryValueKind.Binary)
                        result = (byte[])regKey.GetValue(keyName, defValue);
                }
            }
            return result;
        }
        /// <summary>
		/// Do registru vloží danou hodnotu.
		/// </summary>
		/// <param name="folder">Složka pro ukládání (v rámci aplikačního rootu, tedy nezadávat absolutní cestu)</param>
		/// <param name="keyName">Jméno hodnoty</param>
		/// <param name="value">Hodnota, měla by odpovídat typu (kind)</param>
		/// <param name="kind"></param>
        private static void _WriteValue(WinRegFolder folder, string keyName, object value, RegistryValueKind kind)
		{
            using (RegistryKey regKey = _OpenKey(folder, true))
			{
                if (regKey != null)
                {
                    _WriteValue(regKey, keyName, value, kind);
                    regKey.Flush();
                    regKey.Close();
                }
                else
                    _ThrowSysError("Chyba při zápisu do registru, do složky " + folder.ToString() + " není možno zapisovat.");
            }
		}
        /// <summary>
        /// Do registru vloží danou hodnotu.
        /// </summary>
        /// <param name="folder">Složka pro ukládání (v rámci aplikačního rootu, tedy nezadávat absolutní cestu)</param>
        /// <param name="keyName">Jméno hodnoty</param>
        /// <param name="value">Hodnota, měla by odpovídat typu (kind)</param>
        /// <param name="kind"></param>
        private static void _WriteValue(RegistryKey regKey, string keyName, object value, RegistryValueKind kind)
        {
            if (regKey != null)
            {
                try
                {
                    regKey.SetValue(keyName, value, kind);
                }
                catch (System.Security.SecurityException exc)
                {
                    _ThrowSysError("Nemáte oprávnění pro tvorbu registrů.", exc);
                }
                catch (System.UnauthorizedAccessException exc)
                {
                    _ThrowSysError("Nemáte oprávnění pro přístup k registrům.", exc);
                }
                catch (System.Exception exc)
                {
                    _ThrowSysError("Chyba při tvorbě záznamu v registru.", exc);
                }
            }
            else
            {
                _ThrowSysError("Chyba při zápisu do registru, klíč je NULL.");
            }
        }
        /// <summary>
        /// Vymaže hodnotu z Windows registru.
        /// Privátní VÝKONNÁ metoda.
        /// </summary>
        /// <param name="folder">Složka</param>
        /// <param name="keyName">Jméno hodnoty</param>
        private static void _DeleteValue(WinRegFolder folder, string keyName)
        {
            using (RegistryKey regKey = _OpenKey(folder, true))
            {
                if (regKey != null)
                    _DeleteValue(regKey, keyName);
                else
                    _ThrowSysError("Chyba při výmazu z registru, do složky " + folder.ToString() + " není možno zapisovat.");
            }
        }
        /// <summary>
        /// Vymaže hodnotu z Windows registru.
        /// Privátní VÝKONNÁ metoda.
        /// </summary>
        /// <param name="folder">Složka</param>
        /// <param name="keyName">Jméno hodnoty</param>
        private static void _DeleteValue(RegistryKey regKey, string keyName)
        {
            if (regKey != null)
                regKey.DeleteValue(keyName, false);
            else
                _ThrowSysError("Chyba při výmazu z registru, předaný klíč je NULL.");
        }
        #endregion
		#region throw errors
		/// <summary>
		/// Můstek na vyvolání metody SysError
		/// </summary>
		/// <param name="message">Zpráva o chybě</param>
		private static void _ThrowSysError(string message)
		{
            throw new InvalidOperationException(message);
		}
		/// <summary>
		/// Můstek na vyvolání metody SysError
		/// </summary>
		/// <param name="message">Zpráva o chybě</param>
		/// <param name="exc">Vnitřní výjimka</param>
        private static void _ThrowSysError(string message, Exception exc)
        {
            throw new InvalidOperationException(message, exc);
        }
		/// <summary>
		/// Můstek na vyvolání metody SysError
		/// </summary>
		/// <param name="message"></param>
		private static void _ThrowInpError(string message)
		{
            throw new InvalidOperationException(message);
		}
		#endregion
	}
    #region struct WinRegFolder : specifikace složky registru Windows, počínaje Hive a View, včetně textu Folder
    /// <summary>
    /// WinRegFolder : specifikace složky registru Windows, počínaje Hive a View, včetně textu Folder
    /// </summary>
    public struct WinRegFolder
    {
        /// <summary>
        /// Vrátí specifikaci složky Win registru pro daný Hive a složku, pro View 32/64 podle operačního systému
        /// </summary>
        /// <param name="hive"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static WinRegFolder CreateForSystemView(RegistryHive hive, string folder)
        {
            return new WinRegFolder(hive, DefaultSystemView, folder);
        }
        /// <summary>
        /// Vrátí specifikaci složky Win registru pro daný Hive a složku, pro View 32/64 podle aktuálně běžícího procesu
        /// </summary>
        /// <param name="hive"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static WinRegFolder CreateForProcessView(RegistryHive hive, string folder)
        {
            return new WinRegFolder(hive, DefaultProcessView, folder);
        }
        /// <summary>
        /// Vrátí specifikaci složky Win registru pro daný Hive, View a složku
        /// </summary>
        /// <param name="hive"></param>
        /// <param name="view"></param>
        /// <param name="folder"></param>
        public WinRegFolder(RegistryHive hive, RegistryView view, string folder)
        {
            this._Hive = hive;
            this._View = view;
            this._Folder = folder;
            this._HasData = true;
        }
        public override string ToString()
        {
            string hive = this.Hive.ToString();
            string view = (this.View == RegistryView.Registry32 ? "[32]" : (this.View == RegistryView.Registry64 ? "[64]" : ""));
            return hive + view + ":" + this.Folder;
        }
        /// <summary>
        /// Hive = "úl" = základní členění registru (ClassesRoot, CurrentUser (=HKCU), LocalMachine (=HKLM), ... )
        /// </summary>
        public RegistryHive Hive { get { return this._Hive; } } private RegistryHive _Hive;
        /// <summary>
        /// Pohled z hlediska bitů (32/64 bit).
        /// View výchozí pro aktuální prostředí je ve statické property DefaultView
        /// </summary>
        public RegistryView View { get { return this._View; } } private RegistryView _View;
        /// <summary>
        /// Název složky (například "SOFTWARE\Microsoft\Office\Outlook\Addins\ESET.OutlookAddin": bez rootu, bez úvodního lomítka).
        /// Úvodní i koncové lomítko (pokud bude zadáno) bude ignorováno.
        /// String nesmí být null ani prázdný, délka nesmí přesáhnout 255 znaků.
        /// </summary>
        public string Folder { get { return this._Folder; } } private string _Folder;
        /// <summary>
        /// true pokud tento klíč je prázdný (například WinRegFolder.Empty, nebo new WinRegFolder()).
        /// </summary>
        public bool IsEmpty { get { return !this._HasData; } } private bool _HasData;
        /// <summary>
        /// Obsahuje prázdný klíč
        /// </summary>
        public static WinRegFolder Empty { get { return new WinRegFolder(); } }
        /// <summary>
        /// View dané aktuálním operačním systémem (Environment.Is64BitOperatingSystem ? Registry64 : Registry32);
        /// </summary>
        public static RegistryView DefaultSystemView { get { return (Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32); } }
        /// <summary>
        /// View dané aktuálním procesem (Environment.Is64BitProcess ? Registry64 : Registry32);
        /// </summary>
        public static RegistryView DefaultProcessView { get { return (Environment.Is64BitProcess ? RegistryView.Registry64 : RegistryView.Registry32); } }
        /// <summary>
        /// Ověří správnost zadaného názvu klíče.
        /// </summary>
        /// <param name="keyName">Název klíče</param>
        public void CheckKeyName()
        {
            if (this.Folder == null)
                throw new InvalidOperationException("Chybně zadaný název klíče registru (null).");
            else if (this.Folder.Trim().Length == 0)
                throw new InvalidOperationException("Nezadaný název klíče registru (délka = 0).");
            else if (this.Folder.Trim().Length >= 255)
                throw new InvalidOperationException("Nesprávně zadaný název klíče registru (délka >= 255).");
        }
        /// <summary>
        /// Vrátí nový objekt, který bude mít shodný Hive i View, a jehož Folder = this.Folder + [\] + (folder).
        /// Oddělovač přidá tehdy, pokud this.Folder nekončí oddělovačem a ani dodaný folder nezačíná oddělovačem.
        /// Pokud this.Folder končí oddělovačem, nebo dodaný folder začíná oddělovačem, pak žádný oddělovač nevkládá.
        /// Pokud this.Folder končí oddělovačem, a současně dodaný folder začíná oddělovačem, pak sice oddělovač nevkládá, ale ony dva oddělovače ponechává.
        /// Je tedy vhodné oddělovače NEZADÁVAT, v případě potřeby budou DOPLNĚNY, ale v případě duplicty NEBUDOU ODSTRANĚNY.
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public WinRegFolder AddFolder(string folder)
        {
            string separator = (this.Folder.EndsWith(@"\") || folder.StartsWith(@"\") ? "" : @"\");
            return new WinRegFolder(this.Hive, this.View, this.Folder + separator + folder);
        }
        /// <summary>
        /// Vrátí nový objekt, který bude mít shodný View i Folder, ale bude mít vyměněný Hive.
        /// </summary>
        /// <param name="hive"></param>
        /// <returns></returns>
        public WinRegFolder ChangeHive(Microsoft.Win32.RegistryHive hive)
        {
            return new WinRegFolder(hive, this.View, this.Folder);
        }
    }
    #endregion
    #region class Crypt : Poskytuje metody pro zakryptování a dekryptování stringu na byte array/string a zpátky s pomocí Rijndael algoritmu s fixním klíčem.
    /// <summary>
    /// Třída pro šifrování textů pomocí mechanismu Rijndael.
    /// </summary>
    public class Crypt
    {
        #region PUBLIC METODY Encrypt(), EncryptToString() a Decrypt()
        /// <summary>
        /// Zašifruje zadaný text k nepoznání
        /// </summary>
        /// <param name="text">Čitelný text</param>
        /// <returns>Pole zašifrovaných znaků</returns>
        public static byte[] Encrypt(string text)
        {
            if (text == null) return null;
            return _Encrypt(text, _GetCryptor());
        }
        /// <summary>
        /// Zašifruje zadaný text k nepoznání
        /// </summary>
        /// <param name="text">Čitelný text</param>
        /// <returns>Pole zašifrovaných znaků zakódované</returns>
        public static string EncryptToString(string text)
        {
            if (text == null) return null;
            return Convert.ToBase64String(_Encrypt(text, _GetCryptor()));
        }
        /// <summary>
        /// Dešifruje zadaný kód do původního textu
        /// </summary>
        /// <param name="buffer">Pole zašifrovaných znaků</param>
        /// <returns>Čitelný text</returns>
        public static string Decrypt(byte[] buffer)
        {
            if (buffer == null) return null;
            return _Decrypt(buffer, _GetCryptor());
        }
        /// <summary>
        /// Dešifruje zadaný kód do původního textu
        /// </summary>
        /// <param name="crypted">Zašifrovaný text v kódování Base64</param>
        /// <returns>Čitelný text</returns>
        public static string Decrypt(string crypted)
        {
            if (crypted == null) return null;
            return _Decrypt(_GetBytes64(crypted), _GetCryptor());
        }
        #endregion
        #region PRIVÁTNÍ VÝKONNÉ KRYPTOVACÍ METODY
        /// <summary>
        /// Vytvoří a vrátí kryptovací nástroj, po každém volání zcela identický (nutnost pro dekryptování !)
        /// </summary>
        /// <returns></returns>
        private static System.Security.Cryptography.SymmetricAlgorithm _GetCryptor()
        {
            // Jednoduchý klíč a IV mechanismus:
            string cText = "gB7Bmiq6gaqYrfBwHWypOHWnnY7+hX4dPWC1LJND6uPQKq7+Vwaqm8FRDipVUBAcpNExSApiBvXB6tIdG4TZ/vpcH1QKI3wmUvzcQU8f5vl/InDWHBrCPA/MAf2vLW6yqfnW1FgkXHJE+4ykL8DbCDozBNHx0TUIkQaUyCrWzWPfxQn+QaLGQBrTAuDAi64NfAU+IM+VZ4F/OJ/b9S298ebJ/vbNkB8/LbRsIKU+mODWsP6Y6ahaNuC1DZPGS4W2";
            byte[] buffer = Convert.FromBase64String(cText);

            // Rijndael mechanismus detekuje i chyby v zakryptovaném kódu, vyhodí chybu a Decrypt metody vrací null:
            System.Security.Cryptography.Rijndael rijndael = System.Security.Cryptography.Rijndael.Create();
            rijndael.Key = _GetByteArray(buffer, 12, rijndael.Key.Length);
            rijndael.IV = _GetByteArray(buffer, 7, rijndael.IV.Length);
            rijndael.Mode = System.Security.Cryptography.CipherMode.CFB;
            return rijndael;
        }
        /// <summary>
        /// Kryptovací metoda. Výkonný kód.
        /// </summary>
        /// <param name="text">Vstupní text</param>
        /// <param name="key">Kryptor</param>
        /// <returns>Byte array obsahující zakryptovaný text</returns>
        private static byte[] _Encrypt(string text, System.Security.Cryptography.SymmetricAlgorithm key)
        {
            if (text == null) return null;
            byte[] buffer;
            using (MemoryStream ms = new MemoryStream())
            using (System.Security.Cryptography.CryptoStream encStream = new System.Security.Cryptography.CryptoStream(ms, key.CreateEncryptor(), System.Security.Cryptography.CryptoStreamMode.Write))
            using (StreamWriter sw = new StreamWriter(encStream))
            {
                sw.Write(text);
                sw.Close();
                encStream.Close();
                buffer = ms.ToArray();
                ms.Close();
            }
            return buffer;
        }
        /// <summary>
        /// Dekryptovací metoda. Výkonný kód.
        /// </summary>
        /// <param name="buffer">Byte array obsahující zakryptovaný text</param>
        /// <param name="key">Kryptor</param>
        /// <returns>Původní čitelný text</returns>
        /// <exception cref="SysException">Výjimka při chybě dekryptování</exception>
        private static string _Decrypt(byte[] buffer, System.Security.Cryptography.SymmetricAlgorithm key)
        {
            if (buffer == null) return null;
            string text = "";
            try
            {
                using (MemoryStream ms = new MemoryStream(buffer))
                using (System.Security.Cryptography.CryptoStream cs = new System.Security.Cryptography.CryptoStream(ms, key.CreateDecryptor(), System.Security.Cryptography.CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    try
                    {
                        text = sr.ReadToEnd();
                    }
#pragma warning disable 0168
                    catch (Exception exc)
#pragma warning restore 0168
                    {
                        text = null;
                        key.Clear();
                    }
                    finally
                    {
                        try { cs.Clear(); }
                        catch { }
                        try { sr.Close(); }
                        catch { }
                        try { ms.Close(); }
                        catch { }
                    }
                }
            }
            catch
            {
                text = null;
            }
            return text;
        }
        #endregion
        #region PRIVÁTNÍ VÝKONNÉ PRIMITIVNÍ METODY
        /// <summary>
        /// Ze zadaného stringu vrátí přesně definovanou část bytového pole
        /// </summary>
        /// <param name="text">Vstupní string</param>
        /// <param name="from">Počátek pozice bytového pole, odkud začneme přebírat výsledek</param>
        /// <param name="count">Délka požadovaného bytového pole</param>
        /// <returns></returns>
        private static byte[] _GetByteArray(string text, int from, int count)
        {
            System.Collections.Generic.List<byte> codeList = new System.Collections.Generic.List<byte>(_GetByteArray(text));
            byte[] result = new byte[count];
            codeList.CopyTo(0, result, 0, count);
            return result;
        }
        /// <summary>
        /// Ze zadaného bytového pole vrátí přesně definovanou část bytového pole
        /// </summary>
        /// <param name="buffer">Vstupní bytové pole</param>
        /// <param name="from">Počátek pozice bytového pole, odkud začneme přebírat výsledek</param>
        /// <param name="count">Délka požadovaného bytového pole</param>
        /// <returns></returns>
        private static byte[] _GetByteArray(byte[] buffer, int from, int count)
        {
            System.Collections.Generic.List<byte> codeList = new System.Collections.Generic.List<byte>(buffer);
            byte[] result = new byte[count];
            codeList.CopyTo(0, result, 0, count);
            return result;
        }
        /// <summary>
        /// Primitivní kryptovací metoda - ze stringu vrátí byte[]
        /// </summary>
        /// <param name="text">string</param>
        /// <returns>byte[] vytvořené ze stringu</returns>
        private static byte[] _GetByteArray(string text)
        {
            byte[] buffer;
            using (MemoryStream ms = new MemoryStream())
            using (StreamWriter sw = new StreamWriter(ms))
            {
                sw.WriteLine(text);
                sw.Close();
                buffer = ms.ToArray();
                ms.Close();
            }
            return buffer;
        }
        /// <summary>
        /// Primitivní dekryptovací metoda - z byte[] vrátí string
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static string _GetString(byte[] buffer)
        {
            string text;
            using (MemoryStream ms = new MemoryStream(buffer))
            using (StreamReader sr = new StreamReader(ms))
            {
                text = sr.ReadLine();
                sr.Close();
                ms.Close();
            }
            return text;
        }
        /// <summary>
        /// Konverze stringu ve formátu Base64 na byte array
        /// </summary>
        /// <param name="crypted">String ve formátu Base64</param>
        /// <returns>byte array nebo null</returns>
        private static byte[] _GetBytes64(string crypted)
        {
            byte[] buffer = null;
            try
            {
                buffer = Convert.FromBase64String(crypted);
            }
#pragma warning disable 0168
            catch (Exception exc)
#pragma warning restore 0168
            {
                buffer = null;
            }
            return buffer;
        }

        #endregion
    }
    #endregion
}