using System.Windows;
using MusicPlayer.Properties; // Подключите пространство имен с настройками

namespace MusicPlayer
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (Settings.Default.UserId > 0) // Используйте Settings.Default
            {
                // Если пользователь выбрал "Не выходить", запускаем MainWindow
                MainWindow mainWindow = new MainWindow(Settings.Default.UserId);
                mainWindow.Show();
            }
            else
            {
                // Иначе открываем окно авторизации
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
            }
        }
    }
}
