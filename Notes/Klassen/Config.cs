using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using IO = System.IO;
using Re = System.Text.RegularExpressions;

namespace Notes {
  public class Config {

    #region Events
    public delegate void ConfigEventHandler(EventArgs e);
    public static event ConfigEventHandler ConfigChanged;
    public static event ConfigEventHandler ConfigSaved;
    public static event ConfigEventHandler ConfigReloaded;
    #endregion

    #region Construction
    private static Config MySelf;

    private const string CONFIG_FILE_NAME = "Notes_Config.json";
    private static string GetConfigFilePath() {
      string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
      path += @"\Notes";
      if (!IO.Directory.Exists(path)) {
        IO.Directory.CreateDirectory(path);
      }
      path += "\\" + CONFIG_FILE_NAME;
      return path;
    }

    public static void Save() {
      if (MySelf == null) {
        MySelf = new Config();
      }
      try {
        string jj = Newtonsoft.Json.JsonConvert.SerializeObject(MySelf, Newtonsoft.Json.Formatting.Indented);
        string path = GetConfigFilePath();
        IO.File.WriteAllText(path, jj);
        MySelf._Notes.Where(nc => nc.IsFileMapped()).ToList().ForEach(nc => nc.SaveToFile());
        ConfigSaved?.Invoke(new EventArgs());
      }
      catch (Exception ex) {
        MessageBox.Show(ex.Message, "Error saving Config!", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    public static void Reload() {
      string path = GetConfigFilePath();
      if (!IO.File.Exists(path)) {
        Save();
      } else {
        try {
          string jj = IO.File.ReadAllText(path);
          MySelf = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(jj);
          MySelf._Notes.Where(nc => nc.IsFileMapped()).ToList().ForEach(nc => nc.LoadFromFile());
          ConfigReloaded?.Invoke(new EventArgs());
        }
        catch (Exception ex) {
          MessageBox.Show(ex.Message, "Error loading Config!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    static Config() {
      Reload();
    }
    #endregion

    #region Properties
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [Newtonsoft.Json.JsonProperty("Notes")]
    private ManagedList<NoteConfig> _Notes = new ManagedList<NoteConfig>();
    /// <summary>
    /// Containes all NoteConfigs
    /// </summary>
    public static ManagedList<NoteConfig> Notes {
      get {
        return MySelf._Notes;
      }
      set {
        bool isEquals = MySelf._Notes.Equals(value);
        MySelf._Notes = value;
        if (!isEquals) {
          ConfigChanged?.Invoke(new EventArgs());
        }
      }
    }


    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [Newtonsoft.Json.JsonProperty("CustomStylePatterns")]
    private List<ReplacePattern> _CustomStylePatterns = new List<ReplacePattern>() {
      new ReplacePattern(@"^(\s*)(\*)(\s+)", "&#160;&#x25C9;&#160;", Re.RegexOptions.IgnoreCase | Re.RegexOptions.Multiline),
      new ReplacePattern(@"^(\s*)(\*){2}(\s+)", "&#160;&#160;&#160;&#x25C9;&#160;", Re.RegexOptions.IgnoreCase | Re.RegexOptions.Multiline),
      new ReplacePattern(@"^(\s*)(\*){3}(\s+)", "&#160;&#160;&#160;&#160;&#160;&#x25C9;&#160;", Re.RegexOptions.IgnoreCase | Re.RegexOptions.Multiline),
      new ReplacePattern(@"^(\s*)(\*){4}(\s+)", "&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#x25C9;&#160;", Re.RegexOptions.IgnoreCase | Re.RegexOptions.Multiline),
      new ReplacePattern(@"^(\s*)(\*){5}(\s+)", "&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#x25C9;&#160;", Re.RegexOptions.IgnoreCase | Re.RegexOptions.Multiline),
      new ReplacePattern(@"^(\s*)(\*){6}(\s+)", "&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#x25C9;&#160;", Re.RegexOptions.IgnoreCase | Re.RegexOptions.Multiline),
      new ReplacePattern(@"^---$", "<hr />", Re.RegexOptions.Multiline),
      new ReplacePattern(@"\n", "<br />"),
    };
    /// <summary>
    /// Contains all Replacement Patterns for your custom Style.
    /// </summary>
    public static List<ReplacePattern> CustomStylePatterns {
      get {
        return MySelf._CustomStylePatterns;
      }
      set {
        MySelf._CustomStylePatterns = value;
      }
    }
    #endregion
  }
}
