using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Application
{
    /// <summary>
    /// Class Plugin: search for interface of IPlugin and their implementations in assemblies.
    /// </summary>
    public class Plugin
    {
        #region Loading all plugins from assemblies
        /// <summary>
        /// Constructor
        /// </summary>
        internal Plugin()
        {
            this._PluginsDict = new Dictionary<Type, PluginInfoList>();
            this._PluginsSingletonDict = new Dictionary<Type, object>();
        }
        /// <summary>
        /// Search for plugins. Call only once when application is started.
        /// </summary>
        internal void LoadPlugins()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Type[] types = assembly.GetTypes();
            this.LoadPluginsInterfaces(types);
            this.LoadPluginsImplementationsInTypes(types);
        }
        /// <summary>
        /// Search for interfaces, which is descendant of IPlugin
        /// </summary>
        /// <param name="types"></param>
        private void LoadPluginsInterfaces(Type[] types)
        {
            Type iPlugin = typeof(IPlugin);
            foreach (Type type in types)
            {
                if (!type.IsInterface || type == iPlugin) continue;

                // Detect, if "type" (=interface) has interface IPlugin, when have, then add this interface into this._PluginsDict:
                Type[] implementInterfaces = type.FindInterfaces((t, o) => (t == iPlugin), null);
                if (implementInterfaces.Length > 0)
                    this._PluginsDict.Add(type, new PluginInfoList(type));
            }
        }
        /// <summary>
        /// Search for implementations of all IPlugins in this assembly
        /// </summary>
        /// <param name="assembly"></param>
        private void LoadPluginsImplementationsInAssembly(System.Reflection.Assembly assembly)
        {
            Type[] types = assembly.GetTypes();
            this.LoadPluginsImplementationsInTypes(types);
        }
        /// <summary>
        /// Search for implementations of all IPlugins in this types
        /// </summary>
        /// <param name="types"></param>
        private void LoadPluginsImplementationsInTypes(Type[] types)
        {
            Dictionary<Type, PluginInfoList> iPlugins = this._PluginsDict;

            foreach (Type type in types)
            {
                if (type.IsInterface || !type.IsClass || type.IsAbstract) continue;

                // Search in "type" (class) for interfaces from this._IPluginsList (descendants from IPlugin):
                //  (one class can implement more than one plugin !)
                Type[] implementInterfaces = type.FindInterfaces((t, o) => (iPlugins.ContainsKey(t)), null);
                foreach (Type implementInterface in implementInterfaces)
                {   // This "type" implements one (or more) IPlugin interfaces:
                    this.AddPluginImplementation(type, implementInterface);
                }
            }
        }
        /// <summary>
        /// Add singleton of "instanceType" into info array for "pluginType"
        /// </summary>
        /// <param name="instanceType"></param>
        /// <param name="pluginType"></param>
        private void AddPluginImplementation(Type instanceType, Type pluginType)
        {
            object singleton = GetSingleton(instanceType);
            PluginInfoList pluginInfoList = this.GetPluginInfoList(pluginType, true);
            pluginInfoList.AddImplementation(instanceType, singleton);
        }
        /// <summary>
        /// Get or Create new instance (=singleton) for specified "instanceType"
        /// </summary>
        /// <param name="instanceType"></param>
        /// <returns></returns>
        private object GetSingleton(Type instanceType)
        {
            object singleton;
            if (!this._PluginsSingletonDict.TryGetValue(instanceType, out singleton))
            {
                singleton = CreateSingleton(instanceType);
                this._PluginsSingletonDict.Add(instanceType, singleton);
            }
            return singleton;
        }
        /// <summary>
        /// Create new instance for specified "instanceType"
        /// </summary>
        /// <param name="instanceType"></param>
        /// <returns></returns>
        private object CreateSingleton(Type instanceType)
        {
            object singleton = null;
            try
            {
                singleton = System.Activator.CreateInstance(instanceType);
            }
            catch (Exception exc)
            {
                App.TraceException(exc, "Exception when create instance of plugin", "Type: " + instanceType.Namespace + "." + instanceType.Name, "Assembly: " + instanceType.AssemblyQualifiedName);
                singleton = null;
            }
            return singleton;
        }
        /// <summary>
        /// Get or Create new info list for "pluginType".
        /// One pluginType has one PluginInfoList, in one PluginInfoList is contained List of instances (type, instance, activity).
        /// If one instance implements more than one plugin, then this same instance (=singleton) is referenced from more PluginInfoList (each one for each pluginType).
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="canCreateNewInfo">true = can create new info, when does not exists / false ´when does not exists, return null</param>
        /// <returns></returns>
        private PluginInfoList GetPluginInfoList(Type pluginType, bool canCreateNewInfo)
        {
            PluginInfoList pluginInfoList;
            if (!this._PluginsDict.TryGetValue(pluginType, out pluginInfoList))
            {
                pluginInfoList = new PluginInfoList(pluginType);
                this._PluginsDict.Add(pluginType, pluginInfoList);
            }
            return pluginInfoList;
        }
        /// <summary>
        /// Dictionary, where Key = specific PluginType (interface from IPlugin),
        /// and Value is List of all classes, which implements this IPlugin.
        /// </summary>
        private Dictionary<Type, PluginInfoList> _PluginsDict;
        /// <summary>
        /// Dictionary, where Key = Type of instance, and Value is this instance. Only for types, which is present in _PluginsDict.
        /// This is array of Singleton of all (IPlugin) implenent classes.
        /// </summary>
        private Dictionary<Type, object> _PluginsSingletonDict;
        /// <summary>
        /// Info for one pluginType (=interface, descendant from IPlugin)
        /// </summary>
        protected class PluginInfoList
        {
            /// <summary>
            /// Constructor for pluginType
            /// </summary>
            /// <param name="pluginType"></param>
            public PluginInfoList(Type pluginType)
            {
                this.PluginType = pluginType;
                this.PluginList = new List<PluginInfo>();
            }
            public override string ToString()
            {
                return "PluginType: " + this.PluginType.NsName() + "; Implementations found: " + this.PluginList.Count.ToString();
            }
            /// <summary>
            /// Type of Plugins = interface
            /// </summary>
            public Type PluginType { get; private set; }
            /// <summary>
            /// List of all plugins of this type
            /// </summary>
            public List<PluginInfo> PluginList { get; private set; }
            /// <summary>
            /// Add new item for instance into this list.
            /// </summary>
            /// <param name="instanceType">Type of instance</param>
            /// <param name="singleton">Object (singleton) or null (when Exception occured on object creation)</param>
            internal void AddImplementation(Type instanceType, object singleton)
            {
                PluginActivity activity = PluginActivity.Disabled;
                if (singleton != null && singleton is IPlugin)
                    activity = (singleton as IPlugin).Activity;
                this.PluginList.Add(new PluginInfo(this.PluginType, instanceType, activity, singleton));
            }
        }
        /// <summary>
        /// Info for one (pluginType + instanceType)
        /// </summary>
        protected class PluginInfo
        {
            public PluginInfo(Type pluginType, Type instanceType, PluginActivity activity, object instance)
            {
                this.PluginType = pluginType;
                this.InstanceType = instanceType;
                this.Activity = activity;
                this.Instance = instance;
            }
            public override string ToString()
            {
                return "PluginType: " + PluginType.NsName() + "; InstanceType: " + this.InstanceType.NsName() + "; Activity: " + this.Activity.ToString();
            }
            /// <summary>
            /// Type of Plugin = interface
            /// </summary>
            public Type PluginType { get; private set; }
            /// <summary>
            /// Type of instance = implementation
            /// </summary>
            public Type InstanceType { get; private set; }
            /// <summary>
            /// Activity of this implementation. PluginActivity.Disabled = error on instantiationg of object.
            /// </summary>
            public PluginActivity Activity { get; private set; }
            /// <summary>
            /// Implementing object itself (or null, when Activity == Disable)
            /// </summary>
            public object Instance { get; private set; }
            /// <summary>
            /// Returns true when this plugin is active (for current debug mode)
            /// </summary>
            /// <param name="isDebug"></param>
            /// <returns></returns>
            public bool IsActiveInMode(bool isDebug)
            {
                return (this.Instance != null && (this.Activity == PluginActivity.Standard || (isDebug && this.Activity == PluginActivity.OnlyDebug)));
            }
        }
        #endregion
        #region Find plugins for specific type
        /// <summary>
        /// Returns all objects, which implements specified interface.
        /// When type is null or is not interface, return empty array.
        /// When type is interface, which is not descendant of IPlugin, return also empty array.
        /// When return non-empty array, then none of items is null.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isDebugMode"></param>
        /// <returns></returns>
        public IEnumerable<object> GetPlugins(Type type, bool isDebugMode)
        {
            List<object> result = new List<object>();
            if (type != null && type.IsInterface)
            {
                PluginInfoList infoList = this.GetPluginInfoList(type, false);
                if (infoList != null)
                {
                    result.AddRange(infoList.PluginList
                        .Where(i => i.IsActiveInMode(isDebugMode))
                        .Select(i => i.Instance));
                }
            }
            return result;
        }
        #endregion
    }
    #region Interface IPlugin; enum PluginActivity
    /// <summary>
    /// Předek pro všechny interface Pluginů
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Aktivita tohoto pluginu
        /// </summary>
        PluginActivity Activity { get; }
    }
    /// <summary>
    /// Aktivita jakéhokoli pluginu
    /// </summary>
    public enum PluginActivity
    {
        /// <summary>
        /// Plugin není aktivní (slouží k operativnímu vypnutí pluginu)
        /// </summary>
        None,
        /// <summary>
        /// Plugin je aktivní pouze v Debug modu (pokud je připojen Debugger)
        /// </summary>
        OnlyDebug,
        /// <summary>
        /// Standardní aktivita
        /// </summary>
        Standard,
        /// <summary>
        /// Disabled = po chybě při inicializaci instance pluginu
        /// </summary>
        Disabled
    }
    #endregion
}
