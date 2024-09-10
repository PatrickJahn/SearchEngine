using Microsoft.Data.SqlClient;

namespace WordService
{
    public class Database
    {
        private static Database? _instance;
        private SqlConnection _connection;
        private const string ConnectionString = "Server=word-db;User Id=sa;Password=SuperSecret7!;Encrypt=false";
        
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
            // Connect to the target database
            _connection = new SqlConnection(ConnectionString);
            _connection.Open();
        }

    

        // Method to execute SQL commands
        private void Execute(string sql)
        {  
            using var trans = _connection.BeginTransaction();
            var cmd = _connection.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = sql;
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
            Execute("CREATE TABLE Documents(id INTEGER PRIMARY KEY, url VARCHAR(500))");

            // Recreate the Words table
            Execute("CREATE TABLE Words(id INTEGER PRIMARY KEY, name VARCHAR(500))");
            Console.WriteLine("HEP");
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
            var insertCmd = _connection.CreateCommand();
            insertCmd.CommandText = "INSERT INTO Documents(id, url) VALUES(@id,@url)";

            var pName = new SqlParameter("url", url);
            insertCmd.Parameters.Add(pName);

            var pCount = new SqlParameter("id", id);
            insertCmd.Parameters.Add(pCount);

            insertCmd.ExecuteNonQuery();
        }

        // Method to insert words
        internal void InsertAllWords(Dictionary<string, int> words)
        {
            using var transaction = _connection.BeginTransaction();
            var command = _connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO Words(id, name) VALUES(@id,@name)";

            var paramName = command.CreateParameter();
            paramName.ParameterName = "name";
            command.Parameters.Add(paramName);

            var paramId = command.CreateParameter();
            paramId.ParameterName = "id";
            command.Parameters.Add(paramId);

            // Insert all entries in words
            foreach (var p in words)
            {
                paramName.Value = p.Key;
                paramId.Value = p.Value;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        // Method to insert occurrences
        internal void InsertAllOccurrences(int docId, ISet<int> wordIds)
        {
            using var transaction = _connection.BeginTransaction();
            var command = _connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO Occurrences(wordId, docId) VALUES(@wordId,@docId)";

            var paramwordId = command.CreateParameter();
            paramwordId.ParameterName = "wordId";
               
            command.Parameters.Add(paramwordId);

            var paramDocId = command.CreateParameter();
            paramDocId.ParameterName = "docId";
            paramDocId.Value = docId;

            command.Parameters.Add(paramDocId);

            foreach (var p in wordIds)
            {
                paramwordId.Value = p;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        // Method to get documents containing specific words
        public Dictionary<int, int> GetDocuments(List<int> wordIds)
        {
            var res = new Dictionary<int, int>();

            var sql = @"SELECT docId, COUNT(wordId) AS count FROM Occurrences WHERE wordId IN " + AsString(wordIds) + " GROUP BY docId ORDER BY count DESC;";

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = sql;

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var docId = reader.GetInt32(0);
                    var count = reader.GetInt32(1);
                   
                    res.Add(docId, count);
                }
            }

            return res;
        }

        // Helper method to convert list of integers to SQL-friendly string format
        private string AsString(List<int> ids)
        {
            return $"({string.Join(',', ids.Select(i => i.ToString()))})";
        }

        // Method to get all words
        public Dictionary<string, int> GetAllWords()
        {
            Dictionary<string, int> res = new Dictionary<string, int>();
      
            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Words";

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var w = reader.GetString(1);
                    
                    res.Add(w, id);
                }
            }
            return res;
        }

        // Method to get document details
        public List<string> GetDocDetails(List<int> docIds)
        {
            List<string> res = new List<string>();

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Documents WHERE id IN " + AsString(docIds);

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var url = reader.GetString(1);

                    res.Add(url);
                }
            }
            return res;
        }
    }
}
