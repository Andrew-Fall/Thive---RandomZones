using Godot;
using HarmonyLib;
using System;
using System.Collections.Generic;

[HarmonyPatch(typeof(Patch))]
internal class PatchPatch {
	static readonly Modifier SunlightMod = (0.75f, 1.1f, 0.01f); // Sunlight and gases are precentage based (0 - 1f)
	static readonly Modifier GasMod = (0.9f, 1.1f, 0.01f);
	static readonly Modifier DefaultAmountMod = (0.75f, 1.25f, 5000f); // Amount is generally in the 100k to 500k range
	static readonly Modifier DefaultDensityMod = (0.75f, 1.25f, 1E-5f); // Density is generally in  the 1e-5 - 1e-4 range

	[HarmonyPostfix]
	[HarmonyPatch(MethodType.Constructor, typeof(LocalizedString), typeof(int), typeof(Biome), typeof(BiomeType), typeof(PatchRegion))]
	public static void Postfix(ref PatchSnapshot ___currentSnapshot) {
		var random = new Random();
		var biome = ___currentSnapshot.Biome;

		// Local copy for enumeration while modifying
		var biomeCompounds = new List<Compound>(biome.Compounds.Keys);
		var biomeChunks = new List<string>(biome.Chunks.Keys);

		// Changes the biome's Max/Average/Current compound amount by a random number and rounds it. Order is important.
		foreach(var cat in new CompoundAmountType[] { CompoundAmountType.Maximum, CompoundAmountType.Average, CompoundAmountType.Current }) {
			foreach(var compound in biomeCompounds) {
				if(biome.TryGetCompound(compound, cat, out var BCP)) {
					switch(compound.InternalName) {

						case "sunlight":
							BCP.Ambient *= random.Next(SunlightMod.Low, SunlightMod.High);
							BCP.Ambient = Mathf.Stepify(Mathf.Clamp(BCP.Ambient, 0, 1), SunlightMod.Round);
							break;

						case "oxygen":
						case "carbondioxide":
						case "nitrogen":
							BCP.Ambient *= random.Next(GasMod.Low, GasMod.High);
							BCP.Ambient = Mathf.Stepify(Mathf.Clamp(BCP.Ambient, 0, 1), GasMod.Round);
							break;

						default:
							BCP.Amount *= random.Next(DefaultAmountMod.Low, DefaultAmountMod.High);
							BCP.Amount = Mathf.Stepify(BCP.Amount, DefaultAmountMod.Round);

							BCP.Density *= random.Next(DefaultDensityMod.Low, DefaultDensityMod.High);
							BCP.Density = Mathf.Stepify(BCP.Density, DefaultDensityMod.Round);
							break;
					}

					// Makes sure the current and average is below the max before finally setting before setting it.
					switch(cat) {
						case CompoundAmountType.Maximum:
							biome.MaximumCompounds[compound] = BCP;
							break;
						case CompoundAmountType.Average:
							BCP.Ambient = Mathf.Min(BCP.Ambient, biome.MaximumCompounds[compound].Ambient);
							BCP.Amount = Mathf.Min(BCP.Amount, biome.MaximumCompounds[compound].Amount);
							BCP.Density = Mathf.Min(BCP.Density, biome.MaximumCompounds[compound].Density);
							biome.AverageCompounds[compound] = BCP;
							break;
						case CompoundAmountType.Current:
							BCP.Ambient = Mathf.Min(BCP.Ambient, biome.MaximumCompounds[compound].Ambient);
							BCP.Amount = Mathf.Min(BCP.Amount, biome.MaximumCompounds[compound].Amount);
							BCP.Density = Mathf.Min(BCP.Density, biome.MaximumCompounds[compound].Density);
							biome.CurrentCompoundAmounts[compound] = BCP;
							break;
					}
				}
			}
		}

		// Similar to above, changes chunks randomly
		foreach(string chunkKey in biomeChunks) {
			var chunk = biome.Chunks[chunkKey];

			if(chunk.Compounds == null || chunk.Compounds.Count <= 0) {
				continue;
			}

			// Local copy for enumeration while modifying
			var chunkCompounds = new List<Compound>(chunk.Compounds.Keys);

			foreach(var compound in chunkCompounds) {
				var CC = chunk.Compounds[compound];
				CC.Amount *= random.Next(DefaultAmountMod.Low, DefaultAmountMod.High);
				CC.Amount = Mathf.Stepify(CC.Amount, DefaultAmountMod.Round);
				chunk.Density *= random.Next(DefaultDensityMod.Low, DefaultDensityMod.High);
				chunk.Density = Mathf.Stepify(chunk.Density, DefaultDensityMod.Round);
				chunk.Compounds[compound] = CC;
			}

			biome.Chunks[chunkKey] = chunk;
		}
	}


	internal struct Modifier {
		public float Low;
		public float High;
		public float Round;

		public static implicit operator Modifier((float, float, float) tuple) => new Modifier() { Low = tuple.Item1, High = tuple.Item2, Round = tuple.Item3 };
	}
}
