/*
 * Copyright 2024 Đorđe Mančić
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using System.Drawing;
using System.Drawing.Imaging;

namespace SavaMonitor
{
    public partial class App : Application
    {
        public Classroom Classroom;
        public Computer LocalComputer;
        public FileStream ClassroomConfigFileStream;
        public FileStream LocalComputerConfigFileStream;
        public NotifyIcon TaskbarIcon;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //Read data
            if (File.Exists("teacher.json"))
            {
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                //Read files
                LocalComputerConfigFileStream = File.Open("teacher.json", FileMode.Open, FileAccess.ReadWrite);
                ClassroomConfigFileStream = File.Open("classroom.json", FileMode.Open, FileAccess.ReadWrite);

                Byte[] classroomJsonBytes = new Byte[ClassroomConfigFileStream.Length];
                Byte[] teacherJsonBytes = new Byte[LocalComputerConfigFileStream.Length];
                ClassroomConfigFileStream.Read(classroomJsonBytes);
                LocalComputerConfigFileStream.Read(teacherJsonBytes);
                string classroomJsonString = Encoding.Unicode.GetString(classroomJsonBytes);
                string teacherJsonString = Encoding.Unicode.GetString(teacherJsonBytes);
                Classroom = JsonSerializer.Deserialize<Classroom>(classroomJsonString);
                LocalComputer = JsonSerializer.Deserialize<Teacher>(teacherJsonString);
                Classroom.Teacher = (Teacher)LocalComputer;

                //Start hosting
                Task.Run(Classroom.StartHosting);

                MainWindow main = new MainWindow();
                main.Show();
            }
            else if (File.Exists("student.json"))
            {
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                //Read files
                LocalComputerConfigFileStream = File.Open("student.json", FileMode.Open, FileAccess.ReadWrite);
                ClassroomConfigFileStream = File.Open("classroom.json", FileMode.Open, FileAccess.ReadWrite);

                Byte[] classroomJsonBytes = new Byte[ClassroomConfigFileStream.Length];
                Byte[] teacherJsonBytes = new Byte[LocalComputerConfigFileStream.Length];
                ClassroomConfigFileStream.Read(classroomJsonBytes);
                LocalComputerConfigFileStream.Read(teacherJsonBytes);
                string classroomJsonString = Encoding.Unicode.GetString(classroomJsonBytes);
                string studentJsonString = Encoding.Unicode.GetString(teacherJsonBytes);
                Classroom = JsonSerializer.Deserialize<Classroom>(classroomJsonString);
                LocalComputer = JsonSerializer.Deserialize<Student>(studentJsonString);

                //Start listening
                Task.Run(Classroom.StartListeningToTeacher);
            }
            else
            {
                FirstTimeSetupWindow firstTimeSetupWindow = new FirstTimeSetupWindow();
                firstTimeSetupWindow.Show();
            }
        }

        public static string GenerateRandomString(int length)
        {
            var charactersAllowed = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            string finalString = "";

            for (int i = 0; i < length; ++i)
            {
                int charNum = RandomNumberGenerator.GetInt32(0, charactersAllowed.Length);
                finalString += charactersAllowed[charNum];
            }

            return finalString;
        }

        public static int GetIntFromBytes(Byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        public static Byte[] GetBytesFromInt(int number)
        {
            Byte[] bytes = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        public static async Task WriteSavaDataToStream(NetworkStream stream, Byte[] data, Byte header)
        {
            Byte[] writeBuffer;
            if (data != null)
            {
                IEnumerable<Byte> startData = App.GetBytesFromInt(data.Length).Concat(new Byte[] { header });
                writeBuffer = startData.Concat(data).ToArray();
            }
            else
            {
                writeBuffer = new Byte[5] { 0, 0, 0, 0, header };
            }
            
            await stream.WriteAsync(writeBuffer, 0, writeBuffer.Length);
        }

        public static async Task<Byte[]> GetScreenShotBytes(ImageFormat imageFormat = null)
        {
            if (imageFormat == null)
            {
                imageFormat = ImageFormat.Jpeg;
            }
            Rectangle primaryScreenBounds = Screen.PrimaryScreen.Bounds;
            Bitmap screenBitmap = new Bitmap(primaryScreenBounds.Width, primaryScreenBounds.Height, PixelFormat.Format32bppRgb);
            Graphics graphics = Graphics.FromImage(screenBitmap);
            graphics.CopyFromScreen(primaryScreenBounds.Left, primaryScreenBounds.Top, 0, 0, primaryScreenBounds.Size);
            MemoryStream memoryStream = new MemoryStream();
            screenBitmap.Save(memoryStream, imageFormat);
            return memoryStream.ToArray();
        }

        public class NetworkInterfaceInfoResult
        {
            public IPAddress LocalAddress;
            public IPAddress IPv4Mask;
            public IPAddress BroadcastAddress;
        }

        public List<NetworkInterfaceInfoResult> GetNetworkInterfaceInfo()
        {
            List<NetworkInterfaceInfoResult> resultList = new List<NetworkInterfaceInfoResult>();

            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || networkInterface.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet)
                {
                    IPInterfaceProperties interfaceProperties = networkInterface.GetIPProperties();
                    foreach (UnicastIPAddressInformation unicastInfo in interfaceProperties.UnicastAddresses)
                    {
                        if (unicastInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            NetworkInterfaceInfoResult interfaceResult = new NetworkInterfaceInfoResult
                            {
                                LocalAddress = unicastInfo.Address,
                                IPv4Mask = unicastInfo.IPv4Mask
                            };
                            UInt32 localAddressInt = BitConverter.ToUInt32(interfaceResult.LocalAddress.GetAddressBytes(), 0);
                            UInt32 ipV4MaskInt = BitConverter.ToUInt32(interfaceResult.IPv4Mask.GetAddressBytes(), 0);
                            UInt32 broadcastAddressInt = localAddressInt | ~ipV4MaskInt;
                            interfaceResult.BroadcastAddress = new IPAddress(BitConverter.GetBytes(broadcastAddressInt));
                            resultList.Add(interfaceResult);
                        }
                    }
                }
            }

            return resultList;
        }

        public async Task<List<string>> GetActiveSavaAddresses(int timeout)
        {
            List<string> computerAddresses = new List<string>();
            List<string> forbiddenAddresses = new List<string>();
            bool addressListLocked = false;

            UdpClient temporaryUdpClient = new UdpClient(this.Classroom.Port);
            temporaryUdpClient.EnableBroadcast = true;

            List<NetworkInterfaceInfoResult> interfaceResults = GetNetworkInterfaceInfo();

            foreach (NetworkInterfaceInfoResult infoResult in interfaceResults)
            {
                IPEndPoint endPoint = new IPEndPoint(infoResult.BroadcastAddress, this.Classroom.Port);
                forbiddenAddresses.Add(infoResult.LocalAddress.ToString());
                _ = temporaryUdpClient.SendAsync(new Byte[] { 0, 0, 0, 0, 20 }, 5, endPoint);
            }

            _ = Task.Run(() =>
            {
                while (addressListLocked == false)
                {
                    try
                    {
                        Debug.WriteLine("Trying to receive...");
                        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, this.Classroom.Port);
                        temporaryUdpClient.Receive(ref endPoint);
                        if (addressListLocked == false)
                        {
                            computerAddresses.Add(endPoint.Address.ToString());
                            //MessageBox.Show("Received connection from " + endPoint.Address.ToString());
                        }
                        Debug.WriteLine("Received connection from " + endPoint.Address.ToString());
                    }
                    catch (Exception exception)
                    {
                        if (exception.GetType().Name != "SocketException")
                        {
                            Debug.WriteLine("UDP Search Exception");
                            Debug.WriteLine(exception.ToString());
                            Debug.WriteLine("End of exception");
                        }
                    }
                }
            });

            await Task.Delay(timeout);
            addressListLocked = true;
            temporaryUdpClient.Close();
            computerAddresses.RemoveAll(str => forbiddenAddresses.Exists(frstr => frstr.Equals(str)));

            return computerAddresses;
        }

        public void SaveData(bool IsExit)
        {
            if (LocalComputerConfigFileStream != null)
            {
                LocalComputerConfigFileStream.SetLength(0);
                LocalComputerConfigFileStream.Flush();

                string computerJsonString;
                LocalComputer.LatestScreenShot.Freeze();
                if (LocalComputer is Student)
                {
                    computerJsonString = JsonSerializer.Serialize<Student>((Student)LocalComputer);
                }
                else
                {
                    computerJsonString = JsonSerializer.Serialize<Teacher>((Teacher)LocalComputer);
                }

                Byte[] computerJsonBytes = Encoding.Unicode.GetBytes(computerJsonString);

                LocalComputerConfigFileStream.Write(computerJsonBytes);
                if (IsExit == true)
                {
                    LocalComputerConfigFileStream.Close();
                }
                else
                {
                    LocalComputerConfigFileStream.Flush();
                }
               
            }

            if (ClassroomConfigFileStream != null)
            {
                ClassroomConfigFileStream.SetLength(0);
                ClassroomConfigFileStream.Flush();

                string classroomJsonString = JsonSerializer.Serialize(Classroom);
                Byte[] classroomJsonBytes = Encoding.Unicode.GetBytes(classroomJsonString);

                ClassroomConfigFileStream.Write(classroomJsonBytes);
                if (IsExit == true)
                {
                    ClassroomConfigFileStream.Close();
                }
                else
                {
                    ClassroomConfigFileStream.Flush();
                }
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            SaveData(true);
        }
    }
}
