using UnityEngine;

namespace GameClient
{
    /// <summary>Yeni eğitilen askerin kısa görünümü: ileri yürür, sonra küçülerek kaybolur (yalnızca görsel).</summary>
    public sealed class SoldierCameoMotion : MonoBehaviour
    {
        private float t;
        private Vector3 scale0;
        private ISoldierAnim rig;

        private void Start()
        {
            scale0 = transform.localScale;
            rig = GetComponent<ISoldierAnim>();
            if (rig != null) rig.SetMoving(true);
        }

        private void Update()
        {
            t += Time.deltaTime;
            if (t < 2.2f) transform.position += transform.forward * (0.45f * Time.deltaTime);
            else if (rig != null) rig.SetMoving(false);
            if (t > 2.6f) transform.localScale = scale0 * Mathf.Max(0f, 1f - (t - 2.6f) / 0.4f);
            if (t > 3f) Destroy(gameObject);
        }
    }
}
