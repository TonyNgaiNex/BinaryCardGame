using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Jazz;
using Nex;
using TMPro;
using UnityEngine;

namespace Nex.BinaryCard
{
    public class OnePlayerManager : MonoBehaviour
    {
        [SerializeField] float chooseAnswerThreshold;
        [SerializeField] int initHealth;
        [SerializeField] TextMeshProUGUI text;
        [SerializeField] Transform cardLeftContainer;
        [SerializeField] Transform cardRightContainer;
        [SerializeField] List<CardBase> attackedCards;
        [SerializeField] List<CardBase> defensiveCards;
        [HideInInspector]public int health;
        int shield;
        OnePlayerDetectionEngine playerDetectionEngine;
        PlayerState currentPlayerState;
        UniTaskCompletionSource<PlayerAnswer> _playerAnswer;
        public void Initialize(OnePlayerDetectionEngine onePlayerDetectionEngine)
        {
            playerDetectionEngine = onePlayerDetectionEngine;
            onePlayerDetectionEngine.NewDetectionCapturedAndProcessed += ProcessPose;

            health = initHealth;
            shield = 0;
            text.text =$"Health: {health}, Shield: {shield}";
        }

        public void Damage(int damage)
        {
            if(shield > damage)shield -= damage;
            else
            {
                damage -= shield;
                shield = 0;
                health -= damage;
            }
            text.text =$"Health: {health}, Shield: {shield}";
            if (health <= 0)
            {
                text.text ="Dead";
            }
        }

        public void Shield(int amount)
        {
            shield += amount;
            text.text =$"Health: {health}, Shield: {shield}";
        }

        public void ResetShield()
        {
            shield = 0;
            text.text =$"Health: {health}, Shield: {shield}";
        }
        void ProcessPose(BodyPoseDetectionResult result)
        {
            if (currentPlayerState != PlayerState.AwaitAnswer) return;
            var processResult = result.processed;
            var dpi = playerDetectionEngine.DistancePerInch;

        }

        public async UniTask Turn(EnemyBase enemyBase)
        {
            var chosenCard = await ChooseCard();
            await ProcessCard(chosenCard, enemyBase);
        }

        async UniTask ProcessCard(CardBase card, EnemyBase enemy)
        {
            Shield(card.shield);
            enemy.Damage(card.damage);
            await UniTask.Delay(TimeSpan.FromSeconds(2));
        }

        async UniTask<CardBase> ChooseCard()
        {
            var randomAttackCard = attackedCards[UnityEngine.Random.Range(0, attackedCards.Count)];
            var randomDefenseCard = defensiveCards[UnityEngine.Random.Range(0, defensiveCards.Count)];

            var leftCard = Instantiate(randomAttackCard, cardLeftContainer);
            var rightCard = Instantiate(randomDefenseCard, cardRightContainer);

            leftCard.button.onClick.AddListener(() =>
            {
                _playerAnswer.TrySetResult(PlayerAnswer.Left);
            });
            rightCard.button.onClick.AddListener(() =>
            {
                _playerAnswer.TrySetResult(PlayerAnswer.Right);
            });

            _playerAnswer = new UniTaskCompletionSource<PlayerAnswer>();
            currentPlayerState = PlayerState.AwaitAnswer;
            var playerAnswer = await _playerAnswer.Task;
            currentPlayerState = PlayerState.None;

            Destroy(leftCard.gameObject);
            Destroy(rightCard.gameObject);

            if(playerAnswer==PlayerAnswer.Left)return leftCard;
            else return rightCard;
        }
    }

    public enum PlayerState
    {
        None,
        AwaitAnswer,
    }

    public enum PlayerAnswer
    {
        Left,
        Right,
    }
}
