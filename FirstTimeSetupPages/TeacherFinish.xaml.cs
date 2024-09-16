/*
 * Copyright 2024 Đorđe Mančić
 */
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SavaMonitor.FirstTimeSetupPages
{
    public partial class TeacherFinish : Page
    {
        public TeacherFinish()
        {
            InitializeComponent();
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            app.Classroom.Teacher = (Teacher)app.LocalComputer;
            Task.Run(app.Classroom.StartHosting);

            app.MainWindow = new MainWindow();
            app.MainWindow.Show();

            FirstTimeSetupWindow currentWindow = Application.Current.Windows.OfType<FirstTimeSetupWindow>().ElementAt(0);
            currentWindow.Close();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);
            App app = Application.Current as App;

            ProgressListBox.Items.Add("Creating configuration file for classroom...");
            string classroomJsonString = JsonSerializer.Serialize(app.Classroom);
            Byte[] classroomJsonBytes = Encoding.Unicode.GetBytes(classroomJsonString);
            app.ClassroomConfigFileStream = File.Create("classroom.json");
            ProgressBar.Value = 25;

            ProgressListBox.Items.Add("Writing classroom data to file...");
            await app.ClassroomConfigFileStream.WriteAsync(classroomJsonBytes);
            await app.ClassroomConfigFileStream.FlushAsync();

            ProgressBar.Value = 50;

            ProgressListBox.Items.Add("Creating configuration file for teacher computer...");
            string teacherJsonString = JsonSerializer.Serialize<Teacher>((Teacher)app.LocalComputer);
            Byte[] teacherJsonBytes = Encoding.Unicode.GetBytes(teacherJsonString);
            app.LocalComputerConfigFileStream = File.Create("teacher.json");
            ProgressBar.Value = 75;

            ProgressListBox.Items.Add("Writing teacher computer data to file...");
            await app.LocalComputerConfigFileStream.WriteAsync(teacherJsonBytes);
            await app.LocalComputerConfigFileStream.FlushAsync();

            //End of configuring
            ProgressListBox.Items.Add("Successfully finished setup.");
            ProgressBar.Value = 100;

            TitleLabel.Content = "Setup finished!";
            DescriptionLabel.Content = "Click 'Next' to open the program.";
            FinishButton.IsEnabled = true;

        }
    }
}
