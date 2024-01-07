using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace VMoiChat
{
    public partial class MainWindow : Window
    {
        private TcpServer tcpServer;
        public MainWindow()
        {
            InitializeComponent();
        }

        public void AddMessageToListBox(string message)
        {
            MessageListBox.Items.Add(message);
        }

        private void Window_Closing(object render, System.ComponentModel.CancelEventArgs e)
        {
            tcpServer.Stop();
        }

        private void UpdateServerStatus(bool running)
        {
            ServerStatusTextBlock.Text = "Server status: " + (running ? "Running" : "Stopped");
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(InputTextBox.Text) && !string.IsNullOrEmpty(InputNameRecipientBox.Text) && tcpServer != null)
            {
                string username = InputNameRecipientBox.Text;
                string message = InputTextBox.Text;

                Application.Current.Dispatcher.Invoke(() => { ((MainWindow)Application.Current.MainWindow).AddMessageToListBox($"{username}, {message}"); });
                InputTextBox.Clear();

                await tcpServer.SendMessageToClient(username, message);
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            tcpServer = new TcpServer(8888);
            tcpServer.Start();
            UpdateServerStatus(true);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (tcpServer != null)
            {
                tcpServer.Stop();
                tcpServer = null;
                UpdateServerStatus(false);
            } else
            {
                Application.Current.Dispatcher.Invoke(() => { ((MainWindow)Application.Current.MainWindow).AddMessageToListBox("Сервер уже остановлен, узбагойся"); });
            }
        }

        private async void ConnectedButton_Click(object sender, RoutedEventArgs e)
        {
            IPAddress ipAddress;
            int port;

            if (!string.IsNullOrEmpty(InputNameBox.Text) && tcpServer != null)
            {
                ipAddress = IPAddress.Parse("127.0.0.1");
                port = 8888;
                string username = InputNameBox.Text;
                await tcpServer.StartClient(ipAddress, port, username);
            }

        }

        private async void DisconnectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(InputNameBox.Text) && tcpServer != null)
            {
                string username = InputNameBox.Text;
                await tcpServer.DisconnectClient(username);
            }
        }
    }
}
