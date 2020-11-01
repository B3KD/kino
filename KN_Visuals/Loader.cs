using BepInEx;
using KN_Core;

namespace KN_Visuals {
  [BepInPlugin("trbflxr.kn_visuals", "KN_Visuals", StringVersion)]
  public class Loader : BaseUnityPlugin {
    private const int Version = 202;
    private const int Patch = 0;
    private const int ClientVersion = 273;
    private const string StringVersion = "2.0.2";

    public Loader() {
      Core.CoreInstance.AddMod(new Visuals(Core.CoreInstance, Version, Patch, ClientVersion));
    }
  }
}