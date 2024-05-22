using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Notes.Config;
using System.Windows.Media;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;
using System.Windows;
using System.Windows.Media.Animation;
using IO = System.IO;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace Notes {
  public enum DisplayTypes {
    Plain = 0,
    Org = 1,
    MarkDown = 2,
    Custom = 3
  }

  public enum DisplayColors {
    Yellow,
    Orange,
    Red,
    Pink,
    Blue,
    Green
  }

  public class NoteConfig {

    public const int DEFAULT_MARGIN = 20;
    public const int DEFAULT_PADDING = 10;
    public const int DEFAULT_TOP_BAR = 5;
    public const int DEFAULT_WIDTH = 200;
    public const int DEFAULT_HEIGHT = 200;

    private NoteWindow myWindow = null;
    private bool LastSynkOk = false;
    private bool IsSyncing = false;

    #region JsonProperties
    public int Top { get; set; }
    public int Left { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Visible { get; set; }
    public string File { get; set; }
    public DisplayColors Color { get; set; }
    public DisplayTypes DisplayType { get; set; }

    private string _Content = "";
    public string Content {
      get {
        return _Content;
      }
      set {
        bool changed = _Content != value;
        _Content = value;
        if (changed) {
          ContentChanged = true;
        }
      }
    }
    #endregion

    #region WpfProperties
    public const string EMPTY_STR = "<<EMPTY>>";
    [Newtonsoft.Json.JsonIgnore()]
    public string DisplayName {
      get {
        string[] lines = Content.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 1) {
          lines = new string[] { EMPTY_STR };
        }
        string result = lines[0].Trim();
        bool addDots = false;
        if (result.Length > 200) {
          result = result.Substring(0, 200).Trim();
          addDots = true;
        }
        if (string.IsNullOrEmpty(result) || string.IsNullOrWhiteSpace(result))
          return EMPTY_STR;
        if (addDots) {
          result += "...";
        }
        result = Regex.Replace(result, @"^[\s#]*", "");
        return result;
      }
      set { }
    }

    [Newtonsoft.Json.JsonIgnore()]
    public Visibility UiEyeVisibility {
      get {
        return Visible ? Visibility.Collapsed : Visibility.Visible;
      }
      set {
      }
    }

    [Newtonsoft.Json.JsonIgnore()]
    public Visibility UiCrossVisibility {
      get {
        return Visible ? Visibility.Visible : Visibility.Collapsed;
      }
      set {
      }
    }

    [Newtonsoft.Json.JsonIgnore()]
    public Visibility SyncOnAndOk {
      get {
        if (IsSyncing)
          return Visibility.Collapsed;
        if (IsFileMapped())
          return LastSynkOk ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
      }
      set { }
    }

    [Newtonsoft.Json.JsonIgnore()]
    public Visibility SyncOnButFailed {
      get {
        if (IsSyncing)
          return Visibility.Collapsed;
        if (IsFileMapped())
          return LastSynkOk ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Collapsed;
      }
      set { }
    }

    [Newtonsoft.Json.JsonIgnore()]
    public Visibility SyncOnAndSyncing {
      get {
        if (IsFileMapped())
          return IsSyncing ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
      }
      set { }
    }

    [Newtonsoft.Json.JsonIgnore()]
    public bool ContentChanged { get; private set; } = false;
    #endregion

    #region Construction
    public NoteConfig() {
      Drawing.Rectangle wa = Forms.Screen.PrimaryScreen.WorkingArea;
      Top = wa.Top + (wa.Height / 2) - (DEFAULT_HEIGHT / 2) - DEFAULT_MARGIN - DEFAULT_PADDING - DEFAULT_TOP_BAR;
      Left = wa.Left + (wa.Width / 2) - (DEFAULT_WIDTH / 2) - DEFAULT_MARGIN - DEFAULT_PADDING;
      Width = DEFAULT_WIDTH;
      Height = DEFAULT_HEIGHT;
      Content = " Note -  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
      Content += ("\n" + ("".PadLeft(Content.Length, '─'))) + "\n";
      Visible = true;
      File = string.Empty;
      DisplayType = DisplayTypes.Plain;
      Color = DisplayColors.Yellow;
    }

    public NoteConfig(int top, int left, int width, int height, string content, bool visible, string file, DisplayTypes displayType) {
      this.Top = top;
      this.Left = left;
      this.Width = width;
      this.Height = height;
      this.Content = content;
      this.Visible = visible;
      this.File = file;
      this.DisplayType = displayType;
    }
    #endregion

    #region Functions
    public bool IsFileMapped() {
      if (File == null) { return false; }
      if (File.Length == 0) { return false; }
      if (string.IsNullOrEmpty(File)) { return false; }
      if (string.IsNullOrWhiteSpace(File)) { return false; }
      if (!IO.File.Exists(File)) {
        try {
          IO.File.WriteAllText(File, Content, Encoding.UTF8);
        }
        catch (Exception ex) {
          App.OccuredExceptions.Add(new App.ExceptionMessage("Error creating mapped file!", ex));
        }
      }
      return true;
    }

    public bool HasFileChanged() {
      if (!IsFileMapped()) { return false; }
      string fileContent = "\b\0\0\b\t";
      try {
        fileContent = IO.File.ReadAllText(File, Encoding.UTF8);
      }
      catch (Exception ex) {
        App.OccuredExceptions.Add(new App.ExceptionMessage("Error reading File!", ex));
      }
      return fileContent != Content;
    }

    public void SetFile(string file) {
      try {
        File = file;
        IO.File.WriteAllText(File, Content, Encoding.UTF8);
      }
      catch (Exception ex) {
        App.OccuredExceptions.Add(new App.ExceptionMessage("Error creating File!", ex));
      }
    }

    public void SaveToFile(bool ignoreContentCheck = false) {
      if (IsFileMapped()) {
        if (ContentChanged || ignoreContentCheck) {
          IsSyncing = true;
          myWindow?.UpdateSyncBindingTargets();
          WriteToFileBgw(File, Content, Encoding.UTF8, success => {
            IsSyncing = false;
            LastSynkOk = success;
            myWindow?.UpdateSyncBindingTargets();
          });
          ContentChanged = false;
        }
      }
    }

    private struct WriteToFileBgwArgs {
      public string file;
      public string content;
      public Encoding encoding;
    }

    private void WriteToFileBgw(string file, string content, Encoding encoding, Action<bool> Callback) {
      BackgroundWorker bgw = new BackgroundWorker();
      bgw.DoWork += (b, e) => {
        Thread.Sleep(1);
        WriteToFileBgwArgs? a = e.Argument as WriteToFileBgwArgs?;
        if (a.HasValue) {
          try {
            IO.File.WriteAllText(a.Value.file, a.Value.content, a.Value.encoding);
            e.Result = true;
          }
          catch (Exception ex) {
            e.Result = false;
            lock(App.OccuredExceptions) {
              App.OccuredExceptions.Add(new App.ExceptionMessage("Error saving File!", ex));
            }
          }
        } else {
          e.Result = false;
        }
      };
      bgw.RunWorkerCompleted += (b, e) => {
        bool? success = e.Result as bool?;
        bool s = false;
        if (success.HasValue) s = success.Value;
        Callback(s);
      };
      WriteToFileBgwArgs args = new WriteToFileBgwArgs() {
        file = file,
        content = content,
        encoding = encoding
      };
      bgw.RunWorkerAsync(args);
    }

    public void LoadFromFile() {
      if (IsFileMapped()) {
        try {
          Content = IO.File.ReadAllText(File, Encoding.UTF8);
        }
        catch (Exception ex) {
          App.OccuredExceptions.Add(new App.ExceptionMessage("Error loading File!", ex));
        }
      }
    }

    public void Show() {
      if (myWindow == null) {
        myWindow = new NoteWindow(this);
        //myWindow.Owner = MainWindow.Instance;
        myWindow.Closed += (s, e) => {
          myWindow = null;
        };
      }
      myWindow.Show();
      myWindow.BringIntoView();
      myWindow.Activate();
      this.Visible = true;
      Config.Save();
    }

    public void Hide() {
      if (myWindow != null) {
        myWindow.Close();
      }
      this.Visible = false;
      Config.Save();
    }

    public void Delete() {
      if (MessageBox.Show("Do you really want to delete this?\n\nNote:\n" + DisplayName, "Delete?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
        Config.Notes.Remove(this);
        Hide();
      }
    }

    public string GetContent(System.Windows.Media.Color BgColor) {
      return NoteRenderer.RenderNote(this, BgColor);
    }
    #endregion
  }
}
