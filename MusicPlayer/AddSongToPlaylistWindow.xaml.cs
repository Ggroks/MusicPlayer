using System;
using System.Collections.Generic;
using System.Windows;
using MusicPlayer.Models;

namespace MusicPlayer
{
    public partial class AddSongToPlaylistWindow : Window
    {
        public int SelectedSongId { get; private set; }
        private int userId;

        // Событие для уведомления об обновлении
        public event Action SongAdded;

        public AddSongToPlaylistWindow(int userId)
        {
            InitializeComponent();
            this.userId = userId;
            LoadSongs();
        }

        private void LoadSongs()
        {
            List<Song> songs = DatabaseHelper.GetSongs(userId);

            if (songs != null && songs.Count > 0)
            {
                songListBox.ItemsSource = songs;
            }
            else
            {
                MessageBox.Show("У вас нет добавленных песен. Сначала добавьте песни.");
            }
        }

        private void btnSelectSong_Click(object sender, RoutedEventArgs e)
        {
            if (songListBox.SelectedItem is Song selectedSong)
            {
                SelectedSongId = selectedSong.Id;
                DialogResult = true;
                SongAdded?.Invoke(); // Уведомляем об обновлении
                Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите песню.");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
