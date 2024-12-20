using BingoSync.Clients;
using BingoSync.Sessions;
using Modding;
using System;

namespace BingoSync.Interfaces
{
    public static class SessionManager
    {
        private static Action<string> Log;
        internal static void Setup(Action<string> log)
        {
            Log = log;
        }

        /// <summary>
        /// Sets the active session to the default BingoSync session.
        /// </summary>
        public static void ResetToDefaultSession()
        {
            Controller.ActiveSession = Controller.DefaultSession;
        }

        /// <summary>
        /// Creates a connection session for the given server. 
        /// This can be done manually, e.g. to use a custom client, 
        /// but the session needs to be manually signed up for automarking, 
        /// using SessionManager.RegisterForAutomarking(Session)
        /// </summary>
        /// <param name="server"></param>
        /// <param name="isMarking"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static ConnectionSession CreateSession(Servers server, bool isMarking)
        {
            IRemoteClient remoteClient = server switch
            {
                Servers.BingoSync => new BingoSyncClient(Log),
                _ => throw new NotImplementedException()
            };
            ConnectionSession session = new(remoteClient, isMarking);
            ModHooks.HeroUpdateHook += delegate { BingoTracker.ProcessBingo(session); };
            return session;
        }

        /// <summary>
        /// Registers a session as capable of automarking. Not to be confused with
        /// Session.isMarking.
        /// </summary>
        /// <param name="session"></param>
        public static void RegisterForAutomarking(ConnectionSession session)
        {
            ModHooks.HeroUpdateHook += delegate { BingoTracker.ProcessBingo(session); };
        }

        /// <summary>
        /// Sets the session as the current active session. By default, the UI 
        /// (board display, connection and generation menu, keybinds, etc.)
        /// interacts with the active session.
        /// </summary>
        /// <param name="session"></param>
        public static void SetActiveSession(ConnectionSession session)
        {
            Controller.ActiveSession = session;
        }
    }
}
