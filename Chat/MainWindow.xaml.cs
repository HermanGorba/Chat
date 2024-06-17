using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Chat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UdpClient? udpClient;
        private IPEndPoint remoteEndPoint;
        private bool isListening;
        private IPAddress multicastGroup;
        private int localPort;
        private string username;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void JoinChatButton_Click(object sender, RoutedEventArgs e)
        {
            if (udpClient == null)
            {
                multicastGroup = IPAddress.Parse(txtMulticastGroupId.Text);
                localPort = int.Parse(txtPort.Text);
                username = txtUsername.Text;

                udpClient = new UdpClient();

                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, localPort));
                udpClient.JoinMulticastGroup(multicastGroup);

                txtMulticastGroupId.IsReadOnly = true;
                txtPort.IsReadOnly = true;
                txtUsername.IsReadOnly = true;

                remoteEndPoint = new IPEndPoint(multicastGroup, localPort);

                isListening = true;
                Task.Run(() => ReceiveMessagesAsync());

                SendMessageAsync($"{username} joined the chat.\n");
            }
        }

        private void LeaveChatButton_Click(object sender, RoutedEventArgs e)
        {
            if (udpClient != null)
            {
                SendMessageAsync($"{username} left the chat.\n");

                isListening = false;
                udpClient.DropMulticastGroup(multicastGroup);
                udpClient.Close();
                udpClient = null;

                txtMulticastGroupId.IsReadOnly = false;
                txtPort.IsReadOnly = false;
                txtUsername.IsReadOnly = false;
            }
        }

        private async void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            string message = txtMessage.Text;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(message))
            {
                string fullMessage = $"{DateTime.Now}\n{username}:\n{message}\n";
                await SendMessageAsync(fullMessage);
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            using var receiver = new UdpClient();
            receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            receiver.Client.Bind(new IPEndPoint(IPAddress.Any, localPort));
            receiver.JoinMulticastGroup(multicastGroup);
            while (isListening)
            {
                var result = await receiver.ReceiveAsync();
                string text = Encoding.UTF8.GetString(result.Buffer);
                Dispatcher.Invoke(() => txtChat.AppendText($"{text}\n"));
            }
        }

        private async Task SendMessageAsync(string message)
        {
            if (udpClient == null) return;
            byte[] data = Encoding.UTF8.GetBytes(message);
            await udpClient?.SendAsync(data, data.Length, remoteEndPoint);
            txtMessage.Clear();
        }
    }
}
