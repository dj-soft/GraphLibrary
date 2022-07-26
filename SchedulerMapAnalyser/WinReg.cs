using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace DjSoft.Support
{
	#region CLASS WinReg, STATICKÉ METODY PRO PŘÍSTUP K WINDOWS REGISTRŮM
	/// <summary>
	/// Třída pro snadný přístup k Windows registru, pro ověřování instalačních informací a ukládání konfigurací
	/// </summary>
	public class WinReg
	{
		#region PUBLIC METODY
		#region READ VALUE NAMES
		/// <summary>
		/// Načte soupis datových klíčů z dané složky Win registru.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <returns>Seznam názvů hodnot v dané složce</returns>
		public static List<string> GetValueNames(string folder)
		{
			return _GetValueNames(folder);
		}
		/// <summary>
		/// Načte soupis podsložek z dané složky Win registru.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <returns>Seznam názvů podsložek v dané složce</returns>
		public static List<string> GetSubKeyNames(string folder)
		{
			return _GetSubKeyNames(folder);
		}
		#endregion
		#region READ & WRITE PLAIN STRING
		/// <summary>
		/// Načte řetězec z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Nalezený string / Default hodnota (null)</returns>
		public static string ReadString(string folder, string keyName)
		{
			return ReadString(folder, keyName, null);
		}
		/// <summary>
		/// Načte řetězec z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezený string / Default hodnota</returns>
		public static string ReadString(string folder, string keyName, string defValue)
		{
			_CheckKeyName(keyName);
			return _ReadString(folder, keyName, defValue);
		}
		/// <summary>
		/// Do registru zapíše hodnotu typu string.
		/// </summary>
		/// <param name="folder">Složka, může být ""</param>
		/// <param name="keyName">Název hodnoty</param>
		/// <param name="value">Hodnota, může být i null.</param>
		public static void WriteString(string folder, string keyName, string value)
		{
			_CheckKeyName(keyName);
			_CheckValueNotNull(value, keyName);
			_WriteValue(folder, keyName, value, RegistryValueKind.String);
		}
		#endregion
		#region READ & WRITE CRYPTED STRING
		/// <summary>
		/// Načte řetězec z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Nalezený string / Default hodnota (null)</returns>
		public static string ReadStringCrypt(string folder, string keyName)
		{
			return ReadStringCrypt(folder, keyName, null);
		}
		/// <summary>
		/// Načte řetězec z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezený string / Default hodnota</returns>
		public static string ReadStringCrypt(string folder, string keyName, string defValue)
		{
			_CheckKeyName(keyName);
			return _ReadStringCrypt(folder, keyName, defValue);
		}
		/// <summary>
		/// Do registru zapíše hodnotu typu string, zakryptovanou.
		/// </summary>
		/// <param name="folder">Složka, může být ""</param>
		/// <param name="keyName">Název hodnoty</param>
		/// <param name="value">Hodnota, může být i null.</param>
		public static void WriteStringCrypt(string folder, string keyName, string value)
		{
			throw new NotImplementedException("Crypt not implemented");
			/*
			_CheckKeyName(keyName);
			_CheckValueNotNull(value, keyName);
			byte[] crypt = Crypt.Encrypt(value);
			_WriteValue(folder, keyName, crypt, RegistryValueKind.Binary);
			*/
		}
		#endregion
		#region READ & WRITE BOOL
		/// <summary>
		/// Načte hodnotu Bool z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Nalezená hodnota bool / Default hodnota (false)</returns>
		public static bool ReadBool(string folder, string keyName)
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
		public static bool ReadBool(string folder, string keyName, bool defValue)
		{
			_CheckKeyName(keyName);
			int saved = _ReadInt32(folder, keyName, -1);
			return (saved == -1 ? defValue : (saved != 0));
		}
		/// <summary>
		/// Do registru zapíše hodnotu typu Int32.
		/// </summary>
		/// <param name="folder">Složka, může být ""</param>
		/// <param name="keyName">Název hodnoty</param>
		/// <param name="value">Hodnota.</param>
		public static void WriteBool(string folder, string keyName, bool value)
		{
			_CheckKeyName(keyName);
			int saved = (value ? 1 : 0);                                 // Ukládáme hodnotu Int32: false = 0; true = 1;
			_WriteValue(folder, keyName, saved, RegistryValueKind.DWord);
		}
		#endregion
		#region READ & WRITE INTEGER
		/// <summary>
		/// Načte číslo Int32 z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Nalezené číslo / Default hodnota (0)</returns>
		public static int ReadInt32(string folder, string keyName)
		{
			return ReadInt32(folder, keyName, 0);
		}
		/// <summary>
		/// Načte číslo Int32 z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezený string / Default hodnota</returns>
		public static int ReadInt32(string folder, string keyName, int defValue)
		{
			_CheckKeyName(keyName);
			return _ReadInt32(folder, keyName, defValue);
		}
		/// <summary>
		/// Do registru zapíše hodnotu typu Int32.
		/// </summary>
		/// <param name="folder">Složka, může být ""</param>
		/// <param name="keyName">Název hodnoty</param>
		/// <param name="value">Hodnota.</param>
		public static void WriteInt32(string folder, string keyName, int value)
		{
			_CheckKeyName(keyName);
			_WriteValue(folder, keyName, value, RegistryValueKind.DWord);
		}
		#endregion
		#region READ & WRITE LONG
		/// <summary>
		/// Načte číslo Int64 z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Nalezené číslo / Default hodnota (0)</returns>
		public static long ReadInt64(string folder, string keyName)
		{
			return ReadInt64(folder, keyName, 0);
		}
		/// <summary>
		/// Načte číslo Int64 z Windows registru
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezené číslo / Default hodnota</returns>
		public static long ReadInt64(string folder, string keyName, long defValue)
		{
			_CheckKeyName(keyName);
			return _ReadInt64(folder, keyName, defValue);
		}
		/// <summary>
		/// Do registru zapíše hodnotu typu Int64.
		/// </summary>
		/// <param name="folder">Složka, může být ""</param>
		/// <param name="keyName">Název hodnoty</param>
		/// <param name="value">Hodnota.</param>
		public static void WriteInt32(string folder, string keyName, long value)
		{
			_CheckKeyName(keyName);
			_WriteValue(folder, keyName, value, RegistryValueKind.QWord);
		}
		#endregion
		#region READ & WRITE BINARY (tj. BYTE ARRAY)
		/// <summary>
		/// Načte binární data z Windows registru do pole byte[]
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Nalezené číslo / Default hodnota (0)</returns>
		public static byte[] ReadBinary(string folder, string keyName)
		{
			return ReadBinary(folder, keyName, null);
		}
		/// <summary>
		/// Načte binární data z Windows registru do pole byte[]
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezené číslo / Default hodnota</returns>
		public static byte[] ReadBinary(string folder, string keyName, byte[] defValue)
		{
			_CheckKeyName(keyName);
			return _ReadBinary(folder, keyName, defValue);
		}
		/// <summary>
		/// Do registru zapíše binární data z pole byte[]
		/// </summary>
		/// <param name="folder">Složka, může být ""</param>
		/// <param name="keyName">Název hodnoty</param>
		/// <param name="value">Hodnota, pole byte[].</param>
		public static void WriteBinary(string folder, string keyName, byte[] value)
		{
			_CheckKeyName(keyName);
			_WriteValue(folder, keyName, value, RegistryValueKind.Binary);
		}
		#endregion
		#region EXIST VALUE, EXIST FOLDER
		/// <summary>
		/// Detekuje, zda existuje klíč daného jména. Vrací true = klíč existuje / false = klíč neexistuje.
		/// </summary>
		/// <param name="folder">Složka</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Hodnota true = klíč existuje / false = klíč neexistuje</returns>
		public static bool ValueExists(string folder, string keyName)
		{
			_CheckKeyName(keyName);
			return _ValueExists(folder, keyName);
		}
		/// <summary>
		/// Detekuje, zda existuje podsložka daného jména. Vrací true = podsložka existuje / false = podsložka neexistuje.
		/// </summary>
		/// <param name="folder">Složka, v jejímž rámci testuji.</param>
		/// <param name="subKeyName">Název podsložky, kterou testuji. Musí být zadán</param>
		/// <returns>Hodnota true = klíč existuje / false = klíč neexistuje</returns>
		public static bool SubKeyExists(string folder, string subKeyName)
		{
			_CheckKeyName(subKeyName);
			return _SubKeyExists(folder, subKeyName);
		}
		#endregion
		#region DELETE VALUE, DELETE SUBKEY
		/// <summary>
		/// Vymaže hodnotu z Windows registru
		/// </summary>
		/// <param name="folder">Složka</param>
		/// <param name="keyName">Jméno hodnoty</param>
		public static void DeleteValue(string folder, string keyName)
		{
			_CheckKeyName(keyName);
			_DeleteValue(folder, keyName);
		}
		/// <summary>
		/// Vymaže hodnotu z Windows registru.
		/// Privátní VÝKONNÁ metoda.
		/// </summary>
		/// <param name="folder">Složka</param>
		/// <param name="keyName">Jméno hodnoty</param>
		private static void _DeleteValue(string folder, string keyName)
		{
			using (RegistryKey regKey = _OpenRootAndFolder(folder, true))
			{
				regKey.DeleteValue(keyName, false);
			}
		}
		/// <summary>
		/// Vymaže složku z Windows registru
		/// </summary>
		/// <param name="folder">Složka, v níž bude mazání probíhat (tedy ne ta, kterou chceme mazat)</param>
		/// <param name="subKeyName">Jméno podsložky, kterou budeme mazat</param>
		public static void DeleteSubKey(string folder, string subKeyName)
		{
			_CheckKeyName(subKeyName);
			_DeleteSubKey(folder, subKeyName);
		}
		/// <summary>
		/// Vymaže složku z Windows registru
		/// Privátní VÝKONNÁ metoda.
		/// </summary>
		/// <param name="folder">Složka, v níž bude mazání probíhat (tedy ne ta, kterou chceme mazat)</param>
		/// <param name="subKeyName">Jméno podsložky, kterou budeme mazat</param>
		private static void _DeleteSubKey(string folder, string subKeyName)
		{
			using (RegistryKey regKey = _OpenRootAndFolder(folder, true))
			{
				regKey.DeleteSubKeyTree(subKeyName);
			}
		}
		#endregion
		#region BASE REGISTRY PATH
		/// <summary>
		/// Obsahuje true, pokud je systém registrů připraven k práci (je nastaven root) nebo false když systém není připraven.
		/// </summary>
		public static bool Prepared { get { return (!String.IsNullOrEmpty(_RootFolder)); } }
		/// <summary>
		/// Vrací základnu registru, default = "CurrentUser" = HKEY_CURRENT_USER
		/// </summary>
		/// <returns>RegistryKey odpovídající základně používaného registru. Přímo fyzický root.</returns>
		private static RegistryKey _OpenRegRoot()
		{
			RegistryHive hive = RootHive;
			switch (hive)
			{
				case RegistryHive.ClassesRoot:
					//     Represents the HKEY_CLASSES_ROOT base key on another computer. This value
					//     can be passed to the Microsoft.Win32.RegistryKey.OpenRemoteBaseKey(Microsoft.Win32.RegistryHive,System.String)
					//     method, to open this node remotely.
					return Registry.ClassesRoot;

				case RegistryHive.CurrentUser:
					//     Represents the HKEY_CURRENT_USER base key on another computer. This value
					//     can be passed to the Microsoft.Win32.RegistryKey.OpenRemoteBaseKey(Microsoft.Win32.RegistryHive,System.String)
					//     method, to open this node remotely.
					return Registry.CurrentUser;

				case RegistryHive.LocalMachine:
					//     Represents the HKEY_LOCAL_MACHINE base key on another computer. This value
					//     can be passed to the Microsoft.Win32.RegistryKey.OpenRemoteBaseKey(Microsoft.Win32.RegistryHive,System.String)
					//     method, to open this node remotely.
					return Registry.LocalMachine;

				case RegistryHive.Users:
					//     Represents the HKEY_USERS base key on another computer. This value can be
					//     passed to the Microsoft.Win32.RegistryKey.OpenRemoteBaseKey(Microsoft.Win32.RegistryHive,System.String)
					//     method, to open this node remotely.
					return Registry.Users;

				case RegistryHive.PerformanceData:
					//     Represents the HKEY_PERFORMANCE_DATA base key on another computer. This value
					//     can be passed to the Microsoft.Win32.RegistryKey.OpenRemoteBaseKey(Microsoft.Win32.RegistryHive,System.String)
					//     method, to open this node remotely.
					return Registry.PerformanceData;

				case RegistryHive.CurrentConfig:
					//     Represents the HKEY_CURRENT_CONFIG base key on another computer. This value
					//     can be passed to the Microsoft.Win32.RegistryKey.OpenRemoteBaseKey(Microsoft.Win32.RegistryHive,System.String)
					//     method, to open this node remotely.
					return Registry.CurrentConfig;

				case RegistryHive.DynData:
					//     Represents the HKEY_DYN_DATA base key on another computer. This value can
					//     be passed to the Microsoft.Win32.RegistryKey.OpenRemoteBaseKey(Microsoft.Win32.RegistryHive,System.String)
					//     method, to open this node remotely.
					return Registry.DynData;

				default:
					_ThrowSysError("Nastavený registry root není platný (" + ((int)hive).ToString() + "=" + hive.ToString() + ").");
					break;
			}
			return null;
		}
		/// <summary>
		/// Vrací použitý registry root (hive).
		/// </summary>
		public static RegistryHive RootHive { get { return _GetRootHive(); } }
		/// <summary>
		/// Vrací fixní část cesty (folder) k registru aplikace.
		/// </summary>
		public static string RootFolder { get { return _GetRootFolder(); } }
		/// <summary>
		/// Vrátí aktuální registry root (hive), anebo vyhodí chybu pokud dosud nebyla nastavena.
		/// </summary>
		/// <returns></returns>
		private static RegistryHive _GetRootHive()
		{
			if ((int)_RootHive != 0) return _RootHive;
			_ThrowSysError("Byl proveden pokus o přístup do WinReg, ale nebyla nastavena výchozí cesta.");
			return _RootHive;        // Pouze pro kompiler
		}
		/// <summary>
		/// Vrátí aktuální základnovou cestu, anebo vyhodí chybu pokud dosud nebyla nastavena.
		/// </summary>
		/// <returns></returns>
		private static string _GetRootFolder()
		{
			if (Prepared) return _RootFolder;
			_ThrowSysError("Byl proveden pokus o přístup do WinReg, ale nebyla nastavena výchozí cesta.");
			return null;            // Pouze pro kompiler
		}
		/// <summary>
		/// Nastaví výchozí (defaultní) aktuální základní cestu do registru. Současně ji aktivuje jako aktuální.
		/// Tuto metodu je třeba volat dříve, než se začne používat systém WinReg. Bez nastavení defaultní cesty nebude systém pracovat.
		/// Na tuto cestu je možno se kdykoli vrátit vyvoláním metody RestoreRootDefaultFolder() - bez parametrů, 
		/// defaultní hodnota je uložena ve třídě WinReg.
		/// Základní cestu je možno dočasně změnit metodou SetRootFolder(string folder).
		/// </summary>
		/// <param name="defaultFolder">Defaultní základní adresář. Typicky: "Software\Firma\Produkt\Verze", bez lomítek na začátku a na konci, se zpětnými lomítky.</param>
		public static void SetRootDefaultFolder(string defaultFolder)
		{
			SetRootDefaultFolder(RegistryHive.CurrentUser, defaultFolder);
		}
		/// <summary>
		/// Nastaví výchozí (defaultní) aktuální základní cestu do registru. Současně ji aktivuje jako aktuální.
		/// Tuto metodu je třeba volat dříve, než se začne používat systém WinReg. Bez nastavení defaultní cesty nebude systém pracovat.
		/// Na tuto cestu je možno se kdykoli vrátit vyvoláním metody RestoreRootDefaultFolder() - bez parametrů, 
		/// defaultní hodnota je uložena ve třídě WinReg.
		/// Základní cestu je možno dočasně změnit metodou SetRootFolder(string folder).
		/// </summary>
		/// <param name="defaultHive">Defaultní root (hive). Pokud se použije varianta metody bez zadání rootu, použije se root "CurrentUser".</param>
		/// <param name="defaultFolder">Defaultní základní adresář. Typicky: "Software\Firma\Produkt\Verze", bez lomítek na začátku a na konci, se zpětnými lomítky.</param>
		public static void SetRootDefaultFolder(RegistryHive defaultHive, string defaultFolder)
		{
			_RootHive = defaultHive;
			_RootDefaultHive = defaultHive;
			_RootDefaultFolder = defaultFolder;
			_RootFolder = defaultFolder;
		}
		/// <summary>
		/// Znovu nastaví do aktuální základní cesty hodnotu, která byla předána jako defaultní (metodou SetRootDefaultFolder(string defaultFolder)).
		/// Tuto metodu je vhodné volat poté, kdy byla dočasně změněna aktuální základní cesta metodou SetRootFolder(string folder).
		/// Metoda vrací defaultní hodnoty pro root i folder.
		/// </summary>
		public static void RestoreRootDefaultFolder()
		{
			_RootHive = _RootDefaultHive;
			_RootFolder = _RootDefaultFolder;
		}
		/// <summary>
		/// Nastaví aktuální základní cestu do registru.
		/// Nemění defaultní cestu (viz metody SetRootDefaultFolder(string defaultFolder) a RestoreRootDefaultFolder()).
		/// </summary>
		/// <param name="folder">Nově platná základní cesta. Root se v této variantě nemění.</param>
		public static void SetRootFolder(string folder)
		{
			_RootFolder = folder;
		}
		/// <summary>
		/// Nastaví aktuální základní cestu do registru.
		/// Nemění defaultní cestu (viz metody SetRootDefaultFolder(string defaultFolder) a RestoreRootDefaultFolder()).
		/// </summary>
		/// <param name="hive">Nově platný root.</param>
		/// <param name="folder">Nově platná základní cesta.</param>
		public static void SetRootFolder(RegistryHive hive, string folder)
		{
			_RootHive = hive;
			_RootFolder = folder;
		}
		/// <summary>Úložiště pro aktuální cestu k registrům</summary>
		private static string _RootFolder;
		/// <summary>Úložiště pro defaultní cestu k registrům</summary>
		private static string _RootDefaultFolder;
		/// <summary>Úložiště pro aktuální registr root (top-level node)</summary>
		private static RegistryHive _RootHive = (RegistryHive)0;
		/// <summary>Úložiště pro defaultní registr root (top-level node)</summary>
		private static RegistryHive _RootDefaultHive = (RegistryHive)0;
		#endregion
		#endregion
		#region PRIVÁTNÍ METODY : PRÁCE SE SLOŽKAMI REGISTRU (OTEVÍRÁNÍ, VYTVÁŘENÍ)
		/// <summary>
		/// Otevře aktuální segment registru, základnovou cestu (není součástí parametru "folder") a v něm najde a otevře danou podsložku.
		/// Pokud není podsložka zadaná, vrací aplikační root.
		/// Pokud je složka zadaná a neexistuje, bude vytvořena a pak vrácena.
		/// Funguje i víceúrovňově, tj. dokáže zpracovat i vnořené cesty.
		/// </summary>
		/// <param name="folder">Název složky registru. Pokud neexistuje, bude vytvořena.</param>
		/// <param name="forWrite">Požadavek na otevření (true) pro čtení i zápis  /  false = jen pro čtení. Týká se jen poslední úrovně SubKeys.</param>
		/// <returns>RegistryKey odpovídající požadovanému názvu. Pokud neexistuje, bude vytvořena.</returns>
		private static RegistryKey _OpenRootAndFolder(string folder, bool forWrite)
		{
			RegistryKey regKey = _OpenRegRoot();                       // Registr HKEY_CURRENT_USER\
			string regFolder = _GetAbsFolder(folder);                  // Zkombinuje základní cestu plus přidanou (folder)
			return _OpenOrCreateFolder(regKey, regFolder, forWrite);
		}
		/// <summary>
		/// Otevře aktuální segment registru, a danou absolutní cestu (která již obsahuje základnovou cestu!)
		/// </summary>
		/// <param name="folder">Název složky registru. Pokud neexistuje, bude vytvořena.</param>
		/// <param name="forWrite">Požadavek na otevření (true) pro čtení i zápis  /  false = jen pro čtení. Týká se jen poslední úrovně SubKeys.</param>
		/// <returns>RegistryKey odpovídající požadovanému názvu. Pokud neexistuje, bude vytvořena.</returns>
		private static RegistryKey _OpenAbsoluteFolder(string folder, bool forWrite)
		{
			RegistryKey regKey = _OpenRegRoot();                       // Registr HKEY_CURRENT_USER\
			if (folder == null || folder.Length == 0) return regKey;   // Bez další cesty: hotovo.
			return _OpenOrCreateFolder(regKey, folder, forWrite);      // Otevírá cestu bez kombinace s aplikačním rootem (bez metody _GetAbsFolder())
		}
		/// <summary>
		/// Vytvoří absolutní cestu k dané složce.
		/// Fixní část = "Software\LCS International\Helios Green - Backup utility"
		/// Pokud je zadáno folder, pak se přidá lomítko a daná cesta.
		/// Příklad:
		/// _GetAbsFolder("DataSetting") vrátí "Software\LCS International\Helios Green - Backup utility\DataSetting".
		/// </summary>
		/// <param name="folder">Složka v rámci aplikačního rootu (RootFolder), tj. ne fyzického rootu</param>
		/// <returns>Plná cesta ke složce</returns>
		private static string _GetAbsFolder(string folder)
		{
			string result = RootFolder;
			if (folder != null && folder.Length > 0)
				result += "\\" + folder;
			return result;
		}
		/// <summary>
		/// V rámci daného klíče otevře / vytvoří složku i při zadání s podsložkami, a vrátí daný RegistryKey.
		/// </summary>
		/// <param name="regKey">Registr, současná otevřená úroveň</param>
		/// <param name="folder">Jméno složky, může obsahovat i podsložky oddělené \ </param>
		/// <param name="forWrite">Otevřít pro zápis, platí pouze pro poslední úroveň hierarchie složek</param>
		/// <returns>RegistryKey zadané podsložky</returns>
		private static RegistryKey _OpenOrCreateFolder(RegistryKey regKey, string folder, bool forWrite)
		{
			RegistryKey result = regKey;

			if (folder == null || folder.Length == 0) return result;

			// Rozdělení dané cesty na jednotlivé složky:
			List<string> folders = new List<string>(folder.Split('\\'));          // Rozdělit cestu na složky

			// Otevřeme / vytvoříme postupně každou úroveň, přičemž parametr "forWrite" aplikujeme jen na poslední úroveň:
			int last = folders.Count - 1;
			for (int i = 0; i <= last; i++)
			{
				string item = folders[i];
				if (item.Trim().Length > 0)
				{
					bool iForWrite = (i == last && forWrite);                         // forWrite jen na poslední úroveň
																					  // Ponorný algoritmus (ponor je realizovan tím, že výsledek otevření složky ukládáme zpětně do result)
					result = _OpenOrCreateFolderOneItem(result, item, iForWrite);
				}
			}

			return result;
		}
		/// <summary>
		/// V rámci daného klíče otevře / vytvoří složku, pouze jednoúrovňově (složka nesmí obsahovat \),
		/// a vrátí daný RegistryKey.
		/// </summary>
		/// <param name="regKey">Registr, současná otevřená úroveň</param>
		/// <param name="item">Jméno složky, nesmí obsahovat podsložky oddělené \ </param>
		/// <param name="forWrite">Otevřít pro zápis</param>
		/// <returns>RegistryKey daného podklíče</returns>
		private static RegistryKey _OpenOrCreateFolderOneItem(RegistryKey regKey, string item, bool forWrite)
		{
			// Kontrola, zda nalezená úroveň obsahuje danou složku, případně ji vytvoří:
			bool existFolder = _SubKeyExistInKey(regKey, item);
			if (!existFolder)
			{
				_CreateSubKey(regKey, item);
			}
			RegistryKey subKey = regKey.OpenSubKey(item, forWrite);

			return subKey;
		}
		/// <summary>
		/// Zajistí vytvoření subkey v dané úrovni registru.
		/// Vyšší funkce otestovala, že daný subkey zde neexistuje.
		/// </summary>
		/// <param name="regKey">RegistryKey aktuální úrovně, v níž máme vytvářet SubKey. Vždy je otevřena pro čtení.</param>
		/// <param name="item">Jméno pro SubKey</param>
		private static void _CreateSubKey(RegistryKey regKey, string item)
		{
			// Protože úroveň (regKey) je otevřena pro čtení, a my ji chceme otevřít pro zápis, musíme ji otevřít znovu - pro zápis:
			string regAbsFolder = _GetFolderFromKey(regKey);

			using (RegistryKey regKeyWritable = _OpenAbsoluteFolder(regAbsFolder, true))
			{
				// Vytvoří další subKey:
				try
				{
					regKeyWritable.CreateSubKey(item);
					regKeyWritable.Flush();
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
				finally
				{
					regKeyWritable.Close();
				}
			}
		}
		/// <summary>
		/// Vrátí název absolutní cesty (folder) z daného registru.
		/// Odebere počáteční definici HKEY_..., kterou převede na enum.
		/// </summary>
		/// <param name="regKey">Registr, současná otevřená úroveň</param>
		/// <returns></returns>
		private static string _GetFolderFromKey(RegistryKey regKey)
		{
			RegistryHive hive;
			return _GetFolderFromKey(regKey, out hive);
		}
		/// <summary>
		/// Vrátí název absolutní cesty (folder) z daného registru.
		/// Odebere počáteční definici HKEY_..., kterou převede na enum.
		/// </summary>
		/// <param name="regKey">Registr, současná otevřená úroveň</param>
		/// <param name="hive">Výstup hodnoty RegistryHive, do které patří zadaná cesta</param>
		/// <returns>Cesta ze zadané adresy, bez hive</returns>
		private static string _GetFolderFromKey(RegistryKey regKey, out RegistryHive hive)
		{
			string regName = regKey.Name;  // Obsahuje typicky: "HKEY_CURRENT_USER\Software\LCS International\Helios Green - Backup utility"
			string firstName = "";
			int firstDiv = regName.IndexOf("\\");              // Pozice prvního znaku "\"
			if (firstDiv >= 0)
			{
				firstName = regName.Substring(0, firstDiv);    // Text "HKEY_CURRENT_USER"
				if (firstDiv >= (regName.Length - 1))          // Pokud by první \ bylo posledním znakem textu:
					regName = "";
				else
					regName = regName.Substring(firstDiv + 1); // Text "Software\LCS International\Helios Green - Backup utility"
			}

			hive = _GetHiveFromName(firstName);

			return regName;
		}
		/// <summary>
		/// Podle názvu top-nodu registru (např. "HKEY_LOCAL_MACHINE") vrátí jeho enum (např. RegistryHive.LocalMachine)
		/// </summary>
		/// <param name="hiveName">Název, Trim().ToUpper()</param>
		/// <returns>Hive</returns>
		private static RegistryHive _GetHiveFromName(string hiveName)
		{
			switch (hiveName)
			{
				case "HKEY_CLASSES_ROOT":
					return RegistryHive.ClassesRoot;
				case "HKEY_CURRENT_USER":
					return RegistryHive.CurrentUser;
				case "HKEY_LOCAL_MACHINE":
					return RegistryHive.LocalMachine;
				case "HKEY_USERS":
					return RegistryHive.Users;
				case "HKEY_PERFORMANCE_DATA":
					return RegistryHive.PerformanceData;
				case "HKEY_CURRENT_CONFIG":
					return RegistryHive.CurrentConfig;
				case "HKEY_DYN_DATA":
					return RegistryHive.DynData;
			}
			return RegistryHive.CurrentUser;
		}
		/// <summary>
		/// Ověří správnost zadaného názvu klíče.
		/// </summary>
		/// <param name="keyName">Název klíče</param>
		private static void _CheckKeyName(string keyName)
		{
			if (keyName == null)
				_ThrowSysError("Chybně zadaný název klíče registru (null).");
			else if (keyName.Trim().Length == 0)
				_ThrowSysError("Nezadaný název klíče registru (délka = 0).");
			else if (keyName.Trim().Length >= 255)
				_ThrowSysError("Nesprávně zadaný název klíče registru (délka >= 255).");
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
			List<string> subKeyNames = _GetSubKeyNames(regKey, true);
			return (subKeyNames.Contains(folder.ToLower()));
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
			List<string> valueNames = _GetValueNames(regKey, true);
			return (valueNames.Contains(value.ToLower()));
		}
		/// <summary>
		/// Vrátí seznam názvů všech subkey (podsložek) na aktuální úrovni registru
		/// </summary>
		/// <param name="regKey">Registr, současná otevřená úroveň</param>
		/// <param name="toLower">Požadavek, aby všechna jména byla převedena na Lower()</param>
		/// <returns>Seznam názvů všech podsložek na dané úrovni</returns>
		private static List<string> _GetSubKeyNames(RegistryKey regKey, bool toLower)
		{
			List<string> subNames = new List<string>(regKey.GetSubKeyNames());

			if (toLower)
			{
				for (int s = 0; s < subNames.Count; s++)
					subNames[s] = subNames[s].ToLower();
			}

			return subNames;
		}
		/// <summary>
		/// Vrátí seznam názvů všech values (hodnot) na aktuální úrovni registru
		/// </summary>
		/// <param name="regKey">Registr, současná otevřená úroveň</param>
		/// <param name="toLower">Požadavek, aby všechna jména byla převedena na Lower()</param>
		/// <returns>Seznam názvů všech hodnot na dané úrovni</returns>
		private static List<string> _GetValueNames(RegistryKey regKey, bool toLower)
		{
			List<string> subNames = new List<string>(regKey.GetValueNames());

			if (toLower)
			{
				for (int s = 0; s < subNames.Count; s++)
					subNames[s] = subNames[s].ToLower();
			}

			return subNames;
		}
		#endregion
		#region PRIVÁTNÍ METODY : EXISTS, ČTENÍ, ZÁPIS A VÝMAZ HODNOT
		/// <summary>
		/// Detekuje, zda existuje klíč daného jména. Vrací true = klíč existuje / false = klíč neexistuje.
		/// Privátní výkonná metoda.
		/// </summary>
		/// <param name="folder">Složka</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <returns>Hodnota true = klíč existuje / false = klíč neexistuje</returns>
		private static bool _ValueExists(string folder, string keyName)
		{
			bool exists = false;
			using (RegistryKey regKey = _OpenRootAndFolder(folder, false))
			{
				exists = _ValueExistInKey(regKey, keyName);
			}
			return exists;
		}
		/// <summary>
		/// Detekuje, zda existuje podsložka daného jména. Vrací true = podsložka existuje / false = podsložka neexistuje.
		/// </summary>
		/// <param name="folder">Složka, v jejímž rámci testuji.</param>
		/// <param name="subKeyName">Název podsložky, kterou testuji. Musí být zadán</param>
		/// <returns>Hodnota true = klíč existuje / false = klíč neexistuje</returns>
		private static bool _SubKeyExists(string folder, string subKeyName)
		{
			bool exists = false;
			using (RegistryKey regKey = _OpenRootAndFolder(folder, false))
			{
				exists = _SubKeyExistInKey(regKey, subKeyName);
			}
			return exists;
		}
		/// <summary>
		/// Načte soupis datových klíčů z dané složky Win registru.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <returns>Seznam názvů hodnot v dané složce</returns>
		private static List<string> _GetValueNames(string folder)
		{
			List<string> result = new List<string>();
			using (RegistryKey regKey = _OpenRootAndFolder(folder, false))
			{
				result.AddRange(regKey.GetValueNames());
			}
			return result;
		}
		/// <summary>
		/// Načte soupis podsložek z dané složky Win registru.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <returns>Seznam názvů podsložek v dané složce</returns>
		private static List<string> _GetSubKeyNames(string folder)
		{
			List<string> result = new List<string>();
			using (RegistryKey regKey = _OpenRootAndFolder(folder, false))
			{
				result.AddRange(regKey.GetSubKeyNames());
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
		private static string _ReadString(string folder, string keyName, string defValue)
		{
			string result = defValue;
			using (RegistryKey regKey = _OpenRootAndFolder(folder, false))
			{
				if (_ValueExistInKey(regKey, keyName))
				{
					RegistryValueKind valueKind = regKey.GetValueKind(keyName);
					if (valueKind == RegistryValueKind.String)
						result = (string)regKey.GetValue(keyName, defValue);
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
		private static string _ReadStringCrypt(string folder, string keyName, string defValue)
		{
			throw new NotImplementedException("Crypt not implemented");
			/*
			string result = defValue;
			using (RegistryKey regKey = _OpenRootAndFolder(folder, false))
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
			*/
		}
		/// <summary>
		/// Načte číslo Int32 z Windows registru.
		/// Privátní výkonná metoda.
		/// </summary>
		/// <param name="folder">Složka, může být "" (tj. aplikační root, ne registrový root)</param>
		/// <param name="keyName">Název klíče, musí být zadán</param>
		/// <param name="defValue">Default hodnota</param>
		/// <returns>Nalezený string / Default hodnota</returns>
		private static int _ReadInt32(string folder, string keyName, int defValue)
		{
			int result = defValue;
			using (RegistryKey regKey = _OpenRootAndFolder(folder, false))
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
		private static long _ReadInt64(string folder, string keyName, long defValue)
		{
			long result = defValue;
			using (RegistryKey regKey = _OpenRootAndFolder(folder, false))
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
		private static byte[] _ReadBinary(string folder, string keyName, byte[] defValue)
		{
			byte[] result = defValue;
			using (RegistryKey regKey = _OpenRootAndFolder(folder, false))
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
		private static void _WriteValue(string folder, string keyName, object value, RegistryValueKind kind)
		{
			using (RegistryKey regKey = _OpenRootAndFolder(folder, true))
			{
				try
				{
					regKey.SetValue(keyName, value, kind);
					regKey.Flush();
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
				finally
				{
					regKey.Close();
				}
			}
		}
		#endregion
		#region MŮSTEK NA DIALOGOVÉ METODY
		/// <summary>
		/// Můstek na vyvolání metody SysError
		/// </summary>
		/// <param name="message">Zpráva o chybě</param>
		private static void _ThrowSysError(string message)
		{
			throw new InvalidOperationException(message);
			// Throw.SysError(MessageInfo.Get(message));
		}
		/// <summary>
		/// Můstek na vyvolání metody SysError
		/// </summary>
		/// <param name="message">Zpráva o chybě</param>
		/// <param name="exc">Vnitřní výjimka</param>
		private static void _ThrowSysError(string message, Exception exc)
		{
			throw new InvalidOperationException(message, exc);
			// Throw.SysError(MessageInfo.Get(message), exc);
		}
		/// <summary>
		/// Můstek na vyvolání metody SysError
		/// </summary>
		/// <param name="message"></param>
		private static void _ThrowInpError(string message)
		{
			throw new InvalidOperationException(message);
			// Throw.InputError(MessageInfo.Get(message));
		}
		#endregion
	}
	#endregion
}
