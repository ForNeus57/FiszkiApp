﻿using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace FiszkiApp.Models;

// * INFO: Aby zresetować bazę danych usuń plik ./Database/database.db
// * INFO: Dane do szybkiego logowania: Login: "asd", hasło: "asd"   

public sealed class DatabaseConnector
{
    private DatabaseConnector()
    {
        // _connection.Open();
        if (!File.Exists(@"./Database/database.db"))
        {
            CreateDatabase();
        }
        else
        {
            _connection = new SqliteConnection("Data Source=Database/database.db;");
            _connection.Open();
        }

    }

    private static DatabaseConnector _instance;

    private static SqliteConnection _connection;

    private static int questionNumber = -1;
    
    public static string? ActiveUser = null;

    public static List<Question> Ques = new List<Question>();

    public static Question currentQuestion;

    public static bool showAnswer = false;

	public static string? Status = null;

	public static List<string>? batches;
	public static List<string>? subjects;

	public static int rand = -1;

    public static DatabaseConnector GetInstance()
    {
        if (_instance == null) _instance = new DatabaseConnector();

        return _instance;
    }

    private static void ExecuteCommand(string command)
    {
        SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = command;
        cmd.ExecuteNonQuery();
    }

    private static SqliteDataReader ExecuteQuery(string query)
    {
        SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = query;
        SqliteDataReader reader = cmd.ExecuteReader();

        return reader;
    }

    private static void CreateDatabase()
    {
        _connection = new SqliteConnection("Data Source=Database/database.db;");
        _connection.Open();
        
        ExecuteCommand("DROP TABLE IF EXISTS \"users\";");
        ExecuteCommand("DROP TABLE IF EXISTS \"questions\";");
        ExecuteCommand("DROP TABLE IF EXISTS \"subjects\";");
        ExecuteCommand("DROP TABLE IF EXISTS \"archive\";");
        ExecuteCommand("DROP TABLE IF EXISTS \"stats\";");
        ExecuteCommand("CREATE TABLE \"users\" (\"userid\" TEXT PRIMARY KEY, \"password\" TEXT NOT NULL);");
        ExecuteCommand("CREATE TABLE \"subjects\" (\"subject\" TEXT PRIMARY KEY, \"imagedir\" TEXT NOT NULL);");
        ExecuteCommand(
            "CREATE TABLE \"batches\" (\"batch\" TEXT PRIMARY KEY , \"path\" TEXT, \"userid\" TEXT not null, FOREIGN KEY (userid) REFERENCES users(userid));");
        ExecuteCommand(
            "CREATE TABLE \"questions\" (\"qid\" INTEGER PRIMARY KEY, \"question\" TEXT NOT NULL, \"answer\" TEXT, \"image\" TEXT, \"subject\" TEXT NOT NULL , \"batch\" TEXT NOT NULL, FOREIGN KEY (subject) REFERENCES subjects(subject), FOREIGN KEY (batch) REFERENCES batches(batch));");
        ExecuteCommand(
            "CREATE TABLE \"stats\" (\"userid\" INTEGER NOT NULL, \"subject\" TEXT NOT NULL, \"batch\" TEXT NOT NULL, \"time\" TEXT, \"acurracy\" REAL, \"date\" TEXT NOT NULL, FOREIGN KEY (userid) REFERENCES users(userid), FOREIGN KEY (subject) REFERENCES subjects(subject), FOREIGN KEY (batch) REFERENCES batches(batch));");
        AddUser("asd", "asd");
        AddUser("admin", "admin");
    }

    public static bool AddUser(string name, string pwd)
    {
        SqliteDataReader reader = ExecuteQuery("select * from users where userid=\"" + name + "\";");

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

        ExecuteCommand("INSERT INTO users(userid, password) values (\"" + name + "\", \"" + pwdhash + "\");");
        return true;
    }

    public static bool AddSubject(string name, string imagedir)
    {
        SqliteDataReader reader = ExecuteQuery("select * from subjects where subject=\"" + name + "\";");

        //check if subject exists, if yes, we don't create another
        if (reader.HasRows) return false;

        ExecuteCommand("INSERT INTO subjects values(\"" + name + "\", \"" + imagedir + "\")");
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
            if (line.Count(t => t == ';') != 3)
            {
                Console.WriteLine("zła liczba ';'");
                return false;
                
            } 
            List<string> args = new List<string>(line.Split(';'));
            if (args[0].Equals(""))
            {
                Console.WriteLine("puste pytanie");
                return false;
            } //puste pytanie
            // if (Regex.Matches(args[2], @"[\w]+\.png").Count == 0) return false; //zła nazwa pliku ze zdjęciem

            data.Add(args);
        }

        int newQid= GetNewQid();;
        string query;
        string values = "";

        foreach (var row in data)
        {
            query = "INSERT INTO questions values "+"(" + newQid + ", \"" + row[0] + "\", \"" + row[1] + "\", \"" + row[2] + "\", \"" + subject +
                     "\", \"" + row[3] + "\");";
            try
            {
                ExecuteQuery(query);

            }
            catch (Exception e)
            {
                Console.WriteLine(query);
            }
            newQid++;
        }

        return true;
    }

    public static void AddSingleQuestion(Question question)
    {
        int newQid = GetNewQid();
        string query =
            $"INSERT INTO questions values ({newQid}, \"{question.q}\", \"{question.ans}\", \"{question.image}\", \"{question.subject}\", \"{question.batch}\")";   
        ExecuteCommand(query);
    }

    /*get id of a new question*/
    private static int GetNewQid()
    {
        SqliteDataReader reader = ExecuteQuery("SELECT qid FROM questions ORDER BY qid desc limit 1;");
        if (!reader.HasRows) return 0;

        reader.Read();
        return Convert.ToInt32(reader[reader.GetName(0)]);
    }

    /*check if this user exists*/
    public static bool CheckLogin(String name, String pwd)
    {
        Encoding enc = Encoding.UTF8;
        var hashBuilder = new StringBuilder();
        using var hash = MD5.Create();
        byte[] result = hash.ComputeHash(enc.GetBytes(pwd));
        foreach (var b in result)
            hashBuilder.Append(b.ToString("x2"));

        string pwdhash = hashBuilder.ToString();
        
        string command = "SELECT * FROM users where userid=\"" + name + "\" and password=\"" + pwdhash + "\";";
        SqliteDataReader r = ExecuteQuery(command);
    
        if (r.HasRows) Console.WriteLine("User "+name+" exists");
        return r.HasRows;
    }

    public static bool StartAddQuestions(string subject, string batch)
    {
        string command = "Select question, answer, image, batch from questions where subject=\"" + subject + "\" and batch=\"" + batch + "\"";
        SqliteDataReader reader = ExecuteQuery(command);

        while (reader.Read())
        {
            Question question = new Question(Convert.ToString(reader[reader.GetName(0)]), Convert.ToString(reader[reader.GetName(1)]), Convert.ToString(reader[reader.GetName(2)]), Convert.ToString(reader[reader.GetName(3)]), subject);
            Ques.Add(question);
        }
        
        return reader.HasRows;
    }

    public static void ShuffleQuestions()
    {
        Random rng = new Random();
        Ques = Ques.OrderBy(a => rng.Next()).ToList();
        questionNumber = 0;
    }

    public static void NextQuestion()
    {
        questionNumber++;
        
        if (questionNumber == Ques.Count)
        {
            ShuffleQuestions();
            questionNumber = 0;
        }
        currentQuestion = Ques[questionNumber];
                
    }

    public static void RemoveKnownQuestion()
    {
        Ques.RemoveAt(questionNumber);
        if (questionNumber >= Ques.Count) questionNumber = 0;
    }

	public static void getBatches() {
		SqliteDataReader reader = ExecuteQuery("SELECT batch FROM questions GROUP BY batch;");
		DatabaseConnector.batches = new List<string>();
		while (reader.Read())
        {
			DatabaseConnector.batches.Add(Convert.ToString(reader[reader.GetName(0)]));
		}
	}

	public static void getSubjects() {
		SqliteDataReader reader = ExecuteQuery("SELECT subject FROM questions GROUP BY subject;");
		DatabaseConnector.subjects = new List<string>();
		while (reader.Read())
        {
			DatabaseConnector.subjects.Add(Convert.ToString(reader[reader.GetName(0)]));
		}
	}

	public static void addQuestionsMemory(String query) {
		SqliteDataReader reader = ExecuteQuery(query);
		DatabaseConnector.Ques = new List<Question>();
		while(reader.Read()) {
			DatabaseConnector.Ques.Add(
				new Question(
					Convert.ToString(reader[reader.GetName(0)]),
					Convert.ToString(reader[reader.GetName(1)]),
					Convert.ToString(reader[reader.GetName(2)]),
					Convert.ToString(reader[reader.GetName(3)]),
					Convert.ToString(reader[reader.GetName(4)])
				)
			);
		}
	}

    public static void AddStat()
    {
        // "userid", "subject", "batch", "time", "acurracy", "date"
        TimeSpan ts = Statistics.stopwatch.Elapsed;
        string elapsedTime = $"{ts.Hours}h{ts.Minutes}m{ts.Seconds}s";
        string command = "insert into stats(userid, subject, batch, time, acurracy, date) values (\""+ ActiveUser+"\", \""+currentQuestion.batch+"\", \""+currentQuestion.subject+"\", \""+elapsedTime+"\", "+
                         (Statistics.batchSize / Statistics.skips).ToString().Replace(',', '.')+", \""+DateTime.Now.ToString("dd/MM/yyyy")+"\");";
        Console.WriteLine(command);
        ExecuteCommand(command);

        Console.WriteLine("added stats");
    }

    public static List<Statistics> getStats()
    {
        List<Statistics> stats = new List<Statistics>();
        SqliteDataReader reader = ExecuteQuery("select * from stats order by acurracy desc ;");
        while (reader.Read())
        {
            stats.Add(new Statistics(Convert.ToString(reader[reader.GetName(0)]), Convert.ToString(reader[reader.GetName(1)]), Convert.ToString(reader[reader.GetName(2)]), Convert.ToString(reader[reader.GetName(3)]), Convert.ToString(reader[reader.GetName(4)]), Convert.ToString(reader[reader.GetName(5)])));
            
        }
        
        return stats;
    }

    public static List<Contributor> getContributions()
    {
        List<Contributor> cont = new List<Contributor>();
        SqliteDataReader reader = ExecuteQuery("select userid, count(*) from batch group by userid limit 1; ");
        while (reader.Read())
        {
            cont.Add(new Contributor(Convert.ToString(reader[reader.GetName(0)]), Convert.ToInt16(reader[reader.GetName(1)])));
            
        }
        reader = ExecuteQuery("select userid, count(*) from subjects group by userid limit 1; ");
        while (reader.Read())
        {
            cont.Add(new Contributor(Convert.ToString(reader[reader.GetName(0)]), Convert.ToInt16(reader[reader.GetName(1)])));
            
        }

        return cont;
    }

}