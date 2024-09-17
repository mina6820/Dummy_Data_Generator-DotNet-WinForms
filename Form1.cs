using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Dummy_Data_Generator
{
    public partial class Form1 : Form
    {
        private string connectionString = "Server=DESKTOP-1B5FFEI;Database=Task1;Trusted_Connection=True;";

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string tableName = textBox1.Text;
            int numberOfRows = (int)numericUpDown1.Value;

            // Collect column information
            var columns = new DataTable();
            columns.Columns.Add("ColumnName", typeof(string));
            columns.Columns.Add("DataType", typeof(string));

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                string columnName = row.Cells["ColumnName"].Value.ToString();
                string dataType = row.Cells["DataType"].Value.ToString();
                columns.Rows.Add(columnName, dataType);
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if the table exists
                    if (!TableExists(connection, tableName))
                    {
                        // Create the table
                        CreateTable(connection, tableName, columns);
                    }
                    else
                    {
                        // Check and add any new columns to the existing table
                        AddMissingColumns(connection, tableName, columns);
                    }

                    // Insert dummy data
                    InsertData(connection, tableName, columns, numberOfRows);
                }

                MessageBox.Show("Data has been successfully inserted.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void AddMissingColumns(SqlConnection connection, string tableName, DataTable columns)
        {
            var existingColumns = new List<string>();

            // Retrieve existing columns from the database
            using (SqlCommand command = new SqlCommand(
                $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName", connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        existingColumns.Add(reader["COLUMN_NAME"].ToString());
                    }
                }
            }

            // Alter table by adding new columns if they don't exist
            foreach (DataRow row in columns.Rows)
            {
                string columnName = row["ColumnName"].ToString();
                string dataType = row["DataType"].ToString();

                if (!existingColumns.Contains(columnName))
                {
                    string alterTableQuery = $"ALTER TABLE {tableName} ADD {columnName} {dataType}";

                    using (SqlCommand command = new SqlCommand(alterTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }


        private bool TableExists(SqlConnection connection, string tableName)
        {
            using (SqlCommand command = new SqlCommand(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName", connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                int tableCount = (int)command.ExecuteScalar();
                return tableCount > 0;
            }
        }

        private void CreateTable(SqlConnection connection, string tableName, DataTable columns)
        {
            string createTableQuery = $"CREATE TABLE {tableName} (";

            foreach (DataRow row in columns.Rows)
            {
                string columnName = row["ColumnName"].ToString();
                string dataType = row["DataType"].ToString();

                // Check if the data type is varchar and if a length is specified, otherwise default to varchar(50)
                if (dataType.StartsWith("varchar", StringComparison.OrdinalIgnoreCase))
                {
                    if (!dataType.Contains("("))
                    {
                        dataType = "varchar(50)";  // Default length for varchar
                    }
                }

                createTableQuery += $"{columnName} {dataType}, ";
            }

            createTableQuery = createTableQuery.TrimEnd(',', ' ') + ")";

            using (SqlCommand command = new SqlCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }


        private void InsertData(SqlConnection connection, string tableName, DataTable columns, int numberOfRows)
        {

            int batchSize = 1000;
            int totalRowsInserted = 0;

            while (totalRowsInserted < numberOfRows)
            {
                int rowsToInsert = Math.Min(batchSize, numberOfRows - totalRowsInserted);

                string insertQuery = $"INSERT INTO {tableName} ({string.Join(", ", columns.AsEnumerable().Select(r => r["ColumnName"].ToString()))}) VALUES ";

                for (int i = 0; i < rowsToInsert; i++)
                {
                    var values = columns.AsEnumerable().Select(r => GetDummyValue(r["DataType"].ToString())).ToArray();
                    insertQuery += $"({string.Join(", ", values)}), ";
                }

                // Remove the trailing comma and space
                insertQuery = insertQuery.TrimEnd(',', ' ');

                using (SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                totalRowsInserted += rowsToInsert;
            }
        }


        private string GetDummyValue(string dataType)
        {
            switch (dataType.ToLower())
            {
                case "int":
                    return "1";
                case "varchar":
                    return "\'SampleText\'";
                case "float":
                    return "1.0";
                case "datetime":
                    return "GETDATE()";
                case "bit":
                    return "1";
                case "decimal":
                    return "1.0";
                default:
                    throw new Exception("Unsupported data type");
            }
        }

    }
}