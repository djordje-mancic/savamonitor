/*
 * Copyright 2024 Đorđe Mančić
 */
using SavaMonitor.TeacherPrompts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SavaMonitor.TeacherViewPages
{
    public partial class ListViewPage : Page
    {
        App application;
        public ListViewPage()
        {
            InitializeComponent();
            application = (App)Application.Current;
            ListBoxObject.ItemsSource = application.Classroom.StudentList;
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Student student = (Student)(sender as ListBoxItem).DataContext;
            ShowStudentScreen showStudentScreen = new ShowStudentScreen{
                DataContext = student
            };
            showStudentScreen.Owner = application.MainWindow;
            showStudentScreen.ShowDialog();
        }
    }
}
