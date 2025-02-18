using System.Collections.Generic;
using System.Linq;
using Libplanet.Crypto;
using Nekoyume.EnumType;
using Nekoyume.Game.Avatar;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class CharacterAppearance : MonoBehaviour
    {
        [SerializeField]
        private AvatarSpineController avatarSpineController;

        [SerializeField]
        private BoxCollider boxCollider;

        [SerializeField]
        private Pet pet;

        private CharacterAnimator _animator;
        private HudContainer _hudContainer;
        private GameObject _cachedCharacterTitle;
        private Costume _fullCostume;

        public AvatarSpineController SpineController => avatarSpineController;
        public BoxCollider BoxCollider => boxCollider;
        public Pet Pet => pet;

        private void OnDisable()
        {
            Destroy(_cachedCharacterTitle);
        }

        public void Set(
            ArenaPlayerDigest digest,
            Address avatarAddress,
            CharacterAnimator animator,
            HudContainer hudContainer,
            TableSheets tableSheets)
        {
            var armor = (Armor)digest.Equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            var weapon = (Weapon)digest.Equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Weapon);
            var aura = (Aura)digest.Equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Aura);

            UpdateAvatar(avatarAddress, animator, hudContainer,
                digest.Costumes, armor, weapon, aura,
                digest.EarIndex, digest.LensIndex, digest.HairIndex, digest.TailIndex, tableSheets);
        }

        public void Set(
            Address avatarAddress,
            CharacterAnimator animator,
            HudContainer hudContainer,
            List<Costume> costumes,
            List<Equipment> equipments,
            int earIndex,
            int lensIndex,
            int hairIndex,
            int tailIndex,
            TableSheets tableSheets,
            bool isFriendCharacter = false,
            System.Action onFinish = null)
        {
            var armor = (Armor)equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            var weapon = (Weapon)equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Weapon);
            var aura = (Aura)equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Aura);

            UpdateAvatar(avatarAddress, animator, hudContainer,
                costumes, armor, weapon, aura,
                earIndex, lensIndex, hairIndex, tailIndex, tableSheets, isFriendCharacter, onFinish);
        }

        public void Set(
            Address avatarAddress,
            CharacterAnimator animator,
            HudContainer hudContainer,
            List<Costume> costumes,
            Armor armor,
            Weapon weapon,
            Aura aura,
            int earIndex,
            int lensIndex,
            int hairIndex,
            int tailIndex,
            TableSheets tableSheets)
        {
            UpdateAvatar(avatarAddress, animator, hudContainer,
                costumes, armor, weapon, aura,
                earIndex, lensIndex, hairIndex, tailIndex, tableSheets);
        }

        public void SetForPrologue(
            CharacterAnimator animator,
            HudContainer hudContainer,
            int armorId,
            int weaponId,
            int earIndex,
            int lensIndex,
            int hairIndex,
            int tailIndex,
            CostumeItemSheet costumeItemSheet)
        {
            _animator = animator;
            _hudContainer = hudContainer;

            SpineController.UnequipFullCostume(false);
            SpineController.UpdateBody(armorId, 0, false);
            SpineController.UpdateWeapon(weaponId, null);
            UpdateEar(earIndex, false, costumeItemSheet);
            UpdateFace(lensIndex, false, costumeItemSheet);
            UpdateHair(hairIndex, false, costumeItemSheet);
            UpdateTail(tailIndex, false, costumeItemSheet);
            UpdateAcFace(0, false);
            UpdateAcEye(0, false);
            UpdateAcHead(0, false);
            UpdateTarget();
            UpdateHitPoint();
        }

        public void SetForCostumes(
            CharacterAnimator animator,
            HudContainer hudContainer,
            List<Costume> costumes)
        {
            _animator = animator;
            _hudContainer = hudContainer;
            Destroy(_cachedCharacterTitle);

            var fullCostume =
                costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.FullCostume);
            if (fullCostume is not null)
            {
                UpdateFullCostume(fullCostume);
            }

            var title = costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.Title);
            if (title is not null)
            {
                UpdateTitle(title);
            }
        }

        private async void UpdateAvatar(
            Address avatarAddress,
            CharacterAnimator animator,
            HudContainer hudContainer,
            List<Costume> costumes,
            Armor armor,
            Weapon weapon,
            Aura aura,
            int earIndex,
            int lensIndex,
            int hairIndex,
            int tailIndex,
            TableSheets tableSheets,
            bool isFriendCharacter = false,
            System.Action onFinish = null)
        {
            _animator = animator;
            _hudContainer = hudContainer;
            Destroy(_cachedCharacterTitle);
            var costumeItemSheet = tableSheets.CostumeItemSheet;

            var isDcc = Dcc.instance.IsVisible(avatarAddress, out var id, out var isVisible);
            if (isDcc && !isFriendCharacter &&
                States.Instance.CurrentAvatarState is not null &&
                avatarAddress == States.Instance.CurrentAvatarState.address)
            {
                isDcc = isVisible;
            }

            if (isDcc && await Dcc.instance.GetParts(id) is { } dccParts)
            {
                // ignore full costume
                SpineController.UnequipFullCostume(false);
                UpdateArmor(armor, dccParts[DccPartsType.skin], true);
                UpdateWeapon(weapon);
                UpdateAura(aura);
                UpdateEar(dccParts[DccPartsType.ear_tail], true, costumeItemSheet);
                UpdateFace(dccParts[DccPartsType.face], true, costumeItemSheet);
                UpdateHair(dccParts[DccPartsType.hair], true, costumeItemSheet);
                UpdateTail(dccParts[DccPartsType.ear_tail], true, costumeItemSheet);
                UpdateAcFace(dccParts[DccPartsType.ac_face], true);
                UpdateAcEye(dccParts[DccPartsType.ac_eye], true);
                UpdateAcHead(dccParts[DccPartsType.ac_head], true);

                if (!isFriendCharacter && pet)
                {
                    pet.SetPosition(SpineController.GetBodySkeletonAnimation(), false);
                }
            }
            else
            {
                var fullCostume =
                    costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.FullCostume);
                var useEquipment = true;
                if (fullCostume is not null)
                {
                    useEquipment = !UpdateFullCostume(fullCostume);
                }

                if (useEquipment)
                {
                    SpineController.UnequipFullCostume(false);
                    UpdateEar(earIndex, false, costumeItemSheet);
                    UpdateFace(lensIndex, false, costumeItemSheet);
                    UpdateHair(hairIndex, false, costumeItemSheet);
                    UpdateTail(tailIndex, false, costumeItemSheet);
                    UpdateAcFace(0, false);
                    UpdateAcEye(0, false);
                    UpdateAcHead(0, false);
                    UpdateArmor(armor, 0, false);
                    UpdateWeapon(weapon);
                }

                UpdateAura(aura);
                if (!isFriendCharacter && pet)
                {
                    pet.SetPosition(SpineController.GetBodySkeletonAnimation(), fullCostume is not null);
                }
            }

            var title = costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.Title);
            if (title is not null)
            {
                UpdateTitle(title);
            }

            onFinish?.Invoke();
        }

        /// <summary>
        /// Updates the full costume for the character by setting the corresponding costume ID.
        /// If the costume exists, updates the target and hit points.
        /// Note: If <c>UpdateHitPoint</c> is called conditionally on <c>costumeExist</c>,
        /// fallback behavior (when the costume does not exist) may not execute as intended.
        /// </summary>
        /// <param name="fullCostume">The costume object containing the ID to be applied.</param>
        /// <returns>True if the costume exists and was successfully updated; otherwise, false.</returns>
        private bool UpdateFullCostume(Costume fullCostume)
        {
            // Attempt to update the full costume using the SpineController and check if it exists.
            var costumeExist = SpineController.UpdateFullCostume(fullCostume.Id, false);

            // If the costume exists, update the character's target and hit points.
            if (costumeExist)
            {
                UpdateTarget(); // Update the target object for the character's animations.

                // Warning: If the costume does not exist, fallback logic (e.g., resetting hit points)
                // will not execute as intended because UpdateHitPoint is conditionally called here.
                UpdateHitPoint(); // Update the character's collider and hit point dimensions.
            }

            // Return whether the costume exists.
            return costumeExist;
        }

        private void UpdateAcFace(int index, bool isDcc)
        {
            SpineController.UpdateAcFace(index, isDcc);
        }

        private void UpdateAcEye(int index, bool isDcc)
        {
            SpineController.UpdateAcEye(index, isDcc);
        }

        private void UpdateAcHead(int index, bool isDcc)
        {
            SpineController.UpdateAcHead(index, isDcc);
        }

        private void UpdateArmor(Armor armor, int skinTone, bool isDcc)
        {
            var armorId = armor?.Id ?? GameConfig.DefaultAvatarArmorId;
            SpineController.UpdateBody(armorId, skinTone, isDcc);
            UpdateTarget();
            UpdateHitPoint();
        }

        private void UpdateWeapon(Weapon weapon)
        {
            var weaponId = weapon?.IconId ?? GameConfig.DefaultAvatarWeaponId;
            var level = weapon?.level ?? 0;
            var vfx = ResourcesHelper.GetAuraWeaponPrefab(weaponId, level);
            SpineController.UpdateWeapon(weaponId, vfx);
        }

        private void UpdateAura(Aura aura)
        {
            if (aura == null)
            {
                SpineController.UpdateAura(null);
                return;
            }

            var vfx = ResourcesHelper.GetAuraPrefab(aura.Id, aura.level);
            SpineController.UpdateAura(vfx);
        }

        private void UpdateEar(int index, bool isDcc, CostumeItemSheet sheet)
        {
            var row = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.EarCostume);
            var id = isDcc ? index : row.Id + index;
            SpineController.UpdateEar(id, isDcc);
        }

        private void UpdateFace(int index, bool isDcc, CostumeItemSheet sheet)
        {
            var row = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.EyeCostume);
            var id = isDcc ? index : row.Id + index;
            SpineController.UpdateFace(id, isDcc);
        }

        private void UpdateHair(int index, bool isDcc, CostumeItemSheet sheet)
        {
            var row = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.HairCostume);
            var id = isDcc ? index : row.Id + index;
            SpineController.UpdateHair(id, isDcc);
        }

        private void UpdateTail(int index, bool isDcc, CostumeItemSheet sheet)
        {
            var row = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.TailCostume);
            var id = isDcc ? index : row.Id + index;
            SpineController.UpdateTail(id, isDcc);
        }

        private void UpdateTitle(ItemBase costume)
        {
            Destroy(_cachedCharacterTitle);
            if (costume == null)
            {
                return;
            }

            var clone = ResourcesHelper.GetCharacterTitle(costume.Grade,
                costume.GetLocalizedNonColoredName(false));
            _cachedCharacterTitle = Instantiate(clone, _hudContainer.transform);
            _cachedCharacterTitle.name = costume.Id.ToString();
            _cachedCharacterTitle.transform.SetAsFirstSibling();
        }

        private void UpdateTarget()
        {
            var target = SpineController.gameObject;
            var animator = SpineController.GetComponent<Animator>();
            var sk = SpineController.GetBodySkeletonAnimation();
            var mr = sk.GetComponent<MeshRenderer>();
            _animator.InitTarget(target, mr, sk, animator);
        }

        private void UpdateHitPoint()
        {
            var source = SpineController.Collider;
            var scale = _animator.Target.transform.localScale;
            var center = source.center;
            var size = source.size;
            boxCollider.center =
                new Vector3(center.x * scale.x, center.y * scale.y, center.z * scale.z);
            boxCollider.size =
                new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);
        }

        public void SetSpineColor(Color color, int propertyID)
        {
            SpineController.SetSpineColor(color, propertyID);
        }
    }
}
