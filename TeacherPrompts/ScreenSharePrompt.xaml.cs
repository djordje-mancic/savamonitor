/*
 * Copyright 2024 Đorđe Mančić
 */
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;

namespace SavaMonitor.TeacherPrompts
{
    public partial class ScreenSharePrompt : Window
    {
        private List<Student> studentsToShareTo;
        bool isStarting = false;
        App application;

        public ScreenSharePrompt(List<Student> studentsToShareTo)
        {
            InitializeComponent();
            application = (App)Application.Current;
            this.studentsToShareTo = studentsToShareTo;
        }

        private async void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Starting share...");
            if (isStarting == true) return;
            isStarting = true;
            MainWindow mainWindow = (MainWindow)application.MainWindow;
            mainWindow.ShareScreenButton.Visibility = Visibility.Collapsed;
            mainWindow.StopSharingScreenButton.Visibility = Visibility.Visible;
            application.LocalComputer.IsSharingScreen = true;

            string shareSizeType = "Fullscreen";
            if (WindowedRadioButton.IsChecked == true)
            {
                shareSizeType = "Windowed";
            }

            JsonObject shareJson = new JsonObject(){
                ["ShareSizeType"] = shareSizeType
            };
            string jsonString = shareJson.ToJsonString();

            foreach (Student student in studentsToShareTo)
            {
                if (student.IsConnected == true && student.NetworkStream != null)
                {
                    await App.WriteSavaDataToStream(student.NetworkStream, Encoding.Unicode.GetBytes(jsonString), 60);
                }
            }
            Task.Run(application.Classroom.ShareScreen);
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
