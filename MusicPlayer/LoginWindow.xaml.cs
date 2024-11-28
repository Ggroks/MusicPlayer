using System.Windows;

namespace MusicPlayer
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            DatabaseHelper.InitializeDatabase(); 
            if (Properties.Settings.Default.UserId > 0)
            {
                MainWindow mainWindow = new MainWindow(Properties.Settings.Default.UserId);
                mainWindow.Show();
                this.Close();
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            int? nullableUserId = DatabaseHelper.GetUserId(username);
            if (nullableUserId.HasValue)
            {
                int userId = nullableUserId.Value;

                // Сохраняем пользователя, если установлен флажок
                if (rememberMeCheckbox.IsChecked == true)
                {
                    Properties.Settings.Default.UserId = userId;
                    Properties.Settings.Default.Save();
                }

                MainWindow mainWindow = new MainWindow(userId);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Пользователь не найден.");
            }
        }



        private void BtnOpenRegister_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.ShowDialog();
        }
    }
}
