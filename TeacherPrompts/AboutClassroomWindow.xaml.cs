/*
 * Copyright 2024 Đorđe Mančić
 */
using System.Windows;

namespace SavaMonitor.TeacherPrompts
{
    public partial class AboutClassroomWindow : Window
    {
        public AboutClassroomWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App application = (App)Application.Current;
            InfoListBox.Items.Add("Classroom ID: " + application.Classroom.ID);
            InfoListBox.Items.Add("Classroom name: " + application.Classroom.Name);
            InfoListBox.Items.Add("Classroom port: " + application.Classroom.Port.ToString());
            if (application.Classroom.StaticIPAddress != null)
            {
                InfoListBox.Items.Add("Static IP address: " + application.Classroom.StaticIPAddress.ToString());
            }
            else
            {
                InfoListBox.Items.Add("Static IP address: not set");
            }
            InfoListBox.Items.Add("Amount of student computers: " + application.Classroom.StudentList.Count.ToString());
            InfoListBox.Items.Add("Amount of active student computers: " + application.Classroom.StudentList.FindAll(student => student.IsConnected == true).Count.ToString());
        }
    }
}
