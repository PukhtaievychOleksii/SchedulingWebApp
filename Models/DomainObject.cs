namespace SchedulingWebApp.Models;

public class DomainObject
{
    public string Name { get; set; }
    public int Index { get; private set; }
    public DomainObject(string name, int index)
    {
        Index = index;
        Name = name;
    }
}
