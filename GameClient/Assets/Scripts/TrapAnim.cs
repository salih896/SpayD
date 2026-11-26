using GameCore.Combat;
using UnityEngine;

namespace GameClient
{
    /// <summary>
    /// Tuzak animasyonu (kullanıcı isteği 27.11.2026; yalnızca görsel). Bekleme: dikenler ara ara uç gösterir, alev titrer,
    /// zehir birikintisi nabız gibi atar, buz kristalleri döner, şok çubukları titrer, patlayıcı hafifçe sallanır.
    /// Tetiklenme (Trigger): dikenler fırlar, kapanın çeneleri kapanır, alev büyür, kristaller uzar, patlayıcı şişip kaybolur...
    /// </summary>
    public sealed class TrapAnim : MonoBehaviour
    {
        public TrapType Type;
        public Transform Mov, JawL, JawR;   // hareketli parçalar (tür başına)
        private float trigT = -10f, phase;
        private bool spent, consume = true;
        private Vector3 movPos, movScale = Vector3.one;

        private void Start()
        {
            phase = (transform.position.x * 3.1f + transform.position.z * 1.7f) % 6.28f;
            if (Mov != null) { movPos = Mov.localPosition; movScale = Mov.localScale; }
        }

        /// <summary>Tetiklenme animasyonunu başlatır.</summary>
        /// <param name="oneShot">false: köydeki önizleme (patlayıcı tekrar görünür); savaşta true.</param>
        public void Trigger(bool oneShot = true) { if (!spent) { trigT = Time.time; consume = oneShot; } }

        private void Update()
        {
            float t = Time.time + phase;
            float e = Time.time - trigT; // tetiklenmeden beri geçen
            bool active = e >= 0f && e < 1.2f;
            switch (Type)
            {
                case TrapType.Spikes:
                {
                    // içeride bekler, ~3 sn'de bir uçları görünür; tetiklenince fırlar, 0,5 sn kalır, geri çekilir
                    float up = -0.075f + Mathf.Max(0f, Mathf.Sin(t * 2.1f) - 0.92f) * 0.25f;
                    if (active) up = e < 0.08f ? Mathf.Lerp(-0.075f, 0.02f, e / 0.08f) : e < 0.6f ? Mathf.Lerp(0.02f, 0f, (e - 0.08f) / 0.1f) : Mathf.Lerp(0f, -0.075f, (e - 0.6f) / 0.4f);
                    Mov.localPosition = movPos + new Vector3(0f, up, 0f);
                    break;
                }
                case TrapType.Snare:
                {
                    // çeneler açık (yatık) bekler; tetiklenince çat diye kapanır, 0,6 sn sonra yavaşça açılır
                    float close = 0f;
                    if (active) close = e < 0.06f ? e / 0.06f : e < 0.7f ? 1f : Mathf.Clamp01(1f - (e - 0.7f) / 0.45f);
                    float ang = Mathf.Lerp(0f, 82f, close) + (active ? 0f : Mathf.Sin(t * 1.3f) * 2f);
                    JawL.localRotation = Quaternion.Euler(0f, 0f, ang);  // sol çene içe doğru yukarı kalkar
                    JawR.localRotation = Quaternion.Euler(0f, 0f, -ang);
                    break;
                }
                case TrapType.Fire:
                {
                    float fl = 1f + Mathf.PerlinNoise(t * 4f, 0.3f) * 0.35f - 0.15f;
                    float boost = active ? 1f + 1.8f * Mathf.Sin(Mathf.Clamp01(e / 0.9f) * Mathf.PI) : 1f;
                    Mov.localScale = Vector3.Scale(movScale, new Vector3(1f + (boost - 1f) * 0.5f, fl * boost, 1f + (boost - 1f) * 0.5f));
                    break;
                }
                case TrapType.Poison:
                {
                    float pulse = 1f + Mathf.Sin(t * 1.6f) * 0.04f;
                    if (active) pulse += 0.5f * Mathf.Sin(Mathf.Clamp01(e / 0.8f) * Mathf.PI);
                    Mov.localScale = new Vector3(movScale.x * pulse, movScale.y, movScale.z * pulse);
                    break;
                }
                case TrapType.Frost:
                {
                    float grow = active ? 1f + 1.6f * Mathf.Sin(Mathf.Clamp01(e / 1f) * Mathf.PI) : 1f;
                    Mov.localRotation = Quaternion.Euler(0f, t * 25f, 0f);
                    Mov.localScale = new Vector3(movScale.x, movScale.y * grow, movScale.z);
                    break;
                }
                case TrapType.Shock:
                {
                    float jit = active ? 8f : 1.5f;
                    Mov.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 40f) * jit + (active ? e * 900f : 0f), 0f);
                    float s = active ? 1f + 0.6f * Mathf.Sin(Mathf.Clamp01(e / 0.5f) * Mathf.PI) : 1f;
                    Mov.localScale = movScale * s;
                    break;
                }
                case TrapType.Bomb:
                {
                    if (spent) break;
                    if (active)
                    {
                        // şişer, titrer, kaybolur (tek seferlik)
                        float s = e < 0.12f ? 1f + 0.5f * (e / 0.12f) : Mathf.Max(0f, 1.5f - (e - 0.12f) * 12f);
                        Mov.localScale = movScale * s;
                        Mov.localPosition = movPos + new Vector3(Mathf.Sin(e * 90f) * 0.01f, 0f, 0f);
                        if (s <= 0f && consume) { spent = true; Mov.gameObject.SetActive(false); }
                    }
                    else
                    {
                        Mov.localScale = movScale; Mov.localPosition = movPos; // önizleme bitince geri gelir
                        Mov.localRotation = Quaternion.Euler(Mathf.Sin(t * 2.2f) * 3f, 0f, Mathf.Cos(t * 1.7f) * 3f);
                    }
                    break;
                }
                case TrapType.Slippery:
                {
                    float wob = active ? Mathf.Sin(e * 30f) * 0.15f * (1f - e / 1.2f) : Mathf.Sin(t * 1.2f) * 0.03f;
                    Mov.localScale = new Vector3(movScale.x * (1f + wob), movScale.y, movScale.z * (1f - wob));
                    break;
                }
            }
        }
    }
}
