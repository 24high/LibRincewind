using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;

namespace FactorDemo;

// ---------------------------------------------------------------------------
// A REAL, working integer-factorisation demo.
//
// This is the honest, functional core of the "known algebraic methods -> p,q"
// idea. There is NO Riemann-Hypothesis "bias" step, because no such reduction
// exists: RH describes how primes are distributed on average, not which prime
// divides a given N. What actually factors integers are the classical
// algorithms below. They break *weak* or *small* moduli, and they demonstrate
// the single real shortcut in this family: Fermat's method destroys any RSA
// modulus whose two primes were generated too close together (a bad RNG).
// ---------------------------------------------------------------------------

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0)
        {
            RunDemoSuite();
            return 0;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "gen":
            {
                int bits = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 64;
                BigInteger p = IntMath.RandomPrime(bits / 2);
                BigInteger q = IntMath.RandomPrime(bits - bits / 2);
                BigInteger n = p * q;
                Console.WriteLine($"Random semiprime ({n.GetBitLength()} bit)");
                Console.WriteLine($"  (secret) p = {p}");
                Console.WriteLine($"  (secret) q = {q}");
                Console.WriteLine($"  N          = {n}\n");
                FactorAndReport(n);
                return 0;
            }
            case "weak":
            {
                int bits = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 128;
                BigInteger p = IntMath.RandomPrime(bits / 2);
                // q is the next prime just above p  ->  p and q are extremely close.
                BigInteger q = IntMath.NextPrime(p + IntMath.RandomSmall(1_000));
                BigInteger n = p * q;
                Console.WriteLine($"WEAK semiprime with near-equal primes ({n.GetBitLength()} bit)");
                Console.WriteLine($"  (secret) p    = {p}");
                Console.WriteLine($"  (secret) q    = {q}");
                Console.WriteLine($"  |q - p|       = {BigInteger.Abs(q - p)}");
                Console.WriteLine($"  N             = {n}\n");
                FactorAndReport(n);
                return 0;
            }
            default:
            {
                if (!BigInteger.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out BigInteger n))
                {
                    Console.Error.WriteLine($"Could not parse '{args[0]}' as an integer.");
                    Console.Error.WriteLine("Usage: FactorDemo [<N> | gen <bits> | weak <bits>]");
                    return 1;
                }
                FactorAndReport(n);
                return 0;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Curated demo suite: each case highlights the method that cracks it.
    // -----------------------------------------------------------------------
    private static void RunDemoSuite()
    {
        Console.WriteLine("=== FactorDemo — classical integer factorisation ===\n");
        Console.WriteLine("No Riemann magic. Just the algorithms that genuinely factor integers,");
        Console.WriteLine("each shown against the kind of modulus it is good at.\n");

        // 1. Textbook RSA-like small semiprime.
        DemoCase("Small semiprime (trial / rho)",
            BigInteger.Parse("10403", CultureInfo.InvariantCulture)); // 101 * 103

        // 2. Fermat's shortcut: two primes chosen very close together.
        BigInteger p = IntMath.NextPrime(BigInteger.Pow(10, 24));
        BigInteger q = IntMath.NextPrime(p + 4200);
        DemoCase("Near-equal primes  ->  Fermat breaks it instantly", p * q);

        // 3. Pollard p-1 shines when p-1 is smooth (product of small primes).
        //    p-1 = 2*3*5*7*11*13*17*19*23*29 * ... built to be smooth.
        BigInteger smoothP = IntMath.NextPrime(SmoothBase() );
        BigInteger otherQ = IntMath.RandomPrime(48);
        DemoCase("Smooth p-1  ->  Pollard p-1 wins", smoothP * otherQ);

        // 4. A "fair" random 72-bit semiprime: rho (Brent) does the work.
        BigInteger rp = IntMath.RandomPrime(36);
        BigInteger rq = IntMath.RandomPrime(36);
        DemoCase("Random 72-bit semiprime  ->  Pollard rho (Brent)", rp * rq);

        // 5. Composite with several prime factors -> full recursive factoring.
        DemoCase("Multi-factor composite  ->  full factorisation",
            new BigInteger(2) * 3 * 3 * 5 * 101 * 9973 * 99991);

        Console.WriteLine("Try it yourself:");
        Console.WriteLine("  dotnet run -- <N>          factor any integer");
        Console.WriteLine("  dotnet run -- gen 80       random 80-bit semiprime, then factor");
        Console.WriteLine("  dotnet run -- weak 256     near-equal primes -> Fermat instantly");
    }

    private static BigInteger SmoothBase()
    {
        // Build M = product of small primes so that M+1-ish primes have smooth p-1.
        BigInteger m = 1;
        foreach (int pr in new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43 })
            m *= pr;
        return m; // NextPrime(m) usually has p-1 sharing these small factors.
    }

    private static void DemoCase(string label, BigInteger n)
    {
        Console.WriteLine($"----- {label} -----");
        Console.WriteLine($"N = {n}   ({n.GetBitLength()} bit)");
        FactorAndReport(n);
        Console.WriteLine();
    }

    private static void FactorAndReport(BigInteger n)
    {
        var sw = Stopwatch.StartNew();
        List<(BigInteger factor, string method)> factors = Factorizer.Factor(n);
        sw.Stop();

        factors.Sort((a, b) => a.factor.CompareTo(b.factor));

        // Group into prime^exponent with the method that first split it out.
        var grouped = new List<(BigInteger prime, int exp, string method)>();
        foreach (var (f, method) in factors)
        {
            if (grouped.Count > 0 && grouped[^1].prime == f)
            {
                var last = grouped[^1];
                grouped[^1] = (last.prime, last.exp + 1, last.method);
            }
            else
            {
                grouped.Add((f, 1, method));
            }
        }

        string pretty = string.Join(" * ",
            grouped.Select(g => g.exp == 1 ? g.prime.ToString() : $"{g.prime}^{g.exp}"));

        // Verify the factorisation actually multiplies back to N.
        BigInteger check = 1;
        foreach (var (f, _) in factors) check *= f;
        bool ok = check == n;

        Console.WriteLine($"  = {pretty}");
        foreach (var g in grouped)
            Console.WriteLine($"      {g.prime}   (found by: {g.method}){(g.exp > 1 ? $"  x{g.exp}" : "")}");
        Console.WriteLine($"  verified: {(ok ? "YES (product == N)" : "NO — BUG")}   time: {sw.Elapsed.TotalMilliseconds:F1} ms");
    }
}

// ===========================================================================
//  Factorisation engine
// ===========================================================================
internal static class Factorizer
{
    // Fully factor n into primes, tagging each prime with the method that
    // first isolated it. Recursive: split n into d and n/d, recurse on both.
    public static List<(BigInteger factor, string method)> Factor(BigInteger n)
    {
        var result = new List<(BigInteger, string)>();
        if (n < 0) { result.Add((-1, "sign")); n = -n; }
        if (n <= 1) { if (n == 1) return result; result.Add((n, "trivial")); return result; }
        FactorRec(n, "trial-division", result);
        return result;
    }

    private static void FactorRec(BigInteger n, string via, List<(BigInteger, string)> acc)
    {
        // 1. Peel off small factors by trial division.
        foreach (int pr in SmallPrimes)
        {
            while (n % pr == 0)
            {
                acc.Add((pr, "trial-division"));
                n /= pr;
            }
        }
        if (n == 1) return;

        if (Primality.IsProbablePrime(n))
        {
            acc.Add((n, via));
            return;
        }

        // 2. n is composite. Find one non-trivial divisor, trying methods in
        //    order of "cheap and possibly instant" to "general workhorse".
        (BigInteger divisor, string method) = FindDivisor(n);

        BigInteger other = n / divisor;
        FactorRec(divisor, method, acc);
        FactorRec(other, method, acc);
    }

    private static (BigInteger, string) FindDivisor(BigInteger n)
    {
        // Fermat: instant if the two factors are close (weak RNG). Its bounded
        // search also happens to reach any factor pair within ~maxIters of
        // sqrt(n), which is why it can peel small cofactors too.
        BigInteger d = Methods.Fermat(n, maxIters: 200_000);
        if (d > 1 && d < n) return (d, "Fermat");

        // Pollard p-1: instant if some factor p has smooth p-1.
        d = Methods.PollardPMinus1(n, bound: 100_000);
        if (d > 1 && d < n) return (d, "Pollard p-1");

        // Pollard rho (Brent): the general-purpose workhorse.
        d = Methods.PollardRhoBrent(n);
        if (d > 1 && d < n) return (d, "Pollard rho (Brent)");

        // Last resort so the demo never hangs silently.
        throw new InvalidOperationException($"Failed to find a divisor of {n}");
    }

    private static readonly int[] SmallPrimes = BuildSmallPrimes(2000);

    private static int[] BuildSmallPrimes(int limit)
    {
        var sieve = new bool[limit + 1];
        var primes = new List<int>();
        for (int i = 2; i <= limit; i++)
        {
            if (sieve[i]) continue;
            primes.Add(i);
            for (long j = (long)i * i; j <= limit; j += i) sieve[j] = true;
        }
        return primes.ToArray();
    }
}

// ===========================================================================
//  Individual algorithms
// ===========================================================================
internal static class Methods
{
    // Fermat's factorisation. Writes n = a^2 - b^2 = (a-b)(a+b).
    // Finds factors fast when they are close to sqrt(n); bounded so it gives
    // up cheaply when they are not.
    public static BigInteger Fermat(BigInteger n, int maxIters)
    {
        if (n.IsEven) return 2;
        BigInteger a = IntMath.CeilSqrt(n);
        for (int i = 0; i < maxIters; i++, a++)
        {
            BigInteger b2 = a * a - n;
            if (b2.Sign < 0) continue;
            BigInteger b = IntMath.ISqrt(b2);
            if (b * b == b2)
            {
                BigInteger f = a - b;
                if (f > 1 && f < n) return f;
            }
        }
        return 1; // give up
    }

    // Pollard's p-1: exploits a prime factor p where p-1 is B-smooth.
    public static BigInteger PollardPMinus1(BigInteger n, int bound)
    {
        BigInteger a = 2;
        for (int j = 2; j <= bound; j++)
        {
            a = BigInteger.ModPow(a, j, n);
            if ((j & 0x3FF) == 0 || j == bound)
            {
                BigInteger g = BigInteger.GreatestCommonDivisor(a - 1, n);
                if (g > 1 && g < n) return g;
                if (g == n) return 1; // overshot; caller falls through to rho
            }
        }
        BigInteger gfin = BigInteger.GreatestCommonDivisor(a - 1, n);
        return (gfin > 1 && gfin < n) ? gfin : 1;
    }

    // Pollard's rho with Brent's cycle detection — the general workhorse.
    public static BigInteger PollardRhoBrent(BigInteger n)
    {
        if (n.IsEven) return 2;

        for (int attempt = 0; attempt < 64; attempt++)
        {
            BigInteger c = IntMath.RandomBelow(n - 1) + 1;
            BigInteger y = IntMath.RandomBelow(n - 1) + 1;
            BigInteger m = 128;
            BigInteger g = 1, r = 1, q = 1;
            BigInteger x = 0, ys = 0;

            while (g == 1)
            {
                x = y;
                for (BigInteger i = 0; i < r; i++) y = (y * y + c) % n;

                BigInteger k = 0;
                while (k < r && g == 1)
                {
                    ys = y;
                    BigInteger lim = BigInteger.Min(m, r - k);
                    for (BigInteger i = 0; i < lim; i++)
                    {
                        y = (y * y + c) % n;
                        q = q * BigInteger.Abs(x - y) % n;
                    }
                    g = BigInteger.GreatestCommonDivisor(q, n);
                    k += m;
                }
                r *= 2;
            }

            if (g == n)
            {
                // Back-track to find the factor one step at a time.
                do
                {
                    ys = (ys * ys + c) % n;
                    g = BigInteger.GreatestCommonDivisor(BigInteger.Abs(x - ys), n);
                } while (g == 1);
            }

            if (g > 1 && g < n) return g;
            // else: unlucky c/y, try again
        }
        return 1;
    }
}

// ===========================================================================
//  Primality test (Miller-Rabin, deterministic for < 3.3e24, then random)
// ===========================================================================
internal static class Primality
{
    // Deterministic bases covering all n < 3,317,044,064,679,887,385,961,981.
    private static readonly int[] DetBases = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };

    public static bool IsProbablePrime(BigInteger n)
    {
        if (n < 2) return false;
        foreach (int p in new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 })
        {
            if (n == p) return true;
            if (n % p == 0) return false;
        }

        // n-1 = d * 2^s
        BigInteger d = n - 1;
        int s = 0;
        while (d.IsEven) { d >>= 1; s++; }

        foreach (int a in DetBases)
            if (!MillerRabinWitness(n, a, d, s))
                return false;

        // For very large n, add random bases for extra confidence.
        if (n.GetBitLength() > 81)
        {
            for (int i = 0; i < 16; i++)
            {
                BigInteger a = IntMath.RandomBelow(n - 3) + 2;
                if (!MillerRabinWitness(n, a, d, s))
                    return false;
            }
        }
        return true;
    }

    private static bool MillerRabinWitness(BigInteger n, BigInteger a, BigInteger d, int s)
    {
        BigInteger x = BigInteger.ModPow(a, d, n);
        if (x == 1 || x == n - 1) return true;
        for (int r = 1; r < s; r++)
        {
            x = x * x % n;
            if (x == n - 1) return true;
        }
        return false;
    }
}

// ===========================================================================
//  Big-integer helpers
// ===========================================================================
internal static class IntMath
{
    // Integer square root (floor) via Newton's method.
    public static BigInteger ISqrt(BigInteger n)
    {
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
        if (n < 2) return n;
        BigInteger x = (BigInteger)1 << (int)((n.GetBitLength() + 1) / 2);
        while (true)
        {
            BigInteger y = (x + n / x) >> 1;
            if (y >= x) break;
            x = y;
        }
        while (x * x > n) x--;
        while ((x + 1) * (x + 1) <= n) x++;
        return x;
    }

    public static BigInteger CeilSqrt(BigInteger n)
    {
        BigInteger s = ISqrt(n);
        return s * s == n ? s : s + 1;
    }

    // Uniform random in [0, exclusiveMax).
    public static BigInteger RandomBelow(BigInteger exclusiveMax)
    {
        if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
        int bytes = (int)(exclusiveMax.GetBitLength() / 8) + 1;
        while (true)
        {
            byte[] buf = new byte[bytes + 1];
            RandomNumberGenerator.Fill(buf.AsSpan(0, bytes));
            buf[bytes] = 0; // force non-negative
            BigInteger v = new BigInteger(buf);
            if (v < exclusiveMax) return v;
        }
    }

    public static BigInteger RandomSmall(int exclusiveMax) =>
        RandomBelow(exclusiveMax);

    // A random probable-prime with exactly `bits` bits (top bit set).
    public static BigInteger RandomPrime(int bits)
    {
        if (bits < 2) bits = 2;
        while (true)
        {
            byte[] buf = new byte[(bits + 7) / 8 + 1];
            RandomNumberGenerator.Fill(buf);
            buf[^1] = 0; // non-negative
            BigInteger cand = new BigInteger(buf);
            cand &= (BigInteger.One << bits) - 1;   // trim to bits
            cand |= BigInteger.One << (bits - 1);   // set top bit
            cand |= 1;                              // make odd
            if (Primality.IsProbablePrime(cand)) return cand;
        }
    }

    public static BigInteger NextPrime(BigInteger n)
    {
        if (n <= 2) return 2;
        if (n.IsEven) n++;
        while (!Primality.IsProbablePrime(n)) n += 2;
        return n;
    }
}
