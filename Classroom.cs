/*
 * Copyright 2024 Đorđe Mančić
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace SavaMonitor
{
    public class Classroom
    {
        public Teacher Teacher { get; set; }
        public List<Student> StudentList { get; set; }
        public string Name { get; set; }
        public string ID { get; set; }
        public int Port { get; set; }
        public string StaticIPAddress { get; set; }
        public string Passkey { get; set; }
        public bool AcceptsStudents = false;
        public Student StudentJoining;

        public Classroom()
        {
            Passkey = App.GenerateRandomString(32);
            StudentList = new List<Student>();
        }

        #region Teacher methods
        public async Task ApproveStudentJoining()
        {
            JsonObject returnJson = new JsonObject
            {
                ["IsApproved"] = true,
                ["Passkey"] = this.Passkey
            };
            string jsonString = returnJson.ToJsonString();
            Byte[] jsonBytes = Encoding.Unicode.GetBytes(jsonString);

            await App.WriteSavaDataToStream(this.StudentJoining.NetworkStream, jsonBytes, 20);

            this.StudentJoining.LastResponse = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.StudentJoining.IsConnected = true;
            this.StudentList.Add(this.StudentJoining);
            this.StudentJoining = null;
            App application = (App)Application.Current;
            application.Dispatcher.Invoke(() =>
            {
                MainWindow mainWindow = (MainWindow)application.MainWindow;
                mainWindow.UpdateViewPage();
                application.SaveData(false);
            });
        }

        public async Task DeclineStudentJoining(NetworkStream providedStream = null)
        {
            if (providedStream != null)
            {
                await App.WriteSavaDataToStream(providedStream, null, 21); // 21 - Decline joining
                providedStream.Close();
            }
            else
            {
                await App.WriteSavaDataToStream(this.StudentJoining.NetworkStream, null, 21); // 21 - Decline joining
                this.StudentJoining.NetworkStream.Close();
                this.StudentJoining = null;
            }

        }

        async Task StartReceivingFromTcpClient(TcpClient tcpClient)
        {
            NetworkStream stream = tcpClient.GetStream();
            Byte[] buffer = new byte[4096];
            Student student = null;

            bool IsAuthenticated()
            {
                return student != null && student.IsConnected == true;
            }

            while (tcpClient.Connected)
            {
                try
                {
                    //First bytes
                    int bytesReceived = stream.Read(buffer, 0, 5);
                    if (bytesReceived == 0)
                    {
                        Debug.WriteLine("Ending TCP connection, 0 bytes received...");
                        break;
                    }
                    else if (bytesReceived < 5) 
                    {
                        Debug.WriteLine("Malformed start data for " + tcpClient.Client.RemoteEndPoint + " - Only " + bytesReceived + " bytes received");
                        continue;
                    }
                    int dataLength = App.GetIntFromBytes(new Byte[] { buffer[0], buffer[1], buffer[2], buffer[3] }); // Bytes that go after the "info" message
                    int bytesRemaining = dataLength;
                    int headerType = buffer[4];
                    Debug.WriteLine("Length: " + dataLength);
                    Debug.WriteLine("Header: " + headerType);

                    //Read rest of data
                    if (headerType == 6) // Ping response
                    {
                        if (IsAuthenticated())
                        {
                            Debug.WriteLine("Got back ping response from " + student.ID);
                            student.LastResponse = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            string jsonString = "";
                            while (bytesRemaining > 0)
                            {
                                bytesReceived = stream.Read(buffer, 0, Math.Min(bytesRemaining, buffer.Length));
                                if (bytesReceived == 0) break;
                                bytesRemaining -= bytesReceived;
                                jsonString += Encoding.Unicode.GetString(buffer, 0, bytesReceived);
                            }
                            Debug.WriteLine(jsonString);
                        }
                        else
                        {
                            tcpClient.Close();
                        }
                    }
                    else if (headerType == 7) // Ping response - Screenshot
                    {
                        if (IsAuthenticated())
                        {
                            Debug.WriteLine("Got screenshot data from " + student.ID + ". Reading...");
                            MemoryStream memoryStream = new MemoryStream();
                            while (bytesRemaining > 0)
                            {
                                bytesReceived = stream.Read(buffer, 0, Math.Min(bytesRemaining, buffer.Length));
                                if (bytesReceived == 0) break;
                                bytesRemaining -= bytesReceived;
                                memoryStream.Write(buffer, 0, bytesReceived);
                            }
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            BitmapImage newBitmapImage = new BitmapImage();
                            newBitmapImage.BeginInit();
                            newBitmapImage.StreamSource = memoryStream;
                            newBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            newBitmapImage.EndInit();
                            newBitmapImage.Freeze();
                            student.LatestScreenShot = newBitmapImage;
                        }
                        else
                        {
                            tcpClient.Close();
                        }
                    }
                    else if (headerType == 10) // Get classroom information, contains 0 data
                    {
                        bool acceptsStudents = false;
                        if (this.AcceptsStudents == true && this.StudentJoining == null)
                        {
                            acceptsStudents = true;
                        }
                        JsonObject classroomJson = new JsonObject
                        {
                            ["ID"] = this.ID,
                            ["Name"] = this.Name,
                            ["StaticIPAddress"] = this.StaticIPAddress,
                            ["AcceptsStudents"] = acceptsStudents
                        };

                        string jsonString = classroomJson.ToJsonString();
                        Byte[] writeBuffer = Encoding.Unicode.GetBytes(jsonString);
                        stream.Write(writeBuffer, 0, writeBuffer.Length);
                    }
                    else if (headerType == 20) // Request to join the classroom
                    {
                        if (this.AcceptsStudents == true && this.StudentJoining == null)
                        {
                            string jsonString = "";
                            while (bytesRemaining > 0)
                            {
                                bytesReceived = stream.Read(buffer, 0, Math.Min(bytesRemaining, buffer.Length));
                                if (bytesReceived == 0) break;
                                bytesRemaining -= bytesReceived;
                                jsonString += Encoding.Unicode.GetString(buffer, 0, bytesReceived);
                            }
                            Debug.WriteLine("Received JSON string for joining classroom " + jsonString);
                            JsonNode studentJsonNode = JsonNode.Parse(jsonString);
                            this.StudentJoining = new Student();
                            this.StudentJoining.TCPClient = tcpClient;
                            this.StudentJoining.NetworkStream = stream;
                            this.StudentJoining.ID = studentJsonNode["ID"].GetValue<string>();
                            this.StudentJoining.Name = studentJsonNode["Name"].GetValue<string>();
                            student = this.StudentJoining;

                            string pairingNumber = studentJsonNode["PairingNumber"].GetValue<string>();
                            
                            App application = (App)Application.Current;
                            application.Dispatcher.Invoke(() =>
                            {
                                TeacherPrompts.AddStudentPrompt addStudentPrompt = application.Windows.OfType<TeacherPrompts.AddStudentPrompt>().FirstOrDefault();
                                addStudentPrompt.StartAddProcess(pairingNumber);
                            });
                        
                        }
                        else
                        {
                            await this.DeclineStudentJoining(stream);
                        }
                    }
                    else if (headerType == 30) // Request to connect to the classroom (already joined)
                    {
                        string jsonString = "";
                        while (bytesRemaining > 0)
                        {
                            bytesReceived = stream.Read(buffer, 0, Math.Min(bytesRemaining, buffer.Length));
                            if (bytesReceived == 0) break;
                            bytesRemaining -= bytesReceived;
                            jsonString += Encoding.Unicode.GetString(buffer, 0, bytesReceived);
                        }
                        Debug.WriteLine("Received JSON string for connecting to classroom " + jsonString);
                        JsonNode connectionJsonNode = JsonNode.Parse(jsonString);
                        if (connectionJsonNode["StudentID"] != null && connectionJsonNode["Passkey"] != null)
                        {
                            string studentID = connectionJsonNode["StudentID"].GetValue<string>();
                            string receivedPasskey = connectionJsonNode["Passkey"].GetValue<string>();
                            if (receivedPasskey == this.Passkey)
                            {
                                student = this.StudentList.Find(stud => stud.ID == studentID);
                                if (student != null)
                                {
                                    student.TCPClient = tcpClient;
                                    student.NetworkStream = stream;
                                    student.IsConnected = true;
                                    student.LastResponse = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                    await App.WriteSavaDataToStream(stream, null, 31);
                                }
                            }
                            else
                            {
                                tcpClient.Close();
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Unknown header");
                    }
                }
                catch (Exception exception)
                {
                    if (tcpClient.Connected == true)
                    {
                        Debug.WriteLine("CLASSROOM HOST EXCEPTION!!!");
                        Debug.WriteLine(exception.ToString());
                        Debug.WriteLine("END OF EXCEPTION");
                    }
                }

            }
        }

        public async Task StartHosting()
        { 
            //Listen for TCP requests
            _ = Task.Run(() =>
            {
                TcpListener listener = new TcpListener(IPAddress.Any, this.Port);
                listener.Start();
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Debug.WriteLine("Connected client " + client.Client.RemoteEndPoint);
                    Task.Run(() => StartReceivingFromTcpClient(client));
                }
            });

            //Listen for UDP requests
            _ = Task.Run(() =>
            {
                Debug.WriteLine("Classroom UDP Started.");
                UdpClient listener = new UdpClient(this.Port);
                listener.EnableBroadcast = true;
                this.Teacher.UDPClient = listener;
                while (true)
                {
                    Debug.WriteLine("Waiting for receive...");
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                    Byte[] bytesReceived = listener.Receive(ref endPoint);
                    Debug.WriteLine("UDP - Received from " + endPoint.Address.ToString());
                    if (bytesReceived.Length < 5)
                    {
                        Debug.WriteLine("Bytes received too short - " + bytesReceived.Length + " bytes.");
                        continue;
                    }
                    if (bytesReceived[4] == 20) // Ping
                    {
                        listener.Send(new Byte[] { 0, 0, 0, 0, 25 }, bytesReceived.Length, endPoint);
                        Debug.WriteLine("Discovery ping received, pinging back...");
                    }
                }
            });

            //Ping all students every 5 seconds to check if they're actually online, also gather some information on them
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    foreach (Student student in this.StudentList)
                    {
                        try
                        {
                            bool isActuallyConnected = false;
                            if (student.IsConnected == true)
                            {
                                if (student.TCPClient != null && student.NetworkStream != null && student.TCPClient.Connected == true)
                                {
                                    if (student.LastResponse >= DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 10)
                                    {
                                        isActuallyConnected = true;
                                    }
                                    Debug.WriteLine("Pinging " + student.ID);
                                    await App.WriteSavaDataToStream(student.NetworkStream, null, 5);
                                }
                            }
                            if (student.IsConnected == true && isActuallyConnected == false)
                            {
                                BitmapImage bitmapImage = new BitmapImage();
                                bitmapImage.Freeze();
                                student.LatestScreenShot = bitmapImage;
                            }
                            student.IsConnected = isActuallyConnected;
                        }
                        catch (Exception exception)
                        {
                            Debug.WriteLine("CLASSROOM HOST PINGING EXCEPTION!!!");
                            Debug.WriteLine(exception.ToString());
                            Debug.WriteLine("END OF EXCEPTION");
                        }
                        
                    }
                    await Task.Delay(3000);
                }
            });
        }
        #endregion

        #region Student methods
        async Task SearchForTeacher()
        {
            App application = (App)Application.Current;
            Student student = (Student)application.LocalComputer;

            if (student.UDPClient != null)
            {
                student.UDPClient.Close();
            }

            List<string> savaAddresses;
            if (this.StaticIPAddress != null)
            {
                savaAddresses = new List<string> { this.StaticIPAddress };
            }
            else
            {
                savaAddresses = await application.GetActiveSavaAddresses(3500);
            }
            foreach (string address in savaAddresses)
            {
                try
                {
                    Debug.WriteLine("Trying to connect to " + address + " as student...");
                    TcpClient tcpClient = new TcpClient();
                    Task connectTask = tcpClient.ConnectAsync(address, this.Port);
                    if (await Task.WhenAny(connectTask, Task.Delay(1000)) != connectTask)
                    {
                        Debug.WriteLine("Timed out.");
                        continue;
                    }
                    if (tcpClient.Connected == false) continue;
                    Debug.WriteLine("Connected!");
                    NetworkStream stream = tcpClient.GetStream();
                    Debug.WriteLine("Trying to fetch info about classroom...");

                    Byte[] readBuffer = new byte[4096];
                    Byte[] questionBytes = { 0, 0, 0, 0, 10 };
                    await stream.WriteAsync(questionBytes);

                    int bytesReceived = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);

                    string classroomJsonString = Encoding.Unicode.GetString(readBuffer, 0, bytesReceived);
                    JsonNode classroomJsonNode = JsonNode.Parse(classroomJsonString);

                    if (classroomJsonNode["ID"].GetValue<string>() == this.ID)
                    {
                        Debug.WriteLine("Found classroom! Trying to connect...");
                        JsonObject connectionJsonObject = new JsonObject
                        {
                            ["StudentID"] = application.LocalComputer.ID,
                            ["Passkey"] = this.Passkey
                        };
                        Byte[] connectionBytes = Encoding.Unicode.GetBytes(connectionJsonObject.ToJsonString());
                        await App.WriteSavaDataToStream(stream, connectionBytes, 30);

                        bytesReceived = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);
                        if (readBuffer[4] == 31)
                        {
                            if (this.Teacher == null)
                            {
                                this.Teacher = new Teacher();
                            }
                            this.Teacher.NetworkStream = stream;
                            student.TCPClient = tcpClient;
                            return;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Not right classroom ID, searching for next...");
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
        }

        public async Task StartListeningToTeacher()
        {
            App application = (App)Application.Current;
            Student student = (Student)application.LocalComputer;
            application.TaskbarIcon = new NotifyIcon();
            application.TaskbarIcon.Icon = SavaMonitor.Properties.Resources.sava;
            application.TaskbarIcon.Text = "Sava Monitor - Pokreće se...";
            application.TaskbarIcon.Visible = true;
            
            //Networking loop
            while (true)
            {
                application.TaskbarIcon.Text = "Sava Monitor - Waiting for classroom connection...";
                application.TaskbarIcon.Visible = true;
                Debug.WriteLine("Waiting for classroom connection...");
                while (student.TCPClient == null)
                {
                    try
                    {
                        await SearchForTeacher();
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine("EXCEPTION WHILE SEARCHING FOR TEACHER!!!!");
                        Debug.WriteLine(exception.ToString());
                        Debug.WriteLine("END OF EXCEPTION");
                    }
                    
                    if (student.TCPClient == null)
                    {
                        Debug.WriteLine("Haven't found teacher computer on network, retrying in 10 seconds...");
                        await Task.Delay(10000);
                    }
                }
                Debug.WriteLine("Connected to classroom!");

                application.TaskbarIcon.Text = "Sava Monitor - Connected";
                application.TaskbarIcon.Visible = true;

                if (student.TCPClient == null) continue;
                TcpClient tcpClient = student.TCPClient;
                NetworkStream stream = tcpClient.GetStream();

                //TCP loop
                Byte[] buffer = new Byte[4096];
                MemoryStream screenshotMemoryStream = new MemoryStream();
                while (tcpClient != null && tcpClient.Connected)
                {
                    try
                    {
                        //First bytes
                        int bytesReceived = stream.Read(buffer, 0, 5);
                        if (bytesReceived == 0)
                        {
                            Debug.WriteLine("Ending TCP connection, 0 bytes received...");
                            break;
                        }
                        else if (bytesReceived < 5)
                        {
                            Debug.WriteLine("Malformed start data for " + tcpClient.Client.RemoteEndPoint + " - Only " + bytesReceived + " bytes received");
                            continue;
                        }
                        int dataLength = App.GetIntFromBytes(new Byte[] { buffer[0], buffer[1], buffer[2], buffer[3] }); // Bytes that go after the "info" message
                        int bytesRemaining = dataLength;
                        int headerType = buffer[4];
                        Debug.WriteLine("Length: " + dataLength);
                        Debug.WriteLine("Header: " + headerType);

                        //Read rest of data
                        if (headerType == 5) // Ping from server
                        {
                            string dataToSend = "Pong!";
                            Byte[] dataBytes = Encoding.Unicode.GetBytes(dataToSend);
                            await App.WriteSavaDataToStream(stream, dataBytes, 6);
                            Byte[] screenshotBytes = await App.GetScreenShotBytes();
                            await App.WriteSavaDataToStream(stream, screenshotBytes, 7);
                        }
                        else if (headerType == 40) // Message from teacher
                        {
                            string messageString = "";
                            while (bytesRemaining > 0)
                            {
                                bytesReceived = stream.Read(buffer, 0, Math.Min(bytesRemaining, buffer.Length));
                                if (bytesReceived == 0) break;
                                bytesRemaining -= bytesReceived;
                                messageString += Encoding.Unicode.GetString(buffer, 0, bytesReceived);
                            }

                            _ = Task.Run(() =>
                            {
                                MessageBox.Show(messageString, "Message from the teacher", MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }
                        else if (headerType == 50) // Shutdown computer
                        {
                            _ = Task.Run(() =>
                            {
                                try
                                {
                                    ProcessStartInfo processStartInfo = new ProcessStartInfo("shutdown", "/s /t 0");
                                    processStartInfo.CreateNoWindow = true;
                                    processStartInfo.UseShellExecute = false;
                                    Process.Start(processStartInfo);
                                }
                                catch (Exception exception)
                                {
                                    Debug.WriteLine("Can't shutdown - exception thrown");
                                    Debug.WriteLine(exception.ToString());
                                    Debug.WriteLine("End of exception");
                                }
                            });
                        }
                        else if (headerType == 60) // Start screen sharing
                        {
                            string jsonString = "";
                            while (bytesRemaining > 0)
                            {
                                bytesReceived = stream.Read(buffer, 0, Math.Min(bytesRemaining, buffer.Length));
                                if (bytesReceived == 0) break;
                                bytesRemaining -= bytesReceived;
                                jsonString += Encoding.Unicode.GetString(buffer, 0, bytesReceived);
                            }
                            Debug.WriteLine("Received JSON string for starting screen share " + jsonString);
                            JsonNode screenShareJsonNode = JsonNode.Parse(jsonString);

                            application.Dispatcher.Invoke(() =>
                            {
                                ScreenShareWindow screenShareWindow = new ScreenShareWindow
                                {
                                    DataContext = this.Teacher,
                                    ShareSizeType = screenShareJsonNode["ShareSizeType"].GetValue<string>()
                                };
                                screenShareWindow.Show();
                            });
                        }
                        else if (headerType == 62) // Stop screen sharing
                        {
                            application.Dispatcher.Invoke(() =>
                            {
                                if (application.Windows.OfType<ScreenShareWindow>().Count() > 0)
                                {
                                    foreach (ScreenShareWindow screenShareWindow in application.Windows.OfType<ScreenShareWindow>())
                                    {
                                        screenShareWindow.Close();
                                    }
                                }
                            });
                        }
                        else if (headerType == 65) // Receive screen share data
                        {
                            Debug.WriteLine("Got screenshot data, reading...");
                            screenshotMemoryStream.SetLength(0);
                            while (bytesRemaining > 0)
                            {
                                bytesReceived = stream.Read(buffer, 0, Math.Min(bytesRemaining, buffer.Length));
                                if (bytesReceived == 0) break;
                                bytesRemaining -= bytesReceived;
                                screenshotMemoryStream.Write(buffer, 0, bytesReceived);
                            }
                            screenshotMemoryStream.Seek(0, SeekOrigin.Begin);
                            BitmapImage newBitmapImage = new BitmapImage();
                            newBitmapImage.BeginInit();
                            newBitmapImage.StreamSource = screenshotMemoryStream;
                            newBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            newBitmapImage.EndInit();
                            newBitmapImage.Freeze();
                            application.Classroom.Teacher.LatestScreenShot = (BitmapImage)newBitmapImage;
                        }
                        else
                        {
                            Debug.WriteLine("Unknown header");
                        }
                    }
                    catch (Exception exception)
                    {
                        if (tcpClient != null && tcpClient.Connected == true)
                        {
                            Debug.WriteLine("CLASSROOM LISTEN EXCEPTION!!!");
                            Debug.WriteLine(exception.ToString());
                            Debug.WriteLine("END OF EXCEPTION");
                        }
                        
                    }
                }

                student.TCPClient = null;
                application.Dispatcher.Invoke(() =>
                {
                    if (application.Windows.OfType<ScreenShareWindow>().Count() > 0)
                    {
                        foreach (ScreenShareWindow screenShareWindow in application.Windows.OfType<ScreenShareWindow>())
                        {
                            screenShareWindow.Close();
                        }
                    }
                });
            }
        }
        #endregion

        #region Universal methods

        public async Task ShareScreen()
        {
            
            Debug.WriteLine("Sharing screen...");
            App application = (App)Application.Current;
            Computer localComputer = application.LocalComputer;

            int screenMillisecondInterval = 1000 / 5;

            while (localComputer.IsSharingScreen == true)
            {
                try
                {
                    Byte[] shareBytes = await App.GetScreenShotBytes();
                    Debug.WriteLine("Screenshot size: " + shareBytes.Length);
                    Debug.WriteLine("Screenshot made, sending...");
                    foreach (Student student in this.StudentList)
                    {
                        if (student.IsConnected == true && student.TCPClient != null)
                        {
                            App.WriteSavaDataToStream(student.NetworkStream, shareBytes, 65);
                        }
                    }
                    Debug.WriteLine("Sent!");
                    await Task.Delay(screenMillisecondInterval);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine("SCREENSHARE EXCEPTION");
                    Debug.WriteLine(exception.ToString());
                    Debug.WriteLine("END OF EXCEPTION");
                }
            }
            
        }

        #endregion



    }
}
