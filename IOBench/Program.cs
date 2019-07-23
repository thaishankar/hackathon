using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;




namespace IOBench
{
    class Program
    {
        private const int ReadBufferSize = 1024 * 1024; // Deal with 1MiB chucks at a time
        private const int WriteBufferSize = 1024 * 1024; // Deal with 1MiB chucks at a time
        private const string ResultsFilename = "resultsDisk.csv";
        private const int FullIOSize = 10 * 1024 * 1024; // 10MiB IOs
        private const int SweepSize = 50;
        private const byte TestDatum = 66; // ASCII 'B'

        static void Main(string[] args)
        {
            if(args.Length != 1)
            {
                Console.WriteLine("Usage: IOBench.exe <Test Directory Path>");
                return;
            }

            // Setup the test directory
            string testDir = args[0];
            if (Directory.Exists(testDir))
            {
                Directory.CreateDirectory(testDir);
            }

            // Do the write test first (creates the files used for the read test
            long[] writeDistribution;
            Program.WriteSweep(testDir, SweepSize, FullIOSize, out writeDistribution);

            // Do the read test
            long[] readDistribution;
            Program.ReadSweep(testDir, SweepSize, FullIOSize, out readDistribution);

            // Capture the results from the sweeps
            GenerateResultsFile(testDir, readDistribution, writeDistribution);

            // Done
        }

        private static void GenerateResultsFile(string resultsDir, long[] readDistribution, long[] writeDistribution)
        {
            if (!Directory.Exists(resultsDir))
            {
                throw new ArgumentException("Invalid result directory supplied to GenerateResultsFile");
            }

            if(readDistribution.Length != writeDistribution.Length)
            {
                throw new ArgumentException("Read and Write distributions do not match sizes");
            }

            string resultsFilepath = Path.Combine(resultsDir, ResultsFilename);

            StreamWriter resultsFile = new StreamWriter(resultsFilepath, false);
            resultsFile.WriteLine("Attempt #, Read Latency (ms), Write Latency (ms)");
            for(int attempt = 0; attempt < readDistribution.Length; attempt++)
            {
                resultsFile.WriteLine(string.Format("{0}, {1}, {2}", attempt, readDistribution[attempt], writeDistribution[attempt]));
            }
            resultsFile.Close();
        }

        private static long ReadSweep (string sweepDir, int sweepSize, int ioSize, out long[] readDistribution)
        {
            if (!Directory.Exists(sweepDir))
            {
                Directory.CreateDirectory(sweepDir);
            }

            readDistribution = new long[sweepSize];
            long totalReadLatency = 0;
            long readLatency;

            for (int attempt = 0; attempt < readDistribution.Length; attempt++)
            {
                string filename = string.Format("temp{0}.tmp", attempt);
                string filepath = Path.Combine(sweepDir, filename);
                readLatency = ReadFile(filepath, ioSize);
                readDistribution[attempt] = readLatency;
                totalReadLatency += readLatency;
            }

            return totalReadLatency / sweepSize;
        }

        private static long WriteSweep(string sweepDir, int sweepSize, int ioSize, out long[] writeDistribution)
        {
            if (!Directory.Exists(sweepDir))
            {
                Directory.CreateDirectory(sweepDir);
            }

            writeDistribution = new long[sweepSize];
            long totalWriteLatency = 0;
            long writeLatency;

            for (int attempt = 0; attempt < writeDistribution.Length; attempt++)
            {
                string filename = string.Format("temp{0}.tmp", attempt);
                string filepath = Path.Combine(sweepDir, filename);
                writeLatency = WriteFile(filepath, ioSize);
                writeDistribution[attempt] = writeLatency;
                totalWriteLatency += writeLatency;
            }

            return totalWriteLatency / sweepSize;
        }


        private static long ReadFile(string filepath, int size)
        {
            Stopwatch watch = new Stopwatch();
            
            byte[] dataBuffer = new byte[ReadBufferSize];
            int bytesRead = 0;
            int bytesToRead;
            int bytesRemaining = size;

            try
            {
                FileStream reader = new FileStream(filepath, FileMode.Open);

                watch.Start();
                do
                {
                    if(bytesRemaining > dataBuffer.Length)
                    {
                        bytesToRead = dataBuffer.Length;
                    }
                    else
                    {
                        bytesToRead = bytesRemaining;
                    }

                    bytesRead = reader.Read(dataBuffer, 0, bytesToRead);
                    bytesRemaining -= bytesRead;

                    // Only uncomment inorder to validate write-finishing/write-read coherence (Significant slow down)
                    //for (int b = 0; b <bytesToRead; b++)
                    //{
                    //    if (dataBuffer[b] != TestDatum)
                    //    {
                    //        Console.WriteLine("Didn't not read the expected data value for dataBuffer[{0}].", b);
                    //    }
                    //}
                } while (bytesRemaining > 0);
                reader.Close();
                watch.Stop();

                // Sanity Check the data retrieved (via the last round of the buffer)
                for(int b = 0; b < dataBuffer.Length; b++)
                {
                    if(dataBuffer[b] != TestDatum)
                    {
                        Console.WriteLine("Didn't not read the expected data value for dataBuffer[{0}].", b);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Encountered exception during file read attempt: {0}", ex.ToString()));
            }

            return watch.ElapsedMilliseconds;
        }

        private static long WriteFile(string filepath, int size)
        {
            Stopwatch watch = new Stopwatch();

            byte[] dataBuffer = new byte[WriteBufferSize];
            for(int b = 0; b < dataBuffer.Length; b++)
            {
                dataBuffer[b] = TestDatum;
            }

            int bytesToWrite;
            int bytesRemaining = size;

            try
            {
                // Make sure to create new files for writing to filter out cross-sweep caching effects
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }
                // Force write-through to see true disk performance
                FileStream writer = new FileStream(filepath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 100, FileOptions.WriteThrough);

                watch.Start();
                do
                {
                    if (bytesRemaining > dataBuffer.Length)
                    {
                        bytesToWrite = dataBuffer.Length;
                    }
                    else
                    {
                        bytesToWrite = bytesRemaining;
                    }

                    writer.Write(dataBuffer, 0, bytesToWrite);
                    bytesRemaining -= bytesToWrite;
                } while (bytesRemaining > 0);
                writer.Flush();
                writer.Close();
                watch.Stop();

            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Encountered exception during file write attempt: {0}", ex.ToString()));
            }

            return watch.ElapsedMilliseconds;
        }

    }
}
