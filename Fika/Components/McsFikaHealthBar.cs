using System;
using System.Collections.Generic;
using Comfort.Common;
using DG.Tweening;
using EFT;
using EFT.Ballistics;
using EFT.CameraControl;
using EFT.HealthSystem;
using Fika.Core;
using Fika.Core.Bundles;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MiyakoCarryService.Fika.Components
{
    /// <summary>
    /// Displays the Fika Coop Nameplate & Health Bar indicator above companion bots
    /// </summary>
    public sealed class McsFikaHealthBar : MonoBehaviour
    {
        private const float _tweenLength = 0.25f;

        private Player _currentPlayer;
        private Camera _camera;
        private FikaPlayer _mainPlayer;
        private PlayerPlateUI _playerPlate;
        private float _counter;
        private bool _updatePos = true;
        private RectTransform _canvasRect;
        private float _lastFinalAlpha = -1f;
        private RectTransform _plateRectTransform;
        private CanvasGroup _alphaGroup;
        private Transform _neckBone;
        private Dictionary<EBodyPart, GameObject> _bodyParts;
        private int _destroyedLimbs;

        private static GameObject _playerUIPrefab;
        private static readonly int _checkLayers = LayerMask.GetMask("HighPolyCollider", "Terrain", "Player");
        private static readonly int _playerLayer = LayerMask.NameToLayer("Player");

        public static GameObject GetPlayerUIPrefab()
        {
            if (_playerUIPrefab != null)
            {
                return _playerUIPrefab;
            }

            try
            {
                // InternalBundleLoader is internal — access via reflection
                var loaderType = AccessTools.TypeByName("Fika.Core.Bundles.InternalBundleLoader");
                if (loaderType != null)
                {
                    var instance = AccessTools.Property(loaderType, "Instance")?.GetValue(null);
                    if (instance != null)
                    {
                        // EFikaAsset.PlayerUI == 4
                        var efAssetType = AccessTools.TypeByName("Fika.Core.Bundles.InternalBundleLoader+EFikaAsset")
                            ?? AccessTools.TypeByName("InternalBundleLoader+EFikaAsset");
                        if (efAssetType != null)
                        {
                            var playerUiValue = Enum.Parse(efAssetType, "PlayerUI");
                            var getAssetMethod = AccessTools.Method(loaderType, "GetFikaAsset", new[] { efAssetType });
                            if (getAssetMethod != null)
                            {
                                _playerUIPrefab = (GameObject)getAssetMethod.Invoke(instance, new object[] { playerUiValue });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback: prefab not available
            }

            return _playerUIPrefab;
        }

        public static McsFikaHealthBar Create(Player player)
        {
            if (player == null)
            {
                return null;
            }

            var existing = player.GetComponent<McsFikaHealthBar>();
            if (existing != null)
            {
                return existing;
            }

            var healthBar = player.gameObject.AddComponent<McsFikaHealthBar>();
            healthBar._currentPlayer = player;
            healthBar._mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer as FikaPlayer;
            healthBar._neckBone = player.PlayerBones?.Neck;

            healthBar.CreateHealthBar();
            return healthBar;
        }

        private void Update()
        {
            try
            {
                if (_currentPlayer == null || _currentPlayer.HealthController == null || !_currentPlayer.HealthController.IsAlive)
                {
                    if (_playerPlate != null && _playerPlate.gameObject.activeSelf)
                    {
                        _playerPlate.gameObject.SetActive(false);
                    }
                    return;
                }

                if (_mainPlayer == null)
                {
                    _mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer as FikaPlayer;
                    if (_mainPlayer == null)
                    {
                        return;
                    }
                }

                if (_camera == null)
                {
                    _camera = CameraManager.Instance?.Camera;
                    if (_camera == null)
                    {
                        return;
                    }
                }

                if (_neckBone == null && _currentPlayer.PlayerBones != null)
                {
                    _neckBone = _currentPlayer.PlayerBones.Neck;
                }

                if (_playerPlate == null || _alphaGroup == null || _plateRectTransform == null)
                {
                    return;
                }

                var deltaTime = Time.deltaTime;
                UpdateScreenSpacePosition();

                if (FikaPlugin.Instance?.Settings?.UseOcclusion?.Value == true)
                {
                    _counter += deltaTime;
                    if (_counter > 1f)
                    {
                        _counter = 0f;
                        CheckForOcclusion();
                    }
                }
            }
            catch
            {
            }
        }

        private void CheckForOcclusion()
        {
            if (_camera == null || _neckBone == null || _playerPlate?.ScalarObjectScreen == null)
            {
                return;
            }

            var camPos = _camera.transform.position;
            var targetPos = _neckBone.position;

            if (Physics.Raycast(camPos, targetPos - camPos, out var hitinfo, 800f, _checkLayers))
            {
                if (hitinfo.collider.gameObject.layer != _playerLayer)
                {
                    _playerPlate.ScalarObjectScreen.SetActive(false);
                    _updatePos = false;
                }
                else
                {
                    if (!_playerPlate.ScalarObjectScreen.activeSelf)
                    {
                        _playerPlate.ScalarObjectScreen.SetActive(true);
                        _updatePos = true;
                        UpdateScreenSpacePosition();
                    }
                }
            }
        }

        private void UpdateScreenSpacePosition()
        {
            if (!_updatePos || _mainPlayer == null || _camera == null || _neckBone == null || _canvasRect == null || _playerPlate?.ScalarObjectScreen == null)
            {
                return;
            }

            var settings = FikaPlugin.Instance?.Settings;
            if (settings == null)
            {
                return;
            }

            var proceduralAnimation = _mainPlayer.ProceduralWeaponAnimation;

            var opacityMultiplier = 1f;
            if (_mainPlayer.HealthController != null && _mainPlayer.HealthController.IsAlive && proceduralAnimation != null && proceduralAnimation.IsAiming)
            {
                if (proceduralAnimation.CurrentScope != null && proceduralAnimation.CurrentScope.IsOptic && settings.HideNamePlateInOptic.Value)
                {
                    if (_playerPlate.ScalarObjectScreen.activeSelf)
                    {
                        _playerPlate.ScalarObjectScreen.SetActive(false);
                    }
                    return;
                }
                opacityMultiplier = settings.OpacityInADS.Value;
            }

            var cameraPos = _camera.transform.position;
            var offset = _currentPlayer.Position - cameraPos;
            var sqrDist = offset.sqrMagnitude;
            var maxDist = settings.MaxDistanceToShow.Value;

            if (sqrDist > maxDist * maxDist)
            {
                if (_playerPlate.ScalarObjectScreen.activeSelf)
                {
                    _playerPlate.ScalarObjectScreen.SetActive(false);
                }
                return;
            }

            if (!_playerPlate.ScalarObjectScreen.activeSelf)
            {
                _playerPlate.ScalarObjectScreen.SetActive(true);
            }

            var targetPosition = _neckBone.position + Vector3.up;
            if (!WorldToScreen.ProjectToCanvas(targetPosition, _mainPlayer, _canvasRect, out var canvasPos, settings.NamePlateUseOpticZoom.Value, false))
            {
                _alphaGroup.alpha = 0f;
                return;
            }

            _plateRectTransform.anchoredPosition = canvasPos;

            var distance = Mathf.Sqrt(sqrDist);
            var t = Mathf.InverseLerp(2f, maxDist, distance);

            var distFromCenterMult = 1f;
            if (settings.DecreaseOpacityNotLookingAt.Value)
            {
                var sqrDistFromCenter = canvasPos.sqrMagnitude;
                var maxSqrDist = Mathf.Pow(Mathf.Min(_canvasRect.sizeDelta.x, _canvasRect.sizeDelta.y) * 0.5f, 2f);
                distFromCenterMult = Mathf.Clamp01(1f - (sqrDistFromCenter / maxSqrDist));
            }

            var distanceAlpha = Mathf.Lerp(1f, 0.3f, t);
            var finalAlpha = Mathf.Max(settings.MinimumOpacity.Value, distanceAlpha * opacityMultiplier * distFromCenterMult);

            if (!Mathf.Approximately(_lastFinalAlpha, finalAlpha))
            {
                _lastFinalAlpha = finalAlpha;
                _alphaGroup.alpha = finalAlpha;

                var scaleMultiplier = Mathf.Lerp(0.48f, 0.075f, t * Mathf.Sqrt(t)) * settings.NamePlateScale.Value;
                _plateRectTransform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, 1f);
            }
        }

        private void CreateHealthBar()
        {
            try
            {
                var uiPrefab = GetPlayerUIPrefab();
                if (uiPrefab == null)
                {
                    return;
                }

                var uiGameObj = Instantiate(uiPrefab);
                _playerPlate = uiGameObj.GetComponent<PlayerPlateUI>();
                if (_playerPlate == null)
                {
                    return;
                }

                _alphaGroup = _playerPlate.AlphaGroup;
                _plateRectTransform = _playerPlate.ScalarObjectScreen.GetComponent<RectTransform>();

                var nickname = _currentPlayer.Profile?.Info?.MainProfileNickname;
                if (string.IsNullOrEmpty(nickname))
                {
                    nickname = _currentPlayer.Profile?.Info?.Nickname ?? _currentPlayer.Profile?.Nickname ?? "Teammate";
                }
                _playerPlate.SetNameText(nickname);
                _camera = CameraManager.Instance?.Camera;

                _playerPlate.playerNameScreen.color = FikaPlugin.Instance?.Settings?.NamePlateTextColor?.Value ?? Color.white;

                _playerPlate.usecPlateScreen.gameObject.SetActive(false);
                _playerPlate.bearPlateScreen.gameObject.SetActive(false);

                if (FikaPlugin.Instance?.Settings != null)
                {
                    SetPlayerPlateFactionVisibility(FikaPlugin.Instance.Settings.UsePlateFactionSide.Value);
                    SetPlayerPlateHealthVisibility(FikaPlugin.Instance.Settings.HideHealthBar.Value);

                    FikaPlugin.Instance.Settings.UsePlateFactionSide.SettingChanged += UsePlateFactionSide_SettingChanged;
                    FikaPlugin.Instance.Settings.HideHealthBar.SettingChanged += HideHealthBar_SettingChanged;
                    FikaPlugin.Instance.Settings.UseNamePlates.SettingChanged += UseNamePlates_SettingChanged;
                    FikaPlugin.Instance.Settings.UseHealthNumber.SettingChanged += UseHealthNumber_SettingChanged;
                }

                ToggleHealthControllerEvents(FikaPlugin.Instance?.Settings != null && !FikaPlugin.Instance.Settings.HideHealthBar.Value);

                _playerPlate.SetHealthNumberText(100);

                _canvasRect = _playerPlate.ScalarObjectScreen.transform.parent.RectTransform();

                _bodyParts = new Dictionary<EBodyPart, GameObject>()
                {
                    [EBodyPart.Common] = _playerPlate.Skeleton,
                    [EBodyPart.Head] = _playerPlate.Head,
                    [EBodyPart.LeftArm] = _playerPlate.LeftArm,
                    [EBodyPart.RightArm] = _playerPlate.RightArm,
                    [EBodyPart.LeftLeg] = _playerPlate.LeftLeg,
                    [EBodyPart.RightLeg] = _playerPlate.RightLeg,
                    [EBodyPart.Chest] = _playerPlate.Chest,
                    [EBodyPart.Stomach] = _playerPlate.Stomach
                };
                if (_bodyParts.ContainsKey(EBodyPart.Common) && _bodyParts[EBodyPart.Common] != null)
                {
                    _bodyParts[EBodyPart.Common].SetActive(false);
                }

                if (FikaPlugin.Instance?.Settings == null || !FikaPlugin.Instance.Settings.HideHealthBar.Value)
                {
                    UpdateHealth();
                }
                ToggleNamePlate();
            }
            catch (Exception ex)
            {
                FikaGlobals.LogError($"[MiyakoCarryServiceFika] Error creating McsFikaHealthBar: {ex.Message}");
            }
        }

        private void ToggleHealthControllerEvents(bool enabled)
        {
            var healthController = _currentPlayer?.HealthController;
            if (healthController == null)
            {
                return;
            }

            if (enabled)
            {
                healthController.HealthChangedEvent += HealthController_HealthChangedEvent;
                healthController.BodyPartDestroyedEvent += HealthController_BodyPartDestroyedEvent;
                healthController.BodyPartRestoredEvent += HealthController_BodyPartRestoredEvent;
                healthController.DiedEvent += HealthController_DiedEvent;
            }
            else
            {
                healthController.HealthChangedEvent -= HealthController_HealthChangedEvent;
                healthController.BodyPartDestroyedEvent -= HealthController_BodyPartDestroyedEvent;
                healthController.BodyPartRestoredEvent -= HealthController_BodyPartRestoredEvent;
                healthController.DiedEvent -= HealthController_DiedEvent;
            }
        }

        private void ToggleNamePlate()
        {
            var useNamePlates = FikaPlugin.Instance?.Settings?.UseNamePlates?.Value ?? true;
            if (_playerPlate != null)
            {
                _playerPlate.gameObject.SetActive(useNamePlates);
            }
            enabled = useNamePlates;
        }

        private void UsePlateFactionSide_SettingChanged(object sender, EventArgs e)
        {
            if (FikaPlugin.Instance?.Settings != null)
            {
                SetPlayerPlateFactionVisibility(FikaPlugin.Instance.Settings.UsePlateFactionSide.Value);
            }
        }

        private void HideHealthBar_SettingChanged(object sender, EventArgs e)
        {
            if (FikaPlugin.Instance?.Settings != null)
            {
                SetPlayerPlateHealthVisibility(FikaPlugin.Instance.Settings.HideHealthBar.Value);
                ToggleHealthControllerEvents(!FikaPlugin.Instance.Settings.HideHealthBar.Value);
            }
        }

        private void UseNamePlates_SettingChanged(object sender, EventArgs e)
        {
            ToggleNamePlate();
        }

        private void UseHealthNumber_SettingChanged(object sender, EventArgs e)
        {
            if (FikaPlugin.Instance?.Settings != null)
            {
                SetPlayerPlateHealthVisibility(FikaPlugin.Instance.Settings.HideHealthBar.Value);
                UpdateHealth();
            }
        }

        private void HealthController_HealthChangedEvent(EBodyPart bodyPart, float damage, DamageInfo damageInfo)
        {
            UpdateHealth();
        }

        private void HealthController_BodyPartDestroyedEvent(EBodyPart bodyPart, EDamageType damageType)
        {
            HandleLimb(bodyPart, true);
        }

        private void HealthController_BodyPartRestoredEvent(EBodyPart bodyPart, ValueStruct valueStruct)
        {
            HandleLimb(bodyPart, false);
        }

        private void HealthController_DiedEvent(EDamageType damageType)
        {
            if (_playerPlate != null)
            {
                _playerPlate.gameObject.SetActive(false);
            }
            enabled = false;
        }

        private void HandleLimb(EBodyPart bodyPart, bool destroyed)
        {
            _destroyedLimbs += destroyed ? 1 : -1;
            if (_bodyParts != null)
            {
                if (_bodyParts.ContainsKey(EBodyPart.Common) && _bodyParts[EBodyPart.Common] != null)
                {
                    _bodyParts[EBodyPart.Common].SetActive(_destroyedLimbs > 0);
                }
                if (_bodyParts.ContainsKey(bodyPart) && _bodyParts[bodyPart] != null)
                {
                    _bodyParts[bodyPart].SetActive(destroyed);
                }
            }
        }

        private void UpdateHealth()
        {
            if (_currentPlayer?.HealthController == null || _playerPlate == null)
            {
                return;
            }

            var health = _currentPlayer.HealthController.GetBodyPartHealth(EBodyPart.Common, true);
            var currentHealth = health.Current;
            var maxHealth = health.Maximum;
            if (FikaPlugin.Instance?.Settings?.UseHealthNumber?.Value == true)
            {
                if (_playerPlate.healthNumberBackgroundScreen != null && !_playerPlate.healthNumberBackgroundScreen.gameObject.activeSelf)
                {
                    SetPlayerPlateHealthVisibility(false);
                }
                var healthNumberPercentage = (int)Math.Round(currentHealth / maxHealth * 100);
                _playerPlate.SetHealthNumberText(healthNumberPercentage);
            }
            else
            {
                if (_playerPlate.healthBarBackgroundScreen != null && !_playerPlate.healthBarBackgroundScreen.gameObject.activeSelf)
                {
                    SetPlayerPlateHealthVisibility(false);
                }

                var normalizedHealth = Mathf.Clamp01(currentHealth / maxHealth);
                if (_playerPlate.healthBarScreen != null)
                {
                    _playerPlate.healthBarScreen.fillAmount = normalizedHealth;
                    UpdateHealthBarColor(normalizedHealth);
                }
            }
        }

        private void UpdateHealthBarColor(float normalizedHealth)
        {
            if (_playerPlate?.healthBarScreen == null) return;
            var lowColor = FikaPlugin.Instance?.Settings?.LowHealthColor?.Value ?? Color.red;
            var fullColor = FikaPlugin.Instance?.Settings?.FullHealthColor?.Value ?? Color.green;
            var color = Color.Lerp(lowColor, fullColor, normalizedHealth);
            color.a = _playerPlate.healthBarScreen.color.a;
            _playerPlate.healthBarScreen.color = color;
        }

        private void SetPlayerPlateHealthVisibility(bool hidden)
        {
            if (_playerPlate == null) return;
            var useHealthNumber = FikaPlugin.Instance?.Settings?.UseHealthNumber?.Value ?? false;
            if (_playerPlate.healthNumberScreen != null) _playerPlate.healthNumberScreen.gameObject.SetActive(!hidden && useHealthNumber);
            if (_playerPlate.healthNumberBackgroundScreen != null) _playerPlate.healthNumberBackgroundScreen.gameObject.SetActive(!hidden && useHealthNumber);
            if (_playerPlate.healthBarScreen != null) _playerPlate.healthBarScreen.gameObject.SetActive(!hidden && !useHealthNumber);
            if (_playerPlate.healthBarBackgroundScreen != null) _playerPlate.healthBarBackgroundScreen.gameObject.SetActive(!hidden && !useHealthNumber);
        }

        private void SetPlayerPlateFactionVisibility(bool visible)
        {
            if (_playerPlate == null || _currentPlayer?.Profile == null) return;
            if (_currentPlayer.Profile.Side == EPlayerSide.Usec && _playerPlate.usecPlateScreen != null)
            {
                _playerPlate.usecPlateScreen.gameObject.SetActive(visible);
            }
            else if (_currentPlayer.Profile.Side == EPlayerSide.Bear && _playerPlate.bearPlateScreen != null)
            {
                _playerPlate.bearPlateScreen.gameObject.SetActive(visible);
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (FikaPlugin.Instance?.Settings != null)
                {
                    FikaPlugin.Instance.Settings.UsePlateFactionSide.SettingChanged -= UsePlateFactionSide_SettingChanged;
                    FikaPlugin.Instance.Settings.HideHealthBar.SettingChanged -= HideHealthBar_SettingChanged;
                    FikaPlugin.Instance.Settings.UseNamePlates.SettingChanged -= UseNamePlates_SettingChanged;
                    FikaPlugin.Instance.Settings.UseHealthNumber.SettingChanged -= UseHealthNumber_SettingChanged;
                }

                if (_currentPlayer?.HealthController != null)
                {
                    _currentPlayer.HealthController.HealthChangedEvent -= HealthController_HealthChangedEvent;
                    _currentPlayer.HealthController.BodyPartDestroyedEvent -= HealthController_BodyPartDestroyedEvent;
                    _currentPlayer.HealthController.BodyPartRestoredEvent -= HealthController_BodyPartRestoredEvent;
                    _currentPlayer.HealthController.DiedEvent -= HealthController_DiedEvent;
                }

                if (_playerPlate != null)
                {
                    _playerPlate.gameObject.SetActive(false);
                }
            }
            catch
            {
            }
        }
    }
}
