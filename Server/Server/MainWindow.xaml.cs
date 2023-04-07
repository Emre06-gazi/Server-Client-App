using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Server
{
    public partial class MainWindow : Window
    {
        // Bağlı client soketlerinin tutulacağı liste
        private List<Socket> _clientSockets = new List<Socket>();

        // Server soketi
        private Socket _serverSocket;

        // Verilerin alınacağı buffer boyutu
        private byte[] _buffer = new byte[1024];

        public MainWindow()
        {
            InitializeComponent();

            // Server'ı başlat
            SetupServer();
        }

        private void SetupServer()
        {
            try
            {
                // Server soketini oluştur ve istemcilerin bağlanacağı portu belirle
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 8888));

                // Dinleme moduna al
                _serverSocket.Listen(5);

                // İstemci bağlandığında tetiklenecek metod belirlenir ve başlatılır
                _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);

                // Server başarıyla başlatıldı mesajı yazdırılır
                statusLabel.Content = "Server başlatıldı.";
            }
            catch (Exception ex)
            {
                // Server başlatılırken bir hata oluşursa mesaj kutusu ile kullanıcıya bilgi verilir
                Console.WriteLine(ex.Message);
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Yeni bir istemci bağlandığında tetiklenecek metod
            Socket socket = _serverSocket.EndAccept(ar);

            // Bağlanan istemci soketi listeye eklenir
            _clientSockets.Add(socket);

            // Bağlanan istemci sayısı gösterilir
            statusLabel.Dispatcher.Invoke(() => statusLabel.Content = $"Bağlanılan Clientlar: {_clientSockets.Count}");

            // Veri almak için sokete dinleme işlemi başlatılır
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);

            // Yeni bir istemci bağlantısı için dinleme modu tekrar aktif edilir
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            // İstemciden veri alındığında tetiklenecek metod
            Socket socket = (Socket)ar.AsyncState;

            try
            {
                // Gelen verinin boyutu alınır
                int received = socket.EndReceive(ar);

                // Gelen veri buffer'a kopyalanır
                byte[] dataBuf = new byte[received];
                Array.Copy(_buffer, dataBuf, received);

                // Gelen veri ASCII kodlaması ile stringe çevrilir
                string text = Encoding.ASCII.GetString(dataBuf);

                // Gelen veri, lamba aç/kapa işlemi ise
                if (text.StartsWith("LAMP"))
                {
                    // Durum metni ayrıştırılır
                    string state = text.Split('|')[1];
                    // Eğer durum "Açık" veya "Kapalı" ise
                    if (state == "ON" || state == "OFF")
                    {
                        // Lamba görseli değiştirilir ve değişiklik tüm istemcilere gönderilir
                        LambaImage.Dispatcher.Invoke(() => LambaImage.Source = state == "ON" ? new BitmapImage(new Uri("LampOn.jpg", UriKind.Relative)) : new BitmapImage(new Uri("LampOff.jpg", UriKind.Relative)));
                        BroadcastMessage(text);
                    }
                }
                // Gelen veri, text kutusunda gösterilecek metin ise
                else 
                {
                    // Gösterilecek metin ayrıştırılır
                    string value = text.Replace("Text:", "");
                    // Metin kutusu güncellenir ve değişiklik tüm istemcilere gönderilir
                    TextInput.Dispatcher.Invoke(() => TextInput.Text = value);
                    BroadcastMessage(text);
                }
                // Veri alımı devam eder
                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            }
            // Hata durumunda
            catch (Exception ex)
            {
                // Hata mesajı gösterilir
                Console.WriteLine(ex.Message);
                // Socket kapatılır ve listeden çıkarılır
                socket.Close();
                _clientSockets.Remove(socket);
                // Bağlı istemci sayısı güncellenir
                statusLabel.Dispatcher.Invoke(() => statusLabel.Content = $"Bağlanılan Clientlar: {_clientSockets.Count}");
            }
        }

        // Verilen iletiyi ASCII kodlaması kullanarak tüm soketlere gönderir.
        private void BroadcastMessage(string message)
        {
            // İletiyi ASCII kodlaması kullanarak byte dizisine dönüştürür.
            byte[] buffer = Encoding.ASCII.GetBytes(message);

            // Her bir soket nesnesi için döngü oluşturulur.
            foreach (Socket socket in _clientSockets)
            {
                // Byte dizisi gönderilir.
                socket.Send(buffer);
            }
        }


        private void LambaButton_Click(object sender, RoutedEventArgs e)
        {
            // Lamba durumu değiştirilir.


            var durum = LambaImage.Source;

            var ImageUrl = new Uri($"{durum}");

            BitmapImage image = new BitmapImage(ImageUrl);

          

            string state = image.UriSource.AbsolutePath.EndsWith("LampOff.jpg") ? "On" : "Off";

            LambaImage.Source = new BitmapImage(new Uri($"pack://application:,,,/Lamp{state}.jpg"));

            // Durum mesajı gösterilir ve tüm clientlara durum bildirimi yapılır.


            string statusMessage = $"{state}";

            StatusMessage.Text = statusMessage;
            StatusMessage.Visibility = Visibility.Visible;
            BroadcastMessage(statusMessage);
        }
       

        private void TextInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Text değiştiğinde son değer alınır ve tüm clientlara gönderilir.
            string text = TextInput.Text;
            BroadcastMessage(text);
        }

        private void RandomButton_Click(object sender, RoutedEventArgs e)
        {
            // Rastgele bir sayı oluşturulur ve text alanına yazılır.
            Random random = new Random();
            int randomNumber = random.Next(10000);
            TextInput.Text = randomNumber.ToString();

            // Durum mesajı gösterilir ve tüm clientlara sayı gönderimi yapılır.
            string statusMessage2 = $"TEXT {randomNumber}";
            StatusMessage2.Text = statusMessage2;
            StatusMessage2.Visibility = Visibility.Visible;
            BroadcastMessage(statusMessage2);
        }
    }
}
