/*
 * Copyright 2024 Đorđe Mančić
 */
using System.Threading.Tasks;
using System.Windows;

namespace SavaMonitor
{
    public partial class ScreenShareWindow : Window
    {
        public ScreenShareWindow()
        {
            InitializeComponent();
        }

        public string ShareSizeType;

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ShareSizeType == "Windowed")
            {
                this.WindowStyle = WindowStyle.ThreeDBorderWindow;
                this.Topmost = false;
                this.ShowInTaskbar = true;
                this.WindowState = WindowState.Maximized;
            }
            await Task.Delay(2000);
            SharingSoonLabel.Visibility = Visibility.Collapsed;
        }
    }
}
