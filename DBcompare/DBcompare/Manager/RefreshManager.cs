using System.Reflection;
using DBcompare.Common;

namespace DBcompare.Manager;

public class RefreshManager
{
    public static void RefreshAll()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();

        foreach (var type in types)
        {
            var methods = type.GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(RefreshableAttribute), false).Any());

            foreach (var method in methods)
            {
                var instance = Activator.CreateInstance(type);
                method.Invoke(instance, null);
            }
        }
    }
}