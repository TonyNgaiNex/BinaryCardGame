using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Nex.BinaryCard
{
    public class ActionVisualEffect : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI displayText;
        [SerializeField] Vector2 movement;
        [SerializeField] float duration;

        public async UniTask Animation(string text, Color color)
        {
            displayText.text = text;
            displayText.color = color;

            var targetPosition = displayText.rectTransform.anchoredPosition+movement;
            await displayText.rectTransform.DOLocalMove(targetPosition, duration);

            Destroy(gameObject);
        }
    }

}
