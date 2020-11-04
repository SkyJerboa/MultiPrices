using System;
using System.IO;
using System.Threading;

namespace MP.Core.Common.Heplers
{
    public static class FileHelper
    {
        public static bool WhaitFileUnlock(string path, int tries = 10)
        {
            int triesCount = 0;
            while (triesCount < tries)
            {
                try
                {
                    using (FileStream fs = new FileStream(path, FileMode.Open))
                    {
                        fs.ReadByte();

                        return true;
                    }
                }
                catch
                {
                    Thread.Sleep(500);
                }
            }

            return true;
        }

        public static byte[] ComputeFileHash(string filePath)
        {
            int runCount = 1;

            while (runCount < 4)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        using (var fs = File.OpenRead(filePath))
                        {
                            return System.Security.Cryptography.SHA1
                                .Create().ComputeHash(fs);
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException();
                    }
                }
                catch (IOException ex)
                {
                    if (runCount == 3 || ex.HResult != -2147024864)
                    {
                        throw;
                    }
                    else
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(2, runCount)));
                        runCount++;
                    }
                }
            }

            return new byte[20];
        }
    }
}
