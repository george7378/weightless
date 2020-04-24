using System;
using WeightlessCore;

namespace WeightlessWindows
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class ProgramMain
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new WeightlessGame())
            {
                game.Run();
            }
        }
    }
}
