// See https://aka.ms/new-console-template for more information
using System.Numerics;

Console.WriteLine("Starting");

static BigInteger CalculatePi(int digits)
{
    BigInteger C = 426880 * BigInteger.Pow(10005, 1 / 2);
    BigInteger K = 6;
    BigInteger M = 1;
    BigInteger X = 1;
    BigInteger L = 13591409;
    BigInteger S = L;

    for (int i = 1; i < digits; i++)
    {
        M = (K * K * K - 16 * K) * M / (i * i * i);
        L += 545140134;
        X *= -262537412640768000;
        S += M * L / X;
        K += 12;
    }

    return C / S;
}

CalculatePi(10)