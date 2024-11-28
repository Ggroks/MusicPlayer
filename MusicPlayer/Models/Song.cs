namespace MusicPlayer.Models
{
    public class Song
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Genre { get; set; }
        public string FilePath { get; set; }
        public int RowNumber { get; set; }
        public string ImagePath { get; set; }
    }
}
