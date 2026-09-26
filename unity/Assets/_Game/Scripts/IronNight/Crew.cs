using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// The leader's crew, seat by seat: gunner, loader, driver, radio operator. Each nation has three men for every seat,
    /// each with his own gift, and an ace (a woman with two gifts in one, dearer, listed once her portrait is in); the first of each serves from the start, the others are taken on for points. A man trains
    /// through five levels with crew experience (Depot.CrewXp), each making his gift stronger, and keeps them if he leaves the seat. The commander keeps his
    /// own place (Depot.Commanders); the crew's nights together still count as they did (Depot.CrewNights).
    /// </summary>
    public static class Crew
    {
        public class Man { public string id, nation, role, name, perk, gift, perk2, gift2; public int cost; }
        public static readonly string[] Roles = { "gunner", "loader", "driver", "radio" };
        public static readonly int[] TrainCosts = { 0, 300, 600, 1000, 1500 };   // from level 1 to 2, 2 to 3, 3 to 4, 4 to 5
        public const int MaxLevel = 5;

        public static readonly Man[] Men =
        {
            // the United States
            new Man { id = "us_gunner_1", nation = "us", role = "gunner", name = "Pvt. Dale Whitaker", perk = "steady", gift = "Steady hand", cost = 0 },
            new Man { id = "us_gunner_2", nation = "us", role = "gunner", name = "Cpl. Ike Thompson", perk = "eagle", gift = "Eagle eye", cost = 1000 },
            new Man { id = "us_gunner_3", nation = "us", role = "gunner", name = "Sgt. Ray Delgado", perk = "snap", gift = "Snap shot", cost = 1200 },
            new Man { id = "us_loader_1", nation = "us", role = "loader", name = "Cpl. Frank Moreno", perk = "hands", gift = "Quick hands", cost = 0 },
            new Man { id = "us_loader_2", nation = "us", role = "loader", name = "Pvt. Billy Hargrove", perk = "racks", gift = "Ammo handler", cost = 1000 },
            new Man { id = "us_loader_3", nation = "us", role = "loader", name = "Pvt. Lou Ferrante", perk = "he", gift = "HE man", cost = 1200 },
            new Man { id = "us_driver_1", nation = "us", role = "driver", name = "Pvt. Eddie Kowalczyk", perk = "foot", gift = "Lead foot", cost = 0 },
            new Man { id = "us_driver_2", nation = "us", role = "driver", name = "T/4 Andy Novak", perk = "mech", gift = "Mechanic", cost = 1000 },
            new Man { id = "us_driver_3", nation = "us", role = "driver", name = "T/5 Hank Beaumont", perk = "rough", gift = "Rough rider", cost = 1200 },
            new Man { id = "us_radio_1", nation = "us", role = "radio", name = "T/5 Sam Ruiz", perk = "bow", gift = "Bow gunner", cost = 0 },
            new Man { id = "us_radio_2", nation = "us", role = "radio", name = "Pvt. Tom Whitehorse", perk = "signals", gift = "Signaller", cost = 1000 },
            new Man { id = "us_radio_3", nation = "us", role = "radio", name = "Cpl. Al Brennan", perk = "spot", gift = "Spotter", cost = 1200 },
            // the Soviet Union
            new Man { id = "su_gunner_1", nation = "su", role = "gunner", name = "Ryad. Vasya Petrov", perk = "steady", gift = "Steady hand", cost = 0 },
            new Man { id = "su_gunner_2", nation = "su", role = "gunner", name = "Serzh. Tolya Zaytsev", perk = "eagle", gift = "Eagle eye", cost = 1000 },
            new Man { id = "su_gunner_3", nation = "su", role = "gunner", name = "Ml. serzh. Pavel Rudnev", perk = "snap", gift = "Snap shot", cost = 1200 },
            new Man { id = "su_loader_1", nation = "su", role = "loader", name = "Ml. serzh. Kolya Nikitin", perk = "hands", gift = "Quick hands", cost = 0 },
            new Man { id = "su_loader_2", nation = "su", role = "loader", name = "Ryad. Misha Krylov", perk = "racks", gift = "Ammo handler", cost = 1000 },
            new Man { id = "su_loader_3", nation = "su", role = "loader", name = "Yefr. Stepan Gorbunov", perk = "he", gift = "HE man", cost = 1200 },
            new Man { id = "su_driver_1", nation = "su", role = "driver", name = "Ryad. Sasha Denisov", perk = "foot", gift = "Lead foot", cost = 0 },
            new Man { id = "su_driver_2", nation = "su", role = "driver", name = "Yefr. Lena Sokolova", perk = "mech", gift = "Mechanic", cost = 1000 },
            new Man { id = "su_driver_3", nation = "su", role = "driver", name = "Serzh. Ivan Kolesnik", perk = "rough", gift = "Rough rider", cost = 1200 },
            new Man { id = "su_radio_1", nation = "su", role = "radio", name = "Yefr. Grisha Volkov", perk = "bow", gift = "Bow gunner", cost = 0 },
            new Man { id = "su_radio_2", nation = "su", role = "radio", name = "Ryad. Yura Belyaev", perk = "signals", gift = "Signaller", cost = 1000 },
            new Man { id = "su_radio_3", nation = "su", role = "radio", name = "Serzh. Arkady Frolov", perk = "spot", gift = "Spotter", cost = 1200 },
            // the aces: a woman in every seat, two gifts in one
            new Man { id = "us_gunner_4", nation = "us", role = "gunner", name = "Sgt. Maggie O'Hara", perk = "steady", gift = "Steady hand", perk2 = "eagle", gift2 = "Eagle eye", cost = 3000 },
            new Man { id = "us_loader_4", nation = "us", role = "loader", name = "Cpl. Rosie Kowalski", perk = "hands", gift = "Quick hands", perk2 = "racks", gift2 = "Ammo handler", cost = 3000 },
            new Man { id = "us_driver_4", nation = "us", role = "driver", name = "T/5 Dolly Reyes", perk = "foot", gift = "Lead foot", perk2 = "rough", gift2 = "Rough rider", cost = 3000 },
            new Man { id = "us_radio_4", nation = "us", role = "radio", name = "Pvt. Grace Holloway", perk = "signals", gift = "Signaller", perk2 = "spot", gift2 = "Spotter", cost = 3000 },
            new Man { id = "su_gunner_4", nation = "su", role = "gunner", name = "Ml. serzh. Anya Voronova", perk = "eagle", gift = "Eagle eye", perk2 = "snap", gift2 = "Snap shot", cost = 3000 },
            new Man { id = "su_loader_4", nation = "su", role = "loader", name = "Ryad. Katya Smirnova", perk = "hands", gift = "Quick hands", perk2 = "he", gift2 = "HE expert", cost = 3000 },
            new Man { id = "su_driver_4", nation = "su", role = "driver", name = "Serzh. Nadya Kuznetsova", perk = "foot", gift = "Lead foot", perk2 = "mech", gift2 = "Mechanic", cost = 3000 },
            new Man { id = "su_radio_4", nation = "su", role = "radio", name = "Ml. serzh. Zoya Petrenko", perk = "bow", gift = "Bow gunner", perk2 = "spot", gift2 = "Spotter", cost = 3000 },
        };

        public static string RoleName(string role) => role == "gunner" ? "Gunner" : role == "loader" ? "Loader" : role == "driver" ? "Driver" : "Radio operator";
        public static string RolePlural(string role) => role == "gunner" ? "Gunners" : role == "loader" ? "Loaders" : role == "driver" ? "Drivers" : "Radio operators";

        /// <summary>What a gift does at a level.</summary>
        public static string Effect(string perk, int l)
        {
            switch (perk)
            {
                case "steady": return "+" + 3 * l + "% damage";
                case "eagle": return "sees " + 3 * l + "% farther";
                case "snap": return "the turret turns " + 8 * l + "% faster";
                case "hands": return "loads " + 3 * l + "% faster";
                case "racks": return "+" + 3 * l + " AP and +" + l + " HE in the racks";
                case "he": return "HE bursts " + (0.4f * l).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " m wider";
                case "foot": return "drives " + 3 * l + "% faster";
                case "mech": return "a thrown track back on in " + (10f * (1f - 0.15f * l)).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " s";
                case "rough": return "loses " + 12 * l + "% less speed going through things";
                case "bow": return "the bow gun hits " + 5 * l + "% more often and reaches " + 2 * l + " m farther";
                case "signals": return "air strikes every " + (60 - 6 * l) + " s";
                case "spot": return "the radar reaches " + 8 * l + "% farther";
                default: return "";
            }
        }

        /// <summary>What a gift does at a level, in a few words: an ace's two gifts share one line.</summary>
        public static string Short(string perk, int l)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            switch (perk)
            {
                case "steady": return "+" + 3 * l + "% damage";
                case "eagle": return "+" + 3 * l + "% sight";
                case "snap": return "turret +" + 8 * l + "%";
                case "hands": return "reload +" + 3 * l + "%";
                case "racks": return "+" + 3 * l + " AP, +" + l + " HE";
                case "he": return "HE +" + (0.4f * l).ToString("0.#", inv) + " m";
                case "foot": return "+" + 3 * l + "% speed";
                case "mech": return "tracks in " + (10f * (1f - 0.15f * l)).ToString("0.#", inv) + " s";
                case "rough": return "-" + 12 * l + "% drag";
                case "bow": return "bow gun +" + 5 * l + "%";
                case "signals": return "air every " + (60 - 6 * l) + " s";
                case "spot": return "radar +" + 8 * l + "%";
                default: return "";
            }
        }
        public static bool Ace(Man m) => m.perk2 != null;
        public static string GiftName(Man m) => Ace(m) ? m.gift + " + " + m.gift2 : m.gift;
        public static string EffectOf(Man m, int l) => Ace(m) ? Short(m.perk, l) + " · " + Short(m.perk2, l) : Effect(m.perk, l);
        /// <summary>An ace is listed only once her own portrait is in.</summary>
        public static bool Seen(Man m) => !Ace(m) || Hud.UiSprite("crew_" + m.id) != null;

        static string K(string id, string what) => "crewman." + id + "." + what;
        public static Man Find(string id) { foreach (var m in Men) if (m.id == id) return m; return null; }
        public static Man First(string nation, string role) { foreach (var m in Men) if (m.nation == nation && m.role == role && m.cost == 0) return m; return null; }
        public static bool Owns(Man m) => m.cost == 0 || PlayerPrefs.GetInt(K(m.id, "hired"), 0) == 1;
        public static int Level(Man m) => Mathf.Clamp(PlayerPrefs.GetInt(K(m.id, "level"), 1), 1, MaxLevel);
        /// <summary>The next level's price, 0 at the top.</summary>
        public static int NextTrain(Man m) { int l = Level(m); return l >= MaxLevel ? 0 : TrainCosts[l]; }

        /// <summary>The man in the seat: the one picked, if he is still on the books, else the first of the seat.</summary>
        public static Man Chosen(string nation, string role)
        {
            var m = Find(PlayerPrefs.GetString("crew.pick." + nation + "." + role, ""));
            return m != null && m.nation == nation && m.role == role && Owns(m) ? m : First(nation, role);
        }
        public static bool InSeat(Man m) => Chosen(m.nation, m.role) == m;

        /// <summary>Takes a man on if he is not yet (for points) and puts him in his seat.</summary>
        public static bool Pick(Man m)
        {
            if (!Owns(m)) { if (!Depot.Spend(m.cost)) return false; PlayerPrefs.SetInt(K(m.id, "hired"), 1); }
            PlayerPrefs.SetString("crew.pick." + m.nation + "." + m.role, m.id); PlayerPrefs.Save(); return true;
        }

        /// <summary>A level of training, for crew experience.</summary>
        public static bool Train(Man m)
        {
            int c = NextTrain(m); if (c <= 0 || !Owns(m) || !Depot.SpendCrewXp(c)) return false;
            PlayerPrefs.SetInt(K(m.id, "level"), Level(m) + 1); PlayerPrefs.Save(); return true;
        }

        /// <summary>The level of the man in the nation's crew who has this gift; 0 when none of them has it.</summary>
        public static int Gift(string nation, string perk) { foreach (var r in Roles) { var m = Chosen(nation, r); if (m != null && (m.perk == perk || m.perk2 == perk)) return Level(m); } return 0; }

        /// <summary>A man's portrait: his own when there is one, else his seat's.</summary>
        public static string Portrait(Man m) => Hud.UiSprite("crew_" + m.id) != null ? "crew_" + m.id : "crew_" + m.nation + "_" + m.role;
    }
}
