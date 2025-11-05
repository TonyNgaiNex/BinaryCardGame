using Cysharp.Threading.Tasks;
using DG.Tweening;
using Nex.BinaryCard;
using UnityEngine;
using UnityEngine.UI;

namespace Nex.BinaryCard
{
    public class CardBase : MonoBehaviour
    {
        public Button button;
        public async UniTask EntryAnimation()
        {
            await transform.DOScale(1.1f, 0.12f).SetEase(Ease.InCubic);
            await transform.DOScale(1f, 0.12f).SetEase(Ease.OutCubic);
        }

        public async UniTask ChosenAnimation()
        {
            await transform.DOScale(1.25f, 0.5f).SetEase(Ease.OutCubic);
        }

        public async UniTask ExitAnimation()
        {
            await transform.DOScale(0f, 0.2f).SetEase(Ease.OutCubic);
        }
    }

}
