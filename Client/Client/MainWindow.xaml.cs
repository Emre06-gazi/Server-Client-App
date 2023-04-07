using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ClientUygulamasi
{
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;
        private TcpListener listener;
        private bool isListening;

        public MainWindow()
        {
            InitializeComponent();
            ConnectToServer();
            ReceiveResponseFromServerAsync();
            GetInitialValuesFromServer();
        }

        private void ConnectToServer()
        {
            try
            {
                // Server IP adresi ve port numarasını buraya girin
                client = new TcpClient("127.0.0.1", 8888);
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server'a bağlanırken bir hata oluştu: " + ex.Message);
            }
        }

        private void GetInitialValuesFromServer()
        {
            // Server'dan lamba ve text değerlerini alıp ekranda göster
            string initialData = SendMessageToServer("INITIAL");
            string[] values = initialData.Split('|');
            if (values.Length == 2)
            {
                // Lamba durumunu güncelle
                bool isLampOn = bool.Parse(values[0]);
                UpdateLampButton(isLampOn);

                // Text değerini güncelle
                string textValue = values[1];
                UpdateTextButton(textValue);
            }
        }

        private void LampButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Lamba butonuna basıldığında server'a mesaj gönder
                string message = "LAMP|" + (LampButton.Background == Brushes.Yellow ? "OFF" : "ON");
                string response = SendMessageToServer(message);

                if (response.Contains("|"))
                {
                    UpdateLampButton(response.Split('|')[1].ToUpper() == "ON" ? true : false);
                }
                else
                {
                    UpdateLampButton(response == "ON" ? true : false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lamba butonuna basıldığında bir hata oluştu: " + ex.Message);
            }
        }

        private void UpdateLampButton(bool isLampOn)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Lamba durumuna göre lamba butonunun rengini güncelle
                    LampButton.Background = isLampOn ? Brushes.Yellow : Brushes.Gray;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lamba butonunun rengi güncellenirken bir hata oluştu: " + ex.Message);
            }
        }

        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            // Text butonuna basıldığında server'a mesaj gönder
            string message = "TEXT";
            string response = SendMessageToServer(message);
            // Server'dan "TEXT|değer" şeklinde yanıt gelirse text değerini güncelle
            UpdateTextButton(response);
        }

        private void UpdateTextButton(string response)
        {
            // Text değerini güncelle
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Texte göre text butonunun değerini güncelle
                    TextButton.Content = response;
                });
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        private string SendMessageToServer(string message)
        {
            try
            {
                // Server'a mesaj gönder
                byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);

                //Server'dan yanıt al
                //data = new byte[256];
                //string response = "";
                //int bytes = stream.Read(data, 0, data.Length);
                //response = System.Text.Encoding.UTF8.GetString(data, 0, bytes);

                return message;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server ile iletişim kurulurken bir hata oluştu: " + ex.Message);
                return "";
            }
        }
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;


        private void ReceiveResponseFromServerAsync()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        byte[] bufferToRead = new byte[256];
                        int len = stream.Read(bufferToRead, 0, bufferToRead.Length);
                        if (len > 0)
                        {
                            byte[] data = new byte[256];
                            string result = System.Text.Encoding.UTF8.GetString(bufferToRead, 0, len);
                            //if (result.Contains("TEXT"))
                            //{
                            dispatcher.Invoke(() =>
                            {
                                if (result.Contains("|"))
                                {
                                    UpdateLampButton(result.Split('|')[1].ToUpper() == "ON" ? true : false);
                                }
                                else
                                {
                                    UpdateLampButton(result.ToUpper() == "ON" ? true : false);
                                }
                            });
                            
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    { }
                }
            });
        }

        private Brush originalLampButtonBackground;
        private void LampButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            originalLampButtonBackground = LampButton.Background;
        }


    }
}