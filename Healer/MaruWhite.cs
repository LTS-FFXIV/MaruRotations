namespace MaruRotations.Healer.WHM
{
    [SourceCode(Path = "main/MaruRotations/Healer/MaruWhite.cs")]
    internal sealed class MaruWhite : WHM_Base
    {

        #region General rotation info

        public override string GameVersion => VERSION;
        public override string RotationName => $"MaruWhite {ClassJob.Abbreviation} [{Type}]";
        public override CombatType Type => CombatType.PvE;

        #endregion General rotation info

        internal static IBaseAction RegenDefense { get; } = new BaseAction(ActionID.Regen, ActionOption.Hot)
        {
            ActionCheck = (b, m) => b.IsJobCategory(JobRole.Tank),
            TargetStatus = Regen.TargetStatus,
        };

        #region Countdown logic

        protected override IAction CountDownAction(float remainTime)
        {
            if (remainTime < Stone.CastTime + CountDownAhead && Stone.CanUse(out var act))
                return act;

            if (Configs.GetBool("UsePreRegen") && remainTime <= 5 && remainTime > 3 &&
                ((RegenDefense.CanUse(out act, CanUseOption.IgnoreClippingCheck)) ||
                DivineBenison.CanUse(out act, CanUseOption.IgnoreClippingCheck)))
                return act;

            return base.CountDownAction(remainTime);
        }

        #endregion Countdown logic

        #region Configuration

        protected override IRotationConfigSet CreateConfiguration()
        {
            return base.CreateConfiguration()
                .SetBool(CombatType.PvE, "UseLilyWhenFull", true, "Use Lily at max stacks.")
                .SetBool(CombatType.PvE, "UsePreRegen", false, "Regen on Tank at 5 seconds remaining on Countdown.")
                .SetInt(CombatType.PvE, "AsylumThreshold", 2, "At how many hostiles should Asylum be used", 1, 20)
                .SetInt(CombatType.PvE, "HolyHostileThreshold", 3, "At how many hostiles should Holy be used", 1, 20);
        }

        #endregion Configuration

        #region oGCDs
        [RotationDesc(ActionID.Assize, ActionID.Aero, ActionID.PresenceOfMind)]
        protected override bool AttackAbility(out IAction act)
        {
            return (HasHostilesInMaxRange && (Assize.CanUse(out act, CanUseOption.MustUse))) ||
                Aero.CanUse(out act) ||
                PresenceOfMind.CanUse(out act) ||
                base.AttackAbility(out act);
        }

        #region Defense abilities

        [RotationDesc(ActionID.DivineBenison, ActionID.Aquaveil)]
        protected override bool DefenseSingleAbility(out IAction act)
        {
            return DivineBenison.CanUse(out act) ||
                Aquaveil.CanUse(out act) ||
                base.DefenseSingleAbility(out act);
        }

        [RotationDesc(ActionID.Temperance, ActionID.LiturgyOfTheBell)]
        protected override bool DefenseAreaAbility(out IAction act)
        {
            return Temperance.CanUse(out act) ||
                LiturgyOfTheBell.CanUse(out act) ||
                base.DefenseAreaAbility(out act);
        }

        #endregion Defense abilities

        #region Emergency abilities
        [RotationDesc(ActionID.ThinAir, ActionID.AfflatusRapture, ActionID.Medica, ActionID.Medica2, ActionID.Cure3, ActionID.PlenaryIndulgence)]
        protected override bool EmergencyAbility(IAction nextGCD, out IAction act)
        {
            if ((nextGCD is IBaseAction action && action.MPNeed >= 999 && ThinAir.CanUse(out act)) ||
                (nextGCD.IsTheSameTo(true, AfflatusRapture, Medica, Medica2, Cure3) &&
                 PlenaryIndulgence.CanUse(out act)))
            {
                return true;
            }

            return base.EmergencyAbility(nextGCD, out act);
        }


        #endregion Emergency abilities

        #region Heal single Target oGCD logic
        [RotationDesc(ActionID.Benediction, ActionID.DivineBenison, ActionID.Tetragrammaton)]
        protected override bool HealSingleAbility(out IAction act)
        {

            if (Benediction.CanUse(out act) && Benediction.Target.GetHealthRatio() < 0.5)
                return true;
            if (!IsMoving &&
                NumberOfAllHostilesInRange >= Configs.GetInt("AsylumThreshold") &&
                HostileTarget.IsBossFromIcon() &&
                Asylum.CanUse(out act)) return true;

            return DivineBenison.CanUse(out act) ||
                   Tetragrammaton.CanUse(out act) ||
                   base.HealSingleAbility(out act);
        }
        #endregion 

        #region Heal area oGCD logic
        [RotationDesc(ActionID.Asylum)]
        protected override bool HealAreaAbility(out IAction act)
        {
            if (!IsMoving &&
                NumberOfAllHostilesInRange >= Configs.GetInt("AsylumThreshold") &&
                HostileTarget.IsBossFromIcon() &&
                Asylum.CanUse(out act)) return true;

            return base.HealAreaAbility(out act);
        }

        #endregion Heal area oGCD logic

        #endregion oGCDs

        #region GCDs
        [RotationDesc(ActionID.Aero, ActionID.Stone, ActionID.Holy, ActionID.AfflatusMisery, ActionID.AfflatusRapture)]
        protected override bool GeneralGCD(out IAction act)
        {
            if (InCombat &&
                RegenDefense.CanUse(out act) &&
                RegenDefense.Target.GetHealthRatio() < 0.7f)
            {
                return true;
            }

            if (AfflatusMisery.CanUse(out act, CanUseOption.MustUse)) return true;

            bool liliesNearlyFull = Lily == 2 && LilyAfter(17);
            bool liliesFullNoBlood = Lily == 3 && BloodLily < 3;
            if (Configs.GetBool("UseLilyWhenFull") && (liliesNearlyFull || liliesFullNoBlood) && AfflatusMisery.EnoughLevel)
            {
                if (PartyMembersAverHP < 0.7)
                {
                    if (AfflatusRapture.CanUse(out act)) return true;
                }
                if (AfflatusSolace.CanUse(out act)) return true;
            }
            if (PresenceOfMind.CanUse(out act)) return true;
            if (NumberOfAllHostilesInRange >= Configs.GetInt("HolyHostileThreshold") && Holy.CanUse(out act)) return true;

            if (Aero.CanUse(out act)) return true;
            if (Stone.CanUse(out act)) return true;
            if (Aero.CanUse(out act, CanUseOption.MustUse)) return true;

            return base.GeneralGCD(out act);
        }

        #region Heal area GCD logic
        [RotationDesc(ActionID.Cure3, ActionID.Medica, ActionID.Medica2)]
        protected override bool HealAreaGCD(out IAction act)
        {
            int membersNeedingHeal = PartyMembers.Count(m => m.GetHealthRatio() < 0.7f);
            int membersWithMedica2 = PartyMembers.Count(n => n.HasStatus(true, StatusID.Medica2));
            bool medica2Available = Medica2.CanUse(out act);

            if (membersNeedingHeal >= 2 && medica2Available && membersWithMedica2 < PartyMembers.Count())
            {
                return true;
            }

            if (Cure3.CanUse(out act))
            {
                return true;
            }
            else if (Medica.CanUse(out act))
            {
                return true;
            }

            return base.HealAreaGCD(out act);
        }

        #endregion Heal area GCD logic
        [RotationDesc(ActionID.Cure, ActionID.Cure2)]
        #region Heal single Target GCD logic
        protected override bool HealSingleGCD(out IAction act)
        {
            return (Regen.CanUse(out act) && Regen.Target.GetHealthRatio() < 0.7f) ||
                   Cure2.CanUse(out act) ||
                   Cure.CanUse(out act) ||
                   base.HealSingleGCD(out act);

        }

        #endregion Heal single Target GCD logic

        #endregion GCDs
    }
}