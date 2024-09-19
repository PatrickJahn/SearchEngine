using System.Data;
using Microsoft.Data.SqlClient;
using WordService.Services;

namespace WordService
{
    public class Database
    {
        private readonly Coordinator _coordinator = new();
        private readonly LoggingService _loggingService;

        // Inject LoggingService through the constructor
        public Database(LoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        // Execute method with proper exception handling and transaction management
        private void Execute(IDbConnection connection, string sql)
        {
            var activity = _loggingService.StartTrace($"Executing SQL: {sql}");
            try
            {
                using var trans = connection.BeginTransaction();
                var cmd = connection.CreateCommand();
                cmd.Transaction = trans;
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
                trans.Commit();
                _loggingService.LogInformation($"SQL executed successfully: {sql}");
            }
            catch (SqlException ex)
            {
                _loggingService.LogError($"SQL Execution failed: {sql}", ex);
                throw;
            }
            finally
            {
                _loggingService.EndTrace(activity);
            }
        }

        // Method to delete the database (drop tables)
        public void DeleteDatabase()
        {
            using var trace = _loggingService.StartTrace("DeleteDatabase");
            foreach (var connection in _coordinator.GetAllConnections())
            {
                try
                {
                    _loggingService.LogInformation("Dropping tables in database.");
                    Execute(connection, "DROP TABLE IF EXISTS Occurrences");
                    Execute(connection, "DROP TABLE IF EXISTS Words");
                    Execute(connection, "DROP TABLE IF EXISTS Documents");
                    _loggingService.LogInformation("Tables dropped successfully.");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("Error while deleting database", ex);
                    throw;
                }
            }
            _loggingService.EndTrace(trace);
        }

        // Method to recreate the database (recreate tables)
        public void RecreateDatabase()
        {
            using var trace = _loggingService.StartTrace("RecreateDatabase");
            foreach (var connection in _coordinator.GetAllConnections())
            {
                try
                {
                    _loggingService.LogInformation("Recreating tables in database.");
                    Execute(connection, "DROP TABLE IF EXISTS Occurrences");
                    Execute(connection, "DROP TABLE IF EXISTS Words");
                    Execute(connection, "DROP TABLE IF EXISTS Documents");

                    Execute(connection, "CREATE TABLE Documents(id INTEGER PRIMARY KEY, url VARCHAR(500))");
                    Execute(connection, "CREATE TABLE Words(id INTEGER PRIMARY KEY, name VARCHAR(500))");
                    Execute(connection, "CREATE TABLE Occurrences(wordId INTEGER, docId INTEGER)");
                    _loggingService.LogInformation("Tables recreated successfully.");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("Error while recreating database", ex);
                    throw;
                }
            }
            _loggingService.EndTrace(trace);
        }

        // Method to insert documents
        public void InsertDocument(int id, string url)
        {
            var activity = _loggingService.StartTrace("InsertDocument");
            try
            {
                _loggingService.LogInformation($"Inserting document with ID: {id} and URL: {url}");
                var connection = _coordinator.GetDocumentConnection();
                var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Documents(id, url) VALUES(@id,@url)";

                var pUrl = new SqlParameter("url", url);
                var pId = new SqlParameter("id", id);
                insertCmd.Parameters.Add(pUrl);
                insertCmd.Parameters.Add(pId);

                insertCmd.ExecuteNonQuery();
                _loggingService.LogInformation("Document inserted successfully.");
            }
            catch (SqlException ex)
            {
                _loggingService.LogError($"Insert Document failed with ID: {id}", ex);
                throw;
            }
            finally
            {
                _loggingService.EndTrace(activity);
            }
        }

        // Method to insert words
        internal void InsertAllWords(Dictionary<string, int> words)
        {
            using var trace = _loggingService.StartTrace("InsertAllWords");
            foreach (var word in words)
            {
                var activity = _loggingService.StartTrace($"InsertWord: {word.Key}");
                var connection = _coordinator.GetWordConnection(word.Key);
                using var transaction = connection.BeginTransaction();
                try
                {
                    _loggingService.LogInformation($"Inserting word: {word.Key}");
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
                    _loggingService.LogInformation($"Word inserted: {word.Key}");
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    _loggingService.LogError($"Insert All Words failed: {word.Key}", ex);
                    throw;
                }
                finally
                {
                    _loggingService.EndTrace(activity);
                }
            }
            _loggingService.EndTrace(trace);
        }

        // Method to insert occurrences
        internal void InsertAllOccurrences(int docId, ISet<int> wordIds)
        {
            using var trace = _loggingService.StartTrace("InsertAllOccurrences");
            var connection = _coordinator.GetOccurrenceConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                _loggingService.LogInformation($"Inserting occurrences for document ID: {docId}");
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
                _loggingService.LogInformation($"Occurrences inserted for document ID: {docId}");
            }
            catch (SqlException ex)
            {
                transaction.Rollback();
                _loggingService.LogError($"Insert All Occurrences failed for document ID: {docId}", ex);
                throw;
            }
            finally
            {
                _loggingService.EndTrace(trace);
            }
        }

        // Method to get documents containing specific words
        public Dictionary<int, int> GetDocuments(List<int> wordIds)
        {
            var res = new Dictionary<int, int>();
            var activity = _loggingService.StartTrace("GetDocuments");
            try
            {
                _loggingService.LogInformation($"Retrieving documents for word IDs: {string.Join(',', wordIds)}");
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
                _loggingService.LogInformation("Documents retrieved successfully.");
            }
            catch (SqlException ex)
            {
                _loggingService.LogError("Get Documents failed", ex);
                throw;
            }
            finally
            {
                _loggingService.EndTrace(activity);
            }
            return res;
        }

        // Method to get all words
        public Dictionary<string, int> GetAllWords()
        {
            var res = new Dictionary<string, int>();
            var activity = _loggingService.StartTrace("GetAllWords");
            try
            {
                foreach (var connection in _coordinator.GetAllWordConnections())
                {
                    _loggingService.LogInformation("Retrieving all words from database.");
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
                _loggingService.LogInformation("Words retrieved successfully.");
            }
            catch (SqlException ex)
            {
                _loggingService.LogError("Get All Words failed", ex);
                throw;
            }
            finally
            {
                _loggingService.EndTrace(activity);
            }
            return res;
        }

        // Method to get document details
        public List<string> GetDocDetails(List<int> docIds)
        {
            var res = new List<string>();
            var activity = _loggingService.StartTrace("GetDocDetails");
            try
            {
                _loggingService.LogInformation($"Retrieving document details for document IDs: {string.Join(',', docIds)}");
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
                _loggingService.LogInformation("Document details retrieved successfully.");
            }
            catch (SqlException ex)
            {
                _loggingService.LogError("Get Doc Details failed", ex);
                throw;
            }
            finally
            {
                _loggingService.EndTrace(activity);
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
