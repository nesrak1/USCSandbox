namespace USCSandbox
{
    public static class Logger
    {
        public static void Info(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        public static void Warning(string message)
        {
            Console.WriteLine($"[WARNING] {message}");
        }

        public static void Error(string message)
        {
            Console.WriteLine($"[ERROR] {message}");
        }
    }
}
