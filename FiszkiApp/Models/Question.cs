namespace FiszkiApp.Models;

public class Question
{
    public string q { get; set; }
    public string ans { get; set; }
    public string image { get; set; }
    public string batch { get; set; }
    public string subject { get; set; }
    public string path { get; set; }
    // public bool known { get; set; }


    public Question(string q = null, string ans = null, string image = null, string batch = null, string subject = null,
        string path = null/*, bool known = false*/)
    {
        this.q = q;
        this.ans = ans;
        this.image = image;
        this.batch = batch;
        this.subject = subject;
        this.path = path;
        // this.known = known;
    }
    
    public Question()
    {
        this.q = null;
        this.ans = null;
        this.image = null;
        this.batch = null;
        this.subject = null;
        this.path = null;
        // this.known = known;
    }
}