using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Notes {
  /// <summary>
  /// Interaktionslogik für MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {

    public static MainWindow Instance { get; private set; }

    public NoteConfig[] Notes {
      get {
        return Config.Notes.ToArray();
      }
      set { }
    }

    public MainWindow() {
      InitializeComponent();
      Instance = this;
    }

    private void Config_Changed(EventArgs e) {
      Config.Save();
      this.GetBindingExpression(DataContextProperty).UpdateTarget();
    }

    private void Node_ListChanged(object sender, ManagedList<NoteConfig>.ListChangedEventArgs e) {
      Config.Save();
      this.GetBindingExpression(DataContextProperty).UpdateTarget();
    }

    private void Win_Loaded(object sender, RoutedEventArgs e) {
      Config.Notes.ListChanged += Node_ListChanged;
      Config.ConfigChanged += Config_Changed;
      Config.ConfigReloaded += Config_Changed;

      Config.Notes.Where(nc => nc.Visible).ToList().ForEach(nc => nc.Show());
    }

    private void NewNoteTBtn_Click(object sender, EventArgs e) {
      NoteConfig nc = new NoteConfig();
      Config.Notes.Add(nc);
      this.GetBindingExpression(DataContextProperty).UpdateTarget();
      nc.Show();
    }
  }
}
