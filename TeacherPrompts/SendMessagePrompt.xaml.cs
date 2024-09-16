/*
 * Copyright 2024 Đorđe Mančić
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace SavaMonitor.TeacherPrompts
{
    public partial class SendMessagePrompt : Window
    {
        private List<Student> studentsToSend;
        bool isSending = false;

        public SendMessagePrompt(List<Student> studentsToSend)
        {
            InitializeComponent();
            this.studentsToSend = studentsToSend;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSending == true) return;
            isSending = true;
            foreach (Student student in studentsToSend)
            {
                if (student.IsConnected == true && student.NetworkStream != null)
                {
                    Byte[] messageBytes = Encoding.Unicode.GetBytes(MessageTextBox.Text);
                    await App.WriteSavaDataToStream(student.NetworkStream, messageBytes, 40);
                }
            }
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSending == true) return;
            this.Close();
        }
    }
}
