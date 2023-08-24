using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace chat_client_cs
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string name = "";
        bool flag = false;
        public MainWindow()
        {
            InitializeComponent();
        }



        Dictionary<String, String> GetMessageDict(string name, string message, string status)
        {
            Dictionary<String, String> msgDict = new Dictionary<string, string>();
            msgDict.Add("name", name);
            msgDict.Add("message", message);
            msgDict.Add("status", status);
            return msgDict;
        }

        class mMessage
        {
            Dictionary<String, String> dictMsg = new Dictionary<String, String>();
            public mMessage(Dictionary<String, String> dict)
            {
                this.dictMsg = dict;
            }

            public mMessage(String str)
            {
                this.dictMsg = JsonConvert.DeserializeObject<Dictionary<String, String>>(str);
            }

            public String getJsonString()
            {
                return JsonConvert.SerializeObject(this.dictMsg);
            }

            public Dictionary<String, String> getDictMsg() { return this.dictMsg; }
        }
        
        Socket clientSocket;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        public void ClientSocketConnect(String strAddress, int serverPort)
        {
            if (clientSocket != null && this.clientSocket.Connected)
            {
                this.clientSocket.Close();
            }
            IPAddress serverAddress = IPAddress.Parse(strAddress);
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                
            try
            {
                // Connect to the server
                clientSocket.Connect(new IPEndPoint(serverAddress, serverPort));
                Task.Run(() =>
                {
                    chat.Dispatcher.Invoke(() =>
                    {
                        chat.Text += "\n";
                    });
                });
                setSendThread(GetLocalIPAddress(), "connect");
                setReceiveThread();
                Console.WriteLine("Connected to the server.");
                //Console.WriteLine($"Sent message: {message}");
                        
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                MessageBox.Show("Server offline.");
            }
                
        }

        public void setReceiveThread()
        {
            Task.Run(() =>
            {
                try
                {
                    //while (!_cts.Token.IsCancellationRequested)
                    while (flag)
                    {
                        // Optionally: Receive a response from the server
                        byte[] buffer = new byte[4096];
                        int bytesRead = clientSocket.Receive(buffer);
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Task.Run(() =>
                        {
                            chat.Dispatcher.Invoke(() =>
                            {
                                chat.Text += receivedMessage;
                            });
                        });
                    }
                }
                catch (SocketException ex)
                {
                    // Handle any socket-specific errors, like if the client forcibly closes the connection
                    if(clientSocket.Connected)
                    {
                        MessageBox.Show("Disconnect.");
                    }
                    else
                    {
                        MessageBox.Show("Disconnect");
                        // Clean up resources
                        clientSocket.Close();
                        //Console.WriteLine($"Socket Exception: {ex.Message}");
                    }
                }




            });
        }
        public void setSendThread(string message, string status)
        {
            Task.Run(() =>
            {
                try
                {
                    // Send a message to the server
                    Dictionary<String, String> msgDict = new Dictionary<String, String>();
                    msgDict.Add("name", name);
                    msgDict.Add("message", message);
                    msgDict.Add("status", status);
                    mMessage msg = new mMessage(msgDict);
                    byte[] messageBytes = Encoding.UTF8.GetBytes(msg.getJsonString());
                    clientSocket.Send(messageBytes);
                    if(status == "disconnect")
                    {
                        chat.Dispatcher.Invoke(() =>
                        {
                            chat.Text += name + " left!\n";
                        });
                        clientSocket.Close();
                    }
                }
                catch (Exception ex)
                {
                    // Handle any socket-specific errors, like if the client forcibly closes the connection
                    MessageBox.Show("Server offline");
                    Console.WriteLine($"Socket Exception: {ex.Message}");
                }
            });
        }

        

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            name = inputName.Text;
            _cts = new CancellationTokenSource();
            flag = true;
            if(clientSocket != null && clientSocket.Connected) { clientSocket.Close(); }
            ClientSocketConnect(inputIp.Text, int.Parse(inputPort.Text));
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            setSendThread(inputMessage.Text, "");
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            flag = false;
            setSendThread("Bye!", "disconnect");
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
