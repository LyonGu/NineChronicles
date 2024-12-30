using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI
{
    using Game;
    using Helper;
    using L10n;
    using Nekoyume.Model.AdventureBoss;
    using System.Linq;
    using TMPro;
    using UniRx;
    using Unity.Mathematics;
    using UnityEngine.UI;

    public class AdventureBossRewardInfoPopup : PopupWidget
    {
        [SerializeField] private ToggleGroup toggleGroup;
        [SerializeField] private Toggle toggleScore;
        [SerializeField] private Toggle toggleFloor;
        [SerializeField] private Toggle toggleOperational;
        [SerializeField] private GameObject contentsScore;
        [SerializeField] private GameObject contentsFloor;
        [SerializeField] private GameObject contentsOperational;
        [SerializeField] private TextMeshProUGUI remainingBlockTime;

        [Header("Score Contents")]
        [SerializeField] private TextMeshProUGUI totalScore;

        [SerializeField] private TextMeshProUGUI myScore;
        [SerializeField] private BaseItemView[] baseItemViews;
        [SerializeField] private TextMeshProUGUI scoreSubDesc;

        [Header("Floor Contents")]
        [SerializeField] private FloorRewardCell[] floorRewardCells;
        [SerializeField] private Scrollbar floorRewardScrollbar;

        [Header("Operational Contents")]
        [SerializeField] private Transform bossImgRoot;

        [SerializeField] private TextMeshProUGUI currentSeasonBossName;
        [SerializeField] private BaseItemView[] currentSeasonBossRewardViews;
        [SerializeField] private BossRewardCell[] bossRewardCells;

        private readonly List<System.IDisposable> _disposablesByEnable = new();
        private long _seasonEndBlock;
        private int _bossId;
        private GameObject _bossImage;

        private void SetBossData(int bossId)
        {
            if (_bossId != bossId)
            {
                if (_bossImage != null)
                {
                    DestroyImmediate(_bossImage);
                }

                _bossId = bossId;
                _bossImage = Instantiate(SpriteHelper.GetBigCharacterIconFace(_bossId),
                    bossImgRoot);
                _bossImage.transform.localPosition = Vector3.zero;
            }
        }

        protected override void Awake()
        {
            toggleScore.onValueChanged.AddListener((isOn) =>
            {
                contentsScore.SetActive(isOn);
                if (!isOn)
                {
                    return;
                }

                RefreshToggleScore();
            });
            toggleFloor.onValueChanged.AddListener((isOn) =>
            {
                contentsFloor.SetActive(isOn);
                if (!isOn)
                {
                    return;
                }

                if (Game.instance.AdventureBossData.SeasonInfo.Value == null)
                {
                    NcDebug.LogError("SeasonInfo is null");
                    foreach (var item in floorRewardCells)
                    {
                        item.gameObject.SetActive(false);
                    }

                    return;
                }

                var seasonInfo = Game.instance.AdventureBossData.SeasonInfo.Value;
                var tableSheets = TableSheets.Instance;

                var bossRow = tableSheets.AdventureBossSheet.Values.FirstOrDefault(row => row.BossId == seasonInfo.BossId);
                if (bossRow == null)
                {
                    NcDebug.LogError($"BossSheet is not found. BossId: {seasonInfo.BossId}");
                    foreach (var item in floorRewardCells)
                    {
                        item.gameObject.SetActive(false);
                    }
                    return;
                }

                var floorRows = tableSheets.AdventureBossFloorSheet.Values.Where(row =>
                    row.AdventureBossId == bossRow.Id);
                var floorRewardDatas = TableSheets.Instance.AdventureBossFloorFirstRewardSheet.Values.Join(floorRows,
                    rewardRow => rewardRow.FloorId,
                    floorRow => floorRow.Id,
                    (rewardRow, floorRow) => new
                    {
                        floorRow.Floor,
                        rewardRow.Rewards
                    }).OrderByDescending(row => row.Floor).ToList();

                for (var i = 0; i < floorRewardCells.Length; i++)
                {
                    if (i >= floorRewardDatas.Count)
                    {
                        floorRewardCells[i].gameObject.SetActive(false);
                        NcDebug.LogError($"FloorRewardData is not enough. Index: {i}");
                        continue;
                    }

                    floorRewardCells[i].gameObject.SetActive(true);
                    floorRewardCells[i].SetData(floorRewardDatas[i].Floor, floorRewardDatas[i].Rewards);
                }
                if (floorRewardDatas.Count - 4 <= 0)
                {
                    NcDebug.LogError($"FloorRewardData is not enough. floorRewardDatas.Count: {floorRewardDatas.Count}");
                    return;
                }
                var exploreInfo = Game.instance.AdventureBossData.ExploreInfo.Value;
                if (exploreInfo != null)
                {
                    // 스크롤러에 보이는 셀 개수만큼 제거하여 위치 조정
                    float scrollPos = math.max(exploreInfo.Floor - 1, 0) / ((float)floorRewardDatas.Count - 4);
                    floorRewardScrollbar.value = scrollPos;
                }
            });
            toggleOperational.onValueChanged.AddListener((isOn) =>
            {
                contentsOperational.SetActive(isOn);
                if (!isOn)
                {
                    return;
                }

                if (Game.instance.AdventureBossData.SeasonInfo.Value != null)
                {
                    var bossId = Game.instance.AdventureBossData.SeasonInfo.Value.BossId;
                    SetBossData(bossId);
                    currentSeasonBossName.text = L10nManager.LocalizeCharacterName(bossId);
                }

                if (Game.instance.AdventureBossData.BountyBoard.Value != null)
                {
                    var bountyBoard = Game.instance.AdventureBossData.BountyBoard.Value;
                    var currentInvestorInfo = Game.instance.AdventureBossData.GetCurrentInvestorInfo();

                    if (currentInvestorInfo != null)
                    {
                        var wantedReward = Game.instance.AdventureBossData.GetCurrentBountyRewards();
                        var itemIndex = 0;
                        foreach (var item in wantedReward.ItemReward)
                        {
                            if (itemIndex >= currentSeasonBossRewardViews.Length)
                            {
                                NcDebug.LogError("currentSeasonBossRewardViews is not enough");
                                break;
                            }

                            currentSeasonBossRewardViews[itemIndex].ItemViewSetItemData(item.Key, item.Value);
                            itemIndex++;
                        }

                        foreach (var fav in wantedReward.FavReward)
                        {
                            if (itemIndex >= currentSeasonBossRewardViews.Length)
                            {
                                NcDebug.LogError("currentSeasonBossRewardViews is not enough");
                                break;
                            }

                            if (currentSeasonBossRewardViews[itemIndex].ItemViewSetCurrencyData(fav.Key, fav.Value))
                            {
                                itemIndex++;
                            }
                        }

                        for (; itemIndex < currentSeasonBossRewardViews.Length; itemIndex++)
                        {
                            currentSeasonBossRewardViews[itemIndex].gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        if (bountyBoard.FixedRewardItemId != null)
                        {
                            currentSeasonBossRewardViews[0].ItemViewSetItemData(bountyBoard.FixedRewardItemId.Value, 0);
                        }

                        if (bountyBoard.FixedRewardFavId != null)
                        {
                            currentSeasonBossRewardViews[0].ItemViewSetCurrencyData(bountyBoard.FixedRewardFavId.Value, 0);
                        }

                        if (bountyBoard.RandomRewardItemId != null)
                        {
                            currentSeasonBossRewardViews[1].ItemViewSetItemData(bountyBoard.RandomRewardItemId.Value, 0);
                        }

                        if (bountyBoard.RandomRewardFavId != null)
                        {
                            currentSeasonBossRewardViews[1].ItemViewSetCurrencyData(bountyBoard.RandomRewardFavId.Value, 0);
                        }
                    }

                    var adventureBossSheet = TableSheets.Instance.AdventureBossSheet.Values.ToList();
                    for (var i = 0; i < bossRewardCells.Length; i++)
                    {
                        if (i < adventureBossSheet.Count)
                        {
                            bossRewardCells[i].SetData(adventureBossSheet[i]);
                        }
                        else
                        {
                            bossRewardCells[i].gameObject.SetActive(false);
                        }
                    }
                }
            });
        }

        private void RefreshToggleScore()
        {
            var adventureBossData = Game.instance.AdventureBossData;

            var myScoreValue = 0;
            if (adventureBossData.ExploreInfo.Value != null)
            {
                myScoreValue = adventureBossData.ExploreInfo.Value.Score;
            }

            double contribution = 0;
            if (adventureBossData.ExploreBoard.Value != null && adventureBossData.ExploreBoard.Value.TotalPoint != 0)
            {
                totalScore.text = adventureBossData.ExploreBoard.Value.TotalPoint.ToString("#,0");
                contribution = myScoreValue == 0 ? 0 : (double)myScoreValue / adventureBossData.ExploreBoard.Value.TotalPoint * 100;
            }
            else
            {
                totalScore.text = "0";
            }

            var randomRewardText = "0";
            if (adventureBossData.BountyBoard.Value != null)
            {
                var raffleReward = AdventureBossHelper.CalculateRaffleReward(adventureBossData.BountyBoard.Value);
                randomRewardText = raffleReward.MajorUnit.ToString("#,0");
            }

            scoreSubDesc.text = L10nManager.Localize("UI_ADVENTURE_BOSS_REWARD_INFO_SCORE_SUB_DESC", randomRewardText);

            myScore.text = $"{myScoreValue.ToString("#,0")} ({contribution.ToString("F2")}%)";

            var myReward = adventureBossData.GetCurrentExploreRewards();
            var i = 0;
            if (myReward.NcgReward != null && myReward.NcgReward.HasValue && !myReward.NcgReward.Value.RawValue.IsZero)
            {
                baseItemViews[i].ItemViewSetCurrencyData(myReward.NcgReward.Value);
                i++;
            }

            foreach (var item in myReward.ItemReward)
            {
                baseItemViews[i].ItemViewSetItemData(item.Key, item.Value);
                i++;
            }

            foreach (var fav in myReward.FavReward)
            {
                if (baseItemViews[i].ItemViewSetCurrencyData(fav.Key, fav.Value))
                {
                    i++;
                }
            }

            for (; i < baseItemViews.Length; i++)
            {
                baseItemViews[i].gameObject.SetActive(false);
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (Nekoyume.Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                return;
            }

            Game.instance.AdventureBossData.SeasonInfo.Subscribe(RefreshSeasonInfo).AddTo(_disposablesByEnable);
            Game.instance.Agent.BlockIndexSubject
                .StartWith(Game.instance.Agent.BlockIndex)
                .Subscribe(UpdateViewAsync)
                .AddTo(_disposablesByEnable);

            base.Show(ignoreShowAnimation);

            contentsFloor.SetActive(false);
            contentsOperational.SetActive(false);
            toggleScore.isOn = true;
            RefreshToggleScore();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            _disposablesByEnable.DisposeAllAndClear();
        }

        private void RefreshSeasonInfo(SeasonInfo seasonInfo)
        {
            if (seasonInfo == null)
            {
                return;
            }

            _seasonEndBlock = seasonInfo.EndBlockIndex;
        }

        private void UpdateViewAsync(long blockIndex)
        {
            var remainingBlockIndex = _seasonEndBlock - blockIndex;
            if (remainingBlockIndex < 0)
            {
                Close();
                return;
            }

            remainingBlockTime.text = $"{remainingBlockIndex:#,0}({remainingBlockIndex.BlockRangeToTimeSpanString()})";
        }
    }
}
