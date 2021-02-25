using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Resources
{
    public class IllegalStateChangeError : InvalidOperationException
    {
        public IllegalStateChangeError(DbRequestState fromState, DbRequestState toState) : base ($"Cannot change state from {fromState} to {toState}")
        {
            FromState = $"{fromState}";
            ToState = $"{toState}";
        }

        public IllegalStateChangeError(DbRequestState fromState, DbRequestState toState, params DbRequestState[] allowedStates)
            : base($"Cannot change state from {fromState} to {toState}. Allowed states are {string.Join(", ", allowedStates.Select(s => s.ToString()))}.")
        {
            FromState = $"{fromState}";
            ToState = $"{toState}";
            AllowedStates = allowedStates.Select(s => s.ToString()).ToArray();
        }

        public IllegalStateChangeError(DbResourceAllocationRequestState fromState, DbResourceAllocationRequestState toState) : base ($"Cannot change state from {fromState} to {toState}")
        {
            FromState = $"{fromState}";
            ToState = $"{toState}";
        }

        public IllegalStateChangeError(DbResourceAllocationRequestState fromState, DbResourceAllocationRequestState toState, params DbResourceAllocationRequestState[] allowedStates)
            : base($"Cannot change state from {fromState} to {toState}. Allowed states are {string.Join(", ", allowedStates.Select(s => s.ToString()))}.")
        {
            FromState = $"{fromState}";
            ToState = $"{toState}";
            AllowedStates = allowedStates.Select(s => s.ToString()).ToArray();
        }

        public string FromState { get; }
        public string ToState { get; }
        public IEnumerable<string>? AllowedStates { get; }

    }
}
