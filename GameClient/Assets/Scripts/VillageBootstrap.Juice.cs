using System.Collections.Generic;
using GameCore.Combat;
using UnityEngine;

namespace GameClient
{
    /// <summary>
    /// Savaş hissi (yalnızca görsel; kullanıcı isteği 27.11.2026): kamera sarsıntısı, uçan yazılar (yapı hasarı, "YIKILDI",
    /// yıldız eşikleri), askerin devrilerek ölmesi, yapının çökmesi (toz + moloz), ateş eden kulenin geri tepmesi, zafer kutlaması.
    /// </summary>
    public sealed partial class VillageBootstrap
    {
        // ---------- kamera sarsıntısı ----------
        private float shakeAmp, shakeUntil, shakeDur = 0.01f;

        private void Shake(float amp, float dur)
        {
            if (Time.time < shakeUntil && amp < shakeAmp) return; // daha güçlü sarsıntıyı ezme
            shakeAmp = amp; shakeDur = dur; shakeUntil = Time.time + dur;
        }

        private Vector3 ShakeOffset()
        {
            if (Time.time >= shakeUntil) return Vector3.zero;
            float k = (shakeUntil - Time.time) / shakeDur;
            float t = Time.time * 47f;
            return new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * (2f * shakeAmp * k);
        }

        // ---------- uçan yazılar ----------
        private sealed class FloatText { public Vector3 World; public string Text; public Color Col; public float T0, Dur, Size; public bool Screen; }
        private readonly List<FloatText> floatTexts = new List<FloatText>();

        private void PopText(Vector3 world, string text, Color col, float size = 18f, float dur = 1.1f)
        {
            if (floatTexts.Count > 40) floatTexts.RemoveAt(0);
            floatTexts.Add(new FloatText { World = world, Text = text, Col = col, T0 = Time.time, Dur = dur, Size = size });
        }

        /// <summary>Ekranın ortasında büyük duyuru (yıldız, zafer).</summary>
        private void Banner(string text, Color col)
        {
            floatTexts.Add(new FloatText { Text = text, Col = col, T0 = Time.time, Dur = 1.8f, Size = 34f, Screen = true });
        }

        private void DrawFloatTexts(float w, float h)
        {
            var st = new GUIStyle(UiTheme.Title) { alignment = TextAnchor.MiddleCenter, richText = true };
            for (int i = floatTexts.Count - 1; i >= 0; i--)
            {
                var f = floatTexts[i];
                float k = (Time.time - f.T0) / f.Dur;
                if (k >= 1f) { floatTexts.RemoveAt(i); continue; }
                Vector2 gp;
                if (f.Screen) gp = new Vector2(w * 0.5f, h * 0.32f - k * 20f);
                else
                {
                    var sp = cam.WorldToScreenPoint(f.World + Vector3.up * (k * 0.6f));
                    if (sp.z < 0f) continue;
                    gp = ScreenToGui(sp);
                }
                float pop = k < 0.12f ? Mathf.Lerp(0.5f, 1.15f, k / 0.12f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((k - 0.12f) / 0.15f));
                st.fontSize = Mathf.RoundToInt(f.Size * pop);
                float a = k < 0.75f ? 1f : 1f - (k - 0.75f) / 0.25f;
                var r = new Rect(gp.x - 160f, gp.y - 25f, 320f, 50f);
                st.normal.textColor = new Color(0f, 0f, 0f, 0.6f * a);
                GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), f.Text, st); // gölge
                st.normal.textColor = new Color(f.Col.r, f.Col.g, f.Col.b, a);
                GUI.Label(r, f.Text, st);
            }
        }

        // ---------- yapı hasarı sayıları, çökme, yıldız eşikleri ----------
        private int[] structPrevHp;
        private float[] structDmgAcc, structDmgNext;
        private int lastStarMark;

        /// <summary>Her karede: yapıların can kaybını toplayıp ~0,35 sn'de bir "-N" yazar; yıkılınca çökme efekti.</summary>
        private void JuiceStructures()
        {
            int n = sim.Structures.Count;
            if (structPrevHp == null || structPrevHp.Length != n)
            {
                structPrevHp = new int[n]; structDmgAcc = new float[n]; structDmgNext = new float[n];
                for (int i = 0; i < n; i++) structPrevHp[i] = sim.Structures[i].Hp;
                lastStarMark = 0;
            }
            for (int i = 0; i < n; i++)
            {
                var st = sim.Structures[i];
                int lost = structPrevHp[i] - st.Hp;
                structPrevHp[i] = st.Hp;
                var top = new Vector3(st.Pos.X, st.Kind == StructKind.Tower ? 1.7f : 1.0f, st.Pos.Y);
                if (lost > 0) structDmgAcc[i] += lost;
                if (structDmgAcc[i] > 0f && Time.time >= structDmgNext[i])
                {
                    PopText(top + new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.2f, 0.2f)), "-" + Mathf.RoundToInt(structDmgAcc[i]), new Color(1f, 0.92f, 0.6f), 15f, 0.9f);
                    structDmgAcc[i] = 0f; structDmgNext[i] = Time.time + 0.35f;
                }
            }
            // yıldız eşikleri: %50 (1 yıldız), %75 zafer... (simülasyondaki yüzdeyle aynı)
            int pct = LiveDamagePct();
            int[] marks = { 50, 75, 90, 100 };
            string[] names = { "★ %50", "★ %75  ZAFER!", "★★ %90", "★★★ %100" };
            for (int m = lastStarMark; m < marks.Length; m++)
            {
                if (pct < marks[m]) break;
                Banner(names[m], new Color(1f, 0.85f, 0.25f));
                FxKit.LevelUpBurst(null, new Vector3((layout.Size - 1) * 0.5f, 0f, (layout.Size - 1) * 0.5f));
                lastStarMark = m + 1;
            }
        }

        /// <summary>Yapı yıkıldı: çökme efekti + sarsıntı + yazı.</summary>
        private void StructureDestroyed(SimStructure st)
        {
            var p = new Vector3(st.Pos.X, 0f, st.Pos.Y);
            bool tower = st.Kind == StructKind.Tower;
            FxKit.Collapse(DynRoot(), p, tower ? 1.2f : 1f, Mathf.Max(1f, speed));
            Shake(tower ? 0.22f : 0.16f, 0.35f);
            PopText(p + Vector3.up * 1.1f, "YIKILDI", new Color(1f, 0.45f, 0.3f), 20f, 1.2f);
        }

        // ---------- asker ölümü: devrilir, batar, kaybolur ----------
        private readonly List<float> soldierDeathT = new List<float>();
        private readonly List<Quaternion> soldierDeathRot = new List<Quaternion>();
        private readonly List<Vector3> soldierDeathScale = new List<Vector3>();

        /// <summary>Ölen askerin görünümü; true dönerse asker hâlâ çiziliyor (animasyon sürüyor).</summary>
        private bool SoldierDying(int i, Transform t, SimSoldier s)
        {
            while (soldierDeathT.Count <= i) { soldierDeathT.Add(-1f); soldierDeathRot.Add(Quaternion.identity); soldierDeathScale.Add(Vector3.one); }
            if (!t.gameObject.activeSelf) return false;
            if (soldierDeathT[i] < 0f)
            {
                soldierDeathT[i] = Time.time; soldierDeathRot[i] = t.rotation; soldierDeathScale[i] = t.localScale;
                FxKit.SoldierDeath(DynRoot(), t.position, s.Flying);
                if (s.Type == SoldierType.Bomber) { t.gameObject.SetActive(false); return false; } // patlayarak gitti
            }
            float k = (Time.time - soldierDeathT[i]) / 0.75f;
            if (k >= 1f) { t.gameObject.SetActive(false); return false; }
            // yana devrilme (ilk %40), sonra yere gömülüp küçülme
            float tip = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / 0.4f));
            t.rotation = soldierDeathRot[i] * Quaternion.Euler(0f, 0f, 80f * tip);
            if (s.Flying) t.position += Vector3.down * (Time.deltaTime * 2.2f); // uçan düşer
            if (k > 0.55f) t.position += Vector3.down * (Time.deltaTime * 0.35f);
            float sc = k > 0.6f ? 1f - (k - 0.6f) / 0.4f : 1f;
            t.localScale = soldierDeathScale[i] * Mathf.Max(0.01f, sc);
            return true;
        }

        // ---------- kule geri tepmesi ----------
        private readonly Dictionary<GameCore.Grid.GridPos, Transform> towerViews = new Dictionary<GameCore.Grid.GridPos, Transform>();

        private void TowerKick(GameCore.Grid.GridPos p, float amount)
        {
            if (!towerViews.TryGetValue(p, out var tv) || tv == null) return;
            var k = tv.GetComponent<KickAnim>();
            if (k == null) k = tv.gameObject.AddComponent<KickAnim>();
            k.Kick(amount);
        }

        // ---------- saldırı alanı ipucu ----------
        private Transform deployZone;
        private Material deployZoneMat;

        /// <summary>Saldırı alanının üstünde kırmızı yarı saydam parlama; asker yokken belirgin atar, sonra soluklaşır.</summary>
        private void UpdateDeployZone()
        {
            if (sim == null || battleOver) { if (deployZone != null) deployZone.gameObject.SetActive(false); return; }
            if (deployZone == null)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.name = "deployZone";
                q.transform.SetParent(DynRoot(), false);
                int rows = GameCore.Village.VillageLayout.AttackZoneRows;
                q.transform.position = new Vector3((layout.Size - 1) * 0.5f, 0.02f, -(rows + 1) * 0.5f);
                q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                q.transform.localScale = new Vector3(layout.Size, rows, 1f);
                deployZoneMat = new Material(FxKit.Mat(FxKit.SoftRect, true));
                q.GetComponent<MeshRenderer>().sharedMaterial = deployZoneMat;
                q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                deployZone = q.transform;
            }
            deployZone.gameObject.SetActive(true);
            bool none = sim.Soldiers.Count == 0;
            float a = none ? 0.3f + 0.18f * Mathf.Sin(Time.time * 4f) : 0.07f;
            deployZoneMat.SetColor("_Color", new Color(1f, 0.25f, 0.15f, a));
        }

        // ---------- köy: üretim yazıları, yerleştirme tozu, seviye kutlaması ----------
        private float nextProdPop;
        private int prodPopIndex;

        /// <summary>Köyde ~6 sn'de bir sıradaki üretim binasının üstünde "+N kaynak" uçar (üretimin hissi; sayı gerçek hızdan).</summary>
        private void ProductionPops()
        {
            if (sim != null || tab != Tab.Village || LayoutMode || Time.time < nextProdPop) return;
            nextProdPop = Time.time + 6f;
            var list = new List<KeyValuePair<GameCore.Grid.GridPos, GameCore.Combat.BuildingSpec>>();
            foreach (var kv in g.Buildings) if (GameCore.Game.GameRules.ProducedBy(kv.Value.Type).HasValue) list.Add(kv);
            if (list.Count == 0) return;
            var b = list[prodPopIndex++ % list.Count];
            var res = GameCore.Game.GameRules.ProducedBy(b.Value.Type).Value;
            long perHour = (long)GameCore.Game.GameRules.BasePerHour(res) * ContentCatalog.ScalePct(b.Value.Level) / 100;
            int amount = Mathf.Max(1, Mathf.RoundToInt(perHour * 6f / 3600f));
            PopText(new Vector3(b.Key.X, 1.1f, b.Key.Y), "+" + amount + " " + ResName(res), ResColor(res), 16f, 1.6f);
        }

        private void DropPuff(Vector3 p)
        {
            FxKit.GroundRing(null, new Vector3(p.x, 0.05f, p.z), new Color(1f, 0.95f, 0.75f), new Color(0.9f, 0.75f, 0.45f), 1.2f);
        }

        /// <summary>Seviye atlandı: ekranda duyuru + köyün ortasında konfeti.</summary>
        private void LevelUpFx(int level)
        {
            Banner("SEVİYE " + level + "!", new Color(0.55f, 0.85f, 1f));
            FxKit.Confetti(null, new Vector3((layout.Size - 1) * 0.5f, 3f, (layout.Size - 1) * 0.5f));
        }

        // ---------- zafer kutlaması ----------
        private bool celebrate;

        private float resultTime;

        private void OnBattleFinishedFx(bool victory)
        {
            celebrate = victory;
            resultTime = Time.unscaledTime;
            var center = new Vector3((layout.Size - 1) * 0.5f, 0f, (layout.Size - 1) * 0.5f);
            if (victory) { FxKit.Confetti(DynRoot(), center + Vector3.up * 2.5f); Banner("ZAFER!", new Color(0.55f, 1f, 0.45f)); Shake(0.12f, 0.4f); }
            else Banner("YENİLGİ", new Color(1f, 0.45f, 0.35f));
        }

        /// <summary>Kutlamada hayattaki askerler zıplar (yükseklik ofseti).</summary>
        private float CelebrateHop(int i) => celebrate ? Mathf.Abs(Mathf.Sin(Time.time * 7f + i * 0.9f)) * 0.18f : 0f;
    }

    /// <summary>Kısa "geri tepme": modeli bir an basık ve büyük gösterip yerine yumuşakça döndürür.</summary>
    public sealed class KickAnim : MonoBehaviour
    {
        private Vector3 baseScale;
        private float t = 1f, amount;
        private bool init;

        public void Kick(float a)
        {
            if (!init) { baseScale = transform.localScale; init = true; }
            amount = a; t = 0f;
        }

        private void Update()
        {
            if (!init || t >= 1f) return;
            t = Mathf.Min(1f, t + Time.deltaTime / 0.22f);
            float s = Mathf.Sin(t * Mathf.PI) * amount * (1f - t * 0.5f);
            transform.localScale = new Vector3(baseScale.x * (1f + s * 0.6f), baseScale.y * (1f - s), baseScale.z * (1f + s * 0.6f));
        }
    }
}
