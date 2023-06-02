namespace FiszkiApp.Models;

public class Contributor
{
    public string name;

    public Contributor(string name, int contrNumber)
    {
        this.name = name;
        this.contrNumber = contrNumber;
    }

    public int contrNumber;
    
}