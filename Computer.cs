/*
 * Copyright 2024 Đorđe Mančić
 */
using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace SavaMonitor
{
    public class Computer : INotifyPropertyChanged
    {
        public string ID { get; set; }
        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                NotifyPropertyChanged("Name");
            }
        }
        public string IPAddress;
        private bool isConnected = false;
        [JsonIgnore]
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
            set
            {
                isConnected = value;
                NotifyPropertyChanged("IsConnected");
                NotifyPropertyChanged("ConnectionStatus");
            }
        }
        [JsonIgnore]
        public string ConnectionStatus
        {
            get
            {
                if (IsConnected == true)
                {
                    return "On";
                }
                else
                {
                    return "Off";
                }
            }
        }
        private BitmapImage latestScreenShotPrivate;
        [JsonIgnore]
        public BitmapImage LatestScreenShot
        {
            get
            {
                return latestScreenShotPrivate;
            }
            set
            {
                latestScreenShotPrivate = value;
                NotifyPropertyChanged("LatestScreenShot");
            }
        }
        public Int64 LastResponse;
        public NetworkStream NetworkStream;
        public UdpClient UDPClient;
        public bool IsSharingScreen = false;

        public void GenerateID()
        {
            ID = App.GenerateRandomString(16);
        }

        public Computer()
        {
            GenerateID();
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.Freeze();
            this.LatestScreenShot = bitmapImage;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Student : Computer
    {
        public TcpClient TCPClient;

        public Student() : base()
        {

        }
    }

    public class Teacher : Computer
    {

    }
}
