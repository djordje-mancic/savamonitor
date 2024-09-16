/*
 * Copyright 2024 Đorđe Mančić
 */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SavaMonitor.TeacherPrompts
{
    public partial class AddStudentPrompt : Window
    {
        App application;
        string pairingNumber;
        public AddStudentPrompt()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            application = (App)Application.Current;
            application.Classroom.AcceptsStudents = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            application.Classroom.AcceptsStudents = false;
        }

        public void StartAddProcess(string pairingNumber)
        {
            StudentInfoText.Text = "Computer '" + application.Classroom.StudentJoining.Name + "', ID " + application.Classroom.StudentJoining.ID + " wants to join the classroom.";
            WaitingGrid.Visibility = Visibility.Collapsed;
            PairingGrid.Visibility = Visibility.Visible;
            this.pairingNumber = pairingNumber;
        }

        private async void ConfirmNumberInput()
        {
            if (PairingNumberTextBox.Text.Trim() == pairingNumber)
            {
                ResultText.Text = "Computer successfully added to the classroom!";
                await application.Classroom.ApproveStudentJoining();
            }
            else
            {
                ResultText.Text = "4 digit pairing number incorrect. Please try again and make sure you're entering the number from the correct computer.";
                await application.Classroom.DeclineStudentJoining();
            }
            PairingGrid.Visibility = Visibility.Collapsed;
            ResultsGrid.Visibility = Visibility.Visible;
        }

        private async void ConfirmNumberButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmNumberInput();
        }

        private void PairingNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PairingNumberTextBox.Text.Length > 4)
            {
                PairingNumberTextBox.Text = PairingNumberTextBox.Text.Remove(4);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            ResultText.Text = "Student computer join request has been declined.";
            await application.Classroom.DeclineStudentJoining();
            PairingGrid.Visibility = Visibility.Collapsed;
            ResultsGrid.Visibility = Visibility.Visible;
        }

        private async void PairingNumberTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ConfirmNumberInput();
            }
        }
    }
}
