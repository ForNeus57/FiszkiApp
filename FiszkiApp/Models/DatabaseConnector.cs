using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace FiszkiApp.Models;

public sealed class DatabaseConnector
{
    private DatabaseConnector()
    {
        // _connection.Open();
        if (!File.Exists(@"./Database/database.db"))
        {
            createDatabase();
        }
        else
        {
            _connection = new SqliteConnection("Data Source=Database/database.db;");
        }
        _connection.Open();

    }

    private static DatabaseConnector _instance;

    private static SqliteConnection _connection;

    public static string? ActiveUser = null;

    public static DatabaseConnector GetInstance()
    {
        if (_instance == null) _instance = new DatabaseConnector();

        return _instance;
    }

    private static void executeCommand(string command)
    {
        SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = command;
        cmd.ExecuteNonQuery();
    }

    private static SqliteDataReader executeQuery(string query)
    {
        SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = query;
        SqliteDataReader reader = cmd.ExecuteReader();

        return reader;
    }

    private static void createDatabase()
    {
        _connection = new SqliteConnection("Data Source=Database/database.db;");

        executeCommand("DROP TABLE IF EXISTS \"users\";");
        executeCommand("DROP TABLE IF EXISTS \"questions\";");
        executeCommand("DROP TABLE IF EXISTS \"subjects\";");
        executeCommand("DROP TABLE IF EXISTS \"archive\";");
        executeCommand("DROP TABLE IF EXISTS \"stats\";");
        executeCommand("CREATE TABLE \"users\" (\"userid\" TEXT PRIMARY KEY, \"password\" TEXT NOT NULL);");
        executeCommand("CREATE TABLE \"subjects\" (\"subject\" TEXT PRIMARY KEY, \"imagedir\" TEXT NOT NULL);");
        executeCommand(
            "CREATE TABLE \"questions\" (\"qid\" INTEGER PRIMARY KEY, \"question\" TEXT NOT NULL, \"answer\" TEXT, \"image\" TEXT, \"subject\" TEXT NOT NULL , \"batch\" TEXT NOT NULL, FOREIGN KEY (subject) REFERENCES subjects(subject));");
        executeCommand(
            "CREATE TABLE \"archive\" (\"qid\" INTEGER, \"question\" TEXT, \"answer\" TEXT, \"image\" TEXT, \"subject\" TEXT, \"batch\" TEXT, FOREIGN KEY (qid) REFERENCES questions(qid), FOREIGN KEY (subject) REFERENCES subjects(subject));");
        executeCommand(
            "CREATE TABLE \"stats\" (\"userid\" INTEGER NOT NULL, \"subject\" TEXT NOT NULL, \"batch\" TEXT NOT NULL, \"time\" TEXT, \"acurracy\" REAL, \"date\" TEXT NOT NULL, FOREIGN KEY (userid) REFERENCES users(userid), FOREIGN KEY (subject) REFERENCES subjects(subject));");
        addUser("admin", "admin");
        executeCommand("insert into users values (\"asd\", \"asd\")");
    }

    public static bool addUser(string name, string pwd)
    {
        SqliteDataReader reader = executeQuery("select * from users where userid=\"" + name + "\";");

        //check if user exists, if yes, we don't create another
        if (reader.HasRows) return false;

        //create password hash
        Encoding enc = Encoding.UTF8;
        var hashBuilder = new StringBuilder();
        using var hash = MD5.Create();
        byte[] result = hash.ComputeHash(enc.GetBytes(pwd));
        foreach (var b in result)
            hashBuilder.Append(b.ToString("x2"));

        string pwdhash = hashBuilder.ToString();

        executeCommand("INSERT INTO users(userid, password) values (\"" + name + "\", \"" + pwdhash + "\");");
        return true;
    }

    public static bool AddSubject(string name, string imagedir)
    {
        SqliteDataReader reader = executeQuery("select * from subjects where subject=\"" + name + "\";");

        //check if subject exists, if yes, we don't create another
        if (reader.HasRows) return false;

        executeCommand("INSERT INTO subjects values(\"" + name + "\", \"" + imagedir + "\")");
        return true;
    }

    public static bool AddQuestionsFromFile(string path, string subject)
    {
        //wczytaj dane z pliku do listy - dopiero jak sprawdzimy, że wszystko w pliku zgadza się z narzuconym formatem, to wpisz do bazy danych
        // format: question;answer;image;batch
        // subject podany w argumencie, qid sami ustalamy

        List<List<string>> data = new List<List<string>>();
        foreach (var line in System.IO.File.ReadLines(path))
        {
            if (line.Count(t => t == ';') != 3) return false; //zła liczba ';'
            List<string> args = new List<string>(line.Split(';'));
            if (args[0].Equals("")) return false; //puste pytanie
            if (Regex.Matches(args[2], @"[\w]+\.png").Count == 0) return false; //zła nazwa pliku ze zdjęciem

            data.Add(args);
        }

        int newQid = GetNewQid();
        string query = "INSERT INTO questions values ";
        string values = "";
        bool addSemicolon = false;

        foreach (var row in data)
        {
            if (addSemicolon) query += ',';

            addSemicolon = true;
            values = "(" + newQid + ", \"" + row[0] + "\", \"" + row[1] + "\", \"" + row[2] + "\", \"" + subject +
                     "\", \"" + row[3] + "\"),";
            query += values;
            newQid++;
        }

        executeCommand(query);
        return true;
    }

    /*get id of a new question*/
    private static int GetNewQid()
    {
        SqliteDataReader reader = executeQuery("SELECT qid FROM questions ORDER BY qid desc limit 1;");
        if (!reader.HasRows) return 0;

        reader.Read();
        return Convert.ToInt32(reader[reader.GetName(0)]);
    }

    /*check if this user exists*/
    public static bool checkLogin(String name, String pwd)
    {
        Encoding enc = Encoding.UTF8;
        var hashBuilder = new StringBuilder();
        using var hash = MD5.Create();
        byte[] result = hash.ComputeHash(enc.GetBytes(pwd));
        foreach (var b in result)
            hashBuilder.Append(b.ToString("x2"));

        string pwdhash = hashBuilder.ToString();
        
        string command = "SELECT * FROM users where userid=\"" + name + "\" and password=\"" + pwdhash + "\";";
        SqliteDataReader r = executeQuery(command);
        
        return r.HasRows;
    }
}