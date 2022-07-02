using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace ClientWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string fileImage = "";
        TcpClient client = new TcpClient();
        NetworkStream ns;
        Thread thread;
        private ChatMessage _message = new ChatMessage();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fileName = "setting.txt";
                IPAddress ip;
                int port;
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        ip = IPAddress.Parse(sr.ReadLine());
                        port = int.Parse(sr.ReadLine());
                    }
                }
                _message.UserName = txtUserName.Text;
                _message.UserId = Guid.NewGuid().ToString();
                client.Connect(ip, port);
                lbInfo.Items.Add($"Підключаємось до сервера {ip.ToString()}:{port}");
                ns = client.GetStream();
                thread = new Thread(o => RecieveData((TcpClient)o));
                thread.Start(client);
                _message.MessageType = TypeMessage.Login;
                _message.Text = "Приєднався до чату";
                byte[] buffer = _message.Serialize();
                var deser = ChatMessage.Deserialize(buffer);
                ns.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {

                MessageBox.Show("Problem connect server: " + ex.Message);
            }
        }
        private void RecieveData(TcpClient client)
        {
            NetworkStream ns = client.GetStream();
            byte[] recievedBytes = new byte[1000024];
            int byte_count;
            while((byte_count=ns.Read(recievedBytes, 0, recievedBytes.Length)) > 0)
            {
                Dispatcher.BeginInvoke(new Action(() => {
                    try
                    {
                        ChatMessage message = ChatMessage.Deserialize(recievedBytes);
                        switch (message.MessageType)
                        {
                            case TypeMessage.Login:
                                {
                                    if (message.UserId != _message.UserId)
                                        ShowMessage(message);
                                    break;
                                }
                            case TypeMessage.Logout: { if (message.UserId != _message.UserId) ShowMessage(message); }
                            break;
                            case TypeMessage.Message:
                                {
                                    ShowMessage(message);
                                }
                                 break;
                            case TypeMessage.Photo: {
                                    ShowMessage(message);
                                    break;
                                }
                                
                        }
                        lbInfo.Items.MoveCurrentToLast();
                        lbInfo.ScrollIntoView(lbInfo.Items.CurrentItem);
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }));
            }

           
        }
        private void ShowMessage(ChatMessage message)
        {//показ переданого повідомлення
            var imageSource = new BitmapImage(); //створення змінної для роботи з source зображення
            if (message.MessageType == TypeMessage.Photo)
            {
                using (var bmpStream = new MemoryStream(message.Image, 0, message.ImageSize))
                {
                    imageSource.BeginInit();
                    imageSource.StreamSource = bmpStream;
                    imageSource.CacheOption = BitmapCacheOption.OnLoad;
                    imageSource.EndInit();
                }
            }
            else
            {
                using (var bmpStream = new MemoryStream(message.ProfileImage, 0, message.ProfileImageSize))
                {//читання source з переданих файлів
                    imageSource.BeginInit();
                    imageSource.StreamSource = bmpStream;
                    imageSource.CacheOption = BitmapCacheOption.OnLoad;
                    imageSource.EndInit();
                }
            }
            imageSource.Freeze();
            Image image = new Image();
            image.Source = imageSource;//передання source для власне картинки
            if(message.MessageType == TypeMessage.Login)
            {
                user_list.Items.Add(new MessageView { Image = image, Text = message.UserName });
            }
            else if(message.MessageType == TypeMessage.Logout)
            {
                if (user_list.Items.Count != 0)
                {
                    user_list.Items.Remove(new MessageView { Text = message.UserName });
                }
            }
            else if(message.MessageType == TypeMessage.Photo)
            {
                lbInfo.Items.Add(new MessageView { Image = image, Text = message.UserName + " sended a photo" });
                return;
            }
            if (message.UserId == _message.UserId)
            {
                lbInfo.Items.Add(new MessageView() { Image = image, Text =  "You ("+message.UserName+") : " + message.Text });
            }
            else
            {
                lbInfo.Items.Add(new MessageView() { Image = image, Text = message.UserName + " : " + message.Text }); // додавання повідомлення до чату
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            _message.MessageType = TypeMessage.Logout;
            _message.Text = "Покинув чат";
            byte[] buffer = _message.Serialize();
            ns.Write(buffer, 0, buffer.Length);
            client.Client.Shutdown(SocketShutdown.Both);
            thread.Join();
            ns.Close();
            client.Client.Close();
        }

        private void bntSend_Click(object sender, RoutedEventArgs e)
        {
            _message.MessageType = TypeMessage.Message;
            _message.Text = txtText.Text;
            byte[] buffer = _message.Serialize();
            ns.Write(buffer, 0, buffer.Length);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();//відкривання дирикторії
            openFileDialog.Filter = "Image(*.bmp, *.jpg, *.png)|*.bmp; *.jpg; *.png|ALL(*.*)|*.*";//обрання типу для обраних файлів
            if (openFileDialog.ShowDialog() == true) {
                Avatar.Source = new BitmapImage(new Uri(openFileDialog.FileName));//задання шляху для source профільної фотографії
                Byte[] bytes = File.ReadAllBytes(openFileDialog.FileName); //перетворення назви у байти
                _message.ProfileImageSize = bytes.Length;//передання їх кількості як місткості зоображення
                _message.ProfileImage = bytes; /*Convert.ToBase64String(bytes);*/  //передання байтів як власне зоображення
            } 

        }

        private void send_photo_btn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image(*.bmp, *.jpg, *.png)|*.bmp; *.jpg; *.png|ALL(*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                origin_photo.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                Byte[] bytes = File.ReadAllBytes(openFileDialog.FileName);
                _message.ImageSize = bytes.Length;
                _message.Image = bytes; /*Convert.ToBase64String(bytes);*/
            }
            _message.MessageType = TypeMessage.Photo;
            byte[] buffer = _message.Serialize();
            ns.Write(buffer, 0, buffer.Length);
        }
    }
}
