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
        public float hp, yaw, turretYaw, reloadLeft, hitFlash, lastHit = -100f, mgTimer, mgSound, trackOut, fallBack; public int flank, fallenBack, sapperLeft; public bool unloaded, leaving, sapper; public Vector3 lastMine;   // trackOut: seconds left with a track knocked off
        public float speedMul = 1f, damageMul = 1f, rangeMul = 1f, reloadMul = 1f, turretMul = 1f;
        public bool dead;
        public Vehicle target;

        Transform turret; Renderer[] renderers; Color[] baseColors; Transform muzzle, hullT, barrelT; Vector3 barrelHome; float hullYaw, recoil;
        static Material starMaterial, crossMaterial, commanderMaterial; const float CommanderYaw = 0f;   // the figure's facing in its mesh, corrected here if it looks the wrong way
        public float smokeTimer;
        static Material vehicleTemplate, vehicleTemplateN, barrelMaterial; public static bool Wet;   // a rainy night: the armour shines

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
            var tex = Resources.Load<Texture2D>("Models/" + spec.texture); var nrm = Resources.Load<Texture2D>("Models/" + spec.texture + "_n");
            if (vehicleTemplateN == null) vehicleTemplateN = Resources.Load<Material>("VehicleLitN");
            var mat = MakeMaterial(tex, nrm);
            bool artist = HasParts(spec.hullMesh);   // an artist's model: one part per material

            // hull: mesh origin is the turret ring, so it hangs ringHeight below the pivot and the tracks touch the ground
            var hull = artist ? Assemble(spec.hullMesh, transform) : Instantiate(Resources.Load<GameObject>("Models/" + spec.hullMesh), transform);
            hull.name = "Hull"; hull.transform.localPosition = new Vector3(0f, spec.ringHeight, 0f); hull.transform.localRotation = Quaternion.Euler(0f, (spec.forward > 0f ? 0f : 180f) + spec.meshYaw, 0f);
            hullT = hull.transform; hullYaw = spec.forward > 0f ? 0f : 180f;
            foreach (var r in hull.GetComponentsInChildren<Renderer>()) { if (!artist) r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            Transform turretMesh = null;

            var pivot = new GameObject("Turret").transform; pivot.SetParent(transform, false); pivot.localPosition = new Vector3(0f, spec.ringHeight, 0f);
            turret = pivot;
            if (spec.turretMesh != null)
            {
                var tm = artist ? Assemble(spec.turretMesh, pivot) : Instantiate(Resources.Load<GameObject>("Models/" + spec.turretMesh), pivot);
                tm.name = "TurretMesh"; tm.transform.localRotation = Quaternion.Euler(0f, spec.forward > 0f ? 0f : 180f, 0f); tm.transform.localPosition = new Vector3(spec.turretShift, 0f, 0f); turretMesh = tm.transform;   // the turret turns about its own centre
                var tmat = mat; if (spec.turretTexture != null) { tmat = new Material(mat); tmat.SetTexture("_BaseMap", Resources.Load<Texture2D>("Models/" + spec.turretTexture)); var tn = Resources.Load<Texture2D>("Models/" + spec.turretTexture + "_n"); if (tn != null) tmat.SetTexture("_BumpMap", tn); }
                {
                    // the cut turret is open underneath and the hull open above: a plate at the ring, as wide as the turret, covers both
                    var tb0 = LocalBounds(turretMesh, pivot); float ringW = spec.turretTexture != null ? 2.25f : Mathf.Min(tb0.size.x, tb0.size.z) * 0.95f;
                    var plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(plate.GetComponent<Collider>()); plate.name = "RingPlate"; plate.transform.SetParent(pivot, false);
                    if (artist) { ringW = tb0.size.x * 0.7f; tmat = new Material(vehicleTemplate); tmat.SetColor("_BaseColor", new Color(0.09f, 0.09f, 0.085f)); }   // an artist's turret carries its gun: its width is the ring; the plate is the dark of the fighting compartment
                    plate.transform.localPosition = new Vector3(spec.turretTexture != null || artist ? 0f : tb0.center.x, 0.03f, spec.turretTexture != null || artist ? 0f : tb0.center.z); plate.transform.localScale = new Vector3(ringW, 0.04f, ringW); plate.GetComponent<Renderer>().sharedMaterial = tmat; plate.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                foreach (var r in tm.GetComponentsInChildren<Renderer>()) { if (!artist) r.sharedMaterial = tmat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
                if (spec.gunLength <= 0f) { muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(pivot, false); muzzle.localPosition = spec.muzzle; }   // the model keeps its own gun
                else {
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
            }
            else
            {
                // the anti-tank gun is one mesh at ground level that turns as a whole; its muzzle is ahead of the shield
                hull.transform.SetParent(pivot, false); hull.transform.localPosition = Vector3.zero; hull.transform.localScale = Vector3.one * spec.scale;
                muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(pivot, false); muzzle.localPosition = spec.muzzle * spec.scale;
            }
            Markings(hull.transform, pivot, turretMesh);
            // the commander in his hatch on the platoon's tanks
            if (friendly && turretMesh != null && !spec.crewed)
            {
                var cmd = Resources.Load<GameObject>("Props/commander");
                if (cmd != null)
                {
                    var tb = LocalBounds(turretMesh, pivot); var c = Instantiate(cmd, pivot); c.name = "Commander";
                    bool measured = spec.hatch.sqrMagnitude > 0f;
                    var at = measured ? spec.hatch : new Vector3(artist ? 0.3f : tb.center.x + tb.extents.x * 0.3f, 0f, artist ? -0.2f : tb.center.z - tb.extents.z * 0.2f);
                    float roof = measured ? RoofHeightAt(turretMesh, pivot, at.x, at.z) - 0.08f : RoofHeight(turretMesh, pivot) - 0.05f;   // in his cupola, at its own height
                    c.transform.localScale = Vector3.one * 0.55f; c.transform.localPosition = new Vector3(at.x, roof, at.z); c.transform.localRotation = Quaternion.Euler(0f, CommanderYaw, 0f);
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
            if (trackOut > 0f) { yaw += Mathf.Clamp(Mathf.DeltaAngle(yaw * Mathf.Rad2Deg, Mathf.Atan2(wanted.x, wanted.y) * Mathf.Rad2Deg) * Mathf.Deg2Rad, -spec.turnRate * 0.25f * dt, spec.turnRate * 0.25f * dt); return; }   // a track off: it can only pivot, slowly
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

        /// <summary>The racks went up: the turret comes off the ring, free of the hull, for the battle to throw. Null for
        /// guns, casemates and anything without a turret.</summary>
        public Transform BlowTurret()
        {
            if (turret == null || spec.isGun || spec.casemate || spec.turretMesh == null) return null;
            var t = turret; turret = null; t.SetParent(null, true); return t;
        }

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
            if (spec.painted) return;   // the stars (or the Soviet numbers) are in the generated texture, on the armour itself
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
        Material MakeMaterial(Texture2D tex, Texture2D nrm)
        {
            var mat = new Material(nrm != null && vehicleTemplateN != null ? vehicleTemplateN : vehicleTemplate); mat.SetTexture("_BaseMap", tex); if (nrm != null) mat.SetTexture("_BumpMap", nrm);
            mat.SetColor("_BaseColor", friendly ? spec.tint * Depot.CamoTint : spec.tint); if (Wet) mat.SetFloat("_Smoothness", 0.55f); return mat;
        }

        /// <summary>An artist's model comes as one OBJ per material (<name>_m0, _m1, ...) with textures <texture>_m<i>: all of them under one node.</summary>
        public static bool HasParts(string baseName) { for (int i = 0; i < 24; i++) if (Resources.Load<GameObject>("Models/" + baseName + "_m" + i) != null) return true; return false; }

        GameObject Assemble(string baseName, Transform parent)
        {
            var root = new GameObject(baseName); root.transform.SetParent(parent, false);
            for (int i = 0; i < 24; i++)
            {
                var prefab = Resources.Load<GameObject>("Models/" + baseName + "_m" + i); if (prefab == null) continue;
                var part = Instantiate(prefab, root.transform); part.name = "m" + i;
                var m = MakeMaterial(Resources.Load<Texture2D>("Models/" + spec.texture + "_m" + i), Resources.Load<Texture2D>("Models/" + spec.texture + "_m" + i + "_n"));
                m.SetFloat("_Smoothness", Wet ? 0.6f : 0.2f);   // painted steel: a little sheen, the artist's normal map shapes it
                foreach (var r in part.GetComponentsInChildren<Renderer>()) r.sharedMaterial = m;
            }
            return root;
        }

        /// <summary>The top of the turret at one spot (the cupola): the 95th percentile height of the vertices within 0.3 m of it.</summary>
        static float RoofHeightAt(Transform part, Transform frame, float x, float z)
        {
            var ys = new System.Collections.Generic.List<float>();
            foreach (var mf in part.GetComponentsInChildren<MeshFilter>()) { if (mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue; foreach (var v in mf.sharedMesh.vertices) { var p = frame.InverseTransformPoint(mf.transform.TransformPoint(v)); if ((p.x - x) * (p.x - x) + (p.z - z) * (p.z - z) < 0.09f) ys.Add(p.y); } }
            if (ys.Count < 10) return RoofHeight(part, frame); ys.Sort(); return ys[Mathf.Clamp(Mathf.FloorToInt(ys.Count * 0.95f), 0, ys.Count - 1)];
        }

        /// <summary>The turret roof: the highest dense band of vertices within 0.6 m of the turret axis, so the gun, an aerial or a roof MG do not count.</summary>
        static float RoofHeight(Transform part, Transform frame)
        {
            var ys = new System.Collections.Generic.List<float>();
            foreach (var mf in part.GetComponentsInChildren<MeshFilter>()) { if (mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue; foreach (var v in mf.sharedMesh.vertices) { var p = frame.InverseTransformPoint(mf.transform.TransformPoint(v)); if (p.x * p.x + p.z * p.z < 0.36f) ys.Add(p.y); } }   // the central column only: not the gun, not the bustle
            if (ys.Count < 20) return LocalBounds(part, frame).max.y - 0.35f; ys.Sort();
            // the roof is the highest 10 cm band that still holds a good share of the column's vertices; a machine gun or a hatch handle above it is sparse
            float top = ys[ys.Count - 1]; int need = Mathf.Max(8, ys.Count / 25);
            for (float y = top; y > ys[0]; y -= 0.1f) { int n = 0; foreach (var v in ys) if (v > y - 0.1f && v <= y) n++; if (n >= need) return y; }
            return ys[Mathf.Clamp(Mathf.FloorToInt(ys.Count * 0.9f), 0, ys.Count - 1)];
        }

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
