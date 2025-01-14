using Content.Shared.Damage;
using Content.Shared.Sound;
using Robust.Shared.Prototypes;

namespace Content.Shared.Explosion;

[Prototype("explosion")]
public sealed class ExplosionPrototype : IPrototype
{
    [DataField("id", required: true)]
    public string ID { get; } = default!;

    /// <summary>
    ///     Damage to deal to entities. This is scaled by the explosion intensity.
    /// </summary>
    [DataField("damagePerIntensity", required: true)]
    public readonly DamageSpecifier DamagePerIntensity = default!;

    /// <summary>
    ///     This set of points, together with <see cref="_tileBreakIntensity"/> define a function that maps the
    ///     explosion intensity to a tile break chance via linear interpolation. 
    /// </summary>
    [DataField("tileBreakChance")]
    private readonly float[] _tileBreakChance = { 0f, 1f };

    /// <summary>
    ///     This set of points, together with <see cref="_tileBreakChance"/> define a function that maps the
    ///     explosion intensity to a tile break chance via linear interpolation. 
    /// </summary>
    [DataField("tileBreakIntensity")]
    private readonly float[] _tileBreakIntensity = {0f, 15f };

    /// <summary>
    ///     When a tile is broken by an explosion, the intensity is reduced by this amount and is used to try and
    ///     break the tile a second time. This is repeated until a roll fails or the tile has become space.
    /// </summary>
    /// <remarks>
    ///     If this number is too small, even relatively weak explosions can have a non-zero
    ///     chance to create a space tile.
    /// </remarks>
    [DataField("tileBreakRerollReduction")]
    public readonly float TileBreakRerollReduction = 10f;

    /// <summary>
    ///     Color emitted by a point light at the center of the explosion.
    /// </summary>
    [DataField("lightColor")]
    public readonly Color LightColor = Color.Orange;

    /// <summary>
    ///     Color used to modulate the fire texture.
    /// </summary>
    [DataField("fireColor")]
    public readonly Color? FireColor;

    [DataField("Sound")]
    public readonly SoundSpecifier Sound = new SoundCollectionSpecifier("explosion");

    [DataField("texturePath")]
    public readonly string TexturePath = "/Textures/Effects/fire.rsi";

    // Theres probably a better way to do this. Currently Atmos just hard codes a constant int, so I have no one to
    // steal code from.
    [DataField("fireStates")]
    public readonly int FireStates = 3;

    /// <summary>
    ///     Basic function for linear interpolation of _tileBreakChance and _tileBreakIntensity
    /// </summary>
    public float TileBreakChance(float intensity)
    {
        if (_tileBreakChance.Length == 0 || _tileBreakChance.Length != _tileBreakIntensity.Length)
        {
            Logger.Error($"Malformed tile break chance definitions for explosion prototype: {ID}");
            return 0;
        }

        if (intensity >= _tileBreakIntensity[^1] || _tileBreakIntensity.Length == 1)
            return _tileBreakChance[^1];

        if (intensity <= _tileBreakIntensity[0])
            return _tileBreakChance[0];

        int i = Array.FindIndex(_tileBreakIntensity, k => k >= intensity);

        var slope = (_tileBreakChance[i] - _tileBreakChance[i - 1]) / (_tileBreakIntensity[i] - _tileBreakIntensity[i - 1]);
        return _tileBreakChance[i - 1] + slope * (intensity - _tileBreakIntensity[i - 1]);
    }
}
