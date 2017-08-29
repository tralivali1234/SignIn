﻿using System;
using Starcounter;
using Simplified.Ring2;
using Simplified.Ring3;
using System.Net;

namespace SignIn.Models
{
    /// <summary>
    /// The SystemAdminUser Model is responsible for validating and creating an Admin user that's part of the admin group.
    /// </summary>
    public class SystemAdminUser
    {
        private  const string _adminGroupName = "Admin (System Users)";
        private const string _adminGroupDescription = "System User Administrator Group";
        private const string _adminUsername = "admin";
        private const string _adminEmail = "admin@starcounter.com";
        
        public bool CanCreateAdminUser => GetCanCreateAdminUser(this.ClientRootAddress);
     
        public bool IsAdminCreated => !this.CanCreateAdminUser;
        
        public IPAddress ClientRootAddress { get; private set; }
        public string Username { get; private set; }
        public string Password { get;  set; }

        public string PasswordRepeat { get; set; }



        /// <summary>
        /// Factory method to create a new SystemAdminUser
        /// </summary>
        /// <param name="clientRootAddress"></param>
        /// <returns></returns>
        internal static SystemAdminUser Create(IPAddress clientRootAddress)
        {
            return new SystemAdminUser()
            {
                Username = _adminUsername,
                ClientRootAddress = clientRootAddress
            };
        }

        /// <summary>
        /// Get if an Admin user can be created or not
        /// </summary>
        /// <param name="clientRootAddress"></param>
        /// <returns></returns>
        internal static  bool GetCanCreateAdminUser(IPAddress clientRootAddress)
        {
            return IPAddress.IsLoopback(clientRootAddress) && !HasUsers() && !HasAdminUser();
        }


        internal void CreateAdminUser(out string message, out bool isAlert)
        {
            message = String.Empty;
            isAlert = false;
            if (!IsValidPassword(out message))
            {
                isAlert = true;
                return;
            }
            if (!IsEqualPassword(this.PasswordRepeat, out message))
            {
                isAlert = true;
                return;
            }
            CreateAdminSystemUserIfMissing(this.Password, out message, out isAlert);
        }

      
        /// <summary>
        /// Creates Admin User if missing and adds it to the admin group.  
        /// </summary>
        private void CreateAdminSystemUserIfMissing(string adminPassword, out string message, out bool isAlert)
        {
            message = String.Empty;
            isAlert = false;
            SystemUser user = GetAdminUser();
            SystemUserGroup group = GetAdminUserGroup();
            if (IsInGroup(user, group))
            {
                message = "There is already an Admin user created";
                isAlert = true;
                return;//Do nothing if there's already an admin user
            }

            // There is no system user beloning to the admin group
            Db.Transact(() =>
            {
                if (group == null)
                {
                    group = new SystemUserGroup();
                    group.Name = _adminGroupName;
                    group.Description = _adminGroupDescription;
                }

                if (user == null)
                {
                    Person person = new Person()
                    {
                        FirstName = _adminUsername,
                        LastName = _adminUsername
                    };

                    user = SystemUser.RegisterSystemUser(_adminUsername, _adminEmail, adminPassword);
                    user.WhatIs = person;
                }

                // Add the admin group to the system admin user
                SystemUserGroupMember member = new SystemUserGroupMember();

                member.WhatIs = user;
                member.ToWhat = group;
            });
            message = $"Admin user with username = '{_adminUsername}' was created";
        }
       
        internal bool IsEqualPassword(string repeatedPassword, out string message)
        {
            message = String.Empty;
            if (this.Password != repeatedPassword)
            {
                message = "Passwords do not match";
                return false;
            }
            return true;
        }
       
       
        internal bool IsValidPassword(out string message)
        {
            message = String.Empty;
            bool isValid = true;
            if (string.IsNullOrEmpty(this.Password))
            {
                message = "Password cannot be empty";
                isValid = false;
            }
            return isValid;

        }
        private static bool HasUsers()
        {
            return (Db.SQL($"SELECT o FROM {typeof(SystemUser).FullName} o").First != null);
        }
        private static bool HasAdminUser()
        {
            SystemUser user = GetAdminUser();
            SystemUserGroup group = GetAdminUserGroup();
            return IsInGroup(user, group);
        }
        private static bool IsInGroup(SystemUser user, SystemUserGroup group)
        {
            return (group != null && user != null && SystemUser.IsMemberOfGroup(user, group));
        }
        private static SystemUser GetAdminUser()
        {
            SystemUser user =  Db.SQL<SystemUser>($"SELECT o FROM {typeof(SystemUser).FullName} o WHERE o.{nameof(SystemUser.Username)} = ?", _adminUsername).First;
            return user;
        }
        private static SystemUserGroup GetAdminUserGroup()
        {
            SystemUserGroup group = Db.SQL<SystemUserGroup>($"SELECT o FROM {typeof(SystemUserGroup).FullName} o WHERE o.{nameof(SystemUser.Name)} = ?", _adminGroupName).First;
            return group;
        }
    }
}