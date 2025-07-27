﻿using BingoSync.Clients;
using BingoSync.Sessions;
using Modding;
using System;
using System.Collections.Generic;

namespace BingoSync.Interfaces
{
    public static class SessionManager
    {
        private static Action<string> Log;
        private static readonly HashSet<string> NeedsSessionNameKeys = [];
        internal static bool ShowSessionName
        {
            get
            {
                return NeedsSessionNameKeys.Count > 0;
            }
        }

        internal static void Setup(Action<string> log)
        {
            Log = log;
        }

        internal static void SessionChanged(Session previous)
        {
            OnSessionChanged?.Invoke(previous, previous);
        }

        /// <summary>
        /// Called when the active session has changed. 
        /// Passes the previous session as the parameter.
        /// </summary>
        public static event EventHandler<Session> OnSessionChanged;

        /// <summary>
        /// When an external mod wants the names of sessions to be displayed (e.g. 
        /// because several sessions are active at once), it can call this method
        /// to add a unique key. If there are any active keys, the session names
        /// are displayed in game.
        /// </summary>
        /// <param name="key">A unique ID for a reason the session name should be displayed</param>
        public static void ShowBoardNameWithKey(string key)
        {
            NeedsSessionNameKeys.Add(key);
            Controller.BoardUpdate();
        }

        /// <summary>
        /// Remove a key from the list; see ShowBoardNameWithKey.
        /// </summary>
        /// <param name="key"></param>
        public static void HideBoardNameWithKey(string key)
        {
            NeedsSessionNameKeys.Remove(key);
            Controller.BoardUpdate();
        }

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
        /// <param name="name"></param>
        /// <param name="server"></param>
        /// <param name="isMarking"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Session CreateSession(string name, Servers server, bool isMarking)
        {
            IRemoteClient remoteClient = server switch
            {
                Servers.BingoSync => new BingoSyncClient(Log),
                _ => throw new NotImplementedException()
            };
            Session session = new(name, remoteClient, isMarking);
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
