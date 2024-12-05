using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Nekoyume.ApiClient;
using UnityEngine;

namespace Nekoyume.UI
{
    public class SeasonPassNewPopup : PopupWidget
    {
        private const string LastReadingDayKey = "SEASON_PASS_NEW_POPUP_LAST_READING_DAY";
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

        private string LastReadingDayBySeasonId
        {
            get
            {
                var seasonId = string.Empty;
                if (ApiClients.Instance.SeasonPassServiceManager.CurrentSeasonPassData.TryGetValue(SeasonPassServiceClient.PassType.CouragePass, out var seasonPassSchema))
                {
                    seasonId = seasonPassSchema.Id.ToString();
                }

                return $"{LastReadingDayKey}{seasonId}";
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            PlayerPrefs.SetString(LastReadingDayBySeasonId, DateTime.Today.ToString(DateTimeFormat));
        }

        public bool HasUnread => !PlayerPrefs.HasKey(LastReadingDayBySeasonId);

        public void OnSeasonPassBtnClick()
        {
            base.Close();
            Find<SeasonPass>().Show(SeasonPassServiceClient.PassType.CouragePass);
        }
    }
}
