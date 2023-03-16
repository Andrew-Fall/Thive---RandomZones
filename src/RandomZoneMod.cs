using Godot;
using HarmonyLib;

public class RandomZoneMod : IMod {
	private const string HarmonyID = "johnanderson.thrive.mod.randomzones";

	private readonly Harmony harmony = new Harmony(HarmonyID);

	public bool Initialize(IModInterface modInterface, ModInfo currentModInfo) {
		GD.Print(GetType().Name + " is patching.");
		harmony.PatchAll();
		GD.Print(GetType().Name + " patched.");
		return true;
	}

	public bool Unload() {
		GD.Print(GetType().Name + " is unpatching.");
		harmony.UnpatchAll(HarmonyID);
		GD.Print(GetType().Name + " unpatched.");
		return true;
	}

	public void CanAttachNodes(Node currentScene) {
	}
}
