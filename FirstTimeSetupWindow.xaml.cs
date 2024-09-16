/*
 * Copyright 2024 Đorđe Mančić
 */
using System.Windows;

namespace SavaMonitor
{
    public partial class FirstTimeSetupWindow : Window
    {
        public FirstTimeSetupWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new FirstTimeSetupPages.StartPage());
        }
    }
}
