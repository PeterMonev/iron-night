using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// A tank or gun on the field: the hull drives with tracked-vehicle handling (turn on the spot, slow down in turns),
    /// the turret swings toward its target at its own rate, the gun fires when the turret is on target. The visual is
    /// built from the generated meshes: hull at the ring height, a turret pivot at the ring, a straight barrel on it.
    /// </summary>
    public class Vehicle : MonoBehaviour
    {
        public VehicleSpec spec;
        public bool friendly;
        public float hp, yaw, turretYaw, reloadLeft, hitFlash;
        public float speedMul = 1f, damageMul = 1f, rangeMul = 1f, reloadMul = 1f;
        public bool dead;
        public Vehicle target;

        Transform turret; Renderer[] renderers; Color[] baseColors; Transform muzzle;
        static Material vehicleTemplate, barrelMaterial;

        public Vector3 Forward => new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
        public Vector3 GunDirection => new Vector3(Mathf.Sin(turretYaw), 0f, Mathf.Cos(turretYaw));
        public Vector3 MuzzlePosition => muzzle != null ? muzzle.position : transform.position + Vector3.up * 2f;
        public float Range => spec.range * rangeMul;

        public static Vehicle Create(VehicleSpec spec, bool friendly, Vector3 position, float yaw)
        {
            var go = new GameObject((friendly ? "Platoon " : "Enemy ") + spec.name);
            var v = go.AddComponent<Vehicle>();
            v.spec = spec; v.friendly = friendly; v.hp = spec.hp; v.yaw = yaw; v.turretYaw = yaw;
            go.transform.position = position;
            v.Build();
            v.Apply();
            return v;
        }

        void Build()
        {
            if (vehicleTemplate == null) { vehicleTemplate = Resources.Load<Material>("VehicleLit"); barrelMaterial = Resources.Load<Material>("BarrelLit"); }
            var tex = Resources.Load<Texture2D>("Models/" + spec.texture);
            var mat = new Material(vehicleTemplate); mat.SetTexture("_BaseMap", tex); mat.SetColor("_BaseColor", spec.tint);

            // hull: mesh origin is the turret ring, so it hangs ringHeight below the pivot and the tracks touch the ground
            var hull = Instantiate(Resources.Load<GameObject>("Models/" + spec.hullMesh), transform);
            hull.name = "Hull"; hull.transform.localPosition = new Vector3(0f, spec.ringHeight, 0f); hull.transform.localRotation = Quaternion.Euler(0f, spec.forward > 0f ? 0f : 180f, 0f);
            foreach (var r in hull.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }

            var pivot = new GameObject("Turret").transform; pivot.SetParent(transform, false); pivot.localPosition = new Vector3(0f, spec.ringHeight, 0f);
            turret = pivot;
            if (spec.turretMesh != null)
            {
                var tm = Instantiate(Resources.Load<GameObject>("Models/" + spec.turretMesh), pivot);
                tm.name = "TurretMesh"; tm.transform.localRotation = Quaternion.Euler(0f, spec.forward > 0f ? 0f : 180f, 0f);
                foreach (var r in tm.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                // the generated barrel was cut off (it comes out bent and short); this one has the real length
                var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(barrel.GetComponent<Collider>());
                barrel.name = "Barrel"; barrel.transform.SetParent(pivot, false);
                barrel.transform.localScale = new Vector3(spec.gunRadius * 2f, spec.gunLength * 0.5f, spec.gunRadius * 2f);
                barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                barrel.transform.localPosition = new Vector3(0f, spec.gunHeight, spec.mantlet + spec.gunLength * 0.5f);
                barrel.GetComponent<Renderer>().sharedMaterial = barrelMaterial;
                if (spec.muzzleBrake)
                {
                    var brake = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(brake.GetComponent<Collider>());
                    brake.name = "MuzzleBrake"; brake.transform.SetParent(pivot, false);
                    brake.transform.localScale = new Vector3(spec.gunRadius * 3.4f, 0.16f, spec.gunRadius * 3.4f);
                    brake.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    brake.transform.localPosition = new Vector3(0f, spec.gunHeight, spec.mantlet + spec.gunLength - 0.16f);
                    brake.GetComponent<Renderer>().sharedMaterial = barrelMaterial;
                }
                muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(pivot, false); muzzle.localPosition = new Vector3(0f, spec.gunHeight, spec.mantlet + spec.gunLength);
            }
            else
            {
                // the anti-tank gun is one mesh at ground level that turns as a whole; its muzzle is ahead of the shield
                hull.transform.SetParent(pivot, false); hull.transform.localPosition = Vector3.zero;
                muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(pivot, false); muzzle.localPosition = new Vector3(0f, 1.1f, 3.4f);
            }
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].sharedMaterial.GetColor("_BaseColor");
        }

        /// <summary>Tracked-vehicle drive: steer toward the wanted direction, move forward only when roughly aligned.</summary>
        public void Drive(Vector2 wanted, float dt)
        {
            if (spec.isGun || dead) return;
            float mag = wanted.magnitude; if (mag < 0.05f) return;
            float wantYaw = Mathf.Atan2(wanted.x, wanted.y);
            float diff = Mathf.DeltaAngle(yaw * Mathf.Rad2Deg, wantYaw * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float turn = spec.turnRate * dt;
            yaw += Mathf.Clamp(diff, -turn, turn);
            float align = Mathf.Cos(diff);                           // no forward speed while turning around
            float v = spec.speed * speedMul * Mathf.Clamp01(mag) * Mathf.Clamp01(align + 0.15f);
            transform.position += Forward * (v * dt);
        }

        /// <summary>Turret (or the whole gun) swings toward the target; returns true when it is on target.</summary>
        public bool Aim(Vector3 targetPos, float dt)
        {
            var d = targetPos - transform.position; if (d.sqrMagnitude < 0.01f) return false;
            float want = Mathf.Atan2(d.x, d.z);
            float diff = Mathf.DeltaAngle(turretYaw * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float rate = spec.turretRate * dt;
            turretYaw += Mathf.Clamp(diff, -rate, rate);
            return Mathf.Abs(diff) < 0.06f;
        }

        public void IdleTurret(float dt)
        {
            // with nothing to shoot the turret drifts back to the hull's heading
            float diff = Mathf.DeltaAngle(turretYaw * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float rate = spec.turretRate * 0.5f * dt;
            turretYaw += Mathf.Clamp(diff, -rate, rate);
        }

        public void Apply()
        {
            transform.rotation = Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f);
            if (turret != null) turret.rotation = Quaternion.Euler(0f, turretYaw * Mathf.Rad2Deg, 0f);
            if (hitFlash > 0f)
            {
                hitFlash = Mathf.Max(0f, hitFlash - Time.deltaTime * 4f);
                for (int i = 0; i < renderers.Length; i++) renderers[i].material.SetColor("_BaseColor", Color.Lerp(baseColors[i], Color.white, hitFlash));
            }
        }

        public void Hit(float damage)
        {
            if (dead) return;
            hp -= damage; hitFlash = 1f;
        }

        /// <summary>Turns the vehicle into a wreck: dark, scorched, the turret left where it was.</summary>
        public void Wreck()
        {
            dead = true;
            for (int i = 0; i < renderers.Length; i++) { var m = renderers[i].material; m.SetColor("_BaseColor", new Color(0.16f, 0.14f, 0.12f)); m.SetFloat("_Smoothness", 0.1f); }
            gameObject.name = "Wreck " + spec.name;
        }
    }
}
