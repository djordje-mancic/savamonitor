/*
 * Copyright 2024 Đorđe Mančić
 */
using System;
using System.Windows;
using System.Windows.Controls;

namespace SavaMonitor.FirstTimeSetupPages
{
    public partial class StudentSetup1 : Page
    {
        App app;
        public StudentSetup1()
        {
            InitializeComponent();
            app = Application.Current as App;
            app.Classroom = new Classroom();
            app.LocalComputer = new Student();
            StudentIDTextBox.Text = app.LocalComputer.ID;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(StudentNameTextBox.Text))
            {
                MessageBox.Show("You must enter the student computer's name", "Setup error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            app.LocalComputer.Name = StudentNameTextBox.Text;
            app.LocalComputer.ID = StudentIDTextBox.Text;
            app.Classroom.ID = ClassroomIDTextBox.Text;
            app.Classroom.Port = portNumber;

            if (StaticIPCheckBox.IsChecked == true)
            {
                app.Classroom.StaticIPAddress = StaticIPTextBox.Text;
            }
            else
            {
                app.Classroom.StaticIPAddress = null;
            }

            StudentClassroomConnecting classroomConnectingPage = new StudentClassroomConnecting();
            this.NavigationService.Navigate(classroomConnectingPage);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }

        private void RefreshIDButton_Click(object sender, RoutedEventArgs e)
        {
            app.LocalComputer.GenerateID();
            StudentIDTextBox.Text = app.LocalComputer.ID;
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
