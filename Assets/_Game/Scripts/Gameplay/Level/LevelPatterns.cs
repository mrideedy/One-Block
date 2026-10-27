using System.Collections.Generic;

namespace OneBlock.Gameplay
{
    /// <summary>
    /// Hand-authored level chunks. Each pattern is a space-separated token string read left to right:
    /// <list type="bullet">
    /// <item><c>F3</c> flat ×3 (decor allowed) — <c>C5</c> flat ×5 with a coin line</item>
    /// <item><c>G2</c> bottomless gap ×2 (coins over it) — cost 2</item>
    /// <item><c>S2</c> spikes ×2 on the ground — cost 2</item>
    /// <item><c>U</c> step up 1 (free hop) — <c>D2</c> drop down 2 (free)</item>
    /// <item><c>W3x4</c> plateau 3 high, 4 long, then back down — cost 2 (one pillar per extra row)</item>
    /// <item><c>T2</c> permanent climb of 2 — cost 1</item>
    /// <item><c>E5</c> flat ×5 patrolled by an enemy — cost 1 (squash it)</item>
    /// <item><c>L</c> elevator over a 3-wide pit — free, costs time</item>
    /// </list>
    /// Costs assume perfect play; the generator's block ledger uses them to keep levels fair.
    /// </summary>
    public static class LevelPatterns
    {
        public sealed class Pattern
        {
            public string Name;
            public int MinMetres;
            public float Weight = 1f;
            public string Tokens;
            public bool Challenge = true;   // allowed in One Block Challenge
        }

        public static readonly IReadOnlyList<Pattern> All = new List<Pattern>
        {
            // Tier 0 — learning the tap (0 m+)
            P("single gap",        0, 3.0f, "F1 G1 F2"),
            P("step up",           0, 1.2f, "F1 U F2"),
            P("drop",              0, 1.0f, "F1 D1 F2"),
            P("coin run",          0, 1.4f, "C6"),
            P("low wall",          0, 1.6f, "F1 W2x4 F1"),
            P("single spike",      0, 1.6f, "F1 S1 F2"),

            // Tier 1 — combinations (120 m+)
            P("double gap",      120, 2.2f, "F1 G2 F2"),
            P("double spikes",   120, 1.4f, "F1 S2 F2"),
            P("stairs",          120, 1.0f, "U F1 U F2"),
            P("climb",           120, 1.0f, "F1 T2 F2"),
            P("gap pair",        120, 1.2f, "G1 F2 G1 F1"),
            P("enemy",           150, 1.6f, "F1 E6"),
            P("elevator",        180, 1.2f, "F1 L F2", challenge: true),
            P("big drop",        150, 0.8f, "F1 D2 F3 G1 F1"),   // landing room: a 2-row fall carries ~2 cells at speed

            // Tier 2 — pressure (350 m+)
            P("triple gap",      350, 1.6f, "F1 G3 F2", challenge: false),
            P("tall wall",       350, 1.4f, "F1 W3x4 F1", challenge: false),
            P("spike hop",       350, 1.2f, "S1 F1 S1 F1"),
            P("gap into wall",   350, 1.2f, "G2 W2x3 F1", challenge: false),
            P("enemy then gap",  400, 1.2f, "E5 G1 F1"),
            P("drop and gap",    350, 1.0f, "D2 F3 G2 F1"),
            P("spike strip",     400, 1.0f, "F1 S3 F1", challenge: false),
            P("elevator gap",    400, 0.9f, "L F1 G1 F1"),

            // Tier 3 — expert (700 m+)
            P("gap chain",       700, 1.3f, "G2 F1 G2 F1", challenge: false),
            P("big climb",       700, 1.0f, "F1 T3 F1", challenge: false),
            P("spikes to gap",   700, 1.1f, "S2 F1 G2 F1", challenge: false),
            P("guarded spikes",  750, 1.0f, "E4 S2 F1", challenge: false),
            P("fortress",        800, 0.9f, "W3x3 G2 F1", challenge: false),
            P("double elevator", 800, 0.8f, "L F1 L F1"),
        };

        static Pattern P(string name, int minMetres, float weight, string tokens, bool challenge = true) =>
            new Pattern { Name = name, MinMetres = minMetres, Weight = weight, Tokens = tokens, Challenge = challenge };

        public readonly struct Token
        {
            public readonly char Op;
            public readonly int A;
            public readonly int B;

            public Token(char op, int a, int b)
            {
                Op = op;
                A = a;
                B = b;
            }
        }

        static readonly Dictionary<string, Token[]> Cache = new();

        public static Token[] Parse(string tokens)
        {
            if (Cache.TryGetValue(tokens, out var cached)) return cached;
            var parts = tokens.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var result = new Token[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string s = parts[i];
                char op = s[0];
                int a = 1, b = 0;
                if (s.Length > 1)
                {
                    int x = s.IndexOf('x');
                    if (x > 0)
                    {
                        a = int.Parse(s.Substring(1, x - 1));
                        b = int.Parse(s.Substring(x + 1));
                    }
                    else a = int.Parse(s.Substring(1));
                }
                result[i] = new Token(op, a, b);
            }
            Cache[tokens] = result;
            return result;
        }
    }
}
