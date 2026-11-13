using System.Reflection;

namespace HostelManagementSystem.Models
{
    public static class AppVersionInfo
    {
        public static string CurrentVersion =>
            Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
            ?? "1.2.3";
    }
}
