using System.Data;
using Logging;
using Microsoft.Data.SqlClient;

namespace WordService
{
    public class Database
    {
        private readonly Coordinator _coordinator = new();

        // Inject Logging through the constructor
        public Database()
        {
        }

        // Execute method with proper exception handling and transaction management
        private void Execute(IDbConnection connection, string sql)
        {
            using var activity = LoggingService._activitySource.StartActivity();
            try
            {
                using var trans = connection.BeginTransaction();
                var cmd = connection.CreateCommand();
                cmd.Transaction = trans;
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
                trans.Commit();
                LoggingService.Log.Information($"SQL executed successfully: {sql}");
            }
            catch (SqlException ex)
            {
                LoggingService.Log.Information($"SQL Execution failed: {sql}", ex);
                throw;
            }
           
        }

        // Method to delete the database (drop tables)
        public void DeleteDatabase()
        {
            using var trace =  LoggingService._activitySource.StartActivity("DeleteDatabase");
            foreach (var connection in _coordinator.GetAllConnections())
            {
                try
                {
                    LoggingService.Log.Information("Dropping tables in database.");
                    Execute(connection, "DROP TABLE IF EXISTS Occurrences");
                    Execute(connection, "DROP TABLE IF EXISTS Words");
                    Execute(connection, "DROP TABLE IF EXISTS Documents");
                    LoggingService.Log.Information("Tables dropped successfully.");
                }
                catch (Exception ex)
                {
                    LoggingService.Log.Error("Error while deleting database", ex);
                    throw;
                }
            }
        }

        // Method to recreate the database (recreate tables)
        public void RecreateDatabase()
        {
            using var trace = LoggingService._activitySource.StartActivity("RecreateDatabase");
            foreach (var connection in _coordinator.GetAllConnections())
            {
                try
                {
                     LoggingService.Log.Information("Recreating tables in database.");
                    Execute(connection, "DROP TABLE IF EXISTS Occurrences");
                    Execute(connection, "DROP TABLE IF EXISTS Words");
                    Execute(connection, "DROP TABLE IF EXISTS Documents");

                    Execute(connection, "CREATE TABLE Documents(id INTEGER PRIMARY KEY, url VARCHAR(500))");
                    Execute(connection, "CREATE TABLE Words(id INTEGER PRIMARY KEY, name VARCHAR(500))");
                    Execute(connection, "CREATE TABLE Occurrences(wordId INTEGER, docId INTEGER)");
                     LoggingService.Log.Information("Tables recreated successfully.");
                }
                catch (Exception ex)
                {
                     LoggingService.Log.Error("Error while recreating database", ex);
                    throw;
                }
            }
        }

        // Method to insert documents
        public void InsertDocument(int id, string url)
        {
            using var activity = LoggingService._activitySource.StartActivity("InsertDocument");
            try
            {
                 LoggingService.Log.Information($"Inserting document with ID: {id} and URL: {url}");
                var connection = _coordinator.GetDocumentConnection();
                var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Documents(id, url) VALUES(@id,@url)";

                var pUrl = new SqlParameter("url", url);
                var pId = new SqlParameter("id", id);
                insertCmd.Parameters.Add(pUrl);
                insertCmd.Parameters.Add(pId);

                insertCmd.ExecuteNonQuery();
                 LoggingService.Log.Information("Document inserted successfully.");
            }
            catch (SqlException ex)
            {
                 LoggingService.Log.Error($"Insert Document failed with ID: {id}", ex);
                throw;
            }
           
        }

        // Method to insert words
        internal void InsertAllWords(Dictionary<string, int> words)
        {
            using var trace = LoggingService._activitySource.StartActivity("InsertAllWords");
            foreach (var word in words)
            {
                var connection = _coordinator.GetWordConnection(word.Key);
                using var transaction = connection.BeginTransaction();
                try
                {
                     LoggingService.Log.Information($"Inserting word: {word.Key}");
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
                     LoggingService.Log.Information($"Word inserted: {word.Key}");
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                     LoggingService.Log.Error($"Insert All Words failed: {word.Key}", ex);
                    throw;
                }
            }
        }

        // Method to insert occurrences
        internal void InsertAllOccurrences(int docId, ISet<int> wordIds)
        {
            using var trace = LoggingService._activitySource.StartActivity("InsertAllOccurrences");
            var connection = _coordinator.GetOccurrenceConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                 LoggingService.Log.Information($"Inserting occurrences for document ID: {docId}");
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
                 LoggingService.Log.Information($"Occurrences inserted for document ID: {docId}");
            }
            catch (SqlException ex)
            {
                transaction.Rollback();
                 LoggingService.Log.Error($"Insert All Occurrences failed for document ID: {docId}", ex);
                throw;
            }
           
        }

        // Method to get documents containing specific words
        public Dictionary<int, int> GetDocuments(List<int> wordIds)
        {
            var res = new Dictionary<int, int>();
            using var activity = LoggingService._activitySource.StartActivity("GetDocuments");
            try
            {
                 LoggingService.Log.Information($"Retrieving documents for word IDs: {string.Join(',', wordIds)}");
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
                 LoggingService.Log.Information("Documents retrieved successfully.");
            }
            catch (SqlException ex)
            {
                 LoggingService.Log.Error("Get Documents failed", ex);
                throw;
            }
           
            return res;
        }

        // Method to get all words
        public Dictionary<string, int> GetAllWords()
        {
            var res = new Dictionary<string, int>();
            using var activity = LoggingService._activitySource.StartActivity("GetAllWords");
            try
            {
                foreach (var connection in _coordinator.GetAllWordConnections())
                {
                     LoggingService.Log.Information("Retrieving all words from database.");
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
                 LoggingService.Log.Information("Words retrieved successfully.");
            }
            catch (SqlException ex)
            {
                 LoggingService.Log.Error("Get All Words failed", ex);
                throw;
            }
           
            return res;
        }

        // Method to get document details
        public List<string> GetDocDetails(List<int> docIds)
        {
            var res = new List<string>();
            using var activity = LoggingService._activitySource.StartActivity("GetDocDetails");
            try
            {
                 LoggingService.Log.Information($"Retrieving document details for document IDs: {string.Join(',', docIds)}");
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
                 LoggingService.Log.Information("Document details retrieved successfully.");
            }
            catch (SqlException ex)
            {
                 LoggingService.Log.Error("Get Doc Details failed", ex);
                throw;
            }
           
            return res;
        }

        // Helper method to convert list of integers to SQL-friendly string format
        private string AsString(List<int> ids)
        {
            return $"({string.Join(',', ids.Select(i => i.ToString()))})";
        }
    }
}
