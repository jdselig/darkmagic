using System;

namespace DarkMagic
{
    /// <summary>
    /// V integration for state machines (events as types):
    /// - V.StateChanged: fires for any state change (any owner, any group)
    /// - V.StateChanged<TGroup>: fires for state changes where the state type is nested under TGroup (e.g. S.Player.*)
    /// </summary>
    public static partial class V
    {
        public readonly struct StateChange
        {
            public readonly UnityEngine.Object Owner;
            public readonly Type From;
            public readonly Type To;
            public readonly Type Group;

            public StateChange(UnityEngine.Object owner, Type from, Type to, Type group)
            {
                Owner = owner;
                From = from;
                To = to;
                Group = group;
            }

            public override string ToString()
                => $"{Owner?.name ?? "(owner)"}: {(From?.Name ?? "(none)")} -> {To?.Name ?? "(none)"}";
        }

        public sealed class StateChanged : Event<StateChange> { }
        public sealed class StateChanged<TGroup> : Event<StateChange> { }
    }
}
