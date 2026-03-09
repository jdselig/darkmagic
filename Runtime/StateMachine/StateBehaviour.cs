using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// Inherit from this to get the clean syntax:
    ///   S.Enter<...>();
    ///   S.OnEnter<...>(...);
    ///
    /// (C# doesn't support extension properties, so this base class is the cleanest route.)
    /// </summary>
    public abstract class StateBehaviour : MonoBehaviour
    {
        public StateMachine S => StateMachineRegistry.For(this);
    }
}
