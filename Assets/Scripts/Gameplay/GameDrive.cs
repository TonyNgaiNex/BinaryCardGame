using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nex.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nex.BinaryCard
{
    public class GameDrive : MonoBehaviour
    {
        [SerializeField] DetectionManager detectionManager = null!;
        [SerializeField] PreviewsManager previewsManager = null!;
        [SerializeField] Transform playersContainer = null!;
        [SerializeField] Transform enemyContainer;
        [SerializeField] EnemyBase enemyPrefab;
        [SerializeField]OnePlayerManager onePlayerManagerPrefab = null!;
        [SerializeField] int numOfPlayers = 1;

        readonly List<OnePlayerManager> players = new List<OnePlayerManager>();
        List<EnemyBase> enemies = new List<EnemyBase>();
        void Start()
        {
            if (Application.isEditor)
            {
                Application.runInBackground = true;
            }

            StartAsync().Forget();
        }

        async UniTask StartAsync()
        {
            detectionManager.Initialize(numOfPlayers);
            previewsManager.Initialize(numOfPlayers, detectionManager.CvDetectionManager,
                detectionManager.BodyPoseDetectionManager, detectionManager.PlayAreaController,
                detectionManager.SetupStateManager);

            for (int playerIndex = 0; playerIndex < numOfPlayers; playerIndex++)
            {
                var player = Instantiate(onePlayerManagerPrefab, playersContainer);
                player.Initialize(0, detectionManager.BodyPoseDetectionManager);
                player.processBattleEffect.AddListener(ProcessBattleEffect);
                players.Add(player);
            }

            await ScreenBlockerManager.Instance.Hide();

            await RunSetup();
            await RunGame();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SceneManager.LoadScene(GameConfigsManager.Instance.MainScene);
            }
        }

        #region Setup

        async UniTask RunSetup()
        {
            detectionManager.ConfigForSetup();
            await previewsManager.MoveIn(true);

            previewsManager.SetPromptText("Move into the frame");
            await detectionManager.SetupStateManager.WaitForGoodPlayerPosition();

            previewsManager.SetPromptText("Raise your hand to start");
            detectionManager.SetupStateManager.SetAllowPassingRaisingHandState(true);
            await detectionManager.SetupStateManager.WaitForRaiseHand();

            await previewsManager.MoveOut(true);
        }

        #endregion

        #region Game

        async UniTask RunGame()
        {
            detectionManager.ConfigForGameplay();
            await RunInitialSelectCard();
            await RunBattle();
        }

        async UniTask RunInitialSelectCard()
        {
            //TODO when the game start let player select cards
            await UniTask.Delay(1000);
        }

        async UniTask RunBattle()
        {
            enemies = new List<EnemyBase>();
            var enemy = Instantiate(enemyPrefab, enemyContainer);
            enemy.Initialize();
            enemies.Add(enemy);
            while (enemies.Count>0&& IsAllPlayerAlive())
            {
                RestAllPlayerShield();
                List<UniTask> playBattleTasks = new List<UniTask>();
                foreach (var player in players)
                {
                    var task = player.BattleTurn();
                    playBattleTasks.Add(task);
                }
                await UniTask.WhenAll(playBattleTasks);
            }
        }
        #endregion

        #region Players

        bool IsAllPlayerAlive()
        {
            foreach (var player in players)
            {
                if (player.characterAttributes[CharacterAttribute.Health] <= 0) return false;
            }
            return true;
        }

        void RestAllPlayerShield()
        {
            foreach (var player in players)
            {
                player.characterAttributes[CharacterAttribute.Shield] = 0;
            }
        }
        #endregion

        #region BattleProcess

        void ProcessBattleEffect(BattleEffect battleEffect, CharacterBase character)
        {
            var targets = GetBattleTarget(battleEffect.target, character);
            ProcessBattleEffect(battleEffect, targets);
        }

        List<CharacterBase> GetBattleTarget(BattleTarget battleTarget, CharacterBase character)
        {
            List<CharacterBase> targetCharacters = new List<CharacterBase>();
            switch (battleTarget)
            {
                case BattleTarget.Self:
                    targetCharacters.Add(character);
                    break;
                case BattleTarget.AllPlayer:
                    foreach (var player in players)
                        targetCharacters.Add(player);
                    break;
                case BattleTarget.LowestHealthPlayer:
                    var lowestHealthPlayer=players?
                        .OrderBy(c => c.characterAttributes[CharacterAttribute.Health])
                        .FirstOrDefault();
                    targetCharacters.Add(lowestHealthPlayer);
                    break;
                case BattleTarget.RandomEnemy:
                    var randomEnemy = enemies
                        .Where(c => c.characterAttributes[CharacterAttribute.Health] > 0)
                        .OrderBy(_ => Random.value)
                        .FirstOrDefault();
                    targetCharacters.Add(randomEnemy);
                    break;
                case BattleTarget.LowestHealthEnemy:
                    var lowestHealthEnemy=enemies?
                        .OrderBy(c => c.characterAttributes[CharacterAttribute.Health])
                        .FirstOrDefault();
                    targetCharacters.Add(lowestHealthEnemy);
                    break;
                default:
                    Debug.LogError($"Unknown battle target: {battleTarget}");
                    break;
            }
            return targetCharacters;

        #endregion
        }

        void ProcessBattleEffect(BattleEffect battleEffect, List<CharacterBase> characters)
        {
            switch (battleEffect.action)
            {
                case BattleAction.Attack:
                    foreach (var target in characters)
                        target.ReceiveDamage(battleEffect.value);
                    break;
                case BattleAction.Shield:
                    foreach (var target in characters)
                        target.ReceiveShield(battleEffect.value);
                    break;
                case BattleAction.Charge:
                    foreach (var target in characters)
                        target.characterAttributes[CharacterAttribute.Energy]+=battleEffect.value;
                    break;
                case BattleAction.Heal:
                    foreach (var target in characters)
                        target.characterAttributes[CharacterAttribute.Health]+=battleEffect.value;
                    break;
                default:
                    Debug.LogError($"Unknown battle effect: {battleEffect}");
                    break;
            }
        }
    }
}
