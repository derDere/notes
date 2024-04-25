using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Notes {
  public static class CustomCommands {
    public static readonly RoutedCommand Shuffle = new RoutedCommand(
      "Shuffle",
      typeof(CustomCommands),
      new InputGestureCollection() {
        new KeyGesture(Key.R, ModifierKeys.Alt)
      }
    );

    public static readonly RoutedCommand SyncToDocument = new RoutedCommand(
      "Sync to Document",
      typeof(CustomCommands),
      new InputGestureCollection() {
        new KeyGesture(Key.S, ModifierKeys.Control)
      }
    );

    public static readonly RoutedCommand StopSyncToDocument = new RoutedCommand(
      "Stop Sync to Document",
      typeof(CustomCommands),
      new InputGestureCollection() { }
    );

    public static readonly RoutedCommand Reload = new RoutedCommand(
      "Reload",
      typeof(CustomCommands),
      new InputGestureCollection() { }
    );
  }
}
