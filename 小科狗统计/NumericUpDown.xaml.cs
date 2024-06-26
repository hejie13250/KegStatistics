﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace 小科狗统计
{
  /// <summary>
  /// NumericUpDown.xaml 的交互逻辑
  /// </summary>
  public partial class NumericUpDown : UserControl
  {
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(double), typeof(NumericUpDown),
            new FrameworkPropertyMetadata(4.50, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValuePropertyChanged));

    public static readonly DependencyProperty MinValueProperty =
        DependencyProperty.Register("MinValue", typeof(double), typeof(NumericUpDown), new PropertyMetadata(0.0));

    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register("MaxValue", typeof(double), typeof(NumericUpDown), new PropertyMetadata(100.0));

    public event RoutedPropertyChangedEventHandler<double> ValueChanged;

    public double Value
    {
      get { return (double)GetValue(ValueProperty); }
      set { SetValue(ValueProperty, value); }
    }

    public double MinValue
    {
      get { return (double)GetValue(MinValueProperty); }
      set { SetValue(MinValueProperty, value); }
    }

    public double MaxValue
    {
      get { return (double)GetValue(MaxValueProperty); }
      set { SetValue(MaxValueProperty, value); }
    }

    public NumericUpDown()
    {
      InitializeComponent();
      this.DataContext = this;
    }

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      NumericUpDown numericUpDown = (NumericUpDown)d;
      double oldValue = (double)e.OldValue;
      double newValue = (double)e.NewValue;

      if (numericUpDown.ValueChanged != null)
      {
        // 创建事件参数并调用事件
        RoutedPropertyChangedEventArgs<double> args = new RoutedPropertyChangedEventArgs<double>(oldValue, newValue);
        numericUpDown.ValueChanged(numericUpDown, args);
      }
    }

    protected virtual void OnValueChanged(RoutedPropertyChangedEventArgs<double> args)
    {
      ValueChanged?.Invoke(this, args);
    }

    private void Increase_Click(object sender, RoutedEventArgs e)
    {
      if (Value < MaxValue) Value += 0.1;
    }

    private void Decrease_Click(object sender, RoutedEventArgs e)
    {
      if (Value > MinValue) Value -= 0.1;
    }

    private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      double step = 0.1; // 设置滚动步长，默认每次滚动增加或减少0.1
      if (Keyboard.Modifiers == ModifierKeys.Control) step *= 0.1; // 如果按住Ctrl键，则步长 0.01

      if (e.Delta > 0) // 滚动向上
      {
        if (Value + step <= MaxValue)
        {
          Value += step;
        }

      }
      else if (e.Delta < 0) // 滚动向下
      {
        if (Value - step >= MinValue)
          Value -= step;
      }

      Value = Math.Round(Value, 4);
      // 阻止滚轮事件继续向上冒泡
      e.Handled = true;
    }

    // 你可能还需要实现其他逻辑来确保Value的值始终在MinValue和MaxValue之间
  }
}
