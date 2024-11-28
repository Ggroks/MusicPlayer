using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using MusicPlayer.Models;



namespace MusicPlayer
{
    public static class DatabaseHelper
    {
        

        private static string dbPath = "UsersDatabase.db";
        public static string connectionString = $"Data Source={dbPath};Version=3;";

        // Создание базы данных и таблицы, если их нет
        public static void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Создаём таблицу Users, если её ещё нет
                var createUsersTable = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL
            );";
                var usersCmd = new SQLiteCommand(createUsersTable, connection);
                usersCmd.ExecuteNonQuery();

                // Создаём таблицу Songs, если её ещё нет
                var createSongsTable = @"   
            CREATE TABLE IF NOT EXISTS Songs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER,
                FilePath TEXT NOT NULL,
                Title TEXT NOT NULL,
                Artist TEXT,
                Genre TEXT,
                ImagePath TEXT,
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            );";
                var songsCmd = new SQLiteCommand(createSongsTable, connection);
                songsCmd.ExecuteNonQuery();

                string createPlaylistsTableQuery = @"
            CREATE TABLE IF NOT EXISTS Playlists (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                UserId INTEGER,
                ImagePath TEXT,
                FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
            );";

                var command = new SQLiteCommand(createPlaylistsTableQuery, connection);
                command.ExecuteNonQuery();

                string createPlaylistSongsTableQuery = @"
            CREATE TABLE IF NOT EXISTS PlaylistSongs (
                PlaylistId INTEGER NOT NULL,
                SongId INTEGER NOT NULL,
                PRIMARY KEY (PlaylistId, SongId),
                FOREIGN KEY (PlaylistId) REFERENCES Playlists(Id),
                FOREIGN KEY (SongId) REFERENCES Songs(Id)
            );";
                using (var cmd = new SQLiteCommand(createPlaylistSongsTableQuery, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var con = new SQLiteConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        CREATE TABLE IF NOT EXISTS RecentSongs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                SongId INTEGER NOT NULL,
                LastPlayed DATETIME NOT NULL,
                FOREIGN KEY (SongId) REFERENCES Songs(Id)
            );";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                string createSessionTableQuery = @"
            CREATE TABLE IF NOT EXISTS UserSession (
                UserId INTEGER
            );";
                using (var sessionCmd = new SQLiteCommand(createSessionTableQuery, connection))
                {
                    sessionCmd.ExecuteNonQuery();
                }
            }
        }

        public static bool UserExists(string username, string password)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(1) FROM Users WHERE Username = @Username AND Password = @Password";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", password);
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result) > 0;
                }
            }
        }


        public static bool RegisterUser(string username, string password)
        {
            if (UserExists(username, password)) return false;

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Users (Username, Password) VALUES (@Username, @Password)";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    command.ExecuteNonQuery();
                    return true;
                }
            }
        }

        public static bool LoginUser(string username, string password)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id FROM Users WHERE Username = @Username AND Password = @Password";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    var result = command.ExecuteScalar();

                    if (result != null)
                    {
                        int userId = Convert.ToInt32(result);

                        // Очистка предыдущей сессии и добавление текущей
                        string clearSessionQuery = "DELETE FROM UserSession";
                        new SQLiteCommand(clearSessionQuery, connection).ExecuteNonQuery();

                        string insertSessionQuery = "INSERT INTO UserSession (UserId) VALUES (@UserId)";
                        using (var insertCommand = new SQLiteCommand(insertSessionQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@UserId", userId);
                            insertCommand.ExecuteNonQuery();
                        }

                        return true;
                    }
                }
            }
            return false;
        }

        public static void AddSong(int userId, string title, string artist, string genre, string filePath, string imagePath)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"
            INSERT INTO Songs (UserId, Title, Artist, Genre, FilePath, ImagePath) 
            VALUES (@UserId, @Title, @Artist, @Genre, @FilePath, @ImagePath)";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Artist", artist);
                    cmd.Parameters.AddWithValue("@Genre", genre);
                    cmd.Parameters.AddWithValue("@FilePath", filePath);
                    cmd.Parameters.AddWithValue("@ImagePath", imagePath);
                    cmd.ExecuteNonQuery();
                }
            }

            // Обновление списка песен на главной странице
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow && mainWindow.Content is MainPage mainPage)
                {
                    mainPage.LoadAddedSongs(); // Метод для обновления всех песен
                }
            });
        }


        public static List<Song> GetSongs(int userId)
        {
            var songs = new List<Song>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Title, Artist, Genre, FilePath, ImagePath FROM Songs WHERE UserId = @UserId";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            songs.Add(new Song
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Artist = reader.GetString(2),
                                Genre = reader.GetString(3),
                                FilePath = reader.GetString(4),
                                ImagePath = reader.IsDBNull(5) ? null : reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return songs;
        }

        public static int? GetUserId(string username)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id FROM Users WHERE Username = @Username";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    var result = cmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out int userId))
                    {
                        return userId;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        public static List<string> GetSongsByUserId(int userId)
        {
            var songs = new List<string>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT FilePath FROM Songs WHERE UserId = @UserId";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            songs.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return songs;
        }
        public static void CreatePlaylist(string name, int userId, byte[] imageData)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO Playlists (Name, UserId, Image) VALUES (@Name, @UserId, @Image)";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Image", imageData ?? new byte[0]);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании плейлиста: {ex.Message}");
            }
        }

        public static List<Playlist> GetUserPlaylists(int userId)
        {
            List<Playlist> playlists = new List<Playlist>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name, ImagePath FROM Playlists WHERE UserId = @UserId";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var playlist = new Playlist
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                ImagePath = reader["ImagePath"].ToString()
                            };
                            playlists.Add(playlist);
                        }
                    }
                }
            }

            return playlists;
        }
        public static int GetCurrentUserId()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT UserId FROM UserSession LIMIT 1";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        public static Song GetSongInfo(string filePath)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT Id, Title, Artist, ImagePath, FilePath FROM Songs WHERE FilePath = @FilePath LIMIT 1;";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@FilePath", filePath);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Song
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Artist = reader.GetString(2),
                                ImagePath = reader.IsDBNull(3) ? "E:\\Downloads\\IMG_0804.PNG" : reader.GetString(3),
                                FilePath = reader.GetString(4)
                            };
                        }
                    }
                }
            }

            return null; // Если песня не найдена
        }

        public static Playlist GetPlaylistInfo(int playlistId)
        {
            // Извлекаем информацию о плейлисте из таблицы Playlists
            string query = "SELECT * FROM Playlists WHERE Id = @PlaylistId";
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlaylistId", playlistId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Playlist
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                ImagePath = reader["ImagePath"] as string,
                                UserId = reader.GetInt32(reader.GetOrdinal("UserId"))
                            };
                        }
                    }
                }
            }
            return null; // Если плейлист не найден
        }

        public static void AddPlaylist(int userId, string name, string imagePath)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Playlists (Name, ImagePath, UserId) VALUES (@Name, @ImagePath, @UserId)";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@ImagePath", imagePath);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<Song> GetSongsForPlaylist(int playlistId)
        {
            var songs = new List<Song>();
            string query = @"
        SELECT s.Id, s.Title, s.Artist, s.Genre, s.FilePath 
        FROM Songs s
        INNER JOIN PlaylistSongs ps ON s.Id = ps.SongId
        WHERE ps.PlaylistId = @PlaylistId";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlaylistId", playlistId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            songs.Add(new Song
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Title = reader.GetString(reader.GetOrdinal("Title")),
                                Artist = reader.GetString(reader.GetOrdinal("Artist")),
                                Genre = reader.GetString(reader.GetOrdinal("Genre")),
                                FilePath = reader["FilePath"] as string
                            });
                        }
                    }
                }
            }
            return songs;
        }


        public static List<RecentSong> GetRecentSongs(int userId)
        {
            var recentSongs = new List<RecentSong>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = @"
            SELECT rs.SongId, s.Title, s.Artist, s.ImagePath, s.FilePath
            FROM RecentSongs rs
            JOIN Songs s ON rs.SongId = s.Id
            WHERE rs.UserId = @UserId
            ORDER BY rs.LastPlayed DESC
            LIMIT 6;";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            recentSongs.Add(new RecentSong
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Artist = reader.GetString(2),
                                ImagePath = reader.IsDBNull(3) ? "E:\\Downloads\\IMG_0804.PNG" : reader.GetString(3),
                                FilePath = reader.GetString(4)
                            });
                        }
                    }
                }
            }

            return recentSongs;
        }

   

        public static void UpdateLastPlayed(int userId, int songId)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"
            INSERT OR REPLACE INTO RecentSongs (UserId, SongId, LastPlayed) 
            VALUES (@UserId, @SongId, @LastPlayed)";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@SongId", songId);
                    cmd.Parameters.AddWithValue("@LastPlayed", DateTime.Now); // Обновляем время
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void AddSongToPlaylist(int songId, int playlistId)
        {
            string query = @"
        INSERT INTO PlaylistSongs (PlaylistId, SongId) 
        SELECT @PlaylistId, @SongId
        WHERE NOT EXISTS (
            SELECT 1 FROM PlaylistSongs 
            WHERE PlaylistId = @PlaylistId AND SongId = @SongId)";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlaylistId", playlistId);
                    command.Parameters.AddWithValue("@SongId", songId);
                    command.ExecuteNonQuery();
                }
            }
        }


        public static void DeleteSong(int songId)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("DELETE FROM Songs WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", songId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void RemoveSongFromPlaylist(int songId, int playlistId)
        {
            string query = "DELETE FROM PlaylistSongs WHERE PlaylistId = @PlaylistId AND SongId = @SongId";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlaylistId", playlistId);
                    command.Parameters.AddWithValue("@SongId", songId);
                    command.ExecuteNonQuery();
                }
            }
        }


        public static bool DeletePlaylist(int playlistId)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Удаляем плейлист
                    string deletePlaylistQuery = "DELETE FROM Playlists WHERE Id = @Id";
                    using (var command = new SQLiteCommand(deletePlaylistQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Id", playlistId);
                        command.ExecuteNonQuery();
                    }

                    // Удаляем песни из плейлиста
                    string deleteSongsQuery = "DELETE FROM PlaylistSongs WHERE PlaylistId = @PlaylistId";
                    using (var command = new SQLiteCommand(deleteSongsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@PlaylistId", playlistId);
                        command.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении плейлиста: {ex.Message}");
                return false;
            }
        }


    }
}

