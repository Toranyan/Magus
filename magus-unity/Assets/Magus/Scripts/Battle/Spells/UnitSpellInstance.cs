using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace magus.battle
{
    // A spell equipped to a specific unit.
    // Owns cooldown state and the link to the caster.
    public class UnitSpellInstance
    {
        public SpellInfo Info { get; }
        public IBattleEntity Owner { get; }

        public float CooldownRemaining { get; private set; }
        public bool IsReady => CooldownRemaining <= 0f;

        // Cooldown duration resolved for the most recent cast (post-modifiers). Used
        // by UI to turn CooldownRemaining into a fraction (e.g. for a radial mask).
        public float LastCooldownDuration { get; private set; }

        // When true, the owner will keep attempting to cast this spell automatically
        // (e.g. every frame) instead of only on explicit input. TryCast already no-ops
        // while on cooldown, so it's safe to call repeatedly.
        public bool Autocast { get; set; }

        public UnitSpellInstance(SpellInfo info, IBattleEntity owner)
        {
            Info  = info;
            Owner = owner;
        }

        public void Tick(float deltaTime)
        {
            if (CooldownRemaining > 0f)
                CooldownRemaining -= deltaTime;
        }

        // Returns false if the spell cannot be cast right now.
        public bool TryCast(SpellCastContext context)
        {
            if (!IsReady)
            {
                //Debug.Log($"[UnitSpellInstance] {Info.Name} is on cooldown ({CooldownRemaining:F1}s remaining)");
                return false;
            }

            // TODO: check mana (requires ManaComponent on owner — not yet implemented)

            var executor = SpellExecutorFactory.Create(Info);
            if (executor == null)
                return false;

            // Resolved once here, at the point the caster commits to this cast —
            // same snapshot-at-commit approach as DamageDealer.BeginAttack.
            var ownerUnit = Owner as Unit;
            context.ResolvedCastTime = ownerUnit != null
                ? ownerUnit.Modifiers.Resolve(ModifierType.CastTime, Info.CastTime)
                : Info.CastTime;

            CastAsync(executor, context, ownerUnit).Forget();

            CooldownRemaining = ownerUnit != null
                ? ownerUnit.Modifiers.Resolve(ModifierType.Cooldown, Info.Cooldown)
                : Info.Cooldown;
            LastCooldownDuration = CooldownRemaining;

            return true;
        }

        // Runs the cast-time wind-up (if any) before actually executing the spell.
        // While casting: move speed is halved and Unit.IsCasting is true, both undone
        // if the cast completes normally or is interrupted (Unit.CastInterrupted).
        private async UniTaskVoid CastAsync(ISpellExecutor executor, SpellCastContext context, Unit ownerUnit)
        {
            if (ownerUnit == null || context.ResolvedCastTime <= 0f)
            {
                executor.Execute(context);
                return;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ownerUnit.GetCancellationTokenOnDestroy());
            void OnInterrupted() => cts.Cancel();
            ownerUnit.CastInterrupted += OnInterrupted;

            var castModifierOwner = ModifierOwnerId.New();
            ownerUnit.Modifiers.Add(new Modifier(ModifierType.MoveSpeed, ModifierOperation.Percent, -0.5f, castModifierOwner));
            ownerUnit.StartCast();

            bool completed = false;
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(context.ResolvedCastTime), cancellationToken: cts.Token);
                completed = true;
            }
            catch (OperationCanceledException)
            {
                // Interrupted (e.g. death) — CastInterrupted already set IsCasting false.
            }
            finally
            {
                ownerUnit.CastInterrupted -= OnInterrupted;
                ownerUnit.Modifiers.RemoveAll(castModifierOwner);
            }

            if (!completed)
                return;

            ownerUnit.EndCast();
            executor.Execute(context);
        }
    }
}
