using System.Reflection;

namespace Library.Testing;

public static class TestHelpers
{
    /*
     * From: https://blog.ncrunch.net/post/unit-test-private-methods-in-c.aspx
     */
    internal static TReturn? CallPrivateMethod<TInstance, TReturn>(
        TInstance instance,
        string methodName,
        object[] parameters)
    {
        Type? type = instance?.GetType();
        const BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Instance;
        MethodInfo? method = type?.GetMethod(methodName, bindingAttr);

        return (TReturn?)method?.Invoke(instance, parameters);
    }
}