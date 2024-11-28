using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.IO;
using System.Windows.Media;
using System.Data.SQLite;
using MusicPlayer.Models;
using System.Windows.Media.Imaging;
using System.Linq;

namespace MusicPlayer
{
    public partial class MainWindow : Window
    {
        // Поля и флаги
        private bool isPlaying = false;              // Состояние воспроизведения
        private bool isShuffleEnabled = false;      // Режим перемешивания
        private bool isRepeatEnabled = false;       // Режим повторения
        private MediaPlayer mediaPlayer = new MediaPlayer(); // Медиа-плеер
        private DispatcherTimer timer;              // Таймер для обновления UI
        private List<Song> shuffledSongs = new List<Song>(); // Перемешанный список песен
        private int currentShuffledIndex = -1;      // Индекс текущей песни в перемешанном списке
        private int userId;                         // ID текущего пользователя
        public static MainWindow Instance { get; private set; }

        // Конструкторы
        public MainWindow(int userId)
        {
            InitializeComponent();
            this.userId = userId;
            Instance = this;

            InitializeMediaPlayer();
            InitializeTimer();
            LoadUserPlaylists();

            // Загружаем главную страницу
            MainFrame.Navigate(new MainPage(userId));
        }

        public MainWindow() : this(0) { }

        // Инициализация MediaPlayer
        private void InitializeMediaPlayer()
        {
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.Volume = 0.5; // Установка громкости по умолчанию
        }

        // Инициализация таймера
        private void InitializeTimer()
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
        }

        // ------------------- Управление воспроизведением -------------------

        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                mediaPlayer.Pause();
                btnPlayPause.Content = "Play";
            }
            else
            {
                mediaPlayer.Play();
                btnPlayPause.Content = "Pause";
            }
            isPlaying = !isPlaying;
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (isShuffleEnabled && shuffledSongs.Any())
            {
                // Переход к следующей песне в перемешанном списке
                currentShuffledIndex = (currentShuffledIndex + 1) % shuffledSongs.Count;
                PlaySelectedSong(shuffledSongs[currentShuffledIndex].FilePath);
            }
            else if (MainFrame.Content is MainPage mainPage)
            {
                // Переход к следующей песне на главной странице
                int currentIndex = mainPage.Songs.FindIndex(s => s.FilePath == mediaPlayer.Source?.OriginalString);

                if (currentIndex >= 0 && currentIndex + 1 < mainPage.Songs.Count)
                {
                    PlaySelectedSong(mainPage.Songs[currentIndex + 1].FilePath);
                }
                else
                {
                    MessageBox.Show("Следующей песни нет.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (MainFrame.Content is PlaylistPage playlistPage)
            {
                // Переход к следующей песне в плейлисте
                int currentIndex = playlistPage.Songs.FindIndex(s => s.FilePath == mediaPlayer.Source?.OriginalString);

                if (currentIndex >= 0 && currentIndex + 1 < playlistPage.Songs.Count)
                {
                    PlaySelectedSong(playlistPage.Songs[currentIndex + 1].FilePath);
                }
                else
                {
                    MessageBox.Show("Следующей песни нет.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (isShuffleEnabled && currentShuffledIndex > 0)
            {
                // Переход к предыдущей песне в перемешанном списке
                currentShuffledIndex--;
                PlaySelectedSong(shuffledSongs[currentShuffledIndex].FilePath);
            }
            else if (MainFrame.Content is MainPage mainPage)
            {
                // Переход к предыдущей песне на главной странице
                int currentIndex = mainPage.Songs.FindIndex(s => s.FilePath == mediaPlayer.Source?.OriginalString);

                if (currentIndex > 0)
                {
                    PlaySelectedSong(mainPage.Songs[currentIndex - 1].FilePath);
                }
                else
                {
                    MessageBox.Show("Предыдущей песни нет.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (MainFrame.Content is PlaylistPage playlistPage)
            {
                // Переход к предыдущей песне в плейлисте
                int currentIndex = playlistPage.Songs.FindIndex(s => s.FilePath == mediaPlayer.Source?.OriginalString);

                if (currentIndex > 0)
                {
                    PlaySelectedSong(playlistPage.Songs[currentIndex - 1].FilePath);
                }
                else
                {
                    MessageBox.Show("Предыдущей песни нет.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            if (isRepeatEnabled)
            {
                mediaPlayer.Position = TimeSpan.Zero;
                mediaPlayer.Play();
            }
            else
            {
                PlayNextSong();
            }
        }

        // Воспроизведение выбранной песни
        public void PlaySelectedSong(string selectedSongPath)
        {
            Song song = DatabaseHelper.GetSongInfo(selectedSongPath);
            if (song == null || string.IsNullOrEmpty(song.FilePath))
            {
                MessageBox.Show("Не удалось загрузить песню.");
                return;
            }

            mediaPlayer.Open(new Uri(song.FilePath));
            mediaPlayer.Play();
            isPlaying = true;
            btnPlayPause.Content = "Pause";

            // Обновление UI
            songTitle.Text = song.Title ?? "Неизвестно";
            artistName.Text = song.Artist ?? "Неизвестно";
            songImage.Source = new BitmapImage(new Uri(song.ImagePath ?? "E:\\Downloads\\IMG_0804.PNG"));
            DatabaseHelper.UpdateLastPlayed(userId, song.Id);

            timer.Start();
        }

        // Воспроизведение следующей песни
        private void PlayNextSong()
        {
            if (isShuffleEnabled && shuffledSongs.Any())
            {
                currentShuffledIndex = (currentShuffledIndex + 1) % shuffledSongs.Count;
                PlaySelectedSong(shuffledSongs[currentShuffledIndex].FilePath);
            }
           
        }

        // Воспроизведение предыдущей песни
        private void PlayPreviousSong()
        {
            if (isShuffleEnabled && currentShuffledIndex > 0)
            {
                currentShuffledIndex--;
                PlaySelectedSong(shuffledSongs[currentShuffledIndex].FilePath);
            }
           
        }

        // ------------------- Таймер и таймлайн -------------------

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                timelineSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                timelineSlider.Value = mediaPlayer.Position.TotalSeconds;
                currentPositionText.Text = mediaPlayer.Position.ToString(@"mm\:ss");
                totalDurationText.Text = mediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
            }
        }

        private void timelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                mediaPlayer.Position = TimeSpan.FromSeconds(e.NewValue);
            }
        }

        // ------------------- Работа с плейлистами -------------------

        public void LoadUserPlaylists()
        {
            PlaylistsContainer.Children.Clear();
            List<Playlist> playlists = DatabaseHelper.GetUserPlaylists(userId);

            foreach (var playlist in playlists)
            {
                Button playlistButton = new Button
                {
                    Content = playlist.Name,
                    Tag = playlist.Id,
                    Margin = new Thickness(0, 5, 0, 0),
                    Background = new SolidColorBrush(Colors.Black),
                    Foreground = new SolidColorBrush(Colors.White),
                    Height = 35
                };
                playlistButton.Click += PlaylistButton_Click;
                PlaylistsContainer.Children.Add(playlistButton);
            }
        }

        private void PlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int playlistId)
            {
                MainFrame.Navigate(new PlaylistPage(playlistId, userId));
            }
        }

        private void btnCreatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            var createPlaylistWindow = new CreatePlaylistWindow();
            if (createPlaylistWindow.ShowDialog() == true)
            {
                DatabaseHelper.AddPlaylist(userId, createPlaylistWindow.PlaylistName, createPlaylistWindow.ImagePath);
                LoadUserPlaylists();
            }
        }

        // ------------------- Режимы Shuffle и Repeat -------------------

        private void btnShuffle_Click(object sender, RoutedEventArgs e)
        {
            isShuffleEnabled = !isShuffleEnabled;
            btnShuffle.Background = isShuffleEnabled
                ? new SolidColorBrush(Colors.Red)
                : new SolidColorBrush(Colors.Gray);

            if (isShuffleEnabled)
            {
                ShuffleSongs();
            }
            else
            {
                shuffledSongs.Clear();
            }
        }

        private void ShuffleSongs()
        {
            if (MainFrame.Content is MainPage mainPage && mainPage.Songs.Any())
            {
                shuffledSongs = mainPage.Songs.OrderBy(_ => Guid.NewGuid()).ToList();
                currentShuffledIndex = -1;
            }
        }

        private void btnRepeat_Click(object sender, RoutedEventArgs e)
        {
            isRepeatEnabled = !isRepeatEnabled;
            btnRepeat.Background = isRepeatEnabled
                ? new SolidColorBrush(Colors.Red)
                : new SolidColorBrush(Colors.Gray);
        }

        // ------------------- Прочее -------------------

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = volumeSlider.Value / 100;
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UserId = 0;
            Properties.Settings.Default.Save();

            new LoginWindow().Show();
            Close();
        }
        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            // Переход на главную страницу (MainPage)
            MainFrame.Content = new MainPage(userId);
        }

        private void btnAddSong_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Audio Files|*.mp3;*.wav";

            if (openFileDialog.ShowDialog() == true)
            {
                // Создаем окно для ввода информации о песне
                var addSongWindow = new AddSongWindow();
                if (addSongWindow.ShowDialog() == true)
                {
                    string title = addSongWindow.SongTitle;
                    string artist = addSongWindow.Artist;
                    string genre = addSongWindow.Genre;
                    string imagePath = string.IsNullOrEmpty(addSongWindow.ImagePath) ? "E:\\Downloads\\IMG_0804.PNG" : addSongWindow.ImagePath;

                    // Сохранение песни в базу данных
                    DatabaseHelper.AddSong(userId, title, artist, genre, openFileDialog.FileName, imagePath);

                    // Обновляем список добавленных песен на MainPage
                    if (MainFrame.Content is MainPage mainPage)
                    {
                        mainPage.LoadAddedSongs();
                    }
                }
            }
        }

        private void btnDeleteSong_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content is MainPage mainPage)
            {
                mainPage.DeleteSelectedSong();
            }
            else if (MainFrame.Content is PlaylistPage playlistPage)
            {
                playlistPage.DeleteSelectedSongFromPlaylist();
            }
        }


    }
}
