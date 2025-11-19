namespace MiProyecto.infrastructure.nhibernate.filtering;

public static class StringExtender
{
    public static string ToPascalCase(this string instance)
    {
        return string.IsNullOrWhiteSpace(instance)
            ? instance
            : $"{instance.Substring(0, 1).ToUpper()}{instance.Substring(1)}";
    }
}
