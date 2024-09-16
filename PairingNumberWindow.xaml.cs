/*
 * Copyright 2024 Đorđe Mančić
 */
using System.Windows;

namespace SavaMonitor
{
    public partial class PairingNumberWindow : Window
    {
        public PairingNumberWindow(string pairingNumber)
        {
            InitializeComponent();
            PairLabel.Text = pairingNumber;
        }
    }
}
