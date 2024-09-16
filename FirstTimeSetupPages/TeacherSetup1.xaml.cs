/*
 * Copyright 2024 Đorđe Mančić
 */
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace SavaMonitor.FirstTimeSetupPages
{
    public partial class TeacherSetup1 : Page
    {
        App app;
        public TeacherSetup1()
        {
            InitializeComponent();
            app = Application.Current as App;
            app.Classroom = new Classroom();
            app.LocalComputer = new Teacher();
            TeacherIDTextBox.Text = app.LocalComputer.ID;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(ClassroomNameTextBox.Text))
            {
                MessageBox.Show("You must enter the classroom name", "Setup error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (String.IsNullOrWhiteSpace(ClassroomIDTextBox.Text))
            {
                MessageBox.Show("You must enter the classroom ID", "Setup error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int portNumber;
            if (int.TryParse(ClassroomPortTextBox.Text, out portNumber) == false)
            {
                MessageBox.Show("You must enter the classroom port", "Setup error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            app.Classroom.Name = ClassroomNameTextBox.Text;
            app.Classroom.ID = ClassroomIDTextBox.Text;
            app.Classroom.Port = portNumber;

            if (StaticIPCheckBox.IsChecked == true)
            {
                app.Classroom.StaticIPAddress = StaticIPTextBox.Text;
            }
            

            TeacherFinish finishPage = new TeacherFinish();
            this.NavigationService.Navigate(finishPage);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }

        private void ClassroomNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Regex regex = new Regex(@"\d+");
            if (regex.IsMatch(ClassroomNameTextBox.Text) && ClassroomIDTextBox != null)
            {
                ClassroomIDTextBox.Text = regex.Match(ClassroomNameTextBox.Text).Value;
            }
        }

        private void RefreshIDButton_Click(object sender, RoutedEventArgs e)
        {
            app.LocalComputer.GenerateID();
            TeacherIDTextBox.Text = app.LocalComputer.ID;
        }

        private void StaticIPCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            StaticIPTextBox.IsEnabled = true;
        }

        private void StaticIPCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            StaticIPTextBox.IsEnabled = false;
        }
    }
}
