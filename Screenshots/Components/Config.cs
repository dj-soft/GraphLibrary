using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace Djs.Tools.Screenshots.Components
{
    public class Config
    {
        public static Config LoadConfig()
        {
            Config config = new Config();
            config._File = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Screenshots", "Config.ini");
            config._SetDefaults();
            if (File.Exists(config._File))
                config._ConfigLoad();
            return config;
        }
        private Config()
        {
        }
        private string _File;
        public void Save()
        {
            this._ConfigSave();
        }
        public Rectangle FormBounds { get; set; }
        public string TargetPath { get; set; }
        public int FrequencyIndex { get; set; }
        public bool HideHelp { get; set; }

        private void _SetDefaults()
        {
            if (this.FormBounds.Width <= 0 || this.FormBounds.Height <= 0)
                this.FormBounds = new Rectangle(40, 50, 1024, 800);
            if (String.IsNullOrEmpty(this.TargetPath))
                this.TargetPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Screenshots");
        }
        private void _ConfigLoad()
        {
            this._PreparePath(this._File);
            Persist.LoadTo(this._File, this);
            this._SetDefaults();
        }
        private void _ConfigSave()
        {
            this._PreparePath(this._File);
            File.WriteAllText(this._File, Persist.Serialize(this));
        }
        private void _PreparePath(string file)
        {
            string path = Path.GetDirectoryName(file);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        }
}
