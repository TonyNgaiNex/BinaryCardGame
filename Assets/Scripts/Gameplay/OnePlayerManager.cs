using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Jazz;
using Nex.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Nex.BinaryCard
{
    public class OnePlayerManager : CharacterBase
    {
        [SerializeField] OnePlayerDetectionEngine onePlayerDetectionEngine;
        [SerializeField] Transform cardLeftContainer;
        [SerializeField] Transform cardRightContainer;
        [SerializeField] Slider leftSlider;
        [SerializeField] Slider rightSlider;
        [SerializeField] List<BattleCard> initialBattleCards;

        UniTaskCompletionSource<PlayerAnswer> playerAnswer;
        public void Initialize(int aPlayerIndex, BodyPoseDetectionManager aBodyPoseDetectionManager)
        {
            base.Initialize();
            onePlayerDetectionEngine.Initialize(aPlayerIndex, aBodyPoseDetectionManager);

            leftSlider.gameObject.SetActive(false);
            rightSlider.gameObject.SetActive(false);
        }
        void ProcessPose(BodyPoseDetectionResult result)
        {
            var chestY = onePlayerDetectionEngine.Chest.position.y;
            var raiseLeft = onePlayerDetectionEngine.LeftHand.position.y>chestY;
            var raiseRight = onePlayerDetectionEngine.RightHand.position.y>chestY;

            leftSlider.value -= .5f*Time.deltaTime;
            rightSlider.value -= .5f*Time.deltaTime;

            if (raiseLeft)leftSlider.value += 1f*Time.deltaTime;
            if (raiseRight)rightSlider.value += 1f*Time.deltaTime;

            if (leftSlider.value >= 1) playerAnswer.TrySetResult(PlayerAnswer.Left);
            if (rightSlider.value >= 1) playerAnswer.TrySetResult(PlayerAnswer.Right);
        }

        public async UniTask BattleTurn()
        {
            initialBattleCards.Shuffle();
            var randomTwoCards = initialBattleCards.Take(2).ToList();
            var chosenCard =  await PlayerChooseBetweenTwoOption(randomTwoCards[0], randomTwoCards[1]) as BattleCard;
            await ProcessBattleCard(chosenCard);
        }

        async UniTask ProcessBattleCard(BattleCard card)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2));
            foreach(var effect in card.cardEffects)
                processBattleEffect.Invoke(effect, this);
        }

        async UniTask<CardBase> PlayerChooseBetweenTwoOption(CardBase leftCardPrefab, CardBase rightCardPrefab)
        {
            // init left and right card
            var leftCard = Instantiate(leftCardPrefab, cardLeftContainer);
            var rightCard = Instantiate(rightCardPrefab, cardRightContainer);

            var leftEntryAnimation = leftCard.EntryAnimation();
            var rightEntryAnimation = rightCard.EntryAnimation();
            await UniTask.WhenAll(rightEntryAnimation, leftEntryAnimation);

            // Add button for debug usage
            leftCard.button.onClick.AddListener(() =>
            {
                this.playerAnswer.TrySetResult(PlayerAnswer.Left);
            });
            rightCard.button.onClick.AddListener(() =>
            {
                this.playerAnswer.TrySetResult(PlayerAnswer.Right);
            });

            // start the detection
            leftSlider.gameObject.SetActive(true);
            rightSlider.gameObject.SetActive(true);
            leftSlider.value = 0;
            rightSlider.value = 0;
            this.playerAnswer = new UniTaskCompletionSource<PlayerAnswer>();
            onePlayerDetectionEngine.NewDetectionCapturedAndProcessed += ProcessPose;

            // finish detection
            var playerAnswer = await this.playerAnswer.Task;
            onePlayerDetectionEngine.NewDetectionCapturedAndProcessed -= ProcessPose;
            leftSlider.gameObject.SetActive(false);
            rightSlider.gameObject.SetActive(false);

            var chosenCard = playerAnswer==PlayerAnswer.Left ? leftCard : rightCard;
            await chosenCard.ChosenAnimation();

            var leftExitAnimation = leftCard.ExitAnimation();
            var rightExitAnimation = rightCard.ExitAnimation();
            await UniTask.WhenAll(leftExitAnimation, rightExitAnimation);

            Destroy(leftCard.gameObject);
            Destroy(rightCard.gameObject);

            return chosenCard;
        }
    }

    public enum PlayerAnswer
    {
        Left,
        Right,
    }
}
