using System.Collections.Generic;
using UnityEngine;

namespace GameClient
{
    /// <summary>
    /// Karakter iskeleti (yalnızca görsel): "legL/legR" ve "armL/armR" adlı eklemleri yürürken sallar, saldırıda
    /// sağ kolla vuruş yapar. VillageArt.Human bu adlarla eklem üretir; at bacakları da aynı adları kullanır.
    /// </summary>
    public sealed class CharacterRig : MonoBehaviour, ISoldierAnim
    {
        private readonly List<Transform> legsL = new List<Transform>(), legsR = new List<Transform>();
        private readonly List<Transform> armsL = new List<Transform>(), armsR = new List<Transform>();
        private float phase, walk, targetWalk, attackT = -1f;
        public float StrideSpeed = 11f, StrideDeg = 34f;

        private void Awake()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                switch (t.name)
                {
                    case "legL": legsL.Add(t); break;
                    case "legR": legsR.Add(t); break;
                    case "armL": armsL.Add(t); break;
                    case "armR": armsR.Add(t); break;
                }
            }
            phase = Random.value * 6.28f;
        }

        public void SetMoving(bool moving) => targetWalk = moving ? 1f : 0f;

        public void SetSpeed(float v) => StrideSpeed = Mathf.Clamp(v * 22f, 5f, 26f);

        public float TriggerAttack(float interval) { attackT = 0f; return 0.17f; }

        private void Update()
        {
            float dt = Time.deltaTime;
            walk = Mathf.MoveTowards(walk, targetWalk, dt * 6f);
            phase += dt * StrideSpeed * Mathf.Max(walk, 0.0001f);
            float sw = Mathf.Sin(phase) * StrideDeg * walk;
            for (int i = 0; i < legsL.Count; i++) legsL[i].localRotation = Quaternion.Euler(sw, 0f, 0f);
            for (int i = 0; i < legsR.Count; i++) legsR[i].localRotation = Quaternion.Euler(-sw, 0f, 0f);
            for (int i = 0; i < armsL.Count; i++) armsL[i].localRotation = Quaternion.Euler(-sw * 0.8f, 0f, 0f);

            float strike = 0f;
            if (attackT >= 0f)
            {
                attackT += dt;
                float a = attackT / 0.35f;
                if (a >= 1f) attackT = -1f;
                else strike = Mathf.Sin(a * Mathf.PI); // kaldır ve indir
            }
            for (int i = 0; i < armsR.Count; i++)
                armsR[i].localRotation = Quaternion.Euler(sw * 0.8f * (1f - strike) - 115f * strike, 0f, 0f);
        }
    }
}
