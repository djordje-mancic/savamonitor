/*
 * Copyright 2024 Đorđe Mančić
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SavaMonitor.FirstTimeSetupPages
{
    public partial class StudentClassroomConnecting : Page
    {
        App application;
        public StudentClassroomConnecting()
        {
            InitializeComponent();
            application = (App)Application.Current;
            TitleText.Text = "Joining classroom ID " + application.Classroom.ID;
            DescriptionText.Text = "...";
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }

        void throwJoinError(string errorText)
        {
            VisualProgressBar.Visibility = Visibility.Collapsed;
            DescriptionText.Text = errorText;
            DescriptionText.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            BackButton.Visibility = Visibility.Visible;
        }

        async Task SaveDataToFiles()
        {
            string classroomJsonString = JsonSerializer.Serialize(application.Classroom);
            Byte[] classroomJsonBytes = Encoding.Unicode.GetBytes(classroomJsonString);
            application.ClassroomConfigFileStream = File.Create("classroom.json");

            await application.ClassroomConfigFileStream.WriteAsync(classroomJsonBytes);
            await application.ClassroomConfigFileStream.FlushAsync();

            string studentJsonString = JsonSerializer.Serialize<Student>((Student)application.LocalComputer);
            Byte[] studentJsonBytes = Encoding.Unicode.GetBytes(studentJsonString);
            application.LocalComputerConfigFileStream = File.Create("student.json");
            await application.LocalComputerConfigFileStream.WriteAsync(studentJsonBytes);
            await application.LocalComputerConfigFileStream.FlushAsync();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> localAddresses;

            if (application.Classroom.StaticIPAddress != null)
            {
                DescriptionText.Text = "Retrieving static IP address...";

                localAddresses = new List<string>{ application.Classroom.StaticIPAddress };
            }
            else
            {
                DescriptionText.Text = "Searching for computers on the local network...";

                localAddresses = await application.GetActiveSavaAddresses(2000);
            }
            
            // Check for Sava on all available addresses
            bool foundClassroom = false;
            foreach (string address in localAddresses)
            {
                try
                {
                    DescriptionText.Text = "Trying to connect to " + address + "...";
                    Debug.WriteLine("Connecting to " + address + "...");
                    TcpClient tcpClient = new TcpClient();
                    Task connectTask = tcpClient.ConnectAsync(address, 3212);
                    if (await Task.WhenAny(connectTask, Task.Delay(1000)) != connectTask)
                    {
                        Debug.WriteLine("Timed out.");
                        continue;
                    }
                    if (tcpClient.Connected == false) continue;
                    Debug.WriteLine("Connected!");
                    NetworkStream stream = tcpClient.GetStream();

                    DescriptionText.Text = "Trying to get information about the classroom...";

                    Byte[] readBuffer = new byte[4096];
                    await App.WriteSavaDataToStream(stream, null, 10);

                    int bytesReceived = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);
                    
                    string classroomJsonString = Encoding.Unicode.GetString(readBuffer, 0, bytesReceived);
                    JsonNode classroomJsonNode = JsonNode.Parse(classroomJsonString);

                    if (classroomJsonNode["ID"].GetValue<string>() == application.Classroom.ID)
                    {
                        foundClassroom = true;
                        if (classroomJsonNode["AcceptsStudents"].GetValue<bool>() == true)
                        {
                            DescriptionText.Text = "Classroom found and accepts join requests, asking to join...";
                            Random random = new Random();
                            string pairingNumber = random.Next(1000, 10000).ToString();

                            Debug.WriteLine("Pairing number is " + pairingNumber);

                            FirstTimeSetupWindow firstTimeWindow = application.Windows.OfType<FirstTimeSetupWindow>().FirstOrDefault();
                            PairingNumberWindow pairingNumberWindow = new PairingNumberWindow(pairingNumber);
                            pairingNumberWindow.Owner = firstTimeWindow;
                            pairingNumberWindow.Show();

                            JsonObject joinValues = new JsonObject
                            {
                                ["ID"] = application.LocalComputer.ID,
                                ["Name"] = application.LocalComputer.Name,
                                ["PairingNumber"] = pairingNumber
                            };
                            string joinString = joinValues.ToJsonString();
                            Byte[] joinStringBytes = Encoding.Unicode.GetBytes(joinString);

                            Debug.WriteLine("Sending join request to classroom...");

                            await App.WriteSavaDataToStream(stream, joinStringBytes, 20);

                            Debug.WriteLine("Request sent!");
                            Debug.WriteLine("Waiting from response from classroom...");

                            bytesReceived = await stream.ReadAsync(readBuffer, 0, 5);

                            Debug.WriteLine("Response received!");

                            pairingNumberWindow.Close();

                            if (bytesReceived == 0)
                            {
                                Debug.WriteLine("Ending TCP connection to server, 0 bytes received...");
                                break;
                            }
                            else if (bytesReceived < 5)
                            {
                                Debug.WriteLine("Malformed start data for " + tcpClient.Client.RemoteEndPoint + " - Only " + bytesReceived + " bytes received");
                                continue;
                            }
                            int dataLength = App.GetIntFromBytes(readBuffer.Take(4).ToArray()); // Bytes that go after the "info" message
                            int bytesRemaining = dataLength;
                            int headerType = readBuffer[4];
                            Debug.WriteLine("Length: " + dataLength);
                            Debug.WriteLine("Header: " + headerType);

                            if (headerType == 20)
                            {
                                string joinResultString = "";

                                while (bytesRemaining > 0)
                                {
                                    bytesReceived = await stream.ReadAsync(readBuffer, 0, Math.Min(bytesRemaining, readBuffer.Length));
                                    if (bytesReceived == 0) break;
                                    bytesRemaining -= bytesReceived;
                                    joinResultString += Encoding.Unicode.GetString(readBuffer, 0, bytesReceived);
                                }
                                
                                JsonNode resultNode = JsonNode.Parse(joinResultString);
                                if (resultNode["IsApproved"].GetValue<bool>() == true)
                                {
                                    application.Classroom.Name = classroomJsonNode["Name"].GetValue<string>();
                                    if (classroomJsonNode["StaticIPAddress"] != null)
                                    {
                                        application.Classroom.StaticIPAddress = classroomJsonNode["StaticIPAddress"].GetValue<string>();
                                    }
                                    application.Classroom.Passkey = resultNode["Passkey"].GetValue<string>();
                                    VisualProgressBar.Value = 0;
                                    VisualProgressBar.IsIndeterminate = false;
                                    DescriptionText.Text = "Saving information to file...";
                                    await SaveDataToFiles();

                                    VisualProgressBar.Value = 100;
                                    DescriptionText.Text = "Successfully joined. Welcome to classroom " + application.Classroom.Name + "!";
                                    FinishButton.Visibility = Visibility.Visible;
                                    application.Classroom.Teacher = new Teacher();
                                    application.Classroom.Teacher.NetworkStream = stream;
                                    (application.LocalComputer as Student).TCPClient = tcpClient;
                                    application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                                    Task.Run(application.Classroom.StartListeningToTeacher);
                                }
                                else
                                {
                                    throwJoinError("Joining failed. Have you entered the correct number from the screen?");
                                }
                            }
                            else
                            {
                                throwJoinError("Joining failed. Have you entered the correct number from the screen?");
                            }

                            break;
                        }
                        else
                        {
                            throwJoinError("Classroom doesn't accept join requests. If you haven't already, press the \"Add new computers\" button on the teacher computer and make sure that nobody else is trying to join that classroom at this time.");
                            stream.Close();
                            tcpClient.Close();
                            break;
                        }
                    }
                    else
                    {
                        stream.Close();
                        tcpClient.Close();
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine("EXCEPTION CAUGHT!!!!!!!");
                    Debug.WriteLine(exception.ToString());
                    Debug.WriteLine("END OF EXCEPTION!!!!!!!!!!!!");

                }
            }
            if (foundClassroom == false)
            {
                throwJoinError("Classroom not found. Are you sure you have entered the right classroom ID? Are the Windows Firewall rules set up correctly?");
            }
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            FirstTimeSetupWindow firstTimeWindow = application.Windows.OfType<FirstTimeSetupWindow>().FirstOrDefault();
            firstTimeWindow.Close();
        }
    }
}
