using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System;
using MusicPlayer.Models;
using System.Windows.Media;

namespace MusicPlayer
{
    public partial class PlaylistPage : Page
    {
        public int playlistId;
        private int userId;
        public List<Song> Songs { get; private set; }


        public PlaylistPage(int playlistId, int userId)
        {
            InitializeComponent();
            this.playlistId = playlistId;
            this.userId = userId;
            DatabaseHelper.InitializeDatabase();
            LoadPlaylistData();
            LoadSongs();
            RefreshMainWindowPlaylists();
        }


        private void LoadPlaylistData()
        {
            var playlist = DatabaseHelper.GetPlaylistInfo(playlistId);
            if (playlist != null)
            {
                playlistTitle.Text = playlist.Name;
                string imagePath = string.IsNullOrEmpty(playlist.ImagePath) ? "default_playlist.png" : playlist.ImagePath;
                playlistImage.Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
            }
            else
            {
                MessageBox.Show("Плейлист не найден.");
            }
        }

        private void LoadSongs()
        {
            // Получаем список песен в плейлисте
            List<Song> songs = DatabaseHelper.GetSongsForPlaylist(playlistId);
            playlistSongsDataGrid.ItemsSource = songs;

            Songs = DatabaseHelper.GetSongsForPlaylist(playlistId);
            for (int i = 0; i < Songs.Count; i++)
            {
                Songs[i].RowNumber = i + 1; // Нумерация с 1
            }
            playlistSongsDataGrid.ItemsSource = Songs;

        }

        private void btnAddSongToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно добавления песни
            var addSongToPlaylistWindow = new AddSongToPlaylistWindow(userId);
            if (addSongToPlaylistWindow.ShowDialog() == true)
            {
                int selectedSongId = addSongToPlaylistWindow.SelectedSongId;

                // Добавляем выбранную песню в плейлист
                if (selectedSongId > 0)
                {
                    DatabaseHelper.AddSongToPlaylist(selectedSongId, playlistId);
                    LoadSongs();
                    MessageBox.Show("Песня успешно добавлена в плейлист.");
                }
                else
                {
                    MessageBox.Show("Песня не выбрана.");
                }
            }
        }

        private void playlistSongsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (playlistSongsDataGrid.SelectedItem is Song selectedSong)
            {
                if (!string.IsNullOrEmpty(selectedSong.FilePath))
                {
                    MainWindow.Instance.PlaySelectedSong(selectedSong.FilePath);
                }
                else
                {
                    MessageBox.Show("Путь к файлу песни не найден.");
                }
            }
        }

        public void DeleteSelectedSongFromPlaylist()
        {
            if (playlistSongsDataGrid.SelectedItem is Song selectedSong)
            {
                var result = MessageBox.Show($"Удалить песню '{selectedSong.Title}' из плейлиста?", "Удаление песни", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    DatabaseHelper.RemoveSongFromPlaylist(selectedSong.Id, playlistId);
                    LoadSongs();
                    MessageBox.Show("Песня удалена из плейлиста.");
                }
            }
            else
            {
                MessageBox.Show("Выберите песню для удаления.");
            }
        }

        private void btnDeleteSongPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (playlistSongsDataGrid.SelectedItem is Song selectedSong)
            {
                var result = MessageBox.Show($"Удалить песню '{selectedSong.Title}' из плейлиста?", "Удаление песни", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    DatabaseHelper.RemoveSongFromPlaylist(selectedSong.Id, playlistId);
                    LoadSongs();
                    MessageBox.Show("Песня удалена из плейлиста.");
                }
            }
            else
            {
                MessageBox.Show("Выберите песню для удаления.");
            }

        }

        private void btnDeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Вы уверены, что хотите удалить этот плейлист?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Удаляем плейлист из базы данных
                bool success = DatabaseHelper.DeletePlaylist(playlistId);

                if (success)
                {
                    MessageBox.Show("Плейлист успешно удалён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Обновляем список плейлистов в главном окне
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.LoadUserPlaylists();
                        }
                    });

                    // Возвращаемся на предыдущую страницу
                    if (NavigationService != null && NavigationService.CanGoBack)
                    {
                        NavigationService.GoBack(); // Возвращаемся на предыдущую страницу
                    }
                    else
                    {
                        MessageBox.Show("Невозможно вернуться на предыдущую страницу.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                }
                else
                {
                    MessageBox.Show("Ошибка при удалении плейлиста. Попробуйте ещё раз.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }




        private void RefreshMainWindowPlaylists()
        {
            if (MainWindow.Instance != null)
            {
                MainWindow.Instance.LoadUserPlaylists();
            }
            else
            {
                MessageBox.Show("Главное окно недоступно.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }



    }
}
