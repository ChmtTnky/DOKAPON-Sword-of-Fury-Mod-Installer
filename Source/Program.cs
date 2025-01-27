namespace Sword_of_Fury_Mod_Installer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Log.InitializeLogs();

        #if !DEBUG
            try
            {
                Installer.InstallMods();
            }
            catch (Exception ex)
            {
                Log.OutputError(ex.Message);
            }
        #else
            Installer.InstallMods();
        #endif

            Log.WriteLine(
                "Finished!\n" +
                "Press any key to close...");
            Log.Shutdown();
            Console.ReadKey();
        }
    }
}
