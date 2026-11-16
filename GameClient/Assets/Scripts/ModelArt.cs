using System.Collections.Generic;
using UnityEngine;
using GameCore.Combat;

namespace GameClient
{
    /// <summary>Asker animasyonu ortak arayüzü: kodla çizilen iskelet (CharacterRig) ve hazır model (ModelRig).</summary>
    public interface ISoldierAnim
    {
        void SetMoving(bool moving);
        void TriggerAttack();
    }

    /// <summary>
    /// Hazır CC0 modelleri yükler (Assets/Resources/ThirdParty): Kenney Castle Kit + Fantasy Town Kit (yapılar),
    /// KayKit Adventurers + Character Animations (karakterler). Lisanslar: Resources/ThirdParty/LICENSES.md.
    /// </summary>
    public static class ModelLibrary
    {
        private const string Root = "ThirdParty/";
        private static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, AnimationClip[]> clipSets = new Dictionary<string, AnimationClip[]>();
        private static int available = -1;

        public static bool Available
        {
            get
            {
                if (available < 0) available = Resources.Load<GameObject>(Root + "Kenney/Castle/tower-square-base") != null ? 1 : 0;
                return available == 1;
            }
        }

        public static GameObject Prefab(string path)
        {
            if (!prefabs.TryGetValue(path, out var p)) { p = Resources.Load<GameObject>(Root + path); prefabs[path] = p; }
            return p;
        }

        /// <summary>Modeli ebeveynin altına koyar (yerel konum, Y dönüşü, ölçek). Model yoksa null.</summary>
        public static Transform Spawn(Transform parent, string path, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            var p = Prefab(path);
            if (p == null) return null;
            var go = Object.Instantiate(p, parent, false);
            go.name = path.Substring(path.LastIndexOf('/') + 1);
            var t = go.transform;
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f) * t.localRotation;
            t.localScale = t.localScale * scale;
            return t;
        }

        public static AnimationClip[] Clips(string file)
        {
            if (!clipSets.TryGetValue(file, out var c)) { c = Resources.LoadAll<AnimationClip>(Root + "KayKit/Animations/" + file); clipSets[file] = c; }
            return c;
        }

        /// <summary>Dünya uzayında toplam yükseklik (görünür parçalar).</summary>
        public static float WorldHeight(Transform t)
        {
            bool any = false; Bounds b = default;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any ? b.size.y : 0f;
        }

        public static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { var r = FindDeep(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }
    }

    /// <summary>Hazır modelli asker için animasyon: Legacy Animation ile bekleme / yürüme / saldırı.</summary>
    public sealed class ModelRig : MonoBehaviour, ISoldierAnim
    {
        private Animation anim;
        private string idle, walk, attack, current;
        private bool moving;
        private float attackEnd = -1f;

        public void Setup(Animation a, string idleClip, string walkClip, string attackClip, float walkSpeed)
        {
            anim = a; idle = idleClip; walk = walkClip; attack = attackClip;
            if (anim[walk] != null) { anim[walk].wrapMode = WrapMode.Loop; anim[walk].speed = walkSpeed; }
            if (anim[idle] != null) anim[idle].wrapMode = WrapMode.Loop;
            if (attack != null && anim[attack] != null) anim[attack].wrapMode = WrapMode.Once;
            if (anim[idle] != null) { anim.Play(idle); current = idle; }
        }

        public void SetMoving(bool m) => moving = m;

        public void TriggerAttack()
        {
            if (anim == null || attack == null || anim[attack] == null) return;
            anim.CrossFade(attack, 0.05f, PlayMode.StopAll);
            anim[attack].time = 0f;
            attackEnd = Time.time + anim[attack].length / Mathf.Max(0.01f, anim[attack].speed);
            current = attack;
        }

        private void Update()
        {
            if (anim == null || Time.time < attackEnd) return;
            string want = moving ? walk : idle;
            if (want != current && anim[want] != null) { anim.CrossFade(want, 0.15f); current = want; }
        }
    }

    /// <summary>Can çubuğu yüksekliği (sınır kutusu güvenilmez olan modeller için açık değer).</summary>
    public sealed class SoldierHeight : MonoBehaviour { public float Value; }

    /// <summary>Binekli asker: altındaki tüm ModelRig'lere (binek + binici) aynı komutu iletir.</summary>
    public sealed class MultiRig : MonoBehaviour, ISoldierAnim
    {
        private ModelRig[] rigs;
        private ModelRig[] Rigs => rigs ??= GetComponentsInChildren<ModelRig>(true);
        public void SetMoving(bool moving) { foreach (var r in Rigs) r.SetMoving(moving); }
        public void TriggerAttack() { foreach (var r in Rigs) r.TriggerAttack(); }
    }

    /// <summary>
    /// Hazır modellerle askerler, binalar ve kuleler (kullanıcı kararı 16.11.2026: ücretsiz CC0 paketler).
    /// Model bulunamazsa null döner ve VillageArt kodla çizilen görünümü kullanır. Yalnızca görsel.
    /// </summary>
    public static class ModelArt
    {
        private struct Look
        {
            public string Model, Right, Left, Attack, Idle, Walk;
            public float Height;
            public Color Tint;
        }

        private static string W(string n) => n == null ? null : "KayKit/Weapons/" + n;

        private static Look LookOf(SoldierType t, int L)
        {
            var w = Color.white;
            switch (t)
            {
                case SoldierType.Swordsman:
                    return new Look { Model = "Knight", Right = "sword_1handed", Left = L >= 15 ? "shield_badge_color" : (L >= 6 ? "shield_badge" : "shield_round"), Attack = "Melee_1H_Attack_Chop", Idle = "Idle_A", Walk = "Walking_A", Height = 0.72f, Tint = w };
                case SoldierType.Archer:
                    return new Look { Model = "Ranger", Left = "bow_withString", Right = null, Attack = "Ranged_Bow_Release", Idle = "Ranged_Bow_Idle", Walk = "Walking_B", Height = 0.7f, Tint = w };
                case SoldierType.Mage:
                    return new Look { Model = "Mage", Right = "staff", Attack = "Ranged_Magic_Shoot", Idle = "Idle_B", Walk = "Walking_A", Height = 0.7f, Tint = w };
                case SoldierType.Goblin:
                    return new Look { Model = "Rogue", Right = "dagger", Left = "dagger", Attack = "Melee_Dualwield_Attack_Stab", Idle = "Idle_A", Walk = "Walking_C", Height = 0.52f, Tint = new Color(0.72f, 1f, 0.62f) };
                case SoldierType.Spearman:
                    return new Look { Model = "Rogue_Hooded", Right = "staff", Left = L >= 10 ? "shield_spikes_color" : "shield_spikes", Attack = "Melee_2H_Attack_Stab", Idle = "Idle_A", Walk = "Walking_A", Height = 0.72f, Tint = new Color(1f, 0.92f, 0.8f) };
                case SoldierType.Giant:
                    return new Look { Model = "Barbarian", Right = "axe_2handed", Attack = "Melee_2H_Attack_Chop", Idle = "Melee_2H_Idle", Walk = "Walking_A", Height = 1.1f, Tint = new Color(1f, 0.9f, 0.85f) };
                case SoldierType.Fairy:
                    return new Look { Model = "Mage", Right = "wand", Attack = "Ranged_Magic_Shoot", Idle = "Idle_B", Walk = "Idle_B", Height = 0.42f, Tint = new Color(1f, 0.8f, 0.95f) };
                default:
                    return new Look { Model = null };
            }
        }

        private static readonly string[] AnimFiles = { "Rig_Medium_General", "Rig_Medium_MovementBasic", "Rig_Medium_CombatMelee", "Rig_Medium_CombatRanged" };

        public static Transform TryBuildSoldier(Transform parent, SoldierType t, int level)
        {
            if (!ModelLibrary.Available) return null;
            int L = Mathf.Clamp(level, 1, 30);
            if (t == SoldierType.Catapult)
            {
                var croot = VillageArt.Group(parent, t.ToString(), Vector3.zero);
                var c = ModelLibrary.Spawn(croot, "Kenney/Castle/siege-catapult", new Vector3(0.12f, 0.06f, 0f), 90f, 0.32f);
                if (c == null) { Object.Destroy(croot.gameObject); return null; }
                VillageArt.LevelEffectsPublic(croot, L, 1.4f, 0.45f);
                return croot;
            }
            if (t == SoldierType.Cavalry || t == SoldierType.DragonRider) return Mounted(parent, t, L);
            var look = LookOf(t, L);
            if (look.Model == null) return null;
            var root = VillageArt.Group(parent, t.ToString(), Vector3.zero);
            if (Character(root, look, L, t == SoldierType.Giant ? 0.6f : (t == SoldierType.Goblin ? 1.1f : 0.8f)) == null) { Object.Destroy(root.gameObject); return null; }
            if (t == SoldierType.Fairy) VillageArt.FairyWings(root, look.Height);
            VillageArt.LevelEffectsPublic(root, L, t == SoldierType.Giant ? 1.8f : 1f, look.Height * 1.05f);
            return root;
        }

        /// <summary>KayKit karakteri: boy, silahlar, renk tonu, animasyon (ModelRig holder üstünde). Model yoksa null.</summary>
        private static Transform Character(Transform holder, Look look, int L, float walkSpeed)
        {
            var body = ModelLibrary.Spawn(holder, "KayKit/Characters/" + look.Model, Vector3.zero);
            if (body == null) return null;
            float h = ModelLibrary.WorldHeight(body);
            if (h > 0.001f) body.localScale *= look.Height * (1f + Mathf.Min(0.12f, (L - 1) * 0.004f)) / h;
            Attach(body, "handslot.r", W(look.Right));
            Attach(body, "handslot.l", W(look.Left));
            if (look.Tint != Color.white)
                foreach (var r in body.GetComponentsInChildren<Renderer>())
                    foreach (var m in r.materials) m.color *= look.Tint;

            // Legacy içe aktarım modele zaten bir Animation bileşeni koyar; ikincisi eklenemez (AddComponent null döner).
            var anim = body.GetComponent<Animation>();
            if (anim == null) anim = body.gameObject.AddComponent<Animation>();
            anim.playAutomatically = false;
            foreach (var f in AnimFiles)
                foreach (var clip in ModelLibrary.Clips(f))
                    if (clip != null && !clip.name.StartsWith("__preview__") && anim.GetClip(clip.name) == null) { clip.legacy = true; anim.AddClip(clip, clip.name); }
            holder.gameObject.AddComponent<ModelRig>().Setup(anim, look.Idle, look.Walk, look.Attack, walkSpeed);
            return body;
        }

        /// <summary>
        /// Quaternius FBX'lerinde kök kemik 100 kat ölçekli; Unity'nin hazır sınır kutusu gerçek görüntüyle uyuşmuyor
        /// (boy yanlış ölçülüyor, görünmez oluyor). Gerçek (pişirilmiş) mesh'ten sınır kutusunu yeniden kurar; dünya boyunu döner.
        /// </summary>
        private static float FixSkinnedBounds(Transform model, out float baseY)
        {
            // Kemiklerin dünya konumları gerçek görüntüyü izler; iskelet kutusuna mesh payı (%18) eklenir.
            float minY = float.MaxValue, maxY = float.MinValue;
            var all = new List<Vector3>();
            foreach (var sk in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach (var b in sk.bones)
                    if (b != null) { var w = b.position; all.Add(w); minY = Mathf.Min(minY, w.y); maxY = Mathf.Max(maxY, w.y); }
            baseY = 0f;
            if (all.Count < 2 || maxY <= minY) return ModelLibrary.WorldHeight(model);
            float pad = (maxY - minY) * 0.18f;
            foreach (var sk in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var tr = sk.transform;
                var lb = new Bounds(tr.InverseTransformPoint(all[0]), Vector3.zero);
                foreach (var w in all) lb.Encapsulate(tr.InverseTransformPoint(w));
                lb.Expand(lb.size.magnitude * 0.4f);
                sk.localBounds = lb;
            }
            baseY = minY - pad * 0.5f - model.position.y;
            return maxY - minY + pad;
        }

        /// <summary>Gömülü animasyonlu modelde adı belirli bir ekle biten klip (ör. "Armature|Run" için "|Run").</summary>
        private static string ClipEnding(Animation a, string suffix)
        {
            foreach (AnimationState st in a)
                if (st.name.EndsWith(suffix)) return st.name;
            return null;
        }

        /// <summary>
        /// Binekli askerler: Atlı Kılıçlı = Quaternius atı + şövalye, Ejderha Binicisi = Quaternius ejderhası + barbar.
        /// Binek yürür/uçar, binici saldırır; ikisi birlikte MultiRig ile sürülür.
        /// </summary>
        private static Transform Mounted(Transform parent, SoldierType t, int L)
        {
            bool dragon = t == SoldierType.DragonRider;
            var root = VillageArt.Group(parent, t.ToString(), Vector3.zero);
            var mountHolder = VillageArt.Group(root, "mount", Vector3.zero);
            var mount = ModelLibrary.Spawn(mountHolder, dragon ? "Quaternius/Dragon" : "Quaternius/Horse", Vector3.zero, dragon ? DragonYaw : HorseYaw);
            if (mount == null) { Object.Destroy(root.gameObject); return null; }
            // Quaternius malzemelerini oyunun ortak (opak, aynı ışıklı) malzemesiyle değiştir; renk korunur.
            foreach (var r in mount.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var c = mats[i] != null ? mats[i].color : Color.gray; c.a = 1f;
                    if (dragon) c = new Color(Mathf.Min(1f, c.r * 2.8f), Mathf.Min(1f, c.g * 1.8f), Mathf.Min(1f, c.b * 1.8f)); // çok koyu geliyor: kızıl ejderha
                    mats[i] = VillageArt.Mat(c);
                }
                r.sharedMaterials = mats;
            }
            float mh = FixSkinnedBounds(mount, out float baseY);
            float targetH = dragon ? 0.75f : 0.62f;
            float k = mh > 0.001f ? targetH * (1f + Mathf.Min(0.12f, (L - 1) * 0.004f)) / mh : 1f;
            mount.localScale *= k;
            mount.localPosition -= new Vector3(0f, baseY * k, 0f); // ayaklar yere
            var ma = mount.GetComponent<Animation>();
            if (ma != null)
            {
                ma.playAutomatically = false;
                string idle = ClipEnding(ma, dragon ? "_Flying" : "|Idle");
                string move = ClipEnding(ma, dragon ? "_Flying" : "|Run");
                string atk = dragon ? ClipEnding(ma, "_Attack") : null;
                if (idle != null && move != null) mountHolder.gameObject.AddComponent<ModelRig>().Setup(ma, idle, move, atk, dragon ? 1f : 0.9f);
            }

            var riderLook = dragon
                ? new Look { Model = "Barbarian", Right = "axe_1handed", Attack = "Melee_1H_Attack_Chop", Idle = "Jump_Idle", Walk = "Jump_Idle", Height = 0.5f, Tint = new Color(1f, 0.85f, 0.75f) }
                : new Look { Model = "Knight", Right = "sword_1handed", Left = L >= 12 ? "shield_badge_color" : "shield_badge", Attack = "Melee_1H_Attack_Chop", Idle = "Jump_Idle", Walk = "Jump_Idle", Height = 0.5f, Tint = Color.white };
            var rider = VillageArt.Group(root, "rider", new Vector3(0f, (dragon ? DragonSeat : HorseSeat) * targetH, dragon ? DragonSeatZ : HorseSeatZ));
            Character(rider, riderLook, L, 1f);
            root.gameObject.AddComponent<MultiRig>();
            root.gameObject.AddComponent<SoldierHeight>().Value = (dragon ? DragonSeat : HorseSeat) * targetH + 0.62f; // can çubuğu: binicinin başının üstü
            VillageArt.LevelEffectsPublic(root, L, 1.6f, targetH + 0.35f);
            return root;
        }

        // Binek yönü ve eyer konumu (bineğin boyuna oranla); oyunda bakılarak ayarlandı.
        private const float HorseYaw = 0f, DragonYaw = 0f;
        private const float HorseSeat = 0.45f, HorseSeatZ = -0.02f, DragonSeat = 0.4f, DragonSeatZ = 0f;

        private static void Attach(Transform body, string bone, string path)
        {
            if (path == null) return;
            var b = ModelLibrary.FindDeep(body, bone);
            if (b == null) return;
            var w = ModelLibrary.Spawn(b, path, Vector3.zero);
            if (w != null) { w.localRotation = Quaternion.identity; w.localScale = Vector3.one; }
        }

        // ---------- Binalar ----------
        private static Transform T(Transform p, string piece, Vector3 pos, float yaw = 0f, float s = 1f) => ModelLibrary.Spawn(p, "Kenney/Town/" + piece, pos, yaw, s);
        private static Transform C(Transform p, string piece, Vector3 pos, float yaw = 0f, float s = 1f) => ModelLibrary.Spawn(p, "Kenney/Castle/" + piece, pos, yaw, s);

        /// <summary>Tek hücrelik ev: dört duvar (kat başına), kapı önde, çatı üstte.</summary>
        private static void House(Transform g, float s, bool stone, int floors, string roof, bool chimney, string banner)
        {
            var h = VillageArt.Group(g, "house", Vector3.zero, 0f, s);
            string w = stone ? "wall" : "wall-wood";
            for (int f = 0; f < floors; f++)
                for (int k = 0; k < 4; k++)
                {
                    string piece = f == 0 && k == 1 ? w + "-door" : (k % 2 == 0 ? w + "-window-shutters" : w);
                    T(h, piece, new Vector3(0f, f, 0f), k * 90f);
                }
            T(h, roof, new Vector3(0f, floors, 0f));
            if (chimney) T(h, "chimney", new Vector3(0f, floors, 0f), 180f);
            if (banner != null) { T(h, banner, Vector3.zero, 90f); T(h, banner, Vector3.zero, 180f); }
        }

        public static Transform TryBuildBuilding(Transform parent, BuildingType t, int level, int seed, Vector3 pos)
        {
            if (!ModelLibrary.Available) return null;
            int tier = VillageArt.Tier(level);
            var g = VillageArt.Group(parent, t.ToString(), pos);
            switch (t)
            {
                case BuildingType.VillagerHouse:
                    House(g, 0.62f, tier >= 1, tier >= 2 ? 2 : 1, tier >= 3 ? "roof-high-point" : "roof-point", tier >= 1, null);
                    break;
                case BuildingType.Barracks:
                    House(g, 0.7f, true, tier >= 2 ? 2 : 1, "roof-high-point", false, "banner-red");
                    break;
                case BuildingType.University:
                    House(g, 0.6f, true, tier >= 3 ? 3 : 2, "roof-high-point", tier >= 1, "banner-green");
                    break;
                case BuildingType.Bank:
                    House(g, 0.64f, true, tier >= 2 ? 2 : 1, "roof-point", true, "banner-green");
                    T(g, "lantern", new Vector3(-0.38f, 0f, -0.36f), 0f, 0.35f);
                    break;
                case BuildingType.Forest:
                    {
                        var trees = new[] { "tree", "tree-high", "tree-crooked", "tree-high-round" };
                        for (int i = 0; i < 4; i++)
                            T(g, trees[(seed + i) % 4], new Vector3((VillageArt.Rand(seed, i) - 0.5f) * 0.55f, 0f, (VillageArt.Rand(seed, i + 9) - 0.5f) * 0.55f), VillageArt.Rand(seed, i + 3) * 360f, 0.26f + VillageArt.Rand(seed, i + 20) * 0.08f);
                        C(g, "tree-trunk", new Vector3(0.25f, 0f, -0.28f), 0f, 0.3f);
                    }
                    break;
                case BuildingType.LumberDepot:
                    T(g, "stall", new Vector3(0f, 0f, 0.05f), 90f, 1.1f);
                    for (int i = 0; i < 3; i++) C(g, "tree-log", new Vector3(-0.15f + i * 0.15f, 0.02f, -0.22f), 0f, 0.35f);
                    T(g, "cart", new Vector3(0.3f, 0f, 0.22f), 30f, 0.45f);
                    break;
                case BuildingType.StoneQuarry:
                    C(g, "rocks-large", new Vector3(0.05f, 0f, 0.1f), seed % 360, 0.55f);
                    C(g, "rocks-small", new Vector3(-0.25f, 0f, -0.2f), 0f, 0.5f);
                    T(g, "cart", new Vector3(0.28f, 0f, -0.22f), -30f, 0.4f);
                    break;
                case BuildingType.StoneDepot:
                    T(g, "stall-red", new Vector3(0f, 0f, 0.05f), 90f, 1.1f);
                    C(g, "rocks-small", new Vector3(-0.2f, 0f, -0.25f), 30f, 0.45f);
                    C(g, "rocks-small", new Vector3(0.2f, 0f, -0.25f), 160f, 0.4f);
                    break;
                case BuildingType.GoldMine:
                case BuildingType.IronMine:
                    {
                        C(g, "rocks-large", new Vector3(0f, 0f, 0.12f), 0f, 0.55f);
                        T(g, "cart-high", new Vector3(0.25f, 0f, -0.22f), -20f, 0.4f);
                        T(g, "lantern", new Vector3(-0.3f, 0f, -0.3f), 0f, 0.32f);
                        var ore = t == BuildingType.GoldMine ? VillageArt.GoldC : VillageArt.Iron;
                        for (int i = 0; i < 4; i++)
                            VillageArt.Part(g, VillageArt.Shape.Ball, new Vector3(-0.12f + i * 0.08f, 0.33f + (i % 2) * 0.05f, -0.05f), new Vector3(0.07f, 0.06f, 0.07f), VillageArt.Mat(ore, 0.9f, t == BuildingType.GoldMine ? 0.3f : 0f));
                    }
                    break;
                case BuildingType.IronDepot:
                    T(g, "stall-green", new Vector3(0f, 0f, 0.05f), 90f, 1.1f);
                    T(g, "cart-high", new Vector3(-0.25f, 0f, -0.22f), 20f, 0.4f);
                    break;
                default:
                    Object.Destroy(g.gameObject);
                    return null;
            }
            VillageArt.EmbellishPublic(g, tier, seed);
            return g;
        }

        // ---------- Kuleler ----------
        public static Transform TryBuildTower(Transform parent, TowerType t, int level, bool damaged, Vector3 pos)
        {
            if (!ModelLibrary.Available) return null;
            int tier = VillageArt.Tier(level);
            var root = VillageArt.Group(parent, t.ToString(), pos);
            var g = VillageArt.Group(root, "model", Vector3.zero, 0f, 0.5f);
            float y = 0f;
            void Stack(string piece, float height) { C(g, piece, new Vector3(0f, y, 0f)); y += height; }
            switch (t)
            {
                case TowerType.Wall:
                    g.localScale = Vector3.one * 0.9f;
                    C(g, tier >= 1 ? "wall" : "wall-narrow-wood", Vector3.zero);
                    if (tier >= 3) { C(g, "flag-pennant", new Vector3(0f, 1.31f, 0f)); }
                    return root;
                case TowerType.Archer:
                    Stack("tower-square-base", 1.01f);
                    for (int i = 0; i < (tier >= 3 ? 2 : 1); i++) Stack("tower-square-mid-windows", 1.01f);
                    Stack(tier >= 4 ? "tower-square-top-roof-high-windows" : "tower-square-top-roof", 1.0f);
                    break;
                case TowerType.Mage:
                    Stack("tower-hexagon-base", 1.31f);
                    for (int i = 0; i < 1 + tier / 2; i++) Stack("tower-hexagon-mid", 0.46f);
                    Stack("tower-hexagon-roof", 0.83f);
                    break;
                case TowerType.Elf:
                    Stack("tower-hexagon-base", 1.31f);
                    for (int i = 0; i < tier / 2; i++) Stack("tower-hexagon-mid", 0.46f);
                    Stack("tower-hexagon-top-wood", 0.46f);
                    Stack("tower-hexagon-roof-secondary", 0.6f);
                    break;
                case TowerType.Cannon:
                    Stack("tower-base", 1.31f);
                    for (int i = 0; i < tier / 2; i++) Stack("tower-square-mid", 1.01f);
                    C(g, "siege-ballista", new Vector3(0f, y, 0f), -90f, 0.75f);
                    y += 0.7f;
                    break;
                case TowerType.Ice:
                    Stack("tower-square-base-color", 1.01f);
                    for (int i = 0; i < tier / 2; i++) Stack("tower-square-mid-color", 1.01f);
                    Stack("tower-square-top-color", 0.5f);
                    for (int i = 0; i < 3 + tier; i++)
                    {
                        float a = i * 2.4f;
                        VillageArt.Part(g, VillageArt.Shape.Cone, new Vector3(Mathf.Cos(a) * 0.22f, y + 0.25f, Mathf.Sin(a) * 0.22f), new Vector3(0.14f, 0.5f, 0.14f), VillageArt.Mat(new Color(0.65f, 0.9f, 1f), 0.95f, 0.4f), new Vector3(Mathf.Sin(a) * 12f, 0f, Mathf.Cos(a) * 12f));
                    }
                    y += 0.5f;
                    break;
                default: // Şimşek
                    Stack("tower-hexagon-base", 1.31f);
                    for (int i = 0; i < 1 + tier / 2; i++) Stack("tower-hexagon-mid", 0.46f);
                    VillageArt.Part(g, VillageArt.Shape.Cyl, new Vector3(0f, y + 0.3f, 0f), new Vector3(0.05f, 0.6f, 0.05f), VillageArt.Mat(new Color(0.75f, 0.6f, 0.3f), 0.8f));
                    var orb = VillageArt.Part(g, VillageArt.Shape.Ball, new Vector3(0f, y + 0.7f, 0f), new Vector3(0.28f, 0.28f, 0.28f), VillageArt.Mat(new Color(0.6f, 0.8f, 1f), 0.9f, 2.5f));
                    VillageArt.Animate(orb, ArtMotion.Kind.Bob, 3f, 0.04f);
                    y += 0.9f;
                    break;
            }
            if (tier >= 2) C(g, "flag", new Vector3(0.3f, y - 0.05f, 0.3f));
            if (damaged) VillageArt.Part(root, VillageArt.Shape.Ball, new Vector3(0.15f, 0.25f, -0.2f), new Vector3(0.18f, 0.1f, 0.15f), VillageArt.StoneDark); // yıkıntı taşı
            return root;
        }
    }
}
