using Microsoft.Win32;
using System.Windows;
namespace MusicPlayer
{
    public partial class AddSongWindow : Window
    {
        public string SongTitle => TitleTextBox.Text;
        public string Artist => ArtistTextBox.Text;
        public string Genre => GenreTextBox.Text;
        public string ImagePath => ImagePathTextBox.Text;

        public AddSongWindow()
        {
            InitializeComponent();
        }

        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                ImagePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SongTitle) || string.IsNullOrWhiteSpace(Artist))
            {
                MessageBox.Show("Пожалуйста, заполните название и имя исполнителя.");
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
