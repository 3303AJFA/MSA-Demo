using UnityEngine;

[CreateAssetMenu(fileName = "Pedal_Clean", menuName = "MSA/Pedal/Damage/Clean")]
public class CleanDamagePedal : PedalEffect
{
    public override void Execute(CombatContext ctx)
    {
        var lvl = GetLevel();
        if (lvl == null) return;

        int dmg = ctx.ScaleDamage(lvl.value);
        ctx.battle.DamageEnemy(dmg);
    }
}
