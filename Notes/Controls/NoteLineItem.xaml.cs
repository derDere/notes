using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Notes.Controls {
  /// <summary>
  /// Interaktionslogik für NoteLineItem.xaml
  /// </summary>
  public partial class NoteLineItem : UserControl {

    public NoteLineItem() {
      InitializeComponent();

      Config.ConfigReloaded += Config_SavedReloadedChanged;
      Config.ConfigSaved += Config_SavedReloadedChanged;
      Config.ConfigChanged += Config_SavedReloadedChanged;
    }

    private void Config_SavedReloadedChanged(EventArgs e) {
      this.GetBindingExpression(DataContextProperty).UpdateTarget();
      ContentCC.GetBindingExpression(ContentProperty).UpdateTarget();
      ShowBtn.GetBindingExpression(VisibilityProperty).UpdateTarget();
      HideBtn.GetBindingExpression(VisibilityProperty).UpdateTarget();
    }

    private void ShowBtn_Click(object sender, RoutedEventArgs e) {
      NoteConfig noteConfig = DataContext as NoteConfig;
      noteConfig?.Show();
      ShowBtn.GetBindingExpression(VisibilityProperty).UpdateTarget();
      HideBtn.GetBindingExpression(VisibilityProperty).UpdateTarget();
    }

    private void HideBtn_Click(object sender, RoutedEventArgs e) {
      NoteConfig noteConfig = DataContext as NoteConfig;
      noteConfig?.Hide();
      ShowBtn.GetBindingExpression(VisibilityProperty).UpdateTarget();
      HideBtn.GetBindingExpression(VisibilityProperty).UpdateTarget();
    }

    private void DeleteBtn_Click(object sender, RoutedEventArgs e) {
      NoteConfig noteConfig = DataContext as NoteConfig;
      noteConfig?.Delete();
    }

    private void ContentCC_MouseDown(object sender, MouseButtonEventArgs e) {
      NoteConfig noteConfig = DataContext as NoteConfig;
      if (noteConfig != null) {
        if (noteConfig.Visible) {
          noteConfig?.Show();
        }
      }
    }
  }
}
