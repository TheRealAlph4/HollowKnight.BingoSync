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

        internal static void SessionChanged(Session previous)
        {
            OnSessionChanged(previous, previous);
        }

        /// <summary>
        /// Called when the active session has changed. 
        /// Passes the previous session as the parameter.
        /// </summary>
        public static event EventHandler<Session> OnSessionChanged;

        /// <summary>
        /// Sets the active session to the default BingoSync session.
        /// </summary>
        public static void ResetToDefaultSession()
        {
            Controller.ActiveSession = Controller.DefaultSession;
        }

        /// <summary>
        /// Gets the default BingoSync session
        /// </summary>
        /// <returns></returns>
        public static Session GetDefaultSession()
        {
            return Controller.DefaultSession;
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
        public static Session CreateSession(Servers server, bool isMarking)
        {
            IRemoteClient remoteClient = server switch
            {
                Servers.BingoSync => new BingoSyncClient(Log),
                _ => throw new NotImplementedException()
            };
            Session session = new(remoteClient, isMarking);
            ModHooks.HeroUpdateHook += delegate { BingoTracker.ProcessBingo(session); };
            return session;
        }

        /// <summary>
        /// Registers a session as capable of automarking. Not to be confused with
        /// Session.isMarking.
        /// </summary>
        /// <param name="session"></param>
        public static void RegisterForAutomarking(Session session)
        {
            ModHooks.HeroUpdateHook += delegate { BingoTracker.ProcessBingo(session); };
        }

        /// <summary>
        /// Returns the currently active session.
        /// </summary>
        public static Session GetActiveSession()
        {
            return Controller.ActiveSession;
        }

        /// <summary>
        /// Sets the session as the current active session. By default, the UI 
        /// (board display, connection and generation menu, keybinds, etc.)
        /// interacts with the active session.
        /// </summary>
        /// <param name="session"></param>
        public static void SetActiveSession(Session session)
        {
            Controller.ActiveSession = session;
        }
    }
}
