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
        public float hp, yaw, turretYaw, reloadLeft, hitFlash, lastHit = -100f, mgTimer, mgSound; public int flank; public bool unloaded, leaving;
        public float speedMul = 1f, damageMul = 1f, rangeMul = 1f, reloadMul = 1f, turretMul = 1f;
        public bool dead;
        public Vehicle target;

        Transform turret; Renderer[] renderers; Color[] baseColors; Transform muzzle, hullT, barrelT; Vector3 barrelHome; float hullYaw, recoil;
        static Material starMaterial, crossMaterial, commanderMaterial; const float CommanderYaw = 0f;   // the figure's facing in its mesh, corrected here if it looks the wrong way
        public float smokeTimer;
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
            var mat = new Material(vehicleTemplate); mat.SetTexture("_BaseMap", tex); mat.SetColor("_BaseColor", friendly ? spec.tint * Depot.CamoTint : spec.tint);

            // hull: mesh origin is the turret ring, so it hangs ringHeight below the pivot and the tracks touch the ground
            var hull = Instantiate(Resources.Load<GameObject>("Models/" + spec.hullMesh), transform);
            hull.name = "Hull"; hull.transform.localPosition = new Vector3(0f, spec.ringHeight, 0f); hull.transform.localRotation = Quaternion.Euler(0f, (spec.forward > 0f ? 0f : 180f) + spec.meshYaw, 0f);
            hullT = hull.transform; hullYaw = spec.forward > 0f ? 0f : 180f;
            foreach (var r in hull.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            Transform turretMesh = null;

            var pivot = new GameObject("Turret").transform; pivot.SetParent(transform, false); pivot.localPosition = new Vector3(0f, spec.ringHeight, 0f);
            turret = pivot;
            if (spec.turretMesh != null)
            {
                var tm = Instantiate(Resources.Load<GameObject>("Models/" + spec.turretMesh), pivot);
                tm.name = "TurretMesh"; tm.transform.localRotation = Quaternion.Euler(0f, spec.forward > 0f ? 0f : 180f, 0f); tm.transform.localPosition = new Vector3(spec.turretShift, 0f, 0f); turretMesh = tm.transform;   // the turret turns about its own centre
                var tmat = mat; if (spec.turretTexture != null) { tmat = new Material(mat); tmat.SetTexture("_BaseMap", Resources.Load<Texture2D>("Models/" + spec.turretTexture)); }
                if (spec.turretTexture != null)
                {
                    // a turret made on its own does not fill the hole the old one was cut from: a plate under it covers the ring
                    var plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(plate.GetComponent<Collider>()); plate.name = "RingPlate"; plate.transform.SetParent(pivot, false);
                    plate.transform.localPosition = new Vector3(0f, 0.03f, 0f); plate.transform.localScale = new Vector3(2.9f, 0.04f, 2.9f); plate.GetComponent<Renderer>().sharedMaterial = mat; plate.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                foreach (var r in tm.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = tmat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                // the generated barrel was cut off (it comes out bent and short); this one has the real length
                var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(barrel.GetComponent<Collider>());
                barrel.name = "Barrel"; barrel.transform.SetParent(pivot, false);
                barrel.transform.localScale = new Vector3(spec.gunRadius * 2f, spec.gunLength * 0.5f, spec.gunRadius * 2f);
                barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                barrel.transform.localPosition = new Vector3(spec.gunX, spec.gunHeight, spec.mantlet + spec.gunLength * 0.5f); barrelT = barrel.transform; barrelHome = barrel.transform.localPosition;
                barrel.GetComponent<Renderer>().sharedMaterial = barrelMaterial;
                // a short wide collar at the base hides where the generated gun was cut out of the mantlet
                var collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(collar.GetComponent<Collider>());
                collar.name = "Mantlet"; collar.transform.SetParent(pivot, false);
                collar.transform.localScale = new Vector3(spec.gunRadius * 5f, 0.22f, spec.gunRadius * 5f);
                collar.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                collar.transform.localPosition = new Vector3(0f, spec.gunHeight, spec.mantlet + 0.1f);
                collar.GetComponent<Renderer>().sharedMaterial = mat;
                if (spec.muzzleBrake)
                {
                    var brake = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(brake.GetComponent<Collider>());
                    brake.name = "MuzzleBrake"; brake.transform.SetParent(pivot, false);
                    brake.transform.localScale = new Vector3(spec.gunRadius * 3.4f, 0.16f, spec.gunRadius * 3.4f);
                    brake.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    brake.transform.localPosition = new Vector3(spec.gunX, spec.gunHeight, spec.mantlet + spec.gunLength - 0.16f);
                    brake.GetComponent<Renderer>().sharedMaterial = barrelMaterial;
                }
                muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(pivot, false); muzzle.localPosition = new Vector3(spec.gunX, spec.gunHeight, spec.mantlet + spec.gunLength);
            }
            else
            {
                // the anti-tank gun is one mesh at ground level that turns as a whole; its muzzle is ahead of the shield
                hull.transform.SetParent(pivot, false); hull.transform.localPosition = Vector3.zero; hull.transform.localScale = Vector3.one * spec.scale;
                muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(pivot, false); muzzle.localPosition = spec.muzzle * spec.scale;
            }
            Markings(hull.transform, pivot, turretMesh);
            // the commander in his hatch on the platoon's tanks
            if (friendly && turretMesh != null)
            {
                var cmd = Resources.Load<GameObject>("Props/commander");
                if (cmd != null)
                {
                    var tb = LocalBounds(turretMesh, pivot); var c = Instantiate(cmd, pivot); c.name = "Commander";
                    c.transform.localPosition = new Vector3(tb.center.x + tb.extents.x * 0.35f, tb.max.y - 0.5f, tb.center.z - tb.extents.z * 0.15f); c.transform.localRotation = Quaternion.Euler(0f, CommanderYaw, 0f);
                    if (commanderMaterial == null) { commanderMaterial = new Material(Resources.Load<Material>("VehicleLit")); commanderMaterial.SetTexture("_BaseMap", Resources.Load<Texture2D>("Props/commander_tex")); commanderMaterial.SetFloat("_Cull", 0f); }
                    foreach (var r in c.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = commanderMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                }
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
            float rate = spec.turretRate * turretMul * dt;
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

        /// <summary>The gun has just fired: the barrel slams back and the hull rocks on its springs.</summary>
        public void Recoil() { recoil = 1f; }

        public void Apply()
        {
            transform.rotation = Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f);
            if (turret != null) turret.rotation = Quaternion.Euler(0f, turretYaw * Mathf.Rad2Deg, 0f);
            if (recoil > 0f)
            {
                recoil = Mathf.Max(0f, recoil - Time.deltaTime * 4f); float k = Mathf.Sin(recoil * Mathf.PI);
                if (barrelT != null) barrelT.localPosition = barrelHome - Vector3.forward * (0.35f * k);
                if (hullT != null && !spec.isGun) { float back = Mathf.Cos(turretYaw - yaw); hullT.localRotation = Quaternion.Euler(-2.5f * k * back, hullYaw, 2.5f * k * Mathf.Sin(turretYaw - yaw)); }
            }
            if (hitFlash > 0f)
            {
                hitFlash = Mathf.Max(0f, hitFlash - Time.deltaTime * 4f);
                for (int i = 0; i < renderers.Length; i++) renderers[i].material.SetColor("_BaseColor", Color.Lerp(baseColors[i], Color.white, hitFlash));
            }
        }

        public void Hit(float damage)
        {
            if (dead) return;
            hp -= damage; hitFlash = 1f; lastHit = Time.time;
        }

        /// <summary>Turns the vehicle into a wreck: dark, scorched, the turret left where it was.</summary>
        public void Wreck()
        {
            dead = true;
            for (int i = 0; i < renderers.Length; i++) { var m = renderers[i].material; m.SetColor("_BaseColor", new Color(0.16f, 0.14f, 0.12f)); m.SetFloat("_Smoothness", 0.1f); }
            gameObject.name = "Wreck " + spec.name;
        }
        /// <summary>National markings, painted on as small quads: the Allied white star on the hull and turret sides,
        /// the Balkenkreuz on German turret sides (hull sides when there is no turret). Nothing on top.</summary>
        void Markings(Transform hull, Transform pivot, Transform turretMesh)
        {
            if (spec.isGun) return;
            if (starMaterial == null)
            {
                starMaterial = new Material(Resources.Load<Material>("GroundDecal")); starMaterial.SetTexture("_BumpMap", null);   // the keyword stays on: the variant without it is not in the build and the quad would render opaque
                starMaterial.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Star(256).texture); starMaterial.SetColor("_BaseColor", new Color(0.68f, 0.68f, 0.62f, 0.97f));   // weathered paint: white would bloom under the flare light starMaterial.renderQueue = 2470;
                crossMaterial = new Material(starMaterial); crossMaterial.SetTexture("_BaseMap", Lightswarm.ProceduralSprites.Balkenkreuz(256).texture); crossMaterial.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f, 0.97f));
            }
            var hb = LocalBounds(hull, transform);
            if (friendly && Depot.Nation == "su") return;   // the Soviet tanks carry their own painted numbers
            if (friendly)
            {
                // the white star on both sides of the hull (the sponsons are as wide as the tracks) and of the turret
                for (int s = -1; s <= 1; s += 2) Mark(hull, transform, new Vector3(s > 0 ? hb.max.x + 0.03f : hb.min.x - 0.03f, spec.ringHeight - 0.18f, hb.center.z + hb.extents.z * 0.05f), Quaternion.Euler(0f, -s * 90f, 0f), 0.95f, starMaterial);
                if (turretMesh != null) { var tb = LocalBounds(turretMesh, pivot); for (int s = -1; s <= 1; s += 2) Mark(turretMesh, pivot, new Vector3(s > 0 ? tb.max.x + 0.04f : tb.min.x - 0.04f, 0.5f, tb.center.z + tb.extents.z * 0.3f), Quaternion.Euler(0f, -s * 90f, 0f), 0.62f, starMaterial); }
            }
            else if (turretMesh != null)
            {
                var tb = LocalBounds(turretMesh, pivot);
                for (int s = -1; s <= 1; s += 2) Mark(turretMesh, pivot, new Vector3(s > 0 ? tb.max.x + 0.04f : tb.min.x - 0.04f, 0.5f, tb.center.z + tb.extents.z * 0.15f), Quaternion.Euler(0f, -s * 90f, 0f), 0.6f, crossMaterial);
            }
            else
            {
                for (int s = -1; s <= 1; s += 2) Mark(hull, transform, new Vector3(s > 0 ? hb.max.x + 0.04f : hb.min.x - 0.04f, hb.center.y + hb.extents.y * 0.35f, hb.center.z - hb.extents.z * 0.25f), Quaternion.Euler(0f, -s * 90f, 0f), 0.6f, crossMaterial);
            }
        }

        /// <summary>The box round every mesh under a part, measured in the given frame.</summary>
        static Bounds LocalBounds(Transform part, Transform frame)
        {
            bool first = true; var b = new Bounds();
            foreach (var mf in part.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue; var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? mb.min.x : mb.max.x, (i & 2) == 0 ? mb.min.y : mb.max.y, (i & 4) == 0 ? mb.min.z : mb.max.z);
                    var p = frame.InverseTransformPoint(mf.transform.TransformPoint(corner));
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        /// <summary>One marking quad, placed in the frame but parented to the part so it moves with it.</summary>
        static void Mark(Transform part, Transform frame, Vector3 pos, Quaternion rot, float size, Material m)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "Marking";
            q.transform.SetParent(part, false); q.transform.position = frame.TransformPoint(pos); q.transform.rotation = frame.rotation * rot; q.transform.localScale = Vector3.one * size;
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = true;
        }

    }
}
