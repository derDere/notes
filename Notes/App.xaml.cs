using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Notes {
  /// <summary>
  /// Interaktionslogik für "App.xaml"
  /// </summary>
  public partial class App : Application {

    public class ExceptionMessage {
      public string Message { get; private set; }
      public Exception Ex { get; private set; }
      public ExceptionMessage(string message, Exception ex) {
        this.Message = message;
        this.Ex = ex;
      }
    }

    public static readonly ManagedList<ExceptionMessage> OccuredExceptions = new ManagedList<ExceptionMessage>();

  }
}
