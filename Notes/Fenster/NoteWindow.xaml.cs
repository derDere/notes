using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using IO = System.IO;

namespace Notes {
  /// <summary>
  /// Interaktionslogik für NoteWindow.xaml
  /// </summary>
  public partial class NoteWindow : Window {

    public const int COPY_CMD_ID = 50150;
    public const int SELECTALL_CMD_ID = 50156;
    public const int INSPECT_CMD = 50162;
    public const int PRINT_CMD = 35003;

    private static Random RND = new Random();

    private DispatcherTimer ReadOnlyTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(30) };
    private DispatcherTimer FileChangedTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(2) };

    private IO.FileSystemWatcher syncFileWhatcher = null;
    private bool FileChanged = false;

    private Forms.SaveFileDialog SyncFileDialog = new Forms.SaveFileDialog() {
      Title = "Choose file to sync to ...",
      Filter = "Text Files|*.txt|Markdown Files|*.md|Org Files|*.org|All Files|*.*",
      FilterIndex = 0
    };

    private Point? resizeStartPos = null;
    private Size? resizeStartRtxbSize = null;
    private bool loaded = false;
    private CoreWebView2Deferral WebConContextMenuDef = null;
    private CoreWebView2ContextMenuRequestedEventArgs currentConMenEvent = null;

    public NoteConfig NoteConfig { get; set; }

    private SolidColorBrush BgBru = null;
    private SolidColorBrush TitleBgBru = null;

    public NoteWindow(NoteConfig noteConfig) {
      this.NoteConfig = noteConfig;

      string colorSuffix = "Y";
      switch (noteConfig.Color) {
        case DisplayColors.Orange:
          colorSuffix = "O";
          break;
        case DisplayColors.Red:
          colorSuffix = "R";
          break;
        case DisplayColors.Pink:
          colorSuffix = "P";
          break;
        case DisplayColors.Blue:
          colorSuffix = "B";
          break;
        case DisplayColors.Green:
          colorSuffix = "G";
          break;
      }

      BgBru = new SolidColorBrush((Color)App.Current.FindResource("BgColor" + colorSuffix));
      TitleBgBru = new SolidColorBrush((Color)App.Current.FindResource("TitleBgColor" + colorSuffix));

      this.Resources.Add("BgBru", BgBru);
      this.Resources.Add("TitleBgBru", TitleBgBru);

      InitializeComponent();
      this.Title = NoteConfig.DisplayName;

      foreach (MenuItem mi in ColorMi.Items) {
        DisplayColors color = (DisplayColors)mi.Tag;
        if (color == noteConfig.Color) {
          mi.IsChecked = true;
          ColorMi.Icon = mi.Icon;
        }
        else {
          mi.IsChecked = false;
        }
      }

      WebCon.EnsureCoreWebView2Async();

      this.Top = NoteConfig.Top;
      this.Left = NoteConfig.Left;
      this.ContentTxb.Width = NoteConfig.Width;
      this.ContentTxb.Height = NoteConfig.Height;
      this.ContentTxb.Text = NoteConfig.Content;

      StopSyncMi.Visibility = NoteConfig.IsFileMapped() ? Visibility.Visible : Visibility.Collapsed;
      UpdateOrCreateSyncFileWhatcher();
      UpdateSyncBindingTargets();

      foreach (MenuItem mi in ContentTypeMi.Items) {
        DisplayTypes dt = (DisplayTypes)mi.Tag;
        if (dt != NoteConfig.DisplayType) {
          mi.IsChecked = false;
        }
        else {
          mi.IsChecked = true;
          ContentTypeMi.Icon = mi.Icon;
        }
      }

      ReadOnlyTimer.Tick += ReadOnlyTimer_Tick;
      FileChangedTimer.Tick += FileChangedTimer_Tick;
      FileChangedTimer.Start();

      loaded = true;
    }

    private void FileChangedTimer_Tick(object sender, EventArgs e) {
      if (!loaded)
        return;
      if (syncFileWhatcher?.Path == ".") {
        try {
          IO.FileInfo fi = new IO.FileInfo(NoteConfig.File);
          syncFileWhatcher.Path = fi.Directory.FullName;
        }
        catch (Exception ex) {
          if (Debugger.IsAttached) {
            //throw ex;
          }
        }
      }
      if (FileChanged) {
        FileChanged = false;
        if (ContentTxb.IsReadOnly && NoteConfig.HasFileChanged()) {
          NoteConfig.LoadFromFile();
          ContentTxb.Text = NoteConfig.Content;
          WebCon.NavigateToString(NoteConfig.GetContent(BgBru.Color));
        }
      }
    }

    private void UpdateOrCreateSyncFileWhatcher() {
      if (NoteConfig.IsFileMapped()) {
        IO.FileInfo fi = new IO.FileInfo(NoteConfig.File);
        if (syncFileWhatcher == null) {
          syncFileWhatcher = new IO.FileSystemWatcher(".", fi.Name);
          try {
            syncFileWhatcher.Path = fi.Directory.FullName;
          }
          catch (Exception ex) {
            App.OccuredExceptions.Add(new App.ExceptionMessage("Error while setting up FileSystemWatcher!", ex));
          }
          syncFileWhatcher.Changed += SyncFileWhatcher_Changed;
          syncFileWhatcher.EnableRaisingEvents = true;
        }
        else {
          syncFileWhatcher.Path = fi.Directory.FullName;
          syncFileWhatcher.Filter = fi.Name;
        }
      }
      else {
        if (syncFileWhatcher != null) {
          syncFileWhatcher.Changed -= SyncFileWhatcher_Changed;
          syncFileWhatcher.Dispose();
          syncFileWhatcher = null;
        }
      }
    }

    private void SyncFileWhatcher_Changed(object sender, IO.FileSystemEventArgs e) {
      if (!loaded)
        return;
      FileChanged = true;
    }

    private void WebCon_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e) {
      string html = NoteConfig.GetContent(BgBru.Color);
      //WebCon.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
      WebCon.CoreWebView2.Settings.IsZoomControlEnabled = true;
      WebCon.CoreWebView2.ContextMenuRequested += WebCon_CoreWebView2_ContextMenuRequested;
      WebCon.NavigateToString(html);
    }

    private void WebCon_CoreWebView2_ContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e) {
      WebConContextMenuDef = e.GetDeferral();
      currentConMenEvent = e;
      //string cmds = "";
      //foreach (var mi in e.MenuItems) {
      //  cmds += mi.Name + " " + mi.Label + " " + mi.CommandId + "\n";
      //}
      e.Handled = true;
      this.ContextMenu.IsOpen = true;
    }

    public void SetEditMode(bool edit) {
      if (edit != ContentTxb.IsReadOnly)
        return;
      if (edit) {
        ContentTxb.IsReadOnly = false;
        ContentTxb.Cursor = Cursors.IBeam;
        PenImg.Visibility = Visibility.Visible;
        WebCon.Visibility = Visibility.Collapsed;
      }
      else {
        ContentTxb.IsReadOnly = true;
        ContentTxb.Cursor = Cursors.Arrow;
        PenImg.Visibility = Visibility.Collapsed;
        WebCon.NavigateToString(NoteConfig.GetContent(BgBru.Color));
        this.Title = NoteConfig.DisplayName;

        ReadOnlyTimer.Stop();
      }
    }

    private void DragRect_MouseDown(object sender, MouseButtonEventArgs e) {
      if (e.ChangedButton == MouseButton.Left) {
        this.DragMove();
        if (Keyboard.Modifiers == ModifierKeys.Alt) {
          SnapBounds();
        }
      }
    }

    private void ResizeRect_MouseDown(object sender, MouseButtonEventArgs e) {
      if (e.ChangedButton == MouseButton.Left) {
        resizeStartPos = e.GetPosition(this);
        resizeStartRtxbSize = new Size(ContentTxb.Width, ContentTxb.Height);
        ResizeRect.CaptureMouse();
      }
    }

    private void SnapBounds() {
      double top = this.Top;
      double left = this.Left;
      double w = ContentTxb.Width;
      double h = ContentTxb.Height;
      top = Math.Round(top / 20) * 20;
      left = Math.Round(left / 20) * 20;
      w = Math.Round(w / 20) * 20;
      h = Math.Round(h / 20) * 20;
      this.Top = top;
      this.Left = left;
      ContentTxb.Width = w;
      ContentTxb.Height = h;
    }

    private void ResizeRect_MouseUp(object sender, MouseButtonEventArgs e) {
      resizeStartPos = null;
      resizeStartRtxbSize = null;
      ResizeRect.ReleaseMouseCapture();
      if (Keyboard.Modifiers == ModifierKeys.Alt) {
        SnapBounds();
      }
    }

    private void ResizeRect_MouseMove(object sender, MouseEventArgs e) {
      if (resizeStartPos.HasValue && resizeStartRtxbSize.HasValue) {
        Point newPos = e.GetPosition(this);
        Point delta = new Point(newPos.X - resizeStartPos.Value.X, newPos.Y - resizeStartPos.Value.Y);
        int newW = (int)(resizeStartRtxbSize.Value.Width + delta.X);
        int newH = (int)(resizeStartRtxbSize.Value.Height + delta.Y);
        if (newW < 30)
          newW = 30;
        if (newH < 30)
          newH = 30;
        ContentTxb.Width = newW;
        ContentTxb.Height = newH;
      }
    }

    public static int WeekOfYear(DateTime t) {
      CultureInfo ci = CultureInfo.CurrentCulture;
      return ci.Calendar.GetWeekOfYear(t, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);
    }

    private void SendDateUpdate(Dictionary<string, object> jj) {
      jj["cmd"] = "updateDate";
      DateTime t = DateTime.Now;
      if ((bool)jj["utc"]) {
        t = t.ToUniversalTime();
      }
      string value = t.ToString((string)jj["format"]);
      int kw = WeekOfYear(t);
      value = value.Replace("ww", kw.ToString());
      value = value.Replace("WW", kw.ToString().PadLeft(2, '0'));
      jj.Add("value", value);
      WebCon.CoreWebView2.PostWebMessageAsJson(Newtonsoft.Json.JsonConvert.SerializeObject(jj));
    }

    private void OpenLink(Dictionary<string, object> jj) {
      try {
        if (jj["target"]?.ToString().ToLower() == "cmd") {
          ProcessStartInfo psi = new ProcessStartInfo();
          psi.FileName = "cmd";
          psi.Arguments = "/C " + jj["url"]?.ToString();
          psi.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
          Process.Start(psi);
        }
        else if (jj["target"]?.ToString().ToLower() == "explorer") {
          ProcessStartInfo psi = new ProcessStartInfo();
          psi.FileName = "explorer";
          psi.Arguments = "/select, \"" + jj["url"]?.ToString() + "\"";
          psi.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
          Process.Start(psi);
        }
        else {
          Process.Start(jj["url"]?.ToString());
        }
      }
      catch (Exception ex) {
        App.OccuredExceptions.Add(new App.ExceptionMessage("Error Calling Link!", ex));
      }
    }

    private void SetTaskState(Dictionary<string, object> jj) {
      string text = (string)jj["text"];
      bool isChecked = (bool)jj["checked"];
      string id = (string)jj["id"];
      string pattern = @"\[[ Xx]\]" + Regex.Escape(text);
      string newState = (isChecked ? "[X]" : "[ ]") + text;
      ContentTxb.Text = Regex.Replace(ContentTxb.Text, pattern, newState);
    }

    private void WebCon_MessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e) {
      Dictionary<string, object> jj = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(e.WebMessageAsJson);
      switch (jj["cmd"]) {
        case "dateUpdate":
          SendDateUpdate(jj);
          break;
        case "openLink":
          OpenLink(jj);
          break;
        case "ctrlClick":
          if (ContentTxb.IsReadOnly) {
            SetEditMode(true);
          }
          break;
        case "taskUpdate":
          SetTaskState(jj);
          break;
      }
    }

    private void Win_MouseDown(object sender, MouseButtonEventArgs e) {
      if (e.ChangedButton == MouseButton.Left && Keyboard.Modifiers == ModifierKeys.Control) {
        if (ContentTxb.IsReadOnly) {
          SetEditMode(true);
          e.Handled = true;
        }
      }
    }

    private void ReadOnlyTimer_Tick(object sender, EventArgs e) {
      SetEditMode(false);
    }

    private void Win_Activated(object sender, EventArgs e) {
      ReadOnlyTimer.Stop();
    }

    private void Win_Deactivated(object sender, EventArgs e) {
      ReadOnlyTimer.Start();
    }

    private void Cmd_CanAlwaysExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = true;
    }

    private void CloseCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
      this.Close();
    }

    private void ShuffleCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
      if (MessageBox.Show("This will shuffle all line withing the Note except the first one!\n\nDo you want to proceed?", "WARNING!", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
        return;

      string text = ContentTxb.Text;

      text = text.Replace("\r\n", "\n");
      List<string> lines = text.Split(new char[] { '\n', '\r' }).ToList();
      List<string> start = new List<string>() { lines[0] };

      lines = lines.Skip(1).ToList();

      List<int> emptyLines = new List<int>();
      for (int i = 0; i < lines.Count; i++) {
        if (string.IsNullOrEmpty(lines[i]) || string.IsNullOrWhiteSpace(lines[i])) {
          emptyLines.Add(i);
        }
      }
      lines = lines.Where(l => !(string.IsNullOrEmpty(l) || string.IsNullOrWhiteSpace(l))).ToList();

      List<string> newOrder = new List<string>();
      while (lines.Count > 0) {
        int i = RND.Next(10000009, 99999999) % lines.Count;
        newOrder.Add(lines[i]);
        lines.RemoveAt(i);
      }
      foreach (int i in emptyLines) {
        newOrder.Insert(i, "");
      }
      newOrder.InsertRange(0, start);

      text = string.Join("\n", newOrder);

      ContentTxb.Text = text;

      WebCon.NavigateToString(NoteConfig.GetContent(BgBru.Color));
    }

    private void PenImg_MouseDown(object sender, MouseButtonEventArgs e) {
      SetEditMode(false);
    }

    private void Win_SizeChanged(object sender, SizeChangedEventArgs e) {
      if (!loaded)
        return;
      this.NoteConfig.Width = (int)Math.Round(this.ContentTxb.Width);
      this.NoteConfig.Height = (int)Math.Round(this.ContentTxb.Height);
      Config.Save();
    }

    private void Win_LocationChanged(object sender, EventArgs e) {
      if (!loaded)
        return;
      this.NoteConfig.Top = (int)Math.Round(this.Top);
      this.NoteConfig.Left = (int)Math.Round(this.Left);
      Config.Save();
    }

    private void ContentTxb_TextChanged(object sender, TextChangedEventArgs e) {
      if (!loaded)
        return;
      this.NoteConfig.Content = ContentTxb.Text;
      Config.Save();
    }

    private void WebCon_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e) {
      if (ContentTxb.IsReadOnly) {
        WebCon.Visibility = Visibility.Visible;
        if (ContentTxb.Effect != null) {
          ContentTxb.Effect = null;
          ((Grid)LoadingElli.Parent).Children.Remove(LoadingElli);
        }
      }
    }

    private void WinConMen_Closed(object sender, RoutedEventArgs e) {
      if (WebConContextMenuDef != null) {
        WebConContextMenuDef.Complete();
        WebConContextMenuDef = null;
        currentConMenEvent = null;
      }
    }

    private void CopyCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      if (currentConMenEvent != null) {
        foreach (CoreWebView2ContextMenuItem mi in currentConMenEvent.MenuItems) {
          if (mi.CommandId == COPY_CMD_ID) {
            e.CanExecute = true;
            break;
          }
        }
      }
    }

    private void CopyCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
      if (currentConMenEvent != null) {
        currentConMenEvent.SelectedCommandId = COPY_CMD_ID;
      }
    }

    private void SelectAllCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      if (currentConMenEvent != null) {
        foreach (CoreWebView2ContextMenuItem mi in currentConMenEvent.MenuItems) {
          if (mi.CommandId == INSPECT_CMD || mi.CommandId == SELECTALL_CMD_ID) {
            e.CanExecute = true;
            break;
          }
        }
      }
    }

    private void SelectAllCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
      if (currentConMenEvent != null) {
        currentConMenEvent.SelectedCommandId = SELECTALL_CMD_ID;
      }
    }

    private void ContentTypeMi_Checked(object sender, RoutedEventArgs e) {
      if (!loaded)
        return;
      foreach (MenuItem mi in ContentTypeMi.Items) {
        if (mi != sender) {
          mi.IsChecked = false;
        }
      }
      MenuItem mis = sender as MenuItem;
      if (mis != null) {
        DisplayTypes? dt = mis.Tag as DisplayTypes?;
        if (dt.HasValue) {
          if (NoteConfig.DisplayType != dt.Value) {
            NoteConfig.DisplayType = dt.Value;
            Config.Save();
            switch (dt.Value) {
              case DisplayTypes.Plain:
                ContentTypeMi.Icon = FindResource("PlainTextIco");
                break;
              case DisplayTypes.Org:
                ContentTypeMi.Icon = FindResource("OrgIco");
                break;
              case DisplayTypes.MarkDown:
                ContentTypeMi.Icon = FindResource("MarkDownIco");
                break;
              case DisplayTypes.Custom:
                ContentTypeMi.Icon = FindResource("CustomIco");
                break;
            }
            WebCon.NavigateToString(NoteConfig.GetContent(BgBru.Color));
          }
        }
      }
    }

    private void PrintCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      if (currentConMenEvent != null) {
        foreach (CoreWebView2ContextMenuItem mi in currentConMenEvent.MenuItems) {
          if (mi.CommandId == INSPECT_CMD || mi.CommandId == PRINT_CMD) {
            e.CanExecute = true;
            break;
          }
        }
      }
    }

    private void PrintCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
      if (currentConMenEvent != null) {
        currentConMenEvent.SelectedCommandId = PRINT_CMD;
      }
    }

    private void SyncCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
      if (!string.IsNullOrEmpty(NoteConfig.File)) {
        IO.FileInfo fi = new IO.FileInfo(NoteConfig.File);
        SyncFileDialog.InitialDirectory = fi.Directory.FullName;
        SyncFileDialog.FileName = fi.Name;
      }
      if (SyncFileDialog.ShowDialog() == Forms.DialogResult.OK) {
        NoteConfig.SetFile(SyncFileDialog.FileName);
        IO.FileInfo fi = new IO.FileInfo(NoteConfig.File);
        Config.Save();
        StopSyncMi.Visibility = NoteConfig.IsFileMapped() ? Visibility.Visible : Visibility.Collapsed;
        UpdateOrCreateSyncFileWhatcher();
      }
    }

    public void UpdateSyncBindingTargets() {
      FileOkImg.GetBindingExpression(Image.VisibilityProperty).UpdateTarget();
      FileFailedImg.GetBindingExpression(Image.VisibilityProperty).UpdateTarget();
      FileActiveImg.GetBindingExpression(Image.VisibilityProperty).UpdateTarget();
    }

    private void StopSyncCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
      NoteConfig.File = null;
      Config.Save();
      StopSyncMi.Visibility = NoteConfig.IsFileMapped() ? Visibility.Visible : Visibility.Collapsed;
      UpdateOrCreateSyncFileWhatcher();
      UpdateSyncBindingTargets();
    }

    private void ReloadCmd_Executed(object sender, ExecutedRoutedEventArgs e) {
      if (!loaded)
        return;
      WebCon.NavigateToString(NoteConfig.GetContent(BgBru.Color));
    }

    private void ColorMi_Checked(object sender, RoutedEventArgs e) {
      if (!loaded)
        return;
      foreach (MenuItem mi in ColorMi.Items) {
        if (mi != sender) {
          mi.IsChecked = false;
        }
      }
      MenuItem mis = sender as MenuItem;
      if (mis != null) {
        DisplayColors? col = mis.Tag as DisplayColors?;
        if (col.HasValue) {
          if (NoteConfig.Color != col.Value) {
            NoteConfig.Color = col.Value;
            Config.Save();
            Color color = (Color)App.Current.FindResource("BgColorY");
            Color titleColor = (Color)App.Current.FindResource("TitleBgColorY");
            switch (NoteConfig.Color) {
              case DisplayColors.Orange:
                color = (Color)App.Current.FindResource("BgColorO");
                titleColor = (Color)App.Current.FindResource("TitleBgColorO");
                break;
              case DisplayColors.Red:
                color = (Color)App.Current.FindResource("BgColorR");
                titleColor = (Color)App.Current.FindResource("TitleBgColorR");
                break;
              case DisplayColors.Pink:
                color = (Color)App.Current.FindResource("BgColorP");
                titleColor = (Color)App.Current.FindResource("TitleBgColorP");
                break;
              case DisplayColors.Blue:
                color = (Color)App.Current.FindResource("BgColorB");
                titleColor = (Color)App.Current.FindResource("TitleBgColorB");
                break;
              case DisplayColors.Green:
                color = (Color)App.Current.FindResource("BgColorG");
                titleColor = (Color)App.Current.FindResource("TitleBgColorG");
                break;
            }
            BgBru.Color = color;
            TitleBgBru.Color = titleColor;
            ((Rectangle)ColorMi.Icon).Fill = new SolidColorBrush(color);
            WebCon.NavigateToString(NoteConfig.GetContent(BgBru.Color));
          }
        }
      }
    }

    private void FileFailedImg_MouseDown(object sender, MouseButtonEventArgs e) {
      NoteConfig.SaveToFile(true);
    }
  }
}