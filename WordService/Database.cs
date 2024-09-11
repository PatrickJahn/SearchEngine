using System.Data;
using Microsoft.Data.SqlClient;

namespace WordService
{
    public class Database
    {
        private static Database? _instance;
        private readonly Coordinator _coordinator = new();

        // Singleton GetInstance method
        public static Database GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Database();
            }
            return _instance;
        }

        // Execute method with proper exception handling and transaction management
        private void Execute(IDbConnection connection, string sql)
        {
            try
            {
                using var trans = connection.BeginTransaction();
                var cmd = connection.CreateCommand();
                cmd.Transaction = trans;
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
                trans.Commit();
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Execution failed: {ex.Message}");
            }
        }

        // Method to delete the database (drop tables)
        public void DeleteDatabase()
        {
            foreach (var connection in _coordinator.GetAllConnections())
            {
              
                    Execute(connection, "DROP TABLE IF EXISTS Occurrences");
                    Execute(connection, "DROP TABLE IF EXISTS Words");
                    Execute(connection, "DROP TABLE IF EXISTS Documents");
                
            }
        }

        // Method to recreate the database (recreate tables)
        public void RecreateDatabase()
        {
            foreach (var connection in _coordinator.GetAllConnections())
            {
             
                Execute(connection, "DROP TABLE IF EXISTS Occurrences");
                Execute(connection, "DROP TABLE IF EXISTS Words");
                Execute(connection, "DROP TABLE IF EXISTS Documents");

                Execute(connection, "CREATE TABLE Documents(id INTEGER PRIMARY KEY, url VARCHAR(500))");
                Execute(connection, "CREATE TABLE Words(id INTEGER PRIMARY KEY, name VARCHAR(500))");
                Execute(connection, "CREATE TABLE Occurrences(wordId INTEGER, docId INTEGER)");
            
            }
        }

        // Method to insert documents
        public void InsertDocument(int id, string url)
        {
            try
            {
                var connection = _coordinator.GetDocumentConnection();
                var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Documents(id, url) VALUES(@id,@url)";

                var pUrl = new SqlParameter("url", url);
                var pId = new SqlParameter("id", id);
                insertCmd.Parameters.Add(pUrl);
                insertCmd.Parameters.Add(pId);

                insertCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Insert Document failed: {ex.Message}");
            }
        }

        // Method to insert words
        internal void InsertAllWords(Dictionary<string, int> words)
        {
            foreach (var word in words)
            {
                var connection = _coordinator.GetWordConnection(word.Key);
                using var transaction = connection.BeginTransaction();
                try
                {
                    var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = @"INSERT INTO Words(id, name) VALUES(@id,@name)";

                    var paramName = command.CreateParameter();
                    paramName.ParameterName = "name";
                    command.Parameters.Add(paramName);

                    var paramId = command.CreateParameter();
                    paramId.ParameterName = "id";
                    command.Parameters.Add(paramId);

                    paramName.Value = word.Key;
                    paramId.Value = word.Value;
                    command.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Insert All Words failed: {ex.Message}");
                }
            }
        }

        // Method to insert occurrences
        internal void InsertAllOccurrences(int docId, ISet<int> wordIds)
        {
            var connection = _coordinator.GetOccurrenceConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO Occurrences(wordId, docId) VALUES(@wordId,@docId)";

                var paramWordId = command.CreateParameter();
                paramWordId.ParameterName = "wordId";
                command.Parameters.Add(paramWordId);

                var paramDocId = command.CreateParameter();
                paramDocId.ParameterName = "docId";
                paramDocId.Value = docId;
                command.Parameters.Add(paramDocId);

                foreach (var p in wordIds)
                {
                    paramWordId.Value = p;
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (SqlException ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Insert All Occurrences failed: {ex.Message}");
            }
        }

        // Method to get documents containing specific words
        public Dictionary<int, int> GetDocuments(List<int> wordIds)
        {
            var res = new Dictionary<int, int>();
            try
            {
                var connection = _coordinator.GetOccurrenceConnection();
                var sql = @"SELECT docId, COUNT(wordId) AS count FROM Occurrences WHERE wordId IN " + AsString(wordIds) + " GROUP BY docId ORDER BY count DESC;";
                var selectCmd = connection.CreateCommand();
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
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Get Documents failed: {ex.Message}");
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
            var res = new Dictionary<string, int>();
            try
            {
                foreach (var connection in _coordinator.GetAllWordConnections())
                {
               
                        var selectCmd = connection.CreateCommand();
                        selectCmd.CommandText = "SELECT * FROM Words";

                        using (var reader = selectCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var id = reader.GetInt32(0);
                                var word = reader.GetString(1);
                                res.Add(word, id);
                            }
                        }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Get All Words failed: {ex.Message}");
            }
            return res;
        }

        // Method to get document details
        public List<string> GetDocDetails(List<int> docIds)
        {
            var res = new List<string>();
            try
            {
                var connection = _coordinator.GetDocumentConnection();
                var selectCmd = connection.CreateCommand();
                selectCmd.CommandText = "SELECT * FROM Documents WHERE id IN " + AsString(docIds);

                using (var reader = selectCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var url = reader.GetString(1);
                        res.Add(url);
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Get Doc Details failed: {ex.Message}");
            }
            return res;
        }
    }
}
