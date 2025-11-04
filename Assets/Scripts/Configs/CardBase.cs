using Nex.BinaryCard;
using UnityEngine;
using UnityEngine.UI;

public class CardBase : MonoBehaviour
{
    public Button button;
    public int damage;
    public int shield;

    public void Play(OnePlayerManager player, EnemyBase enemyBase)
    {
        player.Shield(shield);
        enemyBase.Damage(damage);
    }
}
