// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for license information. 

using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Microsoft.Vault.Library
{
    public class FileTokenCache
    {
        public string FileName;
        private static readonly object FileLock = new object();

        public FileTokenCache() : this("microsoft.com") { }

        /// <summary>
        /// Initializes the cache against a local file.
        /// If the file is already present, it loads its content in the MSAL cache
        /// </summary>
        /// <param name="domainHint">For example: microsoft.com or gme.gbl</param>
        public FileTokenCache(string domainHint)
        {
            FileName = Environment.ExpandEnvironmentVariables(string.Format(Consts.VaultTokenCacheFileName, domainHint));
            Directory.CreateDirectory(Path.GetDirectoryName(FileName));
        }

        /// <summary>
        /// Gets all login names for which there is a token cached locally.
        /// </summary>
        public static string[] GetAllFileTokenCacheLoginNames()
        {
            string[] paths = Directory.GetFiles(Environment.ExpandEnvironmentVariables(Consts.VaultTokenCacheDirectory));
            for (int i = 0; i < paths.Length; i++)
            {
                //Gets filename from path.
                paths[i] = paths[i].Split('\\').Last();

                //Gets login name from filename.
                paths[i] = paths[i].Split('_')[1];
            }
            return paths;
        }

        /// <summary>
        /// Empties all persistent stores.
        /// </summary>
        public static void ClearAllFileTokenCaches()
        {
            string[] tokenNames = GetAllFileTokenCacheLoginNames();
            foreach(string token in tokenNames)
            {
                new FileTokenCache(token).Clear();
            }
        }

        /// <summary>
        /// Renames the cache.
        /// </summary>
        /// <param name="newName"></param>
        public void Rename(string newName)
        {
            newName = Environment.ExpandEnvironmentVariables(string.Format(Consts.VaultTokenCacheFileName, newName));
            if (File.Exists(newName))
            {
                File.Delete(newName);
            }
            File.Move(FileName, newName);
            FileName = newName;
        }

        /// <summary>
        /// Empties the persistent store
        /// </summary>
        public void Clear()
        {
            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }
        }

        /// <summary>
        /// Configures the token cache for an MSAL client application
        /// </summary>
        /// <param name="tokenCache">The MSAL token cache to configure</param>
        public void ConfigureTokenCache(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        /// <summary>
        /// Triggered right before MSAL needs to access the cache
        /// Reload the cache from the persistent store in case it changed since the last access
        /// </summary>
        /// <param name="args"></param>
        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                if (File.Exists(FileName))
                {
                    try
                    {
                        byte[] encryptedData = File.ReadAllBytes(FileName);
                        byte[] data = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                        args.TokenCache.DeserializeMsalV3(data);
                    }
                    catch (Exception)
                    {
                        // If decryption fails, clear the cache and start fresh
                        Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Triggered right after MSAL accessed the cache
        /// </summary>
        /// <param name="args"></param>
        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    try
                    {
                        // reflect changes in the persistent store
                        byte[] data = args.TokenCache.SerializeMsalV3();
                        byte[] encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                        File.WriteAllBytes(FileName, encryptedData);
                    }
                    catch (Exception)
                    {
                        // If encryption fails, don't crash the application
                        // Log the error if logging is available
                    }
                }
            }
        }
    }
}
