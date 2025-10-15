using Google.Protobuf.WellKnownTypes;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ZstdSharp.Unsafe;

namespace WpfApp1
{
    public class TableColumn
    {
        public string Name { get; set; }
        public string Type { get; set; } 
    }

    public partial class Page1
    {
        private MySqlConnection connection;
        private List<TableColumn> fields = new List<TableColumn>();

        private readonly DataStorage _DataStorage;
        public Page1(DataStorage dataStorage)
        {
            InitializeComponent();
            ButtonDeleteTable.IsEnabled = false;
            ButtonUpdateTable.IsEnabled = false;
            _DataStorage = dataStorage;
            InitializeDatabaseConnection();
        }

        private void InitializeDatabaseConnection()
        {
            string connStr = _DataStorage.GetConnectionString();
            connection = new MySqlConnection(connStr);
            connection.Open();
            LoadTables();
            connection.Close();
        }

        private void LoadTables()
        {
            ListBoxTables.Items.Clear();
            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                    connection.Open();

                string query = "SHOW TABLES;";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ListBoxTables.Items.Add(reader.GetValue(0).ToString());
                }

                reader.Close();
                connection.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки таблиц:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        private void ButtonRefreshTables_Click(object sender, RoutedEventArgs e)
        {
            fields.Clear();
            ListViewFields.ItemsSource = null;
            TextBoxTableName.Clear();
            TextBoxPrimaryKey.Clear();
            ButtonUpdateTable.IsEnabled = false;
            ButtonDeleteTable.IsEnabled = false;
            LoadTables();
        }

        private void ButtonAddField_Click(object sender, RoutedEventArgs e)
        {
            string name = TextBoxFieldName.Text.Trim();
            var selectedItem = ComboBoxFieldType.SelectedItem as ComboBoxItem;

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите имя поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedItem == null)
            {
                MessageBox.Show("Выберите тип поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string type = (selectedItem.Tag as string);

            
            if (fields.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"Поле с именем '{name}' уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            fields.Add(new TableColumn { Name = name, Type = type });
            ListViewFields.ItemsSource = null;
            ListViewFields.ItemsSource = fields;

            TextBoxFieldName.Clear();
        }

        private void ButtonCreateTable_Click(object sender, RoutedEventArgs e)
        {
            string tableName = TextBoxTableName.Text.Trim();
            string primaryKey = TextBoxPrimaryKey.Text.Trim();

            if (string.IsNullOrEmpty(tableName))
            {
                MessageBox.Show("Введите имя таблицы.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(primaryKey))
            {
                MessageBox.Show("Укажите поле первичного ключа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!fields.Any(f => f.Name.Equals(primaryKey, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"Поле '{primaryKey}' должно быть добавлено в список полей.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                connection.Open();
                string createQuery = BuildCreateTableQuery(tableName, primaryKey);
                MySqlCommand cmd = new MySqlCommand(createQuery, connection);
                cmd.ExecuteNonQuery();
                MessageBox.Show($"Таблица '{tableName}' успешно создана.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                connection.Close();

                ButtonClearForm_Click(sender, e);
                ButtonRefreshTables_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка создания таблицы:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        private string BuildCreateTableQuery(string tableName, string primaryKey)
        {
            var lines = new List<string>();

            foreach (var field in fields)
            {
                string line = $"`{field.Name}` ";
                switch (field.Type)
                {
                    case "INT":
                        line += "INT";
                        break;
                    case "DOUBLE":
                        line += "DOUBLE";
                        break;
                    case "TEXT":
                        line += "TEXT";
                        break;
                    case "DATETIME":
                        line += "DATETIME";
                        break;
                }

                if (field.Name.Equals(primaryKey, StringComparison.OrdinalIgnoreCase))
                    line += " PRIMARY KEY";

                lines.Add(line);
            }

            string columns = string.Join(",\n", lines);
            return $"CREATE TABLE `{tableName}` (\n{columns}\n);";
        }
        private void ButtoDeleteField_Click(object sender, RoutedEventArgs e)
        {
            string fieldName = TextBoxFieldName.Text.Trim();
            if (string.IsNullOrEmpty(fieldName)) return;
            string tableName = ListBoxTables.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(tableName)) return;
            string query = $"ALTER TABLE `{tableName}` DROP COLUMN `{fieldName}`";

            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.ExecuteNonQuery();
                    MessageBox.Show($"Столбец '{fieldName}' успешно удалён из таблицы '{tableName}'.",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    connection.Close();
                }
            }
            catch (Exception ex) {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                connection.Close();
            }

        }

        private void ButtonDeleteTable_Click(object sender, RoutedEventArgs e)
        {
            string tableName = ListBoxTables.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(tableName))
                return;

            var result = MessageBox.Show($"Удалить таблицу '{tableName}'?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No) return;

            try
            {
                connection.Open();
                string query = $"DROP TABLE `{tableName}`;";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();
                MessageBox.Show($"Таблица '{tableName}' удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                connection.Close();

                ButtonClearForm_Click(sender, e);
                ButtonRefreshTables_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления таблицы:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        private void ListBoxTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string tableName = ListBoxTables.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(tableName))
            {
                ButtonDeleteTable.IsEnabled = false;
                ButtonUpdateTable.IsEnabled = false;
                return;
            }

            ButtonDeleteTable.IsEnabled = true;
            ButtonUpdateTable.IsEnabled = true;

            
            fields.Clear();
            TextBoxTableName.Text = tableName;
            TextBoxPrimaryKey.Clear();
            ListViewFields.ItemsSource = null;

            try
            {
                connection.Open();
                string query = $"DESCRIBE `{tableName}`;";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string name = reader["Field"].ToString();
                    string type = reader["Type"].ToString().ToLower();

                    string mappedType = "TEXT"; // по умолчанию
                    if (type.Contains("int"))
                        mappedType = "INT";
                    else if (type.Contains("double") || type.Contains("float") || type.Contains("decimal"))
                        mappedType = "DOUBLE";
                    else if (type.Contains("datetime") || type.Contains("timestamp"))
                        mappedType = "DATETIME";

                    fields.Add(new TableColumn { Name = name, Type = mappedType });

                    if (reader["Key"].ToString() == "PRI")
                        TextBoxPrimaryKey.Text = name;
                }

                reader.Close();
                connection.Close();

                ListViewFields.ItemsSource = fields;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка чтения структуры таблицы:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        private void ButtonClearForm_Click(object sender, RoutedEventArgs e)
        {
            TextBoxTableName.Clear();
            TextBoxPrimaryKey.Clear();
            fields.Clear();
            ListViewFields.ItemsSource = null;
            ButtonUpdateTable.IsEnabled = false;
        }

        
        private void ButtonUpdateTable_Click(object sender, RoutedEventArgs e)
        {
            // Пока реализуется как удалить и создать заново
            string oldName = ListBoxTables.SelectedItem?.ToString();
            string newName = TextBoxTableName.Text.Trim();

            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("Имя таблицы не указано.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Изменение таблицы реализовано как удаление и пересоздание.\nПродолжить?", "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            try
            {
                connection.Open();

                // Удаляем старую
                MySqlCommand dropCmd = new MySqlCommand($"DROP TABLE `{oldName}`;", connection);
                dropCmd.ExecuteNonQuery();

                // Создаём новую
                string primaryKey = TextBoxPrimaryKey.Text.Trim();
                string createQuery = BuildCreateTableQuery(newName, primaryKey);
                MySqlCommand createCmd = new MySqlCommand(createQuery, connection);
                createCmd.ExecuteNonQuery();

                MessageBox.Show($"Таблица обновлена: {oldName} → {newName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                connection.Close();

                ButtonClearForm_Click(sender, e);
                ButtonRefreshTables_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления таблицы:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        
    }
}
