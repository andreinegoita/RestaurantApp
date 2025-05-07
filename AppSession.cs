using RestaurantApp.Models;
using System;

namespace RestaurantApp
{
    /// <summary>
    /// Static class to maintain application session state
    /// </summary>
    public static class AppSession
    {
        private static User _currentUser;

        /// <summary>
        /// The currently logged-in user
        /// </summary>
        public static User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                UserChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// The order currently selected for details view
        /// </summary>
        public static Order SelectedOrder { get; set; }

        /// <summary>
        /// Event that is triggered when the current user changes
        /// </summary>
        public static event EventHandler UserChanged;

        /// <summary>
        /// Check if a user is currently logged in
        /// </summary>
        public static bool IsLoggedIn => CurrentUser != null;

        /// <summary>
        /// Check if the current user is an admin
        /// </summary>
        public static bool IsAdmin => CurrentUser?.RoleId == 1;

        /// <summary>
        /// Logs out the current user
        /// </summary>
        public static void Logout()
        {
            CurrentUser = null;
            SelectedOrder = null;
        }
    }
}
