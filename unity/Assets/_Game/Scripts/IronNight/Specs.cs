using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// One vehicle class: which generated meshes it uses, where its turret ring sits, its gun, and the numbers the fight
    /// runs on. Distances are metres, speeds metres per second, times seconds. The meshes come from art/models/parts
    /// (TRELLIS 2 renders split into hull and turret; see art/pipeline/README.md), pivot at the turret ring.
    /// </summary>
    public class VehicleSpec
    {
        public string id, name, hullMesh, turretMesh, texture, turretTexture;   // turretTexture: a turret generated on its own has its own atlas
        public float forward = 1f;      // +1 when the mesh's nose points +Z, -1 when it points -Z
        public float ringHeight;        // metres from the ground to the turret ring (the mesh origin)
        public float gunLength, gunRadius, gunHeight, mantlet, gunX, turretShift, meshYaw; public bool muzzleBrake;   // meshYaw: degrees the model is turned so its barrel points +Z (TRELLIS guns come out diagonal)   // turretShift: the generated turret centred on the ring (game X: Unity mirrors the OBJ); gunX: the gun across the turret
        public Color tint = Color.white;
        public float speed, turnRate, turretRate, reload, damage, range, hp, radius;
        public bool isGun;              // PaK, 88: no turret, aims with its body, never moves
        public bool casemate;           // StuG: no turret, aims by turning the hull, drives
        public bool transport;          // half-track: no gun, brings a squad of tank hunters and leaves
        public Vector3 muzzle = new Vector3(0f, 1.1f, 3.4f);   // where the shell leaves a turretless vehicle
        public float scale = 1f;        // the mesh scaled up: the 88 is the PaK mesh at 1.45
        public bool painted;            // the markings are in the generated texture already: no star quads
        public Vector3 hatch;           // the commander's hatch on the turret (game coordinates about the ring axis); zero: by the turret's shape
        public bool crewed;             // the model carries its own commander figure

        public static readonly VehicleSpec Sherman = new VehicleSpec
        {
            id = "sherman", name = "M4 Sherman", hullMesh = "sherman_hull", turretMesh = "sherman_turret", texture = "sherman", painted = true, hatch = new Vector3(-0.3f, 0f, 0.05f), forward = 1f, ringHeight = 1.8f,
            gunLength = 0f, gunHeight = 0.5f, muzzle = new Vector3(0f, 0.3f, 3.03f),
            speed = 9f, turnRate = 2.2f, turretRate = 3.2f, reload = 1.3f, damage = 1f, range = 26f, hp = 3f, radius = 2.4f
        };
        public static readonly VehicleSpec Firefly = new VehicleSpec
        {
            id = "firefly", name = "Sherman Firefly", hullMesh = "firefly_hull", turretMesh = "firefly_turret", texture = "firefly", painted = true, forward = 1f, ringHeight = 1.98f,
            gunLength = 3.6f, gunRadius = 0.075f, gunHeight = 0.45f, mantlet = 1.7f, turretShift = 0.06f, muzzleBrake = true, tint = new Color(0.95f, 0.95f, 0.88f),
            speed = 8.5f, turnRate = 2f, turretRate = 2.8f, reload = 1.6f, damage = 2f, range = 30f, hp = 3f, radius = 2.4f
        };
        public static readonly VehicleSpec PanzerIV = new VehicleSpec
        {
            id = "pz4", name = "Panzer IV", hullMesh = "pz4_hull", turretMesh = "pz4_turret", texture = "pz4", painted = true, hatch = new Vector3(0f, 0f, -0.55f), forward = 1f, ringHeight = 1.86f,
            gunLength = 0f, gunHeight = 0.4f, muzzle = new Vector3(0f, 0.26f, 4.57f),
            speed = 8f, turnRate = 1.9f, turretRate = 2.6f, reload = 1.7f, damage = 1f, range = 24f, hp = 3f, radius = 2.4f
        };
        public static readonly VehicleSpec Tiger = new VehicleSpec
        {
            id = "tiger", name = "Tiger I", hullMesh = "tiger_hull", turretMesh = "tiger_turret", texture = "tiger", forward = 1f, ringHeight = 2.249f,
            gunLength = 3.9f, gunRadius = 0.085f, gunHeight = 0.45f, mantlet = 1.5f, muzzleBrake = true, tint = new Color(0.95f, 0.96f, 1f),
            speed = 6f, turnRate = 1.4f, turretRate = 2f, reload = 2.6f, damage = 2f, range = 30f, hp = 8f, radius = 2.8f
        };
        public static readonly VehicleSpec TigerAce = new VehicleSpec
        {
            id = "tigerace", name = "Tiger Ace", hullMesh = "tiger_hull", turretMesh = "tiger_turret", texture = "tiger", forward = 1f, ringHeight = 2.249f,
            gunLength = 3.9f, gunRadius = 0.085f, gunHeight = 0.45f, mantlet = 1.5f, muzzleBrake = true, tint = new Color(0.7f, 0.7f, 0.74f),
            speed = 5.5f, turnRate = 1.3f, turretRate = 2.2f, reload = 1.9f, damage = 2f, range = 34f, hp = 30f, radius = 2.9f
        };
        public static readonly VehicleSpec Pak40 = new VehicleSpec
        {
            id = "pak40", name = "PaK 40", hullMesh = "pak40_hull", turretMesh = null, texture = "pak40", forward = 1f, ringHeight = 0f, scale = 0.72f, meshYaw = -47.9f, muzzle = new Vector3(0f, 1.72f, 3.15f),
            gunLength = 0f, tint = new Color(0.95f, 0.96f, 1f),
            speed = 0f, turnRate = 1.4f, turretRate = 1.4f, reload = 2.2f, damage = 1f, range = 32f, hp = 2f, radius = 1.5f, isGun = true
        };

        public static readonly VehicleSpec Flak88 = new VehicleSpec
        {
            id = "flak88", name = "8.8 cm Flak", hullMesh = "flak88_hull", turretMesh = null, texture = "flak88", forward = 1f, ringHeight = 0f, scale = 0.6f, meshYaw = 41.3f, muzzle = new Vector3(0f, 5.39f, 4.83f),
            gunLength = 0f, tint = new Color(1f, 1f, 1f),
            speed = 0f, turnRate = 0.9f, turretRate = 0.9f, reload = 3.2f, damage = 2f, range = 40f, hp = 4f, radius = 2.0f, isGun = true
        };
        public static readonly VehicleSpec Panther = new VehicleSpec
        {
            id = "panther", name = "Panther", hullMesh = "panther_hull", turretMesh = "panther_turret", texture = "panther", forward = 1f, ringHeight = 1.92f,
            gunLength = 4.3f, gunRadius = 0.075f, gunHeight = 0.5f, mantlet = 1.5f, muzzleBrake = true, tint = new Color(0.95f, 0.96f, 1f),
            speed = 8f, turnRate = 1.6f, turretRate = 2.2f, reload = 2.2f, damage = 2f, range = 32f, hp = 6f, radius = 2.8f
        };
        public static readonly VehicleSpec StuG = new VehicleSpec
        {
            id = "stug", name = "StuG III", hullMesh = "stug_hull", turretMesh = null, texture = "stug", forward = 1f, ringHeight = 0f, muzzle = new Vector3(0f, 1.9f, 4.2f), casemate = true,
            gunLength = 0f, tint = new Color(0.95f, 0.96f, 1f),
            speed = 8.5f, turnRate = 1.8f, turretRate = 1.8f, reload = 2f, damage = 2f, range = 30f, hp = 4f, radius = 2.5f
        };
        public static readonly VehicleSpec Halftrack = new VehicleSpec
        {
            id = "halftrack", name = "Sd.Kfz. 251", hullMesh = "halftrack_hull", turretMesh = null, texture = "halftrack", forward = 1f, ringHeight = 0f, transport = true,
            gunLength = 0f, tint = new Color(0.95f, 0.96f, 1f),
            speed = 12f, turnRate = 2.4f, turretRate = 2.4f, reload = 999f, damage = 0f, range = 0f, hp = 2f, radius = 2.4f
        };

        // ---- the American tree ----
        public static readonly VehicleSpec Easy8 = new VehicleSpec
        {
            id = "easy8", name = "M4A3E8 Easy Eight", hullMesh = "easy8_hull", turretMesh = "easy8_turret", texture = "easy8", painted = true, forward = 1f, ringHeight = 2.19f,
            gunLength = 3.9f, gunRadius = 0.075f, gunHeight = 0.5f, mantlet = 1.7f, turretShift = 0.09f, muzzleBrake = true, tint = new Color(0.95f, 0.95f, 0.88f),
            speed = 9.5f, turnRate = 2.2f, turretRate = 3f, reload = 1.2f, damage = 1.5f, range = 28f, hp = 4f, radius = 2.5f
        };
        public static readonly VehicleSpec Hellcat = new VehicleSpec
        {
            id = "hellcat", name = "M18 Hellcat", hullMesh = "hellcat_hull", turretMesh = "hellcat_turret", texture = "hellcat", painted = true, forward = 1f, ringHeight = 1.71f,
            gunLength = 3.9f, gunRadius = 0.075f, gunHeight = 0.5f, mantlet = 1.45f, turretShift = 0.23f, muzzleBrake = true, tint = new Color(0.95f, 0.95f, 0.88f),
            speed = 13f, turnRate = 2.8f, turretRate = 3.4f, reload = 1.4f, damage = 1.6f, range = 30f, hp = 2f, radius = 2.4f
        };
        public static readonly VehicleSpec Chaffee = new VehicleSpec
        {
            id = "chaffee", name = "M24 Chaffee", hullMesh = "chaffee_hull", turretMesh = "chaffee_turret", texture = "chaffee", painted = true, hatch = new Vector3(0.1f, 0f, 0.1f), forward = 1f, ringHeight = 1.76f,
            gunLength = 0f, gunHeight = 0.4f, muzzle = new Vector3(0f, 0.07f, 3.43f),
            speed = 11.5f, turnRate = 2.9f, turretRate = 3.6f, reload = 1f, damage = 0.7f, range = 24f, hp = 2f, radius = 2.2f
        };
        public static readonly VehicleSpec Pershing = new VehicleSpec
        {
            id = "pershing", name = "M26 Pershing", hullMesh = "pershing_hull", turretMesh = "pershing_turret", texture = "pershing", painted = true, crewed = true, forward = 1f, ringHeight = 1.53f,
            gunLength = 0f, gunHeight = 0.5f, muzzle = new Vector3(0f, 0.43f, 5.41f),
            speed = 7.5f, turnRate = 1.7f, turretRate = 2.4f, reload = 1.9f, damage = 2.5f, range = 32f, hp = 6f, radius = 2.8f
        };
        public static readonly VehicleSpec M10 = new VehicleSpec
        {
            id = "m10", name = "M10 Wolverine", hullMesh = "m10_hull", turretMesh = "m10_turret", texture = "m10", painted = true, forward = 1f, ringHeight = 1.9f,
            gunLength = 3.6f, gunRadius = 0.075f, gunHeight = 0.6f, mantlet = 1.9f, turretShift = -0.06f, tint = new Color(0.95f, 0.95f, 0.88f),
            speed = 9f, turnRate = 2.1f, turretRate = 2.6f, reload = 1.5f, damage = 1.5f, range = 28f, hp = 2.5f, radius = 2.5f
        };
        // ---- the Soviet tree ----
        public static readonly VehicleSpec T34_85 = new VehicleSpec
        {
            id = "t34_85", painted = true, hatch = new Vector3(0.1f, 0f, 0.1f), name = "T-34-85", hullMesh = "t34_85_hull", turretMesh = "t34_85_turret", texture = "t34_85", forward = 1f, ringHeight = 1.61f,
            gunLength = 0f, gunHeight = 0.5f, muzzle = new Vector3(0f, 0.27f, 4.56f),
            speed = 10f, turnRate = 2.4f, turretRate = 3f, reload = 1.5f, damage = 1.5f, range = 27f, hp = 3.5f, radius = 2.5f
        };
        public static readonly VehicleSpec KV85 = new VehicleSpec
        {
            id = "kv85", painted = true, hatch = new Vector3(0f, 0f, -0.2f), name = "KV-1", hullMesh = "kv1_hull", turretMesh = "kv1_turret", texture = "kv1", forward = 1f, ringHeight = 1.96f,
            gunLength = 0f, gunHeight = 0.5f, muzzle = new Vector3(0f, 0.21f, 3.06f),
            speed = 6.5f, turnRate = 1.5f, turretRate = 2.2f, reload = 1.8f, damage = 1.5f, range = 27f, hp = 6f, radius = 2.7f
        };
        public static readonly VehicleSpec SU100 = new VehicleSpec
        {
            id = "su100", painted = true, name = "SU-100", hullMesh = "su100_hull", turretMesh = null, texture = "su100", forward = 1f, ringHeight = 0f,
            gunLength = 0f, gunRadius = 0f, gunHeight = 0f, mantlet = 0f,
            speed = 9f, turnRate = 2f, turretRate = 2f, reload = 1.8f, damage = 2.6f, range = 32f, hp = 3f, radius = 2.5f, casemate = true, scale = 1f, muzzle = new Vector3(-0.16f, 1.6f, 5.99f)
        };
        public static readonly VehicleSpec IS2 = new VehicleSpec
        {
            id = "is2", painted = true, hatch = new Vector3(-0.3f, 0f, -0.35f), name = "IS-2", hullMesh = "is2_hull", turretMesh = "is2_turret", texture = "is2", forward = 1f, ringHeight = 1.73f,
            gunLength = 0f, gunHeight = 0.5f, muzzle = new Vector3(0f, 0.14f, 5.81f),
            speed = 6.5f, turnRate = 1.4f, turretRate = 2f, reload = 2.8f, damage = 3.5f, range = 32f, hp = 7f, radius = 2.9f
        };
        // ---- more Germans ----
        public static readonly VehicleSpec Hetzer = new VehicleSpec
        {
            id = "hetzer", name = "Hetzer", hullMesh = "hetzer_hull", turretMesh = null, texture = "hetzer", forward = 1f, ringHeight = 0f,
            gunLength = 0f, gunRadius = 0f, gunHeight = 0f, mantlet = 0f, tint = new Color(0.95f, 0.96f, 1f),
            speed = 7f, turnRate = 1.8f, turretRate = 1.8f, reload = 1.9f, damage = 1.5f, range = 26f, hp = 3f, radius = 2.2f, casemate = true, muzzle = new Vector3(0.15f, 1.5f, 3.2f)
        };
        public static readonly VehicleSpec KingTiger = new VehicleSpec
        {
            id = "kingtiger", name = "King Tiger", hullMesh = "kingtiger_hull", turretMesh = "kingtiger_turret", texture = "kingtiger", forward = 1f, ringHeight = 2.13f,
            gunLength = 5.8f, gunRadius = 0.09f, gunHeight = 0.55f, mantlet = 1.8f, turretShift = 0.16f, muzzleBrake = true, tint = new Color(0.95f, 0.96f, 1f),
            speed = 5f, turnRate = 1.2f, turretRate = 1.8f, reload = 2.4f, damage = 3f, range = 36f, hp = 40f, radius = 3.2f
        };
        public static readonly VehicleSpec Nebelwerfer = new VehicleSpec
        {
            id = "nebelwerfer", name = "Nebelwerfer", hullMesh = "nebelwerfer_hull", turretMesh = null, texture = "nebelwerfer", forward = 1f, ringHeight = 0f,
            gunLength = 0f, gunRadius = 0f, gunHeight = 0f, mantlet = 0f, tint = new Color(0.95f, 0.96f, 1f),
            speed = 0f, turnRate = 0.6f, turretRate = 0.6f, reload = 999f, damage = 0f, range = 0f, hp = 2f, radius = 1.6f, isGun = true, meshYaw = -20f, muzzle = new Vector3(0f, 1.4f, 1f)
        };
        public static readonly VehicleSpec Flak38 = new VehicleSpec
        {
            id = "flak38", name = "2 cm Flak", hullMesh = "flak38_hull", turretMesh = null, texture = "flak38", forward = 1f, ringHeight = 0f,
            gunLength = 0f, gunRadius = 0f, gunHeight = 0f, mantlet = 0f, tint = new Color(0.95f, 0.96f, 1f),
            speed = 0f, turnRate = 1.2f, turretRate = 1.2f, reload = 999f, damage = 0f, range = 0f, hp = 1f, radius = 1.4f, isGun = true, meshYaw = -90f, muzzle = new Vector3(0f, 1.6f, 1.7f)
        };
        public static readonly VehicleSpec Kubelwagen = new VehicleSpec
        {
            id = "kubelwagen", name = "Staff car", hullMesh = "kubelwagen_hull", turretMesh = null, texture = "kubelwagen", forward = 1f, ringHeight = 0f,
            gunLength = 0f, gunRadius = 0f, gunHeight = 0f, mantlet = 0f, tint = new Color(0.95f, 0.96f, 1f),
            speed = 15f, turnRate = 2.8f, turretRate = 2.8f, reload = 999f, damage = 0f, range = 0f, hp = 1f, radius = 1.4f, transport = true
        };

        /// <summary>A spec whose models are in the build; the batch still baking is not.</summary>
        public static bool Available(VehicleSpec s) => Resources.Load<GameObject>("Models/" + s.hullMesh) != null;

        public static VehicleSpec ById(string id)
        {
            switch (id) { case "firefly": return Firefly; case "pz4": return PanzerIV; case "tiger": return Tiger; case "pak40": return Pak40; case "flak88": return Flak88; case "panther": return Panther; case "stug": return StuG; case "halftrack": return Halftrack;
                case "easy8": return Easy8; case "hellcat": return Hellcat; case "chaffee": return Chaffee; case "pershing": return Pershing; case "m10": return M10;
                case "t34_85": return T34_85; case "kv85": return KV85; case "su100": return SU100; case "is2": return IS2;
                case "hetzer": return Hetzer; case "kingtiger": return KingTiger; case "nebelwerfer": return Nebelwerfer; case "flak38": return Flak38; case "kubelwagen": return Kubelwagen; default: return Sherman; }
        }
    }

    public enum Formation { Wedge, Column, Line, Echelon }
}
