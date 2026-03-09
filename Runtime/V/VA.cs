using System;

namespace DarkMagic
{
    public static partial class V
    {
        /// <summary>
        /// Helper to give lambdas an explicit delegate type without a cast.
        /// Use: this.On&lt;MyEvent&gt;(V.A&lt;int&gt;(x =&gt; ...));
        /// </summary>
        public static Action<T> A<T>(Action<T> a) => a;

        public static Action<T1, T2> A<T1, T2>(Action<T1, T2> a) => a;

        public static Action<T1, T2, T3> A<T1, T2, T3>(Action<T1, T2, T3> a) => a;
    }
}
