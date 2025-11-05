using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Nex.BinaryCard
{
    public class EnemyBase : MonoBehaviour
    {
        [SerializeField] int initialHealth = 100;
        [SerializeField] TextMeshProUGUI text;
        [HideInInspector]public int health;
        List<OnePlayerManager> player;
        public void Initialize(List<OnePlayerManager> aPlayers)
        {
            health = initialHealth;
            text.text = $"Health: {health}";

            player = aPlayers;
        }

        public void Damage(int damage)
        {
            health -= damage;
            text.text = $"Health: {health}";
            if (health <= 0)
                Death();
        }

        public async UniTask Turn()
        {
            player[0].Damage(1);
            await UniTask.Delay(TimeSpan.FromSeconds(2));
        }
        void Death()
        {
            text.text = $"Dead";
        }
    }
}
