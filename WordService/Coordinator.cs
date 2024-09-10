using System.Data;
using Microsoft.Data.SqlClient;

namespace WordService;

public class Coordinator
{
    private IDictionary<string, IDbConnection> ConnectionCache = new Dictionary<string, IDbConnection>();
    private const string DOCUMENT_DB = "document-db";
    private const string OCCURRENCE_DB = "occurrence-db";
    private const string SHORT_WORD_DB = "short-word-db";
    private const string MEDIUM_WORD_DB = "medium-word-db";
    private const string LONG_WORD_DB = "long-word-db";

    public IDbConnection GetDocumentConnection()
    {
        return GetConnectionByServerName(DOCUMENT_DB);
    }

    public IDbConnection GetOccurrenceConnection()
    {
        return GetConnectionByServerName(OCCURRENCE_DB);
    }
    
    public IDbConnection GetWordConnection(string word)
    {
        switch (word.Length)
        {
            case var l when (l <= 10):
                return GetConnectionByServerName(SHORT_WORD_DB);
            case var l when (l > 10 && l <= 20):
                return GetConnectionByServerName(MEDIUM_WORD_DB);
            case var l when (l >= 21):
                return GetConnectionByServerName(LONG_WORD_DB);
            default:
                throw new InvalidDataException();
        }
    }

    public IEnumerable<IDbConnection> GetAllConnections()
    {
        yield return GetDocumentConnection();
        yield return GetOccurrenceConnection();
        foreach (var wordConnection in GetAllWordConnections())
        {
            yield return wordConnection;
        }
    }
    
    public IEnumerable<IDbConnection> GetAllWordConnections()
    {
        yield return GetConnectionByServerName(SHORT_WORD_DB);
        yield return GetConnectionByServerName(MEDIUM_WORD_DB);
        yield return GetConnectionByServerName(LONG_WORD_DB);
    }

    private IDbConnection GetConnectionByServerName(string serverName)
    {
        if (ConnectionCache.TryGetValue(serverName, out var connection))
        {
            return connection;
        }
        
        connection = new SqlConnection($"Server={serverName};User Id=sa;Password=SuperSecret7!;Encrypt=false;");
        connection.Open();
        ConnectionCache.Add(serverName, connection);
        return connection;
    }
}