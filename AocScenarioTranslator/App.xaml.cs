using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;

namespace YTY.AocScenarioTranslator
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
  }
  public static class My
  {
    public static App App => Application.Current as App;

    public static ProgramViewModel ProgramViewModel => App.FindResource(nameof(ProgramViewModel)) as ProgramViewModel;

  }
}
