using System.Collections.Generic;
using UnityEngine;
using GameCore.Combat;

namespace GameClient
{
    /// <summary>Asker animasyonu ortak arayüzü: kodla çizilen iskelet (CharacterRig) ve hazır model (ModelRig).</summary>
    public interface ISoldierAnim
    {
        void SetMoving(bool moving);
        /// <summary>Ekrandaki gerçek yürüme hızı (kare/sn): yürüme animasyonu ayak kaymadan bu hıza uyar.</summary>
        void SetSpeed(float cellsPerSecond);
        /// <summary>Vuruş/atış animasyonu; animasyon saldırı aralığına sığdırılır. Dönüş: darbenin (ok bırakma) kaç sn sonra olduğu.</summary>
        float TriggerAttack(float intervalSeconds);
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

    /// <summary>
    /// Hazır modelli asker için animasyon (Legacy Animation): bekleme / yürüme / saldırı.
    /// Yürüme hızı ekrandaki gerçek hıza göre ayarlanır (adım boyu = strideCells); saldırı klipleri sırayla değişir
    /// ve saldırı aralığından uzunsa hızlandırılır.
    /// </summary>
    public sealed class ModelRig : MonoBehaviour, ISoldierAnim
    {
        private Animation anim;
        private string idle, walk, current, reload;
        private string[] attacks = new string[0];
        private int atkIdx;
        private float strideCells, walkSpeedNow = 1f, impactFrac;
        private bool moving;
        private float attackEnd = -1f;

        /// <param name="attackClips">virgülle ayrılmış saldırı klipleri (sırayla oynar)</param>
        /// <param name="stride">bir yürüme döngüsünde alınan yol (kare); 0 = hız uyarlaması yok</param>
        /// <param name="impact">darbe anı, klip süresinin oranı</param>
        public void Setup(Animation a, string idleClip, string walkClip, string attackClips, float stride, float impact = 0.45f, string reloadClip = null)
        {
            anim = a; idle = idleClip; walk = walkClip; strideCells = stride; impactFrac = impact;
            reload = reloadClip != null && anim[reloadClip] != null ? reloadClip : null; // atıştan sonra doldurma (Arbaletli)
            if (reload != null) anim[reload].wrapMode = WrapMode.Once;
            var list = new List<string>();
            if (!string.IsNullOrEmpty(attackClips))
                foreach (var raw in attackClips.Split(','))
                {
                    var c = raw.Trim();
                    if (anim[c] != null) { list.Add(c); anim[c].wrapMode = WrapMode.Once; }
                }
            attacks = list.ToArray();
            atkIdx = Random.Range(0, 7);
            if (walk != null && anim[walk] != null) anim[walk].wrapMode = WrapMode.Loop;
            if (idle != null && anim[idle] != null) { anim[idle].wrapMode = WrapMode.Loop; anim.Play(idle); current = idle; }
        }

        public void SetMoving(bool m) => moving = m;

        public void SetSpeed(float v)
        {
            if (anim == null || walk == null || anim[walk] == null) return;
            float target = strideCells > 0.001f ? Mathf.Clamp(v * anim[walk].length / strideCells, 0.35f, 3f) : 1f;
            walkSpeedNow = Mathf.Lerp(walkSpeedNow, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
            anim[walk].speed = walkSpeedNow;
        }

        public float TriggerAttack(float interval)
        {
            if (anim == null || attacks.Length == 0) return 0.15f;
            var clip = attacks[atkIdx++ % attacks.Length];
            var st = anim[clip];
            float len = st.length, maxDur = Mathf.Max(0.3f, interval * 0.92f);
            st.speed = len > maxDur ? len / maxDur : 1f;
            anim.CrossFade(clip, 0.05f, PlayMode.StopAll);
            st.time = 0f;
            float dur = len / st.speed;
            attackEnd = Time.time + dur;
            if (reload != null && maxDur - dur > 0.25f)
            {
                // atıştan hemen sonra yeni oku yerleştirme; kalan süreye sığdırılır
                var rs = anim.CrossFadeQueued(reload, 0.08f, QueueMode.CompleteOthers);
                float rl = anim[reload].length, room = maxDur - dur;
                rs.speed = rl > room ? rl / room : 1f;
                attackEnd += rl / rs.speed;
            }
            current = clip;
            return dur * impactFrac;
        }

        private void Update()
        {
            if (anim == null || Time.time < attackEnd) return;
            string want = moving ? walk : idle;
            if (want != null && want != current && anim[want] != null) { anim.CrossFade(want, 0.18f); current = want; }
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
        public void SetSpeed(float v) { foreach (var r in Rigs) r.SetSpeed(v); }
        public float TriggerAttack(float interval) { float d = 0f; foreach (var r in Rigs) d = Mathf.Max(d, r.TriggerAttack(interval)); return d; }
    }

    /// <summary>
    /// Hazır modellerle askerler, binalar ve kuleler (kullanıcı kararı 16.11.2026: ücretsiz CC0 paketler).
    /// Model bulunamazsa null döner ve VillageArt kodla çizilen görünümü kullanır. Yalnızca görsel.
    /// </summary>
    public static class ModelArt
    {
        private struct Look
        {
            public string Model, Right, Left, Attack, Idle, Walk, Reload;
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
                    // Uzun Kılıç (kullanıcı isteği 27.11.2026): kalkansız, iki elle tutulan uzun kılıç ve iki el saldırıları
                    return new Look { Model = "Knight", Right = L >= 15 ? "sword_2handed_color" : "sword_2handed", Attack = "Melee_2H_Attack_Chop,Melee_2H_Attack_Slice,Melee_2H_Attack_Spin", Idle = "Melee_2H_Idle", Walk = "Walking_A", Height = 0.62f, Tint = w };
                case SoldierType.Archer:
                    return new Look { Model = "Ranger", Left = "bow_withString", Right = null, Attack = "Ranged_Bow_Release", Idle = "Ranged_Bow_Idle", Walk = "Walking_B", Height = 0.6f, Tint = w };
                case SoldierType.Mage:
                    return new Look { Model = "Mage", Right = "staff", Attack = "Ranged_Magic_Shoot", Idle = "Idle_B", Walk = "Walking_A", Height = 0.6f, Tint = w };
                case SoldierType.Goblin:
                    return new Look { Model = "Rogue", Right = "dagger", Left = "dagger", Attack = "Melee_Dualwield_Attack_Stab,Melee_Dualwield_Attack_Slice,Melee_Dualwield_Attack_Chop", Idle = "Idle_A", Walk = "Walking_C", Height = 0.42f, Tint = w };
                case SoldierType.Spearman:
                    return new Look { Model = "Rogue_Hooded", Right = "staff", Left = L >= 10 ? "shield_spikes_color" : "shield_spikes", Attack = "Melee_2H_Attack_Stab,Melee_2H_Attack_Slice", Idle = "Idle_A", Walk = "Walking_A", Height = 0.62f, Tint = new Color(1f, 0.92f, 0.8f) };
                case SoldierType.Giant:
                    return new Look { Model = "Barbarian", Right = "axe_2handed", Attack = "Melee_2H_Attack_Chop,Melee_2H_Attack_Slice,Melee_2H_Attack_Chop,Melee_2H_Attack_Spin", Idle = "Melee_2H_Idle", Walk = "Walking_A", Height = 0.92f, Tint = new Color(1f, 0.9f, 0.85f) };
                case SoldierType.Fairy:
                    return new Look { Model = "Mage", Right = "wand", Attack = "Ranged_Magic_Shoot", Idle = "Idle_B", Walk = "Idle_B", Height = 0.36f, Tint = new Color(1f, 0.8f, 0.95f) };
                case SoldierType.Shieldbearer:
                    return new Look { Model = "Knight", Right = "axe_1handed", Left = "shield_square_color", Attack = "Melee_Block_Attack,Melee_1H_Attack_Chop", Idle = "Melee_Blocking", Walk = "Walking_A", Height = 0.66f, Tint = new Color(0.82f, 0.9f, 1f) };
                case SoldierType.Assassin:
                    return new Look { Model = "Rogue_Hooded", Right = "dagger", Left = "dagger", Attack = "Melee_Dualwield_Attack_Stab,Melee_Dualwield_Attack_Slice", Idle = "Idle_A", Walk = "Walking_C", Height = 0.58f, Tint = new Color(0.45f, 0.4f, 0.55f) };
                case SoldierType.Healer:
                    return new Look { Model = "Mage", Right = "staff", Attack = "Ranged_Magic_Spellcasting", Idle = "Idle_B", Walk = "Walking_A", Height = 0.6f, Tint = w }; // Şaman: kalkana benzeyen kitap kalktı
                case SoldierType.Crossbowman:
                    return new Look { Model = "Ranger", Right = "crossbow_2handed", Attack = "Ranged_2H_Shoot", Reload = "Ranged_2H_Reload", Idle = "Ranged_2H_Aiming", Walk = "Walking_B", Height = 0.6f, Tint = new Color(0.95f, 0.85f, 0.7f) };
                case SoldierType.Bomber:
                    return new Look { Model = "Barbarian", Right = "smokebomb", Attack = "Throw", Idle = "Idle_A", Walk = "Walking_C", Height = 0.6f, Tint = new Color(0.9f, 0.62f, 0.5f) };
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
                var c = ModelLibrary.Spawn(croot, "Kenney/Castle/siege-catapult", new Vector3(0.15f, 0.06f, 0f), 90f, 0.42f); // kullanıcı isteği: biraz büyük
                if (c == null) { Object.Destroy(croot.gameObject); return null; }
                VillageArt.LevelEffectsPublic(croot, L, 1.7f, 0.58f);
                return croot;
            }
            if (t == SoldierType.Cavalry || t == SoldierType.DragonRider) return Mounted(parent, t, L);
            var look = LookOf(t, L);
            if (look.Model == null) return null;
            var root = VillageArt.Group(parent, t.ToString(), Vector3.zero);
            var body = Character(root, look, L, t == SoldierType.Fairy ? 0f : look.Height * 1.25f, t == SoldierType.Archer || t == SoldierType.Mage || t == SoldierType.Fairy || t == SoldierType.Crossbowman || t == SoldierType.Healer ? 0.3f : 0.45f);
            if (body == null) { Object.Destroy(root.gameObject); return null; }
            if (t == SoldierType.Goblin) Goblinize(root, body, look.Height);
            if (t == SoldierType.Healer) Shamanize(body, look.Height);
            if (t == SoldierType.Swordsman) // uzun kılıç: bıçak %35 daha uzun
            {
                var hand = ModelLibrary.FindDeep(body, "handslot.r");
                if (hand != null && hand.childCount > 0) hand.GetChild(hand.childCount - 1).localScale = new Vector3(1.1f, 1.35f, 1.1f);
            }
            if (t == SoldierType.Fairy) VillageArt.FairyWings(root, look.Height);
            VillageArt.LevelEffectsPublic(root, L, t == SoldierType.Giant ? 1.8f : 1f, look.Height * 1.05f);
            return root;
        }

        /// <summary>
        /// Goblin görünümü (kullanıcı isteği 16.11.2026): yeşil deri (baş), yırtık kahverengi giysi, pelerin yok,
        /// sivri büyük kulaklar, iri burun, sarı gözler ve sırtta yağma çuvalı (depolara saldırır).
        /// </summary>
        private static void Goblinize(Transform root, Transform body, float h)
        {
            var skin = new Color(0.42f, 0.78f, 0.28f);
            foreach (var r in body.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name.Contains("Cape")) { r.enabled = false; continue; }
                if (!r.name.StartsWith("Rogue_")) continue; // silahlar değil
                // KayKit dokusu goblin için fazla koyu: düz renkli malzeme (yeşil baş, kahverengi deri giysi)
                bool head = r.name.Contains("Head"), arm = r.name.Contains("Arm"), leg = r.name.Contains("Leg");
                var col = head ? skin : arm ? new Color(0.6f, 0.42f, 0.25f) : leg ? new Color(0.42f, 0.3f, 0.2f) : new Color(0.68f, 0.5f, 0.3f);
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = VillageArt.Mat(col);
                r.sharedMaterials = mats;
            }
            var headBone = ModelLibrary.FindDeep(body, "head");
            if (headBone != null)
            {
                // başa bağlı parçalar: dünya ölçüsünde verilir, kemiğin ölçeğine bölünür
                var g = new GameObject("goblinHead").transform;
                g.SetParent(headBone, false);
                var ls = headBone.lossyScale;
                g.localScale = new Vector3(1f / Mathf.Max(ls.x, 1e-5f), 1f / Mathf.Max(ls.y, 1e-5f), 1f / Mathf.Max(ls.z, 1e-5f));
                float k = h / 0.48f;
                var ear = VillageArt.Mat(skin);
                foreach (var sgn in new[] { -1f, 1f })
                {
                    VillageArt.Part(g, VillageArt.Shape.Cone, new Vector3(0.2f * sgn * k, 0.11f * k, -0.01f * k), new Vector3(0.07f * k, 0.2f * k, 0.04f * k), ear, new Vector3(10f, 0f, -75f * sgn));
                    VillageArt.Part(g, VillageArt.Shape.Ball, new Vector3(0.055f * sgn * k, 0.12f * k, 0.13f * k), Vector3.one * 0.04f * k, VillageArt.Mat(new Color(1f, 0.85f, 0.15f), 0.6f, 1.5f));
                }
                VillageArt.Part(g, VillageArt.Shape.Ball, new Vector3(0f, 0.075f * k, 0.15f * k), new Vector3(0.06f, 0.05f, 0.08f) * k, VillageArt.Mat(skin * 0.85f));
            }
            // sırtta çuval
            float s = h / 0.48f;
            VillageArt.Part(root, VillageArt.Shape.Ball, new Vector3(0f, h * 0.55f, -h * 0.2f), new Vector3(0.17f, 0.19f, 0.13f) * s, VillageArt.Mat(new Color(0.6f, 0.47f, 0.3f)));
            VillageArt.Part(root, VillageArt.Shape.Cyl, new Vector3(0f, h * 0.76f, -h * 0.2f), new Vector3(0.05f, 0.04f, 0.05f) * s, VillageArt.Mat(new Color(0.45f, 0.32f, 0.2f)));
            VillageArt.Part(root, VillageArt.Shape.Ball, new Vector3(0.05f * s, h * 0.8f, -h * 0.2f), Vector3.one * 0.035f * s, VillageArt.Mat(new Color(1f, 0.82f, 0.2f), 0.8f, 0.6f)); // taşan altın
        }

        /// <summary>
        /// Şifacı = şaman (kullanıcı isteği 27.11.2026 "shaman gibi giydir"): büyücü şapkası ve pelerini yok; deri/keten giysi,
        /// tüylü başlık bandı ve diş kolye.
        /// </summary>
        private static void Shamanize(Transform body, float h)
        {
            foreach (var r in body.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.name.StartsWith("Mage_")) continue; // asa değil
                if (r.name.Contains("Hat") || r.name.Contains("Cape")) { r.enabled = false; continue; }
                if (r.name.Contains("Head")) continue; // yüz dokusu kalsın
                var col = r.name.Contains("Arm") ? new Color(0.62f, 0.42f, 0.26f) : r.name.Contains("Leg") ? new Color(0.36f, 0.26f, 0.17f) : new Color(0.5f, 0.56f, 0.3f);
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = VillageArt.Mat(col);
                r.sharedMaterials = mats;
            }
            var headBone = ModelLibrary.FindDeep(body, "head");
            if (headBone == null) return;
            var g = new GameObject("shamanHead").transform;
            g.SetParent(headBone, false);
            var ls = headBone.lossyScale;
            g.localScale = new Vector3(1f / Mathf.Max(ls.x, 1e-5f), 1f / Mathf.Max(ls.y, 1e-5f), 1f / Mathf.Max(ls.z, 1e-5f));
            float k = h / 0.48f;
            VillageArt.Part(g, VillageArt.Shape.Cyl, new Vector3(0f, 0.1f * k, -0.01f * k), new Vector3(0.2f, 0.03f, 0.19f) * k, VillageArt.Mat(new Color(0.7f, 0.2f, 0.15f))); // başlık bandı
            var fc = new[] { new Color(0.95f, 0.95f, 0.9f), new Color(0.2f, 0.55f, 0.75f), new Color(0.95f, 0.75f, 0.2f), new Color(0.2f, 0.55f, 0.75f), new Color(0.95f, 0.95f, 0.9f) };
            for (int i = 0; i < 5; i++) // arkaya yelpaze tüyler
            {
                float a = (i - 2) * 22f;
                VillageArt.Part(g, VillageArt.Shape.Capsule, new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 0.06f * k, 0.17f * k, -0.07f * k), new Vector3(0.03f, 0.13f, 0.012f) * k, VillageArt.Mat(fc[i]), new Vector3(-15f, 0f, -a));
            }
            // diş kolye (boyun çevresi)
            for (int i = 0; i < 7; i++)
            {
                float a = (i - 3) * 22f * Mathf.Deg2Rad;
                VillageArt.Part(g, VillageArt.Shape.Cone, new Vector3(Mathf.Sin(a) * 0.065f * k, -0.1f * k, Mathf.Cos(a) * 0.055f * k), new Vector3(0.018f, 0.035f, 0.018f) * k, VillageArt.Mat(new Color(0.95f, 0.92f, 0.82f)), new Vector3(180f, 0f, 0f));
            }
        }

        /// <summary>KayKit karakteri: boy, silahlar, renk tonu, animasyon (ModelRig holder üstünde). Model yoksa null.</summary>
        private static Transform Character(Transform holder, Look look, int L, float stride, float impact = 0.45f)
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
            holder.gameObject.AddComponent<ModelRig>().Setup(anim, look.Idle, look.Walk, look.Attack, stride * (1f + Mathf.Min(0.12f, (L - 1) * 0.004f)), impact, look.Reload);
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
            float targetH = dragon ? 1.0f : 0.74f; // binicili: binek insan boyuna orantılı (kullanıcı isteği 27.11.2026 "boyları orantılı yap")
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
                if (idle != null && move != null) mountHolder.gameObject.AddComponent<ModelRig>().Setup(ma, idle, move, atk, dragon ? 0f : targetH * 2.4f);
            }

            var riderLook = dragon
                ? new Look { Model = "Barbarian", Right = "axe_1handed", Attack = "Melee_1H_Attack_Chop,Melee_1H_Attack_Slice_Horizontal", Idle = "Jump_Idle", Walk = "Jump_Idle", Height = 0.52f, Tint = new Color(1f, 0.85f, 0.75f) }
                : new Look { Model = "Knight", Right = "sword_1handed", Left = L >= 12 ? "shield_badge_color" : "shield_badge", Attack = "Melee_1H_Attack_Slice_Horizontal,Melee_1H_Attack_Chop", Idle = "Jump_Idle", Walk = "Jump_Idle", Height = 0.52f, Tint = Color.white };
            var rider = VillageArt.Group(root, "rider", new Vector3(0f, (dragon ? DragonSeat : HorseSeat) * targetH, dragon ? DragonSeatZ : HorseSeatZ));
            Character(rider, riderLook, L, 0f);
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

        /// <summary>
        /// Tek hücrelik ev gövdesi: kat başına dört duvar (zemin kapılı), çatı üstte. woodUpper: üst katlar ahşap;
        /// roofTint: çatıyı renklendirir; window: pencere parçası (ör. "-window-round"). Gövde grubunu döndürür.
        /// </summary>
        private static Transform House(Transform g, float s, bool stone, int floors, string roof, bool chimney, string banner,
            bool woodUpper = false, Color? roofTint = null, string window = "-window-shutters", Vector3 offset = default)
        {
            var h = VillageArt.Group(g, "house", offset, 0f, s);
            for (int f = 0; f < floors; f++)
            {
                string w = stone && !(woodUpper && f > 0) ? "wall" : "wall-wood";
                for (int k = 0; k < 4; k++)
                {
                    string piece = f == 0 && k == 1 ? w + "-door" : (k % 2 == 0 ? w + window : w);
                    T(h, piece, new Vector3(0f, f, 0f), k * 90f);
                }
            }
            var r = T(h, roof, new Vector3(0f, floors, 0f));
            if (r != null && roofTint.HasValue) Paint(r, roofTint.Value); // doku rengini ezen düz boya (yeşil doku çarpımla kırmızı olmaz)
            if (chimney) T(h, "chimney", new Vector3(0f, floors, 0f), 180f);
            if (banner != null) { T(h, banner, Vector3.zero, 90f); T(h, banner, Vector3.zero, 180f); }
            return h;
        }

        /// <summary>Kenney parçasını düz renge boyar: dokuyu beyazla değiştirip temel rengi verir (çatı rengi için).</summary>
        private static void Paint(Transform t, Color c)
        {
            var mpb = new MaterialPropertyBlock();
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(mpb);
                mpb.SetTexture("_BaseMap", Texture2D.whiteTexture);
                mpb.SetTexture("_MainTex", Texture2D.whiteTexture);
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_Color", c);
                r.SetPropertyBlock(mpb);
            }
        }

        // kısa yardımcılar (basit şekiller)
        private static GameObject P(Transform g, VillageArt.Shape sh, Vector3 pos, Vector3 size, Color c, Vector3 e = default) => VillageArt.Part(g, sh, pos, size, c, e);
        private static GameObject PM(Transform g, VillageArt.Shape sh, Vector3 pos, Vector3 size, Material m, Vector3 e = default) => VillageArt.Part(g, sh, pos, size, m, e);

        /// <summary>Üst üste dizilmiş kütükler (3-2-1 piramit), uçları açık renk halkalı.</summary>
        private static void LogPile(Transform g, Vector3 at, int rows, float len)
        {
            for (int r = 0; r < rows; r++)
                for (int k = 0; k < rows - r; k++)
                {
                    var pos = at + new Vector3(0f, 0.045f + r * 0.075f, (k - (rows - r - 1) * 0.5f) * 0.085f);
                    P(g, VillageArt.Shape.Cyl, pos, new Vector3(0.08f, len * 0.5f, 0.08f), VillageArt.Wood, new Vector3(0f, 0f, 90f));
                    foreach (var sx in new[] { -1f, 1f })
                        P(g, VillageArt.Shape.Cyl, pos + new Vector3(sx * len * 0.5f, 0f, 0f), new Vector3(0.065f, 0.006f, 0.065f), VillageArt.WoodLight, new Vector3(0f, 0f, 90f));
                }
        }

        /// <summary>Fener direği: tahta direk + sıcak ışıklı fener.</summary>
        private static void LampPost(Transform g, Vector3 at, int seed)
        {
            P(g, VillageArt.Shape.Cyl, at + new Vector3(0f, 0.16f, 0f), new Vector3(0.025f, 0.32f, 0.025f), VillageArt.WoodDark);
            P(g, VillageArt.Shape.Box, at + new Vector3(0.04f, 0.31f, 0f), new Vector3(0.08f, 0.015f, 0.015f), VillageArt.WoodDark);
            var lamp = PM(g, VillageArt.Shape.Box, at + new Vector3(0.075f, 0.27f, 0f), new Vector3(0.045f, 0.06f, 0.045f), VillageArt.Mat(new Color(1f, 0.82f, 0.45f), 0.4f, 2.2f));
            VillageArt.Animate(lamp, ArtMotion.Kind.Bob, 2.5f, 0.004f, seed);
        }

        /// <summary>Çit: önde alçak tahta çit (ortada giriş aralığı).</summary>
        private static void YardFence(Transform g, float z, float half)
        {
            for (float x = -half; x <= half + 0.001f; x += 0.15f)
            {
                if (Mathf.Abs(x) < 0.12f) continue; // giriş aralığı
                P(g, VillageArt.Shape.Box, new Vector3(x, 0.06f, z), new Vector3(0.025f, 0.12f, 0.025f), VillageArt.WoodLight);
            }
            foreach (var sx in new[] { -1f, 1f })
                P(g, VillageArt.Shape.Box, new Vector3(sx * (half + 0.12f) * 0.5f, 0.08f, z), new Vector3(half - 0.12f, 0.018f, 0.015f), VillageArt.WoodLight);
        }

        /// <summary>
        /// Binalar (kullanıcı isteği 27.11.2026: "tüm yapıların görünüş ve yapısını değiştir, daha güzel yapılar"):
        /// her bina kendine özgü bir yapı; seviye (tier) arttıkça kat, süs ve ayrıntı eklenir. Ön yüz -z (kamera tarafı).
        /// </summary>
        public static Transform TryBuildBuilding(Transform parent, BuildingType t, int level, int seed, Vector3 pos)
        {
            if (!ModelLibrary.Available) return null;
            int tier = VillageArt.Tier(level);
            var g = VillageArt.Group(parent, t.ToString(), pos);
            switch (t)
            {
                case BuildingType.VillagerHouse:
                {
                    // köylü evi (kullanıcı isteği 27.11.2026 "basitleştir ve küçült"): tek katlı küçük kulübe, kırmızı çatı,
                    // kapı önünde tek çiçek saksısı; yüksek seviyede yalnızca taş duvar ve mavi çatı
                    var roofC = tier >= 3 ? new Color(0.36f, 0.48f, 0.78f) : new Color(0.82f, 0.36f, 0.24f);
                    House(g, 0.42f, tier >= 2, 1, "roof-point", false, null, false, roofC, offset: new Vector3(0f, 0f, 0.04f));
                    P(g, VillageArt.Shape.Box, new Vector3(0.13f, 0.03f, -0.2f), new Vector3(0.07f, 0.05f, 0.05f), VillageArt.WoodDark);
                    P(g, VillageArt.Shape.Ball, new Vector3(0.13f, 0.07f, -0.2f), new Vector3(0.07f, 0.05f, 0.05f), new Color(0.95f, 0.45f, 0.6f));
                    break;
                }
                case BuildingType.Barracks:
                {
                    // kışla (kullanıcı isteği 27.11.2026 "daha askeri bir yapı"): mazgallı taş kale; önde demir parmaklıklı kapı
                    // kulesi, çapraz kılıçlı kalkan arması, iki yuvarlak köşe burcu ve dalgalanan kırmızı sancaklar;
                    // avluda okçuluk hedefi ve mızrak rafı; yüksek seviyede burçlara kırmızı külah ve kalkan yığını
                    var stone = VillageArt.Stone;
                    var dark = new Color(0.06f, 0.05f, 0.05f);
                    var iron = VillageArt.Mat(VillageArt.Iron, 0.85f);
                    var red = new Color(0.72f, 0.14f, 0.12f);
                    var gold = VillageArt.Mat(VillageArt.GoldC, 0.88f, 0.3f);
                    float kh = tier >= 2 ? 0.56f : 0.44f;
                    P(g, VillageArt.Shape.Box, new Vector3(0f, 0.02f, 0.06f), new Vector3(0.88f, 0.04f, 0.8f), VillageArt.StoneDark); // taban
                    // ana kale gövdesi + mazgallar
                    P(g, VillageArt.Shape.Box, new Vector3(0f, kh * 0.5f, 0.14f), new Vector3(0.6f, kh, 0.44f), stone);
                    P(g, VillageArt.Shape.Box, new Vector3(0f, kh * 0.3f, 0.14f), new Vector3(0.61f, 0.02f, 0.45f), VillageArt.StoneDark); // taş bant
                    for (int i = 0; i < 5; i++)
                        foreach (var z in new[] { -0.07f, 0.35f })
                            P(g, VillageArt.Shape.Box, new Vector3(-0.26f + i * 0.13f, kh + 0.035f, z), new Vector3(0.08f, 0.07f, 0.04f), VillageArt.StoneLight);
                    for (int i = 0; i < 3; i++)
                        foreach (var x in new[] { -0.28f, 0.28f })
                            P(g, VillageArt.Shape.Box, new Vector3(x, kh + 0.035f, 0.04f + i * 0.13f), new Vector3(0.04f, 0.07f, 0.08f), VillageArt.StoneLight);
                    foreach (var x in new[] { -0.2f, 0.2f }) // ok mazgalları (dar pencereler)
                        P(g, VillageArt.Shape.Box, new Vector3(x, kh * 0.62f, -0.083f), new Vector3(0.025f, 0.1f, 0.006f), dark);
                    // kapı kulesi
                    float gh = kh + 0.1f;
                    P(g, VillageArt.Shape.Box, new Vector3(0f, gh * 0.5f, -0.12f), new Vector3(0.26f, gh, 0.16f), VillageArt.StoneLight);
                    for (int i = 0; i < 3; i++) P(g, VillageArt.Shape.Box, new Vector3(-0.09f + i * 0.09f, gh + 0.035f, -0.18f), new Vector3(0.06f, 0.07f, 0.04f), VillageArt.StoneLight);
                    P(g, VillageArt.Shape.Box, new Vector3(0f, 0.09f, -0.201f), new Vector3(0.13f, 0.18f, 0.006f), dark); // kapı boşluğu
                    P(g, VillageArt.Shape.Cyl, new Vector3(0f, 0.18f, -0.201f), new Vector3(0.13f, 0.006f, 0.13f), dark, new Vector3(90f, 0f, 0f)); // kemer
                    for (int i = 0; i < 4; i++) PM(g, VillageArt.Shape.Box, new Vector3(-0.045f + i * 0.03f, 0.1f, -0.206f), new Vector3(0.008f, 0.2f, 0.006f), iron); // parmaklık
                    foreach (var y in new[] { 0.06f, 0.14f }) PM(g, VillageArt.Shape.Box, new Vector3(0f, y, -0.207f), new Vector3(0.12f, 0.008f, 0.006f), iron);
                    // kalkan arması + çapraz kılıçlar
                    float cy = gh - 0.12f;
                    foreach (var a in new[] { 35f, -35f }) PM(g, VillageArt.Shape.Box, new Vector3(0f, cy, -0.204f), new Vector3(0.012f, 0.17f, 0.005f), iron, new Vector3(0f, 0f, a));
                    P(g, VillageArt.Shape.Cyl, new Vector3(0f, cy, -0.209f), new Vector3(0.1f, 0.006f, 0.1f), red, new Vector3(90f, 0f, 0f));
                    PM(g, VillageArt.Shape.Ball, new Vector3(0f, cy, -0.213f), new Vector3(0.035f, 0.035f, 0.02f), gold);
                    // yuvarlak köşe burçları + sancaklar
                    float th = kh + (tier >= 3 ? 0.2f : 0.14f);
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        var tp = new Vector3(sx * 0.33f, 0f, -0.08f);
                        P(g, VillageArt.Shape.Cyl, tp + new Vector3(0f, th * 0.5f, 0f), new Vector3(0.17f, th, 0.17f), stone);
                        P(g, VillageArt.Shape.Cyl, tp + new Vector3(0f, th + 0.02f, 0f), new Vector3(0.2f, 0.04f, 0.2f), VillageArt.StoneLight);
                        P(g, VillageArt.Shape.Box, tp + new Vector3(0f, th * 0.6f, -0.085f), new Vector3(0.02f, 0.08f, 0.006f), dark);
                        if (tier >= 3) P(g, VillageArt.Shape.Cone, tp + new Vector3(0f, th + 0.13f, 0f), new Vector3(0.22f, 0.18f, 0.22f), red);
                        else for (int k = 0; k < 4; k++) P(g, VillageArt.Shape.Box, tp + new Vector3(Mathf.Cos(k * 1.571f) * 0.08f, th + 0.06f, Mathf.Sin(k * 1.571f) * 0.08f), new Vector3(0.045f, 0.05f, 0.045f), VillageArt.StoneLight);
                        float poleY = th + (tier >= 3 ? 0.22f : 0.04f);
                        P(g, VillageArt.Shape.Cyl, tp + new Vector3(0f, poleY + 0.12f, 0f), new Vector3(0.012f, 0.24f, 0.012f), VillageArt.WoodDark);
                        if (tier >= 3) PM(g, VillageArt.Shape.Ball, tp + new Vector3(0f, poleY + 0.25f, 0f), Vector3.one * 0.025f, gold);
                        var flag = VillageArt.Group(g, "flag", tp + new Vector3(sx * 0.055f, poleY + 0.19f, 0f));
                        P(flag, VillageArt.Shape.Box, Vector3.zero, new Vector3(0.1f, 0.07f, 0.005f), red);
                        VillageArt.Animate(flag.gameObject, ArtMotion.Kind.Flutter, 2.2f, 0.1f, seed + (int)sx);
                        // ön duvarda asılı sancak
                        P(g, VillageArt.Shape.Box, new Vector3(sx * 0.19f, kh - 0.13f, -0.084f), new Vector3(0.07f, 0.2f, 0.005f), red);
                        PM(g, VillageArt.Shape.Box, new Vector3(sx * 0.19f, kh - 0.13f, -0.088f), new Vector3(0.012f, 0.18f, 0.003f), gold);
                    }
                    // okçuluk hedefi
                    var tg = new Vector3(-0.3f, 0f, -0.34f);
                    foreach (var dx in new[] { -0.04f, 0.04f }) P(g, VillageArt.Shape.Box, tg + new Vector3(dx, 0.08f, 0.02f), new Vector3(0.015f, 0.16f, 0.015f), VillageArt.WoodDark, new Vector3(-12f, 0f, 0f));
                    P(g, VillageArt.Shape.Cyl, tg + new Vector3(0f, 0.14f, 0f), new Vector3(0.15f, 0.02f, 0.15f), new Color(0.88f, 0.78f, 0.5f), new Vector3(78f, 0f, 0f));
                    P(g, VillageArt.Shape.Cyl, tg + new Vector3(0f, 0.141f, -0.012f), new Vector3(0.1f, 0.003f, 0.1f), red, new Vector3(78f, 0f, 0f));
                    P(g, VillageArt.Shape.Cyl, tg + new Vector3(0f, 0.142f, -0.015f), new Vector3(0.045f, 0.003f, 0.045f), Color.white, new Vector3(78f, 0f, 0f));
                    // mızrak rafı
                    var rack = new Vector3(0.3f, 0f, -0.34f);
                    P(g, VillageArt.Shape.Box, rack + new Vector3(0f, 0.14f, 0f), new Vector3(0.18f, 0.02f, 0.03f), VillageArt.WoodDark);
                    foreach (var dx in new[] { -0.08f, 0.08f }) P(g, VillageArt.Shape.Box, rack + new Vector3(dx, 0.09f, 0f), new Vector3(0.02f, 0.18f, 0.02f), VillageArt.WoodDark);
                    for (int i = 0; i < 3; i++)
                    {
                        P(g, VillageArt.Shape.Cyl, rack + new Vector3(-0.05f + i * 0.05f, 0.16f, 0.01f), new Vector3(0.012f, 0.32f, 0.012f), VillageArt.WoodLight);
                        PM(g, VillageArt.Shape.Cone, rack + new Vector3(-0.05f + i * 0.05f, 0.34f, 0.01f), new Vector3(0.03f, 0.05f, 0.03f), iron);
                    }
                    if (tier >= 2) // kalkan yığını
                        for (int i = 0; i < 2; i++)
                            P(g, VillageArt.Shape.Cyl, new Vector3(0.14f + i * 0.04f, 0.06f, -0.3f + i * 0.02f), new Vector3(0.1f, 0.01f, 0.1f), i == 0 ? red : new Color(0.25f, 0.35f, 0.6f), new Vector3(70f, 20f, 0f));
                    break;
                }
                case BuildingType.University:
                {
                    // Akademi = okul (kullanıcı isteği 27.11.2026 "akademi okul gibi görünsün"): geniş kırmızı tuğla okul binası,
                    // beyaz silmeler, sıra pencereler; ortada saatli giriş ve altın çanlı çan kulesi; bayrak direği, kara tahta
                    var brick = new Color(0.72f, 0.33f, 0.24f);
                    var white = new Color(0.95f, 0.93f, 0.88f);
                    var roofC = new Color(0.34f, 0.37f, 0.45f);
                    var gold = VillageArt.Mat(VillageArt.GoldC, 0.9f, 0.35f);
                    float bh = tier >= 2 ? 0.44f : 0.32f; // 2. kat
                    P(g, VillageArt.Shape.Box, new Vector3(0f, 0.02f, 0.1f), new Vector3(0.86f, 0.04f, 0.5f), VillageArt.StoneLight); // taban
                    P(g, VillageArt.Shape.Box, new Vector3(0f, bh * 0.5f, 0.1f), new Vector3(0.8f, bh, 0.44f), brick);
                    foreach (var x in new[] { -0.4f, 0.4f }) P(g, VillageArt.Shape.Box, new Vector3(x, bh * 0.5f, -0.12f), new Vector3(0.03f, bh, 0.03f), white); // köşe silmeleri
                    P(g, VillageArt.Shape.Box, new Vector3(0f, bh, 0.1f), new Vector3(0.84f, 0.025f, 0.48f), white); // saçak
                    P(g, VillageArt.Shape.Roof, new Vector3(0f, bh + 0.11f, 0.1f), new Vector3(0.54f, 0.22f, 0.9f), roofC, new Vector3(0f, 90f, 0f));
                    var glass = VillageArt.Mat(new Color(0.55f, 0.75f, 0.9f), 0.8f, tier >= 3 ? 0.4f : 0f);
                    int rowsW = tier >= 2 ? 2 : 1;
                    for (int f = 0; f < rowsW; f++)
                        foreach (var x in new[] { -0.31f, -0.18f, 0.18f, 0.31f })
                        {
                            float y = 0.16f + f * 0.17f;
                            P(g, VillageArt.Shape.Box, new Vector3(x, y, -0.123f), new Vector3(0.09f, 0.11f, 0.008f), white);
                            PM(g, VillageArt.Shape.Box, new Vector3(x, y, -0.128f), new Vector3(0.065f, 0.085f, 0.006f), glass);
                        }
                    // giriş + çan kulesi
                    float th = bh + 0.16f;
                    P(g, VillageArt.Shape.Box, new Vector3(0f, th * 0.5f, -0.14f), new Vector3(0.2f, th, 0.17f), brick);
                    P(g, VillageArt.Shape.Box, new Vector3(0f, th, -0.14f), new Vector3(0.23f, 0.025f, 0.2f), white);
                    P(g, VillageArt.Shape.Box, new Vector3(0f, 0.1f, -0.227f), new Vector3(0.1f, 0.17f, 0.01f), VillageArt.WoodDark); // kapı
                    P(g, VillageArt.Shape.Box, new Vector3(0f, 0.19f, -0.229f), new Vector3(0.12f, 0.02f, 0.01f), white);
                    P(g, VillageArt.Shape.Box, new Vector3(0f, 0.015f, -0.28f), new Vector3(0.2f, 0.03f, 0.09f), VillageArt.StoneLight); // basamak
                    P(g, VillageArt.Shape.Cyl, new Vector3(0f, th - 0.07f, -0.228f), new Vector3(0.1f, 0.006f, 0.1f), white, new Vector3(90f, 0f, 0f)); // saat
                    P(g, VillageArt.Shape.Box, new Vector3(0f, th - 0.055f, -0.233f), new Vector3(0.008f, 0.035f, 0.004f), VillageArt.Dark);
                    P(g, VillageArt.Shape.Box, new Vector3(0.012f, th - 0.07f, -0.233f), new Vector3(0.025f, 0.007f, 0.004f), VillageArt.Dark);
                    foreach (var x in new[] { -0.08f, 0.08f }) // çan kulesi direkleri
                        foreach (var z in new[] { -0.21f, -0.07f })
                            P(g, VillageArt.Shape.Box, new Vector3(x, th + 0.08f, z), new Vector3(0.022f, 0.15f, 0.022f), white);
                    P(g, VillageArt.Shape.Pyramid, new Vector3(0f, th + 0.23f, -0.14f), new Vector3(0.24f, 0.16f, 0.22f), roofC);
                    var bell = VillageArt.Group(g, "bell", new Vector3(0f, th + 0.14f, -0.14f));
                    PM(bell, VillageArt.Shape.Cone, new Vector3(0f, -0.04f, 0f), new Vector3(0.07f, 0.07f, 0.07f), gold);
                    VillageArt.Animate(bell.gameObject, ArtMotion.Kind.Sway, 1.4f, 0.12f, seed);
                    PM(g, VillageArt.Shape.Ball, new Vector3(0f, th + 0.33f, -0.14f), Vector3.one * 0.035f, gold); // tepe topu
                    // bayrak direği
                    var fp = new Vector3(-0.38f, 0f, -0.32f);
                    P(g, VillageArt.Shape.Cyl, fp + new Vector3(0f, 0.3f, 0f), new Vector3(0.016f, 0.6f, 0.016f), white);
                    var flag = VillageArt.Group(g, "flag", fp + new Vector3(0.065f, 0.54f, 0f));
                    P(flag, VillageArt.Shape.Box, Vector3.zero, new Vector3(0.12f, 0.075f, 0.005f), new Color(0.85f, 0.18f, 0.16f));
                    VillageArt.Animate(flag.gameObject, ArtMotion.Kind.Flutter, 2f, 0.1f, seed);
                    // kara tahta (sehpa üstünde)
                    var bb = new Vector3(0.3f, 0f, -0.33f);
                    foreach (var dx in new[] { -0.07f, 0.07f }) P(g, VillageArt.Shape.Box, bb + new Vector3(dx, 0.1f, 0.02f), new Vector3(0.015f, 0.2f, 0.015f), VillageArt.WoodDark, new Vector3(-10f, 0f, 0f));
                    P(g, VillageArt.Shape.Box, bb + new Vector3(0f, 0.15f, 0f), new Vector3(0.2f, 0.13f, 0.012f), VillageArt.Wood, new Vector3(-10f, 0f, 0f));
                    P(g, VillageArt.Shape.Box, bb + new Vector3(0f, 0.15f, -0.007f), new Vector3(0.17f, 0.1f, 0.006f), new Color(0.16f, 0.3f, 0.22f), new Vector3(-10f, 0f, 0f));
                    for (int i = 0; i < 3; i++) P(g, VillageArt.Shape.Box, bb + new Vector3(-0.03f + i * 0.015f, 0.17f - i * 0.025f, -0.012f), new Vector3(0.09f - i * 0.02f, 0.006f, 0.003f), white, new Vector3(-10f, 0f, 0f)); // tebeşir yazısı
                    var bc = new[] { new Color(0.7f, 0.2f, 0.2f), new Color(0.2f, 0.4f, 0.7f), new Color(0.25f, 0.55f, 0.3f) };
                    for (int i = 0; i < 3; i++) P(g, VillageArt.Shape.Box, new Vector3(0.14f, 0.015f + i * 0.026f, -0.36f), new Vector3(0.1f - i * 0.01f, 0.024f, 0.075f), bc[i], new Vector3(0f, i * 14f, 0f)); // kitap yığını
                    if (tier >= 2) LampPost(g, new Vector3(-0.4f, 0f, -0.08f), seed);
                    break;
                }
                case BuildingType.Bank:
                {
                    // Banka (kullanıcı isteği 27.11.2026 "daha gold renkli, daha ihtişamlı"): üç basamaklı mermer kaide,
                    // altın başlıklı altı sütunlu revak, altın frizli alınlık ve sikke arması, altın kapı, altın kubbe;
                    // önde altın küreli kaideler ve sikke kuleleri; yüksek seviyede köşe kubbecikleri
                    var marble = new Color(0.95f, 0.92f, 0.86f);
                    var gold = VillageArt.Mat(VillageArt.GoldC, 0.92f, 0.45f);
                    float bh = tier >= 2 ? 0.46f : 0.36f;
                    for (int i = 0; i < 3; i++) // basamaklı kaide
                        P(g, VillageArt.Shape.Box, new Vector3(0f, 0.015f + i * 0.03f, 0.05f + i * 0.03f), new Vector3(0.9f - i * 0.06f, 0.03f, 0.84f - i * 0.06f), i == 2 ? marble : VillageArt.StoneLight);
                    float y0 = 0.09f, top = y0 + bh;
                    P(g, VillageArt.Shape.Box, new Vector3(0f, y0 + bh * 0.5f, 0.16f), new Vector3(0.66f, bh, 0.5f), marble); // gövde
                    PM(g, VillageArt.Shape.Box, new Vector3(0f, y0 + bh * 0.42f, -0.092f), new Vector3(0.14f, bh * 0.7f, 0.012f), gold); // altın kapı
                    PM(g, VillageArt.Shape.Box, new Vector3(0f, top - 0.02f, 0.16f), new Vector3(0.7f, 0.04f, 0.54f), gold); // altın friz
                    for (int i = 0; i < 6; i++) // revak sütunları
                    {
                        float x = -0.3f + i * 0.12f;
                        P(g, VillageArt.Shape.Cyl, new Vector3(x, y0 + bh * 0.5f, -0.2f), new Vector3(0.055f, bh - 0.04f, 0.055f), marble);
                        PM(g, VillageArt.Shape.Box, new Vector3(x, top - 0.02f, -0.2f), new Vector3(0.075f, 0.03f, 0.075f), gold);  // başlık
                        PM(g, VillageArt.Shape.Box, new Vector3(x, y0 + 0.012f, -0.2f), new Vector3(0.07f, 0.024f, 0.07f), gold);  // ayak
                    }
                    P(g, VillageArt.Shape.Box, new Vector3(0f, top + 0.025f, -0.17f), new Vector3(0.76f, 0.05f, 0.16f), marble); // arşitrav
                    PM(g, VillageArt.Shape.Box, new Vector3(0f, top + 0.025f, -0.252f), new Vector3(0.76f, 0.018f, 0.006f), gold);
                    P(g, VillageArt.Shape.Roof, new Vector3(0f, top + 0.12f, -0.17f), new Vector3(0.78f, 0.15f, 0.16f), marble); // alınlık
                    PM(g, VillageArt.Shape.Cyl, new Vector3(0f, top + 0.1f, -0.252f), new Vector3(0.085f, 0.006f, 0.085f), gold, new Vector3(90f, 0f, 0f)); // sikke arması
                    P(g, VillageArt.Shape.Cyl, new Vector3(0f, top + 0.1f, -0.256f), new Vector3(0.05f, 0.004f, 0.05f), new Color(0.85f, 0.62f, 0.12f), new Vector3(90f, 0f, 0f));
                    PM(g, VillageArt.Shape.Ball, new Vector3(0f, top + 0.21f, -0.17f), Vector3.one * 0.04f, gold); // alınlık tepe süsü
                    // altın kubbe (her seviyede; yüksek seviyede büyür) + tepe süsü
                    float dr = tier >= 3 ? 0.36f : 0.3f;
                    P(g, VillageArt.Shape.Cyl, new Vector3(0f, top + 0.04f, 0.2f), new Vector3(dr * 0.85f, 0.08f, dr * 0.85f), marble);
                    PM(g, VillageArt.Shape.Ball, new Vector3(0f, top + 0.09f, 0.2f), new Vector3(dr, dr * 0.85f, dr), gold);
                    PM(g, VillageArt.Shape.Cone, new Vector3(0f, top + 0.09f + dr * 0.48f, 0.2f), new Vector3(0.04f, 0.09f, 0.04f), gold);
                    if (tier >= 3)
                        foreach (var cx in new[] { -0.29f, 0.29f })
                            foreach (var cz in new[] { -0.05f, 0.37f })
                                PM(g, VillageArt.Shape.Ball, new Vector3(cx, top + 0.03f, cz), new Vector3(0.09f, 0.08f, 0.09f), gold); // köşe kubbecikleri
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        // altın küreli mermer kaide
                        var ped = new Vector3(sx * 0.4f, 0f, -0.33f);
                        P(g, VillageArt.Shape.Box, ped + new Vector3(0f, 0.07f, 0f), new Vector3(0.08f, 0.14f, 0.08f), marble);
                        PM(g, VillageArt.Shape.Box, ped + new Vector3(0f, 0.145f, 0f), new Vector3(0.09f, 0.015f, 0.09f), gold);
                        PM(g, VillageArt.Shape.Ball, ped + new Vector3(0f, 0.19f, 0f), Vector3.one * 0.07f, gold);
                        // sikke kuleleri
                        for (int i = 0; i < 3 + tier; i++)
                            PM(g, VillageArt.Shape.Cyl, new Vector3(sx * 0.4f + (i % 2) * 0.008f, 0.012f + i * 0.022f, -0.18f), new Vector3(0.07f, 0.01f, 0.07f), gold);
                    }
                    break;
                }
                case BuildingType.Forest:
                {
                    var trees = new[] { "tree", "tree-high", "tree-crooked", "tree-high-round" };
                    for (int i = 0; i < 4; i++)
                        T(g, trees[(seed + i) % 4], new Vector3((VillageArt.Rand(seed, i) - 0.5f) * 0.55f, 0f, VillageArt.Rand(seed, i + 9) * 0.6f - 0.1f), VillageArt.Rand(seed, i + 3) * 360f, 0.26f + VillageArt.Rand(seed, i + 20) * 0.08f);
                    var stump = new Vector3(-0.28f, 0f, -0.3f); // oduncu köşesi: balta saplanmış kütük + kesilmiş odun
                    P(g, VillageArt.Shape.Cyl, stump + new Vector3(0f, 0.05f, 0f), new Vector3(0.14f, 0.05f, 0.14f), VillageArt.Wood);
                    P(g, VillageArt.Shape.Cyl, stump + new Vector3(0f, 0.101f, 0f), new Vector3(0.12f, 0.003f, 0.12f), VillageArt.WoodLight);
                    P(g, VillageArt.Shape.Cyl, stump + new Vector3(0.02f, 0.17f, 0f), new Vector3(0.012f, 0.08f, 0.012f), VillageArt.WoodDark, new Vector3(0f, 0f, -20f));
                    PM(g, VillageArt.Shape.Box, stump + new Vector3(-0.005f, 0.11f, 0f), new Vector3(0.05f, 0.035f, 0.008f), VillageArt.Mat(VillageArt.Iron, 0.9f), new Vector3(0f, 0f, -20f));
                    LogPile(g, new Vector3(0.22f, 0f, -0.32f), 2 + Mathf.Min(1, tier / 2), 0.22f);
                    break;
                }
                case BuildingType.LumberDepot:
                {
                    // kereste deposu: dört direkli eğik çatılı sundurma, altında kütük ve kalas yığınları, döner testereli tezgâh
                    float roofY = 0.46f + tier * 0.02f;
                    foreach (var x in new[] { -0.36f, 0.36f })
                        foreach (var z in new[] { -0.3f, 0.32f })
                        {
                            float hh = roofY - (z > 0 ? 0f : 0.08f);
                            P(g, VillageArt.Shape.Box, new Vector3(x, hh * 0.5f, z), new Vector3(0.04f, hh, 0.04f), VillageArt.WoodDark);
                        }
                    P(g, VillageArt.Shape.Box, new Vector3(0f, roofY - 0.04f, 0.01f), new Vector3(0.86f, 0.03f, 0.74f), new Color(0.48f, 0.3f, 0.17f), new Vector3(-6.5f, 0f, 0f));
                    for (int i = 0; i < 6; i++) // çatı tahtası çizgileri
                        P(g, VillageArt.Shape.Box, new Vector3(-0.36f + i * 0.144f, roofY - 0.02f, 0.01f), new Vector3(0.01f, 0.012f, 0.74f), VillageArt.WoodDark, new Vector3(-6.5f, 0f, 0f));
                    LogPile(g, new Vector3(-0.14f, 0f, 0.12f), 3 + Mathf.Min(1, tier / 2), 0.36f);
                    for (int i = 0; i < 4 + tier; i++) // kalaslar
                        P(g, VillageArt.Shape.Box, new Vector3(0.24f, 0.015f + i * 0.025f, 0.12f), new Vector3(0.16f, 0.02f, 0.4f), Color.Lerp(VillageArt.WoodLight, VillageArt.Wood, (i % 3) * 0.3f));
                    var bench = new Vector3(0.05f, 0f, -0.3f);
                    P(g, VillageArt.Shape.Box, bench + new Vector3(0f, 0.1f, 0f), new Vector3(0.28f, 0.03f, 0.12f), VillageArt.Wood);
                    foreach (var dx in new[] { -0.11f, 0.11f }) P(g, VillageArt.Shape.Box, bench + new Vector3(dx, 0.05f, 0f), new Vector3(0.025f, 0.1f, 0.1f), VillageArt.WoodDark);
                    var saw = PM(g, VillageArt.Shape.Cyl, bench + new Vector3(0f, 0.15f, 0f), new Vector3(0.11f, 0.004f, 0.11f), VillageArt.Mat(new Color(0.8f, 0.82f, 0.86f), 0.9f), new Vector3(90f, 0f, 0f));
                    VillageArt.Animate(saw, ArtMotion.Kind.Wheel, 4f, 0f);
                    T(g, "cart", new Vector3(-0.36f, 0f, -0.36f), 30f, 0.35f);
                    break;
                }
                case BuildingType.StoneQuarry:
                {
                    // taş ocağı: kaya yüzü, kesilmiş bloklar, iskele, kazma, el arabası
                    C(g, "rocks-large", new Vector3(0.05f, 0f, 0.16f), seed % 360, 0.55f);
                    C(g, "rocks-small", new Vector3(-0.3f, 0f, 0.05f), 40f, 0.45f);
                    for (int i = 0; i < 3 + Mathf.Min(2, tier); i++)
                        P(g, VillageArt.Shape.Box, new Vector3(-0.25f + (i % 3) * 0.13f, 0.045f + (i / 3) * 0.09f, -0.26f), new Vector3(0.11f, 0.09f, 0.11f), Color.Lerp(VillageArt.StoneLight, VillageArt.Stone, (i % 2) * 0.5f));
                    foreach (var x in new[] { 0.18f, 0.38f }) P(g, VillageArt.Shape.Box, new Vector3(x, 0.22f, -0.05f), new Vector3(0.025f, 0.44f, 0.025f), VillageArt.WoodLight);
                    P(g, VillageArt.Shape.Box, new Vector3(0.28f, 0.3f, -0.05f), new Vector3(0.24f, 0.02f, 0.1f), VillageArt.Wood);
                    P(g, VillageArt.Shape.Cyl, new Vector3(0.12f, 0.08f, -0.33f), new Vector3(0.012f, 0.09f, 0.012f), VillageArt.WoodDark, new Vector3(0f, 0f, 60f));
                    PM(g, VillageArt.Shape.Box, new Vector3(0.05f, 0.12f, -0.33f), new Vector3(0.012f, 0.1f, 0.015f), VillageArt.Mat(VillageArt.Iron, 0.85f), new Vector3(0f, 0f, -25f));
                    T(g, "cart", new Vector3(0.33f, 0f, -0.32f), -30f, 0.35f);
                    break;
                }
                case BuildingType.StoneDepot:
                {
                    // taş deposu: alçak taş duvarlı avlu, düzgün taş blok yığınları, tahta vinç ve sallanan asılı blok
                    var wallC = VillageArt.Stone;
                    P(g, VillageArt.Shape.Box, new Vector3(0f, 0.07f, 0.4f), new Vector3(0.84f, 0.14f, 0.07f), wallC);
                    foreach (var x in new[] { -0.41f, 0.41f }) P(g, VillageArt.Shape.Box, new Vector3(x, 0.07f, 0.06f), new Vector3(0.07f, 0.14f, 0.7f), wallC);
                    int layers = 2 + Mathf.Min(2, tier);
                    for (int l = 0; l < layers; l++)
                        for (int k = 0; k < 6 - l; k++)
                            P(g, VillageArt.Shape.Box, new Vector3(-0.28f + (k % 3) * 0.13f + l * 0.03f, 0.04f + l * 0.08f, 0.2f - (k / 3) * 0.14f), new Vector3(0.12f, 0.075f, 0.12f), Color.Lerp(VillageArt.StoneLight, VillageArt.Stone, ((k + l) % 3) * 0.35f));
                    var crane = new Vector3(0.24f, 0f, -0.1f);
                    P(g, VillageArt.Shape.Box, crane + new Vector3(0f, 0.32f, 0f), new Vector3(0.04f, 0.64f, 0.04f), VillageArt.WoodDark);
                    P(g, VillageArt.Shape.Box, crane + new Vector3(-0.12f, 0.62f, 0f), new Vector3(0.3f, 0.035f, 0.035f), VillageArt.WoodDark);
                    P(g, VillageArt.Shape.Box, crane + new Vector3(0.05f, 0.5f, 0f), new Vector3(0.03f, 0.2f, 0.03f), VillageArt.WoodDark, new Vector3(0f, 0f, 45f));
                    var hook = VillageArt.Group(g, "hook", crane + new Vector3(-0.25f, 0.6f, 0f));
                    P(hook, VillageArt.Shape.Cyl, new Vector3(0f, -0.1f, 0f), new Vector3(0.006f, 0.1f, 0.006f), new Color(0.85f, 0.78f, 0.6f));
                    P(hook, VillageArt.Shape.Box, new Vector3(0f, -0.24f, 0f), new Vector3(0.1f, 0.07f, 0.1f), VillageArt.StoneLight);
                    VillageArt.Animate(hook.gameObject, ArtMotion.Kind.Sway, 1.2f, 0.06f, seed);
                    T(g, "cart", new Vector3(-0.3f, 0f, -0.34f), 20f, 0.35f);
                    break;
                }
                case BuildingType.GoldMine:
                case BuildingType.IronMine:
                {
                    // maden: kaya tepe, tahta destekli maden ağzı, raylar ve vagon, cevher yığını, fener
                    bool goldMine = t == BuildingType.GoldMine;
                    C(g, "rocks-large", new Vector3(0f, 0f, 0.18f), seed % 360, 0.62f);
                    var mouth = new Vector3(0f, 0f, -0.06f);
                    P(g, VillageArt.Shape.Box, mouth + new Vector3(0f, 0.13f, 0.02f), new Vector3(0.22f, 0.26f, 0.06f), new Color(0.06f, 0.05f, 0.05f)); // karanlık ağız
                    foreach (var x in new[] { -0.13f, 0.13f }) P(g, VillageArt.Shape.Box, mouth + new Vector3(x, 0.14f, 0f), new Vector3(0.04f, 0.28f, 0.05f), VillageArt.Wood);
                    P(g, VillageArt.Shape.Box, mouth + new Vector3(0f, 0.29f, 0f), new Vector3(0.34f, 0.045f, 0.06f), VillageArt.Wood);
                    foreach (var x in new[] { -0.05f, 0.05f }) PM(g, VillageArt.Shape.Box, new Vector3(x, 0.012f, -0.24f), new Vector3(0.012f, 0.012f, 0.4f), VillageArt.Mat(VillageArt.Iron, 0.8f));
                    for (int i = 0; i < 5; i++) P(g, VillageArt.Shape.Box, new Vector3(0f, 0.006f, -0.06f - i * 0.085f), new Vector3(0.16f, 0.012f, 0.03f), VillageArt.WoodDark);
                    T(g, "cart-high", new Vector3(0f, 0f, -0.3f), 0f, 0.36f);
                    var ore = goldMine ? VillageArt.Mat(VillageArt.GoldC, 0.9f, 0.5f) : VillageArt.Mat(new Color(0.55f, 0.6f, 0.68f), 0.85f);
                    for (int i = 0; i < 5 + tier; i++) // cevher yığını
                        PM(g, VillageArt.Shape.Ball, new Vector3(0.3f + (i % 3 - 1) * 0.05f, 0.03f + (i / 3) * 0.04f, -0.3f + (i % 2) * 0.04f), Vector3.one * 0.06f, ore);
                    LampPost(g, new Vector3(-0.32f, 0f, -0.3f), seed);
                    break;
                }
                case BuildingType.IronDepot:
                {
                    // Demir deposu (kullanıcı isteği 27.11.2026 "eve benzemesin, demirlerin olduğu bir depo"): oluklu sac çatılı
                    // açık çelik hangar; içinde demir çubuk dolu raflar, askıda sallanan külçe demeti; önde I-kirişler,
                    // külçe piramidi ve demir bantlı sandıklar
                    var steelDark = VillageArt.Mat(new Color(0.22f, 0.23f, 0.27f), 0.7f);
                    var sheet = VillageArt.Mat(new Color(0.5f, 0.55f, 0.62f), 0.75f);
                    var bar = VillageArt.Mat(new Color(0.62f, 0.66f, 0.74f), 0.9f, 0.1f);
                    float roofY = 0.5f + tier * 0.02f;
                    foreach (var x in new[] { -0.4f, 0.4f })
                        foreach (var z in new[] { -0.06f, 0.38f })
                        {
                            float hh = roofY + (z > 0 ? 0.03f : -0.03f);
                            PM(g, VillageArt.Shape.Box, new Vector3(x, hh * 0.5f, z), new Vector3(0.04f, hh, 0.04f), steelDark);
                        }
                    PM(g, VillageArt.Shape.Box, new Vector3(0f, roofY, 0.16f), new Vector3(0.9f, 0.025f, 0.54f), sheet, new Vector3(-8f, 0f, 0f)); // sac çatı
                    for (int i = 0; i < 8; i++) // oluklar
                        PM(g, VillageArt.Shape.Box, new Vector3(-0.39f + i * 0.111f, roofY + 0.016f, 0.16f), new Vector3(0.018f, 0.012f, 0.54f), steelDark, new Vector3(-8f, 0f, 0f));
                    PM(g, VillageArt.Shape.Box, new Vector3(0f, roofY * 0.5f, 0.4f), new Vector3(0.8f, roofY, 0.025f), sheet); // arka sac duvar
                    foreach (var x in new[] { -0.41f, 0.41f }) PM(g, VillageArt.Shape.Box, new Vector3(x, 0.11f, 0.17f), new Vector3(0.025f, 0.22f, 0.44f), sheet); // yan alçak duvarlar
                    // raflar: dikmeler + 3 raf; dolu raf sayısı seviyeyle artar
                    foreach (var x in new[] { -0.34f, 0f, 0.34f }) PM(g, VillageArt.Shape.Box, new Vector3(x, 0.21f, 0.27f), new Vector3(0.03f, 0.42f, 0.14f), steelDark);
                    int filled = 1 + Mathf.Min(2, tier);
                    for (int l = 0; l < 3; l++)
                    {
                        float sy = 0.1f + l * 0.12f;
                        P(g, VillageArt.Shape.Box, new Vector3(0f, sy, 0.27f), new Vector3(0.7f, 0.015f, 0.15f), VillageArt.WoodDark);
                        if (l >= filled) continue;
                        foreach (var cx in new[] { -0.17f, 0.17f })
                            for (int k = 0; k < 5; k++) // yatık demir çubuklar
                                PM(g, VillageArt.Shape.Cyl, new Vector3(cx, sy + 0.02f + (k / 3) * 0.024f, 0.22f + (k % 3) * 0.045f + (k / 3) * 0.02f), new Vector3(0.024f, 0.15f, 0.024f), bar, new Vector3(0f, 0f, 90f));
                    }
                    // çatıdan sarkan zincir + külçe demeti
                    var hoist = VillageArt.Group(g, "hoist", new Vector3(-0.05f, roofY - 0.04f, 0.02f));
                    PM(hoist, VillageArt.Shape.Cyl, new Vector3(0f, -0.1f, 0f), new Vector3(0.008f, 0.2f, 0.008f), steelDark);
                    for (int k = 0; k < 3; k++) PM(hoist, VillageArt.Shape.Box, new Vector3(0f, -0.22f - k * 0.03f, 0f), new Vector3(0.12f, 0.025f, 0.06f), bar);
                    VillageArt.Animate(hoist.gameObject, ArtMotion.Kind.Sway, 1.1f, 0.05f, seed);
                    // önde I-kirişler
                    for (int k = 0; k < 2 + Mathf.Min(1, tier / 2); k++)
                    {
                        var c = new Vector3(-0.02f, 0.02f + k * 0.045f, -0.2f + (k % 2) * 0.02f);
                        PM(g, VillageArt.Shape.Box, c, new Vector3(0.42f, 0.008f, 0.06f), steelDark);
                        PM(g, VillageArt.Shape.Box, c + new Vector3(0f, 0.02f, 0f), new Vector3(0.42f, 0.035f, 0.012f), steelDark);
                        PM(g, VillageArt.Shape.Box, c + new Vector3(0f, 0.04f, 0f), new Vector3(0.42f, 0.008f, 0.06f), steelDark);
                    }
                    // külçe piramidi
                    int rows = 2 + Mathf.Min(2, tier);
                    for (int r = 0; r < rows; r++)
                        for (int k = 0; k < rows - r; k++)
                            PM(g, VillageArt.Shape.Box, new Vector3(0.3f, 0.02f + r * 0.035f, -0.34f + (k - (rows - r - 1) * 0.5f) * 0.07f), new Vector3(0.12f, 0.03f, 0.06f), bar);
                    // demir bantlı sandıklar, üstlerinde külçe
                    int crates = tier >= 2 ? 2 : 1;
                    for (int i = 0; i < crates; i++)
                    {
                        var cp = new Vector3(-0.32f, 0f, -0.32f + i * 0.15f);
                        P(g, VillageArt.Shape.Box, cp + new Vector3(0f, 0.06f, 0f), new Vector3(0.13f, 0.12f, 0.13f), VillageArt.Wood);
                        foreach (var hy in new[] { 0.025f, 0.095f }) PM(g, VillageArt.Shape.Box, cp + new Vector3(0f, hy, 0f), new Vector3(0.135f, 0.012f, 0.135f), steelDark);
                        PM(g, VillageArt.Shape.Box, cp + new Vector3(0f, 0.135f, 0f), new Vector3(0.1f, 0.025f, 0.05f), bar);
                    }
                    break;
                }
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

        /// <summary>Kenney modelini renklendirir (MaterialPropertyBlock; ortak materyal değişmez) ve modelin ölçülen tepe yüksekliğini (model uzayı) döndürür.</summary>
        private static float Tint(Transform t, Color c, float strength = 0.55f)
        {
            var mpb = new MaterialPropertyBlock();
            var soft = Color.Lerp(Color.white, c, strength); // doku renkleri tamamen kaybolmasın
            float top = float.MinValue;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", soft);
                mpb.SetColor("_Color", soft);
                r.SetPropertyBlock(mpb);
                top = Mathf.Max(top, r.bounds.max.y);
            }
            // parça yükseklikleri tahmin değil ölçü: tepe süsleri modelin gerçek tepesine oturur
            return top > float.MinValue ? (top - t.position.y) / Mathf.Max(0.0001f, t.lossyScale.y) : 0f;
        }

        /// <summary>
        /// Elementli Okçu Kulesi (kullanıcı isteği 26.11.2026): her element kendine özgü taş rengi ve tepe:
        /// Ok = balista + altın sancak, Ateş = demir mangal + kor çatlakları, Buz = buz taç + süzülen kristal + sarkıtlar,
        /// Zehir = kazan + yeşil sıvı + damlalar, Büyü = süzülen kristal + dönen rün halkası, Şimşek = bakır bobin + parlak küre.
        /// top: tepe efektinin dünya konumu (FxKit.TowerIdle). Elementsizde null döner (normal Okçu Kulesi çizilir).
        /// </summary>
        public static Transform TryBuildElementTower(Transform parent, TowerElement e, int level, bool damaged, Vector3 pos, out Vector3 top)
        {
            top = pos + new Vector3(0f, 1.6f, 0f);
            if (!ModelLibrary.Available || e == TowerElement.None) return null;
            int tier = VillageArt.Tier(level);
            var root = VillageArt.Group(parent, "Tower_" + e, pos);
            var g = VillageArt.Group(root, "model", Vector3.zero, 0f, 0.5f);
            float y = 0f;
            void Stack(string piece, float height) { C(g, piece, new Vector3(0f, y, 0f)); y += height; }
            Material M(Color c, float emit = 0f, float smooth = 0.6f) => VillageArt.Mat(c, smooth, emit);
            var iron = M(new Color(0.2f, 0.2f, 0.23f), 0f, 0.5f);
            var copper = M(new Color(0.78f, 0.45f, 0.25f), 0.15f, 0.85f);
            var gold = M(VillageArt.GoldC, 0.25f, 0.85f);
            float crown; // tepe efektinin model-uzayı yüksekliği
            switch (e)
            {
                case TowerElement.Arrow:
                    Stack("tower-hexagon-base", 1.31f);
                    for (int i = 0; i < 1 + tier / 2; i++) Stack("tower-hexagon-mid", 0.46f);
                    Stack("tower-hexagon-top-wood", 0.46f);
                    y = Tint(g, new Color(1f, 0.93f, 0.78f), 0.75f);
                    C(g, "siege-ballista", new Vector3(0f, y, 0f), 180f, 0.55f);
                    for (int i = 0; i < 2; i++) // ince altın bantlar
                        VillageArt.Part(g, VillageArt.Shape.Cyl, new Vector3(0f, 0.7f + i * 0.75f, 0f), new Vector3(0.9f, 0.025f, 0.9f), gold);
                    crown = y + 0.5f; y += 0.6f;
                    break;
                case TowerElement.Fire:
                    Stack("tower-square-base", 1.01f);
                    for (int i = 0; i < 1 + tier / 2; i++) Stack("tower-square-mid", 1.01f);
                    Stack("tower-square-top", 0.5f);
                    y = Tint(g, new Color(0.72f, 0.42f, 0.36f), 0.75f);
                    // kor çatlakları (dört yüzde parlayan dikey yarıklar)
                    var lava = M(new Color(1f, 0.45f, 0.08f), 2.2f, 0.9f);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * Mathf.PI * 0.5f; // dört yüzün ortası
                        VillageArt.Part(g, VillageArt.Shape.Box, new Vector3(Mathf.Cos(a) * 0.505f, 0.5f + (i % 2) * 0.35f, Mathf.Sin(a) * 0.505f), new Vector3(0.05f, 0.42f, 0.05f), lava, new Vector3(0f, -a * Mathf.Rad2Deg, (i % 2 == 0 ? 10f : -8f)));
                    }
                    // mangal: üç demir ayak, kase, kor
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * 2.094f;
                        VillageArt.Part(g, VillageArt.Shape.Box, new Vector3(Mathf.Cos(a) * 0.2f, y + 0.12f, Mathf.Sin(a) * 0.2f), new Vector3(0.05f, 0.26f, 0.05f), iron, new Vector3(Mathf.Sin(a) * 15f, 0f, -Mathf.Cos(a) * 15f));
                    }
                    VillageArt.Part(g, VillageArt.Shape.Cyl, new Vector3(0f, y + 0.3f, 0f), new Vector3(0.55f, 0.1f, 0.55f), iron);
                    VillageArt.Part(g, VillageArt.Shape.Cyl, new Vector3(0f, y + 0.36f, 0f), new Vector3(0.45f, 0.03f, 0.45f), M(new Color(1f, 0.55f, 0.12f), 3f, 0.9f));
                    crown = y + 0.4f; y += 0.5f;
                    break;
                case TowerElement.Ice:
                    Stack("tower-square-base", 1.01f);
                    for (int i = 0; i < 1 + tier / 2; i++) Stack("tower-square-mid-windows", 1.01f);
                    Stack("tower-square-top", 0.5f);
                    y = Tint(g, new Color(0.62f, 0.82f, 1f), 0.9f);
                    var ice = M(new Color(0.65f, 0.9f, 1f), 0.6f, 0.95f);
                    for (int i = 0; i < 5 + tier; i++) // buz taç
                    {
                        float a = i * Mathf.PI * 2f / (5 + tier);
                        VillageArt.Part(g, VillageArt.Shape.Cone, new Vector3(Mathf.Cos(a) * 0.36f, y + 0.22f, Mathf.Sin(a) * 0.36f), new Vector3(0.12f, 0.45f + (i % 2) * 0.15f, 0.12f), ice, new Vector3(Mathf.Sin(a) * 14f, 0f, -Mathf.Cos(a) * 14f));
                    }
                    for (int i = 0; i < 8; i++) // sarkıtlar
                    {
                        float a = i * Mathf.PI * 0.25f + 0.2f;
                        VillageArt.Part(g, VillageArt.Shape.Cone, new Vector3(Mathf.Cos(a) * 0.52f, y - 0.12f, Mathf.Sin(a) * 0.52f), new Vector3(0.06f, 0.2f + (i % 3) * 0.06f, 0.06f), ice, new Vector3(180f, 0f, 0f));
                    }
                    var crystal = VillageArt.Group(g, "crystal", new Vector3(0f, y + 0.75f, 0f));
                    var crystalMat = M(new Color(0.7f, 0.95f, 1f), 1.0f, 0.95f);
                    VillageArt.Part(crystal, VillageArt.Shape.Pyramid, new Vector3(0f, 0.14f, 0f), new Vector3(0.22f, 0.28f, 0.22f), crystalMat);
                    VillageArt.Part(crystal, VillageArt.Shape.Pyramid, new Vector3(0f, -0.14f, 0f), new Vector3(0.22f, 0.28f, 0.22f), crystalMat, new Vector3(180f, 0f, 0f));
                    VillageArt.Animate(crystal.gameObject, ArtMotion.Kind.Spin, 1.2f);
                    crown = y + 0.75f; y += 1.1f;
                    break;
                case TowerElement.Poison:
                    Stack("tower-hexagon-base", 1.31f);
                    for (int i = 0; i < 1 + tier / 2; i++) Stack("tower-hexagon-mid", 0.46f);
                    Stack("tower-hexagon-top", 0.46f);
                    y = Tint(g, new Color(0.62f, 0.8f, 0.5f), 0.92f);
                    var slime = M(new Color(0.45f, 0.95f, 0.2f), 1.6f, 0.95f);
                    // kazan: gövde, ağız halkası, yeşil sıvı
                    VillageArt.Part(g, VillageArt.Shape.Ball, new Vector3(0f, y + 0.22f, 0f), new Vector3(0.58f, 0.44f, 0.58f), iron);
                    VillageArt.Part(g, VillageArt.Shape.Cyl, new Vector3(0f, y + 0.4f, 0f), new Vector3(0.5f, 0.05f, 0.5f), iron);
                    VillageArt.Part(g, VillageArt.Shape.Cyl, new Vector3(0f, y + 0.42f, 0f), new Vector3(0.42f, 0.03f, 0.42f), slime);
                    for (int i = 0; i < 6; i++) // duvardan süzülen zehir damlaları
                    {
                        float a = i * 1.047f + 0.3f;
                        VillageArt.Part(g, VillageArt.Shape.Box, new Vector3(Mathf.Cos(a) * 0.49f, y - 0.25f - (i % 3) * 0.15f, Mathf.Sin(a) * 0.49f), new Vector3(0.05f, 0.3f + (i % 2) * 0.2f, 0.05f), slime, new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
                    }
                    crown = y + 0.45f; y += 0.55f;
                    break;
                case TowerElement.Magic:
                    Stack("tower-hexagon-base", 1.31f);
                    for (int i = 0; i < 1 + tier / 2; i++) Stack("tower-hexagon-mid", 0.46f);
                    Stack("tower-hexagon-top", 0.46f);
                    y = Tint(g, new Color(0.74f, 0.64f, 0.92f), 0.92f);
                    var arcane = M(new Color(0.8f, 0.45f, 1f), 0.9f, 0.95f);
                    var gem = VillageArt.Group(g, "gem", new Vector3(0f, y + 0.8f, 0f));
                    VillageArt.Part(gem, VillageArt.Shape.Pyramid, new Vector3(0f, 0.17f, 0f), new Vector3(0.28f, 0.34f, 0.28f), arcane);
                    VillageArt.Part(gem, VillageArt.Shape.Pyramid, new Vector3(0f, -0.17f, 0f), new Vector3(0.28f, 0.34f, 0.28f), arcane, new Vector3(180f, 0f, 0f));
                    VillageArt.Animate(gem.gameObject, ArtMotion.Kind.Spin, 1.6f);
                    var runes = VillageArt.Group(g, "runes", new Vector3(0f, y + 0.8f, 0f));
                    var runeMat = M(new Color(0.9f, 0.6f, 1f), 1.1f, 0.9f);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI * 0.25f;
                        VillageArt.Part(runes, VillageArt.Shape.Box, new Vector3(Mathf.Cos(a) * 0.48f, 0f, Mathf.Sin(a) * 0.48f), new Vector3(0.1f, 0.14f, 0.03f), runeMat, new Vector3(0f, -a * Mathf.Rad2Deg + 90f, 0f));
                    }
                    VillageArt.Animate(runes.gameObject, ArtMotion.Kind.Spin, -0.8f);
                    for (int i = 0; i < 6; i++) // tepe sivri uçları
                    {
                        float a = i * 1.047f;
                        VillageArt.Part(g, VillageArt.Shape.Cone, new Vector3(Mathf.Cos(a) * 0.42f, y + 0.12f, Mathf.Sin(a) * 0.42f), new Vector3(0.08f, 0.25f, 0.08f), gold);
                    }
                    crown = y + 0.8f; y += 1.15f;
                    break;
                case TowerElement.Ballista:
                {
                    // Balista Kulesi (kullanıcı isteği 27.11.2026): kare taş kule, demir döner tabla üstünde hedefe dönen dev balista;
                    // atışta kollar ileri fırlar, gövde geri teper, sonra kollar gerilir ve yeni ok yerleşir (BallistaAim)
                    Stack("tower-square-base", 1.01f);
                    for (int i = 0; i < 1 + tier / 2; i++) Stack("tower-square-mid", 1.01f);
                    Stack("tower-square-top", 0.5f);
                    y = Tint(g, new Color(0.66f, 0.6f, 0.52f), 0.8f);
                    var wood = M(VillageArt.Wood, 0f, 0.3f);
                    var woodD = M(VillageArt.WoodDark, 0f, 0.3f);
                    VillageArt.Part(g, VillageArt.Shape.Cyl, new Vector3(0f, y + 0.04f, 0f), new Vector3(0.75f, 0.08f, 0.75f), iron); // döner tabla
                    var aim = VillageArt.Group(g, "ballista", new Vector3(0f, y + 0.08f, 0f), 0f, 1.3f);
                    var body = VillageArt.Group(aim, "body", Vector3.zero);
                    VillageArt.Part(body, VillageArt.Shape.Box, new Vector3(0f, 0.12f, 0f), new Vector3(0.18f, 0.24f, 0.18f), woodD);           // ayak
                    VillageArt.Part(body, VillageArt.Shape.Box, new Vector3(0f, 0.29f, 0.05f), new Vector3(0.15f, 0.09f, 1.05f), wood);          // kundak
                    VillageArt.Part(body, VillageArt.Shape.Box, new Vector3(0f, 0.338f, 0.05f), new Vector3(0.045f, 0.012f, 1f), iron);          // ok oluğu
                    VillageArt.Part(body, VillageArt.Shape.Box, new Vector3(0f, 0.29f, 0.5f), new Vector3(0.26f, 0.13f, 0.11f), iron);           // yay yuvası
                    VillageArt.Part(body, VillageArt.Shape.Cyl, new Vector3(0f, 0.29f, -0.44f), new Vector3(0.09f, 0.32f, 0.09f), iron, new Vector3(0f, 0f, 90f)); // vinç
                    foreach (var sx in new[] { -1f, 1f }) VillageArt.Part(body, VillageArt.Shape.Box, new Vector3(sx * 0.17f, 0.24f, -0.44f), new Vector3(0.025f, 0.12f, 0.025f), woodD); // vinç kolu
                    var arms = new Transform[2];
                    for (int k = 0; k < 2; k++)
                    {
                        float sx = k == 0 ? -1f : 1f;
                        arms[k] = VillageArt.Group(body, k == 0 ? "armL" : "armR", new Vector3(sx * 0.11f, 0.3f, 0.48f));
                        VillageArt.Part(arms[k], VillageArt.Shape.Box, new Vector3(sx * 0.23f, 0f, 0f), new Vector3(0.46f, 0.055f, 0.065f), woodD);
                        VillageArt.Part(arms[k], VillageArt.Shape.Ball, new Vector3(sx * 0.46f, 0f, 0f), Vector3.one * 0.07f, iron);
                    }
                    var str = VillageArt.Part(body, VillageArt.Shape.Box, new Vector3(0f, 0.3f, 0.3f), new Vector3(1f, 0.014f, 0.014f), new Color(0.92f, 0.88f, 0.78f));
                    var bolt = VillageArt.Group(body, "bolt", new Vector3(0f, 0.36f, 0.12f));
                    VillageArt.Part(bolt, VillageArt.Shape.Cyl, Vector3.zero, new Vector3(0.035f, 0.72f, 0.035f), VillageArt.WoodLight, new Vector3(90f, 0f, 0f));
                    VillageArt.Part(bolt, VillageArt.Shape.Cone, new Vector3(0f, 0f, 0.4f), new Vector3(0.07f, 0.12f, 0.07f), iron, new Vector3(90f, 0f, 0f));
                    foreach (var a in new[] { 0f, 90f }) VillageArt.Part(bolt, VillageArt.Shape.Box, new Vector3(0f, 0f, -0.31f), new Vector3(0.1f, 0.004f, 0.1f), VillageArt.RoofRed, new Vector3(0f, 0f, a)); // tüyler
                    var ba = aim.gameObject.AddComponent<BallistaAim>();
                    ba.Body = body; ba.ArmL = arms[0]; ba.ArmR = arms[1]; ba.StringT = str.transform; ba.Bolt = bolt;
                    crown = y + 0.55f; y += 0.6f;
                    break;
                }
                default: // Şimşek
                    Stack("tower-hexagon-base", 1.31f);
                    for (int i = 0; i < 1 + tier / 2; i++) Stack("tower-hexagon-mid", 0.46f);
                    Stack("tower-hexagon-top", 0.46f);
                    y = Tint(g, new Color(0.66f, 0.73f, 0.86f), 0.92f);
                    VillageArt.Part(g, VillageArt.Shape.Cyl, new Vector3(0f, y + 0.45f, 0f), new Vector3(0.09f, 0.9f, 0.09f), copper);
                    for (int i = 0; i < 4; i++) // bobin halkaları (yukarı doğru küçülür)
                        VillageArt.Part(g, VillageArt.Shape.Cyl, new Vector3(0f, y + 0.18f + i * 0.2f, 0f), new Vector3(0.5f - i * 0.09f, 0.05f, 0.5f - i * 0.09f), copper);
                    var orb = VillageArt.Part(g, VillageArt.Shape.Ball, new Vector3(0f, y + 1.02f, 0f), new Vector3(0.3f, 0.3f, 0.3f), M(new Color(0.7f, 0.85f, 1f), 1.4f, 0.95f));
                    VillageArt.Animate(orb, ArtMotion.Kind.Bob, 4f, 0.03f);
                    crown = y + 1.02f; y += 1.25f;
                    break;
            }
            // element renginde iki sancak (kule gövdesinin iki yanında): elementi uzaktan tanıtır
            var bc = VillageArt.ElementColor(e);
            foreach (var side in new[] { 1f, -1f })
            {
                var ban = C(g, "flag-banner-short", new Vector3(0.5f * side, crown * 0.42f, 0f), 90f * side);
                if (ban != null) Tint(ban, bc, 1f);
            }
            if (tier >= 2 && e != TowerElement.Arrow) C(g, "flag", new Vector3(0.3f, y - 0.4f, 0.3f));
            if (damaged) VillageArt.Part(root, VillageArt.Shape.Ball, new Vector3(0.15f, 0.25f, -0.2f), new Vector3(0.18f, 0.1f, 0.15f), VillageArt.StoneDark);
            top = pos + new Vector3(0f, crown * 0.5f, 0f);
            return root;
        }
    }
}
