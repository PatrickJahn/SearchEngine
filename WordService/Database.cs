using Npgsql;

namespace WordService
{
    public class Database
    {
        private static Database _instance;
        private NpgsqlConnection _connection;
        private const string TargetDatabaseName = "searchdb";
        private const string ConnectionStringWithoutDb = "Host=localhost;Port=5433;Username=postgres;Password=password;";
        private const string ConnectionStringWithDb = "Host=localhost;Port=5433;Database=searchdb;Username=postgres;Password=password;";

        // Singleton GetInstance method
        public static Database GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Database();
            }
            return _instance;
        }

        // Private constructor to prevent external instantiation
        private Database()
        {
            EnsureDatabaseExists();  // Ensure the database exists before connecting to it

            // Connect to the target database
            _connection = new NpgsqlConnection(ConnectionStringWithDb);
            _connection.Open();
        }

        // Ensure that the target database exists, otherwise create it
        private void EnsureDatabaseExists()
        {
            try
            {
                // Try to connect to the target database
                using var testConnection = new NpgsqlConnection(ConnectionStringWithDb);
                testConnection.Open();
                testConnection.Close();
            }
            catch (NpgsqlException ex)
            {
                // If the database does not exist, catch the exception and create it
                if (ex.Message.Contains("does not exist"))
                {
                    CreateDatabase();
                }
                else
                {
                    throw;  // If it's another issue, rethrow the exception
                }
            }
        }

        // Method to create the database if it doesn't exist
        private void CreateDatabase()
        {
            // Connect to the postgres database (default database)
            using var connection = new NpgsqlConnection(ConnectionStringWithoutDb);
            connection.Open();

            // Create the target database
            using var cmd = new NpgsqlCommand($"CREATE DATABASE {TargetDatabaseName};", connection);
            cmd.ExecuteNonQuery();

            connection.Close();
        }

        // Method to execute SQL commands
        private void Execute(string sql)
        {
            using var trans = _connection.BeginTransaction();
            var cmd = new NpgsqlCommand(sql, _connection, trans);
            cmd.ExecuteNonQuery();
            trans.Commit();
        }

        // Method to delete the database (drop tables)
        public void DeleteDatabase()
        {
            Execute("DROP TABLE IF EXISTS Occurrences");
            Execute("DROP TABLE IF EXISTS Words");
            Execute("DROP TABLE IF EXISTS Documents");
        }

        // Method to recreate the database (recreate tables)
        public void RecreateDatabase()
        {
            // Drop the Occurrences table first because it depends on Words and Documents
            Execute("DROP TABLE IF EXISTS Occurrences");

            // Now drop the Words and Documents tables
            Execute("DROP TABLE IF EXISTS Words");
            Execute("DROP TABLE IF EXISTS Documents");

            // Recreate the Documents table
            Execute("CREATE TABLE Documents(id SERIAL PRIMARY KEY, url VARCHAR(500))");

            // Recreate the Words table
            Execute("CREATE TABLE Words(id SERIAL PRIMARY KEY, name VARCHAR(500))");

            // Recreate the Occurrences table
            Execute("CREATE TABLE Occurrences(wordId INTEGER, docId INTEGER, "
                    + "FOREIGN KEY (wordId) REFERENCES Words(id), "
                    + "FOREIGN KEY (docId) REFERENCES Documents(id))");

            // Optionally recreate indexes (uncomment if needed)
            // Execute("CREATE INDEX IF NOT EXISTS word_index ON Occurrences(wordId)");
        }
        
        // Method to insert documents
        public void InsertDocument(int id, string url)
        {
            var insertCmd = new NpgsqlCommand("INSERT INTO Documents(id, url) VALUES(@id, @url)", _connection);
            insertCmd.Parameters.AddWithValue("@id", id);
            insertCmd.Parameters.AddWithValue("@url", url);
            insertCmd.ExecuteNonQuery();
        }

        // Method to insert words
        internal void InsertAllWords(Dictionary<string, int> words)
        {
            using var transaction = _connection.BeginTransaction();
            var command = new NpgsqlCommand("INSERT INTO Words(id, name) VALUES(@id, @name)", _connection, transaction);

            command.Parameters.Add(new NpgsqlParameter("id", NpgsqlTypes.NpgsqlDbType.Integer));
            command.Parameters.Add(new NpgsqlParameter("name", NpgsqlTypes.NpgsqlDbType.Varchar));

            foreach (var word in words)
            {
                command.Parameters["id"].Value = word.Value;
                command.Parameters["name"].Value = word.Key;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        // Method to insert occurrences
        internal void InsertAllOccurrences(int docId, ISet<int> wordIds)
        {
            using var transaction = _connection.BeginTransaction();
            var command = new NpgsqlCommand("INSERT INTO Occurrences(wordId, docId) VALUES(@wordId, @docId)", _connection, transaction);

            command.Parameters.Add(new NpgsqlParameter("wordId", NpgsqlTypes.NpgsqlDbType.Integer));
            command.Parameters.Add(new NpgsqlParameter("docId", NpgsqlTypes.NpgsqlDbType.Integer));
            command.Parameters["docId"].Value = docId;

            foreach (var wordId in wordIds)
            {
                command.Parameters["wordId"].Value = wordId;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        // Method to get documents containing specific words
        public Dictionary<int, int> GetDocuments(List<int> wordIds)
        {
            var result = new Dictionary<int, int>();
            var sql = $"SELECT docId, COUNT(wordId) AS count FROM Occurrences WHERE wordId IN {AsString(wordIds)} GROUP BY docId ORDER BY count DESC;";

            using var selectCmd = new NpgsqlCommand(sql, _connection);
            using var reader = selectCmd.ExecuteReader();
            while (reader.Read())
            {
                var docId = reader.GetInt32(0);
                var count = reader.GetInt32(1);
                result.Add(docId, count);
            }

            return result;
        }

        // Helper method to convert list of integers to SQL-friendly string format
        private string AsString(List<int> ids)
        {
            return $"({string.Join(',', ids.Select(i => i.ToString()))})";
        }

        // Method to get all words
        public Dictionary<string, int> GetAllWords()
        {
            var result = new Dictionary<string, int>();
            var selectCmd = new NpgsqlCommand("SELECT * FROM Words", _connection);

            using var reader = selectCmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var word = reader.GetString(1);
                result.Add(word, id);
            }

            return result;
        }

        // Method to get document details
        public List<string> GetDocDetails(List<int> docIds)
        {
            var result = new List<string>();
            var selectCmd = new NpgsqlCommand("SELECT * FROM Documents WHERE id IN " + AsString(docIds), _connection);

            using var reader = selectCmd.ExecuteReader();
            while (reader.Read())
            {
                var url = reader.GetString(1);
                result.Add(url);
            }

            return result;
        }
    }
}
