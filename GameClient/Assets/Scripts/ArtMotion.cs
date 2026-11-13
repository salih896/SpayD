using UnityEngine;

namespace GameClient
{
    /// <summary>Görsel canlılık: bayrak dalgalanması, küre süzülmesi, kanat çırpma, duman. Oyun kuralına etkisi yoktur.</summary>
    public sealed class ArtMotion : MonoBehaviour
    {
        public enum Kind { Bob, Spin, Flutter, Flap, Smoke, Sway }

        public Kind Mode;
        public float Speed = 1f, Amount = 0.05f, Phase;
        public float Life = -1f;              // Smoke: saniye
        private Vector3 basePos, baseScale;
        private Quaternion baseRot;
        private float born;

        private void Start()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            baseScale = transform.localScale;
            born = Time.time;
        }

        private void Update()
        {
            float t = Time.time * Speed + Phase;
            switch (Mode)
            {
                case Kind.Bob:
                    transform.localPosition = basePos + new Vector3(0f, Mathf.Sin(t) * Amount, 0f);
                    break;
                case Kind.Spin:
                    transform.localRotation = baseRot * Quaternion.Euler(0f, t * 57.3f, 0f);
                    break;
                case Kind.Flutter:
                    transform.localRotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(t) * 18f, Mathf.Sin(t * 1.7f) * 6f);
                    break;
                case Kind.Flap:
                    transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t) * Amount);
                    break;
                case Kind.Sway:
                    transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t) * Amount, 0f, Mathf.Cos(t * 0.8f) * Amount);
                    break;
                case Kind.Smoke:
                    float a = (Time.time - born) / Mathf.Max(0.05f, Life);
                    transform.localPosition = basePos + new Vector3(0f, a * 0.8f, 0f);
                    transform.localScale = baseScale * (1f + a * 1.8f);
                    if (a >= 1f) Destroy(gameObject);
                    break;
            }
        }
    }
}
