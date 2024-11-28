using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MusicPlayer.Models;

namespace MusicPlayer
{
    public partial class MainPage : Page
    {
        public ObservableCollection<Song> AddedSongs { get; set; } = new ObservableCollection<Song>();
        public ObservableCollection<Song> RecentSongs { get; set; } = new ObservableCollection<Song>();
        public ObservableCollection<Playlist> Playlists { get; set; } = new ObservableCollection<Playlist>();

        private int userId;

        public List<Song> Songs { get; private set; }
        public int CurrentSongIndex { get; private set; } = -1;

        public int CurrentUserId { get; private set; }

        public MainPage(int userId)
        {
            InitializeComponent();
            DataContext = this;
            this.userId = userId;
            CurrentUserId = userId;

            DatabaseHelper.InitializeDatabase(); // Проверяем, что таблица существует

            LoadAddedSongs();
            LoadRecentSongs();
            LoadUserPlaylists();

            // Привязка данных к спискам
            songListBox.ItemsSource = AddedSongs;
            recentSongsListBox.ItemsSource = RecentSongs;
        }

        public void LoadAddedSongs()
        {
            AddedSongs.Clear(); // Очистка текущей коллекции

            var songs = DatabaseHelper.GetSongs(userId); // Получение всех песен из базы данных
            Songs = new List<Song>(songs); // Инициализация поля Songs

            foreach (var song in songs)
            {
                AddedSongs.Add(song); // Добавляем каждую песню в коллекцию
            }
        }


        public void LoadRecentSongs()
        {
            RecentSongs.Clear(); // Очистка текущей коллекции

            var recent = DatabaseHelper.GetRecentSongs(userId);
            foreach (var recentSong in recent)
            {
                var song = new Song
                {
                    Id = recentSong.Id,
                    Title = recentSong.Title,
                    Artist = recentSong.Artist,
                    FilePath = recentSong.FilePath,
                    ImagePath = recentSong.ImagePath
                };
                RecentSongs.Add(song); // Добавляем объект Song
            }
        }

        private void LoadUserPlaylists()
        {
            Playlists.Clear(); // Очистка списка плейлистов

            var playlists = DatabaseHelper.GetUserPlaylists(userId);
            foreach (var playlist in playlists)
            {
                Playlists.Add(new Playlist
                {
                    Id = playlist.Id, // ID плейлиста
                    Name = playlist.Name, // Название
                    ImagePath = playlist.ImagePath // Путь к изображению
                });
            }

            // Если у вас есть элемент управления для отображения плейлистов:
            playlistsPanel.ItemsSource = Playlists;
        }

        private void PlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int playlistId)
            {
                // Переход на страницу плейлиста
                var playlistPage = new PlaylistPage(playlistId, userId);
                NavigationService.Navigate(playlistPage);
            }
        }

        private void songListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Логика для обновления информации о выбранной песне
            if (songListBox.SelectedItem is Song selectedSong)
            {
                string imagePath = string.IsNullOrEmpty(selectedSong.ImagePath) ? "E:\\Downloads\\IMG_0804.PNG" : selectedSong.ImagePath;
            }
        }

        private void songListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (songListBox.SelectedItem is Song selectedSong && MainWindow.Instance != null)
            {
                MainWindow.Instance.PlaySelectedSong(selectedSong.FilePath);
                LoadRecentSongs(); // Обновляем список последних песен
            }
        }

        private void RecentSongsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (recentSongsListBox.SelectedItem is Song selectedSong && MainWindow.Instance != null)
            {
                MainWindow.Instance.PlaySelectedSong(selectedSong.FilePath);
                LoadRecentSongs(); // Обновляем список последних песен
            }
        }

        public void PlaySongAt(int index)
        {
            if (Songs == null || !Songs.Any())
            {
                MessageBox.Show("Список песен пуст.");
                return;
            }

            if (index >= 0 && index < Songs.Count)
            {
                CurrentSongIndex = index;
                MainWindow.Instance.PlaySelectedSong(Songs[CurrentSongIndex].FilePath);
            }
            else
            {
                MessageBox.Show("Индекс песни выходит за пределы списка.");
            }
        }


        public void DeleteSelectedSong()
        {
            if (songListBox.SelectedItem is Song selectedSong)
            {
                // Подтверждение удаления
                var result = MessageBox.Show($"Удалить песню '{selectedSong.Title}'?", "Удаление песни", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    // Удаление из базы данных
                    DatabaseHelper.DeleteSong(selectedSong.Id);

                    // Обновление интерфейса
                    LoadAddedSongs();
                    MessageBox.Show("Песня удалена.");
                }
            }
            else
            {
                MessageBox.Show("Выберите песню для удаления.");
            }
        }

        
    }
}
