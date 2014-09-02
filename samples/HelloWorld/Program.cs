using System;
using System.Reflection;

public class Program
{
    public int Main(string[] args)
    {
        Baz("Hello World!");
        Bar("Hello World!");
        Bar(null);
        Console.ReadKey();
        return 0;
    }


    public static void Bar([NotNull] string x)
    {
        System.Console.WriteLine(x.Length);
    }

    public static void Baz([NotNull] string x)
    {
        System.Console.WriteLine(x);
    }


    public class NotNull : System.Attribute
    {

    }
}
