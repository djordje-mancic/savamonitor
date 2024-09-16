/*
 * Copyright 2024 Đorđe Mančić
 */
using SavaMonitor.TeacherPrompts;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SavaMonitor.TeacherViewPages
{
    public partial class GridViewPage : Page
    {
        App application;
        public int RowAmount { get; set; }
        public GridViewPage()
        {
            application = (App)Application.Current;
            InitializeComponent();
            if (application.Classroom.StudentList.Count <= 6)
            {
                RowAmount = 2;
            }
            else
            {
                RowAmount = (int)Math.Ceiling(application.Classroom.StudentList.Count / 3F);
            }
            this.DataContext = this;
            GridObject.ItemsSource = application.Classroom.StudentList;

        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Student student = (Student)(sender as ListViewItem).DataContext;
            ShowStudentScreen showStudentScreen = new ShowStudentScreen
            {
                DataContext = student
            };
            showStudentScreen.Owner = application.MainWindow;
            showStudentScreen.ShowDialog();
        }
    }
}
