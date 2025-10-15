using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;

namespace WpfApp1
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private void ButtonTestConnection_Click(object sender, RoutedEventArgs e)
        {
            string host = TextBoxHost.Text.Trim();
            string port = TextBoxPort.Text.Trim();
            string database = TextBoxDatabase.Text.Trim();
            string password = PasswordBoxPassword.Password;

            if (string.IsNullOrWhiteSpace(host))
            {
                MessageBox.Show("Укажите хост (например, localhost).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(port))
            {
                MessageBox.Show("Укажите порт (например, 3306).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(database))
            {
                MessageBox.Show("Имя базы данных не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Строка подключения
            var connection = new DataStorage
            {
                Host = TextBoxHost.Text.Trim(),
                Port = TextBoxPort.Text.Trim(),
                Database = TextBoxDatabase.Text.Trim(),
                Password = PasswordBoxPassword.Password,
                Username = "root"
            };

            string connStr = connection.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    if (conn.State == System.Data.ConnectionState.Open)
                    {
                        ConnectionStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                        TextBlockStatus.Text = "Подключено";
                        TextBlockStatus.Foreground = new SolidColorBrush(Colors.Green);

                        Page1 page1 = new Page1(connection);
                        page1.Show();
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ConnectionStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                TextBlockStatus.Text = "Ошибка";
                TextBlockStatus.Foreground = new SolidColorBrush(Colors.Red);
                MessageBox.Show($"{connStr}Ошибка подключения:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }   
    }
}