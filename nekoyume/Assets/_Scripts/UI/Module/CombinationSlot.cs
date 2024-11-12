#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace Nekoyume.UI.Module
{
    using Game;
    using Scroller;
    using UniRx;

    public class CombinationSlot : MonoBehaviour
    {
#region Internal
        private enum BackGroundType
        {
            Default,
            CustomCraft,
        }
        
        [Serializable]
        private class BackGroundGroup
        {
            [SerializeField]
            private BackGroundType backGroundType = BackGroundType.Default;
            
            [SerializeField]
            private GameObject[] backGroundObjects = null!;
            
            public BackGroundType BackGroundType => backGroundType;
            
            public void SetActive(bool isActive)
            {
                foreach (var backGroundObject in backGroundObjects)
                {
                    backGroundObject.SetActive(isActive);
                }
            }
        }
        
        public enum SlotUIState
        {
            Empty,
            Appraise,
            Working,
            WaitingReceive,
            Locked
        }
#endregion Internal
        
        [SerializeField]
        private SimpleItemView itemView = null!;

        [SerializeField]
        private SimpleItemView waitingReceiveItemView = null!;

        [SerializeField]
        private TouchHandler touchHandler = null!;

        [SerializeField]
        private Slider progressBar = null!;

        [SerializeField]
        private Image hasNotificationImage = null!;

        [SerializeField]
        private TextMeshProUGUI requiredBlockIndexText = null!;

        [SerializeField]
        private TextMeshProUGUI requiredTimeText = null!;

        [SerializeField]
        private TextMeshProUGUI itemNameText = null!;

        [SerializeField]
        private TextMeshProUGUI hourglassCountText = null!;

        [SerializeField]
        private TextMeshProUGUI preparingText = null!;

        [SerializeField]
        private TextMeshProUGUI waitingReceiveText = null!;

        [Header("StateContainer")]
        [SerializeField]
        private GameObject lockContainer = null!;

        [SerializeField]
        private GameObject baseContainer = null!;

        [SerializeField]
        private GameObject noneContainer = null!;

        [SerializeField]
        private GameObject preparingContainer = null!;

        [SerializeField]
        private GameObject workingContainer = null!;

        [SerializeField]
        private GameObject waitReceiveContainer = null!;

        [SerializeField]
        private PetSelectButton petSelectButton = null!;
        
        [Header("For CustomCraft")]
        [SerializeField]
        private BackGroundGroup[] backGroundGroups = null!;
        
        [SerializeField]
        private GameObject customCraftObject = null!;
        
        [SerializeField]
        private GameObject randomOnlyIcon = null!;

        private CombinationSlotLockObject _lockObject = null!;
        private CombinationSlotState? _state;
        private int _slotIndex;

        // TODO: 클라이언트 액션 에러처리 추가
        private long _sendActionBlockIndex;
        // 액션 렌더보다 block 업데이트가 더 빠른 경우, _state가 갱신이 안됬는데
        // BlockIndex가 갱신되어 UI가 잘못 갱신될 수 있음
        private bool _isWaitingCombinationActionRender;

        private readonly List<IDisposable> _disposablesOfOnEnable = new();

        public CombinationSlotState? State => _state;

        public SlotUIState UIState { get; private set; } = SlotUIState.Locked;

        public void OnCraftActionRender()
        {
            UIState = UIState switch
            {
                SlotUIState.Appraise => SlotUIState.Working,
                SlotUIState.WaitingReceive => SlotUIState.Empty,
                _ => UIState
            };

            UpdateInformation(Game.instance.Agent.BlockIndex);
            _isWaitingCombinationActionRender = false;
        }

        public void OnSendRapidCombinationAction()
        {
            if (UIState != SlotUIState.Working)
            {
                NcDebug.LogWarning("Invalid UIState.");
            }

            UIState = SlotUIState.WaitingReceive;
            UpdateInformation(Game.instance.Agent.BlockIndex);
            _isWaitingCombinationActionRender = false;
        }

        public void OnSendCombinationAction(long requiredBlockIndex, ItemUsable itemUsable)
        {
            if (UIState != SlotUIState.Empty)
            {
                NcDebug.LogWarning("Invalid UIState.");
            }
            _isWaitingCombinationActionRender = true;

            UIState = SlotUIState.Appraise;

            var currentBlockIndex = Game.instance.Agent.BlockIndex;
            UpdateItemInformation(itemUsable, UIState);
            UpdateRequiredBlockInformation(
                requiredBlockIndex + currentBlockIndex,
                currentBlockIndex,
                currentBlockIndex);
            UpdateInformation(currentBlockIndex);
            SetItemUsableImage(itemUsable, true);
            _sendActionBlockIndex = currentBlockIndex;
        }

        private void Awake()
        {
            touchHandler.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnClickSlot(UIState, _state, _slotIndex, Game.instance.Agent.BlockIndex);
            }).AddTo(gameObject);

            _lockObject = lockContainer.GetComponent<CombinationSlotLockObject>();
        }

        private void OnEnable()
        {
            Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(OnUpdateBlock)
                .AddTo(_disposablesOfOnEnable);
            ReactiveAvatarState.Inventory
                .Select(_ => Game.instance.Agent.BlockIndex)
                .Subscribe(OnUpdateBlock)
                .AddTo(_disposablesOfOnEnable);
        }

        private void OnDisable()
        {
            _disposablesOfOnEnable.DisposeAllAndClear();
        }

#region Lock
        private void SetLockObject()
        {
            if (_lockObject == null)
            {
                NcDebug.LogError("Can't find CombinationSlotLockObject component.");
                return;
            }

            if (TableSheets.Instance.UnlockCombinationSlotCostSheet.TryGetValue(_slotIndex, out var data))
            {
                _lockObject.SetData(data);
            }
        }

        public void SetLockLoading(bool isLoading)
        {
            _lockObject.SetLoading(isLoading);
        }
#endregion Lock

        public void Clear()
        {
            UIState = SlotUIState.Locked;
            _state = null;
            _lockObject.SetLoading(false);
            _sendActionBlockIndex = 0;
            _isWaitingCombinationActionRender = false;
            UpdateInformation(Game.instance.Agent.BlockIndex);
        }

        public void SetSlot(
            long currentBlockIndex,
            int slotIndex,
            CombinationSlotState? state = null)
        {
            _slotIndex = slotIndex;
            _state = state;
            UpdateSlotState();
            UpdateInformation(UIState, currentBlockIndex, state);
            SetLockObject();
        }

        private void UpdateSlotState()
        {
            switch (UIState)
            {
                case SlotUIState.Empty:
                    break;
                case SlotUIState.Appraise:
                    break;
                case SlotUIState.Working:
                    break;
                case SlotUIState.WaitingReceive:
                    break;
                case SlotUIState.Locked:
                    if (_state?.IsUnlocked ?? false)
                    {
                        UIState = SlotUIState.Empty;
                    }
                    break;
            }
        }

#region OnBlockRender
        private void OnUpdateBlock(long currentBlockIndex)
        {
            switch (UIState)
            {
                case SlotUIState.Empty:
                    OnBlockRenderEmpty(currentBlockIndex);
                    break;
                case SlotUIState.Appraise:
                    OnBlockRenderAppraise(currentBlockIndex);
                    break;
                case SlotUIState.Working:
                    OnBlockRenderWorking(currentBlockIndex);
                    break;
                case SlotUIState.WaitingReceive:
                    OnBlockRenderWaitingReceive(currentBlockIndex);
                    break;
                case SlotUIState.Locked:
                    OnBlockRenderLocked(currentBlockIndex);
                    break;
            }
        }

        private void OnBlockRenderEmpty(long currentBlockIndex)
        {
            // state result에 값이 있고, 슬롯을 사용할 수 없는 경우 Empty로 변경
            // 이 때, Validate에서 IsUnlocked는 항상 true라고 가정함
            if (_state?.Result == null || _state.ValidateV2(currentBlockIndex))
            {
                return;
            }

            UIState = SlotUIState.Working;
            UpdateInformation(currentBlockIndex);
        }

        private void OnBlockRenderAppraise(long currentBlockIndex)
        {
            if (_state == null)
            {
                UIState = SlotUIState.Empty;
                UpdateInformation(currentBlockIndex);
                return;
            }

            var startBlockIndex = Math.Max(_sendActionBlockIndex, _state.WorkStartBlockIndex);
            if (currentBlockIndex <= startBlockIndex || _isWaitingCombinationActionRender)
            {
                return;
            }

            UIState = SlotUIState.Working;
            UpdateInformation(currentBlockIndex);
        }

        private void OnBlockRenderWorking(long currentBlockIndex)
        {
            if (_state?.Result == null || _sendActionBlockIndex >= currentBlockIndex || !_state.ValidateV2(currentBlockIndex))
            {
                return;
            }
            // 제작 완료(BlockIndex체크)나 제작 아이템 정보가 없는 경우 Empty로 변경
            UIState = SlotUIState.Empty;
            UpdateInformation(currentBlockIndex);
        }

        private void OnBlockRenderWaitingReceive(long currentBlockIndex)
        {
            // Not Cached && slot null
            if (_state?.Result != null)
            {
                return;
            }

            UIState = SlotUIState.Empty;
            UpdateInformation(currentBlockIndex);
        }

        private void OnBlockRenderLocked(long currentBlockIndex)
        {
            // Do nothing.
        }
#endregion OnBlockRender

        private void UpdateInformation(long blockIndex)
        {
            UpdateInformation(UIState, blockIndex, _state);
        }

        private void UpdateInformation(
            SlotUIState uiState,
            long currentBlockIndex,
            CombinationSlotState? state)
        {
            UpdatePetButton(uiState, state);

            switch (uiState)
            {
                case SlotUIState.Empty:
                    SetContainer(false, false, true, false);
                    itemView.Clear();
                    itemView.gameObject.SetActive(false);
                    
                    SetBackGroundGroup(BackGroundType.Default);
                    ClearCustomCraftObject();
                    break;

                case SlotUIState.Appraise:
                    SetContainer(false, true, false, false);
                    preparingContainer.gameObject.SetActive(true);
                    workingContainer.gameObject.SetActive(false);
                    hasNotificationImage.enabled = false;
                    itemView.gameObject.SetActive(false);
                    break;

                case SlotUIState.Working:
                    SetContainer(false, true, false, false);
                    preparingContainer.gameObject.SetActive(false);
                    workingContainer.gameObject.SetActive(true);
                    itemView.gameObject.SetActive(true);
                    
                    if (state is { Result: not null })
                    {
                        UpdateItemInformation(state.Result.itemUsable, uiState);
                        UpdateHourglass(state, currentBlockIndex);
                        UpdateRequiredBlockInformation(
                            state.WorkCompleteBlockIndex,
                            state.WorkStartBlockIndex,
                            currentBlockIndex);
                        UpdateNotification(state, currentBlockIndex);
                    }
                    SetItemUsableImage(state?.Result?.itemUsable);
                    break;

                case SlotUIState.WaitingReceive:
                    SetContainer(false, false, false, true);
                    itemView.gameObject.SetActive(true);
                    if (state != null)
                    {
                        waitingReceiveItemView.SetData(new Item(state.Result.itemUsable));
                        waitingReceiveText.text = string.Format(
                            L10nManager.Localize("UI_SENDING_THROUGH_MAIL"),
                            state.Result.itemUsable.GetLocalizedName(
                                false,
                                true));
                    }
                    SetItemUsableImage(state?.Result?.itemUsable);
                    break;

                case SlotUIState.Locked:
                    SetContainer(true, false, false, false);
                    itemView.Clear();
                    itemView.gameObject.SetActive(false);
                    SetBackGroundGroup(BackGroundType.Default);
                    ClearCustomCraftObject();
                    break;
            }
        }

        private void UpdatePetButton(SlotUIState uiState, CombinationSlotState? state)
        {
            switch (uiState)
            {
                case SlotUIState.Locked:
                case SlotUIState.Empty:
                case SlotUIState.Appraise:
                    petSelectButton.SetData(null);
                    break;
                case SlotUIState.Working:
                case SlotUIState.WaitingReceive:
                    petSelectButton.SetData(state?.PetId ?? null);
                    break;
            }
        }

        private void SetContainer(
            bool isLock,
            bool isWorking,
            bool isEmpty,
            bool isWaitingReceive)
        {
            lockContainer.gameObject.SetActive(isLock);
            baseContainer.gameObject.SetActive(isWorking);
            noneContainer.gameObject.SetActive(isEmpty);
            waitReceiveContainer.gameObject.SetActive(isWaitingReceive);
        }

        private void UpdateRequiredBlockInformation(
            long workCompleteBlockIndex,
            long startBlockIndex,
            long currentBlockIndex)
        {
            progressBar.maxValue = Math.Max(workCompleteBlockIndex - startBlockIndex, 1);
            var diff = Math.Max(workCompleteBlockIndex - currentBlockIndex, 1);
            progressBar.value = diff;
            requiredBlockIndexText.text = $"{diff}";
            requiredTimeText.text = $"({diff.BlockRangeToTimeSpanString(true)})";
        }

        private void UpdateNotification(
            CombinationSlotState state,
            long currentBlockIndex)
        {
            if (UIState != SlotUIState.Working)
            {
                hasNotificationImage.enabled = false;
                return;
            }

            var gameConfigState = Game.instance.States.GameConfigState;
            var diff = state.RequiredBlockIndex - currentBlockIndex;
            int cost;
            if (state.PetId.HasValue &&
                States.Instance.PetStates.TryGetPetState(state.PetId.Value, out var petState))
            {
                cost = PetHelper.CalculateDiscountedHourglass(
                    diff,
                    States.Instance.GameConfigState.HourglassPerBlock,
                    petState,
                    TableSheets.Instance.PetOptionSheet);
            }
            else
            {
                cost = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
            }

            var row = Game.instance.TableSheets.MaterialItemSheet
                .OrderedList?
                .FirstOrDefault(r => r.ItemSubType == ItemSubType.Hourglass);

            if (row == null)
            {
                hasNotificationImage.enabled = false;
                return;
            }

            var isEnough = States.Instance.CurrentAvatarState.inventory
                .HasFungibleItem(row.ItemId, currentBlockIndex, cost);
            hasNotificationImage.enabled = isEnough;
        }

        private void UpdateHourglass(CombinationSlotState state, long blockIndex)
        {
            var diff = state.WorkCompleteBlockIndex - blockIndex;
            int cost;
            if (state.PetId.HasValue &&
                States.Instance.PetStates.TryGetPetState(state.PetId.Value, out var petState))
            {
                cost = PetHelper.CalculateDiscountedHourglass(
                    diff,
                    States.Instance.GameConfigState.HourglassPerBlock,
                    petState,
                    TableSheets.Instance.PetOptionSheet);
            }
            else
            {
                cost = RapidCombination0.CalculateHourglassCount(States.Instance.GameConfigState, diff);
            }

            var inventory = States.Instance.CurrentAvatarState.inventory;
            var count = inventory.GetUsableItemCount(CostType.Hourglass, blockIndex);
            hourglassCountText.text = cost.ToString();
            hourglassCountText.color = count >= cost
                ? Palette.GetColor(ColorType.ButtonEnabled)
                : Palette.GetColor(ColorType.TextDenial);
        }

        private void UpdateItemInformation(ItemUsable? item, SlotUIState slotUIState)
        {
            if (slotUIState == SlotUIState.Working)
            {
                itemView.SetData(new Item(item));
            }
            else
            {
                itemView.SetDataExceptOptionTag(new Item(item));
            }

            itemNameText.text = TextHelper.GetItemNameInCombinationSlot(item);
            preparingText.text = string.Format(
                L10nManager.Localize("UI_COMBINATION_SLOT_IDENTIFYING"),
                item.GetLocalizedName(false, true));
        }

        private static void OnClickSlot(
            SlotUIState uiState,
            CombinationSlotState? state,
            int slotIndex,
            long currentBlockIndex)
        {
            switch (uiState)
            {
                case SlotUIState.Empty:
                    if (BattleRenderer.Instance.IsOnBattle)
                    {
                        NotificationSystem.Push(
                            Nekoyume.Model.Mail.MailType.System,
                            L10nManager.Localize("UI_BLOCK_EXIT"),
                            NotificationCell.NotificationType.Alert);
                        return;
                    }

                    var widgetLayerRoot = MainCanvas.instance
                        .GetLayerRootTransform(WidgetType.Widget);
                    var statusWidget = Widget.Find<Status>();
                    foreach (var widget in MainCanvas.instance.Widgets
                        .Where(widget =>
                            widget.isActiveAndEnabled
                            && widget.transform.parent.Equals(widgetLayerRoot)
                            && !widget.Equals(statusWidget)))
                    {
                        widget.Close(true);
                    }

                    Widget.Find<Craft>()?.gameObject.SetActive(false);
                    Widget.Find<Enhancement>()?.gameObject.SetActive(false);
                    Widget.Find<HeaderMenuStatic>()
                        .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
                    Widget.Find<CombinationMain>().Show();
                    Widget.Find<CombinationSlotsPopup>().Close();
                    break;

                case SlotUIState.Working:
                    Widget.Find<CombinationSlotPopup>().Show(state, slotIndex, currentBlockIndex);
                    break;

                case SlotUIState.Appraise:
                    NotificationSystem.Push(
                        Nekoyume.Model.Mail.MailType.System,
                        L10nManager.Localize("UI_COMBINATION_NOTIFY_IDENTIFYING"),
                        NotificationCell.NotificationType.Information);
                    break;
            }
        }
        
        private void SetBackGroundGroup(BackGroundType backGroundType)
        {
            foreach (var backGround in backGroundGroups)
            {
                if (backGround.BackGroundType == backGroundType)
                {
                    continue;
                }
                backGround.SetActive(false);
            }
            
            foreach (var backGround in backGroundGroups)
            {
                if (backGround.BackGroundType != backGroundType)
                {
                    continue;
                }
                backGround.SetActive(true);
            }
        }
        
        private void SetItemUsableImage(ItemUsable? itemUsable, bool clearCustomObjects = false)
        {                    
            if (itemUsable is Equipment equipment)
            {
                SetBackGroundGroup(equipment.ByCustomCraft ? BackGroundType.CustomCraft : BackGroundType.Default);
                if (!clearCustomObjects)
                {
                    customCraftObject.SetActive(equipment.ByCustomCraft);
                    randomOnlyIcon.SetActive(equipment.HasRandomOnlyIcon);
                }
                else
                {
                    ClearCustomCraftObject();
                }
            }
            else
            {
                SetBackGroundGroup(BackGroundType.Default);
                ClearCustomCraftObject();
            }
        }
        
        private void ClearCustomCraftObject()
        {
            randomOnlyIcon.SetActive(false);
            customCraftObject.SetActive(false);
        }
    }
}
