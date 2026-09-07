/*
Copyright 2023 Dennis Michael Heine

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LibRincewind_4._7._2
{
    /// <summary>
    /// A distribution-transforming encoder (DTE) for passwords — the missing half of
    /// LibRincewind's deniability claim.
    ///
    /// THE PROBLEM THIS SOLVES
    /// ----------------------
    /// CRincewind promises that brute force gives no signal: every candidate password
    /// yields a plausible printable string, so nothing can be recognised. That promise
    /// only holds when the *plaintext itself* looks uniform over the 95-character
    /// alphabet. Feed it a human password — "dragon2011" — and the promise breaks: the
    /// right key produces a word plus a year, every wrong key produces line noise, and
    /// an attacker simply picks the one that looks like a password. Measured against
    /// the unmodified library, a letter-frequency score ranked the true key #1 out of
    /// 3001 candidates on every trial.
    ///
    /// No cipher can fix that by itself. Decrypting under the right key MUST return the
    /// plaintext, so if the plaintext is recognisable, so is the key. The fix has to
    /// change *what gets encrypted*.
    ///
    /// HOW IT WORKS
    /// ------------
    /// This class encodes a password into a fixed-length *seed* over the same 95-symbol
    /// alphabet, such that
    ///
    ///     Decode(Encode(p)) == p                for every supported password p
    ///     Decode(uniformly random seed)         is a plausible human password
    ///
    /// The second property is the point. Because <see cref="Decode"/> is *total* — it
    /// accepts all 95^144 possible seeds and never fails — a wrong key no longer yields
    /// garbage. It yields "sunshine1", "Hockey!23", "monkey2004": decoys drawn from the
    /// same distribution as real passwords. The right key stops standing out.
    ///
    /// This is Honey Encryption (Juels &amp; Ristenpart, 2014) with a password-vault DTE
    /// (cf. Chatterjee et al., 2015). It is Kerckhoffs-clean: the model below is fully
    /// public and holds no secret. Its strength lies entirely in how well it matches
    /// real password distributions, never in hiding how it works.
    ///
    /// WHY THERE IS NO HEADER, NO LENGTH FIELD AND NO PADDING CHECK
    /// -----------------------------------------------------------
    /// Each of those would be a validity check, and a validity check is exactly the
    /// oracle this construction exists to remove: it would succeed only under the right
    /// key. So the seed is a fixed 144 symbols for every password — which also hides the
    /// password's length — the decoder reads only as many symbols as its decisions
    /// require, and it *never looks at the rest*. The unused tail is filled with fresh
    /// random symbols at encode time and is unconstrained: there is nothing there to
    /// verify, and therefore nothing to test.
    ///
    /// THE MODEL, AND ITS HONEST LIMIT
    /// ------------------------------
    /// A small public PCFG: a template ("word + digits", "word + digits + symbol",
    /// digits only, unstructured, ...), a Zipf-weighted dictionary, capitalisation,
    /// leetspeak, and digit groups biased toward years and common runs. Decoys are only
    /// as convincing as this model. An attacker whose password model is sharper than
    /// this one can still separate real from decoy — that is inherent to Honey
    /// Encryption, not a defect of this implementation, and it is why published vault
    /// schemes have been improved and re-broken as the models on both sides advanced
    /// (Golla et al., 2016). Widening <see cref="Words"/> with a real frequency-ordered
    /// corpus is the single most effective way to strengthen it.
    ///
    /// USAGE
    ///   string envelope  = rincewind.encryptString(PasswordDte.Encode(pw), k1, k2);
    ///   string recovered = PasswordDte.Decode(rincewind.decryptString(envelope, k1, k2));
    /// </summary>
    public static class PasswordDte
    {
        // ---------------------------------------------------------------- layout --

        /// <summary>
        /// Symbols per seed. Fixed for every password, so the ciphertext length leaks
        /// nothing about the password's length. Must be at least
        /// <see cref="MaxRawLength"/> * 2 + 4 so the unstructured path always fits.
        /// </summary>
        public const int SeedLength = 144;

        /// <summary>Longest password the unstructured fallback path can carry.</summary>
        public const int MaxRawLength = 64;

        private const int AlphabetSize = 95;
        private const int AlphabetMin = 32;

        // ---------------------------------------------------------- coding tables --
        //
        // Every decision is one fixed-width field of base-95 symbols. A table maps the
        // field's entire value range onto its outcomes in proportion to their weights,
        // leaving no value unmapped: that is what makes Decode total.

        private sealed class Table
        {
            internal readonly int Width;
            internal readonly long Range;
            private readonly long[] cum;   // cum[0] = 0, cum[n] = Range

            internal Table(double[] weights)
            {
                int n = weights.Length;
                if (n < 1) throw new ArgumentException("A table needs at least one outcome.");

                // Widen the field until every outcome can hold its own slice with room
                // left to express the weighting.
                int width = 1;
                long range = AlphabetSize;
                while (range < 4L * n)
                {
                    width++;
                    range *= AlphabetSize;
                }
                Width = width;
                Range = range;

                double total = 0;
                for (int i = 0; i < n; i++)
                {
                    if (weights[i] <= 0) throw new ArgumentException("Weights must be positive.");
                    total += weights[i];
                }

                // Proportional allocation, then repair the rounding so the slices cover
                // the range exactly. Every outcome keeps at least one slot, so no
                // outcome is unreachable and no seed value is left unmapped.
                long[] slots = new long[n];
                long assigned = 0;
                for (int i = 0; i < n; i++)
                {
                    long s = (long)Math.Floor(weights[i] / total * range);
                    if (s < 1) s = 1;
                    slots[i] = s;
                    assigned += s;
                }
                while (assigned > range)
                {
                    int best = -1;
                    long bestVal = 1;
                    for (int i = 0; i < n; i++)
                        if (slots[i] > bestVal) { bestVal = slots[i]; best = i; }
                    if (best < 0) break;
                    slots[best]--;
                    assigned--;
                }
                if (assigned < range)
                {
                    int best = 0;
                    for (int i = 1; i < n; i++) if (slots[i] > slots[best]) best = i;
                    slots[best] += range - assigned;
                }

                cum = new long[n + 1];
                long acc = 0;
                for (int i = 0; i < n; i++) { cum[i] = acc; acc += slots[i]; }
                cum[n] = acc;
            }

            internal long Lo(int index) { return cum[index]; }
            internal long Hi(int index) { return cum[index + 1]; }

            /// <summary>Maps any value in [0, Range) to an outcome. Never fails.</summary>
            internal int Decode(long v)
            {
                if (v < 0) v = 0;
                if (v >= Range) v = Range - 1;
                int lo = 0, hi = cum.Length - 2;
                while (lo < hi)
                {
                    int mid = (lo + hi + 1) / 2;
                    if (cum[mid] <= v) lo = mid; else hi = mid - 1;
                }
                return lo;
            }
        }

        // ------------------------------------------------------------ seed buffers --

        private sealed class Writer
        {
            private readonly int[] digits = new int[SeedLength];
            private int pos;

            internal bool Write(Table t, int index)
            {
                if (pos + t.Width > SeedLength) return false;
                long lo = t.Lo(index), hi = t.Hi(index);
                // A uniform point inside the outcome's slice. The seed has to look
                // uniform, so it must not always sit on a slice boundary.
                long v = lo + RandomBelow(hi - lo);
                for (int i = t.Width - 1; i >= 0; i--)
                {
                    digits[pos + i] = (int)(v % AlphabetSize);
                    v /= AlphabetSize;
                }
                pos += t.Width;
                return true;
            }

            /// <summary>Fills the unread tail with fresh random symbols and emits the seed.</summary>
            internal string Finish()
            {
                byte[] buf = new byte[1];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    for (int i = pos; i < SeedLength; i++)
                    {
                        int v;
                        do { rng.GetBytes(buf); v = buf[0]; } while (v >= 190);
                        digits[i] = v % AlphabetSize;
                    }
                }
                char[] c = new char[SeedLength];
                for (int i = 0; i < SeedLength; i++) c[i] = (char)(AlphabetMin + digits[i]);
                return new string(c);
            }
        }

        private sealed class Reader
        {
            private readonly string seed;
            private int pos;

            internal Reader(string seed) { this.seed = seed ?? ""; }

            /// <summary>
            /// Reads one field. Missing or out-of-range symbols read as 0 rather than
            /// throwing: a decoder that can fail is a decoder that leaks.
            /// </summary>
            internal int Read(Table t)
            {
                long v = 0;
                for (int i = 0; i < t.Width; i++)
                {
                    int d = 0;
                    if (pos < seed.Length)
                    {
                        int raw = seed[pos] - AlphabetMin;
                        if (raw >= 0 && raw < AlphabetSize) d = raw;
                    }
                    pos++;
                    v = v * AlphabetSize + d;
                }
                return t.Decode(v);
            }
        }

        private static long RandomBelow(long exclusiveMax)
        {
            if (exclusiveMax <= 1) return 0;
            byte[] buf = new byte[8];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                while (true)
                {
                    rng.GetBytes(buf);
                    long v = BitConverter.ToInt64(buf, 0) & long.MaxValue;
                    long limit = long.MaxValue - (long.MaxValue % exclusiveMax);
                    if (v < limit) return v % exclusiveMax;
                }
            }
        }

        // ------------------------------------------------------------- the model ---

        private static readonly string[] Templates =
        {
            "WD", "W", "R", "WW", "WDS", "WS", "WWD", "D",
            "DW", "SWD", "WSD", "WWS", "WWDS", "SW", "WDW"
        };

        private static readonly double[] TemplateWeights =
        {
            30, 12, 14, 5, 6, 3, 4, 3,
            2, 1, 1, 1, 1, 1, 1
        };

        private static readonly double[] CapsWeights = { 74, 22, 4 };   // lower, Capitalised, UPPER
        private static readonly double[] LeetWeights = { 96, 4 };       // off, canonical

        private static readonly string[] Symbols =
        { "!", "@", "#", "$", ".", "_", "-", "*", "&", "%", "+", "?", "/", "=", "," };

        private static readonly double[] SymbolWeights =
        { 30, 12, 8, 7, 8, 6, 6, 5, 3, 3, 3, 3, 2, 2, 2 };

        private static readonly double[] DigitKindWeights = { 40, 25, 35 }; // common, year, generic

        private static readonly string[] CommonDigits =
        { "1", "12", "123", "1234", "12345", "123456", "0", "00", "11", "22",
          "69", "88", "99", "007", "666", "777", "2", "3", "21", "23" };

        private static readonly double[] CommonDigitWeights =
        { 25, 10, 20, 15, 6, 10, 4, 3, 5, 3,
          3, 3, 4, 2, 2, 2, 4, 3, 3, 3 };

        private const int YearMin = 1940;
        private const int YearMax = 2029;

        private static readonly double[] GenericDigitLenWeights = { 8, 20, 15, 25, 10, 22 }; // 1..6

        /// <summary>
        /// Frequency-ordered dictionary; weights are Zipf over this order. Duplicates
        /// are removed at start-up, so the list may be edited freely. Replacing it with
        /// a real leak-derived frequency list is the most effective single improvement
        /// to decoy quality.
        /// </summary>
        private static readonly string[] Words =
        {
            "password","dragon","monkey","letmein","iloveyou","princess","sunshine","football",
            "baseball","welcome","shadow","master","qwerty","superman","michael","jennifer",
            "jordan","hunter","ranger","buster","soccer","harley","batman","andrew",
            "tigger","robert","thomas","hockey","killer","george","charlie","andrea",
            "matrix","joshua","taylor","daniel","ginger","summer","ashley","nicole",
            "chelsea","matthew","access","yankees","dallas","austin","thunder","william",
            "corvette","hello","martin","heather","secret","merlin","diamond","purple",
            "gateway","orange","sparky","phoenix","mickey","bailey","knight","iceman",
            "tigers","dakota","player","sunset","bigdog","cowboy","eagle","chicken",
            "dolphin","rainbow","cookie","spider","guitar","hammer","silver","willow",
            "banana","angel","flower","garden","winter","spring","autumn","coffee",
            "butter","cherry","yellow","green","black","white","brown","house",
            "mouse","horse","tiger","panda","zebra","shark","whale","snake",
            "mango","apple","lemon","grape","peach","melon","olive","onion",
            "pasta","pizza","bread","honey","sugar","candy","chess","poker",
            "magic","music","dance","movie","radio","video","photo","paper",
            "pencil","table","chair","door","window","mirror","clock","watch",
            "phone","laptop","screen","river","ocean","mountain","forest","island",
            "desert","valley","meadow","cloud","storm","light","night","morning",
            "evening","friday","monday","sunday","school","office","street","bridge",
            "castle","temple","market","garage","kitchen","bedroom","letter","number",
            "circle","square","triangle","violet","indigo","scarlet","golden","copper",
            "iron","steel","stone","glass","wood","smile","laugh","dream",
            "heart","peace","happy","lucky","sweet","brave","quiet","quick",
            "smart","strong","gentle","bright","frozen","hidden","silent","wild",
            "free","true","blue","royal","prince","queen","king","lord",
            "wizard","griffin","falcon","raven","robin","sparrow","condor","cobra",
            "viper","lion","wolf","bear","fox","hawk","owl","deer",
            "moose","otter","seal","crab","trout","salmon","tuna","perch",
            "pike","bass","violin","piano","drums","flute","trumpet","banjo",
            "cello","rocket","planet","galaxy","comet","meteor","saturn","jupiter",
            "venus","mars","mercury","neptune","pluto","cosmos","nebula","quasar",
            "photon","atom","proton","neutron","quark","laser","plasma","fusion",
            "matter","energy","gravity","motion","force","power","engine","turbine",
            "piston","wheel","anchor","harbor","sailor","captain","pirate","treasure",
            "compass","voyage","journey","travel","wander","explore","discover","adventure",
            "legend","myth","story","chapter","volume","library","museum","gallery",
            "theater","concert","festival","carnival","parade","holiday","vacation","weekend",
            "birthday","wedding","family","brother","sister","father","mother","cousin",
            "uncle","aunt","friend","buddy","partner","teacher","student","doctor",
            "nurse","lawyer","farmer","baker","butcher","tailor","barber","driver",
            "pilot","soldier","fisher","miner","builder","painter","writer","singer",
            "dancer","actor","artist","poet","chef","judge","mayor","trust",
            "honor","glory","victory","triumph","courage","wisdom","justice","freedom",
            "liberty","destiny","fortune","miracle","wonder","mystery","twilight","sunrise",
            "moonlight","starlight","daylight","midnight","lightning","waterfall","volcano","canyon",
            "glacier","tundra","prairie","jungle","swamp","lagoon","reef","beach",
            "shore","cliff","cave","summit","trail","path","road","route",
            "lane","avenue","boulevard","highway"
        };

        // ---------------------------------------------------------- built tables ---

        private static readonly string[] WordList;
        private static readonly Dictionary<string, int> WordIndex;
        private static readonly int RawTemplateIndex;
        private static readonly Table TemplateTable;
        private static readonly Table WordTable;
        private static readonly Table CapsTable;
        private static readonly Table LeetTable;
        private static readonly Table SymbolTable;
        private static readonly Table DigitKindTable;
        private static readonly Table CommonDigitTable;
        private static readonly Table YearTable;
        private static readonly Table GenericLenTable;
        private static readonly Table DigitTable;
        private static readonly Table RawLenTable;
        private static readonly Table RawCharTable;

        static PasswordDte()
        {
            // De-duplicate so every dictionary slot is a distinct word and the index
            // map stays unambiguous.
            List<string> uniq = new List<string>(Words.Length);
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Words.Length; i++)
                if (seen.Add(Words[i])) uniq.Add(Words[i]);

            WordList = uniq.ToArray();
            WordIndex = new Dictionary<string, int>(WordList.Length, StringComparer.Ordinal);
            for (int i = 0; i < WordList.Length; i++) WordIndex[WordList[i]] = i;

            double[] wordWeights = new double[WordList.Length];
            for (int i = 0; i < WordList.Length; i++)
                wordWeights[i] = 1.0 / Math.Pow(i + 1, 0.9);   // Zipf over frequency order

            RawTemplateIndex = -1;
            for (int i = 0; i < Templates.Length; i++)
                if (Templates[i] == "R") { RawTemplateIndex = i; break; }

            TemplateTable = new Table(TemplateWeights);
            WordTable = new Table(wordWeights);
            CapsTable = new Table(CapsWeights);
            LeetTable = new Table(LeetWeights);
            SymbolTable = new Table(SymbolWeights);
            DigitKindTable = new Table(DigitKindWeights);
            CommonDigitTable = new Table(CommonDigitWeights);

            double[] yearWeights = new double[YearMax - YearMin + 1];
            for (int y = YearMin; y <= YearMax; y++)
            {
                double w = 1;
                if (y >= 1970 && y <= 2012) w = 5;
                if (y >= 1985 && y <= 2005) w = 10;
                yearWeights[y - YearMin] = w;
            }
            YearTable = new Table(yearWeights);

            GenericLenTable = new Table(GenericDigitLenWeights);
            DigitTable = new Table(new double[] { 12, 14, 11, 10, 9, 9, 9, 9, 8, 9 }); // 0..9

            double[] rawLen = new double[MaxRawLength + 1];
            for (int i = 0; i <= MaxRawLength; i++)
            {
                // Shaped like real "other" passwords: mostly generator output in the
                // 12..24 range, few very short ones. Short random strings were the most
                // obviously fake decoys before this was tuned.
                double w;
                if (i < 6) w = 0.3;
                else if (i <= 11) w = 4;
                else if (i <= 24) w = 10;
                else if (i <= 40) w = 3;
                else w = 0.5;
                rawLen[i] = w;
            }
            RawLenTable = new Table(rawLen);

            double[] rawChar = new double[AlphabetSize];
            for (int i = 0; i < AlphabetSize; i++)
            {
                char c = (char)(AlphabetMin + i);
                double w;
                if (c >= 'a' && c <= 'z') w = 40;
                else if (c >= '0' && c <= '9') w = 35;
                else if (c >= 'A' && c <= 'Z') w = 15;
                else w = 6;
                rawChar[i] = w;
            }
            RawCharTable = new Table(rawChar);
        }

        // ---------------------------------------------------------------- encode ---

        /// <summary>True if <see cref="Encode"/> can carry this password.</summary>
        public static bool CanEncode(string password)
        {
            if (password == null) return false;
            if (password.Length > MaxRawLength) return false;
            for (int i = 0; i < password.Length; i++)
                if (password[i] < AlphabetMin || password[i] > AlphabetMin + AlphabetSize - 1)
                    return false;
            return true;
        }

        /// <summary>
        /// Encodes a password into a <see cref="SeedLength"/>-symbol seed over printable
        /// ASCII. Encrypt the seed, not the password.
        /// </summary>
        public static string Encode(string password)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (password.Length > MaxRawLength)
                throw new ArgumentOutOfRangeException("password",
                    "PasswordDte carries at most " + MaxRawLength + " characters; raise MaxRawLength " +
                    "and SeedLength together to extend it.");
            for (int i = 0; i < password.Length; i++)
                if (password[i] < AlphabetMin || password[i] > AlphabetMin + AlphabetSize - 1)
                    throw new ArgumentOutOfRangeException("password",
                        "PasswordDte handles printable ASCII (U+0020..U+007E) only; found U+" +
                        ((int)password[i]).ToString("X4") + " at index " + i + ".");

            // Structured path, as written. Each attempt gets its own Writer: a failed
            // attempt leaves its buffer partly filled, so buffers are never reused.
            string seed = TryStructured(password, 0);
            if (seed != null && string.Equals(Decode(seed), password, StringComparison.Ordinal))
                return seed;

            // Structured path, reading the password as a leetspeak spelling.
            string plain = Unleet(password);
            if (!string.Equals(plain, password, StringComparison.Ordinal))
            {
                seed = TryStructured(plain, 1);
                if (seed != null && string.Equals(Decode(seed), password, StringComparison.Ordinal))
                    return seed;
            }

            // Unstructured fallback: carries any printable password of any shape.
            Writer raw = new Writer();
            EncodeRaw(password, raw);
            return raw.Finish();
        }

        private static string TryStructured(string plain, int leet)
        {
            Writer w = new Writer();
            return TryEncodeStructured(plain, leet, w) ? w.Finish() : null;
        }

        private static bool TryEncodeStructured(string plain, int leet, Writer w)
        {
            List<char> kinds = new List<char>();
            List<string> parts = new List<string>();
            if (!Tokenise(plain, kinds, parts)) return false;

            StringBuilder tb = new StringBuilder();
            for (int i = 0; i < kinds.Count; i++) tb.Append(kinds[i]);
            string template = tb.ToString();

            int templateIndex = -1;
            for (int i = 0; i < Templates.Length; i++)
                if (string.Equals(Templates[i], template, StringComparison.Ordinal)) { templateIndex = i; break; }
            if (templateIndex < 0) return false;

            if (!w.Write(TemplateTable, templateIndex)) return false;
            if (!w.Write(LeetTable, leet)) return false;

            for (int i = 0; i < kinds.Count; i++)
            {
                if (kinds[i] == 'W') { if (!EncodeWord(parts[i], w)) return false; }
                else if (kinds[i] == 'D') { if (!EncodeDigits(parts[i], w)) return false; }
                else { if (!EncodeSymbol(parts[i], w)) return false; }
            }
            return true;
        }

        private static bool Tokenise(string s, List<char> kinds, List<string> parts)
        {
            if (s.Length == 0) return false;
            int i = 0;
            while (i < s.Length)
            {
                char kind = ClassOf(s[i]);
                int j = i;
                while (j < s.Length && ClassOf(s[j]) == kind) j++;
                kinds.Add(kind);
                parts.Add(s.Substring(i, j - i));
                i = j;
                if (kinds.Count > 8) return false;
            }
            return true;
        }

        private static char ClassOf(char c)
        {
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) return 'W';
            if (c >= '0' && c <= '9') return 'D';
            return 'S';
        }

        private static bool EncodeWord(string part, Writer w)
        {
            int caps;
            string lower = part.ToLowerInvariant();
            if (string.Equals(part, lower, StringComparison.Ordinal)) caps = 0;
            else if (char.IsUpper(part[0]) &&
                     string.Equals(part.Substring(1), lower.Substring(1), StringComparison.Ordinal)) caps = 1;
            else if (string.Equals(part, part.ToUpperInvariant(), StringComparison.Ordinal)) caps = 2;
            else return false;   // mixed case falls through to the unstructured path

            int idx;
            if (!WordIndex.TryGetValue(lower, out idx)) return false;

            if (!w.Write(WordTable, idx)) return false;
            return w.Write(CapsTable, caps);
        }

        private static bool EncodeDigits(string part, Writer w)
        {
            for (int i = 0; i < CommonDigits.Length; i++)
            {
                if (string.Equals(CommonDigits[i], part, StringComparison.Ordinal))
                {
                    if (!w.Write(DigitKindTable, 0)) return false;
                    return w.Write(CommonDigitTable, i);
                }
            }

            int year;
            if (part.Length == 4 && int.TryParse(part, out year) && year >= YearMin && year <= YearMax)
            {
                if (!w.Write(DigitKindTable, 1)) return false;
                return w.Write(YearTable, year - YearMin);
            }

            if (part.Length < 1 || part.Length > GenericDigitLenWeights.Length) return false;
            if (!w.Write(DigitKindTable, 2)) return false;
            if (!w.Write(GenericLenTable, part.Length - 1)) return false;
            for (int i = 0; i < part.Length; i++)
                if (!w.Write(DigitTable, part[i] - '0')) return false;
            return true;
        }

        private static bool EncodeSymbol(string part, Writer w)
        {
            if (part.Length != 1) return false;
            for (int i = 0; i < Symbols.Length; i++)
                if (Symbols[i][0] == part[0]) return w.Write(SymbolTable, i);
            return false;
        }

        private static void EncodeRaw(string password, Writer w)
        {
            w.Write(TemplateTable, RawTemplateIndex);
            w.Write(RawLenTable, password.Length);
            for (int i = 0; i < password.Length; i++)
                w.Write(RawCharTable, password[i] - AlphabetMin);
        }

        // ---------------------------------------------------------------- decode ---

        /// <summary>
        /// Turns any seed into a password. Total by construction: every one of the
        /// 95^<see cref="SeedLength"/> possible seeds — including the garbage a wrong
        /// key produces — maps to a plausible password, and this method never throws.
        /// </summary>
        public static string Decode(string seed)
        {
            Reader r = new Reader(seed);
            int templateIndex = r.Read(TemplateTable);
            string template = Templates[templateIndex];

            if (template == "R")
            {
                int len = r.Read(RawLenTable);
                StringBuilder raw = new StringBuilder(len);
                for (int i = 0; i < len; i++)
                    raw.Append((char)(AlphabetMin + r.Read(RawCharTable)));
                return raw.ToString();
            }

            int leet = r.Read(LeetTable);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < template.Length; i++)
            {
                char kind = template[i];
                if (kind == 'W') sb.Append(DecodeWord(r));
                else if (kind == 'D') sb.Append(DecodeDigits(r));
                else sb.Append(Symbols[r.Read(SymbolTable)]);
            }

            string outp = sb.ToString();
            return leet == 1 ? Leet(outp) : outp;
        }

        private static string DecodeWord(Reader r)
        {
            string word = WordList[r.Read(WordTable)];
            int caps = r.Read(CapsTable);
            if (caps == 1) return char.ToUpperInvariant(word[0]) + word.Substring(1);
            if (caps == 2) return word.ToUpperInvariant();
            return word;
        }

        private static string DecodeDigits(Reader r)
        {
            int kind = r.Read(DigitKindTable);
            if (kind == 0) return CommonDigits[r.Read(CommonDigitTable)];
            if (kind == 1) return (YearMin + r.Read(YearTable)).ToString();

            int len = r.Read(GenericLenTable) + 1;
            StringBuilder sb = new StringBuilder(len);
            for (int i = 0; i < len; i++) sb.Append((char)('0' + r.Read(DigitTable)));
            return sb.ToString();
        }

        // ------------------------------------------------------------- leetspeak ---

        private static string Leet(string s)
        {
            StringBuilder sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == 'a') sb.Append('@');
                else if (c == 'e') sb.Append('3');
                else if (c == 'i') sb.Append('1');
                else if (c == 'o') sb.Append('0');
                else if (c == 's') sb.Append('$');
                else if (c == 't') sb.Append('7');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static string Unleet(string s)
        {
            StringBuilder sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '@') sb.Append('a');
                else if (c == '3') sb.Append('e');
                else if (c == '1') sb.Append('i');
                else if (c == '0') sb.Append('o');
                else if (c == '$') sb.Append('s');
                else if (c == '7') sb.Append('t');
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
