// See https://aka.ms/new-console-template for more information
using System.Collections;
using System.Diagnostics;

Console.WriteLine("Starting Container");

// Print all environment variables
IDictionary environmentVariables = Environment.GetEnvironmentVariables();
foreach (DictionaryEntry entry in environmentVariables)
{
    Console.WriteLine($"[ENV] {entry.Key} = {entry.Value}");
}


string CalculatePi(int digits)
{
    double pi = 0;
    for (int k = 0; k < digits; k++)
    {
        pi += (1.0 / Math.Pow(16, k)) *
              (4.0 / (8 * k + 1) -
               2.0 / (8 * k + 4) -
               1.0 / (8 * k + 5) -
               1.0 / (8 * k + 6));
    }
    return pi.ToString("F" + digits);
}

int nrOfDigits = 20000000;
while (true)
{
    Stopwatch sw = Stopwatch.StartNew();
    CalculatePi(nrOfDigits);
    Console.WriteLine($"[T:{environmentVariables["TESTID"]}][R:{environmentVariables["REPLICAS"]}][{environmentVariables["HOSTNAME"]}][CALC] For {nrOfDigits} digits it took -{sw.ElapsedMilliseconds}- ms");
}

