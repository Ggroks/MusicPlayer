namespace MusicPlayer.Models
{
    public class Playlist
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImagePath { get; set; } // путь к изображению плейлиста
        public int UserId { get; set; }
    }
}
