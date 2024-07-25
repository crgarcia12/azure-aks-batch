using System.Diagnostics;
using System.Numerics;

namespace Shared
{
    public class PiCalculator
    {

        public string CalculatePi(int numberOfDigits)
        {
            Stopwatch sp = Stopwatch.StartNew();

            Console.WriteLine($"Started calculation for [{numberOfDigits}] digits");

            if (numberOfDigits <= 0) return "3";

            BigInteger pi = 0;
            int k = 0;
            BigInteger sixteen = 16;
            BigInteger precision = BigInteger.Pow(10, numberOfDigits + 1);

            while (true)
            {
                BigInteger ak = (4 * BigInteger.Divide(precision, 8 * k + 1)
                               - 2 * BigInteger.Divide(precision, 8 * k + 4)
                               - BigInteger.Divide(precision, 8 * k + 5)
                               - BigInteger.Divide(precision, 8 * k + 6))
                               * BigInteger.Pow(sixteen, k);

                pi += BigInteger.Divide(ak, BigInteger.Pow(sixteen, k));

                if (ak == 0) break;

                k++;
            }

            string piString = (pi / BigInteger.Pow(10, numberOfDigits)).ToString();

            Console.WriteLine($"Calculation finished [{numberOfDigits}] digits [{sp.ElapsedMilliseconds}] ms");
            return piString[0] + "." + piString.Substring(1, numberOfDigits);
        }
    }
}
