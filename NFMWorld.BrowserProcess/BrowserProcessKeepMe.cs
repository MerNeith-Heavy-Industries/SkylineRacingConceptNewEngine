using System.Runtime.CompilerServices;

namespace NFMWorld.BrowserProcess
{
    public class BrowserProcessKeepMe
    {
        /// <summary>
        /// Call to make sure the reference to NFMWorld.BrowserProcess is kept in the build, even if it is not used
        /// anywhere else.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void KeepMe()
        {
            // empty
        }
    }
}