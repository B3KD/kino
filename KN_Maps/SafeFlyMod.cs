using KN_Core;
using SyncMultiplayer;
using UnityEngine;

namespace KN_Maps {
  public class SafeFlyMod {
    private readonly Core core_;
    private readonly bool cheatsEnabled_;

    private float speed_;
    private float speedMultiplier_;

    private bool noclipEnabled_;
    private float floatingPos_;

    private Camera camera_;

    private bool prevScene_;
    private NetGameCollisionManager collisionManager_;

    public SafeFlyMod(Core core) {
      core_ = core;
      cheatsEnabled_ = core_.IsCheatsEnabled;
    }

    public void OnStart() {
      if (!cheatsEnabled_) {
        speed_ = core_.KnConfig.Get<float>("speed");
        speedMultiplier_ = core_.KnConfig.Get<float>("speed_multiplier");
      }
    }

    public void OnStop() {
      if (!cheatsEnabled_) {
        core_.KnConfig.Set("speed", speed_);
        core_.KnConfig.Set("speed_multiplier", speedMultiplier_);
      }
    }

    public void OnCarLoaded() {
      if (cheatsEnabled_) {
        return;
      }
      SetCollisions();
    }

    public void OnGui(Gui gui, ref float x, ref float y) {
      if (cheatsEnabled_) {
        return;
      }

      const float width = Gui.Width;

      bool guiEnabled = GUI.enabled;

      GUI.enabled = !cheatsEnabled_ && !ModelValidator.isValid;
      gui.SliderH(ref x, ref y, width, ref speed_, 1.0f, 100.0f, $"SPEED: {speed_:F1}");
      gui.SliderH(ref x, ref y, width, ref speedMultiplier_, 1.0f, 10.0f, $"SPEED MULTIPLIER {speedMultiplier_:F1}");

      GUI.enabled = guiEnabled;
    }

    public void Update() {
      if (cheatsEnabled_ || ModelValidator.isValid) {
      if (cheatsEnabled_) {
        return;
      }

      if (TFCar.IsNull(core_.PlayerCar)) {
        return;
      }

      bool sceneChanged = prevScene_ && !core_.IsInGarage || !prevScene_ && core_.IsInGarage;
      prevScene_ = core_.IsInGarage;
      if (sceneChanged || collisionManager_ == null) {
        collisionManager_ = NetworkController.InstanceGame.systems.Get<NetGameCollisionManager>();
      }

      //if follow someone
      if (core_.PlayerCar.IsNetworkCar) {
        return;
      }

      if (camera_ == null || !camera_.CompareTag(KnConfig.CxMainCameraTag)) {
        camera_ = Camera.main;
      }

      if (Controls.KeyDown("disable_all")) {
        noclipEnabled_ = false;
        SetCollisions();
      }

      if (Controls.KeyDown("mode")) {
        noclipEnabled_ = !noclipEnabled_;
        SetCollisions();
      }

      var currentPosition = core_.PlayerCar.CxTransform.position;
      if (Controls.KeyDown("teleport")) {
        if (camera_ != null) {
          currentPosition = camera_.transform.position;
          core_.PlayerCar.CxTransform.position = currentPosition;
        }
      }

      if (!noclipEnabled_) {
        floatingPos_ = core_.PlayerCar.CxTransform.position.y;
        return;
      }

      currentPosition.y = floatingPos_;

      if (Controls.Key("cam_align")) {
        if (camera_ != null) {
          var rotation = camera_.transform.rotation;
          rotation.x = 0.0f;
          rotation.z = 0.0f;
          core_.PlayerCar.CxTransform.rotation = rotation;
        }
      }

      currentPosition += Movement.Move(core_.PlayerCar.CxTransform, speed_, speedMultiplier_);

      //set transform to carx car
      core_.PlayerCar.CxTransform.position = currentPosition;
      core_.PlayerCar.CxTransform.rotation = Quaternion.LookRotation(core_.PlayerCar.CxTransform.forward);

      floatingPos_ = currentPosition.y;

      core_.PlayerCar.CarX.getRigidbody.velocity = Vector3.zero;
      core_.PlayerCar.CarX.getRigidbody.angularVelocity = Vector3.zero;
      core_.PlayerCar.CxTransform.Rotate(-(2.0f * Input.GetAxis("Mouse Y")), 2.0f * Input.GetAxis("Mouse X"), 0.0f);
    }

    private void SetCollisions() {
      if (cheatsEnabled_ || ModelValidator.isValid) {
      if (cheatsEnabled_) {
        return;
      }

      foreach (var car in core_.Cars) {
        var nwPlayer = car.Base.networkPlayer;
        if (nwPlayer != null) {
          if (noclipEnabled_) {
            core_.Udp.SendChangeRoomId(nwPlayer, false);
            collisionManager_?.MovePlayerToColliderGroup("none", car.Base.networkPlayer);
          }
          else if (!(car.IsConsole && core_.Settings.ConsolesDisabled)) {
            core_.Udp.SendChangeRoomId(nwPlayer, true);
            collisionManager_?.MovePlayerToColliderGroup("", car.Base.networkPlayer);
          }
        }
      }
    }
  }
}