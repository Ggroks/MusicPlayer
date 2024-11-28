using System.Windows;

namespace MusicPlayer
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtNewUsername.Text;
            string password = TxtNewPassword.Password;

            if (DatabaseHelper.RegisterUser(username, password))
            {
                MessageBox.Show("Registration successful!");
                Close();
            }
            else
            {
                MessageBox.Show("Username already taken.");
            }
        }
    }
}
