using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace VMoiChat
{
    public class TcpServer
    {
        private TcpListener _tcpListener;
        private CancellationTokenSource cancellationTokenSource;

        private List<ConnectedClient> connectedClients = new List<ConnectedClient>();

        public TcpServer(int port) 
        {
            _tcpListener = new TcpListener(IPAddress.Any, port);
        }

        public async void Start()
        {
            try
            {
                _tcpListener.Start();
                cancellationTokenSource = new CancellationTokenSource();
                string receivedText = "Сервер успешно запущен";
                Application.Current.Dispatcher.Invoke(() => { ((MainWindow)Application.Current.MainWindow).AddMessageToListBox(receivedText); });
                ListenForClients(_tcpListener, cancellationTokenSource.Token);


            } catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private async void ListenForClients(TcpListener listener, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    TcpClient tcpClient = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    HandleClientAsync(tcpClient, token);

                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException обработано: " + ex.Message);
            }
        }

        public async void HandleClientAsync(TcpClient tcpClient, CancellationToken token)
        {
            try
            {
                byte[] buffer = new byte[4096];
                NetworkStream networkStream = tcpClient.GetStream();

                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                    if (bytesRead == 0) break;
                    await networkStream.WriteAsync(buffer, 0, bytesRead, token);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Операция была отменена.");
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            } 
            finally 
            {
                tcpClient.Close();
            }
        }

        public async Task StartClient(IPAddress serverIpAddress, int port, string message)
        {
            TcpClient tcpClient = new TcpClient();

            await tcpClient.ConnectAsync(serverIpAddress, port).ConfigureAwait(false);

            NetworkStream? networkStream = null;
            StreamReader? streamReader = null;

            try
            {
                networkStream = tcpClient.GetStream();
                streamReader = new StreamReader(networkStream, Encoding.UTF8);

                byte[] data = Encoding.UTF8.GetBytes(message);

                await networkStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                char[] buffer = new char[4096];

                int bytesRead = await streamReader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                string receivedMessage = new string(buffer, 0, bytesRead);

                ConnectedClient connectedClient = new ConnectedClient { TcpClient = tcpClient, Username = receivedMessage };

                connectedClients.Add(connectedClient);
                Application.Current.Dispatcher.Invoke(() => { ((MainWindow)Application.Current.MainWindow).AddMessageToListBox(receivedMessage + " Подключился к серверу"); });
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public async Task SendMessageToClient(string username, string message)
        {
            if (username == "All")
            {
                foreach (var client in connectedClients)
                {
                    NetworkStream networkStream = client.TcpClient.GetStream();
                    StreamWriter streamWriter = new StreamWriter(networkStream, Encoding.UTF8);

                    try
                    {
                        await streamWriter.WriteLineAsync(message).ConfigureAwait(false);
                        await streamWriter.FlushAsync().ConfigureAwait(false);
                        Application.Current.Dispatcher.Invoke(() => { ((MainWindow)Application.Current.MainWindow).AddMessageToListBox($"Ответ от {client.Username}: {message}"); });
                    } catch (Exception ex)
                    {
                        MessageBox.Show($"{ex.Message}");
                    }
                }
            } else
            {
                ConnectedClient connectedClient = connectedClients.FirstOrDefault(client => client.Username == username);

                if (connectedClient != null)
                {
                    NetworkStream networkStream = connectedClient.TcpClient.GetStream();
                    StreamWriter streamWriter = new StreamWriter(networkStream, Encoding.UTF8);

                    try
                    {
                        await streamWriter.WriteLineAsync(message).ConfigureAwait(false);
                        await streamWriter.FlushAsync().ConfigureAwait(false);
                        Application.Current.Dispatcher.Invoke(() => { ((MainWindow)Application.Current.MainWindow).AddMessageToListBox($"Ответ от {connectedClient.Username}: {message}"); });

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                }
            }
        }

        public async Task DisconnectClient(string username)
        {
            ConnectedClient connectedClient = connectedClients.FirstOrDefault(client => client.Username == username);

            if (connectedClient != null)
            {
                if (connectedClient.TcpClient.Connected)
                {
                    NetworkStream networkStream = connectedClient.TcpClient.GetStream();
                    if (networkStream != null)
                    {
                        networkStream.Close();
                    }
                    connectedClient.TcpClient.Close();
                }
                connectedClients.Remove(connectedClient);

                Application.Current.Dispatcher.Invoke(() => { ((MainWindow)Application.Current.MainWindow).AddMessageToListBox($"{username} отключился от сервера"); });
            }
        }


        public void Stop() 
        {
            _tcpListener.Stop();
            cancellationTokenSource.Cancel();

            foreach (var client in connectedClients)
            {
                if (client.TcpClient.Connected)
                {
                    try
                    {
                        NetworkStream networkStream = client.TcpClient.GetStream();
                        if (networkStream != null)
                        {
                            networkStream.Close();
                        }
                        client.TcpClient.Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error disconnecting client {client.Username}: {ex.Message}");
                    }
                }
            }

            connectedClients.Clear();
            Application.Current.Dispatcher.Invoke(() => { ((MainWindow)Application.Current.MainWindow).AddMessageToListBox("Сервер остановлен"); });
        }
    }
}
