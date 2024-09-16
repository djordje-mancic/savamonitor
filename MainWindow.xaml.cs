/*
 * Copyright 2024 Đorđe Mančić
 */
using SavaMonitor.TeacherPrompts;
using SavaMonitor.TeacherViewPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace SavaMonitor
{
    public partial class MainWindow : Window
    {
        App application;
        public MainWindow()
        {
            InitializeComponent();
            application = (App)Application.Current;

            //Add taskbar icon
            application.TaskbarIcon = new NotifyIcon();
            application.TaskbarIcon.Icon = SavaMonitor.Properties.Resources.sava;
            application.TaskbarIcon.Text = "Sava Monitor";
            application.TaskbarIcon.Visible = true;
            application.TaskbarIcon.Click += (o, e) =>
            {
                application.MainWindow.Show();
            };
            
            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add("Quit Sava Monitor", null, (o, e) =>
            {
                application.Shutdown();
            });
            application.TaskbarIcon.ContextMenuStrip = contextMenuStrip;

            //Change page to 0 - List View
            ChangeViewPage(0);
        }

        private void ShareScreenButton_Click(object sender, RoutedEventArgs e)
        {
            List<Student> selectedStudents = GetStudentSelection();
            if (selectedStudents != null && selectedStudents.Count > 0)
            {
                ScreenSharePrompt screenSharePrompt = new ScreenSharePrompt(selectedStudents);
                screenSharePrompt.Owner = this;
                screenSharePrompt.ShowDialog();
            }
            
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            List<Student> selectedStudents = GetStudentSelection();
            if (selectedStudents != null && selectedStudents.Count > 0)
            {
                SendMessagePrompt sendMessagePrompt = new SendMessagePrompt(selectedStudents);
                sendMessagePrompt.Owner = this;
                sendMessagePrompt.ShowDialog();
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.Show();
        }

        private void AddStudentButton_Click(object sender, RoutedEventArgs e)
        {
            AddStudentPrompt addStudentPrompt = new AddStudentPrompt();
            addStudentPrompt.Owner = this;
            addStudentPrompt.ShowDialog();
        }

        private void AboutClassroomButton_Click(object sender, RoutedEventArgs e)
        {
            AboutClassroomWindow aboutClassroomWindow = new AboutClassroomWindow();
            aboutClassroomWindow.Owner = this;
            aboutClassroomWindow.ShowDialog();
        }

        private string selectedLabelText(int count)
        {
            string suffix = " computers selected";
            string countString = count.ToString();
            if (count < 10 || count > 20)
            {
                int lastDigit = Convert.ToInt16(countString.Last<char>().ToString());
                if (lastDigit == 1)
                {
                    suffix = " computer selected";
                }
            }
            return countString + suffix;
        }

        int currentPageIndex = 0;
        private void ChangeViewPage(int pageIndex = -1) // -1 = Just update
        {
            if (pageIndex != -1)
            {
                currentPageIndex = pageIndex;
            }

            if (application.Classroom.StudentList.Count > 0)
            {
                if (pageIndex == 0)
                {
                    ListViewButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(92, 99, 115));
                    GridViewButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(132, 141, 160));
                    ListViewPage listViewPage = new ListViewPage();
                    listViewPage.ListBoxObject.SelectionChanged += delegate
                    {
                        SelectedNumberLabel.Content = selectedLabelText(listViewPage.ListBoxObject.SelectedItems.Count);
                    };
                    StudentViewFrame.NavigationService.Content = listViewPage;
                    SelectedNumberLabel.Content = selectedLabelText(0);
                }
                else if (pageIndex == 1)
                {
                    ListViewButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(132, 141, 160));
                    GridViewButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(92, 99, 115));
                    GridViewPage gridViewPage = new GridViewPage();
                    gridViewPage.GridObject.SelectionChanged += delegate
                    {
                        SelectedNumberLabel.Content = selectedLabelText(gridViewPage.GridObject.SelectedItems.Count);
                    };
                    StudentViewFrame.NavigationService.Content = gridViewPage;
                    SelectedNumberLabel.Content = selectedLabelText(0);
                }
                
            }
            else
            {
                StudentViewFrame.NavigationService.Content = new StartPage();
            }
        }

        public void UpdateViewPage()
        {
            ChangeViewPage(0);
        }

        private List<Student> GetStudentSelection()
        {
            if (StudentViewFrame.Content is ListViewPage)
            {
                ListViewPage listViewPage = (ListViewPage)StudentViewFrame.Content;
                return listViewPage.ListBoxObject.SelectedItems.Cast<Student>().ToList();
            }
            else if (StudentViewFrame.Content is GridViewPage)
            {
                GridViewPage gridViewPage = (GridViewPage)StudentViewFrame.Content;
                return gridViewPage.GridObject.SelectedItems.Cast<Student>().ToList();
            }
            else
            {
                return null;
            }
        }

        bool minimizedNotificationShown = false;
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            await Task.Delay(100);
            if (minimizedNotificationShown == false)
            {
                application.TaskbarIcon.BalloonTipTitle = "Sava Monitor";
                application.TaskbarIcon.BalloonTipText = "Sava Monitor will continue to run in the background. You can open it again by clicking on it's tray icon.";
                application.TaskbarIcon.ShowBalloonTip(5);
                minimizedNotificationShown = true;
            }
        }

        private void ListViewButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeViewPage(0);
        }

        private void GridViewButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeViewPage(1);
        }

        private async void ShutdownButton_Click(object sender, RoutedEventArgs e)
        {
            List<Student> selectedStudents = GetStudentSelection();
            if (selectedStudents != null && selectedStudents.Count > 0)
            {
                foreach (Student student in selectedStudents)
                {
                    if (student.IsConnected == true && student.TCPClient != null && student.NetworkStream != null)
                    {
                        await App.WriteSavaDataToStream(student.NetworkStream, null, 50);
                    }
                }
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (StudentViewFrame.Content is ListViewPage)
            {
                ListViewPage listViewPage = (ListViewPage)StudentViewFrame.Content;
                listViewPage.ListBoxObject.SelectAll();
            }
            else if (StudentViewFrame.Content is GridViewPage)
            {
                GridViewPage gridViewPage = (GridViewPage)StudentViewFrame.Content;
                gridViewPage.GridObject.SelectAll();
            }
        }

        private void StopSharingScreenButton_Click(object sender, RoutedEventArgs e)
        {
            application.LocalComputer.IsSharingScreen = false;
            foreach (Student student in application.Classroom.StudentList)
            {
                if (student.IsConnected == true && student.TCPClient != null && student.NetworkStream != null)
                {
                    App.WriteSavaDataToStream(student.NetworkStream, null, 62);
                }
            }
            StopSharingScreenButton.Visibility = Visibility.Collapsed;
            ShareScreenButton.Visibility = Visibility.Visible;
        }
    }
}
