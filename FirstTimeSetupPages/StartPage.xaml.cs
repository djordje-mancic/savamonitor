/*
 * Copyright 2024 Đorđe Mančić
 */
using System.Windows;
using System.Windows.Controls;

namespace SavaMonitor.FirstTimeSetupPages
{
    public partial class StartPage : Page
    {
        public StartPage()
        {
            InitializeComponent();
        }

        private void TeacherButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new TeacherSetup1());
        }

        private void StudentButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new StudentSetup1());
        }

        private void AboutHyperlink_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }
    }
}
