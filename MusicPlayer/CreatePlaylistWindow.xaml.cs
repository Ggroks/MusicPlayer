using Microsoft.Win32;
using System;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;

namespace MusicPlayer
{
    public partial class CreatePlaylistWindow : Window
    {
        public string PlaylistName { get; private set; }
        public string ImagePath { get; private set; }

        public CreatePlaylistWindow()
        {
            InitializeComponent();
        }

        private void btnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
            if (openFileDialog.ShowDialog() == true)
            {
                ImagePath = openFileDialog.FileName;
                imgPlaylistPreview.Source = new BitmapImage(new Uri(ImagePath));
            }
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            PlaylistName = txtPlaylistName.Text;
            if (!string.IsNullOrEmpty(PlaylistName) && !string.IsNullOrEmpty(ImagePath))
            {
                DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Заполните все поля!");
            }
        }
    }
}
