using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;




namespace 小科狗统计
{
  /// <summary>
  /// MainWindow.xaml 的交互逻辑
  /// </summary>
  public partial class MainWindow : Window
  {
    #region 消息接口
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr pdwResult);

    const uint ABORTIFHUNG = 0x0002;
    readonly uint flags = (uint)(ABORTIFHUNG);
    readonly uint timeout = 500;
    const int WM_USER = 0x0400;               // 根据Windows API定义
    const uint KWM_GETALLZSTJ = (uint)WM_USER + 214;  //把字数与速度的所有统计数据吐到剪切板 格式见字数统计界面的样子,具体见剪切板
    #endregion

    #region 读写配置文件项
    // 读写配置项 API
    [DllImport("kernel32", CharSet = CharSet.Unicode)]// 读配置文件方法的6个参数：所在的分区、   键值、      初始缺省值、         StringBuilder、      参数长度上限 、配置文件路径
    private static extern long GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder retVal, int size, string filePath);
    [DllImport("kernel32", CharSet = CharSet.Unicode)]// 写入配置文件方法的4个参数：所在的分区、    键值、      参数值、      配置文件路径
    private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

    /// <summary>
    /// 写配置文件
    /// </summary>
    /// <param name="section">配置项</param>
    /// <param name="key">键</param>
    /// <param name="value">命令行</param>
    /// <param name="filePath">路径</param>
    public void SetValue(string section, string key, string value)
    {
      WritePrivateProfileString(section, key, value, settingConfigFile);
    }

    /// <summary>
    /// 读配置文件
    /// </summary>
    /// <param name="section">配置项</param>
    /// <param name="key">键</param>
    /// <param name="filePath">路径</param>
    /// <returns>命令行</returns>
    public string GetValue(string section, string key)
    {
      if (File.Exists(settingConfigFile))
      {
        StringBuilder sb = new(255);
        GetPrivateProfileString(section, key, "", sb, 255, settingConfigFile);
        return sb.ToString();
      }
      else return string.Empty;
    }

    // 读配置项
    private void ReadConfig()
    {
      settingConfigFile = appPath + "\\窗口配置.ini";
      DZcheckBox.IsChecked = GetValue("dzsjtj", "dz") == "1";
      JJcheckBox.IsChecked = GetValue("dzsjtj", "jj") == "1";
      SPcheckBox.IsChecked = GetValue("dzsjtj", "sp") == "1";
      SCcheckBox.IsChecked = GetValue("dzsjtj", "sc") == "1";
      SDcheckBox.IsChecked = GetValue("dzsjtj", "sd") == "1";
      MCcheckBox.IsChecked = GetValue("dzsjtj", "mc") == "1";
      RQcheckBox.IsChecked = GetValue("dzsjtj", "rq") == "1";
      bool isNumberValid = double.TryParse(GetValue("dzsjtj", "xs"), out double xs);
      nud.Value = isNumberValid ? xs : 4.5;

      isNumberValid = int.TryParse(GetValue("dzsjtj", "ts"), out int xxs);
      comboBox.SelectedIndex = isNumberValid ? xxs : 0;
    }


    #endregion

    #region 定义和初始化
    public class 打字统计数据
    {
      public string[] 日期 { get; set; }
      public double[] 字数 { get; set; }
      public double[] 击键 { get; set; }
      public double[] 上屏 { get; set; }
      public double[] 时长 { get; set; }
      public double[] 累计 { get; set; }
      public double[] 速度 { get; set; }
      public double[] 码长 { get; set; }
    }

    打字统计数据 数据统计 = new();
    string[] 日期;
    double[] 字数;
    double[] 击键;
    double[] 上屏;
    double[] 时长;
    double[] 累计;
    double[] 速度;
    double[] 码长;

    ViewModel viewModel = new();
    MatchCollection matches;

    readonly string appPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    string settingConfigFile = "窗口配置.ini";  // 窗口配置文件
    int ts = 0;


    [Obsolete]
    public MainWindow()
    {
      InitializeComponent();
      DZcheckBox.Click += CheckBox_Click;
      JJcheckBox.Click += CheckBox_Click;
      SPcheckBox.Click += CheckBox_Click;
      SCcheckBox.Click += CheckBox_Click;
      SDcheckBox.Click += CheckBox_Click;
      MCcheckBox.Click += CheckBox_Click;
      RQcheckBox.Click += CheckBox_Click2;
      comboBox.SelectionChanged += ComboBox_SelectionChanged;

      DataContext = new ViewModel();
      ReadConfig();
      GetClipboardData();
      SetData();
      UpViewModelData(数据统计);
      SetControlData();
    }

    #endregion

    #region 数据处理

    // 从剪切板获取数据
    [Obsolete]
    private void GetClipboardData()
    {
      string str;
      try
      {
        IntPtr hWnd = FindWindow("CKegServer_0", null);
        SendMessageTimeout(hWnd, KWM_GETALLZSTJ, IntPtr.Zero, IntPtr.Zero, flags, timeout, out IntPtr pdwResult);
        str = Clipboard.GetText();
      }
      catch
      {
        MessageBox.Show($"操作失败，请重试！");
        return;
      }

      string pattern = @"(\d+).*\t(.*)字.*\t(.*)击.*\t(.*)次.*\t(.*)秒.*\t累计(.*)字";
      matches = Regex.Matches(str, pattern);
    }

    // 数据处理
    private void SetData()
    {
      int count = matches.Count;

      日期 = new string[count];
      字数 = new double[count];
      击键 = new double[count];
      上屏 = new double[count];
      时长 = new double[count];
      累计 = new double[count];
      速度 = new double[count];
      码长 = new double[count];

      if ((bool)RQcheckBox.IsChecked)
      {
        int n = -1;
        foreach (Match match in matches.Cast<Match>())
        {
          n++;  // 正序
          UpData(match, n);
        }
      }
      else
      {
        int n = count;
        foreach (Match match in matches.Cast<Match>())
        {
          n--;  // 倒序
          UpData(match, n);
        }

      }

      数据统计 = new()
      {
        日期 = 日期,
        字数 = 字数,
        击键 = 击键,
        上屏 = 上屏,
        时长 = 时长,
        累计 = 累计,
        速度 = 速度,
        码长 = 码长,
      };
    }

    // 更新数据
    private void UpData(Match match, int n)
    {
      string year = match.Groups[1].Value.Substring(0, 4);
      string month = match.Groups[1].Value.Substring(4, 2);
      string day = match.Groups[1].Value.Substring(6, 2);

      日期[n] = $"{year}-{month}-{day}";
      字数[n] = double.Parse(match.Groups[2].Value);
      击键[n] = double.Parse(match.Groups[3].Value);
      上屏[n] = double.Parse(match.Groups[4].Value);
      时长[n] = double.Parse(match.Groups[5].Value);
      累计[n] = double.Parse(match.Groups[6].Value);
      速度[n] = (double)(字数[n] / (时长[n] / 60 + nud.Value * 上屏[n] * (时长[n] / 60 / 击键[n])));
      码长[n] = Math.Round(击键[n] / 字数[n], 1);
    }

    // 更新数据到图表
    [Obsolete]
    private void UpViewModelData(打字统计数据 全部数据)
    {

      for (int i = 0; i < 时长.Length; i++)
      {
        全部数据.时长[i] = Math.Round(全部数据.时长[i], 2);
        全部数据.速度[i] = Math.Round(全部数据.速度[i], 2);
      }

      // 如果天数为 0 或大于等于数据总数，则使用全部数据，否则就截取指定天数的数据
      打字统计数据 数据片段 = new();
      if (ts == 0 || ts >= matches.Count)
      {
        数据片段 = 全部数据;
      }
      else
      {
        int startIndex;
        int count  = matches.Count > 15 ? ts : matches.Count;
        startIndex = matches.Count > 15 ? matches.Count - ts : 0;

        数据片段.日期 = 全部数据.日期.Skip(startIndex).Take(count).ToArray(); ;
        数据片段.字数 = 全部数据.字数.Skip(startIndex).Take(count).ToArray(); ;
        数据片段.击键 = 全部数据.击键.Skip(startIndex).Take(count).ToArray(); ;
        数据片段.上屏 = 全部数据.上屏.Skip(startIndex).Take(count).ToArray(); ;
        数据片段.时长 = 全部数据.时长.Skip(startIndex).Take(count).ToArray(); ;
        数据片段.速度 = 全部数据.速度.Skip(startIndex).Take(count).ToArray(); ;
        数据片段.码长 = 全部数据.码长.Skip(startIndex).Take(count).ToArray(); ;
      }

      viewModel = new()
      {
        Series = new ISeries[]
        {
          new LineSeries<double> // 曲线图
          {
              Values = 数据片段.击键,
              Name = "击键（次）",
              //Fill = null,
              GeometrySize = 0, //圆点尺寸
              //LineSmoothness = 0, // 0为直线，1为圆弧
              //DataPadding = new LvcPoint(-200, 0),
              IsVisible = (bool)DZcheckBox.IsChecked, // 显示/隐藏
          },
          new LineSeries<double>
          {
              Values = 数据片段.字数,
              Name = "字数（个）",
              //Fill = null,
              GeometrySize = 0,
              IsVisible = (bool)JJcheckBox.IsChecked

          },
          new LineSeries<double>
          {
              Values = 数据片段.上屏,
              Name = "上屏（个）",
              //Fill = null,
              GeometrySize = 0,
              IsVisible = (bool)SPcheckBox.IsChecked
          },
          new LineSeries<double>
          {
              Values = 数据片段.时长,
              Name = "用时（秒）",
              //Fill = null,
              GeometrySize = 0,
              IsVisible = (bool)SCcheckBox.IsChecked
          },
          new LineSeries<double>
          {
              Values = 数据片段.速度,
              Name = "速度（字/分）",
              //Fill = null,
              GeometrySize = 0,
              IsVisible = (bool)SDcheckBox.IsChecked
          },
          new LineSeries<double>
          {
              Values = 数据片段.码长,
              Name = "码长（码）",
              Fill = null,
              GeometrySize = 0,
              IsVisible = (bool)MCcheckBox.IsChecked
          },

        },
        XAxes = new Axis[]
        {
          new()
          {
              // 分隔字体颜色和大小
              LabelsPaint = new SolidColorPaint(SKColors.Blue),
              TextSize = 12,
              Labels =  数据片段.日期,
              LabelsRotation = -30,
              // 颜色和线粗
              SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) { StrokeThickness = 1 },

          }
        },
        YAxes = new Axis[]
        {
          new ()
          {
            LabelsPaint = new SolidColorPaint(SKColors.Green),
            TextSize = 20,

            SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray)
              {
                StrokeThickness = 1,
                PathEffect = new DashEffect(new float[] { 3, 8 }) //设为虚线，3和8为实线和留空大小
              }
          }
        },
      };

      LiveCharts.Series = viewModel.Series;
      LiveCharts.XAxes = viewModel.XAxes;
      LiveCharts.YAxes = viewModel.YAxes;
    }

    // 更新控件上的值
    private void SetControlData()
    {
      Match match = matches[0]; // 今天的数据

      double zs = double.Parse(match.Groups[2].Value); //字数
      double jj = double.Parse(match.Groups[3].Value); //击键
      double sp = double.Parse(match.Groups[4].Value); //上屏
      double sc = double.Parse(match.Groups[5].Value); //时长
      double sd = (double)(zs / (sc / 60 + nud.Value * sp * (sc / 60 / jj)));  //速度
      double mc = Math.Round(jj / zs, 1);  //码长

      // 累计的数据
      double ljzs = double.Parse(match.Groups[6].Value); //累计字数
      double ljjj = 0;  //累计击键
      double ljsp = 0;  //累计上屏
      double ljsc = 0;  //累计时长
      for (int i = 0; i < 字数.Length; i++)
      {
        ljjj += 击键[i];
        ljsp += 上屏[i];
        ljsc += 时长[i];
      }
      double ljsd = (double)(ljzs / (ljsc / 60 + nud.Value * ljsp * (ljsc / 60 / ljjj)));  //累计速度
      double ljmc = Math.Round(ljjj / ljzs, 1);  //累计码长

      zsTextBlock.Text = String.Format("{0:#,###0}", zs); //字数
      jjTextBlock.Text = String.Format("{0:#,###0}", jj); //击键
      spTextBlock.Text = String.Format("{0:#,###0}", sp); //上屏
      scTextBlock.Text = $"{sc / 60:0.00}" + " 分"; //时长
      sdTextBlock.Text = $"{sd:0.00}" + " 字/分";  //速度
      mcTextBlock.Text = $"{mc:0.00}";  //码长

      ljzsTextBlock.Text = String.Format("{0:#,###0}", ljzs); //累计字数
      ljjjTextBlock.Text = String.Format("{0:#,###0}", ljjj); //累计击键
      ljspTextBlock.Text = String.Format("{0:#,###0}", ljsp); //累计上屏
      ljscTextBlock.Text = $"{ljsc / 3600:0.00}" + " 时"; //累计时长
      ljsdTextBlock.Text = $"{ljsd:0.00}" + " 字/分"; //累计速度
      ljmcTextBlock.Text = $"{ljmc:0.00}"; //累计码长

    }

    // 数据选择改变后更新
    [Obsolete]
    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
      UpViewModelData(数据统计);
    }

    [Obsolete]
    private void CheckBox_Click2(object sender, RoutedEventArgs e)
    {
      SetData();
      UpViewModelData(数据统计);
    }

    [Obsolete]
    private void Nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (matches != null)
      {
        nud.Value = Math.Round(nud.Value, 2);
        UpViewModelData(数据统计);
        SetControlData();
      }
    }

    [Obsolete]
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      switch (comboBox.SelectedIndex)
      {
        case 0: ts = 15; break;
        case 1: ts = 30; break;
        case 2: ts = 60; break;
        case 3: ts = 90; break;
        case 4: ts = 120; break;
        case 5: ts = 180; break;
        case 6: ts = 365; break;
        case 7: ts = 730; break;
        case 8: ts = 0; break;
      }
      if (matches != null)
      {
        UpViewModelData(数据统计);
      }
    }

    #endregion

    #region 窗口相关
    // 移动窗口
    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
      // 只有当用户按下左键时才处理
      if (e.LeftButton == MouseButtonState.Pressed)
      {
        // 调用API使窗口跟随鼠标移动
        DragMove();
      }
    }

    // 窗口最小化
    private void MinimizedButton_Click(object sender, RoutedEventArgs e)
    {
      WindowState = WindowState.Minimized;
    }

    private void MaximizedButton_Click(object sender, RoutedEventArgs e)
    {
      WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
    }

    private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
    }
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      LiveCharts.Height = Height - 50;
    }
    // 关闭
    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if ((bool)DZcheckBox.IsChecked) { SetValue("dzsjtj", "dz", "1"); } else SetValue("dzsjtj", "dz", "0");
      if ((bool)JJcheckBox.IsChecked) { SetValue("dzsjtj", "jj", "1"); } else SetValue("dzsjtj", "jj", "0");
      if ((bool)SPcheckBox.IsChecked) { SetValue("dzsjtj", "sp", "1"); } else SetValue("dzsjtj", "sp", "0");
      if ((bool)SCcheckBox.IsChecked) { SetValue("dzsjtj", "sc", "1"); } else SetValue("dzsjtj", "sc", "0");
      if ((bool)SDcheckBox.IsChecked) { SetValue("dzsjtj", "sd", "1"); } else SetValue("dzsjtj", "sd", "0");
      if ((bool)MCcheckBox.IsChecked) { SetValue("dzsjtj", "mc", "1"); } else SetValue("dzsjtj", "mc", "0");
      if ((bool)RQcheckBox.IsChecked) { SetValue("dzsjtj", "rq", "1"); } else SetValue("dzsjtj", "rq", "0");
      SetValue("dzsjtj", "xs", nud.Value.ToString());
      SetValue("dzsjtj", "ts", comboBox.SelectedIndex.ToString());
    }



    #endregion


  }
}