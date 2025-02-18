using IngameDebugConsole;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Nekoyume.UI;
using System.Linq;
using System.Reactive.Linq;
using Nekoyume.ApiClient;
using Nekoyume.State;

namespace Nekoyume.Game.Util
{
    public static class ScreenClear
    {
        public static void ClearScreen()
        {
            var screenBoaderH = GameObject.Find("BackGroundClearing").transform.Find("HorizontalLetterbox").gameObject;
            var screenBoaderV = GameObject.Find("BackGroundClearing").transform.Find("VerticalLetterBox").gameObject;
            if (screenBoaderH != null)
            {
                screenBoaderH.SetActive(false);
            }

            if (screenBoaderV != null)
            {
                screenBoaderV.SetActive(false);
            }
        }

        public static void ClearScreen(bool isHorizontal, float letterBoxSize)
        {
            var screenBoaderH = GameObject.Find("BackGroundClearing").transform.Find("HorizontalLetterbox").gameObject;
            var screenBoaderV = GameObject.Find("BackGroundClearing").transform.Find("VerticalLetterBox").gameObject;

            if (screenBoaderH != null)
            {
                screenBoaderH.SetActive(isHorizontal);
                if (isHorizontal)
                {
                    var rt = screenBoaderH.GetComponent<RectTransform>();
                    rt.offsetMax = new Vector2(rt.offsetMax.x, -letterBoxSize);
                    rt.offsetMin = new Vector2(rt.offsetMin.x, letterBoxSize);
                }
            }

            if (screenBoaderV != null)
            {
                screenBoaderV.SetActive(!isHorizontal);
                if (!isHorizontal)
                {
                    var rt = screenBoaderV.GetComponent<RectTransform>();
                    rt.offsetMax = new Vector2(-letterBoxSize, rt.offsetMax.y);
                    rt.offsetMin = new Vector2(letterBoxSize, rt.offsetMin.y);
                }
            }

            BlackScreenClearing().Forget();

            async UniTaskVoid BlackScreenClearing()
            {
                var blackscreenImageH = screenBoaderH.GetComponent<UnityEngine.UI.Image>();
                var blackscreenImageV = screenBoaderV.GetComponent<UnityEngine.UI.Image>();
                blackscreenImageH.color = Color.black;
                blackscreenImageV.color = Color.black;
                await UniTask.DelayFrame(2);
                blackscreenImageH.color = new Color(0, 0, 0, 0);
                blackscreenImageV.color = new Color(0, 0, 0, 0);
            }
        }
    }

    public class IngameDebugConsoleCommands
    {
        public static GameObject IngameDebugConsoleObj;

        public static void Initailize()
        {
            DebugLogConsole.AddCommand("screen", "Change Screen Ratio State ", () =>
            {
                ActionCamera.instance.ChangeRatioState();
                var raidCam = Object.FindObjectOfType<RaidCamera>();
                if (raidCam != null)
                {
                    raidCam.ChangeRatioState();
                }
            });

            DebugLogConsole.AddCommand("clo", "show current commandline option", () => { Game.instance.ShowCLO(); });

            DebugLogConsole.AddCommand("patrol-avatar", "Sync patrol reward avatar info", () =>
            {
                var avatarAddress = Game.instance.States.CurrentAvatarState.address;
                PatrolReward.LoadAvatarInfo(avatarAddress, ReactiveAvatarState.PatrolRewardClaimedBlockIndex);
            });

            DebugLogConsole.AddCommand("adventureboss-info", "Clear Screen", () =>
            {
                NcDebug.Log("------------------------------------------");
                NcDebug.Log("[AdventureBoss] Info");
                if (Game.instance.AdventureBossData.SeasonInfo.Value is null)
                {
                    NcDebug.Log("[AdventureBoss] No Season Info");
                }
                else
                {
                    NcDebug.Log($"[AdventureBoss] Season Id : {Game.instance.AdventureBossData.SeasonInfo.Value.Season}");
                    NcDebug.Log($"[AdventureBoss] Season StartBlockIndex : {Game.instance.AdventureBossData.SeasonInfo.Value.StartBlockIndex}");
                    NcDebug.Log($"[AdventureBoss] Season EndBlockIndex : {Game.instance.AdventureBossData.SeasonInfo.Value.EndBlockIndex}");
                    NcDebug.Log($"[AdventureBoss] Season NextStartBlockIndex : {Game.instance.AdventureBossData.SeasonInfo.Value.NextStartBlockIndex}");
                    NcDebug.Log($"[AdventureBoss] Season UsedNcg : {Game.instance.AdventureBossData.GetCurrentBountyPrice().MajorUnit.ToString("#,0")}");
                }

                if (Game.instance.AdventureBossData.ExploreBoard.Value is null)
                {
                    NcDebug.Log("[AdventureBoss] No ExploreBoard Info");
                }
                else
                {
                    NcDebug.Log($"[AdventureBoss] Season TotalPoint : {Game.instance.AdventureBossData.ExploreBoard.Value.TotalPoint}");
                    if (Game.instance.AdventureBossData.ExploreBoard.Value is null)
                    {
                        NcDebug.Log($"[AdventureBoss] Season ExplorerListCount : 0");
                    }
                    else
                    {
                        NcDebug.Log($"[AdventureBoss] Season ExplorerListCount : {Game.instance.AdventureBossData.ExploreBoard.Value.ExplorerCount}");
                    }

                    NcDebug.Log($"[AdventureBoss] Season UsedApPotion : {Game.instance.AdventureBossData.ExploreBoard.Value.UsedApPotion}");
                    NcDebug.Log($"[AdventureBoss] Season UsedGoldenDust : {Game.instance.AdventureBossData.ExploreBoard.Value.UsedGoldenDust}");
                    NcDebug.Log($"[AdventureBoss] Season TotalPoint : {Game.instance.AdventureBossData.ExploreBoard.Value.TotalPoint}");
                }

                if (Game.instance.AdventureBossData.BountyBoard.Value is null)
                {
                    NcDebug.Log("[AdventureBoss] No BountyBoard Info");
                }
                else
                {
                    NcDebug.Log($"[AdventureBoss] BountyBoard Count : {Game.instance.AdventureBossData.BountyBoard.Value.Investors.Count()}");
                    foreach (var item in Game.instance.AdventureBossData.BountyBoard.Value.Investors)
                    {
                        NcDebug.Log($"[AdventureBoss] BountyBoard Investor : {item.Price}  {item.Count}  {item.AvatarAddress}");
                    }
                }
            });
        }
    }
}
