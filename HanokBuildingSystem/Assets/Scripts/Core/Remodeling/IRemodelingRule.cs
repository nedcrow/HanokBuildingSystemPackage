using UnityEngine;

namespace HanokBuildingSystem
{
    public enum RemodelingRuleStatus
    {
        Skipped,    // 룰이 해당되지 않아 진행하지 않음
        Succeeded,  // 룰이 진행되고 성공
        Failed      // 룰이 진행되고 실패
    }

    public readonly struct RemodelingRuleResult
    {
        public RemodelingRuleStatus Status { get; }
        public string Reason { get; }
        public bool Enforce { get; }

        private RemodelingRuleResult(RemodelingRuleStatus status, string reason, bool enforce)
        {
            Status = status;
            Reason = reason;
            Enforce = enforce;
        }

        public static RemodelingRuleResult Success(bool enforce = false, string reason = null)
            => new RemodelingRuleResult(RemodelingRuleStatus.Succeeded, reason, enforce);

        public static RemodelingRuleResult Fail(string reason, bool enforce = false)
            => new RemodelingRuleResult(RemodelingRuleStatus.Failed, reason, enforce);

        public static RemodelingRuleResult Skip(string reason = null)
            => new RemodelingRuleResult(RemodelingRuleStatus.Skipped, reason, false);
    }

    public interface IRemodelingRule
    {
        RemodelingRuleResult ControlBuilding(Building building, House house, Vector3 pos);
    }
}
