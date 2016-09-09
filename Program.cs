using System;
using System.IO;
using InstagramBooster.API;

namespace InstagramBooster
{
    class Program
    {
        private static readonly object consoleLock = new object();
        private static object writeLock = new object();

        static void Main(string[] args)
        {
            Console.Title = "[InstagramBooster] - zFlarex";

            try
            {
                StreamReader streamReader = new StreamReader("proxies.txt");
                string[] proxies = streamReader.ReadToEnd().Split('\n');
                streamReader.Close();

                foreach (string proxyAddress in proxies)
                {
                    if (proxyAddress != "")
                    {
                        string[] proxyArray = proxyAddress.Split(':');

                        Registration instagramAccount = new Registration(proxyArray[0], int.Parse(proxyArray[1]));

                        instagramAccount.OnSuccess += HandleSuccess;
                        instagramAccount.OnWarning += HandleWarning;
                        instagramAccount.OnFailure += HandleFailure;
                        instagramAccount.Create();
                    }

                }
            }
            catch(IOException ex)
            {
                HandleFailure("'proxies.txt' wasn't found in this directory.");
            }

            while (true)
            {
                Console.ReadLine();
            }
        }

        static void HandleSuccess(Registration registeredAccount)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[Success]: Successfully created {0}:{1} using '{2}'.", registeredAccount.Username, registeredAccount.Password, registeredAccount.Proxy);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            lock(writeLock)
            {
                StreamWriter streamWriter = new StreamWriter("accounts.txt", true);
                streamWriter.Write(string.Format("{0}:{1}\n", registeredAccount.Username, registeredAccount.Password));
                streamWriter.Close();
            }

        }

        static void HandleWarning(string warningMessage)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Warning]: {0}", warningMessage);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        static void HandleFailure(string errorMessage)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Error]: {0}", errorMessage);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}
