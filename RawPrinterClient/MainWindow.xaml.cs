using System;
using System.Windows;
using System.Drawing.Printing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RawPrinterClient
{
    public partial class MainWindow : Window
    {
        private WebSocketClient _webSocketClient;
        private PrinterManager _printerManager;

        public MainWindow()
        {
            InitializeComponent();
            LoadPrinters();
            InitializeWebSocket();
            _printerManager = new PrinterManager();
        }

        private void LoadPrinters()
        {
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                cmbPrinters.Items.Add(printer);
            }
            if (cmbPrinters.Items.Count > 0)
                cmbPrinters.SelectedIndex = 0;
        }

        private void InitializeWebSocket()
        {
            _webSocketClient = new WebSocketClient();
            _webSocketClient.MessageReceived += WebSocketClient_MessageReceived;
            _webSocketClient.ConnectionStatusChanged += WebSocketClient_ConnectionStatusChanged;
        }

        private void WebSocketClient_MessageReceived(object sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    AddToLog($"Received print job: {message}");
                    if (_printerManager.PrintRaw(message))
                    {
                        AddToLog("Print job completed successfully");
                    }
                    else
                    {
                        AddToLog("Failed to print job");
                    }
                }
                catch (Exception ex)
                {
                    AddToLog($"Error processing print job: {ex.Message}");
                }
            });
        }

        private void WebSocketClient_ConnectionStatusChanged(object sender, bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                lblWsStatus.Text = isConnected ? "Connected" : "Disconnected";
                lblWsStatus.Foreground = isConnected ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
                btnConnect.Content = isConnected ? "Disconnect" : "Connect";
            });
        }

        private void AddToLog(string message)
        {
            lstLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            lstLog.ScrollIntoView(lstLog.Items[lstLog.Items.Count - 1]);
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_webSocketClient.IsConnected)
            {
                await _webSocketClient.DisconnectAsync();
            }
            else
            {
                try
                {
                    await _webSocketClient.ConnectAsync(txtServerUrl.Text);
                }
                catch (Exception ex)
                {
                    AddToLog($"Connection error: {ex.Message}");
                }
            }
        }

        private void btnTestPrint_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPrinters.SelectedItem == null)
            {
                MessageBox.Show("Please select a printer first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string testData = "\x1B@\x1BtTest Print\n\nThis is a test print job.\n\n\n\n\n\n";
            _printerManager.SelectedPrinter = cmbPrinters.SelectedItem.ToString();
            
            try
            {
                if (_printerManager.PrintRaw(testData))
                {
                    AddToLog("Test print successful");
                }
                else
                {
                    AddToLog("Test print failed");
                }
            }
            catch (Exception ex)
            {
                AddToLog($"Test print error: {ex.Message}");
            }
        }

        private void btnReconnect_Click(object sender, RoutedEventArgs e)
        {
            btnConnect_Click(sender, e);
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            lstLog.Items.Clear();
        }
    }
}
