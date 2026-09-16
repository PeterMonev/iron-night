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
        public string id, name, hullMesh, turretMesh, texture;
        public float forward = 1f;      // +1 when the mesh's nose points +Z, -1 when it points -Z
        public float ringHeight;        // metres from the ground to the turret ring (the mesh origin)
        public float gunLength, gunRadius, gunHeight, mantlet; public bool muzzleBrake;
        public Color tint = Color.white;
        public float speed, turnRate, turretRate, reload, damage, range, hp, radius;
        public bool isGun;              // PaK, 88: no turret, aims with its body, never moves
        public bool casemate;           // StuG: no turret, aims by turning the hull, drives
        public bool transport;          // half-track: no gun, brings a squad of tank hunters and leaves
        public Vector3 muzzle = new Vector3(0f, 1.1f, 3.4f);   // where the shell leaves a turretless vehicle
        public float scale = 1f;        // the mesh scaled up: the 88 is the PaK mesh at 1.45

        public static readonly VehicleSpec Sherman = new VehicleSpec
        {
            id = "sherman", name = "M4 Sherman", hullMesh = "sherman_hull", turretMesh = "sherman_turret", texture = "sherman", forward = 1f, ringHeight = 2.178f,
            gunLength = 1.9f, gunRadius = 0.08f, gunHeight = 0.42f, mantlet = 1.15f, tint = new Color(0.86f, 0.9f, 0.68f),
            speed = 9f, turnRate = 2.2f, turretRate = 3.2f, reload = 1.3f, damage = 1f, range = 26f, hp = 3f, radius = 2.4f
        };
        public static readonly VehicleSpec Firefly = new VehicleSpec
        {
            id = "firefly", name = "Sherman Firefly", hullMesh = "sherman_hull", turretMesh = "sherman_turret", texture = "sherman", forward = 1f, ringHeight = 2.178f,
            gunLength = 3.6f, gunRadius = 0.075f, gunHeight = 0.42f, mantlet = 1.15f, muzzleBrake = true, tint = new Color(0.8f, 0.85f, 0.64f),
            speed = 8.5f, turnRate = 2f, turretRate = 2.8f, reload = 1.6f, damage = 2f, range = 30f, hp = 3f, radius = 2.4f
        };
        public static readonly VehicleSpec PanzerIV = new VehicleSpec
        {
            id = "pz4", name = "Panzer IV", hullMesh = "pz4_hull", turretMesh = "pz4_turret", texture = "pz4", forward = 1f, ringHeight = 2.022f,
            gunLength = 3.2f, gunRadius = 0.07f, gunHeight = 0.4f, mantlet = 1.15f, muzzleBrake = true, tint = new Color(0.95f, 0.96f, 1f),
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
            id = "pak40", name = "PaK 40", hullMesh = "pak40_hull", turretMesh = null, texture = "pak40", forward = -1f, ringHeight = 0f,
            gunLength = 0f, tint = new Color(0.95f, 0.96f, 1f),
            speed = 0f, turnRate = 1.4f, turretRate = 1.4f, reload = 2.2f, damage = 1f, range = 32f, hp = 2f, radius = 1.8f, isGun = true
        };

        public static readonly VehicleSpec Flak88 = new VehicleSpec
        {
            id = "flak88", name = "8.8 cm Flak", hullMesh = "flak88_hull", turretMesh = null, texture = "flak88", forward = 1f, ringHeight = 0f, muzzle = new Vector3(0f, 3.6f, 3.2f),
            gunLength = 0f, tint = new Color(1f, 1f, 1f),
            speed = 0f, turnRate = 0.9f, turretRate = 0.9f, reload = 3.2f, damage = 2f, range = 40f, hp = 4f, radius = 2.6f, isGun = true
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

        public static VehicleSpec ById(string id)
        {
            switch (id) { case "firefly": return Firefly; case "pz4": return PanzerIV; case "tiger": return Tiger; case "pak40": return Pak40; case "flak88": return Flak88; case "panther": return Panther; case "stug": return StuG; case "halftrack": return Halftrack; default: return Sherman; }
        }
    }

    public enum Formation { Wedge, Column, Line, Echelon }
}
