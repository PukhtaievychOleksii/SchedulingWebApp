namespace SchedulingWebApp.Models;

public enum Role
{
    Manager,
    Employee
}
public class Employee: DomainObject
{
    public Role Role { get; private set; }

    public Employee(string name, Role role, int id): base(name, id)
    {
        Role = role;
    }
}
