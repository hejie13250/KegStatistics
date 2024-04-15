using LiveChartsCore;
using System.Configuration;
using System.Data;
using System.Windows;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System;

namespace 小科狗统计
{
  /// <summary>
  /// App.xaml 的交互逻辑
  /// </summary>
  public partial class App : Application
  {
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      Mutex mutex = new (true, "小科狗打字统计", out bool isNewInstance);

      if (!isNewInstance)
      {
        // 已有实例运行，将焦点设置到已有实例
        IntPtr mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
        if (mainWindowHandle != IntPtr.Zero)
        {
          SetForegroundWindow(mainWindowHandle);
        }
        Shutdown();
      }


      LiveCharts.Configure(config =>
          config
              // you can override the theme 
              //.AddDarkTheme()

              // In case you need a non-Latin based font, you must register a typeface for SkiaSharp
              .HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('汉')) // <- Chinese 
              //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('あ')) // <- Japanese 
              //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('헬')) // <- Korean 
              //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('Ж'))  // <- Russian 
              //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('أ'))  // <- Arabic 
              //.UseRightToLeftSettings() // Enables right to left tooltips 

              // finally register your own mappers
              // you can learn more about mappers at:
              // https://livecharts.dev/docs/wpf/2.0.0-rc2/Overview.Mappers

              // here we use the index as X, and the population as Y 
              //.HasMap<City>((city, index) => new(index, city.Population))
              // .HasMap<Foo>( .... ) 
              // .HasMap<Bar>( .... ) 
              );
    }

    //public record City(string Name, double Population);

  }

}
